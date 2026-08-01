using System.IO;
using MalbersAnimations.Controller.AI;
using MalbersAnimations.PathCreation;
using Project.AI;
using Project.Creatures;
using Project.Data;
using Project.EditorTools;
using Project.World;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Creatures
{
    /// <summary>
    /// Creatures Manager — CRUD for <see cref="DMICreatureDefinition"/>, rigged mesh + anim slots,
    /// project AI, spit/loot. Legacy Malbers OnWolf kept in a foldout.
    /// Invector humanoid combatants stay on Enemy Prefab Creator.
    /// </summary>
    public class DMICreatureManagerWindow : EditorWindow
    {
        private DMICreatureDefinition[] definitions = System.Array.Empty<DMICreatureDefinition>();
        private int selectedDefinitionIndex = -1;
        private DMICreatureDefinition workingDefinition;
        private SerializedObject workingSerialized;

        private Vector2 listScroll;
        private Vector2 formScroll;

        private GameObject hierarchyVisualSource;
        private GameObject existingPrefabVisual;
        private bool placeInSceneAfterBuild = true;
        private bool showLegacyFoldout;
        private string statusMessage = string.Empty;
        private MessageType statusType = MessageType.Info;
        private string animStatusMessage = string.Empty;
        private PathCreator creaturePatrolPath;

        [MenuItem(SurvivalPioneerEditorMenus.CreatureManager, false, 25)]
        public static void ShowWindow()
        {
            DMICreatureManagerWindow window = GetWindow<DMICreatureManagerWindow>("Creatures Manager");
            window.minSize = new Vector2(880f, 620f);
            window.RefreshDefinitions();
        }

        [MenuItem(SurvivalPioneerEditorMenus.BuildSulfurHoundCreature, false, 26)]
        private static void BuildSulfurHoundQuick()
        {
            DMICreatureDefinition definition = DMICreaturePrefabBuilder.EnsureSulfurHoundDefinition();
            GameObject prefab = DMICreaturePrefabBuilder.BuildCreature(definition, null, out string prefabPath);
            if (prefab != null)
                Debug.Log($"[Creatures Manager] Built Sulfur Hound prefab at {prefabPath}");
            else
                Debug.LogError("[Creatures Manager] Failed to build Sulfur Hound prefab.");
        }

        [MenuItem(SurvivalPioneerEditorMenus.BuildSulfurHoundV2Creature, false, 26)]
        private static void BuildSulfurHoundV2Quick()
        {
            DMICreatureDefinition definition = DMICreaturePrefabBuilder.EnsureSulfurHoundV2Definition();
            GameObject prefab = DMICreaturePrefabBuilder.BuildCreature(definition, null, out string prefabPath);
            if (prefab != null)
                Debug.Log($"[Creatures Manager] Built Sulfur Hound V2-A prefab at {prefabPath}");
            else
                Debug.LogError("[Creatures Manager] Failed to build Sulfur Hound V2-A prefab.");
        }

        [MenuItem(SurvivalPioneerEditorMenus.BuildSulfurHoundBrain, false, 27)]
        private static void BuildSulfurHoundBrainQuick()
        {
            MAIState start = DMICreatureBrainAssetBuilder.EnsureSulfurHoundBrainGraph(out string path);
            DMICreatureDefinition definition = DMICreaturePrefabBuilder.EnsureSulfurHoundDefinition();
            if (definition != null && start != null)
            {
                definition.startBrainState = start;
                if (definition.spitVfxPrefab == null)
                    definition.spitVfxPrefab = DMICreatureParticleCatalog.LoadPoisonSpitPrefab();
                EditorUtility.SetDirty(definition);
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[Creatures Manager] Sulfur Hound brain graph ready. Start state: {path}");
            if (start != null)
                Selection.activeObject = start;
        }

        [MenuItem(SurvivalPioneerEditorMenus.RegisterSulfurHoundEncounter, false, 28)]
        private static void RegisterSulfurHoundEncounterQuick()
        {
            SurfaceEncounterTable table = DMICreatureWorldWireUtility.EnsureB1LifeformEncounterTable(out string message);
            Debug.Log($"[Creatures Manager] {message}");
            if (table != null)
            {
                Selection.activeObject = table;
                EditorGUIUtility.PingObject(table);
            }
        }

        [MenuItem(SurvivalPioneerEditorMenus.ValidateSulfurHoundSetup, false, 29)]
        private static void ValidateSulfurHoundSetupQuick()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                DMICreatureWorldWireUtility.SulfurHoundPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[Creatures Manager] Sulfur_Hound.prefab not found. Build it first.");
                return;
            }

            bool ok = DMICreatureWorldWireUtility.ValidateQuadrupedSetup(prefab, out string report);
            bool spawnReady = EnemyPrefabResolver.IsSpawnReady(prefab);
            Debug.Log($"[Creatures Manager] Validate: {report} | IsSpawnReady={spawnReady} | OK={ok}");
            Selection.activeObject = prefab;
        }

        [MenuItem(SurvivalPioneerEditorMenus.RebuildSulfurHoundReskin, false, 30)]
        private static void RebuildSulfurHoundReskinQuick()
        {
            DMICreatureDefinition definition = DMICreaturePrefabBuilder.EnsureSulfurHoundDefinition();
            definition.skipAutoReskin = true;
            definition.buildTrack = DMICreatureBuildTrack.MalbersAcV1;
            definition.threatSenseRange = 9f;
            definition.threatLeashMultiplier = 1.4f;
            EditorUtility.SetDirty(definition);
            string path;
            GameObject prefab = DMICreaturePrefabBuilder.BuildCreature(definition, null, out path);
            Debug.Log(
                $"[Creatures Manager] Rebuilt Sulfur Hound OnWolf (Houndv3, AutoReskin OFF) → {path} engage=9 leash×1.4");
            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
        }

        [MenuItem(SurvivalPioneerEditorMenus.LegacyCreatures + "Smoke Test RiggedNative Build", false, 40)]
        private static void SmokeTestRiggedNativeBuild()
        {
            const string defPath = "Assets/_Project/Data/Creatures/SmokeTest_RiggedNative.asset";
            DMICreatureDefinition def = AssetDatabase.LoadAssetAtPath<DMICreatureDefinition>(defPath);
            if (def == null)
            {
                def = ScriptableObject.CreateInstance<DMICreatureDefinition>();
                def.ApplyNewCreatureDefaults();
                def.creatureId = "smoke_test_rigged";
                def.displayName = "Smoke Test Rigged";
                def.prefabFileName = "SmokeTest_RiggedNative";
                def.rigArchetype = DMICreatureRigArchetype.QuadrupedGeneric;
                def.generateAnimatorFromSlots = true;
                def.enableRangedParticleAttack = false;
                def.visualMeshSource = AssetDatabase.LoadAssetAtPath<GameObject>(
                    DMICreaturePrefabBuilder.DefaultSulfurHoundMeshPath);
                AssetDatabase.CreateAsset(def, defPath);
            }

            DMICreatureAnimatorFactory.PullClipsFromModel(def, def.visualMeshSource);
            GameObject prefab = DMICreaturePrefabBuilder.BuildCreature(def, null, out string prefabPath);
            bool spawnReady = prefab != null && EnemyPrefabResolver.IsSpawnReady(prefab);
            bool hasAi = prefab != null && prefab.GetComponent<DMICreatureAiController>() != null;
            bool hasAnim = prefab != null
                           && prefab.GetComponentInChildren<Animator>(true) != null
                           && prefab.GetComponentInChildren<Animator>(true).runtimeAnimatorController != null;

            // Confirm Sulfur Legacy definition still points at Malbers track.
            DMICreatureDefinition sulfur = DMICreaturePrefabBuilder.EnsureSulfurHoundDefinition();
            bool sulfurLegacy = sulfur != null && sulfur.UsesLegacyMalbers;

            Debug.Log(
                $"[Creatures Manager] Smoke RiggedNative → {prefabPath} " +
                $"spawnReady={spawnReady} ai={hasAi} anim={hasAnim} sulfurLegacyOk={sulfurLegacy}");
            if (prefab == null || !spawnReady || !hasAi || !hasAnim || !sulfurLegacy)
                Debug.LogError("[Creatures Manager] Smoke Test RiggedNative FAILED.");
            else
                Debug.Log("[Creatures Manager] Smoke Test RiggedNative PASSED.");
        }

        private void OnEnable()
        {
            RefreshDefinitions();
            if (workingDefinition == null && definitions.Length > 0)
                LoadDefinition(definitions[0], 0);
        }

        private void OnSelectionChange()
        {
            if (workingDefinition == null)
                return;

            if (workingDefinition.visualSourceMode == DMICreatureVisualSourceMode.SelectedHierarchyObject
                && Selection.activeGameObject != null
                && !EditorUtility.IsPersistent(Selection.activeGameObject))
            {
                hierarchyVisualSource = Selection.activeGameObject;
                Repaint();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("DMI Creatures Manager", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Default: any rigged model (biped / quadruped / custom) + Idle/Walk/Run/Attack/Death clips " +
                "+ project AI. Assign a mesh, fill animation slots (or Pull from FBX), then Build. " +
                "Invector humanoid combatants stay on Enemy Prefab Creator. " +
                "Malbers OnWolf (Sulfur Hound) is under Legacy.",
                MessageType.Info);

            if (!string.IsNullOrEmpty(statusMessage))
                EditorGUILayout.HelpBox(statusMessage, statusType);

            EditorGUILayout.BeginHorizontal();
            DrawDefinitionList();
            DrawEditorPanel();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDefinitionList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250f));
            EditorGUILayout.LabelField("Definitions", EditorStyles.boldLabel);

            listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.ExpandHeight(true));
            for (int i = 0; i < definitions.Length; i++)
            {
                DMICreatureDefinition definition = definitions[i];
                if (definition == null)
                    continue;

                string label = string.IsNullOrWhiteSpace(definition.displayName)
                    ? definition.name
                    : definition.displayName;
                bool selected = i == selectedDefinitionIndex;
                if (GUILayout.Toggle(selected, label, "Button") && selectedDefinitionIndex != i)
                    LoadDefinition(definition, i);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("New", GUILayout.Height(28f)))
                CreateDefinitionAsset();
            if (GUILayout.Button("Duplicate", GUILayout.Height(28f)))
                DuplicateSelectedDefinition();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Height(24f)))
                RefreshDefinitions();
            using (new EditorGUI.DisabledScope(workingDefinition == null || selectedDefinitionIndex < 0))
            {
                if (GUILayout.Button("Delete", GUILayout.Height(24f)))
                    DeleteSelectedDefinition();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Sulfur Hound Preset", GUILayout.Height(28f)))
            {
                DMICreatureDefinition definition = DMICreaturePrefabBuilder.EnsureSulfurHoundDefinition();
                RefreshDefinitions();
                SelectDefinition(definition);
                SetStatus("Loaded Sulfur Hound definition + brain graph.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawEditorPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            if (workingDefinition == null)
            {
                EditorGUILayout.HelpBox("Create or select a creature definition.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            formScroll = EditorGUILayout.BeginScrollView(formScroll);

            EditorGUI.BeginChangeCheck();
            DrawIdentitySection();
            EditorGUILayout.Space(8f);
            DrawRigVisualSection();
            EditorGUILayout.Space(8f);
            DrawBrainSection();
            EditorGUILayout.Space(8f);
            // Threat/Melee sits next to Brain so Melee Interval is not buried under Anim/Health.
            DrawThreatSection();
            EditorGUILayout.Space(8f);
            DrawSensesSection();
            EditorGUILayout.Space(8f);
            DrawAnimationSection();
            EditorGUILayout.Space(8f);
            DrawHealthSection();
            EditorGUILayout.Space(8f);
            DrawSpitSection();
            EditorGUILayout.Space(8f);
            DrawAudioSection();
            EditorGUILayout.Space(8f);
            DrawLootSection();
            EditorGUILayout.Space(8f);
            DrawLegacyFoldout();
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(workingDefinition);

            EditorGUILayout.Space(12f);
            DrawSpawnReadyStatus();
            EditorGUILayout.Space(8f);
            DrawActionButtons();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawIdentitySection()
        {
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            workingDefinition.creatureId = EditorGUILayout.TextField("Creature Id", workingDefinition.creatureId);
            workingDefinition.displayName = EditorGUILayout.TextField("Display Name", workingDefinition.displayName);
            workingDefinition.prefabFileName = EditorGUILayout.TextField("Prefab File Name", workingDefinition.prefabFileName);
            workingDefinition.surfaceThreatKind = (SurfaceThreatKind)EditorGUILayout.EnumPopup(
                "Surface Threat Kind",
                workingDefinition.surfaceThreatKind);

            string trackLabel = workingDefinition.UsesLegacyMalbers
                ? "Legacy Malbers AC"
                : "Rigged Native (default)";
            EditorGUILayout.LabelField("Build Path", trackLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField("Definition Asset", workingDefinition, typeof(DMICreatureDefinition), false);
            if (GUILayout.Button(
                    new GUIContent(
                        "Rename Asset → Prefab Name",
                        "Renames the Definition .asset file to match Prefab File Name."),
                    GUILayout.Width(200f),
                    GUILayout.Height(18f)))
            {
                RenameDefinitionAssetToPrefabFileName();
            }
            EditorGUILayout.EndHorizontal();

            string assetPath = AssetDatabase.GetAssetPath(workingDefinition);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string assetFile = Path.GetFileNameWithoutExtension(assetPath);
                string target = SanitizeAssetFileName(workingDefinition.prefabFileName);
                if (!string.Equals(assetFile, target, System.StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(target))
                {
                    EditorGUILayout.HelpBox(
                        $"Definition file is '{assetFile}.asset' — Prefab File Name is '{target}'. " +
                        "Click Rename Asset → Prefab Name to sync.",
                        MessageType.None);
                }
            }
        }

        private void DrawRigVisualSection()
        {
            EditorGUILayout.LabelField("Rig / Visual", EditorStyles.boldLabel);
            workingDefinition.rigArchetype = (DMICreatureRigArchetype)EditorGUILayout.EnumPopup(
                "Rig Archetype",
                workingDefinition.rigArchetype);

            string tip = workingDefinition.rigArchetype == DMICreatureRigArchetype.BipedHumanoid
                ? "Biped: import FBX as Humanoid when you want shared humanoid clips. Otherwise Generic is fine."
                : "Quadruped / Custom: import FBX as Generic. Assign clips below (or Pull from FBX).";
            EditorGUILayout.HelpBox(tip, MessageType.None);

            workingDefinition.visualSourceMode = (DMICreatureVisualSourceMode)EditorGUILayout.EnumPopup(
                "Source Mode",
                workingDefinition.visualSourceMode);

            switch (workingDefinition.visualSourceMode)
            {
                case DMICreatureVisualSourceMode.SelectedHierarchyObject:
                    hierarchyVisualSource = (GameObject)EditorGUILayout.ObjectField(
                        "Hierarchy Model",
                        hierarchyVisualSource != null ? hierarchyVisualSource : Selection.activeGameObject,
                        typeof(GameObject),
                        true);
                    if (hierarchyVisualSource == null && Selection.activeGameObject != null
                        && !EditorUtility.IsPersistent(Selection.activeGameObject))
                        hierarchyVisualSource = Selection.activeGameObject;

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Use Current Selection", GUILayout.Width(180f)))
                    {
                        if (Selection.activeGameObject != null)
                            hierarchyVisualSource = Selection.activeGameObject;
                    }

                    if (GUILayout.Button("Assign To Definition Mesh", GUILayout.Width(200f))
                        && hierarchyVisualSource != null)
                    {
                        GameObject asset = PrefabUtility.GetCorrespondingObjectFromSource(hierarchyVisualSource);
                        workingDefinition.visualMeshSource = asset != null ? asset : hierarchyVisualSource;
                        EditorUtility.SetDirty(workingDefinition);
                    }
                    EditorGUILayout.EndHorizontal();
                    break;

                case DMICreatureVisualSourceMode.ExistingPrefab:
                    existingPrefabVisual = (GameObject)EditorGUILayout.ObjectField(
                        "Prefab / FBX Asset",
                        existingPrefabVisual != null
                            ? existingPrefabVisual
                            : workingDefinition.visualMeshSource,
                        typeof(GameObject),
                        false);
                    if (existingPrefabVisual != null)
                        workingDefinition.visualMeshSource = existingPrefabVisual;
                    break;

                case DMICreatureVisualSourceMode.DefinitionMesh:
                default:
                    workingDefinition.visualMeshSource = (GameObject)EditorGUILayout.ObjectField(
                        "Visual Mesh / FBX",
                        workingDefinition.visualMeshSource,
                        typeof(GameObject),
                        false);
                    break;
            }

            workingDefinition.visualMaterialSource = (Material)EditorGUILayout.ObjectField(
                "Material Source",
                workingDefinition.visualMaterialSource,
                typeof(Material),
                false);

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Material Source Emission", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Drives URP _EmissionColor on CreatureVisual via MaterialPropertyBlock. " +
                "Authored material glow = look at Emission Idle. Applied = authored × (intensity / Idle). " +
                "Idle 5 / Attack 10 ⇒ attack is 2× authored. Melee and ranged/spit both trigger Attack.",
                MessageType.None);
            workingDefinition.boostEmissionWhileAttacking = EditorGUILayout.Toggle(
                new GUIContent(
                    "Boost Emission While Attacking",
                    "Raise emission to Attack intensity for melee and ranged/spit Attack pulses."),
                workingDefinition.boostEmissionWhileAttacking);
            workingDefinition.flashEmissionWhileAttacking = EditorGUILayout.Toggle(
                new GUIContent(
                    "Flash While Attacking",
                    "Oscillate emission between Idle and Attack intensity during the attack lock."),
                workingDefinition.flashEmissionWhileAttacking);
            using (new EditorGUI.DisabledScope(!workingDefinition.boostEmissionWhileAttacking))
            {
                workingDefinition.emissionIdleIntensity = EditorGUILayout.FloatField(
                    new GUIContent(
                        "Emission Idle / Normal",
                        "Intensity unit for idle glow. Authored _EmissionColor is the look at this value (default 5)."),
                    workingDefinition.emissionIdleIntensity);
                if (workingDefinition.emissionIdleIntensity < 0.01f)
                    workingDefinition.emissionIdleIntensity = 0.01f;
                workingDefinition.emissionAttackIntensity = EditorGUILayout.FloatField(
                    new GUIContent(
                        "Emission Attack",
                        "Intensity while attacking. Relative to Idle (default 10 ⇒ 2× when Idle is 5)."),
                    workingDefinition.emissionAttackIntensity);
                if (workingDefinition.emissionAttackIntensity < 0.01f)
                    workingDefinition.emissionAttackIntensity = 0.01f;
                workingDefinition.emissionFlashRateHz = EditorGUILayout.FloatField(
                    new GUIContent(
                        "Flash Rate (Hz)",
                        "Oscillation rate when Flash While Attacking is on."),
                    workingDefinition.emissionFlashRateHz);
                if (workingDefinition.emissionFlashRateHz < 0.1f)
                    workingDefinition.emissionFlashRateHz = 0.1f;
                workingDefinition.emissionFlashTint = EditorGUILayout.ColorField(
                    new GUIContent(
                        "Flash Tint",
                        "Optional tint at flash peaks (HDR). White keeps authored hue."),
                    workingDefinition.emissionFlashTint,
                    true,
                    true,
                    true);
            }

            EditorGUILayout.Space(4f);
            workingDefinition.heightOffset = EditorGUILayout.FloatField(
                new GUIContent(
                    "Height Offset (m)",
                    "Applied to CreatureVisual.localY after auto feet-align. Negative lowers into ground when auto-align overshoots."),
                workingDefinition.heightOffset);
            workingDefinition.prefabScale = EditorGUILayout.FloatField(
                new GUIContent(
                    "Prefab Scale",
                    "Uniform scale on the creature root at build time (capsule + agent refit). 1 = source size."),
                workingDefinition.prefabScale);
            if (workingDefinition.prefabScale < 0.01f)
                workingDefinition.prefabScale = 0.01f;

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Melee Hit Reception (creature-side)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Raises/enlarges the hit capsule for short creatures so player melee connects. " +
                "Does not change player swing height (keeps tall enemies fair).",
                MessageType.None);
            workingDefinition.hitCapsuleHeight = EditorGUILayout.FloatField(
                new GUIContent(
                    "Hit Capsule Height (0=auto)",
                    "Optional height override in meters. 0 = fit from visual bounds."),
                workingDefinition.hitCapsuleHeight);
            if (workingDefinition.hitCapsuleHeight < 0f)
                workingDefinition.hitCapsuleHeight = 0f;
            workingDefinition.hitCapsuleRadius = EditorGUILayout.FloatField(
                new GUIContent(
                    "Hit Capsule Radius (0=auto)",
                    "Optional radius override in meters. 0 = fit from visual bounds."),
                workingDefinition.hitCapsuleRadius);
            if (workingDefinition.hitCapsuleRadius < 0f)
                workingDefinition.hitCapsuleRadius = 0f;
            workingDefinition.hitCapsuleCenterYOffset = EditorGUILayout.FloatField(
                new GUIContent(
                    "Hit Capsule Center Y Offset",
                    "Added to capsule center Y. Positive raises hit volume for low-to-ground creatures."),
                workingDefinition.hitCapsuleCenterYOffset);
            workingDefinition.meleeHitHeightMultiplier = EditorGUILayout.FloatField(
                new GUIContent(
                    "Hit Height Multiplier",
                    "Multiplies auto/override capsule height. Use >1 for short creatures."),
                workingDefinition.meleeHitHeightMultiplier);
            if (workingDefinition.meleeHitHeightMultiplier < 0.01f)
                workingDefinition.meleeHitHeightMultiplier = 0.01f;
            workingDefinition.meleeHitRadiusMultiplier = EditorGUILayout.FloatField(
                new GUIContent(
                    "Hit Radius Multiplier",
                    "Multiplies auto/override capsule radius."),
                workingDefinition.meleeHitRadiusMultiplier);
            if (workingDefinition.meleeHitRadiusMultiplier < 0.01f)
                workingDefinition.meleeHitRadiusMultiplier = 0.01f;

            DrawRigInspection(workingDefinition.visualMeshSource);
        }

        private static void DrawRigInspection(GameObject model)
        {
            if (model == null)
                return;

            string path = AssetDatabase.GetAssetPath(model);
            ModelImporter importer = string.IsNullOrEmpty(path)
                ? null
                : AssetImporter.GetAtPath(path) as ModelImporter;
            int clipCount = 0;
            int boneCount = 0;
            if (!string.IsNullOrEmpty(path))
            {
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                        clipCount++;
                }
            }

            SkinnedMeshRenderer smr = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr != null && smr.bones != null)
                boneCount = smr.bones.Length;

            string animType = importer != null ? importer.animationType.ToString() : "n/a";
            EditorGUILayout.LabelField(
                "Rig Inspect",
                $"Import={animType}  Bones≈{boneCount}  Clips={clipCount}");
        }

        private void DrawBrainSection()
        {
            EditorGUILayout.LabelField("Brain / Wander / Idle", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Brain profile drives locomotion mode, speeds, wander radius, patrol, and combat feature toggles. " +
                "Idle / Wander durations on the definition are CM authority (applied on Awake + build) and sync into the profile. " +
                "Melee Interval lives under Threat / Melee.",
                MessageType.None);

            workingDefinition.brainProfile = (DMICreatureBrainProfile)EditorGUILayout.ObjectField(
                "Brain Profile",
                workingDefinition.brainProfile,
                typeof(DMICreatureBrainProfile),
                false);

            // Definition-authority timing — editable even without a brain asset assigned.
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Idle / Wander Timing (Definition)", EditorStyles.miniBoldLabel);
            workingDefinition.idleDuration = EditorGUILayout.FloatField(
                new GUIContent(
                    "Idle Duration",
                    "Base seconds spent idle before next wander/patrol. Applied on Awake + build."),
                workingDefinition.idleDuration);
            if (workingDefinition.idleDuration < 0f)
                workingDefinition.idleDuration = 0f;

            workingDefinition.idleDurationVariation = EditorGUILayout.Slider(
                new GUIContent(
                    "Idle Duration Variation",
                    "Extra random idle time: wait = Idle Duration + Random(0, this). Clamp 0–10s."),
                workingDefinition.idleDurationVariation,
                0f,
                10f);

            workingDefinition.wanderDuration = EditorGUILayout.FloatField(
                new GUIContent(
                    "Wander Duration",
                    "Base max seconds for a wander walk before idle. 0 = no timeout (walk until arrival)."),
                workingDefinition.wanderDuration);
            if (workingDefinition.wanderDuration < 0f)
                workingDefinition.wanderDuration = 0f;

            workingDefinition.wanderDurationVariation = EditorGUILayout.Slider(
                new GUIContent(
                    "Wander Duration Variation",
                    "Extra random wander timeout: timeout = Wander Duration + Random(0, this). Clamp 0–10s."),
                workingDefinition.wanderDurationVariation,
                0f,
                10f);

            if (workingDefinition.brainProfile != null)
            {
                DMICreatureBrainProfile brain = workingDefinition.brainProfile;
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Movement Mode", EditorStyles.miniBoldLabel);
                brain.movementMode = (DMICreatureMovementMode)EditorGUILayout.EnumPopup(
                    new GUIContent("Movement Mode", "Stationary / Wander / Patrol."),
                    brain.movementMode);

                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField("Speeds", EditorStyles.miniBoldLabel);
                brain.walkSpeed = EditorGUILayout.FloatField(
                    new GUIContent("Walk Speed", "NavMesh / locomotion walk speed."),
                    brain.walkSpeed);
                brain.runSpeed = EditorGUILayout.FloatField(
                    new GUIContent("Run Speed", "Chase / combat run speed."),
                    brain.runSpeed);
                brain.turnSpeed = EditorGUILayout.FloatField(
                    new GUIContent("Turn Speed", "Slerp factor for FaceToward (melee / fallback facing)."),
                    brain.turnSpeed);
                brain.agentAngularSpeed = EditorGUILayout.FloatField(
                    new GUIContent("Agent Angular Speed", "NavMeshAgent.angularSpeed (deg/sec). 0 = leave agent default."),
                    brain.agentAngularSpeed);
                brain.stopDistance = EditorGUILayout.FloatField(
                    new GUIContent("Stop Distance", "Agent stopping distance to destinations."),
                    brain.stopDistance);

                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField("Wander / Sample", EditorStyles.miniBoldLabel);
                brain.wanderRadius = EditorGUILayout.FloatField(
                    new GUIContent("Wander Radius", "Max distance from home for wander points."),
                    brain.wanderRadius);
                brain.navMeshSampleRadius = EditorGUILayout.FloatField(
                    new GUIContent("NavMesh Sample Radius", "Sample radius when picking wander / patrol points."),
                    brain.navMeshSampleRadius);

                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField("Patrol", EditorStyles.miniBoldLabel);
                using (new EditorGUI.DisabledScope(brain.movementMode != DMICreatureMovementMode.Patrol))
                {
                    creaturePatrolPath = (PathCreator)EditorGUILayout.ObjectField(
                        new GUIContent(
                            "Path Creator",
                            "Path Creator / Path Creator Variant. Apply writes onto selected scene creature AI; persistent path assets also bake onto selected prefab assets."),
                        creaturePatrolPath,
                        typeof(PathCreator),
                        true);
                    if (GUILayout.Button("Apply Path Creator To Selected Creature"))
                        ApplyCreaturePatrolPathToSelection();

                    brain.patrolMode = (DMICreaturePatrolMode)EditorGUILayout.EnumPopup(
                        new GUIContent("Patrol Mode", "Loop or PingPong along path / generated points."),
                        brain.patrolMode);
                    brain.patrolPointCount = EditorGUILayout.IntField(
                        new GUIContent("Patrol Point Count", "Fallback generated points when no Path Creator is assigned."),
                        brain.patrolPointCount);
                    if (brain.patrolPointCount < 2)
                        brain.patrolPointCount = 2;
                    brain.patrolRadius = EditorGUILayout.FloatField(
                        new GUIContent("Patrol Radius", "Fallback radius when no Path Creator is assigned."),
                        brain.patrolRadius);
                    brain.patrolWaitDuration = EditorGUILayout.FloatField(
                        new GUIContent("Patrol Wait Duration", "Seconds to wait at each patrol point."),
                        brain.patrolWaitDuration);
                    EditorGUILayout.HelpBox(
                        "Preferred: Path Creator Variant — edit anchors with Path Creator Scene tools, then assign here. " +
                        "Build + Place in Scene applies the path to the instance. Circle points are a fallback when no path is set.",
                        MessageType.None);
                }

                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField("Combat Features", EditorStyles.miniBoldLabel);
                brain.allowChase = EditorGUILayout.Toggle(
                    new GUIContent("Allow Chase", "Pursue acquired threats."),
                    brain.allowChase);
                brain.allowMelee = EditorGUILayout.Toggle(
                    new GUIContent("Allow Melee", "Enter melee state and deal melee hits."),
                    brain.allowMelee);
                brain.allowRangedSpit = EditorGUILayout.Toggle(
                    new GUIContent(
                        "Allow Ranged Spit",
                        "Also requires Enable Ranged Attack + spit VFX on the definition."),
                    brain.allowRangedSpit);

                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField("Melee Timing", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox(
                    "Melee Interval = seconds between hits (definition). " +
                    "Melee Attack Lock = Attack anim lock only — not the hit interval.",
                    MessageType.None);

                // Definition-authority hit spacing (same fields as Threat / Melee).
                workingDefinition.meleeAttackCooldown = EditorGUILayout.FloatField(
                    new GUIContent(
                        "Melee Interval (sec)",
                        "Seconds between melee hits (definition.meleeAttackCooldown → brain.meleeHitInterval)."),
                    workingDefinition.meleeAttackCooldown);
                if (workingDefinition.meleeAttackCooldown < 0.05f)
                    workingDefinition.meleeAttackCooldown = 0.05f;

                workingDefinition.meleeIntervalVariation = EditorGUILayout.Slider(
                    new GUIContent(
                        "Melee Interval Variation",
                        "Extra random delay: wait = Melee Interval + Random(0, this). Clamp 0–10s."),
                    workingDefinition.meleeIntervalVariation,
                    0f,
                    10f);

                brain.meleeAttackLockDuration = EditorGUILayout.FloatField(
                    new GUIContent(
                        "Melee Attack Lock (sec)",
                        "Attack anim lock / windup only (brain.meleeAttackLockDuration). Not the time between hits."),
                    brain.meleeAttackLockDuration);
                if (brain.meleeAttackLockDuration < 0f)
                    brain.meleeAttackLockDuration = 0f;

                // Sync definition timing authority into the assigned profile.
                brain.idleDurationMin = workingDefinition.idleDuration;
                brain.idleDurationMax = workingDefinition.idleDuration;
                brain.idleDurationVariation = workingDefinition.idleDurationVariation;
                brain.wanderDuration = workingDefinition.wanderDuration;
                brain.wanderDurationVariation = workingDefinition.wanderDurationVariation;
                brain.meleeHitInterval = workingDefinition.meleeAttackCooldown;

                if (EditorGUI.EndChangeCheck())
                    EditorUtility.SetDirty(brain);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Assign a brain profile (or use Wander / Patrol / Stationary Guard) to edit speeds, radius, patrol, and combat toggles.",
                    MessageType.Warning);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Wander", GUILayout.Height(24f)))
            {
                workingDefinition.brainProfile = DMICreatureBrainProfileUtility.EnsureWanderProfile();
                EditorUtility.SetDirty(workingDefinition);
                SetStatus("Assigned DMI_Brain_Wander.", MessageType.Info);
            }

            if (GUILayout.Button("Patrol", GUILayout.Height(24f)))
            {
                workingDefinition.brainProfile = DMICreatureBrainProfileUtility.EnsurePatrolProfile();
                EditorUtility.SetDirty(workingDefinition);
                SetStatus("Assigned DMI_Brain_Patrol.", MessageType.Info);
            }

            if (GUILayout.Button("Stationary Guard", GUILayout.Height(24f)))
            {
                workingDefinition.brainProfile = DMICreatureBrainProfileUtility.EnsureStationaryGuardProfile();
                EditorUtility.SetDirty(workingDefinition);
                SetStatus("Assigned DMI_Brain_StationaryGuard.", MessageType.Info);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(workingDefinition.brainProfile == null))
            {
                if (GUILayout.Button("Select Brain Asset", GUILayout.Height(22f)))
                {
                    Selection.activeObject = workingDefinition.brainProfile;
                    EditorGUIUtility.PingObject(workingDefinition.brainProfile);
                }
            }

            if (GUILayout.Button("Ping BrainProfiles Folder", GUILayout.Height(22f)))
            {
                CraftingEditorUtility.EnsureFolder(DMICreatureBrainProfileUtility.BrainProfilesFolder);
                Object folder = AssetDatabase.LoadAssetAtPath<Object>(
                    DMICreatureBrainProfileUtility.BrainProfilesFolder);
                if (folder != null)
                {
                    EditorGUIUtility.PingObject(folder);
                    Selection.activeObject = folder;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(workingDefinition.brainProfile == null))
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Save Brain Profile",
                            "Flush definition timing → assigned brain profile, then Dirty + SaveAssets on that profile asset."),
                        GUILayout.Height(26f)))
                {
                    SaveAssignedBrainProfile();
                }
            }

            if (GUILayout.Button(
                    new GUIContent(
                        "Save As New Brain Profile…",
                        "Duplicate current knobs into a new BrainProfiles asset, assign it, then save."),
                    GUILayout.Height(26f)))
            {
                SaveAsNewBrainProfile();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Copies definition-authority timing onto the assigned profile, then persists the profile asset.
        /// Does not replace saving the creature definition asset itself.
        /// </summary>
        private void SaveAssignedBrainProfile()
        {
            if (workingDefinition == null)
            {
                SetStatus("No creature definition selected.", MessageType.Warning);
                return;
            }

            EnsureWorkingSerialized();
            workingSerialized?.ApplyModifiedProperties();
            SyncDefinitionTimingIntoBrainProfile(workingDefinition);

            DMICreatureBrainProfile brain = workingDefinition.brainProfile;
            if (brain == null)
            {
                SetStatus("Assign a brain profile first (or use Save As New).", MessageType.Warning);
                return;
            }

            EditorUtility.SetDirty(brain);
            EditorUtility.SetDirty(workingDefinition);
            AssetDatabase.SaveAssets();
            string path = AssetDatabase.GetAssetPath(brain);
            SetStatus($"Saved brain profile: {Path.GetFileName(path)}", MessageType.Info);
            EditorGUIUtility.PingObject(brain);
        }

        private void SaveAsNewBrainProfile()
        {
            if (workingDefinition == null)
            {
                SetStatus("No creature definition selected.", MessageType.Warning);
                return;
            }

            EnsureWorkingSerialized();
            workingSerialized?.ApplyModifiedProperties();
            SyncDefinitionTimingIntoBrainProfile(workingDefinition);

            string defaultName = "DMI_Brain_"
                + SanitizeAssetFileName(
                    string.IsNullOrWhiteSpace(workingDefinition.creatureId)
                        ? workingDefinition.displayName
                        : workingDefinition.creatureId);
            if (string.IsNullOrWhiteSpace(defaultName) || defaultName == "DMI_Brain_")
                defaultName = "DMI_Brain_Custom";

            string fileName = EditorUtility.SaveFilePanelInProject(
                "Save As New Brain Profile",
                defaultName,
                "asset",
                "Create a new creature brain profile asset.",
                DMICreatureBrainProfileUtility.BrainProfilesFolder);
            if (string.IsNullOrEmpty(fileName))
                return;

            CraftingEditorUtility.EnsureFolder(DMICreatureBrainProfileUtility.BrainProfilesFolder);

            DMICreatureBrainProfile source = workingDefinition.brainProfile;
            DMICreatureBrainProfile created;
            if (source != null)
            {
                created = Object.Instantiate(source);
                created.name = Path.GetFileNameWithoutExtension(fileName);
            }
            else
            {
                created = ScriptableObject.CreateInstance<DMICreatureBrainProfile>();
                created.name = Path.GetFileNameWithoutExtension(fileName);
                created.ApplyWanderDefaults();
            }

            // Re-apply definition timing onto the new asset after clone.
            workingDefinition.brainProfile = created;
            SyncDefinitionTimingIntoBrainProfile(workingDefinition);

            AssetDatabase.CreateAsset(created, fileName);
            workingDefinition.brainProfile = created;
            EditorUtility.SetDirty(created);
            EditorUtility.SetDirty(workingDefinition);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
            SetStatus($"Created and assigned brain profile: {Path.GetFileName(fileName)}", MessageType.Info);
        }

        /// <summary>
        /// Definition owns idle/wander/melee interval; profile owns locomotion + combat toggles + attack lock.
        /// Flush definition → profile so Save Brain writes a coherent asset.
        /// </summary>
        private static void SyncDefinitionTimingIntoBrainProfile(DMICreatureDefinition definition)
        {
            if (definition == null || definition.brainProfile == null)
                return;

            DMICreatureBrainProfile brain = definition.brainProfile;
            brain.idleDurationMin = definition.idleDuration;
            brain.idleDurationMax = definition.idleDuration;
            brain.idleDurationVariation = definition.idleDurationVariation;
            brain.wanderDuration = definition.wanderDuration;
            brain.wanderDurationVariation = definition.wanderDurationVariation;
            brain.meleeHitInterval = Mathf.Max(0.05f, definition.meleeAttackCooldown);
            EditorUtility.SetDirty(brain);
        }

        private void DrawAnimationSection()
        {
            EditorGUILayout.LabelField("Animations", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Add any number of entries (state name + clip). Use Idle / Walk / Run / Attack / Death " +
                "for locomotion AI; extras become custom Animator states. Missing required states fall back " +
                "(e.g. Run←Walk). Size the list to add/remove slots.",
                MessageType.None);

            workingDefinition.EnsureAnimationEntriesMigrated();
            workingDefinition.generateAnimatorFromSlots = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    "Generate Animator Controller from animation entries",
                    "When enabled, Build regenerates the AnimatorController from the Animation Entries list below."),
                workingDefinition.generateAnimatorFromSlots);

            EnsureWorkingSerialized();
            if (workingSerialized != null)
            {
                workingSerialized.Update();
                SerializedProperty animProp = workingSerialized.FindProperty("animationEntries");
                if (animProp != null)
                    EditorGUILayout.PropertyField(animProp, new GUIContent("Animation Entries"), true);
                workingSerialized.ApplyModifiedProperties();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Default Locomotion Slots", GUILayout.Height(22f)))
            {
                AppendDefaultLocomotionSlots();
            }

            if (GUILayout.Button("Clear Empty Slots", GUILayout.Height(22f)))
            {
                ClearEmptyAnimationSlots();
            }
            EditorGUILayout.EndHorizontal();

            workingDefinition.v2AnimatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
                "Animator Controller",
                workingDefinition.v2AnimatorController,
                typeof(RuntimeAnimatorController),
                false);

            EditorGUILayout.BeginHorizontal();
            GameObject pullSource = ResolveBuildVisual() ?? workingDefinition.visualMeshSource;
            using (new EditorGUI.DisabledScope(pullSource == null))
            {
                if (GUILayout.Button("Pull Clips From Model FBX", GUILayout.Height(26f)))
                {
                    int n = DMICreatureAnimatorFactory.PullClipsFromModel(workingDefinition, pullSource);
                    animStatusMessage = n > 0
                        ? $"Pulled/assigned {n} clip(s) from {pullSource.name}."
                        : "No clips found on model (try assigning manually).";
                    workingSerialized = null;
                    SetStatus(animStatusMessage, n > 0 ? MessageType.Info : MessageType.Warning);
                }
            }

            if (GUILayout.Button("Generate Controller Preview", GUILayout.Height(26f)))
            {
                RuntimeAnimatorController ctrl = DMICreatureAnimatorFactory.BuildOrUpdateController(
                    workingDefinition,
                    out string msg);
                animStatusMessage = msg;
                SetStatus(msg, ctrl != null ? MessageType.Info : MessageType.Error);
                if (ctrl != null)
                {
                    Selection.activeObject = ctrl;
                    EditorGUIUtility.PingObject(ctrl);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(animStatusMessage))
                EditorGUILayout.HelpBox(animStatusMessage, MessageType.None);
        }

        private void AppendDefaultLocomotionSlots()
        {
            workingDefinition.EnsureAnimationEntriesMigrated();
            var list = new System.Collections.Generic.List<DMICreatureAnimEntry>();
            if (workingDefinition.animationEntries != null)
                list.AddRange(workingDefinition.animationEntries);

            string[] defaults = { "Idle", "Walk", "Run", "Attack", "Death" };
            for (int d = 0; d < defaults.Length; d++)
            {
                bool exists = false;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] != null
                        && string.Equals(list[i].stateName, defaults[d], System.StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    list.Add(new DMICreatureAnimEntry { stateName = defaults[d] });
            }

            workingDefinition.animationEntries = list.ToArray();
            EditorUtility.SetDirty(workingDefinition);
            workingSerialized = null;
            SetStatus("Ensured Idle/Walk/Run/Attack/Death slots exist.", MessageType.Info);
        }

        private void ClearEmptyAnimationSlots()
        {
            if (workingDefinition.animationEntries == null)
                return;

            var kept = new System.Collections.Generic.List<DMICreatureAnimEntry>();
            for (int i = 0; i < workingDefinition.animationEntries.Length; i++)
            {
                DMICreatureAnimEntry e = workingDefinition.animationEntries[i];
                if (e == null)
                    continue;
                if (e.clip == null && string.IsNullOrWhiteSpace(e.stateName))
                    continue;
                kept.Add(e);
            }

            workingDefinition.animationEntries = kept.ToArray();
            EditorUtility.SetDirty(workingDefinition);
            workingSerialized = null;
            SetStatus($"Cleared empty slots — {kept.Count} entries remain.", MessageType.Info);
        }

        private void RenameDefinitionAssetToPrefabFileName()
        {
            if (workingDefinition == null)
                return;

            string targetName = SanitizeAssetFileName(workingDefinition.prefabFileName);
            if (string.IsNullOrWhiteSpace(targetName))
            {
                SetStatus("Prefab File Name is empty — set it before renaming the asset.", MessageType.Warning);
                return;
            }

            string path = AssetDatabase.GetAssetPath(workingDefinition);
            if (string.IsNullOrEmpty(path))
            {
                SetStatus("Definition has no asset path.", MessageType.Error);
                return;
            }

            string current = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(current, targetName, System.StringComparison.Ordinal))
            {
                SetStatus($"Definition asset already named '{targetName}.asset'.", MessageType.Info);
                return;
            }

            SaveDefinition();
            string error = AssetDatabase.RenameAsset(path, targetName);
            if (!string.IsNullOrEmpty(error))
            {
                SetStatus($"Rename failed: {error}", MessageType.Error);
                return;
            }

            AssetDatabase.SaveAssets();
            RefreshDefinitions();
            SelectDefinition(workingDefinition);
            SetStatus($"Renamed definition asset → {targetName}.asset", MessageType.Info);
        }

        private static string SanitizeAssetFileName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            foreach (char c in Path.GetInvalidFileNameChars())
                raw = raw.Replace(c, '_');
            return raw.Trim().Replace(' ', '_');
        }

        private void DrawLegacyFoldout()
        {
            showLegacyFoldout = EditorGUILayout.Foldout(
                showLegacyFoldout,
                "Legacy — Malbers AC / OnWolf (Sulfur Hound)",
                true);
            if (!showLegacyFoldout)
                return;

            EditorGUILayout.HelpBox(
                "Only for existing Sulfur Hound OnWolf / Malbers AC rebuilds. " +
                "New creatures should stay on Rigged Native.",
                MessageType.Warning);

            bool useLegacy = EditorGUILayout.Toggle(
                "Use Legacy Malbers AC Build",
                workingDefinition.UsesLegacyMalbers);
            if (useLegacy && !workingDefinition.UsesLegacyMalbers)
                workingDefinition.buildTrack = DMICreatureBuildTrack.MalbersAcV1;
            else if (!useLegacy && workingDefinition.UsesLegacyMalbers)
                workingDefinition.buildTrack = DMICreatureBuildTrack.RiggedNative;

            using (new EditorGUI.DisabledScope(!workingDefinition.UsesLegacyMalbers))
            {
                workingDefinition.acTemplate = (DMIAnimalControllerTemplate)EditorGUILayout.EnumPopup(
                    "AC Template",
                    workingDefinition.acTemplate);
                workingDefinition.startBrainState = (MAIState)EditorGUILayout.ObjectField(
                    "Start Brain State",
                    workingDefinition.startBrainState,
                    typeof(MAIState),
                    false);
                workingDefinition.skipAutoReskin = EditorGUILayout.Toggle(
                    new GUIContent(
                        "Skip AutoReskin (OnWolf)",
                        "Blender-authored mesh already uses Wolf bone names."),
                    workingDefinition.skipAutoReskin);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Load Houndv3 OnWolf", GUILayout.Height(24f)))
                {
                    workingDefinition.visualMeshSource =
                        DMICreaturePrefabBuilder.LoadSulfurHoundOnWolfVisual();
                    workingDefinition.visualMaterialSource = AssetDatabase.LoadAssetAtPath<Material>(
                        DMICreaturePrefabBuilder.SulfurHoundV2UnlitMaterialPath);
                    workingDefinition.skipAutoReskin = true;
                    workingDefinition.buildTrack = DMICreatureBuildTrack.MalbersAcV1;
                    workingDefinition.rigArchetype = DMICreatureRigArchetype.QuadrupedGeneric;
                }

                if (GUILayout.Button("Rebuild Brain Graph", GUILayout.Height(24f)))
                {
                    MAIState start = DMICreatureBrainAssetBuilder.EnsureSulfurHoundBrainGraph(out string path);
                    workingDefinition.startBrainState = start;
                    EditorUtility.SetDirty(workingDefinition);
                    AssetDatabase.SaveAssets();
                    SetStatus($"Brain graph rebuilt: {path}", MessageType.Info);
                }

                using (new EditorGUI.DisabledScope(workingDefinition.startBrainState == null))
                {
                    if (GUILayout.Button("Open Brain", GUILayout.Height(24f)))
                        OpenBrain(workingDefinition.startBrainState);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawHealthSection()
        {
            EditorGUILayout.LabelField("Health & Progression", EditorStyles.boldLabel);
            workingDefinition.maxHealth = EditorGUILayout.FloatField("Max Health", workingDefinition.maxHealth);
            workingDefinition.destroyOnDeath = EditorGUILayout.Toggle("Destroy On Death", workingDefinition.destroyOnDeath);
            workingDefinition.destroyDelay = EditorGUILayout.FloatField("Destroy Delay", workingDefinition.destroyDelay);
            workingDefinition.dissolveOnDeath = EditorGUILayout.Toggle(
                new GUIContent(
                    "Dissolve on Death",
                    "Uses EnemyDisintegrationEffect + EnemyDeathSequence (Project/EnemyDisintegrate)."),
                workingDefinition.dissolveOnDeath);
            using (new EditorGUI.DisabledScope(!workingDefinition.dissolveOnDeath))
            {
                workingDefinition.preDisintegrationDelay = EditorGUILayout.FloatField(
                    new GUIContent(
                        "Pre-Dissolve Delay",
                        "Seconds of death pose before dissolve starts."),
                    workingDefinition.preDisintegrationDelay);
                if (workingDefinition.preDisintegrationDelay < 0f)
                    workingDefinition.preDisintegrationDelay = 0f;
            }

            workingDefinition.xpReward = EditorGUILayout.IntField("XP Reward", workingDefinition.xpReward);
            workingDefinition.showFloatingHealthBar = EditorGUILayout.Toggle(
                "Show Health Bar",
                workingDefinition.showFloatingHealthBar);
            workingDefinition.hideHealthBarUntilDamaged = EditorGUILayout.Toggle(
                "Hide Until Damaged",
                workingDefinition.hideHealthBarUntilDamaged);
            workingDefinition.healthBarOffset = EditorGUILayout.Vector3Field(
                "Health Bar Offset",
                workingDefinition.healthBarOffset);
        }

        private void DrawThreatSection()
        {
            EditorGUILayout.LabelField("Threat / Melee", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Melee Interval (sec) = time between melee hits (definition.meleeAttackCooldown). " +
                "Melee Attack Lock is under Brain → Combat Features (anim lock only).",
                MessageType.None);
            workingDefinition.threatSenseRange = EditorGUILayout.FloatField(
                "Engage Range",
                workingDefinition.threatSenseRange);
            workingDefinition.threatLeashMultiplier = EditorGUILayout.FloatField(
                "Leash Multiplier",
                workingDefinition.threatLeashMultiplier);
            workingDefinition.loseTargetDelay = EditorGUILayout.FloatField(
                "Lose Target Delay",
                workingDefinition.loseTargetDelay);
            workingDefinition.meleeEngageRange = EditorGUILayout.FloatField(
                "Melee Engage Range",
                workingDefinition.meleeEngageRange);
            workingDefinition.meleeDamage = EditorGUILayout.FloatField("Melee Damage", workingDefinition.meleeDamage);
            workingDefinition.meleeAttackCooldown = EditorGUILayout.FloatField(
                new GUIContent(
                    "Melee Interval (sec)",
                    "Seconds between melee hits (definition.meleeAttackCooldown). Overrides brain.meleeHitInterval on build/runtime. Not Attack Lock."),
                workingDefinition.meleeAttackCooldown);
            if (workingDefinition.meleeAttackCooldown < 0.05f)
                workingDefinition.meleeAttackCooldown = 0.05f;

            workingDefinition.meleeIntervalVariation = EditorGUILayout.Slider(
                new GUIContent(
                    "Melee Interval Variation",
                    "Extra random delay after each hit: wait = Melee Interval + Random(0, this). Clamp 0–10s. 0 = deterministic."),
                workingDefinition.meleeIntervalVariation,
                0f,
                10f);

            // Keep linked brain profile in sync so Brain section readout matches CM.
            if (workingDefinition.brainProfile != null
                && !Mathf.Approximately(
                    workingDefinition.brainProfile.meleeHitInterval,
                    workingDefinition.meleeAttackCooldown))
            {
                workingDefinition.brainProfile.meleeHitInterval = workingDefinition.meleeAttackCooldown;
                EditorUtility.SetDirty(workingDefinition.brainProfile);
            }
        }

        private void DrawSensesSection()
        {
            EditorGUILayout.LabelField("AI Senses", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Engage Range (Threat / Melee) is vision/proximity acquisition. " +
                "Hearing listens for ranged projectile/hitscan impacts (walls or targets) via EnemyNoiseEvents. " +
                "Aggro On Damaged keeps the existing Skitter ranged-damage Chase fix.",
                MessageType.None);

            workingDefinition.senseHearingEnabled = EditorGUILayout.Toggle(
                new GUIContent("Hearing Enabled", "Listen for combat-impact noise events."),
                workingDefinition.senseHearingEnabled);

            using (new EditorGUI.DisabledScope(!workingDefinition.senseHearingEnabled))
            {
                workingDefinition.hearingRange = EditorGUILayout.FloatField(
                    new GUIContent(
                        "Hearing Range",
                        "Audio sense radius. Hears impacts within Hearing Range + impact noise radius (~10m)."),
                    workingDefinition.hearingRange);
                if (workingDefinition.hearingRange < 0f)
                    workingDefinition.hearingRange = 0f;

                workingDefinition.hearingAggroChance = EditorGUILayout.Slider(
                    new GUIContent(
                        "Hearing Aggro Chance",
                        "0–1 chance to Chase the resolved shooter when a ranged impact is heard."),
                    workingDefinition.hearingAggroChance,
                    0f,
                    1f);

                workingDefinition.hearingCooldown = EditorGUILayout.FloatField(
                    new GUIContent(
                        "Hearing Cooldown",
                        "Seconds between hearing-aggro rolls (prevents burst-fire spam)."),
                    workingDefinition.hearingCooldown);
                if (workingDefinition.hearingCooldown < 0f)
                    workingDefinition.hearingCooldown = 0f;
            }

            workingDefinition.aggroOnDamaged = EditorGUILayout.Toggle(
                new GUIContent("Aggro On Damaged", "Direct melee/ranged damage pulls into Chase."),
                workingDefinition.aggroOnDamaged);
            workingDefinition.aggroOnHeardHit = EditorGUILayout.Toggle(
                new GUIContent(
                    "Aggro On Heard Hit",
                    "Nearby ranged impacts (including wall hits) may pull into Chase."),
                workingDefinition.aggroOnHeardHit);

            EditorGUILayout.LabelField(
                $"Engage / Vision Range: {workingDefinition.threatSenseRange:0.##} m (Threat section)",
                EditorStyles.miniLabel);
        }

        private void DrawSpitSection()
        {
            EditorGUILayout.LabelField("Ranged Particle Attack", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Pick any particle from Assets/_Project/Prefabs/Particles — Poison Spit, FireBreath, Plasma Ball, etc. " +
                "Ranged Attack Cooldown is the seconds between shots (RiggedNative). " +
                "Interval Variation adds 0..N seconds after each shot. " +
                "Base/View chance is for Malbers brain prefer-spit weighting only.",
                MessageType.None);

            workingDefinition.enableRangedParticleAttack = EditorGUILayout.Toggle(
                "Enable Ranged Attack",
                workingDefinition.enableRangedParticleAttack);

            using (new EditorGUI.DisabledScope(!workingDefinition.enableRangedParticleAttack))
            {
                workingDefinition.spitBaseChance = EditorGUILayout.Slider(
                    "Base Chance",
                    workingDefinition.spitBaseChance,
                    0f,
                    1f);
                workingDefinition.spitViewBoostedChance = EditorGUILayout.Slider(
                    "View Boosted Chance",
                    workingDefinition.spitViewBoostedChance,
                    0f,
                    1f);
                workingDefinition.spitRange = EditorGUILayout.FloatField("Range", workingDefinition.spitRange);
                workingDefinition.spitCooldown = EditorGUILayout.FloatField(
                    new GUIContent(
                        "Ranged Attack Cooldown",
                        "Seconds between ranged / spit shots (base). Applied to DMISulfurSpitAttack on build and Awake."),
                    workingDefinition.spitCooldown);
                if (workingDefinition.spitCooldown < 0.05f)
                    workingDefinition.spitCooldown = 0.05f;

                workingDefinition.spitCooldownVariation = EditorGUILayout.Slider(
                    new GUIContent(
                        "Ranged Interval Variation",
                        "Extra random delay after each shot: wait = Ranged Cooldown + Random(0, this). Clamp 0–10s."),
                    workingDefinition.spitCooldownVariation,
                    0f,
                    10f);

                workingDefinition.spitDamage = EditorGUILayout.FloatField("Damage", workingDefinition.spitDamage);

                GameObject picked = DMICreatureParticleCatalog.DrawParticlePopup(
                    "Particle Prefab",
                    workingDefinition.spitVfxPrefab);
                if (picked != workingDefinition.spitVfxPrefab)
                {
                    workingDefinition.spitVfxPrefab = picked;
                    EditorUtility.SetDirty(workingDefinition);
                }

                workingDefinition.spitVfxPrefab = (GameObject)EditorGUILayout.ObjectField(
                    "Or Drag Prefab",
                    workingDefinition.spitVfxPrefab,
                    typeof(GameObject),
                    false);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Poison Spit", GUILayout.Height(22f)))
                    AssignParticlePreset(DMICreatureParticleCatalog.LoadPoisonSpitPrefab());
                if (GUILayout.Button("FireBreath 2", GUILayout.Height(22f)))
                    AssignParticlePreset(DMICreatureParticleCatalog.FindByName("FireBreath 2"));
                if (GUILayout.Button("Plasma Ball 1", GUILayout.Height(22f)))
                    AssignParticlePreset(DMICreatureParticleCatalog.FindByName("Plasma Ball 1"));
                if (GUILayout.Button("FireBall 1", GUILayout.Height(22f)))
                    AssignParticlePreset(DMICreatureParticleCatalog.FindByName("FireBall 1"));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Ping Particles Folder", GUILayout.Height(22f)))
                {
                    Object folder = AssetDatabase.LoadAssetAtPath<Object>(ProjectAssetPaths.PrefabsParticles);
                    if (folder != null)
                    {
                        EditorGUIUtility.PingObject(folder);
                        Selection.activeObject = folder;
                    }
                }

                using (new EditorGUI.DisabledScope(workingDefinition.spitVfxPrefab == null))
                {
                    if (GUILayout.Button("Select Assigned Particle", GUILayout.Height(22f)))
                    {
                        Selection.activeObject = workingDefinition.spitVfxPrefab;
                        EditorGUIUtility.PingObject(workingDefinition.spitVfxPrefab);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawAudioSection()
        {
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Walk footsteps play on an interval while moving (not idle). " +
                "Ranged clip fires with spit / ranged Attack. Melee clip is optional. " +
                "Death Audio Clip fires once when death starts (with Death anim, before dissolve). " +
                "Leave empty until you assign clips — Build adds AudioSource automatically.",
                MessageType.None);

            workingDefinition.walkFootstepClip = (AudioClip)EditorGUILayout.ObjectField(
                new GUIContent("Walk Footstep", "Primary move / footstep one-shot while moving."),
                workingDefinition.walkFootstepClip,
                typeof(AudioClip),
                false);

            EnsureWorkingSerialized();
            if (workingSerialized != null)
            {
                workingSerialized.Update();
                SerializedProperty variants = workingSerialized.FindProperty("walkFootstepVariants");
                if (variants != null)
                    EditorGUILayout.PropertyField(
                        variants,
                        new GUIContent("Walk Variants", "Optional extra footstep clips (random pick)."),
                        true);
                workingSerialized.ApplyModifiedProperties();
            }

            workingDefinition.walkVolume = EditorGUILayout.Slider(
                "Walk Volume",
                workingDefinition.walkVolume,
                0f,
                1f);
            workingDefinition.footstepInterval = EditorGUILayout.FloatField(
                new GUIContent("Footstep Interval", "Seconds between footstep one-shots while moving."),
                workingDefinition.footstepInterval);
            if (workingDefinition.footstepInterval < 0.05f)
                workingDefinition.footstepInterval = 0.05f;

            EditorGUILayout.Space(4f);
            workingDefinition.rangedAttackClip = (AudioClip)EditorGUILayout.ObjectField(
                new GUIContent("Ranged Attack", "One-shot when spit / ranged fires."),
                workingDefinition.rangedAttackClip,
                typeof(AudioClip),
                false);
            workingDefinition.rangedAttackVolume = EditorGUILayout.Slider(
                "Ranged Volume",
                workingDefinition.rangedAttackVolume,
                0f,
                1f);

            workingDefinition.meleeAttackClip = (AudioClip)EditorGUILayout.ObjectField(
                new GUIContent("Melee Attack", "Optional one-shot when melee Attack fires."),
                workingDefinition.meleeAttackClip,
                typeof(AudioClip),
                false);
            workingDefinition.meleeAttackVolume = EditorGUILayout.Slider(
                "Melee Volume",
                workingDefinition.meleeAttackVolume,
                0f,
                1f);

            EditorGUILayout.Space(4f);
            workingDefinition.deathAudioClip = (AudioClip)EditorGUILayout.ObjectField(
                new GUIContent(
                    "Death Audio Clip",
                    "One-shot when death starts (definition.deathAudioClip). Same moment as Death anim; before dissolve."),
                workingDefinition.deathAudioClip,
                typeof(AudioClip),
                false);
            workingDefinition.deathVolume = EditorGUILayout.Slider(
                new GUIContent("Death Volume", "Volume for Death Audio Clip (0–1)."),
                workingDefinition.deathVolume,
                0f,
                1f);

            EditorGUILayout.Space(4f);
            workingDefinition.audioMinDistance = EditorGUILayout.FloatField(
                "Audio Min Distance",
                workingDefinition.audioMinDistance);
            workingDefinition.audioMaxDistance = EditorGUILayout.FloatField(
                "Audio Max Distance",
                workingDefinition.audioMaxDistance);
        }

        private void AssignParticlePreset(GameObject particle)
        {
            if (particle == null)
            {
                SetStatus("Particle prefab not found in Prefabs/Particles.", MessageType.Warning);
                return;
            }

            workingDefinition.spitVfxPrefab = particle;
            EditorUtility.SetDirty(workingDefinition);
            SetStatus($"Assigned ranged particle: {particle.name}", MessageType.Info);
        }

        private void DrawLootSection()
        {
            EditorGUILayout.LabelField("Loot", EditorStyles.boldLabel);
            workingDefinition.enableLoot = EditorGUILayout.Toggle("Enable Loot", workingDefinition.enableLoot);
            workingDefinition.acDropMin = EditorGUILayout.IntField("AC Drop Min", workingDefinition.acDropMin);
            workingDefinition.acDropMax = EditorGUILayout.IntField("AC Drop Max", workingDefinition.acDropMax);
            workingDefinition.randomLootCountMin = EditorGUILayout.IntField(
                "Item Count Min",
                workingDefinition.randomLootCountMin);
            workingDefinition.randomLootCountMax = EditorGUILayout.IntField(
                "Item Count Max",
                workingDefinition.randomLootCountMax);
            workingDefinition.lootRespawnDelay = EditorGUILayout.FloatField(
                "Bag Lifetime",
                workingDefinition.lootRespawnDelay);
            workingDefinition.lootInteractRange = EditorGUILayout.FloatField(
                "Interact Range",
                workingDefinition.lootInteractRange);

            EnsureWorkingSerialized();
            if (workingSerialized != null)
            {
                workingSerialized.Update();
                SerializedProperty lootPool = workingSerialized.FindProperty("lootItemPool");
                if (lootPool != null)
                    EditorGUILayout.PropertyField(lootPool, true);
                workingSerialized.ApplyModifiedProperties();
            }
        }

        private void DrawSpawnReadyStatus()
        {
            EditorGUILayout.LabelField("Spawn Ready", EditorStyles.boldLabel);
            bool hasVisual = ResolveBuildVisual() != null || workingDefinition.visualMeshSource != null;
            bool hasBrain = workingDefinition.startBrainState != null;
            bool hasSpitVfx = workingDefinition.spitVfxPrefab != null;
            bool hasId = !string.IsNullOrWhiteSpace(workingDefinition.creatureId)
                         && !string.IsNullOrWhiteSpace(workingDefinition.prefabFileName);

            workingDefinition.EnsureAnimationEntriesMigrated();
            bool hasAnim = workingDefinition.v2AnimatorController != null
                           || workingDefinition.HasAnyAnimationClip()
                           || workingDefinition.generateAnimatorFromSlots;

            DrawReadyRow("Identity", hasId);
            DrawReadyRow("Visual Mesh / Hierarchy", hasVisual);
            DrawReadyRow("Brain Profile", workingDefinition.brainProfile != null || workingDefinition.UsesLegacyMalbers);
            DrawReadyRow("Animations / Controller", hasAnim);
            if (workingDefinition.UsesLegacyMalbers)
                DrawReadyRow("Start Brain State (Legacy)", hasBrain);
            if (workingDefinition.enableRangedParticleAttack)
                DrawReadyRow("Ranged Particle VFX", hasSpitVfx);

            string prefabPath = DMICreaturePrefabBuilder.GetBuiltPrefabPath(workingDefinition);
            GameObject built = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            DrawReadyRow("Built Prefab Exists", built != null);
            EditorGUILayout.ObjectField("Built Prefab", built, typeof(GameObject), false);
        }

        private static void DrawReadyRow(string label, bool ready)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(180f));
            EditorGUILayout.LabelField(ready ? "Ready" : "Missing", ready ? EditorStyles.boldLabel : EditorStyles.helpBox);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActionButtons()
        {
            placeInSceneAfterBuild = EditorGUILayout.ToggleLeft(
                "Place In Scene After Build",
                placeInSceneAfterBuild);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Definition", GUILayout.Height(34f)))
                SaveDefinition();

            if (GUILayout.Button("Build / Rebuild Prefab", GUILayout.Height(34f)))
                BuildSelectedPrefab(placeInSceneAfterBuild);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Build + Place In Scene", GUILayout.Height(30f)))
                BuildSelectedPrefab(true);

            string prefabPath = DMICreaturePrefabBuilder.GetBuiltPrefabPath(workingDefinition);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            using (new EditorGUI.DisabledScope(existing == null))
            {
                if (GUILayout.Button("Place Existing Prefab", GUILayout.Height(30f)))
                {
                    DMICreaturePrefabBuilder.PlacePrefabInScene(
                        existing,
                        workingDefinition.displayName,
                        DMICreaturePrefabBuilder.ResolveSpawnPosition());
                    SetStatus($"Placed {workingDefinition.displayName} in scene.", MessageType.Info);
                }

                if (GUILayout.Button("Select Prefab", GUILayout.Height(30f)))
                {
                    Selection.activeObject = existing;
                    EditorGUIUtility.PingObject(existing);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(workingDefinition.startBrainState == null))
            {
                if (GUILayout.Button("Open Brain", GUILayout.Height(28f)))
                    OpenBrain(workingDefinition.startBrainState);
            }

            if (GUILayout.Button("Open Definition Asset", GUILayout.Height(28f)))
            {
                Selection.activeObject = workingDefinition;
                EditorGUIUtility.PingObject(workingDefinition);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("World Wire-Up (Phase 5)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Register In B1 Encounter Table", GUILayout.Height(28f)))
            {
                SurfaceEncounterTable table =
                    DMICreatureWorldWireUtility.EnsureB1LifeformEncounterTable(out string message);
                SetStatus(message, table != null ? MessageType.Info : MessageType.Error);
                if (table != null)
                {
                    Selection.activeObject = table;
                    EditorGUIUtility.PingObject(table);
                }
            }

            if (GUILayout.Button("Validate NavMesh + Collider", GUILayout.Height(28f)))
            {
                string builtPath = DMICreaturePrefabBuilder.GetBuiltPrefabPath(workingDefinition);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(builtPath);
                if (prefab == null)
                {
                    SetStatus("Built prefab missing — Build / Rebuild first.", MessageType.Warning);
                }
                else
                {
                    bool ok = DMICreatureWorldWireUtility.ValidateQuadrupedSetup(prefab, out string report);
                    bool spawnReady = EnemyPrefabResolver.IsSpawnReady(prefab);
                    SetStatus($"{report} | IsSpawnReady={spawnReady}", ok && spawnReady ? MessageType.Info : MessageType.Warning);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void BuildSelectedPrefab(bool placeInScene)
        {
            SaveDefinition();

            GameObject visual = ResolveBuildVisual();
            GameObject prefab = DMICreaturePrefabBuilder.BuildCreature(
                workingDefinition,
                visual,
                out string prefabPath);

            if (prefab == null)
            {
                SetStatus("Prefab build failed. Check Console.", MessageType.Error);
                return;
            }

            GameObject sceneInstance = null;
            if (placeInScene)
            {
                sceneInstance = DMICreaturePrefabBuilder.PlacePrefabInScene(
                    prefab,
                    workingDefinition.displayName,
                    DMICreaturePrefabBuilder.ResolveSpawnPosition());
            }

            if (creaturePatrolPath != null
                && workingDefinition.brainProfile != null
                && workingDefinition.brainProfile.movementMode == DMICreatureMovementMode.Patrol)
            {
                int applied = DMIPathFollowEditorUtility.ApplyToCreatures(creaturePatrolPath, sceneInstance);
                SetStatus(
                    applied > 0
                        ? $"Built creature prefab at {prefabPath}. Assigned Path Creator to {applied} target(s)."
                        : $"Built creature prefab at {prefabPath}. Path Creator set — place in scene or select a creature to apply.",
                    applied > 0 ? MessageType.Info : MessageType.Warning);
            }
            else
            {
                SetStatus($"Built creature prefab at {prefabPath}", MessageType.Info);
            }

            if (sceneInstance != null)
                Selection.activeGameObject = sceneInstance;

            RefreshDefinitions();
            SelectDefinition(workingDefinition);
        }

        private GameObject ResolveBuildVisual()
        {
            switch (workingDefinition.visualSourceMode)
            {
                case DMICreatureVisualSourceMode.SelectedHierarchyObject:
                    return hierarchyVisualSource != null
                        ? hierarchyVisualSource
                        : Selection.activeGameObject;
                case DMICreatureVisualSourceMode.ExistingPrefab:
                    return existingPrefabVisual != null
                        ? existingPrefabVisual
                        : workingDefinition.visualMeshSource;
                default:
                    return workingDefinition.visualMeshSource;
            }
        }

        private void SaveDefinition()
        {
            if (workingDefinition == null)
                return;

            if (workingDefinition.spitVfxPrefab == null && workingDefinition.enableRangedParticleAttack)
                workingDefinition.spitVfxPrefab = DMICreatureParticleCatalog.LoadPoisonSpitPrefab();

            EditorUtility.SetDirty(workingDefinition);
            AssetDatabase.SaveAssets();
            SetStatus("Definition saved.", MessageType.Info);
        }

        private void CreateDefinitionAsset()
        {
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.CreaturesData);
            DMICreatureDefinition definition = ScriptableObject.CreateInstance<DMICreatureDefinition>();
            definition.ApplyNewCreatureDefaults();
            definition.brainProfile = DMICreatureBrainProfileUtility.EnsureDefaultForNewCreature();

            string path = AssetDatabase.GenerateUniqueAssetPath(
                $"{ProjectAssetPaths.CreaturesData}/NewCreature.asset");
            AssetDatabase.CreateAsset(definition, path);
            AssetDatabase.SaveAssets();
            RefreshDefinitions();
            SelectDefinition(definition);
            SetStatus($"Created RiggedNative definition at {path}", MessageType.Info);
        }

        private void DuplicateSelectedDefinition()
        {
            if (workingDefinition == null)
                return;

            string sourcePath = AssetDatabase.GetAssetPath(workingDefinition);
            if (string.IsNullOrEmpty(sourcePath))
                return;

            string directory = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/') ?? ProjectAssetPaths.CreaturesData;
            string newPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{directory}/{workingDefinition.name}_Copy.asset");
            if (!AssetDatabase.CopyAsset(sourcePath, newPath))
            {
                SetStatus("Failed to duplicate definition.", MessageType.Error);
                return;
            }

            AssetDatabase.SaveAssets();
            RefreshDefinitions();
            DMICreatureDefinition copy = AssetDatabase.LoadAssetAtPath<DMICreatureDefinition>(newPath);
            if (copy != null)
            {
                copy.creatureId = $"{copy.creatureId}_copy";
                copy.displayName = $"{copy.displayName} Copy";
                copy.prefabFileName = $"{copy.prefabFileName}_Copy";
                EditorUtility.SetDirty(copy);
                AssetDatabase.SaveAssets();
                SelectDefinition(copy);
            }

            SetStatus($"Duplicated to {newPath}", MessageType.Info);
        }

        private void DeleteSelectedDefinition()
        {
            if (workingDefinition == null)
                return;

            string path = AssetDatabase.GetAssetPath(workingDefinition);
            if (string.IsNullOrEmpty(path))
                return;

            if (!EditorUtility.DisplayDialog(
                    "Delete Creature Definition",
                    $"Delete definition asset?\n{path}\n\n(Prefab is not deleted.)",
                    "Delete",
                    "Cancel"))
                return;

            AssetDatabase.DeleteAsset(path);
            workingDefinition = null;
            workingSerialized = null;
            selectedDefinitionIndex = -1;
            RefreshDefinitions();
            if (definitions.Length > 0)
                LoadDefinition(definitions[0], 0);
            SetStatus($"Deleted {path}", MessageType.Warning);
        }

        private void OpenBrain(MAIState state)
        {
            if (state == null)
                return;

            Selection.activeObject = state;
            EditorGUIUtility.PingObject(state);
        }

        private void RefreshDefinitions()
        {
            definitions = DMICreaturePrefabBuilder.LoadAllDefinitions();
            if (selectedDefinitionIndex >= definitions.Length)
                selectedDefinitionIndex = definitions.Length > 0 ? definitions.Length - 1 : -1;
        }

        private void LoadDefinition(DMICreatureDefinition definition, int index)
        {
            workingDefinition = definition;
            selectedDefinitionIndex = index;
            if (definition != null)
                definition.EnsureAnimationEntriesMigrated();
            workingSerialized = definition != null ? new SerializedObject(definition) : null;
            hierarchyVisualSource = null;
            existingPrefabVisual = definition != null ? definition.visualMeshSource : null;
            statusMessage = string.Empty;
        }

        private void EnsureWorkingSerialized()
        {
            if (workingDefinition == null)
            {
                workingSerialized = null;
                return;
            }

            if (workingSerialized == null || workingSerialized.targetObject != workingDefinition)
                workingSerialized = new SerializedObject(workingDefinition);
        }

        private void SelectDefinition(DMICreatureDefinition definition)
        {
            if (definition == null)
                return;

            RefreshDefinitions();
            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] == definition)
                {
                    LoadDefinition(definition, i);
                    return;
                }
            }
        }

        private void ApplyCreaturePatrolPathToSelection(GameObject extraRoot = null)
        {
            if (creaturePatrolPath == null)
            {
                SetStatus("Assign a Path Creator first.", MessageType.Warning);
                return;
            }

            int applied = DMIPathFollowEditorUtility.ApplyToCreatures(creaturePatrolPath, extraRoot);
            SetStatus(
                applied > 0
                    ? $"Assigned Path Creator to {applied} creature AI target(s)."
                    : "Select a scene creature (or persistent path + prefab asset), then apply.",
                applied > 0 ? MessageType.Info : MessageType.Warning);
        }

        private void SetStatus(string message, MessageType type)
        {
            statusMessage = message;
            statusType = type;
            Repaint();
        }
    }
}

using System.Collections.Generic;
using System.IO;
using MalbersAnimations;
using MalbersAnimations.Controller;
using MalbersAnimations.Controller.AI;
using Project.AI;
using Project.AI.Invector;
using Project.Combat;
using Project.Core;
using Project.Creatures;
using Project.Data;
using Project.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Creatures
{
    public static class DMICreaturePrefabBuilder
    {
        public const string WolfLiteAiEnemyTemplatePath =
            "Assets/Malbers Animations/Animal Controller/Wolf Lite/Wolf Lite AI Enemy.prefab";

        public const string EmptyControllerTemplatePath =
            "Assets/Malbers Animations/Animal Controller/Empty Controller/Empty Controller.prefab";

        public const string DefaultPatrolBrainStatePath =
            "Assets/Malbers Animations/Animal Controller/Wolf Lite/Brain/AC 01 Patrol.asset";

        public const string DefaultSulfurHoundMeshPath =
            "Assets/_Project/Prefabs/Environment/Lifeforms Low Level/Sulfur_Hound_01/Meshy_AI_Cragscale_Emberwyrm_quadruped/Sulfur_Hound.fbx";

        /// <summary>
        /// Widened OnWolf visual (prefab variant of Combat/Houndv3.fbx). Prefer this when present —
        /// SMR + Wolf bone order come from the prefab; root X scale carries the widen.
        /// </summary>
        public const string SulfurHoundOnWolfPrefabPath =
            "Assets/_Project/Prefabs/Combat/Houndv3.prefab";

        /// <summary>
        /// Blender OnWolf FBX fallback: Sulfur look skinned to Malbers Wolf bone names (34 bones).
        /// Prefer <see cref="SulfurHoundOnWolfPrefabPath"/> over Lifeforms static Houndv3.
        /// </summary>
        public const string SulfurHoundOnWolfFbxPath =
            "Assets/_Project/Prefabs/Combat/Houndv3.fbx";

        /// <summary>Preferred OnWolf path (prefab). Kept for existing UI / callers.</summary>
        public const string SulfurHoundOnWolfMeshPath = SulfurHoundOnWolfPrefabPath;

        public const string DefaultSulfurHoundMaterialPath =
            "Assets/_Project/Prefabs/Environment/Lifeforms Low Level/Sulfur_Hound_01/Meshy_AI_Cragscale_Emberwyrm_quadruped/Materials/Meshy_AI_Cragscale_Emberwyrm_quadruped_texture_0.mat";

        public const string SulfurHoundV2ControllerPath =
            "Assets/_Project/Animations/Creatures/SulfurHound/SulfurHound_V2.controller";

        public const string SulfurHoundV2UnlitMaterialPath =
            "Assets/_Project/Materials/Creatures/Sulfur_Hound_Body_Unlit.mat";

        public static GameObject BuildCreature(
            DMICreatureDefinition definition,
            GameObject visualOverride,
            out string prefabPath)
        {
            definition ??= ScriptableObject.CreateInstance<DMICreatureDefinition>();

            // Default + obsolete V2-A alias → rigged project-AI path (no Malbers required).
            if (!definition.UsesLegacyMalbers)
                return BuildCreatureRiggedNative(definition, visualOverride, out prefabPath);

            return BuildCreatureLegacyMalbers(definition, visualOverride, out prefabPath);
        }

        /// <summary>
        /// Legacy: Wolf Lite AC + OnWolf / AutoReskin. Used only when buildTrack = MalbersAcV1.
        /// </summary>
        public static GameObject BuildCreatureLegacyMalbers(
            DMICreatureDefinition definition,
            GameObject visualOverride,
            out string prefabPath)
        {
            EnsureProjectFolders();

            prefabPath = GetBuiltPrefabPath(definition);

            GameObject template = LoadTemplatePrefab(definition.acTemplate);
            if (template == null)
            {
                Debug.LogError("[DMICreaturePrefabBuilder] Malbers AC template prefab not found.");
                prefabPath = null;
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(template) as GameObject;
            if (instance == null)
            {
                Debug.LogError("[DMICreaturePrefabBuilder] Failed to instantiate AC template.");
                prefabPath = null;
                return null;
            }

            instance.name = definition.displayName;
            ApplyVisualBinding(instance, definition, visualOverride);
            ApplyGameplayComponents(instance, definition);

            // Unpack so we save a Regular prefab — Variant mesh overrides from Wolf Lite
            // can fail to resolve (sharedMesh null on instantiate).
            if (PrefabUtility.IsPartOfPrefabInstance(instance))
            {
                PrefabUtility.UnpackPrefabInstance(
                    instance,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Selection.activeObject = null;
            Object.DestroyImmediate(instance);

            if (definition != null)
            {
                definition.buildTrack = DMICreatureBuildTrack.MalbersAcV1;
                EditorUtility.SetDirty(definition);
                AssetDatabase.SaveAssets();
            }

            return prefab;
        }

        /// <summary>
        /// Default RiggedNative: any rigged mesh + optional anim slots + project AI. No Malbers AC.
        /// </summary>
        public static GameObject BuildCreatureRiggedNative(
            DMICreatureDefinition definition,
            GameObject visualOverride,
            out string prefabPath)
        {
            EnsureProjectFolders();
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Animations + "/Creatures");

            prefabPath = GetBuiltPrefabPath(definition);
            GameObject visualSource = visualOverride != null
                ? visualOverride
                : definition.visualMeshSource;

            if (visualSource == null)
            {
                Debug.LogError(
                    "[DMICreaturePrefabBuilder] RiggedNative visual mesh source missing. Assign an FBX/prefab.");
                prefabPath = null;
                return null;
            }

            RuntimeAnimatorController controller = ResolveOrBuildRiggedController(definition);
            if (controller == null)
            {
                Debug.LogError(
                    "[DMICreaturePrefabBuilder] No AnimatorController. Assign clips (Idle/Walk/…) " +
                    "or set generateAnimatorFromSlots / v2AnimatorController.");
                prefabPath = null;
                return null;
            }

            Material bodyMat = definition.visualMaterialSource;

            string rootName = string.IsNullOrWhiteSpace(definition.displayName)
                ? SanitizeFileName(definition.prefabFileName, "Creature")
                : definition.displayName;
            GameObject root = new GameObject(rootName);
            float scale = definition.prefabScale > 0.01f ? definition.prefabScale : 1f;
            root.transform.localScale = Vector3.one * scale;

            GameObject visual = PrefabUtility.InstantiatePrefab(visualSource) as GameObject;
            if (visual == null)
                visual = Object.Instantiate(visualSource);
            visual.name = "CreatureVisual";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            AlignVisualFeetToGround(root.transform, visual.transform);
            ApplyHeightOffset(visual.transform, definition.heightOffset);

            Animator animator = visual.GetComponent<Animator>() ?? visual.GetComponentInChildren<Animator>(true);
            if (animator == null)
                animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            ApplyRigAvatarHints(animator, definition, visualSource);

            SkinnedMeshRenderer smr = visual.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr != null)
            {
                if (bodyMat != null)
                    smr.sharedMaterial = bodyMat;
                smr.updateWhenOffscreen = true;
            }

            FitCapsuleToVisual(root, visual, definition);
            EnsureMeleeHitReception(root);

            UnityEngine.AI.NavMeshAgent agent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.height = 1.2f * scale;
            agent.radius = 0.4f * scale;
            agent.speed = 2.2f;
            agent.angularSpeed = 360f;
            agent.stoppingDistance = 0.4f * scale;
            agent.acceleration = 12f;

            EnemyHealth health = root.AddComponent<EnemyHealth>();
            SetSerializedField(health, "maxHealth", definition.maxHealth);
            SetSerializedField(health, "destroyOnDeath", definition.destroyOnDeath);
            SetSerializedField(health, "destroyDelay", definition.destroyDelay);

            DMICreatureHealth creatureHealth = root.AddComponent<DMICreatureHealth>();
            SetSerializedField(creatureHealth, "legacyHealth", health);

            DMISulfurSpitAttack spit = null;
            if (definition.enableRangedParticleAttack)
            {
                spit = root.AddComponent<DMISulfurSpitAttack>();
                if (definition.spitVfxPrefab != null)
                    spit.SetSpitVfxPrefab(definition.spitVfxPrefab);
                else
                    spit.SetSpitVfxPrefab(DMICreatureParticleCatalog.LoadPoisonSpitPrefab());
            }

            if (definition.brainProfile == null)
                definition.brainProfile = DMICreatureBrainProfileUtility.EnsureDefaultForNewCreature();

            DMICreatureBridge bridge = root.AddComponent<DMICreatureBridge>();
            DMICreatureAiController ai = root.AddComponent<DMICreatureAiController>();
            DMICreatureAnimationDriver animDriver = root.AddComponent<DMICreatureAnimationDriver>();
            DMICreatureAudioDriver audioDriver = EnsureCreatureAudio(root, definition);
            DMICreatureEmissionDriver emissionDriver = EnsureCreatureEmission(root, definition);

            SetSerializedField(bridge, "definition", definition);
            SetSerializedField(bridge, "creatureHealth", creatureHealth);
            SetSerializedField(bridge, "legacyHealth", health);
            SetSerializedField(bridge, "spitAttack", spit);
            SetSerializedField(bridge, "autoAcquireThreats", true);

            SetSerializedField(ai, "bridge", bridge);
            SetSerializedField(ai, "spitAttack", spit);
            SetSerializedField(ai, "health", health);
            SetSerializedField(ai, "agent", agent);
            SetSerializedField(ai, "animationDriver", animDriver);
            SetSerializedField(ai, "emissionDriver", emissionDriver);
            SetSerializedField(ai, "audioDriver", audioDriver);
            SetSerializedField(ai, "brainProfile", definition.brainProfile);
            SetSerializedField(ai, "meleeHitInterval", Mathf.Max(0.05f, definition.meleeAttackCooldown));
            SetSerializedField(ai, "meleeHitIntervalVariation", Mathf.Clamp(definition.meleeIntervalVariation, 0f, 10f));
            SetSerializedField(ai, "idleDurationMin", Mathf.Max(0f, definition.idleDuration));
            SetSerializedField(ai, "idleDurationMax", Mathf.Max(0f, definition.idleDuration));
            SetSerializedField(ai, "idleDurationVariation", Mathf.Clamp(definition.idleDurationVariation, 0f, 10f));
            SetSerializedField(ai, "wanderDuration", Mathf.Max(0f, definition.wanderDuration));
            SetSerializedField(ai, "wanderDurationVariation", Mathf.Clamp(definition.wanderDurationVariation, 0f, 10f));
            SetSerializedField(ai, "senseHearingEnabled", definition.senseHearingEnabled);
            SetSerializedField(ai, "hearingRange", Mathf.Max(0f, definition.hearingRange));
            SetSerializedField(ai, "hearingAggroChance", Mathf.Clamp01(definition.hearingAggroChance));
            SetSerializedField(ai, "hearingCooldown", Mathf.Max(0f, definition.hearingCooldown));
            SetSerializedField(ai, "aggroOnDamaged", definition.aggroOnDamaged);
            SetSerializedField(ai, "aggroOnHeardHit", definition.aggroOnHeardHit);
            SetSerializedField(bridge, "meleeHitCooldown", Mathf.Max(0.05f, definition.meleeAttackCooldown));
            SetSerializedField(bridge, "meleeHitIntervalVariation", Mathf.Clamp(definition.meleeIntervalVariation, 0f, 10f));
            SetSerializedField(bridge, "meleeDamage", definition.meleeDamage);
            SetSerializedField(animDriver, "animator", animator);

            if (spit != null)
                spit.ConfigureFromDefinition(definition);
            bridge.ConfigureFromDefinition(definition);
            ai.ConfigureFromDefinition(definition);
            audioDriver.ConfigureFromDefinition(definition);
            emissionDriver.ConfigureFromDefinition(definition);
            if (definition.brainProfile != null)
                emissionDriver.ConfigureAttackPulseDuration(definition.brainProfile.meleeAttackLockDuration);

            ConfigureHealthBar(root, definition);
            ConfigureLoot(root, definition);
            ConfigureDeathDissolve(root, definition);

            // Apply brain-driven agent angular speed after ConfigureFromDefinition.
            if (definition.brainProfile != null && definition.brainProfile.agentAngularSpeed > 0.01f)
                agent.angularSpeed = definition.brainProfile.agentAngularSpeed;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Selection.activeObject = null;
            Object.DestroyImmediate(root);

            definition.v2AnimatorController = controller;
            if (definition.buildTrack != DMICreatureBuildTrack.MeshyNativeV2A)
                definition.buildTrack = DMICreatureBuildTrack.RiggedNative;
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            return prefab;
        }

        /// <summary>Obsolete name kept for menu callers — routes to RiggedNative.</summary>
        public static GameObject BuildCreatureV2A(
            DMICreatureDefinition definition,
            GameObject visualOverride,
            out string prefabPath)
        {
            if (definition != null && definition.buildTrack == DMICreatureBuildTrack.MalbersAcV1)
                definition.buildTrack = DMICreatureBuildTrack.RiggedNative;
            return BuildCreatureRiggedNative(definition, visualOverride, out prefabPath);
        }

        public static DMICreatureDefinition EnsureSulfurHoundV2Definition()
        {
            const string path = "Assets/_Project/Data/Creatures/SulfurHound_V2.asset";
            DMICreatureDefinition existing = AssetDatabase.LoadAssetAtPath<DMICreatureDefinition>(path);
            if (existing != null)
            {
                if (existing.buildTrack == DMICreatureBuildTrack.MalbersAcV1)
                    existing.buildTrack = DMICreatureBuildTrack.RiggedNative;
                return existing;
            }

            DMICreatureDefinition def = ScriptableObject.CreateInstance<DMICreatureDefinition>();
            def.creatureId = "sulfur_hound_v2";
            def.displayName = "Sulfur Hound";
            def.prefabFileName = "Sulfur_Hound_V2";
            def.buildTrack = DMICreatureBuildTrack.RiggedNative;
            def.rigArchetype = DMICreatureRigArchetype.QuadrupedGeneric;
            def.generateAnimatorFromSlots = false;
            def.visualMeshSource = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultSulfurHoundMeshPath);
            def.visualMaterialSource = AssetDatabase.LoadAssetAtPath<Material>(SulfurHoundV2UnlitMaterialPath);
            def.v2AnimatorController =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SulfurHoundV2ControllerPath);
            def.spitVfxPrefab = DMICreatureParticleCatalog.LoadPoisonSpitPrefab();
            def.enableRangedParticleAttack = true;
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        private static RuntimeAnimatorController ResolveOrBuildRiggedController(DMICreatureDefinition definition)
        {
            if (definition == null)
                return null;

            definition.EnsureAnimationEntriesMigrated();
            bool hasSlots = definition.HasAnyAnimationClip();

            // When clip slots are filled, always regenerate so Creature Manager / rebuild
            // applies Idle/Walk/Run fallbacks even if generateAnimatorFromSlots was left false.
            if (hasSlots)
            {
                RuntimeAnimatorController built = DMICreatureAnimatorFactory.BuildOrUpdateController(
                    definition,
                    out string msg);
                if (built != null)
                {
                    definition.generateAnimatorFromSlots = true;
                    Debug.Log($"[DMICreaturePrefabBuilder] {msg}");
                    return built;
                }

                Debug.LogWarning($"[DMICreaturePrefabBuilder] Animator factory: {msg}");
            }

            if (definition.v2AnimatorController != null)
                return definition.v2AnimatorController;

            // Try pull clips from model then generate.
            if (definition.generateAnimatorFromSlots && definition.visualMeshSource != null)
            {
                int pulled = DMICreatureAnimatorFactory.PullClipsFromModel(
                    definition,
                    definition.visualMeshSource);
                if (pulled > 0 || DMICreatureAnimatorFactory.FindFirstClipOnModel(definition.visualMeshSource) != null)
                {
                    RuntimeAnimatorController built = DMICreatureAnimatorFactory.BuildOrUpdateController(
                        definition,
                        out string msg);
                    if (built != null)
                    {
                        Debug.Log($"[DMICreaturePrefabBuilder] Auto-pulled clips + {msg}");
                        return built;
                    }
                }
            }

            // Legacy Sulfur V2 controller fallback when mesh is the default Sulfur FBX.
            return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SulfurHoundV2ControllerPath);
        }

        private static void ApplyRigAvatarHints(
            Animator animator,
            DMICreatureDefinition definition,
            GameObject visualSource)
        {
            if (animator == null)
                return;

            Avatar avatar = null;
            if (visualSource != null)
            {
                string path = AssetDatabase.GetAssetPath(visualSource);
                if (!string.IsNullOrEmpty(path))
                {
                    Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                    for (int i = 0; i < assets.Length; i++)
                    {
                        if (assets[i] is Avatar a && a != null)
                        {
                            avatar = a;
                            break;
                        }
                    }
                }
            }

            if (avatar != null)
                animator.avatar = avatar;

            // Humanoid bipeds keep humanoid avatar; Generic/Custom leave as-is (Generic default).
            if (definition != null
                && definition.rigArchetype == DMICreatureRigArchetype.BipedHumanoid
                && avatar != null
                && avatar.isHuman)
            {
                // Avatar already humanoid — nothing else required at import time here.
            }
        }

        /// <summary>
        /// Lifts the visual so renderer bounds sit on the root XZ plane (fixes Meshy pivots underground).
        /// </summary>
        public static void AlignVisualFeetToGround(Transform root, Transform visual)
        {
            if (root == null || visual == null)
                return;

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float feetWorldY = bounds.min.y;
            float rootY = root.position.y;
            float sink = rootY - feetWorldY;
            if (Mathf.Abs(sink) < 0.001f)
                return;

            visual.localPosition += new Vector3(0f, sink, 0f);
        }

        /// <summary>
        /// Designer nudge after <see cref="AlignVisualFeetToGround"/>. Negative lowers when AABB overshoots foot bones.
        /// </summary>
        public static void ApplyHeightOffset(Transform visual, float heightOffset)
        {
            if (visual == null || Mathf.Abs(heightOffset) < 0.0001f)
                return;

            visual.localPosition += new Vector3(0f, heightOffset, 0f);
        }

        private static void FitCapsuleToVisual(
            GameObject root,
            GameObject visual,
            DMICreatureDefinition definition = null)
        {
            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            Bounds bounds = new Bounds(root.transform.position, Vector3.one);
            bool hasBounds = false;

            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                }
                else
                    bounds.Encapsulate(renderers[i].bounds);
            }

            float heightMul = definition != null
                ? Mathf.Max(0.01f, definition.meleeHitHeightMultiplier)
                : 1f;
            float radiusMul = definition != null
                ? Mathf.Max(0.01f, definition.meleeHitRadiusMultiplier)
                : 1f;
            float centerYOffset = definition != null ? definition.hitCapsuleCenterYOffset : 0f;

            if (!hasBounds)
            {
                float fallbackH = definition != null && definition.hitCapsuleHeight > 0.01f
                    ? definition.hitCapsuleHeight
                    : 1.4f;
                float fallbackR = definition != null && definition.hitCapsuleRadius > 0.01f
                    ? definition.hitCapsuleRadius
                    : 0.45f;
                fallbackH *= heightMul;
                fallbackR *= radiusMul;
                capsule.height = fallbackH;
                capsule.radius = Mathf.Min(fallbackR, fallbackH * 0.45f);
                capsule.center = new Vector3(0f, Mathf.Max(0.05f, fallbackH * 0.5f) + centerYOffset, 0f);
                return;
            }

            Vector3 localCenter = root.transform.InverseTransformPoint(bounds.center);
            float height = definition != null && definition.hitCapsuleHeight > 0.01f
                ? definition.hitCapsuleHeight
                : Mathf.Max(0.5f, bounds.size.y);
            float radius = definition != null && definition.hitCapsuleRadius > 0.01f
                ? definition.hitCapsuleRadius
                : Mathf.Max(0.15f, Mathf.Max(bounds.size.x, bounds.size.z) * 0.35f);

            height *= heightMul;
            radius *= radiusMul;

            // Keep capsule above ground plane of the root, then apply authored Y offset
            // (short creatures: raise center so player chest-height melee connects).
            localCenter.y = Mathf.Max(localCenter.y, height * 0.5f) + centerYOffset;
            capsule.center = localCenter;
            capsule.height = height;
            capsule.radius = Mathf.Min(radius, height * 0.45f);
        }

        /// <summary>
        /// Invector melee filters by tag ("Enemy") then calls Invector.vIDamageReceiver on the
        /// hit collider's GameObject. Ranged uses Project.Interaction.IDamageable / layers and does
        /// not need this — RiggedNative builds historically missed it (take bullets, ignore melee).
        /// </summary>
        public static void EnsureMeleeHitReception(GameObject root)
        {
            if (root == null)
                return;

            if (root.CompareTag("Untagged"))
                root.tag = "Enemy";

            EnemyInvectorHitSetup.EnsureRootDamageReceiver(root);
        }

        public static string GetBuiltPrefabPath(DMICreatureDefinition definition)
        {
            if (definition == null)
                return $"{ProjectAssetPaths.PrefabsCreatures}/Creature.prefab";

            string fileName = SanitizeFileName(definition.prefabFileName, definition.displayName);
            return $"{ProjectAssetPaths.PrefabsCreatures}/{fileName}.prefab";
        }

        public static GameObject PlacePrefabInScene(GameObject prefab, string instanceName, Vector3 position)
        {
            if (prefab == null)
                return null;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
                return null;

            instance.name = string.IsNullOrWhiteSpace(instanceName) ? prefab.name : instanceName;
            instance.transform.position = position;
            Undo.RegisterCreatedObjectUndo(instance, "Place Creature");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            return instance;
        }

        public static DMICreatureDefinition EnsureSulfurHoundDefinition()
        {
            EnsureProjectFolders();
            string assetPath = $"{ProjectAssetPaths.CreaturesData}/SulfurHound.asset";
            DMICreatureDefinition definition = AssetDatabase.LoadAssetAtPath<DMICreatureDefinition>(assetPath);
            bool created = definition == null;
            if (created)
            {
                definition = ScriptableObject.CreateInstance<DMICreatureDefinition>();
                definition.ApplySulfurHoundDefaults();
            }

            // Primary track: Malbers AC V1 + Blender OnWolf (Houndv3) — AutoReskin OFF.
            definition.buildTrack = DMICreatureBuildTrack.MalbersAcV1;
            definition.acTemplate = DMIAnimalControllerTemplate.WolfLiteAiEnemy;
            definition.skipAutoReskin = true;
            definition.prefabFileName = string.IsNullOrWhiteSpace(definition.prefabFileName)
                ? "Sulfur_Hound"
                : definition.prefabFileName;

            GameObject onWolfMesh = LoadSulfurHoundOnWolfVisual();
            if (onWolfMesh != null)
                definition.visualMeshSource = onWolfMesh;
            else if (definition.visualMeshSource == null)
                definition.visualMeshSource = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultSulfurHoundMeshPath);

            Material unlit = AssetDatabase.LoadAssetAtPath<Material>(SulfurHoundV2UnlitMaterialPath);
            if (unlit != null)
                definition.visualMaterialSource = unlit;
            else if (definition.visualMaterialSource == null)
            {
                definition.visualMaterialSource =
                    AssetDatabase.LoadAssetAtPath<Material>(DefaultSulfurHoundMaterialPath);
            }

            if (definition.spitVfxPrefab == null)
                definition.spitVfxPrefab = DMICreatureParticleCatalog.LoadPoisonSpitPrefab();

            MAIState patrolBrain = DMICreatureBrainAssetBuilder.EnsureSulfurHoundBrainGraph(out _);
            definition.startBrainState = patrolBrain;

            if (created)
                AssetDatabase.CreateAsset(definition, assetPath);
            else
                EditorUtility.SetDirty(definition);

            AssetDatabase.SaveAssets();
            return definition;
        }

        public static DMICreatureDefinition[] LoadAllDefinitions()
        {
            EnsureProjectFolders();
            string[] guids = AssetDatabase.FindAssets("t:DMICreatureDefinition", new[] { ProjectAssetPaths.CreaturesData });
            DMICreatureDefinition[] definitions = new DMICreatureDefinition[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                definitions[i] = AssetDatabase.LoadAssetAtPath<DMICreatureDefinition>(path);
            }

            return definitions;
        }

        private static void EnsureProjectFolders()
        {
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.CreaturesData);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.CreaturesBrainData);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.CreaturesBrainTasks);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.CreaturesBrainDecisions);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsCreatures);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsParticles);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.MaterialsCreatures);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.MeshesCreatures);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Animations + "/Creatures");
        }

        private static GameObject LoadTemplatePrefab(DMIAnimalControllerTemplate template)
        {
            string path = template == DMIAnimalControllerTemplate.EmptyController
                ? EmptyControllerTemplatePath
                : WolfLiteAiEnemyTemplatePath;

            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void ApplyVisualBinding(GameObject root, DMICreatureDefinition definition, GameObject visualOverride)
        {
            Transform meshTransform = FindChildRecursive(root.transform, "Mesh");
            SkinnedMeshRenderer wolfSmr = meshTransform != null
                ? meshTransform.GetComponent<SkinnedMeshRenderer>()
                : null;
            Transform pelvis = FindChildRecursive(root.transform, "Pelvis");

            if (wolfSmr == null || pelvis == null)
            {
                Debug.LogError(
                    "[DMICreaturePrefabBuilder] AC template missing Mesh SkinnedMeshRenderer or Pelvis.",
                    root);
                return;
            }

            GameObject visualSource = ResolveVisualSource(definition, visualOverride);
            if (visualSource == null)
            {
                Debug.LogWarning("[DMICreaturePrefabBuilder] No visual mesh source — leaving AC proxy mesh.", root);
                return;
            }

            // Strip leftover overlay/retarget from older builds.
            string visualChildName = string.IsNullOrWhiteSpace(definition.displayName)
                ? "CreatureVisual"
                : $"{SanitizeFileName(definition.prefabFileName, definition.displayName)}_Visual";
            Transform existingVisual = root.transform.Find(visualChildName);
            if (existingVisual != null)
                Object.DestroyImmediate(existingVisual.gameObject);

            DMICreatureBoneRetargeter oldRetarget = root.GetComponent<DMICreatureBoneRetargeter>();
            if (oldRetarget != null)
                Object.DestroyImmediate(oldRetarget);

            Material projectMaterial = DuplicateProjectMaterial(definition, root.name);

            // Primary path: Blender OnWolf — mesh already weighted to Wolf bone names.
            bool useOnWolf = definition == null || definition.skipAutoReskin;
            if (useOnWolf)
            {
                if (TryBindOnWolfAuthoredMesh(root, wolfSmr, meshTransform, pelvis, visualSource, projectMaterial))
                    return;

                Debug.LogWarning(
                    "[DMICreaturePrefabBuilder] OnWolf bind failed — falling back to static overlay (AutoReskin stays OFF).",
                    root);
                ApplyStaticVisualOverlayFallback(root, definition, visualSource, wolfSmr, meshTransform, projectMaterial);
                return;
            }

            // Legacy AutoReskin path (explicitly enabled only when skipAutoReskin = false).
            string safeName = SanitizeFileName(
                definition != null ? definition.prefabFileName : root.name,
                root.name);
            string meshPath = $"{ProjectAssetPaths.MeshesCreatures}/{safeName}_ACSkinned.asset";

            meshTransform.gameObject.SetActive(true);
            wolfSmr.enabled = true;

            DMICreatureAutoReskin.Result reskin = DMICreatureAutoReskin.ReskinVisualToAcTemplate(
                root,
                visualSource,
                projectMaterial,
                meshPath,
                DMICreatureAutoReskin.ReskinSettings.Default);

            if (reskin.mesh != null)
            {
                Debug.Log(
                    $"[DMICreaturePrefabBuilder] Auto-reskin applied — {reskin.message}",
                    root);
                return;
            }

            Debug.LogWarning(
                $"[DMICreaturePrefabBuilder] Auto-reskin failed ({reskin.message}) — using static visual overlay.",
                root);
            ApplyStaticVisualOverlayFallback(root, definition, visualSource, wolfSmr, meshTransform, projectMaterial);
        }

        /// <summary>
        /// Binds an authored OnWolf skinned mesh (Houndv3 / Sulfur_Hound_OnWolf) onto the AC
        /// template Mesh SMR using bone-name remap. Does not run AutoReskin.
        /// </summary>
        private static bool TryBindOnWolfAuthoredMesh(
            GameObject root,
            SkinnedMeshRenderer acMeshSmr,
            Transform meshTransform,
            Transform pelvis,
            GameObject visualSource,
            Material projectMaterial)
        {
            if (acMeshSmr == null || meshTransform == null || pelvis == null || visualSource == null)
                return false;

            // Prefer live SMR from a prefab instance so bone order + authored scale resolve.
            // Falls back to asset hierarchy (FBX / prefab contents) when instantiate fails.
            GameObject donorInstance = null;
            SkinnedMeshRenderer sourceSmr = null;
            try
            {
                donorInstance = PrefabUtility.InstantiatePrefab(visualSource) as GameObject;
                if (donorInstance == null)
                    donorInstance = Object.Instantiate(visualSource);
                if (donorInstance != null)
                {
                    donorInstance.name = "__OnWolfDonorTemp";
                    donorInstance.hideFlags = HideFlags.HideAndDontSave;
                    sourceSmr = FindPrimarySkinnedMesh(donorInstance);
                }
            }
            catch
            {
                sourceSmr = null;
            }

            if (sourceSmr == null)
                sourceSmr = FindPrimarySkinnedMesh(visualSource);

            if (sourceSmr == null || sourceSmr.sharedMesh == null)
            {
                if (donorInstance != null)
                    Object.DestroyImmediate(donorInstance);
                Debug.LogError(
                    "[DMICreaturePrefabBuilder] OnWolf visual has no SkinnedMeshRenderer/mesh. " +
                    "Use Assets/_Project/Prefabs/Combat/Houndv3.prefab (or Houndv3.fbx), not the Lifeforms static mesh.",
                    visualSource);
                return false;
            }

            Transform[] sourceBones = sourceSmr.bones;
            if (sourceBones == null || sourceBones.Length == 0)
            {
                if (donorInstance != null)
                    Object.DestroyImmediate(donorInstance);
                Debug.LogError("[DMICreaturePrefabBuilder] OnWolf visual has no bones.", visualSource);
                return false;
            }

            Transform[] remapped = new Transform[sourceBones.Length];
            int matched = 0;
            var missing = new List<string>();
            for (int i = 0; i < sourceBones.Length; i++)
            {
                string boneName = sourceBones[i] != null ? sourceBones[i].name : null;
                if (string.IsNullOrEmpty(boneName))
                {
                    missing.Add($"[{i}] null");
                    continue;
                }

                Transform acBone = FindChildRecursive(root.transform, boneName);
                if (acBone == null)
                {
                    missing.Add(boneName);
                    continue;
                }

                remapped[i] = acBone;
                matched++;
            }

            if (matched == 0)
            {
                if (donorInstance != null)
                    Object.DestroyImmediate(donorInstance);
                Debug.LogError(
                    "[DMICreaturePrefabBuilder] OnWolf bone remap matched 0 bones — aborting bind.",
                    root);
                return false;
            }

            if (missing.Count > 0)
            {
                Debug.LogWarning(
                    $"[DMICreaturePrefabBuilder] OnWolf bone remap missing {missing.Count}: {string.Join(", ", missing)}",
                    root);
            }

            meshTransform.gameObject.SetActive(true);
            acMeshSmr.enabled = true;
            Mesh boundMesh = sourceSmr.sharedMesh;
            int sourceBoneCount = sourceBones.Length;
            string sourcePath = AssetDatabase.GetAssetPath(visualSource);
            acMeshSmr.sharedMesh = boundMesh;
            acMeshSmr.bones = remapped;
            acMeshSmr.rootBone = pelvis;
            acMeshSmr.sharedMaterials = BuildMaterialArray(sourceSmr, projectMaterial);
            acMeshSmr.updateWhenOffscreen = true;

            // Houndv3.prefab widens via root X scale (variant override). Apply that to AC Mesh
            // so bind picks up the authored look without baking a new mesh asset.
            ApplyOnWolfAuthoredMeshScale(meshTransform, visualSource, donorInstance);

            if (donorInstance != null)
                Object.DestroyImmediate(donorInstance);

            Transform donorVisual = root.transform.Find("__OnWolfDonorTemp");
            if (donorVisual != null)
                Object.DestroyImmediate(donorVisual.gameObject);

            Debug.Log(
                $"[DMICreaturePrefabBuilder] OnWolf bind OK — mesh='{(boundMesh != null ? boundMesh.name : "null")}' " +
                $"bones {matched}/{sourceBoneCount} meshScale={meshTransform.localScale} " +
                $"(AutoReskin OFF, source={sourcePath}).",
                root);
            return true;
        }

        /// <summary>
        /// Loads Combat Houndv3.prefab when it has a valid SMR; otherwise Houndv3.fbx.
        /// </summary>
        public static GameObject LoadSulfurHoundOnWolfVisual()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SulfurHoundOnWolfPrefabPath);
            if (prefab != null && HasSkinnedMesh(prefab))
                return prefab;

            return AssetDatabase.LoadAssetAtPath<GameObject>(SulfurHoundOnWolfFbxPath);
        }

        private static void ApplyOnWolfAuthoredMeshScale(
            Transform meshTransform,
            GameObject visualSourceAsset,
            GameObject donorInstance)
        {
            if (meshTransform == null)
                return;

            Transform scaleRoot = donorInstance != null
                ? donorInstance.transform
                : visualSourceAsset != null
                    ? visualSourceAsset.transform
                    : null;
            if (scaleRoot == null)
                return;

            Vector3 authored = scaleRoot.localScale;
            // Ignore FBX import unit scales (often ~100). Those crash skinned-mesh / PhysX when
            // multiplied onto the AC Mesh transform. Only authored widen (e.g. Houndv3 X=1.573) applies.
            const float unitScaleReject = 10f;
            const float maxWiden = 3f;
            if (Mathf.Abs(authored.x) >= unitScaleReject ||
                Mathf.Abs(authored.y) >= unitScaleReject ||
                Mathf.Abs(authored.z) >= unitScaleReject)
            {
                Debug.LogWarning(
                    $"[DMICreaturePrefabBuilder] Ignoring OnWolf root scale {authored} (FBX unit scale). " +
                    "Use Combat/Houndv3.prefab widen, not raw FBX root scale.",
                    visualSourceAsset);
                return;
            }

            bool hasWiden =
                Mathf.Abs(authored.x - 1f) > 0.01f ||
                Mathf.Abs(authored.y - 1f) > 0.01f ||
                Mathf.Abs(authored.z - 1f) > 0.01f;
            if (!hasWiden)
                return;

            // Absolute authored scale (not multiply) so rebuilds do not compound 1.573 → 2.47 → …
            Vector3 safe = new Vector3(
                Mathf.Clamp(authored.x, 1f / maxWiden, maxWiden),
                Mathf.Clamp(authored.y, 1f / maxWiden, maxWiden),
                Mathf.Clamp(authored.z, 1f / maxWiden, maxWiden));
            meshTransform.localScale = safe;
        }

        private static void ApplyStaticVisualOverlayFallback(
            GameObject root,
            DMICreatureDefinition definition,
            GameObject visualSource,
            SkinnedMeshRenderer wolfSmr,
            Transform meshTransform,
            Material projectMaterial)
        {
            Bounds templateBounds = CaptureRendererBounds(wolfSmr.gameObject);
            wolfSmr.enabled = false;
            meshTransform.gameObject.SetActive(false);

            GameObject visualInstance = PrefabUtility.InstantiatePrefab(visualSource) as GameObject;
            if (visualInstance == null)
                visualInstance = Object.Instantiate(visualSource);

            visualInstance.name = string.IsNullOrWhiteSpace(definition.displayName)
                ? "CreatureVisual"
                : $"{SanitizeFileName(definition.prefabFileName, definition.displayName)}_Visual";
            visualInstance.transform.SetParent(root.transform, false);

            Animator[] strayAnimators = visualInstance.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < strayAnimators.Length; i++)
                Object.DestroyImmediate(strayAnimators[i]);

            SkinnedMeshRenderer sourceSmr = FindPrimarySkinnedMesh(visualInstance);
            if (sourceSmr == null)
            {
                Object.DestroyImmediate(visualInstance);
                return;
            }

            AlignVisualToTemplateBounds(visualInstance, sourceSmr, templateBounds);
            sourceSmr.sharedMaterials = BuildMaterialArray(sourceSmr, projectMaterial);
            sourceSmr.enabled = true;
        }

        private static Bounds CaptureRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
                return new Bounds(root.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].enabled)
                    bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static GameObject ResolveVisualSource(DMICreatureDefinition definition, GameObject visualOverride)
        {
            if (visualOverride != null)
                return visualOverride;

            if (definition != null && definition.visualMeshSource != null)
            {
                // Lifeforms Houndv3 is a static MeshFilter duplicate — prefer Combat OnWolf prefab/FBX.
                if (definition.skipAutoReskin && !HasSkinnedMesh(definition.visualMeshSource))
                {
                    GameObject onWolf = LoadSulfurHoundOnWolfVisual();
                    if (onWolf != null)
                        return onWolf;
                }

                return definition.visualMeshSource;
            }

            if (definition != null && definition.skipAutoReskin)
            {
                GameObject onWolf = LoadSulfurHoundOnWolfVisual();
                if (onWolf != null)
                    return onWolf;
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(DefaultSulfurHoundMeshPath);
        }

        private static bool HasSkinnedMesh(GameObject root)
        {
            return root != null && FindPrimarySkinnedMesh(root) != null;
        }

        private static SkinnedMeshRenderer FindPrimarySkinnedMesh(GameObject visualRoot)
        {
            SkinnedMeshRenderer[] renderers = visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers == null || renderers.Length == 0)
                return null;

            SkinnedMeshRenderer best = renderers[0];
            int bestBones = best.bones != null ? best.bones.Length : 0;
            for (int i = 1; i < renderers.Length; i++)
            {
                int count = renderers[i].bones != null ? renderers[i].bones.Length : 0;
                if (count > bestBones)
                {
                    best = renderers[i];
                    bestBones = count;
                }
            }

            return best;
        }

        private static void AlignVisualToTemplateBounds(
            GameObject visualInstance,
            SkinnedMeshRenderer sourceSmr,
            Bounds templateBounds)
        {
            Bounds visualBounds = sourceSmr.bounds;
            float templateHeight = Mathf.Max(templateBounds.size.y, 0.01f);
            float visualHeight = Mathf.Max(visualBounds.size.y, 0.01f);
            float scale = templateHeight / visualHeight;
            visualInstance.transform.localScale = Vector3.one * scale;

            visualBounds = sourceSmr.bounds;
            Vector3 delta = templateBounds.center - visualBounds.center;
            visualInstance.transform.position += delta;
        }

        private static Material DuplicateProjectMaterial(DMICreatureDefinition definition, string creatureName)
        {
            Material source = definition != null && definition.visualMaterialSource != null
                ? definition.visualMaterialSource
                : AssetDatabase.LoadAssetAtPath<Material>(DefaultSulfurHoundMaterialPath);

            if (source == null)
                return null;

            string safeName = SanitizeFileName(definition != null ? definition.prefabFileName : creatureName, creatureName);
            // Unlit + double-sided: AC reskin winding/tangents often break URP Lit (shadow-only mesh).
            string targetPath = $"{ProjectAssetPaths.MaterialsCreatures}/{safeName}_Body_Unlit.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(targetPath);
            if (existing != null)
            {
                ApplyAlbedoFromSource(existing, source);
                SanitizeCreatureBodyMaterial(existing);
                return existing;
            }

            Shader unlitShader = Shader.Find("HDRP/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color");
            Material duplicate = unlitShader != null
                ? new Material(unlitShader)
                : new Material(source);
            duplicate.name = $"{safeName}_Body_Unlit";
            ApplyAlbedoFromSource(duplicate, source);
            SanitizeCreatureBodyMaterial(duplicate);
            AssetDatabase.CreateAsset(duplicate, targetPath);
            return duplicate;
        }

        private static void ApplyAlbedoFromSource(Material target, Material source)
        {
            if (target == null || source == null)
                return;

            Texture albedo = null;
            if (source.HasProperty("_BaseMap"))
                albedo = source.GetTexture("_BaseMap");
            if (albedo == null && source.HasProperty("_MainTex"))
                albedo = source.GetTexture("_MainTex");

            if (albedo != null)
            {
                if (target.HasProperty("_BaseMap"))
                    target.SetTexture("_BaseMap", albedo);
                if (target.HasProperty("_MainTex"))
                    target.SetTexture("_MainTex", albedo);
            }

            if (source.HasProperty("_BaseColor") && target.HasProperty("_BaseColor"))
                target.SetColor("_BaseColor", source.GetColor("_BaseColor"));
            else if (source.HasProperty("_Color") && target.HasProperty("_BaseColor"))
                target.SetColor("_BaseColor", source.GetColor("_Color"));
        }

        /// <summary>
        /// Reskinned AC meshes often invert winding; URP Lit then draws shadow-only.
        /// Prefer Unlit double-sided with albedo for reliable visibility.
        /// </summary>
        private static void SanitizeCreatureBodyMaterial(Material mat)
        {
            if (mat == null)
                return;

            Shader unlitShader = Shader.Find("HDRP/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color");
            if (unlitShader != null && mat.shader != unlitShader)
                mat.shader = unlitShader;

            if (mat.HasProperty("_Cull"))
                mat.SetFloat("_Cull", 0f); // Double-sided
            mat.doubleSidedGI = true;

            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 0f);
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                if (c.a < 0.99f)
                {
                    c.a = 1f;
                    mat.SetColor("_BaseColor", c);
                }
            }

            EditorUtility.SetDirty(mat);
        }

        private static Material[] BuildMaterialArray(SkinnedMeshRenderer sourceSmr, Material projectMaterial)
        {
            Material[] sourceMats = sourceSmr.sharedMaterials;
            if (sourceMats == null || sourceMats.Length == 0)
                return projectMaterial != null ? new[] { projectMaterial } : System.Array.Empty<Material>();

            Material[] mats = new Material[sourceMats.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = projectMaterial != null ? projectMaterial : sourceMats[i];
            return mats;
        }

        private static void ApplyGameplayComponents(GameObject root, DMICreatureDefinition definition)
        {
            EnemyHealth health = GetOrAdd<EnemyHealth>(root);
            SetSerializedField(health, "maxHealth", definition.maxHealth);
            SetSerializedField(health, "destroyOnDeath", definition.destroyOnDeath);
            SetSerializedField(health, "destroyDelay", definition.destroyDelay);
            SetSerializedField(health, "respawnTime", 0f);
            SetSerializedField(health, "healthBarOffset", definition.healthBarOffset);

            DMICreatureHealth creatureHealth = GetOrAdd<DMICreatureHealth>(root);
            SetSerializedField(creatureHealth, "legacyHealth", health);

            DMISulfurSpitAttack spitAttack = GetOrAdd<DMISulfurSpitAttack>(root);
            SetSerializedField(spitAttack, "enableAttack", definition.enableRangedParticleAttack);
            SetSerializedField(spitAttack, "baseChance", definition.spitBaseChance);
            SetSerializedField(spitAttack, "viewBoostedChance", definition.spitViewBoostedChance);
            SetSerializedField(spitAttack, "range", definition.spitRange);
            SetSerializedField(spitAttack, "cooldown", definition.spitCooldown);
            SetSerializedField(spitAttack, "damage", definition.spitDamage);

            GameObject spitVfx = definition.spitVfxPrefab != null
                ? definition.spitVfxPrefab
                : DMICreatureParticleCatalog.LoadPoisonSpitPrefab();
            SetSerializedField(spitAttack, "spitVfxPrefab", spitVfx);

            Transform jaw = FindChildRecursive(root.transform, "Jaw");
            if (jaw != null)
                SetSerializedField(spitAttack, "muzzle", jaw);

            DMICreatureBridge bridge = GetOrAdd<DMICreatureBridge>(root);
            SetSerializedField(bridge, "definition", definition);
            SetSerializedField(bridge, "creatureHealth", creatureHealth);
            SetSerializedField(bridge, "legacyHealth", health);
            SetSerializedField(bridge, "spitAttack", spitAttack);
            SetSerializedField(bridge, "threatSenseRange", definition.threatSenseRange);
            SetSerializedField(bridge, "threatLeashMultiplier", definition.threatLeashMultiplier);
            SetSerializedField(bridge, "loseTargetDelay", definition.loseTargetDelay);
            SetSerializedField(bridge, "meleeEngageRange", definition.meleeEngageRange);
            SetSerializedField(bridge, "meleeDamage", definition.meleeDamage);

            MAnimal animal = root.GetComponent<MAnimal>() ?? root.GetComponentInChildren<MAnimal>(true);
            MAnimalBrain brain = root.GetComponent<MAnimalBrain>() ?? root.GetComponentInChildren<MAnimalBrain>(true);
            MAnimalAIControl aiControl = root.GetComponent<MAnimalAIControl>() ??
                                         root.GetComponentInChildren<MAnimalAIControl>(true);
            MDamageable damageable = root.GetComponent<MDamageable>() ??
                                     root.GetComponentInChildren<MDamageable>(true);

            SetSerializedField(bridge, "animal", animal);
            SetSerializedField(bridge, "brain", brain);
            SetSerializedField(bridge, "aiControl", aiControl);
            SetSerializedField(bridge, "damageable", damageable);

            if (brain != null && definition.startBrainState != null)
                SetSerializedField(brain, "currentState", definition.startBrainState);

            EnsureQuadrupedWorldSetup(root);

            ConfigureHealthBar(root, definition);
            ConfigureLoot(root, definition);
            ConfigureDeathDissolve(root, definition);

            EnemyProgressionXp xp = GetOrAdd<EnemyProgressionXp>(root);
            SetSerializedField(xp, "xpReward", definition.xpReward);
        }

        /// <summary>
        /// Wires existing enemy dissolve pipeline (<see cref="EnemyDisintegrationEffect"/> +
        /// <see cref="EnemyDeathSequence"/>) onto CM creatures. Default ON via definition.
        /// </summary>
        private static void ConfigureDeathDissolve(GameObject root, DMICreatureDefinition definition)
        {
            bool enable = definition == null || definition.dissolveOnDeath;
            EnemyDisintegrationEffect effect = root.GetComponent<EnemyDisintegrationEffect>();
            EnemyDeathSequence sequence = root.GetComponent<EnemyDeathSequence>();

            if (!enable)
            {
                if (effect != null)
                    Object.DestroyImmediate(effect);
                if (sequence != null)
                    Object.DestroyImmediate(sequence);
                return;
            }

            effect = GetOrAdd<EnemyDisintegrationEffect>(root);
            sequence = GetOrAdd<EnemyDeathSequence>(root);

            float preDelay = definition != null ? Mathf.Max(0f, definition.preDisintegrationDelay) : 1.25f;
            SetSerializedField(sequence, "preDisintegrationDelay", preDelay);

            // Creatures that destroy (no respawn) should not sit for the humanoid 10s post-loot delay.
            bool destroyNoRespawn = definition == null || definition.destroyOnDeath;
            SetSerializedField(sequence, "postLootRespawnDelay", destroyNoRespawn ? 0.5f : 10f);

            // Creatures typically dissolve in place (no lift). Leave enableDeathLift at default false.
            SetSerializedField(effect, "replaceDeathAnimation", true);
            SetSerializedField(effect, "autoStartOnDeathWithoutSequence", true);
        }

        private static void EnsureQuadrupedWorldSetup(GameObject root)
        {
            DMICreatureWorldWireUtility.EnsureQuadrupedCollider(root);
            EnsureMeleeHitReception(root);
            if (!DMICreatureWorldWireUtility.ValidateQuadrupedSetup(root, out string report))
                Debug.LogWarning($"[DMICreaturePrefabBuilder] Quadruped setup check: {report}", root);
            else
                Debug.Log($"[DMICreaturePrefabBuilder] Quadruped setup check: {report}", root);
        }

        private static void ConfigureHealthBar(GameObject root, DMICreatureDefinition definition)
        {
            EnemyHealthBarPresenter presenter = root.GetComponent<EnemyHealthBarPresenter>();
            if (!definition.showFloatingHealthBar)
            {
                if (presenter != null)
                    Object.DestroyImmediate(presenter);
                return;
            }

            presenter = GetOrAdd<EnemyHealthBarPresenter>(root);
            SetSerializedField(presenter, "showFloatingHealthBar", true);
            SetSerializedField(presenter, "hideUntilDamaged", definition.hideHealthBarUntilDamaged);
            SetSerializedField(presenter, "healthBarOffset", definition.healthBarOffset);
        }

        private static void ConfigureLoot(GameObject root, DMICreatureDefinition definition)
        {
            EnemyLootable lootable = GetOrAdd<EnemyLootable>(root);
            SetSerializedField(lootable, "enableLoot", definition.enableLoot);
            SetSerializedField(lootable, "lootDisplayName", definition.displayName);
            SetSerializedField(lootable, "acDropMin", definition.acDropMin);
            SetSerializedField(lootable, "acDropMax", definition.acDropMax);
            SetSerializedField(lootable, "randomLootCountMin", definition.randomLootCountMin);
            SetSerializedField(lootable, "randomLootCountMax", definition.randomLootCountMax);
            SetSerializedField(lootable, "lootItemPool", definition.lootItemPool);
            SetSerializedField(lootable, "lootUnlootedLifetime", definition.lootRespawnDelay);
            SetSerializedField(lootable, "lootInteractRange", definition.lootInteractRange);
            GameObject bagPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.EnemyLootBagPrefab);
            SetSerializedField(lootable, "lootBagPrefab", bagPrefab);
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            if (parent.name == childName)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildRecursive(parent.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static DMICreatureAudioDriver EnsureCreatureAudio(GameObject root, DMICreatureDefinition definition)
        {
            AudioSource source = root.GetComponent<AudioSource>();
            if (source == null)
                source = root.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = false;
            float minDist = definition != null ? Mathf.Max(0.1f, definition.audioMinDistance) : 2f;
            float maxDist = definition != null ? Mathf.Max(minDist + 0.1f, definition.audioMaxDistance) : 28f;
            GameplayAudioUtility.ConfigureWorldSpatialSource(source, minDist, maxDist);

            DMICreatureAudioDriver audioDriver = GetOrAdd<DMICreatureAudioDriver>(root);
            SetSerializedField(audioDriver, "audioSource", source);
            if (definition != null)
                audioDriver.ConfigureFromDefinition(definition);
            return audioDriver;
        }

        private static DMICreatureEmissionDriver EnsureCreatureEmission(GameObject root, DMICreatureDefinition definition)
        {
            DMICreatureEmissionDriver emissionDriver = GetOrAdd<DMICreatureEmissionDriver>(root);
            if (definition != null)
            {
                emissionDriver.ConfigureFromDefinition(definition);
                if (definition.brainProfile != null)
                    emissionDriver.ConfigureAttackPulseDuration(definition.brainProfile.meleeAttackLockDuration);
            }

            return emissionDriver;
        }

        private static T GetOrAdd<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            if (component == null)
                component = root.AddComponent<T>();
            return component;
        }

        private static void SetSerializedField(Object target, string propertyName, object value)
        {
            if (target == null)
                return;

            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
                return;

            switch (value)
            {
                case float floatValue:
                    property.floatValue = floatValue;
                    break;
                case int intValue:
                    property.intValue = intValue;
                    break;
                case bool boolValue:
                    property.boolValue = boolValue;
                    break;
                case string stringValue:
                    property.stringValue = stringValue;
                    break;
                case Vector3 vectorValue:
                    property.vector3Value = vectorValue;
                    break;
                case Object objectValue:
                    property.objectReferenceValue = objectValue;
                    break;
                case ItemData[] itemDataArray:
                    property.arraySize = itemDataArray.Length;
                    for (int i = 0; i < itemDataArray.Length; i++)
                        property.GetArrayElementAtIndex(i).objectReferenceValue = itemDataArray[i];
                    break;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public static string SanitizeFileName(string preferred, string fallback)
        {
            string raw = string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
            if (string.IsNullOrWhiteSpace(raw))
                raw = "Creature";

            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char character in invalid)
                raw = raw.Replace(character, '_');

            return raw.Replace(' ', '_');
        }

        public static Vector3 ResolveSpawnPosition()
        {
            if (Selection.activeTransform != null)
                return Selection.activeTransform.position;

            if (Camera.main != null)
                return Camera.main.transform.position + Camera.main.transform.forward * 8f;

            return Vector3.zero;
        }
    }
}

#if UNITY_EDITOR
using System.Collections.Generic;
using Project.Companions;
using Project.EditorTools;
using Project.EditorTools.Invector;
using Project.Pioneers;
using Project.Survival.Exposure;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Companions
{
    /// <summary>
    /// One-stop editor tool for the companion pipeline: build/refresh the shared "Echo chassis" base
    /// prefab every playable companion is spawned from, author new companion data assets (unique
    /// Echoes — synced/rescued neural imprints of Io's past given form), and keep
    /// CompanionCatalogRegistry in sync with whatever data assets exist under Data/Companions.
    /// </summary>
    public class CompanionPrefabToolWindow : EditorWindow
    {
        private static readonly string[] BakedFeatureChecklist =
        {
            "PioneerCompanionAgent (binds a SkilledPioneerRecord, resolves behavior + loadout)",
            "CompanionFollowController (formation slots, follow/hold/command state)",
            "CompanionAnimationDriver (locomotion + combat animation blending)",
            "CompanionCombatController (engage/attack, assist-alert, group combat synergy)",
            "CompanionSenseController + CompanionThreatSensor (aggro, ally-under-attack alerts)",
            "CompanionHealth + CompanionInjuryHandler (damageable, injury/recovery flow)",
            "CompanionExposureResponder (hazard-zone exposure, group + self mitigation)",
            "PioneerCompanionVisualProfile (per-record visual variation)",
            "CompanionAbilityController (data-asset buffs / active abilities)",
            "CompanionInvectorBootstrap + Loadout/Motor/Damage/Combat bridges (Invector integration)",
        };

        private Vector2 scroll;
        private CompanionOrigin newAssetOrigin = CompanionOrigin.Echo;
        private string newAssetDisplayName = string.Empty;
        private CompanionOrigin? lastSuggestedOrigin;

        [MenuItem(DarkMatterGenesisEditorMenus.CompanionPrefabTool, false, 5)]
        public static void Open()
        {
            GetWindow<CompanionPrefabToolWindow>("Companion Prefab Tool");
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            EditorGUILayout.LabelField("Companion Prefab Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Not every companion is a rescued Echo. This is the one-stop shop for all of them: " +
                "rebuild the shared PioneerCompanion_Invector chassis, seed/author companion data " +
                "assets (Echoes, Expedition/Support Ship pioneers, or unique Other-origin aliens/AI " +
                "bots), sync the catalog registry, and bake per-companion prefabs — a playable " +
                "Companion prefab (Resources/Companions) plus a world-encounter prefab (Echo signal " +
                "in Resources/Echoes, or Recruit NPC in Resources/Recruits) using the same model — " +
                "ready to drop into a scene or feed to a spawner.",
                MessageType.None);

            DrawBaseChassisSection();
            EditorGUILayout.Space(10f);
            DrawSeederSection();
            EditorGUILayout.Space(10f);
            DrawDataAssetSection();
            EditorGUILayout.Space(10f);
            DrawValidationSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSeederSection()
        {
            EditorGUILayout.LabelField("Named Pioneer Catalog Seeder", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Seeds 12 preset named Echoes as data assets under Data/Companions and syncs them " +
                "into the registry. Safe to run repeatedly — existing assets are updated, not duplicated.",
                MessageType.None);

            if (GUILayout.Button("Seed Named Pioneer Catalog (12 preset Echoes)", GUILayout.Height(24f)))
                PioneerCatalogCreator.CreateNamedPioneerCatalog();
        }

        private void DrawBaseChassisSection()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Base Echo Chassis", EditorStyles.boldLabel);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PioneerCompanionDefaults.InvectorPrefabAssetPath);
            EditorGUILayout.LabelField("Prefab", PioneerCompanionDefaults.InvectorPrefabAssetPath);
            EditorGUILayout.LabelField("Status", prefab != null ? "Found" : "Missing — build it below");

            if (GUILayout.Button("Build / Rebuild PioneerCompanion_Invector Prefab", GUILayout.Height(26f)))
                CompanionInvectorSetupUtility.BuildCompanionInvectorPrefab();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Features baked into the chassis:", EditorStyles.miniBoldLabel);
            for (int i = 0; i < BakedFeatureChecklist.Length; i++)
                EditorGUILayout.LabelField("• " + BakedFeatureChecklist[i], EditorStyles.miniLabel);
        }

        private void DrawDataAssetSection()
        {
            EditorGUILayout.LabelField("Companion Data Assets", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Folder", CompanionCatalogRegistryUtility.DataFolder);
            EditorGUILayout.HelpBox(
                "Expedition companions start with the player and are granted immediately on a new " +
                "game. Support Ship companions join later via a story/quest trigger " +
                "(PioneerRosterManager.GrantSupportShipCompanion). Other-origin unique characters " +
                "(aliens, AI bots, hybrids) are met and recruited directly out in the world via a " +
                "Recruit prefab. Neither Support Ship nor Other are auto-granted at game start.",
                MessageType.None);

            if (lastSuggestedOrigin != newAssetOrigin || string.IsNullOrWhiteSpace(newAssetDisplayName))
            {
                newAssetDisplayName = CompanionCatalogRegistryUtility.GetNextSequentialDisplayName(newAssetOrigin);
                lastSuggestedOrigin = newAssetOrigin;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("New asset origin", GUILayout.Width(100f));
            newAssetOrigin = (CompanionOrigin)EditorGUILayout.EnumPopup(newAssetOrigin, GUILayout.Width(120f));
            EditorGUILayout.LabelField("Name", GUILayout.Width(40f));
            newAssetDisplayName = EditorGUILayout.TextField(newAssetDisplayName, GUILayout.Width(150f));
            if (GUILayout.Button("Create New Companion Data Asset"))
            {
                CreateNewCompanionDataAsset(newAssetOrigin, newAssetDisplayName);
                newAssetDisplayName = CompanionCatalogRegistryUtility.GetNextSequentialDisplayName(newAssetOrigin);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(100f + 120f + 40f);
            if (newAssetOrigin == CompanionOrigin.Other && GUILayout.Button("Generate Random Alien / AI Bot Companion"))
                CreateGeneratedOtherCompanion();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Sync Registry From Data Folder"))
            {
                int added = CompanionCatalogRegistryUtility.SyncRegistryWithDataFolder();
                EditorUtility.DisplayDialog(
                    "Companion Catalog Registry",
                    added > 0
                        ? $"Added {added} new companion(s) to the registry."
                        : "Registry already up to date.",
                    "OK");
            }
            EditorGUILayout.EndHorizontal();

            List<NamedPioneerDefinition> definitions = CompanionCatalogRegistryUtility.FindAllDataAssets();

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Companion prefabs (Resources/Companions) are the playable chassis — drop one in a " +
                "scene or spawner and it self-binds to its data on Start. Echo prefabs " +
                "(Resources/Echoes) and Recruit prefabs (Resources/Recruits) are the same model, " +
                "pre-join, as a world-interactable — Echo for rescued signals, Recruit for Other-" +
                "origin aliens/AI bots/hybrids you can ask to join the colony.",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate All Companion Prefabs"))
            {
                int count = CompanionPrefabGenerator.GenerateAllCompanionPrefabs(definitions);
                EditorUtility.DisplayDialog("Companion Prefabs", $"Generated/updated {count} prefab(s) in {CompanionPrefabGenerator.CompanionsOutputFolder}.", "OK");
            }
            if (GUILayout.Button("Generate All Echo Prefabs"))
            {
                int count = CompanionPrefabGenerator.GenerateAllEchoPrefabs(definitions);
                EditorUtility.DisplayDialog("Echo Prefabs", $"Generated/updated {count} prefab(s) in {CompanionPrefabGenerator.EchoesOutputFolder}.", "OK");
            }
            if (GUILayout.Button("Generate All Recruit Prefabs"))
            {
                int count = CompanionPrefabGenerator.GenerateAllRecruitPrefabs(definitions);
                EditorUtility.DisplayDialog("Recruit Prefabs", $"Generated/updated {count} prefab(s) in {CompanionPrefabGenerator.RecruitsOutputFolder}.", "OK");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "New Companion/Echo/Recruit prefabs are always generated camera-free. Use this to " +
                "retrofit any prefabs that were baked before that fix (or from a stale chassis) — it " +
                "scans every existing prefab in Companions/Echoes/Recruits and strips any leftover " +
                "Camera, AudioListener, UniversalAdditionalCameraData, or vThirdPersonCamera.",
                MessageType.None);
            if (GUILayout.Button("Strip Cameras From All Built Companion Prefabs", GUILayout.Height(24f)))
            {
                int modified = CompanionPrefabGenerator.StripCamerasFromAllExistingPrefabs();
                EditorUtility.DisplayDialog(
                    "Camera Strip",
                    modified > 0
                        ? $"Removed camera components from {modified} prefab(s)."
                        : "No cameras found on any existing companion prefab.",
                    "OK");
            }

            EditorGUILayout.Space(4f);

            CompanionCatalogRegistry registry =
                AssetDatabase.LoadAssetAtPath<CompanionCatalogRegistry>(CompanionCatalogRegistryUtility.RegistryPath);

            if (definitions.Count == 0)
            {
                EditorGUILayout.HelpBox("No companion data assets found yet.", MessageType.Info);
                return;
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                NamedPioneerDefinition definition = definitions[i];
                bool registered = registry != null && ContainsDefinition(registry.Companions, definition);
                string safeName = CompanionPrefabGenerator.MakeSafeFileName(definition.displayName);
                bool hasCompanionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{CompanionPrefabGenerator.CompanionsOutputFolder}/{safeName}.prefab") != null;
                bool hasEchoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{CompanionPrefabGenerator.EchoesOutputFolder}/{safeName}_Echo.prefab") != null;
                bool hasRecruitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{CompanionPrefabGenerator.RecruitsOutputFolder}/{safeName}_Recruit.prefab") != null;

                bool isEchoOrigin = definition.origin == CompanionOrigin.Echo;
                bool isOtherOrigin = definition.origin == CompanionOrigin.Other;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    string.IsNullOrWhiteSpace(definition.displayName) ? "(unnamed)" : definition.displayName,
                    GUILayout.Width(180f));
                EditorGUILayout.LabelField(definition.pioneerClass.ToString(), GUILayout.Width(100f));
                EditorGUILayout.LabelField(
                    isOtherOrigin ? $"Other ({definition.nonHumanKind})" : definition.origin.ToString(),
                    GUILayout.Width(130f));
                EditorGUILayout.LabelField($"{definition.buffs?.Length ?? 0} buff(s)", GUILayout.Width(55f));
                EditorGUILayout.LabelField(registered ? "In registry" : "Not registered", GUILayout.Width(90f));
                if (GUILayout.Button("Ping", GUILayout.Width(45f)))
                {
                    Selection.activeObject = definition;
                    EditorGUIUtility.PingObject(definition);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(180f + 100f + 130f + 55f + 90f);
                if (GUILayout.Button(hasCompanionPrefab ? "Rebuild Companion Prefab" : "Generate Companion Prefab", GUILayout.Width(170f)))
                    CompanionPrefabGenerator.GenerateCompanionPrefab(definition);

                using (new EditorGUI.DisabledScope(!isEchoOrigin))
                {
                    string echoLabel = isEchoOrigin
                        ? (hasEchoPrefab ? "Rebuild Echo Prefab" : "Generate Echo Prefab")
                        : "Echo Prefab (N/A)";
                    if (GUILayout.Button(echoLabel, GUILayout.Width(150f)))
                        CompanionPrefabGenerator.GenerateEchoPrefab(definition);
                }

                using (new EditorGUI.DisabledScope(!isOtherOrigin))
                {
                    string recruitLabel = isOtherOrigin
                        ? (hasRecruitPrefab ? "Rebuild Recruit Prefab" : "Generate Recruit Prefab")
                        : "Recruit Prefab (N/A)";
                    if (GUILayout.Button(recruitLabel, GUILayout.Width(150f)))
                        CompanionPrefabGenerator.GenerateRecruitPrefab(definition);
                }

                GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
                bool deletePressed = GUILayout.Button("Delete", GUILayout.Width(60f));
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                if (deletePressed)
                {
                    string displayNameForPrompt = string.IsNullOrWhiteSpace(definition.displayName)
                        ? definition.name
                        : definition.displayName;
                    bool confirmed = EditorUtility.DisplayDialog(
                        "Delete Companion",
                        $"Delete \"{displayNameForPrompt}\"?\n\nThis removes its data asset, its registry " +
                        "entry, and any generated Companion/Echo/Recruit prefabs. This cannot be undone.",
                        "Delete",
                        "Cancel");

                    if (confirmed)
                    {
                        string summary = CompanionCatalogRegistryUtility.DeleteCompanionAndArtifacts(definition);
                        EditorUtility.DisplayDialog("Companion Deleted", summary, "OK");
                        // definition is now a destroyed Unity object reference — stop iterating this pass.
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        private void DrawValidationSection()
        {
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PioneerCompanionDefaults.InvectorPrefabAssetPath);
            List<string> issues = new List<string>();

            if (prefab == null)
            {
                issues.Add("Base chassis prefab is missing — build it above.");
            }
            else
            {
                if (prefab.GetComponentInChildren<PioneerCompanionAgent>(true) == null)
                    issues.Add("Base chassis is missing PioneerCompanionAgent.");
                if (prefab.GetComponentInChildren<CompanionExposureResponder>(true) == null)
                    issues.Add("Base chassis is missing CompanionExposureResponder — hazard mitigation won't apply until rebuilt.");
            }

            if (AssetDatabase.LoadAssetAtPath<CompanionCatalogRegistry>(CompanionCatalogRegistryUtility.RegistryPath) == null)
                issues.Add("CompanionCatalogRegistry.asset not found — Sync Registry will create it.");

            List<NamedPioneerDefinition> definitions = CompanionCatalogRegistryUtility.FindAllDataAssets();
            HashSet<string> seenNames = new HashSet<string>();
            for (int i = 0; i < definitions.Count; i++)
            {
                NamedPioneerDefinition definition = definitions[i];
                if (string.IsNullOrWhiteSpace(definition.displayName))
                {
                    issues.Add($"{definition.name} has no displayName set.");
                    continue;
                }

                if (!seenNames.Add(definition.displayName))
                    issues.Add($"Duplicate displayName \"{definition.displayName}\" — only one will be granted to the roster.");

                if (definition.origin == CompanionOrigin.Other && string.IsNullOrWhiteSpace(definition.recruitmentPitch)
                    && string.IsNullOrWhiteSpace(definition.backstory))
                    issues.Add($"\"{definition.displayName}\" (Other-origin) has no recruitmentPitch or backstory — the recruit dialogue will show \"...\".");
            }

            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("No issues found.", MessageType.Info);
                return;
            }

            for (int i = 0; i < issues.Count; i++)
                EditorGUILayout.HelpBox(issues[i], MessageType.Warning);
        }

        private static void CreateNewCompanionDataAsset(CompanionOrigin origin, string requestedDisplayName)
        {
            CompanionCatalogRegistryUtility.EnsureFolder(CompanionCatalogRegistryUtility.DataFolder);

            // "Echo 1", "Echo 2", ... — auto-numbered per origin so repeated clicks never collide on
            // displayName (see CompanionCatalogRegistryUtility.GetNextSequentialDisplayName), but the
            // user can type their own name in the field instead.
            string displayName = string.IsNullOrWhiteSpace(requestedDisplayName)
                ? CompanionCatalogRegistryUtility.GetNextSequentialDisplayName(origin)
                : requestedDisplayName.Trim();

            string path = AssetDatabase.GenerateUniqueAssetPath(
                $"{CompanionCatalogRegistryUtility.DataFolder}/{CompanionPrefabGenerator.MakeSafeFileName(displayName)}.asset");

            NamedPioneerDefinition definition = ScriptableObject.CreateInstance<NamedPioneerDefinition>();
            definition.pioneerId = System.Guid.NewGuid().ToString("N");
            definition.displayName = displayName;
            definition.origin = origin;

            AssetDatabase.CreateAsset(definition, path);
            AssetDatabase.SaveAssets();

            Selection.activeObject = definition;
            EditorGUIUtility.PingObject(definition);
        }

        /// <summary>
        /// Rolls a fully-populated Other-origin character (name, backstory, recruitment pitch,
        /// weapon/tool, a buff) via UniqueCompanionGenerator instead of leaving the designer to fill
        /// in a blank stub — every click produces a different unique alien/AI bot/hybrid.
        /// </summary>
        private static void CreateGeneratedOtherCompanion()
        {
            CompanionCatalogRegistryUtility.EnsureFolder(CompanionCatalogRegistryUtility.DataFolder);

            UniqueCompanionGenerator.GeneratedCompanion generated = UniqueCompanionGenerator.Generate();

            string path = AssetDatabase.GenerateUniqueAssetPath(
                $"{CompanionCatalogRegistryUtility.DataFolder}/{CompanionPrefabGenerator.MakeSafeFileName(generated.displayName)}.asset");

            NamedPioneerDefinition definition = ScriptableObject.CreateInstance<NamedPioneerDefinition>();
            definition.pioneerId = System.Guid.NewGuid().ToString("N");
            definition.displayName = generated.displayName;
            definition.origin = CompanionOrigin.Other;
            definition.nonHumanKind = generated.nonHumanKind;
            definition.pioneerClass = generated.pioneerClass;
            definition.radiationResistance = generated.radiationResistance;
            definition.expeditionEfficiency = generated.expeditionEfficiency;
            definition.combatSynergy = generated.combatSynergy;
            definition.backstory = generated.backstory;
            definition.recruitmentPitch = generated.recruitmentPitch;
            definition.traitIds = generated.traitIds;
            definition.passiveAbilityIds = generated.passiveAbilityIds;
            definition.learnedSkills = generated.learnedSkills;
            definition.preferredWeaponItemId = generated.preferredWeaponItemId;
            definition.preferredToolItemId = generated.preferredToolItemId;
            definition.buffs = generated.buff != null
                ? new[] { generated.buff }
                : System.Array.Empty<CompanionBuffModifier>();

            AssetDatabase.CreateAsset(definition, path);
            AssetDatabase.SaveAssets();

            Selection.activeObject = definition;
            EditorGUIUtility.PingObject(definition);
        }

        private static bool ContainsDefinition(NamedPioneerDefinition[] array, NamedPioneerDefinition value)
        {
            if (array == null)
                return false;

            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == value)
                    return true;
            }

            return false;
        }
    }
}
#endif

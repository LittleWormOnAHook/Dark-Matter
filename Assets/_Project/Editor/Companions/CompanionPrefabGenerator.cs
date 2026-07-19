#if UNITY_EDITOR
using System.Collections.Generic;
using Project.Companions;
using Project.Companions.Invector;
using Project.Echoes;
using Project.EditorTools;
using Project.Pioneers;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Companions
{
    /// <summary>
    /// Bakes a per-companion PLAYABLE prefab (Resources/Companions) and, depending on origin, a
    /// world-encounter prefab from each NamedPioneerDefinition under Data/Companions — an ECHO
    /// world-entity (Resources/Echoes) for CompanionOrigin.Echo, or a RECRUIT world-entity
    /// (Resources/Recruits) for CompanionOrigin.Other (aliens/AI bots/hybrids) — both built on top of
    /// the shared PioneerCompanion_Invector chassis so they use the same model. All are self-binding
    /// (CompanionPrefabIdentity / EchoDefinitionSeed / UniqueRecruitEntity's own definition field) so
    /// they can be dropped straight into a scene or fed to a generic spawner without going through
    /// PioneerRosterManager.
    /// </summary>
    public static class CompanionPrefabGenerator
    {
        public const string CompanionsOutputFolder = "Assets/_Project/Resources/Companions";
        public const string EchoesOutputFolder = "Assets/_Project/Resources/Echoes";
        public const string RecruitsOutputFolder = "Assets/_Project/Resources/Recruits";

        // Only CompanionExposureResponder declares a [RequireComponent(typeof(PioneerCompanionAgent))]
        // among the active-companion scripts — strip it before PioneerCompanionAgent itself, and strip
        // everything else in any order.
        private static readonly System.Type[] ActiveCompanionComponentsDependentsFirst =
        {
            typeof(Project.Survival.Exposure.CompanionExposureResponder),
            typeof(CompanionFollowController),
            typeof(CompanionAnimationDriver),
            typeof(CompanionCombatController),
            typeof(CompanionThreatSensor),
            typeof(CompanionSenseController),
            typeof(CompanionInjuryHandler),
            typeof(CompanionHealth),
            typeof(PioneerCompanionVisualProfile),
            typeof(Project.Companions.Abilities.CompanionAbilityController),
            typeof(Project.Companions.Invector.CompanionInvectorLoadoutBridge),
            typeof(Project.Companions.Invector.CompanionInvectorMotorBridge),
            typeof(Project.Companions.Invector.CompanionInvectorDamageBridge),
            typeof(Project.Companions.Invector.CompanionInvectorIncomingDamageBridge),
            typeof(Project.Companions.Invector.CompanionInvectorCombatBridge),
            typeof(Project.Companions.Invector.CompanionInvectorBootstrap),
            typeof(PioneerCompanionAgent),
        };

        /// <summary>
        /// Expedition-only scripts stripped from Echo/Recruit world encounters. Ambient locomotion
        /// (CompanionFollowController + motor bridge + CompanionWorldAmbientBehavior) is kept.
        /// </summary>
        private static readonly System.Type[] WorldEncounterComponentsToStrip =
        {
            typeof(Project.Survival.Exposure.CompanionExposureResponder),
            typeof(PioneerCompanionAgent),
            typeof(CompanionCombatController),
            typeof(CompanionThreatSensor),
            typeof(CompanionSenseController),
            typeof(CompanionInjuryHandler),
            typeof(CompanionHealth),
            typeof(Project.Companions.Abilities.CompanionAbilityController),
            typeof(Project.Companions.Invector.CompanionInvectorLoadoutBridge),
            typeof(Project.Companions.Invector.CompanionInvectorCombatBridge),
            typeof(Project.Companions.Invector.CompanionInvectorDamageBridge),
            typeof(Project.Companions.Invector.CompanionInvectorIncomingDamageBridge),
            typeof(CompanionPrefabIdentity),
        };

        public static int GenerateAllCompanionPrefabs(IReadOnlyList<NamedPioneerDefinition> definitions)
        {
            int count = 0;
            for (int i = 0; i < definitions.Count; i++)
            {
                if (GenerateCompanionPrefab(definitions[i]))
                    count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return count;
        }

        public static int GenerateAllEchoPrefabs(IReadOnlyList<NamedPioneerDefinition> definitions)
        {
            int count = 0;
            for (int i = 0; i < definitions.Count; i++)
            {
                if (GenerateEchoPrefab(definitions[i]))
                    count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return count;
        }

        /// <summary>
        /// Full active companion chassis + CompanionPrefabIdentity, renamed after the definition and
        /// saved to Resources/Companions. Drop this straight into a scene (or spawn it) and it binds
        /// itself to this companion's data on Start.
        /// </summary>
        public static bool GenerateCompanionPrefab(NamedPioneerDefinition definition)
        {
            string basePrefabPath = PioneerCompanionDefaults.InvectorPrefabAssetPath;
            if (definition == null || !System.IO.File.Exists(basePrefabPath))
                return false;

            CompanionCatalogRegistryUtility.EnsureFolder(CompanionsOutputFolder);
            string safeName = MakeSafeFileName(definition.displayName);
            string outputPath = $"{CompanionsOutputFolder}/{safeName}.prefab";

            GameObject root = PrefabUtility.LoadPrefabContents(basePrefabPath);
            try
            {
                root.name = safeName;
                StripCameraComponents(root);

                CompanionPrefabIdentity identity = root.GetComponent<CompanionPrefabIdentity>();
                if (identity == null)
                    identity = root.AddComponent<CompanionPrefabIdentity>();
                identity.SetDefinition(definition);

                PrefabUtility.SaveAsPrefabAsset(root, outputPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return true;
        }

        /// <summary>
        /// Same base chassis/model, stripped of every active-companion gameplay script and wrapped as
        /// a world-interactable EchoWorldEntity + EchoDefinitionSeed, saved to Resources/Echoes. This
        /// is the pre-rescue "spiritual remains" encounter — the same body, not yet synced/joined.
        /// </summary>
        public static bool GenerateEchoPrefab(NamedPioneerDefinition definition)
        {
            string basePrefabPath = PioneerCompanionDefaults.InvectorPrefabAssetPath;
            if (definition == null || !System.IO.File.Exists(basePrefabPath))
                return false;

            // Only Echo-origin companions are found in the world as a pre-rescue signal. Expedition
            // and Support Ship companions join the roster directly (see PioneerRosterManager), so an
            // Echo world-entity prefab for them wouldn't make narrative sense.
            if (definition.origin != CompanionOrigin.Echo)
                return false;

            CompanionCatalogRegistryUtility.EnsureFolder(EchoesOutputFolder);
            string safeName = MakeSafeFileName(definition.displayName);
            string outputPath = $"{EchoesOutputFolder}/{safeName}_Echo.prefab";

            GameObject root = PrefabUtility.LoadPrefabContents(basePrefabPath);
            try
            {
                root.name = safeName + "_Echo";
                root.tag = "Untagged";

                StripWorldEncounterComponents(root);
                StripCameraComponents(root);
                DisableRemainingBehaviours(root, typeof(EchoWorldEntity), typeof(EchoDefinitionSeed), typeof(CompanionWorldAmbientBehavior));

                SphereCollider collider = root.GetComponent<SphereCollider>();
                if (collider == null)
                    collider = root.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = 1.2f;

                EchoWorldEntity entity = root.GetComponent<EchoWorldEntity>();
                if (entity == null)
                    entity = root.AddComponent<EchoWorldEntity>();

                EchoDefinitionSeed seed = root.GetComponent<EchoDefinitionSeed>();
                if (seed == null)
                    seed = root.AddComponent<EchoDefinitionSeed>();
                seed.SetDefinition(definition);

                EnsureWorldAmbientBehavior(root, definition);

                PrefabUtility.SaveAsPrefabAsset(root, outputPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return true;
        }

        /// <summary>
        /// Per-definition Recruit prefab for a CompanionOrigin.Other unique character (alien / AI bot
        /// / hybrid) — same chassis/model as the Companion prefab, stripped of active-companion
        /// gameplay scripts and wrapped as a world-interactable UniqueRecruitEntity, saved to
        /// Resources/Recruits. Talk to them and ask them to join, no sync/rescue minigame involved.
        /// </summary>
        public static bool GenerateRecruitPrefab(NamedPioneerDefinition definition)
        {
            string basePrefabPath = PioneerCompanionDefaults.InvectorPrefabAssetPath;
            if (definition == null || !System.IO.File.Exists(basePrefabPath))
                return false;

            // Only Other-origin characters are met out in the world as a stand-alone recruit. Echo
            // has its own EchoWorldEntity flow; Expedition/Support Ship companions join the roster
            // directly and never appear as a loose world encounter.
            if (definition.origin != CompanionOrigin.Other)
                return false;

            CompanionCatalogRegistryUtility.EnsureFolder(RecruitsOutputFolder);
            string safeName = MakeSafeFileName(definition.displayName);
            string outputPath = $"{RecruitsOutputFolder}/{safeName}_Recruit.prefab";

            GameObject root = PrefabUtility.LoadPrefabContents(basePrefabPath);
            try
            {
                root.name = safeName + "_Recruit";
                root.tag = "Untagged";

                StripWorldEncounterComponents(root);
                StripCameraComponents(root);
                DisableRemainingBehaviours(root, typeof(UniqueRecruitEntity), typeof(CompanionWorldAmbientBehavior));

                SphereCollider collider = root.GetComponent<SphereCollider>();
                if (collider == null)
                    collider = root.AddComponent<SphereCollider>();
                collider.isTrigger = true;
                collider.radius = 1.2f;

                UniqueRecruitEntity entity = root.GetComponent<UniqueRecruitEntity>();
                if (entity == null)
                    entity = root.AddComponent<UniqueRecruitEntity>();
                entity.SetDefinition(definition);

                EnsureWorldAmbientBehavior(root, definition);

                PrefabUtility.SaveAsPrefabAsset(root, outputPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return true;
        }

        public static int GenerateAllRecruitPrefabs(IReadOnlyList<NamedPioneerDefinition> definitions)
        {
            int count = 0;
            for (int i = 0; i < definitions.Count; i++)
            {
                if (GenerateRecruitPrefab(definitions[i]))
                    count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return count;
        }

        /// <summary>
        /// The Echo/Recruit prefabs reuse the whole companion chassis for their visual (including
        /// Invector's own native character-controller/animator-driver scripts, which we don't strip
        /// since we don't own them and don't know their full dependency graph). Expedition-only
        /// gameplay scripts are removed; ambient follow + motor bridge stay enabled for PingPong/Idle.
        /// </summary>
        private static void DisableRemainingBehaviours(GameObject root, params System.Type[] keepTypes)
        {
            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || IsKeptType(behaviour, keepTypes))
                    continue;

                behaviour.enabled = false;
            }
        }

        private static bool IsKeptType(MonoBehaviour behaviour, System.Type[] keepTypes)
        {
            if (keepTypes == null)
                return false;

            System.Type actualType = behaviour.GetType();
            for (int i = 0; i < keepTypes.Length; i++)
            {
                if (keepTypes[i] != null && keepTypes[i].IsAssignableFrom(actualType))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Companions never render their own view — only the player's camera does. Belt-and-suspenders
        /// against a stale base chassis: CompanionInvectorSetupUtility already strips these when the
        /// chassis itself is (re)built, but this covers prefabs generated from an older chassis too.
        /// </summary>
        private static void StripCameraComponents(GameObject root)
        {
            RemoveIfPresent(root, typeof(global::Invector.vCamera.vThirdPersonCamera));
            RemoveIfPresent(root, typeof(AudioListener));
            RemoveIfPresent(root, typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData));
            RemoveIfPresent(root, typeof(Camera));
        }

        /// <summary>
        /// Retrofit pass for prefabs baked before camera-stripping existed (or from a stale base
        /// chassis) — scans every prefab under Companions/Echoes/Recruits and removes any lingering
        /// vThirdPersonCamera/AudioListener/UniversalAdditionalCameraData/Camera. Only re-saves prefabs
        /// that actually had something to remove. Returns how many were modified.
        /// </summary>
        public static int StripCamerasFromAllExistingPrefabs()
        {
            List<string> folders = new List<string> { CompanionsOutputFolder, EchoesOutputFolder, RecruitsOutputFolder };
            int modified = 0;

            for (int f = 0; f < folders.Count; f++)
            {
                string folder = folders[f];
                if (!AssetDatabase.IsValidFolder(folder))
                    continue;

                string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    GameObject root = PrefabUtility.LoadPrefabContents(path);
                    try
                    {
                        bool hadCameraStuff =
                            root.GetComponentInChildren<global::Invector.vCamera.vThirdPersonCamera>(true) != null ||
                            root.GetComponentInChildren<AudioListener>(true) != null ||
                            root.GetComponentInChildren<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(true) != null ||
                            root.GetComponentInChildren<Camera>(true) != null;

                        if (!hadCameraStuff)
                            continue;

                        StripCameraComponents(root);
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        modified++;
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }

            if (modified > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return modified;
        }

        private static void StripActiveCompanionComponents(GameObject root)
        {
            for (int i = 0; i < ActiveCompanionComponentsDependentsFirst.Length; i++)
                RemoveIfPresent(root, ActiveCompanionComponentsDependentsFirst[i]);
        }

        private static void StripWorldEncounterComponents(GameObject root)
        {
            for (int i = 0; i < WorldEncounterComponentsToStrip.Length; i++)
                RemoveIfPresent(root, WorldEncounterComponentsToStrip[i]);

            if (root.GetComponent<CompanionFollowController>() == null)
                root.AddComponent<CompanionFollowController>();

            if (root.GetComponent<CompanionInvectorMotorBridge>() == null)
                root.AddComponent<CompanionInvectorMotorBridge>();
        }

        private static void EnsureWorldAmbientBehavior(GameObject root, NamedPioneerDefinition definition)
        {
            CompanionWorldAmbientBehavior ambient = root.GetComponent<CompanionWorldAmbientBehavior>();
            if (ambient == null)
                ambient = root.AddComponent<CompanionWorldAmbientBehavior>();

            ambient.Configure(definition);
        }

        private static void RemoveIfPresent(GameObject root, System.Type componentType)
        {
            Component[] components = root.GetComponentsInChildren(componentType, true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                    Object.DestroyImmediate(components[i], true);
            }
        }

        public static string MakeSafeFileName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return "UnnamedCompanion";

            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            string result = displayName;
            for (int i = 0; i < invalid.Length; i++)
                result = result.Replace(invalid[i], '_');

            return result.Replace(" ", "");
        }

        [MenuItem(SurvivalPioneerEditorMenus.Root + "Generate All Echo Prefabs", false, 6)]
        public static void MenuGenerateAllEchoPrefabs()
        {
            List<NamedPioneerDefinition> definitions = CompanionCatalogRegistryUtility.FindAllDataAssets();
            int count = GenerateAllEchoPrefabs(definitions);
            EditorUtility.DisplayDialog(
                "Echo Prefabs",
                $"Generated/updated {count} prefab(s) in {EchoesOutputFolder}.",
                "OK");
        }

        [MenuItem(SurvivalPioneerEditorMenus.Root + "Generate All Recruit Prefabs", false, 7)]
        public static void MenuGenerateAllRecruitPrefabs()
        {
            List<NamedPioneerDefinition> definitions = CompanionCatalogRegistryUtility.FindAllDataAssets();
            int count = GenerateAllRecruitPrefabs(definitions);
            EditorUtility.DisplayDialog(
                "Recruit Prefabs",
                $"Generated/updated {count} prefab(s) in {RecruitsOutputFolder}.",
                "OK");
        }
    }
}
#endif

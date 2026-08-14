#if UNITY_EDITOR
using Project.Companions;
using Project.Companions.Abilities;
using Project.Companions.Invector;
using Project.EditorTools;
using Project.Interaction;
using Project.Inventory;
using Project.Player;
using Project.Player.Invector;
using Project.Survival.Exposure;
using Project.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.EditorTools.Invector
{
    public static class CompanionInvectorSetupUtility
    {
        private const string SourcePlayerPrefabPath = "Assets/_Project/Prefabs/Players/Player_Invector.prefab";
        private const string OutputPrefabPath = PioneerCompanionDefaults.InvectorPrefabAssetPath;
        private const string ResourcesPrefabPath = "Assets/_Project/Resources/Companions/PioneerCompanion_Invector.prefab";

        [MenuItem(DarkMatterGenesisEditorMenus.Combat + "Build PioneerCompanion_Invector Prefab", false, 130)]
        public static void BuildCompanionInvectorPrefab()
        {
            if (!System.IO.File.Exists(SourcePlayerPrefabPath))
            {
                Debug.LogError(
                    $"CompanionInvectorSetupUtility: Missing {SourcePlayerPrefabPath}. Run Build Player_Invector Prefab first.");
                return;
            }

            EnsureFolder("Assets/_Project/Prefabs/Companions");
            EnsureFolder("Assets/_Project/Resources/Companions");

            GameObject root = PrefabUtility.LoadPrefabContents(SourcePlayerPrefabPath);
            try
            {
                root.name = "PioneerCompanion_Invector";
                root.tag = "Untagged";

                PioneerInvectorPlayerSetupUtility.RefreshPreloadedWeaponSlotsOn(root);
                StripPlayerOnlyComponentsEditor(root);
                EnsureCompanionComponents(root);

                PrefabUtility.SaveAsPrefabAsset(root, OutputPrefabPath);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(ResourcesPrefabPath) != null)
                    AssetDatabase.DeleteAsset(ResourcesPrefabPath);
                AssetDatabase.CopyAsset(OutputPrefabPath, ResourcesPrefabPath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(
                    $"Created {OutputPrefabPath} and {ResourcesPrefabPath}. " +
                    "Set PioneerCompanionDefaults.UseInvectorStackPref to enable runtime spawn.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void StripPlayerOnlyComponentsEditor(GameObject root)
        {
            // Remove dependents before RequireComponent parents.
            RemoveIfPresent<PioneerInvectorInputBridge>(root);
            RemoveIfPresent<PioneerInvectorWeaponBridge>(root);
            RemoveIfPresent<PioneerInvectorDamageBridge>(root);
            RemoveIfPresent<PioneerInvectorAmmoBridge>(root);
            RemoveIfPresent<PioneerInvectorBootstrap>(root);
            RemoveIfPresent<PioneerPlayerInputBinder>(root);
            RemoveIfPresent<PioneerShooterMeleeInput>(root);
            RemoveIfPresent<global::Invector.vCharacterController.vShooterMeleeInput>(root);
            RemoveIfPresent<global::Invector.vShooter.vLockOnShooter>(root);
            RemoveIfPresent<PlayerInput>(root);
            RemoveIfPresent<RangedCombatHud>(root);
            RemoveIfPresent<MeleeCombatController>(root);
            RemoveIfPresent<RangedCombatController>(root);
            RemoveIfPresent<EquippedItemVisual>(root);
            RemoveIfPresent<EquipmentController>(root);
            RemoveIfPresent<InventorySystem>(root);
            RemoveIfPresent<WeaponAmmoState>(root);
            RemoveIfPresent<PlayerDeathHandler>(root);
            RemoveIfPresent<CombatFocusController>(root);
            RemoveIfPresent<PlayerController>(root);
            RemoveIfPresent<ResourceGatherer>(root);

            // Companions never render their own view — only the player's camera does. Player_Invector
            // carries its own camera rig (a "Camera" child with Camera/AudioListener plus Invector's
            // vThirdPersonCamera driver script); leaving those on every spawned companion means one
            // extra idle Camera and AudioListener per squadmate (Unity warns loudly about multiple
            // AudioListeners) for no benefit.
            RemoveIfPresent<global::Invector.vCamera.vThirdPersonCamera>(root);
            RemoveIfPresent<AudioListener>(root);
            RemoveIfPresent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(root);
            RemoveIfPresent<Camera>(root);
        }

        /// <summary>
        /// Full up-to-date companion feature set. PioneerCompanionAgent.Awake() self-heals most of
        /// these at runtime if missing, but baking them into the prefab keeps the asset inspectable
        /// and avoids relying purely on runtime AddComponent fallbacks — this is "the Echo chassis"
        /// every synced/rescued companion (Project.Echoes.EchoWorldEntity) is ultimately spawned on.
        /// </summary>
        private static void EnsureCompanionComponents(GameObject root)
        {
            if (root.GetComponent<PioneerCompanionAgent>() == null)
                root.AddComponent<PioneerCompanionAgent>();
            if (root.GetComponent<CompanionFollowController>() == null)
                root.AddComponent<CompanionFollowController>();
            if (root.GetComponent<CompanionAnimationDriver>() == null)
                root.AddComponent<CompanionAnimationDriver>();
            if (root.GetComponent<CompanionCombatController>() == null)
                root.AddComponent<CompanionCombatController>();
            if (root.GetComponent<CompanionSenseController>() == null)
                root.AddComponent<CompanionSenseController>();
            if (root.GetComponent<CompanionThreatSensor>() == null)
                root.AddComponent<CompanionThreatSensor>();
            if (root.GetComponent<CompanionHealth>() == null)
                root.AddComponent<CompanionHealth>();
            if (root.GetComponent<CompanionInjuryHandler>() == null)
                root.AddComponent<CompanionInjuryHandler>();
            if (root.GetComponent<CompanionExposureResponder>() == null)
                root.AddComponent<CompanionExposureResponder>();
            if (root.GetComponent<PioneerCompanionVisualProfile>() == null)
                root.AddComponent<PioneerCompanionVisualProfile>();
            if (root.GetComponent<CompanionAbilityController>() == null)
                root.AddComponent<CompanionAbilityController>();
            if (root.GetComponent<CompanionInvectorBootstrap>() == null)
                root.AddComponent<CompanionInvectorBootstrap>();
            if (root.GetComponent<CompanionInvectorLoadoutBridge>() == null)
                root.AddComponent<CompanionInvectorLoadoutBridge>();
            if (root.GetComponent<CompanionInvectorMotorBridge>() == null)
                root.AddComponent<CompanionInvectorMotorBridge>();
            if (root.GetComponent<CompanionInvectorDamageBridge>() == null)
                root.AddComponent<CompanionInvectorDamageBridge>();
            if (root.GetComponent<CompanionInvectorIncomingDamageBridge>() == null)
                root.AddComponent<CompanionInvectorIncomingDamageBridge>();
            if (root.GetComponent<CompanionInvectorCombatBridge>() == null)
                root.AddComponent<CompanionInvectorCombatBridge>();

            CapsuleCollider capsule = root.GetComponent<CapsuleCollider>();
            if (capsule != null)
                capsule.isTrigger = true;
        }

        private static void RemoveIfPresent<T>(GameObject root) where T : Component
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null)
                    Object.DestroyImmediate(components[i], true);
            }
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif

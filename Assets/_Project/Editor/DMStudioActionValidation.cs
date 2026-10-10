#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Shared validation for Genesis Studio, Building Studio, Combat Studio, and embedded creator panels.
    /// Character Creator-specific rules stay in <see cref="DMCharacterCreatorActionValidation"/>.
    /// </summary>
    public static class DMStudioActionValidation
    {
        public const string GenesisStudioDialogTitle = "Genesis Studio";
        public const string BuildingStudioDialogTitle = "Building Studio";
        public const string CombatStudioDialogTitle = "Combat Studio";

        public static void ShowValidationDialog(string title, string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            EditorUtility.DisplayDialog(title, message, "OK");
        }

        public static void ShowValidationDialog(string title, IReadOnlyList<string> blockers)
        {
            if (blockers == null || blockers.Count == 0)
                return;
            ShowValidationDialog(title, FormatBlockerList(blockers));
        }

        public static void DrawBlockersHelpBox(
            IReadOnlyList<string> blockers,
            string actionHint = "Save / Apply / Create")
        {
            if (blockers == null || blockers.Count == 0)
                return;

            string body = FormatBlockerList(blockers);
            DMCharacterCreatorSharedUi.DrawWrappedHelpBox(
                $"Fix before {actionHint}:\n" + body,
                MessageType.Warning);
        }

        public static string FormatBlockerList(IReadOnlyList<string> blockers)
        {
            if (blockers == null || blockers.Count == 0)
                return string.Empty;

            var lines = new System.Text.StringBuilder();
            for (int i = 0; i < blockers.Count; i++)
            {
                if (i > 0)
                    lines.Append('\n');
                lines.Append("• ");
                lines.Append(blockers[i]);
            }

            return lines.ToString();
        }

        public static void AddIfEmpty(List<string> blockers, string value, string label)
        {
            if (blockers == null)
                return;
            if (string.IsNullOrWhiteSpace(value))
                blockers.Add(label);
        }

        public static List<string> CollectMissingProfileAsset(string assetPath, string profileLabel)
        {
            var blockers = new List<string>(2);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                blockers.Add("Profile asset path is not configured for this subtab.");
                return blockers;
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
                return blockers;

            blockers.Add(
                $"{profileLabel} is missing at:\n{assetPath}\nCreate the asset on disk or fix the registry path.");
            return blockers;
        }

        public static bool TryRunPrimaryAction(string dialogTitle, List<string> blockers, Action action)
        {
            if (blockers != null && blockers.Count > 0)
            {
                ShowValidationDialog(dialogTitle, blockers);
                return false;
            }

            action?.Invoke();
            return true;
        }

        public static List<string> CollectItemDataCreateBlockers(string itemName)
        {
            var blockers = new List<string>(2);
            AddIfEmpty(blockers, itemName, "Item Name is empty.");
            return blockers;
        }

        public static List<string> CollectCraftingItemCreateBlockers(string itemName, string assetFileName)
        {
            var blockers = new List<string>(3);
            AddIfEmpty(blockers, itemName, "Item Name is empty.");
            string safe = CraftingEditorUtility.SanitizeAssetName(
                string.IsNullOrWhiteSpace(assetFileName) ? itemName : assetFileName);
            if (string.IsNullOrEmpty(safe))
                blockers.Add("Asset File Name could not be sanitized to a valid file name.");
            return blockers;
        }

        public static List<string> CollectBuildingStyleRebuildBlockers(Project.Building.DMBuildingStyleLibrary style)
        {
            var blockers = new List<string>(2);
            if (style == null)
            {
                blockers.Add("No building style is selected.");
                return blockers;
            }

            if (string.IsNullOrWhiteSpace(style.styleId))
                blockers.Add("Style id is empty — save the style asset first.");
            return blockers;
        }

        public static List<string> CollectBuildingFromSelectionBlockers()
        {
            var blockers = new List<string>(1);
            GameObject[] selected = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
            bool anyPrefab = false;
            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i] != null && PrefabUtility.IsPartOfPrefabAsset(selected[i]))
                {
                    anyPrefab = true;
                    break;
                }
            }

            if (!anyPrefab)
                blockers.Add("Select one or more prefab assets in the Project window, then click + From Selected Prefabs.");
            return blockers;
        }

        public static List<string> CollectBuildingHubUpgradeBlockers()
        {
            var blockers = new List<string>(2);
            if (!Application.isPlaying)
                blockers.Add("Enter Play mode to upgrade or reset Build Hubs in the live scene.");
            else if (Project.Building.DMBuildHub.Active.Count == 0)
                blockers.Add("No Build Hub instances in the scene — place a hub before using test upgrades.");
            return blockers;
        }

        public static List<string> CollectAmmoCopyBlockers(
            UnityEngine.Object targetProfile,
            UnityEngine.Object selectedProfile)
        {
            var blockers = new List<string>(2);
            if (selectedProfile == null)
                blockers.Add("Select a DMAmmoFxProfile in the Project window, then click Copy From Selected Profile.");
            else if (selectedProfile == targetProfile)
                blockers.Add("Pick a different DMAmmoFxProfile than the one being edited.");
            return blockers;
        }

        public static List<string> CollectMeleeBuildAndApplyBlockers()
        {
            var blockers = new List<string>(4);
            const string playerController =
                "Assets/_Project/Animations/Player/Invector@ShooterMelee_Jetpack.controller";
            const string weakFbx =
                "Assets/Invector-3rdPersonController/Melee Combat/3DModels/Animations/Melee_CombatSet.fbx";

            if (!File.Exists(playerController))
            {
                blockers.Add(
                    "Player melee animator controller is missing:\n" + playerController);
            }

            if (!File.Exists(weakFbx))
            {
                blockers.Add(
                    "Invector weak-attack FBX is missing (combo light A/B/C):\n" + weakFbx);
            }

            return blockers;
        }

        public static List<string> CollectBuildingCreationFxMaterialBlockers(
            Project.Building.DMBuildingCreationFxProfile fxProfile)
        {
            var blockers = new List<string>(1);
            if (fxProfile == null)
                blockers.Add("Building Creation Effects profile is missing — open the Creation Effects tab first.");
            return blockers;
        }

        public static List<string> CollectAmmoCreateBlockers(
            bool createFxProfile,
            bool registerNewType,
            string newTypeName,
            string ammoName,
            bool isHitscanBeam,
            bool createProjectilePrefab,
            GameObject existingProjectilePrefab)
        {
            var blockers = new List<string>(4);
            if (!createFxProfile)
                blockers.Add("Enable Create DMAmmoFxProfile before creating ammo.");

            AddIfEmpty(blockers, ammoName, "Ammo Name is empty.");

            if (registerNewType && string.IsNullOrWhiteSpace(newTypeName))
                blockers.Add("New Type Id is empty when Create New AmmoType is enabled.");

            if (!isHitscanBeam && !createProjectilePrefab && existingProjectilePrefab == null)
            {
                blockers.Add(
                    "Create a projectile prefab, or assign an Existing Projectile when Create Projectile Prefab is off.");
            }

            return blockers;
        }
    }
}
#endif


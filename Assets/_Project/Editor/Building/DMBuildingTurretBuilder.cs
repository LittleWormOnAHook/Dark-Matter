using System.Collections.Generic;
using FORGE3D;
using Project.Building;
using Project.EditorTools.Combat;
using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Building
{
    /// <summary>
    /// Bakes FORGE3D turret templates into Equipment prefabs and registers one Stone style part per template.
    /// </summary>
    public static class DMBuildingTurretBuilder
    {
        const string IronOrePath = "Assets/_Project/Data/Items/Resources/Mining/Iron Ore.asset";
        const string MetalScrapPath = "Assets/_Project/Data/Items/Components/metal_scrap.asset";

        [MenuItem("Tools/Dark Matter Genesis/Buildings/Register turret (current template) in Equipment", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Buildings_Add_Turret_Equipment)]
        public static void BuildMenu()
        {
            RegisterTemplate(F3DTurretEditorWindow.GetCurrentIndex());
        }

        [MenuItem("Tools/Dark Matter Genesis/Buildings/Register all FORGE3D turrets in Equipment", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Buildings_Add_Turret_Equipment + 1)]
        public static void RegisterAllTemplatesMenu()
        {
            RegisterAllTemplates();
        }

        public static void RegisterAllTemplates()
        {
            F3DTurretScriptable db = AssetDatabase.LoadAssetAtPath<F3DTurretScriptable>(
                DMTurretPrefabBakeUtility.DatabasePath);
            if (db == null || db.Turrets == null || db.Turrets.Count == 0)
            {
                Debug.LogWarning("[DM Building Library] Turret: FORGE3D database empty.");
                return;
            }

            int ok = 0;
            for (int i = 0; i < db.Turrets.Count; i++)
            {
                if (RegisterTemplate(i))
                    ok++;
            }

            DisableLegacySingleTurretPart();
            Debug.Log("[DM Building Library] Registered " + ok + " / " + db.Turrets.Count + " turret templates in Equipment.");
        }

        public static bool RegisterTemplate(int templateIndex)
        {
            ItemData ironOre = AssetDatabase.LoadAssetAtPath<ItemData>(IronOrePath);
            ItemData metalScrap = AssetDatabase.LoadAssetAtPath<ItemData>(MetalScrapPath);
            if (ironOre == null || metalScrap == null)
            {
                Debug.LogWarning("[DM Building Library] Turret: missing cost items.");
                return false;
            }

            if (!DMTurretPrefabBakeUtility.TryGetTemplateInfo(
                    templateIndex,
                    out string displayName,
                    out string slug))
            {
                return false;
            }

            DMBuildingStyleLibraryBuilder.EnsureFolder(DMTurretPrefabBakeUtility.EquipmentFolder);
            string prefabPath = DMTurretPrefabBakeUtility.EquipmentPrefabPath(templateIndex);
            string prefabObjectName = "SI_turret_" + slug;
            GameObject prefab = DMTurretPrefabBakeUtility.BakePrefabFromTemplateIndex(
                templateIndex,
                prefabPath,
                prefabObjectName);
            if (prefab == null)
                return false;

            DMBuildingStyleLibrary home = DMBuildingStyles.Find(DMBuildingStyles.DefaultStyleId);
            if (home == null)
            {
                Debug.LogWarning("[DM Building Library] Turret: default (Stone) style not found.");
                return false;
            }

            if (home.parts == null)
                home.parts = new List<DMBuildingPartEntry>();

            string stylePrefix = DMBuildingStyleLibraryBuilder.PrefixOf(home);
            string partSuffix = DMTurretPrefabBakeUtility.EquipmentPartSuffix(templateIndex);
            string id = stylePrefix + partSuffix;
            DMBuildingPartEntry part = home.FindPart(id);
            bool added = part == null;
            if (added)
            {
                part = new DMBuildingPartEntry { id = id };
                int insertAt = FindTurretInsertIndex(home.parts);
                home.parts.Insert(insertAt, part);
            }

            part.displayName = displayName.StartsWith("Turret ", System.StringComparison.OrdinalIgnoreCase)
                ? displayName
                : "Turret " + displayName;
            part.shape = DMBuildingShape.SurfaceItem;
            part.category = DMBuildingCategory.Equipment;
            part.prefab = prefab;
            part.icon = null;
            part.cost = 60;
            part.enabled = true;
            part.applyStyleFinish = false;
            part.surfaceOffsetMeters = 0f;
            part.customCost = new List<DMBuildingCostLine>
            {
                new DMBuildingCostLine { item = ironOre, amount = 25 },
                new DMBuildingCostLine { item = metalScrap, amount = 20 },
            };

            EditorUtility.SetDirty(home);
            AssetDatabase.SaveAssets();
            DMBuildingStyles.Invalidate();
            DMBuildingStyleLibraryBuilder.BakeIcons(home, false);
            Debug.Log("[DM Building Library] " + (added ? "Added" : "Updated") + " Equipment part "
                + id + " → " + prefabPath);
            return true;
        }

        static int FindTurretInsertIndex(List<DMBuildingPartEntry> parts)
        {
            int afterGenerator = parts.FindIndex(p => p != null && p.id != null && p.id.EndsWith("si_generator"));
            if (afterGenerator >= 0)
                return Mathf.Clamp(afterGenerator + 1, 0, parts.Count);

            int afterCrate = parts.FindIndex(p => p != null && p.id != null && p.id.EndsWith("si_storage_crate"));
            if (afterCrate >= 0)
                return Mathf.Clamp(afterCrate + 1, 0, parts.Count);

            return parts.Count;
        }

        static void DisableLegacySingleTurretPart()
        {
            DMBuildingStyleLibrary home = DMBuildingStyles.Find(DMBuildingStyles.DefaultStyleId);
            if (home?.parts == null)
                return;

            string legacyId = DMBuildingStyleLibraryBuilder.PrefixOf(home) + "si_turret";
            DMBuildingPartEntry legacy = home.FindPart(legacyId);
            if (legacy == null || !legacy.enabled)
                return;

            legacy.enabled = false;
            EditorUtility.SetDirty(home);
            AssetDatabase.SaveAssets();
            DMBuildingStyles.Invalidate();
            Debug.Log("[DM Building Library] Disabled legacy single-entry part " + legacyId
                + " (use per-template si_turret_* parts).");
        }
    }
}

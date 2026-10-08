using System.Collections.Generic;
using Project.Building;
using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Building
{
    /// <summary>
    /// 0926-generator: builds SI_generator from the PolygonSciFiWorlds corp generator and adds it to the Stone style as
    /// the second Equipment item (placeholder cost Iron Ore 20 + Metal Scrap 30). It powers its base (DMBaseGenerator).
    /// </summary>
    public static class DMBuildingGeneratorBuilder
    {
        const string SourcePrefabPath = "Assets/PolygonSciFiWorlds/Prefabs/Buildings/SM_Bld_Corp_Generator_01.prefab";
        const string PrefabPath = DMBuildingStorageCrateBuilder.EquipmentFolder + "/SI_generator.prefab";
        const string IronOrePath = "Assets/_Project/Data/Items/Resources/Mining/Iron Ore.asset";
        const string MetalScrapPath = "Assets/_Project/Data/Items/Components/metal_scrap.asset";

        [MenuItem("Tools/Dark Matter Genesis/Buildings/Add Generator (Equipment)", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Buildings_Add_Generator_Equipment)]
        public static void BuildMenu()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            ItemData ironOre = AssetDatabase.LoadAssetAtPath<ItemData>(IronOrePath);
            ItemData metalScrap = AssetDatabase.LoadAssetAtPath<ItemData>(MetalScrapPath);
            if (source == null || ironOre == null || metalScrap == null)
            {
                Debug.LogWarning("[DM Building Library] Generator: missing source prefab (" + (source != null) + ") or cost items (Iron Ore "
                    + (ironOre != null) + ", Metal Scrap " + (metalScrap != null) + ").");
                return;
            }

            DMBuildingStyleLibraryBuilder.EnsureFolder(DMBuildingStorageCrateBuilder.EquipmentFolder);
            GameObject prefab = BuildPrefab(source);
            if (prefab == null)
                return;

            DMBuildingStyleLibrary home = DMBuildingStyles.Find(DMBuildingStyles.DefaultStyleId);
            if (home == null)
            {
                Debug.LogWarning("[DM Building Library] Generator: default (Stone) style not found.");
                return;
            }

            if (home.parts == null)
                home.parts = new List<DMBuildingPartEntry>();
            string id = DMBuildingStyleLibraryBuilder.PrefixOf(home) + "si_generator";
            DMBuildingPartEntry part = home.FindPart(id);
            bool added = part == null;
            if (added)
            {
                part = new DMBuildingPartEntry { id = id };
                int crate = home.parts.FindIndex(p => p != null && p.id != null && p.id.EndsWith("si_storage_crate"));
                home.parts.Insert(Mathf.Clamp(crate + 1, 0, home.parts.Count), part);
            }

            part.displayName = "Generator";
            part.shape = DMBuildingShape.SurfaceItem;
            part.category = DMBuildingCategory.Equipment;
            part.prefab = prefab;
            part.icon = null;
            part.cost = 50;
            part.enabled = true;
            part.applyStyleFinish = false;
            part.surfaceOffsetMeters = 0f;
            part.customCost = new List<DMBuildingCostLine>
            {
                new DMBuildingCostLine { item = ironOre, amount = 20 },
                new DMBuildingCostLine { item = metalScrap, amount = 30 },
            };
            EditorUtility.SetDirty(home);
            AssetDatabase.SaveAssets();
            DMBuildingStyles.Invalidate();

            int queued = DMBuildingStyleLibraryBuilder.BakeIcons(home, false);
            Debug.Log("[DM Building Library] Generator: " + PrefabPath + (added ? " added to " : " refreshed in ") + home.displayName
                + " (Equipment), " + queued + " icons queued.");
        }

        static GameObject BuildPrefab(GameObject source)
        {
            var root = new GameObject("SI_generator");
            try
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source);
                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                model.name = "Model";
                model.SetActive(true);
                model.transform.SetParent(root.transform, false);
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = source.transform.localScale;
                model.transform.localPosition = Vector3.zero;

                // Sit the base on the pivot plane's centre: centre in X/Z and Y like the other surface items.
                Recenter(root, model);

                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.tag = "Untagged";
                    t.gameObject.layer = 0;
                }

                return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static void Recenter(GameObject root, GameObject model)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool any = false;
            Bounds bounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i] is ParticleSystemRenderer)
                    continue;
                if (!any)
                {
                    bounds = renderers[i].bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            if (any)
                model.transform.position -= bounds.center;
        }
    }
}

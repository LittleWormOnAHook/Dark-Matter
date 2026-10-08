using System.Collections.Generic;
using Project.Building;
using Project.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.EditorTools.Building
{
    /// <summary>
    /// 0926-build-hub: builds SI_build_hub from the DLNK Space Base Pack Terminal00 console and adds it to the Stone style
    /// as an Equipment item after the generator (placeholder cost Iron Ore 15 + Metal Scrap 25). DMBuildHub is added to the
    /// built piece at runtime (like DMBaseGenerator); this menu also creates the zone material Resources/Building/DM_BuildZone.
    /// </summary>
    public static class DMBuildingBuildHubBuilder
    {
        const string SourcePrefabPath = "Assets/_DLNK/Space Base Pack/[Prefabs]/Deco Assets/Tech/Misc/Terminal00.prefab";
        const string PrefabPath = DMBuildingStorageCrateBuilder.EquipmentFolder + "/SI_build_hub.prefab";
        const string IronOrePath = "Assets/_Project/Data/Items/Resources/Mining/Iron Ore.asset";
        const string MetalScrapPath = "Assets/_Project/Data/Items/Components/metal_scrap.asset";
        public const string ZoneMaterialPath = "Assets/_Project/Resources/Building/DM_BuildZone.mat";

        [MenuItem("Tools/Dark Matter Genesis/Buildings/Add Build Hub (Equipment)", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Buildings_Add_Build_Hub_Equipment)]
        public static void BuildMenu()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            ItemData ironOre = AssetDatabase.LoadAssetAtPath<ItemData>(IronOrePath);
            ItemData metalScrap = AssetDatabase.LoadAssetAtPath<ItemData>(MetalScrapPath);
            if (source == null || ironOre == null || metalScrap == null)
            {
                Debug.LogWarning("[DM Building Library] Build Hub: missing source prefab (" + (source != null) + ") or cost items (Iron Ore "
                    + (ironOre != null) + ", Metal Scrap " + (metalScrap != null) + ").");
                return;
            }

            EnsureZoneMaterial();

            DMBuildingStyleLibraryBuilder.EnsureFolder(DMBuildingStorageCrateBuilder.EquipmentFolder);
            GameObject prefab = BuildPrefab(source);
            if (prefab == null)
                return;

            DMBuildingStyleLibrary home = DMBuildingStyles.Find(DMBuildingStyles.DefaultStyleId);
            if (home == null)
            {
                Debug.LogWarning("[DM Building Library] Build Hub: default (Stone) style not found.");
                return;
            }

            if (home.parts == null)
                home.parts = new List<DMBuildingPartEntry>();
            string id = DMBuildingStyleLibraryBuilder.PrefixOf(home) + "si_build_hub";
            DMBuildingPartEntry part = home.FindPart(id);
            bool added = part == null;
            if (added)
            {
                part = new DMBuildingPartEntry { id = id };
                int after = home.parts.FindIndex(p => p != null && p.id != null && p.id.EndsWith("si_generator"));
                if (after < 0)
                    after = home.parts.FindIndex(p => p != null && p.id != null && p.id.EndsWith("si_storage_crate"));
                home.parts.Insert(Mathf.Clamp(after + 1, 0, home.parts.Count), part);
            }

            part.displayName = "Build Hub";
            part.shape = DMBuildingShape.SurfaceItem;
            part.category = DMBuildingCategory.Equipment;
            part.prefab = prefab;
            part.icon = null;
            part.cost = 40;
            part.enabled = true;
            part.applyStyleFinish = false;
            part.surfaceOffsetMeters = 0f;
            part.customCost = new List<DMBuildingCostLine>
            {
                new DMBuildingCostLine { item = ironOre, amount = 15 },
                new DMBuildingCostLine { item = metalScrap, amount = 25 },
            };
            EditorUtility.SetDirty(home);
            AssetDatabase.SaveAssets();
            DMBuildingStyles.Invalidate();

            int queued = DMBuildingStyleLibraryBuilder.BakeIcons(home, false);
            Debug.Log("[DM Building Library] Build Hub: " + PrefabPath + (added ? " added to " : " refreshed in ") + home.displayName
                + " (Equipment), " + queued + " icons queued.");
        }

        /// <summary>The translucent grid material DMBuildHub loads from Resources for its zone box.</summary>
        public static Material EnsureZoneMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(ZoneMaterialPath);
            if (existing != null)
                return existing;
            Shader shader = Shader.Find(DMBuildHub.ZoneShaderName);
            if (shader == null)
            {
                Debug.LogError("[DM Build Hub] Shader " + DMBuildHub.ZoneShaderName + " not found.");
                return null;
            }

            DMBuildingStyleLibraryBuilder.EnsureFolder("Assets/_Project/Resources/Building");
            var created = new Material(shader) { name = "DM_BuildZone" };
            created.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(created, ZoneMaterialPath);
            AssetDatabase.SaveAssets();
            return created;
        }

        static GameObject BuildPrefab(GameObject source)
        {
            var root = new GameObject("SI_build_hub");
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
                UnityEngine.Object.DestroyImmediate(root);
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

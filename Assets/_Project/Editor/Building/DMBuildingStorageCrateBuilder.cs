using System.Collections.Generic;
using Project.Building;
using Project.Data;
using Project.Storage;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Building
{
    /// <summary>
    /// 0926-storage-crate: builds SI_storage_crate from the Storage Crate in the open scene and adds it to every
    /// building style as a floor-only surface item costing Iron Ore 4 + Metal Scrap 10.
    /// </summary>
    public static class DMBuildingStorageCrateBuilder
    {
        /// <summary>0926-equipment: every Equipment item's prefab lives here (storage crate, generator, ...).</summary>
        public const string EquipmentFolder = DMBuildingStyleLibraryBuilder.PrefabLibraryRoot + "/Equipment";
        const string PrefabPath = EquipmentFolder + "/SI_storage_crate.prefab";
        const string IronOrePath = "Assets/_Project/Data/Items/Resources/Mining/Iron Ore.asset";
        const string MetalScrapPath = "Assets/_Project/Data/Items/Components/metal_scrap.asset";
        const string SourcePrefabPath = "Assets/_Project/Prefabs/Storage/Storage Crate.prefab";

        [MenuItem("Tools/Dark Matter Genesis/Buildings/Add Storage Crate (All Styles)", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Buildings_Add_Storage_Crate_All_Styles)]
        public static void BuildMenu()
        {
            DMStorageCrate source = FindSceneCrate();
            if (source == null)
            {
                Debug.LogWarning("[DM Building Library] Storage crate: no DMStorageCrate in the open scene to copy.");
                return;
            }

            ItemData ironOre = AssetDatabase.LoadAssetAtPath<ItemData>(IronOrePath);
            ItemData metalScrap = AssetDatabase.LoadAssetAtPath<ItemData>(MetalScrapPath);
            if (ironOre == null || metalScrap == null)
            {
                Debug.LogWarning("[DM Building Library] Storage crate: cost items missing (Iron Ore " + (ironOre != null) + ", Metal Scrap " + (metalScrap != null) + ").");
                return;
            }

            DMBuildingStyleLibraryBuilder.EnsureFolder(EquipmentFolder);
            GameObject prefab = BuildPrefab(source);
            if (prefab == null)
                return;

            // Equipment lives once, in the default (Stone) style; its Equipment category puts it on the Equipment Tab entry.
            int added = 0;
            IReadOnlyList<DMBuildingStyleLibrary> styles = DMBuildingStyles.All;
            DMBuildingStyleLibrary home = DMBuildingStyles.Find(DMBuildingStyles.DefaultStyleId);
            for (int s = 0; s < styles.Count; s++)
            {
                DMBuildingStyleLibrary style = styles[s];
                if (style == null)
                    continue;
                if (home != null && style != home)
                {
                    int removed = style.parts != null ? style.parts.RemoveAll(p => p != null && p.id != null && p.id.EndsWith("si_storage_crate")) : 0;
                    if (removed > 0)
                        EditorUtility.SetDirty(style);
                    continue;
                }
                if (style.parts == null)
                    style.parts = new List<DMBuildingPartEntry>();

                string id = DMBuildingStyleLibraryBuilder.PrefixOf(style) + "si_storage_crate";
                DMBuildingPartEntry part = style.FindPart(id);
                if (part == null)
                {
                    part = new DMBuildingPartEntry { id = id };
                    style.parts.Insert(0, part);
                    added++;
                }

                part.displayName = "Storage Crate";
                part.shape = DMBuildingShape.SurfaceItem;
                part.category = DMBuildingCategory.Equipment;
                part.prefab = prefab;
                part.icon = null; // rebake from the rebuilt prefab
                part.cost = 14;
                part.enabled = true;
                part.applyStyleFinish = false;
                part.surfaceOffsetMeters = 0f;
                part.customCost = new List<DMBuildingCostLine>
                {
                    new DMBuildingCostLine { item = ironOre, amount = 4 },
                    new DMBuildingCostLine { item = metalScrap, amount = 10 },
                };
                EditorUtility.SetDirty(style);
            }

            AssetDatabase.SaveAssets();
            DMBuildingStyles.Invalidate();

            int queued = 0;
            for (int s = 0; s < styles.Count; s++)
                queued += DMBuildingStyleLibraryBuilder.BakeIcons(styles[s], false);

            Debug.Log("[DM Building Library] Storage crate: " + PrefabPath + ", " + added + " parts added across "
                + " (Equipment, in " + (home != null ? home.displayName : "every style") + "), " + queued + " icons queued.");
        }

        static DMStorageCrate FindSceneCrate()
        {
            // Prefer the project prefab: scene copies can carry per-instance materials that save as missing (magenta icon).
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
            DMStorageCrate fromAsset = asset != null ? asset.GetComponent<DMStorageCrate>() : null;
            if (fromAsset != null)
                return fromAsset;

            DMStorageCrate[] crates = Object.FindObjectsByType<DMStorageCrate>(FindObjectsInactive.Include);
            DMStorageCrate fallback = null;
            for (int i = 0; i < crates.Length; i++)
            {
                DMStorageCrate crate = crates[i];
                if (crate == null || EditorUtility.IsPersistent(crate) || crate.GetComponentInParent<DMBuildingGhost>() != null)
                    continue;
                if (crate.gameObject.name == "Storage Crate")
                    return crate;
                if (fallback == null)
                    fallback = crate;
            }

            return fallback;
        }

        static GameObject BuildPrefab(DMStorageCrate source)
        {
            var root = new GameObject("SI_storage_crate");
            try
            {
                GameObject model = Object.Instantiate(source.gameObject);
                model.name = "Model";
                model.SetActive(true);
                model.transform.SetParent(root.transform, false);
                Vector3 euler = source.transform.eulerAngles;
                model.transform.localRotation = Quaternion.Euler(euler.x, 0f, euler.z);
                model.transform.localScale = source.transform.lossyScale;
                model.transform.localPosition = Vector3.zero;
                Recenter(root, model);

                // Face the opening side (the collection trigger) toward -Z, which faces the player when placed.
                Transform collection = model.transform.Find("collection");
                if (collection != null && collection.position.z > 0.01f)
                {
                    model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * model.transform.localRotation;
                    Recenter(root, model);
                }

                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    t.gameObject.tag = "Untagged";
                    t.gameObject.layer = 0;
                }

                DMStorageCrate crate = model.GetComponent<DMStorageCrate>();
                if (crate != null)
                {
                    var so = new SerializedObject(crate);
                    SerializedProperty idProp = so.FindProperty("crateId");
                    if (idProp != null)
                        idProp.stringValue = string.Empty;
                    SerializedProperty waitProp = so.FindProperty("assignIdOnBuild");
                    if (waitProp != null)
                        waitProp.boolValue = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                Transform particle = model.transform.Find("Particle System");
                if (particle != null)
                    particle.gameObject.SetActive(false);

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

using System.Collections.Generic;
using Project.Building;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Project.EditorTools.Building
{
    /// <summary>
    /// Builds the starter surface items (lights and decorations that stick to built pieces) as wrapper prefabs in
    /// Prefabs/Buildings/Library/SurfaceItems, then adds them to every building style and queues their icons.
    /// Wall items face +Z out of the wall; ceiling and floor items point +Y away from the face (see TrySeatSurfaceItem).
    /// </summary>
    public static class DMBuildingSurfaceItemBuilder
    {
        public const string Folder = DMBuildingStyleLibraryBuilder.PrefabLibraryRoot + "/SurfaceItems";

        struct Seed
        {
            public string Id;
            public string Name;
            public string Source;
            public bool WallMount;
            public bool KeepRotation;
            public bool Light;
            public float Lumens;
            public float Range;
            public int Cost;
        }

        static readonly Color WarmLight = new Color(1f, 0.86f, 0.68f);

        static readonly Seed[] Seeds =
        {
            new Seed { Id = "wall_light", Name = "Wall Light", Source = "Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Light_Wall_01.prefab", WallMount = true, KeepRotation = true, Light = true, Lumens = 500f, Range = 7f, Cost = 2 },
            new Seed { Id = "ceiling_light_a", Name = "Ceiling Light A", Source = "Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Light_Roof_01.prefab", Light = true, Lumens = 800f, Range = 9f, Cost = 2 },
            new Seed { Id = "ceiling_light_b", Name = "Ceiling Light B", Source = "Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Light_Roof_02.prefab", Light = true, Lumens = 800f, Range = 9f, Cost = 2 },
            new Seed { Id = "ceiling_light_c", Name = "Ceiling Light C", Source = "Assets/Synty/PolygonGeneric/Prefabs/Props/SM_Gen_Prop_Light_Roof_03.prefab", Light = true, Lumens = 800f, Range = 9f, Cost = 2 },
            new Seed { Id = "banner", Name = "Banner", Source = "Assets/PolygonSciFiWorlds/Prefabs/Props/Signs/SM_Prop_Banner_01_A.prefab", Cost = 1 },
            new Seed { Id = "poster", Name = "Poster", Source = "Assets/PolygonSciFiWorlds/Prefabs/Props/Signs/SM_Prop_Poster_01.prefab", WallMount = true, Cost = 1 },
            new Seed { Id = "sign", Name = "Sign", Source = "Assets/PolygonSciFiWorlds/Prefabs/Props/Signs/SM_Prop_Sign_01.prefab", WallMount = true, Cost = 1 },
        };

        [MenuItem("Tools/Dark Matter Genesis/Buildings/Build Surface Items (Lights, Decor)")]
        public static void BuildMenu()
        {
            DMBuildingStyleLibraryBuilder.EnsureFolder(Folder);
            var made = new List<KeyValuePair<Seed, GameObject>>();
            for (int i = 0; i < Seeds.Length; i++)
            {
                GameObject prefab = BuildPrefab(Seeds[i]);
                if (prefab != null)
                    made.Add(new KeyValuePair<Seed, GameObject>(Seeds[i], prefab));
            }

            int added = 0;
            IReadOnlyList<DMBuildingStyleLibrary> styles = DMBuildingStyles.All;
            for (int s = 0; s < styles.Count; s++)
            {
                DMBuildingStyleLibrary style = styles[s];
                if (style == null)
                    continue;
                if (style.parts == null)
                    style.parts = new List<DMBuildingPartEntry>();
                string prefix = DMBuildingStyleLibraryBuilder.PrefixOf(style);
                for (int m = 0; m < made.Count; m++)
                {
                    Seed seed = made[m].Key;
                    string id = prefix + "si_" + seed.Id;
                    DMBuildingPartEntry existing = style.FindPart(id);
                    if (existing != null)
                    {
                        if (existing.prefab == null)
                            existing.prefab = made[m].Value;
                        continue;
                    }

                    style.parts.Add(new DMBuildingPartEntry
                    {
                        id = id,
                        displayName = seed.Name,
                        shape = DMBuildingShape.SurfaceItem,
                        category = DMBuildingCategory.Decor,
                        prefab = made[m].Value,
                        cost = seed.Cost,
                        enabled = true,
                        applyStyleFinish = false,
                        surfaceOffsetMeters = 0.01f,
                    });
                    added++;
                }

                EditorUtility.SetDirty(style);
            }

            AssetDatabase.SaveAssets();
            DMBuildingStyles.Invalidate();

            int queued = 0;
            for (int s = 0; s < styles.Count; s++)
                queued += DMBuildingStyleLibraryBuilder.BakeIcons(styles[s], false);

            Debug.Log("[DM Building Library] Surface items: " + made.Count + " prefabs in " + Folder + ", " + added
                + " parts added across " + styles.Count + " styles, " + queued + " icons queued.");
        }

        static GameObject BuildPrefab(Seed seed)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(seed.Source);
            if (source == null)
            {
                Debug.LogWarning("[DM Building Library] Surface item source missing: " + seed.Source);
                return null;
            }

            string path = Folder + "/SI_" + seed.Id + ".prefab";
            var root = new GameObject("SI_" + seed.Id);
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
                model.name = "Model";
                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = Vector3.zero;

                if (!TryBounds(root, out Bounds bounds))
                    return null;

                // The source pivot sits against its mounting face, so the mesh reaches out from the pivot.
                Vector3 outDir = OutDirection(bounds, seed.WallMount, out bool vertical);
                Vector3 target = vertical ? Vector3.up : Vector3.forward;
                if (seed.KeepRotation)
                    target = outDir; // 0926: the source already faces the right way; the light still sits in front of it.
                else
                    model.transform.localRotation = Quaternion.FromToRotation(outDir, target) * model.transform.localRotation;
                TryBounds(root, out bounds);

                BoxCollider box = root.AddComponent<BoxCollider>();
                box.center = bounds.center;
                box.size = Vector3.Max(bounds.size, Vector3.one * 0.05f);

                if (seed.Light)
                {
                    Renderer carrier = model.GetComponentInChildren<Renderer>(true);
                    float reach = Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(target.x), Mathf.Abs(target.y), Mathf.Abs(target.z)));
                    var lightObject = new GameObject("Light");
                    lightObject.transform.position = bounds.center + target * (reach * 0.6f + 0.05f);
                    lightObject.transform.SetParent(carrier != null ? carrier.transform : model.transform, true);
                    lightObject.AddHDLight(LightType.Point);
                    Light light = lightObject.GetComponent<Light>();
                    light.color = WarmLight;
                    light.range = seed.Range;
                    light.shadows = LightShadows.None;
                    light.intensity = LightUnitUtils.ConvertIntensity(light, seed.Lumens, LightUnit.Lumen, LightUnitUtils.GetNativeLightUnit(light.type));
                    light.lightUnit = LightUnit.Lumen;
                }

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
                return saved;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static Vector3 OutDirection(Bounds bounds, bool wallMount, out bool vertical)
        {
            Vector3 c = bounds.center;
            Vector3 e = Vector3.Max(bounds.extents, Vector3.one * 0.005f);
            float rx = Mathf.Abs(c.x) / e.x;
            float ry = Mathf.Abs(c.y) / e.y;
            float rz = Mathf.Abs(c.z) / e.z;

            if (wallMount)
            {
                vertical = false;
                // Flat wall pieces (posters, signs): the thin horizontal axis faces out of the wall.
                bool xThin = e.x < e.z;
                float along = xThin ? c.x : c.z;
                float ratio = xThin ? rx : rz;
                float sign = ratio > 0.25f ? Mathf.Sign(along) : 1f;
                return xThin ? new Vector3(sign, 0f, 0f) : new Vector3(0f, 0f, sign);
            }

            if (ry >= rx && ry >= rz && ry > 0.25f)
            {
                vertical = true;
                return new Vector3(0f, Mathf.Sign(c.y), 0f);
            }

            vertical = false;
            if (rx > 0.25f || rz > 0.25f)
                return rx >= rz ? new Vector3(Mathf.Sign(c.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(c.z));
            vertical = true;
            return Vector3.up;
        }

        static bool TryBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool any = false;
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

            return any;
        }
    }
}

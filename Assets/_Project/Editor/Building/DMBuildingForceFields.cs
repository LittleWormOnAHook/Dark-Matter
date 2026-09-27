using System.Collections.Generic;
using System.IO;
using System.Text;
using Project.Building;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.EditorTools.Building
{
    /// <summary>
    /// 0926-force-fields: adds four plane force field pieces to every style's roster (door, double door, 8 m gate, hatch).
    /// They seat in the same frames as the regular doors, which stay as swinging doors.
    /// Runs after each kit rebuild and from the Buildings menu. Look, sound and behaviour live in Building Studio > Creation Effects.
    /// </summary>
    public static class DMBuildingForceFields
    {
        const string MenuPath = "Tools/Dark Matter Genesis/Buildings/Add Force Field Pieces (All Styles)";
        public const string MaterialPath = "Assets/_Project/Resources/Building/DM_ForceField.mat";
        public const string ShaderName = "Project/DMForceField";
        public const string CornerMaterialPath = "Assets/_Project/Resources/Building/DM_ForceFieldCorner.mat";
        const string MeshFolder = DMBuildingStyleLibraryBuilder.PrefabLibraryRoot + "/ForceFields";
        const float FrameOverlap = 0.04f;
        const float MinColliderDepth = 0.15f;

        /// <summary>Force field suffix and the regular door it sits next to in the roster.</summary>
        static readonly string[,] Pairs =
        {
            { DMBuildingCatalog.ForceFieldPrefix + "door", "door_basic" },
            { DMBuildingCatalog.ForceFieldPrefix + "door_double", "door_double" },
            { DMBuildingCatalog.ForceFieldPrefix + "gate_8x8", "gate_8x8" },
            { DMBuildingCatalog.ForceFieldPrefix + "hatch_lid", "hatch_lid" },
        };

        [MenuItem(MenuPath, priority = 213)]
        public static void ApplyAllMenu()
        {
            int total = 0;
            string[] guids = AssetDatabase.FindAssets("t:DMBuildingStyleLibrary", new[] { DMBuildingStyleLibrary.AssetFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                var style = AssetDatabase.LoadAssetAtPath<DMBuildingStyleLibrary>(AssetDatabase.GUIDToAssetPath(guids[i]));
                total += ApplyToStyle(style, bakeIcons: true);
            }

            DMBuildingStyles.Invalidate();
            Debug.Log("[DM Force Fields] Added or refreshed " + total + " force field prefabs across " + guids.Length + " styles.");
        }

        public static Material EnsureMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null)
                return existing;
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError("[DM Force Fields] Shader " + ShaderName + " not found.");
                return null;
            }

            var created = new Material(shader) { name = "DM_ForceField" };
            created.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(created, MaterialPath);
            AssetDatabase.SaveAssets();
            return created;
        }

        /// <summary>0927-ff-corners: the editable dark metal material for the corner blocks (HDRP/Lit), created once.</summary>
        public static Material EnsureCornerMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(CornerMaterialPath);
            if (existing != null)
                return existing;
            Material created = DMForceField.CreateDefaultCornerMaterial();
            if (created == null)
            {
                Debug.LogError("[DM Force Fields] HDRP/Lit not found; corner block material not created.");
                return null;
            }

            created.hideFlags = HideFlags.None;
            created.name = "DM_ForceFieldCorner";
            AssetDatabase.CreateAsset(created, CornerMaterialPath);
            AssetDatabase.SaveAssets();
            return created;
        }

        /// <summary>Adds or refreshes the force field parts of one style. Returns the number of prefabs created or changed.</summary>
        public static int ApplyToStyle(DMBuildingStyleLibrary style, bool bakeIcons)
        {
            if (style == null)
                return 0;
            if (style.parts == null)
                style.parts = new List<DMBuildingPartEntry>();
            Material material = EnsureMaterial();
            if (material == null)
                return 0;

            var profile = AssetDatabase.LoadAssetAtPath<DMBuildingCreationFxProfile>(DMBuildingCreationFxProfile.AssetPath);
            if (profile != null && profile.forceFieldMaterial == null)
            {
                profile.forceFieldMaterial = material;
                EditorUtility.SetDirty(profile);
            }

            Material used = profile != null && profile.forceFieldMaterial != null ? profile.forceFieldMaterial : material;
            string prefix = DMBuildingStyleLibraryBuilder.PrefixOf(style);
            string folder = DMBuildingStyleLibraryBuilder.StyleRoot(style) + "/ForceFields";
            int layer = LayerOf(style, prefix);
            var log = new StringBuilder();
            var changed = new List<DMBuildingPartEntry>();

            for (int p = 0; p < Pairs.GetLength(0); p++)
            {
                if (!TryFindTemplate(Pairs[p, 0], out DMBuildingCatalog.KitPartTemplate template))
                    continue;
                string id = prefix + template.Suffix;

                DMBuildingPartEntry part = style.FindPart(id);
                if (part == null)
                {
                    part = DMBuildingStyleLibraryBuilder.NewKitPart(id, template);
                    int after = IndexOf(style, prefix + Pairs[p, 1]);
                    if (after >= 0)
                        style.parts.Insert(after + 1, part);
                    else
                        style.parts.Add(part);
                    log.Append(" +").Append(id).Append(";");
                }

                part.shape = template.Shape;
                part.category = DMBuildingCatalog.DefaultCategory(template.Shape);
                part.applyStyleFinish = false;
                if (string.IsNullOrEmpty(part.displayName) || part.displayName == part.id)
                    part.displayName = template.DisplayName;

                bool created = false;
                if (part.prefab == null)
                {
                    string path = folder + "/" + id + ".prefab";
                    part.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (part.prefab == null)
                    {
                        EnsureFolder(folder);
                        var go = new GameObject(id);
                        go.layer = layer;
                        part.prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
                        Object.DestroyImmediate(go);
                        created = true;
                    }
                }

                if (part.prefab == null)
                {
                    log.Append(" ").Append(id).Append("=prefab failed;");
                    continue;
                }

                if (RefreshPrefab(part, used, layer) || created)
                {
                    part.icon = null;
                    changed.Add(part);
                }
            }

            EditorUtility.SetDirty(style);
            AssetDatabase.SaveAssets();
            DMBuildingStyles.Invalidate();

            if (changed.Count > 0 && bakeIcons)
            {
                DMBuildingStyleLibrary captured = style;
                EditorApplication.delayCall += () => DMBuildingStyleLibraryBuilder.BakeIcons(captured, overwrite: false);
            }

            Debug.Log("[DM Force Fields] " + style.displayName + ": " + changed.Count + " force field prefabs made or refreshed." + log);
            return changed.Count;
        }

        /// <summary>Plane mesh, field material, solid box collider, no children. Returns true when anything changed.</summary>
        static bool RefreshPrefab(DMBuildingPartEntry part, Material material, int layer)
        {
            string path = AssetDatabase.GetAssetPath(part.prefab);
            Mesh plane = EnsurePlane(part.shape);
            Vector3 size = DMBuildingCatalog.SizeFor(part.shape, null, Vector3.zero);
            Vector3 colliderSize = part.shape == DMBuildingShape.HatchLid
                ? new Vector3(size.x, Mathf.Max(0.1f, size.y), size.z)
                : new Vector3(size.x, size.y, Mathf.Max(MinColliderDepth, size.z));

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                MeshFilter filter = root.GetComponent<MeshFilter>();
                MeshRenderer renderer = root.GetComponent<MeshRenderer>();
                BoxCollider[] boxes = root.GetComponents<BoxCollider>();
                bool already = filter != null && filter.sharedMesh == plane
                    && renderer != null && renderer.sharedMaterials.Length == 1 && renderer.sharedMaterial == material
                    && boxes.Length == 1 && !boxes[0].isTrigger && boxes[0].center == Vector3.zero && boxes[0].size == colliderSize
                    && root.transform.childCount == 0 && root.GetComponent<MeshCollider>() == null;
                if (already)
                    return false;

                for (int c = root.transform.childCount - 1; c >= 0; c--)
                    Object.DestroyImmediate(root.transform.GetChild(c).gameObject);
                MeshCollider[] meshColliders = root.GetComponents<MeshCollider>();
                for (int c = 0; c < meshColliders.Length; c++)
                    Object.DestroyImmediate(meshColliders[c]);

                if (filter == null)
                    filter = root.AddComponent<MeshFilter>();
                if (renderer == null)
                    renderer = root.AddComponent<MeshRenderer>();
                filter.sharedMesh = plane;
                renderer.sharedMaterials = new[] { material };
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                BoxCollider box = boxes.Length > 0 ? boxes[0] : root.AddComponent<BoxCollider>();
                for (int c = 1; c < boxes.Length; c++)
                    Object.DestroyImmediate(boxes[c]);
                box.isTrigger = false;
                box.center = Vector3.zero;
                box.size = colliderSize;

                root.layer = layer;
                root.tag = "Untagged";
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>One shared flat quad per shape, sized to the opening plus a small overlap so the field touches the frame.
        /// Doors and gates stand in the XY plane; the hatch lies in the XZ plane. The shader draws both sides.</summary>
        static Mesh EnsurePlane(DMBuildingShape shape)
        {
            EnsureFolder(MeshFolder);
            string name = "ff_plane_" + shape.ToString().ToLowerInvariant();
            string path = MeshFolder + "/" + name + ".asset";
            Vector3 size = DMBuildingCatalog.SizeFor(shape, null, Vector3.zero);
            bool flat = shape == DMBuildingShape.HatchLid;
            float w = size.x + (flat ? 0f : FrameOverlap);
            float h = (flat ? size.z : size.y + FrameOverlap);

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool created = mesh == null;
            if (created)
                mesh = new Mesh();

            float x = w * 0.5f;
            float y = h * 0.5f;
            Vector3[] verts = flat
                ? new[] { new Vector3(-x, 0f, -y), new Vector3(x, 0f, -y), new Vector3(x, 0f, y), new Vector3(-x, 0f, y) }
                : new[] { new Vector3(-x, -y, 0f), new Vector3(x, -y, 0f), new Vector3(x, y, 0f), new Vector3(-x, y, 0f) };
            Vector3 normal = flat ? Vector3.up : Vector3.back;
            mesh.Clear();
            mesh.vertices = verts;
            mesh.normals = new[] { normal, normal, normal, normal };
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.triangles = flat ? new[] { 0, 2, 1, 0, 3, 2 } : new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            mesh.name = name;
            if (created)
                AssetDatabase.CreateAsset(mesh, path);
            else
                EditorUtility.SetDirty(mesh);
            return mesh;
        }

        static bool TryFindTemplate(string suffix, out DMBuildingCatalog.KitPartTemplate template)
        {
            for (int i = 0; i < DMBuildingCatalog.KitTemplate.Length; i++)
            {
                if (DMBuildingCatalog.KitTemplate[i].Suffix == suffix)
                {
                    template = DMBuildingCatalog.KitTemplate[i];
                    return true;
                }
            }

            template = default;
            return false;
        }

        static int IndexOf(DMBuildingStyleLibrary style, string id)
        {
            for (int i = 0; i < style.parts.Count; i++)
            {
                if (style.parts[i] != null && style.parts[i].id == id)
                    return i;
            }

            return -1;
        }

        /// <summary>Same layer as the style's regular door, so build-mode rays and overlap checks treat it like one.</summary>
        static int LayerOf(DMBuildingStyleLibrary style, string prefix)
        {
            DMBuildingPartEntry door = style.FindPart(prefix + "door_basic");
            if (door != null && door.prefab != null)
                return door.prefab.layer;
            int layer = LayerMask.NameToLayer(DMBuildingGhostProfile.BuildingLayerName);
            return layer >= 0 ? layer : 0;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}

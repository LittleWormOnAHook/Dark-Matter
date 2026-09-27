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
    /// DM kit 0926-mesh-upgrades: swaps hand-modelled upgrade meshes into every style's legacy ProBuilder piece.
    /// Upgrade prefabs live in Library/Stone/Mesh Upgrades/Prefabs as stone_&lt;id&gt;.prefab (raw FBX variants).
    /// Each one is baked into a clean, grid-centred mesh asset (root rotation and scale applied, footprint snapped to the
    /// 4 m module, slabs top-aligned to the legacy top), then written onto &lt;prefix&gt;&lt;id&gt; for Stone, Iron and Silicate
    /// with that style's own materials. Window glass (a submesh whose material name contains "glass") becomes the
    /// GlassPane child so the runtime finish and ghost paint keep working. Colliders are refit and icons re-baked.
    /// </summary>
    public static class DMBuildingMeshUpgrades
    {
        public const string UpgradeRoot = DMBuildingStyleLibraryBuilder.PrefabLibraryRoot + "/Stone/Mesh Upgrades";
        public const string UpgradePrefabFolder = UpgradeRoot + "/Prefabs";
        public const string BakedFolder = UpgradeRoot + "/Baked";
        const string UpgradePrefix = "stone_";

        [MenuItem("Tools/Dark Matter Genesis/Buildings/Apply Mesh Upgrades (All Styles)")]
        public static void ApplyAllMenu()
        {
            List<DMBuildingStyleLibrary> styles = DMBuildingStyleLibraryBuilder.EnsureStyles();
            int total = 0;
            for (int i = 0; i < styles.Count; i++)
                total += ApplyToStyle(styles[i], bakeIcons: true);
            DMBuildingStyles.Invalidate();
            Debug.Log("[DM Mesh Upgrades] Applied " + total + " upgraded pieces across " + styles.Count + " styles. Icons are baking.");
        }

        /// <summary>Applies every upgrade prefab to the matching part of one style. Returns the number of prefabs changed.</summary>
        public static int ApplyToStyle(DMBuildingStyleLibrary style, bool bakeIcons)
        {
            if (style == null || style.parts == null)
                return 0;
            if (!AssetDatabase.IsValidFolder(UpgradePrefabFolder))
                return 0;

            EnsureFolder(BakedFolder);
            string prefix = DMBuildingStyleLibraryBuilder.PrefixOf(style);
            DMBuildingMaterialVariant first = style.FirstFinish();
            Material solid = first != null ? first.finishedMaterial : null;
            Material door = style.doorMaterial != null ? style.doorMaterial : solid;

            var log = new StringBuilder();
            var changed = new List<DMBuildingPartEntry>();
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { UpgradePrefabFolder });
            for (int g = 0; g < guids.Length; g++)
            {
                string upgradePath = AssetDatabase.GUIDToAssetPath(guids[g]);
                if (Path.GetDirectoryName(upgradePath).Replace('\\', '/') != UpgradePrefabFolder)
                    continue;
                string file = Path.GetFileNameWithoutExtension(upgradePath);
                if (!file.StartsWith(UpgradePrefix))
                    continue;
                string id = file.Substring(UpgradePrefix.Length);
                DMBuildingPartEntry part = style.FindPart(prefix + id);
                if (part == null || part.prefab == null)
                {
                    log.Append(" ").Append(prefix).Append(id).Append("=no part;");
                    continue;
                }

                if (DMBuildingCatalog.IsForceField(part.id))
                {
                    log.Append(" ").Append(part.id).Append("=force field, skipped;");
                    continue;
                }

                GameObject upgrade = AssetDatabase.LoadAssetAtPath<GameObject>(upgradePath);
                if (!TryBake(upgrade, part.shape, id, out Mesh shell, out Mesh glass, log))
                    continue;

                BackupOnce(part);
                Material shellMaterial = part.shape == DMBuildingShape.Door ? door : solid;
                if (ApplyToPrefab(style, part, shell, glass, shellMaterial, log))
                    changed.Add(part);
            }

            if (changed.Count > 0 && bakeIcons)
            {
                for (int i = 0; i < changed.Count; i++)
                    changed[i].icon = null;
                EditorUtility.SetDirty(style);
            }

            AssetDatabase.SaveAssets();
            for (int i = 0; i < changed.Count; i++)
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(changed[i].prefab), ImportAssetOptions.ForceUpdate);

            if (changed.Count > 0 && bakeIcons)
            {
                DMBuildingStyleLibrary captured = style;
                EditorApplication.delayCall += () => DMBuildingStyleLibraryBuilder.BakeIcons(captured, overwrite: false);
            }

            Debug.Log("[DM Mesh Upgrades] " + style.displayName + ": upgraded " + changed.Count + " pieces." + log);
            return changed.Count;
        }

        static bool TryBake(GameObject upgrade, DMBuildingShape shape, string id, out Mesh shell, out Mesh glass, StringBuilder log)
        {
            shell = null;
            glass = null;
            if (upgrade == null)
                return false;

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uv0 = new List<Vector2>();
            var uv1 = new List<Vector2>();
            var shellTris = new List<int>();
            var glassTris = new List<int>();

            Transform root = upgrade.transform;
            Matrix4x4 rootFix = Matrix4x4.TRS(Vector3.zero, root.localRotation, root.localScale) * root.worldToLocalMatrix;
            MeshFilter[] filters = upgrade.GetComponentsInChildren<MeshFilter>(true);
            for (int f = 0; f < filters.Length; f++)
            {
                Mesh src = filters[f].sharedMesh;
                if (src == null)
                    continue;
                MeshRenderer renderer = filters[f].GetComponent<MeshRenderer>();
                Material[] mats = renderer != null ? renderer.sharedMaterials : new Material[0];
                Matrix4x4 m = rootFix * filters[f].transform.localToWorldMatrix;
                Matrix4x4 n = m.inverse.transpose;
                bool flip = m.determinant < 0f;

                Vector3[] v = src.vertices;
                Vector3[] nn = src.normals;
                Vector2[] a = src.uv;
                Vector2[] b = src.uv2;
                int baseIndex = verts.Count;
                for (int i = 0; i < v.Length; i++)
                {
                    verts.Add(m.MultiplyPoint3x4(v[i]));
                    norms.Add(nn != null && nn.Length == v.Length ? n.MultiplyVector(nn[i]).normalized : Vector3.up);
                    uv0.Add(a != null && a.Length == v.Length ? a[i] : Vector2.zero);
                    uv1.Add(b != null && b.Length == v.Length ? b[i] : Vector2.zero);
                }

                int glassSub = GlassSubmesh(src, mats);
                for (int s = 0; s < src.subMeshCount; s++)
                {
                    int[] tris = src.GetTriangles(s);
                    List<int> dst = s == glassSub ? glassTris : shellTris;
                    for (int t = 0; t + 2 < tris.Length; t += 3)
                    {
                        dst.Add(tris[t] + baseIndex);
                        dst.Add((flip ? tris[t + 2] : tris[t + 1]) + baseIndex);
                        dst.Add((flip ? tris[t + 1] : tris[t + 2]) + baseIndex);
                    }
                }
            }

            if (verts.Count == 0 || shellTris.Count == 0)
            {
                log.Append(" ").Append(id).Append("=empty mesh;");
                return false;
            }

            Bounds bounds = new Bounds(verts[0], Vector3.zero);
            for (int i = 1; i < verts.Count; i++)
                bounds.Encapsulate(verts[i]);

            // Ramps rise toward local +Z in the kit; turn the model around if it was authored the other way.
            if (shape == DMBuildingShape.Ramp)
            {
                float band = bounds.extents.z * 0.5f;
                float front = 0f, back = 0f;
                int frontCount = 0, backCount = 0;
                for (int i = 0; i < verts.Count; i++)
                {
                    float z = verts[i].z - bounds.center.z;
                    if (z > band) { front += verts[i].y; frontCount++; }
                    else if (z < -band) { back += verts[i].y; backCount++; }
                }

                if (frontCount > 0 && backCount > 0 && front / frontCount < back / backCount)
                {
                    for (int i = 0; i < verts.Count; i++)
                    {
                        verts[i] = new Vector3(-verts[i].x, verts[i].y, -verts[i].z);
                        norms[i] = new Vector3(-norms[i].x, norms[i].y, -norms[i].z);
                    }

                    bounds = new Bounds(verts[0], Vector3.zero);
                    for (int i = 1; i < verts.Count; i++)
                        bounds.Encapsulate(verts[i]);
                    log.Append(" ").Append(id).Append("=turned to rise +Z;");
                }
            }

            // Snap the footprint to the module (the FBX exports are about 4.005 m, which z-fights at seams).
            float scale = 1f;
            Vector3 lattice = DMBuildingCatalog.SizeFor(shape, null, Vector3.zero);
            if (IsLattice(shape) && bounds.size.x > 0.01f)
            {
                float ratio = lattice.x / bounds.size.x;
                if (Mathf.Abs(ratio - 1f) < 0.03f)
                    scale = ratio;
            }

            bool topAlign = IsSlab(shape);
            Vector3 c = bounds.center * scale;
            float yOffset = topAlign ? lattice.y * 0.5f - bounds.max.y * scale : -c.y;
            Vector3 offset = new Vector3(-c.x, yOffset, -c.z);
            for (int i = 0; i < verts.Count; i++)
                verts[i] = verts[i] * scale + offset;

            shell = SaveMeshAsset(BuildMesh(verts, norms, uv0, uv1, shellTris), BakedFolder + "/" + id + ".asset", id);
            if (glassTris.Count > 0)
                glass = SaveMeshAsset(DoubleSided(BuildMesh(verts, norms, uv0, uv1, glassTris)), BakedFolder + "/" + id + "_glass.asset", id + "_glass");
            return shell != null;
        }

        static bool ApplyToPrefab(DMBuildingStyleLibrary style, DMBuildingPartEntry part, Mesh shell, Mesh glass, Material shellMaterial, StringBuilder log)
        {
            string path = AssetDatabase.GetAssetPath(part.prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                MeshFilter filter = root.GetComponent<MeshFilter>();
                MeshRenderer renderer = root.GetComponent<MeshRenderer>();
                if (filter == null || renderer == null)
                {
                    log.Append(" ").Append(part.id).Append("=no root mesh;");
                    return false;
                }

                if (shellMaterial == null)
                    shellMaterial = renderer.sharedMaterial;
                filter.sharedMesh = shell;
                var mats = new Material[Mathf.Max(1, shell.subMeshCount)];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = shellMaterial;
                renderer.sharedMaterials = mats;

                Transform pane = root.transform.Find("GlassPane");
                if (glass != null)
                {
                    if (pane == null)
                    {
                        pane = new GameObject("GlassPane").transform;
                        pane.SetParent(root.transform, false);
                    }

                    MeshFilter paneFilter = pane.GetComponent<MeshFilter>();
                    if (paneFilter == null)
                        paneFilter = pane.gameObject.AddComponent<MeshFilter>();
                    MeshRenderer paneRenderer = pane.GetComponent<MeshRenderer>();
                    if (paneRenderer == null)
                        paneRenderer = pane.gameObject.AddComponent<MeshRenderer>();
                    Material glassMaterial = style.glassMaterial != null ? style.glassMaterial : paneRenderer.sharedMaterial;
                    pane.localPosition = Vector3.zero;
                    pane.localRotation = Quaternion.identity;
                    pane.localScale = Vector3.one;
                    paneFilter.sharedMesh = glass;
                    paneRenderer.sharedMaterials = new[] { glassMaterial };
                    pane.gameObject.layer = root.layer;
                    pane.gameObject.tag = root.tag;
                    BoxCollider paneBox = pane.GetComponent<BoxCollider>();
                    if (paneBox != null)
                    {
                        paneBox.center = glass.bounds.center;
                        paneBox.size = glass.bounds.size;
                    }
                }

                BoxCollider box = root.GetComponent<BoxCollider>();
                if (box != null)
                {
                    box.center = shell.bounds.center;
                    box.size = shell.bounds.size;
                }

                MeshCollider meshCollider = root.GetComponent<MeshCollider>();
                if (meshCollider != null)
                {
                    meshCollider.sharedMesh = null;
                    meshCollider.sharedMesh = shell;
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Building Backups mirrors Library: the legacy prefab and icon are copied once, before the first swap.</summary>
        public const string BackupRoot = "Assets/_Project/Prefabs/Buildings/Building Backups";

        internal static void BackupOnce(DMBuildingPartEntry part)
        {
            string lib = DMBuildingStyleLibraryBuilder.PrefabLibraryRoot;
            string[] sources =
            {
                AssetDatabase.GetAssetPath(part.prefab),
                part.icon != null ? AssetDatabase.GetAssetPath(part.icon) : null,
            };
            for (int i = 0; i < sources.Length; i++)
            {
                string src = sources[i];
                if (string.IsNullOrEmpty(src) || !src.StartsWith(lib + "/"))
                    continue;
                string dst = BackupRoot + src.Substring(lib.Length);
                if (AssetDatabase.LoadMainAssetAtPath(dst) != null)
                    continue;
                EnsureFolder(Path.GetDirectoryName(dst).Replace('\\', '/'));
                AssetDatabase.CopyAsset(src, dst);
            }
        }

        /// <summary>Glass exported as a single-sided card vanishes from one side; add the back faces.</summary>
        static Mesh DoubleSided(Mesh mesh)
        {
            Vector3[] v = mesh.vertices;
            Vector3[] n = mesh.normals;
            Vector2[] a = mesh.uv;
            Vector2[] b = mesh.uv2;
            int[] t = mesh.triangles;
            int count = v.Length;
            var v2 = new Vector3[count * 2];
            var n2 = new Vector3[count * 2];
            var a2 = new Vector2[count * 2];
            var b2 = new Vector2[count * 2];
            for (int i = 0; i < count; i++)
            {
                v2[i] = v2[i + count] = v[i];
                n2[i] = n[i];
                n2[i + count] = -n[i];
                a2[i] = a2[i + count] = a.Length == count ? a[i] : Vector2.zero;
                b2[i] = b2[i + count] = b.Length == count ? b[i] : Vector2.zero;
            }

            var t2 = new int[t.Length * 2];
            for (int i = 0; i < t.Length; i += 3)
            {
                t2[i] = t[i];
                t2[i + 1] = t[i + 1];
                t2[i + 2] = t[i + 2];
                t2[t.Length + i] = t[i] + count;
                t2[t.Length + i + 1] = t[i + 2] + count;
                t2[t.Length + i + 2] = t[i + 1] + count;
            }

            mesh.indexFormat = count * 2 > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = v2;
            mesh.normals = n2;
            mesh.uv = a2;
            mesh.uv2 = b2;
            mesh.triangles = t2;
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        static int GlassSubmesh(Mesh mesh, Material[] mats)
        {
            if (mesh.subMeshCount < 2)
                return -1;
            for (int i = 0; i < mats.Length && i < mesh.subMeshCount; i++)
            {
                if (mats[i] != null && mats[i].name.ToLowerInvariant().Contains("glass"))
                    return i;
            }

            return -1;
        }

        static bool IsLattice(DMBuildingShape shape)
        {
            // Any grid piece: the 3% ratio guard in TryBake skips pieces whose width is not the lattice width.
            switch (shape)
            {
                case DMBuildingShape.Door:
                case DMBuildingShape.SurfaceItem:
                case DMBuildingShape.Custom:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>Slabs keep the legacy top height; any extra model depth hangs below it.</summary>
        static bool IsSlab(DMBuildingShape shape)
        {
            switch (shape)
            {
                case DMBuildingShape.Foundation:
                case DMBuildingShape.TriFoundation:
                case DMBuildingShape.HalfFoundation:
                case DMBuildingShape.QuarterFoundation:
                case DMBuildingShape.Floor:
                case DMBuildingShape.TriFloor:
                case DMBuildingShape.Ceiling:
                case DMBuildingShape.Hatch:
                    return true;
                default:
                    return false;
            }
        }

        static Mesh BuildMesh(List<Vector3> verts, List<Vector3> norms, List<Vector2> uv0, List<Vector2> uv1, List<int> tris)
        {
            var remap = new Dictionary<int, int>();
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var a = new List<Vector2>();
            var b = new List<Vector2>();
            var t = new int[tris.Count];
            for (int i = 0; i < tris.Count; i++)
            {
                int src = tris[i];
                if (!remap.TryGetValue(src, out int dst))
                {
                    dst = v.Count;
                    remap[src] = dst;
                    v.Add(verts[src]);
                    n.Add(norms[src]);
                    a.Add(uv0[src]);
                    b.Add(uv1[src]);
                }

                t[i] = dst;
            }

            var mesh = new Mesh();
            mesh.indexFormat = v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, a);
            mesh.SetUVs(1, b);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        static Mesh SaveMeshAsset(Mesh mesh, string path, string name)
        {
            mesh.name = name;
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }

            existing.Clear();
            EditorUtility.CopySerialized(mesh, existing);
            existing.name = name;
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mesh);
            return existing;
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

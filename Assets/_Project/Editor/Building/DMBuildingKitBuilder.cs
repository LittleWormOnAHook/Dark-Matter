using System.Collections.Generic;
using System.IO;
using Project.Building;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;

namespace Project.EditorTools.Building
{
    /// <summary>
    /// DM kit 0926: builds one building style kit (Stone, Iron, Silicate, ...) with ProBuilder. Every piece is centred on
    /// its bounds and sized to the 4 m grid from <see cref="DMBuildingCatalog"/> constants plus the style's kit settings.
    /// Meshes, prefabs and part rows are written in place under Prefabs/Buildings/Library/&lt;Style&gt;.
    /// Wall pieces: local +Z is the outer face; placement seats the outer face on the grid line.
    /// Ramp, stairs and roofs rise toward local +Z.
    /// </summary>
    public static partial class DMBuildingKitBuilder
    {
        public const string StoneRoot = DMBuildingStyleLibraryBuilder.PrefabLibraryRoot + "/Stone";

        static readonly string[] Obsolete =
        {
            StoneRoot + "/Slopes/stone_slope_4x4.prefab",
        };

        // Set per rebuild by RebuildStyle.
        static string styleRoot = StoneRoot;
        static string prefix = "stone_";
        static DMBuildingKitSettings kit = new DMBuildingKitSettings();

        static string MeshFolder => styleRoot + "/Meshes";

        enum ColliderKind
        {
            Box,
            Mesh,
        }

        sealed class KitPiece
        {
            public string Id;
            public string Folder;
            public ColliderKind Collider;
            public Material Material;
            public readonly List<ProBuilderMesh> Solid = new List<ProBuilderMesh>();
            public readonly List<ProBuilderMesh> Glass = new List<ProBuilderMesh>();
            public readonly List<ProBuilderMesh> Trim = new List<ProBuilderMesh>();
        }

        static int kitLayer = -1;

        [MenuItem("Tools/Dark Matter Genesis/Buildings/Rebuild Stone Kit (ProBuilder)")]
        public static void RebuildStoneKit()
        {
            DMBuildingStyleLibraryBuilder.EnsureStyles();
            RebuildStyle(DMBuildingStyles.Find(DMBuildingStyles.DefaultId));
        }

        [MenuItem("Tools/Dark Matter Genesis/Buildings/Rebuild All Style Kits (ProBuilder)")]
        public static void RebuildAllKits()
        {
            List<DMBuildingStyleLibrary> styles = DMBuildingStyleLibraryBuilder.EnsureStyles();
            for (int i = 0; i < styles.Count; i++)
                RebuildStyle(styles[i], refresh: i == styles.Count - 1);
        }

        /// <summary>Builds every kit shape for one style into Prefabs/Buildings/Library/&lt;Style&gt; and links the prefabs.</summary>
        public static void RebuildStyle(DMBuildingStyleLibrary style, bool refresh = true)
        {
            if (style == null)
            {
                Debug.LogWarning("[DM Building Kit] No style to rebuild. Run Tools/Dark Matter Genesis/Buildings/Ensure Style Libraries first.");
                return;
            }

            kitLayer = Project.EditorTools.EditorTagUtility.EnsureLayer(DMBuildingGhostProfile.BuildingLayerName); // 0925-layers
            styleRoot = DMBuildingStyleLibraryBuilder.StyleRoot(style);
            prefix = DMBuildingStyleLibraryBuilder.PrefixOf(style);
            kit = style.kit ?? new DMBuildingKitSettings();
            DMBuildingMaterialVariant first = style.FirstFinish();
            Material solid = first != null ? first.finishedMaterial : null;
            Material glass = style.glassMaterial;
            Material door = style.doorMaterial != null ? style.doorMaterial : solid;
            EnsureFolder(MeshFolder);

            var built = new List<KeyValuePair<string, GameObject>>();
            foreach (KitPiece piece in BuildPieces(solid, door))
            {
                GameObject prefab = SavePiece(piece, glass);
                if (prefab != null)
                    built.Add(new KeyValuePair<string, GameObject>(piece.Id, prefab));
            }

            List<string> removed = style.styleId == DMBuildingStyles.DefaultId ? DeleteObsolete() : new List<string>();
            UpdateStyleParts(style, built);
            AssetDatabase.SaveAssets();
            if (refresh)
                AssetDatabase.Refresh();
            DMBuildingStyles.Invalidate();
            Debug.Log(
                "[DM Building Kit] Rebuilt " + built.Count + " " + style.displayName + " pieces in " + styleRoot
                + (removed.Count > 0 ? ". Removed: " + string.Join(", ", removed) : "."));
        }

        // ---------------------------------------------------------------- piece shapes

        static IEnumerable<KitPiece> BuildPieces(Material stone, Material door)
        {
            float m = DMBuildingCatalog.ModuleMeters;
            float h = DMBuildingCatalog.StoryHeight;
            float t = DMBuildingCatalog.WallThickness;

            // Horizontal slabs (bottom at y = 0; recentred on save).
            yield return Box(prefix + "foundation_4x4", "Foundations", stone, new Vector3(m, DMBuildingCatalog.FoundationHeight, m));
            yield return Box(prefix + "floor_4x4", "Floors", stone, new Vector3(m, DMBuildingCatalog.SlabThickness, m));
            yield return Box(prefix + "ceiling_4x4", "Ceilings", stone, new Vector3(m, DMBuildingCatalog.SlabThickness, m));
            yield return TriSlab(prefix + "foundation_tri_4x4", "Foundations", stone, DMBuildingCatalog.FoundationHeight);
            yield return TriSlab(prefix + "floor_tri_4x4", "Floors", stone, DMBuildingCatalog.SlabThickness);
            yield return Hatch(stone);

            // Edge pieces (thin axis = local Z).
            yield return Box(prefix + "wall_4x4", "Walls", stone, new Vector3(m, h, t));
            yield return Box(prefix + "wall_half_4x2", "Walls", stone, new Vector3(m, DMBuildingCatalog.HalfWallHeight, t));
            yield return Window(stone);
            yield return Opening(prefix + "door_frame_4x4", "DoorFrames", stone, DMBuildingCatalog.DoorWidth, DMBuildingCatalog.DoorHeight);
            yield return Opening(prefix + "passage_4x4", "Walls", stone, kit.PassageWidth, kit.PassageHeight);
            yield return TriWall(prefix + "wall_tri_l_4x2", stone, left: true);
            yield return TriWall(prefix + "wall_tri_r_4x2", stone, left: false);
            yield return Railing(stone);
            yield return Box(prefix + "door_basic", "Doors", door, new Vector3(DMBuildingCatalog.DoorWidth, DMBuildingCatalog.DoorHeight, DMBuildingCatalog.DoorDepth));

            // Ground pieces that climb.
            yield return Stairs(stone);
            yield return Wedge(prefix + "ramp_4x4", "Ramps", stone, h);
            yield return Wedge(prefix + "roof_4x4", "Roofs", stone, DMBuildingCatalog.RoofRise);
            yield return RoofCorner(stone);
            yield return RoofInner(stone);

            // Kit phase 2 (0926).
            foreach (KitPiece extra in BuildKit2Pieces(stone, door))
                yield return extra;
        }

        static KitPiece New(string id, string folder, Material material, ColliderKind collider)
        {
            return new KitPiece { Id = id, Folder = folder, Material = material, Collider = collider };
        }

        static KitPiece Box(string id, string folder, Material material, Vector3 size)
        {
            KitPiece piece = New(id, folder, material, ColliderKind.Box);
            piece.Solid.Add(Cube(size, new Vector3(0f, size.y * 0.5f, 0f)));
            return piece;
        }

        static KitPiece TriSlab(string id, string folder, Material material, float thickness)
        {
            float half = DMBuildingCatalog.ModuleMeters * 0.5f;
            KitPiece piece = New(id, folder, material, ColliderKind.Mesh);
            piece.Solid.Add(Prism(
                new[] { new Vector3(-half, 0f, -half), new Vector3(half, 0f, -half), new Vector3(-half, 0f, half) },
                new Vector3(0f, thickness, 0f)));
            return piece;
        }

        static KitPiece Hatch(Material material)
        {
            float m = DMBuildingCatalog.ModuleMeters;
            float s = DMBuildingCatalog.SlabThickness;
            float o = Mathf.Min(kit.HatchOpening, m - 0.4f);
            float band = (m - o) * 0.5f;
            float y = s * 0.5f;
            KitPiece piece = New(prefix + "hatch_4x4", "Ceilings", material, ColliderKind.Mesh);
            piece.Solid.Add(Cube(new Vector3(m, s, band), new Vector3(0f, y, -(o * 0.5f + band * 0.5f))));
            piece.Solid.Add(Cube(new Vector3(m, s, band), new Vector3(0f, y, o * 0.5f + band * 0.5f)));
            piece.Solid.Add(Cube(new Vector3(band, s, o), new Vector3(-(o * 0.5f + band * 0.5f), y, 0f)));
            piece.Solid.Add(Cube(new Vector3(band, s, o), new Vector3(o * 0.5f + band * 0.5f, y, 0f)));
            return piece;
        }

        static KitPiece Window(Material material)
        {
            float m = DMBuildingCatalog.ModuleMeters;
            float h = DMBuildingCatalog.StoryHeight;
            float t = DMBuildingCatalog.WallThickness;
            float ww = Mathf.Min(kit.WindowWidth, m - 0.4f);
            float sill = kit.WindowSill;
            float wh = Mathf.Min(kit.WindowHeight, h - sill - 0.2f);
            float jamb = (m - ww) * 0.5f;
            float head = h - sill - wh;
            KitPiece piece = New(prefix + "wall_window_4x4", "Windows", material, ColliderKind.Mesh);
            piece.Solid.Add(Cube(new Vector3(jamb, h, t), new Vector3(-(ww * 0.5f + jamb * 0.5f), h * 0.5f, 0f)));
            piece.Solid.Add(Cube(new Vector3(jamb, h, t), new Vector3(ww * 0.5f + jamb * 0.5f, h * 0.5f, 0f)));
            piece.Solid.Add(Cube(new Vector3(ww, sill, t), new Vector3(0f, sill * 0.5f, 0f)));
            piece.Solid.Add(Cube(new Vector3(ww, head, t), new Vector3(0f, h - head * 0.5f, 0f)));
            piece.Glass.Add(Cube(new Vector3(ww, wh, kit.GlassThickness), new Vector3(0f, sill + wh * 0.5f, 0f)));
            return piece;
        }

        static KitPiece Opening(string id, string folder, Material material, float width, float height)
        {
            float m = DMBuildingCatalog.ModuleMeters;
            float h = DMBuildingCatalog.StoryHeight;
            float t = DMBuildingCatalog.WallThickness;
            width = Mathf.Min(width, m - 0.4f);
            height = Mathf.Min(height, h - 0.2f);
            float leg = (m - width) * 0.5f;
            float header = h - height;
            KitPiece piece = New(id, folder, material, ColliderKind.Mesh);
            piece.Solid.Add(Cube(new Vector3(leg, h, t), new Vector3(-(width * 0.5f + leg * 0.5f), h * 0.5f, 0f)));
            piece.Solid.Add(Cube(new Vector3(leg, h, t), new Vector3(width * 0.5f + leg * 0.5f, h * 0.5f, 0f)));
            piece.Solid.Add(Cube(new Vector3(width, header, t), new Vector3(0f, h - header * 0.5f, 0f)));
            return piece;
        }

        static KitPiece TriWall(string id, Material material, bool left)
        {
            float half = DMBuildingCatalog.ModuleMeters * 0.5f;
            float t = DMBuildingCatalog.WallThickness;
            float rise = DMBuildingCatalog.RoofRise;
            float peakX = left ? -half : half;
            KitPiece piece = New(id, "Walls", material, ColliderKind.Mesh);
            piece.Solid.Add(Prism(
                new[] { new Vector3(-half, 0f, -t * 0.5f), new Vector3(half, 0f, -t * 0.5f), new Vector3(peakX, rise, -t * 0.5f) },
                new Vector3(0f, 0f, t)));
            return piece;
        }

        static KitPiece Railing(Material material)
        {
            float m = DMBuildingCatalog.ModuleMeters;
            float rh = DMBuildingCatalog.RailingHeight;
            float rt = DMBuildingCatalog.RailingThickness;
            float rail = 0.12f;
            float post = rt;
            int posts = kit.RailingPosts;
            KitPiece piece = New(prefix + "railing_4x1", "Railings", material, ColliderKind.Box);
            piece.Solid.Add(Cube(new Vector3(m, rail, rt), new Vector3(0f, rh - rail * 0.5f, 0f)));
            piece.Solid.Add(Cube(new Vector3(m, rail, rt), new Vector3(0f, 0.15f + rail * 0.5f, 0f)));
            float span = m - post;
            for (int i = 0; i < posts; i++)
            {
                float x = -span * 0.5f + span * i / (posts - 1);
                piece.Solid.Add(Cube(new Vector3(post, rh - rail, rt), new Vector3(x, (rh - rail) * 0.5f, 0f)));
            }

            return piece;
        }

        static KitPiece Stairs(Material material)
        {
            float m = DMBuildingCatalog.ModuleMeters;
            float h = DMBuildingCatalog.StoryHeight;
            int steps = kit.StairSteps;
            float run = m / steps;
            float rise = h / steps;
            KitPiece piece = New(prefix + "stairs_4x4", "Stairs", material, ColliderKind.Mesh);
            for (int i = 0; i < steps; i++)
            {
                float top = rise * (i + 1);
                piece.Solid.Add(Cube(new Vector3(m, top, run), new Vector3(0f, top * 0.5f, -m * 0.5f + run * (i + 0.5f))));
            }

            return piece;
        }

        static KitPiece Wedge(string id, string folder, Material material, float rise)
        {
            float half = DMBuildingCatalog.ModuleMeters * 0.5f;
            KitPiece piece = New(id, folder, material, ColliderKind.Mesh);
            piece.Solid.Add(WedgeMesh(half, rise, alongX: false));
            return piece;
        }

        static KitPiece RoofCorner(Material material)
        {
            float a = DMBuildingCatalog.ModuleMeters * 0.5f;
            float r = DMBuildingCatalog.RoofRise;
            KitPiece piece = New(prefix + "roof_corner_4x4", "Roofs", material, ColliderKind.Mesh);
            // Outer (hip) corner: eaves on -X and -Z, apex at the +X +Z corner.
            var v = new[]
            {
                new Vector3(-a, 0f, -a), new Vector3(a, 0f, -a), new Vector3(a, 0f, a), new Vector3(-a, 0f, a),
                new Vector3(a, r, a),
            };
            piece.Solid.Add(Solid(v, new[]
            {
                new[] { 0, 1, 2, 3 },
                new[] { 0, 1, 4 },
                new[] { 0, 3, 4 },
                new[] { 1, 2, 4 },
                new[] { 3, 2, 4 },
            }));
            return piece;
        }

        static KitPiece RoofInner(Material material)
        {
            float half = DMBuildingCatalog.ModuleMeters * 0.5f;
            float r = DMBuildingCatalog.RoofRise;
            KitPiece piece = New(prefix + "roof_inner_4x4", "Roofs", material, ColliderKind.Mesh);
            // Inner (valley) corner: two roof wedges rising to +Z and +X overlap into one piece.
            piece.Solid.Add(WedgeMesh(half, r, alongX: false));
            piece.Solid.Add(WedgeMesh(half, r, alongX: true));
            return piece;
        }

        static ProBuilderMesh WedgeMesh(float half, float rise, bool alongX)
        {
            if (!alongX)
            {
                return Prism(
                    new[] { new Vector3(-half, 0f, -half), new Vector3(-half, 0f, half), new Vector3(-half, rise, half) },
                    new Vector3(half * 2f, 0f, 0f));
            }

            return Prism(
                new[] { new Vector3(-half, 0f, -half), new Vector3(half, 0f, -half), new Vector3(half, rise, -half) },
                new Vector3(0f, 0f, half * 2f));
        }

        // ---------------------------------------------------------------- ProBuilder helpers

        static ProBuilderMesh Cube(Vector3 size, Vector3 center)
        {
            ProBuilderMesh mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
            mesh.transform.position = center;
            return FinishPart(mesh);
        }

        /// <summary>Convex prism: cap polygon extruded along <paramref name="extrude"/>.</summary>
        static ProBuilderMesh Prism(Vector3[] cap, Vector3 extrude)
        {
            int n = cap.Length;
            var verts = new Vector3[n * 2];
            for (int i = 0; i < n; i++)
            {
                verts[i] = cap[i];
                verts[i + n] = cap[i] + extrude;
            }

            var faces = new List<int[]>();
            var bottom = new int[n];
            var top = new int[n];
            for (int i = 0; i < n; i++)
            {
                bottom[i] = i;
                top[i] = i + n;
            }

            faces.Add(bottom);
            faces.Add(top);
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                faces.Add(new[] { i, j, j + n, i + n });
            }

            return Solid(verts, faces.ToArray());
        }

        /// <summary>
        /// Convex solid from shared corner points. Each face gets its own vertices (hard edges) and is wound to face
        /// away from the solid's centroid, so callers can list polygon corners in any order around the face.
        /// </summary>
        static ProBuilderMesh Solid(Vector3[] corners, int[][] polygons)
        {
            Vector3 centroid = Vector3.zero;
            for (int i = 0; i < corners.Length; i++)
                centroid += corners[i];
            centroid /= Mathf.Max(1, corners.Length);

            var positions = new List<Vector3>();
            var faces = new List<Face>();
            for (int f = 0; f < polygons.Length; f++)
            {
                int[] poly = polygons[f];
                if (poly.Length < 3)
                    continue;

                Vector3 faceCenter = Vector3.zero;
                for (int i = 0; i < poly.Length; i++)
                    faceCenter += corners[poly[i]];
                faceCenter /= poly.Length;

                Vector3 a = corners[poly[0]];
                Vector3 normal = Vector3.Cross(corners[poly[1]] - a, corners[poly[2]] - a);
                bool flip = Vector3.Dot(normal, faceCenter - centroid) < 0f;

                int start = positions.Count;
                for (int i = 0; i < poly.Length; i++)
                    positions.Add(corners[poly[flip ? poly.Length - 1 - i : i]]);

                var tris = new List<int>();
                for (int i = 1; i < poly.Length - 1; i++)
                {
                    tris.Add(start);
                    tris.Add(start + i);
                    tris.Add(start + i + 1);
                }

                faces.Add(new Face(tris.ToArray()));
            }

            ProBuilderMesh mesh = ProBuilderMesh.Create(positions, faces);
            return FinishPart(mesh);
        }

        static ProBuilderMesh FinishPart(ProBuilderMesh mesh)
        {
            // World-space UVs so stone texture flows across neighbouring pieces at the same scale.
            IList<Face> faces = mesh.faces;
            for (int i = 0; i < faces.Count; i++)
            {
                AutoUnwrapSettings uv = faces[i].uv;
                uv.useWorldSpace = true;
                faces[i].uv = uv;
            }

            mesh.ToMesh();
            mesh.Refresh();
            return mesh;
        }

        // ---------------------------------------------------------------- save

        static GameObject SavePiece(KitPiece piece, Material glassMaterial)
        {
            Mesh solid = Combine(piece.Solid);
            Mesh glass = piece.Glass.Count > 0 ? Combine(piece.Glass) : null;
            Mesh trim = piece.Trim.Count > 0 ? Combine(piece.Trim) : null;
            DestroyParts(piece.Solid);
            DestroyParts(piece.Glass);
            DestroyParts(piece.Trim);
            if (solid == null)
                return null;

            Vector3 offset = solid.bounds.center;
            Recentre(solid, offset);
            if (glass != null)
                Recentre(glass, offset);
            if (trim != null)
                Recentre(trim, offset);

            solid = SaveMesh(solid, piece.Id);
            if (glass != null)
                glass = SaveMesh(glass, piece.Id + "_glass");
            if (trim != null)
                trim = SaveMesh(trim, piece.Id + "_trim");

            var root = new GameObject(piece.Id);
            try
            {
                root.AddComponent<MeshFilter>().sharedMesh = solid;
                root.AddComponent<MeshRenderer>().sharedMaterial = piece.Material;
                if (piece.Collider == ColliderKind.Box)
                {
                    BoxCollider box = root.AddComponent<BoxCollider>();
                    box.center = solid.bounds.center;
                    box.size = solid.bounds.size;
                }
                else
                {
                    MeshCollider collider = root.AddComponent<MeshCollider>();
                    collider.sharedMesh = solid;
                    collider.convex = false;
                }

                if (glass != null)
                {
                    var pane = new GameObject("GlassPane");
                    pane.transform.SetParent(root.transform, false);
                    pane.AddComponent<MeshFilter>().sharedMesh = glass;
                    pane.AddComponent<MeshRenderer>().sharedMaterial = glassMaterial;
                }

                if (trim != null)
                {
                    var trimObject = new GameObject("Trim");
                    trimObject.transform.SetParent(root.transform, false);
                    trimObject.AddComponent<MeshFilter>().sharedMesh = trim;
                    trimObject.AddComponent<MeshRenderer>().sharedMaterial = piece.Material;
                    MeshCollider trimCollider = trimObject.AddComponent<MeshCollider>();
                    trimCollider.sharedMesh = trim;
                }

                // 0925: every build piece is climbable.
                foreach (Transform part in root.GetComponentsInChildren<Transform>(true))
                {
                    part.gameObject.tag = DMBuildingPieceFactory.ClimbableTag;
                    if (kitLayer >= 0)
                        part.gameObject.layer = kitLayer;
                }

                string folder = styleRoot + "/" + piece.Folder;
                EnsureFolder(folder);
                return PrefabUtility.SaveAsPrefabAsset(root, folder + "/" + piece.Id + ".prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static Mesh Combine(List<ProBuilderMesh> parts)
        {
            var instances = new List<CombineInstance>();
            for (int i = 0; i < parts.Count; i++)
            {
                MeshFilter filter = parts[i] != null ? parts[i].GetComponent<MeshFilter>() : null;
                if (filter == null || filter.sharedMesh == null)
                    continue;
                instances.Add(new CombineInstance
                {
                    mesh = filter.sharedMesh,
                    subMeshIndex = 0,
                    transform = parts[i].transform.localToWorldMatrix,
                });
            }

            if (instances.Count == 0)
                return null;

            var mesh = new Mesh();
            mesh.CombineMeshes(instances.ToArray(), true, true);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        static void Recentre(Mesh mesh, Vector3 offset)
        {
            Vector3[] vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] -= offset;
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }

        static void DestroyParts(List<ProBuilderMesh> parts)
        {
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] != null)
                    Object.DestroyImmediate(parts[i].gameObject);
            }

            parts.Clear();
        }

        static Mesh SaveMesh(Mesh mesh, string name)
        {
            string path = MeshFolder + "/" + name + ".asset";
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

        static List<string> DeleteObsolete()
        {
            var removed = new List<string>();
            for (int i = 0; i < Obsolete.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<Object>(Obsolete[i]) == null)
                    continue;
                if (AssetDatabase.DeleteAsset(Obsolete[i]))
                    removed.Add(Path.GetFileName(Obsolete[i]));
            }

            string slopes = StoneRoot + "/Slopes";
            if (AssetDatabase.IsValidFolder(slopes) && AssetDatabase.FindAssets(string.Empty, new[] { slopes }).Length == 0)
                AssetDatabase.DeleteAsset(slopes);
            return removed;
        }

        static void UpdateStyleParts(DMBuildingStyleLibrary style, List<KeyValuePair<string, GameObject>> built)
        {
            if (style.parts == null)
                style.parts = new List<DMBuildingPartEntry>();
            for (int i = 0; i < built.Count; i++)
            {
                DMBuildingPartEntry part = style.FindPart(built[i].Key);
                if (part == null)
                {
                    string suffix = built[i].Key.Substring(prefix.Length);
                    for (int t = 0; t < DMBuildingCatalog.KitTemplate.Length; t++)
                    {
                        if (DMBuildingCatalog.KitTemplate[t].Suffix != suffix)
                            continue;
                        part = DMBuildingStyleLibraryBuilder.NewKitPart(built[i].Key, DMBuildingCatalog.KitTemplate[t]);
                        break;
                    }

                    if (part == null)
                        part = new DMBuildingPartEntry { id = built[i].Key, displayName = built[i].Key };
                    style.parts.Add(part);
                }

                part.prefab = built[i].Value;
            }

            EditorUtility.SetDirty(style);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}

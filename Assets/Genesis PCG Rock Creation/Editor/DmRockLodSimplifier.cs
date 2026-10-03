#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>
    /// LOD mesh simplification. Uses Gaia's bundled UnityMeshSimplifier (LODGeneratorHelper's simplifier and default
    /// quality/options) through reflection, so this package compiles and works without Gaia. Without it a built-in
    /// vertex-clustering simplifier is used (lower quality, logs a one-time warning).
    /// </summary>
    public static class DmRockLodSimplifier
    {
        private const string GaiaNs = "UnityMeshSimplifierGaia";
        private static bool s_init;
        private static Type s_simplifierType, s_helperType;
        private static object s_options;            // boxed SimplificationOptions from the helper
        private static float[] s_helperQualities;   // LODLevel.Quality of the helper's default levels
        private static bool s_warned;

        /// <summary>Test hook: force the built-in simplifier even when Gaia is present.</summary>
        public static bool ForceBuiltin;

        public static bool GaiaAvailable { get { Init(); return s_simplifierType != null; } }
        public static string Backend => GaiaAvailable && !ForceBuiltin ? "Gaia UnityMeshSimplifier (LODGeneratorHelper defaults)" : "built-in vertex clustering";

        private static void Init()
        {
            if (s_init) return;
            s_init = true;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType(GaiaNs + ".MeshSimplifier", false);
                if (t == null) continue;
                s_simplifierType = t;
                s_helperType = asm.GetType(GaiaNs + ".LODGeneratorHelper", false);
                break;
            }
            if (s_simplifierType == null)
                return;
            ReadHelperDefaults();
        }

        /// <summary>Reads the helper's own defaults (Reset(): levels + SimplificationOptions) from a temporary hidden instance.</summary>
        private static void ReadHelperDefaults()
        {
            Type optType = s_simplifierType.Assembly.GetType(GaiaNs + ".SimplificationOptions", false);
            FieldInfo defField = optType?.GetField("Default", BindingFlags.Public | BindingFlags.Static);
            s_options = defField?.GetValue(null);
            if (s_helperType == null)
                return;
            var go = new GameObject("__RockLodDefaults") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                Component helper = go.AddComponent(s_helperType);
                MethodInfo reset = s_helperType.GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                reset?.Invoke(helper, null);
                object opts = s_helperType.GetProperty("SimplificationOptions")?.GetValue(helper);
                if (opts != null) s_options = opts;
                if (s_helperType.GetProperty("Levels")?.GetValue(helper) is Array levels && levels.Length > 0)
                {
                    var q = new List<float>();
                    foreach (object lv in levels)
                    {
                        PropertyInfo qp = lv.GetType().GetProperty("Quality");
                        if (qp != null) q.Add((float)qp.GetValue(lv));
                    }
                    if (q.Count > 0) s_helperQualities = q.ToArray();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Rock LODs: could not read LODGeneratorHelper defaults: " + e.Message);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Triangle quality per LOD (LOD0 first). Uses the helper's defaults (1, 0.65, 0.4225); levels beyond the helper's
        /// count continue its geometric ratio (0.65^n).
        /// </summary>
        public static float[] Qualities(int lodCount)
        {
            Init();
            float[] src = s_helperQualities ?? new[] { 1f, 0.65f, 0.4225f };
            var q = new float[lodCount];
            for (int i = 0; i < lodCount; i++)
            {
                if (i < src.Length) q[i] = src[i];
                else
                {
                    float ratio = src.Length >= 2 && src[src.Length - 2] > 0f ? src[src.Length - 1] / src[src.Length - 2] : 0.65f;
                    q[i] = q[i - 1] * ratio;
                }
            }
            return q;
        }

        /// <param name="preserveBorders">Keep open (border) edges in place: the flush ground cut's bottom rim then stays on
        /// the terrain in every LOD.</param>
        public static Mesh Simplify(Mesh source, float quality, bool preserveBorders = false)
        {
            if (quality >= 0.999f)
                return Object.Instantiate(source);
            Init();
            Mesh m = null;
            if (s_simplifierType != null && !ForceBuiltin)
            {
                try { m = SimplifyGaia(source, quality, preserveBorders); }
                catch (Exception e)
                {
                    Debug.LogWarning("Rock LODs: Gaia simplifier failed, using the built-in one: " + (e.InnerException ?? e).Message);
                    m = null;
                }
            }
            else if (!s_warned && !ForceBuiltin)
            {
                s_warned = true;
                Debug.LogWarning("Genesis PCG Rock Creation: Gaia's mesh simplifier was not found; LODs use the built-in clustering simplifier.");
            }
            if (m == null)
                m = ClusterSimplify(source, quality);
            Finish(m, source);
            return m;
        }

        private static Mesh SimplifyGaia(Mesh source, float quality, bool preserveBorders)
        {
            object s = Activator.CreateInstance(s_simplifierType);
            object opts = s_options;
            if (opts != null && preserveBorders)
            {
                // copy the boxed struct (never modify the shared defaults)
                opts = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(s_options, null);
                FieldInfo f = opts.GetType().GetField("PreserveBorderEdges");
                if (f != null) f.SetValue(opts, true);
                else opts.GetType().GetProperty("PreserveBorderEdges")?.SetValue(opts, true);
            }
            if (opts != null)
                s_simplifierType.GetProperty("SimplificationOptions")?.SetValue(s, opts);
            s_simplifierType.GetMethod("Initialize", new[] { typeof(Mesh) }).Invoke(s, new object[] { source });
            s_simplifierType.GetMethod("SimplifyMesh", new[] { typeof(float) }).Invoke(s, new object[] { quality });
            return (Mesh)s_simplifierType.GetMethod("ToMesh", Type.EmptyTypes).Invoke(s, null);
        }

        private static void Finish(Mesh m, Mesh source)
        {
            // Vertex colours (R bottom blend, G up exposure) come out interpolated as float colours: store them as 8-bit again.
            var cols = new List<Color>();
            m.GetColors(cols);
            if (cols.Count == m.vertexCount && cols.Count > 0)
            {
                var c32 = new Color32[cols.Count];
                for (int i = 0; i < c32.Length; i++) c32[i] = cols[i];
                m.colors32 = c32;
            }
            // Texture-array slice / group (UV3) are indices: never let interpolation leave fractional values.
            var s3 = new List<Vector2>(); source.GetUVs(3, s3);
            if (s3.Count > 0)
            {
                var u3 = new List<Vector2>(); m.GetUVs(3, u3);
                if (u3.Count != m.vertexCount) { u3.Clear(); for (int i = 0; i < m.vertexCount; i++) u3.Add(s3[0]); }
                for (int i = 0; i < u3.Count; i++) u3[i] = new Vector2(Mathf.Round(u3[i].x), Mathf.Round(u3[i].y));
                m.SetUVs(3, u3);
            }
            // Keep exactly the source's sub-mesh count (empty sub-meshes stay valid).
            if (m.subMeshCount < source.subMeshCount) m.subMeshCount = source.subMeshCount;
            m.RecalculateTangents();
            m.RecalculateBounds();
        }

        // ------------------------------------------------------------------------------------------------
        // Built-in fallback: vertex clustering on a grid (cluster key also includes a coarse normal direction and UV
        // cell so opposite faces and different UV islands are not welded). Cell size is searched to hit the target count.

        public static Mesh ClusterSimplify(Mesh source, float quality)
        {
            Vector3[] v = source.vertices;
            Vector3[] n = source.normals;
            var uv = new List<Vector2>(); source.GetUVs(0, uv);
            var uv3 = new List<Vector2>(); source.GetUVs(3, uv3);
            int subs = source.subMeshCount;
            var tris = new int[subs][];
            int total = 0;
            for (int s = 0; s < subs; s++) { tris[s] = source.GetTriangles(s); total += tris[s].Length / 3; }
            int target = Mathf.Max(4, Mathf.RoundToInt(total * quality));
            Bounds b = source.bounds;
            float diag = Mathf.Max(1e-3f, b.size.magnitude);

            float lo = diag / 2000f, hi = diag / 4f;
            int[] bestMap = null; int[][] bestTris = null;
            for (int it = 0; it < 14; it++)
            {
                float cell = Mathf.Sqrt(lo * hi);
                Cluster(v, n, uv, uv3, tris, b.min, cell, out int[] map, out int[][] outTris, out int count);
                if (bestMap == null || count >= target) { bestMap = map; bestTris = outTris; }
                if (count > target) lo = cell; else hi = cell;
                if (Mathf.Abs(count - target) <= target * 0.03f) { bestMap = map; bestTris = outTris; break; }
            }

            // Compact: every cluster representative becomes one vertex.
            var remap = new Dictionary<int, int>();
            var order = new List<int>();
            foreach (int[] t in bestTris)
                foreach (int i in t)
                    if (!remap.ContainsKey(i)) { remap[i] = order.Count; order.Add(i); }
            var mesh = new Mesh { indexFormat = order.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(Pick(v, order));
            if (n != null && n.Length == v.Length) mesh.SetNormals(Pick(n, order));
            if (uv.Count == v.Length) mesh.SetUVs(0, Pick(uv.ToArray(), order));
            if (uv3.Count == v.Length) mesh.SetUVs(3, Pick(uv3.ToArray(), order));
            Color32[] c = source.colors32;
            if (c != null && c.Length == v.Length) mesh.colors32 = Pick(c, order).ToArray();
            mesh.subMeshCount = subs;
            for (int s = 0; s < subs; s++)
            {
                int[] t = bestTris[s];
                var o = new int[t.Length];
                for (int i = 0; i < t.Length; i++) o[i] = remap[t[i]];
                mesh.SetTriangles(o, s, false);
            }
            return mesh;
        }

        private static List<T> Pick<T>(T[] a, List<int> order)
        {
            var l = new List<T>(order.Count);
            foreach (int i in order) l.Add(a[i]);
            return l;
        }

        private static void Cluster(Vector3[] v, Vector3[] n, List<Vector2> uv, List<Vector2> uv3, int[][] tris, Vector3 origin, float cell,
            out int[] map, out int[][] outTris, out int count)
        {
            var rep = new Dictionary<(int, int, int, int, int, int), int>();
            map = new int[v.Length];
            bool hasN = n != null && n.Length == v.Length, hasUv = uv.Count == v.Length;
            for (int i = 0; i < v.Length; i++)
            {
                Vector3 p = (v[i] - origin) / cell;
                int dir = 0;
                if (hasN)
                {
                    Vector3 a = n[i];
                    float ax = Mathf.Abs(a.x), ay = Mathf.Abs(a.y), az = Mathf.Abs(a.z);
                    dir = ax > ay && ax > az ? (a.x > 0 ? 0 : 1) : ay > az ? (a.y > 0 ? 2 : 3) : (a.z > 0 ? 4 : 5);
                }
                int uvc = hasUv ? Mathf.FloorToInt(uv[i].x * 48f) * 1000 + Mathf.FloorToInt(uv[i].y * 48f) : 0;
                int slc = uv3.Count == v.Length ? Mathf.RoundToInt(uv3[i].x) * 2 + Mathf.RoundToInt(uv3[i].y) : 0; // never weld across array slices
                var key = (Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y), Mathf.FloorToInt(p.z), dir, uvc, slc);
                if (!rep.TryGetValue(key, out int r)) { r = i; rep[key] = i; }
                map[i] = r;
            }
            outTris = new int[tris.Length][];
            count = 0;
            var seen = new HashSet<(int, int, int)>();
            for (int s = 0; s < tris.Length; s++)
            {
                var o = new List<int>(tris[s].Length);
                int[] t = tris[s];
                for (int i = 0; i + 2 < t.Length; i += 3)
                {
                    int a = map[t[i]], b = map[t[i + 1]], c = map[t[i + 2]];
                    if (a == b || b == c || a == c) continue;
                    int m0 = Mathf.Min(a, Mathf.Min(b, c));
                    (int, int, int) k = m0 == a ? (a, b, c) : m0 == b ? (b, c, a) : (c, a, b);
                    if (!seen.Add(k)) continue;
                    o.Add(a); o.Add(b); o.Add(c);
                }
                outTris[s] = o.ToArray();
                count += o.Count / 3;
            }
        }
    }
}
#endif

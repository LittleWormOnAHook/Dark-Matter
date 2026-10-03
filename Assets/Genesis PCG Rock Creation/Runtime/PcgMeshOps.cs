using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GenesisPCG.RockCreation
{
    /// <summary>Bend + taper along a piece's long (frame y) axis. Default = no deformation.</summary>
    [Serializable]
    public struct PcgPieceDeform
    {
        [Tooltip("Cross-section scale at the tip (1 = none, 0.2 = pointy).")]
        public float tipScale;
        [Tooltip("Taper curve exponent (>1 keeps the body full and sharpens only near the tip).")]
        public float taperPower;
        [Tooltip("Total bend angle over the length (degrees).")]
        public float bendDegrees;
        [Tooltip("Frame-space horizontal direction (x/z) the tip bends toward.")]
        public Vector3 bendDirection;
        [Tooltip("Taper toward the bottom instead of the top (inverted taper, e.g. floating monoliths).")]
        public bool invert;

        public static PcgPieceDeform None => new PcgPieceDeform { tipScale = 1f, taperPower = 1f };
        public bool IsNone => Mathf.Abs(tipScale - 1f) < 1e-4f && Mathf.Abs(bendDegrees) < 1e-3f;
    }

    /// <summary>One placed piece: mesh + frame-space scale/deform + pose in formation (combiner local) space, optional plane cuts.</summary>
    public sealed class PcgPlacedPart
    {
        public Mesh mesh;
        public Material[] materials;
        public DmRockPieceInfo info;           // null = raw matrix part (captured base models)
        public Matrix4x4 rawMatrix = Matrix4x4.identity;
        public Vector3 scale = Vector3.one;    // frame space (x mid, y long, z short)
        public PcgPieceDeform deform = PcgPieceDeform.None;
        public Quaternion rotation = Quaternion.identity; // frame -> formation
        public Vector3 position;
        public readonly List<Plane> cuts = new List<Plane>();
        public int pieceIndex;
        public bool isBase;
        public string role = "";
        public int placeMode = -1;             // PcgGroundSettle.Mode*: -1 fixed/raw, 0 ground, 1 rest, 2 free
        public float sink;                     // ground: fraction of the height pushed below the contact

        private Quaternion m_frameInv;
        private Vector3 m_center;
        private float m_yMin, m_len;
        private bool m_prepared;

        /// <summary>Drops cached frame data (call after changing info / scale / deform).</summary>
        public void Invalidate() { m_prepared = false; }

        public void Prepare()
        {
            if (info != null)
            {
                m_frameInv = Quaternion.Inverse(info.frame);
                m_center = info.center;
                m_yMin = -info.size.y * 0.5f * scale.y;
                m_len = Mathf.Max(1e-4f, info.size.y * scale.y);
            }
            m_prepared = true;
        }

        public Vector3 TransformPoint(Vector3 v)
        {
            if (!m_prepared) Prepare();
            if (info == null) return rawMatrix.MultiplyPoint3x4(v);
            Vector3 f = m_frameInv * (v - m_center);
            f = Vector3.Scale(f, scale);
            if (!deform.IsNone) f = Deform(f, out _);
            return rotation * f + position;
        }

        /// <summary>Point in the part's scaled frame (x mid, y long, z short, centred) to formation space, deform included.</summary>
        public Vector3 FramePoint(Vector3 f)
        {
            if (!m_prepared) Prepare();
            if (info != null && !deform.IsNone) f = Deform(f, out _);
            return rotation * f + position;
        }

        public void TransformVertex(Vector3 v, Vector3 n, Vector4 t, out Vector3 ov, out Vector3 on, out Vector4 ot)
        {
            if (!m_prepared) Prepare();
            if (info == null)
            {
                ov = rawMatrix.MultiplyPoint3x4(v);
                Matrix4x4 nm = rawMatrix.inverse.transpose;
                on = nm.MultiplyVector(n).normalized;
                Vector3 tv = rawMatrix.MultiplyVector(new Vector3(t.x, t.y, t.z)).normalized;
                ot = new Vector4(tv.x, tv.y, tv.z, rawMatrix.determinant < 0f ? -t.w : t.w);
                return;
            }
            Vector3 f = m_frameInv * (v - m_center);
            f = Vector3.Scale(f, scale);
            Vector3 fn = m_frameInv * n;
            fn = new Vector3(fn.x / scale.x, fn.y / scale.y, fn.z / scale.z);
            Vector3 ft = Vector3.Scale(m_frameInv * new Vector3(t.x, t.y, t.z), scale);
            if (!deform.IsNone)
            {
                float taper;
                Quaternion r;
                f = Deform(f, out r, out taper);
                fn = new Vector3(fn.x / taper, fn.y, fn.z / taper);
                fn = r * fn;
                ft = r * new Vector3(ft.x * taper, ft.y, ft.z * taper);
            }
            ov = rotation * f + position;
            on = (rotation * fn).normalized;
            Vector3 w = (rotation * ft).normalized;
            ot = new Vector4(w.x, w.y, w.z, t.w);
        }

        private Vector3 Deform(Vector3 f, out float taper) => Deform(f, out _, out taper);

        private Vector3 Deform(Vector3 f, out Quaternion r, out float taper)
        {
            float t = Mathf.Clamp01((f.y - m_yMin) / m_len);
            float tt = deform.invert ? 1f - t : t;
            taper = Mathf.Lerp(1f, Mathf.Max(0.02f, deform.tipScale), Mathf.Pow(tt, Mathf.Max(0.1f, deform.taperPower)));
            Vector3 cross = new Vector3(f.x * taper, 0f, f.z * taper);
            float s = f.y - m_yMin; // arc length from the base
            Vector3 dir = new Vector3(deform.bendDirection.x, 0f, deform.bendDirection.z);
            float theta = deform.bendDegrees * Mathf.Deg2Rad;
            if (Mathf.Abs(theta) < 1e-5f || dir.sqrMagnitude < 1e-6f)
            {
                r = Quaternion.identity;
                return new Vector3(cross.x, f.y, cross.z);
            }
            dir.Normalize();
            float k = theta / m_len;
            float a = k * s;
            Vector3 centre = dir * ((1f - Mathf.Cos(a)) / k) + Vector3.up * (Mathf.Sin(a) / k + m_yMin);
            r = Quaternion.AngleAxis(a * Mathf.Rad2Deg, Vector3.Cross(Vector3.up, dir));
            return centre + r * cross;
        }
    }

    /// <summary>Mesh helpers for the rock assembler: cached vertex data, combine with deform, plane cut with cap faces.</summary>
    public static class PcgMeshOps
    {
        public sealed class Data
        {
            public Vector3[] v, n;
            public Vector4[] t;
            public Vector2[] uv;
            public int[][] tris;
            public Bounds bounds;
        }

        private static readonly Dictionary<Mesh, Data> s_cache = new Dictionary<Mesh, Data>();

        public static Data Get(Mesh m)
        {
            if (m == null) return null;
            if (s_cache.TryGetValue(m, out Data d) && d.v.Length == m.vertexCount) return d;
            d = new Data { v = m.vertices, n = m.normals, t = m.tangents, uv = m.uv, bounds = m.bounds };
            int vc = d.v.Length;
            if (d.n == null || d.n.Length != vc) { m.RecalculateNormals(); d.n = m.normals; }
            if (d.t == null || d.t.Length != vc) { d.t = new Vector4[vc]; for (int i = 0; i < vc; i++) d.t[i] = new Vector4(1, 0, 0, 1); }
            if (d.uv == null || d.uv.Length != vc) d.uv = new Vector2[vc];
            d.tris = new int[m.subMeshCount][];
            for (int s = 0; s < m.subMeshCount; s++)
                d.tris[s] = m.GetTopology(s) == MeshTopology.Triangles ? m.GetTriangles(s) : new int[0];
            s_cache[m] = d;
            return d;
        }

        public static void ClearCache() => s_cache.Clear();

        /// <summary>Source of a combined sub-mesh (one per part sub-mesh).</summary>
        public struct Source
        {
            public Mesh mesh; public int submesh; public Material material; public int pieceIndex; public bool isBase;
            public int vertexStart, vertexCount;
        }

        public sealed class Stats { public int cutsApplied, cutsSkipped, capTriangles; }

        /// <summary>Optional per-part mesh substitution (e.g. pre-decimated small debris). Returns null to keep the source.</summary>
        public static Func<Mesh, float, Mesh> LowPolyProvider;

        /// <summary>Combines parts into <paramref name="target"/>: one sub-mesh per part sub-mesh, positions in formation space.</summary>
        public static void Combine(Mesh target, IList<PcgPlacedPart> parts, Material overrideMat, List<Material> outMats, List<Source> outSources, Stats stats)
        {
            var V = new List<Vector3>(); var N = new List<Vector3>(); var T = new List<Vector4>(); var U = new List<Vector2>();
            var subTris = new List<List<int>>();
            var map = new Dictionary<int, int>();
            var lv = new List<Vector3>(); var ln = new List<Vector3>(); var lt = new List<Vector4>(); var lu = new List<Vector2>(); var ltri = new List<int>();
            foreach (PcgPlacedPart p in parts)
            {
                if (p == null || p.mesh == null) continue;
                p.Prepare();
                Data d = Get(p.mesh);
                if (d == null || d.v.Length == 0) continue;
                bool flip = p.info == null && p.rawMatrix.determinant < 0f;
                for (int s = 0; s < d.tris.Length; s++)
                {
                    int[] tr = d.tris[s];
                    if (tr.Length == 0) continue;
                    map.Clear(); lv.Clear(); ln.Clear(); lt.Clear(); lu.Clear(); ltri.Clear();
                    for (int i = 0; i < tr.Length; i++)
                    {
                        int vi = tr[i];
                        if (!map.TryGetValue(vi, out int li))
                        {
                            li = lv.Count;
                            map[vi] = li;
                            p.TransformVertex(d.v[vi], d.n[vi], d.t[vi], out Vector3 ov, out Vector3 on, out Vector4 ot);
                            lv.Add(ov); ln.Add(on); lt.Add(ot); lu.Add(d.uv[vi]);
                        }
                        ltri.Add(li);
                    }
                    if (flip)
                        for (int i = 0; i + 2 < ltri.Count; i += 3) { int x = ltri[i + 1]; ltri[i + 1] = ltri[i + 2]; ltri[i + 2] = x; }
                    if (p.cuts.Count > 0)
                    {
                        bool anyCut = false;
                        foreach (Plane pl in p.cuts)
                        {
                            int capsBefore = ltri.Count;
                            if (CutAndCap(lv, ln, lt, lu, ltri, pl, out int caps)) { anyCut = true; if (stats != null) { stats.cutsApplied++; stats.capTriangles += caps; } }
                            else if (stats != null) stats.cutsSkipped++;
                        }
                        if (anyCut) DropCutFragments(lv, ln, lt, lu, ltri);
                    }
                    if (ltri.Count == 0) continue;
                    int start = V.Count;
                    V.AddRange(lv); N.AddRange(ln); T.AddRange(lt); U.AddRange(lu);
                    var idx = new List<int>(ltri.Count);
                    for (int i = 0; i < ltri.Count; i++) idx.Add(ltri[i] + start);
                    subTris.Add(idx);
                    Material mat = p.materials != null && p.materials.Length > 0 ? p.materials[Mathf.Min(s, p.materials.Length - 1)] : null;
                    outMats.Add(overrideMat != null ? overrideMat : mat);
                    outSources.Add(new Source { mesh = p.mesh, submesh = s, material = mat, pieceIndex = p.pieceIndex, isBase = p.isBase, vertexStart = start, vertexCount = lv.Count });
                }
            }
            target.Clear();
            target.indexFormat = IndexFormat.UInt32;
            target.SetVertices(V);
            target.SetNormals(N);
            target.SetTangents(T);
            target.SetUVs(0, U);
            target.subMeshCount = subTris.Count;
            for (int s = 0; s < subTris.Count; s++) target.SetTriangles(subTris[s], s, false);
            target.RecalculateBounds();
        }

        // ------------------------------------------------------------------------------------------------
        // Plane cut with cap faces. Removes the positive side of the plane (plane.GetDistanceToPoint > 0).

        /// <summary>
        /// Plane cuts through a concave piece can leave small disconnected slivers (e.g. a 21-vertex chip floating next to the
        /// body). Drops connected fragments (welded by position) under 4% of the largest fragment's vertices AND under 20% of its
        /// size, then compacts the vertex lists.
        /// </summary>
        public static void DropCutFragments(List<Vector3> v, List<Vector3> n, List<Vector4> t, List<Vector2> uv, List<int> tri)
        {
            int vc = v.Count;
            if (vc == 0 || tri.Count == 0) return;
            var par = new int[vc];
            for (int i = 0; i < vc; i++) par[i] = i;
            int Find(int x) { while (par[x] != x) { par[x] = par[par[x]]; x = par[x]; } return x; }
            var weld = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < vc; i++)
            {
                var k = Vector3Int.RoundToInt(v[i] * 1000f);
                if (weld.TryGetValue(k, out int j)) par[Find(i)] = Find(j); else weld[k] = i;
            }
            for (int i = 0; i + 2 < tri.Count; i += 3)
            {
                int a = Find(tri[i]);
                par[Find(tri[i + 1])] = a;
                par[Find(tri[i + 2])] = a;
            }
            var count = new Dictionary<int, int>();
            var bounds = new Dictionary<int, Bounds>();
            for (int i = 0; i + 2 < tri.Count; i += 3)
                for (int k = 0; k < 3; k++)
                {
                    int vi = tri[i + k], r = Find(vi);
                    count[r] = count.TryGetValue(r, out int c) ? c + 1 : 1;
                    if (bounds.TryGetValue(r, out Bounds b)) { b.Encapsulate(v[vi]); bounds[r] = b; } else bounds[r] = new Bounds(v[vi], Vector3.zero);
                }
            if (count.Count < 2) return;
            int big = -1, bigN = 0;
            foreach (var kv in count) if (kv.Value > bigN) { bigN = kv.Value; big = kv.Key; }
            float bigSize = bounds[big].size.magnitude;
            var drop = new HashSet<int>();
            foreach (var kv in count)
                if (kv.Key != big && kv.Value < bigN * 0.04f && bounds[kv.Key].size.magnitude < bigSize * 0.2f) drop.Add(kv.Key);
            if (drop.Count == 0) return;
            var keepTri = new List<int>(tri.Count);
            for (int i = 0; i + 2 < tri.Count; i += 3)
                if (!drop.Contains(Find(tri[i]))) { keepTri.Add(tri[i]); keepTri.Add(tri[i + 1]); keepTri.Add(tri[i + 2]); }
            var remap = new int[vc];
            for (int i = 0; i < vc; i++) remap[i] = -1;
            var nv = new List<Vector3>(); var nn = new List<Vector3>(); var nt = new List<Vector4>(); var nu = new List<Vector2>();
            for (int i = 0; i < keepTri.Count; i++)
            {
                int vi = keepTri[i];
                if (remap[vi] < 0)
                {
                    remap[vi] = nv.Count;
                    nv.Add(v[vi]); nn.Add(n[vi]); nt.Add(t[vi]); nu.Add(uv[vi]);
                }
                keepTri[i] = remap[vi];
            }
            v.Clear(); v.AddRange(nv); n.Clear(); n.AddRange(nn); t.Clear(); t.AddRange(nt); uv.Clear(); uv.AddRange(nu);
            tri.Clear(); tri.AddRange(keepTri);
        }

        public static bool CutAndCap(List<Vector3> v, List<Vector3> n, List<Vector4> t, List<Vector2> uv, List<int> tri, Plane plane, out int capTris)
        {
            capTris = 0;
            int vc = v.Count;
            var d = new float[vc];
            bool anyPos = false, anyNeg = false;
            for (int i = 0; i < vc; i++)
            {
                d[i] = plane.GetDistanceToPoint(v[i]);
                if (d[i] > 0f) anyPos = true; else anyNeg = true;
            }
            if (!anyPos || !anyNeg) return false; // nothing to cut / everything removed: leave the piece alone

            var cv = new List<Vector3>(v); var cn = new List<Vector3>(n); var ct = new List<Vector4>(t); var cu = new List<Vector2>(uv);
            var outTri = new List<int>(tri.Count);
            var edgeVert = new Dictionary<long, int>();
            var segA = new List<int>(); var segB = new List<int>();
            var poly = new List<int>(4);
            var newPts = new List<int>(2);

            for (int i = 0; i + 2 < tri.Count; i += 3)
            {
                int a = tri[i], b = tri[i + 1], c = tri[i + 2];
                bool pa = d[a] > 0f, pb = d[b] > 0f, pc = d[c] > 0f;
                if (!pa && !pb && !pc) { outTri.Add(a); outTri.Add(b); outTri.Add(c); continue; }
                if (pa && pb && pc) continue;
                poly.Clear(); newPts.Clear();
                Clip(a, b); Clip(b, c); Clip(c, a);
                for (int k = 1; k + 1 < poly.Count; k++) { outTri.Add(poly[0]); outTri.Add(poly[k]); outTri.Add(poly[k + 1]); }
                if (newPts.Count == 2) { segA.Add(newPts[0]); segB.Add(newPts[1]); }
            }

            void Clip(int p, int q)
            {
                bool pIn = d[p] <= 0f, qIn = d[q] <= 0f;
                if (pIn) poly.Add(p);
                if (pIn != qIn)
                {
                    int x = Split(p, q);
                    poly.Add(x);
                    newPts.Add(x);
                }
            }

            int Split(int p, int q)
            {
                long key = p < q ? ((long)p << 32) | (uint)q : ((long)q << 32) | (uint)p;
                if (edgeVert.TryGetValue(key, out int x)) return x;
                float f = d[p] / (d[p] - d[q]);
                x = cv.Count;
                cv.Add(Vector3.Lerp(v[p], v[q], f));
                cn.Add(Vector3.Lerp(n[p], n[q], f).normalized);
                Vector4 tt = Vector4.Lerp(t[p], t[q], f); tt.w = t[p].w;
                ct.Add(tt);
                cu.Add(Vector2.Lerp(uv[p], uv[q], f));
                edgeVert[key] = x;
                return x;
            }

            if (segA.Count < 3) return false;

            // Loops (welded by position: UV seams split the same edge into several source vertices).
            var nodeOf = new Dictionary<Vector3Int, int>();
            var nodePos = new List<Vector3>();
            int Node(int vi)
            {
                Vector3 p = cv[vi];
                var k = new Vector3Int(Mathf.RoundToInt(p.x * 5000f), Mathf.RoundToInt(p.y * 5000f), Mathf.RoundToInt(p.z * 5000f));
                if (!nodeOf.TryGetValue(k, out int id)) { id = nodePos.Count; nodeOf[k] = id; nodePos.Add(p); }
                return id;
            }
            var adj = new Dictionary<int, List<int>>();
            for (int s = 0; s < segA.Count; s++)
            {
                int x = Node(segA[s]), y = Node(segB[s]);
                if (x == y) continue;
                if (!adj.TryGetValue(x, out var lx)) adj[x] = lx = new List<int>();
                if (!adj.TryGetValue(y, out var ly)) adj[y] = ly = new List<int>();
                if (!lx.Contains(y)) lx.Add(y);
                if (!ly.Contains(x)) ly.Add(x);
            }
            var loops = new List<List<int>>();
            var used = new HashSet<long>();
            foreach (var kv in adj)
            {
                foreach (int nb in kv.Value)
                {
                    long ek = EdgeKey(kv.Key, nb);
                    if (used.Contains(ek)) continue;
                    var loop = new List<int> { kv.Key };
                    used.Add(ek);
                    int prev = kv.Key, cur = nb;
                    bool closed = false;
                    for (int guard = 0; guard < 100000; guard++)
                    {
                        if (cur == kv.Key) { closed = true; break; }
                        loop.Add(cur);
                        int next = -1;
                        foreach (int cand in adj[cur])
                        {
                            if (cand == prev) continue;
                            long ck = EdgeKey(cur, cand);
                            if (used.Contains(ck)) continue;
                            next = cand; used.Add(ck); break;
                        }
                        if (next < 0) break;
                        prev = cur; cur = next;
                    }
                    if (closed && loop.Count >= 3) loops.Add(loop);
                    else if (loop.Count > 6) return false; // open cross-section: piece mesh not closed here, skip the cut
                }
            }
            if (loops.Count == 0) return false;

            // Cap faces: flat, own vertices (hard edge), planar UVs inside the piece's own texture space.
            Vector3 nrm = plane.normal;
            Vector3 ax = Vector3.Cross(nrm, Mathf.Abs(nrm.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            Vector3 ay = Vector3.Cross(nrm, ax);
            var tmpUv = new Vector2(0.5f, 0.5f);
            foreach (List<int> loop in loops)
            {
                var pts2 = new List<Vector2>(loop.Count);
                Vector2 mn = new Vector2(float.MaxValue, float.MaxValue), mx = new Vector2(float.MinValue, float.MinValue);
                foreach (int id in loop)
                {
                    Vector3 p = nodePos[id];
                    var q = new Vector2(Vector3.Dot(p, ax), Vector3.Dot(p, ay));
                    pts2.Add(q); mn = Vector2.Min(mn, q); mx = Vector2.Max(mx, q);
                }
                Vector2 mid = (mn + mx) * 0.5f;
                float uvScale = 1f / 3f; // ~3 m of rock per texture unit, kept inside 0..1
                Vector2 ext = (mx - mn) * uvScale;
                Vector2 c0 = new Vector2(Mathf.Clamp(0.5f, 0.05f + ext.x * 0.5f, 0.95f - ext.x * 0.5f), Mathf.Clamp(0.5f, 0.05f + ext.y * 0.5f, 0.95f - ext.y * 0.5f));
                float fit = Mathf.Min(1f, 0.9f / Mathf.Max(1e-4f, Mathf.Max(ext.x, ext.y)));
                int baseIdx = cv.Count;
                for (int k = 0; k < loop.Count; k++)
                {
                    cv.Add(nodePos[loop[k]]);
                    cn.Add(nrm);
                    ct.Add(new Vector4(ax.x, ax.y, ax.z, 1f));
                    Vector2 u = c0 + (pts2[k] - mid) * uvScale * fit;
                    cu.Add(new Vector2(Mathf.Clamp01(u.x), Mathf.Clamp01(u.y)));
                }
                List<int> ears = Triangulate(pts2);
                for (int k = 0; k + 2 < ears.Count; k += 3)
                {
                    int i0 = baseIdx + ears[k], i1 = baseIdx + ears[k + 1], i2 = baseIdx + ears[k + 2];
                    if (Vector3.Dot(Vector3.Cross(cv[i1] - cv[i0], cv[i2] - cv[i0]), nrm) < 0f) { int s = i1; i1 = i2; i2 = s; }
                    outTri.Add(i0); outTri.Add(i1); outTri.Add(i2);
                    capTris++;
                }
            }

            // Compact (drop vertices on the removed side).
            var remap = new int[cv.Count];
            for (int i = 0; i < remap.Length; i++) remap[i] = -1;
            v.Clear(); n.Clear(); t.Clear(); uv.Clear(); tri.Clear();
            foreach (int i in outTri)
            {
                if (remap[i] < 0) { remap[i] = v.Count; v.Add(cv[i]); n.Add(cn[i]); t.Add(ct[i]); uv.Add(cu[i]); }
                tri.Add(remap[i]);
            }
            return true;
        }

        private static long EdgeKey(int a, int b) => a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;

        /// <summary>Ear clipping (handles concave simple polygons; falls back to a fan when it gets stuck).</summary>
        public static List<int> Triangulate(List<Vector2> p)
        {
            var result = new List<int>();
            int n = p.Count;
            if (n < 3) return result;
            var idx = new List<int>(n);
            float area = 0f;
            for (int i = 0; i < n; i++) { Vector2 a = p[i], b = p[(i + 1) % n]; area += a.x * b.y - b.x * a.y; }
            if (area > 0f) for (int i = 0; i < n; i++) idx.Add(i);
            else for (int i = n - 1; i >= 0; i--) idx.Add(i);
            int guard = 0;
            while (idx.Count > 3 && guard++ < n * n)
            {
                bool clipped = false;
                for (int i = 0; i < idx.Count; i++)
                {
                    int i0 = idx[(i + idx.Count - 1) % idx.Count], i1 = idx[i], i2 = idx[(i + 1) % idx.Count];
                    Vector2 a = p[i0], b = p[i1], c = p[i2];
                    float cr = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
                    if (cr <= 1e-9f) continue;
                    bool inside = false;
                    for (int j = 0; j < idx.Count; j++)
                    {
                        int k = idx[j];
                        if (k == i0 || k == i1 || k == i2) continue;
                        if (InTri(p[k], a, b, c)) { inside = true; break; }
                    }
                    if (inside) continue;
                    result.Add(i0); result.Add(i1); result.Add(i2);
                    idx.RemoveAt(i);
                    clipped = true;
                    break;
                }
                if (!clipped) break;
            }
            if (idx.Count == 3) { result.Add(idx[0]); result.Add(idx[1]); result.Add(idx[2]); }
            else if (idx.Count > 3)
                for (int k = 1; k + 1 < idx.Count; k++) { result.Add(idx[0]); result.Add(idx[k]); result.Add(idx[k + 1]); }
            return result;
        }

        private static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }
    }
}

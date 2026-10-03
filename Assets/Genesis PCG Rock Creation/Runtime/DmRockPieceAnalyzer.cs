using System.Collections.Generic;
using UnityEngine;

namespace GenesisPCG.RockCreation
{
    /// <summary>Measures kit pieces: principal axes, class, top/bottom widths and fracture sharpness (mesh space).</summary>
    public static class DmRockPieceAnalyzer
    {
        public static Mesh PieceMesh(GameObject prefab, out Material[] materials)
        {
            materials = null;
            if (prefab == null) return null;
            MeshFilter mf = prefab.GetComponentInChildren<MeshFilter>(true);
            if (mf == null || mf.sharedMesh == null) return null;
            var mr = mf.GetComponent<MeshRenderer>();
            materials = mr != null ? mr.sharedMaterials : new Material[0];
            return mf.sharedMesh;
        }

        public static DmRockPieceInfo Analyze(GameObject prefab, DmRockKit kit)
        {
            Mesh m = PieceMesh(prefab, out _);
            if (m == null) return null;
            PcgMeshOps.Data d = PcgMeshOps.Get(m);
            Vector3[] v = d.v;
            int n = v.Length;
            if (n < 4) return null;

            Vector3 mean = Vector3.zero;
            foreach (Vector3 p in v) mean += p;
            mean /= n;
            var cov = new float[3, 3];
            foreach (Vector3 p in v)
            {
                Vector3 q = p - mean;
                cov[0, 0] += q.x * q.x; cov[0, 1] += q.x * q.y; cov[0, 2] += q.x * q.z;
                cov[1, 1] += q.y * q.y; cov[1, 2] += q.y * q.z; cov[2, 2] += q.z * q.z;
            }
            cov[1, 0] = cov[0, 1]; cov[2, 0] = cov[0, 2]; cov[2, 1] = cov[1, 2];
            Vector3[] axes = Eigen(cov);

            // Sort axes by extent (not variance): long, mid, short.
            var ext = new float[3];
            for (int a = 0; a < 3; a++) ext[a] = Extent(v, axes[a]);
            int il = 0, ish = 0;
            for (int a = 1; a < 3; a++) { if (ext[a] > ext[il]) il = a; if (ext[a] < ext[ish]) ish = a; }
            if (il == ish) ish = (il + 1) % 3;
            Vector3 longAx = axes[il], shortAx = axes[ish];

            // Keep authored verticality / bedding: snap to local up when close (pack group pieces stand upright).
            bool upright = false;
            if (Vector3.Angle(longAx, Vector3.up) < 25f || Vector3.Angle(longAx, Vector3.down) < 25f)
            {
                longAx = Vector3.up; upright = true;
                shortAx = HorizontalMinor(v, mean);
            }
            else if (Vector3.Angle(shortAx, Vector3.up) < 25f || Vector3.Angle(shortAx, Vector3.down) < 25f)
            {
                shortAx = Vector3.up;
                longAx = HorizontalMajor(v, mean);
            }
            shortAx = Vector3.ProjectOnPlane(shortAx, longAx).normalized;
            if (shortAx.sqrMagnitude < 0.5f) shortAx = Vector3.Cross(longAx, Vector3.right).normalized;
            Quaternion frame = Quaternion.LookRotation(shortAx, longAx); // frame y = long, z = short
            Quaternion inv = Quaternion.Inverse(frame);

            Vector3 mn = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), mx = -mn;
            foreach (Vector3 p in v) { Vector3 f = inv * p; mn = Vector3.Min(mn, f); mx = Vector3.Max(mx, f); }
            Vector3 size = mx - mn;
            // Re-order if snapping made x (mid) longer than y (long).
            Vector3 centerF = (mn + mx) * 0.5f;
            Vector3 center = frame * centerF;

            float topW = 0f, botW = 0f;
            {
                float tMinX = float.MaxValue, tMaxX = float.MinValue, bMinX = float.MaxValue, bMaxX = float.MinValue;
                foreach (Vector3 p in v)
                {
                    Vector3 f = inv * p;
                    float t = (f.y - mn.y) / Mathf.Max(1e-4f, size.y);
                    float r = Mathf.Max(Mathf.Abs(f.x - centerF.x), Mathf.Abs(f.z - centerF.z) * 0f);
                    if (t > 0.85f) { tMinX = Mathf.Min(tMinX, f.x); tMaxX = Mathf.Max(tMaxX, f.x); }
                    if (t < 0.15f) { bMinX = Mathf.Min(bMinX, f.x); bMaxX = Mathf.Max(bMaxX, f.x); }
                }
                topW = tMaxX > tMinX ? tMaxX - tMinX : 0f;
                botW = bMaxX > bMinX ? bMaxX - bMinX : 0f;
            }

            var info = new DmRockPieceInfo
            {
                prefab = prefab,
                frame = frame,
                center = center,
                size = size,
                longAxis = longAx,
                topWidth = topW,
                bottomWidth = botW,
                authoredUpright = upright,
                vertexCount = n,
                triangleCount = 0,
                sharpness = Sharpness(m, d),
            };
            foreach (int[] t in d.tris) info.triangleCount += t.Length / 3;
            info.pieceClass = Classify(info, kit);
            return info;
        }

        public static DmRockPieceClass Classify(DmRockPieceInfo p, DmRockKit kit)
        {
            float tallAspect = kit != null ? kit.tallAspect : 1.5f;
            float slabFlat = kit != null ? kit.slabFlatness : 0.42f;
            float small = kit != null ? kit.smallExtent : 1.45f;
            if (p.size.y < small) return DmRockPieceClass.Small;
            if (p.Aspect >= tallAspect && p.authoredUpright) return DmRockPieceClass.Tall;
            if (p.size.z / Mathf.Max(1e-4f, p.size.x) <= slabFlat) return DmRockPieceClass.Slab;
            if (p.Aspect >= tallAspect + 0.4f) return DmRockPieceClass.Tall;
            return DmRockPieceClass.Boulder;
        }

        /// <summary>Bounds-only fallback when a kit has not been analyzed.</summary>
        public static DmRockPieceInfo FromBounds(GameObject prefab, DmRockKit kit)
        {
            Mesh m = PieceMesh(prefab, out _);
            if (m == null) return null;
            Bounds b = m.bounds;
            Vector3 s = b.size;
            var info = new DmRockPieceInfo { prefab = prefab, center = b.center, vertexCount = m.vertexCount };
            if (s.y >= s.x && s.y >= s.z) { info.frame = s.x >= s.z ? Quaternion.identity : Quaternion.Euler(0, 90, 0); info.authoredUpright = true; }
            else info.frame = s.x >= s.z ? Quaternion.Euler(0, 0, 90) * Quaternion.Euler(0, 90, 0) : Quaternion.Euler(90, 0, 0);
            Quaternion inv = Quaternion.Inverse(info.frame);
            Vector3 fs = inv * s;
            info.size = new Vector3(Mathf.Abs(fs.x), Mathf.Abs(fs.y), Mathf.Abs(fs.z));
            info.longAxis = info.frame * Vector3.up;
            info.topWidth = info.bottomWidth = info.size.x * 0.7f;
            info.pieceClass = Classify(info, kit);
            return info;
        }

        private static float Extent(Vector3[] v, Vector3 ax)
        {
            float mn = float.MaxValue, mx = float.MinValue;
            foreach (Vector3 p in v) { float d = Vector3.Dot(p, ax); mn = Mathf.Min(mn, d); mx = Mathf.Max(mx, d); }
            return mx - mn;
        }

        private static Vector3 HorizontalMinor(Vector3[] v, Vector3 mean) => Vector3.Cross(Vector3.up, HorizontalMajor(v, mean)).normalized;

        private static Vector3 HorizontalMajor(Vector3[] v, Vector3 mean)
        {
            // Largest horizontal extent over 36 directions.
            float best = -1f; Vector3 bd = Vector3.right;
            for (int i = 0; i < 36; i++)
            {
                float a = i * 5f * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float e = Extent(v, dir);
                if (e > best) { best = e; bd = dir; }
            }
            return bd;
        }

        private static Vector3[] Eigen(float[,] a)
        {
            // Jacobi rotations on a symmetric 3x3.
            var m = (float[,])a.Clone();
            var V = new float[3, 3] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
            for (int sweep = 0; sweep < 32; sweep++)
            {
                float off = Mathf.Abs(m[0, 1]) + Mathf.Abs(m[0, 2]) + Mathf.Abs(m[1, 2]);
                if (off < 1e-9f) break;
                for (int p = 0; p < 2; p++)
                    for (int q = p + 1; q < 3; q++)
                    {
                        if (Mathf.Abs(m[p, q]) < 1e-12f) continue;
                        float theta = (m[q, q] - m[p, p]) / (2f * m[p, q]);
                        float t = Mathf.Sign(theta) / (Mathf.Abs(theta) + Mathf.Sqrt(theta * theta + 1f));
                        if (theta == 0f) t = 1f;
                        float c = 1f / Mathf.Sqrt(t * t + 1f), s = t * c;
                        for (int k = 0; k < 3; k++)
                        {
                            float mkp = m[k, p], mkq = m[k, q];
                            m[k, p] = c * mkp - s * mkq; m[k, q] = s * mkp + c * mkq;
                        }
                        for (int k = 0; k < 3; k++)
                        {
                            float mpk = m[p, k], mqk = m[q, k];
                            m[p, k] = c * mpk - s * mqk; m[q, k] = s * mpk + c * mqk;
                        }
                        for (int k = 0; k < 3; k++)
                        {
                            float vkp = V[k, p], vkq = V[k, q];
                            V[k, p] = c * vkp - s * vkq; V[k, q] = s * vkp + c * vkq;
                        }
                    }
            }
            return new[]
            {
                new Vector3(V[0, 0], V[1, 0], V[2, 0]).normalized,
                new Vector3(V[0, 1], V[1, 1], V[2, 1]).normalized,
                new Vector3(V[0, 2], V[1, 2], V[2, 2]).normalized,
            };
        }

        private static float Sharpness(Mesh m, PcgMeshOps.Data d)
        {
            // Weld by position, then count edges whose two faces differ by > 35 degrees.
            var weld = new Dictionary<Vector3Int, int>();
            var id = new int[d.v.Length];
            for (int i = 0; i < d.v.Length; i++)
            {
                Vector3 p = d.v[i];
                var k = new Vector3Int(Mathf.RoundToInt(p.x * 2000f), Mathf.RoundToInt(p.y * 2000f), Mathf.RoundToInt(p.z * 2000f));
                if (!weld.TryGetValue(k, out int w)) { w = weld.Count; weld[k] = w; }
                id[i] = w;
            }
            var edgeN = new Dictionary<long, Vector3>();
            int total = 0, sharp = 0;
            foreach (int[] tr in d.tris)
                for (int i = 0; i + 2 < tr.Length; i += 3)
                {
                    Vector3 a = d.v[tr[i]], b = d.v[tr[i + 1]], c = d.v[tr[i + 2]];
                    Vector3 fn = Vector3.Cross(b - a, c - a);
                    if (fn.sqrMagnitude < 1e-12f) continue;
                    fn.Normalize();
                    for (int e = 0; e < 3; e++)
                    {
                        int x = id[tr[i + e]], y = id[tr[i + (e + 1) % 3]];
                        if (x == y) continue;
                        long key = x < y ? ((long)x << 32) | (uint)y : ((long)y << 32) | (uint)x;
                        if (edgeN.TryGetValue(key, out Vector3 other))
                        {
                            total++;
                            if (Vector3.Angle(other, fn) > 35f) sharp++;
                            edgeN.Remove(key);
                        }
                        else edgeN[key] = fn;
                    }
                }
            return total > 0 ? (float)sharp / total : 0f;
        }
    }
}

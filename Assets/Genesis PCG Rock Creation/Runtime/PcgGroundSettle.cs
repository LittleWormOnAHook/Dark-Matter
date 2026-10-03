using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenesisPCG.RockCreation
{
    /// <summary>
    /// Post-assembly grounding pass (formation space, same ground function the assembler used).
    /// Loose ground pieces (scatter / ejecta styles, plus apron / rockfall / toe / debris / rubble roles of any style) are
    /// re-fitted to the ground under their own footprint: tilted toward the local slope and sunk 5-25 % of their height.
    /// Every other ground piece is lowered if it has no contact; resting pieces with nothing below them drop onto what is
    /// under them (previously settled pieces or the ground). Pieces are processed in placement order, so supporters settle
    /// before what rests on them.
    /// </summary>
    public static class PcgGroundSettle
    {
        public struct Stats
        {
            public int checkedParts, refit, tilted, lowered, dropped;
            public float maxCorrection;
            public override string ToString() =>
                $"settle: checked {checkedParts}, refit {refit} (tilted {tilted}), lowered {lowered}, dropped {dropped}, max corr {maxCorrection:0.00} m";
        }

        public const int ModeFixed = -1, ModeGround = 0, ModeRest = 1, ModeFree = 2;
        public const float MaxTiltDeg = 22f;

        private static readonly HashSet<string> LooseRoles = new HashSet<string>(StringComparer.Ordinal)
        {
            "apron", "rockfall", "toe", "debris", "ground-debris", "fallen-cap", "rubble", "anchor", "ejecta", "splinter", "scatter",
        };

        public static bool IsLooseRole(string role) => role != null && LooseRoles.Contains(role);

        public static bool IsScatterStyle(DmRockStyle st) =>
            st != null && (st.kind == DmRockStyle.StyleKind.RubbleScatter || st.kind == DmRockStyle.StyleKind.VolcanicEjecta);

        public static Stats Settle(List<PcgPlacedPart> parts, Func<float, float, float> ground, DmRockStyle style, bool looseOnly)
        {
            var stats = new Stats();
            if (parts == null || ground == null) return stats;
            bool scatter = IsScatterStyle(style);
            var cloud = new Cloud(0.2f);
            var vcache = new Dictionary<Mesh, Vector3[]>();
            var pts = new List<Vector3>(700);

            foreach (PcgPlacedPart p in parts)
            {
                if (p == null || p.mesh == null) continue;
                bool eligible = p.info != null && p.placeMode == ModeGround && (scatter || IsLooseRole(p.role));
                bool doGround = p.info != null && p.placeMode == ModeGround && !looseOnly;
                bool doRest = p.info != null && p.placeMode == ModeRest && !looseOnly;
                if (eligible || doGround || doRest)
                {
                    stats.checkedParts++;
                    Sample(p, vcache, pts);
                    if (pts.Count > 0)
                    {
                        if (eligible) Refit(p, ground, vcache, pts, ref stats);
                        else if (doGround) LowerIfFloating(p, ground, pts, ref stats);
                        else DropIfUnsupported(p, ground, cloud, pts, ref stats);
                    }
                }
                Sample(p, vcache, pts);
                cloud.AddRange(pts);
            }
            return stats;
        }

        // Loose ground piece: fit the ground plane under the footprint, tilt toward it, then contact + sink.
        private static void Refit(PcgPlacedPart p, Func<float, float, float> ground, Dictionary<Mesh, Vector3[]> vc, List<Vector3> pts, ref Stats st)
        {
            Bands(pts, out float minY, out float h);
            var bottom = BottomBand(pts, minY, h, 0.35f);
            Vector3 c = Centroid(bottom);
            float r = 0f;
            foreach (Vector3 b in bottom) r = Mathf.Max(r, new Vector2(b.x - c.x, b.z - c.z).magnitude);
            r = Mathf.Max(r, 0.15f);
            // Ground normal from a 5-point stencil over the footprint radius.
            float g0 = ground(c.x, c.z);
            float gx = (ground(c.x + r, c.z) - ground(c.x - r, c.z)) / (2f * r);
            float gz = (ground(c.x, c.z + r) - ground(c.x, c.z - r)) / (2f * r);
            Vector3 n = new Vector3(-gx, 1f, -gz).normalized;
            float slope = Vector3.Angle(Vector3.up, n);
            Vector3 before = p.position;
            if (slope > 3f)
            {
                Quaternion q = Quaternion.FromToRotation(Vector3.up, n);
                if (slope > MaxTiltDeg) q = Quaternion.Slerp(Quaternion.identity, q, MaxTiltDeg / slope);
                Vector3 pivot = new Vector3(c.x, g0, c.z);
                p.rotation = q * p.rotation;
                p.position = pivot + q * (p.position - pivot);
                p.Invalidate();
                st.tilted++;
                Sample(p, vc, pts);
                Bands(pts, out minY, out h);
                bottom = BottomBand(pts, minY, h, 0.35f);
            }
            float need = float.MinValue;
            foreach (Vector3 b in bottom) need = Mathf.Max(need, ground(b.x, b.z) - b.y);
            float sink = Mathf.Clamp(p.sink > 0f ? p.sink : 0.12f, 0.05f, 0.25f);
            float off = need - sink * h;
            p.position += new Vector3(0f, off, 0f);
            p.Invalidate();
            st.refit++;
            st.maxCorrection = Mathf.Max(st.maxCorrection, (p.position - before).magnitude);
        }

        // Structural ground piece: only lowered when its whole bottom band hangs above the ground.
        private static void LowerIfFloating(PcgPlacedPart p, Func<float, float, float> ground, List<Vector3> pts, ref Stats st)
        {
            Bands(pts, out float minY, out float h);
            float gap = float.MaxValue;
            foreach (Vector3 b in BottomBand(pts, minY, h, 0.2f)) gap = Mathf.Min(gap, b.y - ground(b.x, b.z));
            if (gap <= 0.03f || gap == float.MaxValue) return;
            float sink = Mathf.Clamp(p.sink, 0.03f, 0.25f);
            float d = gap + sink * h * 0.5f;
            p.position -= new Vector3(0f, d, 0f);
            p.Invalidate();
            st.lowered++;
            st.maxCorrection = Mathf.Max(st.maxCorrection, d);
        }

        // Resting piece: drop it if none of its low points has support (settled pieces or ground) close below.
        private static void DropIfUnsupported(PcgPlacedPart p, Func<float, float, float> ground, Cloud cloud, List<Vector3> pts, ref Stats st)
        {
            Bands(pts, out float minY, out float h);
            float drop = float.MaxValue;
            foreach (Vector3 b in BottomBand(pts, minY, h, 0.15f))
            {
                float s = Mathf.Max(ground(b.x, b.z), cloud.HighestBelow(b, 0.03f));
                drop = Mathf.Min(drop, b.y - s);
            }
            if (drop <= 0.08f || drop == float.MaxValue) return;
            float d = drop + 0.02f;
            p.position -= new Vector3(0f, d, 0f);
            p.Invalidate();
            st.dropped++;
            st.maxCorrection = Mathf.Max(st.maxCorrection, d);
        }

        // ------------------------------------------------------------------------------------------------

        private static void Sample(PcgPlacedPart p, Dictionary<Mesh, Vector3[]> vc, List<Vector3> pts)
        {
            pts.Clear();
            if (!vc.TryGetValue(p.mesh, out Vector3[] v))
            {
                v = p.mesh.isReadable ? p.mesh.vertices : new Vector3[0];
                vc[p.mesh] = v;
            }
            int stride = Mathf.Max(1, v.Length / 1500);
            for (int i = 0; i < v.Length; i += stride) pts.Add(p.TransformPoint(v[i]));
        }

        private static void Bands(List<Vector3> pts, out float minY, out float h)
        {
            minY = float.MaxValue; float maxY = float.MinValue;
            foreach (Vector3 q in pts) { minY = Mathf.Min(minY, q.y); maxY = Mathf.Max(maxY, q.y); }
            h = Mathf.Max(0.01f, maxY - minY);
        }

        private static List<Vector3> BottomBand(List<Vector3> pts, float minY, float h, float frac)
        {
            var r = new List<Vector3>();
            float cut = minY + Mathf.Max(0.03f, frac * h);
            foreach (Vector3 q in pts) if (q.y <= cut) r.Add(q);
            return r;
        }

        private static Vector3 Centroid(List<Vector3> pts)
        {
            Vector3 c = Vector3.zero;
            foreach (Vector3 q in pts) c += q;
            return pts.Count > 0 ? c / pts.Count : c;
        }

        private sealed class Cloud
        {
            private readonly float m_cell;
            private readonly Dictionary<long, List<Vector3>> m_map = new Dictionary<long, List<Vector3>>();
            public Cloud(float cell) { m_cell = cell; }
            private long Key(int i, int j) => ((long)i << 32) ^ (uint)j;
            public void AddRange(List<Vector3> pts)
            {
                foreach (Vector3 q in pts)
                {
                    long k = Key(Mathf.FloorToInt(q.x / m_cell), Mathf.FloorToInt(q.z / m_cell));
                    if (!m_map.TryGetValue(k, out List<Vector3> l)) m_map[k] = l = new List<Vector3>();
                    l.Add(q);
                }
            }
            /// <summary>Highest stored point within one cell (xz) of b and below b.y + tol; -inf when none.</summary>
            public float HighestBelow(Vector3 b, float tol)
            {
                float best = float.NegativeInfinity, r2 = m_cell * m_cell;
                int ci = Mathf.FloorToInt(b.x / m_cell), cj = Mathf.FloorToInt(b.z / m_cell);
                for (int i = ci - 1; i <= ci + 1; i++)
                    for (int j = cj - 1; j <= cj + 1; j++)
                        if (m_map.TryGetValue(Key(i, j), out List<Vector3> l))
                            foreach (Vector3 q in l)
                            {
                                if (q.y > b.y + tol || q.y <= best) continue;
                                float dx = q.x - b.x, dz = q.z - b.z;
                                if (dx * dx + dz * dz <= r2) best = q.y;
                            }
                return best;
            }
        }
    }
}

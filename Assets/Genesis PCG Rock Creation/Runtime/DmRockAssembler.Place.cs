using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenesisPCG.RockCreation
{
    public sealed partial class DmRockAssembler
    {
        // Pieces
        private static bool InMask(DmRockPieceInfo p, DmRockPieceClassMask m) => (m & p.Mask) != 0;
        private float m_meanSharp = -1f;

        /// <summary>
        /// Pack "group" prefabs are several rocks merged into one prefab; they read as stacked boulders when used as a
        /// stem / ridge segment. Name rule from the kit scan ("group"), plus a vertex-count fallback for other packs.
        /// </summary>
        private static bool IsCluster(DmRockPieceInfo p) =>
            p != null && p.prefab != null && (p.prefab.name.IndexOf("group", StringComparison.OrdinalIgnoreCase) >= 0 || p.vertexCount > 1500);

        /// <summary>Pick restricted by a filter; falls back to the plain pick when nothing passes.</summary>
        private DmRockPieceInfo PickWhere(Func<DmRockPieceInfo, bool> filter, DmRockPieceClassMask mask, float targetLong, float targetAspect = 0f)
        {
            double total = 0;
            var w = new double[m_pieces.Count];
            for (int i = 0; i < m_pieces.Count; i++)
            {
                DmRockPieceInfo p = m_pieces[i];
                if (p == null || p.prefab == null || !InMask(p, mask) || !filter(p)) continue;
                double fit = Math.Abs(Math.Log(Mathf.Max(0.05f, targetLong) / Mathf.Max(0.05f, p.Long)));
                double wt = Math.Exp(-fit * 0.6);
                if (targetAspect > 0f) wt *= Math.Exp(-Math.Abs(Math.Log(targetAspect / Mathf.Max(0.1f, p.Aspect))) * 2.0);
                w[i] = wt; total += wt;
            }
            if (total <= 0) return Pick(mask, targetLong, -1f, targetAspect);
            double r = m_rng.NextDouble() * total;
            for (int i = 0; i < w.Length; i++)
            {
                r -= w[i];
                if (r <= 0 && w[i] > 0) { m_lastPick = m_pieces[i]; return m_pieces[i]; }
            }
            for (int i = w.Length - 1; i >= 0; i--) if (w[i] > 0) return m_pieces[i];
            return Pick(mask, targetLong, -1f, targetAspect);
        }

        private DmRockPieceInfo Pick(DmRockPieceClassMask mask, float targetLong, float preferSharp = -1f, float targetAspect = 0f)
        {
            if (preferSharp < 0f) preferSharp = m_style.preferSharp;
            if (m_meanSharp < 0f)
            {
                float s = 0f; foreach (DmRockPieceInfo p in m_pieces) s += p.sharpness;
                m_meanSharp = m_pieces.Count > 0 ? s / m_pieces.Count : 0f;
            }
            double total = 0;
            var w = new double[m_pieces.Count];
            for (int pass = 0; pass < 2 && total <= 0; pass++)
            {
                for (int i = 0; i < m_pieces.Count; i++)
                {
                    DmRockPieceInfo p = m_pieces[i];
                    w[i] = 0;
                    if (p == null || p.prefab == null) continue;
                    if (pass == 0 && !InMask(p, mask)) continue;
                    double fit = Math.Abs(Math.Log(Mathf.Max(0.05f, targetLong) / Mathf.Max(0.05f, p.Long)));
                    double wt = Math.Exp(-fit * 1.1);
                    if (targetAspect > 0f) wt *= Math.Exp(-Math.Abs(Math.Log(targetAspect / Mathf.Max(0.1f, p.Aspect))) * 2.0);
                    if (preferSharp > 0f) wt *= Math.Exp((p.sharpness - m_meanSharp) * 12.0 * preferSharp);
                    if (p == m_lastPick) wt *= 0.25;
                    w[i] = wt;
                    total += wt;
                }
            }
            if (total <= 0) return m_pieces.Count > 0 ? m_pieces[0] : null;
            double r = m_rng.NextDouble() * total;
            for (int i = 0; i < w.Length; i++)
            {
                r -= w[i];
                if (r <= 0 && w[i] > 0) { m_lastPick = m_pieces[i]; return m_pieces[i]; }
            }
            m_lastPick = m_pieces[m_pieces.Count - 1];
            return m_lastPick;
        }

        private static Vector3 Uniform(DmRockPieceInfo p, float targetLong) => Vector3.one * Mathf.Clamp(targetLong / Mathf.Max(0.05f, p.Long), 0.08f, 4f);

        private static PcgPlacedPart NewPart(DmRockPieceInfo info, Vector3 scale, Quaternion rot, PcgPieceDeform deform)
        {
            Mesh m = DmRockPieceAnalyzer.PieceMesh(info.prefab, out Material[] mats);
            var part = new PcgPlacedPart { mesh = m, materials = mats, info = info, scale = scale, rotation = rot, deform = deform };
            part.Prepare();
            return part;
        }

        private static readonly Dictionary<Mesh, Vector3[]> s_samples = new Dictionary<Mesh, Vector3[]>();

        private static Vector3[] Samples(Mesh m)
        {
            if (m == null) return new Vector3[0];
            if (s_samples.TryGetValue(m, out Vector3[] s)) return s;
            PcgMeshOps.Data d = PcgMeshOps.Get(m);
            var list = new List<Vector3>(d.v);
            foreach (int[] t in d.tris)
                for (int i = 0; i + 2 < t.Length; i += 3)
                    list.Add((d.v[t[i]] + d.v[t[i + 1]] + d.v[t[i + 2]]) / 3f);
            s = list.ToArray();
            s_samples[m] = s;
            return s;
        }

        // Grid
        private void InitGrid(float extent)
        {
            m_cell = extent > 18f ? 0.3f : 0.2f;
            m_n = Mathf.Clamp(Mathf.CeilToInt(extent * 2f / m_cell), 16, 420);
            m_half = m_n * m_cell * 0.5f;
            int c = m_n * m_n;
            m_top = new float[c]; m_ground = new float[c]; m_owner = new int[c];
            m_tBot = new float[c]; m_tTop = new float[c]; m_tStamp = new int[c];
            for (int i = 0; i < c; i++) { m_top[i] = float.NegativeInfinity; m_ground[i] = float.NaN; m_owner[i] = -1; }
        }

        private int Cell(float x, float z)
        {
            int i = Mathf.FloorToInt((x + m_half) / m_cell), j = Mathf.FloorToInt((z + m_half) / m_cell);
            if (i < 0 || j < 0 || i >= m_n || j >= m_n) return -1;
            return j * m_n + i;
        }

        private Vector2 CellCenter(int c) => new Vector2((c % m_n + 0.5f) * m_cell - m_half, (c / m_n + 0.5f) * m_cell - m_half);

        private float Ground(int c)
        {
            float g = m_ground[c];
            if (float.IsNaN(g)) { Vector2 p = CellCenter(c); g = m_ground[c] = m_groundFn(p.x, p.y); }
            return g;
        }

        private float GroundAt(float x, float z) { int c = Cell(x, z); return c >= 0 ? Ground(c) : m_groundFn(x, z); }
        private float GroundAt(Vector2 p) => GroundAt(p.x, p.y);
        private float Support(int c) => Mathf.Max(Ground(c), m_top[c]);
        private float TopAt(Vector2 p) { int c = Cell(p.x, p.y); return c >= 0 ? Support(c) : m_groundFn(p.x, p.y); }

        private bool Occupied(Vector2 p, float minAbove = 0.15f)
        {
            int c = Cell(p.x, p.y);
            return c >= 0 && m_top[c] > Ground(c) + minAbove;
        }

        /// <summary>Distance from <paramref name="center"/> to the outer edge of the formation's footprint along <paramref name="dir"/>.</summary>
        private float FootRadius(Vector2 center, Vector2 dir, float max)
        {
            float last = 0f;
            for (float d = 0f; d <= max; d += m_cell)
                if (Occupied(center + dir * d)) last = d;
            return last;
        }

        // Placement core
        private Placed TryPlace(Req q)
        {
            S.attempts++;
            m_lastReject = null; m_lastKeepOut = null;
            if (q.info == null || q.info.prefab == null) { m_lastReject = "nopiece"; return null; }
            Vector3 scale = q.scale;

            if (q.fitHeight > 0f)
            {
                PcgPlacedPart probe = NewPart(q.info, scale, q.rot, q.deform);
                MinMaxY(probe, out float y0, out float y1);
                float vis = (y1 - y0) * (q.mode == Mode.Ground ? 1f - q.sink : 1f);
                float k = q.fitHeight / Mathf.Max(0.02f, vis);
                scale *= Mathf.Clamp(k, 0.05f, 6f);
            }

            for (int iter = 0; iter < 3; iter++)
            {
                PcgPlacedPart part = NewPart(q.info, scale, q.rot, q.deform);
                Vector3[] src = Samples(part.mesh);
                var pts = new Vector3[src.Length];
                Vector3 com = Vector3.zero;
                Vector3 mn = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), mx = -mn;
                for (int i = 0; i < src.Length; i++)
                {
                    Vector3 p = part.TransformPoint(src[i]);
                    pts[i] = p; com += p;
                    mn = Vector3.Min(mn, p); mx = Vector3.Max(mx, p);
                }
                if (pts.Length == 0) { m_lastReject = "empty"; return null; }
                com /= pts.Length;
                float h = mx.y - mn.y;
                float dx = q.xz.x - com.x, dz = q.xz.y - com.z;

                m_stamp++;
                m_touched.Clear();
                for (int i = 0; i < pts.Length; i++)
                {
                    int c = Cell(pts[i].x + dx, pts[i].z + dz);
                    if (c < 0) { S.rejBounds++; m_lastReject = "bounds"; return null; }
                    if (m_tStamp[c] != m_stamp) { m_tStamp[c] = m_stamp; m_tBot[c] = pts[i].y; m_tTop[c] = pts[i].y; m_touched.Add(c); }
                    else { if (pts[i].y < m_tBot[c]) m_tBot[c] = pts[i].y; if (pts[i].y > m_tTop[c]) m_tTop[c] = pts[i].y; }
                }

                float off;
                if (q.mode == Mode.Free) off = q.freeY - mn.y;
                else
                {
                    off = float.MinValue;
                    foreach (int c in m_touched)
                    {
                        float sup = q.mode == Mode.Rest ? Support(c) : Ground(c);
                        off = Mathf.Max(off, sup - m_tBot[c]);
                    }
                    off -= q.sink * h;
                }

                var supporters = new List<int>();
                float widthRatio = 0f;
                if (q.mode == Mode.Rest)
                {
                    int groundC = 0, pieceC = 0;
                    var contacts = new List<Vector3>();
                    var contactOwner = new List<int>();
                    float tol = 0.06f + q.sink * h;
                    foreach (int c in m_touched)
                    {
                        float b = m_tBot[c] + off;
                        float sup = Support(c);
                        if (b > sup + tol) continue;
                        Vector2 cc = CellCenter(c);
                        bool onPiece = m_top[c] > Ground(c) + 0.05f && m_owner[c] >= 0;
                        contacts.Add(new Vector3(cc.x, sup, cc.y));
                        contactOwner.Add(onPiece ? m_owner[c] : -1);
                        if (onPiece) { pieceC++; if (!supporters.Contains(m_owner[c])) supporters.Add(m_owner[c]); }
                        else groundC++;
                    }
                    if (contacts.Count == 0) { S.rejNoSupport++; m_lastReject = "nosupport"; return null; }
                    if (pieceC > 0)
                    {
                        S.restTested++;
                        float groundShare = groundC / (float)contacts.Count;
                        if (q.requireGround && groundShare < 0.2f) { S.rejWedge++; m_lastReject = "wedge"; return null; }
                        Vector2 com2 = new Vector2(com.x + dx, com.z + dz);
                        var hullPts = new List<Vector2>(contacts.Count);
                        foreach (Vector3 c in contacts) hullPts.Add(new Vector2(c.x, c.z));
                        List<Vector2> hull = ConvexHull(hullPts);
                        if (hull.Count < 3 || !InsideShrunk(hull, com2, m_style.comShrink))
                        {
                            S.rejCom++; m_lastReject = "com"; return null;
                        }
                        if (groundShare < 0.5f)
                        {
                            var supCells = new List<Vector2>();
                            foreach (int sid in supporters) foreach (int tc in m_placed[sid].topCells) supCells.Add(CellCenter(tc));
                            widthRatio = WidthRatio(pts, dx, dz, supCells);
                            bool capOk = q.allowCap && m_style.allowCaps;
                            float maxRatio = capOk ? m_style.capWidthRatio.y : m_style.maxWidthOverSupport;
                            if (widthRatio > maxRatio) { S.rejWidth++; m_lastReject = "width"; return null; }
                            float pieceW = HorizontalExtent(pts);
                            foreach (int sid in supporters)
                            {
                                Placed sp = m_placed[sid];
                                if (!sp.tall) continue;
                                bool exempt = (capOk && sp.stem) || (q.lintel && supporters.Count >= 2);
                                if (!exempt && pieceW > 0.8f * sp.topWidth) { S.rejOnThin++; m_lastReject = "onThin"; return null; }
                            }
                            if (q.lintel && supporters.Count < 2) { S.rejNoSupport++; m_lastReject = "lintel"; return null; }
                        }
                        float slope = ContactSlope(contacts, out Vector2 downDir);
                        if (slope > m_style.maxContactSlope)
                        {
                            bool wedged = false;
                            float lo = 0f, hi = 0f;
                            var owners = new HashSet<int>();
                            for (int i = 0; i < contacts.Count; i++)
                            {
                                float pr = Vector2.Dot(new Vector2(contacts[i].x, contacts[i].z) - com2, downDir);
                                if (pr < -0.15f) { lo = Mathf.Min(lo, pr); owners.Add(contactOwner[i]); }
                                if (pr > 0.15f) { hi = Mathf.Max(hi, pr); owners.Add(contactOwner[i]); }
                            }
                            if (lo < 0f && hi > 0f) wedged = owners.Count >= 2;
                            if (!wedged) { S.rejSlope++; m_lastReject = "slope"; return null; }
                            S.wedged++;
                        }
                        S.restPassed++;
                    }
                }

                // Size by height.
                if (q.nominal > 0f && m_H > 0f && iter < 2 && q.mode != Mode.Free)
                {
                    float baseY = mn.y + off;
                    float hAbove = baseY - GroundAt(com.x + dx, com.z + dz);
                    float allowed = q.nominal * Mathf.Lerp(1f, m_style.sizeAtTop, Mathf.Clamp01(hAbove / m_H));
                    float cur = Mathf.Max(mx.x - mn.x, Mathf.Max(h, mx.z - mn.z));
                    if (cur > allowed * 1.05f)
                    {
                        scale *= allowed / cur;
                        continue;
                    }
                }

                // Keep-outs: nothing above the local ground may intrude.
                if (!q.ignoreKeepOuts && m_keepOuts.Count > 0)
                {
                    foreach (KeepOut ko in m_keepOuts)
                    {
                        if (!ko.active) continue;
                        for (int i = 0; i < pts.Length; i++)
                        {
                            Vector3 p = new Vector3(pts[i].x + dx, pts[i].y + off, pts[i].z + dz);
                            if (!ko.Contains(p, 0.08f)) continue;
                            if (p.y < GroundAt(p.x, p.z) + 0.03f) continue;
                            S.rejKeepOut++; m_lastReject = "keepout"; m_lastKeepOut = ko; return null;
                        }
                    }
                }

                // Overlap / hidden (material columns from the ground up).
                if (!q.skipOverlap && q.mode != Mode.Free)
                {
                    float vol = 0f, ov = 0f; int vis = 0;
                    foreach (int c in m_touched)
                    {
                        float g = Ground(c), pb = Mathf.Max(m_tBot[c] + off, g), pt = m_tTop[c] + off;
                        if (pt <= g) continue;
                        vol += pt - pb;
                        float ex = m_top[c];
                        if (ex > g)
                        {
                            ov += Mathf.Max(0f, Mathf.Min(pt, ex) - pb);
                            if (pt > ex + 0.05f) vis++;
                        }
                        else vis++;
                    }
                    if (vol > 1e-4f && ov / vol > q.maxOverlap) { S.rejOverlap++; m_lastReject = "overlap"; return null; }
                    if (m_touched.Count > 0 && vis / (float)m_touched.Count < q.minVisible) { S.rejHidden++; m_lastReject = "hidden"; return null; }
                }

                // Commit.
                part.position = new Vector3(dx, off, dz);
                part.placeMode = (int)q.mode;
                part.sink = q.sink;
                var pl = new Placed
                {
                    id = m_placed.Count, part = part, info = q.info, height = h, mode = q.mode, role = q.role, tier = q.tier,
                    com = new Vector3(com.x + dx, com.y + off, com.z + dz), cap = q.allowCap && m_style.allowCaps && supporters.Count > 0,
                    widthRatio = widthRatio,
                };
                pl.supporters.AddRange(supporters);
                Vector3 wmn = new Vector3(mn.x + dx, mn.y + off, mn.z + dz), wmx = new Vector3(mx.x + dx, mx.y + off, mx.z + dz);
                pl.bounds = new Bounds((wmn + wmx) * 0.5f, wmx - wmn);
                pl.bottomY = wmn.y;
                float hx = HorizontalExtent(pts);
                pl.maxExtent = Mathf.Max(hx, h);
                pl.tall = q.tallFlag && q.info.pieceClass == DmRockPieceClass.Tall && h > 1.2f * hx && Vector3.Angle(q.rot * Vector3.up, Vector3.up) < 50f;
                float topCut = wmx.y - 0.2f * h;
                var topPts = new List<Vector3>();
                for (int i = 0; i < pts.Length; i++)
                    if (pts[i].y + off >= topCut) topPts.Add(new Vector3(pts[i].x + dx, pts[i].y + off, pts[i].z + dz));
                pl.topWidth = HorizontalExtent(topPts);
                Vector3 tc0 = Vector3.zero; foreach (Vector3 p in topPts) tc0 += p;
                pl.topCenter = topPts.Count > 0 ? tc0 / topPts.Count : pl.com;
                foreach (int c in m_touched)
                {
                    float pt = m_tTop[c] + off;
                    if (pt > m_top[c]) { pl.undo.Add(new Vector3(c, m_top[c], m_owner[c])); m_top[c] = pt; m_owner[c] = pl.id; }
                    if (pt >= topCut) pl.topCells.Add(c);
                }
                if (pl.cap) { S.caps++; S.maxCapRatio = Mathf.Max(S.maxCapRatio, widthRatio); }
                else if (supporters.Count > 0) S.maxWidthRatio = Mathf.Max(S.maxWidthRatio, widthRatio);
                part.pieceIndex = m_res.parts.Count + 1;
                part.role = q.role;
                m_placed.Add(pl);
                m_res.parts.Add(part);
                S.placed++;
                return pl;
            }
            m_lastReject = "size";
            return null;
        }

        /// <summary>TryPlace with retries: pushes out of keep-outs, jitters position, shrinks after width rejections.</summary>
        private Placed Place(Req q, int tries = 6, float jitter = 0.6f)
        {
            Req r = q.Clone();
            Vector2 start = q.xz;
            for (int t = 0; t < tries; t++)
            {
                Placed p = TryPlace(r);
                if (p != null) return p;
                if (m_lastReject == "bounds" || m_lastReject == "nopiece") return null;
                if (m_lastReject == "keepout" && m_lastKeepOut != null)
                {
                    KeepOut ko = m_lastKeepOut;
                    Vector3 l = ko.Local(X0Z(r.xz));
                    float pr = r.info.Mid * MaxC(r.scale) * 0.5f;
                    if (ko.kind == 1)
                    {
                        Vector3 away = new Vector3(l.x, 0f, l.z);
                        if (away.sqrMagnitude < 1e-4f) away = Vector3.right;
                        away = away.normalized * (Mathf.Max(ko.size.x, ko.size.z) * 0.5f + pr * (0.6f + 0.25f * t) + 0.25f);
                        Vector3 w = ko.center + ko.rotation * away;
                        r.xz = new Vector2(w.x, w.z);
                    }
                    else
                    {
                        float push = ko.size.x * 0.5f + pr * (0.6f + 0.25f * t) + 0.25f;
                        float side = Mathf.Abs(l.x) > 0.05f ? Mathf.Sign(l.x) : (Chance(0.5f) ? 1f : -1f);
                        Vector3 w = ko.center + ko.rotation * new Vector3(side * push, 0f, l.z);
                        r.xz = new Vector2(w.x, w.z);
                    }
                }
                else if (m_lastReject == "width" || m_lastReject == "onThin")
                {
                    r.scale *= 0.85f;
                    if (r.fitHeight > 0f) r.fitHeight *= 0.85f;
                    r.xz = start + InCircle() * jitter * 0.5f;
                }
                else r.xz = start + InCircle() * jitter * (t + 1) / tries;
            }
            return null;
        }

        private static float MaxC(Vector3 v) => Mathf.Max(v.x, Mathf.Max(v.y, v.z));

        private static void MinMaxY(PcgPlacedPart part, out float y0, out float y1)
        {
            y0 = float.MaxValue; y1 = float.MinValue;
            foreach (Vector3 v in Samples(part.mesh))
            {
                float y = part.TransformPoint(v).y;
                if (y < y0) y0 = y;
                if (y > y1) y1 = y;
            }
        }

        private static readonly Vector2[] s_dirs =
        {
            new Vector2(1, 0), new Vector2(0.7071f, 0.7071f), new Vector2(0, 1), new Vector2(-0.7071f, 0.7071f),
        };

        private static float HorizontalExtent(IList<Vector3> pts)
        {
            if (pts == null || pts.Count == 0) return 0f;
            float best = 0f;
            foreach (Vector2 d in s_dirs)
            {
                float lo = float.MaxValue, hi = float.MinValue;
                foreach (Vector3 p in pts) { float x = p.x * d.x + p.z * d.y; if (x < lo) lo = x; if (x > hi) hi = x; }
                best = Mathf.Max(best, hi - lo);
            }
            return best;
        }

        private float WidthRatio(Vector3[] pts, float dx, float dz, List<Vector2> sup)
        {
            if (sup.Count == 0) return 99f;
            float worst = 0f;
            foreach (Vector2 d in s_dirs)
            {
                float lo = float.MaxValue, hi = float.MinValue;
                foreach (Vector3 p in pts) { float x = (p.x + dx) * d.x + (p.z + dz) * d.y; if (x < lo) lo = x; if (x > hi) hi = x; }
                float slo = float.MaxValue, shi = float.MinValue;
                foreach (Vector2 s in sup) { float x = Vector2.Dot(s, d); if (x < slo) slo = x; if (x > shi) shi = x; }
                float se = shi - slo + m_cell;
                worst = Mathf.Max(worst, (hi - lo) / Mathf.Max(0.05f, se));
            }
            return worst;
        }

        private static float ContactSlope(List<Vector3> c, out Vector2 down)
        {
            down = Vector2.right;
            if (c.Count < 3) return 0f;
            double sx = 0, sz = 0, sy = 0, sxx = 0, szz = 0, sxz = 0, sxy = 0, szy = 0; int n = c.Count;
            foreach (Vector3 p in c) { sx += p.x; sz += p.z; sy += p.y; }
            double mx = sx / n, mz = sz / n, my = sy / n;
            foreach (Vector3 p in c)
            {
                double x = p.x - mx, z = p.z - mz, y = p.y - my;
                sxx += x * x; szz += z * z; sxz += x * z; sxy += x * y; szy += z * y;
            }
            double det = sxx * szz - sxz * sxz;
            if (Math.Abs(det) < 1e-9) return 0f;
            double a = (sxy * szz - szy * sxz) / det, b = (szy * sxx - sxy * sxz) / det;
            var g = new Vector2((float)a, (float)b);
            if (g.sqrMagnitude > 1e-8f) down = -g.normalized;
            return Mathf.Atan((float)Math.Sqrt(a * a + b * b)) * Mathf.Rad2Deg;
        }

        private static List<Vector2> ConvexHull(List<Vector2> pts)
        {
            pts.Sort((p, q) => p.x != q.x ? p.x.CompareTo(q.x) : p.y.CompareTo(q.y));
            var h = new List<Vector2>();
            if (pts.Count < 3) { h.AddRange(pts); return h; }
            for (int pass = 0; pass < 2; pass++)
            {
                int start = h.Count;
                for (int k = 0; k < pts.Count; k++)
                {
                    Vector2 p = pass == 0 ? pts[k] : pts[pts.Count - 1 - k];
                    while (h.Count >= start + 2 && Cross(h[h.Count - 2], h[h.Count - 1], p) <= 0f) h.RemoveAt(h.Count - 1);
                    h.Add(p);
                }
                h.RemoveAt(h.Count - 1);
            }
            return h;
        }

        private static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

        private static bool InsideShrunk(List<Vector2> hull, Vector2 p, float shrink)
        {
            Vector2 c = Vector2.zero;
            foreach (Vector2 v in hull) c += v;
            c /= hull.Count;
            float k = 1f - shrink;
            for (int i = 0; i < hull.Count; i++)
            {
                Vector2 a = c + (hull[i] - c) * k, b = c + (hull[(i + 1) % hull.Count] - c) * k;
                if (Cross(a, b, p) < 0f) return false;
            }
            return true;
        }

        private void AddFixed(PcgPlacedPart fp)
        {
            if (fp == null || fp.mesh == null) return;
            fp.Prepare();
            Vector3[] src = Samples(fp.mesh);
            Vector3 mn = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), mx = -mn;
            var pts = new Vector3[src.Length];
            for (int i = 0; i < src.Length; i++) { pts[i] = fp.TransformPoint(src[i]); mn = Vector3.Min(mn, pts[i]); mx = Vector3.Max(mx, pts[i]); }
            var pl = new Placed { id = m_placed.Count, part = fp, height = mx.y - mn.y, role = "fixed", mode = Mode.Free };
            pl.bounds = new Bounds((mn + mx) * 0.5f, mx - mn);
            pl.com = pl.bounds.center; pl.topCenter = pl.com; pl.maxExtent = MaxC(mx - mn); pl.topWidth = pl.maxExtent; pl.bottomY = mn.y;
            foreach (Vector3 p in pts)
            {
                int c = Cell(p.x, p.z);
                if (c >= 0 && p.y > m_top[c]) { m_top[c] = p.y; m_owner[c] = pl.id; }
            }
            fp.pieceIndex = 0;
            fp.isBase = true;
            m_placed.Add(pl);
            m_res.parts.Add(fp);
            m_H = Mathf.Max(0.5f, pl.height);
            m_R = Mathf.Max(0.5f, Mathf.Max(mx.x - mn.x, mx.z - mn.z) * 0.5f);
        }

        private void RemoveLast()
        {
            if (m_placed.Count == 0) return;
            Placed p = m_placed[m_placed.Count - 1];
            m_placed.RemoveAt(m_placed.Count - 1);
            m_res.parts.Remove(p.part);
            S.placed--;
            for (int i = p.undo.Count - 1; i >= 0; i--)
            {
                int c = (int)p.undo[i].x;
                m_top[c] = p.undo[i].y; m_owner[c] = (int)p.undo[i].z;
            }
        }
    }
}

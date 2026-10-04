using System.Collections.Generic;
using UnityEngine;

namespace GenesisPCG.RockCreation
{
    public enum PcgCliffRole { Wall = 0, Backfill = 1, Cap = 2, TalusBody = 3, TalusScatter = 4, ToeBlock = 5 }

    /// <summary>One placed kit piece: mesh-space -> cliff-local matrix, its role and its chunk (by distance along the path).</summary>
    public struct PcgCliffPart
    {
        public int piece;
        public Matrix4x4 matrix;
        public PcgCliffRole role;
        public int chunk;
    }

    /// <summary>Catmull-Rom path sampling shared by the generator and the Scene view handles (cliff-local space).</summary>
    public static class PcgCliffCurve
    {
        public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        /// <summary>Dense polyline through the control points (about one sample per <paramref name="step"/> metres).</summary>
        public static List<Vector3> Sample(IList<Vector3> pts, bool closed, float step)
        {
            var o = new List<Vector3>();
            int n = pts?.Count ?? 0;
            if (n == 0) return o;
            if (n == 1) { o.Add(pts[0]); return o; }
            closed &= n >= 3;
            int segs = closed ? n : n - 1;
            for (int i = 0; i < segs; i++)
            {
                Vector3 p1 = pts[i], p2 = pts[(i + 1) % n];
                Vector3 p0 = i > 0 ? pts[i - 1] : closed ? pts[n - 1] : p1 + (p1 - p2);
                Vector3 p3 = i + 2 < n ? pts[i + 2] : closed ? pts[(i + 2) % n] : p2 + (p2 - p1);
                int k = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(p1, p2) / Mathf.Max(0.1f, step)), 2, 400);
                for (int j = 0; j < k; j++) o.Add(CatmullRom(p0, p1, p2, p3, j / (float)k));
            }
            o.Add(closed ? pts[0] : pts[n - 1]);
            return o;
        }
    }

    /// <summary>
    /// Plans a sheer cliff from a <see cref="PcgCliffPath"/>: stations along the path chosen by curvature, stacked wall
    /// columns of tall pieces, a cap row with a lip, a backfill row and a graded talus apron. No meshes are touched here.
    /// </summary>
    public static class PcgSheerCliffGenerator
    {
        private sealed class Polyline
        {
            public readonly List<Vector3> p;
            public readonly float[] cum;
            public readonly bool closed;
            public float Length => cum[cum.Length - 1];

            public Polyline(List<Vector3> pts, bool closed)
            {
                p = pts; this.closed = closed;
                cum = new float[pts.Count];
                for (int i = 1; i < pts.Count; i++) cum[i] = cum[i - 1] + Flat(pts[i] - pts[i - 1]).magnitude;
            }

            private float Wrap(float s)
            {
                float L = Length;
                if (L <= 0f) return 0f;
                return closed ? Mathf.Repeat(s, L) : Mathf.Clamp(s, 0f, L);
            }

            public void Eval(float s, out Vector3 pos, out Vector3 tan)
            {
                s = Wrap(s);
                int lo = 0, hi = cum.Length - 1;
                while (hi - lo > 1) { int m = (lo + hi) >> 1; if (cum[m] <= s) lo = m; else hi = m; }
                float seg = cum[hi] - cum[lo];
                float t = seg > 1e-5f ? (s - cum[lo]) / seg : 0f;
                pos = Vector3.Lerp(p[lo], p[hi], t);
                tan = Flat(p[hi] - p[lo]);
                tan = tan.sqrMagnitude > 1e-8f ? tan.normalized : Vector3.forward;
            }
        }

        private struct Station
        {
            public float s;
            public Vector3 pos, tan, nrm;
            public float ground, height;
            public int curv; // -1 convex, 0 straight, 1 concave
        }

        public sealed class Palettes
        {
            public readonly List<int> wall = new List<int>(), wallSharp = new List<int>(), wallWide = new List<int>();
            public readonly List<int> cap = new List<int>(), talus = new List<int>(), toe = new List<int>();
        }

        private static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        public static Palettes BuildPalettes(DmRockKit kit)
        {
            var pal = new Palettes();
            if (kit == null || kit.pieces == null) return pal;
            for (int i = 0; i < kit.pieces.Count; i++)
            {
                DmRockPieceInfo p = kit.pieces[i];
                if (p == null || p.prefab == null || p.Long <= 0.01f) continue;
                bool tall = (p.pieceClass == DmRockPieceClass.Tall && p.Long >= 4f) || (p.authoredUpright && p.Long >= 5f && p.Aspect >= 1.15f);
                if (tall) pal.wall.Add(i);
                if (p.pieceClass == DmRockPieceClass.Slab || (p.pieceClass == DmRockPieceClass.Boulder && p.Flatness < 0.75f && p.Long >= 1.8f))
                    pal.cap.Add(i);
                if (p.pieceClass == DmRockPieceClass.Boulder || p.pieceClass == DmRockPieceClass.Small || p.pieceClass == DmRockPieceClass.Slab)
                    pal.talus.Add(i);
                if (p.pieceClass == DmRockPieceClass.Boulder) pal.toe.Add(i);
            }
            if (pal.cap.Count == 0) pal.cap.AddRange(pal.talus);
            if (pal.toe.Count == 0) pal.toe.AddRange(pal.talus);
            if (pal.talus.Count == 0) pal.talus.AddRange(pal.wall);
            if (pal.cap.Count == 0) pal.cap.AddRange(pal.wall);
            if (pal.toe.Count == 0) pal.toe.AddRange(pal.wall);
            // Convex corners prefer crisp pieces, concave bays the widest ones.
            var bySharp = new List<int>(pal.wall);
            bySharp.Sort((a, b) => kit.pieces[b].sharpness.CompareTo(kit.pieces[a].sharpness));
            var byWide = new List<int>(pal.wall);
            byWide.Sort((a, b) => kit.pieces[b].Mid.CompareTo(kit.pieces[a].Mid));
            int half = Mathf.Max(Mathf.Min(2, pal.wall.Count), (pal.wall.Count + 1) / 2);
            for (int i = 0; i < half && i < pal.wall.Count; i++) { pal.wallSharp.Add(bySharp[i]); pal.wallWide.Add(byWide[i]); }
            return pal;
        }

        private static int Pick(System.Random rng, List<int> pal, int avoidA, int avoidB)
        {
            if (pal.Count == 0) return -1;
            if (pal.Count == 1) return pal[0];
            for (int tries = 0; tries < 12; tries++)
            {
                int c = pal[rng.Next(pal.Count)];
                if (c != avoidA && (c != avoidB || pal.Count < 3)) return c;
            }
            return pal[rng.Next(pal.Count)];
        }

        private static float Range(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        /// <summary>Mesh space -> cliff local: frame axes (x mid, y long, z short) scaled, then oriented by <paramref name="rot"/>.</summary>
        public static Matrix4x4 PieceMatrix(DmRockPieceInfo info, Vector3 center, Quaternion rot, Vector3 frameSize)
        {
            Vector3 sc = new Vector3(frameSize.x / Mathf.Max(1e-3f, info.Mid), frameSize.y / Mathf.Max(1e-3f, info.Long), frameSize.z / Mathf.Max(1e-3f, info.Short));
            return Matrix4x4.TRS(center, rot, sc) * Matrix4x4.Rotate(Quaternion.Inverse(info.frame)) * Matrix4x4.Translate(-info.center);
        }

        public static List<PcgCliffPart> Plan(PcgCliffPath path, out string error)
        {
            error = null;
            var parts = new List<PcgCliffPart>();
            PcgSheerCliffSettings st = path != null ? path.settings : null;
            if (st == null) { error = "No cliff path."; return parts; }
            if (st.kit == null) { error = "Pick a rock kit (Settings > Kit)."; return parts; }
            if (path.points == null || path.points.Count < 2) { error = "The path needs at least 2 points (Shift+click in the Scene view)."; return parts; }
            Palettes pal = BuildPalettes(st.kit);
            if (pal.wall.Count == 0) { error = $"{st.kit.name} has no tall pieces (re-scan / analyze the kit)."; return parts; }

            var line = new Polyline(PcgCliffCurve.Sample(path.points, path.closed, 1f), path.closed && path.points.Count >= 3);
            float L = line.Length;
            if (L < 1f) { error = "The path is too short."; return parts; }

            var rng = new System.Random(st.seed * 7919 + 17);
            float nx = (st.seed % 997) * 0.731f + 13.1f, ny = (st.seed % 503) * 0.377f + 5.7f;
            Transform tr = path.transform;
            var ignore = new List<Transform> { tr };
            int mask = ~0;

            float GroundAt(Vector3 local, float fallback)
            {
                if (!st.followGround) return fallback;
                Vector3 w = tr.TransformPoint(local);
                if (PcgSurfaceSnap.SampleGroundY(w, 300f, 600f, mask, PcgSurfaceSnapSettings.Default, ignore, out float gy))
                    return tr.InverseTransformPoint(new Vector3(w.x, gy, w.z)).y;
                return fallback;
            }

            // ---- stations
            var stations = new List<Station>();
            float w0 = st.moduleWidth;
            float sPos = line.closed ? 0f : w0 * 0.35f;
            float sEnd = line.closed ? L - w0 * 0.5f : L - w0 * 0.35f;
            if (sEnd < sPos) sEnd = sPos;
            int guard = 0;
            while (sPos <= sEnd + 1e-3f && guard++ < 4000)
            {
                line.Eval(sPos, out Vector3 pos, out Vector3 tan);
                line.Eval(sPos - w0 * 0.5f, out _, out Vector3 t0);
                line.Eval(sPos + w0 * 0.5f, out _, out Vector3 t1);
                float turn = Vector3.SignedAngle(t0, t1, Vector3.up); // + = turning right
                if (st.faceSide == PcgCliffFaceSide.Left) turn = -turn;
                int curv = turn > st.curvatureThreshold ? 1 : turn < -st.curvatureThreshold ? -1 : 0; // turning toward the face = concave bay
                Vector3 nrm = st.faceSide == PcgCliffFaceSide.Right ? Vector3.Cross(Vector3.up, tan) : Vector3.Cross(tan, Vector3.up);
                float h = st.height * (1f + st.heightNoise * (Mathf.PerlinNoise(sPos / Mathf.Max(1f, st.heightNoiseScale) + nx, ny) * 2f - 1f));
                if (st.taperEnds && !line.closed)
                {
                    float edge = Mathf.Min(sPos, L - sPos), span = w0 * 2.5f;
                    if (edge < span) h *= Mathf.Lerp(0.55f, 1f, Mathf.SmoothStep(0f, 1f, edge / span));
                }
                stations.Add(new Station { s = sPos, pos = pos, tan = tan, nrm = nrm.normalized, ground = GroundAt(pos, pos.y), height = Mathf.Max(3f, h), curv = curv });
                float step = w0 * (1f - st.overlap) * (curv < 0 ? 0.78f : 1f) * Range(rng, 0.92f, 1.08f);
                sPos += Mathf.Max(0.5f, step);
            }

            float chunkLen = Mathf.Max(5f, st.chunkLength);
            int lastWall = -1, lastCap = -1, lastTalus = -1;
            var lastInRow = new Dictionary<int, int>();
            float jit = st.scaleJitter;

            foreach (Station sta in stations)
            {
                int chunk = Mathf.Min(Mathf.FloorToInt(sta.s / chunkLen), Mathf.Max(0, Mathf.CeilToInt(L / chunkLen) - 1));
                Vector3 up = Vector3.up, n = sta.nrm;
                float lean = (90f - st.faceAngle) * Mathf.Deg2Rad;
                Vector3 upL = (up * Mathf.Cos(lean) - n * Mathf.Sin(lean)).normalized;
                Vector3 fwdL = (n * Mathf.Cos(lean) + up * Mathf.Sin(lean)).normalized;
                float Hc = sta.height + st.sink;
                Vector3 baseP = new Vector3(sta.pos.x, sta.ground - st.sink, sta.pos.z);
                List<int> wallPal = sta.curv < 0 ? pal.wallSharp : sta.curv > 0 ? pal.wallWide : pal.wall;

                // ---- wall column: rows of tall pieces, random split of the height
                int rows = Mathf.Max(1, Mathf.RoundToInt(Hc / Mathf.Max(2f, st.rowHeight)));
                var hs = new float[rows];
                float sum = 0f;
                for (int r = 0; r < rows; r++) { hs[r] = Range(rng, 0.8f, 1.2f); sum += hs[r]; }
                float y0 = 0f;
                for (int r = 0; r < rows; r++)
                {
                    float hRow = hs[r] / sum * Hc;
                    int prevRow = lastInRow.TryGetValue(r, out int pr) ? pr : -1;
                    int pi = Pick(rng, wallPal, prevRow, lastWall);
                    lastInRow[r] = pi; lastWall = pi;
                    DmRockPieceInfo info = st.kit.pieces[pi];
                    float tilt = Range(rng, -st.tiltJitter, st.tiltJitter);
                    Quaternion rot = Quaternion.AngleAxis(Range(rng, -st.yawJitter, st.yawJitter), upL) *
                                     Quaternion.AngleAxis(tilt, sta.tan) *
                                     Quaternion.LookRotation(fwdL, upL);
                    if (rng.NextDouble() < 0.5) rot = Quaternion.AngleAxis(180f, upL) * rot;
                    Vector3 size = new Vector3(st.moduleWidth * Range(rng, 1f - jit, 1f + jit),
                                               hRow * 1.12f,
                                               st.moduleDepth * Range(rng, 1f - jit, 1f + jit));
                    Vector3 c = baseP + upL * (y0 + hRow * 0.5f);
                    parts.Add(new PcgCliffPart { piece = pi, matrix = PieceMatrix(info, c, rot, size), role = PcgCliffRole.Wall, chunk = chunk });
                    y0 += hRow;
                }

                // ---- backfill: fewer, plainer pieces behind the face
                if (st.backfill)
                {
                    int bRows = Mathf.Max(1, (rows + 1) / 2);
                    float bH = Hc * 0.97f / bRows;
                    Vector3 bBase = baseP - n * (st.moduleDepth * 0.75f);
                    for (int r = 0; r < bRows; r++)
                    {
                        int pi = Pick(rng, pal.wall, lastWall, -1);
                        DmRockPieceInfo info = st.kit.pieces[pi];
                        Quaternion rot = Quaternion.AngleAxis(Range(rng, 0f, 360f) > 180f ? 180f : 0f, upL) * Quaternion.LookRotation(fwdL, upL);
                        Vector3 size = new Vector3(st.moduleWidth * 1.1f, bH * 1.1f, st.moduleDepth);
                        parts.Add(new PcgCliffPart { piece = pi, matrix = PieceMatrix(info, bBase + upL * (bH * (r + 0.5f)), rot, size), role = PcgCliffRole.Backfill, chunk = chunk });
                    }
                }

                // ---- cap row with the lip
                if (st.cap && pal.cap.Count > 0)
                {
                    int pi = Pick(rng, pal.cap, lastCap, -1); lastCap = pi;
                    DmRockPieceInfo info = st.kit.pieces[pi];
                    float capDepth = st.moduleDepth * 1.6f;
                    Vector3 top = baseP + upL * Hc;
                    Vector3 c = top + n * (st.moduleDepth * 0.5f + st.lip - capDepth * 0.5f) + up * (st.capThickness * 0.15f);
                    Quaternion rot = Quaternion.AngleAxis(Range(rng, -st.yawJitter, st.yawJitter), up) * Quaternion.LookRotation(up, -n);
                    Vector3 size = new Vector3(st.moduleWidth * 1.15f * Range(rng, 1f - jit * 0.5f, 1f + jit * 0.5f), capDepth, st.capThickness * Range(rng, 0.85f, 1.15f));
                    parts.Add(new PcgCliffPart { piece = pi, matrix = PieceMatrix(info, c, rot, size), role = PcgCliffRole.Cap, chunk = chunk });
                }

                // ---- talus apron
                if (st.talus && pal.talus.Count > 0)
                {
                    float W = Mathf.Max(1f, st.apronWidth * sta.height);
                    float h0 = Mathf.Min(W * Mathf.Tan(st.talusAngle * Mathf.Deg2Rad), sta.height * 0.45f);
                    float a = Mathf.Atan2(h0, W);
                    Vector3 dir = (n * Mathf.Cos(a) - up * Mathf.Sin(a)).normalized;   // downhill
                    Vector3 sn = (n * Mathf.Sin(a) + up * Mathf.Cos(a)).normalized;    // slope normal
                    Vector3 foot = new Vector3(sta.pos.x, 0f, sta.pos.z) + n * (st.moduleDepth * 0.5f);
                    float step = st.moduleWidth * (1f - st.overlap);

                    // body: two tilted pieces whose upper faces follow the repose slope
                    for (int j = 0; j < 2; j++)
                    {
                        float dm = (j + 0.5f) * W * 0.5f;
                        float hsl = h0 * (1f - dm / W);
                        Vector3 xz = foot + n * dm + sta.tan * Range(rng, -0.3f, 0.3f) * step;
                        float g = GroundAt(xz + up * sta.ground, sta.ground);
                        float T = Mathf.Max(1f, hsl * 0.9f + 0.6f);
                        Vector3 c = new Vector3(xz.x, g + hsl, xz.z) - sn * (T * 0.4f);
                        int pi = Pick(rng, pal.toe, lastTalus, -1); lastTalus = pi;
                        DmRockPieceInfo info = st.kit.pieces[pi];
                        Quaternion rot = Quaternion.AngleAxis(Range(rng, -8f, 8f), sn) * Quaternion.LookRotation(sn, dir);
                        Vector3 size = new Vector3(step * 1.5f, W * 0.5f / Mathf.Max(0.3f, Mathf.Cos(a)) * 1.4f, T);
                        parts.Add(new PcgCliffPart { piece = pi, matrix = PieceMatrix(info, c, rot, size), role = PcgCliffRole.TalusBody, chunk = chunk });
                    }

                    // surface scatter, graded small (top) to coarse (toe)
                    int count = Mathf.Max(1, Mathf.RoundToInt(st.talusDensity * W * step / 6f));
                    for (int k = 0; k < count; k++)
                    {
                        float t = (float)rng.NextDouble();
                        float d = t * W;
                        float size = Mathf.Lerp(st.talusMinSize, st.talusMaxSize, Mathf.Pow(t, st.grading)) * Range(rng, 0.8f, 1.2f);
                        Vector3 xz = foot + n * d + sta.tan * Range(rng, -0.5f, 0.5f) * step;
                        float g = GroundAt(xz + up * sta.ground, sta.ground);
                        int pi = Pick(rng, pal.talus, lastTalus, -1); lastTalus = pi;
                        DmRockPieceInfo info = st.kit.pieces[pi];
                        Quaternion rot = Quaternion.AngleAxis(Range(rng, 0f, 360f), sn) * Quaternion.AngleAxis(Range(rng, -25f, 25f), sta.tan) * Quaternion.LookRotation(n, up);
                        Vector3 c = new Vector3(xz.x, g + h0 * (1f - t) + size * 0.12f, xz.z);
                        Vector3 fs = new Vector3(size * info.Mid / info.Long, size, size * info.Short / info.Long);
                        parts.Add(new PcgCliffPart { piece = pi, matrix = PieceMatrix(info, c, rot, fs), role = PcgCliffRole.TalusScatter, chunk = chunk });
                    }

                    // big blocks at the toe
                    int toe = rng.NextDouble() < 0.35 * st.talusDensity ? 2 : 1;
                    for (int k = 0; k < toe; k++)
                    {
                        float d = W * Range(rng, 0.9f, 1.25f);
                        float size = st.talusMaxSize * Range(rng, 0.8f, 1.3f);
                        Vector3 xz = foot + n * d + sta.tan * Range(rng, -0.5f, 0.5f) * step;
                        float g = GroundAt(xz + up * sta.ground, sta.ground);
                        int pi = Pick(rng, pal.toe, lastTalus, -1); lastTalus = pi;
                        DmRockPieceInfo info = st.kit.pieces[pi];
                        Quaternion rot = Quaternion.AngleAxis(Range(rng, 0f, 360f), up) * Quaternion.AngleAxis(Range(rng, -15f, 15f), sta.tan) * Quaternion.LookRotation(n, up);
                        Vector3 fs = new Vector3(size * info.Mid / info.Long, size, size * info.Short / info.Long);
                        Vector3 c = new Vector3(xz.x, g + size * 0.2f, xz.z);
                        parts.Add(new PcgCliffPart { piece = pi, matrix = PieceMatrix(info, c, rot, fs), role = PcgCliffRole.ToeBlock, chunk = chunk });
                    }
                }
            }
            return parts;
        }
    }
}

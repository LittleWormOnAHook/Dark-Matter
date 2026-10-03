using System.Collections.Generic;
using UnityEngine;

namespace GenesisPCG.RockCreation
{
    public sealed partial class DmRockAssembler
    {
        private void Dome()
        {
            // Exfoliation dome: one big rounded core, then flattened sheets hugging it tangentially (onion-skin layers,
            // strongly overlapped into the core so the silhouette stays one smooth mass), a crown sheet, no towers.
            DmRockStyle st = m_style;
            int n = R(st.pieceCount);
            Placed core = Hero(st.heroClasses, m_H * R(0.8f, 0.92f), Bedded(st.grainJitter, 10f), Vector2.zero, m_H * 1.5f);
            if (core == null) return;
            float coreR = Mathf.Max(core.bounds.size.x, core.bounds.size.z) * 0.5f;
            int sheets = Mathf.Max(4, n - 2);
            const float golden = 137.5f;
            float az0 = R(0f, 360f);
            for (int i = 0; i < sheets; i++)
            {
                float az = az0 + i * golden;
                Vector3 radial = Dir(az);
                float slopeA = R(25f, 45f) * Mathf.Deg2Rad;
                Vector3 nrm = (Vector3.up * Mathf.Cos(slopeA) + radial * Mathf.Sin(slopeA)).normalized;
                Vector3 tangent = Vector3.Cross(Vector3.up, radial);
                float len = coreR * R(1.1f, 1.5f);
                DmRockPieceInfo info = Pick(DmRockPieceClassMask.Slab | DmRockPieceClassMask.Boulder, len, 0.6f);
                Vector3 scale = Box(info, len, len * R(0.65f, 0.85f), len * R(0.22f, 0.32f), 1.9f);
                Vector2 xz = XZ(radial) * coreR * R(0.5f, 0.78f);
                Req q = Base(info, scale, BeddedAlong(tangent, 6f, nrm), xz, Mode.Ground, i < 2 ? "mid" : "sheet");
                q.sink = R(st.sink.y, st.sink.y + 0.15f); q.nominal = core.maxExtent; q.maxOverlap = 0.85f; q.minVisible = 0.08f;
                Place(q, 4, 0.35f);
            }
            {
                float len = coreR * R(0.9f, 1.2f);
                DmRockPieceInfo info = Pick(DmRockPieceClassMask.Slab, len, 0.6f);
                Vector3 scale = Box(info, len, len * 0.8f, len * 0.22f, 1.9f);
                Req q = Base(info, scale, Bedded(st.grainJitter, 6f), XZ(core.topCenter), Mode.Rest, "crown");
                q.sink = 0.35f; q.tier = 1; q.maxOverlap = 0.85f; q.minVisible = 0.08f; q.nominal = core.maxExtent;
                Place(q, 3, 0.3f);
            }
        }

        private void Hoodoo()
        {
            // Hoodoo field (research 3a): stems on a shared plinth; every stem is one or more vertically stretched SINGLE
            // rocks (never the pack's multi-rock "group" prefabs, which read as stacked boulders) pushed 40-55 % into each
            // other on one axis and narrowing upward; a wider, flattened caprock (1.15-1.45x the stem top) on most stems.
            DmRockStyle st = m_style;
            int n = R(st.pieceCount);
            float plinthSize = m_R * R(1.4f, 1.9f);
            DmRockPieceInfo pinfo = Pick(DmRockPieceClassMask.Slab | DmRockPieceClassMask.Boulder, plinthSize);
            Req pq = Base(pinfo, Uniform(pinfo, plinthSize), Bedded(4f), Vector2.zero, Mode.Ground, "plinth");
            pq.fitHeight = m_H * R(0.18f, 0.28f);
            pq.sink = R(0.25f, 0.4f);
            Place(pq, 4, 0.5f);
            var stems = new List<Placed>();
            for (int i = 0; i < n; i++)
            {
                Placed top = BuildStem(InCircle() * m_R * 0.75f, m_H * R(0.5f, 1f));
                if (top != null) stems.Add(top);
            }
            foreach (Placed s in stems)
            {
                if (!Chance(0.8f)) continue;
                float wantW = s.topWidth * R(1.15f, 1.45f);
                for (int t = 0; t < 4; t++)
                {
                    DmRockPieceInfo info = PickWhere(pc => !IsCluster(pc), DmRockPieceClassMask.Boulder | DmRockPieceClassMask.Slab, wantW);
                    Vector3 scale = Box(info, wantW, wantW * R(0.8f, 0.95f), wantW * R(0.4f, 0.55f), 1.6f);
                    Req q = Base(info, scale, BeddedAlong(Dir(R(0f, 360f)), 5f, Vector3.up), XZ(s.topCenter), Mode.Rest, "cap");
                    q.allowCap = true; q.sink = 0.12f; q.tier = 1; q.maxOverlap = 0.7f; q.tallFlag = false;
                    if (TryPlace(q) != null) break;
                    wantW *= 0.92f;
                }
            }
            int fallen = m_rng.Next(0, 3);
            for (int i = 0; i < fallen && stems.Count > 0; i++)
            {
                Placed s = stems[m_rng.Next(stems.Count)];
                float size = s.topWidth * R(1f, 1.3f);
                DmRockPieceInfo info = PickWhere(pc => !IsCluster(pc), DmRockPieceClassMask.Boulder | DmRockPieceClassMask.Slab, size);
                Vector2 xz = XZ(s.com) + XZ(Dir(R(0f, 360f))) * (s.height * R(0.4f, 0.9f));
                Req q = Base(info, Uniform(info, size), Bedded(25f, 20f), xz, Mode.Ground, "fallen-cap");
                q.tallFlag = false;
                Place(q, 3, 0.5f);
            }
        }

        /// <summary>One hoodoo stem from 1-3 stretched single rocks stacked on one axis with tight overlap; returns the top segment.</summary>
        private Placed BuildStem(Vector2 xz, float hgt)
        {
            DmRockStyle st = m_style;
            float baseW = hgt * R(0.26f, 0.34f);
            int segs = hgt > baseW * 3.2f ? 3 : (hgt > baseW * 1.9f ? 2 : 1);
            float ov = R(0.4f, 0.55f), s0 = R(st.sink);
            float segH = hgt / ((1f - s0) + (segs - 1) * (1f - ov));
            DmRockPieceInfo info = PickWhere(pc => !IsCluster(pc) && pc.Aspect >= 1.2f, DmRockPieceClassMask.Boulder | DmRockPieceClassMask.Tall, segH, 1.5f);
            Vector3 face = Dir(R(0f, 360f));
            Placed prev = null;
            for (int j = 0; j < segs; j++)
            {
                float w = baseW * Mathf.Lerp(1f, 0.8f, segs > 1 ? j / (float)(segs - 1) : 0f);
                Vector3 scale = Box(info, segH, w, w * R(0.85f, 1f), 1.4f);
                Quaternion rot = FrameLong(Jitter(m_up, 2f), Quaternion.AngleAxis(R(-25f, 25f) + j * 140f, Vector3.up) * face);
                Req q = Base(info, scale, rot, prev == null ? xz : XZ(prev.topCenter), prev == null ? Mode.Ground : Mode.Free, j == segs - 1 ? "stem" : "stem-seg");
                q.tallFlag = false;
                Placed p;
                if (prev == null)
                {
                    q.sink = s0; q.maxOverlap = 0.6f; q.minVisible = 0.3f;
                    p = Place(q, 5, 1f);
                }
                else
                {
                    q.freeY = prev.bounds.max.y - segH * ov;
                    p = TryPlace(q);
                    if (p != null) p.supporters.Add(prev.id);
                }
                if (p == null) break;
                p.stem = true;
                prev = p;
            }
            return prev;
        }

        private void Mesa()
        {
            // Butte / mesa: each body is a tight ring of tall wall blocks (flat faces outward, equal heights) capped by one
            // flat caprock slab spanning the ring (resting on several walls); the apron adds the rubble skirt.
            DmRockStyle st = m_style;
            int bodies = Chance(0.35f) ? 2 : 1;
            PassageInfo gully = null;
            if (bodies >= 2 && Chance(PassageChance))
                gully = CreatePassage(m_dip, 0f, Mathf.Max(m_feat.passageWidth, R(1.8f, 2.6f)), m_feat.passageHeadroom, m_R + m_H * st.apronReach + 3f, "gully");
            for (int bi = 0; bi < bodies; bi++)
            {
                float bodyR = m_R * (bodies == 1 ? R(0.6f, 0.75f) : R(0.42f, 0.52f));
                float Hb = m_H * (bi == 0 ? R(0.88f, 1f) : R(0.65f, 0.85f));
                Vector2 c = Vector2.zero;
                if (bodies == 2)
                {
                    float sep = bodyR + (gully != null ? gully.width * 0.5f + 0.4f : bodyR * 0.6f);
                    c = XZ(m_strike) * (bi == 0 ? -sep : sep);
                }
                int walls = m_rng.Next(5, 8);
                float az0 = R(0f, 360f);
                var ring = new List<Placed>();
                float circ = 2f * Mathf.PI * bodyR * 0.62f;
                for (int i = 0; i <= walls; i++)
                {
                    bool centre = i == walls;
                    float az = az0 + i * 360f / walls + R(-8f, 8f);
                    Vector3 radial = Dir(az);
                    float sink = R(st.sink);
                    float h = Hb * R(0.96f, 1.03f) / (1f - sink);
                    DmRockPieceInfo info = Pick(DmRockPieceClassMask.Tall | DmRockPieceClassMask.Boulder, h, -1f, 1.8f);
                    Vector3 scale = Box(info, h, centre ? bodyR * 1.1f : circ / walls * 1.45f, centre ? bodyR * 1.1f : bodyR * 0.75f, 1.8f);
                    Vector2 xz = c + (centre ? Vector2.zero : XZ(radial) * bodyR * 0.62f);
                    Req q = Base(info, scale, FrameLong(Jitter(Vector3.up, 2f), centre ? m_dip : radial), xz, Mode.Ground, centre ? "body" : "wall");
                    q.sink = sink; q.maxOverlap = 0.9f; q.minVisible = 0.02f; q.nominal = Hb * 2f;
                    Placed p = Place(q, 3, 0.25f);
                    if (p != null) ring.Add(p);
                }
                if (ring.Count < 3) continue;
                float top = 0f; Bounds rb = ring[0].bounds;
                foreach (Placed p in ring) { top += p.bounds.max.y; rb.Encapsulate(p.bounds); }
                top /= ring.Count;
                Placed cap = null;
                for (int t = 0; t < 4 && cap == null; t++)
                {
                    float capLen = Mathf.Max(rb.size.x, rb.size.z) * R(0.92f, 1.05f) * (1f - 0.08f * t);
                    float thick = m_H * R(0.13f, 0.2f);
                    DmRockPieceInfo info = Pick(DmRockPieceClassMask.Slab, capLen);
                    Vector3 scale = Box(info, capLen, Mathf.Min(rb.size.x, rb.size.z) * R(0.9f, 1.02f), thick, 2.2f);
                    Req q = Base(info, scale, FrameShort(Jitter(Vector3.up, 2f), m_strike), c, Mode.Rest, "caprock");
                    q.lintel = true; q.sink = 0.18f; q.tier = 1; q.maxOverlap = 0.8f; q.minVisible = 0.1f;
                    cap = TryPlace(q);
                    if (cap == null)
                    {
                        q.mode = Mode.Free; q.freeY = top - thick * 0.45f; q.skipOverlap = true;
                        cap = TryPlace(q);
                        if (cap != null) foreach (Placed p in ring) cap.supporters.Add(p.id);
                    }
                }
                m_res.log.Add($"mesa body {bi}: {ring.Count} walls, height {Hb:0.0}, caprock {(cap != null ? cap.mode.ToString() : "failed")}");
            }
            // Nook after the bodies: its keep-out must not punch holes into the wall ring (the caprock needs a closed ring).
            if (RollNook()) CreateNook(Quaternion.AngleAxis(R(-60f, 60f), Vector3.up) * m_dip, m_R * R(0.85f, 1.05f));
        }

        private void Yardang()
        {
            // Wind-streamlined ridges (research 3a): every ridge is ONE elongated body on the wind axis - the same single
            // piece repeated as 3-6 segments pushed 55-62 % into each other, tallest / widest at the blunt upwind head,
            // descending and narrowing to a tapered lee tail; ridges parallel (+-3 deg) with sand troughs between them.
            DmRockStyle st = m_style;
            int ridges = R(st.ridgeCount);
            Vector3 wind = m_strike; // downwind
            Vector3 across = Vector3.Cross(Vector3.up, wind).normalized;
            float ridgeH = m_H;
            float ridgeW = ridgeH * R(1.3f, 1.8f);
            float gap = R(1.2f, 2.6f);
            bool corridor = ridges >= 2 && Chance(PassageChance);
            int corridorAfter = corridor ? m_rng.Next(0, ridges - 1) : -1;
            var lateral = new float[ridges];
            float x = 0f;
            for (int i = 0; i < ridges; i++)
            {
                lateral[i] = x;
                float g = gap * R(0.7f, 1.4f);
                if (i == corridorAfter) g = Mathf.Max(g, m_feat.passageWidth + 1.2f);
                x += ridgeW + g;
            }
            float shift = -(x - gap) * 0.5f;
            float windJ = R(-st.grainJitter, st.grainJitter);
            Vector3 w0 = Quaternion.AngleAxis(windJ, Vector3.up) * wind;
            if (corridor)
            {
                float off = shift + (lateral[corridorAfter] + ridgeW * 0.5f + lateral[corridorAfter + 1] - ridgeW * 0.5f) * 0.5f;
                CreatePassage(w0, off, m_feat.passageWidth, m_feat.passageHeadroom, ridgeH * 6f + 4f, "sand corridor");
            }
            for (int i = 0; i < ridges; i++)
            {
                Vector3 w = Quaternion.AngleAxis(R(-3f, 3f), Vector3.up) * w0;
                float h = ridgeH * R(0.65f, 1f);
                float len = h * R(5f, 9f) * R(0.55f, 1f);
                int segs = Mathf.Clamp(Mathf.RoundToInt(len / (h * 1.5f)), 3, 6);
                DmRockPieceInfo info = PickWhere(pc => !IsCluster(pc) && pc.Flatness <= 0.8f, DmRockPieceClassMask.Slab | DmRockPieceClassMask.Boulder, h * 2f);
                float step = R(0.38f, 0.45f);
                float sumAdv = 0f;
                for (int j = 0; j < segs; j++) sumAdv += (1f - 0.1f * j) * (j < segs - 1 ? step : 1f);
                float L0 = len / Mathf.Max(0.1f, sumAdv);
                Vector3 start = across * (shift + lateral[i]) - w * len * 0.5f + w * R(-1f, 1f) * h;
                float along = 0f;
                for (int j = 0; j < segs; j++)
                {
                    float t = segs > 1 ? j / (float)(segs - 1) : 0f;
                    float segLen = L0 * (1f - 0.1f * j);
                    float hh = h * Mathf.Lerp(1f, 0.42f, Mathf.Pow(t, 1.15f));
                    float ww = ridgeW * Mathf.Lerp(1f, 0.6f, t);
                    float sink = Mathf.Clamp(R(st.sink) + 0.12f * t, 0f, 0.6f);
                    float sy = segLen / Mathf.Max(0.1f, info.Long);
                    float sz = hh / (1f - sink) / Mathf.Max(0.05f, info.Short);
                    float sx = ww / Mathf.Max(0.1f, info.Mid);
                    float u = Mathf.Pow(sx * sy * sz, 1f / 3f);
                    float lim = st.maxStretch;
                    sx = Mathf.Clamp(sx, u / lim, u * lim); sy = Mathf.Clamp(sy, u / lim, u * lim); sz = Mathf.Clamp(sz, u / lim, u * lim);
                    var def = PcgPieceDeform.None;
                    bool tail = j == segs - 1;
                    if (tail) { def.tipScale = R(0.35f, 0.5f); def.taperPower = 1.2f; }
                    bool flip = !tail && Chance(0.5f); // vary the repeated piece; the tail keeps its taper downwind
                    Quaternion rot = FrameShort(Jitter(Vector3.up, 2f), flip ? -w : w);
                    Vector3 c = start + w * (along + segLen * 0.5f);
                    Req q = Base(info, new Vector3(sx, sy, sz), rot, XZ(c), Mode.Ground, j == 0 ? "ridge-head" : (tail ? "ridge-tail" : "ridge"));
                    q.deform = def; q.sink = sink; q.maxOverlap = 0.9f; q.minVisible = j == 0 ? 0.25f : 0.04f; q.tallFlag = false;
                    Place(q, 3, 0.15f);
                    along += segLen * step;
                }
            }
        }

        private void SharpClean()
        {
            // Tall sheared slabs: a tight bundle of thin plates stacked along the dip (short axes parallel, +-3 deg),
            // tallest in the middle stepping down outward, sharing one top-shear and one cleavage plane (hard planar cuts),
            // small blocks wedged at the foot (ground contact, never perched).
            DmRockStyle st = m_style;
            int count = Mathf.Min(10, R(st.pieceCount) + m_rng.Next(2, 5));
            float jit = st.alignmentDegrees;
            if (Chance(PassageChance))
                CreatePassage(m_dip, R(0.55f, 0.8f) * m_R * (Chance(0.5f) ? 1f : -1f), Mathf.Max(m_feat.passageWidth, R(1.6f, 2f)), m_feat.passageHeadroom, m_R + m_H + 2f, "cleft");
            if (RollNook()) CreateNook(Quaternion.AngleAxis(R(-60f, 60f), Vector3.up) * m_strike * (Chance(0.5f) ? 1f : -1f), m_R * R(0.6f, 0.8f));
            float plateT = m_H * R(0.13f, 0.18f);
            float plateW = m_H * R(0.24f, 0.34f);
            float pitch = plateT * R(0.55f, 0.7f);
            float half = (count - 1) * 0.5f;
            var plates = new List<Placed>();
            var order = new List<int>();
            for (int i = 0; i < count; i++) order.Add(i);
            order.Sort((x, y) => Mathf.Abs(x - half).CompareTo(Mathf.Abs(y - half)));
            foreach (int i in order)
            {
                float u = half > 0f ? (i - half) / half : 0f;           // -1..1 across the stack
                float h = m_H * (1f - 0.55f * Mathf.Pow(Mathf.Abs(u), 1.3f)) * R(0.86f, 1.04f);
                float sink = R(st.sink);
                DmRockPieceInfo info = Pick(DmRockPieceClassMask.Tall | DmRockPieceClassMask.Slab, h, 1f, 2.6f);
                Vector3 scale = Box(info, h / (1f - sink), plateW * R(0.8f, 1.25f), plateT * R(0.8f, 1.3f), 2.4f);
                Vector3 l = Quaternion.AngleAxis(R(-jit, jit), m_strike) * Quaternion.AngleAxis(R(-jit, jit), m_dip) * m_up;
                Quaternion rot = FrameLong(l, Quaternion.AngleAxis(R(-jit, jit), m_up) * m_dip);
                Vector2 xz = XZ(m_dip) * (i - half) * pitch + XZ(m_strike) * R(-0.3f, 0.3f) * plateW * (0.4f + Mathf.Abs(u));
                Req q = Base(info, scale, rot, xz, Mode.Ground, plates.Count == 0 ? "hero" : "mid");
                q.sink = sink; q.maxOverlap = 0.92f; q.minVisible = 0.04f;
                Placed p = Place(q, 4, 0.25f);
                if (p != null) plates.Add(p);
            }
            if (plates.Count == 0) return;
            int wedges = m_rng.Next(2, 5);
            for (int i = 0; i < wedges; i++)
            {
                Placed nb = plates[m_rng.Next(plates.Count)];
                float size = m_H * R(st.fillScale);
                DmRockPieceInfo info = Pick(DmRockPieceClassMask.Slab | DmRockPieceClassMask.Boulder | DmRockPieceClassMask.Small, size, 1f);
                Req q = Base(info, Uniform(info, size), FrameShort(Jitter(m_dip, jit * 2f), m_up), NextTo(nb, Az(m_strike) + (Chance(0.5f) ? 0f : 180f) + R(-50f, 50f), size * 0.25f), Mode.Ground, "wedge");
                q.sink = R(0.15f, 0.3f); q.maxOverlap = 0.6f;
                Place(q, 3, 0.4f);
            }
            // Shared fracture planes: one top-shear and one side-cleavage orientation for the whole formation.
            Vector3 topN = Quaternion.AngleAxis(R(-st.planeCutMaxAngle, st.planeCutMaxAngle), m_strike) * Quaternion.AngleAxis(R(-12f, 12f), m_dip) * m_up;
            Vector3 sideN = Quaternion.AngleAxis(R(-15f, 15f), Vector3.up) * (Chance(0.5f) ? m_strike : -m_strike);
            sideN = (sideN + Vector3.up * R(-0.15f, 0.15f)).normalized;
            foreach (Placed p in m_placed)
            {
                if (p.role != "hero" && p.role != "mid") continue;
                if (Chance(st.planeCutChance))
                {
                    Vector3 pt = new Vector3(p.com.x, p.bottomY + p.height * R(0.78f, 0.93f), p.com.z);
                    p.part.cuts.Add(new Plane(topN, pt));
                    S.cutsPlanned++;
                }
                if (Chance(st.planeCutChance * 0.5f))
                {
                    float ext = Vector3.Dot(p.bounds.extents, new Vector3(Mathf.Abs(sideN.x), Mathf.Abs(sideN.y), Mathf.Abs(sideN.z)));
                    Vector3 pt = p.com + sideN * ext * R(0.5f, 0.7f);
                    p.part.cuts.Add(new Plane(sideN, pt));
                    S.cutsPlanned++;
                }
            }
        }

        private void Shards()
        {
            DmRockStyle st = m_style;
            int seeds = R(st.shardSeeds);
            var seedPts = new List<Vector2> { Vector2.zero };
            for (int i = 1; i < seeds; i++) seedPts.Add(XZ(Dir(R(0f, 360f))) * R(1.8f, 3.5f));
            bool heroDone = false;
            for (int si = 0; si < seedPts.Count; si++)
            {
                int count = R(st.shardsPerSeed);
                float az0 = R(0f, 360f);
                for (int i = 0; i < count; i++)
                {
                    bool hero = !heroDone;
                    float len = hero ? m_H : m_H * (si == 0 ? R(st.midScale) : R(st.midScale) * 0.8f);
                    if (!hero && i >= count - 1 && Chance(0.5f)) len = m_H * R(0.12f, 0.22f);
                    float az = az0 + i * 360f / count + R(-25f, 25f);
                    Vector3 radial = Dir(az);
                    float lean = hero ? R(st.shardLean.x * 0.5f, st.shardLean.x) : R(st.shardLean);
                    Vector3 longDir = (Vector3.up * Mathf.Cos(lean * Mathf.Deg2Rad) + radial * Mathf.Sin(lean * Mathf.Deg2Rad)).normalized;
                    longDir = Jitter(longDir, 4f);
                    Quaternion rot = FrameLong(longDir, radial);
                    float stretch = R(st.shardStretch);
                    DmRockPieceInfo info = Pick(DmRockPieceClassMask.Tall, len / stretch, 1f, 2.6f);
                    float s = len / stretch / Mathf.Max(0.1f, info.Long);
                    Vector3 scale = new Vector3(s / Mathf.Sqrt(stretch), s * stretch, s / Mathf.Sqrt(stretch));
                    Vector3 bendF = Quaternion.Inverse(rot) * radial;
                    var def = new PcgPieceDeform
                    {
                        tipScale = st.shardTipScale * R(0.8f, 1.3f), taperPower = st.shardTaperPower,
                        bendDegrees = R(st.shardCurve), bendDirection = new Vector3(bendF.x, 0f, bendF.z),
                    };
                    Vector2 xz = seedPts[si] + XZ(radial) * R(0.1f, 0.5f) * Mathf.Max(0.4f, len * 0.12f);
                    string role = hero ? "hero-shard" : (len < m_H * 0.25f ? "splinter" : "shard");
                    Req q = Base(info, scale, rot, xz, Mode.Ground, role);
                    q.deform = def; q.sink = R(st.sink) * 0.5f;
                    q.maxOverlap = 0.7f; q.minVisible = 0.1f;
                    Placed p = Place(q, 5, 0.4f);
                    if (p == null) continue;
                    if (hero) heroDone = true;
                    if (Chance(0.45f))
                    {
                        // Jagged facet near the tip (oblique plane cut).
                        Vector3 axis = p.part.rotation * Vector3.up;
                        float L = p.part.info.size.y * p.part.scale.y;
                        Vector3 tip = p.part.FramePoint(new Vector3(0f, L * 0.5f, 0f));
                        Vector3 n = (Quaternion.AngleAxis(R(40f, 60f), Vector3.Cross(axis, Jitter(radial, 30f))) * axis).normalized;
                        p.part.cuts.Add(new Plane(n, tip - axis * L * R(0.06f, 0.12f)));
                        S.cutsPlanned++;
                    }
                }
            }
            int spl = m_rng.Next(2, 6);
            for (int i = 0; i < spl; i++)
            {
                Vector2 c = seedPts[m_rng.Next(seedPts.Count)];
                Vector3 radial = Dir(R(0f, 360f));
                float len = m_H * R(0.08f, 0.16f);
                DmRockPieceInfo info = Pick(DmRockPieceClassMask.Tall | DmRockPieceClassMask.Small, len, 1f, 2f);
                float s = len / Mathf.Max(0.1f, info.Long);
                float tilt = R(50f, 75f) * Mathf.Deg2Rad;
                Vector3 longDir = (Vector3.up * Mathf.Cos(tilt) + radial * Mathf.Sin(tilt)).normalized;
                Quaternion rot = FrameLong(longDir, radial);
                var def = new PcgPieceDeform { tipScale = st.shardTipScale * 1.5f, taperPower = st.shardTaperPower, bendDegrees = R(st.shardCurve) * 0.5f, bendDirection = Vector3.forward };
                Req q = Base(info, new Vector3(s * 0.75f, s * 1.4f, s * 0.75f), rot, c + XZ(radial) * R(1f, 2.5f), Mode.Ground, "splinter");
                q.deform = def; q.sink = R(0.15f, 0.3f); q.maxOverlap = 0.4f;
                Place(q, 3, 0.4f);
            }
        }

        private void Floating()
        {
            DmRockStyle st = m_style;
            m_res.noConform = true;
            float hover = Mathf.Max(R(st.hover), m_feat.passageHeadroom + 0.5f);
            PassageInfo under = CreatePassage(m_dip, 0f, Mathf.Max(m_feat.passageWidth, m_R * 0.8f), hover - 0.4f, m_R + 3f, "under-mass");
            float gTop = float.MinValue;
            foreach (KeepOut k in under.segments)
                if (Mathf.Abs(Vector3.Dot(k.center, m_dip)) < m_R + 1f) gTop = Mathf.Max(gTop, k.center.y + k.size.y * 0.5f);
            if (gTop == float.MinValue) gTop = hover;
            Placed mono = null;
            for (int t = 0; t < 4 && mono == null; t++)
            {
                DmRockPieceInfo info = Pick(DmRockPieceClassMask.Tall | DmRockPieceClassMask.Boulder, m_H, 0.5f, 1.6f);
                var def = new PcgPieceDeform { tipScale = R(0.25f, 0.4f), taperPower = 1.4f, invert = true };
                Quaternion rot = FrameLong(Jitter(Vector3.up, 4f), m_dip);
                Req q = Base(info, Uniform(info, m_H), rot, Vector2.zero, Mode.Free, "monolith");
                q.deform = def; q.freeY = gTop + 0.35f; q.skipOverlap = true; q.ignoreKeepOuts = true;
                mono = TryPlace(q);
            }
            if (mono == null) return;
            int sats = R(st.satellites);
            var sat = new List<Bounds>();
            for (int i = 0; i < sats; i++)
            {
                for (int t = 0; t < 5; t++)
                {
                    float size = m_H * R(0.08f, 0.22f);
                    DmRockPieceInfo info = Pick(DmRockPieceClassMask.Boulder | DmRockPieceClassMask.Small | DmRockPieceClassMask.Slab, size, 0.5f);
                    Vector3 d = Dir(R(0f, 360f));
                    float r = Mathf.Max(mono.bounds.extents.x, mono.bounds.extents.z) + R(0.6f, 2.8f);
                    float y = mono.bottomY + mono.height * R(-0.15f, 0.9f);
                    Quaternion rot = Quaternion.Euler(R(0f, 360f), R(0f, 360f), R(0f, 360f));
                    Req q = Base(info, Uniform(info, size), rot, XZ(d) * r, Mode.Free, "satellite");
                    q.freeY = Mathf.Max(y, gTop + 0.3f); q.skipOverlap = true;
                    Placed p = TryPlace(q);
                    if (p == null) continue;
                    bool hit = p.bounds.Intersects(mono.bounds);
                    foreach (Bounds b in sat) if (b.Intersects(p.bounds)) hit = true;
                    if (hit) { RemoveLast(); continue; }
                    sat.Add(p.bounds);
                    break;
                }
            }
            int ring = m_rng.Next(5, 11);
            for (int i = 0; i < ring; i++)
            {
                float size = m_H * R(0.05f, 0.14f);
                DmRockPieceInfo info = Pick(DmRockPieceClassMask.Small | DmRockPieceClassMask.Boulder, size);
                Vector2 xz = XZ(Dir(R(0f, 360f))) * (m_R * R(0.6f, 1.4f));
                Req q = Base(info, Uniform(info, size), Bedded(15f, 15f), xz, Mode.Ground, "ground-debris");
                q.maxOverlap = 0.2f;
                Place(q, 3, 0.6f);
            }
            m_res.log.Add($"floating: hover {mono.bottomY - GroundAt(0f, 0f):0.00} m, satellites {sat.Count}");
        }

        // ------------------------------------------------------------------------------------------------
        // Nooks: partly enclosed (>= 2 walls) or dropped.

        private void ResolveNooks()
        {
            foreach (NookInfo nk in new List<NookInfo>(m_res.nooks))
            {
                if (nk.box == null || !nk.box.active) continue;
                if (!ResolveNook(nk)) { nk.box.active = false; m_res.log.Add("nook dropped (not enclosed)"); }
            }
            m_res.nooks.RemoveAll(nk => nk.box != null && !nk.box.active);
            // Guarantee: nooks enabled but none survived -> carve one beside the biggest grounded pieces (still seeded).
            if (m_feat.guaranteeNook && NookChance > 0f && m_res.nooks.Count == 0 && NookFits()) LateNook();
        }

        /// <summary>Adds walls until the nook has >= 2 of back / left / right (3 passes, growing pieces); optional roof.</summary>
        private bool ResolveNook(NookInfo nk)
        {
            int start = m_placed.Count; // walls added here are the newest pieces -> rolled back if the nook fails
            Count(nk, out bool[] walls);
            for (int pass = 0; pass < 3 && nk.walls < 2; pass++)
            {
                for (int side = 0; side < 3; side++)
                {
                    if (walls[side] || nk.walls >= 2) continue;
                    Vector3 dir = side == 0 ? -(nk.rotation * Vector3.forward) : (side == 1 ? -(nk.rotation * Vector3.right) : nk.rotation * Vector3.right);
                    float size = nk.size.y * R(1.35f, 1.9f) * (1f + 0.15f * pass);
                    DmRockPieceInfo info = Pick(DmRockPieceClassMask.Boulder | DmRockPieceClassMask.Slab, size);
                    Vector3 at = nk.position + dir * (nk.size.x * 0.5f + size * (0.32f - 0.04f * pass));
                    Req q = Base(info, Uniform(info, size), Bedded(m_style.grainJitter, 10f), XZ(at), Mode.Ground, "nook-wall");
                    q.maxOverlap = 0.7f; q.minVisible = 0.08f; q.tallFlag = false;
                    Place(q, 4, 0.25f);
                    Count(nk, out walls);
                }
            }
            if (nk.walls < 2)
            {
                while (m_placed.Count > start) RemoveLast();
                Count(nk, out walls);
                return false;
            }
            if (!nk.roof && walls[1] && walls[2] && Chance(0.5f))
            {
                float len = nk.size.x + 1.2f;
                DmRockPieceInfo info = Pick(DmRockPieceClassMask.Slab, len);
                Req q = Base(info, Uniform(info, len), BeddedAlong(nk.rotation * Vector3.right, 5f, Vector3.up), XZ(nk.position), Mode.Rest, "nook-roof");
                q.lintel = true; q.sink = 0.05f; q.tier = 1;
                TryPlace(q);
                Count(nk, out walls);
            }
            S.nooksKept++;
            m_res.log.Add($"nook kept: walls {nk.walls} roof {nk.roof}");
            return true;
        }

        /// <summary>Guaranteed nook: a free box against the side of one of the biggest grounded pieces (that piece is the back wall).</summary>
        private void LateNook()
        {
            float b = NookBox;
            var cands = new List<Placed>();
            foreach (Placed p in m_placed) if (p.mode != Mode.Free && p.tier == 0 && p.height >= b * 0.9f) cands.Add(p);
            cands.Sort((a, c) => c.maxExtent.CompareTo(a.maxExtent));
            int tried = 0;
            for (int ci = 0; ci < cands.Count && ci < 4; ci++)
            {
                Placed hero = cands[ci];
                float az0 = R(0f, 360f);
                for (int k = 0; k < 8; k++)
                {
                    Vector3 outDir = Dir(az0 + k * 45f);
                    float foot = FootRadius(XZ(hero.com), XZ(outDir), hero.maxExtent + 2f);
                    if (foot <= 0f) continue;
                    Vector2 c = XZ(hero.com) + XZ(outDir) * (foot + b * 0.5f - 0.05f);
                    if (!BoxFree(c, outDir, b)) continue;
                    tried++;
                    NookInfo nk = CreateNookAt(c, outDir);
                    if (ResolveNook(nk)) { m_res.log.Add($"nook guaranteed beside {hero.role}"); return; }
                    nk.box.active = false;
                    m_res.nooks.Remove(nk);
                    if (tried >= 6) { m_res.log.Add("nook guarantee: no enclosable spot"); return; }
                }
            }
            m_res.log.Add("nook guarantee: no free spot");
        }

        private bool BoxFree(Vector2 c, Vector3 outward, float b)
        {
            Vector3 f = outward.normalized, r = Vector3.Cross(Vector3.up, f);
            for (float i = -0.35f; i <= 0.36f; i += 0.35f)
                for (float j = -0.35f; j <= 0.36f; j += 0.35f)
                {
                    Vector2 p = c + XZ(r) * (i * b) + XZ(f) * (j * b);
                    if (Cell(p.x, p.y) < 0 || Occupied(p, 0.05f)) return false;
                }
            float g = GroundAt(c);
            foreach (KeepOut ko in m_keepOuts)
                if (ko.active && ko.kind != 1 && ko.Contains(new Vector3(c.x, g + 0.5f, c.y), b * 0.5f)) return false;
            return true;
        }

        private void Count(NookInfo nk, out bool[] walls)
        {
            walls = new bool[3];
            float floor = nk.position.y;
            float need = floor + nk.size.y * 0.5f;
            Vector3 f = nk.rotation * Vector3.forward, r = nk.rotation * Vector3.right;
            float hx = nk.size.x * 0.5f + 0.25f;
            for (int side = 0; side < 3; side++)
            {
                int hits = 0, total = 0;
                for (float t = -0.4f; t <= 0.41f; t += 0.2f)
                {
                    Vector3 p = side == 0 ? nk.position - f * hx + r * (t * nk.size.x)
                              : nk.position + (side == 1 ? -r : r) * hx + f * (t * nk.size.z);
                    total++;
                    if (TopAt(XZ(p)) >= need) hits++;
                }
                walls[side] = hits >= total * 0.4f;
            }
            nk.walls = (walls[0] ? 1 : 0) + (walls[1] ? 1 : 0) + (walls[2] ? 1 : 0);
            int roofHits = 0;
            for (float a = -0.3f; a <= 0.31f; a += 0.3f)
                for (float b = -0.3f; b <= 0.31f; b += 0.3f)
                {
                    Vector3 p = nk.position + r * (a * nk.size.x) + f * (b * nk.size.z);
                    if (TopAt(XZ(p)) > floor + nk.size.y + 0.05f) roofHits++;
                }
            nk.roof = roofHits >= 3;
        }

        // ------------------------------------------------------------------------------------------------
        // Debris apron: settles at the foot (angle of repose), mostly downhill / lean side, biggest blocks farthest out.

        private void Apron(int count)
        {
            if (count <= 0 || m_placed.Count == 0) return;
            DmRockStyle st = m_style;
            Vector2 c = Vector2.zero; float wsum = 0f;
            foreach (Placed p in m_placed) { float w = p.maxExtent; c += XZ(p.com) * w; wsum += w; }
            c /= Mathf.Max(1e-4f, wsum);
            float gx = GroundAt(c.x + 2f, c.y) - GroundAt(c.x - 2f, c.y), gz = GroundAt(c.x, c.y + 2f) - GroundAt(c.x, c.y - 2f);
            Vector2 down = new Vector2(-gx, -gz);
            Vector2 bias = down.magnitude > 0.15f ? down.normalized : XZ(m_dip);
            if ((st.kind == DmRockStyle.StyleKind.Spire || st.kind == DmRockStyle.StyleKind.SharpClean) && m_lean > 2f) bias = XZ(m_dip);
            float H = 0f;
            foreach (Placed p in m_placed) H = Mathf.Max(H, p.bounds.max.y - GroundAt(XZ(p.com)));
            H = Mathf.Max(0.5f, Mathf.Min(H, m_H * 1.2f));
            float reach = Mathf.Max(0.8f, H * st.apronReach);
            float tanRepose = Mathf.Tan(st.reposeDegrees * Mathf.Deg2Rad);
            bool rockfall = Chance(st.rockfallChance);
            for (int i = 0; i < count + (rockfall ? 1 : 0); i++)
            {
                bool fall = rockfall && i == count;
                Vector2 dir = Chance(st.downhillBias) ? Rotate(bias, Gauss() * 45f) : XZ(Dir(R(0f, 360f)));
                dir.Normalize();
                float foot = FootRadius(c, dir, m_R * 2.5f + H);
                float u = fall ? R(1f, 1.5f) * H / reach : Mathf.Pow(R(), 0.85f);
                float dist = foot + u * reach;
                float size = fall ? H * st.apronScale.y * R(1.3f, 1.7f) : H * Mathf.Lerp(st.apronScale.x, st.apronScale.y, Mathf.Clamp01(u * 0.75f + R() * 0.35f));
                DmRockPieceInfo info = Pick(st.debrisClasses, size);
                Vector2 xz = c + dir * dist + InCircle() * 0.3f;
                Quaternion rot = st.kind == DmRockStyle.StyleKind.VolcanicColumnar
                    ? FrameLong(Jitter(X0Z(Rotate(dir, R(-60f, 60f))), 12f), Vector3.up)
                    : FrameShort(Jitter(Vector3.up, 22f), X0Z(Rotate(dir, R(-50f, 50f))));
                Mode mode = u < 0.3f && !fall ? Mode.Rest : Mode.Ground;
                Req q = Base(info, Uniform(info, size), rot, xz, mode, fall ? "rockfall" : "apron");
                q.tier = 2; q.maxOverlap = 0.3f; q.minVisible = 0.35f; q.tallFlag = false;
                q.sink = mode == Mode.Ground ? R(0.15f, 0.35f) : 0.08f;
                Placed p = TryPlace(q);
                if (p != null && mode == Mode.Rest)
                {
                    float above = p.bottomY - GroundAt(XZ(p.com));
                    float allowed = tanRepose * Mathf.Max(0f, reach - (Vector2.Distance(XZ(p.com), c) - foot));
                    if (above > allowed * 0.6f + 0.1f) { RemoveLast(); p = null; }
                }
                if (p == null)
                {
                    q.mode = Mode.Ground; q.sink = R(0.15f, 0.35f);
                    Place(q, 3, 0.4f);
                }
            }
        }

        private static Vector2 Rotate(Vector2 v, float deg)
        {
            float a = deg * Mathf.Deg2Rad, cs = Mathf.Cos(a), sn = Mathf.Sin(a);
            return new Vector2(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
        }

        private void Finish()
        {
            foreach (Placed p in m_placed)
            {
                if (p.supporters.Count >= 2 && !p.cap)
                {
                    // Spanning piece (lintel, caprock, roof): compare with the combined support (span between the
                    // supporters' tops plus their top widths), not with one tall supporter.
                    float span = 0f, wsum = 0f; int any = 0;
                    for (int i = 0; i < p.supporters.Count; i++)
                    {
                        int a = p.supporters[i];
                        if (a < 0 || a >= m_placed.Count) continue;
                        wsum += m_placed[a].topWidth; any++;
                        for (int j = i + 1; j < p.supporters.Count; j++)
                        {
                            int b = p.supporters[j];
                            if (b < 0 || b >= m_placed.Count) continue;
                            span = Mathf.Max(span, Vector2.Distance(XZ(m_placed[a].topCenter), XZ(m_placed[b].topCenter)));
                        }
                    }
                    float support = span + (any > 0 ? wsum / any : 0f);
                    if (any >= 2 && p.maxExtent > 1.2f * Mathf.Max(0.05f, support))
                    {
                        S.bigOnThinViolations++;
                        m_res.log.Add($"bigOnThin: spanning {p.role} ({p.maxExtent:0.00} m) on combined support {support:0.00} m");
                    }
                    continue;
                }
                foreach (int sid in p.supporters)
                {
                    if (sid < 0 || sid >= m_placed.Count) continue;
                    Placed s = m_placed[sid];
                    if (!s.tall || p.cap) continue;
                    if (p.maxExtent > 1.2f * Mathf.Max(0.05f, s.topWidth))
                    {
                        S.bigOnThinViolations++;
                        m_res.log.Add($"bigOnThin: {p.role} {(p.info != null ? p.info.prefab.name : "?")} ({p.maxExtent:0.00} m) on {s.role} top {s.topWidth:0.00} m");
                    }
                }
            }
            S.passagesKept = 0;
            foreach (PassageInfo p in m_res.passages) if (p.kept) S.passagesKept++;
            foreach (Placed p in m_placed)
                m_res.log.Add($"{p.id:00} {p.role,-12} {(p.info != null ? p.info.prefab.name : "fixed"),-18} {p.mode,-6} h {p.height:0.00} w {p.maxExtent:0.00} sup {p.supporters.Count} wr {p.widthRatio:0.00}{(p.cap ? " cap" : "")}{(p.tall ? " tall" : "")}");
        }
    }
}

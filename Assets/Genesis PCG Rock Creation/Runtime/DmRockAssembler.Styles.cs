using System.Collections.Generic;
using UnityEngine;

namespace GenesisPCG.RockCreation
{
    public sealed partial class DmRockAssembler
    {
        private float PassageChance => m_feat.useStyleChances ? m_style.passageChance : m_feat.passageChance;
        private float NookChance => m_feat.useStyleChances ? m_style.nookChance : m_feat.nookChance;

        private PassageInfo CreatePassage(Vector3 dir, float offset, float width, float headroom, float halfLength, string purpose = "passage")
        {
            dir = new Vector3(dir.x, 0f, dir.z).normalized;
            Vector3 across = Vector3.Cross(Vector3.up, dir).normalized;
            Vector3 c = across * offset;
            var p = new PassageInfo { width = width, headroom = headroom, purpose = purpose };
            float segLen = 2f;
            int segs = Mathf.Max(1, Mathf.CeilToInt(halfLength * 2f / segLen));
            segLen = halfLength * 2f / segs;
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
            for (int i = 0; i < segs; i++)
            {
                float t0 = -halfLength + i * segLen, t1 = t0 + segLen;
                float gmin = float.MaxValue, gmax = float.MinValue;
                for (float t = t0; t <= t1 + 1e-3f; t += 0.5f)
                    for (int s = -1; s <= 1; s++)
                    {
                        Vector3 q = c + dir * t + across * (s * width * 0.5f);
                        float g = GroundAt(q.x, q.z);
                        gmin = Mathf.Min(gmin, g); gmax = Mathf.Max(gmax, g);
                    }
                float bottom = gmin - 0.3f, top = gmax + headroom;
                var ko = new KeepOut
                {
                    kind = 0, rotation = rot,
                    center = c + dir * ((t0 + t1) * 0.5f) + Vector3.up * ((bottom + top) * 0.5f),
                    size = new Vector3(width, top - bottom, segLen + 0.02f),
                };
                p.segments.Add(ko);
                m_keepOuts.Add(ko);
            }
            p.a = c - dir * halfLength; p.a.y = GroundAt(p.a.x, p.a.z);
            p.b = c + dir * halfLength; p.b.y = GroundAt(p.b.x, p.b.z);
            m_res.passages.Add(p);
            S.passagesRequested++;
            m_res.log.Add($"passage ({purpose}) width {width:0.00} headroom {headroom:0.00} dir {Az(dir):0}deg offset {offset:0.00}");
            return p;
        }

        private NookInfo CreateNook(Vector3 outward, float dist)
        {
            outward = new Vector3(outward.x, 0f, outward.z).normalized;
            return CreateNookAt(XZ(outward * dist), outward);
        }

        private float NookBox => Mathf.Max(0.5f, m_feat.crateSize * 1.4f);
        private bool NookFits() => m_R >= NookBox * 1.2f && m_H >= NookBox * 0.9f;

        /// <summary>
        /// Nook roll during the build (seeded, one random number). Deliberately NOT forced: an early nook reserves its pocket
        /// before the pieces are placed and would hollow out the formation's core. The guarantee
        /// (<see cref="DmRockFeatureSettings.guaranteeNook"/>) is applied after the build instead, by carving a nook against
        /// the side of one of the biggest grounded pieces (see LateNook).
        /// </summary>
        private bool RollNook() => Chance(NookChance) && NookChance > 0f;

        private NookInfo CreateNookAt(Vector2 cxz, Vector3 outward)
        {
            outward = new Vector3(outward.x, 0f, outward.z).normalized;
            float b = NookBox;
            Vector3 c = X0Z(cxz);
            float g = GroundAt(c.x, c.z);
            float gmin = g, gmax = g;
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    float gg = GroundAt(c.x + i * b * 0.5f, c.z + j * b * 0.5f);
                    gmin = Mathf.Min(gmin, gg); gmax = Mathf.Max(gmax, gg);
                }
            float bottom = gmin - 0.2f, top = gmax + b;
            var ko = new KeepOut
            {
                kind = 1, rotation = Quaternion.LookRotation(outward, Vector3.up),
                center = new Vector3(c.x, (bottom + top) * 0.5f, c.z),
                size = new Vector3(b, top - bottom, b),
            };
            m_keepOuts.Add(ko);
            var n = new NookInfo { position = new Vector3(c.x, g, c.z), rotation = ko.rotation, size = new Vector3(b, b, b), box = ko };
            m_res.nooks.Add(n);
            S.nooksRequested++;
            return n;
        }

        /// <summary>Default passage (through the middle, along <paramref name="dir"/>) and nook rolls.</summary>
        private void GenericFeatures(Vector3 dir, float width = 0f)
        {
            if (Chance(PassageChance))
            {
                float w = Mathf.Max(m_feat.passageWidth, width);
                CreatePassage(dir, R(-0.2f, 0.2f) * m_R, w, m_feat.passageHeadroom, m_R + m_H * m_style.apronReach + 2f);
            }
            if (RollNook())
            {
                Vector3 outDir = Quaternion.AngleAxis(R(-70f, 70f), Vector3.up) * m_dip;
                CreateNook(outDir, m_R * R(0.5f, 0.75f));
            }
        }

        private Req Base(DmRockPieceInfo info, Vector3 scale, Quaternion rot, Vector2 xz, Mode mode, string role)
        {
            var q = new Req { info = info, scale = scale, rot = rot, xz = xz, mode = mode, role = role };
            q.sink = mode == Mode.Ground ? R(m_style.sink) : R(m_style.overlap) * 0.35f;
            q.maxOverlap = Mathf.Max(0.35f, m_style.overlap.y + 0.2f);
            return q;
        }

        /// <summary>A position next to <paramref name="nb"/> at azimuth <paramref name="azDeg"/>, overlapping by the style's overlap.</summary>
        private Vector2 NextTo(Placed nb, float azDeg, float myRadius)
        {
            Vector3 d = Dir(azDeg);
            Vector2 c = new Vector2(nb.com.x, nb.com.z);
            float rN = FootRadius(c, XZ(d), nb.maxExtent + 1f);
            if (rN <= 0f) rN = nb.maxExtent * 0.4f;
            float ov = R(m_style.overlap);
            return c + XZ(d) * (rN + myRadius * (1f - 2f * ov));
        }

        private Placed Hero(DmRockPieceClassMask mask, float fitH, Quaternion rot, Vector2 xz, float targetLong = 0f, float aspect = 0f)
        {
            Placed best = null;
            for (int t = 0; t < 4 && best == null; t++)
            {
                DmRockPieceInfo info = Pick(mask, targetLong > 0f ? targetLong : fitH, -1f, aspect);
                Req q = Base(info, Vector3.one, rot, xz, Mode.Ground, "hero");
                q.fitHeight = fitH;
                best = Place(q, 4, 0.8f);
            }
            return best;
        }

        // ------------------------------------------------------------------------------------------------

        private void BoulderPile()
        {
            DmRockStyle st = m_style;
            int n = R(st.pieceCount);
            Placed hero = Hero(st.heroClasses, m_H * R(0.75f, 0.92f), Bedded(st.grainJitter), Vector2.zero, m_H * 1.25f);
            if (hero == null) return;
            float heroSize = hero.maxExtent;
            int mids = Mathf.Max(1, Mathf.RoundToInt((n - 1) * st.midShare));
            float az0 = R(0f, 360f);
            var big = new List<Placed> { hero };
            for (int i = 0; i < mids; i++)
            {
                float size = heroSize * R(st.midScale);
                DmRockPieceInfo info = Pick(st.midClasses, size);
                float az = az0 + i * 360f / mids + R(-35f, 35f);
                Req q = Base(info, Uniform(info, size), Bedded(st.grainJitter), Vector2.zero, Mode.Ground, "mid");
                q.xz = NextTo(hero, az, size * 0.4f);
                q.nominal = heroSize;
                Placed p = Place(q);
                if (p != null) big.Add(p);
            }
            int fills = Mathf.Max(0, n - 1 - mids);
            for (int i = 0; i < fills; i++)
            {
                float size = heroSize * R(st.fillScale);
                DmRockPieceInfo info = Pick(st.fillClasses, size);
                Placed p = null;
                if (st.maxTiers >= 2 && big.Count >= 2 && Chance(0.35f))
                {
                    // Nestle into the crotch between two big pieces (upper tier, <= upperMaxRatio of the smaller one).
                    Placed a = big[m_rng.Next(big.Count)], b = big[m_rng.Next(big.Count)];
                    if (a != b)
                    {
                        float lim = Mathf.Min(a.maxExtent, b.maxExtent) * st.upperMaxRatio;
                        float s2 = Mathf.Min(size, lim);
                        Vector2 xz = Vector2.Lerp(XZ(a.com), XZ(b.com), R(0.4f, 0.6f)) + InCircle() * 0.3f;
                        Req q = Base(info, Uniform(info, s2), Bedded(st.grainJitter, 15f), xz, Mode.Rest, "fill-top");
                        q.nominal = heroSize; q.tier = 1;
                        p = Place(q, 4, 0.4f);
                    }
                }
                if (p == null)
                {
                    Placed nb = big[m_rng.Next(big.Count)];
                    Req q = Base(info, Uniform(info, size), Bedded(st.grainJitter, 10f), NextTo(nb, R(0f, 360f), size * 0.4f), Mode.Ground, "fill");
                    q.nominal = heroSize;
                    Place(q);
                }
            }
        }

        /// <summary>Non-uniform frame scale hitting the long length exactly; mid / short limited to 1/lim..lim of it.</summary>
        private static Vector3 Box(DmRockPieceInfo info, float longL, float midL, float shortL, float lim)
        {
            float sy = longL / Mathf.Max(0.1f, info.Long);
            float sx = midL / Mathf.Max(0.1f, info.Mid);
            float sz = shortL / Mathf.Max(0.05f, info.Short);
            sx = Mathf.Clamp(sx, sy / lim, sy * lim);
            sz = Mathf.Clamp(sz, sy / lim, sy * lim);
            return new Vector3(sx, sy, sz);
        }

        private void Outcrop()
        {
            // ONE continuous bedrock shelf (research rules 1, 4, 8): a dominant flattened mass along strike, sunk deep so
            // it reads as bedrock emerging from the ground; shoulder blocks share the SAME bedding frame (one grain, often
            // the same piece) and push 35-55 % into it along strike, stepping down outward; a thin set-back upper bed;
            // small blocks tucked into the seams on the down-dip face; a few toe fragments down-dip. Apron adds talus.
            DmRockStyle st = m_style;
            int n = R(st.pieceCount);
            var mask = DmRockPieceClassMask.Slab | DmRockPieceClassMask.Boulder;
            float ledgeH = m_H * R(0.55f, 0.7f);
            float heroLen = m_R * R(1.3f, 1.6f);
            DmRockPieceInfo hinfo = PickWhere(pc => pc.Flatness <= 0.75f, mask, heroLen * 0.7f);
            Quaternion grain = Bedded(1.5f);
            Vector3 nrm = grain * Vector3.forward;   // bedding normal (piece short axis)
            Vector3 along = grain * Vector3.up;      // piece long axis ~ strike
            Vector2 strike2 = new Vector2(along.x, along.z);
            strike2 = strike2.sqrMagnitude > 1e-4f ? strike2.normalized : XZ(m_strike);
            Vector2 dip2 = XZ(m_dip);
            Quaternion Grain(float yawJ) => Quaternion.AngleAxis(R(-yawJ, yawJ) + (Chance(0.5f) ? 180f : 0f), nrm) * grain;

            float sinkH = R(Mathf.Max(0.35f, st.sink.x), Mathf.Max(0.42f, st.sink.y));
            Vector3 hs = Box(hinfo, heroLen, heroLen * R(0.5f, 0.62f), ledgeH / (1f - sinkH), 2.2f);
            Req hq = Base(hinfo, hs, grain, Vector2.zero, Mode.Ground, "hero");
            hq.sink = sinkH; hq.maxOverlap = 0.8f; hq.minVisible = 0.1f;
            Placed hero = Place(hq, 5, 0.4f);
            if (hero == null) return;

            int shoulders = Mathf.Clamp(Mathf.RoundToInt(n * 0.45f), 2, 4);
            var body = new List<Placed> { hero };
            Placed[] ends = { hero, hero };
            float[] hEnd = { ledgeH, ledgeH };
            for (int i = 0; i < shoulders; i++)
            {
                int side = i % 2;
                Vector2 dir = strike2 * (side == 0 ? -1f : 1f);
                Placed nb = ends[side];
                float edge = FootRadius(XZ(nb.com), dir, nb.maxExtent + 1f);
                if (edge <= 0f) edge = nb.maxExtent * 0.45f;
                float hgt = hEnd[side] * R(0.68f, 0.85f);
                float len = heroLen * R(0.55f, 0.75f) * (nb == hero ? 1f : 0.85f);
                float ov = R(0.35f, 0.55f);
                DmRockPieceInfo info = Chance(0.6f) ? hinfo : PickWhere(pc => pc.Flatness <= 0.75f, mask, len * 0.7f);
                float sink = R(st.sink.x, st.sink.y);
                Vector3 scale = Box(info, len, len * R(0.55f, 0.7f), hgt / (1f - sink), 2.2f);
                Vector2 xz = XZ(nb.com) + dir * (edge + len * (0.5f - ov)) + dip2 * (R(-0.12f, 0.2f) * len);
                Req q = Base(info, scale, Grain(3f), xz, Mode.Ground, "shoulder");
                q.sink = sink; q.maxOverlap = 0.85f; q.minVisible = 0.08f; q.nominal = hero.maxExtent;
                Placed p = Place(q, 4, 0.25f);
                if (p == null) continue;
                body.Add(p); ends[side] = p; hEnd[side] = hgt;
                // Seam: a small block pushed into the junction on the down-dip face hides the join.
                Vector2 seam = XZ(nb.com) + dir * edge;
                float face = FootRadius(seam, dip2, hero.maxExtent + 1f);
                float ss = ledgeH * R(0.35f, 0.55f);
                DmRockPieceInfo si = Pick(mask | DmRockPieceClassMask.Small, ss);
                Req sq = Base(si, Uniform(si, ss), Grain(8f), seam + dip2 * (face + ss * 0.05f), Mode.Ground, "seam");
                sq.sink = R(0.3f, 0.45f); sq.maxOverlap = 0.75f; sq.minVisible = 0.12f; sq.tallFlag = false; sq.nominal = hero.maxExtent;
                Place(sq, 3, 0.25f);
            }

            int upper = Mathf.Clamp(n - 1 - shoulders, 1, 2);
            for (int i = 0; i < upper; i++)
            {
                Placed b = i == 0 ? hero : body[m_rng.Next(body.Count)];
                float len = b.maxExtent * R(0.45f, 0.65f);
                DmRockPieceInfo info = PickWhere(pc => pc.Flatness <= 0.6f, mask, len * 0.7f);
                // Thin, wide bed in the same grain, set back up-dip (stepped shelf, not a block on a block).
                Vector3 scale = Box(info, len, len * R(0.6f, 0.8f), Mathf.Max(0.25f, b.height * R(0.22f, 0.3f)), 2.6f);
                Vector2 xz = XZ(b.topCenter) - dip2 * (b.topWidth * R(0.12f, 0.28f));
                Req q = Base(info, scale, Grain(2f), xz, Mode.Rest, "upper-bed");
                q.nominal = b.maxExtent; q.tier = 1; q.maxOverlap = 0.7f; q.sink = R(0.15f, 0.25f); q.minVisible = 0.15f;
                Place(q, 4, 0.3f);
            }

            int toes = m_rng.Next(1, 4);
            for (int i = 0; i < toes; i++)
            {
                Placed b = body[m_rng.Next(body.Count)];
                float size = ledgeH * R(0.25f, 0.45f);
                DmRockPieceInfo info = Pick(mask | DmRockPieceClassMask.Small, size);
                Req q = Base(info, Uniform(info, size), Grain(12f), NextTo(b, Az(m_dip) + R(-35f, 35f), size * 0.45f), Mode.Ground, "toe");
                q.nominal = hero.maxExtent; q.tallFlag = false;
                Place(q, 3, 0.5f);
            }
        }

        private void Spire()
        {
            DmRockStyle st = m_style;
            int n = R(st.pieceCount);
            Placed hero = Hero(DmRockPieceClassMask.Tall, m_H, Upright(2f, m_dip, 180f), Vector2.zero, m_H * 1.1f, 2.4f);
            if (hero == null) return;
            float heroW = hero.bounds.size.x * 0.5f + hero.bounds.size.z * 0.5f;
            int sibs = Mathf.Max(1, n - 1);
            float az0 = R(0f, 360f);
            for (int i = 0; i < sibs; i++)
            {
                float hgt = m_H * R(st.midScale);
                DmRockPieceInfo info = Pick(DmRockPieceClassMask.Tall, hgt * 1.1f, -1f, 2f);
                float az = az0 + i * 360f / sibs + R(-40f, 40f);
                Req q = Base(info, Vector3.one, Upright(3f, m_dip, 180f), Vector2.zero, Mode.Ground, "sibling");
                q.fitHeight = hgt;
                q.xz = XZ(hero.com) + XZ(Dir(az)) * (heroW * 0.45f + hgt * 0.18f + R(-0.2f, 1.6f));
                q.maxOverlap = 0.3f;
                Place(q);
            }
            int rocks = m_rng.Next(1, 3);
            for (int i = 0; i < rocks; i++)
            {
                float size = m_H * R(0.22f, 0.38f);
                DmRockPieceInfo info = Pick(DmRockPieceClassMask.Boulder | DmRockPieceClassMask.Slab, size);
                Req q = Base(info, Uniform(info, size), Bedded(st.grainJitter, 10f), NextTo(hero, Az(m_dip) + R(-80f, 80f), size * 0.35f), Mode.Ground, "base-rock");
                Place(q);
            }
        }

        private void Arch()
        {
            DmRockStyle st = m_style;
            float span = Mathf.Max(2.5f, R(st.archSpan));
            float clear = Mathf.Max(3f, R(st.archHeight));
            bool walk = Chance(PassageChance);
            float headroom = Mathf.Max(m_feat.passageHeadroom, clear);
            float width = Mathf.Max(m_feat.passageWidth, span - 0.3f);
            PassageInfo pass = CreatePassage(m_dip, 0f, width, headroom, m_R + m_H * st.apronReach + 2f, walk ? "passage" : "arch-opening");
            float openTop = float.MinValue;
            foreach (KeepOut k in pass.segments)
                if (Mathf.Abs(Vector3.Dot(k.center, m_dip)) < 2.5f) openTop = Mathf.Max(openTop, k.center.y + k.size.y * 0.5f);
            if (openTop == float.MinValue) openTop = headroom;

            float thick = Mathf.Max(0.25f, st.lintelThicknessRatio) * span;
            var legs = new List<Placed>();
            for (int side = -1; side <= 1; side += 2)
            {
                Placed leg = null;
                for (int t = 0; t < 4 && leg == null; t++)
                {
                    Vector3 at = m_strike * side * (width * 0.5f + 0.9f + 0.25f * t);
                    float g = GroundAt(at.x, at.z);
                    float visH = openTop - g + thick * 0.45f + 0.3f;
                    if (legs.Count == 1) visH = Mathf.Max(visH, legs[0].bounds.max.y - g); // matching leg tops so the lintel seats on both
                    DmRockPieceInfo info = Pick(DmRockPieceClassMask.Tall, visH * 1.2f, -1f, 2.2f);
                    Req q = Base(info, Vector3.one, FrameLong(Jitter(Vector3.up, 2f), m_dip), XZ(at), Mode.Ground, "leg");
                    q.sink = R(0.25f, 0.4f);
                    q.fitHeight = visH;
                    q.maxOverlap = 0.5f;
                    leg = Place(q, 5, 0.3f);
                }
                if (leg != null) legs.Add(leg);
            }
            if (legs.Count < 2) { m_res.log.Add("arch: legs failed"); return; }

            Placed lintel = null;
            float needLen = Vector2.Distance(XZ(legs[0].topCenter), XZ(legs[1].topCenter)) + (legs[0].topWidth + legs[1].topWidth) * 0.35f + 0.4f;
            Vector2 mid = (XZ(legs[0].topCenter) + XZ(legs[1].topCenter)) * 0.5f;
            for (int t = 0; t < 6 && lintel == null; t++)
            {
                float len = needLen * Mathf.Pow(1.12f, t);
                DmRockPieceInfo info = Pick(t < 3 ? DmRockPieceClassMask.Slab : DmRockPieceClassMask.Boulder | DmRockPieceClassMask.Slab, len);
                float sy = len / Mathf.Max(0.1f, info.Long);
                float sz = thick / Mathf.Max(0.05f, info.Short);
                float u = Mathf.Sqrt(sy * sz);
                const float lim = 1.55f;
                sy = Mathf.Clamp(sy, u / lim, u * lim); sz = Mathf.Clamp(sz, u / lim, u * lim);
                float sx = Mathf.Clamp(u, Mathf.Max(sy, sz) / lim, Mathf.Min(sy, sz) * lim);
                Quaternion rot = FrameShort(Jitter(Vector3.up, 2f), m_strike);
                Req q = Base(info, new Vector3(sx, sy, sz), rot, mid, Mode.Rest, "lintel");
                q.lintel = true; q.sink = 0.08f; q.tier = 1; q.maxOverlap = 0.6f;
                lintel = TryPlace(q);
                if (lintel == null)
                {
                    // Seat it just above the opening, embedded into both leg tops.
                    float legTop = Mathf.Min(legs[0].bounds.max.y, legs[1].bounds.max.y);
                    q.mode = Mode.Free; q.freeY = Mathf.Max(openTop + 0.1f, legTop - thick * 0.55f);
                    q.skipOverlap = true;
                    lintel = TryPlace(q);
                    if (lintel != null)
                    {
                        lintel.supporters.Add(legs[0].id); lintel.supporters.Add(legs[1].id);
                        if (legTop < lintel.bottomY + 0.05f) m_res.log.Add("arch: lintel above leg tops (gap)");
                    }
                }
                if (lintel != null && !SeatedOnBoth(lintel, legs)) { RemoveLast(); lintel = null; m_res.log.Add("arch: lintel try " + t + " not seated on both legs"); }
            }
            if (lintel != null && !SeatedOnBoth(lintel, legs))
            {
                RemoveLast();
                lintel = null;
                m_res.log.Add("arch: lintel not seated on both legs -> removed");
            }
            if (lintel != null)
                m_res.log.Add($"arch: span {span:0.00} clear {clear:0.00} lintel thickness {lintel.height:0.00} ({lintel.height / span:0.00} x span)");
            else m_res.log.Add("arch: lintel failed");

            int extra = m_rng.Next(2, 5);
            for (int i = 0; i < extra; i++)
            {
                Placed leg = legs[i % 2];
                float side = Vector3.Dot(leg.com, m_strike) >= 0f ? 1f : -1f;
                float size = leg.height * R(0.35f, 0.6f);
                DmRockPieceInfo info = Pick(DmRockPieceClassMask.Boulder | DmRockPieceClassMask.Slab, size);
                Vector2 xz = XZ(leg.com) + XZ(m_strike) * side * (leg.topWidth * 0.5f + size * 0.3f) + XZ(m_dip) * R(-0.6f, 0.6f);
                Req q = Base(info, Uniform(info, size), BeddedAlong(m_strike, 6f, Vector3.up), xz, Mode.Ground, "fin");
                Place(q);
            }
            if (!walk)
            {
                // Blind arch: a block behind the opening (window, not a walk-through).
                pass.kept = false;
                foreach (KeepOut k in pass.segments) if (Vector3.Dot(k.center, m_dip) < -1.5f) k.active = false;
                float size = span * 0.8f;
                DmRockPieceInfo info = Pick(DmRockPieceClassMask.Boulder, size);
                Req q = Base(info, Uniform(info, size), Bedded(5f), XZ(-m_dip * 1.8f), Mode.Ground, "back-block");
                Place(q);
            }
        }

        private static bool SeatedOnBoth(Placed lintel, List<Placed> legs)
        {
            foreach (Placed leg in legs)
            {
                if (leg.bounds.max.y < lintel.bottomY + 0.05f) return false;
                Bounds b = lintel.bounds; b.Expand(new Vector3(0.1f, 100f, 0.1f));
                Vector3 tc = leg.topCenter; tc.y = b.center.y;
                if (!b.Contains(tc)) return false;
            }
            return true;
        }

        private void Rubble()
        {
            // Clustered rubble (research 3a): 2-4 anchor boulders, most pieces gathered around them with size falling off
            // with distance, a thin scatter between the clusters; power-law sizes (many small, few mid), one layer, half-buried.
            DmRockStyle st = m_style;
            int n = R(st.pieceCount);
            int clusters = m_rng.Next(2, 5);
            var centres = new List<Vector2>();
            var radii = new List<float>();
            for (int i = 0; i < clusters; i++)
            {
                Vector2 c = Vector2.zero;
                for (int t = 0; t < 8; t++)
                {
                    c = InCircle() * m_R * 0.65f;
                    bool ok = true;
                    foreach (Vector2 o in centres) if (Vector2.Distance(o, c) < m_R * 0.5f) ok = false;
                    if (ok) break;
                }
                float size = m_H * (i == 0 ? R(0.8f, 1f) : R(0.5f, 0.8f));
                bool first = i == 0;
                DmRockPieceInfo info = PickWhere(pc => first || !IsCluster(pc), st.heroClasses, size);
                Req q = Base(info, Uniform(info, size), Bedded(st.grainJitter, 10f), c, Mode.Ground, "anchor");
                q.sink = R(0.25f, 0.4f); q.maxOverlap = 0.3f; q.minVisible = 0.4f; q.tallFlag = false;
                Placed p = Place(q, 4, 0.6f);
                if (p != null) { centres.Add(XZ(p.com)); radii.Add(Mathf.Max(p.bounds.extents.x, p.bounds.extents.z)); }
            }
            if (centres.Count == 0) { centres.Add(Vector2.zero); radii.Add(0.3f); }
            for (int i = 0; i < n; i++)
            {
                int k = Chance(0.75f) ? (Chance(0.45f) ? 0 : m_rng.Next(centres.Count)) : -1;
                Vector2 xz;
                float fall;
                if (k >= 0)
                {
                    float d = radii[k] * R(0.7f, 1f) + Mathf.Abs(Gauss()) * m_R * 0.22f;
                    xz = centres[k] + XZ(Dir(R(0f, 360f))) * d;
                    fall = Mathf.Clamp01((d - radii[k]) / (m_R * 0.5f));
                }
                else { xz = InCircle() * m_R; fall = 1f; }
                float size = m_H * Mathf.Lerp(0.09f, 0.45f, Mathf.Pow(R(), 2.2f)) * Mathf.Lerp(1.15f, 0.6f, fall);
                DmRockPieceInfo info = Pick(st.fillClasses, size);
                Req q = Base(info, Uniform(info, size), Bedded(st.grainJitter, 12f), xz, Mode.Ground, "rubble");
                q.sink = R(st.sink); q.maxOverlap = 0.3f; q.minVisible = 0.45f; q.tallFlag = false;
                Place(q, 3, 0.4f);
            }
        }

        private void Columnar()
        {
            DmRockStyle st = m_style;
            float D = R(0.85f, 1.25f);
            float spacing = D * 0.9f;
            int want = R(st.columnCount);
            var sites = new List<Vector2>();
            var ring = new List<int>();
            sites.Add(Vector2.zero); ring.Add(0);
            Vector2[] ax = { new Vector2(1, 0), new Vector2(0.5f, 0.8660254f), new Vector2(-0.5f, 0.8660254f), new Vector2(-1, 0), new Vector2(-0.5f, -0.8660254f), new Vector2(0.5f, -0.8660254f) };
            for (int k = 1; sites.Count < want * 1.25f && k < 8; k++)
                for (int side = 0; side < 6; side++)
                    for (int j = 0; j < k; j++)
                    {
                        Vector2 p = ax[side] * k + ax[(side + 2) % 6] * j;
                        sites.Add(p * spacing); ring.Add(k);
                    }
            Quaternion yaw = Quaternion.AngleAxis(R(0f, 60f), Vector3.up);
            int rings = ring[ring.Count - 1];
            float rc = Mathf.Max(spacing, rings * spacing);
            if (RollNook())
            {
                Vector3 od = Quaternion.AngleAxis(R(-60f, 60f), Vector3.up) * m_dip;
                CreateNook(od, Mathf.Max(spacing, rc - spacing * 0.5f));
            }
            if (Chance(PassageChance))
                CreatePassage(m_strike, R(-0.3f, 0.3f) * rc, m_feat.passageWidth, m_feat.passageHeadroom, rc + 4f);
            float tiltPlane = R(-0.25f, 0.25f);
            float stepUnit = R(st.columnStep.x, Mathf.Max(st.columnStep.x, st.columnStep.y * 0.6f));
            int placed = 0;
            for (int i = 0; i < sites.Count && placed < want; i++)
            {
                if (i > 0 && Chance(st.missingColumnChance) && ring[i] >= rings - 1) continue;
                Vector3 p3 = yaw * X0Z(sites[i]);
                Vector2 xz = XZ(p3) + InCircle() * D * 0.06f;
                float rn = xz.magnitude / rc;
                float top = m_H * (1f - 0.5f * Mathf.Pow(rn, 1.6f)) + Vector2.Dot(xz, XZ(m_dip)) * tiltPlane;
                top += Mathf.Round(R(-1f, 1f) * 2f) * 0.5f * stepUnit + R(-0.08f, 0.08f);
                top = Mathf.Max(D * 1.2f, top);
                float d = D * R(0.87f, 1.13f);
                DmRockPieceInfo info = Pick(DmRockPieceClassMask.Tall, top * 1.2f, -1f, top / d);
                float sxz = d / Mathf.Max(0.1f, info.Mid);
                float sink = R(st.sink);
                float sy = (top / (1f - sink)) / Mathf.Max(0.1f, info.Long);
                float lim = st.maxStretch;
                if (sy > sxz * lim) sy = sxz * lim;
                if (sy < sxz / lim) sy = sxz / lim;
                Vector3 fan = rn > 0.01f ? X0Z(xz.normalized) : Vector3.zero;
                Vector3 l = Jitter((m_up + fan * Mathf.Tan(st.columnFan * rn * Mathf.Deg2Rad)).normalized, 1.5f);
                Quaternion rot = FrameLong(l, Quaternion.AngleAxis(60f * m_rng.Next(6) + R(-5f, 5f), Vector3.up) * m_dip);
                Req q = Base(info, new Vector3(sxz, sy, sxz * R(0.95f, 1.05f)), rot, xz, Mode.Ground, "column");
                q.sink = sink;
                q.maxOverlap = 0.8f; q.minVisible = 0.02f;
                if (TryPlace(q) != null) placed++;
            }
            m_res.log.Add($"columnar: {placed} columns, diameter {D:0.00}, rings {rings}");
        }

        private void Ejecta()
        {
            // Impact / ballistic field (research 3a): one dominant block, tilted and set deep, with a rim of half-buried
            // spall pushed up around it; 2-4 secondary blocks near the vent; then a radial field whose density falls
            // ~1/r^2 and size ~1/r out to the footprint radius (optional fan sector), tilted 10-30 deg, 30-60 % buried.
            DmRockStyle st = m_style;
            int n = R(st.pieceCount);
            float half = st.ejectaFanDegrees * 0.5f;
            float FanAz() => m_az + (half >= 179f ? R(0f, 360f) : R(-half, half));
            if (Chance(PassageChance)) CreatePassage(m_strike, R(-0.3f, 0.3f) * m_R, m_feat.passageWidth, m_feat.passageHeadroom, m_R + 2f);

            float heroSize = m_H * R(1.5f, 1.9f);
            DmRockPieceInfo hinfo = Pick(st.heroClasses, heroSize, -1f, 1.2f);
            Req hq = Base(hinfo, Uniform(hinfo, heroSize), FrameShort(Jitter(Vector3.up, R(12f, 25f)), Dir(R(0f, 360f))), Vector2.zero, Mode.Ground, "hero");
            hq.sink = R(0.3f, 0.42f); hq.maxOverlap = 0.5f; hq.minVisible = 0.3f;
            Placed hero = Place(hq, 5, 0.5f);
            Vector2 c0 = hero != null ? XZ(hero.com) : Vector2.zero;
            float heroR = hero != null ? Mathf.Max(hero.bounds.extents.x, hero.bounds.extents.z) : heroSize * 0.4f;

            int rim = m_rng.Next(5, 9);
            float az0 = R(0f, 360f);
            for (int i = 0; i < rim; i++)
            {
                Vector3 radial = Dir(az0 + i * 360f / rim + R(-18f, 18f));
                float size = heroSize * R(0.1f, 0.2f);
                DmRockPieceInfo info = Pick(st.fillClasses, size);
                float tilt = R(20f, 35f) * Mathf.Deg2Rad;
                Vector3 nrm = (Vector3.up * Mathf.Cos(tilt) + radial * Mathf.Sin(tilt)).normalized; // shoved up and outward
                Req q = Base(info, Uniform(info, size), FrameShort(nrm, X0Z(Rotate(XZ(radial), 90f))), c0 + XZ(radial) * heroR * R(0.95f, 1.25f), Mode.Ground, "rim");
                q.sink = R(0.35f, 0.55f); q.maxOverlap = 0.5f; q.minVisible = 0.2f; q.tallFlag = false;
                Place(q, 3, 0.3f);
            }

            int mids = m_rng.Next(2, 5);
            for (int i = 0; i < mids; i++)
            {
                float size = heroSize * R(0.3f, 0.5f);
                DmRockPieceInfo info = Pick(st.heroClasses | st.fillClasses, size);
                Vector2 xz = c0 + XZ(Dir(FanAz())) * heroR * R(1.7f, 3.2f);
                Req q = Base(info, Uniform(info, size), FrameShort(Jitter(Vector3.up, R(10f, 30f)), Dir(R(0f, 360f))), xz, Mode.Ground, "secondary");
                q.sink = R(0.3f, 0.5f); q.maxOverlap = 0.35f; q.minVisible = 0.35f; q.tallFlag = false;
                Place(q, 3, 0.5f);
            }

            int field = Mathf.Max(8, n - rim - mids);
            float r0 = heroR * 1.6f, r1 = Mathf.Max(r0 * 2.5f, m_R);
            for (int i = 0; i < field; i++)
            {
                float r = r0 * Mathf.Pow(r1 / r0, R());                       // log-uniform radius: density ~ 1/r^2
                float size = Mathf.Max(0.18f, heroSize * 0.4f * Mathf.Pow(r0 / r, 0.85f) * R(0.7f, 1.3f)); // size ~ 1/r
                DmRockPieceInfo info = Pick(st.fillClasses, size);
                Req q = Base(info, Uniform(info, size), FrameShort(Jitter(Vector3.up, R(10f, 30f)), Dir(R(0f, 360f))), c0 + XZ(Dir(FanAz())) * r, Mode.Ground, "ejecta");
                q.sink = R(st.sink); q.maxOverlap = 0.35f; q.minVisible = 0.4f; q.tallFlag = false;
                Place(q, 3, 0.4f);
            }
        }
    }
}

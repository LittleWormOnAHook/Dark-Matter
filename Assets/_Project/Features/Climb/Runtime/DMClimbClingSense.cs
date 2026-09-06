using UnityEngine;

namespace Project.Features.Climb
{
    /// <summary>
    /// Body-centered climb awareness: overlap bubble + short ray fan.
    /// Classifies face / soffit / walkable ground / lip depth / side faces for free climb.
    /// Not a grab-hold graph — Dune-style volume feel.
    /// </summary>
    public sealed class DMClimbClingSense
    {
        public struct Sample
        {
            public bool valid;
            public Vector3 origin;

            public bool hasFace;
            public RaycastHit faceHit;
            public Vector3 faceNormal;

            public bool hasSoffit;
            public RaycastHit soffitHit;

            public bool hasWalkableBelow;
            public RaycastHit groundHit;
            public float groundDist;

            public bool hasLip;
            public RaycastHit lipHit;
            public float lipProtrusion;
            public bool isStubLip;
            public bool isDeepLip;

            public bool hasSideL;
            public bool hasSideR;
            public RaycastHit sideLHit;
            public RaycastHit sideRHit;

            // Full-sphere sense fan (every SphereStepDeg) for free-surface awareness.
            public int sphereHitCount;
            public int sphereRayCount;
            public float sphereRange;
        }

        private readonly RaycastHit[] _hits = new RaycastHit[48];
        private readonly Collider[] _overlap = new Collider[32];
        // Gizmo / debug: last sphere-fan hit points (capped).
        private readonly Vector3[] _sphereHitPts = new Vector3[96];
        private readonly Vector3[] _sphereHitNrm = new Vector3[96];
        private readonly byte[] _sphereHitKind = new byte[96]; // 1 face, 2 soffit, 3 ground/lip, 4 other
        private int _sphereHitStored;

        public float BubbleRadius { get; set; } = 1.35f;
        public float RayRange { get; set; } = 1.9f;
        /// <summary>Angular step for full-sphere fan (degrees). 20 => ~162 directions.</summary>
        public float SphereStepDeg { get; set; } = 20f;
        /// <summary>Off by default — directed probes only. 360 fan was noisy for mantle/grab.</summary>
        public bool EnableSphereFan { get; set; } = false;
        public float DeepLipMeters { get; set; } = 0.65f;
        public float WalkMaxSlopeDeg { get; set; } = 45f;
        public float ClimbMinSlopeDeg { get; set; } = 55f;

        public Sample Last { get; private set; }
        public int SphereHitStored => _sphereHitStored;
        public Vector3 GetSphereHitPoint(int i) => (i >= 0 && i < _sphereHitStored) ? _sphereHitPts[i] : Vector3.zero;
        public Vector3 GetSphereHitNormal(int i) => (i >= 0 && i < _sphereHitStored) ? _sphereHitNrm[i] : Vector3.up;
        public byte GetSphereHitKind(int i) => (i >= 0 && i < _sphereHitStored) ? _sphereHitKind[i] : (byte)0;

        public Sample Refresh(
            Transform body,
            Vector3 wallNormal,
            LayerMask climbMask,
            string climbTag,
            float handHeight,
            float standOff,
            System.Func<RaycastHit, bool> isSelf,
            System.Func<RaycastHit, bool> isClimbable)
        {
            Sample s = default;
            s.valid = body != null;
            if (!s.valid)
            {
                Last = s;
                return s;
            }

            Vector3 n = wallNormal.sqrMagnitude > 0.0001f ? wallNormal.normalized : -body.forward;
            Vector3 into = -n;
            Vector3 flatN = Vector3.ProjectOnPlane(n, Vector3.up);
            if (flatN.sqrMagnitude < 0.0001f)
                flatN = Vector3.ProjectOnPlane(-body.forward, Vector3.up);
            if (flatN.sqrMagnitude > 0.0001f)
                flatN.Normalize();
            else
                flatN = body.forward;

            Vector3 right = Vector3.Cross(Vector3.up, into);
            if (right.sqrMagnitude < 0.0001f)
                right = body.right;
            right.Normalize();

            Vector3 wallUp = Vector3.ProjectOnPlane(Vector3.up, n);
            if (wallUp.sqrMagnitude < 0.0001f)
                wallUp = Vector3.up;
            wallUp.Normalize();

            float hh = Mathf.Max(0.6f, handHeight);
            s.origin = body.position + Vector3.up * (hh * 0.55f);
            float radius = Mathf.Clamp(BubbleRadius, 0.55f, 2.2f);
            float range = Mathf.Clamp(RayRange, 0.6f, 3.0f);

            // Volume presence (debug / future soft push); rays do the classification.
            Physics.OverlapSphereNonAlloc(s.origin, radius, _overlap, climbMask.value != 0 ? climbMask : ~0, QueryTriggerInteraction.Ignore);

            Vector3 head = body.position + Vector3.up * (hh + 0.28f);
            Vector3 chest = s.origin;

            // --- Face (into wall) from chest + head + hips ---
            TryBestClimbable(chest, into, range, 0.16f, climbMask, isSelf, isClimbable, ClimbMinSlopeDeg, 180f,
                out s.hasFace, out s.faceHit, out s.faceNormal);
            if (!s.hasFace)
                TryBestClimbable(head, into, range, 0.14f, climbMask, isSelf, isClimbable, ClimbMinSlopeDeg, 180f,
                    out s.hasFace, out s.faceHit, out s.faceNormal);
            if (!s.hasFace)
                TryBestClimbable(body.position + Vector3.up * 0.35f, into, range, 0.16f, climbMask, isSelf, isClimbable, ClimbMinSlopeDeg, 180f,
                    out s.hasFace, out s.faceHit, out s.faceNormal);
            // Angled face rays (more lines).
            if (!s.hasFace)
            {
                Vector3[] angled =
                {
                    (into + wallUp * 0.35f).normalized,
                    (into - wallUp * 0.25f).normalized,
                    (into + right * 0.4f).normalized,
                    (into - right * 0.4f).normalized,
                    (into + wallUp * 0.5f + right * 0.35f).normalized,
                    (into + wallUp * 0.5f - right * 0.35f).normalized,
                };
                for (int a = 0; a < angled.Length && !s.hasFace; a++)
                    TryBestClimbable(chest, angled[a], range, 0.12f, climbMask, isSelf, isClimbable, ClimbMinSlopeDeg, 180f,
                        out s.hasFace, out s.faceHit, out s.faceNormal);
            }
            if (s.hasFace && s.faceNormal.sqrMagnitude < 0.0001f)
                s.faceNormal = s.faceHit.normal.normalized;

            // --- Soffit / nothing-above check from head + chest ---
            Vector3[] soffitOrigins =
            {
                head + flatN * 0.05f,
                chest + flatN * 0.05f,
                head + flatN * 0.2f,
                head - flatN * 0.05f + right * 0.15f,
                head - flatN * 0.05f - right * 0.15f,
            };
            for (int i = 0; i < soffitOrigins.Length; i++)
            {
                if (SphereCastFirst(soffitOrigins[i], Vector3.up, range * 0.95f, 0.07f, climbMask, isSelf, out RaycastHit soff)
                    && isClimbable(soff)
                    && soff.normal.y < -0.12f)
                {
                    s.hasSoffit = true;
                    s.soffitHit = soff;
                    break;
                }
            }
            // Also detect walkable deck directly above/out (ledge top without soffit normal).
            if (!s.hasSoffit)
            {
                Vector3 deckProbe = head + flatN * 0.25f + Vector3.up * 0.15f;
                if (RayFirst(deckProbe, Vector3.down, 0.95f, ~0, isSelf, out RaycastHit deck)
                    && Vector3.Angle(Vector3.up, deck.normal) <= WalkMaxSlopeDeg
                    && deck.point.y > body.position.y + hh * 0.35f)
                {
                    // Treat as lip support presence via soffit-or-deck flag using lip path below.
                    // Mark soffit false but ensure lip can see this deck: stash as soft lip hint.
                }
            }

            // --- Walkable below ---
            Vector3 feet = body.position + Vector3.up * 0.08f;
            if (RayFirst(feet, Vector3.down, 1.15f, ~0, isSelf, out RaycastHit ground)
                && Vector3.Angle(Vector3.up, ground.normal) <= WalkMaxSlopeDeg
                && ground.distance <= 0.95f)
            {
                s.hasWalkableBelow = true;
                s.groundHit = ground;
                s.groundDist = ground.distance;
            }

            // --- Lip / top shelf (cast forward-up then down) ---
            Vector3 lipProbe = body.position + Vector3.up * hh + flatN * 0.08f;
            // Prefer lip nearest hand height (not highest deck) so we don't aim way above/past the rim.
            bool lipFound = false;
            RaycastHit bestLip = default;
            float bestScore = float.MaxValue;
            float handY = body.position.y + hh;
            float[] overs = { 0.08f, 0.16f, 0.28f, 0.42f, 0.6f };
            float[] ups = { 0.05f, 0.18f, 0.32f, 0.48f };
            for (int u = 0; u < ups.Length; u++)
            {
                for (int o = 0; o < overs.Length; o++)
                {
                    Vector3 origin = lipProbe + Vector3.up * ups[u] + flatN * overs[o];
                    if (!RayFirst(origin, Vector3.down, 0.7f, ~0, isSelf, out RaycastHit lip))
                        continue;
                    if (Vector3.Angle(Vector3.up, lip.normal) > 60f)
                        continue;
                    if (lip.point.y < body.position.y + 0.25f)
                        continue;
                    // Reject lips far above hands (that was "way past / too high").
                    if (lip.point.y > handY + 0.55f)
                        continue;
                    float score = Mathf.Abs(lip.point.y - handY) + overs[o] * 0.15f;
                    if (!lipFound || score < bestScore)
                    {
                        bestLip = lip;
                        bestScore = score;
                        lipFound = true;
                    }
                }
            }

            if (lipFound)
            {
                s.hasLip = true;
                s.lipHit = bestLip;
                // Outward depth from face hit (or body) along flat wall normal.
                Vector3 faceRef = s.hasFace ? s.faceHit.point : body.position;
                Vector3 toLip = bestLip.point - faceRef;
                float along = Vector3.Dot(toLip, flatN);
                s.lipProtrusion = Mathf.Max(0f, along);
                s.isDeepLip = s.lipProtrusion >= DeepLipMeters;
                s.isStubLip = s.hasLip && !s.isDeepLip;
            }

            // --- Sphere fan (optional; off for mantle-simple-v2) ---
            if (EnableSphereFan)
            {
                // --- Full sphere fan: one ray every SphereStepDeg, all directions, chest + head ---
                // Fills gaps the directed probes miss (wraps, backs, odd soffits, ground shelves).
                float step = Mathf.Clamp(SphereStepDeg, 10f, 45f);
                float sphereRange = Mathf.Clamp(range, 1.0f, 2.0f);
                s.sphereRange = sphereRange;
                _sphereHitStored = 0;
                int rayCount = 0;
                float bestFaceDist = s.hasFace ? s.faceHit.distance : float.MaxValue;
                float bestSoffitDist = s.hasSoffit ? s.soffitHit.distance : float.MaxValue;
                float bestGroundDist = s.hasWalkableBelow ? s.groundDist : float.MaxValue;
                float bestSideLDist = float.MaxValue;
                float bestSideRDist = float.MaxValue;

                // Pitch -90..90, yaw 0..360-step. At poles, one ray only.
                for (float pitch = -90f; pitch <= 90.01f; pitch += step)
                {
                    float pitchRad = pitch * Mathf.Deg2Rad;
                    float cosP = Mathf.Cos(pitchRad);
                    float sinP = Mathf.Sin(pitchRad);
                    bool pole = Mathf.Abs(pitch) >= 89.5f;
                    for (float yaw = 0f; yaw < 359.9f; yaw += step)
                    {
                        if (pole && yaw > 0.01f)
                            break;
                        float yawRad = yaw * Mathf.Deg2Rad;
                        Vector3 dir = new Vector3(
                            Mathf.Sin(yawRad) * cosP,
                            sinP,
                            Mathf.Cos(yawRad) * cosP);
                        if (dir.sqrMagnitude < 0.0001f)
                            continue;
                        dir.Normalize();
                        rayCount++;

                        // Chest sample
                        if (Physics.Raycast(chest, dir, out RaycastHit hit, sphereRange, climbMask.value != 0 ? climbMask : ~0, QueryTriggerInteraction.Ignore)
                            && hit.collider != null && !isSelf(hit))
                        {
                            ClassifySphereHit(ref s, hit, dir, into, right, isClimbable, ref bestFaceDist, ref bestSoffitDist, ref bestGroundDist, ref bestSideLDist, ref bestSideRDist);
                        }

                        // Head sample (skip near-identical down pole)
                        if (!pole || pitch > 0f)
                        {
                            rayCount++;
                            if (Physics.Raycast(head, dir, out RaycastHit hitH, sphereRange, climbMask.value != 0 ? climbMask : ~0, QueryTriggerInteraction.Ignore)
                                && hitH.collider != null && !isSelf(hitH))
                            {
                                ClassifySphereHit(ref s, hitH, dir, into, right, isClimbable, ref bestFaceDist, ref bestSoffitDist, ref bestGroundDist, ref bestSideLDist, ref bestSideRDist);
                            }
                        }
                    }
                }
                s.sphereRayCount = rayCount;
                s.sphereHitCount = _sphereHitStored;

            }
            else
            {
                _sphereHitStored = 0;
                s.sphereHitCount = 0;
                s.sphereRayCount = 0;
                s.sphereRange = 0f;
            }

            // --- Side faces (corners) ---
            TryBestClimbable(s.origin + right * 0.2f, (into + right * 0.85f).normalized, range, 0.12f, climbMask, isSelf, isClimbable, ClimbMinSlopeDeg, 180f,
                out s.hasSideR, out s.sideRHit, out _);
            TryBestClimbable(s.origin - right * 0.2f, (into - right * 0.85f).normalized, range, 0.12f, climbMask, isSelf, isClimbable, ClimbMinSlopeDeg, 180f,
                out s.hasSideL, out s.sideLHit, out _);

            Last = s;
            return s;
        }

        private void ClassifySphereHit(
            ref Sample s,
            RaycastHit hit,
            Vector3 dir,
            Vector3 into,
            Vector3 right,
            System.Func<RaycastHit, bool> isClimbable,
            ref float bestFaceDist,
            ref float bestSoffitDist,
            ref float bestGroundDist,
            ref float bestSideLDist,
            ref float bestSideRDist)
        {
            float ang = Vector3.Angle(Vector3.up, hit.normal);
            byte kind = 4;
            bool climbable = isClimbable(hit);

            // Walkable / lip shelf
            if (ang <= WalkMaxSlopeDeg)
            {
                kind = 3;
                // Ground under feet hemisphere
                if (dir.y < -0.35f && hit.distance < bestGroundDist && hit.distance <= 1.05f)
                {
                    bestGroundDist = hit.distance;
                    s.hasWalkableBelow = true;
                    s.groundHit = hit;
                    s.groundDist = hit.distance;
                }
                // Lip / deck — sphere may see far shelves; only promote to hasLip when near.
                if (dir.y > -0.15f && hit.point.y > s.origin.y - 0.15f && hit.distance <= 1.15f)
                {
                    if (!s.hasLip || hit.distance < s.lipHit.distance + 0.05f)
                    {
                        Vector3 flatInto = Vector3.ProjectOnPlane(into, Vector3.up);
                        if (flatInto.sqrMagnitude > 0.0001f)
                            flatInto.Normalize();
                        float along = Vector3.Dot(hit.point - s.origin, flatInto.sqrMagnitude > 0.0001f ? flatInto : into);
                        if (along > -0.1f)
                        {
                            s.hasLip = true;
                            s.lipHit = hit;
                            Vector3 faceRef = s.hasFace ? s.faceHit.point : s.origin;
                            float protrude = Mathf.Max(0f, Vector3.Dot(hit.point - faceRef, flatInto.sqrMagnitude > 0.0001f ? flatInto : into));
                            s.lipProtrusion = protrude;
                            s.isDeepLip = protrude >= DeepLipMeters;
                            s.isStubLip = s.hasLip && !s.isDeepLip;
                        }
                    }
                }
            }
            // Soffit — only mark for movement gating when close above (sphere foresight still stores hit).
            else if (hit.normal.y < -0.12f && climbable && hit.distance < bestSoffitDist)
            {
                kind = 2;
                if (dir.y > 0.2f && hit.distance <= 1.05f)
                {
                    bestSoffitDist = hit.distance;
                    s.hasSoffit = true;
                    s.soffitHit = hit;
                }
            }
            // Climbable face / sides
            else if (climbable && ang >= ClimbMinSlopeDeg)
            {
                float intoDot = Vector3.Dot(dir, into);
                float sideDot = Vector3.Dot(dir, right);
                if (intoDot > 0.35f && hit.distance < bestFaceDist)
                {
                    kind = 1;
                    bestFaceDist = hit.distance;
                    s.hasFace = true;
                    s.faceHit = hit;
                    s.faceNormal = hit.normal.normalized;
                }
                else if (sideDot > 0.45f && hit.distance < bestSideRDist)
                {
                    kind = 1;
                    bestSideRDist = hit.distance;
                    s.hasSideR = true;
                    s.sideRHit = hit;
                }
                else if (sideDot < -0.45f && hit.distance < bestSideLDist)
                {
                    kind = 1;
                    bestSideLDist = hit.distance;
                    s.hasSideL = true;
                    s.sideLHit = hit;
                }
                else if (!s.hasFace && intoDot > 0.05f && hit.distance < bestFaceDist + 0.35f)
                {
                    kind = 1;
                    if (hit.distance < bestFaceDist)
                    {
                        bestFaceDist = hit.distance;
                        s.hasFace = true;
                        s.faceHit = hit;
                        s.faceNormal = hit.normal.normalized;
                    }
                }
            }

            if (_sphereHitStored < _sphereHitPts.Length)
            {
                _sphereHitPts[_sphereHitStored] = hit.point;
                _sphereHitNrm[_sphereHitStored] = hit.normal;
                _sphereHitKind[_sphereHitStored] = kind;
                _sphereHitStored++;
            }
        }

        private void TryBestClimbable(
            Vector3 origin,
            Vector3 dir,
            float range,
            float radius,
            LayerMask mask,
            System.Func<RaycastHit, bool> isSelf,
            System.Func<RaycastHit, bool> isClimbable,
            float minSlopeFromUp,
            float maxSlopeFromUp,
            out bool found,
            out RaycastHit best,
            out Vector3 normal)
        {
            found = false;
            best = default;
            normal = Vector3.zero;
            if (dir.sqrMagnitude < 0.0001f)
                return;
            dir.Normalize();

            int count = radius > 0.001f
                ? Physics.SphereCastNonAlloc(origin, radius, dir, _hits, range, mask.value != 0 ? mask : ~0, QueryTriggerInteraction.Ignore)
                : Physics.RaycastNonAlloc(origin, dir, _hits, range, mask.value != 0 ? mask : ~0, QueryTriggerInteraction.Ignore);

            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit h = _hits[i];
                if (h.collider == null || isSelf(h) || !isClimbable(h))
                    continue;
                float ang = Vector3.Angle(Vector3.up, h.normal);
                if (ang < minSlopeFromUp || ang > maxSlopeFromUp)
                    continue;
                if (h.distance < bestDist)
                {
                    bestDist = h.distance;
                    best = h;
                    normal = h.normal.normalized;
                    found = true;
                }
            }
        }

        private bool SphereCastFirst(
            Vector3 origin,
            Vector3 dir,
            float range,
            float radius,
            LayerMask mask,
            System.Func<RaycastHit, bool> isSelf,
            out RaycastHit hit)
        {
            hit = default;
            if (dir.sqrMagnitude < 0.0001f)
                return false;
            dir.Normalize();
            int count = Physics.SphereCastNonAlloc(origin, radius, dir, _hits, range, mask.value != 0 ? mask : ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            bool any = false;
            for (int i = 0; i < count; i++)
            {
                if (_hits[i].collider == null || isSelf(_hits[i]))
                    continue;
                if (_hits[i].distance < best)
                {
                    best = _hits[i].distance;
                    hit = _hits[i];
                    any = true;
                }
            }
            return any;
        }

        private static bool RayFirst(
            Vector3 origin,
            Vector3 dir,
            float range,
            LayerMask mask,
            System.Func<RaycastHit, bool> isSelf,
            out RaycastHit hit)
        {
            hit = default;
            if (!Physics.Raycast(origin, dir, out RaycastHit h, range, mask, QueryTriggerInteraction.Ignore))
                return false;
            if (isSelf(h))
                return false;
            hit = h;
            return true;
        }
    }
}
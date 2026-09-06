using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Features.Climb
{
    /// <summary>
    /// Serialized grab probes on a climbable prefab root (AAA hold style).
    /// Baked by Dark Matter Genesis / Climb / Probe Baker.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMClimbProbeSet : MonoBehaviour
    {
        public enum ProbeType
        {
            Face = 0,
            Lip = 1,
            Mantle = 2,
            Hang = 3,
        }

        public enum HandSide
        {
            None = 0,
            Left = 1,
            Right = 2,
        }

        [Serializable]
        public struct Probe
        {
            public Vector3 localPosition;
            public Vector3 localNormal;
            public float radius;
            public ProbeType type;
            /// <summary>True when placed/edited manually in the Probe Baker (always gets Scene handles).</summary>
            public bool isManual;
            /// <summary>Bake pair id (>=0). Left/Right of the same sample share pairId. -1 = unpaired / manual.</summary>
            public int pairId;
            public HandSide hand;
        }

        [SerializeField] private List<Probe> probes = new List<Probe>();
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.85f, 1f, 0.85f);
        [SerializeField] private Color selectedGizmoColor = new Color(1f, 0.15f, 0.12f, 0.95f);
        [SerializeField] private float gizmoScale = 1f;

        [Header("Optional bake sources")]
        [SerializeField] private MeshFilter[] bakeMeshFilters;
        [SerializeField] private Collider[] bakeColliders;

        /// <summary>Editor-only: panel / Scene selection index for gizmo highlight. Not serialized.</summary>
        [NonSerialized] public int EditorSelectedIndex = -1;

        public IReadOnlyList<Probe> Probes => probes;
        public int Count => probes != null ? probes.Count : 0;

        /// <summary>True when surface is steep enough to climb (not a walkable top). Default 75deg matches DMClimbProfile.walkMaxSlopeDeg.</summary>
        public static bool IsClimbableProbeNormal(Vector3 worldNormal, float walkableMaxSlopeDeg = 50f)
        {
            if (worldNormal.sqrMagnitude < 0.0001f)
                return false;
            return Vector3.Angle(Vector3.up, worldNormal.normalized) > walkableMaxSlopeDeg;
        }


        public Color GizmoColor { get => gizmoColor; set => gizmoColor = value; }
        public Color SelectedGizmoColor { get => selectedGizmoColor; set => selectedGizmoColor = value; }
        public float GizmoScale { get => gizmoScale; set => gizmoScale = Mathf.Max(0.05f, value); }
        public MeshFilter[] BakeMeshFilters => bakeMeshFilters;
        public Collider[] BakeColliders => bakeColliders;

        public void AddProbe(Probe probe)
        {
            if (probes == null)
                probes = new List<Probe>();
            if (probe.radius <= 0.001f)
                probe.radius = 0.12f;
            if (probe.localNormal.sqrMagnitude < 0.0001f)
                probe.localNormal = Vector3.forward;
            else
                probe.localNormal = probe.localNormal.normalized;
            probes.Add(probe);
        }

        public void AddProbe(Vector3 localPosition, Vector3 localNormal, float radius, ProbeType type, bool isManual = false, int pairId = -1, HandSide hand = HandSide.None)
        {
            AddProbe(new Probe
            {
                localPosition = localPosition,
                localNormal = localNormal,
                radius = radius,
                type = type,
                isManual = isManual,
                pairId = pairId,
                hand = hand,
            });
        }

        /// <summary>Find the paired probe (other hand) sharing pairId. Returns false if unpaired or partner missing.</summary>
        public bool TryGetPairPartner(int index, out int partnerIndex)
        {
            partnerIndex = -1;
            if (probes == null || index < 0 || index >= probes.Count)
                return false;
            int pid = probes[index].pairId;
            if (pid < 0)
                return false;
            for (int i = 0; i < probes.Count; i++)
            {
                if (i == index)
                    continue;
                if (probes[i].pairId != pid)
                    continue;
                partnerIndex = i;
                return true;
            }
            return false;
        }

        /// <summary>
        /// World poses for the Left/Right stance pair containing index.
        /// Mid is the L/R midpoint (body attach). Returns false if unpaired or partner missing.
        /// </summary>
        public bool TryGetPairWorldPoses(
            int index,
            out Vector3 leftPos,
            out Vector3 leftN,
            out Vector3 rightPos,
            out Vector3 rightN,
            out Vector3 midPos,
            out Vector3 midN)
        {
            leftPos = default;
            leftN = Vector3.up;
            rightPos = default;
            rightN = Vector3.up;
            midPos = default;
            midN = Vector3.up;
            if (!TryGetPairPartner(index, out int partner))
                return false;

            Probe a = probes[index];
            Probe b = probes[partner];
            int li = a.hand == HandSide.Left ? index : (b.hand == HandSide.Left ? partner : index);
            int ri = a.hand == HandSide.Right ? index : (b.hand == HandSide.Right ? partner : partner);
            // If neither tagged Left/Right, keep index as left and partner as right.
            if (a.hand != HandSide.Left && b.hand != HandSide.Left && a.hand != HandSide.Right && b.hand != HandSide.Right)
            {
                li = index;
                ri = partner;
            }

            if (!GetWorldPose(li, out leftPos, out leftN, out _, out _))
                return false;
            if (!GetWorldPose(ri, out rightPos, out rightN, out _, out _))
                return false;

            midPos = (leftPos + rightPos) * 0.5f;
            Vector3 nSum = leftN + rightN;
            midN = nSum.sqrMagnitude > 0.0001f ? nSum.normalized : leftN;
            return true;
        }

        /// <summary>Nearest matched Left+Right pair whose midpoint is within maxDistance of worldPoint.</summary>
        public bool FindNearestPair(Vector3 worldPoint, float maxDistance, out int leftIndex, out int rightIndex, out Vector3 midPos)
        {
            leftIndex = -1;
            rightIndex = -1;
            midPos = default;
            if (probes == null || probes.Count == 0)
                return false;

            float maxSqr = maxDistance > 0f ? maxDistance * maxDistance : float.MaxValue;
            float bestSqr = maxSqr;
            var seen = new HashSet<int>();
            for (int i = 0; i < probes.Count; i++)
            {
                Probe a = probes[i];
                if (a.pairId < 0 || !seen.Add(a.pairId))
                    continue;
                if (!TryGetPairPartner(i, out int j))
                    continue;
                Probe b = probes[j];
                int li = a.hand == HandSide.Left ? i : (b.hand == HandSide.Left ? j : i);
                int ri = a.hand == HandSide.Right ? i : (b.hand == HandSide.Right ? j : j);
                if (!GetWorldPose(li, out Vector3 lp, out _, out _, out _))
                    continue;
                if (!GetWorldPose(ri, out Vector3 rp, out _, out _, out _))
                    continue;
                Vector3 mid = (lp + rp) * 0.5f;
                float sqr = (mid - worldPoint).sqrMagnitude;
                if (sqr > bestSqr)
                    continue;
                bestSqr = sqr;
                leftIndex = li;
                rightIndex = ri;
                midPos = mid;
            }
            return leftIndex >= 0 && rightIndex >= 0;
        }

        public bool SetProbe(int index, Probe probe)
        {
            if (probes == null || index < 0 || index >= probes.Count)
                return false;
            if (probe.radius <= 0.001f)
                probe.radius = 0.12f;
            if (probe.localNormal.sqrMagnitude < 0.0001f)
                probe.localNormal = Vector3.forward;
            else
                probe.localNormal = probe.localNormal.normalized;
            probes[index] = probe;
            return true;
        }

        public bool SetProbeWorldPose(int index, Vector3 worldPos, Vector3 worldNormal, bool markManual = false)
        {
            if (probes == null || index < 0 || index >= probes.Count)
                return false;
            Probe p = probes[index];
            p.localPosition = transform.InverseTransformPoint(worldPos);
            Vector3 n = worldNormal.sqrMagnitude > 0.0001f ? worldNormal.normalized : Vector3.up;
            p.localNormal = transform.InverseTransformDirection(n).normalized;
            if (markManual)
                p.isManual = true;
            probes[index] = p;
            return true;
        }

        public bool RemoveProbe(int index)
        {
            if (probes == null || index < 0 || index >= probes.Count)
                return false;
            probes.RemoveAt(index);
            if (EditorSelectedIndex == index)
                EditorSelectedIndex = -1;
            else if (EditorSelectedIndex > index)
                EditorSelectedIndex--;
            return true;
        }

        public void ClearProbes()
        {
            if (probes == null)
                probes = new List<Probe>();
            else
                probes.Clear();
            EditorSelectedIndex = -1;
        }

        public bool GetWorldPose(int index, out Vector3 worldPos, out Vector3 worldNormal, out float radius, out ProbeType type)
        {
            worldPos = default;
            worldNormal = Vector3.up;
            radius = 0.12f;
            type = ProbeType.Face;
            if (probes == null || index < 0 || index >= probes.Count)
                return false;

            Probe p = probes[index];
            worldPos = transform.TransformPoint(p.localPosition);
            worldNormal = transform.TransformDirection(p.localNormal.sqrMagnitude > 0.0001f ? p.localNormal.normalized : Vector3.forward).normalized;
            radius = p.radius > 0.001f ? p.radius : 0.12f;
            type = p.type;
            return true;
        }

        /// <summary>Nearest probe within maxDistance of worldPoint. Returns false if none in range.</summary>
        public bool FindNearestProbe(Vector3 worldPoint, float maxDistance, out int index, out Vector3 worldPos, out Vector3 worldNormal, out float radius, out ProbeType type)
        {
            index = -1;
            worldPos = default;
            worldNormal = Vector3.up;
            radius = 0.12f;
            type = ProbeType.Face;
            if (probes == null || probes.Count == 0)
                return false;

            float maxSqr = maxDistance > 0f ? maxDistance * maxDistance : float.MaxValue;
            float bestSqr = maxSqr;
            for (int i = 0; i < probes.Count; i++)
            {
                if (!GetWorldPose(i, out Vector3 pos, out Vector3 n, out float r, out ProbeType t))
                    continue;
                if (!IsClimbableProbeNormal(n))
                    continue;
                float sqr = (pos - worldPoint).sqrMagnitude;
                if (sqr > bestSqr)
                    continue;
                bestSqr = sqr;
                index = i;
                worldPos = pos;
                worldNormal = n;
                radius = r;
                type = t;
            }
            return index >= 0;
        }

        /// <summary>Nearest probe facing roughly toward fromDirection (player looking at wall).</summary>
        public bool FindNearestFacingProbe(Vector3 worldPoint, Vector3 fromDirection, float maxDistance, out int index, out Vector3 worldPos, out Vector3 worldNormal, out float radius, out ProbeType type)
        {
            index = -1;
            worldPos = default;
            worldNormal = Vector3.up;
            radius = 0.12f;
            type = ProbeType.Face;
            if (probes == null || probes.Count == 0)
                return false;

            Vector3 dir = fromDirection.sqrMagnitude > 0.0001f ? fromDirection.normalized : Vector3.forward;
            float maxSqr = maxDistance > 0f ? maxDistance * maxDistance : float.MaxValue;
            float bestScore = float.MaxValue;
            for (int i = 0; i < probes.Count; i++)
            {
                if (!GetWorldPose(i, out Vector3 pos, out Vector3 n, out float r, out ProbeType t))
                    continue;
                if (!IsClimbableProbeNormal(n))
                    continue;
                float sqr = (pos - worldPoint).sqrMagnitude;
                if (sqr > maxSqr)
                    continue;
                // Prefer probes whose outward normal faces the player (dot with -dir).
                float face = Vector3.Dot(n, -dir);
                if (face < 0.05f)
                    continue;
                float score = sqr - face * 0.35f;
                if (score >= bestScore)
                    continue;
                bestScore = score;
                index = i;
                worldPos = pos;
                worldNormal = n;
                radius = r;
                type = t;
            }
            return index >= 0;
        }


        /// <summary>
        /// Nearest probe in a travel direction from worldPoint (or fromIndex stance mid / pose).
        /// Among candidates with Dot(to, desiredDir) >= minForwardDot and within maxDistance,
        /// picks smallest distance; tie-break higher forward, then lower lateral.
        /// Skips the other hand of the same stance pair. Rejects hard normal flips.
        /// </summary>
        public bool FindInDirection(
            Vector3 worldPoint,
            Vector3 desiredDir,
            float maxDistance,
            out int index,
            out Vector3 worldPos,
            out Vector3 worldNormal,
            out float radius,
            out ProbeType type,
            int fromIndex = -1,
            float minForwardDot = 0.1f,
            float maxNormalDeltaDot = 0.85f)
        {
            index = -1;
            worldPos = default;
            worldNormal = Vector3.up;
            radius = 0.12f;
            type = ProbeType.Face;
            if (probes == null || probes.Count == 0)
                return false;

            Vector3 origin = worldPoint;
            Vector3 originN = Vector3.up;
            int skipPairId = -1;
            if (fromIndex >= 0 && fromIndex < probes.Count)
            {
                skipPairId = probes[fromIndex].pairId;
                if (TryGetPairWorldPoses(fromIndex, out _, out _, out _, out _, out Vector3 mid, out Vector3 midN))
                {
                    origin = mid;
                    originN = midN;
                }
                else if (GetWorldPose(fromIndex, out Vector3 fromPos, out Vector3 fromN, out _, out _))
                {
                    origin = fromPos;
                    originN = fromN;
                }
            }

            Vector3 dir = desiredDir.sqrMagnitude > 0.0001f ? desiredDir.normalized : transform.up;
            float maxDist = maxDistance > 0f ? maxDistance : 2.5f;
            float maxSqr = maxDist * maxDist;
            float bestDist = float.MaxValue;
            float bestForward = float.NegativeInfinity;
            float bestLateral = float.MaxValue;

            for (int i = 0; i < probes.Count; i++)
            {
                if (i == fromIndex)
                    continue;
                // Do not step to the other hand of the same L/R stance pair.
                if (skipPairId >= 0 && probes[i].pairId == skipPairId)
                    continue;
                if (!GetWorldPose(i, out Vector3 pos, out Vector3 n, out float r, out ProbeType t))
                    continue;
                // Skip walkable tops leftover from older bakes.
                if (!IsClimbableProbeNormal(n))
                    continue;

                // Paired candidates: distance/direction use stance midpoint so a far L/R hand can't leap.
                Vector3 candidate = pos;
                Vector3 candidateN = n;
                if (probes[i].pairId >= 0
                    && TryGetPairWorldPoses(i, out _, out _, out _, out _, out Vector3 mid, out Vector3 midN))
                {
                    candidate = mid;
                    if (midN.sqrMagnitude > 0.0001f)
                        candidateN = midN;
                }

                Vector3 delta = candidate - origin;
                float sqr = delta.sqrMagnitude;
                if (sqr < 0.0001f || sqr > maxSqr)
                    continue;

                Vector3 to = delta.normalized;
                float forward = Vector3.Dot(to, dir);
                if (forward < minForwardDot)
                    continue;

                // Prefer holds on a coherent face (normals not flipped too hard).
                float nAlign = Vector3.Dot(candidateN, originN);
                if (fromIndex >= 0 && nAlign < -maxNormalDeltaDot)
                    continue;

                float dist = Mathf.Sqrt(sqr);
                float lateral = (delta - dir * Vector3.Dot(delta, dir)).magnitude;

                // Distance primary; tie-break: higher forward, then lower lateral.
                const float eps = 0.0001f;
                bool better = false;
                if (dist < bestDist - eps)
                    better = true;
                else if (dist <= bestDist + eps)
                {
                    if (forward > bestForward + eps)
                        better = true;
                    else if (forward >= bestForward - eps && lateral < bestLateral - eps)
                        better = true;
                }
                if (!better)
                    continue;

                bestDist = dist;
                bestForward = forward;
                bestLateral = lateral;
                index = i;
                worldPos = pos;
                worldNormal = n;
                radius = r;
                type = t;
            }

            return index >= 0;
        }

        /// <summary>
        /// Nearest other stance/probe within maxDistance, skipping fromIndex and its L/R pair partner.
        /// Optional preferredDir softly biases ties (does not hard-reject opposite directions).
        /// </summary>
        public bool FindNearestOtherStance(
            Vector3 worldPoint,
            float maxDistance,
            int fromIndex,
            out int index,
            out Vector3 worldPos,
            out Vector3 worldNormal,
            out float radius,
            out ProbeType type,
            Vector3 preferredDir = default)
        {
            index = -1;
            worldPos = default;
            worldNormal = Vector3.up;
            radius = 0.12f;
            type = ProbeType.Face;
            if (probes == null || probes.Count == 0)
                return false;

            Vector3 origin = worldPoint;
            int skipPairId = -1;
            if (fromIndex >= 0 && fromIndex < probes.Count)
            {
                skipPairId = probes[fromIndex].pairId;
                if (TryGetPairWorldPoses(fromIndex, out _, out _, out _, out _, out Vector3 mid, out _))
                    origin = mid;
                else if (GetWorldPose(fromIndex, out Vector3 fromPos, out _, out _, out _))
                    origin = fromPos;
            }

            float maxDist = maxDistance > 0f ? maxDistance : 2.75f;
            float maxSqr = maxDist * maxDist;
            bool hasPref = preferredDir.sqrMagnitude > 0.0001f;
            Vector3 pref = hasPref ? preferredDir.normalized : Vector3.zero;
            float bestScore = float.MaxValue;

            for (int i = 0; i < probes.Count; i++)
            {
                if (i == fromIndex)
                    continue;
                if (skipPairId >= 0 && probes[i].pairId == skipPairId)
                    continue;
                if (!GetWorldPose(i, out Vector3 pos, out Vector3 n, out float r, out ProbeType t))
                    continue;
                if (!IsClimbableProbeNormal(n))
                    continue;

                Vector3 candidate = pos;
                if (probes[i].pairId >= 0
                    && TryGetPairWorldPoses(i, out _, out _, out _, out _, out Vector3 mid, out _))
                    candidate = mid;

                Vector3 delta = candidate - origin;
                float sqr = delta.sqrMagnitude;
                if (sqr < 0.0001f || sqr > maxSqr)
                    continue;

                float dist = Mathf.Sqrt(sqr);
                float score = dist;
                if (hasPref)
                {
                    float forward = Vector3.Dot(delta.normalized, pref);
                    score -= forward * 0.35f;
                }

                if (score >= bestScore)
                    continue;

                bestScore = score;
                index = i;
                worldPos = pos;
                worldNormal = n;
                radius = r;
                type = t;
            }

            return index >= 0;
        }

        /// <summary>
        /// Nearest other stance with the strongest lateral offset along wallRight (A/D column unlock).
        /// Ignores forward cone; still skips same L/R pair and walkable tops.
        /// </summary>
        public bool FindNearestLateralStance(
            Vector3 worldPoint,
            Vector3 wallRight,
            float maxDistance,
            int fromIndex,
            float sideSign,
            out int index,
            out Vector3 worldPos,
            out Vector3 worldNormal,
            out float radius,
            out ProbeType type)
        {
            index = -1;
            worldPos = default;
            worldNormal = Vector3.up;
            radius = 0.12f;
            type = ProbeType.Face;
            if (probes == null || probes.Count == 0)
                return false;

            Vector3 origin = worldPoint;
            int skipPairId = -1;
            if (fromIndex >= 0 && fromIndex < probes.Count)
            {
                skipPairId = probes[fromIndex].pairId;
                if (TryGetPairWorldPoses(fromIndex, out _, out _, out _, out _, out Vector3 mid, out _))
                    origin = mid;
                else if (GetWorldPose(fromIndex, out Vector3 fromPos, out _, out _, out _))
                    origin = fromPos;
            }

            Vector3 right = wallRight.sqrMagnitude > 0.0001f ? wallRight.normalized : Vector3.right;
            float maxDist = maxDistance > 0f ? maxDistance : 2.85f;
            float maxSqr = maxDist * maxDist;
            float prefer = sideSign >= 0f ? 1f : -1f;
            float bestScore = float.MaxValue;

            for (int i = 0; i < probes.Count; i++)
            {
                if (i == fromIndex)
                    continue;
                if (skipPairId >= 0 && probes[i].pairId == skipPairId)
                    continue;
                if (!GetWorldPose(i, out Vector3 pos, out Vector3 n, out float r, out ProbeType t))
                    continue;
                if (!IsClimbableProbeNormal(n))
                    continue;

                Vector3 candidate = pos;
                if (probes[i].pairId >= 0
                    && TryGetPairWorldPoses(i, out _, out _, out _, out _, out Vector3 omid, out _))
                    candidate = omid;

                Vector3 delta = candidate - origin;
                float sqr = delta.sqrMagnitude;
                if (sqr < 0.04f || sqr > maxSqr) // ignore near-zero / same stance
                    continue;

                float lateral = Vector3.Dot(delta, right);
                // Must move toward pressed side (or accept either if sideSign ~ 0).
                if (Mathf.Abs(prefer) > 0.01f && lateral * prefer < 0.02f)
                    continue;

                float absLat = Mathf.Abs(lateral);
                // Allow tighter column spacing (0.35m+ sideways).
                if (absLat < 0.28f)
                {
                    // Still reject near-pure vertical neighbors.
                    Vector3 up = Vector3.up;
                    float vert = Mathf.Abs(Vector3.Dot(delta, up));
                    if (absLat < vert * 0.35f)
                        continue;
                }

                float dist = Mathf.Sqrt(sqr);
                // Prefer clear sideways offset, then shorter distance.
                float score = dist - absLat * 0.65f;
                if (score >= bestScore)
                    continue;

                bestScore = score;
                index = i;
                worldPos = pos;
                worldNormal = n;
                radius = r;
                type = t;
            }

            return index >= 0;
        }

        /// <summary>Convenience: nearest probe along desiredDir within maxDistance (no fromIndex).</summary>
        public bool FindNearestInDirection(Vector3 worldPoint, Vector3 desiredDir, float maxDistance, out int index, out Vector3 worldPos, out Vector3 worldNormal, out float radius, out ProbeType type)
        {
            return FindInDirection(worldPoint, desiredDir, maxDistance, out index, out worldPos, out worldNormal, out radius, out type, fromIndex: -1);
        }

        private void OnDrawGizmosSelected()
        {
            DrawProbes(selected: true);
        }

        private void OnDrawGizmos()
        {
            // Perf: skip always-on draw for every ProbeSet in the scene. Selected uses OnDrawGizmosSelected;
            // baker Scene overlays still draw while the Probe Baker window is open.
        }

        private void DrawProbes(bool selected)
        {
            if (probes == null || probes.Count == 0)
                return;

            Color c = gizmoColor;
            if (!selected)
                c.a *= 0.35f;
            float scale = Mathf.Max(0.05f, gizmoScale);

            for (int i = 0; i < probes.Count; i++)
            {
                if (!GetWorldPose(i, out Vector3 pos, out Vector3 n, out float r, out ProbeType t))
                    continue;

                bool isPanelSelected = EditorSelectedIndex == i;
                Color draw = isPanelSelected ? selectedGizmoColor : TintForType(c, t);
                if (isPanelSelected && !selected)
                    draw.a = Mathf.Max(draw.a, 0.9f);
                Gizmos.color = draw;
                float rad = Mathf.Max(0.02f, r) * scale;
                Gizmos.DrawSphere(pos, rad);
                Gizmos.DrawWireSphere(pos, rad);
                Gizmos.DrawLine(pos, pos + n * (rad * 2.2f));
                // Stance pair connector (L/R ~0.5m) — draw once from Left.
                if (probes[i].hand == HandSide.Left && TryGetPairPartner(i, out int partner) &&
                    GetWorldPose(partner, out Vector3 pPos, out _, out _, out _))
                {
                    Color link = draw;
                    link.a = Mathf.Clamp01(draw.a + 0.25f);
                    Gizmos.color = link;
                    Gizmos.DrawLine(pos, pPos);
                }
            }
        }

        private static Color TintForType(Color baseColor, ProbeType type)
        {
            switch (type)
            {
                case ProbeType.Lip:
                    return Color.Lerp(baseColor, new Color(1f, 0.85f, 0.2f, baseColor.a), 0.55f);
                case ProbeType.Mantle:
                    return Color.Lerp(baseColor, new Color(0.3f, 1f, 0.4f, baseColor.a), 0.55f);
                case ProbeType.Hang:
                    return Color.Lerp(baseColor, new Color(1f, 0.35f, 0.75f, baseColor.a), 0.55f);
                default:
                    return baseColor;
            }
        }
    }
}

using System.Collections.Generic;
using Project.Core;
using UnityEngine;

namespace Project.Player
{
    /// <summary>
    /// Records the player's recent movement path for companion stuck recovery.
    /// Keeps a rolling window of the most recent trail length.
    /// </summary>
    public class PlayerPathTrail : MonoBehaviour
    {
        public const float DefaultMaxTrailLength = 90f;

        public static PlayerPathTrail Instance { get; private set; }

        [SerializeField] private float maxTrailLength = DefaultMaxTrailLength;
        [SerializeField] private float minRecordDistance = 0.35f;

        private readonly List<Vector3> points = new List<Vector3>(128);
        private float totalLength;
        private Vector3 lastRecordedPosition;
        private bool hasRecordedPosition;

        public float MaxTrailLength => maxTrailLength;
        public float TotalLength => totalLength;
        public int PointCount => points.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void LateUpdate()
        {
            RecordCurrentPosition();
        }

        public static PlayerPathTrail EnsureExists()
        {
            if (Instance != null)
                return Instance;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
                return null;

            PlayerPathTrail existing = player.GetComponent<PlayerPathTrail>();
            if (existing != null)
                return existing;

            return player.AddComponent<PlayerPathTrail>();
        }

        public void Clear()
        {
            points.Clear();
            totalLength = 0f;
            hasRecordedPosition = false;
        }

        public bool TryGetBacktrackTarget(
            Vector3 fromPosition,
            float minLookbackDistance,
            float maxLookbackDistance,
            out Vector3 target)
        {
            return TryGetBacktrackTarget(
                fromPosition,
                minLookbackDistance,
                maxLookbackDistance,
                stuckAttempt: 0,
                obstructionMask: default,
                out target);
        }

        public bool TryGetBacktrackTarget(
            Vector3 fromPosition,
            float minLookbackDistance,
            float maxLookbackDistance,
            int stuckAttempt,
            LayerMask obstructionMask,
            out Vector3 target)
        {
            target = fromPosition;
            if (points.Count == 0)
                return false;

            float escalatedMin = minLookbackDistance + stuckAttempt * 2.5f;
            float effectiveMin = Mathf.Min(escalatedMin, maxLookbackDistance * 0.85f);

            int nearestIndex = FindNearestPointIndex(fromPosition);
            if (nearestIndex < 0)
                return false;

            float walked = 0f;
            Vector3 fallbackTarget = points[0];
            bool reachedMin = false;
            Vector3 losTarget = fromPosition;
            bool hasLosTarget = false;

            for (int i = nearestIndex; i > 0; i--)
            {
                float segmentLength = SegmentDistance(points[i], points[i - 1]);
                walked += segmentLength;

                if (!reachedMin && walked >= effectiveMin)
                {
                    reachedMin = true;
                    fallbackTarget = points[i - 1];

                    if (obstructionMask.value != 0
                        && HasLineOfSight(fromPosition, fallbackTarget, obstructionMask))
                    {
                        target = fallbackTarget;
                        return true;
                    }

                    losTarget = fallbackTarget;
                    hasLosTarget = true;
                }

                if (reachedMin && walked >= maxLookbackDistance)
                {
                    if (obstructionMask.value != 0
                        && TryFindLineOfSightTarget(fromPosition, i - 1, nearestIndex, obstructionMask, out Vector3 visibleTarget))
                    {
                        target = visibleTarget;
                        return true;
                    }

                    target = fallbackTarget;
                    return true;
                }
            }

            if (!reachedMin)
            {
                if (walked < effectiveMin * 0.5f)
                    return false;

                fallbackTarget = points[0];
            }

            if (obstructionMask.value != 0
                && TryFindLineOfSightTarget(fromPosition, 0, nearestIndex, obstructionMask, out Vector3 visibleFallback))
            {
                target = visibleFallback;
                return true;
            }

            target = hasLosTarget ? losTarget : fallbackTarget;
            return true;
        }

        /// <summary>
        /// Returns the next walkable point ahead on the player's recent path (toward newer trail points).
        /// Used when a pioneer cannot reach its target directly because of walls or obstacles.
        /// </summary>
        public bool TryGetTrailFollowTarget(
            Vector3 fromPosition,
            float minLookaheadDistance,
            float maxLookaheadDistance,
            LayerMask obstructionMask,
            out Vector3 target)
        {
            target = fromPosition;
            if (points.Count < 2)
                return false;

            int fromIndex = FindNearestPointIndex(fromPosition);
            if (fromIndex < 0)
                return false;

            float walked = 0f;
            Vector3 fallbackTarget = points[fromIndex];
            bool reachedMin = false;

            for (int i = fromIndex; i < points.Count - 1; i++)
            {
                float segmentLength = SegmentDistance(points[i], points[i + 1]);
                walked += segmentLength;

                if (!reachedMin && walked >= minLookaheadDistance)
                {
                    reachedMin = true;
                    fallbackTarget = points[i + 1];

                    if (obstructionMask.value == 0
                        || HasLineOfSight(fromPosition, fallbackTarget, obstructionMask))
                    {
                        target = fallbackTarget;
                        return true;
                    }
                }
                else if (reachedMin)
                {
                    Vector3 candidate = points[i + 1];
                    if (obstructionMask.value == 0
                        || HasLineOfSight(fromPosition, candidate, obstructionMask))
                    {
                        target = candidate;
                        return true;
                    }
                }

                if (walked >= maxLookaheadDistance)
                    break;
            }

            if (reachedMin)
            {
                target = fallbackTarget;
                return true;
            }

            return walked >= minLookaheadDistance * 0.5f;
        }

        private void RecordCurrentPosition()
        {
            Vector3 position = transform.position;
            position.y = SampleGroundHeight(position);

            if (!hasRecordedPosition)
            {
                points.Add(position);
                lastRecordedPosition = position;
                hasRecordedPosition = true;
                return;
            }

            if (HorizontalDistance(lastRecordedPosition, position) < minRecordDistance
                && Mathf.Abs(lastRecordedPosition.y - position.y) < 0.35f)
                return;

            float segmentLength = SegmentDistance(lastRecordedPosition, position);
            points.Add(position);
            totalLength += segmentLength;
            lastRecordedPosition = position;
            TrimToMaxLength();
        }

        private void TrimToMaxLength()
        {
            while (points.Count >= 2 && totalLength > maxTrailLength)
            {
                float removedLength = HorizontalDistance(points[0], points[1]);
                totalLength -= removedLength;
                points.RemoveAt(0);
            }
        }

        private int FindNearestPointIndex(Vector3 position)
        {
            if (points.Count == 0)
                return -1;

            const float maxVerticalSeparation = 3.5f;

            int bestIndex = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < points.Count; i++)
            {
                float verticalDelta = Mathf.Abs(position.y - points[i].y);
                if (verticalDelta > maxVerticalSeparation)
                    continue;

                float horizontal = HorizontalDistance(position, points[i]);
                float distance = horizontal + verticalDelta * 1.35f;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            if (bestDistance < float.MaxValue)
                return bestIndex;

            return FindNearestPointIndexPlanar(position);
        }

        private int FindNearestPointIndexPlanar(Vector3 position)
        {
            int bestIndex = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < points.Count; i++)
            {
                float distance = HorizontalDistance(position, points[i]);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private bool TryFindLineOfSightTarget(
            Vector3 fromPosition,
            int startIndex,
            int endIndex,
            LayerMask obstructionMask,
            out Vector3 target)
        {
            target = fromPosition;
            int from = Mathf.Clamp(startIndex, 0, points.Count - 1);
            int to = Mathf.Clamp(endIndex, 0, points.Count - 1);
            if (from > to)
                (from, to) = (to, from);

            for (int i = to; i >= from; i--)
            {
                if (!HasLineOfSight(fromPosition, points[i], obstructionMask))
                    continue;

                target = points[i];
                return true;
            }

            return false;
        }

        private static bool HasLineOfSight(Vector3 fromPosition, Vector3 targetPosition, LayerMask obstructionMask)
        {
            Vector3 origin = fromPosition + Vector3.up * 0.85f;
            Vector3 destination = targetPosition + Vector3.up * 0.85f;
            Vector3 delta = destination - origin;
            float distance = delta.magnitude;
            if (distance < 0.05f)
                return true;

            return !Physics.Raycast(
                origin,
                delta / distance,
                distance,
                obstructionMask,
                QueryTriggerInteraction.Ignore);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private static float SegmentDistance(Vector3 a, Vector3 b)
        {
            float horizontal = HorizontalDistance(a, b);
            float vertical = Mathf.Abs(a.y - b.y);
            return horizontal + vertical * 0.65f;
        }

        private static float SampleGroundHeight(Vector3 position)
        {
            const float probeHeight = 2.5f;
            const float probeDistance = 10f;
            const float groundOffset = 0.05f;
            const float maxHeightAboveTerrain = 0.35f;

            float baselineY = GetTerrainBaselineY(position, groundOffset);
            float originY = Mathf.Max(position.y + probeHeight, baselineY + probeHeight);
            Vector3 origin = new Vector3(position.x, originY, position.z);
            float rayLength = (originY - position.y) + probeDistance;

            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                Vector3.down,
                rayLength,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            float maxAllowedY = Mathf.Min(position.y + 0.5f, baselineY + maxHeightAboveTerrain);
            float bestScore = float.MaxValue;
            float bestY = baselineY;
            bool found = false;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null || collider.isTrigger || !IsWalkableGroundCollider(collider))
                    continue;

                float candidateY = hits[i].point.y + groundOffset;
                if (candidateY > maxAllowedY + 0.01f)
                    continue;

                float score = Mathf.Abs(candidateY - baselineY);
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestY = candidateY;
                found = true;
            }

            return found ? bestY : baselineY;
        }

        private static float GetTerrainBaselineY(Vector3 worldPosition, float groundOffset)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
                return worldPosition.y;

            return terrain.SampleHeight(worldPosition) + terrain.transform.position.y + groundOffset;
        }

        private static bool IsWalkableGroundCollider(Collider collider)
        {
            if (collider is TerrainCollider)
                return true;

            if (collider.CompareTag("Dirt") || collider.CompareTag("Walkable"))
                return true;

            return IsWalkableGeometryName(collider.name);
        }

        private static bool IsWalkableGeometryName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return false;

            string lower = objectName.ToLowerInvariant();
            return lower.Contains("ramp")
                || lower.Contains("stair")
                || lower.Contains("step")
                || lower.Contains("floor")
                || lower.Contains("walkway")
                || lower.Contains("platform");
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (points.Count < 2)
                return;

            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.65f);
            for (int i = 1; i < points.Count; i++)
                Gizmos.DrawLine(points[i - 1] + Vector3.up * 0.05f, points[i] + Vector3.up * 0.05f);
        }
#endif
    }
}

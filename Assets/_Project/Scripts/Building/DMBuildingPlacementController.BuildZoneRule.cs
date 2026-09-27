using System.Collections.Generic;
using MalbersAnimations.PathCreation;
using Project.World;
using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// 0926-build-hub: build zone and patrol path rules, checked after every aim (TryAim in Kit2.cs).
    /// (a) Every piece except the Build Hub must sit fully inside a Build Hub zone (Studio > Placement "Require Build Hub").
    ///     With no hub in the world only the Build Hub can be placed.
    /// (b) A new Build Hub may not stand inside another hub's zone.
    /// (c) Nothing may be built on or across a creature / pet patrol path: the ghost bounds, grown by the profile's
    ///     clearance, must not touch a DMIPathFollowProvider polyline, and its centre must not be inside the
    ///     provider's sphere trigger.
    /// (d) 0926-zone-clear: a Build Hub's whole zone box (at its level) must hold no collider except terrain
    ///     (Studio "Zone must be clear"; see DMBuildHub.FindZoneBlocker for what is ignored). Throttled: re-checked
    ///     only when the ghost moves over 0.5 m, turns, changes size, or every 2 s.
    /// The failing rule's text is exposed as BlockedReason and shown under the piece name on the build hotbar.
    /// </summary>
    public sealed partial class DMBuildingPlacementController
    {
        public const string ReasonNoHub = "Place a Build Hub first";
        public const string ReasonOutsideZone = "Outside the Build Hub zone";
        public const string ReasonHubOverlap = "Inside another Build Hub's zone";
        public const string ReasonPatrolPath = "Blocked: creature patrol path";
        public const string ReasonPatrolArea = "Blocked: patrol path area";

        const float PatrolRefreshSeconds = 2f;

        struct PatrolPath
        {
            public DMIPathFollowProvider Provider;
            public Vector3[] Points;
            public bool Closed;
            public Bounds Bounds;
            public bool HasSphere;
            public Vector3 SphereCenter;
            public float SphereRadius;
        }

        static readonly List<PatrolPath> patrolPaths = new List<PatrolPath>(8);
        static float patrolPathsAt = -100f;
        static string blockedReason;
        static int blockedReasonFrame = -10;

        const float ZoneClearRecheckMeters = 0.5f;
        const float ZoneClearRecheckSeconds = 2f;
        static bool zoneClearValid;
        static Vector3 zoneClearPosition;
        static Quaternion zoneClearRotation = Quaternion.identity;
        static Vector3 zoneClearSize;
        static float zoneClearAt = -100f;
        static string zoneClearResult;

        /// <summary>Why the current ghost is blocked by a build zone or patrol path rule this frame, or null.</summary>
        public static string BlockedReason => Time.frameCount - blockedReasonFrame <= 1 ? blockedReason : null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetBuildZoneStatics()
        {
            patrolPaths.Clear();
            patrolPathsAt = -100f;
            blockedReason = null;
            blockedReasonFrame = -10;
            ResetZoneClearCache();
        }

        static void ResetZoneClearCache()
        {
            zoneClearValid = false;
            zoneClearResult = null;
            zoneClearAt = -100f;
        }

        /// <summary>"Build zone blocked by ..." for a hub zone box here, or null when clear. Throttled (big boxes are costly).</summary>
        static string ZoneClearReason(Vector3 center, Vector3 size, Quaternion rotation)
        {
            float now = Time.unscaledTime;
            if (zoneClearValid
                && now >= zoneClearAt && now - zoneClearAt < ZoneClearRecheckSeconds
                && (center - zoneClearPosition).sqrMagnitude < ZoneClearRecheckMeters * ZoneClearRecheckMeters
                && (size - zoneClearSize).sqrMagnitude < 0.0001f
                && Quaternion.Angle(rotation, zoneClearRotation) < 1f)
                return zoneClearResult;

            Collider blocker = DMBuildHub.FindZoneBlocker(center, size);
            zoneClearResult = blocker != null ? DMBuildHub.BlockedByText(blocker) : null;
            zoneClearValid = true;
            zoneClearAt = now;
            zoneClearPosition = center;
            zoneClearRotation = rotation;
            zoneClearSize = size;
            return zoneClearResult;
        }

        static void ApplyBuildZoneRules(bool seated, DMBuildingPiece piece, Vector3 position, Quaternion rotation, ref bool canCommit)
        {
            blockedReason = null;
            blockedReasonFrame = Time.frameCount;
            if (piece == null)
                return;

            bool isHub = DMBuildingCatalog.IsBuildHub(piece.Id);
            if (!isHub && DMBuildingGhostProfile.RequireBuildHub && !DMBuildHub.AnyHub)
            {
                blockedReason = ReasonNoHub;
                canCommit = false;
                return;
            }

            if (!seated)
                return;

            string reason = BuildZoneReason(piece, isHub, position, rotation);
            if (reason == null)
                return;
            blockedReason = reason;
            canCommit = false;
        }

        static string BuildZoneReason(DMBuildingPiece piece, bool isHub, Vector3 position, Quaternion rotation)
        {
            Bounds bounds = GhostWorldBounds(piece, position, rotation);
            if (isHub && MovingHub() != null)
            {
                // 0927-zone-anchor: a moved hub keeps its zone where it was first placed, so neither the zone-overlap
                // nor the zone-clear check applies; the hub itself may go anywhere.
            }
            else if (isHub)
            {
                // Kept simple: the new hub's position may not be inside an existing zone.
                if (DMBuildHub.ZoneFor(position) != null)
                    return ReasonHubOverlap;
                if (DMBuildingGhostProfile.BuildHubZoneMustBeClear)
                {
                    string blocked = ZoneClearReason(position, DMBuildHub.SizeForLevel(0), rotation);
                    if (blocked != null)
                        return blocked;
                }
            }
            else if (DMBuildingGhostProfile.RequireBuildHub && !DMBuildHub.IsInsideAnyZone(bounds))
            {
                return ReasonOutsideZone;
            }

            return DMBuildingGhostProfile.BlockPatrolPaths ? PatrolPathReason(bounds) : null;
        }

        /// <summary>World AABB of the piece's box (piece.Size) at this seat.</summary>
        static Bounds GhostWorldBounds(DMBuildingPiece piece, Vector3 position, Quaternion rotation)
        {
            Vector3 half = piece.Size * 0.5f;
            Matrix4x4 m = Matrix4x4.Rotate(rotation);
            Vector3 extents = new Vector3(
                Mathf.Abs(m.m00) * half.x + Mathf.Abs(m.m01) * half.y + Mathf.Abs(m.m02) * half.z,
                Mathf.Abs(m.m10) * half.x + Mathf.Abs(m.m11) * half.y + Mathf.Abs(m.m12) * half.z,
                Mathf.Abs(m.m20) * half.x + Mathf.Abs(m.m21) * half.y + Mathf.Abs(m.m22) * half.z);
            return new Bounds(position, extents * 2f);
        }

        // ---- Patrol paths ----

        static string PatrolPathReason(Bounds bounds)
        {
            RefreshPatrolPaths();
            if (patrolPaths.Count == 0)
                return null;

            float clearance = DMBuildingGhostProfile.PatrolPathClearanceMeters;
            Bounds padded = bounds;
            padded.Expand(new Vector3(clearance * 2f, (clearance + DMBuildingGhostProfile.PatrolPathHeightToleranceMeters) * 2f, clearance * 2f));
            Vector3 center = bounds.center;
            for (int p = 0; p < patrolPaths.Count; p++)
            {
                PatrolPath path = patrolPaths[p];
                if (path.Provider == null)
                    continue;
                if (path.HasSphere && (center - path.SphereCenter).sqrMagnitude <= path.SphereRadius * path.SphereRadius)
                    return ReasonPatrolArea;

                Vector3[] points = path.Points;
                if (points == null || points.Length < 2 || !path.Bounds.Intersects(padded))
                    continue;
                int count = points.Length;
                int segments = path.Closed ? count : count - 1;
                for (int s = 0; s < segments; s++)
                {
                    if (SegmentIntersectsBounds(points[s], points[(s + 1) % count], padded))
                        return ReasonPatrolPath;
                }
            }

            return null;
        }

        /// <summary>Re-reads every patrol path provider in the scene every couple of seconds (not every frame).</summary>
        static void RefreshPatrolPaths()
        {
            float now = Time.unscaledTime;
            if (now >= patrolPathsAt && now - patrolPathsAt < PatrolRefreshSeconds)
                return;
            patrolPathsAt = now;
            patrolPaths.Clear();

            DMIPathFollowProvider[] providers = UnityEngine.Object.FindObjectsByType<DMIPathFollowProvider>(FindObjectsInactive.Exclude);
            for (int i = 0; i < providers.Length; i++)
            {
                DMIPathFollowProvider provider = providers[i];
                if (provider == null || !provider.isActiveAndEnabled)
                    continue;

                var entry = new PatrolPath { Provider = provider };
                bool closed;
                entry.Points = SamplePatrolPath(provider, out closed);
                entry.Closed = closed;
                if (entry.Points.Length >= 2)
                {
                    var box = new Bounds(entry.Points[0], Vector3.zero);
                    for (int k = 1; k < entry.Points.Length; k++)
                        box.Encapsulate(entry.Points[k]);
                    entry.Bounds = box;
                }

                SphereCollider sphere = provider.GetComponent<SphereCollider>();
                if (sphere != null && sphere.enabled && sphere.isTrigger)
                {
                    Vector3 scale = sphere.transform.lossyScale;
                    entry.HasSphere = true;
                    entry.SphereCenter = sphere.transform.TransformPoint(sphere.center);
                    entry.SphereRadius = sphere.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
                }

                if (entry.Points.Length >= 2 || entry.HasSphere)
                    patrolPaths.Add(entry);
            }
        }

        /// <summary>World polyline of the provider's Path Creator vertex path, or its cached anchors as a fallback.</summary>
        static Vector3[] SamplePatrolPath(DMIPathFollowProvider provider, out bool closed)
        {
            closed = false;
            PathCreator creator = provider.PathCreator;
            VertexPath vertexPath = null;
            if (creator != null)
            {
                try
                {
                    vertexPath = creator.path;
                }
                catch (System.Exception)
                {
                    vertexPath = null; // an uninitialised Path Creator falls back to the anchors below
                }
            }

            if (vertexPath != null && vertexPath.NumPoints >= 2)
            {
                vertexPath.UpdateTransform(creator.transform);
                closed = vertexPath.isClosedLoop;
                var points = new Vector3[vertexPath.NumPoints];
                for (int i = 0; i < points.Length; i++)
                    points[i] = vertexPath.GetPoint(i);
                return points;
            }

            Vector3[] anchors = provider.AnchorWorldPoints;
            if (anchors == null || anchors.Length < 2)
                return System.Array.Empty<Vector3>();
            closed = provider.PatrolMode == DMIPathPatrolMode.Loop;
            return (Vector3[])anchors.Clone();
        }

        /// <summary>Slab test: does the segment a-b touch the box?</summary>
        static bool SegmentIntersectsBounds(Vector3 a, Vector3 b, Bounds box)
        {
            Vector3 d = b - a;
            Vector3 min = box.min;
            Vector3 max = box.max;
            float tMin = 0f;
            float tMax = 1f;
            for (int axis = 0; axis < 3; axis++)
            {
                float origin = a[axis];
                float dir = d[axis];
                if (Mathf.Abs(dir) < 1e-6f)
                {
                    if (origin < min[axis] || origin > max[axis])
                        return false;
                    continue;
                }

                float inv = 1f / dir;
                float t1 = (min[axis] - origin) * inv;
                float t2 = (max[axis] - origin) * inv;
                if (t1 > t2)
                {
                    float swap = t1;
                    t1 = t2;
                    t2 = swap;
                }

                tMin = Mathf.Max(tMin, t1);
                tMax = Mathf.Min(tMax, t2);
                if (tMin > tMax)
                    return false;
            }

            return true;
        }
    }
}

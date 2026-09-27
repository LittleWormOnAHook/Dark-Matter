using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// 0926-rules: foundation connection rule and the trimmed build aim ray.
    /// A foundation may stand alone only when it starts a new base (no foundation within the
    /// profile's new-base clearance). Every other foundation must share an edge with a built
    /// foundation piece (full, triangle, half or quarter).
    /// </summary>
    public sealed partial class DMBuildingPlacementController
    {
        const float FoundationProbeOut = 0.2f;
        const float FoundationProbeDepth = 0.12f;
        const float FoundationProbeHalfHeight = 0.5f;
        const float AimRayBehindPlayerMeters = 0.35f;

        static readonly Collider[] foundationProbeHits = new Collider[24];
        static readonly Vector2[] rectFootprint = new Vector2[4];
        static readonly Vector2[] triFootprint = new Vector2[3];

        /// <summary>
        /// Crosshair ray for build mode, starting just behind the player instead of at the camera.
        /// In tight rooms the pulled-back build camera sits behind walls and props, and those hits
        /// used to win the snap before anything the player was actually looking at.
        /// </summary>
        static Ray BuildAimRay(Camera camera)
        {
            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (cachedPlayer == null)
                cachedPlayer = UnityEngine.Object.FindAnyObjectByType<Project.Player.PlayerController>();
            if (cachedPlayer == null)
                return ray;

            Vector3 chest = cachedPlayer.transform.position + Vector3.up * 1.2f;
            float skip = Vector3.Dot(chest - ray.origin, ray.direction) - AimRayBehindPlayerMeters;
            if (skip > 0f)
                ray.origin += ray.direction * skip;
            return ray;
        }

        static bool AnyFoundationBuilt()
        {
            DMBuildingGhost[] ghosts = BuiltGhosts();
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost != null && ghost.Built && DMBuildingCatalog.IsAnyFoundation(ghost.PieceId))
                    return true;
            }

            return false;
        }

        /// <summary>True when a free-standing foundation here would start a new base.</summary>
        static bool IsNewBaseSpot(Vector3 position)
        {
            float radius = DMBuildingGhostProfile.NewBaseClearanceMeters;
            if (radius <= 0f)
                return !AnyFoundationBuilt();

            DMBuildingGhost[] ghosts = BuiltGhosts();
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost == null || !ghost.Built || !DMBuildingCatalog.IsAnyFoundation(ghost.PieceId))
                    continue;
                if (FlatDistance(position, HorizontalPlacementCenter(ghost)) < radius)
                    return false;
            }

            return true;
        }

        static bool PassesFoundationRule(DMBuildingPiece piece, Vector3 position, Quaternion rotation)
        {
            if (piece == null)
                return false;
            if (IsNewBaseSpot(position))
                return DMBuildingCatalog.IsFoundation(piece.Id);
            return TouchesFoundationEdge(piece, position, rotation);
        }

        /// <summary>
        /// Probes a thin box just outside the middle of each footprint edge. Corner-only contact does not count.
        /// </summary>
        static bool TouchesFoundationEdge(DMBuildingPiece piece, Vector3 position, Quaternion rotation)
        {
            Vector2[] poly = FoundationFootprint(piece);
            int count = poly.Length;
            Vector2 centroid = Vector2.zero;
            for (int i = 0; i < count; i++)
                centroid += poly[i];
            centroid /= count;

            for (int i = 0; i < count; i++)
            {
                Vector2 a = poly[i];
                Vector2 b = poly[(i + 1) % count];
                Vector2 edge = b - a;
                float length = edge.magnitude;
                if (length < 0.2f)
                    continue;

                Vector2 dir = edge / length;
                Vector2 normal = new Vector2(dir.y, -dir.x);
                Vector2 mid = (a + b) * 0.5f;
                if (Vector2.Dot(normal, mid - centroid) < 0f)
                    normal = -normal;

                Vector3 normal3 = new Vector3(normal.x, 0f, normal.y);
                Vector3 local = new Vector3(mid.x, 0f, mid.y) + normal3 * FoundationProbeOut;
                Vector3 center = position + rotation * local;
                Quaternion orient = rotation * Quaternion.LookRotation(normal3, Vector3.up);
                Vector3 half = new Vector3(length * 0.3f, FoundationProbeHalfHeight, FoundationProbeDepth);

                int hitCount = Physics.OverlapBoxNonAlloc(center, half, foundationProbeHits, orient,
                    DMBuildingLayers.OverlapMask, QueryTriggerInteraction.Ignore);
                for (int h = 0; h < hitCount; h++)
                {
                    Collider collider = foundationProbeHits[h];
                    if (collider == null)
                        continue;
                    DMBuildingGhost ghost = collider.GetComponentInParent<DMBuildingGhost>();
                    if (ghost != null && ghost.Built && DMBuildingCatalog.IsAnyFoundation(ghost.PieceId))
                        return true;
                }
            }

            return false;
        }

        static Vector2[] FoundationFootprint(DMBuildingPiece piece)
        {
            float hx = piece.Size.x * 0.5f;
            float hz = piece.Size.z * 0.5f;
            if (piece.Shape == DMBuildingShape.TriFoundation)
            {
                // Matches DMBuildingKitBuilder.TriSlab: right triangle in the lower-left half of the cell.
                triFootprint[0] = new Vector2(-hx, -hz);
                triFootprint[1] = new Vector2(hx, -hz);
                triFootprint[2] = new Vector2(-hx, hz);
                return triFootprint;
            }

            rectFootprint[0] = new Vector2(-hx, -hz);
            rectFootprint[1] = new Vector2(hx, -hz);
            rectFootprint[2] = new Vector2(hx, hz);
            rectFootprint[3] = new Vector2(-hx, hz);
            return rectFootprint;
        }
    }
}

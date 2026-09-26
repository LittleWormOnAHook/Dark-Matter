using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// Kit phase 3 (0926) snap rules: the 8 m gate frame spans two grid edges of the same story, and half/quarter
    /// foundations hang flush off any foundation edge. Overlap rules keep half-cell pieces off full foundations
    /// and keep walls out of a gate's span.
    /// </summary>
    public sealed partial class DMBuildingPlacementController
    {
        const float GateSupportTopTolerance = 0.35f;
        const float GateSupportReachMeters = 2.5f;

        // ---- WideEdge: 8 m gate frame ----

        static bool TryAimGateFrame(DMBuildingPiece piece, out Vector3 position, out Quaternion rotation, out bool canCommit)
        {
            position = default;
            rotation = Quaternion.identity;
            canCommit = false;
            Camera camera = Camera.main;
            if (camera == null || !HasBuiltBase())
                return false;

            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            bool lookingUp = ray.direction.y > 0.18f || AimIsAboveWallMid(ray);
            ResolveAimPoint(ray, lookingUp, out Vector3 aim, out bool hasHit);
            if (!hasHit && !lookingUp)
                return false;

            float yaw = instance.yawNotches * YawStep();
            float lift = Mathf.Clamp(instance.heightOffset, -DMBuildingGhostProfile.MaxHeightOffsetMeters, DMBuildingGhostProfile.MaxHeightOffsetMeters);
            if (!TrySnapEdge(aim, piece, yaw, lift, lookingUp, out Vector3 seat, out Quaternion seatRotation))
                return false;

            // The edge seat sits on one 4 m edge. Slide half a module along the edge so the frame covers
            // this edge and its neighbour, preferring the side the crosshair is on.
            float module = DMBuildingCatalog.ModuleMeters;
            Vector3 tangent = FlatDirection(seatRotation * Vector3.right);
            float bottom = seat.y - piece.Size.y * 0.5f - lift;
            float aimSide = Vector3.Dot(aim - seat, tangent) >= 0f ? 1f : -1f;
            bool preferredOk = HasEdgeSupportAlong(seat + tangent * (aimSide * module), bottom);
            bool otherOk = HasEdgeSupportAlong(seat - tangent * (aimSide * module), bottom);
            float side = preferredOk || !otherOk ? aimSide : -aimSide;
            position = seat + tangent * (side * module * 0.5f);
            rotation = seatRotation;
            if (EdgeFlipped(instance.yawNotches))
                rotation *= Quaternion.Euler(0f, 180f, 0f);

            canCommit = (preferredOk || otherOk)
                && DMBuildingCatalog.HasCost(piece)
                && !Overlaps(position, piece.Size, rotation, piece.Id);
            return true;
        }

        /// <summary>True when a built slab (foundation, floor, ...) at this height owns the edge midpoint near point.</summary>
        static bool HasEdgeSupportAlong(Vector3 point, float bottom)
        {
            DMBuildingGhost[] ghosts = BuiltGhosts();
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost == null || !ghost.Built || !DMBuildingCatalog.IsEdgeSupport(ghost.PieceId))
                    continue;
                if (Mathf.Abs(SurfaceTop(ghost) - bottom) > GateSupportTopTolerance)
                    continue;
                if (FlatDistance(HorizontalPlacementCenter(ghost), point) <= GateSupportReachMeters)
                    return true;
            }

            return false;
        }

        // ---- HalfCell: 4x2 and 2x2 foundations off a foundation edge ----

        static bool TryAimHalfCell(DMBuildingPiece piece, out Vector3 position, out Quaternion rotation, out bool canCommit)
        {
            position = default;
            rotation = Quaternion.identity;
            canCommit = false;
            Camera camera = Camera.main;
            if (camera == null || !HasBuiltBase())
                return false;

            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            bool lookingUp = ray.direction.y > 0.18f || AimIsAboveWallMid(ray);
            ResolveAimPoint(ray, lookingUp, out Vector3 aim, out bool hasHit);
            bool hitBuilt = TryHitBuiltForSnap(ray, out DMBuildingGhost hitGhost, out RaycastHit hit);
            if (!hitBuilt && !hasHit)
                return false;

            Vector3 point = hitBuilt ? hit.point : aim;
            DMBuildingGhost anchor = Kit2Pick(hitGhost, point, Kit2SearchMeters, DMBuildingCatalog.IsFoundationLike);
            if (anchor == null)
                return false;

            Vector3 center = HorizontalPlacementCenter(anchor);
            Vector3 right = FlatDirection(anchor.transform.right);
            Vector3 forward = FlatDirection(anchor.transform.forward);
            Vector3 extents = HalfExtents(anchor);
            Vector3 offset = point - center;
            float u = Vector3.Dot(offset, right) / Mathf.Max(0.1f, extents.x);
            float v = Vector3.Dot(offset, forward) / Mathf.Max(0.1f, extents.z);

            Vector3 outward;
            Vector3 tangent;
            float edgeOut;
            float edgeHalf;
            if (Mathf.Abs(u) >= Mathf.Abs(v))
            {
                outward = right * (u >= 0f ? 1f : -1f);
                tangent = forward;
                edgeOut = extents.x;
                edgeHalf = extents.z;
            }
            else
            {
                outward = forward * (v >= 0f ? 1f : -1f);
                tangent = right;
                edgeOut = extents.z;
                edgeHalf = extents.x;
            }

            float cellHalf = DMBuildingCatalog.HalfCellMeters * 0.5f;
            float module = DMBuildingCatalog.ModuleMeters;
            Vector3 mid = center + outward * edgeOut;
            if (piece.Shape == DMBuildingShape.QuarterFoundation)
            {
                float slide = Mathf.Max(0f, edgeHalf - cellHalf);
                float along = Vector3.Dot(point - mid, tangent) >= 0f ? slide : -slide;
                position = mid + tangent * along + outward * cellHalf;
                rotation = Quaternion.LookRotation(outward, Vector3.up);
            }
            else if (edgeHalf * 2f >= module - 0.1f)
            {
                // Long side runs along the 4 m edge, 2 m deep.
                position = mid + outward * cellHalf;
                rotation = Quaternion.LookRotation(outward, Vector3.up);
            }
            else
            {
                // A 2 m edge: the half foundation sticks straight out 4 m.
                position = mid + outward * (module * 0.5f);
                rotation = Quaternion.LookRotation(tangent, Vector3.up);
            }

            float lift = Mathf.Clamp(instance.heightOffset, -DMBuildingGhostProfile.MaxHeightOffsetMeters, DMBuildingGhostProfile.MaxHeightOffsetMeters);
            position.y = SurfaceTop(anchor) - piece.Size.y * 0.5f + lift;
            canCommit = DMBuildingCatalog.HasCost(piece) && !Overlaps(position, piece.Size, rotation, piece.Id);
            return true;
        }

        // ---- Overlap rules (called from Overlaps for every built piece the padded box touches) ----

        static bool Kit3OverlapBlocks(DMBuildingGhost ghost, Vector3 center, Vector3 size, Quaternion rotation, string pieceId)
        {
            if (ghost == null || string.IsNullOrEmpty(pieceId))
                return false;

            string other = ghost.PieceId;
            if ((DMBuildingCatalog.IsHalfCellFoundation(pieceId) || DMBuildingCatalog.IsHalfCellFoundation(other))
                && DMBuildingCatalog.IsFoundationLike(pieceId)
                && DMBuildingCatalog.IsFoundationLike(other))
                return true;

            DMBuildingShape placing = DMBuildingCatalog.ShapeOf(pieceId);
            DMBuildingShape existing = DMBuildingCatalog.ShapeOf(other);
            if (existing == DMBuildingShape.GateFrame && IsWallRun(placing))
                return true;
            if (placing == DMBuildingShape.GateFrame && IsWallRun(existing))
                return true;

            return false;
        }

        static bool IsWallRun(DMBuildingShape shape)
        {
            if (shape == DMBuildingShape.Ladder)
                return false;
            DMBuildingSnap snap = DMBuildingCatalog.SnapFor(shape);
            return snap == DMBuildingSnap.Edge || snap == DMBuildingSnap.WideEdge;
        }
    }
}

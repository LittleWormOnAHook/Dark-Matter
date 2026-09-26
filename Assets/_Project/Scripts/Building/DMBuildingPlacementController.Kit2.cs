using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// Kit phase 2 (0926) snap rules: support pillars under foundations, columns on grid corners,
    /// beams along wall tops, ridge caps on roof peaks, overhangs (foundation steps, balcony) on slab edges,
    /// hatch lids on hatches, and ladders against a wall face. Everything else goes through TryAimCore.
    /// </summary>
    public sealed partial class DMBuildingPlacementController
    {
        const float Kit2SearchMeters = 6f;
        const float PillarCornerPickMeters = 1.3f;

        static bool TryAim(out DMBuildingPiece piece, out Vector3 position, out Quaternion rotation, out bool canCommit)
        {
            piece = DMBuildingMode.SelectedPiece;
            if (piece != null && instance != null)
            {
                if (IsKit2Snap(piece.Snap))
                    return TryAimKit2(piece, out position, out rotation, out canCommit);
                if (piece.Shape == DMBuildingShape.Ladder)
                    return TryAimLadder(piece, out position, out rotation, out canCommit);
                if (piece.Snap == DMBuildingSnap.WideEdge)
                    return TryAimGateFrame(piece, out position, out rotation, out canCommit);
                if (piece.Snap == DMBuildingSnap.HalfCell)
                    return TryAimHalfCell(piece, out position, out rotation, out canCommit);
            }

            return TryAimCore(out piece, out position, out rotation, out canCommit);
        }

        static bool IsKit2Snap(DMBuildingSnap snap)
        {
            return snap == DMBuildingSnap.Under
                || snap == DMBuildingSnap.Corner
                || snap == DMBuildingSnap.Beam
                || snap == DMBuildingSnap.Overhang
                || snap == DMBuildingSnap.Lid;
        }

        static bool TryAimKit2(DMBuildingPiece piece, out Vector3 position, out Quaternion rotation, out bool canCommit)
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
            float lift = Mathf.Clamp(instance.heightOffset, -DMBuildingGhostProfile.MaxHeightOffsetMeters, DMBuildingGhostProfile.MaxHeightOffsetMeters);
            DMBuildingGhost anchor;
            bool seated;
            switch (piece.Snap)
            {
                case DMBuildingSnap.Under:
                    seated = TrySeatUnder(hitGhost, point, piece, out position, out rotation, out anchor);
                    break;
                case DMBuildingSnap.Corner:
                    seated = TrySeatCorner(hitGhost, point, piece, out position, out rotation, out anchor);
                    break;
                case DMBuildingSnap.Beam:
                    seated = piece.Shape == DMBuildingShape.RidgeCap
                        ? TrySeatRidgeCap(hitGhost, point, piece, out position, out rotation, out anchor)
                        : TrySeatBeam(hitGhost, point, piece, out position, out rotation, out anchor);
                    break;
                case DMBuildingSnap.Overhang:
                    seated = TrySeatOverhang(hitGhost, point, piece, lift, out position, out rotation, out anchor);
                    break;
                default:
                    seated = TrySeatLid(hitGhost, point, piece, out position, out rotation, out anchor);
                    break;
            }

            if (!seated || anchor == null)
                return false;

            canCommit = DMBuildingCatalog.HasCost(piece) && !Overlaps(position, piece.Size, rotation, piece.Id);
            return true;
        }

        static DMBuildingGhost Kit2Pick(DMBuildingGhost hitGhost, Vector3 point, float range, System.Func<string, bool> accept)
        {
            if (hitGhost != null && hitGhost.Built && accept(hitGhost.PieceId))
                return hitGhost;

            DMBuildingGhost best = null;
            float bestDistance = range;
            DMBuildingGhost[] ghosts = BuiltGhosts();
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost == null || !ghost.Built || !accept(ghost.PieceId))
                    continue;

                Vector3 center = HorizontalPlacementCenter(ghost);
                float distance = FlatDistance(point, center) + Mathf.Abs(point.y - SurfaceTop(ghost)) * 0.5f;
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                best = ghost;
            }

            return best;
        }

        static Quaternion FlatRotation(Transform source)
        {
            Vector3 forward = source.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            return Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        static Vector3 FlatDirection(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude < 0.0001f ? Vector3.forward : v.normalized;
        }

        // ---- Under: support pillar below a foundation ----

        static bool TrySeatUnder(DMBuildingGhost hitGhost, Vector3 point, DMBuildingPiece piece, out Vector3 position, out Quaternion rotation, out DMBuildingGhost anchor)
        {
            position = default;
            rotation = Quaternion.identity;
            anchor = Kit2Pick(hitGhost, point, Kit2SearchMeters, DMBuildingCatalog.IsFoundation);
            if (anchor == null)
                return false;

            Vector3 center = HorizontalPlacementCenter(anchor);
            Vector3 right = FlatDirection(anchor.transform.right);
            Vector3 forward = FlatDirection(anchor.transform.forward);
            Vector3 half = HalfExtents(anchor);
            Vector3 local = point - center;
            float u = Vector3.Dot(local, right);
            float v = Vector3.Dot(local, forward);
            float insetU = Mathf.Max(0f, half.x - piece.Size.x * 0.5f);
            float insetV = Mathf.Max(0f, half.z - piece.Size.z * 0.5f);
            Vector3 xz = center;
            // Near a corner: pillar sits in that corner. Otherwise it sits under the middle.
            if (Mathf.Abs(u) >= half.x - PillarCornerPickMeters && Mathf.Abs(v) >= half.z - PillarCornerPickMeters)
                xz = center + right * (Mathf.Sign(u) * insetU) + forward * (Mathf.Sign(v) * insetV);

            float bottom = anchor.transform.position.y - half.y;
            position = new Vector3(xz.x, bottom - piece.Size.y * 0.5f, xz.z);
            rotation = FlatRotation(anchor.transform);
            return true;
        }

        // ---- Corner: 4 m column / 2 m half column on a grid corner ----

        static bool TrySeatCorner(DMBuildingGhost hitGhost, Vector3 point, DMBuildingPiece piece, out Vector3 position, out Quaternion rotation, out DMBuildingGhost anchor)
        {
            position = default;
            rotation = Quaternion.identity;
            anchor = null;
            float half = piece.Size.y * 0.5f;

            // Stack on a column top.
            if (hitGhost != null && hitGhost.Built && DMBuildingCatalog.IsColumn(hitGhost.PieceId))
            {
                anchor = hitGhost;
                Vector3 c = HorizontalPlacementCenter(hitGhost);
                position = new Vector3(c.x, SurfaceTop(hitGhost) + half, c.z);
                rotation = FlatRotation(hitGhost.transform);
                return true;
            }

            // Wall top: nearest end of the wall's grid edge.
            if (hitGhost != null && hitGhost.Built && DMBuildingCatalog.IsStackableWall(hitGhost.PieceId))
            {
                DMBuildingGhost reference = NearestHorizontalLatticeReference(point, DMBuildingGhostProfile.LargeModuleMeters * 3f);
                if (reference != null)
                {
                    ModuleCornerLattice wallLattice = ModuleCornerLattice.FromSupport(reference);
                    wallLattice.SnapCornerIndices(point, out int wu, out int wv);
                    Vector3 corner = wallLattice.Corner(wu, wv);
                    anchor = hitGhost;
                    position = new Vector3(corner.x, SurfaceTop(hitGhost) + half, corner.z);
                    rotation = Quaternion.LookRotation(FlatDirection(wallLattice.Forward), Vector3.up);
                    return true;
                }
            }

            // Slab or foundation: nearest corner of its cell, standing on its top.
            DMBuildingGhost support = Kit2Pick(hitGhost, point, Kit2SearchMeters, DMBuildingCatalog.IsEdgeSupport);
            if (support == null)
                return false;

            ModuleCornerLattice lattice = ModuleCornerLattice.FromSupport(support);
            lattice.SnapCornerIndices(ClampAimToSupportCell(support, point), out int iu, out int iv);
            Vector3 seat = lattice.Corner(iu, iv);
            anchor = support;
            position = new Vector3(seat.x, SurfaceTop(support) + half, seat.z);
            rotation = Quaternion.LookRotation(FlatDirection(lattice.Forward), Vector3.up);
            return true;
        }

        // ---- Beam: 4 m beam along a wall top or out from a column top ----

        static bool TrySeatBeam(DMBuildingGhost hitGhost, Vector3 point, DMBuildingPiece piece, out Vector3 position, out Quaternion rotation, out DMBuildingGhost anchor)
        {
            position = default;
            rotation = Quaternion.identity;
            anchor = null;
            float half = piece.Size.y * 0.5f;

            if (hitGhost != null && hitGhost.Built && DMBuildingCatalog.IsColumn(hitGhost.PieceId))
            {
                DMBuildingGhost reference = NearestHorizontalLatticeReference(point, DMBuildingGhostProfile.LargeModuleMeters * 3f);
                Vector3 axisRight = reference != null ? FlatDirection(reference.transform.right) : FlatDirection(hitGhost.transform.right);
                Vector3 axisForward = reference != null ? FlatDirection(reference.transform.forward) : FlatDirection(hitGhost.transform.forward);
                Vector3 top = HorizontalPlacementCenter(hitGhost);
                Vector3 toAim = FlatDirection(point - top);
                float du = Vector3.Dot(toAim, axisRight);
                float dv = Vector3.Dot(toAim, axisForward);
                Vector3 dir = Mathf.Abs(du) >= Mathf.Abs(dv) ? axisRight * Mathf.Sign(du) : axisForward * Mathf.Sign(dv);
                Vector3 center = top + dir * (piece.Size.x * 0.5f);
                anchor = hitGhost;
                position = new Vector3(center.x, SurfaceTop(hitGhost) + half, center.z);
                rotation = Quaternion.LookRotation(Vector3.Cross(dir, Vector3.up), Vector3.up);
                return true;
            }

            DMBuildingGhost wall = hitGhost != null && hitGhost.Built && DMBuildingCatalog.IsStackableWall(hitGhost.PieceId)
                ? hitGhost
                : NearestWallTop(point, PlayerFeet());
            if (wall == null || !DMBuildingCatalog.IsStackableWall(wall.PieceId))
                return false;

            Vector3 wallCenter = HorizontalPlacementCenter(wall);
            anchor = wall;
            position = new Vector3(wallCenter.x, SurfaceTop(wall) + half, wallCenter.z);
            rotation = FlatRotation(wall.transform);
            return true;
        }

        // ---- Ridge cap on the peak of a roof slope ----

        static bool TrySeatRidgeCap(DMBuildingGhost hitGhost, Vector3 point, DMBuildingPiece piece, out Vector3 position, out Quaternion rotation, out DMBuildingGhost anchor)
        {
            position = default;
            rotation = Quaternion.identity;
            anchor = Kit2Pick(hitGhost, point, Kit2SearchMeters, DMBuildingCatalog.IsRidgeRoof);
            if (anchor == null)
                return false;

            Vector3 half = HalfExtents(anchor);
            Vector3 forward = FlatDirection(anchor.transform.forward);
            Vector3 center = HorizontalPlacementCenter(anchor) + forward * half.z;
            float y = SurfaceTop(anchor) + piece.Size.y * 0.5f - 0.1f;
            position = new Vector3(center.x, y, center.z);
            rotation = FlatRotation(anchor.transform);
            return true;
        }

        // ---- Overhang: foundation steps / balcony off a slab edge ----

        static bool TrySeatOverhang(DMBuildingGhost hitGhost, Vector3 point, DMBuildingPiece piece, float lift, out Vector3 position, out Quaternion rotation, out DMBuildingGhost anchor)
        {
            position = default;
            rotation = Quaternion.identity;
            System.Func<string, bool> accept = piece.Shape == DMBuildingShape.FoundationSteps
                ? (System.Func<string, bool>)DMBuildingCatalog.IsFoundation
                : DMBuildingCatalog.IsEdgeSupport;
            anchor = Kit2Pick(hitGhost, point, Kit2SearchMeters, accept);
            if (anchor == null)
                return false;

            ModuleCornerLattice lattice = ModuleCornerLattice.FromSupport(anchor);
            Vector3 cellCenter = lattice.SnapCellCenter(ClampAimToSupportCell(anchor, point), point.y);
            lattice.PickEdgeFromCellCenter(cellCenter, point, -1, out Vector3 a, out Vector3 b, out Vector3 outward, out Vector3 tangent, out int side);
            outward = FlatDirection(outward);
            Vector3 mid = (a + b) * 0.5f;
            Vector3 center = mid + outward * (piece.Size.z * 0.5f);
            float top = SurfaceTop(anchor);
            position = new Vector3(center.x, top - piece.Size.y * 0.5f + lift, center.z);
            // Local +Z (the high side of the steps / the balcony's wall side) faces back toward the building.
            rotation = Quaternion.LookRotation(-outward, Vector3.up);
            return true;
        }

        // ---- Lid: hatch lid on a built hatch ----

        static bool TrySeatLid(DMBuildingGhost hitGhost, Vector3 point, DMBuildingPiece piece, out Vector3 position, out Quaternion rotation, out DMBuildingGhost anchor)
        {
            position = default;
            rotation = Quaternion.identity;
            anchor = Kit2Pick(hitGhost, point, 3.5f, DMBuildingCatalog.IsHatch);
            if (anchor == null)
                return false;

            Vector3 center = HorizontalPlacementCenter(anchor);
            position = new Vector3(center.x, SurfaceTop(anchor) + piece.Size.y * 0.5f, center.z);
            rotation = FlatRotation(anchor.transform);
            return true;
        }

        // ---- Ladder: edge seat, then pressed against the inside wall face (flip = outside) ----

        static bool TryAimLadder(DMBuildingPiece piece, out Vector3 position, out Quaternion rotation, out bool canCommit)
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

            float lift = Mathf.Clamp(instance.heightOffset, -DMBuildingGhostProfile.MaxHeightOffsetMeters, DMBuildingGhostProfile.MaxHeightOffsetMeters);
            float yaw = GridLockedRotation(aim, instance.yawNotches).eulerAngles.y;
            if (!TrySnapEdge(aim, piece, yaw, lift, lookingUp, out Vector3 seat, out Quaternion edgeRotation))
                return false;

            bool anchored = PassesStructureAnchor(piece, seat, edgeRotation);

            // Which way is out of the cell: compare the seat with the centre of the cell it sits in.
            float halfDepth = VerticalPieceHalfThickness(piece);
            Vector3 normal = FlatDirection(edgeRotation * Vector3.forward);
            DMBuildingGhost reference = NearestHorizontalLatticeReference(seat, DMBuildingGhostProfile.LargeModuleMeters * 3f);
            if (reference != null)
            {
                ModuleCornerLattice lattice = ModuleCornerLattice.FromSupport(reference);
                Vector3 cell = lattice.SnapCellCenter(seat, seat.y);
                if (Vector3.Dot(seat - cell, normal) < 0f)
                    normal = -normal;
            }

            Vector3 line = seat + normal * halfDepth;
            bool outside = EdgeFlipped(instance.yawNotches);
            position = outside
                ? line + normal * halfDepth
                : line - normal * (DMBuildingCatalog.WallThickness + halfDepth);
            rotation = Quaternion.LookRotation(outside ? normal : -normal, Vector3.up);
            canCommit = DMBuildingCatalog.HasCost(piece) && anchored && !Overlaps(position, piece.Size, rotation, piece.Id);
            return true;
        }
    }
}

using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// 0927-ramps: stairs, half stairs, spiral stairs, ramps and half ramps.
    /// Order: chain onto a built stair / ramp under the crosshair, seat against a foundation side
    /// (high end meets the foundation top), the normal core seat (on top of slabs), then free on terrain.
    /// Build zone rules still apply afterwards through TryAim.
    /// </summary>
    public sealed partial class DMBuildingPlacementController
    {
        const float RampFoundationReachFactor = 0.75f;

        static bool IsRampLike(DMBuildingShape shape)
        {
            return shape == DMBuildingShape.Stairs
                || shape == DMBuildingShape.HalfStairs
                || shape == DMBuildingShape.SpiralStairs
                || shape == DMBuildingShape.Ramp
                || shape == DMBuildingShape.HalfRamp
                || IsRoofSlope(shape);
        }

        /// <summary>Straight roof slopes rise toward local +Z like a ramp (the ridge sits on the +Z edge).</summary>
        static bool IsRoofSlope(DMBuildingShape shape)
        {
            return shape == DMBuildingShape.Roof || shape == DMBuildingShape.SteepRoof;
        }

        static bool IsRampLikeId(string pieceId)
        {
            DMBuildingPiece piece = DMBuildingCatalog.Find(pieceId);
            return piece != null && IsRampLike(piece.Shape);
        }

        static bool TryAimRamp(DMBuildingPiece piece, out Vector3 position, out Quaternion rotation, out bool canCommit)
        {
            position = default;
            rotation = Quaternion.identity;
            canCommit = false;
            Camera camera = Camera.main;
            if (camera == null)
                return false;

            Ray ray = BuildAimRay(camera);
            float lift = Mathf.Clamp(instance.heightOffset, -DMBuildingGhostProfile.MaxHeightOffsetMeters, DMBuildingGhostProfile.MaxHeightOffsetMeters);
            bool hitBuilt = TryHitBuiltForSnap(ray, out DMBuildingGhost hitGhost, out RaycastHit hit);
            bool roof = IsRoofSlope(piece.Shape);

            // Roof slopes keep their normal seat (wall tops, gables) first; the rest is the fallback.
            if (roof && TryAimCore(out DMBuildingPiece roofPiece, out Vector3 roofPosition, out Quaternion roofRotation, out bool roofCommit)
                && roofCommit)
            {
                position = roofPosition;
                rotation = roofRotation;
                canCommit = true;
                return true;
            }

            // 1) Chain onto the stair / ramp under the crosshair.
            if (hitBuilt && IsRampLikeId(hitGhost.PieceId)
                && TrySeatRampChain(hitGhost, hit.point, piece, lift, out position, out rotation))
            {
                canCommit = RampCanCommit(piece, position, rotation);
                return true;
            }

            // 2) Crosshair on a foundation's side face: seat against that side.
            if (hitBuilt && DMBuildingCatalog.IsFoundation(hitGhost.PieceId) && Mathf.Abs(hit.normal.y) < 0.5f
                && TrySeatRampOnFoundationSide(hitGhost, hit.point, piece, lift, out position, out rotation))
            {
                canCommit = RampCanCommit(piece, position, rotation);
                return true;
            }

            // 3) Normal seat (top of a slab, upper stories). Keep it when it can actually be placed.
            if (!roof && TryAimCore(out DMBuildingPiece corePiece, out Vector3 corePosition, out Quaternion coreRotation, out bool coreCommit)
                && coreCommit)
            {
                position = corePosition;
                rotation = coreRotation;
                canCommit = true;
                return true;
            }

            if (!TryGroundHit(ray, out RaycastHit ground))
                return false;
            Vector3 aim = ground.point;

            // 4) Terrain right beside a foundation edge: seat against that side.
            float reach = Mathf.Max(piece.Size.z, 1f) * RampFoundationReachFactor;
            DMBuildingGhost foundation = NearestFoundation(aim, DMBuildingGhostProfile.LargeModuleMeters * 2f);
            if (foundation != null && DistanceOutsideFoundation(foundation, aim) <= reach
                && TrySeatRampOnFoundationSide(foundation, aim, piece, lift, out position, out rotation))
            {
                canCommit = RampCanCommit(piece, position, rotation);
                return true;
            }

            // 5) Free on terrain (outside any base). Near a base it still follows the grid yaw and cells.
            bool nearBase = NearestFoundation(aim, DMBuildingGhostProfile.LargeModuleMeters * 4f) != null;
            Vector3 xz = nearBase ? SnapModule(aim, piece) : aim;
            position = new Vector3(xz.x, aim.y + piece.Size.y * 0.5f + lift, xz.z);
            rotation = HasBuiltBase()
                ? GridLockedRotation(aim, instance.yawNotches)
                : Quaternion.Euler(0f, instance.yawNotches * YawStep(), 0f);
            canCommit = RampCanCommit(piece, position, rotation);
            return true;
        }

        static bool RampCanCommit(DMBuildingPiece piece, Vector3 position, Quaternion rotation)
        {
            return piece != null
                && HasCostOrMoving(piece)
                && !Overlaps(position, piece.Size, rotation, piece.Id);
        }

        /// <summary>Flat distance from the foundation's footprint (0 when inside it).</summary>
        static float DistanceOutsideFoundation(DMBuildingGhost foundation, Vector3 point)
        {
            Vector3 half = HalfExtents(foundation);
            Quaternion flat = FlatRotation(foundation.transform);
            Vector3 local = Quaternion.Inverse(flat) * (point - HorizontalPlacementCenter(foundation));
            float dx = Mathf.Max(0f, Mathf.Abs(local.x) - half.x);
            float dz = Mathf.Max(0f, Mathf.Abs(local.z) - half.z);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>High end (local +Z) faces the foundation; the piece's top meets the foundation top.</summary>
        static bool TrySeatRampOnFoundationSide(DMBuildingGhost foundation, Vector3 point, DMBuildingPiece piece, float lift, out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = Quaternion.identity;
            if (foundation == null)
                return false;

            ModuleCornerLattice lattice = ModuleCornerLattice.FromSupport(foundation);
            Vector3 cellCenter = lattice.SnapCellCenter(ClampAimToSupportCell(foundation, point), point.y);
            lattice.PickEdgeFromCellCenter(cellCenter, point, -1, out Vector3 a, out Vector3 b, out Vector3 outward, out Vector3 tangent, out int side);
            outward = FlatDirection(outward);
            if (outward.sqrMagnitude < 0.0001f)
                return false;

            Vector3 mid = (a + b) * 0.5f;
            Vector3 center = mid + outward * (piece.Size.z * 0.5f);
            float top = SurfaceTop(foundation);
            position = new Vector3(center.x, top - piece.Size.y * 0.5f + lift, center.z);
            rotation = Quaternion.LookRotation(-outward, Vector3.up);
            if (EdgeFlipped(instance.yawNotches))
                rotation *= Quaternion.Euler(0f, 180f, 0f); // scroll-turn: run along the side the other way up
            return true;
        }

        /// <summary>
        /// Side of a built stair / ramp: next to it, same level. High end: the next flight continues up.
        /// Low end: the next flight continues down. Spiral on spiral stacks straight up.
        /// </summary>
        static bool TrySeatRampChain(DMBuildingGhost anchor, Vector3 point, DMBuildingPiece piece, float lift, out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = Quaternion.identity;
            DMBuildingPiece anchorPiece = DMBuildingCatalog.Find(anchor.PieceId);
            if (anchorPiece == null)
                return false;

            Quaternion flat = FlatRotation(anchor.transform);
            Vector3 right = flat * Vector3.right;
            Vector3 forward = flat * Vector3.forward;
            Vector3 center = HorizontalPlacementCenter(anchor);
            Vector3 hA = anchorPiece.Size * 0.5f;
            Vector3 hP = piece.Size * 0.5f;
            Vector3 d = point - center;
            float lx = Vector3.Dot(d, right);
            float lz = Vector3.Dot(d, forward);
            rotation = flat;

            if (anchorPiece.Shape == DMBuildingShape.SpiralStairs && piece.Shape == DMBuildingShape.SpiralStairs)
            {
                bool below = point.y < center.y - hA.y * 0.5f;
                float y = below ? center.y - hA.y - hP.y : center.y + hA.y + hP.y;
                position = new Vector3(center.x, y + lift, center.z);
                return true;
            }

            float nx = Mathf.Abs(lx) / Mathf.Max(0.01f, hA.x);
            float nz = Mathf.Abs(lz) / Mathf.Max(0.01f, hA.z);
            Vector3 xz;
            float cy;
            if (nx > nz)
            {
                xz = center + right * (Mathf.Sign(lx) * (hA.x + hP.x));
                cy = center.y - hA.y + hP.y;
            }
            else if (lz >= 0f && IsRoofSlope(anchorPiece.Shape) && IsRoofSlope(piece.Shape))
            {
                // Roof peak: the other half of the gable, facing back, same height.
                xz = center + forward * (hA.z + hP.z);
                cy = center.y - hA.y + hP.y;
                rotation = flat * Quaternion.Euler(0f, 180f, 0f);
            }
            else if (lz >= 0f)
            {
                xz = center + forward * (hA.z + hP.z);
                cy = center.y + hA.y + hP.y;
            }
            else
            {
                xz = center - forward * (hA.z + hP.z);
                cy = center.y - hA.y - hP.y;
            }

            position = new Vector3(xz.x, cy + lift, xz.z);
            return true;
        }

        /// <summary>
        /// 0927-ramps: walls, railings, windows and other edge pieces aimed at a foundation's side face sit flush
        /// against the outside of that face, standing on the ground beside it (Shift + scroll raises / lowers).
        /// </summary>
        static bool TryAimEdgeOnFoundationSide(DMBuildingPiece piece, out Vector3 position, out Quaternion rotation, out bool canCommit)
        {
            position = default;
            rotation = Quaternion.identity;
            canCommit = false;
            Camera camera = Camera.main;
            if (camera == null || piece == null)
                return false;

            Ray ray = BuildAimRay(camera);
            if (!TryHitBuiltForSnap(ray, out DMBuildingGhost foundation, out RaycastHit hit))
                return false;
            if (!DMBuildingCatalog.IsFoundation(foundation.PieceId) || Mathf.Abs(hit.normal.y) >= 0.5f)
                return false;

            ModuleCornerLattice lattice = ModuleCornerLattice.FromSupport(foundation);
            Vector3 cellCenter = lattice.SnapCellCenter(ClampAimToSupportCell(foundation, hit.point), hit.point.y);
            lattice.PickEdgeFromCellCenter(cellCenter, hit.point, -1, out Vector3 a, out Vector3 b, out Vector3 outward, out Vector3 tangent, out int side);
            outward = FlatDirection(outward);

            float lift = Mathf.Clamp(instance.heightOffset, -DMBuildingGhostProfile.MaxHeightOffsetMeters, DMBuildingGhostProfile.MaxHeightOffsetMeters);
            float top = SurfaceTop(foundation);
            Vector3 mid = (a + b) * 0.5f;
            Vector3 center = mid + outward * (piece.Size.z * 0.5f);
            float bottom = GroundYBeside(center, top, top - HalfExtents(foundation).y * 2f);
            bottom = Mathf.Min(bottom, top - 0.1f);
            position = new Vector3(center.x, bottom + piece.Size.y * 0.5f + lift, center.z);
            rotation = Quaternion.LookRotation(outward, Vector3.up);
            if (EdgeFlipped(instance.yawNotches))
                rotation *= Quaternion.Euler(0f, 180f, 0f);
            canCommit = RampCanCommit(piece, position, rotation);
            return true;
        }

        /// <summary>Terrain height under a point (building pieces ignored); fallback when nothing is hit.</summary>
        static float GroundYBeside(Vector3 point, float fromY, float fallback)
        {
            Ray down = new Ray(new Vector3(point.x, fromY + 1f, point.z), Vector3.down);
            RaycastHit[] hits = rayHits;
            int count = Physics.RaycastNonAlloc(down, rayHits, 60f, DMBuildingLayers.GroundMask, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            float y = fallback;
            for (int i = 0; i < count; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null || collider.gameObject.layer == 8)
                    continue;
                if (collider.GetComponentInParent<DMBuildingGhost>() != null)
                    continue;
                if (collider.GetComponentInParent<DMBuildingPlacementController>() != null)
                    continue;
                if (hits[i].distance >= best)
                    continue;
                best = hits[i].distance;
                y = hits[i].point.y;
            }

            return y;
        }

        /// <summary>0927-ramps: foundation steps chain sideways off built steps, or step down and out from the outer edge.</summary>
        static bool TrySeatStepsChain(DMBuildingGhost hitGhost, Vector3 point, DMBuildingPiece piece, float lift, out Vector3 position, out Quaternion rotation, out DMBuildingGhost anchor)
        {
            position = default;
            rotation = Quaternion.identity;
            anchor = null;
            if (hitGhost == null || !hitGhost.Built)
                return false;
            DMBuildingPiece anchorPiece = DMBuildingCatalog.Find(hitGhost.PieceId);
            if (anchorPiece == null || anchorPiece.Shape != DMBuildingShape.FoundationSteps)
                return false;

            Quaternion flat = FlatRotation(hitGhost.transform);
            Vector3 right = flat * Vector3.right;
            Vector3 forward = flat * Vector3.forward; // toward the building (high side)
            Vector3 center = HorizontalPlacementCenter(hitGhost);
            Vector3 hA = anchorPiece.Size * 0.5f;
            Vector3 d = point - center;
            float lx = Vector3.Dot(d, right);
            float lz = Vector3.Dot(d, forward);
            float nx = Mathf.Abs(lx) / Mathf.Max(0.01f, hA.x);
            float nz = Mathf.Abs(lz) / Mathf.Max(0.01f, hA.z);

            Vector3 xz;
            float cy;
            if (lz < 0f && nz > nx * 0.8f)
            {
                // Outer (low) edge: the next set steps down and out.
                xz = center - forward * (hA.z + piece.Size.z * 0.5f);
                cy = center.y - hA.y - piece.Size.y * 0.5f;
            }
            else
            {
                xz = center + right * (Mathf.Sign(lx == 0f ? 1f : lx) * (hA.x + piece.Size.x * 0.5f));
                cy = center.y - hA.y + piece.Size.y * 0.5f;
            }

            position = new Vector3(xz.x, cy + lift, xz.z);
            rotation = flat;
            anchor = hitGhost;
            return true;
        }
    }
}

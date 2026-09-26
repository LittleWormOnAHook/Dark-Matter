using Project.Data;
using Project.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

namespace Project.Building
{
    /// <summary>
    /// Preview ghost follows aim. Hold left click to create it when the seat is green.
    /// Red means not enough of the style cost item (Rock, Iron Ore, Silicate Ore) in inventory or storage. Hold right click to destroy.
    /// </summary>
    public sealed class DMBuildingPlacementController : MonoBehaviour
    {
        static DMBuildingPlacementController instance;
        static Material ghostMaterial;
        static Material solidMaterial;
        static int solidMaterialSourceId;
        float nextColorRefresh;
        static Material blockedMaterial;
        static Material glassMaterial;
        static Material ghostGlassMaterial;
        static int validGhostMaterialSourceId = int.MinValue;
        static int blockedGhostMaterialSourceId = int.MinValue;

        GameObject preview;
        Material previewTintMaterial;
        string previewId;
        float buildHold;
        float destroyHold;
        DMBuildingGhost lastPlaced;
        DMBuildingGhost destroyFocus;
        Vector3 holdSeat;
        // 0925-rotate: Alt + scroll notches. Free pieces turn by YawStep, grid-locked pieces by whole quarter turns.
        int yawNotches;
        float heightOffset;
        string adjustPieceId;
        string heightResetPieceId;
        int stickyVerticalSide = -1;
        // DM snap 0925-support-cell: keep one horizontal support while aim stays on/near it.
        DMBuildingGhost stickyVerticalSupport;

        static int builtGhostCacheVersion = -1;
        static DMBuildingGhost[] builtGhostCache = System.Array.Empty<DMBuildingGhost>();
        // 0925-layers: shared cast buffers so aim and overlap tests stop allocating every frame.
        static readonly RaycastHit[] rayHits = new RaycastHit[64];
        static readonly Collider[] overlapHits = new Collider[128];
        static Project.Player.PlayerController cachedPlayer;
        float nextWorldMaskCheck;

        /// <summary>Placed pieces. Rebuilt only when a piece is enabled or disabled, not every frame.</summary>
        internal static DMBuildingGhost[] BuiltGhosts()
        {
            int version = DMBuildingGhost.RegistryVersion;
            if (builtGhostCacheVersion == version)
                return builtGhostCache;

            builtGhostCacheVersion = version;
            builtGhostCache = DMBuildingGhost.Snapshot();
            return builtGhostCache;
        }

        static void InvalidateBuiltGhostCache()
        {
            builtGhostCacheVersion = -1;
        }

        static bool IsHorizontalLatticePiece(string pieceId)
        {
            return DMBuildingCatalog.IsEdgeSupport(pieceId);
        }

        static DMBuildingGhost NearestHorizontalLatticeReference(Vector3 aim, float searchRadius)
        {
            DMBuildingGhost[] ghosts = BuiltGhosts();
            DMBuildingGhost bestFloor = null;
            float bestFloorDistance = searchRadius;
            DMBuildingGhost bestAny = null;
            float bestAnyDistance = searchRadius;
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost == null || !ghost.Built || !IsHorizontalLatticePiece(ghost.PieceId))
                    continue;

                float distance = FlatDistance(aim, HorizontalPlacementCenter(ghost));
                if (DMBuildingCatalog.IsFloor(ghost.PieceId))
                {
                    if (distance >= bestFloorDistance)
                        continue;
                    bestFloorDistance = distance;
                    bestFloor = ghost;
                    continue;
                }

                if (distance >= bestAnyDistance)
                    continue;
                bestAnyDistance = distance;
                bestAny = ghost;
            }

            return bestFloor != null ? bestFloor : bestAny;
        }

        public static void Ensure()
        {
            if (!Application.isPlaying || instance != null)
                return;

            var host = new GameObject("DMBuildingPlacement");
            host.hideFlags = HideFlags.HideAndDontSave;
            instance = host.AddComponent<DMBuildingPlacementController>();
        }

        public static void Release()
        {
            if (instance == null)
                return;

            DMBuildingPlacementController host = instance;
            instance = null;
            host.DestroyPreview();
            if (Application.isPlaying)
                Destroy(host.gameObject);
            else
                DestroyImmediate(host.gameObject);
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        void OnEnable()
        {
            DMBuildingMode.Changed += OnBuildingModeChanged;
        }

        void OnDisable()
        {
            DMBuildingMode.Changed -= OnBuildingModeChanged;
            DestroyPreview();
        }

        void OnBuildingModeChanged()
        {
            DMBuildingPiece piece = DMBuildingMode.SelectedPiece;
            string id = piece != null ? piece.Id : null;
            if (id == heightResetPieceId)
                return;

            heightResetPieceId = id;
            heightOffset = 0f;
            stickyVerticalSide = -1;
            stickyVerticalSupport = null;
        }

        void OnDestroy()
        {
            DestroyPreview();
            if (instance == this)
                instance = null;
        }

        void Update()
        {
            if (!Application.isPlaying)
            {
                DestroyPreview();
                return;
            }

            // 0925-layers: characters and the camera must treat the Building layer like Default.
            if (Time.unscaledTime >= nextWorldMaskCheck)
            {
                nextWorldMaskCheck = Time.unscaledTime + 3f;
                if (DMBuildingMode.IsActive || BuiltGhosts().Length > 0)
                    DMBuildingLayers.EnsureWorldMasks();
            }

            if (!DMBuildingMode.IsActive)
            {
                buildHold = 0f;
                destroyHold = 0f;
                destroyFocus = null;
                lastPlaced = null;
                yawNotches = 0;
                heightOffset = 0f;
                stickyVerticalSide = -1;
                stickyVerticalSupport = null;
                if (preview != null)
                    preview.SetActive(false);
                DMUiToolkitBuildingHotbar.SetHoldRing(false, 0f, Vector3.zero);
                return;
            }

            // 0925-adjust-reset: height and rotate belong to one pick. Switching pieces starts clean.
            DMBuildingPiece picked = DMBuildingMode.SelectedPiece;
            string pickedId = picked != null ? picked.Id : null;
            if (pickedId != adjustPieceId)
            {
                adjustPieceId = pickedId;
                yawNotches = 0;
                heightOffset = 0f;
                stickyVerticalSide = -1;
                stickyVerticalSupport = null;
                buildHold = 0f;
            }

            ConsumeWheel();
            // 0926-perf: Studio colour edits show within a quarter second; no need to repaint five materials every frame.
            if (Time.unscaledTime >= nextColorRefresh)
            {
                nextColorRefresh = Time.unscaledTime + 0.25f;
                ApplyProfileColors();
            }

            Mouse mouse = Mouse.current;
            bool overBar = DMUiToolkitBuildingHotbar.PointerOverBar();
            bool leftHeld = mouse != null && mouse.leftButton.isPressed;
            bool rightHeld = mouse != null && mouse.rightButton.isPressed;

            if (WasMaterialKeyPressed())
                TryApplyNextMaterial();

            // Hold right click to destroy. The ring is the same center-screen hold as building.
            // Refund runs only when the hold completes. Looking directly at the piece is optional:
            // the crosshair hit wins, otherwise the nearest ghost in aim range.
            if (rightHeld && !overBar && TryResolveDestroyTarget(out DMBuildingGhost destroyTarget))
            {
                if (destroyFocus != destroyTarget)
                {
                    destroyFocus = destroyTarget;
                    destroyHold = 0f;
                }

                destroyHold += Time.deltaTime;
                DMUiToolkitBuildingHotbar.SetHoldRing(true, destroyHold / DMBuildingGhostProfile.DestroyHoldSeconds, Vector3.zero);
                if (destroyHold >= DMBuildingGhostProfile.DestroyHoldSeconds)
                {
                    if (lastPlaced == destroyTarget)
                        lastPlaced = null;
                    RefundAndDestroy(destroyTarget);
                    destroyHold = 0f;
                    destroyFocus = null;
                    DMUiToolkitBuildingHotbar.SetHoldRing(false, 0f, Vector3.zero);
                }

                if (preview != null)
                    preview.SetActive(false);
                return;
            }

            destroyHold = 0f;
            destroyFocus = null;

            DMBuildingPiece aimedPiece = null;
            Vector3 aimedPosition = default;
            Quaternion aimedRotation = Quaternion.identity;
            bool canCommit = false;
            bool seated = !overBar && TryAim(out aimedPiece, out aimedPosition, out aimedRotation, out canCommit);

            if (leftHeld && seated && canCommit)
            {
                if ((aimedPosition - holdSeat).sqrMagnitude > 0.04f)
                    buildHold = 0f;
                holdSeat = aimedPosition;
                buildHold += Time.deltaTime;
                DMUiToolkitBuildingHotbar.SetHoldRing(true, buildHold / DMBuildingGhostProfile.BuildSeconds, Vector3.zero);
                if (buildHold >= DMBuildingGhostProfile.BuildSeconds
                    && DMBuildingCatalog.TrySpend(aimedPiece, out ItemData paid))
                {
                    lastPlaced = Commit(aimedPiece, aimedPosition, aimedRotation, paid);
                    buildHold = 0f;
                    DMUiToolkitBuildingHotbar.SetHoldRing(false, 0f, Vector3.zero);
                }
            }
            else
            {
                buildHold = 0f;
                if (!leftHeld)
                    DMUiToolkitBuildingHotbar.SetHoldRing(false, 0f, Vector3.zero);
            }

            if (!seated)
            {
                if (preview != null)
                    preview.SetActive(false);
                return;
            }

            EnsurePreview(aimedPiece.Id);
            if (preview == null)
                return;
            preview.SetActive(true);
            preview.transform.SetPositionAndRotation(aimedPosition, aimedRotation);
            // 0925-perf: repaint the ghost only when valid/blocked flips, not every frame.
            Material tint = canCommit ? GhostMaterial() : BlockedMaterial();
            if (tint != previewTintMaterial)
            {
                ApplyTint(preview, tint, keepGlass: true);
                previewTintMaterial = tint;
            }
        }

        static bool TryAim(out DMBuildingPiece piece, out Vector3 position, out Quaternion rotation, out bool canCommit)
        {
            piece = DMBuildingMode.SelectedPiece;
            position = default;
            rotation = Quaternion.identity;
            canCommit = false;
            if (piece == null || instance == null)
                return false;

            Camera camera = Camera.main;
            if (camera == null)
                return false;

            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // Library: surface items (lights, decorations) stick to the face of the built piece under the crosshair.
            if (piece.Snap == DMBuildingSnap.Surface)
            {
                surfaceHost = null;
                if (!TrySeatSurfaceItem(ray, piece, camera, out position, out rotation, out DMBuildingGhost host))
                    return false;
                surfaceHost = host;
                canCommit = DMBuildingCatalog.HasCost(piece);
                return true;
            }

            bool lookingUp = ray.direction.y > 0.18f || AimIsAboveWallMid(ray);
            ResolveAimPoint(ray, lookingUp, out Vector3 aim, out bool hasHit);
            if (!hasHit && !lookingUp)
                return false;

            Vector3 feet = PlayerFeet();
            float yaw = instance.yawNotches * YawStep();
            float lift = Mathf.Clamp(instance.heightOffset, -DMBuildingGhostProfile.MaxHeightOffsetMeters, DMBuildingGhostProfile.MaxHeightOffsetMeters);
            rotation = Quaternion.Euler(0f, yaw, 0f);
            bool hasBase = HasBuiltBase();

            if (!hasBase)
            {
                if (!DMBuildingCatalog.IsFoundation(piece.Id))
                    return false;
                if (!hasHit)
                    return false;

                position = SnapModule(aim, piece);
                position.y = SeatY(aim.y, piece, lift);
                canCommit = CanAffordAndClear(piece, position, rotation);
                return true;
            }

            // 0925-rotate: after the first foundation every piece follows the building grid yaw.
            rotation = GridLockedRotation(aim, instance.yawNotches);
            yaw = rotation.eulerAngles.y;

            // 0925-upper: above the ground story the built piece under the crosshair is the anchor.
            if (TrySnapFromHitPiece(ray, piece, lift, out Vector3 hitSeat, out Quaternion hitRotation, out float hitGround))
            {
                position = hitSeat;
                rotation = hitRotation;
                int upperLayer = float.IsNegativeInfinity(hitGround)
                    ? 0
                    : StackLayer(position.y - piece.Size.y * 0.5f, hitGround);
                canCommit = DMBuildingCatalog.IsStackLayerAllowed(upperLayer) && CanAffordAndClear(piece, position, rotation);
                return true;
            }

            if (piece.Snap == DMBuildingSnap.Door)
            {
                DMBuildingGhost frame = FindDoorFrame(aim);
                if (frame == null)
                    return false;

                SeatDoor(frame, lift, out position, out rotation);
                canCommit = CanAffordAndClear(piece, position, rotation);
                return true;
            }

            if (lookingUp
                && !DMBuildingCatalog.IsFoundation(piece.Id)
                && TrySeatOnNearestTop(aim, piece, yaw, lift, out position, out rotation))
            {
                int layer = StackLayer(position.y - piece.Size.y * 0.5f, feet.y);
                canCommit = DMBuildingCatalog.IsStackLayerAllowed(layer) && CanAffordAndClear(piece, position, rotation);
                return true;
            }

            if (DMBuildingCatalog.IsFloor(piece.Id) || DMBuildingCatalog.IsCeiling(piece.Id))
            {
                if (TrySnapFloorOrCeiling(aim, piece, lift, lookingUp, out position, out int layer))
                {
                    canCommit = DMBuildingCatalog.IsStackLayerAllowed(layer) && CanAffordAndClear(piece, position, rotation);
                    return true;
                }

                return false;
            }

            if (DMBuildingCatalog.IsFoundation(piece.Id))
            {
                if (TrySnapModuleToNeighbor(aim, piece, lift, out position))
                {
                    canCommit = CanAffordAndClear(piece, position, rotation);
                    return true;
                }

                return false;
            }

            if (piece.Snap == DMBuildingSnap.Edge
                && TrySnapEdge(aim, piece, yaw, lift, lookingUp, out position, out rotation))
            {
                if (EdgeFlipped(instance.yawNotches))
                    rotation *= Quaternion.Euler(0f, 180f, 0f);
                canCommit = CanAffordAndClear(piece, position, rotation);
                return true;
            }

            if (TrySnapModuleToNeighbor(aim, piece, lift, out position))
            {
                canCommit = CanAffordAndClear(piece, position, rotation);
                return true;
            }

            return false;
        }

        // ---- Library: surface items ----

        static DMBuildingGhost surfaceHost;

        /// <summary>
        /// Walls: the item's forward (+Z) points out of the face and its up stays world up.
        /// Floors and ceilings: the item's up (+Y) points out of the face. Alt + scroll spins it around the face normal.
        /// </summary>
        static bool TrySeatSurfaceItem(Ray ray, DMBuildingPiece piece, Camera camera, out Vector3 position, out Quaternion rotation, out DMBuildingGhost host)
        {
            position = default;
            rotation = Quaternion.identity;
            host = null;
            if (!TryHitBuiltAlongRay(ray, DMBuildingGhostProfile.AimDistanceMeters, out RaycastHit hit))
                return false;

            host = hit.collider != null ? hit.collider.GetComponentInParent<DMBuildingGhost>() : null;
            if (host == null)
                return false;

            Vector3 normal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : Vector3.up;
            float spin = instance.yawNotches * YawStep();
            Quaternion baseRotation;
            float extent;
            if (Mathf.Abs(normal.y) < 0.5f)
            {
                baseRotation = Quaternion.LookRotation(normal, Vector3.up);
                extent = piece.Size.z * 0.5f;
            }
            else
            {
                Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, normal);
                if (forward.sqrMagnitude < 0.0001f)
                    forward = Vector3.ProjectOnPlane(camera.transform.up, normal);
                baseRotation = Quaternion.LookRotation(forward.normalized, normal);
                extent = piece.Size.y * 0.5f;
            }

            rotation = Quaternion.AngleAxis(spin, normal) * baseRotation;
            position = hit.point + normal * (extent + Mathf.Max(0f, piece.SurfaceOffset));
            return true;
        }

        // ---- 0925-upper: crosshair-anchored snapping for second story and higher ----

        static bool TryHitBuiltForSnap(Ray ray, out DMBuildingGhost ghost, out RaycastHit hit)
        {
            ghost = null;
            hit = default;
            RaycastHit[] hits = rayHits;
            int hitCount = Physics.RaycastNonAlloc(ray, rayHits, DMBuildingGhostProfile.AimDistanceMeters, DMBuildingLayers.AimMask, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                if (hits[i].collider == null || hits[i].distance >= best)
                    continue;
                DMBuildingGhost candidate = hits[i].collider.GetComponentInParent<DMBuildingGhost>();
                if (candidate == null || !candidate.Built)
                    continue;
                best = hits[i].distance;
                ghost = candidate;
                hit = hits[i];
            }

            return ghost != null;
        }

        static float GroundTopNear(Vector3 point)
        {
            DMBuildingGhost foundation = NearestFoundation(point, DMBuildingGhostProfile.LargeModuleMeters * 6f);
            return foundation != null ? SurfaceTop(foundation) : float.NegativeInfinity;
        }

        static bool TrySnapFromHitPiece(Ray ray, DMBuildingPiece piece, float lift, out Vector3 position, out Quaternion rotation, out float groundTop)
        {
            position = default;
            rotation = Quaternion.identity;
            groundTop = float.NegativeInfinity;
            if (piece == null || instance == null || piece.Snap == DMBuildingSnap.Door || DMBuildingCatalog.IsFoundation(piece.Id))
                return false;
            // 0925-last: the crosshair's piece wins; with nothing under it, the last placed piece anchors.
            DMBuildingGhost hitGhost;
            Vector3 hitPoint;
            if (TryHitBuiltForSnap(ray, out DMBuildingGhost rayGhost, out RaycastHit hit))
            {
                hitGhost = rayGhost;
                hitPoint = hit.point;
            }
            else if (!TryAimAtLastPlaced(ray, out hitGhost, out hitPoint))
                return false;
            if (DMBuildingCatalog.IsFoundation(hitGhost.PieceId))
                return false;

            groundTop = GroundTopNear(hitPoint);
            float top = SurfaceTop(hitGhost);
            float bottom = hitGhost.transform.position.y - HalfExtents(hitGhost).y;
            bool groundStory = bottom <= groundTop + 0.6f;

            if (DMBuildingCatalog.IsEdgeSupport(hitGhost.PieceId))
            {
                // Ground-story floors stay on the tuned first-floor path.
                if (groundStory)
                    return false;
                return TrySnapFromHitSlab(hitGhost, hitPoint, top, piece, lift, out position, out rotation);
            }

            if (DMBuildingCatalog.IsStackableWall(hitGhost.PieceId))
            {
                if (piece.Snap == DMBuildingSnap.Edge)
                {
                    bool upperHalf = hitPoint.y >= hitGhost.transform.position.y;
                    if (upperHalf)
                        return TryStackOnHitWall(hitGhost, top, piece, lift, out position, out rotation);
                    if (groundStory)
                        return false;
                    return TrySideOfHitWall(hitGhost, hitPoint, bottom, piece, lift, out position, out rotation);
                }

                return TrySeatOnHitWallTop(hitGhost, top, piece, lift, out position, out rotation);
            }

            return false;
        }

        static bool TryAimAtLastPlaced(Ray ray, out DMBuildingGhost ghost, out Vector3 point)
        {
            ghost = instance != null ? instance.lastPlaced : null;
            point = default;
            if (ghost == null || !ghost.isActiveAndEnabled)
            {
                ghost = null;
                return false;
            }

            float range = DMBuildingGhostProfile.AimDistanceMeters;
            float module = DMBuildingGhostProfile.LargeModuleMeters;
            Vector3 center = ghost.transform.position;
            float t = Vector3.Dot(center - ray.origin, ray.direction);
            if (DMBuildingCatalog.IsEdgeSupport(ghost.PieceId) && Mathf.Abs(ray.direction.y) > 0.05f)
            {
                float onTop = (SurfaceTop(ghost) - ray.origin.y) / ray.direction.y;
                if (onTop > 0f && onTop <= range)
                    t = onTop;
            }

            if (t <= 0f || t > range)
            {
                ghost = null;
                return false;
            }

            point = ray.origin + ray.direction * t;
            if (FlatDistance(point, center) > module * 1.25f || Mathf.Abs(point.y - center.y) > module)
            {
                ghost = null;
                return false;
            }

            return true;
        }

        static bool TrySnapFromHitSlab(DMBuildingGhost slab, Vector3 point, float top, DMBuildingPiece piece, float lift, out Vector3 position, out Quaternion rotation)
        {
            if (piece.Snap == DMBuildingSnap.Edge)
            {
                if (!TrySeatVerticalOnCornerEdge(slab, point, top, piece, lift, instance.stickyVerticalSide, out position, out rotation, out int side))
                    return false;
                instance.stickyVerticalSide = side;
                instance.stickyVerticalSupport = slab;
                if (EdgeFlipped(instance.yawNotches))
                    rotation *= Quaternion.Euler(0f, 180f, 0f);
                return true;
            }

            ModuleCornerLattice lattice = ModuleCornerLattice.FromSupport(slab);
            rotation = GridLockedRotation(point, instance.yawNotches);
            Vector3 center = slab.transform.position;
            if (DMBuildingCatalog.IsSlab(piece.Id))
            {
                // Extend at the same level into the neighbour cell the crosshair leans toward.
                Vector3 local = point - center;
                float u = Vector3.Dot(local, lattice.Right);
                float v = Vector3.Dot(local, lattice.Forward);
                Vector3 step = Mathf.Abs(u) >= Mathf.Abs(v)
                    ? lattice.Right * (u >= 0f ? 1f : -1f)
                    : lattice.Forward * (v >= 0f ? 1f : -1f);
                float seatY = top - piece.Size.y * 0.5f + lift;
                position = lattice.SnapCellCenter(center + step * lattice.Module, seatY);
                return true;
            }

            // Stairs, ramps and roofs sit on top of the slab cell under the crosshair.
            position = lattice.SnapCellCenter(center, SeatCenterY(top, piece, lift));
            return true;
        }

        static bool TryStackOnHitWall(DMBuildingGhost wall, float top, DMBuildingPiece piece, float lift, out Vector3 position, out Quaternion rotation)
        {
            position = wall.transform.position;
            position.y = SeatCenterY(top, piece, lift);
            rotation = wall.transform.rotation;
            if (EdgeFlipped(instance.yawNotches))
                rotation *= Quaternion.Euler(0f, 180f, 0f);
            return true;
        }

        static bool TrySideOfHitWall(DMBuildingGhost wall, Vector3 point, float bottom, DMBuildingPiece piece, float lift, out Vector3 position, out Quaternion rotation)
        {
            Vector3 run = wall.transform.right;
            run.y = 0f;
            if (run.sqrMagnitude < 0.0001f)
            {
                position = default;
                rotation = Quaternion.identity;
                return false;
            }

            run.Normalize();
            float sign = Vector3.Dot(point - wall.transform.position, run) >= 0f ? 1f : -1f;
            position = wall.transform.position + run * (sign * DMBuildingGhostProfile.LargeModuleMeters);
            position.y = SeatCenterY(bottom, piece, lift);
            rotation = wall.transform.rotation;
            if (EdgeFlipped(instance.yawNotches))
                rotation *= Quaternion.Euler(0f, 180f, 0f);
            return true;
        }

        static bool TrySeatOnHitWallTop(DMBuildingGhost wall, float top, DMBuildingPiece piece, float lift, out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = Quaternion.identity;
            Vector3 thin = wall.transform.forward;
            thin.y = 0f;
            if (thin.sqrMagnitude < 0.0001f)
                return false;
            thin.Normalize();

            DMBuildingGhost reference = NearestHorizontalLatticeReference(wall.transform.position, DMBuildingGhostProfile.LargeModuleMeters * 6f);
            if (reference == null)
                return false;

            ModuleCornerLattice lattice = ModuleCornerLattice.FromSupport(reference);
            Camera camera = Camera.main;
            float camSide = camera != null && Vector3.Dot(camera.transform.position - wall.transform.position, thin) < 0f ? -1f : 1f;
            float seatY = SeatCenterY(top, piece, lift);
            float reach = lattice.Module * 0.5f;
            rotation = GridLockedRotation(wall.transform.position, instance.yawNotches);
            Vector3 near = lattice.SnapCellCenter(wall.transform.position + thin * (camSide * reach), seatY);
            Vector3 far = lattice.SnapCellCenter(wall.transform.position - thin * (camSide * reach), seatY);
            position = near;
            if (Overlaps(near, piece.Size, rotation, piece.Id) && !Overlaps(far, piece.Size, rotation, piece.Id))
                position = far;
            return true;
        }

        static bool CanAffordAndClear(DMBuildingPiece piece, Vector3 position, Quaternion rotation)
        {
            return piece != null
                && DMBuildingCatalog.HasCost(piece)
                && PassesStructureAnchor(piece, position, rotation)
                && !Overlaps(position, piece.Size, rotation, piece.Id);
        }

        /// <summary>
        /// First foundation uses terrain aim only. After that, every seat must touch the built graph.
        /// </summary>
        static bool PassesStructureAnchor(DMBuildingPiece piece, Vector3 position, Quaternion rotation)
        {
            if (piece == null)
                return false;

            if (!HasBuiltBase())
                return DMBuildingCatalog.IsFoundation(piece.Id);

            return IsAnchoredToBuiltPiece(piece, position, rotation);
        }

        static bool HasBuiltBase()
        {
            DMBuildingGhost[] ghosts = BuiltGhosts();
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost != null && ghost.Built && DMBuildingCatalog.IsFoundation(ghost.PieceId))
                    return true;
            }

            return false;
        }

        static bool IsAnchoredToBuiltPiece(DMBuildingPiece piece, Vector3 position, Quaternion rotation)
        {
            if (piece.Snap == DMBuildingSnap.Door)
            {
                DMBuildingGhost frame = FindDoorFrame(position);
                return frame != null && frame.Built;
            }

            float halfY = piece.Size.y * 0.5f;
            float bottom = position.y - halfY;
            float module = ModuleFor(piece);
            float yTol = 0.5f;
            float xzTol = module * 0.55f;

            DMBuildingGhost[] ghosts = BuiltGhosts();
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost support = ghosts[i];
                if (support == null || !support.Built)
                    continue;

                float top = SurfaceTop(support);
                Vector3 supportPos = HorizontalPlacementCenter(support);

                if (Mathf.Abs(bottom - top) <= yTol)
                {
                    if (FlatDistance(position, supportPos) <= xzTol)
                        return true;
                }

                if (DMBuildingCatalog.IsFoundation(piece.Id) && DMBuildingCatalog.IsEdgeSupport(support.PieceId))
                {
                    float neighbor = FlatDistance(position, supportPos);
                    float centerY = support.transform.position.y;
                    if (Mathf.Abs(position.y - centerY) <= yTol && IsModuleGridDistance(neighbor, module))
                        return true;
                }

                if (DMBuildingCatalog.IsSlab(piece.Id)
                    && (DMBuildingCatalog.IsEdgeSupport(support.PieceId) || DMBuildingCatalog.IsStackableWall(support.PieceId)))
                {
                    float expectedY = SeatCenterY(top, piece, 0f);
                    if (Mathf.Abs(expectedY - position.y) <= yTol)
                    {
                        float neighbor = FlatDistance(position, supportPos);
                        if (IsModuleGridDistance(neighbor, module))
                            return true;
                    }
                }

                if (piece.Snap != DMBuildingSnap.Edge || !DMBuildingCatalog.IsEdgeSupport(support.PieceId))
                    continue;

                if (Mathf.Abs(SeatCenterY(top, piece, 0f) - position.y) > yTol)
                    continue;

                if (IsEdgeSeatOnSupport(support, position, piece))
                    return true;
            }

            return false;
        }

        static bool IsEdgeSeatOnSupport(DMBuildingGhost support, Vector3 position, DMBuildingPiece piece)
        {
            float top = SurfaceTop(support);

            if (TrySeatVerticalOnCornerEdge(support, position, top, piece, 0f, out Vector3 seat, out _, out _, out _, out _))
            {
                if (FlatDistance(seat, position) <= 0.08f && Mathf.Abs(seat.y - position.y) <= 0.08f)
                    return true;
            }

            if (DMBuildingCatalog.IsVerticalSupport(support.PieceId)
                && FlatDistance(position, HorizontalPlacementCenter(support)) <= DMBuildingGhostProfile.TopSnapRangeMeters
                && Mathf.Abs(position.y - piece.Size.y * 0.5f - top) <= 0.5f)
                return true;

            return false;
        }

        static bool AimIsAboveWallMid(Ray ray)
        {
            RaycastHit[] hits = rayHits;
            int hitCount = Physics.RaycastNonAlloc(ray, rayHits, DMBuildingGhostProfile.AimDistanceMeters, DMBuildingLayers.AimMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                if (hits[i].collider == null)
                    continue;
                DMBuildingGhost ghost = hits[i].collider.GetComponentInParent<DMBuildingGhost>();
                if (ghost == null || !ghost.Built || !DMBuildingCatalog.IsVerticalSupport(ghost.PieceId))
                    continue;
                if (hits[i].point.y >= ghost.transform.position.y)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Look-up seats the piece on the nearest built wall top. Foundation edges do not get a vote.
        /// </summary>
        static bool TrySeatOnNearestTop(
            Vector3 aim,
            DMBuildingPiece piece,
            float yaw,
            float lift,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = default;
            rotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 feet = PlayerFeet();
            DMBuildingGhost wall = NearestWallTop(aim, feet);
            if (wall == null)
                return false;

            float top = SurfaceTop(wall);
            if (piece.Snap == DMBuildingSnap.Edge)
            {
                position = HorizontalPlacementCenter(wall);
                position.y = SeatCenterY(top, piece, lift);
                rotation = Quaternion.LookRotation(wall.transform.forward, Vector3.up);
                return true;
            }

            float module = ModuleFor(piece);
            Vector3 anchor = FlatDistance(aim, feet) > DMBuildingGhostProfile.TopSnapRangeMeters ? feet : aim;
            position = SnapToOuterModuleGrid(anchor, piece, SeatCenterY(top, piece, lift));
            return true;
        }

        static DMBuildingGhost SelectHorizontalSupportForVertical(Vector3 aim, Vector3 feet, DMBuildingPiece piece, float heightOffset)
        {
            float search = DMBuildingGhostProfile.EdgeSnapRangeMeters * 1.5f;
            DMBuildingGhost bestFloor = null;
            float bestFloorScore = float.MaxValue;
            DMBuildingGhost bestOther = null;
            float bestOtherScore = float.MaxValue;
            DMBuildingGhost[] ghosts = BuiltGhosts();
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost == null || !ghost.Built || !IsHorizontalLatticePiece(ghost.PieceId))
                    continue;

                float top = SurfaceTop(ghost);
                if (!SupportsSnapStory(top, aim, feet))
                    continue;

                float distance = FlatDistance(aim, HorizontalPlacementCenter(ghost));
                if (distance >= search)
                    continue;

                float seatY = SeatCenterY(top, piece, heightOffset);
                float score = distance + Mathf.Abs(aim.y - seatY) * 0.15f;
                if (instance != null && ghost == instance.stickyVerticalSupport
                    && AimInsideSupportFootprint(ghost, aim, StickySupportMarginMeters))
                    score -= StickySupportBiasMeters;
                if (DMBuildingCatalog.IsFloor(ghost.PieceId))
                {
                    if (score >= bestFloorScore)
                        continue;
                    bestFloorScore = score;
                    bestFloor = ghost;
                }
                else
                {
                    if (score >= bestOtherScore)
                        continue;
                    bestOtherScore = score;
                    bestOther = ghost;
                }
            }

            DMBuildingGhost chosen = bestFloor != null ? bestFloor : bestOther;
            if (instance != null)
                instance.stickyVerticalSupport = chosen;
            return chosen;
        }

        const float StickySupportMarginMeters = 0.6f;
        const float StickySupportBiasMeters = 1.5f;

        static bool AimInsideSupportFootprint(DMBuildingGhost support, Vector3 aim, float margin)
        {
            if (support == null)
                return false;

            GetCatalogFootprint(support, out Vector3 center, out float halfRight, out float halfForward);
            Vector3 local = aim - center;
            float u = Vector3.Dot(local, support.transform.right);
            float v = Vector3.Dot(local, support.transform.forward);
            return Mathf.Abs(u) <= halfRight + margin && Mathf.Abs(v) <= halfForward + margin;
        }

        /// <summary>
        /// Clamp the aim into the support's own footprint so the edge cell is always a cell the support covers.
        /// Aim past the slab edge then picks that edge (outward) instead of the empty neighbour cell (inward flip).
        /// </summary>
        static Vector3 ClampAimToSupportCell(DMBuildingGhost support, Vector3 aim)
        {
            GetCatalogFootprint(support, out Vector3 center, out float halfRight, out float halfForward);
            Vector3 right = support.transform.right;
            Vector3 forward = support.transform.forward;
            Vector3 local = aim - center;
            float u = Vector3.Dot(local, right);
            float v = Vector3.Dot(local, forward);
            float inset = 0.01f;
            float cu = Mathf.Clamp(u, -Mathf.Max(0f, halfRight - inset), Mathf.Max(0f, halfRight - inset));
            float cv = Mathf.Clamp(v, -Mathf.Max(0f, halfForward - inset), Mathf.Max(0f, halfForward - inset));
            Vector3 clamped = center + right * cu + forward * cv;
            clamped.y = aim.y;
            return clamped;
        }

        static bool TrySnapEdge(
            Vector3 aimPoint,
            DMBuildingPiece piece,
            float yawDegrees,
            float heightOffset,
            bool lookingUp,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = default;
            rotation = Quaternion.identity;
            if (piece == null)
                return false;

            Vector3 feet = PlayerFeet();
            DMBuildingGhost support = SelectHorizontalSupportForVertical(aimPoint, feet, piece, heightOffset);
            if (support == null)
                return false;

            float top = SurfaceTop(support);
            if (!SupportsSnapStory(top, aimPoint, feet))
                return false;

            int sticky = instance != null ? instance.stickyVerticalSide : -1;
            if (!TrySeatVerticalOnCornerEdge(
                    support,
                    aimPoint,
                    top,
                    piece,
                    heightOffset,
                    sticky,
                    out position,
                    out rotation,
                    out int side))
                return false;

            if (instance != null)
                instance.stickyVerticalSide = side;
            return true;
        }

        static void SeatDoor(DMBuildingGhost frame, float lift, out Vector3 position, out Quaternion rotation)
        {
            rotation = frame.transform.rotation;
            float nudge = DMBuildingGhostProfile.DoorSeatDropMeters - DMBuildingCatalog.DoorSeatDrop;
            Renderer renderer = frame.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;
                position = bounds.center;
                position.y = bounds.min.y + DMBuildingCatalog.DoorHeight * 0.5f - nudge + lift;
                return;
            }

            position = frame.transform.position - frame.transform.up * DMBuildingGhostProfile.DoorSeatDropMeters;
            position += Vector3.up * lift;
        }

        static float SeatY(float groundY, DMBuildingPiece piece, float lift)
        {
            float surface = groundY;
            DMBuildingGhost support = NearestSameStorySupport(PlayerFeet(), DMBuildingGhostProfile.EdgeSnapRangeMeters);
            if (support != null)
            {
                float top = SurfaceTop(support);
                if (groundY < top - 0.35f)
                    surface = top;
            }

            return SeatCenterY(surface, piece, lift);
        }

        static bool TryGroundHit(Ray ray, out RaycastHit chosen)
        {
            chosen = default;
            RaycastHit[] hits = rayHits;
            int hitCount = Physics.RaycastNonAlloc(ray, rayHits, DMBuildingGhostProfile.AimDistanceMeters, DMBuildingLayers.GroundMask, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null)
                    continue;
                if (collider.gameObject.layer == 8)
                    continue;
                if (collider.GetComponentInParent<DMBuildingGhost>() != null)
                    continue;
                if (collider.GetComponentInParent<DMBuildingPlacementController>() != null)
                    continue;
                if (hits[i].distance >= best)
                    continue;
                best = hits[i].distance;
                chosen = hits[i];
                found = true;
            }

            return found;
        }

        static void ResolveAimPoint(Ray ray, bool lookingUp, out Vector3 aim, out bool hasHit)
        {
            hasHit = false;
            aim = PlayerFeet();
            float maxDistance = DMBuildingGhostProfile.AimDistanceMeters;

            if (TryHitBuiltAlongRay(ray, maxDistance, out RaycastHit builtHit))
            {
                aim = builtHit.point;
                hasHit = true;
                return;
            }

            if (TryGroundHit(ray, out RaycastHit groundHit))
            {
                aim = groundHit.point;
                hasHit = true;
                return;
            }

            if (lookingUp)
            {
                aim = ray.GetPoint(Mathf.Min(maxDistance, 12f));
                hasHit = true;
            }
        }

        static bool TryHitBuiltAlongRay(Ray ray, float maxDistance, out RaycastHit chosen)
        {
            chosen = default;
            RaycastHit[] hits = rayHits;
            int hitCount = Physics.RaycastNonAlloc(ray, rayHits, maxDistance, DMBuildingLayers.AimMask, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = hits[i].collider;
                if (collider == null || collider.gameObject.layer == 8)
                    continue;
                DMBuildingGhost ghost = collider.GetComponentInParent<DMBuildingGhost>();
                if (ghost == null || !ghost.Built)
                    continue;
                if (DMBuildingCatalog.IsSurfaceItem(ghost.PieceId))
                    continue; // lights and decorations never anchor structure or other items
                if (hits[i].distance >= best)
                    continue;
                best = hits[i].distance;
                chosen = hits[i];
                found = true;
            }

            return found;
        }

        static Vector3 SnapOnSupportGrid(DMBuildingGhost support, Vector3 aim, float module, DMBuildingPiece piece)
        {
            if (piece != null && UsesLargeModuleGrid(piece))
                return SnapModule(aim, piece);

            if (support == null)
                return SnapModule(aim, piece);

            Vector3 right = support.transform.right;
            Vector3 forward = support.transform.forward;
            Vector3 local = aim - support.transform.position;
            float u = Vector3.Dot(local, right);
            float v = Vector3.Dot(local, forward);
            int iu = Mathf.RoundToInt(u / module);
            int iv = Mathf.RoundToInt(v / module);
            float y = aim.y;
            Vector3 snapped = support.transform.position + right * (iu * module) + forward * (iv * module);
            snapped.y = y;
            return snapped;
        }

        const float LatticeEndBindMeters = 0.2f;
        const float VerticalPeerBindMeters = 0.25f;

        static void SeatHorizontalTile(
            Vector3 aim,
            DMBuildingPiece piece,
            float storyTop,
            float lift,
            float module,
            out Vector3 position)
        {
            float seatY = SeatCenterY(storyTop, piece, lift);
            float search = module * 3f + DMBuildingGhostProfile.EdgeSnapRangeMeters;
            position = SnapToOuterModuleGrid(aim, piece, seatY);
            DMBuildingGhost reference = NearestHorizontalLatticeReference(aim, search);
            if (reference != null)
                position = ModuleCornerLattice.FromSupport(reference).SnapCellCenter(aim, seatY);

            if (DMBuildingCatalog.IsCeiling(piece.Id))
            {
                DMBuildingGhost floorBelow = NearestFloorBelowCeiling(aim, seatY, search);
                if (floorBelow != null)
                    position = ModuleCornerLattice.FromSupport(floorBelow).SnapCellCenter(aim, seatY);
            }
        }

        static DMBuildingGhost NearestFloorBelowCeiling(Vector3 aim, float ceilingSeatY, float searchRadius)
        {
            DMBuildingGhost[] ghosts = BuiltGhosts();
            DMBuildingGhost best = null;
            float bestDistance = searchRadius;
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost == null || !ghost.Built || !DMBuildingCatalog.IsFloor(ghost.PieceId))
                    continue;
                if (ghost.transform.position.y >= ceilingSeatY - 0.05f)
                    continue;

                float distance = FlatDistance(aim, HorizontalPlacementCenter(ghost));
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                best = ghost;
            }

            return best;
        }

        static DMBuildingGhost NearestWallTopRelaxed(Vector3 aim, Vector3 feet)
        {
            float range = DMBuildingGhostProfile.TopSnapRangeMeters * 1.35f;
            DMBuildingGhost[] ghosts = BuiltGhosts();
            DMBuildingGhost best = null;
            float bestScore = range;
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost == null || !ghost.Built || !DMBuildingCatalog.IsVerticalSupport(ghost.PieceId))
                    continue;

                Vector3 center = HorizontalPlacementCenter(ghost);
                float aimDist = FlatDistance(aim, center);
                float playerDist = FlatDistance(feet, center);
                if (aimDist >= range && playerDist >= range)
                    continue;

                float score = Mathf.Min(aimDist, playerDist);
                if (score >= bestScore)
                    continue;
                bestScore = score;
                best = ghost;
            }

            return best;
        }

        static void FinalizeVerticalSeatFromCorners(
            ref Vector3 seat,
            float seatY,
            Vector3 cornerA,
            Vector3 cornerB,
            Vector3 outward,
            float halfThick)
        {
            outward.y = 0f;
            if (outward.sqrMagnitude < 0.0001f)
                return;

            outward.Normalize();
            Vector3 mid = (cornerA + cornerB) * 0.5f;
            // 0925-flush: wall sits inside the footprint; its outer face lands on the grid line.
            seat = mid - outward * halfThick;
            seat.y = seatY;
            float plane = Vector3.Dot(cornerA, outward);
            seat += outward * (plane - halfThick - Vector3.Dot(seat, outward));
        }

        /// <summary>
        /// Fills an upper-story bay using the nearest wall top and support-local module grid.
        /// </summary>
        static bool TrySnapHorizontalBay(
            Vector3 aim,
            DMBuildingPiece piece,
            float lift,
            float module,
            out Vector3 position,
            out int layer)
        {
            position = default;
            layer = -1;
            Vector3 feet = PlayerFeet();
            DMBuildingGhost wall = NearestWallTopRelaxed(aim, feet);
            if (wall == null)
                return false;

            float wallTop = SurfaceTop(wall);
            layer = StackLayer(wallTop, feet.y);
            if (layer < 1)
                layer = 1;
            if (!DMBuildingCatalog.IsStackLayerAllowed(layer))
                return false;

            SeatHorizontalTile(aim, piece, wallTop, lift, module, out position);
            return true;
        }

        static bool TryHitGhost(out DMBuildingGhost ghost)
        {
            ghost = null;
            Camera camera = Camera.main;
            if (camera == null)
                return false;

            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = rayHits;
            int hitCount = Physics.RaycastNonAlloc(ray, rayHits, DMBuildingGhostProfile.AimDistanceMeters, DMBuildingLayers.AimMask, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                if (hits[i].collider == null || hits[i].distance >= best)
                    continue;
                DMBuildingGhost candidate = hits[i].collider.GetComponentInParent<DMBuildingGhost>();
                if (candidate == null)
                    continue;
                best = hits[i].distance;
                ghost = candidate;
            }

            return ghost != null;
        }

        static bool TryHitUnbuilt(out DMBuildingGhost ghost)
        {
            if (!TryHitGhost(out ghost))
                return false;
            if (ghost.Built)
            {
                ghost = null;
                return false;
            }

            return true;
        }

        static int StackLayer(float bottomY, float groundY)
        {
            float story = DMBuildingGhostProfile.LargeModuleMeters;
            return Mathf.RoundToInt((bottomY - groundY) / story);
        }

        /// <summary>
        /// Floor layer 0 sits on a foundation. Upper floors and every ceiling need a built wall top.
        /// Layers are 0 through 3 only (ground plus three stories).
        /// </summary>
        static bool TrySnapFloorOrCeiling(Vector3 aim, DMBuildingPiece piece, float lift, bool lookingUp, out Vector3 position, out int layer)
        {
            position = default;
            layer = -1;
            float module = ModuleFor(piece);
            bool ceiling = DMBuildingCatalog.IsCeiling(piece.Id);
            Vector3 feet = PlayerFeet();
            float story = DMBuildingGhostProfile.LargeModuleMeters;

            DMBuildingGhost foundation = NearestFoundation(aim, module * 2.5f);
            float foundationTop = foundation != null ? SurfaceTop(foundation) : float.NegativeInfinity;

            DMBuildingGhost wall = NearestWallTopRelaxed(aim, feet);
            float wallTop = wall != null ? SurfaceTop(wall) : float.NegativeInfinity;

            bool aimUpperStory = ceiling
                || lookingUp
                || aim.y >= foundationTop + story * 0.3f;

            if (foundation != null && !ceiling && !aimUpperStory && SupportsSnapStory(foundationTop, aim, feet))
            {
                Vector3 flat = aim - HorizontalPlacementCenter(foundation);
                flat.y = 0f;
                if (flat.magnitude <= module * 1.15f
                    || FlatDistance(feet, HorizontalPlacementCenter(foundation)) <= module * 1.15f)
                {
                    layer = 0;
                    SeatHorizontalTile(aim, piece, foundationTop, lift, module, out position);
                    return true;
                }
            }

            if (wall != null && wallTop > foundationTop + story * 0.2f && aimUpperStory)
            {
                int upperLayer = StackLayer(wallTop, feet.y);
                if (upperLayer < 1)
                    upperLayer = 1;

                if (DMBuildingCatalog.IsStackLayerAllowed(upperLayer))
                {
                    SeatHorizontalTile(aim, piece, wallTop, lift, module, out position);
                    layer = upperLayer;
                    return true;
                }
            }

            if (ceiling && TrySnapHorizontalBay(aim, piece, lift, module, out position, out layer))
                return true;

            return false;
        }

        static DMBuildingGhost NearestFoundation(Vector3 aim, float range)
        {
            DMBuildingGhost[] ghosts = BuiltGhosts();
            DMBuildingGhost best = null;
            float bestDistance = range;
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost == null || !ghost.Built || !DMBuildingCatalog.IsFoundation(ghost.PieceId))
                    continue;
                float distance = Vector2.Distance(
                    new Vector2(aim.x, aim.z),
                    new Vector2(HorizontalPlacementCenter(ghost).x, HorizontalPlacementCenter(ghost).z));
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                best = ghost;
            }

            return best;
        }

        static DMBuildingGhost NearestWallTop(Vector3 aim, Vector3 feet)
        {
            float range = DMBuildingGhostProfile.TopSnapRangeMeters;
            DMBuildingGhost[] ghosts = BuiltGhosts();
            DMBuildingGhost best = null;
            float bestScore = range;
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost == null || !ghost.Built || !DMBuildingCatalog.IsVerticalSupport(ghost.PieceId))
                    continue;

                float top = SurfaceTop(ghost);
                float story = DMBuildingGhostProfile.LargeModuleMeters;
                if (top < feet.y + 0.35f && aim.y < top - story * 0.35f)
                    continue;

                float aimDist = FlatDistance(aim, ghost.transform.position);
                float playerDist = FlatDistance(feet, ghost.transform.position);
                if (aimDist >= range && playerDist >= range)
                    continue;

                float score = Mathf.Min(aimDist, playerDist);
                if (score >= bestScore)
                    continue;
                bestScore = score;
                best = ghost;
            }

            return best;
        }

        static DMBuildingGhost NearestSameStorySupport(Vector3 feet, float range)
        {
            DMBuildingGhost[] ghosts = BuiltGhosts();
            DMBuildingGhost best = null;
            float bestDistance = range;
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost == null || !ghost.Built || !DMBuildingCatalog.IsEdgeSupport(ghost.PieceId))
                    continue;
                if (!SameStory(SurfaceTop(ghost), feet.y))
                    continue;

                float distance = FlatDistance(feet, ghost.transform.position);
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                best = ghost;
            }

            return best;
        }

        static bool SameStory(float surfaceTop, float feetY)
        {
            return surfaceTop >= feetY - 1.6f && surfaceTop <= feetY + 1.25f;
        }

        /// <summary>
        /// Snap targets the support's top when the player or crosshair is on that story band.
        /// </summary>
        static bool SupportsSnapStory(float surfaceTop, Vector3 aim, Vector3 feet)
        {
            if (SameStory(surfaceTop, feet.y))
                return true;

            float story = DMBuildingGhostProfile.LargeModuleMeters;
            float band = story * 0.55f;
            if (Mathf.Abs(surfaceTop - aim.y) <= band)
                return true;

            return SameStory(surfaceTop, aim.y);
        }

        static float SeatCenterY(float supportTop, DMBuildingPiece piece, float lift)
        {
            float half = piece != null ? piece.Size.y * 0.5f : 0.2f;
            return supportTop + half + lift;
        }

        static float SurfaceTop(DMBuildingGhost ghost)
        {
            if (ghost == null)
                return 0f;

            Renderer renderer = ghost.GetComponentInChildren<Renderer>();
            if (renderer != null)
                return renderer.bounds.max.y;

            return ghost.transform.position.y + Mathf.Max(0.05f, ghost.LocalHalfExtents.y);
        }

        static Vector3 HalfExtents(DMBuildingGhost ghost)
        {
            if (ghost != null && ghost.LocalHalfExtents.sqrMagnitude > 0.01f)
                return ghost.LocalHalfExtents;

            Renderer renderer = ghost != null ? ghost.GetComponentInChildren<Renderer>() : null;
            if (renderer != null)
                return renderer.bounds.extents;

            return new Vector3(2f, 0.2f, 2f);
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        static Vector3 PlayerFeet()
        {
            if (cachedPlayer == null)
                cachedPlayer = UnityEngine.Object.FindAnyObjectByType<Project.Player.PlayerController>();
            Project.Player.PlayerController player = cachedPlayer;
            if (player != null)
                return player.transform.position;

            Camera camera = Camera.main;
            return camera != null ? camera.transform.position : Vector3.zero;
        }

        static float ModuleFor(DMBuildingPiece piece)
        {
            float span = piece == null ? 4f : Mathf.Max(piece.Size.x, piece.Size.z);
            float large = DMBuildingGhostProfile.LargeModuleMeters;
            float small = DMBuildingGhostProfile.SmallModuleMeters;
            if (span < (large + small) * 0.5f)
                return small;
            return large;
        }

        static bool UsesLargeModuleGrid(DMBuildingPiece piece)
        {
            return ModuleFor(piece) >= DMBuildingGhostProfile.LargeModuleMeters - 0.01f;
        }

        /// <summary>
        /// 4 m pieces share one world lattice (SnapCenter). 2 m pieces only when footprint is below 3 m span.
        /// </summary>
        static Vector3 SnapModule(Vector3 point, DMBuildingPiece piece)
        {
            return SnapToOuterModuleGrid(point, piece, point.y);
        }

        static float SnapCenter(float value, float module)
        {
            float half = module * 0.5f;
            return Mathf.Round((value - half) / module) * module + half;
        }

        static Vector3 PlacementCenter(DMBuildingGhost ghost)
        {
            if (ghost == null)
                return Vector3.zero;

            Renderer renderer = ghost.GetComponentInChildren<Renderer>();
            return renderer != null ? renderer.bounds.center : ghost.transform.position;
        }

        /// <summary>XZ from renderer bounds; Y from root so story height stays stable.</summary>
        static Vector3 HorizontalPlacementCenter(DMBuildingGhost ghost)
        {
            Vector3 center = PlacementCenter(ghost);
            center.y = ghost.transform.position.y;
            return center;
        }

        static DMBuildingGhost NearestOuterGridReference(Vector3 aim, float seatY, DMBuildingPiece piece)
        {
            float module = ModuleFor(piece);
            float search = module * 3f + DMBuildingGhostProfile.EdgeSnapRangeMeters;
            return NearestHorizontalLatticeReference(aim, search);
        }

        static float VerticalPieceHalfThickness(DMBuildingPiece piece)
        {
            if (piece == null)
                return 0.15f;

            return Mathf.Min(piece.Size.x, piece.Size.z) * 0.5f;
        }

        /// <summary>
        /// 4 m snap lattice: outer corner origin + integer steps. Horizontals use cell center (corner + half module).
        /// Verticals use pairs of cell corner vertices per edge.
        /// </summary>
        struct ModuleCornerLattice
        {
            public Vector3 Origin;
            public Vector3 Right;
            public Vector3 Forward;
            public float Module;

            public static ModuleCornerLattice FromSupport(DMBuildingGhost support)
            {
                GetCatalogFootprint(support, out Vector3 center, out float halfRight, out float halfForward);
                return new ModuleCornerLattice
                {
                    Origin = center - support.transform.right * halfRight - support.transform.forward * halfForward,
                    Right = support.transform.right,
                    Forward = support.transform.forward,
                    Module = DMBuildingGhostProfile.LargeModuleMeters,
                };
            }

            public Vector3 Corner(int iu, int iv)
            {
                return Origin + Right * (iu * Module) + Forward * (iv * Module);
            }

            public void SnapCornerIndices(Vector3 aim, out int iu, out int iv)
            {
                Vector3 local = aim - Origin;
                float u = Vector3.Dot(local, Right);
                float v = Vector3.Dot(local, Forward);
                iu = Mathf.RoundToInt(u / Module);
                iv = Mathf.RoundToInt(v / Module);
            }

            public void PickEdgeFromCellCenter(
                Vector3 cellCenter,
                Vector3 aim,
                int stickySide,
                out Vector3 cornerA,
                out Vector3 cornerB,
                out Vector3 outward,
                out Vector3 tangent,
                out int side)
            {
                GetCellCorners(cellCenter, out Vector3 sw, out Vector3 se, out Vector3 ne, out Vector3 nw);
                Vector3 local = aim - cellCenter;
                local.y = 0f;
                float alongRight = Vector3.Dot(local, Right);
                float alongForward = Vector3.Dot(local, Forward);
                float bias = Module * 0.04f;

                side = 0;
                if (Mathf.Abs(alongRight) >= Mathf.Abs(alongForward))
                    side = alongRight >= 0f ? 1 : 3;
                else
                    side = alongForward >= 0f ? 2 : 0;

                if (stickySide >= 0 && Mathf.Abs(Mathf.Abs(alongRight) - Mathf.Abs(alongForward)) <= bias)
                    side = stickySide;

                switch (side)
                {
                    case 0:
                        cornerA = sw;
                        cornerB = se;
                        tangent = Right;
                        outward = -Forward;
                        break;
                    case 1:
                        cornerA = se;
                        cornerB = ne;
                        tangent = Forward;
                        outward = Right;
                        break;
                    case 2:
                        cornerA = ne;
                        cornerB = nw;
                        tangent = -Right;
                        outward = Forward;
                        break;
                    default:
                        cornerA = nw;
                        cornerB = sw;
                        tangent = -Forward;
                        outward = -Right;
                        break;
                }
            }

            public void PickEdgeFromCorner(int iu, int iv, Vector3 aim, out Vector3 cornerA, out Vector3 cornerB, out Vector3 tangent)
            {
                Vector3 anchor = Corner(iu, iv);
                Vector3 local = aim - anchor;
                local.y = 0f;
                float alongRight = Vector3.Dot(local, Right);
                float alongForward = Vector3.Dot(local, Forward);

                if (Mathf.Abs(alongRight) >= Mathf.Abs(alongForward))
                {
                    if (alongRight >= 0f)
                    {
                        tangent = Right;
                        cornerA = anchor;
                        cornerB = Corner(iu + 1, iv);
                    }
                    else
                    {
                        tangent = -Right;
                        cornerA = Corner(iu - 1, iv);
                        cornerB = anchor;
                    }
                }
                else if (alongForward >= 0f)
                {
                    tangent = Forward;
                    cornerA = anchor;
                    cornerB = Corner(iu, iv + 1);
                }
                else
                {
                    tangent = -Forward;
                    cornerA = Corner(iu, iv - 1);
                    cornerB = anchor;
                }
            }

            public Vector3 SnapCellCenter(Vector3 aim, float seatY)
            {
                float half = Module * 0.5f;
                Vector3 local = aim - Origin;
                float u = Vector3.Dot(local, Right);
                float v = Vector3.Dot(local, Forward);
                int iu = Mathf.RoundToInt((u - half) / Module);
                int iv = Mathf.RoundToInt((v - half) / Module);
                Vector3 snapped = Origin + Right * (half + iu * Module) + Forward * (half + iv * Module);
                snapped.y = seatY;
                return snapped;
            }

            public void GetCellCorners(Vector3 cellCenter, out Vector3 sw, out Vector3 se, out Vector3 ne, out Vector3 nw)
            {
                float half = Module * 0.5f;
                sw = cellCenter - Right * half - Forward * half;
                se = cellCenter + Right * half - Forward * half;
                ne = cellCenter + Right * half + Forward * half;
                nw = cellCenter - Right * half + Forward * half;
            }
        }

        static Quaternion RotationForVerticalEdgePiece(Vector3 outward, Vector3 tangent)
        {
            outward.y = 0f;
            tangent.y = 0f;
            if (outward.sqrMagnitude < 0.0001f)
                return Quaternion.identity;

            outward.Normalize();
            tangent.Normalize();
            // LookRotation: local Z+ = outward (thin axis), local X+ = cross(up, outward) when aligned with edge run.
            Quaternion rotation = Quaternion.LookRotation(outward, Vector3.up);
            Vector3 widthAxis = rotation * Vector3.right;
            if (Vector3.Dot(widthAxis, tangent) < 0f)
                rotation *= Quaternion.Euler(0f, 180f, 0f);
            return rotation;
        }

        static Vector3 OuterCornerOrigin(DMBuildingGhost ghost)
        {
            return ModuleCornerLattice.FromSupport(ghost).Origin;
        }

        static Vector3 SnapLargeModuleHorizontalCenter(Vector3 aim, float seatY, DMBuildingGhost reference)
        {
            return ModuleCornerLattice.FromSupport(reference).SnapCellCenter(aim, seatY);
        }

        static void GetCatalogFootprint(DMBuildingGhost ghost, out Vector3 center, out float halfRight, out float halfForward)
        {
            center = ghost.transform.position;
            Vector3 ext = HalfExtents(ghost);
            halfRight = ext.x;
            halfForward = ext.z;
        }

        static bool TrySeatVerticalOnCornerEdge(
            DMBuildingGhost support,
            Vector3 aim,
            float top,
            DMBuildingPiece piece,
            float heightOffset,
            int stickySide,
            out Vector3 seat,
            out Quaternion rotation,
            out int side)
        {
            seat = default;
            rotation = Quaternion.identity;
            side = -1;
            if (support == null || piece == null)
                return false;

            float halfThick = VerticalPieceHalfThickness(piece);
            float seatY = SeatCenterY(top, piece, heightOffset);
            ModuleCornerLattice lattice = ModuleCornerLattice.FromSupport(support);
            Vector3 cellCenter = lattice.SnapCellCenter(ClampAimToSupportCell(support, aim), seatY);
            lattice.PickEdgeFromCellCenter(
                cellCenter,
                aim,
                stickySide,
                out Vector3 cornerA,
                out Vector3 cornerB,
                out Vector3 outward,
                out Vector3 tangent,
                out side);

            FinalizeVerticalSeatFromCorners(ref seat, seatY, cornerA, cornerB, outward, halfThick);
            rotation = RotationForVerticalEdgePiece(outward, tangent);
            return true;
        }

        static bool TrySeatVerticalOnCornerEdge(
            DMBuildingGhost support,
            Vector3 aim,
            float top,
            DMBuildingPiece piece,
            float heightOffset,
            out Vector3 seat,
            out Vector3 outward,
            out Vector3 tangent,
            out Vector3 cornerA,
            out Vector3 cornerB)
        {
            if (!TrySeatVerticalOnCornerEdge(
                    support,
                    aim,
                    top,
                    piece,
                    heightOffset,
                    -1,
                    out seat,
                    out Quaternion rotation,
                    out _))
            {
                outward = Vector3.forward;
                tangent = Vector3.right;
                cornerA = default;
                cornerB = default;
                return false;
            }

            outward = rotation * Vector3.forward;
            tangent = rotation * Vector3.right;
            ModuleCornerLattice lattice = ModuleCornerLattice.FromSupport(support);
            float seatY = SeatCenterY(top, piece, heightOffset);
            Vector3 cellCenter = lattice.SnapCellCenter(ClampAimToSupportCell(support, aim), seatY);
            lattice.GetCellCorners(cellCenter, out Vector3 sw, out Vector3 se, out Vector3 ne, out Vector3 nw);
            lattice.PickEdgeFromCellCenter(cellCenter, aim, -1, out cornerA, out cornerB, out _, out _, out _);
            return true;
        }

        static Vector3 VerticalRunEndBottomOuter(Vector3 seat, Quaternion rotation, DMBuildingPiece piece, bool maxTangentEnd)
        {
            float halfW = piece.Size.x * 0.5f;
            float halfH = piece.Size.y * 0.5f;
            float halfT = VerticalPieceHalfThickness(piece);
            float x = maxTangentEnd ? halfW : -halfW;
            return seat + rotation * new Vector3(x, -halfH, halfT);
        }

        static void SnapVerticalRunEndsToLattice(ref Vector3 seat, Quaternion rotation, DMBuildingPiece piece, Vector3 cornerA, Vector3 cornerB)
        {
            if (piece == null)
                return;

            float bind = LatticeEndBindMeters;
            Vector3 endMin = VerticalRunEndBottomOuter(seat, rotation, piece, false);
            Vector3 endMax = VerticalRunEndBottomOuter(seat, rotation, piece, true);

            Vector3 targetA = cornerA;
            targetA.y = endMin.y;
            Vector3 targetB = cornerB;
            targetB.y = endMin.y;

            if (FlatDistance(endMin, targetA) <= bind)
                seat += targetA - endMin;
            else if (FlatDistance(endMin, targetB) <= bind)
                seat += targetB - endMin;

            endMax = VerticalRunEndBottomOuter(seat, rotation, piece, true);
            endMin = VerticalRunEndBottomOuter(seat, rotation, piece, false);
            targetA.y = endMax.y;
            targetB.y = endMax.y;

            if (FlatDistance(endMax, targetB) <= bind)
                seat += targetB - endMax;
            else if (FlatDistance(endMax, targetA) <= bind)
                seat += targetA - endMax;
        }

        static Vector3 VerticalRunEndBottomOuterFromGhost(DMBuildingGhost ghost, bool maxTangentEnd)
        {
            Vector3 ext = HalfExtents(ghost);
            float x = maxTangentEnd ? ext.x : -ext.x;
            return ghost.transform.position + ghost.transform.rotation * new Vector3(x, -ext.y, ext.z);
        }

        static void LockVerticalSeatToBuiltPeers(ref Vector3 seat, Quaternion rotation, DMBuildingPiece piece)
        {
            if (piece == null)
                return;

            float bind = VerticalPeerBindMeters;
            Vector3 endMin = VerticalRunEndBottomOuter(seat, rotation, piece, false);
            Vector3 endMax = VerticalRunEndBottomOuter(seat, rotation, piece, true);

            DMBuildingGhost[] ghosts = BuiltGhosts();
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost peer = ghosts[i];
                if (peer == null || !peer.Built || !DMBuildingCatalog.IsVerticalSupport(peer.PieceId))
                    continue;

                Vector3 peerMin = VerticalRunEndBottomOuterFromGhost(peer, false);
                Vector3 peerMax = VerticalRunEndBottomOuterFromGhost(peer, true);

                if (FlatDistance(endMin, peerMin) <= bind)
                    seat += peerMin - endMin;
                else if (FlatDistance(endMin, peerMax) <= bind)
                    seat += peerMax - endMin;

                endMax = VerticalRunEndBottomOuter(seat, rotation, piece, true);
                endMin = VerticalRunEndBottomOuter(seat, rotation, piece, false);

                if (FlatDistance(endMax, peerMax) <= bind)
                    seat += peerMax - endMax;
                else if (FlatDistance(endMax, peerMin) <= bind)
                    seat += peerMin - endMax;
            }
        }

        static Vector3 SnapToOuterModuleGrid(Vector3 aim, DMBuildingPiece piece, float seatY)
        {
            float module = ModuleFor(piece);
            float half = module * 0.5f;

            if (UsesLargeModuleGrid(piece))
            {
                DMBuildingGhost reference = NearestOuterGridReference(aim, seatY, piece);
                if (reference != null)
                    return SnapLargeModuleHorizontalCenter(aim, seatY, reference);

                return new Vector3(
                    Mathf.Round((aim.x - half) / module) * module + half,
                    seatY,
                    Mathf.Round((aim.z - half) / module) * module + half);
            }

            return new Vector3(SnapCenter(aim.x, module), seatY, SnapCenter(aim.z, module));
        }

        static bool TrySnapModuleToNeighbor(Vector3 aim, DMBuildingPiece piece, float lift, out Vector3 position)
        {
            position = default;
            float module = ModuleFor(piece);
            DMBuildingGhost support = NearestSupport(aim, module + DMBuildingGhostProfile.EdgeSnapRangeMeters);
            if (support == null)
                return false;

            Vector3 flat = aim - HorizontalPlacementCenter(support);
            flat.y = 0f;
            float distance = flat.magnitude;

            if (UsesLargeModuleGrid(piece))
            {
                float seatY = HorizontalSeatCenterY(support, lift);
                position = SnapToOuterModuleGrid(aim, piece, seatY);
                if (DMBuildingCatalog.IsFoundation(piece.Id))
                {
                    float cell = FlatDistance(position, HorizontalPlacementCenter(support));
                    return cell >= module * 0.55f;
                }

                if (distance < module * 0.45f)
                {
                    position = SnapToOuterModuleGrid(HorizontalPlacementCenter(support), piece, SeatCenterY(SurfaceTop(support), piece, lift));
                    return true;
                }

                return true;
            }

            if (distance < module * 0.45f && !DMBuildingCatalog.IsFoundation(piece.Id))
            {
                position = support.transform.position;
                position.y = SeatCenterY(SurfaceTop(support), piece, lift);
                return true;
            }

            Vector3 right = support.transform.right;
            Vector3 forward = support.transform.forward;
            Vector3 supportCenter = HorizontalPlacementCenter(support);
            Vector3[] neighbors =
            {
                supportCenter + right * module,
                supportCenter - right * module,
                supportCenter + forward * module,
                supportCenter - forward * module,
            };

            float bestAim = float.MaxValue;
            bool found = false;
            for (int i = 0; i < neighbors.Length; i++)
            {
                Vector3 candidate = neighbors[i];
                candidate.y = HorizontalSeatCenterY(support, lift);
                float aimDist = FlatDistance(aim, candidate);
                if (aimDist >= bestAim)
                    continue;
                bestAim = aimDist;
                position = candidate;
                found = true;
            }

            return found;
        }

        /// <summary>Same-story modules share pivot height; only floors/ceilings stack on SurfaceTop.</summary>
        static float HorizontalSeatCenterY(DMBuildingGhost support, float lift)
        {
            return support.transform.position.y + lift;
        }

        static DMBuildingGhost NearestSupport(Vector3 aim, float range)
        {
            DMBuildingGhost[] ghosts = BuiltGhosts();
            DMBuildingGhost best = null;
            float bestDistance = range;
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost == null || !ghost.Built || !DMBuildingCatalog.IsEdgeSupport(ghost.PieceId))
                    continue;
                float distance = Vector2.Distance(
                    new Vector2(aim.x, aim.z),
                    new Vector2(HorizontalPlacementCenter(ghost).x, HorizontalPlacementCenter(ghost).z));
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                best = ghost;
            }

            return best;
        }

        static bool Overlaps(Vector3 center, Vector3 size, Quaternion rotation, string pieceId)
        {
            Vector3 half = size * 0.5f;
            float pad = DMBuildingGhostProfile.OverlapPaddingMeters;
            half.x = Mathf.Max(0.05f, half.x - pad);
            half.y = Mathf.Max(0.05f, half.y - pad);
            half.z = Mathf.Max(0.05f, half.z - pad);
            Collider[] hits = overlapHits;
            int hitCount = Physics.OverlapBoxNonAlloc(center, half, overlapHits, rotation, DMBuildingLayers.OverlapMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                Collider collider = hits[i];
                if (collider == null || collider is TerrainCollider)
                    continue;
                if (collider.gameObject.layer == 8)
                    continue;
                if (collider.GetComponentInParent<DMBuildingPlacementController>() != null)
                    continue;

                DMBuildingGhost ghost = collider.GetComponentInParent<DMBuildingGhost>();
                if (ghost != null)
                {
                    if (DMBuildingCatalog.SameShape(ghost.PieceId, pieceId) && OccupiesSameModuleCell(center, ghost, size))
                        return true;
                    continue;
                }

                return true;
            }

            return false;
        }

        static bool IsModuleGridDistance(float distance, float module)
        {
            if (module <= 0.01f)
                return false;
            if (distance <= module * 0.12f)
                return true;

            float steps = distance / module;
            int rounded = Mathf.RoundToInt(steps);
            return rounded >= 1 && Mathf.Abs(steps - rounded) <= 0.12f;
        }

        static bool OccupiesSameModuleCell(Vector3 center, DMBuildingGhost existing, Vector3 size)
        {
            if (existing == null)
                return false;

            float module = Mathf.Max(size.x, size.z);
            return FlatDistance(center, HorizontalPlacementCenter(existing)) < module * 0.42f;
        }

        static DMBuildingGhost FindDoorFrame(Vector3 near)
        {
            Vector3 feet = PlayerFeet();
            DMBuildingGhost[] ghosts = BuiltGhosts();
            DMBuildingGhost best = null;
            float bestDistance = DMBuildingGhostProfile.DoorFrameRangeMeters;
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost frame = ghosts[i];
                if (frame == null || !frame.Built || !DMBuildingCatalog.IsDoorFrame(frame.PieceId))
                    continue;

                Renderer renderer = frame.GetComponentInChildren<Renderer>();
                float sill = renderer != null ? renderer.bounds.min.y : frame.transform.position.y - DMBuildingCatalog.FrameOuter * 0.5f;
                if (Mathf.Abs(sill - feet.y) > 1.75f)
                    continue;

                float distance = FlatDistance(near, frame.transform.position);
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                best = frame;
            }

            return best;
        }

        static DMBuildingGhost Commit(DMBuildingPiece piece, Vector3 position, Quaternion rotation, ItemData paid)
        {
            GameObject ghostObject = DMBuildingPieceFactory.Create(piece.Id);
            ghostObject.name = "BuildingGhost_" + piece.Id;
            ghostObject.transform.SetPositionAndRotation(position, rotation);
            ghostObject.transform.localScale = Vector3.one;
            DMBuildingGhost marker = ghostObject.GetComponent<DMBuildingGhost>();
            if (marker == null)
                marker = ghostObject.AddComponent<DMBuildingGhost>();
            marker.PieceId = piece.Id;
            marker.Cost = piece.Cost;
            marker.PaidItem = paid;
            marker.Built = true;
            marker.LocalHalfExtents = piece.Size * 0.5f;
            bool isDoor = DMBuildingCatalog.IsDoor(piece.Id);
            DMBuildingStyleLibrary style = DMBuildingCatalog.StyleOf(piece.Id);
            DMBuildingMaterialVariant variant = !isDoor && style != null ? style.FirstFinish() : null;
            marker.MaterialVariantId = variant != null ? variant.id : null;

            if (isDoor && ghostObject.GetComponent<DMBuildingDoor>() == null)
                ghostObject.AddComponent<DMBuildingDoor>();

            if (piece.Snap == DMBuildingSnap.Surface)
            {
                marker.Host = surfaceHost;
                DMBuildingPieceFactory.SetTag(ghostObject, "Untagged"); // small items are not climb holds
            }

            ApplyBuiltMaterial(marker);
            InvalidateBuiltGhostCache();
            DMBuildingLayers.EnsureWorldMasks();
            return marker;
        }

        /// <summary>
        /// M cycles the Stone finish on the built piece under the crosshair, or the nearest built piece.
        /// </summary>
        static void TryApplyNextMaterial()
        {
            if (!TryResolveBuiltTarget(out DMBuildingGhost ghost) || DMBuildingCatalog.IsDoor(ghost.PieceId))
                return;
            if (KeepsPrefabMaterials(ghost.PieceId))
                return;

            DMBuildingStyleLibrary style = DMBuildingCatalog.StyleOf(ghost.PieceId);
            DMBuildingMaterialVariant next = style != null ? style.NextFinish(ghost.MaterialVariantId) : null;
            if (next == null)
                return;

            ghost.MaterialVariantId = next.id;
            ApplyBuiltMaterial(ghost);
        }

        static void ApplyBuiltMaterial(DMBuildingGhost ghost)
        {
            if (ghost == null)
                return;

            // Library: custom and surface prefabs can keep their own materials.
            if (KeepsPrefabMaterials(ghost.PieceId))
                return;

            DMBuildingStyleLibrary style = DMBuildingCatalog.StyleOf(ghost.PieceId);
            Material finished = null;
            if (DMBuildingCatalog.IsDoor(ghost.PieceId))
                finished = style != null ? style.doorMaterial : null;
            else
            {
                DMBuildingMaterialVariant variant = style != null ? style.FindFinish(ghost.MaterialVariantId) : null;
                if (variant == null)
                    variant = DMBuildingStyles.FindFinish(ghost.MaterialVariantId);
                finished = variant != null ? variant.finishedMaterial : null;
            }

            if (finished != null)
                ApplySharedMaterial(ghost.gameObject, finished);
            else
                ApplyTint(ghost.gameObject, SolidMaterial(), keepGlass: false);

            PaintPanes(ghost.gameObject, style != null && style.glassMaterial != null ? style.glassMaterial : GlassMaterial());
        }

        static bool KeepsPrefabMaterials(string pieceId)
        {
            DMBuildingPiece piece = DMBuildingCatalog.Find(pieceId);
            return piece != null && piece.Prefab != null && !piece.ApplyStyleFinish;
        }

        static void ApplySharedMaterial(GameObject target, Material material)
        {
            if (target == null || material == null)
                return;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].gameObject.name != "GlassPane")
                    renderers[i].sharedMaterial = material;
            }
        }

        static bool WasMaterialKeyPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.mKey.wasPressedThisFrame;
        }

        static bool TryResolveDestroyTarget(out DMBuildingGhost ghost)
        {
            if (TryHitGhost(out ghost))
                return true;

            ghost = NearestGhost(null, -1);
            return ghost != null;
        }

        static bool TryResolveBuiltTarget(out DMBuildingGhost ghost)
        {
            if (TryHitGhost(out ghost) && ghost.Built)
                return true;

            ghost = NearestGhost(null, 1);
            return ghost != null;
        }

        static DMBuildingGhost NearestGhost(string pieceId, int builtFilter)
        {
            Camera camera = Camera.main;
            if (camera == null)
                return null;

            Vector3 origin = camera.transform.position;
            float range = DMBuildingGhostProfile.AimDistanceMeters;
            DMBuildingGhost[] ghosts = BuiltGhosts();
            DMBuildingGhost best = null;
            float bestDistance = range;
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost candidate = ghosts[i];
                if (candidate == null)
                    continue;
                if (builtFilter == 0 && candidate.Built)
                    continue;
                if (builtFilter == 1 && !candidate.Built)
                    continue;
                if (!string.IsNullOrEmpty(pieceId) && candidate.PieceId != pieceId)
                    continue;

                float distance = Vector3.Distance(origin, candidate.transform.position);
                if (distance >= bestDistance)
                    continue;
                bestDistance = distance;
                best = candidate;
            }

            return best;
        }

        public static void DiscardUnbuiltGhosts()
        {
            DMBuildingGhost[] ghosts = BuiltGhosts();
            for (int i = 0; i < ghosts.Length; i++)
            {
                if (ghosts[i] != null && !ghosts[i].Built)
                    RefundAndDestroy(ghosts[i]);
            }

            if (instance == null)
                return;

            instance.buildHold = 0f;
            DMUiToolkitBuildingHotbar.SetHoldRing(false, 0f, Vector3.zero);
        }

        static void RefundAndDestroy(DMBuildingGhost ghost)
        {
            if (ghost == null)
                return;
            DMBuildingCatalog.Refund(ghost.PaidItem, ghost.Cost, ghost.PieceId);

            // Library: lights and decorations stuck to this piece come off with it.
            DMBuildingGhost[] built = BuiltGhosts();
            for (int i = 0; i < built.Length; i++)
            {
                DMBuildingGhost attached = built[i];
                if (attached == null || attached == ghost || attached.Host != ghost)
                    continue;
                DMBuildingCatalog.Refund(attached.PaidItem, attached.Cost, attached.PieceId);
                Destroy(attached.gameObject);
            }

            InvalidateBuiltGhostCache();
            Destroy(ghost.gameObject);
        }

        static void ApplyTint(GameObject target, Material material, bool keepGlass)
        {
            if (target == null || material == null)
                return;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (keepGlass && renderers[i].gameObject.name == "GlassPane")
                {
                    renderers[i].sharedMaterial = GhostGlassMaterial();
                    continue;
                }

                renderers[i].sharedMaterial = material;
            }
        }

        void ConsumeWheel()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            float raw = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(raw) < 0.01f)
                return;

            int notches = Mathf.RoundToInt(raw / 120f);
            if (notches == 0)
                notches = raw > 0f ? 1 : -1;

            bool alt = Keyboard.current != null
                && (Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed);
            if (alt)
            {
                int turns = Mathf.Max(1, Mathf.RoundToInt(360f / YawStep()));
                yawNotches = ((yawNotches + notches) % turns + turns) % turns;
                return;
            }

            bool shift = Keyboard.current != null
                && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            if (!shift)
                return;

            float limit = DMBuildingGhostProfile.MaxHeightOffsetMeters;
            heightOffset = Mathf.Clamp(heightOffset + notches * DMBuildingGhostProfile.HeightStepMeters, -limit, limit);
        }

        static Quaternion GridLockedRotation(Vector3 aim, int notches)
        {
            float gridYaw = 0f;
            DMBuildingGhost reference = NearestHorizontalLatticeReference(
                aim,
                DMBuildingGhostProfile.LargeModuleMeters * 3f + DMBuildingGhostProfile.EdgeSnapRangeMeters);
            if (reference != null)
                gridYaw = Mathf.Repeat(reference.transform.eulerAngles.y, 90f);
            return Quaternion.Euler(0f, gridYaw + GridQuarterTurns(notches) * 90f, 0f);
        }

        /// <summary>Grid pieces turn at least 90 degrees per notch so squares never sit diagonal on the 4 m grid.</summary>
        static int GridQuarterTurns(int notches)
        {
            float step = Mathf.Max(90f, YawStep());
            return Mathf.RoundToInt(notches * step / 90f);
        }

        static bool EdgeFlipped(int notches)
        {
            return (GridQuarterTurns(notches) & 1) != 0;
        }

        static float YawStep()
        {
            float step = DMBuildingGhostProfile.YawStepDegrees;
            if (step < 1f)
                return 90f;

            float turns = 360f / step;
            if (Mathf.Abs(turns - Mathf.Round(turns)) > 0.01f)
                return 90f;

            return step;
        }

        void DestroyPreview()
        {
            if (preview == null)
                return;

            if (Application.isPlaying)
                Destroy(preview);
            else
                DestroyImmediate(preview);
            preview = null;
            previewId = null;
        }

        void EnsurePreview(string pieceId)
        {
            if (preview != null && previewId == pieceId)
                return;

            if (preview != null)
                Destroy(preview);

            preview = DMBuildingPieceFactory.Create(pieceId);
            DMBuildingPieceFactory.SetTag(preview, "Untagged"); // the ghost itself is never climbable
            DMBuildingLayers.SetLayer(preview, DMBuildingLayers.IgnoreRaycastLayer); // 0925-layers: never in any build mask
            previewId = pieceId;
            previewTintMaterial = null;
            preview.name = "BuildingPreview";
            preview.hideFlags = HideFlags.HideAndDontSave;
            preview.transform.SetParent(transform, false);
            Collider[] colliders = preview.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
        }

        static void ApplyProfileColors()
        {
            Material validMat = ResolvePreviewMaterial(
                ref ghostMaterial,
                ref validGhostMaterialSourceId,
                DMBuildingGhostProfile.ValidGhostMaterialTemplate,
                "DM_BuildGhost");
            Material blockedMat = ResolvePreviewMaterial(
                ref blockedMaterial,
                ref blockedGhostMaterialSourceId,
                DMBuildingGhostProfile.BlockedGhostMaterialTemplate,
                "DM_BuildGhost_Blocked");

            Paint(validMat, DMBuildingGhostProfile.ResolveGhostColor());
            Paint(SolidMaterial(), DMBuildingGhostProfile.ResolveFinishedColor());
            Paint(GlassMaterial(), DMBuildingGhostProfile.ResolveGlassColor());
            Color ghostGlass = DMBuildingGhostProfile.ResolveGlassColor();
            ghostGlass.a *= 0.55f;
            Paint(GhostGlassMaterial(), ghostGlass);
            Paint(blockedMat, DMBuildingGhostProfile.ResolveBlockedGhostColor());
        }

        static Material ResolvePreviewMaterial(
            ref Material runtime,
            ref int cachedSourceId,
            Material template,
            string fallbackName)
        {
            int sourceId = template != null ? template.GetEntityId().GetHashCode() : 0;
            if (runtime == null || cachedSourceId != sourceId)
            {
                if (runtime != null)
                {
                    if (Application.isPlaying)
                        Object.Destroy(runtime);
                    else
                        Object.DestroyImmediate(runtime);
                }

                cachedSourceId = sourceId;
                runtime = template != null
                    ? new Material(template)
                    {
                        name = fallbackName + "_Inst",
                        hideFlags = HideFlags.HideAndDontSave,
                    }
                    : CreateMaterial(fallbackName);
                // 0925-perf: instanced HDRP ghosts drew flat with undefined matrices and spammed the console.
                if (runtime != null)
                    runtime.enableInstancing = false;
                if (instance != null)
                    instance.previewTintMaterial = null;
            }

            return runtime;
        }

        static Material GhostMaterial()
        {
            if (ghostMaterial == null)
                ghostMaterial = CreateMaterial("DM_BuildGhost");
            return ghostMaterial;
        }

        /// <summary>
        /// Built pieces whose style has no finish. 0926-tint: an opaque lit copy of the profile's Built material
        /// (tinted by Finished mesh), or a plain HDRP/Lit surface, instead of a flat transparent unlit colour.
        /// </summary>
        static Material SolidMaterial()
        {
            Material template = DMBuildingGhostProfile.BuiltMaterialTemplate;
            int sourceId = template != null ? template.GetEntityId().GetHashCode() : 0;
            if (solidMaterial != null && solidMaterialSourceId == sourceId)
                return solidMaterial;

            Material previous = solidMaterial;
            solidMaterialSourceId = sourceId;
            solidMaterial = template != null
                ? new Material(template)
                {
                    name = "DM_BuiltTint_Inst",
                    hideFlags = HideFlags.HideAndDontSave,
                }
                : CreateLitMaterial("DM_BuiltTint");
            Paint(solidMaterial, DMBuildingGhostProfile.ResolveFinishedColor());

            if (previous != null)
            {
                SwapBuiltMaterial(previous, solidMaterial);
                if (Application.isPlaying)
                    Object.Destroy(previous);
                else
                    Object.DestroyImmediate(previous);
            }

            return solidMaterial;
        }

        static Material CreateLitMaterial(string materialName)
        {
            Shader shader = Shader.Find("HDRP/Lit");
            if (shader == null)
                return CreateMaterial(materialName);

            var material = new Material(shader)
            {
                name = materialName,
                hideFlags = HideFlags.HideAndDontSave,
                enableInstancing = false,
            };
            HDMaterial.ValidateMaterial(material);
            return material;
        }

        /// <summary>Placed pieces still using the old built material move to the new one (runs only when the template changes).</summary>
        static void SwapBuiltMaterial(Material from, Material to)
        {
            if (from == null || to == null)
                return;

            DMBuildingGhost[] ghosts = BuiltGhosts();
            for (int g = 0; g < ghosts.Length; g++)
            {
                if (ghosts[g] == null)
                    continue;
                Renderer[] renderers = ghosts[g].GetComponentsInChildren<Renderer>(true);
                for (int r = 0; r < renderers.Length; r++)
                {
                    Material[] shared = renderers[r].sharedMaterials;
                    bool changed = false;
                    for (int m = 0; m < shared.Length; m++)
                    {
                        if (shared[m] == from)
                        {
                            shared[m] = to;
                            changed = true;
                        }
                    }

                    if (changed)
                        renderers[r].sharedMaterials = shared;
                }
            }
        }

        static Material BlockedMaterial()
        {
            if (blockedMaterial == null)
                blockedMaterial = CreateMaterial("DM_BuildGhost_Blocked");
            return blockedMaterial;
        }

        static Material GlassMaterial()
        {
            if (glassMaterial == null)
                glassMaterial = CreateMaterial("DM_BuildGlass");
            return glassMaterial;
        }

        static Material GhostGlassMaterial()
        {
            if (ghostGlassMaterial == null)
                ghostGlassMaterial = CreateMaterial("DM_BuildGhostGlass");
            return ghostGlassMaterial;
        }

        static void PaintPanes(GameObject target, Material material)
        {
            if (target == null || material == null)
                return;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].gameObject.name == "GlassPane")
                    renderers[i].sharedMaterial = material;
            }
        }

        static Material CreateMaterial(string materialName)
        {
            Shader shader = Shader.Find("HDRP/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Sprites/Default");
            if (shader == null)
                return null;

            var material = new Material(shader)
            {
                name = materialName,
                hideFlags = HideFlags.HideAndDontSave,
                enableInstancing = false,
            };

            if (shader.name.StartsWith("HDRP/", System.StringComparison.Ordinal))
            {
                HDMaterial.SetSurfaceType(material, true);
                if (material.HasProperty("_BlendMode"))
                    material.SetFloat("_BlendMode", 0f);
                material.renderQueue = 3000;
            }

            return material;
        }

        static void Paint(Material material, Color color)
        {
            if (material == null)
                return;

            material.color = color;
            if (material.HasProperty("_UnlitColor"))
                material.SetColor("_UnlitColor", color);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }
    }
}

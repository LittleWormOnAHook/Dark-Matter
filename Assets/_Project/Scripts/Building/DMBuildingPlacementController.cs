using Project.Data;
using Project.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;

namespace Project.Building
{
    /// <summary>
    /// Green seat places a ghost on left click. Hold left click for 2 seconds to finish it.
    /// Right click destroys a placed ghost and refunds the stone.
    /// </summary>
    public sealed class DMBuildingPlacementController : MonoBehaviour
    {
        static DMBuildingPlacementController instance;
        static Material ghostMaterial;
        static Material solidMaterial;
        static Material blockedMaterial;
        static Material glassMaterial;
        static Material ghostGlassMaterial;

        GameObject preview;
        string previewId;
        float buildHold;
        float destroyHold;
        DMBuildingGhost buildTarget;
        DMBuildingGhost lastPlaced;
        DMBuildingGhost destroyFocus;
        float yawDegrees;
        float heightOffset;

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

        void OnDisable()
        {
            DestroyPreview();
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

            if (!DMBuildingMode.IsActive)
            {
                buildHold = 0f;
                destroyHold = 0f;
                buildTarget = null;
                destroyFocus = null;
                lastPlaced = null;
                yawDegrees = 0f;
                heightOffset = 0f;
                if (preview != null)
                    preview.SetActive(false);
                DMUiToolkitBuildingHotbar.SetHoldRing(false, 0f, Vector3.zero);
                return;
            }

            ConsumeWheel();
            ApplyProfileColors();

            Mouse mouse = Mouse.current;
            bool overBar = DMUiToolkitBuildingHotbar.PointerOverBar();
            bool leftPressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
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

            bool placedThisFrame = false;
            DMBuildingPiece aimedPiece = null;
            Vector3 aimedPosition = default;
            Quaternion aimedRotation = Quaternion.identity;
            bool canCommit = false;
            if (leftPressed && !overBar
                && TryAim(out aimedPiece, out aimedPosition, out aimedRotation, out canCommit)
                && canCommit
                && DMBuildingCatalog.TrySpendStone(aimedPiece.StoneCost, out ItemData paid))
            {
                lastPlaced = Commit(aimedPiece, aimedPosition, aimedRotation, paid);
                placedThisFrame = true;
                buildHold = 0f;
                buildTarget = null;
            }

            // Hold-to-build does not require the crosshair on the piece.
            // Prefer the unbuilt ghost just placed. Otherwise the nearest unbuilt ghost
            // of the selected piece within aim distance, then any nearest unbuilt ghost.
            if (!placedThisFrame && leftHeld && !overBar && TryResolveBuildTarget(out DMBuildingGhost ghost))
            {
                if (buildTarget != ghost)
                {
                    buildTarget = ghost;
                    buildHold = 0f;
                }

                buildHold += Time.deltaTime;
                DMUiToolkitBuildingHotbar.SetHoldRing(true, buildHold / DMBuildingGhostProfile.BuildSeconds, Vector3.zero);
                if (buildHold >= DMBuildingGhostProfile.BuildSeconds)
                {
                    Finish(ghost);
                    if (lastPlaced == ghost)
                        lastPlaced = null;
                    buildHold = 0f;
                    buildTarget = null;
                    DMUiToolkitBuildingHotbar.SetHoldRing(false, 0f, Vector3.zero);
                }
            }
            else if (!leftHeld)
            {
                buildHold = 0f;
                buildTarget = null;
                DMUiToolkitBuildingHotbar.SetHoldRing(false, 0f, Vector3.zero);
            }

            if (!TryAim(out aimedPiece, out aimedPosition, out aimedRotation, out canCommit))
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
            ApplyTint(preview, canCommit ? GhostMaterial() : BlockedMaterial(), keepGlass: true);
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
            bool hasHit = TryGroundHit(ray, out RaycastHit hit);
            bool lookingUp = ray.direction.y > 0.18f || AimIsAboveWallMid(ray);
            if (!hasHit && !lookingUp)
                return false;

            Vector3 feet = PlayerFeet();
            Vector3 aim = hasHit ? hit.point : feet;
            float yaw = instance.yawDegrees;
            float lift = Mathf.Clamp(instance.heightOffset, -DMBuildingGhostProfile.MaxHeightOffsetMeters, DMBuildingGhostProfile.MaxHeightOffsetMeters);
            rotation = Quaternion.Euler(0f, yaw, 0f);

            if (lookingUp
                && piece.Snap != DMBuildingSnap.Door
                && TrySeatOnNearestTop(aim, piece, yaw, lift, out position, out rotation))
            {
                int layer = StackLayer(position.y - piece.Size.y * 0.5f, feet.y);
                canCommit = DMBuildingCatalog.IsStackLayerAllowed(layer)
                    && DMBuildingCatalog.HasStone(piece.StoneCost)
                    && !Overlaps(position, piece.Size, rotation, piece.Id);
                return true;
            }

            if (DMBuildingCatalog.IsFloor(piece.Id) || DMBuildingCatalog.IsCeiling(piece.Id))
            {
                if (TrySnapFloorOrCeiling(aim, piece, lift, lookingUp, out position, out int layer))
                {
                    canCommit = DMBuildingCatalog.IsStackLayerAllowed(layer)
                        && DMBuildingCatalog.HasStone(piece.StoneCost)
                        && !Overlaps(position, piece.Size, rotation, piece.Id);
                    return true;
                }

                position = SnapModule(aim, piece);
                position.y = SeatY(aim.y, piece, lift);
                return true;
            }

            if (piece.Snap == DMBuildingSnap.Door)
            {
                DMBuildingGhost frame = FindDoorFrame(aim);
                if (frame == null)
                {
                    position = SnapModule(aim, piece);
                    position.y = SeatY(aim.y, piece, lift);
                    return true;
                }

                SeatDoor(frame, lift, out position, out rotation);
                canCommit = DMBuildingCatalog.HasStone(piece.StoneCost);
                return true;
            }

            if (piece.Snap == DMBuildingSnap.Edge
                && TrySnapEdge(aim, piece, yaw, lift, lookingUp, out position, out rotation))
            {
                canCommit = DMBuildingCatalog.HasStone(piece.StoneCost) && !Overlaps(position, piece.Size, rotation, piece.Id);
                return true;
            }

            if (piece.Snap == DMBuildingSnap.Edge)
            {
                position = SnapModule(aim, piece);
                position.y = SeatY(aim.y, piece, lift);
                return true;
            }

            if (TrySnapModuleToNeighbor(aim, piece, lift, out position))
            {
                canCommit = DMBuildingCatalog.HasStone(piece.StoneCost) && !Overlaps(position, piece.Size, rotation, piece.Id);
                return true;
            }

            position = SnapModule(aim, piece);
            position.y = SeatY(aim.y, piece, lift);
            bool clear = !Overlaps(position, piece.Size, rotation, piece.Id);
            canCommit = clear && DMBuildingCatalog.HasStone(piece.StoneCost);
            return true;
        }

        static bool AimIsAboveWallMid(Ray ray)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, DMBuildingGhostProfile.AimDistanceMeters, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
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
                position = wall.transform.position;
                position.y = top + piece.Size.y * 0.5f + lift;
                rotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.LookRotation(wall.transform.forward, Vector3.up);
                return true;
            }

            float module = ModuleFor(piece);
            Vector3 anchor = FlatDistance(aim, feet) > DMBuildingGhostProfile.TopSnapRangeMeters ? feet : aim;
            position = new Vector3(
                SnapCenter(anchor.x, module),
                top + piece.Size.y * 0.5f + lift,
                SnapCenter(anchor.z, module));
            return true;
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
            DMBuildingGhost[] ghosts = FindObjectsByType<DMBuildingGhost>(FindObjectsInactive.Exclude);
            Vector3 feet = PlayerFeet();
            float edgeRange = DMBuildingGhostProfile.EdgeSnapRangeMeters;
            float topRange = DMBuildingGhostProfile.TopSnapRangeMeters;
            float bestScore = float.MaxValue;
            bool found = false;
            Vector3 bestEdge = default;
            Vector3 bestOutward = Vector3.forward;

            bool stackOnWall = false;
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost support = ghosts[i];
                if (support == null)
                    continue;

                float top = SurfaceTop(support);
                if (lookingUp && support.Built && DMBuildingCatalog.IsVerticalSupport(support.PieceId))
                {
                    float aimDist = FlatDistance(aimPoint, support.transform.position);
                    float playerDist = FlatDistance(feet, support.transform.position);
                    if (aimDist > topRange && playerDist > topRange)
                        continue;
                    if (top < feet.y + 1.25f)
                        continue;

                    float score = aimDist;
                    if (score >= bestScore)
                        continue;

                    bestScore = score;
                    bestEdge = support.transform.position;
                    bestEdge.y = top;
                    bestOutward = support.transform.forward;
                    stackOnWall = true;
                    found = true;
                    continue;
                }

                if (!DMBuildingCatalog.IsEdgeSupport(support.PieceId))
                    continue;
                if (!SameStory(top, feet.y))
                    continue;

                Vector3 half = HalfExtents(support);
                Vector3 right = support.transform.right;
                Vector3 forward = support.transform.forward;
                Vector3[] outwards = { right, -right, forward, -forward };
                float[] reach = { half.x, half.x, half.z, half.z };
                for (int e = 0; e < 4; e++)
                {
                    Vector3 outward = outwards[e].normalized;
                    Vector3 edge = support.transform.position + outward * reach[e];
                    float edgeAim = FlatDistance(aimPoint, edge);
                    float edgePlayer = FlatDistance(feet, edge);
                    if (edgeAim > edgeRange && edgePlayer > edgeRange)
                        continue;

                    float score = edgeAim <= edgeRange ? edgeAim : edgePlayer + edgeRange;
                    if (score >= bestScore)
                        continue;

                    bestScore = score;
                    bestEdge = edge;
                    bestEdge.y = top;
                    bestOutward = outward;
                    stackOnWall = false;
                    found = true;
                }
            }

            if (!found)
                return false;

            if (stackOnWall)
            {
                position = bestEdge;
                position.y = bestEdge.y + piece.Size.y * 0.5f + heightOffset;
                rotation = Quaternion.Euler(0f, yawDegrees, 0f) * Quaternion.LookRotation(bestOutward, Vector3.up);
                return true;
            }

            float thickness = Mathf.Max(0.1f, piece.Size.z);
            position = bestEdge + bestOutward * (thickness * 0.5f);
            position.y = bestEdge.y + piece.Size.y * 0.5f + heightOffset;
            Quaternion seat = Quaternion.LookRotation(bestOutward, Vector3.up);
            rotation = Quaternion.Euler(0f, yawDegrees, 0f) * seat;
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

            return surface + (piece != null ? piece.Size.y : 0.4f) * 0.5f + lift;
        }

        static bool TryGroundHit(Ray ray, out RaycastHit chosen)
        {
            chosen = default;
            RaycastHit[] hits = Physics.RaycastAll(ray, DMBuildingGhostProfile.AimDistanceMeters, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < hits.Length; i++)
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

        static bool TryHitGhost(out DMBuildingGhost ghost)
        {
            ghost = null;
            Camera camera = Camera.main;
            if (camera == null)
                return false;

            Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, DMBuildingGhostProfile.AimDistanceMeters, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
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

            if (lookingUp || ceiling)
            {
                DMBuildingGhost wall = NearestWallTop(aim, feet);
                if (wall != null)
                {
                    float wallTop = SurfaceTop(wall);
                    layer = StackLayer(wallTop, feet.y);
                    if (layer < 1)
                        layer = 1;
                    if (DMBuildingCatalog.IsStackLayerAllowed(layer))
                    {
                        position = wall.transform.position;
                        position.y = wallTop + piece.Size.y * 0.5f + lift;
                        position.x = SnapCenter(aim.x, module);
                        position.z = SnapCenter(aim.z, module);
                        return true;
                    }
                }

                if (ceiling)
                    return false;
            }

            DMBuildingGhost foundation = NearestFoundation(aim, module);
            if (foundation == null || !SameStory(SurfaceTop(foundation), feet.y))
                return false;

            Vector3 flat = aim - foundation.transform.position;
            flat.y = 0f;
            if (flat.magnitude > module * 0.6f && FlatDistance(feet, foundation.transform.position) > module * 0.6f)
                return false;

            layer = 0;
            position = foundation.transform.position;
            position.y = SurfaceTop(foundation) + piece.Size.y * 0.5f + lift;
            return true;
        }

        static DMBuildingGhost NearestFoundation(Vector3 aim, float range)
        {
            DMBuildingGhost[] ghosts = FindObjectsByType<DMBuildingGhost>(FindObjectsInactive.Exclude);
            DMBuildingGhost best = null;
            float bestDistance = range;
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost == null || ghost.PieceId != "stone_foundation_4x4")
                    continue;
                float distance = Vector2.Distance(
                    new Vector2(aim.x, aim.z),
                    new Vector2(ghost.transform.position.x, ghost.transform.position.z));
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
            DMBuildingGhost[] ghosts = FindObjectsByType<DMBuildingGhost>(FindObjectsInactive.Exclude);
            DMBuildingGhost best = null;
            float bestScore = range;
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost == null || !ghost.Built || !DMBuildingCatalog.IsVerticalSupport(ghost.PieceId))
                    continue;

                float top = SurfaceTop(ghost);
                if (top < feet.y + 1.25f)
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
            DMBuildingGhost[] ghosts = FindObjectsByType<DMBuildingGhost>(FindObjectsInactive.Exclude);
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
            Project.Player.PlayerController player = UnityEngine.Object.FindAnyObjectByType<Project.Player.PlayerController>();
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
            return span >= (large + small) * 0.5f ? large : small;
        }

        /// <summary>
        /// Centers sit half a module in from the lattice so neighboring edges meet at 2 m or 4 m.
        /// </summary>
        static Vector3 SnapModule(Vector3 point, DMBuildingPiece piece)
        {
            float module = ModuleFor(piece);
            return new Vector3(SnapCenter(point.x, module), point.y, SnapCenter(point.z, module));
        }

        static float SnapCenter(float value, float module)
        {
            float cell = Mathf.Floor(value / module) * module;
            return cell + module * 0.5f;
        }

        static bool TrySnapModuleToNeighbor(Vector3 aim, DMBuildingPiece piece, float lift, out Vector3 position)
        {
            position = default;
            float module = ModuleFor(piece);
            DMBuildingGhost support = NearestSupport(aim, module + DMBuildingGhostProfile.EdgeSnapRangeMeters);
            if (support == null)
                return false;

            Vector3 flat = aim - support.transform.position;
            flat.y = 0f;
            float distance = flat.magnitude;
            if (distance < module * 0.45f)
            {
                position = support.transform.position;
                position.y = SurfaceTop(support) + piece.Size.y * 0.5f + lift;
                return true;
            }

            Vector3 axis = Mathf.Abs(flat.x) >= Mathf.Abs(flat.z) ? Vector3.right : Vector3.forward;
            if (Vector3.Dot(axis, flat) < 0f)
                axis = -axis;
            position = support.transform.position + axis * module;
            position.y = support.transform.position.y + lift;
            return true;
        }

        static DMBuildingGhost NearestSupport(Vector3 aim, float range)
        {
            DMBuildingGhost[] ghosts = FindObjectsByType<DMBuildingGhost>(FindObjectsInactive.Exclude);
            DMBuildingGhost best = null;
            float bestDistance = range;
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost ghost = ghosts[i];
                if (ghost == null || !DMBuildingCatalog.IsEdgeSupport(ghost.PieceId))
                    continue;
                float distance = Vector2.Distance(
                    new Vector2(aim.x, aim.z),
                    new Vector2(ghost.transform.position.x, ghost.transform.position.z));
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
            Collider[] hits = Physics.OverlapBox(center, half, rotation, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
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
                    if (ghost.PieceId == pieceId)
                        return true;
                    continue;
                }

                return true;
            }

            return false;
        }

        static DMBuildingGhost FindDoorFrame(Vector3 near)
        {
            Vector3 feet = PlayerFeet();
            DMBuildingGhost[] ghosts = FindObjectsByType<DMBuildingGhost>(FindObjectsInactive.Exclude);
            DMBuildingGhost best = null;
            float bestDistance = DMBuildingGhostProfile.DoorFrameRangeMeters;
            for (int i = 0; i < ghosts.Length; i++)
            {
                DMBuildingGhost frame = ghosts[i];
                if (frame == null || frame.PieceId != DMBuildingCatalog.DoorFrameId)
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
            marker.StoneCost = piece.StoneCost;
            marker.PaidItem = paid;
            marker.Built = false;
            marker.LocalHalfExtents = piece.Size * 0.5f;
            if (piece.Id == "stone_door_basic")
                marker.MaterialVariantId = "door";
            else
            {
                DMBuildingMaterialVariant variant = DMBuildingMaterialLibrary.FirstForTier(DMBuildingMaterialTier.Stone);
                marker.MaterialVariantId = variant != null ? variant.id : null;
            }

            if (piece.Id == "stone_door_basic")
                ghostObject.AddComponent<DMBuildingDoor>();

            ApplyTint(ghostObject, GhostMaterial(), keepGlass: true);
            return marker;
        }

        static void Finish(DMBuildingGhost ghost)
        {
            if (ghost == null)
                return;
            ghost.Built = true;
            ApplyBuiltMaterial(ghost);
        }

        /// <summary>
        /// M cycles the Stone finish on the built piece under the crosshair, or the nearest built piece.
        /// </summary>
        static void TryApplyNextMaterial()
        {
            if (!TryResolveBuiltTarget(out DMBuildingGhost ghost) || ghost.PieceId == "stone_door_basic")
                return;

            DMBuildingMaterialVariant next = DMBuildingMaterialLibrary.NextForTier(
                DMBuildingMaterialTier.Stone,
                ghost.MaterialVariantId);
            if (next == null)
                return;

            ghost.MaterialVariantId = next.id;
            ApplyBuiltMaterial(ghost);
        }

        static void ApplyBuiltMaterial(DMBuildingGhost ghost)
        {
            if (ghost == null)
                return;

            DMBuildingMaterialVariant variant = DMBuildingMaterialLibrary.Find(ghost.MaterialVariantId);
            if (variant != null && variant.finishedMaterial != null)
                ApplySharedMaterial(ghost.gameObject, variant.finishedMaterial);
            else
                ApplyTint(ghost.gameObject, SolidMaterial(), keepGlass: false);

            PaintPanes(ghost.gameObject, GlassMaterial());
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

        static bool TryResolveBuildTarget(out DMBuildingGhost ghost)
        {
            if (instance != null && instance.lastPlaced != null && !instance.lastPlaced.Built)
            {
                ghost = instance.lastPlaced;
                return true;
            }

            string selectedId = DMBuildingMode.SelectedPiece != null ? DMBuildingMode.SelectedPiece.Id : null;
            ghost = NearestGhost(selectedId, 0);
            if (ghost != null)
                return true;

            ghost = NearestGhost(null, 0);
            return ghost != null;
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
            DMBuildingGhost[] ghosts = FindObjectsByType<DMBuildingGhost>(FindObjectsInactive.Exclude);
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
            DMBuildingGhost[] ghosts = FindObjectsByType<DMBuildingGhost>(FindObjectsInactive.Exclude);
            for (int i = 0; i < ghosts.Length; i++)
            {
                if (ghosts[i] != null && !ghosts[i].Built)
                    RefundAndDestroy(ghosts[i]);
            }

            if (instance == null)
                return;

            instance.buildHold = 0f;
            instance.buildTarget = null;
            DMUiToolkitBuildingHotbar.SetHoldRing(false, 0f, Vector3.zero);
        }

        static void RefundAndDestroy(DMBuildingGhost ghost)
        {
            if (ghost == null)
                return;
            DMBuildingCatalog.RefundStone(ghost.PaidItem, ghost.StoneCost);
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

            bool shift = Keyboard.current != null
                && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            if (DMBuildingMode.MaterialsOpen && !shift)
            {
                DMUiToolkitBuildingHotbar.NudgeMenu(-notches);
                return;
            }

            if (shift)
            {
                float limit = DMBuildingGhostProfile.MaxHeightOffsetMeters;
                heightOffset = Mathf.Clamp(heightOffset + notches * DMBuildingGhostProfile.HeightStepMeters, -limit, limit);
                return;
            }

            float step = YawStep();
            yawDegrees = Mathf.Repeat(yawDegrees + notches * step, 360f);
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
            previewId = pieceId;
            preview.name = "BuildingPreview";
            preview.hideFlags = HideFlags.HideAndDontSave;
            preview.transform.SetParent(transform, false);
            Collider[] colliders = preview.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;
        }

        static void ApplyProfileColors()
        {
            Color ghost = DMBuildingGhostProfile.ResolveGhostColor();
            Paint(GhostMaterial(), ghost);
            Paint(SolidMaterial(), DMBuildingGhostProfile.ResolveFinishedColor());
            Paint(GlassMaterial(), DMBuildingGhostProfile.ResolveGlassColor());
            Color ghostGlass = DMBuildingGhostProfile.ResolveGlassColor();
            ghostGlass.a *= 0.55f;
            Paint(GhostGlassMaterial(), ghostGlass);

            Color blocked = DarkMatterGenesisUiPalette.DeepMagenta;
            blocked.a = ghost.a;
            Paint(BlockedMaterial(), blocked);
        }

        static Material GhostMaterial()
        {
            if (ghostMaterial == null)
                ghostMaterial = CreateMaterial("DM_BuildGhost");
            return ghostMaterial;
        }

        static Material SolidMaterial()
        {
            if (solidMaterial == null)
                solidMaterial = CreateMaterial("DM_BuildGhost_Solid");
            return solidMaterial;
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

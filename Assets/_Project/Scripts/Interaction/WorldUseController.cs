using System.Collections.Generic;
using ECM2;
using Project.AI;
using Project.Building;
using Project.Combat;
using Project.Companions;
using Project.Core;
using Project.Crafting;
using Project.Inventory;
using Project.Pet;
using Project.Player;
using Project.Quests;
using Project.Survival;
using Project.UI;
using UnityEngine;

namespace Project.Interaction
{
    /// <summary>
    /// Central Use (E) handler for pickups, gathering, crafting stations, NPCs, and future world interactions.
    /// </summary>
    [RequireComponent(typeof(ResourceGatherer))]
    [RequireComponent(typeof(InventorySystem))]
    public class WorldUseController : MonoBehaviour
    {
        private const float UseDebounce = 0.15f;
        private const float AimBonus = 500f;
        private const float MaxPickupAimRadius = 0.72f;
        private const float PickupIntentAimRadius = 1.45f;
        private const float PickupIntentDistanceMultiplier = 2.5f;
        private const float MinRegisteredUsePriority = 70f;

        /// <summary>Normalized screen offset used when the forward aim point is off-screen.</summary>
        private static readonly Vector2 PickupAimScreenOffset = new Vector2(0f, 0f);

        private const float PickupAimForwardDistance = 1f;
        private const float DefaultPickupAimHeight = 1f;
        /// <summary>How much the reticle follows camera pitch (0 = locked, 1 = full).</summary>
        private const float PickupAimVerticalArc = 0.25f;
        /// <summary>Extra normalized screen push toward the ground / forward of the player.</summary>
        private const float PickupAimForwardScreenBias = 0.04f;
        private const float CraftingStationAimRadius = 1.25f;
        private const float BuildingControlPanelAimRadius = 1.25f;
        private const float QuestGiverAimRadius = 1.25f;
        private const float RecipePickupScanRange = 8f;

        private static readonly List<IWorldUsable> RegisteredUsables = new List<IWorldUsable>();

        [SerializeField] private float useRange = 4f;

        private ResourceGatherer gatherer;
        private InventorySystem inventory;
        private PlayerController playerController;
        private Camera viewCamera;
        private float lastUseTime = -999f;
        private UIManager promptUiManager;
        private bool worldPromptOwned;

        public static void Register(IWorldUsable usable)
        {
            if (usable == null || RegisteredUsables.Contains(usable))
                return;

            RegisteredUsables.Add(usable);
        }

        public static void Unregister(IWorldUsable usable)
        {
            if (usable == null)
                return;

            RegisteredUsables.Remove(usable);
        }

        private void Awake()
        {
            gatherer = GetComponent<ResourceGatherer>();
            inventory = GetComponent<InventorySystem>();
            playerController = GetComponent<PlayerController>();
        }

        private void Start()
        {
            ResolveCamera();
        }

        private void LateUpdate()
        {
            ClearOwnedWorldPrompt();
        }

        public void TryUse()
        {
            if (!CanUseNow())
                return;

            if (Time.time - lastUseTime < UseDebounce)
                return;

            lastUseTime = Time.time;

            if (inventory == null)
                inventory = GetComponent<InventorySystem>();

            if (gatherer == null)
                gatherer = GetComponent<ResourceGatherer>();

            Camera camera = ResolveCamera();
            if (camera == null)
                return;

            Ray viewRay = BuildScreenCenterRay(camera, transform);
            RaycastHit? aimHit = null;
            LayerMask aimMask = gatherer != null ? gatherer.resourceLayer : Physics.DefaultRaycastLayers;

            if (Physics.Raycast(viewRay, out RaycastHit hit, gatherer != null ? gatherer.gatherRange : useRange, aimMask, QueryTriggerInteraction.Collide))
                aimHit = hit;

            float range = gatherer != null ? gatherer.pickupRange : useRange;
            WorldUseContext context = new WorldUseContext(
                transform,
                transform.position,
                camera,
                inventory,
                gatherer,
                range,
                viewRay,
                aimHit);

            if (TryHandleAimedPriorityInteractUse(context))
                return;

            if (TryHandleEnemyLootUse(context))
                return;

            if (TryHandlePetAdoptUse(context))
                return;

            if (TryHandlePickupUse(context, range))
                return;

            if (TryBestRegisteredUse(context, MinRegisteredUsePriority))
                return;

            if (TryAimedUse(context))
                return;
        }

        public static bool IsCollectiblePickup(ItemPickup pickup, Transform playerRoot = null)
        {
            if (pickup == null || pickup.IsPickedUp || pickup.itemData == null || !pickup.isActiveAndEnabled)
                return false;

            if (pickup.GetComponent<EquippedVisualMarker>() != null || pickup.GetComponentInParent<EquippedVisualMarker>() != null)
                return false;

            if (playerRoot != null && (pickup.transform == playerRoot || pickup.transform.IsChildOf(playerRoot)))
                return false;

            Transform current = pickup.transform;
            while (current != null)
            {
                if (current.CompareTag("Player"))
                    return false;

                current = current.parent;
            }

            return true;
        }

        /// <summary>
        /// True when the player is looking at a collectible pickup (items or recipe scrolls), even if out of range.
        /// </summary>
        public static bool IsPlayerFocusedOnPickup(WorldUseContext context)
        {
            if (TryFindFocusedItemPickup(context, context.UseRange, out _, out _))
                return true;

            return TryFindFocusedRecipePickup(context, out _, out _);
        }

        /// <summary>
        /// True when a nearby item pickup is roughly aimed at — blocks crafting/NPC use stealing E.
        /// </summary>
        public static bool HasCompetingNearbyItemPickup(WorldUseContext context)
        {
            if (IsAimedAtPriorityWorldInteractable(context))
                return false;

            float range = context.UseRange;
            ItemPickup[] pickups = Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Exclude);
            for (int i = 0; i < pickups.Length; i++)
            {
                ItemPickup candidate = pickups[i];
                if (!IsCollectiblePickup(candidate, context.PlayerTransform))
                    continue;

                float distance = Vector3.Distance(context.PlayerPosition, candidate.transform.position);
                if (distance > range)
                    continue;

                Vector3 aimPoint = GetItemPickupAimPoint(candidate);
                if (ScorePickupAim(context.ViewRay, aimPoint, distance, range, MaxPickupAimRadius * 1.35f) >= 0f)
                    return true;
            }

            RecipePickup[] recipePickups = Object.FindObjectsByType<RecipePickup>(FindObjectsInactive.Exclude);
            for (int i = 0; i < recipePickups.Length; i++)
            {
                RecipePickup candidate = recipePickups[i];
                if (candidate == null || candidate.IsLearned)
                    continue;

                float distance = Vector3.Distance(context.PlayerPosition, candidate.transform.position);
                if (distance > range)
                    continue;

                Vector3 aimPoint = GetRecipePickupAimPoint(candidate);
                if (ScorePickupAim(context.ViewRay, aimPoint, distance, range, MaxPickupAimRadius * 1.35f) >= 0f)
                    return true;
            }

            return false;
        }

        public static bool HasActiveEnemyLootInRange(WorldUseContext context)
        {
            EnemyLootBag[] bags = Object.FindObjectsByType<EnemyLootBag>(FindObjectsInactive.Exclude);
            for (int i = 0; i < bags.Length; i++)
            {
                if (bags[i] != null && bags[i].CanPlayerLoot(context.PlayerPosition))
                    return true;
            }

            return false;
        }

        public static bool IsAimedAtPriorityWorldInteractable(WorldUseContext context)
        {
            return IsAimedAtAnyInRangeQuestGiver(context)
                || IsAimedAtAnyInRangeCraftingStation(context)
                || IsAimedAtAnyInRangeBuildingControlPanel(context);
        }

        public static bool IsAimedAtAnyInRangeCraftingStation(WorldUseContext context)
        {
            CraftingStation[] stations = Object.FindObjectsByType<CraftingStation>(FindObjectsInactive.Exclude);
            for (int i = 0; i < stations.Length; i++)
            {
                CraftingStation station = stations[i];
                if (station == null || !station.IsWithinInteractRange(context.PlayerPosition))
                    continue;

                Collider stationCollider = station.InteractCollider;
                if (IsAimedAtCraftingStation(context, station, stationCollider))
                    return true;
            }

            return false;
        }

        public static bool IsAimedAtAnyInRangeQuestGiver(WorldUseContext context)
        {
            QuestGiverNpc[] givers = Object.FindObjectsByType<QuestGiverNpc>(FindObjectsInactive.Exclude);
            for (int i = 0; i < givers.Length; i++)
            {
                QuestGiverNpc giver = givers[i];
                if (giver == null || !giver.IsWithinInteractRange(context.PlayerPosition))
                    continue;

                Collider giverCollider = giver.GetComponentInChildren<Collider>();
                if (IsAimedAtQuestGiver(context, giver, giverCollider))
                    return true;
            }

            return false;
        }

        public static bool IsAimedAtAnyInRangeBuildingControlPanel(WorldUseContext context)
        {
            BuildingControlPanel[] panels = Object.FindObjectsByType<BuildingControlPanel>(FindObjectsInactive.Exclude);
            for (int i = 0; i < panels.Length; i++)
            {
                BuildingControlPanel panel = panels[i];
                if (panel == null || !panel.IsWithinInteractRange(context.PlayerPosition))
                    continue;

                Collider panelCollider = panel.InteractCollider;
                if (IsAimedAtBuildingControlPanel(context, panel, panelCollider))
                    return true;
            }

            return false;
        }

        public static bool IsAimedAtBuildingControlPanel(WorldUseContext context, BuildingControlPanel panel, Collider panelCollider)
        {
            if (panel == null)
                return false;

            if (context.AimHit.HasValue && context.AimHit.Value.collider != null)
            {
                BuildingControlPanel hitPanel = context.AimHit.Value.collider.GetComponentInParent<BuildingControlPanel>();
                if (hitPanel == panel)
                    return true;
            }

            Collider targetCollider = panelCollider;
            if (targetCollider == null)
                targetCollider = panel.GetComponentInChildren<Collider>();

            if (targetCollider == null)
                return false;

            Vector3 aimPoint = targetCollider.bounds.center;
            return GetViewRayDistance(context.ViewRay, aimPoint) <= BuildingControlPanelAimRadius;
        }

        public static bool ShouldBlockNonPickupUse(Transform playerTransform, ResourceGatherer gatherer, float useRange)
        {
            if (playerTransform == null)
                return false;

            Camera camera = ResolveViewCamera(playerTransform);
            if (camera == null)
                return false;

            Ray viewRay = BuildScreenCenterRay(camera, playerTransform);
            WorldUseContext context = new WorldUseContext(
                playerTransform,
                playerTransform.position,
                camera,
                playerTransform.GetComponent<InventorySystem>(),
                gatherer,
                useRange,
                viewRay,
                null);

            if (IsAimedAtPriorityWorldInteractable(context))
                return false;

            return IsPlayerFocusedOnPickup(context) || HasCompetingNearbyItemPickup(context);
        }

        private bool TryHandleAimedPriorityInteractUse(WorldUseContext context)
        {
            IWorldUsable best = null;
            float bestScore = float.MinValue;

            QuestGiverNpc[] givers = Object.FindObjectsByType<QuestGiverNpc>(FindObjectsInactive.Exclude);
            for (int i = 0; i < givers.Length; i++)
            {
                QuestGiverNpc giver = givers[i];
                if (giver == null || !giver.IsWithinInteractRange(context.PlayerPosition))
                    continue;

                Collider giverCollider = giver.GetComponentInChildren<Collider>();
                if (!IsAimedAtQuestGiver(context, giver, giverCollider))
                    continue;

                float distance = PlayerInteractionUtility.DistanceToInteractable(
                    context.PlayerPosition,
                    giverCollider,
                    giver.transform.position);
                float score = 95f - distance;
                if (score <= bestScore)
                    continue;

                best = giver;
                bestScore = score;
            }

            CraftingStation[] stations = Object.FindObjectsByType<CraftingStation>(FindObjectsInactive.Exclude);
            for (int i = 0; i < stations.Length; i++)
            {
                CraftingStation station = stations[i];
                if (station == null || !station.IsWithinInteractRange(context.PlayerPosition))
                    continue;

                Collider stationCollider = station.InteractCollider;
                if (!IsAimedAtCraftingStation(context, station, stationCollider))
                    continue;

                float distance = PlayerInteractionUtility.DistanceToInteractable(
                    context.PlayerPosition,
                    stationCollider,
                    station.transform.position);
                float score = 88f - distance;
                if (score <= bestScore)
                    continue;

                best = station;
                bestScore = score;
            }

            BuildingControlPanel[] controlPanels = Object.FindObjectsByType<BuildingControlPanel>(FindObjectsInactive.Exclude);
            for (int i = 0; i < controlPanels.Length; i++)
            {
                BuildingControlPanel panel = controlPanels[i];
                if (panel == null || !panel.IsWithinInteractRange(context.PlayerPosition))
                    continue;

                Collider panelCollider = panel.InteractCollider;
                if (!IsAimedAtBuildingControlPanel(context, panel, panelCollider))
                    continue;

                float distance = PlayerInteractionUtility.DistanceToInteractable(
                    context.PlayerPosition,
                    panelCollider,
                    panel.transform.position);
                float score = 90f - distance;
                if (score <= bestScore)
                    continue;

                best = panel;
                bestScore = score;
            }

            return best != null && best.TryUse(context);
        }

        private bool TryHandlePetAdoptUse(WorldUseContext context)
        {
            PetWorldAdoptable closest = PetWorldAdoptable.FindClosestAdoptable(context.PlayerPosition, context.UseRange);
            if (closest != null && PetWorldAdoptable.IsWithinImmediateAdoptRange(context.PlayerPosition, closest))
                return closest.TryUse(context);

            if (IsAimedAtPriorityWorldInteractable(context))
                return false;

            PetWorldAdoptable best = PetWorldAdoptable.FindBestAdoptable(context, MinRegisteredUsePriority);
            if (best == null)
            {
                if (closest == null || closest.GetUsePriority(context) < 0f)
                    return false;

                return closest.TryUse(context);
            }

            return best.TryUse(context);
        }

        private bool TryHandleEnemyLootUse(WorldUseContext context)
        {
            EnemyLootBag best = null;
            float bestScore = float.MinValue;

            EnemyLootBag[] bags = Object.FindObjectsByType<EnemyLootBag>(FindObjectsInactive.Exclude);
            for (int i = 0; i < bags.Length; i++)
            {
                EnemyLootBag bag = bags[i];
                if (bag == null)
                    continue;

                float score = bag.GetUsePriority(context);
                if (score < MinRegisteredUsePriority || score <= bestScore)
                    continue;

                best = bag;
                bestScore = score;
            }

            return best != null && best.TryUse(context);
        }

        private bool TryHandlePickupUse(WorldUseContext context, float range)
        {
            if (IsAimedAtPriorityWorldInteractable(context))
                return false;

            if (HasActiveEnemyLootInRange(context))
                return false;

            if (TryFindFocusedItemPickup(context, range, out ItemPickup itemPickup, out bool itemInRange))
            {
                if (itemInRange)
                    itemPickup.TryUse(context);

                return true;
            }

            if (TryFindFocusedRecipePickup(context, out RecipePickup recipePickup, out bool recipeInRange))
            {
                if (recipeInRange)
                    recipePickup.TryUse(context);

                return true;
            }

            return false;
        }

        private static bool TryFindFocusedItemPickup(
            WorldUseContext context,
            float range,
            out ItemPickup pickup,
            out bool inPickupRange)
        {
            pickup = null;
            inPickupRange = false;

            LayerMask itemMask = context.Gatherer != null ? context.Gatherer.itemLayer : Physics.DefaultRaycastLayers;
            float intentRange = range * PickupIntentDistanceMultiplier;
            if (Physics.Raycast(context.ViewRay, out RaycastHit hit, intentRange, itemMask, QueryTriggerInteraction.Collide))
            {
                ItemPickup rayPickup = hit.collider.GetComponentInParent<ItemPickup>();
                if (IsCollectiblePickup(rayPickup, context.PlayerTransform))
                {
                    pickup = rayPickup;
                    inPickupRange = Vector3.Distance(context.PlayerPosition, pickup.transform.position) <= range;
                    return true;
                }
            }

            return TryResolveAimedPickup(context, range, out pickup, out inPickupRange);
        }

        public static bool TryFindFocusedRecipePickup(
            WorldUseContext context,
            out RecipePickup pickup,
            out bool inInteractRange)
        {
            pickup = null;
            inInteractRange = false;

            if (Physics.Raycast(
                    context.ViewRay,
                    out RaycastHit hit,
                    RecipePickupScanRange,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Collide))
            {
                RecipePickup rayPickup = hit.collider.GetComponentInParent<RecipePickup>();
                if (rayPickup != null && !rayPickup.IsLearned)
                {
                    pickup = rayPickup;
                    inInteractRange = Vector3.Distance(context.PlayerPosition, pickup.transform.position) <= pickup.InteractRange;
                    return true;
                }
            }

            RecipePickup[] recipePickups = Object.FindObjectsByType<RecipePickup>(FindObjectsInactive.Exclude);
            float bestScore = -1f;
            for (int i = 0; i < recipePickups.Length; i++)
            {
                RecipePickup candidate = recipePickups[i];
                if (candidate == null || candidate.IsLearned)
                    continue;

                float distance = Vector3.Distance(context.PlayerPosition, candidate.transform.position);
                if (distance > candidate.InteractRange * PickupIntentDistanceMultiplier)
                    continue;

                Vector3 aimPoint = candidate.transform.position + Vector3.up * 0.35f;
                bool candidateInRange = distance <= candidate.InteractRange;
                float aimRadius = candidateInRange ? MaxPickupAimRadius : PickupIntentAimRadius;
                float score = ScorePickupAim(context.ViewRay, aimPoint, distance, candidate.InteractRange, aimRadius);
                if (score <= bestScore)
                    continue;

                bestScore = score;
                pickup = candidate;
                inInteractRange = candidateInRange;
            }

            return pickup != null;
        }

        public static float GetPickupAimHeight(Transform playerTransform)
        {
            if (playerTransform == null)
                return DefaultPickupAimHeight;

            Character character = playerTransform.GetComponent<Character>();
            if (character != null && character.height > 0.1f)
                return character.height * 0.5f;

            return DefaultPickupAimHeight;
        }

        public static Vector3 GetPickupAimWorldPoint(Transform playerTransform, Camera camera)
        {
            Vector3 playerPosition = playerTransform != null ? playerTransform.position : Vector3.zero;
            float aimHeight = GetPickupAimHeight(playerTransform);
            Vector3 forward = GetHorizontalForward(playerTransform, camera);
            return playerPosition + Vector3.up * aimHeight + forward * PickupAimForwardDistance;
        }

        public static Vector3 GetPickupAimScreenPoint(Camera camera, Transform playerTransform)
        {
            float baseScreenY = GetBaseAimScreenY();
            float baseScreenX = Screen.width * (0.5f + PickupAimScreenOffset.x);

            if (camera == null || playerTransform == null)
                return new Vector3(baseScreenX, baseScreenY, 0f);

            Vector3 projected = camera.WorldToScreenPoint(GetPickupAimWorldPoint(playerTransform, camera));
            if (projected.z < 0f)
                return new Vector3(baseScreenX, baseScreenY, 0f);

            float screenX = projected.x;
            float screenY = Mathf.Lerp(baseScreenY, projected.y, PickupAimVerticalArc);
            return new Vector3(screenX, screenY, 0f);
        }

        public static Vector2 GetReticleCanvasOffset(Camera camera, Transform playerTransform, RectTransform canvasRect)
        {
            if (canvasRect == null)
                return Vector2.zero;

            Canvas canvas = canvasRect.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            Vector3 screenPoint = GetPickupAimScreenPoint(camera, playerTransform);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPoint,
                    uiCamera,
                    out Vector2 localPoint))
                return localPoint;

            return GetFallbackReticleCanvasOffset(canvasRect);
        }

        public static Vector2 GetFallbackReticleCanvasOffset(RectTransform canvasRect)
        {
            if (canvasRect == null)
                return Vector2.zero;

            Rect rect = canvasRect.rect;
            return new Vector2(
                rect.width * PickupAimScreenOffset.x,
                rect.height * (PickupAimScreenOffset.y - PickupAimForwardScreenBias));
        }

        public static Ray BuildScreenCenterRay(Camera camera, Transform playerTransform = null)
        {
            if (camera == null)
                return default;

            return camera.ScreenPointToRay(GetPickupAimScreenPoint(camera, playerTransform));
        }

        private static Vector3 GetHorizontalForward(Transform playerTransform, Camera camera)
        {
            Vector3 cameraForward = Vector3.forward;
            if (camera != null)
            {
                cameraForward = camera.transform.forward;
                cameraForward.y = 0f;
            }

            Vector3 playerForward = Vector3.forward;
            if (playerTransform != null)
            {
                playerForward = playerTransform.forward;
                playerForward.y = 0f;
            }

            Vector3 blended = Vector3.zero;
            if (cameraForward.sqrMagnitude > 0.001f)
                blended += cameraForward.normalized;
            if (playerForward.sqrMagnitude > 0.001f)
                blended += playerForward.normalized;

            if (blended.sqrMagnitude < 0.001f)
                return Vector3.forward;

            return blended.normalized;
        }

        private static float GetBaseAimScreenY()
        {
            return Screen.height * (0.5f + PickupAimScreenOffset.y - PickupAimForwardScreenBias);
        }

        private static Vector3 GetFallbackAimScreenPoint()
        {
            return new Vector3(
                Screen.width * (0.5f + PickupAimScreenOffset.x),
                GetBaseAimScreenY(),
                0f);
        }

        public static bool TryGetAimedItemPickup(
            Ray viewRay,
            ResourceGatherer gatherer,
            float fallbackRange,
            out ItemPickup pickup,
            Vector3? playerPosition = null)
        {
            return TryResolveAimedPickup(
                viewRay,
                gatherer,
                fallbackRange,
                playerPosition,
                null,
                out pickup,
                out bool inPickupRange)
                && inPickupRange;
        }

        private static bool TryResolveAimedPickup(
            WorldUseContext context,
            float range,
            out ItemPickup pickup,
            out bool inPickupRange)
        {
            return TryResolveAimedPickup(
                context.ViewRay,
                context.Gatherer,
                range,
                context.PlayerPosition,
                context.PlayerTransform,
                out pickup,
                out inPickupRange);
        }

        private static bool TryResolveAimedPickup(
            Ray viewRay,
            ResourceGatherer gatherer,
            float fallbackRange,
            Vector3? playerPosition,
            Transform playerRoot,
            out ItemPickup pickup,
            out bool inPickupRange)
        {
            pickup = null;
            inPickupRange = false;
            float range = gatherer != null ? gatherer.pickupRange : fallbackRange;
            Vector3 rangeOrigin = playerPosition ?? viewRay.origin;
            float intentRange = range * PickupIntentDistanceMultiplier;
            ItemPickup[] pickups = Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Exclude);

            float bestScore = -1f;
            for (int i = 0; i < pickups.Length; i++)
            {
                ItemPickup candidate = pickups[i];
                if (!IsCollectiblePickup(candidate, playerRoot))
                    continue;

                float distance = Vector3.Distance(rangeOrigin, candidate.transform.position);
                if (distance > intentRange)
                    continue;

                Vector3 aimPoint = GetItemPickupAimPoint(candidate);
                bool candidateInRange = distance <= range;
                float aimRadius = candidateInRange ? MaxPickupAimRadius : PickupIntentAimRadius;
                float score = ScorePickupAim(viewRay, aimPoint, distance, range, aimRadius);
                if (score <= bestScore)
                    continue;

                bestScore = score;
                pickup = candidate;
                inPickupRange = candidateInRange;
            }

            return pickup != null;
        }

        private static Vector3 GetItemPickupAimPoint(ItemPickup pickup)
        {
            if (pickup == null)
                return Vector3.zero;

            Collider col = pickup.GetComponentInChildren<Collider>();
            if (col != null)
                return col.bounds.center;

            return pickup.transform.position + Vector3.up * 0.2f;
        }

        private static Vector3 GetRecipePickupAimPoint(RecipePickup pickup)
        {
            if (pickup == null)
                return Vector3.zero;

            return pickup.transform.position + Vector3.up * 0.35f;
        }

        public static Vector2 PickupAimScreenOffsetNormalized => PickupAimScreenOffset;

        public static bool IsAimedAtCraftingStation(WorldUseContext context, CraftingStation station, Collider stationCollider)
        {
            if (station == null)
                return false;

            if (context.AimHit.HasValue && context.AimHit.Value.collider != null)
            {
                CraftingStation hitStation = context.AimHit.Value.collider.GetComponentInParent<CraftingStation>();
                if (hitStation == station)
                    return true;
            }

            Collider targetCollider = stationCollider;
            if (targetCollider == null)
                targetCollider = station.GetComponentInChildren<Collider>();

            if (targetCollider == null)
                return false;

            Vector3 aimPoint = targetCollider.bounds.center;
            return GetViewRayDistance(context.ViewRay, aimPoint) <= CraftingStationAimRadius;
        }

        public static bool IsAimedAtQuestGiver(WorldUseContext context, QuestGiverNpc giver, Collider giverCollider)
        {
            if (giver == null)
                return false;

            if (context.AimHit.HasValue && context.AimHit.Value.collider != null)
            {
                QuestGiverNpc hitGiver = context.AimHit.Value.collider.GetComponentInParent<QuestGiverNpc>();
                if (hitGiver == giver)
                    return true;
            }

            Collider targetCollider = giverCollider;
            if (targetCollider == null)
                targetCollider = giver.GetComponentInChildren<Collider>();

            if (targetCollider == null)
                return false;

            Vector3 aimPoint = targetCollider.bounds.center;
            return GetViewRayDistance(context.ViewRay, aimPoint) <= QuestGiverAimRadius;
        }

        private bool CanUseNow()
        {
            if (!GameSession.HasStarted)
                return false;

            SurvivalStats survivalStats = GetComponent<SurvivalStats>();
            if (survivalStats != null && survivalStats.IsDead)
                return false;

            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            if (playerController != null)
            {
                if (playerController.IsInventoryOpen
                    || playerController.IsJournalOpen
                    || playerController.IsMapOpen
                    || playerController.IsQuestDialogOpen
                    || playerController.IsLootDialogOpen
                    || playerController.IsBuildingControlOpen
                    || playerController.IsOpticsOpen)
                    return false;
            }

            return true;
        }

        private bool TryAimedUse(WorldUseContext context)
        {
            if (IsPlayerFocusedOnPickup(context) || HasCompetingNearbyItemPickup(context))
                return false;

            if (!context.AimHit.HasValue)
                return false;

            Collider hitCollider = context.AimHit.Value.collider;
            if (hitCollider == null)
                return false;

            ItemPickup pickup = hitCollider.GetComponentInParent<ItemPickup>();
            if (IsCollectiblePickup(pickup, context.PlayerTransform) && pickup.TryUse(context))
                return true;

            ResourceNode node = hitCollider.GetComponentInParent<ResourceNode>();
            if (node != null && context.Gatherer != null)
            {
                node.Gather(context.Gatherer);
                return true;
            }

            IWorldUsable usable = hitCollider.GetComponentInParent<IWorldUsable>();
            if (usable != null && usable.TryUse(context))
                return true;

            return false;
        }

        private bool TryBestRegisteredUse(WorldUseContext context, float minPriority)
        {
            if (!IsAimedAtPriorityWorldInteractable(context) && !HasActiveEnemyLootInRange(context))
            {
                if (IsPlayerFocusedOnPickup(context))
                    return false;

                if (HasCompetingNearbyItemPickup(context))
                    return false;
            }

            IWorldUsable best = null;
            float bestScore = float.MinValue;

            for (int i = 0; i < RegisteredUsables.Count; i++)
            {
                IWorldUsable usable = RegisteredUsables[i];
                if (usable == null)
                    continue;

                float score = usable.GetUsePriority(context);
                if (score < minPriority || score <= bestScore)
                    continue;

                best = usable;
                bestScore = score;
            }

            return best != null && best.TryUse(context);
        }

        internal static float GetViewRayDistance(Ray viewRay, Vector3 worldPoint)
        {
            Vector3 offset = worldPoint - viewRay.origin;
            float projection = Vector3.Dot(offset, viewRay.direction);
            if (projection < 0f)
                return float.MaxValue;

            Vector3 closestPoint = viewRay.origin + viewRay.direction * projection;
            return Vector3.Distance(closestPoint, worldPoint);
        }

        internal static float ScorePickupAim(Ray viewRay, Vector3 pickupPosition, float distance, float useRange, float maxAimRadius = MaxPickupAimRadius)
        {
            float rayDistance = GetViewRayDistance(viewRay, pickupPosition);
            if (rayDistance > maxAimRadius)
                return -1f;

            float aimScore = (1f - rayDistance / maxAimRadius) * 400f;
            float distanceScore = Mathf.Max(0f, useRange - distance) * 2f;
            return aimScore + distanceScore;
        }

        private Camera ResolveCamera()
        {
            if (viewCamera != null)
                return viewCamera;

            viewCamera = ResolveViewCamera(transform);
            return viewCamera;
        }

        private static Camera ResolveViewCamera(Transform playerTransform)
        {
            if (playerTransform == null)
                return Camera.main;

            PlayerController controller = playerTransform.GetComponent<PlayerController>();
            if (controller != null && controller.GameplayCamera != null)
                return controller.GameplayCamera;

            Character character = playerTransform.GetComponent<Character>();
            if (character != null && character.camera != null)
                return character.camera;

            return Camera.main;
        }

        public static Camera ResolveViewCameraForInteract(Transform playerTransform) => ResolveViewCamera(playerTransform);

        private void ResolveWorldInteractionPrompt()
        {
            if (!ShouldDriveWorldInteractionPrompt())
            {
                ClearOwnedWorldPrompt();
                return;
            }

            WorldUseContext context = BuildPromptContext();
            if (context.PlayerTransform == null)
            {
                ClearOwnedWorldPrompt();
                return;
            }

            string message = BuildWorldInteractionPrompt(context);
            if (!string.IsNullOrEmpty(message))
            {
                if (promptUiManager == null)
                    promptUiManager = FindAnyObjectByType<UIManager>();

                promptUiManager?.ShowInteractionPrompt(message);
                worldPromptOwned = true;
                return;
            }

            ClearOwnedWorldPrompt();
        }

        private bool ShouldDriveWorldInteractionPrompt()
        {
            if (!GameSession.HasStarted)
                return false;

            if (playerController == null)
                playerController = GetComponent<PlayerController>();

            if (playerController == null)
                return true;

            return !playerController.IsInventoryOpen
                && !playerController.IsJournalOpen
                && !playerController.IsMapOpen
                && !playerController.IsQuestDialogOpen
                && !playerController.IsLootDialogOpen
                && !playerController.IsBuildingControlOpen
                && !playerController.IsOpticsOpen;
        }

        private void ClearOwnedWorldPrompt()
        {
            if (!worldPromptOwned)
                return;

            if (promptUiManager == null)
                promptUiManager = FindAnyObjectByType<UIManager>();

            promptUiManager?.HideInteractionPrompt();
            worldPromptOwned = false;
        }

        private WorldUseContext BuildPromptContext()
        {
            if (gatherer == null)
                gatherer = GetComponent<ResourceGatherer>();

            if (inventory == null)
                inventory = GetComponent<InventorySystem>();

            Camera camera = ResolveCamera();
            Ray viewRay = camera != null
                ? BuildScreenCenterRay(camera, transform)
                : default;

            RaycastHit? aimHit = null;
            if (camera != null && gatherer != null
                && Physics.Raycast(viewRay, out RaycastHit hit, gatherer.gatherRange, gatherer.resourceLayer, QueryTriggerInteraction.Collide))
            {
                aimHit = hit;
            }

            float range = gatherer != null ? gatherer.pickupRange : useRange;
            return new WorldUseContext(
                transform,
                transform.position,
                camera,
                inventory,
                gatherer,
                range,
                viewRay,
                aimHit);
        }

        private static string BuildWorldInteractionPrompt(WorldUseContext context)
        {
            if (TryFindFocusedItemPickup(context, context.UseRange, out ItemPickup itemPickup, out bool itemInRange)
                && itemInRange
                && itemPickup != null
                && itemPickup.itemData != null)
            {
                return itemPickup.promptText + " " + itemPickup.itemData.itemName;
            }

            if (TryFindFocusedRecipePickup(context, out RecipePickup recipePickup, out bool recipeInRange)
                && recipeInRange
                && recipePickup != null
                && !recipePickup.IsLearned)
            {
                return recipePickup.GetInteractionPromptMessage();
            }

            PetWorldAdoptable adoptable = PetWorldAdoptable.FindAdoptableForPrompt(context);
            if (adoptable != null)
                return adoptable.PromptText + " " + (adoptable.GetComponent<PetController>()?.DisplayName ?? "pet");

            InjuredPioneerLabRecoverable injuredRecoverable = InjuredPioneerLabRecoverable.FindForPrompt(context);
            if (injuredRecoverable != null)
                return injuredRecoverable.GetPromptText();

            QuestGiverNpc questGiver = FindClosestQuestGiverInRange(context.PlayerPosition);
            if (questGiver != null)
                return questGiver.GetInteractionPromptMessage();

            CraftingStation craftingStation = FindAimedCraftingStationInRange(context);
            if (craftingStation != null)
                return craftingStation.GetInteractionPromptMessage();

            BuildingControlPanel controlPanel = FindAimedBuildingControlPanelInRange(context);
            if (controlPanel != null)
                return controlPanel.GetInteractionPromptMessage();

            EnemyLootBag lootBag = FindClosestLootBagInRange(context.PlayerPosition);
            if (lootBag != null)
                return lootBag.GetInteractionPromptMessage();

            return null;
        }

        private static QuestGiverNpc FindClosestQuestGiverInRange(Vector3 playerPosition)
        {
            QuestGiverNpc[] givers = Object.FindObjectsByType<QuestGiverNpc>(FindObjectsInactive.Exclude);
            QuestGiverNpc best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < givers.Length; i++)
            {
                QuestGiverNpc giver = givers[i];
                if (giver == null || !giver.IsWithinInteractRange(playerPosition))
                    continue;

                float distance = Vector3.Distance(playerPosition, giver.transform.position);
                if (distance >= bestDistance)
                    continue;

                best = giver;
                bestDistance = distance;
            }

            return best;
        }

        private static CraftingStation FindAimedCraftingStationInRange(WorldUseContext context)
        {
            CraftingStation[] stations = Object.FindObjectsByType<CraftingStation>(FindObjectsInactive.Exclude);
            CraftingStation best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < stations.Length; i++)
            {
                CraftingStation station = stations[i];
                if (station == null || !station.IsWithinInteractRange(context.PlayerPosition))
                    continue;

                Collider stationCollider = station.InteractCollider;
                if (!IsAimedAtCraftingStation(context, station, stationCollider))
                    continue;

                float distance = PlayerInteractionUtility.DistanceToInteractable(
                    context.PlayerPosition,
                    stationCollider,
                    station.transform.position);
                if (distance >= bestDistance)
                    continue;

                best = station;
                bestDistance = distance;
            }

            return best;
        }

        private static BuildingControlPanel FindAimedBuildingControlPanelInRange(WorldUseContext context)
        {
            BuildingControlPanel[] panels = Object.FindObjectsByType<BuildingControlPanel>(FindObjectsInactive.Exclude);
            BuildingControlPanel best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < panels.Length; i++)
            {
                BuildingControlPanel panel = panels[i];
                if (panel == null || !panel.IsWithinInteractRange(context.PlayerPosition))
                    continue;

                Collider panelCollider = panel.InteractCollider;
                if (!IsAimedAtBuildingControlPanel(context, panel, panelCollider))
                    continue;

                float distance = PlayerInteractionUtility.DistanceToInteractable(
                    context.PlayerPosition,
                    panelCollider,
                    panel.transform.position);
                if (distance >= bestDistance)
                    continue;

                best = panel;
                bestDistance = distance;
            }

            return best;
        }

        private static EnemyLootBag FindClosestLootBagInRange(Vector3 playerPosition)
        {
            EnemyLootBag[] bags = Object.FindObjectsByType<EnemyLootBag>(FindObjectsInactive.Exclude);
            EnemyLootBag best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < bags.Length; i++)
            {
                EnemyLootBag bag = bags[i];
                if (bag == null || !bag.CanPlayerLoot(playerPosition))
                    continue;

                float distance = Vector3.Distance(playerPosition, bag.transform.position);
                if (distance >= bestDistance)
                    continue;

                best = bag;
                bestDistance = distance;
            }

            return best;
        }
    }
}

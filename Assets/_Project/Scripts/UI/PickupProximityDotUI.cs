using System.Collections.Generic;
using Project.Core;
using Project.Crafting;
using Project.Data;
using Project.Interaction;
using Project.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Soft colored on-screen dots for nearby world pickups, blueprint scrolls, and plant harvest nodes.
    /// Within <see cref="WorldUseController.MaxPickupDistance"/> and a forward camera cone,
    /// only the single nearest target is dotted.
    /// </summary>
    public class PickupProximityDotUI : MonoBehaviour
    {
        public static PickupProximityDotUI Instance { get; private set; }

        private bool uitkHidden;

        /// <summary>Full cone angle (degrees) aligned with camera forward for exclusive pickup dots.</summary>
        public const float PickupConeFovDegrees = 70f;

        [SerializeField, Tooltip("Only the nearest pickup/blueprint/harvest within this radius gets a dot.")]
        private float nearExclusiveRadius = WorldUseController.MaxPickupDistance;
        [SerializeField, Tooltip("Full cone FOV in degrees facing camera forward.")]
        private float pickupConeFovDegrees = PickupConeFovDegrees;
        [SerializeField] private float verticalWorldOffset = ProximityDotStyle.DefaultWorldOffset;

        private readonly HashSet<ItemPickup> trackedPickups = new HashSet<ItemPickup>();
        private readonly HashSet<RecipePickup> trackedRecipes = new HashSet<RecipePickup>();
        private readonly HashSet<ResourceNode> trackedHarvestNodes = new HashSet<ResourceNode>();
        private readonly Dictionary<ItemPickup, RectTransform> activeDots = new Dictionary<ItemPickup, RectTransform>();
        private readonly Dictionary<RecipePickup, RectTransform> activeRecipeDots = new Dictionary<RecipePickup, RectTransform>();
        private readonly Dictionary<ResourceNode, RectTransform> activeHarvestDots = new Dictionary<ResourceNode, RectTransform>();
        private readonly Stack<RectTransform> dotPool = new Stack<RectTransform>();

        private readonly List<ItemPickup> stalePickupScratch = new List<ItemPickup>(8);
        private readonly List<RecipePickup> staleRecipeScratch = new List<RecipePickup>(8);
        private readonly List<ResourceNode> staleHarvestScratch = new List<ResourceNode>(8);
        private readonly List<DotCandidate> candidateScratch = new List<DotCandidate>(32);
        private readonly HashSet<ItemPickup> visiblePickupScratch = new HashSet<ItemPickup>();
        private readonly HashSet<RecipePickup> visibleRecipeScratch = new HashSet<RecipePickup>();
        private readonly HashSet<ResourceNode> visibleHarvestScratch = new HashSet<ResourceNode>();

        private struct DotCandidate
        {
            public ItemPickup Pickup;
            public RecipePickup Recipe;
            public ResourceNode Harvest;
            public Vector3 WorldPos;
            public float Distance;
        }

        private RectTransform dotLayer;
        private Canvas rootCanvas;
        private Camera worldCamera;
        private Transform playerTransform;
        private PlayerController cachedPlayer;

        private ItemPickup primaryNearPickup;
        private RecipePickup primaryNearRecipe;
        private ResourceNode primaryNearHarvest;

        public static bool TryGetPrimaryNearPickup(out ItemPickup pickup)
        {
            pickup = null;
            if (Instance == null)
                return false;

            pickup = Instance.primaryNearPickup;
            return pickup != null && !pickup.IsPickedUp && WorldUseController.IsCollectiblePickup(pickup);
        }

        public static bool TryGetPrimaryNearRecipe(out RecipePickup recipe)
        {
            recipe = null;
            if (Instance == null)
                return false;

            recipe = Instance.primaryNearRecipe;
            return recipe != null && !recipe.IsLearned;
        }

        public static void Register(ItemPickup pickup)
        {
            if (pickup == null || Instance == null)
                return;

            Instance.trackedPickups.Add(pickup);
        }

        public static void Unregister(ItemPickup pickup)
        {
            if (Instance == null)
                return;

            if (pickup != null)
                Instance.trackedPickups.Remove(pickup);

            Instance.HideDot(pickup);
        }

        public static void NotifyCollected(ItemPickup pickup)
        {
            if (pickup == null || Instance == null)
                return;

            Instance.trackedPickups.Remove(pickup);
            Instance.HideDot(pickup);
        }

        public static void RegisterRecipe(RecipePickup recipe)
        {
            if (recipe == null || Instance == null || recipe.IsLearned)
                return;

            Instance.trackedRecipes.Add(recipe);
        }

        public static void UnregisterRecipe(RecipePickup recipe)
        {
            if (Instance == null)
                return;

            if (recipe != null)
                Instance.trackedRecipes.Remove(recipe);

            Instance.HideRecipeDot(recipe);
        }

        public static void NotifyRecipeCollected(RecipePickup recipe)
        {
            if (recipe == null || Instance == null)
                return;

            Instance.trackedRecipes.Remove(recipe);
            Instance.HideRecipeDot(recipe);
        }

        public static void RegisterHarvestNode(ResourceNode node)
        {
            if (node == null || Instance == null)
                return;

            if (node.interactionMode != ResourceNodeInteractionMode.HoldHarvest)
                return;

            Instance.trackedHarvestNodes.Add(node);
        }

        public static void UnregisterHarvestNode(ResourceNode node)
        {
            if (Instance == null)
                return;

            if (node != null)
                Instance.trackedHarvestNodes.Remove(node);

            Instance.HideHarvestDot(node);
        }

        public static void NotifyHarvested(ResourceNode node)
        {
            if (node == null || Instance == null)
                return;

            Instance.trackedHarvestNodes.Remove(node);
            Instance.HideHarvestDot(node);
        }

        private void Awake()
        {
            Instance = this;
            nearExclusiveRadius = WorldUseController.MaxPickupDistance;
            BuildDotLayer();
        }

        private void Start()
        {
            ItemPickup[] existingPickups = SceneComponentCache.GetAll<ItemPickup>(FindObjectsInactive.Exclude, refreshInterval: 0.05f);
            for (int i = 0; i < existingPickups.Length; i++)
                trackedPickups.Add(existingPickups[i]);

            RecipePickup[] existingRecipes = SceneComponentCache.GetAll<RecipePickup>(FindObjectsInactive.Exclude, refreshInterval: 0.05f);
            for (int i = 0; i < existingRecipes.Length; i++)
            {
                RecipePickup recipe = existingRecipes[i];
                if (recipe != null && !recipe.IsLearned)
                    trackedRecipes.Add(recipe);
            }

            ResourceNode[] existingNodes = SceneComponentCache.GetAll<ResourceNode>(FindObjectsInactive.Exclude, refreshInterval: 0.05f);
            for (int i = 0; i < existingNodes.Length; i++)
            {
                ResourceNode node = existingNodes[i];
                if (node != null && node.interactionMode == ResourceNodeInteractionMode.HoldHarvest)
                    trackedHarvestNodes.Add(node);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void LateUpdate()
        {
            // UITK world chrome paints these dots now. Without this the full pickup/recipe/node
            // scan below still ran every frame behind the UITK layer.
            if (DMUiToolkitHud.IsDriving)
            {
                if (!uitkHidden)
                {
                    HideAllDots();
                    uitkHidden = true;
                }
                return;
            }

            uitkHidden = false;

            if (!GameSession.HasStarted)
            {
                HideAllDots();
                return;
            }

            PlayerController player = ResolvePlayer();
            if (player != null && player.BlocksCombatInput)
            {
                HideAllDots();
                return;
            }

            if (!ResolveReferences())
                return;

            PruneInvalidPickups();
            PruneInvalidRecipes();
            PruneInvalidHarvestNodes();
            CleanupOrphanedDots();
            CleanupOrphanedRecipeDots();
            CleanupOrphanedHarvestDots();

            RebuildVisibleDotSet();

            foreach (ItemPickup pickup in trackedPickups)
            {
                if (visiblePickupScratch.Contains(pickup))
                    ShowDot(pickup);
                else
                    HideDot(pickup);
            }

            foreach (RecipePickup recipe in trackedRecipes)
            {
                if (visibleRecipeScratch.Contains(recipe))
                    ShowRecipeDot(recipe);
                else
                    HideRecipeDot(recipe);
            }

            foreach (ResourceNode node in trackedHarvestNodes)
            {
                if (visibleHarvestScratch.Contains(node))
                    ShowHarvestDot(node);
                else
                    HideHarvestDot(node);
            }

            CleanupOrphanedDots();
            CleanupOrphanedRecipeDots();
            CleanupOrphanedHarvestDots();
        }

        private void RebuildVisibleDotSet()
        {
            visiblePickupScratch.Clear();
            visibleRecipeScratch.Clear();
            visibleHarvestScratch.Clear();
            candidateScratch.Clear();
            primaryNearPickup = null;
            primaryNearRecipe = null;
            primaryNearHarvest = null;

            float nearR = Mathf.Min(WorldUseController.MaxPickupDistance, Mathf.Max(0.5f, nearExclusiveRadius));
            float nearSqr = nearR * nearR;
            float halfCone = Mathf.Clamp(pickupConeFovDegrees, 1f, 179f) * 0.5f;
            Vector3 coneOrigin = worldCamera != null
                ? worldCamera.transform.position
                : playerTransform.position;
            Vector3 coneForward = worldCamera != null
                ? worldCamera.transform.forward
                : playerTransform.forward;

            foreach (ItemPickup pickup in trackedPickups)
            {
                if (!IsPickupTrackable(pickup) || pickup.IsPickedUp || pickup.itemData == null)
                    continue;

                Vector3 pos = pickup.transform.position;
                float sqr = (pos - playerTransform.position).sqrMagnitude;
                if (sqr > nearSqr || !IsInsideForwardCone(coneOrigin, coneForward, pos, halfCone))
                    continue;

                candidateScratch.Add(new DotCandidate
                {
                    Pickup = pickup,
                    WorldPos = pos,
                    Distance = Mathf.Sqrt(sqr)
                });
            }

            foreach (RecipePickup recipe in trackedRecipes)
            {
                if (recipe == null || recipe.IsLearned)
                    continue;

                Vector3 pos = recipe.transform.position;
                float sqr = (pos - playerTransform.position).sqrMagnitude;
                if (sqr > nearSqr || !IsInsideForwardCone(coneOrigin, coneForward, pos, halfCone))
                    continue;

                candidateScratch.Add(new DotCandidate
                {
                    Recipe = recipe,
                    WorldPos = pos,
                    Distance = Mathf.Sqrt(sqr)
                });
            }

            foreach (ResourceNode node in trackedHarvestNodes)
            {
                if (node == null
                    || node.interactionMode != ResourceNodeInteractionMode.HoldHarvest
                    || node.resourceItem == null
                    || node.IsHoldActive)
                {
                    continue;
                }

                Vector3 pos = node.GetNodeCenter();
                float sqr = (pos - playerTransform.position).sqrMagnitude;
                if (sqr > nearSqr || !IsInsideForwardCone(coneOrigin, coneForward, pos, halfCone))
                    continue;

                candidateScratch.Add(new DotCandidate
                {
                    Harvest = node,
                    WorldPos = pos,
                    Distance = Mathf.Sqrt(sqr)
                });
            }

            if (candidateScratch.Count == 0)
                return;

            candidateScratch.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            AcceptCandidate(candidateScratch[0]);
            primaryNearPickup = candidateScratch[0].Pickup;
            primaryNearRecipe = candidateScratch[0].Recipe;
            primaryNearHarvest = candidateScratch[0].Harvest;
        }

        private static bool IsInsideForwardCone(
            Vector3 origin,
            Vector3 forward,
            Vector3 worldPos,
            float halfAngleDegrees)
        {
            Vector3 toTarget = worldPos - origin;
            if (toTarget.sqrMagnitude < 0.0001f)
                return true;

            return Vector3.Angle(forward, toTarget) <= halfAngleDegrees;
        }

        private void AcceptCandidate(DotCandidate c)
        {
            if (c.Pickup != null)
                visiblePickupScratch.Add(c.Pickup);
            if (c.Recipe != null)
                visibleRecipeScratch.Add(c.Recipe);
            if (c.Harvest != null)
                visibleHarvestScratch.Add(c.Harvest);
        }

        private static bool IsPickupTrackable(ItemPickup pickup)
        {
            return WorldUseController.IsCollectiblePickup(pickup);
        }

        private void PruneInvalidPickups()
        {
            trackedPickups.RemoveWhere(pickup => pickup == null);
        }

        private void PruneInvalidRecipes()
        {
            trackedRecipes.RemoveWhere(recipe => recipe == null || recipe.IsLearned);
        }

        private void PruneInvalidHarvestNodes()
        {
            trackedHarvestNodes.RemoveWhere(node =>
                node == null || node.interactionMode != ResourceNodeInteractionMode.HoldHarvest);
        }

        private void CleanupOrphanedDots()
        {
            stalePickupScratch.Clear();
            foreach (KeyValuePair<ItemPickup, RectTransform> pair in activeDots)
            {
                ItemPickup pickup = pair.Key;
                if (IsPickupTrackable(pickup) && !pickup.IsPickedUp)
                    continue;

                stalePickupScratch.Add(pickup);
            }

            for (int i = 0; i < stalePickupScratch.Count; i++)
                HideDot(stalePickupScratch[i]);
        }

        private void CleanupOrphanedRecipeDots()
        {
            staleRecipeScratch.Clear();
            foreach (KeyValuePair<RecipePickup, RectTransform> pair in activeRecipeDots)
            {
                RecipePickup recipe = pair.Key;
                if (recipe != null && !recipe.IsLearned)
                    continue;

                staleRecipeScratch.Add(recipe);
            }

            for (int i = 0; i < staleRecipeScratch.Count; i++)
                HideRecipeDot(staleRecipeScratch[i]);
        }

        private void CleanupOrphanedHarvestDots()
        {
            staleHarvestScratch.Clear();
            foreach (KeyValuePair<ResourceNode, RectTransform> pair in activeHarvestDots)
            {
                ResourceNode node = pair.Key;
                if (node != null
                    && node.interactionMode == ResourceNodeInteractionMode.HoldHarvest
                    && node.resourceItem != null
                    && !node.IsHoldActive)
                {
                    continue;
                }

                staleHarvestScratch.Add(node);
            }

            for (int i = 0; i < staleHarvestScratch.Count; i++)
                HideHarvestDot(staleHarvestScratch[i]);
        }

        private PlayerController ResolvePlayer()
        {
            if (cachedPlayer != null)
                return cachedPlayer;

            cachedPlayer = PlayerLocator.FindPlayerController();
            if (cachedPlayer != null)
                playerTransform = cachedPlayer.transform;

            return cachedPlayer;
        }

        private bool ResolveReferences()
        {
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            if (worldCamera == null)
                worldCamera = PlayerReference.ResolveCamera();

            if (playerTransform == null)
                ResolvePlayer();

            return rootCanvas != null && worldCamera != null && playerTransform != null;
        }

        private void ShowDot(ItemPickup pickup)
        {
            if (pickup == null)
                return;

            if (!activeDots.TryGetValue(pickup, out RectTransform dotRect))
            {
                dotRect = AcquireDot(ProximityDotStyle.PickupColor(pickup.itemData.itemType));
                activeDots[pickup] = dotRect;
            }

            PositionDot(dotRect, pickup.transform.position);
        }

        private void ShowRecipeDot(RecipePickup recipe)
        {
            if (recipe == null)
                return;

            if (!activeRecipeDots.TryGetValue(recipe, out RectTransform dotRect))
            {
                dotRect = AcquireDot(ProximityDotStyle.RecipeColor);
                activeRecipeDots[recipe] = dotRect;
            }

            PositionDot(dotRect, recipe.transform.position);
        }

        private void ShowHarvestDot(ResourceNode node)
        {
            if (node == null || node.resourceItem == null)
                return;

            if (!activeHarvestDots.TryGetValue(node, out RectTransform dotRect))
            {
                dotRect = AcquireDot(ProximityDotStyle.PickupColor(node.resourceItem.itemType));
                activeHarvestDots[node] = dotRect;
            }

            PositionDot(dotRect, node.GetNodeCenter());
        }

        private void PositionDot(RectTransform dotRect, Vector3 worldPosition)
        {
            Vector3 worldPoint = worldPosition + Vector3.up * verticalWorldOffset;
            Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPoint);
            if (screenPoint.z <= 0f)
            {
                dotRect.gameObject.SetActive(false);
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dotLayer,
                    screenPoint,
                    rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldCamera,
                    out Vector2 localPoint))
            {
                dotRect.gameObject.SetActive(true);
                dotRect.anchoredPosition = localPoint;
            }
        }

        private void HideDot(ItemPickup pickup)
        {
            if (pickup == null)
            {
                CleanupOrphanedDots();
                return;
            }

            if (!activeDots.TryGetValue(pickup, out RectTransform dotRect))
                return;

            activeDots.Remove(pickup);
            ReleaseDotRect(dotRect);
        }

        private void HideRecipeDot(RecipePickup recipe)
        {
            if (recipe == null)
            {
                CleanupOrphanedRecipeDots();
                return;
            }

            if (!activeRecipeDots.TryGetValue(recipe, out RectTransform dotRect))
                return;

            activeRecipeDots.Remove(recipe);
            ReleaseDotRect(dotRect);
        }

        private void HideHarvestDot(ResourceNode node)
        {
            if (node == null)
            {
                CleanupOrphanedHarvestDots();
                return;
            }

            if (!activeHarvestDots.TryGetValue(node, out RectTransform dotRect))
                return;

            activeHarvestDots.Remove(node);
            ReleaseDotRect(dotRect);
        }

        private void HideAllDots()
        {
            foreach (KeyValuePair<ItemPickup, RectTransform> pair in activeDots)
                ReleaseDotRect(pair.Value);

            foreach (KeyValuePair<RecipePickup, RectTransform> pair in activeRecipeDots)
                ReleaseDotRect(pair.Value);

            foreach (KeyValuePair<ResourceNode, RectTransform> pair in activeHarvestDots)
                ReleaseDotRect(pair.Value);

            activeDots.Clear();
            activeRecipeDots.Clear();
            activeHarvestDots.Clear();
            trackedPickups.RemoveWhere(pickup => pickup == null);
            trackedRecipes.RemoveWhere(recipe => recipe == null);
            trackedHarvestNodes.RemoveWhere(node => node == null);
            primaryNearPickup = null;
            primaryNearRecipe = null;
            primaryNearHarvest = null;
        }

        private void ReleaseDotRect(RectTransform dotRect)
        {
            if (dotRect == null)
                return;

            dotRect.gameObject.SetActive(false);
            dotPool.Push(dotRect);
        }

        private RectTransform AcquireDot(Color color)
        {
            RectTransform dotRect = dotPool.Count > 0 ? dotPool.Pop() : ProximityDotStyle.CreateDotWidget(dotLayer);
            ProximityDotStyle.ApplyColor(dotRect, color);
            return dotRect;
        }

        private void BuildDotLayer()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            rootCanvas = canvas;
            GameObject layerObject = new GameObject("PickupProximityDots", typeof(RectTransform));
            layerObject.transform.SetParent(canvas.transform, false);
            dotLayer = layerObject.GetComponent<RectTransform>();
            MenuUiBuilder.StretchRectToFill(dotLayer);
            dotLayer.SetAsLastSibling();
        }
    }
}

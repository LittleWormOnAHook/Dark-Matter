using System.Collections.Generic;
using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Soft colored on-screen dots for nearby world pickups and plant harvest nodes.
    /// </summary>
    public class PickupProximityDotUI : MonoBehaviour
    {
        public static PickupProximityDotUI Instance { get; private set; }

        [SerializeField] private float proximityRadius = 2f;
        [SerializeField] private float verticalWorldOffset = ProximityDotStyle.DefaultWorldOffset;

        private readonly HashSet<ItemPickup> trackedPickups = new HashSet<ItemPickup>();
        private readonly HashSet<ResourceNode> trackedHarvestNodes = new HashSet<ResourceNode>();
        private readonly Dictionary<ItemPickup, RectTransform> activeDots = new Dictionary<ItemPickup, RectTransform>();
        private readonly Dictionary<ResourceNode, RectTransform> activeHarvestDots = new Dictionary<ResourceNode, RectTransform>();
        private readonly Stack<RectTransform> dotPool = new Stack<RectTransform>();

        private readonly List<ItemPickup> stalePickupScratch = new List<ItemPickup>(8);
        private readonly List<ResourceNode> staleHarvestScratch = new List<ResourceNode>(8);

        private RectTransform dotLayer;
        private Canvas rootCanvas;
        private Camera worldCamera;
        private Transform playerTransform;
        private PlayerController cachedPlayer;
        private float proximityRadiusSqr;

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
            proximityRadiusSqr = proximityRadius * proximityRadius;
            BuildDotLayer();
        }

        private void Start()
        {
            ItemPickup[] existingPickups = SceneComponentCache.GetAll<ItemPickup>(FindObjectsInactive.Exclude, refreshInterval: 0.05f);
            for (int i = 0; i < existingPickups.Length; i++)
                trackedPickups.Add(existingPickups[i]);

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
            PruneInvalidHarvestNodes();
            CleanupOrphanedDots();
            CleanupOrphanedHarvestDots();

            foreach (ItemPickup pickup in trackedPickups)
            {
                if (ShouldShowDot(pickup))
                    ShowDot(pickup);
                else
                    HideDot(pickup);
            }

            foreach (ResourceNode node in trackedHarvestNodes)
            {
                if (ShouldShowHarvestDot(node))
                    ShowHarvestDot(node);
                else
                    HideHarvestDot(node);
            }

            CleanupOrphanedDots();
            CleanupOrphanedHarvestDots();
        }

        private static bool IsPickupTrackable(ItemPickup pickup)
        {
            return WorldUseController.IsCollectiblePickup(pickup);
        }

        private void PruneInvalidPickups()
        {
            trackedPickups.RemoveWhere(pickup => pickup == null);
        }

        private void PruneInvalidHarvestNodes()
        {
            trackedHarvestNodes.RemoveWhere(node =>
                node == null || node.interactionMode != ResourceNodeInteractionMode.HoldHarvest);
        }

        private void CleanupOrphanedDots()
        {
            if (activeDots.Count == 0)
                return;

            stalePickupScratch.Clear();
            foreach (KeyValuePair<ItemPickup, RectTransform> pair in activeDots)
            {
                ItemPickup pickup = pair.Key;
                if (IsPickupTrackable(pickup) && !pickup.IsPickedUp)
                    continue;

                stalePickupScratch.Add(pickup);
            }

            for (int i = 0; i < stalePickupScratch.Count; i++)
            {
                ItemPickup key = stalePickupScratch[i];
                if (activeDots.TryGetValue(key, out RectTransform dotRect))
                    ReleaseDotRect(dotRect);

                activeDots.Remove(key);
            }
        }

        private void CleanupOrphanedHarvestDots()
        {
            if (activeHarvestDots.Count == 0)
                return;

            staleHarvestScratch.Clear();
            foreach (KeyValuePair<ResourceNode, RectTransform> pair in activeHarvestDots)
            {
                ResourceNode node = pair.Key;
                if (node != null
                    && node.interactionMode == ResourceNodeInteractionMode.HoldHarvest
                    && node.resourceItem != null)
                {
                    continue;
                }

                staleHarvestScratch.Add(node);
            }

            for (int i = 0; i < staleHarvestScratch.Count; i++)
            {
                ResourceNode key = staleHarvestScratch[i];
                if (activeHarvestDots.TryGetValue(key, out RectTransform dotRect))
                    ReleaseDotRect(dotRect);

                activeHarvestDots.Remove(key);
            }
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

        private bool ShouldShowDot(ItemPickup pickup)
        {
            if (pickup == null || pickup.IsPickedUp || pickup.itemData == null)
                return false;

            return (pickup.transform.position - playerTransform.position).sqrMagnitude <= proximityRadiusSqr;
        }

        private bool ShouldShowHarvestDot(ResourceNode node)
        {
            if (node == null
                || node.interactionMode != ResourceNodeInteractionMode.HoldHarvest
                || node.resourceItem == null
                || node.IsHoldActive)
            {
                return false;
            }

            return (node.GetNodeCenter() - playerTransform.position).sqrMagnitude <= proximityRadiusSqr;
        }

        private void ShowDot(ItemPickup pickup)
        {
            if (pickup == null)
                return;

            if (!activeDots.TryGetValue(pickup, out RectTransform dotRect))
            {
                dotRect = AcquireDot(pickup.itemData.itemType);
                activeDots[pickup] = dotRect;
            }

            PositionDot(dotRect, pickup.transform.position);
        }

        private void ShowHarvestDot(ResourceNode node)
        {
            if (node == null || node.resourceItem == null)
                return;

            if (!activeHarvestDots.TryGetValue(node, out RectTransform dotRect))
            {
                dotRect = AcquireDot(node.resourceItem.itemType);
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

            foreach (KeyValuePair<ResourceNode, RectTransform> pair in activeHarvestDots)
                ReleaseDotRect(pair.Value);

            activeDots.Clear();
            activeHarvestDots.Clear();
            trackedPickups.RemoveWhere(pickup => pickup == null);
            trackedHarvestNodes.RemoveWhere(node => node == null);
        }

        private void ReleaseDotRect(RectTransform dotRect)
        {
            if (dotRect == null)
                return;

            dotRect.gameObject.SetActive(false);
            dotPool.Push(dotRect);
        }

        private RectTransform AcquireDot(ItemType itemType)
        {
            RectTransform dotRect = dotPool.Count > 0 ? dotPool.Pop() : ProximityDotStyle.CreateDotWidget(dotLayer);
            ProximityDotStyle.ApplyColor(dotRect, ProximityDotStyle.PickupColor(itemType));
            dotRect.gameObject.SetActive(true);
            return dotRect;
        }

        private void BuildDotLayer()
        {
            GameObject layerObject = new GameObject("PickupProximityDots", typeof(RectTransform));
            layerObject.transform.SetParent(transform, false);

            dotLayer = layerObject.GetComponent<RectTransform>();
            dotLayer.anchorMin = Vector2.zero;
            dotLayer.anchorMax = Vector2.one;
            dotLayer.offsetMin = Vector2.zero;
            dotLayer.offsetMax = Vector2.zero;
            dotLayer.SetAsFirstSibling();
        }
    }
}

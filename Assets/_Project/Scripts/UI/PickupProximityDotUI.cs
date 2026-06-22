using System.Collections.Generic;
using Project.Core;
using Project.Data;
using Project.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Shows a small on-screen dot for nearby pickup items within range.
    /// </summary>
    public class PickupProximityDotUI : MonoBehaviour
    {
        public static PickupProximityDotUI Instance { get; private set; }

        [SerializeField] private float proximityRadius = 2f;
        [SerializeField] private float dotSize = 10f;
        [SerializeField] private float glowSizeMultiplier = 2.35f;
        [SerializeField] private float glowAlpha = 0.5f;
        [SerializeField] private float verticalWorldOffset = 0.45f;

        private readonly HashSet<ItemPickup> trackedPickups = new HashSet<ItemPickup>();
        private readonly Dictionary<ItemPickup, RectTransform> activeDots = new Dictionary<ItemPickup, RectTransform>();
        private readonly Stack<RectTransform> dotPool = new Stack<RectTransform>();

        private RectTransform dotLayer;
        private Canvas rootCanvas;
        private Camera worldCamera;
        private Transform playerTransform;

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

        private void Awake()
        {
            Instance = this;
            BuildDotLayer();
        }

        private void Start()
        {
            ItemPickup[] existingPickups = FindObjectsByType<ItemPickup>(FindObjectsInactive.Exclude);
            for (int i = 0; i < existingPickups.Length; i++)
                trackedPickups.Add(existingPickups[i]);
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

            if (!ResolveReferences())
                return;

            PruneInvalidPickups();
            CleanupOrphanedDots();

            foreach (ItemPickup pickup in trackedPickups)
            {
                if (ShouldShowDot(pickup))
                    ShowDot(pickup);
                else
                    HideDot(pickup);
            }

            CleanupOrphanedDots();
        }

        private static bool IsPickupTrackable(ItemPickup pickup)
        {
            return WorldUseController.IsCollectiblePickup(pickup);
        }

        private void PruneInvalidPickups()
        {
            trackedPickups.RemoveWhere(pickup => pickup == null);
        }

        private void CleanupOrphanedDots()
        {
            if (activeDots.Count == 0)
                return;

            List<ItemPickup> staleKeys = null;
            foreach (KeyValuePair<ItemPickup, RectTransform> pair in activeDots)
            {
                ItemPickup pickup = pair.Key;
                if (IsPickupTrackable(pickup) && !pickup.IsPickedUp)
                    continue;

                staleKeys ??= new List<ItemPickup>();
                staleKeys.Add(pickup);
            }

            if (staleKeys == null)
                return;

            for (int i = 0; i < staleKeys.Count; i++)
            {
                ItemPickup key = staleKeys[i];
                if (activeDots.TryGetValue(key, out RectTransform dotRect))
                    ReleaseDotRect(dotRect);

                activeDots.Remove(key);
            }
        }

        private bool ResolveReferences()
        {
            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            if (worldCamera == null)
                worldCamera = Camera.main;

            if (playerTransform == null)
            {
                GameObject player = PlayerLocator.FindPlayerObject();
                if (player != null)
                    playerTransform = player.transform;
            }

            return rootCanvas != null && worldCamera != null && playerTransform != null;
        }

        private bool ShouldShowDot(ItemPickup pickup)
        {
            if (pickup == null || pickup.IsPickedUp || pickup.itemData == null)
                return false;

            Vector3 pickupPosition = pickup.transform.position;
            float distance = Vector3.Distance(playerTransform.position, pickupPosition);
            return distance <= proximityRadius;
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

            Vector3 worldPoint = pickup.transform.position + Vector3.up * verticalWorldOffset;
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

        private void HideAllDots()
        {
            foreach (KeyValuePair<ItemPickup, RectTransform> pair in activeDots)
                ReleaseDotRect(pair.Value);

            activeDots.Clear();
            trackedPickups.RemoveWhere(pickup => pickup == null);
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
            RectTransform dotRect;
            if (dotPool.Count > 0)
            {
                dotRect = dotPool.Pop();
                ApplyDotStyle(dotRect, itemType);
            }
            else
            {
                dotRect = CreateDotWidget();
                ApplyDotStyle(dotRect, itemType);
            }

            dotRect.gameObject.SetActive(true);
            return dotRect;
        }

        private RectTransform CreateDotWidget()
        {
            GameObject dotObject = new GameObject("PickupDot", typeof(RectTransform));
            dotObject.transform.SetParent(dotLayer, false);

            RectTransform dotRect = dotObject.GetComponent<RectTransform>();
            dotRect.sizeDelta = new Vector2(dotSize * glowSizeMultiplier, dotSize * glowSizeMultiplier);

            GameObject glowObject = new GameObject("Glow", typeof(RectTransform));
            glowObject.transform.SetParent(dotObject.transform, false);
            RectTransform glowRect = glowObject.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;

            Image glowImage = glowObject.AddComponent<Image>();
            glowImage.sprite = ShiftUiTheme.CircleGlow ?? MapUiSprites.Dot;
            glowImage.color = BuildGlowColor(MapUiSprites.GetResourceColor(ItemType.Resource));
            glowImage.raycastTarget = false;
            glowImage.preserveAspect = true;

            GameObject coreObject = new GameObject("Core", typeof(RectTransform));
            coreObject.transform.SetParent(dotObject.transform, false);
            RectTransform coreRect = coreObject.GetComponent<RectTransform>();
            coreRect.anchorMin = new Vector2(0.5f, 0.5f);
            coreRect.anchorMax = new Vector2(0.5f, 0.5f);
            coreRect.pivot = new Vector2(0.5f, 0.5f);
            coreRect.anchoredPosition = Vector2.zero;
            coreRect.sizeDelta = new Vector2(dotSize, dotSize);

            Image coreImage = coreObject.AddComponent<Image>();
            coreImage.sprite = ShiftUiTheme.CircleFilled ?? MapUiSprites.Dot;
            coreImage.raycastTarget = false;
            coreImage.preserveAspect = true;

            return dotRect;
        }

        private void ApplyDotStyle(RectTransform dotRect, ItemType itemType)
        {
            if (dotRect == null)
                return;

            dotRect.sizeDelta = new Vector2(dotSize * glowSizeMultiplier, dotSize * glowSizeMultiplier);

            Color color = MapUiSprites.GetResourceColor(itemType);

            Transform glow = dotRect.Find("Glow");
            if (glow != null && glow.TryGetComponent<Image>(out Image glowImage))
            {
                glowImage.sprite = ShiftUiTheme.CircleGlow ?? MapUiSprites.Dot;
                glowImage.color = BuildGlowColor(color);
            }

            Transform core = dotRect.Find("Core");
            if (core != null && core.TryGetComponent<Image>(out Image coreImage))
            {
                coreImage.sprite = ShiftUiTheme.CircleFilled ?? MapUiSprites.Dot;
                coreImage.color = color;
            }
        }

        private Color BuildGlowColor(Color baseColor)
        {
            baseColor.a = glowAlpha;
            return baseColor;
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

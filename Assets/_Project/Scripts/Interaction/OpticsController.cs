using System.Collections.Generic;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Map;
using Project.Player;
using Project.Survival;
using Project.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Interaction
{
    [RequireComponent(typeof(EquipmentController))]
    public class OpticsController : MonoBehaviour
    {
        [SerializeField] private LayerMask scanLayers = ~0;
        [SerializeField] private float scanRefreshInterval = 0.15f;
        [SerializeField] private float scannerViewportHalfWidthPixels = 420f;
        [SerializeField] private float scannerViewportHalfHeightPixels = 250f;
        [SerializeField] private float scrollZoomRatioPerNotch = 1.28f;
        [SerializeField] private float scrollWheelUnitsPerNotch = 120f;
        [SerializeField] private float scrollZoomExponentScale = 2.4f;
        [SerializeField] private float scrollMomentumGain = 1.8f;
        [SerializeField] private float scrollMomentumAcceleration = 2.2f;
        [SerializeField] private float maxScrollMomentum = 5f;
        [SerializeField] private float scrollMomentumDecay = 10f;
        [SerializeField] private float minScrollNotchStep = 0.18f;

        private EquipmentController equipment;
        private PlayerController playerController;
        private EquippedItemVisual equippedVisual;
        private OpticsOverlayUI overlayUi;
        private OpticsCameraRig cameraRig;
        private ScannerWorldHighlight worldHighlight;
        private Camera worldCamera;
        private readonly List<OpticsScanTarget> scanResults = new List<OpticsScanTarget>(24);
        private readonly HashSet<int> scanResultKeys = new HashSet<int>();
        private float nextScanTime;
        private float scrollMomentum;
        private int activeSinceFrame = -1;
        private bool isActive;

        public bool IsActive => isActive;

        public bool ShouldSuppressBlockInput =>
            isActive || (equipment != null && equipment.HasOpticsToolSelected());

        private void Awake()
        {
            equipment = GetComponent<EquipmentController>();
            playerController = GetComponent<PlayerController>();
            equippedVisual = GetComponent<EquippedItemVisual>();
            cameraRig = GetComponent<OpticsCameraRig>();
            if (cameraRig == null)
                cameraRig = gameObject.AddComponent<OpticsCameraRig>();

            worldHighlight = GetComponent<ScannerWorldHighlight>();
            if (worldHighlight == null)
                worldHighlight = gameObject.AddComponent<ScannerWorldHighlight>();
        }

        private void OnEnable()
        {
            if (equipment != null)
            {
                equipment.OnToolbarSelectionChanged += HandleToolSelectionChanged;
                equipment.OnSelectedHotbarChanged += HandleHotbarChanged;
            }
        }

        private void OnDisable()
        {
            if (equipment != null)
            {
                equipment.OnToolbarSelectionChanged -= HandleToolSelectionChanged;
                equipment.OnSelectedHotbarChanged -= HandleHotbarChanged;
            }

            ForceDeactivate();
        }

        private void Start()
        {
            worldCamera = playerController != null ? playerController.GameplayCamera : null;
            if (worldCamera == null)
                worldCamera = Camera.main;

            if (cameraRig != null && playerController != null)
                cameraRig.Initialize(playerController, worldCamera);

            EnsureOverlayUi();
        }

        private void Update()
        {
            if (!CanOperate())
            {
                if (isActive || IsOpticsDisplayStuck())
                    ForceDeactivate();
                return;
            }

            if (isActive)
            {
                ItemData activeTool = equipment.ActiveToolItem;
                if (activeTool == null || !activeTool.IsOpticsTool || !equipment.HasOpticsToolSelected())
                {
                    ForceDeactivate();
                    return;
                }

                if (Time.frameCount > activeSinceFrame + 1 &&
                    (overlayUi == null || !overlayUi.IsBuilt || !overlayUi.IsVisible ||
                     cameraRig == null || !cameraRig.HasValidOutput))
                {
                    ForceDeactivate();
                    return;
                }
            }

            if (isActive && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ForceDeactivate();
                return;
            }

            if (!isActive)
                return;

            ItemData tool = equipment.ActiveToolItem;
            if (tool == null || !tool.IsOpticsTool)
            {
                ForceDeactivate();
                return;
            }

            if (tool.toolType == ToolType.Scanner && Time.unscaledTime >= nextScanTime)
            {
                nextScanTime = Time.unscaledTime + scanRefreshInterval;
                RefreshScannerTargets(tool);
            }

            if (Mouse.current != null)
                HandleOpticsScrollZoom(tool);
            else
                DecayScrollMomentum();
        }

        private void HandleOpticsScrollZoom(ItemData tool)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float rawNotches = scroll / scrollWheelUnitsPerNotch;
                float signedNotches = Mathf.Sign(rawNotches) * Mathf.Max(Mathf.Abs(rawNotches), minScrollNotchStep);

                scrollMomentum = Mathf.Min(
                    maxScrollMomentum,
                    scrollMomentum + Mathf.Abs(signedNotches) * scrollMomentumGain);

                float acceleration = 1f + scrollMomentum * scrollMomentumAcceleration;
                float effectiveNotches = signedNotches * scrollZoomExponentScale * acceleration;

                float zoomFov = ApplyExponentialOpticsZoom(
                    playerController.OpticsTargetFov,
                    tool.opticsMinZoomFov,
                    tool.opticsMaxZoomFov,
                    effectiveNotches);

                playerController.SnapOpticsZoom(zoomFov);
                cameraRig?.SetFieldOfView(zoomFov);
                return;
            }

            DecayScrollMomentum();
        }

        private void DecayScrollMomentum()
        {
            if (scrollMomentum <= 0f)
                return;

            scrollMomentum = Mathf.MoveTowards(scrollMomentum, 0f, scrollMomentumDecay * Time.deltaTime);
        }

        private float ApplyExponentialOpticsZoom(
            float currentFov,
            float minFov,
            float maxFov,
            float notches)
        {
            if (maxFov <= minFov + 0.001f || Mathf.Abs(notches) < 0.001f)
                return currentFov;

            float clampedFov = Mathf.Clamp(currentFov, minFov, maxFov);
            float zoomMultiplier = maxFov / clampedFov;
            float maxMultiplier = maxFov / minFov;

            zoomMultiplier *= Mathf.Pow(scrollZoomRatioPerNotch, notches);
            zoomMultiplier = Mathf.Clamp(zoomMultiplier, 1f, maxMultiplier);

            return maxFov / zoomMultiplier;
        }

        public bool TryHandleBlockInput(InputAction.CallbackContext context)
        {
            if (!context.started || !CanOperate() || equipment == null)
                return false;

            if (playerController != null &&
                (playerController.IsInventoryOpen || playerController.IsMapOpen))
                return false;

            if (UiInputGuard.ShouldBlockOpticsActivation)
                return false;

            if (!equipment.HasOpticsToolSelected())
                return false;

            if (isActive)
                ForceDeactivate();
            else
                TryActivate();

            return true;
        }

        public void Toggle()
        {
            if (isActive)
                ForceDeactivate();
            else
                TryActivate();
        }

        public void HandleToolHotkey(ToolType toolType)
        {
            if (!CanOperate() || equipment == null)
                return;

            if (playerController != null &&
                (playerController.IsInventoryOpen || playerController.IsMapOpen))
                return;

            if (UiInputGuard.ShouldBlockOpticsActivation)
                return;

            int toolbarSlot = toolType == ToolType.Scanner
                ? equipment.ScannerToolbarSlot
                : equipment.BinocularsToolbarSlot;

            bool wasUsingThisTool = isActive &&
                                    equipment.ActiveToolItem != null &&
                                    equipment.ActiveToolItem.toolType == toolType;

            if (wasUsingThisTool)
            {
                ForceDeactivate();
                equipment.SelectToolbarSlot(toolbarSlot, allowToggleOff: true);
                return;
            }

            if (!equipment.TryEnsureToolbarTool(toolType, out _))
                return;

            ItemData tool = equipment.ActiveToolItem;
            if (tool == null || !tool.IsOpticsTool)
                return;

            TryActivate();
        }

        private bool TryActivate()
        {
            ItemData tool = equipment?.ActiveToolItem;
            if (tool == null || !tool.IsOpticsTool || playerController == null)
                return false;

            if (playerController.IsInventoryOpen || playerController.IsMapOpen)
                return false;

            if (!EnsureOverlayUi())
                return false;

            worldCamera = playerController.GameplayCamera != null ? playerController.GameplayCamera : Camera.main;
            if (worldCamera == null)
                return false;

            if (cameraRig != null)
                cameraRig.Initialize(playerController, worldCamera);

            if (cameraRig == null || !cameraRig.EnsureOutputReady())
                return false;

            float zoomFov = Mathf.Clamp(tool.opticsZoomFov, tool.opticsMinZoomFov, tool.opticsMaxZoomFov);

            if (!cameraRig.Activate(tool.toolType))
            {
                cameraRig.Deactivate();
                return false;
            }

            RenderTexture renderTexture = cameraRig.RenderTexture;
            if (renderTexture == null || !cameraRig.HasValidOutput)
            {
                cameraRig.Deactivate();
                return false;
            }

            overlayUi.BindRenderTexture(renderTexture);
            overlayUi.SetVisible(true, tool.toolType);

            if (!overlayUi.IsVisible)
            {
                cameraRig.Deactivate();
                overlayUi.SetVisible(false, ToolType.None);
                return false;
            }

            isActive = true;
            activeSinceFrame = Time.frameCount;
            playerController.SetOpticsOpen(true, zoomFov);
            cameraRig.SetFieldOfView(zoomFov);
            worldHighlight?.SetActive(tool.toolType == ToolType.Scanner);
            if (tool.toolType != ToolType.Scanner)
            {
                overlayUi.ClearScannerMarkers();
                worldHighlight?.Clear();
            }

            equippedVisual?.ForceRefresh();
            return true;
        }

        public void CloseOpticsIfActive()
        {
            ForceDeactivate();
        }

        private void ForceDeactivate()
        {
            bool needsCleanup = isActive
                || (playerController != null && playerController.IsOpticsOpen)
                || (cameraRig != null && cameraRig.IsMainCameraBlackedOut);

            if (!needsCleanup)
                return;

            isActive = false;
            activeSinceFrame = -1;
            scrollMomentum = 0f;
            scanResults.Clear();
            scanResultKeys.Clear();
            playerController?.SetOpticsOpen(false);
            cameraRig?.Deactivate();
            cameraRig?.ForceRestoreMainCamera();
            overlayUi?.BindRenderTexture(null);
            overlayUi?.ClearScannerMarkers();
            overlayUi?.SetVisible(false, ToolType.None);
            worldHighlight?.SetActive(false);
            worldHighlight?.Clear();
            equippedVisual?.ForceRefresh();
            playerController?.RefreshCameraFollow();

            worldCamera = playerController != null ? playerController.GameplayCamera : Camera.main;
        }

        private bool IsOpticsDisplayStuck()
        {
            return (cameraRig != null && cameraRig.IsMainCameraBlackedOut)
                || (playerController != null && playerController.IsOpticsOpen);
        }

        private void HandleToolSelectionChanged()
        {
            HandleSelectionChanged();
        }

        private void HandleHotbarChanged(int _)
        {
            HandleSelectionChanged();
        }

        private void HandleSelectionChanged()
        {
            if (!isActive)
            {
                equippedVisual?.ForceRefresh();
                return;
            }

            ItemData tool = equipment?.ActiveToolItem;
            if (tool == null || !tool.IsOpticsTool || !equipment.HasOpticsToolSelected())
            {
                ForceDeactivate();
                return;
            }

            float zoomFov = Mathf.Clamp(tool.opticsZoomFov, tool.opticsMinZoomFov, tool.opticsMaxZoomFov);
            playerController?.SetOpticsZoomFov(zoomFov);
            cameraRig?.SetFieldOfView(zoomFov);
            overlayUi?.SetVisible(true, tool.toolType);
            worldHighlight?.SetActive(tool.toolType == ToolType.Scanner);
            if (tool.toolType != ToolType.Scanner)
            {
                overlayUi?.ClearScannerMarkers();
                worldHighlight?.Clear();
            }

            equippedVisual?.ForceRefresh();
        }

        private void RefreshScannerTargets(ItemData tool)
        {
            if (overlayUi == null)
                return;

            if (worldCamera == null && cameraRig != null && cameraRig.IsActive)
                worldCamera = cameraRig.OpticsCamera;

            if (worldCamera == null)
                worldCamera = Camera.main;

            if (worldCamera == null)
                return;

            scanResults.Clear();
            scanResultKeys.Clear();
            float scanRange = Mathf.Max(4f, tool.scanRange);
            Vector3 origin = worldCamera.transform.position;

            ScanPhysicsTargets(origin, scanRange);
            ScanMapMarkers(origin, scanRange);
            ScanScannableComponents(origin, scanRange);

            overlayUi.UpdateScannerMarkers(
                worldCamera,
                scanResults,
                scannerViewportHalfWidthPixels,
                scannerViewportHalfHeightPixels);
            worldHighlight?.UpdateHighlights(scanResults, worldCamera);
        }

        private void ScanPhysicsTargets(Vector3 origin, float scanRange)
        {
            Collider[] hits = Physics.OverlapSphere(origin, scanRange, scanLayers, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length && scanResults.Count < 24; i++)
            {
                Collider hit = hits[i];
                if (hit == null || hit.transform.IsChildOf(transform))
                    continue;

                if (!TryBuildScanTarget(hit, out OpticsScanTarget target))
                    continue;

                if (!HasLineOfSight(origin, target.WorldPosition))
                    continue;

                TryAddScanResult(target);
            }
        }

        private void ScanMapMarkers(Vector3 origin, float scanRange)
        {
            IReadOnlyList<MapMarker> markers = MapRegistry.ActiveMarkers;
            for (int i = 0; i < markers.Count && scanResults.Count < 24; i++)
            {
                MapMarker marker = markers[i];
                if (marker == null)
                    continue;

                float distance = Vector3.Distance(origin, marker.WorldPosition);
                if (distance > scanRange)
                    continue;

                if (!HasLineOfSight(origin, marker.WorldPosition))
                    continue;

                TryAddScanResult(new OpticsScanTarget(marker.WorldPosition, marker.Label, marker.Color));
            }
        }

        private void ScanScannableComponents(Vector3 origin, float scanRange)
        {
            ScannableTarget[] scannables = FindObjectsByType<ScannableTarget>();
            for (int i = 0; i < scannables.Length && scanResults.Count < 24; i++)
            {
                ScannableTarget scannable = scannables[i];
                if (scannable == null || scannable.transform.IsChildOf(transform))
                    continue;

                float distance = Vector3.Distance(origin, scannable.ScanPosition);
                if (distance > scanRange)
                    continue;

                if (scannable.RequiresLineOfSight && !HasLineOfSight(origin, scannable.ScanPosition))
                    continue;

                TryAddScanResult(new OpticsScanTarget(scannable.ScanPosition, scannable.ScanLabel, scannable.ScanColor));
            }
        }

        private void TryAddScanResult(OpticsScanTarget target)
        {
            int key = BuildScanKey(target.WorldPosition, target.Label);
            if (!scanResultKeys.Add(key))
                return;

            scanResults.Add(target);
        }

        private static int BuildScanKey(Vector3 position, string label)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Mathf.RoundToInt(position.x * 4f);
                hash = hash * 31 + Mathf.RoundToInt(position.y * 4f);
                hash = hash * 31 + Mathf.RoundToInt(position.z * 4f);
                hash = hash * 31 + (label != null ? label.GetHashCode() : 0);
                return hash;
            }
        }

        private static bool TryBuildScanTarget(Collider collider, out OpticsScanTarget target)
        {
            target = default;

            ScannableTarget scannable = collider.GetComponentInParent<ScannableTarget>();
            if (scannable != null)
            {
                target = new OpticsScanTarget(scannable.ScanPosition, scannable.ScanLabel, scannable.ScanColor);
                return true;
            }

            ResourceNode resourceNode = collider.GetComponentInParent<ResourceNode>();
            if (resourceNode != null && resourceNode.resourceItem != null)
            {
                ItemData item = resourceNode.resourceItem;
                target = new OpticsScanTarget(
                    resourceNode.transform.position,
                    item.itemName,
                    MapUiSprites.GetResourceColor(item.itemType));
                return true;
            }

            MapMarker mapMarker = collider.GetComponentInParent<MapMarker>();
            if (mapMarker != null)
            {
                target = new OpticsScanTarget(mapMarker.WorldPosition, mapMarker.Label, mapMarker.Color);
                return true;
            }

            ItemPickup pickup = collider.GetComponentInParent<ItemPickup>();
            if (pickup != null && !pickup.IsPickedUp)
            {
                string label = pickup.itemData != null ? pickup.itemData.itemName : "Pickup";
                target = new OpticsScanTarget(pickup.transform.position, label, new Color(0.95f, 0.85f, 0.35f, 1f));
                return true;
            }

            return false;
        }

        private bool HasLineOfSight(Vector3 origin, Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - origin;
            float distance = direction.magnitude;
            if (distance <= 0.05f)
                return true;

            return !Physics.Raycast(origin, direction / distance, distance, scanLayers, QueryTriggerInteraction.Ignore);
        }

        private bool CanOperate()
        {
            if (!GameSession.HasStarted || playerController == null || playerController.IsGameplayPaused)
                return false;

            SurvivalStats survivalStats = GetComponent<SurvivalStats>();
            return survivalStats == null || !survivalStats.IsDead;
        }

        private bool EnsureOverlayUi()
        {
            if (overlayUi != null && overlayUi.IsBuilt)
                return true;

            Canvas canvas = MainMenuController.ResolveMainCanvas();
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();

            if (canvas == null)
                return false;

            if (canvas.transform.localScale.sqrMagnitude < 0.001f)
                canvas.transform.localScale = Vector3.one;

            UIManager uiManager = canvas.GetComponent<UIManager>();
            Transform canvasRoot = uiManager != null ? uiManager.transform : canvas.transform;
            overlayUi = canvasRoot.GetComponent<OpticsOverlayUI>();
            if (overlayUi == null)
                overlayUi = canvasRoot.gameObject.AddComponent<OpticsOverlayUI>();

            overlayUi.EnsureBuilt(canvasRoot);
            return overlayUi.IsBuilt;
        }
    }
}

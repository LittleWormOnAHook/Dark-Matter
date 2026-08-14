using Project.Core;
using Project.Survival.Exposure;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Environment status HUD pinned to the bottom-left screen edge: a standalone Temperature panel
    /// and a standalone Hazards panel, side by side, each with its own hovercraft-style chrome
    /// (dark navy background + fuchsia trim). Temperature keeps its needle/gradient-tube gauge;
    /// Hazards lists all six zone types, each row restyled to match the Hovercraft status HUD
    /// (icon badge + name + percentage + segmented block bar) via VehicleStatSegmentBar. Both
    /// widgets size themselves internally (see VerticalThermalNeedleGauge / VerticalHazardExposureGauge)
    /// — this class just places them side by side and forwards visibility/refresh calls.
    /// </summary>
    public class HotbarExposureGaugeCluster : MonoBehaviour
    {
        /// <summary>Gap between the Temperature panel and the Hazards panel.</summary>
        public const float ClusterGap = 12f * HudLayoutMetrics.HudScale;

        /// <summary>Screen-edge inset for bottom-left placement (literal pixels).</summary>
        public const float ScreenEdgeGap = 30f;

        [SerializeField] private HazardHudIconSet hazardIconSet;

        private RectTransform containerRoot;
        private RectTransform thermalRect;
        private VerticalThermalNeedleGauge thermalGauge;
        private RectTransform hazardRect;
        private VerticalHazardExposureGauge hazardGauge;
        private ExposureZoneEntryBannerUI zoneEntryBanner;
        private Transform clusterOriginalParent;
        private int clusterOriginalSiblingIndex;
        private bool uiBuilt;
        private bool raisedToFrontLayer;
        private ExposureReceiver cachedZoneReceiver;
        private bool zoneBannerBound;

        public bool IsBuilt => uiBuilt;

        public float GetClusterWidth()
        {
            float width = thermalRect != null ? thermalRect.sizeDelta.x : 0f;
            if (hazardRect != null)
                width += (width > 0f ? ClusterGap : 0f) + hazardRect.sizeDelta.x;
            return width;
        }

        public float GetClusterHeight()
        {
            float thermalHeight = thermalRect != null ? thermalRect.sizeDelta.y : 0f;
            float hazardHeight = hazardRect != null ? hazardRect.sizeDelta.y : 0f;
            return Mathf.Max(thermalHeight, hazardHeight);
        }

        public void EnsureBuilt(Transform layoutParent, float anchoredY)
        {
            if (uiBuilt || layoutParent == null)
                return;

            Transform canvasRoot = layoutParent;
            while (canvasRoot.parent != null)
                canvasRoot = canvasRoot.parent;

            containerRoot = new GameObject("EnvironmentStatusHud", typeof(RectTransform), typeof(HorizontalLayoutGroup))
                .GetComponent<RectTransform>();
            containerRoot.SetParent(layoutParent, false);

            HorizontalLayoutGroup group = containerRoot.GetComponent<HorizontalLayoutGroup>();
            group.spacing = ClusterGap;
            group.childAlignment = TextAnchor.LowerLeft;
            group.childControlWidth = false;
            group.childControlHeight = false;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;

            GameObject thermalObject = new GameObject("ThermalGauge", typeof(RectTransform), typeof(VerticalThermalNeedleGauge));
            thermalObject.transform.SetParent(containerRoot, false);
            thermalRect = thermalObject.GetComponent<RectTransform>();
            thermalGauge = thermalObject.GetComponent<VerticalThermalNeedleGauge>();
            thermalGauge.Configure(compact: false);

            GameObject hazardObject = new GameObject("HazardGauge", typeof(RectTransform), typeof(VerticalHazardExposureGauge));
            hazardObject.transform.SetParent(containerRoot, false);
            hazardRect = hazardObject.GetComponent<RectTransform>();
            hazardGauge = hazardObject.GetComponent<VerticalHazardExposureGauge>();
            HazardHudIconSet icons = hazardIconSet != null ? hazardIconSet : HazardHudIconSet.LoadDefault();
            hazardGauge.Configure(compact: false, icons, suppressOwnPanelChrome: false, enableAutoHide: true);

            Canvas.ForceUpdateCanvases();
            AlignToScreenBottomLeft();

            EnsureZoneEntryBanner(canvasRoot);
            BindZoneEntryBanner();

            ExposureStatusService service = ExposureStatusService.Instance;
            if (service != null)
                service.OnSnapshotChanged += HandleSnapshotChanged;

            uiBuilt = true;
            SetGameplayVisible(!MainMenuController.BlocksGameplayHud);
            Refresh(ExposureStatusService.Current);
        }

        private void Update()
        {
            if (!uiBuilt || zoneBannerBound)
                return;

            BindZoneEntryBanner();
        }

        private void BindZoneEntryBanner()
        {
            if (zoneEntryBanner == null)
                return;

            if (cachedZoneReceiver == null)
            {
                ExposureStatusService service = ExposureStatusService.Instance;
                cachedZoneReceiver = service != null
                    ? service.GetComponent<ExposureController>() ?? service.GetComponent<ExposureReceiver>()
                    : null;
            }

            if (cachedZoneReceiver == null)
                return;

            zoneEntryBanner.BindReceiver(cachedZoneReceiver);
            zoneBannerBound = true;
        }

        private void EnsureZoneEntryBanner(Transform canvasRoot)
        {
            if (zoneEntryBanner != null)
            {
                zoneEntryBanner.EnsureBuilt(canvasRoot);
                return;
            }

            GameObject bannerObject = new GameObject(
                "ExposureZoneEntryBanner",
                typeof(RectTransform),
                typeof(ExposureZoneEntryBannerUI));
            bannerObject.transform.SetParent(canvasRoot, false);
            zoneEntryBanner = bannerObject.GetComponent<ExposureZoneEntryBannerUI>();
            zoneEntryBanner.EnsureBuilt(canvasRoot);
        }

        private void OnDestroy()
        {
            ExposureStatusService service = ExposureStatusService.Instance;
            if (service != null)
                service.OnSnapshotChanged -= HandleSnapshotChanged;

            zoneEntryBanner?.UnbindReceiver();
        }

        public void SetGameplayVisible(bool visible)
        {
            if (MainMenuController.BlocksGameplayHud)
                visible = false;

            if (containerRoot != null)
                containerRoot.gameObject.SetActive(visible);

            if (zoneEntryBanner != null && !visible)
                zoneEntryBanner.gameObject.SetActive(false);
        }

        /// <summary>Pin cluster to bottom-left of screen with a fixed edge gap.</summary>
        public void AlignToScreenBottomLeft()
        {
            if (containerRoot == null)
                return;

            containerRoot.anchorMin = new Vector2(0f, 0f);
            containerRoot.anchorMax = new Vector2(0f, 0f);
            containerRoot.pivot = new Vector2(0f, 0f);
            containerRoot.anchoredPosition = new Vector2(ScreenEdgeGap, ScreenEdgeGap);
        }

        public void EnsureRaisedToFrontLayer(Transform canvasRoot)
        {
            if (containerRoot == null || canvasRoot == null)
                return;

            if (!raisedToFrontLayer)
            {
                clusterOriginalParent = containerRoot.parent;
                clusterOriginalSiblingIndex = containerRoot.GetSiblingIndex();
                raisedToFrontLayer = true;
            }

            UiFrontLayer.ReparentToFront(containerRoot, canvasRoot);
        }

        public void RestoreFromFrontLayer()
        {
            if (containerRoot == null || clusterOriginalParent == null)
                return;

            containerRoot.SetParent(clusterOriginalParent, true);
            containerRoot.SetSiblingIndex(Mathf.Clamp(clusterOriginalSiblingIndex, 0, clusterOriginalParent.childCount - 1));
            raisedToFrontLayer = false;
            AlignToScreenBottomLeft();
        }

        private void HandleSnapshotChanged(ExposureStatusSnapshot snapshot)
        {
            Refresh(snapshot);
        }

        private void Refresh(ExposureStatusSnapshot snapshot)
        {
            if (!uiBuilt)
                return;

            thermalGauge?.Refresh(snapshot);
            hazardGauge?.Refresh(snapshot);
        }
    }
}

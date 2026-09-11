using Project.Core;
using Project.Map;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    // UITK dual-run bridge: expose live minimap texture / crop / heading / chrome
    // without changing world-map bake, pan math, or marker registry behavior.
    public partial class MapUI
    {
        private int lastUitkSpanAdjustFrame = -1;

        public bool ShouldPresentMinimap =>
            GameSettings.MinimapEnabled && GameSession.HasStarted && !IsJournalOpen();

        public Texture MinimapSourceTexture
        {
            get
            {
                if (mapProvider == null)
                    EnsureMapProvider();

                if (mapProvider != null && mapProvider.MinimapTexture != null)
                    return mapProvider.MinimapTexture;

                if (mapProvider != null && mapProvider.MapTexture != null)
                    return mapProvider.MapTexture;

                if (minimapImage != null && minimapImage.texture != null)
                    return minimapImage.texture;

                return WorldMapProvider.LoadMinimapMapTexture() ?? ResolveMapTexture();
            }
        }

        public float MinimapFacingYaw => GetMapFacingYaw();

        public float MapDisplayYaw => GetMapDisplayYaw();

        public float MapCompassYaw => GetMapCompassYaw();

        /// <summary>Full / journal map arrow: character (or craft) facing, not look camera.</summary>
        public float MapPlayerCompassYaw => GetMapPlayerCompassYaw();

        public string MinimapInfoText =>
            minimapInfoLabel != null ? minimapInfoLabel.text : string.Empty;

        public bool HasMinimapPlayerPosition => HasMapWorldPosition();

        public Vector3 MinimapPlayerWorldPosition =>
            HasMapWorldPosition() ? GetMapWorldPosition() : Vector3.zero;

        public GameObject MinimapPanelObject => minimapRoot;

        public GameObject CompassHudObject =>
            compassHud != null ? compassHud.gameObject : transform.Find("CompassHud")?.gameObject;

        public GameObject InfoPanelObject => transform.Find("InfoPanel")?.gameObject;

        /// <summary>
        /// Player-centered UV crop matching the live uGUI circular viewport.
        /// Source is WorldMapProvider.MapTexture (Texture2D bake), not a camera RT.
        /// </summary>
        public bool TryGetMinimapViewParams(
            out Texture source,
            out Vector2 playerUv,
            out float uvSpan,
            out float facingYaw)
        {
            source = MinimapSourceTexture;
            playerUv = new Vector2(0.5f, 0.5f);
            uvSpan = 0.25f;
            facingYaw = GetMapDisplayYaw();

            if (source == null)
            {
                EnsureMapProvider();
                source = WorldMapProvider.CreateDisplayFallback();
            }

            if (source == null)
                return false;

            if (mapProvider == null)
                EnsureMapProvider();

            float worldSpan = ReferenceTerrainSpan;
            if (mapProvider != null)
            {
                if (HasMapWorldPosition())
                    playerUv = mapProvider.WorldToPlayerMap01(GetMapWorldPosition());

                worldSpan = Mathf.Max(mapProvider.WorldBounds.size.x, mapProvider.WorldBounds.size.z);
                float spanMeters = Mathf.Clamp(minimapWorldSpan, MinMinimapSpan, MaxMinimapSpan);
                uvSpan = spanMeters / Mathf.Max(1f, worldSpan);
            }

            float minUv = worldSpan > 0f ? MinMinimapSpan / worldSpan : 0.005f;
            uvSpan = Mathf.Clamp(uvSpan, minUv, 1f);
            return true;
        }

        public void UitkEnsureLegacyStartSpan()
        {
            if (minimapWorldSpan > DefaultMinimapWorldSpan)
            {
                minimapWorldSpan = DefaultMinimapWorldSpan;
                UpdateMinimapInfoPanel();
            }
        }

        public void UitkAdjustMinimapSpan(float multiplier)
        {
            if (Time.frameCount == lastUitkSpanAdjustFrame)
                return;
            lastUitkSpanAdjustFrame = Time.frameCount;

            autoScaleMinimapToTerrain = false;
            float step = (MaxMinimapSpan - MinMinimapSpan) / MinimapScrollNotchesFullRange;
            if (multiplier < 1f)
                minimapWorldSpan = Mathf.Clamp(minimapWorldSpan - step, MinMinimapSpan, MaxMinimapSpan);
            else
                minimapWorldSpan = Mathf.Clamp(minimapWorldSpan + step, MinMinimapSpan, MaxMinimapSpan);

            UpdateMinimapInfoPanel();
        }

        public void UitkMinimapScanClicked()
        {
            OnMinimapScanClicked();
        }

        internal void ApplyUitkMinimapUguiHide(bool hideGraphics)
        {
            if (minimapRoot != null)
            {
                CanvasGroup group = minimapRoot.GetComponent<CanvasGroup>();
                if (group == null)
                    group = minimapRoot.AddComponent<CanvasGroup>();
                group.alpha = hideGraphics ? 0f : 1f;
                group.blocksRaycasts = !hideGraphics;
                group.interactable = !hideGraphics;
            }

            Transform info = transform.Find("InfoPanel");
            if (info != null)
            {
                bool showInfo = !hideGraphics && ShouldPresentMinimap;
                if (info.gameObject.activeSelf != showInfo)
                    info.gameObject.SetActive(showInfo);
            }
        }
    }
}

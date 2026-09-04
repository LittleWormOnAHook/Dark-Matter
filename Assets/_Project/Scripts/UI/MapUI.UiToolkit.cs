using Project.Core;
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
                if (minimapImage != null && minimapImage.texture != null)
                    return minimapImage.texture;

                if (mapProvider != null && mapProvider.MapTexture != null)
                    return mapProvider.MapTexture;

                return ResolveMapTexture();
            }
        }

        public float MinimapFacingYaw => GetMapFacingYaw();

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
            facingYaw = GetMapFacingYaw();

            if (source == null)
                return false;

            if (mapProvider == null)
                EnsureMapProvider();

            if (mapProvider != null)
            {
                if (HasMapWorldPosition())
                    playerUv = mapProvider.WorldToMap01(GetMapWorldPosition());

                float world = Mathf.Max(mapProvider.WorldBounds.size.x, mapProvider.WorldBounds.size.z);
                float spanMeters = Mathf.Max(32f, minimapWorldSpan);
                uvSpan = spanMeters / Mathf.Max(1f, world);

                int texWidth = source.width;
                if (texWidth > 0)
                {
                    float viewportPx = DefaultMinimapSize;
                    float maxZoom = texWidth / Mathf.Max(48f, viewportPx * 0.35f);
                    float zoom = world / spanMeters;
                    zoom = Mathf.Min(zoom, Mathf.Max(1f, maxZoom));
                    uvSpan = 1f / Mathf.Max(1f, zoom);
                }
            }

            uvSpan = Mathf.Clamp(uvSpan, 0.02f, 1f);
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
            // ] zoom-out is capped at 5% past the default 96m span.
            if (Time.frameCount == lastUitkSpanAdjustFrame)
                return;
            lastUitkSpanAdjustFrame = Time.frameCount;

            float maxOut = DefaultMinimapWorldSpan * 1.05f;
            minimapWorldSpan = Mathf.Clamp(minimapWorldSpan * multiplier, MinMinimapSpan, maxOut);
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

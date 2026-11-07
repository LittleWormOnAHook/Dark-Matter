using Project.Core;
using Project.Map;
using Project.Vehicles;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    // UITK dual-run bridge: expose live minimap texture / crop / heading / chrome
    // without changing world-map bake, pan math, or marker registry behavior.
    public partial class MapUI
    {
        private int lastUitkSpanAdjustFrame = -1;

        private static int sharedMapGeneration;
        private static bool sharedMapSceneHooks;
        private int sharedPositionFrame = -1;
        private int sharedPositionGeneration = -1;
        private DMWorldScope.World sharedPositionWorld;
        private bool sharedPositionDualWorld;
        private bool sharedPositionMounted;
        private HovercraftController sharedPositionCraft;
        private Transform sharedPositionPlayer;
        private bool sharedHasPosition;
        private Vector3 sharedWorldPosition;
        private int sharedPositionRevision;
        private int sharedViewFrame = -1;
        private int sharedViewGeneration = -1;
        private int sharedViewPositionRevision = -1;
        private float sharedViewWorldSpan;
        private WorldMapProvider sharedViewProvider;
        private WorldMapProvider sharedViewSingleton;
        private Texture sharedViewSource;
        private Vector2 sharedViewUv;
        private float sharedViewSpan;
        private float sharedViewYaw;
        private bool sharedViewValid;
        private int sharedViewRevision;

        // Invalidates all MapUI instances, including disabled resident-world instances.
        public static void InvalidateSharedMinimapSnapshot() => sharedMapGeneration++;

        private static void EnsureSharedMapSceneHooks()
        {
            if (sharedMapSceneHooks)
                return;
            SceneManager.sceneLoaded -= HandleSharedMapSceneLoaded;
            SceneManager.sceneLoaded += HandleSharedMapSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSharedMapSceneUnloaded;
            SceneManager.sceneUnloaded += HandleSharedMapSceneUnloaded;
            SceneManager.activeSceneChanged -= HandleSharedMapActiveSceneChanged;
            SceneManager.activeSceneChanged += HandleSharedMapActiveSceneChanged;
            sharedMapSceneHooks = true;
        }

        private static void HandleSharedMapSceneLoaded(Scene scene, LoadSceneMode mode) => InvalidateSharedMinimapSnapshot();
        private static void HandleSharedMapSceneUnloaded(Scene scene) => InvalidateSharedMinimapSnapshot();
        private static void HandleSharedMapActiveSceneChanged(Scene before, Scene after) => InvalidateSharedMinimapSnapshot();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSharedMapSnapshot()
        {
            InvalidateSharedMinimapSnapshot();
            sharedMapSceneHooks = false;
            EnsureSharedMapSceneHooks();
        }

        private bool IsSharedMapPositionCurrent()
        {
            return sharedPositionFrame == Time.frameCount
                && sharedPositionGeneration == sharedMapGeneration
                && sharedPositionWorld == DMWorldScope.Current
                && sharedPositionDualWorld == DMWorldScope.DualWorldActive
                && sharedPositionMounted == PlayerVehicleState.IsMounted
                && sharedPositionCraft == PlayerVehicleState.ActiveCraft
                && sharedPositionPlayer == playerTransform;
        }

        private void EnsureSharedMapPosition()
        {
            EnsureSharedMapSceneHooks();
            if (IsSharedMapPositionCurrent())
                return;
            sharedHasPosition = HasMapWorldPosition();
            sharedWorldPosition = sharedHasPosition ? GetMapWorldPosition() : Vector3.zero;
            sharedPositionFrame = Time.frameCount;
            sharedPositionGeneration = sharedMapGeneration;
            sharedPositionWorld = DMWorldScope.Current;
            sharedPositionDualWorld = DMWorldScope.DualWorldActive;
            sharedPositionMounted = PlayerVehicleState.IsMounted;
            sharedPositionCraft = PlayerVehicleState.ActiveCraft;
            sharedPositionPlayer = playerTransform;
            sharedPositionRevision++;
        }

        public int MinimapViewSnapshotRevision => sharedViewRevision;

        public bool IsMinimapViewSnapshotCurrent(int revision)
        {
            return sharedViewValid && revision == sharedViewRevision
                && sharedViewFrame == Time.frameCount
                && sharedViewGeneration == sharedMapGeneration
                && IsSharedMapPositionCurrent()
                && sharedViewPositionRevision == sharedPositionRevision
                && sharedViewWorldSpan == minimapWorldSpan
                && sharedViewProvider == mapProvider
                && sharedViewSingleton == WorldMapProvider.Instance;
        }

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

        public Vector3 MinimapPlayerWorldPosition
        {
            get
            {
                EnsureSharedMapPosition();
                return sharedWorldPosition;
            }
        }

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
            EnsureSharedMapPosition();
            if (!IsMinimapViewSnapshotCurrent(sharedViewRevision))
            {
                sharedViewValid = CalculateMinimapViewParams(out sharedViewSource, out sharedViewUv,
                    out sharedViewSpan, out sharedViewYaw);
                sharedViewFrame = Time.frameCount;
                sharedViewGeneration = sharedMapGeneration;
                sharedViewPositionRevision = sharedPositionRevision;
                sharedViewWorldSpan = minimapWorldSpan;
                sharedViewProvider = mapProvider;
                sharedViewSingleton = WorldMapProvider.Instance;
                sharedViewRevision++;
            }
            source = sharedViewSource;
            playerUv = sharedViewUv;
            uvSpan = sharedViewSpan;
            facingYaw = sharedViewYaw;
            return sharedViewValid;
        }

        private bool CalculateMinimapViewParams(
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
                if (sharedHasPosition)
                    playerUv = mapProvider.WorldToPlayerMap01(sharedWorldPosition);

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

using System.Collections.Generic;
using Project.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Applies <see cref="GameSettings.UiScale"/> to gameplay / menu canvases.
    /// Settings uses a root-level locked overlay that stays at a fixed 90% design scale.
    /// </summary>
    public static class UiScaleApplier
    {
        public const string LockedSettingsCanvasName = "SettingsUiCanvas";
        public const string LockedControlsCanvasName = "ControlsUiCanvas";

        /// <summary>Settings panel stays at this fraction of design size regardless of UI Scale.</summary>
        public const float LockedSettingsScale = 0.9f;

        private static readonly Vector2 DesignReferenceResolution = new Vector2(1920f, 1080f);
        private static readonly Dictionary<int, float> BaselineConstantScaleFactors = new Dictionary<int, float>(8);

        public static void ApplyFromSettings()
        {
            Apply(GameSettings.UiScale);
        }

        public static void RefreshSettingsPanelScale()
        {
            Apply(GameSettings.UiScale);
        }

        public static void Apply(float uiScale)
        {
            // Higher slider value → larger on-screen HUD (0.4 smallest, 1.25 largest).
            float scale = Mathf.Clamp(uiScale, GameSettings.UiScaleMin, GameSettings.UiScaleMax);
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
                    continue;

                if (IsScaleLockedCanvas(canvas))
                {
                    EnsureCanvasScaler(canvas);
                    LockCanvasScaler(canvas.GetComponent<CanvasScaler>(), LockedSettingsScale);
                    continue;
                }

                string canvasName = canvas.gameObject.name;
                if (canvasName.IndexOf("Progress", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || canvasName.IndexOf("WorldNode", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || canvasName.IndexOf("Loading", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || canvasName.IndexOf("OpticsOverlay", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                CanvasScaler scaler = EnsureCanvasScaler(canvas);
                if (scaler == null)
                    continue;

                int id = canvas.GetEntityId().GetHashCode();
                switch (scaler.uiScaleMode)
                {
                    case CanvasScaler.ScaleMode.ScaleWithScreenSize:
                    {
                        // Smaller reference resolution → larger on-screen UI.
                        scaler.referenceResolution = DesignReferenceResolution / Mathf.Max(0.01f, scale);
                        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                        scaler.matchWidthOrHeight = 0.5f;
                        break;
                    }
                    case CanvasScaler.ScaleMode.ConstantPixelSize:
                    {
                        if (!BaselineConstantScaleFactors.TryGetValue(id, out float baselineFactor)
                            || baselineFactor <= 0.01f)
                        {
                            float current = Mathf.Max(0.01f, scaler.scaleFactor);
                            float currentSettingsScale = Mathf.Max(0.01f, GameSettings.UiScale);
                            baselineFactor = current / currentSettingsScale;
                            BaselineConstantScaleFactors[id] = Mathf.Max(0.01f, baselineFactor);
                        }

                        scaler.scaleFactor = BaselineConstantScaleFactors[id] * scale;
                        break;
                    }
                    case CanvasScaler.ScaleMode.ConstantPhysicalSize:
                    {
                        // Convert to screen-size mode so UI Scale can drive it.
                        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                        scaler.referenceResolution = DesignReferenceResolution / Mathf.Max(0.01f, scale);
                        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                        scaler.matchWidthOrHeight = 0.5f;
                        break;
                    }
                }
            }

            DMUiToolkitBootstrap.ApplyUiScale(scale);
        }

        private static bool IsScaleLockedCanvas(Canvas canvas)
        {
            if (canvas == null)
                return false;

            string name = canvas.gameObject.name;
            return name == LockedSettingsCanvasName
                || name == LockedControlsCanvasName
                || name.IndexOf("SettingsUi", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Creates (or reuses) a Screen Space Overlay canvas at scene root so MainCanvas UI Scale
        /// never inherits into Settings. Locked at <see cref="LockedSettingsScale"/>.
        /// </summary>
        public static Transform EnsureLockedOverlayCanvas(Transform preferredParent, string canvasName, int sortingOrderBoost = 40)
        {
            Canvas existing = null;
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].gameObject.name == canvasName)
                {
                    existing = canvases[i];
                    break;
                }
            }

            Canvas preferredRootCanvas = preferredParent != null
                ? preferredParent.GetComponentInParent<Canvas>()
                : null;

            // Must be a scene-root sibling of MainCanvas — never nested under a scaled canvas.
            Transform rootParent = preferredRootCanvas != null
                ? preferredRootCanvas.transform.parent
                : null;

            if (existing != null)
            {
                if (existing.transform.parent != rootParent)
                    existing.transform.SetParent(rootParent, false);

                ConfigureLockedCanvas(existing, preferredRootCanvas, sortingOrderBoost);
                return existing.transform;
            }

            GameObject host = new GameObject(
                canvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            if (rootParent != null)
                host.transform.SetParent(rootParent, false);

            Canvas canvas = host.GetComponent<Canvas>();
            ConfigureLockedCanvas(canvas, preferredRootCanvas, sortingOrderBoost);
            return host.transform;
        }

        private static void ConfigureLockedCanvas(Canvas canvas, Canvas preferredRootCanvas, int sortingOrderBoost)
        {
            if (canvas == null)
                return;

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            int baseOrder = preferredRootCanvas != null ? preferredRootCanvas.sortingOrder : 0;
            canvas.sortingOrder = baseOrder + sortingOrderBoost;

            LockCanvasScaler(EnsureCanvasScaler(canvas), LockedSettingsScale);

            RectTransform rect = canvas.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
                rect.localPosition = Vector3.zero;
                rect.localRotation = Quaternion.identity;
            }
        }

        private static CanvasScaler EnsureCanvasScaler(Canvas canvas)
        {
            if (canvas == null)
                return null;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = DesignReferenceResolution;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                scaler.referencePixelsPerUnit = 100f;
            }

            return scaler;
        }

        private static void LockCanvasScaler(CanvasScaler scaler, float fixedScale)
        {
            if (scaler == null)
                return;

            float scale = Mathf.Clamp(fixedScale, 0.5f, 1.25f);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Larger reference → smaller on-screen UI. fixedScale 0.9 → 90% of design size.
            scaler.referenceResolution = DesignReferenceResolution / Mathf.Max(0.01f, scale);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
        }
    }
}

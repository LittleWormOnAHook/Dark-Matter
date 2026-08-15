using System.Collections.Generic;
using Project.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Applies <see cref="GameSettings.UiScale"/> to screen-space canvases so Settings can
    /// scale HUD / menus without rebuilding layout code per widget.
    /// </summary>
    public static class UiScaleApplier
    {
        private static readonly Dictionary<int, Vector2> BaselineReferenceResolutions = new Dictionary<int, Vector2>(8);
        private static readonly Dictionary<int, float> BaselineConstantScaleFactors = new Dictionary<int, float>(8);

        public static void ApplyFromSettings()
        {
            Apply(GameSettings.UiScale);
        }

        public static void Apply(float uiScale)
        {
            float scale = Mathf.Clamp(uiScale, GameSettings.UiScaleMin, GameSettings.UiScaleMax);
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
                    continue;

                // Skip tiny world helper canvases (progress bars, etc.).
                string canvasName = canvas.gameObject.name;
                if (canvasName.IndexOf("Progress", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || canvasName.IndexOf("WorldNode", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null)
                    continue;

                int id = scaler.GetEntityId().GetHashCode();
                switch (scaler.uiScaleMode)
                {
                    case CanvasScaler.ScaleMode.ScaleWithScreenSize:
                    {
                        if (!BaselineReferenceResolutions.TryGetValue(id, out Vector2 baseline)
                            || baseline.x < 1f
                            || baseline.y < 1f)
                        {
                            baseline = scaler.referenceResolution;
                            if (baseline.x < 1f || baseline.y < 1f)
                                baseline = new Vector2(1920f, 1080f);
                            BaselineReferenceResolutions[id] = baseline;
                        }

                        // Smaller reference resolution → larger on-screen UI.
                        scaler.referenceResolution = baseline / Mathf.Max(0.01f, scale);
                        break;
                    }
                    case CanvasScaler.ScaleMode.ConstantPixelSize:
                    {
                        if (!BaselineConstantScaleFactors.TryGetValue(id, out float baselineFactor)
                            || baselineFactor <= 0.01f)
                        {
                            baselineFactor = Mathf.Max(0.01f, scaler.scaleFactor);
                            BaselineConstantScaleFactors[id] = baselineFactor;
                        }

                        scaler.scaleFactor = baselineFactor * scale;
                        break;
                    }
                }
            }
        }
    }
}

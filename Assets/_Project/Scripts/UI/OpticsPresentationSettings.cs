using System;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    [Serializable]
    public class OpticsImageLayerSettings
    {
        [Tooltip("Optional sprite override for this layer. When empty, the library source texture is used.")]
        public Sprite sprite;

        [Tooltip("Optional per-layer UI material.")]
        public Material material;

        public Color color = Color.white;

        [Tooltip("When enabled, the layer fills its parent rect.")]
        public bool stretchToParent = true;

        public Vector2 anchorMin = new Vector2(0.5f, 0.5f);
        public Vector2 anchorMax = new Vector2(0.5f, 0.5f);
        public Vector2 pivot = new Vector2(0.5f, 0.5f);
        public Vector2 anchoredPosition = Vector2.zero;
        public Vector2 sizeDelta = new Vector2(420f, 420f);
        public bool preserveAspect;
        public Image.Type imageType = Image.Type.Simple;
        public bool visible = true;
    }

    [Serializable]
    public class OpticsViewportPresentation
    {
        public Color backgroundColor = Color.black;
        public Color rawImageColor = Color.white;

        [Tooltip("Optional viewport material override. Falls back to the library viewport material.")]
        public Material materialOverride;

        [Header("Binocular Mask")]
        public float binocularRadius = 0.33f;
        public float binocularEdgeSoftness = 0.014f;
        public Color binocularTint = new Color(0.95f, 0.98f, 1f, 1f);

        [Header("Scanner Mask")]
        public float scannerRectHalfWidth = 0.4f;
        public float scannerRectHalfHeight = 0.24f;
        public float scannerEdgeSoftness = 0.012f;
        public float scannerFuzz = 0.035f;
        public Color scannerTint = new Color(0.82f, 1f, 0.9f, 1f);

        [Header("Scanner HUD")]
        [Tooltip("Width of the faded green edge border strips (pixels).")]
        public float scannerBorderWidthPixels = 30f;

        [Tooltip("Marker clamp inset from screen edges. When zero, derived from border width and screen size.")]
        public float scannerHalfWidthPixels;
        public float scannerHalfHeightPixels;

        public void GetScannerMarkerHalfExtents(out float halfWidth, out float halfHeight)
        {
            float border = Mathf.Max(0f, scannerBorderWidthPixels > 0f ? scannerBorderWidthPixels : 30f);
            halfWidth = scannerHalfWidthPixels > 0f
                ? scannerHalfWidthPixels
                : Screen.width * 0.5f - border;
            halfHeight = scannerHalfHeightPixels > 0f
                ? scannerHalfHeightPixels
                : Screen.height * 0.5f - border;
        }
    }

    [Serializable]
    public class OpticsModeLabelSettings
    {
        public Vector2 anchor = new Vector2(0.5f, 0.9f);
        public Vector2 sizeDelta = new Vector2(640f, 36f);
        public float fontSize = 22f;
        public Color scannerColor = new Color(0.549f, 1f, 0.82f, 0.949f);
        public Color binocularColor = new Color(0.85f, 0.95f, 1f, 0.95f);
        public string scannerText = "SCANNER MODE";
        public string binocularText = "BINOCULARS";
    }

    [Serializable]
    public class OpticsHintLabelSettings
    {
        public Vector2 anchor = new Vector2(0.5f, 0.06f);
        public Vector2 sizeDelta = new Vector2(640f, 36f);
        public float fontSize = 20f;
        public Color color = new Color(0.992f, 0.71f, 0.29f, 1f);
        public string scannerHint = "[RMB] Close  |  [Scroll] Zoom  |  POI glow active";
        public string binocularHint = "[RMB] Close  |  [Scroll] Zoom";
    }

    [Serializable]
    public class OpticsScannerMarkerSettings
    {
        public Sprite markerSprite;
        public Material markerMaterial;
        public Vector2 markerSize = new Vector2(18f, 18f);
        public Color labelColor = new Color(0.55f, 1f, 0.85f, 0.95f);
        public float labelFontSize = 14f;
        public Vector2 labelOffset = new Vector2(12f, 0f);
        public Vector2 labelSize = new Vector2(160f, 24f);
        public float activePulseBase = 0.7f;
        public float postScanPulseBase = 0.55f;
    }

    public static class OpticsPresentationDefaults
    {
        public static OpticsImageLayerSettings BinocularScopeOuter()
        {
            return new OpticsImageLayerSettings
            {
                stretchToParent = true,
                color = new Color(1f, 1f, 1f, 0.55f)
            };
        }

        public static OpticsImageLayerSettings BinocularScopeFull()
        {
            return new OpticsImageLayerSettings
            {
                stretchToParent = true,
                color = Color.white
            };
        }

        public static OpticsImageLayerSettings BinocularScopeInnerGlow()
        {
            return new OpticsImageLayerSettings
            {
                stretchToParent = false,
                sizeDelta = new Vector2(680f, 680f),
                color = new Color(0.902f, 0.949f, 1f, 0.922f)
            };
        }

        public static OpticsImageLayerSettings ScannerMaskFrame()
        {
            return new OpticsImageLayerSettings { visible = false };
        }

        public static OpticsImageLayerSettings ScannerFrame()
        {
            return new OpticsImageLayerSettings { visible = false };
        }

        public static OpticsImageLayerSettings ScannerReticle()
        {
            return new OpticsImageLayerSettings { visible = false };
        }

        public static OpticsImageLayerSettings ScannerTintOverlay()
        {
            return new OpticsImageLayerSettings { visible = false };
        }
    }
}

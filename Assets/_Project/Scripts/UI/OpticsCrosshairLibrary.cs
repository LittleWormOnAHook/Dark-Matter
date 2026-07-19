using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Project.UI
{
    public class OpticsCrosshairLibrary : ScriptableObject
    {
        [Header("Source Textures - Binoculars")]
        public Texture2D binocularScopeFull;
        public Texture2D binocularScopeInnerGlow;
        public Texture2D binocularScopeOuter;

        [Header("Source Textures - Scanner")]
        public Texture2D scannerHolographic;
        public Texture2D scannerHolographicGlow;
        public Texture2D scannerRectMask;

        [Header("Shared Overlay Assets")]
        [Tooltip("Optional sliced/UI sprite used for ScannerMaskFrame. Preferred over scannerRectMask texture.")]
        public Sprite scannerMaskFrameSprite;

        [Tooltip("Optional fullscreen backdrop behind the optics viewport.")]
        public Sprite viewportBackgroundSprite;

        [Tooltip("Default material for OpticsViewport RawImage. Falls back to Project/OpticsViewport.")]
        public Material viewportMaterial;

        [Header("Binocular Layers")]
        public OpticsImageLayerSettings binocularScopeOuterLayer = OpticsPresentationDefaults.BinocularScopeOuter();
        public OpticsImageLayerSettings binocularScopeFullLayer = OpticsPresentationDefaults.BinocularScopeFull();
        public OpticsImageLayerSettings binocularScopeInnerGlowLayer = OpticsPresentationDefaults.BinocularScopeInnerGlow();

        [Header("Scanner Layers")]
        public OpticsImageLayerSettings scannerMaskFrameLayer = OpticsPresentationDefaults.ScannerMaskFrame();
        public OpticsImageLayerSettings scannerFrameLayer = OpticsPresentationDefaults.ScannerFrame();
        public OpticsImageLayerSettings scannerReticleLayer = OpticsPresentationDefaults.ScannerReticle();
        public OpticsImageLayerSettings scannerTintOverlayLayer = OpticsPresentationDefaults.ScannerTintOverlay();

        [Header("Viewport")]
        public OpticsViewportPresentation viewport = new OpticsViewportPresentation();

        [Header("Labels")]
        public OpticsModeLabelSettings modeLabel = new OpticsModeLabelSettings();
        public OpticsHintLabelSettings hintLabel = new OpticsHintLabelSettings();

        [Header("Scanner Markers")]
        public OpticsScannerMarkerSettings scannerMarkers = new OpticsScannerMarkerSettings();

        public Material ResolveViewportMaterial()
        {
            if (viewport != null && viewport.materialOverride != null)
                return viewport.materialOverride;

            return viewportMaterial;
        }

        public void ResetPresentationDefaults()
        {
            binocularScopeOuterLayer = OpticsPresentationDefaults.BinocularScopeOuter();
            binocularScopeFullLayer = OpticsPresentationDefaults.BinocularScopeFull();
            binocularScopeInnerGlowLayer = OpticsPresentationDefaults.BinocularScopeInnerGlow();
            scannerMaskFrameLayer = OpticsPresentationDefaults.ScannerMaskFrame();
            scannerFrameLayer = OpticsPresentationDefaults.ScannerFrame();
            scannerReticleLayer = OpticsPresentationDefaults.ScannerReticle();
            scannerTintOverlayLayer = OpticsPresentationDefaults.ScannerTintOverlay();
            viewport = new OpticsViewportPresentation();
            modeLabel = new OpticsModeLabelSettings();
            hintLabel = new OpticsHintLabelSettings();
            scannerMarkers = new OpticsScannerMarkerSettings();
        }

        public static void ApplyImageLayer(Image image, OpticsImageLayerSettings settings, Sprite fallbackSprite)
        {
            if (image == null || settings == null)
                return;

            RectTransform rect = image.rectTransform;
            Sprite resolvedSprite = settings.sprite != null ? settings.sprite : fallbackSprite;
            if (resolvedSprite != null)
                image.sprite = resolvedSprite;

            image.material = settings.material;
            image.color = settings.color;
            image.type = settings.imageType;
            image.preserveAspect = settings.preserveAspect;
            image.enabled = settings.visible;

            if (settings.stretchToParent)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                return;
            }

            rect.anchorMin = settings.anchorMin;
            rect.anchorMax = settings.anchorMax;
            rect.pivot = settings.pivot;
            rect.anchoredPosition = settings.anchoredPosition;
            rect.sizeDelta = settings.sizeDelta;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void ApplyTextLabel(TextMeshProUGUI label, Vector2 anchor, Vector2 sizeDelta, float fontSize, Color color, string text)
        {
            if (label == null)
                return;

            RectTransform rect = label.rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            label.fontSize = fontSize;
            label.color = color;
            if (!string.IsNullOrEmpty(text))
                label.text = text;
        }
    }
}

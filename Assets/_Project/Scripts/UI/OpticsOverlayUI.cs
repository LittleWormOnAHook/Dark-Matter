using System.Collections.Generic;
using Project.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// First-person optics overlay with masked render-texture viewport.
    /// Binoculars use circular scope art; scanner uses a rectangular HUD viewport.
    /// </summary>
    public class OpticsOverlayUI : MonoBehaviour
    {
        private const int MaxScannerMarkers = 24;
        private const float ScannerFrameSize = 900f;
        private const float ScannerReticleSize = 420f;

        private static readonly int ModeId = Shader.PropertyToID("_Mode");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int RectHalfWidthId = Shader.PropertyToID("_RectHalfWidth");
        private static readonly int RectHalfHeightId = Shader.PropertyToID("_RectHalfHeight");
        private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int ScannerFuzzId = Shader.PropertyToID("_ScannerFuzz");
        private static readonly int TintId = Shader.PropertyToID("_Tint");

        private GameObject overlayRoot;
        private GameObject binocularRoot;
        private GameObject scannerRoot;
        private RectTransform markerLayer;
        private TextMeshProUGUI modeLabel;
        private TextMeshProUGUI hintLabel;
        private Image scannerTint;
        private Image viewportBackground;
        private RawImage viewportImage;
        private Material viewportMaterial;
        private readonly List<RectTransform> markerPool = new List<RectTransform>();
        private bool uiBuilt;
        private bool isVisible;
        private float scannerHalfWidthPixels = 420f;
        private float scannerHalfHeightPixels = 250f;

        public bool IsBuilt => uiBuilt;
        public bool IsVisible => isVisible && overlayRoot != null && overlayRoot.activeSelf;

        public void EnsureBuilt(Transform canvasRoot)
        {
            if (uiBuilt)
                return;

            Transform overlayParent = ResolveOverlayParent(canvasRoot);
            if (overlayParent == null)
                return;

            overlayRoot = new GameObject("OpticsOverlay", typeof(RectTransform), typeof(CanvasGroup));
            overlayRoot.transform.SetParent(overlayParent, false);

            RectTransform rootRect = overlayRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            CanvasGroup group = overlayRoot.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.blocksRaycasts = false;
            group.interactable = false;

            viewportBackground = CreateStretchImage(overlayRoot.transform, "ViewportBackground", null);
            viewportBackground.color = new Color(0.01f, 0.01f, 0.02f, 1f);
            viewportBackground.raycastTarget = false;

            viewportMaterial = CreateViewportMaterial();
            GameObject viewportObject = new GameObject("OpticsViewport", typeof(RectTransform), typeof(RawImage));
            viewportObject.transform.SetParent(overlayRoot.transform, false);
            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            viewportImage = viewportObject.GetComponent<RawImage>();
            viewportImage.material = viewportMaterial;
            viewportImage.raycastTarget = false;
            viewportImage.color = Color.white;

            binocularRoot = new GameObject("BinocularOverlay", typeof(RectTransform));
            binocularRoot.transform.SetParent(overlayRoot.transform, false);
            StretchRect(binocularRoot.GetComponent<RectTransform>());

            Image binocularOuter = CreateStretchImage(binocularRoot.transform, "ScopeOuter", OpticsUiSprites.BinocularScopeOuter);
            binocularOuter.color = new Color(1f, 1f, 1f, 0.55f);
            binocularOuter.raycastTarget = false;

            Image binocularFrame = CreateStretchImage(binocularRoot.transform, "ScopeFull", OpticsUiSprites.BinocularScopeFull);
            binocularFrame.color = Color.white;
            binocularFrame.raycastTarget = false;

            Image binocularInner = CreateCenteredImage(
                binocularRoot.transform,
                "ScopeInnerGlow",
                OpticsUiSprites.BinocularScopeInnerGlow,
                680f);
            binocularInner.color = new Color(0.9f, 0.95f, 1f, 0.92f);
            binocularInner.raycastTarget = false;

            scannerRoot = new GameObject("ScannerOverlay", typeof(RectTransform));
            scannerRoot.transform.SetParent(overlayRoot.transform, false);
            StretchRect(scannerRoot.GetComponent<RectTransform>());

            Image scannerMaskFrame = CreateStretchImage(scannerRoot.transform, "ScannerMaskFrame", OpticsUiSprites.ScannerRectMask);
            scannerMaskFrame.color = new Color(0.15f, 0.95f, 0.75f, 0.18f);
            scannerMaskFrame.raycastTarget = false;

            Image scannerFrame = CreateCenteredImage(
                scannerRoot.transform,
                "ScannerFrame",
                OpticsUiSprites.ScannerHolographicGlow,
                ScannerFrameSize);
            scannerFrame.color = new Color(0.35f, 1f, 0.82f, 0.95f);
            scannerFrame.raycastTarget = false;

            Image scannerReticle = CreateCenteredImage(
                scannerRoot.transform,
                "ScannerReticle",
                OpticsUiSprites.ScannerHolographic,
                ScannerReticleSize);
            scannerReticle.color = new Color(0.45f, 1f, 0.85f, 0.9f);
            scannerReticle.raycastTarget = false;

            scannerTint = CreateStretchImage(scannerRoot.transform, "ScannerTint", null);
            scannerTint.color = new Color(0.12f, 0.9f, 0.7f, 0.08f);
            scannerTint.raycastTarget = false;

            GameObject markerRoot = new GameObject("ScannerMarkers", typeof(RectTransform));
            markerRoot.transform.SetParent(overlayRoot.transform, false);
            markerLayer = markerRoot.GetComponent<RectTransform>();
            StretchRect(markerLayer);

            modeLabel = CreateLabel(overlayRoot.transform, "ModeLabel", new Vector2(0.5f, 0.9f), 22f);
            hintLabel = CreateLabel(overlayRoot.transform, "HintLabel", new Vector2(0.5f, 0.06f), 16f);
            hintLabel.color = new Color(0.8f, 0.85f, 0.9f, 0.85f);
            hintLabel.text = "[RMB] Close  |  [Scroll] Zoom";

            overlayRoot.SetActive(false);
            uiBuilt = true;
        }

        public void BindRenderTexture(RenderTexture texture)
        {
            if (viewportImage != null)
                viewportImage.texture = texture;
        }

        public void SetVisible(bool visible, ToolType toolType)
        {
            if (!uiBuilt || overlayRoot == null)
            {
                isVisible = false;
                return;
            }

            isVisible = visible;
            overlayRoot.SetActive(visible);
            if (!visible)
                return;

            overlayRoot.transform.SetAsLastSibling();

            bool scanner = toolType == ToolType.Scanner;
            if (binocularRoot != null)
                binocularRoot.SetActive(!scanner);

            if (scannerRoot != null)
                scannerRoot.SetActive(scanner);

            if (markerLayer != null)
                markerLayer.gameObject.SetActive(scanner);

            ApplyViewportMode(scanner);

            if (modeLabel != null)
            {
                modeLabel.color = scanner
                    ? new Color(0.55f, 1f, 0.82f, 0.95f)
                    : new Color(0.85f, 0.95f, 1f, 0.95f);
                modeLabel.text = scanner ? "SCANNER MODE" : "BINOCULARS";
            }

            if (hintLabel != null)
            {
                hintLabel.text = scanner
                    ? "[RMB] Close  |  [Scroll] Zoom  |  POI glow active"
                    : "[RMB] Close  |  [Scroll] Zoom";
            }

            if (!scanner)
                ClearScannerMarkers();
        }

        private void ApplyViewportMode(bool scanner)
        {
            if (viewportMaterial == null)
                return;

            viewportMaterial.SetFloat(ModeId, scanner ? 1f : 0f);
            viewportMaterial.SetFloat(RadiusId, 0.33f);
            viewportMaterial.SetFloat(RectHalfWidthId, 0.4f);
            viewportMaterial.SetFloat(RectHalfHeightId, 0.24f);
            viewportMaterial.SetFloat(EdgeSoftnessId, scanner ? 0.012f : 0.014f);
            viewportMaterial.SetFloat(ScannerFuzzId, scanner ? 0.035f : 0f);
            viewportMaterial.SetColor(
                TintId,
                scanner ? new Color(0.82f, 1f, 0.9f, 1f) : new Color(0.95f, 0.98f, 1f, 1f));

            scannerHalfWidthPixels = Screen.height * 0.4f * (Screen.width / (float)Mathf.Max(1, Screen.height));
            scannerHalfHeightPixels = Screen.height * 0.24f;
        }

        public void UpdateScannerMarkers(Camera worldCamera, IReadOnlyList<OpticsScanTarget> targets, float viewportRadiusPixels)
        {
            UpdateScannerMarkers(worldCamera, targets, scannerHalfWidthPixels, scannerHalfHeightPixels);
        }

        public void UpdateScannerMarkers(
            Camera worldCamera,
            IReadOnlyList<OpticsScanTarget> targets,
            float halfWidthPixels,
            float halfHeightPixels)
        {
            if (!uiBuilt || markerLayer == null || worldCamera == null)
                return;

            scannerHalfWidthPixels = halfWidthPixels;
            scannerHalfHeightPixels = halfHeightPixels;

            EnsureMarkerPool(targets.Count);

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            for (int i = 0; i < markerPool.Count; i++)
            {
                RectTransform marker = markerPool[i];
                if (i >= targets.Count)
                {
                    marker.gameObject.SetActive(false);
                    continue;
                }

                OpticsScanTarget target = targets[i];
                Vector3 screenPoint = worldCamera.WorldToScreenPoint(target.WorldPosition);
                if (screenPoint.z <= 0f)
                {
                    marker.gameObject.SetActive(false);
                    continue;
                }

                Vector2 offset = new Vector2(screenPoint.x, screenPoint.y) - screenCenter;
                offset.x = Mathf.Clamp(offset.x, -halfWidthPixels, halfWidthPixels);
                offset.y = Mathf.Clamp(offset.y, -halfHeightPixels, halfHeightPixels);

                marker.gameObject.SetActive(true);
                marker.anchoredPosition = offset;

                Image dot = marker.GetComponent<Image>();
                if (dot != null)
                {
                    float pulse = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 4f + i);
                    Color c = target.MarkerColor;
                    c.a = pulse;
                    dot.color = c;
                }

                TextMeshProUGUI label = marker.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = target.Label;
            }
        }

        public void ClearScannerMarkers()
        {
            for (int i = 0; i < markerPool.Count; i++)
            {
                if (markerPool[i] != null)
                    markerPool[i].gameObject.SetActive(false);
            }
        }

        private void EnsureMarkerPool(int requiredCount)
        {
            requiredCount = Mathf.Min(requiredCount, MaxScannerMarkers);

            while (markerPool.Count < requiredCount)
            {
                GameObject markerObject = new GameObject("ScannerMarker", typeof(RectTransform), typeof(Image));
                markerObject.transform.SetParent(markerLayer, false);

                RectTransform markerRect = markerObject.GetComponent<RectTransform>();
                markerRect.anchorMin = new Vector2(0.5f, 0.5f);
                markerRect.anchorMax = new Vector2(0.5f, 0.5f);
                markerRect.pivot = new Vector2(0.5f, 0.5f);
                markerRect.sizeDelta = new Vector2(18f, 18f);

                Image dot = markerObject.GetComponent<Image>();
                dot.sprite = OpticsUiSprites.ScannerHolographic != null
                    ? OpticsUiSprites.ScannerHolographic
                    : MapUiSprites.Dot;
                dot.raycastTarget = false;

                GameObject labelObject = new GameObject("Label", typeof(RectTransform));
                labelObject.transform.SetParent(markerObject.transform, false);
                RectTransform labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0.5f, 0.5f);
                labelRect.anchorMax = new Vector2(0.5f, 0.5f);
                labelRect.pivot = new Vector2(0f, 0.5f);
                labelRect.anchoredPosition = new Vector2(12f, 0f);
                labelRect.sizeDelta = new Vector2(160f, 24f);

                TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
                TmpUiHelper.ApplyDefaultFont(label);
                label.fontSize = 14f;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.color = new Color(0.55f, 1f, 0.85f, 0.95f);

                markerPool.Add(markerRect);
            }
        }

        private static Transform ResolveOverlayParent(Transform fallbackRoot)
        {
            if (fallbackRoot != null && fallbackRoot.localScale.sqrMagnitude < 0.001f)
                fallbackRoot.localScale = Vector3.one;

            const string overlayCanvasName = "OpticsOverlayCanvas";
            GameObject existing = GameObject.Find(overlayCanvasName);
            if (existing != null)
                return existing.transform;

            GameObject canvasObject = new GameObject(
                overlayCanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rect = canvasObject.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return canvasObject.transform;
        }

        private static Material CreateViewportMaterial()
        {
            Shader shader = Shader.Find("Project/OpticsViewport");
            if (shader == null)
                shader = Shader.Find("UI/Default");

            return new Material(shader) { name = "OpticsViewportMaterial" };
        }

        private static void StretchRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Image CreateStretchImage(Transform parent, string name, Sprite sprite)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            StretchRect(rect);

            Image image = imageObject.GetComponent<Image>();
            if (sprite != null)
                image.sprite = sprite;
            return image;
        }

        private static Image CreateCenteredImage(Transform parent, string name, Sprite sprite, float size)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);

            Image image = imageObject.GetComponent<Image>();
            if (sprite != null)
                image.sprite = sprite;
            return image;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, Vector2 anchorY, float fontSize)
        {
            GameObject labelObject = new GameObject(name, typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);

            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorY;
            rect.anchorMax = anchorY;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(640f, 36f);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.fontSize = fontSize;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.85f, 0.95f, 1f, 0.95f);
            return label;
        }

        private void OnDestroy()
        {
            if (viewportMaterial != null)
                Destroy(viewportMaterial);
        }
    }

    public readonly struct OpticsScanTarget
    {
        public OpticsScanTarget(Vector3 worldPosition, string label, Color markerColor)
        {
            WorldPosition = worldPosition;
            Label = label;
            MarkerColor = markerColor;
        }

        public Vector3 WorldPosition { get; }
        public string Label { get; }
        public Color MarkerColor { get; }
    }
}

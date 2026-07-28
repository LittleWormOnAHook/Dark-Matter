using Project.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Interaction
{
    /// <summary>
    /// Tiny world-space mining/harvest time slider (fills 0→1 over hold / mine duration).
    /// Shared by laser mining and plant Hold-E harvest.
    /// </summary>
    public class WorldNodeProgressBar : MonoBehaviour
    {
        private const float Width = 0.27f;
        private const float Height = 0.06f;

        private Canvas _canvas;
        private RectTransform _root;
        private Slider _slider;
        private Image _fill;
        private TextMeshProUGUI _label;

        public static WorldNodeProgressBar Create(Transform host)
        {
            GameObject go = new GameObject("WorldNodeProgressBar");
            if (host != null)
                go.transform.SetParent(host, false);

            WorldNodeProgressBar bar = go.AddComponent<WorldNodeProgressBar>();
            bar.Build();
            bar.SetVisible(false);
            return bar;
        }

        private void Build()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            gameObject.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
            _root = GetComponent<RectTransform>();
            _root.sizeDelta = new Vector2(Width, Height);

            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(_root, false);
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.88f);
            bgImage.raycastTarget = false;
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(_root, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0.06f, 0.22f);
            fillAreaRect.anchorMax = new Vector2(0.94f, 0.78f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillArea.transform, false);
            _fill = fillGo.AddComponent<Image>();
            _fill.color = SurvivalPioneerUiPalette.Gold;
            _fill.raycastTarget = false;
            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            // Left-anchored so Slider can grow fill width with value (0→1 over time).
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            _slider = gameObject.AddComponent<Slider>();
            _slider.minValue = 0f;
            _slider.maxValue = 1f;
            _slider.wholeNumbers = false;
            _slider.interactable = false;
            _slider.transition = Selectable.Transition.None;
            _slider.navigation = new Navigation { mode = Navigation.Mode.None };
            _slider.fillRect = fillRect;
            _slider.targetGraphic = bgImage;
            _slider.direction = Slider.Direction.LeftToRight;
            _slider.SetValueWithoutNotify(0f);

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(_root, false);
            _label = labelGo.AddComponent<TextMeshProUGUI>();
            _label.fontSize = 0.035f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.color = SurvivalPioneerUiPalette.WarmOffWhite;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.raycastTarget = false;
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        public void SetVisible(bool visible)
        {
            if (_canvas != null)
                _canvas.gameObject.SetActive(visible);
        }

        /// <param name="fill01">Normalized mining/harvest time progress (0 = start, 1 = complete).</param>
        public void UpdateBar(Vector3 worldPosition, float fill01, string label, Camera camera)
        {
            if (_root == null)
                return;

            SetVisible(true);
            _root.position = worldPosition;
            Camera cam = camera != null ? camera : Camera.main;
            if (cam != null)
                _root.rotation = Quaternion.LookRotation(_root.position - cam.transform.position);

            float value = Mathf.Clamp01(fill01);
            if (_slider != null)
                _slider.SetValueWithoutNotify(value);
            else if (_fill != null)
                _fill.fillAmount = value;

            if (_label != null)
                _label.text = label ?? string.Empty;
        }
    }
}

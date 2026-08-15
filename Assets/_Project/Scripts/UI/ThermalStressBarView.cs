using Project.Survival;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Bipolar cold/heat fill for the thermal HUD segment (center = safe, left = cold, right = heat).
    /// </summary>
    [DisallowMultipleComponent]
    public class ThermalStressBarView : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private Image coldFill;
        [SerializeField] private Image heatFill;
        [SerializeField] private Image centerMarker;
        [SerializeField] private TextMeshProUGUI valueLabel;

        private SurvivalStats boundStats;
        private int lastLabelBucket = int.MinValue;

        private void Awake()
        {
            slider ??= GetComponentInChildren<Slider>(true);
            EnsureFillImages();
        }

        private void OnEnable()
        {
            BindStats(FindAnyObjectByType<SurvivalStats>());
        }

        private void OnDisable()
        {
            UnbindStats();
        }

        public void BindStats(SurvivalStats stats)
        {
            if (boundStats == stats)
                return;

            UnbindStats();
            boundStats = stats;
            if (boundStats != null)
                boundStats.OnStatsChanged += Refresh;
            Refresh();
        }

        private void UnbindStats()
        {
            if (boundStats != null)
                boundStats.OnStatsChanged -= Refresh;
            boundStats = null;
        }

        public void Refresh()
        {
            EnsureFillImages();
            if (boundStats == null)
                boundStats = FindAnyObjectByType<SurvivalStats>();

            if (boundStats == null)
                return;

            float signed = boundStats.GetThermalNormalizedSigned();
            float coldAmount = signed < 0f ? -signed : 0f;
            float heatAmount = signed > 0f ? signed : 0f;

            ApplyHalfFill(coldFill, coldAmount, fromCenterLeft: true);
            ApplyHalfFill(heatFill, heatAmount, fromCenterLeft: false);

            if (slider != null)
                slider.SetValueWithoutNotify(Mathf.InverseLerp(-1f, 1f, signed));

            if (valueLabel != null)
            {
                int bucket;
                string label;
                if (Mathf.Abs(signed) < 0.05f)
                {
                    bucket = 0;
                    label = "OK";
                }
                else if (signed < 0f)
                {
                    int coldPercent = Mathf.RoundToInt(coldAmount * 100f);
                    bucket = -1000 - coldPercent;
                    label = $"C {coldPercent}";
                }
                else
                {
                    int heatPercent = Mathf.RoundToInt(heatAmount * 100f);
                    bucket = 1000 + heatPercent;
                    label = $"H {heatPercent}";
                }

                if (bucket != lastLabelBucket)
                {
                    lastLabelBucket = bucket;
                    valueLabel.text = label;
                }
            }
        }

        private void EnsureFillImages()
        {
            if (slider == null)
                return;

            Transform track = slider.transform.Find("RingBackground");
            if (track == null)
                return;

            coldFill ??= CreateFillImage(track, "ColdFill", new Color(0.35f, 0.72f, 0.95f, 1f));
            heatFill ??= CreateFillImage(track, "HeatFill", new Color(0.95f, 0.45f, 0.18f, 1f));
            centerMarker ??= CreateFillImage(track, "CenterMarker", DarkMatterGenesisUiPalette.SoftBeigeGray);

            if (centerMarker != null)
            {
                RectTransform markerRect = centerMarker.rectTransform;
                markerRect.anchorMin = new Vector2(0.5f, 0f);
                markerRect.anchorMax = new Vector2(0.5f, 1f);
                markerRect.pivot = new Vector2(0.5f, 0.5f);
                markerRect.sizeDelta = new Vector2(2f, 0f);
                markerRect.anchoredPosition = Vector2.zero;
            }
        }

        private static Image CreateFillImage(Transform parent, string name, Color color)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.GetComponent<Image>();

            GameObject fillObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(parent, false);
            Image image = fillObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void ApplyHalfFill(Image fill, float amount, bool fromCenterLeft)
        {
            if (fill == null)
                return;

            amount = Mathf.Clamp01(amount);
            RectTransform rect = fill.rectTransform;
            if (amount <= 0f)
            {
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                fill.enabled = false;
                return;
            }

            fill.enabled = true;
            if (fromCenterLeft)
            {
                rect.anchorMin = new Vector2(0.5f - amount * 0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 1f);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f + amount * 0.5f, 1f);
            }

            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

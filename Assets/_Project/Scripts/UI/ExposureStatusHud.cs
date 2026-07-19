using Project.Survival;
using TMPro;
using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Compact rad / sulfur / volcano indicators shown when exposure is active.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExposureStatusHud : MonoBehaviour
    {
        [SerializeField] private float showThreshold = 0.08f;

        private SurvivalStats boundStats;
        private TextMeshProUGUI statusLabel;

        private void OnEnable()
        {
            EnsureBuilt();
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
            EnsureBuilt();
            if (boundStats == null)
                boundStats = FindAnyObjectByType<SurvivalStats>();

            if (statusLabel == null)
                return;

            if (boundStats == null)
            {
                statusLabel.gameObject.SetActive(false);
                return;
            }

            float rad = boundStats.GetRadiationNormalized();
            float sulfur = boundStats.GetSulfurNormalized();
            float volcano = boundStats.GetVolcanoNormalized();

            bool showRad = rad >= showThreshold;
            bool showSulfur = sulfur >= showThreshold;
            bool showVolcano = volcano >= showThreshold;

            if (!showRad && !showSulfur && !showVolcano)
            {
                statusLabel.gameObject.SetActive(false);
                return;
            }

            statusLabel.gameObject.SetActive(true);
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            if (showRad)
                builder.Append($"RAD {Mathf.RoundToInt(rad * 100f)}%  ");
            if (showSulfur)
                builder.Append($"S {Mathf.RoundToInt(sulfur * 100f)}%  ");
            if (showVolcano)
                builder.Append($"V {Mathf.RoundToInt(volcano * 100f)}%");

            statusLabel.text = builder.ToString().Trim();
            statusLabel.color = SurvivalPioneerUiPalette.Gold;
        }

        private void EnsureBuilt()
        {
            if (statusLabel != null)
                return;

            GameObject labelObject = new GameObject("ExposureStatusLabel", typeof(RectTransform));
            labelObject.transform.SetParent(transform, false);

            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, 6f);
            rect.sizeDelta = new Vector2(420f, 18f);

            statusLabel = labelObject.AddComponent<TextMeshProUGUI>();
            statusLabel.fontSize = 11f;
            statusLabel.alignment = TextAlignmentOptions.Center;
            statusLabel.raycastTarget = false;
        }
    }
}

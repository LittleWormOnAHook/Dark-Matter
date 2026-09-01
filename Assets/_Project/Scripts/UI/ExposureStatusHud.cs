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
        private readonly System.Text.StringBuilder statusBuilder = new System.Text.StringBuilder(64);
        private string lastStatusText;
        private int lastRadPercent = int.MinValue;
        private int lastSulfurPercent = int.MinValue;
        private int lastVolcanoPercent = int.MinValue;

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
            if (DMUiToolkitHud.IsDriving)
            {
                if (statusLabel != null && statusLabel.gameObject.activeSelf)
                    statusLabel.gameObject.SetActive(false);
                return;
            }

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
                if (statusLabel.gameObject.activeSelf)
                    statusLabel.gameObject.SetActive(false);
                return;
            }

            int radPercent = showRad ? Mathf.RoundToInt(rad * 100f) : -1;
            int sulfurPercent = showSulfur ? Mathf.RoundToInt(sulfur * 100f) : -1;
            int volcanoPercent = showVolcano ? Mathf.RoundToInt(volcano * 100f) : -1;
            if (radPercent == lastRadPercent
                && sulfurPercent == lastSulfurPercent
                && volcanoPercent == lastVolcanoPercent
                && statusLabel.gameObject.activeSelf)
            {
                return;
            }

            lastRadPercent = radPercent;
            lastSulfurPercent = sulfurPercent;
            lastVolcanoPercent = volcanoPercent;

            statusLabel.gameObject.SetActive(true);
            statusBuilder.Clear();
            if (showRad)
            {
                statusBuilder.Append("RAD ");
                statusBuilder.Append(radPercent);
                statusBuilder.Append("%  ");
            }

            if (showSulfur)
            {
                statusBuilder.Append("S ");
                statusBuilder.Append(sulfurPercent);
                statusBuilder.Append("%  ");
            }

            if (showVolcano)
            {
                statusBuilder.Append("V ");
                statusBuilder.Append(volcanoPercent);
                statusBuilder.Append('%');
            }

            string text = statusBuilder.ToString().TrimEnd();
            if (text != lastStatusText)
            {
                lastStatusText = text;
                statusLabel.text = text;
            }

            statusLabel.color = DarkMatterGenesisUiPalette.Gold;
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

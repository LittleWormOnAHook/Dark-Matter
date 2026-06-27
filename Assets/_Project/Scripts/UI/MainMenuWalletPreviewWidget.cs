using Project.Pioneers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public class MainMenuWalletPreviewWidget : MonoBehaviour
    {
        private TextMeshProUGUI statusLabel;
        private TextMeshProUGUI acLabel;
        private TextMeshProUGUI piLabel;
        private TextMeshProUGUI echoesLabel;
        private PioneerRosterManager roster;

        public void Build(Transform parent)
        {
            if (acLabel != null)
                return;

            GameObject widget = new GameObject("WalletPreviewWidget", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            widget.transform.SetParent(parent, false);

            RectTransform widgetRect = widget.GetComponent<RectTransform>();
            widgetRect.anchorMin = new Vector2(1f, 0f);
            widgetRect.anchorMax = new Vector2(1f, 0f);
            widgetRect.pivot = new Vector2(1f, 0f);
            widgetRect.anchoredPosition = new Vector2(-24f, 24f);
            widgetRect.sizeDelta = new Vector2(260f, 120f);

            Image widgetBg = widget.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(widgetBg);
            widgetBg.color = new Color(0.06f, 0.08f, 0.12f, 0.88f);

            VerticalLayoutGroup layout = widget.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            GameObject statusRow = new GameObject("StatusRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            statusRow.transform.SetParent(widget.transform, false);
            HorizontalLayoutGroup statusLayout = statusRow.GetComponent<HorizontalLayoutGroup>();
            statusLayout.spacing = 8;
            statusLayout.childAlignment = TextAnchor.MiddleLeft;

            GameObject dot = new GameObject("ConnectedDot", typeof(RectTransform), typeof(Image));
            dot.transform.SetParent(statusRow.transform, false);
            RectTransform dotRect = dot.GetComponent<RectTransform>();
            dotRect.sizeDelta = new Vector2(10f, 10f);
            Image dotImage = dot.GetComponent<Image>();
            dotImage.color = new Color(0.35f, 0.85f, 0.45f, 1f);

            statusLabel = CreateLine(statusRow.transform, "CONNECTED", 13f, FontStyles.Bold);
            statusLabel.color = new Color(0.35f, 0.85f, 0.45f, 1f);

            acLabel = CreateLine(widget.transform, "AC —", 14f);
            piLabel = CreateLine(widget.transform, "Pi —", 14f);
            echoesLabel = CreateLine(widget.transform, "Echoes —", 14f);
        }

        private void OnEnable()
        {
            roster = PioneerRosterManager.EnsureExists();
            if (roster != null)
            {
                roster.OnCurrencyChanged += Refresh;
                roster.OnRosterChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (roster != null)
            {
                roster.OnCurrencyChanged -= Refresh;
                roster.OnRosterChanged -= Refresh;
            }
        }

        public void Refresh()
        {
            roster = PioneerRosterManager.EnsureExists();
            roster?.EnsureWalletBootstrapped();

            float ac = roster != null ? roster.AetherCredits : 0f;
            float pi = roster != null ? roster.PiWalletBalance : 0f;
            int echoes = 0;
            if (roster != null)
                echoes = roster.SkilledPioneers.Count + roster.WalletOwnedPioneers.Count;

            if (acLabel != null)
                acLabel.text = $"AC {Mathf.RoundToInt(ac)}";
            if (piLabel != null)
                piLabel.text = $"Pi {Mathf.RoundToInt(pi)}";
            if (echoesLabel != null)
                echoesLabel.text = $"Echoes {echoes}";
        }

        private static TextMeshProUGUI CreateLine(Transform parent, string text, float fontSize, FontStyles style = FontStyles.Normal)
        {
            GameObject labelObj = new GameObject("Line", typeof(RectTransform));
            labelObj.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(label, semiBold: style == FontStyles.Bold);
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = new Color(0.82f, 0.88f, 0.94f, 0.95f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            return label;
        }
    }
}

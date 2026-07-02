using Project.Pioneers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project.UI
{
    public class MainMenuWalletPreviewWidget : MonoBehaviour
    {
        private const float ConnectedIconSize = 16f;

        private TextMeshProUGUI statusLabel;
        private TextMeshProUGUI acLabel;
        private TextMeshProUGUI piLabel;
        private TextMeshProUGUI echoesLabel;
        private Image connectedIcon;
        private PioneerRosterManager roster;

        public void Build(Transform parent)
        {
            if (acLabel != null)
                return;

            ShiftUiTheme theme = ShiftUiTheme.Current;

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
            widgetBg.color = SurvivalPioneerUiPalette.PanelBackground;

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
            statusLayout.childControlHeight = true;
            statusLayout.childForceExpandHeight = false;

            GameObject iconObject = new GameObject("ConnectedIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconObject.transform.SetParent(statusRow.transform, false);
            LayoutElement iconLayout = iconObject.GetComponent<LayoutElement>();
            iconLayout.preferredWidth = ConnectedIconSize;
            iconLayout.preferredHeight = ConnectedIconSize;
            iconLayout.minWidth = ConnectedIconSize;
            iconLayout.minHeight = ConnectedIconSize;

            connectedIcon = iconObject.GetComponent<Image>();
            connectedIcon.sprite = ResolveConnectedStatusSprite(theme);
            connectedIcon.preserveAspect = true;
            connectedIcon.raycastTarget = false;
            ApplyConnectedVisual(true);

            statusLabel = CreateLine(statusRow.transform, "CONNECTED", 13f, FontStyles.Bold);
            statusLabel.color = SurvivalPioneerUiPalette.ConnectedGreen;

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

            ApplyConnectedVisual(true);
        }

        private void ApplyConnectedVisual(bool connected)
        {
            if (connectedIcon == null)
                return;

            connectedIcon.sprite = ResolveConnectedStatusSprite(ShiftUiTheme.Current);
            connectedIcon.color = connected
                ? SurvivalPioneerUiPalette.ConnectedGreen
                : SurvivalPioneerUiPalette.MutedText;

            if (statusLabel != null)
                statusLabel.color = connected
                    ? SurvivalPioneerUiPalette.ConnectedGreen
                    : SurvivalPioneerUiPalette.MutedText;
        }

        private static Sprite ResolveConnectedStatusSprite(ShiftUiTheme theme)
        {
            if (theme != null && theme.connectedStatusSprite != null)
                return theme.connectedStatusSprite;

            if (ShiftUiTheme.ConnectedStatusSprite != null)
                return ShiftUiTheme.ConnectedStatusSprite;

#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UGUIKit Flat/Content/Source/Icons/119 Eye.png");
#else
            return null;
#endif
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
            label.color = SurvivalPioneerUiPalette.BodyText;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            return label;
        }
    }
}

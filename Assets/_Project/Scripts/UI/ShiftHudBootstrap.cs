using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Applies Shift styling at runtime. Survival stats panel is intentionally excluded.
    /// </summary>
    public class ShiftHudBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            ApplyShiftStyling();
        }

        public void ApplyShiftStyling()
        {
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme == null)
                return;

            ApplyNamedPanel(transform, "Hotbar", theme, large: false);
            ApplyNamedPanel(transform, "InventoryPanel", theme, large: true);
            ApplyToolbarPanel(transform, theme);
            ApplyNonStatFonts(theme);
        }

        private static void ApplyNamedPanel(Transform root, string panelName, ShiftUiTheme theme, bool large)
        {
            Transform panel = root.Find(panelName);
            if (panel == null)
                return;

            Image image = panel.GetComponent<Image>();
            if (image != null)
                theme.ApplyPanelImage(image, large, alphaMultiplier: large ? 1f : 0.92f);
        }

        private static void ApplyToolbarPanel(Transform root, ShiftUiTheme theme)
        {
            Transform toolbar = root.Find("ToolBar");
            if (toolbar == null)
                return;

            Transform backdrop = toolbar.Find("PanelBackdrop");
            if (backdrop == null)
                return;

            Image image = backdrop.GetComponent<Image>();
            if (image != null)
                theme.ApplyPanelImage(image, large: false, alphaMultiplier: 0.92f);
        }

        private void ApplyNonStatFonts(ShiftUiTheme theme)
        {
            UIManager uiManager = GetComponent<UIManager>();
            if (uiManager == null)
                return;

            ApplyLabelFont(uiManager.piBalanceText, theme);
            ApplyLabelFont(uiManager.interactionPrompt, theme);
        }

        private static void ApplyLabelFont(TextMeshProUGUI label, ShiftUiTheme theme)
        {
            if (label == null)
                return;

            theme.ApplyFont(label, semiBold: true);
            label.color = theme.primaryColor;
        }
    }
}

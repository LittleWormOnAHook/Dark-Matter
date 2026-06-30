using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Core Survival Pioneer UI palette. Use these colors for panels, borders, buttons, and text.
    /// </summary>
    public static class SurvivalPioneerUiPalette
    {
        /// <summary>Accent fill on SoftBeigeGray or WarmOffWhite surfaces only — never for text or dark-panel UI.</summary>
        public static readonly Color DeepMagenta = FromHex("#8F1E5E");
        public static readonly Color RichFuchsia = FromHex("#C02E7A");
        public static readonly Color CharcoalGray = FromHex("#2F2F2F");
        public static readonly Color SlateGray = FromHex("#4A4A5A");
        public static readonly Color WarmOffWhite = FromHex("#EDE9E4");
        public static readonly Color DarkNavy = FromHex("#1C2A38");
        public static readonly Color SoftBeigeGray = FromHex("#8C7F75");
        public static readonly Color Gold = FromHex("#D4A017");
        public static readonly Color ConnectedGreen = new Color(0.35f, 0.85f, 0.45f, 1f);
        public static readonly Color PositiveGreen = new Color(0.42f, 0.78f, 0.48f, 1f);
        public static readonly Color DangerRed = new Color(0.92f, 0.38f, 0.32f, 1f);

        public static Color PanelBackground => WithAlpha(DarkNavy, 0.94f);
        public static Color PanelHeader => WithAlpha(CharcoalGray, 0.98f);
        public static Color PanelBorder => WithAlpha(SlateGray, 0.95f);
        public static Color SlotBackground => WithAlpha(SlateGray, 0.82f);
        public static Color ButtonNormal => WithAlpha(RichFuchsia, 0.95f);
        public static Color ButtonHighlighted => RichFuchsia;
        public static Color ButtonPressed => WithAlpha(RichFuchsia, 0.72f);
        public static Color ButtonDisabled => WithAlpha(SlateGray, 0.55f);
        public static Color BodyText => WithAlpha(WarmOffWhite, 0.96f);
        public static Color MutedText => WithAlpha(SoftBeigeGray, 0.95f);
        public static Color AccentText => RichFuchsia;
        public static Color HighlightText => Gold;
        public static Color InteractionPromptText => FromHex("#F2D056");
        public static Color HotbarLabelText => Gold;
        public static Color WarningText => RichFuchsia;
        public static Color ActiveTabBackground => WithAlpha(RichFuchsia, 0.28f);
        public static Color InactiveTabBackground => WithAlpha(CharcoalGray, 0.92f);
        public static Color ScrollBackground => WithAlpha(DarkNavy, 0.88f);

        public static void ApplyThinPanelBackground(Image image, float alpha = 0.96f)
        {
            if (image == null)
                return;

            MenuUiBuilder.ApplyUiSprite(image);
            image.color = WithAlpha(CharcoalGray, alpha);
        }

        public static void ApplyPanelShellBackground(Image image, float alpha = 0.94f)
        {
            if (image == null)
                return;

            MenuUiBuilder.ApplyUiSprite(image);
            image.color = WithAlpha(DarkNavy, alpha);
        }

        public static Outline ApplyFuchsiaTrim(GameObject target, Vector2? distance = null)
        {
            if (target == null)
                return null;

            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
                outline = target.AddComponent<Outline>();

            outline.effectColor = WithAlpha(RichFuchsia, 0.72f);
            outline.effectDistance = distance ?? new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
            return outline;
        }

        public static void StylePrimaryButton(Button button, Image image, bool interactable = true)
        {
            if (image != null)
                image.color = interactable ? ButtonNormal : ButtonDisabled;

            if (button == null)
                return;

            ColorBlock colors = button.colors;
            colors.normalColor = image != null ? image.color : ButtonNormal;
            colors.highlightedColor = ButtonHighlighted;
            colors.pressedColor = ButtonPressed;
            colors.disabledColor = ButtonDisabled;
            button.colors = colors;
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static Color FromHex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;

            return Color.white;
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project.UI
{
    [CreateAssetMenu(menuName = "Project/UI/Shift UI Theme", fileName = "ShiftUiTheme")]
    public class ShiftUiTheme : ScriptableObject
    {
        public const string ResourcePath = "UI/ShiftUiTheme";
        public const string AssetPath = "Assets/_Project/Resources/UI/ShiftUiTheme.asset";

        private static ShiftUiTheme cachedTheme;

        public static ShiftUiTheme Current
        {
            get
            {
                if (cachedTheme != null)
                    return cachedTheme;

                cachedTheme = Resources.Load<ShiftUiTheme>(ResourcePath);
#if UNITY_EDITOR
                if (cachedTheme == null)
                    cachedTheme = AssetDatabase.LoadAssetAtPath<ShiftUiTheme>(AssetPath);
#endif
                return cachedTheme;
            }
        }

        public static bool IsReady => Current != null;
        public static Color PrimaryColor => Current != null ? Current.primaryColor : SurvivalPioneerUiPalette.RichFuchsia;
        public static Color SlotBackgroundTint => Current != null
            ? Current.slotBackgroundTint
            : SurvivalPioneerUiPalette.SlotBackground;
        public static Color NegativeColor => Current != null ? Current.negativeColor : SurvivalPioneerUiPalette.DangerRed;
        public static Color AccentColor => Current != null ? Current.accentColor : SurvivalPioneerUiPalette.RichFuchsia;
        public static Color HighlightColor => Current != null ? Current.highlightColor : SurvivalPioneerUiPalette.Gold;
        public static Color MutedTextColor => Current != null ? Current.mutedTextColor : SurvivalPioneerUiPalette.MutedText;
        public static Color BodyTextColor => Current != null ? Current.bodyTextColor : SurvivalPioneerUiPalette.BodyText;
        public static Color PanelHeaderColor => Current != null ? Current.panelHeaderColor : SurvivalPioneerUiPalette.PanelHeader;
        public static Sprite ConnectedStatusSprite => Current?.connectedStatusSprite;
        public static TMP_FontAsset RegularFont => Current?.regularFont;
        public static TMP_FontAsset SemiBoldFont => Current?.semiBoldFont ?? Current?.regularFont;
        public static TMP_FontAsset BoldFont => Current?.boldFont ?? Current?.semiBoldFont ?? Current?.regularFont;
        public static Sprite PanelFrame => Current?.panelFrame;
        public static Sprite PanelFrameBig => Current?.panelFrameBig ?? Current?.panelFrame;
        public static Sprite CircleGlow => Current?.circleGlow;
        public static Sprite SquareGlow => Current?.squareGlow;
        public static Sprite CircleFilled => Current?.circleFilled;
        public static Sprite CircleOutline => Current?.circleOutline;
        public static Sprite KeyCapFrame => Current?.keyCapFrame;

        public static void ResetSharedCache()
        {
            cachedTheme = null;
        }

        [Header("Colors")]
        public Color primaryColor = SurvivalPioneerUiPalette.RichFuchsia;
        public Color backgroundColor = SurvivalPioneerUiPalette.PanelBackground;
        public Color negativeColor = SurvivalPioneerUiPalette.DangerRed;
        public Color secondaryTextColor = SurvivalPioneerUiPalette.BodyText;
        public Color slotBackgroundTint = SurvivalPioneerUiPalette.SlotBackground;
        public Color selectionGlowColor = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.Gold, 0.85f);
        public Color accentColor = SurvivalPioneerUiPalette.RichFuchsia;
        public Color highlightColor = SurvivalPioneerUiPalette.Gold;
        public Color mutedTextColor = SurvivalPioneerUiPalette.MutedText;
        public Color bodyTextColor = SurvivalPioneerUiPalette.BodyText;
        public Color panelHeaderColor = SurvivalPioneerUiPalette.PanelHeader;

        [Header("Status Icons")]
        public Sprite connectedStatusSprite;

        [Header("Fonts")]
        public TMP_FontAsset regularFont;
        public TMP_FontAsset semiBoldFont;
        public TMP_FontAsset boldFont;

        [Header("Panel Frames")]
        public Sprite panelFrame;
        public Sprite panelFrameBig;

        [Header("Glow")]
        public Sprite circleGlow;
        public Sprite squareGlow;

        [Header("Circles")]
        public Sprite circleFilled;
        public Sprite circleOutline;

        [Header("Key Labels")]
        public Sprite keyCapFrame;

        public void ApplyPanelImage(Image image, bool large = false, float alphaMultiplier = 1f)
        {
            if (image == null)
                return;

            Sprite sprite = large ? panelFrameBig : panelFrame;
            if (sprite == null)
                sprite = panelFrame ?? panelFrameBig;

            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }

            Color tint = backgroundColor;
            tint.a *= alphaMultiplier;
            image.color = tint;
        }

        public void ApplySlotFrame(Image image)
        {
            if (image == null)
                return;

            if (panelFrame != null)
            {
                image.sprite = panelFrame;
                image.type = Image.Type.Sliced;
            }

            image.color = slotBackgroundTint;
        }

        public void ApplyFont(TMP_Text label, bool semiBold = false, bool bold = false)
        {
            if (label == null)
                return;

            TMP_FontAsset font = bold ? boldFont : semiBold ? semiBoldFont : regularFont;
            if (font == null)
                font = regularFont;

            if (font != null && label.font != font)
                label.font = font;

            label.ForceMeshUpdate();
        }

        public TextMeshProUGUI CreateKeyLabel(
            Transform parent,
            string keyText,
            Vector2 anchoredPosition,
            Vector2 size,
            bool showFrame = true)
        {
            GameObject root = new GameObject("KeyLabel", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = anchoredPosition;
            rootRect.sizeDelta = size;

            if (showFrame && keyCapFrame != null)
            {
                Image frame = root.AddComponent<Image>();
                frame.sprite = keyCapFrame;
                frame.type = Image.Type.Sliced;
                frame.color = new Color(1f, 1f, 1f, 0.92f);
                frame.raycastTarget = false;
            }

            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(root.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            ApplyFont(label, semiBold: true);
            label.text = keyText;
            label.fontSize = 13f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = primaryColor;
            label.raycastTarget = false;

            return label;
        }

        public Image EnsureSelectionGlow(Transform slotTransform, ref Image glowImage)
        {
            if (glowImage != null)
                return glowImage;

            Transform existing = slotTransform.Find("SelectionGlow");
            if (existing != null)
            {
                glowImage = existing.GetComponent<Image>();
                if (glowImage != null)
                    return glowImage;
            }

            GameObject glowObject = new GameObject("SelectionGlow", typeof(RectTransform));
            glowObject.transform.SetParent(slotTransform, false);
            glowObject.transform.SetAsFirstSibling();

            RectTransform glowRect = glowObject.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-4f, -4f);
            glowRect.offsetMax = new Vector2(4f, 4f);

            glowImage = glowObject.AddComponent<Image>();
            glowImage.sprite = circleGlow != null ? circleGlow : squareGlow;
            glowImage.color = selectionGlowColor;
            glowImage.raycastTarget = false;
            glowImage.enabled = false;
            return glowImage;
        }
    }
}

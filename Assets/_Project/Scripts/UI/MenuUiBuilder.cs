using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Project.UI
{
    internal static class MenuUiBuilder
    {
        public const float SubMenuScale = 1.5f;

        private static Sprite uiSprite;

        public static float ScaledSize(float value) => value * SubMenuScale;

        public static int ScaledSizeInt(float value) => Mathf.RoundToInt(value * SubMenuScale);

        private static Sprite GetUiSprite()
        {
            if (uiSprite != null)
                return uiSprite;

            Sprite shiftPanel = ShiftUiTheme.PanelFrame ?? ShiftUiTheme.PanelFrameBig;
            if (shiftPanel != null)
            {
                uiSprite = shiftPanel;
                return uiSprite;
            }

            // Avoid Resources.GetBuiltinResource — it logs errors on many Unity versions when missing.
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            uiSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 100f);
            uiSprite.name = "MenuUiWhiteSprite";
            return uiSprite;
        }

        public static void ApplyUiSprite(Image image)
        {
            if (image == null)
                return;

            image.sprite = GetUiSprite();
            image.type = ShiftUiTheme.PanelFrame != null ? Image.Type.Sliced : Image.Type.Simple;
        }

        public static void StretchRectToFill(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        /// <summary>Returns the Header/Title TMP on a shell from <see cref="CreateFullscreenShell"/>.</summary>
        public static TextMeshProUGUI GetShellTitleText(GameObject shell)
        {
            if (shell == null)
                return null;

            Transform header = shell.transform.Find("Header");
            if (header == null)
                return null;

            Transform titleTransform = header.Find("Title");
            return titleTransform != null ? titleTransform.GetComponent<TextMeshProUGUI>() : null;
        }

        public static GameObject CreateFullScreenPanel(Transform parent, string name, Color backgroundColor, bool blockRaycasts = false)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = panel.GetComponent<Image>();
            ApplyUiSprite(image);
            image.color = backgroundColor;
            image.raycastTarget = blockRaycasts;
            return panel;
        }

        public static Button CreateCircleCloseButton(Transform parent, float size, UnityAction onClick = null)
        {
            GameObject buttonObject = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(size, size);

            Image circleImage = buttonObject.GetComponent<Image>();
            Sprite circleSprite = ShiftUiTheme.CircleOutline ?? ShiftUiTheme.CircleFilled;
            if (circleSprite != null)
            {
                circleImage.sprite = circleSprite;
                circleImage.type = Image.Type.Simple;
            }
            else
            {
                ApplyUiSprite(circleImage);
            }

            Color normalColor = DarkMatterGenesisUiPalette.BodyText;
            circleImage.color = normalColor;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = DarkMatterGenesisUiPalette.RichFuchsia;
            colors.pressedColor = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.72f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            UiSoundHelper.BindButton(button);
            if (onClick != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(onClick);
            }

            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(text);
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(text, semiBold: true);
            text.text = "X";
            text.fontSize = Mathf.Max(12f, size * 0.42f);
            text.fontStyle = FontStyles.Bold;
            text.color = DarkMatterGenesisUiPalette.BodyText;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        /// <summary>
        /// Standard top-right "Back" button for main-menu sub-panels (Settings, Save/Load, etc). Parent
        /// this to the sub-panel's own root — NOT the VerticalLayoutGroup-driven window content inside
        /// it — so it stays pinned to the corner regardless of what's laid out below it.
        /// </summary>
        public static Button CreateTopRightBackButton(
            Transform panelRoot,
            UnityAction onClick,
            float width = -1f,
            float height = -1f,
            float fontSize = -1f,
            float inset = -1f)
        {
            float resolvedWidth = width > 0f ? width : ScaledSize(120f);
            float resolvedHeight = height > 0f ? height : ScaledSize(44f);
            float resolvedFont = fontSize > 0f ? fontSize : ScaledSize(18f);
            float resolvedInset = inset >= 0f ? inset : ScaledSize(20f);

            Button button = CreateButton(
                panelRoot,
                "Back",
                new Vector2(resolvedWidth, resolvedHeight),
                resolvedFont);

            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-resolvedInset, -resolvedInset);

            if (onClick != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(onClick);
            }

            button.transform.SetAsLastSibling();
            return button;
        }

        /// <summary>Minimal close control — text X only, no circle frame.</summary>
        public static Button CreateTextCloseButton(Transform parent, float fontSize, UnityAction onClick = null)
        {
            float hitSize = Mathf.Max(fontSize + 8f, 22f);
            GameObject buttonObject = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(hitSize, hitSize);

            Image hitArea = buttonObject.GetComponent<Image>();
            hitArea.color = new Color(0f, 0f, 0f, 0.001f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.85f);
            colors.pressedColor = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.72f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;

            UiSoundHelper.BindButton(button);
            if (onClick != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(onClick);
            }

            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(text);
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(text, semiBold: true);
            text.text = "X";
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = DarkMatterGenesisUiPalette.BodyText;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        public static Button CreateButton(Transform parent, string label, Vector2 size, float fontSize = 36f)
        {
            GameObject buttonObject = new GameObject(label + "Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            LayoutElement layoutElement = buttonObject.AddComponent<LayoutElement>();
            layoutElement.minWidth = size.x;
            layoutElement.preferredWidth = size.x;
            layoutElement.minHeight = size.y;
            layoutElement.preferredHeight = size.y;

            Image image = buttonObject.GetComponent<Image>();
            ApplyUiSprite(image);
            image.color = DarkMatterGenesisUiPalette.ButtonNormal;
            DarkMatterGenesisUiPalette.ApplyFuchsiaTrim(buttonObject);

            Button button = buttonObject.GetComponent<Button>();
            DarkMatterGenesisUiPalette.StylePrimaryButton(button, image);

            UiSoundHelper.BindButton(button);

            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(text);
            text.text = label;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = DarkMatterGenesisUiPalette.BodyText;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        public static TextMeshProUGUI CreateTitle(Transform parent, string text, float fontSize)
        {
            GameObject titleObject = new GameObject("Title", typeof(RectTransform));
            titleObject.transform.SetParent(parent, false);

            TextMeshProUGUI title = titleObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(title);
            title.text = text;
            title.fontSize = fontSize;
            title.fontStyle = FontStyles.Bold;
            title.color = DarkMatterGenesisUiPalette.BodyText;
            title.alignment = TextAlignmentOptions.Center;
            title.raycastTarget = false;

            LayoutElement layout = titleObject.AddComponent<LayoutElement>();
            layout.minHeight = fontSize + 12f;
            return title;
        }

        /// <summary>Visual-only panel header row (no drag/resize).</summary>
        public static GameObject CreatePanelTitleBar(Transform parent, string title, float height, float fontSize = 12f)
        {
            GameObject titleBarObject = new GameObject("TitleBar", typeof(RectTransform));
            titleBarObject.transform.SetParent(parent, false);

            LayoutElement layout = titleBarObject.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;

            Image titleBarBg = titleBarObject.AddComponent<Image>();
            ApplyUiSprite(titleBarBg);
            DarkMatterGenesisUiPalette.ApplyThinPanelBackground(titleBarBg, 0.95f);
            DarkMatterGenesisUiPalette.ApplyFuchsiaTrim(titleBarObject);
            titleBarBg.raycastTarget = false;

            GameObject titleObject = new GameObject("Title", typeof(RectTransform));
            titleObject.transform.SetParent(titleBarObject.transform, false);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(8f, 0f);
            titleRect.offsetMax = new Vector2(-8f, 0f);

            TextMeshProUGUI label = titleObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = title;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = DarkMatterGenesisUiPalette.BodyText;
            label.raycastTarget = false;

            return titleBarObject;
        }

        public static Slider CreateSliderRow(
            Transform parent,
            string label,
            float initialValue,
            out TextMeshProUGUI valueLabel,
            float handleWidth = 10f,
            float handleHeight = 22f)
        {
            const float TrackHeight = 20f;
            float HandleWidth = Mathf.Max(4f, handleWidth);
            // Only slightly taller than the gold track.
            float HandleHeight = Mathf.Max(TrackHeight + 2f, handleHeight);

            GameObject row = new GameObject(label + "Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);

            VerticalLayoutGroup rowLayout = row.AddComponent<VerticalLayoutGroup>();
            rowLayout.spacing = 2;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            GameObject labelRow = new GameObject("LabelRow", typeof(RectTransform));
            labelRow.transform.SetParent(row.transform, false);
            HorizontalLayoutGroup labelLayout = labelRow.AddComponent<HorizontalLayoutGroup>();
            labelLayout.childAlignment = TextAnchor.MiddleLeft;
            labelLayout.childControlWidth = true;
            labelLayout.childForceExpandWidth = true;

            LayoutElement labelRowLayout = labelRow.AddComponent<LayoutElement>();
            labelRowLayout.minHeight = 18f;
            labelRowLayout.preferredHeight = 18f;
            labelRowLayout.flexibleHeight = 0f;

            TextMeshProUGUI nameLabel = CreateRowLabel(labelRow.transform, label, 14, TextAlignmentOptions.MidlineLeft);
            LayoutElement nameLayout = nameLabel.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;

            valueLabel = CreateRowLabel(labelRow.transform, "100%", 14, TextAlignmentOptions.MidlineRight);
            LayoutElement valueLayout = valueLabel.gameObject.AddComponent<LayoutElement>();
            valueLayout.minWidth = 44f;
            valueLayout.preferredWidth = 44f;

            GameObject sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(row.transform, false);
            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initialValue;

            LayoutElement sliderLayout = sliderObject.AddComponent<LayoutElement>();
            sliderLayout.minHeight = HandleHeight;
            sliderLayout.preferredHeight = HandleHeight;
            sliderLayout.flexibleHeight = 0f;

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(sliderObject.transform, false);
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = null;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.CharcoalGray, 1f);
            backgroundImage.raycastTarget = true;
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            backgroundRect.sizeDelta = new Vector2(0f, TrackHeight);
            backgroundRect.anchoredPosition = Vector2.zero;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
            fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
            fillAreaRect.sizeDelta = new Vector2(-HandleWidth, TrackHeight);
            fillAreaRect.anchoredPosition = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.sprite = null;
            fillImage.type = Image.Type.Simple;
            fillImage.color = DarkMatterGenesisUiPalette.Gold;
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            GameObject handleSlideArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleSlideArea.transform.SetParent(sliderObject.transform, false);
            RectTransform handleAreaRect = handleSlideArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0f, 0.5f);
            handleAreaRect.anchorMax = new Vector2(1f, 0.5f);
            handleAreaRect.pivot = new Vector2(0.5f, 0.5f);
            handleAreaRect.sizeDelta = new Vector2(0f, HandleHeight);
            handleAreaRect.anchoredPosition = Vector2.zero;

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleSlideArea.transform, false);
            Image handleImage = handle.GetComponent<Image>();
            handleImage.sprite = null;
            handleImage.type = Image.Type.Simple;
            handleImage.color = Color.white;
            handleImage.raycastTarget = true;
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(HandleWidth, HandleHeight);
            handleRect.anchoredPosition = Vector2.zero;

            LayoutElement handleLayout = handle.AddComponent<LayoutElement>();
            handleLayout.ignoreLayout = true;
            handleLayout.minWidth = HandleWidth;
            handleLayout.minHeight = HandleHeight;
            handleLayout.preferredWidth = HandleWidth;
            handleLayout.preferredHeight = HandleHeight;

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            LayoutElement rowLayoutElement = row.AddComponent<LayoutElement>();
            rowLayoutElement.minHeight = 44f;
            rowLayoutElement.preferredHeight = 44f;
            rowLayoutElement.flexibleHeight = 0f;
            return slider;
        }

        public static Toggle CreateToggleRow(Transform parent, string label, bool initialValue)
        {
            const float boxSize = 10f;

            GameObject row = new GameObject(label + "ToggleRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI nameLabel = CreateRowLabel(row.transform, label, 14, TextAlignmentOptions.MidlineLeft);
            LayoutElement nameLayout = nameLabel.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;
            nameLayout.minWidth = 80f;

            GameObject toggleObject = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle), typeof(LayoutElement));
            toggleObject.transform.SetParent(row.transform, false);
            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.isOn = initialValue;

            LayoutElement toggleLayout = toggleObject.GetComponent<LayoutElement>();
            toggleLayout.minWidth = boxSize;
            toggleLayout.preferredWidth = boxSize;
            toggleLayout.minHeight = boxSize;
            toggleLayout.preferredHeight = boxSize;
            toggleLayout.flexibleWidth = 0f;
            toggleLayout.flexibleHeight = 0f;

            // Dark empty square; gold fill when on (matches old Settings — no TMP glyphs / font warnings).
            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(toggleObject.transform, false);
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = null;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.CharcoalGray, 1f);
            StretchRectToFill(background.GetComponent<RectTransform>());

            GameObject fill = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(background.transform, false);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.sprite = null;
            fillImage.type = Image.Type.Simple;
            fillImage.color = DarkMatterGenesisUiPalette.Gold;
            StretchRectToFill(fill.GetComponent<RectTransform>());

            toggle.graphic = fillImage;
            toggle.targetGraphic = backgroundImage;
            RefreshToggleVisual(toggle);

            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 26f;
            rowLayout.preferredHeight = 26f;
            rowLayout.flexibleWidth = 1f;
            return toggle;
        }

        /// <summary>Filled circle when on, outline circle when off.</summary>
        public static Toggle CreateCircleToggleRow(Transform parent, string label, bool initialValue)
        {
            GameObject row = new GameObject(label + "CircleToggleRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            GameObject toggleObject = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
            toggleObject.transform.SetParent(row.transform, false);
            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.isOn = initialValue;

            LayoutElement toggleLayout = toggleObject.AddComponent<LayoutElement>();
            toggleLayout.minWidth = 20f;
            toggleLayout.preferredWidth = 20f;
            toggleLayout.minHeight = 20f;
            toggleLayout.preferredHeight = 20f;

            GameObject outlineObject = new GameObject("Outline", typeof(RectTransform), typeof(Image));
            outlineObject.transform.SetParent(toggleObject.transform, false);
            Image outlineImage = outlineObject.GetComponent<Image>();
            Sprite outlineSprite = ShiftUiTheme.CircleOutline ?? ShiftUiTheme.CircleFilled;
            if (outlineSprite != null)
            {
                outlineImage.sprite = outlineSprite;
                outlineImage.type = Image.Type.Simple;
            }
            else
            {
                ApplyUiSprite(outlineImage);
            }

            outlineImage.color = DarkMatterGenesisUiPalette.BodyText;
            RectTransform outlineRect = outlineObject.GetComponent<RectTransform>();
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = Vector2.zero;
            outlineRect.offsetMax = Vector2.zero;

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(toggleObject.transform, false);
            Image fillImage = fillObject.GetComponent<Image>();
            Sprite fillSprite = ShiftUiTheme.CircleFilled ?? ShiftUiTheme.CircleOutline;
            if (fillSprite != null)
            {
                fillImage.sprite = fillSprite;
                fillImage.type = Image.Type.Simple;
            }
            else
            {
                ApplyUiSprite(fillImage);
            }

            fillImage.color = DarkMatterGenesisUiPalette.RichFuchsia;
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0.12f, 0.12f);
            fillRect.anchorMax = new Vector2(0.88f, 0.88f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            toggle.graphic = fillImage;
            toggle.targetGraphic = outlineImage;

            CreateRowLabel(row.transform, label, 14, TextAlignmentOptions.MidlineLeft);
            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 26f;
            rowLayout.preferredHeight = 26f;
            return toggle;
        }

        /// <summary>Syncs 10×10 tick boxes: empty charcoal when off, solid gold fill when on.</summary>
        public static void RefreshToggleVisual(Toggle toggle)
        {
            if (toggle == null)
                return;

            if (toggle.graphic != null)
            {
                toggle.graphic.enabled = toggle.isOn;
                toggle.graphic.gameObject.SetActive(true);
                if (toggle.graphic is Image fillImage)
                    fillImage.color = DarkMatterGenesisUiPalette.Gold;
            }

            Transform background = toggle.transform.Find("Background");
            if (background != null && background.TryGetComponent(out Image backgroundImage))
            {
                backgroundImage.color = DarkMatterGenesisUiPalette.WithAlpha(
                    DarkMatterGenesisUiPalette.CharcoalGray,
                    1f);
            }
        }

        public static Dropdown CreateDropdownRow(Transform parent, string label)
        {
            // ~30% smaller than the previous 170×24 control chrome.
            const float dropdownWidth = 119f;
            const float dropdownHeight = 17f;

            GameObject row = new GameObject(label + "DropdownRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI nameLabel = CreateRowLabel(row.transform, label, 14, TextAlignmentOptions.MidlineLeft);
            LayoutElement nameLayout = nameLabel.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;
            nameLayout.minWidth = 80f;

            GameObject dropdownObject = new GameObject("Dropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown), typeof(LayoutElement));
            dropdownObject.transform.SetParent(row.transform, false);
            Image dropdownImage = dropdownObject.GetComponent<Image>();
            dropdownImage.sprite = null;
            dropdownImage.type = Image.Type.Simple;
            dropdownImage.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.CharcoalGray, 1f);
            Outline dropdownOutline = dropdownObject.AddComponent<Outline>();
            dropdownOutline.effectColor = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.BodyText, 0.45f);
            dropdownOutline.effectDistance = new Vector2(1f, -1f);
            LayoutElement dropdownLayout = dropdownObject.GetComponent<LayoutElement>();
            dropdownLayout.minHeight = dropdownHeight;
            dropdownLayout.preferredHeight = dropdownHeight;
            dropdownLayout.minWidth = dropdownWidth;
            dropdownLayout.preferredWidth = dropdownWidth;
            dropdownLayout.flexibleWidth = 0f;
            RectTransform dropdownRect = dropdownObject.GetComponent<RectTransform>();
            dropdownRect.sizeDelta = new Vector2(dropdownWidth, dropdownHeight);

            Dropdown dropdown = dropdownObject.GetComponent<Dropdown>();
            dropdown.targetGraphic = dropdownImage;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(dropdownObject.transform, false);
            Text labelText = labelObject.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.raycastTarget = false;
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelText.fontSize = 11;
            labelRect.offsetMin = new Vector2(4f, 0f);
            labelRect.offsetMax = new Vector2(-14f, 0f);
            dropdown.captionText = labelText;

            GameObject arrowObject = new GameObject("Arrow", typeof(RectTransform));
            arrowObject.transform.SetParent(dropdownObject.transform, false);
            Text arrowText = arrowObject.AddComponent<Text>();
            arrowText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            arrowText.text = "v";
            arrowText.fontSize = 9;
            arrowText.color = Color.white;
            arrowText.alignment = TextAnchor.MiddleCenter;
            arrowText.raycastTarget = false;
            RectTransform arrowRect = arrowObject.GetComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1f, 0f);
            arrowRect.anchorMax = new Vector2(1f, 1f);
            arrowRect.pivot = new Vector2(1f, 0.5f);
            arrowRect.sizeDelta = new Vector2(12f, 0f);
            arrowRect.anchoredPosition = Vector2.zero;

            GameObject template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            template.transform.SetParent(dropdownObject.transform, false);
            template.SetActive(false);
            Image templateImage = template.GetComponent<Image>();
            ApplyUiSprite(templateImage);
            templateImage.color = DarkMatterGenesisUiPalette.ScrollBackground;
            RectTransform templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, 2f);
            templateRect.sizeDelta = new Vector2(0f, 120f);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(template.transform, false);
            Image viewportImage = viewport.GetComponent<Image>();
            ApplyUiSprite(viewportImage);
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 22f);

            GameObject item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(content.transform, false);
            RectTransform itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 22f);

            GameObject itemBackground = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
            itemBackground.transform.SetParent(item.transform, false);
            Image itemBackgroundImage = itemBackground.GetComponent<Image>();
            ApplyUiSprite(itemBackgroundImage);
            itemBackgroundImage.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.CharcoalGray, 1f);
            RectTransform itemBackgroundRect = itemBackground.GetComponent<RectTransform>();
            itemBackgroundRect.anchorMin = Vector2.zero;
            itemBackgroundRect.anchorMax = Vector2.one;
            itemBackgroundRect.offsetMin = Vector2.zero;
            itemBackgroundRect.offsetMax = Vector2.zero;

            GameObject itemLabelObject = new GameObject("Item Label", typeof(RectTransform));
            itemLabelObject.transform.SetParent(item.transform, false);
            Text itemLabel = itemLabelObject.AddComponent<Text>();
            itemLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itemLabel.fontSize = 14;
            itemLabel.color = DarkMatterGenesisUiPalette.BodyText;
            itemLabel.alignment = TextAnchor.MiddleLeft;
            itemLabel.raycastTarget = false;
            RectTransform itemLabelRect = itemLabelObject.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(8f, 0f);
            itemLabelRect.offsetMax = new Vector2(-8f, 0f);

            Toggle itemToggle = item.GetComponent<Toggle>();
            itemToggle.targetGraphic = itemBackgroundImage;
            itemToggle.isOn = true;

            ScrollRect scrollRect = template.GetComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            dropdown.template = templateRect;
            dropdown.itemText = itemLabel;

            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 28f;
            rowLayout.preferredHeight = 28f;
            rowLayout.flexibleWidth = 1f;
            return dropdown;
        }

        public static Button CreateTiltedMenuButton(
            Transform parent,
            string label,
            Vector2 size,
            float fontSize,
            float yRotationDegrees = -6f)
        {
            Button button = CreateButton(parent, label, size, fontSize);
            button.transform.localRotation = Quaternion.Euler(0f, yRotationDegrees, 0f);
            return button;
        }

        public static GameObject CreateFullscreenShell(
            Transform parent,
            string title,
            out RectTransform contentArea,
            out Button closeButton)
        {
            GameObject shell = CreateFullScreenPanel(parent, title + "Shell", DarkMatterGenesisUiPalette.PanelBackground, blockRaycasts: true);
            BuildModalShellInterior(shell.transform, title, FullscreenUiWindow.HeaderHeight, 26f, out contentArea, out closeButton);
            return shell;
        }

        public static GameObject CreateCenteredModalShell(
            Transform parent,
            string title,
            Vector2 size,
            out RectTransform contentArea,
            out Button closeButton)
        {
            GameObject shell = new GameObject(title + "Shell", typeof(RectTransform), typeof(Image));
            shell.transform.SetParent(parent, false);

            Image shellBg = shell.GetComponent<Image>();
            ApplyUiSprite(shellBg);
            DarkMatterGenesisUiPalette.ApplyPanelShellBackground(shellBg, 0.98f);
            DarkMatterGenesisUiPalette.ApplyFuchsiaTrim(shell);
            shellBg.raycastTarget = true;

            ApplyCenteredModalShellLayout(shell, size);
            BuildModalShellInterior(shell.transform, title, GameplayHudLayout.ModalHeaderHeight, 22f, out contentArea, out closeButton);
            return shell;
        }

        public static void ApplyCenteredModalShellLayout(GameObject shell, Vector2 size)
        {
            if (shell == null)
                return;

            RectTransform shellRect = shell.GetComponent<RectTransform>();
            shellRect.anchorMin = new Vector2(0.5f, 0.5f);
            shellRect.anchorMax = new Vector2(0.5f, 0.5f);
            shellRect.pivot = new Vector2(0.5f, 0.5f);
            shellRect.sizeDelta = size;
            shellRect.anchoredPosition = Vector2.zero;

            Transform content = shell.transform.Find("Content");
            if (content is RectTransform contentRect)
            {
                contentRect.anchorMin = Vector2.zero;
                contentRect.anchorMax = Vector2.one;
                contentRect.offsetMin = Vector2.zero;
                contentRect.offsetMax = new Vector2(0f, -GameplayHudLayout.ModalHeaderHeight);
            }

            Transform header = shell.transform.Find("Header");
            if (header is RectTransform headerRect)
            {
                headerRect.anchorMin = new Vector2(0f, 1f);
                headerRect.anchorMax = new Vector2(1f, 1f);
                headerRect.pivot = new Vector2(0.5f, 1f);
                headerRect.sizeDelta = new Vector2(0f, GameplayHudLayout.ModalHeaderHeight);
            }
        }

        private static void BuildModalShellInterior(
            Transform shellTransform,
            string title,
            float headerHeight,
            float titleFontSize,
            out RectTransform contentArea,
            out Button closeButton)
        {
            GameObject header = new GameObject("Header", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(shellTransform, false);
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, headerHeight);

            Image headerBg = header.GetComponent<Image>();
            ApplyUiSprite(headerBg);
            headerBg.color = DarkMatterGenesisUiPalette.PanelHeader;

            GameObject titleObject = new GameObject("Title", typeof(RectTransform));
            titleObject.transform.SetParent(header.transform, false);
            TextMeshProUGUI titleText = titleObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(titleText);
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(titleText, bold: true);
            titleText.text = title;
            titleText.fontSize = titleFontSize;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = DarkMatterGenesisUiPalette.BodyText;
            titleText.alignment = TextAlignmentOptions.TopLeft;
            titleText.raycastTarget = false;

            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(16f, 6f);
            titleRect.offsetMax = new Vector2(-48f, -6f);

            closeButton = CreateCircleCloseButton(header.transform, 28f);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-12f, 0f);

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(shellTransform, false);
            contentArea = content.GetComponent<RectTransform>();
            contentArea.anchorMin = Vector2.zero;
            contentArea.anchorMax = Vector2.one;
            contentArea.offsetMin = Vector2.zero;
            contentArea.offsetMax = new Vector2(0f, -headerHeight);
        }

        public static Button CreateLaunchTile(Transform parent, string label, Vector2 size)
        {
            GameObject tileObject = new GameObject(label + "Tile", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            tileObject.transform.SetParent(parent, false);

            RectTransform rect = tileObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;

            LayoutElement layoutElement = tileObject.AddComponent<LayoutElement>();
            layoutElement.minWidth = size.x;
            layoutElement.preferredWidth = size.x;
            layoutElement.minHeight = size.y;
            layoutElement.preferredHeight = size.y;

            Image image = tileObject.GetComponent<Image>();
            ApplyUiSprite(image);
            image.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.CharcoalGray, 0.96f);

            Button button = tileObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = DarkMatterGenesisUiPalette.ButtonHighlighted;
            colors.pressedColor = DarkMatterGenesisUiPalette.ButtonPressed;
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            UiSoundHelper.BindButton(button);

            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(tileObject.transform, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(text);
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(text, semiBold: true);
            text.text = label;
            text.fontSize = 18f;
            text.fontStyle = FontStyles.Bold;
            text.color = DarkMatterGenesisUiPalette.BodyText;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        private static TextMeshProUGUI CreateRowLabel(Transform parent, string text, float size, TextAlignmentOptions alignment)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = text;
            label.fontSize = size;
            label.color = DarkMatterGenesisUiPalette.BodyText;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }
    }
}

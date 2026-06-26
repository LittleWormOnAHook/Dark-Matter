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

            Color normalColor = new Color(0.82f, 0.86f, 0.92f, 0.95f);
            circleImage.color = normalColor;

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.62f, 0.66f, 0.72f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            UiSoundHelper.BindButton(button);
            if (onClick != null)
                button.onClick.AddListener(onClick);

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
            text.color = new Color(0.92f, 0.94f, 0.98f, 1f);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

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
            colors.highlightedColor = new Color(1f, 0.85f, 0.85f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;

            UiSoundHelper.BindButton(button);
            if (onClick != null)
                button.onClick.AddListener(onClick);

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
            text.color = new Color(0.88f, 0.9f, 0.94f, 0.95f);
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
            image.color = new Color(0.16f, 0.18f, 0.24f, 0.95f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.24f, 0.28f, 0.36f, 1f);
            colors.pressedColor = new Color(0.1f, 0.12f, 0.16f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            UiSoundHelper.BindButton(button);

            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(buttonObject.transform, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(text);
            text.text = label;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
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
            title.color = Color.white;
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
            titleBarBg.color = new Color(0.1f, 0.12f, 0.16f, 0.95f);
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
            label.color = new Color(0.78f, 0.84f, 0.92f, 1f);
            label.raycastTarget = false;

            return titleBarObject;
        }

        public static Slider CreateSliderRow(Transform parent, string label, float initialValue, out TextMeshProUGUI valueLabel)
        {
            GameObject row = new GameObject(label + "Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);

            VerticalLayoutGroup rowLayout = row.AddComponent<VerticalLayoutGroup>();
            rowLayout.spacing = 4;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = true;

            GameObject labelRow = new GameObject("LabelRow", typeof(RectTransform));
            labelRow.transform.SetParent(row.transform, false);
            HorizontalLayoutGroup labelLayout = labelRow.AddComponent<HorizontalLayoutGroup>();
            labelLayout.childAlignment = TextAnchor.MiddleLeft;
            labelLayout.childControlWidth = true;
            labelLayout.childForceExpandWidth = true;

            TextMeshProUGUI nameLabel = CreateRowLabel(labelRow.transform, label, 18, TextAlignmentOptions.MidlineLeft);
            LayoutElement nameLayout = nameLabel.gameObject.AddComponent<LayoutElement>();
            nameLayout.flexibleWidth = 1f;

            valueLabel = CreateRowLabel(labelRow.transform, "100%", 18, TextAlignmentOptions.MidlineRight);
            LayoutElement valueLayout = valueLabel.gameObject.AddComponent<LayoutElement>();
            valueLayout.minWidth = 70f;

            GameObject sliderObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderObject.transform.SetParent(row.transform, false);
            Slider slider = sliderObject.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initialValue;

            LayoutElement sliderLayout = sliderObject.AddComponent<LayoutElement>();
            sliderLayout.minHeight = 24f;

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(sliderObject.transform, false);
            Image backgroundImage = background.GetComponent<Image>();
            ApplyUiSprite(backgroundImage);
            backgroundImage.color = new Color(0.1f, 0.1f, 0.12f, 1f);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(8f, 0f);
            fillAreaRect.offsetMax = new Vector2(-8f, 0f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Image fillImage = fill.GetComponent<Image>();
            ApplyUiSprite(fillImage);
            fillImage.color = new Color(0.85f, 0.68f, 0.18f, 1f);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            GameObject handleSlideArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleSlideArea.transform.SetParent(sliderObject.transform, false);
            RectTransform handleAreaRect = handleSlideArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleSlideArea.transform, false);
            Image handleImage = handle.GetComponent<Image>();
            ApplyUiSprite(handleImage);
            handleImage.color = Color.white;
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(16f, 16f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            LayoutElement rowLayoutElement = row.AddComponent<LayoutElement>();
            rowLayoutElement.minHeight = 56f;
            return slider;
        }

        public static Toggle CreateToggleRow(Transform parent, string label, bool initialValue)
        {
            GameObject row = new GameObject(label + "ToggleRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            GameObject toggleObject = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
            toggleObject.transform.SetParent(row.transform, false);
            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.isOn = initialValue;

            LayoutElement toggleLayout = toggleObject.AddComponent<LayoutElement>();
            toggleLayout.minWidth = 28f;
            toggleLayout.minHeight = 28f;

            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(toggleObject.transform, false);
            Image backgroundImage = background.GetComponent<Image>();
            ApplyUiSprite(backgroundImage);
            backgroundImage.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkmark.transform.SetParent(background.transform, false);
            Image checkImage = checkmark.GetComponent<Image>();
            ApplyUiSprite(checkImage);
            checkImage.color = new Color(0.85f, 0.68f, 0.18f, 1f);
            RectTransform checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.15f, 0.15f);
            checkRect.anchorMax = new Vector2(0.85f, 0.85f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;

            toggle.graphic = checkImage;
            toggle.targetGraphic = backgroundImage;

            CreateRowLabel(row.transform, label, 18, TextAlignmentOptions.MidlineLeft);
            LayoutElement rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.minHeight = 36f;
            return toggle;
        }

        public static Dropdown CreateDropdownRow(Transform parent, string label)
        {
            GameObject row = new GameObject(label + "DropdownRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);

            VerticalLayoutGroup layout = row.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            CreateRowLabel(row.transform, label, 18, TextAlignmentOptions.MidlineLeft);

            GameObject dropdownObject = new GameObject("Dropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown));
            dropdownObject.transform.SetParent(row.transform, false);
            Image dropdownImage = dropdownObject.GetComponent<Image>();
            ApplyUiSprite(dropdownImage);
            dropdownImage.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            RectTransform dropdownRect = dropdownObject.GetComponent<RectTransform>();
            dropdownRect.sizeDelta = new Vector2(0f, 36f);

            Dropdown dropdown = dropdownObject.GetComponent<Dropdown>();
            dropdown.targetGraphic = dropdownImage;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(dropdownObject.transform, false);
            Text labelText = labelObject.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.raycastTarget = false;
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 0f);
            labelRect.offsetMax = new Vector2(-28f, 0f);
            dropdown.captionText = labelText;

            GameObject template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            template.transform.SetParent(dropdownObject.transform, false);
            template.SetActive(false);
            Image templateImage = template.GetComponent<Image>();
            ApplyUiSprite(templateImage);
            templateImage.color = new Color(0.1f, 0.1f, 0.12f, 0.98f);
            RectTransform templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, 2f);
            templateRect.sizeDelta = new Vector2(0f, 150f);

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
            contentRect.sizeDelta = new Vector2(0f, 28f);

            GameObject item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(content.transform, false);
            RectTransform itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 28f);

            GameObject itemBackground = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
            itemBackground.transform.SetParent(item.transform, false);
            Image itemBackgroundImage = itemBackground.GetComponent<Image>();
            ApplyUiSprite(itemBackgroundImage);
            itemBackgroundImage.color = new Color(0.14f, 0.14f, 0.16f, 1f);
            RectTransform itemBackgroundRect = itemBackground.GetComponent<RectTransform>();
            itemBackgroundRect.anchorMin = Vector2.zero;
            itemBackgroundRect.anchorMax = Vector2.one;
            itemBackgroundRect.offsetMin = Vector2.zero;
            itemBackgroundRect.offsetMax = Vector2.zero;

            GameObject itemLabelObject = new GameObject("Item Label", typeof(RectTransform));
            itemLabelObject.transform.SetParent(item.transform, false);
            Text itemLabel = itemLabelObject.AddComponent<Text>();
            itemLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itemLabel.color = Color.white;
            itemLabel.alignment = TextAnchor.MiddleLeft;
            itemLabel.raycastTarget = false;
            RectTransform itemLabelRect = itemLabelObject.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(12f, 0f);
            itemLabelRect.offsetMax = new Vector2(-12f, 0f);

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
            rowLayout.minHeight = 72f;
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
            GameObject shell = CreateFullScreenPanel(parent, title + "Shell", new Color(0.05f, 0.06f, 0.09f, 0.98f), blockRaycasts: true);
            RectTransform shellRect = shell.GetComponent<RectTransform>();

            GameObject header = new GameObject("Header", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(shell.transform, false);
            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, FullscreenUiWindow.HeaderHeight);

            Image headerBg = header.GetComponent<Image>();
            ApplyUiSprite(headerBg);
            headerBg.color = new Color(0.08f, 0.1f, 0.14f, 0.98f);

            GameObject titleObject = new GameObject("Title", typeof(RectTransform));
            titleObject.transform.SetParent(header.transform, false);
            TextMeshProUGUI titleText = titleObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(titleText);
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(titleText, bold: true);
            titleText.text = title;
            titleText.fontSize = 26f;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            titleText.raycastTarget = false;

            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(20f, 0f);
            titleRect.offsetMax = new Vector2(-56f, 0f);

            closeButton = CreateCircleCloseButton(header.transform, 32f);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-16f, 0f);

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(shell.transform, false);
            contentArea = content.GetComponent<RectTransform>();
            contentArea.anchorMin = Vector2.zero;
            contentArea.anchorMax = Vector2.one;
            contentArea.offsetMin = Vector2.zero;
            contentArea.offsetMax = new Vector2(0f, -FullscreenUiWindow.HeaderHeight);

            return shell;
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
            image.color = new Color(0.12f, 0.14f, 0.18f, 0.96f);

            Button button = tileObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.2f, 0.24f, 0.32f, 1f);
            colors.pressedColor = new Color(0.08f, 0.1f, 0.14f, 1f);
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
            text.color = Color.white;
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
            label.color = new Color(0.92f, 0.92f, 0.92f, 1f);
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }
    }
}

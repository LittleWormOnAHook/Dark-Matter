using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public static class UiLayoutProfileApplier
    {
        public static bool Apply(Transform panelRoot, UiLayoutProfile profile, bool includeInactive = true, bool panelEmbedded = false)
        {
            if (panelRoot == null || profile == null || profile.nodes == null || profile.nodes.Count == 0)
                return false;

            bool appliedAny = false;
            for (int i = 0; i < profile.nodes.Count; i++)
            {
                UiLayoutNodeEntry entry = profile.nodes[i];
                if (entry == null || !UiLayoutCaptureRules.ShouldApplyNode(entry, panelEmbedded))
                    continue;

                RectTransform rect = FindRelativeRect(panelRoot, entry.relativePath);
                if (rect == null)
                    continue;

                bool applyActive = UiLayoutCaptureRules.ShouldApplyRootActiveState(panelRoot, entry, panelEmbedded);
                ApplyEntry(rect, entry, applyActive);
                appliedAny = true;
            }

            if (appliedAny)
                Canvas.ForceUpdateCanvases();

            return appliedAny;
        }

        public static void Capture(Transform panelRoot, UiLayoutProfile profile, bool includeHierarchy)
        {
            if (panelRoot == null || profile == null)
                return;

            profile.panelId ??= string.Empty;
            profile.nodes ??= new System.Collections.Generic.List<UiLayoutNodeEntry>();
            profile.nodes.Clear();

            if (includeHierarchy)
                CaptureRecursive(panelRoot, panelRoot, profile.nodes);
            else
                profile.nodes.Add(CaptureNode(panelRoot, string.Empty));
        }

        public static UiLayoutNodeEntry CaptureNode(Transform current, string relativePath)
        {
            RectTransform rect = current as RectTransform;
            UiLayoutNodeEntry entry = new UiLayoutNodeEntry
            {
                relativePath = relativePath ?? string.Empty,
                activeSelf = current.gameObject.activeSelf,
                localScale = current.localScale
            };

            if (rect != null)
            {
                entry.anchorMin = rect.anchorMin;
                entry.anchorMax = rect.anchorMax;
                entry.pivot = rect.pivot;
                entry.anchoredPosition = rect.anchoredPosition;
                entry.sizeDelta = rect.sizeDelta;
                entry.offsetMin = rect.offsetMin;
                entry.offsetMax = rect.offsetMax;
            }

            if (current.TryGetComponent(out Image image))
            {
                entry.hasImageStyle = true;
                entry.imageSprite = image.sprite;
                entry.imageMaterial = image.material;
                entry.imageColor = image.color;
                entry.imagePreserveAspect = image.preserveAspect;
                entry.imageType = image.type;
            }

            if (current.TryGetComponent(out RawImage rawImage))
            {
                entry.hasRawImageStyle = true;
                entry.rawTexture = rawImage.texture;
                entry.rawImageColor = rawImage.color;
            }

            if (current.TryGetComponent(out LayoutElement layoutElement))
            {
                entry.hasLayoutElement = true;
                entry.layoutMinWidth = layoutElement.minWidth;
                entry.layoutMinHeight = layoutElement.minHeight;
                entry.layoutPreferredWidth = layoutElement.preferredWidth;
                entry.layoutPreferredHeight = layoutElement.preferredHeight;
                entry.layoutFlexibleWidth = layoutElement.flexibleWidth;
                entry.layoutFlexibleHeight = layoutElement.flexibleHeight;
            }

            if (current.TryGetComponent(out GridLayoutGroup gridLayout))
            {
                entry.hasGridLayout = true;
                entry.gridCellSize = gridLayout.cellSize;
                entry.gridSpacing = gridLayout.spacing;
                entry.gridPaddingLeft = gridLayout.padding.left;
                entry.gridPaddingRight = gridLayout.padding.right;
                entry.gridPaddingTop = gridLayout.padding.top;
                entry.gridPaddingBottom = gridLayout.padding.bottom;
                entry.gridConstraint = gridLayout.constraint;
                entry.gridConstraintCount = gridLayout.constraintCount;
                entry.gridStartCorner = gridLayout.startCorner;
                entry.gridStartAxis = gridLayout.startAxis;
                entry.gridChildAlignment = gridLayout.childAlignment;
            }

            CaptureButtonStyle(current, entry);
            CaptureTextStyle(current, entry);
            CaptureLegacyTextStyle(current, entry);

            return entry;
        }

        private static void CaptureButtonStyle(Transform current, UiLayoutNodeEntry entry)
        {
            if (!current.TryGetComponent(out Button button))
                return;

            entry.hasButtonStyle = true;
            entry.buttonInteractable = button.interactable;
            entry.buttonTransition = button.transition;

            if (button.targetGraphic != null)
                entry.buttonTargetGraphicPath = GetRelativePath(current, button.targetGraphic.transform);

            ColorBlock colors = button.colors;
            entry.buttonNormalColor = colors.normalColor;
            entry.buttonHighlightedColor = colors.highlightedColor;
            entry.buttonPressedColor = colors.pressedColor;
            entry.buttonSelectedColor = colors.selectedColor;
            entry.buttonDisabledColor = colors.disabledColor;
            entry.buttonColorMultiplier = colors.colorMultiplier;
            entry.buttonFadeDuration = colors.fadeDuration;

            SpriteState spriteState = button.spriteState;
            entry.buttonHighlightedSprite = spriteState.highlightedSprite;
            entry.buttonPressedSprite = spriteState.pressedSprite;
            entry.buttonSelectedSprite = spriteState.selectedSprite;
            entry.buttonDisabledSprite = spriteState.disabledSprite;

            AnimationTriggers triggers = button.animationTriggers;
            entry.buttonNormalTrigger = triggers.normalTrigger;
            entry.buttonHighlightedTrigger = triggers.highlightedTrigger;
            entry.buttonPressedTrigger = triggers.pressedTrigger;
            entry.buttonSelectedTrigger = triggers.selectedTrigger;
            entry.buttonDisabledTrigger = triggers.disabledTrigger;
        }

        private static void CaptureTextStyle(Transform current, UiLayoutNodeEntry entry)
        {
            if (!current.TryGetComponent(out TextMeshProUGUI text))
                return;

            entry.hasTextStyle = true;
            entry.textContent = text.text;
            entry.textFont = text.font;
            entry.textFontMaterial = text.fontSharedMaterial;
            entry.textFontSize = text.fontSize;
            entry.textFontStyle = text.fontStyle;
            entry.textColor = text.color;
            entry.textAlignment = text.alignment;
            entry.textAutoSize = text.enableAutoSizing;
            entry.textFontSizeMin = text.fontSizeMin;
            entry.textFontSizeMax = text.fontSizeMax;
            entry.textWordWrap = text.textWrappingMode != TextWrappingModes.NoWrap;
            entry.textRichText = text.richText;
            entry.textRaycastTarget = text.raycastTarget;
            entry.textMargin = text.margin;
            entry.textCharacterSpacing = text.characterSpacing;
            entry.textLineSpacing = text.lineSpacing;
            entry.textParagraphSpacing = text.paragraphSpacing;
            entry.textOverflowMode = text.overflowMode;
        }

        private static void CaptureLegacyTextStyle(Transform current, UiLayoutNodeEntry entry)
        {
            if (!current.TryGetComponent(out Text legacyText))
                return;

            entry.hasLegacyTextStyle = true;
            entry.legacyTextContent = legacyText.text;
            entry.legacyFont = legacyText.font;
            entry.legacyFontSize = legacyText.fontSize;
            entry.legacyTextColor = legacyText.color;
            entry.legacyAlignment = legacyText.alignment;
            entry.legacyFontStyle = legacyText.fontStyle;
            entry.legacyLineSpacing = legacyText.lineSpacing;
            entry.legacyRichText = legacyText.supportRichText;
            entry.legacyRaycastTarget = legacyText.raycastTarget;
            entry.legacyBestFit = legacyText.resizeTextForBestFit;
            entry.legacyMinSize = legacyText.resizeTextMinSize;
            entry.legacyMaxSize = legacyText.resizeTextMaxSize;
            entry.legacyHorizontalOverflow = legacyText.horizontalOverflow;
            entry.legacyVerticalOverflow = legacyText.verticalOverflow;
        }

        private static void ApplyGridLayout(RectTransform rect, UiLayoutNodeEntry entry)
        {
            GridLayoutGroup grid = rect.GetComponent<GridLayoutGroup>();
            if (grid == null)
                grid = rect.gameObject.AddComponent<GridLayoutGroup>();

            grid.cellSize = entry.gridCellSize;
            grid.spacing = entry.gridSpacing;
            grid.padding = new RectOffset(
                entry.gridPaddingLeft,
                entry.gridPaddingRight,
                entry.gridPaddingTop,
                entry.gridPaddingBottom);
            grid.constraint = entry.gridConstraint;
            grid.constraintCount = entry.gridConstraintCount;
            grid.startCorner = entry.gridStartCorner;
            grid.startAxis = entry.gridStartAxis;
            grid.childAlignment = entry.gridChildAlignment;
        }

        public static void ApplyEntry(RectTransform rect, UiLayoutNodeEntry entry, bool applyActiveSelf = true)
        {
            if (rect == null || entry == null)
                return;

            if (applyActiveSelf)
                rect.gameObject.SetActive(entry.activeSelf);
            rect.anchorMin = entry.anchorMin;
            rect.anchorMax = entry.anchorMax;
            rect.pivot = entry.pivot;
            rect.anchoredPosition = entry.anchoredPosition;
            rect.sizeDelta = entry.sizeDelta;
            rect.offsetMin = entry.offsetMin;
            rect.offsetMax = entry.offsetMax;
            rect.localScale = entry.localScale;

            if (entry.hasImageStyle && rect.TryGetComponent(out Image image))
            {
                image.sprite = entry.imageSprite;
                if (entry.imageMaterial != null)
                    image.material = entry.imageMaterial;
                image.color = entry.imageColor;
                image.preserveAspect = entry.imagePreserveAspect;
                image.type = entry.imageType;
            }

            if (entry.hasRawImageStyle && rect.TryGetComponent(out RawImage rawImage))
            {
                rawImage.texture = entry.rawTexture;
                rawImage.color = entry.rawImageColor;
            }

            if (entry.hasLayoutElement)
            {
                LayoutElement layoutElement = rect.GetComponent<LayoutElement>();
                if (layoutElement == null)
                    layoutElement = rect.gameObject.AddComponent<LayoutElement>();

                layoutElement.minWidth = entry.layoutMinWidth;
                layoutElement.minHeight = entry.layoutMinHeight;
                layoutElement.preferredWidth = entry.layoutPreferredWidth;
                layoutElement.preferredHeight = entry.layoutPreferredHeight;
                layoutElement.flexibleWidth = entry.layoutFlexibleWidth;
                layoutElement.flexibleHeight = entry.layoutFlexibleHeight;
            }

            if (entry.hasGridLayout)
                ApplyGridLayout(rect, entry);

            ApplyButtonStyle(rect, entry);
            ApplyTextStyle(rect, entry);
            ApplyLegacyTextStyle(rect, entry);
        }

        private static void ApplyButtonStyle(RectTransform rect, UiLayoutNodeEntry entry)
        {
            if (!entry.hasButtonStyle || !rect.TryGetComponent(out Button button))
                return;

            button.interactable = entry.buttonInteractable;
            button.transition = entry.buttonTransition;
            button.targetGraphic = ResolveGraphicOnNode(rect, entry.buttonTargetGraphicPath);

            ColorBlock colors = button.colors;
            colors.normalColor = entry.buttonNormalColor;
            colors.highlightedColor = entry.buttonHighlightedColor;
            colors.pressedColor = entry.buttonPressedColor;
            colors.selectedColor = entry.buttonSelectedColor;
            colors.disabledColor = entry.buttonDisabledColor;
            colors.colorMultiplier = entry.buttonColorMultiplier;
            colors.fadeDuration = entry.buttonFadeDuration;
            button.colors = colors;

            SpriteState spriteState = button.spriteState;
            spriteState.highlightedSprite = entry.buttonHighlightedSprite;
            spriteState.pressedSprite = entry.buttonPressedSprite;
            spriteState.selectedSprite = entry.buttonSelectedSprite;
            spriteState.disabledSprite = entry.buttonDisabledSprite;
            button.spriteState = spriteState;

            AnimationTriggers triggers = button.animationTriggers;
            triggers.normalTrigger = entry.buttonNormalTrigger;
            triggers.highlightedTrigger = entry.buttonHighlightedTrigger;
            triggers.pressedTrigger = entry.buttonPressedTrigger;
            triggers.selectedTrigger = entry.buttonSelectedTrigger;
            triggers.disabledTrigger = entry.buttonDisabledTrigger;
            button.animationTriggers = triggers;

            Graphic targetGraphic = button.targetGraphic;
            if (targetGraphic is Image targetImage)
            {
                if (entry.hasImageStyle)
                {
                    if (entry.imageSprite != null)
                        targetImage.sprite = entry.imageSprite;
                    if (entry.imageMaterial != null)
                        targetImage.material = entry.imageMaterial;
                    targetImage.color = entry.imageColor;
                    targetImage.preserveAspect = entry.imagePreserveAspect;
                    targetImage.type = entry.imageType;
                }
                else if (button.transition == Selectable.Transition.ColorTint)
                {
                    targetImage.color = Color.white;
                }
            }
        }

        private static void ApplyTextStyle(RectTransform rect, UiLayoutNodeEntry entry)
        {
            if (!entry.hasTextStyle || !rect.TryGetComponent(out TextMeshProUGUI text))
                return;

            text.text = entry.textContent ?? string.Empty;
            if (entry.textFont != null)
                text.font = entry.textFont;
            if (entry.textFontMaterial != null)
                text.fontSharedMaterial = entry.textFontMaterial;
            text.fontSize = entry.textFontSize;
            text.fontStyle = entry.textFontStyle;
            text.color = entry.textColor;
            text.alignment = entry.textAlignment;
            text.enableAutoSizing = entry.textAutoSize;
            text.fontSizeMin = entry.textFontSizeMin;
            text.fontSizeMax = entry.textFontSizeMax;
            text.textWrappingMode = entry.textWordWrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.richText = entry.textRichText;
            text.raycastTarget = entry.textRaycastTarget;
            text.margin = entry.textMargin;
            text.characterSpacing = entry.textCharacterSpacing;
            text.lineSpacing = entry.textLineSpacing;
            text.paragraphSpacing = entry.textParagraphSpacing;
            text.overflowMode = entry.textOverflowMode;
        }

        private static void ApplyLegacyTextStyle(RectTransform rect, UiLayoutNodeEntry entry)
        {
            if (!entry.hasLegacyTextStyle || !rect.TryGetComponent(out Text legacyText))
                return;

            legacyText.text = entry.legacyTextContent ?? string.Empty;
            if (entry.legacyFont != null)
                legacyText.font = entry.legacyFont;
            legacyText.fontSize = entry.legacyFontSize;
            legacyText.color = entry.legacyTextColor;
            legacyText.alignment = entry.legacyAlignment;
            legacyText.fontStyle = entry.legacyFontStyle;
            legacyText.lineSpacing = entry.legacyLineSpacing;
            legacyText.supportRichText = entry.legacyRichText;
            legacyText.raycastTarget = entry.legacyRaycastTarget;
            legacyText.resizeTextForBestFit = entry.legacyBestFit;
            legacyText.resizeTextMinSize = entry.legacyMinSize;
            legacyText.resizeTextMaxSize = entry.legacyMaxSize;
            legacyText.horizontalOverflow = entry.legacyHorizontalOverflow;
            legacyText.verticalOverflow = entry.legacyVerticalOverflow;
        }

        private static Graphic ResolveGraphicOnNode(RectTransform rect, string relativePath)
        {
            if (rect == null)
                return null;

            if (string.IsNullOrEmpty(relativePath))
                return rect.GetComponent<Graphic>();

            Transform found = rect.Find(relativePath);
            return found != null ? found.GetComponent<Graphic>() : rect.GetComponent<Graphic>();
        }

        private static void CaptureRecursive(Transform root, Transform current, System.Collections.Generic.List<UiLayoutNodeEntry> nodes)
        {
            if (UiLayoutCaptureRules.ShouldCaptureTransform(current, root))
                nodes.Add(CaptureNode(current, GetRelativePath(root, current)));

            for (int i = 0; i < current.childCount; i++)
            {
                Transform child = current.GetChild(i);
                if (!UiLayoutCaptureRules.ShouldRecurseInto(child, root, current))
                    continue;

                CaptureRecursive(root, child, nodes);
            }
        }

        public static RectTransform FindRelativeRect(Transform root, string relativePath)
        {
            if (root == null)
                return null;

            if (string.IsNullOrEmpty(relativePath))
                return root as RectTransform;

            Transform found = root.Find(relativePath);
            return found as RectTransform;
        }

        public static string GetRelativePath(Transform root, Transform target)
        {
            if (root == null || target == null)
                return string.Empty;

            if (target == root)
                return string.Empty;

            System.Text.StringBuilder builder = new System.Text.StringBuilder(target.name);
            Transform current = target.parent;
            while (current != null && current != root)
            {
                builder.Insert(0, current.name + "/");
                current = current.parent;
            }

            return builder.ToString();
        }
    }
}

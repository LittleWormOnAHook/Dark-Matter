using System;
using Project.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project.EditorTools.UiLayout
{
    internal enum UiHierarchyFilter
    {
        All,
        Images,
        Text,
        Buttons,
        Layout,
        Interactive
    }

    internal static class UiLayoutEditorInspectorDrawers
    {
        public static void DrawRectTransformSection(RectTransform rect)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Stretch", EditorStyles.miniButtonLeft))
                ApplyAnchorPreset(rect, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f));
            if (GUILayout.Button("Top", EditorStyles.miniButtonMid))
                ApplyAnchorPreset(rect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            if (GUILayout.Button("Bottom", EditorStyles.miniButtonMid))
                ApplyAnchorPreset(rect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f));
            if (GUILayout.Button("Left", EditorStyles.miniButtonMid))
                ApplyAnchorPreset(rect, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f));
            if (GUILayout.Button("Right", EditorStyles.miniButtonMid))
                ApplyAnchorPreset(rect, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f));
            if (GUILayout.Button("Center", EditorStyles.miniButtonRight))
                ApplyAnchorPreset(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            Vector2 anchorMin = EditorGUILayout.Vector2Field("Anchor Min", rect.anchorMin);
            Vector2 anchorMax = EditorGUILayout.Vector2Field("Anchor Max", rect.anchorMax);
            Vector2 pivot = EditorGUILayout.Vector2Field("Pivot", rect.pivot);
            Vector2 anchoredPosition = EditorGUILayout.Vector2Field("Anchored Position", rect.anchoredPosition);
            Vector2 sizeDelta = EditorGUILayout.Vector2Field("Size Delta", rect.sizeDelta);
            Vector3 localScale = EditorGUILayout.Vector3Field("Local Scale", rect.localScale);
            Vector3 localRotation = EditorGUILayout.Vector3Field("Local Rotation", rect.localEulerAngles);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(rect, "Edit UI Rect");
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.pivot = pivot;
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = sizeDelta;
                rect.localScale = localScale;
                rect.localEulerAngles = localRotation;
                CommitChange(rect, "Edit UI Rect");
            }
        }

        private static void ApplyAnchorPreset(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot)
        {
            Undo.RecordObject(rect, "Apply Anchor Preset");
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            CommitChange(rect, "Apply Anchor Preset");
        }

        public static void DrawImageSection(Image image)
        {
            EditorGUI.BeginChangeCheck();
            Sprite sprite = (Sprite)EditorGUILayout.ObjectField("Sprite", image.sprite, typeof(Sprite), false);
            Material material = (Material)EditorGUILayout.ObjectField("Material", image.material, typeof(Material), false);
            Color color = EditorGUILayout.ColorField("Color", image.color);
            Image.Type imageType = (Image.Type)EditorGUILayout.EnumPopup("Image Type", image.type);
            bool preserveAspect = EditorGUILayout.Toggle("Preserve Aspect", image.preserveAspect);
            bool raycast = EditorGUILayout.Toggle("Raycast Target", image.raycastTarget);
            bool maskable = EditorGUILayout.Toggle("Maskable", image.maskable);

            float fillAmount = image.fillAmount;
            if (imageType == Image.Type.Filled)
            {
                fillAmount = EditorGUILayout.Slider("Fill Amount", image.fillAmount, 0f, 1f);
                Image.FillMethod fillMethod = (Image.FillMethod)EditorGUILayout.EnumPopup("Fill Method", image.fillMethod);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(image, "Edit UI Image");
                    image.sprite = sprite;
                    image.material = material;
                    image.color = color;
                    image.type = imageType;
                    image.preserveAspect = preserveAspect;
                    image.raycastTarget = raycast;
                    image.maskable = maskable;
                    image.fillAmount = fillAmount;
                    image.fillMethod = fillMethod;
                    CommitChange(image, "Edit UI Image");
                }

                return;
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(image, "Edit UI Image");
                image.sprite = sprite;
                image.material = material;
                image.color = color;
                image.type = imageType;
                image.preserveAspect = preserveAspect;
                image.raycastTarget = raycast;
                image.maskable = maskable;
                CommitChange(image, "Edit UI Image");
            }
        }

        public static void DrawRawImageSection(RawImage rawImage)
        {
            EditorGUI.BeginChangeCheck();
            Texture texture = (Texture)EditorGUILayout.ObjectField("Texture", rawImage.texture, typeof(Texture), false);
            Material material = (Material)EditorGUILayout.ObjectField("Material", rawImage.material, typeof(Material), false);
            Color color = EditorGUILayout.ColorField("Color", rawImage.color);
            Rect uvRect = EditorGUILayout.RectField("UV Rect", rawImage.uvRect);
            bool raycast = EditorGUILayout.Toggle("Raycast Target", rawImage.raycastTarget);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(rawImage, "Edit UI RawImage");
                rawImage.texture = texture;
                rawImage.material = material;
                rawImage.color = color;
                rawImage.uvRect = uvRect;
                rawImage.raycastTarget = raycast;
                CommitChange(rawImage, "Edit UI RawImage");
            }
        }

        public static void DrawTextSection(TextMeshProUGUI text)
        {
            EditorGUI.BeginChangeCheck();
            TMP_FontAsset font = (TMP_FontAsset)EditorGUILayout.ObjectField("Font Asset", text.font, typeof(TMP_FontAsset), false);
            Material fontMaterial = (Material)EditorGUILayout.ObjectField("Font Material", text.fontSharedMaterial, typeof(Material), false);
            string content = EditorGUILayout.TextField("Text", text.text);
            float fontSize = EditorGUILayout.FloatField("Font Size", text.fontSize);
            Color color = EditorGUILayout.ColorField("Color", text.color);
            TextAlignmentOptions alignment = (TextAlignmentOptions)EditorGUILayout.EnumPopup("Alignment", text.alignment);
            bool autoSize = EditorGUILayout.Toggle("Auto Size", text.enableAutoSizing);
            float minSize = text.fontSizeMin;
            float maxSize = text.fontSizeMax;
            if (autoSize)
            {
                minSize = EditorGUILayout.FloatField("Min Size", text.fontSizeMin);
                maxSize = EditorGUILayout.FloatField("Max Size", text.fontSizeMax);
            }

            bool wordWrap = EditorGUILayout.Toggle("Word Wrap", text.textWrappingMode != TextWrappingModes.NoWrap);
            bool richText = EditorGUILayout.Toggle("Rich Text", text.richText);
            bool raycast = EditorGUILayout.Toggle("Raycast Target", text.raycastTarget);
            Vector4 margin = EditorGUILayout.Vector4Field("Margin", text.margin);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(text, "Edit UI Text");
                text.font = font;
                if (fontMaterial != null)
                    text.fontSharedMaterial = fontMaterial;
                text.text = content;
                text.fontSize = fontSize;
                text.color = color;
                text.alignment = alignment;
                text.enableAutoSizing = autoSize;
                text.fontSizeMin = minSize;
                text.fontSizeMax = maxSize;
                text.textWrappingMode = wordWrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                text.richText = richText;
                text.raycastTarget = raycast;
                text.margin = margin;
                CommitChange(text, "Edit UI Text");
            }
        }

        public static void DrawLayoutSection(GameObject target)
        {
            if (target.TryGetComponent(out HorizontalLayoutGroup horizontal))
                DrawHorizontalLayout(horizontal);
            else if (target.TryGetComponent(out VerticalLayoutGroup vertical))
                DrawVerticalLayout(vertical);
            else if (target.TryGetComponent(out GridLayoutGroup grid))
                DrawGridLayout(grid);
            else if (target.TryGetComponent(out LayoutElement layoutElement))
                DrawLayoutElementSection(layoutElement);
            else
                EditorGUILayout.LabelField("No layout group on this object.");
        }

        public static void DrawLayoutElementSection(LayoutElement element)
        {
            EditorGUI.BeginChangeCheck();
            bool ignoreLayout = EditorGUILayout.Toggle("Ignore Layout", element.ignoreLayout);
            float minWidth = EditorGUILayout.FloatField("Min Width", element.minWidth);
            float minHeight = EditorGUILayout.FloatField("Min Height", element.minHeight);
            float preferredWidth = EditorGUILayout.FloatField("Preferred Width", element.preferredWidth);
            float preferredHeight = EditorGUILayout.FloatField("Preferred Height", element.preferredHeight);
            float flexibleWidth = EditorGUILayout.FloatField("Flexible Width", element.flexibleWidth);
            float flexibleHeight = EditorGUILayout.FloatField("Flexible Height", element.flexibleHeight);
            int layoutPriority = EditorGUILayout.IntField("Layout Priority", element.layoutPriority);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(element, "Edit Layout Element");
                element.ignoreLayout = ignoreLayout;
                element.minWidth = minWidth;
                element.minHeight = minHeight;
                element.preferredWidth = preferredWidth;
                element.preferredHeight = preferredHeight;
                element.flexibleWidth = flexibleWidth;
                element.flexibleHeight = flexibleHeight;
                element.layoutPriority = layoutPriority;
                CommitChange(element, "Edit Layout Element");
            }
        }

        private static void DrawHorizontalLayout(HorizontalLayoutGroup layout)
        {
            EditorGUI.BeginChangeCheck();
            RectOffset padding = DrawPadding(layout.padding);
            float spacing = EditorGUILayout.FloatField("Spacing", layout.spacing);
            TextAnchor alignment = (TextAnchor)EditorGUILayout.EnumPopup("Child Alignment", layout.childAlignment);
            bool controlWidth = EditorGUILayout.Toggle("Control Child Width", layout.childControlWidth);
            bool controlHeight = EditorGUILayout.Toggle("Control Child Height", layout.childControlHeight);
            bool forceExpandWidth = EditorGUILayout.Toggle("Force Expand Width", layout.childForceExpandWidth);
            bool forceExpandHeight = EditorGUILayout.Toggle("Force Expand Height", layout.childForceExpandHeight);
            bool reverseArrangement = EditorGUILayout.Toggle("Reverse Arrangement", layout.reverseArrangement);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(layout, "Edit Horizontal Layout");
                layout.padding = padding;
                layout.spacing = spacing;
                layout.childAlignment = alignment;
                layout.childControlWidth = controlWidth;
                layout.childControlHeight = controlHeight;
                layout.childForceExpandWidth = forceExpandWidth;
                layout.childForceExpandHeight = forceExpandHeight;
                layout.reverseArrangement = reverseArrangement;
                CommitChange(layout, "Edit Horizontal Layout");
            }
        }

        private static void DrawVerticalLayout(VerticalLayoutGroup layout)
        {
            EditorGUI.BeginChangeCheck();
            RectOffset padding = DrawPadding(layout.padding);
            float spacing = EditorGUILayout.FloatField("Spacing", layout.spacing);
            TextAnchor alignment = (TextAnchor)EditorGUILayout.EnumPopup("Child Alignment", layout.childAlignment);
            bool controlWidth = EditorGUILayout.Toggle("Control Child Width", layout.childControlWidth);
            bool controlHeight = EditorGUILayout.Toggle("Control Child Height", layout.childControlHeight);
            bool forceExpandWidth = EditorGUILayout.Toggle("Force Expand Width", layout.childForceExpandWidth);
            bool forceExpandHeight = EditorGUILayout.Toggle("Force Expand Height", layout.childForceExpandHeight);
            bool reverseArrangement = EditorGUILayout.Toggle("Reverse Arrangement", layout.reverseArrangement);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(layout, "Edit Vertical Layout");
                layout.padding = padding;
                layout.spacing = spacing;
                layout.childAlignment = alignment;
                layout.childControlWidth = controlWidth;
                layout.childControlHeight = controlHeight;
                layout.childForceExpandWidth = forceExpandWidth;
                layout.childForceExpandHeight = forceExpandHeight;
                layout.reverseArrangement = reverseArrangement;
                CommitChange(layout, "Edit Vertical Layout");
            }
        }

        private static void DrawGridLayout(GridLayoutGroup grid)
        {
            EditorGUI.BeginChangeCheck();
            RectOffset padding = DrawPadding(grid.padding);
            Vector2 cellSize = EditorGUILayout.Vector2Field("Cell Size", grid.cellSize);
            Vector2 spacing = EditorGUILayout.Vector2Field("Spacing", grid.spacing);
            GridLayoutGroup.Corner startCorner = (GridLayoutGroup.Corner)EditorGUILayout.EnumPopup("Start Corner", grid.startCorner);
            GridLayoutGroup.Axis startAxis = (GridLayoutGroup.Axis)EditorGUILayout.EnumPopup("Start Axis", grid.startAxis);
            TextAnchor childAlignment = (TextAnchor)EditorGUILayout.EnumPopup("Child Alignment", grid.childAlignment);
            GridLayoutGroup.Constraint constraint = (GridLayoutGroup.Constraint)EditorGUILayout.EnumPopup("Constraint", grid.constraint);
            int constraintCount = grid.constraintCount;
            if (constraint != GridLayoutGroup.Constraint.Flexible)
                constraintCount = EditorGUILayout.IntField("Constraint Count", grid.constraintCount);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(grid, "Edit Grid Layout");
                grid.padding = padding;
                grid.cellSize = cellSize;
                grid.spacing = spacing;
                grid.startCorner = startCorner;
                grid.startAxis = startAxis;
                grid.childAlignment = childAlignment;
                grid.constraint = constraint;
                grid.constraintCount = constraintCount;
                CommitChange(grid, "Edit Grid Layout");
            }
        }

        private static RectOffset DrawPadding(RectOffset padding)
        {
            int left = EditorGUILayout.IntField("Padding Left", padding.left);
            int right = EditorGUILayout.IntField("Padding Right", padding.right);
            int top = EditorGUILayout.IntField("Padding Top", padding.top);
            int bottom = EditorGUILayout.IntField("Padding Bottom", padding.bottom);
            return new RectOffset(left, right, top, bottom);
        }

        public static void DrawCanvasGroupSection(CanvasGroup group)
        {
            EditorGUI.BeginChangeCheck();
            float alpha = EditorGUILayout.Slider("Alpha", group.alpha, 0f, 1f);
            bool interactable = EditorGUILayout.Toggle("Interactable", group.interactable);
            bool blocksRaycasts = EditorGUILayout.Toggle("Blocks Raycasts", group.blocksRaycasts);
            bool ignoreParentGroups = EditorGUILayout.Toggle("Ignore Parent Groups", group.ignoreParentGroups);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(group, "Edit Canvas Group");
                group.alpha = alpha;
                group.interactable = interactable;
                group.blocksRaycasts = blocksRaycasts;
                group.ignoreParentGroups = ignoreParentGroups;
                CommitChange(group, "Edit Canvas Group");
            }
        }

        public static void DrawButtonSection(Button button)
        {
            EditorGUI.BeginChangeCheck();
            bool interactable = EditorGUILayout.Toggle("Interactable", button.interactable);
            Selectable.Transition transition = (Selectable.Transition)EditorGUILayout.EnumPopup("Transition", button.transition);
            Graphic targetGraphic = (Graphic)EditorGUILayout.ObjectField("Target Graphic", button.targetGraphic, typeof(Graphic), true);
            ColorBlock colors = button.colors;
            colors.normalColor = EditorGUILayout.ColorField("Normal Color", colors.normalColor);
            colors.highlightedColor = EditorGUILayout.ColorField("Highlighted Color", colors.highlightedColor);
            colors.pressedColor = EditorGUILayout.ColorField("Pressed Color", colors.pressedColor);
            colors.selectedColor = EditorGUILayout.ColorField("Selected Color", colors.selectedColor);
            colors.disabledColor = EditorGUILayout.ColorField("Disabled Color", colors.disabledColor);
            colors.colorMultiplier = EditorGUILayout.FloatField("Color Multiplier", colors.colorMultiplier);
            colors.fadeDuration = EditorGUILayout.FloatField("Fade Duration", colors.fadeDuration);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(button, "Edit UI Button");
                button.interactable = interactable;
                button.transition = transition;
                button.targetGraphic = targetGraphic;
                button.colors = colors;
                CommitChange(button, "Edit UI Button");
            }
        }

        public static void DrawSliderSection(Slider slider)
        {
            EditorGUI.BeginChangeCheck();
            float value = EditorGUILayout.Slider("Value", slider.value, slider.minValue, slider.maxValue);
            float minValue = EditorGUILayout.FloatField("Min Value", slider.minValue);
            float maxValue = EditorGUILayout.FloatField("Max Value", slider.maxValue);
            bool wholeNumbers = EditorGUILayout.Toggle("Whole Numbers", slider.wholeNumbers);
            bool interactable = EditorGUILayout.Toggle("Interactable", slider.interactable);
            Graphic targetGraphic = (Graphic)EditorGUILayout.ObjectField("Target Graphic", slider.targetGraphic, typeof(Graphic), true);
            RectTransform fillRect = (RectTransform)EditorGUILayout.ObjectField("Fill Rect", slider.fillRect, typeof(RectTransform), true);
            RectTransform handleRect = (RectTransform)EditorGUILayout.ObjectField("Handle Rect", slider.handleRect, typeof(RectTransform), true);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(slider, "Edit UI Slider");
                slider.minValue = minValue;
                slider.maxValue = maxValue;
                slider.wholeNumbers = wholeNumbers;
                slider.interactable = interactable;
                slider.targetGraphic = targetGraphic;
                slider.fillRect = fillRect;
                slider.handleRect = handleRect;
                slider.value = value;
                CommitChange(slider, "Edit UI Slider");
            }
        }

        public static void DrawScrollRectSection(ScrollRect scrollRect)
        {
            EditorGUI.BeginChangeCheck();
            bool horizontal = EditorGUILayout.Toggle("Horizontal", scrollRect.horizontal);
            bool vertical = EditorGUILayout.Toggle("Vertical", scrollRect.vertical);
            ScrollRect.MovementType movementType = (ScrollRect.MovementType)EditorGUILayout.EnumPopup("Movement Type", scrollRect.movementType);
            float elasticity = EditorGUILayout.FloatField("Elasticity", scrollRect.elasticity);
            bool inertia = EditorGUILayout.Toggle("Inertia", scrollRect.inertia);
            float decelerationRate = EditorGUILayout.FloatField("Deceleration Rate", scrollRect.decelerationRate);
            float scrollSensitivity = EditorGUILayout.FloatField("Scroll Sensitivity", scrollRect.scrollSensitivity);
            RectTransform content = (RectTransform)EditorGUILayout.ObjectField("Content", scrollRect.content, typeof(RectTransform), true);
            RectTransform viewport = (RectTransform)EditorGUILayout.ObjectField("Viewport", scrollRect.viewport, typeof(RectTransform), true);
            Scrollbar horizontalScrollbar = (Scrollbar)EditorGUILayout.ObjectField("Horizontal Scrollbar", scrollRect.horizontalScrollbar, typeof(Scrollbar), true);
            Scrollbar verticalScrollbar = (Scrollbar)EditorGUILayout.ObjectField("Vertical Scrollbar", scrollRect.verticalScrollbar, typeof(Scrollbar), true);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(scrollRect, "Edit Scroll Rect");
                scrollRect.horizontal = horizontal;
                scrollRect.vertical = vertical;
                scrollRect.movementType = movementType;
                scrollRect.elasticity = elasticity;
                scrollRect.inertia = inertia;
                scrollRect.decelerationRate = decelerationRate;
                scrollRect.scrollSensitivity = scrollSensitivity;
                scrollRect.content = content;
                scrollRect.viewport = viewport;
                scrollRect.horizontalScrollbar = horizontalScrollbar;
                scrollRect.verticalScrollbar = verticalScrollbar;
                CommitChange(scrollRect, "Edit Scroll Rect");
            }
        }

        public static void DrawCanvasSection(Canvas canvas)
        {
            EditorGUI.BeginChangeCheck();
            RenderMode renderMode = (RenderMode)EditorGUILayout.EnumPopup("Render Mode", canvas.renderMode);
            int sortingOrder = EditorGUILayout.IntField("Sorting Order", canvas.sortingOrder);
            string sortingLayer = EditorGUILayout.TextField("Sorting Layer Name", canvas.sortingLayerName);
            bool pixelPerfect = EditorGUILayout.Toggle("Pixel Perfect", canvas.pixelPerfect);
            bool overrideSorting = EditorGUILayout.Toggle("Override Sorting", canvas.overrideSorting);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(canvas, "Edit Canvas");
                canvas.renderMode = renderMode;
                canvas.sortingOrder = sortingOrder;
                canvas.sortingLayerName = sortingLayer;
                canvas.pixelPerfect = pixelPerfect;
                canvas.overrideSorting = overrideSorting;
                CommitChange(canvas, "Edit Canvas");
            }
        }

        public static void DrawCanvasScalerSection(CanvasScaler scaler)
        {
            EditorGUI.BeginChangeCheck();
            CanvasScaler.ScaleMode scaleMode = (CanvasScaler.ScaleMode)EditorGUILayout.EnumPopup("UI Scale Mode", scaler.uiScaleMode);
            Vector2 referenceResolution = scaler.referenceResolution;
            if (scaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                referenceResolution = EditorGUILayout.Vector2Field("Reference Resolution", scaler.referenceResolution);

            float match = scaler.matchWidthOrHeight;
            if (scaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                match = EditorGUILayout.Slider("Match Width Or Height", scaler.matchWidthOrHeight, 0f, 1f);

            float scaleFactor = scaler.scaleFactor;
            if (scaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
                scaleFactor = EditorGUILayout.FloatField("Scale Factor", scaler.scaleFactor);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(scaler, "Edit Canvas Scaler");
                scaler.uiScaleMode = scaleMode;
                scaler.referenceResolution = referenceResolution;
                scaler.matchWidthOrHeight = match;
                scaler.scaleFactor = scaleFactor;
                CommitChange(scaler, "Edit Canvas Scaler");
            }
        }

        public static void DrawMapUiSection(MapUI mapUi)
        {
            EditorGUILayout.LabelField("Map UI", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            bool preserveManualLayout = EditorGUILayout.Toggle("Preserve Manual Layout", mapUi.PreservesManualLayout);
            SerializedObject serialized = new SerializedObject(mapUi);
            SerializedProperty applyRuntimeLayout = serialized.FindProperty("applyRuntimeLayout");
            bool runtimeLayout = applyRuntimeLayout != null && applyRuntimeLayout.boolValue;
            if (applyRuntimeLayout != null)
                runtimeLayout = EditorGUILayout.Toggle("Apply Runtime Layout", runtimeLayout);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Layout Shells", GUILayout.Width(140f)))
            {
                Undo.RecordObject(mapUi, "Create Map Layout Shells");
                mapUi.EnsureLayoutShells();
                CommitChange(mapUi, "Create Map Layout Shells");
            }

            if (GUILayout.Button("Prep Manual Layout", GUILayout.Width(140f)))
            {
                UiLayoutEditorPanelRegistry.SetSerializedBoolIfExists(mapUi, "preserveManualLayout", true);
                UiLayoutEditorPanelRegistry.SetSerializedBoolIfExists(mapUi, "applyRuntimeLayout", false);
            }
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                UiLayoutEditorPanelRegistry.SetSerializedBoolIfExists(mapUi, "preserveManualLayout", preserveManualLayout);
                if (applyRuntimeLayout != null)
                {
                    applyRuntimeLayout.boolValue = runtimeLayout;
                    serialized.ApplyModifiedProperties();
                }

                CommitChange(mapUi, "Edit Map UI Layout");
            }
        }

        public static void DrawInventorySlotSection(InventorySlotUI slotUi)
        {
            EditorGUILayout.LabelField("Inventory Slot", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            Image iconImage = (Image)EditorGUILayout.ObjectField("Icon Image", slotUi.iconImage, typeof(Image), true);
            Image backgroundImage = (Image)EditorGUILayout.ObjectField("Background Image", slotUi.backgroundImage, typeof(Image), true);
            TextMeshProUGUI amountText = (TextMeshProUGUI)EditorGUILayout.ObjectField("Amount Text", slotUi.amountText, typeof(TextMeshProUGUI), true);
            bool preserveManualLayout = EditorGUILayout.Toggle("Preserve Manual Layout", slotUi.PreservesManualLayout);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(slotUi, "Edit Inventory Slot");
                slotUi.iconImage = iconImage;
                slotUi.backgroundImage = backgroundImage;
                slotUi.amountText = amountText;
                UiLayoutEditorPanelRegistry.SetSerializedBoolIfExists(slotUi, "preserveManualLayout", preserveManualLayout);
                CommitChange(slotUi, "Edit Inventory Slot");
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Find Icon Child", GUILayout.Width(120f)))
            {
                Transform icon = slotUi.transform.Find("Icon");
                if (icon != null && icon.TryGetComponent(out Image foundIcon))
                {
                    Undo.RecordObject(slotUi, "Assign Icon Image");
                    slotUi.iconImage = foundIcon;
                    CommitChange(slotUi, "Assign Icon Image");
                }
            }

            if (GUILayout.Button("Find Amount Text", GUILayout.Width(120f)))
            {
                TextMeshProUGUI found = slotUi.GetComponentInChildren<TextMeshProUGUI>(true);
                if (found != null)
                {
                    Undo.RecordObject(slotUi, "Assign Amount Text");
                    slotUi.amountText = found;
                    CommitChange(slotUi, "Assign Amount Text");
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        public static void DrawSerializedComponent(Component component, ref bool foldout)
        {
            if (component == null)
                return;

            foldout = EditorGUILayout.Foldout(foldout, component.GetType().Name, true);
            if (!foldout)
                return;

            EditorGUI.indentLevel++;
            SerializedObject serialized = new SerializedObject(component);
            serialized.Update();
            SerializedProperty property = serialized.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.name == "m_Script")
                    continue;

                EditorGUILayout.PropertyField(property, true);
            }

            if (serialized.ApplyModifiedProperties())
                CommitChange(component, "Edit " + component.GetType().Name);

            EditorGUI.indentLevel--;
        }

        public static void AddLayoutElement(RectTransform rect)
        {
            if (rect == null)
                return;

            LayoutElement element = rect.GetComponent<LayoutElement>();
            if (element == null)
                element = Undo.AddComponent<LayoutElement>(rect.gameObject);

            Selection.activeGameObject = rect.gameObject;
            EditorGUIUtility.PingObject(rect.gameObject);
        }

        public static bool PassesHierarchyFilter(RectTransform rect, UiHierarchyFilter filter)
        {
            if (filter == UiHierarchyFilter.All || rect == null)
                return true;

            GameObject go = rect.gameObject;
            return filter switch
            {
                UiHierarchyFilter.Images => go.GetComponent<Image>() != null || go.GetComponent<RawImage>() != null,
                UiHierarchyFilter.Text => go.GetComponent<TextMeshProUGUI>() != null || go.GetComponent<Text>() != null,
                UiHierarchyFilter.Buttons => go.GetComponent<Button>() != null,
                UiHierarchyFilter.Layout => go.GetComponent<LayoutGroup>() != null || go.GetComponent<LayoutElement>() != null,
                UiHierarchyFilter.Interactive => go.GetComponent<Selectable>() != null || go.GetComponent<ScrollRect>() != null,
                _ => true
            };
        }

        public static void DrawHierarchyBadges(RectTransform rect)
        {
            GameObject go = rect.gameObject;
            if (go.GetComponent<Image>() != null || go.GetComponent<RawImage>() != null)
                UiLayoutEditorStyles.DrawMiniBadge("IMG", new Color(0.75f, 0.55f, 1f));
            if (go.GetComponent<TextMeshProUGUI>() != null)
                UiLayoutEditorStyles.DrawMiniBadge("TXT", new Color(0.45f, 0.85f, 1f));
            if (go.GetComponent<Button>() != null)
                UiLayoutEditorStyles.DrawMiniBadge("BTN", new Color(1f, 0.7f, 0.35f));
            if (go.GetComponent<LayoutGroup>() != null || go.GetComponent<LayoutElement>() != null)
                UiLayoutEditorStyles.DrawMiniBadge("LYT", new Color(0.55f, 1f, 0.65f));
        }

        public static void CommitChange(UnityEngine.Object target, string undoLabel)
        {
            if (target == null)
                return;

            EditorUtility.SetDirty(target);

            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                EditorUtility.SetDirty(stage.prefabContentsRoot);
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
            else
            {
                UiLayoutEditorWindow.MarkSceneDirtyStatic();
            }

            SceneView.RepaintAll();
        }
    }
}

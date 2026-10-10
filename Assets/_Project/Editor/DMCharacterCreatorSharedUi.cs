#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class DMCharacterCreatorSharedUi
    {
        public const float MinPanelContentWidth = 300f;
        public const float NarrowButtonStackWidth = 520f;
        /// <summary>Below this content width, toggles and min/max fields stack vertically.</summary>
        public const float NarrowStackControlsWidth = 480f;
        /// <summary>Default minimum label width for property rows in creator panels.</summary>
        public const float CreatorPropertyLabelWidthMin = 180f;
        public const float CreatorPropertyLabelWidthMax = 220f;
        /// <summary>Padding for category rail + margins when deriving split-pane content width.</summary>
        public const float SplitPaneChromePadding = 80f;

        /// <summary>Matches Genesis Studio <c>ContentPanel</c> horizontal padding (Footsteps reference).</summary>
        public const float CreatorContentPaddingLeft = 10f;
        /// <summary>Right gutter from visible content edge (object fields, buttons, path text).</summary>
        public const float CreatorContentPaddingRight = 30f;
        public const float CreatorScrollBarInset = 16f;
        /// <summary>Default label column width (Footsteps uses ~200–240 via profile inspector).</summary>
        public const float CreatorProfileLabelWidthDefault = 200f;
        /// <summary>Fixed float box width on slider rows — same as <c>DMStudioStyles.ProfileFloatFieldWidth</c>.</summary>
        public const float CreatorProfileFieldWidth = 56f;

        static GUIStyle s_WrappedHelpBox;
        static int s_PanelContentDepth;
        static float s_CreatorContentRightGutter;
        static int s_CreatorContentAreaDepth;
        static float s_MeasuredColumnWidth = -1f;

        /// <summary>
        /// When true, <see cref="ContentWidth"/> prefers the last IMGUI column width (Genesis themed host)
        /// instead of full <see cref="EditorGUIUtility.currentViewWidth"/>.
        /// </summary>
        public static bool PreferMeasuredColumnWidth { get; set; } = true;

        public static void ResetMeasuredColumnWidth()
        {
            s_MeasuredColumnWidth = -1f;
        }

        public static void BeginPanelContent()
        {
            s_PanelContentDepth++;
            EditorGUILayout.BeginVertical(
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true),
                GUILayout.MinWidth(MinPanelContentWidth));
        }

        /// <summary>Minimum width for the main column beside the definition sidebar (~240px).</summary>
        public static float SplitContentMinWidth(float sidebarWidth = 240f, float chromePadding = SplitPaneChromePadding)
        {
            float view = EditorGUIUtility.currentViewWidth;
            if (view <= 1f)
                return MinPanelContentWidth;
            return Mathf.Max(MinPanelContentWidth, view - sidebarWidth - chromePadding);
        }

        /// <summary>GUILayout options for the main column beside the definition sidebar (Genesis Studio split).</summary>
        public static GUILayoutOption[] ContentColumnLayoutOptions(bool includeHeight = true)
        {
            float min = SplitContentMinWidth();
            if (includeHeight)
                return new[] { GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinWidth(min) };

            return new[] { GUILayout.ExpandWidth(true), GUILayout.MinWidth(min) };
        }

        /// <summary>Scroll views in the split column must claim horizontal space — ExpandWidth alone can collapse in IMGUI.</summary>
        public static GUILayoutOption[] ContentColumnScrollOptions()
        {
            float min = SplitContentMinWidth();
            return new[]
            {
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true),
                GUILayout.MinWidth(min)
            };
        }

        public static void EndPanelContent()
        {
            if (s_PanelContentDepth > 0)
                s_PanelContentDepth--;
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Horizontal gutter inside scroll views (PropertyField rows ignore GUIStyle padding on parent verticals).
        /// </summary>
        public static void BeginCreatorContentArea()
        {
            s_CreatorContentAreaDepth++;
            s_CreatorContentRightGutter = CreatorContentPaddingRight;
            EditorGUILayout.BeginHorizontal();
            if (CreatorContentPaddingLeft > 0f)
                GUILayout.Space(CreatorContentPaddingLeft);
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        }

        public static void EndCreatorContentArea()
        {
            EditorGUILayout.EndVertical();
            if (s_CreatorContentRightGutter > 0f)
                GUILayout.Space(s_CreatorContentRightGutter);
            EditorGUILayout.EndHorizontal();
            if (Event.current.type == EventType.Repaint)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                // Ignore sliver rects from a collapsed layout pass — they poison PreferMeasuredColumnWidth.
                if (r.width >= MinPanelContentWidth * 0.5f)
                    s_MeasuredColumnWidth = r.width;
            }

            if (s_CreatorContentAreaDepth > 0)
                s_CreatorContentAreaDepth--;
        }

        public static float ContentWidth
        {
            get
            {
                if (PreferMeasuredColumnWidth && s_MeasuredColumnWidth >= MinPanelContentWidth * 0.5f)
                {
                    float fromMeasured = s_MeasuredColumnWidth
                                         - CreatorContentPaddingLeft
                                         - CreatorContentPaddingRight;
                    float fromView = SplitContentMinWidth() - CreatorContentPaddingLeft - CreatorContentPaddingRight;
                    return Mathf.Max(MinPanelContentWidth, fromMeasured, fromView);
                }

                float view = EditorGUIUtility.currentViewWidth;
                if (view <= 1f)
                    view = MinPanelContentWidth + SplitPaneChromePadding;

                if (s_PanelContentDepth > 0)
                {
                    float reserved = DMCharacterCreatorDefinitionSidebar.Width
                                     + CreatorContentPaddingLeft
                                     + CreatorContentPaddingRight
                                     + CreatorScrollBarInset
                                     + SplitPaneChromePadding;
                    return Mathf.Max(MinPanelContentWidth, view - reserved);
                }

                return Mathf.Max(
                    MinPanelContentWidth,
                    view - CreatorContentPaddingLeft - CreatorContentPaddingRight - 32f);
            }
        }

        /// <summary>Content width minus IMGUI indent — use for label/toggle layout decisions.</summary>
        public static float EffectiveContentWidth()
        {
            float indent = EditorGUI.indentLevel * 15f;
            float width = ContentWidth - indent - CreatorScrollBarInset * 0.5f;
            return Mathf.Max(120f, width);
        }

        public static bool ShouldStackControls()
        {
            return EffectiveContentWidth() < NarrowStackControlsWidth;
        }

        public static float CreatorLabelWidth()
        {
            float inner = EffectiveContentWidth();
            float proportional = inner * 0.4f;
            return Mathf.Clamp(
                CreatorProfileLabelWidthDefault,
                CreatorPropertyLabelWidthMin,
                Mathf.Min(CreatorPropertyLabelWidthMax, proportional));
        }

        public readonly struct ScopedLabelWidth : IDisposable
        {
            readonly float _previous;

            public ScopedLabelWidth(float width)
            {
                _previous = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = width;
            }

            public void Dispose()
            {
                EditorGUIUtility.labelWidth = _previous;
            }
        }

        public sealed class ScopedCreatorProfileLayout : IDisposable
        {
            readonly float _previousLabelWidth;
            readonly float _previousFieldWidth;
            readonly bool _previousWideMode;

            public ScopedCreatorProfileLayout()
            {
                _previousLabelWidth = EditorGUIUtility.labelWidth;
                _previousFieldWidth = EditorGUIUtility.fieldWidth;
                _previousWideMode = EditorGUIUtility.wideMode;
                EditorGUIUtility.labelWidth = CreatorLabelWidth();
                EditorGUIUtility.fieldWidth = CreatorProfileFieldWidth;
                EditorGUIUtility.wideMode = false;
            }

            public void Dispose()
            {
                EditorGUIUtility.labelWidth = _previousLabelWidth;
                EditorGUIUtility.fieldWidth = _previousFieldWidth;
                EditorGUIUtility.wideMode = _previousWideMode;
            }
        }

        /// <summary>Footsteps-style label + control column (scoped label width, fixed float box, wideMode off).</summary>
        public static ScopedCreatorProfileLayout ScopedCreatorProfileLayoutScope()
        {
            return new ScopedCreatorProfileLayout();
        }

        /// <summary>Temporarily sets label width for creator property rows.</summary>
        public static ScopedCreatorProfileLayout ScopedCreatorLabelWidth()
        {
            return ScopedCreatorProfileLayoutScope();
        }

        public static string DrawStandardTextField(string label, string value)
        {
            using (ScopedCreatorProfileLayoutScope())
                return EditorGUILayout.TextField(label, value);
        }

        public static bool DrawPropertyToggle(string label, bool value)
        {
            return DrawToggleWithWrappedLabel(label, value);
        }

        public static void DrawPropertyToggle(string label, ref bool value)
        {
            value = DrawToggleWithWrappedLabel(label, value);
        }

        public static bool DrawToggleWithWrappedLabel(string label, bool value)
        {
            if (string.IsNullOrEmpty(label))
                return EditorGUILayout.Toggle(value);

            if (ShouldStackControls() || LabelExceedsToggleRow(label))
            {
                DrawWrappedLabel(label, EditorStyles.wordWrappedMiniLabel);
                Rect row = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                return EditorGUI.Toggle(row, GUIContent.none, value);
            }

            using (ScopedCreatorProfileLayoutScope())
                return EditorGUILayout.ToggleLeft(label, value);
        }

        static bool LabelExceedsToggleRow(string label)
        {
            float textWidth = EditorStyles.label.CalcSize(new GUIContent(label)).x;
            return textWidth + 24f > CreatorLabelWidth();
        }

        public static Vector3 DrawVector3Field(string label, Vector3 value)
        {
            if (!ShouldStackControls())
            {
                using (ScopedCreatorProfileLayoutScope())
                    return EditorGUILayout.Vector3Field(label, value);
            }

            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            value.x = EditorGUILayout.FloatField("X", value.x);
            value.y = EditorGUILayout.FloatField("Y", value.y);
            value.z = EditorGUILayout.FloatField("Z", value.z);
            EditorGUI.indentLevel--;
            return value;
        }

        public static void DrawIntMinMaxRow(string groupLabel, ref int min, ref int max, string minLabel = null, string maxLabel = null)
        {
            minLabel ??= groupLabel + " Min";
            maxLabel ??= groupLabel + " Max";

            if (ShouldStackControls())
            {
                using (ScopedCreatorProfileLayoutScope())
                {
                    min = EditorGUILayout.IntField(minLabel, min);
                    max = EditorGUILayout.IntField(maxLabel, max);
                }

                return;
            }

            EditorGUILayout.BeginHorizontal();
            using (ScopedCreatorProfileLayoutScope())
            {
                min = EditorGUILayout.IntField(minLabel, min, GUILayout.MaxWidth(EffectiveContentWidth() * 0.48f));
                max = EditorGUILayout.IntField(maxLabel, max, GUILayout.MaxWidth(EffectiveContentWidth() * 0.48f));
            }

            EditorGUILayout.EndHorizontal();
        }

        public static void DrawWrappedHelpBox(string message, MessageType type)
        {
            if (string.IsNullOrEmpty(message))
                return;

            GUIStyle style = WrappedHelpBoxStyle;
            float height = style.CalcHeight(new GUIContent(message), EffectiveContentWidth());
            Rect rect = EditorGUILayout.GetControlRect(false, height, GUILayout.ExpandWidth(true));
            EditorGUI.HelpBox(rect, message, type);
        }

        public static void DrawWrappedLabel(string text, GUIStyle style = null)
        {
            if (string.IsNullOrEmpty(text))
                return;

            style ??= EditorStyles.wordWrappedLabel;
            float height = style.CalcHeight(new GUIContent(text), EffectiveContentWidth());
            Rect rect = EditorGUILayout.GetControlRect(false, height, GUILayout.ExpandWidth(true));
            GUI.Label(rect, text, style);
        }

        public static void DrawAssetPathLabel(string prefix, string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            string line = string.IsNullOrEmpty(prefix) ? path : prefix + path;
            GUIStyle style = EditorStyles.wordWrappedMiniLabel;
            float height = style.CalcHeight(new GUIContent(line, path), EffectiveContentWidth());
            Rect rect = EditorGUILayout.GetControlRect(false, height, GUILayout.ExpandWidth(true));
            GUI.Label(rect, new GUIContent(line, path), style);
        }

        public static void DrawResponsiveButtonRow(float height, params (string label, Action onClick, bool enabled)[] buttons)
        {
            if (buttons == null || buttons.Length == 0)
                return;

            bool stack = EffectiveContentWidth() < NarrowButtonStackWidth;
            if (!stack)
                EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < buttons.Length; i++)
            {
                (string label, Action onClick, bool enabled) = buttons[i];
                GUI.enabled = enabled;
                if (stack)
                {
                    if (GUILayout.Button(label, GUILayout.Height(height), GUILayout.ExpandWidth(true)))
                        onClick?.Invoke();
                }
                else if (GUILayout.Button(label, GUILayout.Height(height), GUILayout.ExpandWidth(true)))
                    onClick?.Invoke();
            }

            GUI.enabled = true;
            if (!stack)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        public static void DrawToolbarButtonsWrapped(float height, params (string label, Action onClick)[] buttons)
        {
            if (buttons == null || buttons.Length == 0)
                return;

            bool stack = EffectiveContentWidth() < NarrowButtonStackWidth;
            if (!stack)
                EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < buttons.Length; i++)
            {
                if (stack)
                {
                    if (GUILayout.Button(buttons[i].label, GUILayout.Height(height), GUILayout.ExpandWidth(true)))
                        buttons[i].onClick?.Invoke();
                }
                else if (GUILayout.Button(buttons[i].label, GUILayout.Height(height), GUILayout.ExpandWidth(true)))
                    buttons[i].onClick?.Invoke();
            }

            if (!stack)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        static GUIStyle WrappedHelpBoxStyle
        {
            get
            {
                if (s_WrappedHelpBox == null)
                {
                    s_WrappedHelpBox = new GUIStyle(EditorStyles.helpBox) { wordWrap = true };
                }

                return s_WrappedHelpBox;
            }
        }

        public static void DrawModelInspectionPanel(GameObject model, string emptyLabel, bool playerRecommendations)
        {
            EnemyModelAvatarUtility.ModelInspection inspection = EnemyModelAvatarUtility.Inspect(model);
            MessageType messageType = MessageType.None;
            if (!inspection.HasModel)
                messageType = MessageType.None;
            else if (inspection.IsHumanoidAvatar && inspection.IsAvatarValid && inspection.LooksHumanoidSized)
                messageType = MessageType.Info;
            else if (inspection.HasModel)
                messageType = MessageType.Warning;

            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("Model Inspection", EditorStyles.miniBoldLabel);
            if (!inspection.HasModel)
            {
                DrawWrappedLabel(emptyLabel);
            }
            else
            {
                DrawWrappedLabel(inspection.Summary);
                if (!string.IsNullOrEmpty(inspection.AssetPath))
                    DrawAssetPathLabel(string.Empty, inspection.AssetPath);

                string recommendation = inspection.Recommendation;
                if (playerRecommendations && recommendation != null)
                {
                    recommendation = recommendation.Replace(
                        "Enemy Prefab Creator",
                        "Player Prefab Creator");
                    recommendation = recommendation.Replace(
                        "use Archetype HumanoidInvector and Create Prefab",
                        "set Prefab File Name and click Create Prefab / Rebuild");
                }

                DrawWrappedHelpBox(recommendation, messageType);
            }

            EditorGUILayout.EndVertical();
        }

        public static void DrawHumanoidPrefabStatus(
            string outputPath,
            string visualChildName,
            bool showSpawnReady,
            System.Func<GameObject, bool> spawnReadyCheck)
        {
            EditorGUILayout.LabelField("Prefab Status", EditorStyles.boldLabel);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            if (prefab == null)
            {
                DrawWrappedHelpBox($"Output prefab not created yet at {outputPath}.", MessageType.None);
                DMCharacterCreatorPrefabPreview.Draw(outputPath, visualChildName);
                return;
            }

            Animator animator = prefab.GetComponent<Animator>();
            Transform stock = prefab.transform.Find("3D Model");
            string child = string.IsNullOrWhiteSpace(visualChildName) ? "Visual" : visualChildName;
            Transform visual = prefab.transform.Find(child);

            string avatarLabel = animator != null && animator.avatar != null
                ? $"{animator.avatar.name} (human={animator.avatar.isHuman}, valid={animator.avatar.isValid})"
                : "none";

            bool hipsBound = animator != null && animator.avatar != null && animator.avatar.isValid &&
                             animator.GetBoneTransform(HumanBodyBones.Hips) != null;

            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
            DrawWrappedLabel("Avatar: " + avatarLabel);
            DrawWrappedLabel("Stock 3D Model: " + (stock != null ? "present" : "missing"));
            DrawWrappedLabel("Visual Child: " + (visual != null ? visual.name : "not applied yet"));
            DrawWrappedLabel("Hips bound: " + (hipsBound ? "yes" : "no"));
            DrawWrappedLabel(
                "Edit-mode Animator: " + (animator != null ? (animator.enabled ? "enabled" : "disabled (bind pose)") : "n/a"));

            if (showSpawnReady && spawnReadyCheck != null)
            {
                bool ready = spawnReadyCheck(prefab);
                DrawWrappedLabel("Spawn-ready: " + (ready ? "yes" : "needs full rebuild"));
            }

            EditorGUILayout.EndVertical();

            DMCharacterCreatorPrefabPreview.Draw(outputPath, visualChildName);
        }
    }
}
#endif

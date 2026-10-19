#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>Orange-accent inspector sections for <see cref="Project.Combat.Turrets.DMTurretRuntime"/>.</summary>
    public static class DMTurretInspectorSectionsGui
    {
        public static readonly Color AuthorityAccent = new Color(1f, 0.55f, 0.12f, 1f);
        const float AccentWidth = 4f;
        const float Pad = 6f;

        public static void DrawAuthoritySection(
            ref bool foldout,
            string title,
            bool overridesActive,
            Action drawContent)
        {
            EditorGUILayout.Space(4f);
            string header = overridesActive ? title + " (overrides active)" : title;
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, header);
            if (!foldout)
            {
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            Rect outer = EditorGUILayout.BeginVertical();
            GUILayout.Space(Pad);
            EditorGUI.indentLevel++;
            drawContent?.Invoke();
            EditorGUI.indentLevel--;
            GUILayout.Space(Pad);
            EditorGUILayout.EndVertical();

            if (Event.current.type != EventType.Repaint)
                return;

            Rect last = GUILayoutUtility.GetLastRect();
            if (last.height < 1f)
                last = outer;
            float y = last.yMin - Pad;
            float h = last.height + Pad * 2f;
            EditorGUI.DrawRect(new Rect(outer.xMin, y, AccentWidth, h), AuthorityAccent);
            Color fill = new Color(0.12f, 0.12f, 0.12f, 0.35f);
            EditorGUI.DrawRect(new Rect(outer.xMin + AccentWidth, y, outer.width - AccentWidth, h), fill);
        }

        public static void DrawOverridePair(
            SerializedProperty block,
            string toggleName,
            string valueName,
            string label)
        {
            DrawOverridePair(block, toggleName, valueName, label, autoEnableOnValueEdit: false);
        }

        /// <summary>
        /// When <paramref name="autoEnableOnValueEdit"/> is true, editing the value turns the override toggle on
        /// so limits/speeds stay pushed to F3DTurret via ApplyTrackingToForge.
        /// </summary>
        public static void DrawOverridePair(
            SerializedProperty block,
            string toggleName,
            string valueName,
            string label,
            bool autoEnableOnValueEdit)
        {
            SerializedProperty toggle = block.FindPropertyRelative(toggleName);
            SerializedProperty value = block.FindPropertyRelative(valueName);
            if (toggle == null || value == null)
                return;

            EditorGUILayout.BeginHorizontal();
            toggle.boolValue = EditorGUILayout.ToggleLeft(label, toggle.boolValue, GUILayout.Width(200f));
            if (autoEnableOnValueEdit)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(value, GUIContent.none);
                if (EditorGUI.EndChangeCheck() && !toggle.boolValue)
                    toggle.boolValue = true;
            }
            else
            {
                using (new EditorGUI.DisabledScope(!toggle.boolValue))
                    EditorGUILayout.PropertyField(value, GUIContent.none);
            }

            EditorGUILayout.EndHorizontal();
        }

        public static void DrawOverridePair(
            SerializedProperty block,
            string toggleName,
            string valueName,
            string label,
            float labelWidth)
        {
            SerializedProperty toggle = block.FindPropertyRelative(toggleName);
            SerializedProperty value = block.FindPropertyRelative(valueName);
            if (toggle == null || value == null)
                return;

            EditorGUILayout.BeginHorizontal();
            toggle.boolValue = EditorGUILayout.ToggleLeft(label, toggle.boolValue, GUILayout.Width(labelWidth));
            using (new EditorGUI.DisabledScope(!toggle.boolValue))
                EditorGUILayout.PropertyField(value, GUIContent.none);
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using Project.Audio.Voice;
using UnityEditor;
using UnityEngine;

namespace Project.Audio.Editor
{
    /// <summary>
    /// 2026-11-03n. Line ref picker: line set asset field, then a popup of that set's lines showing
    /// their subtitle (missing ids are flagged), plus a small Play button to audition the baked clip.
    /// </summary>
    [CustomPropertyDrawer(typeof(DMVoiceLineRef))]
    public sealed class DMVoiceLineRefDrawer : PropertyDrawer
    {
        private const float PlayButtonWidth = 42f;
        private const float Gap = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2f + Gap * 3f;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty setProp = property.FindPropertyRelative(nameof(DMVoiceLineRef.set));
            SerializedProperty idProp = property.FindPropertyRelative(nameof(DMVoiceLineRef.lineId));

            Rect row1 = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect row2 = new Rect(position.x, row1.yMax + Gap, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.PropertyField(row1, setProp, label);

            DMVoiceLineSet set = setProp.objectReferenceValue as DMVoiceLineSet;
            float idWidth = row2.width - PlayButtonWidth - Gap;
            Rect idRect = new Rect(row2.x, row2.y, idWidth, row2.height);
            Rect playRect = new Rect(row2.xMax - PlayButtonWidth, row2.y, PlayButtonWidth, row2.height);

            if (set == null)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.TextField(idRect, string.Empty, "Assign a line set");
                EditorGUI.EndDisabledGroup();
                EditorGUI.EndProperty();
                return;
            }

            List<string> ids = new List<string>(set.lines != null ? set.lines.Count + 1 : 1) { "<none>" };
            List<string> labels = new List<string>(ids.Capacity) { "None" };
            int currentIndex = 0;
            bool missingId = !string.IsNullOrEmpty(idProp.stringValue);

            if (set.lines != null)
            {
                for (int i = 0; i < set.lines.Count; i++)
                {
                    DMVoiceLine line = set.lines[i];
                    if (line == null)
                        continue;

                    string subtitle = line.SubtitleClean;
                    if (subtitle.Length > 48)
                        subtitle = subtitle.Substring(0, 45) + "...";
                    string flag = line.clip == null ? "  (no clip)" : string.Empty;
                    labels.Add(line.id + "  \u2014  " + subtitle + flag);
                    ids.Add(line.id);

                    if (string.Equals(line.id, idProp.stringValue, System.StringComparison.Ordinal))
                    {
                        currentIndex = ids.Count - 1;
                        missingId = false;
                    }
                }
            }

            if (missingId)
            {
                labels.Add(idProp.stringValue + "  (missing)");
                ids.Add(idProp.stringValue);
                currentIndex = ids.Count - 1;
            }

            int picked = EditorGUI.Popup(idRect, currentIndex, labels.ToArray());
            if (picked != currentIndex && picked >= 0 && picked < ids.Count)
                idProp.stringValue = picked == 0 ? string.Empty : ids[picked];

            using (new EditorGUI.DisabledScope(set.actor == null && set.lines == null))
            {
                if (GUI.Button(playRect, new GUIContent("Play", "Audition the baked clip.")))
                    PlayClip(set, idProp.stringValue);
            }

            EditorGUI.EndProperty();
        }

        private static void PlayClip(DMVoiceLineSet set, string lineId)
        {
            if (set == null || set.lines == null || string.IsNullOrEmpty(lineId))
                return;

            for (int i = 0; i < set.lines.Count; i++)
            {
                DMVoiceLine line = set.lines[i];
                if (line == null || !string.Equals(line.id, lineId, System.StringComparison.Ordinal))
                    continue;

                if (line.clip == null)
                {
                    Debug.LogWarning($"[DMVoiceLineRef] '{lineId}' has no baked clip yet.");
                    return;
                }

                PlayPreview(line.clip);
                return;
            }
        }

        // UnityEditor.AudioUtil moved around across versions; resolve by name like the Voices tab does.
        private static void PlayPreview(AudioClip clip)
        {
            if (clip == null)
                return;

            System.Type audioUtil = null;
            System.Reflection.Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                audioUtil = assemblies[i].GetType("UnityEditor.AudioUtil");
                if (audioUtil != null)
                    break;
            }

            if (audioUtil == null)
                return;

            audioUtil.GetMethod("StopAllPreviewClips", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.Invoke(null, null);

            System.Reflection.MethodInfo[] methods = audioUtil.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                System.Reflection.MethodInfo method = methods[i];
                if (method.Name != "PlayPreviewClip")
                    continue;

                System.Reflection.ParameterInfo[] parameters = method.GetParameters();
                object[] args = new object[parameters.Length];
                args[0] = clip;
                for (int p = 1; p < parameters.Length; p++)
                    args[p] = parameters[p].ParameterType == typeof(bool) ? (object)false : 0;
                method.Invoke(null, args);
                return;
            }
        }
    }
}
#endif

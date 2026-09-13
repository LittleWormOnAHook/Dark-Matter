#if UNITY_EDITOR
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Draws a filtered subset of fields on a shared ScriptableObject (e.g. survival vs climb on DM_ClimbDashProfile).
    /// </summary>
    internal sealed class DMStudioSectionProfilePanel
    {
        private Vector2 scroll;
        private Object cachedAsset;
        private SerializedObject serialized;

        public void Draw(string assetPath, DMStudioProfileSectionFilter section, string note)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset == null)
            {
                EditorGUILayout.HelpBox($"Missing profile at:\n{assetPath}", MessageType.Error);
                return;
            }

            DrawHeader(asset, note);

            if (!string.IsNullOrEmpty(note))
            {
                EditorGUILayout.HelpBox(note, MessageType.Info);
                EditorGUILayout.Space(4f);
            }

            if (cachedAsset != asset)
            {
                cachedAsset = asset;
                serialized = new SerializedObject(asset);
            }

            if (serialized == null || serialized.targetObject != asset)
                serialized = new SerializedObject(asset);

            serialized.Update();
            float labelWidth = DMStudioStyles.MeasureInspectorLabelWidth(
                serialized,
                path => DMStudioProfileSections.IncludesField(path, section));

            scroll = EditorGUILayout.BeginScrollView(scroll);
            using (DMStudioStyles.PushLabelWidth(labelWidth))
            {
                SerializedProperty iterator = serialized.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (!DMStudioProfileSections.IncludesField(iterator.propertyPath, section))
                        continue;

                    if (iterator.propertyPath == "m_Script")
                    {
                        using (new EditorGUI.DisabledScope(true))
                            EditorGUILayout.PropertyField(iterator, true);
                        continue;
                    }

                    EditorGUILayout.PropertyField(iterator, true);
                }
            }

            EditorGUILayout.EndScrollView();

            if (serialized.ApplyModifiedProperties())
                EditorUtility.SetDirty(asset);
        }

        private static void DrawHeader(Object asset, string _)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(asset.name, DMStudioStyles.SectionTitle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Ping", GUILayout.Width(52f), GUILayout.Height(22f)))
                EditorGUIUtility.PingObject(asset);
            if (GUILayout.Button("Select", GUILayout.Width(56f), GUILayout.Height(22f)))
                Selection.activeObject = asset;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
        }
    }
}
#endif

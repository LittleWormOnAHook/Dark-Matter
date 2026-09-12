#if UNITY_EDITOR
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// One Footsteps tab: look profile first, then GameAudio clip libraries.
    /// </summary>
    internal sealed class DMStudioFootstepsPanel
    {
        private const string LookPath = "Assets/_Project/Resources/Player/DM_FootstepProfile.asset";
        private const string AudioPath = "Assets/_Project/Resources/GameAudioProfile.asset";

        private SerializedObject lookSerialized;
        private SerializedObject audioSerialized;
        private Object lookAsset;
        private Object audioAsset;

        public void Draw()
        {
            EditorGUILayout.HelpBox(
                "Terrain splat 0-10 wins (dominant layer only — blended sand/rock does not spawn twice). Then Unity tag, then Default. One dust + mark + clip per foot plant.",
                MessageType.Info);
            EditorGUILayout.Space(6f);

            DrawLookBlock();
            EditorGUILayout.Space(14f);
            DMStudioStyles.DrawAccentLine(
                EditorGUILayout.GetControlRect(false, 2f),
                DarkMatterGenesisUiPalette.SlateGray);
            EditorGUILayout.Space(10f);
            DrawAudioBlock();
        }

        private void DrawLookBlock()
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(LookPath);
            if (asset == null)
            {
                EditorGUILayout.HelpBox("Missing look profile:\n" + LookPath, MessageType.Error);
                return;
            }

            DrawBlockHeader("Look", "Marks, dust, tags, and terrain layers 0-10", asset);
            Bind(ref lookAsset, ref lookSerialized, asset);
            lookSerialized.Update();
            DrawAllVisible(lookSerialized);
            if (lookSerialized.ApplyModifiedProperties())
                EditorUtility.SetDirty(asset);
        }

        private void DrawAudioBlock()
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(AudioPath);
            if (asset == null)
            {
                EditorGUILayout.HelpBox("Missing audio profile:\n" + AudioPath, MessageType.Error);
                return;
            }

            DrawBlockHeader("Audio", "Default fallback, Unity tags, and terrain layers 0-10", asset);
            Bind(ref audioAsset, ref audioSerialized, asset);
            audioSerialized.Update();
            DrawFiltered(audioSerialized, DMStudioProfileSectionFilter.FootstepsAudioOnly);
            if (audioSerialized.ApplyModifiedProperties())
                EditorUtility.SetDirty(asset);
        }

        private static void DrawBlockHeader(string title, string subtitle, Object asset)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(title, DMStudioStyles.SectionTitle);
            EditorGUILayout.LabelField(subtitle, DMStudioStyles.HeroSubtitle);
            EditorGUILayout.EndVertical();
            if (GUILayout.Button("Ping", GUILayout.Width(52f), GUILayout.Height(22f)))
                EditorGUIUtility.PingObject(asset);
            if (GUILayout.Button("Select", GUILayout.Width(56f), GUILayout.Height(22f)))
                Selection.activeObject = asset;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
        }

        private static void Bind(ref Object cached, ref SerializedObject serialized, Object asset)
        {
            if (cached != asset || serialized == null || serialized.targetObject != asset)
            {
                cached = asset;
                serialized = new SerializedObject(asset);
            }
        }

        private static void DrawAllVisible(SerializedObject serialized)
        {
            using (DMStudioStyles.PushLabelWidth(280f))
            {
                SerializedProperty iterator = serialized.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (iterator.propertyPath == "m_Script")
                    {
                        using (new EditorGUI.DisabledScope(true))
                            EditorGUILayout.PropertyField(iterator, true);
                        continue;
                    }

                    EditorGUILayout.PropertyField(iterator, true);
                }
            }
        }

        private static void DrawFiltered(SerializedObject serialized, DMStudioProfileSectionFilter filter)
        {
            using (DMStudioStyles.PushLabelWidth(280f))
            {
                SerializedProperty iterator = serialized.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (!DMStudioProfileSections.IncludesField(iterator.propertyPath, filter))
                        continue;

                    if (iterator.propertyPath == "m_Script")
                        continue;

                    EditorGUILayout.PropertyField(iterator, true);
                }
            }
        }
    }
}
#endif

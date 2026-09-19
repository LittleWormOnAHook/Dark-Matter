#if UNITY_EDITOR
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Jump/Landing tab: jump bounce + land timing profile, 3-tier height band, optional clip refs.
    /// </summary>
    internal sealed class DMStudioLandingPanel
    {
        private const string LandingProfilePath = "Assets/_Project/Resources/Landing/DMLandingProfile.asset";
        private const string ClimbDashPath = "Assets/_Project/Resources/Climb/DM_ClimbDashProfile.asset";
        private const string ClipsPath = "Assets/_Project/Resources/Landing/DMLandingClips.asset";
        private const float ProfileLabelWidth = 300f;

        private readonly DMStudioSectionProfilePanel sectionPanel = new DMStudioSectionProfilePanel();

        private Object landingAsset;
        private SerializedObject landingSerialized;
        private Object clipsAsset;
        private SerializedObject clipsSerialized;

        public void Draw()
        {
            EditorGUILayout.HelpBox(
                "Jump/Landing: regular-jump bounce + land timing apply in Play via DMLandingProfile.Live. " +
                "3-tier height band (short bounce / mid hero / high hero+damage) is on DM_ClimbDashProfile below. " +
                "Planar slide: Zero+Hold Planar OFF = momentum slide for Sliding Landing Hold Seconds; either ON = anti-drift guard (0 = whole clip).",
                MessageType.Info);
            EditorGUILayout.Space(6f);

            sectionPanel.Draw(
                ClimbDashPath,
                DMStudioProfileSectionFilter.LandingHeightsOnly,
                DMStudioProfileSections.GetSectionNote(DMStudioProfileSectionFilter.LandingHeightsOnly));

            EditorGUILayout.Space(14f);
            DMStudioStyles.DrawAccentLine(
                EditorGUILayout.GetControlRect(false, 2f),
                DarkMatterGenesisUiPalette.SlateGray);
            EditorGUILayout.Space(10f);

            DrawProfileBlock(
                LandingProfilePath,
                "Jump + land runtime",
                "Jump bounce toggles, early land, foot snap, probes, clip lengths, slide guard, anti double-land",
                ref landingAsset,
                ref landingSerialized);

            EditorGUILayout.Space(14f);
            DMStudioStyles.DrawAccentLine(
                EditorGUILayout.GetControlRect(false, 2f),
                DarkMatterGenesisUiPalette.SlateGray);
            EditorGUILayout.Space(10f);

            DrawProfileBlock(
                ClipsPath,
                "Clip library",
                "Optional fall / get-up references (animator states are primary)",
                ref clipsAsset,
                ref clipsSerialized);
        }

        private static void DrawProfileBlock(
            string path,
            string title,
            string subtitle,
            ref Object cachedAsset,
            ref SerializedObject serialized)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (asset == null)
            {
                EditorGUILayout.HelpBox("Missing asset:\n" + path, MessageType.Error);
                return;
            }

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

            Bind(ref cachedAsset, ref serialized, asset);
            serialized.Update();
            DrawAllVisible(serialized);
            if (serialized.ApplyModifiedProperties())
                EditorUtility.SetDirty(asset);
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
            using (DMStudioStyles.PushLabelWidth(ProfileLabelWidth))
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
    }
}
#endif

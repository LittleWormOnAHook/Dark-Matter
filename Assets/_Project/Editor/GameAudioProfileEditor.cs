#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Project.Audio.Editor
{
    [CustomEditor(typeof(GameAudioProfile))]
    public class GameAudioProfileEditor : Project.EditorTools.Theme.GenesisImguiEditor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Body impacts = confirmed damage; Weapon / guard clashes = successful blocks/parries. " +
                "Incoming player impact/pain has dedicated local playback. Center circular hit direction is configured below. " +
                "Use Ambient Audio Zone components in the scene for local birds/insects/tree creaks.",
                MessageType.Info);

            EditorGUILayout.HelpBox("Folder pools include subfolders and take priority over manual clip elements. Empty/unassigned pools use the elements below. Refresh pools after adding clips; builds refresh automatically.", MessageType.None);
            SerializedProperty property = serializedObject.GetIterator();
            bool enter = true;
            while (property.NextVisible(enter))
            {
                enter = false;
                if (property.name == "m_Script") continue;
                EditorGUILayout.PropertyField(property, true);
                if (IsCombatClipField(property.name)) DrawFolderPool(property.name);
            }

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            GameAudioProfile profile = (GameAudioProfile)target;
            DrawPreviewButton("Preview Music Track", profile.musicTracks);
            DrawPreviewButton("Preview Footstep", profile.defaultFootsteps?.walkClips);
            DrawPreviewButton("Preview Landing", profile.defaultLandingClips);
            DrawPreviewButton("Preview Weapon Clash (Melee Hit)", profile.ResolveCombatClips("meleeHitClips", profile.meleeHitClips));
            DrawPreviewButton("Preview Critical / Parry Clash", profile.ResolveCombatClips("meleeCriticalHitClips", profile.meleeCriticalHitClips));
            DrawPreviewButton("Preview Body Impact", profile.ResolveCombatClips("meleeBodyHitClips", profile.meleeBodyHitClips));
            DrawPreviewButton("Preview Critical Body Impact", profile.ResolveCombatClips("meleeBodyCriticalHitClips", profile.meleeBodyCriticalHitClips));
            DrawPreviewButton("Preview Player Impact", profile.ResolveCombatClips("playerBodyHitClips", profile.playerBodyHitClips));
            DrawPreviewButton("Preview Player Pain", profile.ResolveCombatClips("playerPainClips", profile.playerPainClips));
            DrawPreviewButton("Preview Ranged Hit", profile.ResolveCombatClips("rangedHitClips", profile.rangedHitClips));
            DrawPreviewButton("Preview Ranged Critical", profile.ResolveCombatClips("rangedCriticalHitClips", profile.rangedCriticalHitClips));
            DrawPreviewButton("Preview Melee Swing", profile.ResolveCombatClips("meleeSwingClips", profile.meleeSwingClips));
            DrawPreviewButton("Preview Button Click", profile.buttonClickClips);
            DrawPreviewButton("Preview Item Use", profile.itemUseClips);
            DrawPreviewButton("Preview Achievement Unlock", profile.achievementUnlockClips);
            DrawPreviewButton("Preview Level Up", profile.levelUpClips);

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Select Resources Profile Copy"))
            {
                GameAudioProfile resourcesProfile = GameAudioProfileAssetSetup.EnsureResourcesProfile();
                if (resourcesProfile != null && resourcesProfile != profile)
                    EditorGUIUtility.PingObject(resourcesProfile);
            }

            EditorGUILayout.HelpBox(
                "Runtime loads Assets/_Project/Resources/GameAudioProfile.asset. " +
                "Ambient zones use AmbientAudioZone components in the scene (Project → Audio → Create Ambient Audio Zone).",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawPreviewButton(string label, AudioClip[] clips)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                GUI.enabled = clips != null && clips.Length > 0;
                if (GUILayout.Button("Play", GUILayout.Width(80f)))
                {
                    AudioClip clip = clips[Random.Range(0, clips.Length)];
                    if (clip != null)
                    {
                        StopPreviewClips();
                        AudioUtil.PlayPreviewClip(clip);
                    }
                }

                GUI.enabled = true;
            }
        }

        private static bool IsCombatClipField(string name) => name == "meleeSwingClips" || name == "meleeHitClips" ||
            name == "meleeCriticalHitClips" || name == "meleeBodyHitClips" || name == "meleeBodyCriticalHitClips" ||
            name == "playerBodyHitClips" || name == "playerPainClips" || name == "rangedHitClips" ||
            name == "rangedCriticalHitClips" || name == "punchSwingClips" || name == "punchHitClips" ||
            name == "punchCriticalHitClips" || name == "resourceHitClips" || name == "poiseStaggerDingClips";

        private void DrawFolderPool(string field)
        {
            SerializedProperty pools = serializedObject.FindProperty("combatFolderPools");
            SerializedProperty entry = null;
            for (int i = 0; i < pools.arraySize; i++)
                if (pools.GetArrayElementAtIndex(i).FindPropertyRelative("clipField").stringValue == field)
                    entry = pools.GetArrayElementAtIndex(i);
            if (entry == null)
            {
                int i = pools.arraySize++;
                entry = pools.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("clipField").stringValue = field;
                entry.FindPropertyRelative("folderPath").stringValue = "";
                entry.FindPropertyRelative("clips").arraySize = 0;
            }
            SerializedProperty path = entry.FindPropertyRelative("folderPath");
            SerializedProperty clips = entry.FindPropertyRelative("clips");
            using (new EditorGUILayout.HorizontalScope())
            {
                DefaultAsset old = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path.stringValue);
                EditorGUI.BeginChangeCheck();
                DefaultAsset next = (DefaultAsset)EditorGUILayout.ObjectField("Folder pool", old, typeof(DefaultAsset), false);
                bool changed = EditorGUI.EndChangeCheck();
                bool refresh = GUILayout.Button("Refresh", GUILayout.Width(65f));
                if (changed || refresh)
                {
                    string candidate = next != null ? AssetDatabase.GetAssetPath(next) : "";
                    if (next == null || AssetDatabase.IsValidFolder(candidate))
                    {
                        path.stringValue = candidate;
                        AudioClip[] found = DMCombatAudioFolderCache.Scan(candidate);
                        clips.arraySize = found.Length;
                        for (int i = 0; i < found.Length; i++) clips.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
                    }
                    else Debug.LogWarning("Choose a Project folder for an audio pool, not a file.");
                }
            }
            EditorGUILayout.LabelField(clips.arraySize > 0 ? $"Pool: {clips.arraySize} clips (takes priority)" : "Pool empty — using manual clip elements", EditorStyles.miniLabel);
        }

        private static void StopPreviewClips()
        {
            AudioUtil.StopAllPreviewClips();
        }
    }

    internal static class AudioUtil
    {
        public static void PlayPreviewClip(AudioClip clip)
        {
            System.Type audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtil == null)
                return;

            System.Reflection.MethodInfo method = audioUtil.GetMethod(
                "PlayPreviewClip",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null,
                new[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null);

            method?.Invoke(null, new object[] { clip, 0, false });
        }

        public static void StopAllPreviewClips()
        {
            System.Type audioUtil = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtil == null)
                return;

            System.Reflection.MethodInfo method = audioUtil.GetMethod(
                "StopAllPreviewClips",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            method?.Invoke(null, null);
        }
    }
}
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Project.Audio.Editor
{
    [CustomEditor(typeof(GameAudioProfile))]
    public class GameAudioProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Assign clips here for music, footsteps, combat hits, and ambient layers. " +
                "Use Ambient Audio Zone components in the scene for local birds/insects/tree creaks.",
                MessageType.Info);

            DrawDefaultInspector();

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            GameAudioProfile profile = (GameAudioProfile)target;
            DrawPreviewButton("Preview Music Track", profile.musicTracks);
            DrawPreviewButton("Preview Footstep", profile.defaultFootsteps?.walkClips);
            DrawPreviewButton("Preview Landing", profile.defaultLandingClips);
            DrawPreviewButton("Preview Weapon Hit", profile.weaponHitClips);
            DrawPreviewButton("Preview Critical Hit", profile.weaponCriticalHitClips);
            DrawPreviewButton("Preview Button Click", profile.buttonClickClips);
            DrawPreviewButton("Preview Item Use", profile.itemUseClips);
            DrawPreviewButton("Preview Achievement Unlock", profile.achievementUnlockClips);

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

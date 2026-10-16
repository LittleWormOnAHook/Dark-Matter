#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Project.Audio;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Project.Audio.Editor
{
    /// <summary>Bakes folder contents into direct clip references for player builds.</summary>
    [InitializeOnLoad]
    public sealed class DMCombatAudioFolderCache : AssetPostprocessor, IPreprocessBuildWithReport
    {
        private static bool queued;
        public int callbackOrder => 0;
        static DMCombatAudioFolderCache() { QueueRefresh(); }

        public static AudioClip[] Scan(string path)
        {
            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path)) return Array.Empty<AudioClip>();
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { path });
            var paths = new List<string>(guids.Length);
            foreach (string guid in guids) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort(StringComparer.Ordinal);
            var clips = new List<AudioClip>(paths.Count);
            foreach (string file in paths)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(file);
                if (clip != null) clips.Add(clip);
            }
            return clips.ToArray();
        }

        public static void RefreshAll()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:GameAudioProfile"))
            {
                GameAudioProfile profile = AssetDatabase.LoadAssetAtPath<GameAudioProfile>(AssetDatabase.GUIDToAssetPath(guid));
                if (profile == null || profile.combatFolderPools == null) continue;
                bool changed = false;
                foreach (CombatAudioFolderPool pool in profile.combatFolderPools)
                {
                    if (pool == null) continue;
                    AudioClip[] clips = Scan(pool.folderPath);
                    bool same = pool.clips != null && pool.clips.Length == clips.Length;
                    for (int i = 0; same && i < clips.Length; i++) same = clips[i] == pool.clips[i];
                    if (same) continue;
                    pool.clips = clips;
                    changed = true;
                }
                if (!changed) continue;
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
            }
        }

        private static void QueueRefresh()
        {
            if (queued) return;
            queued = true;
            EditorApplication.delayCall += () => { queued = false; RefreshAll(); };
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (moved.Length > 0)
            {
                foreach (string guid in AssetDatabase.FindAssets("t:GameAudioProfile"))
                {
                    var profile = AssetDatabase.LoadAssetAtPath<GameAudioProfile>(AssetDatabase.GUIDToAssetPath(guid));
                    if (profile == null || profile.combatFolderPools == null) continue;
                    foreach (var pool in profile.combatFolderPools)
                    {
                        if (pool == null || string.IsNullOrEmpty(pool.folderPath)) continue;
                        for (int i = 0; i < moved.Length; i++)
                            if (pool.folderPath == movedFrom[i] || pool.folderPath.StartsWith(movedFrom[i] + "/", StringComparison.Ordinal))
                            {
                                pool.folderPath = moved[i] + pool.folderPath.Substring(movedFrom[i].Length);
                                EditorUtility.SetDirty(profile);
                            }
                    }
                }
            }
            // No per-frame or per-sound scans; additions/removals/imports refresh once.
            foreach (var paths in new[] { imported, deleted, moved, movedFrom })
                foreach (string path in paths)
                {
                    string extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
                    if (moved.Length > 0 || extension == ".wav" || extension == ".mp3" || extension == ".ogg" ||
                        extension == ".aif" || extension == ".aiff" || extension == ".flac") { QueueRefresh(); return; }
                }
        }

        public void OnPreprocessBuild(BuildReport report) { RefreshAll(); }
    }
}
#endif

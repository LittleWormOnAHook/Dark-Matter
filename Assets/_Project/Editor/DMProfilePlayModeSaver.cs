#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Unity drops ScriptableObject inspector tweaks when you leave Play.
    /// Snapshot climb/landing/jetpack profiles on exit and write them back in edit mode.
    /// Always on. Does not save player transforms, scenes, or prefabs.
    /// </summary>
    [InitializeOnLoad]
    public static class DMProfilePlayModeSaver
    {
        private const string Stamp = "DMProfileSave 0904";
        private const string PrefsEnabled = "DM.ProfilePlayModeSaver.Enabled";
        private const string MenuPath = "Tools/Dark Matter Genesis/Keep Profiles After Play";

        private static readonly string[] Roots =
        {
            "Assets/_Project/Resources/Climb",
            "Assets/_Project/Resources/Landing",
            "Assets/_Project/Features/Jetpack/Data",
        };

        private static readonly string[] ExtraTypes =
        {
            "t:DMJetpackProfile",
        };

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefsEnabled, true);
            set => EditorPrefs.SetBool(PrefsEnabled, value);
        }

        private static string SnapshotPath =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/DMProfilePlayMode.json"));

        static DMProfilePlayModeSaver()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        [MenuItem(MenuPath)]
        private static void ToggleEnabled()
        {
            Enabled = !Enabled;
            Debug.Log(Enabled
                ? $"[{Stamp}] on. Climb/landing/jetpack profile tweaks keep when you exit Play."
                : $"[{Stamp}] off.");
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleEnabledValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        private static void OnPlayMode(PlayModeStateChange state)
        {
            if (!Enabled)
                return;

            if (state == PlayModeStateChange.ExitingPlayMode)
                Capture();
            else if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += Restore;
        }

        private static void Capture()
        {
            var bundle = new Bundle { items = Collect().ToArray() };
            if (bundle.items == null || bundle.items.Length == 0)
            {
                DeleteSnapshot();
                return;
            }

            try
            {
                File.WriteAllText(SnapshotPath, JsonUtility.ToJson(bundle));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{Stamp}] could not write snapshot: {ex.Message}");
            }
        }

        private static List<Item> Collect()
        {
            var items = new List<Item>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int r = 0; r < Roots.Length; r++)
            {
                if (!AssetDatabase.IsValidFolder(Roots[r]))
                    continue;
                AddGuids(items, seen, AssetDatabase.FindAssets("t:ScriptableObject", new[] { Roots[r] }));
            }

            for (int t = 0; t < ExtraTypes.Length; t++)
                AddGuids(items, seen, AssetDatabase.FindAssets(ExtraTypes[t]));

            return items;
        }

        private static void AddGuids(List<Item> items, HashSet<string> seen, string[] guids)
        {
            if (guids == null)
                return;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (so == null || string.IsNullOrEmpty(path) || !seen.Add(path))
                    continue;

                items.Add(new Item
                {
                    path = path,
                    json = EditorJsonUtility.ToJson(so),
                });
            }
        }

        private static void Restore()
        {
            if (!Enabled)
                return;

            string file = SnapshotPath;
            if (!File.Exists(file))
                return;

            Bundle bundle;
            try
            {
                bundle = JsonUtility.FromJson<Bundle>(File.ReadAllText(file));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{Stamp}] could not read snapshot: {ex.Message}");
                DeleteSnapshot();
                return;
            }

            DeleteSnapshot();
            if (bundle == null || bundle.items == null || bundle.items.Length == 0)
                return;

            int wrote = 0;
            for (int i = 0; i < bundle.items.Length; i++)
            {
                Item item = bundle.items[i];
                if (item == null || string.IsNullOrEmpty(item.path) || string.IsNullOrEmpty(item.json))
                    continue;

                ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(item.path);
                if (so == null)
                    continue;

                string now = EditorJsonUtility.ToJson(so);
                if (now == item.json)
                    continue;

                EditorJsonUtility.FromJsonOverwrite(item.json, so);
                EditorUtility.SetDirty(so);
                wrote++;
            }

            if (wrote == 0)
                return;

            AssetDatabase.SaveAssets();
            Debug.Log($"[{Stamp}] kept {wrote} profile asset(s) from Play.");
        }

        private static void DeleteSnapshot()
        {
            try
            {
                if (File.Exists(SnapshotPath))
                    File.Delete(SnapshotPath);
            }
            catch
            {
                // ignore
            }
        }

        [Serializable]
        private class Bundle
        {
            public Item[] items;
        }

        [Serializable]
        private class Item
        {
            public string path;
            public string json;
        }
    }
}
#endif

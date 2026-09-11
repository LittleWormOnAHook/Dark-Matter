#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Draws singleton ScriptableObjects or folder lists with cached custom inspectors.
    /// </summary>
    internal sealed class DMStudioAssetPanel : IDisposable
    {
        private readonly Dictionary<UnityEngine.Object, UnityEditor.Editor> editorCache =
            new Dictionary<UnityEngine.Object, UnityEditor.Editor>();
        private string[] assetPaths = Array.Empty<string>();
        private string[] assetLabels = Array.Empty<string>();
        private int selectedIndex = -1;
        private string search = string.Empty;
        private Vector2 listScroll;
        private Vector2 inspectorScroll;
        private UnityEngine.Object cachedTarget;
        private string lastFolderKey;

        public void Dispose()
        {
            ClearEditors();
        }

        public void DrawSingleton(string assetPath, string description)
        {
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
            {
                EditorGUILayout.HelpBox($"Missing profile at:\n{assetPath}", MessageType.Error);
                if (GUILayout.Button("Ping Expected Path"))
                    Debug.Log($"Expected: {assetPath}");
                return;
            }

            DrawAssetHeader(asset, description);
            DrawInspector(asset);
        }

        public void DrawFolder(string folder, string typeFilter, string description)
        {
            string key = folder + "|" + typeFilter;
            if (lastFolderKey != key)
            {
                lastFolderKey = key;
                RefreshFolder(folder, typeFilter);
            }

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            DMStudioStyles.DrawSection(
                "Assets",
                DMStudioStyles.SidebarPanel,
                DrawListSidebar,
                DarkMatterGenesisUiPalette.FromHex("#4A4A5A"));
            DMStudioStyles.DrawSection(
                "Inspector",
                DMStudioStyles.ContentPanel,
                () => DrawFolderInspector(description),
                DarkMatterGenesisUiPalette.RichFuchsia);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawListSidebar()
        {
            search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
            listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.ExpandHeight(true));

            int shown = 0;
            for (int i = 0; i < assetLabels.Length; i++)
            {
                if (!string.IsNullOrEmpty(search) &&
                    assetLabels[i].IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                bool selected = i == selectedIndex;
                GUIStyle style = selected ? DMStudioStyles.ListButtonSelected : DMStudioStyles.ListButton;
                if (GUILayout.Button(assetLabels[i], style))
                {
                    if (selectedIndex != i)
                    {
                        selectedIndex = i;
                        cachedTarget = null;
                    }
                }

                shown++;
            }

            if (shown == 0)
                EditorGUILayout.LabelField("No matching assets.", EditorStyles.centeredGreyMiniLabel);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Height(22f)))
                RefreshFolderFromKey();
            if (GUILayout.Button("Ping Folder", GUILayout.Height(22f)))
            {
                UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GetFolderFromKey());
                if (folder != null)
                    EditorGUIUtility.PingObject(folder);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFolderInspector(string description)
        {
            if (!string.IsNullOrEmpty(description))
            {
                EditorGUILayout.LabelField(description, DMStudioStyles.HeroSubtitle);
                EditorGUILayout.Space(4f);
            }

            if (selectedIndex < 0 || selectedIndex >= assetPaths.Length)
            {
                EditorGUILayout.HelpBox("Select an asset from the list.", MessageType.Info);
                return;
            }

            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPaths[selectedIndex]);
            if (asset == null)
            {
                EditorGUILayout.HelpBox("Asset missing — click Refresh.", MessageType.Warning);
                return;
            }

            DrawAssetHeader(asset, null);
            DrawInspector(asset);
        }

        private void DrawAssetHeader(UnityEngine.Object asset, string description)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(asset.name, DMStudioStyles.SectionTitle);
            if (!string.IsNullOrEmpty(description))
                EditorGUILayout.LabelField(description, DMStudioStyles.HeroSubtitle);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Ping", GUILayout.Width(52f), GUILayout.Height(22f)))
                EditorGUIUtility.PingObject(asset);
            if (GUILayout.Button("Select", GUILayout.Width(56f), GUILayout.Height(22f)))
                Selection.activeObject = asset;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4f);
        }

        private void DrawInspector(UnityEngine.Object target)
        {
            if (target == null)
                return;

            inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);
            GetOrCreateEditor(target)?.OnInspectorGUI();
            EditorGUILayout.EndScrollView();

            if (GUI.changed && target != null)
                EditorUtility.SetDirty(target);
        }

        private UnityEditor.Editor GetOrCreateEditor(UnityEngine.Object target)
        {
            if (target == null)
                return null;

            if (editorCache.TryGetValue(target, out UnityEditor.Editor existing) && existing != null)
            {
                cachedTarget = target;
                return existing;
            }

            cachedTarget = target;
            UnityEditor.Editor created = UnityEditor.Editor.CreateEditor(target);
            editorCache[target] = created;
            return created;
        }

        private void RefreshFolder(string folder, string typeFilter)
        {
            string[] guids = AssetDatabase.FindAssets(typeFilter, new[] { folder });
            var paths = new List<string>(guids.Length);
            var labels = new List<string>(guids.Length);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path))
                    continue;
                paths.Add(path);
                labels.Add(System.IO.Path.GetFileNameWithoutExtension(path));
            }

            var pairs = new List<(string path, string label)>(paths.Count);
            for (int i = 0; i < paths.Count; i++)
                pairs.Add((paths[i], labels[i]));
            pairs.Sort((a, b) => string.Compare(a.label, b.label, StringComparison.OrdinalIgnoreCase));
            paths.Clear();
            labels.Clear();
            for (int i = 0; i < pairs.Count; i++)
            {
                paths.Add(pairs[i].path);
                labels.Add(pairs[i].label);
            }

            assetPaths = paths.ToArray();
            assetLabels = labels.ToArray();
            selectedIndex = assetPaths.Length > 0 ? 0 : -1;
            cachedTarget = null;
        }

        private void RefreshFolderFromKey()
        {
            if (string.IsNullOrEmpty(lastFolderKey))
                return;

            int pipe = lastFolderKey.IndexOf('|');
            if (pipe <= 0)
                return;

            RefreshFolder(lastFolderKey.Substring(0, pipe), lastFolderKey.Substring(pipe + 1));
        }

        private string GetFolderFromKey()
        {
            int pipe = lastFolderKey?.IndexOf('|') ?? -1;
            return pipe > 0 ? lastFolderKey.Substring(0, pipe) : string.Empty;
        }

        private void ClearEditors()
        {
            foreach (KeyValuePair<UnityEngine.Object, UnityEditor.Editor> pair in editorCache)
            {
                if (pair.Value != null)
                    UnityEngine.Object.DestroyImmediate(pair.Value);
            }

            editorCache.Clear();
            cachedTarget = null;
        }
    }
}
#endif

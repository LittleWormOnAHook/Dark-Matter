#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Project.Combat;
using Project.EditorTools;
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
        private DMStudioProfileSectionFilter folderSectionFilter;

        // 2026-10-22a: resizable asset column + common-prefix header + full-name tooltips.
        private const string SidebarWidthPrefKey = "DM.GenesisStudio.AssetPanel.SidebarWidth";
        private const float SidebarMinWidth = 160f;
        private const float SidebarMaxWidth = 640f;
        private const float SidebarDefaultWidth = 260f;
        private float sidebarWidth = -1f;
        private string commonPrefix = string.Empty;
        private string[] displayLabels = Array.Empty<string>();
        private static readonly int SplitterHash = "DMStudioAssetPanelSplitter".GetHashCode();

        public void Dispose()
        {
            ClearEditors();
        }

        public void DrawSingleton(string assetPath, string description)
        {
            ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
            {
                var blockers = DMStudioActionValidation.CollectMissingProfileAsset(assetPath, "Profile asset");
                DMStudioActionValidation.DrawBlockersHelpBox(blockers, "edit this profile");
                EditorGUILayout.HelpBox($"Missing profile at:\n{assetPath}", MessageType.Error);
                if (GUILayout.Button("Ping Expected Path"))
                    Debug.Log($"Expected: {assetPath}");
                return;
            }

            DrawAssetHeader(asset, description);
            DrawInspector(asset);
        }

        public void DrawFolder(
            string folder,
            string typeFilter,
            string description,
            DMStudioProfileSectionFilter sectionFilter = DMStudioProfileSectionFilter.None)
        {
            string key = folder + "|" + typeFilter + "|" + (int)sectionFilter;
            folderSectionFilter = sectionFilter;
            if (lastFolderKey != key)
            {
                lastFolderKey = key;
                RefreshFolder(folder, typeFilter);
            }

            if (sidebarWidth < 0f)
                sidebarWidth = Mathf.Clamp(EditorPrefs.GetFloat(SidebarWidthPrefKey, SidebarDefaultWidth), SidebarMinWidth, SidebarMaxWidth);

            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical(GUILayout.Width(sidebarWidth), GUILayout.ExpandHeight(true));
            DMStudioStyles.DrawSection(
                "Assets",
                DMStudioStyles.SidebarPanel,
                DrawListSidebar,
                DarkMatterGenesisUiPalette.FromHex("#4A4A5A"));
            EditorGUILayout.EndVertical();
            DrawSidebarSplitter();
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
            if (!string.IsNullOrEmpty(commonPrefix))
                EditorGUILayout.LabelField(new GUIContent(commonPrefix + "*", "Common prefix hidden from the rows below"), EditorStyles.miniBoldLabel);
            listScroll = EditorGUILayout.BeginScrollView(listScroll, GUILayout.ExpandHeight(true));

            int shown = 0;
            for (int i = 0; i < assetLabels.Length; i++)
            {
                if (!string.IsNullOrEmpty(search) &&
                    assetLabels[i].IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                bool selected = i == selectedIndex;
                GUIStyle style = selected ? DMStudioStyles.ListButtonSelected : DMStudioStyles.ListButton;
                string shownLabel = i < displayLabels.Length ? displayLabels[i] : assetLabels[i];
                if (GUILayout.Button(new GUIContent(shownLabel, assetLabels[i]), style))
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

        private void DrawSidebarSplitter()
        {
            Rect r = GUILayoutUtility.GetRect(6f, 6f, GUILayout.Width(6f), GUILayout.ExpandHeight(true));
            int id = GUIUtility.GetControlID(SplitterHash, FocusType.Passive);
            EditorGUIUtility.AddCursorRect(r, MouseCursor.ResizeHorizontal);
            Event e = Event.current;
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (e.button == 0 && r.Contains(e.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        sidebarWidth = Mathf.Clamp(sidebarWidth + e.delta.x, SidebarMinWidth, SidebarMaxWidth);
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        EditorPrefs.SetFloat(SidebarWidthPrefKey, sidebarWidth);
                        e.Use();
                    }
                    break;
                case EventType.Repaint:
                    EditorGUI.DrawRect(new Rect(r.center.x - 0.5f, r.y, 1f, r.height),
                        GUIUtility.hotControl == id ? new Color(1f, 1f, 1f, 0.45f) : new Color(1f, 1f, 1f, 0.12f));
                    break;
            }
        }

        private static string ComputeCommonPrefix(string[] labels)
        {
            if (labels == null || labels.Length < 2)
                return string.Empty;
            string p = labels[0];
            for (int i = 1; i < labels.Length && p.Length > 0; i++)
            {
                int n = 0, max = Math.Min(p.Length, labels[i].Length);
                while (n < max && p[n] == labels[i][n]) n++;
                p = p.Substring(0, n);
            }
            int us = p.LastIndexOf('_');
            if (us <= 0) return string.Empty;
            p = p.Substring(0, us + 1);
            for (int i = 0; i < labels.Length; i++)
                if (labels[i].Length <= p.Length) return string.Empty;
            return p;
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
            if (folderSectionFilter != DMStudioProfileSectionFilter.None)
                DrawFilteredInspector(asset, folderSectionFilter);
            else
                DrawInspector(asset);
        }

        private void DrawFilteredInspector(UnityEngine.Object asset, DMStudioProfileSectionFilter section)
        {
            string note = DMStudioProfileSections.GetSectionNote(section);
            if (!string.IsNullOrEmpty(note))
            {
                EditorGUILayout.HelpBox(note, MessageType.Info);
                EditorGUILayout.Space(4f);
            }

            SerializedObject serialized = new SerializedObject(asset);
            serialized.Update();
            inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);
            using (DMStudioStyles.BeginProfileInspector(serialized, path => DMStudioProfileSections.IncludesField(path, section)))
            {
                SerializedProperty iterator = serialized.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (!DMStudioSearch.MatchesProperty(iterator)) continue; // 2026-10-12 Studio search
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

            // 2026-10-20t: body weight reset (Locomotion body weight + weapon weight groups).
            if (asset is Project.Features.Climb.DM_ClimbDashProfile climbProfile && DMStudioProfileSections.IncludesField("bodyWeightEnabled", section))
            {
                EditorGUILayout.Space(6f);
                if (GUILayout.Button("Reset Body Weight to Default", GUILayout.Height(24f)))
                {
                    Undo.RecordObject(climbProfile, "Reset Body Weight to Default");
                    climbProfile.ResetBodyWeightToDefault();
                    EditorUtility.SetDirty(climbProfile);
                    AssetDatabase.SaveAssetIfDirty(climbProfile);
                    GUIUtility.ExitGUI();
                }
            }

            // 2026-10-24o: wall hand touch presets.
            if (asset is Project.Features.Climb.DM_ClimbDashProfile wallProfile && DMStudioProfileSections.IncludesField("bodyWeightWallHandEnabled", section))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Wall hand touch presets", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Wall Hand: Subtle")) { Undo.RecordObject(wallProfile, "Wall hand preset"); wallProfile.ApplyWallHandPreset(0); EditorUtility.SetDirty(wallProfile); }
                if (GUILayout.Button("Wall Hand: Default")) { Undo.RecordObject(wallProfile, "Wall hand preset"); wallProfile.ApplyWallHandPreset(1); EditorUtility.SetDirty(wallProfile); }
                if (GUILayout.Button("Wall Hand: Extreme")) { Undo.RecordObject(wallProfile, "Wall hand preset"); wallProfile.ApplyWallHandPreset(2); EditorUtility.SetDirty(wallProfile); }
                EditorGUILayout.EndHorizontal();
            }

            // 2026-10-24ac: enemy chase stuck-recovery presets.
            if (asset is Project.AI.DM_EnemyBrainProfile stuckProfile && DMStudioProfileSections.IncludesField("enableChaseStuckRecovery", section))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Chase stuck recovery presets", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Unstick: Subtle")) { Undo.RecordObject(stuckProfile, "Stuck preset"); stuckProfile.ApplyChaseStuckPreset(0); EditorUtility.SetDirty(stuckProfile); }
                if (GUILayout.Button("Unstick: Default")) { Undo.RecordObject(stuckProfile, "Stuck preset"); stuckProfile.ApplyChaseStuckPreset(1); EditorUtility.SetDirty(stuckProfile); }
                if (GUILayout.Button("Unstick: Extreme")) { Undo.RecordObject(stuckProfile, "Stuck preset"); stuckProfile.ApplyChaseStuckPreset(2); EditorUtility.SetDirty(stuckProfile); }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("Obstacle avoidance presets (whisker fan)", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Avoid: Subtle")) { Undo.RecordObject(stuckProfile, "Avoid preset"); stuckProfile.ApplyObstacleAvoidancePreset(0); EditorUtility.SetDirty(stuckProfile); }
                if (GUILayout.Button("Avoid: Default")) { Undo.RecordObject(stuckProfile, "Avoid preset"); stuckProfile.ApplyObstacleAvoidancePreset(1); EditorUtility.SetDirty(stuckProfile); }
                if (GUILayout.Button("Avoid: Extreme")) { Undo.RecordObject(stuckProfile, "Avoid preset"); stuckProfile.ApplyObstacleAvoidancePreset(2); EditorUtility.SetDirty(stuckProfile); }
                EditorGUILayout.EndHorizontal();
            }

            // 2026-10-24m: training dummy reaction presets.
            if (asset is DM_CombatCoreProfile coreProfile && DMStudioProfileSections.IncludesField("trainingDummyPivotBase", section))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Training dummy reaction presets", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Dummy: Snappy (default)")) { Undo.RecordObject(coreProfile, "Dummy preset"); coreProfile.TrainingDummyPresetSnappy(); EditorUtility.SetDirty(coreProfile); }
                if (GUILayout.Button("Dummy: Classic (old)")) { Undo.RecordObject(coreProfile, "Dummy preset"); coreProfile.TrainingDummyPresetClassic(); EditorUtility.SetDirty(coreProfile); }
                if (GUILayout.Button("Dummy: Extreme")) { Undo.RecordObject(coreProfile, "Dummy preset"); coreProfile.TrainingDummyPresetExtreme(); EditorUtility.SetDirty(coreProfile); }
                EditorGUILayout.EndHorizontal();
            }

            DMAmmoFxProfile profile = asset as DMAmmoFxProfile;
            if (profile == null)
                return;

            // 2026-10-24j: chain lightning presets (fields are in the "Chain lightning" header above).
            if (DMStudioProfileSections.IncludesField("chainEnabled", section))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Chain lightning presets", EditorStyles.boldLabel);
                if (!profile.beamUsesAmmoPrefab || !profile.isHitscanBeam)
                    EditorGUILayout.HelpBox("Chain lightning only runs on hitscan ammo with 'Beam Uses Ammo Prefab' on (e.g. Lightning Gun).", MessageType.Info);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Chain: Recommended")) { Undo.RecordObject(profile, "Chain preset"); profile.ChainPresetRecommended(); EditorUtility.SetDirty(profile); }
                if (GUILayout.Button("Chain: Extreme")) { Undo.RecordObject(profile, "Chain preset"); profile.ChainPresetExtreme(); EditorUtility.SetDirty(profile); }
                EditorGUILayout.EndHorizontal();
            }

            // 2026-10-24n: vendor impact audio presets.
            if (DMStudioProfileSections.IncludesField("vendorImpactAudio", section))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Vendor impact audio presets", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Impact Audio: Recommended")) { Undo.RecordObject(profile, "Impact audio preset"); profile.ImpactAudioPresetRecommended(); EditorUtility.SetDirty(profile); }
                if (GUILayout.Button("Impact Audio: Extreme")) { Undo.RecordObject(profile, "Impact audio preset"); profile.ImpactAudioPresetExtreme(); EditorUtility.SetDirty(profile); }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(8f);
            DMAmmoFxProfile selectedProfile = Selection.activeObject as DMAmmoFxProfile;
            var copyBlockers = DMStudioActionValidation.CollectAmmoCopyBlockers(profile, selectedProfile);
            DMStudioActionValidation.DrawBlockersHelpBox(copyBlockers, "Copy From Selected Profile");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Seed Hit-Mark Rows From Catalog"))
            {
                Undo.RecordObject(profile, "Seed ammo hit marks");
                profile.SeedSurfacesFromCatalog();
                EditorUtility.SetDirty(profile);
            }

            if (GUILayout.Button("Copy From Selected Profile"))
            {
                DMStudioActionValidation.TryRunPrimaryAction(
                    DMStudioActionValidation.GenesisStudioDialogTitle,
                    copyBlockers,
                    () =>
                    {
                        Undo.RecordObject(profile, "Copy ammo profile");
                        profile.CopyFrom(selectedProfile);
                        EditorUtility.SetDirty(profile);
                    });
            }

            EditorGUILayout.EndHorizontal();
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
            UnityEditor.Editor ed = GetOrCreateEditor(target);
            using (DMStudioStyles.BeginProfileInspector(280f))
            {
                if (DMStudioSearch.Active && ed != null && !DMStudioSearch.HandlesSearch(ed))
                    DMStudioSearch.DrawFilteredDefault(ed.serializedObject); // 2026-10-12 Studio search
                else
                    ed?.OnInspectorGUI();
            }
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
            commonPrefix = ComputeCommonPrefix(assetLabels);
            displayLabels = new string[assetLabels.Length];
            for (int i = 0; i < assetLabels.Length; i++)
                displayLabels[i] = commonPrefix.Length > 0 ? assetLabels[i].Substring(commonPrefix.Length) : assetLabels[i];
            selectedIndex = assetPaths.Length > 0 ? 0 : -1;
            cachedTarget = null;
        }

        private void RefreshFolderFromKey()
        {
            if (string.IsNullOrEmpty(lastFolderKey))
                return;

            string[] parts = lastFolderKey.Split('|');
            if (parts.Length < 2)
                return;

            RefreshFolder(parts[0], parts[1]);
        }

        private string GetFolderFromKey()
        {
            if (string.IsNullOrEmpty(lastFolderKey))
                return string.Empty;

            int pipe = lastFolderKey.IndexOf('|');
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

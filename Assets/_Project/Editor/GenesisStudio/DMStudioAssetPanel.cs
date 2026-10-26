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

            // 2026-10-25i: controller Journal presets.
            if (asset is Project.UI.DMJournalPadProfile padProfile)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Journal controller presets", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Pad: Subtle")) { Undo.RecordObject(padProfile, "Journal pad preset"); padProfile.ApplyPadPreset(0); EditorUtility.SetDirty(padProfile); }
                if (GUILayout.Button("Pad: Default")) { Undo.RecordObject(padProfile, "Journal pad preset"); padProfile.ApplyPadPreset(1); EditorUtility.SetDirty(padProfile); }
                if (GUILayout.Button("Pad: Extreme")) { Undo.RecordObject(padProfile, "Journal pad preset"); padProfile.ApplyPadPreset(2); EditorUtility.SetDirty(padProfile); }
                EditorGUILayout.EndHorizontal();
            }

            // 2026-10-25d: player sidestep + Slice 4 target switch presets.
            if (asset is Project.Combat.DM_CombatCoreProfile qsProfile && DMStudioProfileSections.IncludesField("quickStepEnabled", section))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Sidestep presets", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Sidestep: Subtle")) { Undo.RecordObject(qsProfile, "Sidestep preset"); qsProfile.ApplyQuickStepPreset(0); EditorUtility.SetDirty(qsProfile); }
                if (GUILayout.Button("Sidestep: Default")) { Undo.RecordObject(qsProfile, "Sidestep preset"); qsProfile.ApplyQuickStepPreset(1); EditorUtility.SetDirty(qsProfile); }
                if (GUILayout.Button("Sidestep: Extreme")) { Undo.RecordObject(qsProfile, "Sidestep preset"); qsProfile.ApplyQuickStepPreset(2); EditorUtility.SetDirty(qsProfile); }
                EditorGUILayout.EndHorizontal();
            }
            if (asset is Project.Combat.DM_CombatCoreProfile dsProfile && DMStudioProfileSections.IncludesField("dashPadHoldEnabled", section))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Directional dash input presets", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Dash Input: Subtle")) { Undo.RecordObject(dsProfile, "Dash input preset"); dsProfile.ApplyDashInputPreset(0); EditorUtility.SetDirty(dsProfile); }
                if (GUILayout.Button("Dash Input: Default")) { Undo.RecordObject(dsProfile, "Dash input preset"); dsProfile.ApplyDashInputPreset(1); EditorUtility.SetDirty(dsProfile); }
                if (GUILayout.Button("Dash Input: Extreme")) { Undo.RecordObject(dsProfile, "Dash input preset"); dsProfile.ApplyDashInputPreset(2); EditorUtility.SetDirty(dsProfile); }
                EditorGUILayout.EndHorizontal();
            }
            if (asset is Project.Combat.DM_CombatCoreProfile jaProfile && DMStudioProfileSections.IncludesField("jumpAttackEnabled", section))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Jump attack presets", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Jump Attack: Subtle")) { Undo.RecordObject(jaProfile, "Jump attack preset"); jaProfile.ApplyJumpAttackPreset(0); EditorUtility.SetDirty(jaProfile); }
                if (GUILayout.Button("Jump Attack: Default")) { Undo.RecordObject(jaProfile, "Jump attack preset"); jaProfile.ApplyJumpAttackPreset(1); EditorUtility.SetDirty(jaProfile); }
                if (GUILayout.Button("Jump Attack: Extreme")) { Undo.RecordObject(jaProfile, "Jump attack preset"); jaProfile.ApplyJumpAttackPreset(2); EditorUtility.SetDirty(jaProfile); }
                EditorGUILayout.EndHorizontal();
                if (GUILayout.Button("Build / rebuild jump attack animator states")) Project.EditorTools.Combat.DMJumpAttackAnimatorSetup.Build();
            }
            if (asset is Project.Combat.DM_CombatCoreProfile tsProfile && DMStudioProfileSections.IncludesField("meleeTargetSwitchEnabled", section))
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Target switch (Slice 4) presets", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Switch: Subtle")) { Undo.RecordObject(tsProfile, "Target switch preset"); tsProfile.ApplyTargetSwitchPreset(0); EditorUtility.SetDirty(tsProfile); }
                if (GUILayout.Button("Switch: Default")) { Undo.RecordObject(tsProfile, "Target switch preset"); tsProfile.ApplyTargetSwitchPreset(1); EditorUtility.SetDirty(tsProfile); }
                if (GUILayout.Button("Switch: Extreme")) { Undo.RecordObject(tsProfile, "Target switch preset"); tsProfile.ApplyTargetSwitchPreset(2); EditorUtility.SetDirty(tsProfile); }
                EditorGUILayout.EndHorizontal();
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

            // 2026-10-24af: carve hot glow + molten drip presets.
            if (asset is Project.SurfaceCarve.DMCarveSettings carveSettings)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Hot cut glow + molten drip presets", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Heat: Subtle")) { Undo.RecordObject(carveSettings, "Carve heat preset"); carveSettings.ApplyCarveHeatPreset(0); EditorUtility.SetDirty(carveSettings); }
                if (GUILayout.Button("Heat: Default")) { Undo.RecordObject(carveSettings, "Carve heat preset"); carveSettings.ApplyCarveHeatPreset(1); EditorUtility.SetDirty(carveSettings); }
                if (GUILayout.Button("Heat: Extreme")) { Undo.RecordObject(carveSettings, "Carve heat preset"); carveSettings.ApplyCarveHeatPreset(2); EditorUtility.SetDirty(carveSettings); }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("Through-hole + falling chunk presets", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Chunks: Subtle")) { Undo.RecordObject(carveSettings, "Carve chunk preset"); carveSettings.ApplyCarveChunkPreset(0); EditorUtility.SetDirty(carveSettings); }
                if (GUILayout.Button("Chunks: Default")) { Undo.RecordObject(carveSettings, "Carve chunk preset"); carveSettings.ApplyCarveChunkPreset(1); EditorUtility.SetDirty(carveSettings); }
                if (GUILayout.Button("Chunks: Extreme")) { Undo.RecordObject(carveSettings, "Carve chunk preset"); carveSettings.ApplyCarveChunkPreset(2); EditorUtility.SetDirty(carveSettings); }
                EditorGUILayout.EndHorizontal();
            }

            // 2026-10-25q: Io Clock subtab - day/night, real-time scale, computed sunrise/sunset + real lengths.
            if (asset is Project.World.Clock.DMIoClockProfile clockProfile25q)
                DMStudioDayNight.Draw(null, clockProfile25q);

            // 2026-10-24ai: Io sky + orbit presets.
            if (asset is Project.World.Sky.DM_IoSkyProfile skyProfile)
            {
                EditorGUILayout.Space(6f);
                DMStudioDayNight.Draw(skyProfile, null); // 25q
                EditorGUILayout.LabelField("Sky & orbit presets (eclipse length, ambient, Jupiter-shine, moon size)", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Sky: Subtle")) { Undo.RecordObject(skyProfile, "Sky preset"); skyProfile.ApplyPreset(0); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Sky: Default")) { Undo.RecordObject(skyProfile, "Sky preset"); skyProfile.ApplyPreset(1); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Sky: Extreme")) { Undo.RecordObject(skyProfile, "Sky preset"); skyProfile.ApplyPreset(2); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Eclipse: Realistic (1.6 h)")) { Undo.RecordObject(skyProfile, "Sky preset"); skyProfile.ApplyRealisticEclipse(); EditorUtility.SetDirty(skyProfile); }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("Realtime indirect light presets (SSGI quality, indirect multiplier, exposure)", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Indirect: Subtle")) { Undo.RecordObject(skyProfile, "Indirect preset"); skyProfile.ApplyIndirectPreset(0); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Indirect: Default")) { Undo.RecordObject(skyProfile, "Indirect preset"); skyProfile.ApplyIndirectPreset(1); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Indirect: Extreme")) { Undo.RecordObject(skyProfile, "Indirect preset"); skyProfile.ApplyIndirectPreset(2); EditorUtility.SetDirty(skyProfile); }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("AAA polish presets (AO, contact/micro shadows, cascades, fog, grade, vignette)", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Polish: Subtle")) { Undo.RecordObject(skyProfile, "Polish preset"); skyProfile.ApplyPolishPreset(0); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Polish: Default")) { Undo.RecordObject(skyProfile, "Polish preset"); skyProfile.ApplyPolishPreset(1); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Polish: Extreme")) { Undo.RecordObject(skyProfile, "Polish preset"); skyProfile.ApplyPolishPreset(2); EditorUtility.SetDirty(skyProfile); }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("Eclipse readability floor (min ambient, dim fill, exposure floor)", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Eclipse floor: Subtle")) { Undo.RecordObject(skyProfile, "Eclipse floor preset"); skyProfile.ApplyEclipseFloorPreset(0); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Eclipse floor: Default")) { Undo.RecordObject(skyProfile, "Eclipse floor preset"); skyProfile.ApplyEclipseFloorPreset(1); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Eclipse floor: Extreme")) { Undo.RecordObject(skyProfile, "Eclipse floor preset"); skyProfile.ApplyEclipseFloorPreset(2); EditorUtility.SetDirty(skyProfile); }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("Eclipse ring & flare (Jupiter limb glow, ring light, ring flare)", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Ring: Subtle")) { Undo.RecordObject(skyProfile, "Ring preset"); skyProfile.ApplyRingPreset(0); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Ring: Default")) { Undo.RecordObject(skyProfile, "Ring preset"); skyProfile.ApplyRingPreset(1); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Ring: Extreme")) { Undo.RecordObject(skyProfile, "Ring preset"); skyProfile.ApplyRingPreset(2); EditorUtility.SetDirty(skyProfile); }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("Night sky (DM_IoNightSky: stars, Milky Way, day fade)", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Night sky: Subtle")) { Undo.RecordObject(skyProfile, "Night sky preset"); skyProfile.ApplyNightSkyPreset(0); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Night sky: Default")) { Undo.RecordObject(skyProfile, "Night sky preset"); skyProfile.ApplyNightSkyPreset(1); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Night sky: Extreme")) { Undo.RecordObject(skyProfile, "Night sky preset"); skyProfile.ApplyNightSkyPreset(2); EditorUtility.SetDirty(skyProfile); }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("Aurora (Io: Na/S blue-green low, O red/violet high)", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Aurora: Subtle")) { Undo.RecordObject(skyProfile, "Aurora preset"); skyProfile.ApplyAuroraPreset(0); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Aurora: Default")) { Undo.RecordObject(skyProfile, "Aurora preset"); skyProfile.ApplyAuroraPreset(1); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Aurora: Extreme")) { Undo.RecordObject(skyProfile, "Aurora preset"); skyProfile.ApplyAuroraPreset(2); EditorUtility.SetDirty(skyProfile); }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("Aurora Height Coverage (25p)", EditorStyles.miniBoldLabel);
                { float v = EditorGUILayout.Slider("Min Elevation (deg)", skyProfile.auroraMinElevDeg, -10f, 80f); if (!Mathf.Approximately(v, skyProfile.auroraMinElevDeg)) { Undo.RecordObject(skyProfile, "Aurora Min Elevation (deg)"); skyProfile.auroraMinElevDeg = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Max Elevation (deg)", skyProfile.auroraMaxElevDeg, 1f, 90f); if (!Mathf.Approximately(v, skyProfile.auroraMaxElevDeg)) { Undo.RecordObject(skyProfile, "Aurora Max Elevation (deg)"); skyProfile.auroraMaxElevDeg = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Height Variation (deg)", skyProfile.auroraHeightVariationDeg, 0f, 40f); if (!Mathf.Approximately(v, skyProfile.auroraHeightVariationDeg)) { Undo.RecordObject(skyProfile, "Aurora Height Variation (deg)"); skyProfile.auroraHeightVariationDeg = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Variation Scale", skyProfile.auroraHeightNoiseScale, 0.2f, 20f); if (!Mathf.Approximately(v, skyProfile.auroraHeightNoiseScale)) { Undo.RecordObject(skyProfile, "Aurora Variation Scale"); skyProfile.auroraHeightNoiseScale = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Bottom Softness (deg)", skyProfile.auroraBottomSoftDeg, 0.05f, 30f); if (!Mathf.Approximately(v, skyProfile.auroraBottomSoftDeg)) { Undo.RecordObject(skyProfile, "Aurora Bottom Softness (deg)"); skyProfile.auroraBottomSoftDeg = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Top Softness (deg)", skyProfile.auroraTopSoftDeg, 0.05f, 45f); if (!Mathf.Approximately(v, skyProfile.auroraTopSoftDeg)) { Undo.RecordObject(skyProfile, "Aurora Top Softness (deg)"); skyProfile.auroraTopSoftDeg = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Azimuth Coverage (deg)", skyProfile.auroraAzimuthSpanDeg, 5f, 360f); if (!Mathf.Approximately(v, skyProfile.auroraAzimuthSpanDeg)) { Undo.RecordObject(skyProfile, "Aurora Azimuth Coverage (deg)"); skyProfile.auroraAzimuthSpanDeg = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Azimuth Centre (deg)", skyProfile.auroraAzimuthCenterDeg, 0f, 360f); if (!Mathf.Approximately(v, skyProfile.auroraAzimuthCenterDeg)) { Undo.RecordObject(skyProfile, "Aurora Azimuth Centre (deg)"); skyProfile.auroraAzimuthCenterDeg = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Azimuth Edge Softness (deg)", skyProfile.auroraAzimuthEdgeSoftDeg, 0f, 180f); if (!Mathf.Approximately(v, skyProfile.auroraAzimuthEdgeSoftDeg)) { Undo.RecordObject(skyProfile, "Aurora Azimuth Edge Softness (deg)"); skyProfile.auroraAzimuthEdgeSoftDeg = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Height Drift Speed", skyProfile.auroraHeightDriftSpeed, 0f, 1f); if (!Mathf.Approximately(v, skyProfile.auroraHeightDriftSpeed)) { Undo.RecordObject(skyProfile, "Aurora Height Drift Speed"); skyProfile.auroraHeightDriftSpeed = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Height Drift Amount (deg)", skyProfile.auroraHeightBobDeg, 0f, 20f); if (!Mathf.Approximately(v, skyProfile.auroraHeightBobDeg)) { Undo.RecordObject(skyProfile, "Aurora Height Drift Amount (deg)"); skyProfile.auroraHeightBobDeg = v; EditorUtility.SetDirty(skyProfile); } }
                EditorGUILayout.LabelField("Sun Flare (25p: horizon/eclipse/night fade + depth occlusion)", EditorStyles.boldLabel);
                { bool b = EditorGUILayout.Toggle("Flare Enabled", skyProfile.flareEnabled); if (b != skyProfile.flareEnabled) { Undo.RecordObject(skyProfile, "Flare Enabled"); skyProfile.flareEnabled = b; EditorUtility.SetDirty(skyProfile); } }
                { bool b = EditorGUILayout.Toggle("Flare Depth Occlusion", skyProfile.flareUseOcclusion); if (b != skyProfile.flareUseOcclusion) { Undo.RecordObject(skyProfile, "Flare Depth Occlusion"); skyProfile.flareUseOcclusion = b; EditorUtility.SetDirty(skyProfile); } }
                { int v = EditorGUILayout.IntSlider("Flare Occlusion Samples", skyProfile.flareOcclusionSamples, 1, 64); if (v != skyProfile.flareOcclusionSamples) { Undo.RecordObject(skyProfile, "Flare samples"); skyProfile.flareOcclusionSamples = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Flare Intensity", skyProfile.flareIntensityScale, 0f, 4f); if (!Mathf.Approximately(v, skyProfile.flareIntensityScale)) { Undo.RecordObject(skyProfile, "Flare Intensity"); skyProfile.flareIntensityScale = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Flare Min Elevation (deg)", skyProfile.flareMinElevationDeg, -10f, 30f); if (!Mathf.Approximately(v, skyProfile.flareMinElevationDeg)) { Undo.RecordObject(skyProfile, "Flare Min Elevation (deg)"); skyProfile.flareMinElevationDeg = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Flare Elevation Fade (deg)", skyProfile.flareElevationFadeDeg, 0.01f, 45f); if (!Mathf.Approximately(v, skyProfile.flareElevationFadeDeg)) { Undo.RecordObject(skyProfile, "Flare Elevation Fade (deg)"); skyProfile.flareElevationFadeDeg = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Flare Eclipse Fade", skyProfile.flareEclipseFade, 0f, 1f); if (!Mathf.Approximately(v, skyProfile.flareEclipseFade)) { Undo.RecordObject(skyProfile, "Flare Eclipse Fade"); skyProfile.flareEclipseFade = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Ring Flare During Eclipse", skyProfile.flareRingDuringEclipse, 0f, 1f); if (!Mathf.Approximately(v, skyProfile.flareRingDuringEclipse)) { Undo.RecordObject(skyProfile, "Ring Flare During Eclipse"); skyProfile.flareRingDuringEclipse = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Flare Occlusion Radius", skyProfile.flareOcclusionRadius, 0f, 5f); if (!Mathf.Approximately(v, skyProfile.flareOcclusionRadius)) { Undo.RecordObject(skyProfile, "Flare Occlusion Radius"); skyProfile.flareOcclusionRadius = v; EditorUtility.SetDirty(skyProfile); } }
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Flare: Subtle")) { Undo.RecordObject(skyProfile, "Flare preset"); skyProfile.ApplyFlarePreset(0); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Flare: Default")) { Undo.RecordObject(skyProfile, "Flare preset"); skyProfile.ApplyFlarePreset(1); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Flare: Extreme")) { Undo.RecordObject(skyProfile, "Flare preset"); skyProfile.ApplyFlarePreset(2); EditorUtility.SetDirty(skyProfile); }
                EditorGUILayout.EndHorizontal();
                { bool es = EditorGUILayout.Toggle("Eclipse soft shadows (Jupiter light)", skyProfile.eclipseShadowsEnabled); if (es != skyProfile.eclipseShadowsEnabled) { Undo.RecordObject(skyProfile, "Eclipse shadows"); skyProfile.eclipseShadowsEnabled = es; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Jupiter Height", skyProfile.jupiterElevationDeg, -10f, 90f); if (!Mathf.Approximately(v, skyProfile.jupiterElevationDeg)) { Undo.RecordObject(skyProfile, "Jupiter Height"); skyProfile.jupiterElevationDeg = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Sky Rotation", skyProfile.jupiterAzimuthDeg, 0f, 360f); if (!Mathf.Approximately(v, skyProfile.jupiterAzimuthDeg)) { Undo.RecordObject(skyProfile, "Sky Rotation"); skyProfile.jupiterAzimuthDeg = v; EditorUtility.SetDirty(skyProfile); } }
                // 25o: edit-mode live preview (auto-saved; orbit time only advances in Play)
                EditorGUILayout.LabelField("Preview: " + (Application.isPlaying ? "off (Play)" : (Project.World.Sky.DMIoSkyController.PreviewActive ? "ACTIVE" : "inactive")) + "  hour " + Project.World.Sky.DMIoSkyController.PreviewHour.ToString("0.00") + " (" + Project.World.Sky.DMIoSkyController.PreviewSource + ")  sun driven: " + (Project.World.Sky.DMIoSkyController.PreviewSunDriven ? "yes" : "no"), EditorStyles.miniBoldLabel);
                { bool b = EditorGUILayout.Toggle("Edit-mode Preview", skyProfile.editPreviewEnabled); if (b != skyProfile.editPreviewEnabled) { Undo.RecordObject(skyProfile, "Edit Preview"); skyProfile.editPreviewEnabled = b; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Preview Hour (-1 = clock)", skyProfile.editPreviewHour, -1f, 30f); if (!Mathf.Approximately(v, skyProfile.editPreviewHour)) { Undo.RecordObject(skyProfile, "Preview Hour"); skyProfile.editPreviewHour = v; EditorUtility.SetDirty(skyProfile); } }
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Preview Dawn 6")) { Undo.RecordObject(skyProfile, "Preview Hour"); skyProfile.editPreviewHour = 6f; EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Preview Eclipse 15")) { Undo.RecordObject(skyProfile, "Preview Hour"); skyProfile.editPreviewHour = 15f; EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Preview Night 27")) { Undo.RecordObject(skyProfile, "Preview Hour"); skyProfile.editPreviewHour = 27f; EditorUtility.SetDirty(skyProfile); }
                EditorGUILayout.EndHorizontal();
                // 25o: star dome coverage (camera-centred, past all terrain tiles)
                EditorGUILayout.LabelField("Star Dome  (radius now " + Project.World.Sky.DMIoNightSky.LastDomeRadius.ToString("0") + " m, world diag " + Project.World.Sky.DMIoNightSky.LastTerrainExtent.ToString("0") + " m" + (Project.World.Sky.DMIoNightSky.LastDomeCapped ? ", CAPPED by camera far clip" : "") + ")", EditorStyles.boldLabel);
                { bool b = EditorGUILayout.Toggle("Cover All Terrain", skyProfile.nightDomeCoverTerrain); if (b != skyProfile.nightDomeCoverTerrain) { Undo.RecordObject(skyProfile, "Dome Cover"); skyProfile.nightDomeCoverTerrain = b; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Dome Terrain Margin (m)", skyProfile.nightDomeTerrainMargin, 0f, 20000f); if (!Mathf.Approximately(v, skyProfile.nightDomeTerrainMargin)) { Undo.RecordObject(skyProfile, "Dome Terrain Margin (m)"); skyProfile.nightDomeTerrainMargin = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Dome Min Radius (m)", skyProfile.nightDomeMinRadius, 0f, 100000f); if (!Mathf.Approximately(v, skyProfile.nightDomeMinRadius)) { Undo.RecordObject(skyProfile, "Dome Min Radius (m)"); skyProfile.nightDomeMinRadius = v; EditorUtility.SetDirty(skyProfile); } }
                { float v = EditorGUILayout.Slider("Dome Far Fraction", skyProfile.nightDomeFarFraction, 0.92f, 0.995f); if (!Mathf.Approximately(v, skyProfile.nightDomeFarFraction)) { Undo.RecordObject(skyProfile, "Dome Far Fraction"); skyProfile.nightDomeFarFraction = v; EditorUtility.SetDirty(skyProfile); } }
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Dome Subtle")) { Undo.RecordObject(skyProfile, "Dome preset"); skyProfile.nightDomeCoverTerrain = true; skyProfile.nightDomeTerrainMargin = 100f; skyProfile.nightDomeFarFraction = 0.95f; EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Dome Default")) { Undo.RecordObject(skyProfile, "Dome preset"); skyProfile.nightDomeCoverTerrain = true; skyProfile.nightDomeTerrainMargin = 500f; skyProfile.nightDomeFarFraction = 0.98f; EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Dome Extreme")) { Undo.RecordObject(skyProfile, "Dome preset"); skyProfile.nightDomeCoverTerrain = true; skyProfile.nightDomeTerrainMargin = 5000f; skyProfile.nightDomeFarFraction = 0.995f; EditorUtility.SetDirty(skyProfile); }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField("Start Hour (Io hours, 0-30; noon 15 = eclipse centre). Current: " + skyProfile.startHour.ToString("0.##"), EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Dawn")) { Undo.RecordObject(skyProfile, "Start hour"); skyProfile.ApplyStartHourPreset(6f); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Morning 7.5")) { Undo.RecordObject(skyProfile, "Start hour"); skyProfile.ApplyStartHourPreset(7.5f); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Noon 15")) { Undo.RecordObject(skyProfile, "Start hour"); skyProfile.ApplyStartHourPreset(15f); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Dusk")) { Undo.RecordObject(skyProfile, "Start hour"); skyProfile.ApplyStartHourPreset(24f); EditorUtility.SetDirty(skyProfile); }
                if (GUILayout.Button("Eclipse/Night")) { Undo.RecordObject(skyProfile, "Start hour"); skyProfile.ApplyStartHourPreset(15.5f); EditorUtility.SetDirty(skyProfile); }
                EditorGUILayout.EndHorizontal();
            }

            // 2026-10-24af: global ground blood decal cap presets.
            if (asset is Project.Combat.DM_EnemyHitMarkProfile hitMarkProfile)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Ground blood decal cap presets", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Decal Cap: Subtle (12)")) { Undo.RecordObject(hitMarkProfile, "Decal cap preset"); hitMarkProfile.ApplyGroundDecalCapPreset(0); EditorUtility.SetDirty(hitMarkProfile); }
                if (GUILayout.Button("Decal Cap: Default (24)")) { Undo.RecordObject(hitMarkProfile, "Decal cap preset"); hitMarkProfile.ApplyGroundDecalCapPreset(1); EditorUtility.SetDirty(hitMarkProfile); }
                if (GUILayout.Button("Decal Cap: Extreme (60)")) { Undo.RecordObject(hitMarkProfile, "Decal cap preset"); hitMarkProfile.ApplyGroundDecalCapPreset(2); EditorUtility.SetDirty(hitMarkProfile); }
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

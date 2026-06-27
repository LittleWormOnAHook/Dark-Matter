using Project.EditorTools.UiLayout;
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.UiLayout
{
    public partial class UiStudioWindow
    {
        private UiStudioBrowserTab browserTab = UiStudioBrowserTab.Panels;
        private UiStudioPreviewSource previewSource = UiStudioPreviewSource.Scene;
        private Vector2 scriptableScroll;
        private Vector2 scriptableInspectorScroll;
        private ScriptableObject selectedScriptable;
        private UiLayoutProfile activeProfile;
        private string activePanelId;

        private void DrawStudioHeader()
        {
            UiLayoutEditorStyles.DrawSection("UI Studio", UiLayoutEditorStyles.HeaderBar, () =>
            {
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Play sync: edit live UI, click Capture (saves rects, sprites, sizes to scene + profile), or enable Maintenance → Persist Play Mode Edits.",
                    MessageType.Warning);
            }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Preview scene sandbox for runtime-built panels. Save layout to UiLayoutProfile assets; runtime hosts apply profiles before default code layout.",
                        MessageType.Info);
                }
            });
        }

        private void DrawPreviewToolbar()
        {
            UiLayoutEditorStyles.DrawSection("Preview", UiLayoutEditorStyles.ToolbarPanel, () =>
            {
                EditorGUILayout.BeginHorizontal();
                previewSource = (UiStudioPreviewSource)EditorGUILayout.EnumPopup("Source", previewSource, GUILayout.Width(220f));

                if (GUILayout.Button("Open Preview Scene", GUILayout.Width(140f)))
                    UiPreviewSceneSetup.CreateOrOpenPreviewScene();

                using (new EditorGUI.DisabledScope(previewSource != UiStudioPreviewSource.Sandbox))
                {
                    if (GUILayout.Button("Rebuild Sandbox", GUILayout.Width(120f)))
                        RebuildSandboxForActivePanel();
                }

                using (new EditorGUI.DisabledScope(previewSource != UiStudioPreviewSource.PlaySync || !Application.isPlaying))
                {
                    if (GUILayout.Button("Use Play Canvas", GUILayout.Width(120f)))
                        TryResolveCanvasFromPlayMode();
                }

                if (GUILayout.Button("Apply Theme", GUILayout.Width(100f)) && rootCanvas != null)
                    UiStudioPreviewHost.ApplyThemeToHierarchy(rootCanvas.transform);

                EditorGUILayout.EndHorizontal();

                DrawProfileToolbar();
            });
        }

        private void DrawProfileToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            activeProfile = (UiLayoutProfile)EditorGUILayout.ObjectField("Layout Profile", activeProfile, typeof(UiLayoutProfile), false);

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(activePanelId)))
            {
                if (GUILayout.Button("Load Profile", GUILayout.Width(90f)))
                    activeProfile = UiStudioProfileIO.LoadProfile(activePanelId);

                using (new EditorGUI.DisabledScope(selectedRect == null && GetActivePanelRoot() == null))
                {
                    if (GUILayout.Button("Save to Profile", GUILayout.Width(110f)))
                        SaveSelectionToProfile(includeHierarchy: false);

                    if (GUILayout.Button("Save Hierarchy", GUILayout.Width(110f)))
                        SaveSelectionToProfile(includeHierarchy: true);
                }

                using (new EditorGUI.DisabledScope(activeProfile == null || GetActivePanelRoot() == null))
                {
                    if (GUILayout.Button("Apply Profile", GUILayout.Width(100f)))
                        ApplyActiveProfileToPanel();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBrowserTabs()
        {
            UiLayoutEditorStyles.DrawSection("Browser", UiLayoutEditorStyles.GamePanelsPanel, () =>
            {
                browserTab = (UiStudioBrowserTab)GUILayout.Toolbar((int)browserTab, new[] { "Panels", "Prefabs", "Scriptables" });
                EditorGUILayout.Space(4f);

                switch (browserTab)
                {
                    case UiStudioBrowserTab.Panels:
                        DrawPanelsBrowser();
                        break;
                    case UiStudioBrowserTab.Prefabs:
                        DrawPrefabsBrowser();
                        break;
                    case UiStudioBrowserTab.Scriptables:
                        DrawScriptablesBrowser();
                        break;
                }
            });
        }

        private void DrawPanelsBrowser()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Prepare Scene Panels", GUILayout.Width(140f)))
                PrepareAllPanels(includePlayModeOnly: false);

            if (GUILayout.Button("Prepare All (Play)", GUILayout.Width(130f)))
                PrepareAllPanels(includePlayModeOnly: true);

            if (Application.isPlaying && GUILayout.Button("Capture Visible Panels", GUILayout.Width(150f)))
                CaptureVisiblePanels();
            EditorGUILayout.EndHorizontal();

            DrawInventorySlotPrefabBar();
            EditorGUILayout.Space(4f);

            panelScroll = EditorGUILayout.BeginScrollView(panelScroll, GUILayout.MaxHeight(220f));
            string currentCategory = null;
            for (int i = 0; i < UiLayoutEditorPanelRegistry.Panels.Length; i++)
            {
                UiPanelDefinition panel = UiLayoutEditorPanelRegistry.Panels[i];
                if (string.IsNullOrEmpty(panel.PanelId) && panel.Category == "Prefabs")
                    continue;

                if (currentCategory != panel.Category)
                {
                    currentCategory = panel.Category;
                    EditorGUILayout.LabelField(currentCategory, EditorStyles.miniBoldLabel);
                }

                DrawStudioPanelRow(panel);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPrefabsBrowser()
        {
            panelScroll = EditorGUILayout.BeginScrollView(panelScroll, GUILayout.MaxHeight(260f));
            for (int i = 0; i < UiLayoutEditorPanelRegistry.Panels.Length; i++)
            {
                UiPanelDefinition panel = UiLayoutEditorPanelRegistry.Panels[i];
                if (panel.Category != "Prefabs" && string.IsNullOrEmpty(panel.PrefabAssetPath))
                    continue;

                DrawStudioPanelRow(panel);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawScriptablesBrowser()
        {
            scriptableScroll = EditorGUILayout.BeginScrollView(scriptableScroll, GUILayout.MaxHeight(260f));
            string currentCategory = null;
            for (int i = 0; i < UiStudioCatalog.Scriptables.Length; i++)
            {
                UiStudioScriptableEntry entry = UiStudioCatalog.Scriptables[i];
                if (currentCategory != entry.Category)
                {
                    currentCategory = entry.Category;
                    EditorGUILayout.LabelField(currentCategory, EditorStyles.miniBoldLabel);
                }

                EditorGUILayout.BeginHorizontal();
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(entry.AssetPath);
                bool exists = asset != null;

                using (new EditorGUI.DisabledScope(!exists))
                {
                    if (GUILayout.Button("Select", GUILayout.Width(52f)) && asset != null)
                    {
                        selectedScriptable = asset;
                        Selection.activeObject = asset;
                        EditorGUIUtility.PingObject(asset);
                    }
                }

                EditorGUILayout.LabelField(entry.Label, GUILayout.Width(180f));
                EditorGUILayout.LabelField(exists ? "asset" : "missing", EditorStyles.miniLabel, GUILayout.Width(60f));

                if (!string.IsNullOrEmpty(entry.Description) && GUILayout.Button("?", GUILayout.Width(22f)))
                    EditorUtility.DisplayDialog(entry.Label, entry.Description, "OK");

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (selectedScriptable != null)
            {
                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Scriptable Inspector", EditorStyles.boldLabel);
                scriptableInspectorScroll = EditorGUILayout.BeginScrollView(scriptableInspectorScroll, GUILayout.MaxHeight(220f));
                Editor editor = Editor.CreateEditor(selectedScriptable);
                editor.OnInspectorGUI();
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawStudioPanelRow(UiPanelDefinition panel)
        {
            RectTransform panelRect = UiLayoutEditorPanelRegistry.FindPanelRect(panel, rootCanvas);
            bool hasPrefabAsset = UiLayoutEditorPanelRegistry.HasPrefabAsset(panel);
            bool availableInScene = panelRect != null;
            bool canSelect = availableInScene || hasPrefabAsset || (panel.PlayModeOnly && Application.isPlaying);
            bool needsPlayMode = panel.PlayModeOnly && !Application.isPlaying;
            bool hasProfile = !string.IsNullOrEmpty(panel.PanelId)
                && UiStudioProfileIO.LoadProfile(panel.PanelId) != null;

            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(!canSelect))
            {
                if (GUILayout.Button("Select", GUILayout.Width(52f)))
                {
                    activePanelId = panel.PanelId;
                    SelectGamePanel(panel, panelRect);
                }
            }

            using (new EditorGUI.DisabledScope(needsPlayMode))
            {
                if (GUILayout.Button("Prep", GUILayout.Width(42f)))
                    PreparePanelWithFeedback(panel);
            }

            if (!string.IsNullOrEmpty(panel.PanelId))
            {
                using (new EditorGUI.DisabledScope(!availableInScene && previewSource != UiStudioPreviewSource.Sandbox))
                {
                    if (GUILayout.Button("Prof", GUILayout.Width(40f)))
                    {
                        activePanelId = panel.PanelId;
                        activeProfile = UiStudioProfileIO.GetOrCreateProfile(panel.PanelId);
                        Selection.activeObject = activeProfile;
                    }
                }
            }

            if (Application.isPlaying && availableInScene && GUILayout.Button("Cap", GUILayout.Width(36f)))
                CapturePanelHierarchy(panel, panelRect);

            string status = needsPlayMode ? "▶ play"
                : availableInScene ? "in scene"
                : hasPrefabAsset ? "prefab"
                : panel.PlayModeOnly ? "hidden"
                : "missing";

            EditorGUILayout.LabelField(panel.Label, GUILayout.Width(150f));
            if (hasProfile)
                UiLayoutEditorStyles.DrawMiniBadge("profile", new Color(0.45f, 0.75f, 1f));

            EditorGUILayout.LabelField(status, EditorStyles.miniLabel, GUILayout.Width(70f));

            if (!string.IsNullOrEmpty(panel.Description) && GUILayout.Button("?", GUILayout.Width(22f)))
                EditorUtility.DisplayDialog(panel.Label, panel.Description, "OK");

            EditorGUILayout.EndHorizontal();
        }

        private void RebuildSandboxForActivePanel()
        {
            string panelId = activePanelId;
            if (string.IsNullOrEmpty(panelId) && selectedRect != null)
                panelId = GuessPanelIdFromSelection();

            if (string.IsNullOrEmpty(panelId) || !UiStudioCatalog.SupportsSandboxPreview(panelId))
            {
                EditorUtility.DisplayDialog(
                    "UI Studio",
                    "Sandbox preview is available for Inventory Panel in this slice. Select that panel or set activePanelId.",
                    "OK");
                return;
            }

            Canvas canvas = UiStudioPreviewHost.RebuildSandbox(panelId, out Transform panelRoot);
            if (canvas == null || panelRoot == null)
            {
                EditorUtility.DisplayDialog("UI Studio", "Could not rebuild sandbox preview.", "OK");
                return;
            }

            rootCanvas = canvas;
            activePanelId = panelId;
            activeProfile = UiStudioProfileIO.LoadProfile(panelId);
            selectedRect = panelRoot as RectTransform;
            Selection.activeGameObject = panelRoot.gameObject;
            RebuildFlatList();
            Repaint();
        }

        private string GuessPanelIdFromSelection()
        {
            if (selectedRect == null)
                return null;

            if (selectedRect.name.Contains("Inventory"))
                return UiPanelIds.InventoryPanel;

            return null;
        }

        private Transform GetActivePanelRoot()
        {
            if (selectedRect != null)
                return selectedRect;

            if (string.IsNullOrEmpty(activePanelId))
                return null;

            UiPanelDefinition panel = UiStudioCatalog.FindPanelById(activePanelId);
            if (panel == null)
                return null;

            RectTransform rect = UiLayoutEditorPanelRegistry.FindPanelRect(panel, rootCanvas);
            return rect;
        }

        private void SaveSelectionToProfile(bool includeHierarchy)
        {
            Transform root = GetActivePanelRoot();
            if (root == null || string.IsNullOrEmpty(activePanelId))
            {
                EditorUtility.DisplayDialog("UI Studio", "Select a panel with a PanelId first.", "OK");
                return;
            }

            UiStudioProfileIO.SaveFromPanelRoot(root, activePanelId, includeHierarchy);
            activeProfile = UiStudioProfileIO.LoadProfile(activePanelId);
            EditorUtility.DisplayDialog(
                "UI Studio",
                includeHierarchy
                    ? $"Saved hierarchy to {activeProfile.name}."
                    : $"Saved \"{root.name}\" to {activeProfile.name}.",
                "OK");
        }

        private void ApplyActiveProfileToPanel()
        {
            Transform root = GetActivePanelRoot();
            if (root == null || activeProfile == null)
                return;

            UiStudioProfileIO.ApplyToPanelRoot(root, activeProfile);
            MarkSceneDirtyStatic();
            Repaint();
        }

        private void TryResolveCanvasFromPlayMode()
        {
            if (!UiStudioPreviewHost.TrySyncPlayModeCanvas(out Canvas playCanvas))
                return;

            rootCanvas = playCanvas;
            previewSource = UiStudioPreviewSource.PlaySync;
            RebuildFlatList();
            Repaint();
        }
    }
}

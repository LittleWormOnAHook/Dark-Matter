using System.Collections.Generic;
using Project.EditorTools;
using Project.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project.EditorTools.UiLayout
{
    /// <summary>
    /// Scene UI inspector for editing panel sizes, sprites, colors, layout, buttons, and runtime-only panels.
    /// </summary>
    public class UiLayoutEditorWindow : EditorWindow
    {
        private Canvas rootCanvas;
        private Vector2 treeScroll;
        private Vector2 inspectorScroll;
        private Vector2 panelScroll;
        private string searchFilter = string.Empty;
        private RectTransform selectedRect;
        private RectTransform prefabEditRoot;
        private readonly List<RectTransform> flatList = new List<RectTransform>();
        private UiHierarchyFilter hierarchyFilter = UiHierarchyFilter.All;

        private bool showRectTransform = true;
        private bool showImage = true;
        private bool showRawImage = true;
        private bool showText = true;
        private bool showLayout = true;
        private bool showLayoutElement = true;
        private bool showCanvasGroup = true;
        private bool showCanvasScaler = true;
        private bool showCanvas = true;
        private bool showButton = true;
        private bool showSlider = true;
        private bool showScrollRect = true;
        private bool showInventorySlot = true;
        private bool showGamePanels = true;
        private bool showOtherComponents = true;

        private readonly Dictionary<Component, bool> componentFoldouts = new Dictionary<Component, bool>();

        [MenuItem(SurvivalPioneerEditorMenus.Ui + "UI Layout Editor")]
        public static void ShowWindow()
        {
            UiLayoutEditorWindow window = GetWindow<UiLayoutEditorWindow>("UI Layout Editor");
            window.minSize = new Vector2(920f, 560f);
            window.TryResolveCanvasFromSelection();
        }

        public static void MarkSceneDirtyStatic()
        {
            if (Application.isPlaying)
                return;

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private void OnEnable()
        {
            TryResolveCanvasFromSelection();
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            PrefabStage.prefabStageOpened += HandlePrefabStageChanged;
            PrefabStage.prefabStageClosing += HandlePrefabStageChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            PrefabStage.prefabStageOpened -= HandlePrefabStageChanged;
            PrefabStage.prefabStageClosing -= HandlePrefabStageChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode && state != PlayModeStateChange.EnteredEditMode)
                return;

            selectedRect = null;
            TryResolveCanvasFromSelection();
            Repaint();
        }

        private void HandlePrefabStageChanged(PrefabStage stage)
        {
            RefreshPrefabEditRoot();
            RebuildFlatList();
            Repaint();
        }

        private void OnSelectionChanged()
        {
            if (EditorLayoutGuard.HasStaleInspectorTargets())
            {
                selectedRect = null;
                EditorLayoutGuard.RecoverStaleInspectorState(silent: true, aggressive: false);
                Repaint();
                return;
            }

            RefreshPrefabEditRoot();

            GameObject active = Selection.activeGameObject;
            if (active == null)
            {
                selectedRect = null;
                return;
            }

            RectTransform rect;
            try
            {
                rect = active.GetComponent<RectTransform>();
            }
            catch (MissingReferenceException)
            {
                selectedRect = null;
                EditorLayoutGuard.ClearSelectionAndRebuildInspectors();
                return;
            }

            if (rect == null)
                return;

            selectedRect = rect;
            if (rootCanvas == null)
                rootCanvas = rect.GetComponentInParent<Canvas>();

            Repaint();
        }

        private void OnGUI()
        {
            UiLayoutEditorStyles.DrawSection("UI Layout Editor", UiLayoutEditorStyles.HeaderBar, DrawHeaderHelp);
            UiLayoutEditorStyles.DrawSection("Canvas & Actions", UiLayoutEditorStyles.ToolbarPanel, DrawCanvasBar);
            UiLayoutEditorStyles.DrawSection("Game UI Panels", UiLayoutEditorStyles.GamePanelsPanel, DrawGamePanelsSection);

            if (rootCanvas == null && prefabEditRoot == null)
            {
                EditorGUILayout.HelpBox("Assign a Root Canvas or open the Inventory Slot prefab to begin editing.", MessageType.Warning);
                return;
            }

            if (prefabEditRoot != null)
            {
                EditorGUILayout.HelpBox(
                    "Editing Inventory Slot prefab. Use Save Slot To Prefab to write changes to the asset.",
                    MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            UiLayoutEditorStyles.DrawSection("Hierarchy", UiLayoutEditorStyles.HierarchyPanel, DrawHierarchyPanel);
            UiLayoutEditorStyles.DrawSection("Inspector", UiLayoutEditorStyles.InspectorPanel, DrawInspectorPanel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeaderHelp()
        {
            if (Application.isPlaying)
            {
                string persistNote = PlayModeEditPersistence.Enabled
                    ? "Persist Play Mode Edits is ON — scene changes may auto-save when you stop playing."
                    : "Use Capture Selected/Panel/Hierarchy before stopping Play, or enable Tools → Maintenance → Persist Play Mode Edits.";

                EditorGUILayout.HelpBox(
                    "Play Mode: runtime panels and buttons are fully editable here. " + persistNote,
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Edit mode: scene UI is undoable and saved with the scene. Play-mode-only panels appear after you press Play — use Prepare to stop runtime repositioning, then Capture to persist layout.",
                    MessageType.Info);
            }
        }

        private void DrawCanvasBar()
        {
            EditorGUI.BeginChangeCheck();
            rootCanvas = (Canvas)EditorGUILayout.ObjectField("Root Canvas", rootCanvas, typeof(Canvas), true);
            if (EditorGUI.EndChangeCheck())
            {
                selectedRect = null;
                RebuildFlatList();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Selected Canvas", GUILayout.Width(150f)))
                TryResolveCanvasFromSelection();

            if (GUILayout.Button("Select Canvas", GUILayout.Width(110f)) && rootCanvas != null)
            {
                Selection.activeGameObject = rootCanvas.gameObject;
                EditorGUIUtility.PingObject(rootCanvas.gameObject);
            }

            if (GUILayout.Button("Refresh Tree", GUILayout.Width(100f)))
                RebuildFlatList();

            if (GUILayout.Button("Mark Scene Dirty", GUILayout.Width(120f)))
                MarkSceneDirtyStatic();

            if (GUILayout.Button("Prepare Manual Layout", GUILayout.Width(150f)))
                PrepareSelectedForManualLayout();

            if (Application.isPlaying)
            {
                if (GUILayout.Button("Capture Selected", GUILayout.Width(120f)))
                    CaptureSelectedLayoutToScene(false);

                if (GUILayout.Button("Capture Hierarchy", GUILayout.Width(130f)))
                    CaptureSelectedLayoutToScene(true);

                if (UiLayoutEditorCapture.PendingCount > 0)
                    UiLayoutEditorStyles.DrawMiniBadge($"{UiLayoutEditorCapture.PendingCount} pending", Color.yellow);
            }

            if (GUILayout.Button("Save Slot To Prefab", GUILayout.Width(130f)))
                SaveSelectedSlotToPrefab();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            searchFilter = EditorGUILayout.TextField("Search", searchFilter);
            hierarchyFilter = (UiHierarchyFilter)EditorGUILayout.EnumPopup(hierarchyFilter, GUILayout.Width(120f));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGamePanelsSection()
        {
            showGamePanels = EditorGUILayout.Foldout(showGamePanels, "Browse known panels, mini windows, and prefabs", true);
            if (!showGamePanels)
                return;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Prepare Scene Panels", GUILayout.Width(140f)))
                PrepareAllPanels(includePlayModeOnly: false);

            if (GUILayout.Button("Prepare All (Play)", GUILayout.Width(130f)))
                PrepareAllPanels(includePlayModeOnly: true);

            if (GUILayout.Button("Prepare Selected", GUILayout.Width(120f)))
                PrepareSelectedForManualLayout();

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
                if (currentCategory != panel.Category)
                {
                    currentCategory = panel.Category;
                    EditorGUILayout.LabelField(currentCategory, EditorStyles.miniBoldLabel);
                }

                DrawGamePanelRow(panel);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawGamePanelRow(UiPanelDefinition panel)
        {
            RectTransform panelRect = UiLayoutEditorPanelRegistry.FindPanelRect(panel, rootCanvas);
            bool hasPrefabAsset = UiLayoutEditorPanelRegistry.HasPrefabAsset(panel);
            bool availableInScene = panelRect != null;
            bool canSelect = availableInScene || hasPrefabAsset || (panel.PlayModeOnly && Application.isPlaying);
            bool needsPlayMode = panel.PlayModeOnly && !Application.isPlaying;

            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(!canSelect))
            {
                if (GUILayout.Button("Select", GUILayout.Width(52f)))
                    SelectGamePanel(panel, panelRect);
            }

            using (new EditorGUI.DisabledScope(needsPlayMode))
            {
                if (GUILayout.Button("Prep", GUILayout.Width(42f)))
                    PreparePanelWithFeedback(panel);
            }

            if (Application.isPlaying && availableInScene && GUILayout.Button("Cap", GUILayout.Width(36f)))
                CapturePanelHierarchy(panel, panelRect);

            string status = needsPlayMode ? "▶ play to edit"
                : availableInScene ? "in scene"
                : hasPrefabAsset ? "prefab asset"
                : panel.PlayModeOnly ? "not visible"
                : "missing";

            EditorGUILayout.LabelField(panel.Label, GUILayout.Width(160f));
            EditorGUILayout.LabelField(status, EditorStyles.miniLabel, GUILayout.Width(90f));

            if (!string.IsNullOrEmpty(panel.Description))
            {
                if (GUILayout.Button("?", GUILayout.Width(22f)))
                    EditorUtility.DisplayDialog(panel.Label, panel.Description, "OK");
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawInventorySlotPrefabBar()
        {
            GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiLayoutEditorPanelRegistry.InventorySlotPrefabPath);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Inventory Slot Prefab", slotPrefab, typeof(GameObject), false);
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Open", GUILayout.Width(52f)))
                OpenPrefabForLayoutEditing(UiLayoutEditorPanelRegistry.InventorySlotPrefabPath);

            if (GUILayout.Button("Preview", GUILayout.Width(64f)))
                PlaceInventorySlotPreview();

            if (GUILayout.Button("Prep", GUILayout.Width(42f)))
                UiLayoutEditorPanelRegistry.PrepareInventorySlotLayout();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHierarchyPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(300f), GUILayout.ExpandHeight(true));

            treeScroll = EditorGUILayout.BeginScrollView(treeScroll, GUILayout.ExpandHeight(true));
            if (flatList.Count == 0)
                RebuildFlatList();

            for (int i = 0; i < flatList.Count; i++)
            {
                RectTransform rect = flatList[i];
                if (rect == null)
                    continue;

                if (!PassesSearch(rect))
                    continue;

                if (!UiLayoutEditorInspectorDrawers.PassesHierarchyFilter(rect, hierarchyFilter))
                    continue;

                int depth = GetDepth(rect);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(depth * 14f);

                bool isSelected = selectedRect == rect;
                GUIStyle style = isSelected ? EditorStyles.boldLabel : EditorStyles.label;
                if (GUILayout.Button(rect.name, style))
                {
                    selectedRect = rect;
                    Selection.activeGameObject = rect.gameObject;
                    EditorGUIUtility.PingObject(rect.gameObject);
                }

                UiLayoutEditorInspectorDrawers.DrawHierarchyBadges(rect);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawInspectorPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll, GUILayout.ExpandHeight(true));

            if (selectedRect == null)
            {
                EditorGUILayout.HelpBox("Select a UI element from the tree, Game UI Panels list, or Hierarchy.", MessageType.None);
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
                return;
            }

            GameObject target = selectedRect.gameObject;
            EditorGUILayout.LabelField(target.name, EditorStyles.largeLabel);

            EditorGUI.BeginChangeCheck();
            bool active = EditorGUILayout.Toggle("Active", target.activeSelf);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Toggle UI Active");
                target.SetActive(active);
                UiLayoutEditorInspectorDrawers.CommitChange(target, "Toggle UI Active");
            }

            showRectTransform = EditorGUILayout.Foldout(showRectTransform, "Rect Transform", true);
            if (showRectTransform)
                UiLayoutEditorInspectorDrawers.DrawRectTransformSection(selectedRect);

            if (target.TryGetComponent(out Image image))
            {
                showImage = EditorGUILayout.Foldout(showImage, "Image", true);
                if (showImage)
                    UiLayoutEditorInspectorDrawers.DrawImageSection(image);
            }

            if (target.TryGetComponent(out RawImage rawImage))
            {
                showRawImage = EditorGUILayout.Foldout(showRawImage, "Raw Image", true);
                if (showRawImage)
                    UiLayoutEditorInspectorDrawers.DrawRawImageSection(rawImage);
            }

            if (target.TryGetComponent(out TextMeshProUGUI text))
            {
                showText = EditorGUILayout.Foldout(showText, "TextMeshPro", true);
                if (showText)
                    UiLayoutEditorInspectorDrawers.DrawTextSection(text);
            }

            showLayout = EditorGUILayout.Foldout(showLayout, "Layout Groups", true);
            if (showLayout)
                UiLayoutEditorInspectorDrawers.DrawLayoutSection(target);

            if (target.TryGetComponent(out LayoutElement layoutElement))
            {
                showLayoutElement = EditorGUILayout.Foldout(showLayoutElement, "Layout Element", true);
                if (showLayoutElement)
                    UiLayoutEditorInspectorDrawers.DrawLayoutElementSection(layoutElement);
            }

            if (target.TryGetComponent(out CanvasGroup group))
            {
                showCanvasGroup = EditorGUILayout.Foldout(showCanvasGroup, "Canvas Group", true);
                if (showCanvasGroup)
                    UiLayoutEditorInspectorDrawers.DrawCanvasGroupSection(group);
            }

            if (target.TryGetComponent(out Button button))
            {
                showButton = EditorGUILayout.Foldout(showButton, "Button", true);
                if (showButton)
                    UiLayoutEditorInspectorDrawers.DrawButtonSection(button);
            }

            if (target.TryGetComponent(out Slider slider))
            {
                showSlider = EditorGUILayout.Foldout(showSlider, "Slider", true);
                if (showSlider)
                    UiLayoutEditorInspectorDrawers.DrawSliderSection(slider);
            }

            if (target.TryGetComponent(out ScrollRect scrollRect))
            {
                showScrollRect = EditorGUILayout.Foldout(showScrollRect, "Scroll Rect", true);
                if (showScrollRect)
                    UiLayoutEditorInspectorDrawers.DrawScrollRectSection(scrollRect);
            }

            if (target.TryGetComponent(out InventorySlotUI slotUi))
            {
                showInventorySlot = EditorGUILayout.Foldout(showInventorySlot, "Inventory Slot", true);
                if (showInventorySlot)
                    UiLayoutEditorInspectorDrawers.DrawInventorySlotSection(slotUi);
            }

            if (rootCanvas != null && selectedRect == rootCanvas.GetComponent<RectTransform>())
            {
                showCanvas = EditorGUILayout.Foldout(showCanvas, "Canvas", true);
                if (showCanvas)
                    UiLayoutEditorInspectorDrawers.DrawCanvasSection(rootCanvas);

                showCanvasScaler = EditorGUILayout.Foldout(showCanvasScaler, "Canvas Scaler", true);
                if (showCanvasScaler && rootCanvas.TryGetComponent(out CanvasScaler scaler))
                    UiLayoutEditorInspectorDrawers.DrawCanvasScalerSection(scaler);
            }

            showOtherComponents = EditorGUILayout.Foldout(showOtherComponents, "Other Components", true);
            if (showOtherComponents)
                DrawOtherComponents(target);

            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Layout Element"))
                UiLayoutEditorInspectorDrawers.AddLayoutElement(selectedRect);
            if (GUILayout.Button("Ping In Hierarchy"))
            {
                Selection.activeGameObject = target;
                EditorGUIUtility.PingObject(target);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawOtherComponents(GameObject target)
        {
            Component[] components = target.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                    continue;

                if (component is Transform
                    or RectTransform
                    or Image
                    or RawImage
                    or TextMeshProUGUI
                    or LayoutGroup
                    or LayoutElement
                    or CanvasGroup
                    or Button
                    or Slider
                    or ScrollRect
                    or InventorySlotUI
                    or Canvas
                    or CanvasScaler)
                    continue;

                if (!componentFoldouts.TryGetValue(component, out bool foldout))
                    foldout = false;

                UiLayoutEditorInspectorDrawers.DrawSerializedComponent(component, ref foldout);
                componentFoldouts[component] = foldout;
            }
        }

        private void TryResolveCanvasFromSelection()
        {
            if (Selection.activeGameObject != null)
            {
                Canvas selectedCanvas = Selection.activeGameObject.GetComponentInParent<Canvas>();
                if (selectedCanvas != null)
                    rootCanvas = selectedCanvas;
            }

            if (rootCanvas == null)
                rootCanvas = Object.FindAnyObjectByType<Canvas>();

            RebuildFlatList();
        }

        private void RebuildFlatList()
        {
            flatList.Clear();
            RefreshPrefabEditRoot();

            if (prefabEditRoot != null)
            {
                CollectRects(prefabEditRoot);
                return;
            }

            if (rootCanvas == null)
                return;

            RectTransform rootRect = rootCanvas.GetComponent<RectTransform>();
            if (rootRect == null)
                return;

            CollectRects(rootRect);

            if (Application.isPlaying)
            {
                RectTransform frontLayer = UiLayoutEditorPanelRegistry.FindDeepRect(rootCanvas.transform, "UiFrontLayer");
                if (frontLayer != null && !flatList.Contains(frontLayer))
                    CollectRects(frontLayer);
            }
        }

        private void CollectRects(RectTransform parent)
        {
            flatList.Add(parent);
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i) is RectTransform child)
                    CollectRects(child);
            }
        }

        private bool PassesSearch(RectTransform rect)
        {
            if (string.IsNullOrWhiteSpace(searchFilter))
                return true;

            return rect.name.IndexOf(searchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private int GetDepth(RectTransform rect)
        {
            int depth = 0;
            Transform current = rect;
            Transform root = prefabEditRoot != null
                ? prefabEditRoot
                : rootCanvas != null ? rootCanvas.transform : null;
            while (current != null && current != root)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        private void PrepareSelectedForManualLayout()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("UI Layout Editor", "Select a UI object first.", "OK");
                return;
            }

            int preparedCount = 0;
            RectTransform rect = selected.GetComponent<RectTransform>();
            if (rect != null)
                preparedCount += UiLayoutEditorPanelRegistry.TryPrepareFromRect(rect) ? 1 : 0;

            if (preparedCount == 0)
                preparedCount += PreparePanelMatchingSelection(selected);

            EditorUtility.DisplayDialog(
                "UI Layout Editor",
                preparedCount > 0
                    ? $"Manual layout mode enabled for {preparedCount} panel component(s). Save the scene, then edit rects here or in the Inspector."
                    : "No matching runtime layout overrides were found. Use the Game UI Panels list to prepare a known panel, or edit in Play Mode and Capture.",
                "OK");
        }

        private static int PreparePanelMatchingSelection(GameObject selected)
        {
            Transform current = selected.transform;
            while (current != null)
            {
                for (int i = 0; i < UiLayoutEditorPanelRegistry.Panels.Length; i++)
                {
                    UiPanelDefinition panel = UiLayoutEditorPanelRegistry.Panels[i];
                    for (int j = 0; j < panel.SearchNames.Length; j++)
                    {
                        if (current.name != panel.SearchNames[j])
                            continue;

                        return UiLayoutEditorPanelRegistry.PreparePanel(panel) ? 1 : 0;
                    }
                }

                current = current.parent;
            }

            return 0;
        }

        private void PrepareAllPanels(bool includePlayModeOnly)
        {
            int preparedCount = 0;
            for (int i = 0; i < UiLayoutEditorPanelRegistry.Panels.Length; i++)
            {
                UiPanelDefinition panel = UiLayoutEditorPanelRegistry.Panels[i];
                if (panel.PlayModeOnly && !includePlayModeOnly)
                    continue;

                if (panel.PlayModeOnly && !Application.isPlaying)
                    continue;

                if (UiLayoutEditorPanelRegistry.PreparePanel(panel))
                    preparedCount++;
            }

            EditorUtility.DisplayDialog(
                "UI Layout Editor",
                preparedCount > 0
                    ? $"Manual layout mode enabled for {preparedCount} panel(s)."
                    : "No panels were prepared. Enter Play Mode for runtime-only panels, open them in-game, then Prepare or Capture.",
                "OK");
        }

        private static void PreparePanelWithFeedback(UiPanelDefinition panel)
        {
            bool prepared = UiLayoutEditorPanelRegistry.PreparePanel(panel);
            if (!prepared && panel.PlayModeOnly)
            {
                EditorUtility.DisplayDialog(
                    panel.Label,
                    "This panel has no runtime layout override flag. Enter Play Mode, open the panel, edit rects here, then use Capture.",
                    "OK");
                return;
            }

            if (!prepared)
            {
                EditorUtility.DisplayDialog(
                    panel.Label,
                    "Panel not found or no layout flags to disable. Place it in the scene or enter Play Mode.",
                    "OK");
            }
        }

        private void SelectGamePanel(UiPanelDefinition panel, RectTransform panelRect)
        {
            if (panelRect == null)
                panelRect = UiLayoutEditorPanelRegistry.FindPanelRect(panel, rootCanvas);

            if (panelRect != null)
            {
                SelectPanelRect(panelRect);
                return;
            }

            if (!string.IsNullOrEmpty(panel.PrefabAssetPath))
            {
                OpenPrefabForLayoutEditing(panel.PrefabAssetPath);
                return;
            }

            EditorUtility.DisplayDialog(
                "UI Layout Editor",
                panel.PlayModeOnly
                    ? $"\"{panel.Label}\" is not visible yet. Enter Play Mode, open the panel in-game, then click Select again."
                    : $"Could not find \"{panel.Label}\" in the scene.",
                "OK");
        }

        private void OpenPrefabForLayoutEditing(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath)
                || AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                EditorUtility.DisplayDialog("UI Layout Editor", "Inventory slot prefab not found.", "OK");
                return;
            }

            PrefabStageUtility.OpenPrefab(prefabPath);
            RefreshPrefabEditRoot();
            UiLayoutEditorPanelRegistry.PrepareInventorySlotLayout();
            RebuildFlatList();

            if (prefabEditRoot != null)
                SelectPanelRect(prefabEditRoot);
        }

        private void RefreshPrefabEditRoot()
        {
            prefabEditRoot = null;

            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null || stage.assetPath != UiLayoutEditorPanelRegistry.InventorySlotPrefabPath)
                return;

            prefabEditRoot = stage.prefabContentsRoot.GetComponent<RectTransform>();
        }

        private void PlaceInventorySlotPreview()
        {
            if (rootCanvas == null)
                return;

            InventoryUI inventoryUi = Object.FindAnyObjectByType<InventoryUI>();
            Transform parent = inventoryUi != null && inventoryUi.mainInventoryParent != null
                ? inventoryUi.mainInventoryParent
                : rootCanvas.transform;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiLayoutEditorPanelRegistry.InventorySlotPrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("UI Layout Editor", "Inventory slot prefab not found.", "OK");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = "InventorySlot_LayoutPreview";
            Undo.RegisterCreatedObjectUndo(instance, "Place Inventory Slot Preview");

            InventorySlotUI slot = instance.GetComponent<InventorySlotUI>();
            if (slot != null)
                UiLayoutEditorPanelRegistry.SetSerializedBoolIfExists(slot, "preserveManualLayout", true);

            UiLayoutEditorPanelRegistry.PrepareInventorySlotLayout();
            SelectPanelRect(instance.GetComponent<RectTransform>());
        }

        private static void SaveSelectedSlotToPrefab()
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath == UiLayoutEditorPanelRegistry.InventorySlotPrefabPath)
            {
                UiLayoutEditorPanelRegistry.PrepareInventorySlotLayout();
                PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog(
                    "UI Layout Editor",
                    "Inventory slot prefab saved with manual layout persistence enabled.",
                    "OK");
                return;
            }

            GameObject selected = Selection.activeGameObject;
            InventorySlotUI slot = selected != null
                ? selected.GetComponentInParent<InventorySlotUI>()
                : null;

            if (slot == null)
            {
                EditorUtility.DisplayDialog("UI Layout Editor", "Select an inventory slot to save.", "OK");
                return;
            }

            bool prepared = UiLayoutEditorPanelRegistry.SetSerializedBoolIfExists(slot, "preserveManualLayout", true);
            UiLayoutEditorPanelRegistry.PrepareInventorySlotLayout();

            GameObject sourceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(slot.gameObject);
            if (sourceRoot != null && PrefabUtility.IsPartOfPrefabInstance(sourceRoot))
            {
                PrefabUtility.ApplyPrefabInstance(sourceRoot, InteractionMode.UserAction);
                prepared = true;
            }
            else
            {
                prepared |= UiLayoutEditorPanelRegistry.SetSerializedBoolOnPrefabAsset(
                    UiLayoutEditorPanelRegistry.InventorySlotPrefabPath,
                    "preserveManualLayout",
                    true);
            }

            EditorUtility.DisplayDialog(
                "UI Layout Editor",
                prepared
                    ? "Slot layout saved to prefab with manual layout persistence enabled."
                    : "Could not save slot layout to prefab.",
                "OK");
        }

        private void SelectPanelRect(RectTransform panelRect)
        {
            if (panelRect == null)
                return;

            try
            {
                if (!panelRect.gameObject)
                    return;
            }
            catch (MissingReferenceException)
            {
                EditorLayoutGuard.ClearSelectionAndRebuildInspectors();
                return;
            }

            selectedRect = panelRect;
            Selection.activeGameObject = panelRect.gameObject;
            EditorGUIUtility.PingObject(panelRect.gameObject);
            Repaint();
        }

        private void CaptureSelectedLayoutToScene(bool includeHierarchy)
        {
            if (!Application.isPlaying)
                return;

            if (selectedRect == null || rootCanvas == null)
            {
                EditorUtility.DisplayDialog("UI Layout Editor", "Select a UI element to capture first.", "OK");
                return;
            }

            if (includeHierarchy)
                UiLayoutEditorCapture.CaptureHierarchy(selectedRect, rootCanvas.transform);
            else
                UiLayoutEditorCapture.CaptureRect(selectedRect, rootCanvas.transform);

            EditorUtility.DisplayDialog(
                "UI Layout Editor",
                includeHierarchy
                    ? $"Captured \"{selectedRect.name}\" and children. Exit Play Mode to write into the scene."
                    : $"Captured \"{selectedRect.name}\". Exit Play Mode to write into the scene.",
                "OK");
        }

        private void CapturePanelHierarchy(UiPanelDefinition panel, RectTransform panelRect)
        {
            if (!Application.isPlaying || panelRect == null || rootCanvas == null)
                return;

            UiLayoutEditorCapture.CaptureHierarchy(panelRect, rootCanvas.transform);
            Debug.Log($"[UI Layout Editor] Captured panel \"{panel.Label}\" for scene persistence.");
        }

        private void CaptureVisiblePanels()
        {
            if (!Application.isPlaying || rootCanvas == null)
                return;

            int captured = 0;
            for (int i = 0; i < UiLayoutEditorPanelRegistry.Panels.Length; i++)
            {
                UiPanelDefinition panel = UiLayoutEditorPanelRegistry.Panels[i];
                RectTransform rect = UiLayoutEditorPanelRegistry.FindPanelRect(panel, rootCanvas);
                if (rect == null || !rect.gameObject.activeInHierarchy)
                    continue;

                UiLayoutEditorCapture.CaptureHierarchy(rect, rootCanvas.transform);
                captured++;
            }

            EditorUtility.DisplayDialog(
                "UI Layout Editor",
                captured > 0
                    ? $"Captured {captured} visible panel(s). Exit Play Mode to write layouts into the scene."
                    : "No visible panels found. Open panels in-game first, then capture again.",
                "OK");
        }
    }
}

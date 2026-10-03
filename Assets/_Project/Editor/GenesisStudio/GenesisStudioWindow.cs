#if UNITY_EDITOR
using System.Collections.Generic;
using Project.EditorTools.Theme;
using Project.EditorTools.UiLayout;
using Project.Player;
using Project.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Unified profile studio for player, world, combat, crafting, UI, and audio.
    /// With the Genesis Theme on, the window chrome (header, category nav, subtabs, footer) is UI Toolkit in the
    /// "Frontier" style and each panel's IMGUI content is hosted inside it. With the theme off it draws the
    /// original IMGUI layout.
    /// </summary>
    public sealed class GenesisStudioWindow : EditorWindow
    {
        private const string TipText = "Tip: tune profiles in Play mode. Profile Save keeps climb, jetpack, map, and jump/landing edits when you stop.";

        [SerializeField] private int categoryIndex;
        [SerializeField] private int subtabIndex;
        private Vector2 contentScroll;
        private readonly DMStudioAssetPanel assetPanel = new DMStudioAssetPanel();
        private readonly DMStudioSectionProfilePanel sectionProfilePanel = new DMStudioSectionProfilePanel();
        private readonly DMStudioFootstepsPanel footstepsPanel = new DMStudioFootstepsPanel();
        private readonly DMStudioLandingPanel landingPanel = new DMStudioLandingPanel();
        private readonly DMStudioControlsPanel controlsPanel = new DMStudioControlsPanel();
        private readonly DMStudioCameraPanel cameraPanel = new DMStudioCameraPanel();
        private readonly DMStudioCompanionEditorPanel companionEditorPanel = new DMStudioCompanionEditorPanel();
        private readonly DMStudioCompanionSystemsPanel companionSystemsPanel = new DMStudioCompanionSystemsPanel();
        private readonly ItemDataCreatorPanel itemDataPanel = new ItemDataCreatorPanel();
        private readonly DMAmmoCreatorPanel ammoPanel = new DMAmmoCreatorPanel();
        private readonly CraftingItemCreatorPanel craftingItemPanel = new CraftingItemCreatorPanel();
        private readonly DMStudioPickupItemsPanel pickupItemsPanel = new DMStudioPickupItemsPanel();
        private readonly Project.EditorTools.Building.DMBuildingLibraryPanel buildingLibraryPanel = new Project.EditorTools.Building.DMBuildingLibraryPanel();
        private readonly DMStudioStrataPanel strataPanel = new DMStudioStrataPanel();
        private UnityEditor.Editor playerSystemsEditor;
        private DMPlayerSystemsProfile playerSystemsTarget;

        // Themed (UI Toolkit) chrome
        private VisualElement subtabBar;
        private Label subtabDescription;
        private IMGUIContainer contentHost;
        private Label modePill;
        private Label savePill;

        [MenuItem(DarkMatterGenesisEditorMenus.GenesisStudio, false, 5)]
        public static void Open()
        {
            GenesisStudioWindow window = GetWindow<GenesisStudioWindow>("Genesis Studio");
            window.minSize = new Vector2(920f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            GenesisTheme.Changed += RebuildChrome;
        }

        private void OnDisable()
        {
            GenesisTheme.Changed -= RebuildChrome;
            assetPanel.Dispose();
            controlsPanel.Dispose();
            companionEditorPanel.Dispose();
            companionSystemsPanel.Dispose();
            pickupItemsPanel.Dispose();
            strataPanel.Dispose();
            DestroyPlayerSystemsEditor();
        }

        // ------------------------------------------------------------------ Build

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.Clear();
            subtabBar = null;
            subtabDescription = null;
            contentHost = null;
            modePill = null;
            savePill = null;
            ClampIndices();

            if (!GenesisTheme.Apply(root))
            {
                IMGUIContainer legacy = new IMGUIContainer(DrawLegacy);
                legacy.style.flexGrow = 1f;
                root.Add(legacy);
                return;
            }

            root.style.flexDirection = FlexDirection.Column;
            root.Add(BuildHeader());

            VisualElement body = new VisualElement();
            body.AddToClassList("g-studio__body");
            body.Add(BuildNav());

            VisualElement main = new VisualElement();
            main.AddToClassList("g-studio__main");

            subtabBar = new VisualElement();
            subtabBar.AddToClassList("g-subtabs");
            main.Add(subtabBar);

            subtabDescription = new Label();
            subtabDescription.AddToClassList("g-studio__desc");
            main.Add(subtabDescription);

            contentHost = new IMGUIContainer(DrawThemedContent);
            contentHost.AddToClassList("g-studio__content");
            main.Add(contentHost);

            body.Add(main);
            root.Add(body);
            root.Add(BuildFooter());

            RefreshSubtabs();
            RefreshStatus();
            root.schedule.Execute(RefreshStatus).Every(500);
        }

        private void RebuildChrome()
        {
            CreateGUI();
            Repaint();
        }

        private VisualElement BuildHeader()
        {
            VisualElement header = GWidgets.HeaderBar("Genesis Studio");

            Label subtitle = new Label("Survival profiles, map calibration, ammo FX, companions, crafting, UI and audio");
            subtitle.AddToClassList("g-studio__subtitle");
            header.Add(subtitle);

            VisualElement right = new VisualElement();
            right.AddToClassList("g-studio__header-right");
            modePill = GWidgets.Pill("Edit mode", GWidgets.PillKind.Ok);
            savePill = GWidgets.Pill("Profile save off", GWidgets.PillKind.Warn);
            right.Add(modePill);
            right.Add(savePill);
            right.Add(GWidgets.ActionButton("Toggle Profile Save", () =>
            {
                DMProfilePlayModeSaver.Enabled = !DMProfilePlayModeSaver.Enabled;
                RefreshStatus();
            }));
            header.Add(right);
            return header;
        }

        private VisualElement BuildNav()
        {
            ScrollView nav = new ScrollView(ScrollViewMode.Vertical);
            nav.AddToClassList("g-nav");

            Label title = GenesisTheme.HeaderLabel("Categories", 10f);
            title.AddToClassList("g-nav__title");
            nav.Add(title);

            IReadOnlyList<DMStudioCategory> cats = DMStudioRegistry.Categories;
            for (int i = 0; i < cats.Count; i++)
            {
                int index = i;
                nav.Add(GWidgets.NavItem(cats[i].Label, i == categoryIndex, () => SelectCategory(index)));
            }

            return nav;
        }

        private VisualElement BuildFooter()
        {
            VisualElement footer = GWidgets.Footer(out Label tip, TipText);
            footer.AddToClassList("g-studio__footer");
            tip.AddToClassList("g-studio__tip");
            footer.Add(GWidgets.ActionButton("Genesis Tools", () => DarkMatterGenesisToolsWindow.Open()));
            footer.Add(GWidgets.ActionButton("UI Studio", () => UiStudioWindow.ShowWindow()));
            return footer;
        }

        private void SelectCategory(int index)
        {
            if (index == categoryIndex)
                return;

            categoryIndex = index;
            subtabIndex = 0;
            contentScroll = Vector2.zero;
            GUI.FocusControl(null);
            RefreshSubtabs();
        }

        private void SelectSubtab(int index)
        {
            if (index == subtabIndex)
                return;

            subtabIndex = index;
            contentScroll = Vector2.zero;
            GUI.FocusControl(null);
            RefreshSubtabs();
        }

        private void RefreshSubtabs()
        {
            if (subtabBar == null)
                return;

            ClampIndices();
            DMStudioCategory category = DMStudioRegistry.Categories[categoryIndex];
            subtabBar.Clear();
            for (int i = 0; i < category.Subtabs.Length; i++)
            {
                int index = i;
                Button tab = GWidgets.ActionButton(category.Subtabs[i].Label, () => SelectSubtab(index));
                tab.AddToClassList("g-subtab");
                if (i == subtabIndex)
                    tab.AddToClassList("g-subtab--on");
                subtabBar.Add(tab);
            }

            subtabBar.style.display = category.Subtabs.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            string description = category.Subtabs.Length > 0 ? category.Subtabs[subtabIndex].Description : null;
            if (string.IsNullOrEmpty(description))
                description = category.Description;
            subtabDescription.text = description ?? string.Empty;
            subtabDescription.style.display = string.IsNullOrEmpty(description) ? DisplayStyle.None : DisplayStyle.Flex;

            contentHost?.MarkDirtyRepaint();
        }

        private void RefreshStatus()
        {
            if (modePill == null || savePill == null)
                return;

            SetPill(modePill, Application.isPlaying ? "Play mode" : "Edit mode", Application.isPlaying ? GWidgets.PillKind.Warn : GWidgets.PillKind.Ok);
            bool saverOn = DMProfilePlayModeSaver.Enabled;
            SetPill(savePill, saverOn ? "Profile save on" : "Profile save off", saverOn ? GWidgets.PillKind.Ok : GWidgets.PillKind.Bad);
        }

        private static void SetPill(Label pill, string text, GWidgets.PillKind kind)
        {
            pill.text = text.ToUpperInvariant();
            pill.EnableInClassList("g-pill--ok", kind == GWidgets.PillKind.Ok);
            pill.EnableInClassList("g-pill--warn", kind == GWidgets.PillKind.Warn);
            pill.EnableInClassList("g-pill--bad", kind == GWidgets.PillKind.Bad);
        }

        private void ClampIndices()
        {
            IReadOnlyList<DMStudioCategory> cats = DMStudioRegistry.Categories;
            if (categoryIndex < 0 || categoryIndex >= cats.Count)
                categoryIndex = 0;
            if (subtabIndex < 0 || subtabIndex >= cats[categoryIndex].Subtabs.Length)
                subtabIndex = 0;
        }

        private void DrawThemedContent()
        {
            using var genesisImgui = Project.EditorTools.Theme.GenesisImgui.Begin();
            ClampIndices();
            DMStudioCategory category = DMStudioRegistry.Categories[categoryIndex];
            if (category.Subtabs.Length == 0)
                return;

            DrawContentBody(category.Subtabs[subtabIndex], false);
            strataPanel.EndGUI();
        }

        // ------------------------------------------------------------------ Legacy IMGUI layout (theme off)

        private void DrawLegacy()
        {
            ClampIndices();
            DrawHeader();
            DrawCategoryBar();
            DrawSubtabBar();
            DrawContentArea();
            DrawFooter();
            strataPanel.EndGUI();
        }

        private void DrawHeader()
        {
            DMStudioStyles.DrawHeroHeader(
                "Genesis Studio",
                "One place for survival profiles, map calibration, ammo FX, companions, crafting, UI, and audio.",
                () =>
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(200f));
                    if (Application.isPlaying)
                        DMStudioStyles.DrawBadge("PLAY MODE", DarkMatterGenesisUiPalette.Gold, 88f);
                    else
                        DMStudioStyles.DrawBadge("EDIT MODE", DarkMatterGenesisUiPalette.SlateGray, 88f);

                    bool saverOn = DMProfilePlayModeSaver.Enabled;
                    Color saverColor = saverOn
                        ? DarkMatterGenesisUiPalette.PositiveGreen
                        : DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.SoftBeigeGray, 0.85f);
                    DMStudioStyles.DrawBadge(saverOn ? "PROFILE SAVE ON" : "PROFILE SAVE OFF", saverColor, 120f);

                    if (GUILayout.Button("Toggle Profile Save", GUILayout.Height(20f)))
                    {
                        DMProfilePlayModeSaver.Enabled = !DMProfilePlayModeSaver.Enabled;
                        Repaint();
                    }

                    EditorGUILayout.EndVertical();
                });
        }

        private void DrawCategoryBar()
        {
            DMStudioStyles.DrawSection(string.Empty, DMStudioStyles.FooterPanel, () =>
            {
                EditorGUILayout.BeginHorizontal();
                IReadOnlyList<DMStudioCategory> cats = DMStudioRegistry.Categories;
                for (int i = 0; i < cats.Count; i++)
                {
                    DMStudioCategory cat = cats[i];
                    bool selected = categoryIndex == i;
                    if (DMStudioStyles.DrawCategoryTab(cat.Label, selected, cat.Accent, cat.Icon) && !selected)
                    {
                        categoryIndex = i;
                        subtabIndex = 0;
                        GUI.FocusControl(null);
                    }
                }

                EditorGUILayout.EndHorizontal();
            });
        }

        private void DrawSubtabBar()
        {
            DMStudioCategory category = DMStudioRegistry.Categories[categoryIndex];
            if (category.Subtabs.Length == 0)
                return;

            if (subtabIndex >= category.Subtabs.Length)
                subtabIndex = 0;

            DMStudioStyles.DrawSection(category.Label, DMStudioStyles.SidebarPanel, () =>
            {
                if (!string.IsNullOrEmpty(category.Description))
                    EditorGUILayout.LabelField(category.Description, DMStudioStyles.HeroSubtitle);

                EditorGUILayout.BeginHorizontal();
                for (int i = 0; i < category.Subtabs.Length; i++)
                {
                    DMStudioSubtab sub = category.Subtabs[i];
                    bool selected = subtabIndex == i;
                    if (DMStudioStyles.DrawSubTab(sub.Label, selected) && !selected)
                    {
                        subtabIndex = i;
                        GUI.FocusControl(null);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }, category.Accent);
        }

        private void DrawContentArea()
        {
            DMStudioCategory category = DMStudioRegistry.Categories[categoryIndex];
            if (category.Subtabs.Length == 0)
                return;

            DMStudioSubtab sub = category.Subtabs[subtabIndex];
            DMStudioStyles.DrawSection(sub.Label, DMStudioStyles.ContentPanel, () => DrawContentBody(sub, true), category.Accent);
        }

        private void DrawFooter()
        {
            DMStudioStyles.DrawSection(string.Empty, DMStudioStyles.FooterPanel, () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(TipText, DMStudioStyles.HeroSubtitle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Genesis Tools", GUILayout.Width(100f)))
                    DarkMatterGenesisToolsWindow.Open();
                if (GUILayout.Button("UI Studio", GUILayout.Width(80f)))
                    UiStudioWindow.ShowWindow();
                EditorGUILayout.EndHorizontal();
            });
        }

        // ------------------------------------------------------------------ Shared panel content

        private void DrawContentBody(DMStudioSubtab sub, bool showDescription)
        {
            contentScroll = EditorGUILayout.BeginScrollView(contentScroll, GUILayout.ExpandHeight(true));

            if (showDescription && !string.IsNullOrEmpty(sub.Description))
            {
                EditorGUILayout.LabelField(sub.Description, DMStudioStyles.HeroSubtitle);
                EditorGUILayout.Space(6f);
            }

            switch (sub.Mode)
            {
                case DMStudioPanelMode.SingletonAsset:
                    if (sub.SectionFilter != DMStudioProfileSectionFilter.None)
                    {
                        sectionProfilePanel.Draw(
                            sub.AssetPath,
                            sub.SectionFilter,
                            DMStudioProfileSections.GetSectionNote(sub.SectionFilter));
                    }
                    else
                    {
                        assetPanel.DrawSingleton(sub.AssetPath, null);
                    }

                    break;
                case DMStudioPanelMode.AssetFolder:
                    contentScroll = Vector2.zero;
                    EditorGUILayout.EndScrollView();
                    assetPanel.DrawFolder(
                        sub.SearchFolder,
                        sub.TypeFilter,
                        null,
                        sub.SectionFilter);
                    contentScroll = Vector2.zero;
                    EditorGUILayout.BeginScrollView(contentScroll, GUILayout.ExpandHeight(true));
                    break;
                case DMStudioPanelMode.EmbeddedItemData:
                    itemDataPanel.Draw();
                    break;
                case DMStudioPanelMode.EmbeddedAmmo:
                    ammoPanel.Draw();
                    break;
                case DMStudioPanelMode.EmbeddedCraftingItem:
                    craftingItemPanel.Draw();
                    break;
                case DMStudioPanelMode.PickupItemsCombined:
                    pickupItemsPanel.Draw();
                    break;
                case DMStudioPanelMode.BuildingLibrary:
                    buildingLibraryPanel.Draw();
                    break;
                case DMStudioPanelMode.EnvironmentStrata:
                    contentScroll = Vector2.zero;
                    EditorGUILayout.EndScrollView();
                    strataPanel.Draw(this);
                    EditorGUILayout.BeginScrollView(contentScroll, GUILayout.ExpandHeight(true));
                    break;
                case DMStudioPanelMode.ExternalBlueprintTab:
                    DrawExternalBlueprint(sub);
                    break;
                case DMStudioPanelMode.ExternalTool:
                    DrawExternalTool(sub);
                    break;
                case DMStudioPanelMode.PlayerSystemsLink:
                    DrawPlayerSystemsLink();
                    break;
                case DMStudioPanelMode.LandingCombined:
                    landingPanel.Draw();
                    break;
                case DMStudioPanelMode.ControlsInputEditor:
                    controlsPanel.Draw();
                    break;
                case DMStudioPanelMode.FootstepsCombined:
                    footstepsPanel.Draw();
                    break;
                case DMStudioPanelMode.CameraCombined:
                    cameraPanel.Draw();
                    break;
                case DMStudioPanelMode.EmbeddedCompanionEditor:
                    contentScroll = Vector2.zero;
                    EditorGUILayout.EndScrollView();
                    companionEditorPanel.Draw();
                    EditorGUILayout.BeginScrollView(contentScroll, GUILayout.ExpandHeight(true));
                    break;
                case DMStudioPanelMode.EmbeddedCompanionSystems:
                    contentScroll = Vector2.zero;
                    EditorGUILayout.EndScrollView();
                    companionSystemsPanel.Draw();
                    EditorGUILayout.BeginScrollView(contentScroll, GUILayout.ExpandHeight(true));
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawExternalBlueprint(DMStudioSubtab sub)
        {
            EditorGUILayout.HelpBox(
                "Full blueprint authoring (equipment craft, pickup prefabs, registry sync) lives in the Blueprint + Crafting Manager.",
                MessageType.Info);

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Open Blueprint + Crafting Manager", GUILayout.Height(32f)))
                BlueprintCraftingManagerWindow.Open();

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Blueprints Tab", GUILayout.Height(26f)))
                BlueprintCraftingManagerWindow.Open();
            if (GUILayout.Button("Registry Tab", GUILayout.Height(26f)))
                BlueprintCraftingManagerWindow.OpenRegistryTab();
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawExternalTool(DMStudioSubtab sub)
        {
            EditorGUILayout.HelpBox("Opens a dedicated editor window for this surface.", MessageType.None);
            if (GUILayout.Button("Open " + sub.Label, GUILayout.Height(32f)))
            {
                if (!string.IsNullOrEmpty(sub.ExternalMenuPath))
                    EditorApplication.ExecuteMenuItem(sub.ExternalMenuPath);
            }
        }

        private void DrawPlayerSystemsLink()
        {
            EditorGUILayout.HelpBox(
                "DMPlayerSystemsProfile is a MonoBehaviour on the player prefab. Enable or disable climb, jetpack, jump/landing, and related modules.",
                MessageType.Info);

            const string variantPath = "Assets/_Project/Prefabs/Players/Player_v7 Variant.prefab";
            GameObject variant = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
            DMPlayerSystemsProfile profile = variant != null
                ? variant.GetComponentInChildren<DMPlayerSystemsProfile>(true)
                : null;

            if (profile == null)
            {
                profile = Object.FindAnyObjectByType<DMPlayerSystemsProfile>();
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping Player_v7 Variant", GUILayout.Height(28f)) && variant != null)
                EditorGUIUtility.PingObject(variant);
            if (GUILayout.Button("Select Live Profile", GUILayout.Height(28f)) && profile != null)
                Selection.activeObject = profile;
            EditorGUILayout.EndHorizontal();

            if (profile != null)
            {
                EditorGUILayout.Space(8f);
                GetOrCreatePlayerSystemsEditor(profile).OnInspectorGUI();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "No DMPlayerSystemsProfile found on Player_v7 Variant or in the open scene.",
                    MessageType.Warning);
            }
        }

        private UnityEditor.Editor GetOrCreatePlayerSystemsEditor(DMPlayerSystemsProfile profile)
        {
            if (playerSystemsEditor != null && playerSystemsTarget == profile)
                return playerSystemsEditor;

            DestroyPlayerSystemsEditor();
            playerSystemsTarget = profile;
            playerSystemsEditor = UnityEditor.Editor.CreateEditor(profile);
            return playerSystemsEditor;
        }

        private void DestroyPlayerSystemsEditor()
        {
            if (playerSystemsEditor != null)
            {
                DestroyImmediate(playerSystemsEditor);
                playerSystemsEditor = null;
            }

            playerSystemsTarget = null;
        }
    }
}
#endif

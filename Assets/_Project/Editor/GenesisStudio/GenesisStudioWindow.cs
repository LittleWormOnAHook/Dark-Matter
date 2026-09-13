#if UNITY_EDITOR
using System.Collections.Generic;
using Project.EditorTools.UiLayout;
using Project.Player;
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Unified profile studio: Shift-themed tabs for player, world, combat, crafting, UI, and audio.
    /// </summary>
    public sealed class GenesisStudioWindow : EditorWindow
    {
        private int categoryIndex;
        private int subtabIndex;
        private Vector2 contentScroll;
        private readonly DMStudioAssetPanel assetPanel = new DMStudioAssetPanel();
        private readonly DMStudioSectionProfilePanel sectionProfilePanel = new DMStudioSectionProfilePanel();
        private readonly DMStudioFootstepsPanel footstepsPanel = new DMStudioFootstepsPanel();
        private readonly DMStudioCompanionEditorPanel companionEditorPanel = new DMStudioCompanionEditorPanel();
        private readonly DMStudioCompanionSystemsPanel companionSystemsPanel = new DMStudioCompanionSystemsPanel();
        private readonly ItemDataCreatorPanel itemDataPanel = new ItemDataCreatorPanel();
        private readonly DMAmmoCreatorPanel ammoPanel = new DMAmmoCreatorPanel();
        private readonly CraftingItemCreatorPanel craftingItemPanel = new CraftingItemCreatorPanel();
        private UnityEditor.Editor playerSystemsEditor;
        private DMPlayerSystemsProfile playerSystemsTarget;

        [MenuItem(DarkMatterGenesisEditorMenus.GenesisStudio, false, 5)]
        public static void Open()
        {
            GenesisStudioWindow window = GetWindow<GenesisStudioWindow>("Genesis Studio");
            window.minSize = new Vector2(920f, 620f);
            window.Show();
        }

        private void OnDisable()
        {
            assetPanel.Dispose();
            companionEditorPanel.Dispose();
            companionSystemsPanel.Dispose();
            DestroyPlayerSystemsEditor();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawCategoryBar();
            DrawSubtabBar();
            DrawContentArea();
            DrawFooter();
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
            DMStudioSubtab sub = DMStudioRegistry.Categories[categoryIndex].Subtabs[subtabIndex];

            DMStudioStyles.DrawSection(sub.Label, DMStudioStyles.ContentPanel, () =>
            {
                contentScroll = EditorGUILayout.BeginScrollView(contentScroll, GUILayout.ExpandHeight(true));

                if (!string.IsNullOrEmpty(sub.Description))
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
                    case DMStudioPanelMode.ExternalBlueprintTab:
                        DrawExternalBlueprint(sub);
                        break;
                    case DMStudioPanelMode.ExternalTool:
                        DrawExternalTool(sub);
                        break;
                    case DMStudioPanelMode.PlayerSystemsLink:
                        DrawPlayerSystemsLink();
                        break;
                    case DMStudioPanelMode.FootstepsCombined:
                        footstepsPanel.Draw();
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
            }, DMStudioRegistry.Categories[categoryIndex].Accent);
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
                "DMPlayerSystemsProfile is a MonoBehaviour on the player prefab — enable/disable climb, jetpack, landing, and related modules.",
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

        private void DrawFooter()
        {
            DMStudioStyles.DrawSection(string.Empty, DMStudioStyles.FooterPanel, () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    "Tip: tune profiles in Play — Profile Save keeps climb, jetpack, map, and landing edits when you stop.",
                    DMStudioStyles.HeroSubtitle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Genesis Tools", GUILayout.Width(100f)))
                    DarkMatterGenesisToolsWindow.Open();
                if (GUILayout.Button("UI Studio", GUILayout.Width(80f)))
                    UiStudioWindow.ShowWindow();
                EditorGUILayout.EndHorizontal();
            });
        }
    }
}
#endif

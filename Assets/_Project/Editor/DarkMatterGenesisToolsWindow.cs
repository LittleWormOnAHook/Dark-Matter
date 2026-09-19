#if UNITY_EDITOR
using Project.EditorTools.GenesisStudio;
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Genesis-themed hub for Dark Matter Genesis editor utilities.
    /// </summary>
    public class DarkMatterGenesisToolsWindow : EditorWindow
    {
        private const string SearchPref = "DM.ToolsWindow.Search";
        private const string SectionPref = "DM.ToolsWindow.Section";
        private Vector2 contentScroll;
        private string search;
        private int sectionIndex;

        [MenuItem(DarkMatterGenesisEditorMenus.ToolsWindow, false, 0)]
        public static void Open()
        {
            DarkMatterGenesisToolsWindow window = GetWindow<DarkMatterGenesisToolsWindow>("Genesis Tools");
            window.minSize = new Vector2(920f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            search = EditorPrefs.GetString(SearchPref, string.Empty);
            sectionIndex = EditorPrefs.GetInt(SectionPref, 0);
            ClampSectionIndex();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawSearchBar();
            DrawSectionTabs();
            DrawContent();
            DrawFooter();
        }

        private void DrawHeader()
        {
            DMStudioStyles.DrawHeroHeader(
                "Genesis Tools",
                "Editor hub for profiles, authoring, player wiring, combat, world, UI, and maintenance.",
                () =>
                {
                    EditorGUILayout.BeginVertical(GUILayout.Width(210f));
                    if (Application.isPlaying)
                        DMStudioStyles.DrawBadge("PLAY MODE", DarkMatterGenesisUiPalette.Gold, 88f);
                    else
                        DMStudioStyles.DrawBadge("EDIT MODE", DarkMatterGenesisUiPalette.SlateGray, 88f);

                    DMStudioStyles.DrawBadge("SCENE v1.6.3.1", DarkMatterGenesisUiPalette.DeepMagenta, 120f);

                    if (GUILayout.Button("Open Genesis Studio", DMStudioStyles.SubTabInactive, GUILayout.Height(22f)))
                        GenesisStudioWindow.Open();

                    EditorGUILayout.EndVertical();
                });
        }

        private void DrawSearchBar()
        {
            DMStudioStyles.DrawSection(
                "Filter",
                DMStudioStyles.FooterPanel,
                () =>
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    search = EditorGUILayout.TextField(search, EditorStyles.toolbarSearchField);
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorPrefs.SetString(SearchPref, search);
                        EnsureVisibleSectionSelected();
                    }

                    if (GUILayout.Button("Clear", DMStudioStyles.SubTabInactive, GUILayout.Width(56f), GUILayout.Height(22f)))
                    {
                        search = string.Empty;
                        EditorPrefs.SetString(SearchPref, search);
                    }

                    EditorGUILayout.EndHorizontal();
                });
        }

        private void DrawSectionTabs()
        {
            DMToolsSection[] sections = DarkMatterGenesisToolsCatalog.Sections;
            DMStudioStyles.DrawSection(string.Empty, DMStudioStyles.SidebarPanel, () =>
            {
                EditorGUILayout.BeginHorizontal();
                for (int i = 0; i < sections.Length; i++)
                {
                    if (CountMatches(sections[i]) == 0)
                        continue;

                    DMToolsSection section = sections[i];
                    bool selected = sectionIndex == i;
                    string label = string.IsNullOrWhiteSpace(search)
                        ? section.Title
                        : $"{section.Title} ({CountMatches(section)})";

                    if (DMStudioStyles.DrawCategoryTab(
                            label,
                            selected,
                            DMToolsSectionVisual.GetAccent(section.Title),
                            DMToolsSectionVisual.GetIcon(section.Title))
                        && !selected)
                    {
                        sectionIndex = i;
                        EditorPrefs.SetInt(SectionPref, sectionIndex);
                        GUI.FocusControl(null);
                    }
                }

                EditorGUILayout.EndHorizontal();
            });
        }

        private void DrawContent()
        {
            DMToolsSection section = DarkMatterGenesisToolsCatalog.Sections[sectionIndex];
            Color accent = DMToolsSectionVisual.GetAccent(section.Title);

            DMStudioStyles.DrawSection(section.Title, DMStudioStyles.ContentPanel, () =>
            {
                if (!string.IsNullOrEmpty(section.Hint))
                {
                    EditorGUILayout.LabelField(section.Hint, DMStudioStyles.HeroSubtitle);
                    EditorGUILayout.Space(6f);
                }

                int shown = 0;
                contentScroll = EditorGUILayout.BeginScrollView(contentScroll, GUILayout.ExpandHeight(true));
                for (int i = 0; i < section.Entries.Length; i++)
                {
                    DMToolsEntry entry = section.Entries[i];
                    if (!Matches(entry))
                        continue;

                    DrawEntryRow(entry);
                    shown++;
                }

                if (shown == 0)
                    EditorGUILayout.LabelField("No tools match the current filter.", DMStudioStyles.HeroSubtitle);

                EditorGUILayout.EndScrollView();
            }, accent);
        }

        private void DrawEntryRow(DMToolsEntry entry)
        {
            EditorGUILayout.BeginHorizontal();
            string label = GetButtonLabel(entry.MenuPath);
            if (GUILayout.Button(label, DMStudioStyles.ListButton, GUILayout.Width(280f), GUILayout.MinHeight(26f)))
                InvokeToolEntry(entry.MenuPath);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(entry.Description, DMStudioStyles.HeroSubtitle);
            if (entry.Tier != DMToolsTier.Primary)
                DrawTierBadge(entry.Tier);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2f);
        }

        private static void DrawTierBadge(DMToolsTier tier)
        {
            switch (tier)
            {
                case DMToolsTier.Seed:
                    DMStudioStyles.DrawBadge("SEED", DarkMatterGenesisUiPalette.Gold, 48f);
                    break;
                case DMToolsTier.Legacy:
                    DMStudioStyles.DrawBadge("LEGACY", DarkMatterGenesisUiPalette.CharcoalGray, 56f);
                    break;
                case DMToolsTier.Destructive:
                    DMStudioStyles.DrawBadge("DESTRUCTIVE", DarkMatterGenesisUiPalette.DeepMagenta, 92f);
                    break;
            }
        }

        private void DrawFooter()
        {
            DMStudioStyles.DrawSection(string.Empty, DMStudioStyles.FooterPanel, () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    "Playable scene: Dark Matter Genesis v1.6.3.1. Older Genesis / Pioneer scenes are backups only.",
                    DMStudioStyles.HeroSubtitle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Genesis Studio", DMStudioStyles.SubTabInactive, GUILayout.Width(110f), GUILayout.Height(24f)))
                    GenesisStudioWindow.Open();
                if (GUILayout.Button("Blueprint Manager", DMStudioStyles.SubTabInactive, GUILayout.Width(130f), GUILayout.Height(24f)))
                    BlueprintCraftingManagerWindow.Open();
                EditorGUILayout.EndHorizontal();
            });
        }

        private void EnsureVisibleSectionSelected()
        {
            if (CountMatches(DarkMatterGenesisToolsCatalog.Sections[sectionIndex]) > 0)
                return;

            DMToolsSection[] sections = DarkMatterGenesisToolsCatalog.Sections;
            for (int i = 0; i < sections.Length; i++)
            {
                if (CountMatches(sections[i]) > 0)
                {
                    sectionIndex = i;
                    EditorPrefs.SetInt(SectionPref, sectionIndex);
                    return;
                }
            }
        }

        private void ClampSectionIndex()
        {
            int max = DarkMatterGenesisToolsCatalog.Sections.Length - 1;
            if (sectionIndex < 0)
                sectionIndex = 0;
            if (sectionIndex > max)
                sectionIndex = max;
        }

        private int CountMatches(DMToolsSection section)
        {
            int count = 0;
            for (int i = 0; i < section.Entries.Length; i++)
            {
                if (Matches(section.Entries[i]))
                    count++;
            }

            return count;
        }

        private bool Matches(DMToolsEntry entry)
        {
            if (string.IsNullOrWhiteSpace(search))
                return true;

            string needle = search.Trim();
            return Contains(entry.MenuPath, needle)
                || Contains(GetButtonLabel(entry.MenuPath), needle)
                || Contains(entry.Description, needle);
        }

        private static bool Contains(string haystack, string needle)
        {
            return !string.IsNullOrEmpty(haystack)
                && haystack.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void InvokeToolEntry(string menuPath)
        {
            if (menuPath == DarkMatterGenesisEditorMenus.AddWeaponHitboxToSelectedPrefab)
            {
                WeaponPrefabBuilder.AddHitboxToSelectedPrefab();
                return;
            }

            if (menuPath == DarkMatterGenesisEditorMenus.RefreshAllWeaponHitboxes)
            {
                WeaponPrefabBuilder.RefreshAllWeaponHitboxes();
                return;
            }

            if (menuPath == DarkMatterGenesisEditorMenus.SplineCreatorWindowPrimary
                || menuPath == DarkMatterGenesisEditorMenus.SplineCreatorWindow)
            {
                DMSplineCreatorWindow.Open();
                return;
            }

            if (menuPath == DarkMatterGenesisEditorMenus.CreateElectricalLineSpline)
            {
                DMSplineCreatorMenus.CreateElectricalLine();
                return;
            }

            if (menuPath == DarkMatterGenesisEditorMenus.CreateObjectPlacerSpline)
            {
                DMSplineCreatorMenus.CreateObjectPlacerLine();
                return;
            }

            if (menuPath == DarkMatterGenesisEditorMenus.CreateScatterPlacer)
            {
                DMSplineCreatorMenus.CreateScatter();
                return;
            }

            EditorApplication.ExecuteMenuItem(menuPath);
        }

        private static string GetButtonLabel(string menuPath)
        {
            if (string.IsNullOrEmpty(menuPath))
                return string.Empty;

            int lastSlash = menuPath.LastIndexOf('/');
            return lastSlash >= 0 ? menuPath.Substring(lastSlash + 1) : menuPath;
        }
    }
}
#endif

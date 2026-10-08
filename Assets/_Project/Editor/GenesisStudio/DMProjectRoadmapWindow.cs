#if UNITY_EDITOR
using Project.EditorTools;
using Project.EditorTools.Building;
using Project.EditorTools.Combat;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Cross-domain roadmap: World Engine, companions, UI, survival, communications.
    /// Combat and Building roadmaps live in their dedicated studio windows.
    /// </summary>
    public sealed class DMProjectRoadmapWindow : EditorWindow
    {
        const string PrefsTab = "DM.ProjectRoadmap.Tab";

        static readonly string[] Tabs =
        {
            "World Engine",
            "Building",
            "Companions",
            "UI / UITK",
            "Survival",
            "Communications"
        };

        const string DiskStatusPath = "Assets/_Project/Documentation/Architecture/World_Engine_Disk_Status.md";
        const string BuildingScopePath = "Assets/_Project/Documentation/Design/DMG_Building_System_Scope_Plan.md";
        const string CommFrameworkPath =
            "Assets/_Project/Features/Communications/Documentation/Dark_Matter_Communication_Framework.md";
        const string GddPath = "Assets/_Project/GAME_DESIGN_DOCUMENT_5.0.txt";

        Vector2 scroll;
        int tab;

        [MenuItem(DarkMatterGenesisEditorMenus.Project + "Project Roadmap", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Project_Project_Roadmap)]
        public static void Open()
        {
            GetWindow<DMProjectRoadmapWindow>("Project Roadmap");
        }

        public static void OpenTab(int tabIndex)
        {
            var window = GetWindow<DMProjectRoadmapWindow>("Project Roadmap");
            window.tab = Mathf.Clamp(tabIndex, 0, Tabs.Length - 1);
            EditorPrefs.SetInt(PrefsTab, window.tab);
            window.Show();
            window.Focus();
        }

        void OnEnable()
        {
            tab = EditorPrefs.GetInt(PrefsTab, 0);
            if (tab < 0 || tab >= Tabs.Length)
                tab = 0;
        }

        void OnGUI()
        {
            using var genesisTheme = Project.EditorTools.Theme.GenesisImgui.Window(this);

            DMStudioStyles.DrawHeroHeader(
                "Project Roadmap",
                "Phases and disk status for major domains. Combat and Building also expose roadmaps inside their studio windows.");

            DMStudioStyles.DrawHorizontalTabBar(() =>
            {
                for (int i = 0; i < Tabs.Length; i++)
                {
                    if (DMStudioStyles.DrawCategoryTab(Tabs[i], tab == i, new Color(0.45f, 0.28f, 0.55f, 1f)) && tab != i)
                    {
                        tab = i;
                        EditorPrefs.SetInt(PrefsTab, tab);
                        GUI.FocusControl(null);
                    }
                }
            });

            scroll = EditorGUILayout.BeginScrollView(scroll);
            switch (tab)
            {
                case 1:
                    DrawBuildingTab();
                    break;
                case 2:
                    DrawCompanionsTab();
                    break;
                case 3:
                    DrawUiTab();
                    break;
                case 4:
                    DrawSurvivalTab();
                    break;
                case 5:
                    DrawCommunicationsTab();
                    break;
                default:
                    DrawWorldEngineTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        static void DrawWorldEngineTab()
        {
            DMStudioRoadmapPanel.DrawIntro(
                "Authority: World_Engine_Disk_Status.md + GDD Appendix B. Gameplay AI is authored logic only — no in-Play LLM.",
                MessageType.Info);

            DMStudioRoadmapPanel.DrawDocLinks(
                ("Disk status", DiskStatusPath),
                ("GDD 5.0", GddPath),
                ("Reasoning map", "Assets/_Project/Documentation/Architecture/World_Engine_Reasoning_Map.md"));

            DMStudioRoadmapPanel.DrawPhase(
                "0",
                "Doc honesty",
                DMStudioRoadmapPanel.Status.Done,
                "Disk status file and agent rules treat Features/ modules as shipped only when .cs exists on disk.");

            DMStudioRoadmapPanel.DrawPhase(
                "1",
                "World Engine spine",
                DMStudioRoadmapPanel.Status.Done,
                "GameState, WorldState, Directors, Validation Features modules + CompanionSystemsBootstrap chain; smoke F9–F11.");

            DMStudioRoadmapPanel.DrawPhase(
                "2",
                "Internal Communications (rule-based)",
                DMStudioRoadmapPanel.Status.NotStarted,
                "Features/Communications Runtime absent on disk. Presentation-layer radio / PTT — not LocalVoiceLLM in Play.");

            DMStudioRoadmapPanel.DrawPhase(
                "3",
                "Persistent generated world",
                DMStudioRoadmapPanel.Status.NotStarted,
                "World seed in GameSaveData, Generation wrap, save fields — Run 3 backlog.");

            DMStudioRoadmapPanel.DrawPhase(
                "4",
                "Living-world slice",
                DMStudioRoadmapPanel.Status.InProgress,
                "Weather/Simulation director adapters present; richer logic deferred. PPT Phase 1 on disk.");

            DMStudioRoadmapPanel.DrawPhase(
                "—",
                "Deferred terrain / ecology",
                DMStudioRoadmapPanel.Status.Blocked,
                "Gaia 16→64 subtile split plan only. Full Io biomes/ecology prefabs deferred (W0–W8 design docs present). Sulfur Hound prototype exception on disk.");
        }

        static void DrawBuildingTab()
        {
            DMStudioRoadmapPanel.DrawIntro(
                "Component track largely on disk (Sep 2026). Tune ghosts, snap, and library in Building Studio.",
                MessageType.Info);

            if (GUILayout.Button("Open Building Studio → Roadmap", GUILayout.Height(30f)))
                DMBuildingStudioWindow.OpenTab(DMBuildingStudioWindow.TabRoadmap);

            DMStudioRoadmapPanel.DrawDocLinks(("Building scope plan", BuildingScopePath));

            DMStudioRoadmapPanel.DrawPhase(
                "C1",
                "Hold-B build mode + UITK hotbar",
                DMStudioRoadmapPanel.Status.Done,
                "Stone/Iron/Silicate libraries, 4 m lattice snap, short hold-to-build bar, save/load of placed pieces.");

            DMStudioRoadmapPanel.DrawPhase(
                "C2",
                "Building Studio + Genesis Building tabs",
                DMStudioRoadmapPanel.Status.Done,
                "DMBuildingGhostProfile, library panel, creation FX profile; Genesis subtabs mirror groups.");

            DMStudioRoadmapPanel.DrawPhase(
                "C3",
                "Power, doors, BCP hooks",
                DMStudioRoadmapPanel.Status.InProgress,
                "Generator, force-field doors, Build Hub zone, storage crates on disk. Full BCP depth + campaign facilities pending.");

            DMStudioRoadmapPanel.DrawPhase(
                "S2",
                "Materialize path (ghost commit, drain, reverse dissolve)",
                DMStudioRoadmapPanel.Status.NotStarted,
                "Slice 2 full hold-drain materialization not finished; validity hologram rules locked in scope plan.");

            DMStudioRoadmapPanel.DrawPhase(
                "S3",
                "Wreck scan unlocks + map blueprints",
                DMStudioRoadmapPanel.Status.NotStarted,
                "BuildingDefinition SO still missing; style libraries stand in. Prologue placement quests future.");

            DMStudioRoadmapPanel.DrawPhase(
                "Tiers",
                "Steel / Amalgam + in-place upgrade",
                DMStudioRoadmapPanel.Status.NotStarted,
                "Skill gates and tier upgrades deferred per scope plan §19.");
        }

        static void DrawCompanionsTab()
        {
            DMStudioRoadmapPanel.DrawIntro(
                "Combat Plan §11–14 and GDD roster (22 camp / expedition trio). Runtime companion anims paused until UITK HUD slice stable.",
                MessageType.Info);

            DMStudioRoadmapPanel.DrawDocLinks(
                ("Combat plan §11", "Assets/_Project/Documentation/Design/Combat/DMG_Combat_Plan_v2.md"),
                ("GDD 5.0", GddPath));

            DMStudioRoadmapPanel.DrawPhase(
                "1",
                "Expedition trio + Invector bridges",
                DMStudioRoadmapPanel.Status.Done,
                "PioneerRosterManager, follow/combat controllers, H/G hold-follow commands, group buffs.");

            DMStudioRoadmapPanel.DrawPhase(
                "2",
                "Combat roles (Tank, Marksman, …)",
                DMStudioRoadmapPanel.Status.InProgress,
                "Role layering designed; full role assets and radial commands not shipped.");

            DMStudioRoadmapPanel.DrawPhase(
                "3",
                "Utility brain migration (Phase 11)",
                DMStudioRoadmapPanel.Status.NotStarted,
                "Blocked on Humanoid_Enemy Phase 3 brain sign-off. Same brain pattern as §31 #3.");

            DMStudioRoadmapPanel.DrawPhase(
                "4",
                "Downed / Medic revive loop",
                DMStudioRoadmapPanel.Status.InProgress,
                "Injured → Science Lab path on disk; full downed-in-field revive TBD.");

            DMStudioRoadmapPanel.DrawPhase(
                "5",
                "Companion runtime animations",
                DMStudioRoadmapPanel.Status.Blocked,
                "Explicitly paused per project lock until UITK HUD cutover milestones complete.");
        }

        static void DrawUiTab()
        {
            DMStudioRoadmapPanel.DrawIntro(
                "UITK lock: all new UI is UI Toolkit. Hot Cross uses Resources/UI/HotCrossIcons cutouts only.",
                MessageType.Info);

            DMStudioRoadmapPanel.DrawDocLinks(
                ("UITK runtime", "Assets/UI Toolkit/Runtime/DMUiToolkitHud.cs"),
                ("World chrome perf note", "Assets/_Project/Documentation/Design/Combat/DMG_Combat_Plan_v2.md"));

            DMStudioRoadmapPanel.DrawPhase(
                "1",
                "Gameplay HUD + Hot Cross (UITK)",
                DMStudioRoadmapPanel.Status.InProgress,
                "DMUiToolkitHud, HotCross, pilot cluster prototype; legacy uGUI retirement incremental.");

            DMStudioRoadmapPanel.DrawPhase(
                "2",
                "Menus / journal / settings (UITK)",
                DMStudioRoadmapPanel.Status.InProgress,
                "DMUiToolkitMenus, settings apply flow; pause/settings reload rules locked.");

            DMStudioRoadmapPanel.DrawPhase(
                "3",
                "Loot window (UITK)",
                DMStudioRoadmapPanel.Status.InProgress,
                "DM loot chest phases 1–7 shipped per handoff; uGUI loot fallback removal when stable.");

            DMStudioRoadmapPanel.DrawPhase(
                "4",
                "WorldChrome / perf",
                DMStudioRoadmapPanel.Status.Done,
                "Pickup/dot/bar scans throttled (0.2 s); EnemyHealthSceneRegistry for nearest threat.");

            DMStudioRoadmapPanel.DrawPhase(
                "5",
                "UI Studio (legacy uGUI browse)",
                DMStudioRoadmapPanel.Status.InProgress,
                "UiStudioWindow for layout forensics; new surfaces extend UITK, not MainCanvas.");
        }

        static void DrawSurvivalTab()
        {
            DMStudioRoadmapPanel.DrawIntro(
                "Single cold/heat meter, exposure zones, survival pools on Genesis Studio → Survival.",
                MessageType.Info);

            DMStudioRoadmapPanel.DrawDocLinks(
                ("Exposure scripts", "Assets/_Project/Scripts/Survival/Exposure"),
                ("Climb/dash profile (locomotion)", "Assets/_Project/Resources/DM_ClimbDashProfile.asset"));

            DMStudioRoadmapPanel.DrawPhase(
                "1",
                "Exposure + thermal HUD",
                DMStudioRoadmapPanel.Status.Done,
                "Exposure zone kit, crisis HUD modes, survival pools on disk.");

            DMStudioRoadmapPanel.DrawPhase(
                "2",
                "Walk / run / sprint tuning",
                DMStudioRoadmapPanel.Status.Done,
                "Locomotion multipliers on DM_ClimbDashProfile; Genesis Survival subtabs.");

            DMStudioRoadmapPanel.DrawPhase(
                "3",
                "Echo / chronicle hooks",
                DMStudioRoadmapPanel.Status.InProgress,
                "EchoGenerator + building ops save present; full Neural Echo UX later.");

            DMStudioRoadmapPanel.DrawPhase(
                "4",
                "Storm ↔ base pause integration",
                DMStudioRoadmapPanel.Status.InProgress,
                "Environmental crisis banners; Building queue pause preview in prologue scope.");
        }

        static void DrawCommunicationsTab()
        {
            DMStudioRoadmapPanel.DrawIntro(
                "Presentation-layer comms only. LocalAgent / Ollama stays editor bridge — not in playable scene.",
                MessageType.Warning);

            DMStudioRoadmapPanel.DrawDocLinks(
                ("Communication framework", CommFrameworkPath),
                ("Engineering standard", "Assets/_Project/Features/Communications/Documentation/Dark_Matter_Framework_Engineering_Standard.md"));

            DMStudioRoadmapPanel.DrawPhase(
                "1",
                "Features/Communications runtime",
                DMStudioRoadmapPanel.Status.NotStarted,
                "World Engine track #2. Bootstrap order reserves slot after Directors.");

            DMStudioRoadmapPanel.DrawPhase(
                "2",
                "Rule-based radio + PTT presentation",
                DMStudioRoadmapPanel.Status.NotStarted,
                "Procedural/authored voice; swappable TTS adapters — no in-Play LLM.");

            DMStudioRoadmapPanel.DrawPhase(
                "3",
                "Director intent → comms adapter",
                DMStudioRoadmapPanel.Status.NotStarted,
                "ICommunicationsIntentService boundary documented; implementation pending.");

            DMStudioRoadmapPanel.DrawPhase(
                "—",
                "Phase 8.1 LocalVoiceLLM",
                DMStudioRoadmapPanel.Status.Blocked,
                "Explicitly out of product scope per GDD and disk status.");
        }
    }
}
#endif

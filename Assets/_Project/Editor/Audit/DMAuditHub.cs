#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Audit
{
    /// <summary>
    /// One runtime/editor audit that can be launched from the Audit hub. Implement this and the hub picks it up
    /// automatically (TypeCache discovery, no manual registration): the class needs a public parameterless constructor.
    /// Editor-only: lives in an Editor folder, never ships.
    /// </summary>
    public interface IDMAuditTool
    {
        /// <summary>Stable id, also used for headless runs (DMAuditRegistry.Run("enemy-runtime")).</summary>
        string Id { get; }
        string Title { get; }
        string Description { get; }
        /// <summary>Hub grouping, e.g. "Combat".</summary>
        string Category { get; }
        /// <summary>True while a run is in progress (play-mode audits can span domain reloads).</summary>
        bool IsRunning { get; }
        /// <summary>Short status line for the hub (progress / last result).</summary>
        string Status { get; }
        /// <summary>Project-relative path of the newest report, or null.</summary>
        string LatestReportAssetPath { get; }
        void Run();
    }

    /// <summary>Optional: a tool that can show a pass/fail line of its newest run in the hub.</summary>
    public interface IDMAuditResultSummary
    {
        string ResultSummary { get; }
        /// <summary>Number of FAIL results in the newest run, or -1 when unknown.</summary>
        int ResultFailCount { get; }
    }

    /// <summary>Runs every registered tool one after another ("Run all"); the queue survives play-mode domain reloads.</summary>
    [InitializeOnLoad]
    public static class DMAuditRunAll
    {
        private const string Key = "DMAudit.RunAll.Queue";

        static DMAuditRunAll()
        {
            EditorApplication.update += Pump;
        }

        public static bool Active => SessionState.GetString(Key, "").Length > 0;

        public static void Start()
        {
            SessionState.SetString(Key, string.Join(",", DMAuditRegistry.Tools.Select(t => t.Id)));
        }

        public static void Cancel()
        {
            SessionState.EraseString(Key);
        }

        private static void Pump()
        {
            string q = SessionState.GetString(Key, "");
            if (q.Length == 0 || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            foreach (IDMAuditTool t in DMAuditRegistry.Tools)
                if (t.IsRunning)
                    return;
            string[] ids = q.Split(',');
            SessionState.SetString(Key, string.Join(",", ids.Skip(1)));
            DMAuditRegistry.Run(ids[0]);
        }
    }

    public static class DMAuditRegistry
    {
        private static List<IDMAuditTool> _tools;

        public static IReadOnlyList<IDMAuditTool> Tools
        {
            get
            {
                if (_tools == null)
                    Rebuild();
                return _tools;
            }
        }

        public static void Rebuild()
        {
            _tools = new List<IDMAuditTool>();
            foreach (Type t in TypeCache.GetTypesDerivedFrom<IDMAuditTool>())
            {
                if (t.IsAbstract || t.IsInterface || t.GetConstructor(Type.EmptyTypes) == null)
                    continue;
                try { _tools.Add((IDMAuditTool)Activator.CreateInstance(t)); }
                catch (Exception e) { Debug.LogWarning($"DMAudit: could not create tool {t.Name}: {e.Message}"); }
            }

            _tools.Sort((a, b) =>
            {
                int c = string.CompareOrdinal(a.Category, b.Category);
                return c != 0 ? c : string.CompareOrdinal(a.Title, b.Title);
            });
        }

        public static IDMAuditTool Find(string id)
        {
            foreach (IDMAuditTool t in Tools)
                if (string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase))
                    return t;
            return null;
        }

        /// <summary>Headless entry point (MCP execute_code / dm.py): runs the tool with the given id.</summary>
        public static string Run(string id)
        {
            IDMAuditTool tool = Find(id);
            if (tool == null)
                return "unknown audit tool: " + id;
            tool.Run();
            return "started " + tool.Id;
        }
    }

    public sealed class DMAuditHubWindow : EditorWindow
    {
        private Vector2 _scroll;

        [MenuItem("Tools/Dark Matter Genesis/Audit/Audit Hub", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Audit_Audit_Hub)]
        public static void Open()
        {
            DMAuditHubWindow w = GetWindow<DMAuditHubWindow>("DMG Audit Hub");
            w.minSize = new Vector2(520f, 320f);
            w.Show();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Dark Matter Genesis - Audit Hub", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Audits registered here run from this window, from Tools > Dark Matter Genesis > Audit, or headlessly " +
                "(DMAuditRegistry.Run(id)). To add an audit: implement IDMAuditTool in an Editor folder; it appears automatically.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            bool anyRunning = DMAuditRegistry.Tools.Any(t => t.IsRunning);
            using (new EditorGUI.DisabledScope(anyRunning || DMAuditRunAll.Active))
            {
                if (GUILayout.Button("Run all audits", GUILayout.Width(140f)))
                    DMAuditRunAll.Start();
            }

            if (DMAuditRunAll.Active && GUILayout.Button("Cancel queue", GUILayout.Width(110f)))
                DMAuditRunAll.Cancel();
            bool rec = GUILayout.Toggle(DMAuditInputRecorder.Enabled, new GUIContent("Record my inputs", "While you play in the editor (no audit running), log key/mouse/pad events and error snapshots to Documentation/Audits/Recordings."), GUILayout.Width(130f));
            if (rec != DMAuditInputRecorder.Enabled) DMAuditInputRecorder.Enabled = rec;
            bool quickMode = GUILayout.Toggle(EditorPrefs.GetString("DMAudit.Mode", "full") == "quick", new GUIContent("Quick mode", "Quick: skip units (enemies, weapons, scenarios) unchanged since they last passed, and only sample ammo types with special FX. Full: run everything."), GUILayout.Width(100f));
            EditorPrefs.SetString("DMAudit.Mode", quickMode ? "quick" : "full");
            bool stepHud = GUILayout.Toggle(DMAuditHud.Enabled, new GUIContent("Step label", "Show the current audit unit / scenario + step in the Game view and log a timestamped step line during a run (default on)."), GUILayout.Width(90f));
            if (stepHud != DMAuditHud.Enabled) DMAuditHud.Enabled = stepHud;
            EditorGUILayout.EndHorizontal();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            string lastCategory = null;
            foreach (IDMAuditTool tool in DMAuditRegistry.Tools)
            {
                if (tool.Category != lastCategory)
                {
                    lastCategory = tool.Category;
                    EditorGUILayout.Space(6f);
                    EditorGUILayout.LabelField(lastCategory, EditorStyles.miniBoldLabel);
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(tool.Title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(tool.Description, EditorStyles.wordWrappedMiniLabel);
                if (!string.IsNullOrEmpty(tool.Status))
                    EditorGUILayout.LabelField("Status: " + tool.Status, EditorStyles.miniLabel);
                if (tool is IDMAuditResultSummary rs && !string.IsNullOrEmpty(rs.ResultSummary))
                {
                    Color old = GUI.contentColor;
                    GUI.contentColor = rs.ResultFailCount > 0 ? new Color(1f, 0.45f, 0.4f) : new Color(0.5f, 0.9f, 0.5f);
                    EditorGUILayout.LabelField((rs.ResultFailCount > 0 ? "FAIL - " : "PASS - ") + rs.ResultSummary, EditorStyles.wordWrappedMiniLabel);
                    GUI.contentColor = old;
                }
                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(tool.IsRunning))
                {
                    if (GUILayout.Button("Run", GUILayout.Width(80f)))
                        tool.Run();
                }

                string report = tool.LatestReportAssetPath;
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(report)))
                {
                    if (GUILayout.Button("Open latest report", GUILayout.Width(130f)) && !string.IsNullOrEmpty(report))
                        EditorUtility.RevealInFinder(Path.GetFullPath(report));
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            if (DMAuditRegistry.Tools.Count == 0)
                EditorGUILayout.LabelField("No audit tools registered.");
            EditorGUILayout.EndScrollView();
        }
    }

    public static class DMAuditMenu
    {
        [MenuItem("Tools/Dark Matter Genesis/Audit/Run Enemy Runtime Audit", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Audit_Run_Enemy_Runtime_Audit)]
        public static void RunEnemyRuntimeAudit()
        {
            DMAuditRegistry.Run("enemy-runtime");
        }

        [MenuItem("Tools/Dark Matter Genesis/Audit/Run All Audits", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Audit_Run_Enemy_Runtime_Audit + 1)]
        public static void RunAllAudits()
        {
            DMAuditRunAll.Start();
        }

        [MenuItem("Tools/Dark Matter Genesis/Audit/Run Enemy Runtime Audit (update baseline)", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Audit_Run_Enemy_Runtime_Audit + 2)]
        public static void RunEnemyRuntimeAuditUpdateBaseline()
        {
            Project.EditorTools.Audit.DMEnemyRuntimeAudit.Launch("{\"updateBaseline\":true}");
        }

        [MenuItem("Tools/Dark Matter Genesis/Audit/Run Enemy Runtime Audit (Quick, skip unchanged)")]
        public static void RunEnemyRuntimeAuditQuick() { Project.EditorTools.Audit.DMEnemyRuntimeAudit.Launch("{\"mode\":\"quick\"}"); }

        [MenuItem("Tools/Dark Matter Genesis/Audit/Run Enemy Runtime Audit (Full)")]
        public static void RunEnemyRuntimeAuditFull() { Project.EditorTools.Audit.DMEnemyRuntimeAudit.Launch("{\"mode\":\"full\"}"); }

        [MenuItem("Tools/Dark Matter Genesis/Audit/Open Latest Enemy Audit Report", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Audit_Open_Latest_Enemy_Audit_Report)]
        public static void OpenLatestEnemyAuditReport()
        {
            IDMAuditTool tool = DMAuditRegistry.Find("enemy-runtime");
            string path = tool != null ? tool.LatestReportAssetPath : null;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Debug.Log("DMAudit: no enemy audit report yet - run Tools > Dark Matter Genesis > Audit > Run Enemy Runtime Audit.");
                return;
            }

            EditorUtility.RevealInFinder(Path.GetFullPath(path));
        }
    }
}
#endif
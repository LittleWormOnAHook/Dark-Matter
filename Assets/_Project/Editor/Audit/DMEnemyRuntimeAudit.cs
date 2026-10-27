#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Invector;
using Invector.vCharacterController;
using Invector.vMelee;
using Invector.vShooter;
using Project.AI;
using Project.AI.Invector;
using Project.Combat;
using Project.Core;
using Project.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Audit
{
    /// <summary>Hub registration for the enemy runtime audit.</summary>
    public sealed class DMEnemyRuntimeAuditTool : IDMAuditTool, IDMAuditResultSummary
    {
        public string Id => "enemy-runtime";
        public string Title => "Enemy Runtime Audit";
        public string Category => "Combat";
        public string Description =>
            "Plays the main scene, spawns every enemy prefab alone and measures: weapons visible, unaware behaviour (no circling), " +
            "notice/chase/attack timing, melee + ranged damage to the player, player damage to the enemy, hit marks, death/loot/cleanup, " +
            "animation clip timings and console errors. Writes a markdown report. Never saves the scene.";
        public bool IsRunning => DMEnemyRuntimeAudit.IsActive;
        public string Status => DMEnemyRuntimeAudit.StatusLine;
        public string LatestReportAssetPath => File.Exists(DMEnemyAuditReport.LatestMd) ? DMEnemyAuditReport.LatestMd : null;
        public void Run() { DMEnemyRuntimeAudit.Launch(null); }
        public string ResultSummary { get { int f; return DMEnemyAuditReport.SummaryLine(out f); } }
        public int ResultFailCount { get { int f; DMEnemyAuditReport.SummaryLine(out f); return f; } }
    }

    [Serializable]
    internal sealed class AuditOptions
    {
        public string filter = "";       // comma separated prefab-name substrings; empty = all
        public bool realSwing = true;    // inject Mouse input for the player melee probe
        public bool includeTemplate = true;
        public bool weaponMatrix = true; // equip every weapon item on the template and test damage
        public bool scenarios = true;    // block/parry, movement, multi-enemy, player weapons, performance
        public bool updateBaseline = false; // overwrite the regression baseline with this run
        public string mode = "full";    // full: run every unit, quick: skip units unchanged since they last passed, fewer ammo-specific cases
        public bool force = false;       // ignore the unit store
        public string mix = "";            // interruptsOnly: comma separated prefab names used for the 2nd, 3rd... enemy instead of copies (e.g. "Humadroid,Robot"); the rest are copies of the filtered enemy
        public int enemyCount = 1;          // interruptsOnly: how many copies of the enemy to fight at once (1-5), for token rotation / hand-off / spacing checks
        public string sceneEnemies = "";   // interruptsOnly: comma separated names of pre-placed (disabled) scene enemies to use instead of spawned prefab copies, optional "@x;z" picks the one nearest that XZ (e.g. "SwordMaster,Humadroid@-780;-75"). First = primary. Scene is never saved: play-mode changes revert on exit.
        public bool interruptsOnly = false; // run only the Slice 0b attack-interrupt / cadence scenario on the filtered enemies
        public bool obstaclesOnly = false; // targeted wall/corner movement audit, bypass broad combat/weapon suite
        public bool thinWallsOnly = false; // endpoint-focused variant of obstaclesOnly
        public bool rockMeshesOnly = false; // real scene-rock MeshCollider movement cases
        public bool keepTerrain = false; // 2026-10-25s: leave Gaia terrain streaming ON (slicesOnly implies it)
        public string sliceFilter = ""; // 2026-10-26b: "humaMelee" = Humadroid melee-lock damage + spin only
        public bool slicesOnly = false; // 2026-10-25s: Combat Slice 4 / Phase 4 director / Slice 5 momentum+spin targeted audit
        public bool useActiveScene = false; // 2026-10-25s: audit the open scene instead of opening MainScene
    }

    /// <summary>
    /// Play-mode test harness + driver. Editor-only: stepped from EditorApplication.update, so nothing is added to
    /// the game assemblies. Survives the play-mode domain reload through SessionState.
    /// </summary>
    [InitializeOnLoad]
    public static partial class DMEnemyRuntimeAudit
    {
        private const string KeyPending = "DMAudit.Enemy.Pending";
        private const string KeyOptions = "DMAudit.Enemy.Options";
        private const string KeyStatus = "DMAudit.Enemy.Status";
        private const string MainScene = "Assets/_Project/Scenes/Dark Matter Genesis v1.6.6.unity";
        private const string EnemiesFolder = "Assets/_Project/Prefabs";
        private const BindingFlags BF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private static bool _running;
        private static AuditOptions _opts;
        private static DMAuditRun _run;
        private static readonly Stack<IEnumerator> Stack = new Stack<IEnumerator>();
        private static IWait _wait;
        private static DMAuditEnemyResult _cur;
        private static Live _live;
        private static double _realStart;
        private static double _lastProgressReal;
        private static float _lastGameTime;
        private static GameObject _player;
        private static Vector3 _origin;
        private static readonly List<string> SceneDisabled = new List<string>();
        private static bool _bypassDone;
        private static string _bypassFail = "";

        public static bool IsActive => _running || SessionState.GetBool(KeyPending, false);
        public static string StatusLine => SessionState.GetString(KeyStatus, "idle");

        static DMEnemyRuntimeAudit()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.update += Tick;
            if (SessionState.GetBool(KeyPending, false) && EditorApplication.isPlaying && !_running)
                EditorApplication.delayCall += TryStartInPlay;
        }

        // =============================================================== launch / lifecycle
        public static void Launch(string optionsJson)
        {
            if (_running)
            {
                Debug.Log("DMAudit: enemy audit already running.");
                return;
            }

            var opts = new AuditOptions();
            opts.mode = EditorPrefs.GetString("DMAudit.Mode", "full");
            if (!string.IsNullOrEmpty(optionsJson))
                JsonUtility.FromJsonOverwrite(optionsJson, opts);
            SessionState.SetString(KeyOptions, JsonUtility.ToJson(opts));

            if (!opts.useActiveScene) EnsureMainScene();
            SessionState.SetBool(KeyPending, true);
            SetStatus("launching");
            if (EditorApplication.isPlaying)
                EditorApplication.isPlaying = false; // restarts via EnteredEditMode
            else
                EditorApplication.isPlaying = true;
        }

        private static void EnsureMainScene()
        {
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (active.path == MainScene)
                return;
            if (!EditorApplication.isPlaying && !active.isDirty)
                EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
            else
                Debug.LogWarning("DMAudit: active scene is '" + active.path + "', not the main scene; auditing it anyway (the scene is never saved).");
        }

        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode && !_running) AuditSettings.Restore(null); // safety net after a crash/domain reload
            if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(KeyPending, false) && !_running)
            {
                EditorApplication.delayCall += () => { if (SessionState.GetBool(KeyPending, false)) EditorApplication.isPlaying = true; };
            }
            else if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(KeyPending, false) && !_running)
            {
                EditorApplication.delayCall += TryStartInPlay;
            }
            else if (change == PlayModeStateChange.ExitingPlayMode && _running)
            {
                Finish("aborted", "play mode exited before the run completed");
            }
        }

        private static void TryStartInPlay()
        {
            if (_running || !EditorApplication.isPlaying || !SessionState.GetBool(KeyPending, false))
                return;
            SessionState.SetBool(KeyPending, false);
            _opts = new AuditOptions();
            JsonUtility.FromJsonOverwrite(SessionState.GetString(KeyOptions, "{}"), _opts);
            _run = new DMAuditRun
            {
                startedLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                unityVersion = Application.unityVersion,
                state = "running",
                mode = _opts.mode
            };
            _unitsRan = _unitsCached = 0;
            AuditTiming.Init();
            Stack.Clear();
            _wait = null;
            _bypassDone = false;
            _menuReBypassCount = 0;
            _running = true;
            _realStart = EditorApplication.timeSinceStartup;
            _lastProgressReal = _realStart;
            Application.logMessageReceived += OnLog;
            AuditSettings.Begin();
            DMAuditHud.Begin();
            Stack.Push(Main());
            SetStatus("running");
        }

        private static void SetStatus(string s)
        {
            SessionState.SetString(KeyStatus, s);
        }

        private static void Finish(string state, string reason)
        {
            if (!_running)
                return;
            _running = false;
            Application.logMessageReceived -= OnLog;
            try { ReleaseAuditPlayerGuard(); } catch { /* best effort */ }
            try { var cp = Project.Combat.DM_CombatCoreProfile.Live; if (cp != null && cp.auditAllowPlayerDeath) { cp.auditAllowPlayerDeath = false; EditorUtility.SetDirty(cp); } } catch { /* 2026-10-19 h: audit-only option reset */ }
            try { Cleanup(); } catch { /* best effort */ }
            try { RestoreAuditState(); } catch { /* best effort */ }
            try { AuditTiming.End(); BuildFixRegression(); } catch (Exception ex) { Debug.LogWarning("DMAudit: regression summary failed: " + ex.Message); }
            _run.wallSeconds = (float)(EditorApplication.timeSinceStartup - _realStart);
            _run.timingNote = AuditTiming.Note;
            _run.environment.Add($"Mode {_run.mode}: {_unitsRan} unit(s) ran, {_unitsCached} reused from the unit store (unchanged and previously passing)");
            _run.environment.Add("Excluded characters (broken, not used): " + string.Join(", ", ExcludedEnemies));
            if (state == "complete") SaveStore();
            AuditTiming.Dispose();
            try { DMAuditHud.End(); } catch { /* best effort */ }
            try { AuditSettings.Restore(_run); } catch (Exception ex) { Debug.LogWarning("DMAudit: settings restore failed: " + ex.Message); }
            _run.state = state;
            _run.abortReason = reason;
            _run.finishedLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            string path = null;
            try { path = DMEnemyAuditReport.Write(_run, _opts != null && _opts.updateBaseline); }
            catch (Exception e) { Debug.LogError("DMAudit: failed to write report: " + e); }
            int f = 0, w = 0;
            foreach (var e in _run.enemies) { f += e.Count("FAIL"); w += e.Count("WARN"); }
            int wfail = 0;
            foreach (var e in _run.weapons) wfail += e.Count("FAIL");
            string summary = $"DMAudit enemy runtime audit {state}: {_run.enemies.Count} enemies, {f} FAIL, {w} warn; weapon matrix {_run.weapons.Count} cases, {wfail} FAIL; scenarios {_run.scenarios.Count}; overall {_run.CountAll("PASS")} PASS / {_run.CountAll("FAIL")} FAIL / {_run.CountAll("WARN")} warn, {_run.baselineChanges.Count} change(s) vs baseline; mode {_run.mode}, wall {_run.wallSeconds:0}s ({_unitsRan} ran / {_unitsCached} cached). Report: {path}";
            SetStatus(summary);
            Debug.Log(summary);
            foreach (var e in _run.scenarios)
                Debug.Log($"DMAudit | scenario {e.name,-60} PASS={e.Count("PASS")} FAIL={e.Count("FAIL")} warn={e.Count("WARN")}");
            foreach (var e in _run.weapons)
                Debug.Log($"DMAudit | weapon {e.name,-26} equip={e.Status("w.equip"),-4} damage={e.Status("w.damage"),-4} vis={e.Status("vis"),-4}");
            foreach (var e in _run.enemies)
                Debug.Log($"DMAudit | {e.name,-34} melee={e.Status("melee.damage"),-4} ranged={e.Status("ranged.damage"),-4} vis={e.Status("vis"),-4} unaware={e.Status("unaware"),-4} notice={e.Status("notice"),-4} player->enemy={e.Status("player.dmg"),-4} marks={e.Status("hitmarks"),-4} death={e.Status("death"),-4} console={e.Status("console")}");
            Stack.Clear();
            _wait = null;
            _live = null;
            if (EditorApplication.isPlaying)
                EditorApplication.isPlaying = false;
        }

        // =============================================================== coroutine pump
        private interface IWait { bool Done(); }

        private sealed class WaitGame : IWait
        {
            private readonly float _until;
            public WaitGame(float seconds) { _until = Time.time + seconds; }
            public bool Done() { return Time.time >= _until; }
        }

        /// <summary>Real-time wait: works while Time.timeScale is 0 (title menu / loading screen).</summary>
        private sealed class WaitReal : IWait
        {
            private readonly float _until;
            public WaitReal(float seconds) { _until = Time.realtimeSinceStartup + seconds; }
            public bool Done() { return Time.realtimeSinceStartup >= _until; }
        }

        private sealed class WaitUntil : IWait
        {
            private readonly Func<bool> _cond;
            private readonly float _until;
            public WaitUntil(Func<bool> cond, float timeout) { _cond = cond; _until = Time.time + timeout; }
            public bool Done() { return Time.time >= _until || _cond(); }
        }

        private static void Tick()
        {
            if (!_running)
                return;
            if (!EditorApplication.isPlaying)
            {
                Finish("aborted", "left play mode");
                return;
            }

            if (_bypassDone && _player != null) { GuardFloor(); AuditPlayerGuard(); }
            if (_bypassDone) MenuBypassWatchdog(); // 2026-10-19 e: the main menu / title pause must not come back mid-run
            double now = EditorApplication.timeSinceStartup;
            if (!_bypassDone) _lastProgressReal = now; // watchdog only after the menu bypass succeeded
            if (Time.time > _lastGameTime + 0.001f) { _lastGameTime = Time.time; _lastProgressReal = now; }
            if (now - _lastProgressReal > 45.0)
            {
                Finish("aborted", $"game time stopped advancing for 45s (timeScale={Time.timeScale}, frozen={GameplayWorldSimulation.IsFrozen}); editor probably lost focus or a menu froze the world");
                return;
            }

            if (now - _realStart > 90 * 60)
            {
                Finish("aborted", "90 minute watchdog");
                return;
            }

            AuditTiming.Frame();
            try { WeaponGuardTick(); } catch (Exception e) { LogProbeError("guard", e); }
            try { AudioProbe.Tick(_live); } catch (Exception e) { LogProbeError("audio", e); }
            try { if (_live != null) _live.Sample(); }
            catch (Exception e) { LogProbeError("sample", e); }

            if (Stack.Count == 0)
                return;
            if (_wait != null)
            {
                bool done;
                try { done = _wait.Done(); } catch { done = true; }
                if (!done)
                    return;
                _wait = null;
            }

            IEnumerator top = Stack.Peek();
            bool more;
            try { more = top.MoveNext(); }
            catch (Exception ex)
            {
                string msg = "audit step threw: " + ex.GetType().Name + ": " + ex.Message;
                if (_cur != null)
                    _cur.Add("Harness", "harness.exception", "Harness exception", "WARN", msg, ex.StackTrace != null ? ex.StackTrace.Split('\n')[0].Trim() : "");
                Debug.LogWarning("DMAudit: " + msg + "\n" + ex.StackTrace);
                // Unwind to the Main routine so the next enemy still runs.
                while (Stack.Count > 1)
                    Stack.Pop();
                _wait = null;
                return;
            }

            if (!more)
            {
                Stack.Pop();
                if (Stack.Count == 0)
                    Finish("complete", null);
                return;
            }

            object y = top.Current;
            if (y is IEnumerator sub) Stack.Push(sub);
            else if (y is IWait w) _wait = w;
        }

        private static void LogProbeError(string what, Exception e)
        {
            if (_cur != null && _cur.checks.Count(c => c.id == "harness.probe." + what) == 0)
                _cur.Add("Harness", "harness.probe." + what, "Harness probe '" + what + "' threw", "WARN", e.GetType().Name + ": " + e.Message, "");
        }

        private static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (_cur == null || condition == null)
                return;
            if (condition.StartsWith("DMAudit", StringComparison.Ordinal))
                return; // our own summary lines
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _cur.consoleErrors++;
            else if (type == LogType.Warning)
                _cur.consoleWarnings++;
            else
                return;
            if (_cur.consoleLines.Count < 30)
            {
                string where = "";
                if (!string.IsNullOrEmpty(stackTrace) && type != LogType.Warning)
                {
                    string[] sl = stackTrace.Split('\n');
                    where = " @ " + string.Join(" <- ", sl.Where(x => x.Trim().Length > 0).Take(3).Select(x => x.Trim()));
                }

                string msg = condition + where;
                _cur.consoleLines.Add($"[{type}] " + (msg.Length > 420 ? msg.Substring(0, 420) : msg));
            }
        }
    }
}
#endif

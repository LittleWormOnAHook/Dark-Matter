#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Project.AI;
using Project.Data;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Audit
{
    public static partial class DMEnemyRuntimeAudit
    {
        // =============================================================== exclusions
        /// <summary>Characters that are broken and deliberately not used: cut from every list, scenario, baseline and report.</summary>
        private static readonly string[] ExcludedEnemies = { "Lisa_Hybrid" };
        private static bool IsExcludedEnemy(string name) { return ExcludedEnemies.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase)); }

        // =============================================================== continuous top-up (virtual hp)
        private static bool _observeDeath;     // true only while a death is being observed
        private static bool _enemyTopUp = true; // tweak tests that need low-hp behaviour can switch it off

        /// <summary>Enemy hp including everything the harness refilled: drops monotonically with damage dealt.</summary>
        private static float EHp(Live L) { return L.health.CurrentHealth - L.refilled; }

        private static void EnemyTopUpNow(Live L)
        {
            if (L == null || L.health == null || L.health.IsDead) return;
            float max = L.health.MaxHealth, cur = L.health.CurrentHealth;
            if (cur >= max - 0.01f) return;
            L.refilled += max - cur;
            RefillEnemyHp(L.health);
        }

        private static void EnemyTopUpTick(Live L)
        {
            if (!_enemyTopUp || _observeDeath || L == null || L.health == null || L.health.IsDead) return;
            if (L.health.CurrentHealth < L.health.MaxHealth * 0.97f) EnemyTopUpNow(L); // continuous: keep the enemy effectively full so a run never ends on a death
        }

        // =============================================================== waits that end on an event
        /// <summary>Waits at least `min` seconds, then until cond() or `max` seconds in total.</summary>
        private static IEnumerator WaitMin(float min, Func<bool> cond, float max)
        {
            yield return new WaitGame(min);
            if (cond != null && !cond()) yield return new WaitUntil(cond, Mathf.Max(0.05f, max - min));
        }

        private static bool PlayerAttacking() { return PlayerFlag("isAttacking"); }

        /// <summary>Spawn-ready: the enemy has had its Start/bootstrap frames and its animator is running.</summary>
        private static IEnumerator SpawnReady(Live L)
        {
            float t0 = Time.time;
            yield return new WaitUntil(() => Time.time - t0 >= 0.9f && L.go != null && L.anim != null && L.anim.isInitialized && L.health != null, 3f);
        }

        // =============================================================== editor/project settings guard
        /// <summary>Saves every setting the harness touches (and watches the ones it must not), restores them on exit, reports the differences.</summary>
        private static class AuditSettings
        {
            private const string Key = "DMAudit.Settings.RunInBackground";
            private static bool _active;
            private static bool _origRunInBg;
            private static StackTraceLogType _origLog, _origWarn;
            private static string _before = "", _scene0 = "";
            private const string KeyBg = "DMAudit.Settings.InputBackground", KeyEd = "DMAudit.Settings.InputEditor";
            private static string _inputNote = "";

            /// <summary>Injected keyboard/mouse events are dropped when the Unity window or the Game view is not focused (Input System defaults).
            /// For the run only: let all device input go to the Game view regardless of focus, and focus the Game view. Restored at exit.</summary>
            private static void InputBegin()
            {
                try
                {
                    var st = UnityEngine.InputSystem.InputSystem.settings;
                    SessionState.SetInt(KeyBg, (int)st.backgroundBehavior);
                    SessionState.SetInt(KeyEd, (int)st.editorInputBehaviorInPlayMode);
                    _inputNote = $"InputSystem backgroundBehavior {st.backgroundBehavior} -> IgnoreFocus, editorInputBehaviorInPlayMode {st.editorInputBehaviorInPlayMode} -> AllDeviceInputAlwaysGoesToGameView (so injected keys/mouse work even when the editor is not the foreground window; Application.isFocused was {Application.isFocused})";
                    st.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
                    st.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    Type gv = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
                    if (gv != null) EditorWindow.FocusWindowIfItsOpen(gv);
                }
                catch (Exception e) { _inputNote = "could not adjust InputSystem focus settings: " + e.Message; }
            }

            private static void InputRestore()
            {
                try
                {
                    int bg = SessionState.GetInt(KeyBg, -1), ed = SessionState.GetInt(KeyEd, -1);
                    var st = UnityEngine.InputSystem.InputSystem.settings;
                    if (bg >= 0) { st.backgroundBehavior = (UnityEngine.InputSystem.InputSettings.BackgroundBehavior)bg; SessionState.EraseInt(KeyBg); }
                    if (ed >= 0) { st.editorInputBehaviorInPlayMode = (UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode)ed; SessionState.EraseInt(KeyEd); }
                }
                catch { /* ignore */ }
            }

            private static string _unlockNote = "";
            private static readonly System.Collections.Generic.Dictionary<string, int> _unlockPrev = new System.Collections.Generic.Dictionary<string, int>();
            private static System.Collections.Generic.Dictionary<string, int> _unlockRanks;
            private static readonly string[] UnlockIds = { "skill_system_jetpack", "skill_system_dash", "skill_system_climb" };

            /// <summary>Jetpack, dash and climbing are gated by Journal Tier-0 skills (DMSkillMovementSystemGates -> PlayerSkillAllocator.GetSkillRank > 0).
            /// For the run only: write rank 1 for those three ids into the in-memory PlayerProgressionManager.skillRanks dictionary (no TrySpendSkillPoint, no skill points spent,
            /// no save call, no asset touched), remember the previous values and put them back at exit. Play-mode memory only, so nothing can reach disk.</summary>
            private static void UnlockBegin()
            {
                _unlockPrev.Clear(); _unlockRanks = null; _unlockNote = "";
                try
                {
                    Type pm = null;
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) { pm = asm.GetType("Project.Progression.PlayerProgressionManager"); if (pm != null) break; }
                    if (pm == null) { _unlockNote = "movement unlock skipped: PlayerProgressionManager type not found"; return; }
                    var ensure = pm.GetMethod("EnsureExists", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    object inst = ensure != null ? ensure.Invoke(null, null) : null;
                    var f = pm.GetField("skillRanks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (inst == null || f == null) { _unlockNote = "movement unlock skipped: progression instance or skillRanks field not found"; return; }
                    _unlockRanks = f.GetValue(inst) as System.Collections.Generic.Dictionary<string, int>;
                    if (_unlockRanks == null) { _unlockNote = "movement unlock skipped: skillRanks is not a Dictionary<string,int>"; return; }
                    var sb = new System.Text.StringBuilder();
                    foreach (string id in UnlockIds)
                    {
                        int prev; bool had = _unlockRanks.TryGetValue(id, out prev);
                        _unlockPrev[id] = had ? prev : -1;
                        if (!had || prev < 1) _unlockRanks[id] = 1;
                        sb.Append(id + " " + (had ? prev.ToString() : "absent") + "->" + _unlockRanks[id] + "; ");
                    }
                    _unlockNote = "Jetpack/dash/climb unlocked for the run by writing rank 1 into the in-memory skill ranks (" + sb + "restored at exit; no skill points spent, no save, no asset written)";
                }
                catch (Exception e) { _unlockNote = "movement unlock failed: " + e.Message; }
            }

            private static void UnlockRestore()
            {
                try
                {
                    if (_unlockRanks != null)
                        foreach (var kv in _unlockPrev) { if (kv.Value < 0) _unlockRanks.Remove(kv.Key); else _unlockRanks[kv.Key] = kv.Value; }
                }
                catch { /* ignore */ }
                _unlockPrev.Clear(); _unlockRanks = null;
            }
            private static string Snapshot()
            {
                string rp = QualitySettings.renderPipeline != null ? QualitySettings.renderPipeline.name : "(quality: none)";
                string grp = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null ? UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.name : "(none)";
                return $"runInBackground={Application.runInBackground}; vSyncCount={QualitySettings.vSyncCount}; lodBias={QualitySettings.lodBias:0.##}; targetFrameRate={Application.targetFrameRate}; qualityLevel={QualitySettings.GetQualityLevel()} ({QualitySettings.names[Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, QualitySettings.names.Length - 1)]}); qualityRenderPipeline={rp}; graphicsDefaultRenderPipeline={grp}; fixedDeltaTime={Time.fixedDeltaTime:0.####}; maximumDeltaTime={Time.maximumDeltaTime:0.###}";
            }

            private static string SceneStamp()
            {
                try { var fi = new FileInfo(MainScene); return fi.Exists ? fi.LastWriteTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "/" + fi.Length : "missing"; } catch { return "?"; }
            }

            public static void Begin()
            {
                _before = Snapshot();
                _scene0 = SceneStamp();
                _origRunInBg = Application.runInBackground;
                _origLog = Application.GetStackTraceLogType(LogType.Log);
                _origWarn = Application.GetStackTraceLogType(LogType.Warning);
                SessionState.SetInt(Key, _origRunInBg ? 1 : 0);
                Application.runInBackground = true;                              // needed so the run continues when the editor loses focus
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);      // no stack capture on every Debug.Log during the run
                Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
                InputBegin();
                UnlockBegin();
                _active = true;
            }

            /// <summary>Restores what Begin changed. Also safe after a domain reload (uses SessionState).</summary>
            public static void Restore(DMAuditRun run)
            {
                bool wasActive = _active;
                if (wasActive)
                {
                    Application.SetStackTraceLogType(LogType.Log, _origLog);
                    Application.SetStackTraceLogType(LogType.Warning, _origWarn);
                }

                InputRestore();
                UnlockRestore();
                int s = SessionState.GetInt(Key, -1);
                if (s >= 0) { Application.runInBackground = s == 1; SessionState.EraseInt(Key); }
                if (!wasActive || run == null) { _active = false; return; }
                _active = false;
                string after = Snapshot(), scene1 = SceneStamp();
                run.environment.Add($"Settings the audit changed (all restored at exit): Application.runInBackground {_origRunInBg} -> true -> {Application.runInBackground}; stack traces for Log and Warning set to None during the run (were {_origLog}/{_origWarn}, restored). " + (_inputNote.Length > 0 ? "Also (restored): " + _inputNote + ". " : "") + (_unlockNote.Length > 0 ? _unlockNote + ". " : "") + $"Nothing else is touched: no scene save, no ProjectSettings/QualitySettings/GraphicsSettings write, no timeScale change.");
                run.environment.Add("Settings at start: " + _before);
                if (after == _before) run.environment.Add("Settings at end: identical to the start (no drift during the run)");
                else run.environment.Add("SETTINGS DRIFT during the run (changed by something other than the audit, e.g. game code or another tool): start [" + _before + "] -> end [" + after + "]");
                run.environment.Add($"Main scene file {MainScene}: {(scene1 == _scene0 ? "not written during the run" : "WRITTEN during the run (" + _scene0 + " -> " + scene1 + ") - the audit never saves scenes, so another tool did")} (the audit has no SaveScene/SaveOpenScenes call)");
            }
        }

        // =============================================================== timing
        private static class AuditTiming
        {
            private static string _unit = "", _phase = "";
            private static float _pStart;
            private static int _fr;
            private static double _sum, _max, _phys, _rend, _scr;
            private static int _lastFrame = -1;
            private static float _lastFixed = -1f;
            private static ProfilerRecorder _rp, _rr, _rs1, _rs2;
            private static bool _init;
            public static string Note = "";

            private static ProfilerRecorder Start(ProfilerCategory cat, string name, List<string> ok, List<string> bad)
            {
                ProfilerRecorder r = default;
                try { r = ProfilerRecorder.StartNew(cat, name, 1); } catch { }
                if (r.Valid) ok.Add(name); else bad.Add(name);
                return r;
            }

            public static void Init()
            {
                if (_init) return;
                _init = true;
                var ok = new List<string>(); var bad = new List<string>();
                _rp = Start(ProfilerCategory.Internal, "FixedUpdate.PhysicsFixedUpdate", ok, bad);
                _rr = Start(ProfilerCategory.Render, "Camera.Render", ok, bad);
                _rs1 = Start(ProfilerCategory.Internal, "Update.ScriptRunBehaviourUpdate", ok, bad);
                _rs2 = Start(ProfilerCategory.Internal, "PreLateUpdate.ScriptRunBehaviourLateUpdate", ok, bad);
                Note = "frame split from ProfilerRecorder markers (approximate, editor): " + (ok.Count > 0 ? string.Join(", ", ok) : "none available") + (bad.Count > 0 ? "; not available: " + string.Join(", ", bad) : "");
            }

            public static void Dispose()
            {
                try { if (_rp.Valid) _rp.Dispose(); if (_rr.Valid) _rr.Dispose(); if (_rs1.Valid) _rs1.Dispose(); if (_rs2.Valid) _rs2.Dispose(); } catch { }
                _init = false;
            }

            public static void Frame()
            {
                int f = Time.frameCount;
                if (f == _lastFrame) return;
                _lastFrame = f;
                double dt = Time.unscaledDeltaTime;
                _fr++; _sum += dt; if (dt > _max) _max = dt;
                if (_rp.Valid && !Mathf.Approximately(Time.fixedTime, _lastFixed)) _phys += _rp.LastValue;
                _lastFixed = Time.fixedTime;
                if (_rr.Valid) _rend += _rr.LastValue;
                if (_rs1.Valid) _scr += _rs1.LastValue;
                if (_rs2.Valid) _scr += _rs2.LastValue;
            }

            private static void Flush()
            {
                if (_run == null || string.IsNullOrEmpty(_unit) || string.IsNullOrEmpty(_phase)) { Reset(); return; }
                float sec = Time.realtimeSinceStartup - _pStart;
                if (sec >= 0.05f)
                {
                    int n = Math.Max(1, _fr);
                    _run.timings.Add(new DMAuditTimingRow
                    {
                        unit = _unit, phase = _phase, seconds = sec, frames = _fr,
                        avgMs = (float)(_sum / n * 1000.0), maxMs = (float)(_max * 1000.0),
                        physicsMs = (float)(_phys / n / 1e6), renderMs = (float)(_rend / n / 1e6), scriptsMs = (float)(_scr / n / 1e6)
                    });
                }
                Reset();
            }

            private static void Reset() { _fr = 0; _sum = _max = _phys = _rend = _scr = 0; _pStart = Time.realtimeSinceStartup; }

            public static void Begin(string unit) { Flush(); _unit = unit; _phase = "setup"; Reset(); DMAuditHud.Unit(unit, "setup"); }
            public static void Phase(string name) { Flush(); _phase = name; Reset(); DMAuditHud.Step(name); }
            public static void End() { Flush(); _unit = ""; _phase = ""; }
        }

        private static void Ph(string name) { AuditTiming.Phase(name); }

        // =============================================================== skip-unchanged unit store
        [Serializable] private sealed class UnitEntry { public string key; public string hash; public string hashAlt; public string json; public string when; }
        [Serializable] private sealed class UnitStore { public List<UnitEntry> entries = new List<UnitEntry>(); }

        private const string UnitsJson = "Assets/_Project/Documentation/Audits/DM_Enemy_Runtime_Audit_Units.json";
        private static UnitStore _store;
        private static int _unitsRan, _unitsCached;
        private static string _auditStamp, _gameStamp, _gearStamp, _tplStamp, _sceneStamp;
        private static readonly Dictionary<string, string> _assetStampCache = new Dictionary<string, string>();

        private static bool FullMode { get { return _opts == null || !string.Equals(_opts.mode, "quick", StringComparison.OrdinalIgnoreCase); } }

        private static string Md5(string s)
        {
            using (var m = MD5.Create()) return BitConverter.ToString(m.ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-", "");
        }

        private static string FileMd5(string path)
        {
            try { using (var m = MD5.Create()) return BitConverter.ToString(m.ComputeHash(File.ReadAllBytes(path))).Replace("-", ""); }
            catch { return "?"; }
        }

        private static string FolderStamp(string root, string pattern)
        {
            if (!Directory.Exists(root)) return "nofolder";
            var sb = new StringBuilder();
            foreach (string f in Directory.GetFiles(root, pattern, SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal))
                sb.Append(f).Append(FileMd5(f));
            return Md5(sb.ToString());
        }

        private static string AssetStamp(string path)
        {
            if (string.IsNullOrEmpty(path)) return "none";
            if (_assetStampCache.TryGetValue(path, out string hit)) return hit;
            var sb = new StringBuilder();
            foreach (string dep in AssetDatabase.GetDependencies(path, true).OrderBy(x => x, StringComparer.Ordinal))
            {
                if (!dep.StartsWith("Assets/", StringComparison.Ordinal)) continue;
                string ext = Path.GetExtension(dep).ToLowerInvariant();
                if (ext == ".cs") continue; // covered by the game-code stamp
                try
                {
                    var fi = new FileInfo(dep);
                    if (!fi.Exists) { sb.Append(dep).Append("missing"); continue; }
                    bool text = ext == ".prefab" || ext == ".asset" || ext == ".controller" || ext == ".overridecontroller" || ext == ".mat" || ext == ".physicmaterial" || ext == ".mask" || ext == ".unity";
                    if (text && fi.Length < 8 * 1024 * 1024) sb.Append(dep).Append(FileMd5(dep));
                    else sb.Append(dep).Append(fi.Length).Append(fi.LastWriteTimeUtc.Ticks);
                }
                catch { sb.Append(dep).Append("?"); }
            }

            string h = Md5(sb.ToString());
            _assetStampCache[path] = h;
            return h;
        }

        private static void ComputeStamps()
        {
            _assetStampCache.Clear();
            string data = Application.dataPath;
            var sb = new StringBuilder();
            foreach (string f in Directory.GetFiles(Path.Combine(data, "_Project", "Editor", "Audit"), "*.cs"))
            {
                string n = Path.GetFileName(f);
                if (n == "DMAuditHub.cs" || n == "DMAuditInputRecorder.cs") continue; // UI only
                sb.Append(n).Append(FileMd5(f));
            }

            _auditStamp = Md5(sb.ToString());
            _gameStamp = Md5(FolderStamp(Path.Combine(data, "_Project", "Scripts"), "*.cs")
                + FolderStamp(Path.Combine(data, "Invector-3rdPersonController"), "*.cs")
                + FolderStamp(Path.Combine(data, "UI Toolkit", "Runtime"), "*.cs")
                + FolderStamp(Path.Combine(data, "_Project", "Resources"), "*.asset"));
            var gear = new StringBuilder();
            foreach (ItemData it in AllWeaponItems()) gear.Append(AssetStamp(AssetDatabase.GetAssetPath(it)));
            foreach (ItemData it in AllAmmoItems()) gear.Append(AssetStamp(AssetDatabase.GetAssetPath(it)));
            _gearStamp = Md5(gear.ToString());
            _tplStamp = AssetStamp(MatrixPrefab);
            try { var fi = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), MainScene)); _sceneStamp = fi.Exists ? fi.Length + "/" + fi.LastWriteTimeUtc.Ticks : "?"; } catch { _sceneStamp = "?"; }
        }

        private static string UnitHash(string assetPath, string extra, bool needsScene)
        {
            return Md5(_auditStamp + "|" + _gameStamp + "|" + _gearStamp + "|" + AssetStamp(assetPath) + "|" + (needsScene ? _sceneStamp : "") + "|" + extra);
        }

        private static void LoadStore()
        {
            _store = null;
            try { if (File.Exists(UnitsJson)) _store = JsonUtility.FromJson<UnitStore>(File.ReadAllText(UnitsJson)); } catch { _store = null; }
            if (_store == null) _store = new UnitStore();
            _store.entries.RemoveAll(e => e.key != null && ExcludedEnemies.Any(x => e.key.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static void SaveStore()
        {
            try { if (_store != null) File.WriteAllText(UnitsJson, JsonUtility.ToJson(_store, false), new UTF8Encoding(false)); } catch (Exception e) { Debug.LogWarning("DMAudit: could not save the unit store: " + e.Message); }
        }

        /// <summary>Reuses the stored result of an unchanged unit that passed before (never in Full mode, never for a unit that had a FAIL).</summary>
        private static bool TryReuse(string key, string hash, string hashAlt, out DMAuditEnemyResult result)
        {
            result = null;
            if (FullMode || (_opts != null && _opts.force) || _store == null) return false;
            UnitEntry e = _store.entries.FirstOrDefault(x => x.key == key);
            if (e == null || (e.hash != hash && (hashAlt == null || e.hash != hashAlt))) return false;
            try { result = JsonUtility.FromJson<DMAuditEnemyResult>(e.json); } catch { result = null; }
            if (result == null || result.Count("FAIL") > 0) { result = null; return false; }
            result.cached = true;
            result.cachedFrom = e.when;
            _unitsCached++;
            return true;
        }

        private static void RecordUnit(string key, string hash, DMAuditEnemyResult r)
        {
            _unitsRan++;
            if (_store == null || r == null) return;
            _store.entries.RemoveAll(x => x.key == key);
            _store.entries.Add(new UnitEntry { key = key, hash = hash, json = JsonUtility.ToJson(r), when = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) });
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Audit
{
    public static partial class DMEnemyRuntimeAudit
    {
        // =============================================================== setup / cleanup
        private static void Cleanup()
        {
            CleanupObstacleAudit();
            if (_live != null && _live.go != null)
                Object.Destroy(_live.go);
            _live = null;
        }

        private const BindingFlags SBF = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        private static bool LoaderBlocking()
        {
            Type lo = FindType("LoadingOverlayController");
            PropertyInfo p = lo != null ? lo.GetProperty("IsBlockingMenu", SBF) : null;
            return p != null && (bool)p.GetValue(null);
        }

        /// <summary>
        /// Boot -> gameplay without any UI: waits (real time) for the boot loader to hand off, then does what New Expedition's
        /// BeginExpedition does minus the loading overlay and minus the slot-0 autosave: hide menu chrome, MarkStarted, unpause the
        /// world, release input capture, restore the gameplay HUD. Verified afterwards; retried up to 3 times.
        /// </summary>
        private static IEnumerator BypassRoutine()
        {
            var log = new List<string>();
            float until = Time.realtimeSinceStartup + 40f;
            while (LoaderBlocking() && Time.realtimeSinceStartup < until)
                yield return null;
            log.Add(LoaderBlocking() ? "boot loader still blocking after 40s (forced past it)" : "boot loader finished");

            string pre = VerifyGameplayState();
            log.Add("state before bypass: " + (pre.Length == 0 ? "already clean" : pre));
            string problems = "";
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                ApplyBypass(log);
                float t2 = Time.realtimeSinceStartup + 2f;
                while (Time.realtimeSinceStartup < t2)
                    yield return null;
                problems = VerifyGameplayState();
                if (problems.Length == 0)
                {
                    log.Add($"attempt {attempt}: verified clean ({DescribeGameplayState()})");
                    _bypassDone = true;
                    try
                    {
                        string shot = System.IO.Path.GetFullPath("Temp/DMAudit_first_step.png");
                        ScreenCapture.CaptureScreenshot(shot);
                        log.Add("Game view screenshot at first test step: " + shot);
                    }
                    catch (Exception e) { log.Add("screenshot failed: " + e.Message); }
                    break;
                }

                log.Add($"attempt {attempt}: still {problems}");
            }

            _run.environment.Add("Menu/loading bypass: " + string.Join("; ", log));
            if (!_bypassDone)
                _bypassFail = "could not reach clean gameplay: " + problems;
        }

        private static void ApplyBypass(List<string> log)
        {
            try
            {
                Type mmType = FindType("MainMenuController");
                Object menu = mmType != null ? Object.FindAnyObjectByType(mmType, FindObjectsInactive.Include) : null;
                if (menu != null)
                {
                    mmType.GetMethod("HideMenuChrome", BF)?.Invoke(menu, null);
                    PropertyInfo pause = mmType.GetProperty("IsPauseOverlayActive", SBF);
                    if (pause != null && (bool)pause.GetValue(null))
                        mmType.GetMethod("InvokeResumeFromPause", BF)?.Invoke(menu, null);
                }
                else log.Add("no MainMenuController in scene");

                GameSession.MarkStarted();
                if (menu != null)
                {
                    mmType.GetMethod("SetGameWorldPaused", BF)?.Invoke(menu, new object[] { false });
                    mmType.GetMethod("ReleaseGameplayInputCapture", BF)?.Invoke(menu, null);
                    mmType.GetMethod("RefreshGameplayCamera", SBF)?.Invoke(null, null);
                    mmType.GetMethod("RestoreGameplayUi", BF)?.Invoke(menu, null);
                }

                Type lo = FindType("LoadingOverlayController");
                lo?.GetMethod("ReleaseOpaqueCover", SBF)?.Invoke(null, null);
                Type flow = FindType("MainCanvasFlow");
                flow?.GetMethod("Refresh", SBF)?.Invoke(null, null);

                // any loader/menu document that is still painted: hide it
                foreach (var d in Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(FindObjectsInactive.Exclude))
                {
                    if (d == null || (d.name != "UITK_Loading" && d.name != "UITK_MainMenu") || d.rootVisualElement == null) continue;
                    foreach (var c in d.rootVisualElement.Children())
                        if (c.resolvedStyle.display != UnityEngine.UIElements.DisplayStyle.None)
                            c.style.display = UnityEngine.UIElements.DisplayStyle.None;
                }

                Time.timeScale = 1f;
            }
            catch (Exception e) { log.Add("bypass exception: " + (e.InnerException != null ? e.InnerException.Message : e.Message)); }
        }

        private static string VerifyGameplayState()
        {
            var p = new List<string>();
            if (!GameSession.HasStarted) p.Add("GameSession not started");
            if (GameplayWorldSimulation.IsFrozen) p.Add("world frozen");
            if (Time.timeScale <= 0.01f) p.Add("timeScale 0");
            if (LoaderBlocking()) p.Add("loading overlay blocking");
            Type mt = FindType("DMUiToolkitMainMenu");
            PropertyInfo vis = mt != null ? mt.GetProperty("IsVisible", SBF) : null;
            if (vis != null && (bool)vis.GetValue(null)) p.Add("main menu visible");
            Type mc = FindType("MainMenuController");
            PropertyInfo pause = mc != null ? mc.GetProperty("IsPauseOverlayActive", SBF) : null;
            if (pause != null && (bool)pause.GetValue(null)) p.Add("pause overlay active");
            foreach (var d in Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(FindObjectsInactive.Exclude))
            {
                if (d == null || (d.name != "UITK_Loading" && d.name != "UITK_MainMenu") || d.rootVisualElement == null) continue;
                foreach (var c in d.rootVisualElement.Children())
                    if (c.resolvedStyle.display != UnityEngine.UIElements.DisplayStyle.None) p.Add(d.name + "/" + c.name + " still displayed");
            }

            GameObject pl = GameObject.FindGameObjectWithTag("Player");
            var pi = pl != null ? pl.GetComponent<PlayerInput>() : null;
            if (pl == null) p.Add("no player");
            else if (pi != null && !pi.enabled) p.Add("PlayerInput disabled (player not controllable)");
            return string.Join(", ", p);
        }

        private static string DescribeGameplayState()
        {
            GameObject pl = GameObject.FindGameObjectWithTag("Player");
            var pi = pl != null ? pl.GetComponent<PlayerInput>() : null;
            return $"phase={GameSession.Phase}, frozen={GameplayWorldSimulation.IsFrozen}, timeScale={Time.timeScale}, loader=hidden, main menu=hidden, pause overlay=off, PlayerInput={(pi != null && pi.enabled ? "enabled" : "n/a")}, cursor={Cursor.lockState}";
        }

        private static List<GameObject> EnemyPrefabs()
        {
            var list = new List<GameObject>();
            string[] filters = string.IsNullOrWhiteSpace(_opts.filter)
                ? new string[0]
                : _opts.filter.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim().ToLowerInvariant()).ToArray();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EnemiesFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string lower = path.ToLowerInvariant();
                if (!(lower.Contains("/enemies/") || lower.Contains("enemy")))
                    continue;
                if (lower.Contains("/test") || lower.Contains("_old") || lower.Contains("backup") || lower.Contains("variant"))
                    continue;
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                if (IsExcludedEnemy(prefab.name)) continue;
                if (prefab.GetComponent<EnemyAiController>() == null || prefab.GetComponent<EnemyInvectorBootstrap>() == null)
                    continue;
                if (!_opts.includeTemplate && prefab.name == "HumanoidEnemy_Invector")
                    continue;
                if (filters.Length > 0 && !filters.Any(f => prefab.name.ToLowerInvariant().Contains(f)))
                    continue;
                list.Add(prefab);
            }
            return list.OrderBy(p => p.name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        // =============================================================== main routine
        private static IEnumerator Main()
        {
            yield return new WaitReal(1.5f);
            yield return BypassRoutine();
            if (!_bypassDone)
            {
                Finish("aborted", _bypassFail);
                yield break;
            }

            _player = GameObject.FindGameObjectWithTag("Player");
            if (_player == null) { Finish("aborted", "no GameObject tagged Player in the scene"); yield break; }
            EnforceTestTerrain(true); // Gaia terrain off before any spawn / Ground() snap
            _run.environment.Add($"Player: {_player.name} at {_player.transform.position:F1}, hp {Fmt(PlayerHpInvector())}/{Fmt(PlayerHpSurvival())}");
            _origin = Ground(_player.transform.position);
            yield return ArmPlayerRoutine();
            _camFollow = true;
            CameraBehind(); // camera behind the player, looking the way the player faces
            _run.environment.Add("Camera: set behind the player at spawn and re-seated after every teleport/movement step (vThirdPersonCamera); scroll-wheel zoom available");

            // Quiet the world: disable every pre-placed enemy and spawner so only the audited enemy acts.
            int disabled = 0;
            foreach (var e in Object.FindObjectsByType<EnemyAiController>(FindObjectsInactive.Exclude))
            {
                e.gameObject.SetActive(false); disabled++;
            }

            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude))
            {
                if (mb == null) continue;
                Type t = mb.GetType();
                if (t.Namespace != null && t.Namespace.StartsWith("Project.AI") && (t.Name.Contains("Spawner") || t.Name.Contains("EncounterZone")))
                {
                    mb.enabled = false; SceneDisabled.Add(t.Name + ":" + mb.name);
                }
            }

            _run.environment.Add($"Scene enemies deactivated for the run (runtime only): {disabled}; spawner components disabled: {SceneDisabled.Count}");
            _run.environment.Add("Time.timeScale=" + Time.timeScale.ToString(CultureInfo.InvariantCulture) + ", runInBackground=" + Application.runInBackground);

            List<GameObject> prefabs = EnemyPrefabs();
            _run.environment.Add("Enemy prefabs found: " + prefabs.Count);
            LoadStore();
            ComputeStamps();
            _run.environment.Add($"Unit store: {_store.entries.Count} stored unit(s); stamps audit {_auditStamp.Substring(0, 6)} game {_gameStamp.Substring(0, 6)} gear {_gearStamp.Substring(0, 6)}; skip-unchanged is {(FullMode ? "OFF (Full mode)" : "ON (Quick mode)")}");
            int i = 0;
            if (_opts.obstaclesOnly)
            {
                if (prefabs.Count == 0) { Finish("aborted", "No enemy prefab matched obstacle audit filter"); yield break; }
                if (_opts.rockMeshesOnly)
                    yield return Scn("Obstacle movement: scene rock MeshColliders", prefabs[0], ScnRockMeshes);
                else
                    yield return Scn("Obstacle movement: corners, wander, ping-pong, combat", prefabs[0], ScnObstacles);
                yield break;
            }
            foreach (GameObject prefab in prefabs)
            {
                i++;
                string path = AssetDatabase.GetAssetPath(prefab);
                string ukey = "enemy|" + prefab.name;
                string uhash = UnitHash(path, "enemy", false);
                if (TryReuse(ukey, uhash, null, out DMAuditEnemyResult reused)) { _run.enemies.Add(reused); SetStatus($"enemy {i}/{prefabs.Count}: {prefab.name} (cached)"); continue; }
                _cur = new DMAuditEnemyResult { name = prefab.name, prefabPath = path };
                _run.enemies.Add(_cur);
                SetStatus($"enemy {i}/{prefabs.Count}: {prefab.name}");
                AuditTiming.Begin(ukey);
                float started = Time.realtimeSinceStartup;
                if (_opts.interruptsOnly) yield return RunInterruptScenario(prefab);
                else yield return RunEnemy(prefab);
                _observeDeath = false;
                _cur.seconds = Time.realtimeSinceStartup - started;
                FinalizeConsole(_cur);
                Ph("cleanup");
                Cleanup();
                yield return new WaitGame(0.2f);
                AuditTiming.End();
                RecordUnit(ukey, uhash, _cur);
                _cur = null;
            }

            if (_opts.weaponMatrix && !_opts.interruptsOnly)
                yield return RunWeaponMatrix();
            if (_opts.scenarios && !_opts.interruptsOnly)
                yield return RunScenarios();
        }

        private static void FinalizeConsole(DMAuditEnemyResult r)
        {
            string first = r.consoleLines.Count > 0 ? r.consoleLines[0] : "";
            if (r.consoleErrors > 0)
                r.Add("Console", "console", "Console errors/warnings", "FAIL", $"{r.consoleErrors} error(s), {r.consoleWarnings} warning(s)", first);
            else if (r.consoleWarnings > 0)
                r.Add("Console", "console", "Console errors/warnings", "WARN", $"0 errors, {r.consoleWarnings} warning(s)", first);
            else
                r.Add("Console", "console", "Console errors/warnings", "PASS", "0 errors, 0 warnings", "");
        }
    }
}
#endif

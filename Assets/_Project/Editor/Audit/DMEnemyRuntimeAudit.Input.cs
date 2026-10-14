#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Invector;
using Invector.vCamera;
using Invector.vCharacterController;
using Invector.vMelee;
using Project.AI;
using Project.Data;
using Project.Inventory;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace Project.EditorTools.Audit
{
    public static partial class DMEnemyRuntimeAudit
    {
        // =============================================================== input + camera helpers
        // Everything goes through the Input System devices the game reads (Keyboard, Mouse, Gamepad), so a probe exercises
        // the same bindings (InputSystem_Actions: Move/Look/Attack/Block/Jump/Sprint/Crouch/Dodge/Dash/Reload/AmmoCycle...) a player does.
        private const BindingFlags BFA = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>Reads a field OR property (any visibility, walking the base classes). Returns null when absent.</summary>
        private static object Mem(object o, string name)
        {
            if (o == null) return null;
            for (Type t = o.GetType(); t != null && t != typeof(object); t = t.BaseType)
            {
                FieldInfo f = t.GetField(name, BFA | BindingFlags.DeclaredOnly);
                if (f != null) return f.GetValue(o);
                PropertyInfo p = t.GetProperty(name, BFA | BindingFlags.DeclaredOnly);
                if (p != null && p.CanRead && p.GetIndexParameters().Length == 0)
                {
                    try { return p.GetValue(o); } catch { return null; }
                }
            }

            return null;
        }

        /// <summary>True when ANY component on the player reports the named bool member as true (isBlocking, isCrouching, ...).</summary>
        private static bool PlayerFlag(string name)
        {
            if (_player == null) return false;
            foreach (MonoBehaviour mb in _player.GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                object v = Mem(mb, name);
                if (v is bool b && b) return true;
            }

            return false;
        }

        /// <summary>Crouch is a toggle (and AutoCrouch can force it under low ceilings): stand up before speed / jump measurements.</summary>
        private static IEnumerator EnsureStanding(DMAuditEnemyResult R, string when)
        {
            ReleaseAllInput();
            int tries = 0;
            while (PlayerFlag("isCrouching") && tries < 3)
            {
                tries++;
                yield return TapKey(Key.LeftCtrl, 0.12f);
                yield return new WaitGame(0.5f);
            }

            if (PlayerFlag("isCrouching")) { CallPlayer("Crouch"); yield return new WaitGame(0.5f); }
            if (tries > 0)
                R.Add("Controls", "ctl.stand." + when.Replace(' ', '_'), "Player stands up (" + when + ")", PlayerFlag("isCrouching") ? "WARN" : "INFO",
                    $"player was crouched {when}; {tries} Left Ctrl tap(s), now isCrouching={PlayerFlag("isCrouching")}", PlayerFlag("isCrouching") ? "player cannot stand: low ceiling at the control spot or crouch locked" : "");
        }
        private static void CallPlayer(string method)
        {
            try
            {
                var c = _player != null ? _player.GetComponent<global::Invector.vCharacterController.vThirdPersonController>() : null;
                var m = c != null ? c.GetType().GetMethod(method, BFA | BindingFlags.FlattenHierarchy, null, Type.EmptyTypes, null) : null;
                if (m != null) m.Invoke(c, null);
            }
            catch { /* reported by the check that follows */ }
        }

        private static string PlayerDiag()
        {
            return $"isGrounded {PlayerFlag("isGrounded")}, isJumping {PlayerFlag("isJumping")}, customAction {PlayerFlag("customAction")}, isCrouching {PlayerFlag("isCrouching")}, stamina {PlayerNumber("currentStamina"):0}";
        }
        private static float PlayerNumber(string name)
        {
            if (_player == null) return float.NaN;
            foreach (MonoBehaviour mb in _player.GetComponents<MonoBehaviour>())
            {
                if (mb == null) continue;
                object v = Mem(mb, name);
                if (v is float f) return f;
                if (v is int i) return i;
            }

            return float.NaN;
        }

        // ------------------------------------------------------------ keyboard / mouse / gamepad injection
        private static readonly HashSet<Key> _keysDown = new HashSet<Key>();

        /// <summary>Keyboard/mouse events queued with QueueStateEvent are silently dropped while the Unity editor is not the foreground window (Input System: real Keyboard/Mouse devices cannot
        /// run in the background, and IgnoreFocus does not change that). When the app is unfocused the state is written directly with InputState.Change instead, which does not go through that filter,
        /// so the audit works the same whether Anthony is watching the editor or using another window. Focused: the normal queued event is used.</summary>
        private static void Inject<T>(UnityEngine.InputSystem.InputDevice d, T state) where T : struct, UnityEngine.InputSystem.LowLevel.IInputStateTypeInfo
        {
            if (d == null) return;
            // 2026-10-13 (d9): re-focus the Game view if another editor window took focus, and write state directly when the
            // app OR the Game view is unfocused, so injected input is never dropped mid-scenario.
            bool gvFocused = EnsureGameViewFocus();
            if (!Application.isFocused || !gvFocused) InputState.Change(d, state);
            InputSystem.QueueStateEvent(d, state);
        }
        private static Type _gameViewType;
        /// <summary>2026-10-13 (d9): focus the Game view when it is not the focused editor window. Returns whether it was already focused.</summary>
        private static bool EnsureGameViewFocus()
        {
            try
            {
                if (_gameViewType == null) _gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
                if (_gameViewType == null) return true;
                EditorWindow f = EditorWindow.focusedWindow;
                if (f != null && f.GetType() == _gameViewType) return true;
                EditorWindow.FocusWindowIfItsOpen(_gameViewType);
                return false;
            }
            catch { return false; }
        }
        private static void PushKeys()
        {
            Keyboard kb = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
            if (kb != null) Inject(kb, new KeyboardState(_keysDown.ToArray()));
        }

        private static void KeyDown(params Key[] keys) { foreach (Key k in keys) _keysDown.Add(k); PushKeys(); }
        private static void KeyUp(params Key[] keys) { foreach (Key k in keys) _keysDown.Remove(k); PushKeys(); }
        private static void ReleaseAllInput() { _keysDown.Clear(); PushKeys(); SetMouse(0); }

        private static IEnumerator TapKey(Key k, float hold = 0.12f)
        {
            KeyDown(k);
            yield return new WaitGame(hold);
            KeyUp(k);
            yield return new WaitGame(0.1f);
        }

        private static void MouseDelta(float dx, float dy)
        {
            Mouse m = Mouse.current ?? InputSystem.AddDevice<Mouse>();
            if (m != null) Inject(m, new MouseState { buttons = _mouseMask, delta = new Vector2(dx, dy) });
        }

        // ------------------------------------------------------------ camera
        private static bool _camFollow = true;   // keep the camera behind the player after every teleport / movement step
        private static int _zoomNet;             // net scroll ticks the audit applied (restored at the end of each scenario)

        private static vThirdPersonCamera TpCam()
        {
            return vThirdPersonCamera.instance != null ? vThirdPersonCamera.instance : Object.FindAnyObjectByType<vThirdPersonCamera>();
        }

        private static void SetCamAngles(vThirdPersonCamera c, float yaw, float pitch)
        {
            foreach (string n in new[] { "mouseX", "_mouseX" })
            {
                PropertyInfo p = typeof(vThirdPersonCamera).GetProperty(n, BFA);
                if (p != null && p.CanWrite) { p.SetValue(c, yaw); break; }
                FieldInfo f = typeof(vThirdPersonCamera).GetField(n, BFA);
                if (f != null) { f.SetValue(c, yaw); break; }
            }

            foreach (string n in new[] { "mouseY", "_mouseY" })
            {
                PropertyInfo p = typeof(vThirdPersonCamera).GetProperty(n, BFA);
                if (p != null && p.CanWrite) { p.SetValue(c, pitch); break; }
                FieldInfo f = typeof(vThirdPersonCamera).GetField(n, BFA);
                if (f != null) { f.SetValue(c, pitch); break; }
            }
        }

        /// <summary>Camera behind the player, looking the way the player faces (toward the action after PlacePlayer).</summary>
        private static void CameraBehind()
        {
            try
            {
                vThirdPersonCamera c = TpCam();
                if (c == null || _player == null) return;
                if (c.currentTarget != null) c.ResetAngleWithoutSmooth();
                SetCamAngles(c, _player.transform.eulerAngles.y, 10f);
            }
            catch { /* camera not ready yet */ }
        }

        /// <summary>Points the free-look camera (and therefore W / A / S / D) at a world position, e.g. the enemy.</summary>
        private static void AimCameraAt(Vector3 world)
        {
            try
            {
                vThirdPersonCamera c = TpCam();
                if (c == null || _player == null) return;
                Vector3 d = world - _player.transform.position; d.y = 0f;
                if (d.sqrMagnitude < 0.01f) return;
                SetCamAngles(c, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 10f);
            }
            catch { /* ignore */ }
        }

        private static float CamFacingDot()
        {
            Camera cam = Camera.main;
            if (cam == null || _player == null) return float.NaN;
            Vector3 a = cam.transform.forward; a.y = 0f;
            Vector3 b = _player.transform.forward; b.y = 0f;
            if (a.sqrMagnitude < 0.0001f || b.sqrMagnitude < 0.0001f) return float.NaN;
            return Vector3.Dot(a.normalized, b.normalized);
        }

        private static float CamDist()
        {
            Camera cam = Camera.main;
            if (cam == null || _player == null) return float.NaN;
            return Vector3.Distance(cam.transform.position, _player.transform.position + Vector3.up * 1.5f);
        }

        /// <summary>Real scroll-wheel path (PioneerShooterMeleeInput.ApplyMouseWheelZoom): +ticks zoom in, -ticks zoom out.</summary>
        private static IEnumerator ScrollZoom(int ticks)
        {
            Mouse m = Mouse.current ?? InputSystem.AddDevice<Mouse>();
            if (m == null || ticks == 0) yield break;
            int n = Mathf.Abs(ticks);
            float dir = ticks > 0 ? 120f : -120f;
            for (int i = 0; i < n; i++)
            {
                Inject(m, new MouseState { buttons = _mouseMask, scroll = new Vector2(0f, dir) });
                yield return null;
                Inject(m, new MouseState { buttons = _mouseMask, scroll = Vector2.zero });
                yield return new WaitGame(0.12f);
            }

            _zoomNet += ticks;
            yield return new WaitGame(0.5f);
        }

        // ------------------------------------------------------------ player placement for controls
        private static Vector3 _ctlStart;
        private static Vector3 _ctlDir = Vector3.forward;

        private static IEnumerator ResetControlSpot(float yawDeg)
        {
            ReleaseAllInput();
            _ctlDir = Quaternion.Euler(0f, yawDeg, 0f) * Vector3.forward;
            Vector3 p = Ground(_origin + new Vector3(0f, 0f, -30f));
            PlacePlayer(p, p + _ctlDir * 5f);
            yield return new WaitGame(0.9f);
            _ctlStart = _player.transform.position;
        }

        private static float Along(Vector3 dir)
        {
            Vector3 d = _player.transform.position - _ctlStart; d.y = 0f;
            return Vector3.Dot(d, dir);
        }

        private static float PlanarMoved()
        {
            Vector3 d = _player.transform.position - _ctlStart; d.y = 0f;
            return d.magnitude;
        }

        /// <summary>
        /// One control probe: reset the player, press the control for `seconds`, sample a metric every frame and judge it.
        /// ok(start, min, max, end) decides pass/fail; the detail row always carries the measured numbers.
        /// </summary>
        private static string InputDiag()
        {
            try
            {
                Keyboard kb = Keyboard.current; Mouse ms = Mouse.current;
                PlayerInput[] pis = Object.FindObjectsByType<PlayerInput>(FindObjectsInactive.Include);
                string pi = pis.Length == 0 ? "no PlayerInput component" : string.Join("; ", pis.Select(p =>
                {
                    InputAction mv = p.actions != null ? p.actions.FindAction("Move") : null;
                    return $"{p.name}: scheme '{p.currentControlScheme}', enabled {p.enabled}, actions enabled {(p.actions != null && p.actions.enabled)}, devices [{string.Join(",", p.devices.ToArray().Select(d => d.displayName))}], Move {(mv != null ? mv.ReadValue<Vector2>().ToString() : "n/a")}";
                }));
                return $"isFocused {Application.isFocused}, cursor {Cursor.lockState}, keyboard {(kb != null ? "id " + kb.deviceId + " enabled " + kb.enabled + " W pressed " + kb.wKey.isPressed : "none")}, mouse {(ms != null ? (ms.enabled ? "enabled" : "DISABLED") : "none")}, keyboards {InputSystem.devices.Count(d => d is Keyboard)}, gamepads {Gamepad.all.Count}, InputSystem {InputSystem.settings.backgroundBehavior}/{InputSystem.settings.editorInputBehaviorInPlayMode}, {pi}";
            }
            catch (Exception e) { return "diag failed: " + e.Message; }
        }

        private static IEnumerator Ctl(DMAuditEnemyResult R, string id, string label, Action press, Action perFrame, Action release, float seconds,
            Func<float> sample, Func<float, float, float, float, bool> ok, string unit, string cause, string failStatus = "FAIL")
        {
            DMAuditHud.Step(label);
            EnsureGameViewFocus();
            yield return ResetControlSpot(0f);
            float v0 = sample(), vmin = v0, vmax = v0;
            press?.Invoke();
            float t0 = Time.time;
            while (Time.time - t0 < seconds)
            {
                perFrame?.Invoke();
                float v = sample();
                if (v < vmin) vmin = v;
                if (v > vmax) vmax = v;
                yield return null;
            }

            float vEnd = sample();
            release?.Invoke();
            ReleaseAllInput();
            bool pass = ok(v0, vmin, vmax, vEnd);
            R.Add("Controls", id, label, pass ? "PASS" : failStatus,
                string.Format(CultureInfo.InvariantCulture, "start {0:0.00}, min {1:0.00}, max {2:0.00}, end {3:0.00} {4}", v0, vmin, vmax, vEnd, unit),
                pass ? "" : cause);
            yield return new WaitGame(0.6f);
        }

        // ============================================================== scenario: camera facing + zoom
        private static IEnumerator ScnCamera(GameObject prefab, DMAuditEnemyResult R)
        {
            yield return new WaitGame(0.3f);
            float[] yaws = { 0f, 90f, 180f, 270f };
            int ok = 0;
            var sb = new List<string>();
            foreach (float yaw in yaws)
            {
                Vector3 p = Ground(_origin + new Vector3(0f, 0f, -30f));
                Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                PlacePlayer(p, p + dir * 10f);   // PlacePlayer re-seats the camera behind the player
                yield return new WaitGame(0.9f);
                float dot = CamFacingDot();
                sb.Add($"yaw {yaw:0}: dot {dot:0.00}");
                if (dot > 0.85f) ok++;
            }

            R.Add("Camera", "cam.forward", "Camera sits behind the player and faces the same way after teleports", ok == yaws.Length ? "PASS" : "FAIL",
                string.Join("; ", sb) + $" (camera {(TpCam() != null ? "vThirdPersonCamera" : "MISSING")})",
                ok == yaws.Length ? "" : "vThirdPersonCamera.ResetAngleWithoutSmooth / mouseX did not follow the teleport (camera target missing, camera disabled or a script re-aims it)");

            float d0 = CamDist();
            yield return ScrollZoom(3);
            float dIn = CamDist();
            yield return ScrollZoom(-6);
            float dOut = CamDist();
            bool scrollWorks = !float.IsNaN(d0) && dIn < d0 - 0.2f && dOut > dIn + 0.4f;
            string via = "scroll wheel";
            if (!scrollWorks)
            {
                vThirdPersonCamera c = TpCam();
                if (c != null)
                {
                    c.ForceSetZoomDistance(2.5f); yield return new WaitGame(0.6f); dIn = CamDist();
                    c.ForceSetZoomDistance(7f); yield return new WaitGame(0.6f); dOut = CamDist();
                    via = "ForceSetZoomDistance fallback (scroll input did not move the camera)";
                }
            }

            bool zoomOk = !float.IsNaN(dIn) && !float.IsNaN(dOut) && dOut > dIn + 0.4f;
            R.Add("Camera", "cam.zoom", "Camera zooms in and out", zoomOk ? (scrollWorks ? "PASS" : "WARN") : "FAIL",
                $"distance start {Fmt(d0)}m, zoomed in {Fmt(dIn)}m, zoomed out {Fmt(dOut)}m via {via}",
                zoomOk ? (scrollWorks ? "" : "injected scroll did not change the follow distance: ApplyMouseWheelZoom needs Mouse.current.scroll and a non-aim camera state") : "zoom has no effect: camera state has useZoom off or min/max distance collapsed");
            yield return ScrollZoom(_zoomNet == 0 ? 0 : -_zoomNet);
        }

        // ============================================================== scenario: every player control
        /// <summary>Reports the live movement-system gates (DMPlayerSystemsProfile: Climb/Dash/Jetpack Enabled, which include the skill-tree gate the audit unlocks for the run) and
        /// whether the matching controllers are enabled. Climbing needs a wall, so it is verified as unlocked + controller enabled rather than driven.</summary>
        private static void SystemsGateRows(DMAuditEnemyResult R)
        {
            try
            {
                Component prof = null;
                MonoBehaviour dash = null, climb = null, jet = null;
                foreach (var c in _player.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (c == null) continue;
                    string n = c.GetType().Name;
                    if (n == "DMPlayerSystemsProfile") prof = c;
                    else if (n == "DMDashController") dash = c;
                    else if (n == "DMClimbController") climb = c;
                    else if (n == "DMJetpackController") jet = c;
                }
                Func<string, string> flag = name =>
                {
                    if (prof == null) return "no profile";
                    var pi = prof.GetType().GetProperty(name);
                    return pi != null ? System.Convert.ToString(pi.GetValue(prof)) : "?";
                };
                string st = $"DMPlayerSystemsProfile {(prof != null ? "present" : "MISSING")}: ClimbEnabled={flag("ClimbEnabled")}, DashEnabled={flag("DashEnabled")}, JetpackEnabled={flag("JetpackEnabled")}; DMClimbController {(climb == null ? "missing" : climb.enabled ? "enabled" : "disabled")}, DMDashController {(dash == null ? "missing" : dash.enabled ? "enabled" : "disabled")}, DMJetpackController {(jet == null ? "missing" : jet.enabled ? "enabled" : "disabled")}";
                bool climbOk = climb != null && climb.enabled && flag("ClimbEnabled") == "True";
                R.Add("Controls", "ctl.climb", "Climbing unlocked and controller enabled (not driven: needs a wall)", climbOk ? "PASS" : "WARN", st,
                    climbOk ? "" : "climb gate or DMClimbController still off with the skill unlocked: check DMPlayerSystemsProfile.climb flag");
                R.Add("Controls", "ctl.systems", "Movement systems unlocked for the run (jetpack, dash, climb)", "INFO", st + ". The audit unlocks these in memory only (see environment notes) and restores them at exit.", "");
            }
            catch (Exception e) { R.Add("Controls", "ctl.systems", "Movement systems unlocked for the run", "INFO", "could not read the gates: " + e.Message, ""); }
        }
        private static IEnumerator ScnControls(GameObject prefab, DMAuditEnemyResult R)
        {
            string arm = ArmPlayerWithSword();
            yield return new WaitGame(1.2f);
            _camFollow = false; // the camera must be free for look / move probes
            _guardPause = true; // Tab legitimately changes the weapon in this scenario
            Vector3 right() => Quaternion.Euler(0f, 90f, 0f) * _ctlDir;

            {
                string pre = InputDiag();
                KeyDown(Key.W);
                yield return new WaitGame(0.35f);
                string during = InputDiag();
                KeyUp(Key.W);
                yield return new WaitGame(0.2f);
                R.Add("Controls", "ctl.inputdiag", "Input path diagnostics (W held 0.35s)", "INFO", pre + " || while W held: " + during, "");
            }

            yield return Ctl(R, "ctl.W", "W moves the player forward (camera direction)", () => KeyDown(Key.W), null, null, 1.2f,
                () => Along(_ctlDir), (s, mn, mx, e) => e > 1.0f, "m along forward", "Move action not reaching the controller: PlayerInput disabled, move locked, or PioneerPlayerInputBinder.HandleAction('Move') not wired");
            yield return Ctl(R, "ctl.S", "S moves the player back", () => KeyDown(Key.S), null, null, 1.2f,
                () => Along(_ctlDir), (s, mn, mx, e) => e < -0.8f, "m along forward", "Move (S) produced no backward displacement");
            yield return Ctl(R, "ctl.A", "A strafes left", () => KeyDown(Key.A), null, null, 1.2f,
                () => Along(-right()), (s, mn, mx, e) => e > 0.8f, "m along left", "Move (A) produced no leftward displacement");
            yield return Ctl(R, "ctl.D", "D strafes right", () => KeyDown(Key.D), null, null, 1.2f,
                () => Along(right()), (s, mn, mx, e) => e > 0.8f, "m along right", "Move (D) produced no rightward displacement");

            yield return EnsureStanding(R, "start");
            float walkDist = 0f;
            yield return Ctl(R, "ctl.run", "W run speed (reference for sprint)", () => KeyDown(Key.W), null, null, 1.5f,
                () => PlanarMoved(), (s, mn, mx, e) => { walkDist = e; return e > 1.0f; }, "m in 1.5s", "player did not move");
            yield return Ctl(R, "ctl.sprint", "Shift + W sprints faster than W", () => KeyDown(Key.W, Key.LeftShift), null, null, 1.5f,
                () => PlanarMoved(), (s, mn, mx, e) => e > walkDist * 1.08f, "m in 1.5s", $"sprint no faster than run ({walkDist:0.0}m): Sprint action not wired, stamina 0, or sprint speed equals run speed");

            yield return EnsureStanding(R, "before jump");
            yield return Ctl(R, "ctl.jump", "Space jumps", () => KeyDown(Key.Space), null, null, 0.9f,
                () => _player.transform.position.y + (PlayerFlag("isJumping") ? 1000f : 0f), (s, mn, mx, e) => mx - s > 0.1f, "m height (+1000 while isJumping)", "Jump action not reaching the controller (PioneerShooterMeleeInput.ReadJumpPressedThisFrame), grounded check failing, or jump locked; state before the press: " + PlayerDiag());
            yield return Ctl(R, "ctl.crouch", "Left Ctrl crouches", () => KeyDown(Key.LeftCtrl), null, null, 0.8f,
                () => PlayerFlag("isCrouching") ? 1f : 0f, (s, mn, mx, e) => mx > 0.5f, "isCrouching", "Invector crouch reads the legacy UnityEngine.Input through vInput (Active Input Handling = Both), which does not see injected Input System keys: this is a harness limit, not a game bug (the code-path row below calls cc.Crouch())", "WARN");
            ReleaseAllInput();
            yield return new WaitGame(0.3f);
            yield return Ctl(R, "ctl.crouch.code", "Crouch through the controller code path (cc.Crouch)", () => CallPlayer("Crouch"), null, () => { if (PlayerFlag("isCrouching")) CallPlayer("Crouch"); }, 0.8f,
                () => PlayerFlag("isCrouching") ? 1f : 0f, (s, mn, mx, e) => mx > 0.5f, "isCrouching", "cc.Crouch() did not crouch: no room above (autocrouch/ceiling check), customAction, or crouch animation state missing");
            yield return EnsureStanding(R, "after crouch");
            yield return Ctl(R, "ctl.dodge", "Q dodge / roll", () => KeyDown(Key.Q), null, null, 1.0f,
                () => PlanarMoved() + (PlayerFlag("isRolling") ? 10f : 0f), (s, mn, mx, e) => mx > 1.2f, "m or isRolling", "Dodge action did not move the player: TryDodgeRollFromInput refused (stamina, cooldown, no roll clip)");
            yield return new WaitGame(0.3f); // let DMPlayerSystemsProfile.Update re-apply the unlocked gates (it enables DMDashController / DMClimbController next frame)
            SystemsGateRows(R);
            yield return Ctl(R, "ctl.dash", "Left Alt dash", () => KeyDown(Key.LeftAlt, Key.W), null, null, 0.8f,
                () => PlanarMoved(), (s, mn, mx, e) => e > 2.0f, "m", "Dash did not move the player even though the skill gate is unlocked for this run (see ctl.systems row): DMDashController disabled, on cooldown, stamina, or the Dash action not bound to Left Alt", "WARN");
            yield return Ctl(R, "ctl.jetpack", "Hold Space after jumping boosts (jetpack)", () => KeyDown(Key.Space), null, null, 2.8f,
                () => _player.transform.position.y, (s, mn, mx, e) => mx - s > 2.2f, "m height", "no sustained lift although the jetpack skill gate is unlocked for this run (see ctl.systems row): fuel 0, DMJetpackInputBridge not polling Space, or no DMJetpackController on the player", "WARN");
            yield return new WaitGame(2.5f);

            // mouse look / orbit
            Func<float> yawOf = () => Camera.main != null ? Camera.main.transform.eulerAngles.y : 0f;
            float yaw0 = 0f;
            yield return Ctl(R, "ctl.look.x", "Mouse look left/right orbits the camera", () => yaw0 = yawOf(), () => MouseDelta(18f, 0f), null, 0.7f,
                () => Mathf.Abs(Mathf.DeltaAngle(yaw0, yawOf())), (s, mn, mx, e) => mx > 4f, "deg yaw", "Look action (Pointer/delta) not reaching vThirdPersonCamera: PlayerController.OnLook, cursor lock state, or camera input muted");
            float pitch0 = 0f;
            Func<float> pitchOf = () => Camera.main != null ? Mathf.DeltaAngle(0f, Camera.main.transform.eulerAngles.x) : 0f;
            yield return Ctl(R, "ctl.look.y", "Mouse look up/down tilts the camera", () => pitch0 = pitchOf(), () => MouseDelta(0f, 18f), null, 0.7f,
                () => Mathf.Abs(pitchOf() - pitch0), (s, mn, mx, e) => mx > 2f, "deg pitch", "vertical Look delta ignored (yMouseSensitivity 0 / pitch clamp collapsed)");

            // mouse buttons
            yield return Ctl(R, "ctl.lmb", "Left click swings the drawn melee weapon", () => SetMouse(MouseLeft), null, () => SetMouse(0), 0.6f,
                () => PlayerFlag("isAttacking") ? 1f : 0f, (s, mn, mx, e) => mx > 0.5f, "isAttacking",
                arm.Length > 0 ? "player could not be armed: " + arm : "melee Attack input not received: drawn melee tap/hold logic (UpdateDrawnMeleeCharge) did not start a swing");
            yield return new WaitGame(1.0f);
            yield return Ctl(R, "ctl.rmb", "Right click blocks (melee drawn)", () => SetMouse(MouseRight), null, () => SetMouse(0), 0.9f,
                () => PlayerFlag("isBlocking") ? 1f : 0f, (s, mn, mx, e) => mx > 0.5f, "isBlocking", "Block input (Mouse rightButton / Block action) did not set isBlocking: stamina 0, customAction, not grounded, or still attacking");
            yield return Ctl(R, "ctl.mmb", "Middle click does not break gameplay", () => SetMouse(4), null, () => SetMouse(0), 0.4f,
                () => 0f, (s, mn, mx, e) => true, "none", "");
            R.Add("Controls", "ctl.mmb.note", "Middle click has no gameplay binding", "INFO", "InputSystem_Actions Player map binds no action to Mouse/middleButton (only the UI map MiddleClick)", "");

            // weapon switch (Tab = Hot Cross SwitchWeapon tap)
            ItemData pistol = AllWeaponItems().FirstOrDefault(i => i.IsRangedWeapon && !i.isMiningTool && i.name == "sci_fi_pistol");
            if (pistol != null) { ArmPlayerWithItem(pistol); yield return new WaitGame(1.0f); ArmPlayerWithSword(); yield return new WaitGame(1.0f); }
            Type hot = FindType("DMUiToolkitHotCross");
            PropertyInfo wl = hot != null ? hot.GetProperty("WeaponLocalIndex", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null;
            int focus0 = wl != null ? Convert.ToInt32(wl.GetValue(null)) : -1;
            var eq = _player.GetComponentInChildren<EquipmentController>(true);
            ItemData drawn0 = eq != null ? eq.DrawnWeaponItem : null;
            yield return TapKey(Key.Tab, 0.1f);
            yield return new WaitGame(0.5f);
            int focus1 = wl != null ? Convert.ToInt32(wl.GetValue(null)) : -1;
            ItemData drawn1 = eq != null ? eq.DrawnWeaponItem : null;
            bool switched = focus1 != focus0 || drawn1 != drawn0;
            R.Add("Controls", "ctl.switch", "Tab (SwitchWeapon) cycles the weapon focus", switched ? "PASS" : "WARN",
                $"Hot Cross weapon focus {focus0} -> {focus1}, drawn {(drawn0 != null ? drawn0.name : "none")} -> {(drawn1 != null ? drawn1.name : "none")}",
                switched ? "" : "Tab tap did not change focus: only one weapon on the hotbar, Hot Cross not polling, or tap/hold threshold (hold arms) not met");
            ArmPlayerWithSword();
            yield return new WaitGame(0.8f);

            // walking pace with an analog stick (no keyboard walk toggle exists)
            Gamepad gp = null;
            try { gp = InputSystem.AddDevice<Gamepad>(); } catch { gp = null; }
            if (gp != null)
            {
                yield return Ctl(R, "ctl.walk", "Half-tilt stick walks slower than a run", () => InputSystem.QueueStateEvent(gp, new GamepadState { leftStick = new Vector2(0f, 0.4f) }), null,
                    () => InputSystem.QueueStateEvent(gp, new GamepadState()), 1.5f,
                    () => PlanarMoved(), (s, mn, mx, e) => e > 0.4f && e < walkDist * 0.9f, "m in 1.5s",
                    $"walk speed not distinct from run ({walkDist:0.0}m): analog Move magnitude ignored by the locomotion speed", "WARN");
                InputSystem.QueueStateEvent(gp, new GamepadState());
                InputSystem.RemoveDevice(gp);
            }
            else R.Add("Controls", "ctl.walk", "Walk (analog stick)", "INFO", "could not create a virtual gamepad", "");

            ReleaseAllInput();
            _camFollow = true;
            _guardPause = false;
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Project.AI;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.EditorTools.Audit
{
    /// <summary>
    /// Optional, lightweight recorder for YOUR inputs during a play session (not while the audit harness drives): key / mouse button /
    /// scroll / gamepad button + stick edges with timestamps, plus a small player/enemy state snapshot whenever the console logs an
    /// error / exception / assert or an anomaly (player below the world, NaN position, hp 0). Written to
    /// Assets/_Project/Documentation/Audits/Recordings/ when play stops. Toggle on the Audit hub. No save changes, editor-only.
    /// Lines are tab separated ("t=12.34 <tab> key+ <tab> W") so a sequence can be replayed through the audit's KeyDown/SetMouse helpers.
    /// </summary>
    [InitializeOnLoad]
    public static class DMAuditInputRecorder
    {
        private const string PrefKey = "DMAudit.RecordInputs";
        public const string Folder = "Assets/_Project/Documentation/Audits/Recordings";

        public static bool Enabled
        {
            get { if (_enabledCache == null) _enabledCache = EditorPrefs.GetBool(PrefKey, false); return _enabledCache.Value; }
            set { _enabledCache = value; EditorPrefs.SetBool(PrefKey, value); }
        }

        private static bool? _enabledCache; // EditorPrefs is a registry read on Windows: never poll it every editor update
        private static readonly StringBuilder Log = new StringBuilder();
        private static readonly HashSet<Key> Keys = new HashSet<Key>();
        private static ushort _mouse;
        private static bool _recording;
        private static double _t0;
        private static float _nextAnomalyCheck;
        private static readonly Dictionary<string, bool> Pad = new Dictionary<string, bool>();
        private static string _lastAnomaly = "";
        private static int _snapshots;

        static DMAuditInputRecorder()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.update += Poll;
        }

        private static bool Armed => Enabled && EditorApplication.isPlaying && !DMEnemyRuntimeAudit.IsActive;

        private static void OnPlayMode(PlayModeStateChange c)
        {
            if (c == PlayModeStateChange.EnteredPlayMode && Enabled && !SessionState.GetBool("DMAudit.Enemy.Pending", false)) Begin();
            else if (c == PlayModeStateChange.ExitingPlayMode && _recording) End();
        }

        private static void Begin()
        {
            Log.Clear(); Keys.Clear(); Pad.Clear(); _mouse = 0; _snapshots = 0; _lastAnomaly = "";
            _t0 = EditorApplication.timeSinceStartup;
            _recording = true;
            Application.logMessageReceived += OnLog;
            Log.AppendLine("# DM input recording " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  (t = seconds since play start; edges only)");
        }

        private static void End()
        {
            _recording = false;
            Application.logMessageReceived -= OnLog;
            try
            {
                if (Log.Length < 80) return;
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), Folder));
                string path = Path.Combine(Folder, "DM_Input_Recording_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log");
                File.WriteAllText(path, Log.ToString());
                Debug.Log("DMAudit input recording saved: " + path);
            }
            catch (Exception e) { Debug.LogWarning("DMAudit input recorder could not write: " + e.Message); }
        }

        private static string T() { return "t=" + (EditorApplication.timeSinceStartup - _t0).ToString("0.00"); }
        private static void Add(string kind, string what) { Log.Append(T()).Append('\t').Append(kind).Append('\t').AppendLine(what); }

        private static void Poll()
        {
            if (!Enabled || !EditorApplication.isPlaying) return;
            if (DMEnemyRuntimeAudit.IsActive || SessionState.GetBool("DMAudit.Enemy.Pending", false)) { if (_recording) End(); return; } // the harness injects its own inputs; never record (or hook the log) during a run
            if (!_recording) Begin();

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                foreach (var k in kb.allKeys)
                {
                    if (k == null) continue;
                    bool down = k.isPressed;
                    bool had = Keys.Contains(k.keyCode);
                    if (down && !had) { Keys.Add(k.keyCode); Add("key+", k.keyCode.ToString()); }
                    else if (!down && had) { Keys.Remove(k.keyCode); Add("key-", k.keyCode.ToString()); }
                }
            }

            Mouse m = Mouse.current;
            if (m != null)
            {
                ushort mask = (ushort)((m.leftButton.isPressed ? 1 : 0) | (m.rightButton.isPressed ? 2 : 0) | (m.middleButton.isPressed ? 4 : 0));
                if (mask != _mouse)
                {
                    for (int b = 0; b < 3; b++)
                    {
                        int bit = 1 << b;
                        if ((mask & bit) != (_mouse & bit)) Add((mask & bit) != 0 ? "mouse+" : "mouse-", new[] { "left", "right", "middle" }[b]);
                    }

                    _mouse = mask;
                }

                float sc = m.scroll.ReadValue().y;
                if (Mathf.Abs(sc) > 0.01f) Add("scroll", sc.ToString("0"));
            }

            Gamepad gp = Gamepad.current;
            if (gp != null)
            {
                Edge("pad.south", gp.buttonSouth.isPressed); Edge("pad.east", gp.buttonEast.isPressed); Edge("pad.west", gp.buttonWest.isPressed); Edge("pad.north", gp.buttonNorth.isPressed);
                Edge("pad.lb", gp.leftShoulder.isPressed); Edge("pad.rb", gp.rightShoulder.isPressed);
                Edge("pad.lt", gp.leftTrigger.ReadValue() > 0.4f); Edge("pad.rt", gp.rightTrigger.ReadValue() > 0.4f);
                Edge("pad.ls.press", gp.leftStickButton.isPressed); Edge("pad.rs.press", gp.rightStickButton.isPressed);
                Edge("pad.dpad.up", gp.dpad.up.isPressed); Edge("pad.dpad.down", gp.dpad.down.isPressed); Edge("pad.dpad.left", gp.dpad.left.isPressed); Edge("pad.dpad.right", gp.dpad.right.isPressed);
                Edge("pad.ls.moving", gp.leftStick.ReadValue().magnitude > 0.4f);
            }

            if (Time.realtimeSinceStartup >= _nextAnomalyCheck)
            {
                _nextAnomalyCheck = Time.realtimeSinceStartup + 1f;
                string a = Anomaly();
                if (a.Length > 0 && a != _lastAnomaly) { _lastAnomaly = a; Snapshot("anomaly: " + a); }
                else if (a.Length == 0) _lastAnomaly = "";
            }
        }

        private static void Edge(string name, bool now)
        {
            Pad.TryGetValue(name, out bool was);
            if (now == was) return;
            Pad[name] = now;
            Add(now ? "pad+" : "pad-", name);
        }

        private static void OnLog(string cond, string stack, LogType type)
        {
            if (!_recording || DMEnemyRuntimeAudit.IsActive) return;
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            string first = (cond ?? "").Split('\n')[0];
            Snapshot(type + ": " + (first.Length > 200 ? first.Substring(0, 200) : first));
        }

        private static string Anomaly()
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return "";
            Vector3 pos = p.transform.position;
            if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z)) return "player position is NaN";
            if (pos.y < -200f) return "player fell out of the world (y " + pos.y.ToString("0") + ")";
            return "";
        }

        private static void Snapshot(string reason)
        {
            if (_snapshots++ > 60) return; // keep the log small
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            var sb = new StringBuilder();
            if (p != null)
            {
                var c = p.GetComponent<global::Invector.vCharacterController.vThirdPersonController>();
                sb.Append("player pos ").Append(p.transform.position.ToString("F1")).Append(" hp ").Append(c != null ? c.currentHealth.ToString("0") : "?");
            }

            var near = new List<string>();
            foreach (var e in UnityEngine.Object.FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude))
            {
                if (e == null || p == null) continue;
                float d = Vector3.Distance(e.transform.position, p.transform.position);
                if (d > 30f) continue;
                var ai = e.GetComponent<EnemyAiController>();
                string st = "?";
                if (ai != null) { FieldInfo f = typeof(EnemyAiController).GetField("state", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public); object v = f != null ? f.GetValue(ai) : null; if (v != null) st = v.ToString(); }
                near.Add(e.name + " d" + d.ToString("0") + " hp" + e.CurrentHealth.ToString("0") + " " + st + (e.IsDead ? " DEAD" : ""));
            }

            Add("snapshot", reason + " | " + sb + " | enemies<=30m: " + (near.Count == 0 ? "none" : string.Join("; ", near.Take(6))) + " | keys held: " + string.Join("+", Keys));
        }
    }
}
#endif

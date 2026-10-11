#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Audit
{
    /// <summary>Game-view label for the enemy runtime audit: current unit / scenario + step + time, so a watcher can tell what is being tested.
    /// Editor-only, created at run start and destroyed at run end; toggle on the Audit Hub (default on).</summary>
    public sealed class DMAuditHudBehaviour : MonoBehaviour
    {
        public string Unit = "", Step = "", Clock = "";
        private GUIStyle _style;
        private GUIStyle _small;

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperLeft, wordWrap = false };
                _style.normal.textColor = Color.white;
                _small = new GUIStyle(_style) { fontSize = 13, fontStyle = FontStyle.Normal };
                _small.normal.textColor = new Color(0.85f, 0.95f, 1f, 1f);
            }

            Rect box = new Rect(10f, 10f, Mathf.Min(Screen.width - 20f, 760f), 62f);
            Color c = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = c;
            GUI.Label(new Rect(box.x + 8f, box.y + 4f, box.width - 16f, 26f), "AUDIT  " + Unit, _style);
            GUI.Label(new Rect(box.x + 8f, box.y + 32f, box.width - 16f, 24f), Step + "   [" + Clock + "]", _small);
        }
    }

    public static class DMAuditHud
    {
        public const string Pref = "DMAudit.StepHud";
        public static bool Enabled { get { return EditorPrefs.GetBool(Pref, true); } set { EditorPrefs.SetBool(Pref, value); } }

        private static bool _on;
        private static GameObject _go;
        private static DMAuditHudBehaviour _b;
        private static string _unit = "", _step = "";

        public static void Begin()
        {
            _unit = _step = "";
            _on = Enabled;
            if (!_on || !Application.isPlaying) return;
            if (_go != null) UnityEngine.Object.Destroy(_go);
            _go = new GameObject("DMAuditStepHud") { hideFlags = HideFlags.HideAndDontSave };
            _b = _go.AddComponent<DMAuditHudBehaviour>();
        }

        public static void End()
        {
            _on = false;
            if (_go != null) UnityEngine.Object.Destroy(_go);
            _go = null; _b = null;
        }

        public static void Unit(string unit, string step)
        {
            _unit = unit ?? ""; Set(step);
        }

        public static void Step(string step) { Set(step); }

        private static void Set(string step)
        {
            _step = step ?? "";
            if (!_on) return;
            string clock = DateTime.Now.ToString("HH:mm:ss");
            if (_b != null) { _b.Unit = _unit; _b.Step = _step; _b.Clock = clock; }
            Debug.Log("[DMAudit " + clock + "] " + _unit + " > " + _step);
        }
    }
}
#endif

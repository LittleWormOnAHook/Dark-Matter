// 2026-11-01b: Pre-Prologue Set light budget. Shadows off except N hero lights (largest range x intensity among the
// shadowed ones), range 8-15 m (lights > 40 m -> 25 m), affects volumetrics off. Undo + log Logs/lights1101b.tsv, Revert.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Project.EditorTools.GenesisStudio
{
    public static class DMPrePrologueLights1101b
    {
        public const float BaseMargin = 40f;
        static string LogPath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs", "lights1101b.tsv");

        static List<Light> SetLights()
        {
            var go = GameObject.Find("PrePrologue_Set");
            if (!go) return new List<Light>();
            // Set + the landing base around it (MARS spots / prefab lights inside the set bounds + BaseMargin), not the sun.
            Bounds b = new Bounds(); bool any = false; // set root sits at 0,0,0; children are out at the base, so use their bounds
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true)) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            b.Expand(BaseMargin * 2f);
            return Object.FindObjectsByType<Light>(FindObjectsInactive.Include)
                .Where(l => l.gameObject.scene.IsValid() && l.type != LightType.Directional && (l.hideFlags & HideFlags.DontSave) == 0
                    && (l.transform.IsChildOf(go.transform) || (any && b.Contains(l.transform.position)))).ToList();
        }

        public static string Stats()
        {
            var ls = SetLights(); if (ls.Count == 0) return "no PrePrologue_Set in the open scene";
            int sh = ls.Count(l => l.shadows != LightShadows.None); float mr = ls.Count > 0 ? ls.Max(l => l.range) : 0f;
            return ls.Count + " lights, " + sh + " shadowed, max range " + mr.ToString("0") + " m" + (File.Exists(LogPath) ? " (applied; Revert available)" : "");
        }

        public static string Apply(int heroes)
        {
            var ls = SetLights(); if (ls.Count == 0) return "no PrePrologue_Set";
            bool fresh = !File.Exists(LogPath); // keep the ORIGINAL values from the first apply
            var hero = new HashSet<Light>(ls.Where(l => l.shadows != LightShadows.None).OrderByDescending(l => l.range * l.intensity).Take(Mathf.Max(0, heroes)));
            Undo.IncrementCurrentGroup(); int g = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("11-01b Pre-Prologue lights");
            var lines = new List<string>(); int shOff = 0, rng = 0, vol = 0; var heroNames = new List<string>();
            foreach (Light l in ls)
            {
                var hd = l.GetComponent<HDAdditionalLightData>();
                if (fresh) lines.Add(GlobalObjectId.GetGlobalObjectIdSlow(l) + "\t" + (int)l.shadows + "\t" + l.range.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\t" + (hd != null && hd.affectsVolumetric ? 1 : 0));
                Undo.RecordObject(l, "light"); if (hd) Undo.RecordObject(hd, "light");
                if (hero.Contains(l)) heroNames.Add(Path_(l.transform) + " (" + l.range.ToString("0") + " m)");
                else if (l.shadows != LightShadows.None) { l.shadows = LightShadows.None; shOff++; }
                float want = l.range > 40f ? 25f : Mathf.Clamp(l.range, 8f, 15f);
                if (l.type == LightType.Directional) want = l.range;
                if (!Mathf.Approximately(want, l.range)) { l.range = want; rng++; }
                if (hd != null && hd.affectsVolumetric) { hd.affectsVolumetric = false; vol++; }
                EditorUtility.SetDirty(l); if (hd) EditorUtility.SetDirty(hd);
            }
            Undo.CollapseUndoOperations(g);
            if (fresh) { Directory.CreateDirectory(Path.GetDirectoryName(LogPath)); File.WriteAllLines(LogPath, lines); }
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            return "Pre-Prologue lights: " + ls.Count + " lights, shadows off " + shOff + ", range changed " + rng + ", volumetrics off " + vol + ". Heroes: " + string.Join("; ", heroNames);
        }

        public static string Revert()
        {
            if (!File.Exists(LogPath)) return "no lights1101b log";
            Undo.IncrementCurrentGroup(); int g = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("11-01b lights revert");
            int n = 0, miss = 0;
            foreach (string line in File.ReadAllLines(LogPath))
            {
                string[] p = line.Split('\t'); if (p.Length < 4 || !GlobalObjectId.TryParse(p[0], out GlobalObjectId id)) { miss++; continue; }
                if (!(GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) is Light l)) { miss++; continue; }
                var hd = l.GetComponent<HDAdditionalLightData>();
                Undo.RecordObject(l, "light"); if (hd) Undo.RecordObject(hd, "light");
                l.shadows = (LightShadows)int.Parse(p[1]); l.range = float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture); if (hd) hd.affectsVolumetric = p[3] == "1";
                EditorUtility.SetDirty(l); if (hd) EditorUtility.SetDirty(hd); n++;
            }
            Undo.CollapseUndoOperations(g);
            File.Move(LogPath, LogPath + ".reverted_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            return "lights reverted " + n + (miss > 0 ? ", missing " + miss : "");
        }

        static string Path_(Transform t) { string s = t.name; for (var p = t.parent; p != null && p.name != "PrePrologue_Set" && p.parent != null; p = p.parent) s = p.name + "/" + s; return s; }
    }
}
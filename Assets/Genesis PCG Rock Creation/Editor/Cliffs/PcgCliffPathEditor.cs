using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>Scene view editing for a Sheer Cliff path: Shift+click adds points, Ctrl+click removes, handles move them.</summary>
    [CustomEditor(typeof(PcgCliffPath))]
    public sealed class PcgCliffPathEditor : UnityEditor.Editor
    {
        private static readonly Color Curve = new Color(1f, 0.54f, 0.16f, 1f);   // Frontier orange
        private static readonly Color Top = new Color(1f, 0.76f, 0.24f, 0.6f);   // gold
        private int m_selected = -1;
        private double m_regenAt = -1;

        private PcgCliffPath Path => (PcgCliffPath)target;

        private void OnEnable()
        {
            EditorApplication.update += Tick;
            Undo.undoRedoPerformed += OnUndo;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            Undo.undoRedoPerformed -= OnUndo;
        }

        private void OnUndo() { if (Path != null) Schedule(); }

        private void Schedule()
        {
            m_regenAt = EditorApplication.timeSinceStartup + 0.25;
        }

        private void Tick()
        {
            if (m_regenAt < 0 || EditorApplication.timeSinceStartup < m_regenAt) return;
            if (GUIUtility.hotControl != 0) { m_regenAt = EditorApplication.timeSinceStartup + 0.1; return; } // wait for mouse up
            m_regenAt = -1;
            PcgCliffPath p = Path;
            if (p == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
            p.Regenerate(true);
            SceneView.RepaintAll();
            Repaint();
        }

        private void Changed(PcgCliffPath p)
        {
            p.Baked = false;
            EditorUtility.SetDirty(p);
            Schedule();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            PcgCliffPath p = Path;
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Scene view: Shift+click adds a point, Ctrl+click a point removes it, click a point to move it.\n" +
                                    (p.Baked ? "Baked: meshes are saved assets with LODs." : "Preview: meshes are rebuilt when the scene opens. Bake to save them."), MessageType.None);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate")) { p.Regenerate(true); }
                if (GUILayout.Button("Bake Chunks")) Debug.Log("Sheer Cliff: " + PcgCliffBake.Bake(p), p);
                if (GUILayout.Button("Clear")) p.ClearGenerated();
            }
            if (!string.IsNullOrEmpty(p.LastStats.error) || p.LastStats.parts > 0)
                EditorGUILayout.LabelField(p.LastStats.ToString(), EditorStyles.miniLabel);
        }

        private void OnSceneGUI()
        {
            PcgCliffPath p = Path;
            if (p == null || p.points == null) return;
            Transform tr = p.transform;
            Event e = Event.current;
            bool closed = p.closed && p.points.Count >= 3;

            // Curve, face arrows and the approximate top edge.
            List<Vector3> poly = PcgCliffCurve.Sample(p.points, closed, 1f);
            var world = new Vector3[poly.Count];
            for (int i = 0; i < poly.Count; i++) world[i] = tr.TransformPoint(poly[i]);
            Handles.color = Curve;
            if (world.Length > 1) Handles.DrawAAPolyLine(4f, world);
            float h = p.settings != null ? p.settings.height : 30f;
            var top = new Vector3[world.Length];
            for (int i = 0; i < world.Length; i++) top[i] = world[i] + Vector3.up * h;
            Handles.color = Top;
            if (top.Length > 1) Handles.DrawAAPolyLine(2f, top);
            float every = Mathf.Max(3f, (p.settings != null ? p.settings.moduleWidth : 5f) * 2f), acc = every * 0.5f;
            for (int i = 1; i < world.Length; i++)
            {
                Vector3 seg = world[i] - world[i - 1];
                seg.y = 0f;
                acc += seg.magnitude;
                if (acc < every || seg.sqrMagnitude < 1e-6f) continue;
                acc = 0f;
                Vector3 t = seg.normalized;
                Vector3 n = p.settings != null && p.settings.faceSide == PcgCliffFaceSide.Left ? Vector3.Cross(t, Vector3.up) : Vector3.Cross(Vector3.up, t);
                float s = HandleUtility.GetHandleSize(world[i]) * 0.6f;
                Handles.color = Curve;
                Handles.ArrowHandleCap(0, world[i] + Vector3.up * 0.2f, Quaternion.LookRotation(n), s, EventType.Repaint);
            }

            // Points: click selects, Ctrl/Cmd+click removes.
            for (int i = 0; i < p.points.Count; i++)
            {
                Vector3 w = tr.TransformPoint(p.points[i]);
                float size = HandleUtility.GetHandleSize(w) * 0.12f;
                Handles.color = i == m_selected ? Color.white : Curve;
                if (Handles.Button(w, Quaternion.identity, size, size * 1.2f, Handles.SphereHandleCap))
                {
                    if ((e.control || e.command) && p.points.Count > 2)
                    {
                        Undo.RecordObject(p, "Remove Cliff Point");
                        p.points.RemoveAt(i);
                        m_selected = -1;
                        Changed(p);
                        GUIUtility.ExitGUI();
                    }
                    m_selected = i;
                    Repaint();
                }
            }

            if (m_selected >= 0 && m_selected < p.points.Count)
            {
                Vector3 w = tr.TransformPoint(p.points[m_selected]);
                EditorGUI.BeginChangeCheck();
                Vector3 nw = Handles.PositionHandle(w, Tools.pivotRotation == PivotRotation.Local ? tr.rotation : Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(p, "Move Cliff Point");
                    p.points[m_selected] = tr.InverseTransformPoint(nw);
                    Changed(p);
                }
            }

            // Shift+click on the ground adds a point.
            if (e.shift)
            {
                int id = GUIUtility.GetControlID(FocusType.Passive);
                if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(id);
                if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
                {
                    if (RaycastGround(p, HandleUtility.GUIPointToWorldRay(e.mousePosition), out Vector3 hit))
                    {
                        Undo.RecordObject(p, "Add Cliff Point");
                        m_selected = Insert(p, tr.InverseTransformPoint(hit), closed);
                        Changed(p);
                    }
                    e.Use();
                }
            }
        }

        private static bool RaycastGround(PcgCliffPath p, Ray ray, out Vector3 hit)
        {
            float best = float.MaxValue;
            hit = default;
            foreach (RaycastHit h in Physics.RaycastAll(ray, 5000f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.transform.IsChildOf(p.transform)) continue;
                if (h.distance < best) { best = h.distance; hit = h.point; }
            }
            if (best < float.MaxValue) return true;
            var plane = new Plane(Vector3.up, p.transform.position);
            if (plane.Raycast(ray, out float d)) { hit = ray.GetPoint(d); return true; }
            return false;
        }

        /// <summary>Inserts into the nearest segment, or appends at the nearer end when the click is beyond it.</summary>
        private static int Insert(PcgCliffPath p, Vector3 local, bool closed)
        {
            List<Vector3> pts = p.points;
            int n = pts.Count;
            if (n < 2) { pts.Add(local); return pts.Count - 1; }
            int segs = closed ? n : n - 1, bestSeg = 0;
            float bestD = float.MaxValue, bestT = 0f;
            Vector2 q = new Vector2(local.x, local.z);
            for (int i = 0; i < segs; i++)
            {
                Vector2 a = new Vector2(pts[i].x, pts[i].z), b = new Vector2(pts[(i + 1) % n].x, pts[(i + 1) % n].z);
                Vector2 ab = b - a;
                float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude) : 0f;
                float d = (a + ab * t - q).sqrMagnitude;
                if (d < bestD) { bestD = d; bestSeg = i; bestT = t; }
            }
            if (!closed && bestSeg == 0 && bestT <= 0f) { pts.Insert(0, local); return 0; }
            if (!closed && bestSeg == segs - 1 && bestT >= 1f) { pts.Add(local); return pts.Count - 1; }
            pts.Insert(bestSeg + 1, local);
            return bestSeg + 1;
        }
    }
}

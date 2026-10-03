#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using GenesisPCG.RockCreation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>
    /// Scene view: hold Shift and drag a selected proc rock (any IPcgSeeded, e.g. DmRockCombiner) to drag out a copy.
    /// - With the Move/Transform tool handle: Shift + drag the handle.
    /// - Free drag: Shift + drag on the rock itself (away from the handle); the rock follows the mouse over the ground.
    /// A copy is left behind at the start pose (identical shape); the dragged rock becomes the new variation
    /// (new stamp + seed via the paste-reseed path, unless Lock Seed is on) and snaps to the surface under it,
    /// aligned to the surface normal, while dragging and on release. One Undo step for the whole drag.
    /// Ctrl/Cmd+Shift (Unity surface snapping) and drags without Shift are left untouched.
    /// </summary>
    [InitializeOnLoad]
    internal static class PcgShiftDragDuplicate
    {
        private enum Mode { None, Armed, FreeArmed, HandleDrag, FreeDrag }

        private static readonly int ControlHash = "PcgShiftDragDuplicate".GetHashCode();
        private const float HandleGuardPixels = 110f;

        private static Mode s_mode;
        private static int s_freeControl;
        private static int s_undoGroup;
        private static Vector2 s_mouseDown;
        private static Transform s_grabbed;
        private static readonly List<Transform> s_rocks = new List<Transform>();
        private static readonly List<Transform> s_stationary = new List<Transform>();
        private static readonly List<Transform> s_ignore = new List<Transform>();
        private static readonly Dictionary<Transform, Pose> s_start = new Dictionary<Transform, Pose>();
        private static readonly Dictionary<Transform, Vector3> s_offsets = new Dictionary<Transform, Vector3>();
        private static Vector3 s_anchor;

        static PcgShiftDragDuplicate()
        {
            SceneView.beforeSceneGui -= BeforeSceneGui;
            SceneView.beforeSceneGui += BeforeSceneGui;
        }

        private static bool ShiftOnly(Event e) => e.shift && !e.control && !e.command && !e.alt;

        private static void BeforeSceneGui(SceneView sv)
        {
            Event e = Event.current;
            int id = GUIUtility.GetControlID(ControlHash, FocusType.Passive);

            switch (e.type)
            {
                case EventType.MouseDown:
                    Reset();
                    if (e.button != 0 || e.alt)
                        break;
                    CollectSelectedRocks();
                    if (s_rocks.Count == 0)
                        break;
                    foreach (Transform t in s_rocks)
                        s_start[t] = new Pose(t.position, t.rotation);
                    s_mouseDown = e.mousePosition;
                    s_undoGroup = Undo.GetCurrentGroup();
                    s_mode = Mode.Armed;

                    if (ShiftOnly(e) && GUIUtility.hotControl == 0 && !NearTransformHandle(e.mousePosition))
                    {
                        GameObject picked = HandleUtility.PickGameObject(e.mousePosition, false);
                        Transform rock = picked != null ? FindRockRoot(picked.transform) : null;
                        if (rock != null)
                        {
                            s_grabbed = rock;
                            s_freeControl = id;
                            GUIUtility.hotControl = id;
                            s_mode = Mode.FreeArmed;
                            e.Use();
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    if (s_mode == Mode.Armed && ShiftOnly(e) && GUIUtility.hotControl != 0 && AnyRockMoved())
                    {
                        // A transform tool handle is moving the selection with Shift held.
                        BeginDuplicate();
                        s_mode = Mode.HandleDrag;
                    }
                    if (s_mode == Mode.HandleDrag)
                    {
                        SnapDragged(true);
                    }
                    else if ((s_mode == Mode.FreeArmed || s_mode == Mode.FreeDrag) && GUIUtility.hotControl == s_freeControl)
                    {
                        if (s_mode == Mode.FreeArmed && (e.mousePosition - s_mouseDown).sqrMagnitude > 16f)
                        {
                            BeginDuplicate();
                            BeginFreeDrag();
                            s_mode = Mode.FreeDrag;
                        }
                        if (s_mode == Mode.FreeDrag)
                            FreeDragTo(e.mousePosition);
                        e.Use();
                    }
                    break;

                case EventType.Repaint:
                    if (s_mode == Mode.HandleDrag)
                        SnapDragged(false); // tools rewrite the position every drag event; re-snap before the frame is drawn
                    break;

                case EventType.MouseUp:
                    if (s_mode == Mode.HandleDrag)
                    {
                        SnapDragged(true);
                        Finish();
                    }
                    else if ((s_mode == Mode.FreeArmed || s_mode == Mode.FreeDrag) && GUIUtility.hotControl == s_freeControl)
                    {
                        GUIUtility.hotControl = 0;
                        if (s_mode == Mode.FreeArmed && s_grabbed != null)
                        {
                            // Plain Shift+click on a selected rock: keep Unity's toggle-selection behaviour.
                            var sel = new List<Object>(Selection.objects);
                            sel.Remove(s_grabbed.gameObject);
                            Selection.objects = sel.ToArray();
                        }
                        else
                        {
                            FreeDragTo(e.mousePosition);
                            Finish();
                        }
                        e.Use();
                    }
                    Reset();
                    break;
            }
        }

        // ---------------------------------------------------------------------------------------------

        private static void CollectSelectedRocks()
        {
            s_rocks.Clear();
            foreach (Transform t in Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.Editable))
                if (t != null && t.GetComponent<IPcgSeeded>() != null && !EditorUtility.IsPersistent(t))
                    s_rocks.Add(t);
        }

        private static Transform FindRockRoot(Transform t)
        {
            for (; t != null; t = t.parent)
                if (s_rocks.Contains(t))
                    return t;
            return null;
        }

        private static bool NearTransformHandle(Vector2 mouse)
        {
            if (Tools.current == Tool.View || Tools.current == Tool.None || Tools.hidden)
                return false;
            Vector2 hp = HandleUtility.WorldToGUIPoint(Tools.handlePosition);
            return (mouse - hp).magnitude < HandleGuardPixels;
        }

        private static bool AnyRockMoved()
        {
            foreach (Transform t in s_rocks)
                if (t != null && s_start.TryGetValue(t, out Pose p) && (t.position - p.position).sqrMagnitude > 1e-8f)
                    return true;
            return false;
        }

        private static void BeginDuplicate()
        {
            s_stationary.Clear();
            foreach (Transform t in s_rocks)
            {
                if (t == null) continue;
                Pose start = s_start.TryGetValue(t, out Pose p) ? p : new Pose(t.position, t.rotation);
                GameObject copy = DuplicateStationary(t.gameObject, start);
                if (copy != null)
                    s_stationary.Add(copy.transform);

                // The dragged rock is the new copy: new identity + seed through the paste-reseed path.
                var seeded = t.GetComponent<IPcgSeeded>();
                Undo.RecordObject(t.gameObject, "Shift+Drag Duplicate");
                t.gameObject.name = GameObjectUtility.GetUniqueNameForSibling(t.parent, t.gameObject.name);
                PcgSeededPasteWatcher.ReseedAsCopy(seeded, "Shift+Drag Duplicate");
            }
            s_ignore.Clear();
            s_ignore.AddRange(s_rocks);
            s_ignore.AddRange(s_stationary);
        }

        /// <summary>Leaves an identical copy at <paramref name="start"/> (watcher-suppressed so it keeps its seed).</summary>
        internal static GameObject DuplicateStationary(GameObject src, Pose start)
        {
            GameObject copy;
            if (PrefabUtility.IsOutermostPrefabInstanceRoot(src))
            {
                Object asset = PrefabUtility.GetCorrespondingObjectFromSource(src);
                copy = (GameObject)PrefabUtility.InstantiatePrefab(asset, src.transform.parent);
                PcgSeededPasteWatcher.Suppress(copy);
                PrefabUtility.SetPropertyModifications(copy, PrefabUtility.GetPropertyModifications(src));
            }
            else
            {
                // Instantiate copies serialized seed/stamp, so OnEnable rebuilds the exact same shape.
                copy = Object.Instantiate(src, src.transform.parent);
                PcgSeededPasteWatcher.Suppress(copy);
            }
            copy.name = src.name;
            // Instantiate without a parent lands in the ACTIVE scene; keep the copy in the source rock's scene
            // (multi-scene editing: otherwise the stationary copy silently ends up in another open scene).
            if (copy.transform.parent == null && copy.scene != src.scene && src.scene.IsValid())
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(copy, src.scene);
            copy.transform.SetPositionAndRotation(start.position, start.rotation);
            copy.transform.localScale = src.transform.localScale;
            copy.transform.SetSiblingIndex(src.transform.GetSiblingIndex());
            Undo.RegisterCreatedObjectUndo(copy, "Shift+Drag Duplicate");
            return copy;
        }

        private static void SnapDragged(bool record)
        {
            foreach (Transform t in s_rocks)
            {
                if (t == null) continue;
                var seeded = t.GetComponent<IPcgSeeded>();
                PcgSurfaceSnapSettings s = seeded != null ? seeded.SnapSettings : PcgSurfaceSnapSettings.Default;
                if (!s.snapToSurface) continue;
                if (record) Undo.RecordObject(t, "Shift+Drag Duplicate");
                PcgSurfaceSnap.Snap(t, s, s_ignore);
            }
        }

        private static void BeginFreeDrag()
        {
            s_offsets.Clear();
            PcgSurfaceSnapSettings s = s_grabbed.GetComponent<IPcgSeeded>().SnapSettings;
            s_anchor = MouseOnSurface(s_mouseDown, s, s_grabbed.position);
            foreach (Transform t in s_rocks)
                if (t != null) s_offsets[t] = t.position - s_anchor;
        }

        private static void FreeDragTo(Vector2 mouse)
        {
            if (s_grabbed == null) return;
            PcgSurfaceSnapSettings gs = s_grabbed.GetComponent<IPcgSeeded>().SnapSettings;
            Vector3 p = MouseOnSurface(mouse, gs, s_anchor);
            foreach (Transform t in s_rocks)
            {
                if (t == null) continue;
                Undo.RecordObject(t, "Shift+Drag Duplicate");
                Vector3 off = s_offsets.TryGetValue(t, out Vector3 o) ? o : Vector3.zero;
                t.position = new Vector3(p.x + off.x, p.y + off.y, p.z + off.z);
                var seeded = t.GetComponent<IPcgSeeded>();
                PcgSurfaceSnapSettings s = seeded != null ? seeded.SnapSettings : PcgSurfaceSnapSettings.Default;
                if (s.snapToSurface)
                    PcgSurfaceSnap.Snap(t, s, s_ignore);
            }
        }

        /// <summary>Point under the mouse on the ground (ignoring the dragged rocks); falls back to a horizontal plane.</summary>
        private static Vector3 MouseOnSurface(Vector2 mouse, PcgSurfaceSnapSettings s, Vector3 planePoint)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mouse);
            if (PcgSurfaceSnap.RaycastSurface(ray, s, s_grabbed != null ? s_grabbed.gameObject.layer : -1, s_ignore, out PcgSurfaceSnap.Result r))
                return r.point;
            var plane = new Plane(Vector3.up, planePoint);
            return plane.Raycast(ray, out float d) ? ray.GetPoint(d) : planePoint;
        }

        private static void Finish()
        {
            int group = s_undoGroup;
            EditorApplication.delayCall += () => Undo.CollapseUndoOperations(group);
            RequestBakes();
        }

        /// <summary>Bake the dragged copies and the stationary ones once the drag has settled (Bake On Shift-Drag Copy / Paste).</summary>
        private static void RequestBakes()
        {
            foreach (Transform t in s_rocks.Concat(s_stationary))
            {
                if (t == null) continue;
                var c = t.GetComponent<DmRockCombiner>();
                if (c != null && c.BakeSettings.bakeOnShiftDragCopyPaste)
                    DmRockBakeScheduler.Request(c, 0.3f, false);
            }
        }

        private static void Reset()
        {
            s_mode = Mode.None;
            s_grabbed = null;
            s_rocks.Clear();
            s_stationary.Clear();
            s_ignore.Clear();
            s_start.Clear();
            s_offsets.Clear();
        }

        // ---------------------------------------------------------------------------------------------
        // Test hook (MCP / scripts): runs the same duplicate + reseed + snap path without mouse input.

        /// <summary>Simulates a Shift+drag of <paramref name="rocks"/> to world positions <paramref name="targets"/>. Returns the stationary copies.</summary>
        internal static List<GameObject> SimulateShiftDrag(IList<GameObject> rocks, IList<Vector3> targets)
        {
            Reset();
            foreach (GameObject g in rocks)
                if (g != null && g.GetComponent<IPcgSeeded>() != null)
                {
                    s_rocks.Add(g.transform);
                    s_start[g.transform] = new Pose(g.transform.position, g.transform.rotation);
                }
            s_undoGroup = Undo.GetCurrentGroup();
            BeginDuplicate();
            for (int i = 0; i < s_rocks.Count; i++)
            {
                Undo.RecordObject(s_rocks[i], "Shift+Drag Duplicate");
                s_rocks[i].position = targets[Mathf.Min(i, targets.Count - 1)];
            }
            SnapDragged(true);
            var result = new List<GameObject>();
            foreach (Transform t in s_stationary) result.Add(t.gameObject);
            Undo.CollapseUndoOperations(s_undoGroup);
            RequestBakes();
            Reset();
            return result;
        }
    }
}
#endif

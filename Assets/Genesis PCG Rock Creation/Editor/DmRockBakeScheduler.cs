#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using GenesisPCG.RockCreation;
using UnityEditor;
using UnityEngine;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>
    /// Debounced auto-bake: after Shift+drag copy release, paste / Ctrl+D, and (re)bake of baked rocks that were moved
    /// (re-snap height, re-conform, re-mask, rebake into the same asset) or edited. A request runs once the rock's
    /// transform has been stable for its delay and no mouse drag (hot control) is active. Bakes never record Undo;
    /// the bake state is derived and re-validated (settings hash + pose) whenever Undo restores a rock.
    /// Also sweeps orphaned baked assets after rocks are deleted or un-baked.
    /// </summary>
    [InitializeOnLoad]
    public static class DmRockBakeScheduler
    {
        private sealed class Pending
        {
            public double due, since;
            public float delay;
            public bool snap;
            public bool hasStart; public Vector3 start; // pose the rock was baked at before the move
            public Vector3 p; public Quaternion r; public Vector3 s;
        }

        private static readonly Dictionary<DmRockCombiner, Pending> s_pending = new Dictionary<DmRockCombiner, Pending>();
        private static double s_lastUndo = -10;
        private static double s_sweepDue = -1;
        private const int MaxBakesPerTick = 4;

        public static int PendingCount => s_pending.Count;
        public static int BakesDone { get; private set; }
        public static DmRockBaker.Report LastReport { get; private set; }

        static DmRockBakeScheduler()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            DmRockCombiner.Unbaked -= OnUnbaked;
            DmRockCombiner.Unbaked += OnUnbaked;
            Undo.undoRedoPerformed -= OnUndo;
            Undo.undoRedoPerformed += OnUndo;
            ObjectChangeEvents.changesPublished -= OnChanges;
            ObjectChangeEvents.changesPublished += OnChanges;
        }

        private static void OnUndo() => s_lastUndo = EditorApplication.timeSinceStartup;

        public static void Request(DmRockCombiner c, float delay, bool snap)
        {
            if (c == null || EditorUtility.IsPersistent(c) || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            double now = EditorApplication.timeSinceStartup;
            Transform t = c.transform;
            if (s_pending.TryGetValue(c, out Pending p))
            {
                p.snap |= snap;
                p.delay = Mathf.Max(p.delay, delay);
                p.due = now + p.delay;
                return;
            }
            s_pending[c] = new Pending
            {
                due = now + delay, since = now, delay = delay, snap = snap, p = t.position, r = t.rotation, s = t.lossyScale,
                hasStart = snap && c.HasLastBakedPose, start = c.LastBakedPosition,
            };
        }

        public static void Cancel(DmRockCombiner c) => s_pending.Remove(c);

        private static void OnUnbaked(DmRockCombiner c, DmRockCombiner.UnbakeReason reason, Mesh oldMesh)
        {
            ScheduleSweep(3.0);
            if (DmRockBaker.Busy || c == null)
                return;
            DmRockBakeSettings bs = c.BakeSettings;
            bool fromUndo = EditorApplication.timeSinceStartup - s_lastUndo < 0.5;
            switch (reason)
            {
                case DmRockCombiner.UnbakeReason.Moved:
                    // Undo/redo restores an exact pose: rebake there, never re-snap (no fight with the undo history).
                    if (bs.autoRebakeOnMove) Request(c, 0.3f, !fromUndo);
                    break;
                case DmRockCombiner.UnbakeReason.Edited:
                    if (bs.rebakeAfterEdit) Request(c, 0.5f, false);
                    break;
                case DmRockCombiner.UnbakeReason.MissingAsset:
                    if (bs.rebakeAfterEdit || bs.bakeOnShiftDragCopyPaste) Request(c, 0.5f, false);
                    break;
            }
        }

        private static void OnChanges(ref ObjectChangeEventStream stream)
        {
            bool sweep = false;
            for (int i = 0; i < stream.length; i++)
            {
                ObjectChangeKind kind = stream.GetEventType(i);
                if (kind == ObjectChangeKind.DestroyGameObjectHierarchy) sweep = true;
                else if (kind == ObjectChangeKind.ChangeGameObjectOrComponentProperties)
                {
                    stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out ChangeGameObjectOrComponentPropertiesEventArgs e);
                    CollectMoved(EditorUtility.EntityIdToObject(e.entityId));
                }
            }
            if (sweep) ScheduleSweep(3.0);
        }

        // Edit-mode Update only ticks when the editor runs a player-loop update (it does while dragging in a focused
        // Scene view, but not while the editor is in the background, nor reliably for Inspector number fields or
        // scripts / tools that move rocks with Undo). A moved rock then kept its old terrain match, per-pixel binding
        // and bake. Moves published as object changes are handled from the editor update as well.
        private static readonly HashSet<DmRockCombiner> s_moved = new HashSet<DmRockCombiner>();

        private static void CollectMoved(Object o)
        {
            GameObject go = o as GameObject ?? (o as Component)?.gameObject;
            if (go == null || EditorUtility.IsPersistent(go)) return;
            foreach (DmRockCombiner c in go.GetComponentsInChildren<DmRockCombiner>(true))
                if (c.transform.hasChanged) s_moved.Add(c);
        }

        private static void HandleMoved()
        {
            if (s_moved.Count == 0) return;
            DmRockCombiner[] moved = s_moved.ToArray();
            s_moved.Clear();
            foreach (DmRockCombiner c in moved)
                if (c != null) c.HandleEditorMove();
        }

        private static void ScheduleSweep(double delay)
        {
            double due = EditorApplication.timeSinceStartup + delay;
            if (s_sweepDue < 0 || due > s_sweepDue) s_sweepDue = due;
        }

        private static void Tick()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;
            if (!DmRockBaker.Busy) HandleMoved();
            double now = EditorApplication.timeSinceStartup;
            if (s_pending.Count > 0)
            {
                int done = 0;
                bool editing = false; // batch every save of this tick into a single asset refresh
                try
                {
                    foreach (DmRockCombiner c in s_pending.Keys.ToList())
                    {
                        Pending p = s_pending[c];
                        if (c == null || !c.isActiveAndEnabled) { s_pending.Remove(c); continue; }
                        Transform t = c.transform;
                        if ((t.position - p.p).sqrMagnitude > 1e-10f || Quaternion.Angle(t.rotation, p.r) > 0.001f || (t.lossyScale - p.s).sqrMagnitude > 1e-10f)
                        {
                            p.p = t.position; p.r = t.rotation; p.s = t.lossyScale;
                            p.due = now + p.delay; // still moving: debounce
                            continue;
                        }
                        if (now < p.due) continue;
                        if (GUIUtility.hotControl != 0 && now - p.due < 5.0) continue; // mouse still held on a handle
                        if (done >= MaxBakesPerTick) break;
                        s_pending.Remove(c);
                        if (!DmRockBaker.AtlasReady(c))
                        {
                            // A new shared atlas imports its textures synchronously: build it outside the batch.
                            if (editing) { AssetDatabase.StopAssetEditing(); editing = false; }
                            DmRockBaker.EnsureAtlas(c, out _);
                        }
                        if (!editing) { AssetDatabase.StartAssetEditing(); editing = true; }
                        if (p.snap) SnapAfterMove(c, p);
                        if (DmRockBaker.Bake(c, false, out DmRockBaker.Report r))
                        {
                            BakesDone++;
                            LastReport = r;
                        }
                        done++;
                    }
                }
                finally
                {
                    if (editing) AssetDatabase.StopAssetEditing();
                }
            }
            if (s_sweepDue > 0 && now >= s_sweepDue && s_pending.Count == 0 && GUIUtility.hotControl == 0)
            {
                s_sweepDue = -1;
                DmRockBaker.CleanUnused(null);
            }
        }

        /// <summary>
        /// After a baked rock was moved: a vertical-only move is a deliberate sink / lift, so it is stored as the rock's
        /// user sink offset and the rock stays where it was put. Any horizontal move re-snaps, keeping that offset.
        /// </summary>
        private static void SnapAfterMove(DmRockCombiner c, Pending p)
        {
            Transform t = c.transform;
            if (p.hasStart)
            {
                Vector3 d = t.position - p.start;
                if (new Vector2(d.x, d.z).magnitude < 0.01f && Mathf.Abs(d.y) > 0.002f && GroundTarget(c, out Vector3 target0))
                {
                    Vector3 up = t.rotation * Vector3.up;
                    float offset = Vector3.Dot(target0 - t.position, up);
                    c.UserSinkOffset = Mathf.Clamp(offset, -5f, 50f);
                    EditorUtility.SetDirty(c);
                    return;
                }
            }
            SnapHeight(c);
        }

        private static bool GroundTarget(DmRockCombiner c, out Vector3 target)
        {
            target = default;
            PcgSurfaceSnapSettings s = c.SnapSettings;
            if (s == null || !s.snapToSurface) return false;
            Transform t = c.transform;
            PcgSurfaceSnap.Result r = PcgSurfaceSnap.FindSurface(t.position, s, t.gameObject.layer, new List<Transform> { t });
            if (!r.hit) return false;
            target = r.point - (t.rotation * Vector3.up) * s.sinkDepth; // without the user offset
            return true;
        }

        /// <summary>Drops the rock (pivot = bottom center) onto the ground under it, keeping the user's rotation and sink offset. No Undo record.</summary>
        internal static bool SnapHeight(DmRockCombiner c)
        {
            if (!GroundTarget(c, out Vector3 target)) return false;
            Transform t = c.transform;
            target -= (t.rotation * Vector3.up) * c.UserSinkOffset;
            if ((target - t.position).sqrMagnitude < 1e-8f) return false;
            t.position = target;
            EditorUtility.SetDirty(t);
            return true;
        }
    }
}
#endif

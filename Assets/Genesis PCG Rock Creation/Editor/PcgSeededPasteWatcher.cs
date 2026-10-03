#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using GenesisPCG.RockCreation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>
    /// Watches for new objects in the scene (paste, Ctrl+D, prefab drag-in). If a new seeded proc object
    /// (any IPcgSeeded, e.g. DmRockCombiner) still shares its identity with another live one, it is a copy, so it
    /// rolls a new seed (unless Lock Seed is on) and snaps to the surface under it.
    /// Undo-restores and cut/paste have no live twin, so they keep their exact shape and pose.
    /// </summary>
    [InitializeOnLoad]
    public static class PcgSeededPasteWatcher
    {
        private static readonly HashSet<GameObject> Suppressed = new HashSet<GameObject>();

        static PcgSeededPasteWatcher()
        {
            ObjectChangeEvents.changesPublished -= OnChangesPublished;
            ObjectChangeEvents.changesPublished += OnChangesPublished;
        }

        /// <summary>Tell the watcher to leave a tool-created copy alone (e.g. the Shift+drag stationary copy).</summary>
        public static void Suppress(GameObject go)
        {
            if (go != null)
                Suppressed.Add(go);
        }

        private static void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            // Collect everything created in this batch first: a batch can hold an original AND its copies
            // (e.g. create + Ctrl+D in one editor tick), and only the copies may reseed.
            var created = new List<IPcgSeeded>();
            for (int i = 0; i < stream.length; i++)
            {
                if (stream.GetEventType(i) != ObjectChangeKind.CreateGameObjectHierarchy)
                    continue;

                stream.GetCreateGameObjectHierarchyEvent(i, out CreateGameObjectHierarchyEventArgs data);
                if (!(EditorUtility.EntityIdToObject(data.entityId) is GameObject go))
                    continue;
                if (Suppressed.Remove(go))
                    continue;

                foreach (IPcgSeeded seeded in go.GetComponentsInChildren<IPcgSeeded>(true))
                    if (!created.Contains(seeded))
                        created.Add(seeded);
            }
            Suppressed.RemoveWhere(g => g == null);
            if (created.Count == 0)
                return;

            var createdSet = new HashSet<IPcgSeeded>(created);
            var keptStamps = new HashSet<long>();
            foreach (IPcgSeeded seeded in created)
            {
                var c = seeded as Component;
                if (c == null || EditorUtility.IsPersistent(c))
                    continue;
                long stamp = seeded.PlacementStamp;
                bool olderTwin = false, anyTwin = false;
                foreach (IPcgSeeded o in PcgSeededRegistry.All)
                {
                    if (ReferenceEquals(o, seeded) || o.PlacementStamp != stamp) continue;
                    anyTwin = true;
                    if (!createdSet.Contains(o)) { olderTwin = true; break; }
                }
                if (!anyTwin)
                    continue;
                // Twins only among this batch: the first one (event order) is the original and keeps its seed.
                if (!olderTwin && keptStamps.Add(stamp))
                    continue;
                HandleCreated(seeded);
            }
        }

        private static void HandleCreated(IPcgSeeded seeded)
        {
            var c = seeded as Component;

            if (seeded.LockSeed)
            {
                // Exact copy: keep the seed, but give it its own identity.
                Undo.RecordObject(c, "Proc Copy");
                seeded.NewPlacementStamp();
                CloneBlendForCopy(c, "Proc Copy");
                EditorUtility.SetDirty(c);
            }
            else
            {
                ReseedAsCopy(seeded, "Proc New Variation");
            }
            SnapWithUndo(c.transform, seeded.SnapSettings, null, "Proc Snap To Surface");
            if (c is DmRockCombiner rc && rc.BakeSettings.bakeOnShiftDragCopyPaste)
                DmRockBakeScheduler.Request(rc, 0.5f, false);
        }

        /// <summary>New placement stamp + new seed, recorded for Undo.</summary>
        public static void ReseedAsCopy(IPcgSeeded seeded, string undoName)
        {
            var c = (Component)seeded;
            Undo.RecordObject(c, undoName);
            seeded.NewPlacementStamp();
            CloneBlendForCopy(c, undoName);
            if (!seeded.LockSeed)
                seeded.SetSeed(NewSeed());
            EditorUtility.SetDirty(c);
        }

        /// <summary>A copy must not edit the original's per-rock blend: give it its own (cloned from the original's).</summary>
        private static void CloneBlendForCopy(Component c, string undoName)
        {
            if (c is DmRockCombiner rc && DmRockBlendMaterials.NeedsOwnBlend(rc))
                DmRockBlendMaterials.EnsureOwnBlend(rc, undoName);
        }

        public static bool SnapWithUndo(Transform t, PcgSurfaceSnapSettings settings, IList<Transform> ignore, string undoName)
        {
            if (t == null || settings == null || !settings.snapToSurface)
                return false;
            Undo.RecordObject(t, undoName);
            return PcgSurfaceSnap.Snap(t, settings, ignore);
        }

        public static bool HasLiveTwin(Component self, long stamp)
        {
            foreach (IPcgSeeded o in PcgSeededRegistry.All)
                if (!ReferenceEquals(o, self) && o.PlacementStamp == stamp)
                    return true;
            return false;
        }

        public static int NewSeed()
        {
            int s = Guid.NewGuid().GetHashCode() & 0x7FFFFFFF;
            return s == 0 ? 1 : s;
        }
    }
}
#endif

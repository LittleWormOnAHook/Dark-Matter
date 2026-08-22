using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Hierarchy context menus to sort direct children (or selected siblings) by name, scene age, or bounds size.
    /// </summary>
    public static class HierarchySortUtility
    {
        private const int LargeChildConfirmThreshold = 100;
        private const string GameObjectMenuRoot = "GameObject/Dark Matter Genesis/Sort Children/";

        private enum SortMode
        {
            Name,
            Age,
            Size
        }

        [MenuItem(GameObjectMenuRoot + "By Name (A→Z)", false, 49)]
        [MenuItem(DarkMatterGenesisEditorMenus.HierarchySortChildren + "By Name (A→Z)", false, 100)]
        public static void SortByNameAscending() => SortSelection(SortMode.Name, ascending: true);

        [MenuItem(GameObjectMenuRoot + "By Name (Z→A)", false, 50)]
        [MenuItem(DarkMatterGenesisEditorMenus.HierarchySortChildren + "By Name (Z→A)", false, 101)]
        public static void SortByNameDescending() => SortSelection(SortMode.Name, ascending: false);

        [MenuItem(GameObjectMenuRoot + "By Scene Age (Oldest First)", false, 51)]
        [MenuItem(DarkMatterGenesisEditorMenus.HierarchySortChildren + "By Scene Age (Oldest First)", false, 102)]
        public static void SortByAgeAscending() => SortSelection(SortMode.Age, ascending: true);

        [MenuItem(GameObjectMenuRoot + "By Scene Age (Newest First)", false, 52)]
        [MenuItem(DarkMatterGenesisEditorMenus.HierarchySortChildren + "By Scene Age (Newest First)", false, 103)]
        public static void SortByAgeDescending() => SortSelection(SortMode.Age, ascending: false);

        [MenuItem(GameObjectMenuRoot + "By Size (Largest First)", false, 53)]
        [MenuItem(DarkMatterGenesisEditorMenus.HierarchySortChildren + "By Size (Largest First)", false, 104)]
        public static void SortBySizeDescending() => SortSelection(SortMode.Size, ascending: false);

        [MenuItem(GameObjectMenuRoot + "By Size (Smallest First)", false, 54)]
        [MenuItem(DarkMatterGenesisEditorMenus.HierarchySortChildren + "By Size (Smallest First)", false, 105)]
        public static void SortBySizeAscending() => SortSelection(SortMode.Size, ascending: true);

        [MenuItem(GameObjectMenuRoot + "By Name (A→Z)", true)]
        [MenuItem(GameObjectMenuRoot + "By Name (Z→A)", true)]
        [MenuItem(GameObjectMenuRoot + "By Scene Age (Oldest First)", true)]
        [MenuItem(GameObjectMenuRoot + "By Scene Age (Newest First)", true)]
        [MenuItem(GameObjectMenuRoot + "By Size (Largest First)", true)]
        [MenuItem(GameObjectMenuRoot + "By Size (Smallest First)", true)]
        [MenuItem(DarkMatterGenesisEditorMenus.HierarchySortChildren + "By Name (A→Z)", true)]
        [MenuItem(DarkMatterGenesisEditorMenus.HierarchySortChildren + "By Name (Z→A)", true)]
        [MenuItem(DarkMatterGenesisEditorMenus.HierarchySortChildren + "By Scene Age (Oldest First)", true)]
        [MenuItem(DarkMatterGenesisEditorMenus.HierarchySortChildren + "By Scene Age (Newest First)", true)]
        [MenuItem(DarkMatterGenesisEditorMenus.HierarchySortChildren + "By Size (Largest First)", true)]
        [MenuItem(DarkMatterGenesisEditorMenus.HierarchySortChildren + "By Size (Smallest First)", true)]
        public static bool ValidateSortMenus()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return false;

            return TryResolveSortTargets(out _, out List<Transform> targets) && targets != null && targets.Count >= 2;
        }

        private static void SortSelection(SortMode mode, bool ascending)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Hierarchy Sort",
                    "Sorting is only available in Edit Mode.",
                    "OK");
                return;
            }

            if (!TryResolveSortTargets(out Transform parent, out List<Transform> targets) || targets.Count < 2)
            {
                EditorUtility.DisplayDialog(
                    "Hierarchy Sort",
                    "Select a parent with at least two children, or select two or more sibling objects.",
                    "OK");
                return;
            }

            if (targets.Count >= LargeChildConfirmThreshold &&
                !EditorUtility.DisplayDialog(
                    "Hierarchy Sort",
                    $"Reorder {targets.Count} objects under '{parent.name}'?",
                    "Sort",
                    "Cancel"))
            {
                return;
            }

            SortChildren(parent, targets, mode, ascending);
        }

        private static bool TryResolveSortTargets(out Transform parent, out List<Transform> targets)
        {
            parent = null;
            targets = null;

            GameObject[] selection = Selection.gameObjects;
            if (selection == null || selection.Length == 0)
                return false;

            if (selection.Length == 1)
            {
                Transform selected = selection[0].transform;
                if (selected.childCount < 2)
                    return false;

                parent = selected;
                targets = new List<Transform>(selected.childCount);
                for (int i = 0; i < selected.childCount; i++)
                    targets.Add(selected.GetChild(i));
                return true;
            }

            Transform sharedParent = selection[0].transform.parent;
            if (sharedParent == null)
                return false;

            var siblingSet = new HashSet<Transform>();
            for (int i = 0; i < selection.Length; i++)
            {
                Transform t = selection[i].transform;
                if (t.parent != sharedParent)
                    return false;
                siblingSet.Add(t);
            }

            if (siblingSet.Count < 2)
                return false;

            parent = sharedParent;
            targets = new List<Transform>(siblingSet.Count);
            for (int i = 0; i < sharedParent.childCount; i++)
            {
                Transform child = sharedParent.GetChild(i);
                if (siblingSet.Contains(child))
                    targets.Add(child);
            }

            return targets.Count >= 2;
        }

        private static void SortChildren(
            Transform parent,
            List<Transform> targets,
            SortMode mode,
            bool ascending)
        {
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"Sort Children ({mode})");
            Undo.RegisterCompleteObjectUndo(parent, $"Sort Children ({mode})");

            // Snapshot full sibling list so non-selected siblings keep relative order.
            var allChildren = new List<Transform>(parent.childCount);
            for (int i = 0; i < parent.childCount; i++)
                allChildren.Add(parent.GetChild(i));

            var selectedSet = new HashSet<Transform>(targets);
            var selectedOrdered = new List<Transform>(targets);
            selectedOrdered.Sort(CreateComparer(mode, ascending));

            var finalOrder = new List<Transform>(allChildren.Count);
            int selectedCursor = 0;
            for (int i = 0; i < allChildren.Count; i++)
            {
                if (selectedSet.Contains(allChildren[i]))
                    finalOrder.Add(selectedOrdered[selectedCursor++]);
                else
                    finalOrder.Add(allChildren[i]);
            }

            for (int i = 0; i < finalOrder.Count; i++)
                finalOrder[i].SetSiblingIndex(i);

            Undo.CollapseUndoOperations(undoGroup);
            EditorUtility.SetDirty(parent.gameObject);
            if (!Application.isPlaying)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);
        }

        private static Comparison<Transform> CreateComparer(SortMode mode, bool ascending)
        {
            int direction = ascending ? 1 : -1;
            return (a, b) =>
            {
                int result = mode switch
                {
                    SortMode.Name => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase),
                    SortMode.Age => GetSceneAgeKey(a).CompareTo(GetSceneAgeKey(b)),
                    SortMode.Size => GetWorldBoundsVolume(a).CompareTo(GetWorldBoundsVolume(b)),
                    _ => 0
                };

                if (result == 0)
                    result = a.GetSiblingIndex().CompareTo(b.GetSiblingIndex());

                return result * direction;
            };
        }

        /// <summary>
        /// Lower local file ID ≈ older in the scene/prefab serialization order.
        /// Falls back to sibling index when the identifier is unavailable.
        /// </summary>
        private static long GetSceneAgeKey(Transform transform)
        {
            long id = TryGetLocalFileId(transform.gameObject);
            if (id != 0)
                return id;

            return transform.GetSiblingIndex();
        }

        private static long TryGetLocalFileId(UnityEngine.Object obj)
        {
            if (obj == null)
                return 0;

            // Scene/prefab local file ID ≈ creation order in the asset (not wall-clock age).
            GlobalObjectId gid = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            if (gid.identifierType != 0 && gid.targetObjectId != 0)
                return unchecked((long)gid.targetObjectId);

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out _, out long fileId))
                return fileId;

            return 0;
        }

        private static float GetWorldBoundsVolume(Transform transform)
        {
            Bounds? combined = null;

            Renderer[] renderers = transform.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                Bounds b = renderer.bounds;
                if (!IsFiniteBounds(b))
                    continue;

                combined = combined.HasValue ? Encapsulate(combined.Value, b) : b;
            }

            Collider[] colliders = transform.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                    continue;

                Bounds b = collider.bounds;
                if (!IsFiniteBounds(b))
                    continue;

                combined = combined.HasValue ? Encapsulate(combined.Value, b) : b;
            }

            if (!combined.HasValue)
                return 0f;

            Vector3 size = combined.Value.size;
            return Mathf.Abs(size.x * size.y * size.z);
        }

        private static bool IsFiniteBounds(Bounds bounds)
        {
            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;
            return IsFinite(c.x) && IsFinite(c.y) && IsFinite(c.z)
                   && IsFinite(e.x) && IsFinite(e.y) && IsFinite(e.z);
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private static Bounds Encapsulate(Bounds a, Bounds b)
        {
            a.Encapsulate(b);
            return a;
        }
    }
}

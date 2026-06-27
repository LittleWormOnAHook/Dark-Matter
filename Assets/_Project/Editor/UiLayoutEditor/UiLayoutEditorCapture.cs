using System.Collections.Generic;
using Project.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Project.EditorTools.UiLayout
{
    internal static class UiLayoutEditorCapture
    {
        private static readonly Dictionary<string, UiLayoutNodeEntry> PendingCaptures = new Dictionary<string, UiLayoutNodeEntry>();
        private static Transform pendingCaptureRoot;

        [InitializeOnLoadMethod]
        private static void RegisterPlayModeCaptureHandler()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredEditMode || PendingCaptures.Count == 0)
                    return;

                if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                EditorApplication.delayCall += FlushPendingCaptures;
            };
        }

        private static void FlushPendingCaptures()
        {
            if (PendingCaptures.Count == 0 || Application.isPlaying)
                return;

            Transform searchRoot = ResolveCaptureRoot();
            if (searchRoot == null)
            {
                PendingCaptures.Clear();
                pendingCaptureRoot = null;
                return;
            }

            foreach (KeyValuePair<string, UiLayoutNodeEntry> entry in PendingCaptures)
            {
                Transform target = string.IsNullOrEmpty(entry.Key)
                    ? searchRoot
                    : FindDeep(searchRoot, entry.Key);

                if (target is not RectTransform rect)
                    continue;

                Undo.RecordObject(rect, "Capture UI Layout");
                if (rect.TryGetComponent(out Graphic graphic))
                    Undo.RecordObject(graphic, "Capture UI Layout");

                if (entry.Value.hasLayoutElement && rect.TryGetComponent(out LayoutElement layoutElement))
                    Undo.RecordObject(layoutElement, "Capture UI Layout");

                if (entry.Value.hasGridLayout && rect.TryGetComponent(out GridLayoutGroup gridLayout))
                    Undo.RecordObject(gridLayout, "Capture UI Layout");

                if (entry.Value.hasButtonStyle && rect.TryGetComponent(out Button button))
                    Undo.RecordObject(button, "Capture UI Layout");

                if (entry.Value.hasTextStyle && rect.TryGetComponent(out TextMeshProUGUI text))
                    Undo.RecordObject(text, "Capture UI Layout");

                if (entry.Value.hasLegacyTextStyle && rect.TryGetComponent(out Text legacyText))
                    Undo.RecordObject(legacyText, "Capture UI Layout");

                UiLayoutProfileApplier.ApplyEntry(rect, entry.Value);
                EditorUtility.SetDirty(rect);
            }

            PendingCaptures.Clear();
            pendingCaptureRoot = null;
            UiStudioWindow.MarkSceneDirtyStatic();
            SceneView.RepaintAll();
        }

        private static Transform ResolveCaptureRoot()
        {
            if (pendingCaptureRoot != null)
            {
                try
                {
                    if (pendingCaptureRoot)
                        return pendingCaptureRoot;
                }
                catch (MissingReferenceException)
                {
                    pendingCaptureRoot = null;
                }
            }

            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            return canvas != null ? canvas.transform : null;
        }

        public static void CaptureRect(RectTransform selectedRect, Transform root)
        {
            if (selectedRect == null || root == null)
                return;

            pendingCaptureRoot = root;
            string path = GetRelativePath(selectedRect, root);
            PendingCaptures[path] = UiLayoutProfileApplier.CaptureNode(selectedRect, path);
        }

        public static void CaptureHierarchy(RectTransform root, Transform canvasRoot)
        {
            if (root == null || canvasRoot == null)
                return;

            CaptureRect(root, canvasRoot);
            for (int i = 0; i < root.childCount; i++)
            {
                if (root.GetChild(i) is RectTransform child)
                    CaptureHierarchy(child, canvasRoot);
            }
        }

        public static int PendingCount => PendingCaptures.Count;

        private static string GetRelativePath(Transform target, Transform root)
        {
            return UiLayoutProfileApplier.GetRelativePath(root, target);
        }

        private static Transform FindDeep(Transform parent, string path)
        {
            if (parent == null || string.IsNullOrEmpty(path))
                return null;

            string[] segments = path.Split('/');
            Transform current = parent;
            for (int i = 0; i < segments.Length; i++)
            {
                Transform next = current.Find(segments[i]);
                if (next == null)
                {
                    next = FindDeepChild(current, segments[i]);
                    if (next == null)
                        return null;
                }

                current = next;
            }

            return current;
        }

        private static Transform FindDeepChild(Transform parent, string objectName)
        {
            if (parent.name == objectName)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeepChild(parent.GetChild(i), objectName);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}

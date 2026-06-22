using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.EditorTools.UiLayout
{
    internal static class UiLayoutEditorCapture
    {
        internal sealed class CapturedRectTransform
        {
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
            public Vector2 Pivot;
            public Vector2 AnchoredPosition;
            public Vector2 SizeDelta;
            public Vector3 LocalScale;
            public bool ActiveSelf;
        }

        private static readonly Dictionary<string, CapturedRectTransform> PendingRectCaptures = new Dictionary<string, CapturedRectTransform>();

        [InitializeOnLoadMethod]
        private static void RegisterPlayModeCaptureHandler()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredEditMode || PendingRectCaptures.Count == 0)
                    return;

                Canvas canvas = Object.FindAnyObjectByType<Canvas>();
                if (canvas == null)
                {
                    PendingRectCaptures.Clear();
                    return;
                }

                foreach (KeyValuePair<string, CapturedRectTransform> entry in PendingRectCaptures)
                {
                    Transform target = string.IsNullOrEmpty(entry.Key)
                        ? canvas.transform
                        : FindDeep(canvas.transform, entry.Key);

                    if (target is not RectTransform rect)
                        continue;

                    CapturedRectTransform captured = entry.Value;
                    Undo.RecordObject(rect, "Capture UI Layout");
                    rect.gameObject.SetActive(captured.ActiveSelf);
                    rect.anchorMin = captured.AnchorMin;
                    rect.anchorMax = captured.AnchorMax;
                    rect.pivot = captured.Pivot;
                    rect.anchoredPosition = captured.AnchoredPosition;
                    rect.sizeDelta = captured.SizeDelta;
                    rect.localScale = captured.LocalScale;
                    EditorUtility.SetDirty(rect);
                }

                PendingRectCaptures.Clear();
                UiLayoutEditorWindow.MarkSceneDirtyStatic();
                SceneView.RepaintAll();
            };
        }

        public static void CaptureRect(RectTransform selectedRect, Transform root)
        {
            if (selectedRect == null || root == null)
                return;

            string path = GetRelativePath(selectedRect, root);
            PendingRectCaptures[path] = new CapturedRectTransform
            {
                AnchorMin = selectedRect.anchorMin,
                AnchorMax = selectedRect.anchorMax,
                Pivot = selectedRect.pivot,
                AnchoredPosition = selectedRect.anchoredPosition,
                SizeDelta = selectedRect.sizeDelta,
                LocalScale = selectedRect.localScale,
                ActiveSelf = selectedRect.gameObject.activeSelf
            };
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

        public static int PendingCount => PendingRectCaptures.Count;

        private static string GetRelativePath(Transform target, Transform root)
        {
            if (target == null || root == null || target == root)
                return string.Empty;

            List<string> parts = new List<string>();
            Transform current = target;
            while (current != null && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
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

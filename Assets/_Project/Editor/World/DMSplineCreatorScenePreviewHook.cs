#if UNITY_EDITOR
using System.Collections.Generic;
using Project.World;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>Draws edit-mode anchor / wire-hook previews for <see cref="DMSplineCreator"/> in the Scene view.</summary>
    [InitializeOnLoad]
    internal static class DMSplineCreatorScenePreviewHook
    {
        private static readonly List<DMSplineCreator> CachedCreators = new List<DMSplineCreator>(16);
        private static bool cacheDirty = true;

        static DMSplineCreatorScenePreviewHook()
        {
            SceneView.duringSceneGui += OnSceneGui;
            EditorApplication.hierarchyChanged += MarkCacheDirty;
            EditorApplication.projectChanged += MarkCacheDirty;
        }

        private static void MarkCacheDirty()
        {
            cacheDirty = true;
        }

        private static void EnsureCache()
        {
            if (!cacheDirty)
                return;

            CachedCreators.Clear();
            DMSplineCreator[] found = Object.FindObjectsByType<DMSplineCreator>(FindObjectsInactive.Exclude);
            if (found != null && found.Length > 0)
                CachedCreators.AddRange(found);
            cacheDirty = false;
        }

        private static void OnSceneGui(SceneView view)
        {
            if (Application.isPlaying)
                return;

            EnsureCache();
            for (int c = 0; c < CachedCreators.Count; c++)
            {
                DMSplineCreator creator = CachedCreators[c];
                if (creator == null || !creator.ShowEditModeAnchorPreview)
                    continue;

                DrawCreatorPreviews(creator, creator == Selection.activeGameObject);
            }
        }

        private static void DrawCreatorPreviews(DMSplineCreator creator, bool allowRadiusHandles)
        {
            int count = creator.PlacedAnchorCount;
            float pivotR = creator.EditPreviewPivotSphereRadius;
            float hookR = creator.EditPreviewWireHookSphereRadius;

            for (int i = 0; i < count; i++)
            {
                Transform anchor = creator.GetAnchorTransform(i);
                if (anchor == null)
                    continue;

                Color pivotColor = creator.GetAnchorColor(i);
                Vector3 pivot = anchor.position;
                Vector3 hook = creator.GetLineAttachWorldPosition(i);

                DMSplineCreatorEditPreviewDraw.DrawPivotSphere(pivot, pivotR, pivotColor);
                DMSplineCreatorEditPreviewDraw.DrawWireHookSphere(
                    hook,
                    hookR,
                    new Color(0.95f, 0.82f, 0.25f, 1f));

                Handles.color = new Color(0.85f, 0.85f, 0.85f, 0.65f);
                Handles.DrawLine(pivot, hook);
            }

            if (!allowRadiusHandles || count == 0)
                return;

            Transform first = creator.GetAnchorTransform(0);
            if (first == null)
                return;

            EditorGUI.BeginChangeCheck();
            float newPivotR = DMSplineCreatorEditPreviewDraw.RadiusHandle(
                first.position,
                pivotR,
                creator.GetAnchorColor(0));
            float newHookR = DMSplineCreatorEditPreviewDraw.RadiusHandle(
                creator.GetLineAttachWorldPosition(0),
                hookR,
                new Color(0.95f, 0.82f, 0.25f, 1f));
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(creator, "Resize Preview Spheres");
                creator.SetEditPreviewSphereRadii(newPivotR, newHookR);
                EditorUtility.SetDirty(creator);
            }
        }
    }
}
#endif

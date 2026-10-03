using Project.SurfaceCarve;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.SurfaceCarve
{
    [InitializeOnLoad]
    internal static class DMCarveEditorHooks
    {
        static DMCarveEditorHooks()
        {
            DMCarvable.EditorCreatedObject = o =>
            {
                if (o != null)
                    Undo.RegisterCreatedObjectUndo(o, "Surface Carve");
            };
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private static void OnUndoRedo()
        {
            foreach (DMCarvable c in Object.FindObjectsByType<DMCarvable>(FindObjectsInactive.Include))
                c.InvalidateCache();
            DMCarveToolWindow.ClearPickCache();
        }
    }
}

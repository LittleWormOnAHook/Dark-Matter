using Project.EditorTools;
using Project.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Rebuilds a clean OpticsOverlayCanvas in the open Pioneer scene for edit-mode hierarchy.
/// Overlay content stays inactive until OpticsController opens binoculars/scanner at runtime.
/// </summary>
public static class OpticsOverlaySceneSetup
{
    private const string CanvasName = "OpticsOverlayCanvas";

    [MenuItem(SurvivalPioneerEditorMenus.Optics + "Rebuild Scene Optics Overlay Canvas")]
    public static void RebuildSceneCanvasFromMenu()
    {
        int result = RebuildSceneCanvas();
        EditorUtility.DisplayDialog(
            "Optics Overlay Canvas",
            result >= 0
                ? "Rebuilt OpticsOverlayCanvas in the open scene (hidden until optics open)."
                : "No open scene to rebuild into.",
            "OK");
    }

    /// <returns>1 if rebuilt, 0 if skipped, -1 on failure.</returns>
    public static int RebuildSceneCanvas()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            return -1;

        OpticsUiSprites.ResetCache();
        OpticsOverlayUI.ResetRuntimeState();

        // Remove any existing optics canvases (scene leftovers / duplicates).
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas.gameObject.name != CanvasName)
                continue;

            Undo.DestroyObjectImmediate(canvas.gameObject);
        }

        // Build via the same runtime path, then keep the result in the scene.
        OpticsOverlayUI ui = OpticsOverlayUI.EnsureExists();
        if (ui == null)
            return -1;

        Transform canvasTransform = ui.transform;
        while (canvasTransform != null && canvasTransform.name != CanvasName)
            canvasTransform = canvasTransform.parent;

        if (canvasTransform == null)
            return -1;

        // Ensure visual root is present and hidden for edit mode.
        Transform overlay = canvasTransform.Find("OpticsOverlay");
        if (overlay == null)
        {
            ui.EnsureBuilt();
            overlay = canvasTransform.Find("OpticsOverlay");
        }

        if (overlay != null)
        {
            overlay.gameObject.SetActive(false);

            Transform binocular = overlay.Find("BinocularOverlay");
            if (binocular != null)
                binocular.gameObject.SetActive(false);

            Transform scanner = overlay.Find("ScannerOverlay");
            if (scanner != null)
                scanner.gameObject.SetActive(false);

            Transform markers = overlay.Find("ScannerMarkers");
            if (markers != null)
                markers.gameObject.SetActive(false);

            // Clear any leftover RT binding from play-mode tests.
            RawImage viewport = overlay.Find("OpticsViewport")?.GetComponent<RawImage>();
            if (viewport != null)
            {
                viewport.texture = null;
                viewport.enabled = true;
            }
        }

        // Canvas itself stays active so Hierarchy shows it; only OpticsOverlay is hidden.
        canvasTransform.gameObject.SetActive(true);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        OpticsOverlayUI.ResetRuntimeState();
        return 1;
    }
}

using Project.EditorTools;
using Project.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project.EditorTools.UiLayout
{
    internal static class UiStudioPreviewHost
    {
        public static Scene EnsurePreviewSceneLoaded()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ProjectAssetPaths.UiPreviewScene)
                return active;

            if (!System.IO.File.Exists(ProjectAssetPaths.UiPreviewScene))
                UiPreviewSceneSetup.CreatePreviewSceneAsset();

            return EditorSceneManager.OpenScene(ProjectAssetPaths.UiPreviewScene, OpenSceneMode.Single);
        }

        public static Canvas EnsurePreviewCanvas()
        {
            EnsurePreviewSceneLoaded();
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
                return canvas;

            return UiPreviewSceneSetup.CreatePreviewCanvasInActiveScene();
        }

        public static Canvas RebuildSandbox(string panelId, out Transform panelRoot)
        {
            panelRoot = null;
            Canvas canvas = EnsurePreviewCanvas();
            if (canvas == null)
                return null;

            TeardownSandbox(canvas.transform);

            IUiPreviewSurface surface = ResolvePreviewSurface(canvas, panelId);
            if (surface == null)
                return canvas;

            surface.BuildPreview(canvas.transform);
            panelRoot = surface.GetPreviewPanelRoot();

            if (panelRoot != null)
            {
                UiLayoutProfile profile = UiStudioProfileIO.LoadProfile(panelId);
                if (profile != null)
                    UiLayoutProfileApplier.Apply(panelRoot, profile);

                ApplyThemeToHierarchy(canvas.transform);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            return canvas;
        }

        private static IUiPreviewSurface ResolvePreviewSurface(Canvas canvas, string panelId)
        {
            if (panelId == UiPanelIds.InventoryPanel)
            {
                InventoryPanelPreviewHost host = canvas.GetComponent<InventoryPanelPreviewHost>();
                if (host == null)
                    host = canvas.gameObject.AddComponent<InventoryPanelPreviewHost>();

                GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.InventorySlotPrefab);
                if (slotPrefab == null)
                    slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.InventorySlotResourcesPrefab);

                SerializedObject so = new SerializedObject(host);
                so.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
                return host;
            }

            return null;
        }

        public static void TeardownSandbox(Transform canvasRoot)
        {
            if (canvasRoot == null)
                return;

            IUiPreviewSurface[] surfaces = canvasRoot.GetComponents<IUiPreviewSurface>();
            for (int i = 0; i < surfaces.Length; i++)
                surfaces[i]?.TeardownPreview();

            Transform inventory = canvasRoot.Find("InventoryPanel");
            if (inventory != null)
                Object.DestroyImmediate(inventory.gameObject);
        }

        public static void ApplyThemeToHierarchy(Transform root)
        {
            ShiftUiTheme theme = AssetDatabase.LoadAssetAtPath<ShiftUiTheme>(UiStudioCatalog.ShiftUiThemePath);
            if (theme == null || root == null)
                return;

            ShiftUiTheme.ResetSharedCache();
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (image == null)
                    continue;

                if (image.gameObject.name.Contains("Panel"))
                    theme.ApplyPanelImage(image, large: true);
            }
        }

        public static bool TrySyncPlayModeCanvas(out Canvas playCanvas)
        {
            playCanvas = null;
            if (!EditorApplication.isPlaying)
                return false;

            playCanvas = Object.FindAnyObjectByType<Canvas>();
            return playCanvas != null;
        }
    }
}

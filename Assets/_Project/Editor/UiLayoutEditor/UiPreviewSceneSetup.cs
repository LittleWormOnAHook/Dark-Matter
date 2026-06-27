using Project.EditorTools;
using Project.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project.EditorTools.UiLayout
{
    /// <summary>
    /// Creates and opens the UI Studio preview scene (sandbox editing, no gameplay bootstraps).
    /// </summary>
    public static class UiPreviewSceneSetup
    {
        [MenuItem(SurvivalPioneerEditorMenus.Ui + "Create / Open UI Preview Scene")]
        public static void CreateOrOpenPreviewScene()
        {
            if (!System.IO.File.Exists(ProjectAssetPaths.UiPreviewScene))
                CreatePreviewSceneAsset();
            else
                EditorSceneManager.OpenScene(ProjectAssetPaths.UiPreviewScene, OpenSceneMode.Single);
        }

        public static void CreatePreviewSceneAsset()
        {
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scenes);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Data + "/UI");
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.UiLayoutProfiles);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            CreatePreviewCanvasInActiveScene();
            EnsureEventSystem();

            EditorSceneManager.SaveScene(scene, ProjectAssetPaths.UiPreviewScene);
            AssetDatabase.Refresh();
            Debug.Log($"[UI Studio] Created preview scene at {ProjectAssetPaths.UiPreviewScene}");
        }

        public static Canvas CreatePreviewCanvasInActiveScene()
        {
            GameObject existing = GameObject.Find("MainCanvas");
            if (existing != null && existing.TryGetComponent(out Canvas existingCanvas))
                return existingCanvas;

            GameObject canvasGo = new GameObject("MainCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rect = canvasGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            EnsureEventSystem();
            InventoryPanelPreviewHost previewHost = canvasGo.AddComponent<InventoryPanelPreviewHost>();
            GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.InventorySlotPrefab);
            if (slotPrefab == null)
                slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.InventorySlotResourcesPrefab);

            SerializedObject so = new SerializedObject(previewHost);
            so.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            return canvas;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}

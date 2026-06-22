using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Project.UI;

namespace Project.EditorTools
{
    public static class ShiftUiSetup
    {
        private const string ShiftRoot = "Assets/Shift - Complete Sci-Fi UI";

        [MenuItem(SurvivalPioneerEditorMenus.Ui + "Setup Shift UI Theme")]
        public static void SetupShiftUiTheme()
        {
            EnsureThemeAsset();
            ApplyInventorySlotPrefab();
            ApplyPioneerScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ShiftUiTheme.ResetSharedCache();
            Debug.Log("Shift UI theme setup complete (survival stats panel unchanged).");
        }

        private static ShiftUiTheme EnsureThemeAsset()
        {
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.ResourcesUi);

            ShiftUiTheme theme = AssetDatabase.LoadAssetAtPath<ShiftUiTheme>(ShiftUiTheme.AssetPath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<ShiftUiTheme>();
                AssetDatabase.CreateAsset(theme, ShiftUiTheme.AssetPath);
            }

            theme.regularFont = LoadAsset<TMP_FontAsset>($"{ShiftRoot}/Fonts/Rajdhani-Regular SDF.asset");
            theme.semiBoldFont = LoadAsset<TMP_FontAsset>($"{ShiftRoot}/Fonts/Rajdhani-SemiBold SDF.asset");
            theme.boldFont = LoadAsset<TMP_FontAsset>($"{ShiftRoot}/Fonts/Rajdhani-Bold SDF.asset");
            theme.panelFrame = LoadSprite($"{ShiftRoot}/Textures/Border/Cut/Cut Frame Filled.png");
            theme.panelFrameBig = LoadSprite($"{ShiftRoot}/Textures/Border/Cut/Cut Frame Filled Big.png");
            theme.circleGlow = LoadSprite($"{ShiftRoot}/Textures/Glow & Shadow/Circle Filled Glow.png");
            theme.squareGlow = LoadSprite($"{ShiftRoot}/Textures/Border/Square/Filled Square Glow.png");
            theme.circleFilled = LoadSprite($"{ShiftRoot}/Textures/Border/Circle/Circle Filled.png");
            theme.circleOutline = LoadSprite($"{ShiftRoot}/Textures/Border/Circle/Circle Outline - Stroke 20px.png");
            theme.keyCapFrame = LoadSprite($"{ShiftRoot}/Textures/Border/Cut/Cut Frame - 3px.png");

            EditorUtility.SetDirty(theme);
            return theme;
        }

        private static void ApplyInventorySlotPrefab()
        {
            ShiftUiTheme theme = AssetDatabase.LoadAssetAtPath<ShiftUiTheme>(ShiftUiTheme.AssetPath);
            if (theme == null)
                return;

            string[] paths =
            {
                ProjectAssetPaths.InventorySlotPrefab,
                ProjectAssetPaths.InventorySlotResourcesPrefab,
            };

            foreach (string path in paths)
            {
                GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefabRoot == null)
                    continue;

                Image image = prefabRoot.GetComponent<Image>();
                if (image != null)
                    theme.ApplySlotFrame(image);

                EditorUtility.SetDirty(prefabRoot);
            }
        }

        private static void ApplyPioneerScene()
        {
            ShiftUiTheme theme = AssetDatabase.LoadAssetAtPath<ShiftUiTheme>(ShiftUiTheme.AssetPath);
            if (theme == null)
                return;

            GameObject mainCanvas = GameObject.Find("MainCanvas");
            if (mainCanvas == null)
                return;

            if (mainCanvas.GetComponent<ShiftHudBootstrap>() == null)
                mainCanvas.AddComponent<ShiftHudBootstrap>();

            ApplyPanelImage(mainCanvas.transform, "Hotbar", theme, large: false);
            ApplyPanelImage(mainCanvas.transform, "InventoryPanel", theme, large: true);
        }

        private static void ApplyPanelImage(Transform canvasRoot, string panelName, ShiftUiTheme theme, bool large)
        {
            Transform panel = canvasRoot.Find(panelName);
            if (panel == null)
                return;

            Image image = panel.GetComponent<Image>();
            if (image == null)
                return;

            theme.ApplyPanelImage(image, large, alphaMultiplier: large ? 1f : 0.92f);
            EditorUtility.SetDirty(panel.gameObject);
        }

        private static T LoadAsset<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}

using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class ControlsMenuContentSetup
    {
        private const string ImagesRoot = "Assets/_Project/UI/Controls/Images";
        private const string ResourcesRoot = "Assets/_Project/Resources/UI/Controls";
        private const string KeyboardImagePath = ImagesRoot + "/KeyboardMouse/Controls_KBM_Overview.png";
        private const string GamepadImagePath = ImagesRoot + "/Controller/Controls_Gamepad_Overview.png";
        private const string KeyboardSchemePath = ResourcesRoot + "/ControlsScheme_KeyboardMouse.asset";
        private const string GamepadSchemePath = ResourcesRoot + "/ControlsScheme_Gamepad.asset";

        [MenuItem(DarkMatterGenesisEditorMenus.Ui + "Create Controls Menu Content", false, 10)]
        public static void CreateControlsMenuContent()
        {
            EnsureFolder("Assets/_Project/UI/Controls");
            EnsureFolder(ImagesRoot + "/KeyboardMouse");
            EnsureFolder(ImagesRoot + "/Controller");
            EnsureFolder("Assets/_Project/Resources/UI");
            EnsureFolder(ResourcesRoot);

            Sprite keyboardSprite = ImportSprite(KeyboardImagePath);
            Sprite gamepadSprite = ImportSprite(GamepadImagePath);

            ControlsSchemeDefinition keyboardScheme = LoadOrCreateScheme(KeyboardSchemePath, "Keyboard and Mouse");
            SetSchemePages(
                keyboardScheme,
                keyboardSprite,
                "InputSystem_Actions — WASD move · mouse look · LMB attack · RMB block · E interact · Shift sprint · Space jump · Ctrl crouch · 1/2 hotbar · Tab switch weapon · I inventory · M map · J journal · C craft · B blueprints · P pioneers · K pets · U character · L echoes · Esc menu/pause");

            ControlsSchemeDefinition gamepadScheme = LoadOrCreateScheme(GamepadSchemePath, "Controller");
            SetSchemePages(
                gamepadScheme,
                gamepadSprite,
                "Controller support is in progress. Current bindings: left stick move · right stick look · West (X) attack · North (Y) interact · East (B) crouch · South (A) jump · left stick press sprint · D-pad left/right hotbar. Block, weapon switch, and several UI panels are keyboard-only for now.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Controls menu content ready under Resources/UI/Controls.");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static Sprite ImportSprite(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Controls setup: missing image at {assetPath}. Add the PNG, then re-run this menu.");
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static ControlsSchemeDefinition LoadOrCreateScheme(string path, string title)
        {
            ControlsSchemeDefinition existing = AssetDatabase.LoadAssetAtPath<ControlsSchemeDefinition>(path);
            if (existing != null)
            {
                SerializedObject so = new SerializedObject(existing);
                so.FindProperty("schemeTitle").stringValue = title;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(existing);
                return existing;
            }

            ControlsSchemeDefinition created = ScriptableObject.CreateInstance<ControlsSchemeDefinition>();
            SerializedObject createdSo = new SerializedObject(created);
            createdSo.FindProperty("schemeTitle").stringValue = title;
            createdSo.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void SetSchemePages(ControlsSchemeDefinition scheme, Sprite sprite, string caption)
        {
            if (scheme == null)
                return;

            SerializedObject so = new SerializedObject(scheme);
            SerializedProperty pages = so.FindProperty("pages");
            pages.arraySize = 1;
            SerializedProperty page = pages.GetArrayElementAtIndex(0);
            page.FindPropertyRelative("image").objectReferenceValue = sprite;
            page.FindPropertyRelative("caption").stringValue = caption;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(scheme);
        }
    }
}

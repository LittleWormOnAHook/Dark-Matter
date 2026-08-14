using Project.EditorTools;
using Project.UI;
using UnityEditor;
using UnityEngine;

public static class OpticsCrosshairLibrarySetup
{
    private const string AssetPath = "Assets/_Project/Resources/Optics/OpticsCrosshairLibrary.asset";
    private const string CrosshairRoot = "Assets/TooManyCrosshairs/Unknown's Crosshairs";

    [MenuItem(DarkMatterGenesisEditorMenus.Optics + "Setup Crosshair Library")]
    public static void CreateOrUpdateLibrary()
    {
        EnsureFolder("Assets/_Project/Resources");
        EnsureFolder("Assets/_Project/Resources/Optics");

        OpticsCrosshairLibrary library = AssetDatabase.LoadAssetAtPath<OpticsCrosshairLibrary>(AssetPath);
        if (library == null && System.IO.File.Exists(AssetPath))
        {
            Debug.LogWarning($"Optics crosshair library at {AssetPath} could not be loaded. Recreating asset.");
            AssetDatabase.DeleteAsset(AssetPath);
        }

        if (library == null)
        {
            library = ScriptableObject.CreateInstance<OpticsCrosshairLibrary>();
            AssetDatabase.CreateAsset(library, AssetPath);
        }

        library.binocularScopeFull = LoadTexture("Assets/TooManyCrosshairs/128px/Base/Triangle/Triangle3Split128.png");
        library.binocularScopeInnerGlow = LoadTexture("Assets/TooManyCrosshairs/Unknown's Crosshairs/Scopes/UnknownsMarksmanInnerGlow2048.png");
        library.binocularScopeOuter = LoadTexture("Assets/TooManyCrosshairs/Unknown's Crosshairs/Scopes/Unknowns6xFull2048.png");
        library.scannerHolographic = LoadTexture("Assets/TooManyCrosshairs/2048px/Optics/WW2Tank2048.png");
        library.scannerHolographicGlow = LoadTexture("Assets/TooManyCrosshairs/128px/BaseDot/xhairDot/xHairHexDot128.png");
        library.scannerRectMask = LoadTexture("Assets/Shift - Complete Sci-Fi UI/Textures/Border/Square/Outline - Stroke 24x.png");
        library.scannerMaskFrameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Shift - Complete Sci-Fi UI/Textures/Border/Square/Outline - Stroke 24x.png");
        library.viewportBackgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/UGUIKit Flat/Content/Source/Icons/160 Desktop.png");
        library.viewportMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/_Project/Misc Tools and Shaders/ScanlinesPostProcess.mat");

        library.ResetPresentationDefaults();

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Optics crosshair library saved to {AssetPath}");
    }

    [InitializeOnLoadMethod]
    private static void EnsureLibraryOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadAssetAtPath<OpticsCrosshairLibrary>(AssetPath) == null)
                CreateOrUpdateLibrary();
        };
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
            AssetDatabase.CreateFolder(parent, folderName);
    }

    private static Texture2D LoadTexture(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}

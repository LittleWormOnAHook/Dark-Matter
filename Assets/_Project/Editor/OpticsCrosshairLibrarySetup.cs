using Project.EditorTools;
using Project.UI;
using UnityEditor;
using UnityEngine;

public static class OpticsCrosshairLibrarySetup
{
    private const string AssetPath = "Assets/_Project/Resources/Optics/OpticsCrosshairLibrary.asset";
    private const string CrosshairRoot = "Assets/TooManyCrosshairs/Unknown's Crosshairs";

    [MenuItem(SurvivalPioneerEditorMenus.Optics + "Setup Crosshair Library")]
    public static void CreateOrUpdateLibrary()
    {
        EnsureFolder("Assets/_Project/Resources");
        EnsureFolder("Assets/_Project/Resources/Optics");

        OpticsCrosshairLibrary library = AssetDatabase.LoadAssetAtPath<OpticsCrosshairLibrary>(AssetPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<OpticsCrosshairLibrary>();
            AssetDatabase.CreateAsset(library, AssetPath);
        }

        library.binocularScopeFull = LoadTexture($"{CrosshairRoot}/Scopes/UnknownsMarksmanFull2048.png");
        library.binocularScopeInnerGlow = LoadTexture($"{CrosshairRoot}/Scopes/UnknownsMarksmanInnerGlow2048.png");
        library.binocularScopeOuter = LoadTexture($"{CrosshairRoot}/Scopes/UnknownsMarksmanOuter2048.png");
        library.scannerHolographic = LoadTexture($"{CrosshairRoot}/Sights/UnknownsHolographic2048.png");
        library.scannerHolographicGlow = LoadTexture($"{CrosshairRoot}/Sights/UnknownsHolographicGlow2048.png");
        library.scannerRectMask = LoadTexture($"{CrosshairRoot}/Masks/MaskHolographic.png");

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

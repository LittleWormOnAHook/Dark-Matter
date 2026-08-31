using System.IO;
using Project.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.EditorTools
{
    /// <summary>
    /// Writes PanelSettings + config under Assets/UI Toolkit when missing.
    /// Does not overwrite UnityDefaultRuntimeTheme or Anthony's existing PanelSettings.asset.
    /// </summary>
    public static class DMUiToolkitShellMenu
    {
        private const string MenuPathTools = "Tools/Dark Matter Genesis/UI/Create Toolkit Shell";
        private const string MenuPathRoot = "Dark Matter Genesis/UI/Create Toolkit Shell";
        private const string ToolkitFolder = "Assets/UI Toolkit";
        private const string ResourcesFolder = ToolkitFolder + "/Resources";
        private const string ConfigAssetPath = ResourcesFolder + "/DMUiToolkitConfig.asset";
        private const string LoadingPanelSettingsPath = ToolkitFolder + "/LoadingPanelSettings.asset";

        [MenuItem(MenuPathTools, false, 2100)]
        [MenuItem(MenuPathRoot, false, 2100)]
        public static void CreateToolkitShell()
        {
            Directory.CreateDirectory(ToFull(ToolkitFolder + "/Themes"));
            Directory.CreateDirectory(ToFull(ToolkitFolder + "/Screens"));
            Directory.CreateDirectory(ToFull(ToolkitFolder + "/Runtime"));
            Directory.CreateDirectory(ToFull(ToolkitFolder + "/Editor"));
            Directory.CreateDirectory(ToFull(ResourcesFolder));
            AssetDatabase.Refresh();

            EnsureLoadingPanelSettings();
            EnsureConfigAsset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(DMUiToolkitConfig.LogStamp + " Create Toolkit Shell — PanelSettings + config ready under Assets/UI Toolkit. UITK_Root is spawned at Play (sibling of MainCanvas). Disable config.enabled or UITK_Root for uGUI-only.");
        }

        private static void EnsureLoadingPanelSettings()
        {
            PanelSettings existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(LoadingPanelSettingsPath);
            if (existing != null)
            {
                existing.sortingOrder = DMUiToolkitBootstrap.LoadingSortingOrder;
                existing.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                existing.referenceResolution = new Vector2Int(1920, 1080);
                existing.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                existing.match = 0.5f;
                EditorUtility.SetDirty(existing);
                return;
            }

            PanelSettings settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.sortingOrder = DMUiToolkitBootstrap.LoadingSortingOrder;
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;

            ThemeStyleSheet theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(DMUiToolkitBootstrap.DefaultThemePath);
            if (theme != null)
                settings.themeStyleSheet = theme;

            AssetDatabase.CreateAsset(settings, LoadingPanelSettingsPath);
            Debug.Log(DMUiToolkitConfig.LogStamp + " wrote " + LoadingPanelSettingsPath);
        }

        private static void EnsureConfigAsset()
        {
            DMUiToolkitConfig existing = AssetDatabase.LoadAssetAtPath<DMUiToolkitConfig>(ConfigAssetPath);
            if (existing != null)
                return;

            DMUiToolkitConfig config = ScriptableObject.CreateInstance<DMUiToolkitConfig>();
            config.enabled = true;
            AssetDatabase.CreateAsset(config, ConfigAssetPath);
            Debug.Log(DMUiToolkitConfig.LogStamp + " wrote " + ConfigAssetPath + " (enabled=true)");
        }

        private static string ToFull(string assetPath)
        {
            string project = Directory.GetParent(Application.dataPath)?.FullName ?? "";
            return Path.GetFullPath(Path.Combine(project, assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}

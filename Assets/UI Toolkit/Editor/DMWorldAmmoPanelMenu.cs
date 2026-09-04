using System.IO;
using Project.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.EditorTools
{
    /// <summary>
    /// One-shot: writes WorldAmmoPanelSettings.asset (WorldSpace) if missing.
    /// Does not touch Assets/UI Toolkit/PanelSettings.asset (shell/HUD overlay).
    /// </summary>
    public static class DMWorldAmmoPanelMenu
    {
        private const string MenuPathTools = "Tools/Dark Matter Genesis/UI/Create World Ammo Panel Settings";
        private const string MenuPathRoot = "Dark Matter Genesis/UI/Create World Ammo Panel Settings";
        private const string PanelPath = DMWorldAmmoHud.PanelSettingsPath;

        [MenuItem(MenuPathTools, false, 2105)]
        [MenuItem(MenuPathRoot, false, 2105)]
        public static void CreateWorldAmmoPanelSettings()
        {
            EnsureWorldAmmoPanelSettings(forceRefresh: true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(DMWorldAmmoHud.LogStamp + " WorldAmmo PanelSettings ready at " + PanelPath);
        }

        [InitializeOnLoadMethod]
        private static void BootstrapEnsure()
        {
            // Quiet ensure on domain reload - create asset only if missing.
            EditorApplication.delayCall += () => EnsureWorldAmmoPanelSettings(forceRefresh: false);
        }

        public static PanelSettings EnsureWorldAmmoPanelSettings(bool forceRefresh)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ToFull(PanelPath)) ?? "Assets/UI Toolkit");

            PanelSettings existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (existing != null)
            {
                ApplyWorldSettings(existing);
                if (forceRefresh)
                    EditorUtility.SetDirty(existing);
                return existing;
            }

            PanelSettings settings = ScriptableObject.CreateInstance<PanelSettings>();
            ApplyWorldSettings(settings);

            ThemeStyleSheet theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(DMUiToolkitBootstrap.DefaultThemePath);
            if (theme != null)
                settings.themeStyleSheet = theme;

            AssetDatabase.CreateAsset(settings, PanelPath);
            Debug.Log(DMWorldAmmoHud.LogStamp + " wrote " + PanelPath);
            return settings;
        }

        private static void ApplyWorldSettings(PanelSettings settings)
        {
            if (settings == null)
                return;

            settings.renderMode = PanelRenderMode.WorldSpace;
            settings.clearColor = true;
            settings.colorClearValue = Color.clear;
            settings.sortingOrder = 200;
            settings.forceGammaRendering = true;
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            settings.referenceResolution = new Vector2Int(180, 80);
        }

        private static string ToFull(string assetPath)
        {
            string project = Directory.GetParent(Application.dataPath)?.FullName ?? "";
            return Path.GetFullPath(Path.Combine(project, assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}

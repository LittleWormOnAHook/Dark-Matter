using System;
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.UiLayout
{
    internal enum UiStudioBrowserTab
    {
        Panels,
        Prefabs,
        Scriptables
    }

    internal enum UiStudioPreviewSource
    {
        Scene,
        Sandbox,
        PlaySync
    }

    internal sealed class UiStudioScriptableEntry
    {
        public string Label;
        public string Category;
        public string AssetPath;
        public Type AssetType;
        public string Description;
    }

    /// <summary>
    /// UI Studio catalog: panel ids, profile paths, scriptable assets, sandbox preview routing.
    /// </summary>
    internal static class UiStudioCatalog
    {
        public const string ShiftUiThemePath = "Assets/_Project/Resources/UI/ShiftUiTheme.asset";

        public static readonly UiStudioScriptableEntry[] Scriptables =
        {
            new UiStudioScriptableEntry
            {
                Label = "Shift UI Theme",
                Category = "Theme",
                AssetPath = ShiftUiThemePath,
                AssetType = typeof(ShiftUiTheme),
                Description = "Panel colors, fonts, and button styling applied across runtime UI."
            },
            new UiStudioScriptableEntry
            {
                Label = "Optics Crosshair Library",
                Category = "HUD",
                AssetPath = "Assets/_Project/Resources/Optics/OpticsCrosshairLibrary.asset",
                AssetType = typeof(ScriptableObject),
                Description = "Crosshair sprites for scoped weapons."
            }
        };

        public static UiPanelDefinition FindPanelById(string panelId)
        {
            if (string.IsNullOrEmpty(panelId))
                return null;

            for (int i = 0; i < UiLayoutEditorPanelRegistry.Panels.Length; i++)
            {
                UiPanelDefinition panel = UiLayoutEditorPanelRegistry.Panels[i];
                if (panel != null && panel.PanelId == panelId)
                    return panel;
            }

            return null;
        }

        public static bool SupportsSandboxPreview(string panelId)
        {
            return panelId == UiPanelIds.InventoryPanel;
        }

        public static string GetProfilePath(string panelId)
        {
            return UiLayoutProfileResolver.GetAssetPath(panelId);
        }
    }
}

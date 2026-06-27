using Project.EditorTools;
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.UiLayout
{
    internal static class UiStudioProfileIO
    {
        public static UiLayoutProfile GetOrCreateProfile(string panelId)
        {
            if (string.IsNullOrEmpty(panelId))
                return null;

            string path = UiLayoutProfileResolver.GetAssetPath(panelId);
            UiLayoutProfile profile = AssetDatabase.LoadAssetAtPath<UiLayoutProfile>(path);
            if (profile != null)
                return profile;

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.Scenes);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.UiLayoutProfiles);
            profile = ScriptableObject.CreateInstance<UiLayoutProfile>();
            profile.panelId = panelId;
            profile.name = $"UiLayoutProfile_{panelId}";
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            return profile;
        }

        public static void SaveFromPanelRoot(Transform panelRoot, string panelId, bool includeHierarchy)
        {
            if (panelRoot == null || string.IsNullOrEmpty(panelId))
                return;

            InventoryUI inventoryUi = panelRoot.GetComponentInParent<InventoryUI>();
            if (panelId == UiPanelIds.InventoryPanel && inventoryUi != null && inventoryUi.IsInventoryEmbedded)
            {
                Debug.LogWarning(
                    "[UI Studio] Skipped saving inventory layout profile while panel is embedded in the journal. " +
                    "Close the journal inventory tab and capture the standalone Inventory Panel instead.");
                return;
            }

            UiLayoutProfile profile = GetOrCreateProfile(panelId);
            Undo.RecordObject(profile, "Save UI Layout Profile");
            UiLayoutProfileApplier.Capture(panelRoot, profile, includeHierarchy);
            profile.panelId = panelId;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        public static void ApplyToPanelRoot(Transform panelRoot, UiLayoutProfile profile)
        {
            if (panelRoot == null || profile == null)
                return;

            Undo.RecordObject(panelRoot, "Apply UI Layout Profile");
            UiLayoutProfileApplier.Apply(panelRoot, profile);
            EditorUtility.SetDirty(panelRoot);
        }

        public static UiLayoutProfile LoadProfile(string panelId)
        {
            return AssetDatabase.LoadAssetAtPath<UiLayoutProfile>(UiLayoutProfileResolver.GetAssetPath(panelId));
        }
    }
}

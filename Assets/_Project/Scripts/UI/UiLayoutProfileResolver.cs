using UnityEngine;

namespace Project.UI
{
    public static class UiLayoutProfileResolver
    {
        public const string LayoutProfilesFolder = "Assets/_Project/Data/UI/LayoutProfiles";

        public static UiLayoutProfile Load(string panelId)
        {
            if (string.IsNullOrEmpty(panelId))
                return null;

#if UNITY_EDITOR
            string path = GetAssetPath(panelId);
            UiLayoutProfile profile = UnityEditor.AssetDatabase.LoadAssetAtPath<UiLayoutProfile>(path);
            if (profile != null)
                return profile;
#endif

            return Resources.Load<UiLayoutProfile>($"UI/LayoutProfiles/{panelId}");
        }

        public static string GetAssetPath(string panelId)
        {
            return $"{LayoutProfilesFolder}/UiLayoutProfile_{panelId}.asset";
        }
    }
}

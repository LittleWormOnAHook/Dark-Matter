using System.Collections.Generic;
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.UiLayout
{
    public static class UiLayoutProfileSanitizeUtility
    {
        [MenuItem(SurvivalPioneerEditorMenus.Ui + "Sanitize Layout Profiles")]
        public static void SanitizeAllProfiles()
        {
            string[] guids = AssetDatabase.FindAssets("t:UiLayoutProfile", new[] { UiLayoutProfileResolver.LayoutProfilesFolder });
            int sanitized = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                UiLayoutProfile profile = AssetDatabase.LoadAssetAtPath<UiLayoutProfile>(path);
                if (profile == null || profile.nodes == null)
                    continue;

                int before = profile.nodes.Count;
                SanitizeProfile(profile);
                if (profile.nodes.Count != before)
                {
                    EditorUtility.SetDirty(profile);
                    sanitized++;
                }
            }

            if (sanitized > 0)
                AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "UI Studio",
                sanitized > 0
                    ? $"Sanitized {sanitized} layout profile(s). Removed runtime slot clones and invalid nodes."
                    : "All layout profiles are already clean.",
                "OK");
        }

        public static void SanitizeProfile(UiLayoutProfile profile)
        {
            if (profile?.nodes == null)
                return;

            List<UiLayoutNodeEntry> kept = new List<UiLayoutNodeEntry>(profile.nodes.Count);
            for (int i = 0; i < profile.nodes.Count; i++)
            {
                UiLayoutNodeEntry node = profile.nodes[i];
                if (node != null && UiLayoutCaptureRules.ShouldApplyNode(node))
                    kept.Add(node);
            }

            if (kept.Count > 0 && kept[0].relativePath == string.Empty && !kept[0].activeSelf)
                kept[0].activeSelf = true;

            profile.nodes = kept;
        }
    }
}

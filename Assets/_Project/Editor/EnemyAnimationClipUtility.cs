using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class EnemyAnimationClipUtility
    {
        public static AnimationClip LoadEmbeddedAnimationClip(string assetPath, string preferredClipName = null)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null || assets.Length == 0)
                return null;

            AnimationClip fallback = null;
            AnimationClip mixamoClip = null;

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is not AnimationClip clip || clip.name.StartsWith("__preview__"))
                    continue;

                if (!string.IsNullOrWhiteSpace(preferredClipName) && clip.name == preferredClipName)
                    return clip;

                if (clip.name == "mixamo.com")
                    mixamoClip = clip;

                fallback ??= clip;
            }

            return mixamoClip != null ? mixamoClip : fallback;
        }
    }
}

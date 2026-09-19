using UnityEngine;

namespace Project.Player
{
    /// <summary>Resolves the active Player_v7 root (Variant preferred when it is in the hierarchy).</summary>
    internal static class DMPlayerRuntimeRoot
    {
        internal static GameObject Find()
        {
            GameObject variant = GameObject.Find("Player_v7 Variant");
            if (variant != null && variant.activeInHierarchy)
                return variant;

            GameObject v7 = GameObject.Find("Player_v7");
            if (v7 != null && v7.activeInHierarchy)
                return v7;

            return variant != null ? variant : v7;
        }
    }
}

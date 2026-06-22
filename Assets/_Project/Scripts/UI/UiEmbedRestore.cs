using UnityEngine;

namespace Project.UI
{
    internal static class UiEmbedRestore
    {
        public static bool TryRestoreParent(Transform child, Transform originalParent)
        {
            if (child == null || originalParent == null)
                return false;

            if (originalParent.gameObject == null)
                return false;

            if (!Application.isPlaying)
                return false;

            if (child.parent == originalParent)
                return true;

            child.SetParent(originalParent, false);
            return true;
        }
    }
}

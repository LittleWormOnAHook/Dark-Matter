using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Filters layout profile capture/apply to structural shell nodes — excludes runtime-generated UI.
    /// </summary>
    public static class UiLayoutCaptureRules
    {
        public static bool ShouldCaptureTransform(Transform current, Transform profileRoot)
        {
            if (current == null || profileRoot == null)
                return false;

            if (current == profileRoot)
                return true;

            string name = current.name;
            if (IsRuntimeGeneratedName(name))
                return false;

            if (IsDescendantOf(current, profileRoot, "MainInventoryGrid") && current.name != "MainInventoryGrid")
                return false;

            return true;
        }

        public static bool ShouldApplyNode(UiLayoutNodeEntry entry, bool panelEmbedded = false)
        {
            if (entry == null)
                return false;

            if (panelEmbedded && string.IsNullOrEmpty(entry.relativePath))
                return false;

            string path = entry.relativePath ?? string.Empty;
            if (path.Contains("(Clone)", System.StringComparison.Ordinal))
                return false;

            if (path.StartsWith("MainInventoryGrid/", System.StringComparison.Ordinal))
                return false;

            if (path.Contains("TMP SubMeshUI", System.StringComparison.Ordinal))
                return false;

            return true;
        }

        public static bool ShouldApplyRootActiveState(Transform profileRoot, UiLayoutNodeEntry entry, bool panelEmbedded)
        {
            if (entry == null || profileRoot == null)
                return true;

            // Panel visibility is driven by journal embed/restore, not layout profiles.
            if (string.IsNullOrEmpty(entry.relativePath))
                return false;

            return true;
        }

        private static bool IsRuntimeGeneratedName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return name.Contains("(Clone)", System.StringComparison.Ordinal)
                || name.StartsWith("InventorySlot", System.StringComparison.Ordinal)
                || name.StartsWith("TMP SubMeshUI", System.StringComparison.Ordinal);
        }

        private static bool IsDescendantOf(Transform current, Transform root, string ancestorName)
        {
            Transform node = current.parent;
            while (node != null && node != root)
            {
                if (node.name == ancestorName)
                    return true;

                node = node.parent;
            }

            return false;
        }

        public static bool ShouldRecurseInto(Transform child, Transform root, Transform parent)
        {
            if (child == null)
                return false;

            if (parent != null && parent.name == "MainInventoryGrid")
                return false;

            if (IsRuntimeGeneratedName(child.name))
                return false;

            return true;
        }
    }
}

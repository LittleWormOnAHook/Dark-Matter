#if UNITY_EDITOR
using Invector;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Play Mode Saver must not persist animated bones, body snaps, or weapon slot transforms on the player rig.
    /// </summary>
    internal static class PlayModeEditPlayerExclusions
    {
        private const string PlayerInvectorName = "Player_Invector";

        public static bool ShouldSkipCapture(Transform transform)
        {
            if (transform == null || !IsUnderPlayerRig(transform))
                return false;

            if (IsCaptureSubtreeRoot(transform.name))
                return true;

            for (Transform parent = transform.parent; parent != null; parent = parent.parent)
            {
                if (IsCaptureSubtreeRoot(parent.name))
                    return true;
            }

            return IsSkippedLeafTransform(transform);
        }

        public static bool ShouldSkipCaptureChildren(Transform transform)
        {
            if (transform == null || !IsUnderPlayerRig(transform))
                return false;

            if (IsCaptureSubtreeRoot(transform.name))
                return true;

            for (Transform parent = transform.parent; parent != null; parent = parent.parent)
            {
                if (IsCaptureSubtreeRoot(parent.name))
                    return true;
            }

            return false;
        }

        public static bool ShouldSkipApply(Transform transform)
        {
            return ShouldSkipCapture(transform);
        }

        public static bool ShouldSkipPrefabAssetCapture(string assetPath)
        {
            return assetPath != null
                && assetPath.EndsWith("/Player_Invector.prefab", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUnderPlayerRig(Transform transform)
        {
            Transform root = transform.root;
            if (root == null)
                return false;

            if (root.CompareTag("Player"))
                return true;

            return root.name.IndexOf(PlayerInvectorName, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsCaptureSubtreeRoot(string transformName)
        {
            return transformName == "BodySnaps"
                || transformName == "WeaponHolders"
                || transformName == "PreloadedMeleeWeaponSlots"
                || transformName == "PreloadedRangedWeaponSlots";
        }

        private static bool IsSkippedLeafTransform(Transform transform)
        {
            string name = transform.name;
            if (name.StartsWith("Drawn_", System.StringComparison.Ordinal)
                || name.StartsWith("Holstered_", System.StringComparison.Ordinal))
            {
                return true;
            }

            if (name.StartsWith("VBOT_", System.StringComparison.Ordinal))
                return true;

            if (name == "RifleHolder" || name == "HandgunHolder")
                return true;

            return transform.GetComponent<vSnapToBody>() != null
                || transform.GetComponent<vBodySnappingControl>() != null;
        }
    }
}
#endif

using Project.Player.Invector;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Legacy ECM2 player animator restore — no-op for Invector-only project.
    /// </summary>
    public static class EditorPlayModeAnimatorSafety
    {
        [MenuItem(DarkMatterGenesisEditorMenus.Maintenance + "Restore Player Animators After Play", false, 4)]
        public static void RestorePlayerAnimatorsMenu()
        {
            EditorUtility.DisplayDialog(
                "Player Animators",
                "Invector player prefabs manage their own animator controllers. No restore needed.",
                "OK");
        }

        public static bool RestoreMissingPlayerAnimators() => false;
    }
}

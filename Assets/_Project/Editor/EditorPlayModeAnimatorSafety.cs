using Project.EditorTools.Player;
using Project.Player;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Restores missing player animator controllers after Play Mode. Menu-only by default.
    /// </summary>
    public static class EditorPlayModeAnimatorSafety
    {
        [MenuItem(SurvivalPioneerEditorMenus.Maintenance + "Restore Player Animators After Play", false, 4)]
        public static void RestorePlayerAnimatorsMenu()
        {
            if (RestoreMissingPlayerAnimators())
            {
                EditorUtility.DisplayDialog(
                    "Player Animators",
                    "Restored missing Player animator controllers.",
                    "OK");
                return;
            }

            EditorUtility.DisplayDialog(
                "Player Animators",
                "All Player animators already had controllers assigned.",
                "OK");
        }

        public static bool RestoreMissingPlayerAnimators()
        {
            PlayerController[] players = Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include);

            bool changed = false;
            for (int i = 0; i < players.Length; i++)
            {
                PlayerController player = players[i];
                if (player == null)
                    continue;

                Animator animator = player.GetComponentInChildren<Animator>(true);
                if (animator == null || animator.runtimeAnimatorController != null)
                    continue;

                if (PlayerLocomotionOverrideAssetUtility.AssignController(
                        animator,
                        player.LocomotionAnimations))
                {
                    EditorUtility.SetDirty(animator);
                    if (PrefabUtility.IsPartOfPrefabInstance(animator.gameObject))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

                    changed = true;
                }
            }

            if (changed)
                Debug.Log("[Survival Pioneer] Restored missing Player animator controllers.");

            return changed;
        }
    }
}

using Project.AI.Validation;
using UnityEditor;
using UnityEngine;

namespace Project.AI.EditorTools
{
    /// <summary>Play-mode menu for <see cref="DMEnemyCollisionWalkValidation"/>.</summary>
    public static class DMEnemyCollisionWalkValidationMenu
    {
        private const string MenuPath = "Tools/Dark Matter Genesis/Combat/Enemy Wall Walk Validation (Play)";
        private const string StopMenuPath = "Tools/Dark Matter Genesis/Combat/Stop Enemy Wall Walk Validation (Play)";

        [MenuItem(MenuPath, true)]
        private static bool ValidatePlayMode() => Application.isPlaying;

        [MenuItem(MenuPath, false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Combat_Studio + 2)]
        private static void AttachAndStartOnSelection()
        {
            GameObject go = Selection.activeGameObject;
            if (go == null)
            {
                Debug.LogWarning("[EnemyWalkValidation] Select an enemy GameObject with EnemyAiController.");
                return;
            }

            EnemyAiController ai = go.GetComponent<EnemyAiController>();
            if (ai == null)
            {
                Debug.LogWarning("[EnemyWalkValidation] Selection has no EnemyAiController.");
                return;
            }

            DMEnemyCollisionWalkValidation validation = go.GetComponent<DMEnemyCollisionWalkValidation>();
            if (validation == null)
                validation = go.AddComponent<DMEnemyCollisionWalkValidation>();

            validation.EnablePatrolDriveForManualRun();
            validation.StartValidationPatrol();
            Selection.activeGameObject = go;
            Debug.LogWarning(
                "[EnemyWalkValidation] TEMPORARY test patrol on '" + go.name + "' only — green gizmo lines are NOT normal wander. "
                + "Use Stop menu or exit Play to restore authored wander/patrol.",
                go);
        }

        [MenuItem(StopMenuPath, true)]
        private static bool ValidateStopPlayMode() => Application.isPlaying;

        [MenuItem(StopMenuPath, false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Combat_Studio + 3)]
        private static void StopOnSelection()
        {
            GameObject go = Selection.activeGameObject;
            if (go == null)
            {
                Debug.LogWarning("[EnemyWalkValidation] Select the enemy that is running validation.");
                return;
            }

            DMEnemyCollisionWalkValidation validation = go.GetComponent<DMEnemyCollisionWalkValidation>();
            if (validation == null)
            {
                Debug.LogWarning("[EnemyWalkValidation] No validation component on selection — nothing to stop.");
                return;
            }

            validation.StopValidationPatrol();
            Object.Destroy(validation);
            Debug.Log("[EnemyWalkValidation] Stopped and removed validation component from '" + go.name + "'. Wander/patrol restored.", go);
        }
    }
}

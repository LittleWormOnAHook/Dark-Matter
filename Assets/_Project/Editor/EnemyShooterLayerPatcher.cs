using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Menu commands for patching existing enemy controllers onto the ShooterMelee base.
    ///
    /// "Rebuild from ShooterMelee Base": replaces the selected controller with a full copy of
    /// Invector@ShooterMelee (all layers preserved) and re-adds any states that were in its
    /// old Base Layer. Use this once on any enemy controller that predates the ShooterMelee-base
    /// approach (e.g. The_Evil_OneController).
    /// </summary>
    public static class EnemyShooterLayerPatcher
    {
        [MenuItem(SurvivalPioneerEditorMenus.RebuildEnemyControllerFromShooterMelee)]
        private static void RebuildSelected()
        {
            AnimatorController target = Selection.activeObject as AnimatorController;
            if (target == null)
            {
                EditorUtility.DisplayDialog("Rebuild from ShooterMelee Base",
                    "Select an AnimatorController asset in the Project window first.", "OK");
                return;
            }

            string path = AssetDatabase.GetAssetPath(target);
            if (string.IsNullOrEmpty(path))
            {
                EditorUtility.DisplayDialog("Rebuild from ShooterMelee Base",
                    "Could not resolve asset path for the selected controller.", "OK");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Rebuild from ShooterMelee Base",
                $"This will REPLACE '{target.name}' with a copy of Invector@ShooterMelee, " +
                "then restore its existing Base Layer states (Idle, Walk, Run, Attack, Hit, Death).\n\n" +
                "All non-Base layers (UpperBody, Shot, OnlyArms) will be replaced with the " +
                "ShooterMelee versions.\n\nContinue?",
                "Rebuild", "Cancel");

            if (!confirmed) return;

            AnimatorController result =
                EnemyShooterControllerBuilder.RebuildFromShooterMeleeBase(path);

            EditorUtility.DisplayDialog("Rebuild from ShooterMelee Base",
                result != null
                    ? $"Done — '{result.name}' now uses ShooterMelee as its base."
                    : "Failed — check the Console for errors.",
                "OK");
        }

        [MenuItem(SurvivalPioneerEditorMenus.RebuildEnemyControllerFromShooterMelee, true)]
        private static bool RebuildSelectedValidate() =>
            Selection.activeObject is AnimatorController;
    }
}

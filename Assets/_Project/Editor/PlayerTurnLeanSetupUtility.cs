using Project.EditorTools;
using Project.EditorTools.Player;
using Project.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// Legacy Turn Lean layer helper. Forward turn lean now lives in the Grounded blend tree.
    /// </summary>
    public static class PlayerTurnLeanSetupUtility
    {
        [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Remove Legacy Turn Lean Layer", false, 7)]
        public static void RemoveLegacyTurnLeanLayerMenu()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                PlayerAnimatorControllerPaths.GkcControllerPath);
            if (controller == null)
            {
                EditorUtility.DisplayDialog(
                    "Turn Lean",
                    "Could not find ProjectGKCCharacterController.",
                    "OK");
                return;
            }

            PlayerCombatLocomotionSetupUtility.RemoveTurnLeanLayer(controller);
            AnimatorControllerGraphRepairUtility.RemoveOrphanSubAssets(
                PlayerAnimatorControllerPaths.GkcControllerPath);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog(
                "Turn Lean",
                "Removed the legacy additive Turn Lean layer.\n\n" +
                "Use Tools → Survival Pioneer → Combat → Animations → Rebuild Grounded Blend Tree " +
                "to bake forward turn clips into the base locomotion tree.",
                "OK");
        }
    }
}

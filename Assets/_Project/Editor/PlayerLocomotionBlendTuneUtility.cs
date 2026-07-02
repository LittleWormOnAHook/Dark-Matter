using Project.EditorTools;
using Project.EditorTools.Player;
using Project.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// Syncs swapped locomotion clips and tunes cross-layer blend times on the player animator.
    /// </summary>
    public static class PlayerLocomotionBlendTuneUtility
    {
        private const string ControllerPath = PlayerAnimatorControllerPaths.GkcControllerPath;

        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Players/Player.prefab";

        private const float BaseTransitionDuration = 0.18f;
        private const float UpperBodyAttackExitTime = 0.88f;
        private const float UpperBodyAttackTransitionDuration = 0.22f;

        [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Tune Locomotion Layer Blending", false, 4)]
        public static void TuneLocomotionLayerBlendingMenu()
        {
            bool changed = ApplyLocomotionLayerBlending(showDialog: true);
            if (changed)
                return;

            EditorUtility.DisplayDialog(
                "Locomotion Layer Blending",
                "Locomotion layer blending is already tuned.",
                "OK");
        }

        public static bool ApplyLocomotionLayerBlending(bool showDialog)
        {
            bool changed = false;
            changed |= SyncPlayerPrefabLocomotionOverrides();
            changed |= TuneBaseLayerTransitions();
            changed |= TuneUpperBodyLayerTransitions();

            if (!changed)
                return false;

            AssetDatabase.SaveAssets();

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Locomotion Layer Blending",
                    "Synced locomotion clip overrides and smoothed base-layer, combat, and charge transitions.",
                    "OK");
            }

            return true;
        }

        private static bool SyncPlayerPrefabLocomotionOverrides()
        {
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefabRoot == null)
                return false;

            PlayerController player = prefabRoot.GetComponent<PlayerController>();
            if (player == null || player.LocomotionAnimations == null)
                return false;

            bool changed = PlayerLocomotionOverrideAssetUtility.SyncOverrideAsset(player.LocomotionAnimations);

            Animator animator = player.GetComponentInChildren<Animator>(true);
            if (animator != null)
                changed |= PlayerLocomotionOverrideAssetUtility.AssignController(animator, player.LocomotionAnimations);

            if (changed)
                EditorUtility.SetDirty(prefabRoot);

            return changed;
        }

        private static bool TuneBaseLayerTransitions(AnimatorController controller)
        {
            if (controller == null || controller.layers.Length == 0)
                return false;

            bool changed = false;
            changed |= TuneStateMachineTransitions(controller.layers[0].stateMachine, BaseTransitionDuration, false);
            return changed;
        }

        private static bool TuneBaseLayerTransitions()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                return false;

            bool changed = TuneBaseLayerTransitions(controller);
            if (changed)
                EditorUtility.SetDirty(controller);

            return changed;
        }

        private static bool TuneUpperBodyLayerTransitions()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                return false;

            bool changed = false;
            for (int i = 0; i < controller.layers.Length; i++)
            {
                AnimatorControllerLayer layer = controller.layers[i];
                if (layer.name != "Upper Body Combat" && layer.name != "Upper Body Charge")
                    continue;

                changed |= TuneStateMachineTransitions(
                    layer.stateMachine,
                    UpperBodyAttackTransitionDuration,
                    true,
                    UpperBodyAttackExitTime);
            }

            if (changed)
                EditorUtility.SetDirty(controller);

            return changed;
        }

        private static bool TuneStateMachineTransitions(
            AnimatorStateMachine machine,
            float duration,
            bool requireExitTime,
            float exitTime = 0.75f)
        {
            if (machine == null)
                return false;

            bool changed = false;

            AnimatorStateTransition[] anyStateTransitions = machine.anyStateTransitions;
            for (int i = 0; i < anyStateTransitions.Length; i++)
            {
                AnimatorStateTransition transition = anyStateTransitions[i];
                if (transition == null)
                    continue;

                if (Mathf.Approximately(transition.duration, duration)
                    && transition.hasFixedDuration
                    && (!requireExitTime || transition.hasExitTime))
                    continue;

                transition.duration = duration;
                transition.hasFixedDuration = true;
                if (requireExitTime)
                {
                    transition.hasExitTime = true;
                    transition.exitTime = exitTime;
                }

                changed = true;
            }

            foreach (ChildAnimatorState child in machine.states)
            {
                if (child.state == null)
                    continue;

                foreach (AnimatorStateTransition transition in child.state.transitions)
                {
                    if (transition == null)
                        continue;

                    if (requireExitTime && !transition.hasExitTime)
                        continue;

                    float targetDuration = requireExitTime ? duration : BaseTransitionDuration;
                    float targetExitTime = requireExitTime ? exitTime : transition.exitTime;

                    if (Mathf.Approximately(transition.duration, targetDuration)
                        && transition.hasFixedDuration
                        && (!requireExitTime || Mathf.Approximately(transition.exitTime, targetExitTime)))
                        continue;

                    transition.duration = targetDuration;
                    transition.hasFixedDuration = true;
                    if (requireExitTime)
                        transition.exitTime = targetExitTime;

                    changed = true;
                }
            }

            ChildAnimatorStateMachine[] childMachines = machine.stateMachines;
            for (int i = 0; i < childMachines.Length; i++)
            {
                if (childMachines[i].stateMachine != null)
                {
                    changed |= TuneStateMachineTransitions(
                        childMachines[i].stateMachine,
                        duration,
                        requireExitTime,
                        exitTime);
                }
            }

            return changed;
        }
    }
}

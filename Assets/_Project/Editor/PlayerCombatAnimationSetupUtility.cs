using System.Collections.Generic;
using Project.EditorTools;
using Project.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// Wires AttackAnimSpeed on attack states, block clips, and Block2H on the charge layer.
    /// </summary>
    public static class PlayerCombatAnimationSetupUtility
    {
        private const string ControllerPath = PlayerAnimatorControllerPaths.GkcControllerPath;

        private const string AttackSpeedParameter = "AttackAnimSpeed";

        private const string Block1HClipPath =
            "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Combat/1H/HumanM@Parry1H01_R - Loop.fbx";

        private const string Block2HClipPath =
            "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Combat/2H/HumanM@Parry2H01 - Loop.fbx";

        private static readonly string[] AttackStateNames =
        {
            "AttackCombo1",
            "AttackCombo2",
            "AttackCombo3",
            "AttackCombo4",
            "AttackCombo5",
            "TwoHandAttack1",
            "TwoHandAttack2",
            "TwoHandAttack3",
            "TwoHandAttack4",
            "TwoHandPowerHit"
        };

        [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Setup Combat Animation Speed + Block", false, 6)]
        public static void SetupCombatAnimationSpeedAndBlockMenu()
        {
            if (ApplyCombatAnimationSetup(showDialog: true))
                return;

            EditorUtility.DisplayDialog(
                "Combat Animations",
                "Attack speed parameter and block states are already configured.",
                "OK");
        }

        private static bool ApplyCombatAnimationSetup(bool showDialog)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                return false;

            bool changed = false;
            changed |= EnsureFloatParameter(controller, AttackSpeedParameter, 1f);

            for (int layerIndex = 0; layerIndex < controller.layers.Length; layerIndex++)
                changed |= WireAttackStates(controller.layers[layerIndex].stateMachine);

            AnimatorStateMachine chargeMachine = FindLayerStateMachine(controller, "Upper Body Charge");
            if (chargeMachine != null)
            {
                AnimationClip block1H = LoadClip(Block1HClipPath, "HumanM@Parry1H01_R - Loop");
                AnimationClip block2H = LoadClip(Block2HClipPath, "HumanM@Parry2H01 - Loop");

                AnimatorState block1HState = FindOrCreateState(chargeMachine, "Block", new Vector3(540f, 240f, 0f));
                AnimatorState block2HState = FindOrCreateState(chargeMachine, "Block2H", new Vector3(780f, 240f, 0f));

                if (block1H != null)
                    changed |= AssignMotion(block1HState, block1H);
                if (block2H != null)
                    changed |= AssignMotion(block2HState, block2H);

                changed |= EnsureBlockEndTransition(chargeMachine, block1HState);
                changed |= EnsureBlockEndTransition(chargeMachine, block2HState);
            }

            if (!changed)
                return false;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Combat Animations",
                    "AttackAnimSpeed is wired on attack states.\nBlock and Block2H use Human parry loops.",
                    "OK");
            }

            return true;
        }

        private static bool WireAttackStates(AnimatorStateMachine machine)
        {
            if (machine == null)
                return false;

            bool changed = false;
            Queue<AnimatorStateMachine> machines = new Queue<AnimatorStateMachine>();
            machines.Enqueue(machine);

            while (machines.Count > 0)
            {
                AnimatorStateMachine current = machines.Dequeue();
                foreach (ChildAnimatorState child in current.states)
                {
                    AnimatorState state = child.state;
                    if (state == null || !IsAttackState(state.name))
                        continue;

                    if (!Mathf.Approximately(state.speed, 1f))
                    {
                        state.speed = 1f;
                        changed = true;
                    }

                    if (!state.speedParameterActive || state.speedParameter != AttackSpeedParameter)
                    {
                        state.speedParameterActive = true;
                        state.speedParameter = AttackSpeedParameter;
                        changed = true;
                    }
                }

                foreach (ChildAnimatorStateMachine childMachine in current.stateMachines)
                {
                    if (childMachine.stateMachine != null)
                        machines.Enqueue(childMachine.stateMachine);
                }
            }

            return changed;
        }

        private static bool IsAttackState(string stateName)
        {
            for (int i = 0; i < AttackStateNames.Length; i++)
            {
                if (AttackStateNames[i] == stateName)
                    return true;
            }

            return false;
        }

        private static bool EnsureFloatParameter(AnimatorController controller, string name, float defaultValue)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name == name)
                    return false;
            }

            controller.AddParameter(name, AnimatorControllerParameterType.Float);
            AnimatorControllerParameter added = controller.parameters[controller.parameters.Length - 1];
            added.defaultFloat = defaultValue;
            return true;
        }

        private static AnimatorStateMachine FindLayerStateMachine(AnimatorController controller, string layerName)
        {
            for (int i = 0; i < controller.layers.Length; i++)
            {
                if (controller.layers[i].name == layerName)
                    return controller.layers[i].stateMachine;
            }

            return null;
        }

        private static AnimatorState FindOrCreateState(AnimatorStateMachine machine, string stateName, Vector3 position)
        {
            foreach (ChildAnimatorState child in machine.states)
            {
                if (child.state != null && child.state.name == stateName)
                    return child.state;
            }

            return machine.AddState(stateName, position);
        }

        private static bool AssignMotion(AnimatorState state, Motion motion)
        {
            if (state == null || state.motion == motion)
                return false;

            state.motion = motion;
            state.writeDefaultValues = false;
            return true;
        }

        private static bool EnsureBlockEndTransition(AnimatorStateMachine machine, AnimatorState blockState)
        {
            if (machine == null || blockState == null)
                return false;

            foreach (AnimatorStateTransition transition in blockState.transitions)
            {
                for (int i = 0; i < transition.conditions.Length; i++)
                {
                    if (transition.conditions[i].parameter == "Block"
                        && transition.conditions[i].mode == AnimatorConditionMode.IfNot)
                    {
                        return false;
                    }
                }
            }

            AnimatorState emptyState = null;
            foreach (ChildAnimatorState child in machine.states)
            {
                if (child.state != null && child.state.name == "Empty")
                {
                    emptyState = child.state;
                    break;
                }
            }

            if (emptyState == null)
                return false;

            AnimatorStateTransition toEmpty = blockState.AddTransition(emptyState);
            toEmpty.AddCondition(AnimatorConditionMode.IfNot, 0f, "Block");
            toEmpty.duration = 0.1f;
            toEmpty.hasExitTime = false;
            toEmpty.canTransitionToSelf = true;
            return true;
        }

        private static AnimationClip LoadClip(string assetPath, string clipName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && clip.name == clipName)
                    return clip;
            }

            return null;
        }
    }
}

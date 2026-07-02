using System;
using Project.EditorTools;
using Project.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// Adds an upper-body combat layer so attack clips play on arms/torso while the base layer keeps walk/run/jump.
    /// </summary>
    public static class PlayerUpperBodyCombatSetupUtility
    {
        private const string ControllerPath = PlayerAnimatorControllerPaths.GkcControllerPath;

        private const string UpperBodyMaskPath =
            "Assets/Animations/Human Animations Melee/Human Animations/Models/Avatar Masks/Human Body Upper Mask.mask";

        private const string CombatLayerName = GkcAnimatorConstants.UpperBodyCombatLayer;
        private const float AttackExitTime = 0.88f;
        private const float AttackTransitionDuration = 0.22f;

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
            "TwoHandPowerHit",
            "PowerHitCharge"
        };

        [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Tune Upper Body Combat Blending", false, 7)]
        public static void TuneUpperBodyCombatBlendingMenu()
        {
            if (TuneUpperBodyLayerBlending(showDialog: true))
                return;

            EditorUtility.DisplayDialog(
                "Upper Body Combat",
                "Upper Body Combat blending is already tuned.",
                "OK");
        }

        [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Setup Upper Body Combat Layer", false, 5)]
        public static void SetupUpperBodyCombatLayer()
        {
            if (ApplyUpperBodyCombatLayer(showDialog: true))
                return;

            EditorUtility.DisplayDialog(
                "Upper Body Combat",
                "Upper Body Combat layer is already configured.",
                "OK");
        }

        private static void TryEnsureUpperBodyCombatLayer()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            ApplyUpperBodyCombatLayer(showDialog: false);
        }

        private static bool ApplyUpperBodyCombatLayer(bool showDialog)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "Upper Body Combat",
                        $"Could not load animator controller at:\n{ControllerPath}",
                        "OK");
                }

                return false;
            }

            if (HasCombatLayer(controller))
                return false;

            AvatarMask upperBodyMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperBodyMaskPath);
            if (upperBodyMask == null)
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "Upper Body Combat",
                        $"Could not load avatar mask at:\n{UpperBodyMaskPath}",
                        "OK");
                }

                return false;
            }

            AnimatorControllerLayer combatLayer = CreateCombatLayer(controller, upperBodyMask);
            AnimatorStateMachine combatMachine = combatLayer.stateMachine;
            AnimatorStateMachine baseMachine = controller.layers[0].stateMachine;

            AnimatorState emptyState = combatMachine.AddState("Empty", new Vector3(280f, 120f, 0f));
            emptyState.writeDefaultValues = false;
            combatMachine.defaultState = emptyState;

            int updated = 0;
            for (int i = 0; i < AttackStateNames.Length; i++)
            {
                string stateName = AttackStateNames[i];
                AnimatorState source = FindState(baseMachine, stateName);
                if (source == null || source.motion == null)
                    continue;

                AnimatorState combatState = combatMachine.AddState(stateName, new Vector3(540f, 40f - i * 56f, 0f));
                combatState.motion = source.motion;
                combatState.speed = source.speed;
                combatState.tag = "Attack";
                EnsureEmptyTransition(combatState, emptyState);
                updated++;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Upper Body Combat",
                    $"Upper Body Combat layer is ready ({updated} attack states mirrored).\n\n" +
                    "Base layer keeps walk/run/jump. Attacks play on the masked upper-body layer.",
                    "OK");
            }
            else
            {
                // Applied silently on editor load.
            }

            return true;
        }

        private static bool HasCombatLayer(AnimatorController controller)
        {
            for (int i = 0; i < controller.layers.Length; i++)
            {
                if (controller.layers[i].name == CombatLayerName)
                    return true;
            }

            return false;
        }

        private static AnimatorControllerLayer CreateCombatLayer(
            AnimatorController controller,
            AvatarMask upperBodyMask)
        {
            AnimatorControllerLayer layer = new AnimatorControllerLayer
            {
                name = CombatLayerName,
                stateMachine = new AnimatorStateMachine(),
                avatarMask = upperBodyMask,
                defaultWeight = 0f,
                blendingMode = AnimatorLayerBlendingMode.Override
            };

            layer.stateMachine.name = CombatLayerName;
            layer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
            if (AssetDatabase.GetAssetPath(controller) is { Length: > 0 } path)
                AssetDatabase.AddObjectToAsset(layer.stateMachine, path);

            controller.AddLayer(layer);
            return layer;
        }

        private static void EnsureEmptyTransition(AnimatorState attackState, AnimatorState emptyState)
        {
            foreach (AnimatorStateTransition transition in attackState.transitions)
            {
                if (transition.destinationState == emptyState &&
                    transition.hasExitTime &&
                    Mathf.Approximately(transition.exitTime, AttackExitTime))
                    return;
            }

            AnimatorStateTransition newTransition = attackState.AddTransition(emptyState);
            newTransition.hasExitTime = true;
            newTransition.exitTime = AttackExitTime;
            newTransition.duration = AttackTransitionDuration;
            newTransition.hasFixedDuration = true;
            newTransition.canTransitionToSelf = false;
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            foreach (ChildAnimatorState child in stateMachine.states)
            {
                if (child.state != null && child.state.name == stateName)
                    return child.state;
            }

            return null;
        }

        private static bool TuneUpperBodyLayerBlending(bool showDialog)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                return false;

            bool updated = false;
            for (int layerIndex = 0; layerIndex < controller.layers.Length; layerIndex++)
            {
                AnimatorControllerLayer layer = controller.layers[layerIndex];
                if (layer.name != CombatLayerName && layer.name != "Upper Body Charge")
                    continue;

                if (layer.defaultWeight > 0f && layer.name == "Upper Body Charge")
                {
                    layer.defaultWeight = 0f;
                    controller.layers[layerIndex] = layer;
                    updated = true;
                }

                updated |= TuneStateMachineTransitions(layer.stateMachine);
            }

            if (!updated)
                return false;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Upper Body Combat",
                    "Upper body combat and charge layers now use longer blend transitions.",
                    "OK");
            }
            else
            {
                // Applied silently on editor load.
            }

            return true;
        }

        private static bool TuneStateMachineTransitions(AnimatorStateMachine stateMachine)
        {
            bool updated = false;
            foreach (ChildAnimatorState child in stateMachine.states)
            {
                if (child.state == null)
                    continue;

                foreach (AnimatorStateTransition transition in child.state.transitions)
                {
                    if (!transition.hasExitTime)
                        continue;

                    if (Mathf.Approximately(transition.exitTime, AttackExitTime) &&
                        Mathf.Approximately(transition.duration, AttackTransitionDuration))
                        continue;

                    transition.exitTime = AttackExitTime;
                    transition.duration = AttackTransitionDuration;
                    transition.hasFixedDuration = true;
                    updated = true;
                }
            }

            return updated;
        }
    }
}

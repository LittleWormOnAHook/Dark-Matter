using Project.EditorTools;
using Project.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    public static class PlayerLootAnimationSetupUtility
    {
        private const string ControllerPath = PlayerAnimatorControllerPaths.GkcControllerPath;

        private const string LootClipPath =
            "Assets/Animations/Basic Motions/Animations/Misc/BasicMotions@Loot01.fbx";

        private const string StartClipName = "BasicMotions@Loot01 - Start";
        private const string LoopClipName = "BasicMotions@Loot01 - Loop";
        private const string EndClipName = "BasicMotions@Loot01 - End";

        [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Setup Player Loot Animations", false, 9)]
        public static void SetupPlayerLootAnimations()
        {
            if (ApplyLootStates(showDialog: true))
                return;

            EditorUtility.DisplayDialog(
                "Player Loot Animations",
                "Loot Start/Loop/End states are already configured.",
                "OK");
        }

        public static bool ApplyLootStates(bool showDialog)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                return false;

            AnimationClip startClip = LoadClip(StartClipName);
            AnimationClip loopClip = LoadClip(LoopClipName);
            AnimationClip endClip = LoadClip(EndClipName);
            if (startClip == null || loopClip == null || endClip == null)
            {
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "Player Loot Animations",
                        $"Could not load loot clips from:\n{LootClipPath}",
                        "OK");
                }

                return false;
            }

            AnimatorStateMachine root = controller.layers[0].stateMachine;
            AnimatorState startState = FindOrCreateState(root, "LootStart", new Vector3(590f, -120f, 0f));
            AnimatorState loopState = FindOrCreateState(root, "LootLoop", new Vector3(820f, -120f, 0f));
            AnimatorState endState = FindOrCreateState(root, "LootEnd", new Vector3(1050f, -120f, 0f));

            bool changed = false;
            changed |= AssignMotion(startState, startClip);
            changed |= AssignMotion(loopState, loopClip);
            changed |= AssignMotion(endState, endClip);
            changed |= ConfigureTransition(startState, loopState, 0.9f);
            changed |= ConfigureTransition(endState, FindState(root, "Grounded"), 0.88f);

            RemoveState(root, "Pickup");

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Player Loot Animations",
                    "LootStart, LootLoop, and LootEnd are wired to BasicMotions@Loot01.",
                    "OK");
            }

            return changed;
        }

        private static AnimationClip LoadClip(string clipName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(LootClipPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && clip.name == clipName)
                    return clip;
            }

            return null;
        }

        private static AnimatorState FindOrCreateState(AnimatorStateMachine machine, string stateName, Vector3 position)
        {
            AnimatorState existing = FindState(machine, stateName);
            if (existing != null)
                return existing;

            return machine.AddState(stateName, position);
        }

        private static AnimatorState FindState(AnimatorStateMachine machine, string stateName)
        {
            foreach (ChildAnimatorState child in machine.states)
            {
                if (child.state != null && child.state.name == stateName)
                    return child.state;
            }

            return null;
        }

        private static void RemoveState(AnimatorStateMachine machine, string stateName)
        {
            for (int i = 0; i < machine.states.Length; i++)
            {
                if (machine.states[i].state != null && machine.states[i].state.name == stateName)
                {
                    machine.RemoveState(machine.states[i].state);
                    return;
                }
            }
        }

        private static bool AssignMotion(AnimatorState state, Motion motion)
        {
            if (state == null || state.motion == motion)
                return false;

            state.motion = motion;
            state.writeDefaultValues = true;
            return true;
        }

        private static bool ConfigureTransition(AnimatorState from, AnimatorState to, float exitTime)
        {
            if (from == null || to == null)
                return false;

            for (int i = 0; i < from.transitions.Length; i++)
            {
                if (from.transitions[i].destinationState == to)
                    return false;
            }

            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.duration = 0.1f;
            transition.hasFixedDuration = true;
            return true;
        }
    }
}

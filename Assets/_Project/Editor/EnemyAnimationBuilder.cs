using System.IO;
using Project.AI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools
{
    public static class EnemyAnimationBuilder
    {
        public struct BuiltAnimationSet
        {
            public RuntimeAnimatorController Controller;
            public string[] IdleStateNames;
            public string WalkStateName;
            public string RunStateName;
            public string[] AttackStateNames;
            public string[] HitStateNames;
            public string DeathStateName;
        }

        public static bool HasClipAssignments(EnemyDefinition definition)
        {
            if (definition == null)
                return false;

            return HasClips(definition.idleClips) ||
                   HasClips(definition.walkClips) ||
                   HasClips(definition.runClips) ||
                   HasClips(definition.attackClips) ||
                   HasClips(definition.hitClips) ||
                   HasClips(definition.deathClips);
        }

        public static BuiltAnimationSet BuildController(EnemyDefinition definition)
        {
            BuiltAnimationSet result = new BuiltAnimationSet
            {
                IdleStateNames = BuildStateNames("Idle", definition.idleClips),
                WalkStateName = HasClips(definition.walkClips) ? "Walk" : string.Empty,
                RunStateName = HasClips(definition.runClips) ? "Run" : string.Empty,
                AttackStateNames = BuildStateNames("Attack", definition.attackClips),
                HitStateNames = BuildStateNames("Hit", definition.hitClips),
                DeathStateName = HasClips(definition.deathClips) ? "Death" : string.Empty
            };

            if (!HasClipAssignments(definition))
                return result;

            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.AnimationsEnemies);

            string fileName = EnemyPrefabBuilder.SanitizeFileName(
                string.IsNullOrWhiteSpace(definition.animatorControllerFileName)
                    ? definition.prefabFileName + "Controller"
                    : definition.animatorControllerFileName,
                definition.displayName + "Controller");
            string controllerPath = $"{ProjectAssetPaths.AnimationsEnemies}/{fileName}.controller";

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            AnimatorStateMachine root = controller.layers[0].stateMachine;
            ClearStates(root);

            AnimatorState defaultState = null;
            defaultState = AddClipStates(root, result.IdleStateNames, definition.idleClips, defaultState);
            AddSingleClipState(root, result.WalkStateName, definition.walkClips);
            AddSingleClipState(root, result.RunStateName, definition.runClips);
            AddClipStates(root, result.AttackStateNames, definition.attackClips, null);
            AddClipStates(root, result.HitStateNames, definition.hitClips, null);

            if (HasClips(definition.deathClips))
            {
                AnimatorState deathState = root.AddState(result.DeathStateName);
                deathState.motion = definition.deathClips[0];
                if (defaultState == null)
                    defaultState = deathState;
            }

            if (defaultState != null)
                root.defaultState = defaultState;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            result.Controller = controller;
            return result;
        }

        private static AnimatorState AddClipStates(
            AnimatorStateMachine root,
            string[] stateNames,
            AnimationClip[] clips,
            AnimatorState defaultState)
        {
            if (stateNames == null || clips == null)
                return defaultState;

            int count = Mathf.Min(stateNames.Length, clips.Length);
            for (int i = 0; i < count; i++)
            {
                if (clips[i] == null)
                    continue;

                AnimatorState state = root.AddState(stateNames[i]);
                state.motion = clips[i];
                if (defaultState == null)
                    defaultState = state;
            }

            return defaultState;
        }

        private static void AddSingleClipState(AnimatorStateMachine root, string stateName, AnimationClip[] clips)
        {
            if (string.IsNullOrEmpty(stateName) || !HasClips(clips))
                return;

            AnimatorState state = root.AddState(stateName);
            state.motion = clips[0];
        }

        private static string[] BuildStateNames(string prefix, AnimationClip[] clips)
        {
            if (!HasClips(clips))
                return System.Array.Empty<string>();

            if (prefix == "Death")
                return new[] { "Death" };

            string[] names = new string[clips.Length];
            for (int i = 0; i < clips.Length; i++)
                names[i] = $"{prefix}{i + 1:00}";

            return names;
        }

        private static bool HasClips(AnimationClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return false;

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                    return true;
            }

            return false;
        }

        private static void ClearStates(AnimatorStateMachine stateMachine)
        {
            ChildAnimatorState[] states = stateMachine.states;
            for (int i = states.Length - 1; i >= 0; i--)
                stateMachine.RemoveState(states[i].state);
        }
    }
}

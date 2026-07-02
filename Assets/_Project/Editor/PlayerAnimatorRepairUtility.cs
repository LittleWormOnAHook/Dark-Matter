using Project.EditorTools;
using Project.EditorTools.Player;
using Project.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools.Player
{
    public static class PlayerAnimatorRepairUtility
    {
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Players/Player.prefab";
        private const string ControllerPath = PlayerAnimatorControllerPaths.GkcControllerPath;
        private const string LocomotionAnimSpeedParameter = "LocomotionAnimSpeed";

        [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Repair Player Animator Setup", false, 2)]
        public static void RepairPlayerAnimatorSetupMenu()
        {
            EditorLayoutGuard.ClearSelectionOnly();

            bool changed = RepairPlayerAnimatorSetup(showDialog: false);
            EditorUtility.DisplayDialog(
                "Repair Player Animator",
                changed
                    ? "Player animator setup was repaired. Selection was cleared to avoid Inspector errors."
                    : "Player animator controller and prefab were already configured correctly.",
                "OK");
        }

        public static bool RepairPlayerAnimatorSetup(bool showDialog)
        {
            EditorLayoutGuard.ClearSelectionOnly();
            bool changed = AnimatorControllerGraphRepairUtility.RepairPlayerAnimatorControllerGraphSilent() > 0;

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"PlayerAnimatorRepairUtility: Could not load prefab at {PlayerPrefabPath}");
                return changed;
            }

            changed |= EnsureLocomotionAnimSpeedOnController();
            try
            {
                PlayerController player = prefabRoot.GetComponent<PlayerController>();
                PlayerLocomotionAnimationSettings settings = player != null
                    ? player.LocomotionAnimations
                    : null;

                if (settings != null && settings.HasClipOverrides)
                    changed |= PlayerLocomotionOverrideAssetUtility.SyncOverrideAsset(settings);

                Animator[] animators = prefabRoot.GetComponentsInChildren<Animator>(true);
                for (int i = 0; i < animators.Length; i++)
                {
                    Animator animator = animators[i];
                    if (animator == null)
                        continue;

                    if (PlayerLocomotionOverrideAssetUtility.AssignController(animator, settings))
                        changed = true;
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Repair Player Animator",
                    changed
                        ? "Player prefab animator controller references were repaired."
                        : "Player prefab animator was already configured correctly.",
                    "OK");
            }

            return changed;
        }

        private static bool EnsureLocomotionAnimSpeedOnController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                return false;

            bool changed = EnsureFloatParameter(controller, LocomotionAnimSpeedParameter, 1f);
            changed |= WireGroundedLocomotionSpeedBlend(controller);
            if (changed)
            {
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
            }

            return changed;
        }

        private static bool WireGroundedLocomotionSpeedBlend(AnimatorController controller)
        {
            if (controller.layers.Length == 0)
                return false;

            AnimatorState groundedState = FindState(controller.layers[0].stateMachine, "Grounded");
            if (groundedState == null || groundedState.motion is not BlendTree blendTree)
                return false;

            bool changed = false;
            ChildMotion[] children = blendTree.children;
            for (int i = 0; i < children.Length; i++)
            {
                Vector2 position = children[i].position;
                if (Mathf.Approximately(position.x, 0f)
                    && (Mathf.Approximately(position.y, 0.5f) || Mathf.Approximately(position.y, 1f)))
                {
                    if (children[i].directBlendParameter == LocomotionAnimSpeedParameter)
                        continue;

                    children[i].directBlendParameter = LocomotionAnimSpeedParameter;
                    changed = true;
                }
            }

            if (changed)
                blendTree.children = children;

            return changed;
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
    }
}

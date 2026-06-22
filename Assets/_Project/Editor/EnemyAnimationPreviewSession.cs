using Project.AI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class EnemyAnimationPreviewSession
    {
        private static GameObject previewRoot;
        private static bool animationModeActive;

        public static bool IsActive => previewRoot != null;

        public static bool Start(EnemyDefinition definition, GameObject visualSource, EnemyPrefabBuilder.VisualSourceMode sourceMode)
        {
            Stop();

            if (definition == null)
            {
                Debug.LogWarning("Enemy animation preview requires a definition.");
                return false;
            }

            if (!EnemyAnimationBuilder.HasClipAssignments(definition))
            {
                Debug.LogWarning("Enemy animation preview requires at least one assigned clip.");
                return false;
            }

            previewRoot = EnemyPrefabBuilder.CreatePreviewRoot(definition, sourceMode, visualSource);
            if (previewRoot == null)
            {
                Debug.LogWarning("Enemy animation preview could not create a visual root.");
                return false;
            }

            previewRoot.name = $"{definition.displayName}_AnimPreview";
            previewRoot.transform.position = EnemyPrefabBuilder.ResolveSpawnPosition();
            previewRoot.hideFlags = HideFlags.DontSave;

            EnemyAnimationBuilder.BuiltAnimationSet builtSet = EnemyAnimationSetupUtility.RebuildAnimationTree(definition);
            if (!EnemyAnimationSetupUtility.ApplyAnimationToGameObject(previewRoot, definition, builtSet))
            {
                EditorLayoutGuard.BeforeDestroySceneObject(previewRoot);
                Object.DestroyImmediate(previewRoot);
                previewRoot = null;
                EditorLayoutGuard.ScheduleInspectorRecovery();
                Debug.LogWarning("Enemy animation preview failed to apply animator setup.");
                return false;
            }

            AnimationMode.StartAnimationMode();
            animationModeActive = true;
            Selection.activeGameObject = previewRoot;
            SceneView.lastActiveSceneView?.FrameSelected();
            PlayIdle(definition);
            return true;
        }

        public static void PlayIdle(EnemyDefinition definition)
        {
            PlayState(GetFirstState(definition?.idleClips, "Idle01"));
        }

        public static void PlayWalk(EnemyDefinition definition)
        {
            PlayState(GetFirstState(definition?.walkClips, "Walk"));
        }

        public static void PlayRun(EnemyDefinition definition)
        {
            PlayState(GetFirstState(definition?.runClips, "Run"));
        }

        public static void PlayAttack(EnemyDefinition definition, int index = 0)
        {
            PlayState(GetIndexedState("Attack", index, definition?.attackClips));
        }

        public static void PlayHit(EnemyDefinition definition, int index = 0)
        {
            PlayState(GetIndexedState("Hit", index, definition?.hitClips));
        }

        public static void PlayDeath(EnemyDefinition definition)
        {
            PlayState(GetFirstState(definition?.deathClips, "Death"));
        }

        public static void Stop()
        {
            if (animationModeActive)
            {
                AnimationMode.StopAnimationMode();
                animationModeActive = false;
            }

            if (previewRoot != null)
            {
                EditorLayoutGuard.BeforeDestroySceneObject(previewRoot);
                Object.DestroyImmediate(previewRoot);
                previewRoot = null;
                EditorLayoutGuard.ScheduleInspectorRecovery();
            }
        }

        private static void PlayState(string stateName)
        {
            if (previewRoot == null || string.IsNullOrEmpty(stateName))
                return;

            Animator animator = previewRoot.GetComponent<Animator>();
            if (animator == null)
                animator = previewRoot.GetComponentInChildren<Animator>();

            if (animator == null || animator.runtimeAnimatorController == null)
                return;

            animator.Play(stateName, 0, 0f);
            animator.Update(0f);
        }

        private static string GetFirstState(AnimationClip[] clips, string fallback)
        {
            if (clips == null || clips.Length == 0 || clips[0] == null)
                return fallback;

            return fallback;
        }

        private static string GetIndexedState(string prefix, int index, AnimationClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return $"{prefix}01";

            index = Mathf.Clamp(index, 0, clips.Length - 1);
            return $"{prefix}{index + 1:00}";
        }
    }
}

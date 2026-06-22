using Project.AI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.EditorTools
{
    public static class EnemyAnimationSetupUtility
    {
        public struct AnimationSetupStatus
        {
            public bool HasAnyClips;
            public int IdleCount;
            public int WalkCount;
            public int RunCount;
            public int AttackCount;
            public int HitCount;
            public int DeathCount;
            public bool HasBuiltController;
            public string ControllerPath;
            public bool HasAvatar;
            public string AvatarMessage;
            public string ModelAssetPath;
        }

        public static AnimationSetupStatus Analyze(EnemyDefinition definition, GameObject visualSource)
        {
            AnimationSetupStatus status = new AnimationSetupStatus
            {
                HasAnyClips = EnemyAnimationBuilder.HasClipAssignments(definition),
                IdleCount = CountClips(definition?.idleClips),
                WalkCount = CountClips(definition?.walkClips),
                RunCount = CountClips(definition?.runClips),
                AttackCount = CountClips(definition?.attackClips),
                HitCount = CountClips(definition?.hitClips),
                DeathCount = CountClips(definition?.deathClips),
            };

            RuntimeAnimatorController controller = definition?.animatorController;
            if (controller == null && definition != null && definition.buildAnimatorFromClips && status.HasAnyClips)
            {
                string fileName = EnemyPrefabBuilder.SanitizeFileName(
                    string.IsNullOrWhiteSpace(definition.animatorControllerFileName)
                        ? definition.prefabFileName + "Controller"
                        : definition.animatorControllerFileName,
                    definition.displayName + "Controller");
                status.ControllerPath = $"{ProjectAssetPaths.AnimationsEnemies}/{fileName}.controller";
                controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(status.ControllerPath);
            }
            else if (controller != null)
            {
                status.ControllerPath = AssetDatabase.GetAssetPath(controller);
            }

            status.HasBuiltController = controller != null;

            if (visualSource != null)
            {
                EnemyModelAvatarUtility.AvatarStatus avatarStatus = EnemyModelAvatarUtility.ResolveAvatar(visualSource);
                status.HasAvatar = avatarStatus.HasAvatar;
                status.AvatarMessage = avatarStatus.Message;
                status.ModelAssetPath = avatarStatus.ModelAssetPath;
            }
            else
            {
                status.AvatarMessage = "Assign a visual source to inspect avatar status.";
            }

            return status;
        }

        public static EnemyAnimationBuilder.BuiltAnimationSet RebuildAnimationTree(EnemyDefinition definition)
        {
            if (definition == null || !EnemyAnimationBuilder.HasClipAssignments(definition))
                return default;

            EnemyAnimationBuilder.BuiltAnimationSet builtSet = EnemyAnimationBuilder.BuildController(definition);
            if (builtSet.Controller != null)
                definition.animatorController = builtSet.Controller;

            return builtSet;
        }

        public static bool ApplyAnimationToGameObject(
            GameObject root,
            EnemyDefinition definition,
            EnemyAnimationBuilder.BuiltAnimationSet builtSet)
        {
            if (root == null || definition == null)
                return false;

            RuntimeAnimatorController controller = definition.animatorController;
            if (builtSet.Controller != null)
                controller = builtSet.Controller;

            if (controller == null)
                return false;

            Animator animator = root.GetComponent<Animator>();
            if (animator == null)
                animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null)
                animator = root.AddComponent<Animator>();

            EnemyModelAvatarUtility.AvatarStatus avatarStatus = EnemyModelAvatarUtility.ResolveAvatar(root);
            if (avatarStatus.Avatar != null)
                animator.avatar = avatarStatus.Avatar;

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (definition.addEnemyAnimationController)
                WireEnemyAnimationController(root, definition, builtSet);

            EditorUtility.SetDirty(root);
            return true;
        }

        public static bool ApplyAnimationToPrefabAsset(string prefabPath, EnemyDefinition definition)
        {
            if (string.IsNullOrEmpty(prefabPath) || definition == null)
                return false;

            PrepareForPrefabContentsEdit(prefabPath);

            EnemyAnimationBuilder.BuiltAnimationSet builtSet = default;
            if (definition.buildAnimatorFromClips && EnemyAnimationBuilder.HasClipAssignments(definition))
                builtSet = RebuildAnimationTree(definition);
            else if (definition.animatorController == null)
                return false;

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefabRoot == null)
                return false;

            bool applied = ApplyAnimationToGameObject(prefabRoot, definition, builtSet);
            if (applied)
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);

            EditorLayoutGuard.BeforeDestroySceneObject(prefabRoot);
            PrefabUtility.UnloadPrefabContents(prefabRoot);
            EditorLayoutGuard.ScheduleInspectorRecovery();

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset != null)
                Selection.activeObject = prefabAsset;

            AssetDatabase.SaveAssets();
            return applied;
        }

        public static void OpenControllerAsset(EnemyDefinition definition)
        {
            if (definition == null)
                return;

            RuntimeAnimatorController controller = definition.animatorController;
            if (controller == null && definition.buildAnimatorFromClips && EnemyAnimationBuilder.HasClipAssignments(definition))
            {
                EnemyAnimationBuilder.BuiltAnimationSet builtSet = RebuildAnimationTree(definition);
                controller = builtSet.Controller;
            }

            if (controller == null)
            {
                Debug.LogWarning("Enemy Prefab Creator: no animator controller to open. Assign clips and rebuild the animation tree first.");
                return;
            }

            Selection.activeObject = controller;
            EditorGUIUtility.PingObject(controller);
            EditorApplication.ExecuteMenuItem("Window/Animation/Animator");
        }

        public static AnimationClip ResolveClipReference(Object reference)
        {
            if (reference == null)
                return null;

            if (reference is AnimationClip clip)
                return clip;

            string path = AssetDatabase.GetAssetPath(reference);
            if (string.IsNullOrEmpty(path))
                return null;

            return EnemyAnimationClipUtility.LoadEmbeddedAnimationClip(path, "mixamo.com");
        }

        public static void WireEnemyAnimationController(
            GameObject root,
            EnemyDefinition definition,
            EnemyAnimationBuilder.BuiltAnimationSet builtSet)
        {
            EnemyAnimationController animationController = root.GetComponent<EnemyAnimationController>();
            if (animationController == null)
                animationController = root.AddComponent<EnemyAnimationController>();

            SerializedObject serialized = new SerializedObject(animationController);

            if (EnemyAnimationBuilder.HasClipAssignments(definition) && builtSet.Controller != null)
            {
                SetStringArray(serialized, "idleStateNames", builtSet.IdleStateNames);
                serialized.FindProperty("walkStateName").stringValue = builtSet.WalkStateName ?? "Walk";
                serialized.FindProperty("runStateName").stringValue = builtSet.RunStateName ?? "Run";
                SetStringArray(serialized, "attackStateNames", builtSet.AttackStateNames);
                SetStringArray(serialized, "hitStateNames", builtSet.HitStateNames);
                serialized.FindProperty("deathStateName").stringValue =
                    string.IsNullOrEmpty(builtSet.DeathStateName) ? "Death" : builtSet.DeathStateName;
            }
            else
            {
                serialized.FindProperty("walkStateName").stringValue = "Walk";
                serialized.FindProperty("runStateName").stringValue = "Run";
            }

            serialized.FindProperty("walkSpeedThreshold").floatValue = 0.05f;
            serialized.FindProperty("runSpeedThreshold").floatValue = ComputeRunSpeedThreshold(definition);
            serialized.FindProperty("lockVisualRootPosition").boolValue = definition.lockVisualRootPosition;
            serialized.FindProperty("visualChildName").stringValue = definition.visualChildName ?? "scene";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void PrepareForPrefabContentsEdit(string prefabPath)
        {
            PrefabStage openStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (openStage != null && openStage.assetPath == prefabPath)
                StageUtility.GoToMainStage();

            EditorLayoutGuard.ClearStaleSelection();
        }

        private static float ComputeRunSpeedThreshold(EnemyDefinition definition)
        {
            if (definition == null)
                return 3.2f;

            float walk = Mathf.Max(definition.walkSpeed, 0.01f);
            float run = Mathf.Max(definition.runSpeed, walk + 0.01f);
            return walk + (run - walk) * 0.55f;
        }

        private static int CountClips(AnimationClip[] clips)
        {
            if (clips == null)
                return 0;

            int count = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                    count++;
            }

            return count;
        }

        private static void SetStringArray(SerializedObject serialized, string propertyName, string[] values)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                return;

            values ??= System.Array.Empty<string>();
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).stringValue = values[i] ?? string.Empty;
        }
    }
}

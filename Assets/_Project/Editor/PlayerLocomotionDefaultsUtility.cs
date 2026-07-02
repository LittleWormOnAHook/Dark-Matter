using Project.EditorTools;
using Project.Player;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Player
{
    public static class PlayerLocomotionDefaultsUtility
    {
        private const string WalkClipPath =
            "Assets/Animations/Basic Motions/Animations/Movement/BasicMotions@Walk01.fbx";
        private const string RunClipPath =
            "Assets/Animations/Basic Motions/Animations/Movement/BasicMotions@Run01.fbx";
        private const string WalkClipName = "BasicMotions@Walk01 - Forwards";
        private const string RunClipName = "BasicMotions@Run01 - Forwards";

        [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Apply Basic Motions Locomotion To Player", false, 8)]
        public static void ApplyBasicMotionsLocomotionToSelectedPlayer()
        {
            PlayerController player = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<PlayerController>()
                : null;

            if (player == null)
            {
                EditorUtility.DisplayDialog(
                    "Locomotion Animations",
                    "Select the Player object in the hierarchy first.",
                    "OK");
                return;
            }

            ApplyDefaults(player);
            PlayerLocomotionOverrideAssetUtility.SyncOverrideAsset(player.LocomotionAnimations);

            Animator animator = player.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                PlayerLocomotionOverrideAssetUtility.AssignController(animator, player.LocomotionAnimations);
                EditorUtility.SetDirty(animator);
                if (PrefabUtility.IsPartOfPrefabInstance(player.gameObject))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            }

            EditorUtility.SetDirty(player);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "Locomotion Animations",
                "Assigned Basic Motions walk/run clips on PlayerController → Locomotion Animations.",
                "OK");
        }

        [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Apply Basic Motions Locomotion To Player Prefab", false, 9)]
        public static void ApplyBasicMotionsLocomotionToPlayerPrefab()
        {
            const string prefabPath = "Assets/_Project/Prefabs/Players/Player.prefab";
            GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabRoot == null)
            {
                EditorUtility.DisplayDialog("Locomotion Animations", $"Could not load:\n{prefabPath}", "OK");
                return;
            }

            PlayerController player = prefabRoot.GetComponent<PlayerController>();
            if (player == null)
            {
                EditorUtility.DisplayDialog("Locomotion Animations", "Player prefab has no PlayerController.", "OK");
                return;
            }

            ApplyDefaults(player);
            PlayerLocomotionOverrideAssetUtility.SyncOverrideAsset(player.LocomotionAnimations);

            Animator animator = player.GetComponentInChildren<Animator>(true);
            if (animator != null)
                PlayerLocomotionOverrideAssetUtility.AssignController(animator, player.LocomotionAnimations);

            EditorUtility.SetDirty(player);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "Locomotion Animations",
                "Player prefab locomotion clips and speeds updated.",
                "OK");
        }

        public static void ApplyDefaults(PlayerController player)
        {
            if (player == null)
                return;

            SerializedObject serialized = new SerializedObject(player);
            SerializedProperty locomotion = serialized.FindProperty("locomotionAnimations");
            if (locomotion == null)
                return;

            AnimationClip walk = LoadClip(WalkClipPath, WalkClipName);
            AnimationClip run = LoadClip(RunClipPath, RunClipName);

            if (walk != null)
                locomotion.FindPropertyRelative("walkAnimation").objectReferenceValue = walk;
            if (run != null)
                locomotion.FindPropertyRelative("runAnimation").objectReferenceValue = run;

            locomotion.FindPropertyRelative("walkAnimationSpeed").floatValue = 1f;
            locomotion.FindPropertyRelative("runAnimationSpeed").floatValue = 0.78f;
            locomotion.FindPropertyRelative("walkBlendForward").floatValue = 0.5f;
            locomotion.FindPropertyRelative("runBlendForward").floatValue = 0.92f;
            locomotion.FindPropertyRelative("locomotionSmoothTime").floatValue = 0.18f;
            locomotion.FindPropertyRelative("locomotionAnimSpeedSmoothTime").floatValue = 0.12f;
            locomotion.FindPropertyRelative("layerWeightSmoothSpeed").floatValue = 8f;
            locomotion.FindPropertyRelative("baseLocomotionCrossFadeTime").floatValue = 0.15f;

            serialized.FindProperty("walkSpeed").floatValue = 5.2f;
            serialized.FindProperty("sprintSpeed").floatValue = 7.2f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

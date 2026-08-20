#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Invector;
using Project.Data;
using Project.EditorTools;
using Project.Player.Invector;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools.Invector
{
    /// <summary>
    /// Restores Player_Invector skeleton, body snaps, and weapon slot transforms after bad Play Mode captures.
    /// </summary>
    public static class PlayerInvectorPoseResetUtility
    {
        private const string OutputPlayerPrefabPath = "Assets/_Project/Prefabs/Players/Player_Invector.prefab";
        private const string SourceInvectorPrefabPath =
            "Assets/Invector-3rdPersonController/Shooter/Prefabs/Player/vShooterMelee_NoInventory.prefab";

        [MenuItem(DarkMatterGenesisEditorMenus.Combat + "Reset Player_Invector T-Pose & Weapon Slots", false, 126)]
        public static void ResetPlayerInvectorPoseAndWeaponSlots()
        {
            if (!EditorUtility.DisplayDialog(
                    "Reset Player_Invector",
                    "Restore T-pose skeleton, body snaps, and weapon slot transforms on Player_Invector? " +
                    "Pending Play Mode Saver player rig data will be cleared.",
                    "Reset",
                    "Cancel"))
            {
                return;
            }

            ResetPlayerInvectorPoseAndWeaponSlotsImmediate();
        }

        public static void ResetPlayerInvectorPoseAndWeaponSlotsImmediate()
        {
            PlayModeEditPersistence.ClearPendingSnapshot();
            ResetPlayerPrefab();
            ResetOpenScenePlayerInstances();
            AssetDatabase.SaveAssets();
            Debug.Log("[Player_Invector] Reset T-pose, body snaps, and weapon slot transforms.");
        }

        private static void ResetPlayerPrefab()
        {
            GameObject sourceRoot = PrefabUtility.LoadPrefabContents(SourceInvectorPrefabPath);
            GameObject playerRoot = PrefabUtility.LoadPrefabContents(OutputPlayerPrefabPath);
            if (sourceRoot == null || playerRoot == null)
            {
                Debug.LogError("[Player_Invector] Missing source or output prefab.");
                if (sourceRoot != null)
                    PrefabUtility.UnloadPrefabContents(sourceRoot);
                if (playerRoot != null)
                    PrefabUtility.UnloadPrefabContents(playerRoot);
                return;
            }

            try
            {
                CopyMatchingSkeletonAndSnaps(sourceRoot.transform, playerRoot.transform);
                PioneerInvectorPlayerSetupUtility.ResetWeaponSlotTransformsFromItemData(playerRoot);
                Project.AI.Invector.EnemyInvectorBodySnapSetupEditor.ConfigureEditor(playerRoot);
                PrefabUtility.SaveAsPrefabAsset(playerRoot, OutputPlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(sourceRoot);
                PrefabUtility.UnloadPrefabContents(playerRoot);
            }
        }

        private static void ResetOpenScenePlayerInstances()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                GameObject[] roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                    ResetPlayerHierarchyInScene(roots[r], scene);
            }
        }

        private static void ResetPlayerHierarchyInScene(GameObject root, Scene scene)
        {
            if (root == null)
                return;

            bool isPlayer = root.CompareTag("Player")
                || root.name.IndexOf("Player_Invector", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isPlayer && PrefabUtility.IsPartOfPrefabInstance(root))
            {
                PrefabUtility.RevertPrefabInstance(root, InteractionMode.UserAction);
                EditorSceneManager.MarkSceneDirty(scene);
                return;
            }

            for (int i = 0; i < root.transform.childCount; i++)
                ResetPlayerHierarchyInScene(root.transform.GetChild(i).gameObject, scene);
        }

        private static void CopyMatchingSkeletonAndSnaps(Transform sourceRoot, Transform targetRoot)
        {
            Dictionary<string, Transform> sourceLookup = BuildRelativePathLookup(sourceRoot);
            CopyMatchingTransformsRecursive(targetRoot, targetRoot, sourceLookup);
        }

        private static void CopyMatchingTransformsRecursive(
            Transform target,
            Transform targetRoot,
            Dictionary<string, Transform> sourceLookup)
        {
            if (target == null)
                return;

            string relativePath = GetRelativePath(target, targetRoot);
            if (ShouldCopyTransform(target.name, relativePath)
                && sourceLookup.TryGetValue(relativePath, out Transform sourceTransform))
            {
                target.localPosition = sourceTransform.localPosition;
                target.localRotation = sourceTransform.localRotation;
                target.localScale = sourceTransform.localScale;
            }

            for (int i = 0; i < target.childCount; i++)
                CopyMatchingTransformsRecursive(target.GetChild(i), targetRoot, sourceLookup);
        }

        private static bool ShouldCopyTransform(string transformName, string relativePath)
        {
            if (string.IsNullOrEmpty(transformName))
                return false;

            if (relativePath.IndexOf("/BodySnaps/", StringComparison.Ordinal) >= 0
                || relativePath.EndsWith("/BodySnaps", StringComparison.Ordinal)
                || relativePath.IndexOf("/WeaponHolders/", StringComparison.Ordinal) >= 0
                || relativePath.EndsWith("/WeaponHolders", StringComparison.Ordinal))
            {
                return true;
            }

            if (transformName.StartsWith("VBOT_", StringComparison.Ordinal))
                return true;

            return transformName == "RifleHolder" || transformName == "HandgunHolder";
        }

        private static Dictionary<string, Transform> BuildRelativePathLookup(Transform root)
        {
            Dictionary<string, Transform> lookup = new Dictionary<string, Transform>(StringComparer.Ordinal);
            IndexRelativePaths(root, root, lookup);
            return lookup;
        }

        private static void IndexRelativePaths(
            Transform transform,
            Transform root,
            Dictionary<string, Transform> lookup)
        {
            if (transform == null)
                return;

            lookup[GetRelativePath(transform, root)] = transform;
            for (int i = 0; i < transform.childCount; i++)
                IndexRelativePaths(transform.GetChild(i), root, lookup);
        }

        private static string GetRelativePath(Transform transform, Transform root)
        {
            if (transform == null || root == null)
                return string.Empty;

            if (transform == root)
                return string.Empty;

            string parentPath = GetRelativePath(transform.parent, root);
            return string.IsNullOrEmpty(parentPath)
                ? transform.name
                : parentPath + "/" + transform.name;
        }
    }
}
#endif

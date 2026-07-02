using Project.EditorTools;
using Project.Player;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Player
{
    public static class ProjectUnityCharacterSetupUtility
    {
        private const string Ecm2CharacterPath = "Assets/ECM2/Shared Assets/Prefabs/UnityCharacter.prefab";
        private const string ProjectCharacterPath = "Assets/_Project/Prefabs/Players/ProjectUnityCharacter.prefab";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Players/Player.prefab";

        [MenuItem(SurvivalPioneerEditorMenus.CombatAnimations + "Create Project Unity Character Prefab", false, 1)]
        public static void CreateProjectUnityCharacterPrefabMenu()
        {
            if (!CreateOrUpdateProjectUnityCharacter(showDialog: true))
            {
                EditorUtility.DisplayDialog(
                    "Project Unity Character",
                    "Could not create or update the project character prefab. Check the Console.",
                    "OK");
            }
        }

        public static bool CreateOrUpdateProjectUnityCharacter(bool showDialog)
        {
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Ecm2CharacterPath);
            if (sourcePrefab == null)
            {
                Debug.LogError($"ProjectUnityCharacterSetupUtility: Missing source prefab at {Ecm2CharacterPath}");
                return false;
            }

            bool createdAsset = false;
            GameObject instance = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectCharacterPath);
            if (instance == null)
            {
                instance = Object.Instantiate(sourcePrefab);
                instance.name = "ProjectUnityCharacter";
                createdAsset = true;
            }
            else
            {
                instance = PrefabUtility.LoadPrefabContents(ProjectCharacterPath);
            }

            bool changed = createdAsset;
            try
            {
                Transform root = instance.transform;
                if (root.name != "ProjectUnityCharacter")
                {
                    root.name = "ProjectUnityCharacter";
                    changed = true;
                }

                Animator animator = instance.GetComponent<Animator>();
                if (animator != null)
                {
                    RuntimeAnimatorController targetController =
                        PlayerLocomotionOverrideAssetUtility.ResolveControllerForSettings(null);
                    if (targetController != null && animator.runtimeAnimatorController != targetController)
                    {
                        animator.runtimeAnimatorController = targetController;
                        changed = true;
                    }

                    if (animator.applyRootMotion)
                    {
                        animator.applyRootMotion = false;
                        changed = true;
                    }
                }

                if (instance.GetComponent<PlayerGkcAnimatorDriver>() == null)
                {
                    PlayerGkcAnimatorDriver driver = instance.AddComponent<PlayerGkcAnimatorDriver>();
                    GkcActionCatalog catalog = AssetDatabase.LoadAssetAtPath<GkcActionCatalog>(
                        "Assets/_Project/Data/Animation/GkcActionCatalog.asset");
                    if (catalog != null)
                    {
                        SerializedObject so = new SerializedObject(driver);
                        so.FindProperty("actionCatalog").objectReferenceValue = catalog;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    changed = true;
                }

                if (createdAsset || changed)
                    PrefabUtility.SaveAsPrefabAsset(instance, ProjectCharacterPath);
            }
            finally
            {
                if (createdAsset)
                    Object.DestroyImmediate(instance);
                else
                    PrefabUtility.UnloadPrefabContents(instance);
            }

            bool playerChanged = RepointPlayerPrefabToProjectCharacter();
            changed |= playerChanged;

            if (changed)
                AssetDatabase.SaveAssets();

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Project Unity Character",
                    changed
                        ? "Created/updated ProjectUnityCharacter and repointed the Player prefab."
                        : "ProjectUnityCharacter and Player prefab were already configured.",
                    "OK");
            }

            return changed;
        }

        public static bool RepointPlayerPrefabToProjectCharacter()
        {
            GameObject projectCharacterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectCharacterPath);
            if (projectCharacterPrefab == null)
                return false;

            GameObject playerRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            if (playerRoot == null)
                return false;

            bool changed = false;
            try
            {
                Transform unityCharacter = FindUnityCharacterTransform(playerRoot.transform);
                if (unityCharacter == null)
                    return false;

                GameObject nestedInstance = unityCharacter.gameObject;
                if (PrefabUtility.GetCorrespondingObjectFromSource(nestedInstance) == projectCharacterPrefab)
                    return false;

                Transform parent = unityCharacter.parent;
                Vector3 localPosition = unityCharacter.localPosition;
                Quaternion localRotation = unityCharacter.localRotation;
                Vector3 localScale = unityCharacter.localScale;
                string name = unityCharacter.name;

                Object.DestroyImmediate(nestedInstance);

                GameObject replacement = PrefabUtility.InstantiatePrefab(projectCharacterPrefab, parent) as GameObject;
                if (replacement == null)
                    return false;

                Transform replacementTransform = replacement.transform;
                replacementTransform.localPosition = localPosition;
                replacementTransform.localRotation = localRotation;
                replacementTransform.localScale = localScale;
                replacementTransform.name = name;
                changed = true;

                PrefabUtility.SaveAsPrefabAsset(playerRoot, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(playerRoot);
            }

            return changed;
        }

        private static Transform FindUnityCharacterTransform(Transform root)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == "UnityCharacter" || child.name == "ProjectUnityCharacter")
                    return child;

                Transform nested = FindUnityCharacterTransform(child);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}

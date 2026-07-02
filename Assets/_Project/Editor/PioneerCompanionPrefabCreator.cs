using Project.Companions;
using Project.EditorTools.Player;
using Project.Player;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class PioneerCompanionPrefabCreator
    {
        private const string OutputPrefabPath = PioneerCompanionDefaults.DefaultPrefabAssetPath;
        private const string ResourcesPrefabPath = "Assets/_Project/Resources/Companions/PioneerCompanion.prefab";

        [MenuItem(SurvivalPioneerEditorMenus.Content + "Create Pioneer Companion Prefab (Demo)", false, 25)]
        public static void CreateDemoPioneerCompanionPrefab()
        {
            EnsureFolder("Assets/_Project/Prefabs/Companions");
            EnsureFolder("Assets/_Project/Resources/Companions");

            GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PioneerCompanionDefaults.CharacterModelPrefabPath);
            if (characterPrefab == null)
            {
                Debug.LogError(
                    $"PioneerCompanionPrefabCreator: Missing character prefab at {PioneerCompanionDefaults.CharacterModelPrefabPath}");
                return;
            }

            GameObject root = new GameObject("PioneerCompanion");
            try
            {
                GameObject modelInstance = PrefabUtility.InstantiatePrefab(characterPrefab, root.transform) as GameObject;
                if (modelInstance == null)
                {
                    Debug.LogError("PioneerCompanionPrefabCreator: Failed to instantiate ProjectUnityCharacter prefab.");
                    return;
                }

                modelInstance.name = "ProjectUnityCharacter";
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one;

                Animator animator = modelInstance.GetComponent<Animator>();
                if (animator == null)
                    animator = modelInstance.GetComponentInChildren<Animator>(true);

                RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    PioneerCompanionDefaults.PioneerControllerAssetPath);
                if (controller == null)
                {
                    controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                        PlayerAnimatorControllerPaths.GkcControllerPath);
                }
                if (animator != null && controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false;
                }
                else
                {
                    Debug.LogWarning(
                        "PioneerCompanionPrefabCreator: Animator controller not assigned. Run Repair Player Animator Setup first.");
                }

                root.AddComponent<PioneerCompanionAgent>();
                root.AddComponent<CompanionFollowController>();

                CapsuleCollider bodyCollider = root.AddComponent<CapsuleCollider>();
                bodyCollider.radius = 0.42f;
                bodyCollider.height = 1.75f;
                bodyCollider.center = new Vector3(0f, 0.875f, 0f);
                bodyCollider.isTrigger = true;

                root.AddComponent<CompanionAnimationDriver>();
                root.AddComponent<CompanionCombatController>();
                root.AddComponent<CompanionThreatSensor>();
                root.AddComponent<CompanionEquipmentVisual>();
                root.AddComponent<CompanionSenseController>();
                root.AddComponent<PioneerCompanionVisualProfile>();

                CompanionModelSanitizer.StripPlayerComponents(root);

                SavePrefab(root, OutputPrefabPath);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(ResourcesPrefabPath) != null)
                    AssetDatabase.DeleteAsset(ResourcesPrefabPath);
                AssetDatabase.CopyAsset(OutputPrefabPath, ResourcesPrefabPath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log(
                    $"Pioneer companion demo prefab created at {OutputPrefabPath} and {ResourcesPrefabPath}. " +
                    "Expedition trio companions use PioneerController (independent copy of the player animator).");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SavePrefab(GameObject source, string assetPath)
        {
            if (source == null)
                return;

            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (existing != null)
                PrefabUtility.SaveAsPrefabAsset(source, assetPath);
            else
                PrefabUtility.SaveAsPrefabAsset(source, assetPath);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}

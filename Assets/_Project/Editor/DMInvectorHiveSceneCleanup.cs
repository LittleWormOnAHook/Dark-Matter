#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools
{
    /// <summary>
    /// Removes leftover Invector demo Hive environment instances from scenes.
    /// No runtime code references these assets — they are scene-only prefab placements.
    /// </summary>
    public static class DMInvectorHiveSceneCleanup
    {
        private static readonly string[] HiveObjectNames =
        {
            "Basic_Hive",
            "The Hive",
            "Hive",
            "Shooter_Hive",
            "MeleeCombat_Hive",
            "BasicLocomotion_Hive"
        };

        private static readonly string[] HivePrefabGuids =
        {
            "063c2094dce9b804ea8a45f866dfe0ea", // Basic_Hive.prefab
            "40e1590d7bdf576488cbbdb99660dd93"  // legacy hive environment pack
        };

        [MenuItem("Dark Matter/Scene/Remove Invector Hive Instances")]
        public static void RemoveHiveInstancesFromOpenScene()
        {
            int removed = RemoveHiveInstances(SceneManager.GetActiveScene());
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[DMInvectorHiveSceneCleanup] Removed {removed} Invector Hive object(s) from '{SceneManager.GetActiveScene().path}'.");
        }

        public static int RemoveHiveInstances(Scene scene)
        {
            if (!scene.IsValid())
                return 0;

            int removed = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
                removed += RemoveHiveRecursive(roots[i].transform);

            return removed;
        }

        private static int RemoveHiveRecursive(Transform node)
        {
            if (node == null)
                return 0;

            int removed = 0;
            for (int i = node.childCount - 1; i >= 0; i--)
                removed += RemoveHiveRecursive(node.GetChild(i));

            if (IsHiveInstance(node.gameObject))
            {
                Object.DestroyImmediate(node.gameObject);
                return removed + 1;
            }

            return removed;
        }

        private static bool IsHiveInstance(GameObject go)
        {
            if (go == null)
                return false;

            for (int n = 0; n < HiveObjectNames.Length; n++)
            {
                if (go.name == HiveObjectNames[n])
                    return true;
            }

            if (!PrefabUtility.IsPartOfPrefabInstance(go))
                return false;

            string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            if (!string.IsNullOrEmpty(assetPath))
            {
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                for (int g = 0; g < HivePrefabGuids.Length; g++)
                {
                    if (guid == HivePrefabGuids[g])
                        return true;
                }

                if (assetPath.IndexOf("Hive", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }
    }
}
#endif

#if UNITY_EDITOR
using Project.AI;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Project.EditorTools
{
    public static class EnemyNavMeshStripUtility
    {
        [MenuItem(SurvivalPioneerEditorMenus.Combat + "Strip NavMesh From Combat Prefabs", false, 21)]
        public static void StripNavMeshFromCombatPrefabs()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { ProjectAssetPaths.PrefabsCombat });
            int updated = 0;

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                if (!path.EndsWith(".prefab"))
                    continue;

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root == null || root.GetComponent<EnemyHealth>() == null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                    continue;
                }

                bool changed = StripNavMeshFromRoot(root);
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    updated++;
                    Debug.Log($"Stripped NavMeshAgent from {path}");
                }

                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "NavMesh Strip",
                updated > 0
                    ? $"Removed NavMeshAgent from {updated} combat prefab(s) and disabled NavMesh on EnemyAiController."
                    : "No combat prefabs required NavMesh stripping.",
                "OK");
        }

        public static bool StripNavMeshFromRoot(GameObject root)
        {
            if (root == null)
                return false;

            bool changed = false;

            NavMeshAgent[] agents = root.GetComponentsInChildren<NavMeshAgent>(true);
            for (int i = 0; i < agents.Length; i++)
            {
                if (agents[i] == null)
                    continue;

                Object.DestroyImmediate(agents[i], true);
                changed = true;
            }

            EnemyAiController ai = root.GetComponent<EnemyAiController>();
            if (ai != null)
            {
                SerializedObject serialized = new SerializedObject(ai);
                SerializedProperty useNav = serialized.FindProperty("useNavMeshForChaseAndWander");
                if (useNav != null && useNav.boolValue)
                {
                    useNav.boolValue = false;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }
            }

            return changed;
        }
    }
}
#endif

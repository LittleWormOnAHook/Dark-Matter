#if UNITY_EDITOR
using MalbersAnimations.PathCreation;
using Project.AI;
using Project.Creatures;
using Project.Pet;
using Project.World;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Shared Path Creator assignment for Enemy / Creature / Pet manager windows.
    /// Scene Path Creators apply to scene instances; persistent Path Creator assets can also bake onto prefabs.
    /// </summary>
    public static class DMIPathFollowEditorUtility
    {
        public static bool IsPersistentPath(PathCreator path)
        {
            return path != null && EditorUtility.IsPersistent(path);
        }

        public static int ApplyToEnemies(PathCreator path, params GameObject[] extraRoots)
        {
            DMIPathFollowProvider provider = DMIPathFollowBinding.Resolve(path);
            if (path == null || provider == null)
                return 0;

            int applied = 0;
            applied += ApplyOnRoots(
                CollectRoots(extraRoots),
                go =>
                {
                    EnemyAiController ai = go.GetComponentInParent<EnemyAiController>()
                        ?? go.GetComponentInChildren<EnemyAiController>(true);
                    if (ai == null)
                        return false;

                    Undo.RecordObject(ai, "Assign Enemy Patrol Path");
                    ai.SetPatrolPath(path, provider);
                    EditorUtility.SetDirty(ai);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(ai);
                    return true;
                });

            if (IsPersistentPath(path))
                applied += ApplyOnPrefabAssets<EnemyAiController>(
                    (ai, so) =>
                    {
                        so.FindProperty("patrolPath").objectReferenceValue = path;
                        so.FindProperty("patrolPathProvider").objectReferenceValue = provider;
                        so.FindProperty("movementMode").intValue = (int)EnemyMovementMode.Patrol;
                    });

            return applied;
        }

        public static int ApplyToCreatures(PathCreator path, params GameObject[] extraRoots)
        {
            DMIPathFollowProvider provider = DMIPathFollowBinding.Resolve(path);
            if (path == null || provider == null)
                return 0;

            int applied = 0;
            applied += ApplyOnRoots(
                CollectRoots(extraRoots),
                go =>
                {
                    DMICreatureAiController ai = go.GetComponentInParent<DMICreatureAiController>()
                        ?? go.GetComponentInChildren<DMICreatureAiController>(true);
                    if (ai == null)
                        return false;

                    Undo.RecordObject(ai, "Assign Creature Patrol Path");
                    ai.SetPatrolPath(path, provider);
                    EditorUtility.SetDirty(ai);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(ai);
                    return true;
                });

            if (IsPersistentPath(path))
                applied += ApplyOnPrefabAssets<DMICreatureAiController>(
                    (ai, so) =>
                    {
                        so.FindProperty("patrolPath").objectReferenceValue = path;
                        so.FindProperty("patrolPathProvider").objectReferenceValue = provider;
                        so.FindProperty("movementMode").intValue = (int)DMICreatureMovementMode.Patrol;
                    });

            return applied;
        }

        public static int ApplyToPets(PathCreator path, bool enablePathFollow, params GameObject[] extraRoots)
        {
            return ApplyToPets(path, enablePathFollow, DMIPathPatrolMode.Loop, 2f, extraRoots);
        }

        public static int ApplyToPets(
            PathCreator path,
            bool enablePathFollow,
            DMIPathPatrolMode patrolMode,
            float patrolWaitDuration,
            params GameObject[] extraRoots)
        {
            DMIPathFollowProvider provider = DMIPathFollowBinding.Resolve(path);
            if (path == null || provider == null)
                return 0;

            Undo.RecordObject(provider, "Configure Pet Path Patrol");
            provider.ConfigurePatrol(patrolMode, patrolWaitDuration);
            EditorUtility.SetDirty(provider);

            int applied = 0;
            applied += ApplyOnRoots(
                CollectRoots(extraRoots),
                go =>
                {
                    PetController pet = go.GetComponentInParent<PetController>()
                        ?? go.GetComponentInChildren<PetController>(true);
                    if (pet == null)
                        return false;

                    Undo.RecordObject(pet, "Assign Pet Patrol Path");
                    pet.ConfigurePathPatrol(patrolMode, patrolWaitDuration);
                    pet.SetPatrolPath(path, provider, enablePathFollow);
                    EditorUtility.SetDirty(pet);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(pet);
                    return true;
                });

            if (IsPersistentPath(path))
                applied += ApplyOnPrefabAssets<PetController>(
                    (pet, so) =>
                    {
                        so.FindProperty("patrolPath").objectReferenceValue = path;
                        so.FindProperty("patrolPathProvider").objectReferenceValue = provider;
                        so.FindProperty("pathFollowEnabled").boolValue = enablePathFollow;
                        SerializedProperty modeProp = so.FindProperty("pathPatrolMode");
                        if (modeProp != null)
                            modeProp.enumValueIndex = (int)patrolMode;
                        SerializedProperty waitProp = so.FindProperty("pathPatrolWaitDuration");
                        if (waitProp != null)
                            waitProp.floatValue = Mathf.Max(0f, patrolWaitDuration);
                    });

            return applied;
        }

        /// <summary>
        /// Writes path-follow fields onto an open prefab contents root (no scene refs unless path is persistent).
        /// </summary>
        public static bool TryWritePetPathOnPrefabRoot(
            GameObject prefabRoot,
            PathCreator path,
            bool enablePathFollow,
            DMIPathPatrolMode patrolMode = DMIPathPatrolMode.Loop,
            float patrolWaitDuration = 2f)
        {
            if (prefabRoot == null)
                return false;

            PetController pet = prefabRoot.GetComponent<PetController>()
                ?? prefabRoot.GetComponentInChildren<PetController>(true);
            if (pet == null)
                return false;

            DMIPathFollowProvider provider = path != null ? DMIPathFollowBinding.Resolve(path) : null;
            if (provider != null)
            {
                provider.ConfigurePatrol(patrolMode, patrolWaitDuration);
                EditorUtility.SetDirty(provider);
            }

            SerializedObject so = new SerializedObject(pet);
            SerializedProperty enabledProp = so.FindProperty("pathFollowEnabled");
            if (enabledProp != null)
                enabledProp.boolValue = enablePathFollow;

            SerializedProperty modeProp = so.FindProperty("pathPatrolMode");
            if (modeProp != null)
                modeProp.enumValueIndex = (int)patrolMode;

            SerializedProperty waitProp = so.FindProperty("pathPatrolWaitDuration");
            if (waitProp != null)
                waitProp.floatValue = Mathf.Max(0f, patrolWaitDuration);

            if (path != null && IsPersistentPath(path))
            {
                SerializedProperty pathProp = so.FindProperty("patrolPath");
                SerializedProperty providerProp = so.FindProperty("patrolPathProvider");
                if (pathProp != null)
                    pathProp.objectReferenceValue = path;
                if (providerProp != null)
                    providerProp.objectReferenceValue = provider;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        private static GameObject[] CollectRoots(GameObject[] extraRoots)
        {
            var list = new System.Collections.Generic.List<GameObject>();
            GameObject[] selected = Selection.gameObjects;
            if (selected != null)
            {
                for (int i = 0; i < selected.Length; i++)
                {
                    if (selected[i] != null && !list.Contains(selected[i]))
                        list.Add(selected[i]);
                }
            }

            if (extraRoots != null)
            {
                for (int i = 0; i < extraRoots.Length; i++)
                {
                    if (extraRoots[i] != null && !list.Contains(extraRoots[i]))
                        list.Add(extraRoots[i]);
                }
            }

            return list.ToArray();
        }

        private static int ApplyOnRoots(GameObject[] roots, System.Func<GameObject, bool> apply)
        {
            int applied = 0;
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && apply(roots[i]))
                    applied++;
            }

            return applied;
        }

        private static int ApplyOnPrefabAssets<T>(System.Action<T, SerializedObject> write)
            where T : Component
        {
            int applied = 0;
            Object[] selected = Selection.objects;
            for (int i = 0; i < selected.Length; i++)
            {
                GameObject go = selected[i] as GameObject;
                if (go == null)
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(go);
                if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                GameObject root = PrefabUtility.LoadPrefabContents(assetPath);
                try
                {
                    T[] comps = root.GetComponentsInChildren<T>(true);
                    if (comps == null || comps.Length == 0)
                        continue;

                    for (int c = 0; c < comps.Length; c++)
                    {
                        SerializedObject so = new SerializedObject(comps[c]);
                        write(comps[c], so);
                        so.ApplyModifiedPropertiesWithoutUndo();
                        applied++;
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            return applied;
        }
    }
}
#endif

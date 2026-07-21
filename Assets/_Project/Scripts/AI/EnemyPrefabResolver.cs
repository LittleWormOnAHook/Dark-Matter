using Project.AI.Invector;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Reads spawn configuration baked onto enemy prefabs (bootstrap definition, loadout, gameplay stack).
    /// </summary>
    public static class EnemyPrefabResolver
    {
        public static EnemyDefinition GetDefinition(GameObject prefab)
        {
            if (prefab == null)
                return null;

            EnemyInvectorBootstrap bootstrap = prefab.GetComponent<EnemyInvectorBootstrap>();
            if (bootstrap != null)
                return bootstrap.Definition;

            return null;
        }

        public static bool IsSpawnReady(GameObject prefab)
        {
            if (prefab == null)
                return false;

            if (prefab.GetComponent<EnemyInvectorBootstrap>() != null)
            {
                return prefab.GetComponent<EnemyHealth>() != null &&
                       prefab.GetComponent<EnemyAiController>() != null &&
                       prefab.GetComponent<EnemyCombat>() != null;
            }

            return prefab.GetComponent<EnemyHealth>() != null &&
                   prefab.GetComponent<EnemyAiController>() != null;
        }
    }
}

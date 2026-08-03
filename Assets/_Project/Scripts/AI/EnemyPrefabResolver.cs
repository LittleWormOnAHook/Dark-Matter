using Project.AI.Invector;
using Project.Creatures;
using UnityEngine;
using UnityEngine.AI;

namespace Project.AI
{
    /// <summary>
    /// Reads spawn configuration baked onto enemy prefabs (bootstrap definition, loadout, gameplay stack).
    /// Also recognizes DMI creatures — RiggedNative or Legacy Malbers (bridge + health + NavMeshAgent).
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

            // DMI creatures (RiggedNative / Legacy Malbers): EnemyHealth + bridge + NavMeshAgent.
            if (prefab.GetComponent<DMICreatureBridge>() != null)
            {
                bool hasAgent = prefab.GetComponentInChildren<NavMeshAgent>(true) != null;
                bool hasHealth = prefab.GetComponent<EnemyHealth>() != null;
                bool hasAi = prefab.GetComponent<DMICreatureAiController>() != null
                             || prefab.GetComponentInChildren<NavMeshAgent>(true) != null;
                return hasHealth && hasAgent && hasAi;
            }

            return prefab.GetComponent<EnemyHealth>() != null &&
                   prefab.GetComponent<EnemyAiController>() != null;
        }
    }
}

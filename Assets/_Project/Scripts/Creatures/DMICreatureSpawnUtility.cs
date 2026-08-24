using Project.AI;
using UnityEngine;
using UnityEngine.AI;

namespace Project.Creatures
{
    /// <summary>
    /// Lightweight spawn helper for RiggedNative / DMI creatures.
    /// Instantiates the prefab as-authored — does <b>not</b> run
    /// <c>EnemyInvectorGameplaySetup</c> / humanoid <c>EnemyAiController</c> stacking.
    /// </summary>
    public static class DMICreatureSpawnUtility
    {
        public static GameObject Spawn(
            GameObject prefab,
            Vector3 worldPosition,
            Quaternion rotation,
            Transform parent = null,
            bool snapToGround = true)
        {
            if (prefab == null)
                return null;

            if (snapToGround)
                worldPosition = EnemyGroundUtility.SnapPositionToGround(worldPosition);

            GameObject instance = Object.Instantiate(prefab, worldPosition, rotation, parent);
            instance.name = prefab.name;

            WarmNavMeshAgent(instance, worldPosition);
            return instance;
        }

        public static GameObject SpawnAround(
            GameObject prefab,
            Vector3 center,
            float radius,
            Transform parent = null,
            bool snapToGround = true)
        {
            Vector2 disk = Random.insideUnitCircle * Mathf.Max(0f, radius);
            Vector3 pos = center + new Vector3(disk.x, 0f, disk.y);
            float yaw = Random.Range(0f, 360f);
            return Spawn(prefab, pos, Quaternion.Euler(0f, yaw, 0f), parent, snapToGround);
        }

        private static void WarmNavMeshAgent(GameObject instance, Vector3 worldPosition)
        {
            if (instance == null)
                return;

            NavMeshAgent agent = instance.GetComponent<NavMeshAgent>();
            if (agent == null)
                return;

            NavMeshAgentSafeBoot.PrepareAgent(instance, 12f);
            if (NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, 12f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }
    }
}

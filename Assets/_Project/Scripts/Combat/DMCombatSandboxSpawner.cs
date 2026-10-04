using System.Collections.Generic;
using Project.AI;
using Project.AI.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>Simple combat sandbox spawner — one humanoid at a time, capped count.</summary>
    public sealed class DMCombatSandboxSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject humanoidPrefab;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float spawnIntervalSeconds = 12f;
        [SerializeField] private int maxAlive = 1;
        [SerializeField] private float groundSnapRayHeight = 4f;
        [SerializeField] private float groundSnapRayDistance = 12f;
        [SerializeField] private LayerMask groundMask = ~0;

        private float nextSpawnTime;
        private readonly List<EnemyHealth> spawnedEnemies = new List<EnemyHealth>();

        private void Update()
        {
            if (humanoidPrefab == null || spawnPoint == null)
                return;

            if (Time.time < nextSpawnTime)
                return;

            if (CountAlive() >= maxAlive)
                return;

            nextSpawnTime = Time.time + spawnIntervalSeconds;
            Vector3 spawnPosition = spawnPoint.position;
            float standLift = EnemyGroundUtility.ResolveCapsuleStandLift(humanoidPrefab);
            float minAcceptedY = spawnPoint.position.y;
            if (EnemyGroundUtility.TryGetGroundY(
                    spawnPosition,
                    out float groundY,
                    0f,
                    ignoreRoot: null,
                    minAcceptedY: float.NegativeInfinity))
            {
                minAcceptedY = groundY;
                spawnPosition.y = groundY + standLift;
            }
            else if (Physics.Raycast(
                    spawnPosition + Vector3.up * groundSnapRayHeight,
                    Vector3.down,
                    out RaycastHit hit,
                    groundSnapRayDistance,
                    ResolveTerrainMask(),
                    QueryTriggerInteraction.Ignore))
            {
                if (!IsIgnoredSpawnHit(hit.collider))
                {
                    minAcceptedY = hit.point.y;
                    spawnPosition = hit.point + Vector3.up * standLift;
                }
                else
                {
                    spawnPosition.y = Mathf.Max(spawnPosition.y, minAcceptedY) + standLift;
                }
            }
            else
            {
                spawnPosition.y = Mathf.Max(spawnPosition.y, minAcceptedY) + standLift;
            }

            spawnPosition = NudgeOffOverlaps(spawnPosition);

            GameObject instance = Instantiate(humanoidPrefab, spawnPosition, spawnPoint.rotation);
            if (instance.GetComponent<CombatPoise>() == null)
                instance.AddComponent<CombatPoise>();

            EnemyGroundUtility.SnapCreatureToGround(instance.transform, instance.transform.position, minAcceptedY);
            HumanoidPerformanceController.ForceSpawnVisible(instance);
            DMSpawnPhysicsStabilizer.EnsureOn(instance);

            EnemyHealth health = instance.GetComponent<EnemyHealth>();
            if (health != null)
                spawnedEnemies.Add(health);
        }

        private LayerMask ResolveTerrainMask()
        {
            int terrainLayer = LayerMask.NameToLayer("Terrain");
            if (terrainLayer >= 0)
                return 1 << terrainLayer;

            return groundMask;
        }

        private static Vector3 NudgeOffOverlaps(Vector3 spawnPosition)
        {
            const float probeRadius = 0.55f;
            Collider[] overlaps = Physics.OverlapSphere(
                spawnPosition + Vector3.up,
                probeRadius,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider col = overlaps[i];
                if (col == null || col is TerrainCollider || col is MeshCollider)
                    continue;

                spawnPosition += Vector3.right * 1.25f;
                break;
            }

            return spawnPosition;
        }

        private static bool IsIgnoredSpawnHit(Collider col)
        {
            if (col == null)
                return true;
            if (col is TerrainCollider || col is MeshCollider)
                return false;
            if (col.CompareTag("Terrain"))
                return false;

            string n = col.gameObject.name;
            if (n.IndexOf("Dummy", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (col.GetComponentInParent<EnemyHealth>() != null)
                return true;
            return col.GetComponentInParent<CharacterController>() != null;
        }

        private int CountAlive()
        {
            PruneSpawnedList();
            int count = 0;
            for (int i = 0; i < spawnedEnemies.Count; i++)
            {
                EnemyHealth health = spawnedEnemies[i];
                if (health != null && health.gameObject.activeInHierarchy && !health.IsDead)
                    count++;
            }

            return count;
        }

        private void PruneSpawnedList()
        {
            for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
            {
                EnemyHealth health = spawnedEnemies[i];
                if (health == null || health.IsDead)
                    spawnedEnemies.RemoveAt(i);
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Project.AI;
using Project.AI.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Combat sandbox spawner. Default mode keeps one live instance per prefab type, fills the roster
    /// sequentially, and respawns each type on its own timer after death.
    /// </summary>
    public sealed class DMCombatSandboxSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject humanoidPrefab;
        [SerializeField] private GameObject[] additionalPawnablePrefabs;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float spawnIntervalSeconds = 12f;
        [SerializeField] private float additionalSpawnIntervalSeconds = 3f;
        [SerializeField] private int maxAlive = 1;
        [SerializeField] private bool oneAlivePerPrefabType = true;
        [SerializeField] private float groundSnapRayHeight = 4f;
        [SerializeField] private float groundSnapRayDistance = 12f;
        [SerializeField] private LayerMask groundMask = ~0;

        private float nextSpawnTime;
        private float nextAdditionalSpawnTime;
        private int additionalPrefabCycleIndex;
        private readonly List<EnemyHealth> spawnedEnemies = new List<EnemyHealth>();
        private readonly List<SandboxSlot> slots = new List<SandboxSlot>(8);
        private Coroutine initialFillRoutine;

        private void OnEnable()
        {
            if (oneAlivePerPrefabType)
            {
                RebuildRosterSlots();
                initialFillRoutine = StartCoroutine(InitialFillRoutine());
                return;
            }

            nextSpawnTime = Time.time;
            nextAdditionalSpawnTime = Time.time;
        }

        private void OnDisable()
        {
            if (initialFillRoutine != null)
            {
                StopCoroutine(initialFillRoutine);
                initialFillRoutine = null;
            }

            UnsubscribeAllSlots();
        }

        private void Update()
        {
            if (spawnPoint == null)
                return;

            if (oneAlivePerPrefabType)
            {
                ProcessPerTypeRespawns();
                return;
            }

            if (CountAlive() >= maxAlive)
                return;

            if (humanoidPrefab != null && Time.time >= nextSpawnTime)
            {
                nextSpawnTime = Time.time + spawnIntervalSeconds;
                SpawnPawn(humanoidPrefab);
            }

            if (Time.time >= nextAdditionalSpawnTime && TryGetNextAdditionalPrefab(out GameObject additionalPrefab))
            {
                nextAdditionalSpawnTime = Time.time + additionalSpawnIntervalSeconds;
                SpawnPawn(additionalPrefab);
            }
        }

        private void RebuildRosterSlots()
        {
            UnsubscribeAllSlots();
            slots.Clear();

            var seen = new HashSet<GameObject>();
            TryAddRosterPrefab(humanoidPrefab, seen);
            if (additionalPawnablePrefabs != null)
            {
                for (int i = 0; i < additionalPawnablePrefabs.Length; i++)
                    TryAddRosterPrefab(additionalPawnablePrefabs[i], seen);
            }
        }

        private void TryAddRosterPrefab(GameObject prefab, HashSet<GameObject> seen)
        {
            if (prefab == null || !seen.Add(prefab))
                return;

            var slot = new SandboxSlot { Prefab = prefab };
            slot.DiedHandler = () => HandleSlotDied(slot);
            slots.Add(slot);
        }

        private IEnumerator InitialFillRoutine()
        {
            yield return null;

            float interval = ResolveSequentialSpawnInterval();
            for (int i = 0; i < slots.Count; i++)
            {
                if (!IsSlotAlive(slots[i]))
                    SpawnForSlot(slots[i]);

                if (i < slots.Count - 1)
                    yield return new WaitForSeconds(interval);
            }

            initialFillRoutine = null;
        }

        private void ProcessPerTypeRespawns()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                SandboxSlot slot = slots[i];
                if (slot.RespawnAt < 0f || Time.time < slot.RespawnAt)
                    continue;

                if (IsSlotAlive(slot))
                {
                    slot.RespawnAt = -1f;
                    continue;
                }

                slot.RespawnAt = -1f;
                SpawnForSlot(slot);
            }
        }

        private void HandleSlotDied(SandboxSlot slot)
        {
            if (slot.Active != null && slot.DiedHandler != null)
                slot.Active.Died -= slot.DiedHandler;

            slot.RespawnAt = Time.time + ResolveSequentialSpawnInterval();
        }

        private void SpawnForSlot(SandboxSlot slot)
        {
            if (slot == null || slot.Prefab == null)
                return;

            ReleaseDeadSlotInstance(slot);
            if (IsSlotAlive(slot))
                return;

            EnemyHealth health = SpawnPawnInternal(slot.Prefab);
            if (health == null)
                return;

            slot.Active = health;
            health.respawnTime = 0f;
            health.SetRespawnExternallyManaged(true);
            health.Died += slot.DiedHandler;
        }

        private static bool IsSlotAlive(SandboxSlot slot)
        {
            EnemyHealth health = slot.Active;
            if (health == null)
                return false;

            if (!health.gameObject.activeInHierarchy)
            {
                slot.Active = null;
                return false;
            }

            return !health.IsDead;
        }

        private void ReleaseDeadSlotInstance(SandboxSlot slot)
        {
            EnemyHealth health = slot.Active;
            if (health == null)
                return;

            if (slot.DiedHandler != null)
                health.Died -= slot.DiedHandler;

            if (health.IsDead)
                health.DestroyDeadShellNow();

            slot.Active = null;
        }

        private void UnsubscribeAllSlots()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                SandboxSlot slot = slots[i];
                if (slot.Active != null && slot.DiedHandler != null)
                    slot.Active.Died -= slot.DiedHandler;

                slot.Active = null;
                slot.RespawnAt = -1f;
            }
        }

        private float ResolveSequentialSpawnInterval()
        {
            if (spawnIntervalSeconds > 0f)
                return spawnIntervalSeconds;

            return Mathf.Max(0.01f, additionalSpawnIntervalSeconds);
        }

        private bool TryGetNextAdditionalPrefab(out GameObject prefab)
        {
            prefab = null;
            if (additionalPawnablePrefabs == null || additionalPawnablePrefabs.Length == 0)
                return false;

            int length = additionalPawnablePrefabs.Length;
            for (int attempt = 0; attempt < length; attempt++)
            {
                int index = (additionalPrefabCycleIndex + attempt) % length;
                GameObject candidate = additionalPawnablePrefabs[index];
                if (candidate == null)
                    continue;

                additionalPrefabCycleIndex = (index + 1) % length;
                prefab = candidate;
                return true;
            }

            return false;
        }

        private void SpawnPawn(GameObject prefab)
        {
            if (CountAlive() >= maxAlive)
                return;

            EnemyHealth health = SpawnPawnInternal(prefab);
            if (health != null)
                spawnedEnemies.Add(health);
        }

        private EnemyHealth SpawnPawnInternal(GameObject prefab)
        {
            if (prefab == null || spawnPoint == null)
                return null;

            Vector3 spawnPosition = spawnPoint.position;
            float standLift = EnemyGroundUtility.ResolveCapsuleStandLift(prefab);
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

            GameObject instance = Instantiate(prefab, spawnPosition, spawnPoint.rotation);
            if (instance.GetComponent<CombatPoise>() == null)
                instance.AddComponent<CombatPoise>();

            EnemyGroundUtility.SnapCreatureToGround(instance.transform, instance.transform.position, minAcceptedY);
            HumanoidPerformanceController.ForceSpawnVisible(instance);
            DMSpawnPhysicsStabilizer.EnsureOn(instance);

            return instance.GetComponent<EnemyHealth>();
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

        private sealed class SandboxSlot
        {
            public GameObject Prefab;
            public EnemyHealth Active;
            public float RespawnAt = -1f;
            public Action DiedHandler;
        }
    }
}

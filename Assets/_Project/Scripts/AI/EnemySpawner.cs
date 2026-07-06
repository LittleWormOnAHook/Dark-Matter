using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Project.AI
{
    [Serializable]
    public class EnemySpawnEntry
    {
        [Tooltip("Spawn-ready combat enemy prefab.")]
        public GameObject prefab;

        [Min(1)]
        public int count = 1;
    }

    /// <summary>
    /// Spawns configured enemies at runtime. Assign one or more combat enemy prefabs with per-type counts.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Enemies")]
        [Tooltip("Add one row per enemy type. Leave empty to use the legacy single-prefab field below.")]
        [SerializeField] private EnemySpawnEntry[] enemyEntries = Array.Empty<EnemySpawnEntry>();

        [FormerlySerializedAs("enemyPrefab")]
        [SerializeField] private GameObject legacyEnemyPrefab;

        [Header("Spawn")]
        [SerializeField] private bool spawnOnStart = true;
        [FormerlySerializedAs("spawnCount")]
        [Min(1)]
        [SerializeField] private int legacySpawnCount = 1;
        [SerializeField] private float spawnRadius = 2f;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private bool useChildSpawnPoints = true;
        [SerializeField] private Transform[] manualSpawnPoints = Array.Empty<Transform>();

        [Header("Respawn")]
        [SerializeField] private bool respawnOnDeath = true;
        [Tooltip("Legacy delay when the enemy prefab has no EnemyDeathSequence. Spawner-managed enemies use the sequence post-loot delay instead.")]
        [Min(0f)]
        [SerializeField] private float respawnDelay = 10f;

        [Header("Optional Overrides")]
        [FormerlySerializedAs("enemyDefinition")]
        [SerializeField] private EnemyDefinition definitionOverride;
        [SerializeField] private EnemySpawnSettings spawnSettings = new EnemySpawnSettings();

        public GameObject EnemyPrefab => legacyEnemyPrefab;
        public IReadOnlyList<EnemySpawnEntry> Entries => enemyEntries;
        public EnemySpawnSettings Settings => spawnSettings;
        public bool RespawnOnDeath => respawnOnDeath;
        public float RespawnDelay => respawnDelay;

        private readonly List<SpawnSlot> _slots = new List<SpawnSlot>();

        private sealed class SpawnSlot
        {
            public GameObject Prefab;
            public Vector3 Position;
            public Quaternion Rotation;
            public GameObject Instance;
            public Coroutine RespawnRoutine;
        }

        private void Start()
        {
            if (spawnOnStart)
                SpawnAll();
        }

        [ContextMenu("Spawn Enemies")]
        public void SpawnAll()
        {
            ClearSlots();
            List<EnemySpawnEntry> entries = GetResolvedEntries();
            if (entries.Count == 0)
            {
                Debug.LogWarning($"{nameof(EnemySpawner)} on {name} has no enemy prefabs assigned.", this);
                return;
            }

            List<GameObject> spawnQueue = BuildSpawnQueue(entries);
            List<Transform> points = CollectSpawnPoints();

            if (points.Count == 0)
            {
                Transform origin = spawnPoint != null ? spawnPoint : transform;
                for (int i = 0; i < spawnQueue.Count; i++)
                {
                    Vector3 position = ResolveSpawnPosition(origin, spawnQueue.Count, i);
                    Quaternion rotation = ResolveSpawnRotation(origin);
                    SpawnTrackedInstance(spawnQueue[i], position, rotation);
                }

                return;
            }

            for (int i = 0; i < spawnQueue.Count; i++)
            {
                Transform point = points[i % points.Count];
                Vector3 position = ResolveSpawnPosition(point, 1, 0);
                Quaternion rotation = ResolveSpawnRotation(point);
                SpawnTrackedInstance(spawnQueue[i], position, rotation);
            }
        }

        public GameObject SpawnAtTransform(Transform point, GameObject prefab, int count = 1)
        {
            if (prefab == null || point == null)
                return null;

            GameObject lastInstance = null;
            int spawnTotal = Mathf.Max(1, count);
            for (int i = 0; i < spawnTotal; i++)
            {
                Vector3 position = ResolveSpawnPosition(point, spawnTotal, i);
                Quaternion rotation = ResolveSpawnRotation(point);
                lastInstance = SpawnTrackedInstance(prefab, position, rotation);
            }

            return lastInstance;
        }

        public GameObject SpawnAtTransform(Transform point, int count = 1)
        {
            List<EnemySpawnEntry> entries = GetResolvedEntries();
            if (entries.Count == 0 || point == null)
                return null;

            return SpawnAtTransform(point, entries[0].prefab, count);
        }

        private GameObject SpawnTrackedInstance(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            position = EnemyGroundUtility.SnapPositionToGround(position);

            SpawnSlot slot = new SpawnSlot
            {
                Prefab = prefab,
                Position = position,
                Rotation = rotation,
            };

            slot.Instance = SpawnInstance(prefab, position, rotation);
            _slots.Add(slot);
            TrackInstance(slot);
            return slot.Instance;
        }

        private GameObject SpawnInstance(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            GameObject instance = Instantiate(prefab, position, rotation);
            instance.name = prefab.name;
            EnemyDefinition definition = ResolveDefinition(prefab);
            EnemySpawnConfigurator.Apply(instance, spawnSettings, definition);
            ConfigureSpawnerRespawn(instance);
            return instance;
        }

        private void ConfigureSpawnerRespawn(GameObject instance)
        {
            if (!respawnOnDeath || respawnDelay < 0f)
                return;

            EnemyHealth health = instance.GetComponent<EnemyHealth>();
            if (health == null)
                return;

            health.respawnTime = 0f;
            health.SetRespawnExternallyManaged(true);
            SetField(health, "destroyOnDeath", false);
        }

        private static void SetField(UnityEngine.Object target, string fieldName, object value)
        {
            if (target == null)
                return;

            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
            field?.SetValue(target, value);
        }

        private void TrackInstance(SpawnSlot slot)
        {
            if (!respawnOnDeath || slot.Instance == null)
                return;

            EnemyHealth health = slot.Instance.GetComponent<EnemyHealth>();
            if (health == null)
                return;

            health.Died += () => HandleSlotDied(slot);
        }

        private void HandleSlotDied(SpawnSlot slot)
        {
            if (!respawnOnDeath || slot.RespawnRoutine != null)
                return;

            slot.RespawnRoutine = StartCoroutine(RespawnSlotAfterDelay(slot));
        }

        private IEnumerator RespawnSlotAfterDelay(SpawnSlot slot)
        {
            GameObject deadInstance = slot.Instance;

            if (deadInstance != null)
            {
                EnemyDeathSequence deathSequence = deadInstance.GetComponent<EnemyDeathSequence>();
                if (deathSequence != null)
                {
                    while (deadInstance != null && !deathSequence.IsComplete)
                        yield return null;
                }
                else
                {
                    EnemyLootable lootable = deadInstance.GetComponent<EnemyLootable>();
                    while (deadInstance != null && lootable != null && lootable.IsLootPending)
                        yield return null;

                    if (respawnDelay > 0f)
                        yield return new WaitForSeconds(respawnDelay);
                }
            }

            if (deadInstance != null)
                Destroy(deadInstance);

            slot.Instance = null;
            slot.RespawnRoutine = null;

            if (!isActiveAndEnabled || slot.Prefab == null)
                yield break;

            slot.Instance = SpawnInstance(slot.Prefab, slot.Position, slot.Rotation);
            TrackInstance(slot);
        }

        private void ClearSlots()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                SpawnSlot slot = _slots[i];
                if (slot.RespawnRoutine != null)
                    StopCoroutine(slot.RespawnRoutine);
            }

            _slots.Clear();
        }

        private void OnDisable()
        {
            ClearSlots();
        }

        private EnemyDefinition ResolveDefinition(GameObject prefab)
        {
            if (definitionOverride != null)
                return definitionOverride;

            if (spawnSettings.definition != null)
                return spawnSettings.definition;

            return EnemyPrefabResolver.GetDefinition(prefab);
        }

        private List<EnemySpawnEntry> GetResolvedEntries()
        {
            List<EnemySpawnEntry> resolved = new List<EnemySpawnEntry>();

            if (enemyEntries != null)
            {
                for (int i = 0; i < enemyEntries.Length; i++)
                {
                    EnemySpawnEntry entry = enemyEntries[i];
                    if (entry == null || entry.prefab == null)
                        continue;

                    resolved.Add(entry);
                }
            }

            if (resolved.Count == 0 && legacyEnemyPrefab != null)
            {
                resolved.Add(new EnemySpawnEntry
                {
                    prefab = legacyEnemyPrefab,
                    count = Mathf.Max(1, legacySpawnCount),
                });
            }

            return resolved;
        }

        private static List<GameObject> BuildSpawnQueue(List<EnemySpawnEntry> entries)
        {
            List<GameObject> queue = new List<GameObject>();
            for (int i = 0; i < entries.Count; i++)
            {
                EnemySpawnEntry entry = entries[i];
                int count = Mathf.Max(1, entry.count);
                for (int j = 0; j < count; j++)
                    queue.Add(entry.prefab);
            }

            return queue;
        }

        private List<Transform> CollectSpawnPoints()
        {
            List<Transform> points = new List<Transform>();

            if (manualSpawnPoints != null)
            {
                for (int i = 0; i < manualSpawnPoints.Length; i++)
                {
                    if (manualSpawnPoints[i] != null)
                        points.Add(manualSpawnPoints[i]);
                }
            }

            if (useChildSpawnPoints)
            {
                EnemySpawnPoint[] childPoints = GetComponentsInChildren<EnemySpawnPoint>(true);
                for (int i = 0; i < childPoints.Length; i++)
                {
                    if (childPoints[i] != null)
                        points.Add(childPoints[i].transform);
                }
            }

            return points;
        }

        private Vector3 ResolveSpawnPosition(Transform point, int total, int index)
        {
            EnemySpawnPoint marker = point.GetComponent<EnemySpawnPoint>();
            if (marker != null)
                return marker.ResolvePosition();

            if (total <= 1 && spawnRadius <= 0f)
                return point.position;

            Vector2 offset2D = UnityEngine.Random.insideUnitCircle * spawnRadius;
            return point.position + new Vector3(offset2D.x, 0f, offset2D.y);
        }

        private Quaternion ResolveSpawnRotation(Transform point)
        {
            EnemySpawnPoint marker = point.GetComponent<EnemySpawnPoint>();
            if (marker != null)
                return marker.ResolveRotation(transform);

            return point.rotation;
        }

        private void OnDrawGizmosSelected()
        {
            List<Transform> points = CollectSpawnPoints();
            if (points.Count == 0)
            {
                Vector3 origin = spawnPoint != null ? spawnPoint.position : transform.position;
                Gizmos.color = new Color(0.8f, 0.2f, 0.35f, 0.85f);
                Gizmos.DrawWireSphere(origin, Mathf.Max(0.1f, spawnRadius));
                Gizmos.DrawSphere(origin, 0.15f);
                return;
            }

            Gizmos.color = new Color(0.8f, 0.2f, 0.35f, 0.65f);
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] == null)
                    continue;

                Gizmos.DrawLine(transform.position, points[i].position);
            }
        }
    }
}

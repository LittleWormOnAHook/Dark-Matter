using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Expedition / full-map encounter zone that spawns weighted random surface threats
    /// (aliens, lifeforms, androids) and optionally assigns patrol routes.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class SurfaceEncounterZone : MonoBehaviour
    {
        [Header("Activation")]
        [SerializeField] private bool spawnOnStart;
        [SerializeField] private bool spawnOnPlayerEnter = true;
        [Min(0f)]
        [SerializeField] private float reactivationCooldown = 120f;

        [Header("Encounter Table")]
        [SerializeField] private SurfaceEncounterTable encounterTable;

        [Header("Spawn Count")]
        [Min(0)]
        [SerializeField] private int minSpawnCount = 1;
        [Min(0)]
        [SerializeField] private int maxSpawnCount = 3;

        [Header("Anchors")]
        [SerializeField] private bool useChildAnchors = true;
        [SerializeField] private SurfaceEncounterSpawnAnchor[] manualAnchors = Array.Empty<SurfaceEncounterSpawnAnchor>();

        [Header("Spawn Overrides")]
        [SerializeField] private EnemySpawnSettings spawnSettings = new EnemySpawnSettings();
        [SerializeField] private bool respawnOnDeath = true;
        [Min(0f)]
        [SerializeField] private float respawnDelay = 45f;

        private readonly HashSet<Transform> occupants = new HashSet<Transform>();
        private readonly List<SpawnSlot> slots = new List<SpawnSlot>();
        private CombatZoneController combatZone;
        private float nextActivationTime;
        private bool hasSpawned;

        private sealed class SpawnSlot
        {
            public SurfaceEncounterSpawnAnchor Anchor;
            public SurfaceEncounterSpawnEntry Entry;
            public GameObject Instance;
            public Coroutine RespawnRoutine;
        }

        private void Awake()
        {
            Collider zoneCollider = GetComponent<Collider>();
            zoneCollider.isTrigger = true;
            combatZone = GetComponentInParent<CombatZoneController>();
        }

        private void Start()
        {
            if (spawnOnStart)
                TryActivateZone();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!spawnOnPlayerEnter || !IsPlayer(other))
                return;

            occupants.Add(other.transform);
            if (occupants.Count == 1)
                TryActivateZone();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other))
                return;

            occupants.Remove(other.transform);
        }

        [ContextMenu("Spawn Surface Encounters")]
        public void TryActivateZone()
        {
            if (!isActiveAndEnabled)
                return;

            if (Time.time < nextActivationTime)
                return;

            if (encounterTable == null)
            {
                Debug.LogWarning($"{nameof(SurfaceEncounterZone)} on {name} has no encounter table.", this);
                return;
            }

            List<SurfaceEncounterSpawnAnchor> anchors = CollectAnchors();
            if (anchors.Count == 0)
            {
                Debug.LogWarning($"{nameof(SurfaceEncounterZone)} on {name} has no spawn anchors.", this);
                return;
            }

            ClearSlots();

            int spawnCount = ResolveSpawnCount(anchors.Count);
            ShuffleAnchors(anchors);

            for (int i = 0; i < spawnCount; i++)
            {
                SurfaceEncounterSpawnAnchor anchor = anchors[i];
                if (!encounterTable.TryPickRandom(anchor.PreferredThreatKind, out SurfaceEncounterSpawnEntry entry))
                    continue;

                SpawnAtAnchor(anchor, entry);
            }

            hasSpawned = slots.Count > 0;
            if (hasSpawned)
                nextActivationTime = Time.time + reactivationCooldown;
        }

        private int ResolveSpawnCount(int anchorCount)
        {
            int min = Mathf.Max(0, minSpawnCount);
            int max = Mathf.Max(min, maxSpawnCount);
            int desired = UnityEngine.Random.Range(min, max + 1);
            return Mathf.Clamp(desired, 0, anchorCount);
        }

        private void SpawnAtAnchor(SurfaceEncounterSpawnAnchor anchor, SurfaceEncounterSpawnEntry entry)
        {
            SpawnSlot slot = new SpawnSlot
            {
                Anchor = anchor,
                Entry = entry,
            };

            if (!TrySpawnIntoSlot(slot))
                return;

            slots.Add(slot);
            TrackSlot(slot);
        }

        private bool TrySpawnIntoSlot(SpawnSlot slot)
        {
            if (slot.Anchor == null || slot.Entry == null || slot.Entry.prefab == null)
                return false;

            Vector3 position = slot.Anchor.ResolvePosition();
            if (!CanSpawnAt(position))
                return false;

            position = EnemyGroundUtility.SnapPositionToGround(position);
            Quaternion rotation = slot.Anchor.ResolveRotation();

            GameObject instance = Instantiate(slot.Entry.prefab, position, rotation);
            instance.name = slot.Entry.prefab.name;

            EnemyDefinition definition = ResolveDefinition(slot.Entry);
            EnemySpawnConfigurator.Apply(instance, spawnSettings, definition);
            ConfigureRespawn(instance);
            SurfaceEncounterPatrolBinder.Apply(instance, slot.Anchor.PatrolRoute);
            combatZone?.TryRegisterHumanoid(instance);

            slot.Instance = instance;
            return true;
        }

        private EnemyDefinition ResolveDefinition(SurfaceEncounterSpawnEntry entry)
        {
            if (spawnSettings.definition != null)
                return spawnSettings.definition;

            return EnemyPrefabResolver.GetDefinition(entry.prefab);
        }

        private void ConfigureRespawn(GameObject instance)
        {
            if (!respawnOnDeath)
                return;

            EnemyHealth health = instance.GetComponent<EnemyHealth>();
            if (health == null)
                return;

            health.respawnTime = 0f;
            health.SetRespawnExternallyManaged(true);
            SetField(health, "destroyOnDeath", false);
        }

        private void TrackSlot(SpawnSlot slot)
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
                }
            }

            if (respawnDelay > 0f)
                yield return new WaitForSeconds(respawnDelay);

            if (deadInstance != null)
                Destroy(deadInstance);

            slot.Instance = null;
            slot.RespawnRoutine = null;

            if (!isActiveAndEnabled || slot.Anchor == null || slot.Entry == null)
                yield break;

            if (!TrySpawnIntoSlot(slot))
            {
                slot.RespawnRoutine = StartCoroutine(RetryRespawnWhenAllowed(slot));
                yield break;
            }

            TrackSlot(slot);
        }

        private IEnumerator RetryRespawnWhenAllowed(SpawnSlot slot)
        {
            while (isActiveAndEnabled && slot.Anchor != null && slot.Entry != null && !TrySpawnIntoSlot(slot))
                yield return new WaitForSeconds(1f);

            slot.RespawnRoutine = null;
            if (!isActiveAndEnabled || slot.Instance == null)
                yield break;

            TrackSlot(slot);
        }

        private bool CanSpawnAt(Vector3 position)
        {
            if (combatZone == null)
                return true;

            if (!combatZone.CanSpawnMore())
                return false;

            return combatZone.IsSpawnAllowedByDistance(position);
        }

        private List<SurfaceEncounterSpawnAnchor> CollectAnchors()
        {
            List<SurfaceEncounterSpawnAnchor> anchors = new List<SurfaceEncounterSpawnAnchor>();

            if (manualAnchors != null)
            {
                for (int i = 0; i < manualAnchors.Length; i++)
                {
                    if (manualAnchors[i] != null)
                        anchors.Add(manualAnchors[i]);
                }
            }

            if (useChildAnchors)
            {
                SurfaceEncounterSpawnAnchor[] childAnchors =
                    GetComponentsInChildren<SurfaceEncounterSpawnAnchor>(true);
                for (int i = 0; i < childAnchors.Length; i++)
                {
                    SurfaceEncounterSpawnAnchor anchor = childAnchors[i];
                    if (anchor != null && !anchors.Contains(anchor))
                        anchors.Add(anchor);
                }
            }

            return anchors;
        }

        private static void ShuffleAnchors(List<SurfaceEncounterSpawnAnchor> anchors)
        {
            for (int i = anchors.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (anchors[i], anchors[swapIndex]) = (anchors[swapIndex], anchors[i]);
            }
        }

        private void ClearSlots()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                SpawnSlot slot = slots[i];
                if (slot.RespawnRoutine != null)
                    StopCoroutine(slot.RespawnRoutine);

                if (slot.Instance != null)
                    Destroy(slot.Instance);
            }

            slots.Clear();
        }

        private void OnDisable()
        {
            ClearSlots();
        }

        private static bool IsPlayer(Collider other)
        {
            return other.CompareTag("Player") || other.GetComponentInParent<ECM2.Character>() != null;
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

        private void OnDrawGizmosSelected()
        {
            Collider col = GetComponent<Collider>();
            if (col == null)
                return;

            Gizmos.color = new Color(0.75f, 0.16f, 0.37f, 0.25f);
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.color = new Color(0.75f, 0.16f, 0.37f, 0.9f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}

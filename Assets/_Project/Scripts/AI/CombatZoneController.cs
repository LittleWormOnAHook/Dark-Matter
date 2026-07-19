using System.Collections.Generic;
using Project.Core;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Caps active humanoid enemies near the player and defers spawns when the zone budget is full.
    /// </summary>
    [DisallowMultipleComponent]
    public class CombatZoneController : MonoBehaviour
    {
        [Header("Budget")]
        [SerializeField] private int maxActiveHumanoids = 10;
        [SerializeField] private bool usePlatformDefaults = true;

        [Header("Distance")]
        [SerializeField] private float activationRadius = 50f;
        [SerializeField] private bool requirePlayerInRangeToSpawn = true;

        [Header("Spawners")]
        [SerializeField] private bool autoCollectChildSpawners = true;
        [SerializeField] private EnemySpawner[] spawners = System.Array.Empty<EnemySpawner>();

        private readonly HashSet<EnemyHealth> _activeHumanoids = new HashSet<EnemyHealth>();
        private readonly List<EnemyHealth> _pruneBuffer = new List<EnemyHealth>(16);
        private Transform _playerTransform;

        public int MaxActiveHumanoids => ResolveMaxActiveHumanoids();
        public int ActiveHumanoidCount => _activeHumanoids.Count;
        public float ActivationRadius => ResolveActivationRadius();

        private void Awake()
        {
            ApplyPlatformDefaultsIfNeeded();
            CollectSpawners();
        }

        private void Start()
        {
            CachePlayerTransform();
            WireSpawners();
        }

        private void Update()
        {
            if (((Time.frameCount + GetEntityId().GetHashCode()) & 15) != 0)
                return;

            PruneInactiveEntries();
        }

        public bool CanSpawnMore()
        {
            PruneInactiveEntries();
            return _activeHumanoids.Count < ResolveMaxActiveHumanoids();
        }

        public bool IsSpawnAllowedByDistance(Vector3 spawnPosition)
        {
            if (!requirePlayerInRangeToSpawn)
                return true;

            if (!TryGetPlayerPosition(out Vector3 playerPosition))
                return true;

            Vector3 delta = spawnPosition - playerPosition;
            delta.y = 0f;
            return delta.sqrMagnitude <= ResolveActivationRadius() * ResolveActivationRadius();
        }

        public bool TryRegisterHumanoid(GameObject instance)
        {
            if (instance == null)
                return false;

            PruneInactiveEntries();
            if (_activeHumanoids.Count >= ResolveMaxActiveHumanoids())
                return false;

            EnemyHealth health = instance.GetComponent<EnemyHealth>();
            if (health == null)
                return true;

            _activeHumanoids.Add(health);
            health.Died += () => UnregisterHumanoid(health);
            return true;
        }

        public void UnregisterHumanoid(EnemyHealth health)
        {
            if (health == null)
                return;

            _activeHumanoids.Remove(health);
        }

        private void WireSpawners()
        {
            for (int i = 0; i < spawners.Length; i++)
            {
                EnemySpawner spawner = spawners[i];
                if (spawner != null)
                    spawner.BindCombatZone(this);
            }
        }

        private void CollectSpawners()
        {
            if (!autoCollectChildSpawners)
                return;

            spawners = GetComponentsInChildren<EnemySpawner>(true);
        }

        private void ApplyPlatformDefaultsIfNeeded()
        {
            if (!usePlatformDefaults)
                return;

            maxActiveHumanoids = PlatformGraphicsProfile.DefaultMaxZoneHumanoids;
            activationRadius = PlatformGraphicsProfile.DefaultZoneActivationRadius;
        }

        private int ResolveMaxActiveHumanoids()
        {
            return Mathf.Max(1, maxActiveHumanoids);
        }

        private float ResolveActivationRadius()
        {
            return Mathf.Max(5f, activationRadius);
        }

        private void PruneInactiveEntries()
        {
            if (_activeHumanoids.Count == 0)
                return;

            _pruneBuffer.Clear();
            foreach (EnemyHealth health in _activeHumanoids)
            {
                if (health == null || !health.isActiveAndEnabled || health.IsDead)
                    _pruneBuffer.Add(health);
            }

            for (int i = 0; i < _pruneBuffer.Count; i++)
                _activeHumanoids.Remove(_pruneBuffer[i]);
        }

        private void CachePlayerTransform()
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                _playerTransform = playerObject.transform;
        }

        private bool TryGetPlayerPosition(out Vector3 playerPosition)
        {
            if (_playerTransform == null)
                CachePlayerTransform();

            if (_playerTransform == null)
            {
                playerPosition = default;
                return false;
            }

            playerPosition = _playerTransform.position;
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.2f, 0.35f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, ResolveActivationRadius());
        }
    }
}

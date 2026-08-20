using System;
using System.Collections.Generic;
using Project.Combat;
using Project.Core;
using Project.Data;
using Project.Quests;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Generates enemy loot on death and spawns a world loot bag after disintegration completes.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyLootable : MonoBehaviour
    {
        [Header("Loot")]
        [SerializeField] private bool enableLoot = true;
        [SerializeField] private string lootDisplayName = "Enemy";
        [Header("Loot AC")]
        [Tooltip("Aether Credits (AC) dropped by this enemy.")]
        [UnityEngine.Serialization.FormerlySerializedAs("piCoinsMin")]
        [SerializeField] private int acDropMin = 1;
        [UnityEngine.Serialization.FormerlySerializedAs("piCoinsMax")]
        [SerializeField] private int acDropMax = 5;
        [SerializeField] private int randomLootCountMin = 0;
        [SerializeField] private int randomLootCountMax = 2;
        [SerializeField] private ItemData[] lootItemPool = Array.Empty<ItemData>();
        [SerializeField] private float lootUnlootedLifetime = 20f;
        [SerializeField] private float lootedBagDissolveDelay = 2f;
        [SerializeField] private float lootInteractRange = 2.75f;
        [SerializeField] private string promptText = "Press E to loot bag";

        [Header("Loot Bag Prefab")]
        public const string DefaultLootBagPrefabPath = "Assets/_Project/Prefabs/Combat/EnemyLootBag.prefab";

        [Tooltip("Instanced bag dropped after disintegration.")]
        [SerializeField] private GameObject lootBagPrefab;
        [Tooltip("Optional mesh override for this enemy's bag. Applied in Edit Mode onto spawned instances.")]
        [SerializeField] private Mesh dropMesh;
        [Tooltip("Optional albedo texture override for this enemy's bag.")]
        [SerializeField] private Texture dropTexture;

        private readonly List<QuestRewardDefinition> remainingLoot = new List<QuestRewardDefinition>();

        private EnemyHealth health;
        private bool lootPending;
        private bool bagSpawned;
        private Vector3 pendingBagPosition;

        public bool HasRemainingLoot => remainingLoot.Count > 0;
        public bool IsLootPending => lootPending;
        public string DisplayName => lootDisplayName;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            ResolveLootBagPrefabReference();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (lootBagPrefab == null)
            {
                lootBagPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultLootBagPrefabPath);
            }
        }
#endif

        private void ResolveLootBagPrefabReference()
        {
            if (lootBagPrefab != null)
                return;

            lootBagPrefab = Resources.Load<GameObject>("Combat/EnemyLootBag");
        }

        private void OnEnable()
        {
            if (health != null)
                health.Died += HandleEnemyDied;
        }

        private void OnDisable()
        {
            if (health != null)
                health.Died -= HandleEnemyDied;

            lootPending = false;
            bagSpawned = false;
        }

        public void ConfigureFromDefinition(EnemyDefinition definition)
        {
            if (definition == null)
                return;

            enableLoot = definition.enableLoot;
            lootDisplayName = string.IsNullOrWhiteSpace(definition.displayName)
                ? definition.enemyId
                : definition.displayName;
            acDropMin = definition.acDropMin;
            acDropMax = definition.acDropMax;
            randomLootCountMin = definition.randomLootCountMin;
            randomLootCountMax = definition.randomLootCountMax;
            lootItemPool = definition.lootItemPool ?? Array.Empty<ItemData>();
            lootUnlootedLifetime = definition.lootRespawnDelay;
            lootInteractRange = definition.lootInteractRange;
            lootBagPrefab = definition.lootBagPrefab;
            if (lootBagPrefab == null)
                ResolveLootBagPrefabReference();
#if UNITY_EDITOR
            if (lootBagPrefab == null)
                lootBagPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultLootBagPrefabPath);
#endif
            if (definition.lootBagMesh != null)
                dropMesh = definition.lootBagMesh;
            if (definition.lootBagTexture != null)
                dropTexture = definition.lootBagTexture;
        }

        public void TrySpawnLootBag(Vector3 worldPosition)
        {
            if (!lootPending || bagSpawned || !HasRemainingLoot)
                return;

            bagSpawned = true;
            pendingBagPosition = worldPosition;

            EnemyLootBag bag = EnemyLootBag.Spawn(
                pendingBagPosition,
                this,
                remainingLoot,
                lootDisplayName,
                lootInteractRange,
                promptText,
                lootUnlootedLifetime,
                lootedBagDissolveDelay,
                lootBagPrefab,
                dropMesh,
                dropTexture);

            if (bag == null)
            {
                bagSpawned = false;
                FinishLootPhase();
            }
        }

        public void NotifyLootBagDissolved()
        {
            FinishLootPhase();
        }

        private void HandleEnemyDied()
        {
            if (!enableLoot)
                return;

            GenerateLoot();
            if (!HasRemainingLoot)
                return;

            lootPending = true;
            bagSpawned = false;
            pendingBagPosition = transform.position;

            if (health != null && GetComponent<EnemyDeathSequence>() == null)
                health.SetRespawnExternallyManaged(true);

            EnemyDisintegrationEffect disintegration = GetComponent<EnemyDisintegrationEffect>();
            if (disintegration == null)
                TrySpawnLootBag(pendingBagPosition);
        }

        private void GenerateLoot()
        {
            remainingLoot.Clear();

            int piAmount = RollAcAmount();
            if (piAmount > 0)
            {
                remainingLoot.Add(new QuestRewardDefinition
                {
                    type = QuestRewardType.Pi,
                    amount = piAmount
                });
            }

            int itemRollCount = RollItemCount();
            if (itemRollCount <= 0)
                return;

            List<ItemData> pool = BuildLootPool();
            if (pool.Count == 0)
                return;

            for (int i = 0; i < itemRollCount && pool.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                ItemData item = pool[index];
                pool.RemoveAt(index);

                if (item == null)
                    continue;

                remainingLoot.Add(new QuestRewardDefinition
                {
                    type = QuestRewardType.Item,
                    item = item,
                    amount = 1
                });
            }
        }

        private int RollAcAmount()
        {
            int min = Mathf.Max(0, acDropMin);
            int max = Mathf.Max(min, acDropMax);
            return UnityEngine.Random.Range(min, max + 1);
        }

        private int RollItemCount()
        {
            int min = Mathf.Max(0, randomLootCountMin);
            int max = Mathf.Max(min, randomLootCountMax);
            return UnityEngine.Random.Range(min, max + 1);
        }

        private List<ItemData> BuildLootPool()
        {
            List<ItemData> pool = new List<ItemData>();
            if (lootItemPool != null && lootItemPool.Length > 0)
            {
                for (int i = 0; i < lootItemPool.Length; i++)
                {
                    ItemData item = lootItemPool[i];
                    if (item != null && item.worldPrefab != null)
                        pool.Add(item);
                }

                return pool;
            }

            ItemData[] registryItems = ItemRegistry.GetAllItems();
            for (int i = 0; i < registryItems.Length; i++)
            {
                ItemData item = registryItems[i];
                if (item == null || item.worldPrefab == null || item.itemType == ItemType.Quest
                    || item.itemType == ItemType.MeleeWeapon)
                    continue;

                pool.Add(item);
            }

            return pool;
        }

        private void FinishLootPhase()
        {
            if (!lootPending && !bagSpawned && remainingLoot.Count == 0)
                return;

            lootPending = false;
            bagSpawned = false;
            remainingLoot.Clear();

            if (health != null && health.IsDead && !health.IsRespawnExternallyManaged)
                health.FinishLootHoldAndRespawn();
        }
    }
}

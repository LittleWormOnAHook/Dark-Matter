using System;
using System.Collections.Generic;
using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Quests;
using Project.UI;
using UnityEngine;

namespace Project.AI
{
    [DisallowMultipleComponent]
    public class EnemyLootable : MonoBehaviour, IWorldUsable
    {
        [Header("Loot")]
        [SerializeField] private bool enableLoot = true;
        [SerializeField] private string lootDisplayName = "Enemy";
        [SerializeField] private int piCoinsMin = 1;
        [SerializeField] private int piCoinsMax = 5;
        [SerializeField] private int randomLootCountMin = 0;
        [SerializeField] private int randomLootCountMax = 2;
        [SerializeField] private ItemData[] lootItemPool = Array.Empty<ItemData>();
        [SerializeField] private float lootRespawnDelay = 20f;
        [SerializeField] private float lootInteractRange = 2.75f;
        [SerializeField] private string promptText = "Press E to loot";

        private readonly List<QuestRewardDefinition> remainingLoot = new List<QuestRewardDefinition>();

        private EnemyHealth health;
        private UIManager uiManager;
        private bool lootActive;
        private bool playerInRange;
        private float respawnDeadline;

        public bool HasRemainingLoot => remainingLoot.Count > 0;
        public bool IsLootActive => lootActive;

        public bool CanPlayerLoot(Vector3 playerPosition)
        {
            return lootActive && HasRemainingLoot && IsWithinLootRange(playerPosition);
        }

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
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

            WorldUseController.Unregister(this);
            ResolveUiManager()?.HideInteractionPrompt();
            lootActive = false;
            playerInRange = false;
        }

        private void Update()
        {
            if (!lootActive)
                return;

            RefreshProximityPrompt();

            if (Time.time >= respawnDeadline)
                FinishLootPhase();
        }

        public void ConfigureFromDefinition(EnemyDefinition definition)
        {
            if (definition == null)
                return;

            enableLoot = definition.enableLoot;
            lootDisplayName = string.IsNullOrWhiteSpace(definition.displayName)
                ? definition.enemyId
                : definition.displayName;
            piCoinsMin = definition.piCoinsMin;
            piCoinsMax = definition.piCoinsMax;
            randomLootCountMin = definition.randomLootCountMin;
            randomLootCountMax = definition.randomLootCountMax;
            lootItemPool = definition.lootItemPool ?? Array.Empty<ItemData>();
            lootRespawnDelay = definition.lootRespawnDelay;
            lootInteractRange = definition.lootInteractRange;
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (!lootActive || !HasRemainingLoot || !IsWithinLootRange(context.PlayerPosition))
                return -1f;

            float distance = Vector3.Distance(context.PlayerPosition, transform.position);
            return 92f - distance;
        }

        public bool TryUse(WorldUseContext context)
        {
            if (!lootActive || !HasRemainingLoot || !IsWithinLootRange(context.PlayerPosition))
                return false;

            OpenLootDialog();
            return true;
        }

        public bool TryLootNextEntry()
        {
            if (!HasRemainingLoot)
                return false;

            QuestRewardDefinition entry = remainingLoot[0];
            remainingLoot.RemoveAt(0);
            GrantLootEntry(entry);
            RefreshLootState();
            return true;
        }

        public bool TryLootAll()
        {
            if (!HasRemainingLoot)
                return false;

            for (int i = remainingLoot.Count - 1; i >= 0; i--)
                GrantLootEntry(remainingLoot[i]);

            remainingLoot.Clear();
            RefreshLootState();
            return true;
        }

        public string BuildLootSummary()
        {
            if (!HasRemainingLoot)
                return "Nothing left to loot.";

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.AppendLine("Remaining loot:");
            for (int i = 0; i < remainingLoot.Count; i++)
            {
                QuestRewardDefinition entry = remainingLoot[i];
                if (entry == null)
                    continue;

                if (entry.type == QuestRewardType.Pi)
                    builder.AppendLine($"- {entry.amount} Pi");
                else if (entry.type == QuestRewardType.Item && entry.item != null)
                    builder.AppendLine($"- {entry.amount}x {entry.item.itemName}");
            }

            return builder.ToString().TrimEnd();
        }

        private void HandleEnemyDied()
        {
            if (!enableLoot)
                return;

            GenerateLoot();
            if (!HasRemainingLoot)
                return;

            lootActive = true;
            respawnDeadline = Time.time + Mathf.Max(1f, lootRespawnDelay);

            if (health != null)
                health.SetRespawnExternallyManaged(true);

            WorldUseController.Register(this);
        }

        private void GenerateLoot()
        {
            remainingLoot.Clear();

            int piAmount = RollPiAmount();
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

        private int RollPiAmount()
        {
            int min = Mathf.Max(0, piCoinsMin);
            int max = Mathf.Max(min, piCoinsMax);
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

        private void GrantLootEntry(QuestRewardDefinition entry)
        {
            if (entry == null)
                return;

            QuestRewardGranter.GrantReward(entry, lootDisplayName);

            if (entry.type == QuestRewardType.Item && entry.item != null)
                PickupToastUI.Show($"+{entry.amount} {entry.item.itemName}");
        }

        private void OpenLootDialog()
        {
            if (EnemyLootDialogUI.IsDialogOpen)
                return;

            EnemyLootDialogUI.Show(this, lootDisplayName, BuildLootSummary());
        }

        private void RefreshLootState()
        {
            if (HasRemainingLoot)
            {
                if (playerInRange)
                    ShowPrompt();
                return;
            }

            FinishLootPhase();
        }

        private void FinishLootPhase()
        {
            if (!lootActive)
                return;

            lootActive = false;
            remainingLoot.Clear();
            playerInRange = false;

            WorldUseController.Unregister(this);
            ResolveUiManager()?.HideInteractionPrompt();

            if (health != null && health.respawnTime > 0f)
            {
                health.SetRespawnExternallyManaged(false);
                health.ForceRespawn();
            }
        }

        private void RefreshProximityPrompt()
        {
            if (!GameSession.HasStarted || !HasRemainingLoot)
                return;

            if (!PlayerInteractionUtility.TryGetPlayerPosition(out Vector3 playerPosition))
                return;

            bool nearby = IsWithinLootRange(playerPosition);
            if (nearby == playerInRange)
                return;

            playerInRange = nearby;
            if (playerInRange)
                ShowPrompt();
            else
                ResolveUiManager()?.HideInteractionPrompt();
        }

        private bool IsWithinLootRange(Vector3 playerPosition)
        {
            return Vector3.Distance(playerPosition, transform.position) <= lootInteractRange;
        }

        private void ShowPrompt()
        {
            UIManager manager = ResolveUiManager();
            if (manager == null)
                return;

            string label = string.IsNullOrWhiteSpace(lootDisplayName) ? "Enemy" : lootDisplayName;
            manager.ShowInteractionPrompt($"{promptText} — {label}");
        }

        private UIManager ResolveUiManager()
        {
            if (uiManager == null)
                uiManager = FindAnyObjectByType<UIManager>();
            return uiManager;
        }
    }
}

using System;
using System.Collections.Generic;
using Project.Core;
using Project.Crafting;
using Project.Data;
using Project.Inventory;
using Project.Progression;
using Project.UI;
using UnityEngine;

namespace Project.Quests
{
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [Header("Bootstrap")]
        [Tooltip("Quest ids made Available when the session starts (for testing / story bootstrap).")]
        [SerializeField] private string[] availableOnStart;

        private readonly Dictionary<string, QuestProgress> progressById = new Dictionary<string, QuestProgress>();
        private InventorySystem inventorySystem;

        public event Action<QuestProgress> OnQuestUpdated;
        public event Action<QuestProgress> OnQuestCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            RegisterAllQuestDefinitions();
            BootstrapAvailableQuests();
            BindInventorySystem();
        }

        private void OnDestroy()
        {
            UnbindInventorySystem();

            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            BindInventorySystem();
            RefreshCollectItemObjectives();
        }

        public void RefreshFromInventory()
        {
            BindInventorySystem();
            RefreshCollectItemObjectives();
        }

        public void NotifyItemCollected(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0)
                return;

            BindInventorySystem();
            UpdateActiveCollectObjectivesForItem(item);
        }

        public static QuestManager EnsureExists()
        {
            if (Instance != null)
                return Instance;

            QuestManager found = FindAnyObjectByType<QuestManager>();
            if (found != null)
                return found;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
                return player.AddComponent<QuestManager>();

            UIManager uiManager = FindAnyObjectByType<UIManager>();
            if (uiManager != null)
                return uiManager.gameObject.AddComponent<QuestManager>();

            return null;
        }

        public IReadOnlyList<QuestProgress> GetAllProgress()
        {
            return new List<QuestProgress>(progressById.Values);
        }

        public QuestProgress GetProgress(string questId)
        {
            return progressById.TryGetValue(questId, out QuestProgress progress) ? progress : null;
        }

        public QuestDefinition GetDefinition(string questId)
        {
            return QuestRegistry.Resolve(questId);
        }

        public bool StartQuest(string questId)
        {
            QuestDefinition definition = QuestRegistry.Resolve(questId);
            if (definition == null)
            {
                Debug.LogWarning($"QuestManager: Unknown quest '{questId}'.");
                return false;
            }

            QuestProgress progress = GetOrCreateProgress(definition);
            if (progress.status != QuestStatus.Available)
                return false;

            progress.status = QuestStatus.Active;
            NotifyUpdated(progress);
            RefreshCollectItemObjectivesForQuest(definition, progress);
            return true;
        }

        public bool UpdateObjectiveProgress(string questId, int objectiveIndex, int amount, bool setAbsolute = false)
        {
            QuestDefinition definition = QuestRegistry.Resolve(questId);
            QuestProgress progress = GetProgress(questId);
            if (definition == null || progress == null || progress.status != QuestStatus.Active)
                return false;

            if (objectiveIndex < 0 || objectiveIndex >= definition.objectives.Count)
                return false;

            int current = progress.GetObjectiveProgress(objectiveIndex);
            int newValue = setAbsolute ? amount : current + amount;
            progress.SetObjectiveProgress(objectiveIndex, newValue);

            if (AreAllObjectivesComplete(definition, progress))
                CompleteQuest(questId);
            else
                NotifyUpdated(progress);

            return true;
        }

        public bool CompleteQuest(string questId)
        {
            QuestProgress progress = GetProgress(questId);
            if (progress == null || progress.status != QuestStatus.Active)
                return false;

            progress.status = QuestStatus.Completed;
            OnQuestCompleted?.Invoke(progress);
            NotifyUpdated(progress);
            return true;
        }

        public bool ClaimRewards(string questId)
        {
            QuestDefinition definition = QuestRegistry.Resolve(questId);
            QuestProgress progress = GetProgress(questId);
            if (definition == null || progress == null || progress.status != QuestStatus.Completed)
                return false;

            if (!TryConsumeCollectItemObjectives(definition))
                return false;

            QuestRewardGranter.GrantRewards(definition);

            if (definition.xpReward > 0)
                ProgressionRewardGranter.GrantXp(definition.xpReward, XpSource.Quest, $"quest-turnin:{questId}", "Quest");
            else
                ProgressionRewardGranter.GrantXp(ProgressionXpDefaults.QuestCompleteXp, XpSource.Quest, $"quest-turnin-default:{questId}", "Quest");

            progress.status = QuestStatus.TurnedIn;
            NotifyUpdated(progress);
            return true;
        }

        public void ApplySaveProgress(IReadOnlyList<QuestProgress> savedProgress)
        {
            if (savedProgress == null)
                return;

            foreach (QuestProgress saved in savedProgress)
            {
                if (saved == null || string.IsNullOrEmpty(saved.questId))
                    continue;

                QuestDefinition definition = QuestRegistry.Resolve(saved.questId);
                if (definition == null)
                    continue;

                QuestProgress progress = GetOrCreateProgress(definition);
                progress.status = saved.status;
                int objectiveCount = Mathf.Max(1, definition.objectives.Count);
                progress.objectiveProgress = new int[objectiveCount];

                if (saved.objectiveProgress != null)
                {
                    int copyCount = Mathf.Min(saved.objectiveProgress.Length, objectiveCount);
                    for (int i = 0; i < copyCount; i++)
                        progress.objectiveProgress[i] = saved.objectiveProgress[i];
                }

                NotifyUpdated(progress);
            }

            RefreshCollectItemObjectives();
        }

        public List<QuestProgress> BuildSaveProgress()
        {
            return new List<QuestProgress>(progressById.Values);
        }

        private void RegisterAllQuestDefinitions()
        {
            foreach (QuestDefinition quest in QuestRegistry.GetAllQuests())
            {
                if (quest == null)
                    continue;

                QuestProgress progress = GetOrCreateProgress(quest);
                if (progress.status == QuestStatus.Locked)
                    MakeQuestAvailable(quest.ResolvedId);
            }
        }

        private void BootstrapAvailableQuests()
        {
            if (availableOnStart == null || availableOnStart.Length == 0)
                return;

            foreach (string questId in availableOnStart)
            {
                if (string.IsNullOrEmpty(questId))
                    continue;

                TryMakeAvailable(questId);
            }
        }

        public bool MakeQuestAvailable(string questId)
        {
            QuestDefinition definition = QuestRegistry.Resolve(questId);
            if (definition == null)
                return false;

            QuestProgress progress = GetOrCreateProgress(definition);
            if (progress.status != QuestStatus.Locked)
                return progress.status == QuestStatus.Available;

            progress.status = QuestStatus.Available;
            NotifyUpdated(progress);
            return true;
        }

        public void NotifyNpcTalked(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
                return;

            foreach (KeyValuePair<string, QuestProgress> pair in progressById)
            {
                QuestProgress progress = pair.Value;
                if (progress == null || progress.status != QuestStatus.Active)
                    continue;

                QuestDefinition definition = QuestRegistry.Resolve(pair.Key);
                if (definition?.objectives == null)
                    continue;

                bool changed = false;
                for (int i = 0; i < definition.objectives.Count; i++)
                {
                    QuestObjectiveDefinition objective = definition.objectives[i];
                    if (objective == null || objective.type != QuestObjectiveType.TalkToNpc)
                        continue;

                    if (!MatchesNpcTarget(objective.targetId, npcId))
                        continue;

                    progress.SetObjectiveProgress(i, Mathf.Max(1, objective.requiredCount));
                    changed = true;
                }

                if (!changed)
                    continue;

                if (AreAllObjectivesComplete(definition, progress))
                    CompleteQuest(definition.ResolvedId);
                else
                    NotifyUpdated(progress);
            }
        }

        public bool NotifyLocationReached(string locationId)
        {
            if (string.IsNullOrEmpty(locationId))
                return false;

            bool anyUpdated = false;
            foreach (KeyValuePair<string, QuestProgress> pair in progressById)
            {
                QuestProgress progress = pair.Value;
                if (progress == null || progress.status != QuestStatus.Active)
                    continue;

                QuestDefinition definition = QuestRegistry.Resolve(pair.Key);
                if (definition?.objectives == null)
                    continue;

                bool changed = false;
                for (int i = 0; i < definition.objectives.Count; i++)
                {
                    QuestObjectiveDefinition objective = definition.objectives[i];
                    if (objective == null || objective.type != QuestObjectiveType.ReachLocation)
                        continue;

                    if (!MatchesTargetId(objective.targetId, locationId))
                        continue;

                    progress.SetObjectiveProgress(i, Mathf.Max(1, objective.requiredCount));
                    changed = true;
                }

                if (!changed)
                    continue;

                anyUpdated = true;
                if (AreAllObjectivesComplete(definition, progress))
                    CompleteQuest(definition.ResolvedId);
                else
                    NotifyUpdated(progress);
            }

            return anyUpdated;
        }

        public void NotifyItemCrafted(ItemData item, int amount)
        {
            if (item == null || amount <= 0)
                return;

            NotifyObjectiveMatch(QuestObjectiveType.CraftItem, item.name, amount, item);
            NotifyObjectiveMatch(QuestObjectiveType.CraftItem, item.itemName, amount, item);
        }

        public void NotifyActivity(string activityId, int amount = 1)
        {
            if (string.IsNullOrEmpty(activityId) || amount <= 0)
                return;

            NotifyObjectiveMatch(QuestObjectiveType.Custom, activityId, amount, null);
        }

        private void NotifyObjectiveMatch(QuestObjectiveType type, string targetId, int amount, ItemData craftedItem)
        {
            foreach (KeyValuePair<string, QuestProgress> pair in progressById)
            {
                QuestProgress progress = pair.Value;
                if (progress == null || progress.status != QuestStatus.Active)
                    continue;

                QuestDefinition definition = QuestRegistry.Resolve(pair.Key);
                if (definition?.objectives == null)
                    continue;

                bool changed = false;
                for (int i = 0; i < definition.objectives.Count; i++)
                {
                    QuestObjectiveDefinition objective = definition.objectives[i];
                    if (objective == null || objective.type != type)
                        continue;

                    if (type == QuestObjectiveType.CraftItem)
                    {
                        if (!MatchesCraftTarget(objective.targetId, targetId, craftedItem))
                            continue;
                    }
                    else if (!MatchesTargetId(objective.targetId, targetId))
                    {
                        continue;
                    }

                    int required = Mathf.Max(1, objective.requiredCount);
                    int current = progress.GetObjectiveProgress(i);
                    progress.SetObjectiveProgress(i, Mathf.Min(required, current + amount));
                    changed = true;
                }

                if (!changed)
                    continue;

                if (AreAllObjectivesComplete(definition, progress))
                    CompleteQuest(definition.ResolvedId);
                else
                    NotifyUpdated(progress);
            }
        }

        private static bool MatchesNpcTarget(string targetId, string npcId)
        {
            return MatchesTargetId(targetId, npcId);
        }

        private static bool MatchesTargetId(string targetId, string value)
        {
            return !string.IsNullOrEmpty(targetId)
                && !string.IsNullOrEmpty(value)
                && string.Equals(targetId, value, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesCraftTarget(string targetId, string itemKey, ItemData craftedItem)
        {
            if (string.IsNullOrEmpty(targetId))
                return false;

            if (MatchesTargetId(targetId, itemKey))
                return true;

            if (craftedItem != null && MatchesItemTarget(craftedItem, targetId))
                return true;

            RecipeDefinition recipe = RecipeRegistry.Resolve(targetId);
            return recipe?.outputItem != null && craftedItem != null && recipe.outputItem == craftedItem;
        }

        private void TryMakeAvailable(string questId)
        {
            QuestDefinition definition = QuestRegistry.Resolve(questId);
            if (definition == null)
                return;

            QuestProgress progress = GetOrCreateProgress(definition);
            if (progress.status == QuestStatus.Locked)
            {
                progress.status = QuestStatus.Available;
                NotifyUpdated(progress);
            }
        }

        private QuestProgress GetOrCreateProgress(QuestDefinition definition)
        {
            string id = definition.ResolvedId;
            if (progressById.TryGetValue(id, out QuestProgress existing))
                return existing;

            QuestProgress progress = new QuestProgress(id, QuestStatus.Locked, definition.objectives.Count);
            progressById[id] = progress;
            return progress;
        }

        private void HandleInventoryChanged()
        {
            BindInventorySystem();
            RefreshCollectItemObjectives();
        }

        private void RefreshCollectItemObjectives()
        {
            BindInventorySystem();
            if (inventorySystem == null)
                return;
            foreach (KeyValuePair<string, QuestProgress> pair in progressById)
            {
                if (pair.Value.status != QuestStatus.Active)
                    continue;

                QuestDefinition definition = QuestRegistry.Resolve(pair.Key);
                if (definition == null)
                    continue;

                RefreshCollectItemObjectivesForQuest(definition, pair.Value);
            }
        }

        private void RefreshCollectItemObjectivesForQuest(QuestDefinition definition, QuestProgress progress)
        {
            if (inventorySystem == null || definition.objectives == null)
                return;

            bool changed = false;
            for (int i = 0; i < definition.objectives.Count; i++)
            {
                QuestObjectiveDefinition objective = definition.objectives[i];
                if (objective == null || objective.type != QuestObjectiveType.CollectItem)
                    continue;

                int inventoryCount = CountItemInInventory(objective.targetId);
                int required = Mathf.Max(1, objective.requiredCount);
                int newProgress = Mathf.Min(inventoryCount, required);
                if (progress.GetObjectiveProgress(i) != newProgress)
                {
                    progress.SetObjectiveProgress(i, newProgress);
                    changed = true;
                }
            }

            if (!changed && progress.status != QuestStatus.Completed)
                return;

            if (progress.status == QuestStatus.Completed && !AreAllObjectivesComplete(definition, progress))
            {
                progress.status = QuestStatus.Active;
                NotifyUpdated(progress);
                return;
            }

            if (progress.status == QuestStatus.Active && AreAllObjectivesComplete(definition, progress))
                CompleteQuest(definition.ResolvedId);
            else
                NotifyUpdated(progress);
        }

        private void UpdateActiveCollectObjectivesForItem(ItemData collectedItem)
        {
            if (collectedItem == null)
                return;

            BindInventorySystem();
            if (inventorySystem == null)
                return;

            foreach (KeyValuePair<string, QuestProgress> pair in progressById)
            {
                if (pair.Value.status != QuestStatus.Active)
                    continue;

                QuestDefinition definition = QuestRegistry.Resolve(pair.Key);
                if (definition?.objectives == null)
                    continue;

                bool affectsQuest = false;
                for (int i = 0; i < definition.objectives.Count; i++)
                {
                    QuestObjectiveDefinition objective = definition.objectives[i];
                    if (objective == null || objective.type != QuestObjectiveType.CollectItem)
                        continue;

                    if (MatchesItemTarget(collectedItem, objective.targetId))
                    {
                        affectsQuest = true;
                        break;
                    }
                }

                if (affectsQuest)
                    RefreshCollectItemObjectivesForQuest(definition, pair.Value);
            }
        }

        private int CountItemInInventory(string targetId)
        {
            if (inventorySystem == null || string.IsNullOrEmpty(targetId))
                return 0;

            ItemData resolvedTarget = ItemRegistry.Resolve(targetId);
            int total = 0;
            for (int i = 0; i < inventorySystem.slots.Count; i++)
            {
                InventorySystem.InventorySlot slot = inventorySystem.slots[i];
                if (slot == null || slot.IsEmpty || slot.item == null)
                    continue;

                if (MatchesItemTarget(slot.item, targetId, resolvedTarget))
                    total += slot.amount;
            }

            return total;
        }

        private static bool MatchesItemTarget(ItemData item, string targetId, ItemData resolvedTarget = null)
        {
            if (item == null || string.IsNullOrEmpty(targetId))
                return false;

            resolvedTarget ??= ItemRegistry.Resolve(targetId);
            if (resolvedTarget != null && item == resolvedTarget)
                return true;

            if (string.Equals(item.name, targetId, StringComparison.OrdinalIgnoreCase))
                return true;

            return string.Equals(item.itemName, targetId, StringComparison.OrdinalIgnoreCase);
        }

        private bool TryConsumeCollectItemObjectives(QuestDefinition definition)
        {
            if (inventorySystem == null || definition.objectives == null)
                return true;

            for (int i = 0; i < definition.objectives.Count; i++)
            {
                QuestObjectiveDefinition objective = definition.objectives[i];
                if (objective == null || objective.type != QuestObjectiveType.CollectItem)
                    continue;

                int required = Mathf.Max(1, objective.requiredCount);
                if (CountItemInInventory(objective.targetId) < required)
                    return false;
            }

            for (int i = 0; i < definition.objectives.Count; i++)
            {
                QuestObjectiveDefinition objective = definition.objectives[i];
                if (objective == null || objective.type != QuestObjectiveType.CollectItem)
                    continue;

                int required = Mathf.Max(1, objective.requiredCount);
                if (!RemoveItemsFromInventory(objective.targetId, required))
                    return false;
            }

            return true;
        }

        private bool RemoveItemsFromInventory(string targetId, int amount)
        {
            if (inventorySystem == null || amount <= 0)
                return true;

            int remaining = amount;
            for (int i = 0; i < inventorySystem.slots.Count && remaining > 0; i++)
            {
                InventorySystem.InventorySlot slot = inventorySystem.slots[i];
                if (slot == null || slot.IsEmpty || slot.item == null)
                    continue;

                if (!MatchesItemTarget(slot.item, targetId))
                    continue;

                int removeAmount = Mathf.Min(remaining, slot.amount);
                if (!inventorySystem.RemoveItemAt(i, removeAmount))
                    return false;

                remaining -= removeAmount;
            }

            return remaining <= 0;
        }

        private void BindInventorySystem()
        {
            InventorySystem preferred = ResolvePlayerInventory();
            if (preferred == null)
                return;

            if (inventorySystem == preferred)
                return;

            UnbindInventorySystem();
            inventorySystem = preferred;
            inventorySystem.OnInventoryChanged += HandleInventoryChanged;
        }

        private static InventorySystem ResolvePlayerInventory()
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
            {
                InventorySystem onPlayer = player.GetComponent<InventorySystem>();
                if (onPlayer != null)
                    return onPlayer;
            }

            return FindAnyObjectByType<InventorySystem>();
        }

        private void UnbindInventorySystem()
        {
            if (inventorySystem == null)
                return;

            inventorySystem.OnInventoryChanged -= HandleInventoryChanged;
            inventorySystem = null;
        }

        private static bool AreAllObjectivesComplete(QuestDefinition definition, QuestProgress progress)
        {
            if (definition.objectives == null || definition.objectives.Count == 0)
                return true;

            for (int i = 0; i < definition.objectives.Count; i++)
            {
                QuestObjectiveDefinition objective = definition.objectives[i];
                if (objective == null)
                    continue;

                int required = Mathf.Max(1, objective.requiredCount);
                if (progress.GetObjectiveProgress(i) < required)
                    return false;
            }

            return true;
        }

        private void NotifyUpdated(QuestProgress progress)
        {
            OnQuestUpdated?.Invoke(progress);
        }
    }
}

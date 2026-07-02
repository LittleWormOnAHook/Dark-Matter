using System;
using System.Collections.Generic;
using Project.AI;
using Project.Data;
using UnityEngine;

namespace Project.Achievements
{
    [CreateAssetMenu(menuName = "Project/Achievements/Dynamic Achievement Template", fileName = "DynamicAchievementTemplate")]
    public class DynamicAchievementTemplate : ScriptableObject
    {
        public string templateId;
        public string titleFormat = "Gather {count} {target}";
        public string descriptionFormat = "Collect {count} units of {target}.";
        public AchievementTriggerType triggerType = AchievementTriggerType.CollectItem;
        public Vector2Int countRange = new Vector2Int(25, 75);
        public string poolTag = "resource";
        public int xpReward = 40;
        public int sortOrder = 1000;

        public string ResolvedId => string.IsNullOrEmpty(templateId) ? name : templateId;
    }

    public static class DynamicAchievementGenerator
    {
        private const int ActiveGoalCount = 3;
        private static readonly List<DynamicAchievementTemplate> Templates = new List<DynamicAchievementTemplate>();
        private static int lastRefreshLevel;

        public static void EnsureSessionGoals()
        {
            if (Templates.Count == 0)
                LoadTemplates();

            AchievementManager manager = AchievementManager.EnsureExists();
            if (manager == null)
                return;

            int existing = CountActiveDynamicGoals(manager);
            if (existing >= ActiveGoalCount)
                return;

            int playerLevel = Project.Progression.PlayerProgressionManager.EnsureExists()?.Level ?? 1;
            GenerateGoals(manager, playerLevel, ActiveGoalCount - existing);
        }

        public static void RefreshIfNeeded(int playerLevel)
        {
            if (playerLevel < 5 || playerLevel % 5 != 0 || playerLevel == lastRefreshLevel)
                return;

            lastRefreshLevel = playerLevel;
            AchievementManager manager = AchievementManager.EnsureExists();
            if (manager == null)
                return;

            ClearDynamicGoals(manager);
            GenerateGoals(manager, playerLevel, ActiveGoalCount);
        }

        private static void LoadTemplates()
        {
            Templates.Clear();
            DynamicAchievementTemplate[] loaded =
                Resources.LoadAll<DynamicAchievementTemplate>("Achievements/Templates");
            if (loaded != null && loaded.Length > 0)
            {
                Templates.AddRange(loaded);
                return;
            }

            Templates.Add(CreateBuiltinTemplate(
                "gather_resource",
                "Gather {count} {target}",
                "Collect {count} units of {target}.",
                AchievementTriggerType.CollectItem,
                new Vector2Int(30, 60),
                "resource"));

            Templates.Add(CreateBuiltinTemplate(
                "defeat_enemy",
                "Defeat {count} {target}",
                "Eliminate {count} {target} hostiles.",
                AchievementTriggerType.KillEnemy,
                new Vector2Int(5, 15),
                "enemy"));
        }

        private static DynamicAchievementTemplate CreateBuiltinTemplate(
            string id,
            string titleFormat,
            string descriptionFormat,
            AchievementTriggerType trigger,
            Vector2Int countRange,
            string poolTag)
        {
            DynamicAchievementTemplate template = ScriptableObject.CreateInstance<DynamicAchievementTemplate>();
            template.templateId = id;
            template.titleFormat = titleFormat;
            template.descriptionFormat = descriptionFormat;
            template.triggerType = trigger;
            template.countRange = countRange;
            template.poolTag = poolTag;
            return template;
        }

        private static void GenerateGoals(AchievementManager manager, int playerLevel, int count)
        {
            if (count <= 0)
                return;

            int tierBoost = Mathf.Max(0, playerLevel / 5);
            for (int i = 0; i < count; i++)
            {
                DynamicAchievementTemplate template = Templates[UnityEngine.Random.Range(0, Templates.Count)];
                if (!TryBuildGoal(template, tierBoost, out AchievementDefinition definition))
                    continue;

                manager.RegisterRuntimeDefinition(definition);
            }
        }

        private static bool TryBuildGoal(DynamicAchievementTemplate template, int tierBoost, out AchievementDefinition definition)
        {
            definition = null;
            if (template == null)
                return false;

            if (!TryPickTarget(template.poolTag, out string targetId, out string targetLabel))
                return false;

            int count = UnityEngine.Random.Range(template.countRange.x, template.countRange.y + 1);
            count += tierBoost * 5;

            string achievementId = $"dynamic:{template.ResolvedId}_{targetId}_{count}";
            definition = ScriptableObject.CreateInstance<AchievementDefinition>();
            definition.achievementId = achievementId;
            definition.category = AchievementCategory.Dynamic;
            definition.triggerType = template.triggerType;
            definition.targetCount = count;
            definition.targetId = targetId;
            definition.xpReward = template.xpReward + tierBoost * 10;
            definition.sortOrder = template.sortOrder;
            definition.title = template.titleFormat
                .Replace("{count}", count.ToString())
                .Replace("{target}", targetLabel);
            definition.description = template.descriptionFormat
                .Replace("{count}", count.ToString())
                .Replace("{target}", targetLabel);
            return true;
        }

        private static bool TryPickTarget(string poolTag, out string targetId, out string targetLabel)
        {
            targetId = null;
            targetLabel = null;

            if (poolTag == "enemy")
            {
                IReadOnlyList<EnemyDefinition> enemies = EnemyRegistry.GetAllEnemies();
                if (enemies != null && enemies.Count > 0)
                {
                    EnemyDefinition enemy = enemies[UnityEngine.Random.Range(0, enemies.Count)];
                    targetId = string.IsNullOrEmpty(enemy.enemyId) ? enemy.name : enemy.enemyId;
                    targetLabel = string.IsNullOrEmpty(enemy.displayName) ? targetId : enemy.displayName;
                    return true;
                }

                targetId = "hostile";
                targetLabel = "Hostile";
                return true;
            }

            ItemData[] items = ItemRegistry.GetAllItems();
            List<ItemData> pool = new List<ItemData>();
            for (int i = 0; i < items.Length; i++)
            {
                ItemData item = items[i];
                if (item == null)
                    continue;

                pool.Add(item);
            }

            if (pool.Count == 0)
                return false;

            ItemData picked = pool[UnityEngine.Random.Range(0, pool.Count)];
            targetId = picked.name;
            targetLabel = string.IsNullOrEmpty(picked.itemName) ? picked.name : picked.itemName;
            return true;
        }

        private static int CountActiveDynamicGoals(AchievementManager manager)
        {
            int count = 0;
            foreach (AchievementProgress progress in manager.GetAllProgress())
            {
                if (progress == null || string.IsNullOrEmpty(progress.achievementId))
                    continue;

                if (progress.achievementId.StartsWith("dynamic:", StringComparison.Ordinal) && !progress.unlocked)
                    count++;
            }

            return count;
        }

        private static void ClearDynamicGoals(AchievementManager manager)
        {
            List<string> staleIds = new List<string>();
            foreach (AchievementProgress progress in manager.GetAllProgress())
            {
                if (progress == null || progress.unlocked)
                    continue;

                if (!string.IsNullOrEmpty(progress.achievementId)
                    && progress.achievementId.StartsWith("dynamic:", StringComparison.Ordinal))
                {
                    staleIds.Add(progress.achievementId);
                }
            }

            for (int i = 0; i < staleIds.Count; i++)
            {
                AchievementProgress progress = manager.GetProgress(staleIds[i]);
                if (progress != null)
                {
                    progress.currentCount = 0;
                    progress.unlocked = false;
                    progress.unlockedAtTicks = 0;
                }
            }
        }
    }
}

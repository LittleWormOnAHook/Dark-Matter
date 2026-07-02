using UnityEngine;

namespace Project.Achievements
{
    internal static class AchievementStarterCatalog
    {
        public static void RegisterIfEmpty()
        {
            if (AchievementRegistry.GetAllAchievements().Count > 0)
                return;

            AchievementManager manager = AchievementManager.EnsureExists();
            if (manager == null)
                return;

            Register(manager, "first_pet", "First Companion", "Adopt your first pet.", AchievementCategory.Pets,
                AchievementTriggerType.AdoptPet, 1, null, 75, hidden: false, sortOrder: 10);

            Register(manager, "trio_leader", "Trio Leader", "Assign a full three-pioneer expedition.", AchievementCategory.Pioneers,
                AchievementTriggerType.AssignTrio, 3, null, 100, hidden: false, sortOrder: 20);

            Register(manager, "artisan", "Artisan", "Craft five items.", AchievementCategory.Crafting,
                AchievementTriggerType.CraftItem, 5, null, 80, hidden: false, sortOrder: 30);

            Register(manager, "survivor", "Survivor", "Reach level 5.", AchievementCategory.General,
                AchievementTriggerType.ReachLevel, 1, "5", 150, hidden: false, sortOrder: 40);

            Register(manager, "hidden_signal", "Signal Found", "Discover a hidden resonance imprint.", AchievementCategory.Exploration,
                AchievementTriggerType.Manual, 1, null, 120, hidden: true, sortOrder: 90);
        }

        private static void Register(
            AchievementManager manager,
            string id,
            string title,
            string description,
            AchievementCategory category,
            AchievementTriggerType trigger,
            int targetCount,
            string targetId,
            int xp,
            bool hidden,
            int sortOrder)
        {
            AchievementDefinition definition = ScriptableObject.CreateInstance<AchievementDefinition>();
            definition.achievementId = id;
            definition.title = title;
            definition.description = description;
            definition.category = category;
            definition.triggerType = trigger;
            definition.targetCount = targetCount;
            definition.targetId = targetId;
            definition.xpReward = xp;
            definition.hidden = hidden;
            definition.sortOrder = sortOrder;
            manager.RegisterRuntimeDefinition(definition);
        }
    }
}

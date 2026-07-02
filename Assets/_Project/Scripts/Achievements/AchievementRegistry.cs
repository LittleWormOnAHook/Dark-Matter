using System.Collections.Generic;
using UnityEngine;

namespace Project.Achievements
{
    [CreateAssetMenu(menuName = "Project/Achievements/Achievement Registry", fileName = "AchievementRegistry")]
    public class AchievementRegistry : ScriptableObject
    {
        private static AchievementRegistry cached;
        private static readonly List<AchievementDefinition> RuntimeAchievements = new List<AchievementDefinition>();

        [SerializeField] private AchievementDefinition[] achievements;

        public static AchievementRegistry Instance
        {
            get
            {
                if (cached == null)
                    cached = Resources.Load<AchievementRegistry>("Achievements/AchievementRegistry");

                return cached;
            }
        }

        public static IReadOnlyList<AchievementDefinition> GetAllAchievements()
        {
            List<AchievementDefinition> result = new List<AchievementDefinition>();

            AchievementRegistry registry = Instance;
            if (registry != null && registry.achievements != null)
            {
                foreach (AchievementDefinition achievement in registry.achievements)
                {
                    if (achievement != null)
                        result.Add(achievement);
                }
            }

            foreach (AchievementDefinition runtime in RuntimeAchievements)
            {
                if (runtime != null && !ContainsAchievement(result, runtime))
                    result.Add(runtime);
            }

            return result;
        }

        public static AchievementDefinition Resolve(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId))
                return null;

            foreach (AchievementDefinition achievement in GetAllAchievements())
            {
                if (achievement != null && achievement.ResolvedId == achievementId)
                    return achievement;
            }

            return null;
        }

        public static void RegisterRuntimeAchievement(AchievementDefinition achievement)
        {
            if (achievement == null || ContainsAchievement(RuntimeAchievements, achievement))
                return;

            RuntimeAchievements.Add(achievement);
        }

        private static bool ContainsAchievement(List<AchievementDefinition> list, AchievementDefinition achievement)
        {
            string id = achievement.ResolvedId;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].ResolvedId == id)
                    return true;
            }

            return false;
        }
    }
}

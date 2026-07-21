using System;
using UnityEngine;

namespace Project.Achievements
{
    [Serializable]
    public class AchievementProgress
    {
        public string achievementId;
        public int currentCount;
        public bool unlocked;
        public long unlockedAtTicks;

        public AchievementProgress()
        {
        }

        public AchievementProgress(string id)
        {
            achievementId = id;
        }
    }
}

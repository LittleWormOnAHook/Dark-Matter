using Project.AI;
using UnityEngine;

namespace Project.Achievements
{
    public class AchievementEnemyKillRelay : MonoBehaviour
    {
        public static AchievementEnemyKillRelay EnsureExists()
        {
            AchievementEnemyKillRelay existing = FindAnyObjectByType<AchievementEnemyKillRelay>();
            if (existing != null)
                return existing;

            GameObject host = new GameObject("AchievementEnemyKillRelay");
            DontDestroyOnLoad(host);
            return host.AddComponent<AchievementEnemyKillRelay>();
        }

        private void OnEnable()
        {
            EnemyKillEvents.EnemyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            EnemyKillEvents.EnemyKilled -= HandleEnemyKilled;
        }

        private void HandleEnemyKilled(string enemyTypeId)
        {
            AchievementManager manager = AchievementManager.EnsureExists();
            manager?.ReportProgress(AchievementTriggerType.KillEnemy, enemyTypeId);
            manager?.ReportProgress(AchievementTriggerType.KillEnemy, null);
        }
    }
}

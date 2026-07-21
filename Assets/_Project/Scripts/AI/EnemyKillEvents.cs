using System;

namespace Project.AI
{
    public static class EnemyKillEvents
    {
        public static event Action<string> EnemyKilled;

        public static void Notify(string enemyTypeId)
        {
            EnemyKilled?.Invoke(enemyTypeId);
        }
    }
}

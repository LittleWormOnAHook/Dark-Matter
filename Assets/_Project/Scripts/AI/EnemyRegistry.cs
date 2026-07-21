using System.Collections.Generic;
using UnityEngine;

namespace Project.AI
{
    [CreateAssetMenu(menuName = "Project/Combat/Enemy Registry", fileName = "EnemyRegistry")]
    public class EnemyRegistry : ScriptableObject
    {
        private static EnemyRegistry cached;
        private static readonly List<EnemyDefinition> RuntimeEnemies = new List<EnemyDefinition>();

        [SerializeField] private EnemyDefinition[] enemies;

        public static EnemyRegistry Instance
        {
            get
            {
                if (cached == null)
                    cached = Resources.Load<EnemyRegistry>("EnemyRegistry");

                return cached;
            }
        }

        public static EnemyDefinition Resolve(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId))
                return null;

            foreach (EnemyDefinition enemy in GetAllEnemies())
            {
                if (enemy == null)
                    continue;

                if (string.Equals(enemy.enemyId, enemyId, System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(enemy.name, enemyId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return enemy;
                }
            }

            return null;
        }

        public static IReadOnlyList<EnemyDefinition> GetAllEnemies()
        {
            List<EnemyDefinition> result = new List<EnemyDefinition>();
            HashSet<EnemyDefinition> seen = new HashSet<EnemyDefinition>();

            EnemyRegistry registry = Instance;
            if (registry?.enemies != null)
            {
                for (int i = 0; i < registry.enemies.Length; i++)
                {
                    EnemyDefinition enemy = registry.enemies[i];
                    if (enemy != null && seen.Add(enemy))
                        result.Add(enemy);
                }
            }

            for (int i = 0; i < RuntimeEnemies.Count; i++)
            {
                EnemyDefinition enemy = RuntimeEnemies[i];
                if (enemy != null && seen.Add(enemy))
                    result.Add(enemy);
            }

            if (result.Count == 0)
            {
                for (int i = 0; i < DefaultEnemyTargets.Length; i++)
                    result.Add(GetOrCreateFallback(DefaultEnemyTargets[i].id, DefaultEnemyTargets[i].label));
            }

            return result;
        }

        public static void RegisterRuntimeEnemy(EnemyDefinition enemy)
        {
            if (enemy == null || RuntimeEnemies.Contains(enemy))
                return;

            RuntimeEnemies.Add(enemy);
        }

        private static readonly Dictionary<string, EnemyDefinition> FallbackById =
            new Dictionary<string, EnemyDefinition>(System.StringComparer.OrdinalIgnoreCase);

        private static readonly (string id, string label)[] DefaultEnemyTargets =
        {
            ("gongo", "Gongo"),
            ("enemy", "Enemy"),
            ("the_evil_one", "The Evil One")
        };

        private static EnemyDefinition GetOrCreateFallback(string enemyId, string displayName)
        {
            if (FallbackById.TryGetValue(enemyId, out EnemyDefinition existing))
                return existing;

            EnemyDefinition definition = CreateInstance<EnemyDefinition>();
            definition.enemyId = enemyId;
            definition.displayName = displayName;
            definition.name = displayName;
            FallbackById[enemyId] = definition;
            return definition;
        }
    }
}

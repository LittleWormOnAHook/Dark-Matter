using UnityEngine;

namespace Project.Progression
{
    [CreateAssetMenu(menuName = "Project/Progression/Progression Curve", fileName = "ProgressionCurve")]
    public class ProgressionCurveDefinition : ScriptableObject
    {
        [Tooltip("XP required to reach level 2 from level 1.")]
        public int baseXpToLevel = 100;

        [Tooltip("Exponent applied per level: required = base * Pow(level, exponent).")]
        [Range(1f, 2.5f)]
        public float levelExponent = 1.35f;

        [Tooltip("Optional explicit thresholds per level index (level 2 at index 0). When empty, formula is used.")]
        public int[] xpRequiredPerLevel;

        public int GetXpRequiredForLevel(int targetLevel)
        {
            if (targetLevel <= 1)
                return 0;

            int index = targetLevel - 2;
            if (xpRequiredPerLevel != null && index >= 0 && index < xpRequiredPerLevel.Length)
                return Mathf.Max(1, xpRequiredPerLevel[index]);

            float level = Mathf.Max(2, targetLevel);
            return Mathf.Max(1, Mathf.RoundToInt(baseXpToLevel * Mathf.Pow(level, levelExponent)));
        }

        public int GetTotalXpForLevel(int level)
        {
            level = Mathf.Max(1, level);
            int total = 0;
            for (int i = 2; i <= level; i++)
                total += GetXpRequiredForLevel(i);

            return total;
        }
    }
}

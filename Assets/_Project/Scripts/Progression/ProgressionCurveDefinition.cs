using UnityEngine;

namespace Project.Progression
{
    /// <summary>
    /// Live XP curve: hybrid mild exp + linear.
    /// XP required to go from (N−1) → N: round(expScale · N^expPower + linearScale · N), N ≥ 2.
    /// Approved target ~9M cumulative XP to <see cref="MaxLevel"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Project/Progression/Progression Curve", fileName = "ProgressionCurve")]
    public class ProgressionCurveDefinition : ScriptableObject
    {
        public const int MaxLevel = 200;

        /// <summary>Approved default: mild exponential coefficient.</summary>
        public const float DefaultExpScale = 55f;

        /// <summary>Approved default: mild exponential power.</summary>
        public const float DefaultExpPower = 1.42f;

        /// <summary>Approved default: linear coefficient.</summary>
        public const float DefaultLinearScale = 25f;

        [Tooltip("Mild exponential scale: round(expScale * N^expPower + linearScale * N).")]
        public float expScale = DefaultExpScale;

        [Tooltip("Mild exponential power applied to target level N.")]
        [Range(1f, 2.5f)]
        public float expPower = DefaultExpPower;

        [Tooltip("Linear scale added per level N.")]
        public float linearScale = DefaultLinearScale;

        [Tooltip("Optional explicit thresholds per level index (level 2 at index 0). When empty, hybrid formula is used.")]
        public int[] xpRequiredPerLevel;

        /// <summary>
        /// XP required to advance from <paramref name="targetLevel"/> − 1 to <paramref name="targetLevel"/>.
        /// </summary>
        public int GetXpRequiredForLevel(int targetLevel)
        {
            if (targetLevel <= 1)
                return 0;

            if (targetLevel > MaxLevel)
                return 0;

            int index = targetLevel - 2;
            if (xpRequiredPerLevel != null && index >= 0 && index < xpRequiredPerLevel.Length)
                return Mathf.Max(1, xpRequiredPerLevel[index]);

            return EvaluateHybridXp(targetLevel, expScale, expPower, linearScale);
        }

        /// <summary>Lifetime XP floor for a character at <paramref name="level"/> (sum of XP(2)..XP(level)).</summary>
        public int GetTotalXpForLevel(int level)
        {
            level = Mathf.Clamp(level, 1, MaxLevel);
            int total = 0;
            for (int i = 2; i <= level; i++)
                total += GetXpRequiredForLevel(i);

            return total;
        }

        /// <summary>Shared hybrid evaluator used by the asset and null-curve fallbacks.</summary>
        public static int EvaluateHybridXp(
            int targetLevel,
            float expScale = DefaultExpScale,
            float expPower = DefaultExpPower,
            float linearScale = DefaultLinearScale)
        {
            float n = Mathf.Max(2, targetLevel);
            return Mathf.Max(1, Mathf.RoundToInt(expScale * Mathf.Pow(n, expPower) + linearScale * n));
        }
    }
}

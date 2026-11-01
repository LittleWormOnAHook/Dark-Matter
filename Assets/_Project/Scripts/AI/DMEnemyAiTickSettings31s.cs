// 2026-10-31s: enemy AI tick spreading. Edited in Genesis Studio (Sky Essentials > Performance).
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Spreads enemy perception (line-of-sight target query) over several frames while an enemy is NOT engaged.
    /// Engaged / aggro enemies always tick every frame, so combat behaviour (and body weight) is unchanged.
    /// Asset: Resources/AI/DMEnemyAiTickSettings31s.asset (defaults used when missing).
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/AI/Enemy AI Tick Settings (31s)")]
    public sealed class DMEnemyAiTickSettings31s : ScriptableObject
    {
        public const string ResourcePath = "AI/DMEnemyAiTickSettings31s";
        [Tooltip("Spread calm-enemy perception over several frames (staggered per enemy).")]
        public bool staggerEnabled = true;
        [Tooltip("Frames between perception queries for a calm enemy (1 = every frame).")]
        [Range(1, 8)] public int tickIntervalFrames = 3;

        private static DMEnemyAiTickSettings31s active;
        public static DMEnemyAiTickSettings31s Active
        {
            get
            {
                if (active == null) active = Resources.Load<DMEnemyAiTickSettings31s>(ResourcePath);
                if (active == null) { active = CreateInstance<DMEnemyAiTickSettings31s>(); active.hideFlags = HideFlags.HideAndDontSave; }
                return active;
            }
        }
        public static void ClearCache() { active = null; }
    }
}
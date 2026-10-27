using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// 2026-10-26b: melee lock. Gun-carrying enemies that also have a melee weapon (Humadroid) stop ranged-style
    /// backing off while the player fights them with a drawn melee weapon, and hold a close standoff so swings land.
    /// Resources/AI/DM_MeleeLockProfile, read live (Genesis Studio > Combat > Melee lock). Rollback: enableMeleeLock = false.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/AI/Melee Lock Profile", fileName = "DM_MeleeLockProfile")]
    public sealed class DM_MeleeLockProfile : ScriptableObject
    {
        public const string ResourcesPath = "AI/DM_MeleeLockProfile";
        private static DM_MeleeLockProfile live, fallback;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLiveCache() { live = null; }

        public static DM_MeleeLockProfile Live
        {
            get
            {
                if (live == null) live = Resources.Load<DM_MeleeLockProfile>(ResourcesPath);
                if (live != null) return live;
                if (fallback == null) { fallback = CreateInstance<DM_MeleeLockProfile>(); fallback.hideFlags = HideFlags.HideAndDontSave; }
                return fallback;
            }
        }

        [Header("Melee lock (stay in melee range vs a melee player)")]
        [Tooltip("On = listed enemy types drop their ranged hold / retreat while the player has a melee weapon drawn nearby.")]
        public bool enableMeleeLock = true;
        [Tooltip("EnemyDefinition ids (or prefab name parts), ';' separated. '*' = every gun enemy that also has a melee weapon.")]
        public string enemyTypes = "Humadroid";
        [Tooltip("Only lock enemies whose definition has a melee weapon (so they can actually strike back).")]
        public bool requireEnemyMeleeWeapon = true;
        [Tooltip("Lock engages when the melee-armed player is within this distance (m).")]
        [Range(1f, 30f)] public float engageDistance = 8f;
        [Tooltip("Distance the locked enemy holds from the player (m). Player sword reach is ~1.6 m; 1.0 keeps swings landing.")]
        [Range(0.4f, 3f)] public float holdDistance = 1.0f;
        [Tooltip("Seconds the lock persists after the player holsters / leaves range (stops flip-flopping).")]
        [Range(0f, 8f)] public float releaseSeconds = 1.5f;

        public bool AppliesTo(EnemyDefinition def, string objectName)
        {
            if (!enableMeleeLock) return false;
            if (requireEnemyMeleeWeapon && (def == null || def.meleeWeaponItem == null)) return false;
            string spec = enemyTypes ?? "";
            foreach (string raw in spec.Split(';'))
            {
                string k = raw.Trim();
                if (k.Length == 0) continue;
                if (k == "*") return true;
                if (def != null && string.Equals(def.enemyId, k, System.StringComparison.OrdinalIgnoreCase)) return true;
                if (!string.IsNullOrEmpty(objectName) && objectName.IndexOf(k, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        /// <summary>0 Subtle, 1 Default, 2 Extreme.</summary>
        public void ApplyPreset(int level)
        {
            enableMeleeLock = true;
            switch (level)
            {
                case 0: engageDistance = 5f; holdDistance = 1.3f; releaseSeconds = 0.8f; break;
                case 2: engageDistance = 14f; holdDistance = 0.75f; releaseSeconds = 3f; break;
                default: engageDistance = 8f; holdDistance = 1.0f; releaseSeconds = 1.5f; break;
            }
        }
    }
}

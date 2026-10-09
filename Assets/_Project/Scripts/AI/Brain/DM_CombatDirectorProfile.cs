using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Combat Plan Phase 4 start (§31 #4): encounter-level attack slot rules layered on top of the Phase 3
    /// <see cref="DMEnemyEngagementDirector"/>. Resources/Combat/DM_CombatDirectorProfile, read live every tick
    /// so Genesis Studio / Combat Studio edits apply in Play.
    /// <para>
    /// <b>Rollback:</b> <see cref="enableCombatDirector"/> = false puts the director back to Phase 3 behaviour
    /// (exactly one melee Engager per target; everyone else Holds on the ring).
    /// </para>
    /// Flank and morale are stubbed fields only for now; attack slots are the live feature.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Combat/Combat Director Profile", fileName = "DM_CombatDirectorProfile")]
    public sealed class DM_CombatDirectorProfile : ScriptableObject
    {
        public const string ResourcesPath = "Combat/DM_CombatDirectorProfile";

        private static DM_CombatDirectorProfile live;
        private static DM_CombatDirectorProfile fallback;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLiveCache()
        {
            live = null;
        }

        /// <summary>Canonical profile; falls back to code defaults if the asset is missing.</summary>
        public static DM_CombatDirectorProfile Live
        {
            get
            {
                if (live == null)
                    live = Resources.Load<DM_CombatDirectorProfile>(ResourcesPath);
                if (live != null)
                    return live;
                if (fallback == null)
                {
                    fallback = CreateInstance<DM_CombatDirectorProfile>();
                    fallback.hideFlags = HideFlags.HideAndDontSave;
                }

                return fallback;
            }
        }

        [Header("Master / rollback")]
        [Tooltip("Off = Phase 3 only: exactly one melee Engager per target, everyone else Holds. Rollback switch for attack slots.")]
        public bool enableCombatDirector = true;
        [Tooltip("Logs slot awards / releases to the console (editor/dev builds).")]
        public bool debugLogSlots = false;

        [Header("Attack slots by enemy count (the token holder counts as slot 1)")]
        [Tooltip("0 = each band uses its Min slots, 1 = its Max slots. Encounter intensity dial (§10). Bands with Min = Max ignore it.")]
        [Range(0f, 1f)] public float intensity = 0.5f;
        [Tooltip("1v1 is always 1 attacker. Small band: 2 up to this many enemies.")]
        [Range(2, 8)] public int smallGroupMaxEnemies = 4;
        [Range(1, 6)] public int smallGroupSlotsMin = 1;
        [Range(1, 6)] public int smallGroupSlotsMax = 2;
        [Tooltip("Medium band: above the small band up to this many enemies.")]
        [Range(3, 16)] public int mediumGroupMaxEnemies = 8;
        [Range(1, 8)] public int mediumGroupSlotsMin = 2;
        [Range(1, 8)] public int mediumGroupSlotsMax = 3;
        [Tooltip("Larger fights (above the medium band): this many slots at the first count above the medium band…")]
        [Range(1, 10)] public int largeGroupBaseSlots = 3;
        [Tooltip("…plus one extra slot for every this-many additional enemies.")]
        [Range(1, 16)] public int largeGroupEnemiesPerExtraSlot = 4;
        [Tooltip("Large band never exceeds this many slots.")]
        [Range(1, 12)] public int largeGroupSlotsMax = 5;
        [Tooltip("Studio hard cap: never more simultaneous melee attackers than this per target, whatever the bands say.")]
        [Range(1, 12)] public int maxSimultaneousAttackers = 5;

        [Header("Slot rotation (secondary attackers)")]
        [Tooltip("A secondary attacker keeps its slot at least this long unless it becomes ineligible.")]
        [Range(0f, 10f)] public float slotMinHoldSeconds = 1.5f;
        [Tooltip("Secondary attacker yields its slot after this many attack sequences (if another enemy is waiting).")]
        [Range(1, 10)] public int slotHoldMaxSequences = 2;
        [Tooltip("Secondary attacker yields its slot after this long (if another enemy is waiting).")]
        [Range(1f, 30f)] public float slotHoldMaxSeconds = 5f;
        [Tooltip("A newly slotted attacker waits a random time in this range before its first swing (tell).")]
        public Vector2 slotEntryGrace = new Vector2(0.5f, 1f);
        [Tooltip("At most one slot change per target in this window (anti-thrash).")]
        [Range(0f, 5f)] public float minSecondsBetweenSlotChanges = 0.6f;
        [Tooltip("A yielding attacker can't be re-slotted for this long (ignored when nobody else is eligible).")]
        [Range(0f, 10f)] public float slotReawardCooldown = 1.5f;

        [Header("Flanking (stub — not wired yet)")]
        public bool enableFlank = false;
        [Range(0, 4)] public int flankMaxConcurrent = 1;
        [Range(0f, 10f)] public float flankMinHoldSeconds = 2f;
        [Range(0f, 1f)] public float flankPersonalityBonus = 0.25f;

        [Header("Group morale (stub — not wired yet)")]
        public bool enableMorale = false;
        [Range(0f, 1f)] public float startingMorale = 1f;
        [Range(0f, 1f)] public float retreatMoraleThreshold = 0.35f;
        [Range(0f, 1f)] public float leaderDeathMoraleDrop = 0.4f;
        [Range(0f, 1f)] public float casualtyMoraleDrop = 0.1f;
        [Range(0f, 1f)] public float gruesomeKillMoraleDrop = 0.15f;
        [Range(0f, 0.5f)] public float moraleRecoverPerSecond = 0.02f;

        /// <summary>
        /// Total simultaneous melee attackers (token holder included) for a fight with
        /// <paramref name="enemyCount"/> eligible enemies. 1 when the director is off (Phase 3 behaviour).
        /// </summary>
        public int ResolveTotalSlots(int enemyCount)
        {
            if (!enableCombatDirector || enemyCount <= 1)
                return 1;

            int slots;
            if (enemyCount <= smallGroupMaxEnemies)
            {
                slots = PickFromBand(smallGroupSlotsMin, smallGroupSlotsMax);
            }
            else if (enemyCount <= Mathf.Max(smallGroupMaxEnemies, mediumGroupMaxEnemies))
            {
                slots = PickFromBand(mediumGroupSlotsMin, mediumGroupSlotsMax);
            }
            else
            {
                int beyond = enemyCount - Mathf.Max(smallGroupMaxEnemies, mediumGroupMaxEnemies) - 1;
                int extra = beyond / Mathf.Max(1, largeGroupEnemiesPerExtraSlot);
                slots = Mathf.Min(Mathf.Max(1, largeGroupSlotsMax), Mathf.Max(1, largeGroupBaseSlots) + extra);
            }

            slots = Mathf.Min(slots, Mathf.Max(1, maxSimultaneousAttackers));
            return Mathf.Clamp(slots, 1, enemyCount);
        }

        private int PickFromBand(int a, int b)
        {
            int min = Mathf.Max(1, Mathf.Min(a, b));
            int max = Mathf.Max(1, Mathf.Max(a, b));
            int picked = min + Mathf.FloorToInt((max - min + 1) * Mathf.Clamp01(intensity));
            return Mathf.Clamp(picked, min, max);
        }

        public float RandomRange(Vector2 range)
        {
            float a = Mathf.Min(range.x, range.y);
            float b = Mathf.Max(range.x, range.y);
            return Random.Range(a, b);
        }
    }
}

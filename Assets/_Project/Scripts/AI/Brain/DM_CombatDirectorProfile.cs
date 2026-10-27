using UnityEngine;

namespace Project.AI
{
    public enum DMCombatAttackMode { Gameplay, Testing }

    /// <summary>
    /// Combat Plan Phase 4 start (§31 #4): encounter-level attack slot rules layered on top of the Phase 3
    /// <see cref="DMEnemyEngagementDirector"/>. Resources/Combat/DM_CombatDirectorProfile, read live every tick
    /// so Genesis Studio / Combat Studio edits apply in Play.
    /// <para>
    /// <b>Rollback:</b> <see cref="enableCombatDirector"/> = false puts the director back to Phase 3 behaviour
    /// (exactly one melee Engager per target; everyone else Holds on the ring).
    /// </para>
    /// 2026-10-25s: flank (side/back hold points) and group morale are live too.
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

        [Header("Attack mode (testing vs gameplay)")]
        [Tooltip("Testing: one attacker per target and every enemy gets a first turn before anyone gets a second (audit / clip checks). Gameplay: random token picks, up to maxSimultaneousAttackers at once plus a random chance for extra attackers.")]
        public DMCombatAttackMode attackMode = DMCombatAttackMode.Gameplay;
        [Tooltip("Gameplay mode: chance, rolled every extraAttackerRollSeconds per target, that extra enemies attack on top of the cap.")]
        [Range(0f, 1f)] public float extraAttackerChance = 0.25f;
        [Range(1, 4)] public int extraAttackersMax = 1;
        [Range(1f, 30f)] public float extraAttackerRollSeconds = 6f;
        /// <summary>Set by the runtime audit: forces Testing mode whatever the asset says.</summary>
        public static bool ForceTestingMode;
        public bool IsTestingMode => ForceTestingMode || attackMode == DMCombatAttackMode.Testing;

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
        [Tooltip("Gameplay mode cap: never more simultaneous melee attackers than this per target (before the random extra attackers). Testing mode always uses 1.")]
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

        [Header("Flanking (Phase 4 / Slice 5 E1, 2026-10-25s)")]
        [Tooltip("Director sends a holder to the target's side / back hold point (no NavMesh, hold-ring anchors). Off = plain ring.")]
        public bool enableFlank = true;
        [Range(0, 6)] public int flankMaxConcurrent = 1;
        [Tooltip("A flanker keeps the role at least this long before rotation.")]
        [Range(0f, 20f)] public float flankMinHoldSeconds = 2f;
        [Tooltip("Flanker rotates out after this long (gives the role to someone else).")]
        [Range(1f, 60f)] public float flankMaxHoldSeconds = 7f;
        [Range(0f, 30f)] public float flankCooldownSeconds = 2.5f;
        [Tooltip("Bearing of the flank hold point from the target's facing (90 = side, 180 = straight behind).")]
        [Range(45f, 180f)] public float flankAngleDegrees = 115f;
        [Tooltip("Minimum enemies on the target before anyone flanks (1v1 / 1v2 never flank).")]
        [Range(2, 12)] public int flankMinEnemies = 3;
        [Tooltip("Reserved (personality weighting, not read yet).")]
        [Range(0f, 1f)] public float flankPersonalityBonus = 0.25f;

        [Header("Group morale (Phase 4, 2026-10-25s)")]
        [Tooltip("Per-target group morale: casualties / leader death / gruesome kills lower it; below the threshold the group breaks (outer ring, no extra attackers).")]
        public bool enableMorale = true;
        [Range(0f, 1f)] public float startingMorale = 1f;
        [Range(0f, 1f)] public float retreatMoraleThreshold = 0.35f;
        [Tooltip("Extra drop when the group's first member (leader) dies.")]
        [Range(0f, 1f)] public float leaderDeathMoraleDrop = 0.4f;
        [Range(0f, 1f)] public float casualtyMoraleDrop = 0.1f;
        [Tooltip("Extra drop when the kill was a critical or charged hit.")]
        [Range(0f, 1f)] public float gruesomeKillMoraleDrop = 0.15f;
        [Range(0f, 0.5f)] public float moraleRecoverPerSecond = 0.02f;
        [Tooltip("Morale must climb back above threshold + this before the group re-commits.")]
        [Range(0f, 0.5f)] public float moraleRecoverHysteresis = 0.1f;
        public bool moraleBrokenOuterRing = true;
        public bool moraleBrokenNoExtraSlots = true;
        public bool debugLogFlankMorale = false;

        /// <summary>2026-10-25s: 0 Subtle, 1 Default, 2 Extreme (flank + morale).</summary>
        public void ApplyFlankMoralePreset(int level)
        {
            enableFlank = true; enableMorale = true;
            switch (level)
            {
                case 0: flankMaxConcurrent = 1; flankMinHoldSeconds = 3f; flankMaxHoldSeconds = 5f; flankCooldownSeconds = 6f; flankAngleDegrees = 90f; flankMinEnemies = 4;
                    leaderDeathMoraleDrop = 0.25f; casualtyMoraleDrop = 0.06f; gruesomeKillMoraleDrop = 0.08f; retreatMoraleThreshold = 0.25f; moraleRecoverPerSecond = 0.04f; break;
                case 2: flankMaxConcurrent = 3; flankMinHoldSeconds = 1.5f; flankMaxHoldSeconds = 12f; flankCooldownSeconds = 1f; flankAngleDegrees = 160f; flankMinEnemies = 2;
                    leaderDeathMoraleDrop = 0.6f; casualtyMoraleDrop = 0.18f; gruesomeKillMoraleDrop = 0.3f; retreatMoraleThreshold = 0.5f; moraleRecoverPerSecond = 0.01f; break;
                default: flankMaxConcurrent = 1; flankMinHoldSeconds = 2f; flankMaxHoldSeconds = 7f; flankCooldownSeconds = 2.5f; flankAngleDegrees = 115f; flankMinEnemies = 3;
                    leaderDeathMoraleDrop = 0.4f; casualtyMoraleDrop = 0.1f; gruesomeKillMoraleDrop = 0.15f; retreatMoraleThreshold = 0.35f; moraleRecoverPerSecond = 0.02f; break;
            }
        }

        /// <summary>
        /// Total simultaneous melee attackers (token holder included) for a fight with
        /// <paramref name="enemyCount"/> eligible enemies. 1 when the director is off (Phase 3 behaviour).
        /// </summary>
        public int ResolveTotalSlots(int enemyCount)
        {
            if (!enableCombatDirector || enemyCount <= 1 || IsTestingMode)
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

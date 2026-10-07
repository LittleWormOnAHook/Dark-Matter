using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Enemy spacing + melee attack token tuning (Enemy Spacing plan §5 / §7, merged into Combat Plan Phase 3).
    /// Resources/Combat/DM_EnemyEngagementProfile, read live every tick so Genesis Studio edits apply in Play.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Combat/Enemy Engagement Profile", fileName = "DM_EnemyEngagementProfile")]
    public sealed class DM_EnemyEngagementProfile : ScriptableObject
    {
        public const string ResourcesPath = "Combat/DM_EnemyEngagementProfile";

        private static DM_EnemyEngagementProfile live;
        private static DM_EnemyEngagementProfile fallback;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLiveCache()
        {
            live = null;
        }

        /// <summary>Canonical profile; falls back to code defaults if the asset is missing.</summary>
        public static DM_EnemyEngagementProfile Live
        {
            get
            {
                if (live == null)
                    live = Resources.Load<DM_EnemyEngagementProfile>(ResourcesPath);
                if (live != null)
                    return live;
                if (fallback == null)
                {
                    fallback = CreateInstance<DM_EnemyEngagementProfile>();
                    fallback.hideFlags = HideFlags.HideAndDontSave;
                }

                return fallback;
            }
        }

        [Header("Master")]
        [Tooltip("Off = legacy behaviour (every enemy attacks whenever in range). Rollback switch for the whole spacing system.")]
        public bool enableEngagementDirector = true;
        [Tooltip("Legacy per-enemy ±42° ring slot rotation (the cause of the constant circling). Leave off.")]
        public bool legacyRingSlotRotation = false;
        [Tooltip("Logs token hand-offs to the console (editor/dev builds).")]
        public bool debugLogHandoffs = false;

        [Header("Ticks")]
        [Range(1f, 20f)] public float directorTickHz = 5f;
        [Range(2f, 30f)] public float agentRoleTickHz = 10f;

        [Header("Melee token (D1: exactly one attacker per target)")]
        [Tooltip("Current token holder keeps it at least this long unless it becomes ineligible.")]
        [Range(0f, 10f)] public float tokenMinHoldSeconds = 2.5f;
        [Tooltip("A challenger needs holder score + this margin…")]
        [Range(0f, 10f)] public float switchScoreMargin = 2f;
        [Tooltip("…sustained for this long.")]
        [Range(0f, 3f)] public float switchConfirmSeconds = 0.35f;
        [Tooltip("At most one hand-off per target in this window.")]
        [Range(0f, 10f)] public float minSecondsBetweenHandoffs = 1.5f;
        [Tooltip("Token can't move for this long after a swing / combo / charged attack ends.")]
        [Range(0f, 2f)] public float postSwingLockSeconds = 0.25f;
        [Tooltip("Holder releases the token after this many attack sequences (if anyone else is eligible).")]
        [Range(1, 10)] public int tokenHoldMaxSequences = 2;
        [Tooltip("Holder releases the token after this long (if anyone else is eligible).")]
        [Range(1f, 30f)] public float tokenHoldMaxSeconds = 6f;
        [Tooltip("New Engager waits a random time in this range before its first swing (hand-over tell).")]
        public Vector2 handoffGraceBeforeSwing = new Vector2(0.6f, 1.2f);
        [Tooltip("The old holder can't be re-awarded the token for this long (ignored when nobody else is eligible).")]
        [Range(0f, 10f)] public float reawardCooldown = 1.5f;
        [Tooltip("Enemies farther than this from the target are not token candidates.")]
        [Range(2f, 30f)] public float candidateMaxDistance = 8f;

        [Header("Focus score weights (§5.4)")]
        public float weightPlayerHit = 5f;
        [Tooltip("Seconds the 'player hit this enemy' bonus lasts (linear decay).")]
        [Range(0.1f, 6f)] public float playerHitMemory = 1.5f;
        public float weightPlayerTargeting = 4f;
        public float weightPlayerFacing = 3f;
        [Tooltip("Full facing score inside this half-angle of the player's facing / camera forward…")]
        [Range(0f, 90f)] public float facingConeFull = 20f;
        [Tooltip("…falling to zero at this half-angle.")]
        [Range(1f, 180f)] public float facingConeZero = 60f;
        public float weightPositioning = 2f;
        [Tooltip("Positioning score reaches zero at this distance.")]
        [Range(1f, 20f)] public float positioningRange = 6f;
        [Tooltip("Fairness: score per second since this enemy last held the token.")]
        public float weightWaitPerSecond = 0.25f;
        public float weightWaitMax = 2f;
        [Tooltip("Bonus while the punish rule is armed on this enemy (3 hits in 4 s).")]
        public float weightPunishPriority = 3f;

        [Header("Engager range hysteresis (× effective attack range)")]
        [Range(0.5f, 1.5f)] public float engageInFactor = 1f;
        [Range(1f, 2.5f)] public float engageOutFactor = 1.35f;
        [Tooltip("Must stay outside engageOutFactor this long before leaving Attack.")]
        [Range(0f, 2f)] public float engageOutGrace = 0.4f;
        [Tooltip("Creep in only when farther than this × effective range…")]
        [Range(0.5f, 1.2f)] public float creepStartFactor = 0.95f;
        [Tooltip("…for this long.")]
        [Range(0f, 2f)] public float creepGrace = 0.25f;
        [Range(0.2f, 1.2f)] public float creepSpeedFactor = 0.62f;

        [Header("Engager facing")]
        [Tooltip("Swing only when the target is inside this half-angle (turn first, at most attackFacingMaxTurnSeconds).")]
        [Range(5f, 90f)] public float attackFacingConeHalfAngle = 25f;
        [Range(0f, 2f)] public float attackFacingMaxTurnSeconds = 0.5f;
        [Range(30f, 720f)] public float engagerTurnRate = 200f;
        [Tooltip("Light swings track the target at this rate until trackUntil (normalized), then lock.")]
        [Range(0f, 720f)] public float lightTrackRate = 120f;
        [Range(0f, 1f)] public float lightTrackUntil = 0.30f;
        [Range(0f, 720f)] public float chargedTrackRate = 60f;
        [Range(0f, 1f)] public float chargedTrackUntil = 0.28f;
        [Tooltip("Extra wind-up when the Engager attacks from outside the camera view (fairness).")]
        [Range(0f, 2f)] public float offscreenAttackExtraWindup = 0.25f;

        [Header("Holders (world-anchored hold points)")]
        [Range(1.5f, 12f)] public float holdRingRadius = 4f;
        [Range(1.5f, 12f)] public float holdRingRadiusMin = 3.2f;
        [Range(1.5f, 12f)] public float holdRingRadiusMax = 5.5f;
        [Tooltip("Re-anchor if the holder stays outside [min, max] from the target…")]
        [Range(1f, 10f)] public float holdBandMin = 3f;
        [Range(2f, 15f)] public float holdBandMax = 6.5f;
        [Tooltip("…for this long.")]
        [Range(0f, 3f)] public float holdBandGrace = 0.6f;
        [Tooltip("Re-anchor once the target has moved this far from the anchor origin.")]
        [Range(0.5f, 10f)] public float holdReanchorPlayerMove = 2f;
        [Tooltip("Random per-holder gap between re-anchors (seconds).")]
        public Vector2 holdReanchorCooldown = new Vector2(2.5f, 4f);
        [Range(0.5f, 5f)] public float holderMinSpacing = 1.6f;
        [Tooltip("Holders keep out of a wedge this wide (half-angle) around the target→Engager line.")]
        [Range(0f, 60f)] public float holderEngagerLineHalfAngle = 20f;
        [Range(0.5f, 4f)] public float holderWalkSpeed = 1.4f;
        [Tooltip("Farther than this from the hold point: run there facing the move direction.")]
        [Range(1f, 10f)] public float holderRunToAnchorDistance = 3f;
        [Range(10f, 360f)] public float holderTurnRate = 90f;
        [Tooltip("Holder only turns its body when the target is more than this off its facing…")]
        [Range(0f, 90f)] public float holderFacingDeadZone = 35f;
        [Tooltip("…and settles once within this.")]
        [Range(0f, 45f)] public float holderFacingSettle = 10f;

        [Header("Holder idle life (D5)")]
        [Tooltip("Each holder rolls one action every random interval in this range (seconds).")]
        public Vector2 holderActionInterval = new Vector2(3.5f, 6f);
        [Range(0f, 1f)] public float holderWeightSidestep = 0.45f;
        [Range(0f, 1f)] public float holderWeightTaunt = 0.30f;
        [Range(0f, 1f)] public float holderWeightFeint = 0.15f;
        [Range(0f, 1f)] public float holderWeightNone = 0.10f;
        public Vector2 sidestepDistance = new Vector2(0.6f, 1.2f);
        [Range(1f, 45f)] public float sidestepMaxArc = 15f;
        [Range(0f, 20f)] public float sidestepCooldown = 4f;
        [Range(0f, 20f)] public float tauntCooldown = 6f;
        public Vector2 tauntDuration = new Vector2(1f, 2f);
        [Range(0.1f, 2f)] public float feintDistance = 0.5f;
        [Tooltip("Feints only happen when farther than this from the target.")]
        [Range(0f, 10f)] public float feintMinDistance = 3.5f;
        [Tooltip("Gap between any two holder actions on the same target.")]
        [Range(0f, 5f)] public float holderGroupActionGap = 1f;
        [Range(1, 4)] public int holderMaxConcurrentActions = 1;
        [Tooltip("Anti-circling: net angular drift around the target per holder in any 10 s window.")]
        [Range(0f, 180f)] public float holderMaxDriftDegPer10s = 30f;
        [Tooltip("No sidesteps while the target moves faster than this.")]
        [Range(0f, 6f)] public float noSidestepPlayerSpeed = 1.5f;
        [Tooltip("No sidesteps for this long after a re-anchor.")]
        [Range(0f, 6f)] public float noSidestepAfterReanchor = 2f;

        [Header("Pools (designed now, used later)")]
        [Tooltip("Only the melee token holder may use the charged attack, so this is 1 by construction.")]
        public int chargedTokensPerTarget = 1;
        [Tooltip("Future ranged enemies. Ranged enemies keep legacy behaviour in this build.")]
        public int rangedShotTokensPerTarget = 2;

        public float RandomRange(Vector2 range)
        {
            float a = Mathf.Min(range.x, range.y);
            float b = Mathf.Max(range.x, range.y);
            return Random.Range(a, b);
        }
    }
}

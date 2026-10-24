using System;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Utility brain tuning (Combat Plan Ã‚Â§5Ã¢â‚¬â€œÃ‚Â§8, Ã‚Â§12, Phase 3). Score per action =
    /// archetype base Ãƒâ€” personality multipliers Ãƒâ€” condition multiplier + small randomness. Highest wins.
    /// Resources/Combat/DM_EnemyBrainProfile, read live so Genesis Studio edits apply in Play.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Combat/Enemy Brain Profile", fileName = "DM_EnemyBrainProfile")]
    public sealed class DM_EnemyBrainProfile : ScriptableObject
    {
        public const string ResourcesPath = "Combat/DM_EnemyBrainProfile";

        private static DM_EnemyBrainProfile live;
        private static DM_EnemyBrainProfile fallback;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLiveCache()
        {
            live = null;
        }

        public static DM_EnemyBrainProfile Live
        {
            get
            {
                if (live == null)
                    live = Resources.Load<DM_EnemyBrainProfile>(ResourcesPath);
                if (live != null)
                    return live;
                if (fallback == null)
                {
                    fallback = CreateInstance<DM_EnemyBrainProfile>();
                    fallback.hideFlags = HideFlags.HideAndDontSave;
                }

                return fallback;
            }
        }

        [Serializable]
        public struct ArchetypeRow
        {
            public DMEnemyArchetype archetype;
            [Tooltip("Base utility of pressing the attack while holding the token.")]
            public float press;
            [Tooltip("Base utility of a block / dodge when threatened.")]
            public float defend;
            [Tooltip("Base utility of a desperation retreat (scaled by condition).")]
            public float retreat;
            [Tooltip("Added to the director focus score (wants the token more / less).")]
            public float tokenBias;
            [Tooltip("Multiplies holder taunt weight.")]
            public float holderTaunt;
            [Tooltip("Multiplies holder feint weight.")]
            public float holderFeint;
        }

        [Serializable]
        public struct PersonalityRow
        {
            public DMEnemyPersonality personality;
            public float pressMul;
            public float defendMul;
            public float retreatMul;
            public float tokenBiasAdd;
            public float holderSidestepMul;
            public float holderTauntMul;
            public float holderFeintMul;
        }

        [Header("Master")]
        [Tooltip("Off = enemies skip utility decisions (no brain-driven defend / retreat, no token bias). Spacing still runs from the engagement profile.")]
        public bool enableUtilityBrain = true;
        [Tooltip("Add a DMEnemyBrain to every EnemyAiController that doesn't have one (uses the defaults below).")]
        public bool autoAttachBrain = true;
        public DMEnemyArchetype defaultArchetype = DMEnemyArchetype.Duelist;
        public DMEnemyPersonality defaultPersonality = DMEnemyPersonality.None;
        public bool debugLogDecisions = false;
        [Tooltip("Play mode: gold movement history on enemies with DMEnemyMovementPathDebug. Buffer reserved for future path analysis.")]
        public bool enableMovementPathDebug = false;
        [Tooltip("When path debug is visible: dashed yellow virtual steer polyline (root â†’ fan hint â†’ move target). Memory only â€” no path objects.")]
        public bool drawVirtualSteerPath = false;

        [Header("Decisions")]
        [Tooltip("The brain re-scores this often and holds its pick until the next decision.")]
        [Range(0.1f, 3f)] public float decisionInterval = 0.8f;
        [Tooltip("Ã‚Â± random added to every score (seeded per enemy).")]
        [Range(0f, 1f)] public float scoreRandomness = 0.25f;
        [Tooltip("Defend is only scored if the target hit this enemy within this many seconds, or is inside its attack range.")]
        [Range(0f, 5f)] public float defendThreatMemory = 1.2f;
        [Tooltip("Added to Defend when the target hit this enemy recently.")]
        public float defendWhenHitBonus = 0.6f;
        [Range(0f, 20f)] public float defendCooldown = 3f;
        [Range(0f, 1f)] public float defendBlockShare = 0.6f;

        [Header("Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance)")]
        [Tooltip("Master switch. Off = the guard pose is cosmetic again (hits are not absorbed, the player is not recoiled).")]
        public bool enableEnemyBlock = true;
        [Tooltip("Reactive block: chance an enemy facing the player snaps its guard up as a player melee hit arrives (when the brain guard is not already up). Uses blockCooldown, blockArcDegrees and the definition blockChanceMultiplier. 0 = brain-only blocks (rare).")]
        [Range(0f, 1f)] public float reactiveBlockChance = 0.25f;
        [Tooltip("Seconds before THIS enemy may start another block. Blocks only start between swings while the enemy holds the attack token, so the one-attacker cap is untouched.")]
        [Range(0f, 30f)] public float blockCooldown = 4f;
        [Tooltip("How long a block lasts (s). 0 = use the prefab defensiveBlockDuration (1.2).")]
        [Range(0f, 4f)] public float blockDurationSeconds = 1f;
        [Tooltip("Fraction of the damage taken by a blocked hit (0 = full absorb). Poise is fed with the reduced damage, so a block also reduces poise damage.")]
        [Range(0f, 1f)] public float blockDamageFraction = 0.1f;
        [Tooltip("Frontal arc (degrees, total) in which a blocking enemy can block. Hits from behind land normally.")]
        [Range(30f, 360f)] public float blockArcDegrees = 150f;
        [Tooltip("Light (non-charged) player melee hits can be blocked.")]
        public bool blockLightHits = true;
        [Tooltip("Charged / heavy player melee hits can be blocked (otherwise they land fully and the block stays up).")]
        public bool blockChargedHits = true;
        [Tooltip("A blockable charged / heavy hit breaks the guard: the block drops and the hit lands normally (full damage, normal reaction). A poise break also drops the guard.")]
        public bool chargedBreaksBlock = true;
        [Tooltip("Chance a blocked hit recoils the player (Invector BreakAttack with playerBlockedRecoilId). Rolled per blocked hit.")]
        [Range(0f, 1f)] public float playerBlockedRecoilChance = 0.6f;
        [Tooltip("Player animator recoil id used for a blocked-hit recoil. 1 = recoil_unarmed (the clip the player own guard impact uses), 2 = recoil_low (default, visibly different), 3 = recoil_hard (CustomAction, long; clears the player block). The enemy block reaction is a procedural flinch, so these never look the same.")]
        [Range(1, 3)] public int playerBlockedRecoilId = 2;
        [Tooltip("After a recoil, the player cannot be recoiled again by a blocked hit for this long (no stun-lock).")]
        [Range(0f, 6f)] public float playerBlockedRecoilImmunitySeconds = 1.5f;
        [Tooltip("Blocked-hit sparks at the blade contact point: scale (player parry sparks use DM_CombatCoreProfile parryClashVfxScale 1.15).")]
        [Range(0.3f, 2.5f)] public float blockSparkScale = 0.8f;
        [Tooltip("Optional spark prefab for enemy blocks. Empty = the same SparksLong prefab the parry uses.")]
        public GameObject blockSparkPrefab;
        [Tooltip("Camera shake on a blocked hit (player block uses 0.18 / 0.08).")]
        [Range(0f, 2f)] public float blockedHitShakeAmplitude = 0.3f;
        [Range(0f, 0.4f)] public float blockedHitShakeDurationSeconds = 0.1f;
        [Tooltip("Sound played when an enemy blocks a player hit (empty = none).")]
        public AudioClip blockedHitSfx;
        [Range(0f, 1f)] public float blockedHitSfxVolume = 0.8f;

        [Header("Melee attacker cap (2026-10-13)")]

        [Tooltip("Limit simultaneous melee swings on one target; ranged enemies are not counted.")]

        public bool meleeAttackerCapEnabled = true;

        [Range(1, 6)] public int maxMeleeAttackers = 2;

        [Tooltip("Random delay between melee swing starts (seconds).")]

        [Range(0f, 2f)] public float meleeSwingStaggerMin = 0.4f;

        [Range(0f, 2f)] public float meleeSwingStaggerMax = 0.8f;


        [Header("Enemy ranged aim (2026-10-13)")]

        [Tooltip("Player bone enemy guns aim at by default (pistol + rifle).")]

        public HumanBodyBones enemyAimTargetBone = HumanBodyBones.Chest;

        [Tooltip("Chance per shot to aim at the head instead.")]

        [Range(0f, 1f)] public float enemyHeadshotChance = 0.15f;


        [Header("Enemy visual aim correction (2026-10-13e)")]


        [Tooltip("While aiming, rotate the chest so the gun barrel points at the aim point (yaw + pitch). Projectiles unchanged.")]


        public bool enemyAimCorrectionEnabled = true;


        [Range(0f, 1f)] public float enemyAimCorrectionStrength = 1f;


        [Range(0f, 90f)] public float enemyAimCorrectionMaxAngle = 60f;


        [Tooltip("Smoothing speed, degrees per second.")]


        public float enemyAimCorrectionSpeed = 360f;

        [Tooltip("Horizontal metres from target at which barrel aim correction reaches full strength.")]
        [Range(1f, 24f)] public float enemyAimCorrectionFullDistance = 8f;

        [Tooltip("Below this horizontal distance (metres), barrel aim correction fades out (avoids close-range hunch / spin).")]
        [Range(0.5f, 8f)] public float enemyAimCorrectionCloseDisableDistance = 2.5f;

        [Tooltip("At close range, clamp chest pitch aim to this many degrees (upper body only).")]
        [Range(5f, 45f)] public float enemyAimCorrectionCloseMaxPitch = 28f;

        [Tooltip("Fire via Invector weapon.Shoot without ShooterManager.ApplyRecoil (stops per-shot spine kick at close range).")]
        public bool enemySkipInvectorShootRecoil = true;


        [Tooltip("Debug: once-per-second per-enemy log of the ranged fire gate that blocks a shot.")]


        public bool logRangedFireGates = false;
        [Tooltip("Ranged enemies keep firing while they back off to their hold distance (else a close player is never shot).")]
        public bool rangedFireWhileRetreating = true;
        [Tooltip("Ranged engage range = max(definition engage, min(weapon range, vision range)): enemies shoot as far as they can see and hit.")]
        public bool reconcileRangedEngageToVision = true;
        [Tooltip("Rifle (two-handed ranged) users: base engage range and vision floor, metres. Pistol unchanged. Extra range from upgrades is added via EnemyInvectorCombatBridge.RifleRangeBonus.")]
        [Range(0f, 80f)] public float rifleEngageRangeBase = 25f;
        [Range(0f, 80f)] public float rifleVisionFloor = 25f;

        [Header("Chase stuck recovery (2026-10-24ac)")]
        [Tooltip("Chase / investigate / search: if the enemy barely moves for chaseStuckSeconds while its goal is still far, sidestep around the blocker (alternating sides).")]
        public bool enableChaseStuckRecovery = true;
        [Range(0.5f, 5f)] public float chaseStuckSeconds = 1.5f;
        [Tooltip("Less horizontal movement than this (m) over chaseStuckSeconds = stuck.")]
        [Range(0.05f, 2f)] public float chaseStuckMinMove = 0.4f;
        [Tooltip("Sideways distance of the recovery sidestep (m).")]
        [Range(0.5f, 5f)] public float chaseStuckSidestepMeters = 2f;
        [Tooltip("Terrain steeper than this (deg) blocks like a wall; gentler terrain is ground, never a wall (slope stalls).")]
        [Range(30f, 89f)] public float chaseTerrainMaxSlopeDeg = 60f;
        [Tooltip("Mesh colliders (rocks, props) count as ground only where their surface is flatter than this (deg): no snapping up rock flanks.")]
        [Range(10f, 80f)] public float groundMeshMaxSlopeDeg = 50f;
        [Tooltip("24ad: after this many failed sidesteps in a row (enemy wedged inside / against a rock mesh), move it to the nearest free grounded spot. 0 = never.")]
        [Range(0, 6)] public int chaseStuckHardAfter = 2;
        [Tooltip("24ad: max search radius (m) for the hard-unstick free spot.")]
        [Range(1f, 8f)] public float chaseStuckHardMaxRadius = 4f;
        public bool chaseStuckLog = true;

        /// <summary>0 = Subtle, 1 = Default, 2 = Extreme.</summary>
        public void ApplyChaseStuckPreset(int preset)
        {
            enableChaseStuckRecovery = true;
            switch (preset)
            {
                case 0: chaseStuckSeconds = 2.5f; chaseStuckMinMove = 0.25f; chaseStuckSidestepMeters = 1.2f; chaseTerrainMaxSlopeDeg = 55f; groundMeshMaxSlopeDeg = 45f; chaseStuckHardAfter = 3; chaseStuckHardMaxRadius = 3f; break;
                case 2: chaseStuckSeconds = 0.8f; chaseStuckMinMove = 0.6f; chaseStuckSidestepMeters = 3.5f; chaseTerrainMaxSlopeDeg = 70f; groundMeshMaxSlopeDeg = 40f; chaseStuckHardAfter = 1; chaseStuckHardMaxRadius = 6f; break;
                default: chaseStuckSeconds = 1.5f; chaseStuckMinMove = 0.4f; chaseStuckSidestepMeters = 2f; chaseTerrainMaxSlopeDeg = 60f; groundMeshMaxSlopeDeg = 50f; chaseStuckHardAfter = 2; chaseStuckHardMaxRadius = 4f; break;
            }
        }

        [Header("Stall watchdog (2026-10-24ae)")]
        [Tooltip("Force-release AI locomotion pause if it is held longer than this (s) while alive (a hit reaction / stagger that never released). 0 = off.")]
        [Range(0f, 15f)] public float stallWatchdogPauseSeconds = 6f;
        [Tooltip("Restore animator speed to 1 if it stays at 0 longer than this (s) (hit-stop never ended = frozen pose). 0 = off.")]
        [Range(0f, 5f)] public float stallWatchdogAnimatorSeconds = 1.5f;
        public bool stallWatchdogLog = true;

        /// <summary>24ae: obstacle avoidance (predictive whisker fan around rocks / props). 0 = Subtle, 1 = Default, 2 = Extreme.</summary>
        public void ApplyObstacleAvoidancePreset(int preset)
        {
            enableChaseWallSlide = true;
            enableChaseWallPredictiveFan = true;
            switch (preset)
            {
                case 0: chaseWallFanRayCount = 5; chaseWallFanHalfAngleDegrees = 35f; chaseWallFanLookAheadScale = 1.2f; chaseWallFanLatchSeconds = 0.5f; chaseWallFanDirectionHysteresis = 0.1f; chaseWallFanMinRetargetSeconds = 0.4f; enableChaseWallLongRangeFan = false; break;
                case 2: chaseWallFanRayCount = 11; chaseWallFanHalfAngleDegrees = 70f; chaseWallFanLookAheadScale = 2.4f; chaseWallFanLatchSeconds = 0.3f; chaseWallFanDirectionHysteresis = 0.05f; chaseWallFanMinRetargetSeconds = 0.2f; enableChaseWallLongRangeFan = true; chaseWallLongRangeFanDistance = 16f; chaseWallLongRangeFanRayCount = 5; chaseWallLongRangeFanBlend = 0.45f; break;
                default: chaseWallFanRayCount = 9; chaseWallFanHalfAngleDegrees = 55f; chaseWallFanLookAheadScale = 1.8f; chaseWallFanLatchSeconds = 0.4f; chaseWallFanDirectionHysteresis = 0.08f; chaseWallFanMinRetargetSeconds = 0.3f; enableChaseWallLongRangeFan = true; chaseWallLongRangeFanDistance = 12f; chaseWallLongRangeFanRayCount = 3; chaseWallLongRangeFanBlend = 0.3f; break;
            }
        }

        [Header("Chase wall slide (no NavMesh)")]
        [Tooltip("Capsule-cast chase steps against Default / Building / Climbable / Resource / PW blockers and slide along walls.")]
        public bool enableChaseWallSlide = true;
        [Tooltip("Fallback capsule radius when no CapsuleCollider is found on the enemy.")]
        [Range(0.15f, 0.8f)] public float chaseWallSlideRadius = 0.35f;
        [Tooltip("Fallback capsule height when no CapsuleCollider is found on the enemy.")]
        [Range(0.8f, 2.5f)] public float chaseWallSlideHeight = 1.75f;
        [Tooltip("Shrink allowed move distance before the hit normal (metres).")]
        [Range(0.01f, 0.2f)] public float chaseWallSlideSkin = 0.05f;
        [Tooltip("Standoff buffer from Default / Building / Climbable walls during chase / investigate / search (capsule cast skin).")]
        [Range(0.15f, 1.2f)] public float chaseWallStandoffMeters = 0.5f;
        [Tooltip("Seconds hugging the wall buffer or blocked before a brief retreat / sidestep nudge.")]
        [Range(0.5f, 4f)] public float chaseWallBufferStuckSeconds = 1.5f;
        [Tooltip("Metres to step along the wall outward normal when unsticking.")]
        [Range(0.2f, 2f)] public float chaseWallRetreatStepMeters = 0.85f;
        [Tooltip("Optional tangent sidestep mixed into the retreat nudge (metres).")]
        [Range(0f, 1.5f)] public float chaseWallRetreatSidestepMeters = 0.55f;
        [Tooltip("Seconds to steer toward the sidestep waypoint after a wall retreat nudge.")]
        [Range(0.2f, 2.5f)] public float chaseWallRetreatSidestepSeconds = 0.65f;
        [Tooltip("Slide-along-wall iterations per chase step.")]
        [Range(1, 4)] public int chaseWallSlidePasses = 2;
        [Tooltip("Chase steps smaller than this (metres) count as blocked for wall-follow.")]
        [Range(0.0005f, 0.05f)] public float chaseWallMinProgress = 0.002f;
        [Tooltip("Blocked frames before tangential wall-follow toward the chase target.")]
        [Range(1, 12)] public int chaseWallBlockedFramesBeforeFollow = 2;
        [Tooltip("Forward capsule probe distance used to read the wall normal when head-on.")]
        [Range(0.2f, 1.5f)] public float chaseWallProbeDistance = 0.6f;
        [Tooltip("Face the slide / wall-follow direction instead of staring into the wall.")]
        public bool chaseWallFollowFaceSlide = true;
        [Tooltip("Turn rate (deg/s) while blocked and steering along a wall.")]
        [Range(120f, 720f)] public float chaseWallBlockedTurnRate = 240f;
        [Tooltip("Min seconds to hold a wall-follow tangent side before picking the other (reduces left/right jitter).")]
        [Range(0.1f, 2f)] public float chaseWallFollowMinSideSeconds = 0.55f;
        [Tooltip("Dot-product gap required before swapping wall-follow side.")]
        [Range(0.05f, 0.5f)] public float chaseWallFollowSideHysteresis = 0.18f;
        [Tooltip("Consecutive failed wall-follow steps before flipping tangential side (reduces left-right hunting).")]
        [Range(2, 8)] public int chaseWallFollowFlipAfterFails = 3;
        [Tooltip("Capsule fan ahead of the chase target to pick a clearer direction before slide / wall-follow.")]
        public bool enableChaseWallPredictiveFan = false;
        [Range(3, 11)] public int chaseWallFanRayCount = 7;
        [Range(10f, 75f)] public float chaseWallFanHalfAngleDegrees = 42f;
        [Tooltip("Multiplies chaseWallProbeDistance for fan clearance samples.")]
        [Range(0.8f, 2.5f)] public float chaseWallFanLookAheadScale = 1.35f;
        [Range(0.15f, 1.2f)] public float chaseWallFanLatchSeconds = 0.4f;
        [Tooltip("Score margin required before replacing a latched fan direction (reduces L/R hunting).")]
        [Range(0.02f, 0.25f)] public float chaseWallFanDirectionHysteresis = 0.08f;
        [Tooltip("Min seconds between full fan re-scans when the latched direction still clears ahead.")]
        [Range(0.1f, 1.2f)] public float chaseWallFanMinRetargetSeconds = 0.32f;
        [Tooltip("Seconds to hold combat ring bearing / chase point while in wall standoff buffer.")]
        [Range(0.4f, 2.5f)] public float chaseWallRingLockSeconds = 1.05f;
        [Tooltip("Sparse long-range fan toward chase target for macro bearing only (local slide unchanged).")]
        public bool enableChaseWallLongRangeFan = false;
        [Range(8f, 24f)] public float chaseWallLongRangeFanDistance = 20f;
        [Range(3, 5)] public int chaseWallLongRangeFanRayCount = 3;
        [Tooltip("Blend weight from long-range macro bearing into short fan wish direction.")]
        [Range(0.1f, 0.55f)] public float chaseWallLongRangeFanBlend = 0.35f;
        [Tooltip("Ignore tiny standoff buffer pushes below this clearance deficit (metres).")]
        [Range(0.02f, 0.15f)] public float chaseWallStandoffPushMinDeficit = 0.07f;
        [Tooltip("Only enter tangential wall-follow when a forward probe hits geometry (not buffer-only stalls).")]
        public bool chaseWallFollowRequiresHeadOnBlock = true;
        [Tooltip("Blocked/stutter frames at a wall with no chase progress â†’ start lost-sight investigate early.")]
        [Range(4, 90)] public int chaseWallStuckFramesEarlyInvestigate = 24;
        [Tooltip("Player this many metres above the enemy with no LOS â†’ treat as unreachable direct chase (flatten / investigate).")]
        [Range(1f, 8f)] public float chaseUnreachableMinVerticalDelta = 2.25f;
        [Tooltip("Horizontal range for unreachable-roof rule (metres).")]
        [Range(2f, 20f)] public float chaseUnreachableMaxHorizontal = 12f;
        [Tooltip("Ranged attacks require EnemySenses line-of-sight (melee touch range still bypasses LOS).")]
        public bool requireClearLosForRangedFire = true;

        [Header("Lost sight â€” investigate")]
        [Tooltip("When chase loses line-of-sight to the player, go to last known position then search instead of immediately giving up.")]
        public bool enableInvestigateOnLostSight = true;
        [Tooltip("Use the search duration / radius below instead of each enemy prefab's search fields.")]
        public bool investigateUseProfileSearchTiming = true;
        [Range(2f, 30f)] public float investigateSearchDurationSeconds = 8f;
        [Range(1f, 12f)] public float investigateSearchRadiusMeters = 5f;
        [Tooltip("Walk speed scale while searching the last-known area.")]
        [Range(0.3f, 1f)] public float investigateSearchSpeedScale = 0.65f;
        [Tooltip("Seconds to pause and sweep look direction at each search waypoint.")]
        [Range(0f, 4f)] public float investigateLookSweepSeconds = 1.25f;
        [Tooltip("Degrees per second while paused on a search look sweep.")]
        [Range(30f, 360f)] public float investigateLookSweepTurnRate = 140f;
        [Tooltip("When player was elevated / unreachable, ray toward last seen and anchor search at the wall impact (metres).")]
        [Range(4f, 40f)] public float investigateWallBaseRayDistance = 22f;
        [Tooltip("Stand-off from the wall plane at the investigate anchor.")]
        [Range(0.1f, 1.2f)] public float investigateWallAnchorOffset = 0.45f;
        [Tooltip("Search by walking along the wall tangent instead of orbiting last-known XZ.")]
        public bool investigateSearchAlongWall = true;
        [Tooltip("Max patrol distance each way from the wall-base anchor (metres).")]
        [Range(2f, 16f)] public float investigateWallPatrolSpanMeters = 7f;
        [Tooltip("Seconds blind + elevated threat at the wall foot before forcing Search (hide on roof).")]
        [Range(0.5f, 8f)] public float investigateElevatedThreatSecondsToSearch = 1.35f;
        [Tooltip("Seconds stuck within ~1m of investigate anchor before committing to Search.")]
        [Range(0.5f, 6f)] public float investigateStuckCommitSeconds = 2f;
        [Tooltip("When false, melee enemies do not swing at a climbing player (ranged is unaffected).")]
        public bool allowMeleeVsClimbingPlayer = false;

        [Header("Enemy ranged ammo pool")]
        [Tooltip("Default shots each ranged enemy may fire before reload. EnemyDefinition.rangedAmmoPool > 0 overrides per type. 0 = unlimited (infinite mag).")]
        [Range(0, 999)] public int enemyRangedAmmoPoolDefault = 1;
        [Tooltip("Seconds after the last shot in a mag before the pool refills. 0 = no auto-reload (empty until respawn / Reapply).")]
        [Range(0f, 30f)] public float enemyRangedAmmoReloadSeconds = 2.5f;

        [Header("Enemy guard stamina")]
        [Tooltip("Guard stamina pool per enemy (EnemyDefinition.guardStaminaMax overrides when > 0). Empty = guard breaks.")]
        [Range(1f, 300f)] public float enemyGuardStaminaMax = 60f;
        [Range(0f, 100f)] public float enemyGuardRegenPerSec = 12f;
        [Tooltip("Seconds after the last spend before regen starts.")]
        [Range(0f, 5f)] public float enemyGuardRegenDelay = 1f;
        [Tooltip("Holding the guard drains per second = base x growth ^ seconds held (capped).")]
        [Range(0f, 30f)] public float enemyBlockHoldCostBase = 3f;
        [Range(1f, 4f)] public float enemyBlockHoldCostGrowth = 1.6f;
        [Range(0f, 100f)] public float enemyBlockHoldCostCap = 25f;
        [Tooltip("Flat cost per blocked hit.")]
        [Range(0f, 100f)] public float enemyBlockedHitStaminaCost = 12f;
        [Tooltip("Cost when staggered or the guard is broken (more than a simple block).")]
        [Range(0f, 100f)] public float enemyStaggerStaminaCost = 20f;
        [Tooltip("An enemy will not raise its guard below this stamina.")]
        [Range(0f, 100f)] public float enemyMinGuardStaminaToBlock = 15f;

        [Header("Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll)")]
        [Tooltip("When a Defend decision does not block, chance the enemy actually rolls (otherwise it falls back to a block, then to attacking). 1 = old behaviour.")]
        [Range(0f, 1f)] public float dodgeRollChance = 1f;
        [Tooltip("Seconds before THIS enemy may roll again (per enemy). Defend cooldown still applies on top.")]
        [Range(0f, 20f)] public float dodgeRollCooldown = 2f;
        [Tooltip("Relative weight of rolling to the LEFT and, separately, to the RIGHT of the threat (each side gets this weight). Old behaviour: sides 1 each, back 1.")]
        [Min(0f)] public float dodgeRollSideWeight = 1f;
        [Tooltip("Relative weight of rolling directly AWAY from the threat (backwards).")]
        [Min(0f)] public float dodgeRollBackWeight = 1f;
        [Tooltip("Relative weight of rolling TOWARD the threat. 0 = never roll forward (the forward roll does not look right). If every weight is 0 the roll goes backwards.")]
        [Min(0f)] public float dodgeRollForwardWeight = 0f;
        [Tooltip("Meters the dodge roll travels along the chosen direction (the roll clip is played in place on transform-driven enemies, so the AI moves the body). 0 = roll in place.")]
        [Range(0f, 6f)] public float dodgeRollDistance = 3.2f;
        [Tooltip("Movement of the side roll runs with the roll clip: it reaches full distance when the Roll animator state reaches this normalized time (the state exits at 0.7 and blends out for 0.1 s). Higher = movement lasts longer into the exit blend.")]
        [Range(0.3f, 1f)] public float dodgeRollMoveEndNormalized = 0.75f;
        [Tooltip("Same for the backward dodge clip (DM_DodgeBack): the displacement finishes at this normalized time of the clip.")]
        [Range(0.3f, 1f)] public float dodgeBackMoveEndNormalized = 0.85f;
        [Tooltip("Fallback / timeout length (s) of a side roll when the animator state cannot be read. Movement normally follows the clip, not this timer (was a fixed 0.65 s).")]
        [Range(0.3f, 2f)] public float dodgeRollSeconds = 1.1f;
        [Tooltip("BACKWARD dodges play the Humanoid@DodgeBackwardsCombat clip (animator state DM_DodgeBack in Invector@ShooterMelee.controller) instead of the forward-roll clip turned around. The body faces the threat and the AI moves it away. Off, or no such state on the controller = old turned-around roll.")]
        public bool dodgeBackUseClip = true;
        [Tooltip("Length (s) of the backward dodge. The clip (1.4 s) is sped up to fit (animator parameter DMDodgeBackSpeed).")]
        [Range(0.3f, 1.6f)] public float dodgeBackSeconds = 0.75f;
        [Tooltip("Meters the backward dodge travels away from the threat (the clip itself is in place; the AI moves the body).")]
        [Range(0f, 5f)] public float dodgeBackDistance = 2.2f;
        [Tooltip("Cross-fade IN time (s) from whatever the animator was playing into the backward dodge clip.")]
        [Range(0f, 0.4f)] public float dodgeBackBlendIn = 0.08f;
        [Tooltip("Cross-fade OUT time (s) from the backward dodge clip back to locomotion (starts this long before the dodge ends).")]
        [Range(0f, 0.5f)] public float dodgeBackBlendOut = 0.15f;
        [Tooltip("Turn the enemy to the roll direction instantly when the roll starts, so the roll animation (a forward roll) does not first travel toward the threat.")]
        public bool dodgeRollSnapFacing = true;
        [Tooltip("When no DM_Dodge clip fits (or the animator is not ready), play the forward Roll instead of skipping the dodge. Off = never forward roll.")]
        public bool allowForwardRollFallback = false;
        [Tooltip("Dodge start / in-dodge turn rate (deg/s): the body turns toward the dodge facing instead of snapping. 0 = old instant snap.")]
        [Range(0f, 2000f)] public float dodgeTurnDegPerSec = 720f;

        [Header("Side dodge clip (clipfix): side rolls use a real side-dodge clip, the body faces the threat and never plays the forward roll")]
        [Tooltip("On = a side roll plays DM_DodgeLeft / DM_DodgeRight (Standing Dodge Left/Right) with the body facing the threat. Off = old forward Roll clip turned sideways.")]
        public bool dodgeSideUseClip = true;
        [Tooltip("Total length of a side dodge (clip is time-scaled to this).")]
        [Range(0.3f, 1.6f)] public float dodgeSideSeconds = 0.95f;
        [Tooltip("Distance travelled by a side dodge (m).")]
        [Range(0f, 5f)] public float dodgeSideDistance = 2.6f;
        [Tooltip("Normalized clip time at which the side dodge movement is complete.")]
        [Range(0.3f, 1f)] public float dodgeSideMoveEndNormalized = 0.85f;
        [Range(0f, 0.4f)] public float dodgeSideBlendIn = 0.08f;
        [Range(0f, 0.5f)] public float dodgeSideBlendOut = 0.15f;
        [Tooltip("On = hit reactions (flinch, stagger, hit slide) are ignored while a dodge roll plays, so the roll is never cut mid-animation. Damage still applies.")]
        public bool dodgeRollBlocksHitReactions = true;
        [Tooltip("Desperation retreat: give up the token and back off to the outer ring for this long.")]
        [Range(0f, 20f)] public float retreatSeconds = 5f;
        [Range(0f, 60f)] public float retreatCooldown = 10f;

        [Header("Low-health retreat stability (2026-10-24a)")]
        [Tooltip("Health fraction a condition must recover past its threshold before it improves again (hysteresis; stops retreat toggling near a threshold). 0 = off.")]
        [Range(0f, 0.5f)] public float conditionRecoverHysteresis = 0.08f;
        [Tooltip("An attack-slot award cannot cancel a retreat during its first N seconds.")]
        [Range(0f, 10f)] public float retreatCancelGuardSeconds = 1f;
        [Tooltip("Seconds without progress toward the retreat point before one re-pick, then giving up and fighting again.")]
        [Range(0.2f, 10f)] public float retreatStuckSeconds = 1f;
        [Tooltip("Metres the enemy must close on its retreat point within the stuck window to count as progress.")]
        [Range(0.01f, 3f)] public float retreatMinProgress = 0.3f;
        [Tooltip("Bearing offset (deg) for the single stuck re-pick before giving up.")]
        [Range(0f, 180f)] public float retreatStuckRepickDegrees = 60f;
        [Tooltip("Minimum seconds between hold / retreat point re-picks (a ring-radius change no longer bypasses the re-anchor cooldown).")]
        [Range(0f, 10f)] public float retreatRepickInterval = 0.75f;
        [Tooltip("Reject hold / retreat point candidates whose body-radius path is blocked, that overlap a collider, or have no ground (collider checks only, no NavMesh).")]
        public bool retreatAnchorObstacleCheck = true;
        [Tooltip("Holder stops moving inside this distance of its point; counts as arrived at +0.1 m.")]
        [Range(0.05f, 3f)] public float holderArriveRadius = 0.5f;
        [Tooltip("Ranged back-off continues until the player is this far beyond the retreat distance (hysteresis).")]
        [Range(0f, 10f)] public float rangedRetreatHysteresis = 1f;

        [Header("Condition thresholds (health fraction)")]
        [Range(0f, 1f)] public float injuredBelow = 0.75f;
        [Range(0f, 1f)] public float severelyInjuredBelow = 0.5f;
        [Range(0f, 1f)] public float criticalBelow = 0.25f;
        [Tooltip("Press multiplier at Healthy / Injured / Severely injured / Critical.")]
        public Vector4 conditionPress = new Vector4(1f, 1f, 0.9f, 0.8f);
        public Vector4 conditionDefend = new Vector4(1f, 1.15f, 1.3f, 1.5f);
        public Vector4 conditionRetreat = new Vector4(0f, 0f, 0.4f, 1.2f);

        [Header("Archetypes (Ã‚Â§6)")]
        public ArchetypeRow[] archetypes =
        {
            new ArchetypeRow { archetype = DMEnemyArchetype.Duelist, press = 1f, defend = 0.35f, retreat = 0.6f, tokenBias = 0f, holderTaunt = 1f, holderFeint = 1f },
            new ArchetypeRow { archetype = DMEnemyArchetype.Rusher, press = 1.3f, defend = 0.15f, retreat = 0.3f, tokenBias = 1f, holderTaunt = 1.2f, holderFeint = 1.5f },
            new ArchetypeRow { archetype = DMEnemyArchetype.Berserker, press = 1.5f, defend = 0.05f, retreat = 0f, tokenBias = 1.5f, holderTaunt = 1.6f, holderFeint = 1.2f },
            new ArchetypeRow { archetype = DMEnemyArchetype.Flanker, press = 1f, defend = 0.3f, retreat = 0.6f, tokenBias = 0f, holderTaunt = 0.6f, holderFeint = 1.4f },
            new ArchetypeRow { archetype = DMEnemyArchetype.Ambusher, press = 1.1f, defend = 0.2f, retreat = 0.8f, tokenBias = 0.5f, holderTaunt = 0.4f, holderFeint = 1f },
            new ArchetypeRow { archetype = DMEnemyArchetype.Tank, press = 0.9f, defend = 0.6f, retreat = 0.1f, tokenBias = 0f, holderTaunt = 1.3f, holderFeint = 0.5f },
            new ArchetypeRow { archetype = DMEnemyArchetype.Guardian, press = 0.8f, defend = 0.7f, retreat = 0.2f, tokenBias = -0.5f, holderTaunt = 1f, holderFeint = 0.5f },
        };

        [Header("Personalities (Ã‚Â§7) Ã¢â‚¬â€ adjust, never override")]
        public PersonalityRow[] personalities =
        {
            new PersonalityRow { personality = DMEnemyPersonality.Aggressive, pressMul = 1.3f, defendMul = 0.8f, retreatMul = 0.6f, tokenBiasAdd = 0.75f, holderSidestepMul = 1f, holderTauntMul = 1.4f, holderFeintMul = 1.3f },
            new PersonalityRow { personality = DMEnemyPersonality.Cautious, pressMul = 1.05f, defendMul = 1.4f, retreatMul = 1.2f, tokenBiasAdd = -0.5f, holderSidestepMul = 1.2f, holderTauntMul = 0.6f, holderFeintMul = 0.7f },
            new PersonalityRow { personality = DMEnemyPersonality.Cowardly, pressMul = 0.8f, defendMul = 1.3f, retreatMul = 2f, tokenBiasAdd = -1f, holderSidestepMul = 1.2f, holderTauntMul = 0.3f, holderFeintMul = 0.5f },
            new PersonalityRow { personality = DMEnemyPersonality.Brave, pressMul = 1.15f, defendMul = 1f, retreatMul = 0.4f, tokenBiasAdd = 0.5f, holderSidestepMul = 1f, holderTauntMul = 1.2f, holderFeintMul = 1f },
            new PersonalityRow { personality = DMEnemyPersonality.Reckless, pressMul = 1.4f, defendMul = 0.5f, retreatMul = 0.2f, tokenBiasAdd = 1f, holderSidestepMul = 0.8f, holderTauntMul = 1.5f, holderFeintMul = 1.5f },
            new PersonalityRow { personality = DMEnemyPersonality.Defensive, pressMul = 0.9f, defendMul = 1.6f, retreatMul = 1f, tokenBiasAdd = -0.25f, holderSidestepMul = 1f, holderTauntMul = 0.8f, holderFeintMul = 0.6f },
            new PersonalityRow { personality = DMEnemyPersonality.Opportunistic, pressMul = 1.1f, defendMul = 1f, retreatMul = 1f, tokenBiasAdd = 0.25f, holderSidestepMul = 1f, holderTauntMul = 0.8f, holderFeintMul = 1.6f },
            new PersonalityRow { personality = DMEnemyPersonality.Calculating, pressMul = 1f, defendMul = 1.25f, retreatMul = 1f, tokenBiasAdd = 0f, holderSidestepMul = 1.1f, holderTauntMul = 0.5f, holderFeintMul = 1.3f },
            new PersonalityRow { personality = DMEnemyPersonality.Territorial, pressMul = 1.1f, defendMul = 1.1f, retreatMul = 0.5f, tokenBiasAdd = 0.25f, holderSidestepMul = 0.8f, holderTauntMul = 1.3f, holderFeintMul = 0.8f },
            new PersonalityRow { personality = DMEnemyPersonality.Vengeful, pressMul = 1.2f, defendMul = 0.9f, retreatMul = 0.5f, tokenBiasAdd = 0.5f, holderSidestepMul = 1f, holderTauntMul = 1.2f, holderFeintMul = 1f },
            new PersonalityRow { personality = DMEnemyPersonality.Predatory, pressMul = 1.2f, defendMul = 0.8f, retreatMul = 0.7f, tokenBiasAdd = 0.5f, holderSidestepMul = 1.2f, holderTauntMul = 0.6f, holderFeintMul = 1.4f },
            new PersonalityRow { personality = DMEnemyPersonality.Protective, pressMul = 1f, defendMul = 1.2f, retreatMul = 0.6f, tokenBiasAdd = 0f, holderSidestepMul = 1f, holderTauntMul = 1f, holderFeintMul = 0.8f },
            new PersonalityRow { personality = DMEnemyPersonality.Curious, pressMul = 1f, defendMul = 1f, retreatMul = 1f, tokenBiasAdd = 0f, holderSidestepMul = 1.2f, holderTauntMul = 0.8f, holderFeintMul = 1.2f },
        };

        public ArchetypeRow ResolveArchetype(DMEnemyArchetype archetype)
        {
            ArchetypeRow fallbackRow = new ArchetypeRow
            {
                archetype = DMEnemyArchetype.Duelist, press = 1f, defend = 0.35f, retreat = 0.6f,
                tokenBias = 0f, holderTaunt = 1f, holderFeint = 1f
            };

            if (archetypes == null)
                return fallbackRow;

            bool foundDuelist = false;
            for (int i = 0; i < archetypes.Length; i++)
            {
                if (archetypes[i].archetype == archetype)
                    return archetypes[i];
                if (!foundDuelist && archetypes[i].archetype == DMEnemyArchetype.Duelist)
                {
                    fallbackRow = archetypes[i];
                    foundDuelist = true;
                }
            }

            return fallbackRow;
        }

        public bool TryResolvePersonality(DMEnemyPersonality personality, out PersonalityRow row)
        {
            row = default;
            if (personality == DMEnemyPersonality.None || personalities == null)
                return false;

            for (int i = 0; i < personalities.Length; i++)
            {
                if (personalities[i].personality != personality)
                    continue;
                row = personalities[i];
                return true;
            }

            return false;
        }

        public DMEnemyCondition ResolveCondition(float healthFraction)
        {
            if (healthFraction < criticalBelow)
                return DMEnemyCondition.Critical;
            if (healthFraction < severelyInjuredBelow)
                return DMEnemyCondition.SeverelyInjured;
            if (healthFraction < injuredBelow)
                return DMEnemyCondition.Injured;
            return DMEnemyCondition.Healthy;
        }

        /// <summary>2026-10-24a: worsening applies at once; improving needs the fraction to clear the threshold by conditionRecoverHysteresis.</summary>
        public DMEnemyCondition ResolveCondition(float healthFraction, DMEnemyCondition previous)
        {
            DMEnemyCondition c = ResolveCondition(healthFraction);
            if ((int)c >= (int)previous || conditionRecoverHysteresis <= 0f)
                return c;
            DMEnemyCondition r = ResolveCondition(Mathf.Max(0f, healthFraction - conditionRecoverHysteresis));
            return (int)r < (int)previous ? r : previous;
        }

        public static float Pick(Vector4 perCondition, DMEnemyCondition condition)
        {
            switch (condition)
            {
                case DMEnemyCondition.Injured: return perCondition.y;
                case DMEnemyCondition.SeverelyInjured: return perCondition.z;
                case DMEnemyCondition.Critical: return perCondition.w;
                default: return perCondition.x;
            }
        }
    
        [Header("Dodge strike (occasional dodge that flows into a quick melee swing; HumanM@Dodge01 clip)")]
        [Tooltip("Master switch. Melee engagers (the enemy holding the attack token) only; the dodge is a short side weave at the current distance, then the normal attack runs.")]
        public bool enableDodgeStrike = true;
        [Tooltip("Chance per roll that a ready engager opens its swing with a dodge strike (scaled per enemy type by EnemyDefinition.dodgeStrikeChanceMultiplier).")]
        [Range(0f, 1f)] public float dodgeStrikeChance = 0.15f;
        [Tooltip("Seconds between rolls while the attack is ready (so the chance is per opportunity, not per frame).")]
        [Range(0.2f, 5f)] public float dodgeStrikeRollInterval = 1.5f;
        [Tooltip("Minimum seconds between two dodge strikes of the same enemy.")]
        [Range(0f, 30f)] public float dodgeStrikeCooldown = 6f;
        [Tooltip("Only starts when the target is inside this multiple of the enemy's attack range (keeps the D1 melee band; the weave moves around the target at constant distance).")]
        [Range(0.3f, 2f)] public float dodgeStrikeMaxRangeFactor = 1.1f;
        [Tooltip("Sideways weave length in metres, taken around the target at constant distance (min, max).")]
        public Vector2 dodgeStrikeSideDistance = new Vector2(0.6f, 1.0f);
        [Tooltip("Length in seconds the HumanM@Dodge01 clip (1.07 s natural) is played at before the swing starts; smaller = snappier.")]
        [Range(0.4f, 1.5f)] public float dodgeStrikeSeconds = 0.8f;
        [Tooltip("The dodge hands over to the normal attack when the clip reaches this normalized time (the clip is blended out to locomotion).")]
        [Range(0.3f, 1f)] public float dodgeStrikeAttackAtNormalized = 0.6f;
        [Tooltip("The side weave reaches its full length at this normalized time of the clip (ease-out ends with the weave, before the hand-over).")]
        [Range(0.2f, 1f)] public float dodgeStrikeMoveEndNormalized = 0.5f;
        [Range(0f, 0.4f)] public float dodgeStrikeBlendIn = 0.08f;
        [Range(0f, 0.4f)] public float dodgeStrikeBlendOut = 0.12f;
        [Tooltip("Turn rate (deg/s) used to keep facing the target during the weave.")]
        [Range(90f, 1080f)] public float dodgeStrikeTurnRate = 540f;
}
}

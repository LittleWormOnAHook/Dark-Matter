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
        [Range(0f, 2f)] public float postSwingLockSeconds = 0.6f;
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
        [Tooltip("Turn rate (deg/s) while chasing / defending / searching with a combat target, i.e. outside the Attack state, so an enemy swings round to a player who moves behind it (180 deg in about 0.7 s). Only used while the engagement director is enabled; the legacy turnSpeed applies otherwise.")]
        [Range(30f, 720f)] public float nonAttackTurnRate = 260f;
        [Tooltip("Light swings track the target at this rate until trackUntil (normalized), then lock.")]
        [Range(0f, 720f)] public float lightTrackRate = 120f;
        [Range(0f, 1f)] public float lightTrackUntil = 0.30f;
        [Range(0f, 720f)] public float chargedTrackRate = 60f;
        [Range(0f, 1f)] public float chargedTrackUntil = 0.28f;
        [Tooltip("Extra wind-up when the Engager attacks from outside the camera view (fairness).")]
        [Range(0f, 2f)] public float offscreenAttackExtraWindup = 0.25f;

        [Header("Cadence and responsiveness (Slice 0b)")]
        [Tooltip("Extra wait (s) after a melee swing ends before the next one, random between Min and Max. Replaces the hard-coded EnemyCombat prefab values (were 0.6 - 1.8).")]
        [Range(0f, 3f)] public float meleeRecoveryPauseMin = 0.25f;
        [Range(0f, 3f)] public float meleeRecoveryPauseMax = 0.9f;
        [Tooltip("Chance a chase stamina roll stops the enemy briefly (was 0.38 on the prefab).")]
        [Range(0f, 1f)] public float chaseStaminaPauseChance = 0.15f;
        [Tooltip("Chase pause length range in seconds (was 0.5 - 0.9 on the prefab).")]
        [Range(0f, 2f)] public float chaseStaminaPauseMin = 0.2f;
        [Range(0f, 2f)] public float chaseStaminaPauseMax = 0.4f;
        [Tooltip("The chase stamina pause only happens while the target is farther than this (m). 0 = allow at any range.")]
        [Range(0f, 20f)] public float chaseStaminaPauseMinTargetDistance = 6f;
        [Tooltip("Chance a light attack continues into a 2nd swing (was 0.6 on the combo driver prefab).")]
        [Range(0f, 1f)] public float comboSecondHitChance = 0.7f;
        [Tooltip("Chance a two-swing chain continues into a 3rd swing (was 0.45).")]
        [Range(0f, 1f)] public float comboThirdHitChance = 0.55f;
        [Tooltip("Upper clamp (deg/s) for the legacy turnSpeed -> degrees conversion on enemies (was hard-coded 220). 220 = old behaviour.")]
        [Range(220f, 720f)] public float turnRateClampMax = 360f;
        [Tooltip("Lowest fraction of move speed while the enemy is turned away from its move direction (was hard-coded 0.18). 1 = never slows to turn.")]
        [Range(0.1f, 1f)] public float facingMoveScaleFloor = 0.45f;
        [Tooltip("Animator speed multiplier during enemy LIGHT swings only (never the charged wind-up). 1 = off, 1.08-1.15 = snappier.")]
        [Range(1f, 1.3f)] public float lightSwingSpeedMultiplier = 1.10f;

        [Header("Enemy-vs-enemy crowding (applies to every enemy; prefab values are fallback only)")]
        [Tooltip("On = enemyAvoidanceRadius/Strength (and minCombatSeparation when > 0) below replace each enemy prefab's own values. Off = prefab values. A definition engagementProfileOverride still wins.")]
        public bool applyCrowdingToAllEnemies = true;
        [Tooltip("Enemies inside this radius (m) of another enemy get pushed apart while chasing / attacking. Prefab default was 1.45.")]
        [Range(0.5f, 4f)] public float enemyAvoidanceRadius = 1.8f;
        [Tooltip("Push strength (m/s) of that separation. Prefab default was 0.85.")]
        [Range(0f, 4f)] public float enemyAvoidanceStrength = 1.6f;
        [Tooltip("Push (m/s) applied to enemies that are waiting (Hold / Holder) or standing between swings when another enemy overlaps them. Moving enemies keep using enemyAvoidanceStrength. 0 = waiting enemies are never pushed.")]
        [Range(0f, 4f)] public float waitingCrowdingStrength = 1.2f;
        [Tooltip("The push grows with overlap depth: strength x (1 + gain x depth), depth 0 = at the radius edge, 1 = fully overlapping. 0 = fixed push like before; 1.5 = up to 2.5x when fully overlapping.")]
        [Range(0f, 4f)] public float crowdingDepthGain = 1.5f;
        [Tooltip("Closest an enemy tries to stand to its combat target (m). 0 = keep each prefab's own minCombatSeparation (0.95 on SwordMaster).")]
        [Range(0f, 3f)] public float minCombatSeparation = 0f;

        [Tooltip("Per-enemy random variety: each enemy instance rolls a fixed -1..1 value at spawn; its back-off distance (min combat separation / ranged retreat) and crowding radius are scaled by 1 + spread * roll. 0.2 = +/-20%. 0 = every enemy identical. Per-type multiplier: EnemyDefinition.backoffDistanceMultiplier.")]
        [Range(0f, 0.5f)] public float crowdingVariationSpread = 0.2f;

        [Header("Engager cooldown movement (Slice 3 / C1: strafe + backstep between swings)")]
        [Tooltip("On = the engager (the one enemy allowed to swing) no longer stands still while its attack is on cooldown: the brain may roll a short slow strafe or a small backstep. Stays inside the melee band (see Max Distance Factor); never delays a swing; Phase 3 one-attacker spacing is untouched. Off = old standing-still behaviour.")]
        public bool enableEngagerCooldownMove = true;
        [Tooltip("Chance per roll (brain decision) that a cooldown move starts. Multiplied by the brain personality sidestep weight.")]
        [Range(0f, 1f)] public float engagerMoveChance = 0.7f;
        [Tooltip("Seconds between brain rolls while the engager is on attack cooldown.")]
        public Vector2 engagerMoveRollInterval = new Vector2(0.35f, 0.9f);
        [Tooltip("Seconds between the end of one cooldown move and the start of the next.")]
        public Vector2 engagerMoveCooldown = new Vector2(0.8f, 1.8f);
        [Tooltip("A move only starts when the attack still needs at least this many seconds to become ready.")]
        [Range(0.1f, 2f)] public float engagerMoveMinCooldown = 0.3f;
        [Tooltip("A running move ends when the attack is ready within this many seconds, so the engager faces and swings on time.")]
        [Range(0f, 1f)] public float engagerMoveStopBeforeAttack = 0.15f;
        [Tooltip("Slow strafe: speed as a fraction of walk speed, duration (s) and the maximum arc (degrees) around the target per move.")]
        [Range(0.2f, 1.6f)] public float engagerStrafeSpeedFactor = 0.95f;
        public Vector2 engagerStrafeSeconds = new Vector2(0.3f, 0.6f);
        [Range(5f, 60f)] public float engagerStrafeMaxArc = 24f;
        [Tooltip("Anti-orbit: running total of strafed degrees (left minus right) is kept inside +/- this value; beyond it the next strafe flips side. The total halves at every move start and resets when the token is awarded.")]
        [Range(10f, 120f)] public float engagerStrafeMaxNetArc = 45f;
        [Tooltip("Chance the next strafe goes the opposite way from the previous one.")]
        [Range(0f, 1f)] public float engagerStrafeFlipChance = 0.7f;
        [Tooltip("Cooldown moves only run while the target is within effective attack range x this (the melee band).")]
        [Range(1f, 2f)] public float engagerStrafeMaxDistanceFactor = 1.35f;
        [Tooltip("Chance a cooldown move is a small backstep instead of a strafe. Never backs out past the creep-start distance.")]
        [Range(0f, 1f)] public float engagerBackstepChance = 0.3f;
        [Tooltip("Backstep distance (m) and speed as a fraction of walk speed.")]
        public Vector2 engagerBackstepDistance = new Vector2(0.35f, 0.8f);
        [Range(0.3f, 2f)] public float engagerBackstepSpeedFactor = 1.3f;
        [Tooltip("How much of the per-enemy crowding variety (crowdingVariationSpread roll x definition backoffDistanceMultiplier) applies to cooldown move speed / distance / arc and to the post-hit backstep. 0 = every enemy identical, 1 = full variety.")]
        [Range(0f, 2f)] public float moveVariationScale = 1f;
        [Tooltip("Quick ease-in / ease-out time (s) of cooldown moves and holder sidesteps: the step starts at ~30% speed and reaches full speed in this time, and slows over the last quarter of the distance. 0 = constant speed.")]
        [Range(0f, 0.3f)] public float moveEaseSeconds = 0.08f;

        [Header("Step animation (quick steps: strafe / backstep / holder sidestep / hit backstep)")]
        [Tooltip("Leg-cycle exaggeration. While an enemy quick-steps, the locomotion blend (walk/strafe clip) is fed this x the real step speed, so the legs cycle faster than the root moves and the feet read as quick steps instead of a glide. 1 = feet matched exactly to ground speed.")]
        [Range(1f, 2.5f)] public float stepAnimSpeedScale = 1.35f;
        [Tooltip("When the step is faster than the fastest strafe / run clip, the animator playback speed is raised up to this multiplier (only while it is exactly 1 = no swing / hit stop / windup speed change running). 1 = never change playback speed.")]
        [Range(1f, 2f)] public float stepAnimMaxPlaybackBoost = 1.3f;
        [Tooltip("Smoothing time (s) of the measured root velocity that drives the locomotion clip during quick steps (normal 0.08 lags a 0.3-0.6 s step).")]
        [Range(0.005f, 0.1f)] public float stepVelocitySmoothing = 0.03f;
        [Tooltip("Blend-out (s) of the leg animation when a quick step ends: the locomotion animator inputs are pulled to 0 in this time (instead of Invector's slow input damping) so the legs stop with the body. 0 = snap to idle at the step end.")]
        [Range(0f, 0.3f)] public float stepAnimBlendOut = 0.06f;

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
        [Tooltip("Holder sidestep speed (m/s). Was holderWalkSpeed 1.4 (a slow glide); now its own value. Scaled per enemy by moveVariationScale.")]
        [Range(0.8f, 8f)] public float sidestepSpeed = 3.4f;
        [Tooltip("Seconds added to distance / speed for the sidestep action length (was a fixed 0.6 s pad). Keep it short so the step stays snappy.")]
        [Range(0f, 1f)] public float sidestepDurationPad = 0.2f;
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
    
        [Header("Swing start distance (first swing starts inside the weapon's real reach)")]
        [Tooltip("Master switch. The melee swing only starts when the target is inside the equipped weapon's real reach (instead of the generic attack range); the engager closes in first. Off = old behaviour (swing at the generic range).")]
        public bool swingStartUseWeaponReach = true;
        [Tooltip("Metres from the enemy root to the weapon grip when the arm is extended in a swing. Weapon tip reach = this + the weapon's hitbox length. Calibrated on the Blade of Chaos swing animations.")]
        [Range(0f, 2f)] public float weaponReachArm = 0.9f;
        [Tooltip("Same as Weapon Reach Arm but for Androids / Robots (non-Humanoid body types): their swing clips do not extend the arm as far, measured tip about 1.6-1.7 m with a 1.08 m sword (shakefix, was 0.9 for everyone).")]
        [Range(0f, 2f)] public float weaponReachArmMech = 0.55f;
        [Tooltip("Arm extension for Robots / machines (their own swing reach). Tip reach = this + weapon blade length.")]
        [Range(0f, 2f)] public float weaponReachArmRobot = 0.55f;
        [Tooltip("Crowd / hard-separation pushes write the leg locomotion blend un-damped for 0.25 s so the legs cycle with the push instead of lagging behind it (no sliding).")]
        public bool pushSnapsLegs = true;
        [Tooltip("Write the leg blend un-damped whenever the body moves faster than the legs show (step / chase / hold moves). Fixes idle-leg sliding.")]
        public bool snapLegsToMovement = true;
        [Tooltip("Tip reach (m from the root) of an unarmed enemy (fists / no melee weapon hitbox).")]
        [Range(0.3f, 2.5f)] public float unarmedTipReach = 1.1f;
        [Tooltip("The swing starts when the target is closer than tip reach minus this margin (so the hit window lands on the body, not on its edge). Small tolerance: lower = starts later / closer.")]
        [Range(-0.5f, 1f)] public float swingStartInnerMargin = 0.25f;
        [Tooltip("Visual blade cut into the target body radius at swing start (0.2 = 20%; Anthony's window 10-30%). Gate = arm + ItemData.meleeBladeLength + radius*(1-fraction).")]
        [Range(0f, 1f)] public float swingCutDepthFraction = 0.2f;
        [Tooltip("Apply the weapon-reach swing gate to every melee attacker, not only the token engager.")]
        public bool swingGateAllAttackers = true;
        [Tooltip("A target moving away is compensated by this many seconds of its outward speed (the swing needs ~this long to connect), so a kiting player is not swung at from the edge.")]
        [Range(0f, 0.6f)] public float swingStartLeadSeconds = 0.2f;
        [Tooltip("An engager whose attack becomes ready within this many seconds closes in (creeps) until it is inside the swing start distance.")]
        [Range(0f, 1.5f)] public float swingCloseInReadySeconds = 0.5f;

        [Header("Token fairness (random but starvation-free rotation, focus bias, group pace)")]
        [Tooltip("Master switch. On: the token is awarded by a weighted random pick among eligible enemies (softmax of the focus score), a starvation bonus favours enemies that waited longest / got fewer turns, and bigger mobs attack at a slower pace. Off = old behaviour (highest score always wins).")]
        public bool fairnessEnabled = true;
        [Tooltip("Randomness of the pick, in focus-score units. Small = the best score nearly always wins; large = almost uniform. The enemy the player targets / hit gets its focus-score bonus (weightPlayerTargeting / weightPlayerHit), so it is picked more often but the others still get turns.")]
        [Range(0.3f, 10f)] public float fairnessTemperature = 2.5f;
        [Tooltip("Starvation: focus-score bonus per second since this enemy last held the token (or joined the ring).")]
        [Range(0f, 3f)] public float starvationBonusPerSecond = 0.6f;
        [Tooltip("Cap of the time-based starvation bonus (focus-score units).")]
        [Range(0f, 20f)] public float starvationBonusMax = 6f;
        [Tooltip("Starvation: focus-score bonus for every token this enemy has had fewer than the ring's most-served enemy.")]
        [Range(0f, 5f)] public float starvationBonusPerMissedTurn = 1.5f;
        [Tooltip("Group pace: from this many enemies in the ring the attack pace slows (longer hand-off grace and recovery pause).")]
        [Range(2, 12)] public int groupPaceStartCount = 3;
        [Tooltip("Group pace: extra gap per enemy at or above the start count (0.25 = +25% gap per extra enemy).")]
        [Range(0f, 1f)] public float groupPaceExtraPerEnemy = 0.25f;
        [Tooltip("Group pace: cap of the gap multiplier (1 = no slowing). Per enemy type: EnemyDefinition.attackPaceMultiplier.")]
        [Range(1f, 4f)] public float groupPaceMaxMultiplier = 2f;
        [Tooltip("Extra focus score for an enemy that has never had an attack turn yet (clipfix: nobody in a big mob waits for the first swing).")]
        [Range(0f, 20f)] public float fairnessFirstTurnBonus = 8f;
        [Tooltip("Always on in Testing attack mode. On here = also in Gameplay: an enemy that never held the token goes before anyone's second turn.")]
        public bool fairnessFirstTurnGuaranteed = false; // testing mode (DM_CombatDirectorProfile.attackMode) forces it on
        [Tooltip("At or above this many enemies in the ring a token holder keeps the token for only ONE attack sequence before handing off.")]
        [Range(2, 12)] public int fairnessLargeMobCount = 4;

        [Header("Real weapon reach (clipfix): the generic attack range is capped to the equipped weapon's real reach")]
        [Tooltip("On = melee attack range = min(prefab attackRange, weapon tip reach + slack). Unarmed enemies (Humadroid, Robot fists) no longer swing from the generic 2.2 m.")]
        public bool capAttackRangeToWeaponReach = true;
        [Range(0f, 1f)] public float attackRangeReachSlack = 0.1f;

        [Header("Hard enemy separation (clipfix): enemies never merge into the same spot")]
        [Tooltip("Hard minimum distance between any two enemies (m, root to root). Overlap is pushed apart every frame (not during rolls / staggers / swings). 0 = off.")]
        [Range(0f, 2f)] public float hardEnemySeparation = 0.95f;
        [Tooltip("Max speed (m/s) of the hard separation push. The push never goes below ~0.9 m/s while active so the legs animate.")]
        [Range(0.5f, 6f)] public float hardSeparationMaxSpeed = 3.5f;
}
}

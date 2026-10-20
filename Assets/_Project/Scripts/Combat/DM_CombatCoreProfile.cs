using UnityEngine;

namespace Project.Combat
{
    public enum PlayerRigidbodyInterpolationMode
    {
        Keep = 0,
        None = 1,
        Interpolate = 2,
        Extrapolate = 3
    }

    [CreateAssetMenu(menuName = "Dark Matter/Combat/Combat Core Profile", fileName = "DM_CombatCoreProfile")]
    public sealed class DM_CombatCoreProfile : ScriptableObject
    {
        public const string ResourcesPath = "Combat/DM_CombatCoreProfile";

        private static DM_CombatCoreProfile live;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLiveCache() => live = null;

        public static DM_CombatCoreProfile Live
        {
            get
            {
                if (live == null)
                    live = Resources.Load<DM_CombatCoreProfile>(ResourcesPath);
                return live;
            }
        }

        public static DM_CombatCoreProfile Resolve(DM_CombatCoreProfile assigned)
        {
            DM_CombatCoreProfile canonical = Live;
            return canonical != null ? canonical : assigned;
        }

        [Header("Poise")]
        public float trainingDummyPoiseMax = 80f;
        [Range(0f, 2f)] public float poiseDamageFromHealthMultiplier = 0.35f;
        [Tooltip("Caps poise lost on one hit as a fraction of max. 0.4 ≈ never one-shot; 1 = allow a single swing to break.")]
        [Range(0.1f, 1f)] public float maxPoiseDamageFractionPerHit = 0.4f;
        public float poiseRegenPerSecond = 12f;
        public float poiseRegenDelayAfterHit = 2f;

        [Header("Humanoid poise")]
        public float humanoidPoiseMax = 100f;
        [Tooltip("Poise-break hitstun on humanoids (~0.32s baseline). Block/parry guard-break uses longer lockouts below.")]
        [Range(0.1f, 1f)] public float humanoidPoiseStaggerSeconds = 0.32f;

        [Header("Block / parry guard-break (attacker stagger)")]
        [Tooltip("Attack lockout after a normal block. Tunable 0–3s (default 0.4). Typical Souls / God of War guard recoil is ~0.35–0.45s — shorter than a parry.")]
        [Range(0f, 3f)] public float blockStaggerSeconds = 0.4f;
        [Tooltip("Attack lockout after a parry (block inside parryWindowSeconds). Riposte opening on the attacker — tunable 0–10s (default 5). Shorter than block at low values; Sekiro/Souls-style at ~0.7–0.85s.")]
        [Range(0f, 10f)] public float parryStaggerSeconds = 5f;
        [Tooltip("Flinch shove on a normal block. Kept under 1 so a guard tap is a mild stagger, not a launch.")]
        [Range(0.5f, 3f)] public float blockParryStaggerImpulseScale = 0.85f;
        [Tooltip("Multiplies the block shove on a parry. 1.75 × 0.85 ≈ 1.5, a clearly harder impact than a block.")]
        [Range(1f, 2f)] public float parryStaggerImpulseBonus = 1.75f;
        [Tooltip("Enemy animator freeze on a normal block. Leave at 0 — a block does not hitstop the enemy or the player. The mild flinch and the short guard pose carry the impact.")]
        [Range(0f, 0.25f)] public float blockStaggerHitstopSeconds = 0f;
        [Tooltip("Enemy animator freeze on a parry only. About 0.10–0.15s (default 0.12s), the short pose-hold used by Souls, Sekiro, and God of War on a real parry. Local animator speed, not Time.timeScale. Does not freeze the player.")]
        [Range(0f, 0.3f)] public float parryStaggerHitstopSeconds = 0.12f;

        [Header("Dummy spring / stagger")]
        [Range(1f, 80f)] public float trainingDummyPositionSpring = 20f;
        [Range(0f, 30f)] public float trainingDummyPositionDamping = 7f;
        [Range(1f, 80f)] public float trainingDummyRotationSpring = 24f;
        [Range(0f, 30f)] public float trainingDummyRotationDamping = 8f;
        [Range(0f, 5f)] public float trainingDummyHitImpulse = 0.85f;
        [Range(0f, 180f)] public float trainingDummyHitTorque = 32f;
        [Range(0.05f, 2f)] public float trainingDummyMaxPositionOffset = 0.45f;
        [Range(1f, 90f)] public float trainingDummyMaxRotationOffset = 24f;
        [Range(0.25f, 6f)] public float trainingDummyPoiseStaggerImpulseScale = 1f;

        [Header("I-Frames")]
        [Range(0.05f, 1f)] public float dodgeIFrameSeconds = 0.25f;
        [Tooltip("Max charged-attack hold (s), player and enemies, counted from the press / charged start. When reached the attack auto-releases and follows through. 0 = no cap.")]
        [Range(0f, 5f)] public float chargedMaxHoldSeconds = 1f;

        [Header("Player melee stamina (spent when the swing starts, hit or miss)")]
        [Tooltip("Stamina per light swing.")]
        [Range(0f, 100f)] public float meleeLightStaminaCost = 8f;
        [Tooltip("Stamina per heavy swing (interact-hold combo attack).")]
        [Range(0f, 100f)] public float meleeHeavyStaminaCost = 14f;
        [Tooltip("Stamina per charged (hold) attack release.")]
        [Range(0f, 100f)] public float meleeChargedStaminaCost = 22f;
        [Tooltip("Also spend the weapon's own ItemData meleeStaminaCost on every landed hit (old behaviour; double-charges with the costs above).")]
        public bool meleeStaminaAlsoOnHit = false;

        [Header("Player block stamina")]
        [Tooltip("Holding block drains stamina per second = base x growth ^ seconds held (capped). Runs out = guard breaks.")]
        [Range(0f, 30f)] public float playerBlockHoldCostBase = 2f;
        [Range(1f, 4f)] public float playerBlockHoldCostGrowth = 1.6f;
        [Range(0f, 100f)] public float playerBlockHoldCostCap = 25f;
        [Tooltip("Flat stamina per blocked hit.")]
        [Range(0f, 100f)] public float playerBlockedHitStaminaCost = 10f;
        [Tooltip("Stamina lost when the player is staggered (more than a simple block).")]
        [Range(0f, 100f)] public float playerStaggerStaminaCost = 18f;
        [Tooltip("Seconds block is locked after the guard breaks from empty stamina.")]
        [Range(0f, 5f)] public float playerGuardBreakLockSeconds = 1.2f;
        [Tooltip("Player roll guard: the Roll clip cannot be cut (snap back to standing) or re-triggered before playerRollMinNormalized.")]
        public bool playerRollGuard = true;
        [Range(0f, 1f)] public float playerRollMinNormalized = 0.7f;
        [Range(0.05f, 1f)] public float dashIFrameSeconds = 0.25f;

        [Header("Player holsters (2026-10-12)")]
        [Tooltip("Every weapon in the 4 hotbar weapon slots (mining tool, pistol, rifles, melee) shows holstered on the body when it is not the drawn one. Off = only the equipped weapon shows (old rule).")]
        public bool holsterAllHotbarWeapons = true;
        [Tooltip("Melee: only one holstered melee per body location (Spine / Left leg / Right leg). Location comes from where the Holstered_ slot is placed in the rig.")]
        public bool holsterOneMeleePerLocation = true;
        [Tooltip("Max melee weapons visible at once, the drawn one included (1 spine + 1 left leg + 1 right leg + 1 in hand = 4).")]
        [Range(1, 4)] public int holsterMaxMeleeVisible = 4;
        [Tooltip("Also allow only one ranged weapon per location (off = all hotbar ranged weapons show).")]
        public bool holsterOneRangedPerLocation = false;

        [Header("Player stagger (enemy hits on the player)")]
        [Tooltip("On = these chances drive the player hit reaction (the player controller is immortal, so Invector never played it; the reaction is now triggered directly). Off = PioneerAnimationPlanSettings small-hit chances.")]
        public bool playerStaggerUseProfile = true;
        [Tooltip("Stagger chance at full health / normal hit.")]
        [Range(0f, 1f)] public float playerStaggerChanceMin = 0.2f;
        [Tooltip("Stagger chance at low health or on a crit.")]
        [Range(0f, 1f)] public float playerStaggerChanceMax = 0.4f;
        [Tooltip("After a stagger, no new stagger for this long (no stun-lock in group fights).")]
        [Range(0f, 6f)] public float playerStaggerImmunitySeconds = 1.5f;
        [Tooltip("Allow a stagger while the player holds a charged attack (interrupts the charge).")]
        public bool playerStaggerInterruptsCharge = true;

        [Header("Player hit reactions (DM, 2026-10-19 d; replaces stock Invector reaction for enemy hits)")]
        [Tooltip("On = DMPlayerHitReaction (lean / light / heavy / rare knockdown) replaces the stock Invector TriggerReaction. The stagger chances above still gate light/heavy.")]
        public bool enablePlayerHitReactions = true;
        [Range(0f, 40f)] public float playerLeanDegreesMin = 10f;
        [Range(0f, 40f)] public float playerLeanDegreesMax = 14f;
        [Range(0.01f, 0.3f)] public float playerLeanAttackSeconds = 0.06f;
        [Range(0.05f, 1f)] public float playerLeanSettleSeconds = 0.3f;
        [Tooltip("Multiplier on the shared headWhipMultiplier (player camera rides the head less).")]
        [Range(0f, 1.5f)] public float playerHeadWhipScale = 0.7f;
        [Range(0f, 45f)] public float playerLeanJitterDegrees = 18f;
        [Range(0f, 0.5f)] public float playerIntensityJitter = 0.15f;
        [Range(0.5f, 1.5f)] public float playerReactionIntensityMax = 0.85f;
        [Range(0.5f, 1.5f)] public float playerClipSpeedMin = 0.95f;
        [Range(0.5f, 1.5f)] public float playerClipSpeedMax = 1.1f;
        [Range(0.1f, 1.2f)] public float playerClipWeightMin = 0.7f;
        [Range(0.1f, 1.2f)] public float playerClipWeightMax = 0.9f;
        [Tooltip("A hit of at least this fraction of max HP counts as heavy (also Invector big reaction / crit).")]
        [Range(0.05f, 0.6f)] public float playerHeavyDamageFraction = 0.18f;
        [Range(0f, 1f)] public float playerPushMeters = 0.25f;
        [Range(0.05f, 1f)] public float playerPushSeconds = 0.25f;
        [Tooltip("Chance a heavy push becomes a longer stumble (x1.6).")]
        [Range(0f, 1f)] public float playerStumbleChance = 0.35f;
        [Range(0f, 1f)] public float playerLightLockSeconds = 0.2f;
        [Range(0f, 1.5f)] public float playerHeavyLockSeconds = 0.35f;
        [Range(0f, 1f)] public float playerHeavyMoveScale = 0.4f;
        [Tooltip("Knockdown chance on a HEAVY hit only.")]
        [Range(0f, 0.5f)] public float playerKnockdownChance = 0.05f;
        [Tooltip("Added on crit or HP < 30%.")]
        [Range(0f, 0.5f)] public float playerKnockdownBonusChance = 0.04f;
        [Range(0f, 0.5f)] public float playerKnockdownChanceCap = 0.12f;
        [Range(0f, 120f)] public float playerKnockdownCooldownSeconds = 20f;
        [Range(0.1f, 3f)] public float playerDownHoldSecondsMin = 0.6f;
        [Range(0.1f, 3f)] public float playerDownHoldSecondsMax = 1f;
        [Tooltip("Damage multiplier while knocked down.")]
        [Range(0f, 1f)] public float playerDownedDamageScale = 0.5f;
        [Tooltip("Invulnerable for this long from get-up start (and after a roll-cancel).")]
        [Range(0f, 2f)] public float playerGetUpIFrameSeconds = 0.6f;
        public bool playerRollCancel = true;
        [Range(0f, 2f)] public float playerRollCancelAfterSeconds = 0.4f;
        [Tooltip("God mode / audit: which tiers still play (default lean + light; no heavy push, no knockdown).")]
        public DMPlayerReactionsUnderGodMode playerReactionsUnderGodMode = DMPlayerReactionsUnderGodMode.LeanAndLight;
        [Range(0f, 1f)] public float playerAdsLeanScale = 0.5f;
        public bool logPlayerHitReactions = false;
        [Tooltip("God mode / audit: enemy hits still play the player body-hit sound (health does not drop, so SurvivalStats stays silent).")]
        public bool playerHitSoundUnderGodMode = true;

        [Header("Player feel guards (shake while blocking / charging)")]
        [Tooltip("Min seconds between guard-impact recoil poses while holding block. Hits inside the window are still absorbed, just no new pose (group fights re-triggered it every hit = shaking).")]
        [Range(0f, 2f)] public float guardImpactMinIntervalSeconds = 0.45f;
        [Tooltip("Min seconds between block camera shakes.")]
        [Range(0f, 2f)] public float blockShakeMinIntervalSeconds = 0.35f;
        [Tooltip("Charged-attack hold: the charge clip is re-pinned only after it drifts this far (normalized) past the hold frame. 0 = old behaviour (re-pinned every frame = jitter).")]
        [Range(0f, 0.15f)] public float chargeHoldPinTolerance = 0.05f;

        [Header("Melee facing target stickiness (block / attack auto-face)")]
        [Tooltip("Once auto-face picks an enemy it keeps it at least this long (unless it dies / leaves range). Stops the body flipping between enemies of a group between swings.")]
        [Range(0f, 5f)] public float meleeFacingTargetLockSeconds = 1.5f;
        [Tooltip("After the lock, switch only when another enemy is closer than this fraction of the current target's distance (0.7 = 30% closer).")]
        [Range(0.1f, 1f)] public float meleeFacingSwitchCloserFraction = 0.7f;

        [Header("Melee combo target lock (DMMeleeAttackFacing, 2026-10-19 d)")]
        [Tooltip("On = one target per combo, eased wind-up-only turn in FixedUpdate (single rotation owner). Off = legacy per-frame ApplyAttackFacing.")]
        public bool meleeComboTargetLock = true;
        [Range(1f, 10f)] public float meleeTargetAcquireDistance = 4.5f;
        [Range(1f, 15f)] public float meleeTargetReleaseDistance = 6.5f;
        [Tooltip("Half-angle of the intent cone around the stick (else camera) direction for acquiring.")]
        [Range(10f, 180f)] public float meleeTargetIntentConeHalfAngle = 60f;
        [Tooltip("Score = distance/acquire + weight * angle/cone.")]
        [Range(0f, 3f)] public float meleeTargetAngleWeight = 1f;
        [Range(0f, 2f)] public float meleeComboLockGraceSeconds = 0.3f;
        [Tooltip("Stick must point more than this away from the target to retarget.")]
        [Range(10f, 180f)] public float meleeRetargetStickAngle = 45f;
        [Range(0f, 2f)] public float meleeRetargetMinSeconds = 0.4f;
        [Range(0.1f, 1f)] public float meleeRetargetStickMagnitude = 0.5f;
        [Tooltip("Max assist turn speed (deg/s), eased.")]
        [Range(30f, 1080f)] public float meleeAssistTurnSpeed = 360f;
        [Range(0.01f, 0.5f)] public float meleeAssistSmoothTime = 0.08f;
        [Range(0f, 180f)] public float meleeAssistMaxPerSwingDegrees = 70f;
        [Tooltip("Turn only during the first fraction of each swing (wind-up).")]
        [Range(0.05f, 1f)] public float meleeAssistWindupFraction = 0.35f;
        [Tooltip("Turn speed fades in over this error (no hard deadband edge).")]
        [Range(1f, 90f)] public float meleeAssistSoftFadeDegrees = 22f;
        public bool meleeTargetIgnoreDowned = true;
        [Tooltip("Legacy: light combo attack-move forces strafe on (caused feet pivot). Default off.")]
        public bool lightComboForcesStrafe = false;
        public bool logMeleeFacing = false;

        [Header("Melee block target lock (DMMeleeAttackFacing, 2026-10-19 e)")]
        [Tooltip("On = block auto-face uses the same sticky target / cone / eased FixedUpdate turn as attacks (acquire / release / cone / retarget settings above). Off = legacy per-frame nearest pick.")]
        public bool meleeBlockTargetLock = true;
        [Range(30f, 1080f)] public float meleeBlockAssistTurnSpeed = 300f;
        [Range(0.01f, 0.5f)] public float meleeBlockAssistSmoothTime = 0.1f;
        [Range(0f, 2f)] public float meleeBlockLockGraceSeconds = 0.25f;
        [Tooltip("No turn while the guard already faces the target within this angle (no micro-turning).")]
        [Range(0f, 20f)] public float meleeBlockFacingToleranceDegrees = 3f;

        [Header("Slow motion (DMSlowMotionDirector, 2026-10-19 e)")]
        [Tooltip("Charged attack: slow-mo when the hit window opens and an enemy is predicted to be hit (in reach + cone).")]
        public bool enableChargedSlowMo = true;
        [Range(0f, 1f)] public float chargedSlowMoChance = 0.1f;
        [Range(0f, 120f)] public float chargedSlowMoCooldownSeconds = 15f;
        [Tooltip("Total real-time length incl. ease in / out.")]
        [Range(2f, 4f)] public float chargedSlowMoSeconds = 3f;
        [Range(0.05f, 1f)] public float chargedSlowMoTimeScale = 0.3f;
        [Range(0f, 1f)] public float chargedSlowMoEaseInSeconds = 0.12f;
        [Range(0.05f, 2f)] public float chargedSlowMoEaseOutSeconds = 0.8f;
        [Tooltip("Predicted hit: nearest standing enemy within this distance...")]
        [Range(0.5f, 5f)] public float chargedSlowMoReach = 2.4f;
        [Tooltip("...and within this half-angle of the player's forward.")]
        [Range(5f, 90f)] public float chargedSlowMoConeHalfAngle = 35f;
        [Tooltip("Perfect parry: block press with an incoming enemy attack starts the before half; a confirmed parry holds the after half.")]
        public bool enableParrySlowMo = true;
        [Range(0f, 1f)] public float parrySlowMoChance = 1f;
        [Tooltip("Total real time, split evenly before / after the parry moment.")]
        [Range(1f, 3f)] public float parrySlowMoTotalSeconds = 2f;
        [Range(0.05f, 1f)] public float parrySlowMoTimeScale = 0.25f;
        [Range(0f, 1f)] public float parrySlowMoEaseInSeconds = 0.08f;
        [Range(0.05f, 2f)] public float parrySlowMoEaseOutSeconds = 0.35f;
        [Range(1f, 8f)] public float parrySlowMoThreatRadius = 3.5f;
        [Range(0f, 30f)] public float parrySlowMoCooldownSeconds = 3f;
        [Tooltip("Audit only: allow slow-mo while an audit session runs (off = audits never slow time).")]
        public bool slowMoAllowInAudit = false;
        public bool logSlowMo = false;

        [Header("Hitch monitor (lightweight, play mode)")]
        [Tooltip("Logs any frame longer than hitchThresholdMs with GC counts and the last combat event (blood spawn, hit, etc.). Off = zero cost.")]
        public bool logHitchesInPlay = false;
        [Range(30f, 1000f)] public float hitchThresholdMs = 120f;

        [Header("Player quick step (triple-tap A / D = sidestep, triple-tap S = backstep; pad: triple stick flick)")]
        [Tooltip("Master switch. Dash (B) and jetpack (Space double-tap) are separate and unchanged. No roll fallback.")]
        public bool quickStepEnabled = true;
        [Tooltip("All three taps must land within this many seconds.")]
        [Range(0.2f, 1.2f)] public float quickStepTapWindowSeconds = 0.5f;
        [Range(0.3f, 5f)] public float sidestepDistance = 1.8f;
        [Range(0.3f, 5f)] public float backstepDistance = 1.5f;
        [Tooltip("Seconds the step takes (eased).")]
        [Range(0.05f, 0.6f)] public float quickStepDurationSeconds = 0.2f;
        [Range(0f, 3f)] public float quickStepCooldownSeconds = 0.4f;
        [Tooltip("Gamepad: stick must pass this to count a flick, and drop below quickStepStickRelease to re-arm.")]
        [Range(0.3f, 1f)] public float quickStepStickFlick = 0.8f;
        [Range(0f, 0.6f)] public float quickStepStickRelease = 0.3f;
        [Tooltip("Stamina cost per step (0 = free).")]
        [Range(0f, 100f)] public float quickStepStaminaCost = 0f;

        [Header("Holstered weapon sway (player + enemies, runtime only)")]
        [Tooltip("Holstered_ weapons swing on a spring when walking / running / fighting. Rest pose = the hand-set placement; nothing is saved. 0 = off.")]
        [Range(0f, 3f)] public float holsterSwayAmount = 1f;
        [Tooltip("Pivot position along the weapon, measured from its lower end (0.66 = two thirds up).")]
        [Range(0.3f, 1f)] public float holsterSwayPivot = 0.66f;
        [Tooltip("Advanced spring settings (collapsed).")]
        public HolsterSwayAdvanced holsterSwayAdvanced = new HolsterSwayAdvanced();

        [System.Serializable]
        public class HolsterSwayAdvanced
        {
            [Range(5f, 400f)] public float stiffness = 60f;
            [Range(0f, 40f)] public float damping = 6f;
            [Tooltip("How strongly body acceleration swings the weapon.")]
            [Range(0f, 20f)] public float inertia = 4f;
            [Tooltip("Pull toward hanging straight down (0 = rest pose only).")]
            [Range(0f, 50f)] public float gravity = 6f;
            [Range(1f, 45f)] public float maxAngle = 14f;
        }

        [Header("Status (global rules)")]
        [Range(1, 10)] public int statusMaxStacks = 3;
        [Range(0f, 10f)] public float statusImmunityWindowSeconds = 2f;
        [Range(0.1f, 1f)] public float statusBossMultiplier = 0.5f;
        [Tooltip("World offset along the hit outward normal when parenting status DoT VFX (ION corroded orb, etc.).")]
        [Range(0f, 0.05f)] public float statusVfxNormalOffset = 0.004f;

        [Header("Melee hit detection")]
        [Tooltip("Scales Invector vHitBox trigger width (left/right thickness).")]
        [Range(1f, 2.5f)] public float meleeHitboxWidthScale = 2.1f;
        [Tooltip("Scales reach along the blade (box depth / length).")]
        [Range(1f, 2.5f)] public float meleeHitboxReachScale = 1.75f;
        [Tooltip("Half-angle (degrees) from body forward for melee block/parry. Invector default is 90 (180° frontal).")]
        [Range(45f, 180f)] public float meleeBlockDefenseHalfAngle = 130f;
        [Tooltip("Hold block (RMB): auto-face the nearest threat when their bearing exceeds this many degrees off forward.")]
        [Range(5f, 75f)] public float meleeBlockAutoFaceHalfAngle = 30f;
        [Tooltip("Max horizontal distance to snap facing while blocking.")]
        [Range(2f, 8f)] public float meleeBlockAutoFaceMaxDistance = 4.5f;
        [Tooltip("Yaw degrees per second while block-assist turning.")]
        [Range(180f, 900f)] public float meleeBlockAutoFaceTurnSpeed = 680f;
        [Tooltip("Light/strong swing and strong charge: auto-face nearest threat when bearing exceeds this half-angle (tighter than block).")]
        [Range(5f, 45f)] public float meleeAttackAutoFaceHalfAngle = 22f;
        [Tooltip("Extra reach scale for humanoid enemy melee hitboxes only.")]
        [Range(1f, 2.5f)] public float enemyMeleeHitboxWidthScale = 2.25f;
        [Range(1f, 2.5f)] public float enemyMeleeHitboxReachScale = 2.15f;
        [Tooltip("Multiplies EnemyCombat.attackRange for AI strike distance (prefab values are often short).")]
        [Range(1f, 1.5f)] public float enemyMeleeAttackRangeMultiplier = 1.26f;
        [Tooltip("When > 0, overrides each enemy prefab attackStandoffFraction for melee ring / creep stop (lower = stand closer). 0 = prefab only.")]
        [Range(0f, 0.95f)] public float enemyMeleeStandoffFraction = 0.56f;
        [Tooltip("Caps combat standoff at effective attack range × this (contact band inside weapon reach).")]
        [Range(0.75f, 1f)] public float enemyMeleeMaxOrbitFactor = 0.9f;
        [Tooltip("Per swing sequence: roll to play the attack anim but suppress outgoing melee damage (Invector hitboxes stay on for feedback).")]
        [Range(0f, 1f)] public float enemyIntentionalMeleeMissChance = 0.1f;
        [Tooltip("After an intentional miss, the engager brain steps in toward the target for this long (no NavMesh).")]
        [Range(0.15f, 1.5f)] public float enemyMeleeMissStepSeconds = 0.55f;
        [Tooltip("Miss recovery creep stops once planar distance is within effective attack range × this.")]
        [Range(0.5f, 0.95f)] public float enemyMeleeMissStepStopFactor = 0.72f;
        [Tooltip("Walk speed multiplier while miss-recovery step-in is active.")]
        [Range(0.5f, 1.2f)] public float enemyMeleeMissStepSpeedMultiplier = 0.95f;
        [Tooltip("Unused (legacy). Forgiveness is handled by scaled primary hitbox + per-swing target dedupe.")]
        [Range(0f, 45f)] public float meleeHitYawForgivenessDegrees = 0f;

        [Header("Light melee combo (Weak SwordAttack A→B→C)")]
        [Tooltip("Play-mode Animator.speed in WeakAttacks/SwordAttack A. 1 = clip default, 1.25 = 25% faster. Future knife/baton: add parallel A/B/C triplets or a weapon-family nested profile — keep named slots for now.")]
        [Range(0.75f, 2f)] public float lightComboAnimSpeedA = 1f;
        [Tooltip("WeakAttacks/SwordAttack B. 1 = normal, 1.25 = +25% faster.")]
        [Range(0.75f, 2f)] public float lightComboAnimSpeedB = 1f;
        [Tooltip("WeakAttacks/SwordAttack C. 1 = normal, 1.25 = +25% faster.")]
        [Range(0.75f, 2f)] public float lightComboAnimSpeedC = 1f;
        [Tooltip("Crimson-style attack move: camera-relative strafe during chained WeakAttacks/SwordAttack A→B→C only (not strong, charge, parry, or Hold-E upper-body combo).")]
        public bool enableLightComboAttackMove = true;
        [Tooltip("Multiplies locomotion speed while strafing during the weak A→B→C chain when attack move is on.")]
        [Range(0.35f, 1.2f)] public float lightComboStrafeSpeedMultiplier = 0.65f;
        [Tooltip("Minimum forward input (0–1) blended in during weak combo attack move when strafing or idle — small drift toward camera forward.")]
        [Range(0f, 0.45f)] public float lightComboForwardDriftMultiplier = 0.12f;
        [Tooltip("When off (default with attack move on), Invector LockMovement on weak A/B/C is bypassed so WASD strafe works. When on, weak combo plants like legacy Invector.")]
        public bool lockMovementDuringLightCombo = false;

        [Header("Light random pool (Weak SwordRandomAttack A→B→C)")]
        [Tooltip("Parallel random weak swings (not the A→B→C chain). 1 = clip default, 1.25 = 25% faster.")]
        [Range(0.75f, 2f)] public float lightRandomAnimSpeedA = 1f;
        [Range(0.75f, 2f)] public float lightRandomAnimSpeedB = 1f;
        [Range(0.75f, 2f)] public float lightRandomAnimSpeedC = 1f;

        [Header("Strong melee slots (A/C — B + charge below)")]
        [Tooltip("StrongAttacks/SwordAttack A (non-charge heavy). Charge + Strong B use strongMeleeAnimSpeedMultiplier.")]
        [Range(0.75f, 2f)] public float strongMeleeAnimSpeedA = 1f;
        [Tooltip("StrongAttacks/SwordAttack C. 1 = normal, 1.25 = +25% faster.")]
        [Range(0.75f, 2f)] public float strongMeleeAnimSpeedC = 1f;

        [Header("Strong melee (hold light attack, then release)")]
        [Tooltip("Seconds the light-attack button (left mouse / Attack) must be held before a release plays the strong sword swing. After this threshold the charge pose stays until release. There is no maximum hold and the swing does not fire by itself. A shorter press-and-release stays a light tap. Right mouse stays block.")]
        [Range(0.2f, 0.8f)] public float strongMeleeChargeSeconds = 0.4f;
        [Tooltip("Scales the normal melee damage roll on that charged swing. 1.8 is clearly above a light hit.")]
        [Range(1.2f, 3f)] public float strongMeleeDamageMultiplier = 1.8f;
        [Tooltip("Normalized clip time when charged release (Strong SwordAttack B) hitboxes turn on. Late in the swing.")]
        [Range(0.05f, 0.95f)] public float strongMeleeDamageStartNormalized = 0.72f;
        [Tooltip("Normalized clip time when charged release hitboxes turn off.")]
        [Range(0.1f, 1f)] public float strongMeleeDamageEndNormalized = 0.98f;
        [Tooltip("Play-mode speed for StrongAttacks/SwordCharge and charged Strong A/B/C release (1 = clip default, 1.25 = 25% faster, 0.75 = slowest allowed). Applied at runtime from profile.Live; Build And Apply only syncs controller clips.")]
        [Range(0.75f, 2f)] public float strongMeleeAnimSpeedMultiplier = 1.25f;
        [Tooltip("Relative weights when picking Strong A/B/C on charged release (normalized at runtime). Favor A for the default heavy.")]
        [Range(0f, 1f)] public float strongReleaseWeightA = 0.55f;
        [Range(0f, 1f)] public float strongReleaseWeightB = 0.22f;
        [Range(0f, 1f)] public float strongReleaseWeightC = 0.23f;
        [Tooltip("Hold E + light attack (InteractHoldCombo state). Default 1.75 matches design.")]
        [Range(0.75f, 2.5f)] public float interactHoldComboAnimSpeed = 1.75f;
        [Tooltip("Charged Strong B / AttackC ignores the defender's sword and other outgoing weapon volumes. Damage waits for a real body / dummy hit. Block and parry still register as guard.")]
        public bool chargedHitsIgnoreEnemyWeapons = true;

        [Header("Slice 1 feel pack: melee hit stop (A1)")]
        [Tooltip("On = a landed player melee hit briefly freezes the HIT enemy's animator (and the player's swing) for hitstopLightFrames / hitstopHeavyFrames below. Local animator speed dip only, never Time.timeScale. Other enemies keep moving.")]
        public bool enableMeleeHitstop = true;
        [Tooltip("Extra freeze frames (60 fps) on a critical hit, added to the light/heavy frames.")]
        [Range(0, 6)] public int hitstopCritBonusFrames = 2;
        [Tooltip("Animator speed during the freeze (enemy and, with Hitstop Freezes Attacker, the player swing). 0 = fully frozen. Was 0.05 (near-frozen) which made the player judder when root motion / the motor stopped and restarted; 0.3 is a soft hold (shakefix).")]
        [Range(0f, 0.5f)] public float hitstopHoldSpeed = 0.3f;
        [Tooltip("Rigidbody interpolation applied at runtime to the live player root (Player_v7 prefab untouched). The prefab ships with None: the motor moves in FixedUpdate (60 Hz) while rendering runs at the frame rate, which shows up as a small shake / judder. Interpolate smooths it. Keep = leave as the prefab has it.")]
        public PlayerRigidbodyInterpolationMode playerRigidbodyInterpolation = PlayerRigidbodyInterpolationMode.Interpolate;
        [Tooltip("Also freeze the player's own swing for the same time (the blade 'sticks'). Off = victim only.")]
        public bool hitstopFreezesAttacker = true;

        [Header("Slice 1 feel pack: light / heavy melee hit shake (A2)")]
        [Tooltip("Trauma on a normal player melee hit that damages an enemy. Very subtle by default. 0 = off.")]
        [Range(0f, 1f)] public float lightHitShakeAmplitude = 0.05f;
        [Range(0f, 0.4f)] public float lightHitShakeDurationSeconds = 0.04f;
        [Tooltip("Trauma on a heavy non-charged hit: a critical, a poise break or a killing blow. Charged hits keep chargedHitShake*.")]
        [Range(0f, 1f)] public float heavyHitShakeAmplitude = 0.20f;
        [Range(0f, 0.4f)] public float heavyHitShakeDurationSeconds = 0.08f;

        [Header("Slice 1 feel pack: damage number tiers (A3)")]
        [Tooltip("On = damage numbers on enemies use the tier colors/sizes below (normal / crit / heavy). Off = legacy red and yellow. Orange is a number colour only, not a telegraph.")]
        public bool useDamageNumberTiers = true;
        public Color damageNumberNormalColor = new Color(1f, 0.96f, 0.78f, 1f);
        public Color damageNumberCritColor = new Color(1f, 0.55f, 0.10f, 1f);
        public Color damageNumberHeavyColor = new Color(1f, 0.40f, 0.05f, 1f);
        [Range(0.6f, 2f)] public float damageNumberCritScale = 1.25f;
        [Range(0.6f, 2f)] public float damageNumberHeavyScale = 1.4f;

        [Header("Slice 2 hit reactions (B1-B4)")]
        [Tooltip("Master switch for the Slice 2 hit reactions: directional flinch / backstep (B1), hit-chain escalation (B2), heavy hit slide-back + waist bend (B3) and hit recovery before the enemy swings again (B4). Off = Slice 1 behaviour.")]
        public bool enableHitReactions = true;
        [Tooltip("B4: seconds the enemy cannot start a new swing after a poise break, a heavy-hit Big reaction or a chain stumble. Extends the attack lockout only; postSwingLockSeconds is not touched.")]
        [Range(0f, 4f)] public float hitRecoverySeconds = 1.6f;
        public bool hitRecoveryOnPoiseBreak = true;
        public bool hitRecoveryOnHeavyHit = true;
        public bool hitRecoveryOnChainStumble = true;

        [Header("Slice 2: directional flinch (B1)")]
        [Tooltip("On = a normal player melee hit twists the enemy's torso toward the swing direction and gives a tiny backstep (procedural, on top of the existing light flinch).")]
        public bool directionalFlinchEnabled = true;
        [Tooltip("Swing side of the player's light combo steps: +1 = left-to-right slash, -1 = right-to-left, 0 = overhead / thrust (no sideways twist).")]
        [Range(-1f, 1f)] public float swingSideLightA = 1f;
        [Range(-1f, 1f)] public float swingSideLightB = -1f;
        [Range(-1f, 1f)] public float swingSideLightC = 0f;
        [Tooltip("Swing side of charged / heavy swings.")]
        [Range(-1f, 1f)] public float swingSideCharged = 0f;
        [Tooltip("Torso twist (degrees) at full swing side.")]
        [Range(0f, 25f)] public float swingTwistDegrees = 10f;
        [Tooltip("Lean-back (degrees) and duration of the directional flinch on a plain hit.")]
        [Range(0f, 15f)] public float directionalFlinchDegrees = 6f;
        [Range(0.15f, 0.5f)] public float directionalFlinchSeconds = 0.28f;
        [Tooltip("Share of the backstep that follows the swing sideways (0 = straight away from the player).")]
        [Range(0f, 1f)] public float swingLateralBias = 0.35f;
        [Tooltip("Backstep on a plain hit (meters) and how long it takes. Slice 2b default 0 = light hits never move the enemy (was 0.08).")]
        [Range(0f, 0.5f)] public float lightBackstepMeters = 0f;
        [Range(0.05f, 0.5f)] public float lightBackstepSeconds = 0.18f;
        [Tooltip("Extra backstep (meters) on a poise break.")]
        [Range(0f, 1.5f)] public float poiseBreakBackstepMeters = 0.35f;

        [Header("Slice 2: hit-chain escalation (B2)")]
        public bool chainReactionsEnabled = true;
        [Tooltip("Hits in a row on the same enemy inside this window count as a chain (resets when it lapses).")]
        [Range(0.3f, 3f)] public float hitChainWindowSeconds = 1.2f;
        [Tooltip("Chain hit number that gets the stronger flinch + backstep.")]
        [Range(2, 6)] public int chainStrongHitCount = 2;
        [Tooltip("Chain hit number that gets the full-body stumble (Big reaction, two backsteps, lean back).")]
        [Range(2, 8)] public int chainStumbleHitCount = 3;
        [Range(1f, 3f)] public float chainStrongFlinchMultiplier = 1.6f;
        [Range(0f, 1f)] public float chainStrongBackstepMeters = 0.25f;
        [Range(0.3f, 2f)] public float chainStumbleSeconds = 0.9f;
        [Range(0f, 2f)] public float chainStumbleBackstepMeters = 0.8f;
        [Range(0f, 30f)] public float chainStumbleLeanDegrees = 18f;
        [Tooltip("After a stumble, heavy-hit Big reaction or poise break the enemy cannot be Big-staggered again by hit chains / heavy hits for this long (cap against stun-lock). Poise breaks still always work.")]
        [Range(0f, 8f)] public float bigReactionImmunitySeconds = 2.5f;

        [Header("Slice 2: heavy hit slide-back + waist bend (B3)")]
        public bool heavyReactionEnabled = true;
        [Tooltip("Big reaction length on a charged / heavy hit (seconds).")]
        [Range(0.3f, 2f)] public float heavyBigReactionSeconds = 0.7f;
        [Tooltip("Slide-back distance (meters) and duration on a heavy hit. Slice 2b default 0.35 m (was 0.8) = push a bit.")]
        [Range(0f, 2f)] public float heavySlideMeters = 0.35f;
        [Range(0.1f, 1f)] public float heavySlideSeconds = 0.3f;
        [Tooltip("Procedural waist bend (degrees) and duration on a heavy hit.")]
        [Range(0f, 40f)] public float heavyWaistBendDegrees = 20f;
        [Range(0.2f, 1f)] public float heavyWaistBendSeconds = 0.5f;
        [Tooltip("On = a heavy hit does not Big-stagger an enemy that is committed to a swing (swing not interrupted) or has attackArmor AllAttacks; it only bends the waist.")]
        public bool heavyRespectsCommitment = true;
        [Tooltip("Slide distance multiplier when a heavy hit does not Big-stagger (immunity window).")]
        [Range(0f, 1f)] public float heavyFallbackScale = 0.4f;
        [Tooltip("Slides stop at a ledge: max ground drop (meters) under the next step. Total distance of stacked slides is capped at Max Total Slide.")]
        [Range(0.1f, 2f)] public float hitSlideMaxLedgeDrop = 0.5f;
        [Range(0.2f, 3f)] public float hitSlideMaxTotalMeters = 1.2f;

        [Header("Slice 2b: directional reactions v2 (strike direction)")]
        [Tooltip("Master switch for Slice 2b: lean along the actual strike direction, weighted spring lean with head whip, directional hit clips (medium / heavy) and the animation-only knockdown. Off = Slice 2 behaviour.")]
        public bool enableDirectionalReactionsV2 = true;
        [Tooltip("BladeVelocity = read the player's blade motion at the hit (fallback: combo table below). ComboTable = Slice 2 swingSide table only.")]
        public DMSwingDirectionSource swingDirectionSource = DMSwingDirectionSource.BladeVelocity;
        [Tooltip("Blade speed (m/s, relative to the player root) below which the combo table is used instead.")]
        [Range(0f, 10f)] public float bladeVelocityMinSpeed = 2f;
        [Tooltip("How far back (seconds) the blade sampler looks to measure the swing.")]
        [Range(0.016f, 0.15f)] public float bladeVelocityWindowSeconds = 0.05f;
        [Tooltip("Share of the lean that follows the blade sideways (rest = straight away from the player). 1 = pure strike direction.")]
        [Range(0f, 1f)] public float strikeLeanSideShare = 0.7f;

        [Header("Slice 2b: light lean (procedural, weighted, root never moves)")]
        [Tooltip("Peak lean (degrees) of a plain light hit, spread over spine / chest / head. Light hits never move the root (lightBackstepMeters 0).")]
        [Range(0f, 35f)] public float lightLeanDegrees = 16f;
        [Tooltip("Torso twist (degrees) toward the strike side on a light hit.")]
        [Range(0f, 30f)] public float lightTwistDegrees = 12f;
        [Tooltip("Seconds to snap to the peak lean (fast attack = impact).")]
        [Range(0.02f, 0.15f)] public float lightLeanAttackSeconds = 0.045f;
        [Tooltip("Seconds of the weighted settle after the peak (spring with overshoot).")]
        [Range(0.15f, 1.5f)] public float lightLeanSettleSeconds = 0.6f;
        [Tooltip("Settle spring frequency (Hz). Lower = heavier, slower sway back.")]
        [Range(0.5f, 8f)] public float leanSpringFrequency = 2.6f;
        [Tooltip("Settle spring damping ratio. Lower = more overshoot past neutral (weight); 1 = no overshoot.")]
        [Range(0.1f, 1f)] public float leanSpringDamping = 0.42f;
        [Range(0f, 1f)] public float leanSpineWeight = 0.35f;
        [Range(0f, 1f)] public float leanChestWeight = 0.4f;
        [Range(0f, 1f)] public float leanHeadWeight = 0.25f;
        [Tooltip("Head whip: the head starts this many seconds after the torso and swings further (multiplier) - reads as a snap.")]
        [Range(0f, 0.12f)] public float headWhipDelaySeconds = 0.035f;
        [Range(1f, 3f)] public float headWhipMultiplier = 1.7f;
        [Tooltip("Lean / twist multiplier on a critical hit.")]
        [Range(1f, 2.5f)] public float critLeanMultiplier = 1.4f;
        [Tooltip("Hit stop punch: the lean snaps to its peak during the hit-stop freeze and holds there until the freeze ends, then settles.")]
        public bool leanHoldPeakThroughHitstop = true;

        [Header("Slice 2b: weight scaling (damage / heavy attack)")]
        [Tooltip("Damage that gives a 1x reaction. Reactions scale with (damage / this) ^ exponent, clamped to min / max. Weapon heft is read through damage (heavier weapons hit harder).")]
        [Range(1f, 500f)] public float reactionDamageReference = 25f;
        [Range(0.1f, 2f)] public float reactionDamageExponent = 0.6f;
        [Range(0.2f, 1f)] public float reactionDamageScaleMin = 0.75f;
        [Range(1f, 3f)] public float reactionDamageScaleMax = 1.6f;
        [Tooltip("Extra multiplier for charged / strong player attacks.")]
        [Range(1f, 3f)] public float heavyAttackWeightMultiplier = 1.35f;
        [Tooltip("Hit stop frames added at the top of the weight range (scaled by weight). Adds punch to big hits. 0 = hit stop unchanged.")]
        [Range(0, 6)] public int hitstopWeightBonusFrames = 2;

        [Header("Slice 2b: randomness (per hit)")]
        [Tooltip("Random +/- degrees added to the lean direction each hit.")]
        [Range(0f, 45f)] public float leanAngleJitterDegrees = 14f;
        [Tooltip("Random intensity range multiplied into every reaction (lean, twist, clip weight).")]
        [Range(0.3f, 1f)] public float reactionIntensityRandomMin = 0.85f;
        [Range(1f, 2f)] public float reactionIntensityRandomMax = 1.2f;
        [Tooltip("Random +/- degrees added to the twist each hit.")]
        [Range(0f, 15f)] public float twistRandomDegrees = 4f;
        [Tooltip("Chance (0-1) the twist goes the opposite way (sign variance).")]
        [Range(0f, 0.5f)] public float twistSignFlipChance = 0.2f;
        [Tooltip("Random hit clip playback speed range (medium / heavy).")]
        [Range(0.5f, 1f)] public float clipSpeedRandomMin = 0.9f;
        [Range(1f, 1.6f)] public float clipSpeedRandomMax = 1.15f;
        [Tooltip("Random hit clip layer weight range (multiplied into the tier weight).")]
        [Range(0.3f, 1f)] public float clipWeightRandomMin = 0.85f;
        [Range(0.3f, 1f)] public float clipWeightRandomMax = 1f;
        [Tooltip("Max random start offset (normalized) into a hit clip, for variety.")]
        [Range(0f, 0.3f)] public float clipStartOffsetRandomMax = 0.1f;
        [Tooltip("Never play the same clip / variation twice in a row on the same enemy, and keep consecutive lean directions apart.")]
        public bool avoidRepeatVariation = true;
        [Tooltip("Heavy hits: chance (0-1) of an extra stumble step after the push, and its length (meters).")]
        [Range(0f, 1f)] public float heavyStumbleStepChance = 0.3f;
        [Range(0f, 0.5f)] public float heavyStumbleStepMeters = 0.15f;

        [Header("Slice 2b: medium / heavy directional clips (arms masked = weapon grip kept)")]
        [Tooltip("Medium = chain hit 2: PROTOFACTOR GetHitLight* on the additive torso layer (arms / hands masked).")]
        public bool mediumUsesClip = true;
        [Range(0f, 1f)] public float mediumClipWeight = 0.75f;
        [Range(0.5f, 2f)] public float mediumClipSpeed = 1.15f;
        [Tooltip("Lean scale on top of the medium clip (x light lean).")]
        [Range(0f, 2f)] public float mediumLeanScale = 1.25f;
        [Tooltip("Medium variation: chance of using the GetHitHeavy clip at reduced weight instead of GetHitLight.")]
        [Range(0f, 1f)] public float mediumAltHeavyClipChance = 0.35f;
        [Range(0f, 1f)] public float mediumAltHeavyClipWeight = 0.55f;
        [Tooltip("Heavy = charged / strong hit, chain stumble, poise break: GetHitHeavy* on the body layer (hips, spine, head, legs; arms masked).")]
        public bool heavyUsesClip = true;
        [Range(0f, 1f)] public float heavyClipWeight = 1f;
        [Range(0.5f, 2f)] public float heavyClipSpeed = 1.2f;
        [Tooltip("Peak procedural lean (degrees) on top of the heavy clip.")]
        [Range(0f, 45f)] public float heavyLeanDegrees = 24f;
        [Range(0.2f, 2f)] public float heavyLeanSettleSeconds = 0.8f;
        [Range(0.02f, 0.3f)] public float reactionBlendInSeconds = 0.06f;
        [Range(0.05f, 0.6f)] public float reactionBlendOutSeconds = 0.22f;

        [Header("Slice 2b: knockdown (animation only, no ragdoll)")]
        [Tooltip("Heavy hits can knock the enemy down: PROTOFACTOR Death* fall (cut at the land point) -> Dead* hold -> GetBackUp*, matched by strike direction. No ragdoll (ragdoll is death only). Weapon stays in hand.")]
        public bool knockdownEnabled = true;
        [Range(0f, 100f)] public float knockdownChancePercent = 15f;
        [Tooltip("Bonus when the enemy's poise is below 30%.")]
        [Range(0f, 100f)] public float knockdownLowPoiseBonusPercent = 15f;
        [Range(0f, 100f)] public float knockdownCritBonusPercent = 10f;
        [Tooltip("Replaces the base chance when the hit breaks poise.")]
        [Range(0f, 100f)] public float knockdownOnPoiseBreakPercent = 25f;
        [Range(0f, 100f)] public float knockdownMaxChancePercent = 40f;
        [Tooltip("Push along the strike direction during the fall (meters). Clip root motion is ignored.")]
        [Range(0f, 1.5f)] public float knockdownPushMeters = 0.5f;
        [Range(0.1f, 1f)] public float knockdownPushSeconds = 0.4f;
        [Range(0.5f, 2f)] public float knockdownFallSpeed = 1.1f;
        [Tooltip("Normalized time in each Death* fall clip where the body lands (cut there, cross-fade to the lying hold). Named by FALL direction: Front = falls forward (hit from behind), Back = falls backward (hit in front), Left / Right = falls to that side. Defaults measured from the clips' body height (Oct 19).")]
        [Range(0.1f, 1f)] public float knockdownFallExitFront = 0.75f;
        [Range(0.1f, 1f)] public float knockdownFallExitBack = 0.63f;
        [Range(0.1f, 1f)] public float knockdownFallExitLeft = 0.45f;
        [Range(0.1f, 1f)] public float knockdownFallExitRight = 0.52f;
        [Tooltip("Seconds lying down before the get-up (hits while down do not extend it).")]
        [Range(0.2f, 4f)] public float knockdownDownSeconds = 1f;
        [Tooltip("Random +/- seconds on the lying time.")]
        [Range(0f, 1f)] public float knockdownDownRandomSeconds = 0.25f;
        [Range(0.5f, 2.5f)] public float getUpPlaybackSpeed = 1.25f;
        [Tooltip("Normalized time in the GetBackUp clip where control returns (layer blends out, aim fades back in).")]
        [Range(0.6f, 1f)] public float getUpExitNormalized = 0.92f;
        [Tooltip("Aim / chest pitch fade back in after the get-up (the knockdown layer blends out over this time).")]
        [Range(0.05f, 1f)] public float knockdownAimFadeInSeconds = 0.25f;
        [Tooltip("Player melee cannot damage the enemy during the last part of the get-up (seconds).")]
        [Range(0f, 1.5f)] public float getUpInvulnerableSeconds = 0.4f;
        [Tooltip("Seconds after a get-up before the same enemy can be knocked down again.")]
        [Range(0f, 30f)] public float knockdownImmunitySeconds = 8f;
        [Tooltip("A downed enemy gives up its melee token / attack slot (the next attacker still waits its staggered start).")]
        public bool knockdownReleasesAttackSlot = true;
        [Tooltip("Skip the knockdown (heavy reaction instead) on ground steeper than this or near a ledge.")]
        [Range(5f, 45f)] public float knockdownMaxSlopeDegrees = 20f;
        [Range(0.2f, 3f)] public float knockdownLedgeCheckMeters = 1f;
        [Tooltip("On = player melee crits never take the old crit-ragdoll knockdown (EnemyInvectorRagdollBridge). Crit damage / numbers / VFX unchanged.")]
        public bool retireCritRagdollKnockdown = true;
        public bool logHitReactionsInPlay = false;

        [Header("Future tuning hooks")]
        [Range(0, 12)] public int hitstopLightFrames = 2;
        [Range(0, 12)] public int hitstopHeavyFrames = 6;
        [Range(0.05f, 0.5f)] public float parryWindowSeconds = 0.2f;

        [Header("Parry clash VFX")]
        [Tooltip("One-shot sparks at the blade contact on a successful parry. Assign SparksLong.prefab under Prefabs/Combat/VFX.")]
        public GameObject parryClashVfxPrefab;
        [Range(0.5f, 2.5f)] public float parryClashVfxScale = 1.15f;
        [Range(0.1f, 1.5f)] public float parryClashVfxLifetimeSeconds = 0.45f;

        [Header("Combat camera snap")]
        [Tooltip("When on, melee block/attack auto-face eases Invector tpCamera yaw behind the body and ECM2 combat focus uses combatCameraSnapSeconds for yaw damp. When off, camera yaw is not snapped on auto-face and combat focus uses the CombatFocusController yawSmoothLambda only.")]
        public bool enableCombatCameraSnap = false;
        [Tooltip("Seconds to ease the camera behind the player when melee attack/block auto-face rotates the body toward a threat (Invector tpCamera yaw + ECM2 combat focus). Ignored when enableCombatCameraSnap is off.")]
        [Range(0.1f, 0.5f)] public float combatCameraSnapSeconds = 0.35f;

        [Header("Player combat buffer (enemy standoff)")]
        [Tooltip("When on, enemies treat playerCombatBufferRadius as a global minimum planar separation vs Kade (Mathf.Max with each enemy prefab minCombatSeparation). Off or radius 0 = prefab-only spacing.")]
        public bool enableGlobalCombatSeparation = true;
        [Tooltip("Minimum keep-away radius (meters) between Kade and engaging enemies. Does not change Player_v7 capsule — AI ring/standoff and step clamps only.")]
        [Range(0f, 4f)] public float playerCombatBufferRadius = 1.35f;

        /// <summary>Global buffer vs player; prefab min when global off or radius ~0.</summary>
        public float ResolveMinCombatSeparationVsPlayer(float enemyPrefabMinSeparation)
        {
            if (!enableGlobalCombatSeparation || playerCombatBufferRadius <= 0.001f)
                return enemyPrefabMinSeparation;

            return Mathf.Max(enemyPrefabMinSeparation, playerCombatBufferRadius);
        }

        [Header("Camera shake")]
        [Tooltip("Trauma added on a charged strong hit that actually damages an enemy. 0 = off. Useful range 0–1 (CameraShake trauma caps at 1).")]
        [Range(0f, 2f)] public float chargedHitShakeAmplitude = 0.55f;
        [Tooltip("Hold trauma this long after a charged hit, then decay. 0 = one-shot punch only.")]
        [Range(0f, 0.4f)] public float chargedHitShakeDurationSeconds = 0.22f;
        [Tooltip("Trauma on a successful parry. 0 = off. Medium vs block; weaker than a charged hit.")]
        [Range(0f, 2f)] public float parryShakeAmplitude = 0.32f;
        [Range(0f, 0.4f)] public float parryShakeDurationSeconds = 0.14f;
        [Tooltip("Trauma on a successful guard connect that is not a parry. 0 = off. Smallest of the three.")]
        [Range(0f, 2f)] public float blockShakeAmplitude = 0.18f;
        [Range(0f, 0.4f)] public float blockShakeDurationSeconds = 0.08f;

        [Header("Grenades")]
        [Tooltip("Master switch for throwable grenades. On: G (LB) arms; hold LMB (LT) to cook; release LMB (LT) to throw. Off = stacks stay in inventory but cannot be thrown. Default off.")]
        public bool enableGrenades = false;
        [Tooltip("Uncooked / minimum cook fuse before Grenade Cook Mastery skill ranks.")]
        [Range(1f, 15f)] public float grenadeCookSecondsBase = 5f;
        [Tooltip("Hard cap on cook fuse (base + skill ranks, +1s per rank up to this).")]
        [Range(1f, 20f)] public float grenadeCookSecondsMax = 10f;
        [Tooltip("Landing projector ring diameter scales with splash radius skills.")]
        public bool grenadeLandingRingUsesSplashSkills = true;
        [Tooltip("Tint for the throw arc LineRenderer while aiming.")]
        public Color grenadeThrowArcLineColor = new Color(1f, 0f, 0f, 0.88f);
        [Tooltip("Tint for the landing target projector ring.")]
        public Color grenadeLandingRingColor = new Color(1f, 0f, 0f, 0.42f);
        [Tooltip("Multiplies blast radius for the throw landing projector ring (ring diameter vs splash).")]
        [Range(1f, 25f)] public float grenadeLandingRingSizeMultiplier = 10f;
        [Tooltip("Optional ring projector texture (Invector damage indicator ring). Null = color-only ring sizing.")]
        public Texture2D grenadeLandingRingIndicatorTexture;
        [Tooltip("Flat HP removed from Kade when a cooked grenade detonates in hand (cook-out). Enemy splash still uses grenade min/max damage.")]
        [Range(1f, 200f)] public float grenadeCookOutSelfDamage = 50f;
        [Tooltip("Enemy locomotion + attack lockout after a grenade blast (severe full-body reaction). Thrown and cook-out detonations.")]
        [Range(0f, 10f)] public float grenadeEnemyStaggerSeconds = 5f;
        [Tooltip("Hotbar local index (0–9) for frag grenades. Must match this slot to arm with G; shown on Hot Cross lower-right.")]
        [Range(0, 9)] public int grenadeHotbarLocalIndex = 9;
        [Tooltip("Hold G / LB this long (seconds) on Hot Cross lower-right to load a grenade to the right hand. Tap still cycles stacks.")]
        [Range(0.25f, 2.5f)] public float hotCrossGrenadeLoadHoldSeconds = 1f;

        [Header("Grenades — splash & detonation")]
        [Tooltip("AOE splash radius (world units). Also drives Invector vExplosive max radius for force/VFX sync.")]
        [Range(0.5f, 25f)] public float grenadeExplosionRadius = 10f;
        [Tooltip("Random damage roll min (before splash damage skills).")]
        [Range(1f, 200f)] public float grenadeMinDamage = 25f;
        [Tooltip("Random damage roll max (before splash damage skills).")]
        [Range(1f, 200f)] public float grenadeMaxDamage = 60f;
        [Tooltip("Full damage at or inside this distance from blast center.")]
        [Range(0f, 15f)] public float grenadeInnerRadiusFullDamage = 2f;
        [Tooltip("Damage multiplier at outer edge of splash (1 at/inside inner radius).")]
        [Range(0f, 1f)] public float grenadeEdgeDamageFalloff = 0.35f;
        [Tooltip("Uncooked throw: seconds after first bounce before land detonation (vExplosive collisionEnterTimer).")]
        [Range(0.1f, 15f)] public float grenadeLandFuseSeconds = 3f;
        [Tooltip("Physics knockback on vExplosive apply-force pass.")]
        [Range(0f, 15000f)] public float grenadeExplosionForce = 5000f;
        [Tooltip("Upward modifier on explosion force.")]
        [Range(0f, 3f)] public float grenadeExplosionUpwardsModifier = 1f;
        [Tooltip("Optional extra explosion VFX prefab spawned at detonation (Invector child VFX still runs). Null = skip.")]
        public GameObject grenadeExplosionVfxPrefab;
        [Tooltip("Trauma when the player's grenade detonates. 0 = off.")]
        [Range(0f, 2f)] public float grenadeCameraShakeAmplitude = 0.38f;
        [Range(0f, 0.5f)] public float grenadeCameraShakeDurationSeconds = 0.16f;
        [Tooltip("When on, Pioneer splash can hurt Kade (not cook-out flat self damage).")]
        public bool grenadeSplashDamagesPlayer = false;
        [Tooltip("When on, splash can hurt expedition companions.")]
        public bool grenadeSplashDamagesCompanions = false;
        [Tooltip("Pioneer AOE OverlapSphere mask on DMIGrenadeExplosive. 0 = keep prefab/component hitLayers (do not overwrite at runtime).")]
        public LayerMask grenadePioneerSplashLayerMask;
        [Tooltip("vExplosive applyDamageLayer. 0 = keep prefab (e.g. DM_Frag_Grenade Enemy + BodyPart). Pioneer splash ignores Invector damage.")]
        public LayerMask grenadeVExplosiveDamageLayerMask;
        [Tooltip("vExplosive applyForceLayer. 0 = keep prefab force layers.")]
        public LayerMask grenadeVExplosiveForceLayerMask;

        [Tooltip("Hotbar local index (0–9) for survival consumables (meds/food). Must be on hotbar to use; shown Hot Cross lower-left.")]
        [Range(0, 9)] public int survivalHotbarLocalIndex = 8;

        [Header("Attack interrupts (player hit interrupts an enemy attack)")]
        [Tooltip("Master switch. Off = enemy attacks are never interrupted by a player hit (poise break / parry / guard break still stagger).")]
        public bool enableAttackInterrupts = true;
        [Tooltip("Base chance (0-1) a player hit interrupts an enemy light swing (first swing of a chain).")]
        [Range(0f, 1f)] public float interruptBaseChanceLight = 0.12f;
        [Tooltip("Base chance for combo follow-up swings (2nd / 3rd). Lower = follow-ups are harder to interrupt.")]
        [Range(0f, 1f)] public float interruptBaseChanceFollowUp = 0.06f;
        [Tooltip("Base chance for the enemy charged swing before its commit window.")]
        [Range(0f, 1f)] public float interruptBaseChanceCharged = 0.10f;
        [Tooltip("Swing progress (0-1 normalized) below which a hit counts as 'early': earlyBonus applies, and the hit is a guaranteed interrupt when Early Window Guaranteed is on.")]
        [Range(0f, 1f)] public float interruptEarlyWindowNormalized = 0.35f;
        [Tooltip("A hit inside the early window always interrupts (unless super-armor or interrupt immunity). Replaces the old 'hit interrupts a wind-up' idea (B6).")]
        public bool interruptEarlyWindowGuaranteed = true;
        [Range(0f, 1f)] public float interruptEarlyBonus = 0.25f;
        [Tooltip("Added chance on a critical hit.")]
        [Range(0f, 1f)] public float interruptCritBonus = 0.20f;
        [Tooltip("Added chance on a player charged / heavy hit.")]
        [Range(0f, 1f)] public float interruptHeavyHitBonus = 0.40f;
        [Tooltip("Player hits closer together than this (seconds) count as a chain on the same enemy.")]
        [Range(0.2f, 5f)] public float interruptChainWindowSeconds = 1.5f;
        [Range(0f, 0.5f)] public float interruptChainBonusPerHit = 0.10f;
        [Range(0f, 1f)] public float interruptChainBonusMax = 0.30f;
        [Tooltip("Poise fraction (0-1) below which the low-poise bonus applies.")]
        [Range(0f, 1f)] public float interruptLowPoiseThreshold = 0.5f;
        [Range(0f, 1f)] public float interruptLowPoiseBonus = 0.15f;
        [Tooltip("Health fraction (0-1) below which the low-health bonus applies.")]
        [Range(0f, 1f)] public float interruptLowHealthThreshold = 0.25f;
        [Range(0f, 1f)] public float interruptLowHealthBonus = 0.10f;
        [Tooltip("Hard cap on the chance interrupt probability.")]
        [Range(0f, 1f)] public float interruptMaxChance = 0.85f;
        [Tooltip("Guaranteed interrupt when a player hit breaks poise (ignores super-armor, commit window and immunity).")]
        public bool interruptGuaranteedOnPoiseBreak = true;
        [Tooltip("Guaranteed interrupt when a player charged / heavy hit lands on an enemy light swing before its commit window.")]
        public bool interruptGuaranteedOnHeavyVsLight = true;
        [Tooltip("Light swing progress (0-1) where the damage window is committed: from here only a poise break (or parry / guard break) can interrupt.")]
        [Range(0f, 1f)] public float interruptCommitStartLight = 0.45f;
        [Tooltip("Enemy charged swing progress (0-1) where super-armor starts (its damage window opens at 0.35).")]
        [Range(0f, 1f)] public float interruptCommitStartCharged = 0.35f;
        [Tooltip("After an interrupt, no further chance interrupts for this long (anti-stunlock). A poise break still interrupts.")]
        [Range(0f, 6f)] public float interruptImmunitySeconds = 2.0f;
        [Tooltip("Enemy cannot start another attack for this long after an interrupt (hit recovery lock).")]
        [Range(0.2f, 4f)] public float interruptRecoverySeconds = 1.5f;
        [Tooltip("How long the Small hit reaction holds the enemy in place (locomotion paused) after a chance / guaranteed interrupt.")]
        [Range(0.2f, 1.5f)] public float interruptReactionSeconds = 0.45f;
        [Tooltip("Logs every interrupt decision (enemy, class, swing progress, chance, roll) to the console in Play.")]
        public bool logInterruptsInPlay;

        [Header("God mode cheat + respawn fallback (2026-10-13 i)")]
        [Tooltip("Typing the cheat code during play toggles god mode (DMDevCommandState.GodMode): player AND enemies unkillable, infinite ammo.")]
        public bool godModeCheatEnabled = true;
        public string godModeCheatCode = "godmode";
        [Tooltip("God mode keeps enemies at 1 HP minimum (hits, hit marks and the health bar still work).")]
        public bool godModeEnemiesUnkillable = true;
        [Tooltip("God mode: shots consume no rounds.")]
        public bool godModeInfiniteAmmo = true;
        [Tooltip("Audit/test sessions only: dead enemies without their own respawn timer respawn after enemyRespawnDelay.")]
        public bool respawnEnemiesAfterDeath = true;
        [Range(0.5f, 120f)] public float enemyRespawnDelay = 6f;
        [Tooltip("Audit/test sessions only: the player respawns automatically after playerRespawnDelay (skips the death popup).")]
        public bool respawnPlayerAfterDeath = true;
        [Range(0.5f, 60f)] public float playerRespawnDelay = 3f;
        [Tooltip("Enemy Runtime Audit / tests: player-only protection (no damage, health refill, infinite ammo) for the run; enemies take real damage. Cleared when the run ends or play stops. Does not touch the typed godmode cheat.")]
        public bool auditGodMode = true;
        [Tooltip("2026-10-19 h: audit-only. When on, the audit player-health guard is released so the player can die to real enemy hits (death / Retry tests). Reset to off when the audit finishes.")]
        public bool auditAllowPlayerDeath = false;
        [Range(0.5f, 10f)] public float godModeToastSeconds = 2f;

        [Header("Player corpse (2026-10-13 i)")]
        [Tooltip("Dead player stays where it fell until respawn (root stopped, slide capped, ragdoll frozen after settle).")]
        public bool playerCorpseStayInPlace = true;
        [Range(0.2f, 5f)] public float playerCorpseSettleSeconds = 1.5f;
        [Tooltip("Max horizontal bone speed (m/s) while the corpse settles.")]
        [Range(0f, 10f)] public float playerCorpseMaxSlideSpeed = 1.5f;
        [Tooltip("After Retry/respawn: minimum seconds velocity is zeroed and ragdoll bones stay kinematic (includes stand-up).")]
        [Range(0f, 3f)] public float playerRespawnSettleSeconds = 0.85f;
        [Tooltip("After stand-up finishes: brief movement lock before walk input applies (reduces Invector inputSmooth slide).")]
        [Range(0f, 1.5f)] public float playerRespawnPostStandUpLockSeconds = 0.35f;
        [Tooltip("After stand-up: extra seconds horizontal RB velocity is clamped when grounded and movement input is idle.")]
        [Range(0f, 2f)] public float playerRespawnPostStandUpSettleSeconds = 0.75f;

        [Header("Ranged aggro + alerts (2026-10-14 a)")]
        [Tooltip("A player ranged hit provokes the enemy: it targets the shooter and chases past its vision, leash and chase-time limits for this many seconds (refreshed by each hit).")]
        [Range(0f, 60f)] public float rangedHitProvokeSeconds = 15f;
        [Tooltip("Player shots that miss alert enemies within this radius of the impact (they turn, investigate, search).")]
        [Range(0f, 30f)] public float rangedImpactAlertRadius = 8f;
        [Tooltip("Enemies within this radius of the shooter hear the gunshot and investigate toward it. 0 = off.")]
        [Range(0f, 80f)] public float gunshotHearingRadius = 25f;
        [Tooltip("Engaged enemy health bar stays up this long after the last hit even if the enemy isn't targeting the player.")]
        [Range(1f, 30f)] public float engagedHudLingerSeconds = 6f;

        [Header("Enemy spawn stabilizer (perf 2026-10-18 b)")]
        [Tooltip("Seconds after an enemy's spawn settle (grounded) before DMSpawnPhysicsStabilizer drops to watch mode: ground raycasts run on the interval below instead of every frame/physics step. Root stays kinematic every step. 0 = never (old every-frame behaviour).")]
        [Range(0f, 30f)] public float spawnStabilizerWatchAfterSettleSeconds = 2f;
        [Tooltip("Watch mode: seconds between ground / launch raycast checks per enemy.")]
        [Range(0.02f, 2f)] public float spawnStabilizerWatchCheckInterval = 0.2f;

        [Header("Debug")]
        public bool logCombatEventsInPlay;
        [Tooltip("Logs engaged enemy HUD fill (0-1) on each health change when that enemy is focused.")]
        public bool logEngagedHudFill;
    }

    public enum DMPlayerReactionsUnderGodMode
    {
        None = 0,
        LeanOnly = 1,
        LeanAndLight = 2,
        All = 3
    }
}

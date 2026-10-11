using UnityEngine;

namespace Project.Combat
{
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
        [Range(0.05f, 1f)] public float dashIFrameSeconds = 0.25f;

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
        [Tooltip("Animator speed during the freeze. 0 = fully frozen, 0.05 = near-frozen (keeps the pose alive).")]
        [Range(0f, 0.5f)] public float hitstopHoldSpeed = 0.05f;
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
        [Tooltip("Backstep on a plain hit (meters) and how long it takes. 0.08 m = 8 cm.")]
        [Range(0f, 0.5f)] public float lightBackstepMeters = 0.08f;
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
        [Tooltip("Slide-back distance (meters) and duration on a heavy hit. 0.5 - 1.0 m reads as a shove.")]
        [Range(0f, 2f)] public float heavySlideMeters = 0.8f;
        [Range(0.1f, 1f)] public float heavySlideSeconds = 0.35f;
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
        [Tooltip("Master switch for throwable grenades (G throw, cook, throw manager). Off = grenade items stay in inventory but cannot be thrown. Default off.")]
        public bool enableGrenades = false;

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

        [Header("Debug")]
        public bool logCombatEventsInPlay;    }
}

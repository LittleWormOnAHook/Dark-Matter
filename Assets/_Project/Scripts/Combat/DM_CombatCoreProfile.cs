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
        [Range(1f, 1.5f)] public float enemyMeleeAttackRangeMultiplier = 1.22f;
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

        [Header("Debug")]
        public bool logCombatEventsInPlay;
    }
}

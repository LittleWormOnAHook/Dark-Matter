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
        [Tooltip("Attack lockout after a normal block. Mild guard stun, about 0.35–0.45s — shorter than a parry. Souls and God of War treat a held block as a light recoil, not a riposte.")]
        [Range(0.2f, 1.2f)] public float blockStaggerSeconds = 0.4f;
        [Tooltip("Attack lockout after a parry (block inside parryWindowSeconds). Stronger riposte stun, about 0.7–0.85s. Sekiro and Souls parries are a real opening; a block is not.")]
        [Range(0.25f, 1.5f)] public float parryStaggerSeconds = 0.8f;
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

        [Header("Strong melee (hold light attack, then release)")]
        [Tooltip("Seconds the light-attack button (left mouse / Attack) must be held before a release plays the strong sword swing. After this threshold the charge pose stays until release. There is no maximum hold and the swing does not fire by itself. A shorter press-and-release stays a light tap. Right mouse stays block.")]
        [Range(0.2f, 0.8f)] public float strongMeleeChargeSeconds = 0.4f;
        [Tooltip("Scales the normal melee damage roll on that charged swing. 1.8 is clearly above a light hit.")]
        [Range(1.2f, 3f)] public float strongMeleeDamageMultiplier = 1.8f;

        [Header("Future tuning hooks")]
        [Range(0, 12)] public int hitstopLightFrames = 2;
        [Range(0, 12)] public int hitstopHeavyFrames = 6;
        [Range(0.05f, 0.5f)] public float parryWindowSeconds = 0.2f;

        [Header("Debug")]
        public bool logCombatEventsInPlay;
    }
}

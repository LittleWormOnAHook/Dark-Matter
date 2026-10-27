using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-25s Slice 5 E3 (A31 #6 Momentum start): player momentum meter + Spin AoE special.
    /// Resources/Combat/DM_MomentumProfile, read live (Genesis Studio > Combat > Momentum &amp; spin).
    /// Body weight: suit-assisted human spin (anticipation counter-turn, eased 360, short recovery); EVA suit
    /// compensates Io gravity so timings are Earth-like.
    /// Rollback: enableMomentum = false (no gain, spin rejected).
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Combat/Momentum Profile", fileName = "DM_MomentumProfile")]
    public sealed class DM_MomentumProfile : ScriptableObject
    {
        public const string ResourcesPath = "Combat/DM_MomentumProfile";
        private static DM_MomentumProfile live;
        private static DM_MomentumProfile fallback;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLiveCache() { live = null; }

        public static DM_MomentumProfile Live
        {
            get
            {
                if (live == null) live = Resources.Load<DM_MomentumProfile>(ResourcesPath);
                if (live != null) return live;
                if (fallback == null) { fallback = CreateInstance<DM_MomentumProfile>(); fallback.hideFlags = HideFlags.HideAndDontSave; }
                return fallback;
            }
        }

        [Header("Momentum meter")]
        public bool enableMomentum = true;
        [Range(10f, 500f)] public float maxMomentum = 100f;
        [Tooltip("Gain per light melee hit on an enemy.")]
        [Range(0f, 100f)] public float gainLightHit = 8f;
        [Tooltip("Gain per charged / heavy hit.")]
        [Range(0f, 100f)] public float gainHeavyHit = 15f;
        [Range(0f, 50f)] public float gainCritBonus = 5f;
        [Range(0f, 100f)] public float gainKill = 12f;
        [Tooltip("Hits within this window chain; each chain step multiplies gain.")]
        [Range(0.1f, 5f)] public float chainWindowSeconds = 1.6f;
        [Range(0f, 1f)] public float chainStepMultiplier = 0.15f;
        [Range(0, 20)] public int chainMaxSteps = 5;
        [Tooltip("Meter starts draining this long after the last gain.")]
        [Range(0f, 30f)] public float decayDelaySeconds = 3f;
        [Range(0f, 100f)] public float decayPerSecond = 6f;
        [Tooltip("Fraction of the meter lost when the player takes damage.")]
        [Range(0f, 1f)] public float lossOnPlayerHitFraction = 0.25f;
        [Tooltip("Ranged / turret hits also build momentum.")]
        public bool rangedHitsGain = false;

        [Header("Spin AoE special (Slice 5 E3)")]
        public bool enableSpin = true;
        [Tooltip("Momentum spent per spin (0 = free).")]
        [Range(0f, 500f)] public float spinCost = 100f;
        [Range(0f, 50f)] public float spinStaminaCost = 0f;
        [Tooltip("Anticipation counter-turn before the spin (body weight wind-up).")]
        [Range(0f, 60f)] public float spinWindupDegrees = 14f;
        [Range(0f, 0.6f)] public float spinWindupSeconds = 0.12f;
        [Tooltip("Duration of the 360 sweep (eased in / out).")]
        [Range(0.2f, 2f)] public float spinSeconds = 0.55f;
        [Range(0f, 1f)] public float spinRecoverySeconds = 0.18f;
        [Tooltip("1 = full 360, 2 = double spin.")]
        [Range(1, 3)] public int spinTurns = 1;
        public bool spinClockwise = true;
        [Range(0.5f, 8f)] public float spinRadius = 2.6f;
        [Range(0.5f, 4f)] public float spinHeightTolerance = 1.8f;
        [Range(0f, 500f)] public float spinDamage = 28f;
        [Range(0f, 5f)] public float spinPoiseMultiplier = 1.5f;
        [Tooltip("Spin hits count as charged hits (interrupts / knockback rules).")]
        public bool spinCountsAsChargedHit = true;
        [Range(0f, 2f)] public float spinShakeAmplitude = 0.35f;
        [Range(0f, 1f)] public float spinShakeSeconds = 0.18f;
        [Header("Spin input (2026-10-26a, Anthony: both)")]
        [Tooltip("Charged release (hold Attack past the charge threshold, release) spins instead of the strong swing when the meter is full.")]
        public bool spinAutoOnChargedRelease = true;
        [Tooltip("Hold Attack (KBM: Left Mouse, pad: Right Trigger; InputSystem_Actions Player/Attack) with a full meter for spinHoldSeconds to spin.")]
        public bool spinOnHoldAttack = true;
        [Range(0.2f, 2f)] public float spinHoldSeconds = 0.7f;
        public bool debugLog = false;

        /// <summary>0 Subtle, 1 Default, 2 Extreme.</summary>
        public void ApplyPreset(int level)
        {
            enableMomentum = true; enableSpin = true;
            switch (level)
            {
                case 0: gainLightHit = 5f; gainHeavyHit = 10f; gainKill = 8f; decayDelaySeconds = 2f; decayPerSecond = 10f; lossOnPlayerHitFraction = 0.4f;
                    spinHoldSeconds = 0.9f;
                    spinWindupDegrees = 20f; spinWindupSeconds = 0.18f; spinSeconds = 0.7f; spinRecoverySeconds = 0.25f; spinRadius = 2.2f; spinDamage = 18f; spinPoiseMultiplier = 1f; spinShakeAmplitude = 0.2f; break;
                case 2: gainLightHit = 14f; gainHeavyHit = 25f; gainKill = 20f; decayDelaySeconds = 6f; decayPerSecond = 3f; lossOnPlayerHitFraction = 0.1f;
                    spinHoldSeconds = 0.5f;
                    spinWindupDegrees = 8f; spinWindupSeconds = 0.06f; spinSeconds = 0.4f; spinRecoverySeconds = 0.1f; spinRadius = 3.6f; spinDamage = 45f; spinPoiseMultiplier = 2.5f; spinShakeAmplitude = 0.6f; break;
                default: gainLightHit = 8f; gainHeavyHit = 15f; gainKill = 12f; decayDelaySeconds = 3f; decayPerSecond = 6f; lossOnPlayerHitFraction = 0.25f;
                    spinHoldSeconds = 0.7f;
                    spinWindupDegrees = 14f; spinWindupSeconds = 0.12f; spinSeconds = 0.55f; spinRecoverySeconds = 0.18f; spinRadius = 2.6f; spinDamage = 28f; spinPoiseMultiplier = 1.5f; spinShakeAmplitude = 0.35f; break;
            }
        }
    }
}

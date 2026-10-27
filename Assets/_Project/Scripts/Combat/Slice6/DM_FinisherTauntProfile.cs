using System.Collections.Generic;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-26a Combat Slice 6: finishers, enemy taunts, group camera pull-back.
    /// Resources/Combat/DM_FinisherTauntProfile, read live (Genesis Studio > Combat > Finishers, taunts &amp; camera).
    /// 2026-10-26i: solo player heavy strike (Standing Melee Attack Downward) -- no paired SwordFinisher grab/throw.
    /// 2026-10-26k: when finisher available, other melee plays faster; only finisher gets bonus damage; jetpack Rad/Heat/Cold/Mix sequential blink.
    /// 2026-10-26l: finisher damage = MaxHealth * percent * mult (no forced kill); input = hold Use + Attack (Finisher action).
    /// Enemy reacts/dies via normal damage. PROTOFACTOR taunts unchanged. Rollback: enableFinishers = false.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Combat/Finisher Taunt Profile", fileName = "DM_FinisherTauntProfile")]
    public sealed class DM_FinisherTauntProfile : ScriptableObject
    {
        public const string ResourcesPath = "Combat/DM_FinisherTauntProfile";
        private static DM_FinisherTauntProfile live, fallback;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLiveCache() { live = null; }

        public static DM_FinisherTauntProfile Live
        {
            get
            {
                if (live == null) live = Resources.Load<DM_FinisherTauntProfile>(ResourcesPath);
                if (live != null) return live;
                if (fallback == null) { fallback = CreateInstance<DM_FinisherTauntProfile>(); fallback.hideFlags = HideFlags.HideAndDontSave; }
                return fallback;
            }
        }

        [Header("Finishers (Attack press near a low-HP enemy, melee drawn) -- solo heavy strike, no paired grab")]
        public bool enableFinishers = true;
        [Tooltip("Player-only finisher clip (default: Standing Melee Attack Downward). Not the charged attack.")]
        public AnimationClip finisherPlayerClip;
        [Tooltip("Legacy paired enemy clip -- unused since 2026-10-26i (no grab/throw). Kept for rollback.")]
        public AnimationClip finisherEnemyClip;
        [Tooltip("Enemy HP fraction at or below which a finisher is offered (default 20%).")]
        [Range(0f, 1f)] public float finisherLowHpFraction = 0.2f;
        [Tooltip("On = also offer finisher for finisherStaggerWindow after interrupt. Default off (HP-only).")]
        public bool enableFinisherOnStagger = false;
        [Tooltip("Seconds after an enemy is interrupted / staggered during which a finisher is offered (only if enableFinisherOnStagger).")]
        [Range(0f, 5f)] public float finisherStaggerWindow = 1.2f;
        [Header("Finisher body weight (26i)")]
        [Tooltip("Spine/chest pitch (deg) through DMPlayerBodyWeight during the finisher swing.")]
        [Range(0f, 45f)] public float finisherBodyPitchDeg = 14f;
        [Tooltip("Hip drop (m) impulse during the finisher swing.")]
        [Range(0f, 0.4f)] public float finisherBodyHipDrop = 0.06f;
        [Range(0.5f, 5f)] public float finisherRange = 2.4f;
        [Range(10f, 180f)] public float finisherMaxAngle = 70f;
        [Tooltip("Paired distance: the enemy is aligned this far in front of the player.")]
        [Range(0.4f, 3f)] public float finisherPairDistance = 1.1f;
        [Range(0f, 1f)] public float finisherAlignSeconds = 0.15f;
        [Tooltip("Normalized time of the player clip at which the enemy is killed.")]
        [Range(0.05f, 1f)] public float finisherKillNormalizedTime = 0.6f;
        [Range(0.5f, 8f)] public float finisherMaxSeconds = 3.2f;
        [Range(0.5f, 2f)] public float finisherPlaySpeed = 1.15f;
        [Range(0f, 0.5f)] public float finisherBlendIn = 0.15f;
        [Range(0f, 0.6f)] public float finisherBlendOut = 0.25f;
        [Range(0f, 30f)] public float finisherCooldown = 2f;
        [Tooltip("Non-humanoid enemies (generic rigs) are killed with the player clip only.")]
        public bool finisherAllowNonHumanoid = true;
        public bool finisherCountsAsGruesome = true;
        [Range(0f, 2f)] public float finisherShakeAmplitude = 0.4f;

        [Header("Taunts (enemies in the Director's wait / ring hold)")]
        public bool enableTaunts = true;
        public List<AnimationClip> tauntClips = new List<AnimationClip>();
        [Tooltip("Chance per second, per eligible holder.")]
        [Range(0f, 1f)] public float tauntChancePerSecond = 0.12f;
        [Range(1, 4)] public int tauntMaxConcurrent = 1;
        [Range(0f, 30f)] public float tauntPerEnemyCooldown = 9f;
        [Range(0f, 15f)] public float tauntGroupCooldown = 3f;
        [Range(0f, 10f)] public float tauntMinDistance = 3f;
        [Range(2f, 30f)] public float tauntMaxDistance = 12f;
        [Range(0f, 3f)] public float tauntMaxMoveSpeed = 0.8f;
        [Range(0.5f, 6f)] public float tauntMaxSeconds = 2.6f;
        [Range(0f, 0.6f)] public float tauntBlendIn = 0.2f;
        [Range(0f, 0.6f)] public float tauntBlendOut = 0.3f;
        [Tooltip("Cancel the taunt when the enemy gets an attack slot.")]
        public bool tauntCancelOnAttackSlot = true;

        [Header("Group camera pull-back")]
        public bool enableCameraPullback = true;
        [Tooltip("Ring members (engaged enemies) at which pull-back starts.")]
        [Range(1, 10)] public int pullbackMinEnemies = 3;
        [Tooltip("Ring members at which the full extra distance is reached.")]
        [Range(1, 16)] public int pullbackFullEnemies = 6;
        [Tooltip("Each attack slot in use adds this many 'enemies' of threat.")]
        [Range(0f, 3f)] public float pullbackSlotThreatWeight = 0.5f;
        [Range(0f, 6f)] public float pullbackMaxExtraDistance = 1.6f;
        [Range(0.05f, 3f)] public float pullbackOutSeconds = 0.8f;
        [Range(0.05f, 5f)] public float pullbackReturnSeconds = 1.6f;
        [Tooltip("Hold the pull-back this long after the threat drops before returning.")]
        [Range(0f, 5f)] public float pullbackReturnDelay = 1.2f;
        public bool debugLog = false;

        [Header("Finisher-available cue (brighten + pulse the eligible enemy's red HP fill)")]
        [Tooltip("On = nearest finisher-eligible enemy's health-bar fill brightens and pulses.")]
        public bool enableFinisherCue = true;
        [Tooltip("Bright red the fill leans toward while the cue is active.")]
        public Color finisherCueBrightColor = new Color(1f, 0.55f, 0.42f, 1f);
        [Tooltip("0 = keep the normal red; 1 = full lean to Bright Color; >1 overshoots toward white.")]
        [Range(0f, 2f)] public float finisherCueBrightnessBoost = 0.55f;
        [Tooltip("Pulse frequency in Hz.")]
        [Range(0.2f, 10f)] public float finisherCuePulseSpeed = 2.5f;
        [Tooltip("How hard the pulse swings (0 = steady brighten only, 1 = full throb).")]
        [Range(0f, 1f)] public float finisherCuePulseDepth = 0.35f;
        [Tooltip("How often eligibility is re-probed (Hz). Cheap; nearest eligible only.")]
        [Range(1f, 30f)] public float finisherCueProbeHz = 8f;
        [Tooltip("Reserved; cue is silent by default.")]
        public bool finisherCueSound = false;

        [Header("Finisher-available feel (26k) -- other melee faster; finisher-only bonus damage")]
        [Tooltip("Animator speed multiplier for non-finisher melee while a finisher-eligible target exists (same eligibility as the HP cue). 1 = unchanged.")]
        [Range(1f, 1.5f)] public float finisherAvailableOtherMeleeSpeed = 1.15f;
        [Tooltip("Fraction of the enemy MaxHealth used as the finisher damage base (default 10%). Damage = MaxHealth * this * finisherDamageMultiplier.")]
        [Range(0.01f, 1f)] public float finisherDamageMaxHealthPercent = 0.1f;
        [Tooltip("Multiplier on (MaxHealth * percent). Default 2.5 => 25% of max HP at 10% base. Does NOT force a kill.")]
        [Range(0.5f, 10f)] public float finisherDamageMultiplier = 2.5f;
        [Tooltip("Body-weight swing-gain multiplier while finisher-available other-melee speed is active (matches faster swings).")]
        [Range(1f, 1.5f)] public float finisherAvailableBodySwingMult = 1.12f;

        [Header("Jetpack hazard lights (26k) -- Rad/Red, Heat, Cold, Mix sequential blink")]
        [Tooltip("0 = Off, 1 = Finisher Available (default), 2 = Always. Runtime-only on the player jetpack indicators; environmental ExposureHazardIndicators untouched.")]
        [Range(0, 2)] public int jetpackLightMode = 1;
        [Tooltip("Seconds each light stays on in the Red/Rad -> Heat -> Cold -> Mix sequence.")]
        [Range(0.05f, 2f)] public float jetpackLightStepTime = 0.22f;
        [Tooltip("Emission intensity while a step is on.")]
        [Range(0f, 30f)] public float jetpackLightOnIntensity = 5f;
        [Tooltip("Emission intensity for the other three lights during a step (0 = fully dark).")]
        [Range(0f, 5f)] public float jetpackLightOffIntensity = 0.05f;
        [Tooltip("Dark pause after Mix before the sequence loops.")]
        [Range(0f, 3f)] public float jetpackLightLoopGap = 0.25f;

        /// <summary>0 Subtle, 1 Default, 2 Extreme -- finishers/taunts/camera + 26k feel + jetpack lights.</summary>
        public void ApplyPreset(int level)
        {
            enableFinishers = enableTaunts = enableCameraPullback = true;
            enableFinisherCue = true;
            finisherCueSound = false;
            enableFinisherOnStagger = false;
            finisherEnemyClip = null; // 26i: no paired grab
            switch (level)
            {
                case 0:
                    finisherLowHpFraction = 0.12f; finisherStaggerWindow = 0.8f; finisherRange = 2f; finisherMaxAngle = 55f; finisherPlaySpeed = 1.3f; finisherCooldown = 4f;
                    finisherBodyPitchDeg = 10f; finisherBodyHipDrop = 0.04f;
                    tauntChancePerSecond = 0.06f; tauntMaxConcurrent = 1; tauntPerEnemyCooldown = 14f; tauntGroupCooldown = 5f; tauntMaxSeconds = 2f;
                    pullbackMinEnemies = 4; pullbackFullEnemies = 7; pullbackMaxExtraDistance = 0.9f; pullbackOutSeconds = 1.2f; pullbackReturnSeconds = 2f;
                    finisherCueBrightColor = new Color(1f, 0.42f, 0.36f, 1f); finisherCueBrightnessBoost = 0.3f; finisherCuePulseSpeed = 1.5f; finisherCuePulseDepth = 0.18f; finisherCueProbeHz = 6f;
                    ApplyFinisherFeelPreset(0); ApplyJetpackLightPreset(0); break;
                case 2:
                    finisherLowHpFraction = 0.35f; finisherStaggerWindow = 2f; finisherRange = 3f; finisherMaxAngle = 100f; finisherPlaySpeed = 1f; finisherCooldown = 0.5f;
                    finisherBodyPitchDeg = 22f; finisherBodyHipDrop = 0.1f;
                    tauntChancePerSecond = 0.3f; tauntMaxConcurrent = 3; tauntPerEnemyCooldown = 5f; tauntGroupCooldown = 1f; tauntMaxSeconds = 3.5f;
                    pullbackMinEnemies = 2; pullbackFullEnemies = 5; pullbackMaxExtraDistance = 2.8f; pullbackOutSeconds = 0.5f; pullbackReturnSeconds = 1.2f;
                    finisherCueBrightColor = new Color(1f, 0.72f, 0.55f, 1f); finisherCueBrightnessBoost = 1.1f; finisherCuePulseSpeed = 4.5f; finisherCuePulseDepth = 0.6f; finisherCueProbeHz = 12f;
                    ApplyFinisherFeelPreset(2); ApplyJetpackLightPreset(2); break;
                default:
                    finisherLowHpFraction = 0.2f; finisherStaggerWindow = 1.2f; finisherRange = 2.4f; finisherMaxAngle = 70f; finisherPlaySpeed = 1.15f; finisherCooldown = 2f;
                    finisherBodyPitchDeg = 14f; finisherBodyHipDrop = 0.06f;
                    tauntChancePerSecond = 0.12f; tauntMaxConcurrent = 1; tauntPerEnemyCooldown = 9f; tauntGroupCooldown = 3f; tauntMaxSeconds = 2.6f;
                    pullbackMinEnemies = 3; pullbackFullEnemies = 6; pullbackMaxExtraDistance = 1.6f; pullbackOutSeconds = 0.8f; pullbackReturnSeconds = 1.6f;
                    finisherCueBrightColor = new Color(1f, 0.55f, 0.42f, 1f); finisherCueBrightnessBoost = 0.55f; finisherCuePulseSpeed = 2.5f; finisherCuePulseDepth = 0.35f; finisherCueProbeHz = 8f;
                    ApplyFinisherFeelPreset(1); ApplyJetpackLightPreset(1); break;
            }
        }

        /// <summary>0 Subtle, 1 Default, 2 Extreme -- other-melee speed, finisher damage, body-weight swing.</summary>
        public void ApplyFinisherFeelPreset(int level)
        {
            switch (level)
            {
                case 0:
                    finisherAvailableOtherMeleeSpeed = 1.1f; finisherDamageMaxHealthPercent = 0.08f; finisherDamageMultiplier = 1.5f; finisherAvailableBodySwingMult = 1.05f; break;
                case 2:
                    finisherAvailableOtherMeleeSpeed = 1.35f; finisherDamageMaxHealthPercent = 0.15f; finisherDamageMultiplier = 4f; finisherAvailableBodySwingMult = 1.25f; break;
                default:
                    finisherAvailableOtherMeleeSpeed = 1.15f; finisherDamageMaxHealthPercent = 0.1f; finisherDamageMultiplier = 2.5f; finisherAvailableBodySwingMult = 1.12f; break;
            }
        }

        /// <summary>0 Subtle, 1 Default, 2 Extreme -- jetpack Rad/Heat/Cold/Mix blink.</summary>
        public void ApplyJetpackLightPreset(int level)
        {
            jetpackLightMode = 1;
            switch (level)
            {
                case 0:
                    jetpackLightStepTime = 0.35f; jetpackLightOnIntensity = 2f; jetpackLightOffIntensity = 0f; jetpackLightLoopGap = 0.4f; break;
                case 2:
                    jetpackLightStepTime = 0.12f; jetpackLightOnIntensity = 10f; jetpackLightOffIntensity = 0.1f; jetpackLightLoopGap = 0.1f; break;
                default:
                    jetpackLightStepTime = 0.22f; jetpackLightOnIntensity = 5f; jetpackLightOffIntensity = 0.05f; jetpackLightLoopGap = 0.25f; break;
            }
        }
    }
}

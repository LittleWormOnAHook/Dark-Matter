using System;
using Project.Pioneers;
using UnityEngine;

namespace Project.Survival.Exposure
{
    [Serializable]
    public class ExposureMitigationRule
    {
        [Tooltip("Which pressure this rule reduces.")]
        public ExposurePressureType pressureType = ExposurePressureType.Radiation;

        public ExposureMitigationSource source = ExposureMitigationSource.CompanionClass;

        [Tooltip("Primary class required for class-based rules.")]
        public SkilledPioneerClass requiredClass = SkilledPioneerClass.ScienceSpecialist;

        [Tooltip("Optional second class for trio composition rules.")]
        public SkilledPioneerClass secondaryClass = SkilledPioneerClass.ArchitectEngineer;

        [Tooltip("Passive ability id, assigned skill id, or tool ability id on a companion.")]
        public string requiredAbilityId;

        [Range(0f, 1f)]
        [Tooltip("Fraction of incoming exposure removed (0.35 = 35% reduction).")]
        public float exposureReduction = 0.35f;

        [Tooltip("Optional player-facing label when this mitigation is active.")]
        public string activeLabel = "Crew mitigation active";
    }

    [Serializable]
    public class ExposurePulseSettings
    {
        public bool enabled;
        public float activeDurationSeconds = 8f;
        public float inactiveDurationSeconds = 4f;
        public float activeIntensityMultiplier = 1.5f;

        [Tooltip("Random variance applied to each phase length.")]
        [Range(0f, 0.5f)]
        public float timingJitter = 0.15f;
    }

    [Serializable]
    public class ExposureDebuffSettings
    {
        [Range(0f, 1f)]
        public float moveSpeedPenalty = 0.15f;

        [Range(0f, 1f)]
        public float staminaRegenPenalty = 0.2f;

        [Range(0f, 1f)]
        public float accuracyPenalty = 0.1f;

        [Range(1f, 3f)]
        public float damageTakenMultiplier = 1.15f;

        [Tooltip("Exposure level (0-1) before debuffs begin applying.")]
        [Range(0f, 1f)]
        public float debuffThreshold = 0.35f;
    }
}

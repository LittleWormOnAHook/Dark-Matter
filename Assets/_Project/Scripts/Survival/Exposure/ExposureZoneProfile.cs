using UnityEngine;

namespace Project.Survival.Exposure
{
    /// <summary>
    /// Data-driven pressure recipe for an exposure volume. O2 norm always applies globally;
    /// zones can increase oxygen drain and stack rad / thermal / sulfur / volcano stress.
    /// </summary>
    [CreateAssetMenu(
        fileName = "exposure_zone_profile",
        menuName = "Dark Matter Genesis/Survival/Exposure Zone Profile")]
    public class ExposureZoneProfile : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Exposure Zone";
        public ExposureZoneKind zoneKind = ExposureZoneKind.Custom;

        [TextArea(2, 4)]
        public string designerNotes;

        [Header("Intensity")]
        [Range(0.1f, 1f)]
        [Tooltip("Overall effect intensity for this zone, 10%-100%. Scales all pressure rates and health drain together so you can soften/harden a zone without re-tuning every rate individually. Oxygen drain multiplier is blended toward 1 (no effect) as this drops.")]
        public float effectIntensity = 1f;

        [Header("Pressure Rates (per second while inside)")]
        [Tooltip("Radiation exposure buildup (0-100 scale).")]
        public float radiationPerSecond;

        [Tooltip("Pushes thermal stress toward cold (negative).")]
        public float thermalColdPerSecond;

        [Tooltip("Pushes thermal stress toward heat (positive).")]
        public float thermalHeatPerSecond;

        public float sulfurPerSecond;
        public float volcanoPerSecond;

        [Tooltip("Multiplies global oxygen drain while inside. 1 = unchanged.")]
        public float oxygenDrainMultiplier = 1f;

        [Header("Recovery Overrides")]
        [Tooltip("When <= 0, uses SurvivalStats defaults.")]
        public float exposureRecoveryPerSecond = -1f;

        public float thermalRecoveryPerSecond = -1f;

        [Header("Damage")]
        [Tooltip("Bonus health drain per second at max stacked exposure (rad+sulfur+volcano average).")]
        public float healthDrainAtMaxExposure = 2f;

        [Tooltip("Bonus health drain per second at max thermal magnitude.")]
        public float healthDrainAtMaxThermal = 1.5f;

        [Header("Timing")]
        public ExposurePulseSettings pulse = new ExposurePulseSettings();

        [Header("Mitigation")]
        public ExposureMitigationRule[] mitigationRules;

        [Header("Debuffs")]
        public ExposureDebuffSettings playerDebuffs = new ExposureDebuffSettings();
        public ExposureDebuffSettings companionDebuffs = new ExposureDebuffSettings();

        [Header("Presentation")]
        public Color gizmoColor = new Color(0.79f, 0.18f, 0.48f, 0.45f);
        public GameObject ambientVfxPrefab;
        public AudioClip ambientLoopClip;

        public ExposureSample BuildSample(float pulseMultiplier)
        {
            return BuildSample(pulseMultiplier, 1f);
        }

        public ExposureSample BuildSample(float pulseMultiplier, float spatial01)
        {
            float pulse = Mathf.Max(0f, pulseMultiplier);
            float spatial = Mathf.Clamp01(spatial01);
            float intensity = Mathf.Clamp(effectIntensity * spatial, 0f, 1f);

            // Rates stay at their authored speed — effectIntensity / spatial falloff
            // cap how high the hazard is allowed to settle (see ceiling fields below).
            return new ExposureSample
            {
                radiationPerSecond = radiationPerSecond * pulse,
                thermalColdPerSecond = thermalColdPerSecond * pulse,
                thermalHeatPerSecond = thermalHeatPerSecond * pulse,
                sulfurPerSecond = sulfurPerSecond * pulse,
                volcanoPerSecond = volcanoPerSecond * pulse,
                oxygenDrainMultiplier = Mathf.Lerp(1f, oxygenDrainMultiplier, intensity),
                healthDrainAtMaxExposure = healthDrainAtMaxExposure,
                healthDrainAtMaxThermal = healthDrainAtMaxThermal,
                exposureRecoveryPerSecond = exposureRecoveryPerSecond,
                thermalRecoveryPerSecond = thermalRecoveryPerSecond,
                playerDebuffs = playerDebuffs,
                companionDebuffs = companionDebuffs,
                radiationCeiling01 = radiationPerSecond > 0f ? Mathf.Max(0.01f, intensity) : 1f,
                thermalCeiling01 = (thermalColdPerSecond > 0f || thermalHeatPerSecond > 0f) ? Mathf.Max(0.01f, intensity) : 1f,
                sulfurCeiling01 = sulfurPerSecond > 0f ? Mathf.Max(0.01f, intensity) : 1f,
                volcanoCeiling01 = volcanoPerSecond > 0f ? Mathf.Max(0.01f, intensity) : 1f
            };
        }
    }

    public struct ExposureSample
    {
        public float radiationPerSecond;
        public float thermalColdPerSecond;
        public float thermalHeatPerSecond;
        public float sulfurPerSecond;
        public float volcanoPerSecond;
        public float oxygenDrainMultiplier;
        public float healthDrainAtMaxExposure;
        public float healthDrainAtMaxThermal;
        public float exposureRecoveryPerSecond;
        public float thermalRecoveryPerSecond;
        public ExposureDebuffSettings playerDebuffs;
        public ExposureDebuffSettings companionDebuffs;

        /// <summary>
        /// Normalized (0.1-1) ceiling that this zone's effectIntensity allows its driven hazard
        /// channel(s) to settle at. 1 = no cap. Only meaningful when this zone actually drives
        /// the matching *PerSecond rate above zero — see ExposureZoneProfile.BuildSample.
        /// </summary>
        public float radiationCeiling01;
        public float thermalCeiling01;
        public float sulfurCeiling01;
        public float volcanoCeiling01;
    }
}

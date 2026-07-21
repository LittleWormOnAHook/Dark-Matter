using Project.Pioneers;
using Project.Survival.Exposure;
using UnityEngine;

namespace Project.Survival.Exposure
{
    /// <summary>
    /// Default exposure zone recipes for editor prefab creation.
    /// </summary>
    public static class ExposureZoneProfilePresets
    {
        public static ExposureZoneProfile CreatePreset(ExposureZoneKind kind)
        {
            ExposureZoneProfile profile = ScriptableObject.CreateInstance<ExposureZoneProfile>();
            profile.zoneKind = kind;

            switch (kind)
            {
                case ExposureZoneKind.RadiationFlat:
                    profile.displayName = "Radiation Flat";
                    profile.radiationPerSecond = 6f;
                    profile.healthDrainAtMaxExposure = 2.5f;
                    profile.gizmoColor = new Color(0.85f, 0.78f, 0.15f, 0.4f);
                    profile.mitigationRules = new[]
                    {
                        new ExposureMitigationRule
                        {
                            pressureType = ExposurePressureType.Radiation,
                            source = ExposureMitigationSource.CompanionClass,
                            requiredClass = SkilledPioneerClass.ScienceSpecialist,
                            exposureReduction = 0.35f,
                            activeLabel = "Science shielding active"
                        },
                        new ExposureMitigationRule
                        {
                            pressureType = ExposurePressureType.Radiation,
                            source = ExposureMitigationSource.CompanionAbility,
                            requiredAbilityId = "rad_hardening",
                            exposureReduction = 0.25f,
                            activeLabel = "Rad hardening active"
                        }
                    };
                    break;

                case ExposureZoneKind.ThermalCold:
                    profile.displayName = "Cold Basin";
                    profile.thermalColdPerSecond = 8f;
                    profile.healthDrainAtMaxThermal = 1.8f;
                    profile.gizmoColor = new Color(0.35f, 0.72f, 0.95f, 0.4f);
                    profile.mitigationRules = new[]
                    {
                        new ExposureMitigationRule
                        {
                            pressureType = ExposurePressureType.ThermalCold,
                            source = ExposureMitigationSource.CompanionClass,
                            requiredClass = SkilledPioneerClass.ArchitectEngineer,
                            exposureReduction = 0.3f,
                            activeLabel = "Thermal shelter rig active"
                        }
                    };
                    break;

                case ExposureZoneKind.ThermalHeat:
                    profile.displayName = "Heat Vent";
                    profile.thermalHeatPerSecond = 9f;
                    profile.healthDrainAtMaxThermal = 2.2f;
                    profile.gizmoColor = new Color(0.95f, 0.35f, 0.12f, 0.4f);
                    profile.pulse = new ExposurePulseSettings
                    {
                        enabled = true,
                        activeDurationSeconds = 6f,
                        inactiveDurationSeconds = 3f,
                        activeIntensityMultiplier = 1.6f
                    };
                    profile.mitigationRules = new[]
                    {
                        new ExposureMitigationRule
                        {
                            pressureType = ExposurePressureType.ThermalHeat,
                            source = ExposureMitigationSource.TrioComposition,
                            requiredClass = SkilledPioneerClass.ArchitectEngineer,
                            secondaryClass = SkilledPioneerClass.ScienceSpecialist,
                            exposureReduction = 0.4f,
                            activeLabel = "Engineer + Science thermal buffer"
                        }
                    };
                    break;

                case ExposureZoneKind.SulfurField:
                    profile.displayName = "Sulfur Field";
                    profile.sulfurPerSecond = 7f;
                    profile.oxygenDrainMultiplier = 1.35f;
                    profile.healthDrainAtMaxExposure = 2f;
                    profile.gizmoColor = new Color(0.79f, 0.18f, 0.48f, 0.4f);
                    profile.pulse = new ExposurePulseSettings
                    {
                        enabled = true,
                        activeDurationSeconds = 10f,
                        inactiveDurationSeconds = 5f,
                        activeIntensityMultiplier = 1.45f
                    };
                    profile.mitigationRules = new[]
                    {
                        new ExposureMitigationRule
                        {
                            pressureType = ExposurePressureType.Sulfur,
                            source = ExposureMitigationSource.CompanionAbility,
                            requiredAbilityId = "sulfur_filter",
                            exposureReduction = 0.45f,
                            activeLabel = "Sulfur filters online"
                        }
                    };
                    break;

                case ExposureZoneKind.VolcanoCaldera:
                    profile.displayName = "Volcano Caldera";
                    profile.volcanoPerSecond = 8f;
                    profile.thermalHeatPerSecond = 4f;
                    profile.healthDrainAtMaxExposure = 3f;
                    profile.healthDrainAtMaxThermal = 2.5f;
                    profile.gizmoColor = new Color(0.92f, 0.22f, 0.08f, 0.45f);
                    profile.mitigationRules = new[]
                    {
                        new ExposureMitigationRule
                        {
                            pressureType = ExposurePressureType.Volcano,
                            source = ExposureMitigationSource.CompanionClass,
                            requiredClass = SkilledPioneerClass.CombatTactician,
                            exposureReduction = 0.2f,
                            activeLabel = "Tactician hazard routing"
                        },
                        new ExposureMitigationRule
                        {
                            pressureType = ExposurePressureType.Volcano,
                            source = ExposureMitigationSource.FullTrioClass,
                            requiredClass = SkilledPioneerClass.InfiltratorScout,
                            exposureReduction = 0.15f,
                            activeLabel = "Scout pathfinding bonus"
                        }
                    };
                    break;

                case ExposureZoneKind.MixedHazard:
                    profile.displayName = "Mixed Hazard";
                    profile.radiationPerSecond = 3f;
                    profile.sulfurPerSecond = 3f;
                    profile.thermalHeatPerSecond = 3f;
                    profile.healthDrainAtMaxExposure = 2.5f;
                    profile.gizmoColor = new Color(0.55f, 0.2f, 0.65f, 0.4f);
                    break;

                case ExposureZoneKind.ShelterSafe:
                    profile.displayName = "Shelter Safe Zone";
                    profile.exposureRecoveryPerSecond = 18f;
                    profile.thermalRecoveryPerSecond = 20f;
                    profile.oxygenDrainMultiplier = 0.75f;
                    profile.gizmoColor = new Color(0.35f, 0.85f, 0.45f, 0.35f);
                    break;

                default:
                    profile.displayName = "Custom Exposure Zone";
                    break;
            }

            return profile;
        }
    }
}

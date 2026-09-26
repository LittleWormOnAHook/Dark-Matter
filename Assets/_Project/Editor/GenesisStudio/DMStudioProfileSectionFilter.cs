#if UNITY_EDITOR
namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Which serialized fields to show when editing a shared profile asset in Genesis Studio.
    /// </summary>
    public enum DMStudioProfileSectionFilter
    {
        None = 0,
        ClimbOnly = 1,
        DashOnly = 2,
        SurvivalOnly = 3,
        LocomotionOnly = 4,
        FootstepsAudioOnly = 5,
        CombatAmmoOnly = 6,
        LandingHeightsOnly = 7,
        BuildingPreviewOnly = 8,
        BuildingSnapOnly = 9,
        BuildingPlacementOnly = 10,
        BuildingDoorOnly = 11,
        BuildingBuiltTintsOnly = 12
    }

    internal static class DMStudioProfileSections
    {
        private static readonly string[] FootstepsAudioFields =
        {
            "defaultFootsteps",
            "surfaceFootsteps",
            "terrainLayerFootsteps"
        };

        private static readonly string[] LocomotionFields =
        {
            "slowWalkSpeedMultiplier",
            "jogSpeedMultiplier",
            "sprintBurstSpeedMultiplier",
            "shiftDoubleTapWindow"
        };

        private static readonly string[] LandingHeightFields =
        {
            "heroDropMeters",
            "lethalDropMeters",
            "fallDamageStartMeters",
            "fallDamageLethalPercent",
            "fallDamageHealthFraction",
            "jetpackLethalDelay"
        };

        private static readonly string[] BuildingPreviewFields =
        {
            "validGhostMaterial",
            "ghostColor",
            "ghostAlpha",
            "blockedGhostMaterial",
            "blockedGhostColor",
            "blockedGhostAlpha"
        };

        private static readonly string[] BuildingBuiltTintFields =
        {
            "builtMaterial",
            "finishedColor",
            "glassColor",
            "glassAlpha"
        };

        private static readonly string[] BuildingSnapFields =
        {
            "largeModuleMeters",
            "smallModuleMeters",
            "yawStepDegrees",
            "heightStepMeters",
            "maxHeightOffsetMeters",
            "edgeSnapRangeMeters",
            "topSnapRangeMeters",
            "buildLookUpDegrees",
            "buildModeCameraDistanceMultiplier",
            "buildModeCameraExtraMeters",
            "doorFrameRangeMeters",
            "edgeFacingDot"
        };

        private static readonly string[] BuildingPlacementFields =
        {
            "buildSeconds",
            "destroyHoldSeconds",
            "aimDistanceMeters",
            "doorSeatDropMeters",
            "overlapPaddingMeters"
        };

        private static readonly string[] BuildingDoorFields =
        {
            "doorSwingDegrees",
            "doorSwingSeconds",
            "doorInteractRangeMeters",
            "gateSwingDegrees",
            "gateSwingSeconds"
        };

        private static readonly string[] SurvivalFields =
        {
            "maxHealth",
            "maxEnergy",
            "maxStamina",
            "maxOxygen",
            "energyDrainPerSecond",
            "toolEnergyDrainPerSecond",
            "handCraftEnergyPerItem",
            "oxygenDrainPerSecond",
            "lowOxygenWarningPercent",
            "lowOxygenFlashPerSecond",
            "lowOxygenWarningText",
            "lowOxygenWarningEnabled",
            "healthDrainPerSecond",
            "lowStatThresholdPercent",
            "oxygenDepletedHealthDrainMultiplier",
            "enableHealthRegen",
            "healthRegenPerSecond",
            "healthRegenDelayAfterDamage",
            "maxThermalStress",
            "maxRadiation",
            "maxSulfur",
            "maxVolcano",
            "exposureRecoveryPerSecond",
            "thermalRecoveryPerSecond"
        };

        private static readonly string[] CombatAmmoFields =
        {
            "itemName",
            "ammoType",
            "tooltipDescription",
            "rangedDamage",
            "rangedDamageRandomRange",
            "rangedRange",
            "projectileSpeed",
            "projectileSpreadDegrees",
            "weaponAccuracy",
            "closeRangeFullAccuracyDistance",
            "closeRangeSpreadScale",
            "fireRate",
            "shotsPerBurst",
            "burstFireRate",
            "magazineSize",
            "reloadTimeSeconds",
            "recoilVertical",
            "recoilHorizontal",
            "recoilFireRateScale",
            "ammoRecoilProfile",
            "isHitscanBeam",
            "isContinuousLaser",
            "projectileGravityScale",
            "splashRadius",
            "splashDamageFalloff",
            "statusEffectOverride",
            "statusEffectDamagePerTick",
            "statusEffectTickInterval",
            "statusEffectDuration",
            "statusEffectVfxPrefab",
            "projectilePrefab",
            "muzzleFlashPrefab",
            "tracerPrefab",
            "impactVfxPrefab",
            "beamVfxPrefab",
            "fireSound",
            "projectileTravelSound",
            "continuousLoopSound",
            "continuousStartSound",
            "continuousStopSound",
            "spawnLaserBurn",
            "useHitMarks",
            "defaultDecals",
            "defaultHitEffects",
            "surfaces",
            "fallBackToCatalog",
            "catalog"
        };

        private static readonly string[] DashFields =
        {
            "dashStaminaCost",
            "dashStaminaTickExtraPercent",
            "dashStaminaRegenPerSecond",
            "dashRecoveryStaminaRegenPerSecond",
            "dashStaminaRecoverySeconds",
            "dashDoubleTapWindow",
            "dashDistance",
            "dashSpeed",
            "dashDuration",
            "dashCooldown",
            "dashAllowAirDash",
            "dashAnimationSpeed",
            "dashCollisionSkin",
            "dashDetachesFromClimb",
            "dashAllowedWhileClimbing",
            "dashHologramColor",
            "dashHologramEmission",
            "dashHologramMaterial",
            "dashStreakColor",
            "dashStreakCount",
            "dashStreakLifetime",
            "dashStreakSize",
            "dashStreakStretch",
            "dashStreakRadius",
            "dashStreakMaterial",
            "dashStreakPrefab",
            "dashSmokeColor",
            "dashSmokeCount",
            "dashSmokeLifetime",
            "dashSmokeSize",
            "dashSmokeMaterial",
            "dashSmokePrefab"
        };

        public static bool IncludesField(string propertyPath, DMStudioProfileSectionFilter filter)
        {
            if (filter == DMStudioProfileSectionFilter.None || string.IsNullOrEmpty(propertyPath))
                return true;

            if (propertyPath == "m_Script")
                return true;

            return filter switch
            {
                DMStudioProfileSectionFilter.SurvivalOnly => IsSurvivalField(propertyPath),
                DMStudioProfileSectionFilter.DashOnly => IsDashField(propertyPath),
                DMStudioProfileSectionFilter.LocomotionOnly => IsLocomotionField(propertyPath),
                DMStudioProfileSectionFilter.ClimbOnly =>
                    !IsSurvivalField(propertyPath)
                    && !IsDashField(propertyPath)
                    && !IsLocomotionField(propertyPath)
                    && !IsLandingHeightField(propertyPath),
                DMStudioProfileSectionFilter.FootstepsAudioOnly => IsFootstepsAudioField(propertyPath),
                DMStudioProfileSectionFilter.CombatAmmoOnly => IsCombatAmmoField(propertyPath),
                DMStudioProfileSectionFilter.LandingHeightsOnly => IsLandingHeightField(propertyPath),
                DMStudioProfileSectionFilter.BuildingPreviewOnly => IsBuildingPreviewField(propertyPath),
                DMStudioProfileSectionFilter.BuildingBuiltTintsOnly => IsBuildingBuiltTintField(propertyPath),
                DMStudioProfileSectionFilter.BuildingSnapOnly => IsBuildingSnapField(propertyPath),
                DMStudioProfileSectionFilter.BuildingPlacementOnly => IsBuildingPlacementField(propertyPath),
                DMStudioProfileSectionFilter.BuildingDoorOnly => IsBuildingDoorField(propertyPath),
                _ => true
            };
        }

        public static string GetSectionNote(DMStudioProfileSectionFilter filter)
        {
            return filter switch
            {
                DMStudioProfileSectionFilter.SurvivalOnly =>
                    "Survival fields on DM_ClimbDashProfile — same asset as Climb and Dash tabs; Play-mode edits persist via Profile Save.",
                DMStudioProfileSectionFilter.DashOnly =>
                    "Dash motion, stamina cost, hologram, streaks, and smoke on DM_ClimbDashProfile — climb tuning lives under Climb.",
                DMStudioProfileSectionFilter.ClimbOnly =>
                    "Wall attach, mantle, climb stamina, sprint stamina, and surface probes on DM_ClimbDashProfile — dash, survival, and locomotion have their own tabs.",
                DMStudioProfileSectionFilter.LocomotionOnly =>
                    "On-foot gaits on DM_ClimbDashProfile — slow walk, Shift jog, and double-tap Shift sprint burst. Play-mode edits persist via Profile Save.",
                DMStudioProfileSectionFilter.FootstepsAudioOnly =>
                    "Default fallback, Unity-tag, and terrain-layer 0-10 clip libraries on GameAudioProfile.",
                DMStudioProfileSectionFilter.CombatAmmoOnly =>
                    "Live ammo combat fields (Play Mode edits push to the drawn weapon each tick). Fire Rate / burst / reload / mag on the loaded ammo profile win when greater than zero; else the weapon ItemData is used. Recoil Vertical/Horizontal are camera kick; rifle column on Ammo Recoil Profile still overrides two-hand weapons. Invector weapon recoilUp does nothing.",
                DMStudioProfileSectionFilter.LandingHeightsOnly =>
                    "Jump/Landing 3-tier height band on DM_ClimbDashProfile (bounce / hero / hero+damage + jetpack grace) - same asset as Climb/Dash; Play-mode edits persist via Profile Save.",
                DMStudioProfileSectionFilter.BuildingPreviewOnly =>
                    "Valid snap/build and blocked preview holograms on DM_BuildingGhostProfile — optional materials plus color and alpha.",
                DMStudioProfileSectionFilter.BuildingBuiltTintsOnly =>
                    "Finished mesh and window glass tints on DM_BuildingGhostProfile. Finishes (M key) live per style in Building > Library — use Building Studio.",
                DMStudioProfileSectionFilter.BuildingSnapOnly =>
                    "Module grid, yaw/height steps, edge/top snap, and build look-up on DM_BuildingGhostProfile.",
                DMStudioProfileSectionFilter.BuildingPlacementOnly =>
                    "Hold to build/destroy, aim distance, overlap padding, and door frame seat on DM_BuildingGhostProfile.",
                DMStudioProfileSectionFilter.BuildingDoorOnly =>
                    "Built stone door swing and interact range on DM_BuildingGhostProfile.",
                _ => string.Empty
            };
        }

        private static bool IsFootstepsAudioField(string propertyPath)
        {
            for (int i = 0; i < FootstepsAudioFields.Length; i++)
            {
                if (propertyPath == FootstepsAudioFields[i])
                    return true;
            }

            return false;
        }

        private static bool IsLandingHeightField(string propertyPath)
        {
            for (int i = 0; i < LandingHeightFields.Length; i++)
            {
                if (propertyPath == LandingHeightFields[i])
                    return true;
            }

            return false;
        }

        private static bool IsLocomotionField(string propertyPath)
        {
            for (int i = 0; i < LocomotionFields.Length; i++)
            {
                if (propertyPath == LocomotionFields[i])
                    return true;
            }

            return false;
        }

        private static bool IsSurvivalField(string propertyPath)
        {
            for (int i = 0; i < SurvivalFields.Length; i++)
            {
                if (propertyPath == SurvivalFields[i])
                    return true;
            }

            return false;
        }

        private static bool IsCombatAmmoField(string propertyPath)
        {
            return MatchesField(propertyPath, CombatAmmoFields);
        }

        private static bool MatchesField(string propertyPath, string[] fields)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                string field = fields[i];
                if (propertyPath == field || propertyPath.StartsWith(field + ".", System.StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static bool IsDashField(string propertyPath)
        {
            for (int i = 0; i < DashFields.Length; i++)
            {
                if (propertyPath == DashFields[i])
                    return true;
            }

            return false;
        }

        private static bool IsBuildingPreviewField(string propertyPath) => MatchesField(propertyPath, BuildingPreviewFields);

        private static bool IsBuildingBuiltTintField(string propertyPath) => MatchesField(propertyPath, BuildingBuiltTintFields);

        private static bool IsBuildingSnapField(string propertyPath) => MatchesField(propertyPath, BuildingSnapFields);

        private static bool IsBuildingPlacementField(string propertyPath) => MatchesField(propertyPath, BuildingPlacementFields);

        private static bool IsBuildingDoorField(string propertyPath) => MatchesField(propertyPath, BuildingDoorFields);
    }
}
#endif

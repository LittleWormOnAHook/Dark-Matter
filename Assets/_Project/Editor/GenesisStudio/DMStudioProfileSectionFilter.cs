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
        SurvivalOnly = 3
    }

    internal static class DMStudioProfileSections
    {
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
            "thermalRecoveryPerSecond",
            "heroDropMeters",
            "lethalDropMeters",
            "fallDamageStartMeters",
            "fallDamageLethalPercent",
            "fallDamageHealthFraction",
            "jetpackLethalDelay"
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
                DMStudioProfileSectionFilter.ClimbOnly => !IsSurvivalField(propertyPath) && !IsDashField(propertyPath),
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
                    "Wall attach, mantle, climb stamina, sprint stamina, and surface probes on DM_ClimbDashProfile — dash and survival have their own tabs.",
                _ => string.Empty
            };
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

        private static bool IsDashField(string propertyPath)
        {
            for (int i = 0; i < DashFields.Length; i++)
            {
                if (propertyPath == DashFields[i])
                    return true;
            }

            return false;
        }
    }
}
#endif

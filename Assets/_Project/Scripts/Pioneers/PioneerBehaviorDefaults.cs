using UnityEngine;

namespace Project.Pioneers
{
    public static class PioneerBehaviorDefaults
    {
        public static PioneerBehaviorProfile CreateForClass(SkilledPioneerClass pioneerClass)
        {
            PioneerBehaviorProfile profile = new PioneerBehaviorProfile();

            switch (pioneerClass)
            {
                case SkilledPioneerClass.CombatTactician:
                    profile.followMode = PioneerFollowMode.DefendPlayer;
                    profile.combatTetherRadius = 5.5f;
                    profile.preferredCombatDistance = 2.2f;
                    break;

                case SkilledPioneerClass.InfiltratorScout:
                    profile.followMode = PioneerFollowMode.FollowSelf;
                    profile.combatTetherRadius = 8.5f;
                    profile.rangedPreferredDistance = 7.5f;
                    profile.preferredCombatDistance = 6.5f;
                    break;

                case SkilledPioneerClass.ScienceSpecialist:
                    profile.followMode = PioneerFollowMode.FollowPlayer;
                    profile.combatTetherRadius = 7f;
                    profile.rangedPreferredDistance = 6.5f;
                    profile.preferredCombatDistance = 5f;
                    break;

                case SkilledPioneerClass.ArchitectEngineer:
                    profile.followMode = PioneerFollowMode.FollowPlayer;
                    profile.combatTetherRadius = 6f;
                    profile.preferredCombatDistance = 3f;
                    break;

                case SkilledPioneerClass.IoHybrid:
                    profile.followMode = PioneerFollowMode.FollowSelf;
                    profile.combatTetherRadius = 7f;
                    break;

                case SkilledPioneerClass.MedTech:
                    profile.followMode = PioneerFollowMode.FollowPlayer;
                    profile.combatTetherRadius = 6.5f;
                    profile.preferredCombatDistance = 4.5f;
                    profile.rangedPreferredDistance = 5.5f;
                    break;

                case SkilledPioneerClass.LogisticsOfficer:
                    profile.followMode = PioneerFollowMode.FollowPlayer;
                    profile.combatTetherRadius = 7f;
                    profile.preferredCombatDistance = 5f;
                    profile.worldIdleJob = PioneerWorldIdleJob.None;
                    break;

                case SkilledPioneerClass.SalvageEngineer:
                    profile.followMode = PioneerFollowMode.FollowSelf;
                    profile.combatTetherRadius = 6.5f;
                    profile.preferredCombatDistance = 3.5f;
                    break;

                case SkilledPioneerClass.CommunicationsOfficer:
                    profile.followMode = PioneerFollowMode.FollowPlayer;
                    profile.combatTetherRadius = 7.5f;
                    profile.rangedPreferredDistance = 6f;
                    profile.preferredCombatDistance = 5.5f;
                    break;
            }

            return profile;
        }

        public static void MergeDefinitionOverrides(PioneerBehaviorProfile target, NamedPioneerDefinition definition)
        {
            if (target == null || definition == null || definition.behavior == null)
                return;

            PioneerBehaviorProfile source = definition.behavior;
            if (definition.overrideDefaultFollowMode)
                target.followMode = source.followMode;

            OverlayPositiveFields(target, source);

            if (definition.overrideDefaultWorldAmbientMode)
                target.worldAmbientMode = source.worldAmbientMode;

            if (source.worldIdleJob != PioneerWorldIdleJob.None)
                target.worldIdleJob = source.worldIdleJob;
        }

        public static void OverlayPositiveFields(PioneerBehaviorProfile target, PioneerBehaviorProfile source)
        {
            if (target == null || source == null)
                return;

            if (source.wanderPaceScale > 0.09f)
                target.wanderPaceScale = source.wanderPaceScale;
            if (source.walkSpeed > 0.1f)
                target.walkSpeed = source.walkSpeed;
            if (source.runSpeed > 0.1f)
                target.runSpeed = source.runSpeed;
            if (source.catchUpSpeed > 0.1f)
                target.catchUpSpeed = source.catchUpSpeed;
            if (source.catchUpDistance > 0.1f)
                target.catchUpDistance = source.catchUpDistance;
            if (source.maxFollowDistance > 0.1f)
                target.maxFollowDistance = source.maxFollowDistance;
            if (source.stopDistance > 0.01f)
                target.stopDistance = source.stopDistance;
            if (source.formationHeadingSmoothTime > 0.01f)
                target.formationHeadingSmoothTime = source.formationHeadingSmoothTime;
            if (source.walkAnimationSpeed > 0.01f)
                target.walkAnimationSpeed = source.walkAnimationSpeed;
            if (source.runAnimationSpeed > 0.01f)
                target.runAnimationSpeed = source.runAnimationSpeed;
            if (source.walkSpeedReference > 0.1f)
                target.walkSpeedReference = source.walkSpeedReference;
            if (source.runSpeedReference > 0.1f)
                target.runSpeedReference = source.runSpeedReference;
            if (source.combatTetherRadius > 0.1f)
                target.combatTetherRadius = source.combatTetherRadius;
            if (source.preferredCombatDistance > 0.1f)
                target.preferredCombatDistance = source.preferredCombatDistance;
            if (source.rangedPreferredDistance > 0.1f)
                target.rangedPreferredDistance = source.rangedPreferredDistance;
            if (source.losSearchRadius > 0.01f)
                target.losSearchRadius = source.losSearchRadius;
            if (source.formationDriftDegreesPerSecond > 0.01f)
                target.formationDriftDegreesPerSecond = source.formationDriftDegreesPerSecond;
        }

        public static bool HasMissingNumericFields(PioneerBehaviorProfile profile)
        {
            if (profile == null)
                return true;

            return profile.wanderPaceScale <= 0.09f
                || profile.walkSpeed <= 0.1f
                || profile.runSpeed <= 0.1f
                || profile.catchUpSpeed <= 0.1f
                || profile.catchUpDistance <= 0.1f
                || profile.maxFollowDistance <= 0.1f
                || profile.stopDistance <= 0.01f
                || profile.formationHeadingSmoothTime <= 0.01f
                || profile.walkAnimationSpeed <= 0.01f
                || profile.runAnimationSpeed <= 0.01f
                || profile.walkSpeedReference <= 0.1f
                || profile.runSpeedReference <= 0.1f
                || profile.combatTetherRadius <= 0.1f
                || profile.preferredCombatDistance <= 0.1f
                || profile.rangedPreferredDistance <= 0.1f
                || profile.losSearchRadius <= 0.01f
                || profile.formationDriftDegreesPerSecond <= 0.01f;
        }

        public static void FillMissingNumericFields(PioneerBehaviorProfile target, SkilledPioneerClass pioneerClass)
        {
            if (target == null)
                return;

            PioneerBehaviorProfile defaults = CreateForClass(pioneerClass);
            PioneerBehaviorProfile filled = defaults.Clone();
            OverlayPositiveFields(filled, target);
            OverlayPositiveFields(target, filled);
        }

        public static PioneerBehaviorProfile ResolveForRecord(SkilledPioneerRecord record)
        {
            if (record == null)
                return new PioneerBehaviorProfile();

            PioneerBehaviorProfile profile = CreateForClass(record.pioneerClass);
            OverlayPositiveFields(profile, record.behavior);

            if (record.followMode >= 0)
                profile.followMode = (PioneerFollowMode)Mathf.Clamp(record.followMode, 0, 2);

            NamedPioneerDefinition definition = NamedPioneerCatalog.FindByDisplayName(record.displayName);
            if (definition == null && !string.IsNullOrEmpty(record.displayName))
                definition = NamedPioneerCatalog.FindById(record.id);

            MergeDefinitionOverrides(profile, definition);
            FillMissingNumericFields(profile, record.pioneerClass);
            return profile;
        }
    }
}

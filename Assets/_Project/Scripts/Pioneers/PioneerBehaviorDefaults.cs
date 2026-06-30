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

            if (source.wanderPaceScale > 0.09f)
                target.wanderPaceScale = source.wanderPaceScale;
            if (source.walkSpeed > 0.1f)
                target.walkSpeed = source.walkSpeed;
            if (source.runSpeed > 0.1f)
                target.runSpeed = source.runSpeed;
            if (source.catchUpSpeed > 0.1f)
                target.catchUpSpeed = source.catchUpSpeed;
            if (source.combatTetherRadius > 0.1f)
                target.combatTetherRadius = source.combatTetherRadius;
            if (source.preferredCombatDistance > 0.1f)
                target.preferredCombatDistance = source.preferredCombatDistance;
            if (source.rangedPreferredDistance > 0.1f)
                target.rangedPreferredDistance = source.rangedPreferredDistance;
        }

        public static PioneerBehaviorProfile ResolveForRecord(SkilledPioneerRecord record)
        {
            if (record == null)
                return new PioneerBehaviorProfile();

            PioneerBehaviorProfile profile = record.behavior != null
                ? record.behavior.Clone()
                : CreateForClass(record.pioneerClass);

            if (record.followMode >= 0)
                profile.followMode = (PioneerFollowMode)Mathf.Clamp(record.followMode, 0, 2);

            NamedPioneerDefinition definition = NamedPioneerCatalog.FindByDisplayName(record.displayName);
            if (definition == null && !string.IsNullOrEmpty(record.displayName))
                definition = NamedPioneerCatalog.FindById(record.id);

            MergeDefinitionOverrides(profile, definition);
            return profile;
        }
    }
}

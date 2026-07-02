using System;
using UnityEngine;

namespace Project.Pioneers
{
    [Serializable]
    public class SkilledPioneerRecord
    {
        public string id;
        public string displayName;
        public SkilledPioneerClass pioneerClass;
        public int level = 1;
        public float radiationResistance = 0.5f;
        public float expeditionEfficiency = 0.5f;
        public float combatSynergy = 0.5f;
        public string backstory;
        public bool isStarterPick;
        public int kind;
        public int disposition;
        public float saturation;
        public string[] traitIds;
        public string[] passiveAbilityIds;
        public string[] learnedSkills;
        public string weaponItemId;
        public string toolItemId;
        public string[] assignedSkillIds;
        public bool isInExpeditionTrio;
        public int workState;
        public float injuryRecoveryRemaining;
        public int followMode = -1;
        public PioneerBehaviorProfile behavior;

        public PioneerKind Kind
        {
            get => (PioneerKind)Mathf.Clamp(kind, 0, 2);
            set => kind = (int)value;
        }

        public EchoDisposition Disposition
        {
            get => (EchoDisposition)Mathf.Clamp(disposition, 0, 3);
            set => disposition = (int)value;
        }

        public PioneerWorkState WorkState
        {
            get => (PioneerWorkState)Mathf.Clamp(workState, 0, 3);
            set => workState = (int)value;
        }

        public static SkilledPioneerRecord CreateFromStarter(StarterPioneerOffer offer)
        {
            if (offer == null)
                return null;

            var record = new SkilledPioneerRecord
            {
                id = Guid.NewGuid().ToString("N"),
                displayName = offer.displayName,
                pioneerClass = offer.pioneerClass,
                level = 1,
                radiationResistance = offer.radiationResistance,
                expeditionEfficiency = offer.expeditionEfficiency,
                combatSynergy = offer.combatSynergy,
                backstory = offer.backstory,
                isStarterPick = true,
                Kind = PioneerKind.NamedCatalog,
                Disposition = EchoDisposition.Synced,
                saturation = 0.15f,
                traitIds = offer.traitIds,
                passiveAbilityIds = offer.passiveAbilityIds,
                learnedSkills = offer.learnedSkills,
                WorkState = PioneerWorkState.Idle
            };

            record.followMode = -1;
            PioneerLoadoutDefaults.EnsureDefaults(record);
            return record;
        }

        public static SkilledPioneerRecord CreateFromCatalog(NamedPioneerDefinition definition, bool applyLoadoutDefaults = true)
        {
            if (definition == null)
                return null;

            var record = new SkilledPioneerRecord
            {
                id = definition.ResolvedId,
                displayName = definition.displayName,
                pioneerClass = definition.pioneerClass,
                level = definition.startLevel,
                radiationResistance = definition.radiationResistance,
                expeditionEfficiency = definition.expeditionEfficiency,
                combatSynergy = definition.combatSynergy,
                backstory = definition.backstory,
                Kind = PioneerKind.NamedCatalog,
                Disposition = EchoDisposition.Synced,
                saturation = definition.saturation,
                traitIds = definition.traitIds,
                passiveAbilityIds = definition.passiveAbilityIds,
                learnedSkills = definition.learnedSkills,
                WorkState = PioneerWorkState.Idle
            };

            if (definition.overrideDefaultFollowMode)
                record.followMode = (int)definition.behavior.followMode;

            if (applyLoadoutDefaults)
                PioneerLoadoutDefaults.EnsureDefaults(record);

            return record;
        }

        public PioneerFollowMode ResolvedFollowMode =>
            followMode >= 0
                ? (PioneerFollowMode)Mathf.Clamp(followMode, 0, 2)
                : PioneerBehaviorDefaults.CreateForClass(pioneerClass).followMode;
    }

    [Serializable]
    public class SkilledPioneerSaveRecord
    {
        public string id;
        public string displayName;
        public int pioneerClass;
        public int level;
        public float radiationResistance;
        public float expeditionEfficiency;
        public float combatSynergy;
        public string backstory;
        public bool isStarterPick;
        public int kind;
        public int disposition;
        public float saturation;
        public string[] traitIds;
        public string[] passiveAbilityIds;
        public string[] learnedSkills;
        public string weaponItemId;
        public string toolItemId;
        public string[] assignedSkillIds;
        public bool isInExpeditionTrio;
        public int workState;
        public float injuryRecoveryRemaining;
        public int followMode = -1;

        public static SkilledPioneerSaveRecord FromRuntime(SkilledPioneerRecord record)
        {
            if (record == null)
                return null;

            return new SkilledPioneerSaveRecord
            {
                id = record.id,
                displayName = record.displayName,
                pioneerClass = (int)record.pioneerClass,
                level = record.level,
                radiationResistance = record.radiationResistance,
                expeditionEfficiency = record.expeditionEfficiency,
                combatSynergy = record.combatSynergy,
                backstory = record.backstory,
                isStarterPick = record.isStarterPick,
                kind = record.kind,
                disposition = record.disposition,
                saturation = record.saturation,
                traitIds = record.traitIds,
                passiveAbilityIds = record.passiveAbilityIds,
                learnedSkills = record.learnedSkills,
                weaponItemId = record.weaponItemId,
                toolItemId = record.toolItemId,
                assignedSkillIds = record.assignedSkillIds,
                isInExpeditionTrio = record.isInExpeditionTrio,
                workState = record.workState,
                injuryRecoveryRemaining = record.injuryRecoveryRemaining,
                followMode = record.followMode
            };
        }

        public SkilledPioneerRecord ToRuntime()
        {
            int maxClass = (int)SkilledPioneerClass.IoHybrid;
            var runtime = new SkilledPioneerRecord
            {
                id = id,
                displayName = displayName,
                pioneerClass = (SkilledPioneerClass)Mathf.Clamp(pioneerClass, 0, maxClass),
                level = level,
                radiationResistance = radiationResistance,
                expeditionEfficiency = expeditionEfficiency,
                combatSynergy = combatSynergy,
                backstory = backstory,
                isStarterPick = isStarterPick,
                kind = kind,
                disposition = disposition,
                saturation = saturation,
                traitIds = traitIds,
                passiveAbilityIds = passiveAbilityIds,
                learnedSkills = learnedSkills,
                weaponItemId = weaponItemId,
                toolItemId = toolItemId,
                assignedSkillIds = assignedSkillIds,
                isInExpeditionTrio = isInExpeditionTrio,
                workState = workState,
                injuryRecoveryRemaining = injuryRecoveryRemaining,
                followMode = followMode
            };

            PioneerLoadoutDefaults.EnsureDefaults(runtime);
            return runtime;
        }
    }
}

using System;
using Project.Pioneers;
using UnityEngine;

namespace Project.Companions.Abilities
{
    /// <summary>
    /// Runtime ability loadout and cooldown tracking.
    /// </summary>
    [DisallowMultipleComponent]
    public class CompanionAbilityController : MonoBehaviour
    {
        public event Action<CompanionAbilityData> OnAbilityUsed;

        [SerializeField] private CompanionClassProfile classProfile;

        private SkilledPioneerClass _pioneerClass;
        private string _pioneerRecordId = string.Empty;
        private SkilledPioneerRecord _boundRecord;

        public CompanionClassProfile ClassProfile => classProfile;
        public SkilledPioneerClass PioneerClass => _pioneerClass;

        public void Bind(SkilledPioneerRecord record, CompanionClassProfile profileOverride = null)
        {
            if (record == null)
                return;

            _pioneerRecordId = record.id;
            _pioneerClass = record.pioneerClass;
            _boundRecord = record;
            classProfile = profileOverride != null
                ? profileOverride
                : CompanionClassProfileRegistry.GetProfile(record.pioneerClass);
        }

        public bool CanUseAbility(CompanionAbilityData ability)
        {
            if (ability == null)
                return false;

            return ability.IsAllowedForClass(_pioneerClass) &&
                   (classProfile == null || classProfile.AllowsKind(ability.kind));
        }

        public void NotifyAbilityUsed(CompanionAbilityData ability)
        {
            if (ability == null)
                return;

            OnAbilityUsed?.Invoke(ability);
        }

        public CompanionAbilityData EvaluateBestAbility()
        {
            if (_boundRecord == null || _pioneerClass != SkilledPioneerClass.MedTech)
                return null;

            if (!PioneerTraitUtility.RecordHasAbility(_boundRecord, MedTechCompanionAbilityController.FieldTriageAbilityId))
                return null;

            CompanionAbilityData triage = CompanionAbilityRegistry.Find(MedTechCompanionAbilityController.FieldTriageAbilityId);
            return CanUseAbility(triage) ? triage : null;
        }
    }
}

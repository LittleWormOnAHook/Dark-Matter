using System;
using Project.Pioneers;
using UnityEngine;

namespace Project.Companions.Abilities
{
    /// <summary>
    /// Runtime ability loadout and cooldown tracking. Execution wiring arrives in Phase 5.
    /// </summary>
    [DisallowMultipleComponent]
    public class CompanionAbilityController : MonoBehaviour
    {
        public event Action<CompanionAbilityData> OnAbilityUsed;

        [SerializeField] private CompanionClassProfile classProfile;

        private SkilledPioneerClass _pioneerClass;
        private string _pioneerRecordId = string.Empty;

        public CompanionClassProfile ClassProfile => classProfile;
        public SkilledPioneerClass PioneerClass => _pioneerClass;

        public void Bind(SkilledPioneerRecord record, CompanionClassProfile profileOverride = null)
        {
            if (record == null)
                return;

            _pioneerRecordId = record.id;
            _pioneerClass = record.pioneerClass;
            if (profileOverride != null)
                classProfile = profileOverride;
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

        /// <summary>Phase 5: evaluate combat/utility context and return the best ability to fire.</summary>
        public CompanionAbilityData EvaluateBestAbility()
        {
            return null;
        }
    }
}

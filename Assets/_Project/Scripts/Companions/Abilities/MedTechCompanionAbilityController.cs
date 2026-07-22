using Project.Pioneers;
using Project.UI;
using UnityEngine;

namespace Project.Companions.Abilities
{
    /// <summary>
    /// Med Tech expedition kit: Field Triage heals low-health squadmates; Injury Stabilize is passive on roster.
    /// </summary>
    [DisallowMultipleComponent]
    public class MedTechCompanionAbilityController : MonoBehaviour
    {
        public const string FieldTriageAbilityId = "field_triage";
        public const string InjuryStabilizeAbilityId = "injury_stabilize";
        public const float InjuryRecoveryMultiplier = 0.75f;
        public const float BaseInjuryRecoverySpeedBonus = 1.35f;

        [SerializeField] private float triageRadius = 8f;
        [SerializeField] private float triageHealPercent = 0.2f;
        [SerializeField] private float triageHealthThreshold = 0.4f;
        [SerializeField] private float triageCooldownSeconds = 10f;
        [SerializeField] private float triagePollInterval = 1f;

        private PioneerCompanionAgent agent;
        private SkilledPioneerRecord boundRecord;
        private float nextTriagePollTime;
        private float nextTriageReadyTime;

        public bool HasFieldTriage =>
            boundRecord != null && PioneerTraitUtility.RecordHasAbility(boundRecord, FieldTriageAbilityId);

        public bool HasInjuryStabilize =>
            boundRecord != null && PioneerTraitUtility.RecordHasAbility(boundRecord, InjuryStabilizeAbilityId);

        public void Bind(PioneerCompanionAgent companionAgent, SkilledPioneerRecord record)
        {
            agent = companionAgent;
            boundRecord = record;
            nextTriagePollTime = Time.time + 0.5f;
            nextTriageReadyTime = 0f;
        }

        private void Update()
        {
            if (!HasFieldTriage || agent == null || boundRecord == null)
                return;

            if (Time.time < nextTriagePollTime)
                return;

            nextTriagePollTime = Time.time + triagePollInterval;
            if (Time.time < nextTriageReadyTime)
                return;

            if (!TryFindTriageTarget(out CompanionHealth target))
                return;

            CompanionAbilityData ability = CompanionAbilityRegistry.Find(FieldTriageAbilityId);
            float cooldown = triageCooldownSeconds;
            if (ability != null && ability.cooldownSeconds > 0f)
                cooldown = ability.cooldownSeconds;

            target.ApplyHealPercent(triageHealPercent);
            nextTriageReadyTime = Time.time + cooldown;

            CompanionAbilityController abilityController = GetComponent<CompanionAbilityController>();
            abilityController?.NotifyAbilityUsed(ability);

            string targetName = target.GetComponent<PioneerCompanionAgent>()?.DisplayName ?? "ally";
            PickupToastUI.Show($"{agent.DisplayName}: Field Triage on {targetName}.");
        }

        private bool TryFindTriageTarget(out CompanionHealth bestTarget)
        {
            bestTarget = null;
            float bestRatio = triageHealthThreshold;

            CompanionRosterBridge bridge = FindAnyObjectByType<CompanionRosterBridge>();
            if (bridge?.ActiveCompanions != null)
            {
                for (int i = 0; i < bridge.ActiveCompanions.Count; i++)
                {
                    PioneerCompanionAgent companion = bridge.ActiveCompanions[i];
                    if (companion == null)
                        continue;

                    CompanionHealth health = companion.GetComponent<CompanionHealth>();
                    if (health == null || health.IsDead)
                        continue;

                    if (!IsWithinTriageRange(companion.transform.position))
                        continue;

                    float ratio = health.MaxHealth > 0f ? health.CurrentHealth / health.MaxHealth : 1f;
                    if (ratio >= triageHealthThreshold)
                        continue;

                    if (ratio < bestRatio)
                    {
                        bestRatio = ratio;
                        bestTarget = health;
                    }
                }
            }

            if (bestTarget != null)
                return true;

            CompanionHealth selfHealth = GetComponent<CompanionHealth>();
            if (selfHealth != null && !selfHealth.IsDead)
            {
                float selfRatio = selfHealth.MaxHealth > 0f ? selfHealth.CurrentHealth / selfHealth.MaxHealth : 1f;
                if (selfRatio < triageHealthThreshold)
                {
                    bestTarget = selfHealth;
                    return true;
                }
            }

            return false;
        }

        private bool IsWithinTriageRange(Vector3 worldPosition)
        {
            return (worldPosition - transform.position).sqrMagnitude <= triageRadius * triageRadius;
        }

        public static bool RosterHasInjuryStabilize(PioneerRosterManager roster)
        {
            if (roster == null)
                return false;

            IReadOnlyList<SkilledPioneerRecord> skilled = roster.SkilledPioneers;
            for (int i = 0; i < skilled.Count; i++)
            {
                SkilledPioneerRecord record = skilled[i];
                if (record == null || record.WorkState == PioneerWorkState.Injured)
                    continue;

                if (record.pioneerClass == SkilledPioneerClass.MedTech
                    && PioneerTraitUtility.RecordHasAbility(record, InjuryStabilizeAbilityId))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ExpeditionHasInjuryStabilize()
        {
            CompanionRosterBridge bridge = Object.FindAnyObjectByType<CompanionRosterBridge>();
            if (bridge?.ActiveCompanions == null)
                return false;

            for (int i = 0; i < bridge.ActiveCompanions.Count; i++)
            {
                PioneerCompanionAgent companion = bridge.ActiveCompanions[i];
                if (companion == null || companion.PioneerClass != SkilledPioneerClass.MedTech)
                    continue;

                MedTechCompanionAbilityController medTech = companion.GetComponent<MedTechCompanionAbilityController>();
                if (medTech != null && medTech.HasInjuryStabilize)
                    return true;
            }

            return false;
        }
    }
}

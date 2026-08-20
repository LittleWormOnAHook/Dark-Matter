using Project.Pioneers;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Resolves live expedition companion HP for roster UI and hover cards.
    /// </summary>
    public static class CompanionHealthLookup
    {
        public const float DefaultMaxHealth = 80f;

        public static bool TryGetDisplayedHealth(string pioneerRecordId, out int current, out int max)
        {
            current = 0;
            max = Mathf.RoundToInt(DefaultMaxHealth);

            if (string.IsNullOrEmpty(pioneerRecordId))
                return false;

            CompanionHealth live = FindLiveHealth(pioneerRecordId);
            if (live != null)
            {
                current = Mathf.CeilToInt(live.CurrentHealth);
                max = Mathf.CeilToInt(live.MaxHealth);
                return true;
            }

            SkilledPioneerRecord record = PioneerRosterManager.EnsureExists()?.FindSkilledById(pioneerRecordId);
            if (record == null)
                return false;

            if (record.WorkState == PioneerWorkState.Injured)
            {
                current = 0;
                return true;
            }

            current = max;
            return true;
        }

        public static string FormatHealthLine(string pioneerRecordId)
        {
            if (!TryGetDisplayedHealth(pioneerRecordId, out int current, out int max))
                return "HP —";

            return $"HP {current} / {max}";
        }

        public static CompanionHealth FindLiveHealth(string pioneerRecordId)
        {
            if (string.IsNullOrEmpty(pioneerRecordId))
                return null;

            CompanionRosterBridge bridge = Object.FindAnyObjectByType<CompanionRosterBridge>();
            if (bridge == null)
                return null;

            var agents = bridge.ActiveCompanions;
            for (int i = 0; i < agents.Count; i++)
            {
                PioneerCompanionAgent agent = agents[i];
                if (agent == null || agent.PioneerRecordId != pioneerRecordId)
                    continue;

                CompanionHealth health = agent.GetComponent<CompanionHealth>();
                if (health != null && !health.IsDead)
                    return health;
            }

            return null;
        }
    }
}

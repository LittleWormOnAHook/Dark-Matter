using System;
using UnityEngine;

namespace Project.Pioneers
{
    [Serializable]
    public class EchoChronicleEntry
    {
        public string id;
        public string echoName;
        public long rescuedAtUtcTicks;
        public string coreId;
        public int dispositionAtRescue;
        public string classSummary;
        public string abilitySummary;
        public bool rescueFailed;
        public bool simulationIncident;

        public EchoDisposition DispositionAtRescue =>
            (EchoDisposition)Math.Max(0, Math.Min(3, dispositionAtRescue));

        public static EchoChronicleEntry CreateSuccess(
            SkilledPioneerRecord record,
            string coreId = "",
            string abilitySummary = "")
        {
            if (record == null)
                return null;

            return new EchoChronicleEntry
            {
                id = Guid.NewGuid().ToString("N"),
                echoName = record.displayName,
                rescuedAtUtcTicks = DateTime.UtcNow.Ticks,
                coreId = coreId ?? string.Empty,
                dispositionAtRescue = (int)record.disposition,
                classSummary = SkilledPioneerClassUtility.ToDisplayName(record.pioneerClass),
                abilitySummary = string.IsNullOrWhiteSpace(abilitySummary)
                    ? PioneerTraitUtility.FormatTraitList(record.passiveAbilityIds)
                    : abilitySummary,
                rescueFailed = false
            };
        }

        public static EchoChronicleEntry CreateFailure(string echoName, string coreId = "")
        {
            return new EchoChronicleEntry
            {
                id = Guid.NewGuid().ToString("N"),
                echoName = string.IsNullOrWhiteSpace(echoName) ? "Lost Echo" : echoName,
                rescuedAtUtcTicks = DateTime.UtcNow.Ticks,
                coreId = coreId ?? string.Empty,
                dispositionAtRescue = (int)EchoDisposition.HostileUntilSynced,
                classSummary = "Signal lost",
                abilitySummary = "Permanent imprint loss",
                rescueFailed = true
            };
        }

        public static EchoChronicleEntry CreateSimulationIncident(
            string incidentId,
            float severity01,
            string debugReason = "")
        {
            string id = incidentId ?? string.Empty;
            return new EchoChronicleEntry
            {
                id = Guid.NewGuid().ToString("N"),
                echoName = FormatSimulationIncidentTitle(id),
                rescuedAtUtcTicks = DateTime.UtcNow.Ticks,
                coreId = id,
                dispositionAtRescue = 0,
                classSummary = $"Severity {Mathf.RoundToInt(Mathf.Clamp01(severity01) * 100f)}%",
                abilitySummary = string.IsNullOrWhiteSpace(debugReason)
                    ? "Off-screen colony event"
                    : debugReason,
                rescueFailed = false,
                simulationIncident = true
            };
        }

        public static string FormatSimulationIncidentTitle(string incidentId)
        {
            if (string.IsNullOrWhiteSpace(incidentId))
                return "Simulation Incident";

            switch (incidentId)
            {
                case "sim_sulfur_strain":
                    return "Sulfur Strain";
                case "sim_threat_pressure":
                    return "Threat Pressure";
                case "sim_facility_strain":
                    return "Facility Strain";
                default:
                    return incidentId.Replace('_', ' ');
            }
        }
    }
}

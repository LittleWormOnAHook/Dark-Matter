using System.Collections.Generic;

namespace Project.Pioneers
{
    public static class PioneerTraitUtility
    {
        public static readonly string[] ActiveAbilityIds =
        {
            "vent_burst",
            "crust_fracture",
            "purification_field",
            "shield_drone",
            "aggro_pulse",
            "decoy_beacon",
            "phase_step"
        };

        public static readonly string[] PassiveAbilityIds =
        {
            "echo_reverb",
            "rad_hardening",
            "tremor_sense",
            "harvest_boost",
            "synergy_link"
        };

        public static readonly string[] MenialSkillIds =
        {
            "salvage",
            "forage",
            "patchwork",
            "haul",
            "sanitize"
        };

        private static readonly Dictionary<string, string> DisplayNames = new Dictionary<string, string>
        {
            { "vent_burst", "Vent Burst" },
            { "crust_fracture", "Crust Fracture" },
            { "purification_field", "Purification Field" },
            { "shield_drone", "Shield Drone" },
            { "aggro_pulse", "Aggro Pulse" },
            { "decoy_beacon", "Decoy Beacon" },
            { "phase_step", "Phase Step" },
            { "echo_reverb", "Echo Reverb" },
            { "rad_hardening", "Rad Hardening" },
            { "tremor_sense", "Tremor Sense" },
            { "harvest_boost", "Harvest Boost" },
            { "synergy_link", "Synergy Link" },
            { "salvage", "Salvage" },
            { "forage", "Forage" },
            { "patchwork", "Patchwork" },
            { "haul", "Haul" },
            { "sanitize", "Sanitize" }
        };

        public static string ToDisplayName(string traitId)
        {
            if (string.IsNullOrWhiteSpace(traitId))
                return "Unknown";

            return DisplayNames.TryGetValue(traitId, out string displayName)
                ? displayName
                : traitId.Replace('_', ' ');
        }

        public static string FormatTraitList(string[] traitIds)
        {
            if (traitIds == null || traitIds.Length == 0)
                return "None";

            string[] labels = new string[traitIds.Length];
            for (int i = 0; i < traitIds.Length; i++)
                labels[i] = ToDisplayName(traitIds[i]);

            return string.Join(" · ", labels);
        }

        public static string GetDispositionLabel(EchoDisposition disposition)
        {
            return disposition switch
            {
                EchoDisposition.Friendly => "Friendly",
                EchoDisposition.HostileUntilSynced => "Hostile",
                EchoDisposition.Synced => "Synced",
                _ => "Neutral"
            };
        }
    }
}

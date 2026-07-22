using System;
using UnityEngine;

namespace Project.Pioneers
{
    public static class EchoGenerator
    {
        public static SkilledPioneerRecord GenerateSignal(EchoDisposition forcedDisposition = EchoDisposition.Neutral)
        {
            SkilledPioneerClass pioneerClass = RollClass();
            EchoDisposition disposition = forcedDisposition;
            if (forcedDisposition == EchoDisposition.Neutral && UnityEngine.Random.value < 0.25f)
                disposition = EchoDisposition.Friendly;
            if (forcedDisposition == EchoDisposition.Neutral && UnityEngine.Random.value < 0.2f)
                disposition = EchoDisposition.HostileUntilSynced;

            string displayName = BuildEchoName(pioneerClass);
            float saturation = disposition == EchoDisposition.HostileUntilSynced
                ? UnityEngine.Random.Range(0.65f, 0.95f)
                : UnityEngine.Random.Range(0.15f, 0.55f);

            return new SkilledPioneerRecord
            {
                id = Guid.NewGuid().ToString("N"),
                displayName = displayName,
                pioneerClass = pioneerClass,
                level = 1,
                radiationResistance = UnityEngine.Random.Range(0.35f, 0.85f),
                expeditionEfficiency = UnityEngine.Random.Range(0.35f, 0.85f),
                combatSynergy = UnityEngine.Random.Range(0.35f, 0.85f),
                backstory = "Unstable neural imprint detected near an Io vent fracture.",
                Kind = PioneerKind.RescuedEcho,
                Disposition = disposition,
                saturation = saturation,
                traitIds = RollTraits(PioneerTraitUtility.ActiveAbilityIds, 1),
                passiveAbilityIds = RollTraits(PioneerTraitUtility.PassiveAbilityIds, 2),
                learnedSkills = RollTraits(PioneerTraitUtility.MenialSkillIds, 1),
                WorkState = PioneerWorkState.Idle
            };
        }

        private static SkilledPioneerClass RollClass()
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.03f)
                return SkilledPioneerClass.IoHybrid;
            if (roll < 0.1513f)
                return SkilledPioneerClass.ArchitectEngineer;
            if (roll < 0.2725f)
                return SkilledPioneerClass.ScienceSpecialist;
            if (roll < 0.3938f)
                return SkilledPioneerClass.CombatTactician;
            if (roll < 0.515f)
                return SkilledPioneerClass.InfiltratorScout;
            if (roll < 0.6363f)
                return SkilledPioneerClass.MedTech;
            if (roll < 0.7575f)
                return SkilledPioneerClass.LogisticsOfficer;
            if (roll < 0.8788f)
                return SkilledPioneerClass.SalvageEngineer;
            return SkilledPioneerClass.CommunicationsOfficer;
        }

        private static string BuildEchoName(SkilledPioneerClass pioneerClass)
        {
            string[] prefixes =
            {
                "Sulfur-Blooded", "Rift-Touched", "Lava-Phased", "Echo-Bound", "Pyroclast",
                "Neural-Scarred", "Magma-Forged", "Void-Glitched", "Tectonic", "Crystalized"
            };
            string[] cores = { "Kael", "Lira", "Thorne", "Voss", "Nyx", "Solara", "Draven", "Quill", "Mira", "Vesper" };
            string[] suffixes = { "9", "Prime", "7X", "Storm", "Core", "-4", "-7V" };

            string prefix = prefixes[UnityEngine.Random.Range(0, prefixes.Length)];
            string core = cores[UnityEngine.Random.Range(0, cores.Length)];
            string suffix = suffixes[UnityEngine.Random.Range(0, suffixes.Length)];
            if (pioneerClass == SkilledPioneerClass.IoHybrid)
                return $"{prefix} I/O {core} {suffix}";

            return $"{prefix} {core} {suffix}";
        }

        private static string[] RollTraits(string[] pool, int count)
        {
            if (pool == null || pool.Length == 0 || count <= 0)
                return Array.Empty<string>();

            string[] result = new string[count];
            for (int i = 0; i < count; i++)
                result[i] = pool[UnityEngine.Random.Range(0, pool.Length)];

            return result;
        }
    }
}

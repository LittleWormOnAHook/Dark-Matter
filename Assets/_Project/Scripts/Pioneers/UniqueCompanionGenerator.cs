using System;
using UnityEngine;

namespace Project.Pioneers
{
    /// <summary>
    /// Procedurally rolls a unique CompanionOrigin.Other character — an alien, AI bot, hybrid, or
    /// otherwise unclassified non-human met out in the world. No network/LLM calls; everything is
    /// generated locally via randomized combinatorial templates (same approach as EchoGenerator), so
    /// it works offline on every platform and still gives each playthrough a different roster of
    /// unique recruits with their own backstory, weapon/tool preference, and buff.
    /// </summary>
    public static class UniqueCompanionGenerator
    {
        public class GeneratedCompanion
        {
            public string displayName;
            public NonHumanKind nonHumanKind;
            public SkilledPioneerClass pioneerClass;
            public float radiationResistance;
            public float expeditionEfficiency;
            public float combatSynergy;
            public string backstory;
            public string recruitmentPitch;
            public string[] traitIds;
            public string[] passiveAbilityIds;
            public string[] learnedSkills;
            public string preferredWeaponItemId;
            public string preferredToolItemId;
            public CompanionBuffModifier buff;
        }

        public static GeneratedCompanion Generate(NonHumanKind? forcedKind = null)
        {
            NonHumanKind kind = forcedKind ?? RollNonHumanKind();
            SkilledPioneerClass pioneerClass = RollClass();

            return new GeneratedCompanion
            {
                displayName = BuildName(kind),
                nonHumanKind = kind,
                pioneerClass = pioneerClass,
                radiationResistance = UnityEngine.Random.Range(0.35f, 0.9f),
                expeditionEfficiency = UnityEngine.Random.Range(0.35f, 0.9f),
                combatSynergy = UnityEngine.Random.Range(0.35f, 0.9f),
                backstory = BuildBackstory(kind),
                recruitmentPitch = BuildRecruitmentPitch(kind),
                traitIds = RollTraits(PioneerTraitUtility.ActiveAbilityIds, 1),
                passiveAbilityIds = RollTraits(PioneerTraitUtility.PassiveAbilityIds, 2),
                learnedSkills = RollTraits(PioneerTraitUtility.MenialSkillIds, 1),
                preferredWeaponItemId = RollWeapon(pioneerClass),
                preferredToolItemId = RollTool(),
                buff = RollBuff(kind)
            };
        }

        private static NonHumanKind RollNonHumanKind()
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.4f) return NonHumanKind.Alien;
            if (roll < 0.75f) return NonHumanKind.AiBot;
            if (roll < 0.92f) return NonHumanKind.Hybrid;
            return NonHumanKind.Unknown;
        }

        private static SkilledPioneerClass RollClass()
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.04f) return SkilledPioneerClass.IoHybrid;
            if (roll < 0.232f) return SkilledPioneerClass.ArchitectEngineer;
            if (roll < 0.424f) return SkilledPioneerClass.ScienceSpecialist;
            if (roll < 0.616f) return SkilledPioneerClass.CombatTactician;
            if (roll < 0.808f) return SkilledPioneerClass.InfiltratorScout;
            return SkilledPioneerClass.MedTech;
        }

        private static string BuildName(NonHumanKind kind)
        {
            switch (kind)
            {
                case NonHumanKind.AiBot:
                {
                    string[] designators = { "Unit", "Node", "Construct", "Frame", "Relay", "Cortex" };
                    string[] cores = { "VESTA", "ORYX", "KAEL", "NULLA", "IRIS", "THORN", "ZERO", "ECHO" };
                    string[] suffixes = { "-01", "-X", "-9", "-Prime", "-MK2", "-Delta" };
                    return $"{Pick(designators)} {Pick(cores)}{Pick(suffixes)}";
                }

                case NonHumanKind.Hybrid:
                {
                    string[] prefixes = { "Fused", "Twin-Sync", "Grafted", "Chimeric", "Split-Signal" };
                    string[] cores = { "Kess", "Ozby", "Thal", "Wren", "Ilva", "Coren" };
                    return $"{Pick(prefixes)} {Pick(cores)}";
                }

                case NonHumanKind.Unknown:
                {
                    string[] prefixes = { "Unclassified", "Anomalous", "Undesignated", "Flagged" };
                    string[] cores = { "Signal-7", "Entity-B", "Presence-Q", "Visitor-M" };
                    return $"{Pick(prefixes)} {Pick(cores)}";
                }

                default: // Alien
                {
                    string[] prefixes = { "Zar'", "Ilth-", "Vex'", "Korr-", "Nyth'", "Ss'" };
                    string[] cores = { "Kaelith", "Voruun", "Sethra", "Miraxi", "Odrenn", "Thyvex" };
                    string[] suffixes = { " of the Deep Vents", " the Wanderer", " of the Sulfur Choir", "", "" };
                    return $"{Pick(prefixes)}{Pick(cores)}{Pick(suffixes)}";
                }
            }
        }

        private static string BuildBackstory(NonHumanKind kind)
        {
            switch (kind)
            {
                case NonHumanKind.AiBot:
                    return "A salvaged automaton, its original mission logs corrupted — it doesn't " +
                        "remember who built it, only that it still has a job to do.";
                case NonHumanKind.Hybrid:
                    return "Neither fully human nor fully Io-native, this survivor was changed by " +
                        "prolonged exposure to the moon's deep vents — and came out the other side " +
                        "stronger for it.";
                case NonHumanKind.Unknown:
                    return "Its origin remains unconfirmed. Scans return conflicting results every " +
                        "time — organic, mechanical, both, neither.";
                default:
                    return "A native intelligence from somewhere beneath Io's crust, drawn to the " +
                        "surface by the expedition's arrival.";
            }
        }

        private static string BuildRecruitmentPitch(NonHumanKind kind)
        {
            switch (kind)
            {
                case NonHumanKind.AiBot:
                    return "Designation logged. Your colony registers as... survivable. I volunteer " +
                        "my remaining operational cycles.";
                case NonHumanKind.Hybrid:
                    return "I've walked both worlds now. Let me walk this one with you.";
                case NonHumanKind.Unknown:
                    return "You want to know what I am. Honestly? So do I. Let's find out together.";
                default:
                    return "Your kind came from the stars. Mine came from below. Perhaps there is " +
                        "room in your colony for both.";
            }
        }

        private static string RollWeapon(SkilledPioneerClass pioneerClass)
        {
            string[] pool = pioneerClass == SkilledPioneerClass.InfiltratorScout
                || pioneerClass == SkilledPioneerClass.ScienceSpecialist
                || pioneerClass == SkilledPioneerClass.MedTech
                ? new[] { "Sci-Fi Pistol", "Survival Rifle", "Spear of Fate" }
                : new[] { "Sword of Fear", "Two-Handed Sword", "Death Axe", "2 Hander" };

            return Pick(pool);
        }

        private static string RollTool()
        {
            string[] pool = { "Wood Axe", "Scanner B44", "Binoculars 250" };
            return Pick(pool);
        }

        private static CompanionBuffModifier RollBuff(NonHumanKind kind)
        {
            var buff = new CompanionBuffModifier();
            switch (kind)
            {
                case NonHumanKind.AiBot:
                    buff.label = "Precision Targeting";
                    buff.description = "Onboard ballistics computer sharpens squad accuracy.";
                    buff.combatSynergyBonus = UnityEngine.Random.Range(0.03f, 0.08f);
                    break;

                case NonHumanKind.Hybrid:
                    buff.label = "Adapted Physiology";
                    buff.description = "Built different — shrugs off environmental extremes better than most.";
                    buff.radiationResistanceBonus = UnityEngine.Random.Range(0.05f, 0.15f);
                    buff.debuffResistance = UnityEngine.Random.Range(0.1f, 0.25f);
                    break;

                case NonHumanKind.Unknown:
                    buff.label = "Uncertain Nature";
                    buff.description = "Whatever it is, it helps.";
                    buff.expeditionEfficiencyBonus = UnityEngine.Random.Range(0.03f, 0.1f);
                    buff.debuffResistance = UnityEngine.Random.Range(0.05f, 0.15f);
                    break;

                default: // Alien
                    buff.label = "Deep Vent Adaptation";
                    buff.description = "Native to Io's hazards long before the expedition arrived.";
                    buff.radiationResistanceBonus = UnityEngine.Random.Range(0.08f, 0.2f);
                    break;
            }

            return buff;
        }

        private static string Pick(string[] pool)
        {
            return pool[UnityEngine.Random.Range(0, pool.Length)];
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

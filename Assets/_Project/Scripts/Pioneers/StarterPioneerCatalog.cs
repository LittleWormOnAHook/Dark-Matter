using System.Collections.Generic;

namespace Project.Pioneers
{
    public class StarterPioneerOffer
    {
        public string offerId;
        public string displayName;
        public SkilledPioneerClass pioneerClass;
        public int acCost;
        public float radiationResistance;
        public float expeditionEfficiency;
        public float combatSynergy;
        public string backstory;
        public string abilitySummary;
    }

    public static class StarterPioneerCatalog
    {
        public const int StarterAcGrant = 5000;

        public static IReadOnlyList<StarterPioneerOffer> Offers => offers;

        private static readonly List<StarterPioneerOffer> offers = new List<StarterPioneerOffer>
        {
            new StarterPioneerOffer
            {
                offerId = "starter_kael",
                displayName = "Sulfur-Blooded Kael-9",
                pioneerClass = SkilledPioneerClass.CombatTactician,
                acCost = StarterAcGrant,
                radiationResistance = 0.62f,
                expeditionEfficiency = 0.48f,
                combatSynergy = 0.74f,
                backstory = "Recovered imprint from a failed caldera survey team.",
                abilitySummary = "Aggro Pulse · Tremor Sense"
            },
            new StarterPioneerOffer
            {
                offerId = "starter_lira",
                displayName = "Rift-Touched Lira Prime",
                pioneerClass = SkilledPioneerClass.InfiltratorScout,
                acCost = StarterAcGrant,
                radiationResistance = 0.55f,
                expeditionEfficiency = 0.71f,
                combatSynergy = 0.52f,
                backstory = "Signal trace found near a collapsed sulfur vent route.",
                abilitySummary = "Vent Burst · Echo Reverb"
            },
            new StarterPioneerOffer
            {
                offerId = "starter_calder",
                displayName = "Neural-Scarred Calder Sol",
                pioneerClass = SkilledPioneerClass.ScienceSpecialist,
                acCost = StarterAcGrant,
                radiationResistance = 0.68f,
                expeditionEfficiency = 0.64f,
                combatSynergy = 0.46f,
                backstory = "Archive fragment linked to an early isotope research crew.",
                abilitySummary = "Scan Boost · Rad Hardening"
            },
            new StarterPioneerOffer
            {
                offerId = "starter_ryn",
                displayName = "Pyroclast Ryn Vale",
                pioneerClass = SkilledPioneerClass.ArchitectEngineer,
                acCost = StarterAcGrant,
                radiationResistance = 0.5f,
                expeditionEfficiency = 0.58f,
                combatSynergy = 0.6f,
                backstory = "Imprint recovered from a ruined hab shell near lava tubes.",
                abilitySummary = "Purification Field · Shield Drone"
            }
        };
    }
}

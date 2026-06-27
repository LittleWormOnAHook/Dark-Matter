using System.Collections.Generic;

namespace Project.Pioneers
{
    public class WalletMarketplaceOffer
    {
        public string offerId;
        public string displayName;
        public SkilledPioneerClass pioneerClass;
        public int acListPrice;
        public int level = 1;
        public float radiationResistance;
        public float expeditionEfficiency;
        public float combatSynergy;
        public string abilitySummary;
        public string listingNote;
    }

    /// <summary>
    /// Mock Pioneer Survivor Exchange listings (AC-priced, in-game currency).
    /// </summary>
    public static class WalletMarketplaceCatalog
    {
        public static IReadOnlyList<WalletMarketplaceOffer> Listings => listings;

        private static readonly List<WalletMarketplaceOffer> listings = new List<WalletMarketplaceOffer>
        {
            new WalletMarketplaceOffer
            {
                offerId = "market_vexa",
                displayName = "Ash-Veiled Vexa Null",
                pioneerClass = SkilledPioneerClass.InfiltratorScout,
                acListPrice = 35,
                level = 3,
                radiationResistance = 0.58f,
                expeditionEfficiency = 0.74f,
                combatSynergy = 0.51f,
                abilitySummary = "Vent Burst · Echo Reverb",
                listingNote = "Echo Card · Unminted until listed on-chain."
            },
            new WalletMarketplaceOffer
            {
                offerId = "market_brann",
                displayName = "Basalt Warden Brann-7",
                pioneerClass = SkilledPioneerClass.CombatTactician,
                acListPrice = 42,
                level = 4,
                radiationResistance = 0.66f,
                expeditionEfficiency = 0.49f,
                combatSynergy = 0.78f,
                abilitySummary = "Aggro Pulse · Tremor Sense",
                listingNote = "Recovered from sulfur trench raid imprint."
            },
            new WalletMarketplaceOffer
            {
                offerId = "market_iora",
                displayName = "Isotope Scribe Iora-3",
                pioneerClass = SkilledPioneerClass.ScienceSpecialist,
                acListPrice = 38,
                level = 2,
                radiationResistance = 0.72f,
                expeditionEfficiency = 0.61f,
                combatSynergy = 0.44f,
                abilitySummary = "Scan Boost · Rad Hardening",
                listingNote = "Archive specialist with rad-hardened field kit."
            },
            new WalletMarketplaceOffer
            {
                offerId = "market_dex",
                displayName = "Forge-Saint Dex Hale",
                pioneerClass = SkilledPioneerClass.ArchitectEngineer,
                acListPrice = 40,
                level = 3,
                radiationResistance = 0.52f,
                expeditionEfficiency = 0.57f,
                combatSynergy = 0.63f,
                abilitySummary = "Purification Field · Shield Drone",
                listingNote = "Habitat engineer imprint from ruined dome shell."
            }
        };

        public static WalletMarketplaceOffer Find(string offerId)
        {
            for (int i = 0; i < listings.Count; i++)
            {
                if (listings[i].offerId == offerId)
                    return listings[i];
            }

            return null;
        }

        public static SkilledPioneerRecord CreateOwnedFromListing(WalletMarketplaceOffer offer)
        {
            if (offer == null)
                return null;

            return new SkilledPioneerRecord
            {
                id = System.Guid.NewGuid().ToString("N"),
                displayName = offer.displayName,
                pioneerClass = offer.pioneerClass,
                level = offer.level,
                radiationResistance = offer.radiationResistance,
                expeditionEfficiency = offer.expeditionEfficiency,
                combatSynergy = offer.combatSynergy,
                backstory = offer.listingNote,
                isStarterPick = false
            };
        }

        public static SkilledPioneerRecord CreateMockOwned(string displayName, SkilledPioneerClass pioneerClass, int level)
        {
            return new SkilledPioneerRecord
            {
                id = System.Guid.NewGuid().ToString("N"),
                displayName = displayName,
                pioneerClass = pioneerClass,
                level = level,
                radiationResistance = 0.55f,
                expeditionEfficiency = 0.6f,
                combatSynergy = 0.58f,
                backstory = "Mock wallet roster pioneer (prototype).",
                isStarterPick = false
            };
        }
    }
}

namespace Project.Pioneers
{
    public enum PioneerKind
    {
        NamedCatalog = 0,
        RescuedEcho = 1,
        ColonistWorker = 2
    }

    public enum EchoDisposition
    {
        Neutral = 0,
        Friendly = 1,
        HostileUntilSynced = 2,
        Synced = 3
    }

    public enum PioneerWorkState
    {
        Idle = 0,
        AssignedFacility = 1,
        Injured = 2,
        Sheltered = 3
    }

    /// <summary>
    /// Narrative origin of a NamedPioneerDefinition — not every companion is a rescued Echo. Drives
    /// both roster-grant timing (PioneerRosterManager) and which prefabs the Companion Prefab Tool
    /// generates for it.
    /// </summary>
    public enum CompanionOrigin
    {
        /// <summary>A rescued/synced neural imprint found in the world via an EchoWorldEntity.</summary>
        Echo = 0,

        /// <summary>Already with the player at the start of the expedition — granted immediately on
        /// a new game, no Echo encounter involved.</summary>
        Expedition = 1,

        /// <summary>Arrives later in the campaign via a support ship delivering supplies/tech/
        /// personnel — NOT auto-granted at new-game start; a story/quest trigger must call
        /// PioneerRosterManager.GrantSupportShipCompanion once the delivery event happens.</summary>
        SupportShip = 2,

        /// <summary>A unique alien, non-human, or AI bot character encountered out in the world —
        /// not a rescued Echo, not part of the starting expedition, and not delivered by a support
        /// ship. Met via a UniqueRecruitEntity, who can be asked to join the colony. NOT auto-granted
        /// at new-game start.</summary>
        Other = 3
    }

    /// <summary>
    /// Flavor classifier for a CompanionOrigin.Other companion — purely narrative, doesn't affect
    /// gameplay math, but lets the Companion Prefab Tool's generator and UI distinguish "what kind of
    /// unique character is this" (an alien, an AI bot, etc.).
    /// </summary>
    public enum NonHumanKind
    {
        Alien = 0,
        AiBot = 1,
        Hybrid = 2,
        Unknown = 3
    }
}

using System;
using Project.Achievements;
using Project.Pet;
using Project.Pioneers;
using Project.Quests;
using UnityEngine;

namespace Project.Core
{
    [Serializable]
    public class GameSaveData
    {
        public int version = 0;
        public int slotIndex;
        public long savedAtUtcTicks;
        public float health;
        public float energy;
        public float stamina;
        public float oxygen;
        public float thermalStress;
        public float radiation;
        public float sulfur;
        public float volcano;
        public float hunger;
        public float thirst;
        /// <summary>Legacy save field — migrated into <see cref="aetherCredits"/> on load.</summary>
        public float piBalance;
        public float aetherCredits;
        /// <summary>Legacy save field — migrated into <see cref="aetherCredits"/> on load.</summary>
        public float piWalletBalance;
        public bool starterPioneerSelected;
        public int workerCount;
        public SkilledPioneerSaveRecord[] skilledPioneers;
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;
        public int selectedHotbarSlot;
        public int selectedToolbarSlot = -1;
        public int activeWeaponSlot;
        public bool weaponDrawn;
        public int inventorySize = 50;
        public int unlockedMainSlots = 20;
        public int hotbarSize = 10;
        public int toolbarSize = 2;
        public InventorySlotSave[] slots;
        public QuestProgress[] questProgress;
        public string[] discoveredRecipeIds;
        public string[] pendingRecipeScrollIds;
        public int playerLevel = 1;
        public int playerXp;
        public int unspentSkillPoints;
        public string[] allocatedSkillIds;
        public int[] allocatedSkillRanks;
        public string[] exploredXpIds;
        public string[] claimedOneTimeXpKeys;
        public string[] expeditionTrioIds;
        public ColonistAggregateSaveRecord colonistAggregate;
        public EchoChronicleEntry[] echoChronicle;
        public BuildingOperationsSaveRecord buildingOperations;
        public string[] ownedPetIds;
        public string toolbarPetId;
        public AchievementProgress[] achievementProgress;
        public PetTamingProgressSaveEntry[] petTamingProgress;
        public VehicleSaveEntry[] vehicles;
        public PowerGeneratorSaveEntry[] powerGenerators;
        public byte[] fogOfWarMask;
        public int fogOfWarResolution;
        public string[] scannedDiscoveryIds;
        public string[] identifiedResourceIds;
    }

    /// <summary>Per-building generator fuel level, keyed by BuildingControlPanel.BuildingId so it
    /// round-trips independently of the vehicle/pioneer save sections.</summary>
    [Serializable]
    public class PowerGeneratorSaveEntry
    {
        public string buildingId;
        public float currentFuel;
    }

    /// <summary>
    /// Per-hovercraft persistence, keyed by the GameObject name (e.g. "Hovercraft_Pioneer") so a scene
    /// with more than one vehicle still round-trips correctly. Without this, a hovercraft's position,
    /// remaining fuel, and shield/health reset to defaults every time the game reloads.
    /// </summary>
    [Serializable]
    public class VehicleSaveEntry
    {
        public string vehicleId;
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;
        public float currentFuel;
        public float currentShield;
        public float currentHealth;
        public bool isDestroyed;
    }

    [Serializable]
    public class BuildingOperationsSaveRecord
    {
        public BuildingOperationSaveEntry[] entries;
    }

    [Serializable]
    public class BuildingOperationSaveEntry
    {
        public string buildingId;
        public string[] assignedPioneerIds;
        public string[] productionRecipeNames;
        public float[] productionProgress;
        public bool autoMaintenance;
        public bool batchProductionMode;
        public float outputMultiplier;
    }

    [Serializable]
    public class InventorySlotSave
    {
        public string itemId;
        public int amount;
    }

    public struct SaveSlotInfo
    {
        public int SlotIndex;
        public bool HasData;
        public bool HasScreenshot;
        public long SavedAtUtcTicks;
        public float Health;
        public float AetherCredits;
        public int PlayerLevel;

        public string GetSummaryLine()
        {
            if (!HasData)
                return "Empty";

            DateTime savedAt = new DateTime(SavedAtUtcTicks, DateTimeKind.Utc).ToLocalTime();
            int level = PlayerLevel > 0 ? PlayerLevel : 1;
            return $"Lv {level} | AC: {Mathf.RoundToInt(AetherCredits)} | HP: {Mathf.RoundToInt(Health)} | {savedAt:g}";
        }
    }
}

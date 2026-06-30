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
        public int version = 14;
        public int slotIndex;
        public long savedAtUtcTicks;
        public float health;
        public float energy;
        public float stamina;
        public float oxygen;
        public float hunger;
        public float thirst;
        public float piBalance;
        public float aetherCredits;
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
        public int inventorySize = 24;
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
        public float PiBalance;
        public int PlayerLevel;

        public string GetSummaryLine()
        {
            if (!HasData)
                return "Empty";

            DateTime savedAt = new DateTime(SavedAtUtcTicks, DateTimeKind.Utc).ToLocalTime();
            float credits = AetherCredits > 0f ? AetherCredits : PiBalance;
            int level = PlayerLevel > 0 ? PlayerLevel : 1;
            return $"Lv {level} | AC: {Mathf.RoundToInt(credits)} | HP: {Mathf.RoundToInt(Health)} | {savedAt:g}";
        }
    }
}

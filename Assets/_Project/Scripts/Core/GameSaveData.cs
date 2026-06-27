using System;
using Project.Pioneers;
using Project.Quests;
using UnityEngine;

namespace Project.Core
{
    [Serializable]
    public class GameSaveData
    {
        public int version = 9;
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

        public string GetSummaryLine()
        {
            if (!HasData)
                return "Empty";

            DateTime savedAt = new DateTime(SavedAtUtcTicks, DateTimeKind.Utc).ToLocalTime();
            float credits = AetherCredits > 0f ? AetherCredits : PiBalance;
            return $"AC: {Mathf.RoundToInt(credits)} | HP: {Mathf.RoundToInt(Health)} | {savedAt:g}";
        }
    }
}

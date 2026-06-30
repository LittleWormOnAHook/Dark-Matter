using System;
using System.IO;
using System.Linq;
using Project.Building;
using Project.Data;
using Project.Inventory;
using Project.Managers;
using Project.Player;
using Project.Crafting;
using Project.Pet;
using Project.Pioneers;
using Project.Progression;
using Project.Achievements;
using Project.Quests;
using Project.Survival;
using Project.UI;
using UnityEngine;

namespace Project.Core
{
    public static class GameSaveSystem
    {
        public const int SlotCount = 5;

        private const string LegacySaveFileName = "savegame.json";
        private const string SlotFileNameFormat = "savegame_slot{0}.json";

        public static bool HasSaveFile => HasAnySaveFile;

        public static bool HasAnySaveFile
        {
            get
            {
                for (int i = 0; i < SlotCount; i++)
                {
                    if (HasSaveInSlot(i))
                        return true;
                }

                return File.Exists(LegacySaveFilePath);
            }
        }

        private static string LegacySaveFilePath =>
            Path.Combine(Application.persistentDataPath, LegacySaveFileName);

        public static string GetSlotFilePath(int slotIndex)
        {
            slotIndex = ClampSlot(slotIndex);
            return Path.Combine(Application.persistentDataPath, string.Format(SlotFileNameFormat, slotIndex));
        }

        public static bool HasSaveInSlot(int slotIndex)
        {
            slotIndex = ClampSlot(slotIndex);
            return File.Exists(GetSlotFilePath(slotIndex));
        }

        public static SaveSlotInfo GetSlotInfo(int slotIndex)
        {
            MigrateLegacySaveIfNeeded();
            slotIndex = ClampSlot(slotIndex);
            SaveSlotInfo info = new SaveSlotInfo
            {
                SlotIndex = slotIndex,
                HasData = false
            };

            if (!TryReadSlotData(slotIndex, out GameSaveData data))
                return info;

            info.HasData = true;
            info.HasScreenshot = SaveSlotScreenshotUtility.HasScreenshot(slotIndex);
            info.SavedAtUtcTicks = data.savedAtUtcTicks;
            info.Health = data.health;
            info.AetherCredits = data.version >= 9 ? data.aetherCredits : data.piBalance;
            info.PiBalance = data.piBalance;
            info.PlayerLevel = data.version >= 10 ? data.playerLevel : 1;
            return info;
        }

        public static bool TrySave(int slotIndex, out string message)
        {
            return TrySave(slotIndex, null, out message);
        }

        public static bool TrySave(int slotIndex, Texture2D screenshot, out string message)
        {
            slotIndex = ClampSlot(slotIndex);

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
            {
                message = "No player found to save.";
                return false;
            }

            InventorySystem inventory = player.GetComponent<InventorySystem>();
            SurvivalStats stats = player.GetComponent<SurvivalStats>();
            EquipmentController equipment = player.GetComponent<EquipmentController>();
            UIManager ui = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            QuestManager questManager = UnityEngine.Object.FindAnyObjectByType<QuestManager>();
            CraftingManager craftingManager = UnityEngine.Object.FindAnyObjectByType<CraftingManager>();
            PlayerProgressionManager progressionManager = player.GetComponent<PlayerProgressionManager>()
                ?? PlayerProgressionManager.EnsureExists();
            AchievementManager achievementManager = AchievementManager.EnsureExists();

            PioneerRosterManager roster = UnityEngine.Object.FindAnyObjectByType<PioneerRosterManager>();
            PetManager petManager = PetManager.EnsureExists();

            if (inventory == null || stats == null)
            {
                message = "Player data is missing.";
                return false;
            }

            GameSaveData data = new GameSaveData
            {
                version = 14,
                slotIndex = slotIndex,
                savedAtUtcTicks = DateTime.UtcNow.Ticks,
                health = stats.CurrentHealth,
                energy = stats.CurrentEnergy,
                stamina = stats.CurrentStamina,
                oxygen = stats.CurrentOxygen,
                piBalance = 0f,
                aetherCredits = roster != null ? roster.AetherCredits : (ui != null ? ui.GetAetherCredits() : 0f),
                piWalletBalance = roster != null ? roster.PiWalletBalance : (ui != null ? ui.GetPiWalletBalance() : 0f),
                starterPioneerSelected = roster != null && roster.StarterPioneerSelected,
                workerCount = roster != null ? roster.WorkerCount : 0,
                skilledPioneers = roster != null ? roster.BuildSaveRecords() : null,
                expeditionTrioIds = roster != null ? roster.BuildExpeditionTrioSave() : null,
                colonistAggregate = roster != null ? roster.BuildColonistAggregateSave() : null,
                echoChronicle = roster != null ? roster.BuildEchoChronicleSave() : null,
                buildingOperations = BuildingOperationRegistry.BuildSaveSnapshot(),
                ownedPetIds = petManager != null ? petManager.BuildOwnedPetSave() : null,
                toolbarPetId = petManager != null ? petManager.BuildToolbarPetSave() : string.Empty,
                petTamingProgress = petManager != null ? petManager.BuildTamingSave() : null,
                achievementProgress = achievementManager != null ? achievementManager.BuildSaveSnapshot() : null,
                posX = player.transform.position.x,
                posY = player.transform.position.y,
                posZ = player.transform.position.z,
                rotY = player.transform.eulerAngles.y,
                selectedHotbarSlot = equipment != null ? equipment.SelectedHotbarSlot : 0,
                selectedToolbarSlot = equipment != null ? equipment.SelectedToolbarSlot : -1,
                activeWeaponSlot = equipment != null ? equipment.ActiveWeaponSlot : 0,
                weaponDrawn = equipment == null || equipment.IsWeaponDrawn,
                inventorySize = inventory.inventorySize,
                hotbarSize = inventory.hotbarSize,
                toolbarSize = inventory.toolbarSize,
                slots = BuildInventorySave(inventory),
                questProgress = questManager != null ? questManager.BuildSaveProgress().ToArray() : null,
                discoveredRecipeIds = craftingManager != null ? craftingManager.BuildSave() : null,
                pendingRecipeScrollIds = craftingManager != null ? craftingManager.BuildPendingSave() : null
            };

            if (progressionManager != null)
            {
                ProgressionSaveSnapshot snapshot = progressionManager.BuildSaveSnapshot();
                data.playerLevel = snapshot.playerLevel;
                data.playerXp = snapshot.playerXp;
                data.unspentSkillPoints = snapshot.unspentSkillPoints;
                data.allocatedSkillIds = snapshot.allocatedSkillIds;
                data.allocatedSkillRanks = snapshot.allocatedSkillRanks;
                data.exploredXpIds = snapshot.exploredXpIds;
                data.claimedOneTimeXpKeys = snapshot.claimedOneTimeXpKeys;
            }

            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(GetSlotFilePath(slotIndex), json);

            if (screenshot != null)
                SaveSlotScreenshotUtility.SaveScreenshot(slotIndex, screenshot);

            GameSettings.MarkSaveExists(true);
            message = $"Saved to slot {slotIndex + 1}.";
            return true;
        }

        public static bool TryLoad(int slotIndex, out string message)
        {
            slotIndex = ClampSlot(slotIndex);
            MigrateLegacySaveIfNeeded();

            if (!TryReadSlotData(slotIndex, out GameSaveData data))
            {
                message = $"Save slot {slotIndex + 1} is empty.";
                return false;
            }

            ApplySaveData(data);
            GameSettings.MarkSaveExists(true);
            message = $"Loaded slot {slotIndex + 1}.";
            return true;
        }

        public static void ApplySaveData(GameSaveData data)
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
            {
                Debug.LogError("GameSaveSystem: Player not found while loading.");
                return;
            }

            GameSession.MarkStarted();

            InventorySystem inventory = player.GetComponent<InventorySystem>();
            SurvivalStats stats = player.GetComponent<SurvivalStats>();
            EquipmentController equipment = player.GetComponent<EquipmentController>();
            UIManager ui = UnityEngine.Object.FindAnyObjectByType<UIManager>();

            if (inventory != null)
            {
                ApplyInventorySave(inventory, data.slots, data);
                RefreshInventoryUiAfterLoad();
            }

            if (stats != null)
            {
                ResolveSurvivalValues(stats, data, out float health, out float energy, out float stamina, out float oxygen);
                stats.ApplySaveState(health, energy, stamina, oxygen);
                stats.SetSimulationPaused(false);
            }

            equipment?.ApplySaveState(
                data.selectedHotbarSlot,
                data.activeWeaponSlot,
                data.weaponDrawn,
                data.version >= 3 ? data.selectedToolbarSlot : -1);

            Vector3 position = new Vector3(data.posX, data.posY, data.posZ);
            player.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, data.rotY, 0f));

            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.SetGameplayPaused(false);
                playerController.SetInventoryOpen(false);
                playerController.SetJournalOpen(false);
            }

            if (ui != null)
            {
                ui.HideDeathPopup();
                ui.RefreshSurvivalDisplay();
            }

            ApplyRosterSave(data, ui);
            ApplyPetSave(data);
            ApplyAchievementSave(data);

            ApplyQuestSave(player, data.questProgress);
            ApplyCraftingSave(player, data.discoveredRecipeIds, data.pendingRecipeScrollIds);
            ApplyProgressionSave(player, data);

            SimpleGameManager.Instance?.BeginNewGameSession(grantStartingItems: false);
        }

        private static void ApplyProgressionSave(GameObject player, GameSaveData data)
        {
            PlayerProgressionManager progression = player.GetComponent<PlayerProgressionManager>();
            if (progression == null)
                progression = PlayerProgressionManager.EnsureExists();

            ProgressionStatScaler scaler = player.GetComponent<ProgressionStatScaler>();
            if (scaler == null)
                scaler = player.AddComponent<ProgressionStatScaler>();

            if (data.version >= 10)
            {
                progression.ApplySaveSnapshot(new ProgressionSaveSnapshot
                {
                    playerLevel = data.playerLevel,
                    playerXp = data.playerXp,
                    unspentSkillPoints = data.unspentSkillPoints,
                    allocatedSkillIds = data.allocatedSkillIds,
                    allocatedSkillRanks = data.allocatedSkillRanks,
                    exploredXpIds = data.exploredXpIds,
                    claimedOneTimeXpKeys = data.claimedOneTimeXpKeys
                });
            }
            else
            {
                progression.ResetToNewGame();
            }

            scaler.CaptureBaseMaxValues();
            scaler.ApplyLevelScaling();
        }

        private static void ApplyRosterSave(GameSaveData data, UIManager ui)
        {
            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            if (roster == null)
                return;

            if (data.version >= 11)
            {
                roster.ApplySaveV11(
                    data.aetherCredits,
                    data.piWalletBalance,
                    data.workerCount,
                    data.starterPioneerSelected,
                    data.skilledPioneers,
                    data.expeditionTrioIds,
                    data.colonistAggregate,
                    data.echoChronicle);
                BuildingOperationRegistry.ApplySaveSnapshot(data.buildingOperations);
                return;
            }

            if (data.version >= 9)
            {
                roster.ApplySave(
                    data.aetherCredits,
                    data.piWalletBalance,
                    data.workerCount,
                    data.starterPioneerSelected,
                    data.skilledPioneers);
                roster.EnsureDefaultTrioIfNeededPublic();
                return;
            }

            roster.ApplySave(0f, 0f, 0, false, null);
            roster.ApplyLegacyPiBalanceMigration(data.piBalance);

            if (ui != null)
            {
                ui.SetAetherCredits(roster.AetherCredits);
                ui.SetPiWalletBalance(roster.PiWalletBalance);
            }
        }

        private static void ApplyPetSave(GameSaveData data)
        {
            if (data.version < 12)
                return;

            PetManager manager = PetManager.EnsureExists();
            manager?.ApplySave(data.ownedPetIds, data.toolbarPetId);

            if (data.version >= 13)
                manager?.ApplyTamingSave(data.petTamingProgress);
        }

        private static void ApplyAchievementSave(GameSaveData data)
        {
            if (data.version < 13)
                return;

            AchievementManager manager = AchievementManager.EnsureExists();
            manager?.ApplySave(data.achievementProgress);
            DynamicAchievementGenerator.EnsureSessionGoals();
        }

        private static void ApplyCraftingSave(GameObject player, string[] discoveredRecipeIds, string[] pendingRecipeScrollIds)
        {
            CraftingManager craftingManager = UnityEngine.Object.FindAnyObjectByType<CraftingManager>();
            if (craftingManager == null)
                craftingManager = player.GetComponent<CraftingManager>() ?? player.AddComponent<CraftingManager>();

            craftingManager.ApplySave(discoveredRecipeIds);
            craftingManager.ApplyPendingSave(pendingRecipeScrollIds);
        }

        private static void ApplyQuestSave(GameObject player, QuestProgress[] savedQuestProgress)
        {
            if (savedQuestProgress == null)
                return;

            QuestManager questManager = QuestManager.EnsureExists();
            if (questManager == null)
                return;

            questManager.ApplySaveProgress(savedQuestProgress);
        }

        private static void ResolveSurvivalValues(
            SurvivalStats stats,
            GameSaveData data,
            out float health,
            out float energy,
            out float stamina,
            out float oxygen)
        {
            health = data.health;
            energy = data.energy;
            stamina = data.stamina;
            oxygen = data.oxygen;

            if (data.version < 8)
            {
                float legacyHunger = data.hunger;
                float legacyThirst = data.thirst;
                float legacyStamina = data.energy;

                if (health <= 0f)
                    health = stats.maxHealth;

                energy = (legacyHunger + legacyThirst) * 0.5f;
                if (energy <= 0f && legacyHunger <= 0f && legacyThirst <= 0f)
                    energy = stats.maxEnergy;

                stamina = legacyStamina > 0f ? legacyStamina : stats.maxStamina;
                oxygen = stats.maxOxygen;
                return;
            }

            bool missingSurvivalFields = energy <= 0f && stamina <= 0f && oxygen <= 0f;
            if (data.version < 2 && missingSurvivalFields)
            {
                health = health > 0f ? health : stats.maxHealth;
                energy = stats.maxEnergy;
                stamina = stats.maxStamina;
                oxygen = stats.maxOxygen;
            }
        }

        private static bool TryReadSlotData(int slotIndex, out GameSaveData data)
        {
            data = null;
            slotIndex = ClampSlot(slotIndex);
            string path = GetSlotFilePath(slotIndex);

            if (!File.Exists(path))
                return false;

            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<GameSaveData>(json);
            return data != null;
        }

        private static void MigrateLegacySaveIfNeeded()
        {
            if (!File.Exists(LegacySaveFilePath) || HasSaveInSlot(0))
                return;

            try
            {
                string json = File.ReadAllText(LegacySaveFilePath);
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
                if (data == null)
                    return;

                data.slotIndex = 0;
                if (data.savedAtUtcTicks <= 0)
                    data.savedAtUtcTicks = DateTime.UtcNow.Ticks;

                File.WriteAllText(GetSlotFilePath(0), JsonUtility.ToJson(data, prettyPrint: true));
                File.Delete(LegacySaveFilePath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"GameSaveSystem: Failed to migrate legacy save. {exception.Message}");
            }
        }

        private static int ClampSlot(int slotIndex)
        {
            return Mathf.Clamp(slotIndex, 0, SlotCount - 1);
        }

        private static InventorySlotSave[] BuildInventorySave(InventorySystem inventory)
        {
            InventorySlotSave[] result = new InventorySlotSave[inventory.slots.Count];
            for (int i = 0; i < inventory.slots.Count; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot == null || slot.IsEmpty || slot.item == null)
                {
                    result[i] = new InventorySlotSave();
                    continue;
                }

                result[i] = new InventorySlotSave
                {
                    itemId = slot.item.name,
                    amount = slot.amount
                };
            }

            return result;
        }

        private static void ApplyInventorySave(InventorySystem inventory, InventorySlotSave[] savedSlots, GameSaveData data)
        {
            if (inventory == null)
                return;

            if (data != null && data.version >= 7)
            {
                inventory.EnsureSlotCounts(data.inventorySize, data.hotbarSize, data.toolbarSize);
            }

            inventory.ClearAllSlots();

            if (savedSlots == null)
            {
                inventory.NotifyInventoryChanged();
                return;
            }

            int count = Mathf.Min(savedSlots.Length, inventory.slots.Count);
            for (int i = 0; i < count; i++)
            {
                InventorySlotSave saved = savedSlots[i];
                if (saved == null || string.IsNullOrEmpty(saved.itemId) || saved.amount <= 0)
                    continue;

                ItemData item = ItemRegistry.Resolve(saved.itemId);
                if (item == null)
                {
                    Debug.LogWarning($"GameSaveSystem: Unknown item '{saved.itemId}' in save file.");
                    continue;
                }

                inventory.slots[i].item = item;
                inventory.slots[i].amount = saved.amount;
            }

            inventory.NotifyInventoryChanged();
        }

        private static void RefreshInventoryUiAfterLoad()
        {
            InventoryUI inventoryUi = UnityEngine.Object.FindAnyObjectByType<InventoryUI>();
            inventoryUi?.RebuildSlotsIfNeeded();
            inventoryUi?.RefreshUI();
            inventoryUi?.RefreshMainInventoryLayout();
        }
    }
}

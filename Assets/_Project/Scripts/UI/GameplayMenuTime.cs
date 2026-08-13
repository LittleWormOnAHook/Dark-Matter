using System.Collections.Generic;
using Project.Core;
using Project.Player;
using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Gameplay menu time policy: Inventory and Mode Switch fully pause (<c>timeScale = 0</c>);
    /// other in-game menus slow the world to 20%. Main-menu / boot hard-pause is left alone.
    /// </summary>
    public static class GameplayMenuTime
    {
        public const float SlowMotionScale = 0.2f;

        public const string ReasonJournalInventory = "JournalInventory";
        public const string ReasonJournal = "Journal";
        public const string ReasonBuildingControl = "BuildingControl";
        public const string ReasonQuestDialog = "QuestDialog";
        public const string ReasonLootDialog = "LootDialog";
        public const string ReasonCraftingStation = "CraftingStation";
        public const string ReasonHovercraftMenu = "HovercraftMenu";
        public const string ReasonWeaponModeSwitch = "WeaponModeSwitch";
        public const string ReasonPetPanel = "PetPanel";
        public const string ReasonStandaloneMap = "StandaloneMap";

        private static readonly HashSet<string> pauseReasons = new HashSet<string>();
        private static readonly HashSet<string> slowReasons = new HashSet<string>();

        public static bool IsInventoryPaused => pauseReasons.Count > 0;
        public static bool IsMenuSlowMotion => pauseReasons.Count == 0 && slowReasons.Count > 0;

        public static void SetPause(string reason, bool active)
        {
            if (string.IsNullOrEmpty(reason))
                return;

            if (active)
                pauseReasons.Add(reason);
            else
                pauseReasons.Remove(reason);

            Apply();
        }

        public static void SetSlowMotion(string reason, bool active)
        {
            if (string.IsNullOrEmpty(reason))
                return;

            if (active)
                slowReasons.Add(reason);
            else
                slowReasons.Remove(reason);

            Apply();
        }

        /// <summary>
        /// Journal tabs: Inventory → full pause; every other journal window → 20% slow-mo.
        /// </summary>
        public static void SyncJournal(bool open, JournalWindowId? window)
        {
            if (!open || !window.HasValue)
            {
                SetPause(ReasonJournalInventory, false);
                SetSlowMotion(ReasonJournal, false);
                return;
            }

            bool inventory = window.Value == JournalWindowId.Inventory;
            SetPause(ReasonJournalInventory, inventory);
            SetSlowMotion(ReasonJournal, !inventory);
        }

        public static void ClearAll()
        {
            pauseReasons.Clear();
            slowReasons.Clear();
            Apply();
        }

        public static void Apply()
        {
            if (!Application.isPlaying || !GameSession.HasStarted)
                return;

            // Hard pause owned by main menu / boot — do not fight it when we have no reasons.
            if (pauseReasons.Count == 0 && slowReasons.Count == 0)
            {
                PlayerController player = PlayerLocator.FindPlayerController();
                if (player != null && player.IsGameplayPaused)
                    return;

                if (!Mathf.Approximately(Time.timeScale, 1f))
                    Time.timeScale = 1f;
                return;
            }

            if (pauseReasons.Count > 0)
            {
                if (!Mathf.Approximately(Time.timeScale, 0f))
                    Time.timeScale = 0f;
                return;
            }

            if (!Mathf.Approximately(Time.timeScale, SlowMotionScale))
                Time.timeScale = SlowMotionScale;
        }
    }
}

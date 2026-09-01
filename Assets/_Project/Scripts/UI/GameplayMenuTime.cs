using System.Collections.Generic;
using Project.Core;
using Project.Player;
using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Gameplay menu time policy: Journal tabs and Mode Switch fully pause (<c>timeScale = 0</c>);
    /// other in-game menus slow the world to 20%. Main-menu / boot hard-pause is left alone.
    /// </summary>
    public static class GameplayMenuTime
    {
        public const float SlowMotionScale = 0.2f;

        public const string ReasonJournalInventory = "JournalInventory";
        public const string ReasonJournal = "Journal";
        public const string ReasonBuildingControl = "BuildingControl";
        public const string ReasonQuestDialog = "QuestDialog";
        public const string ReasonPptDirections = "PptDirections";
        public const string ReasonLootDialog = "LootDialog";
        public const string ReasonCraftingStation = "CraftingStation";
        public const string ReasonHovercraftMenu = "HovercraftMenu";
        public const string ReasonWeaponModeSwitch = "WeaponModeSwitch";
        public const string ReasonPetPanel = "PetPanel";
        public const string ReasonStandaloneMap = "StandaloneMap";
        public const string ReasonQuoraShelterMenu = "QuoraShelterMenu";
        public const string ReasonWalkerDrillMenu = "WalkerDrillMenu";

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
        /// Journal tabs: every open journal window fully freezes gameplay (<c>timeScale = 0</c>)
        /// so cursor stays free and UI stays window-bound (Inventory and all other tabs).
        /// </summary>
        public static void SyncJournal(bool open, JournalWindowId? window)
        {
            // Mutate sets then Apply once — avoid SetPause(false) clearing to "no reasons"
            // (which queues a gameplay cursor relock) before slow-mo/pause is re-applied.
            if (!open || !window.HasValue)
            {
                pauseReasons.Remove(ReasonJournalInventory);
                slowReasons.Remove(ReasonJournal);
                Apply();
                return;
            }

            pauseReasons.Add(ReasonJournalInventory);
            slowReasons.Remove(ReasonJournal);
            Apply();
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

            // Hard pause owned by main menu / boot. Do not fight it when we have no reasons.
            if (pauseReasons.Count == 0 && slowReasons.Count == 0)
            {
                PlayerController player = PlayerLocator.FindPlayerController();
                if (player != null && player.IsGameplayPaused)
                    return;

                if (!Mathf.Approximately(Time.timeScale, 1f))
                    Time.timeScale = 1f;

                // Menus often apply cursor while timeScale is still 0, then unpause.
                // Relock after time is restored, and again next frame so a UI click does not eat it.
                GameplayInputRecovery.QueueCursorRestore();
                return;
            }

            if (pauseReasons.Count > 0)
            {
                if (!Mathf.Approximately(Time.timeScale, 0f))
                    Time.timeScale = 0f;
                PlayerLocator.FindPlayerController()?.ApplyCursorState();
                return;
            }

            if (!Mathf.Approximately(Time.timeScale, SlowMotionScale))
                Time.timeScale = SlowMotionScale;
            PlayerLocator.FindPlayerController()?.ApplyCursorState();
        }
    }
}
using System;

namespace Project.Building
{
    /// <summary>
    /// Hold B toggles build mode. The up panel selects a style library (Stone, Iron, Silicate...) or one known building.
    /// </summary>
    public static class DMBuildingMode
    {
        public const float HoldSeconds = 0.4f;

        public static bool IsActive { get; private set; }
        public static string HotbarId { get; private set; } = DMBuildingCatalog.StoneId;
        public static int SelectedIndex { get; private set; }
        public static int SlotFocus { get; private set; }
        public static int WindowStart { get; private set; }
        public static bool MaterialsOpen { get; private set; }

        public static event Action Changed;

        public static DMBuildingPiece SelectedPiece => DMBuildingCatalog.Get(HotbarId, SelectedIndex);

        /// <summary>0927-fast-play: with domain reload off (Fast Play) build mode must not carry over into the next play.</summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            IsActive = false;
            HotbarId = DMBuildingCatalog.StoneId;
            SelectedIndex = 0;
            SlotFocus = 0;
            WindowStart = 0;
            MaterialsOpen = false;
            Changed = null;
        }

        // 0927-last-style: build mode reopens on the style (or building) and slot used last, also across plays.
        const string LastHotbarPref = "DM.Build.LastHotbar";
        const string LastSlotPref = "DM.Build.LastSlot";

        static void RememberLast()
        {
            if (string.IsNullOrEmpty(HotbarId))
                return;
            UnityEngine.PlayerPrefs.SetString(LastHotbarPref, HotbarId);
            UnityEngine.PlayerPrefs.SetInt(LastSlotPref, SelectedIndex);
        }

        static void RestoreLast()
        {
            string hotbar = HotbarId;
            int slot = SelectedIndex;
            string saved = UnityEngine.PlayerPrefs.GetString(LastHotbarPref, string.Empty);
            if (!string.IsNullOrEmpty(saved) && DMBuildingCatalog.PiecesFor(saved).Count > 0)
            {
                hotbar = saved;
                slot = UnityEngine.PlayerPrefs.GetInt(LastSlotPref, 0);
            }

            int count = DMBuildingCatalog.PiecesFor(hotbar).Count;
            if (count == 0) // a building that is no longer known: fall back to Stone
            {
                hotbar = DMBuildingStyles.DefaultStyleId;
                slot = 0;
                count = DMBuildingCatalog.PiecesFor(hotbar).Count;
            }

            HotbarId = hotbar;
            SelectedIndex = count > 0 ? UnityEngine.Mathf.Clamp(slot, 0, count - 1) : 0;
            SlotFocus = SelectedIndex;
            WindowStart = SlotFocus >= 10 ? SlotFocus - 9 : 0;
        }

        public static void Toggle()
        {
            IsActive = !IsActive;
            if (!IsActive)
            {
                RememberLast();
                MaterialsOpen = false;
                DMBuildingPlacementController.DiscardUnbuiltGhosts();
                DMBuildingPlacementController.Release();
            }
            else
            {
                RestoreLast();
                DMBuildingPlacementController.Ensure();
            }

            Changed?.Invoke();
        }

        /// <summary>0927-cleaner: close build mode (if open) and drop its preview; used on New Game / Load.</summary>
        public static void ForceExit()
        {
            if (IsActive)
            {
                Toggle();
                return;
            }

            MaterialsOpen = false;
            DMBuildingPlacementController.Release();
        }

        public static void SelectStone()
        {
            SelectStyle(DMBuildingStyles.DefaultStyleId);
        }

        /// <summary>Opens one style library (Stone, Iron, Silicate...) as the build hotbar.</summary>
        public static void SelectStyle(string styleId)
        {
            HotbarId = string.IsNullOrEmpty(styleId) ? DMBuildingStyles.DefaultStyleId : styleId;
            SelectedIndex = 0;
            SlotFocus = 0;
            WindowStart = 0;
            MaterialsOpen = false;
            RememberLast();
            Changed?.Invoke();
        }

        public static void SelectBuilding(string buildingId)
        {
            HotbarId = string.IsNullOrEmpty(buildingId) ? DMBuildingCatalog.StoneId : buildingId;
            SelectedIndex = 0;
            SlotFocus = 0;
            WindowStart = 0;
            MaterialsOpen = false;
            RememberLast();
            Changed?.Invoke();
        }

        public static void ToggleMaterials()
        {
            MaterialsOpen = !MaterialsOpen;
            Changed?.Invoke();
        }

        public static void SelectSlot(int index)
        {
            int count = DMBuildingCatalog.PiecesFor(HotbarId).Count;
            if (count <= 0)
                return;
            SelectedIndex = UnityEngine.Mathf.Clamp(index, 0, count - 1);
            SlotFocus = SelectedIndex;
            if (SlotFocus < WindowStart)
                WindowStart = SlotFocus;
            else if (SlotFocus >= WindowStart + 10)
                WindowStart = SlotFocus - 9;
            RememberLast();
            Changed?.Invoke();
        }

        public static void ConfirmHighlight()
        {
            SelectSlot(SlotFocus);
        }

        /// <summary>Cycles the selected piece immediately. Used by the mouse wheel and side arrows.</summary>
        public static void StepSelection(int direction)
        {
            int count = DMBuildingCatalog.PiecesFor(HotbarId).Count;
            if (count <= 0 || direction == 0)
                return;

            int next = (SelectedIndex + direction) % count;
            if (next < 0)
                next += count;
            SelectSlot(next);
        }

        public static void StepHighlight(int direction)
        {
            StepSelection(direction);
        }
    }
}

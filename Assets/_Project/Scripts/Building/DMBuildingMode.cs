using System;

namespace Project.Building
{
    /// <summary>
    /// Hold B toggles build mode. The up panel selects Stone components or one known building.
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

        public static void Toggle()
        {
            IsActive = !IsActive;
            if (!IsActive)
            {
                MaterialsOpen = false;
                DMBuildingPlacementController.DiscardUnbuiltGhosts();
                DMBuildingPlacementController.Release();
            }
            else
                DMBuildingPlacementController.Ensure();

            Changed?.Invoke();
        }

        public static void SelectStone()
        {
            HotbarId = DMBuildingCatalog.StoneId;
            SelectedIndex = 0;
            SlotFocus = 0;
            WindowStart = 0;
            MaterialsOpen = false;
            Changed?.Invoke();
        }

        public static void SelectBuilding(string buildingId)
        {
            HotbarId = string.IsNullOrEmpty(buildingId) ? DMBuildingCatalog.StoneId : buildingId;
            SelectedIndex = 0;
            SlotFocus = 0;
            WindowStart = 0;
            MaterialsOpen = false;
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

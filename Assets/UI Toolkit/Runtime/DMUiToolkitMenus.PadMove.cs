using Project.Audio;
using Project.Inventory;
using Project.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// 2026-10-25i: ghost "move" mode shared by gamepad (hold A / context Move) and mouse (context Move).
    /// Reuses BeginInvDrag ghost + MoveOrMergeSlots/CanAcceptItemAt rules. Drop snaps to the slot under the
    /// ghost, else the nearest empty slot within DMJournalPadProfile.snapRadiusSlots; otherwise it returns.
    /// </summary>
    public partial class DMUiToolkitMenus
    {
        private const string PadMoveTargetClass = "dmg-pad-move-target";
        private bool padMoveActive;
        private bool padMoveFromHold;
        private bool padMoveMouse;
        private int padMoveStartFrame = -1;
        private int padMoveSwallowFrame = -1;
        private VisualElement padMoveTarget;

        public static bool IsPadMoveActive => instance != null && instance.padMoveActive;
        public bool PadMoveFromHold => padMoveActive && padMoveFromHold;
        public bool PadMoveMouse => padMoveActive && padMoveMouse;

        public static bool TryCancelPadMove()
        {
            if (instance == null || !instance.padMoveActive)
                return false;
            instance.CancelPadMove();
            return true;
        }

        private bool ShouldSwallowInvPointer() => padMoveActive || Time.frameCount <= padMoveSwallowFrame;

        public bool BeginPadMove(int slotIndex, Vector2 panelPos, bool fromHold, bool mouse)
        {
            if (padMoveActive)
                CancelPadMove();
            BeginInvDrag(slotIndex, panelPos);
            if (!invDragActive || invDragSource != slotIndex)
                return false;

            DMJournalPadProfile prof = DMJournalPadProfile.Active;
            padMoveActive = true;
            padMoveFromHold = fromHold;
            padMoveMouse = mouse;
            padMoveStartFrame = Time.frameCount;
            if (invDragGhost != null)
                invDragGhost.style.opacity = prof.ghostOpacity;
            GameAudioManager.Instance?.PlayInventoryItemClick();
            UpdatePadMove(panelPos);
            return true;
        }

        public void UpdatePadMove(Vector2 panelPos)
        {
            if (!padMoveActive)
                return;
            PositionInvDragGhost(panelPos);
            SetPadMoveTarget(ResolvePadMoveTarget(panelPos, out _));
        }

        public void FinishPadMove(Vector2 panelPos)
        {
            if (!padMoveActive)
                return;
            ResolvePadMoveTarget(panelPos, out int dest);
            int src = invDragSource;
            EndPadMove();
            if (dest >= 0 && dest != src && boundInventory != null && src >= 0 && src < boundInventory.slots.Count)
            {
                InventorySystem.InventorySlot from = boundInventory.slots[src];
                if (from != null && !from.IsEmpty && from.item != null
                    && boundInventory.CanAcceptItemAt(dest, from.item, showLevelToast: true))
                    boundInventory.MoveOrMergeSlots(src, dest);
            }
            RefreshInventory();
        }

        public void CancelPadMove()
        {
            if (!padMoveActive)
                return;
            EndPadMove();
            RefreshInventory();
        }

        private void EndPadMove()
        {
            SetPadMoveTarget(null);
            ClearInvDragGhost();
            invDragSource = -1;
            invDragActive = false;
            SetInventoryScrollEnabled(true);
            padMoveActive = false;
            padMoveMouse = false;
            padMoveFromHold = false;
        }

        /// <summary>Pad X tap: use a usable item, else a small click as feedback.</summary>
        public void PadUseSlot(int slotIndex)
        {
            InventoryItemActions actions = EnsureBoundItemActions();
            if (actions != null && actions.CanUse(slotIndex))
            {
                actions.TryUse(slotIndex);
                RefreshInventory();
                return;
            }
            GameAudioManager.Instance?.PlayInventoryItemClick();
        }

        public VisualElement GetInventorySlotElement(int absoluteIndex)
        {
            if (absoluteIndex >= 0 && absoluteIndex < invSlots.Count && invSlots[absoluteIndex]?.userData is int a && a == absoluteIndex)
                return invSlots[absoluteIndex];
            for (int i = 0; i < invHotbarSlots.Count; i++)
                if (invHotbarSlots[i]?.userData is int h && h == absoluteIndex)
                    return invHotbarSlots[i];
            return null;
        }

        private void StartMoveFromContext(int slotIndex)
        {
            VisualElement el = GetInventorySlotElement(slotIndex);
            Vector2 c = el != null ? el.worldBound.center : invLastPanelPos;
            bool mouse = !DMInputSchemeRouter.IsGamepadScheme;
            if (mouse)
                c = ScreenToMenuPanel(CurrentPointerScreenPosition());
            BeginPadMove(slotIndex, c, false, mouse);
            if (!mouse && el != null)
                el.schedule.Execute(() => { if (padMoveActive) el.Focus(); }).ExecuteLater(1);
        }

        private void TickPadMoveMouse()
        {
            if (!padMoveActive)
                return;
            if (!IsInventoryOpen)
            {
                CancelPadMove();
                return;
            }
            if (!padMoveMouse)
                return;

            Vector2 p = ScreenToMenuPanel(CurrentPointerScreenPosition());
            UpdatePadMove(p);
            if (Time.frameCount <= padMoveStartFrame + 1)
                return;
            Mouse m = Mouse.current;
            if (m == null)
                return;
            if (m.leftButton.wasPressedThisFrame)
            {
                padMoveSwallowFrame = Time.frameCount + 1;
                FinishPadMove(p);
            }
            else if (m.rightButton.wasPressedThisFrame)
            {
                padMoveSwallowFrame = Time.frameCount + 1;
                CancelPadMove();
            }
        }

        private VisualElement ResolvePadMoveTarget(Vector2 p, out int dest)
        {
            dest = -1;
            int src = invDragSource;
            if (boundInventory == null || src < 0 || src >= boundInventory.slots.Count)
                return null;
            InventorySystem.InventorySlot from = boundInventory.slots[src];
            if (from == null || from.IsEmpty || from.item == null)
                return null;

            float pitch = 0f;
            VisualElement under = null;
            int underIdx = -1;
            int total = invSlots.Count + invHotbarSlots.Count;
            for (int k = 0; k < total; k++)
            {
                VisualElement el = k < invSlots.Count ? invSlots[k] : invHotbarSlots[k - invSlots.Count];
                if (!PadMoveSlotUsable(el, out int idx, out Rect r))
                    continue;
                if (pitch <= 0f)
                    pitch = Mathf.Max(r.width, r.height) + 4f;
                if (r.Contains(p)) { under = el; underIdx = idx; }
            }

            if (under != null)
            {
                if (underIdx == src)
                    return null; // over its own slot: stays put
                if (boundInventory.CanAcceptItemAt(underIdx, from.item, showLevelToast: false))
                {
                    dest = underIdx; // empty = drop, occupied = swap/merge (exactly on it)
                    return under;
                }
            }

            int radius = DMJournalPadProfile.Active.snapRadiusSlots;
            if (radius <= 0 || pitch <= 0f)
                return null;
            float maxDist = pitch * (radius + 0.5f);
            float best = float.MaxValue;
            VisualElement bestEl = null;
            for (int k = 0; k < total; k++)
            {
                VisualElement el = k < invSlots.Count ? invSlots[k] : invHotbarSlots[k - invSlots.Count];
                if (!PadMoveSlotUsable(el, out int idx, out Rect r) || idx == src)
                    continue;
                InventorySystem.InventorySlot s = boundInventory.slots[idx];
                if (s != null && !s.IsEmpty)
                    continue;
                float d = (r.center - p).magnitude;
                if (d > maxDist || d >= best)
                    continue;
                if (!boundInventory.CanAcceptItemAt(idx, from.item, showLevelToast: false))
                    continue;
                best = d;
                bestEl = el;
                dest = idx;
            }
            return bestEl;
        }

        private bool PadMoveSlotUsable(VisualElement el, out int idx, out Rect r)
        {
            idx = -1;
            r = default;
            if (el == null || el.userData is not int i || el.resolvedStyle.display == DisplayStyle.None)
                return false;
            if (i < 0 || i >= boundInventory.slots.Count)
                return false;
            if (i < boundInventory.inventorySize && !boundInventory.IsMainSlotUnlocked(i))
                return false;
            r = el.worldBound;
            if (r.width < 1f || r.height < 1f)
                return false;
            idx = i;
            return true;
        }

        private void SetPadMoveTarget(VisualElement target)
        {
            if (target == padMoveTarget)
                return;
            if (padMoveTarget != null)
            {
                padMoveTarget.RemoveFromClassList(PadMoveTargetClass);
                IStyle s = padMoveTarget.style;
                s.borderTopColor = s.borderBottomColor = s.borderLeftColor = s.borderRightColor = StyleKeyword.Null;
                s.borderTopWidth = s.borderBottomWidth = s.borderLeftWidth = s.borderRightWidth = StyleKeyword.Null;
            }
            padMoveTarget = target;
            if (target == null)
                return;
            DMJournalPadProfile prof = DMJournalPadProfile.Active;
            target.AddToClassList(PadMoveTargetClass);
            IStyle t = target.style;
            t.borderTopColor = t.borderBottomColor = t.borderLeftColor = t.borderRightColor = prof.targetHighlight;
            t.borderTopWidth = t.borderBottomWidth = t.borderLeftWidth = t.borderRightWidth = prof.targetBorderWidth;
        }
    }
}

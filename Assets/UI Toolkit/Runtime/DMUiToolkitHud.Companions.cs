using Project.Companions;
using Project.Pioneers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK expedition trio cluster on the gameplay HUD. Data-binds roster + CompanionHealth only.
    /// Does not spawn companions or touch animators.
    /// </summary>
    public partial class DMUiToolkitHud
    {
        private const int CompanionHudSlots = PioneerRosterManager.ExpeditionTrioSize;

        private readonly CompanionHudSlot[] companionSlots = new CompanionHudSlot[CompanionHudSlots];
        private readonly string[] lastCompanionIds = new string[CompanionHudSlots];
        private readonly int[] lastCompanionHp = new int[CompanionHudSlots];
        private readonly int[] lastCompanionMax = new int[CompanionHudSlots];

        private PioneerRosterManager companionRoster;
        private bool companionRosterHooked;
        private bool companionHealthHooked;
        private bool companionHudBound;

        private void BindCompanionHud(VisualElement root)
        {
            if (root == null)
                return;

            for (int i = 0; i < CompanionHudSlots; i++)
            {
                VisualElement host = root.Q<VisualElement>("companion-slot-" + i);
                if (host == null)
                    continue;

                CompanionHudSlot slot = companionSlots[i] ?? new CompanionHudSlot();
                slot.Index = i;
                slot.Host = host;
                slot.Health = host.Q<VisualElement>("companion-health-" + i) ?? root.Q<VisualElement>("companion-health-" + i);
                slot.Icon = host.Q<VisualElement>("companion-icon-" + i) ?? root.Q<VisualElement>("companion-icon-" + i);
                slot.Name = host.Q<Label>("companion-name-" + i) ?? root.Q<Label>("companion-name-" + i);
                if (slot.Initials == null)
                {
                    Label initials = host.Q<Label>("companion-initials-" + i);
                    if (initials == null)
                    {
                        initials = new Label();
                        initials.name = "companion-initials-" + i;
                        initials.AddToClassList("dmg-hud-companion-name");
                        initials.pickingMode = PickingMode.Ignore;
                        initials.style.fontSize = 18;
                        initials.style.unityTextAlign = TextAnchor.MiddleCenter;
                        host.Add(initials);
                    }
                    slot.Initials = initials;
                }

                host.pickingMode = PickingMode.Position;
                host.UnregisterCallback<PointerEnterEvent>(OnCompanionHudEnter);
                host.UnregisterCallback<PointerLeaveEvent>(OnCompanionHudLeave);
                host.RegisterCallback<PointerEnterEvent>(OnCompanionHudEnter);
                host.RegisterCallback<PointerLeaveEvent>(OnCompanionHudLeave);
                host.userData = i;
                companionSlots[i] = slot;
            }

            companionHudBound = companionSlots[0] != null && companionSlots[0].Host != null;
            HookCompanionRoster();
            ForceRefreshCompanionHud();
        }

        private void UnbindCompanionHud()
        {
            if (companionRoster != null && companionRosterHooked)
            {
                companionRoster.OnTrioChanged -= HandleCompanionTrioChanged;
                companionRoster.OnRosterChanged -= HandleCompanionTrioChanged;
            }

            companionRosterHooked = false;
            companionRoster = null;

            if (companionHealthHooked)
            {
                CompanionHealth.AnyHealthChanged -= HandleAnyCompanionHealthChanged;
                companionHealthHooked = false;
            }

            for (int i = 0; i < CompanionHudSlots; i++)
            {
                CompanionHudSlot slot = companionSlots[i];
                if (slot != null && slot.Host != null)
                {
                    slot.Host.UnregisterCallback<PointerEnterEvent>(OnCompanionHudEnter);
                    slot.Host.UnregisterCallback<PointerLeaveEvent>(OnCompanionHudLeave);
                    slot.Host.userData = null;
                }

                companionSlots[i] = null;
                lastCompanionIds[i] = null;
                lastCompanionHp[i] = -1;
                lastCompanionMax[i] = -1;
            }

            companionHudBound = false;
        }

        private void HookCompanionRoster()
        {
            companionRoster ??= PioneerRosterManager.Instance ?? PioneerRosterManager.EnsureExists();
            if (companionRoster == null || companionRosterHooked)
                return;

            companionRoster.OnTrioChanged -= HandleCompanionTrioChanged;
            companionRoster.OnRosterChanged -= HandleCompanionTrioChanged;
            companionRoster.OnTrioChanged += HandleCompanionTrioChanged;
            companionRoster.OnRosterChanged += HandleCompanionTrioChanged;
            companionRosterHooked = true;

            if (!companionHealthHooked)
            {
                CompanionHealth.AnyHealthChanged -= HandleAnyCompanionHealthChanged;
                CompanionHealth.AnyHealthChanged += HandleAnyCompanionHealthChanged;
                companionHealthHooked = true;
            }
        }

        private void HandleCompanionTrioChanged()
        {
            ForceRefreshCompanionHud();
        }

        private void HandleAnyCompanionHealthChanged(CompanionHealth health, float current, float max)
        {
            TickCompanionHud();
        }

        private void TickCompanionHud()
        {
            if (!companionHudBound || !gameplayVisible)
                return;

            HookCompanionRoster();
            if (companionRoster == null)
                return;

            bool dirty = false;
            for (int i = 0; i < CompanionHudSlots; i++)
            {
                SkilledPioneerRecord record = companionRoster.GetExpeditionTrioRecordAtSlot(i);
                string id = record != null ? record.id : null;
                int hp = 0;
                int max = 0;
                if (!string.IsNullOrEmpty(id))
                    CompanionHealthLookup.TryGetDisplayedHealth(id, out hp, out max);

                if (id != lastCompanionIds[i] || hp != lastCompanionHp[i] || max != lastCompanionMax[i])
                {
                    dirty = true;
                    break;
                }
            }

            if (dirty)
                PaintCompanionHud();
        }

        private void ForceRefreshCompanionHud()
        {
            for (int i = 0; i < CompanionHudSlots; i++)
            {
                lastCompanionIds[i] = null;
                lastCompanionHp[i] = -1;
                lastCompanionMax[i] = -1;
            }

            PaintCompanionHud();
        }

        private void PaintCompanionHud()
        {
            if (!companionHudBound)
                return;

            companionRoster ??= PioneerRosterManager.Instance ?? PioneerRosterManager.EnsureExists();

            for (int i = 0; i < CompanionHudSlots; i++)
            {
                CompanionHudSlot slot = companionSlots[i];
                if (slot == null || slot.Host == null)
                    continue;

                SkilledPioneerRecord record = companionRoster != null
                    ? companionRoster.GetExpeditionTrioRecordAtSlot(i)
                    : null;
                string id = record != null ? record.id : null;
                int hp = 0;
                int max = 0;
                if (!string.IsNullOrEmpty(id))
                    CompanionHealthLookup.TryGetDisplayedHealth(id, out hp, out max);

                lastCompanionIds[i] = id;
                lastCompanionHp[i] = hp;
                lastCompanionMax[i] = max;

                bool filled = record != null;
                slot.Host.style.display = filled ? DisplayStyle.Flex : DisplayStyle.None;
                slot.Host.pickingMode = filled ? PickingMode.Position : PickingMode.Ignore;
                if (!filled)
                {
                    if (slot.Icon != null)
                    {
                        DMUiToolkitStyle.ClearBackgroundImage(slot.Icon);
                        slot.Icon.style.backgroundColor = Color.clear;
                    }

                    if (slot.Name != null)
                        slot.Name.text = string.Empty;
                    if (slot.Initials != null)
                        slot.Initials.text = string.Empty;
                    if (slot.Health != null)
                        slot.Health.style.height = Length.Percent(0f);
                    continue;
                }

                Sprite sprite = PioneerPortraitResolver.Resolve(record);
                if (slot.Icon != null)
                {
                    if (DMUiToolkitStyle.TrySetSpriteBackground(slot.Icon, sprite, ScaleMode.ScaleToFit))
                        slot.Icon.style.backgroundColor = Color.clear;
                    else
                    {
                        DMUiToolkitStyle.ClearBackgroundImage(slot.Icon);
                        slot.Icon.style.backgroundColor = DarkMatterGenesisUiPalette.SlateGray;
                    }
                }

                if (slot.Initials != null)
                    slot.Initials.text = sprite != null
                        ? string.Empty
                        : PioneerPortraitUi.BuildInitials(PioneerUiLabels.GetDisplayName(record));

                if (slot.Name != null)
                    slot.Name.text = PioneerUiLabels.GetDisplayName(record);

                if (slot.Health != null)
                {
                    float pct = max > 0 ? Mathf.Clamp01((float)hp / max) : 0f;
                    slot.Health.style.height = Length.Percent(Mathf.Lerp(8f, 42f, pct));
                }
            }
        }

        private void OnCompanionHudEnter(PointerEnterEvent evt)
        {
            if (evt.currentTarget is not VisualElement host || host.userData is not int index)
                return;
            if (companionRoster == null)
                companionRoster = PioneerRosterManager.Instance ?? PioneerRosterManager.EnsureExists();
            SkilledPioneerRecord record = companionRoster != null
                ? companionRoster.GetExpeditionTrioRecordAtSlot(index)
                : null;
            if (record == null)
                return;

            PioneerHoverTooltip.HideAny();
            Vector2 screen = Vector2.zero;
            if (UnityEngine.InputSystem.Mouse.current != null)
                screen = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            else if (UnityEngine.InputSystem.Pointer.current != null)
                screen = UnityEngine.InputSystem.Pointer.current.position.ReadValue();
            DMUiToolkitWorldMenus.TryShowPioneerHover(record, screen);
        }

        private void OnCompanionHudLeave(PointerLeaveEvent evt)
        {
            PioneerHoverTooltip.HideAny();
            DMUiToolkitWorldMenus.HidePioneerHover();
        }

        private sealed class CompanionHudSlot
        {
            public int Index;
            public VisualElement Host;
            public VisualElement Health;
            public VisualElement Icon;
            public Label Name;
            public Label Initials;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Text;
using Project.Building;
using Project.Data;
using Project.Pioneers;
using Project.Survival.Exposure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    // Expedition trio: slot assignment commands (drag/drop, click-to-cycle, context menu actions),
    // the trio picker refresh, per-slot mini loadout panels, and the live exposure buff/debuff
    // status readout with its peak-detection flash animation. Split out of PioneerRosterPanelUI.cs
    // — this is the single largest cluster of related behavior in the original file.
    public partial class PioneerRosterPanelUI
    {
        internal void HandlePioneerDroppedOnTrioSlot(int slotIndex, string pioneerId)
        {
            SkilledPioneerRecord record = roster.FindSkilledById(pioneerId);
            if (record == null)
                return;

            int sourceSlot = PioneerRosterDragState.SourceTrioSlot;
            if (sourceSlot >= 0 && sourceSlot != slotIndex)
            {
                string displacedId = trioDraft[slotIndex];
                trioDraft[slotIndex] = pioneerId;
                trioDraft[sourceSlot] = displacedId ?? string.Empty;
                pendingTrioSlot = -1;
                selectedPioneerId = pioneerId;
                CommitTrioDraft($"Swapped slot {sourceSlot + 1} ↔ {slotIndex + 1}.");
                return;
            }

            AssignToTrioSlot(slotIndex, record);
        }

        internal void ClearTrioSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= PioneerRosterManager.ExpeditionTrioSize)
                return;

            trioDraft[slotIndex] = string.Empty;
            CommitTrioDraft($"Cleared slot {slotIndex + 1}.");
        }

        internal void BeginPendingTrioSlot(int slotIndex)
        {
            pendingTrioSlot = slotIndex;
            trioStatusLabel.text = $"Select a roster companion for slot {slotIndex + 1}.";
            trioStatusLabel.color = DarkMatterGenesisUiPalette.HighlightText;
            RefreshTrioPicker();
        }

        internal void SlotPioneerToFirstEmpty(string pioneerId)
        {
            SkilledPioneerRecord record = roster.FindSkilledById(pioneerId);
            if (record == null || !roster.CanJoinTrio(record))
                return;

            for (int i = 0; i < PioneerRosterManager.ExpeditionTrioSize; i++)
            {
                if (!string.IsNullOrWhiteSpace(trioDraft[i]))
                    continue;

                AssignToTrioSlot(i, record);
                return;
            }

            trioStatusLabel.text = "All trio slots are filled. Unslot one first.";
            trioStatusLabel.color = DarkMatterGenesisUiPalette.WarningText;
        }

        internal void AssignPioneerToTrioSlot(int slotIndex, string pioneerId)
        {
            SkilledPioneerRecord record = roster.FindSkilledById(pioneerId);
            if (record != null)
                AssignToTrioSlot(slotIndex, record);
        }

        internal void UnslotTrioSlot(int slotIndex)
        {
            ClearTrioSlot(slotIndex);
        }

        internal void TransmuteTrioSlot(int slotIndex)
        {
            List<SkilledPioneerRecord> eligible = GetEligibleTrioPioneers();
            if (eligible.Count == 0)
                return;

            string currentId = trioDraft[slotIndex];
            int currentIndex = -1;
            for (int i = 0; i < eligible.Count; i++)
            {
                if (eligible[i].id == currentId)
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = (currentIndex + 1) % eligible.Count;
            trioDraft[slotIndex] = eligible[nextIndex].id;
            CommitTrioDraft($"Transmuted slot {slotIndex + 1} to {eligible[nextIndex].displayName}.");
        }

        internal void TransmutePioneerLoadout(string pioneerId)
        {
            SkilledPioneerRecord record = roster.FindSkilledById(pioneerId);
            if (record == null)
                return;

            string nextWeapon = CycleWeaponLoadoutItem(record.weaponItemId);
            string nextTool = CycleLoadoutItem(record.toolItemId, ItemType.Tool, allowEmpty: true);
            string[] nextSkills = record.assignedSkillIds;
            if (record.learnedSkills != null && record.learnedSkills.Length > 0)
            {
                string current = nextSkills != null && nextSkills.Length > 0 ? nextSkills[0] : string.Empty;
                int index = System.Array.IndexOf(record.learnedSkills, current);
                index = index < 0 ? 0 : (index + 1) % record.learnedSkills.Length;
                nextSkills = new[] { record.learnedSkills[index] };
            }

            roster.TrySetPioneerLoadout(record.id, nextWeapon, nextTool, nextSkills, out _);
            selectedPioneerId = pioneerId;
            Refresh();
        }

        private void CommitTrioDraft(string successMessage = null)
        {
            if (roster.TrySetExpeditionTrio(trioDraft, out string error))
            {
                trioStatusLabel.text = successMessage ?? "Expedition trio updated.";
                trioStatusLabel.color = DarkMatterGenesisUiPalette.PositiveGreen;
            }
            else
            {
                trioStatusLabel.text = string.IsNullOrEmpty(error) ? "Could not update trio." : error;
                trioStatusLabel.color = DarkMatterGenesisUiPalette.WarningText;
            }

            Refresh();
        }

        private void SyncTrioDraftFromRoster()
        {
            for (int i = 0; i < PioneerRosterManager.ExpeditionTrioSize; i++)
                trioDraft[i] = roster.GetExpeditionTrioIdAtSlot(i) ?? string.Empty;

            if (string.IsNullOrEmpty(selectedPioneerId) && roster.SkilledPioneers.Count > 0)
                selectedPioneerId = roster.SkilledPioneers[0].id;
        }

        private void AssignToTrioSlot(int slotIndex, SkilledPioneerRecord record)
        {
            if (record == null || !roster.CanJoinTrio(record))
            {
                trioStatusLabel.text = $"{record?.displayName ?? "Companion"} cannot join the expedition trio.";
                trioStatusLabel.color = DarkMatterGenesisUiPalette.WarningText;
                return;
            }

            for (int i = 0; i < trioDraft.Length; i++)
            {
                if (i != slotIndex && trioDraft[i] == record.id)
                    trioDraft[i] = string.Empty;
            }

            trioDraft[slotIndex] = record.id;
            pendingTrioSlot = -1;
            selectedPioneerId = record.id;
            CommitTrioDraft($"Assigned {PioneerUiLabels.GetDisplayName(record)} to slot {slotIndex + 1}.");
        }

        private void HandleTrioSlotClicked(int slotIndex)
        {
            string assignedId = trioDraft[slotIndex];
            if (!string.IsNullOrEmpty(assignedId))
            {
                selectedPioneerId = assignedId;
                pendingTrioSlot = -1;
                RefreshRosterList();
                RefreshDetailPanel();
                RefreshLoadoutPanel();
                RefreshTrioPicker();
                return;
            }

            if (pendingTrioSlot == slotIndex)
            {
                CycleTrioSlot(slotIndex);
                return;
            }

            pendingTrioSlot = slotIndex;
            trioStatusLabel.text = $"Select a roster companion for slot {slotIndex + 1}, or click the slot again to cycle.";
            trioStatusLabel.color = DarkMatterGenesisUiPalette.HighlightText;
            RefreshTrioPicker();
        }

        private void HandleTrioLoadoutPanelClicked(int slotIndex)
        {
            string assignedId = trioDraft[slotIndex];
            if (string.IsNullOrEmpty(assignedId))
                return;

            selectedPioneerId = assignedId;
            pendingTrioSlot = -1;
            RefreshRosterList();
            RefreshDetailPanel();
            RefreshLoadoutPanel();
            RefreshTrioPicker();
        }

        private void CycleTrioSlot(int slotIndex)
        {
            List<SkilledPioneerRecord> eligible = GetEligibleTrioPioneers();
            if (eligible.Count == 0)
            {
                trioStatusLabel.text = "No eligible companions for expedition trio.";
                trioStatusLabel.color = DarkMatterGenesisUiPalette.WarningText;
                return;
            }

            string currentId = trioDraft[slotIndex];
            int currentIndex = -1;
            for (int i = 0; i < eligible.Count; i++)
            {
                if (eligible[i].id == currentId)
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = (currentIndex + 1) % eligible.Count;
            SkilledPioneerRecord next = eligible[nextIndex];
            trioDraft[slotIndex] = next.id;
            pendingTrioSlot = -1;
            CommitTrioDraft($"Cycled slot {slotIndex + 1} to {PioneerUiLabels.GetDisplayName(next)}.");
        }

        private List<SkilledPioneerRecord> GetEligibleTrioPioneers()
        {
            List<SkilledPioneerRecord> eligible = new List<SkilledPioneerRecord>();
            for (int i = 0; i < roster.SkilledPioneers.Count; i++)
            {
                SkilledPioneerRecord record = roster.SkilledPioneers[i];
                if (record != null && roster.CanJoinTrio(record))
                    eligible.Add(record);
            }

            return eligible;
        }

        private void RefreshTrioPicker()
        {
            for (int i = 0; i < PioneerRosterManager.ExpeditionTrioSize; i++)
            {
                bool pending = pendingTrioSlot == i;
                SkilledPioneerRecord assigned = roster.FindSkilledById(trioDraft[i]);
                string slotName = assigned != null ? PioneerUiLabels.GetDisplayName(assigned) : "Empty";
                trioSlotLabels[i].text = $"Slot {i + 1}\n{slotName}";

                Image slotImage = trioSlotButtons[i].GetComponent<Image>();
                slotImage.color = pending
                    ? DarkMatterGenesisUiPalette.ButtonHighlighted
                    : assigned != null
                        ? DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.RichFuchsia, 0.85f)
                        : DarkMatterGenesisUiPalette.SlotBackground;

                trioSlotLabels[i].color = assigned != null || pending
                    ? DarkMatterGenesisUiPalette.Gold
                    : DarkMatterGenesisUiPalette.WarmOffWhite;
            }

            if (pendingTrioSlot < 0)
            {
                int active = roster.GetActiveExpeditionTrioCount();
                trioStatusLabel.text = active == 0
                    ? "Drag or right-click companions into trio slots (1–3 active)."
                    : $"{active} companion(s) active. Right-click slots to unslot or transmute.";
                trioStatusLabel.color = DarkMatterGenesisUiPalette.Gold;
            }
        }

        private void RefreshTrioLoadoutPanels()
        {
            for (int i = 0; i < PioneerRosterManager.ExpeditionTrioSize; i++)
            {
                SkilledPioneerRecord record = roster.FindSkilledById(trioDraft[i]);
                if (trioLoadoutLabels[i] == null)
                    continue;

                if (record == null)
                {
                    trioLoadoutLabels[i].text = $"Slot {i + 1} — Empty";
                    SetTrioLoadoutButtonLabel(trioLoadoutWeaponButtons[i], "Wpn", "—");
                    SetTrioLoadoutButtonLabel(trioLoadoutToolButtons[i], "Tool", "—");
                    SetTrioLoadoutButtonLabel(trioLoadoutSkillButtons[i], "Skl", "—");
                    continue;
                }

                PioneerLoadoutDefaults.EnsureDefaults(record);
                ItemData weapon = ItemRegistry.Resolve(record.weaponItemId);
                ItemData tool = ItemRegistry.Resolve(record.toolItemId);
                string skill = record.assignedSkillIds != null && record.assignedSkillIds.Length > 0
                    ? record.assignedSkillIds[0]
                    : "None";

                trioLoadoutLabels[i].text = $"Slot {i + 1} — {PioneerUiLabels.GetDisplayName(record)}";
                SetTrioLoadoutButtonLabel(trioLoadoutWeaponButtons[i], "Wpn", weapon != null ? weapon.itemName : record.weaponItemId);
                SetTrioLoadoutButtonLabel(trioLoadoutToolButtons[i], "Tool", tool != null ? tool.itemName : (string.IsNullOrEmpty(record.toolItemId) ? "None" : record.toolItemId));
                SetTrioLoadoutButtonLabel(trioLoadoutSkillButtons[i], "Skl", skill);
            }
        }

        /// <summary>
        /// Per-trio-slot specs (class/level/core stats) plus live buffs and debuffs, sourced from
        /// ExposureStatusService.Current.ExpeditionCompanionSlots — the same per-slot data the HUD
        /// modifier ticks use, so this always matches what's actively affecting each companion.
        /// </summary>
        private void RefreshTrioStatusPanels()
        {
            CompanionExposureModifierSlot[] slots = ExposureStatusService.Current?.ExpeditionCompanionSlots;

            for (int i = 0; i < PioneerRosterManager.ExpeditionTrioSize; i++)
            {
                SkilledPioneerRecord record = roster.FindSkilledById(trioDraft[i]);
                CompanionExposureModifierSlot slot = slots != null && i < slots.Length ? slots[i] : null;

                if (trioSpecsLabels[i] != null)
                {
                    trioSpecsLabels[i].text = record == null
                        ? "Specs: —"
                        : $"{SkilledPioneerClassUtility.ToDisplayName(record.pioneerClass)} · Lv {record.level}  ·  " +
                          $"Rad {record.radiationResistance:P0} · Exp {record.expeditionEfficiency:P0} · Syn {record.combatSynergy:P0}";
                    trioSpecsLabels[i].color = DarkMatterGenesisUiPalette.Gold;
                }

                if (trioBuffLabels[i] != null)
                {
                    // Combine the companion's authored data-asset buffs (CompanionBuffModifier,
                    // always-on) with the live exposure-tick buffs (only present while a mitigating
                    // effect is actively countering a hazard) into one line.
                    string buffText = FormatBuffs(record?.buffs, record != null ? slot?.BuffTicks : null);
                    trioBuffLabels[i].text = $"Buffs: {buffText}";
                    trioBuffLabels[i].color = buffText == "—"
                        ? DarkMatterGenesisUiPalette.Gold
                        : DarkMatterGenesisUiPalette.PositiveGreen;
                }

                Color debuffActiveColor = DarkMatterGenesisUiPalette.Gold;
                if (trioDebuffLabels[i] != null)
                {
                    string debuffText = FormatTicks(record != null ? slot?.DebuffTicks : null);
                    trioDebuffLabels[i].text = $"Debuffs: {debuffText}";
                    debuffActiveColor = debuffText == "—"
                        ? DarkMatterGenesisUiPalette.Gold
                        : DarkMatterGenesisUiPalette.WarningText;
                    if (!trioIsFlashing[i])
                        trioDebuffLabels[i].color = debuffActiveColor;
                }

                float exposureLevel = record != null && slot != null ? Mathf.Clamp01(slot.ExposureLevel) : 0f;
                DetectTrioPeakAndFlash(i, exposureLevel, debuffActiveColor);
            }
        }

        /// <summary>
        /// Mirrors VerticalHazardExposureGauge's climb/settle detection for this companion's
        /// combined exposure level — fires a triple flash on the Debuffs line the instant the level
        /// stops climbing and holds at its cap (the hazard cap was reached and the debuff landed).
        /// The level does NOT auto-drain — it only goes back down via leaving the zone or a
        /// mitigation source (companion buff, later food/inoculation). Any real decline re-arms the
        /// flash so re-entering the same zone always flashes again.
        /// </summary>
        private void DetectTrioPeakAndFlash(int slotIndex, float level, Color debuffActiveColor)
        {
            float delta = level - trioLastExposureLevel[slotIndex];
            bool rising = delta > TrioSettleRiseEpsilon;
            bool falling = delta < -TrioSettleRiseEpsilon;

            if (falling)
                trioHasFlashed[slotIndex] = false;

            if (level < TrioSettleResetThreshold)
            {
                trioHasFlashed[slotIndex] = false;
                trioWasRising[slotIndex] = false;
                trioLastExposureLevel[slotIndex] = level;
                return;
            }

            if (trioWasRising[slotIndex] && !rising && !falling && level >= TrioSettleActiveThreshold && !trioHasFlashed[slotIndex])
            {
                trioHasFlashed[slotIndex] = true;
                PlayTrioDebuffFlash(slotIndex, debuffActiveColor);
            }

            trioWasRising[slotIndex] = rising;
            trioLastExposureLevel[slotIndex] = level;
        }

        private void PlayTrioDebuffFlash(int slotIndex, Color activeColor)
        {
            if (trioDebuffLabels[slotIndex] == null)
                return;

            if (trioFlashRoutines[slotIndex] != null)
                StopCoroutine(trioFlashRoutines[slotIndex]);

            trioFlashRoutines[slotIndex] = StartCoroutine(TrioDebuffFlashRoutine(slotIndex, activeColor));
        }

        private IEnumerator TrioDebuffFlashRoutine(int slotIndex, Color activeColor)
        {
            trioIsFlashing[slotIndex] = true;
            TextMeshProUGUI label = trioDebuffLabels[slotIndex];

            float t = 0f;
            while (t < TrioFlashDuration)
            {
                t += Time.deltaTime;
                float pop = Mathf.Abs(Mathf.Sin(Mathf.Clamp01(t / TrioFlashDuration) * Mathf.PI * TrioFlashCycles));
                if (label != null)
                    label.color = Color.Lerp(activeColor, Color.white, pop);

                yield return null;
            }

            if (label != null)
                label.color = activeColor;

            trioIsFlashing[slotIndex] = false;
            trioFlashRoutines[slotIndex] = null;
        }

        private static string FormatTicks(ExposureModifierTick[] ticks)
        {
            if (ticks == null || ticks.Length == 0)
                return "—";

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < ticks.Length; i++)
            {
                if (i > 0)
                    builder.Append("  ·  ");
                builder.Append(ticks[i].Label);
            }

            return builder.ToString();
        }

        private static string FormatBuffs(CompanionBuffModifier[] dataBuffs, ExposureModifierTick[] liveTicks)
        {
            StringBuilder builder = new StringBuilder();

            if (dataBuffs != null)
            {
                for (int i = 0; i < dataBuffs.Length; i++)
                {
                    if (dataBuffs[i] == null || string.IsNullOrWhiteSpace(dataBuffs[i].label))
                        continue;

                    if (builder.Length > 0)
                        builder.Append("  ·  ");
                    builder.Append(dataBuffs[i].label);
                }
            }

            if (liveTicks != null)
            {
                for (int i = 0; i < liveTicks.Length; i++)
                {
                    if (builder.Length > 0)
                        builder.Append("  ·  ");
                    builder.Append(liveTicks[i].Label);
                }
            }

            return builder.Length > 0 ? builder.ToString() : "—";
        }

        private static void SetTrioLoadoutButtonLabel(Button button, string prefix, string value)
        {
            if (button == null)
                return;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = $"{prefix}\n{value}";
        }

        private void CycleTrioSlotWeapon(int slotIndex)
        {
            SkilledPioneerRecord record = roster.FindSkilledById(trioDraft[slotIndex]);
            if (record == null)
                return;

            string nextId = CycleWeaponLoadoutItem(record.weaponItemId);
            roster.TrySetPioneerLoadout(record.id, nextId, record.toolItemId, record.assignedSkillIds, out _);
            RefreshTrioLoadoutPanels();
        }

        private void CycleTrioSlotTool(int slotIndex)
        {
            SkilledPioneerRecord record = roster.FindSkilledById(trioDraft[slotIndex]);
            if (record == null)
                return;

            string nextId = CycleLoadoutItem(record.toolItemId, ItemType.Tool, allowEmpty: true);
            roster.TrySetPioneerLoadout(record.id, record.weaponItemId, nextId, record.assignedSkillIds, out _);
            RefreshTrioLoadoutPanels();
        }

        private void CycleTrioSlotSkill(int slotIndex)
        {
            SkilledPioneerRecord record = roster.FindSkilledById(trioDraft[slotIndex]);
            if (record == null || record.learnedSkills == null || record.learnedSkills.Length == 0)
                return;

            string current = record.assignedSkillIds != null && record.assignedSkillIds.Length > 0
                ? record.assignedSkillIds[0]
                : string.Empty;
            int index = System.Array.IndexOf(record.learnedSkills, current);
            index = index < 0 ? 0 : (index + 1) % record.learnedSkills.Length;
            string[] nextSkills = { record.learnedSkills[index] };
            roster.TrySetPioneerLoadout(record.id, record.weaponItemId, record.toolItemId, nextSkills, out _);
            RefreshTrioLoadoutPanels();
        }
    }
}

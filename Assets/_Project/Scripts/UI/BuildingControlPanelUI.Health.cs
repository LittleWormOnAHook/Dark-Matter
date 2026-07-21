using System;
using System.Collections.Generic;
using Project.Building;
using Project.Companions;
using Project.Core;
using Project.Crafting;
using Project.Inventory;
using Project.Pioneers;
using Project.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Project.UI
{
    // Health tab: the injured-pioneer roster list, its PioneerRosterManager.OnRosterChanged
    // subscription, and the reassign-from-recovery flow invoked by ScienceLabHealthContextMenu/
    // ScienceLabHealthRowHandler. Split out of BuildingControlPanelUI.cs so the roster-event
    // lifecycle (subscribe/unsubscribe) is easy to audit in one place.
    public partial class BuildingControlPanelUI
    {
        internal void TryReassignInjuredPioneer(string pioneerId)
        {
            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            if (!roster.TryRecoverSkilledFromLab(pioneerId, out string message))
            {
                if (healthStatusLabel != null)
                {
                    healthStatusLabel.text = message;
                    healthStatusLabel.color = SurvivalPioneerUiPalette.WarningText;
                }

                return;
            }

            CompanionRosterBridge bridge = FindAnyObjectByType<CompanionRosterBridge>();
            bridge?.RefreshCompanions();

            if (healthStatusLabel != null)
            {
                healthStatusLabel.text = message;
                healthStatusLabel.color = SurvivalPioneerUiPalette.BodyText;
            }

            RefreshHealthTab();
        }

        private void EnsureHealthRosterSubscription()
        {
            if (healthRosterSubscribed)
                return;

            healthRoster = PioneerRosterManager.EnsureExists();
            if (healthRoster == null)
                return;

            healthRoster.OnRosterChanged += HandleHealthRosterChanged;
            healthRosterSubscribed = true;
        }

        /// <summary>
        /// Defensive unsubscribe. EnsureHealthRosterSubscription() already guards against
        /// double-subscribing, so this isn't fixing an active leak, but it stops this panel from
        /// holding a live reference in PioneerRosterManager's event after the panel itself is gone.
        /// </summary>
        private void OnDestroy()
        {
            if (healthRosterSubscribed && healthRoster != null)
                healthRoster.OnRosterChanged -= HandleHealthRosterChanged;

            healthRosterSubscribed = false;
        }

        private void HandleHealthRosterChanged()
        {
            if (!IsOpen || activeTab != BuildingControlTab.Health)
                return;

            RefreshHealthTab();
        }

        private void TickLiveHealthTab()
        {
            if (activePanel == null || activeTab != BuildingControlTab.Health || !IsActiveScienceLab())
                return;

            RefreshHealthTab();
        }

        private void RefreshHealthTab()
        {
            if (healthListParent == null)
                return;

            for (int i = healthListParent.childCount - 1; i >= 0; i--)
                Destroy(healthListParent.GetChild(i).gameObject);

            if (!IsActiveScienceLab())
                return;

            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            if (roster == null)
                return;

            List<SkilledPioneerRecord> injured = roster.GetInjuredSkilledPioneers();
            if (injured.Count == 0)
            {
                CreateHealthInfoRow("No pioneers are recovering here.");
                if (healthStatusLabel != null)
                    healthStatusLabel.text = "All expedition pioneers are field-ready.";
                return;
            }

            for (int i = 0; i < injured.Count; i++)
                CreateHealthRow(injured[i], roster);

            if (healthStatusLabel != null)
                healthStatusLabel.text = $"{injured.Count} pioneer(s) in recovery. Reassign when the timer reaches 0s.";
        }

        private void CreateHealthInfoRow(string message)
        {
            ShiftUiTheme theme = ShiftUiTheme.Current;
            TextMeshProUGUI label = CreateBodyText(healthListParent, theme, 18f);
            label.text = message;
            label.color = SurvivalPioneerUiPalette.MutedText;
        }

        private void CreateHealthRow(SkilledPioneerRecord record, PioneerRosterManager roster)
        {
            GameObject row = new GameObject($"Injured_{record.id}", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(LayoutElement));
            row.transform.SetParent(healthListParent, false);

            UnityEngine.UI.Image bg = row.GetComponent<UnityEngine.UI.Image>();
            MenuUiBuilder.ApplyUiSprite(bg);
            bg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.95f);

            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minHeight = 56f;
            rowLayout.preferredHeight = 56f;

            float remaining = roster.GetInjuryRecoveryRemaining(record);
            bool ready = remaining <= 0.5f;
            string status = ready
                ? "Ready to reassign"
                : $"Recovering ({Mathf.CeilToInt(remaining)}s)";

            GameObject textHost = new GameObject("Label", typeof(RectTransform));
            textHost.transform.SetParent(row.transform, false);
            RectTransform textRect = textHost.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 6f);
            textRect.offsetMax = new Vector2(-12f, -6f);

            TextMeshProUGUI label = textHost.AddComponent<TextMeshProUGUI>();
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(label, semiBold: true);
            else
                TmpUiHelper.ApplyDefaultFont(label);

            label.fontSize = 16f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = ready ? SurvivalPioneerUiPalette.BodyText : SurvivalPioneerUiPalette.MutedText;
            label.text =
                $"<color=#{ColorUtility.ToHtmlStringRGB(SurvivalPioneerUiPalette.RichFuchsia)}>{record.displayName}</color>\n" +
                $"{SkilledPioneerClassUtility.ToDisplayName(record.pioneerClass)}  ·  {status}";

            ScienceLabHealthRowHandler handler = row.AddComponent<ScienceLabHealthRowHandler>();
            handler.Configure(this, record.id);
        }
    }
}

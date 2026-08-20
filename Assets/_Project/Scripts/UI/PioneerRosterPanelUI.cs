using System.Collections;
using System.Collections.Generic;
using System.Text;
using Project.Building;
using Project.Companions;
using Project.Data;
using Project.Pioneers;
using Project.Survival.Exposure;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Journal Pioneers tab: skilled roster list, colonist summary, pioneer detail, expedition trio picker.
    /// Split across partials by responsibility — see PioneerRosterPanelUI.RosterList.cs (the four
    /// grouped browser columns + roster-row selection/drag/tooltip API), .Detail.cs (pioneer detail +
    /// personal loadout), .Trio.cs (expedition trio slot commands, picker, per-slot loadout/status +
    /// exposure-peak flash), .Layout.cs (all runtime UI construction). This file keeps just the
    /// fields and the top-level Embed/Unembed/Refresh lifecycle. Purely a mechanical reorganization
    /// (partial class split) — no behavior changed by the split.
    /// </summary>
    public partial class PioneerRosterPanelUI : MonoBehaviour
    {
        private Transform embeddedParent;
        private GameObject panelRoot;
        private Transform classListParent;
        private Transform echoListParent;
        private Transform trioListParent;
        private Transform campListParent;
        private TextMeshProUGUI colonistSummaryLabel;
        private TextMeshProUGUI detailLabel;
        private RawImage detailPortraitPhoto;
        private Image detailPortraitFrame;
        private TextMeshProUGUI synergyHintLabel;
        private TextMeshProUGUI trioStatusLabel;
        private TextMeshProUGUI loadoutStatusLabel;
        private Button weaponSlotButton;
        private Button toolSlotButton;
        private Button skillSlotButton;
        private TextMeshProUGUI weaponSlotLabel;
        private TextMeshProUGUI toolSlotLabel;
        private TextMeshProUGUI skillSlotLabel;
        private readonly Button[] trioSlotButtons = new Button[PioneerRosterManager.ExpeditionTrioSize];
        private readonly TextMeshProUGUI[] trioSlotLabels = new TextMeshProUGUI[PioneerRosterManager.ExpeditionTrioSize];
        private readonly Button[] trioLoadoutWeaponButtons = new Button[PioneerRosterManager.ExpeditionTrioSize];
        private readonly Button[] trioLoadoutToolButtons = new Button[PioneerRosterManager.ExpeditionTrioSize];
        private readonly Button[] trioLoadoutSkillButtons = new Button[PioneerRosterManager.ExpeditionTrioSize];
        private readonly TextMeshProUGUI[] trioLoadoutLabels = new TextMeshProUGUI[PioneerRosterManager.ExpeditionTrioSize];
        private readonly TextMeshProUGUI[] trioSpecsLabels = new TextMeshProUGUI[PioneerRosterManager.ExpeditionTrioSize];
        private readonly TextMeshProUGUI[] trioBuffLabels = new TextMeshProUGUI[PioneerRosterManager.ExpeditionTrioSize];
        private readonly TextMeshProUGUI[] trioDebuffLabels = new TextMeshProUGUI[PioneerRosterManager.ExpeditionTrioSize];

        // Companion peak-flash tracking, mirroring VerticalHazardExposureGauge's climb/peak/drain
        // detection so trio members flash their debuff line every time their exposure caps out —
        // including every time they (or the player) re-enter the same hazard zone.
        private readonly float[] trioLastExposureLevel = new float[PioneerRosterManager.ExpeditionTrioSize];
        private readonly bool[] trioWasRising = new bool[PioneerRosterManager.ExpeditionTrioSize];
        private readonly bool[] trioHasFlashed = new bool[PioneerRosterManager.ExpeditionTrioSize];
        private readonly Coroutine[] trioFlashRoutines = new Coroutine[PioneerRosterManager.ExpeditionTrioSize];
        private readonly bool[] trioIsFlashing = new bool[PioneerRosterManager.ExpeditionTrioSize];
        private const float TrioSettleRiseEpsilon = 0.0025f;
        private const float TrioSettleActiveThreshold = 0.05f;
        private const float TrioSettleResetThreshold = 0.04f;
        private const float TrioFlashDuration = 0.6f;
        private const float TrioFlashCycles = 3f;

        private PioneerRosterManager roster;
        private ShiftUiTheme theme;
        private string selectedPioneerId;
        private int pendingTrioSlot = -1;
        private readonly string[] trioDraft = new string[PioneerRosterManager.ExpeditionTrioSize];
        private ExposureStatusService subscribedExposureService;
        private bool subscribedToCompanionHealth;

        public void EmbedIn(Transform parent)
        {
            if (parent == null)
                return;

            embeddedParent = parent;
            roster = PioneerRosterManager.EnsureExists();
            theme = ShiftUiTheme.Current;
            EnsureBuilt(parent);

            Canvas canvas = parent.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                PioneerRosterContextMenu.EnsureExists(canvas.transform, this);
                PioneerHoverTooltip.EnsureExists(canvas.transform);
            }

            if (roster != null)
            {
                roster.OnRosterChanged += Refresh;
                roster.OnTrioChanged += Refresh;
            }

            subscribedExposureService = ExposureStatusService.Instance;
            if (subscribedExposureService != null)
                subscribedExposureService.OnSnapshotChanged += HandleExposureSnapshotChanged;

            if (!subscribedToCompanionHealth)
            {
                CompanionHealth.AnyHealthChanged += HandleCompanionHealthChanged;
                subscribedToCompanionHealth = true;
            }

            Refresh();
        }

        private void HandleCompanionHealthChanged(CompanionHealth health, float current, float max)
        {
            if (panelRoot == null)
                return;

            RefreshTrioStatusPanels();

            if (!string.IsNullOrEmpty(selectedPioneerId)
                && health != null
                && health.PioneerRecordId == selectedPioneerId)
            {
                RefreshDetailPanel();
            }
        }

        public void Unembed()
        {
            if (roster != null)
            {
                roster.OnRosterChanged -= Refresh;
                roster.OnTrioChanged -= Refresh;
            }

            if (subscribedExposureService != null)
                subscribedExposureService.OnSnapshotChanged -= HandleExposureSnapshotChanged;
            subscribedExposureService = null;

            if (subscribedToCompanionHealth)
            {
                CompanionHealth.AnyHealthChanged -= HandleCompanionHealthChanged;
                subscribedToCompanionHealth = false;
            }

            for (int i = 0; i < trioFlashRoutines.Length; i++)
            {
                if (trioFlashRoutines[i] != null)
                    StopCoroutine(trioFlashRoutines[i]);
                trioFlashRoutines[i] = null;
                trioIsFlashing[i] = false;
                trioHasFlashed[i] = false;
                trioWasRising[i] = false;
                trioLastExposureLevel[i] = 0f;
            }

            PioneerHoverTooltip.HideAny();

            if (panelRoot != null)
                Destroy(panelRoot);

            panelRoot = null;
            classListParent = null;
            echoListParent = null;
            trioListParent = null;
            campListParent = null;
            colonistSummaryLabel = null;
            detailLabel = null;
            detailPortraitPhoto = null;
            detailPortraitFrame = null;
            synergyHintLabel = null;
            trioStatusLabel = null;
            loadoutStatusLabel = null;
            weaponSlotButton = null;
            toolSlotButton = null;
            skillSlotButton = null;
            weaponSlotLabel = null;
            toolSlotLabel = null;
            skillSlotLabel = null;
            pendingTrioSlot = -1;
            embeddedParent = null;
        }

        public void Refresh()
        {
            if (panelRoot == null || roster == null)
                return;

            theme = ShiftUiTheme.Current;
            SyncTrioDraftFromRoster();
            RefreshColonistSummary();
            RefreshRosterList();
            RefreshDetailPanel();
            RefreshLoadoutPanel();
            RefreshTrioPicker();
            RefreshTrioLoadoutPanels();
            RefreshTrioStatusPanels();
        }

        private void HandleExposureSnapshotChanged(ExposureStatusSnapshot snapshot)
        {
            // Lightweight — only the buff/debuff/specs labels, not a full rebuild — so this can run
            // every time the live exposure snapshot changes without churning the whole tab.
            RefreshTrioStatusPanels();
        }
    }
}

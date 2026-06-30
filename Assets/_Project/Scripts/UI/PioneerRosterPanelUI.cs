using System.Collections.Generic;
using Project.Data;
using Project.Pioneers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Journal Pioneers tab: skilled roster list, colonist summary, pioneer detail, expedition trio picker.
    /// </summary>
    public class PioneerRosterPanelUI : MonoBehaviour
    {
        private Transform embeddedParent;
        private GameObject panelRoot;
        private Transform rosterListParent;
        private TextMeshProUGUI colonistSummaryLabel;
        private TextMeshProUGUI detailLabel;
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

        private PioneerRosterManager roster;
        private ShiftUiTheme theme;
        private string selectedPioneerId;
        private int pendingTrioSlot = -1;
        private readonly string[] trioDraft = new string[PioneerRosterManager.ExpeditionTrioSize];

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
                PioneerRosterContextMenu.EnsureExists(canvas.transform, this);

            if (roster != null)
            {
                roster.OnRosterChanged += Refresh;
                roster.OnTrioChanged += Refresh;
            }

            Refresh();
        }

        public void Unembed()
        {
            if (roster != null)
            {
                roster.OnRosterChanged -= Refresh;
                roster.OnTrioChanged -= Refresh;
            }

            if (panelRoot != null)
                Destroy(panelRoot);

            panelRoot = null;
            rosterListParent = null;
            colonistSummaryLabel = null;
            detailLabel = null;
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
        }

        internal string GetTrioDraftId(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= PioneerRosterManager.ExpeditionTrioSize)
                return string.Empty;

            return trioDraft[slotIndex] ?? string.Empty;
        }

        internal void OnDragStarted(string pioneerId)
        {
            selectedPioneerId = pioneerId;
            RefreshRosterList();
            RefreshDetailPanel();
            RefreshLoadoutPanel();
        }

        internal void OnDragEnded()
        {
            Refresh();
        }

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

        internal void SelectPioneer(string pioneerId)
        {
            selectedPioneerId = pioneerId;
            Refresh();
        }

        internal void BeginPendingTrioSlot(int slotIndex)
        {
            pendingTrioSlot = slotIndex;
            trioStatusLabel.text = $"Select a roster pioneer for slot {slotIndex + 1}.";
            trioStatusLabel.color = SurvivalPioneerUiPalette.HighlightText;
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
            trioStatusLabel.color = SurvivalPioneerUiPalette.WarningText;
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

            string nextWeapon = CycleLoadoutItem(record.weaponItemId, ItemType.MeleeWeapon);
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
                trioStatusLabel.color = SurvivalPioneerUiPalette.PositiveGreen;
            }
            else
            {
                trioStatusLabel.text = string.IsNullOrEmpty(error) ? "Could not update trio." : error;
                trioStatusLabel.color = SurvivalPioneerUiPalette.WarningText;
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

        private void RefreshColonistSummary()
        {
            ColonistAggregateState colonists = roster.GetColonistState();
            int total = roster.GetTotalPioneerCount();
            colonistSummaryLabel.text =
                $"Total {total}/{PioneerRosterManager.MaxTotalPioneers}  ·  " +
                $"Skilled {roster.SkilledPioneers.Count}/{PioneerRosterManager.MaxSkilledPioneers}\n" +
                $"Workers {colonists.workerCount}/{PioneerRosterManager.MaxWorkerPioneers}  ·  " +
                $"Available {colonists.AvailableWorkers}  ·  " +
                $"Injured {colonists.injuredCount}  ·  " +
                $"Sheltered {colonists.shelteredCount}  ·  " +
                $"Assigned {colonists.assignedToFacilityCount}";
        }

        private void RefreshRosterList()
        {
            for (int i = rosterListParent.childCount - 1; i >= 0; i--)
                Destroy(rosterListParent.GetChild(i).gameObject);

            int availableCount = 0;
            for (int i = 0; i < roster.SkilledPioneers.Count; i++)
            {
                SkilledPioneerRecord record = roster.SkilledPioneers[i];
                if (record == null || record.WorkState == PioneerWorkState.Injured)
                    continue;

                availableCount++;
                CreateRosterRow(record);
            }

            if (availableCount == 0)
            {
                CreateRosterInfoRow(
                    roster.SkilledPioneers.Count == 0
                        ? "No skilled pioneers recruited yet."
                        : "No pioneers available. Injured pioneers recover at the Science Lab.");
            }
        }

        private void CreateRosterRow(SkilledPioneerRecord record)
        {
            bool selected = record.id == selectedPioneerId;
            bool inTrio = record.isInExpeditionTrio;
            bool canJoin = roster.CanJoinTrio(record);

            GameObject row = new GameObject($"Pioneer_{record.id}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            row.transform.SetParent(rosterListParent, false);

            Image bg = row.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(bg);
            bg.color = selected
                ? SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.35f)
                : SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.96f);

            if (selected)
                SurvivalPioneerUiPalette.ApplyFuchsiaTrim(row);

            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minHeight = 56f;

            string starterTag = record.isStarterPick ? " [Starter]" : string.Empty;
            string trioTag = inTrio ? "  ·  TRIO" : string.Empty;
            string stateTag = record.WorkState == PioneerWorkState.Injured ? "  ·  INJURED" : string.Empty;

            TextMeshProUGUI label = CreateLabel(row.transform, string.Empty, 13f, semiBold: selected);
            label.color = canJoin ? SurvivalPioneerUiPalette.BodyText : SurvivalPioneerUiPalette.MutedText;
            label.text =
                $"<color=#{ColorUtility.ToHtmlStringRGB(SurvivalPioneerUiPalette.RichFuchsia)}>{record.displayName}</color>{starterTag}{trioTag}{stateTag}\n" +
                $"{SkilledPioneerClassUtility.ToDisplayName(record.pioneerClass)}  ·  Lv {record.level}";
            Stretch(label.rectTransform, 10f, 6f);

            SkilledPioneerRecord captured = record;
            row.GetComponent<Button>().onClick.AddListener(() => HandleRosterEntryClicked(captured));

            PioneerRosterRowDragHandler drag = row.AddComponent<PioneerRosterRowDragHandler>();
            drag.Configure(this, record.id);
        }

        private void HandleRosterEntryClicked(SkilledPioneerRecord record)
        {
            if (record == null)
                return;

            if (pendingTrioSlot >= 0)
            {
                AssignToTrioSlot(pendingTrioSlot, record);
                return;
            }

            selectedPioneerId = record.id;
            RefreshRosterList();
            RefreshDetailPanel();
            RefreshLoadoutPanel();
        }

        private void AssignToTrioSlot(int slotIndex, SkilledPioneerRecord record)
        {
            if (record == null || !roster.CanJoinTrio(record))
            {
                trioStatusLabel.text = $"{record?.displayName ?? "Pioneer"} cannot join the expedition trio.";
                trioStatusLabel.color = SurvivalPioneerUiPalette.WarningText;
                return;
            }

            for (int i = 0; i < trioDraft.Length; i++)
            {
                if (i != slotIndex && trioDraft[i] == record.id)
                    trioDraft[i] = string.Empty;
            }

            trioDraft[slotIndex] = record.id;
            pendingTrioSlot = -1;
            CommitTrioDraft($"Assigned {record.displayName} to slot {slotIndex + 1}.");
        }

        private void HandleTrioSlotClicked(int slotIndex)
        {
            if (pendingTrioSlot == slotIndex)
            {
                CycleTrioSlot(slotIndex);
                return;
            }

            pendingTrioSlot = slotIndex;
            trioStatusLabel.text = $"Select a roster pioneer for slot {slotIndex + 1}, or click the slot again to cycle.";
            trioStatusLabel.color = SurvivalPioneerUiPalette.HighlightText;
            RefreshTrioPicker();
        }

        private void CycleTrioSlot(int slotIndex)
        {
            List<SkilledPioneerRecord> eligible = GetEligibleTrioPioneers();
            if (eligible.Count == 0)
            {
                trioStatusLabel.text = "No eligible pioneers for expedition trio.";
                trioStatusLabel.color = SurvivalPioneerUiPalette.WarningText;
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
            CommitTrioDraft($"Cycled slot {slotIndex + 1} to {next.displayName}.");
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

        private void RefreshDetailPanel()
        {
            SkilledPioneerRecord record = roster.FindSkilledById(selectedPioneerId);
            if (record == null)
            {
                detailLabel.text = "Select a skilled pioneer from the roster.";
                synergyHintLabel.text = BuildTrioSynergySummary();
                return;
            }

            string traits = PioneerTraitUtility.FormatTraitList(record.traitIds);
            string passives = PioneerTraitUtility.FormatTraitList(record.passiveAbilityIds);
            string skills = record.learnedSkills == null || record.learnedSkills.Length == 0
                ? "None"
                : PioneerTraitUtility.FormatTraitList(record.learnedSkills);
            string disposition = record.Kind == PioneerKind.RescuedEcho
                ? PioneerTraitUtility.GetDispositionLabel(record.Disposition)
                : "N/A";

            detailLabel.text =
                $"<color=#{ColorUtility.ToHtmlStringRGB(SurvivalPioneerUiPalette.RichFuchsia)}>{record.displayName}</color>\n" +
                $"{SkilledPioneerClassUtility.ToDisplayName(record.pioneerClass)}  ·  Lv {record.level}\n\n" +
                $"Rad {record.radiationResistance:P0}  ·  Exp {record.expeditionEfficiency:P0}  ·  Syn {record.combatSynergy:P0}\n" +
                $"Saturation {record.saturation:P0}  ·  Disposition {disposition}\n\n" +
                $"Traits: {traits}\n" +
                $"Passives: {passives}\n" +
                $"Learned skills: {skills}\n\n" +
                (string.IsNullOrEmpty(record.backstory) ? string.Empty : record.backstory);

            synergyHintLabel.text = GetClassSynergyHint(record.pioneerClass) + "\n" + BuildTrioSynergySummary();
        }

        private void RefreshLoadoutPanel()
        {
            SkilledPioneerRecord record = roster.FindSkilledById(selectedPioneerId);
            if (record == null)
            {
                if (weaponSlotLabel != null)
                    weaponSlotLabel.text = "Weapon\n—";
                if (toolSlotLabel != null)
                    toolSlotLabel.text = "Tool\n—";
                if (skillSlotLabel != null)
                    skillSlotLabel.text = "Skill\n—";
                if (loadoutStatusLabel != null)
                    loadoutStatusLabel.text = "Select a pioneer to edit loadout.";
                return;
            }

            PioneerLoadoutDefaults.EnsureDefaults(record);
            ItemData weapon = ItemRegistry.Resolve(record.weaponItemId);
            ItemData tool = ItemRegistry.Resolve(record.toolItemId);

            if (weaponSlotLabel != null)
                weaponSlotLabel.text = $"Weapon\n{(weapon != null ? weapon.itemName : record.weaponItemId)}";
            if (toolSlotLabel != null)
                toolSlotLabel.text = $"Tool\n{(tool != null ? tool.itemName : (string.IsNullOrEmpty(record.toolItemId) ? "None" : record.toolItemId))}";

            string activeSkill = record.assignedSkillIds != null && record.assignedSkillIds.Length > 0
                ? record.assignedSkillIds[0]
                : "None";
            if (skillSlotLabel != null)
                skillSlotLabel.text = $"Skill\n{activeSkill}";

            string skills = record.assignedSkillIds == null || record.assignedSkillIds.Length == 0
                ? "None"
                : PioneerTraitUtility.FormatTraitList(record.assignedSkillIds);
            if (loadoutStatusLabel != null)
                loadoutStatusLabel.text = $"Assigned skills: {skills}";
        }

        private void CycleWeaponLoadout()
        {
            SkilledPioneerRecord record = roster.FindSkilledById(selectedPioneerId);
            if (record == null)
                return;

            string nextId = CycleLoadoutItem(record.weaponItemId, ItemType.MeleeWeapon);
            roster.TrySetPioneerLoadout(record.id, nextId, record.toolItemId, record.assignedSkillIds, out _);
            RefreshLoadoutPanel();
        }

        private void CycleToolLoadout()
        {
            SkilledPioneerRecord record = roster.FindSkilledById(selectedPioneerId);
            if (record == null)
                return;

            string nextId = CycleLoadoutItem(record.toolItemId, ItemType.Tool, allowEmpty: true);
            roster.TrySetPioneerLoadout(record.id, record.weaponItemId, nextId, record.assignedSkillIds, out _);
            RefreshLoadoutPanel();
        }

        private void CycleSkillLoadout()
        {
            SkilledPioneerRecord record = roster.FindSkilledById(selectedPioneerId);
            if (record == null)
                return;

            string[] pool = record.learnedSkills != null && record.learnedSkills.Length > 0
                ? record.learnedSkills
                : System.Array.Empty<string>();

            if (pool.Length == 0)
            {
                loadoutStatusLabel.text = "No learned skills to assign.";
                return;
            }

            string current = record.assignedSkillIds != null && record.assignedSkillIds.Length > 0
                ? record.assignedSkillIds[0]
                : string.Empty;

            int index = System.Array.IndexOf(pool, current);
            index = index < 0 ? 0 : (index + 1) % pool.Length;
            string[] nextSkills = { pool[index] };
            roster.TrySetPioneerLoadout(record.id, record.weaponItemId, record.toolItemId, nextSkills, out _);
            RefreshLoadoutPanel();
        }

        private static string CycleLoadoutItem(string currentId, ItemType itemType, bool allowEmpty = false)
        {
            ItemData[] allItems = ItemRegistry.GetAllItems();
            List<string> ids = new List<string>();
            if (allowEmpty)
                ids.Add(string.Empty);

            for (int i = 0; i < allItems.Length; i++)
            {
                ItemData item = allItems[i];
                if (item == null || item.itemType != itemType)
                    continue;

                ids.Add(item.name);
            }

            if (ids.Count == 0)
                return currentId ?? string.Empty;

            int index = ids.IndexOf(currentId ?? string.Empty);
            if (index < 0)
                index = 0;
            else
                index = (index + 1) % ids.Count;

            return ids[index];
        }

        private void RefreshTrioPicker()
        {
            for (int i = 0; i < PioneerRosterManager.ExpeditionTrioSize; i++)
            {
                bool pending = pendingTrioSlot == i;
                SkilledPioneerRecord assigned = roster.FindSkilledById(trioDraft[i]);
                string slotName = assigned != null ? assigned.displayName : "Empty";
                trioSlotLabels[i].text = $"Slot {i + 1}\n{slotName}";

                Image slotImage = trioSlotButtons[i].GetComponent<Image>();
                slotImage.color = pending
                    ? SurvivalPioneerUiPalette.ButtonHighlighted
                    : assigned != null
                        ? SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.85f)
                        : SurvivalPioneerUiPalette.SlotBackground;
            }

            if (pendingTrioSlot < 0)
            {
                int active = roster.GetActiveExpeditionTrioCount();
                trioStatusLabel.text = active == 0
                    ? "Drag or right-click pioneers into trio slots (1–3 active)."
                    : $"{active} pioneer(s) active. Right-click slots to unslot or transmute.";
                trioStatusLabel.color = SurvivalPioneerUiPalette.MutedText;
            }
        }

        private string BuildTrioSynergySummary()
        {
            HashSet<SkilledPioneerClass> classes = new HashSet<SkilledPioneerClass>();
            for (int i = 0; i < PioneerRosterManager.ExpeditionTrioSize; i++)
            {
                SkilledPioneerRecord record = roster.FindSkilledById(trioDraft[i]);
                if (record != null)
                    classes.Add(record.pioneerClass);
            }

            if (classes.Count == 0)
                return "Trio synergy: slot 1–3 pioneers to unlock combo bonuses.";

            if (classes.Contains(SkilledPioneerClass.ArchitectEngineer)
                && classes.Contains(SkilledPioneerClass.CombatTactician)
                && classes.Contains(SkilledPioneerClass.InfiltratorScout))
            {
                return "Trio synergy: Rescue setpiece ready — Purification Field + hold line + vent burst timing.";
            }

            if (classes.Count >= 3)
                return "Trio synergy: Mixed class imprint — Echo sync and ability combos enabled on expeditions.";

            return "Trio synergy: Add more classes to unlock rescue setpiece combos.";
        }

        private static string GetClassSynergyHint(SkilledPioneerClass pioneerClass)
        {
            return pioneerClass switch
            {
                SkilledPioneerClass.ArchitectEngineer => "Class synergy: Portable Purification Field stabilizes hostile Echo saturation.",
                SkilledPioneerClass.ScienceSpecialist => "Class synergy: Analysis link amplifies Aether-9 scans and core archive gains.",
                SkilledPioneerClass.CombatTactician => "Class synergy: Hold line protects the trio during echo rescue setpieces.",
                SkilledPioneerClass.InfiltratorScout => "Class synergy: Vent burst timing detects Echo signals near hazards.",
                SkilledPioneerClass.IoHybrid => "Class synergy: Synergy Link bridges class combos across the expedition trio.",
                _ => "Class synergy: Mix pioneer classes for expedition combo bonuses."
            };
        }

        private void EnsureBuilt(Transform parent)
        {
            if (panelRoot != null)
                return;

            theme = ShiftUiTheme.Current;

            panelRoot = new GameObject("PioneerRosterPanel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(parent, false);
            RectTransform rootRect = panelRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = new Vector2(16f, 16f);
            rootRect.offsetMax = new Vector2(-16f, -16f);

            Image panelBg = panelRoot.GetComponent<Image>();
            if (theme != null)
                theme.ApplyPanelImage(panelBg, large: true, alphaMultiplier: 0.98f);
            else
            {
                MenuUiBuilder.ApplyUiSprite(panelBg);
                panelBg.color = SurvivalPioneerUiPalette.PanelBackground;
            }

            HorizontalLayoutGroup splitLayout = panelRoot.AddComponent<HorizontalLayoutGroup>();
            splitLayout.spacing = 12f;
            splitLayout.padding = new RectOffset(14, 14, 14, 14);
            splitLayout.childControlWidth = true;
            splitLayout.childControlHeight = true;
            splitLayout.childForceExpandWidth = true;
            splitLayout.childForceExpandHeight = true;

            GameObject leftColumn = CreateColumn(panelRoot.transform, flexibleWidth: 0.42f);
            BuildLeftColumn(leftColumn.transform);

            GameObject rightColumn = CreateColumn(panelRoot.transform, flexibleWidth: 0.58f);
            BuildRightColumn(rightColumn.transform);
        }

        private void BuildLeftColumn(Transform parent)
        {
            VerticalLayoutGroup layout = parent.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI header = CreateLabel(parent, "Skilled Roster", 18f, semiBold: true);
            header.color = SurvivalPioneerUiPalette.AccentText;

            GameObject scrollObject = new GameObject("RosterScroll", typeof(RectTransform), typeof(ScrollRect), typeof(LayoutElement), typeof(Image));
            scrollObject.transform.SetParent(parent, false);
            LayoutElement scrollLayout = scrollObject.GetComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.minHeight = 240f;

            Image scrollBg = scrollObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(scrollBg);
            scrollBg.color = SurvivalPioneerUiPalette.ScrollBackground;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollObject.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect);
            viewportRect.offsetMin = new Vector2(4f, 4f);
            viewportRect.offsetMax = new Vector2(-4f, -4f);
            viewport.GetComponent<Image>().color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.35f);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 6f;
            contentLayout.padding = new RectOffset(4, 4, 4, 4);
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            rosterListParent = content.transform;

            GameObject colonistRow = new GameObject("ColonistSummary", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            colonistRow.transform.SetParent(parent, false);
            colonistRow.GetComponent<Image>().color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.98f);
            colonistRow.GetComponent<LayoutElement>().minHeight = 64f;

            colonistSummaryLabel = CreateLabel(colonistRow.transform, string.Empty, 12f);
            colonistSummaryLabel.color = SurvivalPioneerUiPalette.BodyText;
            Stretch(colonistSummaryLabel.rectTransform, 10f, 8f);
        }

        private void BuildRightColumn(Transform parent)
        {
            VerticalLayoutGroup layout = parent.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            TextMeshProUGUI header = CreateLabel(parent, "Pioneer Detail", 18f, semiBold: true);
            header.color = SurvivalPioneerUiPalette.AccentText;

            GameObject detailHost = new GameObject("DetailHost", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            detailHost.transform.SetParent(parent, false);
            detailHost.GetComponent<Image>().color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.88f);
            LayoutElement detailLayout = detailHost.GetComponent<LayoutElement>();
            detailLayout.flexibleHeight = 1f;
            detailLayout.minHeight = 180f;

            detailLabel = CreateLabel(detailHost.transform, "Select a skilled pioneer from the roster.", 14f);
            detailLabel.color = SurvivalPioneerUiPalette.BodyText;
            Stretch(detailLabel.rectTransform, 12f, 10f);

            synergyHintLabel = CreateLabel(parent, string.Empty, 12f);
            synergyHintLabel.color = SurvivalPioneerUiPalette.MutedText;

            TextMeshProUGUI loadoutHeader = CreateLabel(parent, "Loadout", 16f, semiBold: true);
            loadoutHeader.color = SurvivalPioneerUiPalette.HighlightText;

            GameObject loadoutRow = new GameObject("LoadoutSlots", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            loadoutRow.transform.SetParent(parent, false);
            loadoutRow.GetComponent<LayoutElement>().minHeight = 64f;
            HorizontalLayoutGroup loadoutLayout = loadoutRow.GetComponent<HorizontalLayoutGroup>();
            loadoutLayout.spacing = 8f;
            loadoutLayout.childControlWidth = true;
            loadoutLayout.childForceExpandWidth = true;
            loadoutLayout.childControlHeight = true;
            loadoutLayout.childForceExpandHeight = true;

            weaponSlotButton = CreateLoadoutSlotButton(loadoutRow.transform, "WeaponSlot", "Weapon\n—", CycleWeaponLoadout, out weaponSlotLabel);
            toolSlotButton = CreateLoadoutSlotButton(loadoutRow.transform, "ToolSlot", "Tool\n—", CycleToolLoadout, out toolSlotLabel);
            skillSlotButton = CreateLoadoutSlotButton(loadoutRow.transform, "SkillSlot", "Skill\n—", CycleSkillLoadout, out skillSlotLabel);

            loadoutStatusLabel = CreateLabel(parent, "Select a pioneer to edit loadout.", 12f);
            loadoutStatusLabel.color = SurvivalPioneerUiPalette.MutedText;

            TextMeshProUGUI trioHeader = CreateLabel(parent, "Expedition Trio", 16f, semiBold: true);
            trioHeader.color = SurvivalPioneerUiPalette.HighlightText;

            GameObject trioRow = new GameObject("TrioSlots", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            trioRow.transform.SetParent(parent, false);
            trioRow.GetComponent<LayoutElement>().minHeight = 72f;
            HorizontalLayoutGroup trioLayout = trioRow.GetComponent<HorizontalLayoutGroup>();
            trioLayout.spacing = 8f;
            trioLayout.childControlWidth = true;
            trioLayout.childForceExpandWidth = true;
            trioLayout.childControlHeight = true;
            trioLayout.childForceExpandHeight = true;

            for (int i = 0; i < PioneerRosterManager.ExpeditionTrioSize; i++)
                CreateTrioSlotButton(trioRow.transform, i);

            trioStatusLabel = CreateLabel(parent, string.Empty, 12f);
            trioStatusLabel.color = SurvivalPioneerUiPalette.MutedText;

            TextMeshProUGUI trioLoadoutHeader = CreateLabel(parent, "Trio Loadouts", 16f, semiBold: true);
            trioLoadoutHeader.color = SurvivalPioneerUiPalette.HighlightText;

            for (int i = 0; i < PioneerRosterManager.ExpeditionTrioSize; i++)
                CreateTrioLoadoutMiniPanel(parent, i);
        }

        private void CreateTrioLoadoutMiniPanel(Transform parent, int slotIndex)
        {
            GameObject host = new GameObject($"TrioLoadout_{slotIndex + 1}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            host.transform.SetParent(parent, false);
            host.GetComponent<Image>().color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.94f);
            host.GetComponent<LayoutElement>().minHeight = 72f;

            VerticalLayoutGroup hostLayout = host.AddComponent<VerticalLayoutGroup>();
            hostLayout.padding = new RectOffset(8, 8, 6, 6);
            hostLayout.spacing = 4;
            hostLayout.childControlWidth = true;
            hostLayout.childForceExpandWidth = true;

            trioLoadoutLabels[slotIndex] = CreateLabel(host.transform, $"Slot {slotIndex + 1} — Empty", 12f, semiBold: true);
            trioLoadoutLabels[slotIndex].color = SurvivalPioneerUiPalette.AccentText;

            GameObject row = new GameObject("LoadoutRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(host.transform, false);
            HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 6f;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandHeight = true;

            int capturedSlot = slotIndex;
            trioLoadoutWeaponButtons[slotIndex] = CreateLoadoutSlotButton(
                row.transform,
                "Weapon",
                "Wpn\n—",
                () => CycleTrioSlotWeapon(capturedSlot),
                out _);
            trioLoadoutToolButtons[slotIndex] = CreateLoadoutSlotButton(
                row.transform,
                "Tool",
                "Tool\n—",
                () => CycleTrioSlotTool(capturedSlot),
                out _);
            trioLoadoutSkillButtons[slotIndex] = CreateLoadoutSlotButton(
                row.transform,
                "Skill",
                "Skl\n—",
                () => CycleTrioSlotSkill(capturedSlot),
                out _);
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

                trioLoadoutLabels[i].text = $"Slot {i + 1} — {record.displayName}";
                SetTrioLoadoutButtonLabel(trioLoadoutWeaponButtons[i], "Wpn", weapon != null ? weapon.itemName : record.weaponItemId);
                SetTrioLoadoutButtonLabel(trioLoadoutToolButtons[i], "Tool", tool != null ? tool.itemName : (string.IsNullOrEmpty(record.toolItemId) ? "None" : record.toolItemId));
                SetTrioLoadoutButtonLabel(trioLoadoutSkillButtons[i], "Skl", skill);
            }
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

            string nextId = CycleLoadoutItem(record.weaponItemId, ItemType.MeleeWeapon);
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

        private void CreateTrioSlotButton(Transform parent, int slotIndex)
        {
            GameObject slotObject = new GameObject($"TrioSlot_{slotIndex + 1}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            slotObject.transform.SetParent(parent, false);

            Image slotImage = slotObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(slotImage);
            slotImage.color = SurvivalPioneerUiPalette.SlotBackground;
            SurvivalPioneerUiPalette.StylePrimaryButton(slotObject.GetComponent<Button>(), slotImage);

            LayoutElement slotLayout = slotObject.GetComponent<LayoutElement>();
            slotLayout.flexibleWidth = 1f;
            slotLayout.minHeight = 64f;

            trioSlotLabels[slotIndex] = CreateLabel(slotObject.transform, $"Slot {slotIndex + 1}\nEmpty", 12f, semiBold: true);
            trioSlotLabels[slotIndex].alignment = TextAlignmentOptions.Center;
            trioSlotLabels[slotIndex].color = SurvivalPioneerUiPalette.WarmOffWhite;
            Stretch(trioSlotLabels[slotIndex].rectTransform, 6f, 6f);

            int capturedSlot = slotIndex;
            Button button = slotObject.GetComponent<Button>();
            trioSlotButtons[slotIndex] = button;
            button.onClick.AddListener(() => HandleTrioSlotClicked(capturedSlot));

            PioneerTrioSlotDropHandler drop = slotObject.AddComponent<PioneerTrioSlotDropHandler>();
            drop.Configure(this, slotIndex);
        }

        private Button CreateLoadoutSlotButton(
            Transform parent,
            string objectName,
            string defaultText,
            UnityEngine.Events.UnityAction onClick,
            out TextMeshProUGUI label)
        {
            GameObject slotObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            slotObject.transform.SetParent(parent, false);

            Image slotImage = slotObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(slotImage);
            slotImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.SlateGray, 0.92f);

            Button button = slotObject.GetComponent<Button>();
            SurvivalPioneerUiPalette.StylePrimaryButton(button, slotImage);
            button.onClick.AddListener(onClick);

            slotObject.GetComponent<LayoutElement>().flexibleWidth = 1f;

            label = CreateLabel(slotObject.transform, defaultText, 12f, semiBold: true);
            label.alignment = TextAlignmentOptions.Center;
            label.color = SurvivalPioneerUiPalette.WarmOffWhite;
            Stretch(label.rectTransform, 6f, 6f);
            return button;
        }

        private void CreateRosterInfoRow(string message)
        {
            GameObject row = new GameObject("Info", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            row.transform.SetParent(rosterListParent, false);
            row.GetComponent<LayoutElement>().minHeight = 48f;
            TextMeshProUGUI label = row.GetComponent<TextMeshProUGUI>();
            ApplyThemeFont(label);
            label.fontSize = 13f;
            label.color = SurvivalPioneerUiPalette.MutedText;
            label.text = message;
        }

        private static GameObject CreateColumn(Transform parent, float flexibleWidth)
        {
            GameObject column = new GameObject("Column", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            column.transform.SetParent(parent, false);
            LayoutElement layout = column.GetComponent<LayoutElement>();
            layout.flexibleWidth = flexibleWidth;
            layout.flexibleHeight = 1f;
            return column;
        }

        private TextMeshProUGUI CreateLabel(Transform parent, string text, float size, bool semiBold = false)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            labelObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            ApplyThemeFont(label, semiBold: semiBold);
            label.fontSize = size;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.text = text;
            return label;
        }

        private void ApplyThemeFont(TextMeshProUGUI label, bool semiBold = false)
        {
            if (theme != null)
                theme.ApplyFont(label, semiBold: semiBold);
            else
                TmpUiHelper.ApplyDefaultFont(label);
        }

        private static void Stretch(RectTransform rect, float padX = 0f, float padY = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padX, padY);
            rect.offsetMax = new Vector2(-padX, -padY);
        }
    }
}

using System.Collections.Generic;
using Project.Audio;
using Project.Building;
using Project.Companions;
using Project.Data;
using Project.Pioneers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Toolkit companions remaining: portraits, drag-to-trio, loadout cycling, camp columns.
    /// Stamp: DMUiToolkit 0901-finish
    /// </summary>
    public partial class DMUiToolkitMenus
    {
        private VisualElement companionsPortrait;
        private Label companionsPortraitInitials;
        private Button loadoutWeapon;
        private Button loadoutTool;
        private Button loadoutSkill;
        private Label loadoutStatus;
        private ScrollView campList;
        private string companionDragId;
        private int companionDragSourceTrio = -1;
        private bool companionDragActive;
        private Vector2 companionPointerDown;
        private Vector2 companionLastPos;
        private int companionPointerId = -1;
        private VisualElement companionDragGhost;
        private VisualElement companionPointerHost;
        private bool companionIgnoreClick;

        private void BindCompanionsExtras(VisualElement tree)
        {
            if (tree == null)
                return;

            companionsPortrait = tree.Q<VisualElement>("companions-portrait");
            companionsPortraitInitials = tree.Q<Label>("companions-portrait-initials");
            loadoutWeapon = tree.Q<Button>("loadout-weapon");
            loadoutTool = tree.Q<Button>("loadout-tool");
            loadoutSkill = tree.Q<Button>("loadout-skill");
            loadoutStatus = tree.Q<Label>("loadout-status");
            campList = tree.Q<ScrollView>("camp-list");

            if (loadoutWeapon != null)
            {
                loadoutWeapon.clicked -= CycleSelectedWeaponLoadout;
                loadoutWeapon.clicked += CycleSelectedWeaponLoadout;
            }

            if (loadoutTool != null)
            {
                loadoutTool.clicked -= CycleSelectedToolLoadout;
                loadoutTool.clicked += CycleSelectedToolLoadout;
            }

            if (loadoutSkill != null)
            {
                loadoutSkill.clicked -= CycleSelectedSkillLoadout;
                loadoutSkill.clicked += CycleSelectedSkillLoadout;
            }

            for (int i = 0; i < trioButtons.Length; i++)
            {
                Button slot = trioButtons[i];
                if (slot == null)
                    continue;
                slot.UnregisterCallback<PointerDownEvent>(OnTrioPointerDown);
                slot.UnregisterCallback<PointerMoveEvent>(OnCompanionPointerMove);
                slot.UnregisterCallback<PointerUpEvent>(OnCompanionPointerUp);
                slot.UnregisterCallback<PointerEnterEvent>(OnTrioPointerEnter);
                slot.UnregisterCallback<PointerLeaveEvent>(OnPioneerHoverLeave);
                slot.RegisterCallback<PointerDownEvent>(OnTrioPointerDown);
                slot.RegisterCallback<PointerMoveEvent>(OnCompanionPointerMove);
                slot.RegisterCallback<PointerUpEvent>(OnCompanionPointerUp);
                slot.RegisterCallback<PointerEnterEvent>(OnTrioPointerEnter);
                slot.RegisterCallback<PointerLeaveEvent>(OnPioneerHoverLeave);
            }
        }

        private void RefreshCompanionsExtras()
        {
            ApplyCompanionPortrait();
            ApplyLoadoutEditors();
            RefreshCampColumn();
        }

        private VisualElement MakeCompanionRowWithPortrait(SkilledPioneerRecord record)
        {
            bool selected = record.id == selectedPioneerId;
            Button row = new Button();
            row.AddToClassList("dmg-companion-row");
            row.EnableInClassList("dmg-list-row--selected", selected);
            row.userData = record.id;
            row.pickingMode = PickingMode.Position;

            VisualElement portrait = new VisualElement();
            portrait.AddToClassList("dmg-companion-row-portrait");
            portrait.pickingMode = PickingMode.Ignore;
            ApplyPioneerSprite(portrait, record);

            Label initials = new Label();
            initials.AddToClassList("dmg-companion-row-initials");
            initials.pickingMode = PickingMode.Ignore;
            bool hasSprite = PioneerPortraitResolver.Resolve(record) != null;
            initials.text = hasSprite
                ? string.Empty
                : PioneerPortraitUi.BuildInitials(PioneerUiLabels.GetDisplayName(record));
            portrait.Add(initials);
            row.Add(portrait);

            string displayName = PioneerUiLabels.GetDisplayName(record);
            Label name = new Label(displayName);
            name.AddToClassList("dmg-companion-row-name");
            name.pickingMode = PickingMode.Ignore;
            row.Add(name);

            string captured = record.id;
            row.clicked += () =>
            {
                selectedPioneerId = captured;
                RefreshCompanions();
            };
            row.RegisterCallback<PointerDownEvent>(OnRosterPointerDown);
            row.RegisterCallback<PointerMoveEvent>(OnCompanionPointerMove);
            row.RegisterCallback<PointerUpEvent>(OnCompanionPointerUp);
            AttachPioneerHover(row, captured);
            return row;
        }

        private void AttachPioneerHover(VisualElement target, string pioneerId)
        {
            if (target == null)
                return;
            target.RegisterCallback<PointerEnterEvent>(evt => ShowPioneerHoverForId(pioneerId));
            target.RegisterCallback<PointerLeaveEvent>(OnPioneerHoverLeave);
        }

        private void ShowPioneerHoverForId(string pioneerId)
        {
            if (string.IsNullOrEmpty(pioneerId))
                return;
            boundRoster ??= PioneerRosterManager.EnsureExists();
            SkilledPioneerRecord hover = boundRoster != null ? boundRoster.FindSkilledById(pioneerId) : null;
            if (hover == null)
                return;
            PioneerHoverTooltip.HideAny();
            DMUiToolkitWorldMenus.TryShowPioneerHover(hover, CurrentPointerScreenPosition());
        }

        private void OnTrioPointerEnter(PointerEnterEvent evt)
        {
            if (evt.currentTarget is not VisualElement slot || slot.userData is not int index)
                return;
            boundRoster ??= PioneerRosterManager.EnsureExists();
            SkilledPioneerRecord record = boundRoster != null
                ? boundRoster.GetExpeditionTrioRecordAtSlot(index)
                : null;
            if (record == null)
                return;
            PioneerHoverTooltip.HideAny();
            DMUiToolkitWorldMenus.TryShowPioneerHover(record, CurrentPointerScreenPosition());
        }

        private void OnPioneerHoverLeave(PointerLeaveEvent evt)
        {
            PioneerHoverTooltip.HideAny();
            DMUiToolkitWorldMenus.HidePioneerHover();
        }

        private void ApplyCompanionPortrait()
        {
            if (companionsPortrait == null || boundRoster == null)
                return;

            SkilledPioneerRecord record = boundRoster.FindSkilledById(selectedPioneerId);
            ApplyPioneerSprite(companionsPortrait, record);
            if (companionsPortraitInitials != null)
            {
                bool hasSprite = record != null && PioneerPortraitResolver.Resolve(record) != null;
                companionsPortraitInitials.text = hasSprite || record == null
                    ? string.Empty
                    : PioneerPortraitUi.BuildInitials(PioneerUiLabels.GetDisplayName(record));
            }
        }

        private static void ApplyPioneerSprite(VisualElement target, SkilledPioneerRecord record)
        {
            if (target == null)
                return;

            Sprite sprite = PioneerPortraitResolver.Resolve(record);
            if (DMUiToolkitStyle.TrySetSpriteBackground(target, sprite, ScaleMode.ScaleToFit))
                target.style.backgroundColor = Color.clear;
            else
            {
                DMUiToolkitStyle.ClearBackgroundImage(target);
                target.style.backgroundColor = DarkMatterGenesisUiPalette.SlateGray;
            }
        }

        private void ApplyLoadoutEditors()
        {
            boundRoster ??= PioneerRosterManager.EnsureExists();
            SkilledPioneerRecord record = boundRoster != null ? boundRoster.FindSkilledById(selectedPioneerId) : null;
            if (record == null)
            {
                if (loadoutWeapon != null) loadoutWeapon.text = "Weapon\n—";
                if (loadoutTool != null) loadoutTool.text = "Tool\n—";
                if (loadoutSkill != null) loadoutSkill.text = "Skill\n—";
                if (loadoutStatus != null) loadoutStatus.text = "Select a companion to edit loadout.";
                return;
            }

            PioneerLoadoutDefaults.EnsureDefaults(record);
            ItemData weapon = ItemRegistry.Resolve(record.weaponItemId);
            ItemData tool = ItemRegistry.Resolve(record.toolItemId);
            if (loadoutWeapon != null)
                loadoutWeapon.text = "Weapon\n" + (weapon != null ? weapon.itemName : record.weaponItemId);
            if (loadoutTool != null)
                loadoutTool.text = "Tool\n" + (tool != null ? tool.itemName : (string.IsNullOrEmpty(record.toolItemId) ? "None" : record.toolItemId));
            string activeSkill = record.assignedSkillIds != null && record.assignedSkillIds.Length > 0
                ? record.assignedSkillIds[0]
                : "None";
            if (loadoutSkill != null)
                loadoutSkill.text = "Skill\n" + activeSkill;
            string skills = record.assignedSkillIds == null || record.assignedSkillIds.Length == 0
                ? "None"
                : PioneerTraitUtility.FormatTraitList(record.assignedSkillIds);
            if (loadoutStatus != null)
                loadoutStatus.text = "Assigned skills: " + skills;
        }

        private void CycleSelectedWeaponLoadout()
        {
            boundRoster ??= PioneerRosterManager.EnsureExists();
            SkilledPioneerRecord record = boundRoster != null ? boundRoster.FindSkilledById(selectedPioneerId) : null;
            if (record == null)
                return;
            string nextId = CycleWeaponLoadoutItem(record.weaponItemId);
            boundRoster.TrySetPioneerLoadout(record.id, nextId, record.toolItemId, record.assignedSkillIds, out _);
            RefreshCompanions();
        }

        private void CycleSelectedToolLoadout()
        {
            boundRoster ??= PioneerRosterManager.EnsureExists();
            SkilledPioneerRecord record = boundRoster != null ? boundRoster.FindSkilledById(selectedPioneerId) : null;
            if (record == null)
                return;
            string nextId = CycleLoadoutItem(record.toolItemId, ItemType.Tool, true);
            boundRoster.TrySetPioneerLoadout(record.id, record.weaponItemId, nextId, record.assignedSkillIds, out _);
            RefreshCompanions();
        }

        private void CycleSelectedSkillLoadout()
        {
            boundRoster ??= PioneerRosterManager.EnsureExists();
            SkilledPioneerRecord record = boundRoster != null ? boundRoster.FindSkilledById(selectedPioneerId) : null;
            if (record == null)
                return;

            string[] pool = record.learnedSkills != null && record.learnedSkills.Length > 0
                ? record.learnedSkills
                : System.Array.Empty<string>();
            if (pool.Length == 0)
            {
                if (loadoutStatus != null)
                    loadoutStatus.text = "No learned skills to assign.";
                return;
            }

            string current = record.assignedSkillIds != null && record.assignedSkillIds.Length > 0
                ? record.assignedSkillIds[0]
                : string.Empty;
            int index = System.Array.IndexOf(pool, current);
            index = index < 0 ? 0 : (index + 1) % pool.Length;
            boundRoster.TrySetPioneerLoadout(record.id, record.weaponItemId, record.toolItemId, new[] { pool[index] }, out _);
            RefreshCompanions();
        }

        private static string CycleLoadoutItem(string currentId, ItemType itemType, bool allowEmpty)
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
            index = index < 0 ? 0 : (index + 1) % ids.Count;
            return ids[index];
        }

        private static string CycleWeaponLoadoutItem(string currentId)
        {
            ItemData[] allItems = ItemRegistry.GetAllItems();
            List<string> ids = new List<string>();
            for (int i = 0; i < allItems.Length; i++)
            {
                ItemData item = allItems[i];
                if (item == null || !item.IsWeapon)
                    continue;
                ids.Add(item.name);
            }

            if (ids.Count == 0)
                return currentId ?? string.Empty;
            int index = ids.IndexOf(currentId ?? string.Empty);
            index = index < 0 ? 0 : (index + 1) % ids.Count;
            return ids[index];
        }

        private void RefreshCampColumn()
        {
            if (campList == null)
                return;

            campList.Clear();
            boundRoster ??= PioneerRosterManager.EnsureExists();
            if (boundRoster == null)
            {
                campList.Add(MakeEmpty("No roster manager."));
                return;
            }

            Dictionary<string, string> buildingNames = BuildPlacedBuildingNameLookup();
            Dictionary<string, List<SkilledPioneerRecord>> byBuilding = new Dictionary<string, List<SkilledPioneerRecord>>();
            List<SkilledPioneerRecord> unassigned = new List<SkilledPioneerRecord>();
            IReadOnlyList<SkilledPioneerRecord> skilled = boundRoster.SkilledPioneers;
            for (int i = 0; i < skilled.Count; i++)
            {
                SkilledPioneerRecord record = skilled[i];
                if (record == null || record.isInExpeditionTrio)
                    continue;
                string buildingId = BuildingOperationRegistry.FindAssignedBuildingId(record.id);
                if (string.IsNullOrEmpty(buildingId))
                {
                    unassigned.Add(record);
                    continue;
                }

                if (!byBuilding.TryGetValue(buildingId, out List<SkilledPioneerRecord> bucket))
                {
                    bucket = new List<SkilledPioneerRecord>();
                    byBuilding[buildingId] = bucket;
                }

                bucket.Add(record);
            }

            if (byBuilding.Count == 0 && unassigned.Count == 0)
            {
                campList.Add(MakeEmpty("No one is benched at camp right now."));
                return;
            }

            foreach (KeyValuePair<string, List<SkilledPioneerRecord>> pair in byBuilding)
            {
                string label = buildingNames.TryGetValue(pair.Key, out string niceName) ? niceName : pair.Key;
                campList.Add(MakeCampHeader(label));
                for (int i = 0; i < pair.Value.Count; i++)
                    campList.Add(MakeCompanionRowWithPortrait(pair.Value[i]));
            }

            if (unassigned.Count > 0)
            {
                campList.Add(MakeCampHeader("Unassigned"));
                for (int i = 0; i < unassigned.Count; i++)
                    campList.Add(MakeCompanionRowWithPortrait(unassigned[i]));
            }
        }

        private static VisualElement MakeCampHeader(string text)
        {
            Label header = new Label(text);
            header.AddToClassList("dmg-gold-heading");
            header.pickingMode = PickingMode.Ignore;
            return header;
        }

        private static Dictionary<string, string> BuildPlacedBuildingNameLookup()
        {
            Dictionary<string, string> names = new Dictionary<string, string>();
            BuildingControlPanel[] panels = Object.FindObjectsByType<BuildingControlPanel>(FindObjectsInactive.Exclude);
            for (int i = 0; i < panels.Length; i++)
            {
                BuildingControlPanel panel = panels[i];
                if (panel == null || string.IsNullOrEmpty(panel.BuildingId))
                    continue;
                names[panel.BuildingId] = string.IsNullOrEmpty(panel.BuildingDisplayName)
                    ? panel.BuildingId
                    : panel.BuildingDisplayName;
            }

            return names;
        }

        private void OnRosterPointerDown(PointerDownEvent evt)
        {
            if (evt.currentTarget is not VisualElement host || host.userData is not string id)
                return;
            if (evt.button != 0)
                return;

            companionDragId = id;
            companionDragSourceTrio = -1;
            companionDragActive = false;
            companionPointerDown = (Vector2)evt.position;
            companionLastPos = companionPointerDown;
            companionPointerId = evt.pointerId;
            companionPointerHost = host;
            host.CapturePointer(evt.pointerId);
        }

        private void OnTrioPointerDown(PointerDownEvent evt)
        {
            if (evt.currentTarget is not Button button || button.userData is not int slot)
                return;
            if (evt.button != 0)
                return;

            boundRoster ??= PioneerRosterManager.EnsureExists();
            SkilledPioneerRecord existing = boundRoster != null ? boundRoster.GetExpeditionTrioRecordAtSlot(slot) : null;
            companionDragId = existing != null ? existing.id : string.Empty;
            companionDragSourceTrio = slot;
            companionDragActive = false;
            companionPointerDown = (Vector2)evt.position;
            companionLastPos = companionPointerDown;
            companionPointerId = evt.pointerId;
            companionPointerHost = button;
            button.CapturePointer(evt.pointerId);
        }

        private void OnCompanionPointerMove(PointerMoveEvent evt)
        {
            if (string.IsNullOrEmpty(companionDragId) && companionDragSourceTrio < 0)
                return;

            companionLastPos = (Vector2)evt.position;
            if (companionDragActive)
            {
                PositionCompanionGhost(companionLastPos);
                return;
            }

            if ((evt.pressedButtons & 1) == 0)
                return;
            Vector2 delta = companionLastPos - companionPointerDown;
            if (delta.sqrMagnitude < InvDragThresholdPx * InvDragThresholdPx)
                return;
            if (string.IsNullOrEmpty(companionDragId))
                return;

            companionDragActive = true;
            ClearCompanionGhost();
            companionDragGhost = new VisualElement { pickingMode = PickingMode.Ignore };
            companionDragGhost.style.position = Position.Absolute;
            companionDragGhost.style.width = 48f;
            companionDragGhost.style.height = 48f;
            companionDragGhost.style.opacity = 0.75f;
            SkilledPioneerRecord record = boundRoster != null ? boundRoster.FindSkilledById(companionDragId) : null;
            ApplyPioneerSprite(companionDragGhost, record);
            (root ?? companionsBody)?.Add(companionDragGhost);
            PositionCompanionGhost(companionLastPos);
        }

        private void OnCompanionPointerUp(PointerUpEvent evt)
        {
            bool dragging = companionDragActive;
            string id = companionDragId;
            int sourceTrio = companionDragSourceTrio;
            Vector2 panelPos = (Vector2)evt.position;
            ReleaseCompanionPointer();
            if (!dragging)
                return;

            companionIgnoreClick = true;

            int dest = FindTrioSlotAtPanel(panelPos);
            boundRoster ??= PioneerRosterManager.EnsureExists();
            if (boundRoster == null || string.IsNullOrEmpty(id))
                return;

            if (dest >= 0)
            {
                if (!boundRoster.TryAssignTrioSlot(dest, id, out string error) && !string.IsNullOrEmpty(error))
                    PickupToastUI.Show(error);
            }
            else if (sourceTrio >= 0)
            {
                boundRoster.TryAssignTrioSlot(sourceTrio, string.Empty, out _);
            }

            RefreshCompanions();
        }

        private void ReleaseCompanionPointer()
        {
            if (companionPointerHost != null && companionPointerId >= 0 && companionPointerHost.HasPointerCapture(companionPointerId))
                companionPointerHost.ReleasePointer(companionPointerId);

            companionPointerHost = null;
            companionPointerId = -1;
            companionDragId = null;
            companionDragSourceTrio = -1;
            companionDragActive = false;
            ClearCompanionGhost();
        }

        private void PositionCompanionGhost(Vector2 panelPos)
        {
            if (companionDragGhost == null)
                return;
            VisualElement parent = companionDragGhost.parent != null ? companionDragGhost.parent : root;
            Vector2 local = parent != null ? parent.WorldToLocal(panelPos) : panelPos;
            companionDragGhost.style.left = local.x - 24f;
            companionDragGhost.style.top = local.y - 24f;
        }

        private void ClearCompanionGhost()
        {
            if (companionDragGhost == null)
                return;
            companionDragGhost.RemoveFromHierarchy();
            companionDragGhost = null;
        }

        private int FindTrioSlotAtPanel(Vector2 panelPos)
        {
            for (int i = 0; i < trioButtons.Length; i++)
            {
                Button button = trioButtons[i];
                if (button == null)
                    continue;
                if (button.worldBound.Contains(panelPos))
                    return i;
            }

            return -1;
        }
    }
}

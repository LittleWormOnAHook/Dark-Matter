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
    // Pioneer Detail panel (stats/traits/backstory readout) and the selected pioneer's personal
    // Weapon/Tool/Skill loadout slots. Split out of PioneerRosterPanelUI.cs.
    public partial class PioneerRosterPanelUI
    {
        private void RefreshDetailPanel()
        {
            SkilledPioneerRecord record = roster.FindSkilledById(selectedPioneerId);
            if (record == null)
            {
                detailLabel.text = "Select a skilled companion from the roster.";
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
                    loadoutStatusLabel.text = "Select a companion to edit loadout.";
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

            string nextId = CycleWeaponLoadoutItem(record.weaponItemId);
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
            if (index < 0)
                index = 0;
            else
                index = (index + 1) % ids.Count;

            return ids[index];
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
                return "Trio synergy: slot 1–3 colonists to unlock combo bonuses.";

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
                SkilledPioneerClass.MedTech => "Class synergy: Field triage stabilizes injured companions after sulfur exposure.",
                SkilledPioneerClass.LogisticsOfficer => "Base role: Quartermaster routes boost storage, logistics, and vendor throughput.",
                SkilledPioneerClass.SalvageEngineer => "Base role: Upkeep patch speeds salvage, repairs, and building maintenance.",
                SkilledPioneerClass.IoHybrid => "Class synergy: Synergy Link bridges class combos across the expedition trio.",
                _ => "Class synergy: Mix companion classes for expedition combo bonuses."
            };
        }
    }
}

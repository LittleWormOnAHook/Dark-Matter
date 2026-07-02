using System.Collections.Generic;
using Project.Data;
using UnityEngine;

namespace Project.Player
{
    [CreateAssetMenu(
        fileName = "GkcActionCatalog",
        menuName = "Project/Animation/GKC Action Catalog")]
    public class GkcActionCatalog : ScriptableObject
    {
        [SerializeField] private List<GkcActionCatalogEntry> entries = new();

        public IReadOnlyList<GkcActionCatalogEntry> Entries
        {
            get
            {
                EnsureVerifiedEntries();
                return entries;
            }
        }

        public bool TryGet(GkcCombatAction action, GkcWeaponKind weaponKind, out GkcActionCatalogEntry entry)
        {
            entry = null;
            EnsureVerifiedEntries();
            if (entries == null || entries.Count == 0)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                GkcActionCatalogEntry candidate = entries[i];
                if (candidate == null || candidate.combatAction != action)
                    continue;

                if (!candidate.MatchesWeapon(weaponKind))
                    continue;

                entry = candidate;
                return true;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                GkcActionCatalogEntry candidate = entries[i];
                if (candidate != null && candidate.combatAction == action)
                {
                    entry = candidate;
                    return true;
                }
            }

            return false;
        }

        public void SetEntries(List<GkcActionCatalogEntry> newEntries)
        {
            entries = newEntries ?? new List<GkcActionCatalogEntry>();
        }

        private void EnsureVerifiedEntries()
        {
            if (NeedsCatalogReseed())
                entries = GkcActionCatalogClassifier.BuildManualSeedEntries();
        }

        private bool NeedsCatalogReseed()
        {
            if (entries == null || entries.Count == 0)
                return true;

            if (!TryFindEntry(GkcCombatAction.Sword1HCombo1, GkcWeaponKind.OneHandSword, out GkcActionCatalogEntry sword1)
                || sword1.actionId != 4100
                || !sword1.clearActionIdAfterTrigger
                || !sword1.requiresActionActive
                || sword1.stateName != "Sword Attack 1 Hand Full Body 1")
            {
                return true;
            }

            if (!TryFindEntry(GkcCombatAction.Axe1HCombo1, GkcWeaponKind.OneHandAxe, out GkcActionCatalogEntry axe1)
                || axe1.actionId != 700
                || !axe1.requiresActionActive
                || axe1.stateName != "Axe Attack 1 Hand 1")
            {
                return true;
            }

            if (!TryFindEntry(GkcCombatAction.Charge1H, GkcWeaponKind.OneHandSword, out GkcActionCatalogEntry charge1H)
                || !charge1H.useDirectCrossFade
                || charge1H.actionId != 0
                || charge1H.layerName != GkcAnimatorConstants.UpperBodyLayer)
            {
                return true;
            }

            if (!TryFindEntry(GkcCombatAction.HitReactionUnarmed, GkcWeaponKind.Unarmed, out GkcActionCatalogEntry hitUnarmed)
                || !hitUnarmed.useDirectCrossFade
                || hitUnarmed.actionId != 0
                || hitUnarmed.weaponFilter != GkcWeaponKind.Unarmed
                || hitUnarmed.layerName != "Base Layer"
                || hitUnarmed.requiresActionActive
                || string.IsNullOrWhiteSpace(hitUnarmed.stateName)
                || !hitUnarmed.stateName.StartsWith("Hit Reaction."))
            {
                return true;
            }

            if (!TryFindEntry(GkcCombatAction.HitReactionArmed, GkcWeaponKind.OneHandSword, out GkcActionCatalogEntry hitArmed)
                || !hitArmed.useDirectCrossFade
                || hitArmed.actionId != 0
                || hitArmed.layerName != "Base Layer"
                || hitArmed.requiresActionActive
                || string.IsNullOrWhiteSpace(hitArmed.stateName)
                || !hitArmed.stateName.StartsWith("Hit Reaction."))
            {
                return true;
            }

            if (!TryFindEntry(GkcCombatAction.Block, GkcWeaponKind.OneHandSword, out GkcActionCatalogEntry blockSword)
                || blockSword.actionId != 1000
                || !blockSword.requiresActionActive
                || blockSword.requiresStrafeMode
                || blockSword.useDirectCrossFade
                || blockSword.clearActionIdAfterTrigger)
            {
                return true;
            }

            if (!TryFindEntry(GkcCombatAction.Block, GkcWeaponKind.TwoHand, out GkcActionCatalogEntry blockTwoHand)
                || blockTwoHand.actionId != 2100
                || blockTwoHand.stateName != "Block With Axe"
                || blockTwoHand.requiresStrafeMode
                || blockTwoHand.useDirectCrossFade)
            {
                return true;
            }

            if (!TryFindEntry(GkcCombatAction.Block, GkcWeaponKind.OneHandAxe, out GkcActionCatalogEntry blockAxe)
                || blockAxe.actionId != 2100
                || blockAxe.stateName != "Block With Axe"
                || blockAxe.requiresStrafeMode
                || blockAxe.useDirectCrossFade)
            {
                return true;
            }

            if (!TryFindEntry(GkcCombatAction.Sword2HCombo4, GkcWeaponKind.TwoHand, out GkcActionCatalogEntry twoHandCombo4)
                || twoHandCombo4.actionId != 4203)
            {
                return true;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                GkcActionCatalogEntry row = entries[i];
                if (row != null && (row.actionId == 88888 || row.actionId == 889988))
                    return true;
            }

            return false;
        }

        private bool TryFindEntry(
            GkcCombatAction action,
            GkcWeaponKind weaponKind,
            out GkcActionCatalogEntry entry)
        {
            entry = null;
            if (entries == null)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                GkcActionCatalogEntry candidate = entries[i];
                if (candidate == null || candidate.combatAction != action)
                    continue;

                if (!candidate.MatchesWeapon(weaponKind))
                    continue;

                entry = candidate;
                return true;
            }

            return false;
        }
    }
}

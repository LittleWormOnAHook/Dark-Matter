// 2026-10-28 (a): DM Ranged morphing weapon. One mesh (DM Ranged 1.fbx), one legacy clip, one ranged slot,
// five forms that each behave as a different weapon (name, HUD label, stats, sound).
using System;
using System.Collections.Generic;
using System.Reflection;
using Project.Data;
using Project.Inventory;
using UnityEngine;

namespace Project.Combat.DMRanged
{
    /// <summary>Static hook consulted by combat/HUD/input code.</summary>
    public static class DMRangedMorphRuntime
    {
        const string PrefKey = "DMRanged.LastForm";
        static int s_last = -1;
        public static DMRangedMorphDriver Active { get; private set; }
        public static DM_RangedMorphSettings Settings => DM_RangedMorphSettings.Instance;

        public static int LastFormIndex
        {
            get
            {
                if (s_last < 0) s_last = PlayerPrefs.GetInt(PrefKey, Settings != null ? Settings.HolsterIndex : 0);
                int n = Settings != null ? Settings.FormCount : 1;
                return Mathf.Clamp(s_last, 0, Mathf.Max(0, n - 1));
            }
            set { s_last = value; PlayerPrefs.SetInt(PrefKey, value); }
        }

        // 2026-10-28g: first draw after pickup / new game starts in Pistol 1; afterwards the form is kept across holster/draw.
        const string FirstDrawKey = "DMRanged.FirstDrawPending";
        public static bool FirstDrawPending
        {
            get => PlayerPrefs.GetInt(FirstDrawKey, 1) != 0;
            set => PlayerPrefs.SetInt(FirstDrawKey, value ? 1 : 0);
        }

        /// <summary>Called on pickup (first copy added to the player) and on a new game.</summary>
        public static void MarkFirstDraw()
        {
            var s = Settings;
            FirstDrawPending = true;
            if (s != null) LastFormIndex = s.HolsterIndex;
            RaiseFormChanged();
        }

        /// <summary>Form shown while holstered (always Pistol 1 / frame 40).</summary>
        public static int RestIndex
        {
            get
            {
                var s = Settings;
                if (s == null) return 0;
                return s.HolsterIndex; // holster always morphs back to Pistol 1 (frame 40); the holstered form is remembered in LastFormIndex
            }
        }

        public static bool IsDMRanged(ItemData item)
        {
            if (item == null) return false;
            var s = Settings;
            if (s != null && s.item == item) return true;
            return string.Equals(item.name, "dm_ranged", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsPlayerDisabledWeapon(ItemData item)
        {
            var s = Settings;
            if (item == null || s == null || IsDMRanged(item)) return false;
            if (s.disableSciFiPistolForPlayer && (s.disabledPlayerItem != null ? s.disabledPlayerItem == item : string.Equals(item.name, "sci_fi_pistol", StringComparison.OrdinalIgnoreCase))) return true;
            // 2026-10-28g: Sci-fi / Survival rifle retired for the player (DM Ranged reuses its holders).
            if (s.disableSurvivalRifleForPlayer && (s.disabledPlayerRifle != null ? s.disabledPlayerRifle == item : string.Equals(item.name, "survival_rifle", StringComparison.OrdinalIgnoreCase))) return true;
            // 2026-10-28n: MorphBlade is the only player melee weapon.
            if (s.morphBladeOnlyPlayerMelee && item.itemType == ItemType.MeleeWeapon
                && !(s.keptPlayerMelee != null ? s.keptPlayerMelee == item : string.Equals(item.name, "MorphBlade", StringComparison.OrdinalIgnoreCase))) return true;
            return false;
        }

        // 2026-10-28e: also accept a renamed instance that already carries the driver.
        public static bool IsDMRangedVisual(Transform t) =>
            t != null && t.GetComponent<Animation>() != null
            && (t.name.StartsWith("DM Ranged", StringComparison.Ordinal) || t.GetComponent<DMRangedMorphDriver>() != null);

        static bool UnderHolster(Transform t)
        {
            for (Transform p = t != null ? t.parent : null; p != null; p = p.parent)
                if (p.name.StartsWith("Holstered_", StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>2026-10-28e: Active can go stale after hierarchy edits; find (or attach) the drawn driver under the player.</summary>
        static DMRangedMorphDriver ResolveDrawnDriver(EquipmentController equipment)
        {
            if (Active != null && Active.isActiveAndEnabled && !Active.IsHolsterInstance) return Active;
            if (equipment == null) return null;
            Transform root = equipment.transform.root;
            foreach (var d in root.GetComponentsInChildren<DMRangedMorphDriver>(false))
                if (d != null && d.isActiveAndEnabled && !d.IsHolsterInstance) { Active = d; return d; }
            foreach (var a in root.GetComponentsInChildren<Animation>(false))
            {
                Transform t = a != null ? a.transform : null;
                if (!IsDMRangedVisual(t) || UnderHolster(t) || !t.gameObject.activeInHierarchy) continue;
                var d = t.GetComponent<DMRangedMorphDriver>();
                if (d == null) d = t.gameObject.AddComponent<DMRangedMorphDriver>(); // OnEnable registers it
                if (d.isActiveAndEnabled) { Active = d; return d; }
            }
            return null;
        }

        public static bool ContainsDMRangedVisual(GameObject root)
        {
            if (root == null) return false;
            var anims = root.GetComponentsInChildren<Animation>(true);
            for (int i = 0; i < anims.Length; i++) if (IsDMRangedVisual(anims[i].transform)) return true;
            return false;
        }

        internal static void Register(DMRangedMorphDriver d) { if (d != null) Active = d; }
        internal static void Unregister(DMRangedMorphDriver d) { if (Active == d) Active = null; }

        static bool LiveFor(ItemData item) =>
            Active != null && Active.isActiveAndEnabled && Active.IsLiveFor(item);

        public static bool IsMorphing => Active != null && Active.isActiveAndEnabled && Active.IsDrawnDMRanged && Active.IsMorphing;

        public static bool TryGetActiveForm(ItemData item, out DMRangedForm form)
        {
            form = null;
            var s = Settings;
            if (s == null || !IsDMRanged(item) || s.FormCount == 0) return false;
            int idx = LiveFor(item) ? Active.CurrentFormIndex : (s.drawStartsInPistol1 && FirstDrawPending ? s.HolsterIndex : LastFormIndex); // 28g: holstered shows the form it will draw in
            form = s.forms[Mathf.Clamp(idx, 0, s.FormCount - 1)];
            return true;
        }

        public static bool TryGetDrawnForm(out DMRangedForm form)
        {
            form = null;
            if (Active == null || !Active.isActiveAndEnabled || !Active.IsDrawnDMRanged) return false;
            return TryGetActiveForm(Active.DrawnItem, out form);
        }

        public static float BodyWeightScale => TryGetDrawnForm(out var f) && Settings != null ? Settings.Weight(f) : 1f;

        public static bool TryCycle(EquipmentController equipment, int dir)
        {
            if (equipment == null || !equipment.IsWeaponDrawn || !IsDMRanged(equipment.DrawnWeaponItem)) return false;
            // 2026-10-28e: DM Ranged drawn = key 1 / Y are always consumed here (never fall through to a weapon swap).
            if (ResolveDrawnDriver(equipment) == null) { Debug.LogWarning("[DMRanged] DM Ranged drawn but no live DM Ranged visual (Animation + name 'DM Ranged') under a Drawn_ slot."); return true; }
            // 2026-10-28b: key 1 is read by both the legacy and UITK shortcut paths; cycle once per frame.
            if (lastCycleFrame == Time.frameCount) return true;
            lastCycleFrame = Time.frameCount;
            Active.Cycle(dir);
            return true;
        }

        static int lastCycleFrame = -1;

        /// <summary>2026-10-28c: raised when the cycle target or the shown form changes (Hot Cross icon refresh).</summary>
        public static event Action FormChanged;
        internal static void RaiseFormChanged() { try { FormChanged?.Invoke(); } catch (Exception e) { Debug.LogException(e); } }

        /// <summary>2026-10-28b: icon of the active/last form for HUD + Hot Cross (item.icon is never mutated).</summary>
        public static Sprite FormIcon(ItemData item) =>
            TryGetActiveForm(item, out var f) && f.icon != null ? f.icon : null;

        public static Sprite FormHotCrossIcon(ItemData item) =>
            TryGetActiveForm(item, out var f) ? (f.hotCrossIcon != null ? f.hotCrossIcon : f.icon) : null;

        public static string DisplayName(ItemData item)
        {
            if (TryGetActiveForm(item, out var f) && !string.IsNullOrWhiteSpace(f.displayName)) return f.displayName;
            return item != null ? item.itemName : string.Empty;
        }

        public static string HudPrefix(ItemData item) =>
            TryGetActiveForm(item, out var f) && !string.IsNullOrWhiteSpace(f.hudLabel) ? f.hudLabel + " " : string.Empty;

        public static Transform ActiveMuzzle => Active != null && Active.isActiveAndEnabled ? Active.CurrentMuzzle : null;
    }
}

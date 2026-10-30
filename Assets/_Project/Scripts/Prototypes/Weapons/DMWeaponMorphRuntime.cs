using Project.Data;
using Project.Inventory;
using UnityEngine;

namespace Project.Prototypes.Weapons
{
    /// <summary>
    /// Static bridge: Y / same-slot cycle when a morph weapon is drawn; fire block; stats + body-weight overrides.
    /// </summary>
    public static class DMWeaponMorphRuntime
    {
        public static DMWeaponMorphController Active { get; private set; }
        public static bool IsMorphing => (Active != null && Active.IsMorphing) || Project.Combat.DMRanged.DMRangedMorphRuntime.IsMorphing; // 20261028a DM Ranged
        public static bool BlocksWeaponFire => IsMorphing;
        public static float ActiveBodyWeightScale => Project.Combat.DMRanged.DMRangedMorphRuntime.TryGetDrawnForm(out _) ? Project.Combat.DMRanged.DMRangedMorphRuntime.BodyWeightScale : ControllerBodyWeightScale();

        // 2026-10-28m: a holstered MorphBlade copy does not add its blade weight.
        static float ControllerBodyWeightScale()
        {
            if (Active == null) return 1f;
            if (Active.Kind == DMWeaponMorphKind.MeleeBlade && !UnderDrawnHolder(Active.transform)) return 1f;
            return Active.ActiveBodyWeightScale;
        }
        public static DM_WeaponMorphProfile ActiveProfile => Active != null ? Active.ActiveProfile : null;

        public static void Register(DMWeaponMorphController controller)
        {
            if (controller == null) return;
            Active = controller;
        }

        public static void Unregister(DMWeaponMorphController controller)
        {
            if (Active == controller)
                Active = null;
        }

        /// <summary>True when the drawn Hot Cross weapon hosts a morph controller.</summary>
        public static bool TryGetDrawnController(EquipmentController equipment, out DMWeaponMorphController controller)
        {
            controller = null;
            if (equipment == null || !equipment.IsWeaponDrawn)
                return false;

            ItemData drawn = equipment.DrawnWeaponItem;
            if (drawn == null)
                return false;

            // Prefer live visual under player (mounted PioneerVisual).
            var all = Object.FindObjectsByType<DMWeaponMorphController>(FindObjectsInactive.Exclude);
            DMWeaponMorphController fallback = null;
            for (int i = 0; i < all.Length; i++)
            {
                DMWeaponMorphController c = all[i];
                if (c == null || c.BoundItem != drawn)
                    continue;
                // Prefer the mounted held visual (PioneerVisual_*) over world pickups sharing the same ItemData.
                string n = c.gameObject.name;
                bool mounted = n.StartsWith("PioneerVisual_", System.StringComparison.Ordinal)
                    || c.transform.root == equipment.transform.root;
                // 2026-10-28l: authored Drawn_MorphBlade holder wins over the Holstered_ copy.
                if (mounted && !UnderDrawnHolder(c.transform))
                {
                    if (fallback == null || !UnderDrawnHolder(fallback.transform)) fallback = c;
                    continue;
                }
                if (mounted)
                {
                    controller = c;
                    Active = c;
                    return true;
                }
                if (fallback == null)
                    fallback = c;
            }
            if (fallback != null)
            {
                controller = fallback;
                Active = fallback;
                return true;
            }
            return false;
        }

        static bool UnderDrawnHolder(Transform t)
        {
            for (Transform p = t; p != null; p = p.parent)
            {
                if (p.name.StartsWith("Drawn_", System.StringComparison.Ordinal)) return true;
                if (p.name.StartsWith("Holstered_", System.StringComparison.Ordinal)) return false;
            }
            return true; // runtime-mounted PioneerVisual_ (no holder) keeps the old behaviour
        }

        /// <summary>Y / same-slot: cycle morph forms when morph weapon is drawn. Returns true if handled.</summary>
        public static bool TryCycleDrawnForm(EquipmentController equipment) => TryCycleDrawnForm(equipment, 1);

        /// <summary>20261028a: dir +1 forward (Tab / Y), -1 back (Shift+Tab). DM Ranged first, then prototype controllers.</summary>
        public static bool TryCycleDrawnForm(EquipmentController equipment, int dir)
        {
            if (Project.Combat.DMRanged.DMRangedMorphRuntime.TryCycle(equipment, dir))
                return true;
            if (!TryGetDrawnController(equipment, out DMWeaponMorphController controller))
                return false;
            return controller.TryCycleForm();
        }

        // ---- 2026-10-28m: MorphBlade shared blade form (Drawn_/Holstered_ copies, icons, damage element) ----
        const string BladeFormPrefKey = "DMMorphBlade.Form";
        static int s_bladeForm = -1;
        static ItemData s_bladeItem;
        static DM_WeaponMorphProfile[] s_bladeForms;

        /// <summary>Raised after the MorphBlade form changes (key 1 / pad Y). Controllers mirror it; UI polls the icon.</summary>
        public static event System.Action BladeFormChanged;

        public static int SharedBladeFormIndex
        {
            get
            {
                if (s_bladeForm < 0) s_bladeForm = Mathf.Max(0, PlayerPrefs.GetInt(BladeFormPrefKey, 0));
                return s_bladeForm;
            }
        }

        internal static void RegisterBladeForms(ItemData item, DM_WeaponMorphProfile[] forms)
        {
            if (item == null || forms == null || forms.Length == 0) return;
            s_bladeItem = item;
            s_bladeForms = forms;
        }

        internal static void SetSharedBladeForm(int index)
        {
            s_bladeForm = Mathf.Max(0, index);
            PlayerPrefs.SetInt(BladeFormPrefKey, s_bladeForm);
            try { BladeFormChanged?.Invoke(); } catch (System.Exception e) { Debug.LogException(e); }
            Project.Combat.DMRanged.DMRangedMorphRuntime.RaiseFormChanged(); // same form-changed signal as DM Ranged
        }

        public static bool TryGetBladeForm(ItemData item, out DM_WeaponMorphProfile profile)
        {
            profile = null;
            if (item == null || item != s_bladeItem || s_bladeForms == null || s_bladeForms.Length == 0) return false;
            profile = s_bladeForms[Mathf.Clamp(SharedBladeFormIndex, 0, s_bladeForms.Length - 1)];
            return profile != null;
        }

        public static Sprite BladeFormIcon(ItemData item) =>
            TryGetBladeForm(item, out var p) ? p.icon : null;

        public static Sprite BladeFormHotCrossIcon(ItemData item) =>
            TryGetBladeForm(item, out var p) ? (p.hotCrossIcon != null ? p.hotCrossIcon : p.icon) : null;

        public static bool TryGetBladeElement(ItemData item, out Project.Combat.AmmoType element)
        {
            element = Project.Combat.AmmoType.Gunpowder;
            if (!TryGetBladeForm(item, out var p)) return false;
            element = p.damageElement;
            return true;
        }

        /// <summary>Key 1 with MorphBlade drawn: cycle blade forms. False when the drawn weapon is not a morph blade.</summary>
        public static bool TryCycleDrawnBlade(EquipmentController equipment)
        {
            if (!TryGetDrawnController(equipment, out DMWeaponMorphController c) || c.Kind != DMWeaponMorphKind.MeleeBlade)
            {
                // 29a debug: drawn item is the MorphBlade but no live blade controller was found.
                if (equipment != null && equipment.IsWeaponDrawn && equipment.DrawnWeaponItem != null && equipment.DrawnWeaponItem == s_bladeItem)
                    Debug.LogWarning("[MorphBlade] blade switch ignored: no active DMWeaponMorphController bound to " + equipment.DrawnWeaponItem.name + " (check Drawn_MorphBlade holder is active and its controller's boundItem/forms).");
                return false;
            }
            bool ok = c.TryCycleForm();
            if (!ok)
                Debug.LogWarning("[MorphBlade] blade switch ignored on " + c.name + ": fewer than 2 forms assigned.", c);
            return ok;
        }

        public static bool TryGetEffectiveWeaponGrip(ItemData item, out WeaponGrip grip)
        {
            grip = WeaponGrip.OneHanded;
            if (Project.Combat.DMRanged.DMRangedMorphRuntime.TryGetActiveForm(item, out var dmrForm))
            {
                grip = dmrForm.useRifleScope ? WeaponGrip.TwoHanded : WeaponGrip.OneHanded; // Sniper uses the rifle scope
                return true;
            }
            if (item == null || Active == null || Active.BoundItem != item || Active.ActiveProfile == null)
                return false;
            if (Active.ActiveProfile.kind != DMWeaponMorphKind.Ranged)
                return false;
            grip = Active.ActiveProfile.ResolveGrip();
            if (Active.ActiveProfile.enableScopeAsRifle)
                grip = WeaponGrip.TwoHanded;
            return true;
        }

        public static bool TryApplyRangedOverrides(ItemData item, out float damage, out float damageRand, out float range,
            out float fireRate, out float spread, out float accuracy, out int mag, out float reload, out float aimFov)
        {
            damage = damageRand = range = fireRate = spread = accuracy = reload = aimFov = 0f;
            mag = 0;
            var p = ActiveProfile;
            if (item == null || Active == null || Active.BoundItem != item || p == null || p.kind != DMWeaponMorphKind.Ranged)
                return false;
            damage = p.ResolveRangedDamage();
            damageRand = p.ResolveRangedDamageRandom();
            range = p.ResolveRangedRange();
            fireRate = p.ResolveFireRate();
            spread = p.ResolveSpread();
            accuracy = p.ResolveAccuracy();
            mag = p.ResolveMagazine();
            reload = p.ResolveReload();
            aimFov = p.ResolveAimFov();
            return true;
        }

        public static bool TryApplyMeleeOverrides(ItemData item, out float damage, out float damageRand, out float range,
            out float bladeLength, out float cooldown)
        {
            damage = damageRand = range = bladeLength = cooldown = 0f;
            var p = ActiveProfile;
            if (item == null || Active == null || Active.BoundItem != item || p == null || p.kind != DMWeaponMorphKind.MeleeBlade)
                return false;
            damage = p.meleeDamage;
            damageRand = p.meleeDamageRandomRange;
            range = p.meleeRange;
            bladeLength = p.meleeBladeLength;
            cooldown = p.meleeCooldown;
            return true;
        }
    }
}

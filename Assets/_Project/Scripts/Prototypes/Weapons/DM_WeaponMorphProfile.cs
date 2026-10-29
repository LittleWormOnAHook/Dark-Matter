using System;
using Project.Data;
using UnityEngine;

namespace Project.Prototypes.Weapons
{
    /// <summary>One morph form (gun or blade). Data-driven list entries on DMWeaponMorphController.</summary>
    [CreateAssetMenu(fileName = "DM_WeaponMorphProfile", menuName = "Dark Matter Genesis/Combat/Weapon Morph Profile", order = 62)]
    public sealed class DM_WeaponMorphProfile : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Form";
        public DMWeaponMorphKind kind = DMWeaponMorphKind.Ranged;
        public DMWeaponMorphGunForm gunForm = DMWeaponMorphGunForm.Pistol;
        public DMWeaponMorphBladeType bladeType = DMWeaponMorphBladeType.Metal;

        [Header("Optional stats source (ranged)")]
        [Tooltip("When set, ranged stats are copied from this ItemData (e.g. sci_fi_pistol / survival_rifle).")]
        public ItemData statsSource;

        [Header("Ranged overrides (used when statsSource null or field > 0 wins per builder)")]
        public float rangedDamage = 14f;
        public float rangedDamageRandomRange = 4f;
        public float rangedRange = 32f;
        public float fireRate = 2.5f;
        public float projectileSpreadDegrees = 1.8f;
        public float weaponAccuracy = 70f;
        public int magazineSize = 10;
        public float reloadTimeSeconds = 1.6f;
        public float aimFovMultiplier = 0.84f;
        public WeaponGrip weaponGrip = WeaponGrip.OneHanded;
        [Tooltip("Sniper form: treat as rifle for DMScopeMode when Scope mode is armed.")]
        public bool enableScopeAsRifle;

        [Header("Melee overrides")]
        public float meleeDamage = 15f;
        public float meleeDamageRandomRange = 4f;
        public float meleeRange = 2.2f;
        public float meleeBladeLength = 0.85f;
        public float meleeCooldown = 0.6f;
        public float criticalChance = 0.12f;
        public float criticalDamageMultiplier = 2f;
        public float meleeStaminaCost = 8f;

        [Header("MorphBlade (2026-10-28m)")]
        [Tooltip("Damage element of this blade form (melee hits read it via DMEnemyHitFx.ResolveMeleeElement while MorphBlade is on this form).")]
        public Project.Combat.AmmoType damageElement = Project.Combat.AmmoType.Gunpowder;
        [Tooltip("Hotbar icon for this form (Resources/UI/Game Icons). Null = item icon.")]
        public Sprite icon;
        [Tooltip("Hot Cross alpha icon for this form (Resources/UI/HotCrossIcons). Null = icon.")]
        public Sprite hotCrossIcon;
        [Tooltip("2026-10-28o: EAU MorphBlade - material put on the 'Blade' child at swap frame 20. Null = Studio settings material for this blade type.")]
        public Material bladeMaterial;
        [Tooltip("2026-10-28o: VFX prefab run along the blade (base -> MB_BladeTip). Null = Studio settings VFX for this blade type (Steel has none).")]
        public GameObject bladeVfx;        [Header("Visual")]
        public Color bladeColor = new Color(0.75f, 0.78f, 0.82f, 1f);
        [Range(0f, 8f)] public float bladeEmission = 0f;
        [Tooltip("Blade length scale along local forward (Y on capsule/cube). Hitbox follows.")]
        public float bladeLengthScale = 1f;
        public Color accentColor = new Color(0.35f, 0.4f, 0.45f, 1f);

        [Header("Body weight (DMPlayerBodyWeight)")]
        [Tooltip("Multiplies profile weapon weight while this form is active. Pistol ~0.85, Rifle ~1.15, Sniper ~1.35. MorphBlade forms: multiplied with the Studio settings per-blade weight (keep 1 to use the Studio value alone).")]
        [Range(0.1f, 5f)] public float bodyWeightScale = 1f;

        [Header("Part poses (optional; names match child transforms on morph root)")]
        public MorphPartPose[] partPoses = Array.Empty<MorphPartPose>();

        public float ResolveRangedDamage() =>
            statsSource != null && statsSource.rangedDamage > 0.01f ? statsSource.rangedDamage : rangedDamage;

        public float ResolveRangedDamageRandom() =>
            statsSource != null ? statsSource.rangedDamageRandomRange : rangedDamageRandomRange;

        public float ResolveRangedRange() =>
            statsSource != null && statsSource.rangedRange > 0.01f ? statsSource.rangedRange : rangedRange;

        public float ResolveFireRate() =>
            statsSource != null && statsSource.fireRate > 0.01f ? statsSource.fireRate : fireRate;

        public float ResolveSpread() =>
            statsSource != null ? statsSource.projectileSpreadDegrees : projectileSpreadDegrees;

        public float ResolveAccuracy() =>
            statsSource != null && statsSource.weaponAccuracy > 0.01f ? statsSource.weaponAccuracy : weaponAccuracy;

        public int ResolveMagazine() =>
            statsSource != null && statsSource.magazineSize > 0 ? statsSource.magazineSize : magazineSize;

        public float ResolveReload() =>
            statsSource != null && statsSource.reloadTimeSeconds > 0.01f ? statsSource.reloadTimeSeconds : reloadTimeSeconds;

        public float ResolveAimFov() =>
            statsSource != null && statsSource.aimFovMultiplier > 0.01f ? statsSource.aimFovMultiplier : aimFovMultiplier;

        public WeaponGrip ResolveGrip() =>
            statsSource != null ? statsSource.weaponGrip : weaponGrip;
    }

    [Serializable]
    public struct MorphPartPose
    {
        public string partName;
        public Vector3 localPosition;
        public Vector3 localEuler;
        public Vector3 localScale;
    }
}

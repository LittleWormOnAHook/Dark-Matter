using System;
using FORGE3D;
using Project.Combat;
using Project.Data;
using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>
    /// Per-prefab / per-instance overrides on <see cref="DMTurretRuntime"/>.
    /// When a toggle is off, the runtime serialized fields (then profile defaults) supply that value.
    /// </summary>
    [Serializable]
    public sealed class DMTurretInstanceAuthority
    {
        [Header("Ammo")]
        [Tooltip("When on, Ammo Mode below wins over the runtime / profile default.")]
        public bool overrideAmmoMode;
        public DMTurretAmmoMode ammoMode = DMTurretAmmoMode.Forge3DVfx;

        [Tooltip("When on, Vendor VFX below wins over the runtime F3DFXType.")]
        public bool overrideVendorFxType;
        public F3DFXType vendorFxType = F3DFXType.Vulcan;

        [Tooltip("When on, Ammo Type below wins over the runtime / profile ammoType.")]
        public bool overrideAmmoType;
        public AmmoType ammoType = AmmoType.Gunpowder;

        [Header("Barrel")]
        [Tooltip("When on, Barrel below wins over the runtime barrel kind.")]
        public bool overrideBarrelKind;
        public DMTurretBarrelKind barrelKind = DMTurretBarrelKind.Auto;

        [Tooltip("When on, Database Barrel below wins over the runtime catalog barrel id.")]
        public bool overrideDatabaseBarrel;
        [Tooltip("FORGE3D database.asset barrel prefab name. Empty = Auto (socket enable).")]
        public string databaseBarrelId;

        [Header("Game combat ammo (ItemData path)")]
        [Tooltip("When on, Game Weapon / Ammo below win over profile ItemData references.")]
        public bool overrideGameAmmoItems;
        public ItemData gameWeaponItem;
        public ItemData gameAmmoItem;

        [Tooltip("When on, Game Fire Interval below wins over profile gameFireInterval.")]
        public bool overrideGameFireInterval;
        public float gameFireInterval = 0.15f;

        [Tooltip("When on, Game Spread below wins over profile gameSpreadDegrees.")]
        public bool overrideGameSpread;
        [Range(0f, 12f)] public float gameSpreadDegrees = 1.5f;

        [Header("Damage")]
        public bool overrideDamage;
        public float damage = 18f;

        public bool overrideSplashRadius;
        public float splashRadius;

        public bool overrideBeamDamageInterval;
        public float beamDamageInterval = 0.2f;

        [Header("Ranges (meters)")]
        public bool overrideScanRange;
        public float scanRange = 45f;

        public bool overrideTrackRange;
        public float trackRange = 32f;

        public bool overrideFireRange;
        public float fireRange = 22f;

        [Header("Hull")]
        public bool overrideMaxHealth;
        public float maxHealth = 100f;

        public bool Any =>
            overrideAmmoMode || overrideVendorFxType || overrideAmmoType || overrideBarrelKind
            || overrideDatabaseBarrel
            || overrideGameAmmoItems || overrideGameFireInterval
            || overrideGameSpread || overrideDamage || overrideSplashRadius || overrideBeamDamageInterval
            || overrideScanRange || overrideTrackRange || overrideFireRange || overrideMaxHealth;
    }
}

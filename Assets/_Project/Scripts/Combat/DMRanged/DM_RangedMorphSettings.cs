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
    public enum DMRangedPreset { Subtle, Default, Extreme }

    [Serializable]
    public sealed class DMRangedForm
    {
        public string id = "pistol_1";
        public string displayName = "Pistol 1";
        public string hudLabel = "P1";
        [Range(0, 40)] public int frame = 40;
        [Range(0.1f, 30f)] public float fireRate = 2f;
        [Range(0.05f, 5f)] public float damageMultiplier = 1.4f;
        [Range(5f, 400f)] public float range = 60f;
        [Tooltip("Base cone spread in degrees (absolute, overrides weapon/ammo spread).")]
        [Range(0f, 10f)] public float spreadDegrees = 0.4f;
        [Range(0.05f, 5f)] public float recoilMultiplier = 1.2f;
        [Range(1, 200)] public int magazineSize = 8;
        [Range(0.1f, 8f)] public float reloadSeconds = 1.5f;
        [Range(0.3f, 2.5f)] public float shotPitch = 0.95f;
        [Range(0.25f, 3f)] public float bodyWeight = 0.85f;
        [Tooltip("Optional per-form shot sound. Empty = ammo/weapon sound, still pitched.")]
        public AudioClip shotClip;
        [Tooltip("Rifle form: rifle hold/aim pose; treated as a rifle by DMScopeMode (rifle scope, DM_ScopeProfile.rifleScopedRange).")]
        public bool useRifleScope;
        [Range(5f, 150f)] public float scopeDistance = 50f;
        [Tooltip("2026-10-28h: full auto like the old Survival rifle (vShooterWeapon.automaticWeapon): hold = full auto, tap = semi. Always single shot while Scope mode (hold-R menu) is scoped.")]
        public bool automatic;
        [Tooltip("2026-10-28j: old Survival rifle magazine rule: loaded ammo ItemData.magazineSize, else DM_RangedMorphSettings.rifleFallbackMagazine (50). magazineSize below is ignored for this form.")]
        public bool magazineByAmmo;
        [Tooltip("2026-10-28b: per-form HUD / Hot Cross icon. Empty = item icon.")]
        public Sprite icon;
        [Tooltip("2026-10-28b: transparent (alpha) cutout for the Hot Cross. Empty = icon.")]
        public Sprite hotCrossIcon;
    }

    [Serializable]
    public sealed class DMRangedAmmoMagazine
    {
        [Tooltip("Ammo ItemData itemName (case-insensitive).")]
        public string ammoName;
        [Range(1, 200)] public int magazineSize = 35;
        public DMRangedAmmoMagazine() { }
        public DMRangedAmmoMagazine(string n, int m) { ammoName = n; magazineSize = m; }
    }

    [CreateAssetMenu(menuName = "Dark Matter/Combat/DM Ranged Morph Settings")]
    public sealed class DM_RangedMorphSettings : ScriptableObject
    {
        public const string ResourcePath = "Combat/Morph/DMRanged/DM_RangedMorphSettings";
        static DM_RangedMorphSettings s_instance;
        public static DM_RangedMorphSettings Instance
        {
            get
            {
                if (s_instance == null) s_instance = Resources.Load<DM_RangedMorphSettings>(ResourcePath);
                return s_instance;
            }
        }

        [Tooltip("ItemData this settings asset drives (dm_ranged).")]
        public ItemData item;
        [Range(0.02f, 1f)] public float stepSeconds = 0.15f;
        [Tooltip("Frame the weapon always holsters at (Pistol 1).")]
        [Range(0, 40)] public int holsterFrame = 40;
        [Tooltip("When on, sci_fi_pistol cannot be placed in or drawn from the player hotbar. The asset is kept.")]
        public bool disableSciFiPistolForPlayer = true;
        public ItemData disabledPlayerItem;
        [Tooltip("2026-10-28g: when on, survival_rifle (Sci-fi rifle) is retired for the player: no hotbar / Hot Cross, no draw, no holster visual, no player bridge slot, not granted as a starting item. DM Ranged keeps using its Drawn_/Holstered_Survival_Rifle holders. Enemies / companions / vendors are unaffected. The asset is kept.")]
        public bool disableSurvivalRifleForPlayer = true;
        public ItemData disabledPlayerRifle;
        [Tooltip("2026-10-28n: when on, every player melee weapon except MorphBlade is retired for the player (no hotbar / Hot Cross, no draw, no holster visual, no player bridge slot, not a starting item). Enemies keep theirs; assets and placements are kept. Mining tool is not a melee weapon and is unaffected.")]
        public bool morphBladeOnlyPlayerMelee = true;
        [Tooltip("2026-10-28n: the melee item kept when morphBladeOnlyPlayerMelee is on (empty = asset named MorphBlade).")]
        public ItemData keptPlayerMelee;
        [Tooltip("2026-10-28g: the first draw after pickup / new game starts in Pistol 1 (frame 40, no morph). Later draws morph from frame 40 to the form it was in when holstered. Off = every draw morphs to the last form.")]
        public bool drawStartsInPistol1 = true;
        [Header("Studio")]
        public DMRangedPreset preset = DMRangedPreset.Default;
        [Tooltip("Scales each form's difference from Pistol 1 (Subtle 0.5, Default 1, Extreme 1.5).")]
        [Range(0f, 3f)] public float differenceScale = 1f;
        [Header("Step Sound (2026-10-28b)")]
        [Tooltip("Mechanical transition sound played on every form step, via the weapon's AudioSource.")]
        public AudioClip stepClip;
        [Range(0f, 2f)] public float stepVolume = 1f;
        [Tooltip("Random +/- pitch per step.")]
        [Range(0f, 0.3f)] public float stepPitchJitter = 0.06f;
        public DMRangedForm[] forms = DefaultForms();
        [Header("Rifle Magazine (2026-10-28j, old Survival rifle rule)")]
        [Tooltip("Forms with magazineByAmmo (Rifle) use the old Survival rifle rule: the loaded ammo's ItemData.magazineSize (edit per ammo in Studio's ammo profiles); when the ammo has none / no ammo, this value (survival_rifle.asset magazineSize 50).")]
        [Range(1, 200)] public int rifleFallbackMagazine = 50;
        /// <summary>Magazine for a form + loaded ammo (28j). Non-magazineByAmmo forms use Magazine(f).</summary>
        public int Magazine(DMRangedForm f, ItemData ammo)
        {
            if (f == null || !f.magazineByAmmo) return Magazine(f);
            if (ammo != null && ammo.magazineSize > 0) return ammo.magazineSize;
            return Mathf.Max(1, rifleFallbackMagazine);
        }

        void OnValidate()
        {
            float want = preset == DMRangedPreset.Subtle ? 0.5f : preset == DMRangedPreset.Extreme ? 1.5f : 1f;
            if (!Mathf.Approximately(differenceScale, want) && lastPreset != preset) { differenceScale = want; ApplyStepPreset(); }
            lastPreset = preset;
        }
        [SerializeField, HideInInspector] DMRangedPreset lastPreset = DMRangedPreset.Default;

        [ContextMenu("Preset/Subtle")] void PresetSubtle() { preset = lastPreset = DMRangedPreset.Subtle; differenceScale = 0.5f; ApplyStepPreset(); }
        [ContextMenu("Preset/Default")] void PresetDefault() { preset = lastPreset = DMRangedPreset.Default; differenceScale = 1f; ApplyStepPreset(); }
        [ContextMenu("Preset/Extreme")] void PresetExtreme() { preset = lastPreset = DMRangedPreset.Extreme; differenceScale = 1.5f; ApplyStepPreset(); }
        // Presets also set the step sound (Subtle 0.6 / 0.03, Default 1.0 / 0.06, Extreme 1.4 / 0.10).
        void ApplyStepPreset()
        {
            stepVolume = preset == DMRangedPreset.Subtle ? 0.6f : preset == DMRangedPreset.Extreme ? 1.4f : 1f;
            stepPitchJitter = preset == DMRangedPreset.Subtle ? 0.03f : preset == DMRangedPreset.Extreme ? 0.1f : 0.06f;
        }
        [ContextMenu("Reset Forms To Spec")] void ResetForms() { forms = DefaultForms(); }

        public static DMRangedForm[] DefaultForms() => new[]
        {
            // 2026-10-28i: frame 0 = "Rifle" (id stays "sniper"), normal damage x1.0 (ammo/profiles); NOT the hold-R Scope mode.
            // 2026-10-28h: Rifle + SMR use the old Survival rifle cadence (5.5 shots/s, automatic).
            F("sniper", "DM Rifle", "RIFLE", 0, 5.5f, 1f, 150f, 0.1f, 1.6f, 50, 2.6f, 0.85f, 1.2f, true, true, true),
            F("smp", "DM Sub-Machine Rifle", "SMR", 10, 5.5f, 0.45f, 35f, 1.4f, 0.5f, 30, 1.8f, 1.15f, 1.05f, false, true),
            F("pistol_3", "DM Pistol 3", "P3", 20, 5f, 0.8f, 45f, 0.8f, 0.8f, 15, 1.4f, 1.05f, 0.95f, false),
            F("pistol_2", "DM Pistol 2", "P2", 30, 3.5f, 1.0f, 50f, 0.6f, 1.0f, 12, 1.3f, 1.0f, 0.9f, false),
            F("pistol_1", "DM Pistol 1", "P1", 40, 2f, 1.4f, 60f, 0.4f, 1.2f, 8, 1.5f, 0.95f, 0.85f, false),
        };

        static DMRangedForm F(string id, string n, string hud, int frame, float rate, float dmg, float range, float spread,
            float recoil, int mag, float reload, float pitch, float weight, bool scope, bool auto = false, bool magByAmmo = false) => new DMRangedForm
        {
            id = id, displayName = n, hudLabel = hud, frame = frame, fireRate = rate, damageMultiplier = dmg, range = range,
            spreadDegrees = spread, recoilMultiplier = recoil, magazineSize = mag, reloadSeconds = reload, shotPitch = pitch,
            bodyWeight = weight, useRifleScope = scope, scopeDistance = 50f, automatic = auto, magazineByAmmo = magByAmmo
        };

        public int FormCount => forms != null ? forms.Length : 0;
        public int HolsterIndex
        {
            get
            {
                for (int i = 0; i < FormCount; i++) if (forms[i].frame == holsterFrame) return i;
                return Mathf.Max(0, FormCount - 1);
            }
        }
        DMRangedForm Baseline => forms[HolsterIndex];
        float S(float b, float v) => Mathf.Max(0.001f, Mathf.LerpUnclamped(b, v, differenceScale));

        public float FireRate(DMRangedForm f) => S(Baseline.fireRate, f.fireRate);
        public float DamageMul(DMRangedForm f) => S(Baseline.damageMultiplier, f.damageMultiplier);
        public float Range(DMRangedForm f) => S(Baseline.range, f.range);
        public float Spread(DMRangedForm f) => Mathf.Max(0f, Mathf.LerpUnclamped(Baseline.spreadDegrees, f.spreadDegrees, differenceScale));
        public float RecoilMul(DMRangedForm f) => S(Baseline.recoilMultiplier, f.recoilMultiplier);
        public int Magazine(DMRangedForm f) => Mathf.Max(1, Mathf.RoundToInt(S(Baseline.magazineSize, f.magazineSize)));
        public float Reload(DMRangedForm f) => S(Baseline.reloadSeconds, f.reloadSeconds);
        public float Pitch(DMRangedForm f) => S(Baseline.shotPitch, f.shotPitch);
        public float Weight(DMRangedForm f) => S(Baseline.bodyWeight, f.bodyWeight);
    }
}

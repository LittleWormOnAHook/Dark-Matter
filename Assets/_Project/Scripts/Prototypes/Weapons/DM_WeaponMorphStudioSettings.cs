using UnityEngine;

namespace Project.Prototypes.Weapons
{
    /// <summary>
    /// Global morph timing / curves / colours. Studio presets Subtle / Default / Extreme
    /// (same stash style as body-weight ClimbDash presets).
    /// </summary>
    [CreateAssetMenu(fileName = "DM_WeaponMorphStudioSettings", menuName = "Dark Matter Genesis/Combat/Weapon Morph Studio Settings", order = 61)]
    public sealed class DM_WeaponMorphStudioSettings : ScriptableObject
    {
        public const string ResourcesPath = "Combat/Morph/DM_WeaponMorphStudioSettings";

        public enum Preset
        {
            Subtle = 0,
            Default = 1,
            Extreme = 2,
            Custom = 3
        }

        [Header("Preset")]
        public Preset preset = Preset.Default;

        [Header("Timing")]
        [Range(0.05f, 2f)] public float morphDurationSeconds = 0.3f;
        public AnimationCurve morphCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Range(0.05f, 2f)] public float bladeExtendSeconds = 0.22f;
        public AnimationCurve bladeExtendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Gun part defaults (wide ranges for Studio)")]
        [Range(0.1f, 3f)] public float gunMorphDurationMin = 0.1f;
        [Range(0.1f, 3f)] public float gunMorphDurationMax = 1.2f;

        [Header("Blade colours (defaults; profiles can override)")]
        public Color metalColor = new Color(0.72f, 0.74f, 0.78f, 1f);
        public Color lightColor = new Color(0.85f, 0.95f, 1f, 0.85f);
        public Color plasmaColor = new Color(0.35f, 1f, 0.55f, 0.9f);
        public Color ionColor = new Color(0.55f, 0.35f, 1f, 0.9f);
        public Color laserColor = new Color(1f, 0.25f, 0.3f, 0.95f);
        public Color cryoColor = new Color(0.45f, 0.85f, 1f, 0.9f);
        public Color lightningColor = new Color(0.75f, 0.85f, 1f, 0.95f);

        [Header("Blade length metres (grip to tip)")]
        [Range(0.2f, 3f)] public float metalLength = 0.85f;
        [Range(0.2f, 3f)] public float lightLength = 1.05f;
        [Range(0.2f, 3f)] public float plasmaLength = 1.15f;
        [Range(0.2f, 3f)] public float ionLength = 1.0f;
        [Range(0.2f, 3f)] public float laserLength = 1.25f;
        [Range(0.2f, 3f)] public float cryoLength = 0.95f;
        [Range(0.2f, 3f)] public float lightningLength = 1.05f;

        public enum BladeWeightPreset { Custom, Subtle, Default, Extreme }

        [Header("MorphBlade body weight per blade (2026-10-28m; DMPlayerBodyWeight melee weight multiplier)")]
        [Tooltip("Subtle = half the Default spread around 1, Default = Metal 1.15 / Light 0.9 / Plasma 1.05 / Ion 1.0 / Laser 0.85 / Cryo 1.2, Extreme = double spread. Picking one writes the fields below; editing them by hand is fine (Custom).")]
        public BladeWeightPreset bladeWeightPreset = BladeWeightPreset.Default;
        [SerializeField, HideInInspector] private BladeWeightPreset _lastBladeWeightPreset = BladeWeightPreset.Default;
        [Range(0.1f, 5f)] public float metalWeight = 1.15f;
        [Range(0.1f, 5f)] public float lightWeight = 0.9f;
        [Range(0.1f, 5f)] public float plasmaWeight = 1.05f;
        [Range(0.1f, 5f)] public float ionWeight = 1.0f;
        [Range(0.1f, 5f)] public float laserWeight = 0.85f;
        [Range(0.1f, 5f)] public float cryoWeight = 1.2f;
        [Range(0.1f, 5f)] public float lightningWeight = 0.95f;

        public float WeightForBlade(DMWeaponMorphBladeType type)
        {
            switch (type)
            {
                case DMWeaponMorphBladeType.Light: return lightWeight;
                case DMWeaponMorphBladeType.Plasma: return plasmaWeight;
                case DMWeaponMorphBladeType.Ion: return ionWeight;
                case DMWeaponMorphBladeType.Laser: return laserWeight;
                case DMWeaponMorphBladeType.Cryo: return cryoWeight;
                case DMWeaponMorphBladeType.Lightning: return lightningWeight;
                default: return metalWeight;
            }
        }

        public void ApplyBladeWeightPreset(BladeWeightPreset next)
        {
            bladeWeightPreset = next;
            _lastBladeWeightPreset = next;
            float k;
            switch (next)
            {
                case BladeWeightPreset.Subtle: k = 0.5f; break;
                case BladeWeightPreset.Default: k = 1f; break;
                case BladeWeightPreset.Extreme: k = 2f; break;
                default: return;
            }
            metalWeight = 1f + 0.15f * k;
            lightWeight = 1f - 0.1f * k;
            plasmaWeight = 1f + 0.05f * k;
            ionWeight = 1f;
            laserWeight = 1f - 0.15f * k;
            cryoWeight = 1f + 0.2f * k;
            lightningWeight = 1f - 0.05f * k;
        }

        [Header("EAU MorphBlade swap (2026-10-28o; Animator Take 001, key 1 = frame 0 -> swapFrame, material swap, -> 0)")]
        public string swapAnimatorState = "Take 001";
        [Range(1, 60)] public int swapFrame = 20;
        [Range(1f, 120f)] public float swapClipFps = 30f;
        [Tooltip("Seconds for each half (0->swapFrame and back). 0.667 = clip speed at 30 fps.")]
        [Range(0.05f, 3f)] public float swapHalfSeconds = 0.667f;
        public Material metalMaterial;
        public Material lightMaterial;
        public Material plasmaMaterial;
        public Material ionMaterial;
        public Material laserMaterial;
        public Material cryoMaterial;
        public Material lightningMaterial;

        [Header("EAU MorphBlade VFX (2026-10-28o; runs blade base -> MB_BladeTip, Steel has none)")]
        public GameObject lightVfx;
        public GameObject plasmaVfx;
        public GameObject ionVfx;
        public GameObject laserVfx;
        public GameObject cryoVfx;
        public GameObject lightningVfx;
        [Tooltip("Off = VFX only while drawn. On = holstered copy shows the VFX too.")]
        public bool vfxWhenHolstered = false;
        [Range(0f, 3f)] public float vfxIntensity = 1f;

        [Header("EAU MorphBlade swap audio (2026-10-28o)")]
        public AudioClip swapClip;
        [Range(0f, 1f)] public float swapVolume = 0.8f;
        [Range(0.5f, 2f)] public float swapPitch = 1f;
        [Range(0f, 0.3f)] public float swapPitchRandom = 0.05f;

        public GameObject VfxForBlade(DMWeaponMorphBladeType type)
        {
            switch (type)
            {
                case DMWeaponMorphBladeType.Light: return lightVfx;
                case DMWeaponMorphBladeType.Plasma: return plasmaVfx;
                case DMWeaponMorphBladeType.Ion: return ionVfx;
                case DMWeaponMorphBladeType.Laser: return laserVfx;
                case DMWeaponMorphBladeType.Cryo: return cryoVfx;
                case DMWeaponMorphBladeType.Lightning: return lightningVfx;
                default: return null; // Steel: no VFX
            }
        }
        public Material MaterialForBlade(DMWeaponMorphBladeType type)
        {
            switch (type)
            {
                case DMWeaponMorphBladeType.Light: return lightMaterial;
                case DMWeaponMorphBladeType.Plasma: return plasmaMaterial;
                case DMWeaponMorphBladeType.Ion: return ionMaterial;
                case DMWeaponMorphBladeType.Laser: return laserMaterial;
                case DMWeaponMorphBladeType.Cryo: return cryoMaterial;
                case DMWeaponMorphBladeType.Lightning: return lightningMaterial;
                default: return metalMaterial;
            }
        }

        [SerializeField, HideInInspector] private float _stashDuration = 0.3f;
        [SerializeField, HideInInspector] private float _stashBladeExtend = 0.22f;

        private static DM_WeaponMorphStudioSettings _live;

        public static DM_WeaponMorphStudioSettings Live
        {
            get
            {
                if (_live == null)
                    _live = Resources.Load<DM_WeaponMorphStudioSettings>(ResourcesPath);
                return _live;
            }
        }

        public float MorphDuration => Mathf.Clamp(morphDurationSeconds, gunMorphDurationMin, gunMorphDurationMax);

        public Color ColorForBlade(DMWeaponMorphBladeType type)
        {
            switch (type)
            {
                case DMWeaponMorphBladeType.Light: return lightColor;
                case DMWeaponMorphBladeType.Plasma: return plasmaColor;
                case DMWeaponMorphBladeType.Ion: return ionColor;
                case DMWeaponMorphBladeType.Laser: return laserColor;
                case DMWeaponMorphBladeType.Cryo: return cryoColor;
                case DMWeaponMorphBladeType.Lightning: return lightningColor;
                default: return metalColor;
            }
        }

        public float LengthForBlade(DMWeaponMorphBladeType type)
        {
            switch (type)
            {
                case DMWeaponMorphBladeType.Light: return lightLength;
                case DMWeaponMorphBladeType.Plasma: return plasmaLength;
                case DMWeaponMorphBladeType.Ion: return ionLength;
                case DMWeaponMorphBladeType.Laser: return laserLength;
                case DMWeaponMorphBladeType.Cryo: return cryoLength;
                case DMWeaponMorphBladeType.Lightning: return lightningLength;
                default: return metalLength;
            }
        }

        public void ApplyPreset(Preset next)
        {
            if (preset == Preset.Custom && next != Preset.Custom)
            {
                _stashDuration = morphDurationSeconds;
                _stashBladeExtend = bladeExtendSeconds;
            }

            preset = next;
            switch (next)
            {
                case Preset.Subtle:
                    morphDurationSeconds = 0.45f;
                    bladeExtendSeconds = 0.35f;
                    vfxIntensity = 0.6f; swapVolume = 0.6f;
                    morphCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
                    break;
                case Preset.Extreme:
                    morphDurationSeconds = 0.15f;
                    bladeExtendSeconds = 0.12f;
                    vfxIntensity = 1.6f; swapVolume = 1f;
                    morphCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
                    break;
                case Preset.Custom:
                    morphDurationSeconds = _stashDuration > 0.01f ? _stashDuration : 0.3f;
                    bladeExtendSeconds = _stashBladeExtend > 0.01f ? _stashBladeExtend : 0.22f;
                    break;
                default:
                    morphDurationSeconds = 0.3f;
                    bladeExtendSeconds = 0.22f;
                    vfxIntensity = 1f; swapVolume = 0.8f;
                    morphCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
                    break;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (bladeWeightPreset != _lastBladeWeightPreset)
                ApplyBladeWeightPreset(bladeWeightPreset);
            if (preset != Preset.Custom)
            {
                // Keep inspector edits that move away from preset values → Custom.
            }
        }
#endif
    }
}

using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-13 (h): player scope / sniper mode settings (Studio-editable, Resources/Combat/DM_ScopeProfile).
    /// Scope mode is toggled from the hold-R Mode Switch menu; while it is on, aiming (RMB) with the rifle or pistol
    /// opens the full scope view (UITK overlay + FOV zoom). Skill ranks (Rifles tree, "Scope" skills) scale these values.
    /// </summary>
    [CreateAssetMenu(menuName = "Project/Combat/Scope Profile", fileName = "DM_ScopeProfile")]
    public sealed class DM_ScopeProfile : ScriptableObject
    {
        public const string ResourcePath = "Combat/DM_ScopeProfile";

        [Header("Master")]
        public bool scopeEnabled = true;
        [Tooltip("Scope is an upgrade; unlocked for now. When false, ScopeUnlockedHook (code) decides.")]
        public bool scopeUnlockedByDefault = true;

        [Header("Range (metres, scoped)")]
        [Range(5f, 150f)] public float rifleScopedRange = 50f;
        [Range(5f, 100f)] public float pistolScopedRange = 28f;

        [Header("Zoom")]
        [Range(3f, 60f)] public float rifleScopedFov = 14f;
        [Range(3f, 60f)] public float pistolScopedFov = 26f;
        [Tooltip("Seconds to reach full zoom (Quick Scope skill shortens it).")]
        [Range(0.02f, 2f)] public float scopeInSeconds = 0.3f;
        [Range(0.02f, 2f)] public float scopeOutSeconds = 0.15f;

        [Header("Fire interval (seconds per shot while scoped)")]
        [Range(0.2f, 5f)] public float rifleScopedFireInterval = 2.5f;
        [Range(0.2f, 5f)] public float pistolScopedFireInterval = 2f;
        [Tooltip("Scoped Cadence skill never goes below this.")]
        [Range(0.1f, 5f)] public float minScopedFireInterval = 1f;

        [Header("Damage (scoped hits on enemies; base damage comes from the ammo profile)")]
        [Range(1f, 5f)] public float scopedHeadMultiplier = 2f;
        [Range(1f, 5f)] public float scopedChestMultiplier = 1.25f;

        [Header("Accuracy (hooks: skills / future attachments add to these)")]
        [Range(0f, 100f)] public float scopedAccuracyBonusPercent = 25f;
        [Tooltip("Reticle sway in pixels at full zoom; Steady Aim reduces it.")]
        [Range(0f, 40f)] public float reticleSwayPixels = 6f;
        [Range(0.05f, 3f)] public float reticleSwaySpeed = 0.6f;

        [Header("Ammo allowed while scoped (matched against the loaded ammo item / FX name)")]
        public string[] allowedAmmoKeywords = { "Standard", "Plasma" };

        [Header("Shot audio while scoped")]
        [Range(0.1f, 3f)] public float scopedShotVolume = 1.2f;
        [Range(0.3f, 2f)] public float scopedShotPitch = 0.85f;

        [Header("Skill scaling (percent per skill rank comes from the skill asset; these clamp the result)")]
        [Range(0f, 0.95f)] public float maxSwayReduction = 0.85f;
        [Range(0f, 0.9f)] public float maxScopeInReduction = 0.7f;
        [Range(0f, 1f)] public float maxExtraZoom = 0.5f;

        [Header("Overlay")]
        [Tooltip("Rifle scope only: center crosshair sprite (Invector sniperCrossHair). Pistol scope keeps procedural/legacy art.")]
        public Sprite scopeCrosshairSprite;
        [Tooltip("When true, rifle scope uses scopeCrosshairSprite + binocular-style mask. Pistol scope is unchanged.")]
        public bool scopeOverlayUseSpriteCrosshair = true;

        public const string RifleCrosshairResourcesPath = "UI/Combat/sniperCrossHair";
        private static Sprite s_resolvedRifleCrosshair;

        /// <summary>Rifle sniper overlay sprite; asset reference, then Resources fallback.</summary>
        public Sprite ResolveRifleScopeCrosshairSprite()
        {
            if (scopeCrosshairSprite != null)
                return scopeCrosshairSprite;
            if (s_resolvedRifleCrosshair == null)
                s_resolvedRifleCrosshair = Resources.Load<Sprite>(RifleCrosshairResourcesPath);
            return s_resolvedRifleCrosshair;
        }
        [Range(0.15f, 0.55f)] public float rifleScopeMaskRadius = 0.34f;
        [Tooltip("Multiplies rifle circular mask radius (0.8 ≈ 20% smaller hole).")]
        [Range(0.5f, 1f)] public float rifleScopeMaskScale = 0.8f;
        [Tooltip("Rifle sniperCrossHair UITK sprite size multiplier (0.8 = 20% smaller than base 196px).")]
        [Range(0.35f, 1.5f)] public float rifleScopeCrosshairScale = 0.8f;
        [Range(0.15f, 0.55f)] public float pistolScopeMaskRadius = 0.4f;
        [Tooltip("Pistol scope procedural reticle size (0.25 = quarter size).")]
        [Range(0.1f, 1f)] public float pistolScopeReticleScale = 0.25f;
        [Tooltip("New UITK reticle art for the scope AND the binoculars (replaces the old sprite frame).")]
        public bool useNewScopeArt = true;
        [Tooltip("Retire the old uGUI optics canvas: the UITK overlay always drives optics.")]
        public bool retireUguiOptics = true;
        public Color reticleColor = new Color(0.05f, 0.05f, 0.05f, 0.95f);
        public Color ringColor = new Color(0f, 0f, 0f, 1f);
        [Range(0.5f, 6f)] public float reticleLineWidth = 1.6f;
        [Range(0f, 40f)] public float reticleCenterGap = 10f;
        [Range(0, 10)] public int milDotsPerArm = 4;
        public string rifleScopeLabel = "SCOPE";
        public string pistolScopeLabel = "SCOPE";
        public string scopeHint = "[RMB] Release  |  Standard / Plasma ammo only";

        private static DM_ScopeProfile s_live;
        public static DM_ScopeProfile Live
        {
            get
            {
                if (s_live == null)
                    s_live = Resources.Load<DM_ScopeProfile>(ResourcePath);
                if (s_live == null)
                    s_live = CreateInstance<DM_ScopeProfile>();
                return s_live;
            }
        }
    }
}

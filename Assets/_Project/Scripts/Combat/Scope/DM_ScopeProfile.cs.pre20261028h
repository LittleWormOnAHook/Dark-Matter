using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-13 (h): player scope / sniper mode settings (Studio-editable, Resources/Combat/DM_ScopeProfile).
    /// Scope mode is toggled from the hold-R Mode Switch menu; while it is on, ADS input (Hold or Toggle) drives
    /// scoped gameplay and optional UITK overlay + FOV zoom (<see cref="scopeAdsEnablesZoomOverlay"/>).
    /// Profile fields: base zoom FOV, fire interval, range, damage multipliers, sway pixels, accuracy bonus, overlay art.
    /// Rifles skill_scope_* assets (via SkillModifierType.Scope*) modify zoom, sway, cadence, range, accuracy, headshot 
    /// never bypass PlayerSkillAllocator. Overlay color/size/mask fields are cosmetic only.
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

        [Header("Zoom / ADS input (Scope mode armed in hold-R menu)")]
        [Tooltip("When Scope mode is armed (hold-R menu): ON = RMB/LT click toggles ADS zoom, overlay, and scoped fire rules until the next click. OFF = hold RMB/LT to ADS (legacy). When Scope mode is off, RMB hold aim is unchanged.")]
        public bool scopeAdsRightMouseToggle = true;
        [Tooltip("When false, scoped cadence/range/damage still apply while ADS is active, but FOV zoom and the UITK scope overlay stay off.")]
        public bool scopeAdsEnablesZoomOverlay = true;
        [Range(3f, 60f)] public float rifleScopedFov = 14f;
        [Range(3f, 60f)] public float pistolScopedFov = 26f;
        [Tooltip("Seconds to reach full zoom (Quick Scope skill shortens it).")]
        [Range(0.02f, 2f)] public float scopeInSeconds = 0.3f;
        [Range(0.02f, 2f)] public float scopeOutSeconds = 0.15f;

        [Header("Fire interval (seconds per scoped single shot)")]
        [Tooltip("Minimum seconds between scoped shots (burst is disabled while ADS). Skills and min interval apply after ammo ±10%.")]
        [Range(0.2f, 5f)] public float rifleScopedFireInterval = 2.5f;
        [Range(0.2f, 5f)] public float pistolScopedFireInterval = 2f;
        [Tooltip("Scoped Cadence skill never goes below this.")]
        [Range(0.1f, 5f)] public float minScopedFireInterval = 1f;
        [Tooltip("Extra wait after each scoped shot, added to the fire interval above (0 = interval alone).")]
        [Range(0f, 5f)] public float rifleScopedPostShotWaitSeconds;
        [Range(0f, 5f)] public float pistolScopedPostShotWaitSeconds;

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

        [Header("Overlay (binoculars / legacy procedural)")]
        [Tooltip("Rifle scope: large mil-dot crosshair sprite (Invector sniperCrossHair). Also used for pistol ADS when sprite overlay is on.")]
        public Sprite scopeCrosshairSprite;
        [Tooltip("When true, rifle and pistol ADS use the mil-dot sprite for the outer crosshair plus procedural inner ring/reticle.")]
        public bool scopeOverlayUseSpriteCrosshair = true;

        public const string RifleCrosshairResourcesPath = "UI/Combat/sniperCrossHair";
        private static Sprite s_resolvedRifleCrosshair;

        /// <summary>Mil-dot overlay sprite; asset reference, then Resources fallback.</summary>
        public Sprite ResolveRifleScopeCrosshairSprite()
        {
            if (scopeCrosshairSprite != null)
                return scopeCrosshairSprite;
            if (s_resolvedRifleCrosshair == null)
                s_resolvedRifleCrosshair = Resources.Load<Sprite>(RifleCrosshairResourcesPath);
            return s_resolvedRifleCrosshair;
        }

        [Range(0.15f, 0.55f)] public float rifleScopeMaskRadius = 0.34f;
        [Tooltip("Multiplies rifle circular mask radius (0.8  20% smaller hole).")]
        [Range(0.5f, 1f)] public float rifleScopeMaskScale = 0.8f;
        [Tooltip("Legacy alias  use rifleOuterCrosshairScale. Kept for existing assets.")]
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
        public string scopeHint = "[RMB] Toggle ADS  |  Standard / Plasma ammo only";
        [Tooltip("Gap (px) between the top edge of the outer orange circle and the bottom of the range label (e.g. '11 m'). Negative = overlap.")]
        public float rangeLabelGapPx = 4f;

        [Header("Scope overlay  rifle ADS (reticle / ring / outer crosshair)")]
        [Tooltip("Center dot and inner plus inside the small ring.")]
        public Color rifleReticleColor = new Color(0.05f, 0.05f, 0.05f, 0.95f);
        [Tooltip("Small circular crosshair outer ring.")]
        public Color rifleRingColor = new Color(0f, 0f, 0f, 1f);
        [Tooltip("Large mil-dot crosshair sprite tint.")]
        public Color rifleOuterCrosshairColor = Color.white;
        [Tooltip("Scale of the large mil-dot sprite (base 196 px).")]
        [Range(0.35f, 2.5f)] public float rifleOuterCrosshairScale = 0.8f;
        [Tooltip("HDR-style bloom on UITK overlay (multiplies RGB; 0 = off).")]
        [Range(0f, 8f)] public float rifleOuterCrosshairEmission = 0f;
        public Color rifleOuterCrosshairEmissionColor = Color.white;
        [Tooltip("Outer diameter in screen pixels of the small scoped inner ring.")]
        [Range(8f, 320f)] public float rifleRingOuterDiameterPx = 56f;

        [Header("Scope overlay  pistol ADS")]
        public Color pistolReticleColor = new Color(0.05f, 0.05f, 0.05f, 0.95f);
        public Color pistolRingColor = new Color(0f, 0f, 0f, 1f);
        public Color pistolOuterCrosshairColor = Color.white;
        [Range(0.35f, 2.5f)] public float pistolOuterCrosshairScale = 0.45f;
        [Range(0f, 8f)] public float pistolOuterCrosshairEmission = 0f;
        public Color pistolOuterCrosshairEmissionColor = Color.white;
        [Range(8f, 320f)] public float pistolRingOuterDiameterPx = 40f;

        [Header("Scope overlay  outer viewport mask (deprecated, runtime ignored)")]
        [Tooltip("Deprecated: scope viewport masking removed from runtime overlay.")]
        public bool scopeCenterMaskEnabled = false;
        [Tooltip("Deprecated: no longer applied at runtime.")]
        public Color scopeCenterMaskColor = new Color(0f, 0f, 0f, 1f);
        [Range(0f, 1f)] [Tooltip("Deprecated: no longer applied at runtime.")]
        public float scopeCenterMaskAlpha = 0.32f;
        [Range(0f, 64f)] [Tooltip("Deprecated: no longer applied at runtime.")]
        public float scopeCenterMaskBlurPixels = 12f;
        [Tooltip("Deprecated: passthrough vignette for scope removed from runtime.")]
        [Range(0.005f, 0.08f)] public float scopeInnerRingViewportEdgeSoftness = 0.012f;

        public Color ResolveScopedReticleColor(bool rifle) => rifle ? rifleReticleColor : pistolReticleColor;

        public Color ResolveScopedRingColor(bool rifle) => rifle ? rifleRingColor : pistolRingColor;

        public Color ResolveOuterCrosshairColor(bool rifle) => rifle ? rifleOuterCrosshairColor : pistolOuterCrosshairColor;

        public float ResolveOuterCrosshairScale(bool rifle)
        {
            if (rifle)
                return Mathf.Clamp(rifleOuterCrosshairScale > 0.001f ? rifleOuterCrosshairScale : rifleScopeCrosshairScale, 0.35f, 2.5f);
            return Mathf.Clamp(pistolOuterCrosshairScale, 0.35f, 2.5f);
        }

        public float ResolveRingOuterDiameterPx(bool rifle) =>
            Mathf.Clamp(rifle ? rifleRingOuterDiameterPx : pistolRingOuterDiameterPx, 8f, 320f);

        /// <summary>Scoped inner ring outer diameter (px) after presentation scale; used for range label layout.</summary>
        public float ResolveCenterMaskOuterDiameterPx(bool rifle, float presentationScale)
        {
            float scale = Mathf.Clamp(presentationScale, 0.1f, 1.5f);
            return Mathf.Max(8f, ResolveRingOuterDiameterPx(rifle) * scale);
        }

        public float ResolveScopedPostShotWaitSeconds(bool rifle) =>
            Mathf.Max(0f, rifle ? rifleScopedPostShotWaitSeconds : pistolScopedPostShotWaitSeconds);

        /// <summary>Normalized passthrough hole radius (see UITK optics dim mask) from inner ring outer diameter.</summary>
        public static float ViewportMaskRadiusFromOuterDiameterPx(float outerDiameterPx, int screenHeight)
        {
            float h = Mathf.Max(1, screenHeight);
            return Mathf.Clamp((outerDiameterPx * 0.5f) / h, 0.08f, 0.55f);
        }

        public Color ResolveOuterCrosshairTint(bool rifle)
        {
            Color baseTint = ResolveOuterCrosshairColor(rifle);
            float emission = rifle ? rifleOuterCrosshairEmission : pistolOuterCrosshairEmission;
            Color emissionColor = rifle ? rifleOuterCrosshairEmissionColor : pistolOuterCrosshairEmissionColor;
            return ApplyOverlayEmission(baseTint, emissionColor, emission);
        }

        public static Color ApplyOverlayEmission(Color baseColor, Color emissionColor, float emissionStrength)
        {
            if (emissionStrength <= 0.0001f)
                return baseColor;

            float e = emissionStrength;
            return new Color(
                baseColor.r * (1f + emissionColor.r * e),
                baseColor.g * (1f + emissionColor.g * e),
                baseColor.b * (1f + emissionColor.b * e),
                baseColor.a);
        }

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

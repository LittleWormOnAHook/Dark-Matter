using System;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>Which hit effect a body type plays (prefabs live under "Effect prefabs" on the profile).</summary>
    public enum DMEnemyHitFxKind
    {
        None = 0,
        Blood = 1,
        Coolant = 2,
        SparksSmall = 3,
        SparksLarge = 4
    }

    /// <summary>Per body type response to a bullet hit: splatter + sparks, burn mark look, embers.</summary>
    [Serializable]
    public sealed class DMEnemyBodyHitResponse
    {
        [Tooltip("Main splatter burst. Blood is always skipped for ammo that bans blood (Laser, Combat Plan §16).")]
        public DMEnemyHitFxKind splatter = DMEnemyHitFxKind.None;
        [Range(0.25f, 3f)] public float splatterScale = 1f;
        [Tooltip("Second burst played with the splatter (sparks on mechs).")]
        public DMEnemyHitFxKind sparks = DMEnemyHitFxKind.None;
        [Range(0.25f, 3f)] public float sparkScale = 1f;
        [Tooltip("Heavy hits (see Exit spray damage) also spray the splatter out along the bullet path.")]
        public bool exitSpray = true;
        [Tooltip("Burn mark look. BloodChar = red blood spot with a char ring; GlowBurn = Laser scorch with a cooling glow.")]
        public DMHitMarkStyle burnStyle = DMHitMarkStyle.GlowBurn;
        [Tooltip("Spawn a short ember / glow burst on glowing burn marks.")]
        public bool embers = true;

        public static DMEnemyBodyHitResponse HumanoidDefault() => new DMEnemyBodyHitResponse
        {
            splatter = DMEnemyHitFxKind.Blood,
            sparks = DMEnemyHitFxKind.None,
            burnStyle = DMHitMarkStyle.BloodChar,
            embers = false
        };

        public static DMEnemyBodyHitResponse AndroidDefault() => new DMEnemyBodyHitResponse
        {
            splatter = DMEnemyHitFxKind.Coolant,
            sparks = DMEnemyHitFxKind.SparksSmall,
            burnStyle = DMHitMarkStyle.GlowBurn,
            embers = true
        };

        public static DMEnemyBodyHitResponse RobotDefault() => new DMEnemyBodyHitResponse
        {
            splatter = DMEnemyHitFxKind.None,
            sparks = DMEnemyHitFxKind.SparksLarge,
            burnStyle = DMHitMarkStyle.GlowBurn,
            embers = true
        };
    }

    /// <summary>
    /// Live-tuned settings for enemy hitboxes and bullet hit marks (Enemy Spacing &amp; Hit Marks plan, Part B).
    /// Lives at Resources/Combat/DM_EnemyHitMarkProfile and is read live, so Genesis Studio (Combat → Hit Marks)
    /// edits apply in Play. Built by Tools/Dark Matter Genesis/Combat/Hit Marks.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Combat/Enemy Hit Mark Profile", fileName = "DM_EnemyHitMarkProfile")]
    public sealed class DM_EnemyHitMarkProfile : ScriptableObject
    {
        public const string ResourcesPath = "Combat/DM_EnemyHitMarkProfile";
        public const int MaxMarksPerEnemy = 5;

        private static DM_EnemyHitMarkProfile live;
        private static bool loadAttempted;

        /// <summary>Bumped whenever the profile is edited (inspector / Studio), so live enemies re-apply mark settings.</summary>
        public static int Revision { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLiveCache()
        {
            live = null;
            loadAttempted = false;
        }

        /// <summary>Profile asset from Resources, or null (code falls back to the defaults below).</summary>
        public static DM_EnemyHitMarkProfile Live
        {
            get
            {
                if (live == null && !loadAttempted)
                {
                    loadAttempted = true;
                    live = Resources.Load<DM_EnemyHitMarkProfile>(ResourcesPath);
                }

                return live;
            }
        }

        private static DM_EnemyHitMarkProfile runtimeDefaults;

        /// <summary>Live profile, or an in-memory default (hitboxes work; no decal/FX assets) when the asset is missing.</summary>
        public static DM_EnemyHitMarkProfile LiveOrDefault
        {
            get
            {
                DM_EnemyHitMarkProfile profile = Live;
                if (profile != null)
                    return profile;

                if (runtimeDefaults == null)
                {
                    runtimeDefaults = CreateInstance<DM_EnemyHitMarkProfile>();
                    runtimeDefaults.name = "DM_EnemyHitMarkProfile (runtime defaults)";
                    runtimeDefaults.hideFlags = HideFlags.DontSave;
                }

                return runtimeDefaults;
            }
        }

        // ---------------------------------------------------------------- Marks
        [Header("Mark limit & lifetime")]
        [Tooltip("Burn marks kept per enemy (oldest rotates out). Lowering it in Play trims live enemies.")]
        [Range(1, MaxMarksPerEnemy)] public int maxBurnMarksPerEnemy = 5;
        [Tooltip("If an enemy's health goes back up (regen), marks stamped at or below the new health fade out over this time.")]
        [Range(0.1f, 5f)] public float regenFadeSeconds = 1.5f;

        [Header("Glow & cool-down")]
        [Tooltip("Seconds for a glowing burn to cool from hot to the dim residual.")]
        [Range(0.2f, 6f)] public float glowCoolSeconds = 2f;
        [Tooltip("Glow left after cooling (0 = fully out). Applies to cooled marks live.")]
        [Range(0f, 1f)] public float glowResidual = 0.15f;
        [Tooltip("Glow layer size relative to the burn mark.")]
        [Range(0.2f, 1.2f)] public float glowSizeScale = 0.7f;
        [Tooltip("Lifetime of the ember / glow burst on glowing burns.")]
        [Range(0.2f, 4f)] public float emberGlowSeconds = 1.5f;
        [Tooltip("Laser always leaves a glowing burn (cauterised, §16), even on Humanoid bodies.")]
        public bool laserAlwaysGlowBurn = true;

        [Header("Decal size & projection")]
        [Tooltip("Random ranged ammo burn mark size range in metres (min, max) before rangedAmmoMarkSizeMultiplier.")]
        public Vector2 burnSize = new Vector2(0.10f, 0.10f);
        [Range(0.02f, 1f)] public float burnProjectionDepth = 0.6f;
        [Tooltip("Multiplies bullet / glow burn decal footprint and projection depth (default 2×).")]
        [Range(1f, 4f)] public float rangedAmmoMarkSizeMultiplier = 2f;
        [Tooltip("Push stamped marks along the outward surface normal (metres) so the decal volume straddles the struck face.")]
        [Range(0f, 0.05f)] public float markSurfaceNormalOffset = 0.004f;
        [Tooltip("When > 0, also shifts the stamp by this fraction of projection depth along the normal (centres the HDRP box on the face).")]
        [Range(0f, 1f)] public float markDepthCenterBias = 0.5f;
        [Tooltip("Start / end angle fade in degrees so marks do not smear along grazing surfaces. Applies to live marks.")]
        public Vector2 burnAngleFade = new Vector2(50f, 80f);
        [Tooltip("Marks stop drawing beyond this camera distance. Applies to live marks.")]
        public float decalDrawDistance = 40f;

        // ---------------------------------------------------------------- Effects
        [Header("Effect budget & distance")]
        [Tooltip("Max splatter / spark bursts alive at once across all enemies.")]
        [Range(4, 64)] public int fxBurstBudget = 24;
        [Tooltip("Min seconds between splatter bursts on the same enemy (automatic fire).")]
        [Range(0f, 0.5f)] public float perEnemyMinInterval = 0.06f;
        [Tooltip("No splatter / sparks / embers beyond this camera distance (marks still stamp).")]
        public float fxMaxDistance = 40f;

        [Header("Splatter shape")]
        [Tooltip("Burst scale at zero damage (x) and at Full-scale damage (y).")]
        public Vector2 splatterDamageScale = new Vector2(0.8f, 1.3f);
        [Tooltip("Damage that reaches the full splatter scale.")]
        [Min(1f)] public float splatterFullScaleDamage = 30f;
        [Tooltip("How much the entry spray leans toward the surface normal (0) vs back at the shooter (1).")]
        [Range(0f, 1f)] public float bloodEntryNormalBlend = 0.35f;
        [Tooltip("Hits at or above this damage also play an exit spray (per body type toggle below).")]
        public float exitSprayDamageThreshold = 25f;
        [Range(0f, 1f)] public float exitSprayStrength = 0.3f;

        [Header("Body type responses")]
        [Tooltip("Humanoid (D8): red blood splatter + blood spot with a char ring.")]
        public DMEnemyBodyHitResponse humanoidResponse = DMEnemyBodyHitResponse.HumanoidDefault();
        [Tooltip("Android: green coolant + small sparks + glowing burn.")]
        public DMEnemyBodyHitResponse androidResponse = DMEnemyBodyHitResponse.AndroidDefault();
        [Tooltip("Robot: large sparks + glowing burn.")]
        public DMEnemyBodyHitResponse robotResponse = DMEnemyBodyHitResponse.RobotDefault();

        [Header("Effect prefabs")]
        [Tooltip("Blood kind.")] public GameObject bloodHitPrefab;
        [Tooltip("Coolant kind.")] public GameObject coolantHitPrefab;
        [Tooltip("SparksSmall kind.")] public GameObject sparkHitSmallPrefab;
        [Tooltip("SparksLarge kind (falls back to small when empty).")] public GameObject sparkHitLargePrefab;
        [Tooltip("Ember / glow burst on glowing burns.")] public GameObject burnEmberPrefab;

        [Header("Melee slash marks")]
        [Tooltip("Elongated decal along the swing (length × width in metres) before meleeSlashMarkSizeMultiplier.")]
        public Vector2 slashSize = new Vector2(0.6f, 0.1f);
        [Range(0.02f, 1f)] public float slashProjectionDepth = 0.6f;
        [Tooltip("Multiplies melee slash decal length, width, and projection depth (default 3×).")]
        [Range(1f, 4f)] public float meleeSlashMarkSizeMultiplier = 3f;

        [Header("Burn mark materials")]
        [Tooltip("BloodChar look: bright red blood spot with a circular char ring. 2x2 atlas.")]
        public Material bulletBurnMaterial;
        [Tooltip("GlowBurn look: the Laser burn (scorch + char). 2x2 atlas.")]
        public Material glowBurnMaterial;
        [Tooltip("Emissive-only glow layer stacked on the glow burn; faded with the projector fade factor.")]
        public Material glowBurnHotMaterial;
        [Tooltip("HDRP emissive flash for BloodChar bullet holes (debug / tuning). Empty = reuse glow hot material.")]
        public Material bulletBurnHotMaterial;

        // ---------------------------------------------------------------- Hitboxes
        [Header("Hitboxes (template scale applies on next rig build / spawn)")]
        [Tooltip("Scales player-like per-bone DMHitbox templates (~1m humanoid). Live for newly built rigs.")]
        [Range(0.5f, 1.5f)] public float hitboxTemplateScale = 1f;
        [Tooltip("Legacy ragdoll-copy inflate (unused — templates replace ragdoll copy). Kept for asset compatibility.")]
        [Range(1f, 1.3f)] public float hitboxInflate = 1.05f;
        [Tooltip("Projectile sweep radius against hitboxes (world sweep keeps the projectile's own radius). Live.")]
        [Range(0.005f, 0.08f)] public float hitboxSweepRadius = 0.02f;
        [Tooltip("Add filler hitboxes (neck) where the ragdoll leaves gaps.")]
        public bool addFillerHitboxes = true;

        [Header("Debug hit mark visibility")]
        [Tooltip("Boost HDRP decal emission on bullet + melee marks so hits are easy to spot in Play.")]
        public bool debugBrightHitMarkEmission = true;
        [Range(1f, 24f)] public float debugHitMarkEmissionIntensity = 10f;
        [Tooltip("Melee slash marks get the hot emissive glow layer when debug emission is on.")]
        public bool debugMeleeSlashEmissiveGlow = true;
        [Tooltip("Ranged bullet marks get a hot emissive flash (round impact), including humanoid BloodChar holes.")]
        public bool debugBrightRangedMarks = true;
        [Tooltip("Scales the hot emissive decal when debug bright marks are active.")]
        [Range(0.5f, 2.5f)] public float debugHotGlowSizeScale = 1.25f;
        [Tooltip("Brief spark burst on ranged BloodChar impacts (humanoids have no default sparks).")]
        public bool debugRangedImpactSpark = true;

        [Header("Zone damage multipliers (Combat Plan D4)")]
        [Tooltip("Head shots (+75% default).")]
        public float headMultiplier = 1.75f;
        [Tooltip("Chest / torso (+25% default).")]
        public float torsoMultiplier = 1.25f;
        public float armMultiplier = 1f;
        public float legMultiplier = 1f;

        [Header("Rendering layer (advanced)")]
        [Tooltip("Rendering layer the enemy marks project onto (enemy renderers get this bit when first marked).")]
        public string enemyDecalRenderingLayer = "DM Enemy Marks";
        [Tooltip("Fallback rendering layer index when the named layer is missing.")]
        [Range(0, 15)] public int enemyDecalRenderingLayerFallbackIndex = 15;

        public float ResolveZoneMultiplier(CombatBodyPart zone)
        {
            switch (zone)
            {
                case CombatBodyPart.Head: return headMultiplier;
                case CombatBodyPart.Torso: return torsoMultiplier;
                case CombatBodyPart.Arm: return armMultiplier;
                case CombatBodyPart.Leg: return legMultiplier;
                default: return 1f;
            }
        }

        /// <summary>Response for a body type (never null).</summary>
        public DMEnemyBodyHitResponse GetResponse(DMEnemyBodyType bodyType)
        {
            switch (bodyType)
            {
                case DMEnemyBodyType.Android:
                    return androidResponse ?? (androidResponse = DMEnemyBodyHitResponse.AndroidDefault());
                case DMEnemyBodyType.Robot:
                    return robotResponse ?? (robotResponse = DMEnemyBodyHitResponse.RobotDefault());
                default:
                    return humanoidResponse ?? (humanoidResponse = DMEnemyBodyHitResponse.HumanoidDefault());
            }
        }

        /// <summary>Prefab for an effect kind (SparksLarge falls back to SparksSmall).</summary>
        public GameObject ResolveFxPrefab(DMEnemyHitFxKind kind)
        {
            switch (kind)
            {
                case DMEnemyHitFxKind.Blood: return bloodHitPrefab;
                case DMEnemyHitFxKind.Coolant: return coolantHitPrefab;
                case DMEnemyHitFxKind.SparksSmall: return sparkHitSmallPrefab;
                case DMEnemyHitFxKind.SparksLarge: return sparkHitLargePrefab != null ? sparkHitLargePrefab : sparkHitSmallPrefab;
                default: return null;
            }
        }

        /// <summary>Splatter burst scale for a hit's damage.</summary>
        public float ResolveSplatterScale(float damage)
        {
            float t = Mathf.Clamp01(damage / Mathf.Max(1f, splatterFullScaleDamage));
            return Mathf.Max(0.05f, Mathf.Lerp(splatterDamageScale.x, splatterDamageScale.y, t));
        }

        private void OnValidate()
        {
            maxBurnMarksPerEnemy = Mathf.Clamp(maxBurnMarksPerEnemy, 1, MaxMarksPerEnemy);
            burnSize.x = Mathf.Max(0.005f, burnSize.x);
            burnSize.y = Mathf.Max(burnSize.x, burnSize.y);
            markSurfaceNormalOffset = Mathf.Clamp(markSurfaceNormalOffset, 0f, 0.05f);
            markDepthCenterBias = Mathf.Clamp01(markDepthCenterBias);
            burnAngleFade.x = Mathf.Clamp(burnAngleFade.x, 0f, 180f);
            burnAngleFade.y = Mathf.Clamp(burnAngleFade.y, burnAngleFade.x, 180f);
            decalDrawDistance = Mathf.Max(1f, decalDrawDistance);
            fxMaxDistance = Mathf.Max(0f, fxMaxDistance);
            exitSprayDamageThreshold = Mathf.Max(0f, exitSprayDamageThreshold);
            hitboxTemplateScale = Mathf.Clamp(hitboxTemplateScale, 0.5f, 1.5f);
            debugHitMarkEmissionIntensity = Mathf.Max(1f, debugHitMarkEmissionIntensity);
            rangedAmmoMarkSizeMultiplier = Mathf.Clamp(rangedAmmoMarkSizeMultiplier, 1f, 4f);
            meleeSlashMarkSizeMultiplier = Mathf.Clamp(meleeSlashMarkSizeMultiplier, 1f, 4f);
            slashProjectionDepth = Mathf.Clamp(slashProjectionDepth, 0.02f, 1f);
            burnProjectionDepth = Mathf.Clamp(burnProjectionDepth, 0.02f, 1f);
            Revision++;
        }
    }
}

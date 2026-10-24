using Project.Data;
using Project.SurfaceCarve;
using UnityEngine;
using UnityEngine.Serialization;

namespace Project.Combat
{
    /// <summary>How this ammo travels to the target (Combat Plan phase D shared resolver).</summary>
    public enum DMAmmoDeliveryMode
    {
        Auto = 0,
        Projectile = 1,
        Hitscan = 2,
        Beam = 3
    }

    /// <summary>
    /// One ammo asset: inventory ItemData plus combat FX / hit marks.
    /// Pickups, registry, and weapons reference this asset directly.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Combat/Ammo FX Profile", fileName = "DMAmmoFxProfile_")]
    public class DMAmmoFxProfile : ItemData
    {
        [Header("Delivery (Combat Plan §16 / phase D)")]
        [Tooltip("Auto = infer from isHitscanBeam and projectile prefab. All nine ammo types resolve through DMCombatRangedResolver.")]
        public DMAmmoDeliveryMode delivery = DMAmmoDeliveryMode.Auto;

        [Header("Muzzle flash / vendor FX (2026-10-21c)")]
        [Tooltip("Use the muzzle prefab's FORGE3D F3DDespawn delay as its lifetime when the component is present (highest priority).")]
        public bool useF3DDespawn = true;
        [Tooltip("Muzzle flash lifetime (s) when no F3DDespawn delay applies. 0 = global default (2 s, or until particles finish). Vendor ammo: 0.05.")]
        [Min(0f)] public float muzzleLifetime = 0f;
        [Tooltip("Hitscan beam: always draw this ammo's Beam Vfx Prefab instead of pulsing the weapon's own laser stack (e.g. Lightning Gun).")]
        public bool beamUsesAmmoPrefab = false;

        [Header("Lightning beam (2026-10-21c; used when Beam Uses Ammo Prefab is on)")]
        [Tooltip("Hold (s) after the bolt head reaches the contact point, before the tail retracts. 0 = retract immediately.")]
        [Range(0f, 6f)] public float beamDurationSeconds = 1.5f;
        [Tooltip("2026-10-21d: bolt head travel speed (m/s) from the barrel to the contact point (chain links too).")]
        [Range(5f, 1000f)] public float boltSpeed = 120f;
        [Tooltip("2026-10-21d: max visible bolt length (m) while travelling; the tail follows the head.")]
        [Range(0.5f, 200f)] public float boltMaxLength = 12f;
        [Tooltip("2026-10-21d: time (s) for the tail to retract from the shooter end into the target.")]
        [Range(0.01f, 2f)] public float boltTailRetractTime = 0.12f;
        [Tooltip("2026-10-21d: bolt line width (m).")]
        [Range(0.01f, 2f)] public float boltWidth = 0.12f;
        [Tooltip("On = damage every tick while the beam is active. Off = damage once on fire.")]
        public bool beamDamagePerTick = true;
        [Tooltip("Damage ticks per second while active (per-tick mode).")]
        [Range(1f, 30f)] public float beamDamageTicksPerSecond = 5f;
        [Tooltip("Each tick deals shot damage x this (per-tick mode). The fire frame always deals full shot damage.")]
        [Range(0f, 2f)] public float beamTickDamageScale = 0.2f;

        [Header("Chain lightning (2026-10-21c)")]
        [Tooltip("Chain lightning on/off for this ammo.")]
        public bool chainEnabled = true;
        [Tooltip("Chain Lightning skill unlocked (temporary flag until the skill tree drives it).")]
        public bool chainSkillUnlocked = true;
        [Tooltip("Extra enemies the bolt jumps to after the first hit (1-5 normal; up to 20 for testing).")]
        [Range(0, 50)] public int chainMaxTargets = 8;
        [Tooltip("Max jump distance (m) from the current enemy to the next.")]
        [Range(0.5f, 100f)] public float chainRange = 8f;
        [Tooltip("Damage multiplier per jump (1 = no falloff, 0.85 = -15% per jump).")]
        [Range(0f, 2f)] public float chainDamageFalloff = 0.85f;
        [Tooltip("Jumps need a clear line between enemies.")]
        public bool chainRequiresLineOfSight = true;
        [Tooltip("2026-10-21e: log each chain link (spawn, endpoints, damage, why a jump was skipped) to the Console.")]
        public bool chainDebugLog = false;
        [Tooltip("2026-10-24i: delay (s) before each jump leaves the current enemy.")]
        [Range(0f, 2f)] public float chainJumpDelay = 0.05f;
        [Tooltip("2026-10-24i: chain arc width x the main bolt width.")]
        [Range(0.05f, 10f)] public float chainWidthScale = 1f;
        [Tooltip("2026-10-24i: chain arc tint (line vertex colour; white = prefab colours).")]
        public Color chainColor = Color.white;
        [Tooltip("2026-10-24i: chain arc travel speed x boltSpeed.")]
        [Range(0.05f, 10f)] public float chainSpeedScale = 1f;
        [Tooltip("2026-10-24l: chain arcs start/end at each target's center mass (humanoid chest bone, else body collider / renderer bounds center) and track it while held. Off = legacy (humanoid chest, else pivot + 1.2 m).")]
        public bool chainUseCenterMass = true;
        public enum ChainMode { FanOutFromFirst = 0, SequentialNearest = 1 }
        [Tooltip("2026-10-24l: FanOutFromFirst = arcs from the first target to EVERY live target within chainRange (nearest first, capped by chainMaxTargets; target k gets falloff^k). SequentialNearest = hop to the nearest, then from there (legacy).")]
        public ChainMode chainMode = ChainMode.FanOutFromFirst;
        [Tooltip("2026-10-24l: vertical offset (m) added to the chain center-mass point.")]
        [Range(-1f, 1f)] public float chainCenterMassHeightOffset = 0f;

        // 2026-10-24n: vendor (FORGE3D) impact explosion audio on every hit.
        public enum VendorImpactAudioMode { Off = 0, Auto = 1, Vulcan = 2, SoloGun = 3, Sniper = 4, ShotGun = 5, Seeker = 6, RailGun = 7, PlasmaGun = 8, LaserImpulse = 13 }
        [Header("Vendor Impact Audio (24n)")]
        [Tooltip("Auto = detect from the tracer/projectile prefab's FORGE3D projectile type. Off = no vendor explosion audio.")]
        public VendorImpactAudioMode vendorImpactAudio = VendorImpactAudioMode.Auto;
        [Tooltip("Anti-spam: min seconds between impact sounds for this ammo (0 = every hit).")]
        [Range(0f, 0.5f)] public float impactAudioMinInterval = 0.05f;
        [Tooltip("Anti-spam: max vendor impact sounds playing at once (all ammo).")]
        [Range(1, 32)] public int impactAudioMaxVoices = 6;
        [Range(0f, 2f)] public float impactAudioVolume = 1f;

        [ContextMenu("Chain Lightning: Recommended")]
        public void ChainPresetRecommended() { chainEnabled = true; chainSkillUnlocked = true; chainMode = ChainMode.FanOutFromFirst; chainUseCenterMass = true; chainCenterMassHeightOffset = 0f; chainMaxTargets = 8; chainRange = 8f; chainDamageFalloff = 0.85f; chainRequiresLineOfSight = true; chainJumpDelay = 0.05f; chainWidthScale = 1f; chainSpeedScale = 1f; chainColor = Color.white; MarkDirty(); }
        [ContextMenu("Chain Lightning: Extreme")]
        // 2026-10-24n: vendor impact audio presets.
        public void ImpactAudioPresetRecommended() { vendorImpactAudio = VendorImpactAudioMode.Auto; impactAudioMinInterval = 0.05f; impactAudioMaxVoices = 6; impactAudioVolume = 1f; }
        public void ImpactAudioPresetExtreme() { vendorImpactAudio = VendorImpactAudioMode.Auto; impactAudioMinInterval = 0f; impactAudioMaxVoices = 24; impactAudioVolume = 1.4f; }

        public void ChainPresetExtreme() { chainEnabled = true; chainSkillUnlocked = true; chainMode = ChainMode.FanOutFromFirst; chainUseCenterMass = true; chainCenterMassHeightOffset = 0f; chainMaxTargets = 30; chainRange = 25f; chainDamageFalloff = 1f; chainRequiresLineOfSight = false; chainJumpDelay = 0.02f; chainWidthScale = 2.5f; chainSpeedScale = 2f; chainColor = new Color(0.6f, 0.85f, 1f, 1f); MarkDirty(); }
        private void MarkDirty()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        [Tooltip("2026-10-21i: Stream - fire the bolt as a short burst of segments along the same snapshot path (1 = single bolt). Only the lead segment hits.")]
        [Range(1, 4)] public int streamSegments = 3;
        [Tooltip("2026-10-21i: seconds between stream segments.")]
        [Range(0.01f, 0.2f)] public float streamSegmentGap = 0.045f;
        [Tooltip("2026-10-21e: show the magenta splash-radius bubble (DMAoeTriggerBubble) for this ammo's splash. Off for Lightning Gun.")]
        public bool showSplashBubble = true;

        [Header("Hit VFX")]
        [Tooltip("Stamp the laser burn mark (pulse / continuous laser).")]
        public bool spawnLaserBurn;

        [Header("Enemy hit marks (Combat Plan §16)")]
        [Tooltip("Red blood on Humanoid enemies. Auto = §16 (Laser never bleeds). Coolant / sparks on Android and Robot always play.")]
        public DMEnemyFxRule enemyBlood = DMEnemyFxRule.Auto;
        [Tooltip("Burn mark decal on enemies. Auto = §16 (Ion never burns).")]
        public DMEnemyFxRule enemyBurnMark = DMEnemyFxRule.Auto;

        [Header("Hit Marks (this ammo)")]
        [FormerlySerializedAs("useSurfaceMarks")]
        [Tooltip("Author holes + bursts on this ammo. Rows below override the shared catalog.")]
        public bool useHitMarks;
        [Tooltip("Default hole for this ammo when the tag has no row.")]
        public GameObject[] defaultDecals;
        [Tooltip("Default burst for this ammo when the tag has no row.")]
        public GameObject[] defaultHitEffects;
        [Tooltip("Per-tag marks for this ammo only. Empty tag list still uses the defaults above.")]
        public DMHitMarkSurface[] surfaces;
        [Tooltip("If this ammo has no row/default, use the shared catalog.")]
        public bool fallBackToCatalog = true;
        public DMAmmoFxCatalog catalog;

        [Header("Surface Damage (carve)")]
        [Tooltip("How this ammo carves rocks / carvable surfaces. Auto = built-in preset for the ammo type. Per-tag rows above can override it.")]
        public DMAmmoSurfaceDamage surfaceDamage = new DMAmmoSurfaceDamage();

        [Header("Camera shake (trauma)")]
        [Tooltip("Trauma when the player fires this ammo. 0 = off. Aim kick stays on Recoil Vertical/Horizontal — this is the CameraShake hub only.")]
        [Range(0f, 2f)] public float fireTrauma;
        [Tooltip("Hold fire trauma this long, then decay. 0 = one-shot punch only.")]
        [Range(0f, 0.4f)] public float fireTraumaDurationSeconds;
        [Tooltip("Trauma when the player's shot resolves a hit (enemy or world). 0 = off.")]
        [Range(0f, 2f)] public float impactTrauma;
        [Range(0f, 0.4f)] public float impactTraumaDurationSeconds;
        [Tooltip("When > 0, impact shake uses CameraShake proximity falloff at the hit point (meters). 0 = global trauma at the listener.")]
        [Range(0f, 80f)] public float impactTraumaRadius;
        [Tooltip("When > 0, skip impact trauma if the hit is closer than this distance to the active camera (meters). Suppresses muzzle-adjacent jitter.")]
        [Range(0f, 8f)] public float impactTraumaMinDistance;

        private void OnEnable()
        {
            itemType = ItemType.Ammo;
            fxProfile = this;
        }

        public bool TryResolveHitMark(string tag, out GameObject decal, out GameObject hitEffect)
        {
            decal = null;
            hitEffect = null;

            if (useHitMarks && surfaces != null)
            {
                for (int i = 0; i < surfaces.Length; i++)
                {
                    DMHitMarkSurface row = surfaces[i];
                    if (row == null || !string.Equals(row.tag, tag, System.StringComparison.Ordinal))
                        continue;
                    if (!row.HasAny())
                        break;
                    decal = row.PickDecal();
                    hitEffect = row.PickHitEffect();
                    return true;
                }
            }

            if (useHitMarks)
            {
                decal = Pick(defaultDecals);
                hitEffect = Pick(defaultHitEffects);
                if (decal != null || hitEffect != null)
                    return true;
            }

            if (fallBackToCatalog && catalog != null)
            {
                DMHitMarkSet set = catalog.Resolve(tag);
                if (set != null)
                {
                    decal = set.PickDecal();
                    hitEffect = set.PickHitEffect();
                    return decal != null || hitEffect != null;
                }
            }

            return false;
        }

        /// <summary>Carve settings for a hit on this tag. Tag row override wins, else this ammo's surface damage.</summary>
        public bool TryResolveSurfaceDamage(string tag, out DMSurfaceDamageSettings settings)
        {
            settings = null;
            if (surfaces != null)
            {
                for (int i = 0; i < surfaces.Length; i++)
                {
                    DMHitMarkSurface row = surfaces[i];
                    if (row == null || row.surfaceDamage == null || !string.Equals(row.tag, tag, System.StringComparison.Ordinal))
                        continue;
                    if (row.surfaceDamage.mode == DMSurfaceDamageRowMode.Off)
                        return false;
                    if (row.surfaceDamage.mode == DMSurfaceDamageRowMode.Custom && row.surfaceDamage.custom != null)
                    {
                        settings = row.surfaceDamage.custom;
                        return true;
                    }

                    break;
                }
            }

            if (surfaceDamage == null)
                surfaceDamage = new DMAmmoSurfaceDamage();
            return surfaceDamage.TryResolve(ammoType, itemName, out settings);
        }

        /// <summary>Resolved delivery for the shared ranged hit path (all nine <see cref="AmmoType"/> values).</summary>
        public DMAmmoDeliveryMode ResolveDelivery()
        {
            if (delivery != DMAmmoDeliveryMode.Auto)
                return delivery;

            if (isHitscanBeam)
                return beamVfxPrefab != null ? DMAmmoDeliveryMode.Beam : DMAmmoDeliveryMode.Hitscan;

            if (projectilePrefab != null)
                return DMAmmoDeliveryMode.Projectile;

            return DMAmmoDeliveryMode.Projectile;
        }

        public void CopyFrom(DMAmmoFxProfile source)
        {
            if (source == null || source == this)
                return;

            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), this);
            fxProfile = this;
            itemType = ItemType.Ammo;
        }

        public void SeedSurfacesFromCatalog()
        {
            if (catalog == null)
                return;
            catalog.WriteDefaultRows(ref surfaces);
        }

        private static GameObject Pick(GameObject[] list)
        {
            if (list == null || list.Length == 0)
                return null;
            if (list.Length == 1)
                return list[0];
            return list[Random.Range(0, list.Length)];
        }
    }
}

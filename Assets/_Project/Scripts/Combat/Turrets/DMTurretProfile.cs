using Project.Data;
using Project.UI;
using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>
    /// Live tunables for FORGE3D-wrapped turrets. Combat Studio → Turrets and Genesis Studio → Combat → Turrets.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Combat/Turret Profile", fileName = "DM_TurretProfile")]
    public sealed class DMTurretProfile : ScriptableObject
    {
        public const string ResourcesPath = "Combat/DM_TurretProfile";
        public const string AssetPath = "Assets/_Project/Resources/Combat/DM_TurretProfile.asset";

        private static DMTurretProfile live;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLiveCache() => live = null;

        public static DMTurretProfile Live
        {
            get
            {
                if (live == null)
                    live = Resources.Load<DMTurretProfile>(ResourcesPath);
                return live;
            }
        }

        [Header("Placement defaults")]
        [Tooltip("Faction applied to player-placed turrets (scene FORGE3D turrets bootstrap as Enemy).")]
        public DMTurretFaction defaultPlacedFaction = DMTurretFaction.Player;

        [Tooltip("Control mode applied to player-placed turrets.")]
        public DMTurretControlMode defaultControlMode = DMTurretControlMode.Auto;

        [Header("Ranges (meters)")]
        [Tooltip("Acquire / keep a target. No target or beyond this → Scan.")]
        public float scanRange = 45f;

        [Tooltip("Aim at a target without firing. Typically between fire and scan.")]
        public float trackRange = 32f;

        [Tooltip("Auto-fire when a legal target is this close and the barrel can see it.")]
        public float fireRange = 22f;

        [Header("Auto idle")]
        [Tooltip("Auto mode: seconds with no engageable target (within track range + LOS) before long rest. Barrel stays still; silent FindTarget continues but slows (~1.2s) to reduce CPU.")]
        public float autoRestAfterNoTargetSeconds = 15f;

        [Tooltip("Keep target lock this long after the last confirmed LOS ray (reduces flicker).")]
        public float targetLosGraceSeconds = 0.65f;

        [Tooltip("Drop target after this many consecutive LOS failures (still capped by grace time).")]
        [Range(1, 12)] public int targetLosFailuresBeforeDrop = 4;

        [Header("Scan motion (non-auto only)")]
        [Tooltip("Yaw sweep speed while scanning (degrees / second). Used when control mode is not Auto.")]
        public float scanHeadingSpeed = 18f;

        [Tooltip("Seconds between picking a new scan heading / elevation (non-auto visual sweep).")]
        public float scanRetargetSeconds = 2.4f;

        [Tooltip("Random barrel elevation while scanning, in degrees (mapped onto F3D elevation limits).")]
        public Vector2 scanElevationDegrees = new Vector2(-8f, 12f);

        [Header("Tracking (FORGE3D F3DTurret)")]
        public float headingTrackingSpeed = 40f;
        public float elevationTrackingSpeed = 30f;
        public Vector2 headingLimit = new Vector2(-180f, 180f);
        public Vector2 elevationLimit = new Vector2(-15f, 45f);

        [Header("Ammo mode")]
        [Tooltip("Default for placed turrets and instances without overrideAmmoMode.")]
        public DMTurretAmmoMode defaultAmmoMode = DMTurretAmmoMode.Forge3DVfx;

        [Tooltip("Weapon ItemData for GameCombatAmmo mode (spread, fire rate, projectile prefab).")]
        public ItemData gameWeaponItem;

        [Tooltip("Optional ammo row; empty uses weapon default or catalog lookup from ammoType.")]
        public ItemData gameAmmoItem;

        [Tooltip("Seconds between game-ammo shots when weapon fire rate is unset.")]
        public float gameFireInterval = 0.15f;

        [Tooltip("Cone spread (degrees) for game-ammo shots.")]
        [Range(0f, 12f)] public float gameSpreadDegrees = 1.5f;

        [Header("Damage")]
        public AmmoType ammoType = AmmoType.Gunpowder;
        public float damage = 18f;
        [Tooltip("0 = no splash. Explosive ammo should be > 0.")]
        public float splashRadius = 0f;
        [Tooltip("Beam / lightning damage interval so continuous FX do not melt HP every frame.")]
        public float beamDamageInterval = 0.2f;

        [Header("Durability")]
        public float maxHealth = 100f;

        [Header("Enemy response")]
        [Tooltip("When a player turret threatens an enemy, chance to attack the turret instead of fleeing.")]
        [Range(0f, 1f)] public float enemyDestroyChanceMin = 0.10f;
        [Range(0f, 1f)] public float enemyDestroyChanceMax = 0.40f;
        [Tooltip("Extra meters past fire range before flee counts as safe.")]
        public float enemyFleeMargin = 6f;
        public float enemyTurretDecisionSeconds = 8f;

        [Header("Player seat camera (prefab slot on *SOCKET_SWIVEL)")]
        [Tooltip("Local position on the swivel — follows turret yaw/pitch while seated.")]
        public Vector3 playerCameraLocalPosition = new Vector3(0f, 2.22f, -5.38f);
        public Vector3 playerCameraLocalEuler = Vector3.zero;
        public float playerCameraFov = 60f;
        [Tooltip("Degrees per mouse unit while seated. Idle mouse holds swivel and barrel.")]
        public float playerAimMouseSensitivity = 0.35f;

        [Tooltip("Normalized screen Y bias below center for the turret reticle and aim ray (0.06 ≈ lower crosshair).")]
        [Range(0f, 0.2f)] public float playerReticleScreenBias = 0.06f;

        [Header("Alarm light (swivel tracking indicator)")]
        [Tooltip("When off, uses palette defaults (Gold scan, Map POI red track, PositiveGreen fire).")]
        public bool useCustomAlarmColors;

        public Color alarmScanColor = DarkMatterGenesisUiPalette.Gold;
        public Color alarmTrackColor = DarkMatterGenesisUiPalette.MapPoiRed;
        public Color alarmFireColor = DarkMatterGenesisUiPalette.PositiveGreen;

        [Tooltip("HDR emission multiplier for the Alarm sphere MaterialPropertyBlock.")]
        [Min(0f)] public float alarmEmissionIntensity = 5f;

        [Header("Debug")]
        public bool debugDraw = false;

        public float FireRange => Positive(fireRange, 22f);
        public float TrackRange => Mathf.Max(FireRange, Positive(trackRange, 32f));
        public float ScanRange => Mathf.Max(TrackRange, Positive(scanRange, 45f));
        public float MaxHealth => Positive(maxHealth, 100f);
        public float Damage => Mathf.Max(0f, damage);
        public float SplashRadius => Mathf.Max(0f, splashRadius);
        public float BeamDamageInterval => Positive(beamDamageInterval, 0.2f);
        public float AutoRestAfterNoTargetSeconds => Positive(autoRestAfterNoTargetSeconds, 15f);
        public float TargetLosGraceSeconds => Positive(targetLosGraceSeconds, 0.65f);
        public int TargetLosFailuresBeforeDrop => Mathf.Clamp(targetLosFailuresBeforeDrop, 1, 12);
        public float ScanHeadingSpeed => Positive(scanHeadingSpeed, 18f);
        public float ScanRetargetSeconds => Positive(scanRetargetSeconds, 2.4f);
        public float HeadingTrackingSpeed => Positive(headingTrackingSpeed, 40f);
        public float ElevationTrackingSpeed => Positive(elevationTrackingSpeed, 30f);
        public float EnemyFleeMargin => Mathf.Max(0f, enemyFleeMargin);
        public float EnemyTurretDecisionSeconds => Positive(enemyTurretDecisionSeconds, 8f);
        public float PlayerCameraFov => Positive(playerCameraFov, 60f);
        public Vector3 PlayerCameraLocalPosition => playerCameraLocalPosition;
        public Vector3 PlayerCameraLocalEuler => playerCameraLocalEuler;
        public float PlayerAimMouseSensitivity => Positive(playerAimMouseSensitivity, 0.35f);
        public float PlayerReticleScreenBias => Mathf.Clamp(playerReticleScreenBias, 0f, 0.2f);
        public DMTurretAmmoMode DefaultAmmoMode => defaultAmmoMode;
        public float GameFireInterval => Positive(gameFireInterval, 0.15f);
        public float GameSpreadDegrees => Mathf.Clamp(gameSpreadDegrees, 0f, 12f);
        public Color AlarmScanColor => useCustomAlarmColors ? alarmScanColor : DarkMatterGenesisUiPalette.Gold;
        public Color AlarmTrackColor => useCustomAlarmColors ? alarmTrackColor : DarkMatterGenesisUiPalette.MapPoiRed;
        public Color AlarmFireColor => useCustomAlarmColors ? alarmFireColor : DarkMatterGenesisUiPalette.PositiveGreen;
        public float AlarmEmissionIntensity => alarmEmissionIntensity > 0.001f ? alarmEmissionIntensity : 5f;

        public float RollEnemyDestroyChance()
        {
            float min = Mathf.Clamp01(enemyDestroyChanceMin);
            float max = Mathf.Clamp01(enemyDestroyChanceMax);
            if (min > max)
            {
                float swap = min;
                min = max;
                max = swap;
            }

            return Random.Range(min, max);
        }

        private static float Positive(float value, float fallback)
        {
            return value > 0.001f ? value : fallback;
        }
    }
}

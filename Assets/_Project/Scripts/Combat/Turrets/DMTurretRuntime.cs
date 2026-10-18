using System.Collections.Generic;
using FORGE3D;
using Project.AI;
using Project.Companions;
using Project.Combat;
using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Survival;
using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>
    /// Dark Matter wrap around a FORGE3D turret. Strips demo WASD/mouse fire, owns faction,
    /// ammo/damage, and fire start/stop through the existing F3DFXController.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-80)]
    public sealed class DMTurretRuntime : MonoBehaviour
    {
        public static bool SuppressDemoInput => true;

        [SerializeField] private DMTurretFaction faction = DMTurretFaction.Enemy;
        [SerializeField] private DMTurretControlMode controlMode = DMTurretControlMode.Auto;
        [SerializeField] private DMTurretAmmoMode ammoMode = DMTurretAmmoMode.Forge3DVfx;
        [SerializeField] private F3DFXType vendorFxType = F3DFXType.Vulcan;
        [SerializeField] private AmmoType ammoType = AmmoType.Gunpowder;
        [SerializeField] private DMTurretBarrelKind barrelKind = DMTurretBarrelKind.Auto;
        [SerializeField] private F3DTurretScriptable barrelDatabase;
        [SerializeField] private string databaseBarrelId;
        [SerializeField] private GameObject databaseBarrelPrefab;
        [SerializeField] private bool useProfileDamage = true;
        [SerializeField] private float damageOverride = 18f;
        [SerializeField] private DMTurretInstanceAuthority instanceAuthority = new DMTurretInstanceAuthority();
        [SerializeField] private F3DTurret forgeTurret;
        [SerializeField] private F3DFXController fxController;
        [SerializeField] private Transform barrel;
        [SerializeField] private Camera playerSeatCamera;
        [SerializeField] private bool placedByPlayer;

        private DMTurretHealth health;
        private bool firing;
        private bool destroyed;
        private float nextGameAmmoShotTime;

        public DMTurretInstanceAuthority InstanceAuthority => instanceAuthority;
        public DMTurretFaction Faction => faction;
        public DMTurretControlMode ControlMode => controlMode;
        public AmmoType EffectiveAmmoType
        {
            get
            {
                if (instanceAuthority != null && instanceAuthority.overrideAmmoType)
                    return instanceAuthority.ammoType;
                return ammoType;
            }
        }

        public AmmoType AmmoType => EffectiveAmmoType;

        public DMTurretAmmoMode EffectiveAmmoMode
        {
            get
            {
                if (instanceAuthority != null && instanceAuthority.overrideAmmoMode)
                    return instanceAuthority.ammoMode;
                return ammoMode;
            }
        }

        public F3DFXType EffectiveVendorFxType
        {
            get
            {
                if (instanceAuthority != null && instanceAuthority.overrideVendorFxType)
                    return instanceAuthority.vendorFxType;
                return vendorFxType;
            }
        }

        public DMTurretBarrelKind EffectiveBarrelKind
        {
            get
            {
                string catalogId = EffectiveDatabaseBarrelId;
                if (!string.IsNullOrEmpty(catalogId))
                {
                    DMTurretBarrelKind fromCatalog = DMTurretBarrelCatalog.KindFromId(catalogId);
                    if (fromCatalog != DMTurretBarrelKind.Auto)
                        return fromCatalog;
                }

                if (instanceAuthority != null && instanceAuthority.overrideBarrelKind)
                    return instanceAuthority.barrelKind;
                return barrelKind;
            }
        }

        public string EffectiveDatabaseBarrelId
        {
            get
            {
                if (instanceAuthority != null && instanceAuthority.overrideDatabaseBarrel)
                    return instanceAuthority.databaseBarrelId ?? DMTurretBarrelCatalog.AutoId;
                return databaseBarrelId ?? DMTurretBarrelCatalog.AutoId;
            }
        }
        public F3DTurret ForgeTurret => forgeTurret;
        public F3DFXController FxController => fxController;
        public Transform Barrel => barrel != null ? barrel : transform;
        public Camera PlayerSeatCamera => playerSeatCamera;
        public bool UsesSwivelSeatCamera =>
            playerSeatCamera != null
            && DMTurretPlayerCameraSlot.IsUnderSwivel(playerSeatCamera.transform, transform);
        public bool IsDestroyed => destroyed || (health != null && health.IsDead);
        public bool IsFiring => firing;
        public bool PlacedByPlayer => placedByPlayer;
        public DMTurretBrainMode BrainMode { get; internal set; }

        /// <summary>Auto-brain lock target (avoids re-scanning every frame / from enemy AI).</summary>
        public Transform CurrentAutoTarget { get; internal set; }

        public float ScanRange => ResolveRange(
            instanceAuthority != null && instanceAuthority.overrideScanRange,
            instanceAuthority != null ? instanceAuthority.scanRange : 0f,
            p => p.ScanRange,
            45f);

        public float TrackRange => ResolveRange(
            instanceAuthority != null && instanceAuthority.overrideTrackRange,
            instanceAuthority != null ? instanceAuthority.trackRange : 0f,
            p => p.TrackRange,
            32f);

        public float FireRange => ResolveRange(
            instanceAuthority != null && instanceAuthority.overrideFireRange,
            instanceAuthority != null ? instanceAuthority.fireRange : 0f,
            p => p.FireRange,
            22f);

        public float Damage
        {
            get
            {
                if (instanceAuthority != null && instanceAuthority.overrideDamage)
                    return Mathf.Max(0f, instanceAuthority.damage);
                if (!useProfileDamage)
                    return Mathf.Max(0f, damageOverride);
                DMTurretProfile profile = DMTurretProfile.Live;
                return profile != null ? profile.Damage : 18f;
            }
        }

        public float SplashRadius
        {
            get
            {
                if (instanceAuthority != null && instanceAuthority.overrideSplashRadius)
                    return Mathf.Max(0f, instanceAuthority.splashRadius);

                DMTurretProfile profile = DMTurretProfile.Live;
                AmmoType type = EffectiveAmmoType;
                if (profile == null)
                    return type == AmmoType.Explosive ? 3.5f : 0f;
                if (type == AmmoType.Explosive && profile.SplashRadius <= 0.05f)
                    return 3.5f;
                return profile.SplashRadius;
            }
        }

        public float BeamDamageInterval
        {
            get
            {
                if (instanceAuthority != null && instanceAuthority.overrideBeamDamageInterval)
                    return Mathf.Max(0.05f, instanceAuthority.beamDamageInterval);
                DMTurretProfile profile = DMTurretProfile.Live;
                return profile != null ? profile.BeamDamageInterval : 0.2f;
            }
        }

        public float MaxHealth
        {
            get
            {
                if (instanceAuthority != null && instanceAuthority.overrideMaxHealth)
                    return Mathf.Max(1f, instanceAuthority.maxHealth);
                DMTurretProfile profile = DMTurretProfile.Live;
                return profile != null ? profile.MaxHealth : 100f;
            }
        }

        public float GameFireInterval
        {
            get
            {
                if (instanceAuthority != null && instanceAuthority.overrideGameFireInterval)
                    return Mathf.Max(0.05f, instanceAuthority.gameFireInterval);
                DMTurretProfile profile = DMTurretProfile.Live;
                return profile != null ? profile.GameFireInterval : 0.15f;
            }
        }

        public float GameSpreadDegrees
        {
            get
            {
                if (instanceAuthority != null && instanceAuthority.overrideGameSpread)
                    return Mathf.Clamp(instanceAuthority.gameSpreadDegrees, 0f, 12f);
                DMTurretProfile profile = DMTurretProfile.Live;
                return profile != null ? profile.GameSpreadDegrees : 1.5f;
            }
        }

        private static float ResolveRange(
            bool instanceOverride,
            float instanceValue,
            System.Func<DMTurretProfile, float> profileValue,
            float fallback)
        {
            if (instanceOverride)
                return Mathf.Max(0.1f, instanceValue);
            DMTurretProfile profile = DMTurretProfile.Live;
            return profile != null ? profileValue(profile) : fallback;
        }

        private void EnsureFxSockets()
        {
            if (fxController == null)
                fxController = GetComponent<F3DFXController>() ?? GetComponentInChildren<F3DFXController>(true);
            if (fxController == null)
                return;
            if (fxController.TurretSocket != null && fxController.TurretSocket.Length > 0)
                return;

            GameObject socketGo = FindNamedChild(transform, "SOCKET_WEAPON", "Weapon");
            Transform socket = socketGo != null ? socketGo.transform : (barrel != null ? barrel : transform);
            fxController.TurretSocket = new[] { socket };
            if (fxController.ShellParticles == null)
                fxController.ShellParticles = System.Array.Empty<ParticleSystem>();
        }

        private void TryCopyFxFromSceneDonor()
        {
            if (fxController == null || fxController.vulcanProjectile != null)
                return;
            F3DFXController donor = F3DFXController.instance;
            if (donor == null || donor == fxController || donor.vulcanProjectile == null)
            {
                F3DFXController[] all = Object.FindObjectsByType<F3DFXController>(FindObjectsInactive.Exclude);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i] != fxController && all[i].vulcanProjectile != null)
                    {
                        donor = all[i];
                        break;
                    }
                }
            }

            if (donor == null || donor == fxController || donor.vulcanProjectile == null)
                return;

            fxController.vulcanProjectile = donor.vulcanProjectile;
            fxController.vulcanMuzzle = donor.vulcanMuzzle;
            fxController.vulcanImpact = donor.vulcanImpact;
            fxController.VulcanFireRate = donor.VulcanFireRate;
            fxController.soloGunProjectile = donor.soloGunProjectile;
            fxController.soloGunMuzzle = donor.soloGunMuzzle;
            fxController.soloGunImpact = donor.soloGunImpact;
            fxController.sniperBeam = donor.sniperBeam;
            fxController.sniperMuzzle = donor.sniperMuzzle;
            fxController.sniperImpact = donor.sniperImpact;
            fxController.shotGunProjectile = donor.shotGunProjectile;
            fxController.shotGunMuzzle = donor.shotGunMuzzle;
            fxController.shotGunImpact = donor.shotGunImpact;
            fxController.seekerProjectile = donor.seekerProjectile;
            fxController.seekerMuzzle = donor.seekerMuzzle;
            fxController.seekerImpact = donor.seekerImpact;
            fxController.railgunBeam = donor.railgunBeam;
            fxController.railgunMuzzle = donor.railgunMuzzle;
            fxController.railgunImpact = donor.railgunImpact;
            fxController.plasmagunProjectile = donor.plasmagunProjectile;
            fxController.plasmagunMuzzle = donor.plasmagunMuzzle;
            fxController.plasmagunImpact = donor.plasmagunImpact;
            fxController.plasmaBeam = donor.plasmaBeam;
            fxController.plasmaBeamHeavy = donor.plasmaBeamHeavy;
            fxController.lightningGunBeam = donor.lightningGunBeam;
            fxController.flameRed = donor.flameRed;
            fxController.laserImpulseProjectile = donor.laserImpulseProjectile;
            fxController.laserImpulseMuzzle = donor.laserImpulseMuzzle;
            fxController.laserImpulseImpact = donor.laserImpulseImpact;
        }

        private void TryPlayDestroyFx()
        {
            Transform impact = fxController != null ? fxController.vulcanImpact : null;
            if (impact == null && F3DFXController.instance != null)
                impact = F3DFXController.instance.vulcanImpact;
            if (impact == null)
                return;
            if (F3DPoolManager.Pools == null || !F3DPoolManager.Pools.ContainsKey("GeneratedPool"))
                return;
            F3DPoolManager.Pools["GeneratedPool"].Spawn(impact, transform.position, Quaternion.identity, null);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapSceneTurrets()
        {
            F3DTurret[] turrets = Object.FindObjectsByType<F3DTurret>(FindObjectsInactive.Exclude);
            for (int i = 0; i < turrets.Length; i++)
            {
                F3DTurret turret = turrets[i];
                if (turret == null)
                    continue;
                if (turret.GetComponent<DMTurretRuntime>() == null)
                    turret.gameObject.AddComponent<DMTurretRuntime>();
            }

            StripAllDemoInput();
        }

        private void Update()
        {
            if (!DMTurretCombatGate.ShouldRunAutoBrain() && firing)
                StopFire();

            if (!firing || IsDestroyed || EffectiveAmmoMode != DMTurretAmmoMode.GameCombatAmmo)
                return;
            if (Time.time < nextGameAmmoShotTime)
                return;
            if (!TryFireGameCombatAmmo())
                return;
            nextGameAmmoShotTime = Time.time + ResolveGameShotCooldown();
        }

        private void Awake()
        {
            BindForge();
            if (GetComponentInChildren<Collider>() == null)
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                box.center = Vector3.up;
                box.size = new Vector3(2.2f, 2.4f, 2.2f);
            }

            health = GetComponent<DMTurretHealth>();
            if (health == null)
                health = gameObject.AddComponent<DMTurretHealth>();
            if (GetComponent<DMTurretBrain>() == null)
                gameObject.AddComponent<DMTurretBrain>();
            if (GetComponent<DMTurretInteract>() == null)
                gameObject.AddComponent<DMTurretInteract>();
            StripDemoInput(gameObject);
            ApplyProfileLimits();
            EnsureFxSockets();
            TryCopyFxFromSceneDonor();
            SyncVendorFxFromControllerIfUnset();
            ApplyVendorFxToController();
            ApplyBarrelChoice();
            EnsurePlayerSeatCamera();
        }

        public Camera ResolvePlayerSeatCamera()
        {
            if (playerSeatCamera == null)
                EnsurePlayerSeatCamera();
            return playerSeatCamera;
        }

        private void EnsurePlayerSeatCamera()
        {
            if (playerSeatCamera == null)
                playerSeatCamera = DMTurretPlayerCameraSlot.Ensure(gameObject);
            if (playerSeatCamera == null)
                return;

            playerSeatCamera.enabled = false;
            AudioListener listener = playerSeatCamera.GetComponent<AudioListener>();
            if (listener != null)
                listener.enabled = false;
        }

        public void ConfigurePlaced(DMTurretFaction placedFaction, DMTurretControlMode placedMode)
        {
            placedByPlayer = true;
            faction = placedFaction;
            controlMode = placedMode;
            DMTurretProfile profile = DMTurretProfile.Live;
            if (profile != null)
            {
                ammoType = profile.ammoType;
                ammoMode = profile.DefaultAmmoMode;
            }
        }

        public void SetFaction(DMTurretFaction value) => faction = value;

        public void SetControlMode(DMTurretControlMode value)
        {
            controlMode = value;
            StopFireAndCleanupForgeFx();

            if (DMTurretPlayerControl.IsControlling(this))
                return;

            DMTurretBrain brain = GetComponent<DMTurretBrain>();
            if (brain != null)
                brain.enabled = true;
        }

        /// <summary>While the player is seated, halt scan/track/auto-fire (brain + forge aim sweep).</summary>
        public void PauseAutoBrainForPlayerSeat()
        {
            StopFireAndCleanupForgeFx();
            DMTurretBrain brain = GetComponent<DMTurretBrain>();
            if (brain != null)
            {
                brain.ClearScanState();
                brain.enabled = false;
            }

            if (forgeTurret != null)
            {
                forgeTurret.SyncTrackingFromPose();
                forgeTurret.HoldCurrentAim();
            }
        }

        /// <summary>After leaving the seat, resume auto brain when control mode is Auto.</summary>
        public void ResumeAutoBrainAfterPlayerSeat()
        {
            if (forgeTurret != null)
            {
                forgeTurret.SyncTrackingFromPose();
                forgeTurret.HoldCurrentAim();
                forgeTurret.enabled = true;
            }

            if (IsDestroyed || controlMode != DMTurretControlMode.Auto)
                return;

            DMTurretBrain brain = GetComponent<DMTurretBrain>();
            if (brain == null)
                return;

            brain.ClearScanState();
            brain.enabled = true;
        }

        public void ApplySeatedLook(float yawDegrees, float pitchDegrees)
        {
            if (forgeTurret == null)
                return;
            forgeTurret.ApplyLookDelta(yawDegrees, pitchDegrees);
        }

        /// <summary>Points the FORGE3D barrel at the seated reticle ray (screen-center + profile bias).</summary>
        public void AimSeatedAtScreenCenter(Camera camera)
        {
            if (forgeTurret == null || camera == null)
                return;
            if (!DMTurretSeatedAim.TryResolveAimPoint(camera, this, out Vector3 aim))
                return;
            forgeTurret.SetNewTarget(aim);
        }

        public void HoldSeatedAim()
        {
            if (forgeTurret != null)
                forgeTurret.HoldCurrentAim();
        }

        /// <summary>Auto brain idle: freeze barrel on current pose (no scan sweep).</summary>
        public void HoldAutoIdle()
        {
            if (forgeTurret != null && forgeTurret.isActiveAndEnabled)
                forgeTurret.HoldCurrentAim();
        }

        public void ApplySeatCameraPitch()
        {
            if (playerSeatCamera == null || !UsesSwivelSeatCamera)
                return;
            if (forgeTurret != null && forgeTurret.Mount != null
                && playerSeatCamera.transform.IsChildOf(forgeTurret.Mount.transform))
                return;

            DMTurretProfile profile = DMTurretProfile.Live;
            Vector3 baseEuler = profile != null ? profile.PlayerCameraLocalEuler : Vector3.zero;
            float pitch = 0f;
            if (forgeTurret != null && forgeTurret.Mount != null)
                pitch = SignedEuler(forgeTurret.Mount.transform.localEulerAngles.x);
            playerSeatCamera.transform.localRotation = Quaternion.Euler(
                baseEuler.x + pitch,
                baseEuler.y,
                baseEuler.z);
        }

        public void SetAmmoType(AmmoType value)
        {
            ammoType = value;
            if (instanceAuthority != null && instanceAuthority.overrideAmmoType)
                instanceAuthority.ammoType = value;
        }

        public void SetAmmoMode(DMTurretAmmoMode value)
        {
            ammoMode = value;
            if (instanceAuthority != null && instanceAuthority.overrideAmmoMode)
                instanceAuthority.ammoMode = value;
        }

        public void SetVendorFxType(F3DFXType value)
        {
            vendorFxType = value;
            if (instanceAuthority != null && instanceAuthority.overrideVendorFxType)
                instanceAuthority.vendorFxType = value;
            ApplyVendorFxToController();
        }

        public void SetBarrelKind(DMTurretBarrelKind value)
        {
            barrelKind = value;
            if (instanceAuthority != null && instanceAuthority.overrideBarrelKind)
                instanceAuthority.barrelKind = value;
            ApplyBarrelChoice();
        }

        public void SetDatabaseBarrel(string catalogId, GameObject prefab = null)
        {
            databaseBarrelId = catalogId ?? DMTurretBarrelCatalog.AutoId;
            databaseBarrelPrefab = prefab;
            if (!string.IsNullOrEmpty(databaseBarrelId))
            {
                DMTurretBarrelKind kind = DMTurretBarrelCatalog.KindFromId(databaseBarrelId);
                if (kind != DMTurretBarrelKind.Auto)
                    barrelKind = kind;
            }

            if (instanceAuthority != null && instanceAuthority.overrideDatabaseBarrel)
                instanceAuthority.databaseBarrelId = databaseBarrelId;
            ApplyBarrelChoice();
        }

        public void ApplyDesignerVisuals()
        {
            ApplyVendorFxToController();
            ApplyBarrelChoice();
        }

        /// <summary>
        /// Enemy turrets hurt Kade and companions only. Player-placed turrets hurt enemies only.
        /// </summary>
        public bool CanDamage(IDamageable damageable)
        {
            if (damageable == null)
                return false;
            if (damageable is DMTurretHealth)
                return false;

            switch (faction)
            {
                case DMTurretFaction.Enemy:
                    return damageable is SurvivalStats || damageable is CompanionHealth;
                case DMTurretFaction.Player:
                    return damageable is EnemyHealth;
                default:
                    return false;
            }
        }

        public Transform FindTarget()
        {
            if (forgeTurret != null && forgeTurret.DebugTarget != null)
                return forgeTurret.DebugTarget;

            float range = ScanRange;
            Transform ignoreRoot = transform;
            Vector3 muzzle = Barrel.position;
            if (faction == DMTurretFaction.Player)
                return FindNearestVisibleEnemy(muzzle, range, ignoreRoot);

            Transform player = PlayerReference.ResolveTransform();
            if (player == null || DMTurretPlayerControl.IsSeatedPlayer(player))
                return null;
            SurvivalStats stats = player.GetComponent<SurvivalStats>();
            if (stats != null && stats.IsDead)
                return null;
            Vector3 delta = player.position - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > range * range)
                return null;
            return HasLineOfSight(player) ? player : null;
        }

        public bool HasLineOfSight(Transform target)
        {
            if (target == null)
                return false;
            return DMTurretLos.HasLineOfSight(Barrel.position, target, transform);
        }

        private static readonly List<EnemyHealth> VisibleEnemyBuffer = new List<EnemyHealth>(64);

        private Transform FindNearestVisibleEnemy(Vector3 muzzle, float range, Transform ignoreRoot)
        {
            EnemyHealthSceneRegistry.CopyTo(VisibleEnemyBuffer);
            Transform best = null;
            float bestSqr = range * range;
            Vector3 origin = transform.position;
            for (int i = 0; i < VisibleEnemyBuffer.Count; i++)
            {
                EnemyHealth enemy = VisibleEnemyBuffer[i];
                if (enemy == null || enemy.IsDead)
                    continue;

                Vector3 delta = enemy.transform.position - origin;
                delta.y = 0f;
                float sqr = delta.sqrMagnitude;
                if (sqr >= bestSqr)
                    continue;
                if (!DMTurretLos.HasLineOfSight(muzzle, enemy.transform, ignoreRoot))
                    continue;

                bestSqr = sqr;
                best = enemy.transform;
            }

            return best;
        }

        public void AimAt(Vector3 worldPoint)
        {
            if (forgeTurret != null && forgeTurret.isActiveAndEnabled)
                forgeTurret.SetNewTarget(worldPoint);
        }

        public float HorizontalDistanceTo(Vector3 worldPoint)
        {
            Vector3 a = transform.position;
            Vector3 b = worldPoint;
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        public bool IsAimedEnough()
        {
            if (forgeTurret == null || forgeTurret.Mount == null)
                return true;
            return forgeTurret.GetAngleToTarget() <= 8f;
        }

        public void BeginFire()
        {
            if (IsDestroyed || firing)
                return;

            if (EffectiveAmmoMode == DMTurretAmmoMode.GameCombatAmmo)
            {
                firing = true;
                nextGameAmmoShotTime = 0f;
                if (forgeTurret != null)
                    forgeTurret.PlayAnimationLoop();
                return;
            }

            if (fxController == null)
                return;

            DMTurretWeaponContext.BeginFire(this);
            fxController.Fire();
            if (forgeTurret != null)
                forgeTurret.PlayAnimationLoop();
            firing = true;
        }

        private void OnDisable()
        {
            StopFireAndCleanupForgeFx();
        }

        /// <summary>Stops active fire. Forge FX/projectile cleanup runs only when actually firing unless forced.</summary>
        public void StopFire()
        {
            StopFireInternal(fullForgeCleanup: false);
        }

        /// <summary>Menu/disable/mode change — always tear down stuck beams and loop audio.</summary>
        public void StopFireAndCleanupForgeFx()
        {
            StopFireInternal(fullForgeCleanup: true);
        }

        /// <summary>Combat gate / idle: stop sustained FX without despawning in-flight projectiles.</summary>
        public void StopStuckSustainedForgeFx()
        {
            if (EffectiveAmmoMode != DMTurretAmmoMode.Forge3DVfx)
                return;

            DMTurretForgeFxCleanup.StopSustained(this, fxController);
        }

        private void StopFireInternal(bool fullForgeCleanup)
        {
            bool wasFiring = firing;
            bool runForgeCleanup = fullForgeCleanup || wasFiring;

            if (runForgeCleanup)
                StopForgeFxOutput(despawnOwnedProjectiles: fullForgeCleanup || wasFiring);

            if (forgeTurret != null && wasFiring)
                forgeTurret.StopAnimation();

            if (wasFiring)
            {
                firing = false;
                nextGameAmmoShotTime = 0f;
                if (DMTurretWeaponContext.Current == this)
                    DMTurretWeaponContext.EndFire(this);
            }
        }

        /// <summary>
        /// FORGE3D beams/flames/lightning parent to sockets and ignore <see cref="F3DFXController.Stop"/>
        /// (audio only). Must despawn pooled children or turrets keep "firing" while scanning.
        /// </summary>
        private void StopForgeFxOutput(bool despawnOwnedProjectiles)
        {
            if (EffectiveAmmoMode != DMTurretAmmoMode.Forge3DVfx)
                return;

            if (despawnOwnedProjectiles)
                DMTurretForgeFxCleanup.StopAll(this, fxController);
            else
                DMTurretForgeFxCleanup.StopSustained(this, fxController);
        }

        private float ResolveGameShotCooldown()
        {
            if (!DMTurretAmmoCatalog.TryResolveGameItems(this, out ItemData weapon, out ItemData ammo))
                return GameFireInterval;

            float fireRate = DMRangedAmmoStats.ResolveFireRate(weapon, ammo);
            if (fireRate > 0.01f)
                return 1f / fireRate;
            return GameFireInterval;
        }

        private bool TryFireGameCombatAmmo()
        {
            if (!DMTurretAmmoCatalog.TryResolveGameItems(this, out ItemData weapon, out ItemData ammo))
                return false;

            Transform muzzle = Barrel;
            if (muzzle == null)
                return false;

            Vector3 direction = muzzle.forward;
            float spread = GameSpreadDegrees;
            float damage = Damage;

            DMTurretWeaponContext.BeginFire(this);
            CombatProjectileSpawner.Spawn(
                gameObject,
                muzzle,
                weapon,
                ammo,
                direction,
                spread,
                damageOverride: damage);
            return true;
        }

        public void NotifyDestroyed()
        {
            destroyed = true;
            StopFireAndCleanupForgeFx();
            if (DMTurretPlayerControl.IsControlling(this))
                DMTurretPlayerControl.Exit();

            DMTurretBrain brain = GetComponent<DMTurretBrain>();
            if (brain != null)
                brain.enabled = false;
            if (forgeTurret != null)
                forgeTurret.enabled = false;
            if (fxController != null)
                fxController.enabled = false;

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = false;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].enabled = false;
            }

            TryPlayDestroyFx();
        }

        private void BindForge()
        {
            if (forgeTurret == null)
                forgeTurret = GetComponent<F3DTurret>();
            if (fxController == null)
                fxController = GetComponent<F3DFXController>();
            if (fxController == null)
                fxController = GetComponentInChildren<F3DFXController>(true);
            if (barrel == null && forgeTurret != null && forgeTurret.Mount != null)
                barrel = forgeTurret.Mount.transform;
            if (barrel == null)
                barrel = transform;

            TryBindSwivelMount();
        }

        private void TryBindSwivelMount()
        {
            if (forgeTurret == null)
                return;
            if (forgeTurret.Swivel == null)
                forgeTurret.Swivel = FindNamedChild(transform, "SWIVEL", "Swivel");
            if (forgeTurret.Mount == null)
                forgeTurret.Mount = FindNamedChild(transform, "MOUNT", "Mount");
            if (!forgeTurret.enabled && forgeTurret.Swivel != null && forgeTurret.Mount != null)
                forgeTurret.enabled = true;
        }

        private static GameObject FindNamedChild(Transform root, string a, string b)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                string n = all[i].name;
                if (n.IndexOf(a, System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf(b, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return all[i].gameObject;
            }

            return null;
        }

        private void ApplyProfileLimits()
        {
            DMTurretProfile profile = DMTurretProfile.Live;
            if (profile == null || forgeTurret == null)
                return;

            forgeTurret.HeadingTrackingSpeed = profile.HeadingTrackingSpeed;
            forgeTurret.ElevationTrackingSpeed = profile.ElevationTrackingSpeed;
            forgeTurret.HeadingLimit = profile.headingLimit;
            forgeTurret.ElevationLimit = profile.elevationLimit;
        }

        public static void StripAllDemoInput()
        {
            StripDemoInput(null);
            F3DPlayerTurretController[] playerTurrets =
                Object.FindObjectsByType<F3DPlayerTurretController>(FindObjectsInactive.Include);
            for (int i = 0; i < playerTurrets.Length; i++)
            {
                if (playerTurrets[i] != null)
                    playerTurrets[i].enabled = false;
            }

            F3DPlayerController[] legacy =
                Object.FindObjectsByType<F3DPlayerController>(FindObjectsInactive.Include);
            for (int i = 0; i < legacy.Length; i++)
            {
                if (legacy[i] != null)
                    legacy[i].enabled = false;
            }

            F3DTurretUI[] ui = Object.FindObjectsByType<F3DTurretUI>(FindObjectsInactive.Include);
            for (int i = 0; i < ui.Length; i++)
            {
                if (ui[i] != null)
                    ui[i].enabled = false;
            }
        }

        private static void StripDemoInput(GameObject root)
        {
            if (root == null)
                return;

            F3DPlayerTurretController player = root.GetComponent<F3DPlayerTurretController>();
            if (player != null)
                player.enabled = false;
            F3DPlayerController legacy = root.GetComponent<F3DPlayerController>();
            if (legacy != null)
                legacy.enabled = false;
            F3DTurretUI ui = root.GetComponent<F3DTurretUI>();
            if (ui != null)
                ui.enabled = false;
        }

        private void SyncVendorFxFromControllerIfUnset()
        {
            if (fxController == null)
                return;
            if (instanceAuthority != null && instanceAuthority.overrideVendorFxType)
                return;
            if (vendorFxType == F3DFXType.Vulcan && fxController.DefaultFXType != F3DFXType.Vulcan)
                vendorFxType = fxController.DefaultFXType;
        }

        public void ApplyVendorFxToController()
        {
            if (fxController == null)
                fxController = GetComponent<F3DFXController>() ?? GetComponentInChildren<F3DFXController>(true);
            if (fxController == null)
                return;
            fxController.DefaultFXType = EffectiveVendorFxType;
        }

        public void ApplyBarrelChoice()
        {
            EnsureBarrelDatabase();
            Transform[] sockets = CollectBarrelSockets();
            string catalogId = EffectiveDatabaseBarrelId;
            GameObject catalogPrefab = ResolveCatalogBarrelPrefab(catalogId);

            if (!string.IsNullOrEmpty(catalogId) && catalogPrefab != null)
                ApplyCatalogBarrel(sockets, catalogId, catalogPrefab);
            else
            {
                ClearSpawnedBarrels();
                RestoreOriginalBarrelChildren();
                ApplySocketKindBarrel(sockets);
            }
        }

        private void EnsureBarrelDatabase()
        {
            if (barrelDatabase == null)
                barrelDatabase = DMTurretBarrelCatalog.LoadDatabase(null);
        }

        private GameObject ResolveCatalogBarrelPrefab(string catalogId)
        {
            if (string.IsNullOrEmpty(catalogId))
                return null;
            if (databaseBarrelPrefab != null && DMTurretBarrelCatalog.IdsEqual(databaseBarrelPrefab.name, catalogId))
                return databaseBarrelPrefab;

            GameObject fromDb = DMTurretBarrelCatalog.FindPrefab(
                DMTurretBarrelCatalog.LoadDatabase(barrelDatabase),
                catalogId);
            if (fromDb != null)
            {
                databaseBarrelPrefab = fromDb;
                return fromDb;
            }

            return databaseBarrelPrefab;
        }

        private void ApplyCatalogBarrel(Transform[] sockets, string catalogId, GameObject prefab)
        {
            ClearSpawnedBarrels();
            if (sockets == null || sockets.Length == 0)
            {
                Transform fallback = barrel != null ? barrel.parent : transform;
                SpawnCatalogBarrel(fallback, catalogId, prefab);
                RetargetBarrelAndFx(new[] { fallback });
                return;
            }

            bool anyMatch = false;
            for (int i = 0; i < sockets.Length; i++)
            {
                if (sockets[i] != null && DMTurretBarrelCatalog.SocketMatches(sockets[i].name, catalogId))
                {
                    anyMatch = true;
                    break;
                }
            }

            List<Transform> live = new List<Transform>(sockets.Length);
            for (int i = 0; i < sockets.Length; i++)
            {
                Transform socket = sockets[i];
                if (socket == null)
                    continue;

                bool on = anyMatch
                    ? DMTurretBarrelCatalog.SocketMatches(socket.name, catalogId)
                    : live.Count == 0;
                if (socket.gameObject.activeSelf != on)
                    socket.gameObject.SetActive(on);
                if (!on)
                    continue;

                HideOriginalBarrelChildren(socket, catalogId);
                if (!TryEnableExistingBarrel(socket, catalogId))
                    SpawnCatalogBarrel(socket, catalogId, prefab);
                live.Add(socket);
            }

            if (live.Count == 0)
            {
                Transform first = FirstSocket(sockets);
                if (first != null)
                {
                    first.gameObject.SetActive(true);
                    HideOriginalBarrelChildren(first, catalogId);
                    SpawnCatalogBarrel(first, catalogId, prefab);
                    live.Add(first);
                }
            }

            RetargetBarrelAndFx(live.ToArray());
        }

        private void ApplySocketKindBarrel(Transform[] sockets)
        {
            if (sockets == null || sockets.Length == 0)
                return;

            DMTurretBarrelKind kind = EffectiveBarrelKind;
            if (kind == DMTurretBarrelKind.Auto)
                kind = InferBarrelKind(sockets);

            List<Transform> live = new List<Transform>(sockets.Length);
            for (int i = 0; i < sockets.Length; i++)
            {
                Transform socket = sockets[i];
                if (socket == null)
                    continue;
                bool on = BarrelNameMatches(socket.name, kind);
                if (socket.gameObject.activeSelf != on)
                    socket.gameObject.SetActive(on);
                if (on)
                    live.Add(socket);
            }

            if (live.Count == 0)
            {
                Transform first = FirstSocket(sockets);
                if (first != null)
                {
                    first.gameObject.SetActive(true);
                    live.Add(first);
                }
            }

            RetargetBarrelAndFx(live.ToArray());
        }

        private void RetargetBarrelAndFx(Transform[] hosts)
        {
            if (hosts == null || hosts.Length == 0)
                return;

            List<Transform> fx = new List<Transform>(hosts.Length);
            Transform first = null;
            for (int i = 0; i < hosts.Length; i++)
            {
                Transform host = hosts[i];
                if (host == null)
                    continue;
                GameObject weapon = FindNamedChild(host, "SOCKET_WEAPON", "Weapon");
                Transform point = weapon != null ? weapon.transform : host;
                if (first == null)
                    first = point;
                fx.Add(point);
            }

            if (first == null)
                return;

            barrel = first;
            if (fxController != null)
                fxController.TurretSocket = fx.ToArray();
        }

        static Transform FirstSocket(Transform[] sockets)
        {
            if (sockets == null)
                return null;
            for (int i = 0; i < sockets.Length; i++)
            {
                if (sockets[i] != null)
                    return sockets[i];
            }

            return null;
        }

        private void SpawnCatalogBarrel(Transform parent, string catalogId, GameObject prefab)
        {
            if (parent == null || prefab == null)
                return;

            GameObject instance = Object.Instantiate(prefab, parent, false);
            instance.name = prefab.name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            DMTurretSpawnedBarrel marker = instance.GetComponent<DMTurretSpawnedBarrel>();
            if (marker == null)
                marker = instance.AddComponent<DMTurretSpawnedBarrel>();
            marker.CatalogId = catalogId;
        }

        static bool TryEnableExistingBarrel(Transform socket, string catalogId)
        {
            for (int i = 0; i < socket.childCount; i++)
            {
                Transform child = socket.GetChild(i);
                if (child == null)
                    continue;
                DMTurretSpawnedBarrel spawned = child.GetComponent<DMTurretSpawnedBarrel>();
                bool match = (spawned != null && DMTurretBarrelCatalog.IdsEqual(spawned.CatalogId, catalogId))
                    || DMTurretBarrelCatalog.NamesMatch(child.name, catalogId);
                if (!match)
                    continue;
                if (!child.gameObject.activeSelf)
                    child.gameObject.SetActive(true);
                return true;
            }

            return false;
        }

        static void HideOriginalBarrelChildren(Transform socket, string catalogId)
        {
            for (int i = 0; i < socket.childCount; i++)
            {
                Transform child = socket.GetChild(i);
                if (child == null)
                    continue;
                if (DMTurretBarrelCatalog.NamesMatch(child.name, catalogId))
                {
                    if (!child.gameObject.activeSelf)
                        child.gameObject.SetActive(true);
                    continue;
                }

                if (child.gameObject.activeSelf)
                    child.gameObject.SetActive(false);
            }
        }

        private void RestoreOriginalBarrelChildren()
        {
            Transform[] sockets = CollectBarrelSockets();
            for (int i = 0; i < sockets.Length; i++)
            {
                Transform socket = sockets[i];
                if (socket == null)
                    continue;
                for (int c = 0; c < socket.childCount; c++)
                {
                    Transform child = socket.GetChild(c);
                    if (child == null || child.GetComponent<DMTurretSpawnedBarrel>() != null)
                        continue;
                    if (!child.gameObject.activeSelf)
                        child.gameObject.SetActive(true);
                }
            }
        }

        private void ClearSpawnedBarrels()
        {
            DMTurretSpawnedBarrel[] spawned = GetComponentsInChildren<DMTurretSpawnedBarrel>(true);
            for (int i = 0; i < spawned.Length; i++)
            {
                if (spawned[i] == null)
                    continue;
                DestroySpawned(spawned[i].gameObject);
            }
        }

        static void DestroySpawned(GameObject go)
        {
            if (go == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(go);
            else
                Object.DestroyImmediate(go);
        }

        private Transform[] CollectBarrelSockets()
        {
            Transform[] all = GetComponentsInChildren<Transform>(true);
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && IsBarrelSocketName(all[i].name))
                    count++;
            }

            if (count == 0)
                return System.Array.Empty<Transform>();

            Transform[] sockets = new Transform[count];
            int write = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && IsBarrelSocketName(all[i].name))
                    sockets[write++] = all[i];
            }

            return sockets;
        }

        private DMTurretBarrelKind InferBarrelKind(Transform[] sockets)
        {
            DMTurretBarrelKind fromAmmo = BarrelKindFromAmmo(EffectiveAmmoType);
            if (fromAmmo != DMTurretBarrelKind.Auto && HasBarrelKind(sockets, fromAmmo))
                return fromAmmo;

            for (int i = 0; i < sockets.Length; i++)
            {
                if (sockets[i] != null && sockets[i].gameObject.activeSelf)
                {
                    DMTurretBarrelKind parsed = ParseBarrelKind(sockets[i].name);
                    if (parsed != DMTurretBarrelKind.Auto)
                        return parsed;
                }
            }

            for (int i = 0; i < sockets.Length; i++)
            {
                if (sockets[i] == null)
                    continue;
                DMTurretBarrelKind parsed = ParseBarrelKind(sockets[i].name);
                if (parsed != DMTurretBarrelKind.Auto)
                    return parsed;
            }

            return DMTurretBarrelKind.Auto;
        }

        private static bool HasBarrelKind(Transform[] sockets, DMTurretBarrelKind kind)
        {
            for (int i = 0; i < sockets.Length; i++)
            {
                if (sockets[i] != null && BarrelNameMatches(sockets[i].name, kind))
                    return true;
            }

            return false;
        }

        private static bool IsBarrelSocketName(string name)
        {
            return !string.IsNullOrEmpty(name)
                && name.IndexOf("SOCKET_BARREL", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool BarrelNameMatches(string name, DMTurretBarrelKind kind)
        {
            if (kind == DMTurretBarrelKind.Auto || string.IsNullOrEmpty(name))
                return false;
            string token = BarrelToken(kind);
            return name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static DMTurretBarrelKind ParseBarrelKind(string name)
        {
            if (string.IsNullOrEmpty(name))
                return DMTurretBarrelKind.Auto;
            if (name.IndexOf("FLAMER", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return DMTurretBarrelKind.Flamer;
            if (name.IndexOf("ELECTRIC", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return DMTurretBarrelKind.Electric;
            if (name.IndexOf("REPEATER", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return DMTurretBarrelKind.Repeater;
            if (name.IndexOf("FLACK", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return DMTurretBarrelKind.Flack;
            if (name.IndexOf("GATLING", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return DMTurretBarrelKind.Gatling;
            if (name.IndexOf("GAUSS", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return DMTurretBarrelKind.Gauss;
            if (name.IndexOf("MORTAR", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return DMTurretBarrelKind.Mortar;
            if (name.IndexOf("LASER", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return DMTurretBarrelKind.Laser;
            if (name.IndexOf("HEAVY", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return DMTurretBarrelKind.HeavyMissile;
            return DMTurretBarrelKind.Auto;
        }

        private static string BarrelToken(DMTurretBarrelKind kind)
        {
            switch (kind)
            {
                case DMTurretBarrelKind.Flamer: return "FLAMER";
                case DMTurretBarrelKind.Electric: return "ELECTRIC";
                case DMTurretBarrelKind.Repeater: return "REPEATER";
                case DMTurretBarrelKind.Flack: return "FLACK";
                case DMTurretBarrelKind.Gatling: return "GATLING";
                case DMTurretBarrelKind.Gauss: return "GAUSS";
                case DMTurretBarrelKind.Mortar: return "MORTAR";
                case DMTurretBarrelKind.Laser: return "LASER";
                case DMTurretBarrelKind.HeavyMissile: return "HEAVY";
                default: return string.Empty;
            }
        }

        private static DMTurretBarrelKind BarrelKindFromAmmo(AmmoType type)
        {
            switch (type)
            {
                case AmmoType.Fire: return DMTurretBarrelKind.Flamer;
                case AmmoType.Electricity: return DMTurretBarrelKind.Electric;
                case AmmoType.Gunpowder: return DMTurretBarrelKind.Repeater;
                case AmmoType.Explosive: return DMTurretBarrelKind.Mortar;
                case AmmoType.Ion:
                case AmmoType.Laser:
                case AmmoType.Plasma: return DMTurretBarrelKind.Laser;
                default: return DMTurretBarrelKind.Auto;
            }
        }

        private static float SignedEuler(float angle)
        {
            if (angle > 180f)
                angle -= 360f;
            return angle;
        }

        private void OnDrawGizmosSelected()
        {
            DMTurretProfile profile = DMTurretProfile.Live;
            if (profile == null || !profile.debugDraw)
                return;
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, FireRange);
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, TrackRange);
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, ScanRange);
        }
    }
}

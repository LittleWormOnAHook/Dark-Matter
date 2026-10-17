using Invector.vShooter;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Progression;
using Project.Player;
using Project.UI;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-13 (h): player scope / sniper mode runtime. Added to the player by WeaponModeSwitchController.
    /// Scope mode on (hold-R menu "Scope") + ranged weapon drawn + ADS active = tactical scoped: fire interval, range,
    /// damage multipliers, ammo gate, louder/lower shot audio. ADS is Hold (RMB/LT while aiming) or Toggle (RMB/LT edge)
    /// per DM_ScopeProfile.scopeAdsRightMouseToggle. FOV zoom + UITK overlay require scopeAdsEnablesZoomOverlay (default true).
    /// Scoped cadence base: loaded ammo rifle/pistolScopedFireInterval when &gt; 0, else DM_ScopeProfile; skills via SkillModifierType.Scope*.
    /// Profile overlay colors/size/mask are cosmetic only — sway amplitude, zoom, cadence, accuracy, range,
    /// and headshot damage still come from profile base values modified by allocated scope skills.
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public sealed class DMScopeMode : MonoBehaviour
    {
        private const string PrefRifle = "DM.Scope.Rifle";
        private const string PrefPistol = "DM.Scope.Pistol";

        /// <summary>Upgrade hook: when the profile is not unlocked-by-default, code can set this.</summary>
        public static bool ScopeUnlockedHook;
        /// <summary>Attachment hook: extra scoped accuracy percent (future attachments).</summary>
        public static float AttachmentAccuracyBonusPercent;

        public static DMScopeMode Instance { get; private set; }
        public static bool IsScoped => Instance != null && Instance._scoped;
        public static bool IsScopedRifle => IsScoped && Instance._scopedRifle;
        public static float ZoomBlend => Instance != null ? Instance._blend : 0f;
        /// <summary>True when ADS zoom / scope overlay is active (profile toggle + blend).</summary>
        public static bool IsScopeViewActive => ZoomBlend > 0.001f;
        public static Vector2 ReticleSway => Instance != null ? Instance._sway : Vector2.zero;
        public static string BlockedAmmoMessage { get; private set; }
        public static float BlockedAmmoUntil { get; private set; }

        private EquipmentController _equipment;
        private WeaponAmmoState _ammoState;
        private vShooterManager _shooter;
        private Invector.vCharacterController.vShooterMeleeInput _input;
        private bool _rifleOn;
        private bool _pistolOn;
        private bool _scoped;
        private bool _scopedRifle;
        private bool _toggleAdsActive;
        private float _blend;
        private float _baseFov = -1f;
        private Camera _cam;
        private vShooterWeapon _patchedWeapon;
        private float _patchedOriginalFreq;
        private Vector2 _sway;
        private DMCameraCollisionOverlay _cameraOverlay;

        public bool RifleScopeEnabled => _rifleOn;
        public bool PistolScopeEnabled => _pistolOn;
        /// <summary>Toggle-mode ADS latched on until the next aim press (Scope mode stays armed).</summary>
        public bool ToggleAdsActive => _toggleAdsActive;

        /// <summary>Scope mode armed + profile Toggle: RMB/LT press edges latch ADS; hold does not.</summary>
        public bool UsesToggleAds(ItemData weapon) =>
            IsUnlocked
            && DM_ScopeProfile.Live.scopeAdsRightMouseToggle
            && IsScopeModeArmed(weapon);

        public void ClearToggleAdsLatch() => _toggleAdsActive = false;

        public static bool IsUnlocked
        {
            get
            {
                DM_ScopeProfile p = DM_ScopeProfile.Live;
                return p.scopeEnabled && (p.scopeUnlockedByDefault || ScopeUnlockedHook);
            }
        }

        private void Awake()
        {
            Instance = this;
            _rifleOn = PlayerPrefs.GetInt(PrefRifle, 0) == 1;
            _pistolOn = PlayerPrefs.GetInt(PrefPistol, 0) == 1;
        }

        private void OnDisable()
        {
            ExitScope(instant: true);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetRifleScopeEnabled(bool on)
        {
            _rifleOn = on;
            PlayerPrefs.SetInt(PrefRifle, on ? 1 : 0);
        }

        public void SetPistolScopeEnabled(bool on)
        {
            _pistolOn = on;
            PlayerPrefs.SetInt(PrefPistol, on ? 1 : 0);
        }

        // 2026-10-13 (i): ItemData.IsTwoHanded is melee-only, so every ranged weapon read as "pistol" and both types shared the pistol toggle.
        public static bool IsRifle(ItemData w) => w != null && w.IsRangedWeapon && w.weaponGrip == WeaponGrip.TwoHanded && !w.isMiningTool;

        private ItemData ActiveWeapon()
        {
            if (_equipment == null)
                _equipment = GetComponentInParent<EquipmentController>() ?? FindAnyObjectByType<EquipmentController>();
            return _equipment != null ? _equipment.DrawnWeaponItem : null;
        }

        private bool Aiming()
        {
            if (_input == null)
                _input = GetComponentInParent<Invector.vCharacterController.vShooterMeleeInput>() ?? GetComponentInChildren<Invector.vCharacterController.vShooterMeleeInput>();
            return _input != null && (_input.isAimingByInput || _input.IsAiming);
        }

        private bool IsScopeModeArmed(ItemData w)
        {
            if (w == null || !w.IsRangedWeapon || w.isMiningTool)
                return false;
            return IsRifle(w) ? _rifleOn : _pistolOn;
        }

        /// <summary>
        /// Scope mode armed + Toggle ADS: call from Pioneer AimInput on RMB/LT press. Returns true when toggle ADS should drive aim.
        /// </summary>
        public bool ApplyToggleAdsInput(bool aimEdgeDown, ItemData weapon)
        {
            DM_ScopeProfile p = DM_ScopeProfile.Live;
            if (!IsUnlocked || !p.scopeAdsRightMouseToggle || !IsScopeModeArmed(weapon))
            {
                if (!IsScopeModeArmed(weapon))
                    _toggleAdsActive = false;
                return false;
            }

            if (aimEdgeDown)
                _toggleAdsActive = !_toggleAdsActive;
            return _toggleAdsActive;
        }

        private bool WantsTacticalScoped(DM_ScopeProfile p, bool modeOn)
        {
            if (!IsUnlocked || !modeOn)
                return false;
            if (p.scopeAdsRightMouseToggle)
                return _toggleAdsActive;
            return Aiming();
        }

        private void LateUpdate()
        {
            DM_ScopeProfile p = DM_ScopeProfile.Live;
            ItemData w = ActiveWeapon();
            bool ranged = w != null && w.IsRangedWeapon && !w.isMiningTool;
            bool rifle = ranged && IsRifle(w);
            bool modeOn = IsScopeModeArmed(w);
            if (!modeOn)
                _toggleAdsActive = false;

            bool want = WantsTacticalScoped(p, modeOn);

            if (want && !_scoped)
                EnterScope(rifle);
            else if (!want && _scoped)
                ExitScope(instant: false);
            else if (want && _scopedRifle != rifle)
            {
                ExitScope(instant: true);
                EnterScope(rifle);
            }

            bool wantZoomView = _scoped && p.scopeAdsEnablesZoomOverlay;
            float target = wantZoomView ? 1f : 0f;
            float inSec = Mathf.Max(0.02f, p.scopeInSeconds * (1f - Mathf.Clamp(Pct(SkillModifierType.ScopeInSpeedPercent), 0f, p.maxScopeInReduction)));
            float outSec = Mathf.Max(0.02f, p.scopeOutSeconds);
            _blend = Mathf.MoveTowards(_blend, target, Time.unscaledDeltaTime / (wantZoomView ? inSec : outSec));

            ApplyFov(p);
            ApplyFireInterval(p);
            UpdateSway(p);
            ApplyRifleScopePresentation(_scopedRifle && _blend > 0.05f);
        }

        private void ApplyRifleScopePresentation(bool activeRifleScope)
        {
            if (_cameraOverlay == null)
                _cameraOverlay = FindAnyObjectByType<DMCameraCollisionOverlay>(FindObjectsInactive.Include);
            _cameraOverlay?.SetRifleScopePresentationHideBody(false);
        }

        private void EnterScope(bool rifle)
        {
            _scoped = true;
            _scopedRifle = rifle;
            DMUiToolkitOpticsOverlay.EnsureAttached();
            _cam = ResolveGameplayCamera();
            if (_cam != null && _blend <= 0.001f)
                _baseFov = _cam.fieldOfView;
        }

        private void ExitScope(bool instant)
        {
            _scoped = false;
            if (instant)
                _toggleAdsActive = false;
            ApplyRifleScopePresentation(false);
            RestoreFireInterval();
            if (instant)
            {
                _blend = 0f;
                RestoreInvectorStateFov();
                if (_cam != null && _baseFov > 0f)
                    _cam.fieldOfView = _baseFov;
                _baseFov = -1f;
            }
        }

        private void ApplyFov(DM_ScopeProfile p)
        {
            if (_cam == null || _baseFov <= 0f)
                return;
            if (_blend <= 0f && !_scoped)
            {
                RestoreInvectorStateFov();
                _cam.fieldOfView = _baseFov;
                _baseFov = -1f;
                return;
            }
            float zoomFov = _scopedRifle ? p.rifleScopedFov : p.pistolScopedFov;
            float extra = Mathf.Clamp(Pct(SkillModifierType.ScopeZoomPercent), 0f, p.maxExtraZoom);
            zoomFov = Mathf.Max(2f, zoomFov * (1f - extra));
            float t = _blend * _blend * (3f - 2f * _blend);
            float fov = Mathf.Lerp(_baseFov, zoomFov, t);
            _cam.fieldOfView = fov;
            DriveInvectorStateFov(fov);
        }

        // 2026-10-13 (i): the Invector third-person camera writes camera.fieldOfView = currentState.fov in FixedUpdate.
        // Writing only the camera made the two fight (FixedUpdate base FOV vs our LateUpdate zoom) -> jitter. Drive the state too.
        private Invector.vCamera.vThirdPersonCamera _tp;
        private Invector.vThirdPersonCameraState _drivenState, _drivenLerp;
        private float _drivenStateFov, _drivenLerpFov;

        private void DriveInvectorStateFov(float fov)
        {
            if (_tp == null)
                _tp = FindAnyObjectByType<Invector.vCamera.vThirdPersonCamera>();
            if (_tp == null)
                return;
            if (_tp.currentState != _drivenState || _tp.lerpState != _drivenLerp)
            {
                RestoreInvectorStateFov();
                _drivenState = _tp.currentState;
                _drivenLerp = _tp.lerpState;
                if (_drivenState != null) _drivenStateFov = _drivenState.fov;
                if (_drivenLerp != null) _drivenLerpFov = _drivenLerp.fov;
            }
            if (_drivenState != null) _drivenState.fov = fov;
            if (_drivenLerp != null && _drivenLerp != _drivenState) _drivenLerp.fov = fov;
        }

        private void RestoreInvectorStateFov()
        {
            if (_drivenState != null) _drivenState.fov = _drivenStateFov;
            if (_drivenLerp != null && _drivenLerp != _drivenState) _drivenLerp.fov = _drivenLerpFov;
            _drivenState = null;
            _drivenLerp = null;
        }

        private void ApplyFireInterval(DM_ScopeProfile p)
        {
            if (!_scoped)
            {
                RestoreFireInterval();
                return;
            }
            if (_shooter == null)
                _shooter = GetComponentInParent<vShooterManager>() ?? GetComponentInChildren<vShooterManager>();
            vShooterWeapon cw = _shooter != null ? _shooter.CurrentWeapon : null;
            if (cw != _patchedWeapon)
            {
                RestoreFireInterval();
                if (cw != null)
                {
                    _patchedWeapon = cw;
                    _patchedOriginalFreq = cw.shootFrequency;
                }
            }
            if (_patchedWeapon != null)
            {
                ItemData ammo = ResolveLoadedAmmo();
                _patchedWeapon.shootFrequency = Mathf.Max(_patchedOriginalFreq, ScopedFireInterval(_scopedRifle, ammo));
            }
        }

        private ItemData ResolveLoadedAmmo()
        {
            if (_equipment == null)
                _equipment = GetComponentInParent<EquipmentController>() ?? FindAnyObjectByType<EquipmentController>();
            if (_equipment == null)
                return null;
            if (_ammoState == null)
                _ammoState = GetComponentInParent<WeaponAmmoState>() ?? FindAnyObjectByType<WeaponAmmoState>();
            return _ammoState != null
                ? _ammoState.GetLoadedAmmoItem(_equipment.ActiveWeaponHotbarSlot)
                : null;
        }

        private void RestoreFireInterval()
        {
            if (_patchedWeapon != null)
                _patchedWeapon.shootFrequency = _patchedOriginalFreq;
            _patchedWeapon = null;
        }

        private void UpdateSway(DM_ScopeProfile p)
        {
            if (_blend <= 0.001f)
            {
                _sway = Vector2.zero;
                return;
            }
            float reduce = Mathf.Clamp(Pct(SkillModifierType.ScopeSteadyAimPercent), 0f, p.maxSwayReduction);
            float amp = p.reticleSwayPixels * (1f - reduce) * _blend;
            float t = Time.time * p.reticleSwaySpeed;
            _sway = new Vector2(Mathf.PerlinNoise(t, 0.37f) - 0.5f, Mathf.PerlinNoise(0.71f, t) - 0.5f) * 2f * amp;
        }

        private static float Pct(SkillModifierType type) => PlayerSkillAllocator.GetTotalBonusPercent(type) * 0.01f;

        public static float ScopedFireInterval(bool rifle, ItemData loadedAmmo = null)
        {
            DM_ScopeProfile p = DM_ScopeProfile.Live;
            float beforeSkills = DMRangedAmmoStats.ResolveScopedFireIntervalBeforeSkills(loadedAmmo, rifle);
            float interval = Mathf.Max(
                p.minScopedFireInterval,
                beforeSkills * (1f - Mathf.Clamp01(Pct(SkillModifierType.ScopeFireIntervalPercent))));
            return interval + p.ResolveScopedPostShotWaitSeconds(rifle);
        }

        /// <summary>Player shot range: scoped range when scoped, else the supplied normal range.</summary>
        public static float ResolvePlayerRange(float normalRange)
        {
            if (!IsScoped)
                return normalRange;
            return Mathf.Max(normalRange, ResolveScopedRangeCapMeters());
        }

        /// <summary>
        /// Scoped range cap from profile + Long Shot skill (rifle or pistol). UITK range readout shows "??m" beyond this.
        /// </summary>
        public static float ResolveScopedRangeCapMeters()
        {
            if (!IsScoped || Instance == null)
                return 0f;
            DM_ScopeProfile p = DM_ScopeProfile.Live;
            float r = Instance._scopedRifle ? p.rifleScopedRange : p.pistolScopedRange;
            r *= 1f + Mathf.Max(0f, Pct(SkillModifierType.ScopeRangePercent));
            return r;
        }

        /// <summary>
        /// World distance along scoped reticle aim (camera to impact). Used by UITK scope range label.
        /// </summary>
        public static bool TryMeasureScopedAimDistanceMeters(out float distanceMeters)
        {
            distanceMeters = 0f;
            if (!IsScoped || ZoomBlend < 0.05f)
                return false;

            Camera cam = ResolveGameplayCamera();
            if (cam == null)
                return false;

            float cap = ResolveScopedRangeCapMeters();
            float rayMax = Mathf.Max(cap * 4f, 250f);
            Vector3 aimPoint = RangedFireSolver.ResolveReticleAimPoint(cam, rayMax, null, null);
            distanceMeters = Vector3.Distance(cam.transform.position, aimPoint);
            return true;
        }

        /// <summary>UITK scoped range line — live metres or locked readout when beyond skill cap.</summary>
        public static string FormatScopedRangeReadout()
        {
            if (!TryMeasureScopedAimDistanceMeters(out float meters))
                return string.Empty;

            float cap = ResolveScopedRangeCapMeters();
            if (meters > cap)
                return "??m";

            return Mathf.RoundToInt(meters) + " m";
        }

        public static float ScopedAccuracyBonusPercent
        {
            get
            {
                if (!IsScoped)
                    return 0f;
                return DM_ScopeProfile.Live.scopedAccuracyBonusPercent
                       + PlayerSkillAllocator.GetTotalBonusPercent(SkillModifierType.ScopeSteadyAimPercent) * 0.5f
                       + AttachmentAccuracyBonusPercent;
            }
        }

        public static float DamageMultiplier(CombatBodyPart part)
        {
            if (!IsScoped)
                return 1f;
            DM_ScopeProfile p = DM_ScopeProfile.Live;
            if (part == CombatBodyPart.Head)
                return p.scopedHeadMultiplier * (1f + Mathf.Max(0f, Pct(SkillModifierType.ScopeHeadshotDamagePercent)));
            if (part == CombatBodyPart.Torso)
                return p.scopedChestMultiplier;
            return 1f;
        }

        public static bool AllowsAmmo(Object ammo)
        {
            if (!IsScoped)
                return true;
            string[] keys = DM_ScopeProfile.Live.allowedAmmoKeywords;
            if (keys == null || keys.Length == 0)
                return true;
            if (ammo == null)
                return true;
            string n = ammo.name;
            if (ammo is ItemData id && !string.IsNullOrEmpty(id.itemName))
                n += "|" + id.itemName;
            for (int i = 0; i < keys.Length; i++)
                if (!string.IsNullOrEmpty(keys[i]) && n.IndexOf(keys[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            BlockedAmmoMessage = "Scope: Standard / Plasma ammo only";
            BlockedAmmoUntil = Time.unscaledTime + 1.5f;
            return false;
        }

        public static float ShotVolume => IsScoped ? DM_ScopeProfile.Live.scopedShotVolume : 1f;
        public static float ShotPitch => IsScoped ? DM_ScopeProfile.Live.scopedShotPitch : 1f;

        /// <summary>
        /// Gameplay camera viewport for scoped shots (screen center + reticle sway from Steady Aim skill).
        /// Aim direction only â€” spread still uses <see cref="ScopedAccuracyBonusPercent"/> and weapon accuracy.
        /// </summary>
        public static Vector2 GetScopedAimViewport()
        {
            float w = Mathf.Max(1f, Screen.width);
            float h = Mathf.Max(1f, Screen.height);
            Vector2 sway = ReticleSway;
            return new Vector2(0.5f + sway.x / w, 0.5f - sway.y / h);
        }

        public static Camera ResolveGameplayCamera()
        {
            PlayerController pc = PlayerLocator.FindPlayerController();
            if (pc != null && pc.GameplayCamera != null)
                return pc.GameplayCamera;

            if (Instance != null && Instance._cam != null && Instance._cam.enabled)
                return Instance._cam;

            return Camera.main;
        }
    }
}

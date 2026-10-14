using Invector.vShooter;
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
    /// Scope mode on (hold-R menu "Scope") + ranged weapon drawn + aiming = scoped: FOV zoom, UITK scope overlay,
    /// scoped fire interval, scoped range, head/chest damage multipliers, ammo gate, louder/lower shot audio.
    /// All values in DM_ScopeProfile; skills via SkillModifierType.Scope*.
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
        public static Vector2 ReticleSway => Instance != null ? Instance._sway : Vector2.zero;
        public static string BlockedAmmoMessage { get; private set; }
        public static float BlockedAmmoUntil { get; private set; }

        private EquipmentController _equipment;
        private vShooterManager _shooter;
        private Invector.vCharacterController.vShooterMeleeInput _input;
        private bool _rifleOn;
        private bool _pistolOn;
        private bool _scoped;
        private bool _scopedRifle;
        private float _blend;
        private float _baseFov = -1f;
        private Camera _cam;
        private vShooterWeapon _patchedWeapon;
        private float _patchedOriginalFreq;
        private Vector2 _sway;
        private DMCameraCollisionOverlay _cameraOverlay;

        public bool RifleScopeEnabled => _rifleOn;
        public bool PistolScopeEnabled => _pistolOn;

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

        private void LateUpdate()
        {
            DM_ScopeProfile p = DM_ScopeProfile.Live;
            ItemData w = ActiveWeapon();
            bool ranged = w != null && w.IsRangedWeapon && !w.isMiningTool;
            bool rifle = ranged && IsRifle(w);
            bool modeOn = ranged && (rifle ? _rifleOn : _pistolOn);
            bool want = IsUnlocked && modeOn && Aiming();

            if (want && !_scoped)
                EnterScope(rifle);
            else if (!want && _scoped)
                ExitScope(instant: false);
            else if (want && _scopedRifle != rifle)
            {
                ExitScope(instant: true);
                EnterScope(rifle);
            }

            float target = _scoped ? 1f : 0f;
            float inSec = Mathf.Max(0.02f, p.scopeInSeconds * (1f - Mathf.Clamp(Pct(SkillModifierType.ScopeInSpeedPercent), 0f, p.maxScopeInReduction)));
            float outSec = Mathf.Max(0.02f, p.scopeOutSeconds);
            _blend = Mathf.MoveTowards(_blend, target, Time.unscaledDeltaTime / (_scoped ? inSec : outSec));

            ApplyFov(p);
            ApplyFireInterval(p);
            UpdateSway(p);
            ApplyRifleScopePresentation(_scoped && _scopedRifle && _blend > 0.05f);
        }

        private void ApplyRifleScopePresentation(bool hideBody)
        {
            if (_cameraOverlay == null)
                _cameraOverlay = FindAnyObjectByType<DMCameraCollisionOverlay>(FindObjectsInactive.Include);
            _cameraOverlay?.SetRifleScopePresentationHideBody(hideBody);
            if (hideBody)
                DMUiToolkitHud.RefreshMenuChrome();
        }

        private void EnterScope(bool rifle)
        {
            _scoped = true;
            _scopedRifle = rifle;
            DMUiToolkitOpticsOverlay.EnsureAttached();
            _cam = Camera.main;
            if (_cam != null && _blend <= 0.001f)
                _baseFov = _cam.fieldOfView;
        }

        private void ExitScope(bool instant)
        {
            _scoped = false;
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
                _patchedWeapon.shootFrequency = Mathf.Max(_patchedOriginalFreq, ScopedFireInterval(_scopedRifle));
        }

        private void RestoreFireInterval()
        {
            if (_patchedWeapon != null)
                _patchedWeapon.shootFrequency = _patchedOriginalFreq;
            _patchedWeapon = null;
        }

        private void UpdateSway(DM_ScopeProfile p)
        {
            if (!_scoped)
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

        public static float ScopedFireInterval(bool rifle)
        {
            DM_ScopeProfile p = DM_ScopeProfile.Live;
            float baseI = rifle ? p.rifleScopedFireInterval : p.pistolScopedFireInterval;
            return Mathf.Max(p.minScopedFireInterval, baseI * (1f - Mathf.Clamp01(Pct(SkillModifierType.ScopeFireIntervalPercent))));
        }

        /// <summary>Player shot range: scoped range when scoped, else the supplied normal range.</summary>
        public static float ResolvePlayerRange(float normalRange)
        {
            if (!IsScoped)
                return normalRange;
            DM_ScopeProfile p = DM_ScopeProfile.Live;
            float r = Instance._scopedRifle ? p.rifleScopedRange : p.pistolScopedRange;
            r *= 1f + Mathf.Max(0f, Pct(SkillModifierType.ScopeRangePercent));
            return Mathf.Max(normalRange, r);
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
    }
}

using Invector.vShooter;
using Project.Core;
using Project.Data;
using Project.Prototypes.Weapons;
using Project.Inventory;
using Project.Progression;
using Project.Player;
using Project.UI;
using UnityEngine;
using Unity.Profiling;

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
        // Scope investigation: cached samples only; runtime behavior is unchanged.
        private static readonly ProfilerMarker ScopeActivate = new ProfilerMarker("DM.Scope.Activate");
        private static readonly ProfilerMarker ScopeDeactivate = new ProfilerMarker("DM.Scope.Deactivate");
        private static readonly ProfilerMarker ScopeSwitch = new ProfilerMarker("DM.Scope.Switch");
        private static readonly ProfilerMarker ScopeMenu = new ProfilerMarker("DM.Scope.MenuToggle");
        private static readonly ProfilerMarker ScopePrefs = new ProfilerMarker("DM.Scope.Preferences.Save");
        private static readonly ProfilerMarker ScopeDiag = new ProfilerMarker("DM.Scope.Diagnostics");
        private static readonly ProfilerMarker ScopeCamera = new ProfilerMarker("DM.Scope.CameraZoom");
        private static readonly ProfilerMarker ScopeCameraResolve = new ProfilerMarker("DM.Scope.Camera.Resolve");
        private static readonly ProfilerMarker ScopeFire = new ProfilerMarker("DM.Scope.FireInterval");
        private static readonly ProfilerMarker ScopeSway = new ProfilerMarker("DM.Scope.ReticleSway");
        private static readonly ProfilerMarker ScopePresentation = new ProfilerMarker("DM.Scope.Presentation");
        private static readonly ProfilerMarker ScopeWeapon = new ProfilerMarker("DM.Scope.Weapon.Resolve");
        private static readonly ProfilerMarker ScopeClassify = new ProfilerMarker("DM.Scope.Weapon.Classify");
        private static readonly ProfilerMarker ScopeSkills = new ProfilerMarker("DM.Scope.SkillBonus");
        private static readonly ProfilerMarker ScopeRange = new ProfilerMarker("DM.Scope.Range.Measure");
        private static readonly ProfilerMarker ScopeText = new ProfilerMarker("DM.Scope.Range.Text");
        private static readonly ProfilerMarker ScopeProfile = new ProfilerMarker("DM.Scope.Profile.Resolve");

        private const string PrefRifle = "DM.Scope.Rifle";
        private const string PrefPistol = "DM.Scope.Pistol";
        /// <summary>Opt-in scope/reticle diagnostics; leave off for performance captures.</summary>
        public static bool ScopeDiagnosticsEnabled;
        private static int _rangeReadoutState = -1;
        private static int _rangeReadoutMeters;
        private static string _rangeReadoutText = string.Empty;

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
        /// <summary>2026-10-20r: current max reticle sway travel (px, per axis) = amplitude after Steady Aim and zoom blend.</summary>
        public static float ReticleSwayMax => Instance != null ? Instance._swayAmp : 0f;
        public static string BlockedAmmoMessage { get; private set; }
        public static float BlockedAmmoUntil { get; private set; }

        private EquipmentController _equipment;
        private WeaponAmmoState _ammoState;
        private vShooterManager _shooter;
        private Invector.vCharacterController.vShooterMeleeInput _input;
        // 2026-11-04y build: the weapon-menu Scope rows and the live player could hold different
        // DMScopeMode components, so pressing Scope ON armed a copy the player never used and scope
        // stayed dead (RMB gave plain ADS: no scope zoom, no reticle). Share the flags across instances.
        private static bool _rifleOn;
        private static bool _pistolOn;
        private static bool _scopePrefsLoaded;
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
        private float _scopeOffDiagNext; // 2026-11-04y

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetScopePrefsOnce()
        {
            _scopePrefsLoaded = false;
            ScopeDiagnosticsEnabled = false;
            _rangeReadoutState = -1;
            _rangeReadoutText = string.Empty;
        }

        private void Awake()
        {
            Instance = this;
            if (!_scopePrefsLoaded)
            {
                _scopePrefsLoaded = true;
                _rifleOn = PlayerPrefs.GetInt(PrefRifle, 0) == 1;
                _pistolOn = PlayerPrefs.GetInt(PrefPistol, 0) == 1;
            }
        }

        // Optics overlay pre-warm: once per scene, after gameplay has started and the HUD had a frame to bind.
        // Prewarm only builds the hidden overlay + loads the crosshair; scope behavior/visuals are unchanged.
        private static UnityEngine.SceneManagement.Scene s_opticsPrewarmedScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOpticsPrewarmState() => s_opticsPrewarmedScene = default;

        private void Start()
        {
            if (Application.isPlaying)
                StartCoroutine(PrewarmOpticsWhenReady());
        }

        private System.Collections.IEnumerator PrewarmOpticsWhenReady()
        {
            UnityEngine.SceneManagement.Scene sceneHandle = gameObject.scene;
            if (s_opticsPrewarmedScene == sceneHandle)
                yield break;

            float giveUpAt = Time.unscaledTime + 180f;
            while (!GameSession.HasStarted && Time.unscaledTime < giveUpAt)
                yield return null;
            if (!GameSession.HasStarted)
                yield break;

            yield return null; // let HUD / UI Toolkit bootstrap bind first
            for (int attempt = 0; attempt < 10; attempt++)
            {
                if (s_opticsPrewarmedScene == sceneHandle)
                    yield break;
                DMUiToolkitOpticsOverlay.Prewarm();
                if (DMUiToolkitOpticsOverlay.IsAttached)
                {
                    s_opticsPrewarmedScene = sceneHandle;
                    yield break;
                }
                yield return null;
            }
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
            using (ScopeMenu.Auto())
            {
                _rifleOn = on;
                PlayerPrefs.SetInt(PrefRifle, on ? 1 : 0);
                using (ScopePrefs.Auto()) { PlayerPrefs.Save(); } // 2026-11-04y build: menu toggles were not surviving the run
                if (ScopeDiagnosticsEnabled)
                using (ScopeDiag.Auto())
                {
                    Debug.Log("[ScopeDiag] rifle scope -> " + (on ? "ON" : "OFF") + " on " + name + " armedNow=" + IsScopeModeArmed(ActiveWeapon()));
                }
            }
        }

        public void SetPistolScopeEnabled(bool on)
        {
            using (ScopeMenu.Auto())
            {
                _pistolOn = on;
                PlayerPrefs.SetInt(PrefPistol, on ? 1 : 0);
                using (ScopePrefs.Auto()) { PlayerPrefs.Save(); } // 2026-11-04y build: menu toggles were not surviving the run
                if (ScopeDiagnosticsEnabled)
                using (ScopeDiag.Auto())
                {
                    Debug.Log("[ScopeDiag] pistol scope -> " + (on ? "ON" : "OFF") + " on " + name + " armedNow=" + IsScopeModeArmed(ActiveWeapon()));
                }
            }
        }

        // 2026-10-13 (i): ItemData.IsTwoHanded is melee-only, so every ranged weapon read as "pistol" and both types shared the pistol toggle.
        public static bool IsRifle(ItemData w)
        {
            using (ScopeClassify.Auto())
            {
                if (w == null || !w.IsRangedWeapon || w.isMiningTool)
                    return false;
                if (DMWeaponMorphRuntime.TryGetEffectiveWeaponGrip(w, out WeaponGrip morphGrip))
                    return morphGrip == WeaponGrip.TwoHanded;
                return w.weaponGrip == WeaponGrip.TwoHanded;
            }
        }

        private ItemData ActiveWeapon()
        {
            using (ScopeWeapon.Auto())
            {
                if (_equipment == null)
                    _equipment = GetComponentInParent<EquipmentController>() ?? FindAnyObjectByType<EquipmentController>();
                return _equipment != null ? _equipment.DrawnWeaponItem : null;
            }
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
            DM_ScopeProfile p;
            using (ScopeProfile.Auto()) { p = DM_ScopeProfile.Live; }
            ItemData w = ActiveWeapon();
            bool ranged = w != null && w.IsRangedWeapon && !w.isMiningTool;
            bool rifle = ranged && IsRifle(w);
            bool modeOn = IsScopeModeArmed(w);
            if (!modeOn)
                _toggleAdsActive = false;

            // 2026-11-04y build: the "held RMB, ADS zoomed, but no reticle / no scope zoom" case is exactly
            // scope mode not armed for the held weapon. Say so once every 2 s instead of failing silently.
            if (ScopeDiagnosticsEnabled && !modeOn && Time.unscaledTime >= _scopeOffDiagNext)
            {
                bool aiming = Aiming(); // resolves _input lazily (it is null until something asks)
                if (aiming && ranged)
                {
                    _scopeOffDiagNext = Time.unscaledTime + 2f;
                    using (ScopeDiag.Auto())
                    {
                        Debug.Log("[ScopeDiag] aiming " + (w != null ? w.name : "null") + " but scope OFF rifle=" + _rifleOn
                            + " pistol=" + _pistolOn + " isRifle=" + rifle);
                    }
                }
            }

            bool want = WantsTacticalScoped(p, modeOn);

            if (want && !_scoped)
                EnterScope(rifle);
            else if (!want && _scoped)
                ExitScope(instant: false);
            else if (want && _scopedRifle != rifle)
            {
                using (ScopeSwitch.Auto())
                {
                    ExitScope(instant: true);
                    EnterScope(rifle);
                }
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

        // perf-1018b: retry missing scene searches at most once a second (were per LateUpdate).
        private float _nextOverlaySearchTime;
        private float _nextTpSearchTime;

        private void ApplyRifleScopePresentation(bool activeRifleScope)
        {
            using (ScopePresentation.Auto())
            {
                if (_cameraOverlay == null && Time.unscaledTime >= _nextOverlaySearchTime)
                {
                    _nextOverlaySearchTime = Time.unscaledTime + 1f;
                    _cameraOverlay = FindAnyObjectByType<DMCameraCollisionOverlay>(FindObjectsInactive.Include);
                }
                _cameraOverlay?.SetRifleScopePresentationHideBody(false);
            }
        }

        private void EnterScope(bool rifle)
        {
            using (ScopeActivate.Auto())
            {
                if (Project.Prototypes.Weapons.DMWeaponMorphRuntime.IsMorphing) return; // 20261028a no scope mid-morph
                _scoped = true;
                _scopedRifle = rifle;
                DMUiToolkitOpticsOverlay.EnsureAttached();
                _cam = ResolveGameplayCamera();
                if (_cam != null && _blend <= 0.001f)
                    _baseFov = _cam.fieldOfView;
                // Profile/string diagnostics are opt-in; scope mechanics above are unchanged.
                if (ScopeDiagnosticsEnabled)
                using (ScopeDiag.Auto())
                {
                    DM_ScopeProfile dp = DM_ScopeProfile.Live;
                    Debug.Log("[ScopeDiag] enter rifle=" + rifle + " cam=" + (_cam != null ? _cam.name + " enabled=" + _cam.enabled : "null") + " baseFov=" + _baseFov.ToString("F1")
                        + " zoomOverlay=" + dp.scopeAdsEnablesZoomOverlay + " targetFov=" + (rifle ? dp.rifleScopedFov : dp.pistolScopedFov) + " newArt=" + dp.useNewScopeArt);
                }
            }
        }

        private void ExitScope(bool instant)
        {
            using (ScopeDeactivate.Auto())
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
        }

        private void ApplyFov(DM_ScopeProfile p)
        {
            using (ScopeCamera.Auto())
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
        }

        // 2026-10-13 (i): the Invector third-person camera writes camera.fieldOfView = currentState.fov in FixedUpdate.
        // Writing only the camera made the two fight (FixedUpdate base FOV vs our LateUpdate zoom) -> jitter. Drive the state too.
        private Invector.vCamera.vThirdPersonCamera _tp;
        private Invector.vThirdPersonCameraState _drivenState, _drivenLerp;
        private float _drivenStateFov, _drivenLerpFov;

        private void DriveInvectorStateFov(float fov)
        {
            if (_tp == null && Time.unscaledTime >= _nextTpSearchTime)
            {
                _nextTpSearchTime = Time.unscaledTime + 1f;
                _tp = FindAnyObjectByType<Invector.vCamera.vThirdPersonCamera>();
            }
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
            using (ScopeFire.Auto())
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

        private float _swayAmp;

        private void UpdateSway(DM_ScopeProfile p)
        {
            using (ScopeSway.Auto())
            {
                if (_blend <= 0.001f)
                {
                    _swayAmp = 0f;
                    _sway = Vector2.zero;
                    return;
                }
                float reduce = Mathf.Clamp(Pct(SkillModifierType.ScopeSteadyAimPercent), 0f, p.maxSwayReduction);
                float amp = p.reticleSwayPixels * (1f - reduce) * _blend;
                _swayAmp = amp;
                float t = Time.time * p.reticleSwaySpeed;
                _sway = new Vector2(Mathf.PerlinNoise(t, 0.37f) - 0.5f, Mathf.PerlinNoise(0.71f, t) - 0.5f) * 2f * amp;
            }
        }

        private static float Pct(SkillModifierType type)
        {
            using (ScopeSkills.Auto())
            {
                return PlayerSkillAllocator.GetTotalBonusPercent(type) * 0.01f;
            }
        }

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
            using (ScopeRange.Auto())
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
        }

        /// <summary>UITK scoped range line — live metres or locked readout when beyond skill cap.</summary>
        public static string FormatScopedRangeReadout()
        {
            using (ScopeText.Auto())
            {
                // Keep the ray/skill sampling live; cache only the displayed status and rounded metres.
                int state = 0;
                int rounded = 0;
                if (TryMeasureScopedAimDistanceMeters(out float meters))
                {
                    float cap = ResolveScopedRangeCapMeters();
                    state = meters > cap ? 1 : 2;
                    if (state == 2) rounded = Mathf.RoundToInt(meters);
                }
                if (_rangeReadoutState != state || (state == 2 && _rangeReadoutMeters != rounded))
                {
                    _rangeReadoutState = state;
                    _rangeReadoutMeters = rounded;
                    _rangeReadoutText = state == 0 ? string.Empty : state == 1 ? "??m" : rounded + " m";
                }
                return _rangeReadoutText;
            }
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
            using (ScopeCameraResolve.Auto())
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
}

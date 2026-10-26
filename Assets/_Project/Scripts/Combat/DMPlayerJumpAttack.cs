using System.Collections.Generic;
using Invector.vCharacterController;
using Invector.vMelee;
using Project.AI;
using Project.Audio;
using Project.Core;
using Project.Data;
using Project.Features.Jetpack;
using Project.Features.Locomotion;
using Project.Inventory;
using Project.Player;
using Project.Player.Invector;
using Project.Survival;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-25f/g Jump melee attack.
    /// Ground flow (25g): running + Jump AND light attack within jumpAttackComboWindow (either order). The leap starts at the press
    /// (the normal jump is consumed, or taken over if Jump came first). Clip wind-up = rising arc (apex at wind-up end, short hang),
    /// accelerating descent, clip impact frame = touchdown at jumpAttackStopDistance in front of the target. Run momentum carries in.
    /// Jetpack plunge (25g): falling after a boost (not thrusting) + light attack + target in range = plunge from current height.
    /// Late light attack in a normal jump = pre-25f behaviour (nothing). Jetpack double-tap gesture suppressed during the move.
    /// Impact: heavier hit stop + shake + heavy enemy reaction while the strike is active; landing shake, dust, SFX,
    /// body-weight load / wind-up shift / landing compression, short landing recovery. Damage = normal hitbox path
    /// (FullBody Attacks/JumpAttack1H|2H, vMeleeAttackControl damageType "JumpAttack"). Settings: DM_CombatCoreProfile "Jump attack".
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMPlayerJumpAttack : MonoBehaviour
    {
        public const string State1H = "JumpAttack1H";
        public const string State2H = "JumpAttack2H";
        public const string SpeedParam = "DMJumpAttackSpeed";
        public const string DamageTypeTag = "JumpAttack";

        private static readonly List<EnemyHealth> s_buffer = new List<EnemyHealth>(32);

        private PioneerShooterMeleeInput _input;
        private vThirdPersonController _cc;
        private Rigidbody _rb;
        private Animator _anim;
        private EquipmentController _equipment;
        private SurvivalStats _survival;
        private DMMeleeAttackFacing _facing;
        private DMJetpackInputBridge _jetBridge;
        private DMJetpackController _jet;
        private DMPlayerBodyWeight _body;

        private bool _busy, _leaping, _recovering, _plunge, _windupKicked;
        private float _busyUntil, _readyAt, _recoverUntil;
        private float _lastJumpPress = -99f, _lastAttackPress = -99f;
        private int _stateHash, _layer;
        private EnemyHealth _target;
        private Vector3 _from, _to, _v0;
        private float _leapStart, _leapDur, _apexT = 0.5f, _arc, _strikeSpeed = 1f;
        private bool _strikeSpeedSet;
        private float _postSpeed = 1f; // 25n
        private bool _ref1H; // 25j: 1H reference timing / body
        private float _arch, _landHip;
        private float _bendMax, _bendNow, _bendReleaseStart = -99f, _bendAtRelease;
        private bool _savedKinematic, _savedLockMove, _savedLockAnimMove, _savedCheckGround;

        public bool IsBusy => _busy;

        // 25l: rejection reasons (jumpAttackLog) so a silent fall-through to the normal combo / jump is visible.
        private bool Reject(string why)
        {
            DM_CombatCoreProfile lp = DM_CombatCoreProfile.Live;
            if (lp != null && lp.jumpAttackLog)
                Debug.Log("[JumpAttack] rejected: " + why, this);
            return false;
        }

        /// <summary>25l: raw jump press stamp, called before quick-step deferral / jetpack tap so the combo window sees every press.</summary>
        public static void NoteJumpPress(PioneerShooterMeleeInput input)
        {
            DMPlayerJumpAttack ja = input != null ? input.GetComponent<DMPlayerJumpAttack>() : null;
            if (ja != null && !ja._busy)
                ja._lastJumpPress = Time.time;
        }
        public bool IsLeaping => _leaping;

        /// <summary>True while a jump attack swing is live for this attacker (hit stop / shake / reaction hooks).</summary>
        public static bool IsStrikeActiveFor(GameObject source)
        {
            if (source == null)
                return false;
            DMPlayerJumpAttack ja = source.GetComponentInParent<DMPlayerJumpAttack>();
            return ja != null && ja._busy;
        }

        private void Awake()
        {
            _input = GetComponent<PioneerShooterMeleeInput>();
            _cc = GetComponent<vThirdPersonController>() ?? GetComponentInChildren<vThirdPersonController>();
            _rb = _cc != null ? _cc.GetComponent<Rigidbody>() : GetComponent<Rigidbody>();
            _anim = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            _equipment = GetComponent<EquipmentController>();
            _survival = GetComponent<SurvivalStats>();
            _jetBridge = GetComponent<DMJetpackInputBridge>() ?? GetComponentInChildren<DMJetpackInputBridge>();
            _jet = GetComponent<DMJetpackController>() ?? GetComponentInChildren<DMJetpackController>();
        }

        private void OnDisable()
        {
            if (_leaping)
                EndLeap(false);
            EndRecovery();
            _busy = false;
        }

        /// <summary>Called from PioneerShooterMeleeInput.UpdateDrawnMeleeCharge. True = attack input belongs to the jump attack this frame.</summary>
        public static bool TryHandle(PioneerShooterMeleeInput input, bool pressed)
        {
            if (input == null)
                return false;
            DMPlayerJumpAttack ja = input.GetComponent<DMPlayerJumpAttack>();
            if (ja == null)
                return false;
            ja.TickBusy();
            if (ja._busy)
                return true;
            if (!pressed)
                return false;
            ja._lastAttackPress = Time.time;
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null)
                return false;
            if (Time.time - ja._lastJumpPress <= p.jumpAttackComboWindow && ja.TryStart(false))
                return true;
            return ja.JetpackFalling() && ja.TryStart(true);
        }

        /// <summary>Called from PioneerShooterMeleeInput.JumpInput before the jetpack / normal jump. True = jump press consumed.</summary>
        public static bool InterceptJump(PioneerShooterMeleeInput input, bool jumpPressed)
        {
            if (input == null)
                return false;
            DMPlayerJumpAttack ja = input.GetComponent<DMPlayerJumpAttack>();
            if (ja == null)
                return false;
            ja.TickBusy();
            if (ja._busy)
                return jumpPressed;
            if (!jumpPressed)
                return false;
            ja._lastJumpPress = Time.time;
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            return p != null && Time.time - ja._lastAttackPress <= p.jumpAttackComboWindow && ja.TryStart(false);
        }

        private bool JetpackActive() => _jet != null && (_jet.IsBoostingNow || _jet.UsedJetpackThisAir);

        private bool JetpackFalling()
        {
            return _jet != null && _cc != null && !_cc.isGrounded && _jet.UsedJetpackThisAir && !_jet.IsBoostingNow
                && _rb != null && !_rb.isKinematic && _rb.linearVelocity.y < -0.5f;
        }

        private float HorizontalSpeed()
        {
            if (_rb == null || _rb.isKinematic)
                return 0f;
            Vector3 v = _rb.linearVelocity;
            v.y = 0f;
            return v.magnitude;
        }

        private void TickBusy()
        {
            if (!_busy || _leaping)
                return;
            if (Time.time > _busyUntil)
            {
                _busy = false;
                return;
            }
            if (_anim != null && _layer >= 0 && Time.time > _leapStart + 0.2f)
            {
                AnimatorStateInfo cur = _anim.GetCurrentAnimatorStateInfo(_layer);
                bool inState = cur.shortNameHash == _stateHash || (_anim.IsInTransition(_layer) && _anim.GetNextAnimatorStateInfo(_layer).shortNameHash == _stateHash);
                if (!inState || cur.normalizedTime >= 0.9f)
                    _busy = false;
            }
        }

        private bool TryStart(bool plunge)
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || !p.jumpAttackEnabled || _cc == null || _anim == null || _rb == null)
                return false;
            if (Time.time < _readyAt || _cc.isDead || _cc.ragdolled || _cc.customAction || _cc.isRolling)
                return Reject("cooldown/dead/customAction/rolling");
            if (plunge)
            {
                if (!p.jumpAttackAllowFromJetpackFall || !JetpackFalling())
                    return false;
            }
            else
            {
                // 25l: the jetpack tap from the SAME jump press no longer blocks the combo.
                if (_jet != null && _jet.IsBoostingNow && Time.time - _lastJumpPress > p.jumpAttackComboWindow)
                    return Reject("jetpack boosting");
                // Grounded, or still in the take-off of the jump pressed inside the combo window.
                if (!_cc.isGrounded && Time.time - _lastJumpPress > p.jumpAttackComboWindow)
                    return Reject("airborne outside combo window");
                if (!p.jumpAttackAllowFromStandstill && HorizontalSpeed() < p.jumpAttackMinRunSpeed && _cc.inputMagnitude < 0.5f)
                    return false;
            }
            if (_equipment == null || !_equipment.IsWeaponDrawn)
                return Reject("no drawn weapon");
            ItemData drawn = _equipment.DrawnWeaponItem;
            if (drawn == null || drawn.itemType != ItemType.MeleeWeapon)
                return false;
            if (_survival != null && p.jumpAttackStaminaCost > 0f && !_survival.HasStamina(p.jumpAttackStaminaCost))
                return false;

            bool twoHand = drawn.weaponGrip == WeaponGrip.TwoHanded;
            _layer = _anim.GetLayerIndex("FullBody");
            string stateName = twoHand ? State2H : State1H;
            _stateHash = Animator.StringToHash(stateName);
            if (_layer >= 0 && !_anim.HasState(_layer, _stateHash) && _anim.HasState(_layer, Animator.StringToHash("FullBody.Attacks." + stateName)))
                _stateHash = Animator.StringToHash("FullBody.Attacks." + stateName); // 25l: nested sub-state path
            if (_layer < 0 || !_anim.HasState(_layer, _stateHash))
            {
                if (p.jumpAttackLog)
                    Debug.LogWarning("[JumpAttack] FullBody state '" + stateName + "' missing. Run Tools/Dark Matter Genesis/Combat/Build Jump Attack States.", this);
                return false;
            }

            _target = PickTarget(p);
            if (plunge && _target == null)
                return false;

            float windupEnd = twoHand ? p.jumpAttack2HWindupEnd : p.jumpAttack1HWindupEnd;
            float impact = Mathf.Max(0.05f, twoHand ? p.jumpAttack2HImpact : p.jumpAttack1HImpact);
            float hitStart = twoHand ? p.jumpAttack2HHitStart : p.jumpAttack1HHitStart;
            float hitEnd = twoHand ? p.jumpAttack2HHitEnd : p.jumpAttack1HHitEnd;
            float clipLen = twoHand ? 2.5f : 2.46f;
            // 25n: asset had 1H wind-up end 0.95 (> impact 0.5) -> clamped wind-up = whole pre-impact clip, played at 3.7x. Sanity-clamp.
            if (windupEnd >= impact - 0.05f)
            {
                if (DM_CombatCoreProfile.Live.jumpAttackLog)
                    Debug.LogWarning("[JumpAttack] windupEnd " + windupEnd + " >= impact " + impact + "; using " + (impact * 0.55f), this);
                windupEnd = impact * 0.55f;
            }

            _from = _rb.position;
            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            float travel;
            if (_target != null)
            {
                Vector3 to = _target.transform.position - _from;
                to.y = 0f;
                float d = to.magnitude;
                Vector3 dir = d > 0.01f ? to / d : fwd;
                Vector3 land = _target.transform.position - dir * p.jumpAttackStopDistance;
                land.y = GroundY(land, _target.transform.position.y);
                _to = land;
                travel = Mathf.Max(0f, d - p.jumpAttackStopDistance);
            }
            else
            {
                float fwdDist = !twoHand && !plunge && p.jumpAttack1HReferenceMode ? p.jumpAttack1HLeapDistance : p.jumpAttackFallbackForward;
                Vector3 land = _from + fwd * fwdDist;
                land.y = GroundY(land, _from.y);
                _to = land;
                travel = fwdDist;
            }

            float drop = _from.y - _to.y;
            if (plunge)
            {
                if (drop > p.jumpAttackPlungeMaxHeight)
                    return false;
                _leapDur = Mathf.Clamp(Mathf.Max(travel / Mathf.Max(0.5f, p.jumpAttackLeapSpeed), Mathf.Max(0f, drop) / Mathf.Max(1f, p.jumpAttackPlungeDescentSpeed)), p.jumpAttackMinAirTime, 2f);
                _apexT = 0.15f;
                _arc = p.jumpAttackPlungeArcHeight;
            }
            else
            {
                _leapDur = Mathf.Clamp(travel / Mathf.Max(0.5f, p.jumpAttackLeapSpeed), p.jumpAttackMinAirTime, 1.5f);
                _apexT = Mathf.Clamp(windupEnd / impact, 0.15f, 0.85f);
                _arc = p.jumpAttackArcHeight;
            }

            // Run momentum into the arc (Hermite start tangent), capped so it cannot overshoot wildly.
            Vector3 v0 = _rb.isKinematic ? Vector3.zero : _rb.linearVelocity;
            v0.y = 0f;
            float cap = (travel / Mathf.Max(0.05f, _leapDur)) * 1.5f + 0.5f;
            _v0 = Vector3.ClampMagnitude(v0 * p.jumpAttackMomentumCarry, cap);

            // 25h: two-phase clip speed. Ascent plays 0..wind-up end at windSpeed, descent plays wind-up end..impact at
            // windSpeed x downSwingSpeed; the descent stretches/shrinks so the impact frame stays on touchdown.
            float dss = Mathf.Clamp(twoHand ? p.jumpAttack2HDownSwingSpeed : p.jumpAttack1HDownSwingSpeed, 0.3f, 1.5f);
            float wSec = Mathf.Clamp(windupEnd, 0.02f, impact - 0.02f) * clipLen;
            float sSec = Mathf.Max(0.02f, impact * clipLen - wSec);
            float speed = Mathf.Clamp(clipLen * (wSec / clipLen + sSec / clipLen / dss) / _leapDur, 0.5f, 2.5f);
            float ta = wSec / speed;
            float strike = speed * dss;
            float td = sSec / strike;
            if (td > p.jumpAttackMaxDescentTime)
            {
                td = p.jumpAttackMaxDescentTime;
                strike = sSec / td;
            }
            // 25j: 1H reference - wind-up fills the ascent, strike starts AT the apex and lasts swing seconds, impact = touchdown.
            _ref1H = !twoHand && !plunge && p.jumpAttack1HReferenceMode;
            if (_ref1H)
            {
                ta = p.jumpAttack1HAscentSeconds;
                td = p.jumpAttack1HSwingSeconds / dss;
                speed = Mathf.Clamp(wSec / ta, 0.3f, 4f);
                strike = sSec / td;
                _arc = p.jumpAttack1HArcHeight;
            }
            _leapDur = ta + td;
            // 25n: after touchdown the rest of the clip (impact -> exit 0.82) spans the squat hold + rise instead of snapping.
            _postSpeed = _ref1H ? Mathf.Clamp((0.82f - impact) * clipLen / Mathf.Max(0.2f, p.jumpAttack1HSquatHold + p.jumpAttack1HRiseSeconds), 0.25f, 1f) : 1f;
            _apexT = Mathf.Clamp(ta / _leapDur, plunge ? 0.05f : 0.15f, 0.85f);
            _strikeSpeed = Mathf.Clamp(strike, 0.2f, 4f);
            _strikeSpeedSet = false;
            _bendMax = (twoHand ? p.jumpAttack2HSwingBend : (_ref1H ? p.jumpAttack1HStrikeCrunch : p.jumpAttack1HSwingBend)) * p.jumpAttackBodyWeightScale;
            _arch = _ref1H ? p.jumpAttack1HAscentArch * p.jumpAttackBodyWeightScale : 0f;
            _landHip = _ref1H ? p.jumpAttack1HLandHipDrop * p.jumpAttackBodyWeightScale : 0f;
            _bendReleaseStart = -99f;
            ApplyHitWindow(hitStart, hitEnd, p.jumpAttackDamageMultiplier);

            _plunge = plunge;
            _leapStart = Time.time;
            _windupKicked = false;
            BeginLeap();
            Face(p, true);
            if (_jetBridge != null)
                _jetBridge.SuppressGestureFor(_leapDur + p.jumpAttackLandRecovery + 0.4f);
            if (!plunge)
                BodyKick(p, p.jumpAttackTakeoffPitch, p.jumpAttackTakeoffCrouch); // load / crouch on take-off

            if (_survival != null && p.jumpAttackStaminaCost > 0f)
                _survival.TryConsumeStamina(p.jumpAttackStaminaCost);
            _anim.SetLayerWeight(_layer, 1f);
            _anim.SetFloat(SpeedParam, speed);
            _anim.CrossFadeInFixedTime(_stateHash, p.jumpAttackBlendIn, _layer, 0f);
            _busy = true;
            _busyUntil = Time.time + _leapDur + clipLen * (1f - impact) / Mathf.Max(0.25f, _postSpeed) + 0.6f;
            _readyAt = Time.time + p.jumpAttackCooldownSeconds;
            if (p.jumpAttackLog)
                Debug.Log("[JumpAttack] " + stateName + (plunge ? " PLUNGE" : "") + (_target != null ? " to " + _target.name : " no target") + " dur=" + _leapDur.ToString("0.00") + " speed=" + speed.ToString("0.00"), this);
            return true;
        }

        private void ApplyHitWindow(float start, float end, float mult)
        {
            vMeleeAttackControl[] controls = _anim.GetBehaviours<vMeleeAttackControl>();
            for (int i = 0; i < controls.Length; i++)
            {
                vMeleeAttackControl c = controls[i];
                if (c == null || c.damageType != DamageTypeTag)
                    continue;
                c.startDamage = Mathf.Clamp01(start);
                c.endDamage = Mathf.Clamp(end, c.startDamage + 0.02f, 1f);
                c.damageMultiplier = Mathf.Max(1, Mathf.RoundToInt(mult));
            }
        }

        private float GroundY(Vector3 at, float fallback)
        {
            float up = Mathf.Max(3f, _from.y - at.y + 1f);
            if (Physics.Raycast(at + Vector3.up * up, Vector3.down, out RaycastHit hit, up + 12f, _cc.groundLayer, QueryTriggerInteraction.Ignore))
                return hit.point.y;
            return fallback;
        }

        private EnemyHealth PickTarget(DM_CombatCoreProfile p)
        {
            if (_facing == null)
                _facing = GetComponent<DMMeleeAttackFacing>();
            EnemyHealth lockT = _facing != null ? _facing.Target : null;
            if (InRange(lockT, p, out _))
                return lockT;

            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            EnemyHealthSceneRegistry.CopyTo(s_buffer);
            EnemyHealth best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < s_buffer.Count; i++)
            {
                EnemyHealth e = s_buffer[i];
                if (!InRange(e, p, out float d))
                    continue;
                Vector3 to = e.transform.position - transform.position;
                to.y = 0f;
                float ang = Vector3.Angle(fwd, to);
                if (ang > p.jumpAttackConeHalfAngle)
                    continue;
                float score = d + ang * 0.03f;
                if (score < bestScore) { bestScore = score; best = e; }
            }
            s_buffer.Clear();
            return best;
        }

        private bool InRange(EnemyHealth e, DM_CombatCoreProfile p, out float d)
        {
            d = 0f;
            if (e == null || e.IsDead || !e.isActiveAndEnabled)
                return false;
            Vector3 to = e.transform.position - transform.position;
            to.y = 0f;
            d = to.magnitude;
            return d >= p.jumpAttackRangeMin && d <= p.jumpAttackRangeMax;
        }

        private void BeginLeap()
        {
            EndRecovery();
            _leaping = true;
            _savedKinematic = _rb.isKinematic;
            if (!_rb.isKinematic)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
            _rb.isKinematic = true;
            _savedLockMove = _cc.lockMovement;
            _savedLockAnimMove = _cc.lockAnimMovement;
            _savedCheckGround = _cc.disableCheckGround;
            _cc.lockMovement = true;
            _cc.lockAnimMovement = true; // 25n: no clip root motion on top of the scripted arc (was the 3-4 body-length, head-high leap)
            _cc.disableCheckGround = true;
            _cc.input = Vector3.zero;
            _cc.inputMagnitude = 0f;
            _cc.isJumping = false; // 25l: leap owns the body, cancel the normal jump take-off
            DMLandingDirector landing = GetComponent<DMLandingDirector>();
            if (landing != null)
                landing.IgnoreLandsFor(_leapDur + 0.6f);
        }

        private void EndLeap(bool landed)
        {
            _leaping = false;
            if (_rb != null)
            {
                _rb.isKinematic = _savedKinematic;
                if (!_rb.isKinematic)
                {
                    _rb.linearVelocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                }
            }
            if (_cc != null)
            {
                _cc.lockMovement = _savedLockMove;
                _cc.lockAnimMovement = _savedLockAnimMove;
                _cc.disableCheckGround = _savedCheckGround;
            }
            DMLandingDirector landing = GetComponent<DMLandingDirector>();
            if (landing != null)
                landing.OnDashEnded();
            if (_anim != null)
                _anim.SetFloat(SpeedParam, _postSpeed); // 25n: slowed so the clip covers hold + rise

            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (!landed || p == null)
                return;
            _bendReleaseStart = Time.time;
            LandingImpact(p);
            float lockFor = _ref1H ? p.jumpAttack1HSquatHold + p.jumpAttack1HRiseSeconds * p.jumpAttack1HRecoveryLockFraction : p.jumpAttackLandRecovery;
            if (lockFor > 0f && _cc != null)
            {
                _recovering = true;
                _recoverUntil = Time.time + lockFor;
                _cc.lockMovement = true;
                _cc.lockAnimMovement = true; // 25n: no root-motion slide during the squat / rise
            }
        }

        private void EndRecovery()
        {
            if (!_recovering)
                return;
            _recovering = false;
            if (_cc != null)
            {
                _cc.lockMovement = _savedLockMove;
                _cc.lockAnimMovement = _savedLockAnimMove;
            }
        }

        private void Update()
        {
            if (_recovering && Time.time >= _recoverUntil)
                EndRecovery();
            if (_recovering && _cc != null)
            {
                _cc.input = Vector3.zero; // 25l: no leftover motor input -> no post-landing slide
                _cc.inputMagnitude = 0f;
            }
            TickSwingBend();
        }

        /// <summary>25h: spine/chest forward bend ramping wind-up end -> impact (peak at touchdown), then released.</summary>
        private void TickSwingBend()
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            float target = 0f;
            float hipTarget = -1f;
            if (p != null && _ref1H && _leaping)
            {
                // 25j: back arch through the ascent, then arch -> deep forward crunch through the strike (peak at touchdown).
                float t = Mathf.Clamp01((Time.time - _leapStart) / Mathf.Max(0.05f, _leapDur));
                if (t <= _apexT)
                    target = -_arch * Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.01f, _apexT));
                else
                    target = Mathf.Lerp(-_arch, _bendMax, Mathf.Pow((t - _apexT) / Mathf.Max(0.01f, 1f - _apexT), 1.3f));
                _bendAtRelease = target;
                hipTarget = 0f;
            }
            else if (p != null && _ref1H && _bendReleaseStart > 0f)
            {
                // 25j: deep squat + hunch held, then a smooth rise to neutral.
                float e = Time.time - _bendReleaseStart;
                float rise = Mathf.Max(0.05f, p.jumpAttack1HRiseSeconds);
                float f = e < p.jumpAttack1HSquatHold ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (e - p.jumpAttack1HSquatHold) / rise);
                target = _bendAtRelease * f;
                hipTarget = _landHip * f;
                if (e >= p.jumpAttack1HSquatHold + rise)
                {
                    _bendReleaseStart = -99f;
                    target = 0f;
                    hipTarget = 0f;
                }
            }
            else if (p != null && _leaping)
            {
                float t = Mathf.Clamp01((Time.time - _leapStart) / Mathf.Max(0.05f, _leapDur));
                float s = t <= _apexT ? 0f : (t - _apexT) / Mathf.Max(0.01f, 1f - _apexT);
                target = _bendMax * Mathf.Pow(s, 1.4f);
                _bendAtRelease = target;
            }
            else if (p != null && _bendReleaseStart > 0f)
            {
                float r = (Time.time - _bendReleaseStart) / Mathf.Max(0.05f, p.jumpAttackSwingBendRelease);
                target = r >= 1f ? 0f : _bendAtRelease * (1f - Mathf.SmoothStep(0f, 1f, r));
                if (r >= 1f)
                    _bendReleaseStart = -99f;
            }
            if (target == 0f && _bendNow == 0f && (hipTarget <= 0f))
            {
                if (_body != null && _body.JumpAttackHipDrop != 0f && !_leaping && _bendReleaseStart < 0f)
                    _body.JumpAttackHipDrop = 0f;
                return;
            }
            _bendNow = target;
            if (_body == null)
                _body = GetComponent<DMPlayerBodyWeight>() ?? GetComponentInParent<DMPlayerBodyWeight>();
            if (_body != null)
            {
                _body.JumpAttackBendDeg = _bendNow;
                float hip = p != null && _bendMax > 0.01f ? p.jumpAttackSwingHipDrop * p.jumpAttackBodyWeightScale * (_bendNow / _bendMax) : 0f;
                _body.JumpAttackHipDrop = hipTarget >= 0f ? hipTarget : hip;
            }
        }

        private void LandingImpact(DM_CombatCoreProfile p)
        {
            Vector3 at = _rb != null ? _rb.position : transform.position;
            if (_ref1H)
                DMCombatCameraShake.Play(p.heavyHitShakeAmplitude * p.jumpAttack1HLandShakeScale, p.jumpAttack1HLandShakeSeconds); // 25j: very brief, subtle
            else
                DMCombatCameraShake.Play(p.heavyHitShakeAmplitude * p.jumpAttackLandShakeScale, p.heavyHitShakeDurationSeconds * Mathf.Clamp(p.jumpAttackLandShakeScale, 0.3f, 1.5f));
            BodyKick(p, p.jumpAttackLandPitch, p.jumpAttackLandDrop); // hips / spine compression

            GameObject vfx = p.jumpAttackImpactVfx;
            if (vfx == null)
            {
                DMFootstepProfile fp = DMFootstepProfile.Live;
                vfx = fp != null ? fp.dustPrefab : null;
            }
            float vfxScale = _ref1H ? p.jumpAttack1HDustScale : p.jumpAttackImpactVfxScale; // 25j: small dust burst for 1H
            if (vfx != null && vfxScale > 0f)
            {
                GameObject go = PoolManager.Spawn(vfx, at + Vector3.up * 0.05f, Quaternion.identity);
                if (go != null)
                {
                    go.transform.localScale = vfx.transform.localScale * vfxScale;
                    PoolManager.ReleaseDelayed(go, 3f);
                }
            }

            AudioClip[] clips = p.jumpAttackImpactSfx;
            if (clips != null && clips.Length > 0 && p.jumpAttackImpactSfxVolume > 0f)
            {
                AudioClip c = clips[Random.Range(0, clips.Length)];
                if (c != null)
                    GameAudioManager.PlayWorldSfxPitched(c, at, p.jumpAttackImpactSfxVolume, p.jumpAttackImpactSfxPitch * Random.Range(0.95f, 1.05f));
            }
        }

        private void BodyKick(DM_CombatCoreProfile p, float pitchDeg, float hipDrop)
        {
            if (p.jumpAttackBodyWeightScale <= 0f)
                return;
            if (_body == null)
                _body = GetComponent<DMPlayerBodyWeight>() ?? GetComponentInParent<DMPlayerBodyWeight>();
            if (_body != null)
                _body.AddJumpAttackImpulse(pitchDeg * p.jumpAttackBodyWeightScale, hipDrop * p.jumpAttackBodyWeightScale);
        }

        private void FixedUpdate()
        {
            if (_recovering && _rb != null && !_rb.isKinematic)
            {
                Vector3 rv = _rb.linearVelocity; // 25l: planted feet while squatting / rising
                _rb.linearVelocity = new Vector3(0f, Mathf.Min(rv.y, 0f), 0f);
            }
            if (!_leaping)
                return;
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || _cc == null || _cc.isDead || _cc.ragdolled)
            {
                EndLeap(false);
                return;
            }
            float t = Mathf.Clamp01((Time.time - _leapStart) / Mathf.Max(0.05f, _leapDur));
            float D = _leapDur;

            // Horizontal: cubic Hermite, start tangent = carried run velocity, end tangent 0 (plants at the stop distance).
            float t2 = t * t, t3 = t2 * t;
            float h00 = 2f * t3 - 3f * t2 + 1f, h10 = t3 - 2f * t2 + t, h01 = -2f * t3 + 3f * t2;
            Vector3 a = new Vector3(_from.x, 0f, _from.z), b = new Vector3(_to.x, 0f, _to.z);
            Vector3 pos = h00 * a + h10 * D * _v0 + h01 * b;

            // Vertical: ease-in rise to the apex (clip wind-up end), short hang, accelerating descent into the strike.
            float hang = _ref1H ? 0f : Mathf.Clamp(p.jumpAttackApexHang, 0f, Mathf.Max(0f, 0.9f - _apexT)); // 25j: strike starts AT the apex
            float off;
            if (t < _apexT)
            {
                float s = t / _apexT;
                off = 1f - Mathf.Pow(1f - s, p.jumpAttackRiseEase);
            }
            else if (t < _apexT + hang)
                off = 1f;
            else
            {
                float s = (t - _apexT - hang) / Mathf.Max(0.01f, 1f - _apexT - hang);
                off = 1f - Mathf.Pow(s, p.jumpAttackDescentPower);
            }
            float baseT = _plunge ? Mathf.Pow(t, p.jumpAttackDescentPower) : t;
            pos.y = Mathf.Lerp(_from.y, _to.y, baseT) + _arc * off;
            _rb.MovePosition(pos);
            Face(p, false);

            if (!_strikeSpeedSet && t >= _apexT && _anim != null)
            {
                _strikeSpeedSet = true;
                _anim.SetFloat(SpeedParam, _strikeSpeed); // down swing phase
            }
            if (!_windupKicked && !_ref1H && t >= _apexT * 0.5f)
            {
                _windupKicked = true;
                BodyKick(p, -p.jumpAttackWindupLean, 0f); // weight shifts back as the weapon goes up
            }
            if (t >= 1f)
                EndLeap(true);
        }

        private void Face(DM_CombatCoreProfile p, bool snap)
        {
            if (_target == null)
                return;
            Vector3 to = _target.transform.position - _rb.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.01f)
                return;
            Quaternion want = Quaternion.LookRotation(to.normalized, Vector3.up);
            Quaternion r = snap ? want : Quaternion.RotateTowards(_rb.rotation, want, p.jumpAttackTurnSpeed * Time.fixedDeltaTime);
            _rb.MoveRotation(r);
            transform.rotation = r;
        }
    }
}

using System.Collections.Generic;
using Invector.vCharacterController;
using Project.AI;
using Project.AI.Invector;
using Project.Player.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-19 d: melee attack facing with a combo target lock (replaces the per-frame nearest pick of
    /// DMMeleeBlockThreatFacing.ApplyAttackFacing for swings; block facing is unchanged).
    ///   - The target is picked once at the first swing (within meleeTargetAcquireDistance, inside the intent cone around
    ///     the stick / camera direction, distance + angle score) and held for the whole combo + grace. It is released only
    ///     on death, knockdown (optional), leaving meleeTargetReleaseDistance, or a deliberate stick retarget.
    ///   - Rotation is owned here in FixedUpdate (Invector ControlRotation is locked for that step), eased with
    ///     SmoothDampAngle at max meleeAssistTurnSpeed, soft-faded near alignment, only during the swing wind-up and
    ///     capped per swing. No other code turns the body during a locked swing, so nothing fights.
    /// 2026-10-19 e: also owns BLOCK auto-face (same sticky target / cone / eased turn, no per-swing budget, own grace),
    /// replacing DMMeleeBlockThreatFacing.ApplyBlockFacing (per-frame nearest pick, 540 deg/s LateUpdate step).
    /// Runtime-added by PioneerShooterMeleeInput; no prefab edits.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMMeleeAttackFacing : MonoBehaviour
    {
        // Audit / debug counters (reset with ResetStats).
        public static int StatAcquires;
        public static int StatSwitches;
        public static int StatReleases;
        public static int StatSnapFrames;   // frames with a body yaw change > 45 deg during a combo
        public static float StatMaxFrameYawDelta;
        public static int StatComboFrames;

        private static readonly List<EnemyHealth> s_buffer = new List<EnemyHealth>(32);

        private PioneerShooterMeleeInput _input;
        private vThirdPersonController _cc;
        private Animator _animator;
        private EnemyHealth _target;
        private float _lastComboTime = float.NegativeInfinity;
        private float _lastBlockTime = float.NegativeInfinity;
        public static int StatBlockFrames;
        private float _lastSwitchTime = float.NegativeInfinity;
        private int _swingHash;
        private float _swingBudget;
        private float _yawVelocity;
        private bool _savedLockRotation;
        private float _lastYaw = float.NaN;

        public EnemyHealth Target => _target;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ResetStats();
        }

        public static void ResetStats()
        {
            StatAcquires = 0;
            StatSwitches = 0;
            StatReleases = 0;
            StatSnapFrames = 0;
            StatMaxFrameYawDelta = 0f;
            StatComboFrames = 0;
            StatBlockFrames = 0;
        }

        public static DMMeleeAttackFacing Ensure(PioneerShooterMeleeInput input)
        {
            if (input == null)
                return null;
            DMMeleeAttackFacing f = input.GetComponent<DMMeleeAttackFacing>();
            if (f == null)
                f = input.gameObject.AddComponent<DMMeleeAttackFacing>();
            f._input = input;
            return f;
        }

        public static bool IsEnabled(DM_CombatCoreProfile p) => p != null && p.meleeComboTargetLock;
        public static bool IsBlockEnabled(DM_CombatCoreProfile p) => p != null && p.meleeBlockTargetLock;

        /// <summary>Before base.FixedUpdate: decides whether this step's rotation is owned here, applies it and locks Invector rotation.</summary>
        public bool BeginFixedStep()
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || (!IsEnabled(p) && !IsBlockEnabled(p)) || !Resolve())
                return false;

            float now = Time.time;
            bool block = IsBlockEnabled(p) && _input.WantsMeleeBlockFacing;
            bool combo = IsEnabled(p) && !block && _input.IsMeleeComboActive;
            if (combo)
                _lastComboTime = now;
            if (block)
                _lastBlockTime = now;

            if (!combo && !block && now - _lastComboTime > p.meleeComboLockGraceSeconds && now - _lastBlockTime > p.meleeBlockLockGraceSeconds)
            {
                Release(p, "comboEnd");
                _swingHash = 0;
                return false;
            }

            if (_target != null && !IsValid(_target, p, p.meleeTargetReleaseDistance))
                Release(p, "invalid");

            Vector3 stick = StickDirection(out float stickMag);
            Vector3 intent = stickMag > 0.2f ? stick : CameraForward();

            if (_target == null)
            {
                if (combo || block)
                {
                    _target = Pick(intent, p, null);
                    if (_target != null)
                    {
                        StatAcquires++;
                        _lastSwitchTime = now;
                        if (p.logMeleeFacing)
                            Debug.Log($"[MeleeFacing] acquire {_target.name} d={Planar(_target.transform.position - transform.position):0.0}m", this);
                    }
                }
            }
            else if (stickMag >= p.meleeRetargetStickMagnitude && now - _lastSwitchTime >= p.meleeRetargetMinSeconds)
            {
                Vector3 toTarget = Flat(_target.transform.position - transform.position);
                if (toTarget.sqrMagnitude > 0.0001f && Vector3.Angle(stick, toTarget) > p.meleeRetargetStickAngle)
                {
                    EnemyHealth cand = Pick(stick, p, _target);
                    if (cand != null && cand != _target)
                    {
                        if (p.logMeleeFacing)
                            Debug.Log($"[MeleeFacing] retarget {_target.name} -> {cand.name} (stick)", this);
                        _target = cand;
                        _lastSwitchTime = now;
                        StatSwitches++;
                    }
                }
            }

            if (_target == null)
                return false;

            if (block)
            {
                // Block: same single owner, eased, no per-swing budget; small tolerance so a held guard does not micro-turn.
                _savedLockRotation = _cc.lockRotation;
                _cc.lockRotation = true;
                ApplyAssist(p, p.meleeBlockAssistTurnSpeed, p.meleeBlockAssistSmoothTime, false, p.meleeBlockFacingToleranceDegrees);
                return true;
            }

            if (!_input.WantsMeleeAttackFacing)
                return false;

            // One rotation owner for the whole locked swing: Invector ControlRotation is skipped this step.
            _savedLockRotation = _cc.lockRotation;
            _cc.lockRotation = true;

            bool windup = SwingWindup(p);
            if (windup && _swingBudget > 0.01f)
                ApplyAssist(p, p.meleeAssistTurnSpeed, p.meleeAssistSmoothTime, true, 0.25f);
            else
                _yawVelocity = 0f;
            return true;
        }

        /// <summary>After base.FixedUpdate: restores Invector rotation lock.</summary>
        public void EndFixedStep(bool owned)
        {
            if (owned && _cc != null)
                _cc.lockRotation = _savedLockRotation;
        }

        private void ApplyAssist(DM_CombatCoreProfile p, float turnSpeed, float smoothTime, bool useBudget, float tolerance)
        {
            Vector3 toTarget = Flat(_target.transform.position - transform.position);
            if (toTarget.sqrMagnitude < 0.0025f)
                return;

            float curYaw = transform.eulerAngles.y;
            float targetYaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
            float err = Mathf.Abs(Mathf.DeltaAngle(curYaw, targetYaw));
            if (err < Mathf.Max(0.1f, tolerance))
            {
                _yawVelocity = 0f;
                return;
            }

            // Soft fade instead of a hard deadband edge: the allowed turn speed ramps up with the error.
            float fade = Mathf.SmoothStep(0.15f, 1f, Mathf.Clamp01(err / Mathf.Max(1f, p.meleeAssistSoftFadeDegrees)));
            float maxSpeed = Mathf.Max(1f, turnSpeed * fade);
            float dt = Time.fixedDeltaTime;
            float newYaw = Mathf.SmoothDampAngle(curYaw, targetYaw, ref _yawVelocity, Mathf.Max(0.01f, smoothTime), maxSpeed, dt);
            float step = Mathf.DeltaAngle(curYaw, newYaw);
            if (useBudget)
            {
                if (Mathf.Abs(step) > _swingBudget)
                    step = Mathf.Sign(step) * _swingBudget;
                _swingBudget -= Mathf.Abs(step);
            }
            if (Mathf.Abs(step) < 0.0001f)
                return;

            Vector3 e = transform.eulerAngles;
            e.y = curYaw + step;
            transform.rotation = Quaternion.Euler(e);
        }

        private bool SwingWindup(DM_CombatCoreProfile p)
        {
            if (_input.IsStrongChargeWindup)
            {
                const int chargeHash = 0x43484152; // one budget per charge hold
                if (_swingHash != chargeHash)
                {
                    _swingHash = chargeHash;
                    _swingBudget = p.meleeAssistMaxPerSwingDegrees;
                }
                return true;
            }

            int layer = _input.MeleeFullBodyLayer;
            if (_animator == null || layer < 0 || layer >= _animator.layerCount)
                return false;

            AnimatorStateInfo st = _animator.IsInTransition(layer)
                ? _animator.GetNextAnimatorStateInfo(layer)
                : _animator.GetCurrentAnimatorStateInfo(layer);
            if (st.fullPathHash != _swingHash)
            {
                _swingHash = st.fullPathHash;
                _swingBudget = p.meleeAssistMaxPerSwingDegrees;
            }

            float n = st.normalizedTime;
            return n >= 0f && n < Mathf.Clamp01(p.meleeAssistWindupFraction);
        }

        private EnemyHealth Pick(Vector3 intent, DM_CombatCoreProfile p, EnemyHealth exclude)
        {
            intent = Flat(intent);
            if (intent.sqrMagnitude < 0.0001f)
                intent = Flat(transform.forward);
            intent.Normalize();

            float cone = Mathf.Clamp(p.meleeTargetIntentConeHalfAngle, 5f, 180f);
            float maxD = Mathf.Max(0.5f, p.meleeTargetAcquireDistance);
            EnemyHealthSceneRegistry.CopyTo(s_buffer);
            EnemyHealth best = null;
            float bestScore = float.PositiveInfinity;
            for (int i = 0; i < s_buffer.Count; i++)
            {
                EnemyHealth e = s_buffer[i];
                if (e == null || e == exclude || !IsValid(e, p, maxD))
                    continue;

                Vector3 to = Flat(e.transform.position - transform.position);
                float d = to.magnitude;
                float ang = d > 0.01f ? Vector3.Angle(intent, to) : 0f;
                if (ang > cone)
                    continue;

                float score = d / maxD + p.meleeTargetAngleWeight * (ang / cone);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = e;
                }
            }

            s_buffer.Clear();
            return best;
        }

        private bool IsValid(EnemyHealth e, DM_CombatCoreProfile p, float maxDistance)
        {
            if (e == null || e.IsDead || !e.isActiveAndEnabled)
                return false;
            if (p.meleeTargetIgnoreDowned && DMEnemyKnockdown.IsDown(e.gameObject))
                return false;
            return Planar(e.transform.position - transform.position) <= maxDistance;
        }

        private void Release(DM_CombatCoreProfile p, string why)
        {
            if (_target == null)
                return;
            if (p != null && p.logMeleeFacing)
                Debug.Log($"[MeleeFacing] release {_target.name} ({why})", this);
            _target = null;
            _yawVelocity = 0f;
            StatReleases++;
        }

        private Vector3 StickDirection(out float magnitude)
        {
            Vector3 input = _cc.input;
            magnitude = Mathf.Clamp01(new Vector2(input.x, input.z).magnitude);
            if (magnitude < 0.01f)
                return Vector3.zero;

            Transform cam = _input.cameraMain != null ? _input.cameraMain.transform : null;
            Vector3 fwd = cam != null ? Flat(cam.forward) : Flat(transform.forward);
            if (fwd.sqrMagnitude < 0.0001f)
                fwd = Flat(transform.forward);
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            Vector3 dir = right * input.x + fwd * input.z;
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
        }

        private Vector3 CameraForward()
        {
            Transform cam = _input.cameraMain != null ? _input.cameraMain.transform : null;
            Vector3 fwd = cam != null ? Flat(cam.forward) : Flat(transform.forward);
            return fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Flat(transform.forward).normalized;
        }

        private void LateUpdate()
        {
            // Per-frame body yaw delta while a combo is alive (audit / log only).
            float yaw = transform.eulerAngles.y;
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            bool blockAlive = p != null && Time.time - _lastBlockTime <= p.meleeBlockLockGraceSeconds;
            bool comboAlive = p != null && (Time.time - _lastComboTime <= p.meleeComboLockGraceSeconds || blockAlive);
            if (blockAlive && !float.IsNaN(_lastYaw))
                StatBlockFrames++;
            if (comboAlive && !float.IsNaN(_lastYaw))
            {
                float d = Mathf.Abs(Mathf.DeltaAngle(_lastYaw, yaw));
                StatComboFrames++;
                if (d > StatMaxFrameYawDelta)
                    StatMaxFrameYawDelta = d;
                if (d > 45f)
                {
                    StatSnapFrames++;
                    if (p.logMeleeFacing)
                        Debug.Log($"[MeleeFacing] snap frame dYaw={d:0.0} target={(_target != null ? _target.name : "-")}", this);
                }
            }
            _lastYaw = yaw;
        }

        private void OnDisable()
        {
            _target = null;
            _yawVelocity = 0f;
        }

        private bool Resolve()
        {
            if (_input == null)
                _input = GetComponent<PioneerShooterMeleeInput>();
            if (_cc == null && _input != null)
                _cc = _input.cc as vThirdPersonController;
            if (_animator == null)
                _animator = GetComponent<Animator>();
            return _input != null && _cc != null;
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        private static float Planar(Vector3 v)
        {
            v.y = 0f;
            return v.magnitude;
        }
    }
}

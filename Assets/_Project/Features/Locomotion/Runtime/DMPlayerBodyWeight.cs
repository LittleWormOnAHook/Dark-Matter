using Invector.vCharacterController;
using Invector.vShooter;
using Project.Features.Climb;
using UnityEngine;

namespace Project.Features.Locomotion
{
    /// <summary>
    /// 2026-10-20l/n: grounded "body weight" secondary motion, modelled on DMJetpackLimbDangle (spring-damped bone offsets
    /// driven by body acceleration).
    /// Channels: pelvis turn lead, spine underdamped settle spring (lean into accel, stop carry and sway-settle),
    /// chest phase lag behind spine, shoulder-hip counter-rotation, head stabilisation, CoM weight shift,
    /// upper arm / elbow / wrist trailing chain, landing compression (fall-speed scaled, slow recovery),
    /// direction-change dip, and a melee swing follow-through spring (spine/chest/pelvis/arm).
    /// Never accumulates: each bone's base pose is the animator's fresh pose. If the animator did not re-pose a bone
    /// this frame (Fixed update mode, no physics tick), last frame's cached base is reused instead of stacking.
    /// Global scale: profile.BodyWeightScale (preset or intensity 0-10, times debug exaggerate). Only NaN/explosion guards clamp it.
    /// Settings: DM_ClimbDashProfile "Body weight" (master toggle default OFF).
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(3320)]
    public sealed class DMPlayerBodyWeight : MonoBehaviour
    {
        private const float GuardDeg = 170f;   // explosion guard only
        private const float GuardMeters = 1.5f;
        // 2026-10-20w (chaos pass): output caps. At Intensity x Exaggerate up to 100 the euler offsets reached 150-180 deg and wrapped
        // (360 deg/frame flips) and the hips moved 1.5 m. Look-safe caps; internal state guards unchanged.
        private const float MaxBoneDeg = 75f;
        private const float MaxHipMeters = 0.35f;
        private const float MaxBoneSlew = 1500f; // deg/s per bone

        private struct Bone
        {
            public Transform T;
            public Quaternion Base, Written;
            public bool Has;
            public Vector3 Off, OffVel;
            public Vector3 Extra; // swing spring (deg), already weighted
            public Quaternion LastQ; // 2026-10-20w: last written offset rotation (slew limit; default(Quaternion) = unset)
            public Quaternion Anim; public bool AnimHas; // 2026-10-20y: last fresh animator pose (pre aim IK)
            public Vector3 Kick; // 2026-10-20y: recoil, not aim-faded

            /// <summary>2026-10-20y: runs before Invector aim IK / head track. If the Animator did not re-evaluate this frame
            /// (Fixed/AnimatePhysics update, culling), the bone still holds last frame's IK+offset pose; restore the fresh
            /// animator pose so neither aim IK nor body weight stacks on top of last frame's output (accumulation jitter).</summary>
            public void PreIK()
            {
                if (T == null || !Has) return;
                Quaternion cur = T.localRotation;
                // 2026-10-21a: restore the last pre-offset pose (animator + IK), NOT the raw animator pose. In Animator Fixed mode
                // Invector IK / head track only run on physics-step frames; restoring the raw pose made every non-step frame drop
                // the IK (armed idle: head track + weapon arm IK) -> per-frame upper-body jitter.
                if (Quaternion.Angle(cur, Written) <= 0.01f) { if (AnimHas) T.localRotation = Base; }
                else { Anim = cur; AnimHas = true; }
            }

            /// <summary>2026-10-24h: deterministic restore (no tolerance heuristic) on frames the Animator did not evaluate.</summary>
            public void RestoreBase() { if (T != null && Has) T.localRotation = Base; }

            public void Begin(bool trustFresh)
            {
                if (T == null) return;
                Quaternion cur = T.localRotation;
                // 2026-10-24h: live play: PreIK guarantees the bone holds a clean (offset-free) pose -> always take it.
                // Old heuristic (Quaternion.Angle <= 0.01 deg; Unity treats < ~0.16 deg as equal) mis-classified
                // fresh vs stale poses on alternate frames under Animator Fixed mode -> whole-body per-frame vibration.
                if (trustFresh || !Has || Quaternion.Angle(cur, Written) > 0.01f) Base = cur;
                T.localRotation = Base;
                Has = true;
            }

            public void Drive(Vector3 targetDeg, float smooth, float dt)
            {
                Off = Vector3.SmoothDamp(Off, targetDeg, ref OffVel, Mathf.Max(0.01f, smooth), Mathf.Infinity, dt);
                if (!Ok(Off)) { Off = Vector3.zero; OffVel = Vector3.zero; }
            }

            public void End(float weight) => End(weight, float.PositiveInfinity);

            public void End(float weight, float maxStep) => End(weight, maxStep, 1f);

            public void End(float weight, float maxStep, float fade)
            {
                if (T == null) return;
                Vector3 e = Vector3.ClampMagnitude((Off * weight + Extra) * fade + Kick, MaxBoneDeg);
                Kick = Vector3.zero;
                if (!Ok(e)) e = Vector3.zero;
                // 2026-10-20w (chaos pass): cap + slew limit in rotation space. Saturated euler offsets could flip in one frame
                // (up to 160 deg/frame pops, partly euler gimbal at large angles).
                Quaternion q = Quaternion.Euler(e);
                if (Quaternion.Angle(Quaternion.identity, q) > MaxBoneDeg) q = Quaternion.RotateTowards(Quaternion.identity, q, MaxBoneDeg);
                Quaternion last = LastQ.x == 0f && LastQ.y == 0f && LastQ.z == 0f && LastQ.w == 0f ? Quaternion.identity : LastQ;
                q = Quaternion.RotateTowards(last, q, maxStep);
                LastQ = q;
                Written = Base * q;
                Extra = Vector3.zero;
                T.localRotation = Written;
            }

            public void Commit() { if (T != null) Written = T.localRotation; }

            public void Clear() { Off = Vector3.zero; OffVel = Vector3.zero; Extra = Vector3.zero; Kick = Vector3.zero; LastQ = Quaternion.identity; Has = false; AnimHas = false; }
        }

        private static bool Ok(Vector3 v) => !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));

        private Animator _anim;
        private vThirdPersonMotor _motor;
        private vShooterMeleeInput _shooter;
        private Rigidbody _rb;
        private Bone _pelvis, _neck, _head;
        private Bone _spine, _chest, _lUpperArm, _rUpperArm, _lLowerArm, _rLowerArm, _lHandB, _rHandB;
        private Transform _hips, _hand;
        private Quaternion _lastHandRot;
        private bool _hasHand;
        private Vector3 _swing, _swingVel;
        private float _swingWeight;
        private Vector3 _hipsBase, _hipsWritten;
        private bool _hipsHas;
        private float _hipDrop, _hipDropVel;
        private Vector3 _lastVel, _accel, _accelVel;
        private float _lastYaw, _yawRate, _yawRateVel;
        private bool _hasVel, _wasGrounded = true, _ready;
        private float _weight, _enabledWeight, _airVy;
        private float _comShift, _comShiftVel;
        private Vector3 _trunk, _trunkVel; // underdamped settle spring (deg)
        private bool _lastEnabled, _loggedAttach;
        // 2026-10-20p weapon weight
        private Vector3 _lastOmega, _alpha, _alphaVel, _heldTarget;
        private bool _wasHold;
        private Vector3 _recoil, _recoilVel; // x = spine pitch (deg)
        private float _recoilHip, _recoilHipVel;
        private Object _lastWeapon; private bool _hasWeaponRef;
        private vShooterWeapon _shotWeapon;
        private int _pendingShots;
        private float _ww = 1f;
        private Invector.vMelee.vMeleeManager _melee;
        private string _gate = "-";
        /// <summary>2026-10-20q: extra spine+chest forward pitch (deg) requested by DMDownedCombo; applied even when body weight is off.</summary>
        public static float ExternalPitchDegrees;

        // ---- 2026-10-20w stage 3: hit reactions, foot planting, breathing; editor-harness sim hooks ----
        private Bone _lShoulder, _rShoulder, _lThigh, _lKnee, _lFoot, _rThigh, _rKnee, _rFoot;
        private Vector3 _hitPending, _hitTrunk, _hitTrunkVel, _hitHead, _hitHeadVel, _shove, _shoveVel;
        private float _exert, _breathPhase, _hardGate;
        private Project.Features.Jetpack.DMJetpackController _jet;
        private struct Plant { public bool On, HasLow; public Vector3 Pos; public float W, Low, Err, Over; }
        private Plant _lPlant, _rPlant;
        private float _plantHipLower;
        private float _lagYaw, _lagYawVel, _lagOff; private bool _hasLagYaw;
        private Vector3 _accLocal, _accLocalVel;
        private float _simDt;
        /// <summary>Editor harness only: LateUpdate idles and the harness calls SimStep with these inputs.</summary>
        [System.NonSerialized] public bool SimActive;
        public struct SimFrame { public Vector3 Vel, Input, HandOmegaDeg; public bool Grounded, Attacking, Aiming, Exerting, HitStop; }
        [System.NonSerialized] public SimFrame Sim;
        /// <summary>Editor harness profile (null = Live).</summary>
        public static DM_ClimbDashProfile ProfileOverride;
        private static DM_ClimbDashProfile Prof => ProfileOverride != null ? ProfileOverride : DM_ClimbDashProfile.Live;
        private float Dt => SimActive ? _simDt : Time.deltaTime;

        public static void Ensure(GameObject player)
        {
            if (player != null && player.GetComponent<DMPlayerBodyWeight>() == null && player.GetComponent<vThirdPersonMotor>() != null)
                player.AddComponent<DMPlayerBodyWeight>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var go = new GameObject("DMPlayerBodyWeightBoot") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            go.AddComponent<Poller>();
        }

        private sealed class Poller : MonoBehaviour
        {
            private float _next;
            private void Update()
            {
                if (Time.unscaledTime < _next) return;
                _next = Time.unscaledTime + 1f;
                var pl = Project.Core.PlayerLocator.FindPlayerObject(); Ensure(pl); DMFootstepTriggerFit.Ensure(pl); DMWallHandTouch.Ensure(pl); // 2026-10-24h, 24o
            }
        }

        private void Awake()
        {
            _motor = GetComponent<vThirdPersonMotor>();
            _shooter = GetComponent<vShooterMeleeInput>();
            _rb = GetComponent<Rigidbody>();
            _melee = GetComponent<Invector.vMelee.vMeleeManager>();
            _anim = _motor != null && _motor.animator != null ? _motor.animator : GetComponentInChildren<Animator>();
            _jet = GetComponentInChildren<Project.Features.Jetpack.DMJetpackController>();
            Cache();
        }

        private void Start()
        {
            var p = DM_ClimbDashProfile.Live;
            Debug.Log($"[BodyWeight] attached to '{name}' ready={_ready} human={(_anim != null && _anim.isHuman)} animMode={(_anim != null ? _anim.updateMode.ToString() : "none")} " +
                      $"profile={(p != null ? p.name + "#" + p.GetHashCode() : "NULL")} enabled={(p != null && p.bodyWeightEnabled)} scale={(p != null ? p.BodyWeightScale : 0f):0.##}");
            _loggedAttach = true;
        }

        private void Cache()
        {
            if (_anim == null || !_anim.isHuman) return;
            _spine.T = _anim.GetBoneTransform(HumanBodyBones.Spine);
            _chest.T = _anim.GetBoneTransform(HumanBodyBones.Chest);
            if (_chest.T == null) _chest.T = _anim.GetBoneTransform(HumanBodyBones.UpperChest);
            _lUpperArm.T = _anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            _rUpperArm.T = _anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _lLowerArm.T = _anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            _rLowerArm.T = _anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
            _lHandB.T = _anim.GetBoneTransform(HumanBodyBones.LeftHand);
            _rHandB.T = _anim.GetBoneTransform(HumanBodyBones.RightHand);
            _hips = _anim.GetBoneTransform(HumanBodyBones.Hips);
            _pelvis.T = _hips;
            _neck.T = _anim.GetBoneTransform(HumanBodyBones.Neck);
            _head.T = _anim.GetBoneTransform(HumanBodyBones.Head);
            _hand = _rHandB.T;
            _lShoulder.T = _anim.GetBoneTransform(HumanBodyBones.LeftShoulder);
            _rShoulder.T = _anim.GetBoneTransform(HumanBodyBones.RightShoulder);
            _lThigh.T = _anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            _lKnee.T = _anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            _lFoot.T = _anim.GetBoneTransform(HumanBodyBones.LeftFoot);
            _rThigh.T = _anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            _rKnee.T = _anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            _rFoot.T = _anim.GetBoneTransform(HumanBodyBones.RightFoot);
            _ready = _spine.T != null && _hips != null;
            if (_ready && !SimActive)
            {
                var pre = GetComponent<DMPlayerBodyWeightPreIK>();
                if (pre == null) pre = gameObject.AddComponent<DMPlayerBodyWeightPreIK>();
                pre.Owner = this;
            }
        }

        // 2026-10-20y: called from DMPlayerBodyWeightPreIK (execution order -500, before Invector aim IK / vHeadTrack).
        private bool _stepSeen, _livePre;
        /// <summary>2026-10-24h: FixedUpdate seen since last LateUpdate (Animator Fixed mode evaluates only on physics steps).</summary>
        internal void MarkStep() { _stepSeen = true; }

        internal void PreIK()
        {
            if (!_ready || SimActive) return;
            // 2026-10-24h: deterministic: the Animator re-posed this frame iff it updates every frame or a physics step ran.
            bool fresh = _anim == null || _anim.updateMode != AnimatorUpdateMode.Fixed || _stepSeen;
            _stepSeen = false;
            _livePre = true;
            if (fresh) return;
            if (!_spine.Has) return;
            _pelvis.RestoreBase(); _neck.RestoreBase(); _head.RestoreBase(); _spine.RestoreBase(); _chest.RestoreBase();
            _lUpperArm.RestoreBase(); _rUpperArm.RestoreBase(); _lLowerArm.RestoreBase(); _rLowerArm.RestoreBase(); _lHandB.RestoreBase(); _rHandB.RestoreBase();
            _lShoulder.RestoreBase(); _rShoulder.RestoreBase(); _lThigh.RestoreBase(); _lKnee.RestoreBase(); _lFoot.RestoreBase(); _rThigh.RestoreBase(); _rKnee.RestoreBase(); _rFoot.RestoreBase();
            if (_hipsHas && _hips != null) _hips.localPosition = _hipsBase;
            return;
#pragma warning disable CS0162
            _pelvis.PreIK(); _neck.PreIK(); _head.PreIK(); _spine.PreIK(); _chest.PreIK();
            _lUpperArm.PreIK(); _rUpperArm.PreIK(); _lLowerArm.PreIK(); _rLowerArm.PreIK(); _lHandB.PreIK(); _rHandB.PreIK();
            _lShoulder.PreIK(); _rShoulder.PreIK(); _lThigh.PreIK(); _lKnee.PreIK(); _lFoot.PreIK(); _rThigh.PreIK(); _rKnee.PreIK(); _rFoot.PreIK();
            if (_hipsHas && _hips != null)
            {
                Vector3 hl = _hips.localPosition;
                if ((hl - _hipsWritten).sqrMagnitude <= 1e-8f) { if (_hipsAnimHas) _hips.localPosition = _hipsBase; } // 2026-10-21a
                else { _hipsAnim = hl; _hipsAnimHas = true; }
            }
#pragma warning restore CS0162
        }
        private Vector3 _hipsAnim; private bool _hipsAnimHas;
        private Vector3 _heldAcc; private float _heldYaw, _sampleAge; // 2026-10-21a
        private float _aimFade = 1f, _aimHip = 1f;
        private bool _aimingNow;

        private bool Blocked()
        {
            if (SimActive) { _gate = Sim.Grounded ? "open" : "airborne"; return !Sim.Grounded; }
            if (_motor == null || _anim == null || !_anim.enabled) { _gate = "no motor/anim"; return true; }
            if (_motor.isDead) { _gate = "dead"; return true; }
            if (_motor.ragdolled) { _gate = "ragdolled"; return true; }
            if (_motor.customAction) { _gate = "customAction"; return true; }
            if (!_motor.isGrounded) { _gate = "airborne"; return true; }
            _gate = "open";
            return false;
        }

        private void ClearAll()
        {
            _pelvis.Clear(); _neck.Clear(); _head.Clear();
            _spine.Clear(); _chest.Clear(); _lUpperArm.Clear(); _rUpperArm.Clear(); _lLowerArm.Clear(); _rLowerArm.Clear(); _lHandB.Clear(); _rHandB.Clear();
            _hasHand = false; _swing = Vector3.zero; _swingVel = Vector3.zero; _swingWeight = 0f;
            _hipsHas = false; _hipDrop = 0f; _hipDropVel = 0f; _hasVel = false; _weight = 0f; _enabledWeight = 0f;
            _trunk = Vector3.zero; _trunkVel = Vector3.zero; _comShift = 0f; _comShiftVel = 0f;
            _lShoulder.Clear(); _rShoulder.Clear(); _lThigh.Clear(); _lKnee.Clear(); _lFoot.Clear(); _rThigh.Clear(); _rKnee.Clear(); _rFoot.Clear();
            _hitPending = _hitTrunk = _hitTrunkVel = _hitHead = _hitHeadVel = _shove = _shoveVel = Vector3.zero;
            _lPlant = default; _rPlant = default; _plantHipLower = 0f; _hardGate = 0f;
            _hasLagYaw = false; _lagYawVel = 0f; _lagOff = 0f; _accLocal = _accLocalVel = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (SimActive) return;
            Tick();
        }

        /// <summary>Editor harness: one simulated frame (Sim inputs, fixed dt).</summary>
        public void SimStep(float dt) { _simDt = dt; Tick(); }

        private void Tick()
        {
            DM_ClimbDashProfile p = Prof;
            bool en = p != null && p.bodyWeightEnabled;
            if (en != _lastEnabled || !_loggedAttach)
            {
                Debug.Log($"[BodyWeight] enabled={en} on '{name}' profile={(p != null ? p.name + "#" + p.GetHashCode() : "NULL")} scale={(p != null ? p.BodyWeightScale : 0f):0.##}");
                _lastEnabled = en; _loggedAttach = true;
            }
            bool external = Mathf.Abs(ExternalPitchDegrees) > 0.01f;
            if ((!en && !external) || Dt <= 0f)
            {
                if (_weight > 0f || _spine.Has) ClearAll();
                _gate = p == null ? "no profile" : "disabled";
                return;
            }
            if (!_ready) { Cache(); if (!_ready) { _gate = "bones missing"; return; } }

            float dt = Mathf.Min(Dt, Mathf.Max(0.005f, p.bodyWeightMaxDt)); // 2026-10-24h: dt spike clamp (springs are sub-stepped)
            float S = en ? p.BodyWeightScale : 0f;
            if (!SimActive) UpdateWeaponEvents(p);
            bool blocked = Blocked();
            bool aiming = SimActive ? Sim.Aiming : (_shooter != null && _shooter.IsAiming);
            if (aiming && !blocked) _gate = "aiming x" + p.bodyWeightAimScale.ToString("0.##");
            float goal = blocked ? 0f : (aiming ? p.bodyWeightAimScale : 1f);
            _weight = Mathf.MoveTowards(_weight, goal, dt / 0.15f);
            // Hip/landing channel is NOT faded by airborne (round-1 bug: weight was ~0 on the landing frame, killing the dip).
            bool hardBlock = !SimActive && (_motor == null || _motor.isDead || _motor.ragdolled || _motor.customAction);
            _hardGate = Mathf.MoveTowards(_hardGate, hardBlock ? 0f : 1f, dt / 0.15f);
            _enabledWeight = Mathf.MoveTowards(_enabledWeight, hardBlock ? 0f : (aiming ? p.bodyWeightAimScale : 1f), dt / 0.15f);
            float aimFadeStep = dt / Mathf.Max(0.01f, p.bodyWeightAimFadeTime);
            _aimFade = Mathf.MoveTowards(_aimFade, aiming ? p.bodyWeightAimUpperBodyScale : 1f, aimFadeStep);
            _aimHip = Mathf.MoveTowards(_aimHip, aiming ? p.bodyWeightAimHipSwayScale : 1f, aimFadeStep);
            _aimingNow = aiming;

            bool tf = !SimActive && _livePre; _livePre = false;
            _pelvis.Begin(tf); _neck.Begin(tf); _head.Begin(tf);
            _spine.Begin(tf); _chest.Begin(tf); _lUpperArm.Begin(tf); _rUpperArm.Begin(tf); _lLowerArm.Begin(tf); _rLowerArm.Begin(tf); _lHandB.Begin(tf); _rHandB.Begin(tf);
            _lShoulder.Begin(tf); _rShoulder.Begin(tf); _lThigh.Begin(tf); _lKnee.Begin(tf); _lFoot.Begin(tf); _rThigh.Begin(tf); _rKnee.Begin(tf); _rFoot.Begin(tf);
            Vector3 hl = _hips.localPosition;
            if (tf || !_hipsHas || (hl - _hipsWritten).sqrMagnitude > 1e-8f) _hipsBase = hl;
            _hipsHas = true;

            Vector3 vel = SimActive ? Sim.Vel : (_rb != null ? _rb.linearVelocity : Vector3.zero);
            float yaw = transform.eulerAngles.y;
            if (!_hasVel) { _lastVel = vel; _lastYaw = yaw; _hasVel = true; }
            // 2026-10-21a: the rigidbody (and root yaw) only change on physics steps; per-render-frame differencing gave 0 / spike / 0
            // input. Difference over the time since the last real change, hold between steps, and deadzone idle noise.
            bool noInput = SimActive ? Sim.Input.sqrMagnitude < 0.01f : (_motor == null || new Vector3(_motor.input.x, 0f, _motor.input.z).sqrMagnitude < 0.01f);
            Vector3 vPlanar = new Vector3(vel.x, 0f, vel.z);
            if (noInput && vPlanar.magnitude < p.bodyWeightIdleSpeedDeadzone) { vel.x = 0f; vel.z = 0f; }
            _sampleAge += dt;
            bool velChanged = (vel - _lastVel).sqrMagnitude > 1e-10f, yawChanged = Mathf.Abs(Mathf.DeltaAngle(_lastYaw, yaw)) > 1e-4f;
            float sdt = SimActive ? dt : Mathf.Max(dt, _sampleAge);
            if (velChanged || yawChanged || SimActive || _sampleAge > 0.05f)
            {
                _heldAcc = Vector3.ClampMagnitude((vel - _lastVel) / sdt, 40f); _heldAcc.y = 0f;
                if (_heldAcc.magnitude < p.bodyWeightAccelDeadzone) _heldAcc = Vector3.zero;
                _heldYaw = Mathf.Clamp(Mathf.DeltaAngle(_lastYaw, yaw) / sdt, -720f, 720f);
                if (Mathf.Abs(_heldYaw) < p.bodyWeightYawRateDeadzone) _heldYaw = 0f;
                _lastVel = vel; _lastYaw = yaw; _sampleAge = 0f;
            }
            Vector3 rawAcc = _heldAcc;
            float rawYaw = _heldYaw;
            _accel = Vector3.SmoothDamp(_accel, rawAcc, ref _accelVel, p.bodyWeightAccelSmooth, Mathf.Infinity, dt);
            _yawRate = Mathf.SmoothDamp(_yawRate, rawYaw, ref _yawRateVel, p.bodyWeightAccelSmooth, Mathf.Infinity, dt);
            // 2026-10-20w (harness): smooth acceleration in the character frame. World-space smoothing lagged the heading during fast
            // turns and leaked centripetal acceleration into forward/back pitch (~20 deg spurious pitch in a 180 deg turn at Visible).
            _accLocal = Vector3.SmoothDamp(_accLocal, transform.InverseTransformDirection(rawAcc), ref _accLocalVel, p.bodyWeightAccelSmooth, Mathf.Infinity, dt);
            if (!Ok(_accLocal)) { _accLocal = Vector3.zero; _accLocalVel = Vector3.zero; }
            Vector3 la = _accLocal;

            // Landing compression scaled by fall speed; slow recovery handled in the hip spring below.
            bool grounded = SimActive ? Sim.Grounded : _motor.isGrounded;
            if (!grounded) _airVy = Mathf.Min(_airVy, vel.y); 
            if (grounded && !_wasGrounded)
            {
                float fall = Mathf.Clamp(-_airVy / 5f, 0.5f, 3f);
                _hipDropVel -= p.bodyWeightLandingDrop * 10f * fall;
                _trunkVel.x += 25f * fall * p.bodyWeightLandingDrop / 0.08f; // chest folds forward a touch on impact
                _airVy = 0f;
            }
            if (grounded) _airVy = 0f;
            _wasGrounded = grounded;
            if (!blocked && rawAcc.magnitude > p.bodyWeightDirChangeAccel)
                _hipDropVel -= p.bodyWeightDirChangeDrop * dt * 40f;

            float fwd = la.z;
            float pitch = fwd >= 0f ? fwd * p.bodyWeightLeanAccel : -fwd * p.bodyWeightStopCarry;
            float roll = -la.x * p.bodyWeightLeanTurn;
            // 2026-10-20w (harness): chest yaw = critically damped follower of the root yaw -> a true time lag (~Yaw Lag x 10 ms) behind
            // the pelvis instead of a rate-proportional target behind a spring (measured lag was 14-26 ms, target 50-120 ms).
            float yawLag = 0f;
            if (!_hasLagYaw) { _lagYaw = yaw; _lagYawVel = 0f; _hasLagYaw = true; }
            _lagYaw = Mathf.SmoothDampAngle(_lagYaw, yaw, ref _lagYawVel, Mathf.Max(0.005f, p.bodyWeightYawLag * 0.01f), Mathf.Infinity, dt);
            float lagD = Mathf.DeltaAngle(yaw, _lagYaw), lagMax = Mathf.Max(0f, p.bodyWeightMaxSpineDegrees);
            if (float.IsNaN(lagD) || float.IsNaN(_lagYawVel)) { _lagYaw = yaw; _lagYawVel = 0f; lagD = 0f; }
            if (Mathf.Abs(lagD) > lagMax) { _lagYaw = yaw + Mathf.Sign(lagD) * lagMax; lagD = Mathf.Sign(lagD) * lagMax; } // drag: never stays wound up
            _lagOff = blocked ? 0f : lagD;
            float max = p.bodyWeightMaxSpineDegrees;
            // 2026-10-24r: motor.input is the raw STICK (camera-relative intent), not character-local. Comparing it to the
            // character-local velocity made a backward/diagonal start (free locomotion turns the body toward the move dir)
            // read as a big lateral "input vs velocity" mismatch -> brief anticipation roll (right lean). Use the world
            // moveDirection taken into the body frame instead.
            Vector3 inLocal = SimActive ? Sim.Input : transform.InverseTransformDirection(new Vector3(_motor.moveDirection.x, 0f, _motor.moveDirection.z));
            inLocal.y = 0f;
            Vector3 vLocal = transform.InverseTransformDirection(vel); vLocal.y = 0f;
            if (inLocal.sqrMagnitude > 0.01f && vLocal.sqrMagnitude > 0.04f)
            {
                Vector3 diff = inLocal.normalized - vLocal.normalized;
                pitch += diff.z * p.bodyWeightAnticipation;
                roll -= diff.x * p.bodyWeightAnticipation;
            }

            // Pelvis leads the turn (fast); spine follows through an underdamped spring (sway past + settle after stops);
            // chest lags the spine by bodyWeightChestLag (phase offset) and counter-rotates against the pelvis.
            float pelvisYaw = Mathf.Clamp(_yawRate * p.bodyWeightPelvisLead * 0.01f, -max, max);
            _pelvis.Drive(new Vector3(0f, pelvisYaw, 0f), p.bodyWeightPelvisSmooth, dt);
            Vector3 spineT = new Vector3(Mathf.Clamp(pitch, -max, max), Mathf.Clamp(yawLag, -max, max), Mathf.Clamp(roll, -max, max));
            float tk = 1f / Mathf.Max(0.02f, p.bodyWeightSpineSmooth); tk = tk * tk * 4f; // stiffness from smooth time
            float tc = p.bodyWeightSettleDamping * 2f * Mathf.Sqrt(tk);
            Spring(ref _trunk, ref _trunkVel, tk, p.bodyWeightSettleDamping, dt, GuardDeg, spineT); // 2026-10-20w: sub-stepped (explicit step blew up at Spine Smooth < 0.05)
            Vector3 counter = new Vector3(0f, -_pelvis.Off.y * p.bodyWeightCounterRotation, 0f);
            _spine.Off = _trunk * 0.45f + counter * 0.3f; _spine.OffVel = Vector3.zero;
            _chest.Drive(_trunk * 0.55f + counter * 0.7f, Mathf.Max(0.01f, p.bodyWeightChestLag), dt);

            Vector3 trunk = _pelvis.Off + _spine.Off + _chest.Off;
            Vector3 headT = -trunk * p.bodyWeightHeadStabilize;
            _neck.Drive(headT * 0.4f, p.bodyWeightHeadSmooth, dt);
            _head.Drive(headT * 0.6f, p.bodyWeightHeadSmooth, dt);

            if (!blocked && fwd < 0f)
                _hipDropVel -= (-fwd) * p.bodyWeightDecelCompression * dt * 10f;

            // Arm chain: upper arm -> elbow -> wrist, each with more whip and more lag (overlapping action).
            Vector3 inertia = -_accel * p.bodyWeightArmTrail;
            float asm = p.bodyWeightArmSmooth;
            DriveArm(ref _lUpperArm, inertia, 1f, asm, p, dt);
            DriveArm(ref _rUpperArm, inertia, 1f, asm, p, dt);
            DriveArm(ref _lLowerArm, inertia, p.bodyWeightElbowTrail, asm * 1.4f, p, dt);
            DriveArm(ref _rLowerArm, inertia, p.bodyWeightElbowTrail, asm * 1.4f, p, dt);
            DriveArm(ref _lHandB, inertia, p.bodyWeightWristTrail, asm * 1.8f, p, dt);
            DriveArm(ref _rHandB, inertia, p.bodyWeightWristTrail * 0.5f, asm * 1.8f, p, dt); // right hand holds weapon: half

            // Hip drop spring: full stiffness while compressing, reduced while recovering (slow rise).
            float k = p.bodyWeightHipSpring;
            if (_hipDrop < 0f && _hipDropVel > 0f) k *= p.bodyWeightLandingRecovery;
            SpringF(ref _hipDrop, ref _hipDropVel, 0f, k, 1f, dt);
            _hipDrop = Mathf.Clamp(_hipDrop, -p.bodyWeightMaxHipDrop, 0f); // 2026-10-24h: pelvis never raised (overshoot lifted feet off the ground)
            if (_hipDrop >= 0f && _hipDropVel > 0f) _hipDropVel = 0f;
            if (float.IsNaN(_hipDrop)) { _hipDrop = 0f; _hipDropVel = 0f; }

            float w = _weight * S;
            UpdateSwingSpring(p, dt, blocked, S);
            _rHandB.Extra = Vector3.zero; // keep weapon hand from double-dipping the swing
            float hardW = (!SimActive && (_motor == null || _motor.isDead || _motor.ragdolled)) ? 0f : S; // recoil is NOT aim-faded
            UpdateRecoil(p, dt, hardW);
            if (external)
            {
                _spine.Extra.x += ExternalPitchDegrees * 0.4f;
                _chest.Extra.x += ExternalPitchDegrees * 0.6f;
            }
            AddChar(ref _spine, new Vector3(0f, _lagOff * 0.4f * w, 0f)); AddChar(ref _chest, new Vector3(0f, _lagOff * 0.6f * w, 0f));
            HitAndBreath(p, dt, S * _hardGate, aiming);
            float slew = MaxBoneSlew * dt;
            float af = _aimFade; // 2026-10-20y: upper-body aim fade (recoil Kick is applied unfaded)
            _pelvis.End(w, slew, af); _neck.End(w, slew, af); _head.End(w, slew, af); _lShoulder.End(w, slew, af); _rShoulder.End(w, slew, af);
            _spine.End(w, slew, af); _chest.End(w, slew, af); _lUpperArm.End(w, slew, af); _rUpperArm.End(w, slew, af); _lLowerArm.End(w, slew, af); _rLowerArm.End(w, slew, af); _lHandB.End(w, slew, af); _rHandB.End(w, slew, af);
            Vector3 hipsParentUp = _hips.parent != null ? _hips.parent.InverseTransformDirection(transform.up) : Vector3.up;
            Vector3 hipsParentRight = _hips.parent != null ? _hips.parent.InverseTransformDirection(transform.right) : Vector3.right;
            float com = Mathf.Clamp(la.x * p.bodyWeightComShift, -p.bodyWeightMaxComShift, p.bodyWeightMaxComShift);
            _comShift = Mathf.SmoothDamp(_comShift, com, ref _comShiftVel, p.bodyWeightPelvisSmooth, Mathf.Infinity, dt);
            float hw = _enabledWeight * S;
            Vector3 hipsParentFwd = _hips.parent != null ? _hips.parent.InverseTransformDirection(transform.forward) : Vector3.forward;
            Vector3 hipOff = Vector3.ClampMagnitude(hipsParentUp * (_hipDrop * hw) + hipsParentRight * (_comShift * w * _aimHip) + hipsParentFwd * (_recoilHip * hardW)
                + (hipsParentRight * _shove.x + hipsParentFwd * _shove.z) * (S * _hardGate * _aimHip) - hipsParentUp * (_plantHipLower * _hardGate), MaxHipMeters);
            if (!Ok(hipOff)) hipOff = Vector3.zero;
            float upC = Vector3.Dot(hipOff, hipsParentUp) / Mathf.Max(1e-6f, hipsParentUp.sqrMagnitude);
            if (upC > 0f) hipOff -= hipsParentUp * upC; // 2026-10-24h: never lift the pelvis
            _hipsWritten = _hipsBase + hipOff;
            _hips.localPosition = _hipsWritten;
            FootPlant(p, dt, vel, blocked);
        }

        private void UpdateSwingSpring(DM_ClimbDashProfile p, float dt, bool blocked, float S)
        {
            if (_hand == null) return;
            Quaternion hr = _hand.rotation;
            Vector3 omega = Vector3.zero;
            if (_hasHand)
            {
                (hr * Quaternion.Inverse(_lastHandRot)).ToAngleAxis(out float ang, out Vector3 axis);
                if (ang > 180f) ang -= 360f;
                if (!float.IsNaN(axis.x)) omega = transform.InverseTransformDirection(axis * (ang / dt));
            }
            _lastHandRot = hr; _hasHand = true;

            if (SimActive) omega = Sim.HandOmegaDeg;
            bool attacking = !blocked && (SimActive ? Sim.Attacking : (_shooter != null && _shooter.isAttacking));
            _swingWeight = Mathf.MoveTowards(_swingWeight, attacking ? 1f : 0f, dt / 0.12f);
            bool ww = p.bodyWeightWeaponEnabled;
            float gain = ww ? p.bodyWeightWeaponSwingGain * _ww : 1f;
            float m = p.bodyWeightSwingMaxDegrees * Mathf.Max(1f, gain); // explosion guard, not a look clamp
            // Hit-stop: the hand freezes, so omega -> 0 used to yank the torso back to neutral at the exact moment of
            // contact (round-1 reason the swing read as "nothing"). Hold the pre-contact target, and jolt on contact.
            bool hold = SimActive ? Sim.HitStop : Project.Combat.DMMeleeHitStop.PlayerHoldActive;
            Vector3 target;
            if (hold && attacking) target = _heldTarget;
            else
            {
                target = omega * (p.bodyWeightSwingAmount * 0.01f * gain * _swingWeight);
                if (ww && _hasHand)
                {
                    Vector3 rawAlpha = (omega - _lastOmega) / dt;
                    _alpha = Vector3.SmoothDamp(_alpha, rawAlpha, ref _alphaVel, 0.05f, Mathf.Infinity, dt);
                    // Anticipation: while the hand is spinning up, the torso counter-twists (windup), then the spring releases.
                    if (Vector3.Dot(_alpha, omega) > 0f)
                        target -= _alpha * (p.bodyWeightWeaponWindup * 0.001f * _ww * _swingWeight);
                }
                target = Vector3.ClampMagnitude(target, m);
                _heldTarget = target;
            }
            _lastOmega = omega;
            if (ww && hold && !_wasHold && attacking)
            {
                Vector3 dir = _swing.sqrMagnitude > 1e-4f ? _swing.normalized : Vector3.up;
                _swingVel -= dir * (p.bodyWeightWeaponImpactJolt * _ww * 12f);
                _hipDropVel -= p.bodyWeightWeaponImpactHipDrop * _ww * 10f;
                _recoilVel.x += p.bodyWeightWeaponImpactJolt * 0.5f * _ww * Mathf.Sqrt(p.bodyWeightWeaponRecoilStiffness);
            }
            _wasHold = hold;
            float k = p.bodyWeightSwingStiffness;
            Spring(ref _swing, ref _swingVel, k, p.bodyWeightSwingDamping, dt, GuardDeg, target);
            if (_swing.sqrMagnitude < 1e-6f) return;

            // Round-1 bug: this weight was always 1 (ignored gates). Now follows the gated weight and the intensity.
            float w = Mathf.Max(_weight, _swingWeight * 0.5f) * S;
            Vector3 torso = new Vector3(_swing.x * 0.5f, _swing.y, _swing.z * 0.3f) * w;
            float pull = p.bodyWeightWeaponEnabled ? p.bodyWeightWeaponHipPull : 0.35f;
            // 2026-10-23k: swing offsets are character-frame (omega is root-local); convert per bone instead of writing
            // bone-local euler directly (Kade mixamorig bones are rotated ~90 deg vs v7 -> twist read as a sideways lean).
            SetChar(ref _pelvis, new Vector3(0f, _swing.y * pull * w, 0f)); // swing pulls the hips round
            SetChar(ref _spine, torso * 0.35f);
            SetChar(ref _chest, torso * 0.65f);
            SetChar(ref _rUpperArm, new Vector3(_swing.x, _swing.y * 0.5f, _swing.z) * (0.4f * w));
        }

        private float WeaponWeight(DM_ClimbDashProfile p)
        {
            if (_shooter == null) return p.bodyWeightWeaponUnarmed;
            if (_shooter.shooterManager != null && _shooter.shooterManager.rWeapon != null && _shooter.shooterManager.rWeapon.gameObject.activeInHierarchy)
                return p.bodyWeightWeaponRanged;
            bool armed = _melee != null && _melee.rightWeapon != null;
            float w = armed ? p.bodyWeightWeaponMelee : p.bodyWeightWeaponUnarmed;
            var pio = _shooter as Project.Player.Invector.PioneerShooterMeleeInput;
            if (pio != null && (pio.IsStrongMeleeDamageActive || pio.IsChargedStrongSwingActive)) w *= p.bodyWeightWeaponHeavyMult;
            return w;
        }

        private void OnShot() { _pendingShots++; }

        private void UpdateWeaponEvents(DM_ClimbDashProfile p)
        {
            _ww = WeaponWeight(p);
            vShooterWeapon sw = _shooter != null && _shooter.shooterManager != null ? _shooter.shooterManager.rWeapon : null;
            if (sw != _shotWeapon)
            {
                if (_shotWeapon != null) _shotWeapon.onShot.RemoveListener(OnShot);
                _shotWeapon = sw;
                if (_shotWeapon != null) _shotWeapon.onShot.AddListener(OnShot);
            }
            // Draw / holster: the hand's weapon reference changed (ranged or melee).
            Object cur = sw != null && sw.gameObject.activeInHierarchy ? (Object)sw
                       : (_melee != null ? _melee.rightWeapon : null);
            if (_hasWeaponRef && cur != _lastWeapon && p.bodyWeightWeaponEnabled)
            {
                DrawImpulse(p, Mathf.Max(0.3f, cur != null ? WeaponWeight(p) : (_lastWeapon != null ? p.bodyWeightWeaponMelee : 0.6f)));
            }
            _lastWeapon = cur; _hasWeaponRef = true;
        }

        private void DrawImpulse(DM_ClimbDashProfile p, float wt)
        {
            float sq = Mathf.Sqrt(p.bodyWeightWeaponRecoilStiffness);
            _recoilVel.x += p.bodyWeightWeaponDrawDip * wt * sq;
            _hipDropVel -= p.bodyWeightWeaponDrawHipDrop * wt * 10f;
            _rUpperArm.OffVel += new Vector3(p.bodyWeightWeaponDrawDip * wt * 6f, 0f, 0f);
        }

        // ---------------- 2026-10-20w stage 3 ----------------
        private void OnEnable() { Project.Combat.CombatEvents.PlayerDamaged += OnPlayerDamaged; }
        private void OnDisable() { Project.Combat.CombatEvents.PlayerDamaged -= OnPlayerDamaged; }

        private void OnPlayerDamaged(GameObject player, GameObject source, float amount)
        {
            if (player == null || this == null) return;
            if (player != gameObject && !player.transform.IsChildOf(transform) && !transform.IsChildOf(player.transform)) return;
            Vector3 push = source != null ? transform.position - source.transform.position : -transform.forward;
            ApplyHit(push, amount, 0f);
        }

        /// <summary>Queue a hit reaction. worldPush = direction the body is pushed (away from the attacker).</summary>
        public void ApplyHit(Vector3 worldPush, float amount, float poise)
        {
            var p = Prof;
            if (p == null || !p.bodyWeightEnabled || !p.bodyWeightHitEnabled) return;
            worldPush.y = 0f;
            if (!Ok(worldPush) || worldPush.sqrMagnitude < 1e-6f) worldPush = -transform.forward;
            if (!Ok(new Vector3(amount, poise, 0f))) return;
            Vector3 d = transform.InverseTransformDirection(worldPush.normalized);
            float lo = Mathf.Min(p.bodyWeightHitMinScale, p.bodyWeightHitMaxScale);
            float s = Mathf.Clamp((Mathf.Max(0f, amount) + Mathf.Max(0f, poise) * p.bodyWeightHitPoiseWeight) / Mathf.Max(1f, p.bodyWeightHitDamageRef), lo, p.bodyWeightHitMaxScale);
            _hitPending = Vector3.ClampMagnitude(_hitPending + d * s, p.bodyWeightHitMaxScale * 1.5f); // same-frame hits add, capped
        }

        /// <summary>Editor harness: a ranged shot / a draw-holster event.</summary>
        public void SimShot() { _pendingShots++; }
        public void SimDraw(float weight) { var p = Prof; if (p != null && p.bodyWeightWeaponEnabled) DrawImpulse(p, weight); }

        // Character-space euler (x pitch about right, y yaw, z roll about forward) added to a bone's extra, in its local frame.
        private void SetChar(ref Bone b, Vector3 e) { b.Extra = Vector3.zero; AddChar(ref b, e); }

        private void AddChar(ref Bone b, Vector3 e)
        {
            if (b.T == null || !Ok(e)) return;
            b.Extra += b.T.InverseTransformDirection(transform.right) * e.x + b.T.InverseTransformDirection(transform.up) * e.y + b.T.InverseTransformDirection(transform.forward) * e.z;
        }

        private static int Substeps(float dt, float k, float zeta) => Mathf.Clamp(Mathf.CeilToInt(dt * Mathf.Sqrt(k) * (1f + 2f * Mathf.Max(0f, zeta)) / 0.5f), 1, 24);

        private static void SpringF(ref float x, ref float v, float target, float k, float zeta, float dt)
        {
            k = Mathf.Max(1f, k);
            float c = Mathf.Max(0f, zeta) * 2f * Mathf.Sqrt(k);
            int n = Substeps(dt, k, zeta); float h = dt / n;
            for (int i = 0; i < n; i++) { v += (k * (target - x) - c * v) * h; x += v * h; }
            if (float.IsNaN(x) || float.IsNaN(v) || float.IsInfinity(x) || Mathf.Abs(x) > GuardMeters) { x = 0f; v = 0f; }
        }

        private static bool Spring(ref Vector3 x, ref Vector3 v, float k, float zeta, float dt, float guard) => Spring(ref x, ref v, k, zeta, dt, guard, Vector3.zero);

        private static bool Spring(ref Vector3 x, ref Vector3 v, float k, float zeta, float dt, float guard, Vector3 target)
        {
            k = Mathf.Max(1f, k);
            float c = Mathf.Max(0f, zeta) * 2f * Mathf.Sqrt(k);
            // semi-implicit, sub-stepped so very stiff / very low damping settings stay stable at any frame time
            int n = Substeps(dt, k, zeta);
            float h = dt / n;
            for (int i = 0; i < n; i++) { v += (k * (target - x) - c * v) * h; x += v * h; }
            if (!Ok(x) || !Ok(v) || x.sqrMagnitude > guard * guard) { x = Vector3.zero; v = Vector3.zero; return false; }
            return true;
        }

        private void HitAndBreath(DM_ClimbDashProfile p, float dt, float S, bool aiming)
        {
            float k = p.bodyWeightHitRecovery, sq = Mathf.Sqrt(Mathf.Max(1f, k));
            if (_hitPending.sqrMagnitude > 0f)
            {
                Vector3 d = _hitPending; _hitPending = Vector3.zero;
                Vector3 bend = new Vector3(d.z, 0f, -d.x); // pushed back -> pitch back; pushed right -> top tilts right
                _hitTrunkVel += bend * (p.bodyWeightHitImpulse * sq * 1.4f);
                _shoveVel += new Vector3(d.x, 0f, d.z) * (p.bodyWeightHitHipShove * sq * 1.4f);
                float kh = k * 1.5f;
                _hitHeadVel -= bend * (p.bodyWeightHitHeadWhip * Mathf.Sqrt(kh) * 1.4f); // head lags first, then whips past
            }
            Spring(ref _hitTrunk, ref _hitTrunkVel, k, p.bodyWeightHitDamping, dt, GuardDeg);
            Spring(ref _shove, ref _shoveVel, k, p.bodyWeightHitDamping, dt, GuardMeters);
            Spring(ref _hitHead, ref _hitHeadVel, k * 1.5f, p.bodyWeightHitDamping * 0.6f, dt, GuardDeg);
            if (S > 0f && p.bodyWeightHitEnabled)
            {
                AddChar(ref _spine, _hitTrunk * (0.4f * S)); AddChar(ref _chest, _hitTrunk * (0.6f * S));
                AddChar(ref _neck, _hitHead * (0.4f * S)); AddChar(ref _head, _hitHead * (0.6f * S));
            }

            bool exert = SimActive ? Sim.Exerting : ((_motor != null && _motor.isSprinting) || (_jet != null && _jet.IsBoostingNow));
            float tau = exert ? Mathf.Max(0.05f, p.bodyWeightBreathExertRise) / 3f : Mathf.Max(0.05f, p.bodyWeightBreathExertDecay);
            _exert = Mathf.Clamp01(_exert + ((exert ? 1f : 0f) - _exert) * (1f - Mathf.Exp(-dt / tau)));
            if (!p.bodyWeightBreathEnabled || S <= 0f) return;
            float rate = Mathf.Clamp(p.bodyWeightBreathRate + p.bodyWeightBreathExertRate * _exert, 0.01f, 5f);
            _breathPhase = Mathf.Repeat(_breathPhase + rate * dt, 1f);
            float br = 0.5f - 0.5f * Mathf.Cos(_breathPhase * 2f * Mathf.PI);
            float bw = S * (aiming ? p.bodyWeightBreathAimScale : 1f) * br;
            float depth = (p.bodyWeightBreathDepth + p.bodyWeightBreathExertDepth * _exert) * bw;
            AddChar(ref _chest, new Vector3(-depth * 0.7f, 0f, 0f)); // chest rises (extends back)
            AddChar(ref _spine, new Vector3(-depth * 0.3f, 0f, 0f));
            float sh = p.bodyWeightBreathShoulder * (1f + _exert) * bw;
            AddChar(ref _rShoulder, new Vector3(0f, 0f, sh));
            AddChar(ref _lShoulder, new Vector3(0f, 0f, -sh));
        }

        private void FootPlant(DM_ClimbDashProfile p, float dt, Vector3 vel, bool blocked)
        {
            bool can = p.bodyWeightEnabled && p.bodyWeightFootPlantEnabled && !blocked && _hardGate > 0.5f
                       && _lThigh.T != null && _lKnee.T != null && _lFoot.T != null && _rThigh.T != null && _rKnee.T != null && _rFoot.T != null;
            float spd = new Vector2(vel.x, vel.z).magnitude;
            bool want = can && spd < p.bodyWeightFootPlantMaxSpeed;
            float need = 0f;
            if (_lFoot.T != null && _lThigh.T != null && _lKnee.T != null) PlantFoot(ref _lPlant, _lThigh.T, _lKnee.T, _lFoot.T, p, dt, want, ref need);
            if (_rFoot.T != null && _rThigh.T != null && _rKnee.T != null) PlantFoot(ref _rPlant, _rThigh.T, _rKnee.T, _rFoot.T, p, dt, want, ref need);
            _plantHipLower = Mathf.MoveTowards(_plantHipLower, can ? Mathf.Min(need, p.bodyWeightFootPlantHipLower) : 0f, dt * 0.4f);
            _lThigh.Commit(); _lKnee.Commit(); _lFoot.Commit(); _rThigh.Commit(); _rKnee.Commit(); _rFoot.Commit();
        }

        private void PlantFoot(ref Plant pl, Transform th, Transform kn, Transform ft, DM_ClimbDashProfile p, float dt, bool want, ref float need)
        {
            Vector3 fp = ft.position;
            if (!Ok(fp)) { pl = default; return; }
            float h = fp.y - transform.position.y;
            if (!pl.HasLow) { pl.Low = h; pl.HasLow = true; }
            pl.Low = Mathf.Min(pl.Low + dt * 0.05f, h); // lowest recent pose, slowly forgotten
            bool low = h < pl.Low + p.bodyWeightFootPlantHeight;
            if (want && low && !pl.On && pl.W <= 0f) { pl.On = true; pl.Pos = fp; }
            Vector3 d = pl.Pos - fp; d.y = 0f;
            if (pl.On && (!want || !low)) pl.On = false;
            // 2026-10-20w (chaos pass): standing still kept a dragged anchor (and the hip lowering it caused) forever -> ease back to the pose.
            if (pl.On && new Vector2(_lastVel.x, _lastVel.z).sqrMagnitude < 0.01f)
            {
                Vector3 back = Vector3.Lerp(pl.Pos, new Vector3(fp.x, pl.Pos.y, fp.z), 1f - Mathf.Exp(-dt / 0.4f));
                pl.Pos = back;
            }
            float maxC = p.bodyWeightFootPlantMaxCorrection;
            if (d.magnitude > maxC) { Vector3 n = d.normalized * maxC; pl.Pos = new Vector3(fp.x + n.x, pl.Pos.y, fp.z + n.z); } // 2026-10-20w: drag the anchor at the cap (no release pop)
            pl.W = Mathf.MoveTowards(pl.W, pl.On ? 1f : 0f, dt / Mathf.Max(0.01f, p.bodyWeightFootPlantBlend));
            pl.Err = 0f; pl.Over = 0f;
            if (pl.W <= 0f) return;
            Vector3 goal = Vector3.Lerp(fp, new Vector3(pl.Pos.x, fp.y, pl.Pos.z), pl.W);
            pl.Over = SolveTwoBone(th, kn, ft, goal, -transform.forward);
            need = Mathf.Max(need, pl.Over);
            pl.Err = pl.Over > 5e-4f ? 0f : (ft.position - goal).magnitude; // solver accuracy when reachable
        }

        /// <summary>Analytic two-bone IK (foot world rotation kept). Returns how far the goal is out of reach (m).</summary>
        private static float SolveTwoBone(Transform a, Transform b, Transform c, Vector3 target, Vector3 bendHint)
        {
            Vector3 ap = a.position, bp = b.position, cp = c.position;
            Quaternion cRot = c.rotation;
            float la = (bp - ap).magnitude, lb = (cp - bp).magnitude;
            if (la < 1e-4f || lb < 1e-4f || !Ok(target)) return 0f;
            float lmin = Mathf.Abs(la - lb) + 1e-3f, lmax = (la + lb) * 0.999f;
            float want = (target - ap).magnitude;
            float over = Mathf.Max(0f, want - lmax);
            float lt = Mathf.Clamp(want, lmin, lmax);
            float cur = Mathf.Clamp((cp - ap).magnitude, lmin, lmax);
            Vector3 axis = Vector3.Cross(bp - ap, cp - bp);
            // 2026-10-20w (harness): a fully straight leg has no bend plane -> knee never bent and the foot missed the goal. Use the knee-forward hint.
            if (axis.sqrMagnitude < 1e-6f * la * lb) axis = Vector3.Cross(bp - ap, bendHint);
            if (axis.sqrMagnitude > 1e-10f)
            {
                float inCur = Mathf.Acos(Mathf.Clamp((la * la + lb * lb - cur * cur) / (2f * la * lb), -1f, 1f));
                float inTgt = Mathf.Acos(Mathf.Clamp((la * la + lb * lb - lt * lt) / (2f * la * lb), -1f, 1f));
                b.rotation = Quaternion.AngleAxis((inCur - inTgt) * Mathf.Rad2Deg, axis.normalized) * b.rotation;
            }
            Vector3 from = c.position - ap, to = target - ap;
            if (from.sqrMagnitude > 1e-8f && to.sqrMagnitude > 1e-8f) a.rotation = Quaternion.FromToRotation(from, to) * a.rotation;
            c.rotation = cRot;
            return over;
        }

        // ---- harness probes (character space: x pitch, y yaw, z roll; degrees) ----
        private ref Bone BoneAt(int i)
        {
            switch (i)
            {
                case 0: return ref _pelvis; case 1: return ref _spine; case 2: return ref _chest; case 3: return ref _neck; case 4: return ref _head;
                case 5: return ref _lUpperArm; case 6: return ref _lLowerArm; case 7: return ref _lHandB; case 8: return ref _rUpperArm; case 9: return ref _rLowerArm;
                case 10: return ref _rHandB; case 11: return ref _lShoulder; case 12: return ref _rShoulder; case 13: return ref _lThigh; case 14: return ref _lKnee;
                case 15: return ref _lFoot; case 16: return ref _rThigh; case 17: return ref _rKnee; default: return ref _rFoot;
            }
        }
        public const int ProbeBoneCount = 19;
        public Vector3 ProbeBone(int i)
        {
            ref Bone b = ref BoneAt(i);
            if (b.T == null || !b.Has) return Vector3.zero;
            (Quaternion.Inverse(b.Base) * b.T.localRotation).ToAngleAxis(out float a, out Vector3 ax);
            if (a > 180f) a -= 360f;
            if (!Ok(ax) || Mathf.Abs(a) < 1e-4f) return Vector3.zero;
            Quaternion baseWorld = b.T.parent != null ? b.T.parent.rotation * b.Base : b.Base;
            return transform.InverseTransformDirection(baseWorld * ax) * a;
        }
        /// <summary>Offset rotation in the bone's own frame (deg, axis*angle): parent motion does not leak in (slew checks).</summary>
        public Vector3 ProbeBoneLocal(int i)
        {
            ref Bone b = ref BoneAt(i);
            if (b.T == null || !b.Has) return Vector3.zero;
            (Quaternion.Inverse(b.Base) * b.T.localRotation).ToAngleAxis(out float a, out Vector3 ax);
            if (a > 180f) a -= 360f;
            return Ok(ax) ? ax * a : Vector3.zero;
        }
        public Vector3 ProbeHips => _hips != null && _hips.parent != null ? transform.InverseTransformDirection(_hips.parent.TransformVector(_hipsWritten - _hipsBase)) : Vector3.zero;
        public Vector2 ProbePlantWeight => new Vector2(_lPlant.W, _rPlant.W);
        public float ProbePlantErr => Mathf.Max(_lPlant.Err, _rPlant.Err);
        public float ProbePlantOver => Mathf.Max(_lPlant.Over, _rPlant.Over);
        public float ProbeExert => _exert;
        public float ProbeBreathRate(DM_ClimbDashProfile p) => p.bodyWeightBreathRate + p.bodyWeightBreathExertRate * _exert;
        public string ProbeGate => _gate;

        private void UpdateRecoil(DM_ClimbDashProfile p, float dt, float S)
        {
            float k = p.bodyWeightWeaponRecoilStiffness, sq = Mathf.Sqrt(k);
            float recDamp = p.bodyWeightWeaponRecoilDamping;
            if (_aimingNow)
            {
                // 2026-10-20y: aimed recoil = impulse on the shot frame only, stiff + critically damped settle (no wobble tail).
                k *= p.bodyWeightAimRecoilSettle * p.bodyWeightAimRecoilSettle;
                recDamp = Mathf.Max(recDamp, 1f);
            }
            if (_pendingShots > 0)
            {
                if (p.bodyWeightWeaponEnabled)
                {
                    float n = Mathf.Min(_pendingShots, 3) * (_aimingNow ? p.bodyWeightAimRecoilScale : 1f); // peak = kick x scale / settle
                    _recoilVel.x -= p.bodyWeightWeaponRecoilKick * _ww * sq * n; // spine kicks back
                    _recoilHipVel -= p.bodyWeightWeaponRecoilHipPush * _ww * sq * n;
                    _recoilVel.z += Random.Range(-0.3f, 0.3f) * p.bodyWeightWeaponRecoilKick * _ww * sq;
                }
                _pendingShots = 0;
            }
            Spring(ref _recoil, ref _recoilVel, k, recDamp, dt, GuardDeg, Vector3.zero);
            SpringF(ref _recoilHip, ref _recoilHipVel, 0f, k, recDamp, dt);
            if (!Ok(_recoil) || _recoil.sqrMagnitude > GuardDeg * GuardDeg || float.IsNaN(_recoilHip) || Mathf.Abs(_recoilHip) > GuardMeters)
            { _recoil = Vector3.zero; _recoilVel = Vector3.zero; _recoilHip = 0f; _recoilHipVel = 0f; }
            if (!p.bodyWeightWeaponEnabled || S <= 0f) return;
            // Spine absorbs; chest counter-rotates ~85% so shoulder orientation (aim) is nearly preserved; head stays level.
            Vector3 r = _recoil * S;
            _spine.Kick += r;
            _chest.Kick -= new Vector3(r.x * 0.85f, 0f, r.z * 0.85f);
            _pelvis.Kick += new Vector3(-r.x * 0.25f, 0f, 0f);
        }

        private void OnDestroy()
        {
            if (_shotWeapon != null) _shotWeapon.onShot.RemoveListener(OnShot);
        }

        private static void DriveArm(ref Bone b, Vector3 worldInertia, float whip, float smooth, DM_ClimbDashProfile p, float dt)
        {
            if (b.T == null) return;
            Vector3 local = b.T.InverseTransformDirection(worldInertia);
            float m = Mathf.Min(p.bodyWeightMaxArmDegrees * Mathf.Max(1f, whip), 85f); // 2026-10-20w: clamp grows with whip so distal links can trail more (was a flat cap)
            Vector3 t = new Vector3(
                Mathf.Clamp(-local.y * whip, -m, m),
                Mathf.Clamp(local.x * whip * 0.45f, -m * 0.45f, m * 0.45f),
                Mathf.Clamp(local.z * whip, -m, m));
            b.Drive(t, smooth, dt);
        }

        private void OnGUI()
        {
            var p = DM_ClimbDashProfile.Live;
            if (p == null || !p.bodyWeightDebugReadout) return;
            GUI.Label(new Rect(10, 300, 700, 215),
                $"[BodyWeight] enabled={p.bodyWeightEnabled} scale={p.BodyWeightScale:0.##} ({p.bodyWeightPreset}) gate={_gate}\n" +
                $"weight={_weight:0.00} hipW={_enabledWeight:0.00} swingW={_swingWeight:0.00}\n" +
                $"accel(local)={transform.InverseTransformDirection(_accel):F1} yawRate={_yawRate:0}\n" +
                $"trunk={_trunk:F1} pelvisYaw={_pelvis.Off.y:0.0} chest={_chest.Off:F1}\n" +
                $"lArm={_lUpperArm.Off:F1} lElbow={_lLowerArm.Off:F1} lWrist={_lHandB.Off:F1}\n" +
                $"hipDrop={_hipDrop * 100f:0.0}cm com={_comShift * 100f:0.0}cm swing={_swing:F1}\n" +
                $"weapon w={_ww:0.00} attacking={(_shooter != null && _shooter.isAttacking)} hitstop={Project.Combat.DMMeleeHitStop.PlayerHoldActive} recoil={_recoil:F1} hipPush={_recoilHip * 100f:0.0}cm\n" +
                $"hit={_hitTrunk:F1} head={_hitHead:F1} shove={_shove * 100f:F1}cm exert={_exert:0.00} plant L{_lPlant.W:0.0} R{_rPlant.W:0.0} hipLower={_plantHipLower * 100f:0.0}cm");
        }
    }
}

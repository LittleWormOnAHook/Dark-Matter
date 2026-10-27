using System.Collections.Generic;
using Invector.vCamera;
using Invector.vCharacterController;
using Project.AI;
using Project.Features.Locomotion;
using Project.Survival.Exposure;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Project.Combat
{
    /// <summary>2026-10-26a: plays one AnimationClip over an Animator through a weighted PlayableGraph output (fades in/out over the controller).</summary>
    public sealed class DMClipOverlay
    {
        private PlayableGraph _g;
        private AnimationPlayableOutput _o;
        private float _t, _len, _in, _out, _speed, _stopAt = -1f, _stopFade;
        public bool Done { get; private set; }
        public float Time01 => _len > 0f ? Mathf.Clamp01(_t / _len) : 1f;
        public float Elapsed => _t;
        public float Weight { get; private set; }

        public static DMClipOverlay Play(Animator a, AnimationClip clip, float fadeIn, float fadeOut, float maxSeconds, float speed, string name)
        {
            if (a == null || clip == null) return null;
            var o = new DMClipOverlay();
            o._g = PlayableGraph.Create("DM_" + name);
            o._g.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            var c = AnimationClipPlayable.Create(o._g, clip);
            c.SetApplyFootIK(true);
            c.SetSpeed(Mathf.Max(0.05f, speed));
            o._o = AnimationPlayableOutput.Create(o._g, name, a);
            o._o.SetSourcePlayable(c);
            o._o.SetWeight(0f);
            o._speed = Mathf.Max(0.05f, speed);
            o._len = Mathf.Min(clip.length / o._speed, Mathf.Max(0.1f, maxSeconds));
            o._in = Mathf.Max(0f, fadeIn); o._out = Mathf.Max(0f, fadeOut);
            o._g.Play();
            return o;
        }

        /// <summary>Starts an early fade-out (e.g. taunt cancelled).</summary>
        public void Cancel(float fade)
        {
            if (Done || _stopAt >= 0f) return;
            _stopAt = _t; _stopFade = Mathf.Max(0.01f, fade);
        }

        public void Tick(float dt)
        {
            if (Done) return;
            if (!_g.IsValid()) { Done = true; return; }
            _t += dt;
            float w = _in > 0f ? Mathf.Clamp01(_t / _in) : 1f;
            if (_out > 0f) w = Mathf.Min(w, Mathf.Clamp01((_len - _t) / _out));
            if (_stopAt >= 0f) w = Mathf.Min(w, Weight - dt / _stopFade);
            Weight = Mathf.Clamp01(w);
            _o.SetWeight(Weight);
            if (_t >= _len || (_stopAt >= 0f && Weight <= 0f)) Stop();
        }

        public void Stop()
        {
            if (_g.IsValid()) _g.Destroy();
            Done = true; Weight = 0f;
        }
    }

    /// <summary>
    /// 2026-10-26a Slice 6 runtime: finishers (DMFinisher), enemy taunts in the Director hold ring, group camera pull-back.
    /// Hidden DontDestroyOnLoad driver created on first scene load.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class DMSlice6Runtime : MonoBehaviour
    {
        public static int StatTaunts, StatTauntCancels, StatFinishers, StatFinisherKills, StatFinisherRejects;
        public static string LastFinisherReject = "";
        public static float CameraPullback => s_inst != null ? s_inst._pull : 0f;
        public static float CameraPullbackTarget => s_inst != null ? s_inst._pullTarget : 0f;
        public static int ActiveTaunts => s_inst != null ? s_inst._taunts.Count : 0;
        public static bool FinisherActive => s_inst != null && s_inst._fin != null;

        // 2026-10-26e: finisher-available cue (nearest eligible enemy). Throttled ProbeFinisher.
        // 2026-10-26k: _eligibleTarget drives speed/lights even when the cue visual is disabled.
        private EnemyHealth _cueTarget;
        private EnemyHealth _eligibleTarget;
        private float _cueNext;
        private ExposureHazardIndicator[] _jetpackLights; // Rad, Heat, Cold, Mix
        private float _jetpackBindAt;
        private bool _jetpackDriving;
        private static readonly Color s_DefaultBarRed = new Color(235f / 255f, 46f / 255f, 36f / 255f, 1f);

        public static EnemyHealth CueTarget => s_inst != null ? s_inst._cueTarget : null;
        public static bool CueActive => s_inst != null && s_inst._cueTarget != null;
        public static bool IsCueTarget(EnemyHealth h) => s_inst != null && h != null && ReferenceEquals(s_inst._cueTarget, h);

        /// <summary>2026-10-26k: same eligibility as the HP cue (enableFinishers + FindFinisherTarget ok), even if the cue visual is off.</summary>
        public static bool FinisherAvailable => s_inst != null && s_inst._eligibleTarget != null;

        /// <summary>Non-finisher melee animator speed mult while FinisherAvailable (1 when not).</summary>
        public static float OtherMeleeSpeedMult
        {
            get
            {
                if (!FinisherAvailable || FinisherActive) return 1f;
                DM_FinisherTauntProfile p = DM_FinisherTauntProfile.Live;
                return p != null ? Mathf.Clamp(p.finisherAvailableOtherMeleeSpeed, 1f, 1.5f) : 1f;
            }
        }

        /// <summary>Body-weight swing-gain mult while other-melee speed boost is active.</summary>
        public static float OtherMeleeBodySwingMult
        {
            get
            {
                if (!FinisherAvailable || FinisherActive) return 1f;
                DM_FinisherTauntProfile p = DM_FinisherTauntProfile.Live;
                return p != null ? Mathf.Clamp(p.finisherAvailableBodySwingMult, 1f, 1.5f) : 1f;
            }
        }

        /// <summary>2026-10-26l: suppress Use/interact/pickup while a finisher plays, or while the Finisher combo (Use+Attack) is held and eligible.</summary>
        public static bool BlocksWorldUse
        {
            get
            {
                if (FinisherActive) return true;
                if (!FinisherAvailable) return false;
                if (Project.Player.DMPlayerInputActions.WasPressedThisFrame("Finisher")) return true;
                if (Project.Player.DMPlayerInputActions.IsPressed("Finisher")) return true;
                return Project.Player.DMPlayerInputActions.IsPressed("Use")
                    && Project.Player.DMPlayerInputActions.IsPressed("Attack");
            }
        }

        /// <summary>2026-10-26l: dedicated Finisher combo pressed this frame (Use+Attack / Finisher action).</summary>
        public static bool FinisherComboPressedThisFrame()
        {
            if (Project.Player.DMPlayerInputActions.WasPressedThisFrame("Finisher"))
                return true;
            return Project.Player.DMPlayerInputActions.IsPressed("Use")
                && Project.Player.DMPlayerInputActions.WasPressedThisFrame("Attack");
        }

        /// <summary>Brightened + pulsed fill for the cue; returns baseFill unchanged when the cue is off.</summary>
        public static Color EvaluateCueFill(Color baseFill)
        {
            DM_FinisherTauntProfile p = DM_FinisherTauntProfile.Live;
            if (s_inst == null || s_inst._cueTarget == null || p == null || !p.enableFinisherCue) return baseFill;
            Color bright = Color.Lerp(baseFill, p.finisherCueBrightColor, Mathf.Clamp01(p.finisherCueBrightnessBoost));
            if (p.finisherCueBrightnessBoost > 1f)
                bright = Color.Lerp(bright, Color.white, Mathf.Clamp01(p.finisherCueBrightnessBoost - 1f) * 0.5f);
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * p.finisherCuePulseSpeed * Mathf.PI * 2f);
            float amp = Mathf.Clamp01(p.finisherCuePulseDepth);
            Color dim = Color.Lerp(baseFill, bright, 1f - amp * 0.85f);
            Color c = Color.Lerp(dim, bright, wave);
            c.a = baseFill.a;
            return c;
        }

        private void TickFinisherCue(GameObject player, DM_FinisherTauntProfile p)
        {
            if (p == null || !p.enableFinishers)
            {
                _cueTarget = null;
                _eligibleTarget = null;
                return;
            }
            if (Time.time < _cueNext) return;
            float hz = Mathf.Max(1f, p.finisherCueProbeHz);
            _cueNext = Time.time + 1f / hz;
            string why = FindFinisherTarget(player, p, out EnemyHealth t);
            _eligibleTarget = why == null ? t : null;
            _cueTarget = (p.enableFinisherCue && _eligibleTarget != null) ? _eligibleTarget : null;
        }

        private static DMSlice6Runtime s_inst;
        private static readonly List<EnemyHealth> s_buf = new List<EnemyHealth>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { s_inst = null; ResetStats(); }

        public static void ResetStats() { StatTaunts = StatTauntCancels = StatFinishers = StatFinisherKills = StatFinisherRejects = 0; LastFinisherReject = ""; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot() { Ensure(); }

        public static DMSlice6Runtime Ensure()
        {
            if (s_inst != null) return s_inst;
            var go = new GameObject("DM_Slice6Runtime") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            s_inst = go.AddComponent<DMSlice6Runtime>();
            return s_inst;
        }

        // ------------------------------------------------------------------ taunts
        private sealed class Taunt { public EnemyAiController Ai; public Rigidbody Rb; public DMClipOverlay Clip; public Vector3 Pin; }
        private readonly List<Taunt> _taunts = new List<Taunt>();
        private readonly Dictionary<EnemyAiController, float> _tauntNext = new Dictionary<EnemyAiController, float>();
        private float _groupNext, _tauntScan;

        public static bool IsTaunting(EnemyAiController ai)
        {
            if (s_inst == null || ai == null) return false;
            foreach (var t in s_inst._taunts) if (ReferenceEquals(t.Ai, ai)) return true;
            return false;
        }

        /// <summary>Audit / Studio: force a taunt on this enemy (bypasses chance and cooldowns, keeps eligibility).</summary>
        public static bool ForceTaunt(EnemyAiController ai)
        {
            var r = Ensure();
            return r.StartTaunt(ai, DM_FinisherTauntProfile.Live, Time.time);
        }

        private void TickTaunts(GameObject player, DM_FinisherTauntProfile p, float dt)
        {
            for (int i = _taunts.Count - 1; i >= 0; i--)
            {
                Taunt t = _taunts[i];
                bool gone = t.Ai == null || !t.Ai.isActiveAndEnabled;
                if (!gone)
                {
                    var h = t.Ai.GetComponent<EnemyHealth>();
                    var st = t.Ai.EngagementState;
                    bool cancel = (h != null && h.IsDead) || !p.enableTaunts
                        || (p.tauntCancelOnAttackSlot && st.AttackSlot) || st.Role == DMEngagementRole.None;
                    if (cancel && t.Clip != null && !t.Clip.Done) { t.Clip.Cancel(0.12f); StatTauntCancels++; }
                }
                if (gone) t.Clip?.Stop();
                else t.Clip?.Tick(dt);
                if (gone || t.Clip == null || t.Clip.Done) _taunts.RemoveAt(i);
            }

            if (!p.enableTaunts || player == null || p.tauntClips == null || p.tauntClips.Count == 0) return;
            _tauntScan -= dt;
            if (_tauntScan > 0f) return;
            float step = 0.25f; _tauntScan = step;
            float now = Time.time;
            if (now < _groupNext || _taunts.Count >= p.tauntMaxConcurrent) return;
            EnemyHealthSceneRegistry.CopyTo(s_buf);
            for (int i = 0; i < s_buf.Count; i++)
            {
                EnemyHealth h = s_buf[i];
                if (h == null || h.IsDead) continue;
                var ai = h.GetComponent<EnemyAiController>();
                if (!Eligible(ai, player, p, now)) continue;
                if (Random.value > p.tauntChancePerSecond * step) continue;
                if (StartTaunt(ai, p, now)) break;
            }
        }

        private bool Eligible(EnemyAiController ai, GameObject player, DM_FinisherTauntProfile p, float now)
        {
            if (ai == null || !ai.isActiveAndEnabled || player == null) return false;
            var st = ai.EngagementState;
            if (st.Role == DMEngagementRole.None || st.Role != DMEngagementRole.Holder || st.AttackSlot || st.Flanker) return false;
            if (IsTaunting(ai)) return false;
            if (_tauntNext.TryGetValue(ai, out float next) && now < next) return false;
            Vector3 d = ai.transform.position - player.transform.position; d.y = 0f;
            float dist = d.magnitude;
            if (dist < p.tauntMinDistance || dist > p.tauntMaxDistance) return false;
            var rb = ai.GetComponent<Rigidbody>();
            if (rb != null) { Vector3 v = rb.linearVelocity; v.y = 0f; if (v.magnitude > p.tauntMaxMoveSpeed) return false; }
            var a = ai.GetComponent<Animator>();
            return a != null && a.isHuman && a.isActiveAndEnabled;
        }

        private bool StartTaunt(EnemyAiController ai, DM_FinisherTauntProfile p, float now)
        {
            if (ai == null || p.tauntClips == null || p.tauntClips.Count == 0) return false;
            var st = ai.EngagementState;
            if (st.Role == DMEngagementRole.None || st.AttackSlot || IsTaunting(ai)) return false;
            var a = ai.GetComponent<Animator>();
            if (a == null || !a.isHuman) return false;
            AnimationClip clip = p.tauntClips[Random.Range(0, p.tauntClips.Count)];
            if (clip == null) return false;
            var o = DMClipOverlay.Play(a, clip, p.tauntBlendIn, p.tauntBlendOut, p.tauntMaxSeconds, 1f, "Taunt");
            if (o == null) return false;
            _taunts.Add(new Taunt { Ai = ai, Rb = ai.GetComponent<Rigidbody>(), Clip = o, Pin = ai.transform.position });
            _tauntNext[ai] = now + p.tauntPerEnemyCooldown;
            _groupNext = now + p.tauntGroupCooldown;
            StatTaunts++;
            if (p.debugLog) Debug.Log($"[Slice6] taunt {ai.name} ({clip.name})");
            return true;
        }

        // ------------------------------------------------------------------ camera pull-back
        private float _pull, _pullVel, _pullTarget, _base = -1f, _applied, _lowSince = -1f;

        private void TickCamera(GameObject player, DM_FinisherTauntProfile p, float dt)
        {
            vThirdPersonCamera cam = vThirdPersonCamera.instance;
            if (cam == null || player == null) return;
            float target = 0f;
            bool aiming = false;
            var mi = player.GetComponent<Project.Player.Invector.PioneerShooterMeleeInput>();
            if (mi != null) aiming = mi.IsAiming;
            if (p.enableCameraPullback && !aiming
                && DMEnemyEngagementDirector.TryGetGroupDebug(player.transform, out _, out _, out _, out int members, out int slots))
            {
                float threat = members + slots * p.pullbackSlotThreatWeight;
                float lo = p.pullbackMinEnemies, hi = Mathf.Max(lo + 0.5f, p.pullbackFullEnemies);
                if (threat >= lo) target = Mathf.Lerp(0.35f, 1f, Mathf.InverseLerp(lo, hi, threat)) * p.pullbackMaxExtraDistance;
            }
            // hold before returning
            if (target < _pullTarget)
            {
                if (_lowSince < 0f) _lowSince = Time.time;
                if (Time.time - _lowSince < p.pullbackReturnDelay && !aiming) target = _pullTarget;
                else _lowSince = -1f;
            }
            else _lowSince = -1f;
            _pullTarget = target;

            float smooth = target > _pull ? p.pullbackOutSeconds : p.pullbackReturnSeconds;
            _pull = Mathf.SmoothDamp(_pull, target, ref _pullVel, Mathf.Max(0.02f, smooth * 0.5f), Mathf.Infinity, dt);
            if (target <= 0f && _pull < 0.01f) _pull = 0f;

            if (_pull <= 0f && _applied <= 0f) { _base = -1f; return; }
            if (aiming) { _applied = 0f; _base = -1f; _pull = 0f; _pullVel = 0f; return; }
            if (_base < 0f) _base = cam.CurrentZoom;
            // player scrolled while pulled back: keep his new zoom as the base
            if (Mathf.Abs(cam.CurrentZoom - (_base + _applied)) > 0.08f) _base = cam.CurrentZoom - _applied;
            cam.SetZoomTarget(_base + _pull);
            _applied = _pull;
            if (_pull <= 0f) { _applied = 0f; _base = -1f; }
        }

        // ------------------------------------------------------------------ finishers
        private sealed class Fin
        {
            public GameObject Player; public vThirdPersonController Cc; public Rigidbody PRb; public bool SavedLockMove, SavedLockRot;
            public EnemyHealth Enemy; public EnemyAiController Ai; public Rigidbody ERb; public bool SavedKinematic, AiWasEnabled;
            public DMClipOverlay PClip, EClip; public Vector3 PPin, EPin; public Quaternion PRot, ERot; public bool Killed; public float T;
            public DMPlayerBodyWeight Body; // 26i body-weight tune during solo finisher swing
        }
        private Fin _fin;
        private float _finNext;

        public static bool IsFinisherTarget(EnemyHealth h) => s_inst != null && s_inst._fin != null && ReferenceEquals(s_inst._fin.Enemy, h);

        /// <summary>Called on an Attack press with a drawn melee weapon. Returns true if a finisher started (consume the press).</summary>
        public static bool TryStartFinisher(GameObject player)
        {
            var r = Ensure();
            DM_FinisherTauntProfile p = DM_FinisherTauntProfile.Live;
            string why = r.FindFinisherTarget(player, p, out EnemyHealth target);
            if (why == null && target != null) { r.BeginFinisher(player, target, p); return true; }
            if (why != "noTarget") { StatFinisherRejects++; LastFinisherReject = why; }
            return false;
        }

        public static string ProbeFinisher(GameObject player, out EnemyHealth target)
        {
            return Ensure().FindFinisherTarget(player, DM_FinisherTauntProfile.Live, out target);
        }

        private string FindFinisherTarget(GameObject player, DM_FinisherTauntProfile p, out EnemyHealth best)
        {
            best = null;
            if (!p.enableFinishers) return "disabled";
            if (p.finisherPlayerClip == null) return "noClip";
            if (player == null) return "noPlayer";
            if (_fin != null) return "busy";
            if (Time.time < _finNext) return "cooldown";
            var cc = player.GetComponentInParent<vThirdPersonController>();
            if (cc == null || cc.isDead || !cc.isGrounded) return "playerState";
            if (DMMomentumSpin.AnySpinning) return "spinning";
            EnemyHealthSceneRegistry.CopyTo(s_buf);
            Vector3 me = cc.transform.position, fwd = Vector3.ProjectOnPlane(cc.transform.forward, Vector3.up).normalized;
            float bestScore = float.MaxValue;
            for (int i = 0; i < s_buf.Count; i++)
            {
                EnemyHealth h = s_buf[i];
                if (h == null || h.IsDead || !h.isActiveAndEnabled) continue;
                Vector3 to = h.transform.position - me;
                if (Mathf.Abs(to.y) > 1.2f) continue;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist > p.finisherRange) continue;
                float ang = dist < 0.05f ? 0f : Vector3.Angle(fwd, to);
                if (ang > p.finisherMaxAngle) continue;
                bool low = h.MaxHealth > 0f && h.CurrentHealth / h.MaxHealth <= p.finisherLowHpFraction;
                bool staggered = false;
                if (p.enableFinisherOnStagger)
                {
                    var poise = h.GetComponent<CombatPoise>();
                    staggered = poise != null && Time.time - poise.LastInterruptTime <= p.finisherStaggerWindow;
                }
                if (!low && !staggered) continue;
                var ea = h.GetComponent<Animator>();
                if ((ea == null || !ea.isHuman) && !p.finisherAllowNonHumanoid) continue;
                float score = dist + ang * 0.02f;
                if (score < bestScore) { bestScore = score; best = h; }
            }
            return best != null ? null : "noTarget";
        }

        private void BeginFinisher(GameObject player, EnemyHealth h, DM_FinisherTauntProfile p)
        {
            // 2026-10-26i: solo overhead strike -- no paired enemy clip / grab pin. Enemy dies via normal damage.
            var f = new Fin { Player = player, Enemy = h };
            f.Cc = player.GetComponentInParent<vThirdPersonController>();
            f.PRb = f.Cc.GetComponent<Rigidbody>();
            f.SavedLockMove = f.Cc.lockMovement; f.SavedLockRot = f.Cc.lockRotation;
            f.Cc.lockMovement = true; f.Cc.lockRotation = true;
            if (f.PRb != null) f.PRb.linearVelocity = Vector3.zero;

            Vector3 me = f.Cc.transform.position;
            Vector3 to = h.transform.position - me; to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) to = f.Cc.transform.forward;
            to.Normalize();
            f.PRot = Quaternion.LookRotation(to);
            f.PPin = me;
            f.EPin = h.transform.position;
            f.ERot = h.transform.rotation;

            f.Ai = h.GetComponent<EnemyAiController>();
            f.ERb = h.GetComponent<Rigidbody>();
            // Do not disable AI or freeze the enemy RB -- they react / die normally on the kill hit.
            for (int i = _taunts.Count - 1; i >= 0; i--) if (_taunts[i].Ai != null && f.Ai != null && ReferenceEquals(_taunts[i].Ai, f.Ai)) { _taunts[i].Clip?.Stop(); _taunts.RemoveAt(i); }

            var pa = f.Cc.GetComponent<Animator>();
            f.PClip = DMClipOverlay.Play(pa, p.finisherPlayerClip, p.finisherBlendIn, p.finisherBlendOut, p.finisherMaxSeconds, p.finisherPlaySpeed, "FinisherP");
            f.Body = player.GetComponentInParent<DMPlayerBodyWeight>() ?? player.GetComponent<DMPlayerBodyWeight>();
            _fin = f;
            StatFinishers++;
            if (p.debugLog) Debug.Log("[Slice6] finisher on " + h.name);
        }

        private void TickFinisher(DM_FinisherTauntProfile p, float dt)
        {
            Fin f = _fin;
            if (f == null) return;
            f.T += dt;
            f.PClip?.Tick(dt);
            // Body-weight: peak pitch/hip around mid-swing (before kill frame).
            if (f.Body != null && (p.finisherBodyPitchDeg > 0f || p.finisherBodyHipDrop > 0f))
            {
                float u = f.PClip != null ? f.PClip.Time01 : 0f;
                float env = u < p.finisherKillNormalizedTime
                    ? Mathf.SmoothStep(0f, 1f, u / Mathf.Max(0.05f, p.finisherKillNormalizedTime))
                    : Mathf.SmoothStep(1f, 0f, (u - p.finisherKillNormalizedTime) / Mathf.Max(0.05f, 1f - p.finisherKillNormalizedTime));
                f.Body.AddJumpAttackImpulse(p.finisherBodyPitchDeg * env, p.finisherBodyHipDrop * env);
            }
            bool playerGone = f.Cc == null || f.Cc.isDead;
            float u01 = f.PClip != null ? f.PClip.Time01 : 1f;
            if (!f.Killed && (u01 >= p.finisherKillNormalizedTime || playerGone || f.Enemy == null || f.Enemy.IsDead))
                KillFinisherTarget(f, p);
            if (playerGone || f.PClip == null || f.PClip.Done || f.T > p.finisherMaxSeconds + 1f)
                EndFinisher(f);
        }

        private void KillFinisherTarget(Fin f, DM_FinisherTauntProfile p)
        {
            // "Killed" flag = finisher damage already applied this swing (enemy may survive).
            f.Killed = true;
            f.EClip?.Stop();
            if (f.Enemy == null || f.Enemy.IsDead) return;
            Vector3 hp = f.Enemy.transform.position + Vector3.up * 1.1f;
            // 2026-10-26l: damage = MaxHealth * percent * multiplier. No forced kill / no charged overkill.
            float pct = Mathf.Clamp(p.finisherDamageMaxHealthPercent, 0.01f, 1f);
            float mult = Mathf.Clamp(p.finisherDamageMultiplier, 0.5f, 10f);
            float maxHp = Mathf.Max(1f, f.Enemy.MaxHealth);
            float dmg = Mathf.Max(1f, maxHp * pct * mult);
            DamageInfo info = DamageInfo.FromHit(dmg, p.finisherCountsAsGruesome, f.Player, hp);
            if (f.Player != null) DMPlayerMomentum.NotifyPlayerMeleeHit(f.Enemy, f.Player, false);
            CombatDamageApplicator.ApplyToDamageable(f.Enemy, f.Enemy.gameObject, in info);
            if (f.Enemy != null && f.Enemy.IsDead) StatFinisherKills++;
            if (p.finisherShakeAmplitude > 0f) DMCombatCameraShake.Play(p.finisherShakeAmplitude, 0.2f);
        }

        private void EndFinisher(Fin f)
        {
            f.PClip?.Stop(); f.EClip?.Stop();
            if (f.Cc != null) { f.Cc.lockMovement = f.SavedLockMove; f.Cc.lockRotation = f.SavedLockRot; }
            _fin = null;
            _finNext = Time.time + DM_FinisherTauntProfile.Live.finisherCooldown;
        }

        // ------------------------------------------------------------------ loop
        private void Update()
        {
            DM_FinisherTauntProfile p = DM_FinisherTauntProfile.Live;
            float dt = Time.deltaTime;
            GameObject player = Project.Core.PlayerLocator.FindPlayerObject();
            TickFinisher(p, dt);
            TickFinisherCue(player, p);
            TickJetpackLights(player, p);
            TickTaunts(player, p, dt);
            TickCamera(player, p, Time.unscaledDeltaTime);
        }

        private void LateUpdate()
        {
            // pin positions so clip root motion / AI steering cannot slide the actors
            if (_fin != null)
            {
                var f = _fin;
                float a = DM_FinisherTauntProfile.Live.finisherAlignSeconds;
                float k = a > 0f ? Mathf.Clamp01(f.T / a) : 1f;
                if (f.Cc != null)
                {
                    Vector3 pp = f.Cc.transform.position; pp.x = f.PPin.x; pp.z = f.PPin.z;
                    f.Cc.transform.SetPositionAndRotation(pp, Quaternion.Slerp(f.Cc.transform.rotation, f.PRot, k));
                    if (f.PRb != null) { Vector3 v = f.PRb.linearVelocity; v.x = v.z = 0f; f.PRb.linearVelocity = v; }
                }
                // 26i: do not pin the enemy (no paired grab). Player still faces the target.
            }
            for (int i = 0; i < _taunts.Count; i++)
            {
                Taunt t = _taunts[i];
                if (t.Ai == null) continue;
                Vector3 pos = t.Ai.transform.position; pos.x = t.Pin.x; pos.z = t.Pin.z;
                t.Ai.transform.position = pos;
                if (t.Rb != null && !t.Rb.isKinematic) { Vector3 v = t.Rb.linearVelocity; v.x = v.z = 0f; t.Rb.linearVelocity = v; }
            }
        }


        // ------------------------------------------------------------------ 2026-10-26k jetpack lights
        private void TickJetpackLights(GameObject player, DM_FinisherTauntProfile p)
        {
            if (p == null) { StopJetpackLights(); return; }
            int mode = Mathf.Clamp(p.jetpackLightMode, 0, 2);
            bool want = mode == 2 || (mode == 1 && FinisherAvailable);
            if (!want) { StopJetpackLights(); return; }
            if (player == null) { StopJetpackLights(); return; }
            EnsureJetpackLights(player);
            if (_jetpackLights == null || _jetpackLights.Length < 4) return;

            float step = Mathf.Max(0.05f, p.jetpackLightStepTime);
            float gap = Mathf.Max(0f, p.jetpackLightLoopGap);
            float onI = Mathf.Max(0f, p.jetpackLightOnIntensity);
            float offI = Mathf.Max(0f, p.jetpackLightOffIntensity);
            float cycle = step * 4f + gap;
            float u = cycle > 0.0001f ? (Time.unscaledTime % cycle) : 0f;
            int lit = u < step * 4f ? Mathf.Clamp(Mathf.FloorToInt(u / step), 0, 3) : -1;

            for (int i = 0; i < 4; i++)
            {
                ExposureHazardIndicator ind = _jetpackLights[i];
                if (ind == null) continue;
                ind.SetExternalDrive(true, i == lit ? onI : offI);
            }
            _jetpackDriving = true;
        }

        private void EnsureJetpackLights(GameObject player)
        {
            if (_jetpackLights != null && _jetpackLights.Length == 4 && Time.time < _jetpackBindAt)
                return;
            _jetpackBindAt = Time.time + 2f;
            var all = player.GetComponentsInChildren<ExposureHazardIndicator>(true);
            ExposureHazardIndicator rad = null, heat = null, cold = null, mix = null;
            for (int i = 0; i < all.Length; i++)
            {
                ExposureHazardIndicator ind = all[i];
                if (ind == null) continue;
                string n = ind.gameObject.name;
                if (ContainsToken(n, "Rad") || ContainsToken(n, "Red")) { if (rad == null) rad = ind; }
                else if (ContainsToken(n, "Heat")) { if (heat == null) heat = ind; }
                else if (ContainsToken(n, "Cold")) { if (cold == null) cold = ind; }
                else if (ContainsToken(n, "Mix")) { if (mix == null) mix = ind; }
            }
            if (rad != null && heat != null && cold != null && mix != null)
                _jetpackLights = new[] { rad, heat, cold, mix };
            else
                _jetpackLights = null;
        }

        private static bool ContainsToken(string name, string token)
        {
            return !string.IsNullOrEmpty(name) && name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void StopJetpackLights()
        {
            if (!_jetpackDriving) return;
            if (_jetpackLights != null)
            {
                for (int i = 0; i < _jetpackLights.Length; i++)
                    if (_jetpackLights[i] != null) _jetpackLights[i].SetExternalDrive(false);
            }
            _jetpackDriving = false;
        }


        private void OnDisable()
        {
            StopJetpackLights();
            foreach (var t in _taunts) t.Clip?.Stop();
            _taunts.Clear();
            if (_fin != null) EndFinisher(_fin);
            if (_applied > 0f && vThirdPersonCamera.instance != null && _base >= 0f) vThirdPersonCamera.instance.SetZoomTarget(_base);
            _applied = 0f; _base = -1f; _pull = 0f;
        }
    }
}

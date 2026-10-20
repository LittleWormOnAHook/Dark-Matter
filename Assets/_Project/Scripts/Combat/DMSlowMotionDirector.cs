using Invector.vMelee;
using Project.AI;
using Project.Core;
using Project.Player.Invector;
using Project.UI;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-19 e: the single owner of gameplay slow motion (Time.timeScale + Time.fixedDeltaTime).
    ///   (a) Charged attack: when the charged swing's hit window opens with a living enemy in reach / cone, rolls
    ///       chargedSlowMoChance (cooldown) and plays an eased slow-mo of chargedSlowMoSeconds (real time).
    ///   (b) Perfect parry: a block press while an enemy in parrySlowMoThreatRadius is in its attack sequence starts the
    ///       "before" half (the parry window then stretches to ~1 s real time); a confirmed parry (NotifyParry) holds the
    ///       "after" half, otherwise it eases out when the before half runs out. Total = parrySlowMoTotalSeconds, split evenly.
    /// Never fights other time owners: menus / loot / pause (GameplayMenuTime, timeScale 0), main menu / boot, audits
    /// (unless slowMoAllowInAudit), dead player. If anyone else writes Time.timeScale while slow-mo runs, it yields at once.
    /// Hit stop is animator-speed based (DMMeleeHitStop) and stacks naturally. Runs on unscaled time; UI Toolkit stays unscaled.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class DMSlowMotionDirector : MonoBehaviour
    {
        private enum Kind { None, Charged, ParryPre, ParryPost }

        public static int StatChargedRolls, StatChargedTriggers, StatParryPre, StatParryConfirmed, StatYields;

        private static DMSlowMotionDirector s_instance;

        private Kind _kind;
        private float _start, _holdUntil, _end, _easeIn, _easeOut, _scale;
        private float _lastWritten = -1f;
        private float _baseFixedDelta = 0.02f;
        private bool _owning;
        private float _chargedReadyAt, _parryReadyAt;
        private bool _lastWindowOpen, _lastBlockHeld;
        private PioneerShooterMeleeInput _melee;
        private float _nextFind;

        public static bool IsActive => s_instance != null && s_instance._kind != Kind.None;
        public static float CurrentScale => s_instance != null && s_instance._owning ? s_instance._lastWritten : 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_instance = null;
            StatChargedRolls = StatChargedTriggers = StatParryPre = StatParryConfirmed = StatYields = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (s_instance != null)
                return;
            var go = new GameObject("DM_SlowMotionDirector") { hideFlags = HideFlags.HideInHierarchy };
            DontDestroyOnLoad(go);
            s_instance = go.AddComponent<DMSlowMotionDirector>();
        }

        /// <summary>PioneerShooterMeleeInput: a block landed inside the parry window.</summary>
        public static void NotifyParry()
        {
            DMSlowMotionDirector d = s_instance;
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (d == null || p == null || !p.enableParrySlowMo)
                return;
            float now = Time.unscaledTime;
            float half = Mathf.Clamp(p.parrySlowMoTotalSeconds, 1f, 3f) * 0.5f;
            if (d._kind == Kind.ParryPre)
            {
                d._kind = Kind.ParryPost;
                d._holdUntil = now + half;
                d._end = d._holdUntil + p.parrySlowMoEaseOutSeconds;
            }
            else if (d._kind == Kind.None && d.CanRun(p) && now >= d._parryReadyAt && Random.value < p.parrySlowMoChance)
            {
                // Parry without a pre-trigger (e.g. no threat detected at press): after half only.
                d.Begin(Kind.ParryPost, p.parrySlowMoTimeScale, p.parrySlowMoEaseInSeconds, half, p.parrySlowMoEaseOutSeconds);
            }
            else
                return;
            d._parryReadyAt = now + p.parrySlowMoCooldownSeconds;
            StatParryConfirmed++;
            Log(p, "parry confirmed");
        }

        private void Update()
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null)
                return;

            // Yield if another owner wrote timeScale (menus, pause, death, loot) or the context forbids slow-mo.
            if (_owning && !Mathf.Approximately(Time.timeScale, _lastWritten))
            {
                StatYields++;
                Stop(restoreScale: false);
            }
            if (_kind != Kind.None && !CanRun(p))
            {
                StatYields++;
                Stop(restoreScale: true);
            }

            PollTriggers(p);

            if (_kind == Kind.None)
                return;

            float now = Time.unscaledTime;
            if (now >= _end)
            {
                Log(p, "end " + _kind);
                Stop(restoreScale: true);
                return;
            }

            if (_kind == Kind.ParryPre && now >= _holdUntil)
            {
                // No parry confirmed in the before half: ease out from here.
                _kind = Kind.Charged; // plain ease-out envelope
                _holdUntil = now;
                _end = now + p.parrySlowMoEaseOutSeconds;
                Log(p, "parry pre expired");
            }

            float w;
            if (now < _start + _easeIn)
                w = Mathf.SmoothStep(0f, 1f, (now - _start) / Mathf.Max(0.01f, _easeIn));
            else if (now <= _holdUntil)
                w = 1f;
            else
                w = 1f - Mathf.SmoothStep(0f, 1f, (now - _holdUntil) / Mathf.Max(0.01f, _end - _holdUntil));
            Write(Mathf.Lerp(1f, _scale, w));
        }

        private void PollTriggers(DM_CombatCoreProfile p)
        {
            if (_melee == null && Time.unscaledTime >= _nextFind)
            {
                _nextFind = Time.unscaledTime + 2f;
                _melee = FindAnyObjectByType<PioneerShooterMeleeInput>();
            }
            if (_melee == null)
                return;

            // (a) charged swing hit window opens.
            bool window = false;
            if (p.enableChargedSlowMo && (_melee.IsChargedStrongSwingActive || _melee.IsStrongMeleeDamageActive))
            {
                vMeleeManager mm = _melee.meleeManager;
                vMeleeWeapon w = mm != null ? mm.rightWeapon : null;
                window = w != null && w.canApplyDamage;
            }
            if (window && !_lastWindowOpen && _kind == Kind.None && Time.unscaledTime >= _chargedReadyAt && CanRun(p) && TargetInReach(p))
            {
                StatChargedRolls++;
                _chargedReadyAt = Time.unscaledTime + p.chargedSlowMoCooldownSeconds;
                if (Random.value < p.chargedSlowMoChance)
                {
                    float total = Mathf.Clamp(p.chargedSlowMoSeconds, 2f, 4f);
                    float hold = Mathf.Max(0f, total - p.chargedSlowMoEaseInSeconds - p.chargedSlowMoEaseOutSeconds);
                    Begin(Kind.Charged, p.chargedSlowMoTimeScale, p.chargedSlowMoEaseInSeconds, hold, p.chargedSlowMoEaseOutSeconds);
                    StatChargedTriggers++;
                    Log(p, "charged slow-mo");
                }
            }
            _lastWindowOpen = window;

            // (b) block press with an incoming enemy attack: start the parry "before" half.
            bool held = _melee.IsBlockInputHeld;
            if (p.enableParrySlowMo && held && !_lastBlockHeld && _kind == Kind.None && Time.unscaledTime >= _parryReadyAt && CanRun(p) && IncomingAttack(p) && Random.value < p.parrySlowMoChance)
            {
                float half = Mathf.Clamp(p.parrySlowMoTotalSeconds, 1f, 3f) * 0.5f;
                Begin(Kind.ParryPre, p.parrySlowMoTimeScale, p.parrySlowMoEaseInSeconds, half, p.parrySlowMoEaseOutSeconds);
                StatParryPre++;
                Log(p, "parry pre");
            }
            _lastBlockHeld = held;
        }

        private void Begin(Kind kind, float scale, float easeIn, float hold, float easeOut)
        {
            float now = Time.unscaledTime;
            if (!_owning)
                _baseFixedDelta = Time.fixedDeltaTime;
            _kind = kind;
            _scale = Mathf.Clamp(scale, 0.05f, 1f);
            _start = now;
            _easeIn = Mathf.Max(0f, easeIn);
            _easeOut = Mathf.Max(0.01f, easeOut);
            _holdUntil = now + _easeIn + Mathf.Max(0f, hold);
            _end = _holdUntil + _easeOut;
            _owning = true;
            Write(1f);
        }

        private void Write(float scale)
        {
            Time.timeScale = scale;
            Time.fixedDeltaTime = _baseFixedDelta * scale;
            _lastWritten = Time.timeScale;
        }

        private void Stop(bool restoreScale)
        {
            if (_owning)
            {
                if (restoreScale && Mathf.Approximately(Time.timeScale, _lastWritten))
                    Time.timeScale = 1f;
                Time.fixedDeltaTime = _baseFixedDelta;
            }
            _owning = false;
            _kind = Kind.None;
            _lastWritten = -1f;
        }

        private bool CanRun(DM_CombatCoreProfile p)
        {
            if (!GameSession.HasStarted || GameplayMenuTime.IsInventoryPaused || GameplayMenuTime.IsMenuSlowMotion || DMDevCommandState.GamePaused)
                return false;
            if (DMDevCommandState.AuditSessionActive && !p.slowMoAllowInAudit)
                return false;
            if (!_owning && !Mathf.Approximately(Time.timeScale, 1f))
                return false; // someone else owns time right now
            if (_melee != null && _melee.cc != null && _melee.cc.isDead)
                return false;
            return true;
        }

        private bool TargetInReach(DM_CombatCoreProfile p)
        {
            Transform t = _melee.transform;
            // Predicted hit only (no whiffs): a living, standing enemy inside blade reach and a narrow cone in front.
            EnemyHealthSceneRegistry.FindNearestLiving(t.position, p.chargedSlowMoReach, out EnemyHealth e);
            if (e == null || e.IsDead)
                return false;
            if (Project.AI.Invector.DMEnemyKnockdown.IsDown(e.gameObject))
                return false;
            Vector3 to = e.transform.position - t.position;
            to.y = 0f;
            return to.sqrMagnitude < 0.01f || Vector3.Angle(Flat(t.forward), to) <= p.chargedSlowMoConeHalfAngle;
        }

        private static readonly System.Collections.Generic.List<EnemyHealth> s_buf = new System.Collections.Generic.List<EnemyHealth>(32);

        private bool IncomingAttack(DM_CombatCoreProfile p)
        {
            Vector3 pos = _melee.transform.position;
            EnemyHealthSceneRegistry.CopyTo(s_buf);
            bool found = false;
            for (int i = 0; i < s_buf.Count && !found; i++)
            {
                EnemyHealth e = s_buf[i];
                if (e == null || e.IsDead)
                    continue;
                Vector3 to = pos - e.transform.position;
                to.y = 0f;
                if (to.magnitude > p.parrySlowMoThreatRadius)
                    continue;
                EnemyAiController ai = e.GetComponent<EnemyAiController>();
                if (ai == null || !ai.IsInAttackSequence)
                    continue;
                found = to.sqrMagnitude < 0.01f || Vector3.Dot(Flat(e.transform.forward).normalized, to.normalized) > 0.5f;
            }
            s_buf.Clear();
            return found;
        }

        private void OnDisable() => Stop(restoreScale: true);

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        private static void Log(DM_CombatCoreProfile p, string msg)
        {
            if (p != null && p.logSlowMo)
                Debug.Log("[SlowMo] " + msg);
        }
    }
}

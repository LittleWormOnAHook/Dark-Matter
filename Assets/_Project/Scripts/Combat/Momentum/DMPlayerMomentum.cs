using System.Collections.Generic;
using Invector.vCharacterController;
using Project.AI;
using Project.Player.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-25s Slice 5 E3: player momentum meter (static, event driven, decay evaluated lazily - no Update).
    /// Gains from player melee hits on enemies (HitApplied), kill bonus, chain multiplier; drains after a delay;
    /// loses a fraction when the player is damaged. Spent by <see cref="DMMomentumSpin"/>.
    /// </summary>
    public static class DMPlayerMomentum
    {
        private static float s_value, s_lastGain = -999f, s_lastEval, s_lastHit = -999f;
        private static int s_chain;
        public static int StatGains, StatKills, StatLosses;
        public static float StatTotalGained;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Reset();
            CombatEvents.HitApplied -= OnHit;
            CombatEvents.HitApplied += OnHit;
            CombatEvents.PlayerDamaged -= OnPlayerDamaged;
            CombatEvents.PlayerDamaged += OnPlayerDamaged;
        }

        public static void Reset()
        {
            s_value = 0f; s_lastGain = -999f; s_lastEval = 0f; s_lastHit = -999f; s_chain = 0;
            StatGains = StatKills = StatLosses = 0; StatTotalGained = 0f;
        }

        public static float Max => Mathf.Max(1f, DM_MomentumProfile.Live.maxMomentum);
        public static float Value { get { Eval(); return s_value; } }
        public static float Normalized => Value / Max;

        private static void Eval()
        {
            DM_MomentumProfile p = DM_MomentumProfile.Live;
            float now = Time.time;
            float from = Mathf.Max(s_lastEval, s_lastGain + p.decayDelaySeconds);
            if (now > from && s_value > 0f)
                s_value = Mathf.Max(0f, s_value - p.decayPerSecond * (now - from));
            s_lastEval = now;
        }

        public static void Add(float amount)
        {
            Eval();
            s_value = Mathf.Clamp(s_value + amount, 0f, Max);
            s_lastGain = Time.time;
            s_lastEval = Time.time;
        }

        public static bool TrySpend(float amount)
        {
            Eval();
            if (s_value + 0.001f < amount) return false;
            s_value = Mathf.Max(0f, s_value - amount);
            return true;
        }

        /// <summary>Studio / audit: fill the meter.</summary>
        public static void DevFill() { Add(Max); }

        private static void OnHit(DamageInfo info, GameObject target)
        {
            DM_MomentumProfile p = DM_MomentumProfile.Live;
            if (!p.enableMomentum || target == null || DMMomentumSpin.AnySpinning)
                return;
            GameObject src = info.Source;
            if (src == null || !CombatPlayerSourceUtility.IsPlayerOrPioneerSource(src))
                return;
            if (CombatPlayerSourceUtility.IsPlayerTurretSource(src))
                return;
            EnemyHealth h = target.GetComponentInParent<EnemyHealth>();
            if (h == null)
                return;
            PioneerShooterMeleeInput mi = src.GetComponentInParent<PioneerShooterMeleeInput>();
            bool charged = info.IsChargedHit || AttackInterruptEvaluator.IsPlayerChargedHit(src, out _);
            bool melee = charged || info.SwingStep > 0 || (mi != null && mi.IsMeleeComboActive);
            if (!melee && !p.rangedHitsGain)
                return;
            Gain(h, charged, info.IsCritical);
        }

        private static int s_lastFrame = -1;
        private static EnemyHealth s_lastTarget;

        /// <summary>2026-10-25s batch fix: player melee on Invector enemies lands through PioneerInvectorDamageReceiver
        /// (no CombatEvents.HitApplied), so the receiver reports here. Deduped per frame + target.</summary>
        public static void NotifyPlayerMeleeHit(EnemyHealth h, GameObject source, bool isCritical)
        {
            DM_MomentumProfile p = DM_MomentumProfile.Live;
            if (!p.enableMomentum || h == null || source == null || DMMomentumSpin.AnySpinning)
                return;
            bool charged = AttackInterruptEvaluator.IsPlayerChargedHit(source, out _);
            Gain(h, charged, isCritical);
        }

        private static void Gain(EnemyHealth h, bool charged, bool crit)
        {
            if (Time.frameCount == s_lastFrame && h == s_lastTarget)
                return;
            s_lastFrame = Time.frameCount; s_lastTarget = h;
            DM_MomentumProfile p = DM_MomentumProfile.Live;
            float now = Time.time;
            s_chain = now - s_lastHit <= p.chainWindowSeconds ? Mathf.Min(s_chain + 1, p.chainMaxSteps) : 0;
            s_lastHit = now;
            float g = charged ? p.gainHeavyHit : p.gainLightHit;
            if (crit) g += p.gainCritBonus;
            g *= 1f + s_chain * p.chainStepMultiplier;
            if (h.IsDead) { g += p.gainKill; StatKills++; }
            Add(g);
            StatGains++; StatTotalGained += g;
            if (p.debugLog) Debug.Log($"[Momentum] +{g:0.0} (chain {s_chain}) -> {s_value:0}/{Max:0}");
        }

        private static void OnPlayerDamaged(GameObject player, GameObject source, float amount)
        {
            DM_MomentumProfile p = DM_MomentumProfile.Live;
            if (!p.enableMomentum || amount <= 0f) return;
            Eval();
            if (s_value <= 0f) return;
            s_value *= 1f - p.lossOnPlayerHitFraction;
            StatLosses++;
        }
    }

    /// <summary>
    /// 2026-10-25s Slice 5 E3: Spin AoE special. Procedural body spin (no new clip, no prefab edit; runtime-added),
    /// owns rotation in FixedUpdate (Invector ControlRotation locked), damages each enemy once as the sweep passes
    /// its bearing. NO input binding yet (ask-before-assign): Studio "Test Spin" button / audit / <see cref="TrySpin"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMMomentumSpin : MonoBehaviour
    {
        public static int StatSpins, StatSpinHits, StatSpinRejects;
        public static string LastReject = "";
        public static float StatLastSpinDegrees;
        public static bool AnySpinning => s_active > 0;
        private static int s_active;

        private static readonly List<EnemyHealth> s_buf = new List<EnemyHealth>(32);
        private readonly HashSet<EnemyHealth> _hit = new HashSet<EnemyHealth>();
        private vThirdPersonController _cc;
        private bool _spinning, _savedLock, _shook;
        private float _t, _startYaw, _dir, _total;

        public bool IsSpinning => _spinning;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { ResetStats(); s_active = 0; }

        public static void ResetStats() { StatHoldSpins = StatChargedSpins = 0; StatSpins = StatSpinHits = StatSpinRejects = 0; LastReject = ""; StatLastSpinDegrees = 0f; }

        public static int StatHoldSpins, StatChargedSpins;

        /// <summary>2026-10-26a: hold Attack with a full meter. Fires once per hold.</summary>
        public static bool TryHoldSpin(GameObject player, float heldSeconds)
        {
            DM_MomentumProfile p = DM_MomentumProfile.Live;
            if (!p.enableSpin || !p.spinOnHoldAttack || heldSeconds < p.spinHoldSeconds) return false;
            if (p.spinCost > 0f && DMPlayerMomentum.Value + 0.001f < p.spinCost) return false;
            if (!TrySpin(player)) return false;
            StatHoldSpins++;
            return true;
        }

        /// <summary>2026-10-26a: charged release with a full meter spins instead of the strong swing.</summary>
        public static bool TryChargedReleaseSpin(GameObject player)
        {
            DM_MomentumProfile p = DM_MomentumProfile.Live;
            if (!p.enableSpin || !p.spinAutoOnChargedRelease) return false;
            if (p.spinCost > 0f && DMPlayerMomentum.Value + 0.001f < p.spinCost) return false;
            if (!TrySpin(player)) return false;
            StatChargedSpins++;
            return true;
        }

        public static bool TrySpin(GameObject player)
        {
            DM_MomentumProfile p = DM_MomentumProfile.Live;
            string why = null;
            vThirdPersonController cc = player != null ? player.GetComponentInParent<vThirdPersonController>() : null;
            if (!p.enableSpin) why = "disabled";
            else if (cc == null) why = "noPlayer";
            else if (cc.isDead) why = "dead";
            if (why == null)
            {
                DMMomentumSpin s = cc.GetComponent<DMMomentumSpin>();
                if (s != null && s._spinning) why = "busy";
                else
                {
                    // 2026-10-25s batch fix: no "combo" reject (IsMeleeComboActive stays true in idle attack-move states); spin owns rotation anyway.
                    if (p.spinCost > 0f && DMPlayerMomentum.Value + 0.001f < p.spinCost) why = "momentum";
                    else
                    {
                        var ss = cc.GetComponent<Project.Survival.SurvivalStats>();
                        if (p.spinStaminaCost > 0f && ss != null && !ss.TryConsumeStamina(p.spinStaminaCost)) why = "stamina";
                        else
                        {
                            if (p.spinCost > 0f) DMPlayerMomentum.TrySpend(p.spinCost);
                            if (s == null) s = cc.gameObject.AddComponent<DMMomentumSpin>();
                            s.Begin(cc, p);
                            return true;
                        }
                    }
                }
            }
            StatSpinRejects++; LastReject = why;
            if (p.debugLog) Debug.Log("[MomentumSpin] rejected: " + why);
            return false;
        }

        private void Begin(vThirdPersonController cc, DM_MomentumProfile p)
        {
            _cc = cc; _spinning = true; s_active++; _shook = false;
            _t = 0f; _startYaw = transform.eulerAngles.y; _dir = p.spinClockwise ? 1f : -1f; _total = 0f;
            _hit.Clear();
            _savedLock = cc.lockRotation;
            cc.lockRotation = true;
            StatSpins++;
            if (p.debugLog) Debug.Log("[MomentumSpin] start");
        }

        private void OnDisable() { if (_spinning) End(); }

        private void End()
        {
            _spinning = false; s_active = Mathf.Max(0, s_active - 1);
            if (_cc != null) _cc.lockRotation = _savedLock;
            StatLastSpinDegrees = _total;
        }

        private static float EaseInOut(float u) { u = Mathf.Clamp01(u); return u * u * u * (u * (6f * u - 15f) + 10f); }

        private void FixedUpdate()
        {
            if (!_spinning) return;
            DM_MomentumProfile p = DM_MomentumProfile.Live;
            if (_cc == null || _cc.isDead) { End(); return; }
            _cc.lockRotation = true;
            _t += Time.fixedDeltaTime;
            float w = p.spinWindupSeconds, s = Mathf.Max(0.05f, p.spinSeconds), sweepDeg = 360f * p.spinTurns;
            float off, swept;
            if (_t < w)
            {
                off = -_dir * p.spinWindupDegrees * EaseInOut(_t / Mathf.Max(0.001f, w));
                swept = 0f;
            }
            else
            {
                float u = EaseInOut((_t - w) / s);
                swept = (sweepDeg + p.spinWindupDegrees) * u - p.spinWindupDegrees; // from the wound-up pose through a full turn
                off = _dir * swept;
            }
            Vector3 e = transform.eulerAngles; e.y = _startYaw + off;
            transform.rotation = Quaternion.Euler(e);
            _total = Mathf.Max(_total, swept);
            if (swept > 0f) Sweep(p, swept);
            if (_t >= w + s + p.spinRecoverySeconds) End();
        }

        private void Sweep(DM_MomentumProfile p, float swept)
        {
            EnemyHealthSceneRegistry.CopyTo(s_buf);
            Vector3 me = transform.position;
            for (int i = 0; i < s_buf.Count; i++)
            {
                EnemyHealth h = s_buf[i];
                if (h == null || h.IsDead || _hit.Contains(h)) continue;
                Vector3 to = h.transform.position - me;
                if (Mathf.Abs(to.y) > p.spinHeightTolerance) continue;
                to.y = 0f;
                if (to.sqrMagnitude > p.spinRadius * p.spinRadius) continue;
                float bearing = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                float rel = Mathf.Repeat(_dir * Mathf.DeltaAngle(_startYaw, bearing), 360f);
                if (rel > swept && !(swept >= 360f)) continue;
                _hit.Add(h);
                Strike(p, h, to);
            }
        }

        private void Strike(DM_MomentumProfile p, EnemyHealth h, Vector3 flatTo)
        {
            Collider col = h.GetComponentInChildren<Collider>();
            Vector3 origin = transform.position + Vector3.up * 1.1f;
            Vector3 hp = col != null ? col.ClosestPoint(origin) : h.transform.position + Vector3.up;
            Vector3 dir = flatTo.sqrMagnitude > 0.0001f ? flatTo.normalized : transform.forward;
            DamageInfo info = DamageInfo.FromHit(p.spinDamage, false, gameObject, hp);
            info.IsChargedHit = p.spinCountsAsChargedHit;
            info.PoiseDamage *= p.spinPoiseMultiplier;
            if (!CombatDamageApplicator.ApplyToDamageable(h, h.gameObject, in info)) return;
            StatSpinHits++;
            if (col != null)
                DMEnemyHitMarks.HandleMeleeHit(h, col, hp, dir, p.spinDamage, PioneerInvectorDamageBridge.ResolveEquippedWeapon(gameObject), gameObject);
            if (!_shook && p.spinShakeAmplitude > 0f) { _shook = true; DMCombatCameraShake.Play(p.spinShakeAmplitude, p.spinShakeSeconds); }
            if (p.debugLog) Debug.Log($"[MomentumSpin] hit {h.name}");
        }
    }
}

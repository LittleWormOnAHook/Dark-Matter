using System;
using System.Collections;
using Invector;
using Invector.vCharacterController;
using Project.AI;
using Project.CameraFx;
using Project.Combat;
using Project.Survival;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// SurvivalStats is the health authority. Invector health mirrors Pioneer for hit reactions,
    /// fall damage forwarding, animator death state, and ragdoll presentation.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SurvivalStats))]
    public class PioneerInvectorSurvivalBridge : MonoBehaviour
    {
        private vThirdPersonController _controller;
        private SurvivalStats _survivalStats;
        private bool _subscribedToInvector;
        private bool _pushingToInvector;
        private bool _forwardingInvectorDamage;
        private bool _initialized;
        private bool _holdingCustomAction;
        private bool _savedCustomAction;
        private Coroutine _restoreCustomActionRoutine;

        private void Awake()
        {
            _controller = GetComponent<vThirdPersonController>();
            _survivalStats = GetComponent<SurvivalStats>();
            PioneerAnimationPlanSettings.Resolve(gameObject);
        }

        private void OnEnable()
        {
            SubscribeSurvivalEvents();
            SubscribeInvectorEvents();
            if (_initialized)
                PushHealthToInvector();
        }

        private void OnDisable()
        {
            UnsubscribeSurvivalEvents();
            UnsubscribeInvectorEvents();
            RestoreCustomActionNow();
        }

        private void Start()
        {
            StartCoroutine(InitializeWhenInvectorReady());
        }

        /// <summary>
        /// Mirrors Pioneer health/max onto Invector. Safe to call after damage, drain, death, or respawn.
        /// </summary>
        public void PushHealthToInvector()
        {
            if (_pushingToInvector || _controller == null || _survivalStats == null)
                return;

            _pushingToInvector = true;
            try
            {
                int max = Mathf.Max(1, Mathf.RoundToInt(_survivalStats.maxHealth));
                int current = Mathf.Clamp(Mathf.RoundToInt(_survivalStats.CurrentHealth), 0, max);

                if (_controller.maxHealth != max)
                    _controller.maxHealth = max;

                if (_survivalStats.IsDead)
                {
                    MirrorDeathToInvector();
                    return;
                }

                if (_controller.isDead)
                    _controller.isDead = false;

                if (Mathf.RoundToInt(_controller.currentHealth) != current)
                    _controller.ChangeHealth(current);
            }
            finally
            {
                _pushingToInvector = false;
            }
        }

        private void MirrorDeathToInvector()
        {
            if (_controller.isDead && _controller.currentHealth <= 0f)
                return;

            _controller.ChangeHealth(0);
        }

        private IEnumerator InitializeWhenInvectorReady()
        {
            yield return null;
            yield return new WaitForFixedUpdate();

            if (_controller == null || _survivalStats == null)
                yield break;

            _controller.isImmortal = true;
            ClearStaleInvectorDeathState();
            PushHealthToInvector();
            _initialized = true;
        }

        private void ClearStaleInvectorDeathState()
        {
            if (_controller == null || _survivalStats == null)
                return;

            if (!_controller.isDead || _survivalStats.IsDead)
                return;

            _controller.isDead = false;
            if (_controller.ragdolled)
                _controller.ResetRagdoll();
        }

        private void SubscribeSurvivalEvents()
        {
            if (_survivalStats == null)
                return;

            _survivalStats.OnStatsChanged += HandleSurvivalStatsChanged;
            _survivalStats.OnDamaged += HandleSurvivalDamaged;
            _survivalStats.PlayerDied += HandleSurvivalPlayerDied;
            _survivalStats.PlayerRevived += HandleSurvivalPlayerRevived;
        }

        private void UnsubscribeSurvivalEvents()
        {
            if (_survivalStats == null)
                return;

            _survivalStats.OnStatsChanged -= HandleSurvivalStatsChanged;
            _survivalStats.OnDamaged -= HandleSurvivalDamaged;
            _survivalStats.PlayerDied -= HandleSurvivalPlayerDied;
            _survivalStats.PlayerRevived -= HandleSurvivalPlayerRevived;
        }

        private void SubscribeInvectorEvents()
        {
            if (_subscribedToInvector || _controller == null)
                return;

            // onStartReceiveDamage fires before the isImmortal check; onReceiveDamage fires after
            // and is skipped when the controller is immortal. We must use the former so enemy
            // projectile/melee hits actually reach SurvivalStats.
            _controller.onStartReceiveDamage.AddListener(HandleInvectorDamage);
            _subscribedToInvector = true;
        }

        private void UnsubscribeInvectorEvents()
        {
            if (!_subscribedToInvector || _controller == null)
                return;

            _controller.onStartReceiveDamage.RemoveListener(HandleInvectorDamage);
            _subscribedToInvector = false;
        }

        private void HandleSurvivalStatsChanged() => PushHealthToInvector();

        private void HandleSurvivalDamaged(float _) => PushHealthToInvector();

        private void HandleSurvivalPlayerDied() => PushHealthToInvector();

        private void HandleSurvivalPlayerRevived() => PushHealthToInvector();

        private void HandleInvectorDamage(vDamage damage)
        {
            if (_forwardingInvectorDamage || _pushingToInvector || damage == null || _survivalStats == null)
                return;

            if (_survivalStats.IsDead || damage.damageValue <= 0f)
                return;

            _forwardingInvectorDamage = true;
            try
            {
                string senderName = damage.sender != null ? damage.sender.name : "unknown";
                _survivalStats.ApplyDamage(damage.damageValue, senderName);
                CombatHitVfx.SpawnIncomingEnemyHit(damage, transform);
                PlayerCombatEvents.RaisePlayerAttackedBySender(damage.sender);
                PushHealthToInvector();
            }
            finally
            {
                _forwardingInvectorDamage = false;
            }

            TryApplyHitReactionChance(damage);
        }

        /// <summary>
        /// When enabled, rolls 10-25% to play TriggerReaction. Failures suppress stock recoil/reaction
        /// so the chance is visible. When disabled, Invector TriggerDamageReaction runs unchanged.
        /// </summary>
        private void TryApplyHitReactionChance(vDamage damage)
        {
            PioneerAnimationPlanSettings settings = PioneerAnimationPlanSettings.Resolve(gameObject);
            if (settings == null || !settings.enableHitReactionChance)
                return;

            if (damage == null || damage.sender == null || damage.sender == transform)
                return;

            if (_survivalStats != null && _survivalStats.IsDead)
                return;

            if (TryRollHitReaction(settings, damage, out int reactionId))
            {
                damage.hitReaction = true;
                damage.reaction_id = reactionId;
                ApplyHitDirection(damage);
                TryShakeOnReaction(reactionId);
                return;
            }

            SuppressStockDamageReaction();
        }

        private bool TryRollHitReaction(PioneerAnimationPlanSettings settings, vDamage damage, out int reactionId)
        {
            reactionId = 0;
            float min = settings.smallHitChanceMin;
            float max = settings.smallHitChanceMax;
            if (min > max)
            {
                float swap = min;
                min = max;
                max = swap;
            }

            float healthMax = _survivalStats != null ? Mathf.Max(1f, _survivalStats.maxHealth) : 1f;
            float hpRatio = _survivalStats != null
                ? Mathf.Clamp01(_survivalStats.CurrentHealth / healthMax)
                : 1f;
            bool incomingCrit = IsIncomingCrit(damage);
            bool bigCandidate = incomingCrit || hpRatio < 0.5f;

            if (bigCandidate)
            {
                float bigChance = hpRatio < 0.5f
                    ? Mathf.Lerp(max, min, hpRatio / 0.5f)
                    : UnityEngine.Random.Range(min, max);

                if (UnityEngine.Random.value < bigChance)
                {
                    reactionId = 1;
                    return true;
                }
            }

            float smallChance = UnityEngine.Random.Range(min, max);
            if (UnityEngine.Random.value < smallChance)
            {
                reactionId = 0;
                return true;
            }

            return false;
        }

        private static bool IsIncomingCrit(vDamage damage)
        {
            if (damage == null)
                return false;

            return !string.IsNullOrEmpty(damage.damageType) &&
                   damage.damageType.IndexOf("crit", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ApplyHitDirection(vDamage damage)
        {
            Animator animator = _controller != null ? _controller.animator : GetComponent<Animator>();
            if (animator == null || !animator.enabled || damage == null || damage.sender == null)
                return;

            if (!HasAnimatorParam(animator, "HitDirection"))
                return;

            animator.SetInteger("HitDirection", (int)transform.HitAngle(damage.sender.position));
        }

        private void TryShakeOnReaction(int reactionId)
        {
            CameraShakeService shake = CameraShakeService.Instance;
            if (shake == null)
                return;

            float strength = reactionId == 1 ? 0.22f : 0.12f;
            shake.Impact(transform.position, strength, 8f);
        }

        /// <summary>
        /// vThirdPersonMotor.TriggerDamageReaction skips when customAction is set.
        /// Hold it until end of frame so stock recoil does not fire on a failed roll.
        /// </summary>
        private void SuppressStockDamageReaction()
        {
            if (_controller == null)
                return;

            if (!_holdingCustomAction)
            {
                _savedCustomAction = _controller.customAction;
                _holdingCustomAction = true;
                _controller.customAction = true;
            }

            if (_restoreCustomActionRoutine != null)
                StopCoroutine(_restoreCustomActionRoutine);

            _restoreCustomActionRoutine = StartCoroutine(RestoreCustomActionEndOfFrame());
        }

        private IEnumerator RestoreCustomActionEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            RestoreCustomActionNow();
        }

        private void RestoreCustomActionNow()
        {
            if (_restoreCustomActionRoutine != null)
            {
                StopCoroutine(_restoreCustomActionRoutine);
                _restoreCustomActionRoutine = null;
            }

            if (!_holdingCustomAction)
                return;

            if (_controller != null)
                _controller.customAction = _savedCustomAction;

            _holdingCustomAction = false;
        }

        private static bool HasAnimatorParam(Animator animator, string parameterName)
        {
            if (animator == null || string.IsNullOrEmpty(parameterName))
                return false;

            for (int i = 0; i < animator.parameterCount; i++)
            {
                if (animator.GetParameter(i).name == parameterName)
                    return true;
            }

            return false;
        }
    }
}

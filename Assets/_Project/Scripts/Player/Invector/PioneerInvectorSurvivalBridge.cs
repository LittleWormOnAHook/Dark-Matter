using System.Collections;
using Invector;
using Invector.vCharacterController;
using Project.AI;
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

        private void Awake()
        {
            _controller = GetComponent<vThirdPersonController>();
            _survivalStats = GetComponent<SurvivalStats>();
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
        }
    }
}

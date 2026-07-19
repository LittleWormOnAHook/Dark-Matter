using System;
using System.Collections;
using Project.AI.Invector;
using Project.Combat;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Orchestrates enemy death: death animation/ragdoll, pre-lift delay, disintegration,
    /// loot bag phase, then post-loot respawn delay before the corpse is ready to despawn.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-120)]
    public class EnemyDeathSequence : MonoBehaviour
    {
        [Header("Timing")]
        [Tooltip("Seconds to show death animation / ragdoll before lift and disintegration.")]
        [SerializeField] private float preDisintegrationDelay = 5f;

        [Tooltip("Seconds after loot is collected or the unlooted bag timer expires before respawn.")]
        [SerializeField] private float postLootRespawnDelay = 10f;

        private EnemyHealth _health;
        private EnemyLootable _lootable;
        private EnemyDisintegrationEffect _disintegration;
        private EnemyAnimationController _animationController;
        private EnemyInvectorDeathPresenter _invectorDeathPresenter;
        private EnemyTerrainRescue _terrainRescue;
        private EnemyInvectorMotorBridge _motorBridge;
        private Coroutine _sequenceRoutine;
        private bool _isComplete;

        public event Action SequenceCompleted;

        public bool IsComplete => _isComplete;
        public float PreDisintegrationDelay => preDisintegrationDelay;
        public float PostLootRespawnDelay => postLootRespawnDelay;

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _lootable = GetComponent<EnemyLootable>();
            _disintegration = GetComponent<EnemyDisintegrationEffect>();
            _animationController = GetComponent<EnemyAnimationController>();
            _invectorDeathPresenter = GetComponent<EnemyInvectorDeathPresenter>();
            _terrainRescue = GetComponent<EnemyTerrainRescue>();
            _motorBridge = GetComponent<EnemyInvectorMotorBridge>();
        }

        private void OnEnable()
        {
            _isComplete = false;

            if (_health != null)
            {
                _health.Died += HandleDied;
                _health.Respawned += HandleRespawned;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Died -= HandleDied;
                _health.Respawned -= HandleRespawned;
            }

            if (_sequenceRoutine != null)
            {
                StopCoroutine(_sequenceRoutine);
                _sequenceRoutine = null;
            }
        }

        private void HandleDied()
        {
            if (_sequenceRoutine != null)
                return;

            _sequenceRoutine = StartCoroutine(RunDeathSequence());
        }

        private void HandleRespawned()
        {
            _isComplete = false;

            if (_sequenceRoutine != null)
            {
                StopCoroutine(_sequenceRoutine);
                _sequenceRoutine = null;
            }

            EnemyInvectorBootstrap bootstrap = GetComponent<EnemyInvectorBootstrap>();
            if (bootstrap != null)
                bootstrap.enabled = true;

            if (_motorBridge != null)
                _motorBridge.enabled = true;

            EnemyInvectorCombatBridge combatBridge = GetComponent<EnemyInvectorCombatBridge>();
            if (combatBridge != null)
                combatBridge.enabled = true;

            if (_terrainRescue != null)
                _terrainRescue.enabled = true;

            _invectorDeathPresenter?.ResetForRespawn();
        }

        private IEnumerator RunDeathSequence()
        {
            if (_health != null)
                _health.SetRespawnExternallyManaged(true);

            BeginDeathPresentation();

            if (preDisintegrationDelay > 0f)
                yield return new WaitForSeconds(preDisintegrationDelay);

            Vector3 corpseLiftOrigin = _invectorDeathPresenter != null
                ? _invectorDeathPresenter.FinalizeCorpseForDisintegration()
                : transform.position + Vector3.up;

            EndDeathPresentation();

            if (_disintegration != null)
            {
                _disintegration.SetCorpseLiftOrigin(corpseLiftOrigin);
                bool presentationDone = false;
                _disintegration.BeginPresentation(() => presentationDone = true);

                while (!presentationDone && _health != null && _health.IsDead)
                    yield return null;
            }
            else if (_lootable != null && _lootable.IsLootPending)
            {
                _lootable.TrySpawnLootBag(transform.position);
            }

            while (_lootable != null && _lootable.IsLootPending)
                yield return null;

            if (postLootRespawnDelay > 0f)
                yield return new WaitForSeconds(postLootRespawnDelay);

            _isComplete = true;
            _sequenceRoutine = null;
            SequenceCompleted?.Invoke();

            if (_health != null && _health.IsDead && !_health.IsRespawnExternallyManaged)
                _health.FinishLootHoldAndRespawn();
        }

        private void BeginDeathPresentation()
        {
            EnemyInvectorCombatBridge combatBridge = GetComponent<EnemyInvectorCombatBridge>();
            if (combatBridge != null)
                combatBridge.enabled = false;

            if (_motorBridge != null)
                _motorBridge.enabled = false;

            if (_terrainRescue != null)
                _terrainRescue.enabled = false;

            EnemyInvectorBootstrap bootstrap = GetComponent<EnemyInvectorBootstrap>();
            if (bootstrap != null)
                bootstrap.enabled = false;

            if (_invectorDeathPresenter != null)
            {
                _invectorDeathPresenter.BeginDeathPresentation();
                return;
            }

            if (_animationController != null)
                _animationController.PlayDeath();
        }

        private void EndDeathPresentation()
        {
            Collider collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;

            if (_animationController != null)
                _animationController.enabled = false;
        }
    }
}

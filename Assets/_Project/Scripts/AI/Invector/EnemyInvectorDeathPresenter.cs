using System.Collections;
using Invector;
using Invector.vCharacterController;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Plays Invector death animation and ragdoll for humanoid enemies when Pioneer health reaches zero.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyInvectorDeathPresenter : MonoBehaviour
    {
        private const float RagdollFallbackDelaySeconds = 1.25f;

        private vThirdPersonController _controller;
        private vRagdoll _ragdoll;
        private Coroutine _ensureDeathRoutine;

        private void Awake()
        {
            EnsureReferences();
        }

        public void BeginDeathPresentation()
        {
            EnsureReferences();
            if (_controller == null)
                return;

            if (_controller.ragdolled || _controller.isDead)
                return;

            if (_controller is vHealthController healthController)
            {
                healthController.isImmortal = false;
                healthController.ChangeHealth(0);
            }
            else
            {
                _controller.isDead = true;
            }

            _controller.StopCharacter();

            if (_ensureDeathRoutine != null)
                StopCoroutine(_ensureDeathRoutine);

            _ensureDeathRoutine = StartCoroutine(EnsureDeathPresentation());
        }

        public void ResetForRespawn()
        {
            if (_ensureDeathRoutine != null)
            {
                StopCoroutine(_ensureDeathRoutine);
                _ensureDeathRoutine = null;
            }

            _ragdoll?.RestoreRagdoll();

            if (_controller != null && _controller.ragdolled)
                _controller.ResetRagdoll();

            EnemyInvectorBootstrap bootstrap = GetComponent<EnemyInvectorBootstrap>();
            bootstrap?.EnsureInvectorPhysicsReady();
        }

        private void EnsureReferences()
        {
            if (_controller == null)
                _controller = GetComponent<vThirdPersonController>();
            if (_ragdoll == null)
                _ragdoll = GetComponent<vRagdoll>();
        }

        private IEnumerator EnsureDeathPresentation()
        {
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                if (_controller == null)
                    yield break;

                if (_controller.isDead || _controller.ragdolled)
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_controller == null)
                yield break;

            if (_controller.deathBy == vCharacter.DeathBy.Ragdoll)
            {
                ForceRagdollActivation();
                _ensureDeathRoutine = null;
                yield break;
            }

            elapsed = 0f;
            while (elapsed < RagdollFallbackDelaySeconds)
            {
                if (_controller == null)
                    yield break;

                if (_controller.ragdolled)
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_controller != null && _controller.isDead && !_controller.ragdolled)
                ForceRagdollActivation();

            _ensureDeathRoutine = null;
        }

        private void ForceRagdollActivation()
        {
            if (_controller == null || _controller.ragdolled)
                return;

            if (_ragdoll != null)
            {
                _ragdoll.ActivateRagdoll(null);
                return;
            }

            _controller.onActiveRagdoll.Invoke(null);
        }
    }
}

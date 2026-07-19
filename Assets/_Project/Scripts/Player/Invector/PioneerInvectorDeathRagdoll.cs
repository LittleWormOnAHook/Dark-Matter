using System.Collections;
using Invector.vCharacterController;
using Project.Survival;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// Ensures Invector death animation and ragdoll play when Pioneer SurvivalStats reports death.
    /// Health/death state is mirrored by <see cref="PioneerInvectorSurvivalBridge"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class PioneerInvectorDeathRagdoll : MonoBehaviour
    {
        private const float RagdollFallbackDelaySeconds = 1.25f;

        private vThirdPersonController _controller;
        private vRagdoll _ragdoll;
        private SurvivalStats _survivalStats;
        private Coroutine _ensureDeathRoutine;

        private void Awake()
        {
            EnsureReferences();
        }

        /// <summary>
        /// Called by PlayerDeathHandler when SurvivalStats reports death.
        /// </summary>
        public void ActivateDeathRagdoll()
        {
            EnsureReferences();
            if (_controller == null)
            {
                Debug.LogWarning("[PioneerInvectorDeathRagdoll] Missing vThirdPersonController; cannot play death ragdoll.");
                return;
            }

            if (_controller.ragdolled)
                return;

            GetComponent<PioneerInvectorSurvivalBridge>()?.PushHealthToInvector();

            _controller.StopCharacter();

            if (_ensureDeathRoutine != null)
                StopCoroutine(_ensureDeathRoutine);

            _ensureDeathRoutine = StartCoroutine(EnsureDeathPresentation());
        }

        /// <summary>
        /// Called by PlayerDeathHandler before teleporting on respawn.
        /// </summary>
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

            PioneerInvectorBootstrap bootstrap = GetComponent<PioneerInvectorBootstrap>();
            bootstrap?.EnsureInvectorPhysicsReady();
        }

        private void EnsureReferences()
        {
            if (_controller == null)
                _controller = GetComponent<vThirdPersonController>();
            if (_ragdoll == null)
                _ragdoll = GetComponent<vRagdoll>();
            if (_survivalStats == null)
                _survivalStats = GetComponent<SurvivalStats>();
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

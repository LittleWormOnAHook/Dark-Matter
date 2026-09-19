using System.Collections;

using Invector.vCharacterController;

using Project.Survival;

using UnityEngine;

namespace Project.Player.Invector

{

    /// <summary>

    /// Ensures Invector death animation and ragdoll play when Pioneer SurvivalStats reports death.

    /// After Retry, plays FullBody StandUp at the death spot (belly/back from corpse pose).

    /// </summary>

    [DisallowMultipleComponent]

    public class PioneerInvectorDeathRagdoll : MonoBehaviour

    {

        private const float RagdollFallbackDelaySeconds = 1.25f;

        private const float GetUpTimeoutSeconds = 3.25f;

        private const string FullBodyLayerName = "FullBody";

        private const string StandUpFromBack = "StandUp@FromBack";

        private const string StandUpFromBelly = "StandUp@FromBelly";

        private vThirdPersonController _controller;

        private vRagdoll _ragdoll;

        private SurvivalStats _survivalStats;

        private Coroutine _ensureDeathRoutine;

        private Coroutine _getUpRoutine;

        private bool _standFromBack = true;

        private bool _lieCaptured;

        private bool _deathPoseCaptured;

        private Vector3 _deathPosition;

        private float _deathYaw;

        private void Awake()

        {

            EnsureReferences();

        }

        public void ActivateDeathRagdoll()

        {

            EnsureReferences();

            _lieCaptured = false;

            _deathPoseCaptured = false;

            if (_controller == null)

            {

                Debug.LogWarning("[PioneerInvectorDeathRagdoll] Missing vThirdPersonController; cannot play death ragdoll.");

                return;

            }

            if (_controller.ragdolled)

            {

                if (_ragdoll != null)

                {

                    _ragdoll.keepRagdolled = true;

                    _ragdoll.ignoreGetUpAnimation = true;

                }

                return;

            }

            GetComponent<PioneerInvectorSurvivalBridge>()?.PushHealthToInvector();

            _controller.StopCharacter();

            if (_ensureDeathRoutine != null)

                StopCoroutine(_ensureDeathRoutine);

            _ensureDeathRoutine = StartCoroutine(EnsureDeathPresentation());

        }

        /// <summary>

        /// Snapshot belly/back and world death spot while the corpse is still posed.

        /// </summary>

        public void CaptureLieOrientation()

        {

            EnsureReferences();

            Animator animator = ResolveAnimator();

            Transform hips = null;

            if (animator != null && animator.isHuman)

                hips = animator.GetBoneTransform(HumanBodyBones.Hips);

            if (hips == null && _ragdoll != null)

                hips = _ragdoll.characterHips;

            Transform poseRoot = hips != null ? hips : transform;

            _deathPosition = poseRoot.position;

            Vector3 flatFwd = poseRoot.forward;

            flatFwd.y = 0f;

            if (flatFwd.sqrMagnitude < 0.01f)

            {

                flatFwd = transform.forward;

                flatFwd.y = 0f;

            }

            _deathYaw = flatFwd.sqrMagnitude > 0.01f

                ? Quaternion.LookRotation(flatFwd.normalized, Vector3.up).eulerAngles.y

                : transform.eulerAngles.y;

            _deathPoseCaptured = true;

            if (hips == null)

            {

                _standFromBack = true;

                _lieCaptured = true;

                return;

            }

            _standFromBack = Vector3.Dot(hips.forward, Vector3.up) >= 0f;

            if (_ragdoll != null && _ragdoll.invertGetUpAnim)

                _standFromBack = !_standFromBack;

            _lieCaptured = true;

        }

        public bool TryGetDeathPose(out Vector3 position, out Quaternion uprightRotation)

        {

            position = _deathPosition;

            uprightRotation = Quaternion.Euler(0f, _deathYaw, 0f);

            return _deathPoseCaptured;

        }

        public void ResetForRespawn()

        {

            if (_ensureDeathRoutine != null)

            {

                StopCoroutine(_ensureDeathRoutine);

                _ensureDeathRoutine = null;

            }

            if (_getUpRoutine != null)

            {

                StopCoroutine(_getUpRoutine);

                _getUpRoutine = null;

            }

            if (!_lieCaptured)

                CaptureLieOrientation();

            EnsureReferences();

            if (_ragdoll != null)

            {

                _ragdoll.keepRagdolled = false;

                _ragdoll.ignoreGetUpAnimation = true;

                _ragdoll.RestoreRagdoll();

            }

            if (_controller != null)

            {

                if (_controller.ragdolled)

                    _controller.ResetRagdoll();

                _controller.lockMovement = false;

                _controller.lockAnimMovement = false;

                _controller.EnableGravityAndCollision();

            }

            Animator animator = ResolveAnimator();

            if (animator != null)

                animator.enabled = true;

            GetComponent<PioneerInvectorBootstrap>()?.EnsureInvectorPhysicsReady();

        }

        public void PlayStandUpAfterRespawn()

        {

            if (_getUpRoutine != null)

                StopCoroutine(_getUpRoutine);

            _getUpRoutine = StartCoroutine(StandUpAfterRespawnRoutine());

        }

        private IEnumerator StandUpAfterRespawnRoutine()

        {

            EnsureReferences();

            Animator animator = ResolveAnimator();

            if (animator == null)

            {

                _getUpRoutine = null;

                yield break;

            }

            int layer = animator.GetLayerIndex(FullBodyLayerName);

            if (layer < 0)

                layer = 7;

            string stateName = _standFromBack ? StandUpFromBack : StandUpFromBelly;

            int stateHash = Animator.StringToHash(stateName);

            animator.SetLayerWeight(layer, 1f);

            if (animator.HasState(layer, stateHash))

                animator.CrossFadeInFixedTime(stateHash, 0.08f, layer, 0f);

            else

                animator.Play(stateHash, layer, 0f);

            if (_controller != null)

            {

                _controller.lockMovement = true;

                _controller.lockAnimMovement = true;

                _controller.isJumping = false;

            }

            float elapsed = 0f;

            while (elapsed < GetUpTimeoutSeconds)

            {

                if (animator == null)

                    break;

                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layer);

                bool inStand = info.shortNameHash == stateHash || info.IsName(stateName);

                if (elapsed > 0.2f && !inStand && !animator.IsInTransition(layer))

                    break;

                if (inStand && info.normalizedTime >= 0.92f && !animator.IsInTransition(layer))

                    break;

                elapsed += Time.deltaTime;

                yield return null;

            }

            if (_controller != null)

            {

                _controller.lockMovement = false;

                _controller.lockAnimMovement = false;

            }

            _lieCaptured = false;

            _deathPoseCaptured = false;

            _getUpRoutine = null;

        }

        private Animator ResolveAnimator()

        {

            Animator animator = GetComponentInChildren<Animator>();

            if (animator != null)

                return animator;

            GameObject player = gameObject.name == "Player_v7" ? gameObject : GameObject.Find("Player_v7");

            return player != null ? player.GetComponentInChildren<Animator>() : null;

        }

        private void EnsureReferences()

        {

            if (_controller == null)

                _controller = GetComponent<vThirdPersonController>();

            if (_ragdoll == null)

                _ragdoll = GetComponent<vRagdoll>() ?? GetComponentInChildren<vRagdoll>(true);

            if (_survivalStats == null)

                _survivalStats = GetComponent<SurvivalStats>();

            if (_controller != null && _ragdoll != null)

                return;

            GameObject player = gameObject.name == "Player_v7" ? gameObject : GameObject.Find("Player_v7");

            if (player == null || player == gameObject)

                return;

            if (_controller == null)

                _controller = player.GetComponent<vThirdPersonController>();

            if (_ragdoll == null)

                _ragdoll = player.GetComponent<vRagdoll>() ?? player.GetComponentInChildren<vRagdoll>(true);

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

            if (_controller == null)

                return;

            if (_ragdoll != null)

            {

                _ragdoll.keepRagdolled = true;

                _ragdoll.ignoreGetUpAnimation = true;

                if (!_ragdoll.isActive)

                    _ragdoll.ActivateRagdoll(null, 999f);

                return;

            }

            if (!_controller.ragdolled)

                _controller.onActiveRagdoll.Invoke(null);

        }

    }

}

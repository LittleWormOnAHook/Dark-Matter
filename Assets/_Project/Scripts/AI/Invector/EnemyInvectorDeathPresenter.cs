using System;
using System.Collections;
using Invector;
using Invector.vCharacterController;
using Project.AI;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Drives Invector death animation then delegates ragdoll activation to <see cref="EnemyInvectorRagdollBridge"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyInvectorDeathPresenter : MonoBehaviour
    {
        private const float RagdollFallbackDelaySeconds = 1.25f;

        private vThirdPersonController _controller;
        private EnemyInvectorRagdollBridge _ragdollBridge;
        private Transform _cachedHipsParent;
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

            StopDeathRoutines();
            PrepareAnimatorForDeath();
            CacheHipsParentIfNeeded();
            _ragdollBridge?.PrepareForDeath();

            if (_ragdollBridge != null && _ragdollBridge.HasActiveRagdoll && !_ragdollBridge.IsCorpseRagdolled)
            {
                _ragdollBridge.ActivateCorpseRagdoll();
                _ensureDeathRoutine = StartCoroutine(EnsureDeathPresentation());
                return;
            }

            if (_ragdollBridge != null && _ragdollBridge.IsCorpseRagdolled)
            {
                _ensureDeathRoutine = StartCoroutine(EnsureDeathPresentation());
                return;
            }

            if (_controller.ragdolled)
            {
                _ragdollBridge?.ActivateCorpseRagdoll();
                _ensureDeathRoutine = StartCoroutine(EnsureDeathPresentation());
                return;
            }

            // Ranged / DoT / non-stagger deaths never enter an active hit-stagger ragdoll. Waiting on
            // AnimationWithRagdoll + a Dead-tagged animator state left those corpses frozen in pose.
            // Force corpse ragdoll immediately; EnsureDeathPresentation still retries if body parts
            // need a frame to bind after distance-cull unhide.
            MarkControllerDead();
            ClearLocomotionAnimatorParams();
            _controller.disableAnimations = false;
            _controller.StopCharacter();

            _ragdollBridge?.ActivateCorpseRagdoll();
            _ensureDeathRoutine = StartCoroutine(EnsureDeathPresentation());
        }

        private void MarkControllerDead()
        {
            if (_controller == null || _controller.isDead)
                return;

            if (_controller is vHealthController healthController)
            {
                healthController.isImmortal = false;
                // ChangeHealth sets absolute health (does not add). Drain to zero so isDead flips.
                if (healthController.currentHealth > 0f)
                    healthController.ChangeHealth(0);
            }

            if (!_controller.isDead)
                _controller.isDead = true;
        }

        public void ResetForRespawn()
        {
            StopDeathRoutines();
            _cachedHipsParent = null;

            _ragdollBridge?.RestoreForRespawn();

            EnemyInvectorBootstrap bootstrap = GetComponent<EnemyInvectorBootstrap>();
            if (bootstrap != null)
            {
                bootstrap.enabled = true;
                bootstrap.EnsureInvectorPhysicsReady();
            }

            RestoreInvectorAliveState();
            RestoreCombatLoadout();
            GetComponent<EnemyInvectorLoadoutBridge>()?.ClearDroppedWeaponReference();
            EnemyInvectorRagdollSetup.EnsurePresent(gameObject);
            EnsureReferences();

            CapsuleCollider rootCapsule = GetComponent<CapsuleCollider>();
            if (rootCapsule != null)
                rootCapsule.enabled = true;
        }

        public Vector3 FinalizeCorpseForDisintegration()
        {
            StopDeathRoutines();
            EnsureReferences();
            CollapseRagdollCorpseForDissolve();

            if (_controller != null)
            {
                _controller.moveDirection = Vector3.zero;
                _controller.input = Vector3.zero;
                _controller.isSprinting = false;
                _controller.StopCharacter();
            }

            ClearLocomotionAnimatorParams();
            return ResolveGroundLootPosition();
        }

        private void CacheHipsParentIfNeeded()
        {
            if (_cachedHipsParent != null || _controller?.animator == null)
                return;

            Transform hips = _controller.animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips != null)
                _cachedHipsParent = hips.parent;
        }

        private void CollapseRagdollCorpseForDissolve()
        {
            if (_controller?.animator == null)
                return;

            Transform hips = _controller.animator.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null)
                return;

            Transform restoreParent = _cachedHipsParent != null
                ? _cachedHipsParent
                : _controller.animator.transform;

            if (hips.parent != restoreParent)
                hips.SetParent(restoreParent, true);

            vRagdoll ragdoll = _ragdollBridge != null ? _ragdollBridge.Ragdoll : GetComponent<vRagdoll>();
            if (ragdoll != null)
                Destroy(ragdoll);

            EnemyInvectorHitSetup.StabilizeRigidbodies(gameObject);
        }

        private Vector3 ResolveGroundLootPosition()
        {
            Vector3 sample = transform.position;

            if (_controller?.animator != null)
            {
                Transform hips = _controller.animator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null)
                    sample = hips.position;

                SkinnedMeshRenderer[] meshes = _controller.animator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                Bounds bounds = default;
                bool hasBounds = false;

                for (int i = 0; i < meshes.Length; i++)
                {
                    SkinnedMeshRenderer mesh = meshes[i];
                    if (mesh == null || IsWeaponRenderer(mesh.transform))
                        continue;

                    if (!hasBounds)
                    {
                        bounds = mesh.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(mesh.bounds);
                    }
                }

                if (hasBounds)
                    sample = bounds.center;
            }

            return EnemyGroundUtility.SnapPositionToGround(sample);
        }

        private static bool IsWeaponRenderer(Transform node)
        {
            while (node != null)
            {
                string nodeName = node.name;
                if (nodeName.StartsWith("Drawn_", StringComparison.Ordinal) ||
                    nodeName.StartsWith("Holstered_", StringComparison.Ordinal))
                    return true;

                if (node.CompareTag("Weapon") || node.CompareTag("Ignore Ragdoll"))
                    return true;

                node = node.parent;
            }

            return false;
        }

        private void StopDeathRoutines()
        {
            if (_ensureDeathRoutine != null)
            {
                StopCoroutine(_ensureDeathRoutine);
                _ensureDeathRoutine = null;
            }
        }

        private void PrepareAnimatorForDeath()
        {
            if (_controller == null)
                return;

            _controller.disableAnimations = false;
            _controller.isGrounded = true;

            if (_controller.animator == null)
                return;

            _controller.animator.enabled = true;
            _controller.animator.updateMode = AnimatorUpdateMode.Normal;
            _controller.animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        private IEnumerator EnsureDeathPresentation()
        {
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                if (_controller == null)
                    yield break;

                if (_controller.isDead || IsCorpseRagdolled())
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_controller == null)
                yield break;

            // Immediate corpse path (ranged / non-stagger): retry activation if the first attempt
            // raced animator un-cull / body-part bind.
            if (!IsCorpseRagdolled())
                _ragdollBridge?.ActivateCorpseRagdoll();

            elapsed = 0f;
            while (elapsed < RagdollFallbackDelaySeconds)
            {
                if (_controller == null)
                    yield break;

                if (IsCorpseRagdolled())
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_controller != null && !IsCorpseRagdolled())
                _ragdollBridge?.ActivateCorpseRagdoll();

            _ensureDeathRoutine = null;
        }

        private bool IsCorpseRagdolled()
        {
            if (_ragdollBridge != null && _ragdollBridge.IsCorpseRagdolled)
                return true;

            return _controller != null && _controller.ragdolled;
        }

        private void RestoreInvectorAliveState()
        {
            if (_controller == null)
                return;

            if (_controller is vHealthController healthController)
            {
                healthController.isDead = false;
                healthController.ResetHealth();
                healthController.isImmortal = true;
            }
            else
            {
                _controller.isDead = false;
            }

            _controller.disableAnimations = false;

            if (_controller.animator != null)
            {
                _controller.animator.enabled = true;
                _controller.animator.Rebind();
                _controller.animator.Update(0f);
            }
        }

        private void RestoreCombatLoadout()
        {
            EnemyInvectorBodySnapSetup.ApplyRuntime(gameObject);

            EnemyInvectorLoadoutBridge loadout = GetComponent<EnemyInvectorLoadoutBridge>();
            loadout?.EquipStartingWeapon();
        }

        private void EnsureReferences()
        {
            if (_controller == null)
                _controller = GetComponent<vThirdPersonController>();
            if (_ragdollBridge == null)
                _ragdollBridge = GetComponent<EnemyInvectorRagdollBridge>();
        }

        private void ClearLocomotionAnimatorParams()
        {
            if (_controller == null || _controller.animator == null)
                return;

            Animator animator = _controller.animator;
            if (animator.HasParameter("InputHorizontal"))
                animator.SetFloat("InputHorizontal", 0f);
            if (animator.HasParameter("InputVertical"))
                animator.SetFloat("InputVertical", 0f);
            if (animator.HasParameter("InputMagnitude"))
                animator.SetFloat("InputMagnitude", 0f);
            if (animator.HasParameter("Speed"))
                animator.SetFloat("Speed", 0f);
        }
    }

    internal static class AnimatorParameterExtensions
    {
        public static bool HasParameter(this Animator animator, string parameterName)
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

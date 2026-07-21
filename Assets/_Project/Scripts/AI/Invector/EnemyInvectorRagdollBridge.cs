using System.Collections;
using Invector;
using Invector.vCharacterController;
using Project.AI;
using UnityEngine;
using UnityEngine.Events;

namespace Project.AI.Invector
{
    /// <summary>
    /// Owns Invector <see cref="vRagdoll"/> on enemies: corpse death ragdoll, hit-stagger ragdoll, and respawn restore.
    /// </summary>
    [DefaultExecutionOrder(-850)]
    [DisallowMultipleComponent]
    public class EnemyInvectorRagdollBridge : MonoBehaviour
    {
        [Header("Hit Stagger")]
        [SerializeField] private bool enableHitStagger = true;
        [SerializeField] private float minStaggerDamage = 18f;
        [SerializeField] private float defaultStaggerSeconds = 0.55f;
        [SerializeField] private bool staggerOnCritical = true;

        [Header("Ragdoll Launch Guard")]
        [Tooltip(
            "vRagdoll seeds bone velocity from this root Rigidbody's linearVelocity on activation. " +
            "Enemies move via Rigidbody.MovePosition, which reports an implicit velocity for the " +
            "physics engine; a large single-step displacement (repath snap, terrain rescue, target " +
            "reacquire) spikes that value for one frame. Without a clamp, a corpse can inherit that " +
            "spike and launch/disappear instead of collapsing. Set above normal chase speed so " +
            "natural running momentum still carries into the ragdoll.")]
        [SerializeField] private float maxCorpseLaunchSpeed = 6f;

        private vThirdPersonController _controller;
        private vRagdoll _ragdoll;
        private EnemyHealth _health;
        private EnemyInvectorMotorBridge _motorBridge;
        private EnemyInvectorPhysicsCache _physicsCache;
        private Coroutine _staggerRoutine;
        private bool _isHitStaggerActive;

        public vRagdoll Ragdoll => _ragdoll;

        public bool IsCorpseRagdolled =>
            _ragdoll != null &&
            _ragdoll.keepRagdolled &&
            (_ragdoll.isActive || (_controller != null && _controller.ragdolled));

        public bool IsHitStaggerActive => _isHitStaggerActive;

        public bool HasActiveRagdoll =>
            _ragdoll != null &&
            (_ragdoll.isActive || (_controller != null && _controller.ragdolled));

        private void Awake()
        {
            _controller = GetComponent<vThirdPersonController>();
            _health = GetComponent<EnemyHealth>();
            _motorBridge = GetComponent<EnemyInvectorMotorBridge>();
            _physicsCache = GetComponent<EnemyInvectorPhysicsCache>();
            _ragdoll = EnemyInvectorRagdollSetup.EnsurePresent(gameObject);
            EnemyInvectorRagdollSetup.ConfigureForCorpse(_ragdoll);
        }

        private void OnEnable()
        {
            if (_health != null)
                _health.Died += HandleHealthDied;
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.Died -= HandleHealthDied;

            StopHitStaggerRoutine();
        }

        private void Start()
        {
            StartCoroutine(FinalizeListenerAfterRagdollStart());
        }

        public void PrepareForDeath()
        {
            AbortHitStaggerCoroutine();
            _isHitStaggerActive = false;
            PauseAiLocomotion(false);
            EnsureBodyPartsLoaded();
            if (_ragdoll != null)
            {
                _ragdoll.keepRagdolled = true;
                _ragdoll.ignoreGetUpAnimation = true;
            }
        }

        /// <summary>
        /// Brief ragdoll on heavy hits or attacks flagged with <see cref="vDamage.activeRagdoll"/>.
        /// </summary>
        public void TryHitStagger(
            vDamage sourceDamage,
            float pioneerDamage,
            bool isCritical,
            bool weaponRequestsStagger,
            float weaponStaggerSeconds)
        {
            if (!enableHitStagger || _controller == null || _ragdoll == null)
                return;

            if (_health != null && _health.IsDead)
                return;

            if (_controller.isDead || IsCorpseRagdolled)
                return;

            if (_staggerRoutine != null)
                return;

            bool shouldStagger = weaponRequestsStagger ||
                                 (staggerOnCritical && isCritical) ||
                                 pioneerDamage >= minStaggerDamage;
            if (!shouldStagger)
                return;

            float duration = weaponStaggerSeconds > 0f ? weaponStaggerSeconds : defaultStaggerSeconds;
            vDamage staggerDamage = BuildStaggerDamage(sourceDamage);
            _staggerRoutine = StartCoroutine(HitStaggerRoutine(staggerDamage, duration));
        }

        public void ActivateCorpseRagdoll(vDamage damage = null)
        {
            if (_controller == null || _ragdoll == null)
                return;

            if (IsCorpseRagdolled)
                return;

            // A killing blow that lands while a hit-stagger is blending back to animation (vRagdoll.state
            // == blendToAnim) leaves that transition permanently stuck: RagdollBehaviour() is the only
            // code that can finish it, and it bails out for the rest of the object's life once the
            // character is dead. vRagdoll.ActivateRagdoll() then refuses to run again while state ==
            // blendToAnim, so the corpse freezes mid hit-reaction pose with no warning or exception.
            // Clear the stale transitional state (and any desynced controller.ragdolled flag left by the
            // interrupted stagger) so activation below is guaranteed to take effect.
            if (!_ragdoll.isActive)
                ClearStaleHitStaggerRagdollState();

            AbortHitStaggerCoroutine();

            GetComponent<EnemyInvectorLoadoutBridge>()?.DropHeldWeaponOnDeath();

            _controller.moveDirection = Vector3.zero;
            _controller.input = Vector3.zero;
            _controller.isSprinting = false;
            _controller.StopCharacter();

            Collider rootCollider = GetComponent<Collider>();
            if (rootCollider != null)
                rootCollider.enabled = false;

            if (_motorBridge != null)
                _motorBridge.enabled = false;

            _physicsCache?.MarkBonesUnstable();
            EnemyInvectorHitSetup.ReleaseForRagdoll(gameObject);
            ClampRootVelocityForRagdoll();

            _ragdoll.keepRagdolled = true;
            _ragdoll.ignoreGetUpAnimation = true;

            // Reload body parts immediately before activation. Guarantees the ragdoll has its bones
            // even if the spawn-time load lost a timing race; warns if the rig has no usable ragdoll.
            if (!EnsureBodyPartsLoaded())
            {
                Debug.LogWarning(
                    $"{name}: ragdoll body parts unavailable at death (humanoid avatar not bound or no ragdoll rig); corpse may not ragdoll.",
                    this);
            }

            vDamage corpseDamage = BuildCorpseDamage(damage);
            _ragdoll.ActivateRagdoll(corpseDamage);
            StartCoroutine(SettleCorpseVelocities());
        }

        public void RestoreForRespawn()
        {
            StopHitStaggerRoutine();

            if (_ragdoll != null)
            {
                _ragdoll.keepRagdolled = false;
                _ragdoll.ignoreGetUpAnimation = false;
                _ragdoll.RestoreRagdoll();
            }

            if (_controller != null && _controller.ragdolled)
                _controller.ResetRagdoll();

            Collider rootCollider = GetComponent<CapsuleCollider>();
            if (rootCollider != null)
                rootCollider.enabled = true;

            if (_motorBridge != null)
                _motorBridge.enabled = true;

            EnemyInvectorHitSetup.StabilizeRigidbodies(gameObject);
            EnsureBodyPartsLoaded();
        }

        private IEnumerator HitStaggerRoutine(vDamage staggerDamage, float duration)
        {
            _isHitStaggerActive = true;
            PauseAiLocomotion(true);

            if (_motorBridge != null)
                _motorBridge.enabled = false;

            _physicsCache?.MarkBonesUnstable();
            EnemyInvectorHitSetup.ReleaseForRagdoll(gameObject);
            ClampRootVelocityForRagdoll();

            _ragdoll.keepRagdolled = false;
            _ragdoll.ignoreGetUpAnimation = true;
            _ragdoll.ActivateRagdoll(staggerDamage, duration);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (_health != null && _health.IsDead)
                {
                    _staggerRoutine = null;
                    _isHitStaggerActive = false;
                    PauseAiLocomotion(false);
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_health == null || !_health.IsDead)
                RestoreFromHitStagger();

            _staggerRoutine = null;
            _isHitStaggerActive = false;
            PauseAiLocomotion(false);
        }

        private void RestoreFromHitStagger()
        {
            if (_health != null && _health.IsDead)
                return;
            if (_ragdoll != null)
            {
                _ragdoll.keepRagdolled = false;
                _ragdoll.RestoreRagdoll();
            }

            if (_controller != null && _controller.ragdolled)
                _controller.ResetRagdoll();

            CapsuleCollider rootCapsule = GetComponent<CapsuleCollider>();
            if (rootCapsule != null)
                rootCapsule.enabled = true;

            if (_physicsCache != null)
                _physicsCache.StabilizeBonesIfNeeded();
            else
                EnemyInvectorHitSetup.StabilizeRigidbodies(gameObject);

            EnemyInvectorHitSetup.RestoreRagdollPhysicsLayers(gameObject);

            if (_motorBridge != null)
                _motorBridge.enabled = true;
        }

        private void HandleHealthDied()
        {
            AbortHitStaggerCoroutine();
            _isHitStaggerActive = false;
            PauseAiLocomotion(false);
        }

        private void StopHitStaggerRoutine()
        {
            if (_staggerRoutine == null)
                return;

            AbortHitStaggerCoroutine();
            _isHitStaggerActive = false;
            PauseAiLocomotion(false);

            if (_health == null || !_health.IsDead)
                RestoreFromHitStagger();
        }

        private void AbortHitStaggerCoroutine()
        {
            if (_staggerRoutine == null)
                return;

            StopCoroutine(_staggerRoutine);
            _staggerRoutine = null;
        }

        private void PauseAiLocomotion(bool paused)
        {
            EnemyAiController aiController = GetComponent<EnemyAiController>();
            if (aiController != null)
                aiController.SetLocomotionPaused(paused);
        }

        private IEnumerator FinalizeListenerAfterRagdollStart()
        {
            yield return null;

            if (_controller == null || _ragdoll == null)
                yield break;

            _controller.onActiveRagdoll.RemoveListener((UnityAction<vDamage>)_ragdoll.ActivateRagdoll);
            _controller.onActiveRagdoll.RemoveListener(OnActiveRagdollRequested);
            _controller.onActiveRagdoll.AddListener(OnActiveRagdollRequested);

            // Retry until the humanoid avatar is bound so bodyParts is never left empty at spawn.
            // Without this, an enemy instantiated before its animator initialized would freeze on
            // death instead of ragdolling. Cap the retries so we never spin forever on a bad rig.
            int guard = 0;
            while (!EnsureBodyPartsLoaded() && guard < 120)
            {
                guard++;
                yield return null;
            }
        }

        /// <summary>
        /// Loads vRagdoll body parts once the humanoid avatar is bound. Returns false when the
        /// animator/avatar is not ready yet so callers can retry instead of leaving bodyParts empty.
        /// </summary>
        private bool EnsureBodyPartsLoaded()
        {
            if (_ragdoll == null)
                return false;

            if (!AnimatorHipsReady())
                return false;

            _ragdoll.LoadBodyPart();
            EnemyInvectorHitSetup.RestoreRagdollPhysicsLayers(gameObject);
            return true;
        }

        private bool AnimatorHipsReady()
        {
            Animator animator = _controller != null ? _controller.animator : GetComponentInChildren<Animator>(true);
            return animator != null && animator.isHuman && animator.GetBoneTransform(HumanBodyBones.Hips) != null;
        }

        /// <summary>
        /// Forces the ragdoll out of a stuck mid-transition state left by an interrupted hit-stagger
        /// recovery. Safe to call whenever <see cref="vRagdoll.isActive"/> is false: if the ragdoll
        /// genuinely isn't running, state/isActive/controller.ragdolled should all agree it is at rest,
        /// so resetting them here only ever corrects staleness, never a real in-progress ragdoll.
        /// </summary>
        private void ClearStaleHitStaggerRagdollState()
        {
            if (_ragdoll == null)
                return;

            if (_ragdoll.state == vRagdoll.RagdollState.blendToAnim)
                _ragdoll.state = vRagdoll.RagdollState.animated;

            _ragdoll.isActive = false;

            if (_controller != null)
                _controller.ragdolled = false;
        }

        /// <summary>
        /// vRagdoll.setKinematic(false) seeds every bone's velocity from this root Rigidbody's
        /// linearVelocity. Enemies move via Rigidbody.MovePosition (see
        /// EnemyInvectorMotorBridge.SyncRigidbodyToTransform), and a kinematic body moved a large
        /// distance in a single fixed step (repath snap, terrain rescue, sudden target reacquire)
        /// reports an implicit velocity spike for that step. Clamp before ragdoll activation so a
        /// corpse collapses instead of launching or disappearing off-screen.
        /// </summary>
        private void ClampRootVelocityForRagdoll()
        {
            Rigidbody rootBody = GetComponent<Rigidbody>();
            if (rootBody == null)
                return;

            // linearVelocity is readable/writable on a kinematic Rigidbody (Unity uses it for
            // MovePosition-driven collision response), so this clamp works fine even though this root
            // body is always kinematic (EnemyInvectorPhysicsCache keeps it that way; only the per-bone
            // Rigidbodies go dynamic on ragdoll). angularVelocity is different: Unity does not support
            // setting it on a kinematic body and logs a warning on every call, and vRagdoll never reads
            // this body's angularVelocity anyway (only linearVelocity, see setKinematic() above) — so it
            // must not be touched here.
            Vector3 velocity = rootBody.linearVelocity;
            if (velocity.sqrMagnitude > maxCorpseLaunchSpeed * maxCorpseLaunchSpeed)
                rootBody.linearVelocity = velocity.normalized * maxCorpseLaunchSpeed;
        }

        private void OnActiveRagdollRequested(vDamage damage)
        {
            ActivateCorpseRagdoll(damage);
        }

        private static vDamage BuildStaggerDamage(vDamage sourceDamage)
        {
            vDamage staggerDamage = sourceDamage != null ? new vDamage(sourceDamage) : new vDamage();
            staggerDamage.activeRagdoll = true;
            staggerDamage.hitReaction = false;
            staggerDamage.force = ResolveStaggerImpulse(sourceDamage);
            return staggerDamage;
        }

        private static vDamage BuildCorpseDamage(vDamage sourceDamage)
        {
            if (sourceDamage == null)
                return null;

            vDamage corpseDamage = new vDamage(sourceDamage);
            corpseDamage.activeRagdoll = true;
            corpseDamage.hitReaction = false;
            if (corpseDamage.force.sqrMagnitude < 0.01f && sourceDamage.sender != null)
            {
                Vector3 direction = sourceDamage.hitPosition - sourceDamage.sender.position;
                if (direction.sqrMagnitude > 0.01f)
                    corpseDamage.force = direction.normalized * Mathf.Max(1f, corpseDamage.damageValue * 0.15f);
            }

            return corpseDamage;
        }

        private static Vector3 ResolveStaggerImpulse(vDamage sourceDamage)
        {
            if (sourceDamage == null)
                return Vector3.zero;

            if (sourceDamage.force.sqrMagnitude > 0.01f)
                return sourceDamage.force * 0.35f;

            if (sourceDamage.sender == null || sourceDamage.hitPosition == Vector3.zero)
                return Vector3.zero;

            Vector3 direction = sourceDamage.hitPosition - sourceDamage.sender.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                return Vector3.zero;

            return direction.normalized * Mathf.Clamp(sourceDamage.damageValue * 0.08f, 0.5f, 4f);
        }

        private IEnumerator SettleCorpseVelocities()
        {
            for (int i = 0; i < 3; i++)
                yield return new WaitForFixedUpdate();

            Rigidbody rootBody = GetComponent<Rigidbody>();
            Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];
                if (body == null || body == rootBody)
                    continue;

                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
    }
}

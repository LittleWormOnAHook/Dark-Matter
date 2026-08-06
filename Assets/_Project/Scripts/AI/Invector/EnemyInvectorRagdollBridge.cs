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
        [Tooltip("Legacy soft-ragdoll stumble duration (only used when Prefer Animator Soft Hits is off).")]
        [SerializeField] private float defaultStaggerSeconds = 0.28f;
        [SerializeField] private bool staggerOnCritical = true;
        [Tooltip("Non-crit soft hits use Invector TriggerReaction / animator flinch instead of ActiveRagdoll.")]
        [SerializeField] private bool preferAnimatorSoftHits = true;
        [Tooltip("Horizontal impulse applied on crit knockdown (meters/sec feel). Keep low for a natural tip.")]
        [SerializeField] private float knockdownImpulse = 0.28f;
        [Tooltip("Soft-ragdoll impulse when Prefer Animator Soft Hits is off.")]
        [SerializeField] private float softStaggerImpulse = 0.12f;

        [Header("Reactive Hit Chance (Humanoid)")]
        [Tooltip("Base chance of a subtle hit reaction (~1 in 4 hits).")]
        [SerializeField, Range(0f, 1f)] private float baseReactionChance = 0.25f;
        [Tooltip("Critical hits multiply base reaction chance (1.5 = +50%).")]
        [SerializeField, Range(1f, 3f)] private float criticalReactionChanceMultiplier = 1.5f;
        [Tooltip("When a critical reaction triggers, play knockdown + get-up instead of a short flinch.")]
        [SerializeField] private bool criticalReactionKnocksDown = true;
        [SerializeField] private float knockdownDownSeconds = 1.65f;
        [SerializeField] private float knockdownGetUpTimeout = 2.75f;
        [Tooltip("Max bone speed during soft/crit ragdoll reactions (lower = less flop).")]
        [SerializeField] private float maxReactionBoneSpeed = 1.15f;

        [Header("Ragdoll Launch Guard")]
        [Tooltip("Max bone linear speed kept after death settle. Higher values look like a launch.")]
        [SerializeField] private float maxCorpseBoneSpeed = 2.5f;
        [Tooltip("PhysX depenetration cap on ragdoll bones — high defaults explode overlapping colliders.")]
        [SerializeField] private float maxBoneDepenetrationSpeed = 1.5f;

        private vThirdPersonController _controller;
        private vRagdoll _ragdoll;
        private EnemyHealth _health;
        private EnemyInvectorMotorBridge _motorBridge;
        private EnemyInvectorPhysicsCache _physicsCache;
        private Coroutine _staggerRoutine;
        private bool _isHitStaggerActive;
        private bool _isKnockdownActive;
        private vDamage _pendingCorpseDamage;

        public vRagdoll Ragdoll => _ragdoll;

        public bool IsCorpseRagdolled =>
            _ragdoll != null &&
            _ragdoll.keepRagdolled &&
            (_ragdoll.isActive || (_controller != null && _controller.ragdolled));

        public bool IsHitStaggerActive => _isHitStaggerActive;

        public bool IsKnockdownActive => _isKnockdownActive;

        public bool HasActiveRagdoll =>
            _ragdoll != null &&
            (_ragdoll.isActive || (_controller != null && _controller.ragdolled));

        /// <summary>
        /// True when avatar hips have enough bone rigidbodies for Invector ragdoll to move the mesh.
        /// </summary>
        public bool HasUsableRagdollRig =>
            EnemyInvectorRagdollRigRepair.HasUsableRagdollUnderAvatar(gameObject);

        private void Awake()
        {
            _controller = GetComponent<vThirdPersonController>();
            _health = GetComponent<EnemyHealth>();
            _motorBridge = GetComponent<EnemyInvectorMotorBridge>();
            _physicsCache = GetComponent<EnemyInvectorPhysicsCache>();
            EnemyInvectorRagdollRigRepair.TryRemountOrphanRagdollOntoAvatar(gameObject);
            _ragdoll = EnemyInvectorRagdollSetup.EnsurePresent(gameObject);
            EnemyInvectorRagdollSetup.ConfigureForCorpse(_ragdoll);
            _physicsCache?.Refresh();
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
            _isKnockdownActive = false;
            PauseAiLocomotion(false);
            PrepareAnimatorForBodyPartLoad();
            EnsureBodyPartsLoaded();
            if (_ragdoll != null)
            {
                _ragdoll.keepRagdolled = true;
                _ragdoll.ignoreGetUpAnimation = true;
            }
        }

        /// <summary>
        /// Stores hit impulse from the killing blow so <see cref="ActivateCorpseRagdoll"/> can launch
        /// the corpse even when death is orchestrated on a later event (EnemyDeathSequence).
        /// Ranged kills use this because they never go through <see cref="TryHitStagger"/>.
        /// </summary>
        public void RememberHitForDeath(
            Vector3 hitPoint,
            Vector3 hitDirection,
            float damageAmount,
            Transform sender = null)
        {
            _pendingCorpseDamage = new vDamage(Mathf.Max(1, Mathf.RoundToInt(damageAmount)))
            {
                activeRagdoll = true,
                hitReaction = false,
                hitPosition = hitPoint,
                sender = sender,
                force = Vector3.zero
            };
        }

        public void RememberHitForDeath(vDamage sourceDamage)
        {
            if (sourceDamage == null)
                return;

            _pendingCorpseDamage = new vDamage(sourceDamage)
            {
                activeRagdoll = true,
                hitReaction = false,
                force = Vector3.zero
            };
        }

        /// <summary>
        /// Ranged-friendly entry: builds a light vDamage from shot data then runs the shared stagger roll.
        /// </summary>
        public void TryHitStaggerFromRanged(
            Vector3 hitPoint,
            Vector3 hitDirection,
            float pioneerDamage,
            bool isCritical,
            Transform sender = null)
        {
            Vector3 flat = hitDirection;
            flat.y = 0f;
            vDamage rangedDamage = new vDamage(Mathf.Max(1, Mathf.RoundToInt(pioneerDamage)))
            {
                activeRagdoll = false,
                hitReaction = false,
                hitPosition = hitPoint,
                sender = sender,
                force = flat.sqrMagnitude > 0.01f ? flat.normalized * softStaggerImpulse : Vector3.zero
            };
            TryHitStagger(rangedDamage, pioneerDamage, isCritical, weaponRequestsStagger: false, weaponStaggerSeconds: 0f);
        }

        /// <summary>
        /// Soft hit flinch (animator by default), or knockdown + get-up on a critical reaction.
        /// Humanoid-only; crits raise chance by <see cref="criticalReactionChanceMultiplier"/>.
        /// Prefabs without a usable avatar ragdoll always use animator reactions — soft-ragdoll on
        /// those rigs freezes the pose and AI.
        /// </summary>
        public void TryHitStagger(
            vDamage sourceDamage,
            float pioneerDamage,
            bool isCritical,
            bool weaponRequestsStagger,
            float weaponStaggerSeconds)
        {
            if (!enableHitStagger || _controller == null)
                return;

            if (_health != null && _health.IsDead)
                return;

            if (_controller.isDead || IsCorpseRagdolled)
                return;

            // Already reacting / knocked down — damage still applies via bone proxies; skip stacking.
            if (_staggerRoutine != null)
                return;

            if (!ShouldTriggerHitReaction(isCritical, weaponRequestsStagger))
                return;

            bool knockDown = criticalReactionKnocksDown && isCritical && staggerOnCritical;
            bool canRagdoll = _ragdoll != null && HasUsableRagdollRig && EnsureBodyPartsLoaded();

            // Soft hits: animator flinch by default (no ActiveRagdoll snap/flop).
            if (!knockDown)
            {
                if (preferAnimatorSoftHits || !canRagdoll)
                {
                    PlayAnimatorHitReaction(sourceDamage, isCritical);
                    return;
                }

                float duration = weaponStaggerSeconds > 0f ? weaponStaggerSeconds : defaultStaggerSeconds;
                duration = Mathf.Clamp(duration, 0.18f, 0.4f);
                vDamage staggerDamage = BuildStaggerDamage(sourceDamage, softStaggerImpulse);
                _staggerRoutine = StartCoroutine(HitStaggerRoutine(staggerDamage, duration));
                return;
            }

            if (!canRagdoll)
            {
                PlayAnimatorHitReaction(sourceDamage, isCritical);
                return;
            }

            vDamage knockdownDamage = BuildStaggerDamage(sourceDamage, knockdownImpulse);
            _staggerRoutine = StartCoroutine(HitKnockdownRoutine(knockdownDamage, knockdownDownSeconds));
        }

        /// <summary>
        /// Subtle Mecanim flinch via Invector TriggerReaction — default soft-hit path for humanoids
        /// (including android). Avoids ActiveRagdoll snap-in / snap-out.
        /// </summary>
        private void PlayAnimatorHitReaction(vDamage sourceDamage, bool isCritical)
        {
            Animator animator = _controller != null ? _controller.animator : GetComponentInChildren<Animator>(true);
            if (animator == null || !animator.enabled)
                return;

            if (sourceDamage != null && sourceDamage.sender != null && HasAnimatorParam(animator, "HitDirection"))
            {
                Vector3 toSender = sourceDamage.sender.position - transform.position;
                float angle = Vector3.SignedAngle(transform.forward, toSender, Vector3.up);
                int hitDir = angle > 45f ? 1 : angle < -45f ? 3 : 0;
                if (Mathf.Abs(angle) > 135f)
                    hitDir = 2;
                animator.SetInteger("HitDirection", hitDir);
            }

            if (HasAnimatorParam(animator, "ReactionID"))
                animator.SetInteger("ReactionID", isCritical ? 1 : 0);

            if (HasAnimatorParam(animator, "TriggerReaction"))
            {
                animator.ResetTrigger("TriggerReaction");
                animator.SetTrigger("TriggerReaction");
            }

            if (HasAnimatorParam(animator, "ResetState"))
            {
                animator.ResetTrigger("ResetState");
                animator.SetTrigger("ResetState");
            }
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

        private bool ShouldTriggerHitReaction(bool isCritical, bool weaponRequestsStagger)
        {
            if (weaponRequestsStagger)
                return true;

            float chance = Mathf.Clamp01(baseReactionChance);
            if (staggerOnCritical && isCritical)
                chance = Mathf.Clamp01(chance * criticalReactionChanceMultiplier);

            return Random.value <= chance;
        }

        public void ActivateCorpseRagdoll(vDamage damage = null)
        {
            if (_controller == null)
                return;

            if (IsCorpseRagdolled)
                return;

            if (damage == null)
                damage = _pendingCorpseDamage;
            _pendingCorpseDamage = null;

            // Incomplete avatar ragdoll: do not ActivateRagdoll — it disables animator while
            // bodyParts stay empty and the corpse freezes mid-pose with no rigidbody collapse.
            if (_ragdoll == null || !HasUsableRagdollRig)
            {
                AbortHitStaggerCoroutine();
                GetComponent<EnemyInvectorLoadoutBridge>()?.DropHeldWeaponOnDeath();
                _controller.moveDirection = Vector3.zero;
                _controller.input = Vector3.zero;
                _controller.isSprinting = false;
                _controller.StopCharacter();
                if (_motorBridge != null)
                    _motorBridge.enabled = false;
                PrepareAnimatorForBodyPartLoad();
                PlayAnimatorDeathFallback();
                return;
            }

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
            PrepareBonesForSafeRagdoll();

            _ragdoll.keepRagdolled = true;
            _ragdoll.ignoreGetUpAnimation = true;
            // Unity 6 cannot clear kinematic root linearVelocity — disable inheritance instead.
            _ragdoll.horizontalMultiplier = 0f;
            _ragdoll.verticalMultiplier = 0f;

            // Distance culling can leave Animator disabled (empty bodyParts). Un-cull and reload
            // before ActivateRagdoll — otherwise setKinematic iterates nothing and the corpse freezes.
            PrepareAnimatorForBodyPartLoad();
            if (!EnsureBodyPartsLoaded())
            {
                StartCoroutine(ActivateCorpseRagdollWhenBodyPartsReady(damage));
                return;
            }

            // If a hit-stagger was already active, ActivateRagdoll early-outs on isActive. Keep the
            // live ragdoll as the corpse, but strip residual bone speeds so the body collapses in place.
            bool alreadyRagdolled = _ragdoll.isActive;
            if (!alreadyRagdolled)
                _ragdoll.ActivateRagdoll(BuildCorpseDamage(damage));

            ZeroBoneVelocitiesImmediate();
            StartCoroutine(SettleCorpseVelocities());
        }

        private void PlayAnimatorDeathFallback()
        {
            if (_controller == null)
                return;

            if (_controller is vHealthController healthController)
            {
                healthController.isImmortal = false;
                if (healthController.currentHealth > 0f)
                    healthController.ChangeHealth(0);
            }

            if (!_controller.isDead)
                _controller.isDead = true;

            _controller.disableAnimations = false;
            Animator animator = _controller.animator;
            if (animator == null)
                return;

            animator.enabled = true;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (HasAnimatorParam(animator, "isDead"))
                animator.SetBool("isDead", true);
            if (HasAnimatorParam(animator, "InputMagnitude"))
                animator.SetFloat("InputMagnitude", 0f);
            if (HasAnimatorParam(animator, "InputHorizontal"))
                animator.SetFloat("InputHorizontal", 0f);
            if (HasAnimatorParam(animator, "InputVertical"))
                animator.SetFloat("InputVertical", 0f);
        }

        private IEnumerator ActivateCorpseRagdollWhenBodyPartsReady(vDamage damage)
        {
            PrepareAnimatorForBodyPartLoad();

            int guard = 0;
            while (!EnsureBodyPartsLoaded() && guard < 30)
            {
                guard++;
                PrepareAnimatorForBodyPartLoad();
                yield return null;
            }

            if (_ragdoll == null || _controller == null)
                yield break;

            // keepRagdolled was already set true by ActivateCorpseRagdoll — do not bail on
            // IsCorpseRagdolled or mid-stagger deaths would skip velocity settle entirely.
            if (!EnsureBodyPartsLoaded())
            {
                Debug.LogWarning(
                    $"{name}: ragdoll body parts unavailable at death after retry " +
                    "(humanoid avatar not bound, animator culled, or no ragdoll rig); playing death anim fallback.",
                    this);
                PlayAnimatorDeathFallback();
                yield break;
            }

            PrepareBonesForSafeRagdoll();
            _ragdoll.horizontalMultiplier = 0f;
            _ragdoll.verticalMultiplier = 0f;

            bool alreadyRagdolled = _ragdoll.isActive;
            if (!alreadyRagdolled)
                _ragdoll.ActivateRagdoll(BuildCorpseDamage(damage));

            ZeroBoneVelocitiesImmediate();
            StartCoroutine(SettleCorpseVelocities());
        }

        public void RestoreForRespawn()
        {
            StopHitStaggerRoutine();
            _pendingCorpseDamage = null;

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
            _isKnockdownActive = false;
            PauseAiLocomotion(true);

            if (_motorBridge != null)
                _motorBridge.enabled = false;

            _physicsCache?.MarkBonesUnstable();
            EnemyInvectorHitSetup.ReleaseForRagdoll(gameObject);
            PrepareBonesForSafeRagdoll();

            // Soft stumble: no root-velocity inheritance; keep duration short and bone speeds low.
            _ragdoll.horizontalMultiplier = 0f;
            _ragdoll.verticalMultiplier = 0f;
            _ragdoll.keepRagdolled = false;
            _ragdoll.ignoreGetUpAnimation = true;
            _ragdoll.ActivateRagdoll(staggerDamage, duration);
            ZeroBoneVelocitiesImmediate();
            ClampBoneSpeeds(maxReactionBoneSpeed);

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

                ClampBoneSpeeds(maxReactionBoneSpeed);

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_health == null || !_health.IsDead)
                RestoreFromHitStagger();

            _staggerRoutine = null;
            _isHitStaggerActive = false;
            PauseAiLocomotion(false);
        }

        /// <summary>
        /// Last-10% knockdown: stay down, then let Invector blend into StandUp and resume AI.
        /// </summary>
        private IEnumerator HitKnockdownRoutine(vDamage knockdownDamage, float downSeconds)
        {
            _isHitStaggerActive = true;
            _isKnockdownActive = true;
            PauseAiLocomotion(true);

            if (_motorBridge != null)
                _motorBridge.enabled = false;

            _physicsCache?.MarkBonesUnstable();
            EnemyInvectorHitSetup.ReleaseForRagdoll(gameObject);
            PrepareBonesForSafeRagdoll();

            // Allow StandUp@FromBack / FromBelly when keepRagdolled clears after downSeconds.
            // Keep down long enough for Invector stabilizer (~1.6s+) without a long floor flop.
            float keepDown = Mathf.Clamp(Mathf.Max(downSeconds, 1.55f), 1.4f, 2.1f);
            _ragdoll.horizontalMultiplier = 0f;
            _ragdoll.verticalMultiplier = 0f;
            _ragdoll.keepRagdolled = false;
            _ragdoll.ignoreGetUpAnimation = false;
            _ragdoll.ActivateRagdoll(knockdownDamage, keepDown);
            ZeroBoneVelocitiesImmediate();
            ClampBoneSpeeds(maxReactionBoneSpeed);

            float elapsed = 0f;
            while (elapsed < keepDown)
            {
                if (_health != null && _health.IsDead)
                {
                    _staggerRoutine = null;
                    _isHitStaggerActive = false;
                    _isKnockdownActive = false;
                    PauseAiLocomotion(false);
                    yield break;
                }

                if (elapsed < 0.45f)
                    ClampBoneSpeeds(maxReactionBoneSpeed);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Wait for natural get-up (ragdolled=false → StandUp) or timeout, then harden cleanup.
            elapsed = 0f;
            while (elapsed < knockdownGetUpTimeout)
            {
                if (_health != null && _health.IsDead)
                {
                    _staggerRoutine = null;
                    _isHitStaggerActive = false;
                    _isKnockdownActive = false;
                    PauseAiLocomotion(false);
                    yield break;
                }

                bool stillDown = _ragdoll != null &&
                                 (_ragdoll.isActive || (_controller != null && _controller.ragdolled));
                if (!stillDown)
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_health == null || !_health.IsDead)
            {
                if (_ragdoll != null && (_ragdoll.isActive || (_controller != null && _controller.ragdolled)))
                    RestoreFromHitStagger();
                else
                    FinalizeAfterGetUp();
            }

            _staggerRoutine = null;
            _isHitStaggerActive = false;
            _isKnockdownActive = false;
            PauseAiLocomotion(false);
        }

        private void RestoreFromHitStagger()
        {
            if (_health != null && _health.IsDead)
                return;
            if (_ragdoll != null)
            {
                _ragdoll.keepRagdolled = false;
                _ragdoll.ignoreGetUpAnimation = true;
                _ragdoll.RestoreRagdoll();
            }

            if (_controller != null && _controller.ragdolled)
                _controller.ResetRagdoll();

            FinalizeAfterGetUp();
        }

        private void FinalizeAfterGetUp()
        {
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
            _isKnockdownActive = false;
            PauseAiLocomotion(false);
        }

        private void StopHitStaggerRoutine()
        {
            if (_staggerRoutine == null)
                return;

            AbortHitStaggerCoroutine();
            _isHitStaggerActive = false;
            _isKnockdownActive = false;
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
            // Re-enable Animator each attempt if distance culling turned it off mid-window.
            int guard = 0;
            while (!EnsureBodyPartsLoaded() && guard < 120)
            {
                guard++;
                EnsureAnimatorEnabledForBodyPartLoad();
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
            return HasLoadedBodyParts();
        }

        private bool HasLoadedBodyParts()
        {
            if (_ragdoll == null)
                return false;

            // bodyParts is a private List on vRagdoll; count > 0 means LoadBodyPart succeeded.
            System.Reflection.FieldInfo field = typeof(vRagdoll).GetField(
                "bodyParts",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
            if (field?.GetValue(_ragdoll) is ICollection collection)
                return collection.Count > 0;

            return AnimatorHipsReady();
        }

        /// <summary>
        /// Distance-culled enemies keep Animator disabled; hips then fail isHuman/GetBoneTransform
        /// checks and LoadBodyPart never fills bodyParts. Force visibility + animator on before load.
        /// </summary>
        private void PrepareAnimatorForBodyPartLoad()
        {
            HumanoidPerformanceController performance = GetComponent<HumanoidPerformanceController>();
            performance?.ForceVisibleForDeathPresentation();
            EnsureAnimatorEnabledForBodyPartLoad();
        }

        private void EnsureAnimatorEnabledForBodyPartLoad()
        {
            Animator animator = _controller != null ? _controller.animator : GetComponentInChildren<Animator>(true);
            if (animator != null && !animator.enabled)
                animator.enabled = true;
        }

        private bool AnimatorHipsReady()
        {
            Animator animator = _controller != null ? _controller.animator : GetComponentInChildren<Animator>(true);
            if (animator == null)
                return false;

            if (!animator.enabled)
                animator.enabled = true;

            return animator.isHuman && animator.GetBoneTransform(HumanBodyBones.Hips) != null;
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
        /// Caps PhysX depenetration on bone bodies. Oversized / overlapping ragdoll colliders
        /// otherwise explode the corpse off-map on the first FixedUpdate after activation.
        /// </summary>
        private void PrepareBonesForSafeRagdoll()
        {
            float maxDepenetration = Mathf.Max(0.25f, maxBoneDepenetrationSpeed);
            Rigidbody rootBody = GetComponent<Rigidbody>();
            Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];
                if (body == null || body == rootBody)
                    continue;

                body.maxDepenetrationVelocity = maxDepenetration;
                body.sleepThreshold = 0.005f;
            }
        }

        private void ZeroBoneVelocitiesImmediate()
        {
            Rigidbody rootBody = GetComponent<Rigidbody>();
            Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];
                if (body == null || body == rootBody || body.isKinematic)
                    continue;

                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private void OnActiveRagdollRequested(vDamage damage)
        {
            ActivateCorpseRagdoll(damage);
        }

        private vDamage BuildStaggerDamage(vDamage sourceDamage, float impulseStrength)
        {
            vDamage staggerDamage = sourceDamage != null ? new vDamage(sourceDamage) : new vDamage();
            staggerDamage.activeRagdoll = true;
            staggerDamage.hitReaction = false;
            staggerDamage.force = ResolveStaggerImpulse(sourceDamage, impulseStrength);
            return staggerDamage;
        }

        private static vDamage BuildCorpseDamage(vDamage sourceDamage)
        {
            // Collapse in place. Any kill impulse plus PhysX depenetration is what made corpses vanish.
            if (sourceDamage == null)
            {
                return new vDamage(1)
                {
                    activeRagdoll = true,
                    hitReaction = false,
                    force = Vector3.zero
                };
            }

            vDamage corpseDamage = new vDamage(sourceDamage)
            {
                activeRagdoll = true,
                hitReaction = false,
                force = Vector3.zero
            };
            return corpseDamage;
        }

        private static Vector3 ResolveStaggerImpulse(vDamage sourceDamage, float impulseStrength)
        {
            float strength = Mathf.Max(0f, impulseStrength);
            if (strength <= 0f || sourceDamage == null)
                return Vector3.zero;

            Vector3 direction = sourceDamage.force;
            if (direction.sqrMagnitude < 0.01f &&
                sourceDamage.sender != null &&
                sourceDamage.hitPosition != Vector3.zero)
            {
                direction = sourceDamage.hitPosition - sourceDamage.sender.position;
            }

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
                return Vector3.zero;

            // Subtle tip only — high impulse + PhysX made soft hits look like full-body launches.
            return direction.normalized * strength;
        }

        private IEnumerator SettleCorpseVelocities()
        {
            // Strip launch spikes for several physics steps after activation / mid-stagger conversion.
            for (int i = 0; i < 8; i++)
            {
                ZeroBoneVelocitiesImmediate();
                ClampBoneSpeeds(maxCorpseBoneSpeed);
                yield return new WaitForFixedUpdate();
            }
        }

        private void ClampBoneSpeeds(float maxBoneSpeed)
        {
            float maxSpeed = Mathf.Max(0.5f, maxBoneSpeed);
            float maxSpeedSqr = maxSpeed * maxSpeed;
            Rigidbody rootBody = GetComponent<Rigidbody>();
            Rigidbody[] bodies = GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];
                if (body == null || body == rootBody || body.isKinematic)
                    continue;

                Vector3 velocity = body.linearVelocity;
                if (velocity.sqrMagnitude > maxSpeedSqr)
                    body.linearVelocity = velocity.normalized * maxSpeed;

                Vector3 angular = body.angularVelocity;
                if (angular.sqrMagnitude > 16f)
                    body.angularVelocity = angular.normalized * 4f;
            }
        }
    }
}

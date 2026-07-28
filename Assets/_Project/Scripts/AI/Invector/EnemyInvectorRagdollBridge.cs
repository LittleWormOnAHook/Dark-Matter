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
        [Tooltip("How long a minor stumble keeps the enemy ragdolled before snap-recover.")]
        [SerializeField] private float defaultStaggerSeconds = 1.55f;
        [SerializeField] private bool staggerOnCritical = true;

        [Header("Reactive Hit Chance")]
        [Tooltip("Random minor stumble chance at full health (melee and ranged).")]
        [SerializeField, Range(0f, 1f)] private float baseStaggerChance = 0.06f;
        [Tooltip("Random minor stumble chance as health approaches the knockdown band.")]
        [SerializeField, Range(0f, 1f)] private float lowHealthStaggerChance = 0.28f;
        [Tooltip("Remaining health fraction at or below this triggers a full fall + get-up.")]
        [SerializeField, Range(0.02f, 0.35f)] private float knockdownHealthThreshold = 0.10f;
        [SerializeField] private float knockdownDownSeconds = 2.25f;
        [SerializeField] private float knockdownGetUpTimeout = 3.5f;
        [SerializeField] private float criticalStaggerChanceBonus = 0.15f;

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
                force = flat.sqrMagnitude > 0.01f ? flat.normalized * 0.55f : Vector3.zero
            };
            TryHitStagger(rangedDamage, pioneerDamage, isCritical, weaponRequestsStagger: false, weaponStaggerSeconds: 0f);
        }

        /// <summary>
        /// Brief stumble (or low-HP knockdown + get-up). Chance rises as health drops; at or below
        /// <see cref="knockdownHealthThreshold"/> the enemy falls and stands back up.
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

            float healthFraction = ResolveHealthFraction();
            bool knockdownBand = healthFraction > 0f && healthFraction <= knockdownHealthThreshold;
            if (knockdownBand)
            {
                vDamage knockdownDamage = BuildStaggerDamage(sourceDamage);
                _staggerRoutine = StartCoroutine(HitKnockdownRoutine(knockdownDamage, knockdownDownSeconds));
                return;
            }

            if (!ShouldRollMinorStagger(healthFraction, pioneerDamage, isCritical, weaponRequestsStagger))
                return;

            float duration = weaponStaggerSeconds > 0f ? weaponStaggerSeconds : defaultStaggerSeconds;
            // Slightly longer stumbles when hurt.
            duration *= Mathf.Lerp(1f, 1.2f, 1f - healthFraction);
            duration = Mathf.Max(duration, 1.35f);
            vDamage staggerDamage = BuildStaggerDamage(sourceDamage);
            _staggerRoutine = StartCoroutine(HitStaggerRoutine(staggerDamage, duration));
        }

        private bool ShouldRollMinorStagger(
            float healthFraction,
            float pioneerDamage,
            bool isCritical,
            bool weaponRequestsStagger)
        {
            if (weaponRequestsStagger)
                return true;

            // Map full→knockdown-threshold onto 0→1 so chance ramps across the fight, not only near death.
            float liveSpan = Mathf.Max(0.01f, 1f - knockdownHealthThreshold);
            float ramp = healthFraction > knockdownHealthThreshold
                ? Mathf.Clamp01((1f - healthFraction) / liveSpan)
                : 1f;

            float chance = Mathf.Lerp(baseStaggerChance, lowHealthStaggerChance, ramp);
            if (staggerOnCritical && isCritical)
                chance = Mathf.Clamp01(chance + criticalStaggerChanceBonus);
            if (pioneerDamage >= minStaggerDamage)
                chance = Mathf.Clamp01(chance + 0.2f);

            return Random.value <= chance;
        }

        private float ResolveHealthFraction()
        {
            if (_health == null || _health.MaxHealth <= 0.01f)
                return 1f;

            return Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth);
        }

        public void ActivateCorpseRagdoll(vDamage damage = null)
        {
            if (_controller == null || _ragdoll == null)
                return;

            if (IsCorpseRagdolled)
                return;

            if (damage == null)
                damage = _pendingCorpseDamage;
            _pendingCorpseDamage = null;

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
                    "(humanoid avatar not bound, animator culled, or no ragdoll rig); corpse may not ragdoll.",
                    this);
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

            // Soft stumble: no root-velocity inheritance, snap recover without StandUp.
            _ragdoll.horizontalMultiplier = 0f;
            _ragdoll.verticalMultiplier = 0f;
            _ragdoll.keepRagdolled = false;
            _ragdoll.ignoreGetUpAnimation = true;
            _ragdoll.ActivateRagdoll(staggerDamage, duration);
            ZeroBoneVelocitiesImmediate();

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

                // Keep minor staggers from PhysX-popping mid stumble.
                if (elapsed < 0.2f)
                    ClampBoneSpeeds(maxCorpseBoneSpeed);

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
            // vRagdoll's stabilizer runs ~2s; keep them down at least that long so get-up can start.
            float keepDown = Mathf.Max(downSeconds, 2.15f);
            _ragdoll.horizontalMultiplier = 0f;
            _ragdoll.verticalMultiplier = 0f;
            _ragdoll.keepRagdolled = false;
            _ragdoll.ignoreGetUpAnimation = false;
            _ragdoll.ActivateRagdoll(knockdownDamage, keepDown);
            ZeroBoneVelocitiesImmediate();

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

                if (elapsed < 0.35f)
                    ClampBoneSpeeds(maxCorpseBoneSpeed);

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

        private static vDamage BuildStaggerDamage(vDamage sourceDamage)
        {
            vDamage staggerDamage = sourceDamage != null ? new vDamage(sourceDamage) : new vDamage();
            staggerDamage.activeRagdoll = true;
            staggerDamage.hitReaction = false;
            // Soft flinch only — no dramatic launch.
            staggerDamage.force = ResolveStaggerImpulse(sourceDamage);
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

        private static Vector3 ResolveStaggerImpulse(vDamage sourceDamage)
        {
            if (sourceDamage == null)
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

            // Gentle flinch — enough to tip, not enough to throw.
            return direction.normalized * 0.55f;
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

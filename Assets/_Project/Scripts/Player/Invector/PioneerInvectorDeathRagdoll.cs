using System.Collections;



using Invector.vCharacterController;

using Project.Core;
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

        private bool _pinStandUp;

        private Vector3 _standUpPinPosition;

        private Quaternion _standUpPinRotation;

        private Animator _standUpAnimator;



        private void Awake()



        {



            EnsureReferences();



        }



        public void ActivateDeathRagdoll()



        {



            EnsureReferences();
            StartCorpseFreeze(); // 2026-10-13 (i)



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



            _controller.deathBy = vCharacter.DeathBy.Ragdoll;



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
            ReleaseCorpseFreeze(); // 2026-10-13 (i)
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

            _pinStandUp = false;
            _standUpAnimator = null;

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
                _controller.isJumping = false;
                _controller.StopCharacter();
                _controller.EnableGravityAndCollision();
            }

            Animator animator = ResolveAnimator();
            if (animator != null)
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
            }

            ResolveTpInput()?.EnableOnAnimatorMove();
            GetComponent<PioneerInvectorBootstrap>()?.EnsureInvectorPhysicsReady();
            StartRespawnSettle(); // 2026-10-14 (a)
        }

        // 2026-10-14 (a): Retry slide fix. The corpse freeze released the ragdoll bones back to their death-time
        // (non-kinematic) flags, so after Retry live physics bones stayed under the animated body, pushed the capsule
        // and kept it off the ground (slippy friction) - the player skated. Now bones go back to Invector's alive
        // state (kinematic) and the root's leftover velocity is zeroed for DM_CombatCoreProfile.playerRespawnSettleSeconds.
        private Coroutine _respawnSettleRoutine;

        private void StartRespawnSettle()
        {
            if (_respawnSettleRoutine != null)
                StopCoroutine(_respawnSettleRoutine);
            _respawnSettleRoutine = StartCoroutine(RespawnSettleRoutine());
        }

        private IEnumerator RespawnSettleRoutine()
        {
            Project.Combat.DM_CombatCoreProfile p = Project.Combat.DM_CombatCoreProfile.Live;
            float settle = p != null ? p.playerRespawnSettleSeconds : 0.6f;
            float until = Time.time + settle;
            do
            {
                KillBodyMotion();
                yield return new WaitForFixedUpdate();
            }
            while (Time.time < until);
            KillBodyMotion();
            _respawnSettleRoutine = null;
        }

        private void KillBodyMotion()
        {
            Rigidbody root = GetComponent<Rigidbody>();
            Animator bodyAnimator = ResolveAnimator();
            Transform hips = bodyAnimator != null && bodyAnimator.isHuman ? bodyAnimator.GetBoneTransform(HumanBodyBones.Hips) : null;
            foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
            {
                if (rb == null)
                    continue;
                if (rb == root)
                {
                    if (!rb.isKinematic)
                    {
                        Vector3 v = rb.linearVelocity;
                        rb.linearVelocity = new Vector3(0f, Mathf.Min(0f, v.y), 0f);
                        rb.angularVelocity = Vector3.zero;
                    }
                    continue;
                }

                // Only ragdoll bones (under the hips); leave any other dynamic bodies alone.
                if (hips == null || !(rb.transform == hips || rb.transform.IsChildOf(hips)))
                    continue;

                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = true;
                }
            }

            if (_controller != null && !_controller.ragdolled)
                _controller.StopCharacter();
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

            // StandUp clips carry Invector's LockMovement tag. That path writes
            // transform = animator.rootPosition every frame and drags the player
            // across the map after ragdoll (stale / scaled root curves).
            animator.applyRootMotion = false;
            vThirdPersonInput tpInput = ResolveTpInput();
            tpInput?.DisableOnAnimatorMove();

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

            Transform root = _controller != null ? _controller.transform : transform;
            _standUpPinPosition = root.position;
            _standUpPinRotation = root.rotation;
            _standUpAnimator = animator;
            _pinStandUp = true;

            if (_controller != null)
            {
                _controller.lockMovement = true;
                _controller.lockAnimMovement = false;
                _controller.isJumping = false;
                _controller.StopCharacter();
            }

            float elapsed = 0f;
            while (elapsed < GetUpTimeoutSeconds)
            {
                if (animator == null)
                    break;

                PinStandUpPose();

                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layer);
                bool inStand = info.shortNameHash == stateHash || info.IsName(stateName);
                if (elapsed > 0.2f && !inStand && !animator.IsInTransition(layer))
                    break;

                if (inStand && info.normalizedTime >= 0.92f && !animator.IsInTransition(layer))
                    break;

                elapsed += Time.deltaTime;
                yield return null;
            }

            PinStandUpPose();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                if (layer >= 0)
                    animator.SetLayerWeight(layer, 0f);
            }

            _pinStandUp = false;
            _standUpAnimator = null;

            if (_controller != null)
            {
                _controller.lockMovement = false;
                _controller.lockAnimMovement = false;
                _controller.StopCharacter();
            }

            tpInput?.EnableOnAnimatorMove();
            KillBodyMotion(); // 2026-10-14 (a)

            _lieCaptured = false;
            _deathPoseCaptured = false;
            _getUpRoutine = null;
        }

        private void LateUpdate()
        {
            if (_pinStandUp)
                PinStandUpPose();
        }

        private void PinStandUpPose()
        {
            Transform root = _controller != null ? _controller.transform : transform;
            if (root != null)
                root.SetPositionAndRotation(_standUpPinPosition, _standUpPinRotation);

            if (_controller != null)
            {
                _controller.StopCharacter();
                _controller.lockMovement = true;
                _controller.lockAnimMovement = false;
            }

            if (_standUpAnimator != null)
                _standUpAnimator.applyRootMotion = false;
        }

        private vThirdPersonInput ResolveTpInput()
        {
            if (_controller != null)
            {
                vThirdPersonInput onController = _controller.GetComponent<vThirdPersonInput>();
                if (onController != null)
                    return onController;
            }

            return GetComponent<vThirdPersonInput>();
        }



        private Animator ResolveAnimator()



        {



            Animator animator = GetComponentInChildren<Animator>();



            if (animator != null)



                return animator;



            GameObject player = PlayerLocator.FindPlayerObject() ?? gameObject;



            return player != null ? player.GetComponentInChildren<Animator>() : null;



        }



        // 2026-10-13 (i): the dead player slid away / off the terrain. Nothing removed the momentum the body had at death, and the
        // ragdoll bones kept taking pushes (enemy contacts, late hits) on a low-friction capsule/terrain, so it skated. Now the root
        // body is stopped and made kinematic at death, bone speeds are capped while it falls, and after settle time the whole
        // ragdoll is frozen in place until respawn (DM_CombatCoreProfile "Player corpse").
        private Coroutine _corpseFreezeRoutine;
        private readonly System.Collections.Generic.List<Rigidbody> _frozenBodies = new System.Collections.Generic.List<Rigidbody>();
        private readonly System.Collections.Generic.List<bool> _frozenWasKinematic = new System.Collections.Generic.List<bool>();
        private Rigidbody _frozenRoot;
        private bool _frozenRootWasKinematic;

        private void StartCorpseFreeze()
        {
            Project.Combat.DM_CombatCoreProfile p = Project.Combat.DM_CombatCoreProfile.Live;
            if (p != null && !p.playerCorpseStayInPlace)
                return;
            if (_corpseFreezeRoutine != null)
                StopCoroutine(_corpseFreezeRoutine);
            _corpseFreezeRoutine = StartCoroutine(CorpseFreezeRoutine());
        }

        private IEnumerator CorpseFreezeRoutine()
        {
            Project.Combat.DM_CombatCoreProfile p = Project.Combat.DM_CombatCoreProfile.Live;
            float settle = p != null ? p.playerCorpseSettleSeconds : 1.5f;
            float maxSlide = p != null ? p.playerCorpseMaxSlideSpeed : 1.5f;

            Rigidbody root = GetComponent<Rigidbody>();
            if (root != null && _frozenRoot == null)
            {
                _frozenRoot = root;
                _frozenRootWasKinematic = root.isKinematic;
                if (!root.isKinematic) { root.linearVelocity = Vector3.zero; root.angularVelocity = Vector3.zero; }
                root.isKinematic = true;
            }

            float until = Time.time + settle;
            while (Time.time < until)
            {
                foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
                {
                    if (rb == null || rb == root || rb.isKinematic) continue;
                    Vector3 v = rb.linearVelocity;
                    Vector3 flat = new Vector3(v.x, 0f, v.z);
                    if (flat.magnitude > maxSlide)
                    {
                        flat = flat.normalized * maxSlide;
                        rb.linearVelocity = new Vector3(flat.x, v.y, flat.z);
                    }
                }
                yield return new WaitForFixedUpdate();
            }

            _frozenBodies.Clear();
            _frozenWasKinematic.Clear();
            foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
            {
                if (rb == null || rb == root) continue;
                _frozenBodies.Add(rb);
                _frozenWasKinematic.Add(rb.isKinematic);
                if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                rb.isKinematic = true;
            }
            _corpseFreezeRoutine = null;
        }

        private void ReleaseCorpseFreeze()
        {
            if (_corpseFreezeRoutine != null)
            {
                StopCoroutine(_corpseFreezeRoutine);
                _corpseFreezeRoutine = null;
            }
            // 2026-10-14 (a): bones return to Invector's alive state (kinematic), not their death-time ragdoll flags.
            // Non-bone bodies (if any were frozen) get their saved flag back.
            Animator relAnim = ResolveAnimator();
            Transform relHips = relAnim != null && relAnim.isHuman ? relAnim.GetBoneTransform(HumanBodyBones.Hips) : null;
            for (int i = 0; i < _frozenBodies.Count; i++)
            {
                Rigidbody fb = _frozenBodies[i];
                if (fb == null) continue;
                bool bone = relHips != null && (fb.transform == relHips || fb.transform.IsChildOf(relHips));
                fb.isKinematic = bone || _frozenWasKinematic[i];
            }
            _frozenBodies.Clear();
            _frozenWasKinematic.Clear();
            if (_frozenRoot != null) _frozenRoot.isKinematic = _frozenRootWasKinematic;
            _frozenRoot = null;
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



            GameObject player = PlayerLocator.FindPlayerObject() ?? gameObject;



            if (player == null || player == gameObject)



                return;



            if (_controller == null)



                _controller = player.GetComponent<vThirdPersonController>();



            if (_ragdoll == null)



                _ragdoll = player.GetComponent<vRagdoll>() ?? player.GetComponentInChildren<vRagdoll>(true);



        }



        private IEnumerator EnsureDeathPresentation()

        {

            // Always use fall-death ragdoll; never wait on animation/idle death.

            if (_controller != null)

                _controller.deathBy = vCharacter.DeathBy.Ragdoll;



            ForceRagdollActivation();

            _ensureDeathRoutine = null;

            yield break;

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


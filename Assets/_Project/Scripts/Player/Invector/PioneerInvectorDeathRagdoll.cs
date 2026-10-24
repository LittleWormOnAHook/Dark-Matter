using System.Collections;
using System.Collections.Generic;



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

        // 2026-10-19 f: Retry slide root cause. DMLandingDirector's fall flop sets vRagdoll.disableColliders=false (and
        // verticalMultiplier 1) and nothing restored them, so after a revive ~13 bone colliders stayed solid and one bone
        // body stayed dynamic under the capsule, shoving it around (0.3-0.4 m drift after releasing input). Capture the
        // living body once after vRagdoll.Start and put all of it back in one place on revive.
        private sealed class LivingBodySnapshot
        {
            public bool disableColliders, ignoreGetUp;
            public float verticalMultiplier, horizontalMultiplier;
            public readonly List<KeyValuePair<Rigidbody, BodyState>> bodies = new List<KeyValuePair<Rigidbody, BodyState>>();
            public readonly List<KeyValuePair<Collider, Vector2Int>> colliders = new List<KeyValuePair<Collider, Vector2Int>>();
        }

        private struct BodyState
        {
            public bool kinematic, gravity, detect;
            public RigidbodyConstraints constraints;
        }

        private LivingBodySnapshot _living;

        private IEnumerator Start()
        {
            yield return null; // after vRagdoll.Start disabled the bone colliders
            yield return new WaitForFixedUpdate();
            CaptureLivingBody();
        }

        private void CaptureLivingBody()
        {
            EnsureReferences();
            if (_living != null || (_controller != null && (_controller.isDead || _controller.ragdolled)))
                return;
            var snap = new LivingBodySnapshot();
            if (_ragdoll != null)
            {
                snap.disableColliders = _ragdoll.disableColliders;
                snap.ignoreGetUp = _ragdoll.ignoreGetUpAnimation;
                snap.verticalMultiplier = _ragdoll.verticalMultiplier;
                snap.horizontalMultiplier = _ragdoll.horizontalMultiplier;
            }
            // Ragdoll bones only (under the hips): weapon hitboxes and attachments keep their own runtime state.
            Animator anim = GetComponent<Animator>();
            Transform hips = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Hips) : null;
            if (hips == null)
                return;
            foreach (Rigidbody rb in hips.GetComponentsInChildren<Rigidbody>(true))
            {
                if (rb.transform != hips && rb.GetComponent<Joint>() == null) continue;
                snap.bodies.Add(new KeyValuePair<Rigidbody, BodyState>(rb, new BodyState { kinematic = rb.isKinematic, gravity = rb.useGravity, detect = rb.detectCollisions, constraints = rb.constraints }));
            }
            // Every collider under the hips: DMLandingDirector.ApplyFallVelocityToBones makes ALL of them solid (bone
            // colliders plus the vHitBox body hitboxes and vFootStep foot triggers). x = 1 when the collider sits on a
            // ragdoll bone body (enabled state restored too); others keep their runtime enabled state, only isTrigger.
            foreach (Collider c in hips.GetComponentsInChildren<Collider>(true))
            {
                Rigidbody own = c.GetComponent<Rigidbody>();
                bool bone = own != null && (own.transform == hips || own.GetComponent<Joint>() != null);
                snap.colliders.Add(new KeyValuePair<Collider, Vector2Int>(c, new Vector2Int(bone ? (c.enabled ? 1 : 0) : -1, c.isTrigger ? 1 : 0)));
            }
            _living = snap;
        }

        /// <summary>Put ragdoll config, bone bodies and bone colliders back to the captured living state.</summary>
        public void RestoreLivingBodyState()
        {
            if (_living == null)
                return;
            if (_ragdoll != null)
            {
                _ragdoll.disableColliders = _living.disableColliders;
                _ragdoll.ignoreGetUpAnimation = _living.ignoreGetUp;
                _ragdoll.verticalMultiplier = _living.verticalMultiplier;
                _ragdoll.horizontalMultiplier = _living.horizontalMultiplier;
            }
            foreach (var b in _living.bodies)
            {
                if (b.Key == null) continue;
                if (!b.Key.isKinematic) { b.Key.linearVelocity = Vector3.zero; b.Key.angularVelocity = Vector3.zero; }
                b.Key.isKinematic = b.Value.kinematic;
                b.Key.useGravity = b.Value.gravity;
                b.Key.detectCollisions = b.Value.detect;
                b.Key.constraints = b.Value.constraints;
            }
            foreach (var c in _living.colliders)
            {
                if (c.Key == null) continue;
                c.Key.isTrigger = c.Value.y == 1;
                if (c.Value.x >= 0) c.Key.enabled = c.Value.x == 1;
            }
            // 2026-10-19 h: real-hit combat death drops/re-equips the drawn weapon with its SOLID pickup collider.
            // Parented under the player, it joins the root rigidbody's compound collider and drags/shoves the body
            // while the arm animates (slide on every move, fine idle). The living player has no solid child colliders.
            Rigidbody rootBody = GetComponent<Rigidbody>();
            Collider rootCapsule = GetComponent<Collider>();
            if (rootBody != null)
            {
                foreach (Collider c in GetComponentsInChildren<Collider>(true))
                {
                    if (c == null || c == rootCapsule || c.isTrigger || c.attachedRigidbody == null) continue;
                    if (c.attachedRigidbody != rootBody && !c.attachedRigidbody.transform.IsChildOf(transform)) continue; // own body, or bone body (hand)
                    if (c is MeshCollider cm && !cm.convex) { c.enabled = false; continue; } // 24u: concave mesh cannot be a trigger
                    c.isTrigger = true;
                }
            }
            EnsureLivingWorldCollision();
        }

        /// <summary>2026-10-24u: after revive the player ran through rocks. Force the living root back to solid:
        /// capsule enabled + solid, root body detects collisions, Player layer, and no Player-vs-world layer ignore
        /// (Default / Climbable). Logs what it had to fix ([ReviveCollision]) so the leftover source is visible.</summary>
        private void EnsureLivingWorldCollision()
        {
            if (_controller != null && (_controller.isDead || _controller.ragdolled)) return;
            string fixedWhat = "";
            CapsuleCollider cap = GetComponent<CapsuleCollider>();
            if (cap != null)
            {
                if (!cap.enabled) { cap.enabled = true; fixedWhat += " capsuleEnabled"; }
                if (cap.isTrigger) { cap.isTrigger = false; fixedWhat += " capsuleTrigger"; }
            }
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && !rb.detectCollisions) { rb.detectCollisions = true; fixedWhat += " detectCollisions"; }
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0 && gameObject.layer != playerLayer) { fixedWhat += " layer(" + LayerMask.LayerToName(gameObject.layer) + ")"; gameObject.layer = playerLayer; }
            bool climbing = GetComponent<Project.Features.Climb.DMClimbController>() is var cc && cc != null && cc.IsClimbing;
            foreach (string ln in new[] { "Default", "Climbable" })
            {
                int l = LayerMask.NameToLayer(ln);
                if (l < 0 || gameObject.layer < 0 || (climbing && ln == "Climbable")) continue;
                if (Physics.GetIgnoreLayerCollision(gameObject.layer, l)) { Physics.IgnoreLayerCollision(gameObject.layer, l, false); fixedWhat += " layerIgnore(" + ln + ")"; }
            }
            if (rb != null && rb.isKinematic && _controller != null && !_controller.lockMovement) fixedWhat += " (note: root body still kinematic)";
            if (fixedWhat.Length > 0) Debug.Log("[ReviveCollision] fixed:" + fixedWhat);
        }

        private IEnumerator ReapplyLivingBodyAfterStandUp()
        {
            float until = Time.unscaledTime + 6f; // 2026-10-19 h: weapon re-equip lands after the stand-up
            while (Time.unscaledTime < until)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                if (_controller != null && (_controller.isDead || _controller.ragdolled)) yield break;
                RestoreLivingBodyState();
            }
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



        /// <summary>Clear ragdoll before ApplySaveData; motion settle runs after load via BeginPostLoadMotionSettle.</summary>
        public void PrepareForSaveLoad()
        {
            CancelDeathAndRespawnCoroutines();
            _pinStandUp = false;
            _standUpAnimator = null;
            RestoreLivingBodyFromDeathRagdoll();
        }

        public void BeginPostLoadMotionSettle()
        {
            EnsureReferences();
            KillBodyMotion();
            StartRespawnSettle();
        }

        public void ResetForRespawn()
        {
            CancelDeathAndRespawnCoroutines();
            _pinStandUp = false;
            _standUpAnimator = null;

            if (!_lieCaptured)
                CaptureLieOrientation();

            RestoreLivingBodyFromDeathRagdoll();
        }

        private void CancelDeathAndRespawnCoroutines()
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

            if (_respawnSettleRoutine != null)
            {
                StopCoroutine(_respawnSettleRoutine);
                _respawnSettleRoutine = null;
            }
        }

        private void RestoreLivingBodyFromDeathRagdoll()
        {
            EnsureReferences();

            if (_ragdoll != null)
            {
                _ragdoll.StopAllCoroutines();
                _ragdoll.keepRagdolled = false;
                _ragdoll.ignoreGetUpAnimation = true;
                _ragdoll.RestoreRagdoll();
            }

            ReleaseCorpseFreeze(); // 2026-10-13 (i)

            if (_controller != null)
            {
                if (_controller.ragdolled)
                    _controller.ResetRagdoll();

                _controller.lockMovement = true;
                _controller.lockAnimMovement = false;
                _controller.isJumping = false;
                _controller.customAction = false;
                _controller.StopCharacter();
                _controller.EnableGravityAndCollision();
            }

            Animator animator = ResolveAnimator();
            if (animator != null)
            {
                animator.enabled = true;
                animator.applyRootMotion = false;
            }

            vThirdPersonInput resetInput = ResolveTpInput();
            resetInput?.DisableOnAnimatorMove();
            GetComponent<PioneerShooterMeleeInput>()?.ResetMeleeAfterRevive();
            resetInput?.EnableOnAnimatorMove();
            _lastReviveTime = Time.time; // 24y
            RestoreLivingBodyState(); // 2026-10-19 f
            GetComponent<PioneerInvectorBootstrap>()?.EnsureInvectorPhysicsReady();
            KillBodyMotion();
        }

        /// <summary>Called after PlayerDeathHandler teleports the revived body to the retry spot.</summary>
        public void OnRespawnWorldPlacement()
        {
            RestoreLivingBodyState(); // 2026-10-19 f
            Physics.SyncTransforms();
            KillBodyMotion();
            if (isActiveAndEnabled) StartCoroutine(ReapplyLivingBodyAfterStandUp());
            if (_controller != null)
                _controller.StopCharacter();
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
            float minSettle = p != null ? p.playerRespawnSettleSeconds : 0.85f;
            float postStandUpLock = p != null ? p.playerRespawnPostStandUpLockSeconds : 0.35f;
            float postStandUpSettle = p != null ? p.playerRespawnPostStandUpSettleSeconds : 0.75f;
            float minUntil = Time.time + minSettle;
            float phaseOneCap = minUntil + GetUpTimeoutSeconds + 0.75f;

            while (Time.time < phaseOneCap)
            {
                ApplyRespawnMotionHold(motorInputIdle: true);
                bool standUpDone = _getUpRoutine == null && !_pinStandUp;
                bool grounded = _controller == null || _controller.isGrounded;
                if (Time.time >= minUntil && standUpDone && grounded)
                    break;

                yield return new WaitForFixedUpdate();
            }

            float phaseTwoStart = Time.time;
            float lockUntil = phaseTwoStart + postStandUpLock;
            float settleUntil = phaseTwoStart + postStandUpLock + postStandUpSettle;
            float postHardCap = settleUntil + 1.25f;
            while (Time.time < postHardCap)
            {
                bool grounded = _controller == null || _controller.isGrounded;
                bool inputIdle = _controller == null || !_controller.hasMovementInput;
                bool inPostLock = Time.time < lockUntil;

                if (_controller != null && !_controller.ragdolled)
                {
                    _controller.lockMovement = inPostLock;
                    _controller.lockAnimMovement = false;
                }

                if (inputIdle || inPostLock)
                    ApplyRespawnMotionHold(motorInputIdle: true);
                else
                    ClampRootHorizontalVelocityOnly();

                if (Time.time >= settleUntil && grounded && inputIdle)
                    break;

                yield return new WaitForFixedUpdate();
            }

            ApplyRespawnMotionHold(motorInputIdle: true);
            ReleaseRespawnMovementLocks();
            _respawnSettleRoutine = null;
        }

        private void ApplyRespawnMotionHold(bool motorInputIdle)
        {
            KillBodyMotion();
            if (_controller == null || _controller.ragdolled)
                return;

            if (motorInputIdle)
                _controller.StopCharacter();
        }

        private void ClampRootHorizontalVelocityOnly()
        {
            Rigidbody root = GetComponent<Rigidbody>();
            if (root != null && !root.isKinematic)
            {
                Vector3 v = root.linearVelocity;
                if (v.x * v.x + v.z * v.z > 0.0004f)
                    root.linearVelocity = new Vector3(0f, v.y, 0f);
                root.angularVelocity = Vector3.zero;
            }
        }

        private void ReleaseRespawnMovementLocks()
        {
            if (_controller == null || _controller.ragdolled)
                return;

            _controller.lockMovement = false;
            _controller.lockAnimMovement = false;
            _controller.lockAnimRotation = false;
            _controller.customAction = false;
            _controller.StopCharacter();
            _controller.StopCharacterWithLerp();
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
                    if (_survivalStats != null && !_survivalStats.IsDead)
                    {
                        rb.isKinematic = false;
                        rb.useGravity = true;
                    }
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
                // Kinematic ragdoll bones must also regain their alive collision
                // state. Solid child colliders can otherwise shove the root capsule.
                if (rb.transform == hips || rb.GetComponent<Joint>() != null)
                {
                    Collider boneCollider = rb.GetComponent<Collider>();
                    if (boneCollider != null)
                    {
                        if (_ragdoll != null && _ragdoll.disableColliders) boneCollider.enabled = false;
                        else if (!(boneCollider is MeshCollider bm && !bm.convex)) boneCollider.isTrigger = true; // 24u: concave mesh triggers unsupported
                    }
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

            // Remove the tagged stand-up state BEFORE restoring animation-driven
            // movement. Merely setting its layer weight to zero leaves tags active.
            GetComponent<PioneerShooterMeleeInput>()?.ResetMeleeAfterRevive();
            if (animator != null && layer >= 0)
                animator.SetLayerWeight(layer, 1f);
            if (_controller != null)
            {
                _controller.lockMovement = true;
                _controller.lockAnimMovement = false;
                _controller.lockAnimRotation = false;
                _controller.customAction = false;
                _controller.StopCharacter();
            }
            tpInput?.EnableOnAnimatorMove();
            ApplyRespawnMotionHold(motorInputIdle: true);

            _lieCaptured = false;
            _deathPoseCaptured = false;
            _getUpRoutine = null;
        }

        private void LateUpdate()
        {
            if (_pinStandUp)
                PinStandUpPose();
            ProbeWorldPassThrough();
        }

        // 24y: rocks pass-through after death/revive. Every 0.25 s while alive: any solid world collider the capsule is sunk into
        // gets checked; a per-pair Physics.IgnoreCollision left over (ragdoll/mining/etc.) is removed, and the full state is logged
        // once per collider as [RockPassThrough] so the exact cause shows in the console.
        private float _probeNext;
        private readonly Collider[] _probeHits = new Collider[16];
        private readonly System.Collections.Generic.Dictionary<int, float> _probeLogged = new System.Collections.Generic.Dictionary<int, float>();
        private float _lastReviveTime = -1f;

        private void ProbeWorldPassThrough()
        {
            if (Time.time < _probeNext) return;
            _probeNext = Time.time + 0.25f;
            if (_controller == null || _controller.isDead || _controller.ragdolled || _pinStandUp) return;
            CapsuleCollider cap = GetComponent<CapsuleCollider>();
            if (cap == null) return;
            Rigidbody rb = GetComponent<Rigidbody>();
            float r = cap.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z)) * 0.9f;
            float h = Mathf.Max(cap.height * Mathf.Abs(transform.lossyScale.y), r * 2f);
            Vector3 c = transform.TransformPoint(cap.center);
            Vector3 up = transform.up * (h * 0.5f - r);
            int n = Physics.OverlapCapsuleNonAlloc(c - up, c + up, r, _probeHits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider o = _probeHits[i];
                if (o == null || o == cap || o.transform.IsChildOf(transform) || o.attachedRigidbody != null && !o.attachedRigidbody.isKinematic) continue;
                bool pairIgnored = Physics.GetIgnoreCollision(cap, o);
                if (!Physics.ComputePenetration(cap, cap.transform.position, cap.transform.rotation, o, o.transform.position, o.transform.rotation, out _, out float depth)) depth = -1f;
                if (!pairIgnored && depth < 0.25f) continue;
                if (pairIgnored) Physics.IgnoreCollision(cap, o, false);
                int id = o.GetHashCode();
                if (_probeLogged.TryGetValue(id, out float last) && Time.time - last < 10f) continue;
                _probeLogged[id] = Time.time;
                MeshCollider mc = o as MeshCollider;
                Debug.Log("[RockPassThrough] '" + o.name + "' (" + o.GetType().Name + " layer " + LayerMask.LayerToName(o.gameObject.layer) +
                          (mc != null ? " convex=" + mc.convex + " mesh=" + (mc.sharedMesh != null ? mc.sharedMesh.name : "NULL") : "") +
                          ") depth=" + depth.ToString("F2") + " pairIgnored=" + pairIgnored + (pairIgnored ? "(fixed)" : "") +
                          " layerIgnored=" + Physics.GetIgnoreLayerCollision(gameObject.layer, o.gameObject.layer) +
                          " | player layer=" + LayerMask.LayerToName(gameObject.layer) + " cap en=" + cap.enabled + " trig=" + cap.isTrigger +
                          " rb kin=" + (rb != null && rb.isKinematic) + " detect=" + (rb == null || rb.detectCollisions) + " vel=" + (rb != null && !rb.isKinematic ? rb.linearVelocity.ToString("F1") : "-") +
                          " lockMove=" + _controller.lockMovement + " rootMotion=" + (ResolveAnimator() is Animator an && an.applyRootMotion) +
                          " sinceRevive=" + (_lastReviveTime < 0f ? "never" : (Time.time - _lastReviveTime).ToString("F0") + "s"));
            }
        }

        private void PinStandUpPose()
        {
            Transform root = _controller != null ? _controller.transform : transform;
            if (root != null)
                root.SetPositionAndRotation(_standUpPinPosition, _standUpPinRotation);

            Rigidbody rootBody = root != null ? root.GetComponent<Rigidbody>() : null;
            if (rootBody != null && !rootBody.isKinematic)
            {
                rootBody.linearVelocity = Vector3.zero;
                rootBody.angularVelocity = Vector3.zero;
            }

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


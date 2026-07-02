using Project.Companions;
using Project.Player;
using Project.Pioneers;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Drives pioneer locomotion on the same Grounded blend tree as the player.
    /// Turn = lateral strafe only; forward follow/run keeps Turn at 0 while the body rotates.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class CompanionAnimationDriver : MonoBehaviour
    {
        private static readonly int Forward = Animator.StringToHash("Forward");
        private static readonly int Turn = Animator.StringToHash("Turn");
        private static readonly int Ground = Animator.StringToHash("OnGround");
        private static readonly int LocomotionAnimSpeed = Animator.StringToHash("LocomotionAnimSpeed");

        [SerializeField] private float locomotionSmoothTime = 0.18f;
        [SerializeField] private float leanBlendSmoothTime = 0.24f;
        [SerializeField] private float animSpeedSmoothTime = 0.12f;
        [SerializeField] private float runSpeedReference = 8.5f;
        [SerializeField] private float attackBlendTime = 0.08f;
        [SerializeField] private float idleForwardDamp = 6f;
        [SerializeField] private float moveSpeedThreshold = 0.28f;
        [SerializeField] private float followSpeedThreshold = 0.08f;
        [SerializeField] private float walkBlendForward = 0.5f;
        [SerializeField] private float walkSpeedReference = 4.4f;
        [SerializeField] private float walkAnimationSpeed = 0.7f;
        [SerializeField] private float runAnimationSpeed = 0.9f;
        [SerializeField] private float idleWanderAnimSpeedCap = 0.42f;

        [Header("Strafe (matches player locomotion)")]
        [SerializeField] private float strafeForwardCutoff = 0.12f;
        [SerializeField] private float strafeLateralCutoff = 0.08f;
        [SerializeField] private float strafeBlendScale = 1f;

        private Animator animator;
        private CompanionFollowController followController;
        private Vector3 lastPosition;
        private bool locomotionAnimSpeedAvailable;
        private bool capabilitiesCached;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            followController = GetComponent<CompanionFollowController>();
            lastPosition = transform.position;
        }

        private void Update()
        {
            if (animator == null || followController == null || !animator.isInitialized)
                return;

            CacheAnimatorCapabilities();

            float deltaTime = Time.deltaTime;
            Vector3 delta = transform.position - lastPosition;
            lastPosition = transform.position;
            delta.y = 0f;

            float measuredSpeed = deltaTime > 0.0001f ? delta.magnitude / deltaTime : 0f;
            float controllerSpeed = followController.CurrentSpeed;

            bool companionAtRest = !followController.IsWandering
                && controllerSpeed <= followSpeedThreshold * 0.45f
                && measuredSpeed <= moveSpeedThreshold * 0.25f;

            bool isMoving = !companionAtRest && (
                followController.IsWandering
                || controllerSpeed > followSpeedThreshold
                || measuredSpeed > moveSpeedThreshold * 0.25f);

            if (!IsCompanionAttackPlaying())
                EnsureGroundedState();

            float forwardAmount = 0f;
            float turnAmount = 0f;
            bool driveStrafeTurnAxis = false;

            if (isMoving)
            {
                Vector3 worldMove = followController.CurrentMoveDirection;
                if (worldMove.sqrMagnitude < 0.0001f && delta.sqrMagnitude > 0.0001f)
                    worldMove = delta.normalized;

                worldMove.y = 0f;
                if (worldMove.sqrMagnitude > 0.0001f)
                {
                    worldMove.Normalize();
                    float localForward = Vector3.Dot(worldMove, transform.forward);
                    float localTurn = Vector3.Dot(worldMove, transform.right);
                    Vector2 localMove = Vector2.ClampMagnitude(new Vector2(localTurn, localForward), 1f);

                    float moveIntensity = Mathf.Max(localMove.magnitude, measuredSpeed > 0.02f ? 0.35f : 0f);
                    float speedRatio = Mathf.Clamp01(Mathf.Max(controllerSpeed, measuredSpeed) / runSpeedReference);
                    bool slowWander = followController.IsWandering
                        && Mathf.Max(controllerSpeed, measuredSpeed) < walkSpeedReference * 0.45f;
                    float blendForward = slowWander
                        ? 0.32f
                        : speedRatio >= 0.75f ? 1f : walkBlendForward;

                    ResolveLocomotionBlend(
                        worldMove,
                        localForward,
                        localTurn,
                        localMove,
                        blendForward,
                        moveIntensity,
                        speedRatio,
                        out forwardAmount,
                        out turnAmount,
                        out driveStrafeTurnAxis);
                }
            }

            if (locomotionAnimSpeedAvailable)
            {
                float effectiveSpeed = isMoving ? Mathf.Max(controllerSpeed, measuredSpeed) : 0f;
                float animSpeed;
                if (!isMoving)
                {
                    animSpeed = 0.75f;
                }
                else if (followController.IsWandering && effectiveSpeed < walkSpeedReference * 0.45f)
                {
                    animSpeed = Mathf.Clamp(
                        effectiveSpeed / walkSpeedReference,
                        0.12f,
                        idleWanderAnimSpeedCap);
                }
                else if (effectiveSpeed < runSpeedReference * 0.75f)
                {
                    float walkRatio = walkSpeedReference > 0.01f ? effectiveSpeed / walkSpeedReference : 1f;
                    animSpeed = Mathf.Clamp(walkAnimationSpeed * walkRatio, 0.4f, 1f);
                }
                else
                {
                    float runRatio = runSpeedReference > 0.01f ? effectiveSpeed / runSpeedReference : 1f;
                    animSpeed = Mathf.Clamp(runAnimationSpeed * runRatio, 0.4f, 1.2f);
                }

                animator.SetFloat(
                    LocomotionAnimSpeed,
                    animSpeed,
                    animSpeedSmoothTime,
                    deltaTime);
            }

            float forwardSmoothTime = companionAtRest
                ? idleForwardDamp * 0.015f
                : isMoving ? locomotionSmoothTime : idleForwardDamp * 0.03f;

            bool pureStrafe = driveStrafeTurnAxis && Mathf.Abs(forwardAmount) < 0.01f;
            if (pureStrafe)
                animator.SetFloat(Forward, 0f);
            else
                animator.SetFloat(Forward, forwardAmount, forwardSmoothTime, deltaTime);

            if (driveStrafeTurnAxis)
                animator.SetFloat(Turn, turnAmount, leanBlendSmoothTime, deltaTime);
            else
                animator.SetFloat(Turn, 0f);

            animator.SetBool(Ground, true);
        }

        private void ResolveLocomotionBlend(
            Vector3 worldMove,
            float localForward,
            float localTurn,
            Vector2 localMove,
            float blendForward,
            float moveIntensity,
            float speedRatio,
            out float forwardAmount,
            out float turnAmount,
            out bool driveStrafeTurnAxis)
        {
            forwardAmount = 0f;
            turnAmount = 0f;
            driveStrafeTurnAxis = false;

            if (localForward > strafeForwardCutoff)
            {
                forwardAmount = Mathf.Clamp01(localForward) * blendForward * moveIntensity;
                if (speedRatio < 0.75f && moveIntensity > 0.05f)
                {
                    forwardAmount = Mathf.Sign(Mathf.Abs(forwardAmount) > 0.01f ? forwardAmount : localForward)
                        * Mathf.Max(Mathf.Abs(forwardAmount), walkBlendForward * moveIntensity);
                }

                return;
            }

            bool hasLateral = Mathf.Abs(localTurn) > strafeLateralCutoff;
            bool hasBackward = localForward < -strafeForwardCutoff;

            if (hasLateral && localForward <= strafeForwardCutoff)
            {
                turnAmount = Mathf.Sign(localTurn)
                    * Mathf.Clamp01(Mathf.Abs(localTurn))
                    * blendForward
                    * moveIntensity
                    * strafeBlendScale;
                driveStrafeTurnAxis = Mathf.Abs(turnAmount) > 0.01f;
                return;
            }

            if (!hasBackward)
                return;

            Vector2 backwardMove = localMove;
            backwardMove.x *= strafeBlendScale;
            float intensity = backwardMove.magnitude;
            if (intensity < 0.01f)
                return;

            forwardAmount = backwardMove.y * blendForward * moveIntensity;
            turnAmount = backwardMove.x * blendForward * moveIntensity;
            driveStrafeTurnAxis = Mathf.Abs(turnAmount) > 0.01f;
        }

        public void TriggerAttack()
        {
            if (animator == null)
                return;

            const string attackState = "Sword Attack 1 Hand Full Body 1";
            int attackHash = Animator.StringToHash(attackState);
            int layer = 0;
            for (int i = 0; i < animator.layerCount; i++)
            {
                if (animator.GetLayerName(i) == GkcAnimatorConstants.UpperBodyCombatLayer && animator.HasState(i, attackHash))
                {
                    layer = i;
                    break;
                }
            }

            if (!animator.HasState(layer, attackHash))
                return;

            if (HasFloatParameter(animator, LocomotionAnimSpeed))
                animator.SetFloat(LocomotionAnimSpeed, 0.95f);

            animator.CrossFadeInFixedTime(attackState, attackBlendTime, layer, 0f);
        }

        public void ApplyBehaviorProfile(PioneerBehaviorProfile profile)
        {
            if (profile == null)
                return;

            walkSpeedReference = profile.walkSpeedReference;
            runSpeedReference = profile.runSpeedReference;
            walkAnimationSpeed = profile.walkAnimationSpeed;
            runAnimationSpeed = profile.runAnimationSpeed;
        }

        private void CacheAnimatorCapabilities()
        {
            if (capabilitiesCached && animator.runtimeAnimatorController != null)
                return;

            locomotionAnimSpeedAvailable = HasFloatParameter(animator, LocomotionAnimSpeed);
            capabilitiesCached = true;
        }

        private static bool HasFloatParameter(Animator target, int parameterHash)
        {
            if (target == null)
                return false;

            AnimatorControllerParameter[] parameters = target.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].type == AnimatorControllerParameterType.Float
                    && parameters[i].nameHash == parameterHash)
                    return true;
            }

            return false;
        }

        private bool IsCompanionAttackPlaying()
        {
            if (animator == null)
                return false;

            for (int layer = 0; layer < animator.layerCount; layer++)
            {
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(layer);
                if (info.normalizedTime < 0.95f && info.tagHash != 0)
                    return true;
            }

            return false;
        }

        private void EnsureGroundedState()
        {
            if (animator == null)
                return;

            const string grounded = "Grounded";
            int hash = Animator.StringToHash(grounded);
            if (!animator.HasState(0, hash))
                return;

            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            if (!current.IsName(grounded))
                animator.CrossFadeInFixedTime(grounded, 0.15f, 0, 0f);
        }
    }
}

using Invector.vCharacterController;
using Project.Core;
using UnityEngine;

namespace Project.Features.Climb
{
    /// <summary>
    /// On-foot steps and slopes. Pushes DM_ClimbDashProfile "Steps & slopes" into the Invector motor
    /// every frame (live Studio edits) and adds a lift helper for steps whose front face is angled,
    /// chamfered or rough, which Invector's own step offset and slope block tend to reject.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMStepSlopeAssist : MonoBehaviour
    {
        private const float MaxLiftSeconds = 0.45f;

        private vThirdPersonMotor motor;
        private Rigidbody body;
        private CapsuleCollider capsule;
        private DMClimbController climb;
        private int buildingBit;

        private bool lifting;
        private float liftTargetY;
        private float liftTimer;
        private Vector3 liftDir;

        private bool slopeOverride;
        private bool savedUseSlopeLimit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnPlayer()
        {
            if (!Application.isPlaying)
                return;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null || player.GetComponent<DMStepSlopeAssist>() != null)
                return;
            if (player.GetComponent<vThirdPersonMotor>() == null)
                return;

            player.AddComponent<DMStepSlopeAssist>();
        }

        private void Awake()
        {
            motor = GetComponent<vThirdPersonMotor>();
            body = GetComponent<Rigidbody>();
            capsule = GetComponent<CapsuleCollider>();
            climb = GetComponent<DMClimbController>();
            int buildingLayer = LayerMask.NameToLayer("Building");
            buildingBit = buildingLayer >= 0 ? 1 << buildingLayer : 0;
        }

        private void OnDisable()
        {
            lifting = false;
            RestoreSlopeLimit();
        }

        private void LateUpdate()
        {
            DM_ClimbDashProfile p = DM_ClimbDashProfile.Live;
            if (p == null || motor == null)
                return;

            motor.useStepOffset = p.stepAssistEnabled;
            motor.stepOffsetMaxHeight = p.stepMaxHeight;
            motor.stepOffsetMinHeight = Mathf.Min(p.stepMinHeight, p.stepMaxHeight - 0.01f);
            motor.stepOffsetDistance = p.stepProbeDistance;
            motor.slopeLimit = p.walkableSlopeDegrees;

            if (p.stepOnGroundLayers)
            {
                int mask = motor.groundLayer.value | buildingBit;
                if ((motor.stepOffsetLayer.value & mask) != mask)
                    motor.stepOffsetLayer = motor.stepOffsetLayer.value | mask;
            }
        }

        private void FixedUpdate()
        {
            DM_ClimbDashProfile p = DM_ClimbDashProfile.Live;
            if (p == null || motor == null || body == null || body.isKinematic
                || !p.stepAssistEnabled || !p.stepLiftHelper || !CanAssist())
            {
                lifting = false;
                RestoreSlopeLimit();
                return;
            }

            if (lifting)
            {
                ContinueLift(p);
                return;
            }

            Vector3 dir = motor.moveDirection;
            dir.y = 0f;
            if (motor.input.magnitude < 0.1f || dir.sqrMagnitude < 0.0001f || !motor.isGrounded)
            {
                RestoreSlopeLimit();
                return;
            }

            dir.Normalize();
            if (TryFindStep(p, dir, out float targetY))
            {
                if (p.stepIgnoreSlopeBlock)
                    OverrideSlopeLimit();
                lifting = true;
                liftTargetY = targetY;
                liftTimer = 0f;
                liftDir = dir;
                ContinueLift(p);
            }
            else
            {
                RestoreSlopeLimit();
            }
        }

        private bool CanAssist()
        {
            if (motor.isJumping || motor.customAction || motor.isSliding || motor.lockMovement || motor.isDead)
                return false;
            if (climb != null && climb.IsClimbing)
                return false;
            return true;
        }

        private bool TryFindStep(DM_ClimbDashProfile p, Vector3 dir, out float targetY)
        {
            targetY = 0f;
            Vector3 feet = transform.position;
            float radius = CapsuleRadius();
            int mask = StepMask(p);

            Vector3 origin = feet + Vector3.up * (p.stepMinHeight + 0.04f);
            if (!Physics.Raycast(origin, dir, out RaycastHit riser, radius + p.stepProbeDistance + 0.05f, mask, QueryTriggerInteraction.Ignore))
                return false;
            if (IsSelfOrLoose(riser.collider))
                return false;
            if (Vector3.Angle(Vector3.up, riser.normal) < p.stepRiserMinAngle)
                return false;

            float maxH = p.stepMaxHeight;
            Vector3 flatPoint = new Vector3(riser.point.x, feet.y, riser.point.z);
            Vector3 topStart = flatPoint + dir * 0.12f + Vector3.up * (maxH + 0.08f);
            if (!Physics.SphereCast(topStart, 0.06f, Vector3.down, out RaycastHit top, maxH + 0.08f, mask, QueryTriggerInteraction.Ignore))
                return false;
            if (IsSelfOrLoose(top.collider))
                return false;

            float rise = top.point.y - feet.y;
            if (rise < Mathf.Max(0.03f, p.stepMinHeight) || rise > maxH + 0.01f)
                return false;
            if (Vector3.Angle(Vector3.up, top.normal) > p.walkableSlopeDegrees)
                return false;

            float height = capsule != null ? capsule.height * Mathf.Abs(transform.lossyScale.y) : 1.8f;
            Vector3 low = feet + Vector3.up * (rise + radius + 0.03f);
            Vector3 high = feet + Vector3.up * Mathf.Max(rise + radius + 0.04f, rise + height - radius);
            if (Physics.CheckCapsule(low, high, radius * 0.85f, mask, QueryTriggerInteraction.Ignore))
                return false;

            targetY = top.point.y + 0.01f;
            return true;
        }

        private void ContinueLift(DM_ClimbDashProfile p)
        {
            float dt = Time.fixedDeltaTime;
            liftTimer += dt;
            Vector3 pos = body.position;
            float remaining = liftTargetY - pos.y;

            if (remaining <= 0.005f || liftTimer > MaxLiftSeconds || motor.input.magnitude < 0.1f)
            {
                lifting = false;
                return;
            }

            float step = Mathf.Min(remaining, p.stepLiftSpeed * dt);
            pos += Vector3.up * step + liftDir * (p.stepLiftForwardPush * dt);
            body.position = pos;

            Vector3 v = body.linearVelocity;
            if (v.y < 0f)
            {
                v.y = 0f;
                body.linearVelocity = v;
            }
        }

        private int StepMask(DM_ClimbDashProfile p)
        {
            int mask = motor.stepOffsetLayer.value;
            if (p.stepOnGroundLayers)
                mask |= motor.groundLayer.value | buildingBit;
            return mask;
        }

        private float CapsuleRadius()
        {
            if (capsule == null)
                return 0.3f;
            Vector3 s = transform.lossyScale;
            return capsule.radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
        }

        private bool IsSelfOrLoose(Collider c)
        {
            if (c == null)
                return true;
            if (c.transform == transform || c.transform.IsChildOf(transform))
                return true;
            Rigidbody rb = c.attachedRigidbody;
            return rb != null && !rb.isKinematic;
        }

        private void OverrideSlopeLimit()
        {
            if (slopeOverride)
                return;
            savedUseSlopeLimit = motor.useSlopeLimit;
            motor.useSlopeLimit = false;
            slopeOverride = true;
        }

        private void RestoreSlopeLimit()
        {
            if (!slopeOverride)
                return;
            slopeOverride = false;
            if (motor != null)
                motor.useSlopeLimit = savedUseSlopeLimit;
        }
    }
}

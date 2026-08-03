using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Shared facing helpers so wander / ping-pong direction changes feel natural
    /// instead of snappy Slerp snaps. Legacy <c>turnSpeed</c> inspector values
    /// (typically 5–18) convert to degrees/sec via <see cref="ToDegreesPerSecond"/>.
    /// </summary>
    public static class DMILocomotionFacing
    {
        /// <summary>Legacy slerp-factor → deg/sec. turnSpeed 8 ≈ 144°/s.</summary>
        public static float ToDegreesPerSecond(float turnSpeed)
        {
            return Mathf.Clamp(turnSpeed * 18f, 40f, 220f);
        }

        /// <summary>NavMeshAgent.angularSpeed from the same legacy turnSpeed field.</summary>
        public static float ToAgentAngularSpeed(float turnSpeed)
        {
            return Mathf.Clamp(turnSpeed * 20f, 60f, 200f);
        }

        /// <summary>
        /// Yaw toward a world point at a capped deg/sec rate (frame-rate stable).
        /// </summary>
        public static void FaceToward(Transform transform, Vector3 worldTarget, float turnSpeed)
        {
            if (transform == null)
                return;

            Vector3 toTarget = worldTarget - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.0001f)
                return;

            Quaternion look = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            float maxDegrees = ToDegreesPerSecond(turnSpeed) * Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, maxDegrees);
        }

        /// <summary>
        /// 1 when facing the move direction; drops toward <paramref name="minScale"/> on sharp turns
        /// so ping-pong reversals ease through the turn instead of sliding sideways.
        /// </summary>
        public static float FacingMoveScale(Transform transform, Vector3 desiredFlatDirection, float minScale = 0.18f)
        {
            if (transform == null || desiredFlatDirection.sqrMagnitude <= 0.0001f)
                return 1f;

            float align = Vector3.Dot(transform.forward, desiredFlatDirection.normalized);
            // align -1..1 → 0..1 with a little forgiveness near forward
            float t = Mathf.Clamp01((align + 0.25f) / 1.25f);
            return Mathf.Lerp(Mathf.Clamp01(minScale), 1f, t);
        }
    }
}

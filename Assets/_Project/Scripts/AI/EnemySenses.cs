using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Sense-based target detection: proximity, vision (FOV + LOS), and hearing via noise events.
    /// </summary>
    public class EnemySenses : MonoBehaviour
    {
        [Header("Vision")]
        [SerializeField] private float visionRange = 16f;
        [SerializeField] private float visionFov = 110f;
        [SerializeField] private float eyeHeight = 1.4f;
        [SerializeField] private LayerMask obstructionMask = ~0;

        [Header("Hearing")]
        [SerializeField] private float hearingRange = 18f;
        [SerializeField] private float noiseMemoryDuration = 8f;

        [Header("Proximity")]
        [SerializeField] private float proximityRange = 2.5f;

        private Transform player;
        private Vector3 lastNoisePosition;
        private float lastNoiseTime;
        private bool hasRecentNoise;

        public Vector3 LastNoisePosition => lastNoisePosition;
        public bool HasRecentNoise => hasRecentNoise && Time.time - lastNoiseTime <= noiseMemoryDuration;
        public float NoiseAge => HasRecentNoise ? Time.time - lastNoiseTime : float.MaxValue;

        private void OnEnable()
        {
            EnemyNoiseEvents.OnNoise += HandleNoise;
        }

        private void OnDisable()
        {
            EnemyNoiseEvents.OnNoise -= HandleNoise;
        }

        public Transform GetSensedTarget()
        {
            EnsurePlayer();
            if (player == null)
                return null;

            Vector3 enemyPos = transform.position;
            Vector3 playerPos = player.position;
            float distance = HorizontalDistance(enemyPos, playerPos);

            if (distance <= proximityRange)
                return player;

            if (distance <= visionRange && IsWithinFov(playerPos) && HasLineOfSight(playerPos))
                return player;

            return null;
        }

        public bool TryGetHeardNoise(out Vector3 position)
        {
            position = lastNoisePosition;
            return HasRecentNoise;
        }

        private void HandleNoise(EnemyNoiseEvents.NoiseEvent noiseEvent)
        {
            float distance = Vector3.Distance(transform.position, noiseEvent.Position);
            if (distance > hearingRange + noiseEvent.Radius)
                return;

            lastNoisePosition = noiseEvent.Position;
            lastNoiseTime = Time.time;
            hasRecentNoise = true;
        }

        private void EnsurePlayer()
        {
            if (player != null)
                return;

            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        private bool IsWithinFov(Vector3 targetPosition)
        {
            Vector3 toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
                return true;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();

            float angle = Vector3.Angle(forward, toTarget.normalized);
            return angle <= visionFov * 0.5f;
        }

        private bool HasLineOfSight(Vector3 targetPosition)
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 target = targetPosition + Vector3.up * eyeHeight;
            Vector3 direction = target - origin;
            float distance = direction.magnitude;
            if (distance <= 0.05f)
                return true;

            direction /= distance;

            if (!Physics.Raycast(origin, direction, out RaycastHit hit, distance, obstructionMask, QueryTriggerInteraction.Ignore))
                return true;

            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, proximityRange);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, visionRange);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, hearingRange);

            if (HasRecentNoise)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(lastNoisePosition, 0.35f);
            }
        }
    }
}

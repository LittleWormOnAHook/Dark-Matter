using Project.AI;
using Project.Player;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Detects hostile targets in a wider cone and range than the player's combat focus lock.
    /// </summary>
    public class CompanionThreatSensor : MonoBehaviour
    {
        [SerializeField] private float rangeBeyondPlayerFocus = 2.25f;
        [SerializeField] private float detectFov = 115f;
        [SerializeField] private float eyeHeight = 1.35f;
        [SerializeField] private float ownerForwardBias = 0.45f;
        [SerializeField] private LayerMask obstructionMask = ~0;

        public float EffectiveDetectRange(CombatFocusController playerFocus)
        {
            float playerRange = playerFocus != null ? playerFocus.FocusRange : 3.5f;
            return playerRange + rangeBeyondPlayerFocus;
        }

        public EnemyHealth ScanForThreat(Transform owner, CombatFocusController playerFocus)
        {
            float maxRange = EffectiveDetectRange(playerFocus);
            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>();
            EnemyHealth best = null;
            float bestScore = float.MaxValue;

            Vector3 scanOrigin = transform.position;
            Vector3 scanForward = ResolveScanForward(owner);

            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyHealth enemy = enemies[i];
                if (enemy == null || enemy.IsDead)
                    continue;

                Vector3 toEnemy = enemy.transform.position - scanOrigin;
                toEnemy.y = 0f;
                float distance = toEnemy.magnitude;
                if (distance <= 0.05f || distance > maxRange)
                    continue;

                float angle = Vector3.Angle(scanForward, toEnemy / distance);
                if (angle > detectFov * 0.5f)
                    continue;

                if (!HasLineOfSight(enemy.transform.position))
                    continue;

                float score = distance + angle * 0.04f;
                if (score >= bestScore)
                    continue;

                best = enemy;
                bestScore = score;
            }

            return best;
        }

        private Vector3 ResolveScanForward(Transform owner)
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            else
                forward.Normalize();

            if (owner == null)
                return forward;

            Vector3 ownerForward = owner.forward;
            ownerForward.y = 0f;
            if (ownerForward.sqrMagnitude < 0.0001f)
                return forward;

            ownerForward.Normalize();
            Vector3 blended = Vector3.Slerp(forward, ownerForward, ownerForwardBias);
            blended.y = 0f;
            return blended.sqrMagnitude > 0.0001f ? blended.normalized : forward;
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

            return hit.transform == transform || hit.transform.IsChildOf(transform)
                || hit.collider.GetComponentInParent<EnemyHealth>() != null;
        }
    }
}

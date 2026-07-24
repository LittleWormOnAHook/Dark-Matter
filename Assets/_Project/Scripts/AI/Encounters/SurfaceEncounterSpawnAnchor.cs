using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Marks where a surface encounter zone can spawn a threat and optionally assign a patrol route.
    /// </summary>
    [DisallowMultipleComponent]
    public class SurfaceEncounterSpawnAnchor : MonoBehaviour
    {
        [SerializeField] private SurfaceThreatKind preferredThreatKind = SurfaceThreatKind.Any;
        [SerializeField] private float spawnRadius = 1.25f;
        [SerializeField] private SurfacePatrolRoute patrolRoute;
        [SerializeField] private bool faceRouteForward = true;

        public SurfaceThreatKind PreferredThreatKind => preferredThreatKind;
        public SurfacePatrolRoute PatrolRoute => patrolRoute;

        public Vector3 ResolvePosition()
        {
            if (spawnRadius <= 0f)
                return transform.position;

            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            return transform.position + new Vector3(offset.x, 0f, offset.y);
        }

        public Quaternion ResolveRotation()
        {
            if (faceRouteForward && patrolRoute != null)
            {
                Transform[] points = patrolRoute.ResolveWaypoints();
                if (points.Length > 0 && points[0] != null)
                {
                    Vector3 forward = points[0].position - transform.position;
                    forward.y = 0f;
                    if (forward.sqrMagnitude > 0.01f)
                        return Quaternion.LookRotation(forward.normalized, Vector3.up);
                }
            }

            return transform.rotation;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.85f, 0.65f, 0.12f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, spawnRadius));
            Gizmos.DrawSphere(transform.position, 0.12f);

            if (patrolRoute != null)
            {
                Gizmos.color = new Color(0.75f, 0.16f, 0.37f, 0.65f);
                Gizmos.DrawLine(transform.position, patrolRoute.transform.position);
            }
        }
    }
}

using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// World-space patrol waypoints shared by surface encounter spawns.
    /// </summary>
    [DisallowMultipleComponent]
    public class SurfacePatrolRoute : MonoBehaviour
    {
        [SerializeField] private EnemyPatrolMode patrolMode = EnemyPatrolMode.Loop;
        [SerializeField] private bool autoCollectChildWaypoints = true;
        [SerializeField] private Transform[] manualWaypoints = System.Array.Empty<Transform>();

        public EnemyPatrolMode PatrolMode => patrolMode;

        public Transform[] ResolveWaypoints()
        {
            if (!autoCollectChildWaypoints)
                return FilterValid(manualWaypoints);

            Transform[] children = new Transform[transform.childCount];
            int count = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child == null)
                    continue;

                children[count++] = child;
            }

            if (count == 0)
                return FilterValid(manualWaypoints);

            Transform[] resolved = new Transform[count];
            System.Array.Copy(children, resolved, count);
            return resolved;
        }

        private static Transform[] FilterValid(Transform[] points)
        {
            if (points == null || points.Length == 0)
                return System.Array.Empty<Transform>();

            int count = 0;
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i] != null)
                    count++;
            }

            if (count == 0)
                return System.Array.Empty<Transform>();

            Transform[] resolved = new Transform[count];
            int index = 0;
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i] == null)
                    continue;

                resolved[index++] = points[i];
            }

            return resolved;
        }

        private void OnDrawGizmos()
        {
            Transform[] points = ResolveWaypoints();
            if (points.Length == 0)
                return;

            Gizmos.color = new Color(0.75f, 0.16f, 0.37f, 0.9f);
            for (int i = 0; i < points.Length; i++)
            {
                Transform point = points[i];
                if (point == null)
                    continue;

                Gizmos.DrawSphere(point.position, 0.25f);
                Transform next = points[(i + 1) % points.Length];
                if (next != null && (patrolMode == EnemyPatrolMode.Loop || i < points.Length - 1))
                    Gizmos.DrawLine(point.position, next.position);
            }
        }
    }
}

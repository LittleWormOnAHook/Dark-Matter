using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Applies patrol routes to freshly spawned surface threats.
    /// </summary>
    public static class SurfaceEncounterPatrolBinder
    {
        public static void Apply(GameObject instance, SurfacePatrolRoute route)
        {
            if (instance == null || route == null)
                return;

            Transform[] waypoints = route.ResolveWaypoints();
            if (waypoints.Length == 0)
                return;

            EnemyAiController ai = instance.GetComponent<EnemyAiController>();
            if (ai == null)
                return;

            ai.ConfigurePatrolRoute(waypoints, route.PatrolMode);
        }
    }
}

using Project.Building;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Last-known → navigable footprint at a wall base (ray toward last seen, use impact XZ).
    /// </summary>
    public static class EnemyInvestigateWallBase
    {
        public struct Context
        {
            public Vector3 Anchor;
            public Vector3 WallNormal;
            public Vector3 WallTangent;
            public bool HasWall;
        }

        public static Context Resolve(
            Transform enemy,
            Vector3 lastKnownWorld,
            Vector3 enemyFeet,
            DM_EnemyBrainProfile profile,
            bool preferWallBase)
        {
            Context ctx = default;
            ctx.Anchor = lastKnownWorld;
            ctx.WallNormal = -enemy.forward;
            ctx.WallTangent = enemy.right;

            if (enemy == null)
                return ctx;

            float offset = profile != null
                ? Mathf.Max(profile.investigateWallAnchorOffset, profile.chaseWallStandoffMeters)
                : 0.5f;
            float rayDistance = profile != null ? profile.investigateWallBaseRayDistance : 22f;
            int mask = EnemyChaseWallSlide.ChaseObstructionMask;

            Vector3 aim = lastKnownWorld;
            aim.y = enemyFeet.y;

            Vector3 origin = enemyFeet + Vector3.up * 1.05f;
            Vector3 dir = aim - origin;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.04f)
                dir = enemy.forward;
            dir.Normalize();

            if (preferWallBase &&
                Physics.Raycast(origin, dir, out RaycastHit hit, rayDistance, mask, QueryTriggerInteraction.Ignore) &&
                !IsSelf(hit.collider, enemy))
            {
                ctx.HasWall = true;
                ctx.WallNormal = hit.normal;
                ctx.Anchor = hit.point + hit.normal * offset;
            }
            else
            {
                ctx.Anchor = aim;
            }

            ctx.Anchor.y = enemyFeet.y;
            if (EnemyGroundUtility.TryGetGroundY(ctx.Anchor, out float groundY))
                ctx.Anchor.y = groundY;

            Vector3 tangent = Vector3.Cross(Vector3.up, ctx.WallNormal);
            if (tangent.sqrMagnitude < 0.0001f)
                tangent = enemy.right;
            tangent.Normalize();

            if (Vector3.Dot(tangent, dir) < 0f)
                tangent = -tangent;

            ctx.WallTangent = tangent;
            return ctx;
        }

        public static Vector3 PatrolPointAlongWall(in Context ctx, float signedOffset)
        {
            Vector3 point = ctx.Anchor + ctx.WallTangent * signedOffset;
            if (EnemyGroundUtility.TryGetGroundY(point, out float groundY))
                point.y = groundY;
            return point;
        }

        private static bool IsSelf(Collider c, Transform enemy)
        {
            return c != null && (c.transform == enemy || c.transform.IsChildOf(enemy));
        }
    }
}

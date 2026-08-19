using System.Collections.Generic;
using UnityEngine;

namespace Project.Interaction
{
    /// <summary>
    /// Temporarily ignores physics between the player and one resource node so aim / scan
    /// is not shoved away by the node's mesh collider when working up close.
    /// </summary>
    public static class MiningToolResourceCollisionUtility
    {
        private static readonly List<(Collider player, Collider resource)> IgnoredPairs =
            new List<(Collider, Collider)>(16);

        private static ResourceNode ignoredNode;
        private static int ignoreRefCount;

        public static void PushIgnoredResource(ResourceNode node, Transform playerRoot)
        {
            if (node == null || playerRoot == null)
                return;

            if (ignoredNode != null && ignoredNode != node)
                ClearIgnoredResource(force: true);

            if (ignoredNode != node)
                ApplyIgnore(node, playerRoot);

            ignoredNode = node;
            ignoreRefCount++;
        }

        public static void PopIgnoredResource(ResourceNode node)
        {
            if (ignoredNode == null || node != ignoredNode)
                return;

            ignoreRefCount = Mathf.Max(0, ignoreRefCount - 1);
            if (ignoreRefCount == 0)
                ClearIgnoredResource(force: true);
        }

        public static void ClearIgnoredResource()
        {
            ClearIgnoredResource(force: true);
        }

        private static void ApplyIgnore(ResourceNode node, Transform playerRoot)
        {
            Collider[] playerColliders = playerRoot.GetComponentsInChildren<Collider>(true);
            Collider[] resourceColliders = node.GetComponentsInChildren<Collider>(true);
            if (playerColliders.Length == 0 || resourceColliders.Length == 0)
                return;

            for (int p = 0; p < playerColliders.Length; p++)
            {
                Collider playerCollider = playerColliders[p];
                if (playerCollider == null || !playerCollider.enabled)
                    continue;

                for (int r = 0; r < resourceColliders.Length; r++)
                {
                    Collider resourceCollider = resourceColliders[r];
                    if (resourceCollider == null || !resourceCollider.enabled)
                        continue;

                    Physics.IgnoreCollision(playerCollider, resourceCollider, true);
                    IgnoredPairs.Add((playerCollider, resourceCollider));
                }
            }
        }

        private static void ClearIgnoredResource(bool force)
        {
            if (!force && ignoreRefCount > 0)
                return;

            for (int i = 0; i < IgnoredPairs.Count; i++)
            {
                (Collider player, Collider resource) = IgnoredPairs[i];
                if (player != null && resource != null)
                    Physics.IgnoreCollision(player, resource, false);
            }

            IgnoredPairs.Clear();
            ignoredNode = null;
            ignoreRefCount = 0;
        }
    }
}

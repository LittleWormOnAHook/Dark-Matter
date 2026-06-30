using System.Collections.Generic;
using UnityEngine;

namespace Project.Companions
{
    /// <summary>
    /// Prevents follower capsule colliders from physically pushing the player or each other.
    /// World blocking is handled separately via cast-based movement.
    /// </summary>
    public static class FollowerCollisionUtility
    {
        private static readonly List<Collider> RegisteredColliders = new List<Collider>(16);
        private static Collider[] playerColliders = System.Array.Empty<Collider>();

        public static void Register(Collider collider)
        {
            if (collider == null)
                return;

            collider.isTrigger = true;

            if (RegisteredColliders.Contains(collider))
                return;

            RegisteredColliders.Add(collider);
            CachePlayerColliders(force: true);
            IgnorePlayerAndPeers(collider);
        }

        public static void Unregister(Collider collider)
        {
            if (collider == null)
                return;

            RegisteredColliders.Remove(collider);
        }

        public static void RegisterHierarchyColliders(GameObject root)
        {
            if (root == null)
                return;

            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                Register(colliders[i]);
        }

        public static void UnregisterHierarchyColliders(GameObject root)
        {
            if (root == null)
                return;

            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                Unregister(colliders[i]);
        }

        private static void CachePlayerColliders(bool force)
        {
            if (!force && playerColliders.Length > 0)
                return;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            playerColliders = player != null
                ? player.GetComponentsInChildren<Collider>(true)
                : System.Array.Empty<Collider>();
        }

        private static void IgnorePlayerAndPeers(Collider collider)
        {
            for (int i = 0; i < playerColliders.Length; i++)
            {
                Collider playerCollider = playerColliders[i];
                if (playerCollider != null && playerCollider != collider)
                    Physics.IgnoreCollision(collider, playerCollider, true);
            }

            for (int i = 0; i < RegisteredColliders.Count; i++)
            {
                Collider other = RegisteredColliders[i];
                if (other != null && other != collider)
                    Physics.IgnoreCollision(collider, other, true);
            }
        }
    }
}

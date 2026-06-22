using Project.Core;
using Project.Inventory;
using Project.Player;
using UnityEngine;

namespace Project.Interaction
{
    public static class PlayerInteractionUtility
    {
        public static bool IsPlayerCollider(Collider other)
        {
            if (other == null)
                return false;

            if (other.CompareTag("Player"))
                return true;

            if (other.GetComponentInParent<PlayerController>() != null)
                return true;

            return other.GetComponentInParent<InventorySystem>() != null;
        }

        public static bool TryGetPlayerPosition(out Vector3 position)
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
            {
                position = default;
                return false;
            }

            position = player.transform.position;
            return true;
        }

        public static float DistanceToInteractable(Vector3 playerPosition, Collider interactCollider, Vector3 fallbackPosition)
        {
            if (interactCollider != null)
            {
                Vector3 closest = interactCollider.ClosestPoint(playerPosition);
                return Vector3.Distance(playerPosition, closest);
            }

            return Vector3.Distance(playerPosition, fallbackPosition);
        }
    }
}

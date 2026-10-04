using Project.Inventory;
using Project.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Core
{
    /// <summary>
    /// Resolves the live player when the scene can hold more than one copy
    /// (disabled <c>Player_v7 Variant</c> plus an active Combat Variant).
    /// </summary>
    public static class PlayerLocator
    {
        private static readonly string[] PlayerObjectNames =
        {
            "Player_v7 Combat Variant",
            "Player_v7 Variant",
            "Player_v7"
        };

        public static GameObject FindPlayerObject()
        {
            Transform cached = PlayerReference.Transform;
            if (IsLive(cached))
                return cached.gameObject;

            if (cached != null)
                PlayerReference.Unregister(cached);

            GameObject live = FindLivePlayerObject();
            if (live != null)
                PlayerReference.Register(live.transform);

            return live;
        }

        public static PlayerController FindPlayerController()
        {
            GameObject player = FindPlayerObject();
            return player != null ? player.GetComponent<PlayerController>() : null;
        }

        public static InventorySystem FindLiveInventory()
        {
            GameObject player = FindPlayerObject();
            if (player != null)
            {
                InventorySystem onPlayer = player.GetComponent<InventorySystem>()
                    ?? player.GetComponentInChildren<InventorySystem>();
                if (IsLive(onPlayer))
                    return onPlayer;
            }

            InventorySystem[] found = Object.FindObjectsByType<InventorySystem>(FindObjectsInactive.Exclude);
            for (int i = 0; i < found.Length; i++)
            {
                if (IsLive(found[i]))
                    return found[i];
            }

            return null;
        }

        public static EquipmentController FindLiveEquipment()
        {
            InventorySystem inventory = FindLiveInventory();
            if (inventory != null)
            {
                EquipmentController onInventory = inventory.GetComponent<EquipmentController>()
                    ?? inventory.GetComponentInChildren<EquipmentController>();
                if (IsLive(onInventory))
                    return onInventory;
            }

            GameObject player = FindPlayerObject();
            if (player != null)
            {
                EquipmentController onPlayer = player.GetComponent<EquipmentController>()
                    ?? player.GetComponentInChildren<EquipmentController>();
                if (IsLive(onPlayer))
                    return onPlayer;
            }

            EquipmentController[] found = Object.FindObjectsByType<EquipmentController>(FindObjectsInactive.Exclude);
            for (int i = 0; i < found.Length; i++)
            {
                if (IsLive(found[i]))
                    return found[i];
            }

            return null;
        }

        public static T FindOnLivePlayer<T>() where T : Component
        {
            GameObject player = FindPlayerObject();
            if (player == null)
                return null;

            return player.GetComponent<T>() ?? player.GetComponentInChildren<T>();
        }

        public static PlayerInput FindLivePlayerInput()
        {
            PlayerInput onPlayer = FindOnLivePlayer<PlayerInput>();
            if (IsLive(onPlayer))
                return onPlayer;

            PlayerInput[] found = Object.FindObjectsByType<PlayerInput>(FindObjectsInactive.Exclude);
            for (int i = 0; i < found.Length; i++)
            {
                if (IsLive(found[i]))
                    return found[i];
            }

            return null;
        }

        public static bool IsLive(Component component)
        {
            return component != null && component.gameObject.activeInHierarchy;
        }

        public static bool IsLive(GameObject go)
        {
            return go != null && go.activeInHierarchy;
        }

        public static bool IsLive(Transform transform)
        {
            return transform != null && transform.gameObject.activeInHierarchy;
        }

        private static GameObject FindLivePlayerObject()
        {
            GameObject tagged = GameObject.FindWithTag("Player");
            if (IsLive(tagged))
                return tagged;

            PlayerController[] controllers = Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude);
            for (int i = 0; i < controllers.Length; i++)
            {
                if (IsLive(controllers[i]))
                    return controllers[i].gameObject;
            }

            for (int i = 0; i < PlayerObjectNames.Length; i++)
            {
                GameObject named = GameObject.Find(PlayerObjectNames[i]);
                if (IsLive(named))
                    return named;
            }

            return null;
        }
    }
}

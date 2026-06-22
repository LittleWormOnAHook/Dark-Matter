using Project.Player;
using UnityEngine;

namespace Project.Core
{
    public static class PlayerLocator
    {
        public static GameObject FindPlayerObject()
        {
            GameObject tagged = GameObject.FindWithTag("Player");
            if (tagged != null)
                return tagged;

            PlayerController controller = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            return controller != null ? controller.gameObject : null;
        }

        public static PlayerController FindPlayerController()
        {
            GameObject player = FindPlayerObject();
            return player != null ? player.GetComponent<PlayerController>() : null;
        }
    }
}

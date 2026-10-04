using Project.Player;
using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// Cached player transform/camera for world systems (outlines, AI, interaction).
    /// </summary>
    public static class PlayerReference
    {
        public static Transform Transform { get; private set; }
        public static Camera Camera { get; private set; }

        private static PlayerController cachedController;

        public static void Register(Transform playerTransform, Camera gameplayCamera = null)
        {
            if (playerTransform == null)
                return;

            Transform = playerTransform;
            cachedController = playerTransform.GetComponent<PlayerController>();
            if (gameplayCamera != null)
                Camera = gameplayCamera;
            else if (Camera == null)
                Camera = playerTransform.GetComponentInChildren<Camera>();
        }

        public static void Unregister(Transform playerTransform)
        {
            if (playerTransform == null || Transform != playerTransform)
                return;

            Transform = null;
            Camera = null;
            cachedController = null;
        }

        public static PlayerController ResolvePlayerController()
        {
            if (cachedController != null && cachedController.gameObject.activeInHierarchy)
                return cachedController;

            cachedController = null;
            Transform player = ResolveTransform();
            if (player == null)
                return null;

            cachedController = player.GetComponent<PlayerController>();
            return cachedController;
        }

        public static Transform ResolveTransform()
        {
            if (Transform != null && Transform.gameObject.activeInHierarchy)
                return Transform;

            if (Transform != null)
                Unregister(Transform);

            GameObject live = PlayerLocator.FindPlayerObject();
            return live != null ? live.transform : null;
        }

        public static Camera ResolveCamera()
        {
            if (Camera != null)
                return Camera;

            Transform player = ResolveTransform();
            if (player == null)
                return Camera.main;

            Camera cam = player.GetComponentInChildren<Camera>();
            if (cam != null)
                Camera = cam;
            else
                Camera = Camera.main;

            return Camera;
        }
    }
}

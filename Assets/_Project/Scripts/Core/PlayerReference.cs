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

        private static float nextResolveTime;

        public static void Register(Transform playerTransform, Camera gameplayCamera = null)
        {
            if (playerTransform == null)
                return;

            Transform = playerTransform;
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
        }

        public static Transform ResolveTransform()
        {
            if (Transform != null)
                return Transform;

            if (Time.unscaledTime < nextResolveTime)
                return null;

            nextResolveTime = Time.unscaledTime + 0.5f;

            GameObject tagged = GameObject.FindWithTag("Player");
            if (tagged != null)
            {
                Register(tagged.transform);
                return Transform;
            }

            PlayerController controller = Object.FindAnyObjectByType<PlayerController>();
            if (controller != null)
            {
                Register(controller.transform, controller.GameplayCamera);
                return Transform;
            }

            return null;
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

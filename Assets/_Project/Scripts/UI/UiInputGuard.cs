using Project.Core;
using Project.Interaction;
using Project.Player;
using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Prevents UI click-through from accidentally triggering gameplay actions.
    /// </summary>
    public static class UiInputGuard
    {
        private static int blockOpticsActivationUntilFrame = -1;

        public static bool ShouldBlockOpticsActivation => Time.frameCount <= blockOpticsActivationUntilFrame;

        /// <summary>
        /// True while scanner/binocular overlay is active — block weapon/hotbar selection changes.
        /// </summary>
        public static bool BlocksGameplayEquipmentInput
        {
            get
            {
                PlayerController player = PlayerLocator.FindPlayerController();
                if (player != null && player.IsOpticsOpen)
                    return true;

                GameObject playerObject = player != null ? player.gameObject : PlayerLocator.FindPlayerObject();
                if (playerObject != null &&
                    playerObject.TryGetComponent(out OpticsController optics) &&
                    optics.IsActive)
                {
                    return true;
                }

                return false;
            }
        }

        public static void BlockOpticsActivationForFrames(int frames = 2)
        {
            blockOpticsActivationUntilFrame = Time.frameCount + Mathf.Max(1, frames);
        }
    }
}

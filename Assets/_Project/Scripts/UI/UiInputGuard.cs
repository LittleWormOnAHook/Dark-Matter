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

        public static void BlockOpticsActivationForFrames(int frames = 2)
        {
            blockOpticsActivationUntilFrame = Time.frameCount + Mathf.Max(1, frames);
        }
    }
}

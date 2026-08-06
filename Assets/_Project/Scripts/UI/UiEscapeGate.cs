using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.UI
{
    /// <summary>
    /// Ensures Escape is consumed once per frame so layered UI handlers do not all close.
    /// </summary>
    public static class UiEscapeGate
    {
        private static int consumedFrame = -1;

        public static bool TryConsumeEscape()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
                return false;

            int frame = Time.frameCount;
            if (consumedFrame == frame)
                return false;

            consumedFrame = frame;
            return true;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Player
{
    public static class DMPlayerInputActions
    {
        private static PlayerInput cached;
        private static int cachedFrame = -1;
        private static InputActionAsset cachedAsset;

        public static InputAction Find(string actionName)
        {
            try
            {
                PlayerInput pi = ResolvePlayerInput();
                if (pi == null || pi.actions == null || string.IsNullOrEmpty(actionName))
                    return null;
                cachedAsset = pi.actions;
                return cachedAsset.FindAction(actionName, throwIfNotFound: false);
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        public static bool WasPressedThisFrame(string actionName)
        {
            try
            {
                InputAction a = Find(actionName);
                return a != null && a.actionMap != null && a.WasPressedThisFrame();
            }
            catch (System.Exception) { return false; }
        }

        public static bool WasReleasedThisFrame(string actionName)
        {
            try
            {
                InputAction a = Find(actionName);
                return a != null && a.actionMap != null && a.WasReleasedThisFrame();
            }
            catch (System.Exception) { return false; }
        }

        public static bool IsPressed(string actionName)
        {
            try
            {
                InputAction a = Find(actionName);
                return a != null && a.actionMap != null && a.IsPressed();
            }
            catch (System.Exception) { return false; }
        }

        public static void InvalidateCache()
        {
            cached = null;
            cachedFrame = -1;
            cachedAsset = null;
        }

        private static PlayerInput ResolvePlayerInput()
        {
            if (cached != null && cachedFrame == Time.frameCount)
                return cached;

            cachedFrame = Time.frameCount;

            if (cached != null && cached.isActiveAndEnabled && cached.actions != null)
                return cached;

            // Avoid Object.Find* name clash with System.Object under global usings.
            PlayerInput[] found = Resources.FindObjectsOfTypeAll<PlayerInput>();
            for (int i = 0; i < found.Length; i++)
            {
                PlayerInput pi = found[i];
                if (pi == null) continue;
                if (!pi.gameObject.scene.IsValid()) continue;
                cached = pi;
                break;
            }
            return cached;
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Features.Jetpack
{
    /// <summary>
    /// Shared WASD / stick read + camera-relative to character-local conversion for jetpack.
    /// </summary>
    public static class DMJetpackMoveInput
    {
        public static Vector2 ReadPlanarRaw()
        {
            float rawX = 0f;
            float rawY = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    rawX -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    rawX += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                    rawY -= 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                    rawY += 1f;
            }

            Vector2 stick = Vector2.zero;
            if (Gamepad.current != null)
                stick = Gamepad.current.leftStick.ReadValue();

            rawX = Mathf.Clamp(rawX + stick.x, -1f, 1f);
            rawY = Mathf.Clamp(rawY + stick.y, -1f, 1f);

            Vector2 planar = new Vector2(rawX, rawY);
            if (planar.sqrMagnitude > 1f)
                planar.Normalize();

            return planar;
        }

        public static Vector2 ToCharacterLocal(Vector2 cameraRelativePlanar, Transform character, Camera camera)
        {
            if (character == null)
                return cameraRelativePlanar;

            Vector3 forward;
            Vector3 right;

            if (camera != null)
            {
                forward = camera.transform.forward;
                right = camera.transform.right;
            }
            else
            {
                forward = character.forward;
                right = character.right;
            }

            forward.y = 0f;
            right.y = 0f;

            if (forward.sqrMagnitude < 0.0001f)
                forward = character.forward;

            forward.Normalize();
            right.Normalize();

            Vector3 worldMove = forward * cameraRelativePlanar.y + right * cameraRelativePlanar.x;
            Vector3 localMove = character.InverseTransformDirection(worldMove);
            return new Vector2(localMove.x, localMove.z);
        }

        public static Vector2 ApplyDeadzoneAndGain(Vector2 input, float deadzone, float gain)
        {
            if (input.sqrMagnitude <= deadzone * deadzone)
                return Vector2.zero;

            Vector2 scaled = input * gain;
            if (scaled.sqrMagnitude > 1f)
                scaled.Normalize();

            return scaled;
        }
    }
}

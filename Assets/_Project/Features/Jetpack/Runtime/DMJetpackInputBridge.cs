using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Features.Jetpack
{
    /// <summary>
    /// Tap/hold Space for jetpack boost after leaving the ground. First Space on ground stays on Invector jump.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(440)]
    public sealed class DMJetpackInputBridge : MonoBehaviour
    {
        [SerializeField] private DMJetpackController jetpack;
        [SerializeField] private Transform characterTransform;
        [SerializeField] private Camera referenceCamera;

        private bool _spaceHeld;

        public Vector2 LocalMoveInput { get; private set; }
        public float LocalVerticalInput => LocalMoveInput.y;
        public float LocalHorizontalInput => LocalMoveInput.x;
        public bool BoostHeld => _spaceHeld;

        private void Reset()
        {
            jetpack = GetComponent<DMJetpackController>();
            characterTransform = transform;
        }

        private void Awake()
        {
            if (jetpack == null)
                jetpack = GetComponent<DMJetpackController>();
            if (characterTransform == null)
                characterTransform = transform;
            if (referenceCamera == null)
                referenceCamera = Camera.main;
        }

        private void FixedUpdate()
        {
            if (jetpack == null)
                return;

            _spaceHeld = ReadBoostHeld();
            jetpack.SetBoostHeld(_spaceHeld);

            if (referenceCamera == null)
                referenceCamera = Camera.main;

            Vector2 raw = DMJetpackMoveInput.ReadPlanarRaw();
            LocalMoveInput = DMJetpackMoveInput.ToCharacterLocal(raw, characterTransform, referenceCamera);
        }

        /// <summary>Called from <see cref="PioneerShooterMeleeInput.JumpInput"/> before the normal jump.</summary>
        public bool TryHandleJumpPress()
        {
            if (jetpack == null || !ReadJumpPressedThisFrame())
                return false;

            return jetpack.TryIgniteBoostOnJumpPress();
        }

        private static bool ReadJumpPressedThisFrame()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                return true;

            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                return true;

            return false;
        }

        private static bool ReadBoostHeld()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
                return true;

            if (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed)
                return true;

            return false;
        }
    }
}

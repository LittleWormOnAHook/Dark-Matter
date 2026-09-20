using Invector.vCharacterController;
using Project.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Player.Invector
{
    /// <summary>
    /// Tracks Keyboard&amp;Mouse vs Gamepad and applies Invector GenericInput mute/unmute.
    /// stamp: controller-scheme-router 0920 — Anthony Ctrl+R verify.
    /// </summary>
    [DefaultExecutionOrder(-270)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public class DMInputSchemeRouter : MonoBehaviour
    {
        public const string GamepadScheme = "Gamepad";
        public const string KeyboardMouseScheme = "Keyboard&Mouse";
        public const string Stamp = "controller-scheme-router 0920";

        private static DMInputSchemeRouter instance;

        private PlayerInput playerInput;
        private PioneerShooterMeleeInput shooterInput;
        private bool gamepadSchemeActive;
        private bool stampedLog;

        public static DMInputSchemeRouter Instance => instance;

        public static bool IsGamepadScheme =>
            instance != null && instance.gamepadSchemeActive;

        public static bool IsKeyboardMouseScheme => !IsGamepadScheme;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            shooterInput = GetComponent<PioneerShooterMeleeInput>();
        }

        private void OnEnable()
        {
            instance = this;
            if (playerInput != null)
                playerInput.onControlsChanged += OnControlsChanged;

            RefreshScheme(force: true);
        }

        private void OnDisable()
        {
            if (playerInput != null)
                playerInput.onControlsChanged -= OnControlsChanged;

            if (instance == this)
                instance = null;

            ApplySchemeState(false, force: true);
        }

        private void Update()
        {
            if (!Application.isPlaying || !GameSession.HasStarted)
                return;

            PromoteKeyboardMouseOnLocalActivity();
            RefreshScheme(force: false);
        }

        private void OnControlsChanged(PlayerInput input)
        {
            RefreshScheme(force: true);
        }

        private void PromoteKeyboardMouseOnLocalActivity()
        {
            if (playerInput == null || playerInput.currentControlScheme == KeyboardMouseScheme)
                return;

            if (!DetectKeyboardMouseActivity())
                return;

            playerInput.SwitchCurrentControlScheme(KeyboardMouseScheme);
        }

        private static bool DetectKeyboardMouseActivity()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.delta.ReadValue().sqrMagnitude > 0.25f)
                    return true;

                if (mouse.leftButton.wasPressedThisFrame
                    || mouse.rightButton.wasPressedThisFrame
                    || mouse.middleButton.wasPressedThisFrame
                    || mouse.scroll.ReadValue().sqrMagnitude > 0.01f)
                    return true;
            }

            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.anyKey.wasPressedThisFrame;
        }

        private void RefreshScheme(bool force)
        {
            if (playerInput == null)
                return;

            bool gamepad = playerInput.currentControlScheme == GamepadScheme;
            if (!force && gamepad == gamepadSchemeActive)
                return;

            ApplySchemeState(gamepad, force);
        }

        private void ApplySchemeState(bool gamepad, bool force)
        {
            if (!force && gamepad == gamepadSchemeActive)
                return;

            gamepadSchemeActive = gamepad;

            if (shooterInput == null)
                shooterInput = GetComponent<PioneerShooterMeleeInput>();

            PioneerInvectorGenericInputGate.ApplyGamepadMute(shooterInput, gamepad);
            SyncInvectorInputDevice(gamepad);

            if (!stampedLog && Application.isPlaying)
            {
                stampedLog = true;
                Debug.Log($"[DMInputSchemeRouter] {Stamp} GenericInput {(gamepad ? "muted" : "restored")} for scheme");
            }
        }

        private static void SyncInvectorInputDevice(bool gamepadScheme)
        {
            if (vInput.instance == null)
                return;

            // Gamepad scheme still uses Joystick sensitivity multipliers for RotateCamera stick look.
            vInput.instance.inputDevice = gamepadScheme
                ? InputDevice.Joystick
                : InputDevice.MouseKeyboard;
        }
    }
}

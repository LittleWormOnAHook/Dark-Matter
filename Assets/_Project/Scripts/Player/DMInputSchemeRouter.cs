using global::Invector.vCharacterController;
using Project.Core;
using Project.Features.Locomotion;
using UnityEngine;
using UnityEngine.InputSystem;
using InvInputDevice = global::Invector.vCharacterController.InputDevice;

namespace Project.Player
{
    /// <summary>
    /// Tracks Keyboard&Mouse vs Gamepad and applies Invector GenericInput mute/unmute.
    /// stamp: controller-scheme-router 0920f — safe scheme switch (no Invalid user spam).
    /// </summary>
    [DefaultExecutionOrder(-270)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInput))]
    public class DMInputSchemeRouter : MonoBehaviour
    {
        public const string GamepadScheme = "Gamepad";
        public const string KeyboardMouseScheme = "Keyboard&Mouse";
        public const string Stamp = "controller-scheme-router 0920f";

        private static DMInputSchemeRouter instance;

        private PlayerInput playerInput;
        private vThirdPersonInput shooterInput;
        private bool gamepadSchemeActive;
        private bool stampedLog;
        private float nextSchemeSwitchTime;

        public static DMInputSchemeRouter Instance => instance;

        public static bool IsGamepadScheme =>
            instance != null && instance.gamepadSchemeActive;

        public static bool IsKeyboardMouseScheme => !IsGamepadScheme;

        /// <summary>Instance mirror of <see cref="IsGamepadScheme"/> for Controls / UI hosts.</summary>
        public bool IsGamepad => gamepadSchemeActive;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            shooterInput = GetComponent<vThirdPersonInput>();
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
            if (playerInput == null)
                return;

            if (playerInput.currentControlScheme == KeyboardMouseScheme)
                return;

            // While on gamepad: only deliberate KBM (key / mouse button), not tiny mouse delta —
            // residual pointer motion must not yank the scheme and brick pad InputUser.
            if (!DetectDeliberateKeyboardMouse())
                return;

            if (Time.unscaledTime < nextSchemeSwitchTime)
                return;

            nextSchemeSwitchTime = Time.unscaledTime + 0.35f;
            TrySwitchScheme(KeyboardMouseScheme, Keyboard.current, Mouse.current);
        }

        private static bool DetectDeliberateKeyboardMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame
                    || mouse.rightButton.wasPressedThisFrame
                    || mouse.middleButton.wasPressedThisFrame)
                    return true;
            }

            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.anyKey.wasPressedThisFrame;
        }

        private void TrySwitchScheme(string scheme, params UnityEngine.InputSystem.InputDevice[] devices)
        {
            if (playerInput == null || string.IsNullOrEmpty(scheme))
                return;

            // PlayerInput with no valid user throws InvalidOperationException and breaks pad.
            try
            {
                if (!playerInput.user.valid)
                    return;

                System.Collections.Generic.List<UnityEngine.InputSystem.InputDevice> list = new System.Collections.Generic.List<UnityEngine.InputSystem.InputDevice>(4);
                if (devices != null)
                {
                    for (int i = 0; i < devices.Length; i++)
                    {
                        if (devices[i] != null)
                            list.Add(devices[i]);
                    }
                }

                if (list.Count == 0)
                    return;

                playerInput.SwitchCurrentControlScheme(scheme, list.ToArray());
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DMInputSchemeRouter] scheme switch skipped: {ex.Message}");
            }
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
                shooterInput = GetComponent<vThirdPersonInput>();

            Project.Player.Invector.PioneerInvectorGenericInputGate.ApplyGamepadMute(shooterInput, gamepad);
            SyncInvectorInputDevice(gamepad);

            if (!gamepad)
            {
                DMLocomotionGaitController gait = GetComponent<DMLocomotionGaitController>();
                gait?.ClearGamepadLocomotionLatches();
            }

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

            vInput.instance.inputDevice = gamepadScheme
                ? InvInputDevice.Joystick
                : InvInputDevice.MouseKeyboard;
        }
    }
}
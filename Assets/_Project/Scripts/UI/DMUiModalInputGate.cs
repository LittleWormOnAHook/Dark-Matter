using Project.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.UI
{
    /// <summary>
    /// While journal / pause / popups are open: keep UI Navigate + A/B only.
    /// Disables the Player action map so sticks and face buttons do not drive gameplay.
    /// stamp: modal-player-map-gate 0920k
    /// </summary>
    public static class DMUiModalInputGate
    {
        private static InputActionMap _playerMap;
        private static InputActionAsset _playerMapAsset;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _playerMap = null;
            _playerMapAsset = null;
        }

        public static void Tick()
        {
            if (!Application.isPlaying)
                return;

            bool lockGameplay = GameplayKeyboardShortcuts.IsGameplayInputLockedByUi();
            InputActionMap player = ResolvePlayerMap();
            if (player == null)
                return;

            // Always sync — do not rely on sticky static flags (Fast Play keeps them across plays).
            if (lockGameplay)
            {
                if (player.enabled)
                    player.Disable();
                DMUiGamepadNavigation.EnsureConfigured();
            }
            else if (!player.enabled)
            {
                player.Enable();
            }
        }

        private static InputActionMap ResolvePlayerMap()
        {
            PlayerInput pi = Object.FindAnyObjectByType<PlayerInput>();
            if (pi == null || pi.actions == null)
            {
                _playerMap = null;
                _playerMapAsset = null;
                return null;
            }

            if (_playerMap != null && _playerMapAsset != pi.actions)
            {
                _playerMap = null;
                _playerMapAsset = null;
                Project.Player.DMPlayerInputActions.InvalidateCache();
            }

            if (_playerMap != null)
                return _playerMap;

            _playerMapAsset = pi.actions;
            _playerMap = pi.actions.FindActionMap("Player", throwIfNotFound: false);
            return _playerMap;
        }
    }
}

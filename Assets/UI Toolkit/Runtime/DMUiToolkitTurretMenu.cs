using Project.Combat.Turrets;
using Project.Core;
using Project.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Project.UI
{
    [DefaultExecutionOrder(-360)]
    [DisallowMultipleComponent]
    public sealed class DMUiToolkitTurretMenu : MonoBehaviour
    {
        public const string PauseReason = GameplayMenuTime.ReasonTurretMenu;

        private static DMUiToolkitTurretMenu instance;

        private UIDocument document;
        private VisualElement root;
        private Button closeButton;
        private Button modeAuto;
        private Button ammoPrev;
        private Button ammoNext;
        private Button enterButton;
        private Label ammoLabel;
        private bool bound;
        private bool open;
        private DMTurretInteract turret;
        private int openedFrame = -1;
        private bool pauseHidden;

        public static bool IsOpen => instance != null && instance.open;

        public static DMUiToolkitTurretMenu EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.TurretMenuName,
                DMUiToolkitOverlayDocument.TurretMenuUxml,
                DMUiToolkitOverlayDocument.TurretMenuUss,
                DMUiToolkitOverlayDocument.TurretMenuSort);
            if (doc == null)
                return null;

            DMUiToolkitTurretMenu host = doc.GetComponent<DMUiToolkitTurretMenu>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitTurretMenu>();
            host.document = doc;
            if (doc.GetComponent<DMTurretPlayerControlTicker>() == null)
                doc.gameObject.AddComponent<DMTurretPlayerControlTicker>();
            host.BindTree();
            return host;
        }

        public static bool TryShow(DMTurretInteract interact)
        {
            if (interact == null)
                return false;
            DMUiToolkitTurretMenu host = EnsureHost();
            if (host == null)
                return false;
            host.ShowInternal(interact);
            return true;
        }

        public static bool TryHide()
        {
            if (instance == null || !instance.open)
                return false;
            instance.HideInternal();
            return true;
        }

        public static bool TryHandleBack()
        {
            if (!IsOpen)
                return false;
            TryHide();
            return true;
        }

        private void BindTree()
        {
            if (bound || document == null)
                return;
            root = document.rootVisualElement;
            if (root == null)
                return;
            root = root.Q<VisualElement>("turret-root") ?? root;
            closeButton = root.Q<Button>("turret-close");
            modeAuto = root.Q<Button>("turret-mode-auto");
            ammoPrev = root.Q<Button>("turret-ammo-prev");
            ammoNext = root.Q<Button>("turret-ammo-next");
            enterButton = root.Q<Button>("turret-enter");
            ammoLabel = root.Q<Label>("turret-ammo-label");

            if (closeButton != null)
                closeButton.clicked += HideInternal;
            if (modeAuto != null)
                modeAuto.clicked += SetAuto;
            if (ammoPrev != null)
                ammoPrev.clicked += () => CycleAmmo(-1);
            if (ammoNext != null)
                ammoNext.clicked += () => CycleAmmo(1);
            if (enterButton != null)
                enterButton.clicked += EnterControl;

            // 2026-10-20e: gamepad / D-pad focus order (UITK navigation), mouse hover also moves focus so styles don't fight.
            Button[] order = { modeAuto, enterButton, ammoPrev, ammoNext, closeButton };
            foreach (Button b in order)
            {
                if (b == null)
                    continue;
                b.focusable = true;
                Button bb = b;
                b.RegisterCallback<PointerEnterEvent>(_ => { if (open && !pauseHidden) bb.Focus(); });
            }

            bound = true;
            DMUiToolkitOverlayDocument.SetShown(root, false);
        }

        private void ShowInternal(DMTurretInteract interact)
        {
            BindTree();
            turret = interact;
            open = true;
            openedFrame = Time.frameCount;
            instance = this;
            Refresh();
            DMUiToolkitOverlayDocument.SetShown(root, true);
            DMUiToolkitOverlayDocument.PromoteInteractiveOverlay(document);
            ApplyOverlaySession(true);
            FocusDefault(); // 2026-10-20e: gamepad starts on Enter Turret
        }

        private void FocusDefault()
        {
            Button first = enterButton ?? modeAuto ?? closeButton;
            first?.Focus();
        }

        private bool HasFocusInPanel()
        {
            Focusable f = root?.focusController?.focusedElement;
            return f is VisualElement ve && root.Contains(ve);
        }

        private void HideInternal()
        {
            open = false;
            pauseHidden = false;
            turret = null;
            DMUiToolkitOverlayDocument.SetShown(root, false);
            ApplyOverlaySession(false);
        }

        private static void ApplyOverlaySession(bool overlayOpen)
        {
            GameplayMenuTime.SetPause(PauseReason, overlayOpen);
            PlayerController player = PlayerLocator.FindPlayerController();
            if (player == null)
                return;
            player.SetGameplayPaused(overlayOpen);
            if (overlayOpen)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
            }

            player.ApplyCursorState();
        }

        private void SetAuto()
        {
            turret?.Runtime?.SetControlMode(DMTurretControlMode.Auto);
            Refresh();
        }

        private void CycleAmmo(int delta)
        {
            if (turret == null || turret.Runtime == null)
                return;
            DMTurretAmmoCatalog.CycleMixed(turret.Runtime, delta);
            Refresh();
        }

        private void EnterControl()
        {
            if (turret == null || turret.Runtime == null)
                return;
            DMTurretRuntime runtime = turret.Runtime;
            HideInternal();
            DMTurretPlayerControl.TryEnter(runtime);
        }

        private void Refresh()
        {
            if (turret == null || turret.Runtime == null)
                return;
            modeAuto?.EnableInClassList(
                "dmg-turret-selected",
                turret.Runtime.ControlMode == DMTurretControlMode.Auto);
            if (ammoLabel != null)
                ammoLabel.text = DMTurretAmmoCatalog.GetMixedLabel(turret.Runtime);
            if (enterButton != null)
                DMUiToolkitOverlayDocument.SetShown(enterButton, true);
        }

        private void Update()
        {
            if (!open)
                return;
            if (turret == null || turret.Runtime == null || turret.Runtime.IsDestroyed)
            {
                HideInternal();
                return;
            }

            // 2026-10-20e: while the pause menu is up, hide this panel + drop picking so the two never overlap / flicker.
            bool pauseUp = MainMenuController.IsPauseOverlayActive || DMUiToolkitMainMenu.IsVisible;
            if (pauseUp != pauseHidden)
            {
                pauseHidden = pauseUp;
                DMUiToolkitOverlayDocument.SetShown(root, !pauseUp);
                if (!pauseUp)
                {
                    DMUiToolkitOverlayDocument.PromoteInteractiveOverlay(document);
                    ApplyOverlaySession(true);
                    FocusDefault();
                }
            }

            if (pauseHidden || Time.frameCount <= openedFrame)
                return;

            // Esc / gamepad B go through GameplayKeyboardShortcuts.HandleEscapePressed (TryHandleBack); E toggles.
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                HideInternal();
                return;
            }

            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.buttonEast.wasPressedThisFrame)
                {
                    GameplayKeyboardShortcuts.HandleEscapePressed(); // same frame-guarded Back path
                    return;
                }

                if (!HasFocusInPanel()
                    && (pad.dpad.ReadValue().sqrMagnitude > 0.25f || pad.leftStick.ReadValue().sqrMagnitude > 0.25f
                        || pad.buttonSouth.wasPressedThisFrame))
                    FocusDefault();
            }
        }
    }
}

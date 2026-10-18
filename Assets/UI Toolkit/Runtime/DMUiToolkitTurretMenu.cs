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
            if (closeButton != null)
                closeButton.Focus();
        }

        private void HideInternal()
        {
            open = false;
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

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null
                && Time.frameCount > openedFrame
                && (keyboard.escapeKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame))
                HideInternal();
        }
    }
}

using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK GAME OVER overlay with Bio Gel revive / Retry / End Game.
    /// Forwards from UIManager.ShowDeathPopup.
    /// </summary>
    [DefaultExecutionOrder(-376)]
    [DisallowMultipleComponent]
    public class DMUiToolkitDeath : MonoBehaviour
    {
        private static DMUiToolkitDeath instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement gelIcon;
        private Label gelCount;
        private Button useGelButton;
        private Button retryButton;
        private Button exitButton;
        private bool bound;
        private bool open;
        private bool uguiHidden;

        public static bool IsOpen => instance != null && instance.open;

        public static DMUiToolkitDeath EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.DeathName,
                DMUiToolkitOverlayDocument.DeathUxml,
                DMUiToolkitOverlayDocument.DeathUss,
                DMUiToolkitOverlayDocument.DeathSort);
            if (doc == null)
                return null;

            DMUiToolkitDeath host = doc.GetComponent<DMUiToolkitDeath>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitDeath>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        public static bool TryShow()
        {
            if (!DMUiToolkitHud.IsDriving)
                return false;

            DMUiToolkitDeath host = EnsureHost();
            if (host == null)
                return false;

            host.ShowInternal();
            return true;
        }

        public static void Hide()
        {
            instance?.HideInternal();
        }

        private void Awake()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void LateUpdate()
        {
            if (!bound)
                return;

            if (open)
            {
                if (!uguiHidden)
                {
                    HideUgui();
                    uguiHidden = true;
                }

                // Ghost-pause / cursor-restore can re-lock after Show; keep Game Over clickable.
                EnsurePointerForOpenDeath();
            }
            else
            {
                uguiHidden = false;
            }
        }

        internal void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("death-root") ?? tree;
            if (root != null && root.pickingMode != PickingMode.Position)
                root.pickingMode = PickingMode.Position;

            gelIcon = tree.Q<VisualElement>("death-gel-icon");
            gelCount = tree.Q<Label>("death-gel-count");
            Button nextUseGel = tree.Q<Button>("death-use-gel");
            Button nextRetry = tree.Q<Button>("death-retry");
            Button nextExit = tree.Q<Button>("death-exit");
            WireButtons(nextUseGel, nextRetry, nextExit);

            if (!open)
                DMUiToolkitOverlayDocument.SetShown(root, false);
            bound = root != null;
        }

        private void WireButtons(Button nextUseGel, Button nextRetry, Button nextExit)
        {
            if (useGelButton != nextUseGel)
            {
                if (useGelButton != null)
                    useGelButton.clicked -= HandleUseGel;
                useGelButton = nextUseGel;
                if (useGelButton != null)
                {
                    useGelButton.clicked -= HandleUseGel;
                    useGelButton.clicked += HandleUseGel;
                    if (useGelButton.pickingMode != PickingMode.Position)
                        useGelButton.pickingMode = PickingMode.Position;
                }
            }

            if (retryButton != nextRetry)
            {
                if (retryButton != null)
                    retryButton.clicked -= HandleRetry;
                retryButton = nextRetry;
                if (retryButton != null)
                {
                    retryButton.clicked -= HandleRetry;
                    retryButton.clicked += HandleRetry;
                    if (retryButton.pickingMode != PickingMode.Position)
                        retryButton.pickingMode = PickingMode.Position;
                }
            }

            if (exitButton != nextExit)
            {
                if (exitButton != null)
                    exitButton.clicked -= HandleExit;
                exitButton = nextExit;
                if (exitButton != null)
                {
                    exitButton.clicked -= HandleExit;
                    exitButton.clicked += HandleExit;
                    if (exitButton.pickingMode != PickingMode.Position)
                        exitButton.pickingMode = PickingMode.Position;
                }
            }
        }

        private void ShowInternal()
        {
            BindTree();
            DMUiToolkitOverlayDocument.SetShown(root, true);
            DMUiToolkitOverlayDocument.PromoteInteractiveOverlay(document);
            open = true;
            RefreshGelRow();
            EnsurePointerForOpenDeath();

            if (useGelButton != null && useGelButton.enabledSelf)
                useGelButton.Focus();
            else if (retryButton != null)
                retryButton.Focus();
        }

        private void RefreshGelRow()
        {
            ItemData gel = DMDeathRevive.ResolveBioGel();
            int count = 0;
            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
                count = DMDeathRevive.CountBioGel(player.GetComponent<InventorySystem>());

            if (gelCount != null)
                gelCount.text = count.ToString();

            if (gelIcon != null
                && !DMUiToolkitStyle.TrySetItemIcon(gelIcon, gel)
                && !DMUiToolkitStyle.TrySetSpriteBackground(gelIcon, Resources.Load<Sprite>("UI/Game Icons/Bio Gel")))
                DMUiToolkitStyle.ClearBackgroundImage(gelIcon);

            if (useGelButton != null)
                useGelButton.SetEnabled(count > 0);
        }

        private void HideInternal()
        {
            if (!open && (root == null || root.resolvedStyle.display == DisplayStyle.None))
            {
                RestoreGameplayPointerFlags();
                return;
            }

            open = false;
            DMUiToolkitOverlayDocument.SetShown(root, false);
            RestoreGameplayPointerFlags();
        }

        /// <summary>
        /// Unlock mouse, cancel gameplay cursor relock, and mark player UI-captured so
        /// RecoverGhostUiLocks / ApplyCursorState cannot steal clicks while Game Over is up.
        /// </summary>
        public static void EnsurePointerForOpenDeath()
        {
            if (!IsOpen)
                return;

            GameplayInputRecovery.CancelPendingCursorRestore();

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
            if (pc != null)
            {
                // Reuse inventory-open as the existing "UI has the pointer" flag used by combat/look gates.
                if (!pc.IsInventoryOpen)
                    pc.SetInventoryOpen(true);
                else
                    pc.ApplyCursorState();
            }

            CameraController cam = Object.FindAnyObjectByType<CameraController>();
            if (cam != null)
                cam.SetInventoryOpen(true);
        }

        private static void RestoreGameplayPointerFlags()
        {
            PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
            if (pc != null && pc.IsInventoryOpen)
                pc.SetInventoryOpen(false);

            CameraController cam = Object.FindAnyObjectByType<CameraController>();
            if (cam != null)
                cam.SetInventoryOpen(false);
        }

        private void HandleUseGel()
        {
            if (!open)
                return;

            GameObject player = PlayerLocator.FindPlayerObject();
            InventorySystem inventory = player != null ? player.GetComponent<InventorySystem>() : null;
            if (!DMDeathRevive.TryConsumeBioGel(inventory))
            {
                RefreshGelRow();
                return;
            }

            HideInternal();
            UIManager ui = Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (ui != null)
                ui.RespawnPlayer();
            else
                player?.GetComponent<PlayerDeathHandler>()?.Respawn();
        }

        private void HandleRetry()
        {
            if (!open)
                return;

            HideInternal();
            UIManager ui = Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (ui != null)
                ui.RetryFromDeath();
            else if (!GameSaveSystem.TryLoadNewestSave(out _))
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private void HandleExit()
        {
            if (!open)
                return;

            HideInternal();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void HideUgui()
        {
            if (!DMUiToolkitHud.IsDriving || instance == null || !instance.open)
                return;

            UIManager ui = Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
            if (ui == null)
                return;

            Transform death = ui.transform.Find("DeathPopupPanel");
            if (death != null && death.gameObject.activeSelf)
                death.gameObject.SetActive(false);
        }
    }

    internal static class DMDeathRevive
    {
        private const string BioGelName = "Bio Gel";
        private const string BioGelAssetName = "Bio_Gel";

        public static ItemData ResolveBioGel()
        {
            return ItemRegistry.Resolve(BioGelName) ?? ItemRegistry.Resolve(BioGelAssetName);
        }

        public static int CountBioGel(InventorySystem inventory)
        {
            if (inventory == null || inventory.slots == null)
                return 0;

            ItemData gel = ResolveBioGel();
            int count = gel != null ? inventory.CountItem(gel) : 0;
            if (count > 0)
                return count;

            for (int i = 0; i < inventory.slots.Count; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot == null || slot.IsEmpty || slot.item == null)
                    continue;

                if (IsBioGel(slot.item))
                    count += slot.amount;
            }

            return count;
        }

        public static bool TryConsumeBioGel(InventorySystem inventory)
        {
            if (inventory == null)
                return false;

            ItemData gel = ResolveBioGel();
            if (gel != null && inventory.RemoveItem(gel, 1))
                return true;

            if (inventory.slots == null)
                return false;

            for (int i = 0; i < inventory.slots.Count; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot == null || slot.IsEmpty || slot.item == null || !IsBioGel(slot.item))
                    continue;

                return inventory.RemoveItemAt(i, 1);
            }

            return false;
        }

        private static bool IsBioGel(ItemData item)
        {
            if (item == null)
                return false;

            return string.Equals(item.itemName, BioGelName, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.name, BioGelAssetName, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}

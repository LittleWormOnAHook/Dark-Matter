using System.Collections;
using System.Collections.Generic;
using Project.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    [DisallowMultipleComponent]
    public class DMUiToolkitSaveSlots : MonoBehaviour
    {
        public const string Name = "UITK_SaveSlots";
        public const int Sort = 21200;
        public const string UxmlPath = "Assets/UI Toolkit/Screens/SaveSlots.uxml";

        private enum ConfirmKind
        {
            None,
            Overwrite,
            Delete
        }

        private static DMUiToolkitSaveSlots instance;

        private UIDocument document;
        private VisualElement root;
        private Label titleLabel;
        private Label hintLabel;
        private ScrollView body;
        private VisualElement contextHost;
        private Button contextOverwrite;
        private Button contextDelete;
        private VisualElement confirmHost;
        private VisualElement confirmVeil;
        private Label confirmTitle;
        private Label confirmBody;
        private Button confirmOk;
        private Button confirmCancel;
        private readonly List<Button> slotButtons = new List<Button>();
        private readonly List<VisualElement> slotPreviews = new List<VisualElement>();
        private SaveSlotsPanelController.Mode currentMode;
        private ConfirmKind confirmKind;
        private int contextSlotIndex = -1;
        private int confirmSlotIndex = -1;
        private bool open;

        public static bool IsOpen => instance != null && instance.open;

        public static DMUiToolkitSaveSlots EnsureHost()
        {
            if (!DMUiToolkitConfig.IsEnabled)
                return null;

            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitMenuDocument.Ensure(Name, UxmlPath, Sort);
            if (doc == null)
                return null;

            DMUiToolkitSaveSlots host = doc.GetComponent<DMUiToolkitSaveSlots>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitSaveSlots>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        public static void Open(SaveSlotsPanelController.Mode mode)
        {
            DMUiToolkitSaveSlots host = EnsureHost();
            host?.ShowInternal(mode);
        }

        public static void Close()
        {
            instance?.HideInternal();
        }

        public static void RefreshIfOpen()
        {
            if (instance != null && instance.open)
                instance.RefreshSlots();
        }

        public static bool HandleBack()
        {
            if (!IsOpen)
                return false;

            if (instance.HideConfirm())
                return true;

            if (instance.HideContext())
                return true;

            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            menu?.ClearPendingSaveScreenshot();
            Close();
            menu?.RestoreMenuAfterSubPanel();
            return true;
        }

        private void Awake()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("saveslots-root") ?? tree;
            titleLabel = tree.Q<Label>("saveslots-title");
            hintLabel = tree.Q<Label>("saveslots-hint");
            body = tree.Q<ScrollView>("saveslots-body");
            Button backButton = tree.Q<Button>("saveslots-back");
            contextHost = tree.Q<VisualElement>("saveslots-context");
            contextOverwrite = tree.Q<Button>("saveslots-context-overwrite");
            contextDelete = tree.Q<Button>("saveslots-context-delete");
            confirmHost = tree.Q<VisualElement>("saveslots-confirm");
            confirmVeil = tree.Q<VisualElement>("saveslots-confirm-veil");
            confirmTitle = tree.Q<Label>("saveslots-confirm-title");
            confirmBody = tree.Q<Label>("saveslots-confirm-body");
            confirmOk = tree.Q<Button>("saveslots-confirm-ok");
            confirmCancel = tree.Q<Button>("saveslots-confirm-cancel");

            if (backButton != null)
            {
                backButton.clicked -= OnBackClicked;
                backButton.clicked += OnBackClicked;
            }

            if (contextOverwrite != null)
            {
                contextOverwrite.clicked -= OnContextOverwriteClicked;
                contextOverwrite.clicked += OnContextOverwriteClicked;
            }

            if (contextDelete != null)
            {
                contextDelete.clicked -= OnContextDeleteClicked;
                contextDelete.clicked += OnContextDeleteClicked;
            }

            if (confirmOk != null)
            {
                confirmOk.clicked -= OnConfirmOkClicked;
                confirmOk.clicked += OnConfirmOkClicked;
            }

            if (confirmCancel != null)
            {
                confirmCancel.clicked -= OnConfirmCancelClicked;
                confirmCancel.clicked += OnConfirmCancelClicked;
            }

            if (confirmVeil != null)
            {
                confirmVeil.UnregisterCallback<PointerDownEvent>(OnConfirmVeilPointerDown);
                confirmVeil.RegisterCallback<PointerDownEvent>(OnConfirmVeilPointerDown);
            }

            if (root != null)
            {
                root.UnregisterCallback<PointerDownEvent>(OnRootPointerDown);
                root.RegisterCallback<PointerDownEvent>(OnRootPointerDown);
            }

            if (body != null && slotButtons.Count == 0)
                BuildSlotRows();

            HideContext();
            HideConfirm();
            HideInternal();
        }

        private void BuildSlotRows()
        {
            slotButtons.Clear();
            slotPreviews.Clear();
            body.Clear();

            for (int i = 0; i < GameSaveSystem.SlotCount; i++)
            {
                int slotIndex = i;
                VisualElement row = new VisualElement();
                row.AddToClassList("dmg-menu-slot-row");

                VisualElement preview = new VisualElement { pickingMode = PickingMode.Ignore };
                preview.AddToClassList("dmg-menu-slot-preview");
                row.Add(preview);
                slotPreviews.Add(preview);

                Button button = new Button { pickingMode = PickingMode.Position };
                button.AddToClassList("dmg-menu-slot-button");
                button.clicked += () => OnSlotSelected(slotIndex);
                button.RegisterCallback<PointerDownEvent>(evt => OnSlotPointerDown(evt, slotIndex), TrickleDown.TrickleDown);
                button.RegisterCallback<ContextClickEvent>(evt => OnSlotContextClick(evt, slotIndex));
                row.Add(button);
                slotButtons.Add(button);

                body.Add(row);
            }
        }

        private void ShowInternal(SaveSlotsPanelController.Mode mode)
        {
            BindTree();
            currentMode = mode;
            if (titleLabel != null)
                titleLabel.text = mode == SaveSlotsPanelController.Mode.Save ? "SAVE GAME — SELECT SLOT" : "LOAD GAME — SELECT SLOT";

            if (hintLabel != null)
            {
                hintLabel.text = mode == SaveSlotsPanelController.Mode.Save
                    ? "Left-click an empty slot to save, or a used slot to overwrite. Right-click a used slot to delete. Slots 1–2 autosave every 5 minutes."
                    : "Left-click a used slot to load. Right-click a used slot to delete. Slots 1–2 are autosaves.";
            }

            HideContext();
            HideConfirm();
            RefreshSlots();
            open = true;
            DMUiToolkitOverlayDocument.SetShown(root, true);
            DMUiToolkitOverlayDocument.PromoteInteractiveOverlay(document);
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        private void HideInternal()
        {
            open = false;
            HideContext();
            HideConfirm();
            if (root != null)
                DMUiToolkitOverlayDocument.SetShown(root, false);
        }

        private void RefreshSlots()
        {
            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();

            for (int i = 0; i < GameSaveSystem.SlotCount; i++)
            {
                SaveSlotInfo info = GameSaveSystem.GetSlotInfo(i);
                bool occupied = info.HasData;
                Button button = slotButtons[i];
                VisualElement preview = slotPreviews[i];

                if (button != null)
                {
                    string title = GameSaveSystem.GetSlotTitle(i);
                    button.text = occupied
                        ? $"{title}\n{info.GetSummaryLine()}"
                        : $"{title}\nEmpty";
                    button.SetEnabled(currentMode == SaveSlotsPanelController.Mode.Save || occupied);
                }

                ApplyPreview(preview, i, info, menu);
            }
        }

        private void ApplyPreview(VisualElement preview, int slotIndex, SaveSlotInfo info, MainMenuController menu)
        {
            if (preview == null)
                return;

            preview.style.backgroundImage = StyleKeyword.None;

            if (currentMode == SaveSlotsPanelController.Mode.Save && menu != null && menu.PendingSaveScreenshot != null)
            {
                DMUiToolkitStyle.TrySetTextureBackground(preview, menu.PendingSaveScreenshot, ScaleMode.ScaleToFit);
                return;
            }

            if (!info.HasScreenshot)
                return;

            Texture2D texture = SaveSlotScreenshotUtility.LoadScreenshot(slotIndex);
            DMUiToolkitStyle.TrySetTextureBackground(preview, texture, ScaleMode.ScaleToFit);
        }

        private void OnSlotSelected(int slotIndex)
        {
            if (IsConfirmOpen)
                return;

            HideContext();

            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            if (menu == null)
                return;

            if (currentMode == SaveSlotsPanelController.Mode.Save)
            {
                if (GameSaveSystem.HasSaveInSlot(slotIndex))
                {
                    ShowConfirm(ConfirmKind.Overwrite, slotIndex);
                    return;
                }

                menu.SaveToSlot(slotIndex);
                return;
            }

            menu.LoadFromSlot(slotIndex);
            Close();
        }

        private void OnSlotPointerDown(PointerDownEvent evt, int slotIndex)
        {
            if (evt.button != 1)
                return;

            evt.StopPropagation();
            ShowSlotContext(slotIndex, evt.position);
        }

        private void OnSlotContextClick(ContextClickEvent evt, int slotIndex)
        {
            evt.StopPropagation();
            ShowSlotContext(slotIndex, evt.mousePosition);
        }

        private void ShowSlotContext(int slotIndex, Vector2 panelPosition)
        {
            if (!GameSaveSystem.HasSaveInSlot(slotIndex))
            {
                HideContext();
                return;
            }

            HideConfirm();
            contextSlotIndex = slotIndex;
            bool saveMode = currentMode == SaveSlotsPanelController.Mode.Save;
            DMUiToolkitOverlayDocument.SetShown(contextOverwrite, saveMode);
            PositionContext(panelPosition);
            if (contextHost != null)
                contextHost.BringToFront();
            DMUiToolkitOverlayDocument.SetShown(contextHost, true);
        }

        private void PositionContext(Vector2 panelPosition)
        {
            if (contextHost == null || contextHost.parent == null)
                return;

            VisualElement parent = contextHost.parent;
            Vector2 local = parent.WorldToLocal(panelPosition);
            float width = contextHost.layout.width > 1f ? contextHost.layout.width : 180f;
            float height = contextHost.layout.height > 1f ? contextHost.layout.height : 88f;
            float maxX = Mathf.Max(8f, parent.layout.width - width - 8f);
            float maxY = Mathf.Max(8f, parent.layout.height - height - 8f);
            contextHost.style.left = Mathf.Clamp(local.x, 8f, maxX);
            contextHost.style.top = Mathf.Clamp(local.y, 8f, maxY);
        }

        private void OnRootPointerDown(PointerDownEvent evt)
        {
            if (!IsContextOpen || evt.button == 1)
                return;

            VisualElement target = evt.target as VisualElement;
            if (target != null && contextHost != null && IsDescendantOf(target, contextHost))
                return;

            HideContext();
        }

        private static bool IsDescendantOf(VisualElement target, VisualElement ancestor)
        {
            for (VisualElement walk = target; walk != null; walk = walk.parent)
            {
                if (walk == ancestor)
                    return true;
            }

            return false;
        }

        private void OnContextOverwriteClicked()
        {
            int slotIndex = contextSlotIndex;
            HideContext();
            ShowConfirm(ConfirmKind.Overwrite, slotIndex);
        }

        private void OnContextDeleteClicked()
        {
            int slotIndex = contextSlotIndex;
            HideContext();
            ShowConfirm(ConfirmKind.Delete, slotIndex);
        }

        private void ShowConfirm(ConfirmKind kind, int slotIndex)
        {
            if (kind == ConfirmKind.None || slotIndex < 0)
                return;

            confirmKind = kind;
            confirmSlotIndex = slotIndex;
            string slotName = $"Slot {slotIndex + 1}";

            if (kind == ConfirmKind.Overwrite)
            {
                if (confirmTitle != null)
                    confirmTitle.text = "Overwrite save?";
                if (confirmBody != null)
                    confirmBody.text = $"{slotName} already has a save. Replace it with your current game?";
                if (confirmOk != null)
                    confirmOk.text = "Overwrite";
            }
            else
            {
                if (confirmTitle != null)
                    confirmTitle.text = "Delete save?";
                if (confirmBody != null)
                    confirmBody.text = $"Delete {slotName}? This cannot be undone.";
                if (confirmOk != null)
                    confirmOk.text = "Delete";
            }

            if (confirmOk != null)
                confirmOk.EnableInClassList("dmg-menu-modal-btn--danger", kind == ConfirmKind.Delete);

            if (confirmHost != null)
                confirmHost.BringToFront();
            DMUiToolkitOverlayDocument.SetShown(confirmHost, true);
        }

        private void OnConfirmOkClicked()
        {
            ConfirmKind kind = confirmKind;
            int slotIndex = confirmSlotIndex;
            HideConfirm();

            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            if (menu == null)
                return;

            if (kind == ConfirmKind.Overwrite)
            {
                menu.SaveToSlot(slotIndex);
                return;
            }

            if (kind == ConfirmKind.Delete)
                menu.DeleteSaveSlot(slotIndex, out _);
        }

        private void OnConfirmCancelClicked() => HideConfirm();

        private void OnConfirmVeilPointerDown(PointerDownEvent evt)
        {
            evt.StopPropagation();
            HideConfirm();
        }

        private bool HideContext()
        {
            bool wasOpen = IsContextOpen;
            contextSlotIndex = -1;
            if (contextHost != null)
                DMUiToolkitOverlayDocument.SetShown(contextHost, false);
            return wasOpen;
        }

        private bool HideConfirm()
        {
            bool wasOpen = IsConfirmOpen;
            confirmKind = ConfirmKind.None;
            confirmSlotIndex = -1;
            if (confirmHost != null)
                DMUiToolkitOverlayDocument.SetShown(confirmHost, false);
            return wasOpen;
        }

        private bool IsContextOpen =>
            contextHost != null && contextHost.resolvedStyle.display != DisplayStyle.None;

        private bool IsConfirmOpen =>
            confirmHost != null && confirmHost.resolvedStyle.display != DisplayStyle.None;

        private void OnBackClicked() => HandleBack();

        public static IEnumerator OpenSaveWithScreenshot()
        {
            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            menu?.ClearPendingSaveScreenshot();
            yield return new WaitForEndOfFrame();
        }
    }
}

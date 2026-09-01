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

        private static DMUiToolkitSaveSlots instance;

        private UIDocument document;
        private VisualElement root;
        private Label titleLabel;
        private ScrollView body;
        private readonly List<Button> slotButtons = new List<Button>();
        private readonly List<VisualElement> slotPreviews = new List<VisualElement>();
        private SaveSlotsPanelController.Mode currentMode;
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

        public static bool HandleBack()
        {
            if (!IsOpen)
                return false;

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
            body = tree.Q<ScrollView>("saveslots-body");
            Button backButton = tree.Q<Button>("saveslots-back");

            if (backButton != null)
            {
                backButton.clicked -= OnBackClicked;
                backButton.clicked += OnBackClicked;
            }

            if (body != null && slotButtons.Count == 0)
                BuildSlotRows();

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
                    button.text = occupied
                        ? $"Slot {i + 1}\n{info.GetSummaryLine()}"
                        : $"Slot {i + 1}\nEmpty";
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
            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            if (menu == null)
                return;

            if (currentMode == SaveSlotsPanelController.Mode.Save)
                menu.SaveToSlot(slotIndex);
            else
                menu.LoadFromSlot(slotIndex);

            Close();
        }

        private void OnBackClicked() => HandleBack();

        public static IEnumerator OpenSaveWithScreenshot()
        {
            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            menu?.ClearPendingSaveScreenshot();
            yield return new WaitForEndOfFrame();
            // Screenshot capture stays on MainMenuController coroutine — call from there after hide menu.
        }
    }
}

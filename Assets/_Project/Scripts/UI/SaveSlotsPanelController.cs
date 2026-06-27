using System.Collections.Generic;
using Project.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public class SaveSlotsPanelController : MonoBehaviour
    {
        public enum Mode
        {
            Save,
            Load
        }

        private static readonly Color EmptyPreviewColor = new Color(0.12f, 0.12f, 0.14f, 1f);

        private float PreviewWidth => MenuUiBuilder.ScaledSize(SaveSlotScreenshotUtility.ThumbnailWidth);
        private float PreviewHeight => MenuUiBuilder.ScaledSize(SaveSlotScreenshotUtility.ThumbnailHeight);

        private GameObject panelRoot;
        private TextMeshProUGUI titleLabel;
        private Button[] slotButtons;
        private TextMeshProUGUI[] slotLabels;
        private Image[] slotPreviewImages;
        private MainMenuController menuController;
        private Mode currentMode;
        private readonly List<Object> previewAssets = new List<Object>();

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public void Build(Transform parent, MainMenuController menu)
        {
            if (panelRoot != null)
                return;

            menuController = menu;

            panelRoot = MenuUiBuilder.CreateFullScreenPanel(parent, "SaveSlotsPanel", new Color(0f, 0f, 0f, 0.92f));

            GameObject window = new GameObject("SaveSlotsWindow", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            window.transform.SetParent(panelRoot.transform, false);

            Image windowImage = window.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(windowImage);
            windowImage.color = new Color(0.08f, 0.08f, 0.1f, 0.98f);

            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = Vector2.zero;
            windowRect.anchorMax = Vector2.one;
            windowRect.offsetMin = Vector2.zero;
            windowRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup windowLayout = window.GetComponent<VerticalLayoutGroup>();
            windowLayout.padding = new RectOffset(
                MenuUiBuilder.ScaledSizeInt(24f),
                MenuUiBuilder.ScaledSizeInt(24f),
                MenuUiBuilder.ScaledSizeInt(24f),
                MenuUiBuilder.ScaledSizeInt(24f));
            windowLayout.spacing = MenuUiBuilder.ScaledSizeInt(12f);
            windowLayout.childAlignment = TextAnchor.UpperCenter;
            windowLayout.childControlWidth = true;
            windowLayout.childForceExpandWidth = true;
            windowLayout.childForceExpandHeight = false;

            titleLabel = MenuUiBuilder.CreateTitle(window.transform, "Select Save Slot", MenuUiBuilder.ScaledSize(34f));
            titleLabel.alignment = TextAlignmentOptions.Center;

            slotButtons = new Button[GameSaveSystem.SlotCount];
            slotLabels = new TextMeshProUGUI[GameSaveSystem.SlotCount];
            slotPreviewImages = new Image[GameSaveSystem.SlotCount];

            for (int i = 0; i < GameSaveSystem.SlotCount; i++)
                CreateSlotRow(window.transform, i);

            Button backButton = MenuUiBuilder.CreateButton(
                window.transform,
                "Back",
                new Vector2(MenuUiBuilder.ScaledSize(200f), MenuUiBuilder.ScaledSize(48f)),
                MenuUiBuilder.ScaledSize(22f));
            backButton.onClick.AddListener(OnBackClicked);

            panelRoot.SetActive(false);
        }

        public void Open(Mode mode)
        {
            if (panelRoot == null)
                return;

            currentMode = mode;
            titleLabel.text = mode == Mode.Save ? "Save Game — Select Slot" : "Load Game — Select Slot";
            RefreshSlotButtons();
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void OnBackClicked()
        {
            menuController?.ClearPendingSaveScreenshot();
            Close();
        }

        private void CreateSlotRow(Transform parent, int slotIndex)
        {
            GameObject row = new GameObject($"Slot{slotIndex + 1}Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);

            HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = MenuUiBuilder.ScaledSizeInt(12f);
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            LayoutElement rowLayoutElement = row.AddComponent<LayoutElement>();
            rowLayoutElement.minHeight = PreviewHeight + MenuUiBuilder.ScaledSize(8f);
            rowLayoutElement.preferredHeight = PreviewHeight + MenuUiBuilder.ScaledSize(8f);

            GameObject previewFrame = new GameObject("PreviewFrame", typeof(RectTransform), typeof(Image));
            previewFrame.transform.SetParent(row.transform, false);

            Image previewFrameImage = previewFrame.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(previewFrameImage);
            previewFrameImage.color = EmptyPreviewColor;
            previewFrameImage.raycastTarget = false;

            LayoutElement previewLayout = previewFrame.AddComponent<LayoutElement>();
            previewLayout.minWidth = PreviewWidth;
            previewLayout.preferredWidth = PreviewWidth;
            previewLayout.minHeight = PreviewHeight;
            previewLayout.preferredHeight = PreviewHeight;

            GameObject previewImageObject = new GameObject("PreviewImage", typeof(RectTransform), typeof(Image));
            previewImageObject.transform.SetParent(previewFrame.transform, false);

            Image previewImage = previewImageObject.GetComponent<Image>();
            previewImage.raycastTarget = false;
            previewImage.preserveAspect = true;
            previewImage.color = Color.white;

            RectTransform previewImageRect = previewImageObject.GetComponent<RectTransform>();
            previewImageRect.anchorMin = Vector2.zero;
            previewImageRect.anchorMax = Vector2.one;
            previewImageRect.offsetMin = new Vector2(2f, 2f);
            previewImageRect.offsetMax = new Vector2(-2f, -2f);

            slotPreviewImages[slotIndex] = previewImage;

            Vector2 slotButtonSize = new Vector2(MenuUiBuilder.ScaledSize(560f), PreviewHeight);
            slotButtons[slotIndex] = MenuUiBuilder.CreateButton(
                row.transform,
                $"Slot {slotIndex + 1}",
                slotButtonSize,
                MenuUiBuilder.ScaledSize(20f));
            slotLabels[slotIndex] = slotButtons[slotIndex].GetComponentInChildren<TextMeshProUGUI>();
            if (slotLabels[slotIndex] != null)
            {
                slotLabels[slotIndex].alignment = TextAlignmentOptions.MidlineLeft;
                RectTransform labelRect = slotLabels[slotIndex].GetComponent<RectTransform>();
                labelRect.offsetMin = new Vector2(MenuUiBuilder.ScaledSize(16f), 0f);
            }

            slotButtons[slotIndex].onClick.AddListener(() => OnSlotSelected(slotIndex));
        }

        private void RefreshSlotButtons()
        {
            ClearPreviewAssets();

            for (int i = 0; i < GameSaveSystem.SlotCount; i++)
            {
                SaveSlotInfo info = GameSaveSystem.GetSlotInfo(i);
                bool occupied = info.HasData;

                if (slotLabels[i] != null)
                {
                    if (occupied)
                        slotLabels[i].text = $"Slot {i + 1}\n{info.GetSummaryLine()}";
                    else
                        slotLabels[i].text = $"Slot {i + 1}\nEmpty";
                }

                ApplyPreviewImage(i, info);

                if (slotButtons[i] != null)
                    slotButtons[i].interactable = currentMode == Mode.Save || occupied;
            }
        }

        private void ApplyPreviewImage(int slotIndex, SaveSlotInfo info)
        {
            Image previewImage = slotPreviewImages[slotIndex];
            if (previewImage == null)
                return;

            previewImage.sprite = null;
            previewImage.color = EmptyPreviewColor;

            if (currentMode == Mode.Save && menuController != null && menuController.PendingSaveScreenshot != null)
            {
                SetPreviewFromTexture(previewImage, menuController.PendingSaveScreenshot, cacheAssets: false);
                return;
            }

            if (!info.HasScreenshot)
                return;

            Texture2D texture = SaveSlotScreenshotUtility.LoadScreenshot(slotIndex);
            if (texture == null)
                return;

            SetPreviewFromTexture(previewImage, texture, cacheAssets: true);
        }

        private void SetPreviewFromTexture(Image previewImage, Texture2D texture, bool cacheAssets)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));

            if (cacheAssets)
                previewAssets.Add(texture);

            previewAssets.Add(sprite);
            previewImage.sprite = sprite;
            previewImage.color = Color.white;
        }

        private void ClearPreviewAssets()
        {
            for (int i = 0; i < previewAssets.Count; i++)
            {
                if (previewAssets[i] != null)
                    Destroy(previewAssets[i]);
            }

            previewAssets.Clear();
        }

        private void OnSlotSelected(int slotIndex)
        {
            if (menuController == null)
                return;

            if (currentMode == Mode.Save)
                menuController.SaveToSlot(slotIndex);
            else
                menuController.LoadFromSlot(slotIndex);
        }

        private void OnDestroy()
        {
            ClearPreviewAssets();
        }
    }
}

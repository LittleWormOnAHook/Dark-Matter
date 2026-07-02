using Project.Pet;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// HUD pet slot (3rd toolbar slot): assign an owned pet by drag-drop or journal right-click.
    /// </summary>
    public class PetToolbarUI : MonoBehaviour
    {
        public const float PetClusterGap = 12f * HudLayoutMetrics.HudScale;
        public const float PetSlotOffsetY = 6f * HudLayoutMetrics.HudScale;
        public const float PetSlotLeadingGap = 14f * HudLayoutMetrics.HudScale;

        private static float SlotSize => HudLayoutMetrics.InventorySlotSize(64f);

        private RectTransform petRoot;
        private RectTransform slotRect;
        private Image slotBackgroundImage;
        private Image iconImage;
        private Image selectionGlowImage;
        private TextMeshProUGUI slotLabel;
        private TextMeshProUGUI titleLabel;
        private bool uiBuilt;
        private bool subscribedToPetManager;

        public float GetPetClusterWidth() => SlotSize;

        public float GetPetClusterHeight() => SlotSize + PetSlotOffsetY + HudLayoutMetrics.Scaled(20f);

        public bool IsBuilt => uiBuilt;

        public void AlignLeftOfToolbarCluster(float rightEdgeX, float anchoredY)
        {
            if (petRoot == null)
                return;

            float width = GetPetClusterWidth();
            float height = GetPetClusterHeight();
            petRoot.anchorMin = new Vector2(0.5f, 0f);
            petRoot.anchorMax = new Vector2(0.5f, 0f);
            petRoot.pivot = new Vector2(1f, 0f);
            petRoot.sizeDelta = new Vector2(width, height);
            petRoot.anchoredPosition = new Vector2(rightEdgeX, anchoredY);
        }

        public void EnsureBuilt(Transform canvasRoot, RectTransform toolbarRect, float anchoredY)
        {
            if (uiBuilt || canvasRoot == null)
                return;

            BuildStandaloneCluster(toolbarRect, anchoredY);

            SubscribeToPetManager();

            uiBuilt = true;
            SetGameplayVisible(false);
            Refresh();
        }

        public void EnsureBuiltInToolbarRow(RectTransform slotsRow)
        {
            if (uiBuilt || slotsRow == null)
                return;

            GameObject slotObject = new GameObject("PetSlot", typeof(RectTransform), typeof(Image), typeof(Button), typeof(PetToolbarSlotDropHandler));
            slotObject.transform.SetParent(slotsRow, false);
            slotRect = slotObject.GetComponent<RectTransform>();
            slotRect.sizeDelta = new Vector2(SlotSize, SlotSize);
            slotRect.anchoredPosition = new Vector2(0f, PetSlotOffsetY);

            LayoutElement slotLayout = slotObject.AddComponent<LayoutElement>();
            slotLayout.minWidth = SlotSize;
            slotLayout.preferredWidth = SlotSize;
            slotLayout.minHeight = SlotSize + PetSlotOffsetY;
            slotLayout.preferredHeight = SlotSize + PetSlotOffsetY;

            ConfigureSlotVisuals(slotObject);

            SubscribeToPetManager();

            uiBuilt = true;
            SetGameplayVisible(true);
            Refresh();
        }

        private void SubscribeToPetManager()
        {
            if (subscribedToPetManager || PetManager.Instance == null)
                return;

            PetManager.Instance.OnPetsChanged += Refresh;
            subscribedToPetManager = true;
        }

        public void SetGameplayVisible(bool visible)
        {
            if (slotRect != null)
                slotRect.gameObject.SetActive(visible);
            else if (petRoot != null)
                petRoot.gameObject.SetActive(visible);
        }

        public void RepositionLeftOfToolbar(RectTransform toolbarRect, float anchoredY)
        {
            BuildStandaloneCluster(toolbarRect, anchoredY);
        }

        public void Refresh()
        {
            if (!uiBuilt || slotLabel == null)
                return;

            PetController pet = PetManager.Instance != null ? PetManager.Instance.ToolbarPet : null;
            if (pet == null)
            {
                slotLabel.text = "Empty";
                if (iconImage != null)
                {
                    iconImage.sprite = null;
                    iconImage.enabled = false;
                }

                if (slotBackgroundImage != null)
                    slotBackgroundImage.color = SurvivalPioneerUiPalette.SlotBackground;

                if (selectionGlowImage != null)
                    selectionGlowImage.enabled = false;
                return;
            }

            pet.ApplyDefinition();
            Sprite icon = pet.InventoryIcon;

            slotLabel.text = icon != null ? string.Empty : pet.DisplayName;
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
                iconImage.color = Color.white;
            }

            if (slotBackgroundImage != null)
                slotBackgroundImage.color = SurvivalPioneerUiPalette.SlotBackground;

            if (selectionGlowImage != null)
                selectionGlowImage.enabled = pet.CompanionActive;
        }

        internal void HandlePetDropped(PetController pet)
        {
            if (pet == null || PetManager.Instance == null)
                return;

            PetManager.Instance.TryAssignToolbarPet(pet);
            Refresh();
        }

        internal void HandlePetCleared()
        {
            PetManager.Instance?.ClearToolbarPet();
            Refresh();
        }

        private void ToggleToolbarPetActive()
        {
            PetController pet = PetManager.Instance?.ToolbarPet;
            if (pet == null)
                return;

            pet.CompanionActive = !pet.CompanionActive;
            if (pet.CompanionActive)
                pet.SummonToOwner();

            PetManager.Instance.ApplyToolbarVisibility();
            Refresh();
        }

        private void ConfigureSlotVisuals(GameObject slotObject)
        {
            slotBackgroundImage = slotObject.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(slotBackgroundImage);
            slotBackgroundImage.color = SurvivalPioneerUiPalette.SlotBackground;

            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.EnsureSelectionGlow(slotObject.transform, ref selectionGlowImage);

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform));
            iconObject.transform.SetParent(slotObject.transform, false);
            iconImage = iconObject.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            float inset = HudLayoutMetrics.Scaled(10f);
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(inset, inset);
            iconRect.offsetMax = new Vector2(-inset, -inset);

            Button button = slotObject.GetComponent<Button>();
            SurvivalPioneerUiPalette.StylePrimaryButton(button, slotBackgroundImage);
            button.onClick.AddListener(ToggleToolbarPetActive);

            slotLabel = CreateCenteredLabel(slotObject.transform, "Empty");
            slotObject.GetComponent<PetToolbarSlotDropHandler>().Configure(this);
        }

        private void BuildStandaloneCluster(RectTransform toolbarRect, float anchoredY)
        {
            if (slotRect != null)
                return;

            if (petRoot == null)
            {
                petRoot = new GameObject("PetToolbar", typeof(RectTransform)).GetComponent<RectTransform>();
                petRoot.SetParent(toolbarRect != null ? toolbarRect.parent : transform, false);
            }

            EnsurePetToolbarBackdrop();

            PositionLeftOfToolbar(toolbarRect, anchoredY);

            GameObject titleObject = new GameObject("Title", typeof(RectTransform));
            titleObject.transform.SetParent(petRoot, false);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, HudLayoutMetrics.Scaled(-2f));
            titleRect.sizeDelta = new Vector2(HudLayoutMetrics.Scaled(120f), HudLayoutMetrics.Scaled(20f));

            titleLabel = titleObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(titleLabel);
            titleLabel.text = "PET";
            titleLabel.fontSize = HudLayoutMetrics.ScaledInt(13f);
            titleLabel.fontStyle = FontStyles.Bold;
            titleLabel.alignment = TextAlignmentOptions.Center;
            titleLabel.color = SurvivalPioneerUiPalette.HotbarLabelText;

            GameObject slotObject = new GameObject("PetSlot", typeof(RectTransform), typeof(Image), typeof(Button), typeof(PetToolbarSlotDropHandler));
            slotObject.transform.SetParent(petRoot, false);
            slotRect = slotObject.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0.5f, 0f);
            slotRect.anchorMax = new Vector2(0.5f, 0f);
            slotRect.pivot = new Vector2(0.5f, 0f);
            slotRect.anchoredPosition = new Vector2(0f, HudLayoutMetrics.Scaled(4f) + PetSlotOffsetY);
            slotRect.sizeDelta = new Vector2(SlotSize, SlotSize);

            ConfigureSlotVisuals(slotObject);
        }

        private void PositionLeftOfToolbar(RectTransform toolbarRect, float anchoredY)
        {
            if (petRoot == null || toolbarRect == null)
                return;

            AlignLeftOfToolbarCluster(
                toolbarRect.anchoredPosition.x - PetClusterGap,
                anchoredY);
        }

        private void EnsurePetToolbarBackdrop()
        {
            if (petRoot == null || petRoot.Find("PanelBackdrop") != null)
                return;

            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme == null)
                return;

            GameObject backdrop = new GameObject("PanelBackdrop", typeof(RectTransform));
            backdrop.transform.SetParent(petRoot, false);
            backdrop.transform.SetAsFirstSibling();

            RectTransform backdropRect = backdrop.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = new Vector2(HudLayoutMetrics.Scaled(-8f), HudLayoutMetrics.Scaled(-4f));
            backdropRect.offsetMax = new Vector2(HudLayoutMetrics.Scaled(8f), HudLayoutMetrics.Scaled(4f));

            Image image = backdrop.AddComponent<Image>();
            theme.ApplyPanelImage(image, large: false, alphaMultiplier: 0.92f);
        }

        private static TextMeshProUGUI CreateCenteredLabel(Transform parent, string text)
        {
            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            RectTransform rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(label);
            label.text = text;
            label.fontSize = HudLayoutMetrics.ScaledInt(11f);
            label.alignment = TextAlignmentOptions.Center;
            label.color = SurvivalPioneerUiPalette.WarmOffWhite;
            return label;
        }

        private void OnDestroy()
        {
            if (subscribedToPetManager && PetManager.Instance != null)
                PetManager.Instance.OnPetsChanged -= Refresh;
        }
    }

    internal class PetToolbarSlotDropHandler : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        private PetToolbarUI toolbarUi;

        public void Configure(PetToolbarUI owner)
        {
            toolbarUi = owner;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (toolbarUi == null || PetDragState.Pet == null)
                return;

            toolbarUi.HandlePetDropped(PetDragState.Pet);
            PetDragState.Clear();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right || toolbarUi == null)
                return;

            toolbarUi.HandlePetCleared();
        }
    }

    internal static class PetDragState
    {
        public static PetController Pet;

        public static void Clear()
        {
            Pet = null;
        }
    }
}

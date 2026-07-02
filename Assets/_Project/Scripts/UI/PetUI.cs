using System.Collections.Generic;
using Project.Pet;
using Project.Player;
using Project.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace Project.UI
{
    public class PetUI : MonoBehaviour
    {
        public const int PetInventorySlots = PetSlotUI.InventorySlotCount;

        private const float PanelScale = 1.5f;
        private const float EmbeddedPanelScale = 0.72f;
        private const int GridColumns = 5;

        private static float S(float value, float scale) => value * scale;
        private static int Si(float value, float scale) => Mathf.RoundToInt(value * scale);

        [Header("Panel")]
        [SerializeField] private GameObject petPanel;
        [SerializeField] private Transform petListParent;
        [SerializeField] private TextMeshProUGUI summaryText;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_FontAsset panelFont;

        private readonly List<PetSlotUI> _slots = new List<PetSlotUI>();
        private bool _isBuilt;

        private Transform _petPanelOriginalParent;
        private bool _petPanelEmbedded;
        private VerticalLayoutGroup _panelLayout;
        private LayoutElement _scrollLayoutElement;
        private GameObject _headerRow;
        private float CurrentScale => _petPanelEmbedded ? EmbeddedPanelScale : PanelScale;

        private void Awake()
        {
            EnsurePanelBuilt();
        }

        private void Start()
        {
            if (PetManager.Instance != null)
                PetManager.Instance.OnPetsChanged += RefreshPetList;

            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePanel);

            if (petPanel != null)
                petPanel.SetActive(false);

            RefreshPetList();
        }

        private void OnDestroy()
        {
            if (PetManager.Instance != null)
                PetManager.Instance.OnPetsChanged -= RefreshPetList;
        }

        public void OnTogglePets(InputAction.CallbackContext context)
        {
            if (!GameSession.HasStarted || !context.performed)
                return;

            JournalPanelUI journal = FindAnyObjectByType<JournalPanelUI>();
            if (journal != null)
            {
                journal.TryToggleTab(JournalWindowId.Pet);
                return;
            }

            TogglePanel();
        }

        public static void CloseAnyOpenPet()
        {
            foreach (PetUI petUi in FindObjectsByType<PetUI>(FindObjectsInactive.Include))
            {
                if (petUi != null)
                    petUi.ClosePanel();
            }
        }

        public void TogglePanel()
        {
            if (petPanel == null) return;

            bool open = !petPanel.activeSelf;
            petPanel.SetActive(open);

            if (open)
            {
                petPanel.transform.SetAsLastSibling();
                RefreshPetList();
                if (!_petPanelEmbedded)
                    PauseGameplay(true);
            }
            else
            {
                if (!_petPanelEmbedded)
                    PauseGameplay(false);
            }
        }

        public void ClosePanel()
        {
            if (petPanel == null || !petPanel.activeSelf) return;

            petPanel.SetActive(false);
            if (!_petPanelEmbedded)
                PauseGameplay(false);
        }

        public void EmbedPanel(Transform container)
        {
            EnsurePanelBuilt();
            if (petPanel == null || container == null)
                return;

            EnsureCanvasCamera(container);

            _petPanelOriginalParent = petPanel.transform.parent;
            petPanel.transform.SetParent(container, false);
            StretchToParent(petPanel.GetComponent<RectTransform>());
            ApplyEmbeddedLayout(true);
            petPanel.SetActive(true);
            _petPanelEmbedded = true;
            RefreshPetList();
        }

        public void RestorePanel()
        {
            if (!_petPanelEmbedded || petPanel == null || _petPanelOriginalParent == null)
                return;

            ApplyEmbeddedLayout(false);

            if (!UiEmbedRestore.TryRestoreParent(petPanel.transform, _petPanelOriginalParent))
            {
                _petPanelEmbedded = false;
                return;
            }

            petPanel.SetActive(false);
            _petPanelEmbedded = false;
        }

        private void ApplyEmbeddedLayout(bool embedded)
        {
            if (petPanel == null)
                return;

            RectTransform panelRt = petPanel.GetComponent<RectTransform>();
            if (embedded)
            {
                StretchToParent(panelRt);
                if (_panelLayout != null)
                    _panelLayout.padding = new RectOffset(Si(8f, EmbeddedPanelScale), Si(8f, EmbeddedPanelScale), Si(8f, EmbeddedPanelScale), Si(8f, EmbeddedPanelScale));
            }
            else if (panelRt != null)
            {
                panelRt.anchorMin = new Vector2(0.5f, 0.5f);
                panelRt.anchorMax = new Vector2(0.5f, 0.5f);
                panelRt.pivot = new Vector2(0.5f, 0.5f);
                panelRt.sizeDelta = new Vector2(S(920f, PanelScale), S(620f, PanelScale));
                panelRt.anchoredPosition = Vector2.zero;
                panelRt.localScale = Vector3.one;
                if (_panelLayout != null)
                    _panelLayout.padding = new RectOffset(Si(20f, PanelScale), Si(20f, PanelScale), Si(20f, PanelScale), Si(20f, PanelScale));
            }

            if (_headerRow != null)
                _headerRow.SetActive(!embedded);

            if (_scrollLayoutElement != null)
            {
                _scrollLayoutElement.flexibleHeight = 1f;
                _scrollLayoutElement.minHeight = embedded ? 0f : S(420f, PanelScale);
            }

            Image panelBg = petPanel.GetComponent<Image>();
            if (panelBg != null)
                panelBg.color = embedded ? new Color(0f, 0f, 0f, 0f) : new Color(0f, 0f, 0f, 0.82f);
        }

        private static void StretchToParent(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        public void RefreshPetList()
        {
            if (!_isBuilt || petListParent == null)
                return;

            EnsureSlotGridBuilt();

            IReadOnlyList<PetController> pets = PetManager.Instance != null
                ? PetManager.Instance.GetOwnedPets()
                : System.Array.Empty<PetController>();

            for (int i = 0; i < _slots.Count; i++)
            {
                PetController pet = i < pets.Count ? pets[i] : null;
                _slots[i].Bind(pet);
            }

            if (summaryText != null)
            {
                PetController toolbarPet = PetManager.Instance != null ? PetManager.Instance.ToolbarPet : null;
                string activeName = toolbarPet != null ? toolbarPet.DisplayName : "none";
                summaryText.text = pets.Count == 0
                    ? "No pets befriended yet. Press E to Adopt near a wild pet."
                    : $"{pets.Count}/{PetInventorySlots} owned  ·  Active: {activeName}  ·  Right-click slot to assign";
            }
        }

        private void EnsureSlotGridBuilt()
        {
            if (_slots.Count == PetInventorySlots)
                return;

            foreach (PetSlotUI slot in _slots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            _slots.Clear();

            float slotScale = _petPanelEmbedded ? EmbeddedPanelScale : PanelScale;
            for (int i = 0; i < PetInventorySlots; i++)
            {
                GameObject slotObj = new GameObject($"PetSlot_{i + 1}", typeof(RectTransform));
                slotObj.transform.SetParent(petListParent, false);

                PetSlotUI slot = slotObj.AddComponent<PetSlotUI>();
                slot.Build(slotScale);
                slot.Bind(null);
                _slots.Add(slot);
            }
        }

        private static void EnsureCanvasCamera(Transform container)
        {
            Canvas canvas = container.GetComponentInParent<Canvas>();
            if (canvas != null
                && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                && canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }
        }

        private void EnsurePanelBuilt()
        {
            if (_isBuilt && petPanel != null && petListParent != null)
                return;

            ResolvePanelFont();
            BuildPanel();
            _isBuilt = true;
        }

        private void ResolvePanelFont()
        {
            panelFont = TmpUiHelper.FallbackFont;
        }

        private void ApplyFont(TextMeshProUGUI text)
        {
            TmpUiHelper.ApplyDefaultFont(text);
        }

        private void BuildPanel()
        {
            petPanel = new GameObject("PetPanel", typeof(RectTransform));
            petPanel.transform.SetParent(transform, false);

            RectTransform panelRt = petPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(S(920f, PanelScale), S(620f, PanelScale));
            panelRt.anchoredPosition = Vector2.zero;

            Image panelBg = petPanel.AddComponent<Image>();
            panelBg.color = new Color(0f, 0f, 0f, 0.82f);

            _panelLayout = petPanel.AddComponent<VerticalLayoutGroup>();
            _panelLayout.padding = new RectOffset(Si(20f, PanelScale), Si(20f, PanelScale), Si(20f, PanelScale), Si(20f, PanelScale));
            _panelLayout.spacing = Si(12f, PanelScale);
            _panelLayout.childAlignment = TextAnchor.UpperCenter;
            _panelLayout.childControlWidth = true;
            _panelLayout.childControlHeight = true;
            _panelLayout.childForceExpandWidth = true;
            _panelLayout.childForceExpandHeight = false;

            CreateHeaderRow();
            CreateSlotGrid();
            CreateFooterRow();
            petPanel.SetActive(false);
        }

        public void HideForStartScreen()
        {
            EnsurePanelBuilt();
            if (petPanel != null)
                petPanel.SetActive(false);
        }

        private void CreateHeaderRow()
        {
            _headerRow = new GameObject("Header", typeof(RectTransform));
            _headerRow.transform.SetParent(petPanel.transform, false);

            HorizontalLayoutGroup layout = _headerRow.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            GameObject titleObj = new GameObject("Title", typeof(RectTransform));
            titleObj.transform.SetParent(_headerRow.transform, false);
            TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
            ApplyFont(title);
            title.text = "Pet Journal";
            title.fontSize = S(34f, PanelScale);
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;
            title.alignment = TextAlignmentOptions.MidlineLeft;

            LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1;

            closeButton = MenuUiBuilder.CreateCircleCloseButton(_headerRow.transform, S(42f, PanelScale));
            LayoutElement closeLayout = closeButton.gameObject.AddComponent<LayoutElement>();
            closeLayout.minWidth = S(42f, PanelScale);
            closeLayout.preferredWidth = S(42f, PanelScale);
            closeLayout.minHeight = S(42f, PanelScale);
            closeLayout.preferredHeight = S(42f, PanelScale);
        }

        private void CreateSlotGrid()
        {
            GameObject gridObj = new GameObject("PetSlotGrid", typeof(RectTransform));
            gridObj.transform.SetParent(petPanel.transform, false);

            _scrollLayoutElement = gridObj.AddComponent<LayoutElement>();
            _scrollLayoutElement.flexibleHeight = 1;
            _scrollLayoutElement.minHeight = S(220f, PanelScale);

            Image gridBg = gridObj.AddComponent<Image>();
            gridBg.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.PanelBackground, 0.55f);

            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            float slotSize = S(72f, PanelScale);
            grid.cellSize = new Vector2(slotSize, slotSize);
            grid.spacing = new Vector2(S(10f, PanelScale), S(10f, PanelScale));
            grid.padding = new RectOffset(Si(12f, PanelScale), Si(12f, PanelScale), Si(12f, PanelScale), Si(12f, PanelScale));
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = GridColumns;
            grid.childAlignment = TextAnchor.UpperCenter;

            petListParent = gridObj.transform;
        }

        private void CreateFooterRow()
        {
            GameObject footer = new GameObject("Footer", typeof(RectTransform));
            footer.transform.SetParent(petPanel.transform, false);

            summaryText = footer.AddComponent<TextMeshProUGUI>();
            ApplyFont(summaryText);
            summaryText.text = "Right-click a pet slot to assign your active companion.";
            summaryText.fontSize = S(16f, PanelScale);
            summaryText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            summaryText.alignment = TextAlignmentOptions.MidlineLeft;

            LayoutElement footerLayout = footer.AddComponent<LayoutElement>();
            footerLayout.minHeight = S(28f, PanelScale);
        }

        private static void PauseGameplay(bool pause)
        {
            Cursor.lockState = pause ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = pause;

            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                player.SetInventoryOpen(pause);

            CameraController camera = FindAnyObjectByType<CameraController>();
            if (camera != null)
                camera.SetInventoryOpen(pause);
        }
    }
}

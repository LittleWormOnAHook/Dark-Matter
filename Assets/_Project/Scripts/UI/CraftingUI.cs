using System.Collections;
using System.Collections.Generic;
using Project.Core;
using Project.Crafting;
using Project.Data;
using Project.Inventory;
using Project.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Library = Journal Blueprints (pending scrolls + learned catalog, no production craft).
    /// Production = station/campfire/building craft popup (ingredients + Craft actions).
    /// </summary>
    public enum CraftingUiPresentationMode
    {
        Library = 0,
        Production = 1
    }

    public class CraftingUI : MonoBehaviour
    {
        // Base 0.85 layout, then +25% for station popup readability.
        private const float PanelScale = 0.85f * 1.25f;
        private const int RecipeGridColumns = 5;
        private const float StandaloneWindowWidth = 720f;
        private const float StandaloneWindowHeight = 700f;
        private const int CraftingUiChromeVersion = 4;
        private int builtChromeVersion;
        private int standaloneChromeVersion;
        /// <summary>Journal craft slots — larger than HUD inventory cells so learned blueprints read clearly.</summary>
        private static float RecipeSlotSize => HudLayoutMetrics.InventorySlotSize(96f);

        private static float S(float value) => value * PanelScale;
        private static int Si(float value) => Mathf.RoundToInt(value * PanelScale);

        private GameObject craftPanel;
        private VerticalLayoutGroup panelLayout;
        private LayoutElement recipeScrollLayoutElement;
        private GameObject recipeScrollObject;
        private ScrollRect recipeListScrollRect;
        private Image panelBackground;
        private GameObject headerObject;
        private TextMeshProUGUI headerLabel;
        private Image recipeScrollBackground;
        private Transform recipeScrollSlotsParent;
        private TextMeshProUGUI scrollSectionLabel;
        private TextMeshProUGUI scrollHintText;
        private Transform recipeListParent;
        private RectTransform recipeListContentRect;
        private Transform emptyStateHost;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI learnedSectionLabel;

        private GameObject craftDetailPanel;
        private TextMeshProUGUI craftDetailTitle;
        private TextMeshProUGUI craftDetailMeta;
        private TextMeshProUGUI craftDetailIngredients;
        private TextMeshProUGUI craftAmountValueLabel;
        private Slider craftAmountSlider;
        private Button craftConfirmButton;
        private TextMeshProUGUI craftConfirmLabel;

        private RecipeDefinition selectedRecipe;
        private RecipeCraftSlotUI selectedSlotUi;
        private Coroutine craftRoutine;
        private bool isCrafting;

        private readonly List<RecipeCraftSlotUI> recipeCraftSlots = new List<RecipeCraftSlotUI>();
        private readonly List<RecipeScrollSlotUI> scrollSlots = new List<RecipeScrollSlotUI>();
        private bool isBuilt;

        private Transform craftPanelOriginalParent;
        private bool craftPanelEmbedded;

        private GameObject standaloneWindowRoot;
        private RectTransform standaloneWindowRect;
        private Transform standaloneContentParent;
        private bool standaloneOpen;
        private bool standaloneInputCaptured;
        private bool standaloneOwnedTimePause;
        private TextMeshProUGUI standaloneTitleLabel;

        private CraftingManager craftingManager;
        private InventorySystem inventorySystem;
        private CraftingUiPresentationMode presentationMode = CraftingUiPresentationMode.Library;
        private bool recipeRefreshQueued;

        private void Awake()
        {
            EnsurePanelBuilt();
        }

        private void Start()
        {
            BindSystems();
            EnsureRecipeTooltip();
            if (craftPanel != null)
                craftPanel.SetActive(false);
            RefreshRecipeList();
        }

        private void EnsureRecipeTooltip()
        {
            Canvas canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>();
            if (canvas != null)
                RecipeHoverTooltip.EnsureExists(canvas.transform);
        }

        private void OnDestroy()
        {
            CancelActiveCraft();
            ReleaseStandaloneInput();
            UnbindSystems();
        }

        public bool IsStandaloneOpen => standaloneOpen;
        public CraftingUiPresentationMode PresentationMode => presentationMode;

        public static bool IsAnyStandaloneOpen
        {
            get
            {
                if (DMUiToolkitCraft.IsOpen)
                    return true;

                CraftingUI ui = FindAnyObjectByType<CraftingUI>();
                return ui != null && ui.standaloneOpen;
            }
        }

        public static void CloseAnyOpenStandalone()
        {
            DMUiToolkitCraft.Hide();
            CraftingUI ui = FindAnyObjectByType<CraftingUI>();
            ui?.CloseStandalonePanel(clearStation: true);
        }

        public bool ToolkitIsCrafting => isCrafting;
        public CraftingManager ToolkitCraftingManager
        {
            get
            {
                BindSystems();
                return craftingManager;
            }
        }

        public InventorySystem ToolkitInventory
        {
            get
            {
                BindSystems();
                return inventorySystem;
            }
        }

        public System.Collections.Generic.IReadOnlyList<RecipeDefinition> ToolkitGetRecipes()
        {
            BindSystems();
            if (craftingManager == null)
                return System.Array.Empty<RecipeDefinition>();

            return !craftingManager.CurrentStation.HasValue
                ? craftingManager.GetDiscoveredRecipes()
                : craftingManager.GetDiscoveredRecipes(craftingManager.CurrentStation);
        }

        public void ToolkitHideUguiShell()
        {
            if (standaloneWindowRoot != null && standaloneWindowRoot.activeSelf)
                standaloneWindowRoot.SetActive(false);

            if (craftPanel != null && !craftPanelEmbedded && craftPanel.activeSelf)
                craftPanel.SetActive(false);
        }

        public bool ToolkitTryCraft(RecipeDefinition recipe, int amount)
        {
            BindSystems();
            if (isCrafting || recipe == null || craftingManager == null || inventorySystem == null)
                return false;

            selectedRecipe = recipe;
            amount = Mathf.Max(1, amount);
            if (!craftingManager.CanCraft(recipe, inventorySystem, amount))
            {
                PickupToastUI.Show("Cannot craft - check ingredients, station, or inventory space.");
                return false;
            }

            if (craftRoutine != null)
                StopCoroutine(craftRoutine);
            craftRoutine = StartCoroutine(RunCraftRoutine(recipe, amount));
            return true;
        }

        public void SetPresentationMode(CraftingUiPresentationMode mode)
        {
            presentationMode = mode;
            ApplyPresentationChrome();
            RefreshCraftDetailPanel();
        }

        /// <summary>
        /// Opens the production craft popup at a cooking pot / workbench / campfire.
        /// Journal Blueprints stays library-only; this is the only path for crafting items.
        /// </summary>
        public void OpenStationCraftingPopup(CraftingStationType stationType)
        {
            EnsurePanelBuilt();
            BindSystems();

            if (craftingManager == null)
            {
                PickupToastUI.Show("Crafting is unavailable in this scene.");
                return;
            }

            craftingManager.CurrentStation = stationType;
            SetPresentationMode(CraftingUiPresentationMode.Production);

            if (DMUiToolkitHud.IsDriving && DMUiToolkitCraft.TryShow(this))
            {
                standaloneOpen = true;
                CaptureStandaloneInput();
                ToolkitHideUguiShell();
                return;
            }

            Canvas canvas = GetComponent<Canvas>()
                ?? GetComponentInParent<Canvas>()
                ?? FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return;

            EnsureStandaloneWindow(canvas.transform);
            ApplyStandaloneWindowSize();

            if (craftPanelEmbedded)
                RestorePanel();

            craftPanel.transform.SetParent(standaloneContentParent, false);
            StretchToParent(craftPanel.GetComponent<RectTransform>());
            craftPanel.SetActive(true);
            craftPanelEmbedded = false;
            standaloneOpen = true;
            ApplyEmbeddedAppearance(false);
            ApplyPresentationChrome();

            if (standaloneWindowRect != null)
                standaloneWindowRect.anchoredPosition = Vector2.zero;

            standaloneWindowRoot.SetActive(true);
            CaptureStandaloneInput();
            RefreshRecipeList();
            standaloneWindowRoot.transform.SetAsLastSibling();
            UiFrontLayer.BringLayerToFront(canvas.transform);
        }

        public void OpenStandalonePanel(Transform overlayParent, RectTransform journalPanel, float gap)
        {
            EnsurePanelBuilt();
            SetPresentationMode(CraftingUiPresentationMode.Production);

            if (DMUiToolkitHud.IsDriving && DMUiToolkitCraft.TryShow(this))
            {
                standaloneOpen = true;
                CaptureStandaloneInput();
                BindSystems();
                ToolkitHideUguiShell();
                return;
            }
            EnsureStandaloneWindow(overlayParent);
            ApplyStandaloneWindowSize();

            if (standaloneOpen && craftPanel != null && craftPanel.transform.parent == standaloneContentParent)
            {
                PositionBesideJournal(journalPanel, gap);
                BindSystems();
                CaptureStandaloneInput();
                RefreshRecipeList();
                standaloneWindowRoot.transform.SetAsLastSibling();
                return;
            }

            if (craftPanelEmbedded)
                RestorePanel();

            craftPanel.transform.SetParent(standaloneContentParent, false);
            StretchToParent(craftPanel.GetComponent<RectTransform>());
            craftPanel.SetActive(true);
            craftPanelEmbedded = false;
            standaloneOpen = true;
            ApplyEmbeddedAppearance(true);
            ApplyPresentationChrome();

            standaloneWindowRoot.SetActive(true);
            CaptureStandaloneInput();
            BindSystems();
            RefreshRecipeList();
            PositionBesideJournal(journalPanel, gap);
            standaloneWindowRoot.transform.SetAsLastSibling();
        }

        public void CloseStandalonePanel(bool clearStation = true)
        {
            DMUiToolkitCraft.Hide();

            if (!standaloneOpen && !craftPanelEmbedded)
                return;

            CancelActiveCraft();
            ClearRecipeSelection();
            HideStandaloneWindowShell();

            if (craftPanel == null)
                return;

            if (craftPanelEmbedded)
                return;

            craftPanel.SetActive(false);
            if (!UiEmbedRestore.TryRestoreParent(craftPanel.transform, transform))
                craftPanel.transform.SetParent(transform, false);

            ApplyEmbeddedAppearance(false);

            if (!clearStation)
                return;

            if (craftingManager == null)
                craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();
            if (craftingManager != null)
                craftingManager.CurrentStation = null;
        }

        private void Update()
        {
            if (!standaloneOpen)
                return;

            if (UiEscapeGate.TryConsumeEscape())
                CloseStandalonePanel(clearStation: true);
        }

        public void PositionBesideJournal(RectTransform journalPanel, float gap)
        {
            if (journalPanel == null || standaloneWindowRect == null)
                return;

            float journalHalfWidth = journalPanel.sizeDelta.x * 0.5f;
            float craftHalfWidth = standaloneWindowRect.sizeDelta.x * 0.5f;
            Vector2 journalPos = journalPanel.anchoredPosition;
            standaloneWindowRect.anchoredPosition = journalPos + new Vector2(journalHalfWidth + gap + craftHalfWidth, 0f);
        }

        public void EmbedPanel(Transform container)
        {
            EmbedPanel(container, CraftingUiPresentationMode.Production);
        }

        public void EmbedLibraryPanel(Transform container)
        {
            EmbedPanel(container, CraftingUiPresentationMode.Library);
        }

        public void EmbedPanel(Transform container, CraftingUiPresentationMode mode)
        {
            EnsurePanelBuilt();
            if (craftPanel == null || container == null)
                return;

            HideStandaloneWindowShell();
            SetPresentationMode(mode);

            BindSystems();
            craftPanelOriginalParent = transform;
            craftPanel.transform.SetParent(container, false);
            StretchToParent(craftPanel.GetComponent<RectTransform>());
            craftPanel.SetActive(true);
            craftPanelEmbedded = true;
            ApplyEmbeddedAppearance(true);
            ApplyPresentationChrome();
            RefreshRecipeList();
        }

        public void HideStandaloneWindowShell()
        {
            if (standaloneWindowRoot != null)
                standaloneWindowRoot.SetActive(false);

            standaloneOpen = false;
            ReleaseStandaloneInput();
        }

        public void RestorePanel()
        {
            if (!craftPanelEmbedded || craftPanel == null || craftPanelOriginalParent == null)
                return;

            if (!UiEmbedRestore.TryRestoreParent(craftPanel.transform, craftPanelOriginalParent))
            {
                craftPanelEmbedded = false;
                return;
            }

            craftPanel.SetActive(false);
            craftPanelEmbedded = false;
            ApplyEmbeddedAppearance(false);
        }

        private void CaptureStandaloneInput()
        {
            if (standaloneInputCaptured)
                return;

            standaloneInputCaptured = true;

            // Slow the world while the station craft popup is up (mouse UI needs a free cursor).
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonCraftingStation, true);
            standaloneOwnedTimePause = true;

            PlayerController player = FindAnyObjectByType<PlayerController>();
            player?.SetGameplayPaused(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            GameplayHudVisibility.SetModalOverlayOpen(true);
        }

        private void ReleaseStandaloneInput()
        {
            if (!standaloneInputCaptured)
                return;

            standaloneInputCaptured = false;

            if (standaloneOwnedTimePause)
            {
                GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonCraftingStation, false);
                standaloneOwnedTimePause = false;
            }

            PlayerController player = FindAnyObjectByType<PlayerController>();
            player?.SetGameplayPaused(false);

            GameplayHudVisibility.SetModalOverlayOpen(false);
            GameplayInputRecovery.FinalizeGameplayInput();
        }

        private void ApplyStandaloneWindowSize()
        {
            if (standaloneWindowRect == null)
                return;

            standaloneWindowRect.sizeDelta = new Vector2(S(StandaloneWindowWidth), S(StandaloneWindowHeight));
        }

        /// <summary>
        /// Defers recipe refresh one frame so physics trigger callbacks never tear down UI with DestroyImmediate.
        /// </summary>
        public void RequestRefreshRecipeList()
        {
            if (!isActiveAndEnabled)
            {
                RefreshRecipeList();
                return;
            }

            if (recipeRefreshQueued)
                return;

            recipeRefreshQueued = true;
            StartCoroutine(RefreshRecipeListNextFrame());
        }

        private IEnumerator RefreshRecipeListNextFrame()
        {
            yield return null;
            recipeRefreshQueued = false;
            RefreshRecipeList();
        }

        public void RefreshRecipeList()
        {
            if (!isBuilt || recipeListParent == null)
                return;

            BindSystems();
            RefreshScrollSlots();

            foreach (RecipeCraftSlotUI slot in recipeCraftSlots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            recipeCraftSlots.Clear();

            if (craftingManager == null)
            {
                if (statusText != null)
                    statusText.text = string.Empty;
                if (recipeScrollObject != null)
                    recipeScrollObject.SetActive(false);
                ShowEmptyState(
                    "Crafting offline",
                    "The crafting manager is not present in this scene.",
                    "Ensure a CraftingManager exists in Pioneer.");
                return;
            }

            bool libraryMode = presentationMode == CraftingUiPresentationMode.Library;
            // Production panel only lists blueprints for the active station (Cooking vs Workbench).
            IReadOnlyList<RecipeDefinition> recipes = libraryMode || !craftingManager.CurrentStation.HasValue
                ? craftingManager.GetDiscoveredRecipes()
                : craftingManager.GetDiscoveredRecipes(craftingManager.CurrentStation);
            int pendingScrolls = craftingManager.GetPendingBlueprintScrolls().Count;
            int totalLearned = craftingManager.GetDiscoveredRecipes().Count;

            if (statusText != null)
            {
                if (libraryMode)
                {
                    if (recipes.Count == 0 && pendingScrolls == 0)
                        statusText.text = string.Empty;
                    else if (pendingScrolls > 0)
                    {
                        statusText.text =
                            $"{JournalPanelLayout.FormatGoldValue(pendingScrolls.ToString())} pending blueprint(s)  ·  " +
                            $"{recipes.Count} learned  ·  Craft at a cooking pot or workbench.";
                        statusText.color = DarkMatterGenesisUiPalette.Gold;
                    }
                    else
                    {
                        statusText.text =
                            $"{JournalPanelLayout.FormatGoldValue(recipes.Count.ToString())} learned  ·  " +
                            "Visit a cooking pot or workbench to craft.";
                        statusText.color = DarkMatterGenesisUiPalette.Gold;
                    }
                }
                else if (recipes.Count == 0)
                {
                    statusText.text = string.Empty;
                }
                else if (!craftingManager.CurrentStation.HasValue)
                {
                    statusText.text =
                        $"{JournalPanelLayout.FormatGoldValue(totalLearned.ToString())} learned  ·  " +
                        (pendingScrolls > 0
                            ? $"{pendingScrolls} pending blueprint(s)  ·  Approach a station to craft."
                            : "Approach a cooking pot or workbench to craft.");
                    statusText.color = DarkMatterGenesisUiPalette.Gold;
                }
                else
                {
                    CraftingStationType station = craftingManager.CurrentStation.Value;
                    string stationLabel = station == CraftingStationType.Cooking ? "Cooking" : "Workbench";
                    int readyNow = 0;
                    if (inventorySystem != null)
                    {
                        for (int i = 0; i < recipes.Count; i++)
                        {
                            if (recipes[i] != null && craftingManager.CanCraft(recipes[i], inventorySystem, 1))
                                readyNow++;
                        }
                    }

                    string baseStatus = recipes.Count > 0
                        ? $"{stationLabel} station  ·  {JournalPanelLayout.FormatGoldValue(readyNow.ToString())} ready  ·  {recipes.Count} for this station"
                        : $"{stationLabel} station  ·  no learned blueprints for this station";
                    if (pendingScrolls > 0)
                        baseStatus += $"  ·  {JournalPanelLayout.FormatGoldValue(pendingScrolls.ToString())} pending blueprint(s)";
                    statusText.text = baseStatus;
                    statusText.color = DarkMatterGenesisUiPalette.Gold;
                }
            }

            if (learnedSectionLabel != null)
            {
                string sectionTitle = libraryMode ? "Learned Blueprints" : "Station Blueprints";
                learnedSectionLabel.text = recipes.Count > 0
                    ? $"{sectionTitle}  ·  {JournalPanelLayout.FormatGoldValue(recipes.Count.ToString())}"
                    : sectionTitle;
                learnedSectionLabel.color = DarkMatterGenesisUiPalette.WarmOffWhite;
            }

            if (scrollSectionLabel != null)
            {
                scrollSectionLabel.text = pendingScrolls > 0
                    ? $"Pending Blueprints  ·  {JournalPanelLayout.FormatGoldValue(pendingScrolls.ToString())}"
                    : "Pending Blueprints";
                scrollSectionLabel.color = DarkMatterGenesisUiPalette.WarmOffWhite;
            }

            if (recipes.Count == 0)
            {
                if (recipeScrollObject != null)
                    recipeScrollObject.SetActive(false);

                ShowEmptyState(
                    libraryMode ? "No crafts unlocked" : "Nothing for this station",
                    libraryMode
                        ? (pendingScrolls > 0
                            ? "You have unread blueprint scrolls waiting above."
                            : "Collect one-time blueprint scrolls in the world to unlock recipes.")
                        : (pendingScrolls > 0
                            ? "No learned blueprints match this station yet."
                            : "Learn blueprints that use this station, then return to craft."),
                    libraryMode
                        ? (pendingScrolls > 0
                            ? "Right-click a scroll → Learn, then craft at a station."
                            : "Cooking pots and workbenches unlock once you learn matching blueprints.")
                        : "Cooking pots craft food; workbenches craft gear and modules.");
                return;
            }

            ClearEmptyState();
            if (recipeScrollObject != null)
                recipeScrollObject.SetActive(true);

            foreach (RecipeDefinition recipe in recipes)
                CreateRecipeSlot(recipe);

            // Keep selection if the recipe is still in the list; otherwise clear.
            if (selectedRecipe != null)
            {
                RecipeCraftSlotUI match = null;
                for (int i = 0; i < recipeCraftSlots.Count; i++)
                {
                    if (recipeCraftSlots[i] != null && recipeCraftSlots[i].Recipe == selectedRecipe)
                    {
                        match = recipeCraftSlots[i];
                        break;
                    }
                }

                if (match != null)
                    SelectRecipe(selectedRecipe, match);
                else
                    ClearRecipeSelection();
            }
            else
            {
                RefreshCraftDetailPanel();
            }

            if (recipeListContentRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(recipeListContentRect);

            if (recipeListScrollRect != null)
            {
                recipeListScrollRect.verticalNormalizedPosition = 1f;
                recipeListScrollRect.velocity = Vector2.zero;
            }
        }

        private void ShowEmptyState(string title, string body, string tip)
        {
            ClearEmptyState();
            if (emptyStateHost == null)
                return;

            emptyStateHost.gameObject.SetActive(true);
            JournalPanelLayout.CreateEmptyStateCard(emptyStateHost, ShiftUiTheme.Current, title, body, tip);
        }

        private void ClearEmptyState()
        {
            if (emptyStateHost == null)
                return;

            for (int i = emptyStateHost.childCount - 1; i >= 0; i--)
            {
                Transform child = emptyStateHost.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }

            emptyStateHost.gameObject.SetActive(false);
        }

        private void RefreshScrollSlots()
        {
            if (recipeScrollSlotsParent == null)
                return;

            foreach (RecipeScrollSlotUI slot in scrollSlots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            scrollSlots.Clear();

            for (int i = recipeScrollSlotsParent.childCount - 1; i >= 0; i--)
            {
                Transform child = recipeScrollSlotsParent.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }

            if (craftingManager == null)
            {
                if (scrollHintText != null)
                {
                    scrollHintText.text = "Collect blueprints in the world to fill these slots.";
                    scrollHintText.color = DarkMatterGenesisUiPalette.Gold;
                }
                return;
            }

            IReadOnlyList<string> pending = craftingManager.GetPendingBlueprintScrolls();
            if (scrollHintText != null)
            {
                scrollHintText.text = pending.Count > 0
                    ? "Right-click a scroll, then click Learn to confirm."
                    : "Collect blueprints in the world to fill these slots.";
                scrollHintText.color = DarkMatterGenesisUiPalette.Gold;
            }

            for (int i = 0; i < pending.Count; i++)
            {
                string id = pending[i];
                RecipeDefinition recipe = RecipeRegistry.Resolve(id);

                GameObject slotObject = new GameObject($"RecipeScrollSlot_{i}", typeof(RectTransform));
                slotObject.transform.SetParent(recipeScrollSlotsParent, false);

                RecipeScrollSlotUI slotUi = slotObject.AddComponent<RecipeScrollSlotUI>();
                int capturedIndex = i;
                slotUi.Setup(capturedIndex, id, recipe, HandleScrollSlotLearnRequest);
                scrollSlots.Add(slotUi);
            }
        }

        private void HandleScrollSlotLearnRequest(int index)
        {
            if (craftingManager == null)
                return;

            IReadOnlyList<string> pendingBefore = craftingManager.GetPendingBlueprintScrolls();
            if (index < 0 || index >= pendingBefore.Count)
                return;

            string recipeId = pendingBefore[index];
            RecipeDefinition recipe = RecipeRegistry.Resolve(recipeId);

            if (!craftingManager.TryLearnPendingScrollAt(index))
                return;

            string recipeName = recipe != null && !string.IsNullOrEmpty(recipe.displayName)
                ? recipe.displayName
                : recipeId;
            PickupToastUI.Show($"Learned blueprint: {recipeName}");
            RefreshRecipeList();
        }

        private void CreateRecipeSlot(RecipeDefinition recipe)
        {
            if (recipe == null)
                return;

            GameObject slotObject = new GameObject($"RecipeSlot_{recipe.ResolvedId}", typeof(RectTransform));
            slotObject.transform.SetParent(recipeListParent, false);

            RecipeCraftSlotUI slotUi = slotObject.AddComponent<RecipeCraftSlotUI>();
            bool productionMode = presentationMode == CraftingUiPresentationMode.Production;
            bool canCraft = productionMode
                && craftingManager != null
                && inventorySystem != null
                && craftingManager.CanCraft(recipe, inventorySystem);
            RecipeDefinition capturedRecipe = recipe;
            RecipeCraftSlotUI capturedSlot = slotUi;
            slotUi.Setup(recipe, canCraft, inventorySystem, () =>
            {
                SelectRecipe(capturedRecipe, capturedSlot);
            });

            if (selectedRecipe != null && selectedRecipe == recipe)
            {
                selectedSlotUi = slotUi;
                slotUi.SetSelected(true);
            }

            recipeCraftSlots.Add(slotUi);
        }

        private void SelectRecipe(RecipeDefinition recipe, RecipeCraftSlotUI slotUi)
        {
            if (isCrafting)
                return;

            if (selectedSlotUi != null && selectedSlotUi != slotUi)
                selectedSlotUi.SetSelected(false);

            selectedRecipe = recipe;
            selectedSlotUi = slotUi;
            if (selectedSlotUi != null)
                selectedSlotUi.SetSelected(true);

            RefreshCraftDetailPanel();
        }

        private void ClearRecipeSelection()
        {
            if (selectedSlotUi != null)
            {
                selectedSlotUi.SetCraftProgress(0f);
                selectedSlotUi.SetSelected(false);
            }

            selectedRecipe = null;
            selectedSlotUi = null;
            RefreshCraftDetailPanel();
        }

        private void RefreshCraftDetailPanel()
        {
            bool productionMode = presentationMode == CraftingUiPresentationMode.Production;
            bool show = productionMode && craftDetailPanel != null;
            if (craftDetailPanel != null)
                craftDetailPanel.SetActive(show);

            if (!show)
                return;

            if (selectedRecipe == null)
            {
                if (craftDetailTitle != null)
                    craftDetailTitle.text = "Select a blueprint";
                if (craftDetailMeta != null)
                    craftDetailMeta.text = "Click an icon above to craft.";
                if (craftDetailIngredients != null)
                    craftDetailIngredients.text = string.Empty;
                if (craftAmountSlider != null)
                {
                    craftAmountSlider.minValue = 1f;
                    craftAmountSlider.maxValue = 1f;
                    craftAmountSlider.SetValueWithoutNotify(1f);
                    craftAmountSlider.interactable = false;
                }
                if (craftAmountValueLabel != null)
                    craftAmountValueLabel.text = "1";
                if (craftConfirmButton != null)
                    craftConfirmButton.interactable = false;
                if (craftConfirmLabel != null)
                    craftConfirmLabel.text = "Craft";
                return;
            }

            int tier = Mathf.Max(1, selectedRecipe.recipeTier);
            float duration = CraftingManager.GetCraftDurationSeconds(selectedRecipe);
            int maxCount = craftingManager != null && inventorySystem != null
                ? Mathf.Max(0, craftingManager.GetMaxCraftCount(selectedRecipe, inventorySystem))
                : 0;
            bool canAny = maxCount > 0
                && craftingManager != null
                && inventorySystem != null
                && craftingManager.CanCraft(selectedRecipe, inventorySystem, 1);

            if (craftDetailTitle != null)
            {
                craftDetailTitle.text = !string.IsNullOrEmpty(selectedRecipe.displayName)
                    ? selectedRecipe.displayName
                    : selectedRecipe.name;
            }

            if (craftDetailMeta != null)
            {
                int previewAmount = craftAmountSlider != null
                    ? Mathf.Max(1, Mathf.RoundToInt(craftAmountSlider.value))
                    : 1;
                string timeLabel = previewAmount > 1
                    ? $"{duration:0.#}s each  ·  {duration * previewAmount:0.#}s total"
                    : $"{duration:0.#}s each";
                craftDetailMeta.text =
                    $"Tier {tier}  ·  {timeLabel}  ·  " +
                    (selectedRecipe.outputAmount > 1
                        ? $"{selectedRecipe.outputAmount}x output"
                        : "1x output");
                craftDetailMeta.color = DarkMatterGenesisUiPalette.Gold;
            }

            if (craftDetailIngredients != null)
                craftDetailIngredients.text = BuildIngredientsSummary(selectedRecipe);

            ConfigureAmountSlider(maxCount, canAny);

            if (craftConfirmButton != null)
                craftConfirmButton.interactable = !isCrafting && canAny;
            if (craftConfirmLabel != null)
                craftConfirmLabel.text = isCrafting ? "Crafting…" : "Craft";
        }

        private void ConfigureAmountSlider(int maxCount, bool canAny)
        {
            if (craftAmountSlider == null)
                return;

            int max = Mathf.Max(1, maxCount);
            int previous = Mathf.Max(1, Mathf.RoundToInt(craftAmountSlider.value));
            int current = Mathf.Clamp(previous, 1, max);

            craftAmountSlider.wholeNumbers = true;
            craftAmountSlider.minValue = 1f;
            craftAmountSlider.maxValue = max;
            craftAmountSlider.SetValueWithoutNotify(current);
            craftAmountSlider.interactable = !isCrafting && canAny && max > 1;
            if (craftAmountValueLabel != null)
                craftAmountValueLabel.text = current.ToString();
        }

        private static string BuildIngredientsSummary(RecipeDefinition recipe)
        {
            if (recipe?.ingredients == null || recipe.ingredients.Count == 0)
                return "No ingredients";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < recipe.ingredients.Count; i++)
            {
                RecipeIngredient ingredient = recipe.ingredients[i];
                if (ingredient == null || ingredient.item == null || ingredient.amount <= 0)
                    continue;

                if (sb.Length > 0)
                    sb.Append("  ·  ");
                sb.Append(ingredient.amount);
                sb.Append('×');
                sb.Append(ingredient.item.itemName);
            }

            return sb.Length > 0 ? sb.ToString() : "No ingredients";
        }

        private void HandleCraftAmountChanged(float value)
        {
            if (craftAmountSlider == null)
                return;

            int min = Mathf.RoundToInt(craftAmountSlider.minValue);
            int max = Mathf.Max(min, Mathf.RoundToInt(craftAmountSlider.maxValue));
            int stepped = Mathf.Clamp(Mathf.RoundToInt(value), min, max);

            if (!Mathf.Approximately(craftAmountSlider.value, stepped))
                craftAmountSlider.SetValueWithoutNotify(stepped);

            if (craftAmountValueLabel != null)
                craftAmountValueLabel.text = stepped.ToString();

            UpdateCraftDetailTimeLabel(stepped);
        }

        private void UpdateCraftDetailTimeLabel(int amount)
        {
            if (craftDetailMeta == null || selectedRecipe == null)
                return;

            int tier = Mathf.Max(1, selectedRecipe.recipeTier);
            float duration = CraftingManager.GetCraftDurationSeconds(selectedRecipe);
            amount = Mathf.Max(1, amount);
            string timeLabel = amount > 1
                ? $"{duration:0.#}s each  ·  {duration * amount:0.#}s total"
                : $"{duration:0.#}s each";
            craftDetailMeta.text =
                $"Tier {tier}  ·  {timeLabel}  ·  " +
                (selectedRecipe.outputAmount > 1
                    ? $"{selectedRecipe.outputAmount}x output"
                    : "1x output");
            craftDetailMeta.color = DarkMatterGenesisUiPalette.Gold;
        }

        private void HandleCraftConfirmClicked()
        {
            if (isCrafting || selectedRecipe == null || craftingManager == null || inventorySystem == null)
                return;

            if (presentationMode != CraftingUiPresentationMode.Production)
                return;

            int amount = GetSelectedCraftAmount();

            if (!craftingManager.CanCraft(selectedRecipe, inventorySystem, amount))
            {
                PickupToastUI.Show("Cannot craft — check ingredients, station, or inventory space.");
                RefreshCraftDetailPanel();
                return;
            }

            if (craftRoutine != null)
                StopCoroutine(craftRoutine);
            craftRoutine = StartCoroutine(RunCraftRoutine(selectedRecipe, amount));
        }

        private int GetSelectedCraftAmount()
        {
            if (craftAmountSlider == null)
                return 1;

            int min = Mathf.RoundToInt(craftAmountSlider.minValue);
            int max = Mathf.Max(min, Mathf.RoundToInt(craftAmountSlider.maxValue));
            return Mathf.Clamp(Mathf.RoundToInt(craftAmountSlider.value), min, max);
        }

        private IEnumerator RunCraftRoutine(RecipeDefinition recipe, int amount)
        {
            isCrafting = true;
            RefreshCraftDetailPanel();

            float durationPerItem = CraftingManager.GetCraftDurationSeconds(recipe);
            RecipeCraftSlotUI slot = selectedSlotUi;
            int crafted = 0;
            amount = Mathf.Max(1, amount);

            for (int i = 0; i < amount; i++)
            {
                float elapsed = 0f;
                while (elapsed < durationPerItem)
                {
                    if (recipe == null || selectedRecipe != recipe)
                    {
                        slot?.SetCraftProgress(0f);
                        isCrafting = false;
                        craftRoutine = null;
                        RefreshCraftDetailPanel();
                        yield break;
                    }

                    elapsed += Time.unscaledDeltaTime;
                    float itemProgress = Mathf.Clamp01(elapsed / durationPerItem);
                    slot?.SetCraftProgress((i + itemProgress) / amount);
                    yield return null;
                }

                bool success = craftingManager != null
                    && inventorySystem != null
                    && craftingManager.TryCraft(recipe, inventorySystem, 1);

                if (!success)
                {
                    slot?.SetCraftProgress(0f);
                    isCrafting = false;
                    craftRoutine = null;
                    PickupToastUI.Show(crafted > 0
                        ? $"Crafted {crafted}, then stopped — ingredients or space changed."
                        : "Craft failed — ingredients or space changed.");
                    RefreshRecipeList();
                    yield break;
                }

                crafted++;
                slot?.SetCraftProgress((float)crafted / amount);
            }

            slot?.SetCraftProgress(0f);
            isCrafting = false;
            craftRoutine = null;
            RefreshRecipeList();
        }

        private void CancelActiveCraft()
        {
            if (craftRoutine != null)
            {
                StopCoroutine(craftRoutine);
                craftRoutine = null;
            }

            if (selectedSlotUi != null)
                selectedSlotUi.SetCraftProgress(0f);

            isCrafting = false;
        }

        private void ApplyPresentationChrome()
        {
            bool libraryMode = presentationMode == CraftingUiPresentationMode.Library;
            string title = libraryMode ? "Blueprints" : "Crafting";

            if (headerLabel != null)
                headerLabel.text = title;

            if (standaloneTitleLabel != null)
                standaloneTitleLabel.text = title;

            // Standalone window already has a title bar — hide the inner CraftPanel header
            // so "Crafting" does not stack over the station status line.
            if (headerObject != null)
                headerObject.SetActive(!craftPanelEmbedded && !standaloneOpen);
        }

        private void ApplyEmbeddedAppearance(bool embedded)
        {
            if (panelBackground != null)
                panelBackground.color = embedded
                    ? new Color(0f, 0f, 0f, 0f)
                    : DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.82f);

            if (recipeScrollBackground != null)
                recipeScrollBackground.color = embedded
                    ? DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.35f)
                    : DarkMatterGenesisUiPalette.ScrollBackground;

            if (headerObject != null)
                headerObject.SetActive(!embedded && !standaloneOpen);

            if (panelLayout != null)
            {
                panelLayout.childForceExpandHeight = embedded;
                panelLayout.padding = embedded
                    ? JournalPanelLayout.PanelPaddingRect
                    : new RectOffset(Si(12f), Si(12f), Si(12f), Si(12f));
                panelLayout.spacing = embedded ? JournalPanelLayout.SectionSpacing : Si(8f);
            }

            if (recipeScrollLayoutElement != null)
            {
                recipeScrollLayoutElement.minHeight = embedded ? S(240f) : S(220f);
                recipeScrollLayoutElement.flexibleHeight = 1f;
            }
        }

        private void BindSystems()
        {
            CraftingManager resolvedManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();
            if (craftingManager != null && craftingManager != resolvedManager)
            {
                craftingManager.OnRecipesChanged -= RefreshRecipeList;
                craftingManager.OnPendingScrollsChanged -= RefreshRecipeList;
                craftingManager.OnCrafted -= HandleCrafted;
            }

            craftingManager = resolvedManager;

            if (inventorySystem == null)
            {
                GameObject player = PlayerLocator.FindPlayerObject();
                if (player != null)
                    inventorySystem = player.GetComponent<InventorySystem>();
            }

            if (craftingManager != null)
            {
                craftingManager.OnRecipesChanged -= RefreshRecipeList;
                craftingManager.OnRecipesChanged += RefreshRecipeList;
                craftingManager.OnPendingScrollsChanged -= RefreshRecipeList;
                craftingManager.OnPendingScrollsChanged += RefreshRecipeList;
                craftingManager.OnCrafted -= HandleCrafted;
                craftingManager.OnCrafted += HandleCrafted;
            }

            if (inventorySystem != null)
            {
                inventorySystem.OnInventoryChanged -= RefreshRecipeList;
                inventorySystem.OnInventoryChanged += RefreshRecipeList;
            }
        }

        private void UnbindSystems()
        {
            if (craftingManager != null)
            {
                craftingManager.OnRecipesChanged -= RefreshRecipeList;
                craftingManager.OnPendingScrollsChanged -= RefreshRecipeList;
                craftingManager.OnCrafted -= HandleCrafted;
            }

            if (inventorySystem != null)
                inventorySystem.OnInventoryChanged -= RefreshRecipeList;
        }

        private void HandleCrafted(RecipeDefinition recipe)
        {
            if (recipe?.outputItem != null)
            {
                if (recipe.outputItem.IsInventoryStorageModule)
                    PickupToastUI.Show("Crafted Increase Storage Module — inventory row unlocked.");
                else
                    PickupToastUI.Show($"Crafted {recipe.outputAmount}x {recipe.outputItem.itemName}");
            }

            RefreshRecipeList();
        }

        private void EnsureStandaloneWindow(Transform overlayParent)
        {
            if (overlayParent == null)
                return;

            if (standaloneWindowRoot != null && standaloneChromeVersion == CraftingUiChromeVersion)
            {
                ApplyStandaloneWindowSize();
                return;
            }

            if (standaloneWindowRoot != null)
            {
                Destroy(standaloneWindowRoot);
                standaloneWindowRoot = null;
                standaloneWindowRect = null;
                standaloneContentParent = null;
                standaloneTitleLabel = null;
            }

            standaloneChromeVersion = CraftingUiChromeVersion;
            ShiftUiTheme theme = ShiftUiTheme.Current;

            standaloneWindowRoot = new GameObject("CraftingWindow", typeof(RectTransform));
            standaloneWindowRoot.transform.SetParent(overlayParent, false);
            standaloneWindowRect = standaloneWindowRoot.GetComponent<RectTransform>();
            standaloneWindowRect.anchorMin = new Vector2(0.5f, 0.5f);
            standaloneWindowRect.anchorMax = new Vector2(0.5f, 0.5f);
            standaloneWindowRect.pivot = new Vector2(0.5f, 0.5f);
            ApplyStandaloneWindowSize();

            Image windowBg = standaloneWindowRoot.AddComponent<Image>();
            if (theme != null)
                theme.ApplyPanelImage(windowBg, large: true);
            else
            {
                MenuUiBuilder.ApplyUiSprite(windowBg);
                windowBg.color = DarkMatterGenesisUiPalette.PanelBackground;
            }

            VerticalLayoutGroup windowLayout = standaloneWindowRoot.AddComponent<VerticalLayoutGroup>();
            windowLayout.padding = new RectOffset(Si(8f), Si(8f), Si(8f), Si(8f));
            windowLayout.spacing = Si(6f);
            windowLayout.childControlWidth = true;
            windowLayout.childControlHeight = true;
            windowLayout.childForceExpandWidth = true;
            windowLayout.childForceExpandHeight = false;

            GameObject titleBar = MenuUiBuilder.CreatePanelTitleBar(
                standaloneWindowRoot.transform,
                "Crafting",
                S(34f),
                S(14f));
            standaloneTitleLabel = titleBar != null
                ? titleBar.GetComponentInChildren<TextMeshProUGUI>(true)
                : null;

            GameObject closeRow = new GameObject("CloseRow", typeof(RectTransform));
            closeRow.transform.SetParent(standaloneWindowRoot.transform, false);
            HorizontalLayoutGroup closeLayout = closeRow.AddComponent<HorizontalLayoutGroup>();
            closeLayout.childAlignment = TextAnchor.MiddleRight;
            closeLayout.spacing = Si(8f);
            closeLayout.childControlWidth = false;
            closeLayout.childControlHeight = false;
            closeLayout.childForceExpandWidth = false;
            closeLayout.childForceExpandHeight = false;
            LayoutElement closeRowLayout = closeRow.AddComponent<LayoutElement>();
            closeRowLayout.minHeight = S(30f);
            closeRowLayout.preferredHeight = S(30f);
            closeRowLayout.flexibleWidth = 1f;

            // Spacer so the Close button stays right-aligned without stretching into an oval.
            GameObject spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(closeRow.transform, false);
            LayoutElement spacerLayout = spacer.GetComponent<LayoutElement>();
            spacerLayout.flexibleWidth = 1f;
            spacerLayout.minWidth = 0f;

            Button closeButton = MenuUiBuilder.CreateButton(
                closeRow.transform,
                "Close",
                new Vector2(S(78f), S(28f)),
                S(13f));
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => CloseStandalonePanel());

            GameObject contentHost = new GameObject("CraftContentHost", typeof(RectTransform));
            contentHost.transform.SetParent(standaloneWindowRoot.transform, false);
            LayoutElement contentLayout = contentHost.AddComponent<LayoutElement>();
            contentLayout.flexibleHeight = 1f;
            contentLayout.minHeight = S(520f);
            standaloneContentParent = contentHost.transform;

            windowBg.raycastTarget = true;

            standaloneWindowRoot.SetActive(false);
        }

        private void EnsurePanelBuilt()
        {
            if (isBuilt
                && builtChromeVersion == CraftingUiChromeVersion
                && craftPanel != null
                && recipeListParent != null
                && recipeListScrollRect != null
                && craftDetailPanel != null)
            {
                return;
            }

            if (craftPanel != null)
            {
                Destroy(craftPanel);
                craftPanel = null;
            }

            craftDetailPanel = null;
            BuildPanel();
            isBuilt = true;
            builtChromeVersion = CraftingUiChromeVersion;
        }

        private void BuildPanel()
        {
            craftPanel = new GameObject("CraftPanel", typeof(RectTransform));
            craftPanel.transform.SetParent(transform, false);

            RectTransform panelRt = craftPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(S(StandaloneWindowWidth), S(StandaloneWindowHeight));
            panelRt.anchoredPosition = Vector2.zero;

            panelBackground = craftPanel.AddComponent<Image>();
            panelBackground.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.82f);

            VerticalLayoutGroup panelLayoutGroup = craftPanel.AddComponent<VerticalLayoutGroup>();
            panelLayout = panelLayoutGroup;
            panelLayout.padding = new RectOffset(Si(12f), Si(12f), Si(12f), Si(12f));
            panelLayout.spacing = Si(8f);
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            headerObject = new GameObject("Header", typeof(RectTransform));
            headerObject.transform.SetParent(craftPanel.transform, false);
            headerLabel = CreateText(headerObject.transform, "Blueprints", JournalPanelLayout.HeaderFontSize + 4f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            headerLabel.color = DarkMatterGenesisUiPalette.WarmOffWhite;

            statusText = CreateText(craftPanel.transform, "Use a cooking pot or workbench to craft.", JournalPanelLayout.BodyFontSize, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            statusText.color = DarkMatterGenesisUiPalette.Gold;

            JournalPanelLayout.CreateSectionDivider(craftPanel.transform);

            scrollSectionLabel = CreateText(craftPanel.transform, "Pending Blueprints", JournalPanelLayout.HeaderFontSize, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            scrollSectionLabel.color = DarkMatterGenesisUiPalette.WarmOffWhite;

            GameObject scrollRowHost = new GameObject("RecipeScrollSlots", typeof(RectTransform));
            scrollRowHost.transform.SetParent(craftPanel.transform, false);
            float scrollRowHeight = HudLayoutMetrics.InventorySlotSize(80f) + S(12f);
            LayoutElement scrollRowLayout = scrollRowHost.AddComponent<LayoutElement>();
            scrollRowLayout.minHeight = scrollRowHeight;
            scrollRowLayout.preferredHeight = scrollRowHeight;

            GameObject scrollViewport = new GameObject("ScrollViewport", typeof(RectTransform));
            scrollViewport.transform.SetParent(scrollRowHost.transform, false);
            RectTransform scrollViewportRt = scrollViewport.GetComponent<RectTransform>();
            scrollViewportRt.anchorMin = Vector2.zero;
            scrollViewportRt.anchorMax = Vector2.one;
            scrollViewportRt.offsetMin = Vector2.zero;
            scrollViewportRt.offsetMax = Vector2.zero;

            Image pendingScrollBg = scrollViewport.AddComponent<Image>();
            pendingScrollBg.color = new Color(1f, 1f, 1f, 0.01f);
            pendingScrollBg.raycastTarget = true;

            ScrollRect scrollSlotsScroll = scrollViewport.AddComponent<ScrollRect>();
            scrollSlotsScroll.horizontal = true;
            scrollSlotsScroll.vertical = false;
            scrollSlotsScroll.movementType = ScrollRect.MovementType.Clamped;
            scrollSlotsScroll.scrollSensitivity = 24f;

            GameObject slotsViewport = new GameObject("Viewport", typeof(RectTransform));
            slotsViewport.transform.SetParent(scrollViewport.transform, false);
            RectTransform slotsViewportRt = slotsViewport.GetComponent<RectTransform>();
            slotsViewportRt.anchorMin = Vector2.zero;
            slotsViewportRt.anchorMax = Vector2.one;
            slotsViewportRt.offsetMin = Vector2.zero;
            slotsViewportRt.offsetMax = Vector2.zero;
            Image slotsViewportImage = slotsViewport.AddComponent<Image>();
            slotsViewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            slotsViewportImage.raycastTarget = true;
            slotsViewport.AddComponent<RectMask2D>();

            GameObject slotsContent = new GameObject("Content", typeof(RectTransform));
            slotsContent.transform.SetParent(slotsViewport.transform, false);
            RectTransform slotsContentRt = slotsContent.GetComponent<RectTransform>();
            slotsContentRt.anchorMin = new Vector2(0f, 0.5f);
            slotsContentRt.anchorMax = new Vector2(0f, 0.5f);
            slotsContentRt.pivot = new Vector2(0f, 0.5f);
            slotsContentRt.anchoredPosition = Vector2.zero;

            HorizontalLayoutGroup slotsLayout = slotsContent.AddComponent<HorizontalLayoutGroup>();
            slotsLayout.spacing = Si(8f);
            slotsLayout.padding = new RectOffset(Si(4f), Si(4f), Si(4f), Si(4f));
            slotsLayout.childAlignment = TextAnchor.MiddleLeft;
            slotsLayout.childControlWidth = false;
            slotsLayout.childControlHeight = true;
            slotsLayout.childForceExpandWidth = false;
            slotsLayout.childForceExpandHeight = false;
            ContentSizeFitter slotsFitter = slotsContent.AddComponent<ContentSizeFitter>();
            slotsFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            slotsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollSlotsScroll.viewport = slotsViewportRt;
            scrollSlotsScroll.content = slotsContentRt;
            recipeScrollSlotsParent = slotsContent.transform;

            scrollHintText = CreateText(craftPanel.transform, "Collect blueprints in the world to fill these slots.", JournalPanelLayout.SecondaryFontSize, FontStyles.Italic, TextAlignmentOptions.MidlineLeft);
            scrollHintText.color = DarkMatterGenesisUiPalette.Gold;

            JournalPanelLayout.CreateSectionDivider(craftPanel.transform);

            learnedSectionLabel = CreateText(craftPanel.transform, "Learned Blueprints", JournalPanelLayout.HeaderFontSize, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            learnedSectionLabel.color = DarkMatterGenesisUiPalette.WarmOffWhite;

            GameObject emptyHost = new GameObject("EmptyStateHost", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            emptyHost.transform.SetParent(craftPanel.transform, false);
            LayoutElement emptyLayout = emptyHost.GetComponent<LayoutElement>();
            emptyLayout.minHeight = 140f;
            emptyLayout.flexibleHeight = 0f;
            emptyLayout.flexibleWidth = 1f;
            VerticalLayoutGroup emptyHostLayout = emptyHost.GetComponent<VerticalLayoutGroup>();
            emptyHostLayout.spacing = 0f;
            emptyHostLayout.padding = new RectOffset(0, 0, 4, 4);
            emptyHostLayout.childControlWidth = true;
            emptyHostLayout.childControlHeight = true;
            emptyHostLayout.childForceExpandWidth = true;
            emptyHostLayout.childForceExpandHeight = false;
            emptyStateHost = emptyHost.transform;
            emptyHost.SetActive(false);

            GameObject scrollObj = new GameObject("RecipeScrollView", typeof(RectTransform));
            scrollObj.transform.SetParent(craftPanel.transform, false);
            recipeScrollObject = scrollObj;
            recipeScrollLayoutElement = scrollObj.AddComponent<LayoutElement>();
            recipeScrollLayoutElement.flexibleHeight = 1f;
            recipeScrollLayoutElement.minHeight = S(220f);

            recipeScrollBackground = scrollObj.AddComponent<Image>();
            recipeScrollBackground.color = DarkMatterGenesisUiPalette.ScrollBackground;
            recipeScrollBackground.raycastTarget = true;

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            recipeListScrollRect = scroll;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform viewportRt = viewport.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(S(4f), S(4f));
            viewportRt.offsetMax = new Vector2(S(-4f), S(-4f));
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewportImage.raycastTarget = true;
            viewport.AddComponent<RectMask2D>();
            scroll.viewport = viewportRt;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = content.GetComponent<RectTransform>();
            recipeListContentRect = contentRt;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            float slotSize = RecipeSlotSize;
            GridLayoutGroup contentLayout = content.AddComponent<GridLayoutGroup>();
            contentLayout.cellSize = new Vector2(slotSize, slotSize);
            contentLayout.spacing = new Vector2(4f, 4f);
            contentLayout.padding = new RectOffset(4, 4, 4, 4);
            contentLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            contentLayout.constraintCount = RecipeGridColumns;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            contentLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRt;
            recipeListParent = content.transform;

            BuildCraftDetailPanel(craftPanel.transform);

            craftPanel.SetActive(false);
        }

        private void BuildCraftDetailPanel(Transform parent)
        {
            craftDetailPanel = new GameObject("CraftDetailPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            craftDetailPanel.transform.SetParent(parent, false);

            LayoutElement detailLayoutElement = craftDetailPanel.GetComponent<LayoutElement>();
            detailLayoutElement.minHeight = S(168f);
            detailLayoutElement.preferredHeight = S(168f);
            detailLayoutElement.flexibleHeight = 0f;

            Image detailBg = craftDetailPanel.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(detailBg);
            detailBg.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.CharcoalGray, 0.72f);
            detailBg.raycastTarget = true;

            VerticalLayoutGroup detailLayout = craftDetailPanel.GetComponent<VerticalLayoutGroup>();
            detailLayout.padding = new RectOffset(Si(10f), Si(10f), Si(8f), Si(8f));
            detailLayout.spacing = Si(4f);
            detailLayout.childAlignment = TextAnchor.UpperLeft;
            detailLayout.childControlWidth = true;
            detailLayout.childControlHeight = true;
            detailLayout.childForceExpandWidth = true;
            detailLayout.childForceExpandHeight = false;

            craftDetailTitle = CreateText(
                craftDetailPanel.transform,
                "Select a blueprint",
                JournalPanelLayout.HeaderFontSize,
                FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft);
            craftDetailTitle.color = DarkMatterGenesisUiPalette.WarmOffWhite;

            craftDetailMeta = CreateText(
                craftDetailPanel.transform,
                "Click an icon above to craft.",
                JournalPanelLayout.SecondaryFontSize,
                FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);
            craftDetailMeta.color = DarkMatterGenesisUiPalette.Gold;

            craftDetailIngredients = CreateText(
                craftDetailPanel.transform,
                string.Empty,
                JournalPanelLayout.SecondaryFontSize,
                FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);
            craftDetailIngredients.color = DarkMatterGenesisUiPalette.BodyText;

            craftAmountSlider = MenuUiBuilder.CreateSliderRow(
                craftDetailPanel.transform,
                "Amount",
                1f,
                out craftAmountValueLabel,
                handleWidth: 20f,
                handleHeight: 24f);
            craftAmountSlider.minValue = 1f;
            craftAmountSlider.maxValue = 1f;
            craftAmountSlider.wholeNumbers = true;
            craftAmountSlider.navigation = new Navigation { mode = Navigation.Mode.None };
            craftAmountSlider.SetValueWithoutNotify(1f);
            craftAmountSlider.onValueChanged.RemoveAllListeners();
            craftAmountSlider.onValueChanged.AddListener(HandleCraftAmountChanged);

            GameObject buttonRow = new GameObject("CraftButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            buttonRow.transform.SetParent(craftDetailPanel.transform, false);
            LayoutElement buttonRowLayout = buttonRow.GetComponent<LayoutElement>();
            buttonRowLayout.minHeight = S(36f);
            buttonRowLayout.preferredHeight = S(36f);

            HorizontalLayoutGroup buttonLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            buttonLayout.childAlignment = TextAnchor.MiddleRight;
            buttonLayout.childControlWidth = false;
            buttonLayout.childForceExpandWidth = false;
            buttonLayout.spacing = Si(8f);

            craftConfirmButton = MenuUiBuilder.CreateButton(
                buttonRow.transform,
                "Craft",
                new Vector2(S(140f), S(32f)),
                JournalPanelLayout.BodyFontSize);
            craftConfirmButton.onClick.RemoveAllListeners();
            craftConfirmButton.onClick.AddListener(HandleCraftConfirmClicked);
            craftConfirmLabel = craftConfirmButton.GetComponentInChildren<TextMeshProUGUI>(true);

            craftDetailPanel.SetActive(false);
        }

        private static TextMeshProUGUI CreateText(Transform parent, string value, float fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject("Text", typeof(RectTransform));
            textObj.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(text);
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = DarkMatterGenesisUiPalette.BodyText;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static void StretchToParent(RectTransform rect)
        {
            MenuUiBuilder.StretchRectToFill(rect);
        }
    }
}

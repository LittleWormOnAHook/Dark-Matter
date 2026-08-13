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
        private const float StandaloneWindowHeight = 480f;
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
            ReleaseStandaloneInput();
            UnbindSystems();
        }

        public bool IsStandaloneOpen => standaloneOpen;
        public CraftingUiPresentationMode PresentationMode => presentationMode;

        public static bool IsAnyStandaloneOpen
        {
            get
            {
                CraftingUI ui = FindAnyObjectByType<CraftingUI>();
                return ui != null && ui.standaloneOpen;
            }
        }

        public static void CloseAnyOpenStandalone()
        {
            CraftingUI ui = FindAnyObjectByType<CraftingUI>();
            ui?.CloseStandalonePanel(clearStation: true);
        }

        public void SetPresentationMode(CraftingUiPresentationMode mode)
        {
            presentationMode = mode;
            ApplyPresentationChrome();
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
            if (!standaloneOpen && !craftPanelEmbedded)
                return;

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
            IReadOnlyList<RecipeDefinition> recipes = craftingManager.GetDiscoveredRecipes();
            int pendingScrolls = craftingManager.GetPendingBlueprintScrolls().Count;

            if (statusText != null)
            {
                if (libraryMode)
                {
                    if (recipes.Count == 0 && pendingScrolls == 0)
                        statusText.text = string.Empty;
                    else if (pendingScrolls > 0)
                    {
                        statusText.text =
                            $"{JournalPanelLayout.FormatGoldValue(pendingScrolls.ToString())} pending scroll(s)  ·  " +
                            $"{recipes.Count} learned  ·  Craft at a cooking pot or workbench.";
                        statusText.color = SurvivalPioneerUiPalette.Gold;
                    }
                    else
                    {
                        statusText.text =
                            $"{JournalPanelLayout.FormatGoldValue(recipes.Count.ToString())} learned  ·  " +
                            "Visit a cooking pot or workbench to craft.";
                        statusText.color = SurvivalPioneerUiPalette.Gold;
                    }
                }
                else if (recipes.Count == 0)
                {
                    statusText.text = string.Empty;
                }
                else if (!craftingManager.CurrentStation.HasValue)
                {
                    statusText.text =
                        $"{JournalPanelLayout.FormatGoldValue(recipes.Count.ToString())} learned  ·  " +
                        (pendingScrolls > 0
                            ? $"{pendingScrolls} pending scroll(s)  ·  Approach a station to craft."
                            : "Approach a cooking pot or workbench to craft.");
                    statusText.color = SurvivalPioneerUiPalette.Gold;
                }
                else
                {
                    CraftingStationType station = craftingManager.CurrentStation.Value;
                    string stationLabel = station == CraftingStationType.Cooking ? "Cooking" : "Workbench";
                    int craftableAtStation = 0;
                    for (int i = 0; i < recipes.Count; i++)
                    {
                        if (recipes[i] != null && recipes[i].stationType == station)
                            craftableAtStation++;
                    }

                    string baseStatus = craftableAtStation > 0
                        ? $"{stationLabel} station  ·  {JournalPanelLayout.FormatGoldValue(craftableAtStation.ToString())} ready to craft  ·  {recipes.Count} learned"
                        : $"{stationLabel} station  ·  {recipes.Count} learned  ·  none craftable here yet";
                    if (pendingScrolls > 0)
                        baseStatus += $"  ·  {JournalPanelLayout.FormatGoldValue(pendingScrolls.ToString())} pending scroll(s)";
                    statusText.text = baseStatus;
                    statusText.color = SurvivalPioneerUiPalette.Gold;
                }
            }

            if (learnedSectionLabel != null)
            {
                learnedSectionLabel.text = recipes.Count > 0
                    ? $"Learned Blueprints  ·  {JournalPanelLayout.FormatGoldValue(recipes.Count.ToString())}"
                    : "Learned Blueprints";
                learnedSectionLabel.color = SurvivalPioneerUiPalette.WarmOffWhite;
            }

            if (scrollSectionLabel != null)
            {
                scrollSectionLabel.text = pendingScrolls > 0
                    ? $"Pending Scrolls  ·  {JournalPanelLayout.FormatGoldValue(pendingScrolls.ToString())}"
                    : "Pending Scrolls";
                scrollSectionLabel.color = SurvivalPioneerUiPalette.WarmOffWhite;
            }

            if (recipes.Count == 0)
            {
                if (recipeScrollObject != null)
                    recipeScrollObject.SetActive(false);

                ShowEmptyState(
                    "No crafts unlocked",
                    pendingScrolls > 0
                        ? "You have unread blueprint scrolls waiting above."
                        : "Collect one-time blueprint scrolls in the world to unlock recipes.",
                    pendingScrolls > 0
                        ? "Right-click a scroll → Learn, then craft at a station."
                        : "Cooking pots and workbenches unlock once you learn matching blueprints.");
                return;
            }

            ClearEmptyState();
            if (recipeScrollObject != null)
                recipeScrollObject.SetActive(true);

            foreach (RecipeDefinition recipe in recipes)
                CreateRecipeSlot(recipe);

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
                    scrollHintText.color = SurvivalPioneerUiPalette.Gold;
                }
                return;
            }

            IReadOnlyList<string> pending = craftingManager.GetPendingBlueprintScrolls();
            if (scrollHintText != null)
            {
                scrollHintText.text = pending.Count > 0
                    ? "Right-click a scroll, then click Learn to confirm."
                    : "Collect blueprints in the world to fill these slots.";
                scrollHintText.color = SurvivalPioneerUiPalette.Gold;
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
            slotUi.Setup(recipe, canCraft, inventorySystem, () =>
            {
                if (!productionMode)
                    return;

                if (craftingManager != null && inventorySystem != null && craftingManager.TryCraft(capturedRecipe, inventorySystem))
                    RefreshRecipeList();
            });

            recipeCraftSlots.Add(slotUi);
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
                    : SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.82f);

            if (recipeScrollBackground != null)
                recipeScrollBackground.color = embedded
                    ? SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.35f)
                    : SurvivalPioneerUiPalette.ScrollBackground;

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
            if (standaloneWindowRoot != null || overlayParent == null)
                return;

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
                windowBg.color = SurvivalPioneerUiPalette.PanelBackground;
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
            closeLayout.childControlWidth = false;
            closeLayout.childForceExpandWidth = true;
            LayoutElement closeRowLayout = closeRow.AddComponent<LayoutElement>();
            closeRowLayout.minHeight = S(32f);
            closeRowLayout.preferredHeight = S(32f);

            MenuUiBuilder.CreateCircleCloseButton(closeRow.transform, S(32f), () => CloseStandalonePanel());

            GameObject contentHost = new GameObject("CraftContentHost", typeof(RectTransform));
            contentHost.transform.SetParent(standaloneWindowRoot.transform, false);
            LayoutElement contentLayout = contentHost.AddComponent<LayoutElement>();
            contentLayout.flexibleHeight = 1f;
            contentLayout.minHeight = S(400f);
            standaloneContentParent = contentHost.transform;

            windowBg.raycastTarget = true;

            standaloneWindowRoot.SetActive(false);
        }

        private void EnsurePanelBuilt()
        {
            if (isBuilt && craftPanel != null && recipeListParent != null && recipeListScrollRect != null)
                return;

            if (craftPanel != null)
            {
                Destroy(craftPanel);
                craftPanel = null;
            }

            BuildPanel();
            isBuilt = true;
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
            panelBackground.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.82f);

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
            headerLabel.color = SurvivalPioneerUiPalette.WarmOffWhite;

            statusText = CreateText(craftPanel.transform, "Use a cooking pot or workbench to craft.", JournalPanelLayout.BodyFontSize, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            statusText.color = SurvivalPioneerUiPalette.Gold;

            JournalPanelLayout.CreateSectionDivider(craftPanel.transform);

            scrollSectionLabel = CreateText(craftPanel.transform, "Pending Scrolls", JournalPanelLayout.HeaderFontSize, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            scrollSectionLabel.color = SurvivalPioneerUiPalette.WarmOffWhite;

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
            scrollHintText.color = SurvivalPioneerUiPalette.Gold;

            JournalPanelLayout.CreateSectionDivider(craftPanel.transform);

            learnedSectionLabel = CreateText(craftPanel.transform, "Learned Blueprints", JournalPanelLayout.HeaderFontSize, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            learnedSectionLabel.color = SurvivalPioneerUiPalette.WarmOffWhite;

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
            recipeScrollBackground.color = SurvivalPioneerUiPalette.ScrollBackground;
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

            craftPanel.SetActive(false);
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
            text.color = SurvivalPioneerUiPalette.BodyText;
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

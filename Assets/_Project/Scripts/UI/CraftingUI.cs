using System.Collections.Generic;
using Project.Core;
using Project.Crafting;
using Project.Data;
using Project.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public class CraftingUI : MonoBehaviour
    {
        private const float PanelScale = 0.75f;
        private const int RecipeGridColumns = 7;
        private static float RecipeSlotSize => HudLayoutMetrics.InventorySlotSize(64f);

        private static float S(float value) => value * PanelScale;
        private static int Si(float value) => Mathf.RoundToInt(value * PanelScale);

        private GameObject craftPanel;
        private VerticalLayoutGroup panelLayout;
        private LayoutElement recipeScrollLayoutElement;
        private Image panelBackground;
        private GameObject headerObject;
        private Image recipeScrollBackground;
        private Transform recipeScrollSlotsParent;
        private TextMeshProUGUI scrollSectionLabel;
        private TextMeshProUGUI scrollHintText;
        private Transform recipeListParent;
        private TextMeshProUGUI statusText;

        private readonly List<RecipeCraftSlotUI> recipeCraftSlots = new List<RecipeCraftSlotUI>();
        private readonly List<RecipeScrollSlotUI> scrollSlots = new List<RecipeScrollSlotUI>();
        private bool isBuilt;

        private Transform craftPanelOriginalParent;
        private bool craftPanelEmbedded;

        private GameObject standaloneWindowRoot;
        private RectTransform standaloneWindowRect;
        private Transform standaloneContentParent;
        private bool standaloneOpen;

        private CraftingManager craftingManager;
        private InventorySystem inventorySystem;

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
            UnbindSystems();
        }

        public bool IsStandaloneOpen => standaloneOpen;

        public void OpenStandalonePanel(Transform overlayParent, RectTransform journalPanel, float gap)
        {
            EnsurePanelBuilt();
            EnsureStandaloneWindow(overlayParent);

            if (standaloneOpen && craftPanel != null && craftPanel.transform.parent == standaloneContentParent)
            {
                PositionBesideJournal(journalPanel, gap);
                BindSystems();
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
            ApplyEmbeddedAppearance(true);

            standaloneWindowRoot.SetActive(true);
            standaloneOpen = true;
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
            EnsurePanelBuilt();
            if (craftPanel == null || container == null)
                return;

            HideStandaloneWindowShell();

            BindSystems();
            craftPanelOriginalParent = transform;
            craftPanel.transform.SetParent(container, false);
            StretchToParent(craftPanel.GetComponent<RectTransform>());
            craftPanel.SetActive(true);
            craftPanelEmbedded = true;
            ApplyEmbeddedAppearance(true);
            RefreshRecipeList();
        }

        public void HideStandaloneWindowShell()
        {
            if (standaloneWindowRoot != null)
                standaloneWindowRoot.SetActive(false);

            standaloneOpen = false;
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
                    statusText.text = "Crafting system unavailable.";
                return;
            }

            IReadOnlyList<RecipeDefinition> recipes = craftingManager.GetDiscoveredRecipes();

            if (statusText != null)
            {
                int pendingScrolls = craftingManager.GetPendingRecipeScrolls().Count;
                if (recipes.Count == 0)
                {
                    statusText.text = pendingScrolls > 0
                        ? craftPanelEmbedded
                            ? "Recipe library — right-click recipe scrolls above to learn them."
                            : "Right-click recipe scrolls above to learn them."
                        : craftPanelEmbedded
                            ? "Recipe library — collect recipe scrolls in the world to unlock recipes."
                            : "Collect recipe scrolls in the world to learn recipes.";
                }
                else if (!craftingManager.CurrentStation.HasValue)
                {
                    statusText.text = pendingScrolls > 0
                        ? craftPanelEmbedded
                            ? $"Recipe library — {recipes.Count} learned recipe(s). Right-click scrolls above; visit a station to craft."
                            : $"{recipes.Count} learned recipe(s). Right-click scrolls above or use a station to craft."
                        : craftPanelEmbedded
                            ? $"Recipe library — {recipes.Count} learned recipe(s). Visit a cooking pot or workbench to craft."
                            : $"{recipes.Count} learned recipe(s). Use a cooking pot or workbench to craft.";
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

                    statusText.text = craftableAtStation > 0
                        ? $"{stationLabel} — {craftableAtStation} recipe(s) ready to craft ({recipes.Count} learned)"
                        : $"{recipes.Count} learned recipe(s). None for this {stationLabel.ToLower()} station.";
                }
            }

            foreach (RecipeDefinition recipe in recipes)
                CreateRecipeSlot(recipe);
        }

        private void RefreshScrollSlots()
        {
            if (recipeScrollSlotsParent == null || craftingManager == null)
                return;

            foreach (RecipeScrollSlotUI slot in scrollSlots)
            {
                if (slot != null)
                    Destroy(slot.gameObject);
            }
            scrollSlots.Clear();

            IReadOnlyList<string> pending = craftingManager.GetPendingRecipeScrolls();
            if (scrollHintText != null)
            {
                scrollHintText.text = pending.Count > 0
                    ? "Right-click a scroll to learn the recipe."
                    : "Collect recipe scrolls in the world to fill these slots.";
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

            IReadOnlyList<string> pendingBefore = craftingManager.GetPendingRecipeScrolls();
            if (index < 0 || index >= pendingBefore.Count)
                return;

            string recipeId = pendingBefore[index];
            RecipeDefinition recipe = RecipeRegistry.Resolve(recipeId);

            if (!craftingManager.TryLearnPendingScrollAt(index))
                return;

            string recipeName = recipe != null && !string.IsNullOrEmpty(recipe.displayName)
                ? recipe.displayName
                : recipeId;
            PickupToastUI.Show($"Learned recipe: {recipeName}");
            RefreshRecipeList();
        }

        private void CreateRecipeSlot(RecipeDefinition recipe)
        {
            if (recipe == null)
                return;

            GameObject slotObject = new GameObject($"RecipeSlot_{recipe.ResolvedId}", typeof(RectTransform));
            slotObject.transform.SetParent(recipeListParent, false);

            RecipeCraftSlotUI slotUi = slotObject.AddComponent<RecipeCraftSlotUI>();
            bool canCraft = craftingManager != null && inventorySystem != null && craftingManager.CanCraft(recipe, inventorySystem);
            RecipeDefinition capturedRecipe = recipe;
            slotUi.Setup(recipe, canCraft, inventorySystem, () =>
            {
                if (craftingManager != null && inventorySystem != null && craftingManager.TryCraft(capturedRecipe, inventorySystem))
                    RefreshRecipeList();
            });

            recipeCraftSlots.Add(slotUi);
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
                headerObject.SetActive(!embedded);

            if (panelLayout != null)
                panelLayout.childForceExpandHeight = embedded;

            if (recipeScrollLayoutElement != null)
                recipeScrollLayoutElement.minHeight = embedded ? S(240f) : S(180f);
        }

        private void BindSystems()
        {
            if (craftingManager == null)
                craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();

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
                PickupToastUI.Show($"Crafted {recipe.outputAmount}x {recipe.outputItem.itemName}");
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
            standaloneWindowRect.sizeDelta = new Vector2(S(720f), S(480f));

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

            MenuUiBuilder.CreatePanelTitleBar(
                standaloneWindowRoot.transform,
                "Crafting",
                S(34f),
                S(14f));

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
            contentLayout.minHeight = S(360f);
            standaloneContentParent = contentHost.transform;

            windowBg.raycastTarget = true;

            standaloneWindowRoot.SetActive(false);
        }

        private void EnsurePanelBuilt()
        {
            if (isBuilt && craftPanel != null && recipeListParent != null)
                return;

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
            panelRt.sizeDelta = new Vector2(S(720f), S(480f));
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
            CreateText(headerObject.transform, "Crafting", S(24f), FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

            statusText = CreateText(craftPanel.transform, "Use a cooking pot or workbench to craft.", S(13f), FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            statusText.color = SurvivalPioneerUiPalette.BodyText;

            scrollSectionLabel = CreateText(craftPanel.transform, "Recipe Scrolls", S(15f), FontStyles.Bold, TextAlignmentOptions.MidlineLeft);

            GameObject scrollRowHost = new GameObject("RecipeScrollSlots", typeof(RectTransform));
            scrollRowHost.transform.SetParent(craftPanel.transform, false);
            float scrollRowHeight = RecipeSlotSize + S(8f);
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

            ScrollRect scrollSlotsScroll = scrollViewport.AddComponent<ScrollRect>();
            scrollSlotsScroll.horizontal = true;
            scrollSlotsScroll.vertical = false;
            scrollSlotsScroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject slotsViewport = new GameObject("Viewport", typeof(RectTransform));
            slotsViewport.transform.SetParent(scrollViewport.transform, false);
            RectTransform slotsViewportRt = slotsViewport.GetComponent<RectTransform>();
            slotsViewportRt.anchorMin = Vector2.zero;
            slotsViewportRt.anchorMax = Vector2.one;
            slotsViewportRt.offsetMin = Vector2.zero;
            slotsViewportRt.offsetMax = Vector2.zero;
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

            scrollHintText = CreateText(craftPanel.transform, "Right-click a scroll to learn the recipe.", S(11f), FontStyles.Italic, TextAlignmentOptions.MidlineLeft);
            scrollHintText.color = SurvivalPioneerUiPalette.MutedText;

            GameObject scrollObj = new GameObject("RecipeScrollView", typeof(RectTransform));
            scrollObj.transform.SetParent(craftPanel.transform, false);
            recipeScrollLayoutElement = scrollObj.AddComponent<LayoutElement>();
            recipeScrollLayoutElement.flexibleHeight = 1f;
            recipeScrollLayoutElement.minHeight = S(180f);

            recipeScrollBackground = scrollObj.AddComponent<Image>();
            recipeScrollBackground.color = SurvivalPioneerUiPalette.ScrollBackground;

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform viewportRt = viewport.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(S(4f), S(4f));
            viewportRt.offsetMax = new Vector2(S(-4f), S(-4f));
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRt = content.GetComponent<RectTransform>();
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
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRt;
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

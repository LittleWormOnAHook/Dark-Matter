using System.Collections.Generic;
using System.Text;
using Project.Crafting;
using Project.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public sealed class JournalQuestFullscreenWindow : FullscreenUiWindow
    {
        private JournalPanelUI host;

        public void Configure(JournalPanelUI journalHost)
        {
            host = journalHost;
        }

        protected override void OnBuild()
        {
            if (contentArea == null || host == null)
                return;

            host.BuildQuestWindowContent(contentArea);
        }

        public override void Refresh()
        {
            host?.RefreshQuestList();
        }
    }

    public sealed class PetFullscreenWindow : FullscreenUiWindow
    {
        private PetUI petUi;

        public void Configure(PetUI pet)
        {
            petUi = pet;
        }

        public override void OnShow()
        {
            if (petUi == null)
                petUi = FindAnyObjectByType<PetUI>();

            petUi?.EmbedPanel(contentArea);
            petUi?.RefreshPetList();
        }

        public override void OnHide()
        {
            petUi?.RestorePanel();
        }

        public override void Refresh()
        {
            petUi?.RefreshPetList();
        }
    }

    public sealed class InventoryFullscreenWindow : FullscreenUiWindow
    {
        private InventoryUI inventoryUi;

        public void Configure(InventoryUI inventory)
        {
            inventoryUi = inventory;
        }

        public override void OnShow()
        {
            if (inventoryUi == null)
                inventoryUi = FindAnyObjectByType<InventoryUI>();

            inventoryUi?.EmbedInventoryPanel(contentArea);
            GameplayHudVisibility.SetJournalTabHud(JournalWindowId.Inventory);
        }

        public override void OnHide()
        {
            inventoryUi?.RestoreInventoryPanel();
        }

        public override void Refresh()
        {
            inventoryUi?.RefreshUI();
        }
    }

    public sealed class CraftFullscreenWindow : FullscreenUiWindow
    {
        private CraftingUI craftingUi;

        public void Configure(CraftingUI crafting)
        {
            craftingUi = crafting;
        }

        public override void OnShow()
        {
            if (craftingUi == null)
                craftingUi = FindAnyObjectByType<CraftingUI>();

            craftingUi?.EmbedPanel(contentArea);
            MenuUiBuilder.StretchRectToFill(GetFirstChildRect(contentArea));
        }

        public override void OnHide()
        {
            craftingUi?.RestorePanel();
        }

        public override void Refresh()
        {
            craftingUi?.RefreshRecipeList();
        }

        private static RectTransform GetFirstChildRect(Transform container)
        {
            if (container == null || container.childCount == 0)
                return null;

            return container.GetChild(0) as RectTransform;
        }
    }

    /// <summary>
    /// Real recipe browser for the Journal's Recipes tab (replaces the old StubFullscreenWindow
    /// placeholder). Lists every learned recipe by station with its ingredient costs and output, plus
    /// a summary of unread recipe scrolls waiting to be learned in the inventory.
    /// </summary>
    public sealed class RecipeLibraryFullscreenWindow : FullscreenUiWindow
    {
        private CraftingManager craftingManager;
        private Transform listParent;
        private TextMeshProUGUI pendingSummaryText;

        public override void OnShow()
        {
            if (craftingManager == null)
                craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();
        }

        public override void Refresh()
        {
            BuildListIfNeeded();
            RepopulateList();
        }

        protected override void OnBuild()
        {
            if (contentArea == null)
                return;

            ShiftUiTheme theme = ShiftUiTheme.Current;

            VerticalLayoutGroup rootLayout = contentArea.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(28, 28, 20, 20);
            rootLayout.spacing = 12f;
            rootLayout.childAlignment = TextAnchor.UpperLeft;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = true;
            rootLayout.childForceExpandHeight = false;

            // No section heading — Blueprints tab on the journal rail identifies this panel.
            pendingSummaryText = CreateLabel(contentArea, theme, string.Empty, 18f, FontStyles.Italic);
            pendingSummaryText.color = SurvivalPioneerUiPalette.MutedText;

            GameObject scrollHost = new GameObject("ScrollHost", typeof(RectTransform));
            scrollHost.transform.SetParent(contentArea, false);
            LayoutElement scrollLayout = scrollHost.AddComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1f;
            scrollLayout.flexibleWidth = 1f;
            scrollLayout.minHeight = 300f;

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(scrollHost.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            MenuUiBuilder.StretchRectToFill(viewportRect);
            viewport.AddComponent<RectMask2D>();

            GameObject listContent = new GameObject("Content", typeof(RectTransform));
            listContent.transform.SetParent(viewport.transform, false);
            RectTransform listRect = listContent.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0f, 1f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.anchoredPosition = Vector2.zero;
            listRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup listLayout = listContent.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 8f;
            listLayout.childAlignment = TextAnchor.UpperLeft;
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            listContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollHost.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.viewport = viewportRect;
            scroll.content = listRect;

            listParent = listContent.transform;
        }

        private bool BuildListIfNeeded()
        {
            return listParent != null;
        }

        private void RepopulateList()
        {
            if (listParent == null)
                return;

            for (int i = listParent.childCount - 1; i >= 0; i--)
                Destroy(listParent.GetChild(i).gameObject);

            if (craftingManager == null)
                craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();

            if (craftingManager == null)
            {
                CreateInfoRow("Crafting is not available yet.");
                if (pendingSummaryText != null)
                    pendingSummaryText.text = string.Empty;
                return;
            }

            IReadOnlyList<string> pending = craftingManager.GetPendingRecipeScrolls();
            if (pendingSummaryText != null)
            {
                pendingSummaryText.text = pending != null && pending.Count > 0
                    ? $"{pending.Count} blueprint(s) waiting to be learned — check your Craft / Blueprints tab."
                    : string.Empty;
            }

            List<RecipeDefinition> discovered = new List<RecipeDefinition>(craftingManager.GetDiscoveredRecipes());
            if (discovered.Count == 0)
            {
                CreateInfoRow("No blueprints learned yet. Find one-time-use blueprints out in the world to unlock crafting.");
                return;
            }

            discovered.Sort((a, b) =>
            {
                int stationCompare = a.stationType.CompareTo(b.stationType);
                return stationCompare != 0
                    ? stationCompare
                    : string.Compare(a.displayName, b.displayName, System.StringComparison.OrdinalIgnoreCase);
            });

            CraftingStationType? currentStation = null;
            for (int i = 0; i < discovered.Count; i++)
            {
                RecipeDefinition recipe = discovered[i];
                if (recipe == null)
                    continue;

                if (currentStation != recipe.stationType)
                {
                    currentStation = recipe.stationType;
                    CreateStationHeaderRow(recipe.stationType);
                }

                CreateRecipeRow(recipe);
            }
        }

        private void CreateStationHeaderRow(CraftingStationType stationType)
        {
            ShiftUiTheme theme = ShiftUiTheme.Current;
            TextMeshProUGUI header = CreateLabel(listParent, theme, stationType.ToString(), 20f, FontStyles.Bold);
            header.color = SurvivalPioneerUiPalette.Gold;
        }

        private void CreateInfoRow(string message)
        {
            ShiftUiTheme theme = ShiftUiTheme.Current;
            TextMeshProUGUI label = CreateLabel(listParent, theme, message, 18f, FontStyles.Normal);
            label.color = SurvivalPioneerUiPalette.MutedText;
        }

        private void CreateRecipeRow(RecipeDefinition recipe)
        {
            GameObject row = new GameObject($"Recipe_{recipe.ResolvedId}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            row.transform.SetParent(listParent, false);

            Image background = row.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(background);
            background.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.CharcoalGray, 0.95f);

            LayoutElement rowLayout = row.GetComponent<LayoutElement>();
            rowLayout.minHeight = 64f;
            rowLayout.preferredHeight = 64f;

            ShiftUiTheme theme = ShiftUiTheme.Current;

            GameObject nameObject = new GameObject("Name", typeof(RectTransform));
            nameObject.transform.SetParent(row.transform, false);
            TextMeshProUGUI nameLabel = nameObject.AddComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(nameLabel, semiBold: true);
            else
                TmpUiHelper.ApplyDefaultFont(nameLabel);
            nameLabel.fontSize = 18f;
            nameLabel.alignment = TextAlignmentOptions.TopLeft;
            nameLabel.color = SurvivalPioneerUiPalette.RichFuchsia;
            nameLabel.raycastTarget = false;
            RectTransform nameRect = nameLabel.rectTransform;
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(14f, 0f);
            nameRect.offsetMax = new Vector2(-14f, -6f);
            nameLabel.text = string.IsNullOrEmpty(recipe.displayName) ? recipe.ResolvedId : recipe.displayName;

            GameObject detailObject = new GameObject("Detail", typeof(RectTransform));
            detailObject.transform.SetParent(row.transform, false);
            TextMeshProUGUI detailLabel = detailObject.AddComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(detailLabel);
            else
                TmpUiHelper.ApplyDefaultFont(detailLabel);
            detailLabel.fontSize = 14f;
            detailLabel.alignment = TextAlignmentOptions.BottomLeft;
            detailLabel.color = SurvivalPioneerUiPalette.MutedText;
            detailLabel.raycastTarget = false;
            RectTransform detailRect = detailLabel.rectTransform;
            detailRect.anchorMin = new Vector2(0f, 0f);
            detailRect.anchorMax = new Vector2(1f, 0.5f);
            detailRect.offsetMin = new Vector2(14f, 6f);
            detailRect.offsetMax = new Vector2(-14f, 0f);
            detailLabel.text = BuildIngredientSummary(recipe);
        }

        private static string BuildIngredientSummary(RecipeDefinition recipe)
        {
            StringBuilder builder = new StringBuilder();
            if (recipe.ingredients != null)
            {
                for (int i = 0; i < recipe.ingredients.Count; i++)
                {
                    RecipeIngredient ingredient = recipe.ingredients[i];
                    if (ingredient == null || ingredient.item == null)
                        continue;

                    if (builder.Length > 0)
                        builder.Append(", ");

                    builder.Append(ingredient.amount).Append('x').Append(' ').Append(ingredient.item.itemName);
                }
            }

            string outputName = recipe.outputItem != null ? recipe.outputItem.itemName : "?";
            string costs = builder.Length > 0 ? builder.ToString() : "no ingredients";
            return $"{costs}  →  {recipe.outputAmount}x {outputName}";
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, ShiftUiTheme theme, string text, float size, FontStyles style)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
            if (theme != null)
                theme.ApplyFont(label, semiBold: style == FontStyles.Bold);
            else
                TmpUiHelper.ApplyDefaultFont(label);
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.raycastTarget = false;
            return label;
        }
    }

    public sealed class PioneersFullscreenWindow : FullscreenUiWindow
    {
        private PioneerRosterPanelUI pioneerRosterPanelUi;

        public void Configure(PioneerRosterPanelUI rosterUi)
        {
            pioneerRosterPanelUi = rosterUi;
        }

        public override void OnShow()
        {
            if (pioneerRosterPanelUi == null)
                pioneerRosterPanelUi = FindAnyObjectByType<PioneerRosterPanelUI>();

            pioneerRosterPanelUi?.EmbedIn(contentArea);
        }

        public override void OnHide()
        {
            pioneerRosterPanelUi?.Unembed();
        }

        public override void Refresh()
        {
            pioneerRosterPanelUi?.Refresh();
        }
    }

    public sealed class CharacterFullscreenWindow : FullscreenUiWindow
    {
        private CharacterPanelUI characterPanelUi;

        public void Configure(CharacterPanelUI panel)
        {
            characterPanelUi = panel;
        }

        public override void OnShow()
        {
            if (characterPanelUi == null)
                characterPanelUi = FindAnyObjectByType<CharacterPanelUI>();

            characterPanelUi?.EmbedIn(contentArea);
        }

        public override void OnHide()
        {
            characterPanelUi?.Unembed();
        }

        public override void Refresh()
        {
            characterPanelUi?.Refresh();
        }
    }

    public sealed class SkillsFullscreenWindow : FullscreenUiWindow
    {
        private SkillsPanelUI skillsPanelUi;

        public void Configure(SkillsPanelUI panel)
        {
            skillsPanelUi = panel;
        }

        public override void OnShow()
        {
            if (skillsPanelUi == null)
                skillsPanelUi = FindAnyObjectByType<SkillsPanelUI>();

            skillsPanelUi?.EmbedIn(contentArea);
        }

        public override void OnHide()
        {
            skillsPanelUi?.Unembed();
        }

        public override void Refresh()
        {
            skillsPanelUi?.Refresh();
        }
    }

    public sealed class EchoesFullscreenWindow : FullscreenUiWindow
    {
        private EchoesPanelUI echoesPanelUi;

        public void Configure(EchoesPanelUI panel)
        {
            echoesPanelUi = panel;
        }

        public override void OnShow()
        {
            if (echoesPanelUi == null)
                echoesPanelUi = FindAnyObjectByType<EchoesPanelUI>();

            echoesPanelUi?.EmbedIn(contentArea);
        }

        public override void OnHide()
        {
            echoesPanelUi?.Unembed();
        }

        public override void Refresh()
        {
            echoesPanelUi?.Refresh();
        }
    }

    public sealed class AchievementsFullscreenWindow : FullscreenUiWindow
    {
        private AchievementsPanelUI achievementsPanelUi;

        public void Configure(AchievementsPanelUI panel)
        {
            achievementsPanelUi = panel;
        }

        public override void OnShow()
        {
            if (achievementsPanelUi == null)
                achievementsPanelUi = FindAnyObjectByType<AchievementsPanelUI>();

            achievementsPanelUi?.EmbedIn(contentArea);
        }

        public override void OnHide()
        {
            achievementsPanelUi?.Unembed();
        }

        public override void Refresh()
        {
            achievementsPanelUi?.Refresh();
        }
    }

    public sealed class MapFullscreenWindow : FullscreenUiWindow
    {
        private MapUI mapUi;

        public void Configure(MapUI map)
        {
            mapUi = map;
        }

        public override void OnShow()
        {
            if (mapUi == null)
                mapUi = FindAnyObjectByType<MapUI>();

            if (rootRect != null)
                rootRect.gameObject.SetActive(false);

            mapUi?.OpenMapFullscreen();
            GameplayHudVisibility.SetJournalTabHud(JournalWindowId.Map);
        }

        public override void OnHide()
        {
            mapUi?.CloseFullMapFromNavigator();
        }
    }

    public sealed class StubFullscreenWindow : FullscreenUiWindow
    {
        private string stubHeading;
        private string stubBody;
        private string[] featureBullets;

        public void Configure(string heading, string body, params string[] bullets)
        {
            stubHeading = heading;
            stubBody = body;
            featureBullets = bullets;
        }

        protected override void OnBuild()
        {
            if (contentArea == null)
                return;

            VerticalLayoutGroup layout = contentArea.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 32, 32);
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ShiftUiTheme theme = ShiftUiTheme.Current;

            GameObject iconBlock = new GameObject("IconBlock", typeof(RectTransform), typeof(Image));
            iconBlock.transform.SetParent(contentArea, false);
            Image iconImage = iconBlock.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(iconImage);
            iconImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.RichFuchsia, 0.55f);
            LayoutElement iconLayout = iconBlock.AddComponent<LayoutElement>();
            iconLayout.minHeight = 96f;
            iconLayout.preferredHeight = 96f;
            iconLayout.minWidth = 96f;
            iconLayout.preferredWidth = 96f;

            TextMeshProUGUI heading = CreateStubText(contentArea, stubHeading ?? "Coming Soon", 32f, FontStyles.Bold, TextAlignmentOptions.TopLeft, theme);
            heading.color = SurvivalPioneerUiPalette.BodyText;

            TextMeshProUGUI body = CreateStubText(
                contentArea,
                stubBody ?? string.Empty,
                20f,
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                theme);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.color = theme != null ? theme.secondaryTextColor : SurvivalPioneerUiPalette.BodyText;

            if (featureBullets != null && featureBullets.Length > 0)
            {
                GameObject bulletList = new GameObject("FeatureBullets", typeof(RectTransform));
                bulletList.transform.SetParent(contentArea, false);
                VerticalLayoutGroup bulletLayout = bulletList.AddComponent<VerticalLayoutGroup>();
                bulletLayout.spacing = 8f;
                bulletLayout.childAlignment = TextAnchor.UpperLeft;
                bulletLayout.childControlWidth = true;
                bulletLayout.childForceExpandWidth = true;
                bulletLayout.childForceExpandHeight = false;
                LayoutElement bulletListLayout = bulletList.AddComponent<LayoutElement>();
                bulletListLayout.flexibleHeight = 1f;

                for (int i = 0; i < featureBullets.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(featureBullets[i]))
                        continue;

                    TextMeshProUGUI bullet = CreateStubText(
                        bulletList.transform,
                        $"\u2022 {featureBullets[i]}",
                        17f,
                        FontStyles.Normal,
                        TextAlignmentOptions.TopLeft,
                        theme);
                    bullet.color = theme != null ? theme.secondaryTextColor : SurvivalPioneerUiPalette.MutedText;
                }
            }

            TextMeshProUGUI footer = CreateStubText(
                contentArea,
                "Coming in a future update",
                15f,
                FontStyles.Italic,
                TextAlignmentOptions.Center,
                theme);
            footer.color = SurvivalPioneerUiPalette.MutedText;
        }

        private static TextMeshProUGUI CreateStubText(
            Transform parent,
            string value,
            float size,
            FontStyles style,
            TextAlignmentOptions alignment,
            ShiftUiTheme theme)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            TmpUiHelper.ApplyDefaultFont(text);
            if (theme != null)
                theme.ApplyFont(text, semiBold: style == FontStyles.Bold);
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }
    }
}

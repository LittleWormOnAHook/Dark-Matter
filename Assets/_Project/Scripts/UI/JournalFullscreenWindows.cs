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

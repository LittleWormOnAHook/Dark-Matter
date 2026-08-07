using System.Collections.Generic;
using Project.Core;
using Project.Crafting;
using Project.Pioneers;
using Project.Player;
using Project.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Project.UI
{
  public class JournalPanelUI : MonoBehaviour
  {
    private const float UiScale = 0.92f;

    private GameObject overlayRoot;
    private RectTransform windowHostRect;
    private JournalTabRail tabRail;
    private FullscreenUiNavigator navigator;
    private Transform questListParent;
    private TextMeshProUGUI questDetailTitle;
    private TextMeshProUGUI questDetailBody;
    private Transform objectiveListParent;
    private Button abandonQuestButton;
    private TextMeshProUGUI abandonQuestButtonLabel;
    private bool abandonConfirmPending;

    private enum JournalContentSection
    {
        Quests,
        Chronicle,
        SystemLogs
    }

    private JournalContentSection journalSection = JournalContentSection.Quests;
    private Transform journalSectionTabParent;
    private GameObject questSplitRoot;
    private GameObject chronicleRoot;
    private GameObject systemLogsRoot;
    private Transform chronicleListParent;
    private Transform systemLogListParent;
    private PioneerRosterManager journalRoster;

    private string selectedQuestId;
    private bool uiBuilt;
    private int lastToggleFrame = -1;

    private InventoryUI inventoryUi;
    private PioneerRosterPanelUI pioneerRosterPanelUi;
    private CharacterPanelUI characterPanelUi;
    private SkillsPanelUI skillsPanelUi;
    private EchoesPanelUI echoesPanelUi;
    private AchievementsPanelUI achievementsPanelUi;
    private CraftingUI craftingUi;
    private MapUI mapUi;
    private PetUI petUi;
    private QuestManager questManager;
    private CraftingManager craftingManager;

    private void Awake()
    {
      MenuUiBuilder.StretchRectToFill(GetComponent<RectTransform>());
    }

    private void Start()
    {
      inventoryUi = FindAnyObjectByType<InventoryUI>();
      pioneerRosterPanelUi = GetComponent<PioneerRosterPanelUI>();
      if (pioneerRosterPanelUi == null)
        pioneerRosterPanelUi = gameObject.AddComponent<PioneerRosterPanelUI>();
      characterPanelUi = GetComponent<CharacterPanelUI>();
      if (characterPanelUi == null)
        characterPanelUi = gameObject.AddComponent<CharacterPanelUI>();
      skillsPanelUi = GetComponent<SkillsPanelUI>();
      if (skillsPanelUi == null)
        skillsPanelUi = gameObject.AddComponent<SkillsPanelUI>();
      echoesPanelUi = GetComponent<EchoesPanelUI>();
      if (echoesPanelUi == null)
        echoesPanelUi = gameObject.AddComponent<EchoesPanelUI>();
      achievementsPanelUi = GetComponent<AchievementsPanelUI>();
      if (achievementsPanelUi == null)
        achievementsPanelUi = gameObject.AddComponent<AchievementsPanelUI>();
      craftingUi = FindAnyObjectByType<CraftingUI>();
      mapUi = FindAnyObjectByType<MapUI>();
      petUi = FindAnyObjectByType<PetUI>();
      questManager = FindAnyObjectByType<QuestManager>();
      craftingManager = FindAnyObjectByType<CraftingManager>();

      if (craftingManager == null)
      {
        GameObject player = PlayerLocator.FindPlayerObject();
        if (player != null)
          craftingManager = player.GetComponent<CraftingManager>() ?? player.gameObject.AddComponent<CraftingManager>();
      }

      if (craftingUi == null)
        craftingUi = gameObject.AddComponent<CraftingUI>();

      if (questManager == null)
      {
        GameObject player = PlayerLocator.FindPlayerObject();
        if (player != null)
          questManager = QuestManager.EnsureExists();
      }

      EnsureUiBuilt();
      navigator?.CloseAll();

      if (questManager != null)
      {
        questManager.OnQuestUpdated += HandleQuestUpdated;
        questManager.OnQuestCompleted += HandleQuestUpdated;
      }

      RefreshQuestList();
    }

    private void OnDestroy()
    {
      if (navigator != null)
      {
        navigator.OnPauseGameplayChanged -= HandleNavigatorPauseChanged;
        navigator.OnActiveWindowChanged -= HandleActiveWindowChanged;
      }

      if (questManager != null)
      {
        questManager.OnQuestUpdated -= HandleQuestUpdated;
        questManager.OnQuestCompleted -= HandleQuestUpdated;
      }
    }

    public void OnToggleJournal(InputAction.CallbackContext context)
    {
      if (!context.performed)
        return;

      TryToggleJournal();
    }

    public bool IsOpen => navigator != null && navigator.IsAnyOpen;

    public void TogglePanel()
    {
      TryToggleJournal();
    }

    public bool TryToggleJournal()
    {
      if (!GameSession.HasStarted)
        return false;

      if (Time.frameCount == lastToggleFrame)
        return false;

      if (!EnsureNavigatorReady())
        return false;

      lastToggleFrame = Time.frameCount;

      if (navigator.IsAnyOpen)
      {
        ReleaseInputCapture();
        return true;
      }

      CloseConflictingPanels();
      navigator.SwitchToWindow(JournalWindowId.JournalQuest);
      ItemHoverTooltip.HideAny();
      RecipeHoverTooltip.HideAny();
      UiFrontLayer.BringLayerToFront(transform);
      return true;
    }

    public bool TryToggleTab(JournalWindowId windowId)
    {
      if (!GameSession.HasStarted)
        return false;

      if (Time.frameCount == lastToggleFrame)
        return false;

      if (!EnsureNavigatorReady())
        return false;

      lastToggleFrame = Time.frameCount;

      if (navigator.IsAnyOpen && navigator.CurrentWindow == windowId)
      {
        ReleaseInputCapture();
        return true;
      }

      CloseConflictingPanels();
      navigator.SwitchToWindow(windowId);
      ItemHoverTooltip.HideAny();
      RecipeHoverTooltip.HideAny();
      UiFrontLayer.BringLayerToFront(transform);
      return true;
    }

    public bool TryToggleMapTab() => TryToggleTab(JournalWindowId.Map);

    private bool EnsureNavigatorReady()
    {
      if (uiBuilt && navigator == null)
        uiBuilt = false;

      EnsureUiBuilt();
      if (navigator != null && uiBuilt)
        return true;

      Debug.LogError("[JournalPanelUI] Journal navigator is unavailable. UI build may have failed.");
      return false;
    }

    public void OpenToInventoryTab() => TryToggleTab(JournalWindowId.Inventory);

    public void OpenToMap() => TryToggleTab(JournalWindowId.Map);

    public void OpenToPetTab() => TryToggleTab(JournalWindowId.Pet);

    public void OpenToPioneersTab() => TryToggleTab(JournalWindowId.Pioneers);

    public void OpenToCharacterTab() => TryToggleTab(JournalWindowId.Character);

    public void OpenToBlueprintsTab() => TryToggleTab(JournalWindowId.Recipes);
    /// <summary>Obsolete alias for OpenToBlueprintsTab.</summary>
    public void OpenToRecipesTab() => OpenToBlueprintsTab();

    public void OpenToSkillsTab() => TryToggleTab(JournalWindowId.Skills);

    public void OpenToEchoesTab() => TryToggleTab(JournalWindowId.Echoes);

    public void OpenToAchievementsTab() => TryToggleTab(JournalWindowId.Achievements);

    public void OpenToCraftTab(CraftingStationType? station)
    {
      if (craftingUi == null)
        craftingUi = FindAnyObjectByType<CraftingUI>() ?? gameObject.AddComponent<CraftingUI>();

      if (!station.HasValue)
      {
        OpenToBlueprintsTab();
        return;
      }

      // Production craft opens as a popup at the world station — not a journal Craft tab.
      navigator?.CloseAll();
      craftingUi.OpenStationCraftingPopup(station.Value);
    }

    public static void CloseAnyOpenJournal()
    {
      JournalPanelUI journal = FindAnyObjectByType<JournalPanelUI>();
      journal?.ReleaseInputCapture();
    }

    public void ReleaseInputCapture()
    {
      navigator?.CloseAll();
      EnsureInventoryUi()?.RestoreInventoryPanel();
      craftingUi?.CloseStandalonePanel(clearStation: true);

      if (windowHostRect != null)
        windowHostRect.gameObject.SetActive(false);

      PlayerController player = FindAnyObjectByType<PlayerController>();
      if (player != null)
        player.SetJournalOpen(false);

      CameraController camera = FindAnyObjectByType<CameraController>();
      if (camera != null)
        camera.SetJournalOpen(false);

      GameplayInputRecovery.FinalizeGameplayInput();
    }

    private InventoryUI EnsureInventoryUi()
    {
      if (inventoryUi == null)
        inventoryUi = FindAnyObjectByType<InventoryUI>();
      return inventoryUi;
    }

    private void CloseConflictingPanels()
    {
      // Inventory is journal-only; InventoryFullscreenWindow OnShow/OnHide owns embed lifecycle.
      MapUI.CloseAnyOpenMap();
      PetUI.CloseAnyOpenPet();
      PioneerRosterContextMenu.HideAny();
      PetContextMenu.HideAny();
      PetHoverTooltip.HideAny();
      PioneerHoverTooltip.HideAny();
    }

    public void EnsureUiBuiltForLayoutEditor()
    {
      EnsureUiBuilt();
      if (uiBuilt)
      {
        ApplySavedLayoutProfiles();
        EnforceJournalChromeLayout();
        RefreshTabRailVisualState();
      }
    }

    public void ResetToDefaultLayout()
    {
      if (navigator != null && navigator.IsAnyOpen)
        ReleaseInputCapture();

      uiBuilt = false;
      EnsureUiBuilt();
    }

    private void EnsureUiBuilt()
    {
      if (uiBuilt)
        return;

      CleanupPartialUi();

      try
      {
        BuildUi();
        if (navigator == null || navigator.GetWindowCount() == 0)
          throw new System.InvalidOperationException("[JournalPanelUI] Journal UI build did not register any windows.");

        uiBuilt = true;
        ApplySavedLayoutProfiles();
        EnforceJournalChromeLayout();
        RefreshTabRailVisualState();
      }
      catch (System.Exception ex)
      {
        Debug.LogException(ex);
        CleanupPartialUi();
      }
    }

    private void ApplySavedLayoutProfiles()
    {
      ApplyLayoutProfile(overlayRoot != null ? overlayRoot.transform : null, UiPanelIds.JournalOverlay);
      ApplyLayoutProfile(tabRail != null ? tabRail.transform : null, UiPanelIds.JournalTabRail);
      ApplyLayoutProfile(windowHostRect, UiPanelIds.JournalWindowHost);
    }

    private static void ApplyLayoutProfile(Transform root, string panelId)
    {
      if (root == null || string.IsNullOrEmpty(panelId))
        return;

      UiLayoutProfile profile = UiLayoutProfileResolver.Load(panelId);
      if (profile == null)
        return;

      UiLayoutProfileApplier.Apply(root, profile);
    }

    private void RefreshTabRailVisualState()
    {
      tabRail?.SetActiveTab(navigator != null && navigator.IsAnyOpen ? navigator.CurrentWindow : null);
    }

    private void CleanupPartialUi()
    {
      if (navigator != null)
      {
        navigator.OnPauseGameplayChanged -= HandleNavigatorPauseChanged;
        navigator.OnActiveWindowChanged -= HandleActiveWindowChanged;
        navigator.CloseAll();
      }

      if (navigator != null)
        Destroy(navigator.gameObject);
      else if (windowHostRect != null)
        Destroy(windowHostRect.gameObject);

      if (overlayRoot != null)
        Destroy(overlayRoot);

      if (tabRail != null)
        Destroy(tabRail.gameObject);

      overlayRoot = null;
      windowHostRect = null;
      tabRail = null;
      navigator = null;
      questListParent = null;
      questDetailTitle = null;
      questDetailBody = null;
      objectiveListParent = null;
      journalSectionTabParent = null;
      questSplitRoot = null;
      chronicleRoot = null;
      systemLogsRoot = null;
      chronicleListParent = null;
      systemLogListParent = null;
    }

    private void BuildUi()
    {
      overlayRoot = MenuUiBuilder.CreateFullScreenPanel(transform, "JournalOverlay", new Color(0f, 0f, 0f, 0.55f), blockRaycasts: true);
      overlayRoot.SetActive(false);

      GameObject windowHostObject = new GameObject("JournalWindowHost", typeof(RectTransform));
      windowHostObject.transform.SetParent(transform, false);
      windowHostRect = windowHostObject.GetComponent<RectTransform>();
      windowHostRect.anchorMin = Vector2.zero;
      windowHostRect.anchorMax = Vector2.one;
      windowHostRect.offsetMin = Vector2.zero;
      windowHostRect.offsetMax = new Vector2(0f, -Sc(JournalTabRail.RailHeight));

      navigator = FullscreenUiNavigator.EnsureExists(windowHostRect);
      if (navigator == null)
        throw new System.InvalidOperationException("[JournalPanelUI] Failed to create FullscreenUiNavigator.");

      navigator.OnPauseGameplayChanged += HandleNavigatorPauseChanged;
      navigator.OnActiveWindowChanged += HandleActiveWindowChanged;

      GameObject tabRailObject = new GameObject("JournalTabRailHost", typeof(RectTransform));
      tabRail = tabRailObject.AddComponent<JournalTabRail>();
      tabRail.Build(transform, UiScale, HandleTabSelected);

      RegisterWindow<JournalQuestFullscreenWindow>(JournalWindowId.JournalQuest, "Journal", quest =>
      {
        quest.Configure(this);
      });

      RegisterWindow<InventoryFullscreenWindow>(JournalWindowId.Inventory, "Inventory", inv =>
      {
        inv.Configure(inventoryUi ?? FindAnyObjectByType<InventoryUI>());
      });

      RegisterWindow<MapFullscreenWindow>(JournalWindowId.Map, "Map", map =>
      {
        map.Configure(mapUi ?? FindAnyObjectByType<MapUI>());
      });

      RegisterWindow<PetFullscreenWindow>(JournalWindowId.Pet, "Pet", pet =>
      {
        if (petUi == null)
          petUi = FindAnyObjectByType<PetUI>() ?? gameObject.AddComponent<PetUI>();
        pet.Configure(petUi);
      });

      RegisterWindow<RecipeLibraryFullscreenWindow>(JournalWindowId.Recipes, "Blueprints", recipes =>
      {
        recipes.Configure(craftingUi ?? FindAnyObjectByType<CraftingUI>() ?? gameObject.AddComponent<CraftingUI>());
      });

      // Kept for API compatibility; journal rail no longer shows a Craft tab.
      // Station interaction opens CraftingUI.OpenStationCraftingPopup instead.
      RegisterWindow<CraftFullscreenWindow>(JournalWindowId.Craft, "Craft", craft =>
      {
        craft.Configure(craftingUi ?? FindAnyObjectByType<CraftingUI>() ?? gameObject.AddComponent<CraftingUI>());
      });

      RegisterWindow<PioneersFullscreenWindow>(JournalWindowId.Pioneers, "Companions", pioneers =>
      {
        pioneers.Configure(pioneerRosterPanelUi ?? GetComponent<PioneerRosterPanelUI>() ?? gameObject.AddComponent<PioneerRosterPanelUI>());
      });

      RegisterWindow<CharacterFullscreenWindow>(JournalWindowId.Character, "Character", character =>
      {
        character.Configure(characterPanelUi ?? GetComponent<CharacterPanelUI>() ?? gameObject.AddComponent<CharacterPanelUI>());
      });

      RegisterWindow<SkillsFullscreenWindow>(JournalWindowId.Skills, "Skills", skills =>
      {
        skills.Configure(skillsPanelUi ?? GetComponent<SkillsPanelUI>() ?? gameObject.AddComponent<SkillsPanelUI>());
      });

      RegisterWindow<EchoesFullscreenWindow>(JournalWindowId.Echoes, "Echoes", echoes =>
      {
        echoes.Configure(echoesPanelUi ?? GetComponent<EchoesPanelUI>() ?? gameObject.AddComponent<EchoesPanelUI>());
      });

      RegisterWindow<AchievementsFullscreenWindow>(JournalWindowId.Achievements, "Achievements", achievements =>
      {
        achievements.Configure(achievementsPanelUi ?? GetComponent<AchievementsPanelUI>() ?? gameObject.AddComponent<AchievementsPanelUI>());
      });
    }

    private void RegisterWindow<T>(JournalWindowId id, string title, System.Action<T> configure)
      where T : FullscreenUiWindow
    {
      GameObject host = new GameObject(id + "WindowHost", typeof(RectTransform));
      host.transform.SetParent(windowHostRect != null ? windowHostRect : transform, false);
      StretchRectToParent(host.GetComponent<RectTransform>());

      T window = host.AddComponent<T>();
      configure?.Invoke(window);
      window.Initialize(navigator, id, title, SurvivalPioneerUiPalette.PanelBackground);
      navigator.RegisterWindow(window);
    }

    public void BringJournalChromeToFront()
    {
      if (!IsOpen)
        return;

      transform.SetAsLastSibling();
      if (overlayRoot != null)
        overlayRoot.SetActive(true);
      if (tabRail != null)
        tabRail.gameObject.SetActive(true);

      ApplyJournalChromeSortOrder();
    }

    private void ApplyJournalChromeSortOrder()
    {
      // Bottom â†’ top: dim overlay, window content, top tab bar (always receives clicks).
      overlayRoot?.transform.SetAsLastSibling();
      windowHostRect?.transform.SetAsLastSibling();
      tabRail?.transform.SetAsLastSibling();
    }

    private void EnforceJournalChromeLayout()
    {
      float railHeight = Sc(JournalTabRail.RailHeight);

      if (windowHostRect != null)
      {
        windowHostRect.anchorMin = Vector2.zero;
        windowHostRect.anchorMax = Vector2.one;
        windowHostRect.offsetMin = Vector2.zero;
        windowHostRect.offsetMax = new Vector2(0f, -railHeight);

        if (windowHostRect.TryGetComponent(out Image hostImage))
        {
          hostImage.raycastTarget = false;
          if (hostImage.color.a > 0.01f)
            hostImage.color = new Color(hostImage.color.r, hostImage.color.g, hostImage.color.b, 0f);
        }
      }

      if (tabRail != null)
      {
        RectTransform railRect = tabRail.GetComponent<RectTransform>();
        if (railRect != null)
        {
          railRect.anchorMin = new Vector2(0f, 1f);
          railRect.anchorMax = new Vector2(1f, 1f);
          railRect.pivot = new Vector2(0.5f, 1f);
          railRect.anchoredPosition = Vector2.zero;
          railRect.sizeDelta = new Vector2(0f, railHeight);
        }

        if (tabRail.TryGetComponent(out Image railImage) && railImage.color.a < 0.05f)
          railImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.DarkNavy, 0.96f);
      }

      if (overlayRoot != null && overlayRoot.TryGetComponent(out Image overlayImage))
      {
        if (overlayImage.color.a < 0.05f)
          overlayImage.color = new Color(0f, 0f, 0f, 0.55f);
      }
    }

    private void HandleNavigatorPauseChanged(bool paused)
    {
      if (overlayRoot != null)
        overlayRoot.SetActive(paused);

      if (tabRail != null)
        tabRail.gameObject.SetActive(paused);

      if (paused)
      {
        if (windowHostRect != null)
          windowHostRect.gameObject.SetActive(true);

        ApplyJournalChromeSortOrder();
        HandleActiveWindowChanged(navigator?.CurrentWindow);
        UpdateJournalOverlayInputBlocking(navigator?.CurrentWindow);
        GameplayHudVisibility.SetJournalTabHud(navigator?.CurrentWindow);
        ItemHoverTooltip.HideAny();
        RecipeHoverTooltip.HideAny();
        InventoryContextMenu.Instance?.Hide();
        PioneerRosterContextMenu.HideAny();
      PetContextMenu.HideAny();
      PetHoverTooltip.HideAny();
      PioneerHoverTooltip.HideAny();
      }
      else
      {
        EnsureInventoryUi()?.RestoreInventoryPanel();

        if (windowHostRect != null)
          windowHostRect.gameObject.SetActive(false);

        GameplayHudVisibility.RefreshGameplayHud();
        PioneerRosterContextMenu.HideAny();
      PetContextMenu.HideAny();
      PetHoverTooltip.HideAny();
      PioneerHoverTooltip.HideAny();
        if (craftingManager == null)
          craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();
        if (craftingManager != null)
          craftingManager.CurrentStation = null;
      }
    }

    private void HandleActiveWindowChanged(JournalWindowId? windowId)
    {
      tabRail?.SetActiveTab(windowId);
      UpdateJournalOverlayInputBlocking(windowId);
      if (navigator != null && navigator.IsAnyOpen)
        GameplayHudVisibility.SetJournalTabHud(windowId);
    }

    private void UpdateJournalOverlayInputBlocking(JournalWindowId? windowId)
    {
      if (overlayRoot == null)
        return;

      Image overlayImage = overlayRoot.GetComponent<Image>();
      if (overlayImage == null)
        return;

      // Full map pan needs pointer events to reach MapViewportPanHandler beneath the journal chrome.
      overlayImage.raycastTarget = windowId != JournalWindowId.Map;
    }

    private void HandleTabSelected(JournalWindowId windowId)
    {
      if (!EnsureNavigatorReady())
        return;

      if (navigator.CurrentWindow == windowId)
        return;

      CloseConflictingPanels();
      navigator.SwitchToWindow(windowId);
      ItemHoverTooltip.HideAny();
      RecipeHoverTooltip.HideAny();
      UiFrontLayer.BringLayerToFront(transform);
    }

    public void BuildQuestWindowContent(RectTransform parent)
    {
      if (parent == null)
        return;

      for (int i = parent.childCount - 1; i >= 0; i--)
        Destroy(parent.GetChild(i).gameObject);

      ShiftUiTheme theme = ShiftUiTheme.Current;
      journalRoster = PioneerRosterManager.EnsureExists();

      VerticalLayoutGroup rootLayout = parent.gameObject.GetComponent<VerticalLayoutGroup>();
      if (rootLayout == null)
        rootLayout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
      rootLayout.spacing = Sc(JournalPanelLayout.SectionSpacing);
      rootLayout.padding = new RectOffset(
        Sc((int)JournalPanelLayout.PanelPadding),
        Sc((int)JournalPanelLayout.PanelPadding),
        Sc((int)JournalPanelLayout.PanelPadding),
        Sc((int)JournalPanelLayout.PanelPadding));
      rootLayout.childControlWidth = true;
      rootLayout.childControlHeight = true;
      rootLayout.childForceExpandWidth = true;
      rootLayout.childForceExpandHeight = false;

      GameObject tabRow = new GameObject("JournalSectionTabs", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
      tabRow.transform.SetParent(parent, false);
      LayoutElement tabRowLayout = tabRow.GetComponent<LayoutElement>();
      tabRowLayout.minHeight = Sc(32f);
      tabRowLayout.preferredHeight = Sc(32f);
      HorizontalLayoutGroup tabLayout = tabRow.GetComponent<HorizontalLayoutGroup>();
      tabLayout.spacing = Sc(4f);
      tabLayout.childAlignment = TextAnchor.MiddleLeft;
      tabLayout.childControlWidth = false;
      tabLayout.childForceExpandWidth = false;
      journalSectionTabParent = tabRow.transform;

      CreateJournalSectionTab("Quests", JournalContentSection.Quests, theme);
      CreateJournalSectionTab("Chronicle", JournalContentSection.Chronicle, theme);
      CreateJournalSectionTab("System Logs", JournalContentSection.SystemLogs, theme);

      GameObject contentHost = new GameObject("JournalContentHost", typeof(RectTransform), typeof(LayoutElement));
      contentHost.transform.SetParent(parent, false);
      LayoutElement contentHostLayout = contentHost.GetComponent<LayoutElement>();
      contentHostLayout.flexibleHeight = 1f;
      contentHostLayout.minHeight = Sc(320f);

      questSplitRoot = new GameObject("QuestSplit", typeof(RectTransform));
      questSplitRoot.transform.SetParent(contentHost.transform, false);
      StretchRectToParent(questSplitRoot.GetComponent<RectTransform>());
      HorizontalLayoutGroup splitLayout = questSplitRoot.AddComponent<HorizontalLayoutGroup>();
      splitLayout.spacing = Sc(JournalPanelLayout.SectionSpacing);
      splitLayout.childControlWidth = true;
      splitLayout.childControlHeight = true;
      splitLayout.childForceExpandHeight = true;

      GameObject listColumn = new GameObject("QuestListColumn", typeof(RectTransform));
      listColumn.transform.SetParent(questSplitRoot.transform, false);
      LayoutElement listLayout = listColumn.AddComponent<LayoutElement>();
      listLayout.flexibleWidth = 0.45f;
      listLayout.minWidth = Sc(340f);
      listLayout.flexibleHeight = 1f;
      CreateQuestListColumn(listColumn.transform, theme);

      GameObject detailColumn = new GameObject("QuestDetailColumn", typeof(RectTransform));
      detailColumn.transform.SetParent(questSplitRoot.transform, false);
      LayoutElement detailLayout = detailColumn.AddComponent<LayoutElement>();
      detailLayout.flexibleWidth = 0.55f;
      detailLayout.flexibleHeight = 1f;
      CreateQuestDetailColumn(detailColumn.transform, theme);

      chronicleRoot = CreateLogScrollPanel(contentHost.transform, "ChronicleScroll", out chronicleListParent);
      systemLogsRoot = CreateLogScrollPanel(contentHost.transform, "SystemLogsScroll", out systemLogListParent);

      ApplyJournalSectionVisibility();
      RefreshJournalSectionTabs();
    }

    private void CreateJournalSectionTab(string label, JournalContentSection section, ShiftUiTheme theme)
    {
      GameObject tab = new GameObject(label + "Tab", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
      tab.transform.SetParent(journalSectionTabParent, false);
      LayoutElement layout = tab.GetComponent<LayoutElement>();
      layout.minWidth = Sc(110f);
      layout.preferredHeight = Sc(28f);

      Image bg = tab.GetComponent<Image>();
      MenuUiBuilder.ApplyUiSprite(bg);

      Button button = tab.GetComponent<Button>();
      button.targetGraphic = bg;
      JournalContentSection captured = section;
      button.onClick.RemoveAllListeners();
      button.onClick.AddListener(() =>
      {
        journalSection = captured;
        ApplyJournalSectionVisibility();
        RefreshJournalSectionTabs();
        RefreshQuestList();
      });

      GameObject labelObj = new GameObject("Label", typeof(RectTransform));
      labelObj.transform.SetParent(tab.transform, false);
      TextMeshProUGUI tmp = labelObj.AddComponent<TextMeshProUGUI>();
      ApplyFont(tmp, theme, semiBold: true);
      tmp.text = label;
      tmp.fontSize = Sc(JournalPanelLayout.ButtonFontSize + 1f);
      tmp.alignment = TextAlignmentOptions.Center;
      tmp.color = SurvivalPioneerUiPalette.BodyText;
      tmp.raycastTarget = false;
      StretchRectToParent(labelObj.GetComponent<RectTransform>());
    }

    private GameObject CreateLogScrollPanel(Transform parent, string name, out Transform listParent)
    {
      GameObject scrollObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
      scrollObj.transform.SetParent(parent, false);
      StretchRectToParent(scrollObj.GetComponent<RectTransform>());
      Image scrollBg = scrollObj.GetComponent<Image>();
      JournalPanelLayout.StyleScrollBackground(scrollBg);

      GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
      viewport.transform.SetParent(scrollObj.transform, false);
      RectTransform viewportRt = viewport.GetComponent<RectTransform>();
      StretchRectToParent(viewportRt);
      viewportRt.offsetMin = new Vector2(Sc(4f), Sc(4f));
      viewportRt.offsetMax = new Vector2(-Sc(4f), -Sc(4f));

      GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
      content.transform.SetParent(viewport.transform, false);
      RectTransform contentRt = content.GetComponent<RectTransform>();
      contentRt.anchorMin = new Vector2(0f, 1f);
      contentRt.anchorMax = new Vector2(1f, 1f);
      contentRt.pivot = new Vector2(0.5f, 1f);
      contentRt.anchoredPosition = Vector2.zero;
      contentRt.sizeDelta = Vector2.zero;
      VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
      JournalPanelLayout.ApplyListVerticalLayout(contentLayout);
      content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

      ScrollRect scroll = scrollObj.GetComponent<ScrollRect>();
      scroll.viewport = viewportRt;
      scroll.content = contentRt;
      scroll.horizontal = false;
      scroll.vertical = true;
      listParent = content.transform;
      scrollObj.SetActive(false);
      return scrollObj;
    }

    private void ApplyJournalSectionVisibility()
    {
      if (questSplitRoot != null)
        questSplitRoot.SetActive(journalSection == JournalContentSection.Quests);
      if (chronicleRoot != null)
        chronicleRoot.SetActive(journalSection == JournalContentSection.Chronicle);
      if (systemLogsRoot != null)
        systemLogsRoot.SetActive(journalSection == JournalContentSection.SystemLogs);
    }

    private void RefreshJournalSectionTabs()
    {
      if (journalSectionTabParent == null)
        return;

      for (int i = 0; i < journalSectionTabParent.childCount; i++)
      {
        Transform child = journalSectionTabParent.GetChild(i);
        Image bg = child.GetComponent<Image>();
        TextMeshProUGUI label = child.GetComponentInChildren<TextMeshProUGUI>();
        bool active =
          (journalSection == JournalContentSection.Quests && child.name.StartsWith("Quests")) ||
          (journalSection == JournalContentSection.Chronicle && child.name.StartsWith("Chronicle")) ||
          (journalSection == JournalContentSection.SystemLogs && child.name.StartsWith("System"));

        if (bg != null)
          bg.color = active
            ? SurvivalPioneerUiPalette.ActiveTabBackground
            : SurvivalPioneerUiPalette.InactiveTabBackground;
        if (label != null)
          label.color = active ? SurvivalPioneerUiPalette.Gold : SurvivalPioneerUiPalette.BodyText;
      }
    }

    public void RefreshQuestList()
    {
      if (journalSection == JournalContentSection.Quests)
        RefreshQuestListParents(questListParent, questDetailTitle, questDetailBody);
      else if (journalSection == JournalContentSection.Chronicle)
        RefreshChronicleList();
      else
        RefreshSystemLogList();
    }

    private void RefreshChronicleList()
    {
      if (chronicleListParent == null)
        return;

      foreach (Transform child in chronicleListParent)
        Destroy(child.gameObject);

      journalRoster ??= PioneerRosterManager.EnsureExists();
      if (journalRoster == null || journalRoster.EchoChronicle.Count == 0)
      {
        JournalPanelLayout.CreateEmptyStateCard(
          chronicleListParent,
          ShiftUiTheme.Current,
          "Rescue chronicle empty",
          "Successful and failed Neural Echo rescues are recorded here.",
          "Open the Echoes tab for live signals and dispositions.");
        return;
      }

      for (int i = 0; i < journalRoster.EchoChronicle.Count; i++)
      {
        EchoChronicleEntry entry = journalRoster.EchoChronicle[i];
        if (entry == null || entry.simulationIncident)
          continue;
        CreateJournalLogCard(
          chronicleListParent,
          entry.rescueFailed ? "Rescue Failed" : "Rescue Success",
          entry.echoName,
          $"{entry.classSummary}  ·  {entry.abilitySummary}",
          entry.rescueFailed ? SurvivalPioneerUiPalette.DangerRed : SurvivalPioneerUiPalette.PositiveGreen);
      }

      if (chronicleListParent.childCount == 0)
      {
        JournalPanelLayout.CreateEmptyStateCard(
          chronicleListParent,
          ShiftUiTheme.Current,
          "Rescue chronicle empty",
          "No rescue events yet — simulation logs may still appear under System Logs.");
      }
    }

    private void RefreshSystemLogList()
    {
      if (systemLogListParent == null)
        return;

      foreach (Transform child in systemLogListParent)
        Destroy(child.gameObject);

      journalRoster ??= PioneerRosterManager.EnsureExists();
      bool any = false;
      if (journalRoster != null)
      {
        for (int i = 0; i < journalRoster.EchoChronicle.Count; i++)
        {
          EchoChronicleEntry entry = journalRoster.EchoChronicle[i];
          if (entry == null || !entry.simulationIncident)
            continue;

          any = true;
          CreateJournalLogCard(
            systemLogListParent,
            "Colony Event",
            entry.echoName,
            $"{entry.classSummary}  ·  {entry.abilitySummary}",
            SurvivalPioneerUiPalette.Gold);
        }
      }

      if (!any)
      {
        JournalPanelLayout.CreateEmptyStateCard(
          systemLogListParent,
          ShiftUiTheme.Current,
          "No system logs yet",
          "Off-screen colony simulation incidents and facility strain events will appear here.",
          "Keep the base running through sulfur storms to generate logs.");
      }
    }

    private void CreateJournalLogCard(Transform parent, string heading, string title, string body, Color headingColor)
    {
      GameObject row = new GameObject("LogCard", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
      row.transform.SetParent(parent, false);
      Image bg = row.GetComponent<Image>();
      JournalPanelLayout.StyleDenseCard(bg);
      row.GetComponent<LayoutElement>().minHeight = Sc(JournalPanelLayout.CardMinHeight);

      TextMeshProUGUI label = CreateText(
        row.transform,
        $"<color=#{ColorUtility.ToHtmlStringRGB(headingColor)}>{heading}</color>  ·  {JournalPanelLayout.FormatAccentTitle(title)}\n{JournalPanelLayout.FormatMuted(body)}",
        ShiftUiTheme.Current,
        Sc(JournalPanelLayout.BodyFontSize),
        TextAlignmentOptions.TopLeft);
      label.textWrappingMode = TextWrappingModes.Normal;
      RectTransform labelRect = label.rectTransform;
      labelRect.anchorMin = Vector2.zero;
      labelRect.anchorMax = Vector2.one;
      labelRect.offsetMin = new Vector2(Sc(JournalPanelLayout.RowPaddingH), Sc(JournalPanelLayout.RowPaddingV));
      labelRect.offsetMax = new Vector2(-Sc(JournalPanelLayout.RowPaddingH), -Sc(JournalPanelLayout.RowPaddingV));
    }

    private void CreateQuestListColumn(Transform parent, ShiftUiTheme theme)
    {
      GameObject scrollObj = new GameObject("QuestScroll", typeof(RectTransform));
      scrollObj.transform.SetParent(parent, false);
      RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
      StretchRectToParent(scrollRt);

      LayoutElement scrollLayout = scrollObj.AddComponent<LayoutElement>();
      scrollLayout.flexibleWidth = 1f;
      scrollLayout.flexibleHeight = 1f;

      Image scrollBg = scrollObj.AddComponent<Image>();
      MenuUiBuilder.ApplyUiSprite(scrollBg);
      scrollBg.color = SurvivalPioneerUiPalette.ScrollBackground;
      scrollBg.raycastTarget = true;

      ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
      scroll.horizontal = false;
      scroll.vertical = true;
      scroll.movementType = ScrollRect.MovementType.Clamped;

      GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
      viewport.transform.SetParent(scrollObj.transform, false);
      RectTransform viewportRt = viewport.GetComponent<RectTransform>();
      StretchRectToParent(viewportRt);
      viewportRt.offsetMin = new Vector2(Sc(6f), Sc(6f));
      viewportRt.offsetMax = new Vector2(-Sc(6f), -Sc(6f));
      viewport.AddComponent<RectMask2D>();

      GameObject content = new GameObject("Content", typeof(RectTransform));
      content.transform.SetParent(viewport.transform, false);
      RectTransform contentRt = content.GetComponent<RectTransform>();
      contentRt.anchorMin = new Vector2(0f, 1f);
      contentRt.anchorMax = new Vector2(1f, 1f);
      contentRt.pivot = new Vector2(0.5f, 1f);
      contentRt.anchoredPosition = Vector2.zero;
      contentRt.sizeDelta = new Vector2(0f, 0f);

      VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
      contentLayout.spacing = Sc(JournalPanelLayout.ListSpacing);
      contentLayout.padding = new RectOffset(Sc(3), Sc(3), Sc(3), Sc(3));
      contentLayout.childControlWidth = true;
      contentLayout.childControlHeight = true;
      contentLayout.childForceExpandWidth = true;
      contentLayout.childForceExpandHeight = false;
      ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
      fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
      fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

      scroll.viewport = viewportRt;
      scroll.content = contentRt;

      questListParent = content.transform;
    }

    private void CreateQuestDetailColumn(Transform parent, ShiftUiTheme theme)
    {
      VerticalLayoutGroup layout = parent.GetComponent<VerticalLayoutGroup>();
      if (layout == null)
        layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
      layout.spacing = Sc(JournalPanelLayout.SectionSpacing);
      layout.padding = new RectOffset(
        Sc((int)JournalPanelLayout.PanelPadding),
        Sc((int)JournalPanelLayout.PanelPadding),
        Sc((int)JournalPanelLayout.PanelPadding),
        Sc((int)JournalPanelLayout.PanelPadding));
      layout.childControlWidth = true;
      layout.childControlHeight = true;
      layout.childForceExpandWidth = true;
      layout.childForceExpandHeight = false;

      questDetailTitle = CreateText(parent, "Select a quest", theme, Sc(20f), TextAlignmentOptions.TopLeft);
      questDetailTitle.fontStyle = FontStyles.Bold;
      questDetailTitle.color = SurvivalPioneerUiPalette.WarmOffWhite;

      questDetailBody = CreateText(parent, "", theme, Sc(JournalPanelLayout.BodyFontSize), TextAlignmentOptions.TopLeft);
      questDetailBody.textWrappingMode = TextWrappingModes.Normal;
      questDetailBody.color = SurvivalPioneerUiPalette.MutedText;

      GameObject objectiveHost = new GameObject("ObjectiveList", typeof(RectTransform));
      objectiveHost.transform.SetParent(parent, false);
      VerticalLayoutGroup objectiveLayout = objectiveHost.AddComponent<VerticalLayoutGroup>();
      objectiveLayout.spacing = Sc(JournalPanelLayout.ListSpacing);
      objectiveLayout.childControlWidth = true;
      objectiveLayout.childControlHeight = true;
      objectiveLayout.childForceExpandWidth = true;
      objectiveLayout.childForceExpandHeight = false;
      LayoutElement objectiveHostLayout = objectiveHost.AddComponent<LayoutElement>();
      objectiveHostLayout.flexibleHeight = 1f;
      objectiveHostLayout.minHeight = Sc(120f);
      objectiveListParent = objectiveHost.transform;

      GameObject abandonButtonObject = new GameObject("AbandonQuestButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
      abandonButtonObject.transform.SetParent(parent, false);
      LayoutElement abandonLayout = abandonButtonObject.GetComponent<LayoutElement>();
      abandonLayout.minHeight = Sc(40f);
      abandonLayout.preferredHeight = Sc(40f);

      Image abandonImage = abandonButtonObject.GetComponent<Image>();
      MenuUiBuilder.ApplyUiSprite(abandonImage);
      abandonImage.color = SurvivalPioneerUiPalette.DeepMagenta;

      abandonQuestButton = abandonButtonObject.GetComponent<Button>();
      abandonQuestButton.targetGraphic = abandonImage;
      UiSoundHelper.BindButton(abandonQuestButton);
      abandonQuestButton.onClick.RemoveAllListeners();
      abandonQuestButton.onClick.AddListener(HandleAbandonQuestClicked);

      GameObject abandonLabelObject = new GameObject("Label", typeof(RectTransform));
      abandonLabelObject.transform.SetParent(abandonButtonObject.transform, false);
      abandonQuestButtonLabel = abandonLabelObject.AddComponent<TextMeshProUGUI>();
      ApplyFont(abandonQuestButtonLabel, theme, semiBold: true);
      abandonQuestButtonLabel.text = "Abandon Quest";
      abandonQuestButtonLabel.fontSize = Sc(18f);
      abandonQuestButtonLabel.alignment = TextAlignmentOptions.Center;
      abandonQuestButtonLabel.color = SurvivalPioneerUiPalette.BodyText;
      abandonQuestButtonLabel.raycastTarget = false;
      RectTransform abandonLabelRect = abandonLabelObject.GetComponent<RectTransform>();
      abandonLabelRect.anchorMin = Vector2.zero;
      abandonLabelRect.anchorMax = Vector2.one;
      abandonLabelRect.offsetMin = Vector2.zero;
      abandonLabelRect.offsetMax = Vector2.zero;

      abandonQuestButton.gameObject.SetActive(false);
    }

    private void RefreshQuestListParents(
      Transform listParent,
      TextMeshProUGUI detailTitle,
      TextMeshProUGUI detailBody)
    {
      if (listParent == null)
        return;

      foreach (Transform child in listParent)
        Destroy(child.gameObject);

      if (questManager == null)
        return;

      IReadOnlyList<QuestProgress> allProgress = questManager.GetAllProgress();
      foreach (QuestProgress progress in allProgress)
      {
        if (progress == null || progress.status == QuestStatus.Locked)
          continue;

        if (progress.status != QuestStatus.Active && progress.status != QuestStatus.Completed)
          continue;

        QuestDefinition definition = questManager.GetDefinition(progress.questId);
        if (definition == null)
          continue;

        CreateQuestListEntry(listParent, definition, progress);
      }

      if (string.IsNullOrEmpty(selectedQuestId) && allProgress.Count > 0)
      {
        foreach (QuestProgress progress in allProgress)
        {
          if (progress != null && progress.status != QuestStatus.Locked)
          {
            selectedQuestId = progress.questId;
            break;
          }
        }
      }

      RefreshQuestDetailFor(detailTitle, detailBody, includeObjectives: true);
    }

    private void CreateQuestListEntry(Transform listParent, QuestDefinition definition, QuestProgress progress)
    {
      ShiftUiTheme theme = ShiftUiTheme.Current;
      bool selected = definition.ResolvedId == selectedQuestId;

      GameObject row = new GameObject($"Quest_{definition.ResolvedId}", typeof(RectTransform));
      row.transform.SetParent(listParent, false);

      Image rowBg = row.AddComponent<Image>();
      MenuUiBuilder.ApplyUiSprite(rowBg);
      rowBg.color = QuestUiPalette.GetRowBackgroundColor(progress.status, selected, theme);

      Button button = row.AddComponent<Button>();
      button.onClick.RemoveAllListeners();
      button.onClick.AddListener(() =>
      {
        selectedQuestId = definition.ResolvedId;
        RefreshQuestList();
      });

      LayoutElement rowLayout = row.AddComponent<LayoutElement>();
      rowLayout.minHeight = Sc(44f);
      rowLayout.preferredHeight = Sc(44f);
      rowLayout.flexibleWidth = 1f;

      VerticalLayoutGroup rowGroup = row.AddComponent<VerticalLayoutGroup>();
      rowGroup.padding = new RectOffset(
        Sc((int)JournalPanelLayout.RowPaddingH),
        Sc((int)JournalPanelLayout.RowPaddingH),
        Sc((int)JournalPanelLayout.RowPaddingV),
        Sc((int)JournalPanelLayout.RowPaddingV));
      rowGroup.childAlignment = TextAnchor.UpperLeft;
      rowGroup.childControlWidth = true;
      rowGroup.childControlHeight = true;
      rowGroup.childForceExpandWidth = true;
      rowGroup.childForceExpandHeight = false;

      TextMeshProUGUI title = CreateText(row.transform, definition.title, theme, Sc(JournalPanelLayout.BodyFontSize + 1f), TextAlignmentOptions.TopLeft);
      title.fontStyle = FontStyles.Bold;
      title.textWrappingMode = TextWrappingModes.Normal;
      title.color = QuestUiPalette.GetTitleColor(progress.status, theme);

      TextMeshProUGUI status = CreateText(
        row.transform,
        QuestUiPalette.GetStatusLabel(progress.status),
        theme,
        Sc(JournalPanelLayout.SecondaryFontSize),
        TextAlignmentOptions.TopLeft);
      status.color = QuestUiPalette.GetStatusLabelColor(progress.status, theme);
    }

    private void RefreshQuestDetailFor(TextMeshProUGUI detailTitle, TextMeshProUGUI detailBody, bool includeObjectives)
    {
      if (detailTitle == null)
        return;

      if (string.IsNullOrEmpty(selectedQuestId) || questManager == null)
      {
        detailTitle.text = "No active quests";
        if (detailBody != null)
          detailBody.text = "Accept and complete quests with companions.";
        if (includeObjectives)
          ClearObjectiveRows();
        UpdateAbandonQuestButton(null);
        return;
      }

      QuestDefinition definition = questManager.GetDefinition(selectedQuestId);
      QuestProgress progress = questManager.GetProgress(selectedQuestId);
      if (definition == null || progress == null)
        return;

      detailTitle.text = definition.title;
      detailTitle.color = QuestUiPalette.GetTitleColor(progress.status, ShiftUiTheme.Current);
      if (detailBody != null)
        detailBody.text = definition.description;

      if (includeObjectives)
        RefreshObjectiveRows(definition, progress);

      UpdateAbandonQuestButton(progress);
    }

    private void UpdateAbandonQuestButton(QuestProgress progress)
    {
      if (abandonQuestButton == null)
        return;

      bool canAbandon = progress != null && progress.status == QuestStatus.Active;
      abandonQuestButton.gameObject.SetActive(canAbandon);
      abandonQuestButton.interactable = canAbandon;
      abandonConfirmPending = false;
      if (abandonQuestButtonLabel != null)
        abandonQuestButtonLabel.text = "Abandon Quest";
    }

    private void HandleAbandonQuestClicked()
    {
      if (questManager == null || string.IsNullOrEmpty(selectedQuestId))
        return;

      QuestProgress progress = questManager.GetProgress(selectedQuestId);
      if (progress == null || progress.status != QuestStatus.Active)
        return;

      if (!abandonConfirmPending)
      {
        abandonConfirmPending = true;
        if (abandonQuestButtonLabel != null)
          abandonQuestButtonLabel.text = "Confirm Abandon?";
        return;
      }

      if (!questManager.AbandonQuest(selectedQuestId))
        return;

      abandonConfirmPending = false;
      selectedQuestId = null;
      RefreshQuestList();
      FindAnyObjectByType<ActiveQuestHudUI>(FindObjectsInactive.Include)?.Refresh();
    }

    private void HandleQuestUpdated(QuestProgress progress)
    {
      if (progress != null && string.IsNullOrEmpty(selectedQuestId))
        selectedQuestId = progress.questId;

      RefreshQuestList();
      FindAnyObjectByType<ActiveQuestHudUI>(FindObjectsInactive.Include)?.Refresh();
    }

    private void ClearObjectiveRows()
    {
      if (objectiveListParent == null)
        return;

      foreach (Transform child in objectiveListParent)
        Destroy(child.gameObject);
    }

    private void RefreshObjectiveRows(QuestDefinition definition, QuestProgress progress)
    {
      ClearObjectiveRows();
      if (objectiveListParent == null || definition.objectives == null || definition.objectives.Count == 0)
        return;

      ShiftUiTheme theme = ShiftUiTheme.Current;
      for (int i = 0; i < definition.objectives.Count; i++)
      {
        QuestObjectiveDefinition objective = definition.objectives[i];
        if (objective == null)
          continue;

        int required = Mathf.Max(1, objective.requiredCount);
        int current = progress.GetObjectiveProgress(i);
        string label = string.IsNullOrEmpty(objective.description)
          ? objective.type.ToString()
          : objective.description;

        CreateObjectiveRow(objectiveListParent, label, current, required, progress.status, theme);
      }
    }

    private static void CreateObjectiveRow(
      Transform parent,
      string label,
      int current,
      int required,
      QuestStatus questStatus,
      ShiftUiTheme theme)
    {
      GameObject row = new GameObject("ObjectiveRow", typeof(RectTransform));
      row.transform.SetParent(parent, false);

      LayoutElement rowLayout = row.AddComponent<LayoutElement>();
      rowLayout.minHeight = Sc(22f);

      HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
      layout.spacing = Sc(8f);
      layout.childAlignment = TextAnchor.MiddleLeft;
      layout.childControlWidth = true;
      layout.childControlHeight = true;
      layout.childForceExpandWidth = true;
      layout.childForceExpandHeight = false;

      bool complete = current >= required;
      TextMeshProUGUI descriptionText = CreateText(row.transform, label, theme, Sc(15f), TextAlignmentOptions.MidlineLeft);
      descriptionText.textWrappingMode = TextWrappingModes.Normal;
      descriptionText.color = QuestUiPalette.GetObjectiveTextColor(complete, questStatus, theme);
      LayoutElement descriptionLayout = descriptionText.gameObject.AddComponent<LayoutElement>();
      descriptionLayout.flexibleWidth = 1f;

      TextMeshProUGUI countText = CreateText(
        row.transform,
        $"{Mathf.Min(current, required)}/{required}",
        theme,
        Sc(15f),
        TextAlignmentOptions.MidlineRight);
      countText.fontStyle = FontStyles.Bold;
      countText.color = QuestUiPalette.GetObjectiveTextColor(complete, questStatus, theme);
      LayoutElement countLayout = countText.gameObject.AddComponent<LayoutElement>();
      countLayout.minWidth = Sc(56f);
      countLayout.preferredWidth = Sc(56f);
      countLayout.flexibleWidth = 0f;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string textValue, ShiftUiTheme theme, float fontSize, TextAlignmentOptions alignment)
    {
      GameObject textObj = new GameObject("Text", typeof(RectTransform));
      textObj.transform.SetParent(parent, false);
      TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
      ApplyFont(text, theme);
      text.text = textValue;
      text.fontSize = fontSize;
      text.alignment = alignment;
      text.textWrappingMode = TextWrappingModes.Normal;
      return text;
    }

    private static void ApplyFont(TextMeshProUGUI text, ShiftUiTheme theme, bool bold = false, bool semiBold = false)
    {
      if (theme != null)
        theme.ApplyFont(text, bold: bold, semiBold: semiBold);
      else
        TmpUiHelper.ApplyDefaultFont(text);
    }

    private static float Sc(float value) => value * UiScale;

    private static int Sc(int value) => Mathf.RoundToInt(value * UiScale);

    private static void StretchRectToParent(RectTransform rect)
    {
      if (rect == null)
        return;

      rect.anchorMin = Vector2.zero;
      rect.anchorMax = Vector2.one;
      rect.pivot = new Vector2(0.5f, 0.5f);
      rect.offsetMin = Vector2.zero;
      rect.offsetMax = Vector2.zero;
      rect.anchoredPosition = Vector2.zero;
    }
  }
}

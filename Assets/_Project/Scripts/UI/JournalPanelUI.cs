using System.Collections.Generic;
using Project.Core;
using Project.Crafting;
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
    private const float UiScale = 0.75f;

    private GameObject overlayRoot;
    private RectTransform windowHostRect;
    private JournalTabRail tabRail;
    private FullscreenUiNavigator navigator;
    private Transform questListParent;
    private TextMeshProUGUI questDetailTitle;
    private TextMeshProUGUI questDetailBody;
    private Transform objectiveListParent;

    private string selectedQuestId;
    private bool uiBuilt;
    private int lastToggleFrame = -1;

    private InventoryUI inventoryUi;
    private PioneerRosterPanelUI pioneerRosterPanelUi;
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

      if (windowId == JournalWindowId.Map && !GameSettings.MapSystemEnabled)
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

    public void OpenToRecipesTab() => TryToggleTab(JournalWindowId.Recipes);

    public void OpenToSkillsTab() => TryToggleTab(JournalWindowId.Skills);

    public void OpenToEchoesTab() => TryToggleTab(JournalWindowId.Echoes);

    public void OpenToCraftTab(CraftingStationType? station)
    {
      if (craftingManager == null)
        craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();

      if (craftingManager != null)
        craftingManager.CurrentStation = station;

      TryToggleTab(JournalWindowId.Craft);
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
      windowHostRect.offsetMin = new Vector2(Sc(JournalTabRail.RailWidth), 0f);
      windowHostRect.offsetMax = Vector2.zero;

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
        pet.Configure(petUi ?? FindAnyObjectByType<PetUI>());
      });

      RegisterWindow<CraftFullscreenWindow>(JournalWindowId.Craft, "Craft", craft =>
      {
        craft.Configure(craftingUi ?? FindAnyObjectByType<CraftingUI>() ?? gameObject.AddComponent<CraftingUI>());
      });

      RegisterWindow<PioneersFullscreenWindow>(JournalWindowId.Pioneers, "Pioneers", pioneers =>
      {
        pioneers.Configure(pioneerRosterPanelUi ?? GetComponent<PioneerRosterPanelUI>() ?? gameObject.AddComponent<PioneerRosterPanelUI>());
      });

      RegisterWindow<StubFullscreenWindow>(JournalWindowId.Recipes, "Recipes", stub => stub.Configure(
        "Recipe Library",
        "Browse learned recipes and scroll slots. Production runs at in-world crafting stations and future building control panels.",
        "Collect and learn recipe scrolls",
        "View ingredients and station requirements",
        "Plan crafts before visiting a station"));

      RegisterWindow<StubFullscreenWindow>(JournalWindowId.Skills, "Skills", stub => stub.Configure(
        "Pioneer Skill Trees",
        "Specialize your base pioneers and expedition trio with class-focused skill branches.",
        "Architect Engineer, Science Specialist, Combat Tactician, Infiltrator Scout paths",
        "Hybrid pioneer builds",
        "Resonance-linked skill unlocks"));

      RegisterWindow<StubFullscreenWindow>(JournalWindowId.Echoes, "Echoes", stub => stub.Configure(
        "Neural Echo Memories",
        "Rescued Neural Echoes become pioneers whose memories fuel Resonance Events across Io.",
        "Echo rescue chronicle",
        "Aether-9 memory core archive",
        "Marketplace listing preparation"));
    }

    private void RegisterWindow<T>(JournalWindowId id, string title, System.Action<T> configure)
      where T : FullscreenUiWindow
    {
      GameObject host = new GameObject(id + "WindowHost", typeof(RectTransform));
      host.transform.SetParent(windowHostRect != null ? windowHostRect : transform, false);
      StretchRectToParent(host.GetComponent<RectTransform>());

      T window = host.AddComponent<T>();
      configure?.Invoke(window);
      window.Initialize(navigator, id, title, new Color(0.05f, 0.06f, 0.09f, 0.98f));
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
      // Bottom → top: dim overlay, window content, tab rail (always receives clicks on the left).
      overlayRoot?.transform.SetAsLastSibling();
      windowHostRect?.transform.SetAsLastSibling();
      tabRail?.transform.SetAsLastSibling();
    }

    private void EnforceJournalChromeLayout()
    {
      float railWidth = Sc(JournalTabRail.RailWidth);

      if (windowHostRect != null)
      {
        Vector2 hostOffsetMin = windowHostRect.offsetMin;
        if (hostOffsetMin.x < railWidth - 0.5f)
          windowHostRect.offsetMin = new Vector2(railWidth, hostOffsetMin.y);

        windowHostRect.anchorMin = Vector2.zero;
        windowHostRect.anchorMax = Vector2.one;
        windowHostRect.offsetMax = Vector2.zero;

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
          railRect.anchorMin = new Vector2(0f, 0f);
          railRect.anchorMax = new Vector2(0f, 1f);
          railRect.pivot = new Vector2(0f, 0.5f);
          railRect.anchoredPosition = Vector2.zero;
          railRect.sizeDelta = new Vector2(railWidth, 0f);
        }

        if (tabRail.TryGetComponent(out Image railImage) && railImage.color.a < 0.05f)
          railImage.color = new Color(0.04f, 0.05f, 0.08f, 0.96f);
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
      }
      else
      {
        EnsureInventoryUi()?.RestoreInventoryPanel();

        if (windowHostRect != null)
          windowHostRect.gameObject.SetActive(false);

        GameplayHudVisibility.RefreshGameplayHud();
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
      if (windowId == JournalWindowId.Map && !GameSettings.MapSystemEnabled)
        return;

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
      GameObject split = new GameObject("JournalSplit", typeof(RectTransform));
      split.transform.SetParent(parent, false);
      StretchRectToParent(split.GetComponent<RectTransform>());

      HorizontalLayoutGroup splitLayout = split.AddComponent<HorizontalLayoutGroup>();
      splitLayout.spacing = Sc(8f);
      splitLayout.padding = new RectOffset(Sc(8), Sc(8), Sc(8), Sc(8));
      splitLayout.childControlWidth = true;
      splitLayout.childControlHeight = true;
      splitLayout.childForceExpandHeight = true;

      GameObject listColumn = new GameObject("QuestListColumn", typeof(RectTransform));
      listColumn.transform.SetParent(split.transform, false);
      LayoutElement listLayout = listColumn.AddComponent<LayoutElement>();
      listLayout.flexibleWidth = 0.45f;
      listLayout.minWidth = Sc(340f);
      listLayout.flexibleHeight = 1f;
      CreateQuestListColumn(listColumn.transform, theme);

      GameObject detailColumn = new GameObject("QuestDetailColumn", typeof(RectTransform));
      detailColumn.transform.SetParent(split.transform, false);
      LayoutElement detailLayout = detailColumn.AddComponent<LayoutElement>();
      detailLayout.flexibleWidth = 0.55f;
      detailLayout.flexibleHeight = 1f;
      CreateQuestDetailColumn(detailColumn.transform, theme);
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
      scrollBg.color = new Color(0.08f, 0.09f, 0.12f, 0.92f);
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
      contentLayout.spacing = Sc(6f);
      contentLayout.padding = new RectOffset(Sc(4), Sc(4), Sc(4), Sc(4));
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
      layout.spacing = Sc(8f);
      layout.padding = new RectOffset(Sc(8), Sc(8), Sc(8), Sc(8));
      layout.childControlWidth = true;
      layout.childControlHeight = true;
      layout.childForceExpandWidth = true;
      layout.childForceExpandHeight = false;

      questDetailTitle = CreateText(parent, "Select a quest", theme, Sc(24f), TextAlignmentOptions.TopLeft);
      questDetailTitle.fontStyle = FontStyles.Bold;

      questDetailBody = CreateText(parent, "", theme, Sc(16f), TextAlignmentOptions.TopLeft);
      questDetailBody.textWrappingMode = TextWrappingModes.Normal;
      questDetailBody.color = theme != null ? theme.secondaryTextColor : new Color(0.82f, 0.88f, 0.94f, 0.95f);

      GameObject objectiveHost = new GameObject("ObjectiveList", typeof(RectTransform));
      objectiveHost.transform.SetParent(parent, false);
      VerticalLayoutGroup objectiveLayout = objectiveHost.AddComponent<VerticalLayoutGroup>();
      objectiveLayout.spacing = Sc(4f);
      objectiveLayout.childControlWidth = true;
      objectiveLayout.childControlHeight = true;
      objectiveLayout.childForceExpandWidth = true;
      objectiveLayout.childForceExpandHeight = false;
      LayoutElement objectiveHostLayout = objectiveHost.AddComponent<LayoutElement>();
      objectiveHostLayout.flexibleHeight = 1f;
      objectiveHostLayout.minHeight = Sc(120f);
      objectiveListParent = objectiveHost.transform;
    }

    public void RefreshQuestList()
    {
      RefreshQuestListParents(questListParent, questDetailTitle, questDetailBody);
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
      button.onClick.AddListener(() =>
      {
        selectedQuestId = definition.ResolvedId;
        RefreshQuestList();
      });

      LayoutElement rowLayout = row.AddComponent<LayoutElement>();
      rowLayout.minHeight = Sc(52f);
      rowLayout.preferredHeight = Sc(52f);
      rowLayout.flexibleWidth = 1f;

      VerticalLayoutGroup rowGroup = row.AddComponent<VerticalLayoutGroup>();
      rowGroup.padding = new RectOffset(Sc(8), Sc(8), Sc(6), Sc(6));
      rowGroup.childAlignment = TextAnchor.UpperLeft;
      rowGroup.childControlWidth = true;
      rowGroup.childControlHeight = true;
      rowGroup.childForceExpandWidth = true;
      rowGroup.childForceExpandHeight = false;

      TextMeshProUGUI title = CreateText(row.transform, definition.title, theme, Sc(16f), TextAlignmentOptions.TopLeft);
      title.fontStyle = FontStyles.Bold;
      title.textWrappingMode = TextWrappingModes.Normal;
      title.color = QuestUiPalette.GetTitleColor(progress.status, theme);

      TextMeshProUGUI status = CreateText(
        row.transform,
        QuestUiPalette.GetStatusLabel(progress.status),
        theme,
        Sc(13f),
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
          detailBody.text = "Accept and complete quests with NPCs.";
        if (includeObjectives)
          ClearObjectiveRows();
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
    }

    private void HandleQuestUpdated(QuestProgress progress)
    {
      if (progress != null && string.IsNullOrEmpty(selectedQuestId))
        selectedQuestId = progress.questId;

      RefreshQuestList();
      FindAnyObjectByType<ActiveQuestHudUI>()?.Refresh();
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

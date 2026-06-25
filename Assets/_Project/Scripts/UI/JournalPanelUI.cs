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
    private enum JournalTab
    {
      Journal = 0,
      Inventory = 1,
      Pet = 2,
      Craft = 3
    }

    private const float UiScale = 0.75f;
    private const float PanelWidth = 1080f * UiScale;
    private const float PanelHeight = 720f * UiScale;
    private const float PanelGap = 12f * UiScale;

    private GameObject overlayRoot;
    private GameObject panelRoot;
    private RectTransform panelRect;
    private Transform journalTabContent;
    private Transform inventoryTabContent;
    private Transform pioneersTabContent;
    private Transform craftTabContent;
    private RectTransform tabContentHostRect;
    private RectTransform headerRect;
    private RectTransform tabBarRect;
    private Transform questListParent;
    private TextMeshProUGUI questDetailTitle;
    private TextMeshProUGUI questDetailBody;
    private Transform objectiveListParent;

    private readonly List<Button> tabButtons = new List<Button>();
    private readonly List<GameObject> tabContents = new List<GameObject>();
    private JournalTab activeTab = JournalTab.Journal;
    private string selectedQuestId;
    private bool isOpen;
    private bool uiBuilt;

    private InventoryUI inventoryUi;
    private PioneerRosterPanelUI pioneerRosterPanelUi;
    private CraftingUI craftingUi;
    private QuestManager questManager;
    private CraftingManager craftingManager;

    private void Awake()
    {
      EnsureUiBuilt();
      ClosePanel();
    }

    private void Start()
    {
      inventoryUi = FindAnyObjectByType<InventoryUI>();
      pioneerRosterPanelUi = GetComponent<PioneerRosterPanelUI>();
      if (pioneerRosterPanelUi == null)
        pioneerRosterPanelUi = gameObject.AddComponent<PioneerRosterPanelUI>();
      craftingUi = FindAnyObjectByType<CraftingUI>();
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

      if (questManager != null)
      {
        questManager.OnQuestUpdated += HandleQuestUpdated;
        questManager.OnQuestCompleted += HandleQuestUpdated;
      }

      RefreshQuestList();
    }

    private void OnDestroy()
    {
      if (questManager != null)
      {
        questManager.OnQuestUpdated -= HandleQuestUpdated;
        questManager.OnQuestCompleted -= HandleQuestUpdated;
      }
    }

    private void Update()
    {
      if (!GameSession.HasStarted)
        return;

      if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
        TogglePanel();

      if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        OpenToInventoryTab();

      if (isOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        ClosePanel();
    }

    public void OnToggleJournal(InputAction.CallbackContext context)
    {
      if (!GameSession.HasStarted || !context.performed)
        return;

      TogglePanel();
    }

    public bool IsOpen => isOpen;

    public void TogglePanel()
    {
      if (isOpen)
        ClosePanel();
      else
        OpenPanel();
    }

    public void OpenToInventoryTab()
    {
      if (isOpen && activeTab == JournalTab.Inventory)
      {
        ClosePanel();
        return;
      }

      activeTab = JournalTab.Inventory;
      if (isOpen)
      {
        SetActiveTab(JournalTab.Inventory);
        return;
      }

      OpenPanel();
      SetActiveTab(JournalTab.Inventory);
    }

    public void OpenToCraftTab(CraftingStationType? station)
    {
      if (craftingManager == null)
        craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();

      if (craftingManager != null)
        craftingManager.CurrentStation = station;

      if (!isOpen)
        OpenPanel();

      OpenStandaloneCraftingPanel();
    }

    private void OpenStandaloneCraftingPanel()
    {
      if (craftingUi == null)
        craftingUi = FindAnyObjectByType<CraftingUI>() ?? GetComponent<CraftingUI>();

      if (craftingUi == null)
        craftingUi = gameObject.AddComponent<CraftingUI>();

      if (panelRect == null)
        panelRect = panelRoot.GetComponent<RectTransform>();

      craftingUi.OpenStandalonePanel(overlayRoot.transform, panelRect, PanelGap);
    }

    public void OpenPanel()
    {
      EnsureUiBuilt();
      CloseConflictingPanels();

      isOpen = true;
      overlayRoot.SetActive(true);
      overlayRoot.transform.SetAsLastSibling();
      panelRoot.SetActive(true);

      ItemHoverTooltip.HideAny();
      RecipeHoverTooltip.HideAny();
      SetActiveTab(activeTab);
      RefreshQuestList();
      UiFrontLayer.BringLayerToFront(transform);
      PauseGameplay(true);
    }

    public void ClosePanel()
    {
      if (!uiBuilt)
        return;

      isOpen = false;
      overlayRoot.SetActive(false);
      ItemHoverTooltip.HideAny();
      RecipeHoverTooltip.HideAny();
      InventoryContextMenu.Instance?.Hide();
      if (craftingManager == null)
        craftingManager = CraftingManager.Instance ?? FindAnyObjectByType<CraftingManager>();
      if (craftingManager != null)
        craftingManager.CurrentStation = null;
      craftingUi?.CloseStandalonePanel();
      UnembedAllTabs();
      PauseGameplay(false);
    }

    public static void CloseAnyOpenJournal()
    {
      JournalPanelUI journal = FindAnyObjectByType<JournalPanelUI>();
      journal?.ReleaseInputCapture();
    }

    public void ReleaseInputCapture()
    {
      if (isOpen)
      {
        ClosePanel();
        return;
      }

      PlayerController player = FindAnyObjectByType<PlayerController>();
      if (player != null)
        player.SetJournalOpen(false);

      CameraController camera = FindAnyObjectByType<CameraController>();
      if (camera != null)
        camera.SetJournalOpen(false);
    }

    private void CloseConflictingPanels()
    {
      InventoryUI.CloseAnyOpenInventory();
      MapUI.CloseAnyOpenMap();
    }

    private void EnsureUiBuilt()
    {
      if (uiBuilt)
        return;

      BuildUi();
      uiBuilt = true;
    }

    private void BuildUi()
    {
      ShiftUiTheme theme = ShiftUiTheme.Current;

      overlayRoot = MenuUiBuilder.CreateFullScreenPanel(transform, "JournalOverlay", new Color(0f, 0f, 0f, 0.55f), blockRaycasts: true);
      overlayRoot.transform.SetAsLastSibling();

      panelRoot = new GameObject("JournalPanel", typeof(RectTransform));
      panelRoot.transform.SetParent(overlayRoot.transform, false);
      panelRect = panelRoot.GetComponent<RectTransform>();
      panelRect.anchorMin = new Vector2(0.5f, 0.5f);
      panelRect.anchorMax = new Vector2(0.5f, 0.5f);
      panelRect.pivot = new Vector2(0.5f, 0.5f);
      panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
      panelRect.anchoredPosition = Vector2.zero;

      Image panelBg = panelRoot.AddComponent<Image>();
      if (theme != null)
        theme.ApplyPanelImage(panelBg, large: true);
      else
      {
        MenuUiBuilder.ApplyUiSprite(panelBg);
        panelBg.color = new Color(0.08f, 0.09f, 0.12f, 0.98f);
      }

      VerticalLayoutGroup panelLayout = panelRoot.AddComponent<VerticalLayoutGroup>();
      panelLayout.padding = new RectOffset(Sc(10), Sc(10), Sc(10), Sc(10));
      panelLayout.spacing = Sc(8f);
      panelLayout.childControlWidth = true;
      panelLayout.childControlHeight = true;
      panelLayout.childForceExpandWidth = true;
      panelLayout.childForceExpandHeight = false;

      UiPanelDragHandle.Create(panelRoot.transform, panelRect, "Journal", Sc(34f), Sc(14f));

      CreateHeaderRow(theme);
      CreateTabBar(theme);
      CreateTabContents(theme);
      CreateJournalTabUi(theme);

      UIDragHandler panelDrag = UiPanelDragHandle.EnsureHandler(panelRect);
      if (panelDrag != null)
        panelDrag.targetWindow = panelRect;

      if (headerRect != null)
        UiPanelDragHandle.Bind(headerRect, panelRect);
      if (tabBarRect != null)
        UiPanelDragHandle.Bind(tabBarRect, panelRect);
      if (tabContentHostRect != null)
        UiPanelDragHandle.Bind(tabContentHostRect, panelRect);

      UiPanelResizeHandles.AddAll(
        panelRoot.transform,
        panelRect,
        lockAspectRatio: false,
        minSize: new Vector2(Sc(420f), Sc(320f)),
        maxSize: new Vector2(Sc(1400f), Sc(980f)));
    }

    private void CreateHeaderRow(ShiftUiTheme theme)
    {
      GameObject header = new GameObject("Header", typeof(RectTransform));
      header.transform.SetParent(panelRoot.transform, false);
      headerRect = header.GetComponent<RectTransform>();
      Image headerBg = header.AddComponent<Image>();
      headerBg.color = new Color(0f, 0f, 0f, 0.001f);
      headerBg.raycastTarget = true;
      HorizontalLayoutGroup layout = header.AddComponent<HorizontalLayoutGroup>();
      layout.childAlignment = TextAnchor.MiddleLeft;
      layout.childControlWidth = true;
      layout.childForceExpandWidth = true;

      GameObject titleObj = new GameObject("Title", typeof(RectTransform));
      titleObj.transform.SetParent(header.transform, false);
      TextMeshProUGUI title = titleObj.AddComponent<TextMeshProUGUI>();
      ApplyFont(title, theme, bold: true);
      title.text = "Quests & Inventory";
      title.fontSize = Sc(18f);
      title.alignment = TextAlignmentOptions.MidlineLeft;
      LayoutElement titleLayout = titleObj.AddComponent<LayoutElement>();
      titleLayout.flexibleWidth = 1f;

      Button closeButton = MenuUiBuilder.CreateTextCloseButton(header.transform, Sc(16f), ClosePanel);
      LayoutElement closeLayout = closeButton.gameObject.AddComponent<LayoutElement>();
      closeLayout.minWidth = Sc(24f);
      closeLayout.preferredWidth = Sc(24f);
      closeLayout.minHeight = Sc(24f);
      closeLayout.preferredHeight = Sc(24f);
    }

    private void CreateTabBar(ShiftUiTheme theme)
    {
      GameObject tabBar = new GameObject("TabBar", typeof(RectTransform));
      tabBar.transform.SetParent(panelRoot.transform, false);
      tabBarRect = tabBar.GetComponent<RectTransform>();
      Image tabBarBg = tabBar.AddComponent<Image>();
      tabBarBg.color = new Color(0f, 0f, 0f, 0.001f);
      tabBarBg.raycastTarget = true;
      HorizontalLayoutGroup layout = tabBar.AddComponent<HorizontalLayoutGroup>();
      layout.spacing = Sc(6f);
      layout.childAlignment = TextAnchor.MiddleLeft;
      layout.childControlWidth = false;
      layout.childControlHeight = true;
      LayoutElement tabBarLayout = tabBar.AddComponent<LayoutElement>();
      tabBarLayout.minHeight = Sc(44f);

      string[] labels = { "Journal", "Inventory", "Pioneers", "Recipes" };
      for (int i = 0; i < labels.Length; i++)
      {
        Button tabButton = CreateSmallButton(tabBar.transform, labels[i], theme, new Vector2(Sc(138f), Sc(40f)));
        int tabIndex = i;
        tabButton.onClick.AddListener(() => SetActiveTab((JournalTab)tabIndex));
        tabButtons.Add(tabButton);
      }
    }

    private void CreateTabContents(ShiftUiTheme theme)
    {
      GameObject contentHost = new GameObject("TabContentHost", typeof(RectTransform));
      contentHost.transform.SetParent(panelRoot.transform, false);
      tabContentHostRect = contentHost.GetComponent<RectTransform>();
      Image hostCapture = contentHost.AddComponent<Image>();
      hostCapture.color = new Color(0f, 0f, 0f, 0.001f);
      hostCapture.raycastTarget = true;
      LayoutElement hostLayout = contentHost.AddComponent<LayoutElement>();
      hostLayout.flexibleHeight = 1f;
      hostLayout.minHeight = Sc(520f);

      journalTabContent = CreateTabContentRoot(contentHost.transform, "JournalTabContent", out GameObject journalGo);
      inventoryTabContent = CreateTabContentRoot(contentHost.transform, "InventoryTabContent", out GameObject inventoryGo);
      pioneersTabContent = CreateTabContentRoot(contentHost.transform, "PioneersTabContent", out GameObject pioneersGo);
      craftTabContent = CreateTabContentRoot(contentHost.transform, "CraftTabContent", out GameObject craftGo);

      tabContents.Add(journalGo);
      tabContents.Add(inventoryGo);
      tabContents.Add(pioneersGo);
      tabContents.Add(craftGo);
    }

    private Transform CreateTabContentRoot(Transform parent, string name, out GameObject rootGo)
    {
      rootGo = new GameObject(name, typeof(RectTransform));
      rootGo.transform.SetParent(parent, false);
      RectTransform rect = rootGo.GetComponent<RectTransform>();
      rect.anchorMin = Vector2.zero;
      rect.anchorMax = Vector2.one;
      rect.offsetMin = Vector2.zero;
      rect.offsetMax = Vector2.zero;

      rootGo.SetActive(false);
      return rootGo.transform;
    }

    private void CreateJournalTabUi(ShiftUiTheme theme)
    {
      GameObject split = new GameObject("JournalSplit", typeof(RectTransform));
      split.transform.SetParent(journalTabContent, false);
      RectTransform splitRect = split.GetComponent<RectTransform>();
      splitRect.anchorMin = Vector2.zero;
      splitRect.anchorMax = Vector2.one;
      splitRect.offsetMin = new Vector2(Sc(4f), Sc(4f));
      splitRect.offsetMax = new Vector2(-Sc(4f), -Sc(4f));

      HorizontalLayoutGroup splitLayout = split.AddComponent<HorizontalLayoutGroup>();
      splitLayout.spacing = Sc(8f);
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
      Outline scrollOutline = scrollObj.AddComponent<Outline>();
      scrollOutline.effectColor = new Color(0.22f, 0.26f, 0.32f, 0.65f);
      scrollOutline.effectDistance = new Vector2(1f, -1f);

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
      VerticalLayoutGroup layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
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

      TextMeshProUGUI skillsLabel = CreateText(parent, "Skills — Coming soon", theme, Sc(15f), TextAlignmentOptions.TopLeft);
      skillsLabel.color = theme != null ? theme.secondaryTextColor : new Color(0.75f, 0.82f, 0.9f, 0.9f);
    }

    private void SetActiveTab(JournalTab tab)
    {
      activeTab = tab;

      for (int i = 0; i < tabContents.Count; i++)
        tabContents[i].SetActive(i == (int)tab);

      HighlightTabButtons((int)tab);
      UnembedAllTabs();

      switch (tab)
      {
        case JournalTab.Inventory:
          EmbedInventoryTab();
          break;
        case JournalTab.Pet:
          EmbedPioneersTab();
          break;
        case JournalTab.Craft:
          EmbedCraftTab();
          break;
      }
    }

    private void HighlightTabButtons(int activeIndex)
    {
      ShiftUiTheme theme = ShiftUiTheme.Current;
      for (int i = 0; i < tabButtons.Count; i++)
      {
        Image image = tabButtons[i].GetComponent<Image>();
        if (image == null)
          continue;

        bool active = i == activeIndex;
        image.color = active
          ? (theme != null ? new Color(theme.primaryColor.r, theme.primaryColor.g, theme.primaryColor.b, 0.35f) : new Color(0.24f, 0.36f, 0.48f, 1f))
          : new Color(0.14f, 0.16f, 0.2f, 0.95f);
      }
    }

    private void EmbedInventoryTab()
    {
      if (inventoryUi == null)
        inventoryUi = FindAnyObjectByType<InventoryUI>();

      inventoryUi?.EmbedInventoryPanel(inventoryTabContent);
      if (inventoryUi != null && inventoryUi.inventoryPanel != null)
        ApplyEmbeddedInsets(inventoryUi.inventoryPanel.GetComponent<RectTransform>());

      inventoryUi?.RefreshMainInventoryLayout();
    }

    private void EmbedPioneersTab()
    {
      if (pioneerRosterPanelUi == null)
      {
        pioneerRosterPanelUi = GetComponent<PioneerRosterPanelUI>();
        if (pioneerRosterPanelUi == null)
          pioneerRosterPanelUi = gameObject.AddComponent<PioneerRosterPanelUI>();
      }

      pioneerRosterPanelUi.EmbedIn(pioneersTabContent);
    }

    private void EmbedCraftTab()
    {
      if (craftingUi == null)
        craftingUi = FindAnyObjectByType<CraftingUI>();

      craftingUi?.EmbedPanel(craftTabContent);
      ApplyEmbeddedInsets(GetFirstChildRect(craftTabContent));
    }

    private void UnembedAllTabs()
    {
      inventoryUi?.RestoreInventoryPanel();
      pioneerRosterPanelUi?.Unembed();
      craftingUi?.RestorePanel();
    }

    private static RectTransform GetFirstChildRect(Transform container)
    {
      if (container == null || container.childCount == 0)
        return null;

      return container.GetChild(0) as RectTransform;
    }

    private static void ApplyEmbeddedInsets(RectTransform rect)
    {
      if (rect == null)
        return;

      float pad = Sc(6f);
      rect.anchorMin = Vector2.zero;
      rect.anchorMax = Vector2.one;
      rect.offsetMin = new Vector2(pad, pad);
      rect.offsetMax = new Vector2(-pad, -pad);
      rect.localScale = Vector3.one;
    }

    private void RefreshQuestList()
    {
      if (questListParent == null)
        return;

      foreach (Transform child in questListParent)
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

        CreateQuestListEntry(definition, progress);
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

      RefreshQuestDetail();
    }

        private void CreateQuestListEntry(QuestDefinition definition, QuestProgress progress)
        {
            ShiftUiTheme theme = ShiftUiTheme.Current;
            bool selected = definition.ResolvedId == selectedQuestId;

            GameObject row = new GameObject($"Quest_{definition.ResolvedId}", typeof(RectTransform));
            row.transform.SetParent(questListParent, false);

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
            LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1f;

            TextMeshProUGUI status = CreateText(
                row.transform,
                QuestUiPalette.GetStatusLabel(progress.status),
                theme,
                Sc(13f),
                TextAlignmentOptions.TopLeft);
            status.color = QuestUiPalette.GetStatusLabelColor(progress.status, theme);
        }

    private void RefreshQuestDetail()
    {
      if (questDetailTitle == null)
        return;

        if (string.IsNullOrEmpty(selectedQuestId) || questManager == null)
        {
          questDetailTitle.text = "No active quests";
          questDetailBody.text = "Accept and complete quests with NPCs. Turn them in at the quest giver for rewards.";
          ClearObjectiveRows();
          return;
        }

      QuestDefinition definition = questManager.GetDefinition(selectedQuestId);
      QuestProgress progress = questManager.GetProgress(selectedQuestId);
      if (definition == null || progress == null)
        return;

      questDetailTitle.text = definition.title;
      questDetailTitle.color = QuestUiPalette.GetTitleColor(progress.status, ShiftUiTheme.Current);
      questDetailBody.text = definition.description;
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

    private static void PauseGameplay(bool pause)
    {
      Cursor.lockState = pause ? CursorLockMode.None : CursorLockMode.Locked;
      Cursor.visible = pause;

      PlayerController player = FindAnyObjectByType<PlayerController>();
      if (player != null)
        player.SetJournalOpen(pause);

      CameraController camera = FindAnyObjectByType<CameraController>();
      if (camera != null)
        camera.SetJournalOpen(pause);
    }

    private static Button CreateSmallButton(Transform parent, string label, ShiftUiTheme theme, Vector2 size)
    {
      GameObject buttonObj = new GameObject(label + "Button", typeof(RectTransform));
      buttonObj.transform.SetParent(parent, false);

      RectTransform rect = buttonObj.GetComponent<RectTransform>();
      rect.sizeDelta = size;

      LayoutElement layout = buttonObj.AddComponent<LayoutElement>();
      layout.minWidth = size.x;
      layout.preferredWidth = size.x;
      layout.minHeight = size.y;
      layout.preferredHeight = size.y;

      Image image = buttonObj.AddComponent<Image>();
      MenuUiBuilder.ApplyUiSprite(image);
      image.color = new Color(0.14f, 0.16f, 0.2f, 0.95f);

      Button button = buttonObj.AddComponent<Button>();
      UiSoundHelper.BindButton(button);

      GameObject textObj = new GameObject("Text", typeof(RectTransform));
      textObj.transform.SetParent(buttonObj.transform, false);
      TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
      ApplyFont(text, theme, semiBold: true);
      text.text = label;
      text.fontSize = Sc(15f);
      text.alignment = TextAlignmentOptions.Center;
      RectTransform textRect = textObj.GetComponent<RectTransform>();
      textRect.anchorMin = Vector2.zero;
      textRect.anchorMax = Vector2.one;
      textRect.offsetMin = Vector2.zero;
      textRect.offsetMax = Vector2.zero;

      return button;
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

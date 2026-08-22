using Project.Core;
using Project.Player;
using Project.Shelter;

using TMPro;

using UnityEngine;

using UnityEngine.EventSystems;

using UnityEngine.InputSystem.UI;

using UnityEngine.UI;



namespace Project.UI

{

    /// <summary>

    /// Hold-E popup while sheltered: exit back to the world or exit and store in inventory.

    /// </summary>

    public sealed class QuoraShelterMenuUI : MonoBehaviour

    {

        private static QuoraShelterMenuUI instance;



        private GameObject menuRoot;

        private GameObject menuPanel;

        private TextMeshProUGUI timerText;

        private Transform canvasRoot;

        private QuoraShelterController activeShelter;

        private PlayerController boundPlayer;



        public static bool IsOpen => instance != null && instance.menuRoot != null && instance.menuRoot.activeSelf;



        public static QuoraShelterMenuUI EnsureExists(Transform canvasRootTransform)

        {

            if (instance != null)

            {

                instance.canvasRoot = canvasRootTransform;

                return instance;

            }



            GameObject host = new GameObject("QuoraShelterMenu", typeof(RectTransform));

            host.transform.SetParent(canvasRootTransform, false);

            QuoraShelterMenuUI menu = host.AddComponent<QuoraShelterMenuUI>();

            menu.canvasRoot = canvasRootTransform;

            menu.Build();

            instance = menu;

            return menu;

        }



        public static void CloseAny()

        {

            instance?.Hide();

        }



        private void Build()

        {

            RectTransform hostRect = transform as RectTransform;

            if (hostRect != null)

            {

                hostRect.anchorMin = Vector2.zero;

                hostRect.anchorMax = Vector2.one;

                hostRect.offsetMin = Vector2.zero;

                hostRect.offsetMax = Vector2.zero;

            }



            menuRoot = MenuUiBuilder.CreateFullScreenPanel(transform, "QuoraShelterMenuRoot", new Color(0f, 0f, 0f, 0.35f), blockRaycasts: true);

            menuRoot.SetActive(false);



            menuPanel = new GameObject("MenuPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));

            menuPanel.transform.SetParent(menuRoot.transform, false);



            Image panelImage = menuPanel.GetComponent<Image>();

            MenuUiBuilder.ApplyUiSprite(panelImage);

            panelImage.color = DarkMatterGenesisUiPalette.WithAlpha(DarkMatterGenesisUiPalette.DarkNavy, 0.96f);

            DarkMatterGenesisUiPalette.ApplyFuchsiaTrim(menuPanel);



            RectTransform panelRect = menuPanel.GetComponent<RectTransform>();

            panelRect.anchorMin = new Vector2(0.5f, 0.5f);

            panelRect.anchorMax = new Vector2(0.5f, 0.5f);

            panelRect.pivot = new Vector2(0.5f, 0.5f);

            panelRect.anchoredPosition = Vector2.zero;

            panelRect.sizeDelta = new Vector2(280f, 0f);



            VerticalLayoutGroup layout = menuPanel.GetComponent<VerticalLayoutGroup>();

            layout.padding = new RectOffset(18, 18, 16, 16);

            layout.spacing = 8f;

            layout.childAlignment = TextAnchor.UpperCenter;

            layout.childControlWidth = true;

            layout.childControlHeight = true;

            layout.childForceExpandWidth = true;

            layout.childForceExpandHeight = false;



            ContentSizeFitter fitter = menuPanel.GetComponent<ContentSizeFitter>();

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;



            MenuUiBuilder.CreateTitle(menuPanel.transform, "Quora Shelter", 22f);

            timerText = CreateBodyLabel(menuPanel.transform, string.Empty);



            Button exitButton = MenuUiBuilder.CreateButton(menuPanel.transform, "Exit Shelter", new Vector2(240f, 40f), 17f);

            exitButton.onClick.RemoveAllListeners();

            exitButton.onClick.AddListener(OnExitClicked);



            Button storeButton = MenuUiBuilder.CreateButton(menuPanel.transform, "Exit and Store", new Vector2(240f, 40f), 17f);

            storeButton.onClick.RemoveAllListeners();

            storeButton.onClick.AddListener(OnExitAndStoreClicked);



            Button closeButton = MenuUiBuilder.CreateButton(menuPanel.transform, "Cancel", new Vector2(240f, 34f), 15f);

            closeButton.onClick.RemoveAllListeners();

            closeButton.onClick.AddListener(Hide);

        }



        private static TextMeshProUGUI CreateBodyLabel(Transform parent, string text)

        {

            GameObject textObject = new GameObject("Body", typeof(RectTransform));

            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();

            TmpUiHelper.ApplyDefaultFont(label);

            label.text = text;

            label.fontSize = 16f;

            label.color = DarkMatterGenesisUiPalette.MutedText;

            label.alignment = TextAlignmentOptions.Center;

            label.raycastTarget = false;

            return label;

        }



        public void Show(QuoraShelterController shelter)

        {

            if (shelter == null)

                return;



            activeShelter = shelter;

            EnsureUiInput(canvasRoot);

            menuRoot.SetActive(true);

            menuRoot.transform.SetAsLastSibling();

            if (transform.parent != null)

                UiFrontLayer.BringLayerToFront(transform.parent);



            boundPlayer = PlayerLocator.FindPlayerController();

            boundPlayer?.SetBuildingControlOpen(true);

            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonQuoraShelterMenu, true);

            ApplyMenuCursorFree();

            RefreshLabels();

        }



        private void Update()

        {

            if (!IsOpen)

                return;



            ApplyMenuCursorFree();



            if (UiEscapeGate.TryConsumeEscape())

            {

                Hide();

                return;

            }



            RefreshLabels();

        }



        private void RefreshLabels()

        {

            if (activeShelter == null)

                return;



            int minutes = Mathf.FloorToInt(activeShelter.RemainingLifetimeSeconds / 60f);

            int seconds = Mathf.FloorToInt(activeShelter.RemainingLifetimeSeconds % 60f);

            timerText.text = $"Deploy time remaining: {minutes:00}:{seconds:00}";

        }



        private void OnExitClicked()

        {

            activeShelter?.TryExitShelter(storeInInventory: false);

            Hide();

        }



        private void OnExitAndStoreClicked()

        {

            activeShelter?.TryExitShelter(storeInInventory: true);

            Hide();

        }



        public void Hide()

        {

            activeShelter = null;

            if (menuRoot != null)

                menuRoot.SetActive(false);



            if (boundPlayer != null)

            {

                boundPlayer.SetBuildingControlOpen(false);

                boundPlayer.ApplyCursorState();

                boundPlayer = null;

            }

            else

            {

                PlayerController player = PlayerLocator.FindPlayerController();

                player?.SetBuildingControlOpen(false);

                player?.ApplyCursorState();

            }



            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonQuoraShelterMenu, false);

        }



        private void ApplyMenuCursorFree()

        {

            Cursor.lockState = CursorLockMode.None;

            Cursor.visible = true;

            boundPlayer?.ApplyCursorState();



            if (IsOpen)

            {

                Cursor.lockState = CursorLockMode.None;

                Cursor.visible = true;

            }

        }



        private static void EnsureUiInput(Transform canvasRootTransform)

        {

            Canvas canvas = canvasRootTransform != null

                ? canvasRootTransform.GetComponent<Canvas>() ?? canvasRootTransform.GetComponentInParent<Canvas>()

                : null;

            if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)

                canvas.gameObject.AddComponent<GraphicRaycaster>();



            if (FindAnyObjectByType<EventSystem>() != null)

                return;



            GameObject eventSystemObject = new GameObject("EventSystem");

            eventSystemObject.AddComponent<EventSystem>();

            eventSystemObject.AddComponent<InputSystemUIInputModule>();

        }

    }

}



using Project.Pet;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.UI
{
    public class PetContextMenu : MonoBehaviour
    {
        private static PetContextMenu instance;

        private GameObject menuRoot;
        private GameObject menuPanel;
        private Transform canvasRoot;
        private PetController activePet;
        private int openedOnFrame = -1;

        public static PetContextMenu Instance => instance;

        public static void HideAny()
        {
            instance?.Hide();
        }

        public static PetContextMenu EnsureExists(Transform canvasRootTransform)
        {
            if (instance != null)
            {
                instance.canvasRoot = canvasRootTransform;
                return instance;
            }

            GameObject host = new GameObject("PetContextMenu", typeof(RectTransform));
            host.transform.SetParent(canvasRootTransform, false);
            PetContextMenu menu = host.AddComponent<PetContextMenu>();
            menu.canvasRoot = canvasRootTransform;
            menu.Build();
            instance = menu;
            return menu;
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

            menuRoot = MenuUiBuilder.CreateFullScreenPanel(transform, "PetContextMenuRoot", Color.clear, blockRaycasts: false);
            menuRoot.SetActive(false);

            GameObject dismissOverlay = MenuUiBuilder.CreateFullScreenPanel(menuRoot.transform, "DismissOverlay", new Color(0f, 0f, 0f, 0.01f), blockRaycasts: true);
            dismissOverlay.transform.SetAsFirstSibling();
            EventTrigger dismissTrigger = dismissOverlay.AddComponent<EventTrigger>();
            EventTrigger.Entry clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            clickEntry.callback.AddListener(_ => Hide());
            dismissTrigger.triggers.Add(clickEntry);

            menuPanel = new GameObject("MenuPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            menuPanel.transform.SetParent(menuRoot.transform, false);

            Image panelImage = menuPanel.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(panelImage);
            panelImage.color = SurvivalPioneerUiPalette.WithAlpha(SurvivalPioneerUiPalette.PanelBackground, 0.98f);
            panelImage.raycastTarget = true;

            RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.sizeDelta = new Vector2(200f, 0f);

            VerticalLayoutGroup layout = menuPanel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = menuPanel.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CreateMenuButton("Assign to Companion", AssignActivePet);
            CreateMenuButton("Clear Companion", ClearActivePet);

            menuRoot.SetActive(false);
        }

        private void CreateMenuButton(string label, System.Action action)
        {
            Button button = MenuUiBuilder.CreateButton(menuPanel.transform, label, new Vector2(184f, 34f), 16f);
            button.name = label.Replace(" ", string.Empty) + "Button";
            button.onClick.AddListener(() =>
            {
                action?.Invoke();
                Hide();
            });
        }

        public void Show(PetController pet, Vector2 screenPosition)
        {
            if (pet == null || !pet.IsOwned)
                return;

            PetHoverTooltip.HideAny();
            activePet = pet;
            openedOnFrame = Time.frameCount;
            menuRoot.SetActive(true);

            bool isAssigned = PetManager.Instance != null && PetManager.Instance.ToolbarPet == pet;
            SetButtonVisible("Assign to Companion", !isAssigned);
            SetButtonVisible("Clear Companion", isAssigned);

            if (canvasRoot == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                canvasRoot = canvas != null ? canvas.transform : null;
            }

            if (canvasRoot != null)
                UiFrontLayer.ReparentFullScreenToFront(transform, canvasRoot);

            menuRoot.transform.SetAsLastSibling();

            RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
            panelRect.position = screenPosition;
            ClampToScreen(panelRect);
        }

        public void Hide()
        {
            activePet = null;
            if (menuRoot != null)
                menuRoot.SetActive(false);

            if (canvasRoot != null)
            {
                transform.SetParent(canvasRoot, false);
                RectTransform hostRect = transform as RectTransform;
                if (hostRect != null)
                {
                    hostRect.anchorMin = Vector2.zero;
                    hostRect.anchorMax = Vector2.one;
                    hostRect.offsetMin = Vector2.zero;
                    hostRect.offsetMax = Vector2.zero;
                }
            }
        }

        private void AssignActivePet()
        {
            if (activePet == null || PetManager.Instance == null)
                return;

            PetManager.Instance.TryAssignToolbarPet(activePet);
        }

        private void ClearActivePet()
        {
            if (PetManager.Instance == null)
                return;

            if (activePet != null && PetManager.Instance.ToolbarPet == activePet)
                PetManager.Instance.ClearToolbarPet();
        }

        private void SetButtonVisible(string labelPrefix, bool visible)
        {
            Transform buttonTransform = menuPanel.transform.Find(labelPrefix.Replace(" ", string.Empty) + "Button");
            if (buttonTransform != null)
                buttonTransform.gameObject.SetActive(visible);
        }

        private static void ClampToScreen(RectTransform panelRect)
        {
            Vector3[] corners = new Vector3[4];
            panelRect.GetWorldCorners(corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            Vector2 offset = Vector2.zero;

            if (max.x > Screen.width)
                offset.x = Screen.width - max.x;
            if (min.y < 0f)
                offset.y = -min.y;
            if (max.y > Screen.height)
                offset.y = Screen.height - max.y;
            if (min.x < 0f)
                offset.x = -min.x;

            panelRect.position += (Vector3)offset;
        }

        private void Update()
        {
            if (menuRoot == null || !menuRoot.activeSelf)
                return;

            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Hide();
                return;
            }

            if (Time.frameCount == openedOnFrame)
                return;

            if (UnityEngine.InputSystem.Mouse.current != null &&
                UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame)
                Hide();
        }
    }
}

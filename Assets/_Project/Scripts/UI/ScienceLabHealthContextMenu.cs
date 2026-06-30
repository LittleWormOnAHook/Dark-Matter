using System;
using System.Collections.Generic;
using Project.Pioneers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Project.UI
{
    public class ScienceLabHealthContextMenu : MonoBehaviour
    {
        private static ScienceLabHealthContextMenu instance;

        private GameObject menuRoot;
        private GameObject menuPanel;
        private BuildingControlPanelUI panel;
        private Transform canvasRoot;
        private readonly List<Button> dynamicButtons = new List<Button>(4);

        public static void HideAny()
        {
            instance?.Hide();
        }

        public static ScienceLabHealthContextMenu EnsureExists(Transform canvasRootTransform, BuildingControlPanelUI buildingPanel)
        {
            if (instance != null)
            {
                instance.panel = buildingPanel;
                instance.canvasRoot = canvasRootTransform;
                return instance;
            }

            GameObject host = new GameObject("ScienceLabHealthContextMenu", typeof(RectTransform));
            host.transform.SetParent(canvasRootTransform, false);
            ScienceLabHealthContextMenu menu = host.AddComponent<ScienceLabHealthContextMenu>();
            menu.panel = buildingPanel;
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

            menuRoot = MenuUiBuilder.CreateFullScreenPanel(transform, "ScienceLabHealthContextMenuRoot", Color.clear, blockRaycasts: false);
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
            panelImage.color = new Color(0.08f, 0.09f, 0.12f, 0.98f);

            RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.sizeDelta = new Vector2(200f, 0f);

            VerticalLayoutGroup layout = menuPanel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 4;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = menuPanel.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public void ShowInjuredRow(string pioneerId, Vector2 screenPosition)
        {
            if (panel == null || string.IsNullOrEmpty(pioneerId))
                return;

            ClearDynamicButtons();
            AddButton("Reassign", () => panel.TryReassignInjuredPioneer(pioneerId));
            Present(screenPosition);
        }

        private void AddButton(string label, Action action)
        {
            Button button = MenuUiBuilder.CreateButton(menuPanel.transform, label, new Vector2(184f, 34f), 16f);
            button.name = label.Replace(" ", string.Empty) + "ContextButton";
            button.onClick.AddListener(() =>
            {
                action?.Invoke();
                Hide();
            });
            dynamicButtons.Add(button);
        }

        private void ClearDynamicButtons()
        {
            for (int i = dynamicButtons.Count - 1; i >= 0; i--)
            {
                if (dynamicButtons[i] != null)
                    Destroy(dynamicButtons[i].gameObject);
            }

            dynamicButtons.Clear();
        }

        private void Present(Vector2 screenPosition)
        {
            if (canvasRoot == null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                canvasRoot = canvas != null ? canvas.transform : null;
            }

            menuRoot.SetActive(true);

            if (canvasRoot != null)
                UiFrontLayer.ReparentFullScreenToFront(transform, canvasRoot);

            menuRoot.transform.SetAsLastSibling();

            RectTransform panelRect = menuPanel.GetComponent<RectTransform>();
            panelRect.position = screenPosition;
            ClampToScreen(panelRect);
        }

        private static void ClampToScreen(RectTransform panelRect)
        {
            Canvas canvas = panelRect.GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            RectTransform canvasRect = canvas.transform as RectTransform;
            Vector3[] corners = new Vector3[4];
            panelRect.GetWorldCorners(corners);

            Vector3 delta = Vector3.zero;
            if (corners[2].x > canvasRect.position.x + canvasRect.rect.width * 0.5f)
                delta.x = canvasRect.position.x + canvasRect.rect.width * 0.5f - corners[2].x;
            if (corners[0].x < canvasRect.position.x - canvasRect.rect.width * 0.5f)
                delta.x = canvasRect.position.x - canvasRect.rect.width * 0.5f - corners[0].x;
            if (corners[1].y > canvasRect.position.y + canvasRect.rect.height * 0.5f)
                delta.y = canvasRect.position.y + canvasRect.rect.height * 0.5f - corners[1].y;
            if (corners[0].y < canvasRect.position.y - canvasRect.rect.height * 0.5f)
                delta.y = canvasRect.position.y - canvasRect.rect.height * 0.5f - corners[0].y;

            panelRect.position += delta;
        }

        public void Hide()
        {
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
    }

    internal class ScienceLabHealthRowHandler : MonoBehaviour, IPointerClickHandler
    {
        private string pioneerId;
        private BuildingControlPanelUI panel;

        public void Configure(BuildingControlPanelUI ownerPanel, string id)
        {
            panel = ownerPanel;
            pioneerId = id;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right || panel == null)
                return;

            ScienceLabHealthContextMenu menu = ScienceLabHealthContextMenu.EnsureExists(
                panel.transform,
                panel);
            menu.ShowInjuredRow(pioneerId, eventData.position);
        }
    }
}

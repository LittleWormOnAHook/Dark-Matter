using Project.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public class AboutPanelController : MonoBehaviour
    {
        private const float WindowWidth = 420f;
        private const float WindowHeight = 280f;

        private GameObject panelRoot;
        private TextMeshProUGUI bodyLabel;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        public void Build(Transform parent)
        {
            if (panelRoot != null)
                return;

            panelRoot = MenuUiBuilder.CreateFullScreenPanel(
                parent,
                "AboutPanel",
                SurvivalPioneerUiPalette.WithAlpha(Color.black, 0.82f),
                blockRaycasts: true);

            GameObject window = new GameObject("AboutWindow", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            window.transform.SetParent(panelRoot.transform, false);

            Image windowImage = window.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(windowImage);
            SurvivalPioneerUiPalette.ApplyPanelShellBackground(windowImage, 0.98f);
            SurvivalPioneerUiPalette.ApplyFuchsiaTrim(window);

            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.sizeDelta = new Vector2(WindowWidth, WindowHeight);

            VerticalLayoutGroup windowLayout = window.GetComponent<VerticalLayoutGroup>();
            windowLayout.padding = new RectOffset(20, 20, 18, 18);
            windowLayout.spacing = 10;
            windowLayout.childAlignment = TextAnchor.UpperCenter;
            windowLayout.childControlWidth = true;
            windowLayout.childControlHeight = true;
            windowLayout.childForceExpandWidth = true;
            windowLayout.childForceExpandHeight = false;

            TextMeshProUGUI title = MenuUiBuilder.CreateTitle(window.transform, "About", 24f);
            title.alignment = TextAlignmentOptions.Center;

            TextMeshProUGUI subtitle = MenuUiBuilder.CreateTitle(window.transform, GameVersionInfo.AboutTitle, 18f);
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.color = SurvivalPioneerUiPalette.MutedText;

            bodyLabel = MenuUiBuilder.CreateTitle(window.transform, GameVersionInfo.AboutBody, 16f);
            bodyLabel.alignment = TextAlignmentOptions.Center;
            bodyLabel.color = SurvivalPioneerUiPalette.BodyText;
            bodyLabel.enableWordWrapping = true;

            MenuUiBuilder.CreateTopRightBackButton(
                panelRoot.transform,
                Close,
                width: 88f,
                height: 30f,
                fontSize: 14f,
                inset: 14f);

            panelRoot.SetActive(false);
        }

        public void Open()
        {
            if (panelRoot == null)
                return;

            RefreshBodyText();
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
        }

        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void RefreshBodyText()
        {
            if (bodyLabel != null)
                bodyLabel.text = GameVersionInfo.AboutBody;
        }
    }
}

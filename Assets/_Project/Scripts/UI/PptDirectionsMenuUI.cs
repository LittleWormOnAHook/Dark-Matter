using System.Collections.Generic;
using Project.Player;
using Project.PPT;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Project.UI
{
    public sealed class PptDirectionsMenuUI : MonoBehaviour
    {
        private static PptDirectionsMenuUI instance;

        private GameObject overlayRoot;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI bodyText;
        private Transform choicesRoot;
        private Button moreButton;
        private bool built;
        private PptNpcInteractor currentInteractor;
        private int currentPage;
        private CanvasGroup overlayCanvasGroup;
        private NpcDialogProximityFade proximityFade;

        public static bool IsOpen => instance != null && instance.overlayRoot != null && instance.overlayRoot.activeSelf;

        public static void CloseAnyOpen()
        {
            if (instance != null && IsOpen)
                instance.Close();
        }

        public static PptDirectionsMenuUI EnsureExists(Transform canvasRoot)
        {
            if (instance != null)
                return instance;

            GameObject host = new GameObject("PptDirectionsMenuUI", typeof(RectTransform));
            host.transform.SetParent(canvasRoot, false);
            MenuUiBuilder.StretchRectToFill(host.GetComponent<RectTransform>());
            instance = host.AddComponent<PptDirectionsMenuUI>();
            instance.Build(canvasRoot);
            return instance;
        }

        public void Show(PptNpcInteractor interactor)
        {
            if (!built)
            {
                Canvas canvas = FindAnyObjectByType<Canvas>();
                if (canvas != null)
                    Build(canvas.transform);
            }

            currentInteractor = interactor;
            currentPage = 0;
            RefreshPage();
            OpenOverlay();
        }

        private void Build(Transform canvasRoot)
        {
            EnsureUiInput(canvasRoot);

            overlayRoot = MenuUiBuilder.CreateFullScreenPanel(
                transform,
                "PptDirectionsOverlay",
                new Color(0f, 0f, 0f, 0.55f),
                blockRaycasts: true);
            overlayCanvasGroup = overlayRoot.GetComponent<CanvasGroup>();
            if (overlayCanvasGroup == null)
                overlayCanvasGroup = overlayRoot.AddComponent<CanvasGroup>();
            proximityFade = GetComponent<NpcDialogProximityFade>();
            if (proximityFade == null)
                proximityFade = gameObject.AddComponent<NpcDialogProximityFade>();

            GameObject shell = MenuUiBuilder.CreateCenteredModalShell(
                overlayRoot.transform,
                "Directions",
                new Vector2(720f, 520f),
                out RectTransform contentArea,
                out Button closeButton);

            DarkMatterGenesisUiPalette.ApplyPanelShellBackground(shell.GetComponent<Image>(), 0.98f);
            DarkMatterGenesisUiPalette.ApplyFuchsiaTrim(shell);
            titleText = MenuUiBuilder.GetShellTitleText(shell);
            closeButton.onClick.AddListener(Close);

            GameObject body = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            body.transform.SetParent(contentArea, false);
            RectTransform bodyRect = body.GetComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 0.72f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(24f, 0f);
            bodyRect.offsetMax = new Vector2(-24f, -12f);
            bodyText = body.GetComponent<TextMeshProUGUI>();
            bodyText.text = "Where do you need to go?";
            bodyText.fontSize = 28f;
            bodyText.color = DarkMatterGenesisUiPalette.BodyText;
            bodyText.raycastTarget = false;
            ShiftUiTheme.Current?.ApplyFont(bodyText);

            GameObject choices = new GameObject("Choices", typeof(RectTransform), typeof(VerticalLayoutGroup));
            choices.transform.SetParent(contentArea, false);
            choicesRoot = choices.transform;
            RectTransform choicesRect = choices.GetComponent<RectTransform>();
            choicesRect.anchorMin = new Vector2(0f, 0.12f);
            choicesRect.anchorMax = new Vector2(1f, 0.7f);
            choicesRect.offsetMin = new Vector2(24f, 0f);
            choicesRect.offsetMax = new Vector2(-24f, 0f);
            VerticalLayoutGroup layout = choices.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            moreButton = MenuUiBuilder.CreateButton(contentArea, "More...", new Vector2(220f, 56f), 30f);
            RectTransform moreRect = moreButton.GetComponent<RectTransform>();
            moreRect.anchorMin = new Vector2(0.5f, 0f);
            moreRect.anchorMax = new Vector2(0.5f, 0f);
            moreRect.pivot = new Vector2(0.5f, 0f);
            moreRect.anchoredPosition = new Vector2(0f, 16f);
            moreButton.onClick.AddListener(NextPage);

            overlayRoot.SetActive(false);
            built = true;
        }

        private void OpenOverlay()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            player?.SetQuestDialogOpen(true);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonPptDirections, true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            overlayRoot.SetActive(true);
            overlayRoot.transform.SetAsLastSibling();
            UiFrontLayer.BringLayerToFront(transform.parent);

            if (overlayCanvasGroup != null)
                overlayCanvasGroup.alpha = 1f;

            proximityFade?.BeginMonitoring(currentInteractor != null ? currentInteractor.transform : null, overlayCanvasGroup, Close);
        }

        private void RefreshPage()
        {
            if (currentInteractor == null)
            {
                Close();
                return;
            }

            PptManager manager = PptManager.Instance;
            if (manager == null)
            {
                Close();
                return;
            }

            PptNpcProfile profile = currentInteractor.Profile;
            int pageSize = profile != null ? profile.DirectionChoicesPerPage : 3;
            List<PptEntry> choices = manager.GetDirectionCandidates(
                currentInteractor.NpcId,
                currentInteractor.transform.position,
                pageSize,
                currentPage,
                out int totalCount);

            string speaker = currentInteractor.SpeakerDisplayName;

            titleText.text = speaker;
            bodyText.text = totalCount == 0
                ? "I don't have any places to point you toward yet."
                : "Pick a place you've heard about:";

            ClearChoices();
            for (int i = 0; i < choices.Count; i++)
            {
                PptEntry entry = choices[i];
                if (entry == null)
                    continue;

                Button button = MenuUiBuilder.CreateButton(choicesRoot, entry.DisplayName, new Vector2(640f, 56f), 30f);
                PptEntry captured = entry;
                button.onClick.AddListener(() => OnChoice(captured));
            }

            int totalPages = Mathf.CeilToInt(totalCount / (float)pageSize);
            bool hasMore = currentPage + 1 < totalPages;
            moreButton.gameObject.SetActive(hasMore && totalCount > 0);
        }

        private void OnChoice(PptEntry entry)
        {
            PptNpcInteractor interactor = currentInteractor;
            Close();
            interactor?.HandleDirectionChoice(entry);
        }

        private void NextPage()
        {
            currentPage++;
            RefreshPage();
        }

        private void ClearChoices()
        {
            for (int i = choicesRoot.childCount - 1; i >= 0; i--)
                Destroy(choicesRoot.GetChild(i).gameObject);
        }

        private void Close()
        {
            proximityFade?.StopMonitoring();

            if (overlayRoot != null)
                overlayRoot.SetActive(false);

            if (overlayCanvasGroup != null)
                overlayCanvasGroup.alpha = 1f;

            currentInteractor = null;
            currentPage = 0;

            PlayerController player = FindAnyObjectByType<PlayerController>();
            player?.SetQuestDialogOpen(false);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonPptDirections, false);
        }

        private static void EnsureUiInput(Transform canvasRoot)
        {
            Canvas canvas = canvasRoot.GetComponent<Canvas>() ?? canvasRoot.GetComponentInParent<Canvas>();
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

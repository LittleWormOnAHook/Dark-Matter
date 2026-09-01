using System;
using System.Collections;
using System.Collections.Generic;
using Project.Audio;
using Project.Core;
using Project.Interaction;
using Project.Player;
using Project.Quests;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK NPC conversation + quest board window. Same show/hide APIs as QuestGiverDialogUI.
    /// </summary>
    [DefaultExecutionOrder(-379)]
    [DisallowMultipleComponent]
    public class DMUiToolkitDialogue : MonoBehaviour
    {
        private static DMUiToolkitDialogue instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement panel;
        private Label titleLabel;
        private Label bodyLabel;
        private VisualElement simpleRoot;
        private VisualElement boardRoot;
        private VisualElement questList;
        private Label leftTitle;
        private Label leftDesc;
        private Label leftObj;
        private Label rightStatus;
        private Label rightProgress;
        private Label rightXp;
        private Label rightRewards;
        private Button continueButton;
        private Button directionsButton;
        private Button boardDirectionsButton;
        private Button questActionButton;
        private Button abandonButton;
        private Button closeButton;
        private bool bound;
        private bool open;
        private bool wired;
        private IList<QuestBoardEntry> currentEntries;
        private int selectedEntryIndex = -1;
        private bool abandonConfirmPending;
        private Action onClosed;
        private Action onDirectionsRequested;
        private Transform npcAnchor;
        private bool proximityFading;
        private float proximityFadeElapsed;
        private float proximityStartOpacity = 1f;

        public static bool IsOpen => instance != null && instance.open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            EnsureHost();
        }

        public static DMUiToolkitDialogue EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.DialogueName,
                DMUiToolkitOverlayDocument.DialogueUxml,
                DMUiToolkitOverlayDocument.DialogueUss,
                DMUiToolkitOverlayDocument.DialogueSort);
            if (doc == null)
                return null;

            DMUiToolkitDialogue host = doc.GetComponent<DMUiToolkitDialogue>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitDialogue>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        public static bool TryShowSimple(
            string speakerName,
            string message,
            Action closedCallback,
            string primaryLabel,
            Action directionsCallback,
            Transform npcAnchor)
        {
            if (!DMUiToolkitHud.IsDriving)
                return false;

            DMUiToolkitDialogue host = EnsureHost();
            if (host == null)
                return false;

            host.PresentSimple(speakerName, message, closedCallback, primaryLabel, directionsCallback, npcAnchor);
            return true;
        }

        public static bool TryShowQuestBoard(
            string speakerName,
            string introMessage,
            IList<QuestBoardEntry> entries,
            Action closedCallback,
            Action directionsCallback,
            Transform npcAnchor)
        {
            if (!DMUiToolkitHud.IsDriving)
                return false;

            DMUiToolkitDialogue host = EnsureHost();
            if (host == null)
                return false;

            host.PresentBoard(speakerName, introMessage, entries, closedCallback, directionsCallback, npcAnchor);
            return true;
        }

        public static void Hide()
        {
            instance?.CloseInternal(invokeClosed: true);
        }

        private void Awake()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void LateUpdate()
        {
            if (!bound)
                BindTree();

            if (open && !DMUiToolkitHud.IsDriving)
                CloseInternal(invokeClosed: true);

            HideUguiDialog();
            TickProximityFade();
        }

        internal void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("dialogue-root") ?? tree;
            panel = tree.Q<VisualElement>("dialogue-panel");
            titleLabel = tree.Q<Label>("dialogue-title");
            bodyLabel = tree.Q<Label>("dialogue-body");
            simpleRoot = tree.Q<VisualElement>("dialogue-simple");
            boardRoot = tree.Q<VisualElement>("dialogue-board");
            questList = tree.Q<VisualElement>("dialogue-quest-list");
            leftTitle = tree.Q<Label>("dialogue-quest-title");
            leftDesc = tree.Q<Label>("dialogue-quest-desc");
            leftObj = tree.Q<Label>("dialogue-quest-obj");
            rightStatus = tree.Q<Label>("dialogue-quest-status");
            rightProgress = tree.Q<Label>("dialogue-quest-progress");
            rightXp = tree.Q<Label>("dialogue-quest-xp");
            rightRewards = tree.Q<Label>("dialogue-quest-rewards");
            continueButton = tree.Q<Button>("dialogue-continue");
            directionsButton = tree.Q<Button>("dialogue-directions");
            boardDirectionsButton = tree.Q<Button>("dialogue-board-directions");
            questActionButton = tree.Q<Button>("dialogue-quest-action");
            abandonButton = tree.Q<Button>("dialogue-abandon");
            closeButton = tree.Q<Button>("dialogue-close");

            WireButtons();

            if (!open)
                DMUiToolkitOverlayDocument.SetShown(root, false);

            bound = root != null;
        }

        private void WireButtons()
        {
            if (wired)
                return;

            if (continueButton != null)
                continueButton.clicked += () => CloseInternal(true);
            if (closeButton != null)
                closeButton.clicked += () => CloseInternal(true);
            if (directionsButton != null)
                directionsButton.clicked += HandleDirections;
            if (boardDirectionsButton != null)
                boardDirectionsButton.clicked += HandleDirections;
            if (questActionButton != null)
                questActionButton.clicked += HandleQuestAction;
            if (abandonButton != null)
                abandonButton.clicked += HandleAbandon;

            VisualElement veil = document != null ? document.rootVisualElement.Q<VisualElement>("dialogue-veil") : null;
            if (veil != null)
                veil.RegisterCallback<ClickEvent>(_ => CloseInternal(true));

            wired = true;
        }

        private void PresentSimple(
            string speakerName,
            string message,
            Action closedCallback,
            string primaryLabel,
            Action directionsCallback,
            Transform anchor)
        {
            BindTree();
            onClosed = closedCallback;
            onDirectionsRequested = directionsCallback;
            npcAnchor = anchor;

            if (titleLabel != null)
                titleLabel.text = string.IsNullOrEmpty(speakerName) ? "Quest Giver" : speakerName;
            if (bodyLabel != null)
                bodyLabel.text = message ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(message))
            {
                string speaker = string.IsNullOrEmpty(speakerName) ? "Quest Giver" : speakerName;
                DMGameLog.Add(speaker + ": " + message, DMGameLogKind.Dialogue);
            }

            DMUiToolkitOverlayDocument.SetShown(simpleRoot, true);
            DMUiToolkitOverlayDocument.SetShown(boardRoot, false);

            if (continueButton != null)
            {
                continueButton.text = string.IsNullOrEmpty(primaryLabel) ? "Continue" : primaryLabel;
                continueButton.SetEnabled(true);
            }

            ApplyDirections();
            OpenOverlay();
        }

        private void PresentBoard(
            string speakerName,
            string introMessage,
            IList<QuestBoardEntry> entries,
            Action closedCallback,
            Action directionsCallback,
            Transform anchor)
        {
            BindTree();
            onClosed = closedCallback;
            onDirectionsRequested = directionsCallback;
            npcAnchor = anchor;
            currentEntries = entries;
            selectedEntryIndex = entries != null && entries.Count > 0 ? 0 : -1;
            abandonConfirmPending = false;

            if (titleLabel != null)
                titleLabel.text = string.IsNullOrEmpty(speakerName) ? "Quest Giver" : speakerName;

            DMUiToolkitOverlayDocument.SetShown(simpleRoot, false);
            DMUiToolkitOverlayDocument.SetShown(boardRoot, true);

            RebuildQuestList();

            if (selectedEntryIndex >= 0)
                SelectEntry(selectedEntryIndex);
            else
            {
                if (leftTitle != null)
                    leftTitle.text = "No quests available";
                if (leftDesc != null)
                    leftDesc.text = introMessage ?? string.Empty;
                if (leftObj != null)
                    leftObj.text = string.Empty;
                if (rightStatus != null)
                    rightStatus.text = string.Empty;
                if (rightProgress != null)
                    rightProgress.text = string.Empty;
                if (rightXp != null)
                    rightXp.text = string.Empty;
                if (rightRewards != null)
                    rightRewards.text = string.Empty;
                if (questActionButton != null)
                {
                    questActionButton.text = "Close";
                    questActionButton.SetEnabled(false);
                }

                DMUiToolkitOverlayDocument.SetShown(abandonButton, false);
            }

            ApplyDirections();
            OpenOverlay();
        }

        private void RebuildQuestList()
        {
            if (questList == null)
                return;

            questList.Clear();
            if (currentEntries == null)
                return;

            for (int i = 0; i < currentEntries.Count; i++)
            {
                int captured = i;
                QuestBoardEntry entry = currentEntries[i];
                Button row = new Button();
                row.AddToClassList("dmg-dq-quest-row");
                row.text = string.Empty;

                Label title = new Label(entry.Title);
                title.AddToClassList("dmg-dq-quest-row-title");
                title.pickingMode = PickingMode.Ignore;
                Label status = new Label(QuestUiPalette.GetStatusLabel(entry.Status));
                status.AddToClassList("dmg-dq-quest-row-status");
                status.pickingMode = PickingMode.Ignore;
                status.style.color = QuestUiPalette.GetStatusLabelColor(entry.Status, null);
                title.style.color = QuestUiPalette.GetTitleColor(entry.Status, null);

                row.Add(title);
                row.Add(status);
                row.clicked += () => SelectEntry(captured);
                ApplyRowBackground(row, entry, i == selectedEntryIndex);
                questList.Add(row);
            }
        }

        private void ApplyRowBackground(VisualElement row, QuestBoardEntry entry, bool selected)
        {
            row.style.backgroundColor = QuestUiPalette.GetRowBackgroundColor(entry.Status, selected, null);
        }

        private void SelectEntry(int index)
        {
            if (currentEntries == null || index < 0 || index >= currentEntries.Count)
                return;

            selectedEntryIndex = index;
            QuestBoardEntry entry = currentEntries[index];

            if (leftTitle != null)
                leftTitle.text = entry.Title;
            if (leftDesc != null)
                leftDesc.text = string.IsNullOrWhiteSpace(entry.Description) ? entry.Detail : entry.Description;
            if (leftObj != null)
            {
                leftObj.text = string.IsNullOrWhiteSpace(entry.ObjectivesSummary)
                    ? "Objectives unavailable."
                    : entry.ObjectivesSummary;
            }

            if (rightStatus != null)
            {
                rightStatus.text = QuestUiPalette.GetStatusLabel(entry.Status);
                rightStatus.style.color = QuestUiPalette.GetStatusLabelColor(entry.Status, null);
            }

            if (rightProgress != null)
            {
                rightProgress.text = string.IsNullOrWhiteSpace(entry.ProgressSummary)
                    ? "Progress unavailable."
                    : entry.ProgressSummary;
            }

            if (rightXp != null)
                rightXp.text = $"XP Reward: {Mathf.Max(0, entry.XpReward)}";

            if (rightRewards != null)
            {
                rightRewards.text = string.IsNullOrWhiteSpace(entry.RewardsSummary)
                    ? "No item rewards."
                    : entry.RewardsSummary;
            }

            if (questActionButton != null)
            {
                questActionButton.text = string.IsNullOrEmpty(entry.ActionLabel) ? "Continue" : entry.ActionLabel;
                questActionButton.SetEnabled(entry.CanSelect && entry.OnSelected != null);
            }

            bool canAbandon = entry.CanAbandon && entry.OnAbandon != null;
            DMUiToolkitOverlayDocument.SetShown(abandonButton, canAbandon);
            if (abandonButton != null)
            {
                abandonButton.SetEnabled(canAbandon);
                abandonButton.text = "Abandon Quest";
            }

            abandonConfirmPending = false;
            RefreshRowHighlights();
        }

        private void RefreshRowHighlights()
        {
            if (questList == null || currentEntries == null)
                return;

            for (int i = 0; i < questList.childCount && i < currentEntries.Count; i++)
                ApplyRowBackground(questList[i], currentEntries[i], i == selectedEntryIndex);
        }

        private void ApplyDirections()
        {
            bool show = onDirectionsRequested != null;
            DMUiToolkitOverlayDocument.SetShown(directionsButton, show);
            DMUiToolkitOverlayDocument.SetShown(boardDirectionsButton, show);
            directionsButton?.SetEnabled(show);
            boardDirectionsButton?.SetEnabled(show);
        }

        private void OpenOverlay()
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            player?.SetQuestDialogOpen(true);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonQuestDialog, true);
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            proximityFading = false;
            proximityFadeElapsed = 0f;
            if (root != null)
                root.style.opacity = 1f;

            DMUiToolkitOverlayDocument.SetShown(root, true);
            open = true;
            GameAudioManager.Instance?.PlayButtonClick();
        }

        private void HandleDirections()
        {
            Action callback = onDirectionsRequested;
            CloseInternal(invokeClosed: true);
            callback?.Invoke();
        }

        private void HandleQuestAction()
        {
            if (currentEntries == null || selectedEntryIndex < 0 || selectedEntryIndex >= currentEntries.Count)
            {
                CloseInternal(true);
                return;
            }

            QuestBoardEntry entry = currentEntries[selectedEntryIndex];
            if (!entry.CanSelect || entry.OnSelected == null)
            {
                CloseInternal(true);
                return;
            }

            Action callback = entry.OnSelected;
            CloseInternal(invokeClosed: true);
            callback.Invoke();
        }

        private void HandleAbandon()
        {
            if (currentEntries == null || selectedEntryIndex < 0 || selectedEntryIndex >= currentEntries.Count)
                return;

            QuestBoardEntry entry = currentEntries[selectedEntryIndex];
            if (!entry.CanAbandon || entry.OnAbandon == null)
                return;

            if (!abandonConfirmPending)
            {
                abandonConfirmPending = true;
                if (abandonButton != null)
                    abandonButton.text = "Confirm Abandon?";
                return;
            }

            abandonConfirmPending = false;
            Action callback = entry.OnAbandon;
            CloseInternal(invokeClosed: true);
            callback.Invoke();
        }

        private void CloseInternal(bool invokeClosed)
        {
            if (!open && root != null && root.resolvedStyle.display == DisplayStyle.None)
            {
                if (invokeClosed)
                {
                    Action callback = onClosed;
                    onClosed = null;
                    callback?.Invoke();
                }

                return;
            }

            open = false;
            proximityFading = false;
            npcAnchor = null;
            currentEntries = null;
            selectedEntryIndex = -1;
            abandonConfirmPending = false;
            onDirectionsRequested = null;
            questList?.Clear();

            DMUiToolkitOverlayDocument.SetShown(root, false);
            if (root != null)
                root.style.opacity = 1f;

            PlayerController player = FindAnyObjectByType<PlayerController>();
            player?.SetQuestDialogOpen(false);
            GameplayMenuTime.SetSlowMotion(GameplayMenuTime.ReasonQuestDialog, false);

            if (invokeClosed)
            {
                Action callback = onClosed;
                onClosed = null;
                callback?.Invoke();
            }
            else
            {
                onClosed = null;
            }
        }

        private void TickProximityFade()
        {
            if (!open || npcAnchor == null)
                return;

            if (!proximityFading)
            {
                if (!PlayerInteractionUtility.TryGetPlayerPosition(out Vector3 playerPosition))
                    return;

                Collider anchorCollider = npcAnchor.GetComponent<Collider>();
                float distance = PlayerInteractionUtility.DistanceToInteractable(
                    playerPosition,
                    anchorCollider,
                    npcAnchor.position);
                if (distance <= NpcDialogProximityFade.MaxDistanceMeters)
                    return;

                proximityFading = true;
                proximityFadeElapsed = 0f;
                proximityStartOpacity = root != null ? root.resolvedStyle.opacity : 1f;
            }

            proximityFadeElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(proximityFadeElapsed / NpcDialogProximityFade.FadeDurationSeconds);
            if (root != null)
                root.style.opacity = Mathf.Lerp(proximityStartOpacity, 0f, t);

            if (t >= 1f)
                CloseInternal(invokeClosed: true);
        }

        private static void HideUguiDialog()
        {
            if (!DMUiToolkitHud.IsDriving)
                return;

            QuestGiverDialogUI dialog = FindAnyObjectByType<QuestGiverDialogUI>(FindObjectsInactive.Include);
            if (dialog == null)
                return;

            Transform overlay = dialog.transform.Find("DialogOverlay");
            if (overlay != null && overlay.gameObject.activeSelf)
                overlay.gameObject.SetActive(false);
        }
    }
}

using System;
using System.Collections.Generic;
using Project.Core;
using Project.Interaction;
using Project.UI;
using UnityEngine;

namespace Project.Quests
{
    [RequireComponent(typeof(Collider))]
    [DefaultExecutionOrder(100)]
    public class QuestGiverNpc : MonoBehaviour, IWorldUsable
    {
        [Header("Identity")]
        [SerializeField] private string npcId = "pioneer_guide";
        [SerializeField] private string displayName = "Pioneer Guide";

        [Header("Quest Offers")]
        [SerializeField] private QuestGiverOffer[] questOffers;

        [Header("Interaction")]
        [SerializeField] private string promptText = "Press E to use";
        [SerializeField] private float interactRange = 3.5f;
        [TextArea(2, 3)]
        [SerializeField] private string questBoardIntro = "Pick a quest below to accept, track progress, or claim rewards.";

        [Header("Animation")]
        [SerializeField] private RuntimeAnimatorController idleAnimatorController;
        [SerializeField] private string idleStateName = "Idle";
        [SerializeField] private bool lockVisualTransform = true;

        private UIManager uiManager;
        private QuestManager questManager;
        private Collider interactCollider;
        private bool playerInRange;
        private Animator idleAnimator;
        private Transform visualRoot;
        private Vector3 visualBaseLocalPosition;
        private Quaternion visualBaseLocalRotation;

        public string NpcId => npcId;
        public bool IsPlayerInRange => playerInRange;
        public QuestGiverOffer[] QuestOffers => questOffers;

        private void Awake()
        {
            interactCollider = GetComponent<Collider>();
            if (interactCollider == null)
                interactCollider = GetComponentInChildren<Collider>();

            if (interactCollider != null)
                interactCollider.isTrigger = true;
        }

        private void Update()
        {
            RefreshProximityState();
        }

        private void RefreshProximityState()
        {
            if (!GameSession.HasStarted)
                return;

            bool nearby = IsPlayerNearby();
            if (nearby == playerInRange)
                return;

            playerInRange = nearby;
            if (playerInRange)
                ShowPrompt();
            else
                ResolveUiManager()?.HideInteractionPrompt();
        }

        private void LateUpdate()
        {
            RestoreVisualAnchor();
        }

        private void ApplyIdleAnimation()
        {
            if (idleAnimatorController == null)
                return;

            Animator animator = EnsureAnimatorOnVisual();
            if (animator == null)
                return;

            idleAnimator = animator;
            if (animator.runtimeAnimatorController != idleAnimatorController)
                animator.runtimeAnimatorController = idleAnimatorController;

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            CacheVisualAnchor();
            TryPlayIdleState(animator);
        }

        private Animator EnsureAnimatorOnVisual()
        {
            Transform visual = transform.Find("Body");
            if (visual != null)
            {
                Animator bodyAnimator = visual.GetComponent<Animator>();
                if (bodyAnimator == null)
                    bodyAnimator = visual.gameObject.AddComponent<Animator>();
                return bodyAnimator;
            }

            Animator childAnimator = GetComponentInChildren<Animator>(true);
            if (childAnimator != null)
                return childAnimator;

            return gameObject.GetComponent<Animator>() ?? gameObject.AddComponent<Animator>();
        }

        private void TryPlayIdleState(Animator animator)
        {
            if (animator == null)
                return;

            string[] candidates = BuildIdleStateCandidates();
            for (int i = 0; i < candidates.Length; i++)
            {
                int stateHash = Animator.StringToHash(candidates[i]);
                if (!animator.HasState(0, stateHash))
                    continue;

                animator.Play(stateHash, 0, 0f);
                return;
            }
        }

        private string[] BuildIdleStateCandidates()
        {
            List<string> candidates = new List<string>(4);
            AddIdleCandidate(candidates, idleStateName);
            AddIdleCandidate(candidates, "Idle");
            AddIdleCandidate(candidates, "Idle01");
            AddIdleCandidate(candidates, "idle");
            return candidates.ToArray();
        }

        private static void AddIdleCandidate(List<string> candidates, string stateName)
        {
            if (string.IsNullOrWhiteSpace(stateName))
                return;

            for (int i = 0; i < candidates.Count; i++)
            {
                if (string.Equals(candidates[i], stateName, StringComparison.Ordinal))
                    return;
            }

            candidates.Add(stateName);
        }

        private void CacheVisualAnchor()
        {
            if (!lockVisualTransform || idleAnimator == null)
                return;

            visualRoot = idleAnimator.transform;
            visualBaseLocalPosition = visualRoot.localPosition;
            visualBaseLocalRotation = visualRoot.localRotation;
        }

        private void RestoreVisualAnchor()
        {
            if (!lockVisualTransform || visualRoot == null)
                return;

            visualRoot.localPosition = visualBaseLocalPosition;
            visualRoot.localRotation = visualBaseLocalRotation;
        }

        private void OnEnable()
        {
            WorldUseController.Register(this);
        }

        private void OnDisable()
        {
            WorldUseController.Unregister(this);
        }

        private void Start()
        {
            uiManager = FindAnyObjectByType<UIManager>();
            questManager = QuestManager.EnsureExists();
            EnsureOffersAvailable();
            ApplyIdleAnimation();

            playerInRange = IsPlayerNearby();
            if (playerInRange)
                ShowPrompt();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!PlayerInteractionUtility.IsPlayerCollider(other))
                return;

            playerInRange = true;
            ShowPrompt();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!PlayerInteractionUtility.IsPlayerCollider(other))
                return;

            if (!IsPlayerNearby())
            {
                playerInRange = false;
                ResolveUiManager()?.HideInteractionPrompt();
            }
        }

        public static QuestGiverNpc GetInteractable(Vector3 playerPosition, float range)
        {
            QuestGiverNpc[] givers = FindObjectsByType<QuestGiverNpc>(FindObjectsInactive.Exclude);
            QuestGiverNpc best = null;
            float bestDistance = range;

            for (int i = 0; i < givers.Length; i++)
            {
                QuestGiverNpc giver = givers[i];
                if (giver == null || !giver.playerInRange)
                    continue;

                float distance = Vector3.Distance(playerPosition, giver.transform.position);
                if (distance <= giver.interactRange && distance <= bestDistance)
                {
                    best = giver;
                    bestDistance = distance;
                }
            }

            return best;
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (WorldUseController.IsPlayerFocusedOnPickup(context)
                || WorldUseController.HasCompetingNearbyItemPickup(context))
                return -1f;

            if (!WorldUseController.IsAimedAtQuestGiver(context, this, interactCollider))
                return -1f;

            if (!GameSession.HasStarted || !IsWithinInteractRange(context.PlayerPosition))
                return -1f;

            float distance = PlayerInteractionUtility.DistanceToInteractable(
                context.PlayerPosition,
                interactCollider,
                transform.position);
            return 95f - distance;
        }

        public bool TryUse(WorldUseContext context)
        {
            return TryInteract();
        }

        public bool TryInteract()
        {
            if (!IsPlayerNearby() || !GameSession.HasStarted)
                return false;

            playerInRange = true;

            EnsureQuestManager();
            if (questManager == null)
                return false;

            EnsureOffersAvailable();
            List<QuestBoardEntry> entries = BuildQuestBoardEntries();
            if (entries.Count == 0)
            {
                ShowDialogue("I don't have any quests for you right now.");
                return true;
            }

            uiManager?.HideInteractionPrompt();
            QuestGiverDialogUI.ShowQuestBoard(
                displayName,
                questBoardIntro,
                entries,
                () =>
                {
                    if (playerInRange)
                        ShowPrompt();
                });

            return true;
        }

        private List<QuestBoardEntry> BuildQuestBoardEntries()
        {
            List<QuestBoardEntry> entries = new List<QuestBoardEntry>();

            if (questOffers == null)
                return entries;

            for (int i = 0; i < questOffers.Length; i++)
            {
                QuestGiverOffer offer = questOffers[i];
                if (offer == null || offer.quest == null || !ArePrerequisitesMet(offer))
                    continue;

                string questIdValue = offer.QuestId;
                QuestProgress progress = questManager.GetProgress(questIdValue);
                QuestStatus status = progress?.status ?? QuestStatus.Available;

                switch (status)
                {
                    case QuestStatus.Available:
                    case QuestStatus.Locked:
                        questManager.MakeQuestAvailable(questIdValue);
                        entries.Add(new QuestBoardEntry
                        {
                            Title = offer.quest.title,
                            Detail = offer.GetOfferDialogue(),
                            ActionLabel = "Accept",
                            Status = QuestStatus.Available,
                            CanSelect = true,
                            OnSelected = () => AcceptQuest(offer)
                        });
                        break;

                    case QuestStatus.Active:
                        questManager.NotifyNpcTalked(npcId);
                        progress = questManager.GetProgress(questIdValue);
                        if (progress != null && progress.status == QuestStatus.Completed)
                        {
                            entries.Add(new QuestBoardEntry
                            {
                                Title = offer.quest.title,
                                Detail = offer.GetReadyDialogue(),
                                ActionLabel = "Claim Reward",
                                Status = QuestStatus.Completed,
                                CanSelect = true,
                                OnSelected = () => ClaimRewards(offer)
                            });
                        }
                        else
                        {
                            entries.Add(new QuestBoardEntry
                            {
                                Title = offer.quest.title,
                                Detail = offer.GetProgressDialogue(),
                                ActionLabel = "In Progress",
                                Status = QuestStatus.Active,
                                CanSelect = false
                            });
                        }
                        break;

                    case QuestStatus.Completed:
                        entries.Add(new QuestBoardEntry
                        {
                            Title = offer.quest.title,
                            Detail = offer.GetReadyDialogue(),
                            ActionLabel = "Claim Reward",
                            Status = QuestStatus.Completed,
                            CanSelect = true,
                            OnSelected = () => ClaimRewards(offer)
                        });
                        break;

                    case QuestStatus.TurnedIn:
                        entries.Add(new QuestBoardEntry
                        {
                            Title = offer.quest.title,
                            Detail = offer.GetDoneDialogue(),
                            ActionLabel = "Completed",
                            Status = QuestStatus.TurnedIn,
                            CanSelect = false
                        });
                        break;
                }
            }

            return entries;
        }

        private void AcceptQuest(QuestGiverOffer offer)
        {
            if (questManager == null || offer == null)
                return;

            string id = offer.QuestId;
            questManager.MakeQuestAvailable(id);
            if (!questManager.StartQuest(id))
                return;

            questManager.NotifyNpcTalked(npcId);
            ShowDialogue($"Accepted: {offer.quest.title}", () => TryInteract());
        }

        private void ClaimRewards(QuestGiverOffer offer)
        {
            if (questManager == null || offer == null)
                return;

            questManager.ClaimRewards(offer.QuestId);
            ShowDialogue(offer.GetRewardDialogue(), () => TryInteract());
            FindAnyObjectByType<ActiveQuestHudUI>()?.Refresh();
        }

        private void EnsureOffersAvailable()
        {
            if (questManager == null || questOffers == null)
                return;

            for (int i = 0; i < questOffers.Length; i++)
            {
                QuestGiverOffer offer = questOffers[i];
                if (offer?.quest == null || !ArePrerequisitesMet(offer))
                    continue;

                questManager.MakeQuestAvailable(offer.QuestId);
            }
        }

        private bool ArePrerequisitesMet(QuestGiverOffer offer)
        {
            if (offer.prerequisiteQuestIds == null || offer.prerequisiteQuestIds.Length == 0)
                return true;

            if (questManager == null)
                return false;

            for (int i = 0; i < offer.prerequisiteQuestIds.Length; i++)
            {
                string prerequisiteId = offer.prerequisiteQuestIds[i];
                if (string.IsNullOrEmpty(prerequisiteId))
                    continue;

                QuestProgress progress = questManager.GetProgress(prerequisiteId);
                if (progress == null || progress.status != QuestStatus.TurnedIn)
                    return false;
            }

            return true;
        }

        private void EnsureQuestManager()
        {
            if (questManager == null)
                questManager = QuestManager.EnsureExists();
        }

        private bool IsPlayerNearby()
        {
            if (!PlayerInteractionUtility.TryGetPlayerPosition(out Vector3 playerPosition))
                return false;

            return IsWithinInteractRange(playerPosition);
        }

        public bool IsWithinInteractRange(Vector3 playerPosition)
        {
            float distance = PlayerInteractionUtility.DistanceToInteractable(
                playerPosition,
                interactCollider,
                transform.position);
            return distance <= interactRange;
        }

        private UIManager ResolveUiManager()
        {
            if (uiManager == null)
                uiManager = FindAnyObjectByType<UIManager>();
            return uiManager;
        }

        private void ShowPrompt()
        {
            UIManager manager = ResolveUiManager();
            if (manager == null)
                return;

            string label = string.IsNullOrEmpty(displayName) ? "NPC" : displayName;
            manager.ShowInteractionPrompt($"{promptText} — {label}");
        }

        private void ShowDialogue(string message, Action onContinue = null, string buttonLabel = "Continue")
        {
            if (string.IsNullOrEmpty(message))
                return;

            uiManager?.HideInteractionPrompt();
            QuestGiverDialogUI.Show(displayName, message, () =>
            {
                onContinue?.Invoke();
                if (playerInRange && onContinue == null)
                    ShowPrompt();
            }, buttonLabel);
        }
    }

    [Serializable]
    public class QuestGiverOffer
    {
        [Header("Quest")]
        public QuestDefinition quest;

        [Tooltip("Quest ids that must be TurnedIn before this offer appears.")]
        public string[] prerequisiteQuestIds;

        [Tooltip("When true, talking to the NPC makes this quest Available if it is still Locked.")]
        public bool makeAvailableOnTalk = true;

        [Header("Dialogue")]
        [TextArea(2, 4)] public string offerDialogue;
        [TextArea(2, 4)] public string progressDialogue;
        [TextArea(2, 4)] public string readyDialogue;
        [TextArea(2, 4)] public string rewardDialogue;
        [TextArea(2, 4)] public string doneDialogue;

        public string QuestId => quest != null ? quest.ResolvedId : string.Empty;

        public string GetOfferDialogue() => FirstNonEmpty(offerDialogue, quest?.description);
        public string GetProgressDialogue() => FirstNonEmpty(progressDialogue, "Keep working on the objectives. Check the tracker for progress.");
        public string GetReadyDialogue() => FirstNonEmpty(readyDialogue, BuildRewardPreview());
        public string GetRewardDialogue() => FirstNonEmpty(rewardDialogue, "Well done. Here are your rewards.");
        public string GetDoneDialogue() => FirstNonEmpty(doneDialogue, "Thanks again.");

        private string BuildRewardPreview()
        {
            if (quest?.rewards == null || quest.rewards.Count == 0)
                return "You have completed the objectives. Press Continue to claim your reward.";

            System.Text.StringBuilder builder = new System.Text.StringBuilder("You have what I need. Rewards: ");
            bool first = true;
            for (int i = 0; i < quest.rewards.Count; i++)
            {
                QuestRewardDefinition reward = quest.rewards[i];
                if (reward == null)
                    continue;

                if (!first)
                    builder.Append(", ");
                first = false;

                if (reward.type == QuestRewardType.Pi)
                    builder.Append($"{reward.amount} Pi");
                else if (reward.type == QuestRewardType.Item && reward.item != null)
                    builder.Append($"{reward.amount}x {reward.item.itemName}");
            }

            builder.Append(". Press Continue to claim.");
            return builder.ToString();
        }

        private static string FirstNonEmpty(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }

    public struct QuestBoardEntry
    {
        public string Title;
        public string Detail;
        public string ActionLabel;
        public QuestStatus Status;
        public bool CanSelect;
        public Action OnSelected;
    }
}

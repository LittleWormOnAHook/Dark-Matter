using Project.Core;
using Project.Interaction;
using Project.Quests;
using Project.UI;
using UnityEngine;

namespace Project.PPT
{
    /// <summary>
    /// Hold-E directions interaction for NPCs with a PptNpcProfile.
    /// Tap-E talk remains on QuestGiverNpc / IVendor.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PptNpcInteractor : MonoBehaviour, IHoldWorldUsable
    {
        [SerializeField] private string npcId = "pioneer_guide";
        [SerializeField] private PptNpcProfile profile;
        [SerializeField] private float interactRange = 3.5f;
        [SerializeField] private float holdDurationSeconds = 0.75f;
        [SerializeField] private string holdPromptText = "Hold E — Ask for directions";

        private Collider interactCollider;
        private PptNpcGestureController gestureController;
        private QuestGiverNpc questGiver;
        private bool holdActive;
        private float holdProgress;

        public string NpcId => npcId;
        public PptNpcProfile Profile => profile;
        public string SpeakerDisplayName
        {
            get
            {
                if (questGiver != null && !string.IsNullOrWhiteSpace(questGiver.DisplayName))
                    return questGiver.DisplayName;
                if (profile != null && !string.IsNullOrWhiteSpace(profile.DisplayName))
                    return profile.DisplayName;
                return npcId;
            }
        }
        public float HoldDurationSeconds => holdDurationSeconds;
        public string HoldPromptText => holdPromptText;
        public bool IsHoldActive => holdActive;
        public bool OffersDirections => CanOfferDirections();

        private void Awake()
        {
            interactCollider = GetComponent<Collider>();
            gestureController = GetComponent<PptNpcGestureController>();
            questGiver = GetComponent<QuestGiverNpc>();

            if (gestureController == null)
                gestureController = gameObject.AddComponent<PptNpcGestureController>();

            if (profile != null)
                gestureController.Configure(profile);
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (!CanOfferDirections() || !IsWithinRange(context.PlayerPosition))
                return -1f;

            if (questGiver != null
                && !WorldUseController.IsAimedAtQuestGiver(context, questGiver, interactCollider))
                return -1f;

            float distance = PlayerInteractionUtility.DistanceToInteractable(
                context.PlayerPosition,
                interactCollider,
                transform.position);

            return 900f - distance;
        }

        public bool TryUse(WorldUseContext context)
        {
            return false;
        }

        public bool CanBeginHold(WorldUseContext context)
        {
            return CanOfferDirections()
                && IsWithinRange(context.PlayerPosition)
                && (questGiver == null || WorldUseController.IsAimedAtQuestGiver(context, questGiver, interactCollider));
        }

        public void BeginHold(WorldUseContext context)
        {
            holdActive = true;
            holdProgress = 0f;
        }

        public bool TickHold(WorldUseContext context, float deltaTime, out float progress01)
        {
            if (!holdActive)
            {
                progress01 = 0f;
                return false;
            }

            if (!CanBeginHold(context))
            {
                CancelHold(context);
                progress01 = 0f;
                return false;
            }

            holdProgress += deltaTime / Mathf.Max(0.05f, holdDurationSeconds);
            progress01 = Mathf.Clamp01(holdProgress);

            if (holdProgress < 1f)
                return false;

            holdActive = false;
            OpenDirectionsMenu();
            return true;
        }

        public void CancelHold(WorldUseContext context)
        {
            holdActive = false;
            holdProgress = 0f;
        }

        public void HandleDirectionChoice(PptEntry entry)
        {
            PptManager manager = PptManager.Instance;
            if (manager == null || entry == null)
                return;

            PptDirectionResult result = manager.ResolveDirection(npcId, entry, transform.position);
            ShowDirectionFeedback(result);
        }

        private void ShowDirectionFeedback(PptDirectionResult result)
        {
            string speaker = SpeakerDisplayName;

            QuestGiverDialogUI.Show(speaker, result.Phrase, null, npcAnchor: transform);

            if (result.Kind == PptDirectionKind.Unknown)
            {
                gestureController?.PlayShrug();
                return;
            }

            if (result.Kind == PptDirectionKind.Precise && result.SpawnTracer)
            {
                gestureController?.PlayPoint(result.BearingDegrees);
                PptTerrainDirectionTracer.Spawn(transform.position + Vector3.up * 1.2f, result.AimPosition);
                return;
            }

            if (result.BearingDegrees > 0f || result.AimPosition != Vector3.zero)
                gestureController?.PlayPoint(result.BearingDegrees);
        }

        public void OpenDirectionsMenu()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return;

            PptDirectionsMenuUI ui = PptDirectionsMenuUI.EnsureExists(canvas.transform);
            ui.Show(this);
        }

        private bool CanOfferDirections()
        {
            if (!GameSession.HasStarted)
                return false;

            if (profile == null)
            {
                PptManager manager = PptManager.Instance;
                if (manager != null && manager.TryGetNpcProfile(npcId, out PptNpcProfile loaded))
                    profile = loaded;
            }

            return profile != null && profile.HasTalkOption(PptTalkOptions.Directions);
        }

        private bool IsWithinRange(Vector3 playerPosition)
        {
            if (questGiver != null)
                return questGiver.IsWithinInteractRange(playerPosition);

            float distance = PlayerInteractionUtility.DistanceToInteractable(
                playerPosition,
                interactCollider,
                transform.position);
            return distance <= interactRange;
        }
    }
}

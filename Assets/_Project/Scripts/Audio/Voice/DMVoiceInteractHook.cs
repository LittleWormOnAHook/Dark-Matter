using System.Collections;
using Project.Core;
using Project.Interaction;
using Project.PPT;
using Project.Quests;
using Project.UI;
using UnityEngine;

namespace Project.Audio.Voice
{
    public enum DMVoiceInteractDetection
    {
        /// <summary>Pick from the target: QuestGiverNpc -> QuestGiverDialog, IHoldWorldUsable -> HoldCompleted, other IWorldUsable -> UiOpenedNearby.</summary>
        Auto = 0,
        /// <summary>The quest dialog / quest board opened while the player is in this giver's interact range.</summary>
        QuestGiverDialog = 1,
        /// <summary>The target's hold (IHoldWorldUsable) ended and a menu / dialog opened the same frame (completed, not cancelled).</summary>
        HoldCompleted = 2,
        /// <summary>A menu / dialog took gameplay input while the player was within useRadius of the target.</summary>
        UiOpenedNearby = 3,
        /// <summary>Only fires from code via NotifyUsed().</summary>
        ManualOnly = 4
    }

    /// <summary>
    /// 2026-11-03o. Voice line when the player uses an NPC / usable (QuestGiverNpc, PptNpcInteractor or any
    /// IWorldUsable / IHoldWorldUsable). The use dispatch (WorldUseController) has no "used" event, so the
    /// hook watches the use's own observable result (dialog opened, hold completed) instead of editing the
    /// NPC scripts. Code can also call NotifyUsed() directly.
    /// First use plays <see cref="line"/>; later uses pick randomly from <see cref="repeatBarks"/>
    /// (no immediate repeat), falling back to <see cref="line"/> when the list is empty.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark Matter/Voice/Voice Interact Hook")]
    public sealed class DMVoiceInteractHook : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("QuestGiverNpc / PptNpcInteractor / any IWorldUsable component. Empty = first one found on this object or its parents.")]
        public Component target;
        public DMVoiceInteractDetection detection = DMVoiceInteractDetection.Auto;
        [Tooltip("UiOpenedNearby only: max player distance from the target.")]
        [Min(0.1f)] public float useRadius = 3f;

        [Header("Lines")]
        [Tooltip("First-use line (and every use when repeatBarks is empty).")]
        public DMVoiceLineRef line;
        [Tooltip("Optional: random pick on later uses.")]
        public DMVoiceLineRef[] repeatBarks = new DMVoiceLineRef[0];
        [Tooltip("The first-use line plays only once per save (key interact:<useKey>). Later uses go to repeatBarks.")]
        public bool firstLineOncePerSave = true;
        [Tooltip("Play-once key. Empty = scene + object path.")]
        public string useKey = "";

        [Header("Output")]
        public DMVoicePriority priority = DMVoicePriority.Npc;
        [Tooltip("World = 3D at the NPC (default). Narration / Radio override.")]
        public DMVoiceOutputMode outputMode = DMVoiceOutputMode.World;
        [Tooltip("World mode speaker. Empty = the target's transform.")]
        public Transform worldTarget;
        [Range(0f, 2f)] public float volumeScale = 1f;

        [Header("Timing")]
        [Tooltip("Ignore further uses for this many seconds after one fires.")]
        [Min(0f)] public float cooldownSeconds = 2f;
        [Min(0f)] public float delaySeconds;
        [Tooltip("Extra random delay added on top of delaySeconds, picked in [min, max] each time. Max 0 = off.")]
        [Min(0f)] public float randomDelayMin;
        [Min(0f)] public float randomDelayMax;

        [Header("Quest condition (optional)")]
        public string requiredQuestId = "";
        public QuestStatus requiredStatus = QuestStatus.Active;
        public string forbiddenQuestId = "";
        public QuestStatus forbiddenStatus = QuestStatus.Completed;
        public int requiredObjectiveIndex = -1;
        public int requiredObjectiveCount = 1;

        /// <summary>Raised when a use is detected (before cooldown / gate checks pass the line on).</summary>
        public event System.Action<DMVoiceInteractHook> OnUseDetected;

        private QuestGiverNpc giver;
        private IHoldWorldUsable holdUsable;
        private IWorldUsable usable;
        private DMVoiceInteractDetection resolved;
        private bool lastDialogOpen;
        private bool lastUiLocked;
        private bool lastHoldActive;
        private bool lastDirectionsOpen;
        private float holdEndedAt = -10f;
        private const float HoldResultWindowSeconds = 0.35f;
        private float cooldownUntil;
        private int lastBarkIndex = -1;
        private bool usedThisSession;
        private Coroutine pendingRoutine;

        private void OnEnable()
        {
            ResolveTarget();
            lastDialogOpen = QuestGiverDialogUI.IsDialogOpen;
            lastUiLocked = GameplayKeyboardShortcuts.IsGameplayInputLockedByUi();
            lastHoldActive = holdUsable != null && holdUsable.IsHoldActive;
            lastDirectionsOpen = PptDirectionsMenuUI.IsOpen;
        }

        private void OnDisable()
        {
            if (pendingRoutine != null)
            {
                StopCoroutine(pendingRoutine);
                pendingRoutine = null;
            }
        }

        private void ResolveTarget()
        {
            if (target == null)
            {
                target = GetComponentInParent<QuestGiverNpc>();
                if (target == null)
                    target = GetComponentInParent<PptNpcInteractor>();
                if (target == null && GetComponentInParent<IWorldUsable>() is Component found)
                    target = found;
            }

            giver = target as QuestGiverNpc;
            holdUsable = target as IHoldWorldUsable;
            usable = target as IWorldUsable;

            resolved = detection;
            if (resolved == DMVoiceInteractDetection.Auto)
            {
                if (giver != null)
                    resolved = DMVoiceInteractDetection.QuestGiverDialog;
                else if (holdUsable != null)
                    resolved = DMVoiceInteractDetection.HoldCompleted;
                else if (usable != null)
                    resolved = DMVoiceInteractDetection.UiOpenedNearby;
                else
                    resolved = DMVoiceInteractDetection.ManualOnly;
            }
        }

        private void LateUpdate()
        {
            bool dialogOpen = QuestGiverDialogUI.IsDialogOpen;
            bool uiLocked = GameplayKeyboardShortcuts.IsGameplayInputLockedByUi();
            bool holdActive = holdUsable != null && holdUsable.IsHoldActive;
            bool directionsOpen = PptDirectionsMenuUI.IsOpen;
            bool menuOpened = (dialogOpen && !lastDialogOpen)
                || (directionsOpen && !lastDirectionsOpen)
                || (uiLocked && !lastUiLocked);

            bool used = false;
            switch (resolved)
            {
                case DMVoiceInteractDetection.QuestGiverDialog:
                    used = dialogOpen && !lastDialogOpen && GiverInRange();
                    break;
                case DMVoiceInteractDetection.HoldCompleted:
                    // Completed holds open their result (directions menu, dialog) the same or next frame;
                    // a cancelled hold opens nothing inside the window.
                    if (lastHoldActive && !holdActive)
                        holdEndedAt = Time.unscaledTime;
                    if (menuOpened && Time.unscaledTime - holdEndedAt <= HoldResultWindowSeconds)
                    {
                        used = true;
                        holdEndedAt = -10f;
                    }
                    break;
                case DMVoiceInteractDetection.UiOpenedNearby:
                    used = menuOpened && PlayerWithin(useRadius);
                    break;
            }

            lastDialogOpen = dialogOpen;
            lastUiLocked = uiLocked;
            lastHoldActive = holdActive;
            lastDirectionsOpen = directionsOpen;

            if (used)
                NotifyUsed();
        }

        /// <summary>Report a use from code (any interaction script can call this). Applies cooldown, gate and delay.</summary>
        public void NotifyUsed()
        {
            OnUseDetected?.Invoke(this);

            if (Time.unscaledTime < cooldownUntil || pendingRoutine != null)
                return;

            if (!DMVoiceTriggerUtility.PassesQuestGate(
                    requiredQuestId, requiredStatus, forbiddenQuestId, forbiddenStatus,
                    requiredObjectiveIndex, requiredObjectiveCount))
                return;

            DMVoiceLineRef pick = PickLine(out bool isFirstLine);
            if (!pick.IsSet)
                return;

            cooldownUntil = Time.unscaledTime + cooldownSeconds;
            float delay = DMVoiceTriggerUtility.ResolveDelay(delaySeconds, randomDelayMin, randomDelayMax);
            if (delay > 0.01f && isActiveAndEnabled)
                pendingRoutine = StartCoroutine(FireAfterDelay(pick, isFirstLine, delay));
            else
                Fire(pick, isFirstLine);
        }

        private DMVoiceLineRef PickLine(out bool isFirstLine)
        {
            bool firstAvailable = line.IsSet
                && (firstLineOncePerSave ? !DMVoiceManager.IsPlayed(ResolveOnceKey()) : !usedThisSession);
            bool hasBarks = repeatBarks != null && repeatBarks.Length > 0;

            if (firstAvailable || !hasBarks)
            {
                isFirstLine = firstAvailable;
                if (firstAvailable || !firstLineOncePerSave)
                    return line;
                return default;
            }

            isFirstLine = false;
            int count = repeatBarks.Length;
            int index = Random.Range(0, count);
            if (count > 1 && index == lastBarkIndex)
                index = (index + 1 + Random.Range(0, count - 1)) % count;
            lastBarkIndex = index;
            return repeatBarks[index];
        }

        private IEnumerator FireAfterDelay(DMVoiceLineRef pick, bool isFirstLine, float delay)
        {
            yield return new WaitForSeconds(delay);
            pendingRoutine = null;
            Fire(pick, isFirstLine);
        }

        private void Fire(DMVoiceLineRef pick, bool isFirstLine)
        {
            Transform speaker = worldTarget != null ? worldTarget : (target != null ? target.transform : transform);
            DMVoicePlayOptions options = DMVoiceTriggerUtility.BuildOptions(priority, outputMode, speaker, volumeScale);
            if (!DMVoiceTriggerUtility.Fire(pick, string.Empty, false, options))
                return;

            usedThisSession = true;
            if (isFirstLine && firstLineOncePerSave)
                DMVoiceManager.MarkPlayed(ResolveOnceKey());
        }

        private string ResolveOnceKey()
        {
            if (!string.IsNullOrWhiteSpace(useKey))
                return "interact:" + useKey.Trim();

            Transform t = transform;
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return "interact:" + gameObject.scene.name + "/" + path;
        }

        private bool GiverInRange()
        {
            if (giver == null)
                return false;
            if (giver.IsPlayerInRange)
                return true;
            Transform player = ResolvePlayer();
            return player != null && giver.IsWithinInteractRange(player.position);
        }

        private bool PlayerWithin(float radius)
        {
            Transform player = ResolvePlayer();
            Transform from = target != null ? target.transform : transform;
            return player != null && (player.position - from.position).sqrMagnitude <= radius * radius;
        }

        private static Transform ResolvePlayer()
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            return player != null ? player.transform : null;
        }
    }
}

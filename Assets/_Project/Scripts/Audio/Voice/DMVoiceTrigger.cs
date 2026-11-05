using System.Collections;
using Project.Quests;
using UnityEngine;

namespace Project.Audio.Voice
{
    /// <summary>
    /// Fires a voice line (or a whole stage tag) when a collider zone is entered.
    /// Drop it on a trigger collider with "Is Trigger" on, tag the player object, done.
    /// Optional quest gate, delay, play-once, priority and output mode override.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark Matter/Voice/Voice Trigger")]
    public sealed class DMVoiceTrigger : MonoBehaviour
    {
        [Header("Line")]
        [Tooltip("A single line to play. Leave empty when using a stage tag.")]
        public DMVoiceLineRef line;

        [Tooltip("Optional stage tag (e.g. QM.3.S2). Plays every line tagged with it, in set order.")]
        public string stageTag = "";

        [Tooltip("Play only once per save. Ignored for barks.")]
        public bool playOnce = true;

        [Tooltip("Bark lines are dropped if they sit in the queue too long and are never flagged as played.")]
        public DMVoicePriority priority = DMVoicePriority.Story;

        public DMVoiceOutputMode outputMode = DMVoiceOutputMode.Narration;

        [Tooltip("World mode only: speaker transform to attach the 3D voice to. Defaults to this object.")]
        public Transform worldTarget;

        [Range(0f, 2f)] public float volumeScale = 1f;

        [Header("Zone")]
        [Tooltip("Only fire for this tag. Empty = any collider that carries a player component.")]
        public string playerTag = "Player";

        [Tooltip("Seconds to wait after entering the zone before the line is queued.")]
        [Min(0f)] public float delaySeconds;

        [Tooltip("2026-11-03o. Extra random delay added on top of delaySeconds, picked in [min, max] each time. Max 0 = off.")]
        [Min(0f)] public float randomDelayMin;
        [Min(0f)] public float randomDelayMax;

        [Tooltip("Re-arm when the player leaves the zone (only meaningful when playOnce is off).")]
        public bool rearmOnExit = true;

        [Header("Quest condition (optional)")]
        [Tooltip("Quest id gate. Empty = no gate.")]
        public string requiredQuestId = "";

        public QuestStatus requiredStatus = QuestStatus.Active;

        [Tooltip("Skip the gate and fire when the quest reaches this status. Leave empty for none.")]
        public string forbiddenQuestId = "";

        public QuestStatus forbiddenStatus = QuestStatus.Completed;

        [Tooltip("Objective index that must be at or past requiredObjectiveCount. -1 = ignore objectives.")]
        public int requiredObjectiveIndex = -1;
        public int requiredObjectiveCount = 1;

        private bool fired;
        private bool playerInside;
        private Coroutine pendingRoutine;

        private void Reset()
        {
            Collider zone = GetComponent<Collider>();
            if (zone != null)
                zone.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
                return;

            playerInside = true;
            if (fired && playOnce)
                return;

            if (pendingRoutine != null)
                return;

            if (!PassesQuestGate())
                return;

            float delay = DMVoiceTriggerUtility.ResolveDelay(delaySeconds, randomDelayMin, randomDelayMax);
            if (delay > 0.01f)
                pendingRoutine = StartCoroutine(FireAfterDelay(delay));
            else
                Fire();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other))
                return;

            playerInside = false;
            if (!playOnce && rearmOnExit)
                fired = false;
        }

        private IEnumerator FireAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            pendingRoutine = null;

            if (!playerInside || (fired && playOnce))
                yield break;

            if (!PassesQuestGate())
                yield break;

            Fire();
        }

        private void Fire()
        {
            DMVoiceManager manager = DMVoiceManager.EnsureExists();
            if (manager == null)
                return;

            DMVoicePlayOptions options = new DMVoicePlayOptions
            {
                priority = priority,
                output = outputMode,
                worldTarget = outputMode == DMVoiceOutputMode.World ? ResolveWorldTarget() : null,
                volumeScale = volumeScale
            };

            bool accepted;
            if (!string.IsNullOrWhiteSpace(stageTag))
                accepted = (playOnce ? manager.PlayStageOnce(stageTag, options) : manager.PlayStage(stageTag, options)) > 0;
            else if (playOnce)
                accepted = manager.PlayOnce(line, options);
            else
                accepted = manager.Play(line, options);

            if (accepted)
                fired = true;
        }

        private Transform ResolveWorldTarget() => worldTarget != null ? worldTarget : transform;

        private bool IsPlayer(Collider other)
        {
            if (other == null)
                return false;

            if (!string.IsNullOrEmpty(playerTag))
            {
                if (other.CompareTag(playerTag))
                    return true;
                Transform parent = other.transform.parent;
                return parent != null && parent.CompareTag(playerTag);
            }

            return other.GetComponentInParent<CharacterController>() != null;
        }

        private bool PassesQuestGate()
        {
            if (string.IsNullOrEmpty(requiredQuestId) && string.IsNullOrEmpty(forbiddenQuestId)
                && requiredObjectiveIndex < 0)
                return true;

            QuestManager quests = QuestManager.Instance ?? Object.FindAnyObjectByType<QuestManager>();
            if (quests == null)
                return false;

            if (!string.IsNullOrEmpty(requiredQuestId))
            {
                QuestProgress progress = quests.GetProgress(requiredQuestId);
                if (progress == null || progress.status != requiredStatus)
                    return false;

                if (requiredObjectiveIndex >= 0
                    && progress.GetObjectiveProgress(requiredObjectiveIndex) < requiredObjectiveCount)
                    return false;
            }

            if (!string.IsNullOrEmpty(forbiddenQuestId))
            {
                QuestProgress progress = quests.GetProgress(forbiddenQuestId);
                if (progress != null && progress.status == forbiddenStatus)
                    return false;
            }

            return true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.75f, 0.18f, 0.48f, 0.6f);
            Gizmos.matrix = transform.localToWorldMatrix;
            if (TryGetComponent(out BoxCollider box))
                Gizmos.DrawWireCube(box.center, box.size);
            else if (TryGetComponent(out SphereCollider sphere))
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            else if (TryGetComponent(out CapsuleCollider capsule))
                Gizmos.DrawWireCube(capsule.center, new Vector3(capsule.radius * 2f, capsule.height, capsule.radius * 2f));
        }
#endif
    }
}

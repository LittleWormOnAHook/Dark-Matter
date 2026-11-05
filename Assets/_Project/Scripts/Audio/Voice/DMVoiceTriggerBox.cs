using System.Collections;
using Project.Core;
using Project.Quests;
using UnityEngine;

namespace Project.Audio.Voice
{
    /// <summary>
    /// 2026-11-03o. Drop-in box zone: no collider or physics layer setup needed. Size the box in the
    /// scene (center / size, follows this transform's position, rotation and scale) and the line fires
    /// when the live player's position enters it. Same options as DMVoiceTrigger.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark Matter/Voice/Voice Trigger Box")]
    public sealed class DMVoiceTriggerBox : MonoBehaviour
    {
        [Header("Line")]
        [Tooltip("A single line to play. Leave empty when using a stage tag.")]
        public DMVoiceLineRef line;

        [Tooltip("Optional stage tag (e.g. QM.3.S2). Plays every line tagged with it, in set order.")]
        public string stageTag = "";

        [Tooltip("Play only once per save. Ignored for barks.")]
        public bool playOnce = true;

        public DMVoicePriority priority = DMVoicePriority.Story;

        public DMVoiceOutputMode outputMode = DMVoiceOutputMode.Narration;

        [Tooltip("World mode only: speaker transform to attach the 3D voice to. Defaults to this object.")]
        public Transform worldTarget;

        [Range(0f, 2f)] public float volumeScale = 1f;

        [Header("Box (local space)")]
        public Vector3 center = Vector3.zero;
        public Vector3 size = new Vector3(6f, 4f, 6f);

        [Tooltip("Seconds between player-position checks. 0 = every frame.")]
        [Min(0f)] public float checkInterval = 0.1f;

        [Header("Delay")]
        [Tooltip("Seconds to wait after entering the box before the line is queued.")]
        [Min(0f)] public float delaySeconds;

        [Tooltip("Extra random delay added on top of delaySeconds, picked in [min, max] each time. Max 0 = off.")]
        [Min(0f)] public float randomDelayMin;
        [Min(0f)] public float randomDelayMax;

        [Tooltip("Re-arm when the player leaves the box (only meaningful when playOnce is off).")]
        public bool rearmOnExit = true;

        [Header("Quest condition (optional)")]
        public string requiredQuestId = "";
        public QuestStatus requiredStatus = QuestStatus.Active;
        public string forbiddenQuestId = "";
        public QuestStatus forbiddenStatus = QuestStatus.Completed;
        [Tooltip("Objective index that must be at or past requiredObjectiveCount. -1 = ignore objectives.")]
        public int requiredObjectiveIndex = -1;
        public int requiredObjectiveCount = 1;

        private bool fired;
        private bool playerInside;
        private float nextCheck;
        private Coroutine pendingRoutine;
        private Transform cachedPlayer;

        private void OnDisable()
        {
            if (pendingRoutine != null)
            {
                StopCoroutine(pendingRoutine);
                pendingRoutine = null;
            }

            playerInside = false;
        }

        private void Update()
        {
            if (Time.time < nextCheck)
                return;

            nextCheck = Time.time + checkInterval;

            Transform player = ResolvePlayer();
            bool inside = player != null && ContainsPoint(player.position);

            if (inside && !playerInside)
                OnEnter();
            else if (!inside && playerInside)
                OnExit();

            playerInside = inside;
        }

        /// <summary>True when a world-space point lies inside the box.</summary>
        public bool ContainsPoint(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint) - center;
            Vector3 half = size * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        private void OnEnter()
        {
            if ((fired && playOnce) || pendingRoutine != null)
                return;

            if (!PassesGate())
                return;

            float delay = DMVoiceTriggerUtility.ResolveDelay(delaySeconds, randomDelayMin, randomDelayMax);
            if (delay > 0.01f)
                pendingRoutine = StartCoroutine(FireAfterDelay(delay));
            else
                Fire();
        }

        private void OnExit()
        {
            if (!playOnce && rearmOnExit)
                fired = false;
        }

        private IEnumerator FireAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            pendingRoutine = null;

            Transform player = ResolvePlayer();
            if (player == null || !ContainsPoint(player.position) || (fired && playOnce))
                yield break;

            if (!PassesGate())
                yield break;

            Fire();
        }

        private void Fire()
        {
            DMVoicePlayOptions options = DMVoiceTriggerUtility.BuildOptions(
                priority, outputMode, worldTarget != null ? worldTarget : transform, volumeScale);

            if (DMVoiceTriggerUtility.Fire(line, stageTag, playOnce, options))
                fired = true;
        }

        private bool PassesGate() =>
            DMVoiceTriggerUtility.PassesQuestGate(
                requiredQuestId, requiredStatus, forbiddenQuestId, forbiddenStatus,
                requiredObjectiveIndex, requiredObjectiveCount);

        private Transform ResolvePlayer()
        {
            if (cachedPlayer != null && cachedPlayer.gameObject.activeInHierarchy)
                return cachedPlayer;

            GameObject player = PlayerLocator.FindPlayerObject();
            cachedPlayer = player != null ? player.transform : null;
            return cachedPlayer;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.75f, 0.18f, 0.48f, 0.12f);
            Gizmos.DrawCube(center, size);
            Gizmos.color = new Color(0.75f, 0.18f, 0.48f, 0.8f);
            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}

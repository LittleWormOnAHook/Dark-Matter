using System;
using System.Collections;
using System.Collections.Generic;
using Project.Quests;
using UnityEngine;

namespace Project.Audio.Voice
{
    public enum DMVoiceQuestEvent
    {
        /// <summary>Quest moved into Active (QuestManager.StartQuest).</summary>
        Started = 0,
        /// <summary>An objective counter changed on an Active quest.</summary>
        Updated = 1,
        /// <summary>QuestManager.OnQuestCompleted fired.</summary>
        Completed = 2
    }

    /// <summary>One quest event -> line / stage mapping. Used inline on DMVoiceQuestHook or in a DMVoiceQuestHookTable.</summary>
    [Serializable]
    public sealed class DMVoiceQuestHookEntry
    {
        [Tooltip("Quest id (QuestDefinition.ResolvedId).")]
        public string questId = "";
        public DMVoiceQuestEvent questEvent = DMVoiceQuestEvent.Started;
        [Tooltip("Updated only: objective index that must have changed. -1 = any objective.")]
        public int objectiveIndex = -1;
        [Tooltip("Updated only: fire when that objective reaches at least this count. 0 = any change.")]
        [Min(0)] public int objectiveCount;

        [Header("Line")]
        public DMVoiceLineRef line;
        [Tooltip("Optional stage tag; used instead of the line when set.")]
        public string stageTag = "";
        public DMVoicePriority priority = DMVoicePriority.Story;
        public DMVoiceOutputMode outputMode = DMVoiceOutputMode.Narration;
        [Tooltip("World mode only. Empty = the hook's object.")]
        public Transform worldTarget;
        [Range(0f, 2f)] public float volumeScale = 1f;

        [Header("Once / delay")]
        [Tooltip("Fire once per save for this quest+event(+objective). Uses the voice play-once keys (saved with the game).")]
        public bool oncePerEvent = true;
        [Min(0f)] public float delaySeconds;
        [Min(0f)] public float randomDelayMin;
        [Min(0f)] public float randomDelayMax;

        /// <summary>Play-once key written through DMVoiceManager.MarkPlayed.</summary>
        public string OnceKey => "quest:" + questId + ":" + questEvent + ":" + objectiveIndex
            + (objectiveCount > 0 ? ":" + objectiveCount : string.Empty);
    }

    /// <summary>
    /// 2026-11-03o. Plays voice lines off real QuestManager events. QuestManager has no separate
    /// start event, so Started is detected as a status transition into Active inside OnQuestUpdated;
    /// Updated is an objective-counter change on an Active quest; Completed is OnQuestCompleted.
    /// Put one in the scene (or on any persistent object) and fill entries inline and/or assign tables.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark Matter/Voice/Voice Quest Hook")]
    public sealed class DMVoiceQuestHook : MonoBehaviour
    {
        [Tooltip("Shared tables (ScriptableObject). Checked after the inline entries.")]
        public DMVoiceQuestHookTable[] tables = Array.Empty<DMVoiceQuestHookTable>();

        public List<DMVoiceQuestHookEntry> entries = new List<DMVoiceQuestHookEntry>();

        [Tooltip("Log every quest event this hook sees (for wiring).")]
        public bool logEvents;

        /// <summary>Raised whenever an entry was queued: (entry, event).</summary>
        public event Action<DMVoiceQuestHookEntry, DMVoiceQuestEvent> OnEntryFired;

        private readonly Dictionary<string, QuestStatus> lastStatus = new Dictionary<string, QuestStatus>();
        private readonly Dictionary<string, int[]> lastObjectives = new Dictionary<string, int[]>();
        private readonly List<PendingEvent> pending = new List<PendingEvent>();
        private QuestManager boundManager;
        private Coroutine bindRoutine;

        private void OnEnable()
        {
            if (!TryBind())
                bindRoutine = StartCoroutine(BindWhenReady());
        }

        private void OnDisable()
        {
            if (bindRoutine != null)
            {
                StopCoroutine(bindRoutine);
                bindRoutine = null;
            }

            Unbind();
            pending.Clear();
        }

        private IEnumerator BindWhenReady()
        {
            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0.5f);
            while (!TryBind())
                yield return wait;
            bindRoutine = null;
        }

        private bool TryBind()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager == null)
                return false;

            boundManager = manager;
            boundManager.OnQuestUpdated += HandleQuestUpdated;
            boundManager.OnQuestCompleted += HandleQuestCompleted;
            SeedSnapshots();
            return true;
        }

        private void Unbind()
        {
            if (boundManager != null)
            {
                boundManager.OnQuestUpdated -= HandleQuestUpdated;
                boundManager.OnQuestCompleted -= HandleQuestCompleted;
            }

            boundManager = null;
        }

        /// <summary>Record current status of every quest we care about so a loaded save does not fire "Started".</summary>
        private void SeedSnapshots()
        {
            lastStatus.Clear();
            lastObjectives.Clear();
            foreach (DMVoiceQuestHookEntry entry in AllEntries())
            {
                if (entry == null || string.IsNullOrEmpty(entry.questId) || lastStatus.ContainsKey(entry.questId))
                    continue;

                QuestProgress progress = boundManager.GetProgress(entry.questId);
                if (progress == null)
                    continue;

                lastStatus[entry.questId] = progress.status;
                lastObjectives[entry.questId] = CopyObjectives(progress);
            }
        }

        private void HandleQuestUpdated(QuestProgress progress)
        {
            if (progress == null || string.IsNullOrEmpty(progress.questId))
                return;

            string id = progress.questId;
            bool hadStatus = lastStatus.TryGetValue(id, out QuestStatus previous);
            lastObjectives.TryGetValue(id, out int[] previousObjectives);
            int[] currentObjectives = CopyObjectives(progress);

            lastStatus[id] = progress.status;
            lastObjectives[id] = currentObjectives;

            if (progress.status == QuestStatus.Active && (!hadStatus || previous != QuestStatus.Active))
            {
                Queue(progress, DMVoiceQuestEvent.Started, -1);
                return;
            }

            if (progress.status != QuestStatus.Active)
                return;

            int count = currentObjectives.Length;
            for (int i = 0; i < count; i++)
            {
                int before = previousObjectives != null && i < previousObjectives.Length ? previousObjectives[i] : 0;
                if (currentObjectives[i] == before)
                    continue;

                Queue(progress, DMVoiceQuestEvent.Updated, i);
            }
        }

        private void HandleQuestCompleted(QuestProgress progress)
        {
            if (progress == null || string.IsNullOrEmpty(progress.questId))
                return;

            lastStatus[progress.questId] = QuestStatus.Completed;
            lastObjectives[progress.questId] = CopyObjectives(progress);
            Queue(progress, DMVoiceQuestEvent.Completed, -1);
        }

        private struct PendingEvent
        {
            public QuestProgress progress;
            public DMVoiceQuestEvent questEvent;
            public int objective;
        }

        private void Queue(QuestProgress progress, DMVoiceQuestEvent questEvent, int objective) =>
            pending.Add(new PendingEvent { progress = progress, questEvent = questEvent, objective = objective });

        // Deferred one step: QuestManager.ApplySaveProgress re-broadcasts every quest on load before the
        // voice play-once keys are restored, so loaded quests must not read as "Started".
        private void LateUpdate()
        {
            if (pending.Count == 0)
                return;

            bool fromSaveLoad = DMVoiceManager.SaveAppliedRecently();
            for (int i = 0; i < pending.Count; i++)
            {
                PendingEvent e = pending[i];
                Log(e.progress.questId, e.questEvent, e.objective);
                if (!fromSaveLoad)
                    Dispatch(e.progress.questId, e.questEvent, e.objective, e.progress);
            }

            pending.Clear();
        }

        private void Dispatch(string questId, DMVoiceQuestEvent questEvent, int changedObjective, QuestProgress progress)
        {
            foreach (DMVoiceQuestHookEntry entry in AllEntries())
            {
                if (entry == null || entry.questEvent != questEvent)
                    continue;

                if (!string.Equals(entry.questId, questId, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (questEvent == DMVoiceQuestEvent.Updated)
                {
                    if (entry.objectiveIndex >= 0 && entry.objectiveIndex != changedObjective)
                        continue;

                    if (entry.objectiveCount > 0
                        && progress.GetObjectiveProgress(changedObjective) < entry.objectiveCount)
                        continue;
                }

                if (!DMVoiceTriggerUtility.HasContent(entry.line, entry.stageTag))
                    continue;

                if (entry.oncePerEvent && DMVoiceManager.IsPlayed(entry.OnceKey))
                    continue;

                float delay = DMVoiceTriggerUtility.ResolveDelay(entry.delaySeconds, entry.randomDelayMin, entry.randomDelayMax);
                if (delay > 0.01f && isActiveAndEnabled)
                    StartCoroutine(FireAfterDelay(entry, questEvent, delay));
                else
                    Fire(entry, questEvent);
            }
        }

        private IEnumerator FireAfterDelay(DMVoiceQuestHookEntry entry, DMVoiceQuestEvent questEvent, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (entry.oncePerEvent && DMVoiceManager.IsPlayed(entry.OnceKey))
                yield break;
            Fire(entry, questEvent);
        }

        private void Fire(DMVoiceQuestHookEntry entry, DMVoiceQuestEvent questEvent)
        {
            DMVoicePlayOptions options = DMVoiceTriggerUtility.BuildOptions(
                entry.priority,
                entry.outputMode,
                entry.worldTarget != null ? entry.worldTarget : transform,
                entry.volumeScale);

            // The event's own once-key is the gate; the line itself may be replayed by other hooks.
            if (!DMVoiceTriggerUtility.Fire(entry.line, entry.stageTag, false, options))
                return;

            if (entry.oncePerEvent)
                DMVoiceManager.MarkPlayed(entry.OnceKey);

            OnEntryFired?.Invoke(entry, questEvent);
        }

        private IEnumerable<DMVoiceQuestHookEntry> AllEntries()
        {
            if (entries != null)
                for (int i = 0; i < entries.Count; i++)
                    yield return entries[i];

            if (tables == null)
                yield break;

            for (int t = 0; t < tables.Length; t++)
            {
                DMVoiceQuestHookTable table = tables[t];
                if (table == null || table.entries == null)
                    continue;
                for (int i = 0; i < table.entries.Count; i++)
                    yield return table.entries[i];
            }
        }

        private static int[] CopyObjectives(QuestProgress progress)
        {
            if (progress.objectiveProgress == null)
                return Array.Empty<int>();
            int[] copy = new int[progress.objectiveProgress.Length];
            Array.Copy(progress.objectiveProgress, copy, copy.Length);
            return copy;
        }

        private void Log(string questId, DMVoiceQuestEvent questEvent, int objective)
        {
            if (logEvents)
                Debug.Log($"[DMVoiceQuestHook] {questId} {questEvent}" + (objective >= 0 ? $" obj {objective}" : string.Empty), this);
        }
    }
}

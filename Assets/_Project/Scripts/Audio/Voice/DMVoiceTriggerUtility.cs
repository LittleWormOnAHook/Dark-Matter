using Project.Quests;
using UnityEngine;

namespace Project.Audio.Voice
{
    /// <summary>
    /// 2026-11-03o. Shared plumbing for every voice trigger / hook: delay resolution
    /// (fixed + random range), the line-or-stage fire call and the optional quest gate.
    /// </summary>
    public static class DMVoiceTriggerUtility
    {
        /// <summary>
        /// Fixed delay plus a random extra in [randomMin, randomMax]. A range with max &lt;= 0
        /// adds nothing; min/max are swapped when entered backwards.
        /// </summary>
        public static float ResolveDelay(float fixedSeconds, float randomMinSeconds, float randomMaxSeconds)
        {
            float delay = Mathf.Max(0f, fixedSeconds);
            float min = Mathf.Max(0f, randomMinSeconds);
            float max = Mathf.Max(0f, randomMaxSeconds);
            if (max < min)
            {
                float swap = min;
                min = max;
                max = swap;
            }

            if (max > 0f)
                delay += max > min ? Random.Range(min, max) : min;

            return delay;
        }

        public static DMVoicePlayOptions BuildOptions(
            DMVoicePriority priority,
            DMVoiceOutputMode output,
            Transform worldTarget,
            float volumeScale,
            bool interrupt = false)
        {
            return new DMVoicePlayOptions
            {
                priority = priority,
                output = output,
                interrupt = interrupt,
                worldTarget = output == DMVoiceOutputMode.World ? worldTarget : null,
                volumeScale = volumeScale
            };
        }

        /// <summary>Queue a single line or a stage tag. Returns true when the manager accepted it.</summary>
        public static bool Fire(DMVoiceLineRef line, string stageTag, bool playOnce, DMVoicePlayOptions options)
        {
            DMVoiceManager manager = DMVoiceManager.EnsureExists();
            if (manager == null)
                return false;

            if (!string.IsNullOrWhiteSpace(stageTag))
                return (playOnce ? manager.PlayStageOnce(stageTag, options) : manager.PlayStage(stageTag, options)) > 0;

            if (!line.IsSet)
                return false;

            return playOnce ? manager.PlayOnce(line, options) : manager.Play(line, options);
        }

        public static bool HasContent(DMVoiceLineRef line, string stageTag) =>
            line.IsSet || !string.IsNullOrWhiteSpace(stageTag);

        /// <summary>Same gate semantics as DMVoiceTrigger (required status / objective, forbidden status).</summary>
        public static bool PassesQuestGate(
            string requiredQuestId,
            QuestStatus requiredStatus,
            string forbiddenQuestId,
            QuestStatus forbiddenStatus,
            int requiredObjectiveIndex,
            int requiredObjectiveCount)
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
    }
}

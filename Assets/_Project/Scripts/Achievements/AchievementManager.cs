using System;
using System.Collections.Generic;
using Project.Core;
using Project.Progression;
using Project.UI;
using UnityEngine;

namespace Project.Achievements
{
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        private readonly Dictionary<string, AchievementProgress> progressById =
            new Dictionary<string, AchievementProgress>(StringComparer.Ordinal);

        private readonly Dictionary<string, AchievementDefinition> runtimeDefinitions =
            new Dictionary<string, AchievementDefinition>(StringComparer.Ordinal);

        public event Action<AchievementProgress, AchievementDefinition> OnAchievementUnlocked;
        public event Action<AchievementProgress, AchievementDefinition> OnProgressUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            RegisterAllDefinitions();
        }

        private void Start()
        {
            AchievementStarterCatalog.RegisterIfEmpty();
            DynamicAchievementGenerator.EnsureSessionGoals();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static AchievementManager EnsureExists()
        {
            if (Instance != null)
                return Instance;

            AchievementManager found = FindAnyObjectByType<AchievementManager>();
            if (found != null)
                return found;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
                return player.AddComponent<AchievementManager>();

            UIManager uiManager = FindAnyObjectByType<UIManager>();
            if (uiManager != null)
                return uiManager.gameObject.AddComponent<AchievementManager>();

            GameObject host = new GameObject("AchievementManager");
            DontDestroyOnLoad(host);
            return host.AddComponent<AchievementManager>();
        }

        public IReadOnlyList<AchievementProgress> GetAllProgress()
        {
            return new List<AchievementProgress>(progressById.Values);
        }

        public AchievementProgress GetProgress(string achievementId)
        {
            return progressById.TryGetValue(achievementId, out AchievementProgress progress) ? progress : null;
        }

        public AchievementDefinition GetDefinition(string achievementId)
        {
            if (runtimeDefinitions.TryGetValue(achievementId, out AchievementDefinition runtime))
                return runtime;

            return AchievementRegistry.Resolve(achievementId);
        }

        public void RegisterRuntimeDefinition(AchievementDefinition definition)
        {
            if (definition == null)
                return;

            string id = definition.ResolvedId;
            runtimeDefinitions[id] = definition;
            AchievementRegistry.RegisterRuntimeAchievement(definition);
            GetOrCreateProgress(id);
        }

        public void ReportProgress(AchievementTriggerType triggerType, string targetId = null, int amount = 1, bool setAbsolute = false)
        {
            if (amount < 0)
                return;

            if (!setAbsolute && amount <= 0)
                return;

            foreach (AchievementDefinition definition in EnumerateDefinitions())
            {
                if (definition == null || definition.triggerType != triggerType)
                    continue;

                if (!MatchesTarget(definition, targetId))
                    continue;

                if (setAbsolute)
                    SetProgress(definition, amount);
                else
                    IncrementProgress(definition, amount);
            }
        }

        public bool TryUnlock(string achievementId)
        {
            AchievementDefinition definition = GetDefinition(achievementId);
            if (definition == null)
                return false;

            AchievementProgress progress = GetOrCreateProgress(achievementId);
            if (progress.unlocked)
                return false;

            progress.currentCount = Mathf.Max(progress.currentCount, definition.targetCount);
            UnlockAchievement(progress, definition);
            return true;
        }

        public void ApplySave(AchievementProgress[] savedProgress)
        {
            if (savedProgress == null)
                return;

            for (int i = 0; i < savedProgress.Length; i++)
            {
                AchievementProgress saved = savedProgress[i];
                if (saved == null || string.IsNullOrEmpty(saved.achievementId))
                    continue;

                AchievementProgress progress = GetOrCreateProgress(saved.achievementId);
                progress.currentCount = saved.currentCount;
                progress.unlocked = saved.unlocked;
                progress.unlockedAtTicks = saved.unlockedAtTicks;
            }
        }

        public AchievementProgress[] BuildSaveSnapshot()
        {
            AchievementProgress[] snapshot = new AchievementProgress[progressById.Count];
            int index = 0;
            foreach (KeyValuePair<string, AchievementProgress> pair in progressById)
            {
                AchievementProgress source = pair.Value;
                snapshot[index++] = new AchievementProgress
                {
                    achievementId = source.achievementId,
                    currentCount = source.currentCount,
                    unlocked = source.unlocked,
                    unlockedAtTicks = source.unlockedAtTicks
                };
            }

            return snapshot;
        }

        private void RegisterAllDefinitions()
        {
            foreach (AchievementDefinition definition in AchievementRegistry.GetAllAchievements())
            {
                if (definition == null)
                    continue;

                runtimeDefinitions[definition.ResolvedId] = definition;
                GetOrCreateProgress(definition.ResolvedId);
            }
        }

        private IEnumerable<AchievementDefinition> EnumerateDefinitions()
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (AchievementDefinition definition in AchievementRegistry.GetAllAchievements())
            {
                if (definition == null)
                    continue;

                string id = definition.ResolvedId;
                if (!seen.Add(id))
                    continue;

                yield return definition;
            }

            foreach (KeyValuePair<string, AchievementDefinition> pair in runtimeDefinitions)
            {
                if (pair.Value == null || !seen.Add(pair.Key))
                    continue;

                yield return pair.Value;
            }
        }

        private static bool MatchesTarget(AchievementDefinition definition, string targetId)
        {
            if (string.IsNullOrEmpty(definition.targetId))
                return true;

            if (string.IsNullOrEmpty(targetId))
                return false;

            return string.Equals(definition.targetId, targetId, StringComparison.OrdinalIgnoreCase);
        }

        private void SetProgress(AchievementDefinition definition, int absoluteValue)
        {
            AchievementProgress progress = GetOrCreateProgress(definition.ResolvedId);
            if (progress.unlocked)
                return;

            progress.currentCount = Mathf.Clamp(absoluteValue, 0, definition.targetCount);
            NotifyProgress(progress, definition);

            if (progress.currentCount >= definition.targetCount)
                UnlockAchievement(progress, definition);
        }

        private void IncrementProgress(AchievementDefinition definition, int amount)
        {
            AchievementProgress progress = GetOrCreateProgress(definition.ResolvedId);
            if (progress.unlocked)
                return;

            progress.currentCount = Mathf.Min(definition.targetCount, progress.currentCount + amount);
            NotifyProgress(progress, definition);

            if (progress.currentCount >= definition.targetCount)
                UnlockAchievement(progress, definition);
        }

        private void UnlockAchievement(AchievementProgress progress, AchievementDefinition definition)
        {
            if (progress.unlocked)
                return;

            progress.unlocked = true;
            progress.unlockedAtTicks = DateTime.UtcNow.Ticks;
            progress.currentCount = Mathf.Max(progress.currentCount, definition.targetCount);

            int xp = definition.xpReward;
            if (definition.hidden && xp > 0)
                xp = Mathf.RoundToInt(xp * 1.5f);

            AchievementUnlockPopupUI.Show(definition.title, definition.description, xp);

            PlayerProgressionManager progression = PlayerProgressionManager.EnsureExists();
            if (xp > 0 && progression != null)
                progression.TryGrantXp(xp, XpSource.Achievement, $"achievement:{definition.ResolvedId}");

            OnAchievementUnlocked?.Invoke(progress, definition);
            NotifyProgress(progress, definition);
        }

        private AchievementProgress GetOrCreateProgress(string achievementId)
        {
            if (!progressById.TryGetValue(achievementId, out AchievementProgress progress))
            {
                progress = new AchievementProgress(achievementId);
                progressById[achievementId] = progress;
            }

            return progress;
        }

        private void NotifyProgress(AchievementProgress progress, AchievementDefinition definition)
        {
            OnProgressUpdated?.Invoke(progress, definition);
        }
    }
}

using System;
using System.Collections.Generic;
using Project.Quests;
using UnityEngine;

namespace Project.PPT
{
    public sealed class PptManager : MonoBehaviour
    {
        public static PptManager Instance { get; private set; }
        private const string SessionStartKeywordQuestId = "session_start";

        [SerializeField] private PptRegistry registryAsset;

        private readonly PptRegistryIndex registryIndex = new PptRegistryIndex();
        private readonly PptZoneCatalog zoneCatalog = new PptZoneCatalog();
        private PptDirectionResolver directionResolver;

        private readonly Dictionary<string, RuntimeEntry> runtimeEntries =
            new Dictionary<string, RuntimeEntry>(StringComparer.Ordinal);

        public event Action DirectionCandidatesChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            BootstrapRegistry();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Start()
        {
            zoneCatalog.Refresh();
            registryIndex.IndexSceneObjects();
            EnsureSessionStartKeywords();
        }

        public void EnsureSessionStartKeywords()
        {
            LogSessionStartKeywords();
        }

        public void RefreshCatalog()
        {
            zoneCatalog.Refresh();
            registryIndex.IndexSceneObjects();
            DirectionCandidatesChanged?.Invoke();
        }

        public bool TryGetEntry(string pptId, out PptEntry entry)
        {
            return registryIndex.TryGetEntry(pptId, out entry);
        }

        public bool TryGetNpcProfile(string npcId, out PptNpcProfile profile)
        {
            return registryIndex.TryGetNpcProfile(npcId, out profile);
        }

        public void RegisterRuntimeEntry(
            string pptId,
            string displayName,
            PptType type,
            string discoveryId,
            Vector3 position,
            string npcId = "")
        {
            runtimeEntries[pptId] = new RuntimeEntry
            {
                pptId = pptId,
                displayName = displayName,
                type = type,
                discoveryId = discoveryId,
                position = position,
                npcId = npcId
            };

            PptEntry runtime = PptEntry.CreateRuntime(pptId, displayName, type, discoveryId, position, npcId);
            registryIndex.RegisterEntry(runtime);
        }

        public bool TryGetRuntimePosition(string pptId, out Vector3 position)
        {
            position = Vector3.zero;
            if (!runtimeEntries.TryGetValue(pptId, out RuntimeEntry entry))
                return false;

            position = entry.position;
            return true;
        }

        public List<PptEntry> GetDirectionCandidates(string npcId, Vector3 npcPosition, int pageSize, int pageIndex, out int totalCount)
        {
            var results = new List<PptEntry>();
            totalCount = 0;

            if (!registryIndex.TryGetNpcProfile(npcId, out PptNpcProfile profile))
            {
                profile = ScriptableObject.CreateInstance<PptNpcProfile>();
            }

            var ranked = new List<PptEntry>();
            foreach (PptEntry entry in registryIndex.AllEntries)
            {
                if (!PassesNpcFilters(profile, entry, npcPosition))
                    continue;

                if (!PptDiscoveryGate.IsAvailableToPlayer(entry))
                    continue;

                ranked.Add(entry);
            }

            ranked.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal));
            totalCount = ranked.Count;

            int start = Mathf.Clamp(pageIndex, 0, int.MaxValue) * Mathf.Clamp(pageSize, 2, 3);
            int end = Mathf.Min(start + Mathf.Clamp(pageSize, 2, 3), ranked.Count);
            for (int i = start; i < end; i++)
                results.Add(ranked[i]);

            return results;
        }

        public PptDirectionResult ResolveDirection(string npcId, PptEntry target, Vector3 npcPosition)
        {
            registryIndex.TryGetNpcProfile(npcId, out PptNpcProfile profile);
            return directionResolver.Resolve(profile, target, npcPosition);
        }

        private bool PassesNpcFilters(PptNpcProfile profile, PptEntry entry, Vector3 npcPosition)
        {
            if (profile.ExcludedPptIds != null)
            {
                for (int i = 0; i < profile.ExcludedPptIds.Length; i++)
                {
                    if (string.Equals(profile.ExcludedPptIds[i], entry.PptId, StringComparison.Ordinal))
                        return false;
                }
            }

            if (profile.KnownPptIds != null && profile.KnownPptIds.Length > 0)
            {
                bool whitelisted = false;
                for (int i = 0; i < profile.KnownPptIds.Length; i++)
                {
                    if (string.Equals(profile.KnownPptIds[i], entry.PptId, StringComparison.Ordinal))
                    {
                        whitelisted = true;
                        break;
                    }
                }

                if (!whitelisted)
                    return false;
            }

            switch (profile.KnowledgeScope)
            {
                case PptKnowledgeScope.LocalBiome:
                    if (profile.HomeRegion != Project.Survival.World.IoSurfaceRegionId.None
                        && entry.SurfaceRegion != Project.Survival.World.IoSurfaceRegionId.None
                        && entry.SurfaceRegion != profile.HomeRegion)
                        return false;
                    break;
                case PptKnowledgeScope.QuestRelated:
                    if (!IsQuestRelated(entry))
                        return false;
                    break;
            }

            return true;
        }

        private static bool IsQuestRelated(PptEntry entry)
        {
            QuestManager quests = QuestManager.Instance;
            if (quests == null)
                return false;

            IReadOnlyList<QuestProgress> all = quests.GetAllProgress();
            for (int i = 0; i < all.Count; i++)
            {
                QuestProgress progress = all[i];
                if (progress == null || progress.status != QuestStatus.Active)
                    continue;

                QuestDefinition def = quests.GetDefinition(progress.questId);
                if (def?.objectives == null)
                    continue;

                for (int o = 0; o < def.objectives.Count; o++)
                {
                    QuestObjectiveDefinition objective = def.objectives[o];
                    if (objective == null)
                        continue;

                    if (string.Equals(objective.targetId, entry.QuestObjectiveTargetId, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(objective.targetId, entry.NpcId, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(objective.targetId, entry.QuestLocationId, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }

        private void BootstrapRegistry()
        {
            PptRegistry loaded = registryAsset;
            if (loaded == null)
                loaded = Resources.Load<PptRegistry>(PptRegistry.DefaultResourcePath);

            registryIndex.LoadRegistry(loaded);
            directionResolver = new PptDirectionResolver(registryIndex, zoneCatalog);
        }

        private void LogSessionStartKeywords()
        {
            PptRegistry registry = registryAsset;
            if (registry == null)
                registry = Resources.Load<PptRegistry>(PptRegistry.DefaultResourcePath);

            if (registry?.KeywordSources == null)
                return;

            for (int s = 0; s < registry.KeywordSources.Length; s++)
            {
                PptKeywordSource source = registry.KeywordSources[s];
                if (source?.QuestRules == null)
                    continue;

                for (int r = 0; r < source.QuestRules.Length; r++)
                {
                    PptKeywordSourceRule rule = source.QuestRules[r];
                    if (rule == null || !string.Equals(rule.QuestId, SessionStartKeywordQuestId, StringComparison.Ordinal))
                        continue;

                    PptKeywordLog.LogMany(rule.KeywordIdsOnAccept, "Camp briefing");
                }
            }
        }

        private struct RuntimeEntry
        {
            public string pptId;
            public string displayName;
            public PptType type;
            public string discoveryId;
            public Vector3 position;
            public string npcId;
        }
    }
}

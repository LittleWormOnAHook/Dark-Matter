using Project.Core;
using Project.Managers;
using Project.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Progression
{
    public class PlayerProgressionManager : MonoBehaviour
    {
        public const float StatBonusPerLevel = 0.05f;

        public static PlayerProgressionManager Instance { get; private set; }

        [SerializeField] private ProgressionCurveDefinition curveOverride;

        private ProgressionCurveDefinition curve;
        private int level = 1;
        private int currentXp;
        private int unspentSkillPoints;
        private readonly Dictionary<string, int> skillRanks = new Dictionary<string, int>();
        private readonly HashSet<string> claimedOneTimeXp = new HashSet<string>();
        private readonly HashSet<string> exploredXpIds = new HashSet<string>();

        public int Level => level;
        public int CurrentXp => currentXp;
        public int UnspentSkillPoints => unspentSkillPoints;
        public IReadOnlyDictionary<string, int> SkillRanks => skillRanks;

        public event Action OnXpChanged;
        public event Action<int, int> OnLevelUp;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            curve = curveOverride != null ? curveOverride : ProgressionCurveDefinitionLoader.LoadDefault();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static PlayerProgressionManager EnsureExists()
        {
            if (Instance != null)
                return Instance;

            PlayerProgressionManager found = FindAnyObjectByType<PlayerProgressionManager>();
            if (found != null)
                return found;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
                return player.GetComponent<PlayerProgressionManager>()
                    ?? player.AddComponent<PlayerProgressionManager>();

            SimpleGameManager gameManager = FindAnyObjectByType<SimpleGameManager>();
            if (gameManager != null)
                return gameManager.GetComponent<PlayerProgressionManager>()
                    ?? gameManager.gameObject.AddComponent<PlayerProgressionManager>();

            UIManager uiManager = FindAnyObjectByType<UIManager>();
            if (uiManager != null)
                return uiManager.GetComponent<PlayerProgressionManager>()
                    ?? uiManager.gameObject.AddComponent<PlayerProgressionManager>();

            return null;
        }

        public float GetLevelStatMultiplier() => 1f + (level - 1) * StatBonusPerLevel;

        public int GetXpRequiredForNextLevel() =>
            curve != null ? curve.GetXpRequiredForLevel(level + 1) : 100 + level * 25;

        public int GetXpProgressInCurrentLevel()
        {
            int totalForCurrent = curve != null ? curve.GetTotalXpForLevel(level) : 0;
            return Mathf.Max(0, currentXp - totalForCurrent);
        }

        public float GetXpProgressNormalized()
        {
            int required = GetXpRequiredForNextLevel();
            if (required <= 0)
                return 1f;

            return Mathf.Clamp01((float)GetXpProgressInCurrentLevel() / required);
        }

        public bool TryGrantXp(int amount, XpSource source, string oneTimeKey = null)
        {
            if (amount <= 0)
                return false;

            if (!string.IsNullOrEmpty(oneTimeKey))
            {
                if (claimedOneTimeXp.Contains(oneTimeKey))
                    return false;

                claimedOneTimeXp.Add(oneTimeKey);
            }

            currentXp += amount;
            OnXpChanged?.Invoke();

            int levelsGained = 0;
            while (GetXpProgressInCurrentLevel() >= GetXpRequiredForNextLevel())
            {
                level++;
                levelsGained++;
            }

            if (levelsGained > 0)
            {
                unspentSkillPoints += levelsGained;
                OnLevelUp?.Invoke(level, levelsGained);
                OnXpChanged?.Invoke();
            }

            return true;
        }

        public bool TryMarkExplorationXp(string explorationId, int xpAmount)
        {
            if (string.IsNullOrEmpty(explorationId) || exploredXpIds.Contains(explorationId))
                return false;

            exploredXpIds.Add(explorationId);
            return TryGrantXp(xpAmount, XpSource.Exploration, $"explore:{explorationId}");
        }

        public bool HasExplorationXp(string explorationId) =>
            !string.IsNullOrEmpty(explorationId) && exploredXpIds.Contains(explorationId);

        public bool TrySpendSkillPoint(string skillId, int costPerRank, int maxRank, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(skillId))
            {
                error = "Invalid skill.";
                return false;
            }

            skillRanks.TryGetValue(skillId, out int rank);
            if (rank >= maxRank)
            {
                error = "Skill is max rank.";
                return false;
            }

            if (unspentSkillPoints < costPerRank)
            {
                error = "Not enough skill points.";
                return false;
            }

            unspentSkillPoints -= costPerRank;
            skillRanks[skillId] = rank + 1;
            OnXpChanged?.Invoke();
            return true;
        }

        public int GetSkillRank(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return 0;

            return skillRanks.TryGetValue(skillId, out int rank) ? rank : 0;
        }

        public ProgressionSaveSnapshot BuildSaveSnapshot()
        {
            List<string> skillIds = new List<string>(skillRanks.Count);
            List<int> skillRankValues = new List<int>(skillRanks.Count);
            foreach (KeyValuePair<string, int> pair in skillRanks)
            {
                if (pair.Value <= 0)
                    continue;

                skillIds.Add(pair.Key);
                skillRankValues.Add(pair.Value);
            }

            return new ProgressionSaveSnapshot
            {
                playerLevel = level,
                playerXp = currentXp,
                unspentSkillPoints = unspentSkillPoints,
                allocatedSkillIds = skillIds.ToArray(),
                allocatedSkillRanks = skillRankValues.ToArray(),
                exploredXpIds = exploredXpIds.Count > 0 ? new List<string>(exploredXpIds).ToArray() : null,
                claimedOneTimeXpKeys = claimedOneTimeXp.Count > 0 ? new List<string>(claimedOneTimeXp).ToArray() : null
            };
        }

        public void ApplySaveSnapshot(ProgressionSaveSnapshot snapshot)
        {
            level = snapshot.playerLevel > 0 ? snapshot.playerLevel : 1;
            currentXp = snapshot.playerXp;
            unspentSkillPoints = snapshot.unspentSkillPoints;

            skillRanks.Clear();
            if (snapshot.allocatedSkillIds != null)
            {
                string[] ids = snapshot.allocatedSkillIds;
                int[] ranks = snapshot.allocatedSkillRanks;
                for (int i = 0; i < ids.Length; i++)
                {
                    if (string.IsNullOrEmpty(ids[i]))
                        continue;

                    int rank = ranks != null && i < ranks.Length ? ranks[i] : 1;
                    if (rank > 0)
                        skillRanks[ids[i]] = rank;
                }
            }

            exploredXpIds.Clear();
            if (snapshot.exploredXpIds != null)
            {
                for (int i = 0; i < snapshot.exploredXpIds.Length; i++)
                {
                    if (!string.IsNullOrEmpty(snapshot.exploredXpIds[i]))
                        exploredXpIds.Add(snapshot.exploredXpIds[i]);
                }
            }

            claimedOneTimeXp.Clear();
            if (snapshot.claimedOneTimeXpKeys != null)
            {
                for (int i = 0; i < snapshot.claimedOneTimeXpKeys.Length; i++)
                {
                    if (!string.IsNullOrEmpty(snapshot.claimedOneTimeXpKeys[i]))
                        claimedOneTimeXp.Add(snapshot.claimedOneTimeXpKeys[i]);
                }
            }

            OnXpChanged?.Invoke();
        }

        public void ResetToNewGame()
        {
            level = 1;
            currentXp = 0;
            unspentSkillPoints = 0;
            skillRanks.Clear();
            exploredXpIds.Clear();
            claimedOneTimeXp.Clear();
            OnXpChanged?.Invoke();
        }
    }

    [Serializable]
    public struct ProgressionSaveSnapshot
    {
        public int playerLevel;
        public int playerXp;
        public int unspentSkillPoints;
        public string[] allocatedSkillIds;
        public int[] allocatedSkillRanks;
        public string[] exploredXpIds;
        public string[] claimedOneTimeXpKeys;
    }

    internal static class ProgressionCurveDefinitionLoader
    {
        private static ProgressionCurveDefinition cached;

        public static ProgressionCurveDefinition LoadDefault()
        {
            if (cached != null)
                return cached;

            cached = Resources.Load<ProgressionCurveDefinition>("Progression/ProgressionCurve");
            if (cached != null)
                return cached;

            cached = ScriptableObject.CreateInstance<ProgressionCurveDefinition>();
            return cached;
        }
    }
}

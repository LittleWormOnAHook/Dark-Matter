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
        public const float WeaponDamageBonusPerLevel = 0.03f;

        public static PlayerProgressionManager Instance { get; private set; }

        [SerializeField] private ProgressionCurveDefinition curveOverride;

        private ProgressionCurveDefinition curve;

        /// <summary>
        /// Testing default new-game level (not demo/ship start). Reset to 1 (or 0-index convention) later for demo.
        /// </summary>
        public const int NewGameStartLevel = 5;

        /// <summary>Starter unspent SP granted at new game (separate from level-up SP 2→200 ≈ 959).</summary>
        public const int NewGameStartSkillPoints = 25;

        public const int MaxLevel = ProgressionCurveDefinition.MaxLevel;

        private int level = NewGameStartLevel;
        private int currentXp;
        private int unspentSkillPoints = NewGameStartSkillPoints;
        private bool xpBaselineInitialized;
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
            EnsureXpBaselineForCurrentLevel();
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

        /// <summary>Linear +3% weapon damage per level above 1 (level 1 = 1x, matching GetLevelStatMultiplier's convention).</summary>
        public float GetLevelWeaponDamageMultiplier() => 1f + (level - 1) * WeaponDamageBonusPerLevel;

        public int GetXpRequiredForNextLevel()
        {
            if (level >= MaxLevel)
                return 0;

            int next = level + 1;
            if (curve != null)
                return curve.GetXpRequiredForLevel(next);

            return ProgressionCurveDefinition.EvaluateHybridXp(next);
        }

        public int GetXpProgressInCurrentLevel()
        {
            int totalForCurrent = curve != null
                ? curve.GetTotalXpForLevel(level)
                : GetFallbackTotalXpForLevel(level);
            return Mathf.Max(0, currentXp - totalForCurrent);
        }

        public float GetXpProgressNormalized()
        {
            if (level >= MaxLevel)
                return 1f;

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
            int skillPointsGained = 0;
            while (level < MaxLevel)
            {
                int required = GetXpRequiredForNextLevel();
                if (required <= 0 || GetXpProgressInCurrentLevel() < required)
                    break;

                level++;
                levelsGained++;
                skillPointsGained += GetSkillPointsForLevel(level);
            }

            if (levelsGained > 0)
            {
                unspentSkillPoints += skillPointsGained;
                OnLevelUp?.Invoke(level, levelsGained);
                OnXpChanged?.Invoke();
            }

            return true;
        }

        /// <summary>
        /// Skill points awarded when the player reaches <paramref name="level"/> (prop B).
        /// Base: clamp(1 + floor((N−1)/10), 1, 5); +2 every 10th; +5 every 50th.
        /// Sum from levels 2→200 = 959 (plus <see cref="NewGameStartSkillPoints"/> at new game).
        /// </summary>
        public static int GetSkillPointsForLevel(int level)
        {
            if (level < 1)
                return 0;

            int basePoints = Mathf.Clamp(1 + ((level - 1) / 10), 1, 5);
            int bonus = 0;
            if (level % 10 == 0)
                bonus += 2;
            if (level % 50 == 0)
                bonus += 5;

            return basePoints + bonus;
        }

        /// <summary>Sum of <see cref="GetSkillPointsForLevel"/> for levels <paramref name="fromLevel"/>..<paramref name="toLevel"/> inclusive.</summary>
        public static int GetTotalSkillPointsFromLevels(int fromLevel, int toLevel)
        {
            if (toLevel < fromLevel)
                return 0;

            int total = 0;
            for (int n = fromLevel; n <= toLevel; n++)
                total += GetSkillPointsForLevel(n);

            return total;
        }

        private static int GetFallbackTotalXpForLevel(int forLevel)
        {
            forLevel = Mathf.Clamp(forLevel, 1, MaxLevel);
            int total = 0;
            for (int i = 2; i <= forLevel; i++)
                total += ProgressionCurveDefinition.EvaluateHybridXp(i);

            return total;
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
            level = snapshot.playerLevel > 0 ? snapshot.playerLevel : NewGameStartLevel;
            level = Mathf.Clamp(level, 1, MaxLevel);
            currentXp = snapshot.playerXp;
            unspentSkillPoints = snapshot.unspentSkillPoints;
            xpBaselineInitialized = true;

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
            level = NewGameStartLevel;
            unspentSkillPoints = NewGameStartSkillPoints;
            skillRanks.Clear();
            exploredXpIds.Clear();
            claimedOneTimeXp.Clear();
            xpBaselineInitialized = false;
            EnsureXpBaselineForCurrentLevel();
            OnXpChanged?.Invoke();
        }

        /// <summary>
        /// Lifetime XP must sit at the floor for the current level so in-level progress starts at 0.
        /// </summary>
        private void EnsureXpBaselineForCurrentLevel()
        {
            if (xpBaselineInitialized)
                return;

            if (curve == null)
                curve = curveOverride != null ? curveOverride : ProgressionCurveDefinitionLoader.LoadDefault();

            currentXp = curve != null
                ? curve.GetTotalXpForLevel(level)
                : GetFallbackTotalXpForLevel(level);
            xpBaselineInitialized = true;
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

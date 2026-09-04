using System.Collections.Generic;
using UnityEngine;

namespace Project.Progression
{
    /// <summary>
    /// Derives branch depth from prerequisite chains and maps depth to player-level bands and SP cost.
    /// Depth 0 → player levels 1–5, depth 1 → 6–10, depth 2 → 11–15, depth 3 → 16–20.
    /// SP cost per rank = depth + 1.
    /// </summary>
    public static class SkillTreeDepthUtility
    {
        public const int RanksPerNode = 5;
        public const int LevelsPerDepthBand = 5;

        private static readonly Dictionary<string, int> DepthCache = new Dictionary<string, int>();

        public static void ClearCache() => DepthCache.Clear();

        /// <summary>Shortest-path depth from tree roots (no prerequisites = depth 0).</summary>
        public static int GetBranchDepth(SkillDefinition skill)
        {
            if (skill == null)
                return 0;

            return GetBranchDepth(skill.ResolvedId, new HashSet<string>());
        }

        /// <summary>Player level required to purchase the given target rank (1-based).</summary>
        public static int GetRequiredPlayerLevelForRank(SkillDefinition skill, int targetRank)
        {
            if (skill == null || targetRank < 1)
                return 1;

            int depth = GetBranchDepth(skill);
            return depth * LevelsPerDepthBand + targetRank;
        }

        /// <summary>Skill points spent per rank at this node's branch depth.</summary>
        public static int GetSkillPointCostPerRank(SkillDefinition skill)
        {
            if (skill == null)
                return 1;

            return GetBranchDepth(skill) + 1;
        }

        private static int GetBranchDepth(string skillId, HashSet<string> visiting)
        {
            if (string.IsNullOrEmpty(skillId))
                return 0;

            if (DepthCache.TryGetValue(skillId, out int cached))
                return cached;

            if (!visiting.Add(skillId))
                return 0;

            SkillDefinition skill = SkillRegistry.Resolve(skillId);
            int depth = 0;
            if (skill?.prerequisiteSkillIds != null && skill.prerequisiteSkillIds.Length > 0)
            {
                for (int i = 0; i < skill.prerequisiteSkillIds.Length; i++)
                {
                    string prereqId = skill.prerequisiteSkillIds[i];
                    if (string.IsNullOrEmpty(prereqId))
                        continue;

                    depth = Mathf.Max(depth, GetBranchDepth(prereqId, visiting));
                }

                depth += 1;
            }

            visiting.Remove(skillId);
            DepthCache[skillId] = depth;
            return depth;
        }
    }
}

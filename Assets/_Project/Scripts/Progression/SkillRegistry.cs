using System.Collections.Generic;
using UnityEngine;

namespace Project.Progression
{
    [CreateAssetMenu(menuName = "Project/Progression/Skill Registry", fileName = "SkillRegistry")]
    public class SkillRegistry : ScriptableObject
    {
        private static SkillRegistry cached;

        private static List<SkillDefinition> cachedSkillList;
        private static IReadOnlyList<SkillDefinition> cachedSkillView;
        private static SkillRegistry cacheBuiltFrom;

        [SerializeField] private SkillDefinition[] skills;

        public static SkillRegistry Instance
        {
            get
            {
                if (cached == null)
                    cached = Resources.Load<SkillRegistry>("Progression/SkillRegistry");

                return cached;
            }
        }

        public static IReadOnlyList<SkillDefinition> GetAllSkills()
        {
            SkillRegistry registry = Instance;
            if (cachedSkillView != null && cacheBuiltFrom == registry)
                return cachedSkillView;

            List<SkillDefinition> result = new List<SkillDefinition>();
            if (registry?.skills != null)
            {
                for (int i = 0; i < registry.skills.Length; i++)
                {
                    if (registry.skills[i] != null)
                        result.Add(registry.skills[i]);
                }
            }

            if (result.Count == 0)
            {
                SkillDefinition[] loaded = Resources.LoadAll<SkillDefinition>("Progression/Skills");
                for (int i = 0; i < loaded.Length; i++)
                {
                    if (loaded[i] != null)
                        result.Add(loaded[i]);
                }
            }

            cachedSkillList = result;
            cachedSkillView = result.AsReadOnly();
            cacheBuiltFrom = registry;
            return cachedSkillView;
        }

        public static void InvalidateCache()
        {
            cachedSkillList = null;
            cachedSkillView = null;
            cacheBuiltFrom = null;
        }

        private void OnEnable()
        {
            InvalidateCache();
        }

        private void OnValidate()
        {
            InvalidateCache();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsOnDomainReload()
        {
            cached = null;
            InvalidateCache();
        }

        public static SkillDefinition Resolve(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return null;

            foreach (SkillDefinition skill in GetAllSkills())
            {
                if (skill != null && skill.ResolvedId == skillId)
                    return skill;
            }

            return null;
        }

        public static List<SkillDefinition> GetSkillsByCategory(SkillTreeCategory category)
        {
            List<SkillDefinition> result = new List<SkillDefinition>();
            foreach (SkillDefinition skill in GetAllSkills())
            {
                if (skill != null && skill.treeCategory == category)
                    result.Add(skill);
            }

            result.Sort((a, b) =>
            {
                int row = a.treeRow.CompareTo(b.treeRow);
                return row != 0 ? row : a.treeColumn.CompareTo(b.treeColumn);
            });
            return result;
        }
    }
}

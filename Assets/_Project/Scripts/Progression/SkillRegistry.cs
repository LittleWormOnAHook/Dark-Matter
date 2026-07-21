using System.Collections.Generic;
using UnityEngine;

namespace Project.Progression
{
    [CreateAssetMenu(menuName = "Project/Progression/Skill Registry", fileName = "SkillRegistry")]
    public class SkillRegistry : ScriptableObject
    {
        private static SkillRegistry cached;

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
            List<SkillDefinition> result = new List<SkillDefinition>();
            SkillRegistry registry = Instance;
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

            return result;
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
    }
}

using UnityEngine;

namespace Project.PPT
{
    [CreateAssetMenu(
        fileName = "PptRegistry",
        menuName = "Dark Matter: Genesis/PPT/Registry")]
    public class PptRegistry : ScriptableObject
    {
        public const string DefaultResourcePath = "PPT/PptRegistry";

        [SerializeField] private PptEntry[] entries;
        [SerializeField] private PptNpcProfile[] npcProfiles;
        [SerializeField] private PptKeywordSource[] keywordSources;

        public PptEntry[] Entries => entries;
        public PptNpcProfile[] NpcProfiles => npcProfiles;
        public PptKeywordSource[] KeywordSources => keywordSources;
    }
}

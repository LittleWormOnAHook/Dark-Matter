using System;
using UnityEngine;

namespace Project.PPT
{
    [Serializable]
    public class PptKeywordSourceRule
    {
        [SerializeField] private string questId;
        [SerializeField] private string[] keywordIdsOnAccept;
        [SerializeField] private string[] keywordIdsOnComplete;

        public string QuestId => questId;
        public string[] KeywordIdsOnAccept => keywordIdsOnAccept;
        public string[] KeywordIdsOnComplete => keywordIdsOnComplete;
    }

    [CreateAssetMenu(
        fileName = "ppt_keyword_source",
        menuName = "Dark Matter: Genesis/PPT/Keyword Source")]
    public class PptKeywordSource : ScriptableObject
    {
        [SerializeField] private PptKeywordSourceRule[] questRules;

        public PptKeywordSourceRule[] QuestRules => questRules;
    }
}

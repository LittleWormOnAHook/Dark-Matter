using Project.UI;
using UnityEngine;

namespace Project.Interaction
{
    public enum ScannerTargetCategory
    {
        Generic,
        Resource,
        Loot,
        Enemy,
        Quest,
        Interactable,
        Building
    }

    [System.Serializable]
    public struct ScannerHighlightRule
    {
        public string tag;
        public ScannerTargetCategory category;
        public Color outlineColor;
        [Range(0f, 1f)] public float alpha;
        public float durationSeconds;
        public int priority;

        public static ScannerHighlightRule FromPalette(
            string tagName,
            ScannerTargetCategory cat,
            Color color,
            float alphaValue,
            float duration,
            int rulePriority)
        {
            return new ScannerHighlightRule
            {
                tag = tagName,
                category = cat,
                outlineColor = color,
                alpha = alphaValue,
                durationSeconds = duration,
                priority = rulePriority
            };
        }
    }

    [CreateAssetMenu(fileName = "ScannerHighlightProfile", menuName = "Survival Pioneer/Scanner Highlight Profile")]
    public class ScannerHighlightProfile : ScriptableObject
    {
        [Header("Sweep")]
        public float sweepRange = 50f;
        public float sweepDuration = 0.85f;
        public int sweepSampleSteps = 12;

        [Header("Post-Scan")]
        public float defaultPostScanDuration = 10f;
        public float postScanRangeFalloff = 50f;

        [Header("Tag Rules (first match wins)")]
        public ScannerHighlightRule[] tagRules;

        [Header("Fallback")]
        public ScannerHighlightRule fallbackRule;

        [Header("Future Skill Upgrades")]
        public float skillRangeBonus;
        public string[] unlockedTagFilters;

        private static ScannerHighlightProfile cached;

        public float EffectiveSweepRange => Mathf.Max(4f, sweepRange + skillRangeBonus);

        public static ScannerHighlightProfile Load()
        {
            if (cached != null)
                return cached;

            cached = Resources.Load<ScannerHighlightProfile>("Scanner/ScannerHighlightProfile");
            if (cached == null)
                cached = CreateDefaultInstance();

            return cached;
        }

        public static ScannerHighlightProfile CreateDefaultInstance()
        {
            ScannerHighlightProfile profile = CreateInstance<ScannerHighlightProfile>();
            profile.sweepRange = 50f;
            profile.sweepDuration = 0.85f;
            profile.sweepSampleSteps = 12;
            profile.defaultPostScanDuration = 10f;
            profile.postScanRangeFalloff = 50f;
            profile.tagRules = new[]
            {
                ScannerHighlightRule.FromPalette("Enemy", ScannerTargetCategory.Enemy,
                    SurvivalPioneerUiPalette.DeepMagenta, 0.85f, 12f, 40),
                ScannerHighlightRule.FromPalette("Boss", ScannerTargetCategory.Enemy,
                    SurvivalPioneerUiPalette.RichFuchsia, 0.9f, 14f, 45),
                ScannerHighlightRule.FromPalette("Animal", ScannerTargetCategory.Enemy,
                    new Color(1f, 0.55f, 0.2f, 1f), 0.75f, 10f, 35),
                ScannerHighlightRule.FromPalette("Collectable", ScannerTargetCategory.Loot,
                    SurvivalPioneerUiPalette.Gold, 0.9f, 10f, 30),
                ScannerHighlightRule.FromPalette("Interactable", ScannerTargetCategory.Interactable,
                    SurvivalPioneerUiPalette.RichFuchsia, 0.8f, 10f, 25),
                ScannerHighlightRule.FromPalette("Wood", ScannerTargetCategory.Resource,
                    SurvivalPioneerUiPalette.PositiveGreen, 0.75f, 12f, 20),
                ScannerHighlightRule.FromPalette("Metal", ScannerTargetCategory.Resource,
                    new Color(0.45f, 0.85f, 1f, 1f), 0.75f, 12f, 20),
                ScannerHighlightRule.FromPalette("Dirt", ScannerTargetCategory.Resource,
                    SurvivalPioneerUiPalette.SoftBeigeGray, 0.7f, 12f, 18),
                ScannerHighlightRule.FromPalette("Building", ScannerTargetCategory.Building,
                    SurvivalPioneerUiPalette.SlateGray, 0.55f, 8f, 15),
            };
            profile.fallbackRule = ScannerHighlightRule.FromPalette(
                string.Empty,
                ScannerTargetCategory.Generic,
                new Color(0.35f, 1f, 0.82f, 1f),
                0.65f,
                10f,
                10);
            return profile;
        }

        public bool TryGetRuleForTag(string tag, out ScannerHighlightRule rule)
        {
            if (!string.IsNullOrEmpty(tag) && tagRules != null)
            {
                for (int i = 0; i < tagRules.Length; i++)
                {
                    if (tagRules[i].tag == tag)
                    {
                        rule = tagRules[i];
                        return true;
                    }
                }
            }

            rule = fallbackRule;
            return false;
        }

        public bool TryGetRuleForCategory(ScannerTargetCategory category, out ScannerHighlightRule rule)
        {
            if (tagRules != null)
            {
                for (int i = 0; i < tagRules.Length; i++)
                {
                    if (tagRules[i].category == category)
                    {
                        rule = tagRules[i];
                        return true;
                    }
                }
            }

            rule = fallbackRule;
            return false;
        }
    }
}

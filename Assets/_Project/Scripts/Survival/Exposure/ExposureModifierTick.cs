using UnityEngine;

namespace Project.Survival.Exposure
{
    public enum ExposureModifierKind
    {
        Buff = 0,
        Debuff = 1
    }

    /// <summary>
    /// Single buff or debuff chip for HUD / journal display.
    /// </summary>
    public struct ExposureModifierTick
    {
        public ExposureModifierKind Kind;
        public string Label;
        public string Source;
        public string IconGlyph;
        public Color Tint;
        public float Severity;
        /// <summary>Passive zone effects use -1.</summary>
        public float RemainingSeconds;

        public static ExposureModifierTick Buff(string label, string source, string glyph, Color tint, float severity = 1f)
        {
            return new ExposureModifierTick
            {
                Kind = ExposureModifierKind.Buff,
                Label = label,
                Source = source,
                IconGlyph = glyph,
                Tint = tint,
                Severity = severity,
                RemainingSeconds = -1f
            };
        }

        public static ExposureModifierTick Debuff(string label, string source, string glyph, Color tint, float severity = 1f)
        {
            return new ExposureModifierTick
            {
                Kind = ExposureModifierKind.Debuff,
                Label = label,
                Source = source,
                IconGlyph = glyph,
                Tint = tint,
                Severity = severity,
                RemainingSeconds = -1f
            };
        }
    }
}

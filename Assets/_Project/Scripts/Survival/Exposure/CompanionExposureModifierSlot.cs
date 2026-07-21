namespace Project.Survival.Exposure
{
    /// <summary>
    /// Per-expedition-slot companion buff/debuff readout for HUD and journal.
    /// </summary>
    public sealed class CompanionExposureModifierSlot
    {
        public int SlotIndex { get; internal set; }
        public string PioneerRecordId { get; internal set; } = string.Empty;
        public string DisplayName { get; internal set; } = string.Empty;
        public float ExposureLevel { get; internal set; }
        public ExposureModifierTick[] BuffTicks { get; internal set; } = System.Array.Empty<ExposureModifierTick>();
        public ExposureModifierTick[] DebuffTicks { get; internal set; } = System.Array.Empty<ExposureModifierTick>();
    }
}

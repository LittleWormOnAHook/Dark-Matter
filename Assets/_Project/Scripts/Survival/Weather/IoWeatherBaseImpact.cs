namespace Project.Survival.Weather
{
    /// <summary>How weather affects colony base operations (GDD A2b / A4).</summary>
    public enum IoWeatherBaseImpact
    {
        /// <summary>Normal production; surface-only hazard.</summary>
        None = 0,

        /// <summary>Queues continue at reduced rate; exterior injury/module risk.</summary>
        Reduced = 1,

        /// <summary>All queues pause; Command Center shelter required.</summary>
        FullPause = 2
    }
}

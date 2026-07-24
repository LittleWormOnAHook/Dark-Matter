namespace Project.Features.Validation
{
    /// <summary>Play Mode smoke key registry. F9 WorldState · F10 Directors (Run 1).</summary>
    public static class DarkMatterSmokeKeys
    {
        public const string WorldStateSummary = "F9";
        public const string DirectorsEval = "F10";
        public const string WeatherCommand = "F11";
        public const string ExperienceSummary = "F12";

        // Reserved for Communications Run 2
        public const string CommsEnqueue = "F5";
        public const string CommsEmergency = "F6";
        public const string CommsContext = "F7";
        public const string CommsAudio = "F8";

        public static readonly string[] AllRegistered =
        {
            CommsEnqueue,
            CommsEmergency,
            CommsContext,
            CommsAudio,
            WorldStateSummary,
            DirectorsEval,
            WeatherCommand,
            ExperienceSummary
        };

        public static string GetBindingLabel(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            return key;
        }
    }
}

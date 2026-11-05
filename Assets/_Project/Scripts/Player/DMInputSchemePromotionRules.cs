namespace Project.Player
{
    /// <summary>Scheme promotion thresholds (testable without Input System devices).</summary>
    internal static class DMInputSchemePromotionRules
    {
        public const float StickDeadzone = 0.28f;
        public const float SchemeSwitchCooldownSeconds = 0.35f;

        public static bool StickDeflects(float x, float y, float deadzone = StickDeadzone)
        {
            return x * x + y * y > deadzone * deadzone;
        }
    }
}

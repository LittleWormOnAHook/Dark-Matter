namespace Project.Core
{
    public enum GamePhase
    {
        MainMenu,
        StarterPioneerSelect,
        StartPopup,
        Playing
    }

    public static class GameSession
    {
        public static GamePhase Phase { get; private set; } = GamePhase.MainMenu;

        public static bool HasStarted => Phase == GamePhase.Playing;

        public static bool IsInMainMenu => Phase == GamePhase.MainMenu;

        public static void SetPhase(GamePhase phase)
        {
            Phase = phase;
        }

        public static void MarkStarted()
        {
            Phase = GamePhase.Playing;
        }

        public static void ResetSession()
        {
            Phase = GamePhase.MainMenu;
        }
    }
}

using System;
using UnityEngine;

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

        public static event Action GameStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSessionOnPlay()
        {
            Phase = GamePhase.MainMenu;
        }

        public static void SetPhase(GamePhase phase)
        {
            Phase = phase;
        }

        public static void MarkStarted()
        {
            if (HasStarted)
                return;

            Phase = GamePhase.Playing;
            GameStarted?.Invoke();
        }

        public static void ResetSession()
        {
            Phase = GamePhase.MainMenu;
        }
    }
}

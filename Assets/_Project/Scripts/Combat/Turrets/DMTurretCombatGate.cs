using Project.Core;
using Project.Player;
using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>Auto turret brains idle on main menu / paused gameplay (no scan, no fire).</summary>
    public static class DMTurretCombatGate
    {
        public static bool ShouldRunAutoBrain()
        {
            if (!Application.isPlaying)
                return false;
            if (!GameSession.HasStarted || GameSession.IsInMainMenu)
                return false;
            if (GameSession.Phase == GamePhase.StartPopup
                || GameSession.Phase == GamePhase.StarterPioneerSelect)
                return false;

            PlayerController player = PlayerLocator.FindPlayerController();
            if (player != null && player.IsGameplayPaused)
                return false;

            return true;
        }
    }
}

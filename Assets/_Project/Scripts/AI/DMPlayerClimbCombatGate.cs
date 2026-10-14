using Project.Features.Climb;
using Project.Companions;
using Project.Survival;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Melee-only gate: player on <see cref="DMClimbController"/> must not be swung at (ranged unchanged).
    /// </summary>
    public static class DMPlayerClimbCombatGate
    {
        public static bool IsPlayerSurvivalTarget(Transform candidate)
        {
            if (candidate == null)
                return false;

            if (candidate.GetComponentInParent<PioneerCompanionAgent>() != null)
                return false;

            return candidate.GetComponentInParent<SurvivalStats>() != null;
        }

        public static bool IsPlayerClimbing(Transform candidate)
        {
            if (candidate == null)
                return false;

            DMClimbController climb = candidate.GetComponentInParent<DMClimbController>();
            return climb != null && climb.IsClimbing;
        }

        public static bool BlocksMeleeAgainstTarget(Transform candidate, DM_EnemyBrainProfile profile)
        {
            if (!IsPlayerSurvivalTarget(candidate))
                return false;

            if (profile != null && profile.allowMeleeVsClimbingPlayer)
                return false;

            return IsPlayerClimbing(candidate);
        }
    }
}

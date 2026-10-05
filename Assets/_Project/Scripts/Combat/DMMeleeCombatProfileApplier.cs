using Invector.vMelee;
using Project.Player.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Applies DM_CombatCoreProfile melee block cone to Invector melee managers on the player.
    /// </summary>
    public static class DMMeleeCombatProfileApplier
    {
        public static void ApplyToPlayer(PioneerInvectorBootstrap bootstrap)
        {
            if (bootstrap == null)
                return;

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile == null)
                return;

            vMeleeManager meleeManager = bootstrap.MeleeManager;
            if (meleeManager == null)
                return;

            meleeManager.defaultDefenseRange = profile.meleeBlockDefenseHalfAngle;
        }
    }
}

using Project.Core;
using Project.Player;
using Project.Player.Invector;
using Project.Survival;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>Recognizes player / pioneer outgoing damage for engaged HUD and feedback routing.</summary>
    public static class CombatPlayerSourceUtility
    {
        public static bool IsPlayerOrPioneerSource(GameObject source)
        {
            if (source == null)
                return false;

            if (source.CompareTag("Player"))
                return true;

            if (source.GetComponentInParent<PlayerController>() != null)
                return true;

            if (source.GetComponentInParent<PioneerInvectorBootstrap>() != null)
                return true;

            if (source.GetComponentInParent<PioneerInvectorDamageBridge>() != null)
                return true;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null && (source == player || source.transform.IsChildOf(player.transform)))
                return true;

            SurvivalStats stats = source.GetComponentInParent<SurvivalStats>();
            return stats != null && source.GetComponentInParent<Project.Companions.CompanionHealth>() == null;
        }

        /// <summary>Prefer the pioneer root when the hit source is a weapon / hitbox child.</summary>
        public static GameObject NormalizeForDamageEvents(GameObject source)
        {
            if (source == null)
                return null;

            PioneerInvectorBootstrap bootstrap = source.GetComponentInParent<PioneerInvectorBootstrap>();
            if (bootstrap != null)
                return bootstrap.gameObject;

            PlayerController player = source.GetComponentInParent<PlayerController>();
            if (player != null)
                return player.gameObject;

            GameObject located = PlayerLocator.FindPlayerObject();
            if (located != null && source.transform.IsChildOf(located.transform))
                return located;

            return source;
        }
    }
}

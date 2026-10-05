using System.Collections.Generic;
using Invector.vMelee;
using Project.AI;
using Project.Companions;
using Project.Interaction;
using Project.Player.Invector;
using Project.Survival;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Validates Invector melee overlaps for Pioneer player/companion outgoing swings.
    /// Prevents phantom damage/recoil on self, allies, triggers, and Default-layer ground skims.
    /// </summary>
    public static class PioneerMeleeOutgoingHitFilter
    {
        public static bool IsAttackerPlayerSide(vMeleeManager meleeManager)
        {
            if (meleeManager == null)
                return false;

            return meleeManager.GetComponentInParent<PioneerInvectorBootstrap>() != null;
        }

        public static bool IsExcludedAttackerCollider(vMeleeManager meleeManager, Collider other)
        {
            if (meleeManager == null || other == null)
                return true;

            Transform attackerRoot = meleeManager.transform.root;
            if (other.transform.root == attackerRoot)
                return true;

            if (other.transform.IsChildOf(meleeManager.transform))
                return true;

            return false;
        }

        public static bool IsValidOutgoingDamageTarget(Collider other, HitProperties hitProperties)
        {
            if (other == null || other.isTrigger)
                return false;

            if (hitProperties != null
                && hitProperties.hitDamageTags != null
                && hitProperties.hitDamageTags.Count > 0
                && !MatchesDamageTag(other, hitProperties.hitDamageTags))
            {
                return false;
            }

            IDamageable damageable = DamageableUtility.GetDamageable(other);
            if (damageable == null)
                return false;

            if (damageable is SurvivalStats || damageable is CompanionHealth)
                return false;

            if (damageable is EnemyHealth enemyHealth && enemyHealth.IsDead)
                return false;

            return true;
        }

        /// <summary>
        /// Scaled weapon hitboxes can skim terrain on strong swings and trigger Invector recoil
        /// (mid-torso feedback with no enemy). Suppress recoil unless the collider is a real damage target.
        /// </summary>
        public static bool ShouldSuppressWorldRecoil(vMeleeManager meleeManager, Collider other)
        {
            if (!IsAttackerPlayerSide(meleeManager) || other == null || other.isTrigger)
                return false;

            if (IsValidOutgoingDamageTarget(other, meleeManager != null ? meleeManager.hitProperties : null))
                return false;

            return other.gameObject.layer == 0;
        }

        private static bool MatchesDamageTag(Collider other, List<string> tags)
        {
            if (other == null || tags == null || tags.Count == 0)
                return false;

            Transform node = other.transform;
            while (node != null)
            {
                if (tags.Contains(node.tag))
                    return true;
                node = node.parent;
            }

            return false;
        }
    }
}

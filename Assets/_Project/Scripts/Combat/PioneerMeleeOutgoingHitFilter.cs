using System.Collections.Generic;
using Invector.vEventSystems;
using Invector.vMelee;
using Invector.vShooter;
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
    /// Charged strong release also ignores enemy outgoing blades unless the target is guarding.
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

            if (ShouldIgnoreChargedEnemyWeapon(other))
                return false;

            return true;
        }

        /// <summary>
        /// Scaled weapon hitboxes can skim terrain on strong swings and trigger Invector recoil
        /// (mid-torso feedback with no enemy). Suppress recoil unless the collider is a real damage target.
        /// Charged release also suppresses enemy-blade overlaps so the swing can wait for a body hit.
        /// </summary>
        public static bool ShouldSuppressWorldRecoil(vMeleeManager meleeManager, Collider other)
        {
            if (!IsAttackerPlayerSide(meleeManager) || other == null || other.isTrigger)
                return false;

            if (ShouldIgnoreChargedEnemyWeapon(other))
                return true;

            if (IsValidOutgoingDamageTarget(other, meleeManager != null ? meleeManager.hitProperties : null))
                return false;

            return other.gameObject.layer == 0;
        }

        /// <summary>
        /// Charged Strong B / AttackC: skip the defender's outgoing blade and leftover weapon
        /// volumes. Body / dummy hits still land. Block or parry stays a valid guard event.
        /// </summary>
        public static bool ShouldIgnoreChargedEnemyWeapon(Collider other)
        {
            if (other == null)
                return false;

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile != null && !profile.chargedHitsIgnoreEnemyWeapons)
                return false;

            PioneerShooterMeleeInput meleeInput = ResolvePlayerMeleeInput();
            if (meleeInput == null || !meleeInput.IsStrongMeleeDamageActive)
                return false;

            if (!IsOutgoingEnemyWeaponCollider(other))
                return false;

            return !IsTargetGuarding(other);
        }

        private static PioneerShooterMeleeInput ResolvePlayerMeleeInput()
        {
            PioneerInvectorBootstrap bootstrap = PioneerInvectorBootstrap.Instance;
            if (bootstrap == null)
                return null;

            return bootstrap.GetComponent<PioneerShooterMeleeInput>();
        }

        private static bool IsTargetGuarding(Collider other)
        {
            if (other == null)
                return false;

            vIMeleeFighter fighter = other.GetComponentInParent<vIMeleeFighter>();
            return fighter != null && fighter.isBlocking;
        }

        private static bool IsOutgoingEnemyWeaponCollider(Collider other)
        {
            if (other == null)
                return false;

            if (other.GetComponent<vHitBox>() != null)
                return true;
            if (other.GetComponentInParent<vHitBox>() != null)
                return true;
            if (other.GetComponentInParent<vMeleeWeapon>() != null)
                return true;
            if (other.GetComponentInParent<vShooterWeapon>() != null)
                return true;
            if (other.GetComponentInParent<WeaponHitbox>() != null)
                return true;

            Transform node = other.transform;
            while (node != null)
            {
                string name = node.name;
                if (name.StartsWith("Drawn_", System.StringComparison.Ordinal)
                    || name.StartsWith("Holstered_", System.StringComparison.Ordinal)
                    || name.Equals("WeaponHitbox", System.StringComparison.Ordinal)
                    || name.Equals("hitBox", System.StringComparison.OrdinalIgnoreCase))
                    return true;
                node = node.parent;
            }

            return false;
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

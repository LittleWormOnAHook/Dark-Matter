using System;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Raised when the player or a companion takes damage from a specific enemy, so the rest of
    /// the squad can react to the attacker even if it's outside their own aggro/threat-cone range
    /// (e.g. a companion getting shot from off-screen, or one being flanked while others aren't
    /// close enough or facing the right way to notice on their own).
    /// </summary>
    public static class PlayerCombatEvents
    {
        public static event Action<EnemyHealth> OnPlayerAttackedBy;

        /// <summary>Raised whenever ANY companion is attacked, regardless of distance between
        /// companions or whether the others can see the attack happen.</summary>
        public static event Action<EnemyHealth> OnCompanionAttackedBy;

        public static void RaisePlayerAttackedBy(EnemyHealth attacker)
        {
            if (attacker == null || attacker.IsDead)
                return;

            OnPlayerAttackedBy?.Invoke(attacker);
        }

        public static void RaiseCompanionAttackedBy(EnemyHealth attacker)
        {
            if (attacker == null || attacker.IsDead)
                return;

            OnCompanionAttackedBy?.Invoke(attacker);
        }

        /// <summary>
        /// Resolves an EnemyHealth from an Invector damage sender transform (collider may live
        /// on a child of the enemy root), and raises the event if one is found.
        /// </summary>
        public static void RaisePlayerAttackedBySender(Transform sender)
        {
            EnemyHealth enemy = ResolveSenderEnemy(sender);
            if (enemy != null)
                RaisePlayerAttackedBy(enemy);
        }

        /// <summary>
        /// Resolves an EnemyHealth from an Invector damage sender transform and raises the
        /// companion-attacked event if one is found.
        /// </summary>
        public static void RaiseCompanionAttackedBySender(Transform sender)
        {
            EnemyHealth enemy = ResolveSenderEnemy(sender);
            if (enemy != null)
                RaiseCompanionAttackedBy(enemy);
        }

        private static EnemyHealth ResolveSenderEnemy(Transform sender)
        {
            return sender != null ? sender.GetComponentInParent<EnemyHealth>() : null;
        }
    }
}

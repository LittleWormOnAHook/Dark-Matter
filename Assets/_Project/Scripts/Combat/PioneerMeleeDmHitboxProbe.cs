using Invector.vMelee;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Layer <c>DMHitbox</c> is intentionally excluded from the physics collision matrix (no shoving).
    /// Invector melee relies on <see cref="Collider.OnTriggerEnter"/>, which never fires against those
    /// volumes — ranged uses explicit queries (<see cref="DMEnemyHitQuery"/>). While player weapon
    /// damage windows are open, mirror the weapon trigger bounds against live DM hitboxes and feed
    /// hits through the normal <see cref="vMeleeAttackObject.OnHit"/> path.
    /// </summary>
    public static class PioneerMeleeDmHitboxProbe
    {
        private const int BufferSize = 24;
        private static readonly Collider[] OverlapBuffer = new Collider[BufferSize];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            for (int i = 0; i < OverlapBuffer.Length; i++)
                OverlapBuffer[i] = null;
        }

        public static void ProbeManager(vMeleeManager meleeManager)
        {
            if (meleeManager == null || !DMEnemyHitQuery.AnyRigActive)
                return;

            if (!PioneerMeleeOutgoingHitFilter.IsAttackerPlayerSide(meleeManager))
                return;

            if (meleeManager.rightWeapon != null && meleeManager.rightWeapon.canApplyDamage)
                ProbeAttackObject(meleeManager.rightWeapon, meleeManager.gameObject);

            if (meleeManager.leftWeapon != null && meleeManager.leftWeapon.canApplyDamage)
                ProbeAttackObject(meleeManager.leftWeapon, meleeManager.gameObject);
        }

        private static void ProbeAttackObject(vMeleeAttackObject attackObject, GameObject owner)
        {
            if (attackObject == null || !attackObject.canApplyDamage || attackObject.hitBoxes == null)
                return;

            int mask = DMEnemyHitQuery.HitboxMask;
            for (int i = 0; i < attackObject.hitBoxes.Count; i++)
            {
                vHitBox hitBox = attackObject.hitBoxes[i];
                if (hitBox == null || (hitBox.triggerType & vHitBoxType.Damage) == 0)
                    continue;

                Collider trigger = hitBox.trigger;
                if (trigger == null || !trigger.enabled)
                    continue;

                Bounds bounds = trigger.bounds;
                Vector3 halfExtents = bounds.extents;
                if (halfExtents.sqrMagnitude <= 0.000001f)
                    continue;

                int count = Physics.OverlapBoxNonAlloc(
                    bounds.center,
                    halfExtents,
                    OverlapBuffer,
                    Quaternion.identity,
                    mask,
                    QueryTriggerInteraction.Collide);

                for (int c = 0; c < count; c++)
                {
                    Collider other = OverlapBuffer[c];
                    OverlapBuffer[c] = null;
                    if (other == null)
                        continue;

                    if (!DMEnemyHitQuery.TryGetHitbox(other, out _))
                        continue;

                    if (CombatHitResolver.IsOwnerCollider(owner, other))
                        continue;

                    attackObject.OnHit(hitBox, other);
                }
            }
        }
    }
}

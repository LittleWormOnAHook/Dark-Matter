using Project.AI.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>Maps hit colliders / bones to <see cref="CombatBodyPart"/> zones (player-like limbs, head, torso).</summary>
    public static class DMCombatBodyPartUtility
    {
        public static CombatBodyPart ResolveFromTransform(Transform bone, Animator animator)
        {
            if (bone == null)
                return CombatBodyPart.None;

            DMEnemyHitbox hitbox = bone.GetComponent<DMEnemyHitbox>();
            if (hitbox != null)
                return hitbox.Zone;

            if (animator != null && animator.isHuman)
            {
                Transform walk = bone;
                while (walk != null)
                {
                    for (int i = 0; i < DMEnemyHitboxRig.StandardHumanBones.Length; i++)
                    {
                        HumanBodyBones human = DMEnemyHitboxRig.StandardHumanBones[i];
                        Transform mapped = animator.GetBoneTransform(human);
                        if (mapped == walk)
                            return DMEnemyHitboxRig.MapHumanBoneToZone(human);
                    }

                    walk = walk.parent;
                }
            }

            return ResolveFromBoneName(bone.name);
        }

        public static CombatBodyPart ResolveFromCollider(Collider collider, Animator animator)
        {
            if (collider == null)
                return CombatBodyPart.None;

            if (collider.TryGetComponent(out DMEnemyHitbox hitbox))
                return hitbox.Zone;

            return ResolveFromTransform(collider.transform, animator);
        }

        public static float ApplyZoneDamageMultiplier(
            float damage,
            CombatBodyPart zone,
            DM_EnemyHitMarkProfile profile)
        {
            if (damage <= 0f || zone == CombatBodyPart.None || profile == null)
                return damage;

            float mult = profile.ResolveZoneMultiplier(zone);
            return damage * Mathf.Max(0f, mult);
        }

        private static CombatBodyPart ResolveFromBoneName(string boneName)
        {
            string n = boneName != null ? boneName.ToLowerInvariant() : string.Empty;
            if (n.Contains("head"))
                return CombatBodyPart.Head;
            if (n.Contains("leg") || n.Contains("foot") || n.Contains("thigh") || n.Contains("calf") || n.Contains("knee"))
                return CombatBodyPart.Leg;
            if (n.Contains("arm") || n.Contains("hand") || n.Contains("shoulder") || n.Contains("elbow"))
                return CombatBodyPart.Arm;
            if (n.Contains("spine") || n.Contains("chest") || n.Contains("hip") || n.Contains("pelvis") || n.Contains("torso"))
                return CombatBodyPart.Torso;
            return CombatBodyPart.None;
        }
    }
}

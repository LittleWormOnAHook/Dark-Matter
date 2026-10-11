using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Tag on one per-bone trigger hitbox (layer DMHitbox). Created at runtime by <see cref="DMEnemyHitboxRig"/>.
    /// The collider collides with nothing; only ranged queries (DMEnemyHitQuery) look at it.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public sealed class DMEnemyHitbox : MonoBehaviour
    {
        [SerializeField] private CombatBodyPart zone = CombatBodyPart.Torso;
        [SerializeField] private float damageMultiplier = 1f;

        private DMEnemyHitboxRig rig;
        private DMPlayerHitboxRig playerRig;
        private Transform bone;
        private Collider shape;

        public DMEnemyHitboxRig Rig => rig;
        /// <summary>Set when this hitbox belongs to the player's <see cref="DMPlayerHitboxRig"/> (Rig is null then).</summary>
        public DMPlayerHitboxRig PlayerRig => playerRig;
        /// <summary>Bone this hitbox rides on. Burn marks parent here.</summary>
        public Transform Bone => bone != null ? bone : transform.parent;
        public CombatBodyPart Zone => zone;
        public float DamageMultiplier => damageMultiplier;
        public Collider Shape => shape;
        public bool IsLive => ((rig != null && rig.IsActive) || (playerRig != null && playerRig.IsActive)) && shape != null && shape.enabled;

        internal void Configure(DMPlayerHitboxRig owner, Transform hitBone, CombatBodyPart hitZone, Collider hitShape, float multiplier)
        {
            playerRig = owner;
            bone = hitBone;
            zone = hitZone;
            shape = hitShape;
            damageMultiplier = multiplier;
        }

        internal void Configure(DMEnemyHitboxRig owner, Transform hitBone, CombatBodyPart hitZone, Collider hitShape, float multiplier)
        {
            rig = owner;
            bone = hitBone;
            zone = hitZone;
            shape = hitShape;
            damageMultiplier = multiplier;
        }
    }
}

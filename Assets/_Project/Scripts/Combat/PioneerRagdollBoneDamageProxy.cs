using Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Thin <see cref="vIDamageReceiver"/> on ragdoll bone colliders. Invector
    /// <c>ApplyDamage</c> only queries receivers on the hit GameObject (not parents), so while
    /// bones are unlocked for knockdown/stagger, hits would otherwise silently drop. Forwards to
    /// the root <see cref="PioneerInvectorDamageReceiver"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class PioneerRagdollBoneDamageProxy : MonoBehaviour, vIDamageReceiver
    {
        private readonly OnReceiveDamage _onStartReceiveDamage = new OnReceiveDamage();
        private readonly OnReceiveDamage _onReceiveDamage = new OnReceiveDamage();

        [SerializeField] private PioneerInvectorDamageReceiver rootReceiver;

        public OnReceiveDamage onStartReceiveDamage => _onStartReceiveDamage;
        public OnReceiveDamage onReceiveDamage => _onReceiveDamage;

        public void Configure(PioneerInvectorDamageReceiver root)
        {
            rootReceiver = root;
            EnsureEnemyTag();
        }

        public void TakeDamage(vDamage damage)
        {
            if (damage == null)
                return;

            if (rootReceiver == null)
                rootReceiver = GetComponentInParent<PioneerInvectorDamageReceiver>();

            if (rootReceiver == null)
                return;

            // Avoid re-entrancy if a miswired proxy sits on the same object as the root receiver.
            if (rootReceiver.gameObject == gameObject)
                return;

            onStartReceiveDamage.Invoke(damage);
            rootReceiver.TakeDamage(damage);
            onReceiveDamage.Invoke(damage);
        }

        private void Awake()
        {
            if (rootReceiver == null)
                rootReceiver = GetComponentInParent<PioneerInvectorDamageReceiver>();

            EnsureEnemyTag();
        }

        private void EnsureEnemyTag()
        {
            // Melee hitboxes filter by hitDamageTags ("Enemy"); bone GOs are often Untagged.
            if (gameObject.CompareTag("Untagged"))
                gameObject.tag = "Enemy";
        }
    }
}

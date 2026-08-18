using Project.Companions;
using Project.Survival;
using UnityEngine;

namespace Project.Interaction
{
    public static class DamageableUtility
    {
        public static IDamageable GetDamageable(Collider collider)
        {
            if (collider == null)
                return null;

            CompanionHealth companion = collider.GetComponentInParent<CompanionHealth>();
            if (companion != null)
                return companion;

            SurvivalStats survival = collider.GetComponentInParent<SurvivalStats>();
            if (survival != null)
                return survival;

            if (collider.TryGetComponent(out IDamageable onCollider))
                return onCollider;

            IDamageable inParent = collider.GetComponentInParent<IDamageable>();
            if (inParent != null)
                return inParent;

            // Fallback: Unity interface lookups can miss proxies on ragdoll / nested colliders.
            MonoBehaviour[] behaviours = collider.GetComponentsInParent<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IDamageable damageable)
                    return damageable;
            }

            return null;
        }
    }
}

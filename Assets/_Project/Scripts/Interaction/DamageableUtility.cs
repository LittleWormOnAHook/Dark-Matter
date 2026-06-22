using UnityEngine;

namespace Project.Interaction
{
    public static class DamageableUtility
    {
        public static IDamageable GetDamageable(Collider collider)
        {
            if (collider == null)
                return null;

            MonoBehaviour[] behaviours = collider.GetComponentsInParent<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IDamageable damageable)
                    return damageable;
            }

            return null;
        }
    }
}

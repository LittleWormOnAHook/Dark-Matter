using UnityEngine;

namespace Project.Interaction
{
    public interface IDamageable
    {
        void TakeDamage(float damage, GameObject source, bool isCritical = false);
    }
}

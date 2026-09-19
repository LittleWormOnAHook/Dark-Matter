using Project.Core;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Schedules pooled particle / one-shot VFX return via <see cref="PoolManager"/> (runner survives disable).
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class PooledOneShotVfx : MonoBehaviour
    {
        public void Begin(float maxLifeSeconds, ParticleSystem[] cachedSystems = null)
        {
            PoolManager.ScheduleOneShotVfxRelease(gameObject, maxLifeSeconds, cachedSystems);
        }

        private void OnDisable()
        {
            PoolManager.TryReturnOrphanedPooledInstance(gameObject);
        }
    }
}

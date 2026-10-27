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
            // 2026-10-24af: parent (enemy body / dissolve root) was deactivated while this stays activeSelf:
            // reparenting is illegal inside the deactivation, so hand it back next frame via the pool runner.
            if (gameObject.activeSelf && !gameObject.activeInHierarchy && gameObject.scene.isLoaded
                && TryGetComponent(out PooledInstanceTag tag) && tag.SourcePool != null)
            {
                PoolManager.ReleaseDelayed(gameObject, 0f);
                return;
            }
            PoolManager.TryReturnOrphanedPooledInstance(gameObject);
        }
    }
}

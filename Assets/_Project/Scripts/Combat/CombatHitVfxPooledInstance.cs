using Project.Core;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Returns a pooled blood splatter instance after its particles finish.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class CombatHitVfxPooledInstance : MonoBehaviour
    {
        private ParticleSystem[] _particleSystems;

        public void Play(float scale)
        {
            transform.localScale = Vector3.one * scale;
            CombatVfxUtility.DisableVendorAutoReleaseBehaviours(gameObject);

            if (_particleSystems == null)
                _particleSystems = GetComponentsInChildren<ParticleSystem>(true);

            float releaseDelay = 2f;
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem ps = _particleSystems[i];
                if (ps == null)
                    continue;

                ParticleSystem.MainModule main = ps.main;
                main.loop = false;
                ps.Clear(true);
                ps.Play(true);
                releaseDelay = Mathf.Max(releaseDelay, main.duration + 1f);
            }

            PoolManager.ScheduleOneShotVfxRelease(gameObject, releaseDelay, _particleSystems);
        }

        private void OnDisable()
        {
            PoolManager.TryReturnOrphanedPooledInstance(gameObject);
        }
    }
}

using System.Collections;
using Project.Core;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Returns pooled particle / one-shot VFX to <see cref="PoolManager"/> after particles finish
    /// or a max lifetime elapses. Uses unscaled time so cleanup still runs while paused.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class PooledOneShotVfx : MonoBehaviour
    {
        private const float AlivePollInterval = 0.25f;

        private Coroutine _releaseRoutine;
        private ParticleSystem[] _particleSystems;

        public void Begin(float maxLifeSeconds, ParticleSystem[] cachedSystems = null)
        {
            if (_particleSystems == null && cachedSystems != null && cachedSystems.Length > 0)
                _particleSystems = cachedSystems;

            if (_releaseRoutine != null)
                StopCoroutine(_releaseRoutine);

            _releaseRoutine = StartCoroutine(ReleaseWhenFinished(Mathf.Max(0.05f, maxLifeSeconds)));
        }

        private IEnumerator ReleaseWhenFinished(float maxLifeSeconds)
        {
            if (_particleSystems == null || _particleSystems.Length == 0)
                _particleSystems = GetComponentsInChildren<ParticleSystem>(true);

            ParticleSystem[] systems = _particleSystems;
            float elapsed = 0f;
            const float minVisibleSeconds = 0.2f;
            var pollWait = new WaitForSecondsRealtime(AlivePollInterval);

            while (elapsed < maxLifeSeconds)
            {
                yield return pollWait;
                elapsed += AlivePollInterval;

                if (elapsed < minVisibleSeconds || systems.Length == 0)
                    continue;

                bool anyAlive = false;
                for (int i = 0; i < systems.Length; i++)
                {
                    ParticleSystem ps = systems[i];
                    if (ps != null && ps.IsAlive(true))
                    {
                        anyAlive = true;
                        break;
                    }
                }

                if (!anyAlive)
                    break;
            }

            _releaseRoutine = null;
            PoolManager.Release(gameObject);
        }

        private void OnDisable()
        {
            if (_releaseRoutine != null)
            {
                StopCoroutine(_releaseRoutine);
                _releaseRoutine = null;
            }
        }
    }
}

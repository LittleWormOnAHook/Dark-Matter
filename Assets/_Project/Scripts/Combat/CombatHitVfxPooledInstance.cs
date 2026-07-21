using System.Collections;
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
        private Coroutine _releaseRoutine;

        public void Play(float scale)
        {
            transform.localScale = Vector3.one * scale;

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

            if (_releaseRoutine != null)
                StopCoroutine(_releaseRoutine);

            _releaseRoutine = StartCoroutine(ReleaseAfterDelay(releaseDelay));
        }

        private IEnumerator ReleaseAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _releaseRoutine = null;
            CombatHitVfx.ReleaseToPool(gameObject);
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

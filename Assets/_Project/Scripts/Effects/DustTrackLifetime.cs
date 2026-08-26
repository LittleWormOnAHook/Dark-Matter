using System.Collections.Generic;
using UnityEngine;

namespace Project.Effects
{
    /// <summary>
    /// Unscaled 1s despawn + live cap for player dust tracks.
    /// Invector <c>vAudioSurface.SpawnParticle</c> Instantiates Dust Track with no Destroy;
    /// the prefab also had stopAction=None and playOnAwake=false so GOs sat in the hierarchy forever.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DustTrackLifetime : MonoBehaviour
    {
        public const float DefaultLifetimeSeconds = 1f;
        public const int DefaultMaxLive = 16;

        [SerializeField] private float lifetimeSeconds = DefaultLifetimeSeconds;

        private static readonly List<DustTrackLifetime> Live = new List<DustTrackLifetime>(20);
        private float expireUnscaled;
        private bool armed;

        public static void RegisterSpawned(GameObject instance)
        {
            if (instance == null)
                return;

            UnparentFromPlayer(instance.transform);
            DustTrackLifetime life = instance.GetComponent<DustTrackLifetime>();
            if (life == null)
                life = instance.AddComponent<DustTrackLifetime>();
            life.Arm();
        }

        private void OnEnable()
        {
            Arm();
        }

        private void OnDisable()
        {
            Live.Remove(this);
            armed = false;
        }

        private void OnDestroy()
        {
            Live.Remove(this);
            armed = false;
        }

        private void Update()
        {
            if (!armed)
                return;

            if (Time.unscaledTime >= expireUnscaled)
                RecycleNow();
        }

        public void Arm()
        {
            UnparentFromPlayer(transform);
            PrepareParticles();
            expireUnscaled = Time.unscaledTime + Mathf.Max(0.05f, lifetimeSeconds);
            armed = true;
            if (!Live.Contains(this))
                Live.Add(this);
            EnforceCap();
        }

        private void RecycleNow()
        {
            armed = false;
            Live.Remove(this);
            if (gameObject != null)
                Destroy(gameObject);
        }

        private static void EnforceCap()
        {
            while (Live.Count > DefaultMaxLive)
            {
                DustTrackLifetime oldest = Live[0];
                Live.RemoveAt(0);
                if (oldest != null)
                    oldest.RecycleNow();
            }
        }

        private void PrepareParticles()
        {
            ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (ps == null)
                    continue;

                ParticleSystem.MainModule main = ps.main;
                main.loop = false;
                main.stopAction = ParticleSystemStopAction.Destroy;
                main.useUnscaledTime = true;
                if (!ps.isPlaying)
                    ps.Play(true);
            }
        }

        private static void UnparentFromPlayer(Transform track)
        {
            if (track == null)
                return;

            Transform parent = track.parent;
            while (parent != null)
            {
                string parentName = parent.name;
                if (parentName.IndexOf("Player_v7", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    track.SetParent(null, true);
                    return;
                }

                parent = parent.parent;
            }
        }
    }
}

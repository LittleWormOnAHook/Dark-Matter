using System.Collections.Generic;
using UnityEngine;

namespace Project.Effects
{
    /// <summary>
    /// Reuses dust-track instances. Invector used to Instantiate the heavy Dust Track prefab
    /// on every plant (and often twice: trigger + animation event).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DustTrackLifetime : MonoBehaviour
    {
        public const float DefaultLifetimeSeconds = 1f;
        public const int DefaultMaxLive = 16;

        private static readonly Dictionary<int, Stack<DustTrackLifetime>> Pools = new Dictionary<int, Stack<DustTrackLifetime>>(4);
        private static readonly List<DustTrackLifetime> Live = new List<DustTrackLifetime>(20);

        [SerializeField] private float lifetimeSeconds = DefaultLifetimeSeconds;

        private int prefabId;
        private float expireUnscaled;
        private bool armed;
        private bool recycling;
        private ParticleSystem[] cachedSystems;

        public static GameObject Spawn(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Material overrideMat,
            Transform parent)
        {
            if (prefab == null)
                return null;

            DustTrackLifetime life = Rent(prefab, position, rotation, parent);
            if (life == null)
                return null;

            if (overrideMat != null)
            {
                var renderer = life.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                    renderer.sharedMaterial = overrideMat;
            }

            if (!life.gameObject.activeSelf)
                life.gameObject.SetActive(true);
            else
                life.Arm();

            return life.gameObject;
        }

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

        private static DustTrackLifetime Rent(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            int id = prefab.GetInstanceID();
            if (!Pools.TryGetValue(id, out Stack<DustTrackLifetime> stack))
            {
                stack = new Stack<DustTrackLifetime>(8);
                Pools[id] = stack;
            }

            while (stack.Count > 0)
            {
                DustTrackLifetime recycled = stack.Pop();
                if (recycled == null)
                    continue;

                recycled.recycling = false;
                recycled.transform.SetParent(parent, false);
                recycled.transform.SetPositionAndRotation(position, rotation);
                return recycled;
            }

            GameObject spawned = Object.Instantiate(prefab, position, rotation, parent);
            DustTrackLifetime life = spawned.GetComponent<DustTrackLifetime>();
            if (life == null)
                life = spawned.AddComponent<DustTrackLifetime>();
            life.prefabId = id;
            return life;
        }

        private void OnEnable()
        {
            if (!recycling)
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
            expireUnscaled = Time.unscaledTime + Mathf.Max(0.05f, ResolveLifetimeSeconds());
            armed = true;
            if (!Live.Contains(this))
                Live.Add(this);
            EnforceCap();
        }

        private void RecycleNow()
        {
            armed = false;
            Live.Remove(this);

            if (prefabId != 0)
            {
                if (!Pools.TryGetValue(prefabId, out Stack<DustTrackLifetime> stack))
                {
                    stack = new Stack<DustTrackLifetime>(8);
                    Pools[prefabId] = stack;
                }

                recycling = true;
                if (gameObject != null)
                    gameObject.SetActive(false);
                stack.Push(this);
                return;
            }

            if (gameObject != null)
                Destroy(gameObject);
        }

        private static void EnforceCap()
        {
            int cap = ResolveMaxLive();
            while (Live.Count > cap)
            {
                DustTrackLifetime oldest = Live[0];
                Live.RemoveAt(0);
                if (oldest != null)
                    oldest.RecycleNow();
            }
        }

        private static float ResolveLifetimeSeconds()
        {
            var profile = Project.Player.DMFootstepProfile.Live;
            return profile != null ? profile.dustLifetimeSeconds : DefaultLifetimeSeconds;
        }

        private static int ResolveMaxLive()
        {
            var profile = Project.Player.DMFootstepProfile.Live;
            return profile != null ? Mathf.Max(1, profile.maxLiveDust) : DefaultMaxLive;
        }

        private void PrepareParticles()
        {
            if (cachedSystems == null || cachedSystems.Length == 0)
                cachedSystems = GetComponentsInChildren<ParticleSystem>(true);

            ParticleSystem[] systems = cachedSystems;
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (ps == null)
                    continue;

                ParticleSystem.MainModule main = ps.main;
                main.loop = false;
                main.stopAction = ParticleSystemStopAction.None;
                main.useUnscaledTime = true;
                ps.Clear(true);
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

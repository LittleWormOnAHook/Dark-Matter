using UnityEngine;

namespace Project.Vehicles
{
    /// <summary>
    /// Scales up to four thruster particle systems with planar speed and boost state.
    /// </summary>
    [DisallowMultipleComponent]
    public class HovercraftThrusterVfx : MonoBehaviour
    {
        private const int MaxThrusters = 4;

        [SerializeField] private HovercraftProfile profile;
        [SerializeField] private HoverPhysicsDriver physicsDriver;
        [SerializeField] private HovercraftOccupancy occupancy;

        [Header("Thruster Particles (max 4)")]
        [SerializeField] private ParticleSystem[] thrusterParticles = new ParticleSystem[MaxThrusters];

        [Header("Emission")]
        [SerializeField] private float idleEmissionRate = 8f;
        [SerializeField] private float maxEmissionRate = 72f;
        [SerializeField] private float boostEmissionMultiplier = 1.85f;
        [SerializeField] private Vector2 startSpeedRange = new Vector2(1.5f, 6f);

        private ParticleSystem.MainModule[] _mainModules;
        private ParticleSystem.EmissionModule[] _emissionModules;
        private bool _modulesCached;

        private void Awake()
        {
            if (physicsDriver == null)
                physicsDriver = GetComponent<HoverPhysicsDriver>();

            if (occupancy == null)
                occupancy = GetComponent<HovercraftOccupancy>();

            CacheParticleModules();
        }

        public void Configure(
            HovercraftProfile hoverProfile,
            HoverPhysicsDriver driver,
            HovercraftOccupancy craftOccupancy,
            ParticleSystem[] particles = null)
        {
            profile = hoverProfile;
            physicsDriver = driver;
            occupancy = craftOccupancy;

            if (particles != null)
                thrusterParticles = particles;

            _modulesCached = false;
            CacheParticleModules();
        }

        private void Update()
        {
            if (!Application.isPlaying || physicsDriver == null)
                return;

            CacheParticleModules();

            bool active = occupancy == null || occupancy.IsOccupied;
            float speedRatio = physicsDriver.CurrentSpeedRatio;
            bool boosting = physicsDriver.BoosterActive && speedRatio > 0.01f;
            float boostMultiplier = boosting ? ResolveBoostMultiplier() : 1f;
            float emissionRate = active
                ? Mathf.Lerp(idleEmissionRate, maxEmissionRate, speedRatio) * boostMultiplier
                : 0f;
            float startSpeed = Mathf.Lerp(startSpeedRange.x, startSpeedRange.y, speedRatio) * (boosting ? 1.15f : 1f);

            for (int i = 0; i < thrusterParticles.Length && i < MaxThrusters; i++)
            {
                ParticleSystem ps = thrusterParticles[i];
                if (ps == null)
                    continue;

                if (!active)
                {
                    if (ps.isPlaying)
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

                    continue;
                }

                if (!ps.isPlaying)
                    ps.Play(true);

                if (_emissionModules != null && i < _emissionModules.Length)
                    _emissionModules[i].rateOverTime = emissionRate;

                if (_mainModules != null && i < _mainModules.Length)
                    _mainModules[i].startSpeed = startSpeed;
            }
        }

        private float ResolveBoostMultiplier()
        {
            if (profile != null && profile.boosterMultiplier > 1f)
                return Mathf.Lerp(1f, boostEmissionMultiplier, Mathf.Clamp01((profile.boosterMultiplier - 1f) / 2f));

            return boostEmissionMultiplier;
        }

        private void CacheParticleModules()
        {
            if (_modulesCached || thrusterParticles == null)
                return;

            int count = Mathf.Min(thrusterParticles.Length, MaxThrusters);
            _mainModules = new ParticleSystem.MainModule[count];
            _emissionModules = new ParticleSystem.EmissionModule[count];

            for (int i = 0; i < count; i++)
            {
                ParticleSystem ps = thrusterParticles[i];
                if (ps == null)
                    continue;

                _mainModules[i] = ps.main;
                _emissionModules[i] = ps.emission;
            }

            _modulesCached = true;
        }
    }
}

using QFX.SFX;
using UnityEngine;

namespace Project.Vehicles
{
    /// <summary>
    /// Scales up to four thruster particle systems with planar speed and boost state.
    /// Also drives child QFX SFX_EngineController engines from hover throttle.
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
        private SFX_EngineController[] _qfxEngines;

        private void Awake()
        {
            if (physicsDriver == null)
                physicsDriver = GetComponent<HoverPhysicsDriver>();

            if (occupancy == null)
                occupancy = GetComponent<HovercraftOccupancy>();

            CacheParticleModules();
            CacheQfxEngines();
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
            CacheQfxEngines();
        }

        private void Update()
        {
            if (!Application.isPlaying || physicsDriver == null)
                return;

            CacheParticleModules();

            bool active = occupancy != null && occupancy.IsOccupied;
            float speedRatio = physicsDriver.CurrentSpeedRatio;
            float throttle = physicsDriver.CurrentThrottle;
            bool boosting = physicsDriver.BoosterActive && throttle > 0.01f;
            float boostMultiplier = boosting ? ResolveBoostMultiplier() : 1f;
            float drive = Mathf.Max(speedRatio, throttle);
            float emissionRate = active
                ? Mathf.Lerp(idleEmissionRate, maxEmissionRate, drive) * boostMultiplier
                : 0f;
            float startSpeed = Mathf.Lerp(startSpeedRange.x, startSpeedRange.y, drive) * (boosting ? 1.15f : 1f);
            DriveQfxEngines(active, throttle, speedRatio, boosting);

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

        private void DriveQfxEngines(bool active, float throttle, float speedRatio, bool boosting)
        {
            if (_qfxEngines == null)
                CacheQfxEngines();
            if (_qfxEngines == null || _qfxEngines.Length == 0)
                return;

            float power = 0f;
            if (active)
            {
                power = Mathf.Max(throttle, speedRatio * 0.35f);
                if (boosting)
                    power = Mathf.Clamp01(power * boostEmissionMultiplier);
            }

            for (int i = 0; i < _qfxEngines.Length; i++)
            {
                SFX_EngineController engine = _qfxEngines[i];
                if (engine != null)
                    engine.SetPower(power);
            }
        }

        private void CacheQfxEngines()
        {
            _qfxEngines = GetComponentsInChildren<SFX_EngineController>(true);
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

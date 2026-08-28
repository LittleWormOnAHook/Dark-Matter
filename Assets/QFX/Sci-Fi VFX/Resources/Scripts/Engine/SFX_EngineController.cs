using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
namespace QFX.SFX
{
    public sealed class SFX_EngineController : MonoBehaviour
    {
        public float PowerFactor = 1;

        public KeyCode EngineKeyCode = KeyCode.W;

        public ParticleSystem FlareParticleSystem;
        public float FlareFactor = 150;

        public ParticleSystem SlowSparksParticleSystem;
        public float SlowSparksFactor = 10;

        public ParticleSystem FastSparksParticleSystem;
        public float FastSparksFactor = 10;

        public ParticleSystem DistortionParticleSystem;
        public float DistortionFactor;

        public GameObject EngineInner;

        [Tooltip("Keeps particle systems playing and toggles Emission on/off instead of Play/Stop.")]
        public bool useEmissionToggleOnly;

        // ONLY FOR THE DEMO
        public Text TextUi;

        private float _currentPower;
        private bool _isButtonHeld;
        private bool _externallyDriven;
        private Material _engineInnerMaterial;
        private bool _particlesPlaying;

        private void OnEnable()
        {
            if (EngineInner != null)
            {
                Renderer renderer = EngineInner.GetComponent<Renderer>();
                if (renderer != null)
                    _engineInnerMaterial = renderer.material;
            }

            if (useEmissionToggleOnly)
                EnsureParticlesRunning();
        }

        /// <summary>
        /// 0-1 engine power from hover/jetpack controls. Disables the demo W-key path.
        /// </summary>
        public void SetPower(float power)
        {
            _externallyDriven = true;
            _currentPower = Mathf.Clamp01(power);
        }

        /// <summary>Jetpack startup — particles stay playing, emission off until boost.</summary>
        public void InitializeJetpackDrive()
        {
            useEmissionToggleOnly = true;
            _externallyDriven = true;
            _currentPower = 0f;
            EnsureParticlesRunning();
        }

        private void Update()
        {
            if (useEmissionToggleOnly)
            {
                if (!_externallyDriven)
                    _currentPower = 0f;
            }
            else if (!_externallyDriven)
            {
                TickDemoInput();
            }

            ApplyPower(_currentPower);
        }

        private void TickDemoInput()
        {
            float enginePower = PowerFactor * Time.deltaTime;

            if (Input.GetKeyDown(EngineKeyCode))
                _isButtonHeld = true;
            else if (Input.GetKeyUp(EngineKeyCode))
                _isButtonHeld = false;

            if (_isButtonHeld)
                _currentPower += enginePower;
            else
                _currentPower -= enginePower;

            _currentPower = Mathf.Clamp01(_currentPower);
        }

        private void ApplyPower(float power)
        {
            bool on = power > 0.01f;
            if (useEmissionToggleOnly)
                SetEmission(on);
            else
                SetPlaying(on);

            if (FlareParticleSystem != null)
            {
                var flareForceModule = FlareParticleSystem.forceOverLifetime;
                flareForceModule.zMultiplier = -(FlareFactor * power);
            }

            if (SlowSparksParticleSystem != null)
            {
                var slowSparksMain = SlowSparksParticleSystem.main;
                var slowSpeedModule = slowSparksMain.startSpeed;
                slowSpeedModule.constantMin = power * SlowSparksFactor;
                slowSpeedModule.constantMax = power * (SlowSparksFactor + 7);
                slowSparksMain.startSpeed = slowSpeedModule;
            }

            if (FastSparksParticleSystem != null)
            {
                var fastSparksMain = FastSparksParticleSystem.main;
                var fastSpeedModule = fastSparksMain.startSpeed;
                fastSpeedModule.constantMin = power * FastSparksFactor;
                fastSpeedModule.constantMax = power * (FastSparksFactor + 10);
                fastSparksMain.startSpeed = fastSpeedModule;

                var noiseModule = FastSparksParticleSystem.noise;
                noiseModule.enabled = power > 0.01f;
            }

            if (DistortionParticleSystem != null)
            {
                var distortionModule = DistortionParticleSystem.forceOverLifetime;
                distortionModule.zMultiplier = -(DistortionFactor * power);
            }

            if (_engineInnerMaterial != null && _engineInnerMaterial.HasProperty("_TintColor"))
            {
                var tintColor = _engineInnerMaterial.GetColor("_TintColor");
                tintColor.a = power;
                _engineInnerMaterial.SetColor("_TintColor", tintColor);
            }

            if (TextUi != null)
                TextUi.text = ((int)(power * 100)).ToString(CultureInfo.InvariantCulture);
        }

        private void SetPlaying(bool on)
        {
            if (on == _particlesPlaying)
                return;

            _particlesPlaying = on;
            ParticleSystemStopBehavior stop = ParticleSystemStopBehavior.StopEmitting;
            SetPs(FlareParticleSystem, on, stop);
            SetPs(SlowSparksParticleSystem, on, stop);
            SetPs(FastSparksParticleSystem, on, stop);
            SetPs(DistortionParticleSystem, on, stop);
        }

        private void EnsureParticlesRunning()
        {
            EnsurePsPlaying(FlareParticleSystem);
            EnsurePsPlaying(SlowSparksParticleSystem);
            EnsurePsPlaying(FastSparksParticleSystem);
            EnsurePsPlaying(DistortionParticleSystem);
            SetEmission(false);
        }

        private void SetEmission(bool on)
        {
            SetPsEmission(FlareParticleSystem, on);
            SetPsEmission(SlowSparksParticleSystem, on);
            SetPsEmission(FastSparksParticleSystem, on);
            SetPsEmission(DistortionParticleSystem, on);
            _particlesPlaying = on;
        }

        private static void EnsurePsPlaying(ParticleSystem ps)
        {
            if (ps == null)
                return;

            if (!ps.isPlaying)
                ps.Play(true);
        }

        private static void SetPsEmission(ParticleSystem ps, bool on)
        {
            if (ps == null)
                return;

            EnsurePsPlaying(ps);

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = on;
        }

        private static void SetPs(ParticleSystem ps, bool on, ParticleSystemStopBehavior stop)
        {
            if (ps == null)
                return;
            if (on)
            {
                if (!ps.isPlaying)
                    ps.Play(true);
            }
            else if (ps.isPlaying)
            {
                ps.Stop(true, stop);
            }
        }

        private void OnDisable()
        {
            if (useEmissionToggleOnly)
                SetEmission(false);
            else
                SetPlaying(false);
        }
    }
}

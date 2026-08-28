using QFX.SFX;
using UnityEngine;

namespace Project.Features.Jetpack
{
    /// <summary>
    /// Drives both jetpack QFX engine stacks from <see cref="DMJetpackController.CurrentThrustVisual"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMJetpackThrusterVfx : MonoBehaviour
    {
        [SerializeField] private DMJetpackController jetpack;
        [SerializeField] private DMJetpackProfile profile;
        [SerializeField] private SFX_EngineController[] engineControllers;

        private float _smoothedPower;
        private float _powerVelocity;
        private bool _engineFactorsApplied;

        private void Reset()
        {
            jetpack = GetComponentInParent<DMJetpackController>();
            engineControllers = GetComponentsInChildren<SFX_EngineController>(true);
        }

        private void Awake()
        {
            if (jetpack == null)
                jetpack = GetComponentInParent<DMJetpackController>();
            if (engineControllers == null || engineControllers.Length == 0)
                engineControllers = GetComponentsInChildren<SFX_EngineController>(true);

            ConfigureEngineEmissionMode();
            ApplyEngineFactorMultipliers();
        }

        private void ConfigureEngineEmissionMode()
        {
            if (engineControllers == null)
                return;

            for (int i = 0; i < engineControllers.Length; i++)
            {
                SFX_EngineController engine = engineControllers[i];
                if (engine == null)
                    continue;

                engine.InitializeJetpackDrive();
            }
        }

        private void ApplyEngineFactorMultipliers()
        {
            if (_engineFactorsApplied || profile == null || engineControllers == null)
                return;

            for (int i = 0; i < engineControllers.Length; i++)
            {
                SFX_EngineController engine = engineControllers[i];
                if (engine == null)
                    continue;

                engine.FlareFactor *= profile.flareFactorMultiplier;
                engine.SlowSparksFactor *= profile.slowSparksFactorMultiplier;
                engine.FastSparksFactor *= profile.fastSparksFactorMultiplier;
                engine.DistortionFactor *= profile.distortionFactorMultiplier;
            }

            _engineFactorsApplied = true;
        }

        private void Update()
        {
            if (jetpack == null || engineControllers == null)
                return;

            if (profile != null && !_engineFactorsApplied)
                ApplyEngineFactorMultipliers();

            float target = MapThrustToAlpha(jetpack.CurrentThrustVisual);
            float smoothTime = profile != null ? profile.thrusterPowerSmoothTime : 0.2f;
            _smoothedPower = Mathf.SmoothDamp(
                _smoothedPower,
                target,
                ref _powerVelocity,
                smoothTime);

            for (int i = 0; i < engineControllers.Length; i++)
            {
                SFX_EngineController engine = engineControllers[i];
                if (engine == null)
                    continue;

                engine.SetPower(_smoothedPower);
            }
        }
        private float MapThrustToAlpha(float thrustVisual)
        {
            // Controller already bakes thrusterAlphaMin into fade values; zero must stay off.
            if (thrustVisual <= 0f)
                return 0f;

            if (profile == null)
                return thrustVisual;

            return Mathf.Clamp(thrustVisual, 0f, profile.thrusterAlphaMax);
        }
    }
}

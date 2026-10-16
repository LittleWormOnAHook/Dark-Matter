using UnityEngine;
using System;
using UnityEngine.Rendering.HighDefinition;

namespace BFX
{

    public class BFX_ShaderProperies : IScriptInstance
    {
        public BFX_BloodSettings BloodSettings;

        public AnimationCurve FloatCurve          = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float          GraphTimeMultiplier = 1, GraphIntensityMultiplier = 1;
        public float          TimeDelay           = 0;

 
        bool          isFrized;
        private float startTime;

        private static readonly int cutoutPropertyID     = Shader.PropertyToID("_Cutout");
        private static readonly int forwardDirPropertyID = Shader.PropertyToID("_DecalForwardDir");
        private static readonly int LightIntencity       = Shader.PropertyToID("_LightIntencity");

        float         _animationTimeLapsed;
        private float _timeLapsedBeforeFadeout;

        private DecalProjector decal;
        private Material       decalMat;

        public event Action OnAnimationFinished;

        private void Awake()
        {
            decal          = GetComponent<DecalProjector>();
            decalMat       = new Material(decal.material);
            decal.material = decalMat;

        }

        internal override void OnEnableExtended()
        {
            startTime = Time.time + TimeDelay;

            GetComponent<DecalProjector>().enabled = true;

            var eval = FloatCurve.Evaluate(0) * GraphIntensityMultiplier;
            decalMat.SetFloat(cutoutPropertyID, eval);
            decalMat.SetVector(forwardDirPropertyID, transform.up);
        }

        internal override void OnDisableExtended()
        {
            var eval = FloatCurve.Evaluate(0) * GraphIntensityMultiplier;
            decalMat.SetFloat(cutoutPropertyID, eval);

            _animationTimeLapsed     = 0;
            _timeLapsedBeforeFadeout = 0;
        }



        internal override void ManualUpdate()
        {

            var deltaTime = BloodSettings == null ? Time.deltaTime : Time.deltaTime * BloodSettings.AnimationSpeed;
            _timeLapsedBeforeFadeout += deltaTime;

            if (BloodSettings == null || _timeLapsedBeforeFadeout > BloodSettings.DecalLifeTimeSeconds - 15f || (_animationTimeLapsed / GraphTimeMultiplier) < 0.3f)
                _animationTimeLapsed += deltaTime;

            var eval = FloatCurve.Evaluate(_animationTimeLapsed / GraphTimeMultiplier) * GraphIntensityMultiplier;
            decalMat.SetFloat(cutoutPropertyID, eval);

            if (BloodSettings != null) decalMat.SetFloat(LightIntencity, Mathf.Clamp(BloodSettings.LightIntensityMultiplier, 0.01f, 1f));

            if (_animationTimeLapsed >= GraphTimeMultiplier)
            {
                CanUpdate = false;
                OnAnimationFinished?.Invoke();
            }

            decalMat.SetVector(forwardDirPropertyID, transform.up);
        }
    }
}
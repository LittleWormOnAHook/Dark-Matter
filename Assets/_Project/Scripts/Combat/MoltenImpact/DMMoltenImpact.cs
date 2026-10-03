using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// One pooled molten impact: an additive glow quad on the surface that cools and fades,
    /// and a small world-space particle system of drips that fall, stop where they land and cool.
    /// Spawned by <see cref="DMMoltenImpacts"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMMoltenImpact : MonoBehaviour
    {
        private const float SurfaceOffset = 0.012f;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int NoiseId = Shader.PropertyToID("_Noise");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");
        private static readonly int DepthBiasId = Shader.PropertyToID("_DepthBias");
        private const float DripDepthBias = 0.02f;

        private Transform spot;
        private MeshRenderer spotRenderer;
        private ParticleSystem drips;
        private ParticleSystemRenderer dripRenderer;
        private MaterialPropertyBlock mpb;

        private DMMoltenImpactProfile profile;
        private float heat;
        private float startTime;
        private float endTime;
        private float seed;

        internal static DMMoltenImpact Create(Transform root, Mesh quad, Material spotMat, Material dripMat)
        {
            var go = new GameObject("MoltenImpact");
            go.transform.SetParent(root, false);
            go.SetActive(false);
            var fx = go.AddComponent<DMMoltenImpact>();
            fx.mpb = new MaterialPropertyBlock();

            var spotGo = new GameObject("Glow");
            spotGo.transform.SetParent(go.transform, false);
            spotGo.AddComponent<MeshFilter>().sharedMesh = quad;
            fx.spotRenderer = spotGo.AddComponent<MeshRenderer>();
            fx.spotRenderer.sharedMaterial = spotMat;
            fx.spotRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fx.spotRenderer.receiveShadows = false;
            fx.spotRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            fx.spotRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            fx.spotRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            fx.spot = spotGo.transform;

            var dripGo = new GameObject("Drips");
            dripGo.transform.SetParent(go.transform, false);
            fx.drips = dripGo.AddComponent<ParticleSystem>();
            fx.drips.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            fx.dripRenderer = dripGo.GetComponent<ParticleSystemRenderer>();
            fx.dripRenderer.sharedMaterial = dripMat;
            fx.dripRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            fx.dripRenderer.receiveShadows = false;
            fx.dripRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            fx.dripRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            fx.dripRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            fx.dripRenderer.lengthScale = 1f;
            fx.dripRenderer.minParticleSize = 0f;
            fx.dripRenderer.maxParticleSize = 0.5f;
            fx.ConfigureStaticModules();
            return fx;
        }

        private void ConfigureStaticModules()
        {
            ParticleSystem.MainModule main = drips.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.maxParticles = 48;
            main.startColor = Color.white;

            ParticleSystem.ShapeModule shape = drips.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 55f;
            shape.radius = 0.01f;

            ParticleSystem.CollisionModule col = drips.collision;
            col.type = ParticleSystemCollisionType.World;
            col.mode = ParticleSystemCollisionMode.Collision3D;
            col.quality = ParticleSystemCollisionQuality.High;
            col.bounce = 0f;
            col.dampen = 1f;
            col.lifetimeLoss = 0f;
            col.radiusScale = 0.5f;
            col.enableDynamicColliders = true;
            col.sendCollisionMessages = false;

            ParticleSystem.ColorOverLifetimeModule colLife = drips.colorOverLifetime;
            colLife.enabled = true;

            ParticleSystem.SizeOverLifetimeModule size = drips.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.7f, 0.85f), new Keyframe(1f, 0.6f)));

            ParticleSystem.LimitVelocityOverLifetimeModule limit = drips.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.dampen = 0.5f;
        }

        internal void Begin(DMMoltenImpactProfile p, float heatScale, Vector3 point, Vector3 normal, Transform attach, LayerMask dripMask)
        {
            profile = p;
            heat = Mathf.Max(0f, heatScale);
            seed = Random.value * 100f;
            startTime = Time.time;
            endTime = startTime + p.TotalLifetime + 0.1f;

            Transform t = transform;
            t.SetParent(attach, true);
            t.SetPositionAndRotation(point, Quaternion.LookRotation(normal, Mathf.Abs(normal.y) > 0.95f ? Vector3.forward : Vector3.up));
            CombatVfxUtility.NormalizeAttachedWorldScale(t);
            gameObject.SetActive(true);

            // Glow spot: quad facing out of the surface, nudged off it so it does not z-fight.
            spotRenderer.enabled = p.glow;
            if (p.glow)
            {
                float r = p.glowRadius * Random.Range(0.85f, 1.15f) * Mathf.Lerp(0.8f, 1.15f, Mathf.Clamp01(heat - 0.5f));
                spot.localPosition = new Vector3(0f, 0f, SurfaceOffset);
                spot.localRotation = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.forward);
                spot.localScale = Vector3.one * (r * 2f);
                ApplySpot(0f);
            }

            if (p.HasDrips)
                StartDrips(p, normal, dripMask);
            else
                drips.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void StartDrips(DMMoltenImpactProfile p, Vector3 normal, LayerMask dripMask)
        {
            drips.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            drips.transform.localPosition = new Vector3(0f, 0f, SurfaceOffset * 0.5f);
            drips.transform.localRotation = Quaternion.identity; // cone opens along the surface normal (+Z)

            ParticleSystem.MainModule main = drips.main;
            float run = p.dripRate > 0f ? p.dripRunSeconds : 0f;
            main.duration = Mathf.Max(0.05f, run);
            main.startLifetime = new ParticleSystem.MinMaxCurve(p.dripLifetime * 0.75f, p.dripLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(p.dripSpeed.x, p.dripSpeed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(p.dripSize.x, p.dripSize.y);
            main.gravityModifier = p.dripGravity;

            ParticleSystem.EmissionModule em = drips.emission;
            em.enabled = true;
            em.rateOverTime = run > 0f ? p.dripRate * Mathf.Clamp(heat, 0.25f, 2f) : 0f;
            int burst = Mathf.Clamp(Mathf.RoundToInt(p.dripBurst * Mathf.Clamp(heat, 0.25f, 2f)), 0, 32);
            em.SetBursts(burst > 0 ? new[] { new ParticleSystem.Burst(0f, (short)burst) } : new ParticleSystem.Burst[0]);

            ParticleSystem.CollisionModule col = drips.collision;
            col.enabled = p.dripsCollide;
            col.collidesWith = dripMask;

            ParticleSystem.LimitVelocityOverLifetimeModule limit = drips.limitVelocityOverLifetime;
            limit.limit = p.dripMaxSpeed;

            // Hot -> cool colour; additive shader, so alpha fades the glow out at the end.
            Color hot = p.hotColor;
            Color cool = p.coolColor;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.Lerp(hot, Color.white, 0.25f), 0f), new GradientColorKey(hot, 0.2f), new GradientColorKey(cool, 0.8f), new GradientColorKey(cool, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.6f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule colLife = drips.colorOverLifetime;
            colLife.color = new ParticleSystem.MinMaxGradient(g);

            dripRenderer.velocityScale = p.dripStretch;
            mpb.Clear();
            mpb.SetColor(ColorId, Color.white);
            mpb.SetFloat(IntensityId, p.dripIntensity * Mathf.Clamp(heat, 0.25f, 2f));
            mpb.SetFloat(DepthBiasId, DripDepthBias);
            dripRenderer.SetPropertyBlock(mpb);

            drips.Play(true);
        }

        private void ApplySpot(float age)
        {
            DMMoltenImpactProfile p = profile;
            float cool = Mathf.Clamp01(age / Mathf.Max(0.01f, p.coolSeconds));
            float fade = Mathf.Clamp01((age - p.coolSeconds) / Mathf.Max(0.01f, p.fadeSeconds));
            float k = cool * cool * (3f - 2f * cool);
            Color c = Color.Lerp(p.hotColor, p.coolColor, k);
            float intensity = p.glowIntensity * heat * Mathf.Lerp(1f, 0.25f, k) * (1f - fade);
            mpb.Clear();
            mpb.SetColor(ColorId, c);
            mpb.SetFloat(IntensityId, intensity);
            mpb.SetFloat(NoiseId, p.glowNoise);
            mpb.SetFloat(SeedId, seed);
            mpb.SetFloat(DepthBiasId, p.glowDepthBias);
            spotRenderer.SetPropertyBlock(mpb);
            if (fade >= 1f)
                spotRenderer.enabled = false;
        }

        private void Update()
        {
            if (profile == null)
            {
                StopNow();
                return;
            }

            float age = Time.time - startTime;
            if (spotRenderer.enabled)
                ApplySpot(age);

            if (Time.time >= endTime || (!spotRenderer.enabled && !drips.IsAlive(true)))
                StopNow();
        }

        /// <summary>End now and go back to the pool.</summary>
        public void StopNow()
        {
            profile = null;
            if (drips != null)
                drips.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            DMMoltenImpacts.Return(this);
        }

        private void OnDisable()
        {
            // The hit object was disabled or destroyed under us. No reparenting here (Unity forbids it
            // while a parent is deactivating); Update returns us to the pool if the parent comes back.
            if (profile != null)
            {
                profile = null;
                DMMoltenImpacts.Forget(this);
            }
        }
    }
}

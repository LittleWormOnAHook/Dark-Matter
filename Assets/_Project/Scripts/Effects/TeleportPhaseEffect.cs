using System.Collections;
using Project.Player;
using UnityEngine;

namespace Project.Effects
{
    /// <summary>
    /// Arrival-only teleport phase effect plus a short player input lock.
    /// </summary>
    public sealed class TeleportPhaseEffect : MonoBehaviour
    {
        private const float DefaultDuration = 3f;

        [Header("Phase Lock")]
        [Min(0f)] public float activationDuration = DefaultDuration;

        [Header("Primary Particles")]
        public Material particleMaterial;
        public Color colorA = new Color(0.74f, 0.18f, 0.48f, 0.85f);
        public Color colorB = new Color(0.83f, 0.63f, 0.09f, 0.9f);
        [Min(0f)] public float minLifetime = 0.55f;
        [Min(0f)] public float maxLifetime = 1.15f;
        [Min(0f)] public float minSpeed = 0.8f;
        [Min(0f)] public float maxSpeed = 2.4f;
        [Min(0f)] public float minStartSize = 0.08f;
        [Min(0f)] public float maxStartSize = 0.22f;
        [Min(0f)] public float emissionRate = 42f;
        [Min(0f)] public short entryBurstCount = 80;
        [Min(0f)] public short midBurstCount = 36;
        [Min(0f)] public float shapeRadius = 0.75f;
        [Range(0f, 1f)] public float radiusThickness = 0.08f;
        public Vector3 orbitalVelocity = new Vector3(0f, 1.35f, 0f);
        [Min(0f)] public float radialVelocity = 0.5f;

        [Header("Optional Extra Effect")]
        public GameObject extraEffectPrefab;
        public Vector3 extraEffectLocalOffset;
        public Vector3 extraEffectLocalEuler;
        public Vector3 extraEffectScale = Vector3.one;

        public static void PlayAt(Transform teleportedRoot, Vector3 position, TeleportPhaseEffect settings = null, float? overrideDuration = null)
        {
            if (!Application.isPlaying)
                return;

            GameObject effectObject = new GameObject("TeleportPhaseEffect");
            effectObject.transform.SetPositionAndRotation(position, Quaternion.identity);
            TeleportPhaseEffect effect = effectObject.AddComponent<TeleportPhaseEffect>();
            if (settings != null)
                effect.CopySettingsFrom(settings);

            float duration = Mathf.Max(0f, overrideDuration ?? effect.activationDuration);
            effect.Play(duration);

            PlayerController player = teleportedRoot != null
                ? teleportedRoot.GetComponentInChildren<PlayerController>()
                : null;
            if (player == null)
                player = FindAnyObjectByType<PlayerController>();

            player?.BeginTeleportPhaseLock(duration);
        }

        public static void PlayAt(Transform teleportedRoot, Vector3 position, float duration)
        {
            PlayAt(teleportedRoot, position, settings: null, overrideDuration: duration);
        }

        private void CopySettingsFrom(TeleportPhaseEffect source)
        {
            activationDuration = source.activationDuration;
            particleMaterial = source.particleMaterial;
            colorA = source.colorA;
            colorB = source.colorB;
            minLifetime = source.minLifetime;
            maxLifetime = source.maxLifetime;
            minSpeed = source.minSpeed;
            maxSpeed = source.maxSpeed;
            minStartSize = source.minStartSize;
            maxStartSize = source.maxStartSize;
            emissionRate = source.emissionRate;
            entryBurstCount = source.entryBurstCount;
            midBurstCount = source.midBurstCount;
            shapeRadius = source.shapeRadius;
            radiusThickness = source.radiusThickness;
            orbitalVelocity = source.orbitalVelocity;
            radialVelocity = source.radialVelocity;
            extraEffectPrefab = source.extraEffectPrefab;
            extraEffectLocalOffset = source.extraEffectLocalOffset;
            extraEffectLocalEuler = source.extraEffectLocalEuler;
            extraEffectScale = source.extraEffectScale;
        }

        private void Play(float duration)
        {
            ParticleSystem particles = gameObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.duration = duration;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLifetime, Mathf.Max(minLifetime, maxLifetime));
            main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, Mathf.Max(minSpeed, maxSpeed));
            main.startSize = new ParticleSystem.MinMaxCurve(minStartSize, Mathf.Max(minStartSize, maxStartSize));
            main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = emissionRate;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, entryBurstCount),
                new ParticleSystem.Burst(duration * 0.5f, midBurstCount),
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = shapeRadius;
            shape.radiusThickness = radiusThickness;

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(orbitalVelocity.x);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(orbitalVelocity.y);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(orbitalVelocity.z);
            velocity.radial = new ParticleSystem.MinMaxCurve(radialVelocity);

            ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
            size.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.1f),
                new Keyframe(0.25f, 1f),
                new Keyframe(1f, 0f));
            size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (particleMaterial != null)
                renderer.sharedMaterial = particleMaterial;

            SpawnExtraEffect();

            particles.Play();
            StartCoroutine(DestroyAfter(duration + 1.25f));
        }

        private void SpawnExtraEffect()
        {
            if (extraEffectPrefab == null)
                return;

            GameObject extra = Instantiate(extraEffectPrefab, transform);
            extra.transform.localPosition = extraEffectLocalOffset;
            extra.transform.localRotation = Quaternion.Euler(extraEffectLocalEuler);
            extra.transform.localScale = extraEffectScale == Vector3.zero ? Vector3.one : extraEffectScale;
        }

        private IEnumerator DestroyAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(gameObject);
        }
    }
}

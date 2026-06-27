using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project.Combat
{
    /// <summary>
    /// Soft smoke shell around dissolve targets. Volumetric layers use legacy particle shaders.
    /// </summary>
    [DisallowMultipleComponent]
    public class VolumetricSmokeEmitter : MonoBehaviour
    {
        private enum LegacyParticleBlend
        {
            AlphaBlended,
            AdditiveSoft,
            MultiplyDouble
        }
        private const string SmokePrefabAssetPath = "Assets/PolygonNature/FX/FX_Prefabs/Smoke_Light_FX.prefab";
        private const string SmokeFallbackPrefabAssetPath = "Assets/PolygonNature/FX/FX_Prefabs/Smoke_FX.prefab";
        private const string SmokeMaterialAssetPath = "Assets/PolygonNature/FX/FX_Materials/Smoke_Material.mat";
        private const float CharacterSmokeMaxRadius = 0.5f;

        public struct Settings
        {
            public float radius;
            public float radiusThickness;
            public Vector3 shapeScale;
            public float emissionRate;
            public float startSizeMin;
            public float startSizeMax;
            public float upwardSpeed;
            public float outwardSpeed;
            public Color tint;
            public bool loop;
            public float lifetimeMin;
            public float lifetimeMax;
        }

        private readonly System.Collections.Generic.List<ParticleSystem> spawnedSystems = new();
        private Coroutine releaseRoutine;
        private bool built;

        public static Settings EnemyDissolve => new Settings
        {
            radius = CharacterSmokeMaxRadius,
            radiusThickness = 0.92f,
            shapeScale = new Vector3(0.92f, 1.05f, 0.92f),
            emissionRate = 72f,
            startSizeMin = 0.35f,
            startSizeMax = 0.95f,
            upwardSpeed = 0.08f,
            outwardSpeed = 0.02f,
            tint = new Color(0.93f, 0.95f, 0.98f, 0.38f),
            loop = true,
            lifetimeMin = 2.2f,
            lifetimeMax = 4f
        };

        public static Settings LootBagIdle => new Settings
        {
            radius = 0.34f,
            radiusThickness = 0.78f,
            shapeScale = Vector3.one,
            emissionRate = 36f,
            startSizeMin = 0.28f,
            startSizeMax = 0.62f,
            upwardSpeed = 0.06f,
            outwardSpeed = 0.03f,
            tint = new Color(0.88f, 0.86f, 0.82f, 0.62f),
            loop = true,
            lifetimeMin = 1.8f,
            lifetimeMax = 3.2f
        };

        public static Settings LootBagDissolve => new Settings
        {
            radius = 0.42f,
            radiusThickness = 0.75f,
            shapeScale = Vector3.one,
            emissionRate = 88f,
            startSizeMin = 0.32f,
            startSizeMax = 0.78f,
            upwardSpeed = 0.1f,
            outwardSpeed = 0.05f,
            tint = new Color(0.92f, 0.9f, 0.86f, 0.72f),
            loop = false,
            lifetimeMin = 1.6f,
            lifetimeMax = 3.2f
        };

        public static Settings ForCharacterBounds(Bounds worldBounds, Settings template)
        {
            Settings settings = template;
            Vector3 extents = worldBounds.extents;
            float horizontal = Mathf.Max(extents.x, extents.z);
            settings.radius = Mathf.Clamp(horizontal * 0.42f, 0.22f, CharacterSmokeMaxRadius);
            settings.shapeScale = new Vector3(
                0.92f,
                Mathf.Clamp(extents.y / Mathf.Max(horizontal, 0.01f), 0.85f, 1.05f),
                0.92f);
            return settings;
        }

        public static VolumetricSmokeEmitter Play(Transform parent, Vector3 localPosition, Settings settings)
        {
            return null;
        }

        public void Retarget(Settings settings)
        {
            ClearSystems();
            built = false;
            Build(settings);
        }

        public void StopAndDestroy(float fadeSeconds = 1.25f)
        {
            if (releaseRoutine != null)
                StopCoroutine(releaseRoutine);

            releaseRoutine = StartCoroutine(ReleaseRoutine(Mathf.Max(0.1f, fadeSeconds)));
        }

        private void Build(Settings settings)
        {
            if (built)
                return;

            built = true;

            TryBuildFromProjectPrefab(settings);
            CreateLayer("SmokeCore", settings, 0.34f, 0.4f, 0.22f, LegacyParticleBlend.AlphaBlended);
            CreateLayer("SmokeWisps", settings, 0.24f, 0.32f, 0.25f, LegacyParticleBlend.MultiplyDouble);
            CreateLayer("SmokeHaze", settings, 0.16f, 0.52f, 0.14f, LegacyParticleBlend.AdditiveSoft);

            for (int i = 0; i < spawnedSystems.Count; i++)
            {
                if (spawnedSystems[i] != null)
                    spawnedSystems[i].Play(true);
            }
        }

        private bool TryBuildFromProjectPrefab(Settings settings)
        {
            GameObject prefab = Resources.Load<GameObject>("Combat/VolumetricSmoke");
#if UNITY_EDITOR
            if (prefab == null)
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmokePrefabAssetPath);
            if (prefab == null)
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmokeFallbackPrefabAssetPath);
#endif
            if (prefab == null)
                return false;

            GameObject instance = Instantiate(prefab, transform);
            instance.name = "SmokePrefabFX";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * Mathf.Clamp(settings.radius * 0.85f, 0.35f, 0.5f);

            Color prefabTint = settings.tint;
            prefabTint.r = Mathf.Lerp(prefabTint.r, 1f, 0.35f);
            prefabTint.g = Mathf.Lerp(prefabTint.g, 1f, 0.35f);
            prefabTint.b = Mathf.Lerp(prefabTint.b, 1f, 0.35f);
            prefabTint.a *= 0.42f;

            ParticleSystem[] systems = instance.GetComponentsInChildren<ParticleSystem>(true);
            if (systems.Length == 0)
            {
                Destroy(instance);
                return false;
            }

            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (ps == null)
                    continue;

                ParticleSystem.MainModule main = ps.main;
                main.loop = settings.loop;
                main.startColor = prefabTint;
                main.startSizeMultiplier *= settings.startSizeMax * 0.38f;
                main.startSpeedMultiplier = 0.35f;

                ParticleSystem.EmissionModule emission = ps.emission;
                emission.rateOverTimeMultiplier = settings.emissionRate * 0.42f;

                ConfigurePrefabContainment(ps, settings);

                ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                    renderer.material = CreateSmokeMaterial(prefabTint, LegacyParticleBlend.AlphaBlended);

                spawnedSystems.Add(ps);
            }

            return spawnedSystems.Count > 0;
        }

        private static void ConfigurePrefabContainment(ParticleSystem ps, Settings settings)
        {
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = CharacterSmokeMaxRadius;
            shape.radiusThickness = 1f;
            shape.scale = settings.shapeScale;

            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.y = new ParticleSystem.MinMaxCurve(
                settings.upwardSpeed * 0.25f,
                settings.upwardSpeed * 0.55f);
            velocity.radial = new ParticleSystem.MinMaxCurve(0f, settings.outwardSpeed * 0.35f);

            ParticleSystem.LimitVelocityOverLifetimeModule limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = CharacterSmokeMaxRadius * 0.45f;
            limit.dampen = 0.35f;
        }

        private void CreateLayer(
            string layerName,
            Settings settings,
            float emissionMultiplier,
            float sizeMultiplier,
            float alphaMultiplier,
            LegacyParticleBlend blend)
        {
            GameObject layerObject = new GameObject(layerName);
            layerObject.transform.SetParent(transform, false);

            ParticleSystem ps = layerObject.AddComponent<ParticleSystem>();
            Color layerTint = settings.tint;
            layerTint.a *= alphaMultiplier;

            ParticleSystemRenderer renderer = layerObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateSmokeMaterial(layerTint, blend);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingFudge = layerName.Contains("Haze") ? 0.5f : 0f;
            renderer.maxParticleSize = 3f;

            ConfigureSystem(ps, settings, emissionMultiplier, sizeMultiplier, layerTint);
            spawnedSystems.Add(ps);
        }

        private static void ConfigureSystem(
            ParticleSystem ps,
            Settings settings,
            float emissionMultiplier,
            float sizeMultiplier,
            Color layerTint)
        {
            ParticleSystem.MainModule main = ps.main;
            main.loop = settings.loop;
            main.playOnAwake = false;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(settings.lifetimeMin, settings.lifetimeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, settings.outwardSpeed + 0.08f);
            main.startSize = new ParticleSystem.MinMaxCurve(
                settings.startSizeMin * sizeMultiplier,
                settings.startSizeMax * sizeMultiplier);
            main.startColor = layerTint;
            main.gravityModifier = -0.008f;
            main.maxParticles = 800;
            main.duration = 8f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = settings.emissionRate * emissionMultiplier;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = settings.radius;
            shape.radiusThickness = settings.radiusThickness;
            shape.scale = settings.shapeScale;
            shape.randomDirectionAmount = 0.12f;
            shape.sphericalDirectionAmount = 0.22f;

            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.y = new ParticleSystem.MinMaxCurve(
                settings.upwardSpeed * 0.4f,
                settings.upwardSpeed);
            velocity.radial = new ParticleSystem.MinMaxCurve(
                settings.outwardSpeed * 0.05f,
                settings.outwardSpeed * 0.35f);

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve growCurve = new AnimationCurve(
                new Keyframe(0f, 0.65f),
                new Keyframe(0.35f, 1f),
                new Keyframe(1f, 1.55f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, growCurve);

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            Color start = layerTint;
            Color mid = new Color(start.r, start.g, start.b, start.a * 0.72f);
            Color end = new Color(start.r, start.g, start.b, 0f);
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(start, 0f),
                    new GradientColorKey(mid, 0.5f),
                    new GradientColorKey(end, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(start.a, 0f),
                    new GradientAlphaKey(mid.a, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.14f;
            noise.frequency = 0.28f;
            noise.scrollSpeed = 0.1f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Medium;
        }

        private IEnumerator ReleaseRoutine(float fadeSeconds)
        {
            for (int i = 0; i < spawnedSystems.Count; i++)
            {
                if (spawnedSystems[i] == null)
                    continue;

                ParticleSystem.EmissionModule emission = spawnedSystems[i].emission;
                emission.rateOverTime = 0f;
            }

            yield return new WaitForSeconds(fadeSeconds);
            Destroy(gameObject);
        }

        private void ClearSystems()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }

            spawnedSystems.Clear();
        }

        private static Texture2D sharedParticleTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSharedAssets()
        {
            sharedParticleTexture = null;
        }

        private static Material CreateSmokeMaterial(Color tint, LegacyParticleBlend blend = LegacyParticleBlend.AlphaBlended)
        {
            Material templateMaterial = CreateFromLegacyTemplate(tint);
            if (templateMaterial != null)
            {
                if (blend == LegacyParticleBlend.AlphaBlended)
                    return templateMaterial;

                Shader blendShader = ResolveLegacyParticleShader(blend);
                if (blendShader != null)
                {
                    Material remapped = new Material(templateMaterial);
                    remapped.shader = blendShader;
                    ApplyLegacyTint(remapped, tint);
                    return remapped;
                }

                return templateMaterial;
            }

            Shader shader = ResolveLegacyParticleShader(blend);
            if (shader == null)
                shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

            Material material = new Material(shader);
            material.name = "VolumetricSmokeParticle_Legacy";
            ApplyLegacyTint(material, tint);
            material.SetTexture("_MainTex", GetSoftParticleTexture());
            return material;
        }

        private static Material CreateFromLegacyTemplate(Color tint)
        {
            Material sourceMaterial = Resources.Load<Material>("Combat/SmokeParticle");
#if UNITY_EDITOR
            if (sourceMaterial == null)
                sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(SmokeMaterialAssetPath);
#endif
            if (sourceMaterial == null || sourceMaterial.shader == null)
                return null;

            Material clone = new Material(sourceMaterial);
            ApplyLegacyTint(clone, tint);

            if (clone.HasProperty("_MainTex") && clone.GetTexture("_MainTex") == null)
                clone.SetTexture("_MainTex", GetSoftParticleTexture());

            return clone;
        }

        private static Shader ResolveLegacyParticleShader(LegacyParticleBlend blend)
        {
            string shaderName = blend switch
            {
                LegacyParticleBlend.AdditiveSoft => "Legacy Shaders/Particles/Additive (Soft)",
                LegacyParticleBlend.MultiplyDouble => "Legacy Shaders/Particles/Multiply (Double)",
                _ => "Legacy Shaders/Particles/Alpha Blended"
            };

            return Shader.Find(shaderName);
        }

        private static void ApplyLegacyTint(Material material, Color tint)
        {
            if (material.HasProperty("_TintColor"))
                material.SetColor("_TintColor", tint);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", tint);
        }

        private static Texture2D GetSoftParticleTexture()
        {
            if (sharedParticleTexture != null)
                return sharedParticleTexture;

            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float maxDist = center.magnitude;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    float alpha = Mathf.Exp(-dist * dist * 4.2f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.name = "VolumetricSmokeParticle";
            sharedParticleTexture = texture;
            return sharedParticleTexture;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using Project.AI;
using Project.UI;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Lifts the enemy, then dissolves visuals on death using Project/EnemyDisintegrate.
    /// Skinned meshes are baked to static meshes; smoke shells follow the same silhouette.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public class EnemyDisintegrationEffect : MonoBehaviour
    {
        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int DissolveEdgeWidthId = Shader.PropertyToID("_DissolveEdgeWidth");
        private static readonly int DissolveEdgeColorId = Shader.PropertyToID("_DissolveEdgeColor");
        private static readonly int SmokeAmountId = Shader.PropertyToID("_SmokeAmount");
        private static readonly int RiseOffsetId = Shader.PropertyToID("_RiseOffset");
        private static readonly int SmokeColorId = Shader.PropertyToID("_BaseColor");

        [Header("Lift")]
        [SerializeField] private bool enableDeathLift = true;
        [SerializeField] private float liftDuration = 2f;
        [SerializeField] private float liftHeight = 2f;

        [Header("Dissolve")]
        [SerializeField] private Material dissolveMaterialTemplate;
        [SerializeField] private float dissolveDuration = 1.4f;
        [SerializeField] private float dissolveEdgeWidth = 0.045f;
        [SerializeField] private Color dissolveEdgeColor = new Color(1f, 0.45f, 0.1f, 1f);
        [SerializeField] private bool replaceDeathAnimation = true;

        [Header("Smoke")]
        [SerializeField] private bool enableSmoke = false;
        [SerializeField] private float smokeDuration = 3.2f;
        [SerializeField] private float smokeRiseHeight = 1.1f;
        [SerializeField] private float smokeExpand = 0.18f;
        [SerializeField] private Color smokeColor = new Color(0.62f, 0.64f, 0.68f, 0.48f);

        [Header("Volumetric Smoke")]
        [SerializeField] private bool enableVolumetricSmoke = false;
        [SerializeField] private float volumetricSmokeLinger = 2.4f;

        private static Material sharedDissolveTemplate;
        private static Material sharedSmokeTemplate;

        private EnemyHealth health;
        private Animator animator;
        private EnemyAnimationController animationController;
        private VolumetricSmokeEmitter volumetricSmokeEmitter;
        private readonly List<RendererState> rendererStates = new List<RendererState>();
        private readonly List<GameObject> dissolveObjects = new List<GameObject>();
        private readonly List<GameObject> smokeObjects = new List<GameObject>();
        private readonly List<Material> runtimeMaterials = new List<Material>();
        private readonly List<Material> smokeMaterials = new List<Material>();
        private Coroutine dissolveRoutine;
        private bool isDissolving;
        private Vector3 deathPosition;

        public float TotalDeathPresentationSeconds =>
            (enableDeathLift ? liftDuration : 0f) + dissolveDuration;

        private struct RendererState
        {
            public Renderer Renderer;
            public Material[] OriginalMaterials;
            public bool WasEnabled;
        }

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            animator = GetComponentInChildren<Animator>();
            animationController = GetComponent<EnemyAnimationController>();
            CacheRendererStates();
        }

        private void OnEnable()
        {
            if (health == null)
                return;

            health.Died += OnDied;
            health.Respawned += OnRespawned;
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
                health.Respawned -= OnRespawned;
            }

            if (dissolveRoutine != null)
            {
                StopCoroutine(dissolveRoutine);
                dissolveRoutine = null;
            }

            ReleaseVolumetricSmoke();
        }

        private void OnDestroy()
        {
            ReleaseVolumetricSmoke();
        }

        private void ResolveDissolveTemplate()
        {
            if (dissolveMaterialTemplate != null)
                return;

            if (sharedDissolveTemplate != null)
            {
                dissolveMaterialTemplate = sharedDissolveTemplate;
                return;
            }

            dissolveMaterialTemplate = Resources.Load<Material>("Combat/EnemyDisintegrate");
            if (dissolveMaterialTemplate != null)
            {
                sharedDissolveTemplate = dissolveMaterialTemplate;
                return;
            }

            Shader shader = Shader.Find("Project/EnemyDisintegrate");
            if (shader == null)
                return;

            sharedDissolveTemplate = new Material(shader);
            dissolveMaterialTemplate = sharedDissolveTemplate;
        }

        private Material ResolveSmokeTemplate()
        {
            if (sharedSmokeTemplate != null)
                return sharedSmokeTemplate;

            Material resourceMaterial = Resources.Load<Material>("Combat/EnemyDissolveSmoke");
            if (resourceMaterial != null && resourceMaterial.shader != null && resourceMaterial.shader.isSupported)
            {
                sharedSmokeTemplate = resourceMaterial;
                return sharedSmokeTemplate;
            }

            Shader shader = Shader.Find("Project/EnemyDissolveSmoke");
            if (shader != null && shader.isSupported)
            {
                sharedSmokeTemplate = new Material(shader);
                return sharedSmokeTemplate;
            }

            shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                return null;

            sharedSmokeTemplate = new Material(shader);
            sharedSmokeTemplate.SetFloat("_Surface", 1f);
            sharedSmokeTemplate.SetFloat("_Blend", 0f);
            sharedSmokeTemplate.SetOverrideTag("RenderType", "Transparent");
            sharedSmokeTemplate.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return sharedSmokeTemplate;
        }

        private void CacheRendererStates()
        {
            rendererStates.Clear();

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                Material[] originals = renderer.sharedMaterials;
                if (originals == null || originals.Length == 0)
                    continue;

                rendererStates.Add(new RendererState
                {
                    Renderer = renderer,
                    OriginalMaterials = originals,
                    WasEnabled = renderer.enabled
                });
            }
        }

        private void OnDied()
        {
            ResolveDissolveTemplate();
            if (isDissolving || dissolveMaterialTemplate == null)
                return;

            deathPosition = transform.position;

            if (replaceDeathAnimation)
                DisableDeathAnimation();

            HideHealthBar();

            if (health != null && health.ShouldRespawn && !health.IsRespawnExternallyManaged)
                health.DeferRespawnUntil(TotalDeathPresentationSeconds);

            dissolveRoutine = StartCoroutine(DeathPresentationRoutine());
        }

        private void OnRespawned()
        {
            isDissolving = false;

            if (dissolveRoutine != null)
            {
                StopCoroutine(dissolveRoutine);
                dissolveRoutine = null;
            }

            CleanupDissolveObjects();
            CleanupSmokeObjects();
            DestroyRuntimeMaterials();
            RestoreRenderers();
            ReleaseVolumetricSmoke();

            if (replaceDeathAnimation)
            {
                if (animationController != null)
                    animationController.enabled = true;

                if (animator != null)
                    animator.enabled = true;
            }
        }

        private void DisableDeathAnimation()
        {
            if (animationController != null)
                animationController.enabled = false;

            if (animator != null)
                animator.enabled = false;
        }

        private void HideHealthBar()
        {
            FloatingTargetHealthBar[] bars = FindObjectsByType<FloatingTargetHealthBar>();
            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i] != null && bars[i].EnemyTarget == health)
                    Destroy(bars[i].gameObject);
            }
        }

        private IEnumerator DeathPresentationRoutine()
        {
            isDissolving = true;
            StartVolumetricSmoke();
            CleanupDissolveObjects();
            CleanupSmokeObjects();
            DestroyRuntimeMaterials();

            List<Material> animatedMaterials = new List<Material>();
            BuildDissolveMeshes(animatedMaterials);

            if (enableDeathLift && liftDuration > 0f && liftHeight > 0f)
            {
                float liftElapsed = 0f;
                while (liftElapsed < liftDuration)
                {
                    liftElapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(liftElapsed / liftDuration);
                    float eased = Mathf.SmoothStep(0f, 1f, t);
                    transform.position = deathPosition + Vector3.up * (liftHeight * eased);

                    if (enableSmoke)
                        UpdateSmokeShells(Mathf.Clamp01(liftElapsed / Mathf.Max(0.35f, liftDuration * 0.85f)), smokeRiseHeight * eased * 0.35f);

                    yield return null;
                }

                transform.position = deathPosition + Vector3.up * liftHeight;
            }

            float dissolveElapsed = 0f;
            float smokeElapsed = 0f;
            while (dissolveElapsed < dissolveDuration || (enableSmoke && smokeElapsed < smokeDuration))
            {
                dissolveElapsed += Time.deltaTime;
                smokeElapsed += Time.deltaTime;

                float dissolveAmount = Mathf.Clamp01(dissolveElapsed / dissolveDuration);
                for (int i = 0; i < animatedMaterials.Count; i++)
                    animatedMaterials[i].SetFloat(DissolveAmountId, dissolveAmount);

                if (enableSmoke)
                {
                    float smokeAmount = Mathf.Clamp01(smokeElapsed / smokeDuration);
                    float riseOffset = smokeRiseHeight * smokeAmount;
                    UpdateSmokeShells(smokeAmount, riseOffset);
                }

                yield return null;
            }

            for (int i = 0; i < animatedMaterials.Count; i++)
                animatedMaterials[i].SetFloat(DissolveAmountId, 1f);

            for (int i = 0; i < rendererStates.Count; i++)
            {
                if (rendererStates[i].Renderer != null)
                    rendererStates[i].Renderer.enabled = false;
            }

            for (int i = 0; i < dissolveObjects.Count; i++)
            {
                if (dissolveObjects[i] != null)
                    dissolveObjects[i].SetActive(false);
            }

            for (int i = 0; i < smokeObjects.Count; i++)
            {
                if (smokeObjects[i] != null)
                    smokeObjects[i].SetActive(false);
            }

            NotifyDeathPresentationComplete();
            ReleaseVolumetricSmoke();
            dissolveRoutine = null;
        }

        private void StartVolumetricSmoke()
        {
            if (!enableVolumetricSmoke)
                return;

            ReleaseVolumetricSmoke();
            volumetricSmokeEmitter = VolumetricSmokeEmitter.Play(
                transform,
                ResolveSmokeLocalOffset(),
                VolumetricSmokeEmitter.ForCharacterBounds(
                    ResolveCharacterWorldBounds(),
                    VolumetricSmokeEmitter.EnemyDissolve));
        }

        private Bounds ResolveCharacterWorldBounds()
        {
            if (rendererStates.Count == 0)
                return new Bounds(transform.position + Vector3.up, Vector3.one);

            Bounds bounds = default;
            bool hasBounds = false;
            for (int i = 0; i < rendererStates.Count; i++)
            {
                Renderer renderer = rendererStates[i].Renderer;
                if (renderer == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds ? bounds : new Bounds(transform.position + Vector3.up, Vector3.one);
        }

        private void ReleaseVolumetricSmoke()
        {
            if (volumetricSmokeEmitter == null)
                return;

            volumetricSmokeEmitter.transform.SetParent(null, true);
            volumetricSmokeEmitter.StopAndDestroy(volumetricSmokeLinger);
            volumetricSmokeEmitter = null;
        }

        private Vector3 ResolveSmokeLocalOffset()
        {
            if (rendererStates.Count == 0)
                return Vector3.up;

            Bounds bounds = default;
            bool hasBounds = false;
            for (int i = 0; i < rendererStates.Count; i++)
            {
                Renderer renderer = rendererStates[i].Renderer;
                if (renderer == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
                return Vector3.up;

            return transform.InverseTransformPoint(bounds.center);
        }

        private void UpdateSmokeShells(float smokeAmount, float riseOffset)
        {
            for (int i = 0; i < smokeMaterials.Count; i++)
            {
                Material material = smokeMaterials[i];
                if (material == null || material.shader == null)
                    continue;

                if (material.shader.name.Contains("EnemyDissolveSmoke"))
                {
                    material.SetFloat(SmokeAmountId, smokeAmount);
                    material.SetFloat(RiseOffsetId, riseOffset);
                    continue;
                }

                if (material.HasProperty(BaseColorId))
                {
                    Color tint = smokeColor;
                    tint.a = smokeColor.a * (1f - smokeAmount);
                    material.SetColor(BaseColorId, tint);
                }
            }
        }

        private void NotifyDeathPresentationComplete()
        {
            EnemyLootable lootable = GetComponent<EnemyLootable>();
            if (lootable != null && lootable.IsLootPending)
                lootable.TrySpawnLootBag(deathPosition);
        }

        private void BuildDissolveMeshes(List<Material> animatedMaterials)
        {
            Material smokeTemplate = enableSmoke ? ResolveSmokeTemplate() : null;

            for (int i = 0; i < rendererStates.Count; i++)
            {
                RendererState state = rendererStates[i];
                if (state.Renderer == null)
                    continue;

                if (state.Renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                {
                    BakeSkinnedRenderer(skinnedMeshRenderer, animatedMaterials, smokeTemplate);
                }
                else if (state.Renderer is MeshRenderer meshRenderer)
                {
                    ApplyDissolveMaterials(meshRenderer, state.OriginalMaterials, animatedMaterials);
                    if (smokeTemplate != null)
                        CreateSmokeFromMesh(meshRenderer, smokeTemplate);
                }
                else
                {
                    ApplyDissolveMaterials(state.Renderer, state.OriginalMaterials, animatedMaterials);
                }
            }
        }

        private void BakeSkinnedRenderer(
            SkinnedMeshRenderer skinnedMeshRenderer,
            List<Material> animatedMaterials,
            Material smokeTemplate)
        {
            Mesh bakedMesh = new Mesh();
            bakedMesh.name = skinnedMeshRenderer.gameObject.name + "_DissolveBake";
            skinnedMeshRenderer.BakeMesh(bakedMesh);

            GameObject dissolveObject = CreateMeshObject(
                skinnedMeshRenderer.gameObject.name + "_Dissolve",
                skinnedMeshRenderer.transform,
                bakedMesh,
                skinnedMeshRenderer.shadowCastingMode,
                skinnedMeshRenderer.receiveShadows);

            MeshRenderer meshRenderer = dissolveObject.GetComponent<MeshRenderer>();
            ApplyDissolveMaterials(meshRenderer, skinnedMeshRenderer.sharedMaterials, animatedMaterials);
            skinnedMeshRenderer.enabled = false;
            dissolveObjects.Add(dissolveObject);

            if (smokeTemplate != null)
                CreateSmokeFromBakedMesh(skinnedMeshRenderer.transform, bakedMesh, smokeTemplate, dissolveObject.transform);
        }

        private void CreateSmokeFromMesh(MeshRenderer sourceRenderer, Material smokeTemplate)
        {
            MeshFilter sourceFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
                return;

            Mesh bakedMesh = Instantiate(sourceFilter.sharedMesh);
            bakedMesh.name = sourceRenderer.gameObject.name + "_SmokeBake";
            CreateSmokeFromBakedMesh(sourceRenderer.transform, bakedMesh, smokeTemplate, sourceRenderer.transform);
        }

        private void CreateSmokeFromBakedMesh(
            Transform parentTransform,
            Mesh mesh,
            Material smokeTemplate,
            Transform alignTransform)
        {
            Mesh smokeMesh = Instantiate(mesh);
            smokeMesh.name = mesh.name + "_Smoke";

            GameObject smokeObject = CreateMeshObject(
                parentTransform.gameObject.name + "_Smoke",
                parentTransform,
                smokeMesh,
                UnityEngine.Rendering.ShadowCastingMode.Off,
                false);

            smokeObject.transform.localPosition = alignTransform.localPosition;
            smokeObject.transform.localRotation = alignTransform.localRotation;
            smokeObject.transform.localScale = alignTransform.localScale * 1.14f;

            MeshRenderer smokeRenderer = smokeObject.GetComponent<MeshRenderer>();
            Material smokeMaterial = CreateSmokeMaterial(smokeTemplate);
            smokeRenderer.sharedMaterial = smokeMaterial;
            smokeObjects.Add(smokeObject);
        }

        private GameObject CreateMeshObject(
            string objectName,
            Transform parent,
            Mesh mesh,
            UnityEngine.Rendering.ShadowCastingMode shadowMode,
            bool receiveShadows)
        {
            GameObject meshObject = new GameObject(objectName);
            meshObject.transform.SetParent(parent, false);
            meshObject.transform.localPosition = Vector3.zero;
            meshObject.transform.localRotation = Quaternion.identity;
            meshObject.transform.localScale = Vector3.one;

            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = shadowMode;
            meshRenderer.receiveShadows = receiveShadows;
            return meshObject;
        }

        private void ApplyDissolveMaterials(Renderer renderer, Material[] sourceMaterials, List<Material> animatedMaterials)
        {
            Material[] dissolveMaterials = new Material[sourceMaterials.Length];
            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                Material dissolveMaterial = CreateDissolveMaterial(sourceMaterials[i]);
                dissolveMaterials[i] = dissolveMaterial;
                animatedMaterials.Add(dissolveMaterial);
            }

            renderer.sharedMaterials = dissolveMaterials;
            renderer.enabled = true;
        }

        private Material CreateDissolveMaterial(Material source)
        {
            Material material = new Material(dissolveMaterialTemplate);
            material.name = source != null ? source.name + "_Dissolve" : "EnemyDissolve";
            runtimeMaterials.Add(material);

            if (source != null)
            {
                if (source.HasProperty(BaseMapId))
                    material.SetTexture(BaseMapId, source.GetTexture(BaseMapId));
                else if (source.HasProperty("_MainTex"))
                    material.SetTexture(BaseMapId, source.GetTexture("_MainTex"));

                if (source.HasProperty(BaseColorId))
                    material.SetColor(BaseColorId, source.GetColor(BaseColorId));
                else if (source.HasProperty("_Color"))
                    material.SetColor(BaseColorId, source.GetColor("_Color"));
            }

            material.SetFloat(DissolveEdgeWidthId, dissolveEdgeWidth);
            material.SetColor(DissolveEdgeColorId, dissolveEdgeColor);
            material.SetFloat(DissolveAmountId, 0f);
            return material;
        }

        private Material CreateSmokeMaterial(Material smokeTemplate)
        {
            Material material = new Material(smokeTemplate);
            material.name = "EnemyDissolveSmoke_Runtime";

            if (material.shader != null && material.shader.name.Contains("EnemyDissolveSmoke"))
            {
                material.SetColor(SmokeColorId, smokeColor);
                material.SetFloat(SmokeAmountId, 0f);
                material.SetFloat(RiseOffsetId, 0f);
                material.SetFloat("_Expand", smokeExpand);
            }
            else if (material.HasProperty(BaseColorId))
            {
                Color tint = smokeColor;
                tint.a *= 0.55f;
                material.SetColor(BaseColorId, tint);
            }

            smokeMaterials.Add(material);
            runtimeMaterials.Add(material);
            return material;
        }

        private void CleanupDissolveObjects()
        {
            for (int i = 0; i < dissolveObjects.Count; i++)
            {
                if (dissolveObjects[i] == null)
                    continue;

                MeshFilter meshFilter = dissolveObjects[i].GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                    Destroy(meshFilter.sharedMesh);

                Destroy(dissolveObjects[i]);
            }

            dissolveObjects.Clear();
        }

        private void CleanupSmokeObjects()
        {
            for (int i = 0; i < smokeObjects.Count; i++)
            {
                if (smokeObjects[i] == null)
                    continue;

                MeshFilter meshFilter = smokeObjects[i].GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                    Destroy(meshFilter.sharedMesh);

                Destroy(smokeObjects[i]);
            }

            smokeObjects.Clear();
            smokeMaterials.Clear();
        }

        private void DestroyRuntimeMaterials()
        {
            for (int i = 0; i < runtimeMaterials.Count; i++)
            {
                if (runtimeMaterials[i] != null)
                    Destroy(runtimeMaterials[i]);
            }

            runtimeMaterials.Clear();
        }

        private void RestoreRenderers()
        {
            for (int i = 0; i < rendererStates.Count; i++)
            {
                RendererState state = rendererStates[i];
                if (state.Renderer == null)
                    continue;

                state.Renderer.sharedMaterials = state.OriginalMaterials;
                state.Renderer.enabled = state.WasEnabled;
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Project.AI;
using Project.AI.Invector;
using Project.UI;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Dissolves enemy visuals on death using Project/EnemyDisintegrate.
    /// Skinned meshes are baked to static meshes; smoke shells follow the same silhouette.
    /// Optional lift can be enabled per-prefab; default is dissolve-in-place after ragdoll linger.
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
        private static readonly int DissolveSpreadId = Shader.PropertyToID("_DissolveSpread");
        private static readonly int SmokeAmountId = Shader.PropertyToID("_SmokeAmount");
        private static readonly int RiseOffsetId = Shader.PropertyToID("_RiseOffset");
        private static readonly int SmokeColorId = Shader.PropertyToID("_BaseColor");

        [Header("Lift (optional)")]
        [SerializeField] private bool enableDeathLift = false;
        [SerializeField] private float liftDuration = 2f;
        [SerializeField] private float liftHeight = 2f;

        [Header("Dissolve")]
        [SerializeField] private Material dissolveMaterialTemplate;
        [SerializeField] private float dissolveDuration = 1.4f;
        [SerializeField] private float dissolveEdgeWidth = 0.045f;
        [SerializeField] private Color dissolveEdgeColor = new Color(1f, 0.45f, 0.1f, 1f);
        [Tooltip("Max world-space vertex push diameter for dissolve debris (~1m for Skitter-sized creatures).")]
        [SerializeField] private float maxDissolveDiameter = 1f;
        [SerializeField] private bool replaceDeathAnimation = true;
        [Tooltip("When no EnemyDeathSequence is present, start lift/dissolve immediately on death.")]
        [SerializeField] private bool autoStartOnDeathWithoutSequence = true;

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
        private Transform liftAnchor;
        private bool hasCorpseLiftOrigin;
        private float runtimeDissolveSpread = 0.35f;

        public float TotalDeathPresentationSeconds =>
            (enableDeathLift ? liftDuration : 0f) + dissolveDuration;

        /// <summary>
        /// World position the corpse lift/dissolve should start from (usually torso center after ragdoll).
        /// </summary>
        public void SetCorpseLiftOrigin(Vector3 worldPosition)
        {
            deathPosition = worldPosition;
            hasCorpseLiftOrigin = true;
        }

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

            health.Died += OnDiedFallback;
            health.Respawned += OnRespawned;
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDiedFallback;
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
            CollectDissolveRenderers(GetComponentsInChildren<Renderer>(true));

            if (!ContainsSkinnedRenderer())
            {
                Animator characterAnimator = animator != null ? animator : GetComponentInChildren<Animator>(true);
                if (characterAnimator != null)
                    CollectDissolveRenderers(characterAnimator.GetComponentsInChildren<Renderer>(true));
            }
        }

        private void CollectDissolveRenderers(Renderer[] renderers)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !ShouldDissolveRenderer(renderer))
                    continue;

                Material[] originals = renderer.sharedMaterials;
                if (originals == null || originals.Length == 0)
                    continue;

                bool alreadyCached = false;
                for (int s = 0; s < rendererStates.Count; s++)
                {
                    if (rendererStates[s].Renderer == renderer)
                    {
                        alreadyCached = true;
                        break;
                    }
                }

                if (alreadyCached)
                    continue;

                rendererStates.Add(new RendererState
                {
                    Renderer = renderer,
                    OriginalMaterials = originals,
                    WasEnabled = renderer.enabled
                });
            }
        }

        private bool ContainsSkinnedRenderer()
        {
            for (int i = 0; i < rendererStates.Count; i++)
            {
                if (rendererStates[i].Renderer is SkinnedMeshRenderer)
                    return true;
            }

            return false;
        }

        private static bool ShouldDissolveRenderer(Renderer renderer)
        {
            if (renderer == null || !(renderer is SkinnedMeshRenderer))
                return false;

            // Inactive VBOT LODs keep cm-space AABBs (~100 units). Baking those under a world
            // anchor without the 0.01 armature scale explodes the dissolve to ~100m.
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                return false;

            Transform node = renderer.transform;
            while (node != null)
            {
                string nodeName = node.name;
                if (nodeName.StartsWith("Drawn_", StringComparison.Ordinal) ||
                    nodeName.StartsWith("Holstered_", StringComparison.Ordinal))
                    return false;

                if (nodeName.StartsWith("vHandgun", StringComparison.OrdinalIgnoreCase) ||
                    nodeName.StartsWith("vShotgun", StringComparison.OrdinalIgnoreCase) ||
                    nodeName.StartsWith("vBow", StringComparison.OrdinalIgnoreCase) ||
                    nodeName.StartsWith("vRifle", StringComparison.OrdinalIgnoreCase) ||
                    nodeName.StartsWith("vMelee", StringComparison.OrdinalIgnoreCase))
                    return false;

                if (node.CompareTag("Weapon") || node.CompareTag("Ignore Ragdoll"))
                    return false;

                node = node.parent;
            }

            return true;
        }

        private void OnDiedFallback()
        {
            if (GetComponent<EnemyDeathSequence>() != null)
                return;

            if (!autoStartOnDeathWithoutSequence)
                return;

            BeginPresentation();
        }

        /// <summary>
        /// Starts lift + dissolve after death animation/ragdoll. Called by <see cref="EnemyDeathSequence"/>.
        /// </summary>
        public void BeginPresentation(Action onComplete = null)
        {
            ResolveDissolveTemplate();
            if (isDissolving || dissolveMaterialTemplate == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (!hasCorpseLiftOrigin)
                deathPosition = ResolveCorpseLiftOrigin();
            hasCorpseLiftOrigin = false;
            deathPosition = Project.AI.EnemyGroundUtility.SnapPositionToGround(deathPosition);

            HideHealthBar();
            CacheRendererStates();

            EnemyInvectorMotorBridge motorBridge = GetComponent<EnemyInvectorMotorBridge>();
            if (motorBridge != null)
                motorBridge.enabled = false;

            if (health != null && health.ShouldRespawn && !health.IsRespawnExternallyManaged)
                health.DeferRespawnUntil(TotalDeathPresentationSeconds);

            dissolveRoutine = StartCoroutine(DeathPresentationRoutine(onComplete));
        }

        private void OnRespawned()
        {
            isDissolving = false;
            hasCorpseLiftOrigin = false;

            if (dissolveRoutine != null)
            {
                StopCoroutine(dissolveRoutine);
                dissolveRoutine = null;
            }

            CleanupDissolveObjects();
            CleanupSmokeObjects();
            CleanupLiftAnchor();
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

            EnemyInvectorMotorBridge motorBridge = GetComponent<EnemyInvectorMotorBridge>();
            if (motorBridge != null)
                motorBridge.enabled = true;
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
            EngagedEnemyHealthHud.Instance?.ClearIf(health);

            FloatingTargetHealthBar[] bars = FindObjectsByType<FloatingTargetHealthBar>();
            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i] != null && bars[i].EnemyTarget == health)
                    Destroy(bars[i].gameObject);
            }
        }

        private IEnumerator DeathPresentationRoutine(Action onComplete = null)
        {
            isDissolving = true;
            StartVolumetricSmoke();
            CleanupDissolveObjects();
            CleanupSmokeObjects();
            DestroyRuntimeMaterials();

            List<Material> animatedMaterials = new List<Material>();
            EnsureLiftAnchor();
            runtimeDissolveSpread = ResolveDissolveSpread();
            BuildDissolveMeshes(animatedMaterials);
            IncludeDroppedWeaponDissolve(animatedMaterials);

            if (replaceDeathAnimation)
                DisableDeathAnimation();

            if (dissolveObjects.Count == 0)
            {
                for (int i = 0; i < rendererStates.Count; i++)
                {
                    if (rendererStates[i].Renderer != null)
                        rendererStates[i].Renderer.enabled = rendererStates[i].WasEnabled;
                }
            }

            if (enableDeathLift && liftDuration > 0f && liftHeight > 0f)
            {
                float liftElapsed = 0f;
                while (liftElapsed < liftDuration)
                {
                    liftElapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(liftElapsed / liftDuration);
                    float eased = Mathf.SmoothStep(0f, 1f, t);
                    Vector3 liftedPosition = deathPosition + Vector3.up * (liftHeight * eased);
                    if (liftAnchor != null)
                        liftAnchor.position = liftedPosition;

                    if (enableSmoke)
                        UpdateSmokeShells(Mathf.Clamp01(liftElapsed / Mathf.Max(0.35f, liftDuration * 0.85f)), smokeRiseHeight * eased * 0.35f);

                    yield return null;
                }

                if (liftAnchor != null)
                    liftAnchor.position = deathPosition + Vector3.up * liftHeight;
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
            CleanupDroppedWeapon();
            ReleaseVolumetricSmoke();
            dissolveRoutine = null;
            onComplete?.Invoke();
        }

        private void IncludeDroppedWeaponDissolve(List<Material> animatedMaterials)
        {
            EnemyInvectorLoadoutBridge loadout = GetComponent<EnemyInvectorLoadoutBridge>();
            if (loadout == null || loadout.LastDroppedWeapon == null)
                return;

            loadout.FreezeDroppedWeaponForDissolve();
            GameObject weapon = loadout.LastDroppedWeapon;
            Material smokeTemplate = enableSmoke ? ResolveSmokeTemplate() : null;

            MeshRenderer[] renderers = weapon.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer meshRenderer = renderers[i];
                if (meshRenderer == null)
                    continue;

                ApplyDissolveMaterials(meshRenderer, meshRenderer.sharedMaterials, animatedMaterials);
                dissolveObjects.Add(meshRenderer.gameObject);

                if (smokeTemplate != null)
                    CreateSmokeFromMesh(meshRenderer, smokeTemplate);
            }
        }

        private void CleanupDroppedWeapon()
        {
            EnemyInvectorLoadoutBridge loadout = GetComponent<EnemyInvectorLoadoutBridge>();
            loadout?.DestroyDroppedWeapon();
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
            // Bake in SMR local space, then fit world size to the live mesh bounds. Meshy/VBOT
            // hierarchies use 0.01 / 100× compensations — BakeMesh(useScale) alone still
            // explodes when inactive cm-space LODs or wrong lossyScale paths are involved.
            skinnedMeshRenderer.BakeMesh(bakedMesh, false);

            if (bakedMesh.vertexCount <= 0)
            {
                Destroy(bakedMesh);
                return;
            }

            Transform parent = liftAnchor != null ? liftAnchor : transform;
            GameObject dissolveObject = CreateMeshObject(
                skinnedMeshRenderer.gameObject.name + "_Dissolve",
                parent,
                bakedMesh,
                skinnedMeshRenderer.shadowCastingMode,
                skinnedMeshRenderer.receiveShadows);

            // Fit from baked AABB → live SMR world bounds. Do NOT apply lossyScale here:
            // Meshy Skitter BakeMesh(false) is already ~0.8m world-sized while lossyScale is 100×;
            // multiplying both explodes the dissolve to tens of meters.
            FitDissolveObjectToCharacterBounds(dissolveObject, bakedMesh, skinnedMeshRenderer.bounds);

            MeshRenderer meshRenderer = dissolveObject.GetComponent<MeshRenderer>();
            ApplyDissolveMaterials(meshRenderer, skinnedMeshRenderer.sharedMaterials, animatedMaterials);
            skinnedMeshRenderer.enabled = false;
            dissolveObjects.Add(dissolveObject);

            if (smokeTemplate != null)
                CreateSmokeFromBakedMesh(parent, bakedMesh, smokeTemplate, dissolveObject.transform);
        }

        /// <summary>
        /// Places a baked dissolve mesh so its world AABB matches the source character mesh.
        /// Parent must be unscaled (lift anchor). Caps size to the live bounds / maxDissolveDiameter.
        /// </summary>
        private void FitDissolveObjectToCharacterBounds(
            GameObject dissolveObject,
            Mesh bakedMesh,
            Bounds sourceWorldBounds)
        {
            if (dissolveObject == null || bakedMesh == null)
                return;

            Bounds localBounds = bakedMesh.bounds;
            Vector3 localSize = localBounds.size;
            if (localSize.x < 1e-5f || localSize.y < 1e-5f || localSize.z < 1e-5f)
            {
                dissolveObject.transform.position = sourceWorldBounds.center;
                dissolveObject.transform.rotation = Quaternion.identity;
                dissolveObject.transform.localScale = Vector3.one;
                return;
            }

            Bounds target = sourceWorldBounds;
            float targetMax = MaxExtent(target.size);
            float cap = Mathf.Max(0.35f, maxDissolveDiameter * 2.5f);
            if (targetMax > cap && targetMax > 1e-4f)
            {
                float shrink = cap / targetMax;
                target = new Bounds(target.center, target.size * shrink);
                targetMax = cap;
            }

            float ratioX = target.size.x / localSize.x;
            float ratioY = target.size.y / localSize.y;
            float ratioZ = target.size.z / localSize.z;
            float uniform = Mathf.Clamp((ratioX + ratioY + ratioZ) / 3f, 1e-4f, 8f);

            // Hard stop: never let baked local cm-space (~100u) render as ~100m.
            float projectedMax = MaxExtent(localSize) * uniform;
            if (projectedMax > targetMax * 1.15f && projectedMax > 1e-4f)
                uniform *= targetMax / projectedMax;

            Transform dissolveTransform = dissolveObject.transform;
            dissolveTransform.rotation = Quaternion.identity;
            dissolveTransform.localScale = Vector3.one * uniform;
            dissolveTransform.position = target.center - dissolveTransform.TransformVector(localBounds.center);
        }

        private static float MaxExtent(Vector3 size)
        {
            return Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        }

        private void EnsureLiftAnchor()
        {
            if (liftAnchor == null)
            {
                GameObject anchorObject = new GameObject("EnemyDissolveLiftAnchor");
                liftAnchor = anchorObject.transform;
            }

            liftAnchor.SetParent(null, true);
            liftAnchor.position = deathPosition;
        }

        private void CleanupLiftAnchor()
        {
            if (liftAnchor == null)
                return;

            Destroy(liftAnchor.gameObject);
            liftAnchor = null;
        }

        private Vector3 ResolveCorpseLiftOrigin()
        {
            SkinnedMeshRenderer[] skinnedMeshes = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Bounds bounds = default;
            bool hasBounds = false;

            for (int i = 0; i < skinnedMeshes.Length; i++)
            {
                SkinnedMeshRenderer mesh = skinnedMeshes[i];
                if (mesh == null || !ShouldDissolveRenderer(mesh))
                    continue;

                if (!hasBounds)
                {
                    bounds = mesh.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(mesh.bounds);
                }
            }

            if (hasBounds)
                return Project.AI.EnemyGroundUtility.SnapPositionToGround(bounds.center);

            Animator corpseAnimator = animator != null ? animator : GetComponentInChildren<Animator>();
            if (corpseAnimator != null)
            {
                Transform hips = corpseAnimator.GetBoneTransform(HumanBodyBones.Hips);
                if (hips != null)
                    return Project.AI.EnemyGroundUtility.SnapPositionToGround(hips.position);
            }

            return Project.AI.EnemyGroundUtility.SnapPositionToGround(transform.position + Vector3.up);
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

            // Always parent under the unscaled lift anchor when available so 100× weapon nodes
            // cannot multiply smoke scale again.
            Transform smokeParent = liftAnchor != null ? liftAnchor : parentTransform;
            GameObject smokeObject = CreateMeshObject(
                parentTransform != null ? parentTransform.gameObject.name + "_Smoke" : "DissolveSmoke",
                smokeParent,
                smokeMesh,
                UnityEngine.Rendering.ShadowCastingMode.Off,
                false);

            smokeObject.transform.SetPositionAndRotation(alignTransform.position, alignTransform.rotation);
            Vector3 worldScale = alignTransform.lossyScale;
            float maxAxis = Mathf.Max(Mathf.Abs(worldScale.x), Mathf.Max(Mathf.Abs(worldScale.y), Mathf.Abs(worldScale.z)));
            if (maxAxis > 3f)
                worldScale = Vector3.one;

            if (smokeParent != null)
            {
                Vector3 parentLossy = smokeParent.lossyScale;
                smokeObject.transform.localScale = new Vector3(
                    worldScale.x / Mathf.Max(1e-4f, Mathf.Abs(parentLossy.x)),
                    worldScale.y / Mathf.Max(1e-4f, Mathf.Abs(parentLossy.y)),
                    worldScale.z / Mathf.Max(1e-4f, Mathf.Abs(parentLossy.z))) * 1.06f;
            }
            else
            {
                smokeObject.transform.localScale = worldScale * 1.06f;
            }

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
            if (material.HasProperty(DissolveSpreadId))
                material.SetFloat(DissolveSpreadId, runtimeDissolveSpread);
            return material;
        }

        /// <summary>
        /// Vertex spread is object-space after the dissolve mesh is fitted to character bounds.
        /// Cap by live mesh size only — do not inflate from oversized root capsules.
        /// </summary>
        private float ResolveDissolveSpread()
        {
            Bounds bounds = ResolveCharacterWorldBounds();
            float maxExtent = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
            maxExtent = Mathf.Min(maxExtent, Mathf.Max(0.2f, maxDissolveDiameter * 0.55f));

            float diameter = Mathf.Max(0.2f, maxExtent * 2f);
            float cappedDiameter = Mathf.Min(diameter, Mathf.Max(0.25f, maxDissolveDiameter));

            float templateSpread = 0.35f;
            if (dissolveMaterialTemplate != null && dissolveMaterialTemplate.HasProperty(DissolveSpreadId))
                templateSpread = dissolveMaterialTemplate.GetFloat(DissolveSpreadId);

            float sizedSpread = cappedDiameter * 0.22f;
            return Mathf.Clamp(Mathf.Min(templateSpread, sizedSpread), 0.04f, 0.28f);
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

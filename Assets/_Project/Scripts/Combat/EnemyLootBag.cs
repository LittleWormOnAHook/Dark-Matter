using System.Collections;
using System.Collections.Generic;
using Project.AI;
using Project.Core;
using Project.Data;
using Project.Events;
using Project.Interaction;
using Project.Loot;
using Project.Progression;
using Project.Quests;
using Project.UI;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// World loot bag / drop box dropped after an enemy disintegrates. Dissolves after 20s unlooted or 2s after looting.
    /// The drop is Assets/_Project/Prefabs/Combat/EnemyLootBag.prefab (enemy / EnemyDefinition prefab first, then
    /// DM_LootChestProfile.enemyLootBagPrefab). The old Resources sphere bag and the procedural sphere visual are retired.
    /// Edit the prefab mesh/texture in the Inspector — those visuals are instanced on drop.
    /// A child whose name contains "Lid" pops open (unscaled, <see cref="DMChestLid"/>) while the loot window shows the bag.
    /// Items are looted per entry in the UITK loot window through the shared <see cref="DMLootGrant"/>
    /// path. The bag's AC is granted when the bag is opened (roster AC card only, no loot row, D6).
    /// Phase 7: bags are pooled per prefab (authored materials / colliders / mesh restored on release), the unlooted
    /// expiry is one coroutine instead of a per-frame Update, and <see cref="IsPetAutoLootAllowed"/> is the D16 filter.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class EnemyLootBag : MonoBehaviour, IWorldUsable, IDMLootContainer
    {
        private const int MaxPooledPerPrefab = 8;
        private static readonly Dictionary<GameObject, Stack<EnemyLootBag>> s_pool = new Dictionary<GameObject, Stack<EnemyLootBag>>();
        private static UIManager s_uiManager;

        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int DissolveEdgeWidthId = Shader.PropertyToID("_DissolveEdgeWidth");
        private static readonly int DissolveEdgeColorId = Shader.PropertyToID("_DissolveEdgeColor");

        [SerializeField] private float interactRange = 2.75f;
        [SerializeField] private string promptText = "Press E to loot bag";
        [SerializeField] private float unlootedLifetime = 20f;
        [SerializeField] private float lootedDissolveDelay = 2f;
        [SerializeField] private float dissolveDuration = 1.1f;
        [SerializeField] private float dissolveEdgeWidth = 0.06f;
        [SerializeField] private Color dissolveEdgeColor = new Color(0.85f, 0.55f, 0.15f, 1f);
        [SerializeField] private bool enableVolumetricSmoke = false;
        [SerializeField] private float volumetricSmokeLinger = 1.2f;

        [Header("Dropped Visual (Edit Mode)")]
        [Tooltip("MeshFilter to drive. Empty = first MeshFilter in children.")]
        [SerializeField] private MeshFilter visualMeshFilter;
        [Tooltip("Renderer to drive. Empty = first MeshRenderer in children.")]
        [SerializeField] private MeshRenderer visualRenderer;
        [Tooltip("Optional mesh override applied in the Editor and on spawned instances.")]
        [SerializeField] private Mesh dropMesh;
        [Tooltip("Optional albedo texture override applied in the Editor and on spawned instances.")]
        [SerializeField] private Texture dropTexture;
        [Header("Lid (drop box)")]
        [Tooltip("Lid to pop open while the loot window shows this bag. Empty = first child whose name contains \"Lid\".")]
        [SerializeField] private Transform lidTransform;
        [SerializeField] private bool animateLid = true;
        [Tooltip("Local rotation added to the lid's closed pose when open.")]
        [SerializeField] private Vector3 lidOpenEuler = new Vector3(-24f, 0f, 0f);
        [Tooltip("Local offset (lid parent space) added to the lid's closed pose when open.")]
        [SerializeField] private Vector3 lidOpenOffset = new Vector3(0f, 0.32f, -0.22f);
        [SerializeField, Min(0.05f)] private float lidOpenSeconds = 0.35f;
        [SerializeField, Min(0.05f)] private float lidCloseSeconds = 0.3f;
        [Header("Shaders (cached)")]
        [SerializeField] private Shader dissolveShader;
        private static Shader s_cachedDissolveShader;

        private readonly DMLootEntryList lootEntries = new DMLootEntryList();
        private int pendingAetherCredits;

        private EnemyLootable owner;
        private string displayName;
        private MeshRenderer bagRenderer;
        private Material dissolveMaterial;
        private readonly List<Material> dissolveMaterials = new List<Material>();
        private DMChestLid lid;
        private VolumetricSmokeEmitter volumetricSmokeEmitter;
        private float expireTime;
        private bool isDissolving;
        private bool initialized;
        private Coroutine dissolveRoutine;
        private Coroutine expireRoutine;

        // Pool (phase 7): the prefab this bag came from and its authored look, restored before reuse.
        private GameObject sourcePrefab;
        private bool authoredStateCaptured;
        private MeshRenderer[] authoredRenderers;
        private Material[][] authoredMaterials;
        private Collider[] authoredColliders;
        private bool[] authoredColliderEnabled;
        private Mesh authoredMesh;
        private Mesh authoredDropMesh;
        private Texture authoredDropTexture;
        private readonly List<Material> runtimeTextureMaterials = new List<Material>();

        public bool HasRemainingLoot => !lootEntries.IsEmpty || pendingAetherCredits > 0;

        // IDMLootContainer
        public string LootDisplayName => string.IsNullOrWhiteSpace(displayName) ? "Enemy" : displayName;
        public IReadOnlyList<DMLootEntry> Entries => lootEntries.Entries;
        public bool IsEmpty => lootEntries.IsEmpty;
        public bool HasProtectedEntries => lootEntries.HasProtectedEntries;
        public Vector3 LootWorldPosition => transform.position;

        public bool CanPlayerLoot(Vector3 playerPosition)
        {
            return initialized && !isDissolving && HasRemainingLoot && IsWithinRange(playerPosition);
        }

        /// <summary>
        /// Pet auto-loot filter (D16, plan 7.8): pets may only take from live enemy drops (and world pickups),
        /// never from chests, story crates or storage crates. Checks the concrete source type, not just
        /// <see cref="IDMLootContainer"/>, because chests implement the same interface.
        /// </summary>
        public static bool IsPetAutoLootAllowed(IDMLootContainer container)
        {
            EnemyLootBag bag = container as EnemyLootBag;
            return bag != null && bag.initialized && !bag.isDissolving && bag.HasRemainingLoot;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_pool.Clear();
            s_uiManager = null;
        }

        public static EnemyLootBag Spawn(
            Vector3 worldPosition,
            EnemyLootable lootOwner,
            IReadOnlyList<QuestRewardDefinition> loot,
            string lootDisplayName,
            float range,
            string interactPrompt,
            float unlootedLifetimeSeconds = 20f,
            float lootedDissolveDelaySeconds = 2f,
            GameObject lootBagPrefab = null,
            Mesh meshOverride = null,
            Texture textureOverride = null)
        {
            if (lootOwner == null || loot == null || loot.Count == 0)
                return null;

            Vector3 spawnPosition = SnapToGround(worldPosition, out bool grounded);
            GameObject prefab = lootBagPrefab != null ? lootBagPrefab : DMLootChestProfile.ResolveEnemyLootBagPrefab();
            GameObject bagObject = InstantiateBag(prefab, spawnPosition);
            if (bagObject == null)
                return null;

            EnemyLootBag bag = bagObject.GetComponent<EnemyLootBag>();
            if (bag == null)
                bag = bagObject.AddComponent<EnemyLootBag>();

            bag.sourcePrefab = prefab;
            bag.CaptureAuthoredState();
            bag.ApplyVisualOverrides(meshOverride, textureOverride);
            bag.Initialize(
                lootOwner,
                loot,
                lootDisplayName,
                range,
                interactPrompt,
                unlootedLifetimeSeconds,
                lootedDissolveDelaySeconds);
            // An empty roll goes straight back to the pool: report "no bag" so the owner finishes its loot phase.
            if (bag == null || !bag.initialized)
                return null;
            if (grounded)
                bag.SeatOnGround(spawnPosition.y);
            return bag;
        }

        private static GameObject InstantiateBag(GameObject prefab, Vector3 spawnPosition)
        {
            if (prefab == null)
            {
                Debug.LogWarning(
                    "[EnemyLootBag] Missing loot bag prefab. Assign " +
                    EnemyLootable.DefaultLootBagPrefabPath + " on EnemyLootable or DM_LootChestProfile.");
                return null;
            }

            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            EnemyLootBag pooled = TakeFromPool(prefab);
            GameObject bagObject;
            if (pooled != null)
            {
                bagObject = pooled.gameObject;
                bagObject.transform.SetPositionAndRotation(spawnPosition, rotation);
                bagObject.SetActive(true);
            }
            else
            {
                bagObject = Instantiate(prefab, spawnPosition, rotation);
            }

            bagObject.name = "EnemyLootBag";
            return bagObject;
        }

        private static EnemyLootBag TakeFromPool(GameObject prefab)
        {
            if (!s_pool.TryGetValue(prefab, out Stack<EnemyLootBag> stack))
                return null;

            while (stack.Count > 0)
            {
                EnemyLootBag bag = stack.Pop();
                // Pooled bags are scene objects: a scene change destroys them, so skip dead entries.
                if (bag != null)
                    return bag;
            }

            return null;
        }

        /// <summary>Hides the bag for reuse (or destroys it when the pool is full / the app is tearing down).</summary>
        private void ReleaseToPoolOrDestroy()
        {
            if (!Application.isPlaying || sourcePrefab == null || GameplayInputRecovery.IsTearingDown)
            {
                Destroy(gameObject);
                return;
            }

            if (!s_pool.TryGetValue(sourcePrefab, out Stack<EnemyLootBag> stack))
            {
                stack = new Stack<EnemyLootBag>();
                s_pool[sourcePrefab] = stack;
            }

            if (stack.Count >= MaxPooledPerPrefab)
                PurgeDeadEntries(stack);
            if (stack.Count >= MaxPooledPerPrefab || stack.Contains(this))
            {
                Destroy(gameObject);
                return;
            }

            ResetForPool();
            gameObject.name = "EnemyLootBag (Pooled)";
            gameObject.SetActive(false);
            stack.Push(this);
        }

        private static void PurgeDeadEntries(Stack<EnemyLootBag> stack)
        {
            EnemyLootBag[] items = stack.ToArray();
            stack.Clear();
            for (int i = items.Length - 1; i >= 0; i--)
            {
                if (items[i] != null)
                    stack.Push(items[i]);
            }
        }

        private void ResetForPool()
        {
            if (expireRoutine != null)
            {
                StopCoroutine(expireRoutine);
                expireRoutine = null;
            }

            // Called from the end of DissolveRoutine: deactivation stops it, only drop the handle.
            dissolveRoutine = null;
            DetachVolumetricSmoke();
            lid?.SnapClosed();
            RestoreAuthoredState();
            DestroyDissolveMaterials();
            lootEntries.Clear();
            pendingAetherCredits = 0;
            owner = null;
            displayName = null;
            initialized = false;
            isDissolving = false;
            expireTime = 0f;
        }

        /// <summary>Remembers the prefab's own mesh, materials and colliders the first time this bag spawns.</summary>
        private void CaptureAuthoredState()
        {
            if (authoredStateCaptured)
                return;

            EnsureVisualBindings();
            authoredMesh = visualMeshFilter != null ? visualMeshFilter.sharedMesh : null;
            authoredDropMesh = dropMesh;
            authoredDropTexture = dropTexture;

            authoredRenderers = GetComponentsInChildren<MeshRenderer>(true);
            authoredMaterials = new Material[authoredRenderers.Length][];
            for (int i = 0; i < authoredRenderers.Length; i++)
                authoredMaterials[i] = authoredRenderers[i] != null ? authoredRenderers[i].sharedMaterials : null;

            authoredColliders = GetComponentsInChildren<Collider>(true);
            authoredColliderEnabled = new bool[authoredColliders.Length];
            for (int i = 0; i < authoredColliders.Length; i++)
                authoredColliderEnabled[i] = authoredColliders[i] != null && authoredColliders[i].enabled;

            authoredStateCaptured = true;
        }

        private void RestoreAuthoredState()
        {
            if (!authoredStateCaptured)
                return;

            dropMesh = authoredDropMesh;
            dropTexture = authoredDropTexture;
            if (visualMeshFilter != null)
                visualMeshFilter.sharedMesh = authoredMesh;

            for (int i = 0; i < authoredRenderers.Length; i++)
            {
                if (authoredRenderers[i] != null && authoredMaterials[i] != null)
                    authoredRenderers[i].sharedMaterials = authoredMaterials[i];
            }

            for (int i = 0; i < authoredColliders.Length; i++)
            {
                if (authoredColliders[i] != null)
                    authoredColliders[i].enabled = authoredColliderEnabled[i];
            }

            for (int i = 0; i < runtimeTextureMaterials.Count; i++)
            {
                if (runtimeTextureMaterials[i] != null)
                    Destroy(runtimeTextureMaterials[i]);
            }
            runtimeTextureMaterials.Clear();
        }

        private bool IsAuthoredMaterial(Material material)
        {
            if (authoredMaterials == null)
                return false;
            for (int i = 0; i < authoredMaterials.Length; i++)
            {
                Material[] slots = authoredMaterials[i];
                if (slots == null)
                    continue;
                for (int s = 0; s < slots.Length; s++)
                {
                    if (slots[s] == material)
                        return true;
                }
            }
            return false;
        }

        private static Vector3 SnapToGround(Vector3 worldPosition, out bool grounded)
        {
            Vector3 rayOrigin = worldPosition + Vector3.up * 3f;
            grounded = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 12f, ~0, QueryTriggerInteraction.Ignore);
            if (grounded)
                return hit.point;

            return worldPosition + Vector3.up * 0.14f;
        }

        /// <summary>Rests the visual's lowest point on the ground (any prefab pivot: drop box or sphere bag).</summary>
        private void SeatOnGround(float groundY)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(false);
            bool any = false;
            float minY = float.MaxValue;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled || r is ParticleSystemRenderer || IsUnderSmoke(r.transform))
                    continue;
                minY = Mathf.Min(minY, r.bounds.min.y);
                any = true;
            }

            float lift = any ? Mathf.Clamp(groundY + 0.01f - minY, -1f, 1f) : 0.14f;
            transform.position += Vector3.up * lift;
        }

        private bool IsUnderSmoke(Transform t)
        {
            return volumetricSmokeEmitter != null && t != null && t.IsChildOf(volumetricSmokeEmitter.transform);
        }

        private void Initialize(
            EnemyLootable lootOwner,
            IReadOnlyList<QuestRewardDefinition> loot,
            string lootDisplayName,
            float range,
            string interactPrompt,
            float unlootedLifetimeSeconds,
            float lootedDissolveDelaySeconds)
        {
            owner = lootOwner;
            displayName = lootDisplayName;
            interactRange = range;
            promptText = string.IsNullOrWhiteSpace(interactPrompt) ? promptText : interactPrompt;
            unlootedLifetime = Mathf.Max(1f, unlootedLifetimeSeconds);
            lootedDissolveDelay = Mathf.Max(0.1f, lootedDissolveDelaySeconds);

            lootEntries.Clear();
            pendingAetherCredits = 0;
            for (int i = 0; i < loot.Count; i++)
            {
                QuestRewardDefinition reward = loot[i];
                if (reward == null || reward.amount <= 0)
                    continue;

                if (reward.type == QuestRewardType.Pi)
                    pendingAetherCredits += reward.amount;
                else if (reward.type == QuestRewardType.Item && reward.item != null)
                    lootEntries.Add(reward.item, reward.amount);
            }

            if (!HasRemainingLoot)
            {
                ReleaseToPoolOrDestroy();
                return;
            }

            EnsureVisualBindings();
            if (visualRenderer == null)
                Debug.LogWarning("[EnemyLootBag] Loot bag prefab has no MeshRenderer; the drop is invisible.", this);
            ApplyAuthoredVisual();
            StartIdleSmoke();
            EnsureLid();
            lid?.SnapClosed();
            initialized = true;
            StartExpiry(Mathf.Max(1f, unlootedLifetime));
            WorldUseController.Register(this);
        }

        private void OnEnable()
        {
            CacheShaders();
            if (!Application.isPlaying)
            {
                ApplyAuthoredVisual();
                return;
            }

            // Re-enabled mid-life (coroutines stop on disable): resume the unlooted expiry where it was.
            if (initialized && !isDissolving && expireRoutine == null && !float.IsInfinity(expireTime))
                StartExpiry(Mathf.Max(0f, expireTime - Time.time));
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            expireRoutine = null;
            WorldUseController.Unregister(this);
            if (!GameplayInputRecovery.IsTearingDown)
                ResolveUiManager()?.HideInteractionPrompt();
        }

        /// <summary>Unlooted lifetime on scaled time (pauses with the loot window), no per-frame Update.</summary>
        private void StartExpiry(float seconds)
        {
            if (expireRoutine != null)
                StopCoroutine(expireRoutine);
            expireTime = Time.time + seconds;
            expireRoutine = StartCoroutine(ExpireAfter(seconds));
        }

        private IEnumerator ExpireAfter(float seconds)
        {
            if (seconds > 0f)
                yield return new WaitForSeconds(seconds);
            expireRoutine = null;
            if (initialized && !isDissolving)
                BeginDissolve();
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (!initialized || isDissolving || !HasRemainingLoot || !IsWithinRange(context.PlayerPosition))
                return -1f;

            float distance = Vector3.Distance(context.PlayerPosition, transform.position);
            return 94f - distance;
        }

        public bool TryUse(WorldUseContext context)
        {
            if (!initialized || isDissolving || !HasRemainingLoot || !IsWithinRange(context.PlayerPosition))
                return false;

            OpenLootDialog();
            return true;
        }

        /// <summary>Grants one item entry by stable id (loot window rows).</summary>
        public DMLootGrantResult TryLootEntry(int entryId)
        {
            GrantPendingAetherCredits();
            DMLootGrantResult result = lootEntries.TryLootEntry(entryId);
            RefreshLootState();
            return result;
        }

        /// <summary>Takes whatever fits in list order; the rest stays in the bag (D4).</summary>
        public DMLootGrantResult TryLootAll()
        {
            GrantPendingAetherCredits();
            DMLootGrantResult result = lootEntries.TryLootAll();
            RefreshLootState();
            return result;
        }

        public void NotifyWindowClosed()
        {
            // Bag timers stay 20 s unlooted / 2 s after looted (scaled time, paused while the window is up).
            lootEntries.ResetVisit();
            if (lid != null && !isDissolving)
                lid.Close();
        }

        private void EnsureLid()
        {
            if (!animateLid || !Application.isPlaying)
                return;

            if (lidTransform == null)
                lidTransform = FindLidChild(transform);
            if (lidTransform == null)
                return;

            lid = GetComponent<DMChestLid>();
            if (lid == null)
                lid = gameObject.AddComponent<DMChestLid>();
            lid.ConfigureProcedural(lidTransform, lidOpenEuler, lidOpenOffset, lidOpenSeconds, lidCloseSeconds);
        }

        private static Transform FindLidChild(Transform root)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child != root && child.name.IndexOf("Lid", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return child;
            }

            return null;
        }

        /// <summary>AC never gets a loot row (D6): it is granted when the bag is opened or looted.</summary>
        private void GrantPendingAetherCredits()
        {
            if (pendingAetherCredits <= 0)
                return;

            int amount = pendingAetherCredits;
            pendingAetherCredits = 0;
            DMLootGrant.GrantAetherCredits(amount, "Loot Bag");
        }

        private void RefreshLootState()
        {
            if (HasRemainingLoot)
                return;

            ScheduleDissolveAfterLoot();
        }

        private void ScheduleDissolveAfterLoot()
        {
            if (isDissolving)
                return;

            expireTime = float.PositiveInfinity;
            if (expireRoutine != null)
            {
                StopCoroutine(expireRoutine);
                expireRoutine = null;
            }
            ResolveUiManager()?.HideInteractionPrompt();
            WorldUseController.Unregister(this);
            if (dissolveRoutine == null)
                dissolveRoutine = StartCoroutine(DissolveAfterDelay(Mathf.Max(0.1f, lootedDissolveDelay)));
        }

        private void BeginDissolve()
        {
            if (isDissolving)
                return;

            if (expireRoutine != null)
            {
                StopCoroutine(expireRoutine);
                expireRoutine = null;
            }
            ResolveUiManager()?.HideInteractionPrompt();
            WorldUseController.Unregister(this);
            if (dissolveRoutine != null)
                StopCoroutine(dissolveRoutine);
            dissolveRoutine = StartCoroutine(DissolveRoutine());
        }

        private IEnumerator DissolveAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            yield return DissolveRoutine();
        }

        private IEnumerator DissolveRoutine()
        {
            isDissolving = true;
            BoostDissolveSmoke();
            DisableSolidColliders();
            ApplyDissolveMaterials();

            float elapsed = 0f;
            while (elapsed < dissolveDuration)
            {
                elapsed += Time.deltaTime;
                SetDissolveAmount(Mathf.Clamp01(elapsed / dissolveDuration));
                yield return null;
            }

            SetDissolveAmount(1f);

            DetachVolumetricSmoke();
            // Unity null check: the enemy shell may already be gone (scene cleanup / despawn).
            EnemyLootable lootOwner = owner;
            owner = null;
            if (lootOwner != null)
                lootOwner.NotifyLootBagDissolved();
            ReleaseToPoolOrDestroy();
            // Notify destroys the enemy shell; sweep leftover anchors / weapons / smoke / empty clones.
            EnemyDeathRuntimeCleanup.SweepOrphans(destroyImmediately: false);
        }

        private void StartIdleSmoke()
        {
            if (!enableVolumetricSmoke)
                return;

            volumetricSmokeEmitter = VolumetricSmokeEmitter.Play(
                transform,
                Vector3.up * 0.18f,
                VolumetricSmokeEmitter.LootBagIdle);
        }

        private void BoostDissolveSmoke()
        {
            if (!enableVolumetricSmoke)
                return;

            if (volumetricSmokeEmitter != null)
            {
                volumetricSmokeEmitter.Retarget(VolumetricSmokeEmitter.LootBagDissolve);
                return;
            }

            volumetricSmokeEmitter = VolumetricSmokeEmitter.Play(
                transform,
                Vector3.up * 0.18f,
                VolumetricSmokeEmitter.LootBagDissolve);
        }

        private void DetachVolumetricSmoke()
        {
            if (volumetricSmokeEmitter == null)
                return;

            volumetricSmokeEmitter.transform.SetParent(null, true);
            volumetricSmokeEmitter.StopAndDestroy(volumetricSmokeLinger);
            volumetricSmokeEmitter = null;
        }

        public void ApplyVisualOverrides(Mesh meshOverride, Texture textureOverride)
        {
            if (meshOverride != null)
                dropMesh = meshOverride;
            if (textureOverride != null)
                dropTexture = textureOverride;

            ApplyAuthoredVisual();
        }

        private void CacheShaders()
        {
            if (dissolveShader != null)
                s_cachedDissolveShader = dissolveShader;
        }

        private static Shader ResolveDissolveShader()
        {
            if (s_cachedDissolveShader != null)
                return s_cachedDissolveShader;
            // Serialized profile ref first (survives player builds); Shader.Find stays as the editor fallback.
            DMLootChestProfile profile = DMLootChestProfile.Live;
            if (profile != null && profile.dissolveShader != null)
                return s_cachedDissolveShader = profile.dissolveShader;
            s_cachedDissolveShader = Shader.Find("Project/EnemyDisintegrate");
            return s_cachedDissolveShader;
        }

        private void OnValidate()
        {
            ApplyAuthoredVisual();
        }

        private void ApplyAuthoredVisual()
        {
            EnsureVisualBindings();
            if (visualMeshFilter == null && visualRenderer == null)
                return;

            if (dropMesh != null && visualMeshFilter != null)
                visualMeshFilter.sharedMesh = dropMesh;

            if (dropTexture != null && visualRenderer != null)
            {
                ApplyTextureToRenderer(visualRenderer, dropTexture);
                // Runtime texture overrides clone the material: track the clone so release / destroy frees it.
                Material applied = visualRenderer.sharedMaterial;
                if (Application.isPlaying && applied != null && !IsAuthoredMaterial(applied) && !runtimeTextureMaterials.Contains(applied))
                    runtimeTextureMaterials.Add(applied);
            }
        }

        private void EnsureVisualBindings()
        {
            if (visualMeshFilter == null)
                visualMeshFilter = GetComponentInChildren<MeshFilter>(true);
            if (visualRenderer == null)
                visualRenderer = GetComponentInChildren<MeshRenderer>(true);
            if (visualRenderer != null)
                bagRenderer = visualRenderer;
        }

        private static void ApplyTextureToRenderer(MeshRenderer renderer, Texture texture)
        {
            if (renderer == null || texture == null)
                return;

            Material shared = renderer.sharedMaterial;
            if (shared == null)
                return;

            Material material = shared;
            if (!Application.isPlaying)
            {
                // Prefab/edit mode: write onto the assigned material so Inspector changes stick.
            }
            else
            {
                material = renderer.material;
            }

            if (material.HasProperty("_BaseColorMap"))
                material.SetTexture("_BaseColorMap", texture);
            if (material.HasProperty("_UnlitColorMap"))
                material.SetTexture("_UnlitColorMap", texture);
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
            material.mainTexture = texture;
            renderer.sharedMaterial = material;
        }

        /// <summary>Every visual part (drop box body and lid) dissolves, each keeping its own albedo.</summary>
        private void ApplyDissolveMaterials()
        {
            Shader shader = ResolveDissolveShader();
            MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(false);
            if (shader == null || renderers.Length == 0)
            {
                EnsureDissolveMaterial();
                if (bagRenderer != null && dissolveMaterial != null)
                    bagRenderer.sharedMaterial = dissolveMaterial;
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || IsUnderSmoke(renderer.transform))
                    continue;

                Material material = BuildDissolveMaterial(shader, renderer.sharedMaterial);
                dissolveMaterials.Add(material);
                Material[] slots = renderer.sharedMaterials;
                int count = Mathf.Max(1, slots != null ? slots.Length : 1);
                Material[] replaced = new Material[count];
                for (int s = 0; s < count; s++)
                    replaced[s] = material;
                renderer.sharedMaterials = replaced;
            }
        }

        private Material BuildDissolveMaterial(Shader shader, Material source)
        {
            Material material = new Material(shader) { name = "DM_EnemyLootBag_Dissolve (Runtime)" };
            Color color = new Color(0.42f, 0.28f, 0.14f, 1f);
            if (source != null)
            {
                if (source.HasProperty(BaseColorId))
                    color = source.GetColor(BaseColorId);
                else if (source.HasProperty("_Color"))
                    color = source.color;

                string[] textureProps = { "_BaseColorMap", "_BaseMap", "_MainTex", "_UnlitColorMap" };
                for (int i = 0; i < textureProps.Length; i++)
                {
                    string prop = textureProps[i];
                    if (!source.HasProperty(prop))
                        continue;
                    Texture texture = source.GetTexture(prop);
                    if (texture == null)
                        continue;
                    material.SetTexture("_BaseMap", texture);
                    material.SetTextureScale("_BaseMap", source.GetTextureScale(prop));
                    material.SetTextureOffset("_BaseMap", source.GetTextureOffset(prop));
                    break;
                }
            }

            material.SetColor(BaseColorId, color);
            material.SetFloat(DissolveEdgeWidthId, dissolveEdgeWidth);
            material.SetColor(DissolveEdgeColorId, dissolveEdgeColor);
            material.SetFloat(DissolveAmountId, 0f);
            return material;
        }

        private void SetDissolveAmount(float amount)
        {
            if (dissolveMaterial != null)
                dissolveMaterial.SetFloat(DissolveAmountId, amount);
            for (int i = 0; i < dissolveMaterials.Count; i++)
            {
                if (dissolveMaterials[i] != null)
                    dissolveMaterials[i].SetFloat(DissolveAmountId, amount);
            }
        }

        /// <summary>The drop box body collider must not block the player while it dissolves.</summary>
        private void DisableSolidColliders()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(false);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = false;
            }
        }

        private void EnsureDissolveMaterial()
        {
            if (dissolveMaterial != null)
                return;

            Shader shader = ResolveDissolveShader();
            if (shader == null)
                return;

            Color baseColor = bagRenderer != null && bagRenderer.sharedMaterial != null
                ? bagRenderer.sharedMaterial.color
                : new Color(0.42f, 0.28f, 0.14f, 1f);

            dissolveMaterial = new Material(shader);
            dissolveMaterial.SetColor(BaseColorId, baseColor);
            dissolveMaterial.SetFloat(DissolveEdgeWidthId, dissolveEdgeWidth);
            dissolveMaterial.SetColor(DissolveEdgeColorId, dissolveEdgeColor);
            dissolveMaterial.SetFloat(DissolveAmountId, 0f);
        }

        private void OnDestroy()
        {
            if (dissolveRoutine != null)
                StopCoroutine(dissolveRoutine);

            DestroyDissolveMaterials();
            for (int i = 0; i < runtimeTextureMaterials.Count; i++)
            {
                if (runtimeTextureMaterials[i] != null)
                    Destroy(runtimeTextureMaterials[i]);
            }
            runtimeTextureMaterials.Clear();

            DetachVolumetricSmoke();
        }

        private void DestroyDissolveMaterials()
        {
            if (dissolveMaterial != null)
                Destroy(dissolveMaterial);
            dissolveMaterial = null;
            for (int i = 0; i < dissolveMaterials.Count; i++)
            {
                if (dissolveMaterials[i] != null)
                    Destroy(dissolveMaterials[i]);
            }
            dissolveMaterials.Clear();
        }

#if UNITY_EDITOR
        private void Reset()
        {
            EnsureVisualBindings();
        }
#endif

        private void OpenLootDialog()
        {
            if (EnemyLootDialogUI.IsDialogOpen)
                return;

            GrantPendingAetherCredits();
            if (lootEntries.IsEmpty)
            {
                // AC-only bag: the AC card is the whole reward; dissolve on the normal looted delay.
                RefreshLootState();
                return;
            }

            if (EnemyLootDialogUI.Show(this))
                lid?.Open();
        }

        private bool IsWithinRange(Vector3 playerPosition)
        {
            return Vector3.Distance(playerPosition, transform.position) <= interactRange;
        }

        public string GetInteractionPromptMessage()
        {
            string label = string.IsNullOrWhiteSpace(displayName) ? "Loot Bag" : displayName;
            return $"{promptText} — {label}";
        }

        private static UIManager ResolveUiManager()
        {
            if (s_uiManager == null)
                s_uiManager = FindAnyObjectByType<UIManager>();
            return s_uiManager;
        }
    }
}

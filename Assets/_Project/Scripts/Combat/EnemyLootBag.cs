using System.Collections;
using System.Collections.Generic;
using Project.AI;
using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Progression;
using Project.Quests;
using Project.UI;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// World loot bag dropped after an enemy disintegrates. Dissolves after 20s unlooted or 2s after looting.
    /// Edit the prefab mesh/texture in the Inspector — those visuals are instanced on drop.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class EnemyLootBag : MonoBehaviour, IWorldUsable, IEnemyLootProvider
    {
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

        private readonly List<QuestRewardDefinition> remainingLoot = new List<QuestRewardDefinition>();

        private EnemyLootable owner;
        private string displayName;
        private UIManager uiManager;
        private MeshRenderer bagRenderer;
        private Material dissolveMaterial;
        private VolumetricSmokeEmitter volumetricSmokeEmitter;
        private float expireTime;
        private bool playerInRange;
        private bool isDissolving;
        private bool initialized;
        private Coroutine dissolveRoutine;

        public bool HasRemainingLoot => remainingLoot.Count > 0;

        public bool CanPlayerLoot(Vector3 playerPosition)
        {
            return initialized && !isDissolving && HasRemainingLoot && IsWithinRange(playerPosition);
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

            Vector3 spawnPosition = SnapToGround(worldPosition);
            GameObject bagObject = InstantiateBag(lootBagPrefab, spawnPosition);
            if (bagObject == null)
                return null;

            EnemyLootBag bag = bagObject.GetComponent<EnemyLootBag>();
            if (bag == null)
                bag = bagObject.AddComponent<EnemyLootBag>();

            bag.ApplyVisualOverrides(meshOverride, textureOverride);
            bag.Initialize(
                lootOwner,
                loot,
                lootDisplayName,
                range,
                interactPrompt,
                unlootedLifetimeSeconds,
                lootedDissolveDelaySeconds);
            return bag;
        }

        private static GameObject InstantiateBag(GameObject lootBagPrefab, Vector3 spawnPosition)
        {
            GameObject prefab = lootBagPrefab;
            if (prefab == null)
                prefab = Resources.Load<GameObject>("Combat/EnemyLootBag");

            if (prefab == null)
            {
                Debug.LogWarning(
                    "[EnemyLootBag] Missing loot bag prefab. Assign " +
                    EnemyLootable.DefaultLootBagPrefabPath + " on EnemyLootable.");
                return null;
            }

            GameObject bagObject = Instantiate(prefab);
            bagObject.name = "EnemyLootBag";
            bagObject.transform.position = spawnPosition;
            bagObject.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            return bagObject;
        }

        private static Vector3 SnapToGround(Vector3 worldPosition)
        {
            Vector3 rayOrigin = worldPosition + Vector3.up * 3f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 12f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * 0.14f;

            return worldPosition + Vector3.up * 0.14f;
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

            remainingLoot.Clear();
            for (int i = 0; i < loot.Count; i++)
            {
                if (loot[i] != null)
                    remainingLoot.Add(CloneReward(loot[i]));
            }

            if (remainingLoot.Count == 0)
            {
                Destroy(gameObject);
                return;
            }

            EnsureVisualBindings();
            if (visualRenderer == null)
                BuildVisual();
            ApplyAuthoredVisual();
            StartIdleSmoke();
            expireTime = Time.time + Mathf.Max(1f, unlootedLifetime);
            initialized = true;
            WorldUseController.Register(this);
        }

        private void OnDisable()
        {
            WorldUseController.Unregister(this);
            ResolveUiManager()?.HideInteractionPrompt();
            playerInRange = false;
        }

        private void Update()
        {
            if (!initialized || isDissolving)
                return;

            RefreshProximityPrompt();

            if (Time.time >= expireTime)
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

        public bool TryLootNextEntry()
        {
            if (!HasRemainingLoot)
                return false;

            QuestRewardDefinition entry = remainingLoot[0];
            if (TryGrantLootEntry(entry))
                remainingLoot.RemoveAt(0);

            RefreshLootState();
            return true;
        }

        public bool TryLootAll()
        {
            if (!HasRemainingLoot)
                return false;

            bool anyLeftUnlooted = false;
            for (int i = remainingLoot.Count - 1; i >= 0; i--)
            {
                if (TryGrantLootEntry(remainingLoot[i]))
                    remainingLoot.RemoveAt(i);
                else
                    anyLeftUnlooted = true;
            }

            if (anyLeftUnlooted)
                PickupToastUI.ShowInventoryFull();

            RefreshLootState();
            return true;
        }

        public string BuildLootSummary()
        {
            if (!HasRemainingLoot)
                return "Nothing left to loot.";

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.AppendLine("Loot bag contains:");
            for (int i = 0; i < remainingLoot.Count; i++)
            {
                QuestRewardDefinition entry = remainingLoot[i];
                if (entry == null)
                    continue;

                string line = QuestRewardFormatter.FormatLootLine(entry);
                if (!string.IsNullOrEmpty(line))
                    builder.AppendLine(line);
            }

            return builder.ToString().TrimEnd();
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
            ResolveUiManager()?.HideInteractionPrompt();
            WorldUseController.Unregister(this);
            dissolveRoutine = StartCoroutine(DissolveAfterDelay(Mathf.Max(0.1f, lootedDissolveDelay)));
        }

        private void BeginDissolve()
        {
            if (isDissolving)
                return;

            ResolveUiManager()?.HideInteractionPrompt();
            WorldUseController.Unregister(this);
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
            EnsureDissolveMaterial();

            if (bagRenderer != null && dissolveMaterial != null)
                bagRenderer.sharedMaterial = dissolveMaterial;

            float elapsed = 0f;
            while (elapsed < dissolveDuration)
            {
                elapsed += Time.deltaTime;
                float amount = Mathf.Clamp01(elapsed / dissolveDuration);
                if (dissolveMaterial != null)
                    dissolveMaterial.SetFloat(DissolveAmountId, amount);
                yield return null;
            }

            if (dissolveMaterial != null)
                dissolveMaterial.SetFloat(DissolveAmountId, 1f);

            DetachVolumetricSmoke();
            owner?.NotifyLootBagDissolved();
            Destroy(gameObject);
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

        private void OnEnable()
        {
            if (!Application.isPlaying)
                ApplyAuthoredVisual();
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
                ApplyTextureToRenderer(visualRenderer, dropTexture);
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

        private void BuildVisual()
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "BagVisual";
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = Vector3.up * 0.12f;
            visual.transform.localScale = new Vector3(0.42f, 0.3f, 0.42f);

            Collider primitiveCollider = visual.GetComponent<Collider>();
            if (primitiveCollider != null)
                Destroy(primitiveCollider);

            bagRenderer = visual.GetComponent<MeshRenderer>();
            visualRenderer = bagRenderer;
            visualMeshFilter = visual.GetComponent<MeshFilter>();
            if (bagRenderer != null)
            {
                Shader shader = Shader.Find("HDRP/Lit") ?? Shader.Find("HDRP/Unlit") ?? Shader.Find("Sprites/Default");
                Material bagMaterial = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/InternalErrorShader"));
                bagMaterial.name = "DM_EnemyLootBag (Runtime)";
                Color bagColor = new Color(0.42f, 0.28f, 0.14f, 1f);
                bagMaterial.color = bagColor;
                if (bagMaterial.HasProperty("_BaseColor"))
                    bagMaterial.SetColor("_BaseColor", bagColor);
                if (bagMaterial.HasProperty("_UnlitColor"))
                    bagMaterial.SetColor("_UnlitColor", bagColor);
                bagRenderer.sharedMaterial = bagMaterial;
            }

            GameObject tie = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tie.name = "BagTie";
            tie.transform.SetParent(visual.transform, false);
            tie.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            tie.transform.localScale = new Vector3(0.55f, 0.05f, 0.55f);
            Collider tieCollider = tie.GetComponent<Collider>();
            if (tieCollider != null)
                Destroy(tieCollider);

            MeshRenderer tieRenderer = tie.GetComponent<MeshRenderer>();
            if (tieRenderer != null && bagRenderer != null)
                tieRenderer.sharedMaterial = bagRenderer.sharedMaterial;
        }

        private void EnsureDissolveMaterial()
        {
            if (dissolveMaterial != null)
                return;

            Shader shader = Shader.Find("Project/EnemyDisintegrate");
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

            if (dissolveMaterial != null)
                Destroy(dissolveMaterial);

            DetachVolumetricSmoke();
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

            string label = string.IsNullOrWhiteSpace(displayName) ? "Enemy" : displayName;
            EnemyLootDialogUI.Show(this, label, BuildLootSummary());
        }

        private void RefreshProximityPrompt()
        {
            if (!GameSession.HasStarted || !HasRemainingLoot)
                return;

            if (!PlayerInteractionUtility.TryGetPlayerPosition(out Vector3 playerPosition))
                return;

            bool nearby = IsWithinRange(playerPosition);
            if (nearby == playerInRange)
                return;

            playerInRange = nearby;
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

        private UIManager ResolveUiManager()
        {
            if (uiManager == null)
                uiManager = FindAnyObjectByType<UIManager>();
            return uiManager;
        }

        /// <returns>True when the entry was fully granted and can be removed from remaining loot.</returns>
        private static bool TryGrantLootEntry(QuestRewardDefinition entry)
        {
            if (entry == null)
                return true;

            if (entry.type == QuestRewardType.Item && entry.item != null
                && !LevelUnlockUtility.PassesPickupGate(entry.item, showToast: true))
                return false;

            int requested = Mathf.Max(0, entry.amount);
            int granted = QuestRewardGranter.GrantReward(entry, "Loot Bag");

            if (entry.type == QuestRewardType.Item && entry.item != null)
            {
                if (granted > 0)
                    PickupToastUI.Show($"+{granted} {entry.item.itemName}");

                if (granted >= requested)
                    return true;

                entry.amount = Mathf.Max(0, requested - granted);
                return false;
            }

            if (entry.type == QuestRewardType.Pi && granted > 0)
                PickupToastUI.Show($"+{granted} AC");

            return granted > 0 || requested <= 0;
        }

        private static QuestRewardDefinition CloneReward(QuestRewardDefinition source)
        {
            return new QuestRewardDefinition
            {
                type = source.type,
                amount = source.amount,
                item = source.item
            };
        }
    }
}

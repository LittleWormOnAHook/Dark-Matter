using System.Collections;
using System.Collections.Generic;
using Project.AI;
using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Quests;
using Project.UI;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// World loot bag dropped after an enemy disintegrates. Dissolves after 20s unlooted or 2s after looting.
    /// </summary>
    [DisallowMultipleComponent]
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
            float lootedDissolveDelaySeconds = 2f)
        {
            if (lootOwner == null || loot == null || loot.Count == 0)
                return null;

            Vector3 spawnPosition = SnapToGround(worldPosition);

            GameObject bagObject = new GameObject("EnemyLootBag");
            bagObject.transform.position = spawnPosition;
            bagObject.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            EnemyLootBag bag = bagObject.AddComponent<EnemyLootBag>();
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

            BuildVisual();
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
            remainingLoot.RemoveAt(0);
            GrantLootEntry(entry);
            RefreshLootState();
            return true;
        }

        public bool TryLootAll()
        {
            if (!HasRemainingLoot)
                return false;

            for (int i = remainingLoot.Count - 1; i >= 0; i--)
                GrantLootEntry(remainingLoot[i]);

            remainingLoot.Clear();
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
            {
                if (playerInRange)
                    ShowPrompt();
                return;
            }

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
            if (bagRenderer != null)
            {
                Material bagMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                bagMaterial.color = new Color(0.42f, 0.28f, 0.14f, 1f);
                bagMaterial.SetFloat("_Smoothness", 0.2f);
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
            if (playerInRange)
                ShowPrompt();
            else
                ResolveUiManager()?.HideInteractionPrompt();
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

        private void ShowPrompt()
        {
            UIManager manager = ResolveUiManager();
            if (manager == null)
                return;

            manager.ShowInteractionPrompt(GetInteractionPromptMessage());
        }

        private UIManager ResolveUiManager()
        {
            if (uiManager == null)
                uiManager = FindAnyObjectByType<UIManager>();
            return uiManager;
        }

        private static void GrantLootEntry(QuestRewardDefinition entry)
        {
            if (entry == null)
                return;

            QuestRewardGranter.GrantReward(entry, "Loot Bag");

            if (entry.type == QuestRewardType.Pi)
                PickupToastUI.Show($"+{entry.amount} AC");
            else if (entry.type == QuestRewardType.Item && entry.item != null)
                PickupToastUI.Show($"+{entry.amount} {entry.item.itemName}");
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

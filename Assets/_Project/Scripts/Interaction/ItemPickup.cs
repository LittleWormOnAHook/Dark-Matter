using Project.Audio;
using Project.Combat;
using Project.Data;
using Project.Inventory;
using Project.Map;
using Project.Player;
using Project.Progression;
using Project.Quests;
using Project.UI;
using ECM2;
using UnityEngine;

namespace Project.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class ItemPickup : MonoBehaviour, IWorldUsable, IHoldWorldUsable, IWorldIndicatorAnchor
    {
        private const float AimUseBonus = 500f;

        [Header("Item Settings")]
        public ItemData itemData;
        public int amount = 1;

        [Header("Prompt")]
        public string promptText = "Hold E to Take";

        [Header("Indicator")]
        [Tooltip("Optional explicit stem/dot attach point. When empty, uses renderer/collider bounds center.")]
        [SerializeField] private Transform indicatorAnchor;
        [Tooltip("World-up stem length (m) when the player first locks onto this pickup.")]
        [SerializeField] private float indicatorStemMinHeight = 0.25f;
        [Tooltip("World-up stem length (m) at planar near reach (dot largest, still on tip).")]
        [SerializeField] private float indicatorStemMaxHeight = 0.5f;

        [Header("Respawn Settings")]
        public bool canRespawn = true;
        public float minRespawnTime = 20f;
        public float maxRespawnTime = 90f;

        private UIManager uiManager;
        private Collider[] colliders;
        private Renderer[] renderers;
        private bool[] colliderWasEnabled;
        private bool[] rendererWasEnabled;
        private bool isPickedUp = false;
        private bool holdActive;
        private float holdProgress;
        private int respawnAmount = 1;
        private Vector3 indicatorLocalOffset;
        private bool hasIndicatorLocalOffset;
        private Transform cachedHierarchyParent;
        private bool hierarchyStateValid;
        private bool hierarchyBlocksCollection;

        public bool IsPickedUp => isPickedUp;
        /// <summary>
        /// True when this pickup's item type has been identified (scanner sweep / mining F-scan).
        /// Hold-E known-vs-unknown UI wires through this when present on other branches.
        /// </summary>
        public bool IsItemIdentified =>
            itemData != null && ResourceIdentificationRegistry.IsIdentified(itemData);

        /// <summary>
        /// Label for scanner optics / prompts. Unknown until scanned.
        /// TODO: Later some items may require Gerald or a designated NPC to identify (not scanner alone).
        /// </summary>
        public string GetScanDisplayName()
        {
            if (itemData == null)
                return "Unknown Item";
            if (ResourceIdentificationRegistry.IsIdentified(itemData))
                return string.IsNullOrWhiteSpace(itemData.itemName) ? itemData.name : itemData.itemName;
            return "Unknown Item";
        }

        public bool IsIndicatorAvailable => IsCollectibleWorldPickup();

        public float IndicatorStemMinHeight => Mathf.Max(0.05f, indicatorStemMinHeight);

        public float IndicatorStemMaxHeight => Mathf.Max(IndicatorStemMinHeight, indicatorStemMaxHeight);

        public float HoldDurationSeconds => WorldPickupFocus.PickupHoldSeconds;

        public string HoldPromptText => string.IsNullOrEmpty(promptText) ? "Hold E to Take" : promptText;

        public bool IsHoldActive => holdActive;

        public float HoldProgress01 => Mathf.Clamp01(holdProgress);

        public Vector3 GetIndicatorWorldAnchor()
        {
            if (indicatorAnchor != null)
                return indicatorAnchor.position;

            if (hasIndicatorLocalOffset)
                return transform.TransformPoint(indicatorLocalOffset);

            return ResolveIndicatorWorldAnchor();
        }

        private void OnEnable()
        {
            hierarchyStateValid = false;
            WorldUseController.Register(this);
            PickupProximityDotUI.Register(this);
        }

        private void OnDisable()
        {
            WorldUseController.Unregister(this);
            PickupProximityDotUI.Unregister(this);
        }

        /// <summary>
        /// True when an equipped-visual marker or Player ancestry disqualifies this pickup.
        /// Cached because the world-dot HUD tests every pickup every frame; reparenting (equip /
        /// drop) invalidates it.
        /// </summary>
        public bool HierarchyBlocksCollection
        {
            get
            {
                if (hierarchyStateValid && cachedHierarchyParent == transform.parent)
                    return hierarchyBlocksCollection;

                cachedHierarchyParent = transform.parent;
                hierarchyStateValid = true;
                hierarchyBlocksCollection = EvaluateHierarchyBlock();
                return hierarchyBlocksCollection;
            }
        }

        private void OnTransformParentChanged()
        {
            hierarchyStateValid = false;
        }

        private bool EvaluateHierarchyBlock()
        {
            if (GetComponent<EquippedVisualMarker>() != null || GetComponentInParent<EquippedVisualMarker>() != null)
                return true;

            Transform current = transform;
            while (current != null)
            {
                if (current.CompareTag("Player"))
                    return true;

                current = current.parent;
            }

            return false;
        }

        private void Start()
        {
            uiManager = FindAnyObjectByType<UIManager>();
            colliders = GetComponentsInChildren<Collider>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
            respawnAmount = Mathf.Max(1, amount);
            StripMisplacedProjectileBehaviour();
            EnsurePickupTriggerCollider();
            CacheIndicatorLocalOffset();
        }

        private void StripMisplacedProjectileBehaviour()
        {
            CombatProjectile projectile = GetComponent<CombatProjectile>();
            if (projectile != null)
                Destroy(projectile);
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (!IsCollectibleWorldPickup())
                return -1f;

            Vector3 anchor = GetIndicatorWorldAnchor();
            float distance = Vector3.Distance(context.PlayerPosition, anchor);
            if (distance > context.UseRange)
                return -1f;

            Vector3 aimPoint = anchor;
            float score = WorldUseController.ScorePickupAim(context.ViewRay, aimPoint, distance, context.UseRange);
            if (score < 0f)
                return -1f;

            if (context.AimHit.HasValue && IsAimTarget(context.AimHit.Value.collider))
                score += AimUseBonus;

            return score;
        }

        public bool TryUse(WorldUseContext context)
        {
            // Press path is a no-op. Hold-E via WorldUseController.FindHoldTarget + TickHold owns collect.
            return false;
        }

        public bool CanBeginHold(WorldUseContext context)
        {
            if (!IsCollectibleWorldPickup() || itemData == null)
                return false;
            if (!WorldPickupFocus.IsFocused(this))
                return false;
            if (!WorldPickupFocus.IsWithinClosePromptRange(context.PlayerPosition, GetIndicatorWorldAnchor()))
                return false;
            if (context.Inventory == null)
                return false;
            if (!LevelUnlockUtility.PassesPickupGate(itemData, showToast: false))
                return false;
            return true;
        }

        public void BeginHold(WorldUseContext context)
        {
            holdActive = true;
            holdProgress = 0f;
        }

        public bool TickHold(WorldUseContext context, float deltaTime, out float progress01)
        {
            progress01 = Mathf.Clamp01(holdProgress);
            if (!holdActive)
                return false;

            // Focus lost or walked out of close range cancels.
            if (!WorldPickupFocus.IsFocused(this)
                || !WorldPickupFocus.IsWithinClosePromptRange(context.PlayerPosition, GetIndicatorWorldAnchor()))
            {
                CancelHold(context);
                progress01 = 0f;
                return false;
            }

            float duration = Mathf.Max(0.05f, HoldDurationSeconds);
            holdProgress += deltaTime / duration;
            progress01 = Mathf.Clamp01(holdProgress);
            if (holdProgress < 1f)
                return false;

            holdActive = false;
            holdProgress = 0f;
            bool collected = TryCollectFor(context.Inventory, showPlayerPrompt: true);
            if (collected && itemData != null)
                ResourceIdentificationRegistry.Identify(itemData);
            return true;
        }

        public void CancelHold(WorldUseContext context)
        {
            holdActive = false;
            holdProgress = 0f;
        }

public void PrepareForWorldDrop(ItemData item, int dropAmount)
        {
            itemData = item;
            amount = dropAmount;
            respawnAmount = Mathf.Max(1, dropAmount);
            canRespawn = false;
            isPickedUp = false;
            enabled = true;
            CancelInvoke(nameof(Respawn));

            gameObject.SetActive(true);

            if (transform.localScale.sqrMagnitude < 0.0001f)
                transform.localScale = Vector3.one;

            colliders = GetComponentsInChildren<Collider>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
            CacheIndicatorLocalOffset();

            // Do not force-enable every Renderer/Collider. World pickup prefabs often keep
            // shell meshes disabled on purpose (e.g. Plasma Fuel barrel vs canister child).
            // Blindly enabling them shows the wrong mesh / loses the authored look.

            ResourceNode resourceNode = GetComponent<ResourceNode>();
            if (resourceNode != null)
            {
                if (Application.isPlaying)
                    Destroy(resourceNode);
                else
                    DestroyImmediate(resourceNode);
            }
        }

        private void EnsurePickupTriggerCollider()
        {
            if (colliders == null || colliders.Length == 0)
                colliders = GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].isTrigger)
                    return;
            }

            SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.45f;
        }

        public void TryPickup(InventorySystem inventory = null)
        {
            if (inventory == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                    inventory = player.GetComponent<InventorySystem>();
            }

            TryCollectFor(inventory, showPlayerPrompt: true);
        }

        public bool TryCollectFor(InventorySystem inventory, bool showPlayerPrompt = true)
        {
            if (isPickedUp) return false;

            if (!IsCollectibleWorldPickup()) return false;

            if (inventory == null || itemData == null) return false;

            if (!LevelUnlockUtility.PassesPickupGate(itemData, showToast: showPlayerPrompt))
                return false;

            int requested = amount;
            int added = inventory.AddItem(itemData, requested);
            if (added <= 0)
            {
                if (showPlayerPrompt)
                    PickupToastUI.ShowInventoryFull();
                return false;
            }

            QuestManager questManager = QuestManager.EnsureExists();
            questManager?.NotifyItemCollected(itemData, added);
            Project.Achievements.AchievementManager.EnsureExists()
                ?.ReportProgress(Project.Achievements.AchievementTriggerType.CollectItem, itemData.name, added);

            amount = Mathf.Max(0, requested - added);

            GameAudioManager.Instance?.PlayItemPickup();

            if (showPlayerPrompt && uiManager != null)
            {
                if (itemData.isAcInfused)
                    uiManager.ShowAcReward(itemData.acValue, "Pickup");

                uiManager.HideInteractionPrompt();
            }

            itemData.TryGrantConfiguredXp();

            PickupToastUI.Show($"+{added} {itemData.itemName}");

            if (showPlayerPrompt)
                TryPlayLootAnimation(inventory);

            if (amount <= 0)
            {
                isPickedUp = true;
                PickupProximityDotUI.NotifyCollected(this);

                if (canRespawn)
                {
                    StartRespawn();
                }
                else
                {
                    PickupProximityDotUI.Unregister(this);
                    Destroy(gameObject);
                }
            }

            return true;
        }

        private static void TryPlayLootAnimation(InventorySystem inventory)
        {
            if (inventory == null)
                return;

            PlayerLootAnimationController lootAnimation = inventory.GetComponentInChildren<PlayerLootAnimationController>();
            if (lootAnimation == null)
            {
                ECM2.Character character = inventory.GetComponent<Character>();
                Animator animator = character != null ? character.GetAnimator() : null;
                if (animator == null)
                    return;

                lootAnimation = animator.gameObject.AddComponent<PlayerLootAnimationController>();
            }

            lootAnimation.BeginLoot();
        }

        private bool IsCollectibleWorldPickup()
        {
            if (isPickedUp || itemData == null || !isActiveAndEnabled)
                return false;

            if (GetComponent<EquippedVisualMarker>() != null || GetComponentInParent<EquippedVisualMarker>() != null)
                return false;

            Transform current = transform;
            while (current != null)
            {
                if (current.CompareTag("Player"))
                    return false;

                current = current.parent;
            }

            return true;
        }

        private void CacheIndicatorLocalOffset()
        {
            if (indicatorAnchor != null)
            {
                hasIndicatorLocalOffset = false;
                return;
            }

            Vector3 world = ResolveIndicatorWorldAnchor();
            indicatorLocalOffset = transform.InverseTransformPoint(world);
            hasIndicatorLocalOffset = true;
        }

        private Vector3 ResolveIndicatorWorldAnchor()
        {
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>(true);

            if (TryEncapsulateRendererBounds(renderers, out Bounds rendBounds))
                return rendBounds.center;

            if (colliders == null || colliders.Length == 0)
                colliders = GetComponentsInChildren<Collider>(true);

            if (TryEncapsulateColliderBounds(colliders, out Bounds colBounds))
                return colBounds.center;

            return transform.position + Vector3.up * 0.2f;
        }

        private static bool TryEncapsulateRendererBounds(Renderer[] renderers, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            if (renderers == null)
                return false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rend = renderers[i];
                if (rend == null || !rend.enabled || !rend.gameObject.activeInHierarchy)
                    continue;
                if (rend is ParticleSystemRenderer)
                    continue;

                if (!any)
                {
                    bounds = rend.bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(rend.bounds);
                }
            }

            return any;
        }

        private static bool TryEncapsulateColliderBounds(Collider[] colliders, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            if (colliders == null)
                return false;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
                    continue;

                if (!any)
                {
                    bounds = col.bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(col.bounds);
                }
            }

            return any;
        }

        private bool IsAimTarget(Collider collider)
        {
            return collider != null && collider.GetComponentInParent<ItemPickup>() == this;
        }

        private void StartRespawn()
        {
            colliders = GetComponentsInChildren<Collider>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
            colliderWasEnabled = new bool[colliders.Length];
            rendererWasEnabled = new bool[renderers.Length];

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col == null)
                    continue;
                colliderWasEnabled[i] = col.enabled;
                col.enabled = false;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rend = renderers[i];
                if (rend == null)
                    continue;
                // Remember prefab-authored visibility so respawn does not turn on shell meshes.
                rendererWasEnabled[i] = rend.enabled;
                rend.enabled = false;
            }

            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] != null)
                    particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            // Nested mesh roots can miss Renderer discovery; force-hide the whole instance.
            if (renderers == null || renderers.Length == 0)
                gameObject.SetActive(false);

            float respawnTime = Random.Range(minRespawnTime, maxRespawnTime);
            Invoke(nameof(Respawn), respawnTime);
        }

        private void Respawn()
        {
            isPickedUp = false;
            amount = Mathf.Max(1, respawnAmount);

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (colliders != null)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] == null)
                        continue;
                    bool wasEnabled = colliderWasEnabled != null && i < colliderWasEnabled.Length && colliderWasEnabled[i];
                    colliders[i].enabled = wasEnabled;
                }
            }

            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null)
                        continue;
                    bool wasEnabled = rendererWasEnabled != null && i < rendererWasEnabled.Length && rendererWasEnabled[i];
                    renderers[i].enabled = wasEnabled;
                }
            }

            Collider mainCollider = GetComponent<Collider>();
            if (mainCollider != null)
                mainCollider.isTrigger = true;

            PickupProximityDotUI.Register(this);
        }
    }
}

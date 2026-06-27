using Project.Audio;
using Project.Data;
using Project.Inventory;
using Project.Quests;
using Project.UI;
using UnityEngine;

namespace Project.Interaction
{
    [RequireComponent(typeof(Collider))]
    public class ItemPickup : MonoBehaviour, IWorldUsable
    {
        private const float AimUseBonus = 500f;

        [Header("Item Settings")]
        public ItemData itemData;
        public int amount = 1;

        [Header("Prompt")]
        public string promptText = "Press E to use";

        [Header("Respawn Settings")]
        public bool canRespawn = true;
        public float minRespawnTime = 20f;
        public float maxRespawnTime = 90f;

        private UIManager uiManager;
        private Collider[] colliders;
        private Renderer[] renderers;
        private bool isPickedUp = false;

        public bool IsPickedUp => isPickedUp;

        private void OnEnable()
        {
            WorldUseController.Register(this);
            PickupProximityDotUI.Register(this);
        }

        private void OnDisable()
        {
            WorldUseController.Unregister(this);
            PickupProximityDotUI.Unregister(this);
        }

        private void Start()
        {
            uiManager = FindAnyObjectByType<UIManager>();
            colliders = GetComponentsInChildren<Collider>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
            EnsurePickupTriggerCollider();
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (!IsCollectibleWorldPickup())
                return -1f;

            float distance = Vector3.Distance(context.PlayerPosition, transform.position);
            if (distance > context.UseRange)
                return -1f;

            Collider col = GetComponentInChildren<Collider>();
            Vector3 aimPoint = col != null ? col.bounds.center : transform.position + Vector3.up * 0.2f;
            float score = WorldUseController.ScorePickupAim(context.ViewRay, aimPoint, distance, context.UseRange);
            if (score < 0f)
                return -1f;

            if (context.AimHit.HasValue && IsAimTarget(context.AimHit.Value.collider))
                score += AimUseBonus;

            return score;
        }

        public bool TryUse(WorldUseContext context)
        {
            return TryCollectFor(context.Inventory, showPlayerPrompt: true);
        }

        public void PrepareForWorldDrop(ItemData item, int dropAmount)
        {
            itemData = item;
            amount = dropAmount;
            canRespawn = false;
            isPickedUp = false;
            enabled = true;
            CancelInvoke(nameof(Respawn));

            gameObject.SetActive(true);

            if (transform.localScale.sqrMagnitude < 0.0001f)
                transform.localScale = Vector3.one;

            colliders = GetComponentsInChildren<Collider>(true);
            renderers = GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].enabled = true;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].enabled = true;
            }

            ResourceNode resourceNode = GetComponent<ResourceNode>();
            if (resourceNode != null)
                Destroy(resourceNode);
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

            int added = inventory.AddItem(itemData, amount);
            if (added > 0)
            {
                QuestManager questManager = QuestManager.EnsureExists();
                questManager?.NotifyItemCollected(itemData, added);
            }

            if (added >= amount)
            {
                GameAudioManager.Instance?.PlayItemPickup();

                if (showPlayerPrompt && uiManager != null)
                {
                    if (itemData.isPiInfused)
                        uiManager.ShowAcReward(itemData.piValue, "Pickup");

                    uiManager.HideInteractionPrompt();
                }

                PickupToastUI.Show($"+{amount} {itemData.itemName}");

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

                return true;
            }

            if (showPlayerPrompt && added == 0)
            {
                if (uiManager != null)
                    uiManager.ShowInteractionPrompt("Inventory is full!");
            }

            return false;
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

        private bool IsAimTarget(Collider collider)
        {
            return collider != null && collider.GetComponentInParent<ItemPickup>() == this;
        }

        private void StartRespawn()
        {
            colliders = GetComponentsInChildren<Collider>(true);
            renderers = GetComponentsInChildren<Renderer>(true);

            foreach (var col in colliders)
            {
                if (col != null) col.enabled = false;
            }

            foreach (var rend in renderers)
            {
                if (rend != null) rend.enabled = false;
            }

            float respawnTime = Random.Range(minRespawnTime, maxRespawnTime);
            Invoke(nameof(Respawn), respawnTime);
        }

        private void Respawn()
        {
            isPickedUp = false;

            foreach (var col in colliders)
            {
                if (col != null) col.enabled = true;
            }

            foreach (var rend in renderers)
            {
                if (rend != null) rend.enabled = true;
            }

            Collider mainCollider = GetComponent<Collider>();
            if (mainCollider != null)
            {
                mainCollider.isTrigger = true;
            }

            PickupProximityDotUI.Register(this);
        }
    }
}

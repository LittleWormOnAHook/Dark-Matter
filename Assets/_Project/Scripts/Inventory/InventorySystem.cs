using System.Collections.Generic;
using Project.Combat;
using Project.Data;
using Project.Interaction;
using Project.Progression;
using Project.Rendering;
using Project.Survival;
using UnityEngine;

namespace Project.Inventory
{
    public class InventorySystem : MonoBehaviour
    {
        public const int MainInventoryColumns = 10;
        public const int StorageRowSlotCount = 10;
        public const int DefaultUnlockedMainSlots = 20;
        public const int DefaultTotalMainSlots = 50;

        [Header("Inventory Settings")]
        [Tooltip("Total main inventory slots including locked expansion rows.")]
        public int inventorySize = DefaultTotalMainSlots;
        [Tooltip("How many main inventory slots are unlocked at game start / currently. Remaining slots stay locked until Increase Storage Module crafts.")]
        public int unlockedMainSlots = DefaultUnlockedMainSlots;
        public int hotbarSize = 10;
        public int toolbarSize = 2;

        [System.Serializable]
        public class InventorySlot
        {
            public ItemData item;
            public int amount;

            public bool IsEmpty => item == null || amount <= 0;
        }

        [System.NonSerialized]
        public List<InventorySlot> slots = new List<InventorySlot>();

        public event System.Action OnInventoryChanged;

        private SurvivalStats survivalStats;
        private EquipmentController equipment;

        [Header("Drop Visual Fallback")]
        [SerializeField] private Shader dropGhostShader;
        private static Shader s_cachedDropGhostShader;

        // Consecutive drop spacing — keep rapid drops from stacking on one spot.
        private Vector3 lastDropSettledPosition;
        private Vector3 lastDropPlayerPosition;
        private float lastDropTime = -999f;
        private int dropBurstIndex;
        private const float DropSpacingResetSeconds = 2.5f;
        private const float DropSpacingResetPlayerDistance = 3.5f;
        private const float DropSpacingStep = 0.55f;

        private void Awake()
        {
            if (slots == null)
                slots = new List<InventorySlot>();

            EnsureSlotCounts(inventorySize, hotbarSize, toolbarSize, unlockedMainSlots);

            survivalStats = GetComponent<SurvivalStats>();
            equipment = GetComponent<EquipmentController>();
            CacheDropGhostShader();
        }

        private void OnEnable()
        {
            CacheDropGhostShader();
        }

        private void CacheDropGhostShader()
        {
            if (dropGhostShader == null)
                dropGhostShader = FindDropGhostShader();
            if (dropGhostShader != null)
                s_cachedDropGhostShader = dropGhostShader;
        }

        private static Shader FindDropGhostShader()
        {
            return Shader.Find("HDRP/Lit")
                ?? Shader.Find("HDRP/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Standard");
        }

        private static Shader ResolveDropGhostShader()
        {
            if (s_cachedDropGhostShader != null)
                return s_cachedDropGhostShader;
            s_cachedDropGhostShader = FindDropGhostShader();
            return s_cachedDropGhostShader;
        }

        public void EnsureSlotCounts(int mainSize, int hotbar, int toolbar, int unlockedMain = -1)
        {
            inventorySize = Mathf.Max(1, mainSize);
            hotbarSize = Mathf.Max(0, hotbar);
            toolbarSize = Mathf.Max(0, toolbar);

            if (unlockedMain >= 0)
                unlockedMainSlots = unlockedMain;

            unlockedMainSlots = Mathf.Clamp(unlockedMainSlots, 0, inventorySize);

            int totalSize = inventorySize + hotbarSize + toolbarSize;
            while (slots.Count < totalSize)
                slots.Add(new InventorySlot());

            for (int i = totalSize; i < slots.Count; i++)
            {
                slots[i].item = null;
                slots[i].amount = 0;
            }

            while (slots.Count > totalSize)
                slots.RemoveAt(slots.Count - 1);

            // Locked slots cannot hold items — clear anything that ended up past the unlock edge.
            for (int i = unlockedMainSlots; i < inventorySize; i++)
            {
                slots[i].item = null;
                slots[i].amount = 0;
            }
        }

        public bool IsMainSlotUnlocked(int index)
        {
            if (index < 0)
                return false;

            if (index >= inventorySize)
                return true; // Hotbar / toolbar are always usable.

            return index < unlockedMainSlots;
        }

        public bool CanUnlockNextStorageRow()
        {
            return unlockedMainSlots < inventorySize;
        }

        /// <summary>Unlocks the next storage row (10 slots). Returns false when fully expanded.</summary>
        public bool TryUnlockNextStorageRow()
        {
            if (!CanUnlockNextStorageRow())
                return false;

            unlockedMainSlots = Mathf.Min(inventorySize, unlockedMainSlots + StorageRowSlotCount);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool CanAcceptItemAt(int index, ItemData item, bool showLevelToast = false)
        {
            if (item == null || index < 0 || index >= slots.Count)
                return false;

            if (!IsMainSlotUnlocked(index))
                return false;

            if (equipment != null)
                return equipment.CanPlaceItemAt(index, item, showLevelToast);

            if (IsToolbarIndex(index))
            {
                if (item.itemType != ItemType.Tool)
                    return false;
                return LevelUnlockUtility.PassesEquipGate(item, showToast: showLevelToast);
            }

            return true;
        }

        private bool CanMoveBetweenSlots(int fromIndex, int toIndex)
        {
            InventorySlot from = slots[fromIndex];
            if (from.IsEmpty || from.item == null)
                return false;

            if (!CanAcceptItemAt(toIndex, from.item))
                return false;

            InventorySlot to = slots[toIndex];
            if (!to.IsEmpty && to.item != from.item && !CanAcceptItemAt(fromIndex, to.item))
                return false;

            return true;
        }

        /// <returns>Number of items successfully added.</returns>
        public int AddItem(ItemData item, int amount = 1, bool autoCreditAmmoToWeapons = true)
        {
            if (item == null || amount <= 0) return 0;

            if (autoCreditAmmoToWeapons && EquipmentController.IsAmmoItem(item))
            {
                WeaponAmmoState ammoState = GetComponent<WeaponAmmoState>();
                if (ammoState != null)
                {
                    ammoState.CreditAmmoPickup(item, amount);
                    OnInventoryChanged?.Invoke();
                    return amount;
                }
            }

            int remaining = amount;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].item == item && slots[i].amount < item.maxStack && CanAcceptItemAt(i, item))
                {
                    int canAdd = Mathf.Min(remaining, item.maxStack - slots[i].amount);
                    slots[i].amount += canAdd;
                    remaining -= canAdd;
                    if (remaining <= 0) break;
                }
            }

            if (remaining > 0 && equipment != null && item.IsEquippable)
                remaining = TryAddRemainingToEquipSlots(item, remaining);

            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                if (slots[i].IsEmpty && CanAcceptItemAt(i, item))
                {
                    slots[i].item = item;
                    int canAdd = Mathf.Min(remaining, item.maxStack);
                    slots[i].amount = canAdd;
                    remaining -= canAdd;
                }
            }

            int added = amount - remaining;
            if (added > 0)
                OnInventoryChanged?.Invoke();

            return added;
        }

        private int TryAddRemainingToEquipSlots(ItemData item, int remaining)
        {
            if (equipment == null || remaining <= 0 || item == null)
                return remaining;

            if (EquipmentController.IsWeaponItem(item))
            {
                while (remaining > 0)
                {
                    int hotbarSlot = equipment.FindFirstEmptyWeaponHotbarSlot();
                    if (hotbarSlot < 0)
                        break;

                    int absolute = inventorySize + hotbarSlot;
                    if (!CanAcceptItemAt(absolute, item) || !slots[absolute].IsEmpty)
                        break;

                    slots[absolute].item = item;
                    int canAdd = Mathf.Min(remaining, item.maxStack);
                    slots[absolute].amount = canAdd;
                    remaining -= canAdd;
                }

                return remaining;
            }

            if (item.itemType == ItemType.Tool)
            {
                while (remaining > 0)
                {
                    int toolbarSlot = equipment.FindFirstEmptyToolbarSlot(item);
                    if (toolbarSlot < 0)
                        break;

                    int absolute = ToolbarStartIndex + toolbarSlot;
                    if (!CanAcceptItemAt(absolute, item) || !slots[absolute].IsEmpty)
                        break;

                    slots[absolute].item = item;
                    int canAdd = Mathf.Min(remaining, item.maxStack);
                    slots[absolute].amount = canAdd;
                    remaining -= canAdd;
                }
            }

            return remaining;
        }

        public int CountItem(ItemData item)
        {
            if (item == null)
                return 0;

            int count = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].item == item)
                    count += slots[i].amount;
            }

            return count;
        }

        public bool HasSpaceFor(ItemData item, int amount)
        {
            if (item == null || amount <= 0)
                return false;

            int remaining = amount;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].item == item && slots[i].amount < item.maxStack && CanAcceptItemAt(i, item))
                {
                    remaining -= Mathf.Min(remaining, item.maxStack - slots[i].amount);
                    if (remaining <= 0)
                        return true;
                }
            }

            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                if (slots[i].IsEmpty && CanAcceptItemAt(i, item))
                    remaining -= Mathf.Min(remaining, item.maxStack);
            }

            return remaining <= 0;
        }

        public bool HasSpaceInMainInventory(ItemData item, int amount)
        {
            if (item == null || amount <= 0)
                return false;

            int remaining = amount;

            for (int i = 0; i < inventorySize; i++)
            {
                if (slots[i].item == item && slots[i].amount < item.maxStack && CanAcceptItemAt(i, item))
                {
                    remaining -= Mathf.Min(remaining, item.maxStack - slots[i].amount);
                    if (remaining <= 0)
                        return true;
                }
            }

            for (int i = 0; i < inventorySize && remaining > 0; i++)
            {
                if (slots[i].IsEmpty && CanAcceptItemAt(i, item))
                    remaining -= Mathf.Min(remaining, item.maxStack);
            }

            return remaining <= 0;
        }

        /// <returns>Number of items successfully added to main inventory slots.</returns>
        public int AddItemToMainInventory(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0)
                return 0;

            int remaining = amount;

            for (int i = 0; i < inventorySize; i++)
            {
                if (slots[i].item == item && slots[i].amount < item.maxStack && CanAcceptItemAt(i, item))
                {
                    int canAdd = Mathf.Min(remaining, item.maxStack - slots[i].amount);
                    slots[i].amount += canAdd;
                    remaining -= canAdd;
                    if (remaining <= 0)
                        break;
                }
            }

            for (int i = 0; i < inventorySize && remaining > 0; i++)
            {
                if (slots[i].IsEmpty && CanAcceptItemAt(i, item))
                {
                    slots[i].item = item;
                    int canAdd = Mathf.Min(remaining, item.maxStack);
                    slots[i].amount = canAdd;
                    remaining -= canAdd;
                }
            }

            int added = amount - remaining;
            if (added > 0)
                OnInventoryChanged?.Invoke();

            return added;
        }

        public bool TryConsumeItemById(string itemId, int amount = 1)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0)
                return false;

            ItemData item = ItemRegistry.Resolve(itemId);
            return item != null && RemoveItem(item, amount);
        }

        public bool RemoveItem(ItemData item, int amount = 1)
        {
            if (item == null) return false;

            int remaining = amount;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].item != item) continue;

                if (slots[i].amount >= remaining)
                {
                    slots[i].amount -= remaining;
                    if (slots[i].amount <= 0)
                    {
                        slots[i].item = null;
                        slots[i].amount = 0;
                    }
                    OnInventoryChanged?.Invoke();
                    return true;
                }

                remaining -= slots[i].amount;
                slots[i].item = null;
                slots[i].amount = 0;
            }

            if (remaining < amount)
                OnInventoryChanged?.Invoke();

            return false;
        }

        public void SwapSlots(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || toIndex < 0 || fromIndex >= slots.Count || toIndex >= slots.Count) return;
            if (fromIndex == toIndex) return;

            var temp = slots[fromIndex];
            slots[fromIndex] = slots[toIndex];
            slots[toIndex] = temp;

            OnInventoryChanged?.Invoke();
        }

        public void MoveOrMergeSlots(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || toIndex < 0 || fromIndex >= slots.Count || toIndex >= slots.Count) return;
            if (fromIndex == toIndex) return;
            if (!CanMoveBetweenSlots(fromIndex, toIndex)) return;

            var from = slots[fromIndex];
            var to = slots[toIndex];
            if (from.IsEmpty) return;

            if (!to.IsEmpty && from.item == to.item)
            {
                int total = from.amount + to.amount;
                int maxStack = from.item.maxStack;

                if (total <= maxStack)
                {
                    to.amount = total;
                    from.item = null;
                    from.amount = 0;
                }
                else
                {
                    to.amount = maxStack;
                    from.amount = total - maxStack;
                }

                OnInventoryChanged?.Invoke();
            }
            else
            {
                SwapSlots(fromIndex, toIndex);
            }
        }

        public bool SplitStackAt(int index)
        {
            if (index < 0 || index >= slots.Count) return false;

            var source = slots[index];
            if (source.IsEmpty || source.amount <= 1) return false;

            int emptyIndex = FindFirstEmptySlotIndex();
            if (emptyIndex < 0) return false;

            int splitAmount = source.amount / 2;
            source.amount -= splitAmount;
            slots[emptyIndex].item = source.item;
            slots[emptyIndex].amount = splitAmount;

            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveItemAt(int index, int amount = 1)
        {
            if (index < 0 || index >= slots.Count) return false;
            var slot = slots[index];
            if (slot.IsEmpty) return false;

            if (slot.amount >= amount)
            {
                slot.amount -= amount;
                if (slot.amount <= 0)
                {
                    slot.item = null;
                    slot.amount = 0;
                }
                OnInventoryChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool UseItemAt(int index)
        {
            if (index < 0 || index >= slots.Count) return false;
            var slot = slots[index];
            if (slot.IsEmpty || slot.item == null) return false;

            if (!LevelUnlockUtility.PassesUseGate(slot.item, showToast: true))
                return false;

            if (survivalStats == null)
                survivalStats = GetComponent<SurvivalStats>();

            if (survivalStats != null && slot.item.IsConsumable)
            {
                ItemData consumed = slot.item;
                survivalStats.Consume(consumed);
                RemoveItemAt(index, 1);
                consumed.TryGrantConfiguredXp();
                return true;
            }
            return false;
        }

        public bool DropItemAt(int index, int amount = -1)
        {
            if (index < 0 || index >= slots.Count)
                return false;

            InventorySlot slot = slots[index];
            if (slot.IsEmpty || slot.item == null)
                return false;

            int dropAmount = amount < 0 ? slot.amount : Mathf.Clamp(amount, 1, slot.amount);
            if (!SpawnDroppedItem(slot.item, dropAmount))
                return false;

            RemoveItemAt(index, dropAmount);
            return true;
        }

        private bool SpawnDroppedItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0)
                return false;

            const float dropDistance = 1.15f;
            Vector3 dropHint = ResolveDropHintPosition(dropDistance);

            GameObject prefab = ResolveWorldDropPrefab(item);
            GameObject droppedObject = prefab != null
                ? Instantiate(prefab, dropHint, Quaternion.identity)
                : CreateFallbackDropVisual(item, dropHint);

            // Keep meshes/materials/renderer enabled flags identical to the pickup prefab.
            if (prefab != null)
                RestorePrefabVisualState(droppedObject, prefab);

            StripNonPickupBehaviours(droppedObject);

            // ItemPickup has [RequireComponent(typeof(Collider))]. Incomplete world
            // prefabs (MeshFilter+MeshRenderer only, e.g. box2) fail AddComponent and
            // leave a ghost drop. Fit a trigger collider before attaching pickup.
            EnsureDroppedPhysicsAndPickup(droppedObject);

            ItemPickup pickup = droppedObject.GetComponent<ItemPickup>();
            if (pickup == null)
                pickup = droppedObject.GetComponentInChildren<ItemPickup>();
            if (pickup == null)
            {
                if (droppedObject.GetComponent<Collider>() == null
                    && droppedObject.GetComponentInChildren<Collider>(true) == null)
                {
                    SphereCollider trigger = droppedObject.AddComponent<SphereCollider>();
                    trigger.isTrigger = true;
                    trigger.radius = 0.45f;
                }

                pickup = droppedObject.AddComponent<ItemPickup>();
            }

            if (pickup == null)
            {
                Object.Destroy(droppedObject);
                return false;
            }

            pickup.PrepareForWorldDrop(item, amount);

            // Re-assert after pickup prep so PrepareForWorldDrop cannot drift visuals.
            if (prefab != null)
                RestorePrefabVisualState(droppedObject, prefab);

            SettleDroppedItemOnTerrain(droppedObject, dropHint);
            RememberDropPosition(droppedObject.transform.position);
            return true;
        }

        private static GameObject ResolveWorldDropPrefab(ItemData item)
        {
            if (item == null)
                return null;

            // Always prefer the authored world/pickup prefab so drops match scene pickups
            // (World/*_World, Ammo/*_Pickup, weapon world shells, etc.).
            if (item.worldPrefab != null)
                return item.worldPrefab;

            // Held meshes are often grip-scaled / stripped for hands — only use when they are
            // not an explicit *_Held variant (those rarely match pickup look).
            if (item.heldPrefab != null && !IsHandOnlyHeldPrefab(item.heldPrefab))
                return item.heldPrefab;

            // Weapons sometimes only author the Invector equip prefab — still better than a cube.
            if (item.invectorWeaponPrefab != null)
                return item.invectorWeaponPrefab;

            // Last resort: held grip mesh when no world/pickup exists.
            if (item.heldPrefab != null)
                return item.heldPrefab;

            return null;
        }

        private static bool IsHandOnlyHeldPrefab(GameObject prefab)
        {
            if (prefab == null)
                return false;

            string name = prefab.name;
            return name.EndsWith("_Held", System.StringComparison.OrdinalIgnoreCase)
                || name.EndsWith("_Hand", System.StringComparison.OrdinalIgnoreCase);
        }


        private static GameObject CreateFallbackDropVisual(ItemData item, Vector3 position)
        {
            GameObject droppedObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            droppedObject.name = $"Dropped_{item.itemName}";
            droppedObject.transform.position = position;
            droppedObject.transform.localScale = Vector3.one * 0.28f;

            Collider primitiveCollider = droppedObject.GetComponent<Collider>();
            if (primitiveCollider != null)
                primitiveCollider.isTrigger = true;

            if (item.icon != null)
            {
                Renderer renderer = droppedObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Shader shader = ResolveDropGhostShader();
                    if (shader != null)
                    {
                        Material material = new Material(shader);
                        if (material.HasProperty("_BaseColorMap"))
                            material.SetTexture("_BaseColorMap", item.icon.texture);
                        else if (material.HasProperty("_BaseMap"))
                            material.SetTexture("_BaseMap", item.icon.texture);
                        else
                            material.mainTexture = item.icon.texture;
                        material.color = Color.white;
                        if (material.HasProperty("_BaseColor"))
                            material.SetColor("_BaseColor", Color.white);
                        if (material.HasProperty("_UnlitColor"))
                            material.SetColor("_UnlitColor", Color.white);
                        renderer.sharedMaterial = material;
                    }
                }
            }

            return droppedObject;
        }

private static void StripNonPickupBehaviours(GameObject droppedObject)
        {
            if (droppedObject == null)
                return;

            // Strip combat/runtime behaviours only — never touch MeshFilters, Renderers, or materials.
            CombatProjectile[] projectiles =
                droppedObject.GetComponentsInChildren<CombatProjectile>(true);
            for (int i = 0; i < projectiles.Length; i++)
                DestroyDroppedComponent(projectiles[i]);

            WeaponHitbox[] hitboxes =
                droppedObject.GetComponentsInChildren<WeaponHitbox>(true);
            for (int i = 0; i < hitboxes.Length; i++)
                DestroyDroppedComponent(hitboxes[i]);

            EquippedVisualMarker[] markers =
                droppedObject.GetComponentsInChildren<EquippedVisualMarker>(true);
            for (int i = 0; i < markers.Length; i++)
                DestroyDroppedComponent(markers[i]);

            ResourceNode[] nodes = droppedObject.GetComponentsInChildren<ResourceNode>(true);
            for (int i = 0; i < nodes.Length; i++)
                DestroyDroppedComponent(nodes[i]);

            Rigidbody[] bodies = droppedObject.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];
                if (body == null || body.gameObject == droppedObject)
                    continue;
                DestroyDroppedComponent(body);
            }
        }

private static void DestroyDroppedComponent(Object component)
        {
            if (component == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(component);
            else
                Object.DestroyImmediate(component);
        }


        private Vector3 ResolveDropHintPosition(float dropDistance)
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;
            forward.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, forward);
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.right;
            right.Normalize();

            Vector3 hint = transform.position + forward * dropDistance;

            float now = Time.time;
            bool resetBurst =
                dropBurstIndex <= 0
                || (now - lastDropTime) > DropSpacingResetSeconds
                || Vector3.Distance(transform.position, lastDropPlayerPosition)
                    > DropSpacingResetPlayerDistance;

            if (resetBurst)
            {
                dropBurstIndex = 0;
                lastDropPlayerPosition = transform.position;
            }
            else
            {
                // Spiral / ring around the last settled drop so stacks fan out.
                float angleRad = dropBurstIndex * (70f * Mathf.Deg2Rad);
                float radius = DropSpacingStep * (1f + (dropBurstIndex - 1) * 0.12f);
                Vector3 ring =
                    right * (Mathf.Sin(angleRad) * radius)
                    + forward * (Mathf.Cos(angleRad) * radius * 0.35f);
                hint = lastDropSettledPosition + ring;

                // Keep the fan roughly in front of the player (don't walk behind).
                Vector3 fromPlayer = hint - transform.position;
                fromPlayer.y = 0f;
                float ahead = Vector3.Dot(fromPlayer, forward);
                if (ahead < dropDistance * 0.55f)
                    hint += forward * (dropDistance * 0.55f - ahead);
            }

            if (TryGetDropGroundY(hint, out float groundY))
                hint.y = groundY + 0.35f;
            else
                hint.y = transform.position.y + 0.35f;

            return hint;
        }

        private void RememberDropPosition(Vector3 settledPosition)
        {
            lastDropSettledPosition = settledPosition;
            lastDropTime = Time.time;
            lastDropPlayerPosition = transform.position;
            dropBurstIndex++;
        }

        private bool TryGetDropGroundY(Vector3 worldPosition, out float groundY)
        {
            groundY = worldPosition.y;

            float originY = worldPosition.y + 3f;
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                float terrainY = terrain.SampleHeight(worldPosition) + terrain.transform.position.y;
                originY = Mathf.Max(originY, terrainY + 4f);
            }

            Vector3 origin = new Vector3(worldPosition.x, originY, worldPosition.z);
            float rayLength = originY - (worldPosition.y - 8f);
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                Vector3.down,
                Mathf.Max(4f, rayLength),
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            float closestDistance = float.MaxValue;
            bool foundGround = false;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (IsIgnorableDropSurface(hitCollider))
                    continue;

                if (hits[i].distance >= closestDistance)
                    continue;

                closestDistance = hits[i].distance;
                groundY = hits[i].point.y;
                foundGround = true;
            }

            if (foundGround)
                return true;

            if (terrain != null)
            {
                groundY = terrain.SampleHeight(worldPosition) + terrain.transform.position.y;
                return true;
            }

            return false;
        }

        private bool IsIgnorableDropSurface(Collider hitCollider)
        {
            if (hitCollider == null || hitCollider.isTrigger)
                return true;

            if (hitCollider.CompareTag("Player"))
                return true;

            Transform hitTransform = hitCollider.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                return true;

            // Invector / ECM bodies often leave player colliders untagged.
            if (hitCollider.GetComponentInParent<InventorySystem>() == this)
                return true;

            int layer = hitCollider.gameObject.layer;
            if (layer == LayerMask.NameToLayer("Item")
                || layer == LayerMask.NameToLayer("Player")
                || layer == LayerMask.NameToLayer("Enemy")
                || layer == LayerMask.NameToLayer("CompanionAI")
                || layer == LayerMask.NameToLayer("Triggers")
                || layer == LayerMask.NameToLayer("BodyPart"))
                return true;

            return false;
        }

        private void SettleDroppedItemOnTerrain(GameObject droppedObject, Vector3 hintPosition)
        {
            if (droppedObject == null)
                return;

            Vector3 position = droppedObject.transform.position;
            position.x = hintPosition.x;
            position.z = hintPosition.z;

            if (!TryGetDropGroundY(position, out float groundY))
                groundY = hintPosition.y;

            Bounds bounds = CalculateRendererBounds(droppedObject);
            float bottomOffset = 0.05f;
            if (bounds.size.sqrMagnitude > 0.0001f)
                bottomOffset = Mathf.Max(0.02f, position.y - bounds.min.y);

            position.y = groundY + bottomOffset;
            droppedObject.transform.position = position;

            // Sit like scene pickups — no bounce / fall-through.
            // Unity 6 errors if linear/angular velocity is written while kinematic, so park
            // without touching velocities (fresh Rigidbodies are already at rest).
            Rigidbody body = droppedObject.GetComponent<Rigidbody>();
            if (body == null)
                body = droppedObject.AddComponent<Rigidbody>();

            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.useGravity = false;
            body.detectCollisions = true;
            body.isKinematic = true;
        }

        private static Bounds CalculateRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = new Bounds(root.transform.position, Vector3.zero);
            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
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

            return bounds;
        }

        /// <summary>
        /// Copies renderer enabled flags + sharedMaterials from the source world prefab so drops
        /// stay identical to scene pickups (nested prefab shells, authored materials, etc.).
        /// </summary>
/// <summary>
        /// Copies mesh + renderer enabled flags + sharedMaterials from the source world prefab so
        /// every inventory drop stays identical to scene pickups (nested prefab shells, authored
        /// materials, intentionally disabled shell meshes, ammo FBX scales, weapons, etc.).
        /// Does not force-enable renderers — restores prefab state only.
        /// </summary>
        private static void RestorePrefabVisualState(GameObject instance, GameObject prefab)
        {
            if (instance == null || prefab == null)
                return;

            // Preserve authored root scale (tiny FBX ammo uses large scale; Plasma Fuel uses 0.5).
            instance.transform.localScale = prefab.transform.localScale;

            MeshFilter[] prefabFilters = prefab.GetComponentsInChildren<MeshFilter>(true);
            MeshFilter[] instanceFilters = instance.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < prefabFilters.Length; i++)
            {
                MeshFilter prefabFilter = prefabFilters[i];
                if (prefabFilter == null)
                    continue;

                MeshFilter instanceFilter = FindMatchingComponent(
                    prefab.transform,
                    instance.transform,
                    prefabFilter,
                    instanceFilters,
                    i);
                if (instanceFilter != null && prefabFilter.sharedMesh != null)
                    instanceFilter.sharedMesh = prefabFilter.sharedMesh;
            }

            SkinnedMeshRenderer[] prefabSkinned =
                prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            SkinnedMeshRenderer[] instanceSkinned =
                instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < prefabSkinned.Length; i++)
            {
                SkinnedMeshRenderer prefabSkin = prefabSkinned[i];
                if (prefabSkin == null)
                    continue;

                SkinnedMeshRenderer instanceSkin = FindMatchingComponent(
                    prefab.transform,
                    instance.transform,
                    prefabSkin,
                    instanceSkinned,
                    i);
                if (instanceSkin != null && prefabSkin.sharedMesh != null)
                    instanceSkin.sharedMesh = prefabSkin.sharedMesh;
            }

            Renderer[] prefabRenderers = prefab.GetComponentsInChildren<Renderer>(true);
            Renderer[] instanceRenderers = instance.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < prefabRenderers.Length; i++)
            {
                Renderer prefabRenderer = prefabRenderers[i];
                if (prefabRenderer == null)
                    continue;

                Renderer instanceRenderer = FindMatchingRenderer(
                    prefab.transform,
                    instance.transform,
                    prefabRenderer,
                    instanceRenderers,
                    i);
                if (instanceRenderer == null)
                    continue;

                instanceRenderer.enabled = prefabRenderer.enabled;

                Material[] shared = prefabRenderer.sharedMaterials;
                if (shared != null && shared.Length > 0)
                {
                    // Assign a copy so nested PrefabInstance overrides stick on the clone.
                    Material[] copy = new Material[shared.Length];
                    for (int m = 0; m < shared.Length; m++)
                        copy[m] = shared[m];
                    instanceRenderer.sharedMaterials = copy;
                }
            }

            // Pulse/scroll drivers cache material slots in Awake — refresh after material restore.
            DMIMaterialPulseScroll[] pulses =
                instance.GetComponentsInChildren<DMIMaterialPulseScroll>(true);
            for (int i = 0; i < pulses.Length; i++)
            {
                if (pulses[i] != null)
                    pulses[i].RebuildCaches();
            }
        }

private static Renderer FindMatchingRenderer(
            Transform prefabRoot,
            Transform instanceRoot,
            Renderer prefabRenderer,
            Renderer[] instanceRenderers,
            int prefabIndex)
        {
            return FindMatchingComponent(
                prefabRoot,
                instanceRoot,
                prefabRenderer,
                instanceRenderers,
                prefabIndex);
        }

private static T FindMatchingComponent<T>(
            Transform prefabRoot,
            Transform instanceRoot,
            T prefabComponent,
            T[] instanceComponents,
            int prefabIndex)
            where T : Component
        {
            if (prefabComponent == null)
                return null;

            string relativePath = GetRelativeTransformPath(prefabRoot, prefabComponent.transform);
            Transform instanceTransform = string.IsNullOrEmpty(relativePath)
                ? instanceRoot
                : instanceRoot.Find(relativePath);
            if (instanceTransform != null)
            {
                T byPath = instanceTransform.GetComponent<T>();
                if (byPath != null)
                    return byPath;
            }

            // Nested PrefabInstance path quirks — fall back to same DFS index / name.
            if (instanceComponents != null
                && prefabIndex >= 0
                && prefabIndex < instanceComponents.Length
                && instanceComponents[prefabIndex] != null
                && instanceComponents[prefabIndex].name == prefabComponent.name)
            {
                return instanceComponents[prefabIndex];
            }

            if (instanceComponents == null)
                return null;

            for (int i = 0; i < instanceComponents.Length; i++)
            {
                T candidate = instanceComponents[i];
                if (candidate != null && candidate.name == prefabComponent.name)
                    return candidate;
            }

            return null;
        }


        private static string GetRelativeTransformPath(Transform root, Transform target)
        {
            if (root == null || target == null)
                return string.Empty;

            if (target == root)
                return string.Empty;

            System.Text.StringBuilder path = new System.Text.StringBuilder(target.name);
            Transform current = target.parent;
            while (current != null && current != root)
            {
                path.Insert(0, "/");
                path.Insert(0, current.name);
                current = current.parent;
            }

            return current == root ? path.ToString() : string.Empty;
        }

        private static void EnsureDroppedPhysicsAndPickup(GameObject droppedObject)
        {
            droppedObject.SetActive(true);

            Transform rootTransform = droppedObject.transform;
            if (rootTransform.localScale.sqrMagnitude < 0.0001f)
                rootTransform.localScale = Vector3.one;

            // Preserve prefab collider/renderer enabled flags — only ensure a pickup trigger exists.
            Collider[] colliders = droppedObject.GetComponentsInChildren<Collider>(true);
            bool hasTriggerCollider = false;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider != null && collider.enabled && collider.isTrigger)
                {
                    hasTriggerCollider = true;
                    break;
                }
            }

            // World pickups are trigger-only interactables; keep that contract for drops.
            if (!hasTriggerCollider)
            {
                Bounds bounds = CalculateRendererBounds(droppedObject);
                SphereCollider triggerCollider = droppedObject.AddComponent<SphereCollider>();
                triggerCollider.isTrigger = true;
                float radius = bounds.size.sqrMagnitude > 0.0001f
                    ? Mathf.Clamp(Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.15f, 0.25f, 0.85f)
                    : 0.45f;
                triggerCollider.radius = radius;
                if (bounds.size.sqrMagnitude > 0.0001f)
                    triggerCollider.center = droppedObject.transform.InverseTransformPoint(bounds.center);
            }

            int itemLayer = LayerMask.NameToLayer("Item");
            if (itemLayer >= 0)
                SetLayerRecursively(droppedObject, itemLayer);
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            if (target == null)
                return;

            target.layer = layer;
            Transform root = target.transform;
            for (int i = 0; i < root.childCount; i++)
                SetLayerRecursively(root.GetChild(i).gameObject, layer);
        }

        public int HotbarStartIndex => inventorySize;

        public int ToolbarStartIndex => inventorySize + hotbarSize;

        public bool IsHotbarIndex(int index)
        {
            return index >= HotbarStartIndex && index < ToolbarStartIndex;
        }

        public bool IsToolbarIndex(int index)
        {
            return index >= ToolbarStartIndex && index < ToolbarStartIndex + toolbarSize;
        }

        public int ToToolbarSlotIndex(int absoluteIndex)
        {
            return absoluteIndex - ToolbarStartIndex;
        }

        public ItemData GetItemAt(int index)
        {
            if (index < 0 || index >= slots.Count) return null;

            InventorySlot slot = slots[index];
            return slot == null || slot.IsEmpty ? null : slot.item;
        }

        private int FindFirstEmptySlotIndex()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty && IsMainSlotUnlocked(i))
                    return i;
            }
            return -1;
        }

        public void ClearAllSlots()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].item = null;
                slots[i].amount = 0;
            }
        }

        public void NotifyInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            if (slots == null)
                slots = new List<InventorySlot>();

            EnsureSlotCounts(inventorySize, hotbarSize, toolbarSize, unlockedMainSlots);
        }
#endif
    }
}

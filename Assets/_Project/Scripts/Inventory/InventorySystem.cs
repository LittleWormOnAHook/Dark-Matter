using System.Collections.Generic;
using Project.Data;
using Project.Interaction;
using Project.Survival;
using UnityEngine;

namespace Project.Inventory
{
    public class InventorySystem : MonoBehaviour
    {
        [Header("Inventory Settings")]
        public int inventorySize = 24;
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

        private void Awake()
        {
            if (slots == null)
                slots = new List<InventorySlot>();

            EnsureSlotCounts(inventorySize, hotbarSize, toolbarSize);

            survivalStats = GetComponent<SurvivalStats>();
            equipment = GetComponent<EquipmentController>();
        }

        public void EnsureSlotCounts(int mainSize, int hotbar, int toolbar)
        {
            inventorySize = Mathf.Max(1, mainSize);
            hotbarSize = Mathf.Max(0, hotbar);
            toolbarSize = Mathf.Max(0, toolbar);

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
        }

        public bool CanAcceptItemAt(int index, ItemData item)
        {
            if (item == null || index < 0 || index >= slots.Count)
                return false;

            if (equipment != null)
                return equipment.CanPlaceItemAt(index, item);

            if (IsToolbarIndex(index))
                return item.itemType == ItemType.Tool;

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
        public int AddItem(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return 0;

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

            if (survivalStats == null)
                survivalStats = GetComponent<SurvivalStats>();

            if (survivalStats != null && slot.item.IsConsumable)
            {
                survivalStats.Consume(slot.item);
                RemoveItemAt(index, 1);
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

            const float dropDistance = 1f;
            Vector3 dropPosition = ResolveDropPosition(dropDistance);

            GameObject droppedObject;
            if (item.worldPrefab != null)
            {
                droppedObject = Instantiate(item.worldPrefab, dropPosition, Quaternion.identity);
            }
            else
            {
                droppedObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                droppedObject.name = $"Dropped_{item.itemName}";
                droppedObject.transform.position = dropPosition;
                droppedObject.transform.localScale = Vector3.one * 0.35f;
            }

            ItemPickup pickup = droppedObject.GetComponent<ItemPickup>();
            if (pickup == null)
                pickup = droppedObject.AddComponent<ItemPickup>();

            pickup.PrepareForWorldDrop(item, amount);
            EnsureDroppedPhysicsAndPickup(droppedObject);

            Rigidbody body = droppedObject.GetComponent<Rigidbody>();
            if (body == null)
                body = droppedObject.AddComponent<Rigidbody>();

            body.isKinematic = false;
            body.detectCollisions = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.useGravity = true;
            body.WakeUp();
            return true;
        }

        private Vector3 ResolveDropPosition(float dropDistance)
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = Vector3.forward;

            forward.Normalize();
            Vector3 probeOrigin = transform.position + forward * dropDistance + Vector3.up * 1.5f;

            RaycastHit[] hits = Physics.RaycastAll(
                probeOrigin,
                Vector3.down,
                4f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            float closestDistance = float.MaxValue;
            Vector3 closestPoint = default;
            bool foundGround = false;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider == null || hitCollider.CompareTag("Player"))
                    continue;

                if (hits[i].distance >= closestDistance)
                    continue;

                closestDistance = hits[i].distance;
                closestPoint = hits[i].point;
                foundGround = true;
            }

            if (foundGround)
                return closestPoint + Vector3.up * 0.08f;

            return transform.position + forward * dropDistance + Vector3.up * 0.2f;
        }

        private static void EnsureDroppedPhysicsAndPickup(GameObject droppedObject)
        {
            droppedObject.SetActive(true);

            Transform rootTransform = droppedObject.transform;
            if (rootTransform.localScale.sqrMagnitude < 0.0001f)
                rootTransform.localScale = Vector3.one;

            Renderer[] renderers = droppedObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].enabled = true;
            }

            Collider[] colliders = droppedObject.GetComponentsInChildren<Collider>(true);
            bool hasPhysicsCollider = false;
            bool hasTriggerCollider = false;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                    continue;

                collider.enabled = true;
                if (collider.isTrigger)
                    hasTriggerCollider = true;
                else
                    hasPhysicsCollider = true;
            }

            if (!hasPhysicsCollider)
            {
                SphereCollider physicsCollider = droppedObject.AddComponent<SphereCollider>();
                physicsCollider.radius = 0.28f;
                physicsCollider.isTrigger = false;
            }

            if (!hasTriggerCollider)
            {
                SphereCollider triggerCollider = droppedObject.AddComponent<SphereCollider>();
                triggerCollider.radius = 0.45f;
                triggerCollider.isTrigger = true;
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
                if (slots[i].IsEmpty)
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

            EnsureSlotCounts(inventorySize, hotbarSize, toolbarSize);
        }
#endif
    }
}

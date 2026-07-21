using Project.Data;
using Project.Inventory;
using UnityEngine;

namespace Project.Vehicles
{
    /// <summary>
    /// Shared "Store in Inventory" / "Deploy" logic used by both the world-space hovercraft interact
    /// menu (HovercraftInteractMenuUI) and the inventory right-click context menu
    /// (InventoryItemActions), so the two entry points can't drift out of sync.
    /// </summary>
    public static class HovercraftDeploymentUtility
    {
        public static bool TryStore(HovercraftController craft, InventorySystem inventory, ItemData hovercraftItem, out string message)
        {
            message = string.Empty;

            if (craft == null)
            {
                message = "No hovercraft to store.";
                return false;
            }

            if (PlayerVehicleState.ActiveCraft == craft && PlayerVehicleState.IsMounted)
            {
                message = "Exit the hovercraft before storing it.";
                return false;
            }

            if (hovercraftItem == null || inventory == null)
            {
                message = "Hovercraft storage is not configured.";
                return false;
            }

            HovercraftFuelSystem fuel = craft.GetComponent<HovercraftFuelSystem>();
            HovercraftHealth health = craft.GetComponent<HovercraftHealth>();

            float fuelAmount = fuel != null ? fuel.CurrentFuel : HovercraftStorageState.DefaultMaxFuel;
            float shieldAmount = health != null ? health.CurrentShield : 60f;
            float healthAmount = health != null ? health.CurrentHealth : 120f;

            int added = inventory.AddItem(hovercraftItem, 1);
            if (added <= 0)
            {
                message = "Inventory is full — cannot store the hovercraft.";
                return false;
            }

            HovercraftStorageState.SetStoredState(fuelAmount, shieldAmount, healthAmount);
            Object.Destroy(craft.gameObject);
            message = "Hovercraft stored in inventory.";
            return true;
        }

        public static bool TryDeploy(InventorySystem inventory, ItemData hovercraftItem, Transform playerTransform, out string message)
        {
            message = string.Empty;

            if (inventory == null || hovercraftItem == null || playerTransform == null)
            {
                message = "Cannot deploy the hovercraft right now.";
                return false;
            }

            if (inventory.CountItem(hovercraftItem) <= 0)
            {
                message = "No stored hovercraft to deploy.";
                return false;
            }

            if (hovercraftItem.deployedPrefab == null)
            {
                message = "Hovercraft prefab is not configured on the item.";
                return false;
            }

            Vector3 flatForward = playerTransform.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.0001f)
                flatForward = Vector3.forward;
            flatForward.Normalize();

            Vector3 spawnPosition = playerTransform.position + flatForward * 4f;
            Quaternion spawnRotation = Quaternion.LookRotation(flatForward, Vector3.up);

            GameObject instance = Object.Instantiate(hovercraftItem.deployedPrefab, spawnPosition, spawnRotation);
            ReactivateDeployedCraft(instance);

            HovercraftFuelSystem fuel = instance.GetComponent<HovercraftFuelSystem>();
            fuel?.SetFuel(HovercraftStorageState.StoredFuel);

            HovercraftHealth health = instance.GetComponent<HovercraftHealth>();
            health?.SetState(HovercraftStorageState.StoredShield, HovercraftStorageState.StoredHealth, false);

            inventory.RemoveItem(hovercraftItem, 1);
            HovercraftStorageState.ClearStored();

            message = "Hovercraft deployed.";
            return true;
        }

        /// <summary>
        /// Belt-and-braces re-activation for a freshly deployed craft. A stored/instantiated prefab
        /// should already come back fully functional, but this guards against the GameObject or any
        /// of its core vehicle components ending up disabled (e.g. a stale disabled state baked into
        /// the source prefab, or a Rigidbody left sleeping/kinematic) so "Deploy" always hands back a
        /// drivable hovercraft.
        /// </summary>
        private static void ReactivateDeployedCraft(GameObject instance)
        {
            if (instance == null)
                return;

            if (!instance.activeSelf)
                instance.SetActive(true);

            HovercraftController controller = instance.GetComponent<HovercraftController>();
            if (controller != null)
                controller.enabled = true;

            HovercraftUsable usable = instance.GetComponent<HovercraftUsable>();
            if (usable != null)
                usable.enabled = true;

            HovercraftOccupancy occupancy = instance.GetComponent<HovercraftOccupancy>();
            if (occupancy != null)
                occupancy.enabled = true;

            HoverPhysicsDriver physicsDriver = instance.GetComponent<HoverPhysicsDriver>();
            if (physicsDriver != null)
                physicsDriver.enabled = true;

            Rigidbody rb = instance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.WakeUp();
            }
        }
    }
}

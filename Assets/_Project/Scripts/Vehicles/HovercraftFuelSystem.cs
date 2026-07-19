using System;
using Project.Data;
using Project.Inventory;
using UnityEngine;

namespace Project.Vehicles
{
    /// <summary>
    /// Plasma Fuel tank for the hovercraft. Drains while occupied (faster while boosting) and gates
    /// drive input to zero once empty — the craft stays passively hovering (spring suspension still
    /// holds it up) but can't be steered/climbed/boosted until refueled. Refueling consumes Plasma
    /// Fuel items from the player's inventory one unit at a time via HovercraftUsable's interact press.
    /// </summary>
    [DisallowMultipleComponent]
    public class HovercraftFuelSystem : MonoBehaviour
    {
        [SerializeField] private HovercraftController controller;
        [SerializeField] private HoverPhysicsDriver physicsDriver;

        [Header("Tank")]
        [SerializeField] private float maxFuel = 1000f;
        [SerializeField] private float startingFuel = 1000f;
        [Tooltip("Drain rate while occupied, in units per minute (10/min = 0.1667/sec).")]
        [SerializeField] private float fuelDrainPerMinute = 10f;
        [SerializeField] private float boostDrainMultiplier = 1.8f;

        [Header("Refuel")]
        [Tooltip("Plasma Fuel ItemData. Falls back to ItemRegistry.Resolve(\"Plasma Fuel\") when empty.")]
        [SerializeField] private ItemData plasmaFuelItem;
        [SerializeField] private float fuelPerPlasmaCell = 250f;

        private float currentFuel;

        public event Action<float, float> FuelChanged;

        public float CurrentFuel => currentFuel;
        public float MaxFuel => maxFuel;
        public float FuelPercent01 => maxFuel > 0f ? Mathf.Clamp01(currentFuel / maxFuel) : 0f;
        public bool IsEmpty => currentFuel <= 0.01f;
        public bool IsFull => currentFuel >= maxFuel - 0.01f;
        public ItemData PlasmaFuelItem => ResolvePlasmaFuelItem();

        private void Awake()
        {
            if (controller == null)
                controller = GetComponent<HovercraftController>();

            if (physicsDriver == null)
                physicsDriver = GetComponent<HoverPhysicsDriver>();

            currentFuel = Mathf.Clamp(startingFuel, 0f, maxFuel);
        }

        private void Update()
        {
            if (controller == null || !controller.IsOccupied || currentFuel <= 0f)
                return;

            float rate = fuelDrainPerMinute / 60f;
            if (physicsDriver != null && physicsDriver.BoosterActive)
                rate *= boostDrainMultiplier;

            float next = Mathf.Max(0f, currentFuel - rate * Time.deltaTime);
            if (!Mathf.Approximately(next, currentFuel))
            {
                currentFuel = next;
                FuelChanged?.Invoke(currentFuel, maxFuel);
            }
        }

        /// <summary>Zero out drive input entirely once the tank is dry — called from HovercraftController.</summary>
        public bool ShouldBlockDriveInput => IsEmpty;

        public bool HasPlasmaFuelIn(InventorySystem inventory)
        {
            ItemData item = ResolvePlasmaFuelItem();
            return inventory != null && item != null && inventory.CountItem(item) > 0;
        }

        /// <summary>
        /// Consumes one Plasma Fuel item from the player's inventory and tops the tank up. Returns
        /// false (no side effects) when the tank is already full or the player isn't carrying any —
        /// callers should fall through to a different interaction (e.g. boarding) in that case.
        /// </summary>
        public bool TryRefuelOneUnit(InventorySystem inventory, out string message)
        {
            message = string.Empty;
            if (IsFull)
                return false;

            ItemData item = ResolvePlasmaFuelItem();
            if (inventory == null || item == null || inventory.CountItem(item) <= 0)
                return false;

            inventory.RemoveItem(item, 1);
            currentFuel = Mathf.Min(maxFuel, currentFuel + fuelPerPlasmaCell);
            FuelChanged?.Invoke(currentFuel, maxFuel);
            message = $"Refueled hovercraft (+{Mathf.RoundToInt(fuelPerPlasmaCell)}) — {Mathf.RoundToInt(FuelPercent01 * 100f)}% fuel.";
            return true;
        }

        /// <summary>Directly sets the tank level — used by save/load to restore a persisted amount.</summary>
        public void SetFuel(float value)
        {
            currentFuel = Mathf.Clamp(value, 0f, maxFuel);
            FuelChanged?.Invoke(currentFuel, maxFuel);
        }

        private ItemData ResolvePlasmaFuelItem()
        {
            if (plasmaFuelItem != null)
                return plasmaFuelItem;

            return ItemRegistry.Resolve("Plasma Fuel");
        }
    }
}

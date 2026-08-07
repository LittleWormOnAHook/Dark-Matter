using System;
using System.Collections.Generic;
using Project.Data;
using Project.Inventory;
using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// Per-building Plasma Fuel generator. Independent per building (no shared grid) — drains its own
    /// tank continuously while it has fuel and reports HasPower to any PowerConsumer children (lights,
    /// devices). Crafting and Science Lab healing are NOT gated by this — only wired consumers lose
    /// power when the tank runs dry. Mirrors HovercraftFuelSystem's tank/refuel pattern at building scale.
    /// </summary>
    [DisallowMultipleComponent]
    public class PowerGenerator : MonoBehaviour
    {
        private static readonly List<PowerGenerator> ActiveInstances = new List<PowerGenerator>(8);

        public static IReadOnlyList<PowerGenerator> Active => ActiveInstances;

        [Header("Building")]
        [Tooltip("Resolved automatically from BuildingControlPanel.BuildingId when left blank.")]
        [SerializeField] private string buildingIdOverride;

        [Header("Tank")]
        [SerializeField] private float maxFuel = 100f;
        [SerializeField] private float startingFuel = 60f;
        [Tooltip("Fuel consumed per second while the generator is running (i.e. while it has any fuel).")]
        [SerializeField] private float fuelDrainPerSecond = 0.4f;

        [Header("Refuel")]
        [Tooltip("Plasma Fuel ItemData. Falls back to ItemRegistry.Resolve(\"Plasma Fuel\") when empty.")]
        [SerializeField] private ItemData plasmaFuelItem;
        [SerializeField] private float fuelPerPlasmaCell = 35f;

        private BuildingControlPanel controlPanel;
        private float currentFuel;
        private bool hadPowerLastFrame;

        public event Action<float, float> FuelChanged;
        public event Action<bool> PowerStateChanged;

        public string BuildingId => string.IsNullOrEmpty(buildingIdOverride)
            ? (controlPanel != null ? controlPanel.BuildingId : gameObject.name)
            : buildingIdOverride;

        public float CurrentFuel => currentFuel;
        public float MaxFuel => maxFuel;
        public float FuelPercent01 => maxFuel > 0f ? Mathf.Clamp01(currentFuel / maxFuel) : 0f;
        public bool HasPower => currentFuel > 0f;
        public bool IsFull => currentFuel >= maxFuel - 0.01f;
        public ItemData PlasmaFuelItem => ResolvePlasmaFuelItem();

        private void Awake()
        {
            controlPanel = GetComponent<BuildingControlPanel>();
            currentFuel = Mathf.Clamp(startingFuel, 0f, maxFuel);
            hadPowerLastFrame = HasPower;
        }

        private void OnEnable()
        {
            if (!ActiveInstances.Contains(this))
                ActiveInstances.Add(this);
        }

        private void OnDisable()
        {
            ActiveInstances.Remove(this);
        }

        private void Start()
        {
            // Let PowerConsumer children pick up the initial state even if they subscribed after Awake.
            PowerStateChanged?.Invoke(hadPowerLastFrame);
        }

        private void Update()
        {
            if (currentFuel <= 0f)
                return;

            float next = Mathf.Max(0f, currentFuel - fuelDrainPerSecond * Time.deltaTime);
            if (!Mathf.Approximately(next, currentFuel))
            {
                currentFuel = next;
                FuelChanged?.Invoke(currentFuel, maxFuel);
            }

            bool hasPowerNow = HasPower;
            if (hasPowerNow != hadPowerLastFrame)
            {
                hadPowerLastFrame = hasPowerNow;
                PowerStateChanged?.Invoke(hasPowerNow);
            }
        }

        public bool HasPlasmaFuelIn(InventorySystem inventory)
        {
            ItemData item = ResolvePlasmaFuelItem();
            return inventory != null && item != null && inventory.CountItem(item) > 0;
        }

        /// <summary>
        /// Consumes one Plasma Fuel item from the given inventory and tops the tank up. Returns false
        /// (no side effects) when the tank is already full or the inventory has none to spare.
        /// </summary>
        public bool TryRefuelOneUnit(InventorySystem inventory, out string message)
        {
            message = string.Empty;
            if (IsFull)
            {
                message = "Generator tank is already full.";
                return false;
            }

            ItemData item = ResolvePlasmaFuelItem();
            if (inventory == null || item == null || inventory.CountItem(item) <= 0)
            {
                message = "No Plasma Fuel to load.";
                return false;
            }

            inventory.RemoveItem(item, 1);
            bool poweredBefore = HasPower;
            currentFuel = Mathf.Min(maxFuel, currentFuel + fuelPerPlasmaCell);
            FuelChanged?.Invoke(currentFuel, maxFuel);

            if (!poweredBefore && HasPower)
            {
                hadPowerLastFrame = true;
                PowerStateChanged?.Invoke(true);
            }

            message = $"Loaded Plasma Fuel — generator at {Mathf.RoundToInt(FuelPercent01 * 100f)}%.";
            return true;
        }

        /// <summary>Directly sets the tank level — used by save/load to restore a persisted amount.</summary>
        public void SetFuel(float value)
        {
            currentFuel = Mathf.Clamp(value, 0f, maxFuel);
            FuelChanged?.Invoke(currentFuel, maxFuel);

            bool hasPowerNow = HasPower;
            if (hasPowerNow != hadPowerLastFrame)
            {
                hadPowerLastFrame = hasPowerNow;
                PowerStateChanged?.Invoke(hasPowerNow);
            }
        }

        private ItemData ResolvePlasmaFuelItem()
        {
            if (plasmaFuelItem != null)
                return plasmaFuelItem;

            return ItemRegistry.Resolve("Plasma Fuel");
        }
    }
}

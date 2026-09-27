using System;
using System.Collections.Generic;
using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Inventory;
using Project.Storage;
using Project.UI;
using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// 0926-generator: the placed base generator. It burns Plasma Fuel (1 unit every few minutes of gameplay, set in
    /// Building Studio > Placement) and, while it has fuel, powers the base square it stands in (see DMBasePower).
    /// Press E to open the fuel panel; refuelling takes Plasma Fuel from the inventory, then storage crates in range.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMBaseGenerator : MonoBehaviour, IWorldUsable
    {
        public const string FuelItemName = "Plasma Fuel";
        const float InteractRangeMeters = 3.5f;

        static readonly List<DMBaseGenerator> active = new List<DMBaseGenerator>(2);
        static readonly List<DMStorageCrate> cratesInRange = new List<DMStorageCrate>(8);
        static readonly HashSet<string> seenCrates = new HashSet<string>();

        public static IReadOnlyList<DMBaseGenerator> Active => active;

        /// <summary>Fuel level or running state changed (refuel, ran dry, loaded).</summary>
        public static event Action Changed;

        float fuel;
        bool switchedOff; // 0927-gen-power: the player turned the generator off from its menu
        bool lastRunning;
        float nextLoadCheck;
        Behaviour[] spinners;
        Renderer[] renderers;

        public float Fuel => fuel;
        public int Capacity => DMBuildingGhostProfile.GeneratorTankUnits;
        public float Fill01 => Capacity > 0 ? Mathf.Clamp01(fuel / Capacity) : 0f;
        public int Percent => Mathf.Clamp(Mathf.CeilToInt(Fill01 * 100f - 0.001f), 0, 100);
        public bool HasFuel => fuel > 0.0001f;
        /// <summary>0927-gen-power: false while the player has switched the generator off (no power, no burn).</summary>
        public bool PoweredOn => !switchedOff;
        public bool IsRunning => HasFuel && !switchedOff;
        public float SecondsPerUnit => DMBuildingGhostProfile.GeneratorMinutesPerUnit * 60f;
        /// <summary>0926-power-load: force fields and powered items in this generator's base (refreshed every second).</summary>
        public int LoadForceFields { get; private set; }
        public int LoadItems { get; private set; }
        public bool HasBase { get; private set; }
        /// <summary>Extra units per load period from everything drawing power.</summary>
        public float LoadUnitsPerPeriod => LoadForceFields * DMBuildingGhostProfile.ForceFieldUnitsPerPeriod + LoadItems * DMBuildingGhostProfile.PoweredItemUnitsPerPeriod;
        /// <summary>Total fuel units burned per second: base burn plus load.</summary>
        public float UnitsPerSecond => 1f / Mathf.Max(1f, SecondsPerUnit) + LoadUnitsPerPeriod / Mathf.Max(1f, DMBuildingGhostProfile.PowerLoadPeriodMinutes * 60f);
        public float SecondsLeft => fuel / Mathf.Max(0.000001f, UnitsPerSecond);
        public int Room => Mathf.Max(0, Mathf.FloorToInt(Capacity - fuel + 0.0001f));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            active.Clear();
            Changed = null;
        }

        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            var found = new List<Behaviour>();
            Behaviour[] all = GetComponentsInChildren<Behaviour>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].GetType().Name == "vRotateObject")
                    found.Add(all[i]);
            }

            spinners = found.ToArray();
            lastRunning = IsRunning;
            ApplyRunning();
        }

        void OnEnable()
        {
            if (!active.Contains(this))
                active.Add(this);
            WorldUseController.Register(this);
            DMBasePower.MarkDirty();
        }

        void OnDisable()
        {
            active.Remove(this);
            WorldUseController.Unregister(this);
            DMBasePower.MarkDirty();
        }

        void Update()
        {
            if (Time.unscaledTime >= nextLoadCheck)
                RefreshLoad();

            float dt = Time.deltaTime;
            if (fuel > 0f && dt > 0f && !switchedOff)
                fuel = Mathf.Max(0f, fuel - dt * UnitsPerSecond);

            bool running = IsRunning;
            if (running != lastRunning)
            {
                lastRunning = running;
                ApplyRunning();
                DMBasePower.MarkDirty();
                Changed?.Invoke();
            }
        }

        /// <summary>Recounts what draws power in this generator's base.</summary>
        public void RefreshLoad()
        {
            nextLoadCheck = Time.unscaledTime + 1f;
            DMBuildingGhost anchor = DMBasePower.BaseAnchorFor(transform.position);
            HasBase = anchor != null;
            DMBasePower.CountLoad(anchor, out int fields, out int items);
            LoadForceFields = fields;
            LoadItems = items;
        }

        /// <summary>Save load: puts the saved fuel level back.</summary>
        public void SetFuel(float units)
        {
            fuel = Mathf.Clamp(units, 0f, Mathf.Max(1, Capacity));
            lastRunning = IsRunning;
            ApplyRunning();
            DMBasePower.MarkDirty();
            Changed?.Invoke();
        }

        /// <summary>0927-gen-power: the menu's Turn Off / Turn On Power switch. Off cuts base power and stops the burn.</summary>
        public void SetPoweredOn(bool on)
        {
            if (switchedOff == !on)
                return;
            switchedOff = !on;
            lastRunning = IsRunning;
            ApplyRunning();
            DMBasePower.MarkDirty();
            Changed?.Invoke();
        }

        /// <summary>The chamber spins only while the generator runs.</summary>
        void ApplyRunning()
        {
            if (spinners == null)
                return;
            bool running = IsRunning;
            for (int i = 0; i < spinners.Length; i++)
            {
                if (spinners[i] != null)
                    spinners[i].enabled = running;
            }
        }

        // ---- Fuel ----

        public static bool IsFuel(ItemData item)
        {
            if (item == null)
                return false;
            return string.Equals(item.itemName, FuelItemName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.name, FuelItemName, StringComparison.OrdinalIgnoreCase);
        }

        public int CountInventoryFuel()
        {
            InventorySystem inventory = DMBuildingCatalog.Inventory(true);
            if (inventory == null || inventory.slots == null)
                return 0;
            int count = 0;
            for (int i = 0; i < inventory.slots.Count; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot != null && slot.amount > 0 && IsFuel(slot.item))
                    count += slot.amount;
            }

            return count;
        }

        /// <summary>Plasma Fuel in storage crates within range of this generator (Building Studio > Placement).</summary>
        public int CountCrateFuel()
        {
            CollectCratesInRange();
            int count = 0;
            for (int c = 0; c < cratesInRange.Count; c++)
            {
                DMStorageCrate crate = cratesInRange[c];
                DMStorageCrateState state = DMStorageCrateRuntime.GetOrCreate(crate.CrateId, crate.SlotCount);
                if (state == null)
                    continue;
                for (int i = 0; i < state.Slots.Count; i++)
                {
                    CrateSlot slot = state.Slots[i];
                    if (slot != null && !slot.IsEmpty && IsFuel(slot.item))
                        count += slot.amount;
                }
            }

            return count;
        }

        public int CratesInRangeCount()
        {
            CollectCratesInRange();
            return cratesInRange.Count;
        }

        /// <summary>Nearest first; a crate id shared by several scene copies counts once.</summary>
        void CollectCratesInRange()
        {
            cratesInRange.Clear();
            seenCrates.Clear();
            float range = DMBuildingGhostProfile.GeneratorFuelRangeMeters;
            float rangeSq = range * range;
            Vector3 here = transform.position;
            IReadOnlyList<DMStorageCrate> crates = DMStorageCrate.Active;
            for (int i = 0; i < crates.Count; i++)
            {
                DMStorageCrate crate = crates[i];
                if (crate == null || !crate.isActiveAndEnabled)
                    continue;
                if ((crate.transform.position - here).sqrMagnitude > rangeSq)
                    continue;
                if (!seenCrates.Add(crate.CrateId))
                    continue;
                cratesInRange.Add(crate);
            }

            cratesInRange.Sort((a, b) =>
                (a.transform.position - here).sqrMagnitude.CompareTo((b.transform.position - here).sqrMagnitude));
        }

        /// <summary>Loads up to the requested units (clamped to the room left). Inventory first, then crates in range.</summary>
        public int Refuel(int requested, out int fromInventory, out int fromCrates)
        {
            fromInventory = 0;
            fromCrates = 0;
            int want = Mathf.Min(requested, Room);
            if (want <= 0)
                return 0;

            InventorySystem inventory = DMBuildingCatalog.Inventory(true);
            if (inventory != null && inventory.slots != null)
            {
                for (int i = 0; i < inventory.slots.Count && want > 0; i++)
                {
                    InventorySystem.InventorySlot slot = inventory.slots[i];
                    if (slot == null || slot.amount <= 0 || !IsFuel(slot.item))
                        continue;
                    int take = Mathf.Min(want, slot.amount);
                    if (!inventory.RemoveItem(slot.item, take))
                        continue;
                    want -= take;
                    fromInventory += take;
                }
            }

            if (want > 0)
            {
                CollectCratesInRange();
                for (int c = 0; c < cratesInRange.Count && want > 0; c++)
                {
                    DMStorageCrate crate = cratesInRange[c];
                    DMStorageCrateState state = DMStorageCrateRuntime.GetOrCreate(crate.CrateId, crate.SlotCount);
                    if (state == null)
                        continue;
                    for (int i = 0; i < state.Slots.Count && want > 0; i++)
                    {
                        CrateSlot slot = state.Slots[i];
                        if (slot == null || slot.IsEmpty || !IsFuel(slot.item))
                            continue;
                        int take = Mathf.Min(want, slot.amount);
                        if (!state.RemoveAt(i, take))
                            continue;
                        want -= take;
                        fromCrates += take;
                    }
                }
            }

            int loaded = fromInventory + fromCrates;
            if (loaded > 0)
                SetFuel(fuel + loaded);
            return loaded;
        }

        /// <summary>Destroying the generator gives the whole units left in the tank back as Plasma Fuel.</summary>
        public void ReturnFuelToInventory()
        {
            int units = Mathf.FloorToInt(fuel + 0.0001f);
            fuel = 0f;
            if (units <= 0)
                return;
            ItemData item = ItemRegistry.Resolve(FuelItemName);
            InventorySystem inventory = DMBuildingCatalog.Inventory(true);
            if (item != null && inventory != null)
                inventory.AddItem(item, units, false);
        }

        // ---- Use (E) ----

        Bounds WorldBounds()
        {
            bool any = false;
            Bounds bounds = new Bounds(transform.position, Vector3.one);
            if (renderers == null)
                return bounds;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer r = renderers[i];
                if (r == null || !r.enabled)
                    continue;
                if (!any)
                {
                    bounds = r.bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            return bounds;
        }

        bool IsAimedInRange(WorldUseContext context, out float distance)
        {
            Bounds bounds = WorldBounds();
            distance = Mathf.Sqrt(bounds.SqrDistance(context.PlayerPosition));
            if (distance > InteractRangeMeters)
                return false;

            if (context.AimHit.HasValue && context.AimHit.Value.collider != null
                && context.AimHit.Value.collider.GetComponentInParent<DMBaseGenerator>() == this)
                return true;

            Ray ray = context.ViewRay;
            Vector3 toCenter = bounds.center - ray.origin;
            float along = Vector3.Dot(toCenter, ray.direction);
            if (along < 0f)
                return false;
            float off = (toCenter - ray.direction * along).magnitude;
            return off <= Mathf.Max(1.2f, bounds.extents.magnitude * 0.6f);
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (!GameSession.HasStarted || !IsAimedInRange(context, out float distance))
                return -1f;
            return 88f - distance;
        }

        public bool TryUse(WorldUseContext context)
        {
            if (!GameSession.HasStarted || !IsAimedInRange(context, out _))
                return false;
            return DMUiToolkitGenerator.TryShow(this);
        }

        public string GetInteractionPromptMessage()
        {
            if (switchedOff)
                return "Press E - Generator (switched off, " + Percent + "% fuel)";
            return IsRunning ? "Press E - Generator (" + Percent + "% fuel)" : "Press E - Generator (no fuel)";
        }

        public static string TryGetPrompt(WorldUseContext context)
        {
            DMBaseGenerator best = null;
            float bestPriority = -1f;
            for (int i = 0; i < active.Count; i++)
            {
                DMBaseGenerator generator = active[i];
                if (generator == null)
                    continue;
                float priority = generator.GetUsePriority(context);
                if (priority <= bestPriority)
                    continue;
                best = generator;
                bestPriority = priority;
            }

            return best != null && bestPriority >= 0f ? best.GetInteractionPromptMessage() : null;
        }
    }
}

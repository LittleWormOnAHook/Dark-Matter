using System.Collections.Generic;
using UnityEngine;

namespace Project.Building
{
    public static class BuildingOperationRegistry
    {
        public const int MaxAssignedPioneers = 4;

        private static readonly Dictionary<string, BuildingOperationState> Registry =
            new Dictionary<string, BuildingOperationState>();

        public static BuildingOperationState GetOrCreate(string buildingId)
        {
            string key = NormalizeBuildingId(buildingId);
            if (!Registry.TryGetValue(key, out BuildingOperationState state))
            {
                state = new BuildingOperationState(key);
                Registry[key] = state;
            }

            return state;
        }

        public static bool AssignPioneer(string buildingId, string pioneerDisplayName)
        {
            if (string.IsNullOrWhiteSpace(pioneerDisplayName))
                return false;

            BuildingOperationState state = GetOrCreate(buildingId);
            EnsureAssignedSlots(state);

            for (int i = 0; i < MaxAssignedPioneers; i++)
            {
                if (!string.IsNullOrEmpty(state.AssignedPioneers[i]))
                    continue;

                state.AssignedPioneers[i] = pioneerDisplayName;
                return true;
            }

            return false;
        }

        public static void UnassignSlot(string buildingId, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxAssignedPioneers)
                return;

            BuildingOperationState state = GetOrCreate(buildingId);
            EnsureAssignedSlots(state);
            state.AssignedPioneers[slotIndex] = string.Empty;
        }

        public static string CycleAssignSlot(string buildingId, int slotIndex, string[] availableNames)
        {
            if (slotIndex < 0 || slotIndex >= MaxAssignedPioneers)
                return string.Empty;

            BuildingOperationState state = GetOrCreate(buildingId);
            EnsureAssignedSlots(state);

            string current = state.AssignedPioneers[slotIndex] ?? string.Empty;
            List<string> cycleOptions = BuildCycleOptions(state, slotIndex, availableNames);

            int currentIndex = cycleOptions.IndexOf(current);
            if (currentIndex < 0)
                currentIndex = 0;

            int nextIndex = (currentIndex + 1) % cycleOptions.Count;
            string next = cycleOptions[nextIndex];
            state.AssignedPioneers[slotIndex] = next;
            return next;
        }

        public static void AddDemoQueueEntry(string buildingId)
        {
            BuildingOperationState state = GetOrCreate(buildingId);
            if (state.ProductionQueue.Count > 0)
                return;

            state.ProductionQueue.Add(new ProductionQueueEntry
            {
                RecipeName = "Stone Salve",
                Progress = 0.35f,
                Paused = false
            });
        }

        public static int CountAssignedPioneers(BuildingOperationState state)
        {
            if (state == null)
                return 0;

            int count = 0;
            for (int i = 0; i < state.AssignedPioneers.Count; i++)
            {
                if (!string.IsNullOrEmpty(state.AssignedPioneers[i]))
                    count++;
            }

            return count;
        }

        public static float GetEffectiveOutputMultiplier(BuildingOperationState state)
        {
            if (state == null)
                return 1f;

            int assigned = CountAssignedPioneers(state);
            float pioneerBoost = assigned * 0.12f;
            float settingBoost = state.Settings.BatchProductionMode ? 0.15f : 0f;
            return state.Settings.OutputMultiplier + pioneerBoost + settingBoost;
        }

        public static void TickProductionProgress(BuildingOperationState state, float delta, bool paused)
        {
            if (state == null || paused || state.ProductionQueue.Count == 0)
                return;

            ProductionQueueEntry entry = state.ProductionQueue[0];
            entry.Progress = Mathf.Clamp01(entry.Progress + delta);
            entry.Paused = false;
        }

        private static List<string> BuildCycleOptions(
            BuildingOperationState state,
            int slotIndex,
            string[] availableNames)
        {
            HashSet<string> assignedElsewhere = new HashSet<string>();
            for (int i = 0; i < MaxAssignedPioneers; i++)
            {
                if (i == slotIndex)
                    continue;

                string assigned = state.AssignedPioneers[i];
                if (!string.IsNullOrEmpty(assigned))
                    assignedElsewhere.Add(assigned);
            }

            List<string> options = new List<string> { string.Empty };
            if (availableNames == null)
                return options;

            for (int i = 0; i < availableNames.Length; i++)
            {
                string name = availableNames[i];
                if (string.IsNullOrWhiteSpace(name) || assignedElsewhere.Contains(name))
                    continue;

                if (!options.Contains(name))
                    options.Add(name);
            }

            return options;
        }

        private static void EnsureAssignedSlots(BuildingOperationState state)
        {
            while (state.AssignedPioneers.Count < MaxAssignedPioneers)
                state.AssignedPioneers.Add(string.Empty);

            if (state.AssignedPioneers.Count > MaxAssignedPioneers)
                state.AssignedPioneers.RemoveRange(MaxAssignedPioneers, state.AssignedPioneers.Count - MaxAssignedPioneers);
        }

        private static string NormalizeBuildingId(string buildingId)
        {
            return string.IsNullOrWhiteSpace(buildingId) ? "unknown_building" : buildingId.Trim();
        }
    }

    public sealed class BuildingOperationState
    {
        public string BuildingId { get; }
        public List<string> AssignedPioneers { get; } = new List<string>(BuildingOperationRegistry.MaxAssignedPioneers);
        public List<ProductionQueueEntry> ProductionQueue { get; } = new List<ProductionQueueEntry>();
        public BuildingSettings Settings { get; } = new BuildingSettings();

        public BuildingOperationState(string buildingId)
        {
            BuildingId = buildingId;
            for (int i = 0; i < BuildingOperationRegistry.MaxAssignedPioneers; i++)
                AssignedPioneers.Add(string.Empty);
        }
    }

    public sealed class BuildingSettings
    {
        public bool AutoMaintenance = true;
        public bool PrioritizeSkilledTriage;
        public bool AcceptInjuredOverflow;
        public bool BatchProductionMode;
        public bool DeepDrillMode;
        public float MaintenancePercent = 88f;
        public float OutputMultiplier = 1f;
    }

    public sealed class ProductionQueueEntry
    {
        public string RecipeName;
        public float Progress;
        public bool Paused;
    }
}

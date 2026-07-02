using System.Collections.Generic;
using Project.Core;
using Project.Pioneers;
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
            float classBoost = GetClassAffinityBoost(state);
            return state.Settings.OutputMultiplier + pioneerBoost + settingBoost + classBoost;
        }

        private static float GetClassAffinityBoost(BuildingOperationState state)
        {
            PioneerRosterManager roster = PioneerRosterManager.Instance;
            if (roster == null || state == null)
                return 0f;

            float boost = 0f;
            string buildingId = state.BuildingId ?? string.Empty;
            for (int i = 0; i < state.AssignedPioneerIds.Count; i++)
            {
                string pioneerId = state.AssignedPioneerIds[i];
                if (string.IsNullOrEmpty(pioneerId))
                    continue;

                SkilledPioneerRecord record = roster.FindSkilledById(pioneerId);
                if (record == null)
                {
                    boost += 0.03f;
                    continue;
                }

                boost += PioneerClassTaskAffinity.GetFacilityBonus(record.pioneerClass, buildingId);
            }

            return boost;
        }

        public static void TickProductionProgress(BuildingOperationState state, float delta, bool paused)
        {
            if (state == null || paused || state.ProductionQueue.Count == 0)
                return;

            ProductionQueueEntry entry = state.ProductionQueue[0];
            entry.Progress = Mathf.Clamp01(entry.Progress + delta);
            entry.Paused = false;
        }

        public static bool AssignPioneerById(string buildingId, string pioneerId)
        {
            if (string.IsNullOrWhiteSpace(pioneerId))
                return false;

            BuildingOperationState state = GetOrCreate(buildingId);
            EnsureAssignedSlots(state);

            for (int i = 0; i < MaxAssignedPioneers; i++)
            {
                if (!string.IsNullOrEmpty(state.AssignedPioneerIds[i]))
                    continue;

                state.AssignedPioneerIds[i] = pioneerId;
                SyncLegacyAssignedNames(state);
                PioneerRosterManager roster = PioneerRosterManager.Instance;
                roster?.SyncColonistAssignedCount(CountAllAssignedPioneers());
                return true;
            }

            return false;
        }

        public static string CycleAssignSlotById(string buildingId, int slotIndex, string[] availableIds, string[] availableNames)
        {
            if (slotIndex < 0 || slotIndex >= MaxAssignedPioneers)
                return string.Empty;

            BuildingOperationState state = GetOrCreate(buildingId);
            EnsureAssignedSlots(state);

            string current = state.AssignedPioneerIds[slotIndex] ?? string.Empty;
            List<string> cycleOptions = BuildCycleOptionsById(state, slotIndex, availableIds);

            int currentIndex = cycleOptions.IndexOf(current);
            if (currentIndex < 0)
                currentIndex = 0;

            int nextIndex = (currentIndex + 1) % cycleOptions.Count;
            string next = cycleOptions[nextIndex];
            state.AssignedPioneerIds[slotIndex] = next;
            SyncLegacyAssignedNames(state, availableIds, availableNames);
            PioneerRosterManager roster = PioneerRosterManager.Instance;
            roster?.SyncColonistAssignedCount(CountAllAssignedPioneers());
            return next;
        }

        public static int CountAllAssignedPioneers()
        {
            int total = 0;
            foreach (KeyValuePair<string, BuildingOperationState> pair in Registry)
                total += CountAssignedPioneers(pair.Value);
            return total;
        }

        public static BuildingOperationsSaveRecord BuildSaveSnapshot()
        {
            List<BuildingOperationSaveEntry> entries = new List<BuildingOperationSaveEntry>();
            foreach (KeyValuePair<string, BuildingOperationState> pair in Registry)
            {
                BuildingOperationState state = pair.Value;
                if (state == null)
                    continue;

                BuildingOperationSaveEntry entry = new BuildingOperationSaveEntry
                {
                    buildingId = state.BuildingId,
                    assignedPioneerIds = state.AssignedPioneerIds.ToArray(),
                    autoMaintenance = state.Settings.AutoMaintenance,
                    batchProductionMode = state.Settings.BatchProductionMode,
                    outputMultiplier = state.Settings.OutputMultiplier
                };

                if (state.ProductionQueue.Count > 0)
                {
                    entry.productionRecipeNames = new string[state.ProductionQueue.Count];
                    entry.productionProgress = new float[state.ProductionQueue.Count];
                    for (int i = 0; i < state.ProductionQueue.Count; i++)
                    {
                        entry.productionRecipeNames[i] = state.ProductionQueue[i].RecipeName;
                        entry.productionProgress[i] = state.ProductionQueue[i].Progress;
                    }
                }

                entries.Add(entry);
            }

            return new BuildingOperationsSaveRecord { entries = entries.ToArray() };
        }

        public static void ApplySaveSnapshot(BuildingOperationsSaveRecord snapshot)
        {
            Registry.Clear();
            if (snapshot?.entries == null)
                return;

            for (int i = 0; i < snapshot.entries.Length; i++)
            {
                BuildingOperationSaveEntry saved = snapshot.entries[i];
                if (saved == null || string.IsNullOrWhiteSpace(saved.buildingId))
                    continue;

                BuildingOperationState state = GetOrCreate(saved.buildingId);
                EnsureAssignedSlots(state);

                if (saved.assignedPioneerIds != null)
                {
                    for (int slot = 0; slot < MaxAssignedPioneers && slot < saved.assignedPioneerIds.Length; slot++)
                        state.AssignedPioneerIds[slot] = saved.assignedPioneerIds[slot] ?? string.Empty;
                }

                state.Settings.AutoMaintenance = saved.autoMaintenance;
                state.Settings.BatchProductionMode = saved.batchProductionMode;
                state.Settings.OutputMultiplier = saved.outputMultiplier;
                state.ProductionQueue.Clear();

                if (saved.productionRecipeNames != null)
                {
                    for (int q = 0; q < saved.productionRecipeNames.Length; q++)
                    {
                        float progress = saved.productionProgress != null && q < saved.productionProgress.Length
                            ? saved.productionProgress[q]
                            : 0f;
                        state.ProductionQueue.Add(new ProductionQueueEntry
                        {
                            RecipeName = saved.productionRecipeNames[q],
                            Progress = progress,
                            Paused = false
                        });
                    }
                }

                SyncLegacyAssignedNames(state);
            }

            PioneerRosterManager roster = PioneerRosterManager.Instance;
            roster?.SyncColonistAssignedCount(CountAllAssignedPioneers());
        }

        public static void TickAllFacilities(float deltaSeconds, bool paused)
        {
            foreach (KeyValuePair<string, BuildingOperationState> pair in Registry)
            {
                BuildingOperationState state = pair.Value;
                if (state == null)
                    continue;

                float effectiveDelta = deltaSeconds * GetEffectiveOutputMultiplier(state) * 0.02f;
                TickProductionProgress(state, effectiveDelta, paused);
            }
        }

        private static List<string> BuildCycleOptionsById(
            BuildingOperationState state,
            int slotIndex,
            string[] availableIds)
        {
            HashSet<string> assignedElsewhere = new HashSet<string>();
            for (int i = 0; i < MaxAssignedPioneers; i++)
            {
                if (i == slotIndex)
                    continue;

                string assigned = state.AssignedPioneerIds[i];
                if (!string.IsNullOrEmpty(assigned))
                    assignedElsewhere.Add(assigned);
            }

            List<string> options = new List<string> { string.Empty };
            if (availableIds == null)
                return options;

            for (int i = 0; i < availableIds.Length; i++)
            {
                string id = availableIds[i];
                if (string.IsNullOrWhiteSpace(id) || assignedElsewhere.Contains(id))
                    continue;

                if (!options.Contains(id))
                    options.Add(id);
            }

            return options;
        }

        private static void SyncLegacyAssignedNames(
            BuildingOperationState state,
            string[] availableIds = null,
            string[] availableNames = null)
        {
            PioneerRosterManager roster = PioneerRosterManager.Instance;
            for (int i = 0; i < MaxAssignedPioneers; i++)
            {
                string pioneerId = state.AssignedPioneerIds[i];
                if (string.IsNullOrEmpty(pioneerId))
                {
                    state.AssignedPioneers[i] = string.Empty;
                    continue;
                }

                SkilledPioneerRecord record = roster != null ? roster.FindSkilledById(pioneerId) : null;
                state.AssignedPioneers[i] = record != null ? record.displayName : ResolveNameFromParallelArrays(pioneerId, availableIds, availableNames);
            }
        }

        private static string ResolveNameFromParallelArrays(string pioneerId, string[] availableIds, string[] availableNames)
        {
            if (availableIds == null || availableNames == null)
                return pioneerId;

            for (int i = 0; i < availableIds.Length && i < availableNames.Length; i++)
            {
                if (availableIds[i] == pioneerId)
                    return availableNames[i];
            }

            return pioneerId;
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

            while (state.AssignedPioneerIds.Count < MaxAssignedPioneers)
                state.AssignedPioneerIds.Add(string.Empty);

            if (state.AssignedPioneers.Count > MaxAssignedPioneers)
                state.AssignedPioneers.RemoveRange(MaxAssignedPioneers, state.AssignedPioneers.Count - MaxAssignedPioneers);

            if (state.AssignedPioneerIds.Count > MaxAssignedPioneers)
                state.AssignedPioneerIds.RemoveRange(MaxAssignedPioneers, state.AssignedPioneerIds.Count - MaxAssignedPioneers);
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
        public List<string> AssignedPioneerIds { get; } = new List<string>(BuildingOperationRegistry.MaxAssignedPioneers);
        public List<ProductionQueueEntry> ProductionQueue { get; } = new List<ProductionQueueEntry>();
        public BuildingSettings Settings { get; } = new BuildingSettings();

        public BuildingOperationState(string buildingId)
        {
            BuildingId = buildingId;
            for (int i = 0; i < BuildingOperationRegistry.MaxAssignedPioneers; i++)
            {
                AssignedPioneers.Add(string.Empty);
                AssignedPioneerIds.Add(string.Empty);
            }
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

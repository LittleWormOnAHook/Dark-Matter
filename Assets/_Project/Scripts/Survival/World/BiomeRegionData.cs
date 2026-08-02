using System;
using UnityEngine;

namespace Project.Survival.World
{
    /// <summary>
    /// Data-driven surface biome region for the full-scale Io main map (B1–B7).
    /// Phase W0 foundation — see Io_Genesis_World_Map_Geography.md.
    /// </summary>
    [CreateAssetMenu(
        fileName = "biome_region",
        menuName = "Dark Matter Genesis/World/Biome Region Data")]
    public class BiomeRegionData : ScriptableObject
    {
        [Header("Identity")]
        public IoSurfaceRegionId regionId = IoSurfaceRegionId.None;
        public string displayName = "Biome Region";

        [Tooltip("Campaign unlock sequence — lower unlocks first. B6 hub = 0.")]
        public int campaignUnlockOrder;

        [TextArea(2, 4)]
        public string designerNotes;

        [Header("Map Placement (UV 0–1 on Genesis moon disc)")]
        [Range(0f, 1f)] public float mapCenterU = 0.5f;
        [Range(0f, 1f)] public float mapCenterV = 0.5f;
        [Range(0.01f, 0.5f)] public float mapRadius = 0.15f;
        public Color mapLegendColor = Color.gray;

        [Header("Thermal baseline (tidally locked)")]
        [Tooltip("Positive = hotter than equatorial mean (sub-Jovian); negative = colder (anti-Jovian / polar).")]
        [Range(-1f, 1f)] public float thermalBias = 0f;

        [Header("Pressures & Exploration")]
        public ExposurePressureFlags dominantPressures = ExposurePressureFlags.None;
        public BiomeExplorationVerb[] explorationVerbs;
        public BiomeVehicleAllowance vehicleAllowance = BiomeVehicleAllowance.PathLanes;

        [Header("Weather Weights (relative, not normalized)")]
        [Range(0f, 1f)] public float sulfurStormWeight;
        [Range(0f, 1f)] public float geyserSurgeWeight;
        [Range(0f, 1f)] public float ashGaleWeight;
        [Range(0f, 1f)] public float eruptionColumnWeight;
        [Range(0f, 1f)] public float polarNightWeight;
        [Range(0f, 1f)] public float resonanceSpikeWeight;

        public string ResolvedId => regionId == IoSurfaceRegionId.None
            ? name
            : regionId.ToString();
    }

    [Flags]
    public enum ExposurePressureFlags
    {
        None = 0,
        Radiation = 1 << 0,
        ThermalCold = 1 << 1,
        ThermalHeat = 1 << 2,
        Sulfur = 1 << 3,
        Volcano = 1 << 4,
        Resonance = 1 << 5
    }

    public enum BiomeExplorationVerb
    {
        Route = 0,
        Scan = 1,
        Time = 2,
        Shelter = 3,
        Sample = 4,
        Clear = 5,
        Breach = 6,
        Stabilize = 7,
        Extract = 8
    }

    public enum BiomeVehicleAllowance
    {
        FootOnly = 0,
        PathLanes = 1,
        LimitedPads = 2,
        FlatCorridors = 3,
        HubPads = 4
    }
}

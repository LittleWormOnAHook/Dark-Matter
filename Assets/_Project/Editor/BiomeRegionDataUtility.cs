#if UNITY_EDITOR
using System.IO;
using Project.Survival.World;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.World
{
    /// <summary>
    /// Creates B1–B7 BiomeRegionData assets aligned to Io plan maps (World/WorldMap).
    /// UV origin: bottom-left, V+ = north / anti-Jovian cold.
    /// </summary>
    public static class BiomeRegionDataUtility
    {
        private const string BiomeFolder = "Assets/_Project/Data/World/Biomes";
        private const string RegistryPath = "Assets/_Project/Resources/World/BiomeRegionRegistry.asset";

        private struct BiomeSeed
        {
            public IoSurfaceRegionId Id;
            public string DisplayName;
            public int UnlockOrder;
            public float CenterU;
            public float CenterV;
            public float Radius;
            public float ThermalBias;
            public Color LegendColor;
            public ExposurePressureFlags Pressures;
            public BiomeExplorationVerb[] Verbs;
            public BiomeVehicleAllowance Vehicles;
            public string Notes;
            public float SulfurStorm;
            public float GeyserSurge;
            public float AshGale;
            public float Eruption;
            public float PolarNight;
            public float Resonance;
        }

        // Centers read from Io_Plan_BiomeMap_TopDown.png (plan art, Aug 2026).
        private static readonly BiomeSeed[] Seeds =
        {
            new BiomeSeed
            {
                Id = IoSurfaceRegionId.BasaltHighlands,
                DisplayName = "Basalt Highlands",
                UnlockOrder = 0,
                CenterU = 0.48f, CenterV = 0.60f, Radius = 0.13f,
                ThermalBias = 0.1f,
                LegendColor = new Color(0.29f, 0.27f, 0.25f),
                Pressures = ExposurePressureFlags.ThermalCold | ExposurePressureFlags.ThermalHeat,
                Verbs = new[] { BiomeExplorationVerb.Route, BiomeExplorationVerb.Breach, BiomeExplorationVerb.Shelter },
                Vehicles = BiomeVehicleAllowance.HubPads,
                Notes = "W1 hub. Command Center at UV (0.48, 0.62). Mixed pressures stub OK.",
                AshGale = 0.3f
            },
            new BiomeSeed
            {
                Id = IoSurfaceRegionId.SulfurPlains,
                DisplayName = "Sulfur Plains",
                UnlockOrder = 1,
                CenterU = 0.50f, CenterV = 0.38f, Radius = 0.12f,
                ThermalBias = 0.45f,
                LegendColor = new Color(0.79f, 0.64f, 0.15f),
                Pressures = ExposurePressureFlags.Sulfur,
                Verbs = new[] { BiomeExplorationVerb.Route, BiomeExplorationVerb.Sample, BiomeExplorationVerb.Shelter },
                Vehicles = BiomeVehicleAllowance.PathLanes,
                Notes = "South of B6 on plan map. Storm lanes authored in W2.",
                SulfurStorm = 0.85f
            },
            new BiomeSeed
            {
                Id = IoSurfaceRegionId.GeyserFields,
                DisplayName = "Geyser Fields",
                UnlockOrder = 2,
                CenterU = 0.20f, CenterV = 0.40f, Radius = 0.11f,
                ThermalBias = 0.65f,
                LegendColor = new Color(0.83f, 0.69f, 0.29f),
                Pressures = ExposurePressureFlags.Sulfur | ExposurePressureFlags.Volcano,
                Verbs = new[] { BiomeExplorationVerb.Time, BiomeExplorationVerb.Sample, BiomeExplorationVerb.Clear },
                Vehicles = BiomeVehicleAllowance.LimitedPads,
                Notes = "West lobe on plan map.",
                SulfurStorm = 0.5f, GeyserSurge = 0.9f
            },
            new BiomeSeed
            {
                Id = IoSurfaceRegionId.AshFlatsAndRidges,
                DisplayName = "Ash Flats & Ridges",
                UnlockOrder = 3,
                CenterU = 0.80f, CenterV = 0.55f, Radius = 0.14f,
                ThermalBias = 0.2f,
                LegendColor = new Color(0.55f, 0.45f, 0.33f),
                Pressures = ExposurePressureFlags.ThermalHeat | ExposurePressureFlags.Volcano,
                Verbs = new[] { BiomeExplorationVerb.Route, BiomeExplorationVerb.Scan, BiomeExplorationVerb.Extract },
                Vehicles = BiomeVehicleAllowance.FlatCorridors,
                Notes = "East ash corridor primary; west twin on plan map (author as second volume in W3).",
                AshGale = 0.8f
            },
            new BiomeSeed
            {
                Id = IoSurfaceRegionId.PolarRadiationFlats,
                DisplayName = "Polar Radiation Flats",
                UnlockOrder = 4,
                CenterU = 0.50f, CenterV = 0.92f, Radius = 0.10f,
                ThermalBias = -0.85f,
                LegendColor = new Color(0.42f, 0.55f, 0.68f),
                Pressures = ExposurePressureFlags.Radiation | ExposurePressureFlags.ThermalCold,
                Verbs = new[] { BiomeExplorationVerb.Route, BiomeExplorationVerb.Scan, BiomeExplorationVerb.Shelter },
                Vehicles = BiomeVehicleAllowance.FootOnly,
                Notes = "North cap primary UV. South polar mirror is the same biome id (place second volume later).",
                PolarNight = 0.95f
            },
            new BiomeSeed
            {
                Id = IoSurfaceRegionId.LavaCalderas,
                DisplayName = "Lava Calderas",
                UnlockOrder = 5,
                CenterU = 0.48f, CenterV = 0.20f, Radius = 0.13f,
                ThermalBias = 0.95f,
                LegendColor = new Color(0.36f, 0.18f, 0.18f),
                Pressures = ExposurePressureFlags.Volcano | ExposurePressureFlags.ThermalHeat,
                Verbs = new[] { BiomeExplorationVerb.Time, BiomeExplorationVerb.Clear, BiomeExplorationVerb.Breach },
                Vehicles = BiomeVehicleAllowance.FootOnly,
                Notes = "Sub-Jovian hot south. Large crater bowl on heightmap.",
                Eruption = 0.9f
            },
            new BiomeSeed
            {
                Id = IoSurfaceRegionId.PrecursorRuinBelt,
                DisplayName = "Precursor Ruin Belt",
                UnlockOrder = 6,
                CenterU = 0.50f, CenterV = 0.78f, Radius = 0.12f,
                ThermalBias = -0.55f,
                LegendColor = new Color(0.24f, 0.55f, 0.55f),
                Pressures = ExposurePressureFlags.Radiation | ExposurePressureFlags.Resonance,
                Verbs = new[] { BiomeExplorationVerb.Scan, BiomeExplorationVerb.Stabilize, BiomeExplorationVerb.Extract },
                Vehicles = BiomeVehicleAllowance.FootOnly,
                Notes = "Anti-Jovian band below north polar cap.",
                Resonance = 0.85f
            }
        };

        [MenuItem(SurvivalPioneerEditorMenus.World + "Create / Refresh Biome Region Assets (B1–B7)", false, 10)]
        public static void CreateBiomeRegionAssets()
        {
            Directory.CreateDirectory(BiomeFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(RegistryPath) ?? string.Empty);

            BiomeRegionData[] created = new BiomeRegionData[Seeds.Length];
            for (int i = 0; i < Seeds.Length; i++)
            {
                BiomeSeed seed = Seeds[i];
                string assetPath = $"{BiomeFolder}/Biome_{seed.Id}.asset";
                BiomeRegionData existing = AssetDatabase.LoadAssetAtPath<BiomeRegionData>(assetPath);
                BiomeRegionData asset = existing != null ? existing : ScriptableObject.CreateInstance<BiomeRegionData>();

                asset.regionId = seed.Id;
                asset.displayName = seed.DisplayName;
                asset.campaignUnlockOrder = seed.UnlockOrder;
                asset.designerNotes = seed.Notes;
                asset.mapCenterU = seed.CenterU;
                asset.mapCenterV = seed.CenterV;
                asset.mapRadius = seed.Radius;
                asset.mapLegendColor = seed.LegendColor;
                asset.thermalBias = seed.ThermalBias;
                asset.dominantPressures = seed.Pressures;
                asset.explorationVerbs = seed.Verbs;
                asset.vehicleAllowance = seed.Vehicles;
                asset.sulfurStormWeight = seed.SulfurStorm;
                asset.geyserSurgeWeight = seed.GeyserSurge;
                asset.ashGaleWeight = seed.AshGale;
                asset.eruptionColumnWeight = seed.Eruption;
                asset.polarNightWeight = seed.PolarNight;
                asset.resonanceSpikeWeight = seed.Resonance;

                if (existing == null)
                    AssetDatabase.CreateAsset(asset, assetPath);
                else
                    EditorUtility.SetDirty(asset);

                created[i] = asset;
            }

            BiomeRegionRegistry registry = AssetDatabase.LoadAssetAtPath<BiomeRegionRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<BiomeRegionRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
            }

            SerializedObject serializedRegistry = new SerializedObject(registry);
            serializedRegistry.FindProperty("regions").arraySize = created.Length;
            for (int i = 0; i < created.Length; i++)
                serializedRegistry.FindProperty("regions").GetArrayElementAtIndex(i).objectReferenceValue = created[i];
            serializedRegistry.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BiomeRegionData] Created/updated {created.Length} assets in {BiomeFolder} + registry.");
        }
    }
}
#endif

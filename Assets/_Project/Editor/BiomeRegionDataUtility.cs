#if UNITY_EDITOR
using System.IO;
using Project.Survival.World;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.World
{
    /// <summary>
    /// Creates B1–B7 BiomeRegionData assets aligned to Io_Genesis_World_Map_Geography.md.
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
            public Color LegendColor;
            public ExposurePressureFlags Pressures;
            public BiomeExplorationVerb[] Verbs;
            public BiomeVehicleAllowance Vehicles;
            public float SulfurStorm;
            public float GeyserSurge;
            public float AshGale;
            public float Eruption;
            public float PolarNight;
            public float Resonance;
        }

        private static readonly BiomeSeed[] Seeds =
        {
            new BiomeSeed
            {
                Id = IoSurfaceRegionId.BasaltHighlands,
                DisplayName = "Basalt Highlands",
                UnlockOrder = 0,
                CenterU = 0.52f, CenterV = 0.58f, Radius = 0.18f,
                LegendColor = new Color(0.55f, 0.45f, 0.38f),
                Pressures = ExposurePressureFlags.ThermalCold | ExposurePressureFlags.ThermalHeat,
                Verbs = new[] { BiomeExplorationVerb.Route, BiomeExplorationVerb.Breach, BiomeExplorationVerb.Shelter },
                Vehicles = BiomeVehicleAllowance.HubPads,
                AshGale = 0.3f
            },
            new BiomeSeed
            {
                Id = IoSurfaceRegionId.SulfurPlains,
                DisplayName = "Sulfur Plains",
                UnlockOrder = 1,
                CenterU = 0.72f, CenterV = 0.42f, Radius = 0.16f,
                LegendColor = new Color(0.83f, 0.63f, 0.09f),
                Pressures = ExposurePressureFlags.Sulfur,
                Verbs = new[] { BiomeExplorationVerb.Route, BiomeExplorationVerb.Sample, BiomeExplorationVerb.Shelter },
                Vehicles = BiomeVehicleAllowance.PathLanes,
                SulfurStorm = 0.85f
            },
            new BiomeSeed
            {
                Id = IoSurfaceRegionId.GeyserFields,
                DisplayName = "Geyser Fields",
                UnlockOrder = 2,
                CenterU = 0.78f, CenterV = 0.62f, Radius = 0.14f,
                LegendColor = new Color(0.90f, 0.70f, 0.15f),
                Pressures = ExposurePressureFlags.Sulfur | ExposurePressureFlags.Volcano,
                Verbs = new[] { BiomeExplorationVerb.Time, BiomeExplorationVerb.Sample, BiomeExplorationVerb.Clear },
                Vehicles = BiomeVehicleAllowance.LimitedPads,
                SulfurStorm = 0.5f, GeyserSurge = 0.9f
            },
            new BiomeSeed
            {
                Id = IoSurfaceRegionId.AshFlatsAndRidges,
                DisplayName = "Ash Flats & Ridges",
                UnlockOrder = 3,
                CenterU = 0.48f, CenterV = 0.28f, Radius = 0.17f,
                LegendColor = new Color(0.45f, 0.38f, 0.32f),
                Pressures = ExposurePressureFlags.ThermalHeat | ExposurePressureFlags.Volcano,
                Verbs = new[] { BiomeExplorationVerb.Route, BiomeExplorationVerb.Scan, BiomeExplorationVerb.Extract },
                Vehicles = BiomeVehicleAllowance.FlatCorridors,
                AshGale = 0.8f
            },
            new BiomeSeed
            {
                Id = IoSurfaceRegionId.PolarRadiationFlats,
                DisplayName = "Polar Radiation Flats",
                UnlockOrder = 4,
                CenterU = 0.22f, CenterV = 0.50f, Radius = 0.15f,
                LegendColor = new Color(0.42f, 0.50f, 0.66f),
                Pressures = ExposurePressureFlags.Radiation | ExposurePressureFlags.ThermalCold,
                Verbs = new[] { BiomeExplorationVerb.Route, BiomeExplorationVerb.Scan, BiomeExplorationVerb.Shelter },
                Vehicles = BiomeVehicleAllowance.FootOnly,
                PolarNight = 0.95f
            },
            new BiomeSeed
            {
                Id = IoSurfaceRegionId.LavaCalderas,
                DisplayName = "Lava Calderas",
                UnlockOrder = 5,
                CenterU = 0.68f, CenterV = 0.52f, Radius = 0.13f,
                LegendColor = new Color(0.36f, 0.18f, 0.18f),
                Pressures = ExposurePressureFlags.Volcano | ExposurePressureFlags.ThermalHeat,
                Verbs = new[] { BiomeExplorationVerb.Time, BiomeExplorationVerb.Clear, BiomeExplorationVerb.Breach },
                Vehicles = BiomeVehicleAllowance.FootOnly,
                Eruption = 0.9f
            },
            new BiomeSeed
            {
                Id = IoSurfaceRegionId.PrecursorRuinBelt,
                DisplayName = "Precursor Ruin Belt",
                UnlockOrder = 6,
                CenterU = 0.50f, CenterV = 0.22f, Radius = 0.12f,
                LegendColor = new Color(0.24f, 0.55f, 0.55f),
                Pressures = ExposurePressureFlags.Radiation | ExposurePressureFlags.Resonance,
                Verbs = new[] { BiomeExplorationVerb.Scan, BiomeExplorationVerb.Stabilize, BiomeExplorationVerb.Extract },
                Vehicles = BiomeVehicleAllowance.FootOnly,
                Resonance = 0.85f
            }
        };

        [MenuItem(SurvivalPioneerEditorMenus.World + "Create Biome Region Assets (B1–B7)", false, 10)]
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
                asset.mapCenterU = seed.CenterU;
                asset.mapCenterV = seed.CenterV;
                asset.mapRadius = seed.Radius;
                asset.mapLegendColor = seed.LegendColor;
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
            Debug.Log($"Created/updated {created.Length} BiomeRegionData assets in {BiomeFolder} and registry at {RegistryPath}.");
        }
    }
}
#endif

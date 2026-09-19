using System;
using UnityEngine;

namespace Project.World.TerrainSplat
{
    [Serializable]
    public sealed class DMTerrainSplatLayerEntry
    {
        public const int MaxSplatSlots = 16;

        [Tooltip("Splat index on terrain (0 = first layer).")]
        [Range(0, MaxSplatSlots - 1)]
        public int splatIndex;

        [Tooltip("Editor label (Io set name, biome, etc.).")]
        public string label;

        [Tooltip("TerrainLayer asset on the terrain data.")]
        public TerrainLayer terrainLayer;

        [Tooltip("Optional sidecar; when set, its fields override the inline toggles below.")]
        public DMTerrainLayerRenderOptions optionsSidecar;

        [Header("Inline (used when sidecar is null)")]
        public bool useHeightBasedBlend = true;

        public bool enableParallax;

        [Range(0f, 0.08f)]
        public float parallaxStrength = 0.02f;

        [Range(0.25f, 2f)]
        public float heightBlendContrast = 1f;

        [Range(-0.5f, 0.5f)]
        public float heightBlendBias;

        [Range(-1f, 2f)]
        public float normalScaleOverride = -1f;

        public float tileSizeWorldMeters;

        public DMTerrainLayerRenderResolved Resolve()
        {
            if (optionsSidecar != null)
            {
                return new DMTerrainLayerRenderResolved(
                    optionsSidecar.useHeightBasedBlend,
                    optionsSidecar.enableParallax,
                    optionsSidecar.parallaxStrength,
                    optionsSidecar.heightBlendContrast,
                    optionsSidecar.heightBlendBias,
                    optionsSidecar.normalScaleOverride,
                    optionsSidecar.tileSizeWorldMeters,
                    optionsSidecar.parallaxMaxSteps);
            }

            return new DMTerrainLayerRenderResolved(
                useHeightBasedBlend,
                enableParallax,
                parallaxStrength,
                heightBlendContrast,
                heightBlendBias,
                normalScaleOverride,
                tileSizeWorldMeters,
                parallaxMaxSteps: 12);
        }
    }

    public readonly struct DMTerrainLayerRenderResolved
    {
        public readonly bool UseHeightBasedBlend;
        public readonly bool EnableParallax;
        public readonly float ParallaxStrength;
        public readonly float HeightBlendContrast;
        public readonly float HeightBlendBias;
        public readonly float NormalScaleOverride;
        public readonly float TileSizeWorldMeters;
        public readonly int ParallaxMaxSteps;

        public DMTerrainLayerRenderResolved(
            bool useHeightBasedBlend,
            bool enableParallax,
            float parallaxStrength,
            float heightBlendContrast,
            float heightBlendBias,
            float normalScaleOverride,
            float tileSizeWorldMeters,
            int parallaxMaxSteps)
        {
            UseHeightBasedBlend = useHeightBasedBlend;
            EnableParallax = enableParallax;
            ParallaxStrength = parallaxStrength;
            HeightBlendContrast = heightBlendContrast;
            HeightBlendBias = heightBlendBias;
            NormalScaleOverride = normalScaleOverride;
            TileSizeWorldMeters = tileSizeWorldMeters;
            ParallaxMaxSteps = parallaxMaxSteps;
        }
    }

    /// <summary>
    /// Per-splat render options for Io (and Gaia) terrain layers — height blend on/off per layer, parallax on select layers.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Dark Matter/World/Terrain Splat Render Profile",
        fileName = "DM_TerrainSplatRenderProfile")]
    public sealed class DMTerrainSplatRenderProfile : ScriptableObject
    {
        public const string ResourcesPath = "World/DM_TerrainSplatRenderProfile";
        public const int DefaultSlotCount = 12;

        private static DMTerrainSplatRenderProfile live;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLiveCache()
        {
            live = null;
        }

        public static DMTerrainSplatRenderProfile Live
        {
            get
            {
                if (live == null)
                    live = Resources.Load<DMTerrainSplatRenderProfile>(ResourcesPath);
                if (live != null)
                    live.EnsureSlots();
                return live;
            }
        }

        [Tooltip("When true, terrains with DMTerrainSplatRenderBootstrap apply this profile on enable.")]
        public bool autoApplyOnPlay = true;

        [Tooltip("Splat rows — match TerrainLayer references or splat index on your tiles.")]
        public DMTerrainSplatLayerEntry[] splatLayers;

        public void EnsureSlots()
        {
            if (splatLayers != null && splatLayers.Length >= DefaultSlotCount)
                return;

            int target = Mathf.Max(DefaultSlotCount, splatLayers?.Length ?? 0);
            var next = new DMTerrainSplatLayerEntry[target];
            for (int i = 0; i < target; i++)
            {
                if (splatLayers != null && i < splatLayers.Length && splatLayers[i] != null)
                    next[i] = splatLayers[i];
                else
                    next[i] = CreateDefaultRow(i);

                next[i].splatIndex = i;
                if (string.IsNullOrEmpty(next[i].label))
                    next[i].label = DefaultLabel(i);
            }

            splatLayers = next;
        }

        public DMTerrainSplatLayerEntry FindForTerrainLayer(TerrainLayer layer, int splatIndex)
        {
            EnsureSlots();
            if (layer != null && splatLayers != null)
            {
                for (int i = 0; i < splatLayers.Length; i++)
                {
                    DMTerrainSplatLayerEntry row = splatLayers[i];
                    if (row?.terrainLayer == layer)
                        return row;
                }
            }

            if (splatIndex >= 0 && splatLayers != null && splatIndex < splatLayers.Length)
                return splatLayers[splatIndex];

            return null;
        }

        private static DMTerrainSplatLayerEntry CreateDefaultRow(int index)
        {
            return new DMTerrainSplatLayerEntry
            {
                splatIndex = index,
                label = DefaultLabel(index),
                useHeightBasedBlend = true,
                enableParallax = false
            };
        }

        private static string DefaultLabel(int index)
        {
            return index switch
            {
                0 => "0 Basalt / default",
                1 => "1 Sulfur plains",
                2 => "2 Rock / regolith",
                3 => "3 Sinter / glass",
                4 => "4 Ash",
                5 => "5 Brimstone crystal",
                6 => "6 Flow bands",
                7 => "7 Scorched",
                8 => "8 Alien residue",
                9 => "9 Anthropogenic trace",
                _ => $"{index} Terrain layer"
            };
        }
    }
}

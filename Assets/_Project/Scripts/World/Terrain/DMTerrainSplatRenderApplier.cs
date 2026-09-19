using UnityEngine;

namespace Project.World.TerrainSplat
{
    /// <summary>
    /// Applies <see cref="DMTerrainSplatRenderProfile"/> to TerrainLayer assets (height remap) and terrain materials (packed parallax flags).
    /// </summary>
    public static class DMTerrainSplatRenderApplier
    {
        public static class ShaderPropertyIds
        {
            public const string SplatLayerParams = "_DMSplatLayerParams";
            public static readonly int SplatLayerParamsId = Shader.PropertyToID(SplatLayerParams);
        }

        public static void ApplyToTerrain(Terrain terrain, DMTerrainSplatRenderProfile profile)
        {
            if (terrain == null || profile == null)
                return;

            profile.EnsureSlots();
            TerrainData data = terrain.terrainData;
            if (data == null)
                return;

            TerrainLayer[] layers = data.terrainLayers;
            if (layers == null || layers.Length == 0)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                for (int i = 0; i < layers.Length; i++)
                {
                    TerrainLayer layer = layers[i];
                    if (layer == null)
                        continue;

                    DMTerrainSplatLayerEntry entry = profile.FindForTerrainLayer(layer, i);
                    if (entry == null)
                        continue;

                    ApplyToTerrainLayer(layer, entry.Resolve());
                }
            }
#endif

            PushMaterialLayerParams(terrain, profile, layers);
        }

        public static void ApplyToAllTerrains(DMTerrainSplatRenderProfile profile)
        {
            if (profile == null)
                return;

            Terrain[] terrains = Terrain.activeTerrains;
            if (terrains == null || terrains.Length == 0)
                return;

            for (int i = 0; i < terrains.Length; i++)
                ApplyToTerrain(terrains[i], profile);
        }

        public static void ApplyToTerrainLayer(TerrainLayer layer, in DMTerrainLayerRenderResolved resolved)
        {
            if (layer == null)
                return;

            ApplyHeightRemap(layer, resolved);
            ApplyNormalScale(layer, resolved);
            ApplyTileSize(layer, resolved);
        }

        private static void ApplyHeightRemap(TerrainLayer layer, in DMTerrainLayerRenderResolved resolved)
        {
            Vector4 min = layer.maskMapRemapMin;
            Vector4 max = layer.maskMapRemapMax;

            if (!resolved.UseHeightBasedBlend)
            {
                max.z = min.z;
                layer.maskMapRemapMin = min;
                layer.maskMapRemapMax = max;
                return;
            }

            float contrast = Mathf.Max(0.25f, resolved.HeightBlendContrast);
            float bias = resolved.HeightBlendBias;
            float center = 0.5f + bias;
            float half = 0.5f / contrast;
            min.z = Mathf.Clamp01(center - half);
            max.z = Mathf.Clamp01(center + half);
            layer.maskMapRemapMin = min;
            layer.maskMapRemapMax = max;
        }

        private static void ApplyNormalScale(TerrainLayer layer, in DMTerrainLayerRenderResolved resolved)
        {
            if (resolved.NormalScaleOverride >= 0f)
                layer.normalScale = resolved.NormalScaleOverride;
        }

        private static void ApplyTileSize(TerrainLayer layer, in DMTerrainLayerRenderResolved resolved)
        {
            if (resolved.TileSizeWorldMeters <= 0f)
                return;

            layer.tileSize = new Vector2(resolved.TileSizeWorldMeters, resolved.TileSizeWorldMeters);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(layer);
#endif
        }

        private static void PushMaterialLayerParams(
            Terrain terrain,
            DMTerrainSplatRenderProfile profile,
            TerrainLayer[] layers)
        {
            Material mat = terrain.materialTemplate;
            if (mat == null || !mat.HasProperty(ShaderPropertyIds.SplatLayerParamsId))
                return;

            int count = Mathf.Min(layers.Length, DMTerrainSplatLayerEntry.MaxSplatSlots);
            var packed = new Vector4[DMTerrainSplatLayerEntry.MaxSplatSlots];
            for (int i = 0; i < DMTerrainSplatLayerEntry.MaxSplatSlots; i++)
                packed[i] = Vector4.zero;

            for (int i = 0; i < count; i++)
            {
                DMTerrainSplatLayerEntry entry = profile.FindForTerrainLayer(layers[i], i);
                if (entry == null)
                    continue;

                DMTerrainLayerRenderResolved r = entry.Resolve();
                packed[i] = new Vector4(
                    r.EnableParallax ? 1f : 0f,
                    r.ParallaxStrength,
                    r.UseHeightBasedBlend ? 1f : 0f,
                    r.HeightBlendContrast);
            }

            mat.SetVectorArray(ShaderPropertyIds.SplatLayerParamsId, packed);
        }
    }
}

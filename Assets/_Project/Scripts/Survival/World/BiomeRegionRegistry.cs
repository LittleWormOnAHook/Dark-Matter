using System.Collections.Generic;
using UnityEngine;

namespace Project.Survival.World
{
    [CreateAssetMenu(
        fileName = "BiomeRegionRegistry",
        menuName = "Dark Matter Genesis/World/Biome Region Registry")]
    public class BiomeRegionRegistry : ScriptableObject
    {
        private static BiomeRegionRegistry cached;

        [SerializeField] private BiomeRegionData[] regions;

        public static BiomeRegionRegistry Instance
        {
            get
            {
                if (cached == null)
                    cached = Resources.Load<BiomeRegionRegistry>("World/BiomeRegionRegistry");

                return cached;
            }
        }

        public static IReadOnlyList<BiomeRegionData> GetAllRegions()
        {
            List<BiomeRegionData> result = new List<BiomeRegionData>();
            BiomeRegionRegistry registry = Instance;
            if (registry?.regions != null)
            {
                for (int i = 0; i < registry.regions.Length; i++)
                {
                    if (registry.regions[i] != null)
                        result.Add(registry.regions[i]);
                }
            }

            if (result.Count == 0)
            {
                BiomeRegionData[] loaded = Resources.LoadAll<BiomeRegionData>("World/Biomes");
                for (int i = 0; i < loaded.Length; i++)
                {
                    if (loaded[i] != null)
                        result.Add(loaded[i]);
                }
            }

            result.Sort((a, b) => a.campaignUnlockOrder.CompareTo(b.campaignUnlockOrder));
            return result;
        }

        public static BiomeRegionData Resolve(IoSurfaceRegionId regionId)
        {
            if (regionId == IoSurfaceRegionId.None)
                return null;

            foreach (BiomeRegionData region in GetAllRegions())
            {
                if (region != null && region.regionId == regionId)
                    return region;
            }

            return null;
        }

        public static BiomeRegionData ResolveAtMapUv(Vector2 mapUv)
        {
            BiomeRegionData best = null;
            float bestWeight = -1f;

            foreach (BiomeRegionData region in GetAllRegions())
            {
                if (region == null)
                    continue;

                float du = mapUv.x - region.mapCenterU;
                float dv = mapUv.y - region.mapCenterV;
                float dist = Mathf.Sqrt(du * du + dv * dv);
                if (dist > region.mapRadius)
                    continue;

                float weight = 1f - dist / region.mapRadius;
                if (weight > bestWeight)
                {
                    bestWeight = weight;
                    best = region;
                }
            }

            return best;
        }
    }
}

using Project.Map;
using Project.Survival.World;
using UnityEngine;

namespace Project.Data
{
    /// <summary>
    /// Authoring data for one Io surface biome region on the full-moon map (IO-W0-01).
    /// </summary>
    [CreateAssetMenu(fileName = "BiomeRegion_", menuName = "Dark Matter/World/Biome Region Data")]
    public class BiomeRegionData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private IoSurfaceRegionId regionId;
        [SerializeField] private string displayName;
        [TextArea(2, 4)]
        [SerializeField] private string designSummary;

        [Header("Map placement (normalized 0-1, bottom-left origin)")]
        [SerializeField] private Vector2 mapCenterUv;
        [SerializeField] private float mapInfluenceRadius = 0.15f;

        [Header("Pressures & traversal")]
        [SerializeField] private bool footOnlySurface;
        [SerializeField] private int campaignUnlockOrder;
        [SerializeField] private float maxLocalElevationMeters = 1000f;

        [Header("Thermal baseline (tidally locked)")]
        [Tooltip("Positive = hotter than equatorial mean; negative = colder.")]
        [SerializeField] private float thermalBias = 0f;

        public IoSurfaceRegionId RegionId => regionId;
        public string DisplayName => displayName;
        public string DesignSummary => designSummary;
        public Vector2 MapCenterUv => mapCenterUv;
        public float MapInfluenceRadius => mapInfluenceRadius;
        public bool FootOnlySurface => footOnlySurface;
        public int CampaignUnlockOrder => campaignUnlockOrder;
        public float MaxLocalElevationMeters => maxLocalElevationMeters;
        public float ThermalBias => thermalBias;

        public Color MapColor => IoWorldMapPalette.GetBiomeColor(regionId);
    }
}

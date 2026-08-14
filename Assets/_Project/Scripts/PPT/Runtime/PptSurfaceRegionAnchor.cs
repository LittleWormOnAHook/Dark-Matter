using Project.Survival.World;
using UnityEngine;

namespace Project.PPT
{
    /// <summary>
    /// Authored center point for a surface biome region or hazard tag used by PPT direction replies.
    /// </summary>
    public class PptSurfaceRegionAnchor : MonoBehaviour
    {
        [SerializeField] private IoSurfaceRegionId surfaceRegion = IoSurfaceRegionId.SulfurPlains;
        [SerializeField] private string displayName = "Sulfur Plains";
        [SerializeField] private string hazardTag;
        [SerializeField] private float directionRadiusMeters = 250f;

        public IoSurfaceRegionId SurfaceRegion => surfaceRegion;
        public string DisplayName => displayName;
        public string HazardTag => hazardTag;
        public Vector3 Center => transform.position;
        public float DirectionRadiusMeters => directionRadiusMeters;
    }
}

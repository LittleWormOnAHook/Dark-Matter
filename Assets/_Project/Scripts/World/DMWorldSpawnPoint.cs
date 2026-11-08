using UnityEngine;

namespace Project.World
{
    /// <summary>
    /// ioswitch: marks a world spawn (e.g. "DM_IoSpawn"). DMSceneStack reads this transform's position and
    /// rotation when the persistent Kade enters the world. Optionally re-places the marker from a
    /// latitude/longitude using a simple equirectangular mapping around a calibrated origin
    /// (no Io start lat/long is configured in the project yet, so it is OFF by default).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMWorldSpawnPoint : MonoBehaviour
    {
        [Header("Geographic placement (optional)")]
        [SerializeField] private bool useGeoPlacement;
        [Tooltip("Target start latitude in degrees (+N).")]
        [SerializeField] private double latitude;
        [Tooltip("Target start longitude in degrees (+E).")]
        [SerializeField] private double longitude;
        [Tooltip("Latitude/longitude that sits at originWorldXZ.")]
        [SerializeField] private double originLatitude;
        [SerializeField] private double originLongitude;
        [SerializeField] private Vector2 originWorldXZ;
        [Tooltip("World metres per degree. Io: radius 1821.6 km -> ~31,793 m/deg at true scale; set to the map's scale.")]
        [SerializeField] private float metersPerDegree = 31793f;
        [Tooltip("Height above the ground hit when geo-placed.")]
        [SerializeField] private float groundOffset = 0.05f;

        private bool geoApplied;

        public bool UsesGeoPlacement => useGeoPlacement;

        /// <summary>Moves the marker to the configured lat/long once (XZ from the mapping, Y from a ground raycast).</summary>
        public void ApplyGeoPlacement()
        {
            if (!useGeoPlacement || geoApplied)
                return;
            geoApplied = true;

            double dLat = latitude - originLatitude;
            double dLon = longitude - originLongitude;
            double cosLat = System.Math.Cos(originLatitude * System.Math.PI / 180.0);
            float x = originWorldXZ.x + (float)(dLon * cosLat * metersPerDegree);
            float z = originWorldXZ.y + (float)(dLat * metersPerDegree);

            Vector3 p = transform.position;
            Vector3 top = new Vector3(x, p.y + 2000f, z);
            if (Physics.Raycast(top, Vector3.down, out RaycastHit hit, 4000f, ~0, QueryTriggerInteraction.Ignore))
                p = new Vector3(x, hit.point.y + groundOffset, z);
            else
                p = new Vector3(x, p.y, z);
            transform.position = p;
        }
    }
}

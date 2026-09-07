using UnityEngine;

namespace Project.Map
{
    /// <summary>
    /// Scene anchor for world-map UV alignment. Place on the hierarchy object "Map Zero"
    /// at the map texture origin corner (default: minimum world XZ = UV 0,0).
    /// This is NOT the player spawn and NOT Unity world (0,0).
    /// </summary>
    [DisallowMultipleComponent]
    public class WorldMapOriginMarker : MonoBehaviour
    {
        public const string DefaultObjectName = "Map Zero";

        [SerializeField] private Vector2 worldSizeMeters = new Vector2(
            WorldMapProvider.MultiTerrainWorldSizeMeters,
            WorldMapProvider.MultiTerrainWorldSizeMeters);
        [Tooltip("When enabled, world span comes from the MAP art plane bounds in-scene.")]
        [SerializeField] private bool syncSizeFromMapArt = false;
        [Tooltip("When enabled, this transform marks UV (0,0). When disabled, it marks the map center (Map Zero at world origin).")]
        [SerializeField] private bool originIsMinCorner = false;
        [Tooltip("When MAP art exists, snap UV origin to the art plane min-XZ corner (recommended).")]
        [SerializeField] private bool snapOriginToMapArtMinCorner = true;

        public Vector2 WorldSizeMeters => worldSizeMeters;
        public bool OriginIsMinCorner => originIsMinCorner;

        public static WorldMapOriginMarker FindInScene()
        {
            WorldMapOriginMarker[] markers = FindObjectsByType<WorldMapOriginMarker>(FindObjectsInactive.Include);
            if (markers.Length > 0)
                return markers[0];

            GameObject named = GameObject.Find(DefaultObjectName);
            return named != null ? named.GetComponent<WorldMapOriginMarker>() : null;
        }

        public Bounds BuildWorldBounds()
        {
            WorldMapArtReference art = WorldMapArtReference.FindInScene();
            if (syncSizeFromMapArt && art != null)
            {
                Bounds artBounds = art.GetWorldBounds();
                if (artBounds.size.x > 0.01f && artBounds.size.z > 0.01f)
                    return BuildBoundsFromArt(artBounds);
            }

            Vector2 size = worldSizeMeters;
            Vector3 flatSize = new Vector3(size.x, 100f, size.y);
            Vector3 minCorner = originIsMinCorner
                ? new Vector3(transform.position.x, 0f, transform.position.z)
                : transform.position - new Vector3(flatSize.x * 0.5f, 0f, flatSize.z * 0.5f);
            return new Bounds(minCorner + flatSize * 0.5f, flatSize);
        }

        private Bounds BuildBoundsFromArt(Bounds artBounds)
        {
            Vector3 flatSize = new Vector3(artBounds.size.x, 100f, artBounds.size.z);

            if (!originIsMinCorner)
                return new Bounds(
                    new Vector3(artBounds.center.x, 0f, artBounds.center.z),
                    flatSize);

            Vector3 minCorner = snapOriginToMapArtMinCorner
                ? new Vector3(artBounds.min.x, 0f, artBounds.min.z)
                : new Vector3(transform.position.x, 0f, transform.position.z);

            return new Bounds(minCorner + flatSize * 0.5f, flatSize);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Bounds bounds = BuildWorldBounds();
            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.85f);
            Vector3 flat = new Vector3(bounds.size.x, 0.05f, bounds.size.z);
            Gizmos.DrawWireCube(new Vector3(bounds.center.x, transform.position.y, bounds.center.z), flat);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, 2f);
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureMapZeroComponent()
        {
            if (!Application.isPlaying)
                return;

            GameObject named = GameObject.Find(DefaultObjectName);
            if (named == null || named.GetComponent<WorldMapOriginMarker>() != null)
                return;

            named.AddComponent<WorldMapOriginMarker>();
        }
    }
}

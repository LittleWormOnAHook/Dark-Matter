using Project.Map;
using UnityEngine;

namespace Project.Survival.World
{
    /// <summary>
    /// W1 greybox anchors: Command Center colony, B6 hub, vehicle path tags,
    /// and a fog reveal test hook for colony + B6 sector (IO-W1-01).
    /// </summary>
    [DisallowMultipleComponent]
    public class IoW1BlockoutMarkers : MonoBehaviour
    {
        public const string RootName = "IoW1_Blockout";

        [Header("Anchors (world meters)")]
        [SerializeField] private Transform commandCenterAnchor;
        [SerializeField] private Transform b6HubAnchor;
        [SerializeField] private Transform[] vehiclePathTags;

        [Header("Fog reveal test (IO-W1-01)")]
        [SerializeField] private float colonyRevealRadiusMeters = 180f;
        [SerializeField] private float b6SectorRevealRadiusMeters = 420f;
        [SerializeField] private bool revealOnStart = true;

        public Transform CommandCenterAnchor => commandCenterAnchor;
        public Transform B6HubAnchor => b6HubAnchor;

        private void Start()
        {
            if (revealOnStart)
                RevealColonyAndB6SectorFog();
        }

        [ContextMenu("Reveal Colony + B6 Fog Sector")]
        public void RevealColonyAndB6SectorFog()
        {
            MapFogOfWar fog = MapFogOfWar.Instance ?? MapFogOfWar.EnsureExists();
            if (fog == null)
            {
                Debug.LogWarning("[IoW1BlockoutMarkers] MapFogOfWar missing — fog sector test skipped.");
                return;
            }

            Vector3 colony = commandCenterAnchor != null
                ? commandCenterAnchor.position
                : IoSurfaceWorldScale.MapUvToWorld(IoSurfaceWorldScale.CommandCenterMapUv);

            Vector3 hub = b6HubAnchor != null
                ? b6HubAnchor.position
                : IoSurfaceWorldScale.MapUvToWorld(IoSurfaceWorldScale.BasaltHighlandsHubMapUv);

            fog.RevealCircle(colony, colonyRevealRadiusMeters, edgeSoftnessMeters: 24f);
            fog.RevealCircle(hub, b6SectorRevealRadiusMeters, edgeSoftnessMeters: 40f);
            // RevealScanAt uploads the fog texture after the sector stamps above.
            fog.RevealScanAt(colony);
        }

#if UNITY_EDITOR
        public void EditorBindAnchors(Transform colony, Transform hub, Transform[] pathTags)
        {
            commandCenterAnchor = colony;
            b6HubAnchor = hub;
            vehiclePathTags = pathTags;
        }
#endif

        private void OnDrawGizmos()
        {
            if (commandCenterAnchor != null)
            {
                Gizmos.color = new Color(0.83f, 0.63f, 0.09f, 0.85f);
                Gizmos.DrawWireSphere(commandCenterAnchor.position, 12f);
            }

            if (b6HubAnchor != null)
            {
                Gizmos.color = new Color(0.55f, 0.45f, 0.38f, 0.7f);
                Gizmos.DrawWireSphere(b6HubAnchor.position, 40f);
            }

            if (vehiclePathTags == null)
                return;

            Gizmos.color = new Color(0.75f, 0.18f, 0.48f, 0.8f);
            for (int i = 0; i < vehiclePathTags.Length; i++)
            {
                Transform tag = vehiclePathTags[i];
                if (tag == null)
                    continue;
                Gizmos.DrawWireCube(tag.position, new Vector3(8f, 2f, 8f));
                if (i > 0 && vehiclePathTags[i - 1] != null)
                    Gizmos.DrawLine(vehiclePathTags[i - 1].position, tag.position);
            }
        }
    }
}

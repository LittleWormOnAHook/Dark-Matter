using UnityEngine;

namespace Project.World
{
    /// <summary>
    /// Marker on a baked chunk reflection probe in a Terrain_X_Y_Content scene.
    /// <see cref="DmReflectionProbeRingManager"/> tiers probes by distance to this transform.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ReflectionProbe))]
    public sealed class DmChunkReflectionProbe : MonoBehaviour
    {
        [SerializeField] private int tileX = -1;
        [SerializeField] private int tileZ = -1;

        public int TileX => tileX;
        public int TileZ => tileZ;
        public ReflectionProbe Probe => _probe != null ? _probe : (_probe = GetComponent<ReflectionProbe>());

        private ReflectionProbe _probe;

        private void Awake()
        {
            _probe = GetComponent<ReflectionProbe>();
            TryParseTileFromSceneName();
        }

        private void TryParseTileFromSceneName()
        {
            if (tileX >= 0 && tileZ >= 0)
                return;

            string sceneName = gameObject.scene.name;
            if (string.IsNullOrEmpty(sceneName))
                return;

            if (!DmTerrainContentSceneNames.TryParseContentScene(sceneName, out int x, out int z))
                return;

            tileX = x;
            tileZ = z;
        }

        public void SetTileCoordinates(int x, int z)
        {
            tileX = x;
            tileZ = z;
        }
    }
}

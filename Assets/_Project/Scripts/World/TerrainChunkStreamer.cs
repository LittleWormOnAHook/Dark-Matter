using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Project.World
{
    /// <summary>
    /// Gaia-style streaming for terrains already parented in the hierarchy.
    /// Keeps the N nearest chunks drawn/collidable around the player (default 3).
    /// Add this to the folder that holds the 16 chunk terrains.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-40)]
    public class TerrainChunkStreamer : MonoBehaviour
    {
        public enum UnloadMode
        {
            HideDrawAndCollider = 0,
            DisableGameObject = 1
        }

        [Header("Source")]
        [SerializeField] private Transform chunkRoot;
        [SerializeField] private bool includeInactiveChildren = true;
        [SerializeField] private bool collectOnAwake = true;

        [Header("Player")]
        [SerializeField] private Transform player;
        [SerializeField] private bool autoFindPlayer = true;

        [Header("Streaming")]
        [SerializeField, Min(1)] private int maxActiveChunks = 3;
        [SerializeField, Min(0.05f)] private float updateInterval = 0.2f;
        [SerializeField] private UnloadMode unloadMode = UnloadMode.HideDrawAndCollider;
        [SerializeField] private bool setNeighborsOnActive = true;
        [SerializeField] private bool streamInEditor = false;

        private readonly List<Chunk> _chunks = new List<Chunk>(16);
        private readonly List<int> _order = new List<int>(16);
        private readonly HashSet<int> _active = new HashSet<int>();
        private float _nextUpdate;
        private static readonly Regex GridName = new Regex(@"(\d+)[_\-](\d+)", RegexOptions.Compiled);

        private sealed class Chunk
        {
            public Transform Transform;
            public Terrain Terrain;
            public TerrainCollider Collider;
            public int GridX = int.MinValue;
            public int GridZ = int.MinValue;
            public Vector3 Center;
            public float ExtentX;
            public float ExtentZ;
        }

        private void Reset()
        {
            chunkRoot = transform;
        }

        private void Awake()
        {
            if (chunkRoot == null)
                chunkRoot = transform;
            if (collectOnAwake)
                CollectChunks();
            ResolvePlayer();
        }

        private void OnEnable()
        {
            _nextUpdate = 0f;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying && !streamInEditor)
                return;

            if (Time.unscaledTime < _nextUpdate)
                return;

            _nextUpdate = Time.unscaledTime + updateInterval;
            if (player == null)
                ResolvePlayer();
            RefreshActiveChunks();
        }

        [ContextMenu("Collect Chunks")]
        public void CollectChunks()
        {
            _chunks.Clear();
            _active.Clear();
            if (chunkRoot == null)
                chunkRoot = transform;

            Terrain[] terrains = chunkRoot.GetComponentsInChildren<Terrain>(includeInactiveChildren);
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                if (terrain == null || terrain.terrainData == null)
                    continue;

                Vector3 pos = terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                Chunk chunk = new Chunk
                {
                    Transform = terrain.transform,
                    Terrain = terrain,
                    Collider = terrain.GetComponent<TerrainCollider>(),
                    Center = pos + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f),
                    ExtentX = size.x * 0.5f,
                    ExtentZ = size.z * 0.5f
                };
                ParseGrid(terrain.name, out chunk.GridX, out chunk.GridZ);
                _chunks.Add(chunk);
            }

            RefreshNeighborsIfNeeded();
        }

        public int ChunkCount => _chunks.Count;
        public int ActiveCount => _active.Count;

        private void ResolvePlayer()
        {
            if (!autoFindPlayer && player != null)
                return;

            if (player != null)
                return;

            GameObject tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null)
            {
                player = tagged.transform;
                return;
            }

            GameObject v7 = GameObject.Find("Player_v7");
            if (v7 != null)
                player = v7.transform;
        }

        private void RefreshActiveChunks()
        {
            if (_chunks.Count == 0)
                CollectChunks();
            if (_chunks.Count == 0)
                return;

            Vector3 sample = player != null ? player.position : Vector3.zero;
            _order.Clear();
            for (int i = 0; i < _chunks.Count; i++)
                _order.Add(i);

            _order.Sort((a, b) => DistanceSq(sample, _chunks[a]).CompareTo(DistanceSq(sample, _chunks[b])));

            int keep = Mathf.Min(maxActiveChunks, _order.Count);
            _active.Clear();
            for (int i = 0; i < keep; i++)
                _active.Add(_order[i]);

            for (int i = 0; i < _chunks.Count; i++)
                ApplyChunk(_chunks[i], _active.Contains(i));

            RefreshNeighborsIfNeeded();
        }

        private static float DistanceSq(Vector3 player, Chunk chunk)
        {
            float dx = player.x - chunk.Center.x;
            float dz = player.z - chunk.Center.z;
            float ax = Mathf.Max(0f, Mathf.Abs(dx) - chunk.ExtentX);
            float az = Mathf.Max(0f, Mathf.Abs(dz) - chunk.ExtentZ);
            return ax * ax + az * az;
        }

        private void ApplyChunk(Chunk chunk, bool on)
        {
            if (chunk == null || chunk.Transform == null)
                return;

            if (unloadMode == UnloadMode.DisableGameObject)
            {
                if (chunk.Transform.gameObject.activeSelf != on)
                    chunk.Transform.gameObject.SetActive(on);
                return;
            }

            if (!chunk.Transform.gameObject.activeSelf)
                chunk.Transform.gameObject.SetActive(true);

            if (chunk.Terrain != null && chunk.Terrain.enabled != on)
                chunk.Terrain.enabled = on;

            if (chunk.Collider != null && chunk.Collider.enabled != on)
                chunk.Collider.enabled = on;

            if (chunk.Terrain != null)
            {
                chunk.Terrain.drawHeightmap = on;
                chunk.Terrain.drawTreesAndFoliage = on;
            }
        }

        private void RefreshNeighborsIfNeeded()
        {
            if (!setNeighborsOnActive)
                return;

            for (int i = 0; i < _chunks.Count; i++)
            {
                Chunk chunk = _chunks[i];
                if (chunk.Terrain == null || chunk.GridX == int.MinValue)
                    continue;

                bool on = unloadMode == UnloadMode.DisableGameObject
                    ? chunk.Transform != null && chunk.Transform.gameObject.activeInHierarchy
                    : chunk.Terrain.enabled;

                Terrain left = on ? FindActiveGrid(chunk.GridX - 1, chunk.GridZ) : null;
                Terrain right = on ? FindActiveGrid(chunk.GridX + 1, chunk.GridZ) : null;
                Terrain bottom = on ? FindActiveGrid(chunk.GridX, chunk.GridZ - 1) : null;
                Terrain top = on ? FindActiveGrid(chunk.GridX, chunk.GridZ + 1) : null;
                chunk.Terrain.SetNeighbors(left, top, right, bottom);
            }
        }

        private Terrain FindActiveGrid(int x, int z)
        {
            for (int i = 0; i < _chunks.Count; i++)
            {
                Chunk chunk = _chunks[i];
                if (chunk.GridX != x || chunk.GridZ != z || chunk.Terrain == null)
                    continue;

                bool on = unloadMode == UnloadMode.DisableGameObject
                    ? chunk.Transform != null && chunk.Transform.gameObject.activeInHierarchy
                    : chunk.Terrain.enabled;
                if (on)
                    return chunk.Terrain;
            }

            return null;
        }

        private static void ParseGrid(string name, out int x, out int z)
        {
            x = int.MinValue;
            z = int.MinValue;
            if (string.IsNullOrEmpty(name))
                return;

            Match match = GridName.Match(name);
            if (!match.Success)
                return;

            x = int.Parse(match.Groups[1].Value);
            z = int.Parse(match.Groups[2].Value);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_chunks.Count == 0)
                CollectChunks();

            for (int i = 0; i < _chunks.Count; i++)
            {
                Chunk chunk = _chunks[i];
                bool on = _active.Contains(i);
                Gizmos.color = on ? new Color(0.83f, 0.63f, 0.09f, 0.35f) : new Color(0.11f, 0.16f, 0.22f, 0.2f);
                Gizmos.DrawCube(chunk.Center, new Vector3(chunk.ExtentX * 2f, 2f, chunk.ExtentZ * 2f));
            }
        }
#endif
    }
}
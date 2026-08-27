using System.Collections.Generic;
using Project.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_RENDER_PIPELINE_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Project.World
{
    /// <summary>
    /// Distance-tiers chunk reflection probes and disables the legacy BOTD world probe at runtime.
    /// Tiering uses distance to each probe transform, not tile ownership.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(55)]
    public sealed class DmReflectionProbeRingManager : MonoBehaviour
    {
        public const string BotdWorldProbeName = "BOTD Reflection Probe(Clone)";

        public const float NearTierDistanceMeters = 150f;
        public const float FarTierDistanceMeters = 400f;

        private static DmReflectionProbeRingManager _instance;

        [Header("Runtime diagnostics")]
        [SerializeField] private int playerTileX = -1;
        [SerializeField] private int playerTileZ = -1;
        [SerializeField] private int activeProbeCount = 0;
        [SerializeField] private int registeredProbeCount = 0;

        private Transform _playerTransform;
        private readonly List<DmChunkReflectionProbe> _probes = new List<DmChunkReflectionProbe>();
        private Vector3 _lastPlayerPosition;
        private bool _botdProbeDisabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (!Application.isPlaying || _instance != null)
                return;

            GameObject host = new GameObject(nameof(DmReflectionProbeRingManager));
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<DmReflectionProbeRingManager>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
            DisableBotdWorldProbe();
            RefreshProbeRegistry();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                SceneManager.sceneUnloaded -= HandleSceneUnloaded;
                _instance = null;
            }
        }

        private void OnEnable()
        {
            DisableBotdWorldProbe();
        }

        private void LateUpdate()
        {
            if (!ResolvePlayer())
                return;

            Vector3 playerPosition = _playerTransform.position;
            if ((playerPosition - _lastPlayerPosition).sqrMagnitude < 0.25f && _probes.Count == registeredProbeCount)
                return;

            _lastPlayerPosition = playerPosition;
            UpdatePlayerTile(playerPosition);
            ApplyDistanceTiers(playerPosition);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshProbeRegistry();
            DisableBotdWorldProbe();
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            RefreshProbeRegistry();
        }

        private void RefreshProbeRegistry()
        {
            _probes.Clear();

            DmChunkReflectionProbe[] found = FindObjectsByType<DmChunkReflectionProbe>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < found.Length; i++)
            {
                DmChunkReflectionProbe marker = found[i];
                if (marker == null)
                    continue;

                _probes.Add(marker);
            }

            registeredProbeCount = _probes.Count;
            if (_playerTransform != null)
                ApplyDistanceTiers(_playerTransform.position);
        }

        private bool ResolvePlayer()
        {
            if (_playerTransform != null)
                return true;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
                player = GameObject.Find("Player_v7");

            if (player == null)
                return false;

            _playerTransform = player.transform;
            _lastPlayerPosition = _playerTransform.position;
            UpdatePlayerTile(_lastPlayerPosition);
            return true;
        }

        private void UpdatePlayerTile(Vector3 playerPosition)
        {
            double localX = playerPosition.x - DmTerrainContentSceneNames.TerrainOriginX;
            double localZ = playerPosition.z - DmTerrainContentSceneNames.TerrainOriginZ;

            playerTileX = Mathf.Clamp(
                Mathf.FloorToInt((float)(localX / DmTerrainContentSceneNames.TerrainTileSizeMeters)),
                0,
                DmTerrainContentSceneNames.TerrainGridTiles - 1);
            playerTileZ = Mathf.Clamp(
                Mathf.FloorToInt((float)(localZ / DmTerrainContentSceneNames.TerrainTileSizeMeters)),
                0,
                DmTerrainContentSceneNames.TerrainGridTiles - 1);
        }

        private void ApplyDistanceTiers(Vector3 playerPosition)
        {
            int enabledCount = 0;

            for (int i = 0; i < _probes.Count; i++)
            {
                DmChunkReflectionProbe marker = _probes[i];
                if (marker == null)
                    continue;

                ReflectionProbe probe = marker.Probe;
                if (probe == null)
                    continue;

                if (!HasBakedCubemap(probe))
                {
                    SetProbeActive(probe, false);
                    continue;
                }

                float distance = Vector3.Distance(playerPosition, marker.transform.position);
                if (distance > FarTierDistanceMeters)
                {
                    SetProbeActive(probe, false);
                    continue;
                }

                if (distance <= NearTierDistanceMeters)
                {
                    SetProbeTier(probe, importance: 1, weight: 1f);
                    enabledCount++;
                }
                else
                {
                    SetProbeTier(probe, importance: 0, weight: 0.5f);
                    enabledCount++;
                }
            }

            activeProbeCount = enabledCount;
        }

        private static bool HasBakedCubemap(ReflectionProbe probe)
        {
            return probe.customBakedTexture != null || probe.bakedTexture != null;
        }

        private static void SetProbeActive(ReflectionProbe probe, bool active)
        {
            probe.enabled = active;
#if UNITY_RENDER_PIPELINE_HDRP
            HDAdditionalReflectionData hdProbe = probe.GetComponent<HDAdditionalReflectionData>();
            if (hdProbe != null)
                hdProbe.enabled = active;
#endif
        }

        private static void SetProbeTier(ReflectionProbe probe, int importance, float weight)
        {
            probe.enabled = true;
            probe.importance = importance;
#if UNITY_RENDER_PIPELINE_HDRP
            HDAdditionalReflectionData hdProbe = probe.GetComponent<HDAdditionalReflectionData>();
            if (hdProbe != null)
            {
                hdProbe.enabled = true;
                hdProbe.weight = weight;
            }
#endif
        }

        private void DisableBotdWorldProbe()
        {
            if (_botdProbeDisabled)
                return;

            GameObject botdProbeObject = GameObject.Find(BotdWorldProbeName);
            if (botdProbeObject == null)
                return;

            ReflectionProbe legacyProbe = botdProbeObject.GetComponent<ReflectionProbe>();
            if (legacyProbe != null)
                legacyProbe.enabled = false;

#if UNITY_RENDER_PIPELINE_HDRP
            HDAdditionalReflectionData hdProbe = botdProbeObject.GetComponent<HDAdditionalReflectionData>();
            if (hdProbe != null)
                hdProbe.enabled = false;
#endif

            botdProbeObject.SetActive(false);
            _botdProbeDisabled = true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!ResolvePlayer())
                return;

            Vector3 center = _playerTransform.position;
            Gizmos.color = new Color(0.75f, 0.2f, 0.55f, 0.35f);
            Gizmos.DrawWireSphere(center, NearTierDistanceMeters);
            Gizmos.color = new Color(0.75f, 0.2f, 0.55f, 0.2f);
            Gizmos.DrawWireSphere(center, FarTierDistanceMeters);
        }
#endif
    }
}

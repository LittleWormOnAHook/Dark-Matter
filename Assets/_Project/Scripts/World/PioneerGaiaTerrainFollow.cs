using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Legacy play-mode loader. TLM RefreshRuntimePlayerLoading now streams the
/// 4 nearest tiles. This component stays on Player_v7 for the Bind menu but
/// does not issue extra load bounds (that was stacking to 6 terrains).
/// Play mode: one hero tile stays fat; the other live tiles drop shadow
/// casting, shorten basemap, and disable colliders until the player is close.
/// Pixel Error is never changed (mismatched PE cracks seams).
/// </summary>
[DefaultExecutionOrder(50)]
public class PioneerGaiaTerrainFollow : MonoBehaviour
{
    public float loadRange = 1800f;

    [Header("Cheap neighbors")]
    public float neighborBasemapDistance = 300f;
    public float colliderEnableDistance = 120f;

    class OriginalQuality
    {
        public float basemapDistance;
        public ShadowCastingMode shadowCastingMode;
        public bool colliderEnabled;
    }

    readonly Dictionary<EntityId, OriginalQuality> m_originals = new Dictionary<EntityId, OriginalQuality>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureOnPlayer()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        GameObject player = GameObject.Find("Player_v7");
        if (player == null || player.GetComponent<PioneerGaiaTerrainFollow>() != null)
        {
            return;
        }

        player.AddComponent<PioneerGaiaTerrainFollow>();
    }

    void LateUpdate()
    {
        // TLM owns play-mode streaming. Do not call UpdateTerrainLoadState here.
        if (!Application.isPlaying)
        {
            return;
        }

        ApplyCheapNeighbors();
    }

    void OnDisable()
    {
        RestoreAll();
    }

    void ApplyCheapNeighbors()
    {
        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains == null || terrains.Length == 0)
        {
            return;
        }

        Vector3 player = transform.position;
        Terrain hero = PickHero(terrains, player);
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            if (terrain == null || terrain.terrainData == null)
            {
                continue;
            }

            RememberOriginal(terrain);
            bool isHero = terrain == hero;
            float dist = DistanceXzToTerrain(player, terrain);
            bool colliderOn = isHero || dist <= colliderEnableDistance;
            ApplyQuality(terrain, isHero, colliderOn);
        }
    }

    static Terrain PickHero(Terrain[] terrains, Vector3 player)
    {
        Terrain containing = null;
        Terrain nearest = null;
        float nearestSqr = float.MaxValue;
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            if (terrain == null || terrain.terrainData == null)
            {
                continue;
            }

            if (ContainsXz(player, terrain))
            {
                containing = terrain;
                break;
            }

            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            Vector3 center = origin + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);
            float dx = player.x - center.x;
            float dz = player.z - center.z;
            float sqr = dx * dx + dz * dz;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = terrain;
            }
        }

        return containing != null ? containing : nearest;
    }

    static bool ContainsXz(Vector3 player, Terrain terrain)
    {
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        return player.x >= origin.x && player.x <= origin.x + size.x
            && player.z >= origin.z && player.z <= origin.z + size.z;
    }

    static float DistanceXzToTerrain(Vector3 player, Terrain terrain)
    {
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float x = Mathf.Clamp(player.x, origin.x, origin.x + size.x);
        float z = Mathf.Clamp(player.z, origin.z, origin.z + size.z);
        float dx = player.x - x;
        float dz = player.z - z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    void RememberOriginal(Terrain terrain)
    {
        EntityId id = terrain.GetEntityId();
        if (m_originals.ContainsKey(id))
        {
            return;
        }

        TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
        m_originals[id] = new OriginalQuality
        {
            basemapDistance = terrain.basemapDistance,
            shadowCastingMode = terrain.shadowCastingMode,
            colliderEnabled = collider == null || collider.enabled
        };
    }

    void ApplyQuality(Terrain terrain, bool isHero, bool colliderOn)
    {
        OriginalQuality original;
        if (!m_originals.TryGetValue(terrain.GetEntityId(), out original))
        {
            return;
        }

        ShadowCastingMode shadows = isHero ? original.shadowCastingMode : ShadowCastingMode.Off;
        if (terrain.shadowCastingMode != shadows)
        {
            terrain.shadowCastingMode = shadows;
        }

        float basemap = isHero
            ? original.basemapDistance
            : Mathf.Min(original.basemapDistance, neighborBasemapDistance);
        if (!Mathf.Approximately(terrain.basemapDistance, basemap))
        {
            terrain.basemapDistance = basemap;
        }

        TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
        if (collider != null && collider.enabled != colliderOn)
        {
            collider.enabled = colliderOn;
        }

        if (!Mathf.Approximately(terrain.heightmapPixelError, 25f))
        {
            terrain.heightmapPixelError = 25f;
        }
    }

    void RestoreAll()
    {
        Terrain[] terrains = Terrain.activeTerrains;
        if (terrains == null)
        {
            m_originals.Clear();
            return;
        }

        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            OriginalQuality original;
            if (terrain == null || !m_originals.TryGetValue(terrain.GetEntityId(), out original))
            {
                continue;
            }

            terrain.shadowCastingMode = original.shadowCastingMode;
            terrain.basemapDistance = original.basemapDistance;
            TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
            if (collider != null)
            {
                collider.enabled = original.colliderEnabled;
            }
        }

        m_originals.Clear();
    }
}

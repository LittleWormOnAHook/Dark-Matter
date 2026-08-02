using UnityEngine;

namespace Project.Survival.World
{
    /// <summary>
    /// Locked W1 main-map scale: 1 Unity unit = 1 meter.
    /// Heightmap source: Assets/_Project/World/WorldMap/Io_Plan_Heightmap.png
    /// </summary>
    public static class IoSurfaceWorldScale
    {
        public const float MetersPerUnit = 1f;

        /// <summary>Horizontal span of the W1 blockout terrain (X and Z).</summary>
        public const float MainMapSpanMeters = 4096f;

        /// <summary>Peak elevation cap for plan heightmap import (0–1000 m).</summary>
        public const float MaxTerrainHeightMeters = 1000f;

        /// <summary>RAW / PNG sample resolution before Unity 2^n+1 resize.</summary>
        public const int PlanHeightmapResolution = 1024;

        /// <summary>Command Center / colony UV on the painted plan disc (bottom-left, V+ north).</summary>
        public static readonly Vector2 CommandCenterMapUv = new Vector2(0.48f, 0.62f);

        /// <summary>B6 hub UV used for exposure + fog sector reveal tests.</summary>
        public static readonly Vector2 BasaltHighlandsHubMapUv = new Vector2(0.48f, 0.60f);

        public static Vector3 MapUvToWorld(Vector2 mapUv, float y = 0f)
        {
            float x = (mapUv.x - 0.5f) * MainMapSpanMeters;
            float z = (mapUv.y - 0.5f) * MainMapSpanMeters;
            return new Vector3(x, y, z);
        }

        public static Vector2 WorldToMapUv(Vector3 world)
        {
            return new Vector2(
                (world.x / MainMapSpanMeters) + 0.5f,
                (world.z / MainMapSpanMeters) + 0.5f);
        }

        /// <summary>
        /// Terrain transform origin so map UV (0.5, 0.5) sits at world XZ origin.
        /// </summary>
        public static Vector3 TerrainOrigin =>
            new Vector3(-MainMapSpanMeters * 0.5f, 0f, -MainMapSpanMeters * 0.5f);
    }
}

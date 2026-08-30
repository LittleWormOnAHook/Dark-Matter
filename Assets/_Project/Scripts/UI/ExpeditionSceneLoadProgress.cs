using System.Collections.Generic;
using Project.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.UI
{
    /// <summary>
    /// Real New Expedition load progress: the 4 live Gaia tiles around the player
    /// plus each tile's content scene. Used by Loading Genesis instead of a fake timer.
    /// </summary>
    public static class ExpeditionSceneLoadProgress
    {
        private const float ReadyTimeoutSeconds = 45f;
        private const float TerrainWeight = 0.72f;
        private const float ContentWeight = 0.28f;

        private static readonly List<Vector2Int> LiveTiles = new List<Vector2Int>(4);
        private static float startedAt = -1f;
        private static bool begun;

        public static void Reset()
        {
            begun = false;
            startedAt = -1f;
            LiveTiles.Clear();
        }

        public static void Begin()
        {
            begun = true;
            startedAt = Time.realtimeSinceStartup;
            LiveTiles.Clear();
            CollectLiveTiles(LiveTiles);
        }

        public static float GetProgress()
        {
            if (!begun)
                Begin();

            if (LiveTiles.Count == 0)
                CollectLiveTiles(LiveTiles);

            if (LiveTiles.Count == 0)
                return 1f;

            float terrain = 0f;
            float content = 0f;
            float inv = 1f / LiveTiles.Count;

            for (int i = 0; i < LiveTiles.Count; i++)
            {
                Vector2Int tile = LiveTiles[i];
                terrain += TerrainCredit(tile.x, tile.y) * inv;
                content += ContentCredit(tile.x, tile.y) * inv;
            }

            return Mathf.Clamp01(terrain * TerrainWeight + content * ContentWeight);
        }

        public static bool IsReady()
        {
            if (!begun)
                Begin();

            if (startedAt > 0f && Time.realtimeSinceStartup - startedAt >= ReadyTimeoutSeconds)
                return true;

            if (LiveTiles.Count == 0)
                CollectLiveTiles(LiveTiles);

            if (LiveTiles.Count == 0)
                return true;

            for (int i = 0; i < LiveTiles.Count; i++)
            {
                Vector2Int tile = LiveTiles[i];
                if (TerrainCredit(tile.x, tile.y) < 0.999f)
                    return false;
                if (ContentCredit(tile.x, tile.y) < 0.999f)
                    return false;
            }

            return true;
        }

        private static void CollectLiveTiles(List<Vector2Int> into)
        {
            into.Clear();

            Vector3 origin = ResolveOrigin();
            float localX = origin.x - (float)DmTerrainContentSceneNames.TerrainOriginX;
            float localZ = origin.z - (float)DmTerrainContentSceneNames.TerrainOriginZ;
            float size = DmTerrainContentSceneNames.TerrainTileSizeMeters;
            int max = DmTerrainContentSceneNames.TerrainGridTiles - 1;

            int tx = Mathf.Clamp(Mathf.FloorToInt(localX / size), 0, max);
            int tz = Mathf.Clamp(Mathf.FloorToInt(localZ / size), 0, max);
            float fracX = localX / size - tx;
            float fracZ = localZ / size - tz;

            int xLo = tx;
            int zLo = tz;
            if (fracX < 0.5f && tx > 0)
                xLo = tx - 1;
            else if (tx >= max)
                xLo = max - 1;

            if (fracZ < 0.5f && tz > 0)
                zLo = tz - 1;
            else if (tz >= max)
                zLo = max - 1;

            xLo = Mathf.Clamp(xLo, 0, max - 1);
            zLo = Mathf.Clamp(zLo, 0, max - 1);

            AddTile(into, xLo, zLo);
            AddTile(into, xLo + 1, zLo);
            AddTile(into, xLo, zLo + 1);
            AddTile(into, xLo + 1, zLo + 1);
        }

        private static void AddTile(List<Vector2Int> into, int x, int z)
        {
            Vector2Int tile = new Vector2Int(x, z);
            if (!into.Contains(tile))
                into.Add(tile);
        }

        private static Vector3 ResolveOrigin()
        {
            GameObject player = GameObject.Find("Player_v7");
            if (player != null)
                return player.transform.position;

            if (Camera.main != null)
                return Camera.main.transform.position;

            return Vector3.zero;
        }

        private static float TerrainCredit(int tileX, int tileZ)
        {
            bool loading = false;
            int count = SceneManager.sceneCount;
            for (int i = 0; i < count; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!DmTerrainContentSceneNames.TryParseRegularTerrainScene(scene.name, out int x, out int z))
                    continue;
                if (x != tileX || z != tileZ)
                    continue;
                if (scene.isLoaded)
                    return 1f;
                if (scene.IsValid())
                    loading = true;
            }

            return loading ? 0.45f : 0f;
        }

        private static float ContentCredit(int tileX, int tileZ)
        {
            string name = DmTerrainContentSceneNames.GetContentSceneName(tileX, tileZ);
            Scene scene = SceneManager.GetSceneByName(name);
            if (scene.IsValid() && scene.isLoaded)
                return 1f;
            if (scene.IsValid())
                return 0.45f;

            string path = DmTerrainContentSceneNames.GetContentSceneAssetPath(tileX, tileZ);
            if (SceneUtility.GetBuildIndexByScenePath(path) < 0)
                return 1f;

            return 0f;
        }
    }
}
using System.Text.RegularExpressions;

namespace Project.World
{
    /// <summary>
    /// Naming contract between Gaia terrain tiles and companion content scenes.
    /// </summary>
    public static class DmTerrainContentSceneNames
    {
        public const string ContentScenePrefix = "Terrain_";
        public const string ContentSceneSuffix = "_Content";

        public const double TerrainOriginX = -4096d;
        public const double TerrainOriginZ = -4096d;
        public const int TerrainTileSizeMeters = 2048;
        public const int TerrainGridTiles = 4;

        private static readonly Regex RegularTerrainPattern = new Regex(
            @"^Terrain_(\d+)_(\d+)-",
            RegexOptions.Compiled);

        private static readonly Regex ContentScenePattern = new Regex(
            @"^Terrain_(\d+)_(\d+)_Content$",
            RegexOptions.Compiled);

        private static readonly Regex SkipTerrainPattern = new Regex(
            @"Impostor|Collider|backup",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool IsRegularGaiaTerrainScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return false;

            if (SkipTerrainPattern.IsMatch(sceneName))
                return false;

            return RegularTerrainPattern.IsMatch(sceneName);
        }

        public static bool TryParseRegularTerrainScene(string sceneName, out int tileX, out int tileZ)
        {
            tileX = -1;
            tileZ = -1;

            if (!IsRegularGaiaTerrainScene(sceneName))
                return false;

            Match match = RegularTerrainPattern.Match(sceneName);
            if (!match.Success)
                return false;

            tileX = int.Parse(match.Groups[1].Value);
            tileZ = int.Parse(match.Groups[2].Value);
            return true;
        }

        public static string GetContentSceneName(int tileX, int tileZ)
        {
            return $"{ContentScenePrefix}{tileX}_{tileZ}{ContentSceneSuffix}";
        }

        public static string GetContentSceneAssetPath(int tileX, int tileZ)
        {
            return $"Assets/_Project/Scenes/{GetContentSceneName(tileX, tileZ)}.unity";
        }

        public static bool TryParseContentScene(string sceneName, out int tileX, out int tileZ)
        {
            tileX = -1;
            tileZ = -1;

            if (string.IsNullOrEmpty(sceneName))
                return false;

            Match match = ContentScenePattern.Match(sceneName);
            if (!match.Success)
                return false;

            tileX = int.Parse(match.Groups[1].Value);
            tileZ = int.Parse(match.Groups[2].Value);
            return true;
        }

        public static bool IsContentScene(string sceneName)
        {
            return TryParseContentScene(sceneName, out _, out _);
        }
    }
}

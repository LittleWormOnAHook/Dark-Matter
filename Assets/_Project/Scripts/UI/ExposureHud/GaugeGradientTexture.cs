using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Builds small runtime gradient textures/sprites for exposure HUD gauges
    /// (thermometer tube fill, hazard severity bar). Callers should cache the
    /// returned sprite statically — this always allocates a new texture.
    /// </summary>
    internal static class GaugeGradientTexture
    {
        /// <summary>Bottom-to-top gradient (stops[0] = bottom of the bar).</summary>
        public static Sprite BuildVertical(Color[] stops, int resolution = 128)
        {
            Texture2D texture = new Texture2D(1, resolution, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (int y = 0; y < resolution; y++)
            {
                float t = resolution <= 1 ? 0f : y / (float)(resolution - 1);
                texture.SetPixel(0, y, SampleStops(stops, t));
            }

            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, resolution), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "GaugeVerticalGradient";
            return sprite;
        }

        /// <summary>Left-to-right gradient (stops[0] = left edge of the bar).</summary>
        public static Sprite BuildHorizontal(Color[] stops, int resolution = 128)
        {
            Texture2D texture = new Texture2D(resolution, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (int x = 0; x < resolution; x++)
            {
                float t = resolution <= 1 ? 0f : x / (float)(resolution - 1);
                texture.SetPixel(x, 0, SampleStops(stops, t));
            }

            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, resolution, 1f), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "GaugeHorizontalGradient";
            return sprite;
        }

        private static Color SampleStops(Color[] stops, float t)
        {
            if (stops == null || stops.Length == 0)
                return Color.white;
            if (stops.Length == 1)
                return stops[0];

            float scaled = Mathf.Clamp01(t) * (stops.Length - 1);
            int index = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, stops.Length - 2);
            float localT = scaled - index;
            return Color.Lerp(stops[index], stops[index + 1], localT);
        }
    }
}

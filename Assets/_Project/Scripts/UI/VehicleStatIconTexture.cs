using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Procedurally draws small white silhouette icons (shield / heart / fuel drop) for the vehicle
    /// status HUD, since the project has no imported vehicle icon art yet. Icons are plain white with
    /// per-pixel alpha coverage so callers can tint them via Image.color to match any accent. Sprites
    /// are built once and cached — same convention as GaugeGradientTexture.
    /// </summary>
    internal static class VehicleStatIconTexture
    {
        private const int Resolution = 48;
        private const float PixelAntiAlias = 1.6f;
        private static readonly float AaBandWidth = PixelAntiAlias * 2f / Resolution;

        private static Sprite shieldSprite;
        private static Sprite heartSprite;
        private static Sprite fuelDropSprite;

        public static Sprite GetShield()
        {
            if (shieldSprite == null)
                shieldSprite = BuildSprite("VehicleIcon_Shield", ShieldCoverage);
            return shieldSprite;
        }

        public static Sprite GetHeart()
        {
            if (heartSprite == null)
                heartSprite = BuildSprite("VehicleIcon_Heart", HeartCoverage);
            return heartSprite;
        }

        public static Sprite GetFuelDrop()
        {
            if (fuelDropSprite == null)
                fuelDropSprite = BuildSprite("VehicleIcon_FuelDrop", FuelDropCoverage);
            return fuelDropSprite;
        }

        private static Sprite BuildSprite(string name, System.Func<float, float, float> coverageFunc)
        {
            Texture2D texture = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (int y = 0; y < Resolution; y++)
            {
                float ny = (y / (float)(Resolution - 1)) * 2f - 1f;
                for (int x = 0; x < Resolution; x++)
                {
                    float nx = (x / (float)(Resolution - 1)) * 2f - 1f;
                    float coverage = Mathf.Clamp01(coverageFunc(nx, ny));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, coverage));
                }
            }

            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, Resolution, Resolution), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        /// <summary>Signed-distance-ish value to soft alpha coverage across a small anti-alias band.</summary>
        private static float Smooth(float signedDistance)
        {
            return 0.5f + signedDistance / AaBandWidth;
        }

        /// <summary>Wide rounded top tapering to a point at the bottom (badge/shield silhouette).</summary>
        private static float ShieldCoverage(float nx, float ny)
        {
            float v = Mathf.Clamp01((ny + 1f) * 0.5f);
            const float topWidth = 0.8f;
            float width = topWidth * Mathf.Sin(v * Mathf.PI * 0.5f);
            return Smooth(width - Mathf.Abs(nx));
        }

        /// <summary>Classic implicit heart curve, cusp at the bottom.</summary>
        private static float HeartCoverage(float nx, float ny)
        {
            float x = nx * 1.35f;
            float y = ny * 1.25f;
            float h = Mathf.Pow(x * x + y * y - 1f, 3f) - x * x * y * y * y;
            // h <= 0 is inside; soften the boundary into a small anti-aliased band.
            return Smooth(-h * 0.35f);
        }

        /// <summary>Fat circular base tapering to a point at the top (liquid/fuel drop).</summary>
        private static float FuelDropCoverage(float nx, float ny)
        {
            const float circleCenterY = -0.25f;
            const float circleRadius = 0.62f;

            if (ny <= circleCenterY)
            {
                float dist = circleRadius - Mathf.Sqrt(nx * nx + (ny - circleCenterY) * (ny - circleCenterY));
                return Smooth(dist);
            }

            float topSpan = 1f - circleCenterY;
            float t = Mathf.Clamp01((ny - circleCenterY) / topSpan);
            float width = circleRadius * (1f - t);
            return Smooth(width - Mathf.Abs(nx));
        }
    }
}

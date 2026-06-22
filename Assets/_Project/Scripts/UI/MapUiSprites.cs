using Project.Data;
using UnityEngine;

namespace Project.UI
{
    internal static class MapUiSprites
    {
        private static Sprite arrowSprite;
        private static Sprite circleMaskSprite;
        private static Sprite circleRingSprite;
        private static Sprite dotSprite;

        public static Sprite PlayerArrow
        {
            get
            {
                if (arrowSprite == null)
                    arrowSprite = CreateArrowSprite();
                return arrowSprite;
            }
        }

        public static Sprite CircleRing
        {
            get
            {
                if (circleRingSprite == null)
                    circleRingSprite = CreateCircleSprite(128, filled: false, ringThickness: 6f);
                return circleRingSprite;
            }
        }

        public static Sprite CircleMask
        {
            get
            {
                if (circleMaskSprite == null)
                    circleMaskSprite = CreateCircleSprite(128, filled: true);
                return circleMaskSprite;
            }
        }

        public static Sprite Dot
        {
            get
            {
                if (dotSprite == null)
                    dotSprite = CreateCircleSprite(16, filled: true);
                return dotSprite;
            }
        }

        public static Color GetResourceColor(ItemType itemType)
        {
            return itemType switch
            {
                ItemType.Resource => new Color(0.35f, 0.85f, 0.45f, 1f),
                ItemType.Consumable => new Color(0.95f, 0.35f, 0.45f, 1f),
                ItemType.Tool => new Color(0.45f, 0.75f, 0.95f, 1f),
                ItemType.MeleeWeapon => new Color(0.95f, 0.75f, 0.25f, 1f),
                ItemType.Quest => new Color(0.85f, 0.55f, 1f, 1f),
                _ => new Color(1f, 0.85f, 0.2f, 1f)
            };
        }

        internal static void ResetCache()
        {
            DestroySprite(ref arrowSprite);
            DestroySprite(ref circleMaskSprite);
            DestroySprite(ref circleRingSprite);
            DestroySprite(ref dotSprite);
        }

        private static void DestroySprite(ref Sprite sprite)
        {
            if (sprite == null)
                return;

            if (sprite.texture != null)
                Object.Destroy(sprite.texture);

            Object.Destroy(sprite);
            sprite = null;
        }

        private static Sprite CreateArrowSprite()
        {
            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "MapPlayerArrow",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color fill = Color.white;
            Vector2 tip = new Vector2(size * 0.5f, size - 3f);
            Vector2 left = new Vector2(5f, 4f);
            Vector2 right = new Vector2(size - 5f, 4f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    texture.SetPixel(x, y, PointInTriangle(point, tip, left, right) ? fill : clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateCircleSprite(int size, bool filled, float ringThickness = 3f)
        {
            size = Mathf.Clamp(size, 8, 128);
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = filled ? "MapCircleMask" : "MapCircleRing",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float radius = size * 0.5f - 2f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float inner = filled ? 0f : radius - ringThickness;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    bool inside = filled
                        ? dist <= radius
                        : dist <= radius && dist >= inner;
                    float edge = filled
                        ? Mathf.Clamp01(radius - dist + 1.5f)
                        : Mathf.Clamp01(Mathf.Min(dist - inner, radius - dist) + 1.5f);
                    texture.SetPixel(x, y, inside ? new Color(1f, 1f, 1f, edge) : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Sign(p, a, b);
            float d2 = Sign(p, b, c);
            float d3 = Sign(p, c, a);
            bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNegative && hasPositive);
        }

        private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
        }
    }
}

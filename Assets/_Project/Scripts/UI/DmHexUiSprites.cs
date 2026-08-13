using UnityEngine;

namespace Project.UI
{
    /// <summary>Procedural sprites for Journal hex skill nodes.</summary>
    internal static class DmHexUiSprites
    {
        private static Sprite filledHex;
        private static Sprite outlineHex;
        private static Sprite softGlow;
        private static Sprite rankDot;

        public static Sprite FilledHex
        {
            get
            {
                if (filledHex == null)
                    filledHex = CreatePointyHexSprite(128, filled: true, outlineThickness: 0f);
                return filledHex;
            }
        }

        public static Sprite OutlineHex
        {
            get
            {
                if (outlineHex == null)
                    outlineHex = CreatePointyHexSprite(128, filled: false, outlineThickness: 5f);
                return outlineHex;
            }
        }

        public static Sprite SoftGlow
        {
            get
            {
                if (softGlow == null)
                    softGlow = CreateSoftGlowSprite(96);
                return softGlow;
            }
        }

        public static Sprite RankDot
        {
            get
            {
                if (rankDot == null)
                    rankDot = CreateCircleSprite(24);
                return rankDot;
            }
        }

        private static Sprite CreatePointyHexSprite(int size, bool filled, float outlineThickness)
        {
            size = Mathf.Clamp(size, 32, 256);
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = filled ? "DmHexFilled" : "DmHexOutline",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.48f;
            Vector2[] verts = BuildPointyHex(center, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float dist = DistanceToHexEdge(p, verts);
                    bool inside = PointInConvexPolygon(p, verts);
                    Color pixel = Color.clear;

                    if (filled)
                    {
                        if (inside)
                        {
                            float edgeFade = Mathf.Clamp01(dist + 1.2f);
                            pixel = new Color(1f, 1f, 1f, edgeFade);
                        }
                    }
                    else if (inside)
                    {
                        float ring = outlineThickness - dist;
                        if (ring > -1.5f)
                        {
                            float alpha = Mathf.Clamp01(ring + 1.5f);
                            pixel = new Color(1f, 1f, 1f, alpha);
                        }
                    }

                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateSoftGlowSprite(int size)
        {
            size = Mathf.Clamp(size, 32, 256);
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "DmHexGlow",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) / radius;
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = alpha * alpha * 0.85f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateCircleSprite(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "DmRankDot",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.42f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(radius - dist + 1.2f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Vector2[] BuildPointyHex(Vector2 center, float radius)
        {
            Vector2[] verts = new Vector2[6];
            for (int i = 0; i < 6; i++)
            {
                float angle = Mathf.Deg2Rad * (60f * i - 30f);
                verts[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            return verts;
        }

        private static bool PointInConvexPolygon(Vector2 point, Vector2[] verts)
        {
            bool inside = true;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector2 a = verts[i];
                Vector2 b = verts[(i + 1) % verts.Length];
                float cross = (b.x - a.x) * (point.y - a.y) - (b.y - a.y) * (point.x - a.x);
                if (cross < 0f)
                {
                    inside = false;
                    break;
                }
            }

            return inside;
        }

        private static float DistanceToHexEdge(Vector2 point, Vector2[] verts)
        {
            float min = float.MaxValue;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector2 a = verts[i];
                Vector2 b = verts[(i + 1) % verts.Length];
                min = Mathf.Min(min, DistancePointToSegment(point, a, b));
            }

            return min;
        }

        private static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lenSq = ab.sqrMagnitude;
            if (lenSq < 0.0001f)
                return Vector2.Distance(p, a);

            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
            Vector2 proj = a + ab * t;
            return Vector2.Distance(p, proj);
        }
    }
}

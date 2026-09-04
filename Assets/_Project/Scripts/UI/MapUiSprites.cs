using Project.Data;
using UnityEngine;

namespace Project.UI
{
    internal static class MapUiSprites
    {
        private const int HudRingTextureSize = 512;
        private const float HudRingThickness = 18f;
        private const float HudRingFeather = 3f;
        private const int HudFillTextureSize = 256;
        private const float HudFillFeather = 2f;

        private const int HudHealthRingTextureSize = 512;
        private const float HudHealthRingThickness = 36f;
        private const float HudHealthRingFeather = 3f;
        private const int HudViewportMaskSize = 512;
        private const float HudViewportMaskFeather = 4f;

        private static Sprite arrowSprite;
        private static Sprite circleMaskSprite;
        private static Sprite circleRingSprite;
        private static Sprite hudCircleRingSprite;
        private static Sprite hudCircleFillSprite;
        private static Sprite hudHealthRingSprite;
        private static Sprite portraitCircleMaskSprite;
        private static Sprite portraitCircleRingSprite;
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
                {
                    circleRingSprite = CreateCircleSprite(
                        256,
                        filled: false,
                        ringThickness: 12f,
                        edgeFeather: 2.5f);
                }

                return circleRingSprite;
            }
        }

        public static Sprite CircleMask
        {
            get
            {
                if (circleMaskSprite == null)
                {
                    circleMaskSprite = CreateCircleSprite(
                        HudViewportMaskSize,
                        filled: true,
                        edgeFeather: HudViewportMaskFeather,
                        pixelsPerUnit: HudViewportMaskSize);
                }

                return circleMaskSprite;
            }
        }

        /// <summary>Feathered annular ring for companion top-half health arcs.</summary>
        public static Sprite HudHealthRing
        {
            get
            {
                if (hudHealthRingSprite == null)
                {
                    hudHealthRingSprite = CreateCircleSprite(
                        HudHealthRingTextureSize,
                        filled: false,
                        ringThickness: HudHealthRingThickness,
                        edgeFeather: HudHealthRingFeather,
                        pixelsPerUnit: HudHealthRingTextureSize);
                }

                return hudHealthRingSprite;
            }
        }

        /// <summary>Feathered ring for minimap border and other small HUD chrome (~130–160 px on screen).</summary>
        public static Sprite HudCircleRing
        {
            get
            {
                if (hudCircleRingSprite == null)
                {
                    hudCircleRingSprite = CreateCircleSprite(
                        HudRingTextureSize,
                        filled: false,
                        ringThickness: HudRingThickness,
                        edgeFeather: HudRingFeather,
                        pixelsPerUnit: HudRingTextureSize);
                }

                return hudCircleRingSprite;
            }
        }

        /// <summary>Feathered filled disc for minimap edge buttons and small HUD dots.</summary>
        public static Sprite HudCircleFill
        {
            get
            {
                if (hudCircleFillSprite == null)
                {
                    hudCircleFillSprite = CreateCircleSprite(
                        HudFillTextureSize,
                        filled: true,
                        edgeFeather: HudFillFeather,
                        pixelsPerUnit: HudFillTextureSize);
                }

                return hudCircleFillSprite;
            }
        }

        private const int PortraitTextureSize = 512;
        private const float PortraitRingThickness = 26f;

        /// <summary>Legacy soft ring — portrait UI now uses baked PNG rings only.</summary>
        public static Sprite PortraitCircleRing
        {
            get
            {
                if (portraitCircleRingSprite == null)
                {
                    portraitCircleRingSprite = CreateCircleSprite(
                        PortraitTextureSize,
                        filled: false,
                        ringThickness: PortraitRingThickness,
                        edgeFeather: 5.5f);
                }

                return portraitCircleRingSprite;
            }
        }

        /// <summary>Soft circular clip for companion portraits (PNG corners are opaque square plates).</summary>
        public static Sprite PortraitCircleMask
        {
            get
            {
                if (portraitCircleMaskSprite == null)
                {
                    portraitCircleMaskSprite = CreateCircleSprite(
                        PortraitTextureSize,
                        filled: true,
                        edgeFeather: 3f);
                }

                return portraitCircleMaskSprite;
            }
        }

        /// <summary>Mask inset aligned to the inner edge of <see cref="PortraitCircleRing"/>.</summary>
        public static float GetPortraitRingBandInset(float diameter)
        {
            float radius = PortraitTextureSize * 0.5f - 2f;
            float innerDiameterRatio = 2f * (radius - PortraitRingThickness) / PortraitTextureSize;
            float inset = diameter * (1f - innerDiameterRatio) * 0.5f;
            return Mathf.Clamp(inset, 2f, 12f);
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


        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ResetCache();
        }

        internal static void ResetCache()
        {
            DestroySprite(ref arrowSprite);
            DestroySprite(ref circleMaskSprite);
            DestroySprite(ref circleRingSprite);
            DestroySprite(ref hudCircleRingSprite);
            DestroySprite(ref hudCircleFillSprite);
            DestroySprite(ref hudHealthRingSprite);
            DestroySprite(ref portraitCircleMaskSprite);
            DestroySprite(ref portraitCircleRingSprite);
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
            // Chevron / arrowhead: sharp tip, inverted-V notch at the bottom (not a flat triangle).
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "MapPlayerArrow",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color clear = new Color(0f, 0f, 0f, 0f);
            Color fill = Color.white;
            float cx = size * 0.5f;
            Vector2 tip = new Vector2(cx, size - 2f);
            Vector2 leftWing = new Vector2(4f, 6f);
            Vector2 notch = new Vector2(cx, size * 0.42f);
            Vector2 rightWing = new Vector2(size - 4f, 6f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                    bool inside = PointInTriangle(point, tip, leftWing, notch)
                        || PointInTriangle(point, tip, notch, rightWing);
                    texture.SetPixel(x, y, inside ? fill : clear);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateCircleSprite(
            int size,
            bool filled,
            float ringThickness = 3f,
            float edgeFeather = 1.5f,
            float pixelsPerUnit = 100f)
        {
            size = Mathf.Clamp(size, 8, 512);
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = filled ? "MapCircleMask" : "MapCircleRing",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float radius = size * 0.5f - 2f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float inner = filled ? 0f : radius - ringThickness;
            float feather = Mathf.Max(0.75f, edgeFeather);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float edgeDistance = filled
                        ? radius - dist
                        : Mathf.Min(dist - inner, radius - dist);
                    float alpha = SmoothEdgeAlpha(edgeDistance, feather);
                    texture.SetPixel(x, y, alpha > 0f ? new Color(1f, 1f, 1f, alpha) : Color.clear);
                }
            }

            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit);
        }

        private static float SmoothEdgeAlpha(float edgeDistance, float feather)
        {
            if (edgeDistance <= 0f)
                return 0f;

            if (edgeDistance >= feather)
                return 1f;

            float t = edgeDistance / feather;
            return t * t * (3f - 2f * t);
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

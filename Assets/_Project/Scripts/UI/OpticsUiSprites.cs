using UnityEngine;

namespace Project.UI
{
    internal static class OpticsUiSprites
    {
        private const string LibraryResourcePath = "Optics/OpticsCrosshairLibrary";

        private static OpticsCrosshairLibrary library;
        private static Sprite binocularScopeFull;
        private static Sprite binocularScopeInnerGlow;
        private static Sprite binocularScopeOuter;
        private static Sprite scannerHolographic;
        private static Sprite scannerHolographicGlow;
        private static Sprite scannerRectMask;

        public static Sprite BinocularScopeFull => GetOrCreate(ref binocularScopeFull, Library?.binocularScopeFull);
        public static Sprite BinocularScopeInnerGlow => GetOrCreate(ref binocularScopeInnerGlow, Library?.binocularScopeInnerGlow);
        public static Sprite BinocularScopeOuter => GetOrCreate(ref binocularScopeOuter, Library?.binocularScopeOuter);
        public static Sprite ScannerHolographic => GetOrCreate(ref scannerHolographic, Library?.scannerHolographic);
        public static Sprite ScannerHolographicGlow => GetOrCreate(ref scannerHolographicGlow, Library?.scannerHolographicGlow);
        public static Sprite ScannerRectMask => GetOrCreate(ref scannerRectMask, Library?.scannerRectMask);

        private static OpticsCrosshairLibrary Library
        {
            get
            {
                if (library == null)
                    library = Resources.Load<OpticsCrosshairLibrary>(LibraryResourcePath);
                return library;
            }
        }

        internal static void ResetCache()
        {
            library = null;
            DestroySprite(ref binocularScopeFull);
            DestroySprite(ref binocularScopeInnerGlow);
            DestroySprite(ref binocularScopeOuter);
            DestroySprite(ref scannerHolographic);
            DestroySprite(ref scannerHolographicGlow);
            DestroySprite(ref scannerRectMask);
        }

        private static Sprite GetOrCreate(ref Sprite cache, Texture2D texture)
        {
            if (cache != null)
                return cache;

            if (texture == null)
                return null;

            cache = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);

            cache.name = texture.name;
            return cache;
        }

        private static void DestroySprite(ref Sprite sprite)
        {
            if (sprite == null)
                return;

            Object.Destroy(sprite);
            sprite = null;
        }
    }
}

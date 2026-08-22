using UnityEngine;

namespace Project.Pioneers
{
    /// <summary>
    /// Resolves colony ID-badge sprites for HUD trio slots and journal rows.
    /// Echoes → shared spirit; named catalog → unique portrait; unnamed → Jr. gold-ring silhouette.
    /// </summary>
    public static class PioneerPortraitResolver
    {
        public const string ResourcesFolder = "Portraits";
        public const string EchoSpiritResourceName = "portrait_echo_spirit";
        public const string UnnamedSilhouetteResourceName = "portrait_unnamed_silhouette";
        public const string LibraryResourceName = "PioneerPortraitLibrary";

        private static PioneerPortraitLibrary cachedLibrary;
        private static bool libraryLookupDone;
        private static Sprite cachedEchoSpirit;
        private static Sprite cachedSilhouette;
        private static bool sharedLookupDone;

        /// <summary>
        /// Portrait for a roster record. Returns null only if art failed to load
        /// (caller should fall back to initials + class tint).
        /// </summary>
        public static Sprite Resolve(SkilledPioneerRecord record)
        {
            if (record == null)
                return null;

            if (record.Kind == PioneerKind.RescuedEcho)
                return ResolveEchoSpirit();

            Sprite named = ResolveNamedPortrait(record);
            if (named != null)
                return named;

            Sprite jr = ResolveJrSilhouette(record);
            if (jr != null)
                return jr;

            return ResolveUnnamedSilhouette();
        }

        public static Sprite ResolveEchoSpirit()
        {
            EnsureSharedLoaded();
            return cachedEchoSpirit;
        }

        public static Sprite ResolveUnnamedSilhouette()
        {
            EnsureSharedLoaded();
            return cachedSilhouette;
        }

        public static Sprite ResolveJrSilhouette(SkilledPioneerRecord record)
        {
            if (record == null)
                return null;

            string silhouetteId = record.jrSilhouetteId;
            if (string.IsNullOrWhiteSpace(silhouetteId)
                || !JrPioneerSilhouetteCatalog.TryGet(silhouetteId, out _))
            {
                silhouetteId = JrPioneerSilhouetteCatalog.PickStableForClass(record.pioneerClass, record.id);
            }

            if (!JrPioneerSilhouetteCatalog.TryGet(silhouetteId, out JrPioneerSilhouetteCatalog.Entry entry))
                return null;

            return Resources.Load<Sprite>($"{ResourcesFolder}/{entry.resourceName}");
        }

        public static NamedPioneerDefinition FindDefinitionForRecord(SkilledPioneerRecord record)
        {
            if (record == null)
                return null;

            if (!string.IsNullOrWhiteSpace(record.catalogPioneerId))
            {
                NamedPioneerDefinition byCatalog = NamedPioneerCatalog.FindById(record.catalogPioneerId);
                if (byCatalog != null)
                    return byCatalog;
            }

            NamedPioneerDefinition byId = NamedPioneerCatalog.FindById(record.id);
            if (byId != null)
                return byId;

            return NamedPioneerCatalog.FindByDisplayName(record.displayName);
        }

        public static void ReloadCache()
        {
            cachedLibrary = null;
            libraryLookupDone = false;
            cachedEchoSpirit = null;
            cachedSilhouette = null;
            sharedLookupDone = false;
        }

        private static Sprite ResolveNamedPortrait(SkilledPioneerRecord record)
        {
            // Jr. silhouette recruits never use named art even if displayName collides.
            if (!string.IsNullOrWhiteSpace(record.jrSilhouetteId))
                return null;

            NamedPioneerDefinition definition = FindDefinitionForRecord(record);
            if (definition != null && definition.portrait != null)
                return definition.portrait;

            string resourceKey = definition != null
                ? definition.ResolvedId
                : (!string.IsNullOrWhiteSpace(record.catalogPioneerId) ? record.catalogPioneerId : null);

            if (string.IsNullOrWhiteSpace(resourceKey))
                return null;

            // Don't treat Jr badges or shared fallbacks as "named".
            if (resourceKey.StartsWith("Jr_", System.StringComparison.Ordinal)
                || resourceKey.StartsWith("jr_", System.StringComparison.Ordinal)
                || resourceKey == UnnamedSilhouetteResourceName
                || resourceKey == EchoSpiritResourceName)
            {
                return null;
            }

            return Resources.Load<Sprite>($"{ResourcesFolder}/{resourceKey}");
        }

        private static void EnsureSharedLoaded()
        {
            if (sharedLookupDone)
                return;

            sharedLookupDone = true;

            cachedEchoSpirit = Resources.Load<Sprite>($"{ResourcesFolder}/{EchoSpiritResourceName}");
            cachedSilhouette = Resources.Load<Sprite>($"{ResourcesFolder}/{UnnamedSilhouetteResourceName}");

            PioneerPortraitLibrary library = GetLibrary();
            if (library != null)
            {
                if (cachedEchoSpirit == null)
                    cachedEchoSpirit = library.echoSpirit;
                if (cachedSilhouette == null)
                    cachedSilhouette = library.unnamedSilhouette;
            }
        }

        private static PioneerPortraitLibrary GetLibrary()
        {
            if (libraryLookupDone)
                return cachedLibrary;

            libraryLookupDone = true;
            cachedLibrary = Resources.Load<PioneerPortraitLibrary>(LibraryResourceName);
            return cachedLibrary;
        }
    }
}

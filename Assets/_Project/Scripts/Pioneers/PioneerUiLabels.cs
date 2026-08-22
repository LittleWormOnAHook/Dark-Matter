using UnityEngine;

namespace Project.Pioneers
{
    /// <summary>
    /// UI-only display labels for pioneers. Jr. silhouette titles never overwrite save displayName.
    /// </summary>
    public static class PioneerUiLabels
    {
        /// <summary>
        /// Name shown in HUD / journal / tooltips. Named catalog portraits keep their displayName;
        /// silhouette recruits show their Jr. class title (e.g. Jr. Scientist).
        /// </summary>
        public static string GetDisplayName(SkilledPioneerRecord record)
        {
            if (record == null)
                return string.Empty;

            if (UsesJrSilhouetteTitle(record))
            {
                string title = ResolveJrTitle(record);
                if (!string.IsNullOrEmpty(title))
                    return title;
            }

            return string.IsNullOrWhiteSpace(record.displayName) ? "Pioneer" : record.displayName;
        }

        public static bool UsesJrSilhouetteTitle(SkilledPioneerRecord record)
        {
            if (record == null || record.Kind == PioneerKind.RescuedEcho)
                return false;

            // Explicit Jr badge assignment (procedural unique recruits).
            if (!string.IsNullOrWhiteSpace(record.jrSilhouetteId))
                return true;

            // Named catalog / starters keep their proper names.
            if (PioneerPortraitResolver.FindDefinitionForRecord(record) != null)
                return false;

            // Unnamed / workers without catalog art use Jr. titles in UI.
            return true;
        }

        private static string ResolveJrTitle(SkilledPioneerRecord record)
        {
            string id = record.jrSilhouetteId;
            if (string.IsNullOrWhiteSpace(id) || !JrPioneerSilhouetteCatalog.TryGet(id, out _))
                id = JrPioneerSilhouetteCatalog.PickStableForClass(record.pioneerClass, record.id);

            return JrPioneerSilhouetteCatalog.GetUiTitle(id);
        }
    }
}

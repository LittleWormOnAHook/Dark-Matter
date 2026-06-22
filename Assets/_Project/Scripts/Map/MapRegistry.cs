using System.Collections.Generic;

namespace Project.Map
{
    public static class MapRegistry
    {
        private static readonly List<MapMarker> Markers = new List<MapMarker>();

        public static IReadOnlyList<MapMarker> ActiveMarkers => Markers;

        internal static void Register(MapMarker marker)
        {
            if (marker == null || Markers.Contains(marker))
                return;

            Markers.Add(marker);
        }

        internal static void Unregister(MapMarker marker)
        {
            if (marker == null)
                return;

            Markers.Remove(marker);
        }

        internal static void Clear()
        {
            Markers.Clear();
        }
    }
}

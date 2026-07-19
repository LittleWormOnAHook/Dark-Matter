using System;
using System.Collections.Generic;

namespace Project.Map
{
    public static class MapRegistry
    {
        private static readonly List<MapMarker> Markers = new List<MapMarker>();

        public static IReadOnlyList<MapMarker> ActiveMarkers => Markers;

        public static event Action<MapMarker> MarkerRegistered;
        public static event Action<MapMarker> MarkerUnregistered;

        internal static void Register(MapMarker marker)
        {
            if (marker == null || Markers.Contains(marker))
                return;

            Markers.Add(marker);
            MarkerRegistered?.Invoke(marker);
        }

        internal static void Unregister(MapMarker marker)
        {
            if (marker == null)
                return;

            if (!Markers.Remove(marker))
                return;

            MarkerUnregistered?.Invoke(marker);
        }

        internal static void Clear()
        {
            Markers.Clear();
        }
    }
}

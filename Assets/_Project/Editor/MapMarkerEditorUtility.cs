using Project.Data;
using Project.Map;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class MapMarkerEditorUtility
    {
        public static MapMarker EnsureMapMarker(GameObject root, ItemData item = null)
        {
            if (root == null)
                return null;

            MapMarker marker = root.GetComponent<MapMarker>();
            if (marker == null)
                marker = root.AddComponent<MapMarker>();

            if (item != null)
                marker.ConfigureForResource(item);

            return marker;
        }

        public static void RemoveMapMarkers(GameObject root)
        {
            if (root == null)
                return;

            MapMarker[] markers = root.GetComponentsInChildren<MapMarker>(true);
            for (int i = 0; i < markers.Length; i++)
            {
                if (markers[i] != null)
                    Object.DestroyImmediate(markers[i]);
            }
        }
    }
}

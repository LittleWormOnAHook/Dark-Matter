using Project.Data;
using Project.Map;
using UnityEditor;
using UnityEditor.SceneManagement;
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
            else
                marker.RefreshFromHost();

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

        [MenuItem(DarkMatterGenesisEditorMenus.RefreshAllMapMarkers, false, 41)]
        public static void RefreshAllMapMarkersMenu()
        {
            int count = RefreshAllMapMarkers();
            EditorUtility.DisplayDialog(
                "Map Markers",
                "Refreshed " + count + " MapMarker(s): fog reveal on, live world position on, combat aggro on enemies.",
                "OK");
        }

        public static int RefreshAllMapMarkers()
        {
            int count = 0;

            MapMarker[] sceneMarkers = Object.FindObjectsByType<MapMarker>(FindObjectsInactive.Include);
            for (int i = 0; i < sceneMarkers.Length; i++)
            {
                MapMarker marker = sceneMarkers[i];
                if (marker == null)
                    continue;

                Undo.RecordObject(marker, "Refresh Map Marker");
                marker.RefreshFromHost();
                EditorUtility.SetDirty(marker);
                EditorSceneManager.MarkSceneDirty(marker.gameObject.scene);
                count++;
            }

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                MapMarker[] prefabMarkers = prefab.GetComponentsInChildren<MapMarker>(true);
                if (prefabMarkers == null || prefabMarkers.Length == 0)
                    continue;

                bool dirty = false;
                for (int m = 0; m < prefabMarkers.Length; m++)
                {
                    MapMarker marker = prefabMarkers[m];
                    if (marker == null)
                        continue;

                    marker.RefreshFromHost();
                    EditorUtility.SetDirty(marker);
                    dirty = true;
                    count++;
                }

                if (dirty)
                    EditorUtility.SetDirty(prefab);
            }

            AssetDatabase.SaveAssets();
            return count;
        }
    }
}

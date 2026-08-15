using Project.Map;
using Project.Quests;
using UnityEngine;

namespace Project.PPT
{
    public sealed class PptMapDiscoveryListener : MonoBehaviour
    {
        private void OnEnable()
        {
            ScannerDiscoveryRegistry.Changed += HandleDiscoveryChanged;
        }

        private void OnDisable()
        {
            ScannerDiscoveryRegistry.Changed -= HandleDiscoveryChanged;
        }

        private void HandleDiscoveryChanged()
        {
            PptManager manager = PptManager.Instance;
            if (manager == null)
                return;

            MapMarker[] markers = FindObjectsByType<MapMarker>(FindObjectsInactive.Include);
            for (int i = 0; i < markers.Length; i++)
            {
                MapMarker marker = markers[i];
                if (marker == null || !ScannerDiscoveryRegistry.IsDiscovered(marker.DiscoveryId))
                    continue;

                string pptId = "place_" + marker.DiscoveryId.Replace(' ', '_').ToLowerInvariant();
                PptKeywordLog.Log(pptId, "Scanner: " + marker.Label);
            }

            manager.RefreshCatalog();
        }
    }
}

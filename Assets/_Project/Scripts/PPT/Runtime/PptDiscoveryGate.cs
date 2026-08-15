using Project.Map;

namespace Project.PPT
{
    public static class PptDiscoveryGate
    {
        public static bool IsAvailableToPlayer(PptEntry entry)
        {
            if (entry == null)
                return false;

            if (!PptKeywordLog.IsKnown(entry.PptId))
                return false;

            if (!entry.RequiresDiscovery)
                return true;

            if (!string.IsNullOrWhiteSpace(entry.MapMarkerDiscoveryId))
                return ScannerDiscoveryRegistry.IsDiscovered(entry.MapMarkerDiscoveryId);

            return true;
        }
    }
}

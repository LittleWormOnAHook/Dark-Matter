using Project.UI;
using UnityEngine;

namespace Project.Map
{
    internal static class MapRuntimeCleanup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            MapRegistry.Clear();
            MapUiSprites.ResetCache();
            OpticsUiSprites.ResetCache();
            WorldMapProvider.ResetStaticState();
        }
    }
}

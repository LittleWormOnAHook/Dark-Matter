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
            OpticsOverlayUI.ResetRuntimeState();
            PetTamingProgressUI.ResetRuntimeState();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CleanupStaleRuntimeCanvases()
        {
            if (!Application.isPlaying)
                return;

            OpticsOverlayUI.CleanupStaleRuntimeObjects();
        }
    }
}

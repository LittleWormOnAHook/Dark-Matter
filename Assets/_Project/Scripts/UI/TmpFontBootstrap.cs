using UnityEngine;

namespace Project.UI
{
    internal static class TmpFontBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ApplyFallbackFontAfterSceneLoad()
        {
            if (!Application.isPlaying)
                return;

            TmpUiHelper.ApplyToAllLoadedObjects();
        }
    }
}

using UnityEngine;

namespace Project.PPT
{
    public static class PptBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureSystems()
        {
            if (PptManager.Instance != null)
                return;

            GameObject host = new GameObject("PptSystems");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<PptManager>();
            host.AddComponent<PptMapDiscoveryListener>();
            host.AddComponent<PptQuestKeywordListener>();
        }
    }
}

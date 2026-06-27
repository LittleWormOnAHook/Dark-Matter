using Project.Core;
using Project.Interaction;
using UnityEngine;

namespace Project.Audio
{
    internal static class AudioGameplayBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePlayerAudio()
        {
            if (!Application.isPlaying)
                return;

            GameAudioManager.EnsureExists();

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
                return;

            if (player.GetComponent<FootstepController>() == null)
                player.AddComponent<FootstepController>();

            if (player.GetComponent<LandingAudioController>() == null)
                player.AddComponent<LandingAudioController>();

            if (player.GetComponent<OpticsController>() == null)
                player.AddComponent<OpticsController>();
        }
    }
}

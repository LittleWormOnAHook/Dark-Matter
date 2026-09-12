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

            // Invector vFootStep already plants audio + VFX. Adding FootstepController
            // after Pioneer disables it double-fires clips and GetAlphamaps every frame.
            if (player.GetComponent<Invector.vFootStep>() != null)
            {
                FootstepController extra = player.GetComponent<FootstepController>();
                if (extra != null)
                    extra.enabled = false;
            }
            else if (player.GetComponent<FootstepController>() == null)
            {
                player.AddComponent<FootstepController>();
            }

            if (player.GetComponent<LandingAudioController>() == null)
                player.AddComponent<LandingAudioController>();

            if (player.GetComponent<OpticsController>() == null)
                player.AddComponent<OpticsController>();
        }
    }
}

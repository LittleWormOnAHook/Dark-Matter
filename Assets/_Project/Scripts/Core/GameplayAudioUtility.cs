using UnityEngine;

namespace Project.Core
{
    public static class GameplayAudioUtility
    {
        public static void EnsureListenerOnCamera(Camera camera = null)
        {
            if (camera == null)
                camera = Camera.main;

            if (camera == null)
                return;

            AudioListener target = camera.GetComponent<AudioListener>();
            if (target == null)
                target = camera.gameObject.AddComponent<AudioListener>();

            AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);

            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener listener = listeners[i];
                if (listener == null || listener == target)
                    continue;

                listener.enabled = false;
            }

            if (!target.enabled)
                target.enabled = true;
        }

        /// <summary>
        /// Configures an AudioSource for true world-space 3D audio.
        /// Listener stays on the gameplay camera; the source must remain at its emitter/zone position
        /// (never parented under the camera) so pan/attenuation follow listener↔source geometry only.
        /// </summary>
        public static void ConfigureWorldSpatialSource(
            AudioSource source,
            float minDistance = 4f,
            float maxDistance = 40f,
            AudioRolloffMode rolloff = AudioRolloffMode.Logarithmic)
        {
            if (source == null)
                return;

            if (!source.enabled)
                source.enabled = true;

            source.spatialBlend = 1f;
            source.spatialize = false;
            source.spatializePostEffects = false;
            source.panStereo = 0f;
            source.dopplerLevel = 0f;
            source.spread = 0f;
            source.rolloffMode = rolloff;
            source.minDistance = Mathf.Max(0.1f, minDistance);
            source.maxDistance = Mathf.Max(source.minDistance + 0.1f, maxDistance);
            source.bypassListenerEffects = false;
            source.bypassReverbZones = false;
        }

        /// <summary>
        /// Returns false if the source cannot play (missing, disabled component, or inactive hierarchy).
        /// Does not reparent; callers keep sources at emitter/zone world positions.
        /// </summary>
        public static bool CanPlaySpatialSource(AudioSource source)
        {
            return source != null &&
                   source.enabled &&
                   source.gameObject.activeInHierarchy;
        }
    }
}

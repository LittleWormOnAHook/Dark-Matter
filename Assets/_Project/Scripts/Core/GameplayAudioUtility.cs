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
    }
}

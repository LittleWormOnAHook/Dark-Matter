using UnityEngine;

namespace Project.Audio
{
    /// <summary>
    /// Bookkeeping for throwaway one-shot audio objects that vendor code Instantiates at scene root
    /// (Invector "AudioSource" prefab = AudioSource + vDestroyGameObject with a timed Destroy).
    /// That timer never runs in edit mode or during Play-exit teardown, so such spawns used to be
    /// left behind as "AudioSource(Clone)" roots in the edit scene.
    /// </summary>
    public static class DMSpawnedAudio
    {
        public const string InvectorAudioCloneName = "AudioSource(Clone)";

        /// <summary>
        /// False outside Play: callers skip the spawn, because edit-mode animator evaluation
        /// (bind-pose repair, previews) must not create scene objects nobody will destroy.
        /// </summary>
        public static bool CanSpawn => Application.isPlaying;

        /// <summary>Never serialize a runtime one-shot into the scene, even if it outlives Play.</summary>
        public static void MarkRuntimeSpawned(GameObject spawned)
        {
            if (spawned == null)
                return;

            spawned.hideFlags |= HideFlags.DontSaveInEditor;
        }

        /// <summary>
        /// Strict match for an orphaned one-shot clone: scene root, no children, named "AudioSource(Clone)",
        /// and only Transform + AudioSource (+ Invector vDestroyGameObject) components.
        /// </summary>
        public static bool LooksLikeOrphanedOneShot(GameObject go)
        {
            if (go == null || go.transform.parent != null || go.transform.childCount != 0)
                return false;

            if (!string.Equals(go.name, InvectorAudioCloneName, System.StringComparison.Ordinal))
                return false;

            Component[] components = go.GetComponents<Component>();
            bool hasAudioSource = false;
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                    return false;
                if (component is Transform)
                    continue;
                if (component is AudioSource)
                {
                    hasAudioSource = true;
                    continue;
                }

                if (component is Invector.vDestroyGameObject)
                    continue;

                return false;
            }

            return hasAudioSource;
        }
    }
}

using UnityEngine;

namespace Project.World.Clock
{
    [DefaultExecutionOrder(-800)]
    [DisallowMultipleComponent]
    public sealed class DMIoClockRunner : MonoBehaviour
    {
        private static DMIoClockRunner instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            GameObject host = new GameObject("DMIoClockRunner");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<DMIoClockRunner>();
        }

        private void OnEnable()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private void OnDisable()
        {
            if (instance == this)
                instance = null;
        }

        private void Update()
        {
            DMIoClock.Tick(UnityEngine.Time.deltaTime);
        }
    }
}

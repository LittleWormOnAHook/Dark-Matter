using Project.UI;
using UnityEngine;

namespace Project.Core
{
    [DisallowMultipleComponent]
    public class DMGameAutosave : MonoBehaviour
    {
        private static DMGameAutosave instance;
        private float elapsed;

        public static void EnsureExists()
        {
            if (!Application.isPlaying)
                return;

            if (instance != null)
                return;

            DMGameAutosave found = FindAnyObjectByType<DMGameAutosave>();
            if (found != null)
            {
                instance = found;
                return;
            }

            GameObject host = new GameObject("DMGameAutosave");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<DMGameAutosave>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            instance = this;
        }

        private void OnEnable()
        {
            GameSession.GameStarted += HandleGameStarted;
        }

        private void OnDisable()
        {
            GameSession.GameStarted -= HandleGameStarted;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void HandleGameStarted()
        {
            elapsed = 0f;
        }

        private void Update()
        {
            if (!GameSession.HasStarted || LoadingOverlayController.IsBlockingMenu)
                return;

            elapsed += Time.unscaledDeltaTime;
            if (elapsed < GameSaveSystem.AutosaveIntervalSeconds)
                return;

            elapsed = 0f;
            if (GameSaveSystem.TryAutosave(out string message))
                Debug.Log("DMGameAutosave: " + message);
        }
    }
}

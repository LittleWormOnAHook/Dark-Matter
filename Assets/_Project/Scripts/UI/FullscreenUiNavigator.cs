using System;
using System.Collections.Generic;
using Project.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.UI
{
    public class FullscreenUiNavigator : MonoBehaviour
    {
        public static FullscreenUiNavigator Instance { get; private set; }

        private readonly List<JournalWindowId> windowStack = new List<JournalWindowId>();
        private readonly Dictionary<JournalWindowId, FullscreenUiWindow> windows = new Dictionary<JournalWindowId, FullscreenUiWindow>();

        public event Action<bool> OnPauseGameplayChanged;
        public event Action<JournalWindowId?> OnActiveWindowChanged;

        public bool IsAnyOpen => windowStack.Count > 0;

        public JournalWindowId? CurrentWindow =>
            windowStack.Count > 0 ? windowStack[windowStack.Count - 1] : (JournalWindowId?)null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (!IsAnyOpen || Keyboard.current == null)
                return;

            if (!Keyboard.current.escapeKey.wasPressedThisFrame)
                return;

            HandleEscape();
        }

        public static FullscreenUiNavigator EnsureExists(Transform parent)
        {
            if (parent == null)
            {
                Debug.LogError("[FullscreenUiNavigator] EnsureExists called with null parent.");
                return null;
            }

            if (Instance != null)
            {
                if (Instance.transform == parent)
                    return Instance;

                UnityEngine.Object.Destroy(Instance.gameObject);
                Instance = null;
            }

            FullscreenUiNavigator existing = parent.GetComponent<FullscreenUiNavigator>();
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }

            return parent.gameObject.AddComponent<FullscreenUiNavigator>();
        }

        public void RegisterWindow(FullscreenUiWindow window)
        {
            if (window == null)
                return;

            windows[window.WindowId] = window;
        }

        public int GetWindowCount() => windows.Count;

        public void PushWindow(JournalWindowId id)
        {
            SwitchToWindow(id);
        }

        /// <summary>
        /// Switch directly to a journal section (tab rail). Replaces the current stack with a single window.
        /// </summary>
        public void SwitchToWindow(JournalWindowId id)
        {
            if (GetOrWarn(id) == null)
                return;

            if (windowStack.Count == 1 && windowStack[0] == id && GetWindow(id)?.IsVisible == true)
                return;

            HideAllWindows();
            windowStack.Clear();
            windowStack.Add(id);
            ShowWindow(id);
            NotifyPauseChanged(true);
            NotifyActiveWindowChanged();
        }

        public void PopWindow()
        {
            if (windowStack.Count == 0)
                return;

            if (windowStack.Count <= 1)
            {
                CloseAll();
                return;
            }

            JournalWindowId top = windowStack[windowStack.Count - 1];
            GetWindow(top)?.Hide();
            windowStack.RemoveAt(windowStack.Count - 1);

            HideAllWindows();
            ShowWindow(windowStack[windowStack.Count - 1]);
            NotifyActiveWindowChanged();
        }

        public void CloseAll()
        {
            HideAllWindows(forceAll: true);
            windowStack.Clear();
            NotifyPauseChanged(false);
            NotifyActiveWindowChanged();
        }

        public bool HandleEscape()
        {
            if (!IsAnyOpen)
                return false;

            if (windowStack.Count > 1)
                PopWindow();
            else
                CloseAll();

            return true;
        }

        public void ToggleJournal()
        {
            if (IsAnyOpen)
                CloseAll();
            else
                SwitchToWindow(JournalWindowId.JournalQuest);
        }

        private void ShowWindow(JournalWindowId id)
        {
            FullscreenUiWindow window = GetOrWarn(id);
            window?.Show();
            transform.SetAsLastSibling();
        }

        private void HideAllWindows(bool forceAll = false)
        {
            foreach (FullscreenUiWindow window in windows.Values)
            {
                if (window == null)
                    continue;

                if (forceAll || window.IsVisible)
                    window.Hide();
            }
        }

        private FullscreenUiWindow GetWindow(JournalWindowId id)
        {
            windows.TryGetValue(id, out FullscreenUiWindow window);
            return window;
        }

        private FullscreenUiWindow GetOrWarn(JournalWindowId id)
        {
            if (windows.TryGetValue(id, out FullscreenUiWindow window))
                return window;

            Debug.LogWarning($"[FullscreenUiNavigator] Window not registered: {id}");
            return null;
        }

        private void NotifyActiveWindowChanged()
        {
            OnActiveWindowChanged?.Invoke(CurrentWindow);
        }

        private void NotifyPauseChanged(bool paused)
        {
            OnPauseGameplayChanged?.Invoke(paused);

            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;

            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
                player.SetJournalOpen(paused);

            CameraController camera = FindAnyObjectByType<CameraController>();
            if (camera != null)
                camera.SetJournalOpen(paused);
        }
    }
}

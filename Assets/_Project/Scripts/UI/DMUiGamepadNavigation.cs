using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Project.UI
{
    /// <summary>
    /// Wires EventSystem InputSystemUIInputModule to InputSystem_Actions UI map:
    /// left stick / d-pad Navigate, South Submit, East Cancel.
    /// </summary>
    public static class DMUiGamepadNavigation
    {
        private static bool _wired;
        private static float _nextEnsure;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            _wired = false;
            EnsureConfigured();
        }

        public static void EnsureConfigured()
        {
            if (!Application.isPlaying)
                return;

            if (_wired && Time.unscaledTime < _nextEnsure)
                return;
            _nextEnsure = Time.unscaledTime + 1f;

            EventSystem es = EventSystem.current;
            if (es == null)
                es = Object.FindAnyObjectByType<EventSystem>();

            if (es == null)
            {
                GameObject go = new GameObject("EventSystem");
                es = go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
            }

            InputSystemUIInputModule module = es.GetComponent<InputSystemUIInputModule>();
            if (module == null)
                module = es.gameObject.AddComponent<InputSystemUIInputModule>();

            InputActionAsset asset = ResolveActionsAsset();
            if (asset == null)
                return;

            InputActionMap ui = asset.FindActionMap("UI", throwIfNotFound: false);
            if (ui == null)
                return;

            InputAction navigate = ui.FindAction("Navigate", throwIfNotFound: false);
            InputAction submit = ui.FindAction("Submit", throwIfNotFound: false);
            InputAction cancel = ui.FindAction("Cancel", throwIfNotFound: false);
            InputAction point = ui.FindAction("Point", throwIfNotFound: false);
            InputAction click = ui.FindAction("Click", throwIfNotFound: false);
            InputAction scroll = ui.FindAction("ScrollWheel", throwIfNotFound: false);
            InputAction rightClick = ui.FindAction("RightClick", throwIfNotFound: false);
            InputAction middleClick = ui.FindAction("MiddleClick", throwIfNotFound: false);

            module.actionsAsset = asset;
            if (navigate != null)
                module.move = InputActionReference.Create(navigate);
            if (submit != null)
                module.submit = InputActionReference.Create(submit);
            if (cancel != null)
                module.cancel = InputActionReference.Create(cancel);
            if (point != null)
                module.point = InputActionReference.Create(point);
            if (click != null)
                module.leftClick = InputActionReference.Create(click);
            if (scroll != null)
                module.scrollWheel = InputActionReference.Create(scroll);
            if (rightClick != null)
                module.rightClick = InputActionReference.Create(rightClick);
            if (middleClick != null)
                module.middleClick = InputActionReference.Create(middleClick);

            // Keep UI map enabled so Navigate/Submit/Cancel work while Player map is active.
            if (!ui.enabled)
                ui.Enable();

            _wired = true;
        }

        private static InputActionAsset ResolveActionsAsset()
        {
            PlayerInput pi = Object.FindAnyObjectByType<PlayerInput>();
            if (pi != null && pi.actions != null)
                return pi.actions;

            // Fallback: any loaded asset named like our project asset.
            InputActionAsset[] loaded = Resources.FindObjectsOfTypeAll<InputActionAsset>();
            for (int i = 0; i < loaded.Length; i++)
            {
                if (loaded[i] != null && loaded[i].name.IndexOf("InputSystem_Actions", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return loaded[i];
            }

            return null;
        }
    }
}
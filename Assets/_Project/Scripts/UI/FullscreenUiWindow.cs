using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    public abstract class FullscreenUiWindow : MonoBehaviour
    {
        public const float HeaderHeight = 64f;

        protected RectTransform rootRect;
        protected RectTransform contentArea;
        protected Button closeButton;
        protected FullscreenUiNavigator navigator;

        public JournalWindowId WindowId { get; private set; }
        public bool IsVisible => rootRect != null && rootRect.gameObject.activeSelf;

        public void Initialize(FullscreenUiNavigator owner, JournalWindowId windowId, string title, Color backgroundColor)
        {
            navigator = owner;
            WindowId = windowId;
            BuildFullscreenRoot(transform, title, backgroundColor);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            gameObject.SetActive(false);
            OnBuild();
        }

        protected void BuildFullscreenRoot(Transform parent, string title, Color bg)
        {
            GameObject shell = MenuUiBuilder.CreateFullscreenShell(parent, title, out contentArea, out closeButton);
            rootRect = shell.GetComponent<RectTransform>();

            Image shellImage = shell.GetComponent<Image>();
            if (shellImage != null)
                shellImage.color = bg;
        }

        protected virtual void OnBuild() { }

        public virtual void OnShow() { }

        public virtual void OnHide() { }

        public virtual void Refresh() { }

        public void Show()
        {
            if (rootRect == null)
                return;

            gameObject.SetActive(true);
            rootRect.gameObject.SetActive(true);
            OnShow();
            Refresh();
        }

        public void Hide()
        {
            if (rootRect == null)
                return;

            OnHide();
            rootRect.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        public void Close()
        {
            navigator?.PopWindow();
        }
    }
}

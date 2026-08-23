using Project.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Project.Interaction
{
    /// <summary>
    /// Center-screen harvest / mine / scan time slider. Shared HUD (singleton) so nodes no longer
    /// float a world-space billboard above the resource. Sized like PickupToastUI.
    /// </summary>
    public class WorldNodeProgressBar : MonoBehaviour
    {
        private const float PopupHeight = 48f;
        private const float BarHeight = 14f;
        private const float BarSideInset = 10f;
        private const float BarBottomInset = 6f;

        private static WorldNodeProgressBar instance;

        private RectTransform _root;
        private CanvasGroup _canvasGroup;
        private Image _fill;
        private TextMeshProUGUI _label;
        private Transform _canvasRoot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void HookSceneUnload()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene _)
        {
            instance = null;
        }

        public static WorldNodeProgressBar Create(Transform host)
        {
            // Host is ignored: this is a shared screen-space HUD, not a per-node world canvas.
            return EnsureExists();
        }

        public static WorldNodeProgressBar EnsureExists()
        {
            if (instance == null)
                instance = null;
            else if (!instance)
                instance = null;

            if (instance != null)
                return instance;

            Canvas canvas = MainMenuController.ResolveMainCanvas();
            if (canvas == null)
                canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return null;

            Transform canvasRoot = canvas.transform;
            ActivateParentChain(canvasRoot);

            GameObject go = new GameObject("WorldNodeProgressBar", typeof(RectTransform));
            go.transform.SetParent(canvasRoot, false);
            instance = go.AddComponent<WorldNodeProgressBar>();
            instance.Build(canvasRoot);
            instance.SetVisible(false);
            return instance;
        }

        public static void HideShared()
        {
            if (instance != null)
                instance.SetVisible(false);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void Build(Transform canvasRootTransform)
        {
            _canvasRoot = canvasRootTransform;
            _root = transform as RectTransform;
            ApplyCenterAnchor();
            _root.sizeDelta = new Vector2(GameplayHudLayout.ToastWidth, PopupHeight);

            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(_root, false);
            Image bgImage = bg.GetComponent<Image>();
            DarkMatterGenesisUiPalette.ApplyPanelShellBackground(bgImage, 0.88f);
            bgImage.raycastTarget = false;
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(_root, false);
            _fill = fillGo.GetComponent<Image>();
            MenuUiBuilder.ApplyUiSprite(_fill);
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _fill.color = DarkMatterGenesisUiPalette.Gold;
            _fill.raycastTarget = false;
            _fill.fillAmount = 0f;
            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 0f);
            fillRect.pivot = new Vector2(0.5f, 0f);
            fillRect.sizeDelta = new Vector2(-(BarSideInset * 2f), BarHeight);
            fillRect.anchoredPosition = new Vector2(0f, BarBottomInset);

            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(_root, false);
            _label = labelGo.AddComponent<TextMeshProUGUI>();
            ShiftUiTheme theme = ShiftUiTheme.Current;
            if (theme != null)
                theme.ApplyFont(_label, semiBold: true);
            else
                TmpUiHelper.ApplyDefaultFont(_label);
            _label.fontSize = 20f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.color = DarkMatterGenesisUiPalette.InteractionPromptText;
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.raycastTarget = false;
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, BarHeight + BarBottomInset);
            labelRect.offsetMax = new Vector2(-8f, -2f);
        }

        public void SetVisible(bool visible)
        {
            if (visible && !gameObject.activeSelf)
                EnsurePresented();

            if (_canvasGroup != null)
                _canvasGroup.alpha = visible ? 1f : 0f;

            gameObject.SetActive(visible);
        }

        /// <param name="worldPosition">Ignored. Kept so harvest / mine / scan callers stay unchanged.</param>
        /// <param name="fill01">Normalized mining/harvest time progress (0 = start, 1 = complete).</param>
        /// <param name="camera">Ignored. Screen HUD is canvas-anchored, not world-billboarded.</param>
        public void UpdateBar(Vector3 worldPosition, float fill01, string label, Camera camera)
        {
            if (EnsureExists() == null)
                return;

            SetVisible(true);

            float value = Mathf.Clamp01(fill01);
            if (_fill != null)
                _fill.fillAmount = value;

            if (_label != null)
                _label.text = label ?? string.Empty;
        }

        private void EnsurePresented()
        {
            if (_canvasRoot == null)
            {
                Canvas canvas = MainMenuController.ResolveMainCanvas() ?? Object.FindAnyObjectByType<Canvas>();
                _canvasRoot = canvas != null ? canvas.transform : null;
            }

            if (_canvasRoot == null)
                return;

            ActivateParentChain(_canvasRoot);
            UiFrontLayer.ReparentToFront(transform, _canvasRoot, worldPositionStays: false);
            ApplyCenterAnchor();
            if (_root != null)
                _root.localScale = Vector3.one;
        }

        private void ApplyCenterAnchor()
        {
            if (_root == null)
                _root = transform as RectTransform;
            if (_root == null)
                return;

            _root.anchorMin = new Vector2(0.5f, 0.5f);
            _root.anchorMax = new Vector2(0.5f, 0.5f);
            _root.pivot = new Vector2(0.5f, 0.5f);
            _root.anchoredPosition = GameplayHudLayout.PickupToastAnchoredPosition;
            _root.sizeDelta = new Vector2(GameplayHudLayout.ToastWidth, PopupHeight);
        }

        private static void ActivateParentChain(Transform current)
        {
            while (current != null)
            {
                if (!current.gameObject.activeSelf)
                    current.gameObject.SetActive(true);
                current = current.parent;
            }
        }
    }
}

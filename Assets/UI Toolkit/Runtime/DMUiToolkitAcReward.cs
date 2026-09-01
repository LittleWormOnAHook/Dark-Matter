using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// UITK Aether Credits reward toast. Forwards from UIManager.ShowCurrencyPopup / AcRewardPopup.
    /// </summary>
    [DefaultExecutionOrder(-374)]
    [DisallowMultipleComponent]
    public class DMUiToolkitAcReward : MonoBehaviour
    {
        private static DMUiToolkitAcReward instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement card;
        private Label amountLabel;
        private Label sourceLabel;
        private bool bound;
        private Coroutine routine;

        public static bool IsShowing => instance != null && instance.card != null
            && instance.card.style.display == DisplayStyle.Flex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            EnsureHost();
        }

        public static DMUiToolkitAcReward EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.AcRewardName,
                DMUiToolkitOverlayDocument.AcRewardUxml,
                DMUiToolkitOverlayDocument.AcRewardUss,
                DMUiToolkitOverlayDocument.AcRewardSort);
            if (doc == null)
                return null;

            DMUiToolkitAcReward host = doc.GetComponent<DMUiToolkitAcReward>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitAcReward>();

            host.document = doc;
            host.BindTree();
            return host;
        }

        public static bool TryShow(string amountLine, string source)
        {
            if (!DMUiToolkitHud.IsDriving)
                return false;

            DMUiToolkitAcReward host = EnsureHost();
            if (host == null)
                return false;

            host.Present(amountLine, source);
            return true;
        }

        private void Awake()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private static bool uguiHidden;

        private void LateUpdate()
        {
            if (!bound)
                BindTree();

            if (!DMUiToolkitHud.IsDriving)
            {
                uguiHidden = false;
                return;
            }

            if (!uguiHidden)
            {
                HideUgui();
                uguiHidden = true;
            }
        }

        internal void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("acreward-root") ?? tree;
            card = tree.Q<VisualElement>("acreward-card");
            amountLabel = tree.Q<Label>("acreward-amount");
            sourceLabel = tree.Q<Label>("acreward-source");
            if (card != null && routine == null)
                DMUiToolkitOverlayDocument.SetShown(card, false);
            bound = root != null;
        }

        private void Present(string amountLine, string source)
        {
            BindTree();
            if (card == null)
                return;

            if (amountLabel != null)
                amountLabel.text = amountLine ?? string.Empty;
            if (sourceLabel != null)
            {
                bool hasSource = !string.IsNullOrWhiteSpace(source);
                DMUiToolkitOverlayDocument.SetShown(sourceLabel, hasSource);
                if (hasSource)
                    sourceLabel.text = source;
            }

            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(AnimateCard());
        }

        private IEnumerator AnimateCard()
        {
            DMUiToolkitOverlayDocument.SetShown(card, true);
            card.style.opacity = 0f;
            card.style.scale = new Scale(new Vector3(0.2f, 0.2f, 1f));
            yield return new WaitForSecondsRealtime(0.2f);

            float elapsed = 0f;
            const float bounce = 0.5f;
            while (elapsed < bounce)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / bounce);
                float s = 1.70158f;
                float tMinusOne = t - 1f;
                float scaleVal = tMinusOne * tMinusOne * ((s + 1f) * tMinusOne + s) + 1f;
                card.style.scale = new Scale(new Vector3(scaleVal, scaleVal, 1f));
                card.style.opacity = Mathf.Clamp01(elapsed / 0.3f);
                yield return null;
            }

            card.style.scale = new Scale(Vector3.one);
            card.style.opacity = 1f;
            yield return new WaitForSecondsRealtime(2f);

            elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.unscaledDeltaTime;
                card.style.opacity = 1f - Mathf.Clamp01(elapsed / 0.4f);
                yield return null;
            }

            card.style.opacity = 0f;
            DMUiToolkitOverlayDocument.SetShown(card, false);
            routine = null;
        }

        private static void HideUgui()
        {
            if (!DMUiToolkitHud.IsDriving)
                return;

            AcRewardPopup[] popups = Object.FindObjectsByType<AcRewardPopup>(FindObjectsInactive.Include);
            for (int i = 0; i < popups.Length; i++)
            {
                if (popups[i] != null)
                    DMUiToolkitOverlayDocument.HideGameObject(popups[i].gameObject);
            }
        }
    }
}

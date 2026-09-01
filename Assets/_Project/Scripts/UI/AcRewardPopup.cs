using UnityEngine;

namespace Project.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class AcRewardPopup : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Delay in seconds before the popup starts animating")]
        public float delay = 0.2f;

        [Tooltip("Duration of the scale bounce/overshoot effect")]
        public float bounceDuration = 0.5f;

        [Tooltip("Duration of the alpha fade-in effect")]
        public float fadeInDuration = 0.3f;

        [Tooltip("How long the popup stays fully visible before fading out")]
        public float lifetime = 2.0f;

        [Tooltip("Duration of the alpha fade-out effect")]
        public float fadeOutDuration = 0.4f;

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            transform.localScale = Vector3.zero;
        }

        private void Start()
        {
            if (DMUiToolkitHud.IsDriving)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }

            StartCoroutine(AnimatePopup());
        }

        private System.Collections.IEnumerator AnimatePopup()
        {
            yield return new WaitForSecondsRealtime(delay);

            float elapsed = 0f;
            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / bounceDuration);

                float s = 1.70158f;
                float tMinusOne = t - 1f;
                float scaleVal = tMinusOne * tMinusOne * ((s + 1f) * tMinusOne + s) + 1f;

                transform.localScale = new Vector3(scaleVal, scaleVal, 1f);

                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);

                yield return null;
            }

            transform.localScale = Vector3.one;
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            yield return new WaitForSecondsRealtime(lifetime);

            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);

                if (canvasGroup != null)
                    canvasGroup.alpha = 1f - t;

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}

using System;
using System.Collections;
using Project.Interaction;
using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Fades out and closes NPC dialog overlays when the player walks away from the anchor NPC.
    /// </summary>
    public sealed class NpcDialogProximityFade : MonoBehaviour
    {
        public const float MaxDistanceMeters = 2f;
        public const float FadeDurationSeconds = 0.35f;

        private Transform npcAnchor;
        private CanvasGroup canvasGroup;
        private Action onFadeComplete;
        private bool fading;
        private Coroutine fadeRoutine;

        public bool IsMonitoring => npcAnchor != null;

        public void BeginMonitoring(Transform anchor, CanvasGroup group, Action onComplete)
        {
            StopMonitoring();

            if (anchor == null || group == null || onComplete == null)
                return;

            npcAnchor = anchor;
            canvasGroup = group;
            onFadeComplete = onComplete;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            enabled = true;
        }

        public void StopMonitoring()
        {
            enabled = false;
            fading = false;
            npcAnchor = null;
            onFadeComplete = null;

            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            canvasGroup = null;
        }

        private void Update()
        {
            if (npcAnchor == null || canvasGroup == null || fading)
                return;

            if (!PlayerInteractionUtility.TryGetPlayerPosition(out Vector3 playerPosition))
                return;

            if (GetDistanceToAnchor(playerPosition) <= MaxDistanceMeters)
                return;

            fadeRoutine = StartCoroutine(FadeOutAndClose());
        }

        private float GetDistanceToAnchor(Vector3 playerPosition)
        {
            Collider anchorCollider = npcAnchor.GetComponent<Collider>();
            return PlayerInteractionUtility.DistanceToInteractable(
                playerPosition,
                anchorCollider,
                npcAnchor.position);
        }

        private IEnumerator FadeOutAndClose()
        {
            fading = true;
            canvasGroup.blocksRaycasts = false;

            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            while (elapsed < FadeDurationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / FadeDurationSeconds);
                yield return null;
            }

            canvasGroup.alpha = 0f;

            Action callback = onFadeComplete;
            StopMonitoring();
            callback?.Invoke();
        }
    }
}

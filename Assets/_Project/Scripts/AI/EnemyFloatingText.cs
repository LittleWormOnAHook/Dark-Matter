using System.Collections;
using TMPro;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// A self-contained floating text popup that rises above a world position and fades out.
    /// Spawned via EnemyFloatingText.Show() — no prefab required.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyFloatingText : MonoBehaviour
    {
        private TextMeshPro _text;
        private Camera _cam;

        private const float RiseDuration  = 0.8f;
        private const float HoldDuration  = 0.3f;
        private const float FadeDuration  = 0.4f;
        private const float RiseDistance  = 0.65f;
        private const float FontSize      = 3.2f;

        // ── Public API ───────────────────────────────────────────────────────

        public static void Show(Transform origin, string message, Color color, float heightOffset = 2.1f)
        {
            if (origin == null || string.IsNullOrEmpty(message))
                return;

            Vector3 spawnPos = origin.position + Vector3.up * heightOffset;
            GameObject go = new GameObject($"FloatingText_{message}");
            go.transform.position = spawnPos;

            EnemyFloatingText popup = go.AddComponent<EnemyFloatingText>();
            popup.Init(message, color);
        }

        // Convenience overloads for common messages.
        public static void ShowMiss(Transform origin)     => Show(origin, "Miss",     new Color(1f, 0.25f, 0.25f));
        public static void ShowEnraged(Transform origin)  => Show(origin, "Enraged!", new Color(1f, 0.4f,  0f));
        public static void ShowAlert(Transform origin)    => Show(origin, "!",        new Color(1f, 0.85f, 0f));

        // ── Internal ─────────────────────────────────────────────────────────

        private void Init(string message, Color color)
        {
            _cam = Camera.main;

            _text = gameObject.AddComponent<TextMeshPro>();
            _text.text             = message;
            _text.fontSize         = FontSize;
            _text.color            = color;
            _text.alignment        = TextAlignmentOptions.Center;
            _text.fontStyle        = FontStyles.Bold;
            _text.raycastTarget    = false;
            _text.sortingOrder     = 10;

            // Outlines improve legibility at small sizes.
            _text.outlineWidth     = 0.2f;
            _text.outlineColor     = new Color32(0, 0, 0, 200);

            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            Vector3 startPos = transform.position;
            Vector3 endPos   = startPos + Vector3.up * RiseDistance;
            Color   baseColor = _text.color;

            // Rise + hold at full alpha.
            float t = 0f;
            while (t < RiseDuration + HoldDuration)
            {
                BillboardToCamera();

                float rise = Mathf.Clamp01(t / RiseDuration);
                transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, rise));

                t += Time.deltaTime;
                yield return null;
            }

            // Fade out at end position.
            t = 0f;
            while (t < FadeDuration)
            {
                BillboardToCamera();

                float alpha = Mathf.Lerp(1f, 0f, t / FadeDuration);
                _text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

                t += Time.deltaTime;
                yield return null;
            }

            Destroy(gameObject);
        }

        private void BillboardToCamera()
        {
            if (_cam == null)
                return;

            transform.forward = _cam.transform.forward;
        }
    }
}

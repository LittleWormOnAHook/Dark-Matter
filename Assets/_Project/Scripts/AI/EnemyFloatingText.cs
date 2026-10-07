using System.Collections;
using Project.Core;
using TMPro;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// A self-contained floating text popup that rises above a world position and fades out,
    /// or a sticky follow marker that stays above a transform until Dismiss() (charged wind-up tell).
    /// Spawned via EnemyFloatingText.Show() / ShowFollow() — no authored prefab required (runtime template + PoolManager).
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyFloatingText : MonoBehaviour, IPoolable
    {
        private static GameObject templatePrefab;

        private TextMeshPro _text;
        private Camera _cam;
        private Coroutine _animateRoutine;
        private Transform _followTarget;
        private float _followHeightOffset;
        private bool _sticky;

        private const float RiseDuration = 0.8f;
        private const float HoldDuration = 0.3f;
        private const float FadeDuration = 0.4f;
        private const float RiseDistance = 0.65f;
        private const float FontSize = 3.2f;
        private const float StickyFontSize = 4.6f;

        public static void Show(Transform origin, string message, Color color, float heightOffset = 2.1f)
        {
            if (origin == null || string.IsNullOrEmpty(message))
                return;

            Vector3 spawnPos = origin.position + Vector3.up * heightOffset;
            GameObject go = PoolManager.Spawn(EnsureTemplate(), spawnPos, Quaternion.identity);
            if (go == null)
                return;

            EnemyFloatingText popup = go.GetComponent<EnemyFloatingText>();
            popup.Activate(message, color, sticky: false, follow: null, followHeightOffset: 0f);
        }

        /// <summary>
        /// World-space TMP marker that tracks follow + up offset until Dismiss().
        /// Used for the charged-attack wind-up triangle above the enemy head.
        /// </summary>
        public static EnemyFloatingText ShowFollow(Transform follow, string message, Color color, float heightOffset = 0.35f)
        {
            if (follow == null || string.IsNullOrEmpty(message))
                return null;

            Vector3 spawnPos = follow.position + Vector3.up * heightOffset;
            GameObject go = PoolManager.Spawn(EnsureTemplate(), spawnPos, Quaternion.identity);
            if (go == null)
                return null;

            EnemyFloatingText popup = go.GetComponent<EnemyFloatingText>();
            popup.Activate(message, color, sticky: true, follow: follow, followHeightOffset: heightOffset);
            return popup;
        }

        public static void ShowMiss(Transform origin) => Show(origin, "Miss", new Color(1f, 0.25f, 0.25f));
        public static void ShowEnraged(Transform origin) => Show(origin, "Enraged!", new Color(1f, 0.4f, 0f));
        public static void ShowAlert(Transform origin) => Show(origin, "!", new Color(1f, 0.85f, 0f));

        public void OnSpawnedFromPool()
        {
        }

        public void OnReturnedToPool()
        {
            if (_animateRoutine != null)
            {
                StopCoroutine(_animateRoutine);
                _animateRoutine = null;
            }

            _followTarget = null;
            _sticky = false;
        }

        /// <summary>Ends a sticky follow marker (no-op if already released / not sticky).</summary>
        public void Dismiss()
        {
            if (!_sticky)
                return;

            _sticky = false;
            _followTarget = null;

            if (_animateRoutine != null)
            {
                StopCoroutine(_animateRoutine);
                _animateRoutine = null;
            }

            PoolManager.Release(gameObject);
        }

        private static GameObject EnsureTemplate()
        {
            if (templatePrefab != null)
                return templatePrefab;

            templatePrefab = new GameObject("EnemyFloatingTextTemplate");
            templatePrefab.SetActive(false);
            EnemyFloatingText popup = templatePrefab.AddComponent<EnemyFloatingText>();
            popup.EnsureTextComponent();
            Object.DontDestroyOnLoad(templatePrefab);
            return templatePrefab;
        }

        private void Activate(string message, Color color, bool sticky, Transform follow, float followHeightOffset)
        {
            EnsureTextComponent();
            _cam = Camera.main;

            _sticky = sticky;
            _followTarget = follow;
            _followHeightOffset = followHeightOffset;

            _text.text = message;
            _text.color = color;
            _text.fontSize = sticky ? StickyFontSize : FontSize;
            _text.alignment = TextAlignmentOptions.Center;
            _text.fontStyle = FontStyles.Bold;
            _text.raycastTarget = false;
            _text.sortingOrder = 10;
            _text.outlineWidth = 0.2f;
            _text.outlineColor = new Color32(0, 0, 0, 200);

            if (_animateRoutine != null)
                StopCoroutine(_animateRoutine);

            if (sticky)
            {
                SnapToFollow();
                BillboardToCamera();
                _animateRoutine = StartCoroutine(FollowSticky());
            }
            else
            {
                _animateRoutine = StartCoroutine(Animate());
            }
        }

        private IEnumerator FollowSticky()
        {
            while (_sticky)
            {
                if (_followTarget == null)
                    break;

                SnapToFollow();
                BillboardToCamera();
                yield return null;
            }

            _animateRoutine = null;
            if (_sticky)
            {
                _sticky = false;
                PoolManager.Release(gameObject);
            }
        }

        private void SnapToFollow()
        {
            if (_followTarget == null)
                return;

            transform.position = _followTarget.position + Vector3.up * _followHeightOffset;
        }

        private IEnumerator Animate()
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.up * RiseDistance;
            Color baseColor = _text.color;

            float t = 0f;
            while (t < RiseDuration + HoldDuration)
            {
                BillboardToCamera();

                float rise = Mathf.Clamp01(t / RiseDuration);
                transform.position = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0f, 1f, rise));

                t += Time.deltaTime;
                yield return null;
            }

            t = 0f;
            while (t < FadeDuration)
            {
                BillboardToCamera();

                float alpha = Mathf.Lerp(1f, 0f, t / FadeDuration);
                _text.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

                t += Time.deltaTime;
                yield return null;
            }

            _animateRoutine = null;
            PoolManager.Release(gameObject);
        }

        private void BillboardToCamera()
        {
            if (_cam == null)
                _cam = Camera.main;

            if (_cam == null)
                return;

            transform.forward = _cam.transform.forward;
        }

        private void EnsureTextComponent()
        {
            if (_text != null)
                return;

            _text = GetComponent<TextMeshPro>();
            if (_text == null)
                _text = gameObject.AddComponent<TextMeshPro>();
        }
    }
}


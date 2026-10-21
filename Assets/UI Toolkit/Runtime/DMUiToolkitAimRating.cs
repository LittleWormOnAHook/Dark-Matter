using System.Collections.Generic;
using Invector.vCharacterController;
using Invector.vShooter;
using Project.Combat;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// 2026-10-20r Aim Rating popup (UITK only, hosted in the optics overlay UIDocument - no new document, no uGUI).
    /// On each player shot the scope reticle offset (DMScopeMode.ReticleSway) is measured as a fraction of its current
    /// max travel (DMScopeMode.ReticleSwayMax). If the shot hits within the hit window (or Require Hit is off):
    /// fraction &lt;= Perfect -> "Perfect!", &lt;= Good -> "Good!", &lt;= OK -> "OK!", else nothing. Pop-scale then fade.
    /// Settings: DM_CombatCoreProfile "Aim Rating".
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMUiToolkitAimRating : MonoBehaviour
    {
        private sealed class Popup { public Label L; public float T; public Vector2 Anchor; public Color C; }

        private readonly List<Popup> _popups = new List<Popup>();
        private vShooterWeapon _weapon;
        private bool _pending;
        private float _shotTime, _shotFrac;
        private Vector2 _shotSway;
        private GameObject _player;
        private float _nextFind;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!Application.isPlaying) return;
            var go = new GameObject("DMUiToolkitAimRating") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            go.AddComponent<DMUiToolkitAimRating>();
        }

        private void OnEnable() { CombatEvents.HitApplied += OnHit; }

        private void OnDisable()
        {
            CombatEvents.HitApplied -= OnHit;
            if (_weapon != null) _weapon.onShot.RemoveListener(OnShot);
            _weapon = null;
            for (int i = 0; i < _popups.Count; i++) _popups[i].L?.RemoveFromHierarchy();
            _popups.Clear();
        }

        private void Update()
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (Time.unscaledTime >= _nextFind)
            {
                _nextFind = Time.unscaledTime + 0.5f;
                _player = Project.Core.PlayerLocator.FindPlayerObject();
                vShooterMeleeInput input = _player != null ? _player.GetComponent<vShooterMeleeInput>() : null;
                vShooterWeapon w = input != null && input.shooterManager != null ? input.shooterManager.rWeapon : null;
                if (w != _weapon)
                {
                    if (_weapon != null) _weapon.onShot.RemoveListener(OnShot);
                    _weapon = w;
                    if (_weapon != null) _weapon.onShot.AddListener(OnShot);
                }
            }
            if (_pending && p != null && Time.time - _shotTime > p.aimRatingHitWindowSeconds) _pending = false;
            Animate(p);
        }

        private void OnShot()
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || !p.aimRatingEnabled) return;
            bool scoped = DMScopeMode.IsScopeViewActive;
            if (p.aimRatingScopedOnly && !scoped) return;
            Vector2 sway = DMScopeMode.ReticleSway;
            float max = DMScopeMode.ReticleSwayMax;
            // Max radial travel of the sway is the per-axis amplitude (Perlin rarely reaches the corners).
            _shotFrac = max > 0.001f ? Mathf.Clamp01(sway.magnitude / max) : 0f;
            _shotSway = sway;
            _shotTime = Time.time;
            if (p.aimRatingRequireHit) _pending = true;
            else Show(p, _shotFrac, sway);
        }

        private void OnHit(DamageInfo info, GameObject target)
        {
            if (!_pending || target == null) return;
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || !p.aimRatingEnabled) { _pending = false; return; }
            if (_player != null && info.Source != null && info.Source.transform.root != _player.transform.root) return;
            if (_player != null && target.transform.root == _player.transform.root) return;
            _pending = false;
            Show(p, _shotFrac, _shotSway);
        }

        private void Show(DM_CombatCoreProfile p, float frac, Vector2 sway)
        {
            string text; Color c;
            if (frac <= p.aimRatingPerfect) { text = "Perfect!"; c = p.aimRatingPerfectColor; }
            else if (frac <= p.aimRatingGood) { text = "Good!"; c = p.aimRatingGoodColor; }
            else if (frac <= p.aimRatingOk) { text = "OK!"; c = p.aimRatingOkColor; }
            else return;

            VisualElement host = DMUiToolkitOpticsOverlay.PopupLayer;
            if (host == null) return;
            var l = new Label(text) { pickingMode = PickingMode.Ignore };
            l.style.position = Position.Absolute;
            l.style.left = new Length(50, LengthUnit.Percent);
            l.style.top = new Length(50, LengthUnit.Percent);
            l.style.unityTextAlign = TextAnchor.MiddleCenter;
            l.style.unityFontStyleAndWeight = FontStyle.Bold;
            l.style.fontSize = p.aimRatingFontSize * p.aimRatingScale;
            l.style.color = c;
            l.style.width = 400; l.style.height = 80;
            l.style.marginLeft = -200; l.style.marginTop = -40;
            l.style.unityTextOutlineWidth = 1.2f;
            l.style.unityTextOutlineColor = new Color(0f, 0f, 0f, 0.85f);
            host.Add(l);
            l.BringToFront();
            // Newest replaces older ones so rapid fire does not stack.
            for (int i = 0; i < _popups.Count; i++) _popups[i].L?.RemoveFromHierarchy();
            _popups.Clear();
            _popups.Add(new Popup { L = l, T = 0f, Anchor = p.aimRatingFollowReticle ? sway : Vector2.zero, C = c });
        }

        private void Animate(DM_CombatCoreProfile p)
        {
            if (_popups.Count == 0) return;
            float dur = p != null ? p.aimRatingDuration : 0.9f;
            float pop = p != null ? p.aimRatingPopScale : 1.4f;
            Vector2 off = p != null ? p.aimRatingOffset : new Vector2(0f, -72f);
            for (int i = _popups.Count - 1; i >= 0; i--)
            {
                Popup u = _popups[i];
                u.T += Time.unscaledDeltaTime;
                float k = u.T / Mathf.Max(0.05f, dur);
                if (k >= 1f || u.L == null || u.L.panel == null) { u.L?.RemoveFromHierarchy(); _popups.RemoveAt(i); continue; }
                // Pop: overshoot to pop scale in the first 12%, settle to 1, fade over the last 40%, slight rise.
                float s = k < 0.12f ? Mathf.Lerp(0.4f, pop, k / 0.12f) : Mathf.Lerp(pop, 1f, Mathf.Clamp01((k - 0.12f) / 0.18f));
                float a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
                u.L.style.scale = new Scale(new Vector3(s, s, 1f));
                u.L.style.opacity = a;
                u.L.style.translate = new Translate(u.Anchor.x + off.x, u.Anchor.y + off.y - k * 18f);
            }
        }
    }
}

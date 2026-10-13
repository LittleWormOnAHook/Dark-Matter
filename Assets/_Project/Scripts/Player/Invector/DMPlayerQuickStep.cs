using Invector.vCharacterController;
using Project.Combat;
using Project.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Player.Invector
{
    /// <summary>
    /// Triple-tap A / D = sidestep, triple-tap S = backstep (pad: triple left-stick flick). Camera-relative, wall-safe, no roll.
    /// Added at runtime by PioneerPlayerInputBinder (no prefab edit). Settings: DM_CombatCoreProfile "Player quick step".
    /// </summary>
    [DisallowMultipleComponent]
    public class DMPlayerQuickStep : MonoBehaviour
    {
        private enum Dir { None, Left, Right, Back }

        private vThirdPersonController _cc;
        private Rigidbody _rb;
        private Dir _lastDir;
        private int _count;
        private float _firstTapTime;
        private bool _stickArmedL = true, _stickArmedR = true, _stickArmedB = true;
        private float _nextAllowed;
        private Vector3 _stepDir;
        private float _stepDist, _stepDone, _stepStart, _stepDur;
        private bool _stepping;

        public bool IsStepping => _stepping;

        private void Awake()
        {
            _cc = GetComponent<vThirdPersonController>() ?? GetComponentInChildren<vThirdPersonController>();
            _rb = _cc != null ? _cc.GetComponent<Rigidbody>() : null;
        }

        private void Update()
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || !p.quickStepEnabled || _cc == null)
                return;
            if (GameplayKeyboardShortcuts.IsGameplayInputLockedByUi())
                return;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.wasPressedThisFrame) Tap(Dir.Left, p);
                if (kb.dKey.wasPressedThisFrame) Tap(Dir.Right, p);
                if (kb.sKey.wasPressedThisFrame) Tap(Dir.Back, p);
            }

            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 s = pad.leftStick.ReadValue();
                Flick(ref _stickArmedL, -s.x, Dir.Left, p);
                Flick(ref _stickArmedR, s.x, Dir.Right, p);
                Flick(ref _stickArmedB, -s.y, Dir.Back, p);
            }
        }

        private void Flick(ref bool armed, float v, Dir d, DM_CombatCoreProfile p)
        {
            if (armed && v >= p.quickStepStickFlick) { armed = false; Tap(d, p); }
            else if (!armed && v <= p.quickStepStickRelease) armed = true;
        }

        private void Tap(Dir d, DM_CombatCoreProfile p)
        {
            float now = Time.time;
            if (d != _lastDir || now - _firstTapTime > p.quickStepTapWindowSeconds)
            {
                _lastDir = d;
                _count = 1;
                _firstTapTime = now;
                return;
            }
            if (++_count < 3)
                return;
            _count = 0;
            _lastDir = Dir.None;
            TryStep(d, p);
        }

        private void TryStep(Dir d, DM_CombatCoreProfile p)
        {
            if (_stepping || Time.time < _nextAllowed)
                return;
            if (!_cc.isGrounded || _cc.customAction || _cc.isRolling || _cc.isJumping || _cc.isDead)
                return;
            if (p.quickStepStaminaCost > 0f && _cc.currentStamina < p.quickStepStaminaCost)
                return;

            Transform cam = Camera.main != null ? Camera.main.transform : _cc.transform;
            Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 0.01f) { fwd = _cc.transform.forward; right = _cc.transform.right; }
            _stepDir = d == Dir.Left ? -right : d == Dir.Right ? right : -fwd;
            _stepDist = d == Dir.Back ? p.backstepDistance : p.sidestepDistance;
            _stepDur = Mathf.Max(0.05f, p.quickStepDurationSeconds);
            _stepDone = 0f;
            _stepStart = Time.time;
            _stepping = true;
            _nextAllowed = Time.time + _stepDur + p.quickStepCooldownSeconds;
            if (p.quickStepStaminaCost > 0f)
                _cc.ReduceStamina(p.quickStepStaminaCost, false);
            if (DM_CombatCoreProfile.Live.logInterruptsInPlay)
                Debug.Log("[QuickStep] " + d + " dist=" + _stepDist.ToString("0.00"));
        }

        private void FixedUpdate()
        {
            if (!_stepping)
                return;
            float t = Mathf.Clamp01((Time.time - _stepStart) / _stepDur);
            float eased = 1f - (1f - t) * (1f - t);
            float want = _stepDist * eased - _stepDone;
            if (want > 0f)
            {
                Vector3 pos = _rb != null ? _rb.position : _cc.transform.position;
                CapsuleCollider cap = _cc.GetComponent<CapsuleCollider>();
                float radius = cap != null ? cap.radius * 0.9f : 0.3f;
                float height = cap != null ? cap.height : 1.8f;
                Vector3 bottom = pos + Vector3.up * (radius + 0.25f);
                Vector3 top = pos + Vector3.up * Mathf.Max(radius + 0.3f, height - radius);
                if (Physics.CapsuleCast(bottom, top, radius, _stepDir, out RaycastHit hit, want + 0.05f, _cc.groundLayer, QueryTriggerInteraction.Ignore))
                {
                    want = Mathf.Max(0f, hit.distance - 0.05f);
                    t = 1f;
                }
                Vector3 next = pos + _stepDir * want;
                if (_rb != null) _rb.MovePosition(next); else _cc.transform.position = next;
                _stepDone += want;
            }
            if (t >= 1f || _cc.isDead || _cc.customAction)
                _stepping = false;
        }
    }
}

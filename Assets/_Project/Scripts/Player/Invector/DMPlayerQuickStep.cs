using Invector.vCharacterController;
using Project.Combat;
using Project.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Player.Invector
{
    /// <summary>
    /// 2026-10-25d sidestep. KBM: double-tap A / D = sidestep, double-tap S = backstep (tap count in Studio).
    /// Pad: hold left stick left / right (or back) + HOLD A past quickStepPadHoldSeconds = step; a quick tap of A still jumps
    /// (the jump is deferred until release so it never fires first). Optional legacy triple stick flick.
    /// Camera-relative, wall-safe, no roll. Added at runtime by PioneerPlayerInputBinder. Settings: DM_CombatCoreProfile "Player quick step".
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
        private bool _padPending;
        private float _padPressTime;
        private bool _deferredJump;
        private float _deferredJumpTime;

        public bool IsStepping => _stepping;
        public bool IsPadHoldPending => _padPending;

        private void Awake()
        {
            _cc = GetComponent<vThirdPersonController>() ?? GetComponentInChildren<vThirdPersonController>();
            _rb = _cc != null ? _cc.GetComponent<Rigidbody>() : null;
        }

        /// <summary>Called by JumpInput on the A (South) press. True = this press may become a step; the jump is held back until release.</summary>
        public bool TryDeferPadJump()
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || !p.quickStepEnabled || !p.quickStepPadHoldEnabled || _cc == null || _padPending)
                return false;
            Gamepad pad = Gamepad.current;
            if (pad == null || !pad.buttonSouth.wasPressedThisFrame)
                return false;
            if (_stepping || Time.time < _nextAllowed || !_cc.isGrounded || _cc.customAction || _cc.isRolling || _cc.isJumping || _cc.isDead)
                return false;
            if (PadDir(pad, p) == Dir.None)
                return false;
            _padPending = true;
            _padPressTime = Time.unscaledTime;
            return true;
        }

        /// <summary>True once when a deferred A press turned out to be a tap (jump now).</summary>
        public bool ConsumeDeferredJump()
        {
            if (!_deferredJump)
                return false;
            _deferredJump = false;
            return Time.unscaledTime - _deferredJumpTime < 0.25f;
        }

        private static Dir PadDir(Gamepad pad, DM_CombatCoreProfile p)
        {
            Vector2 s = pad.leftStick.ReadValue();
            float defl = p.quickStepPadDeflection;
            if (Mathf.Abs(s.x) >= defl && Mathf.Abs(s.x) >= -s.y)
                return s.x < 0f ? Dir.Left : Dir.Right;
            if (p.quickStepPadBackstep && -s.y >= defl)
                return Dir.Back;
            return Dir.None;
        }

        private void Update()
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || !p.quickStepEnabled || _cc == null)
            {
                _padPending = false;
                return;
            }

            if (_padPending)
                TickPadHold(p);

            if (GameplayKeyboardShortcuts.IsGameplayInputLockedByUi())
                return;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.wasPressedThisFrame) Tap(Dir.Left, p);
                if (kb.dKey.wasPressedThisFrame) Tap(Dir.Right, p);
                if (p.quickStepKbmBackstep && kb.sKey.wasPressedThisFrame) Tap(Dir.Back, p);
            }

            Gamepad pad = Gamepad.current;
            if (pad != null && p.quickStepPadTripleFlick)
            {
                Vector2 s = pad.leftStick.ReadValue();
                Flick(ref _stickArmedL, -s.x, Dir.Left, p);
                Flick(ref _stickArmedR, s.x, Dir.Right, p);
                Flick(ref _stickArmedB, -s.y, Dir.Back, p);
            }
        }

        private void TickPadHold(DM_CombatCoreProfile p)
        {
            Gamepad pad = Gamepad.current;
            if (pad == null)
            {
                _padPending = false;
                return;
            }
            if (!pad.buttonSouth.isPressed)
            {
                // Released before the hold threshold: it was a tap, jump.
                _padPending = false;
                _deferredJump = true;
                _deferredJumpTime = Time.unscaledTime;
                return;
            }
            if (Time.unscaledTime - _padPressTime < p.quickStepPadHoldSeconds)
                return;
            _padPending = false;
            Dir d = PadDir(pad, p);
            if (d == Dir.None || !TryStep(d, p))
            {
                _deferredJump = true;
                _deferredJumpTime = Time.unscaledTime;
            }
        }

        private void Flick(ref bool armed, float v, Dir d, DM_CombatCoreProfile p)
        {
            if (armed && v >= p.quickStepStickFlick) { armed = false; FlickTap(d, p); }
            else if (!armed && v <= p.quickStepStickRelease) armed = true;
        }

        private void FlickTap(Dir d, DM_CombatCoreProfile p) => Tap(d, p, 3);

        private void Tap(Dir d, DM_CombatCoreProfile p) => Tap(d, p, Mathf.Clamp(p.quickStepKbmTapCount, 2, 3));

        private void Tap(Dir d, DM_CombatCoreProfile p, int need)
        {
            float now = Time.time;
            if (d != _lastDir || now - _firstTapTime > p.quickStepTapWindowSeconds)
            {
                _lastDir = d;
                _count = 1;
                _firstTapTime = now;
                return;
            }
            if (++_count < need)
                return;
            _count = 0;
            _lastDir = Dir.None;
            TryStep(d, p);
        }

        private bool TryStep(Dir d, DM_CombatCoreProfile p)
        {
            if (_stepping || Time.time < _nextAllowed)
                return false;
            if (!_cc.isGrounded || _cc.customAction || _cc.isRolling || _cc.isJumping || _cc.isDead)
                return false;
            if (p.quickStepStaminaCost > 0f && _cc.currentStamina < p.quickStepStaminaCost)
                return false;

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
            if (p.logInterruptsInPlay)
                Debug.Log("[QuickStep] " + d + " dist=" + _stepDist.ToString("0.00"));
            return true;
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

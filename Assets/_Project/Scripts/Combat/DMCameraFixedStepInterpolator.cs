using Invector.vCamera;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-20e: Invector's vThirdPersonCamera moves in FixedUpdate (fixed-step), while the player Rigidbody is
    /// interpolated per render frame (DM_CombatCoreProfile.playerRigidbodyInterpolation). Camera stepping vs. smooth
    /// player = slight player jitter on screen while strafing + panning + aiming. This interpolates the camera pose
    /// between its last two fixed steps for rendering only, and restores Invector's own pose before its next step.
    /// Other LateUpdate offsets (shake, collision) are preserved as a delta. Toggle: DM_CombatCoreProfile.
    /// </summary>
    [DefaultExecutionOrder(32000)]
    [DisallowMultipleComponent]
    public sealed class DMCameraFixedStepInterpolator : MonoBehaviour
    {
        private vThirdPersonCamera _cam;
        private Vector3 _prevPos, _currPos;
        private Quaternion _prevRot, _currRot;
        private bool _has, _applied;
        private float _currTime;

        public static void EnsureOnCamera()
        {
            vThirdPersonCamera cam = vThirdPersonCamera.instance;
            if (cam != null && cam.GetComponent<DMCameraFixedStepInterpolator>() == null)
                { cam.gameObject.AddComponent<DMCameraFixedStepInterpolator>(); cam.gameObject.AddComponent<DMCameraFixedStepRestore>(); }
        }

        private static bool On
        {
            get { DM_CombatCoreProfile p = DM_CombatCoreProfile.Live; return p != null && p.playerCameraFixedStepInterpolation && !p.cameraFollowInLateUpdate; } // 2026-10-25c: ignored when camera follows in LateUpdate
        }

        private void Awake() { _cam = GetComponent<vThirdPersonCamera>(); }

        private bool Usable => _cam != null && _cam.enabled && !_cam.isFreezed && On;

        // Runs after vThirdPersonCamera.FixedUpdate (order 32000); a helper restores before it (see Restore()).
        private void FixedUpdate()
        {
            if (!Usable) { _has = false; _applied = false; return; }
            Transform t = transform;
            if (!_has) { _prevPos = t.position; _prevRot = t.rotation; }
            else { _prevPos = _currPos; _prevRot = _currRot; }
            _currPos = t.position; _currRot = t.rotation;
            _currTime = Time.fixedTime;
            _has = true;
            _applied = false;
        }

        /// <summary>Put Invector's own fixed-step pose back (called early in FixedUpdate and Update).</summary>
        internal void Restore()
        {
            if (!_applied) return;
            _applied = false;
            // 2026-10-20h: restore Invector's pure pose only (re-adding the late delta accumulated shake/collision into Invector's smoothing).
            transform.SetPositionAndRotation(_currPos, _currRot);
        }

        private Vector3 _lateDeltaPos;
        private Quaternion _lateDeltaRot = Quaternion.identity;

        private void LateUpdate()
        {
            if (!_has || !Usable) { _applied = false; return; }
            Transform t = transform;
            // Anything that moved the camera after its fixed step this frame (shake, collision overlay) is kept as a delta.
            _lateDeltaPos = t.position - _currPos;
            _lateDeltaRot = t.rotation * Quaternion.Inverse(_currRot);
            float dt = Time.fixedDeltaTime;
            float a = dt > 0f ? Mathf.Clamp01((Time.time - _currTime) / dt) : 1f;
            Vector3 pos = Vector3.LerpUnclamped(_prevPos, _currPos, a) + _lateDeltaPos;
            Quaternion rot = _lateDeltaRot * Quaternion.Slerp(_prevRot, _currRot, a);
            // Big jump (teleport / state snap): no smoothing.
            if ((_currPos - _prevPos).sqrMagnitude > 9f) { pos = _currPos + _lateDeltaPos; rot = _lateDeltaRot * _currRot; }
            t.SetPositionAndRotation(pos, rot);
            _applied = true;
        }
    }

    /// <summary>Early hook: restores the camera's true pose before Invector/gameplay read or step it.</summary>
    [DefaultExecutionOrder(-32000)]
    [DisallowMultipleComponent]
    public sealed class DMCameraFixedStepRestore : MonoBehaviour
    {
        private DMCameraFixedStepInterpolator _i;
        private void Awake() { _i = GetComponent<DMCameraFixedStepInterpolator>(); }
        private void FixedUpdate() { _i?.Restore(); }
        private void Update() { _i?.Restore(); }
    }
}

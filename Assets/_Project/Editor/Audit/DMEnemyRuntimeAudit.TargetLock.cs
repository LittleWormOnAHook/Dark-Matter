using Project.AI;
using UnityEngine;

namespace Project.EditorTools.Audit
{
    /// <summary>2026-10-13: audit player target stickiness (min 1.5 s lock, switch only when the current target is dead / out of range
    /// or the candidate is 30% closer) and smoothed camera yaw; logs [AuditTarget] switches with the running rate.</summary>
    public static partial class DMEnemyRuntimeAudit
    {
        private const float AuditTargetLockSeconds = 1.5f, AuditTargetCloser = 0.7f, AuditTargetMaxRange = 9f, AuditAimYawSpeed = 220f;
        private static Live _auditTarget;
        private static float _auditTargetSince, _auditTargetFirst = -1f, _auditAimYaw = float.NaN;
        private static int _auditTargetSwitches;

        private static Live StickyAuditTarget(Live cand)
        {
            if (_player == null || cand == null) return cand;
            float now = Time.time;
            if (_auditTargetFirst < 0f) _auditTargetFirst = now;
            bool curOk = _auditTarget != null && _auditTarget.go != null && _auditTarget.go.activeInHierarchy
                && (_auditTarget.health == null || !_auditTarget.health.IsDead)
                && Flat(_auditTarget.go.transform.position - _player.transform.position) <= AuditTargetMaxRange;
            if (curOk && (cand == _auditTarget || now - _auditTargetSince < AuditTargetLockSeconds)) return _auditTarget;
            if (curOk && cand.go != null && Flat(cand.go.transform.position - _player.transform.position) >= AuditTargetCloser * Flat(_auditTarget.go.transform.position - _player.transform.position))
                return _auditTarget;
            if (_auditTarget != null && _auditTarget != cand)
            {
                _auditTargetSwitches++;
                float span = Mathf.Max(1f, now - _auditTargetFirst);
                Debug.Log("[AuditTarget] switch -> " + (cand.go != null ? cand.go.name : "?") + " (" + (curOk ? "closer" : "current dead/out of range") + ") switches=" + _auditTargetSwitches + " rate=" + (_auditTargetSwitches / span).ToString("0.00") + "/s");
            }
            _auditTarget = cand;
            _auditTargetSince = now;
            return cand;
        }

        private static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

        private static void AimCameraAtSmooth(Vector3 world)
        {
            if (_player == null) return;
            Vector3 d = world - _player.transform.position; d.y = 0f;
            if (d.sqrMagnitude < 0.01f) return;
            float want = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            _auditAimYaw = float.IsNaN(_auditAimYaw) ? want : Mathf.MoveTowardsAngle(_auditAimYaw, want, AuditAimYawSpeed * Time.deltaTime);
            float r = _auditAimYaw * Mathf.Deg2Rad;
            AimCameraAt(_player.transform.position + new Vector3(Mathf.Sin(r), 0f, Mathf.Cos(r)) * 5f);
        }
    }
}

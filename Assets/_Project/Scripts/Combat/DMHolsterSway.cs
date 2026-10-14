using System.Collections.Generic;
using Project.AI;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-13: runtime spring sway for every active Holstered_ weapon (player + enemies). Rotates the holster object around a pivot
    /// ~2/3 up the weapon (DM_CombatCoreProfile "Holstered weapon sway"). Rest pose = the placement the object had when sway attached
    /// (re-captured whenever other code moves it). The rest pose is restored on disable; nothing is saved. Play mode only.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMHolsterSway : MonoBehaviour
    {
        private Vector3 _restPos, _lastAppliedPos;
        private Quaternion _restRot, _lastAppliedRot;
        private Vector3 _pivotLocal;   // in parent space
        private Vector3 _lever;        // parent space, pivot -> weapon centre (unit)
        private Vector3 _theta, _omega; // angular offset / velocity, parent space (rad)
        private Vector3 _prevPivotWorld, _prevVel;
        private bool _init, _hasPrev;

        private void OnEnable() { _init = false; }

        private void OnDisable()
        {
            if (_init && transform.localPosition == _lastAppliedPos && transform.localRotation == _lastAppliedRot)
                transform.SetLocalPositionAndRotation(_restPos, _restRot);
            _init = false;
        }

        private void Capture()
        {
            _restPos = transform.localPosition;
            _restRot = transform.localRotation;
            _theta = _omega = Vector3.zero;
            _hasPrev = false;
            Transform parent = transform.parent;
            Renderer[] rs = GetComponentsInChildren<Renderer>(false);
            if (parent == null || rs.Length == 0) { _init = false; return; }
            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            // Long axis of the weapon in world space: compare the object's own axes projected on the bounds.
            Vector3[] axes = { transform.right, transform.up, transform.forward };
            Vector3 axis = axes[0]; float best = -1f;
            foreach (Vector3 a in axes)
            {
                float len = Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(a.x), Mathf.Abs(a.y), Mathf.Abs(a.z))));
                if (len > best) { best = len; axis = a; }
            }
            if (Vector3.Dot(axis, Vector3.up) < 0f) axis = -axis; // axis points to the upper end
            float half = Mathf.Max(0.05f, best);
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            float t = p != null ? p.holsterSwayPivot : 0.66f;
            Vector3 lower = b.center - axis * half;
            Vector3 pivotW = lower + axis * (2f * half * t);
            _pivotLocal = parent.InverseTransformPoint(pivotW);
            Vector3 lev = parent.InverseTransformDirection(b.center - pivotW);
            _lever = lev.sqrMagnitude > 1e-6f ? lev.normalized : parent.InverseTransformDirection(-axis);
            _init = true;
            _lastAppliedPos = _restPos; _lastAppliedRot = _restRot;
        }

        private void LateUpdate()
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || p.holsterSwayAmount <= 0f || !Application.isPlaying) return;
            if (GetComponentInParent<EnemyHealth>() != null || name.StartsWith("Drawn_", System.StringComparison.Ordinal)) { enabled = false; return; } // 2026-10-13b
            if (!_init) { Capture(); if (!_init) return; }
            // Someone else (placement / holster code) moved it: that is the new rest pose.
            if (transform.localPosition != _lastAppliedPos || transform.localRotation != _lastAppliedRot) Capture();

            Transform parent = transform.parent;
            float dt = Mathf.Clamp(Time.deltaTime, 0.0001f, 0.05f);
            Vector3 pivotW = parent.TransformPoint(_pivotLocal);
            Vector3 accW = Vector3.zero;
            if (_hasPrev)
            {
                Vector3 vel = (pivotW - _prevPivotWorld) / dt;
                accW = Vector3.ClampMagnitude((vel - _prevVel) / dt, 60f);
                _prevVel = vel;
            }
            else _prevVel = Vector3.zero;
            _prevPivotWorld = pivotW; _hasPrev = true;

            DM_CombatCoreProfile.HolsterSwayAdvanced a = p.holsterSwayAdvanced ?? new DM_CombatCoreProfile.HolsterSwayAdvanced();
            float amt = p.holsterSwayAmount;
            Vector3 accL = parent.InverseTransformDirection(accW);
            Vector3 curLever = Quaternion.Euler(_theta * Mathf.Rad2Deg) * _lever;
            Vector3 downL = parent.InverseTransformDirection(Vector3.down);
            Vector3 torque = Vector3.Cross(curLever, -accL) * (a.inertia * amt * 0.1f)
                           + Vector3.Cross(curLever, downL) * (a.gravity * amt * 0.1f)
                           - _theta * a.stiffness - _omega * a.damping;
            _omega += torque * dt;
            _theta += _omega * dt;
            float max = a.maxAngle * Mathf.Deg2Rad * Mathf.Min(1f, amt);
            if (_theta.magnitude > max) { _theta = _theta.normalized * max; _omega *= 0.5f; }

            Quaternion q = Quaternion.Euler(_theta * Mathf.Rad2Deg);
            Vector3 pos = _pivotLocal + q * (_restPos - _pivotLocal);
            Quaternion rot = q * _restRot;
            transform.SetLocalPositionAndRotation(pos, rot);
            _lastAppliedPos = transform.localPosition; _lastAppliedRot = transform.localRotation;
        }
    }

    /// <summary>Attaches DMHolsterSway to active Holstered_ objects on the player and enemies (round-robin scan, one root per frame).</summary>
    public sealed class DMHolsterSwayManager : MonoBehaviour
    {
        private static DMHolsterSwayManager _inst;
        private readonly List<Transform> _roots = new List<Transform>();
        private int _cursor;
        private float _nextRefresh;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (_inst != null) return;
            var go = new GameObject("DM_HolsterSway") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            _inst = go.AddComponent<DMHolsterSwayManager>();
        }

        private void Update()
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || p.holsterSwayAmount <= 0f) return;
            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + 2f;
                _roots.Clear();
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _roots.Add(player.transform);
                // 2026-10-13b: enemies get no holster sway (player only).
            }
            if (_roots.Count == 0) return;
            _cursor = (_cursor + 1) % _roots.Count;
            Transform r = _roots[_cursor];
            if (r == null) return;
            foreach (Transform t in r.GetComponentsInChildren<Transform>(false))
                if (t.name.StartsWith("Holstered_", System.StringComparison.Ordinal) && t.GetComponent<DMHolsterSway>() == null)
                    t.gameObject.AddComponent<DMHolsterSway>();
        }
    }
}

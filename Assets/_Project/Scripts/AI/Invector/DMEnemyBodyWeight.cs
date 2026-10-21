using Project.Combat;
using Project.Features.Climb;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// 2026-10-20w: enemy body weight (visual only). Torso lean into acceleration, stop carry with an underdamped settle,
    /// attack windup / follow-through, directional hit reaction. Spine and chest bones only: never touches weapons, hands,
    /// hitboxes or timing. Fully off (no writes) while dead, down (DMEnemyKnockdown), ragdolled or hit-staggered, during a
    /// flinch, at animator speed ~0 (hit-stop / stall guard) and beyond the cull distance or off-screen.
    /// Settings: DM_ClimbDashProfile "Body weight: enemies" (shares the Body Weight master toggle, presets and Reset).
    /// Added from DMEnemyKnockdown.Awake.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(3321)]
    public sealed class DMEnemyBodyWeight : MonoBehaviour
    {
        private Animator _anim;
        private Transform _spine, _chest;
        private Quaternion _spineBase, _chestBase, _spineW, _chestW;
        private bool _has;
        private Vector3 _lastPos, _vel, _acc, _accVel;
        private bool _hasPos;
        private Vector3 _trunk, _trunkVel, _hit, _hitVel;
        private float _w;
        private bool _wasAttacking;
        private EnemyHealth _health;
        private EnemyInvectorRagdollBridge _bridge;
        private DMEnemyHitFlinch _flinch;
        private EnemyCombat _combat;
        private Renderer _renderer;
        private float _typeScale = 1f;
        private string _typeScaleKey;
        private bool _ready;

        private void Awake()
        {
            _anim = GetComponentInChildren<Animator>();
            _health = GetComponent<EnemyHealth>();
            _bridge = GetComponent<EnemyInvectorRagdollBridge>();
            _flinch = GetComponent<DMEnemyHitFlinch>();
            _combat = GetComponent<EnemyCombat>();
            _renderer = GetComponentInChildren<SkinnedMeshRenderer>();
            if (_anim != null && _anim.isHuman)
            {
                _spine = _anim.GetBoneTransform(HumanBodyBones.Spine);
                _chest = _anim.GetBoneTransform(HumanBodyBones.Chest);
                if (_chest == null) _chest = _anim.GetBoneTransform(HumanBodyBones.UpperChest);
            }
            _ready = _spine != null && _chest != null;
        }

        private void OnEnable() { CombatEvents.HitApplied += OnHit; }
        private void OnDisable() { CombatEvents.HitApplied -= OnHit; Clear(); }

        private void Clear() { _trunk = _trunkVel = _hit = _hitVel = Vector3.zero; _w = 0f; _has = false; _hasPos = false; }

        private float TypeScale(DM_ClimbDashProfile p)
        {
            string spec = p.bodyWeightEnemyTypeScales ?? "";
            if (_typeScaleKey == spec) return _typeScale;
            _typeScaleKey = spec; _typeScale = 1f;
            foreach (string part in spec.Split(';'))
            {
                int eq = part.IndexOf('=');
                if (eq <= 0) continue;
                string key = part.Substring(0, eq).Trim();
                if (key.Length == 0 || name.IndexOf(key, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (float.TryParse(part.Substring(eq + 1).Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float s))
                    _typeScale = Mathf.Clamp(s, 0f, 10f);
                break;
            }
            return _typeScale;
        }

        private void OnHit(DamageInfo info, GameObject target)
        {
            if (target == null || !_ready || (target != gameObject && !target.transform.IsChildOf(transform))) return;
            var p = DM_ClimbDashProfile.Live;
            if (p == null || !p.bodyWeightEnabled || !p.bodyWeightEnemyEnabled || _w <= 0f) return;
            Vector3 push = info.Source != null ? transform.position - info.Source.transform.position : -transform.forward;
            push.y = 0f;
            if (push.sqrMagnitude < 1e-6f || float.IsNaN(push.x)) push = -transform.forward;
            Vector3 d = transform.InverseTransformDirection(push.normalized);
            float s = Mathf.Clamp((Mathf.Max(0f, info.Amount) + Mathf.Max(0f, info.PoiseDamage) * 0.5f) / Mathf.Max(1f, p.bodyWeightEnemyHitDamageRef), 0.35f, 2f);
            _hitVel += new Vector3(d.z, 0f, -d.x) * (p.bodyWeightEnemyHitImpulse * s * Mathf.Sqrt(140f) * 1.4f);
        }

        private bool HardBlocked()
        {
            if (_anim == null || !_anim.isActiveAndEnabled) return true;
            if (_health != null && _health.IsDead) return true;
            if (DMEnemyKnockdown.IsDown(gameObject)) return true;
            if (_bridge != null && (_bridge.HasActiveRagdoll || _bridge.IsHitStaggerActive)) return true;
            return false;
        }

        private void LateUpdate()
        {
            var p = DM_ClimbDashProfile.Live;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (!_ready || p == null || !p.bodyWeightEnabled || !p.bodyWeightEnemyEnabled || dt <= 0f || HardBlocked())
            {
                if (_has || _w > 0f) Clear(); // no writes at all: the animator / ragdoll / knockdown own the pose
                return;
            }
            float S = p.BodyWeightScale * TypeScale(p);
            Camera cam = Camera.main;
            bool culled = S <= 0f
                || (cam != null && p.bodyWeightEnemyCullDistance > 0f && (cam.transform.position - transform.position).sqrMagnitude > p.bodyWeightEnemyCullDistance * p.bodyWeightEnemyCullDistance)
                || (_renderer != null && !_renderer.isVisible);
            bool soft = (_flinch != null && _flinch.IsFlinching) || _anim.speed < 0.05f;
            if (culled && _w <= 0f) { _has = false; _hasPos = false; return; }
            _w = Mathf.MoveTowards(_w, culled || soft ? 0f : 1f, dt / 0.2f);

            // Base pose: the animator's fresh pose; reuse the cached one if the bone was not re-posed (no stacking).
            Quaternion sc = _spine.localRotation, cc = _chest.localRotation;
            if (!_has || Quaternion.Angle(sc, _spineW) > 0.01f) _spineBase = sc;
            if (!_has || Quaternion.Angle(cc, _chestW) > 0.01f) _chestBase = cc;
            _has = true;

            Vector3 pos = transform.position;
            if (!_hasPos) { _lastPos = pos; _vel = Vector3.zero; _hasPos = true; }
            Vector3 v = (pos - _lastPos) / dt; v.y = 0f; _lastPos = pos;
            if (v.sqrMagnitude > 400f) v = _vel; // teleport / warp: ignore
            Vector3 rawAcc = Vector3.ClampMagnitude((v - _vel) / dt, 30f); _vel = v;
            _acc = Vector3.SmoothDamp(_acc, rawAcc, ref _accVel, 0.1f, Mathf.Infinity, dt);
            Vector3 la = transform.InverseTransformDirection(_acc);
            float max = p.bodyWeightEnemyMaxDegrees;
            float pitch = la.z >= 0f ? la.z * p.bodyWeightEnemyLean : -la.z * p.bodyWeightEnemyStopCarry;
            Vector3 target = new Vector3(Mathf.Clamp(pitch, -max, max), 0f, Mathf.Clamp(-la.x * p.bodyWeightEnemyLean * 0.6f, -max, max));

            bool attacking = _combat != null && _combat.IsAttacking;
            float tk = 1f / Mathf.Max(0.02f, p.bodyWeightEnemySettleSmooth); tk = tk * tk * 4f;
            if (attacking != _wasAttacking) // windup back on start, follow-through forward on release
                _trunkVel.x += (attacking ? -0.5f : 1f) * p.bodyWeightEnemySwing * Mathf.Sqrt(tk) * 1.4f;
            _wasAttacking = attacking;
            Step(ref _trunk, ref _trunkVel, target, tk, p.bodyWeightEnemySettleDamping, dt);
            Step(ref _hit, ref _hitVel, Vector3.zero, 140f, 0.5f, dt);

            Vector3 e = (_trunk + _hit) * (S * _w);
            if (float.IsNaN(e.x) || float.IsNaN(e.y) || float.IsNaN(e.z) || e.sqrMagnitude > 170f * 170f) { Clear(); return; }
            _spineW = _spineBase * Quaternion.Euler(CharToLocal(_spine, e * 0.4f));
            _chestW = _chestBase * Quaternion.Euler(CharToLocal(_chest, e * 0.6f));
            _spine.localRotation = _spineW;
            _chest.localRotation = _chestW;
        }

        private Vector3 CharToLocal(Transform t, Vector3 e) =>
            t.InverseTransformDirection(transform.right) * e.x + t.InverseTransformDirection(transform.up) * e.y + t.InverseTransformDirection(transform.forward) * e.z;

        private static void Step(ref Vector3 x, ref Vector3 v, Vector3 target, float k, float zeta, float dt)
        {
            k = Mathf.Max(1f, k);
            float c = Mathf.Max(0f, zeta) * 2f * Mathf.Sqrt(k);
            int n = Mathf.Clamp(Mathf.CeilToInt(dt * Mathf.Sqrt(k) / 0.25f), 1, 8);
            float h = dt / n;
            for (int i = 0; i < n; i++) { v += (k * (target - x) - c * v) * h; x += v * h; }
            if (float.IsNaN(x.x) || float.IsNaN(v.x) || x.sqrMagnitude > 170f * 170f) { x = Vector3.zero; v = Vector3.zero; }
        }
    }
}

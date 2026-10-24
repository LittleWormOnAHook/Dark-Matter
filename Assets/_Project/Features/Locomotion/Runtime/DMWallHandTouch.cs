using Invector.vCharacterController;
using Project.Data;
using Project.Features.Climb;
using Project.Features.Jetpack;
using Project.Inventory;
using UnityEngine;

namespace Project.Features.Locomotion
{
    /// <summary>
    /// 2026-10-24o: wall hand touch. While walking/jogging (not sprinting) beside a flat, near-vertical wall, the FREE wall-side
    /// hand reaches out and slides palm-flat along it. Auto-added at runtime (prefab untouched). Runs after aim IK and
    /// DMPlayerBodyWeight (3320); restores its own write before body weight's PreIK so nothing accumulates (24h jitter).
    /// Hands: drawn two-handed weapon (ItemData.weaponGrip) blocks both, drawn one-handed blocks the right; holstered = free.
    /// Settings: DM_ClimbDashProfile "bodyWeightWallHand*" (Studio Player/body section).
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(3340)]
    public sealed class DMWallHandTouch : MonoBehaviour
    {
        private struct Arm
        {
            public bool Left;
            public Transform Shoulder, Upper, Lower, Hand, Finger, Thumb;
            public Vector3 FingerL, PalmL;
            public float W, WVel;
            public bool HasTarget;
            public Vector3 Target, TargetVel, Normal, Tangent;
            public Quaternion OU, OL, OH, WU, WL, WH; // original / written local rotations
            public bool Wrote;
        }

        private Animator _anim;
        private vThirdPersonMotor _motor;
        private vShooterMeleeInput _shooter;
        private EquipmentController _equip;
        private DMClimbController _climb;
        private DMJetpackController _jet;
        private Arm _l, _r;
        private bool _ready;
        private readonly RaycastHit[] _hits = new RaycastHit[8];
        // 24ab: wall standoff (body kept off the wall while a hand touches it).
        private Rigidbody _rb;
        private CapsuleCollider _cap;
        private float _so;              // blended standoff weight 0..1
        private bool _soHas;
        private Vector3 _soPoint, _soNormal; // wall plane of the active touch

        public static void Ensure(GameObject player)
        {
            if (player == null || player.GetComponent<DMWallHandTouch>() != null) return;
            var a = player.GetComponentInChildren<Animator>();
            if (a == null || !a.isHuman) return;
            player.AddComponent<DMWallHandTouch>();
        }

        private void Awake()
        {
            _anim = GetComponentInChildren<Animator>();
            _motor = GetComponent<vThirdPersonMotor>();
            _shooter = GetComponent<vShooterMeleeInput>();
            _equip = GetComponent<EquipmentController>();
            _climb = GetComponent<DMClimbController>();
            _jet = GetComponent<DMJetpackController>();
            _rb = GetComponent<Rigidbody>();
            _cap = GetComponent<CapsuleCollider>();
            gameObject.AddComponent<PreRestore>().Owner = this;
        }

        [DefaultExecutionOrder(-600)]
        private sealed class PreRestore : MonoBehaviour
        {
            public DMWallHandTouch Owner;
            private void LateUpdate() { if (Owner != null) { Owner.Restore(ref Owner._l); Owner.Restore(ref Owner._r); } }
        }

        // If the animator did not re-pose the arm this frame, put back the pre-IK pose so body weight / aim IK see a clean pose.
        private void Restore(ref Arm a)
        {
            if (!a.Wrote || a.Upper == null) return;
            a.Wrote = false;
            if (Quaternion.Angle(a.Upper.localRotation, a.WU) < 0.01f) a.Upper.localRotation = a.OU;
            if (Quaternion.Angle(a.Lower.localRotation, a.WL) < 0.01f) a.Lower.localRotation = a.OL;
            if (Quaternion.Angle(a.Hand.localRotation, a.WH) < 0.01f) a.Hand.localRotation = a.OH;
        }

        private void Cache()
        {
            if (_anim == null || !_anim.isHuman) return;
            _l = MakeArm(true); _r = MakeArm(false);
            _ready = _l.Hand != null && _r.Hand != null;
        }

        private Arm MakeArm(bool left)
        {
            var a = new Arm { Left = left };
            a.Shoulder = _anim.GetBoneTransform(left ? HumanBodyBones.LeftShoulder : HumanBodyBones.RightShoulder);
            a.Upper = _anim.GetBoneTransform(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
            a.Lower = _anim.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
            a.Hand = _anim.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
            a.Finger = _anim.GetBoneTransform(left ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
            a.Thumb = _anim.GetBoneTransform(left ? HumanBodyBones.LeftThumbProximal : HumanBodyBones.RightThumbProximal);
            if (a.Upper == null || a.Lower == null || a.Hand == null) { a.Hand = null; return a; }
            // Palm frame in hand-local space from the skeleton (Kade's bone axes are 90 deg off; never assume axes).
            Vector3 f = a.Finger != null ? a.Finger.position - a.Hand.position : a.Hand.position - a.Lower.position;
            Vector3 t = a.Thumb != null ? a.Thumb.position - a.Hand.position : transform.forward;
            Vector3 palmW = left ? Vector3.Cross(t, f) : Vector3.Cross(f, t);
            if (palmW.sqrMagnitude < 1e-6f) palmW = -transform.up;
            a.FingerL = a.Hand.InverseTransformDirection(f.normalized);
            a.PalmL = a.Hand.InverseTransformDirection(palmW.normalized);
            return a;
        }

        private void LateUpdate()
        {
            var p = DM_ClimbDashProfile.Live;
            if (!_ready) { Cache(); if (!_ready) return; }
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            bool on = p != null && p.bodyWeightWallHandEnabled;
            bool gate = on && StateOpen(p);
            bool leftFree = true, rightFree = true;
            if (_equip != null && _equip.IsWeaponDrawn)
            {
                ItemData w = _equip.DrawnWeaponItem;
                if (w != null) { rightFree = false; if (w.weaponGrip == WeaponGrip.TwoHanded) leftFree = false; }
            }
            Tick(ref _l, p, gate && leftFree && p.bodyWeightWallHandLeft, dt);
            Tick(ref _r, p, gate && rightFree && p.bodyWeightWallHandRight, dt);
            // 24ab: standoff weight follows the strongest active touch, blended by its own speed.
            ref Arm s = ref (_l.W >= _r.W ? ref _l : ref _r);
            bool want = s.HasTarget && s.W > 0.01f && p != null && p.bodyWeightWallHandStandoff > 0f;
            if (want) { _soHas = true; _soPoint = s.Target - s.Normal * 0.02f; _soNormal = Vector3.ProjectOnPlane(s.Normal, Vector3.up).normalized; }
            float spd = p != null ? p.bodyWeightWallHandStandoffBlendSpeed : 5f;
            _so = Mathf.MoveTowards(_so, want ? Mathf.SmoothStep(0f, 1f, s.W) : 0f, dt * spd);
            if (_so <= 0f) _soHas = false;
        }

        // 24ab: gentle movement-level push (runs after Invector's FixedUpdate locomotion, before the physics step):
        // only the velocity component along the wall normal is raised, never a position snap.
        private void FixedUpdate()
        {
            var p = DM_ClimbDashProfile.Live;
            if (!_soHas || _so <= 0f || p == null || _rb == null || _rb.isKinematic || _soNormal.sqrMagnitude < 0.5f) return;
            float radius = _cap != null ? _cap.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) : 0.3f;
            Vector3 c = _cap != null ? transform.TransformPoint(_cap.center) : _rb.position;
            float dist = Vector3.Dot(c - _soPoint, _soNormal);
            float want = radius + p.bodyWeightWallHandStandoff;
            float shortfall = want - dist;
            if (shortfall <= 0f || dist < -0.1f) return; // clear already (or wrong side of the plane: ignore)
            float push = Mathf.Min(shortfall * p.bodyWeightWallHandStandoffBlendSpeed, 1.2f) * _so;
            Vector3 v = _rb.linearVelocity;
            float vn = Vector3.Dot(v, _soNormal);
            if (vn < push) _rb.linearVelocity = v + _soNormal * (push - vn);
            if (p.bodyWeightWallHandDebug) Debug.DrawRay(c, _soNormal * shortfall, Color.yellow);
        }

        private bool StateOpen(DM_ClimbDashProfile p)
        {
            if (_motor == null || _anim == null || !_anim.enabled) return false;
            if (_motor.isDead || _motor.ragdolled || _motor.customAction || !_motor.isGrounded) return false; // customAction = interact/carry/actions
            if (_motor.isSprinting || _motor.isRolling || _motor.isJumping) return false;
            if (new Vector3(_motor.input.x, 0f, _motor.input.z).sqrMagnitude < 0.01f) return false; // walking/jogging only
            if (_shooter != null && (_shooter.IsAiming || _shooter.isAttacking || _shooter.isReloading)) return false;
            if (_climb != null && _climb.IsClimbing) return false;
            if (_jet != null && (_jet.IsJetpackAnimActive || _jet.IsBoostingNow)) return false;
            return true;
        }

        private void Tick(ref Arm a, DM_ClimbDashProfile p, bool allowed, float dt)
        {
            Vector3 tgt = Vector3.zero, n = Vector3.zero, tan = Vector3.zero;
            bool found = allowed && Detect(ref a, p, out tgt, out n, out tan);
            if (found)
            {
                if (!a.HasTarget) { a.Target = tgt; a.TargetVel = Vector3.zero; }
                a.Target = Vector3.SmoothDamp(a.Target, tgt, ref a.TargetVel, 0.06f, Mathf.Infinity, dt);
                a.Normal = a.HasTarget ? Vector3.Slerp(a.Normal, n, Mathf.Clamp01(dt * 15f)) : n;
                a.Tangent = tan;
                a.HasTarget = true;
            }
            float blend = p != null ? Mathf.Max(0.01f, p.bodyWeightWallHandBlendTime) : 0.25f;
            a.W = Mathf.MoveTowards(a.W, found ? 1f : 0f, dt / blend);
            if (a.W <= 0.0001f) { a.HasTarget = false; return; }
            float w = Mathf.SmoothStep(0f, 1f, a.W);
            Solve(ref a, w, p);
            if (p != null && p.bodyWeightWallHandDebug) Debug.DrawRay(a.Target, a.Normal * 0.15f, a.Left ? Color.cyan : Color.magenta);
        }

        private bool Detect(ref Arm a, DM_ClimbDashProfile p, out Vector3 target, out Vector3 normal, out Vector3 tangent)
        {
            target = normal = tangent = Vector3.zero;
            Vector3 up = Vector3.up;
            Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;
            Vector3 side = Vector3.ProjectOnPlane(transform.right, up).normalized * (a.Left ? -1f : 1f);
            Vector3 sh = (a.Shoulder != null ? a.Shoulder : a.Upper).position;
            float reach = p.bodyWeightWallHandReach;
            float half = p.bodyWeightWallHandMinWidth * 0.5f;
            int mask = p.bodyWeightWallHandLayers.value; string[] tags = p.bodyWeightWallHandTags;
            float maxTiltSin = Mathf.Sin(p.bodyWeightWallHandMaxTiltDeg * Mathf.Deg2Rad);
            float facingCos = Mathf.Cos(p.bodyWeightWallHandMaxAngleDeg * Mathf.Deg2Rad);
            Vector3 sumP = Vector3.zero, sumN = Vector3.zero, n0 = Vector3.zero;
            // 5 rays: center, +-half along forward, +-0.15 m vertical. All must hit and agree (flatness).
            Vector3[] offs = { Vector3.zero, fwd * half, -fwd * half, up * 0.15f, -up * 0.15f };
            for (int i = 0; i < offs.Length; i++)
            {
                Vector3 o = sh + offs[i] - side * 0.05f;
                if (!Cast(o, side, reach + 0.05f, mask, tags, out RaycastHit h)) { Dbg(p, o, side * reach, Color.red); return false; }
                Dbg(p, o, h.point - o, Color.green);
                if (Mathf.Abs(h.normal.y) > maxTiltSin) return false;            // near-vertical
                if (Vector3.Dot(-h.normal, side) < facingCos) return false;      // wall angle range vs side axis
                if (i == 0) n0 = h.normal;
                else if (Vector3.Angle(n0, h.normal) > p.bodyWeightWallHandFlatnessDeg) return false; // flat
                if (i > 0 && i < 3 && Mathf.Abs(Vector3.Dot(h.point - sumP / Mathf.Max(1, i), n0)) > 0.06f) return false; // no steps/corners
                sumP += h.point; sumN += h.normal;
            }
            normal = sumN.normalized;
            tangent = Vector3.ProjectOnPlane(fwd, normal).normalized;
            Vector3 c = sumP / offs.Length;
            target = c + tangent * p.bodyWeightWallHandForwardOffset + up * p.bodyWeightWallHandHeight + normal * 0.02f;
            // Recheck the shifted target is on the wall (wall may end ahead).
            if (!Cast(target + normal * 0.15f, -normal, 0.3f, mask, tags, out RaycastHit th) || Vector3.Angle(th.normal, normal) > p.bodyWeightWallHandFlatnessDeg) return false;
            target = th.point + th.normal * 0.02f;
            // Reachable from the shoulder?
            float len = Vector3.Distance(a.Upper.position, a.Lower.position) + Vector3.Distance(a.Lower.position, a.Hand.position);
            if (Vector3.Distance(a.Upper.position, target) > len * 1.02f) return false;
            return true;
        }

        // 2026-10-24q: never-wall layers when casting broadly for tag matches.
        private static int s_excl = -1;

        private static bool HasTag(Collider c, string[] tags)
        {
            string a = c.tag, b = c.transform.root.tag;
            for (int i = 0; i < tags.Length; i++)
            {
                string t = tags[i];
                if (string.IsNullOrEmpty(t)) continue; // string compare: nonexistent tags never match (no CompareTag error)
                if (a == t || b == t) return true;
            }
            return false;
        }

        private static bool AnyTag(string[] tags) { if (tags != null) foreach (var t in tags) if (!string.IsNullOrEmpty(t)) return true; return false; }

        private bool Cast(Vector3 o, Vector3 d, float dist, int mask, string[] tags, out RaycastHit best)
        {
            best = default;
            if (s_excl < 0) s_excl = LayerMask.GetMask("Ignore Raycast", "UI", "Player", "Enemy", "CompanionAI", "Triggers", "BodyPart", "DMHitbox", "HeadTrack", "PW_VFX", "PostProcess", "DistantSky");
            bool useTags = AnyTag(tags);
            int castMask = useTags ? ((Physics.DefaultRaycastLayers & ~s_excl) | mask) : mask;
            int n = Physics.RaycastNonAlloc(o, d, _hits, dist, castMask, QueryTriggerInteraction.Ignore);
            float bd = float.MaxValue; bool ok = false;
            for (int i = 0; i < n; i++)
            {
                var h = _hits[i];
                if (h.collider.transform.IsChildOf(transform)) continue;
                if ((mask & (1 << h.collider.gameObject.layer)) == 0 && !(useTags && HasTag(h.collider, tags))) continue; // layer OR tag
                var rb = h.collider.attachedRigidbody;
                if (rb != null && !rb.isKinematic) continue; // never touch dynamic props / characters
                if (h.distance < bd) { bd = h.distance; best = h; ok = true; }
            }
            return ok;
        }

        private static void Dbg(DM_ClimbDashProfile p, Vector3 o, Vector3 d, Color c) { if (p.bodyWeightWallHandDebug) Debug.DrawRay(o, d, c); }

        // 24ab: forearm/upper-arm spherecast; true if the wall is hit well before the hand target.
        private bool ArmBlocked(Vector3 s, Vector3 e, Vector3 h, DM_ClimbDashProfile p)
        {
            if (p == null) return false;
            Vector3 d = h - e; float len = d.magnitude;
            if (len > 0.08f && Physics.SphereCast(e, 0.04f, d / len, out RaycastHit hit, len - 0.08f, p.bodyWeightWallHandLayers.value, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(transform)) return true;
            d = e - s; len = d.magnitude;
            if (len > 0.02f && Physics.SphereCast(s, 0.04f, d / len, out hit, len, p.bodyWeightWallHandLayers.value, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(transform)) return true;
            return false;
        }

        private void Solve(ref Arm a, float w, DM_ClimbDashProfile p)
        {
            a.OU = a.Upper.localRotation; a.OL = a.Lower.localRotation; a.OH = a.Hand.localRotation;
            Quaternion u0 = a.Upper.rotation, h0 = a.Hand.rotation;
            Vector3 S = a.Upper.position, E = a.Lower.position, H = a.Hand.position;
            float la = Vector3.Distance(S, E), lb = Vector3.Distance(E, H);
            Vector3 T = a.Target;
            Vector3 toT = T - S;
            float c = Mathf.Clamp(toT.magnitude, Mathf.Abs(la - lb) + 0.001f, la + lb - 0.001f);
            Vector3 td = toT.normalized;
            Vector3 side = transform.right * (a.Left ? -1f : 1f);
            float away = p != null ? p.bodyWeightWallHandElbowAway : 0.6f;
            Vector3 wallN = a.Normal.sqrMagnitude > 0.5f ? a.Normal : -side;
            Vector3 wallP = T - wallN * 0.02f;
            float cosA = Mathf.Clamp((la * la + c * c - lb * lb) / (2f * la * c), -1f, 1f);
            Vector3 elbowDir = td;
            // 24ab: elbow bent out AWAY from the wall (+ down); push harder if the elbow / forearm would end up in the wall.
            for (int it = 0; it < 4; it++)
            {
                Vector3 hint = (side * 0.7f - transform.up * 0.6f - transform.forward * 0.2f + wallN * (away + it * 0.6f)).normalized;
                hint = Vector3.ProjectOnPlane(hint, td);
                if (hint.sqrMagnitude < 1e-6f) hint = Vector3.ProjectOnPlane(-transform.up, td);
                Vector3 n = Vector3.Cross(td, hint.normalized);
                if (n.sqrMagnitude < 1e-6f) n = Vector3.Cross(td, -transform.up);
                n.Normalize();
                // rotate td toward the hint side (sign so the elbow lands on the hint side)
                elbowDir = Quaternion.AngleAxis(-Mathf.Acos(cosA) * Mathf.Rad2Deg, n) * td;
                if (Vector3.Dot(elbowDir, hint) < 0f) elbowDir = Quaternion.AngleAxis(Mathf.Acos(cosA) * Mathf.Rad2Deg, n) * td;
                Vector3 Ew = S + elbowDir * la;
                if (Vector3.Dot(Ew - wallP, wallN) >= 0.06f && !ArmBlocked(S, Ew, (S + td * c), p)) break;
            }
            a.Upper.rotation = Quaternion.FromToRotation(E - S, elbowDir) * u0;
            Vector3 E2 = a.Lower.position, H2 = a.Hand.position;
            a.Lower.rotation = Quaternion.FromToRotation(H2 - E2, (S + td * c) - E2) * a.Lower.rotation;
            // Palm flat to the wall, fingers up-and-forward along it (from wall normal in character frame).
            Vector3 palmW = -a.Normal;
            Vector3 fingW = Vector3.ProjectOnPlane(Vector3.up * 0.8f + a.Tangent * 0.6f, a.Normal).normalized;
            Quaternion handW = Quaternion.LookRotation(fingW, palmW) * Quaternion.Inverse(Quaternion.LookRotation(a.FingerL, a.PalmL));
            Quaternion uL = a.Upper.localRotation, lL = a.Lower.localRotation;
            a.Upper.localRotation = Quaternion.Slerp(a.OU, uL, w);
            a.Lower.localRotation = Quaternion.Slerp(a.OL, lL, w);
            a.Hand.rotation = Quaternion.Slerp(h0, handW, w);
            a.WU = a.Upper.localRotation; a.WL = a.Lower.localRotation; a.WH = a.Hand.localRotation; a.Wrote = true;
        }
    }
}

using Project.Combat;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Procedural upper-body hit lean (spine / chest / head), applied in LateUpdate after the Animator posed the skeleton,
    /// so the arms and held weapon keep each enemy's own grip relative to the torso. Animator layers, parameters, speed and
    /// physics are never touched and the root never moves.
    /// Slice 2b: <see cref="PlayStrike"/> = weighted spring lean along the strike direction: fast attack to the peak, optional
    /// hold through the hit-stop freeze, then a damped spring settle with overshoot; the head starts late and swings further
    /// (whip). A new hit while one is still settling cross-fades from it, so stacked hits never pop.
    /// The legacy <see cref="Play(Vector3, float, float, float)"/> keeps the Slice 2 shape (smooth in / out, no overshoot).
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class DMEnemyHitFlinch : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float spineShare = 0.4f;
        [SerializeField, Range(0f, 1f)] private float chestShare = 0.4f;
        [SerializeField, Range(0f, 1f)] private float headShare = 0.2f;
        [Tooltip("Seconds to reach full lean; the rest of the duration is a smooth blend out (legacy Play only).")]
        [SerializeField, Range(0.02f, 0.15f)] private float attackSeconds = 0.05f;
        [Tooltip("Small random twist so repeated flinches don't look identical (legacy Play only).")]
        [SerializeField, Range(0f, 8f)] private float maxTwistDegrees = 2.5f;

        /// <summary>One weighted strike lean. Directions are world space; only the horizontal part of LeanDirection is used.</summary>
        public struct Strike
        {
            /// <summary>Where the top of the torso goes (the strike direction).</summary>
            public Vector3 LeanDirection;
            public float Degrees;
            /// <summary>Signed: + turns toward the enemy's right.</summary>
            public float TwistDegrees;
            public float AttackSeconds;
            public float SettleSeconds;
            /// <summary>Settle spring frequency (Hz). 0 = smooth fade, no overshoot.</summary>
            public float SpringFrequency;
            public float SpringDamping;
            public float SpineWeight;
            public float ChestWeight;
            public float HeadWeight;
            public float HeadDelaySeconds;
            public float HeadWhipMultiplier;
            public bool HoldPeakThroughHitstop;
        }

        private const int BoneCount = 3;
        private const int HeadIndex = 2;

        private struct Instance
        {
            public bool Active;
            public float Elapsed;
            public Vector3 Axis;
            public Strike S;
            public float Fade;      // carry-over multiplier (1 for the current hit)
            public float FadeRate;  // per second, carry-over only
        }

        private Animator _animator;
        private EnemyInvectorRagdollBridge _ragdollBridge;
        private readonly Transform[] _bones = new Transform[BoneCount];
        private readonly float[] _shares = new float[BoneCount];
        private readonly Quaternion[] _preLocal = new Quaternion[BoneCount];
        private readonly Quaternion[] _postLocal = new Quaternion[BoneCount];
        private bool _bonesResolved;
        private bool _offsetApplied;
        private Instance _cur;
        private Instance _prev;

        public bool IsFlinching => _cur.Active || _prev.Active;

        /// <summary>Slice 2b: last per-hit direction jitter on this enemy (kept apart from the next one).</summary>
        public float LastJitterDegrees { get; set; }

        /// <summary>Starts (or restarts) a flinch leaning away from <paramref name="hitFromWorld"/>.</summary>
        public void Play(Vector3 hitFromWorld, float degrees, float duration)
        {
            Play(hitFromWorld, degrees, duration, float.NaN);
        }

        /// <summary>Slice 2 flinch with a fixed torso twist (degrees, signed: + turns toward the enemy's right). NaN = the old small random twist.</summary>
        public void Play(Vector3 hitFromWorld, float degrees, float duration, float twistDegrees)
        {
            if (degrees <= 0f || duration <= 0f)
                return;

            Vector3 away = transform.position - hitFromWorld;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
                away = -transform.forward;

            float attack = attackSeconds;
            PlayStrike(new Strike
            {
                LeanDirection = away,
                Degrees = degrees,
                TwistDegrees = float.IsNaN(twistDegrees) ? Random.Range(-maxTwistDegrees, maxTwistDegrees) : twistDegrees,
                AttackSeconds = attack,
                SettleSeconds = Mathf.Max(duration - attack, 0.05f),
                SpringFrequency = 0f,
                SpringDamping = 1f,
                SpineWeight = spineShare,
                ChestWeight = chestShare,
                HeadWeight = headShare,
                HeadDelaySeconds = 0f,
                HeadWhipMultiplier = 1f,
                HoldPeakThroughHitstop = false
            });
        }

        /// <summary>Slice 2b weighted strike lean (see class summary).</summary>
        public void PlayStrike(Strike strike)
        {
            if (!ResolveBones() || strike.Degrees <= 0f && Mathf.Abs(strike.TwistDegrees) <= 0.01f)
                return;

            Vector3 dir = strike.LeanDirection;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                dir = -transform.forward;

            strike.AttackSeconds = Mathf.Max(0.01f, strike.AttackSeconds);
            strike.SettleSeconds = Mathf.Max(0.05f, strike.SettleSeconds);

            // The still-settling hit becomes the carry-over and fades out while the new one rises.
            if (_cur.Active)
            {
                _prev = _cur;
                _prev.FadeRate = 1f / strike.AttackSeconds;
            }

            _cur = new Instance
            {
                Active = true,
                Elapsed = 0f,
                // Rotating about up x dir tips the top of the torso toward dir.
                Axis = Vector3.Cross(Vector3.up, dir.normalized),
                S = strike,
                Fade = 1f,
                FadeRate = 0f
            };
        }

        public void Cancel()
        {
            UndoStaleOffset();
            _cur.Active = false;
            _prev.Active = false;
        }

        private void OnDisable()
        {
            Cancel();
        }

        private void LateUpdate()
        {
            // If the Animator did not re-pose the bones since last frame (AnimatePhysics without a
            // fixed step, culling), remove last frame's offset first so it can never accumulate.
            UndoStaleOffset();

            if (!_cur.Active && !_prev.Active)
                return;

            if (!CanApply())
            {
                _cur.Active = false;
                _prev.Active = false;
                return;
            }

            float dt = Time.deltaTime;
            bool hitstopHeld = DMMeleeHitStop.IsEnemyAnimatorHeld(_animator);
            Advance(ref _cur, dt, hitstopHeld);
            Advance(ref _prev, dt, hitstopHeld);
            if (_prev.Active)
            {
                _prev.Fade -= _prev.FadeRate * dt;
                if (_prev.Fade <= 0f)
                    _prev.Active = false;
            }

            if (!_cur.Active && !_prev.Active)
                return;

            for (int i = 0; i < BoneCount; i++)
            {
                if (_bones[i] != null)
                    _preLocal[i] = _bones[i].localRotation;
            }

            bool any = false;
            for (int i = 0; i < BoneCount; i++)
            {
                Transform bone = _bones[i];
                if (bone == null)
                    continue;

                Quaternion offset = Quaternion.identity;
                if (Contribution(ref _cur, i, out Quaternion a))
                    offset = a;
                if (Contribution(ref _prev, i, out Quaternion b))
                    offset = b * offset;

                if (offset != Quaternion.identity)
                {
                    bone.rotation = offset * bone.rotation;
                    any = true;
                }
            }

            if (!any)
                return;

            for (int i = 0; i < BoneCount; i++)
            {
                if (_bones[i] != null)
                    _postLocal[i] = _bones[i].localRotation;
            }

            _offsetApplied = true;
        }

        private static void Advance(ref Instance inst, float dt, bool hitstopHeld)
        {
            if (!inst.Active)
                return;

            // Hit-stop punch: rise during the freeze, then hold the peak until it ends.
            bool atPeak = inst.Elapsed >= inst.S.AttackSeconds + inst.S.HeadDelaySeconds;
            if (!(hitstopHeld && inst.S.HoldPeakThroughHitstop && atPeak))
                inst.Elapsed += dt;

            if (inst.Elapsed >= inst.S.HeadDelaySeconds + inst.S.AttackSeconds + inst.S.SettleSeconds)
                inst.Active = false;
        }

        private bool Contribution(ref Instance inst, int boneIndex, out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            if (!inst.Active)
                return false;

            float share = boneIndex == 0 ? inst.S.SpineWeight : boneIndex == 1 ? inst.S.ChestWeight : inst.S.HeadWeight;
            share *= _shares[boneIndex] > 0f ? 1f : 0f;
            if (boneIndex == 0 && _bones[1] == null)
                share += inst.S.ChestWeight;
            if (boneIndex == 1 && _bones[0] == null)
                share += inst.S.SpineWeight;
            if (share <= 0f)
                return false;

            float delay = boneIndex == HeadIndex ? inst.S.HeadDelaySeconds : 0f;
            float env = Envelope(inst.Elapsed - delay, inst.S);
            if (boneIndex == HeadIndex)
                env *= inst.S.HeadWhipMultiplier;
            env *= inst.Fade;
            if (Mathf.Abs(env) <= 0.0001f)
                return false;

            float w = share * env;
            rotation = Quaternion.AngleAxis(inst.S.Degrees * w, inst.Axis) *
                       Quaternion.AngleAxis(inst.S.TwistDegrees * w, Vector3.up);
            return true;
        }

        /// <summary>0 -> 1 (ease-out) over the attack, then a damped spring (or smooth fade) to 0 over the settle.</summary>
        private static float Envelope(float t, Strike s)
        {
            if (t <= 0f)
                return 0f;

            if (t < s.AttackSeconds)
            {
                float k = 1f - t / s.AttackSeconds;
                return 1f - k * k * k;
            }

            float u = t - s.AttackSeconds;
            if (u >= s.SettleSeconds)
                return 0f;

            if (s.SpringFrequency <= 0.001f)
                return 1f - Mathf.SmoothStep(0f, 1f, u / s.SettleSeconds);

            float omega = 2f * Mathf.PI * s.SpringFrequency;
            float zeta = Mathf.Clamp(s.SpringDamping, 0.05f, 0.999f);
            float omegaD = omega * Mathf.Sqrt(1f - zeta * zeta);
            float spring = Mathf.Exp(-zeta * omega * u) * Mathf.Cos(omegaD * u);
            // Guarantee a clean zero at the end of the settle.
            float tail = Mathf.InverseLerp(s.SettleSeconds * 0.7f, s.SettleSeconds, u);
            return spring * (1f - Mathf.SmoothStep(0f, 1f, tail));
        }

        private void UndoStaleOffset()
        {
            if (!_offsetApplied)
                return;

            _offsetApplied = false;
            for (int i = 0; i < BoneCount; i++)
            {
                Transform bone = _bones[i];
                if (bone == null)
                    continue;

                if (Mathf.Abs(Quaternion.Dot(bone.localRotation, _postLocal[i])) > 0.999999f)
                    bone.localRotation = _preLocal[i];
            }
        }

        private bool CanApply()
        {
            if (_animator == null || !_animator.enabled || !_animator.isActiveAndEnabled)
                return false;

            if (_ragdollBridge == null)
                _ragdollBridge = GetComponent<EnemyInvectorRagdollBridge>();

            return _ragdollBridge == null || !_ragdollBridge.HasActiveRagdoll;
        }

        private bool ResolveBones()
        {
            if (_bonesResolved)
                return _bones[0] != null || _bones[1] != null || _bones[2] != null;

            _bonesResolved = true;
            _animator = GetComponent<Animator>();
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);
            if (_animator == null || !_animator.isHuman)
                return false;

            Transform spine = _animator.GetBoneTransform(HumanBodyBones.Spine);
            Transform chest = _animator.GetBoneTransform(HumanBodyBones.Chest);
            if (chest == null)
                chest = _animator.GetBoneTransform(HumanBodyBones.UpperChest);
            Transform head = _animator.GetBoneTransform(HumanBodyBones.Head);

            _bones[0] = spine;
            _bones[1] = chest;
            _bones[2] = head;
            _shares[0] = spine != null ? 1f : 0f;
            _shares[1] = chest != null ? 1f : 0f;
            _shares[2] = head != null ? 1f : 0f;
            return spine != null || chest != null || head != null;
        }
    }
}

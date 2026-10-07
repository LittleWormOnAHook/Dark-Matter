using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Short procedural upper-body flinch for light enemy hits (~0.2-0.35 s).
    /// Leans the spine / chest / head away from the hit in LateUpdate, after the Animator has posed the
    /// skeleton, so the arms and held weapon keep their guard pose relative to the torso. Animator layers,
    /// parameters, speed and physics are never touched; the offset is applied on top of the animated pose
    /// each frame and fades smoothly back to zero.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
    public sealed class DMEnemyHitFlinch : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float spineShare = 0.4f;
        [SerializeField, Range(0f, 1f)] private float chestShare = 0.4f;
        [SerializeField, Range(0f, 1f)] private float headShare = 0.2f;
        [Tooltip("Seconds to reach full lean; the rest of the duration is a smooth blend out.")]
        [SerializeField, Range(0.02f, 0.15f)] private float attackSeconds = 0.05f;
        [Tooltip("Small random twist so repeated flinches don't look identical.")]
        [SerializeField, Range(0f, 8f)] private float maxTwistDegrees = 2.5f;

        private const int BoneCount = 3;

        private Animator _animator;
        private EnemyInvectorRagdollBridge _ragdollBridge;
        private readonly Transform[] _bones = new Transform[BoneCount];
        private readonly float[] _shares = new float[BoneCount];
        private readonly Quaternion[] _preLocal = new Quaternion[BoneCount];
        private readonly Quaternion[] _postLocal = new Quaternion[BoneCount];
        private bool _bonesResolved;
        private bool _offsetApplied;
        private float _startTime = -1f;
        private float _duration;
        private float _degrees;
        private float _twistDegrees;
        private Vector3 _leanAxis;

        public bool IsFlinching => _startTime >= 0f && Time.time - _startTime < _duration;

        /// <summary>Starts (or restarts) a flinch leaning away from <paramref name="hitFromWorld"/>.</summary>
        public void Play(Vector3 hitFromWorld, float degrees, float duration)
        {
            if (!ResolveBones() || degrees <= 0f || duration <= 0f)
                return;

            Vector3 away = transform.position - hitFromWorld;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
                away = -transform.forward;

            // Rotating about up x away tips the top of the torso toward "away".
            _leanAxis = Vector3.Cross(Vector3.up, away.normalized);
            _degrees = degrees;
            _duration = Mathf.Max(duration, attackSeconds + 0.05f);
            _twistDegrees = Random.Range(-maxTwistDegrees, maxTwistDegrees);
            _startTime = Time.time;
        }

        public void Cancel()
        {
            UndoStaleOffset();
            _startTime = -1f;
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

            if (_startTime < 0f)
                return;

            float t = Time.time - _startTime;
            if (t >= _duration || !CanApply())
            {
                _startTime = -1f;
                return;
            }

            float weight = t < attackSeconds
                ? Mathf.SmoothStep(0f, 1f, t / attackSeconds)
                : 1f - Mathf.SmoothStep(0f, 1f, (t - attackSeconds) / (_duration - attackSeconds));
            if (weight <= 0.0001f)
                return;

            for (int i = 0; i < BoneCount; i++)
            {
                if (_bones[i] != null)
                    _preLocal[i] = _bones[i].localRotation;
            }

            for (int i = 0; i < BoneCount; i++)
            {
                Transform bone = _bones[i];
                if (bone == null || _shares[i] <= 0f)
                    continue;

                float share = _shares[i] * weight;
                Quaternion offset = Quaternion.AngleAxis(_degrees * share, _leanAxis) *
                                    Quaternion.AngleAxis(_twistDegrees * share, Vector3.up);
                bone.rotation = offset * bone.rotation;
            }

            for (int i = 0; i < BoneCount; i++)
            {
                if (_bones[i] != null)
                    _postLocal[i] = _bones[i].localRotation;
            }

            _offsetApplied = true;
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
            _shares[0] = spine != null ? spineShare + (chest == null ? chestShare : 0f) : 0f;
            _shares[1] = chest != null ? chestShare + (spine == null ? spineShare : 0f) : 0f;
            _shares[2] = head != null ? headShare : 0f;
            return spine != null || chest != null || head != null;
        }
    }
}

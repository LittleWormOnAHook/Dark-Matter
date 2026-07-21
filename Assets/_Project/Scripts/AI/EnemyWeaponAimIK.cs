using Invector.vShooter;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Post-animation bone rotation that aims the weapon muzzle (Z-axis) at the current target.
    /// Runs in LateUpdate after the Animator.
    ///
    /// Spine handles horizontal (yaw) tracking only — no forward tilt.
    /// Chest handles vertical (pitch) tracking, clamped to avoid unnatural bending.
    /// Upper arm adds a small additional nudge toward the target.
    /// </summary>
    [DefaultExecutionOrder(300)]
    [DisallowMultipleComponent]
    public class EnemyWeaponAimIK : MonoBehaviour
    {
        [Header("Bone Weights")]
        [Tooltip("How much the spine contributes to horizontal (yaw) tracking.")]
        [SerializeField] [Range(0f, 1f)] private float spineYawShare = 0.5f;

        [Tooltip("How much the chest contributes to horizontal (yaw) tracking.")]
        [SerializeField] [Range(0f, 1f)] private float chestYawShare = 0.5f;

        [Tooltip("How much the chest contributes to vertical (pitch) tracking.")]
        [SerializeField] [Range(0f, 1f)] private float chestPitchShare = 0.6f;

        [Tooltip("How much the upper arm contributes to vertical (pitch) tracking.")]
        [SerializeField] [Range(0f, 1f)] private float upperArmPitchShare = 0.4f;

        [Header("Limits")]
        [Tooltip("Max pitch angle (degrees up/down) the chest is allowed to tilt.")]
        [SerializeField] private float maxPitchDegrees = 40f;

        [Tooltip("How fast the aim blends in/out.")]
        [SerializeField] private float smoothSpeed = 8f;

        [Tooltip("Max degrees per second the bones rotate (prevents snapping).")]
        [SerializeField] private float maxDegreesPerSecond = 200f;

        private Animator _animator;
        private EnemyAiController _aiController;
        private vShooterWeapon _weapon;

        private Vector3 _aimTarget;
        private float _currentWeight;

        private Quaternion _spineSmooth  = Quaternion.identity;
        private Quaternion _chestSmooth  = Quaternion.identity;
        private Quaternion _armSmooth    = Quaternion.identity;
        private bool _initialized;

        // ── Public API ───────────────────────────────────────────────────────────

        public void SetAimTarget(Vector3 worldPosition) => _aimTarget = worldPosition;
        public void ClearAimTarget()                    => _aimTarget = Vector3.zero;

        // ── Unity ────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _animator    = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
            _aiController = GetComponent<EnemyAiController>();
        }

        private void Update()
        {
            bool shouldAim = _aiController != null
                && _aiController.IsInRangedEngagement
                && _aimTarget != Vector3.zero;

            float targetWeight = shouldAim ? 1f : 0f;
            _currentWeight = Mathf.MoveTowards(_currentWeight, targetWeight, smoothSpeed * Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (_animator == null || _currentWeight <= 0.001f || _aimTarget == Vector3.zero)
                return;

            // Lazy weapon lookup — refreshes when weapon is swapped.
            if (_weapon == null || _weapon.muzzle == null)
                _weapon = GetComponentInChildren<vShooterWeapon>(true);

            Transform spineBone    = _animator.GetBoneTransform(HumanBodyBones.Spine);
            Transform chestBone    = _animator.GetBoneTransform(HumanBodyBones.Chest);
            Transform upperArmBone = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);

            // Seed smooth rotations on first use so there is no initial snap.
            if (!_initialized)
            {
                _spineSmooth = spineBone  != null ? spineBone.rotation  : Quaternion.identity;
                _chestSmooth = chestBone  != null ? chestBone.rotation  : Quaternion.identity;
                _armSmooth   = upperArmBone != null ? upperArmBone.rotation : Quaternion.identity;
                _initialized = true;
            }

            // ── Determine the aiming reference direction ──────────────────────
            // Use the muzzle Z-axis (forward) when available; fall back to character forward.
            Vector3 aimOrigin   = _weapon?.muzzle != null ? _weapon.muzzle.position : transform.position + Vector3.up * 1.5f;
            Vector3 muzzleFwd   = _weapon?.muzzle != null ? _weapon.muzzle.forward  : transform.forward;
            Vector3 toTarget    = (_aimTarget - aimOrigin).normalized;

            if (toTarget.sqrMagnitude < 0.001f) return;

            // ── Horizontal yaw: spine + chest rotate left/right to face the target ──
            // Project both directions onto the horizontal plane so the spine never tilts forward.
            Vector3 muzzleH  = Vector3.ProjectOnPlane(muzzleFwd, Vector3.up);
            Vector3 targetH  = Vector3.ProjectOnPlane(toTarget,  Vector3.up);

            float yawAngle = 0f;
            if (muzzleH.sqrMagnitude > 0.001f && targetH.sqrMagnitude > 0.001f)
                yawAngle = Vector3.SignedAngle(muzzleH.normalized, targetH.normalized, Vector3.up);

            // Split yaw between spine and chest.
            if (spineBone != null)
            {
                Quaternion spineYaw = Quaternion.AngleAxis(yawAngle * spineYawShare * _currentWeight, Vector3.up);
                Quaternion desired  = spineYaw * spineBone.rotation;
                _spineSmooth = SmoothRotate(_spineSmooth, desired);
                spineBone.rotation = _spineSmooth;
            }

            if (chestBone != null)
            {
                Quaternion chestYaw = Quaternion.AngleAxis(yawAngle * chestYawShare * _currentWeight, Vector3.up);
                Quaternion desired  = chestYaw * chestBone.rotation;
                _chestSmooth = SmoothRotate(_chestSmooth, desired);
                chestBone.rotation = _chestSmooth;
            }

            // ── Vertical pitch: chest + upper arm tilt up/down ───────────────
            // Use the character's right axis so pitch follows the body orientation.
            Vector3 charRight = transform.right;
            Vector3 muzzleV   = Vector3.ProjectOnPlane(muzzleFwd, charRight);
            Vector3 targetV   = Vector3.ProjectOnPlane(toTarget,  charRight);

            float pitchAngle = 0f;
            if (muzzleV.sqrMagnitude > 0.001f && targetV.sqrMagnitude > 0.001f)
                pitchAngle = Vector3.SignedAngle(muzzleV.normalized, targetV.normalized, charRight);

            pitchAngle = Mathf.Clamp(pitchAngle, -maxPitchDegrees, maxPitchDegrees);

            if (chestBone != null)
            {
                Quaternion chestPitch = Quaternion.AngleAxis(pitchAngle * chestPitchShare * _currentWeight, charRight);
                _chestSmooth = SmoothRotate(_chestSmooth, chestPitch * chestBone.rotation);
                chestBone.rotation = _chestSmooth;
            }

            if (upperArmBone != null)
            {
                Quaternion armPitch = Quaternion.AngleAxis(pitchAngle * upperArmPitchShare * _currentWeight, charRight);
                Quaternion desired  = armPitch * upperArmBone.rotation;
                _armSmooth = SmoothRotate(_armSmooth, desired);
                upperArmBone.rotation = _armSmooth;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private Quaternion SmoothRotate(Quaternion current, Quaternion desired)
        {
            float maxStep = maxDegreesPerSecond * Time.deltaTime;
            float angle   = Quaternion.Angle(current, desired);
            if (angle <= 0.001f) return desired;
            return Quaternion.Slerp(current, desired, Mathf.Min(1f, maxStep / angle));
        }
    }
}

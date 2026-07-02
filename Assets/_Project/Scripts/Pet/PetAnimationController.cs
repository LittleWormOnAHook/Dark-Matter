using UnityEngine;

namespace Project.Pet
{
    /// <summary>
    /// Drives pet locomotion clips from PetController movement speed.
    /// Supports PetFoxController (Idle/Walk/Run) and Malbers fox states via serialized names.
    /// </summary>
    [RequireComponent(typeof(PetController))]
    [DefaultExecutionOrder(100)]
    public class PetAnimationController : MonoBehaviour
    {
        [SerializeField] private RuntimeAnimatorController petAnimatorController;
        [SerializeField] private string idleState = "Idle";
        [SerializeField] private string walkState = "Walk";
        [SerializeField] private string runState = "Run";
        [SerializeField] private float walkSpeedThreshold = 0.05f;
        [SerializeField] private float runSpeedThreshold = 2.5f;
        [SerializeField] private float runExitHysteresis = 0.35f;
        [SerializeField] private float crossFadeDuration = 0.1f;
        [SerializeField] private string motionRootBoneName = "CG";
        [SerializeField] private bool stripRootMotion = true;

        private PetController _pet;
        private Animator _animator;
        private Transform _motionRoot;
        private Vector3 _motionRootRestLocalPos;
        private bool _motionRootRestCaptured;
        private int _idleHash;
        private int _walkHash;
        private int _runHash;
        private int _currentStateHash = int.MinValue;
        private bool _hashesCached;

        private void Awake()
        {
            _pet = GetComponent<PetController>();
            ResolveAnimator();
            ResolveMotionRoot();
            ConfigureAnimator();
            ResolveStateNames();
            CacheStateHashes();
        }

        private void OnEnable()
        {
            ConfigureAnimator();
            ResolveMotionRoot();
            ResolveStateNames();
            CacheStateHashes();
            _currentStateHash = int.MinValue;
            _motionRootRestCaptured = false;
            CaptureMotionRootRest();
            PlayState(_idleHash);
        }

        private void Start()
        {
            ConfigureAnimator();
            CaptureMotionRootRest();
            PlayState(_idleHash);
        }

        private void ResolveAnimator()
        {
            if (_animator != null)
                return;

            _animator = GetComponent<Animator>();
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);
        }

        private void ResolveStateNames()
        {
            ResolveAnimator();
            if (_animator == null || _animator.runtimeAnimatorController == null)
                return;

            idleState = ResolveStateName(idleState, "Idle", "Rac_Idle01", "Rac_Stand Idle");
            walkState = ResolveStateName(walkState, "Walk", "Rac_Walk Forward");
            runState = ResolveStateName(runState, "Run", "Rac_Run Forward");
        }

        private string ResolveStateName(string primary, params string[] candidates)
        {
            if (_animator == null)
                return primary;

            foreach (string candidate in candidates)
            {
                if (string.IsNullOrEmpty(candidate))
                    continue;

                int hash = Animator.StringToHash(candidate);
                if (_animator.HasState(0, hash))
                    return candidate;
            }

            return primary;
        }

        private void CacheStateHashes()
        {
            _idleHash = Animator.StringToHash(idleState);
            _walkHash = Animator.StringToHash(walkState);
            _runHash = Animator.StringToHash(runState);
            _hashesCached = true;
        }

        private void ConfigureAnimator()
        {
            ResolveAnimator();
            if (_animator == null)
                return;

            if (petAnimatorController != null)
                _animator.runtimeAnimatorController = petAnimatorController;

            _animator.enabled = true;
            _animator.applyRootMotion = false;
            _animator.updateMode = AnimatorUpdateMode.Normal;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        private void Update()
        {
            if (_animator == null || _pet == null || !_pet.CompanionActive)
                return;

            if (!_hashesCached)
                CacheStateHashes();

            _animator.applyRootMotion = false;

            float speed = _pet.CurrentSpeed;
            int targetHash = _idleHash;

            bool inRun = _currentStateHash == _runHash;
            float runExitThreshold = Mathf.Max(walkSpeedThreshold, runSpeedThreshold - runExitHysteresis);

            if (speed >= runSpeedThreshold || (inRun && speed >= runExitThreshold))
                targetHash = _runHash;
            else if (speed >= walkSpeedThreshold)
                targetHash = _walkHash;

            PlayState(targetHash);
        }

        private void LateUpdate()
        {
            if (!stripRootMotion || _motionRoot == null || _pet == null || !_pet.CompanionActive)
                return;

            if (!_motionRootRestCaptured)
                CaptureMotionRootRest();

            Vector3 local = _motionRoot.localPosition;
            local.x = _motionRootRestLocalPos.x;
            local.z = _motionRootRestLocalPos.z;
            _motionRoot.localPosition = local;
        }

        private void ResolveMotionRoot()
        {
            if (_motionRoot != null)
                return;

            ResolveAnimator();
            if (_animator == null)
                return;

            _motionRoot = FindBone(_animator.transform, motionRootBoneName);
        }

        private static Transform FindBone(Transform root, string boneName)
        {
            if (root == null || string.IsNullOrEmpty(boneName))
                return null;

            if (root.name == boneName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindBone(root.GetChild(i), boneName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void CaptureMotionRootRest()
        {
            ResolveMotionRoot();
            if (_motionRoot == null || _motionRootRestCaptured)
                return;

            _motionRootRestLocalPos = _motionRoot.localPosition;
            _motionRootRestCaptured = true;
        }

        private void PlayState(int stateHash)
        {
            if (_animator == null || stateHash == _currentStateHash)
                return;

            if (_animator.runtimeAnimatorController == null)
                return;

            if (!_animator.isInitialized || !_animator.HasState(0, stateHash))
                return;

            _animator.CrossFadeInFixedTime(stateHash, crossFadeDuration, 0, 0f);
            _currentStateHash = stateHash;
        }
    }
}

using UnityEngine;

namespace Project.Pet
{
    /// <summary>
    /// Drives Malbers fox locomotion clips from PetController movement speed.
    /// Uses PetFoxController with Fox_Walk / Fox_Run clips from the Malbers fox pack.
    /// </summary>
    [RequireComponent(typeof(PetController))]
    [DefaultExecutionOrder(100)]
    public class PetAnimationController : MonoBehaviour
    {
        [SerializeField] private RuntimeAnimatorController petAnimatorController;
        [SerializeField] private float walkSpeedThreshold = 0.05f;
        [SerializeField] private float runSpeedThreshold = 2.5f;

        private static readonly int IdleHash = Animator.StringToHash("Idle");
        private static readonly int WalkHash = Animator.StringToHash("Walk");
        private static readonly int RunHash = Animator.StringToHash("Run");

        private PetController _pet;
        private Animator _animator;
        private int _currentStateHash = int.MinValue;

        private void Awake()
        {
            _pet = GetComponent<PetController>();
            _animator = GetComponent<Animator>();
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            ConfigureAnimator();
        }

        private void Start()
        {
            ConfigureAnimator();
            PlayState(IdleHash);
        }

        private void ConfigureAnimator()
        {
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

            _animator.applyRootMotion = false;

            float speed = _pet.CurrentSpeed;
            int targetHash = IdleHash;

            if (speed >= runSpeedThreshold)
                targetHash = RunHash;
            else if (speed >= walkSpeedThreshold)
                targetHash = WalkHash;

            PlayState(targetHash);
        }

        private void PlayState(int stateHash)
        {
            if (stateHash == _currentStateHash)
                return;

            _animator.CrossFadeInFixedTime(stateHash, 0.1f, 0, 0f);
            _currentStateHash = stateHash;
        }
    }
}

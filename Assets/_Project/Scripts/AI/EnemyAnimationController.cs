using UnityEngine;

namespace Project.AI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public class EnemyAnimationController : MonoBehaviour
    {
        [Header("State Names")]
        [SerializeField] private string[] idleStateNames = { "Idle01" };
        [SerializeField] private string walkStateName = "Walk";
        [SerializeField] private string runStateName = "Run";
        [SerializeField] private string[] attackStateNames = { "Attack01" };
        [SerializeField] private string[] hitStateNames = { "Hit01" };
        [SerializeField] private string deathStateName = "Death";

        [Header("Playback")]
        [SerializeField] private float crossFadeDuration = 0.12f;
        [SerializeField] private float walkSpeedThreshold = 0.05f;
        [SerializeField] private float runSpeedThreshold = 3.2f;
        [SerializeField] private float idleSwapMin = 4f;
        [SerializeField] private float idleSwapMax = 8f;
        [SerializeField] private bool lockVisualRootPosition;
        [SerializeField] private string visualChildName = "scene";

        private Animator animator;
        private EnemyAiController ai;
        private EnemyHealth health;
        private EnemyCombat combat;
        private Transform visualRoot;
        private Vector3 visualBaseLocalPosition;
        private bool isDead;
        private bool wasAttacking;
        private float idleSwapTimer;
        private int currentIdleIndex;
        private int nextAttackIndex;
        private string currentLocomotionState;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            ai = GetComponent<EnemyAiController>();
            health = GetComponent<EnemyHealth>();
            combat = GetComponent<EnemyCombat>();
            CacheVisualRoot();
        }

        private void OnEnable()
        {
            isDead = false;
            wasAttacking = false;
            nextAttackIndex = 0;
            idleSwapTimer = Random.Range(idleSwapMin, idleSwapMax);
            CacheVisualRoot();

            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }

            PlayIdle();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }

        private void LateUpdate()
        {
            if (lockVisualRootPosition)
                RestoreVisualAnchor();
        }

        private void Update()
        {
            if (animator == null || isDead)
                return;

            UpdateAttackPlayback();
            UpdateLocomotion();
            UpdateReturnToIdle();
            UpdateIdleVariation();
        }

        public void PlayIdle(int index = -1)
        {
            if (animator == null || isDead || idleStateNames == null || idleStateNames.Length == 0)
                return;

            if (index < 0 || index >= idleStateNames.Length)
                index = currentIdleIndex;

            currentIdleIndex = index;
            currentLocomotionState = null;
            CrossFade(idleStateNames[index]);
        }

        public void PlayAttack(int index = -1)
        {
            if (animator == null || isDead || attackStateNames == null || attackStateNames.Length == 0)
                return;

            if (index < 0 || index >= attackStateNames.Length)
            {
                index = nextAttackIndex;
                nextAttackIndex = (nextAttackIndex + 1) % attackStateNames.Length;
            }

            CrossFade(attackStateNames[index]);
            currentLocomotionState = null;
        }

        public void PlayHit(int index = -1)
        {
            if (animator == null || isDead || hitStateNames == null || hitStateNames.Length == 0)
                return;

            if (index < 0 || index >= hitStateNames.Length)
                index = Random.Range(0, hitStateNames.Length);

            CrossFade(hitStateNames[index]);
            currentLocomotionState = null;
        }

        public void PlayDeath()
        {
            if (animator == null || isDead || string.IsNullOrEmpty(deathStateName))
                return;

            isDead = true;
            currentLocomotionState = null;
            CrossFade(deathStateName, crossFadeDuration * 2f);
        }

        public void PlayWalk()
        {
            PlayLocomotionState(walkStateName);
        }

        public void PlayRun()
        {
            PlayLocomotionState(runStateName);
        }

        private void CacheVisualRoot()
        {
            if (!lockVisualRootPosition)
                return;

            visualRoot = string.IsNullOrEmpty(visualChildName) ? null : transform.Find(visualChildName);
            if (visualRoot == null)
            {
                MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
                if (renderer != null)
                    visualRoot = renderer.transform.parent != transform ? renderer.transform.parent : renderer.transform;
            }

            if (visualRoot == null)
                return;

            visualBaseLocalPosition = visualRoot.localPosition;
        }

        private void RestoreVisualAnchor()
        {
            if (visualRoot == null)
                return;

            visualRoot.localPosition = visualBaseLocalPosition;
        }

        private void UpdateAttackPlayback()
        {
            if (combat == null || attackStateNames == null || attackStateNames.Length == 0)
                return;

            bool attacking = combat.IsAttacking;
            if (attacking && !wasAttacking)
                PlayAttack();

            wasAttacking = attacking;
        }

        private void UpdateLocomotion()
        {
            if (combat != null && combat.IsAttacking)
                return;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (IsTransientState(stateInfo) && stateInfo.normalizedTime < 0.95f)
                return;

            float speed = ai != null ? ai.CurrentLocomotionSpeed : 0f;
            if (speed >= runSpeedThreshold && !string.IsNullOrEmpty(runStateName))
            {
                PlayLocomotionState(runStateName);
                return;
            }

            if (speed >= walkSpeedThreshold && !string.IsNullOrEmpty(walkStateName))
            {
                PlayLocomotionState(walkStateName);
                return;
            }

            if (IsLocomotionState(stateInfo))
                PlayIdle();
        }

        private void UpdateReturnToIdle()
        {
            if (combat != null && combat.IsAttacking)
                return;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!IsTransientState(stateInfo) || stateInfo.normalizedTime < 0.95f)
                return;

            PlayLocomotionOrIdle();
        }

        private void UpdateIdleVariation()
        {
            if (combat != null && combat.IsAttacking)
                return;

            if (health != null && health.IsDead)
                return;

            if (idleStateNames == null || idleStateNames.Length <= 1)
                return;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (!IsIdleState(stateInfo))
                return;

            idleSwapTimer -= Time.deltaTime;
            if (idleSwapTimer > 0f)
                return;

            idleSwapTimer = Random.Range(idleSwapMin, idleSwapMax);
            int nextIndex = currentIdleIndex;
            while (nextIndex == currentIdleIndex && idleStateNames.Length > 1)
                nextIndex = Random.Range(0, idleStateNames.Length);

            PlayIdle(nextIndex);
        }

        private void OnDamaged(float damage, bool isCritical)
        {
            if (isDead)
                return;

            PlayHit();
        }

        private void OnDied()
        {
            PlayDeath();
        }

        private void PlayLocomotionOrIdle()
        {
            float speed = ai != null ? ai.CurrentLocomotionSpeed : 0f;
            if (speed >= runSpeedThreshold && !string.IsNullOrEmpty(runStateName))
                PlayLocomotionState(runStateName);
            else if (speed >= walkSpeedThreshold && !string.IsNullOrEmpty(walkStateName))
                PlayLocomotionState(walkStateName);
            else
                PlayIdle();
        }

        private void PlayLocomotionState(string stateName)
        {
            if (animator == null || isDead || string.IsNullOrEmpty(stateName) || stateName == currentLocomotionState)
                return;

            currentLocomotionState = stateName;
            CrossFade(stateName);
        }

        private bool IsIdleState(AnimatorStateInfo stateInfo)
        {
            if (idleStateNames == null)
                return false;

            for (int i = 0; i < idleStateNames.Length; i++)
            {
                if (!string.IsNullOrEmpty(idleStateNames[i]) && stateInfo.IsName(idleStateNames[i]))
                    return true;
            }

            return false;
        }

        private bool IsLocomotionState(AnimatorStateInfo stateInfo)
        {
            return (!string.IsNullOrEmpty(walkStateName) && stateInfo.IsName(walkStateName)) ||
                   (!string.IsNullOrEmpty(runStateName) && stateInfo.IsName(runStateName));
        }

        private bool IsTransientState(AnimatorStateInfo stateInfo)
        {
            if (attackStateNames != null)
            {
                for (int i = 0; i < attackStateNames.Length; i++)
                {
                    if (!string.IsNullOrEmpty(attackStateNames[i]) && stateInfo.IsName(attackStateNames[i]))
                        return true;
                }
            }

            if (hitStateNames != null)
            {
                for (int i = 0; i < hitStateNames.Length; i++)
                {
                    if (!string.IsNullOrEmpty(hitStateNames[i]) && stateInfo.IsName(hitStateNames[i]))
                        return true;
                }
            }

            return false;
        }

        private void CrossFade(string stateName, float duration = -1f)
        {
            if (string.IsNullOrEmpty(stateName))
                return;

            if (duration < 0f)
                duration = crossFadeDuration;

            int hash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, hash))
            {
                Debug.LogWarning($"Enemy animator is missing state '{stateName}'. Assign clips and rebuild the controller.");
                return;
            }

            animator.CrossFadeInFixedTime(stateName, duration, 0, 0f);
        }
    }
}

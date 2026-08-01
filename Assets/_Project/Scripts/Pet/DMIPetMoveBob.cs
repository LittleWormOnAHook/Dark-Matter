using UnityEngine;

namespace Project.Pet
{
    /// <summary>
    /// Lightweight locomotion morph for pets without Animator clips (e.g. Brimmy).
    /// Squash/stretch on the visual child while moving; idle restores rest pose.
    /// Morphs PetVisual (or assigned target) — never the scaled root.
    /// Hop is off by default (100× pet roots amplify local Y into huge world hops).
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(50)]
    public class DMIPetMoveBob : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private PetController pet;
        [Tooltip("Mesh/visual transform to morph. Defaults to child named PetVisual.")]
        [SerializeField] private Transform visualTarget;

        [Header("Motion Detection")]
        [SerializeField] private float moveSpeedThreshold = 0.08f;

        [Header("Scale Pulse (squash / stretch)")]
        [Tooltip("Peak ±Y scale fraction while moving (0.12 = ±12%).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float scalePulsePercent = 0.12f;
        [SerializeField] private bool pulseScaleY = true;
        [Tooltip("When Y shrinks, grow XZ a bit (and vice versa) so volume feels preserved.")]
        [SerializeField] private bool compensateXZ = true;
        [Range(0f, 1f)]
        [SerializeField] private float xzCompensateRatio = 0.45f;

        [Header("Hop (local Y bob — prefer off)")]
        [SerializeField] private bool enableHop = false;
        [Tooltip("Local-space hop. Under a 100× root this becomes 100× in world — keep near 0.")]
        [SerializeField] private float hopHeight = 0f;

        [Header("Timing")]
        [SerializeField] private float frequency = 6.5f;
        [SerializeField] private float idleBlendSpeed = 10f;

        private Vector3 _restLocalScale = Vector3.one;
        private Vector3 _restLocalPosition = Vector3.zero;
        private bool _restCaptured;
        private float _phase;
        private float _moveAmount;

        private void Awake()
        {
            CacheRefs();
            CaptureRest();
        }

        private void OnEnable()
        {
            CacheRefs();
            CaptureRest();
            RestoreRestImmediate();
        }

        private void OnDisable()
        {
            RestoreRestImmediate();
        }

        private void LateUpdate()
        {
            CacheRefs();
            if (visualTarget == null)
                return;

            if (!_restCaptured)
                CaptureRest();

            float speed = pet != null ? pet.CurrentSpeed : 0f;
            bool companionOk = pet == null || pet.CompanionActive;
            float targetMove = companionOk && speed >= moveSpeedThreshold ? 1f : 0f;
            _moveAmount = Mathf.MoveTowards(_moveAmount, targetMove, idleBlendSpeed * Time.deltaTime);

            if (_moveAmount <= 0.001f)
            {
                RestoreRestImmediate();
                _phase = 0f;
                return;
            }

            _phase += frequency * Time.deltaTime * Mathf.PI * 2f;
            float wave = Mathf.Sin(_phase);
            float amount = _moveAmount;

            Vector3 scale = _restLocalScale;
            if (pulseScaleY && scalePulsePercent > 0f)
            {
                float yMul = 1f + wave * scalePulsePercent * amount;
                scale.y = _restLocalScale.y * yMul;
                if (compensateXZ)
                {
                    // Classic squash: when Y shrinks, XZ grows (and inverse on stretch).
                    float xzMul = 1f - wave * scalePulsePercent * xzCompensateRatio * amount;
                    scale.x = _restLocalScale.x * xzMul;
                    scale.z = _restLocalScale.z * xzMul;
                }
            }

            Vector3 pos = _restLocalPosition;
            if (enableHop && hopHeight > 0f)
            {
                // Absolute sine so hops land — no underground dips.
                float hop = Mathf.Abs(wave) * hopHeight * amount;
                pos.y = _restLocalPosition.y + hop;
            }

            visualTarget.localScale = scale;
            visualTarget.localPosition = pos;
        }

        private void CacheRefs()
        {
            if (pet == null)
                pet = GetComponent<PetController>();

            if (visualTarget == null)
            {
                Transform named = transform.Find("PetVisual");
                if (named != null)
                    visualTarget = named;
                else
                {
                    MeshRenderer mr = GetComponentInChildren<MeshRenderer>(true);
                    if (mr != null && mr.transform != transform)
                        visualTarget = mr.transform;
                }
            }
        }

        private void CaptureRest()
        {
            if (visualTarget == null)
                return;

            _restLocalScale = visualTarget.localScale;
            _restLocalPosition = visualTarget.localPosition;
            _restCaptured = true;
        }

        private void RestoreRestImmediate()
        {
            if (!_restCaptured || visualTarget == null)
                return;

            visualTarget.localScale = _restLocalScale;
            visualTarget.localPosition = _restLocalPosition;
        }
    }
}

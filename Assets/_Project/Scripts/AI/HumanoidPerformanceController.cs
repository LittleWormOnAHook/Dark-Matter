using Project.AI.Invector;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Distance-based humanoid rendering/animator budget for PC and consoles.
    /// </summary>
    [DisallowMultipleComponent]
    public class HumanoidPerformanceController : MonoBehaviour
    {
        [SerializeField] private float fullDetailDistance = 32f;
        [SerializeField] private float cullDistance = 64f;
        [SerializeField] private float checkInterval = 0.25f;

        private Transform _cameraTransform;
        private Animator _animator;
        private EnemyInvectorMotorBridge _motorBridge;
        private EnemyHealth _health;
        private LODGroup _lodGroup;
        private SkinnedMeshRenderer[] _skinnedRenderers;
        private float _nextCheckTime;
        private bool _culled;
        private int _perfPhase;

        private void Awake()
        {
            _perfPhase = Mathf.Abs(gameObject.GetEntityId().GetHashCode()) % 5;
            _animator = GetComponentInChildren<Animator>(true);
            _motorBridge = GetComponent<EnemyInvectorMotorBridge>();
            _health = GetComponent<EnemyHealth>();
            _lodGroup = GetComponentInChildren<LODGroup>(true);
            _skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);

            fullDetailDistance = Project.Core.PlatformGraphicsProfile.HumanoidFullDetailDistance;
            cullDistance = Project.Core.PlatformGraphicsProfile.HumanoidCullDistance;
            checkInterval = Project.Core.PlatformGraphicsProfile.HumanoidCheckInterval;
        }

        private void OnEnable()
        {
            ApplyPlatformDefaults();
            _culled = false;
            SetCulled(false);

            if (_health != null)
                _health.Died += HandleDied;
        }

        private void OnDisable()
        {
            if (_health != null)
                _health.Died -= HandleDied;
        }

        /// <summary>
        /// A corpse that died while distance-culled (animator + renderers disabled) would otherwise
        /// stay invisible forever: Update() below bails out permanently once IsDead is true, so the
        /// culled-off state from the moment of death never gets revisited. Force the corpse visible
        /// once so death presentation (ragdoll collapse, loot bag) is never silently hidden.
        /// </summary>
        private void HandleDied()
        {
            if (_culled)
                SetCulled(false);
        }

        private void Update()
        {
            if (_health != null && _health.IsDead)
                return;

            if (Time.time < _nextCheckTime)
                return;

            _nextCheckTime = Time.time + checkInterval + _perfPhase * 0.02f;
            UpdateDistanceBand();
        }

        private void ApplyPlatformDefaults()
        {
            fullDetailDistance = Project.Core.PlatformGraphicsProfile.HumanoidFullDetailDistance;
            cullDistance = Project.Core.PlatformGraphicsProfile.HumanoidCullDistance;
            checkInterval = Project.Core.PlatformGraphicsProfile.HumanoidCheckInterval;
        }

        private void UpdateDistanceBand()
        {
            if (!TryGetCameraTransform(out Transform cameraTransform))
                return;

            Vector3 delta = transform.position - cameraTransform.position;
            delta.y = 0f;
            float distance = delta.magnitude;

            if (distance >= cullDistance)
            {
                if (!_culled)
                    SetCulled(true);
                return;
            }

            if (_culled)
                SetCulled(false);

            if (_animator != null)
                _animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        private void SetCulled(bool culled)
        {
            _culled = culled;

            if (_animator != null)
            {
                _animator.enabled = !culled;
                if (!culled)
                    _animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }

            if (_skinnedRenderers != null)
            {
                for (int i = 0; i < _skinnedRenderers.Length; i++)
                {
                    SkinnedMeshRenderer renderer = _skinnedRenderers[i];
                    if (renderer != null)
                        renderer.enabled = !culled;
                }
            }

            if (_lodGroup != null)
                _lodGroup.enabled = !culled;
        }

        private bool TryGetCameraTransform(out Transform cameraTransform)
        {
            if (_cameraTransform != null)
            {
                cameraTransform = _cameraTransform;
                return true;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                cameraTransform = null;
                return false;
            }

            _cameraTransform = mainCamera.transform;
            cameraTransform = _cameraTransform;
            return true;
        }
    }
}

using Project.AI.Invector;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Distance-based humanoid rendering/animator budget for PC and consoles.
    /// Authored-on Meshy / custom body SMRs are never disabled by distance cull — only stock
    /// VBOT meshes (authored off) stay hidden. Animator may still LOD when idle and far.
    /// </summary>
    [DisallowMultipleComponent]
    public class HumanoidPerformanceController : MonoBehaviour
    {
        [SerializeField] private float fullDetailDistance = 32f;
        [SerializeField] private float cullDistance = 64f;
        [SerializeField] private float checkInterval = 0.25f;

        private Transform _cameraTransform;
        private Animator _animator;
        private EnemyAiController _aiController;
        private EnemyHealth _health;
        private EnemyInvectorRagdollBridge _ragdollBridge;
        private LODGroup _lodGroup;
        private SkinnedMeshRenderer[] _skinnedRenderers;
        private bool[] _rendererWasEnabled;
        private bool[] _protectFromCull;
        private float _nextCheckTime;
        private bool _culled;
        private int _perfPhase;

        private void Awake()
        {
            _perfPhase = Mathf.Abs(gameObject.GetEntityId().GetHashCode()) % 5;
            _animator = GetComponentInChildren<Animator>(true);
            _aiController = GetComponent<EnemyAiController>();
            _health = GetComponent<EnemyHealth>();
            _ragdollBridge = GetComponent<EnemyInvectorRagdollBridge>();
            _lodGroup = GetComponentInChildren<LODGroup>(true);
            CacheSkinnedRenderers();

            fullDetailDistance = Project.Core.PlatformGraphicsProfile.HumanoidFullDetailDistance;
            cullDistance = Project.Core.PlatformGraphicsProfile.HumanoidCullDistance;
            checkInterval = Project.Core.PlatformGraphicsProfile.HumanoidCheckInterval;
        }

        private void CacheSkinnedRenderers()
        {
            _skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            _rendererWasEnabled = new bool[_skinnedRenderers.Length];
            _protectFromCull = new bool[_skinnedRenderers.Length];
            for (int i = 0; i < _skinnedRenderers.Length; i++)
            {
                SkinnedMeshRenderer renderer = _skinnedRenderers[i];
                bool enabled = renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy;
                _rendererWasEnabled[i] = enabled;
                // Never cull Meshy / custom body meshes that ship enabled. Stock VBOT LODs stay off.
                _protectFromCull[i] = enabled && IsBodyVisualRenderer(renderer);
            }
        }

        private static bool IsBodyVisualRenderer(SkinnedMeshRenderer renderer)
        {
            if (renderer == null)
                return false;

            Transform t = renderer.transform;
            while (t != null)
            {
                string n = t.name;
                if (n.StartsWith("Drawn_", System.StringComparison.Ordinal) ||
                    n.StartsWith("Holstered_", System.StringComparison.Ordinal) ||
                    n.StartsWith("PioneerVisual_", System.StringComparison.Ordinal) ||
                    n.Equals("WeaponHolders", System.StringComparison.Ordinal) ||
                    n.IndexOf("Mesh_LOD", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;

                // Stock VBOT body under "3D Model" — not a protected Meshy visual.
                if (n.Equals("3D Model", System.StringComparison.Ordinal))
                    return false;

                t = t.parent;
            }

            return true;
        }

        private void OnEnable()
        {
            ApplyPlatformDefaults();
            _culled = false;
            EnsureProtectedBodyVisible();
            SetCulled(false);

            if (_health != null)
            {
                _health.Died += HandleDied;
                _health.Damaged += HandleDamaged;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Died -= HandleDied;
                _health.Damaged -= HandleDamaged;
            }
        }

        private void HandleDamaged(float damage, bool isCritical)
        {
            // Hits / staggers must never leave the Meshy body disabled from a cull race.
            EnsureProtectedBodyVisible();
            if (_culled)
                SetCulled(false);
        }

        /// <summary>
        /// A corpse that died while distance-culled (animator + renderers disabled) would otherwise
        /// stay invisible forever: Update() below bails out permanently once IsDead is true, so the
        /// culled-off state from the moment of death never gets revisited. Force the corpse visible
        /// once so death presentation (ragdoll collapse, loot bag) is never silently hidden.
        /// </summary>
        private void HandleDied()
        {
            ForceVisibleForDeathPresentation();
        }

        /// <summary>
        /// Un-cull immediately so death systems can bind humanoid bones for ragdoll.
        /// Safe to call repeatedly; also used by <see cref="EnemyInvectorRagdollBridge"/> before
        /// <c>LoadBodyPart</c> so distant/spawned enemies don't die with an empty bodyParts list.
        /// </summary>
        public void ForceVisibleForDeathPresentation()
        {
            EnsureProtectedBodyVisible();
            SetCulled(false);
        }

        private void Update()
        {
            if (_health != null && _health.IsDead)
                return;

            // Ragdoll / hit stagger owns the animator — do not fight it with cull LOD.
            if (IsRagdollOrStaggerBlocking())
            {
                EnsureProtectedBodyVisible();
                return;
            }

            if (Time.time < _nextCheckTime)
                return;

            _nextCheckTime = Time.time + checkInterval + _perfPhase * 0.02f;
            UpdateDistanceBand();
        }

        private bool IsRagdollOrStaggerBlocking()
        {
            return _ragdollBridge != null &&
                   (_ragdollBridge.IsHitStaggerActive || _ragdollBridge.HasActiveRagdoll);
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

            // Never disable the animator while the AI is engaged or translating — that freezes
            // the last pose while NavMesh/transform locomotion continues (intermittent glide).
            if (ShouldKeepAnimatorLive())
            {
                if (_culled)
                    SetCulled(false);

                EnsureProtectedBodyVisible();

                if (_animator != null)
                {
                    if (!_animator.enabled)
                        _animator.enabled = true;
                    _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                }

                return;
            }

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

            EnsureProtectedBodyVisible();

            if (_animator != null && _animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        private bool ShouldKeepAnimatorLive()
        {
            if (_aiController != null && _aiController.IsEngagedWithTarget)
                return true;

            if (_aiController != null && _aiController.CurrentLocomotionSpeed > 0.08f)
                return true;

            return false;
        }

        private void EnsureProtectedBodyVisible()
        {
            if (_skinnedRenderers == null || _protectFromCull == null)
                return;

            for (int i = 0; i < _skinnedRenderers.Length; i++)
            {
                if (!_protectFromCull[i])
                    continue;

                SkinnedMeshRenderer renderer = _skinnedRenderers[i];
                if (renderer == null)
                    continue;

                if (!renderer.enabled)
                    renderer.enabled = true;
                if (!renderer.gameObject.activeSelf)
                    renderer.gameObject.SetActive(true);

                renderer.updateWhenOffscreen = true;
            }
        }

        private void SetCulled(bool culled)
        {
            _culled = culled;

            if (_animator != null && !IsRagdollOrStaggerBlocking())
            {
                _animator.enabled = !culled;
                if (!culled && _animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
                    _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            if (_skinnedRenderers != null)
            {
                // Snapshot enabled flags only when entering cull so un-cull restores the authored
                // Meshy-visible / VBOT-hidden layout instead of re-enabling stock body meshes.
                if (culled)
                {
                    if (_rendererWasEnabled == null || _rendererWasEnabled.Length != _skinnedRenderers.Length)
                        _rendererWasEnabled = new bool[_skinnedRenderers.Length];

                    for (int i = 0; i < _skinnedRenderers.Length; i++)
                    {
                        SkinnedMeshRenderer renderer = _skinnedRenderers[i];
                        if (renderer == null)
                            continue;

                        // Protected Meshy / custom body: never disable on distance cull.
                        if (_protectFromCull != null && i < _protectFromCull.Length && _protectFromCull[i])
                        {
                            _rendererWasEnabled[i] = true;
                            renderer.enabled = true;
                            continue;
                        }

                        _rendererWasEnabled[i] = renderer.enabled;
                        renderer.enabled = false;
                    }
                }
                else
                {
                    for (int i = 0; i < _skinnedRenderers.Length; i++)
                    {
                        SkinnedMeshRenderer renderer = _skinnedRenderers[i];
                        if (renderer == null)
                            continue;

                        if (_protectFromCull != null && i < _protectFromCull.Length && _protectFromCull[i])
                        {
                            renderer.enabled = true;
                            continue;
                        }

                        bool restore = _rendererWasEnabled != null &&
                                       i < _rendererWasEnabled.Length &&
                                       _rendererWasEnabled[i];
                        renderer.enabled = restore;
                    }
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

using Project.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.CameraFx
{
    /// <summary>
    /// Applies trauma shake for rendering only via SRP begin/end camera callbacks.
    /// Built-in OnPreCull/OnPostRender never fire under URP, and Invector FixedUpdate
    /// posing would wipe LateUpdate offsets — so we nudge the camera transform just
    /// before cull and restore after render.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class CameraShakeListener : MonoBehaviour
    {
        [SerializeField] private bool activateOnEnable = true;

        private Camera _camera;
        private bool _shakeActive;
        private bool _appliedForRender;
        private bool _subscribed;
        private Vector3 _renderPositionOffset;
        private Quaternion _renderRotationOffset = Quaternion.identity;

        /// <summary>True when an offset was applied for the current camera render.</summary>
        public bool HasAppliedRenderOffset => _appliedForRender;

        /// <summary>Last position offset applied in beginCameraRendering (debug / tests).</summary>
        public Vector3 LastRenderPositionOffset => _renderPositionOffset;

        /// <summary>
        /// Peak |position offset| from the most recent non-zero apply (survives end-camera revert).
        /// </summary>
        public float DebugLastAppliedPositionMagnitude { get; private set; }

        public static CameraShakeListener EnsureOn(Camera camera)
        {
            if (camera == null)
                return null;

            CameraShakeService.EnsureExists();

            CameraShakeListener listener = camera.GetComponent<CameraShakeListener>();
            if (listener == null)
                listener = camera.gameObject.AddComponent<CameraShakeListener>();

            listener._camera = camera;
            listener.Activate();
            return listener;
        }

        private void Awake()
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            SubscribeRenderCallbacks();
            if (activateOnEnable)
                Activate();
        }

        private void OnDisable()
        {
            RevertRenderOffset();
            UnsubscribeRenderCallbacks();
            if (CameraShakeService.Instance != null)
                CameraShakeService.Instance.ClearActiveListener(this);
            _shakeActive = false;
        }

        public void Activate()
        {
            CameraShakeService service = CameraShakeService.EnsureExists();
            if (service == null)
                return;

            if (_camera == null)
                _camera = GetComponent<Camera>();

            SubscribeRenderCallbacks();
            service.SetActiveListener(this);
            _shakeActive = true;
        }

        public void SetActiveForShake(bool active)
        {
            if (!active)
                RevertRenderOffset();

            _shakeActive = active;
        }

        private void SubscribeRenderCallbacks()
        {
            if (_subscribed)
                return;

            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
            _subscribed = true;
        }

        private void UnsubscribeRenderCallbacks()
        {
            if (!_subscribed)
                return;

            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            _subscribed = false;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!_shakeActive || _camera == null || camera != _camera || !_camera.enabled)
                return;

            CameraShakeService service = CameraShakeService.Instance;
            if (service == null || service.ActiveListener != this)
                return;

            // Ensure any leftover offset from a skipped end callback is cleared first.
            RevertRenderOffset();

            service.SampleShake(out Vector3 positionOffset, out Vector3 eulerOffset);
            if (positionOffset.sqrMagnitude < 0.0000001f && eulerOffset.sqrMagnitude < 0.0000001f)
                return;

            Transform t = transform;
            t.position += positionOffset;
            Quaternion rotOffset = Quaternion.Euler(eulerOffset);
            t.rotation = t.rotation * rotOffset;

            _renderPositionOffset = positionOffset;
            _renderRotationOffset = rotOffset;
            _appliedForRender = true;
            DebugLastAppliedPositionMagnitude = positionOffset.magnitude;
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _camera)
                return;

            RevertRenderOffset();
        }

        private void RevertRenderOffset()
        {
            if (!_appliedForRender)
                return;

            Transform t = transform;
            t.position -= _renderPositionOffset;
            t.rotation = t.rotation * Quaternion.Inverse(_renderRotationOffset);
            _appliedForRender = false;
            _renderPositionOffset = Vector3.zero;
            _renderRotationOffset = Quaternion.identity;
        }
    }
}

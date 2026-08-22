using UnityEngine;
using UnityEngine.Rendering;

namespace Project.CameraFx
{
    /// <summary>
    /// Applies trauma shake on the gameplay camera.
    /// HDRP builds <c>HDCamera</c> matrices from the transform before
    /// <see cref="RenderPipelineManager.beginCameraRendering"/>, so offsets applied
    /// there never appear on screen. We apply in <see cref="LateUpdate"/> (after Invector
    /// FixedUpdate posing) and revert in <see cref="OnEndCameraRendering"/> after HDRP
    /// has already sampled the shaken transform.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1000)]
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

        /// <summary>Last position offset applied before HDRP sampled the camera (debug / tests).</summary>
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

        private void LateUpdate()
        {
            if (!_shakeActive || _camera == null || !_camera.enabled || !isActiveAndEnabled)
                return;

            CameraShakeService service = CameraShakeService.Instance;
            if (service == null)
                return;

            // Stay bound even if another listener briefly stole/cleared the slot.
            if (service.ActiveListener != this)
                service.SetActiveListener(this);

            // Clear any leftover offset without subtracting if Invector already reposed us.
            DiscardAppliedOffsetTracking();

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

        private void SubscribeRenderCallbacks()
        {
            if (_subscribed)
                return;

            // Only end-camera: used to restore a clean transform after HDRP sampled LateUpdate offsets.
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
            _subscribed = true;
        }

        private void UnsubscribeRenderCallbacks()
        {
            if (!_subscribed)
                return;

            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            _subscribed = false;
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
            DiscardAppliedOffsetTracking();
        }

        private void DiscardAppliedOffsetTracking()
        {
            _appliedForRender = false;
            _renderPositionOffset = Vector3.zero;
            _renderRotationOffset = Quaternion.identity;
        }
    }
}

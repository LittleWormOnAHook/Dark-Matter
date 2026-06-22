using Project.Data;
using Project.Player;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Project.Interaction
{
    /// <summary>
    /// Dedicated optics camera rendering to a RenderTexture for masked UI display.
    /// Blacks out the gameplay camera while active so only the masked RT is visible.
    /// </summary>
    public class OpticsCameraRig : MonoBehaviour
    {
        [SerializeField] private int renderWidth = 1280;
        [SerializeField] private int renderHeight = 720;
        [SerializeField] private int renderDepth = 24;
        [SerializeField] private float zoomLerpSpeed = 28f;

        private Transform rigRoot;
        private Camera opticsCamera;
        private Camera sourceCamera;
        private Camera blackoutCamera;
        private RenderTexture renderTexture;
        private PlayerController playerController;
        private bool isActive;
        private bool mainCameraBlackedOut;
        private LayerMask storedCullingMask;
        private CameraClearFlags storedClearFlags;
        private Color storedBackgroundColor;

        public RenderTexture RenderTexture => renderTexture;
        public bool IsActive => isActive;
        public bool IsMainCameraBlackedOut => mainCameraBlackedOut;
        public bool IsOutputReady => renderTexture != null && opticsCamera != null;
        public bool HasValidOutput => isActive && IsOutputReady && opticsCamera.enabled;
        public Camera OpticsCamera => opticsCamera;

        public void Initialize(PlayerController controller, Camera mainCamera)
        {
            playerController = controller;
            sourceCamera = ResolveSourceCamera(mainCamera);
            EnsureRigRoot();
            EnsureCamera();
        }

        public bool EnsureOutputReady()
        {
            sourceCamera = ResolveSourceCamera(sourceCamera);
            EnsureRigRoot();
            EnsureCamera();
            return IsOutputReady;
        }

        public bool Activate(ToolType toolType)
        {
            if (!EnsureOutputReady() || opticsCamera == null)
                return false;

            isActive = true;
            rigRoot.gameObject.SetActive(true);
            opticsCamera.enabled = true;
            BlackoutMainCamera();
            SyncFromSource(immediate: true);
            return HasValidOutput;
        }

        public void Deactivate()
        {
            isActive = false;

            if (opticsCamera != null)
                opticsCamera.enabled = false;

            if (rigRoot != null)
                rigRoot.gameObject.SetActive(false);

            ForceRestoreMainCamera();
        }

        public void ForceRestoreMainCamera()
        {
            RestoreMainCamera();
        }

        public void SetFieldOfView(float fov)
        {
            if (opticsCamera != null)
                opticsCamera.fieldOfView = fov;
        }

        private void LateUpdate()
        {
            if (!isActive)
                return;

            sourceCamera = ResolveSourceCamera(sourceCamera);
            SyncFromSource(immediate: false);
        }

        private void SyncFromSource(bool immediate)
        {
            if (opticsCamera == null || playerController == null)
                return;

            sourceCamera = ResolveSourceCamera(sourceCamera);

            opticsCamera.transform.SetPositionAndRotation(
                playerController.OpticsEyeWorldPosition,
                playerController.OpticsLookRotation);

            float targetFov = playerController.OpticsTargetFov;
            if (immediate)
                opticsCamera.fieldOfView = targetFov;
            else
                opticsCamera.fieldOfView = Mathf.Lerp(opticsCamera.fieldOfView, targetFov, Time.deltaTime * zoomLerpSpeed);

            if (sourceCamera != null)
            {
                opticsCamera.nearClipPlane = sourceCamera.nearClipPlane;
                opticsCamera.farClipPlane = sourceCamera.farClipPlane;
            }
        }

        private void BlackoutMainCamera()
        {
            sourceCamera = ResolveSourceCamera(sourceCamera);
            if (sourceCamera == null || mainCameraBlackedOut)
                return;

            blackoutCamera = sourceCamera;
            storedCullingMask = blackoutCamera.cullingMask;
            storedClearFlags = blackoutCamera.clearFlags;
            storedBackgroundColor = blackoutCamera.backgroundColor;

            blackoutCamera.cullingMask = 0;
            blackoutCamera.clearFlags = CameraClearFlags.SolidColor;
            blackoutCamera.backgroundColor = Color.black;
            mainCameraBlackedOut = true;
        }

        private void RestoreMainCamera()
        {
            if (!mainCameraBlackedOut)
                return;

            Camera target = blackoutCamera != null ? blackoutCamera : ResolveSourceCamera(sourceCamera);
            if (target == null)
                return;

            target.cullingMask = storedCullingMask;
            target.clearFlags = storedClearFlags;
            target.backgroundColor = storedBackgroundColor;
            target.enabled = true;
            mainCameraBlackedOut = false;
            blackoutCamera = null;
        }

        private Camera ResolveSourceCamera(Camera fallback)
        {
            if (playerController != null)
            {
                Camera fromPlayer = playerController.GameplayCamera;
                if (fromPlayer != null)
                    return fromPlayer;
            }

            if (fallback != null)
                return fallback;

            return Camera.main;
        }

        private void EnsureRigRoot()
        {
            if (rigRoot != null)
                return;

            GameObject rootObject = new GameObject("OpticsCameraRigRoot");
            rigRoot = rootObject.transform;
            rigRoot.SetParent(transform, false);
            rigRoot.gameObject.SetActive(false);
        }

        private void EnsureCamera()
        {
            if (opticsCamera != null)
                return;

            EnsureRigRoot();

            GameObject cameraObject = new GameObject("OpticsCamera");
            cameraObject.transform.SetParent(rigRoot, false);
            opticsCamera = cameraObject.AddComponent<Camera>();

            sourceCamera = ResolveSourceCamera(sourceCamera);
            if (sourceCamera != null)
            {
                opticsCamera.cullingMask = sourceCamera.cullingMask;
                opticsCamera.clearFlags = sourceCamera.clearFlags;
                opticsCamera.backgroundColor = sourceCamera.backgroundColor;
                opticsCamera.allowHDR = sourceCamera.allowHDR;
                opticsCamera.allowMSAA = sourceCamera.allowMSAA;
            }
            else
            {
                opticsCamera.clearFlags = CameraClearFlags.Skybox;
            }

            opticsCamera.depth = sourceCamera != null ? sourceCamera.depth + 1f : 10f;
            opticsCamera.enabled = false;

            if (sourceCamera != null &&
                sourceCamera.TryGetComponent(out UniversalAdditionalCameraData sourceData))
            {
                UniversalAdditionalCameraData opticsData = opticsCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                opticsData.renderType = sourceData.renderType;
                opticsData.renderPostProcessing = sourceData.renderPostProcessing;
            }

            EnsureRenderTexture();
            opticsCamera.targetTexture = renderTexture;
        }

        private void EnsureRenderTexture()
        {
            if (renderTexture != null &&
                renderTexture.width == renderWidth &&
                renderTexture.height == renderHeight)
                return;

            if (renderTexture != null)
            {
                if (opticsCamera != null)
                    opticsCamera.targetTexture = null;

                renderTexture.Release();
                Destroy(renderTexture);
            }

            renderTexture = new RenderTexture(renderWidth, renderHeight, renderDepth, RenderTextureFormat.ARGB32)
            {
                name = "OpticsViewRT",
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false
            };
            renderTexture.Create();

            if (opticsCamera != null)
                opticsCamera.targetTexture = renderTexture;
        }

        private void OnDestroy()
        {
            ForceRestoreMainCamera();

            if (opticsCamera != null)
                opticsCamera.targetTexture = null;

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }
    }
}

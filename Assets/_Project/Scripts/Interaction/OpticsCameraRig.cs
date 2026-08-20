using Project.Data;
using Project.Player;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.Universal;

namespace Project.Interaction
{
    /// <summary>
    /// Optics view helper. Under URP: dedicated camera → RenderTexture for masked UI.
    /// Under HDRP: passthrough mode (zoom + UI frame on the live gameplay camera) — a second
    /// HDRP camera writing an RT into a Canvas RawImage crashes in DrawRawMesh / D3D12.
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
        private bool passthroughMode;
        private LayerMask storedCullingMask;
        private CameraClearFlags storedClearFlags;
        private Color storedBackgroundColor;
        private HDAdditionalCameraData.ClearColorMode storedHdClearColorMode;
        private Color storedHdBackgroundColorHdr;
        private bool storedHdClearMode;

        public RenderTexture RenderTexture => renderTexture;
        public bool IsActive => isActive;
        public bool IsMainCameraBlackedOut => mainCameraBlackedOut;
        public bool IsPassthroughMode => passthroughMode;
        public bool IsOutputReady => passthroughMode
            ? sourceCamera != null || ResolveSourceCamera(null) != null
            : renderTexture != null && opticsCamera != null;
        public bool HasValidOutput => isActive && IsOutputReady && (passthroughMode || (opticsCamera != null && opticsCamera.enabled));
        public Camera OpticsCamera => passthroughMode ? ResolveSourceCamera(sourceCamera) : opticsCamera;

        private static bool IsHdrpActive()
        {
            RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
            return pipeline is HDRenderPipelineAsset;
        }

        public void Initialize(PlayerController controller, Camera mainCamera)
        {
            playerController = controller;
            sourceCamera = ResolveSourceCamera(mainCamera);
            passthroughMode = IsHdrpActive();
            if (!passthroughMode)
            {
                EnsureRigRoot();
                EnsureCamera();
            }
            else
            {
                DisableOffscreenRig();
            }
        }

        public bool EnsureOutputReady()
        {
            sourceCamera = ResolveSourceCamera(sourceCamera);
            passthroughMode = IsHdrpActive();

            if (passthroughMode)
            {
                DisableOffscreenRig();
                return sourceCamera != null;
            }

            EnsureRigRoot();
            EnsureCamera();

            if (opticsCamera != null && renderTexture != null && opticsCamera.targetTexture != renderTexture)
                opticsCamera.targetTexture = renderTexture;

            return IsOutputReady;
        }

        public bool Activate(ToolType toolType)
        {
            if (!EnsureOutputReady())
                return false;

            isActive = true;

            if (passthroughMode)
            {
                // Live gameplay camera + UI scope frame. No RT / no blackout (HDRP-safe).
                return HasValidOutput;
            }

            if (opticsCamera == null)
                return false;

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
            if (passthroughMode)
            {
                Camera cam = ResolveSourceCamera(sourceCamera);
                if (cam != null)
                    cam.fieldOfView = fov;
                return;
            }

            if (opticsCamera != null)
                opticsCamera.fieldOfView = fov;
        }

        private void LateUpdate()
        {
            if (!isActive)
                return;

            if (passthroughMode)
            {
                // Re-apply after Invector FixedUpdate FOV reset so scroll zoom sticks.
                if (playerController != null)
                    playerController.ApplyOpticsCameraFov();
                else
                    SetFieldOfView(GetFallbackPassthroughFov());
                return;
            }

            sourceCamera = ResolveSourceCamera(sourceCamera);
            SyncFromSource(immediate: false);
        }

        private float GetFallbackPassthroughFov()
        {
            if (playerController != null)
                return playerController.OpticsZoomFov;

            Camera cam = ResolveSourceCamera(sourceCamera);
            return cam != null ? cam.fieldOfView : 40f;
        }

        private void SyncFromSource(bool immediate)
        {
            if (opticsCamera == null || playerController == null)
                return;

            sourceCamera = ResolveSourceCamera(sourceCamera);

            opticsCamera.transform.SetPositionAndRotation(
                playerController.OpticsEyeWorldPosition,
                playerController.OpticsLookRotation);
            playerController.AlignPlayerToOpticsLook();

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
            if (passthroughMode)
                return;

            sourceCamera = ResolveSourceCamera(sourceCamera);
            if (sourceCamera == null || mainCameraBlackedOut)
                return;

            blackoutCamera = sourceCamera;
            storedCullingMask = blackoutCamera.cullingMask;
            storedClearFlags = blackoutCamera.clearFlags;
            storedBackgroundColor = blackoutCamera.backgroundColor;

            if (blackoutCamera.TryGetComponent(out HDAdditionalCameraData hdData))
            {
                storedHdClearColorMode = hdData.clearColorMode;
                storedHdBackgroundColorHdr = hdData.backgroundColorHDR;
                storedHdClearMode = true;
                hdData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                hdData.backgroundColorHDR = Color.black;
            }
            else
            {
                storedHdClearMode = false;
            }

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

            if (storedHdClearMode && target.TryGetComponent(out HDAdditionalCameraData hdData))
            {
                hdData.clearColorMode = storedHdClearColorMode;
                hdData.backgroundColorHDR = storedHdBackgroundColorHdr;
            }

            target.enabled = true;
            mainCameraBlackedOut = false;
            blackoutCamera = null;
            storedHdClearMode = false;
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

        private void DisableOffscreenRig()
        {
            if (opticsCamera != null)
            {
                opticsCamera.targetTexture = null;
                opticsCamera.enabled = false;
            }

            if (rigRoot != null)
                rigRoot.gameObject.SetActive(false);

            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
                renderTexture = null;
            }
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
            EnsureRigRoot();

            if (opticsCamera == null)
            {
                GameObject cameraObject = new GameObject("OpticsCamera");
                cameraObject.transform.SetParent(rigRoot, false);
                opticsCamera = cameraObject.AddComponent<Camera>();
            }

            sourceCamera = ResolveSourceCamera(sourceCamera);
            if (sourceCamera != null)
            {
                opticsCamera.cullingMask = sourceCamera.cullingMask;
                opticsCamera.clearFlags = sourceCamera.clearFlags;
                opticsCamera.backgroundColor = sourceCamera.backgroundColor;
                opticsCamera.allowHDR = sourceCamera.allowHDR;
                opticsCamera.allowMSAA = false;
            }
            else
            {
                opticsCamera.clearFlags = CameraClearFlags.Skybox;
                opticsCamera.allowMSAA = false;
            }

            opticsCamera.depth = sourceCamera != null ? sourceCamera.depth + 1f : 10f;
            if (!isActive)
                opticsCamera.enabled = false;

            ConfigurePipelineCameraDataUrp();
            EnsureRenderTexture();
            opticsCamera.targetTexture = renderTexture;
        }

        private void ConfigurePipelineCameraDataUrp()
        {
            // URP-only path. Never attach UniversalAdditionalCameraData while HDRP is active.
            HDAdditionalCameraData leftoverHd = opticsCamera.GetComponent<HDAdditionalCameraData>();
            if (leftoverHd != null)
                Destroy(leftoverHd);

            UniversalAdditionalCameraData opticsData = opticsCamera.GetComponent<UniversalAdditionalCameraData>();
            if (opticsData == null)
                opticsData = opticsCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();

            opticsData.renderType = CameraRenderType.Base;
            if (sourceCamera != null &&
                sourceCamera.TryGetComponent(out UniversalAdditionalCameraData sourceData))
            {
                opticsData.renderPostProcessing = sourceData.renderPostProcessing;
            }
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

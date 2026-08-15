using Project.Interaction;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Project.Rendering
{
    /// <summary>
    /// Keeps <see cref="ScannerHdrpCustomPass"/> off by default.
    /// Enabling the AfterPostProcess Custom Pass during a sweep caused native D3D12 crashes;
    /// leave <see cref="enableDuringSweep"/> false until a safer fullscreen path exists.
    /// World-space sweep disc + outlines remain the active scanner FX.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CustomPassVolume))]
    public sealed class ScannerHdrpOverlayGate : MonoBehaviour
    {
        [SerializeField] private CustomPassVolume customPassVolume;
        [SerializeField] private ScannerSweepController sweepController;
        [Tooltip("Unsafe under current HDRP Custom Pass blit — keep false to avoid GPU crashes.")]
        [SerializeField] private bool enableDuringSweep = false;

        private void Awake()
        {
            if (customPassVolume == null)
                customPassVolume = GetComponent<CustomPassVolume>();

            if (sweepController == null)
                sweepController = GetComponentInParent<ScannerSweepController>()
                    ?? FindAnyObjectByType<ScannerSweepController>();

            // Always force off at start so prefab/scene overrides cannot leave the pass live.
            SetVolumeEnabled(false);
        }

        private void OnDisable()
        {
            SetVolumeEnabled(false);
        }

        private void LateUpdate()
        {
            // Hard-disable path: never turn the volume on while the crash-prone pass is wired.
            if (!enableDuringSweep)
            {
                SetVolumeEnabled(false);
                return;
            }

            bool wantEnabled = sweepController != null && sweepController.IsSweeping;
            SetVolumeEnabled(wantEnabled);
        }

        private void SetVolumeEnabled(bool enabled)
        {
            if (customPassVolume != null && customPassVolume.enabled != enabled)
                customPassVolume.enabled = enabled;
        }
    }
}

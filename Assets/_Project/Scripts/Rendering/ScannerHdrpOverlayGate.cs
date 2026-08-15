using Project.Interaction;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Project.Rendering
{
    /// <summary>
    /// Keeps <see cref="ScannerHdrpCustomPass"/> off unless a scanner sweep is active.
    /// Prevents the always-on AfterPostProcess overlay from replacing the Game view with scanlines.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CustomPassVolume))]
    public sealed class ScannerHdrpOverlayGate : MonoBehaviour
    {
        [SerializeField] private CustomPassVolume customPassVolume;
        [SerializeField] private ScannerSweepController sweepController;
        [SerializeField] private bool enableDuringSweep = true;

        private void Awake()
        {
            if (customPassVolume == null)
                customPassVolume = GetComponent<CustomPassVolume>();

            if (sweepController == null)
                sweepController = GetComponentInParent<ScannerSweepController>()
                    ?? FindAnyObjectByType<ScannerSweepController>();

            SetVolumeEnabled(false);
        }

        private void OnDisable()
        {
            SetVolumeEnabled(false);
        }

        private void LateUpdate()
        {
            bool wantEnabled = enableDuringSweep
                && sweepController != null
                && sweepController.IsSweeping;

            SetVolumeEnabled(wantEnabled);
        }

        private void SetVolumeEnabled(bool enabled)
        {
            if (customPassVolume != null && customPassVolume.enabled != enabled)
                customPassVolume.enabled = enabled;
        }
    }
}

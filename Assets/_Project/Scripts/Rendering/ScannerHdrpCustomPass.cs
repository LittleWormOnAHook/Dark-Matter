using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Project.Rendering
{
    /// <summary>
    /// HDRP Custom Pass that applies <c>Custom/ScannerPostProcess</c> as a fullscreen scanline overlay.
    /// URP continues to use <see cref="ScannerPostProcess"/> (OnRenderImage) when available.
    /// Must resolve/bind like <see cref="FullScreenCustomPass"/> or AfterPostProcess samples return black.
    /// </summary>
    [Serializable]
    public sealed class ScannerHdrpCustomPass : CustomPass
    {
        private const string FullscreenPassName = "ScannerFullscreen";

        [SerializeField] private Material scanlinesMaterial;
        [SerializeField] private float scanSpeed = 2f;
        [SerializeField] private float lineThickness = 2f;
        [SerializeField] private Color scanColor = new Color(0f, 1f, 1f, 0.3f);

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (scanlinesMaterial != null)
                return;

            Shader shader = Shader.Find("Custom/ScannerPostProcess");
            if (shader != null)
                scanlinesMaterial = CoreUtils.CreateEngineMaterial(shader);
        }

        protected override void Execute(CustomPassContext ctx)
        {
            // Crash-prone AfterPostProcess blit — gate must keep the volume disabled.
            // Extra guard if a scene somehow enables the volume without the overlay gate.
            if (scanlinesMaterial == null)
                return;

            scanlinesMaterial.SetFloat("_ScanSpeed", scanSpeed);
            scanlinesMaterial.SetFloat("_LineThickness", lineThickness);
            scanlinesMaterial.SetColor("_ScanColor", scanColor);
            scanlinesMaterial.SetFloat("_FadeValue", fadeValue);

            // Same fetch path as FullScreenCustomPass.fetchColorBuffer — without this,
            // CustomPassSample/LoadCameraColor at AfterPostProcess reads an unbound buffer (black).
            ResolveMSAAColorBuffer(ctx);
            SetRenderTargetAuto(ctx.cmd);

            int passIndex = scanlinesMaterial.FindPass(FullscreenPassName);
            if (passIndex < 0)
                passIndex = 0;

            CoreUtils.DrawFullScreen(ctx.cmd, scanlinesMaterial, shaderPassId: passIndex);
        }

        protected override void Cleanup()
        {
            CoreUtils.Destroy(scanlinesMaterial);
            scanlinesMaterial = null;
        }
    }
}

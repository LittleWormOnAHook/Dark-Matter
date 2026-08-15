using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Project.Rendering
{
    /// <summary>
    /// HDRP Custom Pass that applies <c>Custom/ScannerPostProcess</c> as a fullscreen scanline overlay.
    /// URP continues to use <see cref="ScannerPostProcess"/> (OnRenderImage) when available.
    /// Dual-pipeline choice: keep URP component + HDRP Custom Pass; do not force Phase 6.
    /// </summary>
    [Serializable]
    public sealed class ScannerHdrpCustomPass : CustomPass
    {
        [SerializeField] private Material scanlinesMaterial;
        [SerializeField] private float scanSpeed = 2f;
        [SerializeField] private float lineThickness = 200f;
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
            if (scanlinesMaterial == null)
                return;

            scanlinesMaterial.SetFloat("_ScanSpeed", scanSpeed);
            scanlinesMaterial.SetFloat("_LineThickness", lineThickness);
            scanlinesMaterial.SetColor("_ScanColor", scanColor);

            // Pass 0 = CustomPassLoadCameraColor path when CustomPassCommon is included.
            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);
            CoreUtils.DrawFullScreen(ctx.cmd, scanlinesMaterial, shaderPassId: 0);
        }
    }
}

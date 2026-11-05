using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

namespace Project.EditorTools.Rendering
{
    /// <summary>
    /// 2026-11-04a. Build-time shader stripping (Unity's IPreprocessShaders API, the same hook HDRP uses to
    /// strip DXR passes when ray tracing is off). Removes only the ray tracing (DXR) passes of the listed
    /// vendor shader graphs. Their generated closesthit code uses ddx/ddy or implicit-LOD Sample, which DXR
    /// libraries (lib_6_3) reject, so those passes never compiled and logged 141 "Shader error in" lines
    /// per player build. The passes were already unusable, so stripping them changes nothing on screen; the
    /// raster passes (what renders the water / BotD foliage normally) are untouched, and the vendor graphs
    /// are not edited. Set <see cref="Enabled"/> false to restore the old behaviour.
    /// </summary>
    internal sealed class DMRayTracingPassStripper : IPreprocessShaders
    {
        public static bool Enabled = true;

        private static readonly HashSet<string> ShaderNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "Shader Graphs/PW_Water_RP",
            "Shader Graphs/BotD_Graph_Lit_TranslucentAlphaCutoff",
        };

        // After HDRP's own preprocessor (callbackOrder 0) so we only ever remove, never re-add.
        public int callbackOrder => 100;

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (!Enabled || shader == null || data == null || data.Count == 0)
                return;

            if (!ShaderNames.Contains(shader.name))
                return;

            if (!IsRayTracingPass(snippet))
                return;

            data.Clear();
        }

        private static bool IsRayTracingPass(ShaderSnippetData snippet)
        {
            if (snippet.shaderType == ShaderType.RayTracing)
                return true;

            string pass = snippet.passName;
            return !string.IsNullOrEmpty(pass) && pass.EndsWith("DXR", StringComparison.Ordinal);
        }
    }
}

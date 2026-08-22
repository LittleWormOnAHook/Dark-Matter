using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.EditorTools.Build
{
    /// <summary>
    /// Player-build safety net for shaders that break or explode Windows builds.
    /// </summary>
    public sealed class DmPlayerBuildHeavyShaderStripper : IPreprocessShaders
    {
        private const string LogPrefix = "[DM Build Shader Strip]";

        private static readonly HashSet<string> StripAllVariants = new HashSet<string>(StringComparer.Ordinal)
        {
            // Dual-target (URP+HDRP+DXR) graph — ~27GB variant prep crash.
            // Materials should already be remapped to HDRP/Lit; this clears leftovers.
            "Shader Graphs/glTF-pbrMetallicRoughness",
        };

        private static readonly HashSet<string> StripDxrPasses = new HashSet<string>(StringComparer.Ordinal)
        {
            // TMP HDRP Shader Graphs fail DXR compiles (UUM-3330 / texture2D in SDFFunctions).
            // Screen/UI text does not need ray-traced passes in the player.
            "TextMeshPro/SRP/TMP_SDF-HDRP LIT",
            "TextMeshPro/SRP/TMP_SDF-HDRP UNLIT",
        };

        public int callbackOrder => -1000;

        public void OnProcessShader(
            Shader shader,
            ShaderSnippetData snippet,
            IList<ShaderCompilerData> data)
        {
            if (shader == null || data == null || data.Count == 0)
                return;

            if (StripAllVariants.Contains(shader.name))
            {
                int before = data.Count;
                data.Clear();
                Debug.Log(
                    $"{LogPrefix} Cleared {before} variant(s) for '{shader.name}' " +
                    $"pass={snippet.passName} stage={snippet.shaderType}.");
                return;
            }

            if (StripDxrPasses.Contains(shader.name) && IsDxrPass(snippet))
            {
                int before = data.Count;
                data.Clear();
                Debug.Log(
                    $"{LogPrefix} Cleared {before} DXR variant(s) for '{shader.name}' " +
                    $"pass={snippet.passName}.");
            }
        }

        private static bool IsDxrPass(ShaderSnippetData snippet)
        {
            string pass = snippet.passName ?? string.Empty;
            return pass.IndexOf("DXR", StringComparison.OrdinalIgnoreCase) >= 0
                   || pass.IndexOf("RayTracing", StringComparison.OrdinalIgnoreCase) >= 0
                   || pass.IndexOf("PathTracing", StringComparison.OrdinalIgnoreCase) >= 0
                   || snippet.shaderType == ShaderType.RayTracing;
        }
    }
}

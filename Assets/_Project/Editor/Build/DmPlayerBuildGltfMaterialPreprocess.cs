using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Project.EditorTools.Rendering;

namespace Project.EditorTools.Build
{
    /// <summary>
    /// Before a player build, remaps any materials still on
    /// <c>Shader Graphs/glTF-pbrMetallicRoughness</c> to <c>HDRP/Lit</c>.
    /// Embedded <c>.glb</c>/<c>.gltf</c> materials often snap back to the glTFast
    /// graph on reimport; this keeps the Windows build from compiling that
    /// dual-target graph (native crash in PrepareStageVariants / ~27GB prep).
    /// </summary>
    public sealed class DmPlayerBuildGltfMaterialPreprocess : IPreprocessBuildWithReport
    {
        private const string LogPrefix = "[DM Build glTF Preprocess]";

        public int callbackOrder => -500;

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log($"{LogPrefix} Remapping glTF-pbr materials before player build ({report.summary.platform})…");
            DmGltfShaderMaterialToHdrpConverter.ConversionReport result =
                DmGltfShaderMaterialToHdrpConverter.ConvertAll(dryRun: false);

            if (result.Failed > 0)
            {
                throw new BuildFailedException(
                    $"{LogPrefix} Failed to remap {result.Failed} glTF-pbr material(s). " +
                    "Fix those assets or the Windows build may crash during shader variant prep.");
            }

            Debug.Log($"{LogPrefix} {result.ToSummary()}");
        }
    }
}

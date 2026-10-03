using System;
using UnityEngine;

namespace GenesisPCG.RockCreation
{
    /// <summary>
    /// Edit-time generated meshes are rebuilt often; the GPU Resident Drawer (BatchRendererGroup) can then
    /// submit draws with stale mesh ids ("A BatchDrawCommand was submitted with an invalid Batch, Mesh, or
    /// Material ID"). SRP Core ships UnityEngine.Rendering.DisallowGPUDrivenRendering for exactly this, but in
    /// this SRP version the class is internal, so it is added by type. It turns
    /// MeshRenderer.allowGPUDrivenRendering off for the renderer on the same GameObject.
    /// </summary>
    public static class PcgRenderUtil
    {
        private const string DisallowTypeName =
            "UnityEngine.Rendering.DisallowGPUDrivenRendering, Unity.RenderPipelines.GPUDriven.Runtime";

        private static Type s_disallowType;
        private static bool s_lookedUp;

        public static Type DisallowGpuDrivenType
        {
            get
            {
                if (!s_lookedUp)
                {
                    s_lookedUp = true;
                    s_disallowType = Type.GetType(DisallowTypeName, false);
                }
                return s_disallowType;
            }
        }

        /// <summary>Ensures the GameObject's MeshRenderer is drawn by the classic path, not the GPU Resident Drawer.</summary>
        public static void DisallowGpuDriven(GameObject go)
        {
            Type t = DisallowGpuDrivenType;
            if (go == null || t == null)
                return;
            Component existing = go.GetComponent(t);
            if (existing == null)
            {
                existing = go.AddComponent(t);
                // Keep it out of the way in the inspector; it has no user-facing settings.
                existing.hideFlags |= HideFlags.HideInInspector;
            }
            else if (existing is Behaviour b && !b.enabled)
            {
                b.enabled = true;
            }
        }

        /// <summary>
        /// Lets the GPU Resident Drawer draw the GameObject again (baked rocks use saved asset meshes, which are fine).
        /// The component's OnDisable restores MeshRenderer.allowGPUDrivenRendering.
        /// </summary>
        public static void AllowGpuDriven(GameObject go, bool removeComponent)
        {
            Type t = DisallowGpuDrivenType;
            if (go == null || t == null)
                return;
            Component existing = go.GetComponent(t);
            if (existing == null)
                return;
            if (removeComponent)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(existing);
                else UnityEngine.Object.DestroyImmediate(existing);
            }
            else if (existing is Behaviour b && b.enabled)
            {
                b.enabled = false;
            }
        }

        public static bool IsGpuDrivenDisallowed(GameObject go)
        {
            Type t = DisallowGpuDrivenType;
            if (go == null || t == null)
                return false;
            return go.GetComponent(t) is Behaviour b && b.enabled;
        }
    }
}

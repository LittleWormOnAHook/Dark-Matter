using System;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Rendering
{
    /// <summary>
    /// Shared rules for when mesh materials should be cloned at runtime.
    /// </summary>
    public static class DMRuntimeMaterialInstancingPolicy
    {
        public static bool ShouldSkipRenderer(Renderer renderer, bool skipParticleRenderers)
        {
            if (renderer == null)
                return true;

            if (skipParticleRenderers && renderer is ParticleSystemRenderer)
                return true;

            if (renderer is SpriteRenderer spriteRenderer && IsUnderCanvas(spriteRenderer.transform))
                return true;

            if (IsUnderCanvas(renderer.transform))
                return true;

            if (IsResourceScanCone(renderer.transform))
                return true;

            return false;
        }

        public static bool IsUnderCanvas(Transform t)
        {
            return t != null && t.GetComponentInParent<Canvas>(true) != null;
        }

        private static bool IsResourceScanCone(Transform t)
        {
            while (t != null)
            {
                string n = t.name;
                if (n.Equals("Scan Cone Field", StringComparison.OrdinalIgnoreCase)
                    || n.Equals("Scan Cone", StringComparison.OrdinalIgnoreCase))
                    return true;
                t = t.parent;
            }

            return false;
        }
    }
}

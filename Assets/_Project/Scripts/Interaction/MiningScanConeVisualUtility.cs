using Project.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Project.Interaction
{
    /// <summary>
    /// Runtime HDRP-safe scan cone look. Player_v7's authored cone uses QFX
    /// DistortionCutOut, which does not draw as a visible mesh in HDRP.
    /// </summary>
    public static class MiningScanConeVisualUtility
    {
        private static readonly Color ScanColor = new Color(0.45f, 0.85f, 1f, 0.52f);
        private static Material cachedMaterial;

        public static void EnsureScanConeMaterials(GameObject cone)
        {
            if (cone == null)
                return;

            Renderer[] renderers = cone.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                EnsureVisibleMaterial(renderers[i]);

            DMIMaterialPulseScroll pulse = cone.GetComponent<DMIMaterialPulseScroll>();
            if (pulse == null)
                pulse = cone.AddComponent<DMIMaterialPulseScroll>();

            pulse.ConfigureForScanCone();
        }

        public static void EnsureVisibleMaterial(Renderer renderer)
        {
            if (renderer == null)
                return;

            Material material = ResolveMaterial();
            if (material == null)
                return;

            if (renderer.sharedMaterial == material)
                return;

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static Material ResolveMaterial()
        {
            if (cachedMaterial != null)
                return cachedMaterial;

            Shader shader = Shader.Find("HDRP/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color");
            if (shader == null)
                return null;

            cachedMaterial = new Material(shader)
            {
                name = "DM_MiningScanCone (Runtime)",
                hideFlags = HideFlags.HideAndDontSave,
                color = ScanColor,
                renderQueue = 3000,
                doubleSidedGI = true
            };

            if (cachedMaterial.HasProperty("_UnlitColor"))
                cachedMaterial.SetColor("_UnlitColor", ScanColor);
            if (cachedMaterial.HasProperty("_BaseColor"))
                cachedMaterial.SetColor("_BaseColor", ScanColor);
            if (cachedMaterial.HasProperty("_Color"))
                cachedMaterial.SetColor("_Color", ScanColor);

            if (shader.name.StartsWith("HDRP/", System.StringComparison.Ordinal))
            {
                HDMaterial.SetSurfaceType(cachedMaterial, true);
                if (cachedMaterial.HasProperty("_BlendMode"))
                    cachedMaterial.SetFloat("_BlendMode", 0f);
                if (cachedMaterial.HasProperty("_DoubleSidedEnable"))
                    cachedMaterial.SetFloat("_DoubleSidedEnable", 1f);
                if (cachedMaterial.HasProperty("_CullMode"))
                    cachedMaterial.SetFloat("_CullMode", 0f);
                if (cachedMaterial.HasProperty("_CullModeForward"))
                    cachedMaterial.SetFloat("_CullModeForward", 0f);
                cachedMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                cachedMaterial.EnableKeyword("_DOUBLESIDED_ON");
                cachedMaterial.SetShaderPassEnabled("DistortionVectors", false);
                HDMaterial.ValidateMaterial(cachedMaterial);
                if (cachedMaterial.HasProperty("_DoubleSidedEnable"))
                    cachedMaterial.SetFloat("_DoubleSidedEnable", 1f);
                if (cachedMaterial.HasProperty("_CullMode"))
                    cachedMaterial.SetFloat("_CullMode", 0f);
                if (cachedMaterial.HasProperty("_CullModeForward"))
                    cachedMaterial.SetFloat("_CullModeForward", 0f);
                cachedMaterial.EnableKeyword("_DOUBLESIDED_ON");
            }

            return cachedMaterial;
        }
    }
}

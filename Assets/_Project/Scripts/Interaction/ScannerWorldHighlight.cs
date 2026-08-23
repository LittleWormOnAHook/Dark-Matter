using System.Collections.Generic;
using Project.UI;
using UnityEngine;

namespace Project.Interaction
{
    /// <summary>
    /// Legacy world-space glow billboards. Scanner highlights now use OutlineController directly.
    /// </summary>
    public class ScannerWorldHighlight : MonoBehaviour
    {
        private const int MaxHighlights = 24;

        private readonly List<GlowEntry> pool = new List<GlowEntry>(MaxHighlights);
        private Material glowMaterial;
        private Transform poolRoot;
        private bool poolBuilt;
        [SerializeField] private Shader glowShader;
        private static Shader s_cachedGlowShader;

        private void Awake()
        {
            CacheGlowShader();
        }

        private void OnEnable()
        {
            CacheGlowShader();
        }

        private void CacheGlowShader()
        {
            if (glowShader == null)
                glowShader = FindGlowShader();
            if (glowShader != null)
                s_cachedGlowShader = glowShader;
        }

        private static Shader FindGlowShader()
        {
            return Shader.Find("HDRP/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color");
        }

        private static Shader ResolveGlowShader()
        {
            if (s_cachedGlowShader != null)
                return s_cachedGlowShader;
            s_cachedGlowShader = FindGlowShader();
            return s_cachedGlowShader;
        }

        public void SetActive(bool active)
        {
            EnsurePool();
            if (poolRoot != null)
                poolRoot.gameObject.SetActive(active);

            if (!active)
                Clear();
        }

        public void UpdateHighlights(IReadOnlyList<OpticsScanTarget> targets, Camera viewCamera)
        {
            // Outlines are driven by OutlineController; keep pool dormant.
            Clear();
        }

        public void Clear()
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].Root != null)
                    pool[i].Root.gameObject.SetActive(false);
            }
        }

        private void EnsurePool()
        {
            if (poolBuilt)
                return;

            poolRoot = new GameObject("ScannerGlowPool").transform;
            poolRoot.SetParent(transform, false);
            glowMaterial = CreateGlowMaterial();
            poolBuilt = true;
        }

        private void EnsurePoolSize(int required)
        {
            while (pool.Count < required)
            {
                GameObject root = GameObject.CreatePrimitive(PrimitiveType.Quad);
                root.name = $"ScannerGlow_{pool.Count}";
                root.transform.SetParent(poolRoot, false);
                Destroy(root.GetComponent<Collider>());

                MeshRenderer renderer = root.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = glowMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;

                pool.Add(new GlowEntry { Root = root.transform, Renderer = renderer });
            }
        }

        private static Material CreateGlowMaterial()
        {
            Shader shader = ResolveGlowShader();

            Color glow = new Color(0.3f, 0.95f, 0.75f, 0.75f);
            Material material = new Material(shader)
            {
                name = "ScannerGlowBillboard",
                color = glow
            };
            if (material.HasProperty("_UnlitColor"))
                material.SetColor("_UnlitColor", glow);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", glow);

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.renderQueue = 3000;
            }

            return material;
        }

        private void OnDestroy()
        {
            if (glowMaterial != null)
                Destroy(glowMaterial);
        }

        private struct GlowEntry
        {
            public Transform Root;
            public MeshRenderer Renderer;
        }
    }
}

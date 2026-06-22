using System.Collections.Generic;
using Project.UI;
using UnityEngine;

namespace Project.Interaction
{
    /// <summary>
    /// World-space glow billboards for scanner POI highlights.
    /// </summary>
    public class ScannerWorldHighlight : MonoBehaviour
    {
        private const int MaxHighlights = 24;

        private readonly List<GlowEntry> pool = new List<GlowEntry>(MaxHighlights);
        private Material glowMaterial;
        private Transform poolRoot;
        private bool poolBuilt;

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
            EnsurePool();
            if (poolRoot == null || !poolRoot.gameObject.activeSelf)
                return;

            int count = Mathf.Min(targets.Count, MaxHighlights);
            EnsurePoolSize(count);

            for (int i = 0; i < pool.Count; i++)
            {
                GlowEntry entry = pool[i];
                if (i >= count)
                {
                    entry.Root.gameObject.SetActive(false);
                    continue;
                }

                OpticsScanTarget target = targets[i];
                entry.Root.gameObject.SetActive(true);
                entry.Root.position = target.WorldPosition + Vector3.up * 0.35f;

                if (viewCamera != null)
                    entry.Root.rotation = Quaternion.LookRotation(entry.Root.position - viewCamera.transform.position);

                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 3.5f + i * 0.4f);
                Color glow = target.MarkerColor * pulse;
                glow.a = 0.55f + 0.25f * pulse;
                entry.Renderer.material.color = glow;

                float scale = 0.55f + 0.12f * pulse;
                entry.Root.localScale = new Vector3(scale, scale, scale);
            }
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
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            Material material = new Material(shader)
            {
                name = "ScannerGlowBillboard",
                color = new Color(0.3f, 0.95f, 0.75f, 0.75f)
            };

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

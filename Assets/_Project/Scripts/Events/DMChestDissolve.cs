using System.Collections.Generic;
using UnityEngine;

namespace Project.Events
{
    /// <summary>
    /// Chest dissolve (loot plan 7.4). Swaps every renderer under the chest (body sub-meshes and lid) to one
    /// dissolve material instance per renderer, copying its albedo (<c>_BaseColorMap</c> / <c>_BaseMap</c> / <c>_MainTex</c>)
    /// into <c>_BaseMap</c>, and drives <c>_DissolveAmount</c>. Shader / material come from
    /// <see cref="DMLootChestProfile"/> serialized refs (no Shader.Find, safe in player builds).
    /// Plain class ticked by <see cref="DMLootChestRuntime"/>: no per-chest Update. <see cref="Restore"/> puts the
    /// original materials and colliders back (used at Gone and by a later load / New Game restore).
    /// </summary>
    public sealed class DMChestDissolve
    {
        private static readonly int DissolveAmountId = Shader.PropertyToID("_DissolveAmount");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int EdgeWidthId = Shader.PropertyToID("_DissolveEdgeWidth");
        private static readonly int EdgeColorId = Shader.PropertyToID("_DissolveEdgeColor");
        private static readonly string[] AlbedoProps = { "_BaseColorMap", "_BaseMap", "_MainTex", "_UnlitColorMap" };

        private readonly Transform root;
        private readonly List<Renderer> renderers = new List<Renderer>(8);
        private readonly List<Material[]> originalMaterials = new List<Material[]>(8);
        private readonly List<Material> instances = new List<Material>(8);
        private readonly List<Collider> disabledColliders = new List<Collider>(8);
        private float duration;
        private float elapsed;

        public bool IsRunning { get; private set; }
        public bool IsComplete => IsRunning && elapsed >= duration;

        public DMChestDissolve(Transform chestRoot)
        {
            root = chestRoot;
        }

        /// <summary>Starts the dissolve; colliders switch off at once (7.4).</summary>
        public void Begin(float seconds)
        {
            if (IsRunning || root == null)
                return;

            IsRunning = true;
            elapsed = 0f;
            duration = Mathf.Max(0.01f, seconds);
            DisableColliders();
            SwapMaterials();
        }

        /// <summary>Advances the dissolve; true once fully dissolved.</summary>
        public bool Tick(float deltaTime)
        {
            if (!IsRunning)
                return false;

            elapsed += deltaTime;
            float amount = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i] != null)
                    instances[i].SetFloat(DissolveAmountId, amount);
            }

            return elapsed >= duration;
        }

        /// <summary>Puts original materials and colliders back and frees the instances.</summary>
        public void Restore()
        {
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                    renderers[i].sharedMaterials = originalMaterials[i];
            }

            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i] != null)
                    Object.Destroy(instances[i]);
            }

            for (int i = 0; i < disabledColliders.Count; i++)
            {
                if (disabledColliders[i] != null)
                    disabledColliders[i].enabled = true;
            }

            renderers.Clear();
            originalMaterials.Clear();
            instances.Clear();
            disabledColliders.Clear();
            IsRunning = false;
            elapsed = 0f;
        }

        private void DisableColliders()
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(false);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider c = colliders[i];
                if (c == null || !c.enabled)
                    continue;
                c.enabled = false;
                disabledColliders.Add(c);
            }
        }

        private void SwapMaterials()
        {
            DMLootChestProfile profile = DMLootChestProfile.Live;
            Material template = profile != null ? profile.dissolveMaterial : null;
            Shader shader = template != null ? template.shader : profile != null ? profile.dissolveShader : null;
            if (shader == null)
            {
                Debug.LogWarning("[DMChestDissolve] DM_LootChestProfile has no dissolve shader; chest hides without a dissolve.");
                return;
            }

            float edgeWidth = profile != null ? profile.dissolveEdgeWidth : 0.06f;
            Color edgeColor = profile != null ? profile.dissolveEdgeColor : new Color(0.831f, 0.627f, 0.09f, 1f);

            Renderer[] found = root.GetComponentsInChildren<Renderer>(false);
            for (int i = 0; i < found.Length; i++)
            {
                Renderer r = found[i];
                if (r == null || !r.enabled || !(r is MeshRenderer || r is SkinnedMeshRenderer))
                    continue;

                Material[] original = r.sharedMaterials;
                Material source = original != null && original.Length > 0 ? original[0] : null;
                Material instance = template != null ? new Material(template) : new Material(shader);
                instance.name = "DM_ChestDissolve (Runtime)";
                CopyAlbedo(source, instance);
                instance.SetFloat(EdgeWidthId, edgeWidth);
                instance.SetColor(EdgeColorId, edgeColor);
                instance.SetFloat(DissolveAmountId, 0f);

                int count = Mathf.Max(1, original != null ? original.Length : 1);
                Material[] replaced = new Material[count];
                for (int s = 0; s < count; s++)
                    replaced[s] = instance;

                renderers.Add(r);
                originalMaterials.Add(original);
                instances.Add(instance);
                r.sharedMaterials = replaced;
            }
        }

        private static void CopyAlbedo(Material source, Material target)
        {
            Color color = Color.white;
            if (source != null)
            {
                if (source.HasProperty(BaseColorId))
                    color = source.GetColor(BaseColorId);
                else if (source.HasProperty("_Color"))
                    color = source.color;

                for (int i = 0; i < AlbedoProps.Length; i++)
                {
                    string prop = AlbedoProps[i];
                    if (!source.HasProperty(prop))
                        continue;
                    Texture texture = source.GetTexture(prop);
                    if (texture == null)
                        continue;
                    target.SetTexture(BaseMapId, texture);
                    target.SetTextureScale(BaseMapId, source.GetTextureScale(prop));
                    target.SetTextureOffset(BaseMapId, source.GetTextureOffset(prop));
                    break;
                }
            }

            target.SetColor(BaseColorId, color);
        }
    }
}

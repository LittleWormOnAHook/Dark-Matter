using System;
using UnityEngine;

namespace Project.Rendering
{
    /// <summary>
    /// Clones shared mesh materials under this hierarchy at runtime so emission/tint/dissolve
    /// drivers never mutate project .mat assets. Safe to call repeatedly — skips slots that
    /// are already instanced.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-850)]
    [AddComponentMenu("Dark Matter/Rendering/DMI Runtime Hierarchy Material Instancer")]
    public sealed class DMRuntimeHierarchyMaterialInstancer : MonoBehaviour
    {
        [SerializeField] private bool includeInactive = true;
        [SerializeField] private bool skipParticleRenderers = true;

        private bool _applied;

        /// <summary>Call after <c>Instantiate</c> when the object is not covered by scene load sweep.</summary>
        public static void NotifySpawned(GameObject root)
        {
            if (root == null || !Application.isPlaying)
                return;

            InstanceHierarchyMaterials(root.transform, includeInactive: true, skipParticleRenderers: true);
        }

        public static void EnsureOn(GameObject root, bool includeInactiveRenderers = true)
        {
            if (root == null)
                return;

            DMRuntimeHierarchyMaterialInstancer instancer = root.GetComponent<DMRuntimeHierarchyMaterialInstancer>();
            if (instancer == null)
                instancer = root.AddComponent<DMRuntimeHierarchyMaterialInstancer>();

            instancer.includeInactive = includeInactiveRenderers;
            instancer.ApplyIfNeeded(force: true);
        }

        private void Awake()
        {
            ApplyIfNeeded(force: false);
        }

        public void ApplyIfNeeded(bool force)
        {
            if (_applied && !force)
                return;

            int instancedSlots = InstanceHierarchyMaterials(transform, includeInactive, skipParticleRenderers);
            _applied = instancedSlots > 0 || _applied;
        }

        public static int InstanceHierarchyMaterials(
            Transform root,
            bool includeInactive,
            bool skipParticleRenderers)
        {
            if (root == null)
                return 0;

            int instancedSlots = 0;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactive);
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (renderer == null
                    || DMRuntimeMaterialInstancingPolicy.ShouldSkipRenderer(renderer, skipParticleRenderers))
                    continue;

                instancedSlots += InstanceRendererMaterials(renderer);
            }

            return instancedSlots;
        }

        public static int InstanceRendererMaterials(Renderer renderer)
        {
            if (renderer == null)
                return 0;

            Material[] shared = renderer.sharedMaterials;
            if (shared == null || shared.Length == 0)
                return 0;

            Material[] resolved = new Material[shared.Length];
            int instanced = 0;
            bool changed = false;

            for (int i = 0; i < shared.Length; i++)
            {
                Material source = shared[i];
                if (source == null)
                {
                    resolved[i] = null;
                    continue;
                }

                if (IsAlreadyInstanced(source))
                {
                    resolved[i] = source;
                    continue;
                }

                resolved[i] = new Material(source)
                {
                    name = source.name + " (Instance)"
                };
                instanced++;
                changed = true;
            }

            if (changed)
                renderer.sharedMaterials = resolved;

            return instanced;
        }

        public static bool IsAlreadyInstanced(Material material)
        {
            if (material == null)
                return false;

            if (material.name.IndexOf("(Instance)", StringComparison.Ordinal) >= 0)
                return true;

#if UNITY_EDITOR
            // Edit-mode preview: only clone assets; leave other runtime instances alone.
            if (!Application.isPlaying)
                return !UnityEditor.AssetDatabase.IsMainAsset(material)
                       && !UnityEditor.AssetDatabase.IsSubAsset(material);
#endif
            return false;
        }

        public static bool IsSharedProjectMaterial(Material material)
        {
            if (material == null || IsAlreadyInstanced(material))
                return false;

#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.Contains(material);
#else
            return false;
#endif
        }
    }
}

using System;
using GenesisPCG.RockCreation;
using UnityEngine;

namespace Project.World.ProcPlacement
{
    /// <summary>
    /// Edit-mode procedural object. The scene only stores the recipe + seed; the mesh is rebuilt on load.
    /// Copy/paste or Ctrl+D rolls a new seed (see DmProcPasteWatcher) unless Lock Seed is on.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("Dark Matter Genesis/World/Proc Placer")]
    public sealed class DmProcPlacer : MonoBehaviour, IPcgSeeded
    {
        [SerializeField] private DmProcRecipe recipe;
        [SerializeField] private int seed = 1;
        [SerializeField, Tooltip("When on, copies keep this exact shape instead of rolling a new one.")]
        private bool lockSeed;
        [SerializeField, HideInInspector] private long placementStamp;

        [NonSerialized] private Mesh generatedMesh;
        [NonSerialized] private int builtHash;

        public DmProcRecipe Recipe => recipe;
        public int Seed => seed;
        public bool LockSeed => lockSeed;
        public long PlacementStamp => placementStamp;
        public Mesh GeneratedMesh => generatedMesh;

        /// <summary>Noise rocks have their pivot on the flattened base; they use the default snap rules.</summary>
        public PcgSurfaceSnapSettings SnapSettings => PcgSurfaceSnapSettings.Default;

        public void SetRecipe(DmProcRecipe value)
        {
            recipe = value;
            Rebuild();
        }

        public void SetSeed(int value)
        {
            seed = value;
            Rebuild();
        }

        /// <summary>Gives this object its own identity so it no longer counts as a twin of its source.</summary>
        public void NewPlacementStamp() => placementStamp = MakeStamp();

        private void OnEnable()
        {
            if (placementStamp == 0)
                placementStamp = MakeStamp();
            // Keep the edit-time generated mesh out of the GPU Resident Drawer (avoids invalid BatchDrawCommand errors).
            PcgRenderUtil.DisallowGpuDriven(gameObject);
            PcgSeededRegistry.Register(this);
            DmProcRecipe.Changed += OnRecipeChanged;
            Rebuild();
        }

        private void OnDisable()
        {
            DmProcRecipe.Changed -= OnRecipeChanged;
        }

        private void OnDestroy()
        {
            PcgSeededRegistry.Unregister(this);
            ReleaseMesh();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Changing MeshFilter/colliders inside OnValidate is not allowed; defer one tick.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && isActiveAndEnabled)
                    RebuildIfChanged();
            };
        }
#endif

        private void OnRecipeChanged(DmProcRecipe changed)
        {
            if (changed == recipe && this != null && isActiveAndEnabled)
                Rebuild();
        }

        public void RebuildIfChanged()
        {
            if (generatedMesh == null || ComputeHash() != builtHash)
                Rebuild();
        }

        public void Rebuild()
        {
            if (recipe == null)
                return;

            // One Mesh per placer for its whole lifetime; the generator Clear()s and refills it.
            if (generatedMesh == null)
            {
                generatedMesh = new Mesh
                {
                    name = "DmProcRock (generated)",
                    hideFlags = HideFlags.DontSave,
                };
            }

            switch (recipe.generator)
            {
                default:
                    DmNoiseRockGenerator.Build(generatedMesh, recipe, seed);
                    break;
            }

            var mf = GetComponent<MeshFilter>();
            if (mf.sharedMesh != generatedMesh)
                mf.sharedMesh = generatedMesh;

            var mr = GetComponent<MeshRenderer>();
            if (recipe.material != null && mr.sharedMaterial != recipe.material)
                mr.sharedMaterial = recipe.material;

            var mc = GetComponent<MeshCollider>();
            if (recipe.addMeshCollider)
            {
                if (mc == null)
                    mc = gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = null; // force the physics shape to rebake
                mc.sharedMesh = generatedMesh;
            }

            builtHash = ComputeHash();
        }

        private int ComputeHash()
        {
            unchecked
            {
                int h = seed;
                h = h * 31 + (recipe != null ? recipe.GetEntityId().GetHashCode() : 0);
                h = h * 31 + (recipe != null ? recipe.Version : 0);
                return h;
            }
        }

        private void ReleaseMesh()
        {
            if (generatedMesh == null)
                return;
            if (Application.isPlaying)
                Destroy(generatedMesh);
            else
                DestroyImmediate(generatedMesh);
            generatedMesh = null;
        }

        private static long MakeStamp()
        {
            long s = Guid.NewGuid().GetHashCode();
            s = (s << 32) ^ DateTime.UtcNow.Ticks;
            return s == 0 ? 1 : s;
        }
    }
}

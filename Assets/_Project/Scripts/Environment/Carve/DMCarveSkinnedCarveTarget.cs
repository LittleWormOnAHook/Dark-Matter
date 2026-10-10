using UnityEngine;

namespace Project.SurfaceCarve
{
    /// <summary>
    /// Lets Surface Carve / Carve Experiment ammo deform a <see cref="SkinnedMeshRenderer"/>.
    /// Carve operates on a baked <see cref="MeshFilter"/> child (the visible skinned mesh alone is not carvable).
    /// Requires the source mesh import to have <b>Read/Write</b> enabled.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark Matter/Environment/Carvable Skinned Mesh Target")]
    public sealed class DMCarveSkinnedCarveTarget : MonoBehaviour
    {
        private const string BakeChildName = "DM_CarveBakeSurface";

        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [Tooltip("When true, hide the SkinnedMeshRenderer after the first successful carve so holes stay visible on the baked mesh.")]
        [SerializeField] private bool hideSkinnedMeshAfterFirstCarve = true;
        [Tooltip("Rebuild the bake from the current pose before each carve (needed while animating).")]
        [SerializeField] private bool rebakePoseBeforeEachCarve = true;

        private Transform bakeRoot;
        private MeshFilter bakeFilter;
        private MeshRenderer bakeRenderer;
        private DMCarvable carvable;
        private Mesh bakedWorkingMesh;
        private bool hideSkinnedApplied;

        public SkinnedMeshRenderer SkinnedRenderer => skinnedMeshRenderer;

        private void Awake()
        {
            if (skinnedMeshRenderer == null)
                skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>(true);

            EnsureBakeHierarchy();
        }

        /// <summary>Used by <see cref="DMCarvable.ResolveForImpact"/>.</summary>
        public bool TryResolveCarvable(out DMCarvable target)
        {
            target = null;
            if (skinnedMeshRenderer == null)
                skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skinnedMeshRenderer == null)
                return false;

            if (!EnsureBakeHierarchy())
                return false;

            if (rebakePoseBeforeEachCarve || bakedWorkingMesh == null)
                SyncBakeFromSkinned();

            target = carvable;
            return target != null;
        }

        private bool EnsureBakeHierarchy()
        {
            if (bakeRoot != null && bakeFilter != null && carvable != null)
                return true;

            bakeRoot = transform.Find(BakeChildName);
            if (bakeRoot == null)
            {
                GameObject go = new GameObject(BakeChildName);
                bakeRoot = go.transform;
                bakeRoot.SetParent(transform, false);
            }

            bakeFilter = bakeRoot.GetComponent<MeshFilter>();
            if (bakeFilter == null)
                bakeFilter = bakeRoot.gameObject.AddComponent<MeshFilter>();

            bakeRenderer = bakeRoot.GetComponent<MeshRenderer>();
            if (bakeRenderer == null)
                bakeRenderer = bakeRoot.gameObject.AddComponent<MeshRenderer>();

            carvable = bakeRoot.GetComponent<DMCarvable>();
            if (carvable == null)
                carvable = bakeRoot.gameObject.AddComponent<DMCarvable>();

            bakeRoot.gameObject.layer = skinnedMeshRenderer.gameObject.layer;
            return skinnedMeshRenderer != null;
        }

        private void SyncBakeFromSkinned()
        {
            if (skinnedMeshRenderer == null || bakeFilter == null || bakeRenderer == null)
                return;

            if (bakedWorkingMesh == null)
            {
                bakedWorkingMesh = new Mesh();
                bakedWorkingMesh.name = skinnedMeshRenderer.sharedMesh != null
                    ? skinnedMeshRenderer.sharedMesh.name + "_CarveBake"
                    : "CarveBake";
                bakedWorkingMesh.MarkDynamic();
            }

            skinnedMeshRenderer.BakeMesh(bakedWorkingMesh, false);
            bakeFilter.sharedMesh = bakedWorkingMesh;
            bakeRenderer.sharedMaterials = skinnedMeshRenderer.sharedMaterials;
            bakeRenderer.enabled = true;

            SyncMeshColliderFromSkinned();
        }

        private void SyncMeshColliderFromSkinned()
        {
            MeshCollider onSkinned = skinnedMeshRenderer.GetComponent<MeshCollider>();
            if (onSkinned == null)
                return;

            MeshCollider onBake = bakeRoot.GetComponent<MeshCollider>();
            if (onBake == null)
                onBake = bakeRoot.gameObject.AddComponent<MeshCollider>();

            onBake.sharedMesh = bakedWorkingMesh;
            onBake.convex = false;
            onSkinned.enabled = false;
        }

        internal void NotifyCarveSucceeded()
        {
            if (!hideSkinnedMeshAfterFirstCarve || hideSkinnedApplied || skinnedMeshRenderer == null)
                return;

            skinnedMeshRenderer.enabled = false;
            hideSkinnedApplied = true;
        }

        private void OnValidate()
        {
            if (skinnedMeshRenderer == null)
                skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>(true);
        }
    }
}

#region License
/*
MIT License

Copyright(c) 2017-2020 Mattias Edlund

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityMeshSimplifierGaia
{
    /// <summary>
    /// A LOD (level of detail) generator helper.
    /// </summary>
    [AddComponentMenu("Rendering/LOD Generator Helper")]
    public sealed class LODGeneratorHelper : MonoBehaviour
    {
        #region Fields
        [SerializeField, Tooltip("The fade mode used by the created LOD group.")]
        private LODFadeMode fadeMode = LODFadeMode.None;
        [SerializeField, Tooltip("If the cross-fading should be animated by time.")]
        private bool animateCrossFading = false;

        [SerializeField, Tooltip("If the renderers under this game object and any children should be automatically collected.")]
        private bool autoCollectRenderers = true;

        [SerializeField, Tooltip("The simplification options.")]
        private SimplificationOptions simplificationOptions = SimplificationOptions.Default;

        [SerializeField, Tooltip("The path within the assets directory to save the generated assets. Leave this empty to use the default path.")]
        private string saveAssetsPath = string.Empty;

        [SerializeField, Tooltip("The LOD levels.")]
        private LODLevel[] levels = null;

        [SerializeField]
        private bool isGenerated = false;

        [Header("LOD 0 mesh collider (generation)")]
        [SerializeField, Tooltip("When LODs are generated (or Setup is run), add/update MeshColliders on Level00 mesh renderers.")]
        private bool addLod0MeshColliderOnGenerate = true;

        [SerializeField, Tooltip("Use a convex hull on the LOD 0 mesh collider. Off = non-convex mesh (static geometry only).")]
        private bool lod0MeshColliderConvex = true;

        [Header("Mesh collider (runtime)")]
        [SerializeField, Tooltip("Play Mode only. Culls only colliders listed below (or on this same GameObject if the list is empty).")]
        private bool cullMeshCollidersWhenNotLod0 = false;

        [SerializeField, Tooltip("MeshColliders managed by this LOD helper only. Leave empty to auto-use MeshCollider(s) under _UMS_LODs_/Level00, then on this GameObject.")]
        private MeshCollider[] managedMeshColliders;

        [SerializeField, Tooltip("Gameplay camera for collider LOD tests. When unset, uses Main/Game cameras (not Scene View).")]
        private Camera lodReferenceCamera;

        [SerializeField, Min(0.02f), Tooltip("Seconds between LOD / collider checks (reduces cost for many instances).")]
        private float colliderLodPollInterval = 0.1f;

        private LODGroup m_LodGroup;
        private MeshCollider[] m_MeshColliders;
        private Mesh[] m_ColliderSourceMeshes;
        private int m_LastAppliedLodIndex = int.MinValue;
        private float m_NextColliderPollTime;
        private Mesh m_CachedLod0Mesh;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the fade mode used by the created LOD group.
        /// </summary>
        public LODFadeMode FadeMode
        {
            get { return fadeMode; }
            set { fadeMode = value; }
        }

        /// <summary>
        /// Gets or sets if the cross-fading should be animated by time. The animation duration
        /// is specified globally as crossFadeAnimationDuration.
        /// </summary>
        public bool AnimateCrossFading
        {
            get { return animateCrossFading; }
            set { animateCrossFading = value; }
        }

        /// <summary>
        /// Gets or sets if the renderers under this game object and any children should be automatically collected.
        /// </summary>
        public bool AutoCollectRenderers
        {
            get { return autoCollectRenderers; }
            set { autoCollectRenderers = value; }
        }

        /// <summary>
        /// Gets or sets the simplification options.
        /// </summary>
        public SimplificationOptions SimplificationOptions
        {
            get { return simplificationOptions; }
            set { simplificationOptions = value; }
        }

        /// <summary>
        /// Gets or sets the path within the project to save the generated assets.
        /// Leave this empty to use the default path.
        /// </summary>
        public string SaveAssetsPath
        {
            get { return saveAssetsPath; }
            set { saveAssetsPath = value; }
        }

        /// <summary>
        /// Gets or sets the LOD levels for this generator.
        /// </summary>
        public LODLevel[] Levels
        {
            get { return levels; }
            set { levels = value; }
        }

        /// <summary>
        /// Gets if the LODs have been generated.
        /// </summary>
        public bool IsGenerated
        {
            get { return isGenerated; }
        }

        /// <summary>When true, mesh colliders are disabled unless LOD 0 (Level00) is active.</summary>
        public bool CullMeshCollidersWhenNotLod0
        {
            get { return cullMeshCollidersWhenNotLod0; }
            set { cullMeshCollidersWhenNotLod0 = value; }
        }

        public bool AddLod0MeshColliderOnGenerate
        {
            get { return addLod0MeshColliderOnGenerate; }
            set { addLod0MeshColliderOnGenerate = value; }
        }

        public bool Lod0MeshColliderConvex
        {
            get { return lod0MeshColliderConvex; }
            set { lod0MeshColliderConvex = value; }
        }

        /// <summary>
        /// Adds or updates MeshColliders on LOD 0 meshes, applies convex setting, then wires runtime culling.
        /// </summary>
        public void ApplyLod0MeshColliderSetup(bool enableCullWhenWired = true)
        {
            if (addLod0MeshColliderOnGenerate)
            {
                foreach (GameObject target in EnumerateLod0MeshColliderTargets())
                {
                    EnsureMeshColliderOnMeshObject(target, lod0MeshColliderConvex);
                }
            }
            else
            {
                SyncLod0MeshColliderConvexOnExisting();
            }

            WireManagedMeshCollidersFromLod0(enableCullWhenWired);
        }

        [ContextMenu("Setup LOD0 Mesh Collider")]
        private void ApplyLod0MeshColliderSetupMenu()
        {
            ApplyLod0MeshColliderSetup();
        }

        /// <summary>
        /// Finds MeshColliders on LOD 0 (Level00) and registers them for runtime culling.
        /// Called automatically when LODs are generated; use on existing LOD setups via context menu.
        /// </summary>
        public void WireManagedMeshCollidersFromLod0(bool enableCullWhenWired = true)
        {
            List<MeshCollider> colliders = DiscoverManagedMeshColliders(includeExplicitList: false);
            if (colliders.Count == 0)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.RecordObject(this, "Wire LOD0 mesh colliders");
#endif
            managedMeshColliders = colliders.ToArray();
            if (enableCullWhenWired)
                cullMeshCollidersWhenNotLod0 = true;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(this);
#endif
        }
        [ContextMenu("Wire LOD0 Mesh Colliders")]
        private void WireManagedMeshCollidersFromLod0Menu()
        {
            WireManagedMeshCollidersFromLod0();
        }
        #endregion

        #region Unity Events
        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            CacheLodAndColliders();
            m_LastAppliedLodIndex = int.MinValue;
            m_NextColliderPollTime = 0f;
            StartCoroutine(ApplyColliderStateAfterLodGroupUpdate());
        }

        private System.Collections.IEnumerator ApplyColliderStateAfterLodGroupUpdate()
        {
            yield return null;
            ApplyColliderStateForCurrentLod(force: true);
        }

        private void OnDisable()
        {
            if (!Application.isPlaying || !cullMeshCollidersWhenNotLod0)
                return;

            RestoreMeshCollidersEnabled();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || !cullMeshCollidersWhenNotLod0)
                return;

            if (m_MeshColliders == null || m_MeshColliders.Length == 0)
                CacheLodAndColliders();

            if (m_LodGroup == null)
                return;

            ApplyColliderStateForCurrentLod(force: false);
        }

        private void Reset()
        {
            fadeMode = LODFadeMode.None;
            animateCrossFading = false;
            autoCollectRenderers = true;
            simplificationOptions = SimplificationOptions.Default;

            levels = new LODLevel[]
            {
                new LODLevel(0.5f, 1f)
                {
                    CombineMeshes = false,
                    CombineSubMeshes = false,
                    SkinQuality = SkinQuality.Auto,
                    ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                    ReceiveShadows = true,
                    SkinnedMotionVectors = true,
                    LightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes,
                    ReflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes,
                },
                new LODLevel(0.17f, 0.65f)
                {
                    CombineMeshes = true,
                    CombineSubMeshes = false,
                    SkinQuality = SkinQuality.Auto,
                    ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                    ReceiveShadows = true,
                    SkinnedMotionVectors = true,
                    LightProbeUsage = UnityEngine.Rendering.LightProbeUsage.BlendProbes,
                    ReflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Simple
                },
                new LODLevel(0.02f, 0.4225f)
                {
                    CombineMeshes = true,
                    CombineSubMeshes = true,
                    SkinQuality = SkinQuality.Bone2,
                    ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
                    ReceiveShadows = false,
                    SkinnedMotionVectors = false,
                    LightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off,
                    ReflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off
                }
            };
        }
        #endregion

        #region Mesh collider LOD culling
        private IEnumerable<GameObject> EnumerateLod0MeshColliderTargets()
        {
            LODGroup lodGroup = GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                LOD[] lods = lodGroup.GetLODs();
                if (lods != null && lods.Length > 0 && lods[0].renderers != null)
                {
                    Renderer[] renderers = lods[0].renderers;
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        Renderer renderer = renderers[i];
                        if (renderer == null)
                            continue;

                        MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                        if (meshFilter != null && meshFilter.sharedMesh != null)
                            yield return meshFilter.gameObject;
                    }

                    yield break;
                }
            }

            Transform level00 = GetLevel00Transform();
            if (level00 == null)
                yield break;

            MeshFilter[] meshFilters = level00.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                if (meshFilter != null && meshFilter.sharedMesh != null)
                    yield return meshFilter.gameObject;
            }
        }

        private MeshCollider EnsureMeshColliderOnMeshObject(GameObject meshObject, bool convex)
        {
            if (meshObject == null)
                return null;

            MeshFilter meshFilter = meshObject.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                return null;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.RecordObject(meshObject, "LOD0 mesh collider");
#endif
            MeshCollider meshCollider = meshObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
#if UNITY_EDITOR
                meshCollider = Undo.AddComponent<MeshCollider>(meshObject);
#else
                meshCollider = meshObject.AddComponent<MeshCollider>();
#endif
            }

            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = convex;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(meshObject);
                EditorUtility.SetDirty(meshCollider);
            }
#endif
            return meshCollider;
        }

        private void SyncLod0MeshColliderConvexOnExisting()
        {
            Transform level00 = GetLevel00Transform();
            if (level00 == null)
                return;

            MeshCollider[] colliders = level00.GetComponentsInChildren<MeshCollider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                MeshCollider meshCollider = colliders[i];
                if (meshCollider == null)
                    continue;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Undo.RecordObject(meshCollider, "LOD0 mesh collider convex");
#endif
                meshCollider.convex = lod0MeshColliderConvex;
            }
        }

        private void CacheLodAndColliders()
        {
            m_LodGroup = GetComponent<LODGroup>();
            m_CachedLod0Mesh = null;

            List<MeshCollider> colliders = DiscoverManagedMeshColliders(includeExplicitList: true);
            var sourceMeshes = new List<Mesh>(colliders.Count);
            for (int i = 0; i < colliders.Count; i++)
                sourceMeshes.Add(colliders[i].sharedMesh);

            m_MeshColliders = colliders.ToArray();
            m_ColliderSourceMeshes = sourceMeshes.ToArray();
        }

        private List<MeshCollider> DiscoverManagedMeshColliders(bool includeExplicitList)
        {
            var colliders = new List<MeshCollider>(4);

            if (includeExplicitList && managedMeshColliders != null && managedMeshColliders.Length > 0)
            {
                for (int i = 0; i < managedMeshColliders.Length; i++)
                {
                    MeshCollider mc = managedMeshColliders[i];
                    if (mc != null && IsColliderManagedByThisHelper(mc))
                        colliders.Add(mc);
                }

                if (colliders.Count > 0)
                    return colliders;
            }

            Transform level00 = GetLevel00Transform();
            if (level00 != null)
            {
                MeshCollider[] onLod0 = level00.GetComponentsInChildren<MeshCollider>(true);
                for (int i = 0; i < onLod0.Length; i++)
                {
                    MeshCollider mc = onLod0[i];
                    if (mc != null)
                        colliders.Add(mc);
                }
            }

            if (colliders.Count == 0)
            {
                MeshCollider[] onRoot = GetComponents<MeshCollider>();
                for (int i = 0; i < onRoot.Length; i++)
                {
                    MeshCollider mc = onRoot[i];
                    if (mc != null)
                        colliders.Add(mc);
                }
            }

            return colliders;
        }

        private bool IsColliderManagedByThisHelper(MeshCollider mc)
        {
            if (mc == null)
                return false;

            if (mc.transform == transform)
                return true;

            Transform level00 = GetLevel00Transform();
            return level00 != null && mc.transform.IsChildOf(level00);
        }

        private Transform GetLevel00Transform()
        {
            Transform lodRoot = transform.Find(LODGenerator.LODParentGameObjectName);
            if (lodRoot == null)
                return null;

            return lodRoot.Find("Level00");
        }

        private bool IsLod0ActiveForColliders()
        {
            if (m_LodGroup == null)
                return true;

            LOD[] lods = m_LodGroup.GetLODs();
            if (lods == null || lods.Length == 0)
                return true;

            Camera cam = ResolveGameplayCamera();
            if (cam != null)
                return GetActiveLodIndexForCamera(cam) == 0;

            // No gameplay camera (e.g. headless) — follow what LODGroup enabled on renderers.
            return IsAnyRendererEnabled(lods[0].renderers);
        }

        /// <summary>
        /// Game / VR camera only — ignores Scene View so colliders cull when the player camera is far
        /// even if the editor Scene camera is close during Play Mode.
        /// </summary>
        private Camera ResolveGameplayCamera()
        {
            if (lodReferenceCamera != null && lodReferenceCamera.isActiveAndEnabled)
                return lodReferenceCamera;

            Camera main = Camera.main;
            if (main != null && main.isActiveAndEnabled && IsGameplayCamera(main))
                return main;

            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera cam = cameras[i];
                if (cam != null && cam.isActiveAndEnabled && IsGameplayCamera(cam))
                    return cam;
            }

            return null;
        }

        private static bool IsGameplayCamera(Camera camera)
        {
            switch (camera.cameraType)
            {
                case CameraType.Game:
                case CameraType.VR:
                    return true;
                default:
                    return false;
            }
        }

        private int GetActiveLodIndexForCamera(Camera cam)
        {
            LOD[] lods = m_LodGroup.GetLODs();
            if (lods == null || lods.Length == 0)
                return 0;

            float relativeHeight = CalculateScreenRelativeMetric(m_LodGroup, cam);
            for (int i = 0; i < lods.Length; i++)
            {
                if (relativeHeight >= lods[i].screenRelativeTransitionHeight)
                    return i;
            }

            return -1;
        }

        private static float CalculateScreenRelativeMetric(LODGroup group, Camera camera)
        {
            Vector3 worldReferencePoint = group.transform.TransformPoint(group.localReferencePoint);
            float distance = Vector3.Distance(worldReferencePoint, camera.transform.position);
            distance = Mathf.Max(distance - camera.nearClipPlane, 0.0001f);

            Vector3 scale = group.transform.lossyScale;
            float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            return (group.size * maxScale) / distance;
        }

        private static bool IsAnyRendererEnabled(Renderer[] renderers)
        {
            if (renderers == null)
                return false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer != null && renderer.enabled)
                    return true;
            }

            return false;
        }

        private Mesh ResolveLod0Mesh()
        {
            if (m_CachedLod0Mesh != null)
                return m_CachedLod0Mesh;

            Transform level00 = GetLevel00Transform();
            if (level00 == null)
                return null;

            MeshFilter meshFilter = level00.GetComponentInChildren<MeshFilter>(true);
            if (meshFilter != null)
                m_CachedLod0Mesh = meshFilter.sharedMesh;

            return m_CachedLod0Mesh;
        }

        private void ApplyColliderStateForCurrentLod(bool force)
        {
            if (!cullMeshCollidersWhenNotLod0 || m_MeshColliders == null || m_MeshColliders.Length == 0)
                return;

            bool lod0Active = IsLod0ActiveForColliders();
            int lodIndex = lod0Active ? 0 : -1;
            if (!force && lodIndex == m_LastAppliedLodIndex)
                return;

            m_LastAppliedLodIndex = lodIndex;

            if (lod0Active)
                EnableAndReinitializeMeshColliders();
            else
                DisableMeshColliders();
        }

        private void DisableMeshColliders()
        {
            for (int i = 0; i < m_MeshColliders.Length; i++)
            {
                MeshCollider mc = m_MeshColliders[i];
                if (mc != null)
                    mc.enabled = false;
            }
        }

        private void EnableAndReinitializeMeshColliders()
        {
            Mesh lod0Mesh = ResolveLod0Mesh();

            for (int i = 0; i < m_MeshColliders.Length; i++)
            {
                MeshCollider mc = m_MeshColliders[i];
                if (mc == null)
                    continue;

                Mesh targetMesh = lod0Mesh;
                if (targetMesh == null && m_ColliderSourceMeshes != null && i < m_ColliderSourceMeshes.Length)
                    targetMesh = m_ColliderSourceMeshes[i];

                if (targetMesh != null && mc.sharedMesh != targetMesh)
                    mc.sharedMesh = targetMesh;

                if (!mc.enabled)
                    mc.enabled = true;
            }
        }

        private void RestoreMeshCollidersEnabled()
        {
            if (m_MeshColliders == null)
                return;

            for (int i = 0; i < m_MeshColliders.Length; i++)
            {
                MeshCollider mc = m_MeshColliders[i];
                if (mc != null)
                    mc.enabled = true;
            }
        }
        #endregion
    }
}

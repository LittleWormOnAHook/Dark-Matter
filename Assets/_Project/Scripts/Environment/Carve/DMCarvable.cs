using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.SurfaceCarve
{
    /// <summary>
    /// Marks a mesh (rocks first) as carvable by the Surface Carve tool and by ammo impacts.
    /// Edit Mode carves write a "_carved" mesh copy (saved by the Carve Tool); Play Mode carves
    /// work on a throwaway runtime copy, so assets are never modified by gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [AddComponentMenu("Dark Matter/Environment/Carvable Surface")]
    public class DMCarvable : MonoBehaviour
    {
        [Tooltip("Original mesh before any carving (Reset restores it).")]
        [SerializeField] private Mesh sourceMesh;
        [Tooltip("Edit Mode carved copy. Saved as an asset by the Carve Tool.")]
        [SerializeField] private Mesh carvedMesh;
        [SerializeField] private int sourceSubmeshCount;
        [Tooltip("For every submesh: the original submesh it belongs to (cap submeshes point at their source).")]
        [SerializeField] private int[] submeshSource = new int[0];
        [Tooltip("For every source submesh: its cut-face submesh index (-1 none).")]
        [SerializeField] private int[] fractureSubmesh = new int[0];
        [Tooltip("For every source submesh: its dark rim submesh index (-1 none).")]
        [SerializeField] private int[] rimSubmesh = new int[0];

        [Header("Options")]
        [Tooltip("Rebuild the MeshCollider after carving so shots and the player hit the new surface.")]
        public bool updateCollider = true;
        [Tooltip("Allow broken-off chunks in Play Mode.")]
        public bool allowDebris = true;
        [Tooltip("Optional: cut-face material for every cut on this object (else Surface Damage settings / auto).")]
        public Material fractureMaterialOverride;
        [Tooltip("Optional: dark rim material for every cut on this object.")]
        public Material rimMaterialOverride;

        [NonSerialized] private Mesh runtimeMesh;
        [NonSerialized] private DMCarveMeshData data;
        [NonSerialized] private Mesh dataMesh;
        [NonSerialized] private bool colliderDirty;
        [NonSerialized] private float nextColliderTime;
        [NonSerialized] private bool warned;
        [NonSerialized] private Material lastFractureMaterial;

        private static readonly Dictionary<Material, Material> AutoFracture = new Dictionary<Material, Material>();
        private static readonly Dictionary<Material, Material> AutoRim = new Dictionary<Material, Material>();

        /// <summary>Editor hook: register objects created by an Edit Mode carve with Undo.</summary>
        public static Action<UnityEngine.Object> EditorCreatedObject;

        public Mesh SourceMesh => sourceMesh;
        public Mesh CarvedMesh => carvedMesh;
        public bool IsCarved => carvedMesh != null || runtimeMesh != null;
        public Material LastFractureMaterial => lastFractureMaterial;
        public int SourceSubmeshCount => sourceSubmeshCount;


        /// <summary>Find the carvable for a hit object, auto-adding one to matching rocks.</summary>
        public static DMCarvable ResolveForImpact(GameObject receiver, DMCarveSettings settings)
        {
            if (receiver == null)
                return null;

            DMCarvable c = receiver.GetComponent<DMCarvable>();
            if (c != null)
                return c;
            c = receiver.GetComponentInParent<DMCarvable>();
            if (c != null)
                return c;

            DMCarveSkinnedCarveTarget skinnedTarget = receiver.GetComponentInParent<DMCarveSkinnedCarveTarget>();
            if (skinnedTarget != null && skinnedTarget.TryResolveCarvable(out DMCarvable skinnedCarvable))
                return skinnedCarvable;

            if (settings == null || !settings.MatchesAutoCarvable(receiver))
                return null;

            MeshFilter mf = receiver.GetComponent<MeshFilter>();
            MeshRenderer mr = receiver.GetComponent<MeshRenderer>();
            if (mf == null || mr == null || mf.sharedMesh == null)
                return null;
            if (Application.isPlaying && (mr.isPartOfStaticBatch || !mf.sharedMesh.isReadable))
                return null;
            Rigidbody rb = receiver.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
                return null;

            return receiver.AddComponent<DMCarvable>();
        }

        /// <summary>Carve at a surface hit: cutter sinks along -normal by depth x radius.</summary>
        public bool CarveAtSurface(Vector3 point, Vector3 normal, DMSurfaceDamageSettings damage, int seed, bool spawnDebris, float radiusScale = 1f)
        {
            if (damage == null)
                return false;

            float r = Mathf.Max(0.01f, damage.radius * radiusScale);
            DMCarveCutter cutter = MakeSurfaceCutter(point, normal, r, damage.depth, damage.style, damage.roughness, seed);
            if (!Carve(cutter))
                return false;

            DMCarveSkinnedCarveTarget skinnedNotify = GetComponentInParent<DMCarveSkinnedCarveTarget>();
            skinnedNotify?.NotifyCarveSucceeded();

            if (spawnDebris && allowDebris && Application.isPlaying && damage.debrisCount > 0)
                DMCarveDebris.Spawn(cutter, normal, damage, lastFractureMaterial, gameObject.layer);
            return true;
        }

        public static DMCarveCutter MakeSurfaceCutter(Vector3 point, Vector3 normal, float radius, float depth, DMCarveStyle style, float roughness, int seed)
        {
            Vector3 n = normal.sqrMagnitude > 1e-8f ? normal.normalized : Vector3.up;
            Vector3 center = point - n * (radius * Mathf.Clamp01(depth));
            Vector3 up = Mathf.Abs(Vector3.Dot(n, Vector3.up)) > 0.95f ? Vector3.forward : Vector3.up;
            float spin = (Mathf.Abs(seed) % 3600) * 0.1f;
            Quaternion rot = Quaternion.LookRotation(-n, up) * Quaternion.AngleAxis(spin, Vector3.forward);
            return new DMCarveCutter(center, rot, radius, style, roughness, seed);
        }

        /// <summary>Carve with any cutter. Returns true when the mesh changed.</summary>
        public bool Carve(DMCarveCutter cutter)
        {
            if (cutter == null || !EnsureReady())
                return false;

            UnityEngine.Renderer r = GetComponent<UnityEngine.Renderer>();
            if (r != null && !cutter.OverlapsBounds(r.bounds))
                return false;

            DMCarveSettings settings = DMCarveSettings.Active;
            if (data.TriangleCount >= settings.maxTrianglesPerObject)
            {
                WarnOnce("reached the triangle limit (" + settings.maxTrianglesPerObject + "); raise it in Surface Damage settings.");
                return false;
            }

            DMCarveParams prm = DMCarveParams.FromSettings(settings, cutter.Radius);
            DMCarveResult res = DMCarveOps.Carve(data, transform.localToWorldMatrix, cutter, prm, ResolveCapTarget);
            if (!res.Changed)
                return false;

            int used = data.CountUsedVertices();
            if (data.VertexCount > 2000 && used < data.VertexCount * 0.7f)
                data.Compact();

            Mesh target = WorkingMesh;
            data.WriteTo(target);
            dataMesh = target;
            SyncMaterials();

            if (updateCollider)
            {
                if (Application.isPlaying)
                    colliderDirty = true;
                else
                    RefreshCollider();
            }

            return true;
        }

        private Mesh WorkingMesh => Application.isPlaying ? runtimeMesh : carvedMesh;

        public void RefreshCollider()
        {
            colliderDirty = false;
            MeshCollider mc = GetComponent<MeshCollider>();
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mc == null || mf == null || mf.sharedMesh == null)
                return;
            if (mc.convex)
                return;
            // Baked rocks collide with a simplified LOD mesh; swapping in the full carved render mesh
            // multiplied collider cost (e.g. 63k triangles re-cooked per carve). Keep a dedicated collider in Play.
            if (Application.isPlaying && mc.sharedMesh != null && mc.sharedMesh != mf.sharedMesh
                && DMCarveSettings.Active.keepDedicatedColliderInPlay)
                return;
            mc.sharedMesh = null;
            mc.sharedMesh = mf.sharedMesh;
        }

        /// <summary>Rebuild the collider now if a carve left it dirty, so a follow-up raycast sees the new surface.</summary>
        public void FlushCollider()
        {
            if (colliderDirty)
                RefreshCollider();
        }

        private void LateUpdate()
        {
            if (!colliderDirty || !Application.isPlaying)
                return;
            if (Time.unscaledTime < nextColliderTime)
                return;
            nextColliderTime = Time.unscaledTime + DMCarveSettings.Active.runtimeColliderRefresh;
            RefreshCollider();
        }

        /// <summary>Drop the cached editable copy (after Undo/Redo or external mesh edits).</summary>
        public void InvalidateCache()
        {
            data = null;
            dataMesh = null;
        }

        private bool EnsureReady()
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                WarnOnce("has no mesh to carve.");
                return false;
            }

            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (Application.isPlaying)
            {
                if (mr != null && mr.isPartOfStaticBatch)
                {
                    WarnOnce("is static batched; untick Batching Static on carvable rocks.");
                    return false;
                }

                if (runtimeMesh == null || mf.sharedMesh != runtimeMesh)
                {
                    Mesh baseMesh = mf.sharedMesh;
                    if (!baseMesh.isReadable)
                    {
                        WarnOnce("mesh '" + baseMesh.name + "' is not Read/Write enabled.");
                        return false;
                    }

                    if (sourceMesh == null)
                        sourceMesh = baseMesh;
                    InitSubmeshMaps(baseMesh);
                    runtimeMesh = Instantiate(baseMesh);
                    runtimeMesh.name = baseMesh.name + "_runtimeCarve";
                    runtimeMesh.MarkDynamic();
                    SwapMesh(baseMesh, runtimeMesh);
                    data = null;
                }
            }
            else
            {
                if (carvedMesh == null || mf.sharedMesh != carvedMesh)
                {
                    Mesh baseMesh = mf.sharedMesh;
                    if (carvedMesh == null || baseMesh != carvedMesh)
                    {
                        sourceMesh = baseMesh;
                        InitSubmeshMaps(baseMesh);
                    }

                    carvedMesh = Instantiate(baseMesh);
                    carvedMesh.name = baseMesh.name + "_carved";
                    EditorCreatedObject?.Invoke(carvedMesh);
                    SwapMesh(baseMesh, carvedMesh);
                    data = null;
                }
            }

            if (data == null || dataMesh != mf.sharedMesh)
            {
                if (!DMCarveMeshData.TryRead(mf.sharedMesh, out data, out string error))
                {
                    WarnOnce(error);
                    return false;
                }

                dataMesh = mf.sharedMesh;
                FixSubmeshMaps(data.Submeshes.Count);
            }

            return true;
        }

        private void SwapMesh(Mesh from, Mesh to)
        {
            GetComponent<MeshFilter>().sharedMesh = to;
            MeshCollider mc = GetComponent<MeshCollider>();
            if (mc != null && (mc.sharedMesh == from || mc.sharedMesh == null))
                mc.sharedMesh = to;
        }

        private void InitSubmeshMaps(Mesh baseMesh)
        {
            if (sourceSubmeshCount > 0 && submeshSource != null && submeshSource.Length == baseMesh.subMeshCount)
                return;

            sourceSubmeshCount = baseMesh.subMeshCount;
            submeshSource = new int[sourceSubmeshCount];
            fractureSubmesh = new int[sourceSubmeshCount];
            rimSubmesh = new int[sourceSubmeshCount];
            for (int i = 0; i < sourceSubmeshCount; i++)
            {
                submeshSource[i] = i;
                fractureSubmesh[i] = -1;
                rimSubmesh[i] = -1;
            }
        }

        private void FixSubmeshMaps(int submeshCount)
        {
            if (sourceSubmeshCount <= 0 || sourceSubmeshCount > submeshCount)
            {
                sourceSubmeshCount = submeshCount;
                submeshSource = null;
            }

            if (submeshSource == null || submeshSource.Length != submeshCount)
            {
                var src = new int[submeshCount];
                for (int i = 0; i < submeshCount; i++)
                    src[i] = submeshSource != null && i < submeshSource.Length ? submeshSource[i] : Mathf.Min(i, sourceSubmeshCount - 1);
                submeshSource = src;
            }

            if (fractureSubmesh == null || fractureSubmesh.Length != sourceSubmeshCount)
                fractureSubmesh = Resize(fractureSubmesh, sourceSubmeshCount);
            if (rimSubmesh == null || rimSubmesh.Length != sourceSubmeshCount)
                rimSubmesh = Resize(rimSubmesh, sourceSubmeshCount);
        }

        private static int[] Resize(int[] arr, int count)
        {
            var r = new int[count];
            for (int i = 0; i < count; i++)
                r[i] = arr != null && i < arr.Length ? arr[i] : -1;
            return r;
        }

        private DMCarveCapTarget ResolveCapTarget(int hitSubmesh)
        {
            int src = hitSubmesh >= 0 && hitSubmesh < submeshSource.Length ? submeshSource[hitSubmesh] : 0;
            src = Mathf.Clamp(src, 0, Mathf.Max(0, sourceSubmeshCount - 1));

            if (fractureSubmesh[src] < 0 || fractureSubmesh[src] >= data.Submeshes.Count)
            {
                fractureSubmesh[src] = data.AddSubmesh();
                AppendSource(src);
            }

            if (rimSubmesh[src] < 0 || rimSubmesh[src] >= data.Submeshes.Count)
            {
                rimSubmesh[src] = data.AddSubmesh();
                AppendSource(src);
            }

            return new DMCarveCapTarget { FractureSubmesh = fractureSubmesh[src], RimSubmesh = rimSubmesh[src] };
        }

        private void AppendSource(int src)
        {
            int n = submeshSource.Length;
            Array.Resize(ref submeshSource, n + 1);
            submeshSource[n] = src;
        }

        /// <summary>Make sure every cap submesh has its cut / rim material.</summary>
        private void SyncMaterials()
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr == null || data == null)
                return;

            Material[] current = mr.sharedMaterials;
            int count = data.Submeshes.Count;
            var mats = new Material[count];
            for (int i = 0; i < count; i++)
            {
                if (i < sourceSubmeshCount)
                {
                    mats[i] = i < current.Length ? current[i] : (current.Length > 0 ? current[current.Length - 1] : null);
                    continue;
                }

                if (i < current.Length && current[i] != null)
                {
                    mats[i] = current[i];
                    continue;
                }

                int src = i < submeshSource.Length ? submeshSource[i] : 0;
                Material source = src < current.Length ? current[src] : (current.Length > 0 ? current[0] : null);
                bool isRim = src < rimSubmesh.Length && rimSubmesh[src] == i;
                mats[i] = isRim ? ResolveRimMaterial(source) : ResolveFractureMaterial(source);
            }

            for (int s = 0; s < sourceSubmeshCount && s < fractureSubmesh.Length; s++)
            {
                int fi = fractureSubmesh[s];
                if (fi >= 0 && fi < mats.Length && mats[fi] != null)
                    lastFractureMaterial = mats[fi];
            }

            mr.sharedMaterials = mats;
        }

        public Material ResolveFractureMaterial(Material source)
        {
            if (fractureMaterialOverride != null)
                return fractureMaterialOverride;
            DMCarveSettings s = DMCarveSettings.Active;
            if (s.TryGetMaterialRow(source, out DMCarveMaterialRow row) && row.fracture != null)
                return row.fracture;
            return AutoMaterial(AutoFracture, source, s.fractureTint, "_CarveCut");
        }

        /// <summary>Cut-face material for a source material on an object with no carvable (settings row, else auto copy).</summary>
        public static Material FractureMaterialFor(Material source)
        {
            DMCarveSettings s = DMCarveSettings.Active;
            if (s.TryGetMaterialRow(source, out DMCarveMaterialRow row) && row.fracture != null)
                return row.fracture;
            return AutoMaterial(AutoFracture, source, s.fractureTint, "_CarveCut");
        }

        public Material ResolveRimMaterial(Material source)
        {
            if (rimMaterialOverride != null)
                return rimMaterialOverride;
            DMCarveSettings s = DMCarveSettings.Active;
            if (s.TryGetMaterialRow(source, out DMCarveMaterialRow row) && row.rim != null)
                return row.rim;
            return AutoMaterial(AutoRim, source, s.rimTint, "_CarveRim");
        }

        private static Material AutoMaterial(Dictionary<Material, Material> cache, Material source, Color tint, string suffix)
        {
            if (source == null)
                return null;
            if (cache.TryGetValue(source, out Material m) && m != null)
                return m;

            m = new Material(source) { name = source.name + suffix };
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", m.GetColor("_BaseColor") * tint);
            if (m.HasProperty("_Color"))
                m.SetColor("_Color", m.GetColor("_Color") * tint);
            if (m.HasProperty("_Smoothness"))
                m.SetFloat("_Smoothness", m.GetFloat("_Smoothness") * 0.6f);
            cache[source] = m;
            return m;
        }

        /// <summary>Edit Mode: restore the original mesh and materials.</summary>
        public void ResetCarve()
        {
            if (sourceMesh == null)
                return;

            Mesh current = GetComponent<MeshFilter>().sharedMesh;
            SwapMesh(current, sourceMesh);
            MeshCollider mc = GetComponent<MeshCollider>();
            if (mc != null && mc.sharedMesh == current)
                mc.sharedMesh = sourceMesh;

            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null && sourceSubmeshCount > 0)
            {
                Material[] mats = mr.sharedMaterials;
                if (mats.Length > sourceSubmeshCount)
                {
                    Array.Resize(ref mats, sourceSubmeshCount);
                    mr.sharedMaterials = mats;
                }
            }

            carvedMesh = null;
            runtimeMesh = null;
            submeshSource = new int[0];
            fractureSubmesh = new int[0];
            rimSubmesh = new int[0];
            sourceSubmeshCount = 0;
            InvalidateCache();
        }

        private void WarnOnce(string message)
        {
            if (warned)
                return;
            warned = true;
            Debug.LogWarning("[Surface Carve] '" + name + "' " + message, this);
        }

        private void OnDestroy()
        {
            if (runtimeMesh != null && Application.isPlaying)
                Destroy(runtimeMesh);
        }
    }
}

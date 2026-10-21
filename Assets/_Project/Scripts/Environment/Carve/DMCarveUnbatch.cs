using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Project.SurfaceCarve
{
    /// <summary>
    /// 2026-10-20x: lets ammo carve statically batched objects without touching the scene's static flags.
    /// A batched renderer has lost its own mesh (MeshFilter shows "Combined Mesh (root: scene)"), so the original is
    /// recovered from DMCarveStaticMeshCache (built in Edit Mode on entering Play) or from the object's MeshCollider.
    /// A readable copy is rendered by an unbatched child ("name (carve)"), the batched renderer is hidden, and the
    /// child gets the DMCarvable. Unreadable meshes get a runtime CPU copy where Unity allows it.
    /// Toggle: DMCarveSettings.carveUnbatchesStatic.
    /// </summary>
    public static class DMCarveUnbatch
    {
        public static bool IsCombined(Mesh m) => m != null && m.name.StartsWith("Combined Mesh", StringComparison.Ordinal);

        /// <summary>Stable key: scene path + hierarchy with sibling indices (same in Edit and Play).</summary>
        public static string Key(GameObject go)
        {
            var parts = new List<string>();
            for (Transform t = go.transform; t != null; t = t.parent) parts.Add(t.GetSiblingIndex() + ":" + t.name);
            parts.Reverse();
            return go.scene.path + "|" + string.Join("/", parts);
        }

        /// <summary>Readable copy of a mesh (Instantiate when readable, else a CPU copy via read-only MeshData). Null + reason when impossible.</summary>
        public static Mesh ReadableCopy(Mesh src, out string why)
        {
            why = null;
            if (src == null) { why = "no mesh"; return null; }
            if (src.isReadable)
            {
                Mesh m = UnityEngine.Object.Instantiate(src);
                m.name = src.name + "_carveCopy";
                return m;
            }
            try
            {
                using (Mesh.MeshDataArray arr = Mesh.AcquireReadOnlyMeshData(src))
                {
                    Mesh.MeshData d = arr[0];
                    int vc = d.vertexCount;
                    var m = new Mesh { name = src.name + "_readableCopy", indexFormat = vc > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                    using (var v = new NativeArray<Vector3>(vc, Allocator.Temp)) { d.GetVertices(v); m.SetVertices(v); }
                    if (d.HasVertexAttribute(VertexAttribute.Normal)) using (var n = new NativeArray<Vector3>(vc, Allocator.Temp)) { d.GetNormals(n); m.SetNormals(n); }
                    if (d.HasVertexAttribute(VertexAttribute.Tangent)) using (var t = new NativeArray<Vector4>(vc, Allocator.Temp)) { d.GetTangents(t); m.SetTangents(t); }
                    if (d.HasVertexAttribute(VertexAttribute.Color)) using (var c = new NativeArray<Color>(vc, Allocator.Temp)) { d.GetColors(c); m.SetColors(c); }
                    for (int ch = 0; ch < 4; ch++)
                        if (d.HasVertexAttribute((VertexAttribute)((int)VertexAttribute.TexCoord0 + ch)))
                            using (var uv = new NativeArray<Vector4>(vc, Allocator.Temp)) { d.GetUVs(ch, uv); m.SetUVs(ch, uv); }
                    m.subMeshCount = d.subMeshCount;
                    for (int s = 0; s < d.subMeshCount; s++)
                    {
                        SubMeshDescriptor sd = d.GetSubMesh(s);
                        using (var idx = new NativeArray<int>(sd.indexCount, Allocator.Temp)) { d.GetIndices(idx, s); m.SetIndices(idx, sd.topology, s, false); }
                    }
                    m.RecalculateBounds();
                    if (m.vertexCount == 0) { why = "mesh data is not available on the CPU"; UnityEngine.Object.Destroy(m); return null; }
                    return m;
                }
            }
            catch (Exception e) { why = e.GetType().Name + ": " + e.Message; return null; }
        }

        /// <summary>Find the original (pre-batching) mesh of a batched host.</summary>
        public static Mesh FindSource(GameObject host)
        {
            Mesh m = DMCarveStaticMeshCache.Find(host);
            if (m != null) return m;
            MeshCollider mc = host.GetComponent<MeshCollider>();
            if (mc != null && mc.sharedMesh != null && !IsCombined(mc.sharedMesh)) return mc.sharedMesh; // colliders keep the asset mesh
            return null;
        }

        /// <summary>Detach a statically batched host: unbatched readable child renderer + DMCarvable. Null + reason on failure.</summary>
        public static DMCarvable Detach(GameObject host, MeshRenderer mr, out string why)
        {
            why = null;
            var link = host.GetComponent<DMCarveUnbatchedLink>();
            if (link != null && link.target != null) return link.target;
            Mesh src = FindSource(host);
            if (src == null)
            {
                why = "static-batched and its original mesh is unknown (no Edit Mode cache entry and no MeshCollider mesh). Enter Play from the Editor so the static mesh cache rebuilds, or untick Batching Static";
                return null;
            }
            Mesh copy = ReadableCopy(src, out string err);
            if (copy == null)
            {
                why = $"static-batched; original mesh '{src.name}' is not Read/Write and a runtime copy failed ({err}). Use Tools > Carve > Enable Read/Write For Auto-Carvable Tags";
                return null;
            }

            var go = new GameObject(host.name + " (carve)") { layer = host.layer, tag = host.tag };
            go.transform.SetParent(host.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = copy;
            var nmr = go.AddComponent<MeshRenderer>();
            nmr.sharedMaterials = mr.sharedMaterials;
            nmr.shadowCastingMode = mr.shadowCastingMode;
            nmr.receiveShadows = mr.receiveShadows;
            nmr.staticShadowCaster = mr.staticShadowCaster;
            nmr.lightProbeUsage = mr.lightProbeUsage;
            nmr.reflectionProbeUsage = mr.reflectionProbeUsage;
            nmr.probeAnchor = mr.probeAnchor;
            nmr.motionVectorGenerationMode = mr.motionVectorGenerationMode;
            nmr.renderingLayerMask = mr.renderingLayerMask;
            nmr.sortingLayerID = mr.sortingLayerID;
            nmr.sortingOrder = mr.sortingOrder;
            nmr.lightmapIndex = mr.lightmapIndex;
            nmr.lightmapScaleOffset = mr.lightmapScaleOffset;
            nmr.realtimeLightmapIndex = mr.realtimeLightmapIndex;
            nmr.realtimeLightmapScaleOffset = mr.realtimeLightmapScaleOffset;
            mr.enabled = false; // hides this object's part of the static batch

            // A non-convex MeshCollider moves to the child so carving can rebuild it; box/convex/other colliders stay
            // on the original and resolve to the child through DMCarveUnbatchedLink.
            MeshCollider omc = host.GetComponent<MeshCollider>();
            if (omc != null && omc.enabled && !omc.convex)
            {
                var nmc = go.AddComponent<MeshCollider>();
                nmc.sharedMaterial = omc.sharedMaterial;
                nmc.sharedMesh = omc.sharedMesh == src ? copy : omc.sharedMesh;
                omc.enabled = false;
            }

            DMCarvable c = go.AddComponent<DMCarvable>();
            if (link == null) link = host.AddComponent<DMCarveUnbatchedLink>();
            link.target = c;
            Debug.Log($"[Carve] '{host.name}' was static-batched: carving an unbatched readable copy of '{src.name}' ({(src.isReadable ? "Read/Write" : "runtime CPU copy")}).", host);
            return c;
        }
    }

    /// <summary>2026-10-20x: runtime-only link from a batched (hidden) original to its unbatched carvable child.</summary>
    [AddComponentMenu("")]
    public sealed class DMCarveUnbatchedLink : MonoBehaviour
    {
        public DMCarvable target;
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Project.SurfaceCarve
{
    /// <summary>
    /// 2026-10-24ag: after a Play Mode carve, pieces that are no longer connected to the main body (near the cut,
    /// containing cut faces) become falling chunks: own mesh, convex MeshCollider, Rigidbody, hot glow, lifetime, cap.
    /// Pieces below chunkMinSize are deleted (no slivers). Tunables: DMCarveSettings "Falling Chunks".
    /// </summary>
    public static class DMCarveChunks
    {
        private static readonly List<DMCarveChunk> Live = new List<DMCarveChunk>();
        private static readonly Stack<DMCarveChunk> Free = new Stack<DMCarveChunk>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { Live.Clear(); Free.Clear(); }

        private static int Find(int[] parent, int x)
        {
            while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; }
            return x;
        }

        public static void SplitIslands(DMCarveMeshData d, DMCarveCutter cutter, DMCarveSettings s, HashSet<int> capSubmeshes, DMCarvable owner)
        {
            if (owner == null || d == null || d.VertexCount == 0)
                return;
            Matrix4x4 l2w = owner.transform.localToWorldMatrix;
            int vc = d.VertexCount;

            // 1. Exact weld; union surface triangles with surface, cap triangles with cap (caps are not stitched to the surface).
            var weld = new int[vc];
            var map = new Dictionary<Vector3Int, int>(vc);
            const float q = 2000f;
            int ids = 0;
            for (int i = 0; i < vc; i++)
            {
                Vector3 p = d.Positions[i];
                var key = new Vector3Int(Mathf.RoundToInt(p.x * q), Mathf.RoundToInt(p.y * q), Mathf.RoundToInt(p.z * q));
                if (!map.TryGetValue(key, out int id)) { id = ids++; map[key] = id; }
                weld[i] = id;
            }
            var parent = new int[ids];
            for (int i = 0; i < ids; i++) parent[i] = i;
            for (int sm = 0; sm < d.Submeshes.Count; sm++)
            {
                List<int> t = d.Submeshes[sm];
                for (int i = 0; i + 2 < t.Count; i += 3)
                {
                    int a = Find(parent, weld[t[i]]), b = Find(parent, weld[t[i + 1]]);
                    if (a != b) parent[b] = a;
                    a = Find(parent, a); int c = Find(parent, weld[t[i + 2]]);
                    if (a != c) parent[c] = a;
                }
            }

            var isSurface = new HashSet<int>();
            var isCap = new HashSet<int>();
            for (int sm = 0; sm < d.Submeshes.Count; sm++)
            {
                List<int> t = d.Submeshes[sm];
                bool cap = capSubmeshes.Contains(sm);
                for (int i = 0; i + 2 < t.Count; i += 3)
                    (cap ? isCap : isSurface).Add(Find(parent, weld[t[i]]));
            }
            if (isSurface.Count < 2)
                return;

            // 2. Assign cap-only pieces to the nearest surface piece (vertex proximity near the cut).
            float reach = cutter.MaxRadius * Mathf.Max(1f, s.chunkSearchRadii);
            Bounds region = new Bounds(cutter.Center, Vector3.one * (reach * 2f));
            float tol = Mathf.Clamp(cutter.Radius * 0.3f, 0.004f, 0.1f);
            float inv = 1f / tol;
            var cells = new Dictionary<Vector3Int, List<int>>();
            var world = new Vector3[vc];
            var near = new bool[vc];
            for (int i = 0; i < vc; i++)
            {
                world[i] = l2w.MultiplyPoint3x4(d.Positions[i]);
                near[i] = region.Contains(world[i]);
            }
            var vertIsCap = new bool[vc];
            for (int sm = 0; sm < d.Submeshes.Count; sm++)
            {
                if (!capSubmeshes.Contains(sm)) continue;
                List<int> t = d.Submeshes[sm];
                for (int i = 0; i < t.Count; i++) vertIsCap[t[i]] = true;
            }
            for (int i = 0; i < vc; i++)
            {
                if (!near[i] || vertIsCap[i]) continue;
                int root = Find(parent, weld[i]);
                if (!isSurface.Contains(root)) continue;
                var c = new Vector3Int(Mathf.FloorToInt(world[i].x * inv), Mathf.FloorToInt(world[i].y * inv), Mathf.FloorToInt(world[i].z * inv));
                if (!cells.TryGetValue(c, out var list)) cells[c] = list = new List<int>();
                list.Add(i);
            }
            var votes = new Dictionary<int, Dictionary<int, int>>();
            for (int i = 0; i < vc; i++)
            {
                if (!near[i] || !vertIsCap[i]) continue;
                int capRoot = Find(parent, weld[i]);
                if (isSurface.Contains(capRoot)) continue;
                var c = new Vector3Int(Mathf.FloorToInt(world[i].x * inv), Mathf.FloorToInt(world[i].y * inv), Mathf.FloorToInt(world[i].z * inv));
                int bestRoot = -1; float bestD = tol * tol;
                for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++) for (int z = -1; z <= 1; z++)
                {
                    if (!cells.TryGetValue(new Vector3Int(c.x + x, c.y + y, c.z + z), out var list)) continue;
                    for (int k = 0; k < list.Count; k++)
                    {
                        float dd = (world[list[k]] - world[i]).sqrMagnitude;
                        if (dd < bestD) { bestD = dd; bestRoot = Find(parent, weld[list[k]]); }
                    }
                }
                if (bestRoot < 0) continue;
                if (!votes.TryGetValue(capRoot, out var v)) votes[capRoot] = v = new Dictionary<int, int>();
                v[bestRoot] = v.TryGetValue(bestRoot, out int n) ? n + 1 : 1;
            }
            var group = new Dictionary<int, int>(); // cap root -> surface root
            var hasCap = new HashSet<int>();
            foreach (var kv in votes)
            {
                int best = -1, bn = 0;
                foreach (var e in kv.Value) if (e.Value > bn) { bn = e.Value; best = e.Key; }
                if (best >= 0) { group[kv.Key] = best; hasCap.Add(best); }
            }

            // 3. Group stats (surface groups only).
            var count = new Dictionary<int, int>();
            var bounds = new Dictionary<int, Bounds>();
            for (int sm = 0; sm < d.Submeshes.Count; sm++)
            {
                List<int> t = d.Submeshes[sm];
                for (int i = 0; i + 2 < t.Count; i += 3)
                {
                    int root = Find(parent, weld[t[i]]);
                    int g = isSurface.Contains(root) ? root : (group.TryGetValue(root, out int gg) ? gg : -1);
                    if (g < 0) continue;
                    count[g] = count.TryGetValue(g, out int n) ? n + 1 : 1;
                    Bounds b = bounds.TryGetValue(g, out Bounds bb) ? bb : new Bounds(world[t[i]], Vector3.zero);
                    b.Encapsulate(world[t[i]]); b.Encapsulate(world[t[i + 1]]); b.Encapsulate(world[t[i + 2]]);
                    bounds[g] = b;
                }
            }
            int main = -1, most = -1;
            foreach (var kv in count) if (isSurface.Contains(kv.Key) && kv.Value > most) { most = kv.Value; main = kv.Key; }

            var detach = new HashSet<int>();
            var delete = new HashSet<int>();
            foreach (int root in isSurface)
            {
                if (root == main || !hasCap.Contains(root) || !bounds.TryGetValue(root, out Bounds b)) continue;
                if (b.SqrDistance(cutter.Center) > reach * reach) continue;
                float size = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                if (size > reach * 2f) continue; // big separate part of the model: leave it
                if (size < s.chunkMinSize) delete.Add(root); else detach.Add(root);
            }
            if (detach.Count == 0 && delete.Count == 0)
                return;

            var groupOfRoot = new Dictionary<int, int>();
            foreach (int r in isSurface) groupOfRoot[r] = r;
            foreach (var kv in group) groupOfRoot[kv.Key] = kv.Value;

            MeshRenderer mr = owner.GetComponent<MeshRenderer>();
            Material[] mats = mr != null ? mr.sharedMaterials : new Material[0];
            foreach (int g in detach)
                SpawnChunk(owner, d, l2w, weld, parent, groupOfRoot, g, mats, capSubmeshes, s);

            for (int sm = 0; sm < d.Submeshes.Count; sm++)
            {
                List<int> t = d.Submeshes[sm];
                var keep = new List<int>(t.Count);
                for (int i = 0; i + 2 < t.Count; i += 3)
                {
                    int root = Find(parent, weld[t[i]]);
                    if (groupOfRoot.TryGetValue(root, out int g) && (detach.Contains(g) || delete.Contains(g))) continue;
                    keep.Add(t[i]); keep.Add(t[i + 1]); keep.Add(t[i + 2]);
                }
                t.Clear(); t.AddRange(keep);
            }
            if (s.logChunkSplit)
                Debug.Log("[CarveChunk] " + owner.name + " detached=" + detach.Count + " deleted=" + delete.Count + " pieces=" + isSurface.Count);
        }

        private static void SpawnChunk(DMCarvable owner, DMCarveMeshData d, Matrix4x4 l2w, int[] weld, int[] parent, Dictionary<int, int> groupOfRoot, int root,
            Material[] mats, HashSet<int> capSubmeshes, DMCarveSettings s)
        {
            if (s.chunkMaxLive <= 0) return;
            Matrix4x4 nrm = l2w.inverse.transpose;
            var remap = new Dictionary<int, int>();
            var pos = new List<Vector3>(); var nor = new List<Vector3>(); var uv = new List<Vector2>();
            var subs = new List<List<int>>(); var subMats = new List<Material>(); var glowSubs = new List<int>();
            Vector3 centroid = Vector3.zero; int cn = 0;
            for (int sm = 0; sm < d.Submeshes.Count; sm++)
            {
                List<int> t = d.Submeshes[sm]; List<int> outT = null;
                for (int i = 0; i + 2 < t.Count; i += 3)
                {
                    if (!groupOfRoot.TryGetValue(Find(parent, weld[t[i]]), out int gr) || gr != root) continue;
                    if (outT == null)
                    {
                        outT = new List<int>(); subs.Add(outT);
                        subMats.Add(sm < mats.Length && mats[sm] != null ? mats[sm] : (owner.LastFractureMaterial != null ? owner.LastFractureMaterial : (mats.Length > 0 ? mats[0] : null)));
                        if (capSubmeshes.Contains(sm)) glowSubs.Add(subs.Count - 1);
                    }
                    for (int k = 0; k < 3; k++)
                    {
                        int v = t[i + k];
                        if (!remap.TryGetValue(v, out int nv))
                        {
                            nv = pos.Count; remap[v] = nv;
                            Vector3 w = l2w.MultiplyPoint3x4(d.Positions[v]);
                            pos.Add(w); centroid += w; cn++;
                            nor.Add(d.HasNormals ? nrm.MultiplyVector(d.Normals[v]).normalized : Vector3.up);
                            uv.Add(d.HasUv0 ? d.Uv0[v] : Vector2.zero);
                        }
                        outT.Add(nv);
                    }
                }
            }
            if (cn == 0) return;
            centroid /= cn;
            for (int i = 0; i < pos.Count; i++) pos[i] -= centroid;

            var mesh = new Mesh { name = owner.name + "_chunk" };
            if (pos.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(pos); mesh.SetNormals(nor); mesh.SetUVs(0, uv);
            mesh.subMeshCount = subs.Count;
            for (int i = 0; i < subs.Count; i++) mesh.SetTriangles(subs[i], i, false);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            while (Live.Count >= s.chunkMaxLive && Live.Count > 0) Release(Live[0]);
            DMCarveChunk c = Free.Count > 0 ? Free.Pop() : null;
            if (c == null)
            {
                var go = new GameObject("CarveChunk");
                go.AddComponent<MeshFilter>(); go.AddComponent<MeshRenderer>();
                go.AddComponent<Rigidbody>();
                c = go.AddComponent<DMCarveChunk>();
            }
            c.Activate(mesh, subMats.ToArray(), glowSubs, centroid, owner.gameObject.layer, s);
            Live.Add(c);
        }

        internal static void Release(DMCarveChunk c)
        {
            Live.Remove(c);
            if (c == null) return;
            c.Deactivate();
            Free.Push(c);
        }
    }

    [AddComponentMenu("")]
    public sealed class DMCarveChunk : MonoBehaviour
    {
        private static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");
        private Mesh mesh;
        private float born;
        private readonly List<int> glow = new List<int>();
        private MaterialPropertyBlock mpb;
        private Collider col;

        public void Activate(Mesh m, Material[] mats, List<int> glowSubs, Vector3 position, int layer, DMCarveSettings s)
        {
            if (mesh != null) Destroy(mesh);
            mesh = m;
            gameObject.layer = layer;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            transform.localScale = Vector3.one;
            GetComponent<MeshFilter>().sharedMesh = m;
            GetComponent<MeshRenderer>().sharedMaterials = mats;
            glow.Clear(); glow.AddRange(glowSubs);

            if (col != null) Destroy(col);
            Vector3 size = m.bounds.size;
            float minSide = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
            if (minSide < 0.01f || m.vertexCount < 4)
            {
                var box = gameObject.AddComponent<BoxCollider>();
                box.center = m.bounds.center; box.size = Vector3.Max(size, Vector3.one * 0.01f);
                col = box;
            }
            else
            {
                var mc = gameObject.AddComponent<MeshCollider>();
                mc.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation | MeshColliderCookingOptions.EnableMeshCleaning | MeshColliderCookingOptions.WeldColocatedVertices;
                mc.convex = true;
                mc.sharedMesh = m;
                col = mc;
            }

            Rigidbody rb = GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.mass = Mathf.Clamp(s.chunkDensity * size.x * size.y * size.z * 0.5f, 0.05f, Mathf.Max(0.05f, s.chunkMaxMass));
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Random.insideUnitSphere * 0.5f;
            born = Time.time;
            gameObject.SetActive(true);
            enabled = true;
        }

        public void Deactivate()
        {
            if (glowApplied) ApplyGlow(0f, DMCarveSettings.Active);
            gameObject.SetActive(false);
            if (mesh != null) { Destroy(mesh); mesh = null; }
        }

        private void Update()
        {
            DMCarveSettings s = DMCarveSettings.Active;
            float age = Time.time - born;
            if (age >= s.chunkLifetime) { DMCarveChunks.Release(this); return; }
            float t = age / Mathf.Max(0.05f, s.carveGlowCoolSeconds);
            float k = !s.carveGlowEnabled || t >= 1f ? 0f
                : Mathf.Max(0f, s.carveGlowCurve != null && s.carveGlowCurve.length > 0 ? s.carveGlowCurve.Evaluate(t) : 1f - t);
            if (s.carveSurfaceGlowEnabled || glowApplied) ApplyGlow(s.carveSurfaceGlowEnabled ? k : 0f, s); // 24ah
        }

        private bool glowApplied;

        private void ApplyGlow(float k, DMCarveSettings s)
        {
            glowApplied = k > 0f;
            MeshRenderer r = GetComponent<MeshRenderer>();
            if (r == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            Color c = s.carveGlowColor * (s.carveGlowIntensity * k); c.a = 1f;
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < glow.Count; i++)
            {
                int sub = glow[i];
                if (sub >= mats.Length || mats[sub] == null || !mats[sub].HasProperty(EmissiveColorId)) continue;
                if (k <= 0f) { r.SetPropertyBlock(null, sub); continue; }
                r.GetPropertyBlock(mpb, sub);
                mpb.SetColor(EmissiveColorId, c);
                r.SetPropertyBlock(mpb, sub);
            }
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }
    }
}

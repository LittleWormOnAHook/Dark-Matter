using System.Collections.Generic;
using UnityEngine;

namespace Project.SurfaceCarve
{
    /// <summary>Small broken-off rock chunks for Fracture / Blast cuts (Play Mode only).</summary>
    public static class DMCarveDebris
    {
        private static readonly Queue<GameObject> Live = new Queue<GameObject>();
        private static readonly List<Vector3> Verts = new List<Vector3>();
        private static readonly List<Vector3> Normals = new List<Vector3>();
        private static readonly List<int> Tris = new List<int>();

        public static void Spawn(DMCarveCutter cutter, Vector3 normal, DMSurfaceDamageSettings damage, Material material, int layer)
        {
            if (!Application.isPlaying || cutter == null || damage == null || damage.style == DMCarveStyle.Erosion)
                return;

            DMCarveSettings s = DMCarveSettings.Active;
            if (!s.spawnDebris || s.maxLiveDebris <= 0)
                return;

            Vector3 n = normal.sqrMagnitude > 1e-8f ? normal.normalized : Vector3.up;
            var rng = new System.Random(cutter.Seed);
            int count = Mathf.Clamp(damage.debrisCount, 0, 8);
            for (int i = 0; i < count; i++)
            {
                while (Live.Count > 0 && (Live.Peek() == null || Live.Count >= s.maxLiveDebris))
                {
                    GameObject old = Live.Dequeue();
                    if (old != null)
                        Object.Destroy(old);
                }

                float size = cutter.Radius * damage.debrisScale * Mathf.Max(0.1f, s.debrisSizeScale) * (0.6f + 0.8f * (float)rng.NextDouble());
                Vector3 spread = new Vector3((float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() - 0.5f) * cutter.Radius;
                Vector3 pos = cutter.Center + n * (cutter.Radius * 0.9f + size * 0.5f) + spread;
                GameObject go = Build(pos, size, rng.Next(1, int.MaxValue), material, layer, s);
                if (go == null)
                    continue;

                Rigidbody rb = go.GetComponent<Rigidbody>();
                Vector3 dir = (n + Random.insideUnitSphere * 0.6f).normalized;
                rb.AddForce(dir * s.debrisImpulse * (0.6f + (float)rng.NextDouble()), ForceMode.VelocityChange);
                rb.AddTorque(Random.insideUnitSphere * 6f, ForceMode.VelocityChange);
                Live.Enqueue(go);
            }
        }

        private static GameObject Build(Vector3 pos, float size, int seed, Material material, int layer, DMCarveSettings s)
        {
            var shape = new DMCarveCutter(pos, Random.rotationUniform, size, DMCarveStyle.Fracture, 1.1f, seed);
            shape.BuildMesh(1, 1f, Verts, Normals, Tris);
            if (Verts.Count < 4)
                return null;

            var uv = new List<Vector2>(Verts.Count);
            for (int i = 0; i < Verts.Count; i++)
            {
                Vector3 local = Verts[i] - pos;
                Verts[i] = local;
                Vector3 a = new Vector3(Mathf.Abs(Normals[i].x), Mathf.Abs(Normals[i].y), Mathf.Abs(Normals[i].z));
                Vector2 u = a.x >= a.y && a.x >= a.z ? new Vector2(local.z, local.y)
                    : a.y >= a.z ? new Vector2(local.x, local.z) : new Vector2(local.x, local.y);
                uv.Add(u / Mathf.Max(0.05f, s.cutUvTileMeters));
            }

            var mesh = new Mesh { name = "CarveDebris" };
            mesh.SetVertices(Verts);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(Tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            var go = new GameObject("CarveDebris") { layer = layer };
            go.transform.position = pos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            if (material != null)
                mr.sharedMaterial = material;
            var mc = go.AddComponent<MeshCollider>();
            mc.convex = true;
            mc.sharedMesh = mesh;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = Mathf.Max(0.05f, size * size * size * 2600f);
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            go.AddComponent<DMCarveDebrisLife>().Init(s.debrisLifetime, mesh);
            return go;
        }
    }

    /// <summary>Shrinks and removes a debris chunk at the end of its life.</summary>
    public sealed class DMCarveDebrisLife : MonoBehaviour
    {
        private float dieAt;
        private float shrink = 0.6f;
        private Vector3 baseScale;
        private Mesh ownedMesh;

        public void Init(float lifetime, Mesh mesh)
        {
            dieAt = Time.time + lifetime;
            ownedMesh = mesh;
            baseScale = transform.localScale;
        }

        private void Update()
        {
            float left = dieAt - Time.time;
            if (left <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (left < shrink)
                transform.localScale = baseScale * Mathf.Max(0.01f, left / shrink);
        }

        private void OnDestroy()
        {
            if (ownedMesh != null)
                Destroy(ownedMesh);
        }
    }
}

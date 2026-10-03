using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.World.ProcPlacement
{
    /// <summary>
    /// Builds a rock mesh from a recipe + seed. Same recipe + same seed always gives the same mesh.
    /// The pivot sits at the flattened base (y = 0) so later phases can snap it straight onto the ground.
    /// </summary>
    public static class DmNoiseRockGenerator
    {
        private static readonly Dictionary<int, (Vector3[] verts, int[] tris)> IcoCache = new Dictionary<int, (Vector3[], int[])>();

        public static void Build(Mesh mesh, DmProcRecipe r, int seed)
        {
            GetIcosphere(Mathf.Clamp(r.subdivisions, 2, 6), out Vector3[] baseVerts, out int[] tris);

            var rng = new System.Random(seed);
            var noise = new DmSeededNoise(seed);

            float shared = (float)rng.NextDouble();
            Vector3 size = new Vector3(
                Mathf.Lerp(r.sizeMin.x, r.sizeMax.x, Mathf.Lerp((float)rng.NextDouble(), shared, r.uniformScaleBias)),
                Mathf.Lerp(r.sizeMin.y, r.sizeMax.y, Mathf.Lerp((float)rng.NextDouble(), shared, r.uniformScaleBias)),
                Mathf.Lerp(r.sizeMin.z, r.sizeMax.z, Mathf.Lerp((float)rng.NextDouble(), shared, r.uniformScaleBias)));

            Quaternion shapeRot = Quaternion.Euler(
                Range(rng, -r.tiltJitter, r.tiltJitter),
                Range(rng, 0f, 360f),
                Range(rng, -r.tiltJitter, r.tiltJitter));

            Vector3 offA = new Vector3(Range(rng, -500f, 500f), Range(rng, -500f, 500f), Range(rng, -500f, 500f));
            Vector3 offB = new Vector3(Range(rng, -500f, 500f), Range(rng, -500f, 500f), Range(rng, -500f, 500f));
            float strataPhase = (float)rng.NextDouble();

            int n = baseVerts.Length;
            var verts = new Vector3[n];
            float minY = float.MaxValue, maxY = float.MinValue;

            for (int i = 0; i < n; i++)
            {
                Vector3 dir = baseVerts[i];
                Vector3 p = dir * r.noiseFrequency;
                float f = noise.Fbm(p + offA, r.octaves, r.lacunarity, r.gain);
                float ridge = noise.Ridged(p * 1.7f + offB, Mathf.Max(1, r.octaves - 1), r.lacunarity, r.gain);
                float disp = 1f + r.noiseAmplitude * f * 1.6f + r.ridgeAmplitude * (ridge - 0.55f);
                disp = Mathf.Max(0.25f, disp);

                Vector3 v = shapeRot * (dir * disp);
                v = Vector3.Scale(v, size * 0.5f);
                verts[i] = v;
                if (v.y < minY) minY = v.y;
                if (v.y > maxY) maxY = v.y;
            }

            // Strata: pull heights toward evenly spaced layers for a layered-outcrop look.
            if (r.strataSteps > 0 && r.strataStrength > 0f)
            {
                float h = Mathf.Max(0.001f, maxY - minY);
                for (int i = 0; i < n; i++)
                {
                    float t = (verts[i].y - minY) / h * r.strataSteps + strataPhase;
                    float stepped = (Mathf.Floor(t) + Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - Mathf.Floor(t) - 0.75f) * 4f)) - strataPhase) / r.strataSteps;
                    verts[i].y = Mathf.Lerp(verts[i].y, minY + stepped * h, r.strataStrength);
                }
            }

            // Flatten the underside and move the pivot to the base.
            float cut = Mathf.Lerp(minY, maxY, r.bottomFlatten);
            for (int i = 0; i < n; i++)
            {
                Vector3 v = verts[i];
                if (v.y < cut)
                    v.y = cut + (v.y - cut) * 0.08f;
                v.y -= cut;
                verts[i] = v;
            }

            mesh.Clear();
            mesh.indexFormat = n > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();

            // Box-projected UVs by dominant normal axis (the Phase 6 triplanar layer hides the seams).
            Vector3[] normals = mesh.normals;
            var uvs = new Vector2[n];
            float s = r.uvScale;
            for (int i = 0; i < n; i++)
            {
                Vector3 nn = normals[i];
                Vector3 v = verts[i];
                float ax = Mathf.Abs(nn.x), ay = Mathf.Abs(nn.y), az = Mathf.Abs(nn.z);
                if (ay >= ax && ay >= az) uvs[i] = new Vector2(v.x, v.z) * s;
                else if (ax >= az) uvs[i] = new Vector2(v.z, v.y) * s;
                else uvs[i] = new Vector2(v.x, v.y) * s;
            }
            mesh.uv = uvs;
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
        }

        private static float Range(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        private static void GetIcosphere(int level, out Vector3[] verts, out int[] tris)
        {
            if (IcoCache.TryGetValue(level, out var cached))
            {
                verts = cached.verts;
                tris = cached.tris;
                return;
            }

            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var v = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };
            for (int i = 0; i < v.Count; i++) v[i] = v[i].normalized;

            var f = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };

            for (int l = 0; l < level; l++)
            {
                var mid = new Dictionary<long, int>();
                var nf = new List<int>(f.Count * 4);
                for (int i = 0; i < f.Count; i += 3)
                {
                    int a = f[i], b = f[i + 1], c = f[i + 2];
                    int ab = Mid(a, b, v, mid), bc = Mid(b, c, v, mid), ca = Mid(c, a, v, mid);
                    nf.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                f = nf;
            }

            // Unity uses clockwise winding for front faces; flip the CCW icosphere.
            for (int i = 0; i < f.Count; i += 3)
                (f[i + 1], f[i + 2]) = (f[i + 2], f[i + 1]);

            verts = v.ToArray();
            tris = f.ToArray();
            IcoCache[level] = (verts, tris);
        }

        private static int Mid(int a, int b, List<Vector3> v, Dictionary<long, int> cache)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (cache.TryGetValue(key, out int idx)) return idx;
            v.Add(((v[a] + v[b]) * 0.5f).normalized);
            idx = v.Count - 1;
            cache[key] = idx;
            return idx;
        }
    }
}

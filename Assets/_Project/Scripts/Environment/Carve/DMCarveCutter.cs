using System.Collections.Generic;
using UnityEngine;

namespace Project.SurfaceCarve
{
    /// <summary>
    /// A star-shaped noisy cutter volume. Local +Z points into the hit surface.
    /// Evaluate() is negative inside the volume.
    /// </summary>
    public sealed class DMCarveCutter
    {
        public const float MaxRadiusFactor = 1.7f;

        public readonly Vector3 Center;
        public readonly Quaternion Rotation;
        public readonly float Radius;
        public readonly DMCarveStyle Style;
        public readonly float Roughness;
        public readonly int Seed;

        private readonly Quaternion inverse;
        private readonly Vector3 offset;
        private Vector3[] planeNormals;
        private float[] planeDistances;

        public float MaxRadius => Radius * MaxRadiusFactor;

        public DMCarveCutter(Vector3 center, Quaternion rotation, float radius, DMCarveStyle style, float roughness, int seed)
        {
            Center = center;
            Rotation = rotation;
            Radius = Mathf.Max(0.005f, radius);
            Style = style;
            Roughness = Mathf.Clamp(roughness, 0f, 2f);
            Seed = seed;
            inverse = Quaternion.Inverse(rotation);
            offset = DMCarveNoise.SeedOffset(seed);
            if (style == DMCarveStyle.Fracture)
                BuildPlanes();
        }

        private void BuildPlanes()
        {
            var rng = new System.Random(Seed * 7919 + 17);
            int count = 7 + rng.Next(6);
            planeNormals = new Vector3[count];
            planeDistances = new float[count];
            for (int i = 0; i < count; i++)
            {
                Vector3 n;
                do
                {
                    n = new Vector3(
                        (float)rng.NextDouble() * 2f - 1f,
                        (float)rng.NextDouble() * 2f - 1f,
                        (float)rng.NextDouble() * 2f - 1f);
                }
                while (n.sqrMagnitude < 0.05f || n.sqrMagnitude > 1f);

                planeNormals[i] = n.normalized;
                // Rougher = planes cut deeper into the sphere = more faceted chunk.
                float minD = Mathf.Lerp(0.9f, 0.6f, Mathf.Clamp01(Roughness * 0.5f));
                planeDistances[i] = Mathf.Lerp(minD, 1.02f, (float)rng.NextDouble());
            }
        }

        /// <summary>Radius in meters along a unit direction given in cutter local space.</summary>
        public float RadialLocal(Vector3 d)
        {
            float r;
            switch (Style)
            {
                case DMCarveStyle.Fracture:
                {
                    float m = 1.3f;
                    for (int i = 0; i < planeNormals.Length; i++)
                    {
                        float c = Vector3.Dot(d, planeNormals[i]);
                        if (c > 1e-4f)
                        {
                            float t = planeDistances[i] / c;
                            if (t < m)
                                m = t;
                        }
                    }

                    // Faceted chunk with small conchoidal chips on each face.
                    r = m * (1f + Roughness * 0.06f * DMCarveNoise.Fbm(d * 3.1f + offset, 3));
                    break;
                }
                case DMCarveStyle.Erosion:
                {
                    float body = 1f + Roughness * 0.22f * DMCarveNoise.Fbm(d * 1.7f + offset, 4);
                    float pit = 1f - Mathf.Abs(DMCarveNoise.Perlin(d * 5.3f + offset * 1.7f));
                    pit *= pit;
                    pit *= pit;
                    r = body + Roughness * 0.12f * pit;
                    break;
                }
                default:
                {
                    // Blast: wide crater (short along +Z into the surface) with ridged spall.
                    const float c = 0.72f;
                    float inv = Mathf.Sqrt(d.x * d.x + d.y * d.y + (d.z * d.z) / (c * c));
                    float e = 1f / Mathf.Max(inv, 1e-4f);
                    float ridge = DMCarveNoise.Ridged(d * 4.2f + offset, 3);
                    r = e * (1f + Roughness * (0.32f * ridge - 0.12f));
                    break;
                }
            }

            return Radius * Mathf.Clamp(r, 0.3f, MaxRadiusFactor);
        }

        /// <summary>Signed value, negative inside the cutter. Not a true distance but the sign and zero set are exact.</summary>
        public float Evaluate(Vector3 world)
        {
            Vector3 v = inverse * (world - Center);
            float len = v.magnitude;
            if (len < 1e-7f)
                return -Radius;
            return len - RadialLocal(v / len);
        }

        public bool OverlapsBounds(Bounds b)
        {
            return b.SqrDistance(Center) <= MaxRadius * MaxRadius;
        }

        /// <summary>World-space surface mesh of the cutter. Normals point outward; winding faces outward.</summary>
        public void BuildMesh(int subdivisions, float inflate, List<Vector3> verts, List<Vector3> normals, List<int> tris)
        {
            DMCarveIcosphere.Get(subdivisions, out Vector3[] unit, out int[] faces);
            verts.Clear();
            normals.Clear();
            tris.Clear();
            for (int i = 0; i < unit.Length; i++)
            {
                Vector3 d = unit[i];
                verts.Add(Center + Rotation * (d * (RadialLocal(d) * inflate)));
                normals.Add(Vector3.zero);
            }

            for (int i = 0; i < faces.Length; i += 3)
            {
                int a = faces[i];
                int b = faces[i + 1];
                int c = faces[i + 2];
                Vector3 n = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
                Vector3 centroid = (verts[a] + verts[b] + verts[c]) / 3f;
                if (Vector3.Dot(n, centroid - Center) < 0f)
                {
                    int t = b;
                    b = c;
                    c = t;
                    n = -n;
                }

                tris.Add(a);
                tris.Add(b);
                tris.Add(c);
                normals[a] += n;
                normals[b] += n;
                normals[c] += n;
            }

            for (int i = 0; i < normals.Count; i++)
            {
                Vector3 n = normals[i];
                normals[i] = n.sqrMagnitude > 1e-12f ? n.normalized : (verts[i] - Center).normalized;
            }
        }
    }

    /// <summary>Cached unit icospheres.</summary>
    public static class DMCarveIcosphere
    {
        private static readonly Dictionary<int, Vector3[]> CachedVerts = new Dictionary<int, Vector3[]>();
        private static readonly Dictionary<int, int[]> CachedFaces = new Dictionary<int, int[]>();

        public static void Get(int subdivisions, out Vector3[] verts, out int[] faces)
        {
            subdivisions = Mathf.Clamp(subdivisions, 0, 5);
            if (CachedVerts.TryGetValue(subdivisions, out verts) && CachedFaces.TryGetValue(subdivisions, out faces))
                return;

            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var v = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            for (int i = 0; i < v.Count; i++)
                v[i] = v[i].normalized;

            var f = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            };

            for (int s = 0; s < subdivisions; s++)
            {
                var mid = new Dictionary<long, int>();
                var nf = new List<int>(f.Count * 4);
                for (int i = 0; i < f.Count; i += 3)
                {
                    int a = f[i], b = f[i + 1], c = f[i + 2];
                    int ab = Mid(a, b, v, mid);
                    int bc = Mid(b, c, v, mid);
                    int ca = Mid(c, a, v, mid);
                    nf.Add(a); nf.Add(ab); nf.Add(ca);
                    nf.Add(b); nf.Add(bc); nf.Add(ab);
                    nf.Add(c); nf.Add(ca); nf.Add(bc);
                    nf.Add(ab); nf.Add(bc); nf.Add(ca);
                }

                f = nf;
            }

            verts = v.ToArray();
            faces = f.ToArray();
            CachedVerts[subdivisions] = verts;
            CachedFaces[subdivisions] = faces;
        }

        private static int Mid(int a, int b, List<Vector3> v, Dictionary<long, int> cache)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (cache.TryGetValue(key, out int idx))
                return idx;
            idx = v.Count;
            v.Add(((v[a] + v[b]) * 0.5f).normalized);
            cache[key] = idx;
            return idx;
        }
    }
}

using UnityEngine;

namespace Project.SurfaceCarve
{
    /// <summary>Small 3D gradient noise (improved Perlin) for natural cutter shapes.</summary>
    public static class DMCarveNoise
    {
        private static readonly int[] Perm = BuildPerm();

        private static int[] BuildPerm()
        {
            var p = new int[256];
            for (int i = 0; i < 256; i++)
                p[i] = i;
            var rng = new System.Random(1337);
            for (int i = 255; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = p[i];
                p[i] = p[j];
                p[j] = t;
            }

            var perm = new int[512];
            for (int i = 0; i < 512; i++)
                perm[i] = p[i & 255];
            return perm;
        }

        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        private static float Grad(int hash, float x, float y, float z)
        {
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        /// <summary>Roughly -1..1.</summary>
        public static float Perlin(Vector3 p)
        {
            int xi = Mathf.FloorToInt(p.x);
            int yi = Mathf.FloorToInt(p.y);
            int zi = Mathf.FloorToInt(p.z);
            float x = p.x - xi;
            float y = p.y - yi;
            float z = p.z - zi;
            xi &= 255;
            yi &= 255;
            zi &= 255;
            float u = Fade(x);
            float v = Fade(y);
            float w = Fade(z);

            int a = Perm[xi] + yi;
            int aa = Perm[a] + zi;
            int ab = Perm[a + 1] + zi;
            int b = Perm[xi + 1] + yi;
            int ba = Perm[b] + zi;
            int bb = Perm[b + 1] + zi;

            float x1 = Mathf.Lerp(Grad(Perm[aa], x, y, z), Grad(Perm[ba], x - 1f, y, z), u);
            float x2 = Mathf.Lerp(Grad(Perm[ab], x, y - 1f, z), Grad(Perm[bb], x - 1f, y - 1f, z), u);
            float y1 = Mathf.Lerp(x1, x2, v);
            float x3 = Mathf.Lerp(Grad(Perm[aa + 1], x, y, z - 1f), Grad(Perm[ba + 1], x - 1f, y, z - 1f), u);
            float x4 = Mathf.Lerp(Grad(Perm[ab + 1], x, y - 1f, z - 1f), Grad(Perm[bb + 1], x - 1f, y - 1f, z - 1f), u);
            float y2 = Mathf.Lerp(x3, x4, v);
            return Mathf.Lerp(y1, y2, w);
        }

        /// <summary>Fractal sum, roughly -1..1.</summary>
        public static float Fbm(Vector3 p, int octaves)
        {
            float sum = 0f;
            float amp = 0.5f;
            float norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += amp * Perlin(p);
                norm += amp;
                p *= 2.03f;
                amp *= 0.5f;
            }

            return norm > 0f ? sum / norm : 0f;
        }

        /// <summary>Ridged fractal, 0..1 with sharp creases (spall / spikes).</summary>
        public static float Ridged(Vector3 p, int octaves)
        {
            float sum = 0f;
            float amp = 0.5f;
            float norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                float n = 1f - Mathf.Abs(Perlin(p));
                sum += amp * n * n;
                norm += amp;
                p *= 2.11f;
                amp *= 0.5f;
            }

            return norm > 0f ? sum / norm : 0f;
        }

        public static Vector3 SeedOffset(int seed)
        {
            var r = new System.Random(seed);
            return new Vector3((float)r.NextDouble() * 173f, (float)r.NextDouble() * 173f, (float)r.NextDouble() * 173f);
        }

        /// <summary>0..1 hash noise for jittering rim widths.</summary>
        public static float Value01(Vector3 p)
        {
            return Mathf.Clamp01(Perlin(p) * 0.5f + 0.5f);
        }
    }
}

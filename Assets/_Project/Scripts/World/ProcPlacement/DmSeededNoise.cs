using UnityEngine;

namespace Project.World.ProcPlacement
{
    /// <summary>Deterministic 3D gradient (Perlin) noise with a per-seed permutation table.</summary>
    public sealed class DmSeededNoise
    {
        private readonly int[] perm = new int[512];

        public DmSeededNoise(int seed)
        {
            var rng = new System.Random(seed);
            var p = new int[256];
            for (int i = 0; i < 256; i++) p[i] = i;
            for (int i = 255; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (p[i], p[j]) = (p[j], p[i]);
            }
            for (int i = 0; i < 512; i++) perm[i] = p[i & 255];
        }

        private static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

        private static float Grad(int hash, float x, float y, float z)
        {
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : (h == 12 || h == 14 ? x : z);
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        /// <summary>Returns roughly -1..1.</summary>
        public float Noise(Vector3 p)
        {
            int xi = Mathf.FloorToInt(p.x), yi = Mathf.FloorToInt(p.y), zi = Mathf.FloorToInt(p.z);
            float x = p.x - xi, y = p.y - yi, z = p.z - zi;
            xi &= 255; yi &= 255; zi &= 255;
            float u = Fade(x), v = Fade(y), w = Fade(z);
            int a = perm[xi] + yi, aa = perm[a] + zi, ab = perm[a + 1] + zi;
            int b = perm[xi + 1] + yi, ba = perm[b] + zi, bb = perm[b + 1] + zi;
            float r = Mathf.Lerp(
                Mathf.Lerp(Mathf.Lerp(Grad(perm[aa], x, y, z), Grad(perm[ba], x - 1, y, z), u),
                           Mathf.Lerp(Grad(perm[ab], x, y - 1, z), Grad(perm[bb], x - 1, y - 1, z), u), v),
                Mathf.Lerp(Mathf.Lerp(Grad(perm[aa + 1], x, y, z - 1), Grad(perm[ba + 1], x - 1, y, z - 1), u),
                           Mathf.Lerp(Grad(perm[ab + 1], x, y - 1, z - 1), Grad(perm[bb + 1], x - 1, y - 1, z - 1), u), v),
                w);
            return Mathf.Clamp(r, -1f, 1f);
        }

        public float Fbm(Vector3 p, int octaves, float lacunarity, float gain)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Noise(p) * amp;
                norm += amp;
                p *= lacunarity;
                amp *= gain;
            }
            return norm > 0f ? sum / norm : 0f;
        }

        public float Ridged(Vector3 p, int octaves, float lacunarity, float gain)
        {
            float sum = 0f, amp = 1f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                float n = 1f - Mathf.Abs(Noise(p));
                sum += n * n * amp;
                norm += amp;
                p *= lacunarity;
                amp *= gain;
            }
            return norm > 0f ? sum / norm : 0f;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Project.SurfaceCarve
{
    /// <summary>
    /// 2026-10-20u: debris chunks take the color of what was hit, plus a rough overlay, via MaterialPropertyBlock
    /// (HDRP Lit _BaseColor / _BaseColorMap / _BaseColorMap_ST) - shared materials and prefabs are never modified.
    /// Sources: renderer base map at the hit UV (readable textures, MeshCollider hit) x _BaseColor; terrain layer mix
    /// at the hit point (alphamap-weighted layer averages); else averaged material color (GPU downsample, cached).
    /// Every Apply writes the full block (or clears it), so a reused renderer never keeps a stale tint.
    /// Settings: DMCarveSettings "Debris Look".
    /// </summary>
    public static class DMCarveDebrisLook
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseColorMap");
        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseColorMap_ST");
        private static readonly Dictionary<Texture, Color> s_avg = new Dictionary<Texture, Color>();
        private static readonly Dictionary<long, Texture2D> s_overlay = new Dictionary<long, Texture2D>();
        private static MaterialPropertyBlock s_mpb;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { s_avg.Clear(); s_overlay.Clear(); s_mpb = null; }

        public static bool TrySample(DMCarveSettings s, GameObject receiver, Vector3 point, Vector3 normal, out Color c)
        {
            c = Color.gray;
            if (s == null || !s.debrisLookEnabled || receiver == null || s.debrisColorSource == DMCarveSettings.DebrisColorSource.FractureMaterial)
                return false;

            Terrain t = receiver.GetComponentInParent<Terrain>();
            if (t != null && t.terrainData != null)
                return TerrainColor(t, point, out c);

            Renderer r = receiver.GetComponentInParent<Renderer>();
            if (r == null) r = receiver.GetComponentInChildren<Renderer>();
            Material m = r != null ? r.sharedMaterial : null;
            if (m == null) return false;

            Color tint = m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : m.HasProperty(ColorId) ? m.GetColor(ColorId) : Color.white;
            Texture tex = m.HasProperty(BaseMapId) ? m.GetTexture(BaseMapId) : m.mainTexture;

            if (s.debrisColorSource == DMCarveSettings.DebrisColorSource.SurfaceSample && tex is Texture2D t2 && t2.isReadable)
            {
                Collider col = receiver.GetComponentInParent<Collider>();
                Vector3 nn = normal.sqrMagnitude > 1e-6f ? normal.normalized : Vector3.up;
                if (col is MeshCollider && col.Raycast(new Ray(point + nn * 0.25f, -nn), out RaycastHit hit, 1f))
                {
                    Vector2 uv = hit.textureCoord;
                    if (m.HasProperty(BaseMapStId)) { Vector4 st = m.GetVector(BaseMapStId); uv = new Vector2(uv.x * st.x + st.z, uv.y * st.y + st.w); }
                    c = t2.GetPixelBilinear(uv.x, uv.y) * tint;
                    c.a = 1f;
                    return true;
                }
            }

            c = (tex != null ? Average(tex) : Color.white) * tint;
            c.a = 1f;
            return true;
        }

        private static bool TerrainColor(Terrain t, Vector3 point, out Color c)
        {
            c = Color.gray;
            TerrainData d = t.terrainData;
            TerrainLayer[] layers = d.terrainLayers;
            if (layers == null || layers.Length == 0) return false;
            Vector3 local = point - t.transform.position;
            int x = Mathf.Clamp(Mathf.RoundToInt(local.x / d.size.x * (d.alphamapWidth - 1)), 0, d.alphamapWidth - 1);
            int z = Mathf.Clamp(Mathf.RoundToInt(local.z / d.size.z * (d.alphamapHeight - 1)), 0, d.alphamapHeight - 1);
            float[,,] w = d.GetAlphamaps(x, z, 1, 1);
            Color sum = Color.black; float tw = 0f;
            for (int i = 0; i < layers.Length && i < w.GetLength(2); i++)
            {
                float wi = w[0, 0, i];
                if (wi <= 0.001f || layers[i] == null) continue;
                Color lc = layers[i].diffuseTexture != null ? Average(layers[i].diffuseTexture) : Color.gray;
                Vector4 rm = layers[i].diffuseRemapMax;
                lc = new Color(lc.r * rm.x, lc.g * rm.y, lc.b * rm.z, 1f);
                sum += lc * wi; tw += wi;
            }
            if (tw <= 0f) return false;
            c = sum / tw; c.a = 1f;
            return true;
        }

        /// <summary>Average color of any texture (readable or not) via an 8x8 GPU downsample. Cached per texture.</summary>
        private static Color Average(Texture tex)
        {
            if (tex == null) return Color.white;
            if (s_avg.TryGetValue(tex, out Color cached)) return cached;
            RenderTexture rt = RenderTexture.GetTemporary(8, 8, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture prev = RenderTexture.active;
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;
            var read = new Texture2D(8, 8, TextureFormat.RGBA32, false, false);
            read.ReadPixels(new Rect(0, 0, 8, 8), 0, 0);
            read.Apply(false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            Color[] px = read.GetPixels();
            Object.Destroy(read);
            Color sum = Color.black;
            for (int i = 0; i < px.Length; i++) sum += px[i];
            Color avg = sum / Mathf.Max(1, px.Length); avg.a = 1f;
            s_avg[tex] = avg;
            return avg;
        }

        public static void Apply(DMCarveSettings s, MeshRenderer mr, bool sampled, Color surface, System.Random rng)
        {
            if (mr == null) return;
            if (s_mpb == null) s_mpb = new MaterialPropertyBlock();
            s_mpb.Clear();
            if (s == null || !s.debrisLookEnabled) { mr.SetPropertyBlock(null); return; }

            Material m = mr.sharedMaterial;
            Color baseC = m != null && m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : Color.white;
            Color c = sampled ? Color.Lerp(baseC, surface, s.debrisTintStrength) : baseC;

            Color.RGBToHSV(c, out float h, out float sat, out float v);
            float var = s.debrisColorVariance;
            double r1 = rng != null ? rng.NextDouble() : Random.value, r2 = rng != null ? rng.NextDouble() : Random.value;
            h = Mathf.Repeat(h + ((float)r1 - 0.5f) * var * 0.15f, 1f);
            v = Mathf.Clamp01(v * s.debrisBrightness * (1f + ((float)r2 - 0.5f) * 2f * var));
            sat = Mathf.Clamp01(sat * s.debrisSaturation);
            c = Color.HSVToRGB(h, sat, v); c.a = 1f;
            s_mpb.SetColor(BaseColorId, c);

            Texture2D overlay = Overlay(s.debrisOverlayTexture, s.debrisOverlayStrength);
            if (overlay != null)
            {
                s_mpb.SetTexture(BaseMapId, overlay);
                float tl = s.debrisOverlayTiling;
                s_mpb.SetVector(BaseMapStId, new Vector4(tl, tl, (float)r1, (float)r2));
            }
            mr.SetPropertyBlock(s_mpb);
        }

        /// <summary>lerp(white, overlay, strength) as a small cached readable texture (source texture or generated noise).</summary>
        private static Texture2D Overlay(Texture2D src, float strength)
        {
            if (strength <= 0.001f) return null;
            int q = Mathf.RoundToInt(strength * 50f);
            long key = ((long)(src != null ? src.GetHashCode() : 0) << 8) ^ q;
            if (s_overlay.TryGetValue(key, out Texture2D tex) && tex != null) return tex;

            const int N = 256;
            Color[] gray = new Color[N * N];
            if (src != null)
            {
                RenderTexture rt = RenderTexture.GetTemporary(N, N, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                RenderTexture prev = RenderTexture.active;
                Graphics.Blit(src, rt);
                RenderTexture.active = rt;
                var read = new Texture2D(N, N, TextureFormat.RGBA32, false, true);
                read.ReadPixels(new Rect(0, 0, N, N), 0, 0);
                read.Apply(false);
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                gray = read.GetPixels();
                Object.Destroy(read);
            }
            else
            {
                // Tileable multi-octave value noise (periodic lattice) - rough rock grain.
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float n = 0f, amp = 0.5f, tot = 0f;
                        for (int o = 0; o < 5; o++)
                        {
                            int per = 4 << o;
                            n += amp * PeriodicNoise(x * per / (float)N, y * per / (float)N, per, o * 17);
                            tot += amp; amp *= 0.55f;
                        }
                        float g = n / tot;
                        gray[y * N + x] = new Color(g, g, g, 1f);
                    }
            }

            // Normalize to mean 1-ish brightness so the sampled color stays recognisable, then blend toward white.
            float mean = 0f;
            for (int i = 0; i < gray.Length; i++) mean += gray[i].grayscale;
            mean = Mathf.Max(0.05f, mean / gray.Length);
            var outPx = new Color[gray.Length];
            for (int i = 0; i < gray.Length; i++)
            {
                float g = Mathf.Clamp01(gray[i].grayscale / mean * 0.85f);
                float k = Mathf.Lerp(1f, g, strength);
                outPx[i] = new Color(k, k, k, 1f);
            }
            tex = new Texture2D(N, N, TextureFormat.RGBA32, true, false) { name = "DMDebrisOverlay", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, hideFlags = HideFlags.DontSave };
            tex.SetPixels(outPx);
            tex.Apply(true, false);
            s_overlay[key] = tex;
            return tex;
        }

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }

        private static float PeriodicNoise(float x, float y, int period, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            int ax = ((x0 % period) + period) % period, bx = (ax + 1) % period;
            int ay = ((y0 % period) + period) % period, by = (ay + 1) % period;
            float a = Hash(ax, ay, seed), b = Hash(bx, ay, seed), c = Hash(ax, by, seed), d = Hash(bx, by, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }
    }
}

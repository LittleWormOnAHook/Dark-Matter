using System.Collections.Generic;
using Project.SurfaceCarve;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-20v: runtime fix-up for pooled world impact VFX (called from DMCombatFx.SpawnPooled on every spawn/reuse).
    ///  - Pink root cause: third-party particle materials on built-in shaders (e.g. JMO WarFX "WFX/Additive Alpha8",
    ///    "WFX/Scroll/Smoke" in WFX_ExplosiveSmokeGround Big Alt) have no HDRP pass -> magenta. Each such source material
    ///    gets ONE cached runtime HDRP copy (Unlit transparent additive / alpha, texture + tint carried over). Assets untouched.
    ///  - Debris-like particles (mesh particles, or names containing debris/chunk/rock/dirt/stone/dots) use a cached
    ///    HDRP Lit alpha-clipped copy, tinted per spawn with the hit surface color (DMCarveDebrisLook sampling) via a
    ///    MaterialPropertyBlock that is rewritten every reuse.
    ///  - Explosion ring: horizontal-billboard rings / glow circles / shockwaves are hidden unless DMCarveSettings.explosionRing.
    /// </summary>
    public static class DMImpactVfxFixer
    {
        private static readonly Dictionary<Material, Material> s_unlit = new Dictionary<Material, Material>();
        private static readonly Dictionary<Material, Material> s_lit = new Dictionary<Material, Material>();
        private static readonly Dictionary<Renderer, Material> s_original = new Dictionary<Renderer, Material>();
        private static readonly List<ParticleSystemRenderer> s_buf = new List<ParticleSystemRenderer>();
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static MaterialPropertyBlock s_mpb;
        private static GameObject s_receiver;
        private static Vector3 s_point, s_normal;
        private static float s_ctxTime = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { s_unlit.Clear(); s_lit.Clear(); s_original.Clear(); s_mpb = null; s_receiver = null; s_ctxTime = -1f; }

        private static bool s_explosive;

        /// <summary>Only explosive (splash) impacts are processed; regular carving ammo VFX are left exactly as authored.</summary>
        public static void SetContext(GameObject receiver, Vector3 point, Vector3 normal, bool explosive)
        {
            s_receiver = receiver; s_point = point; s_normal = normal; s_ctxTime = Time.time; s_explosive = explosive;
        }

        public static void Process(GameObject instance)
        {
            if (instance == null) return;
            DMCarveSettings s = DMCarveSettings.Active;
            if (s == null) return;
            bool ctx = Time.time - s_ctxTime < 0.05f;
            if (!ctx || !s_explosive) return;
            bool sampled = false; Color surface = Color.gray;
            if (ctx && s.impactDebrisSurfaceColor)
                sampled = DMCarveDebrisLook.TrySample(s, s_receiver, s_point, s_normal, out surface);

            instance.GetComponentsInChildren(true, s_buf);
            for (int i = 0; i < s_buf.Count; i++)
            {
                ParticleSystemRenderer r = s_buf[i];
                if (r == null) continue;

                // Ring toggle (re-evaluated each reuse).
                if (IsRing(r)) { r.enabled = s.explosionRing; if (!s.explosionRing) continue; }

                if (!s_original.TryGetValue(r, out Material src)) { src = r.sharedMaterial; s_original[r] = src; }
                if (src == null) continue;
                bool debris = s.impactDebrisSurfaceColor && IsDebris(r, src);
                bool nonHdrp = !IsHdrp(src.shader);

                if (debris && (nonHdrp || s.impactVfxHdrpFix))
                {
                    r.sharedMaterial = LitCopy(src);
                    if (s_mpb == null) s_mpb = new MaterialPropertyBlock();
                    s_mpb.Clear();
                    Color c = sampled ? surface : Tint(src);
                    c.a = 1f;
                    s_mpb.SetColor(BaseColorId, c * Mathf.Max(0f, s.debrisBrightness));
                    r.SetPropertyBlock(s_mpb);
                }
                else
                {
                    r.SetPropertyBlock(null); // clear any stale tint from a previous reuse
                    r.sharedMaterial = nonHdrp && s.impactVfxHdrpFix ? UnlitCopy(src) : src;
                }
            }
            s_buf.Clear();
        }

        private static bool IsHdrp(Shader sh)
        {
            if (sh == null) return false;
            string n = sh.name;
            return n.StartsWith("HDRP/") || n.StartsWith("Shader Graphs/") || n.StartsWith("HDRenderPipeline") || n.Contains("HDRP");
        }

        private static bool IsRing(ParticleSystemRenderer r)
        {
            string n = (r.name + " " + (r.sharedMaterial != null ? r.sharedMaterial.name : "") + " " + (s_original.TryGetValue(r, out Material o) && o != null ? o.name : "")).ToLowerInvariant();
            bool ringName = n.Contains("ring") || n.Contains("shockwave") || n.Contains("shock wave") || n.Contains("glowcircle") || n.Contains("circle");
            return ringName && (r.renderMode == ParticleSystemRenderMode.HorizontalBillboard || n.Contains("ring") || n.Contains("shockwave"));
        }

        private static bool IsDebris(ParticleSystemRenderer r, Material m)
        {
            if (r.renderMode == ParticleSystemRenderMode.Mesh) return true;
            string n = (r.name + " " + m.name).ToLowerInvariant();
            return n.Contains("debris") || n.Contains("chunk") || n.Contains("rock") || n.Contains("dirt") || n.Contains("stone") || n.Contains("dots");
        }

        private static Texture MainTex(Material m)
        {
            foreach (string p in new[] { "_MainTex", "_BaseColorMap", "_UnlitColorMap", "_BaseMap" })
                if (m.HasProperty(p) && m.GetTexture(p) != null) return m.GetTexture(p);
            return null;
        }

        private static Color Tint(Material m)
        {
            foreach (string p in new[] { "_TintColor", "_Color", "_BaseColor", "_UnlitColor" })
                if (m.HasProperty(p)) return m.GetColor(p);
            return Color.white;
        }

        private static bool Additive(Material m)
        {
            string n = (m.shader != null ? m.shader.name : "") + " " + m.name;
            return n.IndexOf("Add", System.StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("Glow", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Material UnlitCopy(Material src)
        {
            if (s_unlit.TryGetValue(src, out Material m) && m != null) return m;
            Shader sh = Shader.Find("HDRP/Unlit");
            if (sh == null) return src;
            m = new Material(sh) { name = src.name + " (HDRP runtime)", hideFlags = HideFlags.DontSave };
            Texture t = MainTex(src);
            Color c = Tint(src);
            if (t != null) m.SetTexture("_UnlitColorMap", t);
            m.SetColor("_UnlitColor", c);
            m.SetFloat("_SurfaceType", 1f);
            m.SetFloat("_BlendMode", Additive(src) ? 1f : 0f); // HDRP: 0 alpha, 1 additive
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_DoubleSidedEnable", 1f);
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            HDMaterial.ValidateMaterial(m);
            s_unlit[src] = m;
            return m;
        }

        private static Material LitCopy(Material src)
        {
            if (s_lit.TryGetValue(src, out Material m) && m != null) return m;
            Shader sh = Shader.Find("HDRP/Lit");
            if (sh == null) return src;
            m = new Material(sh) { name = src.name + " (HDRP Lit debris)", hideFlags = HideFlags.DontSave };
            Texture t = MainTex(src);
            if (t != null) m.SetTexture("_BaseColorMap", t);
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_AlphaCutoffEnable", 1f);
            m.SetFloat("_AlphaCutoff", 0.35f);
            m.SetFloat("_Smoothness", 0.15f);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_DoubleSidedEnable", 1f);
            HDMaterial.ValidateMaterial(m);
            s_lit[src] = m;
            return m;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace GenesisPCG.RockCreation
{
    /// <summary>
    /// Per-pixel terrain match for the Rock Blend shader: binds the Terrain under a rock (control / splat maps, up to 8
    /// layers with TerrainLit's tiling, tint, mask remap and height blend) to the rock's renderers (LODs included) through a
    /// MaterialPropertyBlock, so the faded base shows exactly the ground texture beneath every pixel. Re-bound whenever
    /// the rock is moved, rebuilt or baked (and on enable, so it also works in builds). Materials opt in with
    /// "Match Terrain Per Pixel" (_GRB_TerrainPerPixel); without a terrain the baked dominant layer is used.
    /// Renderers with a property block are drawn outside the SRP Batcher (classic path), so in Play Mode baked rocks get
    /// shared runtime copies of their materials instead (one per material content + terrain, values in the material
    /// constant buffer): identical materials collapse into one, no property blocks, SRP Batcher / GPU Resident Drawer
    /// compatible. Edit Mode keeps the property block path (nothing is written to assets or scenes).
    /// </summary>
    public static class PcgTerrainSplat
    {
        public const int MaxLayers = 8;
        private static readonly int P_Params = Shader.PropertyToID("_GRB_TParams");
        private static readonly int P_Rect = Shader.PropertyToID("_GRB_TRect");
        private static readonly int P_ControlTS = Shader.PropertyToID("_GRB_TControlTS");
        private static readonly int P_Control0 = Shader.PropertyToID("_GRB_TControl0");
        private static readonly int P_Control1 = Shader.PropertyToID("_GRB_TControl1");
        private static readonly int P_HMap = Shader.PropertyToID("_GRB_THeightMap");
        private static readonly int P_HParams = Shader.PropertyToID("_GRB_THeightP");
        private static readonly int P_HTS = Shader.PropertyToID("_GRB_THeightTS");
        /// <summary>TerrainData.heightmapTexture stores normalized heights scaled by kMaxHeight (32766) / 65535.</summary>
        public const float HeightmapUnitToNormalized = 65535f / 32766f;
        private static readonly int[] P_Alb = new int[MaxLayers], P_Nrm = new int[MaxLayers], P_Msk = new int[MaxLayers];
        // Per-layer vectors (_GRB_TST0.._GRB_TST7 ...): one property each, as the SRP Batcher needs (no arrays).
        private static readonly int[] P_ST = new int[MaxLayers], P_Diff = new int[MaxLayers], P_MaskOff = new int[MaxLayers],
            P_MaskScale = new int[MaxLayers], P_Layer = new int[MaxLayers];

        static PcgTerrainSplat()
        {
            for (int i = 0; i < MaxLayers; i++)
            {
                P_Alb[i] = Shader.PropertyToID("_GRB_TAlb" + i);
                P_Nrm[i] = Shader.PropertyToID("_GRB_TNrm" + i);
                P_Msk[i] = Shader.PropertyToID("_GRB_TMsk" + i);
                P_ST[i] = Shader.PropertyToID("_GRB_TST" + i);
                P_Diff[i] = Shader.PropertyToID("_GRB_TDiff" + i);
                P_MaskOff[i] = Shader.PropertyToID("_GRB_TMaskOff" + i);
                P_MaskScale[i] = Shader.PropertyToID("_GRB_TMaskScale" + i);
                P_Layer[i] = Shader.PropertyToID("_GRB_TLayer" + i);
            }
        }

        /// <summary>The terrain whose XZ footprint contains <paramref name="p"/> (closest surface height wins when terrains overlap).</summary>
        public static Terrain FindTerrain(Vector3 p)
        {
            Terrain best = null;
            float bestD = float.MaxValue;
            foreach (Terrain t in Terrain.activeTerrains)
            {
                if (t == null || t.terrainData == null || !t.isActiveAndEnabled) continue;
                Vector3 tp = t.GetPosition(), sz = t.terrainData.size;
                if (p.x < tp.x || p.z < tp.z || p.x > tp.x + sz.x || p.z > tp.z + sz.z) continue;
                float d = Mathf.Abs(t.SampleHeight(p) + tp.y - p.y);
                if (d < bestD) { bestD = d; best = t; }
            }
            return best;
        }

        /// <summary>
        /// True when the material is a Rock Blend material that wants the terrain bound (base blend or ground fade on): the
        /// terrain heightmap is always used for the height above ground (fade / contact AO / base blend line); the splat maps
        /// are only sampled when "Match Terrain Per Pixel" is on (gated in the shader).
        /// </summary>
        public static bool Wants(Material m)
        {
            if (m == null || !m.HasProperty("_GRB_TerrainPerPixel")) return false;
            bool bottom = m.HasProperty("_GRB_BottomEnable") && m.GetFloat("_GRB_BottomEnable") > 0.5f;
            bool fade = m.HasProperty("_GRB_FadeEnable") && m.GetFloat("_GRB_FadeEnable") > 0.5f;
            return bottom || fade;
        }

        private static readonly List<Renderer> s_rs = new List<Renderer>();
        private static MaterialPropertyBlock s_mpb;

        /// <summary>Play Mode: bind baked rocks through shared runtime materials (default) instead of property blocks.
        /// Change it, then call <see cref="RebindAll"/>, to switch every rock (A/B tests).</summary>
        public static bool SharedMaterialsInPlay = true;
        private static readonly Dictionary<string, Material> s_runtime = new Dictionary<string, Material>();
        private static readonly Dictionary<Material, Material> s_sourceOf = new Dictionary<Material, Material>();
        private static readonly Dictionary<Material, string> s_hash = new Dictionary<Material, string>();
        private static readonly Dictionary<Material, Terrain> s_terrainOf = new Dictionary<Material, Terrain>();
        private static readonly Dictionary<Material, Material> s_originOf = new Dictionary<Material, Material>();
        private static int s_splits;
        /// <summary>Shared runtime materials alive this Play session.</summary>
        public static int RuntimeMaterialCount => s_runtime.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime()
        {
            // Domain reload is off: drop last session's copies (terrain data may have changed).
            foreach (Material m in s_runtime.Values)
                if (m != null) Object.Destroy(m);
            s_runtime.Clear();
            s_sourceOf.Clear();
            s_hash.Clear();
            s_terrainOf.Clear();
            s_originOf.Clear();
#if UNITY_EDITOR
            s_dirty.Clear();
#endif
        }

        /// <summary>Re-binds every enabled rock (after toggling <see cref="SharedMaterialsInPlay"/> or editing a source material).</summary>
        public static void RebindAll()
        {
            foreach (DmRockCombiner c in Object.FindObjectsByType<DmRockCombiner>(FindObjectsInactive.Exclude))
                Apply(c.gameObject);
        }

        /// <summary>Binds the terrain under <paramref name="root"/> to all its renderers (or clears the binding when no material wants it).</summary>
        public static Terrain Apply(GameObject root)
        {
            if (root == null) return null;
            s_rs.Clear();
            root.GetComponentsInChildren(true, s_rs);
            bool want = false;
            foreach (Renderer r in s_rs)
            {
                foreach (Material m in r.sharedMaterials)
                    if (Wants(m)) { want = true; break; }
                if (want) break;
            }
            s_mpb ??= new MaterialPropertyBlock();
            Terrain t = want ? FindTerrain(root.transform.position) : null;
            bool shared = want && Application.isPlaying && SharedMaterialsInPlay && IsBakedRock(root);
            foreach (Renderer r in s_rs)
            {
                if (!(r is MeshRenderer)) continue;
                if (shared)
                {
                    BindShared(r, t);
                    continue;
                }
                RestoreSources(r);
                if (!want)
                {
                    if (r.HasPropertyBlock()) { r.GetPropertyBlock(s_mpb); if (s_mpb.HasVector(P_Params)) r.SetPropertyBlock(null); }
                    continue;
                }
                r.GetPropertyBlock(s_mpb);
                Fill(s_mpb, t);
                r.SetPropertyBlock(s_mpb);
            }
            s_rs.Clear();
            return t;
        }

        private static bool IsBakedRock(GameObject root)
        {
            var c = root.GetComponent<DmRockCombiner>();
            return c != null && c.IsBaked;
        }

        private static void BindShared(Renderer r, Terrain t)
        {
            Material[] mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null) continue;
                Material src = s_sourceOf.TryGetValue(m, out Material s0) && s0 != null ? s0 : m;
                if (!Wants(src)) continue;
                Material v = RuntimeVariant(src, t);
                if (v != null && v != m) { mats[i] = v; changed = true; }
            }
            if (changed) r.sharedMaterials = mats;
            if (r.HasPropertyBlock()) { r.GetPropertyBlock(s_mpb); if (s_mpb.HasVector(P_Params)) r.SetPropertyBlock(null); }
        }

        /// <summary>Puts the source materials back where shared runtime copies were assigned.</summary>
        private static void RestoreSources(Renderer r)
        {
            if (s_sourceOf.Count == 0) return;
            Material[] mats = r.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] != null && s_sourceOf.TryGetValue(mats[i], out Material src) && src != null) { mats[i] = src; changed = true; }
            if (changed) r.sharedMaterials = mats;
        }

        private static Material RuntimeVariant(Material src, Terrain t)
        {
            if (s_sourceOf.TryGetValue(src, out Material self) && self == src)
            {
                // A copy split off by a Play Mode edit (Diverge) is its own source: keep it, follow the terrain.
                if (!s_terrainOf.TryGetValue(src, out Terrain st) || st != t) { Fill(src, t); s_terrainOf[src] = t; }
                return src;
            }
            if (!s_hash.TryGetValue(src, out string h)) s_hash[src] = h = ContentHash(src);
            string key = h + "#" + (t != null ? t.GetHashCode() : 0);
            if (s_runtime.TryGetValue(key, out Material v) && v != null) return v;
            v = new Material(src) { name = src.name + " (Play shared)", hideFlags = HideFlags.DontSave };
            Fill(v, t);
            s_runtime[key] = v;
            s_sourceOf[v] = src;
            s_terrainOf[v] = t;
            s_originOf[v] = src;
#if UNITY_EDITOR
            WatchSource(src);
#endif
            return v;
        }

        /// <summary>Play Mode runtime copies alive now, each with the asset material it was made from (edits on them are kept on exit).</summary>
        public static List<KeyValuePair<Material, Material>> RuntimeCopies()
        {
            var l = new List<KeyValuePair<Material, Material>>();
            foreach (var kv in s_originOf)
                if (kv.Key != null && kv.Value != null) l.Add(new KeyValuePair<Material, Material>(kv.Key, kv.Value));
            return l;
        }

        /// <summary>True for a Play Mode runtime copy of a rock material (shared, or split off by an edit).</summary>
        public static bool IsRuntimeCopy(Material m) => m != null && s_sourceOf.ContainsKey(m);

        /// <summary>
        /// Play Mode: <paramref name="copy"/> is a runtime copy made from <paramref name="source"/> or from a material with the
        /// same content (shared copies are keyed by content, so one copy stands for every identical source).
        /// </summary>
        public static bool IsRuntimeCopyOf(Material copy, Material source)
        {
            if (!Application.isPlaying || copy == null || source == null || !s_originOf.TryGetValue(copy, out Material o) || o == null) return false;
            if (o == source) return true;
            if (!s_hash.TryGetValue(o, out string ho)) s_hash[o] = ho = ContentHash(o);
            if (!s_hash.TryGetValue(source, out string hs)) s_hash[source] = hs = ContentHash(source);
            return ho == hs;
        }

        /// <summary>
        /// Play Mode: a shared runtime copy was edited for some of the rocks drawing it (<paramref name="roots"/>). The edited
        /// values move to a new copy used by those rocks only (copies of them share it), and the shared copy goes back to its
        /// source's values for the other rocks (or <paramref name="restore"/>'s: the shared copy before this edit, so earlier
        /// Play Mode edits made for every rock stay). Returns the new copy (null: not a runtime copy).
        /// </summary>
        public static Material Diverge(Material shared, IEnumerable<GameObject> roots, Material restore = null)
        {
            if (shared == null || roots == null || !s_sourceOf.TryGetValue(shared, out Material src) || src == null) return null;
            var split = new Material(shared) { name = src.name.Replace(" (Play edited)", "") + " (Play edited)", hideFlags = HideFlags.DontSave };
            s_sourceOf[split] = split;
            s_originOf[split] = s_originOf.TryGetValue(shared, out Material o) && o != null ? o : src;
            s_terrainOf[split] = s_terrainOf.TryGetValue(shared, out Terrain st) ? st : null;
            s_runtime["split#" + (++s_splits)] = split;
            var rs = new List<Renderer>();
            foreach (GameObject root in roots)
            {
                if (root == null) continue;
                root.GetComponentsInChildren(true, rs);
                foreach (Renderer r in rs)
                {
                    Material[] mats = r.sharedMaterials;
                    bool changed = false;
                    for (int i = 0; i < mats.Length; i++)
                        if (mats[i] == shared) { mats[i] = split; changed = true; }
                    if (!changed) continue;
#if UNITY_EDITOR
                    // Same undo step as the material edit: undoing it puts the rocks back on the shared copy.
                    UnityEditor.Undo.RecordObject(r, "Play Mode material edit");
#endif
                    r.sharedMaterials = mats;
                }
            }
            Material back = restore != null ? restore : src;
            if (back != shared)
            {
                if (shared.shader != back.shader) shared.shader = back.shader;
                shared.CopyPropertiesFromMaterial(back);
                shared.shaderKeywords = back.shaderKeywords;
                shared.renderQueue = back.renderQueue;
                Fill(shared, s_terrainOf.TryGetValue(shared, out Terrain t) ? t : null);
            }
            return split;
        }

        /// <summary>Shader + keywords + queue + every property value: equal hashes render identically.</summary>
        private static string ContentHash(Material m)
        {
            var sb = new System.Text.StringBuilder(2048);
            sb.Append(m.shader != null ? m.shader.name : "-").Append('|').Append(m.renderQueue).Append('|');
            string[] kw = m.shaderKeywords;
            System.Array.Sort(kw, System.StringComparer.Ordinal);
            sb.Append(string.Join(",", kw)).Append('|');
            foreach (string n in m.GetPropertyNames(MaterialPropertyType.Float)) sb.Append(n).Append('=').Append(m.GetFloat(n).ToString("R")).Append(';');
            foreach (string n in m.GetPropertyNames(MaterialPropertyType.Int)) sb.Append(n).Append('=').Append(m.GetInteger(n)).Append(';');
            foreach (string n in m.GetPropertyNames(MaterialPropertyType.Vector)) sb.Append(n).Append('=').Append(m.GetVector(n).ToString("R")).Append(';');
            foreach (string n in m.GetPropertyNames(MaterialPropertyType.Texture))
            {
                Texture tx = m.GetTexture(n);
                sb.Append(n).Append('=').Append(tx != null ? tx.GetHashCode() : 0).Append(m.GetTextureScale(n).ToString("R")).Append(m.GetTextureOffset(n).ToString("R")).Append(';');
            }
            return sb.ToString();
        }

#if UNITY_EDITOR
        // Play Mode material tweaks: a source edited in the inspector re-keys and re-binds every rock.
        private static readonly Dictionary<Material, int> s_dirty = new Dictionary<Material, int>();
        private static bool s_watching;
        private static double s_nextCheck;

        private static void WatchSource(Material src)
        {
            s_dirty[src] = UnityEditor.EditorUtility.GetDirtyCount(src);
            if (s_watching) return;
            s_watching = true;
            UnityEditor.EditorApplication.update += CheckSources;
        }

        private static void CheckSources()
        {
            if (!Application.isPlaying || s_dirty.Count == 0) return;
            double now = UnityEditor.EditorApplication.timeSinceStartup;
            if (now < s_nextCheck) return;
            s_nextCheck = now + 0.4;
            bool any = false;
            foreach (Material src in new List<Material>(s_dirty.Keys))
            {
                if (src == null) { s_dirty.Remove(src); continue; }
                int d = UnityEditor.EditorUtility.GetDirtyCount(src);
                if (d == s_dirty[src]) continue;
                s_dirty[src] = d;
                s_hash.Remove(src);
                any = true;
            }
            if (any) RebindAll();
        }
#endif

        private static readonly Vector4[] s_st = new Vector4[MaxLayers], s_diff = new Vector4[MaxLayers], s_mo = new Vector4[MaxLayers],
            s_ms = new Vector4[MaxLayers], s_li = new Vector4[MaxLayers];

        private interface ISink { void Vec(int id, Vector4 v); void Tex(int id, Texture t); }
        private struct BlockSink : ISink
        {
            public MaterialPropertyBlock b;
            public void Vec(int id, Vector4 v) => b.SetVector(id, v);
            public void Tex(int id, Texture t) => b.SetTexture(id, t);
        }
        private struct MaterialSink : ISink
        {
            public Material m;
            public void Vec(int id, Vector4 v) => m.SetVector(id, v);
            public void Tex(int id, Texture t) => m.SetTexture(id, t);
        }

        public static void Fill(MaterialPropertyBlock b, Terrain t)
        {
            var s = new BlockSink { b = b };
            FillTo(ref s, t);
        }

        /// <summary>Writes the terrain binding into a material (shared runtime copies).</summary>
        public static void Fill(Material m, Terrain t)
        {
            var s = new MaterialSink { m = m };
            FillTo(ref s, t);
        }

        private static void FillTo<TS>(ref TS b, Terrain t) where TS : struct, ISink
        {
            TerrainData td = t != null ? t.terrainData : null;
            TerrainLayer[] layers = td != null ? td.terrainLayers : null;
            Texture2D[] ctl = td != null ? td.alphamapTextures : null;
            // Terrain height under every pixel (ground fade / contact AO / base blend measured from the real ground).
            RenderTexture hm = td != null ? td.heightmapTexture : null;
            if (hm != null)
            {
                Vector3 hp = t.GetPosition(), hs = td.size;
                b.Vec(P_Rect, new Vector4(hp.x, hp.z, 1f / Mathf.Max(1e-3f, hs.x), 1f / Mathf.Max(1e-3f, hs.z)));
                b.Tex(P_HMap, hm);
                b.Vec(P_HTS, new Vector4(1f / hm.width, 1f / hm.height, hm.width, hm.height));
                b.Vec(P_HParams, new Vector4(hp.y, hs.y * HeightmapUnitToNormalized, 1f, 0f));
            }
            else
            {
                b.Tex(P_HMap, Texture2D.blackTexture);
                b.Vec(P_HParams, Vector4.zero);
            }
            if (td == null || layers == null || layers.Length == 0 || ctl == null || ctl.Length == 0 || ctl[0] == null)
            {
                b.Vec(P_Params, Vector4.zero);
                return;
            }
            int n = Mathf.Min(MaxLayers, layers.Length);
            Material tm = t.materialTemplate;
            bool hb = tm != null && tm.HasProperty("_EnableHeightBlend") && tm.GetFloat("_EnableHeightBlend") > 0.5f;
            float tr = tm != null && tm.HasProperty("_HeightTransition") ? tm.GetFloat("_HeightTransition") : 0f;
            Vector3 tp = t.GetPosition(), sz = td.size;
            b.Vec(P_Rect, new Vector4(tp.x, tp.z, 1f / Mathf.Max(1e-3f, sz.x), 1f / Mathf.Max(1e-3f, sz.z)));
            b.Tex(P_Control0, ctl[0]);
            b.Tex(P_Control1, ctl.Length > 1 && ctl[1] != null ? ctl[1] : Texture2D.blackTexture);
            b.Vec(P_ControlTS, new Vector4(1f / ctl[0].width, 1f / ctl[0].height, ctl[0].width, ctl[0].height));
            bool anyMask = false;
            for (int i = 0; i < MaxLayers; i++)
            {
                TerrainLayer l = i < n ? layers[i] : null;
                if (l == null)
                {
                    s_st[i] = new Vector4(1, 1, 0, 0); s_diff[i] = Vector4.one; s_mo[i] = Vector4.zero; s_ms[i] = Vector4.one; s_li[i] = new Vector4(0, 0, 0, 1);
                    b.Tex(P_Alb[i], Texture2D.whiteTexture); b.Tex(P_Nrm[i], Texture2D.normalTexture); b.Tex(P_Msk[i], Texture2D.whiteTexture);
                    continue;
                }
                Vector2 ts = new Vector2(Mathf.Max(0.01f, l.tileSize.x), Mathf.Max(0.01f, l.tileSize.y));
                s_st[i] = new Vector4(sz.x / ts.x, sz.z / ts.y, l.tileOffset.x / ts.x, l.tileOffset.y / ts.y); // TerrainLit _SplatN_ST
                s_diff[i] = l.diffuseRemapMax;                                                                // _DiffuseRemapScaleN
                s_mo[i] = l.maskMapRemapMin;                                                                  // _MaskMapRemapOffsetN
                s_ms[i] = l.maskMapRemapMax - l.maskMapRemapMin;                                              // _MaskMapRemapScaleN
                bool hasMask = l.maskMapTexture != null;
                anyMask |= hasMask;
                s_li[i] = new Vector4(hasMask ? 1f : 0f, l.metallic, l.smoothness, l.normalScale);
                b.Tex(P_Alb[i], l.diffuseTexture != null ? l.diffuseTexture : Texture2D.whiteTexture);
                b.Tex(P_Nrm[i], l.normalMapTexture != null ? l.normalMapTexture : Texture2D.normalTexture);
                b.Tex(P_Msk[i], hasMask ? l.maskMapTexture : Texture2D.whiteTexture);
            }
            for (int i = 0; i < MaxLayers; i++)
            {
                b.Vec(P_ST[i], s_st[i]);
                b.Vec(P_Diff[i], s_diff[i]);
                b.Vec(P_MaskOff[i], s_mo[i]);
                b.Vec(P_MaskScale[i], s_ms[i]);
                b.Vec(P_Layer[i], s_li[i]);
            }
            b.Vec(P_Params, new Vector4(1f, n, hb && anyMask ? 1f : 0f, tr));
        }
    }
}

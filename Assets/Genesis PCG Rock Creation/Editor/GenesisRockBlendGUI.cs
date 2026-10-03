#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>
    /// Material UI for "Genesis PCG/Rock Blend Lit": HDRP's own Lit UI (surface options, base maps, keywords, render
    /// states) plus the Rock Blend section (layer set picker, top layer, terrain blend, strata).
    /// </summary>
    public sealed class GenesisRockBlendGUI : ShaderGUI
    {
        private static Type s_litType;
        private ShaderGUI m_lit;
        private static bool s_fold = true;

        private static ShaderGUI CreateLit()
        {
            if (s_litType == null)
                s_litType = Type.GetType("UnityEditor.Rendering.HighDefinition.LitGUI, Unity.RenderPipelines.HighDefinition.Editor");
            if (s_litType == null) return null;
            try { return (ShaderGUI)Activator.CreateInstance(s_litType, true); }
            catch { return null; }
        }

        /// <summary>Sets HDRP Lit keywords / render states for a Rock Blend material (same validation as HDRP/Lit).</summary>
        public static void Validate(Material m)
        {
            if (m == null) return;
            // Vertex ranges are fixed by the mesh encoding (PcgSurfaceSnap); the shader decodes with the same constants.
            if (m.HasProperty("_GRB_HeightRange")) m.SetFloat("_GRB_HeightRange", PcgSurfaceSnap.VertexHeightRange);
            if (m.HasProperty("_GRB_SizeRange")) m.SetFloat("_GRB_SizeRange", PcgSurfaceSnap.VertexSizeRange);
            SyncGroundFade(m);
            SyncTopMaps(m);
            ShaderGUI lit = CreateLit();
            if (lit != null) lit.ValidateMaterial(m);
        }

        /// <summary>
        /// Top layer mask / AO maps are sampled only when assigned: the shader branches on these auto flags (material
        /// constants, SRP Batcher safe) so materials without the maps render exactly as before.
        /// </summary>
        public static void SyncTopMaps(Material m)
        {
            if (m == null) return;
            if (m.HasProperty("_GRB_TopMaskOn") && m.HasProperty("_GRB_TopMaskMap"))
                m.SetFloat("_GRB_TopMaskOn", m.GetTexture("_GRB_TopMaskMap") != null ? 1f : 0f);
            if (m.HasProperty("_GRB_TopAOOn") && m.HasProperty("_GRB_TopAOMap"))
                m.SetFloat("_GRB_TopAOOn", m.GetTexture("_GRB_TopAOMap") != null ? 1f : 0f);
        }

        private const string FadeTag = "GRB_FadeAlphaClip";

        /// <summary>
        /// Ground Fade needs HDRP's alpha test (_ALPHATEST_ON, alpha-tested queue, depth prepass + depth-equal GBuffer) on the
        /// materials that use it, and only on those: enabling the fade turns Alpha Clipping on (cutoff 0.5; the shader feeds it
        /// 0/1 so the atlas alpha is ignored), disabling it turns Alpha Clipping off again only if the fade had turned it on.
        /// </summary>
        public static void SyncGroundFade(Material m)
        {
            if (m == null || !m.HasProperty("_GRB_FadeEnable") || !m.HasProperty("_AlphaCutoffEnable")) return;
            bool fade = m.GetFloat("_GRB_FadeEnable") > 0.5f;
            if (fade)
            {
                if (m.GetFloat("_AlphaCutoffEnable") < 0.5f || m.GetTag(FadeTag, false, "") != "1")
                {
                    m.SetFloat("_AlphaCutoffEnable", 1f);
                    m.SetFloat("_AlphaCutoff", 0.5f);
                    m.SetOverrideTag(FadeTag, "1");
                }
            }
            else if (m.GetTag(FadeTag, false, "") == "1")
            {
                m.SetFloat("_AlphaCutoffEnable", 0f);
                m.SetOverrideTag(FadeTag, "");
            }
        }

        /// <summary>Switches a Rock Blend material between the deferred (default) and the forward-only shader.</summary>
        public static void SetForwardOnly(Material m, bool forward)
        {
            if (m == null || !DmRockBlendMaterials.IsBlendMaterial(m)) return;
            Shader target = Shader.Find(forward ? DmRockBlendMaterials.ForwardShaderName : DmRockBlendMaterials.ShaderName);
            if (target == null || m.shader == target) return;
            int queue = m.renderQueue;
            m.shader = target;
            m.renderQueue = queue;
            Validate(m);
        }

        // ------------------------------------------------------------------------------------------------
        // Layout: feature toggles first, then the adjustments grouped per feature (greyed out while the feature is off),
        // then HDRP's Lit UI. Layout only: every property is drawn with MaterialEditor.ShaderProperty as before (mixed
        // values and multi-material editing come from the MaterialEditor), no keyword / value changes.

        /// <summary>One feature group of the Rock Blend shader: its on/off toggle property (null = always on) and its adjustments.</summary>
        public sealed class Group
        {
            public string title, toggle, key;
            public string[] enabledBy; // group is greyed out unless one of these toggles is on (null = own toggle)
            public Func<MaterialProperty[], bool> onWhen; // extra condition (value-driven features: Contact AO, Macro)
            public Func<string, bool> owns;
        }

        private static bool Starts(string n, params string[] pre) { foreach (string p in pre) if (n.StartsWith(p, StringComparison.Ordinal)) return true; return false; }

        private static readonly string[] TerrainMapProps =
            { "_GRB_TerrainBaseMap", "_GRB_TerrainNormalMap", "_GRB_TerrainTint", "_GRB_TerrainTiling", "_GRB_TerrainUVOffset", "_GRB_TerrainNormalScale", "_GRB_TerrainSmoothness" };

        /// <summary>Feature toggles in display order (shown together at the top of both the material and the Rock Combiner inspector).</summary>
        public static readonly (string prop, string label, string tip)[] Toggles =
        {
            ("_GRB_TerrainAuto", "Match Terrain Layer (bake)", "On bake the terrain layer under the rock fills the terrain maps (base blend, fade colour, wind-blown top)."),
            ("_GRB_TerrainPerPixel", "Match Terrain Per Pixel", "Terrain splat maps under the rock per pixel (MaterialPropertyBlock, re-bound on move)."),
            ("_GRB_BottomEnable", "Terrain Blend at Base", "Terrain texture blended up the base of the rock (height above the terrain, per pixel)."),
            ("_GRB_TopEnable", "Top Layer", "Deposit layer (moss / sand / snow / sulfur / dust) on up-facing surfaces."),
            ("_GRB_TopFromTerrain", "Top Uses Terrain Layer", "The top layer takes the terrain layer's maps on bake (wind-blown ground)."),
            ("_GRB_FadeEnable", "Transparency Fade", "The last few cm above the terrain take the terrain colour (opaque, the base always reaches the ground). Fade Clips Pixels = legacy dithered clip (see-through band at the contact)."),
            ("_GRB_StrataEnable", "World Strata Bands", "World-space strata bands, continuous across pieces."),
        };

        public static readonly Group[] Groups =
        {
            new Group { key = "top", title = "Top Layer", toggle = "_GRB_TopEnable", owns = n => Starts(n, "_GRB_Top") && n != "_GRB_TopEnable" && n != "_GRB_TopFromTerrain" },
            new Group { key = "bottom", title = "Terrain Blend at Base + terrain maps", toggle = "_GRB_BottomEnable",
                        enabledBy = new[] { "_GRB_BottomEnable", "_GRB_FadeEnable", "_GRB_TopFromTerrain" },
                        owns = n => (Starts(n, "_GRB_Bottom") && n != "_GRB_BottomEnable") || Array.IndexOf(TerrainMapProps, n) >= 0 },
            new Group { key = "fade", title = "Transparency Fade", toggle = "_GRB_FadeEnable", owns = n => Starts(n, "_GRB_Fade") && n != "_GRB_FadeEnable" },
            new Group { key = "ao", title = "Contact AO", enabledBy = new[] { "_GRB_FadeEnable", "_GRB_BottomEnable" }, onWhen = ps => AnyValue(ps, "_GRB_ContactAOKeep", v => v < 0.999f), owns = n => Starts(n, "_GRB_ContactAO") },
            new Group { key = "strata", title = "World Strata Bands", toggle = "_GRB_StrataEnable", owns = n => Starts(n, "_GRB_Strata") && n != "_GRB_StrataEnable" },
            new Group { key = "macro", title = "Macro Variation", onWhen = ps => AnyValue(ps, "_GRB_MacroStrength", v => v > 0.001f), owns = n => Starts(n, "_GRB_Macro") },
            new Group { key = "data", title = "Vertex ranges + texture arrays (set on bake)", owns = n => n == "_GRB_HeightRange" || n == "_GRB_SizeRange" || n == "_GRB_BaseLumRef" || n == "_GRB_VColorRange" || n == "_GRB_UseTexArray" || Starts(n, "_GRB_BaseArray", "_GRB_NormalArray", "_GRB_MaskArray") },
        };

        private static readonly System.Collections.Generic.Dictionary<string, bool> s_groupFold = new System.Collections.Generic.Dictionary<string, bool>();

        // Groups open while their feature is on and collapse while it is off, until the user opens / closes one by hand
        // (that choice is kept per group and feature state for the session).
        private static bool Fold(string key, string title, bool def)
        {
            string k = key + (def ? ":on" : ":off");
            bool f = s_groupFold.TryGetValue(k, out bool user) ? user : def;
            bool nf = EditorGUILayout.Foldout(f, title, true, EditorStyles.foldoutHeader);
            if (nf != f) s_groupFold[k] = nf;
            return nf;
        }

        /// <summary>Any selected material (or mixed) whose float passes <paramref name="test"/>.</summary>
        private static bool AnyValue(MaterialProperty[] props, string name, Func<float, bool> test)
        {
            MaterialProperty p = Find(props, name);
            if (p == null) return false;
            foreach (UnityEngine.Object t in p.targets)
                if (t is Material m && m.HasProperty(name) && test(m.GetFloat(name))) return true;
            return false;
        }

        /// <summary>
        /// Value-driven feature switch (Contact AO = Keep below 1, Macro = Strength above 0): a toggle in the Features block
        /// that sets the same float the slider below edits (on: a usable default if it was off; off: the neutral value).
        /// </summary>
        private static void ValueToggle(MaterialEditor me, MaterialProperty[] props, string prop, string label, string tip, Func<float, bool> isOn, float onValue, float offValue)
        {
            MaterialProperty p = Find(props, prop);
            if (p == null) return;
            bool any = false, all = true;
            foreach (UnityEngine.Object t in p.targets)
                if (t is Material m) { bool o = isOn(m.GetFloat(prop)); any |= o; all &= o; }
            EditorGUI.showMixedValue = any && !all;
            EditorGUI.BeginChangeCheck();
            bool v = EditorGUILayout.Toggle(new GUIContent(label, tip), all);
            EditorGUI.showMixedValue = false;
            if (!EditorGUI.EndChangeCheck()) return;
            foreach (UnityEngine.Object t in p.targets)
                if (t is Material m)
                {
                    Undo.RecordObject(m, "Rock Blend " + label);
                    float cur = m.GetFloat(prop);
                    m.SetFloat(prop, v ? (isOn(cur) ? cur : onValue) : offValue);
                    EditorUtility.SetDirty(m);
                }
        }

        private static MaterialProperty Find(MaterialProperty[] props, string name)
        {
            foreach (MaterialProperty p in props) if (p.name == name) return p;
            return null;
        }

        /// <summary>On, off or mixed (any selected material on) for a toggle property.</summary>
        private static bool AnyOn(MaterialProperty[] props, string[] names)
        {
            foreach (string n in names)
            {
                MaterialProperty p = Find(props, n);
                if (p != null && (p.hasMixedValue || p.floatValue > 0.5f)) return true;
            }
            return false;
        }

        /// <summary>Feature toggles block (shared by the material inspector and the Rock Combiner inspector).</summary>
        public static void DrawToggles(MaterialEditor me, MaterialProperty[] props)
        {
            foreach (var t in Toggles)
            {
                MaterialProperty p = Find(props, t.prop);
                if (p == null) continue;
                // [ToggleUI] floats (no keywords): a plain mixed-aware toggle, without the shader's [Header] decorators
                EditorGUI.showMixedValue = p.hasMixedValue;
                EditorGUI.BeginChangeCheck();
                bool v = EditorGUILayout.Toggle(new GUIContent(t.label, t.tip), p.floatValue > 0.5f);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    me.RegisterPropertyChangeUndo(t.label);
                    p.floatValue = v ? 1f : 0f;
                }
            }
            ValueToggle(me, props, "_GRB_ContactAOKeep", "Contact AO (fade near ground)", "Removes the mask AO in the contact zone (Contact AO Keep below 1).", v => v < 0.999f, 0.3f, 1f);
            ValueToggle(me, props, "_GRB_MacroStrength", "Macro Variation", "Large-scale brightness variation (Macro Strength above 0).", v => v > 0.001f, 0.25f, 0f);
        }

        /// <summary>Grouped adjustments (shared by the material inspector and the Rock Combiner inspector).</summary>
        public static void DrawGroups(MaterialEditor me, MaterialProperty[] props, bool showData)
        {
            foreach (Group g in Groups)
            {
                if (g.key == "data" && !showData) continue;
                bool on = (g.toggle == null && g.enabledBy == null || AnyOn(props, g.enabledBy ?? new[] { g.toggle })) && (g.onWhen == null || g.onWhen(props));
                string title = g.title + (on ? "" : "  (off)");
                if (!Fold("grb_" + g.key, title, on && g.key != "data")) continue;
                using (new EditorGUI.DisabledScope(!on))
                using (new EditorGUI.IndentLevelScope())
                {
                    if (g.key == "top" && me != null) DrawLayerSetPicker(me);
                    foreach (MaterialProperty p in props)
                    {
                        if (!p.name.StartsWith("_GRB_", StringComparison.Ordinal) || !g.owns(p.name)) continue;
                        if ((p.propertyFlags & UnityEngine.Rendering.ShaderPropertyFlags.HideInInspector) != 0) continue;
                        me.ShaderProperty(p, p.displayName);
                    }
                    if (g.key == "top") TopWarnings(me);
                    if (g.key == "fade") { FadeBandInfo(me); GenesisRockBlendSceneFade.DrawInline(); }
                }
            }
        }

        private static int s_setPick;

        private static void DrawLayerSetPicker(MaterialEditor me)
        {
            string[] names = DmRockBlendMaterials.LayerSetNames();
            using (new EditorGUILayout.HorizontalScope())
            {
                s_setPick = EditorGUILayout.Popup("Top Layer Set", Mathf.Clamp(s_setPick, 0, names.Length - 1), names);
                if (GUILayout.Button("Apply", GUILayout.Width(60)))
                    foreach (UnityEngine.Object t in me.targets)
                        if (t is Material mat)
                        {
                            Undo.RecordObject(mat, "Apply Rock Blend layer set");
                            DmRockBlendMaterials.ApplyLayerSet(mat, names[s_setPick]);
                            EditorUtility.SetDirty(mat);
                        }
            }
        }

        /// <summary>Where the fade band actually sits (metres above the terrain), so the sliders read in real units.</summary>
        private static void FadeBandInfo(MaterialEditor me)
        {
            if (me == null || me.targets.Length == 0 || !(me.targets[0] is Material m) || !m.HasProperty("_GRB_FadeHeight")) return;
            float h = m.GetFloat("_GRB_FadeHeight"), soft = Mathf.Clamp01(m.GetFloat("_GRB_FadeSoftness")), n = m.GetFloat("_GRB_FadeNoiseStrength");
            float lo = Mathf.Max(0f, h * (1f - soft) - n * h), hi = h * (1f + n);
            EditorGUILayout.LabelField(" ", $"Band ~{lo:0.00}-{hi:0.00} m above the terrain (fully visible above {hi:0.00} m)", EditorStyles.miniLabel);
            if (hi > 0.35f)
                EditorGUILayout.HelpBox("Fade band reaches above 0.35 m: the rock base reads as see-through / floating. For a tight blend use Fade Height 0.08-0.12, Softness ~0.6, Noise Strength ~0.35.", MessageType.Warning);
        }

        private static void TopWarnings(MaterialEditor me)
        {
            int noCov = 0, noTex = 0;
            foreach (UnityEngine.Object t in me.targets)
                if (t is Material m && m.HasProperty("_GRB_TopEnable") && m.GetFloat("_GRB_TopEnable") > 0.5f)
                {
                    if (m.GetFloat("_GRB_TopCoverage") <= 0.001f) noCov++;
                    if (m.GetTexture("_GRB_TopBaseMap") == null && m.GetFloat("_GRB_TopFromTerrain") < 0.5f) noTex++;
                }
            if (noCov > 0) EditorGUILayout.HelpBox($"Top Layer is on but Top Coverage is 0 on {noCov} material(s): nothing shows.", MessageType.Warning);
            if (noTex > 0) EditorGUILayout.HelpBox($"Top Layer is on without a Top Albedo on {noTex} material(s) (and not from the terrain): it shows as flat tint.", MessageType.Warning);
        }

        /// <summary>Notes for materials whose edits do not stick (bake outputs) or are shared by many rocks (kit array material).</summary>
        private static void GeneratedNotes(MaterialEditor me)
        {
            int variants = 0, arrays = 0;
            foreach (UnityEngine.Object t in me.targets)
            {
                string path = AssetDatabase.GetAssetPath(t);
                if (path.StartsWith(DmRockBlendMaterials.VariantFolder + "/", StringComparison.Ordinal) && !path.StartsWith(DmRockBlendMaterials.PerRockFolder + "/", StringComparison.Ordinal)) variants++;
                else if (path.Contains("/Generated/TextureArrays/")) arrays++;
            }
            if (variants > 0)
                EditorGUILayout.HelpBox("Bake output (variant): regenerated from the rock's blend material on every bake, so edits here are lost. " +
                                        "Edit the rock's blend in the DM PCG Creator inspector (or its per-rock MB_ material).", MessageType.Warning);
            if (arrays > 0)
                EditorGUILayout.HelpBox("Kit texture-array material: shared by every baked rock of the kit that has no blend material, and not used " +
                                        "for terrain matching. Use the DM PCG Creator inspector (per-rock blend) instead.", MessageType.Warning);
        }

        public override void OnGUI(MaterialEditor me, MaterialProperty[] props)
        {
            if (m_lit == null) m_lit = CreateLit();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Genesis Rock Blend", EditorStyles.boldLabel);
            GeneratedNotes(me);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Features", EditorStyles.miniBoldLabel);
                DrawToggles(me, props);
                var first = me.target as Material;
                bool fwd = first != null && first.shader != null && first.shader.name == DmRockBlendMaterials.ForwardShaderName;
                EditorGUI.showMixedValue = MixedForward(me);
                bool fwdNew = EditorGUILayout.Toggle(new GUIContent("Forward Only (fade SSAO too)",
                    "Renders this material forward-only (DepthForwardOnly + ForwardOnly, no GBuffer) so Contact AO Keep can also fade " +
                    "HDRP's screen-space AO on the rock near the ground. In the deferred default only the material / mask AO is faded. " +
                    "Costs an extra depth pass with normals and forward lighting for these rocks."), fwd);
                EditorGUI.showMixedValue = false;
                if (fwdNew != fwd)
                    foreach (UnityEngine.Object t in me.targets)
                        if (t is Material mat)
                        {
                            Undo.RecordObject(mat, "Rock Blend forward only");
                            SetForwardOnly(mat, fwdNew);
                            EditorUtility.SetDirty(mat);
                        }
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Adjustments", EditorStyles.miniBoldLabel);
            DrawGroups(me, props, true);

            EditorGUILayout.Space(8);
            s_fold = EditorGUILayout.BeginFoldoutHeaderGroup(s_fold, "HDRP Lit (surface options, base maps)");
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (s_fold)
            {
                if (m_lit != null) m_lit.OnGUI(me, props);
                else EditorGUILayout.HelpBox("HDRP LitGUI not found: only the Rock Blend section is shown.", MessageType.Warning);
            }
            if (EditorGUI.EndChangeCheck())
            {
                var mats = new System.Collections.Generic.List<Material>();
                foreach (UnityEngine.Object t in me.targets) if (t is Material tm) mats.Add(tm);
                DmRockBlendMaterials.OnBlendEdited(mats); // live rocks follow, per-pixel terrain re-bound, per-rock blends rebaked
            }
        }

        private static bool MixedForward(MaterialEditor me)
        {
            bool? v = null;
            foreach (UnityEngine.Object t in me.targets)
                if (t is Material m)
                {
                    bool f = m.shader != null && m.shader.name == DmRockBlendMaterials.ForwardShaderName;
                    if (v.HasValue && v.Value != f) return true;
                    v = f;
                }
            return false;
        }

        public override void ValidateMaterial(Material material)
        {
            SyncGroundFade(material);
            SyncTopMaps(material);
            if (m_lit == null) m_lit = CreateLit();
            if (m_lit != null) m_lit.ValidateMaterial(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (m_lit == null) m_lit = CreateLit();
            if (m_lit != null) m_lit.AssignNewShaderToMaterial(material, oldShader, newShader);
            else base.AssignNewShaderToMaterial(material, oldShader, newShader);
        }
    }
}
#endif

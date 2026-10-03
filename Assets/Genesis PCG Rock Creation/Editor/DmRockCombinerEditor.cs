#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using GenesisPCG.RockCreation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GenesisPCG.RockCreation.Editor
{
    [CustomEditor(typeof(DmRockCombiner))]
    [CanEditMultipleObjects]
    internal sealed class DmRockCombinerEditor : UnityEditor.Editor
    {
        public const string KitPath = "Assets/Genesis PCG Rock Creation/Data/Kits/RockKit_ProcRock.asset";
        public const string RecipePath = "Assets/Genesis PCG Rock Creation/Data/Recipes/RockCombineRecipe_Default.asset";
        // Optional: an existing recipe-like asset whose size/scale/tilt/flatten values seed the first combine recipe
        // (read by field name, no type dependency). In this project: the noise-rock recipe.
        public const string SizeSourceRecipePath = "Assets/_Project/Art/ProcPlacement/Recipes/DM_ProcRecipe_IoBasaltRock.asset";
        // Project defaults for the menu items only (the kit asset itself is data-driven; change these when packaging).
        public const string SourceFolder = "Assets/Rock and Boulders3/Proc Rock";

        private bool m_customMaterial;
        private MaterialEditor m_blendEditor;
        private Material[] m_blendEditorMats;
        private static bool s_settingsFold = true;

        private DmRockCombiner[] Rocks => targets.OfType<DmRockCombiner>().ToArray();

        private void OnDisable()
        {
            if (m_blendEditor != null) DestroyImmediate(m_blendEditor);
            m_blendEditor = null;
            m_blendEditorMats = null;
        }

        public override void OnInspectorGUI()
        {
            var single = (DmRockCombiner)target;
            serializedObject.Update();
            EditorGUILayout.LabelField("Genesis PCG Rock Creation \u2014 DM PCG Creator" + (targets.Length > 1 ? $"  ({targets.Length} rocks)" : ""), EditorStyles.boldLabel);

            DrawFeatureToggles();
            serializedObject.ApplyModifiedProperties(); // lock seed
            serializedObject.Update();                  // per-rock blends / terrain match set by the toggles
            EditorGUILayout.Space(4);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("preset"));
            DmRockPreset pr = single.Preset;
            Slot("kit", "Kit", pr != null ? pr.kit : null);
            Slot("style", "Style", pr != null ? pr.style : null);
            Slot("recipe", "Recipe", pr != null ? pr.recipe : null);
            MaterialSlot(pr != null ? pr.materialOverride : null);
            BlendSlot(pr != null ? pr.blendMaterial : null);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("seed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("userSinkOffset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("features"), new GUIContent("Passages / Nooks"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseModels"), true);
            // Slot edits go through OnValidate: rebuild with the same seed, and a baked rock is rebaked by the scheduler.
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New Variation"))
                    foreach (Object t in targets)
                    {
                        var c = (DmRockCombiner)t;
                        Undo.RecordObject(c, "DM PCG Creator New Variation");
                        c.SetSeed(PcgSeededPasteWatcher.NewSeed());
                        EditorUtility.SetDirty(c);
                    }
                if (GUILayout.Button("Rebuild"))
                    foreach (Object t in targets)
                        ((DmRockCombiner)t).Rebuild();
                if (GUILayout.Button("Snap To Surface"))
                    foreach (Object t in targets)
                    {
                        var c = (DmRockCombiner)t;
                        PcgSeededPasteWatcher.SnapWithUndo(c.transform, c.SnapSettings, null, "DM PCG Creator Snap");
                    }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(single.Preset == null))
                    if (GUILayout.Button(new GUIContent("Re-apply Preset", "Clears the own Kit / Style / Recipe / Material slots and copies the preset's passage / nook settings. Keeps the seed.")))
                        foreach (Object t in targets)
                            DmRockPaletteWindow.ApplyPresetTo((DmRockCombiner)t, ((DmRockCombiner)t).Preset);
                if (GUILayout.Button(new GUIContent("Save As New Preset...", "Creates a preset asset from this rock's effective kit / style / recipe / material / features.")))
                    DmRockPaletteWindow.NewPresetFromRock(single, true);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Use Selected As Base",
                        "Captures the meshes of the other selected objects (scene objects or prefabs) as this DM PCG Creator's base. " +
                        "Tip: lock this Inspector, then select the rocks.")))
                    UseSelectedAsBase(single);
                using (new EditorGUI.DisabledScope(single.BaseModels.Count == 0))
                    if (GUILayout.Button("Clear Base (use Kit)"))
                        foreach (Object t in targets)
                        {
                            var c = (DmRockCombiner)t;
                            Undo.RecordObject(c, "DM PCG Creator Clear Base");
                            c.SetBaseModels(new List<DmRockCombiner.BaseModel>());
                            EditorUtility.SetDirty(c);
                        }
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Bake (atlas + single mesh + LODs)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Bake", "Cull hidden triangles, merge into one sub-mesh (atlas, or the override material), generate LODs + collider and save into this rock's .asset.")))
                    BakeTargets(false);
                if (GUILayout.Button(new GUIContent("Un-bake", "Back to the live seeded preview.")))
                    foreach (Object t in targets)
                    {
                        var c = (DmRockCombiner)t;
                        DmRockBakeScheduler.Cancel(c);
                        c.Unbake();
                    }
                if (GUILayout.Button(new GUIContent("Rebake", "Rebuild from the seed and bake again (overwrites this rock's asset).")))
                    BakeTargets(true);
            }

            EditorGUILayout.Space(6);
            DrawFeatureSettings();

            if (targets.Length != 1) return;
            DmRockAssembler.Result asm = single.LastAssembly;
            if (single.IsBaked)
            {
                DmRockCombiner.BakeState b = single.BakeData;
                var lines = new System.Text.StringBuilder();
                string why = single.IsBakeCurrent ? DmRockBaker.StaleReason(single) : null;
                lines.Append(!single.IsBakeCurrent ? "Baked (stale)" : why != null ? "Baked (out of date: " + why + ")" : "Baked");
                lines.Append(": ").Append(AssetDatabase.GetAssetPath(b.mesh));
                if (b.mesh != null) lines.Append($"\nLOD0 {b.mesh.vertexCount:N0} v / {DmRockBaker.TriCount(b.mesh):N0} t");
                if (b.lodMeshes != null)
                    for (int i = 0; i < b.lodMeshes.Length; i++)
                        if (b.lodMeshes[i] != null) lines.Append($"\nLOD{i + 1} {b.lodMeshes[i].vertexCount:N0} v / {DmRockBaker.TriCount(b.lodMeshes[i]):N0} t");
                if (b.atlas != null && b.atlas.isArray)
                {
                    int slices = 0;
                    if (b.atlas.baseArrays != null) foreach (Texture2DArray ta in b.atlas.baseArrays) if (ta != null) slices += ta.depth;
                    lines.Append($"\nTexture arrays {b.atlas.key}: slice size {b.atlas.sizeNote}; {b.atlas.entries.Count} materials in {slices} slices");
                }
                else if (b.atlas != null) lines.Append($"\nAtlas {b.atlas.key} ({b.atlas.size}px, {b.atlas.entries.Count} materials)");
                else if (single.MaterialOverride != null) lines.Append($"\nMaterial override {single.MaterialOverride.name} (mesh UVs, no atlas)");
                EditorGUILayout.HelpBox(lines.ToString(), MessageType.None);
            }
            else
            {
                string plan = PlannedArraySize(single);
                if (plan != null) EditorGUILayout.HelpBox("Texture arrays (on bake): slice size " + plan, MessageType.None);
                Mesh m = single.GeneratedMesh;
                if (m != null && m.vertexCount > 0)
                    EditorGUILayout.HelpBox(
                        $"{(single.Style != null ? single.Style.name : "Default boulder pile")}: {single.LastPieceCount} pieces (hero {single.LastBaseName})\n" +
                        $"{m.vertexCount:N0} verts, {m.subMeshCount} sub-meshes, size {m.bounds.size.x:F2} x {m.bounds.size.y:F2} x {m.bounds.size.z:F2} m\n" +
                        "Ctrl+D / paste / Shift+drag rolls a new variation unless Lock Seed is on.",
                        MessageType.None);
                else
                    EditorGUILayout.HelpBox("Nothing to build: assign a Preset or Kit (or base models).", MessageType.Info);
            }
            if (asm != null)
                EditorGUILayout.HelpBox($"Assembly ({asm.kind}): H {asm.height:F1} m, R {asm.radius:F1} m, lean {asm.leanDeg:F0} deg @ {asm.azimuthDeg:F0}\n{asm.stats.Summary()}", MessageType.None);
        }

        private void Slot(string prop, string label, Object inherited)
        {
            SerializedProperty p = serializedObject.FindProperty(prop);
            EditorGUILayout.PropertyField(p, new GUIContent(label, inherited != null ? "Empty = from the preset: " + inherited.name : null));
            if (p.objectReferenceValue == null && inherited != null && !p.hasMultipleDifferentValues)
                EditorGUILayout.LabelField(" ", "\u21b3 from preset: " + inherited.name, EditorStyles.miniLabel);
        }

        private void BlendSlot(Material inherited)
        {
            SerializedProperty p = serializedObject.FindProperty("blendMaterial");
            List<Material> lib = DmRockBlendMaterials.Templates();
            var names = new List<string> { inherited != null ? "Inherit (" + inherited.name + ")" : "None (plain bake material)" };
            names.AddRange(lib.Select(m => m.name));
            var cur = p.objectReferenceValue as Material;
            bool perRock = cur != null && DmRockBlendMaterials.IsPerRock(cur);
            if (perRock)
            {
                Material src = DmRockBlendMaterials.SourceTemplate(cur);
                names.Add("Per-rock: " + cur.name + (src != null ? " (from " + src.name + ")" : ""));
            }
            int li = cur != null ? lib.IndexOf(cur) : -1;
            int idx = cur == null ? 0 : (li >= 0 ? li + 1 : perRock ? names.Count - 1 : -1);
            EditorGUI.showMixedValue = p.hasMultipleDifferentValues;
            if (idx >= 0)
            {
                int pick = EditorGUILayout.Popup(new GUIContent("Blend Material", "Rock Blend Lit template used on bake (top layer / terrain blend / strata). " +
                    "Editing a feature below gives each selected rock its own per-rock copy (never shared)."), idx, names.ToArray());
                if (pick != idx && pick <= lib.Count) p.objectReferenceValue = pick == 0 ? null : lib[pick - 1];
            }
            else EditorGUILayout.PropertyField(p, new GUIContent("Blend Material"));
            EditorGUI.showMixedValue = false;
        }

        private void MaterialSlot(Material inherited)
        {
            SerializedProperty p = serializedObject.FindProperty("materialOverride");
            List<Material> lib = DmRockLibrary.MaterialChoices();
            var names = new List<string> { inherited != null ? "Inherit (" + inherited.name + ")" : "Pack originals (atlas)" };
            names.AddRange(lib.Select(m => m.name));
            names.Add("Custom...");
            var cur = p.objectReferenceValue as Material;
            int idx = cur == null ? 0 : (lib.IndexOf(cur) >= 0 ? lib.IndexOf(cur) + 1 : names.Count - 1);
            EditorGUI.showMixedValue = p.hasMultipleDifferentValues;
            int pick = EditorGUILayout.Popup("Material", idx, names.ToArray());
            EditorGUI.showMixedValue = false;
            if (pick != idx)
            {
                if (pick == 0) p.objectReferenceValue = null;
                else if (pick <= lib.Count) p.objectReferenceValue = lib[pick - 1];
            }
            if (pick == names.Count - 1 && pick != idx) m_customMaterial = true;
            if (m_customMaterial || (cur != null && lib.IndexOf(cur) < 0))
                EditorGUILayout.PropertyField(p, new GUIContent(" "));
        }

        // ------------------------------------------------------------------------------------------------
        // Feature toggles (top) and grouped feature settings (below). Material features live on each rock's own blend
        // material: an edit gives every selected rock its own per-rock blend first (EnsureOwnBlend), then changes each one
        // separately. Ground Settle / Flush Cut live on the recipe (shared by every rock using that recipe).

        private static float BlendFloat(DmRockCombiner c, string prop, float none)
        {
            Material m = c.BlendMaterial;
            return m != null && m.HasProperty(prop) ? m.GetFloat(prop) : none;
        }

        private bool ToggleRow(GUIContent label, Func<DmRockCombiner, bool> get, out bool value)
        {
            DmRockCombiner[] rocks = Rocks;
            bool first = rocks.Length > 0 && get(rocks[0]);
            bool mixed = rocks.Any(r => get(r) != first);
            EditorGUI.showMixedValue = mixed;
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.Toggle(label, first);
            EditorGUI.showMixedValue = false;
            return EditorGUI.EndChangeCheck();
        }

        /// <summary>Edits each selected rock's own blend (created per rock when missing / shared). Off-edits skip rocks without a blend.</summary>
        private void EditBlends(string undo, bool create, Action<Material> edit)
        {
            var edited = new List<Material>();
            foreach (DmRockCombiner c in Rocks)
            {
                Material m = create || DmRockBlendMaterials.OwnsBlend(c) || c.BlendMaterial != null ? DmRockBlendMaterials.EnsureOwnBlend(c, undo) : null;
                if (m == null) continue;
                Undo.RecordObject(m, undo);
                edit(m);
                GenesisRockBlendGUI.Validate(m); // HDRP keywords / Ground Fade alpha clip, as the material inspector does
                EditorUtility.SetDirty(m);
                edited.Add(m);
                if (!c.IsBaked) c.RefreshLiveMaterials();
            }
            DmRockBlendMaterials.OnBlendEdited(edited);
        }

        private void BlendToggle(string prop, string label, string tip, float noBlend = 0f, Action<Material> onEnable = null)
        {
            if (!ToggleRow(new GUIContent(label, tip), c => BlendFloat(c, prop, noBlend) > 0.5f, out bool v)) return;
            EditBlends("Rock Feature " + label, v, m =>
            {
                if (!m.HasProperty(prop)) return;
                m.SetFloat(prop, v ? 1f : 0f);
                if (v) onEnable?.Invoke(m);
            });
        }

        private void DrawFeatureToggles()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Features" + (targets.Length > 1 ? " (applied to each selected rock)" : ""), EditorStyles.miniBoldLabel);

                if (ToggleRow(new GUIContent("Terrain Match", "Match the terrain layer under the rock on bake / rebuild (base blend, fade colour, top deposits). " +
                        "A rock without a blend gets its own per-rock blend from the Default template."),
                        c => c.MatchTerrain && BlendFloat(c, "_GRB_TerrainAuto", 1f) > 0.5f, out bool tm))
                {
                    foreach (DmRockCombiner c in Rocks) { Undo.RecordObject(c, "Rock Terrain Match"); c.MatchTerrain = tm; }
                    EditBlends("Rock Terrain Match", tm, m => m.SetFloat("_GRB_TerrainAuto", tm ? 1f : 0f));
                    foreach (DmRockCombiner c in Rocks)
                        if (c.IsBaked && c.BakeSettings.rebakeAfterEdit) DmRockBakeScheduler.Request(c, 0.6f, false);
                }
                BlendToggle("_GRB_TerrainPerPixel", "  Per Pixel (splat maps)", "Terrain splat maps under the rock per pixel; re-bound on move.");
                BlendToggle("_GRB_BottomEnable", "Terrain Blend at Base", "Terrain texture blended up the base of the rock.");
                BlendToggle("_GRB_TopEnable", "Top Layer", "Deposits on up-facing surfaces (moss / sand / snow / sulfur / dust).", 0f, m =>
                {
                    // a top layer switched on with nothing to show is the usual 'top layer does not apply' trap
                    if (m.GetFloat("_GRB_TopCoverage") <= 0.001f) m.SetFloat("_GRB_TopCoverage", 0.45f);
                    if (m.GetTexture("_GRB_TopBaseMap") == null && m.GetFloat("_GRB_TopFromTerrain") < 0.5f) DmRockBlendMaterials.ApplyLayerSet(m, "Sand");
                });
                BlendToggle("_GRB_TopFromTerrain", "  Top From Terrain Layer", "The top layer takes the terrain layer's maps on bake (wind-blown ground).");
                BlendToggle("_GRB_FadeEnable", "Transparency Fade", "Terrain-coloured blend of the last few cm above the ground (opaque; the material's Fade Clips Pixels toggle restores the legacy dithered clip).");
                if (ToggleRow(new GUIContent("Contact AO", "Removes the mask AO in the contact zone (Contact AO Keep < 1)."), c => BlendFloat(c, "_GRB_ContactAOKeep", 1f) < 0.999f, out bool ao))
                    EditBlends("Rock Feature Contact AO", ao, m => m.SetFloat("_GRB_ContactAOKeep", ao ? (m.GetFloat("_GRB_ContactAOKeep") < 0.999f ? m.GetFloat("_GRB_ContactAOKeep") : 0.3f) : 1f));
                BlendToggle("_GRB_StrataEnable", "World Strata Bands", "World-space strata bands, continuous across pieces.");
                if (ToggleRow(new GUIContent("Macro Variation", "Large-scale brightness variation (Macro Strength > 0)."), c => BlendFloat(c, "_GRB_MacroStrength", 0f) > 0.001f, out bool mac))
                    EditBlends("Rock Feature Macro", mac, m => m.SetFloat("_GRB_MacroStrength", mac ? (m.GetFloat("_GRB_MacroStrength") > 0.001f ? m.GetFloat("_GRB_MacroStrength") : 0.25f) : 0f));

                RecipeToggle("snap.conformBaseToSurface", "Ground Settle (conform base)", "Bend the bottom of the rock so it hugs uneven ground.");
                RecipeToggle("bake.cullBelowGround", "Flush Cut (cull below ground)", "Bake removes / clips the geometry below the ground.");
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lockSeed"));
            }
        }

        private DmRockCombineRecipe[] Recipes(out int withoutRecipe)
        {
            withoutRecipe = Rocks.Count(r => r.Recipe == null);
            return Rocks.Select(r => r.Recipe).Where(r => r != null).Distinct().ToArray();
        }

        private void RecipeToggle(string path, string label, string tip)
        {
            DmRockCombineRecipe[] recipes = Recipes(out int none);
            if (recipes.Length == 0) { using (new EditorGUI.DisabledScope(true)) EditorGUILayout.Toggle(new GUIContent(label + " (no recipe: defaults)", tip), true); return; }
            var so = new SerializedObject(recipes);
            SerializedProperty p = so.FindProperty(path);
            if (p == null) return;
            string shared = recipes.Length == 1 ? recipes[0].name : recipes.Length + " recipes";
            // plain toggle (no [Header] decorators of the recipe field), mixed when the selected rocks' recipes differ
            EditorGUI.showMixedValue = p.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            bool v = EditorGUILayout.Toggle(new GUIContent(label, tip + " Shared recipe setting (" + shared + ")" + (none > 0 ? $"; {none} rock(s) without a recipe use the defaults." : ".")), p.boolValue);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck()) { p.boolValue = v; so.ApplyModifiedProperties(); }
        }

        private void DrawFeatureSettings()
        {
            s_settingsFold = EditorGUILayout.BeginFoldoutHeaderGroup(s_settingsFold, "Feature Settings");
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (!s_settingsFold) return;
            DmRockCombiner[] rocks = Rocks;
            int owned = rocks.Count(DmRockBlendMaterials.OwnsBlend);
            if (owned < rocks.Length)
            {
                EditorGUILayout.HelpBox(rocks.Length == 1
                        ? "This rock uses " + (rocks[0].BlendMaterial != null ? "the shared template " + rocks[0].BlendMaterial.name : "no blend material") + ". Its blend settings become editable once it has its own per-rock blend."
                        : $"{rocks.Length - owned} of {rocks.Length} selected rocks have no per-rock blend yet (shared template or none).", MessageType.None);
                if (GUILayout.Button(rocks.Length == 1 ? "Make Per-Rock Blend" : $"Make Per-Rock Blends ({rocks.Length - owned})"))
                    EditBlends("Make Per-Rock Blend", true, _ => { });
            }
            else
            {
                Material[] mats = rocks.Select(r => r.OwnBlendMaterial).ToArray();
                if (m_blendEditor == null || m_blendEditorMats == null || !mats.SequenceEqual(m_blendEditorMats))
                {
                    if (m_blendEditor != null) DestroyImmediate(m_blendEditor);
                    m_blendEditor = (MaterialEditor)CreateEditor(mats.Distinct().ToArray(), typeof(MaterialEditor));
                    m_blendEditorMats = mats;
                }
                MaterialProperty[] props = MaterialEditor.GetMaterialProperties(m_blendEditor.targets);
                EditorGUI.BeginChangeCheck();
                GenesisRockBlendGUI.DrawGroups(m_blendEditor, props, false);
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (Material m in mats.Distinct()) { GenesisRockBlendGUI.Validate(m); EditorUtility.SetDirty(m); }
                    DmRockBlendMaterials.OnBlendEdited(mats);
                }
            }

            DmRockCombineRecipe[] recipes = Recipes(out _);
            if (recipes.Length == 0) return;
            var so = new SerializedObject(recipes);
            string shared = recipes.Length == 1 ? recipes[0].name : recipes.Length + " recipes";
            RecipeGroup(so, "Ground Settle  (recipe " + shared + ")", "snap.conformBaseToSurface",
                "snap.conformHeight", "snap.conformHeightInMeters", "snap.conformFalloff", "snap.maxConformDistance", "snap.extraSink", "snap.conformLiftBuried", "snap.conformUndersideFully");
            RecipeGroup(so, "Flush Cut  (recipe " + shared + ")", "bake.cullBelowGround",
                "bake.groundCut", "bake.groundCutOffset", "bake.cullBelowGroundMargin", "bake.cullInterior", "bake.interiorInset");
            so.ApplyModifiedProperties();
        }

        private static readonly Dictionary<string, bool> s_recipeFold = new Dictionary<string, bool>();

        private static void RecipeGroup(SerializedObject so, string title, string toggle, params string[] paths)
        {
            SerializedProperty t = so.FindProperty(toggle);
            bool on = t != null && (t.hasMultipleDifferentValues || t.boolValue);
            string key = toggle;
            if (!s_recipeFold.TryGetValue(key, out bool f)) f = on;
            f = EditorGUILayout.Foldout(f, title + (on ? "" : "  (off)"), true, EditorStyles.foldoutHeader);
            s_recipeFold[key] = f;
            if (!f) return;
            using (new EditorGUI.DisabledScope(!on))
            using (new EditorGUI.IndentLevelScope())
                foreach (string p in paths)
                {
                    SerializedProperty sp = so.FindProperty(p);
                    if (sp != null) EditorGUILayout.PropertyField(sp, true);
                }
        }

        private static readonly System.Collections.Generic.Dictionary<string, string> s_plan = new System.Collections.Generic.Dictionary<string, string>();

        /// <summary>Slice size the texture-array bake would pick for this rock's kit (cached per kit content + mode).</summary>
        private static string PlannedArraySize(DmRockCombiner c)
        {
            DmRockBakeSettings bs = c.BakeSettings;
            if (bs == null || bs.textureMode != DmRockTextureMode.TextureArray || c.MaterialOverride != null || c.Kit == null) return null;
            string k = DmRockBaker.ContentHash(c.Kit) + "|" + (int)bs.arraySliceSize;
            if (s_plan.TryGetValue(k, out string note)) return note;
            var mats = new System.Collections.Generic.List<Material>();
            foreach (Material m in DmRockBaker.KitMaterials(c.Kit)) if (!mats.Contains(m)) mats.Add(m);
            if (mats.Count == 0) return null;
            DmRockArrayBuilder.Plan(mats, bs.arraySliceSize, out _, out _, out note);
            s_plan[k] = note;
            return note;
        }

        /// <summary>Bake / Rebake every selected rock, one after another (each with its own per-rock blend, terrain match and asset).</summary>
        private void BakeTargets(bool force)
        {
            DmRockCombiner[] rocks = Rocks;
            int ok = 0;
            try
            {
                for (int i = 0; i < rocks.Length; i++)
                {
                    DmRockCombiner c = rocks[i];
                    if (rocks.Length > 1 && EditorUtility.DisplayCancelableProgressBar(force ? "Rebake rocks" : "Bake rocks", $"{c.name} ({i + 1}/{rocks.Length})", i / (float)rocks.Length))
                        break;
                    DmRockBakeScheduler.Cancel(c);
                    if (DmRockBaker.Bake(c, force, out DmRockBaker.Report r))
                    {
                        ok++;
                        Debug.Log("Rock bake: " + r, c);
                    }
                }
            }
            finally
            {
                if (rocks.Length > 1) EditorUtility.ClearProgressBar();
            }
            if (rocks.Length > 1) Debug.Log($"Rock bake: {ok} of {rocks.Length} selected rocks {(force ? "rebaked" : "baked")}.");
        }

        private void UseSelectedAsBase(DmRockCombiner c)
        {
            var objs = Selection.gameObjects.Where(g => g != null && g != c.gameObject).ToList();
            if (objs.Count == 0)
            {
                EditorUtility.DisplayDialog("Use Selected As Base",
                    "Select the DM PCG Creator plus the rocks to use (or lock this Inspector with the padlock, then select rocks in the Scene or Project).", "OK");
                return;
            }
            var models = CaptureBaseModels(c.transform, objs);
            if (models.Count == 0)
            {
                EditorUtility.DisplayDialog("Use Selected As Base", "None of the selected objects have usable (asset) meshes.", "OK");
                return;
            }
            Undo.RecordObject(c, "DM PCG Creator Use Selected As Base");
            c.SetBaseModels(models);
            EditorUtility.SetDirty(c);
        }

        /// <summary>
        /// Captures meshes/materials of <paramref name="objs"/> relative to <paramref name="frame"/>.
        /// Prefab assets are referenced as a whole; scene objects are captured per MeshFilter.
        /// Generated (non-asset) meshes, e.g. other proc rocks, are skipped.
        /// </summary>
        internal static List<DmRockCombiner.BaseModel> CaptureBaseModels(Transform frame, IEnumerable<GameObject> objs)
        {
            var list = new List<DmRockCombiner.BaseModel>();
            foreach (GameObject go in objs)
            {
                if (go == null) continue;
                if (EditorUtility.IsPersistent(go))
                {
                    list.Add(new DmRockCombiner.BaseModel { source = go });
                    continue;
                }
                foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>(false))
                {
                    if (mf.sharedMesh == null || !EditorUtility.IsPersistent(mf.sharedMesh) || mf.GetComponentInParent<IPcgSeeded>(true) != null)
                        continue;
                    var mr = mf.GetComponent<MeshRenderer>();
                    Transform t = mf.transform;
                    Vector3 ls = t.lossyScale, fs = frame.lossyScale;
                    list.Add(new DmRockCombiner.BaseModel
                    {
                        mesh = mf.sharedMesh,
                        materials = mr != null ? mr.sharedMaterials : null,
                        localPosition = frame.InverseTransformPoint(t.position),
                        localRotation = Quaternion.Inverse(frame.rotation) * t.rotation,
                        localScale = new Vector3(ls.x / fs.x, ls.y / fs.y, ls.z / fs.z),
                    });
                }
            }
            return list;
        }

        // ------------------------------------------------------------------------------------------------
        // Menus (GameObject/... items with low priority also appear in the Hierarchy right-click menu)

        [MenuItem("GameObject/Genesis PCG Rock Creation/DM PCG Creator (from Selection)", false, 11)]
        private static void CreateFromSelection(MenuCommand command)
        {
            // Hierarchy context menus invoke once per selected object; only run for the first.
            if (command.context != null && Selection.objects.Length > 1 && command.context != Selection.objects[0])
                return;
            CreateFromSelectionInternal(Selection.gameObjects);
        }

        [MenuItem("GameObject/Genesis PCG Rock Creation/DM PCG Creator (Random)", false, 12)]
        private static void CreateRandomMenu(MenuCommand command)
        {
            if (command.context != null && Selection.objects.Length > 1 && command.context != Selection.objects[0])
                return;
            GameObject parent = command.context as GameObject;
            CreateRandom(parent != null ? parent.transform.parent : null, null);
        }

        [MenuItem("Tools/Genesis PCG Rock Creation/Refresh Default Kit")]
        private static void RefreshKitMenu()
        {
            DmRockKit kit = EnsureKit(true);
            EditorGUIUtility.PingObject(kit);
        }

        /// <summary>Creates a combiner whose base is the selected scene rocks; disables the originals (Undo).</summary>
        internal static GameObject CreateFromSelectionInternal(IEnumerable<GameObject> selection)
        {
            var sel = (selection ?? Enumerable.Empty<GameObject>())
                .Where(g => g != null && !EditorUtility.IsPersistent(g) && g.GetComponent<IPcgSeeded>() == null &&
                            g.GetComponentsInChildren<MeshFilter>(false).Any(f => f.sharedMesh != null && EditorUtility.IsPersistent(f.sharedMesh)))
                .ToList();
            // Drop children whose ancestor is also selected (they are captured with the parent).
            sel = sel.Where(g => !sel.Any(o => o != g && g.transform.IsChildOf(o.transform))).ToList();
            if (sel.Count == 0)
                return CreateRandom(null, null);

            GameObject first = sel[0];
            var go = new GameObject("DM PCG Creator");
            SceneManager.MoveGameObjectToScene(go, first.scene);
            go.transform.SetParent(first.transform.parent, false);
            go.transform.SetPositionAndRotation(first.transform.position, Quaternion.identity);

            var c = go.AddComponent<DmRockCombiner>();
            c.SetRecipe(EnsureRecipe());
            c.SetKit(EnsureKit(false));
            c.SetSeed(PcgSeededPasteWatcher.NewSeed());
            c.SetBaseModels(CaptureBaseModels(go.transform, sel));

            // The combined mesh is re-pivoted to the base's bottom center: move the object so nothing shifts visually.
            go.transform.position += go.transform.TransformVector(c.LastPivotOffset);

            Undo.RegisterCreatedObjectUndo(go, "Create DM PCG Creator");
            foreach (GameObject g in sel)
            {
                Undo.RecordObject(g, "Create DM PCG Creator");
                g.SetActive(false);
            }
            Selection.activeGameObject = go;
            return go;
        }

        /// <summary>Creates a kit-based combiner at the scene view pivot (or <paramref name="position"/>) and snaps it to the ground.</summary>
        internal static GameObject CreateRandom(Transform parent, Vector3? position)
        {
            var go = new GameObject("DM PCG Creator");
            if (parent != null)
                go.transform.SetParent(parent, false);
            Vector3 pos = position ?? (SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero);
            go.transform.position = pos;

            var c = go.AddComponent<DmRockCombiner>();
            c.SetRecipe(EnsureRecipe());
            c.SetKit(EnsureKit(false));
            c.SetSeed(PcgSeededPasteWatcher.NewSeed());
            PcgSurfaceSnap.Snap(go.transform, c.SnapSettings);

            Undo.RegisterCreatedObjectUndo(go, "Create DM PCG Creator");
            Selection.activeGameObject = go;
            return go;
        }

        // ------------------------------------------------------------------------------------------------
        // Assets

        internal static DmRockKit EnsureKit(bool refresh)
        {
            var kit = AssetDatabase.LoadAssetAtPath<DmRockKit>(KitPath);
            bool created = false;
            if (kit == null)
            {
                EnsureFolder(ParentFolder(KitPath));
                kit = ScriptableObject.CreateInstance<DmRockKit>();
                AssetDatabase.CreateAsset(kit, KitPath);
                created = true;
            }
            if (created || refresh)
            {
                FillKitFromFolder(kit, string.IsNullOrEmpty(kit.scanFolder) ? SourceFolder : kit.scanFolder);
                EditorUtility.SetDirty(kit);
                AssetDatabase.SaveAssetIfDirty(kit);
            }
            return kit;
        }

        /// <summary>Data-driven scan using the kit's own rules (see DmRockKitEditor).</summary>
        internal static void FillKitFromFolder(DmRockKit kit, string folder)
        {
            kit.scanFolder = folder;
            DmRockKitEditor.Scan(kit);
        }

        /// <summary>Combine recipe, initialised from the noise-rock recipe so results come out the same size.</summary>
        internal static DmRockCombineRecipe EnsureRecipe()
        {
            var r = AssetDatabase.LoadAssetAtPath<DmRockCombineRecipe>(RecipePath);
            if (r != null)
                return r;
            EnsureFolder(ParentFolder(RecipePath));
            r = ScriptableObject.CreateInstance<DmRockCombineRecipe>();
            var src = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SizeSourceRecipePath);
            if (src != null)
            {
                var so = new SerializedObject(src);
                SerializedProperty p;
                if ((p = so.FindProperty("sizeMin")) != null) r.sizeMin = p.vector3Value;
                if ((p = so.FindProperty("sizeMax")) != null) r.sizeMax = p.vector3Value;
                if ((p = so.FindProperty("uniformScaleBias")) != null) r.uniformScaleBias = p.floatValue;
                if ((p = so.FindProperty("tiltJitter")) != null) r.tiltJitter = p.floatValue;
                if ((p = so.FindProperty("bottomFlatten")) != null) r.bottomFlatten = p.floatValue;
                if ((p = so.FindProperty("addMeshCollider")) != null) r.addMeshCollider = p.boolValue;
            }
            r.kit = EnsureKit(false);
            AssetDatabase.CreateAsset(r, RecipePath);
            AssetDatabase.SaveAssetIfDirty(r);
            return r;
        }

        private static string ParentFolder(string assetPath) => System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
#endif

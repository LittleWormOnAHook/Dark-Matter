#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>
    /// Keeps a rock's material settings through moves, scaling and copies. A rock draws generated materials (bake variants,
    /// live previews, Play Mode runtime copies) that are regenerated from its blend material on the next bake / rebuild, so
    /// a value edited on one of those would be lost the next time the rock is moved, scaled or copied:
    /// - every generated variant records the blend values it was made from (tag GRB_BakeBase);
    /// - an edit on a generated material is written into the rock's own blend right away (a rock on a shared template gets
    ///   its own per-rock blend only when it is edited, so rocks with identical settings keep sharing);
    /// - before a bake (and when a bake is dropped by a move / scale, and when a rock is copied) values edited on the
    ///   materials the rock showed are adopted the same way (older variants without the tag: what the rock shows wins);
    /// - Undo / Redo of a blend edit, and blend edits made in Play Mode, rebake the rocks that use the blend;
    /// - Play Mode: an edit on a shared runtime copy applies to the selected rocks only (clone on divergence).
    /// Terrain-derived values (dominant terrain layer, per-pixel terrain binding) are not settings and follow the ground.
    /// </summary>
    public static partial class DmRockBlendMaterials
    {
        private const string BaseTag = "GRB_BakeBase";
        private const string ShaderKey = "#shader";

        private static readonly string[] DerivedProps =
        {
            "_BaseColorMap", "_MaskMap", "_NormalMap", "_MainTex", "_GRB_BaseLumRef", "_GRB_UseTexArray",
            "_GRB_HeightRange", "_GRB_SizeRange", "_GRB_TopMaskOn", "_GRB_TopAOOn",
            "_GRB_TerrainBaseMap", "_GRB_TerrainNormalMap", "_GRB_TerrainTiling", "_GRB_TerrainUVOffset", "_GRB_TerrainNormalScale",
            "_GRB_TerrainSmoothness", "_GRB_TerrainTint",
        };

        /// <summary>Top layer properties taken from the terrain layer while Top From Terrain is on.</summary>
        private static readonly string[] TopFromTerrainProps =
        {
            "_GRB_TopBaseMap", "_GRB_TopNormalMap", "_GRB_TopHeightMap", "_GRB_TopTiling", "_GRB_TopNormalScale", "_GRB_TopTint",
            "_GRB_TopSmoothness", "_GRB_TopMaskMap", "_GRB_TopAOMap",
        };

        /// <summary>Per-pixel terrain binding (PcgTerrainSplat).</summary>
        private static readonly string[] SplatPrefixes =
        {
            "_GRB_TParams", "_GRB_TRect", "_GRB_TControl", "_GRB_THeight", "_GRB_TAlb", "_GRB_TNrm", "_GRB_TMsk", "_GRB_TST",
            "_GRB_TDiff", "_GRB_TMaskOff", "_GRB_TMaskScale", "_GRB_TLayer",
        };

        private static HashSet<string> s_derived;

        /// <summary>
        /// True for a property the rock's settings decide (copied to bake variants from the blend); false for values the bake
        /// derives from the base material, the mesh encoding or the terrain under the rock.
        /// </summary>
        private static bool IsSetting(string n, bool topFromTerrain)
        {
            if (s_derived == null)
            {
                s_derived = new HashSet<string>(DerivedProps, StringComparer.Ordinal);
                s_derived.UnionWith(BaseProps);
                s_derived.UnionWith(ArrayProps);
            }
            if (s_derived.Contains(n)) return false;
            if (n.StartsWith("unity_", StringComparison.Ordinal) || n.EndsWith("_ST", StringComparison.Ordinal) ||
                n.EndsWith("_TexelSize", StringComparison.Ordinal) || n.EndsWith("_HDR", StringComparison.Ordinal)) return false;
            if (topFromTerrain && Array.IndexOf(TopFromTerrainProps, n) >= 0) return false;
            foreach (string p in SplatPrefixes)
                if (n.StartsWith(p, StringComparison.Ordinal)) return false;
            return true;
        }

        private static string F(float v) => v.ToString("R", CultureInfo.InvariantCulture);
        private static string V(Vector4 v) => F(v.x) + "," + F(v.y) + "," + F(v.z) + "," + F(v.w);
        private static string V2(Vector2 v) => F(v.x) + "," + F(v.y);

        private static string TexId(Texture t)
        {
            if (t == null) return "-";
            return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(t, out string g, out long l) ? g + ":" + l : "n:" + t.name;
        }

        /// <summary>The material's settings (key = type letter + property name, plus the shader), canonical strings.</summary>
        private static Dictionary<string, string> Settings(Material m)
        {
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            if (m == null || m.shader == null) return d;
            bool topT = m.HasProperty("_GRB_TopFromTerrain") && m.GetFloat("_GRB_TopFromTerrain") > 0.5f;
            d[ShaderKey] = m.shader.name;
            foreach (string n in m.GetPropertyNames(MaterialPropertyType.Float)) if (IsSetting(n, topT)) d["f" + n] = F(m.GetFloat(n));
            foreach (string n in m.GetPropertyNames(MaterialPropertyType.Int)) if (IsSetting(n, topT)) d["i" + n] = m.GetInteger(n).ToString(CultureInfo.InvariantCulture);
            foreach (string n in m.GetPropertyNames(MaterialPropertyType.Vector)) if (IsSetting(n, topT)) d["v" + n] = V(m.GetVector(n));
            foreach (string n in m.GetPropertyNames(MaterialPropertyType.Texture))
                if (IsSetting(n, topT)) d["t" + n] = TexId(m.GetTexture(n)) + "@" + V2(m.GetTextureScale(n)) + "/" + V2(m.GetTextureOffset(n));
            return d;
        }

        private static string H(string s)
        {
            uint h = 2166136261;
            foreach (char ch in s) { h ^= ch; h *= 16777619; }
            return h.ToString("x8");
        }

        /// <summary>Records the settings a generated variant was made from (end of <see cref="ConfigureVariant"/>).</summary>
        private static void StampBakeBase(Material m)
        {
            if (m == null) return;
            var sb = new StringBuilder(4096);
            foreach (var kv in Settings(m)) sb.Append(kv.Key).Append('=').Append(H(kv.Value)).Append(';');
            m.SetOverrideTag(BaseTag, sb.ToString());
        }

        private static Dictionary<string, string> BakeBase(Material m)
        {
            string tag = m != null ? m.GetTag(BaseTag, false, "") : "";
            if (string.IsNullOrEmpty(tag)) return null;
            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string e in tag.Split(';'))
            {
                int i = e.LastIndexOf('=');
                if (i > 0) d[e.Substring(0, i)] = e.Substring(i + 1);
            }
            return d;
        }

        /// <summary>
        /// Settings edited on a generated material since it was made, that differ from <paramref name="blend"/> now. A value
        /// the blend itself changed since then keeps the blend's (later) value. A variant without the base tag (made before
        /// it was recorded): every setting that differs from the blend when <paramref name="untaggedDiffers"/>, else none.
        /// </summary>
        private static List<string> VariantEdits(Material variant, Material blend, bool untaggedDiffers)
        {
            var list = new List<string>();
            if (!IsBlendMaterial(variant) || !IsBlendMaterial(blend) || variant == blend) return list;
            Dictionary<string, string> baseH = BakeBase(variant);
            if (baseH == null && !untaggedDiffers) return list;
            Dictionary<string, string> now = Settings(variant), bl = Settings(blend);
            foreach (var kv in now)
            {
                if (!bl.TryGetValue(kv.Key, out string b) || kv.Value == b) continue;
                if (baseH != null && baseH.TryGetValue(kv.Key, out string h))
                {
                    if (H(kv.Value) == h) continue; // not edited on the variant (the blend changed since: the rebake follows it)
                    if (H(b) != h) continue;        // edited on both: the blend's value wins
                }
                list.Add(kv.Key);
            }
            return list;
        }

        private static bool CopySetting(Material from, Material to, string key)
        {
            if (key == ShaderKey)
            {
                if (from.shader == to.shader) return false;
                GenesisRockBlendGUI.SetForwardOnly(to, from.shader.name == ForwardShaderName);
                return true;
            }
            string n = key.Substring(1);
            if (!from.HasProperty(n) || !to.HasProperty(n)) return false;
            switch (key[0])
            {
                case 'f': to.SetFloat(n, from.GetFloat(n)); return true;
                case 'i': to.SetInteger(n, from.GetInteger(n)); return true;
                case 'v': to.SetVector(n, from.GetVector(n)); return true;
                case 't':
                    to.SetTexture(n, from.GetTexture(n));
                    to.SetTextureScale(n, from.GetTextureScale(n));
                    to.SetTextureOffset(n, from.GetTextureOffset(n));
                    return true;
            }
            return false;
        }

        /// <summary>Copies the given settings (all settings when null) from one blend material to another, then validates it.</summary>
        public static int CopyUserSettings(Material from, Material to, IEnumerable<string> keys = null)
        {
            if (!IsBlendMaterial(from) || !IsBlendMaterial(to) || from == to) return 0;
            List<string> ks = keys != null ? keys.ToList() : Settings(from).Keys.ToList();
            int n = 0;
            if (ks.Remove(ShaderKey) && CopySetting(from, to, ShaderKey)) n++; // shader first: the property set follows it
            foreach (string k in ks) if (CopySetting(from, to, k)) n++;
            if (n > 0) { GenesisRockBlendGUI.Validate(to); EditorUtility.SetDirty(to); }
            return n;
        }

        /// <summary>Bake variants (shared template variants and per-rock variants), not the per-rock blends themselves.</summary>
        public static bool IsBakeVariant(Material m)
        {
            if (m == null) return false;
            string p = AssetDatabase.GetAssetPath(m);
            return p.StartsWith(VariantFolder + "/", StringComparison.Ordinal) && !p.StartsWith(PerRockFolder + "/", StringComparison.Ordinal);
        }

        private static bool IsPreview(Material m, out Material template)
        {
            template = null;
            if (m == null || EditorUtility.IsPersistent(m)) return false;
            foreach (var kv in s_preview)
                if (kv.Value == m) { template = kv.Key.Item1; return template != null; }
            return false;
        }

        /// <summary>The blend a generated material (bake variant / preview) was made from belongs to <paramref name="blend"/>.</summary>
        private static bool MadeFrom(Material generated, Material blend)
        {
            if (generated == null || blend == null) return false;
            if (IsPreview(generated, out Material t)) return t == blend;
            return IsBakeVariant(generated) && generated.name.StartsWith(Safe(blend.name) + "__", StringComparison.Ordinal);
        }

        /// <summary>
        /// Adopts values edited on the materials the rock showed (<paramref name="shown"/>, generated from
        /// <paramref name="shownFrom"/>; null = the rock's current blend) into the rock's own blend. A rock on a shared template
        /// gets its own per-rock blend first when <paramref name="allowFork"/> (only when there is something to keep).
        /// True when the rock's blend changed.
        /// </summary>
        public static bool KeepShownEdits(DmRockCombiner c, Material[] shown, Material shownFrom, string undoName, bool allowFork = true, List<string> kept = null)
        {
            if (c == null || shown == null || shown.Length == 0 || EditorUtility.IsPersistent(c) || !c.gameObject.scene.IsValid()) return false;
            Material blend = c.BlendMaterial;
            if (shownFrom == null) shownFrom = blend;
            if (!IsBlendMaterial(blend) || !IsBlendMaterial(shownFrom)) return false;
            bool changed = false;
            foreach (Material v in shown.Distinct())
            {
                if (v == null || v == blend || !MadeFrom(v, shownFrom)) continue;
                // Untagged per-rock variants (made before the base was recorded): what the rock shows is its look. Untagged shared
                // template variants: no way to tell edits from template changes, so the template wins (as before).
                List<string> keys = VariantEdits(v, blend, IsPerRock(shownFrom));
                if (keys.Count == 0) continue;
                Material own = OwnsBlend(c) ? c.OwnBlendMaterial : (allowFork ? EnsureOwnBlend(c, undoName) : null);
                if (own == null) return changed;
                if (!string.IsNullOrEmpty(undoName)) Undo.RecordObject(own, undoName);
                if (CopyUserSettings(v, own, keys) > 0) changed = true;
                kept?.AddRange(keys);
                blend = own;
            }
            return changed;
        }

        /// <summary>Bake-drop hook (move / scale / edit): keeps edits made on the dropped bake's materials (own blend only; forks wait for the bake).</summary>
        private static void OnBakedMaterialsReleased(DmRockCombiner c, Material[] shown)
        {
            if (Application.isPlaying || DmRockBaker.Busy || !OwnsBlend(c)) return;
            var kept = new List<string>();
            if (KeepShownEdits(c, shown, null, null, false, kept))
                Debug.Log($"[DM PCG] {c.name}: kept the values edited on its baked material in {c.OwnBlendMaterial.name}: " +
                          string.Join(", ", kept.Distinct().Select(k => k == ShaderKey ? "shader" : k.Substring(1))), c);
        }

        // ------------------------------------------------------------------------------------------------
        // Edits on generated materials (renderer material of a baked / live rock)

        private static List<DmRockCombiner> UsersOf(Material m)
        {
            var users = new List<DmRockCombiner>();
            foreach (DmRockCombiner c in Object.FindObjectsByType<DmRockCombiner>(FindObjectsInactive.Exclude))
            {
                if (c.IsBaked && c.BakeData.materials != null && Array.IndexOf(c.BakeData.materials, m) >= 0) { users.Add(c); continue; }
                foreach (Renderer r in c.GetComponentsInChildren<Renderer>(true))
                    if (Array.IndexOf(r.sharedMaterials, m) >= 0) { users.Add(c); break; }
            }
            return users;
        }

        private static bool IsSelected(DmRockCombiner c)
        {
            foreach (GameObject g in Selection.gameObjects)
                if (g != null && (g.transform == c.transform || g.transform.IsChildOf(c.transform) || c.transform.IsChildOf(g.transform)))
                    return true;
            return false;
        }

        /// <summary>
        /// A bake variant / live preview was edited: the values go into the blend of each rock showing it (its own per-rock
        /// blend; a shared template's rocks: the selected ones, or every user when none is selected, get their own blend), so
        /// the next bake keeps them. Returns the blends that changed.
        /// </summary>
        private static List<Material> WriteBackGeneratedEdits(Material m)
        {
            var changed = new List<Material>();
            if (Application.isPlaying || DmRockBaker.Busy) return changed;
            bool preview = IsPreview(m, out Material previewOf);
            if (!preview && !IsBakeVariant(m)) return changed;
            List<DmRockCombiner> users = UsersOf(m);
            if (users.Count == 0) return changed;
            List<DmRockCombiner> sel = users.Where(IsSelected).ToList();
            if (sel.Count == 0) sel = users;
            const string undo = "Rock Blend (edited baked material)";
            var forkedKeys = new HashSet<string>();
            Material keptTemplate = null; // shared template of the rocks that do not take the edit
            foreach (DmRockCombiner c in users)
            {
                Material blend = c.BlendMaterial;
                if (!IsBlendMaterial(blend) || !MadeFrom(m, blend)) continue;
                List<string> keys = VariantEdits(m, blend, true);
                if (keys.Count == 0) continue;
                Material own;
                if (OwnsBlend(c)) own = c.OwnBlendMaterial;
                else if (sel.Contains(c)) { own = EnsureOwnBlend(c, undo); forkedKeys.UnionWith(keys); }
                else { keptTemplate = blend; continue; }
                if (own == null) continue;
                Undo.RecordObject(own, undo);
                if (CopyUserSettings(m, own, keys) > 0) changed.Add(own);
            }
            if (changed.Count > 0)
            {
                // Base = what it shows now: the edit is in the blend, nothing left to adopt (the rebake regenerates it anyway).
                if (IsPerRock(previewOf) || (!preview && users.All(OwnsBlend))) StampBakeBase(m);
                else if (preview) RefreshPreviews(previewOf); // shared template preview: back to the template for the rocks not edited
                else if (keptTemplate != null && forkedKeys.Count > 0) CopyUserSettings(keptTemplate, m, forkedKeys); // shared variant: same
                foreach (DmRockCombiner c in users) if (!c.IsBaked) c.RefreshLiveMaterials();
            }
            return changed;
        }

        // ------------------------------------------------------------------------------------------------
        // Undo / Redo and Play Mode edits: rebake rocks whose blend no longer matches their bake

        private static readonly Dictionary<DmRockCombiner, string> s_blendAtBake = new Dictionary<DmRockCombiner, string>();
        private static readonly HashSet<Material> s_editedInPlay = new HashSet<Material>();
        private static bool s_undoQueued;

        /// <summary>Called after a successful bake: the blend content the bake was made from.</summary>
        public static void NoteBaked(DmRockCombiner c)
        {
            if (c == null) return;
            Material b = c.BlendMaterial;
            if (b != null) s_blendAtBake[c] = DmRockBaker.ContentHash(b); else s_blendAtBake.Remove(c);
        }

        private static void OnUndoRedo()
        {
            if (EditorApplication.isPlaying) RefreshPlayShown();
            if (s_undoQueued) return;
            s_undoQueued = true;
            EditorApplication.delayCall += AfterUndoRedo;
        }

        private static void AfterUndoRedo()
        {
            s_undoQueued = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            RefreshPreviews(null);
            foreach (DmRockCombiner c in s_blendAtBake.Keys.ToList())
            {
                if (c == null) { s_blendAtBake.Remove(c); continue; }
                Material b = c.BlendMaterial;
                if (!c.IsBaked || b == null || !c.BakeSettings.rebakeAfterEdit) continue;
                if (DmRockBaker.ContentHash(b) != s_blendAtBake[c]) DmRockBakeScheduler.Request(c, 0.6f, false);
            }
            foreach (DmRockCombiner c in Object.FindObjectsByType<DmRockCombiner>(FindObjectsInactive.Exclude))
                if (!c.IsBaked && c.BlendMaterial != null) c.RefreshLiveMaterials();
        }

        private static void OnPlayModeChanged(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.ExitingPlayMode) { CapturePlayEdits(); DropPlayShown(); return; }
            if (s != PlayModeStateChange.EnteredEditMode) return;
            var edited = s_editedInPlay.Where(m => m != null).ToList();
            s_editedInPlay.Clear();
            // Blend assets edited in Play: Play Mode cannot rebake, so the rocks would show the old values until their next move.
            // Wait until the edit-mode scene is back and the editor is idle (a delayCall can still run inside the transition).
            double due = EditorApplication.timeSinceStartup + 1.0;
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < due) return;
                EditorApplication.update -= tick;
                ApplyPlayEdits();
                var set = new HashSet<Material>(edited);
                foreach (DmRockCombiner c in Object.FindObjectsByType<DmRockCombiner>(FindObjectsInactive.Exclude))
                    if (c.IsBaked && c.BlendMaterial != null && set.Contains(c.BlendMaterial) && c.BakeSettings.rebakeAfterEdit)
                        DmRockBakeScheduler.Request(c, 0.6f, false);
                AfterUndoRedo(); // also blends edited in Play through other paths (rocks baked this session)
            };
            EditorApplication.update += tick;
        }

        /// <summary>Play Mode edit on a shared runtime copy: only the selected rocks drawing it take the edit.</summary>
        private static void DivergePlayEdit(Material m)
        {
            if (!PcgTerrainSplat.IsRuntimeCopy(m)) return;
            var users = new List<DmRockCombiner>();
            foreach (DmRockCombiner c in Object.FindObjectsByType<DmRockCombiner>(FindObjectsInactive.Exclude))
                foreach (Renderer r in c.GetComponentsInChildren<Renderer>(true))
                    if (Array.IndexOf(r.sharedMaterials, m) >= 0) { users.Add(c); break; }
            List<DmRockCombiner> sel = users.Where(IsSelected).ToList();
            if (sel.Count == 0 || sel.Count == users.Count) { Shown(m); return; } // every rock drawing it takes the edit
            // The other rocks go back to the copy as it was before this edit (earlier edits made for all of them stay).
            s_playShown.TryGetValue(m, out Material before);
            Material split = PcgTerrainSplat.Diverge(m, sel.Select(c => c.gameObject), before);
            Shown(m);
            if (split != null)
            {
                Shown(split);
                Debug.Log($"[DM PCG] Play Mode: {sel.Count} selected rock(s) now use their own copy of {m.name} ({users.Count - sel.Count} other rock(s) keep the shared one). The edits go into those rocks' blends when Play Mode ends.", sel[0]);
                Selection.objects = Selection.objects; // inspector shows the new copy
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Play Mode edits on runtime copies (shared or split off): kept in the blends of the rocks drawing them on exit

        private sealed class PlayEdit
        {
            public string gid, rock;
            public Material values;
            public List<string> keys;
        }

        private static readonly List<PlayEdit> s_playEdits = new List<PlayEdit>();

        // Each runtime copy's values after the last edit handled (Diverge puts the other rocks back to these).
        private static readonly Dictionary<Material, Material> s_playShown = new Dictionary<Material, Material>();

        private static void Shown(Material m)
        {
            if (m == null) return;
            if (!s_playShown.TryGetValue(m, out Material s) || s == null)
            {
                s_playShown[m] = new Material(m) { name = m.name + " (shown)", hideFlags = HideFlags.HideAndDontSave };
                return;
            }
            if (s.shader != m.shader) s.shader = m.shader;
            s.CopyPropertiesFromMaterial(m);
            s.shaderKeywords = m.shaderKeywords;
            s.renderQueue = m.renderQueue;
        }

        /// <summary>Undo / Redo in Play Mode: the copies' values changed outside an edit.</summary>
        private static void RefreshPlayShown()
        {
            foreach (Material m in s_playShown.Keys.ToList())
                if (m != null) Shown(m); else s_playShown.Remove(m);
        }

        private static void DropPlayShown()
        {
            foreach (Material s in s_playShown.Values)
                if (s != null) Object.DestroyImmediate(s);
            s_playShown.Clear();
        }

        private static string KeyName(string k) => k == ShaderKey ? "shader" : k.Substring(1).Replace("_GRB_", "");

        /// <summary>Exiting Play Mode: every runtime copy whose settings differ from its source, with the scene rocks drawing it.</summary>
        private static void CapturePlayEdits()
        {
            DropPlayEdits();
            List<KeyValuePair<Material, Material>> copies = PcgTerrainSplat.RuntimeCopies();
            if (copies.Count == 0) return;
            var users = new Dictionary<Material, List<DmRockCombiner>>();
            foreach (DmRockCombiner c in Object.FindObjectsByType<DmRockCombiner>(FindObjectsInactive.Exclude))
                foreach (Renderer r in c.GetComponentsInChildren<Renderer>(true))
                    foreach (Material m in r.sharedMaterials)
                    {
                        if (m == null || !PcgTerrainSplat.IsRuntimeCopy(m)) continue;
                        if (!users.TryGetValue(m, out List<DmRockCombiner> l)) users[m] = l = new List<DmRockCombiner>();
                        if (!l.Contains(c)) l.Add(c);
                    }
            foreach (var kv in copies)
            {
                if (!users.TryGetValue(kv.Key, out List<DmRockCombiner> us)) continue;
                Dictionary<string, string> now = Settings(kv.Key), src = Settings(kv.Value);
                List<string> keys = now.Where(p => src.TryGetValue(p.Key, out string v) && v != p.Value).Select(p => p.Key).ToList();
                if (keys.Count == 0) continue;
                Material values = null;
                foreach (DmRockCombiner c in us)
                {
                    GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(c);
                    if (id.identifierType != 2 || id.targetObjectId == 0) continue; // not a scene object
                    values ??= new Material(kv.Key) { name = kv.Key.name + " (Play edits)", hideFlags = HideFlags.HideAndDontSave };
                    s_playEdits.Add(new PlayEdit { gid = id.ToString(), rock = c.name, values = values, keys = keys });
                }
            }
        }

        private static void DropPlayEdits()
        {
            foreach (Material m in s_playEdits.Select(e => e.values).Distinct())
                if (m != null) Object.DestroyImmediate(m);
            s_playEdits.Clear();
        }

        /// <summary>Back in Edit Mode: the captured values go into each rock's own blend (Undo-able), then the rock is rebaked.</summary>
        private static void ApplyPlayEdits()
        {
            if (s_playEdits.Count == 0) return;
            const string undo = "Play Mode material edits";
            var done = new List<string>();
            var rocks = new HashSet<DmRockCombiner>();
            foreach (PlayEdit e in s_playEdits)
            {
                if (e.values == null || !GlobalObjectId.TryParse(e.gid, out GlobalObjectId id)) continue;
                var c = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as DmRockCombiner;
                if (c == null) continue; // made in Play Mode: gone after exit
                if (!IsBlendMaterial(c.BlendMaterial)) continue;
                // Values the rock already showed (older bake variants) stay as well, so the rebake below does not undo them.
                if (c.IsBaked) KeepShownEdits(c, c.BakeData.materials, null, undo);
                Material own = OwnsBlend(c) ? c.OwnBlendMaterial : EnsureOwnBlend(c, undo);
                if (own == null) continue;
                Undo.RecordObject(own, undo);
                if (CopyUserSettings(e.values, own, e.keys) == 0) continue;
                // The bake variant still holds the pre-Play values: mark them as generated, not edited, so the blend wins.
                if (c.IsBaked && c.BakeData.materials != null)
                    foreach (Material v in c.BakeData.materials)
                        if (MadeFrom(v, own)) StampBakeBase(v);
                rocks.Add(c);
                done.Add(c.name + " -> " + own.name + ": " + string.Join(", ", e.keys.Select(KeyName)));
            }
            DropPlayEdits();
            foreach (DmRockCombiner c in rocks)
            {
                if (c.IsBaked) DmRockBakeScheduler.Request(c, 0.6f, false);
                else c.RefreshLiveMaterials();
            }
            if (done.Count > 0) Debug.Log("[DM PCG] Play Mode material edits kept: " + string.Join("; ", done));
        }

        [InitializeOnLoadMethod]
        private static void RegisterRetainHooks()
        {
            DmRockCombiner.BakedMaterialsReleasedHook = OnBakedMaterialsReleased;
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }
    }
}
#endif
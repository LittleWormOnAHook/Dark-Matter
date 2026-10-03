#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GenesisPCG.RockCreation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>
    /// Tools/Genesis PCG Rock Creation/Rock Palette: grid of presets. Click a preset, then click in the Scene view to place
    /// a rock there (snapped, conformed, baked, new seed per click). Shift+click keeps placing; Esc cancels.
    /// </summary>
    public sealed class DmRockPaletteWindow : EditorWindow
    {
        private const string ThumbCacheDir = "Library/GenesisPCGRockPalette";
        private static readonly Dictionary<string, Texture2D> s_thumbs = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Texture2D> s_partial = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, double> s_retryAt = new Dictionary<string, double>();

        private List<DmRockPreset> m_presets = new List<DmRockPreset>();
        private DmRockPreset m_selected;
        private bool m_armed;
        private Vector2 m_scroll;
        private float m_tile = 112f;
        private string m_status = "";

        [MenuItem("Tools/Genesis PCG Rock Creation/Rock Palette")]
        public static void Open()
        {
            var w = GetWindow<DmRockPaletteWindow>();
            w.titleContent = new GUIContent("Rock Palette");
            w.minSize = new Vector2(260f, 240f);
            w.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            RefreshList();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            m_armed = false;
        }

        private void OnProjectChange() => RefreshList();

        private void RefreshList()
        {
            m_presets = DmRockLibrary.AllPresets();
            if (m_selected != null && !m_presets.Contains(m_selected)) { m_selected = null; m_armed = false; }
            Repaint();
        }

        // ------------------------------------------------------------------------------------------------
        // GUI

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60))) { s_thumbs.Clear(); RefreshList(); }
                if (GUILayout.Button(new GUIContent("Build Library", "Create missing styles, presets and materials."), EditorStyles.toolbarButton, GUILayout.Width(90)))
                {
                    Debug.Log("Genesis PCG Rock Creation library:\n" + DmRockLibrary.BuildAll(false));
                    RefreshList();
                }
                GUILayout.FlexibleSpace();
                m_tile = GUILayout.HorizontalSlider(m_tile, 72f, 180f, GUILayout.Width(90));
            }

            if (m_armed && m_selected != null)
                EditorGUILayout.HelpBox($"Placing '{Label(m_selected)}': click in the Scene view. Shift+click keeps placing, Esc cancels.", MessageType.Info);
            else if (!string.IsNullOrEmpty(m_status))
                EditorGUILayout.HelpBox(m_status, MessageType.None);

            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
            float w = position.width - 20f;
            int cols = Mathf.Max(1, Mathf.FloorToInt(w / (m_tile + 6f)));
            bool renderedOne = false;
            for (int i = 0; i < m_presets.Count; i += cols)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int j = i; j < Mathf.Min(m_presets.Count, i + cols); j++)
                    {
                        DmRockPreset p = m_presets[j];
                        if (p == null) continue;
                        Rect r = GUILayoutUtility.GetRect(m_tile, m_tile + 30f, GUILayout.Width(m_tile), GUILayout.Height(m_tile + 30f));
                        bool sel = p == m_selected;
                        if (sel) EditorGUI.DrawRect(r, m_armed ? new Color(0.2f, 0.55f, 0.9f, 0.6f) : new Color(0.4f, 0.4f, 0.4f, 0.5f));
                        Rect img = new Rect(r.x + 4f, r.y + 4f, m_tile - 8f, m_tile - 8f);
                        Texture2D t = GetThumb(p, !renderedOne, out bool rendered);
                        renderedOne |= rendered;
                        if (t != null) GUI.DrawTexture(img, t, ScaleMode.ScaleToFit);
                        else EditorGUI.DrawRect(img, new Color(0.15f, 0.15f, 0.15f));
                        GUI.Label(new Rect(r.x, r.y + m_tile - 2f, m_tile, 30f), Label(p), new GUIStyle(EditorStyles.miniLabel) { wordWrap = true, alignment = TextAnchor.UpperCenter });
                        if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                        {
                            if (Event.current.clickCount == 2) { EditorGUIUtility.PingObject(p); Selection.activeObject = p; }
                            else if (sel && m_armed) m_armed = false;
                            else { m_selected = p; m_armed = true; FocusScene(); }
                            Event.current.Use();
                            Repaint();
                        }
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            if (renderedOne || s_partial.Count > 0) Repaint();

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                var rock = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<DmRockCombiner>() : null;
                using (new EditorGUI.DisabledScope(rock == null))
                    if (GUILayout.Button(new GUIContent("New Preset from Selected Rock", "Saves the selected rock's kit / style / recipe / material / passage+nook settings as a preset.")))
                    {
                        DmRockPreset np = NewPresetFromRock(rock, true);
                        if (np != null) { RefreshList(); m_selected = np; m_armed = false; }
                    }
                using (new EditorGUI.DisabledScope(m_selected == null || !Selection.gameObjects.Any(g => g != null && g.GetComponent<DmRockCombiner>() != null)))
                    if (GUILayout.Button(new GUIContent("Apply Preset to Selected", "Keeps each rock's seed and rebuilds (baked rocks are rebaked).")))
                    {
                        int n = 0;
                        foreach (GameObject g in Selection.gameObjects)
                        {
                            var c = g != null ? g.GetComponent<DmRockCombiner>() : null;
                            if (c == null) continue;
                            ApplyPresetTo(c, m_selected);
                            n++;
                        }
                        m_status = $"Applied '{Label(m_selected)}' to {n} rock(s).";
                    }
            }
            if (m_armed && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) { m_armed = false; Event.current.Use(); Repaint(); }
        }

        private static void FocusScene()
        {
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Focus();
        }

        public static string Label(DmRockPreset p) => p == null ? "" : p.name.Replace("DM_RockPreset_", "").Replace("Style", "Style: ");

        // ------------------------------------------------------------------------------------------------
        // Scene placement

        private void OnSceneGUI(SceneView sv)
        {
            if (!m_armed || m_selected == null) return;
            Event e = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(id);
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                m_armed = false; e.Use(); Repaint(); sv.Repaint();
                return;
            }
            PcgSurfaceSnapSettings s = m_selected.recipe != null && m_selected.recipe.snap != null ? m_selected.recipe.snap : PcgSurfaceSnapSettings.Default;
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            bool hit = PcgSurfaceSnap.RaycastSurface(ray, s, -1, null, out PcgSurfaceSnap.Result r);
            if (hit && e.type == EventType.Repaint)
            {
                float rad = m_selected.style != null ? m_selected.style.footprintRadius.y : 3f;
                Handles.color = new Color(0.3f, 0.8f, 1f, 0.9f);
                Handles.DrawWireDisc(r.point, r.normal, rad);
                Handles.DrawLine(r.point, r.point + r.normal * 1.5f);
                Handles.Label(r.point + Vector3.up * 1.6f, Label(m_selected) + (e.shift ? "  (Shift: keep placing)" : ""));
            }
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag) sv.Repaint();
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && GUIUtility.hotControl == 0 && HandleUtility.nearestControl == id)
            {
                if (hit)
                {
                    DmRockCombiner c = PlaceAt(m_selected, r.point, !e.shift);
                    m_status = c != null ? $"Placed {c.name} (seed {c.Seed})." : "Placement failed.";
                    if (!e.shift) m_armed = false;
                    Repaint();
                }
                e.Use();
            }
        }

        /// <summary>Creates a rock from <paramref name="p"/> at <paramref name="point"/>: new seed, snapped, built on the ground there, bake requested.</summary>
        public static DmRockCombiner PlaceAt(DmRockPreset p, Vector3 point, bool select)
        {
            if (p == null) return null;
            var go = new GameObject("Rock_" + Label(p).Replace("Style: ", "").Replace(" ", ""));
            go.SetActive(false);
            if (go.scene != SceneManager.GetActiveScene()) SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
            go.transform.position = point;
            var c = go.AddComponent<DmRockCombiner>();
            c.ApplyPreset(p, false);
            c.InitSeed(PcgSeededPasteWatcher.NewSeed());
            PcgSurfaceSnap.Snap(go.transform, c.SnapSettings);
            go.SetActive(true); // builds on the ground at the snapped pose
            go.transform.hasChanged = false;
            Undo.RegisterCreatedObjectUndo(go, "Place Rock (Palette)");
            DmRockBakeScheduler.Request(c, 0.15f, false);
            if (select) Selection.activeGameObject = go;
            return c;
        }

        /// <summary>Uses the preset for every slot, keeps the seed and rebuilds (a baked rock is rebaked by the scheduler).</summary>
        public static void ApplyPresetTo(DmRockCombiner c, DmRockPreset p)
        {
            if (c == null || p == null) return;
            bool wasBaked = c.IsBaked;
            Undo.RecordObject(c, "Apply Rock Preset");
            c.ApplyPreset(p, true);
            EditorUtility.SetDirty(c);
            if (wasBaked) DmRockBakeScheduler.Request(c, 0.2f, false);
        }

        /// <summary>Preset asset from the rock's effective slots. <paramref name="path"/> null = save dialog (or an automatic name when !interactive).</summary>
        public static DmRockPreset NewPresetFromRock(DmRockCombiner c, bool interactive, string path = null)
        {
            if (c == null) return null;
            DmRockLibrary.EnsureFolder(DmRockLibrary.PresetsFolder);
            string baseName = "DM_RockPreset_" + (c.Style != null ? c.Style.name.Replace("DM_RockStyle_", "") : "Rock") + "_Custom";
            if (path == null)
            {
                path = interactive
                    ? EditorUtility.SaveFilePanelInProject("New Rock Preset", baseName, "asset", "Save the rock's settings as a preset.", DmRockLibrary.PresetsFolder)
                    : AssetDatabase.GenerateUniqueAssetPath($"{DmRockLibrary.PresetsFolder}/{baseName}.asset");
                if (string.IsNullOrEmpty(path)) return null;
            }
            var p = ScriptableObject.CreateInstance<DmRockPreset>();
            p.kit = c.Kit; p.style = c.Style; p.recipe = c.Recipe; p.materialOverride = c.MaterialOverride;
            p.features = c.Features.Clone();
            p.description = $"From {c.name} (seed {c.Seed}).";
            AssetDatabase.CreateAsset(p, path);
            AssetDatabase.SaveAssetIfDirty(p);
            if (interactive) EditorGUIUtility.PingObject(p);
            return p;
        }

        // ------------------------------------------------------------------------------------------------
        // Thumbnails (rendered once per preset settings, cached in memory and under Library/)

        private static string ThumbKey(DmRockPreset p)
        {
            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(p));
            string json = EditorJsonUtility.ToJson(p) + (p.style != null ? EditorJsonUtility.ToJson(p.style) : "") + (p.materialOverride != null ? p.materialOverride.name : "");
            return guid + "_" + Hash128.Compute(json + "|thumb-v2|" + ThumbLightScale);
        }

        private static Texture2D GetThumb(DmRockPreset p, bool mayRender, out bool rendered)
        {
            rendered = false;
            string key = ThumbKey(p);
            if (s_thumbs.TryGetValue(key, out Texture2D t) && t != null) return t;
            string file = Path.Combine(ThumbCacheDir, key + ".png");
            if (File.Exists(file))
            {
                t = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                t.LoadImage(File.ReadAllBytes(file));
                s_thumbs[key] = t;
                return t;
            }
            if (!mayRender || EditorApplication.isCompiling || EditorApplication.isUpdating) return null;
            if (s_retryAt.TryGetValue(key, out double at) && EditorApplication.timeSinceStartup < at)
                return s_partial.TryGetValue(key, out Texture2D pt) ? pt : null;
            rendered = true;
            t = RenderThumb(p, 160, out bool complete);
            if (t == null) { s_retryAt[key] = double.MaxValue; return null; } // failed: don't retry this session
            if (!complete)
            {
                // Shaders still compiling (async): show this one, render again shortly, don't write the disk cache yet.
                if (s_partial.TryGetValue(key, out Texture2D old) && old != null) DestroyImmediate(old);
                s_partial[key] = t;
                s_retryAt[key] = EditorApplication.timeSinceStartup + 1.5;
                return t;
            }
            if (s_partial.TryGetValue(key, out Texture2D stale) && stale != null) DestroyImmediate(stale);
            s_partial.Remove(key);
            s_retryAt.Remove(key);
            Directory.CreateDirectory(ThumbCacheDir);
            File.WriteAllBytes(file, t.EncodeToPNG());
            s_thumbs[key] = t;
            return t;
        }

        /// <summary>Optional diagnostics: when set, RenderThumb appends one line per stage (with ms) to this file.</summary>
        public static string ThumbTraceFile;
        /// <summary>Thumbnail light multiplier (HDRP preview lights use physical units; 1 = built-in-pipeline style values).</summary>
        public static float ThumbLightScale = 16f;
        public static float ThumbAmbient = 0.25f;

        static void Trace(System.Diagnostics.Stopwatch sw, string stage)
        {
            if (string.IsNullOrEmpty(ThumbTraceFile)) return;
            try { File.AppendAllText(ThumbTraceFile, stage + " " + sw.ElapsedMilliseconds + "ms\n"); } catch { }
        }

        public static Texture2D RenderThumb(DmRockPreset p, int size) => RenderThumb(p, size, out _);

        /// <summary>
        /// Renders a preset thumbnail in an isolated preview scene. Shader variants are compiled asynchronously (a static
        /// preview would otherwise compile HDRP variants synchronously and can stall the editor for minutes); when shaders
        /// were still compiling, <paramref name="complete"/> is false and the caller should retry later instead of caching.
        /// </summary>
        public static Texture2D RenderThumb(DmRockPreset p, int size, out bool complete)
        {
            complete = false;
            if (p == null) return null;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Trace(sw, "begin " + p.name);
            var pru = new PreviewRenderUtility();
            GameObject go = null;
            RenderTexture rt = null;
            RenderTexture prevActive = RenderTexture.active;
            bool prevAsync = ShaderUtil.allowAsyncCompilation;
            try
            {
                go = EditorUtility.CreateGameObjectWithHideFlags("RockPaletteThumb", HideFlags.HideAndDontSave);
                go.SetActive(false);
                var c = go.AddComponent<DmRockCombiner>();
                c.flatGroundForPreview = true;
                c.ApplyPreset(p, false);
                c.InitSeed(12345);
                pru.AddSingleGO(go);
                Trace(sw, "added");
                go.SetActive(true); // OnEnable -> Rebuild (flat ground, no surface conform)
                Trace(sw, "built");
                var mf = go.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null || mf.sharedMesh.vertexCount == 0) return null;
                var mc = go.GetComponent<MeshCollider>();
                if (mc != null) DestroyImmediate(mc); // not needed for a picture; keeps the preview out of physics
                Bounds b = mf.sharedMesh.bounds;
                // Frame on the larger of the horizontal / vertical half-sizes (the bounding-sphere fit left the rock tiny).
                float radius = Mathf.Max(0.5f, Mathf.Max(b.extents.y, Mathf.Sqrt(b.extents.x * b.extents.x + b.extents.z * b.extents.z)) * 0.9f);
                Camera cam = pru.camera;
                cam.fieldOfView = 30f;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = radius * 10f + 50f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.17f, 0.18f, 0.2f, 1f);
                Quaternion view = Quaternion.Euler(22f, 35f, 0f);
                float dist = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
                cam.transform.SetPositionAndRotation(b.center - view * Vector3.forward * dist, view);
                pru.lights[0].intensity = 1.3f * ThumbLightScale;
                pru.lights[0].transform.rotation = Quaternion.Euler(45f, 30f, 0f);
                if (pru.lights.Length > 1) { pru.lights[1].intensity = 0.6f * ThumbLightScale; pru.lights[1].transform.rotation = Quaternion.Euler(340f, 218f, 177f); }
                pru.ambientColor = new Color(ThumbAmbient, ThumbAmbient, ThumbAmbient * 1.08f);

                rt = RenderTexture.GetTemporary(new RenderTextureDescriptor(size, size, RenderTextureFormat.ARGB32, 24) { sRGB = true, msaaSamples = 1 });
                foreach (Light l in pru.lights) if (l != null) l.enabled = true;
                cam.targetTexture = rt;
                cam.aspect = 1f;
                ShaderUtil.allowAsyncCompilation = true;
                Trace(sw, "render");
                cam.Render();
                Trace(sw, "rendered");
                complete = !ShaderUtil.anythingCompiling;
                cam.targetTexture = null;
                foreach (Light l in pru.lights) if (l != null) l.enabled = false;

                RenderTexture.active = rt;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                tex.ReadPixels(new Rect(0, 0, size, size), 0, 0, false);
                tex.Apply(false);
                Trace(sw, "done complete=" + complete);
                return tex;
            }
            catch (System.Exception e)
            {
                Trace(sw, "exception " + e.GetType().Name + ": " + e.Message);
                Debug.LogWarning("Rock Palette thumbnail failed for " + p.name + ": " + e.Message);
                return null;
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = prevAsync;
                RenderTexture.active = prevActive;
                if (pru.camera != null) pru.camera.targetTexture = null;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                if (go != null) DestroyImmediate(go);
                pru.Cleanup();
                Trace(sw, "cleanup");
            }
        }
    }
}
#endif

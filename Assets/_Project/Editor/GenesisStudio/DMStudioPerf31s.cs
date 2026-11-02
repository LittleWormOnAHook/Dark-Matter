// 2026-10-31s: Studio Performance tools: sun shadow budget (cascades + distance), small-prop shadow culling
// (logged + revertable, prefab-level where possible), enemy AI tick spreading. Drawn under Sky Essentials.
using System.Collections.Generic;
using System.IO;
using System.Text;
using Project.AI;
using Project.World.Sky;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Project.EditorTools.GenesisStudio
{
    public static class DMStudioPerf31s
    {
        private const string FoldKey = "dm.perf31s.fold";
        private const string SizeKey = "dm.perf31s.size", DistKey = "dm.perf31s.dist";
        private const string LogDir = "Logs";
        private static readonly string[] PresetNames = { "Subtle", "Default", "Extreme" };
        private static readonly float[] PresetSize = { 0.5f, 1.0f, 2.0f };   // bounds diagonal (m) below which a prop stops casting
        private static readonly float[] PresetDist = { 0f, 250f, 150f };     // distance (m) from the Player / scene camera beyond which a prop stops casting (0 = off)
        private static readonly string[] VendorRoots = { "FORGE3D", "BOTD", "Gaia", "Procedural Worlds" };
        private static readonly string[] KeepTypeNames = { "EnemyAiController", "PlayerController", "CompanionFollowController", "PetController", "Animator", "vRagdoll", "WeaponHitbox", "EnemyCombat", "EnemyInvectorLoadoutBridge" };
        private static string lastPreview = "";

        public static void Draw(DM_IoSkyProfile p)
        {
            bool fold = EditorGUILayout.Foldout(EditorPrefs.GetBool(FoldKey, false), "Performance (31s)", true);
            EditorPrefs.SetBool(FoldKey, fold);
            if (!fold) return;
            EditorGUI.indentLevel++;

            // --- sun shadow budget (profile -> DM shadow volume)
            EditorGUILayout.LabelField("Sun shadow budget", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            int cc = EditorGUILayout.IntSlider(new GUIContent("Sun shadow cascades", "HDShadowSettings cascade count on the DM shadow volume."), p.shadowCascadeCount31s, 1, 4);
            float sd = EditorGUILayout.Slider(new GUIContent("Sun shadow distance (m)"), p.shadowMaxDistance, 20f, 1000f);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(p, "Shadow budget"); p.shadowCascadeCount31s = cc; p.shadowMaxDistance = sd; EditorUtility.SetDirty(p); }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15f);
                for (int i = 0; i < 3; i++) if (GUILayout.Button(PresetNames[i], EditorStyles.miniButton)) { Undo.RecordObject(p, "Shadow budget preset"); p.ApplyShadowBudgetPreset31s(i); EditorUtility.SetDirty(p); }
            }

            // --- 11-01b: SSR quality (profile -> DM runtime volume)
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Screen-space reflections (11-01b)", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            bool ssrOn = EditorGUILayout.Toggle("SSR on", p.ssrEnabled);
            int sq = EditorGUILayout.IntPopup("SSR quality (Keep = volume)", p.ssrQuality1101b, new[] { "Keep volume", "Low", "Medium", "High", "Custom" }, new[] { -1, 0, 1, 2, 3 });
            int ss = EditorGUILayout.IntSlider(new GUIContent("  Custom max steps"), p.ssrMaxSteps1101b, 4, 128);
            float sm = EditorGUILayout.Slider(new GUIContent("  Custom min smoothness", "Higher = fewer surfaces reflect = cheaper."), p.ssrMinSmoothness1101b, 0f, 1f);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(p, "SSR quality"); p.ssrEnabled = ssrOn; p.ssrQuality1101b = sq; p.ssrMaxSteps1101b = ss; p.ssrMinSmoothness1101b = sm; EditorUtility.SetDirty(p); }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15f);
                for (int i = 0; i < 3; i++) if (GUILayout.Button(PresetNames[i], EditorStyles.miniButton)) { Undo.RecordObject(p, "SSR preset"); p.ApplySsrPreset1101b(i); EditorUtility.SetDirty(p); }
            }

            // --- 11-01b: Pre-Prologue Set lights + 30m shadow cull
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Pre-Prologue Set lights (11-01b)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("  " + DMPrePrologueLights1101b.Stats(), EditorStyles.miniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15f);
                for (int i = 0; i < 3; i++) if (GUILayout.Button(new[] { "Subtle (8 hero)", "Default (4 hero)", "Extreme (0 hero)" }[i], EditorStyles.miniButton)) Debug.Log("[DM] " + DMPrePrologueLights1101b.Apply(new[] { 8, 4, 0 }[i]));
                if (GUILayout.Button("Revert lights", EditorStyles.miniButton)) Debug.Log("[DM] " + DMPrePrologueLights1101b.Revert());
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15f);
                if (GUILayout.Button("Apply 30m shadow cull", EditorStyles.miniButton)) Debug.Log("[DM] 30m " + DMStudioPerf30m.Apply());
                if (GUILayout.Button("Restore 30m", EditorStyles.miniButton)) Debug.Log("[DM] 30m restore " + DMStudioPerf30m.Restore());
            }
            // --- small / distant prop shadow culling
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Prop shadow culling (open scenes)", EditorStyles.boldLabel);
            float size = EditorGUILayout.Slider(new GUIContent("Max prop size (m)", "Renderers whose bounds diagonal is below this stop casting shadows."), EditorPrefs.GetFloat(SizeKey, PresetSize[1]), 0.05f, 10f);
            float dist = EditorGUILayout.Slider(new GUIContent("Beyond distance (m, 0 = off)", "Also stop casting beyond this distance from the Player (or the Scene view camera)."), EditorPrefs.GetFloat(DistKey, PresetDist[1]), 0f, 2000f);
            EditorPrefs.SetFloat(SizeKey, size); EditorPrefs.SetFloat(DistKey, dist);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15f);
                for (int i = 0; i < 3; i++) if (GUILayout.Button(PresetNames[i], EditorStyles.miniButton)) { EditorPrefs.SetFloat(SizeKey, PresetSize[i]); EditorPrefs.SetFloat(DistKey, PresetDist[i]); }
            }
            EditorGUILayout.HelpBox("Kept casting: player, enemies, companions, pets, anything animated/skinned, terrain, and anything bigger than the size (buildings). Prefab instances from _Project prefabs are changed on the prefab asset; everything else on the scene object (save the scene to keep). Log: Logs/shadowcull31s_*.tsv", MessageType.None);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(EditorGUI.indentLevel * 15f);
                if (GUILayout.Button("Preview")) lastPreview = Run(size, dist, false);
                if (GUILayout.Button("Apply")) lastPreview = Run(size, dist, true);
                if (GUILayout.Button("Revert last")) lastPreview = RevertLast();
            }
            if (!string.IsNullOrEmpty(lastPreview)) EditorGUILayout.HelpBox(lastPreview, MessageType.Info);

            // --- enemy AI tick spreading
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Enemy AI tick spreading", EditorStyles.boldLabel);
            DMEnemyAiTickSettings31s ts = EnsureTickAsset();
            EditorGUI.BeginChangeCheck();
            bool st = EditorGUILayout.Toggle(new GUIContent("Spread calm-enemy AI ticks", "Calm (not engaged) enemies re-check line of sight every N frames, staggered per enemy. Engaged enemies tick every frame (combat unchanged)."), ts.staggerEnabled);
            int iv = EditorGUILayout.IntSlider("Tick interval (frames)", ts.tickIntervalFrames, 1, 8);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(ts, "AI tick"); ts.staggerEnabled = st; ts.tickIntervalFrames = iv; EditorUtility.SetDirty(ts); AssetDatabase.SaveAssetIfDirty(ts); }

            // --- 31u: enemy hearing (DM_CombatCoreProfile)
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Enemy hearing (31u)", EditorStyles.boldLabel);
            var cp = AssetDatabase.LoadAssetAtPath<Project.Combat.DM_CombatCoreProfile>("Assets/_Project/Resources/Combat/DM_CombatCoreProfile.asset");
            if (cp != null)
            {
                EditorGUI.BeginChangeCheck();
                float gh = EditorGUILayout.Slider(new GUIContent("Gunshot hearing radius (m)", "Enemies inside turn and investigate toward the shooter."), cp.gunshotHearingRadius, 0f, 300f);
                float ga = EditorGUILayout.Slider(new GUIContent("Gunshot aggro radius (m)", "Enemies this close may aggro straight away (heard-aggro chance)."), cp.gunshotAggroRadius31u, 0f, 200f);
                float ri = EditorGUILayout.Slider(new GUIContent("Ranged impact radius (m)", "Bullet impacts (hits or misses) alert enemies within this radius."), cp.rangedImpactAlertRadius, 0f, 150f);
                float mi = EditorGUILayout.Slider(new GUIContent("Melee hit radius (m)", "Player melee hits alert enemies within this radius."), cp.meleeImpactHearingRadius31u, 0f, 100f);
                float hc = EditorGUILayout.Slider(new GUIContent("Heard aggro chance", "-1 = each enemy's own value."), cp.heardAggroChance31u, -1f, 1f);
                if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(cp, "Enemy hearing"); cp.gunshotHearingRadius = gh; cp.gunshotAggroRadius31u = ga; cp.rangedImpactAlertRadius = ri; cp.meleeImpactHearingRadius31u = mi; cp.heardAggroChance31u = hc; EditorUtility.SetDirty(cp); AssetDatabase.SaveAssetIfDirty(cp); }
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(EditorGUI.indentLevel * 15f);
                    for (int i = 0; i < 3; i++) if (GUILayout.Button(PresetNames[i], EditorStyles.miniButton)) { Undo.RecordObject(cp, "Hearing preset"); cp.ApplyHearingPreset31u(i); EditorUtility.SetDirty(cp); AssetDatabase.SaveAssetIfDirty(cp); }
                }
            }
            EditorGUI.indentLevel--;
        }

        private static DMEnemyAiTickSettings31s EnsureTickAsset()
        {
            const string path = "Assets/_Project/Resources/AI/DMEnemyAiTickSettings31s.asset";
            var a = AssetDatabase.LoadAssetAtPath<DMEnemyAiTickSettings31s>(path);
            if (a != null) return a;
            Directory.CreateDirectory("Assets/_Project/Resources/AI");
            a = ScriptableObject.CreateInstance<DMEnemyAiTickSettings31s>();
            AssetDatabase.CreateAsset(a, path); AssetDatabase.SaveAssets(); DMEnemyAiTickSettings31s.ClearCache();
            return a;
        }

        private static bool IsVendor(string path) { foreach (var v in VendorRoots) if (path.Contains("/" + v) || path.Contains(v + "/")) return true; return false; }

        private static bool Keep(Renderer r)
        {
            if (r is SkinnedMeshRenderer || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) return true;
            Transform t = r.transform;
            while (t != null)
            {
                if (t.CompareTag("Player") || t.CompareTag("Enemy")) return true;
                string ln = LayerMask.LayerToName(t.gameObject.layer);
                if (ln == "Player" || ln == "Enemy" || ln == "Character" || ln == "Characters") return true;
                foreach (var c in t.GetComponents<Component>()) { if (c == null) continue; string n = c.GetType().Name; foreach (var k in KeepTypeNames) if (n == k) return true; }
                t = t.parent;
            }
            return false;
        }

        private static Vector3 RefPoint()
        {
            var pl = GameObject.FindGameObjectWithTag("Player");
            if (pl != null) return pl.transform.position;
            var sv = SceneView.lastActiveSceneView;
            return sv != null ? sv.camera.transform.position : Vector3.zero;
        }

        public static string Run(float size, float dist, bool apply)
        {
            Vector3 rp = RefPoint();
            var log = new StringBuilder("kind\tscene/asset\tpath\told\n");
            int scanned = 0, cands = 0, assetN = 0, sceneN = 0, before = 0;
            var touchedAssets = new HashSet<GameObject>();
            for (int si = 0; si < SceneManager.sceneCount; si++)
            {
                Scene s = SceneManager.GetSceneAt(si); if (!s.isLoaded) continue;
                foreach (var root in s.GetRootGameObjects())
                foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                {
                    scanned++;
                    if (r.shadowCastingMode == ShadowCastingMode.Off || r.shadowCastingMode == ShadowCastingMode.ShadowsOnly) continue;
                    before++;
                    if (Keep(r)) continue;
                    float diag = r.bounds.size.magnitude;
                    bool small = diag < size;
                    bool far = dist > 0f && (r.bounds.ClosestPoint(rp) - rp).magnitude > dist;
                    if (!small && !far) continue;
                    cands++;
                    if (!apply) continue;
                    // prefab-level when the instance takes its value from a _Project prefab
                    Renderer src = PrefabUtility.IsPartOfPrefabInstance(r) ? PrefabUtility.GetCorrespondingObjectFromSource(r) : null;
                    string srcPath = src != null ? AssetDatabase.GetAssetPath(src) : null;
                    bool overridden = src != null && PrefabUtility.GetPropertyModifications(r) is PropertyModification[] mods && System.Array.Exists(mods, m => m.target == src && m.propertyPath == "m_CastShadows");
                    if (src != null && !overridden && srcPath.StartsWith("Assets/_Project/") && !IsVendor(srcPath) && small) // far-only props are scene-specific -> scene object
                    {
                        if (src.shadowCastingMode != ShadowCastingMode.Off)
                        {
                            log.Append("ASSET\t" + srcPath + "\t" + GoPath(src.transform) + "\t" + (int)src.shadowCastingMode + "\n");
                            Undo.RecordObject(src, "Shadow cull 31s"); src.shadowCastingMode = ShadowCastingMode.Off; EditorUtility.SetDirty(src);
                            touchedAssets.Add(src.transform.root.gameObject); assetN++;
                        }
                    }
                    else
                    {
                        log.Append("SCENE\t" + s.path + "\t" + GoPath(r.transform) + "\t" + (int)r.shadowCastingMode + "\n");
                        Undo.RecordObject(r, "Shadow cull 31s"); r.shadowCastingMode = ShadowCastingMode.Off; EditorUtility.SetDirty(r);
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s); sceneN++;
                    }
                }
            }
            foreach (var go in touchedAssets) PrefabUtility.SavePrefabAsset(go);
            string msg = (apply ? "Applied" : "Preview") + ": scanned " + scanned + " renderers, " + before + " casting, " + cands + " to turn off (size<" + size + " m" + (dist > 0f ? ", or >" + dist + " m away" : "") + ")";
            if (apply)
            {
                Directory.CreateDirectory(LogDir);
                string f = Path.Combine(LogDir, "shadowcull31s_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".tsv");
                File.WriteAllText(f, log.ToString());
                msg += "\nprefab assets: " + assetN + " renderers in " + touchedAssets.Count + " prefabs; scene objects: " + sceneN + (sceneN > 0 ? " (SAVE the scene to keep)" : "") + "\nlog: " + f;
            }
            Debug.Log("[31s] " + msg);
            return msg;
        }

        public static string RevertLast()
        {
            if (!Directory.Exists(LogDir)) return "No log.";
            var files = Directory.GetFiles(LogDir, "shadowcull31s_*.tsv"); if (files.Length == 0) return "No log.";
            System.Array.Sort(files); string f = files[files.Length - 1];
            int done = 0, miss = 0; var touched = new HashSet<GameObject>();
            foreach (var line in File.ReadAllLines(f))
            {
                var c = line.Split('\t'); if (c.Length < 4 || c[0] == "kind") continue;
                var mode = (ShadowCastingMode)int.Parse(c[3]);
                if (c[0] == "ASSET")
                {
                    var root = AssetDatabase.LoadAssetAtPath<GameObject>(c[1]); Transform t = root != null ? FindPath(root.transform, c[2]) : null;
                    var r = t != null ? t.GetComponent<Renderer>() : null; if (r == null) { miss++; continue; }
                    r.shadowCastingMode = mode; EditorUtility.SetDirty(r); touched.Add(root); done++;
                }
                else
                {
                    Scene s = SceneManager.GetSceneByPath(c[1]); if (!s.isLoaded) { miss++; continue; }
                    Transform t = null; foreach (var g in s.GetRootGameObjects()) { t = FindPath(g.transform, c[2]); if (t != null) break; }
                    var r = t != null ? t.GetComponent<Renderer>() : null; if (r == null) { miss++; continue; }
                    Undo.RecordObject(r, "Revert shadow cull 31s"); r.shadowCastingMode = mode; EditorUtility.SetDirty(r); UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s); done++;
                }
            }
            foreach (var go in touched) PrefabUtility.SavePrefabAsset(go);
            string msg = "Reverted " + done + " (missing " + miss + ") from " + f;
            Debug.Log("[31s] " + msg); return msg;
        }

        private static string GoPath(Transform t) { var sb = new StringBuilder(t.name); while (t.parent != null) { t = t.parent; sb.Insert(0, t.name + "/"); } return sb.ToString(); }
        private static Transform FindPath(Transform root, string path)
        {
            var parts = path.Split('/'); if (parts.Length == 0 || parts[0] != root.name) return null;
            Transform t = root; for (int i = 1; i < parts.Length && t != null; i++) { Transform n = null; foreach (Transform ch in t) if (ch.name == parts[i]) { n = ch; break; } t = n; }
            return t;
        }
    }
}
// 2026-10-31h: Studio Sky & Orbit Essentials (top) + Deep Reset to Recommended (per profile, Undo, TSV log, Restore).
// Only DM assets and scene INSTANCES are changed. Volumes that use a Gaia/BOTD original profile get a per-scene DM copy
// (Assets/_Project/Resources/World/SceneVolumes) assigned to the scene instance (scene must be saved by Anthony).
using System.Collections.Generic;
using System.IO;
using System.Text;
using Project.World.Sky;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Project.EditorTools.GenesisStudio
{
    public static class DMStudioSkyEssentials31h
    {
        private const string AdvKey = "dm.sky31h.advanced";
        private const string CopyDir = "Assets/_Project/Resources/World/SceneVolumes";

        /// <summary>Draws Essentials; returns true when Advanced (full inspector + Day/Night panels) should be drawn.</summary>
        public static bool Draw(DM_IoSkyProfile p)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("Io sky profile: " + p.name + (p == DM_IoSkyProfile.Live ? "  (ACTIVE for scene '" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "', " + DM_IoSkyProfile.LiveSource + ")" : "  (not the active scene's profile)"), MessageType.Info);
            EditorGUILayout.LabelField("Essentials", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            var look = (DM_IoSkyProfile.SkyLookPreset)EditorGUILayout.EnumPopup("World Look preset", p.skyLookPreset);
            bool skyOn = EditorGUILayout.Toggle("Use Sky & Orbit", p.skyEnabled);
            bool dbgOn = EditorGUILayout.Toggle("Debug Hour on", p.debugHourEnabled);
            bool rng31v = p.ClockRangeActive31v(30f); // 31v: Debug Hour slider limited to the clock range
            float dbg = EditorGUILayout.Slider("Debug Hour" + (rng31v ? " (" + p.clockMinHour31v.ToString("0.#") + "-" + p.clockMaxHour31v.ToString("0.#") + ")" : ""), Mathf.Max(0f, p.debugHourOverride), rng31v ? p.clockMinHour31v : 0f, rng31v ? p.clockMaxHour31v : 30f);
            float cMin31v = EditorGUILayout.Slider(new GUIContent("Clock Min Hour", "31v: realtime clock and advancing Debug Hour run only from Min to Max. 0-30 = full day (unchanged)."), p.clockMinHour31v, 0f, 30f);
            float cMax31v = EditorGUILayout.Slider(new GUIContent("Clock Max Hour", "31v: Mars 15."), p.clockMaxHour31v, 0f, 30f);
            var cMode31v = (DM_IoSkyProfile.ClockRangeMode31v)EditorGUILayout.EnumPopup(new GUIContent("Clock range mode", "Loop = hard wrap to Min, PingPong = back and forth, Clamp = stop at the ends."), p.clockRangeMode31v);
            bool dbgAdv = EditorGUILayout.Toggle(new GUIContent("Debug Hour advances (clock rate)", "Debug Hour is a start point; sun, Jupiter and moons keep moving at the Io clock rate."), p.debugHourAdvances);
            bool editRun = EditorGUILayout.Toggle(new GUIContent("Edit mode clock runs", "No Debug/Preview Hour: edit-mode sky runs at the Io clock rate."), p.editModeClockRuns);
            float prev = EditorGUILayout.Slider(new GUIContent("Edit Preview Hour (-1 = clock)"), p.editPreviewHour, -1f, 30f);
            float lux = EditorGUILayout.Slider(new GUIContent("Sun lux (0 = scene light)"), p.sunBaseLux31h, 0f, 150000f);
            float mult = EditorGUILayout.Slider("Sun intensity multiplier", p.sunIntensityMultiplier, 0f, 4f);
            float shs = EditorGUILayout.Slider("Sun shadow strength", p.sunShadowStrength31h, 0f, 1f);
            float sTemp = EditorGUILayout.Slider(new GUIContent("Sun temperature K (0 = light colour)"), p.sunTemperature31o, 0f, 20000f); // 31o
            float sVol = EditorGUILayout.Slider(new GUIContent("Sun volumetric dimmer (-1 = keep)"), p.sunVolumetricDimmer31o, -1f, 16f);
            int sRes = EditorGUILayout.IntPopup("Sun shadow resolution", p.sunShadowResolution31o, new[] { "512", "1024", "2048", "4096" }, new[] { 512, 1024, 2048, 4096 });
            bool sDisc = EditorGUILayout.Toggle("Sun disc", p.sunDisc31o);
            float sAng = EditorGUILayout.Slider(new GUIContent("  Sun disc size (deg)", "31p: Jupiter/Io ~0.1, Mars 0.35, Earth 0.53."), p.sunAngularDiameterDeg, 0.01f, 10f);
            float sFs = EditorGUILayout.Slider("  Sun halo size (deg)", p.sunFlareSize31p, 0f, 30f);
            float sFf = EditorGUILayout.Slider("  Sun halo falloff", p.sunFlareFalloff31p, 0f, 20f);
            float sFm = EditorGUILayout.Slider("  Sun halo strength", p.sunFlareMultiplier31p, 0f, 5f);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("  Sun disc preset");
                string[] dn = { "Io 0.1", "Mars 0.35", "Earth 0.53", "Large 2" };
                for (int di = 0; di < dn.Length; di++) if (GUILayout.Button(dn[di], EditorStyles.miniButton)) { Undo.RecordObject(p, "Sun disc preset"); p.ApplySunDiscPreset31p(di); EditorUtility.SetDirty(p); }
            }
            float comp = EditorGUILayout.Slider("Exposure compensation", p.exposureCompensation, -5f, 5f);
            bool autoEx = EditorGUILayout.Toggle(new GUIContent("Auto exposure", "31l: OFF (default) = Fixed exposure. ON = automatic using Exposure min/max/adaptation (Advanced)."), p.autoExposure31l);
            float fixEv = EditorGUILayout.Slider(new GUIContent("Fixed exposure (EV)", "Used when Auto exposure is off. Higher = darker. ~13-14 = sunlit outdoor look."), p.fixedExposureEV31l, -6f, 20f);
            float nDrop = EditorGUILayout.Slider(new GUIContent("  Night/eclipse brighten (EV)", "Fixed exposure drops by this much at full night / full eclipse."), p.fixedNightDropEV31l, 0f, 12f);
            bool tint = EditorGUILayout.Toggle("Sky tint", p.skyTintEnabled);
            Color zt = p.skyZenithTint, ht = p.skyHorizonTint;
            if (tint) { zt = EditorGUILayout.ColorField("  Zenith tint", p.skyZenithTint); ht = EditorGUILayout.ColorField("  Horizon tint", p.skyHorizonTint); }
            float fog = EditorGUILayout.Slider("Fog distance (MFP, m)", p.fogMeanFreePath, 0f, 20000f);
            bool stars = EditorGUILayout.Toggle("Stars / Milky Way", p.nightSkyEnabled);
            float sb = EditorGUILayout.Slider("  Star brightness", p.starBrightness, 0f, 20f);
            float mw = EditorGUILayout.Slider("  Milky Way intensity", p.milkyWayIntensity, 0f, 20f);
            bool jup = EditorGUILayout.Toggle("Show Jupiter", p.showJupiter);
            bool moons = EditorGUILayout.Toggle("Show moons", p.showMoons);
            bool eks = EditorGUILayout.Toggle("Eclipse keeps sun shadows", p.eclipseKeepsSunShadows);
            bool eRamp = EditorGUILayout.Toggle(new GUIContent("Eclipse sun ramp", "31q: Sun intensity multiplier (normal) ramps to the eclipse multiplier with coverage and back. Replaces Eclipse min sun intensity."), p.eclipseSunRamp31q);
            float emin = p.eclipseMinSunIntensity, eMul = p.eclipseSunMultiplier31q, eSh = p.eclipseShadowStrength31q, ePen = p.eclipsePenumbraScale31q, eCv = p.eclipseRampCurve31q, eAmb = p.eclipseAmbientMultiplier;
            if (eRamp)
            {
                eMul = EditorGUILayout.Slider("  Eclipse sun multiplier", p.eclipseSunMultiplier31q, 0f, 10f);
                eSh = EditorGUILayout.Slider("  Eclipse shadow strength", p.eclipseShadowStrength31q, 0f, 1f);
                eAmb = EditorGUILayout.Slider(new GUIContent("  Eclipse shadow darkness (ambient)", "Lower = darker shadows during eclipse."), p.eclipseAmbientMultiplier, 0f, 1f);
                ePen = EditorGUILayout.Slider(new GUIContent("  Eclipse penumbra scale", "Lower = sharper shadows at full eclipse."), p.eclipsePenumbraScale31q, 0.05f, 2f);
                eCv = EditorGUILayout.Slider(new GUIContent("  Eclipse ramp curve", "1 = smooth; >1 holds brightness longer; <1 dims earlier."), p.eclipseRampCurve31q, 0.2f, 5f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("  Eclipse ramp preset");
                    string[] en = { "Subtle", "Default", "Extreme" };
                    for (int ei = 0; ei < en.Length; ei++) if (GUILayout.Button(en[ei], EditorStyles.miniButton)) { Undo.RecordObject(p, "Eclipse ramp preset"); p.ApplyEclipseSunRampPreset31q(ei); EditorUtility.SetDirty(p); }
                }
            }
            else emin = EditorGUILayout.Slider("  Eclipse min sun intensity", p.eclipseMinSunIntensity, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(p, "Sky Essentials");
                p.skyEnabled = skyOn; p.debugHourEnabled = dbgOn; p.debugHourOverride = dbg; p.debugHourAdvances = dbgAdv; p.editModeClockRuns = editRun; p.clockMinHour31v = Mathf.Min(cMin31v, cMax31v); p.clockMaxHour31v = Mathf.Max(cMin31v, cMax31v); p.clockRangeMode31v = cMode31v; // 31v
                p.editPreviewHour = prev; p.sunBaseLux31h = lux; p.sunIntensityMultiplier = mult; p.sunShadowStrength31h = shs; p.sunTemperature31o = sTemp; p.sunVolumetricDimmer31o = sVol; p.sunShadowResolution31o = sRes; p.sunDisc31o = sDisc; p.sunAngularDiameterDeg = sAng; p.sunFlareSize31p = sFs; p.sunFlareFalloff31p = sFf; p.sunFlareMultiplier31p = sFm; p.exposureCompensation = comp; p.autoExposure31l = autoEx; p.fixedExposureEV31l = fixEv; p.fixedNightDropEV31l = nDrop;
                p.skyTintEnabled = tint; p.skyZenithTint = zt; p.skyHorizonTint = ht; p.fogMeanFreePath = fog; p.nightSkyEnabled = stars; p.starBrightness = sb; p.milkyWayIntensity = mw;
                p.showJupiter = jup; p.showMoons = moons; p.eclipseKeepsSunShadows = eks; p.eclipseMinSunIntensity = emin; p.eclipseSunRamp31q = eRamp; if (eRamp) { p.eclipseSunMultiplier31q = eMul; p.eclipseShadowStrength31q = eSh; p.eclipsePenumbraScale31q = ePen; p.eclipseRampCurve31q = eCv; p.eclipseAmbientMultiplier = eAmb; } // 31q
                if (look != p.skyLookPreset) p.ApplySkyLookPreset(look);
                EditorUtility.SetDirty(p); SceneView.RepaintAll();
            }
            bool cs31k = EditorGUILayout.Toggle(new GUIContent("Contact shadows (project)", "31k: project-wide master, default OFF. Synced to every sky profile; our code and Deep Reset never enable contact shadows while off."), p.contactShadowsProject31k);
            if (cs31k != p.contactShadowsProject31k)
                foreach (var g31k in AssetDatabase.FindAssets("t:DM_IoSkyProfile")) { var q = AssetDatabase.LoadAssetAtPath<DM_IoSkyProfile>(AssetDatabase.GUIDToAssetPath(g31k)); if (q == null) continue; Undo.RecordObject(q, "Contact shadows (project)"); q.contactShadowsProject31k = cs31k; q.sunContactShadows = cs31k; EditorUtility.SetDirty(q); }
            EditorGUILayout.BeginHorizontal(); // 31v
            EditorGUILayout.LabelField("Clock range", GUILayout.Width(100f));
            string[] crN = { "Full day", "Mars 5.5-15", "Daylight 6-24", "Midday 10-20" };
            for (int i = 0; i < crN.Length; i++) if (GUILayout.Button(crN[i])) { Undo.RecordObject(p, "Clock range preset"); p.ApplyClockRangePreset31v(i); EditorUtility.SetDirty(p); SceneView.RepaintAll(); }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Fixed exposure", GUILayout.Width(100f));
            string[] exN = { "Subtle (14)", "Default (13)", "Extreme (11.5)" };
            for (int i = 0; i < 3; i++) if (GUILayout.Button(exN[i])) { Undo.RecordObject(p, "Exposure preset"); p.ApplyFixedExposurePreset31l(i); EditorUtility.SetDirty(p); }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Deep Reset to Recommended", "Profile + (if this is the open scene's profile) BOTD scene instances. Undo-able; log Logs/deepreset31h_<profile>.tsv")))
                if (EditorUtility.DisplayDialog("Deep Reset", "Reset '" + p.name + "' to recommended values?\nScene instances are changed only if this is the open scene's profile.", "Reset", "Cancel"))
                    Debug.Log("[DM] " + DeepReset(p));
            if (GUILayout.Button("Restore (before Deep Reset)")) Debug.Log("[DM] " + Restore(p));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
            bool adv = EditorGUILayout.Foldout(EditorPrefs.GetBool(AdvKey, false), "Advanced (all settings)", true);
            EditorPrefs.SetBool(AdvKey, adv);
            return adv;
        }

        // ---------------------------------------------------------------- Deep Reset
        private static string LogPath(DM_IoSkyProfile p) => "Logs/deepreset31h_" + p.name + ".tsv";
        private static string SnapPath(DM_IoSkyProfile p, string tag) => "Logs/deepreset31h_" + p.name + "_" + tag + ".json";

        public static string DeepReset(DM_IoSkyProfile p)
        {
            Directory.CreateDirectory("Logs");
            bool mars = p.name.ToLowerInvariant().Contains("mars") || p.skyLookPreset == DM_IoSkyProfile.SkyLookPreset.Mars;
            var log = new StringBuilder();
            string profSnap = SnapPath(p, "profile");
            if (!File.Exists(profSnap)) File.WriteAllText(profSnap, EditorJsonUtility.ToJson(p));
            Undo.RecordObject(p, "Deep Reset sky profile");
            var before = new Dictionary<string, string>();
            foreach (var f in typeof(DM_IoSkyProfile).GetFields()) if (!f.IsStatic) before[f.Name] = Str(f.GetValue(p));
            p.ApplySkyLookPreset(mars ? DM_IoSkyProfile.SkyLookPreset.Mars : DM_IoSkyProfile.SkyLookPreset.JupiterIo);
            p.skyEnabled = true; p.botdVolumesOnTop = true; p.essentialsOverBotd31h = true; p.fallbackSingleSunDisc = true; p.fallbackDriveSun = true;
            p.sunBaseLux31h = mars ? 30000f : 18000f; p.sunIntensityMultiplier = 1f; p.sunMaxLux = 60000f; p.sunShadowStrength31h = 1f; p.sunTemperature31o = 0f; p.sunVolumetricDimmer31o = -1f; p.sunShadowResolution31o = 2048; p.sunDisc31o = true; // 31o
                p.ApplySunDiscPreset31p(mars ? 1 : 0); // 31p
            p.ApplyExposurePreset31j(0); p.exposureCompensation = 0f; p.autoExposure31l = false; p.fixedExposureEV31l = mars ? 13f : 14f; // 31l: Fixed (auto range kept for the toggle)
            p.fogMeanFreePath = 4000f; p.ssaoIntensity = 0.8f; p.ssaoRadius = 0.6f; p.contactShadowLength = 0.15f; p.sunContactShadows = p.contactShadowsProject31k;
            p.indirectDiffuseMultiplier = 1f; p.indirectReflectionMultiplier = 1f; p.gradeContrast = 0f; p.shadowFloorLift = 0.05f; p.bloomIntensity = 0.3f;
            p.eclipseKeepsSunShadows = true; p.eclipseMinSunIntensity = 0.15f; p.ApplyEclipseSunRampPreset31q(1); if (!mars) p.sunIntensityMultiplier = 2f; p.nightSkyEnabled = true; p.editModeClockRuns = true; p.ApplyClockRangePreset31v(mars ? 1 : 0); // 31q; 31v: Mars clock 5.5-15 Loop, others full day
            if (p.ApplyJupiterDefaults31q()) log.Append("# 31q: Jupiter defaults snapshot applied\n");
            if (p.ApplyMarsDefaults31v()) log.Append("# 31v: Mars defaults snapshot applied (incl. clock 5.5-15)\n"); // 31v // 31q: Anthony's Jupiter values are the Jupiter defaults
            EditorUtility.SetDirty(p);
            foreach (var f in typeof(DM_IoSkyProfile).GetFields())
            {
                if (f.IsStatic || !before.TryGetValue(f.Name, out string o)) continue;
                string n = Str(f.GetValue(p)); if (o != n) log.Append("PROFILE\t" + p.name + "\t" + f.Name + "\t" + o + "\t" + n + "\n");
            }
            string scenePart;
            if (p == DM_IoSkyProfile.Live) scenePart = ResetScene(p, mars, log);
            else scenePart = "scene part PENDING: open the scene that uses " + p.name + " and press Deep Reset again";
            File.AppendAllText(LogPath(p), "# " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") + " deep reset " + p.name + "\n" + log);
            AssetDatabase.SaveAssets();
            return "Deep Reset " + p.name + ": " + scenePart + ". Log " + LogPath(p);
        }

        private static string ResetScene(DM_IoSkyProfile p, bool mars, StringBuilder log)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            int nVol = 0, nCopy = 0, nLight = 0;
            Directory.CreateDirectory(CopyDir);
            foreach (Volume v in Object.FindObjectsByType<Volume>(FindObjectsInactive.Exclude))
            {
                if (v == null || v.gameObject.hideFlags != HideFlags.None || !v.isGlobal || !v.isActiveAndEnabled || !v.name.StartsWith("BOTD") || v.sharedProfile == null) continue;
                nVol++;
                string path = AssetDatabase.GetAssetPath(v.sharedProfile);
                if (!path.StartsWith("Assets/_Project/"))
                {
                    string dst = CopyDir + "/" + San(scene.name) + "_" + San(v.name) + ".asset";
                    if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(dst) == null) AssetDatabase.CopyAsset(path, dst);
                    var cp = AssetDatabase.LoadAssetAtPath<VolumeProfile>(dst);
                    Undo.RecordObject(v, "Deep Reset volume profile");
                    v.sharedProfile = cp; EditorUtility.SetDirty(v);
                    log.Append("ASSIGN\t" + scene.name + "\t" + GoPath(v.transform) + "\t" + path + "\t" + dst + "\n");
                    path = dst; nCopy++;
                }
                if (!Mathf.Approximately(v.weight, 1f)) { log.Append("WEIGHT\t" + scene.name + "\t" + GoPath(v.transform) + "\t" + v.weight + "\t1\n"); Undo.RecordObject(v, "Deep Reset weight"); v.weight = 1f; EditorUtility.SetDirty(v); }
                foreach (VolumeComponent c in v.sharedProfile.components)
                {
                    if (c == null) continue;
                    string snap = SnapPath(p, San(Path.GetFileNameWithoutExtension(path)) + "_" + c.GetType().Name);
                    bool touched = c is PhysicallyBasedSky || c is Fog || c is Exposure || c is ScreenSpaceAmbientOcclusion || c is ContactShadows;
                    if (!touched) continue;
                    if (!File.Exists(snap)) File.WriteAllText(snap, EditorJsonUtility.ToJson(c));
                    log.Append("COMP\t" + path + "\t" + c.GetType().Name + "\t" + snap + "\t" + Describe(c) + "\n");
                    Undo.RecordObject(c, "Deep Reset volume");
                    if (c is PhysicallyBasedSky s) { s.multiplier.Override(1f); s.spaceEmissionMultiplier.Override(1f); s.groundEmissionMultiplier.Override(0f); }
                    if (c is Fog f) { f.meanFreePath.Override(4000f); f.albedo.Override(mars ? new Color(0.86f, 0.78f, 0.68f) : new Color(0.84f, 0.8f, 0.75f)); f.tint.Override(mars ? new Color(1f, 0.88f, 0.74f) : new Color(1f, 0.95f, 0.88f)); }
                    if (c is Exposure e) { e.mode.Override(p.autoExposure31l ? ExposureMode.AutomaticHistogram : ExposureMode.Fixed); e.fixedExposure.Override(p.fixedExposureEV31l); e.limitMin.Override(11.5f); e.limitMax.Override(14f); e.adaptationSpeedDarkToLight.Override(0.6f); e.adaptationSpeedLightToDark.Override(0.6f); e.compensation.Override(0f); } // 31j Subtle
                    if (c is ScreenSpaceAmbientOcclusion a) { a.intensity.Override(0.8f); a.radius.Override(0.6f); }
                    if (c is ContactShadows k) { k.active = p.contactShadowsProject31k; k.enable.Override(p.contactShadowsProject31k); k.length.Override(0.15f); }
                    EditorUtility.SetDirty(c);
                }
                EditorUtility.SetDirty(v.sharedProfile);
            }
            // One shadowed, one visible sun: the driven sun = active light Sky & Orbit picks; other active directionals lose shadow + disc.
            Light sun = null;
            foreach (string nm in new[] { p.sunLightName, "BOTD Directional Light(Clone)" })
                foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude)) if (l.name == nm && l.type == LightType.Directional && l.enabled) { sun = l; break; }
            if (sun == null && RenderSettings.sun != null && RenderSettings.sun.isActiveAndEnabled) sun = RenderSettings.sun;
            foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (l == sun || l.type != LightType.Directional || !l.enabled || l.gameObject.hideFlags != HideFlags.None) continue;
                var hd = l.GetComponent<HDAdditionalLightData>();
                if (l.shadows != LightShadows.None) { log.Append("SHADOW\t" + scene.name + "\t" + GoPath(l.transform) + "\t" + l.shadows + "\tNone\n"); Undo.RecordObject(l, "Deep Reset light"); l.shadows = LightShadows.None; EditorUtility.SetDirty(l); nLight++; }
                if (hd != null && hd.interactsWithSky) { log.Append("DISC\t" + scene.name + "\t" + GoPath(l.transform) + "\tTrue\tFalse\n"); Undo.RecordObject(hd, "Deep Reset light"); hd.interactsWithSky = false; EditorUtility.SetDirty(hd); nLight++; }
            }
            if (sun != null)
            {
                log.Append("SUN\t" + scene.name + "\t" + GoPath(sun.transform) + "\tshadows=" + sun.shadows + "\t(kept on, 2048)\n");
                if (sun.shadows == LightShadows.None) { Undo.RecordObject(sun, "Deep Reset sun"); sun.shadows = LightShadows.Soft; EditorUtility.SetDirty(sun); }
            }
            foreach (var pr in Object.FindObjectsByType<HDProbe>(FindObjectsInactive.Include)) log.Append("AUDIT\tprobe\t" + GoPath(pr.transform) + "\tactive=" + pr.isActiveAndEnabled + "\tmode=" + pr.mode + "\n");
            if (nCopy > 0 || nLight > 0) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            return "scene '" + scene.name + "': " + nVol + " BOTD volumes (" + nCopy + " got DM copies), " + nLight + " light changes" + (nCopy > 0 || nLight > 0 ? " - SAVE SCENE" : "");
        }

        public static string Restore(DM_IoSkyProfile p)
        {
            string lp = LogPath(p);
            if (!File.Exists(lp)) return "nothing to restore for " + p.name;
            string profSnap = SnapPath(p, "profile");
            if (File.Exists(profSnap)) { Undo.RecordObject(p, "Restore sky profile"); EditorJsonUtility.FromJsonOverwrite(File.ReadAllText(profSnap), p); EditorUtility.SetDirty(p); }
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            int done = 0, pending = 0; var keep = new StringBuilder();
            foreach (string line in File.ReadAllLines(lp))
            {
                string[] c = line.Split('\t'); if (c.Length < 4) continue;
                if (c[0] == "COMP")
                {
                    var prof = AssetDatabase.LoadAssetAtPath<VolumeProfile>(c[1]);
                    if (prof != null && File.Exists(c[3])) foreach (VolumeComponent vc in prof.components) if (vc != null && vc.GetType().Name == c[2]) { Undo.RecordObject(vc, "Restore volume"); EditorJsonUtility.FromJsonOverwrite(File.ReadAllText(c[3]), vc); EditorUtility.SetDirty(vc); done++; }
                    continue;
                }
                if (c[0] != "ASSIGN" && c[0] != "WEIGHT" && c[0] != "SHADOW" && c[0] != "DISC") continue;
                if (c[1] != sceneName) { pending++; keep.Append(line + "\n"); continue; }
                Transform t = FindPath(c[2]); if (t == null) continue;
                if (c[0] == "ASSIGN") { var v = t.GetComponent<Volume>(); var o = AssetDatabase.LoadAssetAtPath<VolumeProfile>(c[3]); if (v != null && o != null) { Undo.RecordObject(v, "Restore"); v.sharedProfile = o; EditorUtility.SetDirty(v); done++; } }
                if (c[0] == "WEIGHT") { var v = t.GetComponent<Volume>(); if (v != null) { Undo.RecordObject(v, "Restore"); v.weight = float.Parse(c[3], System.Globalization.CultureInfo.InvariantCulture); done++; } }
                if (c[0] == "SHADOW") { var l = t.GetComponent<Light>(); if (l != null) { Undo.RecordObject(l, "Restore"); l.shadows = (LightShadows)System.Enum.Parse(typeof(LightShadows), c[3]); done++; } }
                if (c[0] == "DISC") { var h = t.GetComponent<HDAdditionalLightData>(); if (h != null) { Undo.RecordObject(h, "Restore"); h.interactsWithSky = true; done++; } }
            }
            AssetDatabase.SaveAssets();
            if (pending == 0) { File.Move(lp, lp + ".restored_" + System.DateTime.Now.ToString("HHmmss")); foreach (string f in Directory.GetFiles("Logs", "deepreset31h_" + p.name + "_*.json")) File.Move(f, f + ".restored_" + System.DateTime.Now.ToString("HHmmss")); }
            else File.WriteAllText(lp + ".pending", keep.ToString());
            return "Restored " + p.name + " (profile + " + done + " items)" + (pending > 0 ? "; " + pending + " scene items belong to another scene - open it and press Restore" : "") + (done > 0 ? " - save the scene if instances changed" : "");
        }

        private static string Describe(VolumeComponent c)
        {
            if (c is PhysicallyBasedSky s) return "mult=" + s.multiplier.value + " space=" + s.spaceEmissionMultiplier.value + " ground=" + s.groundEmissionMultiplier.value + " -> 1/1/0";
            if (c is Fog f) return "mfp=" + f.meanFreePath.value + " albedo=" + f.albedo.value + " tint=" + f.tint.value + " -> 4000, neutral-warm";
            if (c is Exposure e) return "mode=" + e.mode.value + " fixed=" + e.fixedExposure.value + " min=" + e.limitMin.value + " max=" + e.limitMax.value + " comp=" + e.compensation.value + " -> Fixed (31l) unless Auto exposure on";
            if (c is ScreenSpaceAmbientOcclusion a) return "int=" + a.intensity.value + " rad=" + a.radius.value + " -> 0.8/0.6";
            if (c is ContactShadows k) return "active=" + k.active + " len=" + k.length.value + " -> (project toggle)/0.15";
            return "";
        }
        private static string Str(object o) => o is Object uo ? (uo != null ? uo.name : "null") : o is System.Array arr ? "[" + arr.Length + "]" : (o != null ? o.ToString() : "null");
        private static string San(string s) { foreach (char ch in Path.GetInvalidFileNameChars()) s = s.Replace(ch, '_'); return s.Replace(' ', '_').Replace('(', '_').Replace(')', '_'); }
        private static string GoPath(Transform t) { string s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }
        private static Transform FindPath(string path)
        {
            string[] parts = path.Split('/');
            foreach (GameObject r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (r.name != parts[0]) continue;
                Transform t = r.transform;
                for (int i = 1; i < parts.Length && t != null; i++) { Transform n = null; foreach (Transform ch in t) if (ch.name == parts[i]) { n = ch; break; } t = n; }
                if (t != null) return t;
            }
            return null;
        }
    }
}
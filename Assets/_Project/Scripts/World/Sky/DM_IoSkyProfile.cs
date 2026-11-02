using System;
using UnityEngine;

namespace Project.World.Sky
{
    [Serializable]
    public sealed class DMIoMoonSettings
    {
        public string name = "Moon";
        public bool enabled = true;
        [Tooltip("Orbital period as a multiple of Io's (Io = 1 = one 30 h Io day). Europa 2, Ganymede 4, Callisto 9.43.")]
        [Min(0.1f)] public float periodRatio = 2f;
        [Tooltip("Orbit radius around Jupiter in km (drives direction + realistic apparent size from Io).")]
        [Min(1000f)] public float orbitRadiusKm = 671100f;
        [Min(10f)] public float diameterKm = 3121.6f;
        [Tooltip("1 = realistic apparent size. Raise for a stylized, easier-to-see moon.")]
        [Range(0.1f, 10f)] public float sizeMultiplier = 1f;
        [Tooltip("Orbital angle at Io day 1, 00:00.")]
        [Range(0f, 360f)] public float startPhaseDeg = 40f;
        [Header("Look (procedural)")]
        public Color colorA = new Color(0.92f, 0.88f, 0.8f, 1f);
        public Color colorB = new Color(0.55f, 0.38f, 0.25f, 1f);
        [Range(0.5f, 20f)] public float noiseScale = 3f;
        [Range(0f, 1f)] public float lineae = 0.6f;
        [Range(0f, 1f)] public float craterSpots;
        [Range(0f, 5f)] public float brightness = 1f;
    }

    /// <summary>
    /// 2026-10-24ai: Io sky + orbit (Slice 1). // 2026-10-26o nudge Sun, eclipse, Jupiter and the Galilean moons all follow the 30 h Io clock.
    /// Studio: World > Sky &amp; Orbit.
    /// </summary>
    [CreateAssetMenu(fileName = "DM_IoSkyProfile", menuName = "Dark Matter/World/Io Sky Profile")]
    public sealed class DM_IoSkyProfile : ScriptableObject
    {
        public const string ResourcesPath = "World/DM_IoSkyProfile";
        public const string AssetPath = "Assets/_Project/Resources/World/DM_IoSkyProfile.asset";
        public const float RealisticEclipseHours = 1.6f;

        private static DM_IoSkyProfile live;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLive() { live = null; liveScene31g = null; sceneMap31g = null; }
        private static string liveScene31g; private static DMSceneSkyProfileMap sceneMap31g;
        public const string SceneMapResourcesPath = "World/DM_SceneSkyProfiles";
        public static string LiveSource { get; private set; } = "";
        public static void InvalidateLive31g() { live = null; liveScene31g = null; sceneMap31g = null; }
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void Boot31g()
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += (s, m) => InvalidateLive31g();
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += (a, b) => InvalidateLive31g();
            UnityEditor.EditorApplication.projectChanged += InvalidateLive31g;
        }
#endif

        public static DM_IoSkyProfile Live
        {
            get
            {
                // 31g: per-scene profile (DM_SceneSkyProfiles map, keyed by active scene name); re-resolves when the active scene changes.
                string sn = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (live == null || sn != liveScene31g)
                {
                    liveScene31g = sn;
                    if (sceneMap31g == null) sceneMap31g = Resources.Load<DMSceneSkyProfileMap>(SceneMapResourcesPath);
                    DM_IoSkyProfile m = sceneMap31g != null ? sceneMap31g.Find(sn) : null;
                    live = m != null ? m : Resources.Load<DM_IoSkyProfile>(ResourcesPath);
                    LiveSource = m != null ? "scene map (" + sn + ")" : "default (Resources)";
                }
                return live;
            }
        }

        public enum SkyLookPreset { Custom = 0, Subtle = 1, Normal = 2, Extreme = 3, Mars = 4, [InspectorName("Jupiter (Io)")] JupiterIo = 5 }

        [Header("Sky Look Preset (2026-10-26m)")]
        [Tooltip("Custom = your saved look (current settings snapshotted first). Subtle / Normal / Extreme = authored full look snapshots (sky, MW, aurora, stars, night light/shadow, eclipse brightness, dither). Orbit/time never touched. Edit any look field on a preset -> flips to Custom and keeps the edit.")]
        public SkyLookPreset skyLookPreset = SkyLookPreset.Custom;

        [Header("Controller")]
        public bool skyEnabled = true;
        [Tooltip("30n: with Use Sky & Orbit OFF (Gaia lighting stack), hide the sun disc + lens flare of any extra directional light so only the Gaia sun shows. Lights themselves untouched.")]
        public bool fallbackSingleSunDisc = true;
        [Tooltip("31e: Use Sky & Orbit off -> still rotate the BOTD sun from the clock / Debug Hour / Preview Hour (orbit path only; no Jupiter, eclipse or lux changes).")]
        public bool fallbackDriveSun = true;
        [Tooltip("31f: BOTD Post Processing / BOTD Environment volumes sit ON TOP of Sky & Orbit. Our realtime-indirect + eclipse ambient volumes go below them; only eclipse/night ambient scaling and the sky tint ride above (small drive volume). BOTD assets are never written.")]
        public bool botdVolumesOnTop = true;
        [Tooltip("31h: sun lux used by Sky & Orbit (0 = use the scene light's own intensity). Makes lux independent of which Gaia light is active.")]
        [Range(0f, 150000f)] public float sunBaseLux31h = 0f;
        [Tooltip("31h: BOTD sun shadow strength (HDRP shadow dimmer).")]
        [Range(0f, 1f)] public float sunShadowStrength31h = 1f;
        [Tooltip("31o: BOTD sun colour temperature in K (0 = keep the light's own colour).")]
        [Range(0f, 20000f)] public float sunTemperature31o = 0f;
        [Tooltip("31o: BOTD sun volumetric dimmer (-1 = keep the light's own value).")]
        [Range(-1f, 16f)] public float sunVolumetricDimmer31o = -1f;
        [Tooltip("31o: BOTD sun shadow resolution (always shadowed).")]
        public int sunShadowResolution31o = 2048;
        [Tooltip("31o: draw the sun disc in the sky (off = disc hidden, light and atmosphere unchanged).")]
        public bool sunDisc31o = true;
        [Tooltip("31h: with BOTD volumes on top, exposure compensation + fog distance from this profile still apply above BOTD (only those two parameters).")]
        public bool essentialsOverBotd31h = true;
        [Tooltip("31h: Debug Hour is a start point and the sky keeps moving at the Io clock rate (DM_IoClockProfile real seconds per hour). Off = frozen at the Debug Hour.")]
        public bool debugHourAdvances = false;
        // 31v: per-profile clock hour range. Realtime clock + advancing Debug Hour run only inside [min, max] (Io hours, 0-30 day).
        // Full range (0..hours per day) = unchanged. Loop = hard wrap back to min, PingPong = runs back and forth, Clamp = holds at the ends.
        public enum ClockRangeMode31v { Loop, PingPong, Clamp }
        [Tooltip("31v: clock only runs from this hour (0 = day start). Mars 5.5.")]
        [Range(0f, 30f)] public float clockMinHour31v = 0f;
        [Tooltip("31v: clock only runs up to this hour (>= hours per day = no limit). Mars 15.")]
        [Range(0f, 30f)] public float clockMaxHour31v = 30f;
        [Tooltip("31v: Loop = wrap back to Min (hard cut), PingPong = run back down to Min, Clamp = stop at the ends.")]
        public ClockRangeMode31v clockRangeMode31v = ClockRangeMode31v.Loop;
        public bool ClockRangeActive31v(float hpd) => clockMaxHour31v - clockMinHour31v > 0.01f && (clockMinHour31v > 0.001f || clockMaxHour31v < hpd - 0.001f);
        public double ApplyClockRange31v(double total, float hpd)
        {
            if (hpd < 1f || !ClockRangeActive31v(hpd)) return total;
            double mn = System.Math.Max(0.0, clockMinHour31v), mx = System.Math.Min(clockMaxHour31v, hpd), span = mx - mn;
            if (span <= 0.01) return total;
            double day = System.Math.Floor(total / hpd), h;
            if (clockRangeMode31v == ClockRangeMode31v.Clamp) { h = total - day * hpd; h = h < mn ? mn : (h > mx ? mx : h); }
            else if (clockRangeMode31v == ClockRangeMode31v.PingPong) { double m = (total - mn) % (2.0 * span); if (m < 0) m += 2.0 * span; h = mn + (m <= span ? m : 2.0 * span - m); }
            else { double m = (total - mn) % span; if (m < 0) m += span; h = mn + m; }
            return day * hpd + h;
        }
        /// <summary>31v presets: 0 Full day (0-30), 1 Mars (5.5-15), 2 Daylight (6-24), 3 Midday (10-20).</summary>
        public void ApplyClockRangePreset31v(int i)
        {
            float[] mn = { 0f, 5.5f, 6f, 10f }; float[] mx = { 30f, 15f, 24f, 20f }; i = Mathf.Clamp(i, 0, 3);
            clockMinHour31v = mn[i]; clockMaxHour31v = mx[i]; clockRangeMode31v = ClockRangeMode31v.Loop;
            if (debugHourOverride >= 0f && ClockRangeActive31v(30f)) debugHourOverride = Mathf.Clamp(debugHourOverride, clockMinHour31v, clockMaxHour31v);
        }
        [Tooltip("31h: in edit mode (no Debug/Preview Hour) the sky runs on the Io clock rate instead of freezing.")]
        public bool editModeClockRuns = true;
        [Tooltip("31f: during the Jupiter eclipse the BOTD sun keeps casting shadows (dimmed) instead of handing the shadow to Jupiter-shine.")]
        public bool eclipseKeepsSunShadows = true;
        [Tooltip("31f: minimum sun intensity during the eclipse, as a fraction of its normal daylight lux (0 = black, 1 = no dimming). Only with Eclipse keeps sun shadows.")]
        [Range(0f, 1f)] public float eclipseMinSunIntensity = 0.15f;
        /// <summary>31j: auto-exposure presets. 0 Subtle (11.5-14 EV, adapt 0.6), 1 Default (10.5-14, 1), 2 Extreme (8-14, 2/1.5). Outdoors sits at the 14 EV top.</summary>
        public void ApplyExposurePreset31j(int preset)
        {
            exposureOverride = true; exposureHistogram = true; exposureLimitMax = 14f; // 31l: range only used when autoExposure31l is on
            if (preset <= 0) { exposureLimitMin = 11.5f; exposureAdaptDarkToLight = 0.6f; exposureAdaptLightToDark = 0.6f; }
            else if (preset == 1) { exposureLimitMin = 10.5f; exposureAdaptDarkToLight = 1f; exposureAdaptLightToDark = 1f; }
            else { exposureLimitMin = 8f; exposureAdaptDarkToLight = 2f; exposureAdaptLightToDark = 1.5f; }
        }

        [Tooltip("31q: eclipse ramps the sun intensity multiplier smoothly from Sun intensity multiplier (normal) to Eclipse sun multiplier at full coverage, and back as the sun peeks out. Replaces Eclipse min sun intensity while on.")]
        public bool eclipseSunRamp31q = true;
        [Range(0f, 10f)] public float eclipseSunMultiplier31q = 0.05f;
        [Tooltip("31q: HDRP shadow dimmer at full eclipse (normal = Sun shadow strength).")]
        [Range(0f, 1f)] public float eclipseShadowStrength31q = 1f;
        [Tooltip("31q: penumbra scale at full eclipse (1 = unchanged, lower = sharper shadows).")]
        [Range(0.05f, 2f)] public float eclipsePenumbraScale31q = 0.6f;
        [Tooltip("31q: ramp curve exponent on the eclipse coverage (1 = smoothstep, >1 = stays bright longer then drops, <1 = drops early).")]
        [Range(0.2f, 5f)] public float eclipseRampCurve31q = 1f;
        public void ApplyEclipseSunRampPreset31q(int i) { float[] m = { 0.3f, 0.05f, 0.01f }; float[] s = { 0.9f, 1f, 1f }; float[] pen = { 0.85f, 0.6f, 0.3f }; float[] cv = { 1.5f, 1f, 0.7f }; float[] amb = { 0.5f, 0.3f, 0.12f }; i = Mathf.Clamp(i, 0, 2); eclipseSunRamp31q = true; eclipseSunMultiplier31q = m[i]; eclipseShadowStrength31q = s[i]; eclipsePenumbraScale31q = pen[i]; eclipseRampCurve31q = cv[i]; eclipseAmbientMultiplier = amb[i];
            if (i == 1 && IsJupiterProfile31q) { var ta = Resources.Load<TextAsset>("World/DM_IoSkyProfile_Jupiter_Defaults31q"); if (ta != null) { var d = CreateInstance<DM_IoSkyProfile>(); JsonUtility.FromJsonOverwrite(ta.text, d); eclipseSunMultiplier31q = d.eclipseSunMultiplier31q; eclipseShadowStrength31q = d.eclipseShadowStrength31q; eclipsePenumbraScale31q = d.eclipsePenumbraScale31q; eclipseRampCurve31q = d.eclipseRampCurve31q; eclipseAmbientMultiplier = d.eclipseAmbientMultiplier; sunIntensityMultiplier = d.sunIntensityMultiplier; if (Application.isPlaying) Destroy(d); else DestroyImmediate(d); } } if (i == 1 && IsMarsProfile31v) { var ta31v = Resources.Load<TextAsset>("World/DM_IoSkyProfile_Mars_Defaults31v"); if (ta31v != null) { var d = CreateInstance<DM_IoSkyProfile>(); JsonUtility.FromJsonOverwrite(ta31v.text, d); eclipseSunMultiplier31q = d.eclipseSunMultiplier31q; eclipseShadowStrength31q = d.eclipseShadowStrength31q; eclipsePenumbraScale31q = d.eclipsePenumbraScale31q; eclipseRampCurve31q = d.eclipseRampCurve31q; eclipseAmbientMultiplier = d.eclipseAmbientMultiplier; sunIntensityMultiplier = d.sunIntensityMultiplier; if (Application.isPlaying) Destroy(d); else DestroyImmediate(d); } } // 31v: Mars Default = Anthony's values
            // 31q: Jupiter Default = Anthony's values
        }
        // 31q: Anthony's Jupiter values (2026-10-31 17:55) are the Jupiter defaults: Resources/World/DM_IoSkyProfile_Jupiter_Defaults31q.json (JsonUtility).
        public bool IsJupiterProfile31q => name.ToLowerInvariant().Contains("jupiter");
        // 31v: Anthony's Mars values (2026-10-31 21:00) + clock 5.5-15 are the Mars defaults: Resources/World/DM_IoSkyProfile_Mars_Defaults31v.json (object links stripped).
        public bool IsMarsProfile31v => name.ToLowerInvariant().Contains("mars");
        public bool ApplyMarsDefaults31v()
        {
            if (!IsMarsProfile31v) return false;
            var ta = Resources.Load<TextAsset>("World/DM_IoSkyProfile_Mars_Defaults31v"); if (ta == null) return false;
            string n = name; JsonUtility.FromJsonOverwrite(ta.text, this); name = n; return true;
        }
        public bool ApplyJupiterDefaults31q()
        {
            if (!IsJupiterProfile31q) return false;
            var ta = Resources.Load<TextAsset>("World/DM_IoSkyProfile_Jupiter_Defaults31q"); if (ta == null) return false;
            string n = name; JsonUtility.FromJsonOverwrite(ta.text, this); name = n; return true;
        }
        public void ApplyEclipseMinSunPreset(int preset) { float[] v = { 0.03f, 0.08f, 0.15f, 0.3f, 0.6f }; eclipseMinSunIntensity = v[Mathf.Clamp(preset, 0, v.Length - 1)]; }
        [Tooltip("Spawn DMIoSkyController automatically in Play (no scene edit needed).")]
        public bool autoSpawnController = true;
        [Tooltip("Only run in scenes that contain the Jupiter object (keeps menus untouched).")]
        public bool onlyInScenesWithJupiter = true;
        [Tooltip("Shader for Jupiter + moons (DM/IoSkyBody). Lit by the sun in space, so phases show even at night.")]
        public Shader skyBodyShader;
        [Tooltip("-1 = follow the Io clock. 0-30 = force this hour (preview / testing in Play).")]
        [Range(-1f, 30f)] public float debugHourOverride = -1f;
        [Tooltip("30l: Debug Hour freezes the sun/Jupiter/moons ONLY while this is ticked; the hour value is kept when unticked.")]
        public bool debugHourEnabled = false;
        /// <summary>30l: true only when Debug Hour is ticked and has a valid hour.</summary>
        public bool DebugHourActive => debugHourEnabled && debugHourOverride >= 0f;
        [Tooltip("2026-10-24ao: Io hour the clock starts at when Play starts (and on New Game), then runs normally. 15 = noon (eclipse centre). Loading a save restores the saved time instead. Debug Hour Override >= 0 still wins.")]
        [Range(0f, 30f)] public float startHour = 6f;

        [Header("Sun")]
        [Tooltip("Scene directional light the controller drives as the sun.")]
        public string sunLightName = "BOTD Directional Light(Clone)"; // 30i: BOTD clone is the single sun (Gaia may spawn it at runtime)
        [Tooltip("Turn off every other enabled directional light (BOTD sun etc.) at runtime.")]
        public bool disableOtherDirectionalLights = true;
        [Tooltip("Multiplier on the sun light's scene intensity.")]
        [Range(0f, 5f)] public float sunIntensityMultiplier = 1f;
        [Tooltip("Sun fades out over +/- this many degrees around the horizon.")]
        [Range(0.5f, 20f)] public float horizonFadeDegrees = 4f;
        [Tooltip("2026-10-24al: low-sun dimming band above the horizon (degrees). 0 = off.")]
        [Range(0f, 45f)] public float sunHorizonDimDegrees = 20f;
        [Tooltip("Sun intensity factor right at the horizon (ramps to 1 at sunHorizonDimDegrees).")]
        [Range(0f, 1f)] public float sunHorizonMinFactor = 0.5f;
        [Tooltip("Desaturate the sun colour near the horizon (0 = keep scene colour, 1 = white).")]
        [Range(0f, 1f)] public float sunHorizonDesaturate = 0.6f;
        [Tooltip("Hard cap on sun lux after all multipliers (0 = no cap). Scene Day light is ~9400 lux.")]
        [Range(0f, 130000f)] public float sunMaxLux = 8000f;
        [Tooltip("24al: shadowless, specular-free fill light from the anti-sun side (bounce from sun-lit ground SSGI can't see). Scales with sun elevation + eclipse.")]
        public bool shadeFillEnabled = true;
        [Range(0f, 3000f)] public float shadeFillLux = 700f;
        [Tooltip("2026-10-30i: Shade Fill strength multiplier on shadeFillLux (0 = off).")]
        [Range(0f, 4f)] public float shadeFillStrength = 0.5f;
        [Tooltip("2026-10-30j: edit mode mirrors runtime (sky/orbit, sun, shade fill, Jupiter/moons, runtime volume) via hidden DontSave temps; restored on disable/save.")]
        public bool editModeRuntimePreview = true;
        [Tooltip("Degrees the fill travels downward (0 = horizontal, negative = from below like ground bounce).")]
        [Range(-45f, 45f)] public float shadeFillElevationDeg = -10f;
        public Color shadeFillColor = new Color(1f, 0.8f, 0.6f, 1f);
        [Tooltip("Tilt of the sun path around the Jupiter direction (degrees).")]
        [Range(-60f, 60f)] public float orbitPoleTiltDeg;
        [Tooltip("Flip the direction the sky turns.")]
        public bool reverseSkyMotion;

        [Header("Eclipse (centered on local noon, 15:00)")]
        public bool eclipseEnabled = true;
        [Tooltip("Gameplay darkness length in Io hours. Realistic Io eclipse is ~1.6 h (use the Realistic preset). Default 10 h is stylized.")]
        [Range(0.5f, 20f)] public float eclipseDurationHours = 10f;
        [Tooltip("Fade in / out length (hours), centered on each eclipse edge.")]
        [Range(0.05f, 5f)] public float eclipseFadeHours = 1.5f;
        [Tooltip("Sun intensity left at full eclipse (0 = black).")]
        [Range(0f, 1f)] public float eclipseSunRemaining;
        [Tooltip("Indirect diffuse + reflection multiplier at full eclipse (dim ambient).")]
        [Range(0f, 1f)] public float eclipseAmbientMultiplier = 0.25f;
        [Tooltip("Keep the sun parked behind Jupiter for the whole eclipse (the sun path speeds up outside it). Off = geometric path.")]
        public bool sunHidesBehindJupiter = true;
        [Min(0f)] public int ambientVolumePriority = 100;

        [Header("Jupiter")]
        public string jupiterObjectName = "Jupiter";
        [Tooltip("2026-10-30c: off = hide Jupiter (and its small moons), Jupiter-shine, eclipse, ring and Jupiter night shadow. Sun keeps orbiting.")]
        public bool showJupiter = true;
        [Tooltip("2026-10-30c: off = hide Europa, Ganymede, Callisto and the small moons.")]
        public bool showMoons = true;
        // 2026-10-30d: world-look extras (Mars / Jupiter (Io) Sky Look Presets).
        [Tooltip("2026-10-30d: sun disc size in degrees (Earth 0.53, Mars 0.35, Io 0.10). -1 = leave the scene light's value.")]
        [Range(0.01f, 10f)] public float sunAngularDiameterDeg = 0.35f; // 31p: real size always (old -1 "keep light value" removed; <=0 in old assets -> 0.35)
        [Tooltip("31p: sun halo/flare size in degrees (HDRP flareSize). Small = tight glow around the disc.")]
        [Range(0f, 30f)] public float sunFlareSize31p = 2f;
        [Tooltip("31p: sun halo falloff (HDRP flareFalloff). Higher = tighter halo; 0 = flat huge halo.")]
        [Range(0f, 20f)] public float sunFlareFalloff31p = 4f;
        [Tooltip("31p: sun halo strength (HDRP flareMultiplier).")]
        [Range(0f, 5f)] public float sunFlareMultiplier31p = 1f;
        public void ApplySunDiscPreset31p(int i) { float[] a = { 0.1f, 0.35f, 0.53f, 2f }; float[] fs = { 1f, 2f, 3f, 8f }; float[] ff = { 6f, 4f, 4f, 2f }; float[] fm = { 0.5f, 1f, 1f, 1f }; i = Mathf.Clamp(i, 0, 3); sunAngularDiameterDeg = a[i]; sunFlareSize31p = fs[i]; sunFlareFalloff31p = ff[i]; sunFlareMultiplier31p = fm[i]; }
        [Tooltip("2026-10-30d: tint the Physically Based Sky (zenith/horizon), e.g. Mars butterscotch. Off = scene sky untouched.")]
        public bool skyTintEnabled = false;
        public Color skyZenithTint = Color.white;
        public Color skyHorizonTint = Color.white;
        [Tooltip("2026-10-30d: shifts the PBS horizon/zenith blend (-1..1).")]
        [Range(-1f, 1f)] public float skyHorizonZenithShift = 0f;
        [InspectorName("Sky Rotation")]
        [Tooltip("24aw: turns the whole sky (Jupiter, sun path/eclipse, moons, chunks, ring light, stars, aurora) around the vertical. 200 = original.")]
        [Range(0f, 360f)] public float jupiterAzimuthDeg = 200f;
        [InspectorName("Jupiter Height")]
        [Tooltip("24aw: Jupiter elevation above the horizon (deg). Sun path, eclipse, moons, chunks and ring light follow.")]
        [Range(-10f, 90f)] public float jupiterElevationDeg = 32f;
        [Tooltip("Apparent diameter from Io. Realistic ~19.5 deg.")]
        [Range(1f, 60f)] public float jupiterAngularDiameterDeg = 19.5f;
        [Tooltip("Fixed render distance of sky bodies (clamped to 75% of camera far clip).")]
        [Range(200f, 20000f)] public float skyDistance = 2000f;
        [Tooltip("Jupiter's spin period in Io hours (real 9.93 h vs Io 42.5 h -> 7.0 on the 30 h day).")]
        [Range(0.5f, 100f)] public float jupiterSpinHours = 7f;
        [Tooltip("Mesh orientation fix (euler) so the cloud bands line up with the moons' orbit plane.")]
        public Vector3 jupiterTiltEuler;
        [Range(0f, 5f)] public float jupiterBrightness = 1f;
        [Range(0f, 1f)] public float jupiterLimbDarkening = 0.35f;
        [ColorUsage(false, true)] public Color jupiterRimColor = new Color(1f, 0.72f, 0.45f, 1f);
        [Tooltip("Backlit atmosphere rim during eclipse (nits-ish).")]
        [Range(0f, 5000f)] public float jupiterEclipseRimLux = 400f;
        [Tooltip("2026-10-26o: rim glow falloff power (higher = tighter band). Analytic limb; Studio softens jagged rim.")]
        [Range(0.5f, 16f)] public float jupiterRimSoftness = 4f;
        [Tooltip("2026-10-26o: soft alpha at the silhouette (ndv). Hides sphere faceting; 0 = hard edge.")]
        [Range(0f, 0.25f)] public float skyBodyEdgeSoft = 0.035f;
        [Tooltip("2026-10-26o: longitude segments for procedural UV sphere (Jupiter + moons). Higher = smoother silhouette. Unity default ~24.")]
        [Range(24, 96)] public int skyBodySphereSegments = 64;

        [Header("Jupiter-lit sky")]
        public Color jupiterShineColor = new Color(1f, 0.86f, 0.68f, 1f);
        [Tooltip("Jupiter-shine at full Jupiter (midnight). Realistic ~25 lux.")]
        [Range(0f, 500f)] public float jupiterShineLux = 25f;
        [Tooltip("Extra stylized Jupiter-shine during eclipse so the sky stays readable.")]
        [Range(0f, 500f)] public float eclipseJupiterShineLux = 15f;

        [Header("Other moons (existing small ones around Jupiter)")]
        [Tooltip("Orbit periods in Io hours for the existing anchors under Jupiter (Amalthea ~8.4, Thebe ~11.4).")]
        public float[] otherMoonPeriodsHours = { 8.4f, 11.4f };
        public bool otherMoonsUseSkyMaterial = true;
        public Color otherMoonColor = new Color(0.55f, 0.42f, 0.36f, 1f);

        [Header("Galilean moons")]
        public DMIoMoonSettings europa = new DMIoMoonSettings
        {
            name = "Europa", periodRatio = 2f, orbitRadiusKm = 671100f, diameterKm = 3121.6f, startPhaseDeg = 40f,
            colorA = new Color(0.92f, 0.88f, 0.8f, 1f), colorB = new Color(0.55f, 0.38f, 0.25f, 1f), noiseScale = 3f, lineae = 0.6f, craterSpots = 0f
        };
        public DMIoMoonSettings ganymede = new DMIoMoonSettings
        {
            name = "Ganymede", periodRatio = 4f, orbitRadiusKm = 1070400f, diameterKm = 5268f, startPhaseDeg = 160f,
            colorA = new Color(0.58f, 0.55f, 0.5f, 1f), colorB = new Color(0.32f, 0.29f, 0.26f, 1f), noiseScale = 2.5f, lineae = 0.25f, craterSpots = 0.3f
        };
        public DMIoMoonSettings callisto = new DMIoMoonSettings
        {
            name = "Callisto", periodRatio = 9.43f, orbitRadiusKm = 1882700f, diameterKm = 4820.6f, startPhaseDeg = 280f,
            colorA = new Color(0.26f, 0.24f, 0.22f, 1f), colorB = new Color(0.4f, 0.37f, 0.34f, 1f), noiseScale = 4f, lineae = 0f, craterSpots = 0.8f
        };

        public enum IndirectQuality { Low = 0, Medium = 1, High = 2, Custom = 3 }

        [Header("Realtime Indirect Light (2026-10-24aj, SSGI, no baking)")]
        [Tooltip("Spawn a global Volume with DM_IoPostProcessing (BOTD PP copy tuned for Io) in Play (above Gaia/BOTD). Undone on exit.")]
        public bool realtimeIndirectEnabled = true;
        [Tooltip("Resources/World/DM_IoPostProcessing (VolumeProfile). Cloned at runtime, the asset is never modified.")]
        public UnityEngine.Rendering.VolumeProfile indirectVolumeProfile;
        [Tooltip("Above BOTD/Gaia volumes (0). Sky eclipse ambient volume is 100 but is muted while this drives ambient.")]
        public float indirectVolumePriority = 90f;
        [Tooltip("Screen Space Global Illumination (ray marched, DX11 ok).")]
        public bool ssgiEnabled = true;
        [Tooltip("Custom = use the steps/resolution/denoise below (BOTD uses Custom).")]
        public IndirectQuality ssgiQuality = IndirectQuality.Custom;
        [Range(8, 128)] public int ssgiMaxRaySteps = 64;
        public bool ssgiFullResolution = true;
        public bool ssgiDenoise = true;
        [Tooltip("Off = second (full-res) denoiser pass for cleaner bounce.")]
        public bool ssgiHalfResDenoiser = true;
        [Range(0.05f, 1f)] public float ssgiDenoiserRadius = 0.6f;
        [Tooltip("Max SSGI ray thickness. Higher = fewer leaks through thin geometry, more bounce on walls.")]
        [Range(0.01f, 0.5f)] public float ssgiDepthThickness = 0.1f;

        [Header("Io Shade Look (2026-10-24ak)")]
        [Tooltip("Lift the shadow floor so back sides read as warm shade, not black (Shadows/Midtones/Highlights, shadows w).")]
        [Range(0f, 0.3f)] public float shadowFloorLift = 0.08f;
        [Tooltip("Tint of the lifted shadows (Io sulfur-orange ground bounce).")]
        public Color shadowFloorTint = new Color(1f, 0.86f, 0.62f, 1f);
        [Tooltip("Physically Based Sky ground tint: feeds warm ground bounce into the ambient probe for back sides.")]
        public bool overrideGroundTint = true;
        public Color groundBounceTint = new Color(0.45f, 0.36f, 0.24f, 1f);
        [Range(0f, 4f)] public float ssaoIntensity = 0.9f;
        [Range(0f, 1f)] public float ssaoDirectLightingStrength = 0.15f;
        [Tooltip("If a camera's Volume Layer Mask excludes this volume's layer (some turret cameras use 0 = no volumes), add it at runtime; restored on exit.")]
        public bool fixCameraVolumeMask = true;
        [Tooltip("Indirect diffuse multiplier (bounce + sky ambient). BOTD Environment uses 0.5. Eclipse dimming multiplies on top.")]
        [Range(0f, 4f)] public float indirectDiffuseMultiplier = 1.2f;
        [Range(0f, 4f)] public float indirectReflectionMultiplier = 1f;
        [Tooltip("Dynamic = ambient follows the live sky (sun path, eclipse, night).")]
        public bool ambientModeDynamic = true;
        [Tooltip("Override exposure. BOTD = physical camera (ISO200 1/200 f16) + comp 8 => fixed ~EV 6.6, which blows out sun-facing slopes. On = auto histogram.")]
        public bool exposureOverride = true;
        [Tooltip("On = Automatic Histogram, off = Automatic.")]
        public bool exposureHistogram = true;
        [Tooltip("31l: OFF (default) = Fixed exposure at fixedExposureEV31l everywhere we control it. ON = automatic (min/max/adaptation below).")]
        public bool autoExposure31l = false;
        [Tooltip("31l: Fixed exposure EV100 when Auto exposure is off. Higher = darker. ~13-14 matches the sunlit outdoor look.")]
        [Range(-6f, 20f)] public float fixedExposureEV31l = 14f;
        [Tooltip("31l: EV the fixed exposure drops (brightens) at full eclipse / full night so the night/eclipse fills stay visible. 0 = no change.")]
        [Range(0f, 12f)] public float fixedNightDropEV31l = 3f;
        public void ApplyFixedExposurePreset31l(int preset) { autoExposure31l = false; exposureOverride = true; fixedExposureEV31l = preset <= 0 ? 14f : preset == 1 ? 13f : 11.5f; }
        [Tooltip("EV100 floor (brightest the image gets). ~6.6 = BOTD look, keeps shaded views as before.")]
        [Range(-6f, 20f)] public float exposureLimitMin = 6.4f;
        [Tooltip("EV100 ceiling (how far it darkens when facing the sun).")]
        [Range(-6f, 20f)] public float exposureLimitMax = 9f;
        [Tooltip("Histogram percentages: ignore darkest X% / brightest (100-Y)% when metering.")]
        [Range(0f, 100f)] public float exposureHistogramLow = 40f;
        [Range(0f, 100f)] public float exposureHistogramHigh = 90f;
        [Range(0.1f, 10f)] public float exposureAdaptDarkToLight = 2f;
        [Range(0.1f, 10f)] public float exposureAdaptLightToDark = 1.5f;
        [Tooltip("Bloom intensity (BOTD 0.416, threshold 0).")]
        [Range(0f, 1f)] public float bloomIntensity = 0.3f;
        [Range(0f, 5f)] public float bloomThreshold = 0.5f;
        [Range(-5f, 5f)] public float exposureCompensation;

        [Header("AAA Polish (2026-10-24an, DM_IoPostProcessing runtime copy only)")]
        public bool polishEnabled = true;
        [Tooltip("SSAO world radius (BOTD 0.25).")]
        [Range(0.1f, 2f)] public float ssaoRadius = 0.5f;
        [Tooltip("SSAO max radius in pixels (BOTD 16). Higher = broader contact darkening, a bit more cost.")]
        [Range(8, 64)] public int ssaoMaxRadiusPixels = 24;
        [Tooltip("Contact shadow ray length in metres (BOTD 0 = effectively off).")]
        [Range(0f, 1f)] public float contactShadowLength = 0.12f;
        [Tooltip("Enable contact shadows on the sun light at runtime (restored on exit).")]
        public bool sunContactShadows = true;
        [Tooltip("31k: project-wide contact shadows master (Studio 'Contact shadows (project)', synced across all sky profiles). OFF = our code never enables contact shadows.")]
        public bool contactShadowsProject31k = false;
        public static bool ContactShadowsAllowed31k => Live != null && Live.contactShadowsProject31k;
        [Tooltip("Micro shadowing from normal/AO maps (0 = off).")]
        [Range(0f, 1f)] public float microShadowOpacity = 0.5f;
        [Tooltip("Max directional shadow distance (BOTD 150).")]
        [Range(50f, 400f)] public float shadowMaxDistance = 150f;
        [Tooltip("First cascade split (BOTD 0.076). Smaller = sharper shadows near the character.")]
        [Range(0.02f, 0.2f)] public float shadowCascadeSplit0 = 0.06f;
        [Tooltip("31s: sun (directional) shadow cascade count, driven on the DM shadow volume. Fewer = cheaper.")]
        [Range(1, 4)] public int shadowCascadeCount31s = 3;
        /// <summary>31s: shadow budget presets: 0 Subtle (4 cascades, 200 m), 1 Default (3, 150 m), 2 Extreme (2, 100 m).</summary>
        public void ApplyShadowBudgetPreset31s(int i) { int[] c = { 4, 3, 2 }; float[] d = { 200f, 150f, 100f }; i = Mathf.Clamp(i, 0, 2); shadowCascadeCount31s = c[i]; shadowMaxDistance = d[i]; }
        // 2026-11-01b: SSR cost on this profile. -1 = keep the volume's SSR quality (Jupiter/Base). 0 Low, 1 Medium, 2 High, 3 Custom (steps + min smoothness).
        [Tooltip("11-01b: SSR quality override: -1 keep volume, 0 Low, 1 Medium, 2 High, 3 Custom (Max steps + Min smoothness).")]
        [Range(-1, 3)] public int ssrQuality1101b = -1;
        [Tooltip("11-01b: Custom SSR ray march steps (fewer = cheaper).")]
        [Range(4, 128)] public int ssrMaxSteps1101b = 32;
        [Tooltip("11-01b: Custom SSR minimum smoothness: only surfaces smoother than this reflect (higher = fewer pixels traced = cheaper).")]
        [Range(0f, 1f)] public float ssrMinSmoothness1101b = 0.9f;
        [Tooltip("11-01b: push the sun shadow budget (distance/cascades) and SSR quality ABOVE the BOTD volumes (BOTD Environment sets 500 m / 4 cascades). Mars on, Jupiter off.")]
        public bool budgetOverBotd1101b = false;
        /// <summary>11-01b SSR presets (how much to lower): 0 Subtle = Medium, 1 Default = Low, 2 Extreme = Custom 12 steps / min smoothness 0.9.</summary>
        public void ApplySsrPreset1101b(int i) { i = Mathf.Clamp(i, 0, 2); ssrQuality1101b = i == 0 ? 1 : (i == 1 ? 0 : 3); if (i == 2) { ssrMaxSteps1101b = 12; ssrMinSmoothness1101b = 0.9f; } }
        [Tooltip("Fog attenuation distance / mean free path (scene 2000). 0 = leave scene fog.")]
        [Range(0f, 8000f)] public float fogMeanFreePath = 3000f;
        [Tooltip("Colour Adjustments contrast (0 = none).")]
        [Range(-20f, 30f)] public float gradeContrast = 5f;
        [Tooltip("Split toning strength: cooler shadows / warmer highlights (0 = off).")]
        [Range(0f, 1f)] public float splitToneStrength = 0.15f;
        public Color splitToneShadows = new Color(0.42f, 0.52f, 0.66f, 1f);
        public Color splitToneHighlights = new Color(0.66f, 0.55f, 0.42f, 1f);
        [Tooltip("Vignette intensity (BOTD 0.201).")]
        [Range(0f, 0.5f)] public float vignetteIntensity = 0.22f;

        public void ApplyStartHourPreset(float hour) { startHour = Mathf.Clamp(hour, 0f, 30f); }

        [Header("Eclipse Readability Floor (24ap)")]
        [Tooltip("Keeps eclipse from going pitch black: ambient floor, dim fill light, and a brighter exposure limit, all scaled by eclipse depth.")]
        public bool eclipseFloorEnabled = true;
        [Tooltip("Minimum indirect (ambient) multiplier during eclipse; floors eclipseAmbientMultiplier.")]
        [Range(0f, 1f)] public float eclipseMinAmbient = 0.35f;
        [Tooltip("Dim shadowless fill (lux) added at full eclipse via the shade-fill light (Jupiter-shine side).")]
        [Range(0f, 500f)] public float eclipseFillLux = 40f;
        [Tooltip("Exposure Limit Min (EV) at full eclipse. Lower = auto exposure may brighten more in the dark.")]
        [Range(-6f, 20f)] public float eclipseExposureLimitMin = 2f;

        [Header("Night Sky - DM_IoNightSky (24au)")]
        [Tooltip("Procedural stars + Milky Way on a runtime dome (additive, no temporal effects).")]
        public bool nightSkyEnabled = true;
        public Shader nightSkyShader;
        [Range(40f, 600f)] public float starDensity = 180f;
        [Range(0f, 40f)] public float starBrightness = 4f;
        [Range(0.3f, 3f)] public float starSize = 1f;
        [Range(0f, 1f)] public float starTwinkle = 0.3f;
        [Range(0f, 1f)] public float starColorVariation = 0.6f;
        [Range(0f, 20f)] public float milkyWayIntensity = 1.5f;
        [Range(0.02f, 0.6f)] public float milkyWayWidth = 0.18f;
        [Range(0f, 1f)] public float milkyWayDust = 0.7f;
        [ColorUsage(false, true)] public Color milkyWayCoreColor = new Color(1f, 0.85f, 0.7f, 1f);
        [ColorUsage(false, true)] public Color milkyWayEdgeColor = new Color(0.55f, 0.65f, 1f, 1f);
        [Tooltip("Milky Way plane orientation.")]
        [Range(-90f, 90f)] public float milkyWayTiltDeg = 62f;
        [Range(0f, 360f)] public float milkyWayYawDeg = 35f;
        [Range(0f, 360f)] public float starRotationDeg;
        [Tooltip("Stars turn once per Io day with the clock.")]
        public bool starsRotateWithClock = true;
        [Tooltip("Sun elevation (deg) where stars start to fade out / are fully gone.")]
        [Range(-30f, 30f)] public float nightFadeStartDeg = -6f;
        [Range(-30f, 45f)] public float nightFadeEndDeg = 8f;

        [Header("Night Light (2026-10-26f, trilight-style ambient fill)")]
        [Tooltip("On = additive Ground / Mid / Sky fills fade in with nightFadeStartDeg/EndDeg. Blends with Jupiter-shine; does not replace day lighting. No aurora light.")]
        public bool enableNightLight = true;
        [Tooltip("Ambient from below (Io ground bounce).")]
        public Color nightLightGroundColor = new Color(0.38f, 0.24f, 0.14f, 1f);
        [Tooltip("Ambient from the horizon band (equator / mid).")]
        public Color nightLightMidColor = new Color(0.55f, 0.48f, 0.62f, 1f);
        [Tooltip("Ambient from the sky dome.")]
        public Color nightLightSkyColor = new Color(0.32f, 0.42f, 0.78f, 1f);
        [Tooltip("Master lux for the three fills combined. Default is brighter than prior night but still night-readable.")]
        [Range(0f, 1200f)] public float nightLightAmount = 55f;
        [Tooltip("2026-10-26g: Exposure Limit Min (EV) at full night. Lower = auto-exposure may brighten more so Night Light is visible (day uses exposureLimitMin). Fades with nightFade*. Same idea as eclipseExposureLimitMin.")]
        [Range(-6f, 20f)] public float nightExposureLimitMin = 3.5f;
        [Tooltip("2026-10-26g: added to Indirect Diffuse at full night (Ground/Mid/Sky ambient lift). 0 = fills only.")]
        [Range(0f, 4f)] public float nightAmbientBoost = 0.75f;

        [Header("Night Shadow (2026-10-26j)")]
        [Tooltip("On = at night the Jupiter-shine directional takes the single HDRP directional shadow from the sun (fades with NightLightFactor). Edit preview never casts (scene sun keeps the cascade slot).")]
        public bool enableNightShadow = true;
        [Tooltip("Shadow dimmer on the night key light (0 = invisible shadows, 1 = full). Scaled by NightLightFactor.")]
        [Range(0f, 1f)] public float nightShadowStrength = 0.55f;
        [Tooltip("Penumbra softness as angular diameter (deg). interactsWithSky stays false so this never draws a sky disc.")]
        [Range(0.1f, 30f)] public float nightShadowSoftnessDeg = 4f;
        [Tooltip("0 = follow Jupiter direction (default key). 1 = custom yaw/pitch.")]
        [Range(0, 1)] public int nightShadowDirectionMode = 0;
        [Range(0f, 360f)] public float nightShadowCustomYawDeg = 200f;
        [Range(-89f, 89f)] public float nightShadowCustomPitchDeg = 35f;
        [Tooltip("0 Low 256, 1 Medium 512, 2 High 1024, 3 Ultra 2048. HDRP has one directional cascade atlas — keep Medium unless needed.")]
        [Range(0, 3)] public int nightShadowResolutionTier = 1;
        [Tooltip("Max directional shadow distance (m) while night owns the shadow. Day uses Polish shadowMaxDistance.")]
        [Range(20f, 800f)] public float nightShadowMaxDistance = 120f;
        [Tooltip("Star visibility left in full daylight (Milky Way has its own Day Visibility). Fades with nightFadeStart/End.")]
        [Range(0f, 1f)] public float nightSkyDayVisibility = 0f;
        [Header("Milky Way (2026-10-26i)")]
        [Tooltip("Milky Way visibility in full daylight. 0 = gone by day (uses nightFadeStart/End). Was sharing nightSkyDayVisibility and looked like blotchy daytime fog.")]
        [Range(0f, 1f)] public float milkyWayDayVisibility = 0f;
        [Tooltip("Softens MW dust / band noise (higher = smoother, less blotchy banding). 0 = prior look.")]
        [Range(0f, 1f)] public float milkyWaySmoothness = 0.55f;
        [Header("Night Sky Banding (2026-10-26m)")]
        [Tooltip("Triangular / interleaved-gradient dither after exposure before the colour write (helps HDRP R11G11B10 banding). Live + persistent.")]
        public bool nightSkyDitherEnabled = false;
        [Range(0f, 2f)] public float nightSkyDitherAmount = 0.55f;
        [Tooltip("Power on exposure for MW/aurora (stars stay linear). Below 0.5 (old sqrt) reduces AE band amplification. Default 0.28.")]
        [Range(0.1f, 1f)] public float nightSkySoftExpPower = 0.28f;
        [Tooltip("25o: stars + Milky Way are hidden inside the discs of Jupiter and the moons (always behind sky bodies).")]
        public bool starsBehindSkyBodies = true;
        [Tooltip("2026-10-26n: aurora draws additively over Jupiter/moons (atmospheric). Off = aurora also cut out inside discs (old look).")]
        public bool auroraOverSkyBodies = true;
        [Tooltip("2026-10-26n: extra angular radius (deg) on the star/MW cutout. 0 = fit disc. Keep small to avoid a dark halo.")]
        [Range(-0.5f, 1f)] public float starOcclusionMarginDeg = 0f;
        [Tooltip("2026-10-26n: soft edge (deg) straddling the limb for star/MW cutout. Small = no dark ring.")]
        [Range(0.01f, 1f)] public float starOcclusionSoftDeg = 0.08f;

        [Header("Edit-mode preview (25o)")]
        [Tooltip("Sky, Jupiter, moons and stars preview live in the Scene/Game view without Play (DontSave clones, scene never dirtied).")]
        public bool editPreviewEnabled = true;
        [Tooltip("Io hour shown in edit mode (0-30; -1 = clock's current hour). Orbit time only advances in Play.")]
        [Range(-1f, 30f)] public float editPreviewHour = 15.5f;
        [Tooltip("25o: night dome radius as a fraction of the camera far plane (kept behind Jupiter/moons, max 0.75 / 0.9 far).")]
        [Range(0.92f, 0.995f)] public float nightDomeFarFraction = 0.98f;
        [Tooltip("25o: grow the camera-centred star dome past the farthest corner of all terrain tiles (+ margin), so no tile edge/horizon gap. Capped at 0.995 x camera far clip.")]
        public bool nightDomeCoverTerrain = true;
        [Tooltip("25o: extra metres beyond the farthest terrain corner.")]
        [Range(0f, 20000f)] public float nightDomeTerrainMargin = 500f;
        [Tooltip("25o: minimum dome radius in metres (0 = off). Still capped at 0.995 x far clip.")]
        [Range(0f, 100000f)] public float nightDomeMinRadius = 0f;

        [Header("Aurora (24au)")]
        [Tooltip("Io aurora: always on, strongest at night / eclipse. Low = Na/S blue-green, high = O red/violet.")]
        public bool auroraEnabled = true;
        [Range(0f, 20f)] public float auroraIntensity = 1.5f;
        [ColorUsage(false, true)] public Color auroraColorLow = new Color(0.2f, 0.9f, 0.75f, 1f);
        [ColorUsage(false, true)] public Color auroraColorHigh = new Color(0.85f, 0.2f, 0.55f, 1f);
        [Range(0f, 0.3f)] public float auroraSpeed = 0.03f;
        [Tooltip("Height of the curtains above the horizon (fraction of the sky dome).")]
        [Range(0.05f, 0.8f)] public float auroraHeight = 0.3f;
        [Range(0f, 1f)] public float auroraCoverage = 0.55f;
        [Tooltip("Aurora visibility in full daylight. 0 = night/eclipse only (nightFade*).")]
        [Range(0f, 1f)] public float auroraDayVisibility = 0f;
        [Tooltip("Softens aurora curtain / ray noise (higher = less choppy banding).")]
        [Range(0f, 1f)] public float auroraSmoothness = 0.55f;
        [Header("Aurora height coverage (25p)")]
        [Tooltip("25p: elevation band, noise-varied edges along azimuth, azimuth span around a centre heading (0 = dome +Z, follows Sky Rotation), soft top/bottom, slow height drift.")]
        [Range(-10f, 80f)] public float auroraMinElevDeg = 0.5f;
        [Range(1f, 90f)] public float auroraMaxElevDeg = 18f;
        [Range(0f, 40f)] public float auroraHeightVariationDeg = 6f;
        [Range(0.2f, 20f)] public float auroraHeightNoiseScale = 3f;
        [Range(0.05f, 30f)] public float auroraBottomSoftDeg = 2f;
        [Range(0.05f, 45f)] public float auroraTopSoftDeg = 8f;
        [Range(5f, 360f)] public float auroraAzimuthSpanDeg = 360f;
        [Range(0f, 360f)] public float auroraAzimuthCenterDeg = 0f;
        [Range(0f, 180f)] public float auroraAzimuthEdgeSoftDeg = 30f;
        [Range(0f, 1f)] public float auroraHeightDriftSpeed = 0.02f;
        [Range(0f, 20f)] public float auroraHeightBobDeg = 2f;

        public void ApplyNightSkyPreset(int preset)
        {
            nightSkyEnabled = true;
            starsBehindSkyBodies = true;
            if (preset <= 0) { starOcclusionMarginDeg = 0.05f; starOcclusionSoftDeg = 0.05f; starDensity = 140f; starBrightness = 2.5f; starSize = 0.9f; starTwinkle = 0.15f; milkyWayIntensity = 0.8f; milkyWayDust = 0.6f; nightFadeStartDeg = -8f; nightFadeEndDeg = 4f; }
            else if (preset == 1) { starsBehindSkyBodies = true; auroraOverSkyBodies = true; starOcclusionMarginDeg = 0f; starOcclusionSoftDeg = 0.08f; starDensity = 180f; starBrightness = 4f; starSize = 1f; starTwinkle = 0.3f; milkyWayIntensity = 1.5f; milkyWayDust = 0.7f; nightFadeStartDeg = -6f; nightFadeEndDeg = 8f; }
            else { starOcclusionMarginDeg = 0.6f; starOcclusionSoftDeg = 0.5f; starDensity = 280f; starBrightness = 8f; starSize = 1.3f; starTwinkle = 0.5f; milkyWayIntensity = 3.5f; milkyWayDust = 0.85f; nightFadeStartDeg = -3f; nightFadeEndDeg = 15f; }
        }

        public void ApplyAuroraPreset(int preset)
        {
            auroraEnabled = true;
            if (preset <= 0) { auroraIntensity = 0.7f; auroraSpeed = 0.02f; auroraHeight = 0.2f; auroraCoverage = 0.4f; auroraDayVisibility = 0f; auroraSmoothness = 0.7f; auroraMinElevDeg = 1f; auroraMaxElevDeg = 10f; auroraHeightVariationDeg = 3f; auroraHeightNoiseScale = 2f; auroraBottomSoftDeg = 1.5f; auroraTopSoftDeg = 5f; auroraAzimuthSpanDeg = 360f; auroraAzimuthCenterDeg = 0f; auroraAzimuthEdgeSoftDeg = 30f; auroraHeightDriftSpeed = 0.01f; auroraHeightBobDeg = 1f; }
            else if (preset == 1) { auroraIntensity = 1.5f; auroraSpeed = 0.03f; auroraHeight = 0.3f; auroraCoverage = 0.55f; auroraDayVisibility = 0f; auroraSmoothness = 0.55f; auroraMinElevDeg = 0.5f; auroraMaxElevDeg = 18f; auroraHeightVariationDeg = 6f; auroraHeightNoiseScale = 3f; auroraBottomSoftDeg = 2f; auroraTopSoftDeg = 8f; auroraAzimuthSpanDeg = 360f; auroraAzimuthCenterDeg = 0f; auroraAzimuthEdgeSoftDeg = 30f; auroraHeightDriftSpeed = 0.02f; auroraHeightBobDeg = 2f; }
            else { auroraIntensity = 4f; auroraSpeed = 0.06f; auroraHeight = 0.5f; auroraCoverage = 0.8f; auroraDayVisibility = 0.05f; auroraSmoothness = 0.35f; auroraMinElevDeg = 0f; auroraMaxElevDeg = 40f; auroraHeightVariationDeg = 18f; auroraHeightNoiseScale = 5f; auroraBottomSoftDeg = 4f; auroraTopSoftDeg = 14f; auroraAzimuthSpanDeg = 360f; auroraAzimuthCenterDeg = 0f; auroraAzimuthEdgeSoftDeg = 30f; auroraHeightDriftSpeed = 0.06f; auroraHeightBobDeg = 6f; }
        }

        /// <summary>2026-10-26i: Milky Way day fade + smoothness. 0 Subtle, 1 Default, 2 Extreme.</summary>
        public void ApplyMilkyWayPreset(int preset)
        {
            nightSkyEnabled = true;
            if (preset <= 0) { milkyWayIntensity = 0.8f; milkyWayDust = 0.45f; milkyWayWidth = 0.14f; milkyWayDayVisibility = 0f; milkyWaySmoothness = 0.75f; }
            else if (preset == 1) { milkyWayIntensity = 1.5f; milkyWayDust = 0.7f; milkyWayWidth = 0.18f; milkyWayDayVisibility = 0f; milkyWaySmoothness = 0.55f; }
            else { milkyWayIntensity = 3.5f; milkyWayDust = 0.85f; milkyWayWidth = 0.28f; milkyWayDayVisibility = 0.05f; milkyWaySmoothness = 0.3f; }
        }

        [Header("Anti-ghosting (24as)")]
        [Tooltip("Io has no atmosphere: force HDRP Volumetric Clouds OFF (Edit + Play, Scene + Game view) via a hidden, unsaved priority-95 volume. Their temporal reprojection smears terrain/lights across the sky.")]
        public bool disableVolumetricClouds = false; // 24ay: ghosting was a planet/space material, clouds allowed again

        [Header("Eclipse Shadows (24av)")]
        [Tooltip("During the eclipse the Jupiter/ring light casts subtle soft shadows (takes over the single HDRP directional shadow from the hidden sun).")]
        public bool eclipseShadowsEnabled = true;
        [Range(0f, 1f)] public float eclipseShadowStrength = 0.6f;
        [Range(0.5f, 20f)] public float eclipseShadowSoftnessDeg = 6f;

        [Header("Io Orbit + Shadow Cleanup (2026-10-30a)")]
        [Tooltip("30a: SSR on the Io post volume. Off removes all screen-space reflections (incl. the yellow sun streak).")]
        public bool ssrEnabled = true;
        [Tooltip("30a: SSR depth thickness. High values (BOTD 0.85) smear the sun/sky into tall vertical streaks on the ground. ~0.05-0.15 = clean.")]
        [Range(0.005f, 1f)] public float ssrDepthThickness = 0.08f;

        /// <summary>30a: 0 Subtle, 1 Default, 2 Extreme. Releases the debug hour (sun orbits), sizes Jupiter + Galilean moons for visibility. Clock speed is set by Studio (Io Clock profile).</summary>
        public void ApplyIoOrbitPreset(int preset)
        {
            debugHourOverride = -1f;
            if (preset <= 0) { jupiterAngularDiameterDeg = 25f; SetMoonSize(2f); }
            else if (preset == 1) { jupiterAngularDiameterDeg = 37.5f; SetMoonSize(4f); }
            else { jupiterAngularDiameterDeg = 55f; SetMoonSize(8f); }
        }

        /// <summary>30a: 0 Subtle, 1 Default, 2 Extreme cleanup of the blurry eclipse shadow, muddy AO, contact darkening, bloom, ring flare and SSR streak.</summary>
        public void ApplyShadowCleanupPreset(int preset)
        {
            ssrEnabled = true; sunContactShadows = contactShadowsProject31k;
            if (preset <= 0) { eclipseShadowSoftnessDeg = 5f; eclipseShadowStrength = 0.7f; ssaoIntensity = 1.1f; ssaoRadius = 0.7f; ssaoMaxRadiusPixels = 24; ssaoDirectLightingStrength = 0.2f; contactShadowLength = 0.25f; microShadowOpacity = 0.45f; bloomIntensity = 0.5f; bloomThreshold = 0.7f; ringFlareIntensity = 0.3f; flareRingDuringEclipse = 0.45f; ssrDepthThickness = 0.2f; shadowCascadeSplit0 = 0.07f; }
            else if (preset == 1) { eclipseShadowSoftnessDeg = 3f; eclipseShadowStrength = 0.5f; ssaoIntensity = 0.9f; ssaoRadius = 0.5f; ssaoMaxRadiusPixels = 20; ssaoDirectLightingStrength = 0.15f; contactShadowLength = 0.12f; microShadowOpacity = 0.35f; bloomIntensity = 0.35f; bloomThreshold = 0.8f; ringFlareIntensity = 0.2f; flareRingDuringEclipse = 0.3f; ssrDepthThickness = 0.08f; shadowCascadeSplit0 = 0.06f; }
            else { eclipseShadowSoftnessDeg = 1.5f; eclipseShadowStrength = 0.35f; ssaoIntensity = 0.6f; ssaoRadius = 0.35f; ssaoMaxRadiusPixels = 16; ssaoDirectLightingStrength = 0.1f; contactShadowLength = 0.06f; microShadowOpacity = 0.2f; bloomIntensity = 0.2f; bloomThreshold = 1.2f; ringFlareIntensity = 0.1f; flareRingDuringEclipse = 0.15f; ssrDepthThickness = 0.04f; shadowCascadeSplit0 = 0.05f; }
        }

        [Header("Eclipse Ring & Flare (24aq)")]
        [Tooltip("Soft edge (deg) where the sun disc slides behind Jupiter's limb. Direct sun = 0 when fully behind.")]
        [Range(0.05f, 3f)] public float sunOcclusionSoftDeg = 0.6f;
        [Tooltip("Ring brightness bias toward the side nearest the sun (higher = more one-sided).")]
        [Range(0f, 8f)] public float ringSunBias = 3f;
        [Tooltip("Ring starts growing when the sun is within this many degrees outside the limb.")]
        [Range(0.5f, 30f)] public float ringEdgeGrowDeg = 10f;
        [Tooltip("Ring strength at deepest totality (sun at Jupiter's centre) relative to at the limb.")]
        [Range(0f, 1f)] public float ringMinFactor = 0.35f;
        [Tooltip("Dim light (lux) cast from the ring / Jupiter direction, scaled by ring strength.")]
        [Range(0f, 300f)] public float ringLightLux = 20f;
        [Tooltip("Sun lens-flare strength while the sun is behind the limb (ring flare). Flare is otherwise scaled by sun visibility, so it never shows through Jupiter.")]
        [Range(0f, 1f)] public float ringFlareIntensity = 0.25f;
        [Header("Day / night (25q)")]
        [Tooltip("25q: fraction of the Io day the sun is above the horizon. 0.5 = even circle (original). Noon stays at hpd/2; the eclipse keeps its clock-hour length.")]
        [Range(0.2f, 0.9f)] public float dayFraction = 0.5f;
        public void ApplyDayFractionPreset(float f) { dayFraction = Mathf.Clamp(f, 0.2f, 0.9f); }
        /// <summary>25q: clock hour -> sun-path hour so the day half spans dayFraction of the day (identity at 0.5).</summary>
        public static float RemapDayHour(float hour, float hoursPerDay, float fraction, out float slopeAtNoon)
        {
            slopeAtNoon = 1f;
            if (Mathf.Abs(fraction - 0.5f) < 1e-4f) return hour;
            float noon = hoursPerDay * 0.5f, q = hoursPerDay * 0.25f;
            float dayHalf = Mathf.Clamp(fraction, 0.05f, 0.95f) * noon;
            slopeAtNoon = q / dayHalf;
            float d = hour - noon; d -= hoursPerDay * Mathf.Floor((d + noon) / hoursPerDay);
            float ad = Mathf.Abs(d);
            float sd = ad <= dayHalf ? ad * slopeAtNoon : q + (ad - dayHalf) * (q / (noon - dayHalf));
            return noon + Mathf.Sign(d) * sd;
        }
        [Header("Sun flare (25p)")]
        public bool flareEnabled = true;
        [Tooltip("Depth occlusion: terrain/mountains/Jupiter hide the flare.")]
        public bool flareUseOcclusion = true;
        [Range(1, 64)] public int flareOcclusionSamples = 16;
        [Range(0f, 4f)] public float flareIntensityScale = 1f;
        [Range(-10f, 30f)] public float flareMinElevationDeg = 1f;
        [Range(0.01f, 45f)] public float flareElevationFadeDeg = 4f;
        [Range(0f, 1f)] public float flareEclipseFade = 1f;
        [Range(0f, 1f)] public float flareRingDuringEclipse = 0f;
        [Range(0f, 5f)] public float flareOcclusionRadius = 0.1f;
        public void ApplyFlarePreset(int preset)
        {
            flareEnabled = true; flareUseOcclusion = true;
            if (preset <= 0) { flareIntensityScale = 0.5f; flareMinElevationDeg = 3f; flareElevationFadeDeg = 6f; flareEclipseFade = 1f; flareRingDuringEclipse = 0f; flareOcclusionRadius = 0.2f; flareOcclusionSamples = 24; }
            else if (preset == 1) { flareIntensityScale = 1f; flareMinElevationDeg = 1f; flareElevationFadeDeg = 4f; flareEclipseFade = 1f; flareRingDuringEclipse = 0f; flareOcclusionRadius = 0.1f; flareOcclusionSamples = 16; }
            else { flareIntensityScale = 2f; flareMinElevationDeg = -1f; flareElevationFadeDeg = 2f; flareEclipseFade = 0.7f; flareRingDuringEclipse = 1f; flareOcclusionRadius = 0.05f; flareOcclusionSamples = 8; }
        }
        [Tooltip("Turn SSGI off during deep eclipse (kills temporal GI trails behind bright emissives in the dark).")]
        public bool eclipseDisableSsgi = true;
        [Tooltip("Disable lens flares on non-sun directional lights (BOTD clone etc.).")]
        public bool disableOtherSunFlares = true;

        public void ApplyRingPreset(int preset)
        {
            if (preset <= 0) { jupiterEclipseRimLux = 250f; ringSunBias = 2f; ringLightLux = 10f; ringFlareIntensity = 0.12f; ringEdgeGrowDeg = 8f; }
            else if (preset == 1) { jupiterEclipseRimLux = 400f; ringSunBias = 3f; ringLightLux = 20f; ringFlareIntensity = 0.25f; ringEdgeGrowDeg = 10f; }
            else { jupiterEclipseRimLux = 1200f; ringSunBias = 4f; ringLightLux = 50f; ringFlareIntensity = 0.5f; ringEdgeGrowDeg = 14f; }
        }

        public void ApplyEclipseFloorPreset(int preset)
        {
            eclipseFloorEnabled = true;
            if (preset <= 0) { eclipseMinAmbient = 0.25f; eclipseFillLux = 25f; eclipseExposureLimitMin = 3f; }
            else if (preset == 1) { eclipseMinAmbient = 0.35f; eclipseFillLux = 40f; eclipseExposureLimitMin = 2f; }
            else { eclipseMinAmbient = 0.55f; eclipseFillLux = 100f; eclipseExposureLimitMin = 1f; }
        }

        public void ApplyPolishPreset(int preset)
        {
            polishEnabled = true; sunContactShadows = contactShadowsProject31k; shadowMaxDistance = 150f;
            if (preset <= 0) { ssaoRadius = 0.35f; ssaoMaxRadiusPixels = 16; contactShadowLength = 0.08f; microShadowOpacity = 0.3f; shadowCascadeSplit0 = 0.07f; fogMeanFreePath = 3600f; gradeContrast = 3f; splitToneStrength = 0.08f; vignetteIntensity = 0.21f; }
            else if (preset == 1) { ssaoRadius = 0.5f; ssaoMaxRadiusPixels = 24; contactShadowLength = 0.12f; microShadowOpacity = 0.5f; shadowCascadeSplit0 = 0.06f; fogMeanFreePath = 3000f; gradeContrast = 5f; splitToneStrength = 0.15f; vignetteIntensity = 0.22f; }
            else { ssaoRadius = 0.8f; ssaoMaxRadiusPixels = 32; contactShadowLength = 0.2f; microShadowOpacity = 0.75f; shadowCascadeSplit0 = 0.05f; fogMeanFreePath = 2200f; gradeContrast = 10f; splitToneStrength = 0.3f; vignetteIntensity = 0.26f; }
        }

        /// <summary>0 Subtle, 1 Default, 2 Extreme.</summary>
        public void ApplyIndirectPreset(int preset)
        {
            realtimeIndirectEnabled = true; ssgiEnabled = true; ambientModeDynamic = true;
            ApplyPolishPreset(preset);
            ssgiQuality = IndirectQuality.Custom; ssgiFullResolution = true; ssgiDenoise = true; overrideGroundTint = true; fixCameraVolumeMask = true; exposureOverride = true; exposureHistogram = true; exposureCompensation = 0f; exposureHistogramLow = 40f; exposureHistogramHigh = 90f; exposureAdaptDarkToLight = 2f; exposureAdaptLightToDark = 1.5f; sunHorizonDimDegrees = 20f;
            if (preset <= 0) { ssgiMaxRaySteps = 32; ssgiHalfResDenoiser = true; ssgiDenoiserRadius = 0.45f; ssgiDepthThickness = 0.06f; indirectDiffuseMultiplier = 1.0f; indirectReflectionMultiplier = 1.2f; shadowFloorLift = 0.03f; groundBounceTint = new Color(0.4f, 0.24f, 0.1f, 1f); ssaoIntensity = 0.95f; ssaoDirectLightingStrength = 0.18f; exposureLimitMin = 6.4f; exposureLimitMax = 8.5f; sunHorizonMinFactor = 0.7f; sunHorizonDesaturate = 0.3f; sunMaxLux = 10000f; shadeFillEnabled = true; shadeFillLux = 350f; bloomIntensity = 0.38f; bloomThreshold = 0.2f; }
            else if (preset == 1) { ssgiMaxRaySteps = 64; ssgiHalfResDenoiser = true; ssgiDenoiserRadius = 0.6f; ssgiDepthThickness = 0.1f; indirectDiffuseMultiplier = 1.2f; indirectReflectionMultiplier = 1.0f; shadowFloorLift = 0.08f; groundBounceTint = new Color(0.45f, 0.36f, 0.24f, 1f); ssaoIntensity = 1.05f; ssaoDirectLightingStrength = 0.22f; exposureLimitMin = 6.4f; exposureLimitMax = 9f; sunHorizonMinFactor = 0.5f; sunHorizonDesaturate = 0.6f; sunMaxLux = 8000f; shadeFillEnabled = true; shadeFillLux = 700f; bloomIntensity = 0.3f; bloomThreshold = 0.5f; }
            else { ssgiMaxRaySteps = 96; ssgiHalfResDenoiser = false; ssgiDenoiserRadius = 0.8f; ssgiDepthThickness = 0.15f; indirectDiffuseMultiplier = 2.4f; indirectReflectionMultiplier = 1.4f; shadowFloorLift = 0.12f; groundBounceTint = new Color(0.7f, 0.45f, 0.15f, 1f); ssaoIntensity = 1.25f; ssaoDirectLightingStrength = 0.3f; exposureLimitMin = 6.0f; exposureLimitMax = 9.5f; sunHorizonMinFactor = 0.35f; sunHorizonDesaturate = 0.8f; sunMaxLux = 6000f; shadeFillEnabled = true; shadeFillLux = 1200f; bloomIntensity = 0.25f; bloomThreshold = 0.8f; }
        }

        public DMIoMoonSettings[] Moons => new[] { europa, ganymede, callisto };

        /// <summary>0 Subtle, 1 Default, 2 Extreme.</summary>
        public void ApplyPreset(int preset)
        {
            if (preset <= 0)
            {
                eclipseDurationHours = 6f; eclipseFadeHours = 1.5f; eclipseSunRemaining = 0.15f; eclipseAmbientMultiplier = 0.5f;
                jupiterShineLux = 15f; eclipseJupiterShineLux = 8f; jupiterEclipseRimLux = 200f; SetMoonSize(1f);
            }
            else if (preset == 1)
            {
                eclipseDurationHours = 10f; eclipseFadeHours = 1.5f; eclipseSunRemaining = 0f; eclipseAmbientMultiplier = 0.25f;
                jupiterShineLux = 25f; eclipseJupiterShineLux = 15f; jupiterEclipseRimLux = 400f; SetMoonSize(1f);
            }
            else
            {
                eclipseDurationHours = 14f; eclipseFadeHours = 1f; eclipseSunRemaining = 0f; eclipseAmbientMultiplier = 0.08f;
                jupiterShineLux = 60f; eclipseJupiterShineLux = 40f; jupiterEclipseRimLux = 1500f; SetMoonSize(3f);
            }
        }

        /// <summary>Real Io eclipse length (~1.6 h) with a short fade.</summary>
        public void ApplyRealisticEclipse()
        {
            eclipseDurationHours = RealisticEclipseHours;
            eclipseFadeHours = 0.25f;
        }

        /// <summary>0 Subtle, 1 Default, 2 Extreme. Default is noticeably brighter than pre-26f night, still night.</summary>
        public void ApplyNightLightPreset(int preset)
        {
            enableNightLight = true;
            if (preset <= 0)
            {
                nightLightGroundColor = new Color(0.28f, 0.18f, 0.12f, 1f);
                nightLightMidColor = new Color(0.4f, 0.38f, 0.48f, 1f);
                nightLightSkyColor = new Color(0.22f, 0.28f, 0.5f, 1f);
                nightLightAmount = 40f;
                nightExposureLimitMin = 4.5f;
                nightAmbientBoost = 0.35f;
            }
            else if (preset == 1)
            {
                nightLightGroundColor = new Color(0.38f, 0.24f, 0.14f, 1f);
                nightLightMidColor = new Color(0.55f, 0.48f, 0.62f, 1f);
                nightLightSkyColor = new Color(0.32f, 0.42f, 0.78f, 1f);
                nightLightAmount = 120f;
                nightExposureLimitMin = 3.5f;
                nightAmbientBoost = 0.75f;
            }
            else
            {
                nightLightGroundColor = new Color(0.55f, 0.32f, 0.18f, 1f);
                nightLightMidColor = new Color(0.7f, 0.58f, 0.85f, 1f);
                nightLightSkyColor = new Color(0.45f, 0.6f, 1f, 1f);
                nightLightAmount = 420f;
                nightExposureLimitMin = 2f;
                nightAmbientBoost = 1.6f;
            }
            ApplyNightShadowPreset(preset);
        }

        /// <summary>2026-10-26j: night directional shadow (Jupiter-shine). 0 Subtle, 1 Default, 2 Extreme.</summary>
        public void ApplyNightShadowPreset(int preset)
        {
            enableNightShadow = true;
            nightShadowDirectionMode = 0;
            if (preset <= 0)
            {
                nightShadowStrength = 0.3f;
                nightShadowSoftnessDeg = 8f;
                nightShadowResolutionTier = 0;
                nightShadowMaxDistance = 80f;
            }
            else if (preset == 1)
            {
                nightShadowStrength = 0.55f;
                nightShadowSoftnessDeg = 4f;
                nightShadowResolutionTier = 1;
                nightShadowMaxDistance = 120f;
            }
            else
            {
                nightShadowStrength = 0.9f;
                nightShadowSoftnessDeg = 1.5f;
                nightShadowResolutionTier = 2;
                nightShadowMaxDistance = 220f;
            }
        }

﻿        // ---- 2026-10-26m Sky Look Preset (Custom / Subtle / Normal / Extreme), body-weight style snapshots ----

        [SerializeField, HideInInspector] private SkyLookPreset skyLookAppliedPresetInternal = SkyLookPreset.Custom;
        [SerializeField, HideInInspector] private System.Collections.Generic.List<string> skyLookCustomStashNames = new System.Collections.Generic.List<string>();
        [SerializeField, HideInInspector] private System.Collections.Generic.List<string> skyLookCustomStashValues = new System.Collections.Generic.List<string>();
        public bool HasSkyLookCustomStash => skyLookCustomStashNames != null && skyLookCustomStashNames.Count > 0;

        // Look fields only: sky / MW / aurora / stars / night light+shadow / eclipse brightness / dither. Orbit & time excluded.
        private static readonly string[] SkyLookFieldNames =
        {
            "nightSkyEnabled", "starDensity", "starBrightness", "starSize", "starTwinkle", "starColorVariation",
            "milkyWayIntensity", "milkyWayWidth", "milkyWayDust", "milkyWayCoreColor", "milkyWayEdgeColor",
            "milkyWayTiltDeg", "milkyWayYawDeg", "starRotationDeg", "starsRotateWithClock",
            "nightFadeStartDeg", "nightFadeEndDeg", "nightSkyDayVisibility", "milkyWayDayVisibility", "milkyWaySmoothness",
            "starsBehindSkyBodies", "auroraOverSkyBodies", "starOcclusionMarginDeg", "starOcclusionSoftDeg",
            "nightDomeFarFraction", "nightDomeCoverTerrain", "nightDomeTerrainMargin", "nightDomeMinRadius",
            "auroraEnabled", "auroraIntensity", "auroraColorLow", "auroraColorHigh", "auroraSpeed", "auroraHeight",
            "auroraCoverage", "auroraDayVisibility", "auroraSmoothness",
            "auroraMinElevDeg", "auroraMaxElevDeg", "auroraHeightVariationDeg", "auroraHeightNoiseScale",
            "auroraBottomSoftDeg", "auroraTopSoftDeg", "auroraAzimuthSpanDeg", "auroraAzimuthCenterDeg",
            "auroraAzimuthEdgeSoftDeg", "auroraHeightDriftSpeed", "auroraHeightBobDeg",
            "enableNightLight", "nightLightGroundColor", "nightLightMidColor", "nightLightSkyColor",
            "nightLightAmount", "nightExposureLimitMin", "nightAmbientBoost",
            "enableNightShadow", "nightShadowStrength", "nightShadowSoftnessDeg", "nightShadowDirectionMode",
            "nightShadowCustomYawDeg", "nightShadowCustomPitchDeg", "nightShadowResolutionTier", "nightShadowMaxDistance",
            "eclipseSunRemaining", "eclipseAmbientMultiplier", "jupiterShineColor", "jupiterShineLux",
            "eclipseJupiterShineLux", "jupiterEclipseRimLux", "jupiterBrightness", "jupiterLimbDarkening", "jupiterRimColor", "jupiterRimSoftness", "skyBodyEdgeSoft",
            "eclipseFloorEnabled", "eclipseMinAmbient", "eclipseFillLux", "eclipseExposureLimitMin",
            "eclipseShadowsEnabled", "eclipseShadowStrength", "eclipseShadowSoftnessDeg",
            "nightSkyDitherEnabled", "nightSkyDitherAmount", "nightSkySoftExpPower"
        };

        private static System.Collections.Generic.List<System.Reflection.FieldInfo> s_skyLookFields;
        // 2026-10-30d: extra fields the world presets (Mars / Jupiter (Io)) set; also kept in the Custom stash.
        private static readonly string[] SkyWorldExtraFieldNames =
        {
            "showJupiter","showMoons","jupiterAngularDiameterDeg","eclipseEnabled","sunIntensityMultiplier","sunMaxLux",
            "sunAngularDiameterDeg","skyTintEnabled","skyZenithTint","skyHorizonTint","skyHorizonZenithShift",
            "groundBounceTint","shadowFloorTint","shadeFillColor","shadeFillLux","ssaoIntensity","ssaoRadius","ssaoMaxRadiusPixels",
            "ssaoDirectLightingStrength","ssrEnabled","ssrDepthThickness","bloomIntensity","bloomThreshold","contactShadowLength",
            "microShadowOpacity","ringFlareIntensity","flareRingDuringEclipse","fogMeanFreePath","shadowCascadeSplit0"
        };
        private static System.Collections.Generic.List<System.Reflection.FieldInfo> s_skyAllFields;
        private static System.Collections.Generic.List<System.Reflection.FieldInfo> SkyAllFields
        {
            get
            {
                if (s_skyAllFields != null) return s_skyAllFields;
                s_skyAllFields = new System.Collections.Generic.List<System.Reflection.FieldInfo>(SkyLookFields);
                // 30g: world looks cover EVERY public float/int/bool/Color/enum field (sun, orbit, sky, post), not only the look list.
                foreach (var f in typeof(DM_IoSkyProfile).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (f.Name == "skyLookPreset" || f.Name == "debugHourOverride" || f.Name == "debugHourEnabled" || f.Name == "editPreviewHour") continue;
                    var ft = f.FieldType;
                    if (ft != typeof(float) && ft != typeof(int) && ft != typeof(bool) && ft != typeof(Color) && !ft.IsEnum) continue;
                    if (!s_skyAllFields.Contains(f)) s_skyAllFields.Add(f);
                }
                return s_skyAllFields;
            }
        }
        private static bool IsWorldLook(SkyLookPreset p) { return p == SkyLookPreset.Mars || p == SkyLookPreset.JupiterIo; }

        private static System.Collections.Generic.List<System.Reflection.FieldInfo> SkyLookFields
        {
            get
            {
                if (s_skyLookFields != null) return s_skyLookFields;
                s_skyLookFields = new System.Collections.Generic.List<System.Reflection.FieldInfo>();
                var t = typeof(DM_IoSkyProfile);
                foreach (var name in SkyLookFieldNames)
                {
                    var f = t.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (f != null) s_skyLookFields.Add(f);
                }
                return s_skyLookFields;
            }
        }

        private static string SkyLookSerialize(object v)
        {
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            if (v is float fv) return fv.ToString("R", ci);
            if (v is int iv) return iv.ToString(ci);
            if (v is bool bv) return bv ? "1" : "0";
            if (v is Color c) return string.Format(ci, "{0:R},{1:R},{2:R},{3:R}", c.r, c.g, c.b, c.a);
            if (v is System.Enum) return v.ToString();
            return v?.ToString();
        }

        private static bool SkyLookTryParse(System.Reflection.FieldInfo f, string s, out object v)
        {
            v = null;
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            try
            {
                if (f.FieldType == typeof(float)) { v = float.Parse(s, ci); return true; }
                if (f.FieldType == typeof(int)) { v = int.Parse(s, ci); return true; }
                if (f.FieldType == typeof(bool)) { v = s == "1" || s.Equals("true", System.StringComparison.OrdinalIgnoreCase); return true; }
                if (f.FieldType == typeof(Color))
                {
                    var p = s.Split(',');
                    if (p.Length < 3) return false;
                    v = new Color(float.Parse(p[0], ci), float.Parse(p[1], ci), float.Parse(p[2], ci), p.Length > 3 ? float.Parse(p[3], ci) : 1f);
                    return true;
                }
                if (f.FieldType.IsEnum) { v = System.Enum.Parse(f.FieldType, s); return true; }
            }
            catch (System.Exception) { return false; }
            return false;
        }

        private void CaptureSkyLookCustomStash()
        {
            skyLookCustomStashNames = new System.Collections.Generic.List<string>();
            skyLookCustomStashValues = new System.Collections.Generic.List<string>();
            var moons = Moons;
            if (moons != null) for (int mi = 0; mi < moons.Length; mi++) if (moons[mi] != null)
            {
                skyLookCustomStashNames.Add("moon:" + mi);
                skyLookCustomStashValues.Add(SkyLookSerialize(moons[mi].sizeMultiplier));
            }
            foreach (var f in SkyAllFields)
            {
                string s = SkyLookSerialize(f.GetValue(this));
                if (s == null) continue;
                skyLookCustomStashNames.Add(f.Name);
                skyLookCustomStashValues.Add(s);
            }
        }

        private void RestoreSkyLookCustomStash()
        {
            if (!HasSkyLookCustomStash || skyLookCustomStashValues == null) return;
            for (int i = 0; i < skyLookCustomStashNames.Count && i < skyLookCustomStashValues.Count; i++)
            {
                string sn = skyLookCustomStashNames[i];
                if (sn != null && sn.StartsWith("moon:"))
                {
                    var ms = Moons; int mi;
                    if (ms != null && int.TryParse(sn.Substring(5), out mi) && mi >= 0 && mi < ms.Length && ms[mi] != null)
                        ms[mi].sizeMultiplier = float.Parse(skyLookCustomStashValues[i], System.Globalization.CultureInfo.InvariantCulture);
                    continue;
                }
                var f = typeof(DM_IoSkyProfile).GetField(sn, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (f == null || SkyAllFields.IndexOf(f) < 0) continue;
                if (SkyLookTryParse(f, skyLookCustomStashValues[i], out object v)) f.SetValue(this, v);
            }
        }

        private static bool SkyLookValuesEqual(object a, object b)
        {
            if (a is float fa && b is float fb) return Mathf.Abs(fa - fb) <= 1e-3f;
            if (a is Color ca && b is Color cb) return Mathf.Abs(ca.r - cb.r) + Mathf.Abs(ca.g - cb.g) + Mathf.Abs(ca.b - cb.b) + Mathf.Abs(ca.a - cb.a) <= 0.004f;
            return Equals(a, b);
        }

        /// <summary>Writes Subtle/Normal/Extreme look snapshots. Custom restores the last Custom stash. Orbit/time untouched.</summary>
        public void ApplySkyLookPreset(SkyLookPreset preset)
        {
            if (preset == SkyLookPreset.Custom)
            {
                if (skyLookAppliedPresetInternal != SkyLookPreset.Custom) RestoreSkyLookCustomStash();
                skyLookPreset = preset;
                skyLookAppliedPresetInternal = preset;
                return;
            }
            if (skyLookAppliedPresetInternal == SkyLookPreset.Custom) CaptureSkyLookCustomStash();
            ApplySkyLookAuthored(preset);
            skyLookPreset = preset;
            skyLookAppliedPresetInternal = preset;
        }

        public bool SkyLookMatchesPreset(SkyLookPreset preset)
        {
            if (preset == SkyLookPreset.Custom) return true;
            var probe = CreateInstance<DM_IoSkyProfile>();
            try
            {
                probe.ApplySkyLookAuthored(preset);
                if (IsWorldLook(preset))
                {
                    var a = probe.Moons; var b = Moons;
                    if (a != null && b != null) for (int mi = 0; mi < a.Length && mi < b.Length; mi++)
                        if (a[mi] != null && b[mi] != null && Mathf.Abs(a[mi].sizeMultiplier - b[mi].sizeMultiplier) > 1e-3f) return false;
                }
                foreach (var f in (IsWorldLook(preset) ? SkyAllFields : SkyLookFields))
                {
                    if (!SkyLookValuesEqual(f.GetValue(probe), f.GetValue(this))) return false;
                }
                return true;
            }
            finally
            {
                if (Application.isPlaying) Destroy(probe); else DestroyImmediate(probe);
            }
        }

        private void ApplySkyLookAuthored(SkyLookPreset preset)
        {
            if (IsWorldLook(preset)) { ApplySkyWorldLook(preset); return; }
            // Shared enables
            nightSkyEnabled = true;
            auroraEnabled = true;
            starsBehindSkyBodies = true;
            auroraOverSkyBodies = true;
            starsRotateWithClock = true;
            enableNightLight = true;
            enableNightShadow = true;
            eclipseFloorEnabled = true;
            eclipseShadowsEnabled = true;
            nightSkyDitherEnabled = false;
            nightDomeCoverTerrain = true;
            nightDomeFarFraction = 0.98f;
            nightDomeTerrainMargin = 500f;
            nightDomeMinRadius = 0f;
            milkyWayDayVisibility = 0f;
            auroraDayVisibility = 0f;
            starRotationDeg = 0f;
            nightShadowDirectionMode = 0;
            nightShadowCustomYawDeg = 200f;
            nightShadowCustomPitchDeg = 35f;

            if (preset == SkyLookPreset.Subtle)
            {
                // Quieter than Normal; MW/aurora at or below Anthony's lowered Custom (0.15 / 0.1).
                starDensity = 110f; starBrightness = 1.6f; starSize = 0.55f; starTwinkle = 0.2f; starColorVariation = 0.45f;
                milkyWayIntensity = 0.07f; milkyWayWidth = 0.16f; milkyWayDust = 0.55f;
                milkyWayCoreColor = new Color(1f, 0.85f, 0.7f, 1f); milkyWayEdgeColor = new Color(0.55f, 0.65f, 1f, 1f);
                milkyWayTiltDeg = 62f; milkyWayYawDeg = 35f; milkyWaySmoothness = 0.75f;
                nightFadeStartDeg = -8f; nightFadeEndDeg = 4f; nightSkyDayVisibility = 0f;
                starsBehindSkyBodies = true; auroraOverSkyBodies = true; starOcclusionMarginDeg = 0f; starOcclusionSoftDeg = 0.06f;
                auroraIntensity = 0.05f; auroraSpeed = 0.02f; auroraHeight = 0.25f; auroraCoverage = 0.4f; auroraSmoothness = 0.7f;
                auroraColorLow = new Color(0.2f, 0.85f, 0.7f, 1f); auroraColorHigh = new Color(0.75f, 0.25f, 0.5f, 1f);
                auroraMinElevDeg = 1f; auroraMaxElevDeg = 12f; auroraHeightVariationDeg = 4f; auroraHeightNoiseScale = 2.5f;
                auroraBottomSoftDeg = 2f; auroraTopSoftDeg = 6f; auroraAzimuthSpanDeg = 360f; auroraAzimuthCenterDeg = 0f;
                auroraAzimuthEdgeSoftDeg = 40f; auroraHeightDriftSpeed = 0.01f; auroraHeightBobDeg = 1f;
                nightLightGroundColor = new Color(0.28f, 0.18f, 0.12f, 1f);
                nightLightMidColor = new Color(0.4f, 0.38f, 0.48f, 1f);
                nightLightSkyColor = new Color(0.22f, 0.28f, 0.5f, 1f);
                nightLightAmount = 35f; nightExposureLimitMin = 4.5f; nightAmbientBoost = 0.3f;
                nightShadowStrength = 0.3f; nightShadowSoftnessDeg = 8f; nightShadowResolutionTier = 0; nightShadowMaxDistance = 80f;
                eclipseSunRemaining = 0.12f; eclipseAmbientMultiplier = 0.45f;
                jupiterShineColor = new Color(1f, 0.86f, 0.68f, 1f); jupiterShineLux = 15f; eclipseJupiterShineLux = 8f;
                jupiterEclipseRimLux = 200f; jupiterBrightness = 0.85f; jupiterLimbDarkening = 0.55f;
                jupiterRimColor = new Color(1f, 0.9f, 0.7f, 1f);
                eclipseMinAmbient = 0.4f; eclipseFillLux = 30f; eclipseExposureLimitMin = 3f;
                eclipseShadowStrength = 0.4f; eclipseShadowSoftnessDeg = 8f;
                nightSkyDitherAmount = 0.4f; nightSkySoftExpPower = 0.3f;
            }
            else if (preset == SkyLookPreset.Extreme)
            {
                // Brighter / more vibrant; MW+aurora above Custom but dither + smoother FBM keep bands in check.
                starDensity = 220f; starBrightness = 5f; starSize = 0.85f; starTwinkle = 0.45f; starColorVariation = 0.85f;
                milkyWayIntensity = 0.35f; milkyWayWidth = 0.26f; milkyWayDust = 0.9f;
                milkyWayCoreColor = new Color(1f, 0.88f, 0.72f, 1f); milkyWayEdgeColor = new Color(0.5f, 0.7f, 1f, 1f);
                milkyWayTiltDeg = -34.7f; milkyWayYawDeg = 32f; milkyWaySmoothness = 0.5f;
                nightFadeStartDeg = -2f; nightFadeEndDeg = 16f; nightSkyDayVisibility = 0.05f;
                starsBehindSkyBodies = true; auroraOverSkyBodies = true; starOcclusionMarginDeg = 0.02f; starOcclusionSoftDeg = 0.1f;
                auroraIntensity = 0.3f; auroraSpeed = 0.1f; auroraHeight = 0.65f; auroraCoverage = 0.7f; auroraSmoothness = 0.35f;
                auroraColorLow = new Color(0.15f, 1f, 0.35f, 1f); auroraColorHigh = new Color(1f, 0.25f, 0.35f, 1f);
                auroraMinElevDeg = -4f; auroraMaxElevDeg = 24f; auroraHeightVariationDeg = 10f; auroraHeightNoiseScale = 3.2f;
                auroraBottomSoftDeg = 8f; auroraTopSoftDeg = 6f; auroraAzimuthSpanDeg = 360f; auroraAzimuthCenterDeg = 0f;
                auroraAzimuthEdgeSoftDeg = 50f; auroraHeightDriftSpeed = 0.05f; auroraHeightBobDeg = 3f;
                nightLightGroundColor = new Color(0.55f, 0.75f, 0.55f, 1f);
                nightLightMidColor = new Color(0.9f, 0.5f, 1f, 1f);
                nightLightSkyColor = new Color(0.5f, 0.6f, 0.95f, 1f);
                nightLightAmount = 320f; nightExposureLimitMin = 1.5f; nightAmbientBoost = 1.4f;
                nightShadowStrength = 0.75f; nightShadowSoftnessDeg = 2.5f; nightShadowResolutionTier = 2; nightShadowMaxDistance = 180f;
                eclipseSunRemaining = 0.5f; eclipseAmbientMultiplier = 0.55f;
                jupiterShineColor = new Color(1f, 0.9f, 0.75f, 1f); jupiterShineLux = 80f; eclipseJupiterShineLux = 90f;
                jupiterEclipseRimLux = 2000f; jupiterBrightness = 0.75f; jupiterLimbDarkening = 0.65f;
                jupiterRimColor = new Color(1f, 0.92f, 0.7f, 1f);
                eclipseMinAmbient = 0.7f; eclipseFillLux = 200f; eclipseExposureLimitMin = 1.5f;
                eclipseShadowStrength = 0.85f; eclipseShadowSoftnessDeg = 5f;
                nightSkyDitherAmount = 0.85f; nightSkySoftExpPower = 0.28f;
            }
            else // Normal = true-to-life Io; MW/aurora at or below Custom (0.15 / 0.1).
            {
                starDensity = 160f; starBrightness = 2.4f; starSize = 0.7f; starTwinkle = 0.25f; starColorVariation = 0.55f;
                milkyWayIntensity = 0.12f; milkyWayWidth = 0.18f; milkyWayDust = 0.65f;
                milkyWayCoreColor = new Color(1f, 0.85f, 0.7f, 1f); milkyWayEdgeColor = new Color(0.55f, 0.65f, 1f, 1f);
                milkyWayTiltDeg = 62f; milkyWayYawDeg = 35f; milkyWaySmoothness = 0.65f;
                nightFadeStartDeg = -6f; nightFadeEndDeg = 8f; nightSkyDayVisibility = 0f;
                starsBehindSkyBodies = true; auroraOverSkyBodies = true; starOcclusionMarginDeg = 0f; starOcclusionSoftDeg = 0.08f;
                auroraIntensity = 0.08f; auroraSpeed = 0.03f; auroraHeight = 0.3f; auroraCoverage = 0.5f; auroraSmoothness = 0.6f;
                auroraColorLow = new Color(0.2f, 0.9f, 0.75f, 1f); auroraColorHigh = new Color(0.85f, 0.2f, 0.55f, 1f);
                auroraMinElevDeg = 0.5f; auroraMaxElevDeg = 16f; auroraHeightVariationDeg = 5f; auroraHeightNoiseScale = 3f;
                auroraBottomSoftDeg = 2.5f; auroraTopSoftDeg = 7f; auroraAzimuthSpanDeg = 360f; auroraAzimuthCenterDeg = 0f;
                auroraAzimuthEdgeSoftDeg = 35f; auroraHeightDriftSpeed = 0.02f; auroraHeightBobDeg = 1.5f;
                nightLightGroundColor = new Color(0.38f, 0.24f, 0.14f, 1f);
                nightLightMidColor = new Color(0.55f, 0.48f, 0.62f, 1f);
                nightLightSkyColor = new Color(0.32f, 0.42f, 0.78f, 1f);
                nightLightAmount = 55f; nightExposureLimitMin = 3.5f; nightAmbientBoost = 0.55f;
                nightShadowStrength = 0.5f; nightShadowSoftnessDeg = 5f; nightShadowResolutionTier = 1; nightShadowMaxDistance = 120f;
                eclipseSunRemaining = 0.05f; eclipseAmbientMultiplier = 0.3f;
                jupiterShineColor = new Color(1f, 0.86f, 0.68f, 1f); jupiterShineLux = 25f; eclipseJupiterShineLux = 15f;
                jupiterEclipseRimLux = 400f; jupiterBrightness = 1f; jupiterLimbDarkening = 0.45f;
                jupiterRimColor = new Color(1f, 0.92f, 0.75f, 1f);
                eclipseMinAmbient = 0.35f; eclipseFillLux = 40f; eclipseExposureLimitMin = 2.5f;
                eclipseShadowStrength = 0.55f; eclipseShadowSoftnessDeg = 6f;
                nightSkyDitherAmount = 0.55f; nightSkySoftExpPower = 0.28f;
            }
        }

        // 2026-10-30d: world looks. Jupiter (Io) = snapshot of the 30a-30c Io setup (Anthony's look + shadow cleanup, Jupiter 37.5 deg, moons 4x).
        // Mars = same base, Jupiter/moons/eclipse/aurora off, smaller dimmer sun (0.35 deg, ~43%), butterscotch sky tint, dusty fog.
        // 2026-10-30g: Jupiter (Io) = FULL profile snapshot of the pre-Mars setup (DM_IoSkyProfile.asset.pre20261030d) with Jupiter + moons on:
        // lighting, sun (lux/colour/temp/shadows/flare), orbit, sky, exposure/fog/post + 30a shadow cleanup. Mars starts from it and overrides.
        private static readonly string[] JupiterIoSnapshot =
        {
            "skyEnabled=1",
            "autoSpawnController=1",
            "onlyInScenesWithJupiter=1",
            "startHour=11.6",
            "disableOtherDirectionalLights=1",
            "sunIntensityMultiplier=2",
            "horizonFadeDegrees=11.78",
            "sunHorizonDimDegrees=3.66",
            "sunHorizonMinFactor=0.009",
            "sunHorizonDesaturate=0.35",
            "sunMaxLux=42621",
            "shadeFillEnabled=1",
            "shadeFillLux=1856",
            "shadeFillStrength=0.35", // 30l: was 1 (30i); 1856 lux fill washed out sun shadows
            "shadeFillElevationDeg=8.5",
            "shadeFillColor=0.87169814,0.81413317,0.8384384,1",
            "orbitPoleTiltDeg=-0.8",
            "reverseSkyMotion=0",
            "eclipseEnabled=1",
            "eclipseDurationHours=4",
            "eclipseFadeHours=1.33",
            "eclipseSunRemaining=0.858",
            "eclipseAmbientMultiplier=0.605",
            "sunHidesBehindJupiter=1",
            "ambientVolumePriority=90",
            "showJupiter=1",
            "showMoons=1",
            "jupiterAzimuthDeg=188",
            "jupiterElevationDeg=42.8",
            "jupiterAngularDiameterDeg=37.5",
            "skyDistance=6998",
            "jupiterSpinHours=10.9",
            "jupiterBrightness=1.48",
            "jupiterLimbDarkening=0.131",
            "jupiterRimColor=0.97853035,1,0.91886795,1",
            "jupiterEclipseRimLux=3412",
            "jupiterRimSoftness=4",
            "skyBodyEdgeSoft=0.035",
            "skyBodySphereSegments=64",
            "jupiterShineColor=0.7853827,0.87747157,0.93962264,1",
            "jupiterShineLux=103",
            "eclipseJupiterShineLux=129",
            "otherMoonsUseSkyMaterial=1",
            "otherMoonColor=0.24301174,0.31151927,0.3509434,1",
            "realtimeIndirectEnabled=1",
            "indirectVolumePriority=90",
            "ssgiEnabled=1",
            "ssgiQuality=3",
            "ssgiMaxRaySteps=64",
            "ssgiFullResolution=1",
            "ssgiDenoise=1",
            "ssgiHalfResDenoiser=1",
            "ssgiDenoiserRadius=0.431",
            "ssgiDepthThickness=0.11",
            "shadowFloorLift=0.113",
            "shadowFloorTint=0.19999999,0.15207545,0.18115325,1",
            "overrideGroundTint=1",
            "groundBounceTint=0.6901961,0.28429884,0.19215685,1",
            "ssaoIntensity=0.9",
            "ssaoDirectLightingStrength=0.472",
            "fixCameraVolumeMask=1",
            "indirectDiffuseMultiplier=1.2",
            "indirectReflectionMultiplier=1",
            "ambientModeDynamic=1",
            "exposureOverride=1",
            "exposureHistogram=1",
            "exposureLimitMin=3.9",
            "exposureLimitMax=15.9",
            "exposureHistogramLow=40",
            "exposureHistogramHigh=88",
            "exposureAdaptDarkToLight=2.07",
            "exposureAdaptLightToDark=1.3",
            "bloomIntensity=0.591",
            "bloomThreshold=0.94",
            "exposureCompensation=-0.01",
            "polishEnabled=1",
            "ssaoRadius=0.59",
            "ssaoMaxRadiusPixels=16",
            "contactShadowLength=0",
            "sunContactShadows=1",
            "microShadowOpacity=0.497",
            "shadowMaxDistance=228",
            "shadowCascadeSplit0=0.057",
            "fogMeanFreePath=663",
            "gradeContrast=-13.78",
            "splitToneStrength=0.291",
            "splitToneShadows=0.42,0.52,0.66,1",
            "splitToneHighlights=0.66,0.55,0.42,1",
            "vignetteIntensity=0.406",
            "eclipseFloorEnabled=1",
            "eclipseMinAmbient=0.893",
            "eclipseFillLux=373",
            "eclipseExposureLimitMin=2",
            "nightSkyEnabled=1",
            "starDensity=75",
            "starBrightness=3",
            "starSize=0.36",
            "starTwinkle=0.644",
            "starColorVariation=1",
            "milkyWayIntensity=0.035",
            "milkyWayWidth=0.177",
            "milkyWayDust=1",
            "milkyWayCoreColor=1,0.85,0.7,1",
            "milkyWayEdgeColor=0.55,0.65,1,1",
            "milkyWayTiltDeg=-34.7",
            "milkyWayYawDeg=32",
            "starRotationDeg=0",
            "starsRotateWithClock=1",
            "nightFadeStartDeg=-1.2",
            "nightFadeEndDeg=18.6",
            "enableNightLight=1",
            "nightLightGroundColor=0.58472764,0.82641506,0.63728845,1",
            "nightLightMidColor=0.934017,0.4901961,1,1",
            "nightLightSkyColor=0.5469348,0.6291979,0.93207544,1",
            "nightLightAmount=1128",
            "nightExposureLimitMin=0.5",
            "nightAmbientBoost=2.66",
            "enableNightShadow=1",
            "nightShadowStrength=0.55",
            "nightShadowSoftnessDeg=4",
            "nightShadowDirectionMode=0",
            "nightShadowCustomYawDeg=200",
            "nightShadowCustomPitchDeg=35",
            "nightShadowResolutionTier=1",
            "nightShadowMaxDistance=120",
            "nightSkyDayVisibility=0.094",
            "milkyWayDayVisibility=0",
            "milkyWaySmoothness=0.55",
            "nightSkyDitherEnabled=0",
            "nightSkyDitherAmount=0.55",
            "nightSkySoftExpPower=0.28",
            "starsBehindSkyBodies=1",
            "auroraOverSkyBodies=1",
            "starOcclusionMarginDeg=0",
            "starOcclusionSoftDeg=0.08",
            "editPreviewEnabled=1",
            "nightDomeFarFraction=0.98",
            "nightDomeCoverTerrain=1",
            "nightDomeTerrainMargin=500",
            "nightDomeMinRadius=0",
            "auroraEnabled=1",
            "auroraIntensity=0.4",
            "auroraColorLow=0.114575684,1,0,1",
            "auroraColorHigh=0.8980392,0.22878446,0.19999997,1",
            "auroraSpeed=0.047",
            "auroraHeight=0.723",
            "auroraCoverage=0.614",
            "auroraDayVisibility=0",
            "auroraSmoothness=0.313",
            "auroraMinElevDeg=-7.3",
            "auroraMaxElevDeg=21.1",
            "auroraHeightVariationDeg=13.1",
            "auroraHeightNoiseScale=3.63",
            "auroraBottomSoftDeg=21.1",
            "auroraTopSoftDeg=15.2",
            "auroraAzimuthSpanDeg=360",
            "auroraAzimuthCenterDeg=0",
            "auroraAzimuthEdgeSoftDeg=112.8",
            "auroraHeightDriftSpeed=0.078",
            "auroraHeightBobDeg=5.69",
            "disableVolumetricClouds=0",
            "eclipseShadowsEnabled=1",
            "eclipseShadowStrength=0.5",
            "eclipseShadowSoftnessDeg=3",
            "ssrEnabled=1",
            "ssrDepthThickness=0.08",
            "sunOcclusionSoftDeg=0.73",
            "ringSunBias=3.85",
            "ringEdgeGrowDeg=27.8",
            "ringMinFactor=0.052",
            "ringLightLux=107",
            "ringFlareIntensity=0.2",
            "dayFraction=0.783",
            "flareEnabled=1",
            "flareUseOcclusion=1",
            "flareOcclusionSamples=16",
            "flareIntensityScale=1",
            "flareMinElevationDeg=1",
            "flareElevationFadeDeg=16",
            "flareEclipseFade=1",
            "flareRingDuringEclipse=0.3",
            "flareOcclusionRadius=0.28",
            "eclipseDisableSsgi=1",
            "disableOtherSunFlares=1",
            "sunAngularDiameterDeg=0.5",
            "skyTintEnabled=0",
            "skyZenithTint=1,1,1,1",
            "skyHorizonTint=1,1,1,1",
            "skyHorizonZenithShift=0",
        };

        private void ApplySkyWorldLook(SkyLookPreset preset)
        {
            foreach (var kv in JupiterIoSnapshot)
            {
                int eq = kv.IndexOf('=');
                var f = typeof(DM_IoSkyProfile).GetField(kv.Substring(0, eq), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (f != null && SkyLookTryParse(f, kv.Substring(eq + 1), out object v)) f.SetValue(this, v);
            }
            var ms = Moons; if (ms != null) foreach (var mo in ms) if (mo != null) mo.sizeMultiplier = 4f;
            if (preset != SkyLookPreset.Mars) return;
            showJupiter = false; showMoons = false; eclipseEnabled = false;
            auroraEnabled = false; eclipseFloorEnabled = false; eclipseShadowsEnabled = false; ringFlareIntensity = 0f; flareRingDuringEclipse = 0f;
            sunAngularDiameterDeg = 0.35f; sunIntensityMultiplier = 0.86f; sunMaxLux = 18327f;
            skyTintEnabled = true; skyZenithTint = new Color(0.78f, 0.6f, 0.42f, 1f); skyHorizonTint = new Color(0.92f, 0.74f, 0.52f, 1f); skyHorizonZenithShift = 0.2f;
            groundBounceTint = new Color(0.62f, 0.38f, 0.22f, 1f); shadowFloorTint = new Color(0.22f, 0.16f, 0.12f, 1f);
            shadeFillColor = new Color(0.95f, 0.82f, 0.68f, 1f); shadeFillLux = 900f; shadeFillStrength = 0.6f; fogMeanFreePath = 400f;
            nightLightGroundColor = new Color(0.45f, 0.3f, 0.2f, 1f); nightLightMidColor = new Color(0.5f, 0.42f, 0.45f, 1f);
            nightLightSkyColor = new Color(0.35f, 0.4f, 0.6f, 1f); nightLightAmount = 300f;
        }

#if UNITY_EDITOR
        private bool _skyLookValidateQueued;

        private void OnEnable()
        {
            // Seed Custom stash from whatever is on the asset (Anthony's current look, incl. lowered MW/aurora).
            if (!HasSkyLookCustomStash && skyLookPreset == SkyLookPreset.Custom)
                CaptureSkyLookCustomStash();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying) { UnityEditor.EditorApplication.QueuePlayerLoopUpdate(); UnityEditor.SceneView.RepaintAll(); } // 31r: Debug Hour scrubs re-run the sky immediately
            if (_skyLookValidateQueued) return;
            if (skyLookPreset == SkyLookPreset.Custom && skyLookAppliedPresetInternal == SkyLookPreset.Custom)
                return; // Live fields are Custom; stash is captured when leaving Custom.
            _skyLookValidateQueued = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                _skyLookValidateQueued = false;
                ValidateSkyLookPreset();
            };
        }

        private void ValidateSkyLookPreset()
        {
            if (skyLookPreset != skyLookAppliedPresetInternal)
            {
                UnityEditor.Undo.RecordObject(this, "Apply Sky Look Preset");
                ApplySkyLookPreset(skyLookPreset);
                SaveSkyLookSoon();
            }
            else if (skyLookPreset != SkyLookPreset.Custom && !SkyLookMatchesPreset(skyLookPreset))
            {
                UnityEditor.Undo.RecordObject(this, "Sky Look -> Custom");
                skyLookPreset = SkyLookPreset.Custom;
                skyLookAppliedPresetInternal = SkyLookPreset.Custom;
                CaptureSkyLookCustomStash();
                SaveSkyLookSoon();
            }
        }

        private void SaveSkyLookSoon()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
            };
        }
#endif

        private void SetMoonSize(float s)
        {
            europa.sizeMultiplier = s; ganymede.sizeMultiplier = s; callisto.sizeMultiplier = s;
        }
    }
}

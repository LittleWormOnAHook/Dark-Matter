using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Project.World.Sky
{
    /// <summary>
    /// 2026-10-24aj/ak: realtime indirect light, no baking. Spawns a global Volume using a runtime clone of
    /// Resources/World/DM_IoPostProcessing (copy of BOTD Post Processing tuned for Io) above Gaia/BOTD (priority 90).
    /// Owns IndirectLightingController: diffuse = multiplier x lerp(1, eclipseAmbient, eclipseFactor); the Io sky
    /// controller mutes its eclipse ambient volume while this drives. Settings: DM_IoSkyProfile (Studio Sky & Orbit).
    /// 24ak fixes: Jupiter gate used GameObject.Find (inactive Jupiter => weight 0, cached); cameras whose
    /// Volume Layer Mask excludes this layer get it added at runtime (restored on exit).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMRealtimeIndirect : MonoBehaviour
    {
        public const string ProfileResourcePath = "World/DM_IoPostProcessing";
        public const string ProfileAssetPath = "Assets/_Project/Resources/World/DM_IoPostProcessing.asset";
        public const string LegacyProfileAssetPath = "Assets/_Project/Resources/World/DM_RealtimeIndirect.asset";
        public const string BotdSourcePath = "Assets/Gaia User Data/Sessions/DM Genesis/Lighting Profiles/HDRP Lighting Profiles/BOTD Post Processing.asset";

        private static DMRealtimeIndirect instance;
        public static DMRealtimeIndirect Instance => instance;
        public static bool IsDriving => instance != null && instance.isActiveAndEnabled && instance.volume != null && instance.volume.weight > 0f;

        private Volume volume;
        private VolumeProfile runtimeProfile;
        private GlobalIllumination gi;
        private VisualEnvironment env;
        private IndirectLightingController ilc;
        private Exposure exposure;
        private ScreenSpaceAmbientOcclusion ssao;
        private ShadowsMidtonesHighlights smh;
        private PhysicallyBasedSky pbs;
        private Bloom bloom;
        private ContactShadows contact;
        private MicroShadowing micro;
        private HDShadowSettings shadowSettings;
        private Fog fog;
        private ColorAdjustments colorAdj;
        private SplitToning split;
        private Vignette vignette;
        private HDAdditionalLightData contactSun;
        private bool contactSunWasOn;
        private bool groundTintPushed;
        private readonly Dictionary<HDAdditionalCameraData, int> patchedMasks = new Dictionary<HDAdditionalCameraData, int>();
        private float nextCamScan;
        private bool loggedState;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;
            DM_IoSkyProfile p = DM_IoSkyProfile.Live;
            if (p == null || !p.realtimeIndirectEnabled) return;
            if (FindAnyObjectByType<DMRealtimeIndirect>() != null) return;
            var go = new GameObject("DMRealtimeIndirect (auto)");
            DontDestroyOnLoad(go);
            go.AddComponent<DMRealtimeIndirect>();
        }

        private void OnEnable()
        {
            if (instance != null && instance != this) { enabled = false; return; }
            instance = this;
            DM_IoSkyProfile p = DM_IoSkyProfile.Live;
            VolumeProfile src = p != null && p.indirectVolumeProfile != null ? p.indirectVolumeProfile : Resources.Load<VolumeProfile>(ProfileResourcePath);
            runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            runtimeProfile.name = "DM_IoPostProcessing (runtime)";
            if (src != null)
                foreach (VolumeComponent c in src.components)
                    if (c != null) runtimeProfile.components.Add(Instantiate(c));
            if (!runtimeProfile.TryGet(out gi)) gi = runtimeProfile.Add<GlobalIllumination>(true);
            if (!runtimeProfile.TryGet(out env)) env = runtimeProfile.Add<VisualEnvironment>();
            if (!runtimeProfile.TryGet(out ilc)) ilc = runtimeProfile.Add<IndirectLightingController>();
            if (!runtimeProfile.TryGet(out exposure)) { exposure = runtimeProfile.Add<Exposure>(); exposure.active = false; }
            if (!runtimeProfile.TryGet(out ssao)) ssao = runtimeProfile.Add<ScreenSpaceAmbientOcclusion>();
            if (!runtimeProfile.TryGet(out smh)) smh = runtimeProfile.Add<ShadowsMidtonesHighlights>();
            if (!runtimeProfile.TryGet(out pbs)) { pbs = runtimeProfile.Add<PhysicallyBasedSky>(); pbs.active = false; }
            runtimeProfile.TryGet(out bloom);
            runtimeProfile.TryGet(out contact);
            runtimeProfile.TryGet(out shadowSettings);
            runtimeProfile.TryGet(out vignette);
            if (!runtimeProfile.TryGet(out micro)) micro = runtimeProfile.Add<MicroShadowing>();
            if (!runtimeProfile.TryGet(out fog)) { fog = runtimeProfile.Add<Fog>(); foreach (VolumeParameter prm in fog.parameters) prm.overrideState = false; }
            if (!runtimeProfile.TryGet(out colorAdj)) colorAdj = runtimeProfile.Add<ColorAdjustments>();
            if (!runtimeProfile.TryGet(out split)) split = runtimeProfile.Add<SplitToning>();
            exposureWasActive = exposure.active;
            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = runtimeProfile;
            Apply();
        }

        private bool exposureWasActive;

        private void OnDisable()
        {
            if (contactSun != null) contactSun.useContactShadow.@override = contactSunWasOn;
            contactSun = null;
            foreach (var kv in patchedMasks) if (kv.Key != null) kv.Key.volumeLayerMask = kv.Value;
            patchedMasks.Clear();
            if (volume != null) Destroy(volume);
            if (runtimeProfile != null)
            {
                foreach (VolumeComponent c in runtimeProfile.components) if (c != null) Destroy(c);
                Destroy(runtimeProfile);
            }
            volume = null; runtimeProfile = null;
            if (instance == this) instance = null;
        }

        private void LateUpdate() { Apply(); }

        private static void SetParam(object comp, string field, object value)
        {
            for (System.Type t = comp.GetType(); t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f == null) continue;
                if (f.GetValue(comp) is VolumeParameter vp)
                {
                    vp.overrideState = true;
                    PropertyInfo pi = vp.GetType().GetProperty("value");
                    if (pi != null && pi.CanWrite) pi.SetValue(vp, value);
                }
                return;
            }
        }

        private void Apply()
        {
            if (volume == null) return;
            DM_IoSkyProfile p = DM_IoSkyProfile.Live;
            if (p == null || !p.realtimeIndirectEnabled) { volume.weight = 0f; return; }

            volume.weight = 1f;
            volume.priority = p.indirectVolumePriority;
            if (p.fixCameraVolumeMask && Time.unscaledTime >= nextCamScan) { nextCamScan = Time.unscaledTime + 1f; PatchCameraMasks(); }

            gi.active = true;
            { DMIoSkyController skyG = DMIoSkyController.Instance; bool deepEclipse = p.eclipseDisableSsgi && skyG != null && skyG.isActiveAndEnabled && skyG.EclipseFactor > 0.6f; gi.enable.Override(p.ssgiEnabled && !deepEclipse); } // 24aq
            gi.tracing.Override(RayCastingMode.RayMarching);
            gi.quality.Override((int)p.ssgiQuality);
            SetParam(gi, "m_MaxRaySteps", p.ssgiMaxRaySteps);
            SetParam(gi, "fullResolutionSS", p.ssgiFullResolution);
            SetParam(gi, "m_DenoiseSS", p.ssgiDenoise);
            SetParam(gi, "m_HalfResolutionDenoiserSS", p.ssgiHalfResDenoiser);
            SetParam(gi, "m_DenoiserRadiusSS", p.ssgiDenoiserRadius);
            SetParam(gi, "depthBufferThickness", p.ssgiDepthThickness);
            gi.rayMiss.Override(RayMarchingFallbackHierarchy.Sky);

            env.active = true;
            env.skyAmbientMode.Override(p.ambientModeDynamic ? SkyAmbientMode.Dynamic : SkyAmbientMode.Static);

            float eclipseScale = 1f;
            float nightAmb = 0f;
            DMIoSkyController sky = DMIoSkyController.Instance;
            if (sky != null && sky.isActiveAndEnabled)
            {
                float amb = p.eclipseFloorEnabled ? Mathf.Max(p.eclipseAmbientMultiplier, p.eclipseMinAmbient) : p.eclipseAmbientMultiplier;
                eclipseScale = Mathf.Lerp(1f, amb, Mathf.Clamp01(sky.EclipseFactor));
                // 2026-10-26g: Night Ambient boost (lifts Indirect Diffuse with Ground/Mid/Sky night lighting)
                if (p.enableNightLight)
                    nightAmb = Mathf.Max(0f, p.nightAmbientBoost) * Mathf.Clamp01(sky.NightLightFactor);
            }
            ilc.active = true;
            ilc.indirectDiffuseLightingMultiplier.Override(p.indirectDiffuseMultiplier * eclipseScale + nightAmb);
            ilc.reflectionLightingMultiplier.Override(p.indirectReflectionMultiplier * eclipseScale + nightAmb * 0.5f);

            ssao.intensity.Override(p.ssaoIntensity);
            ssao.directLightingStrength.Override(p.ssaoDirectLightingStrength);

            smh.active = p.shadowFloorLift > 0f;
            Color st = p.shadowFloorTint;
            smh.shadows.Override(new Vector4(st.r, st.g, st.b, p.shadowFloorLift));

            // Ground bounce: only touch the PBS ground tint (the BOTD Environment volume keeps everything else).
            if (p.overrideGroundTint)
            {
                pbs.active = true;
                groundTintPushed = true;
                pbs.groundTint.Override(p.groundBounceTint);
                foreach (VolumeParameter prm in pbs.parameters) if (prm != pbs.groundTint) prm.overrideState = false;
            }
            else if (groundTintPushed) { pbs.active = false; groundTintPushed = false; }

            if (p.exposureOverride)
            {
                exposure.active = true;
                exposure.mode.Override(p.exposureHistogram ? ExposureMode.AutomaticHistogram : ExposureMode.Automatic);
                float expMin = p.exposureLimitMin;
                DMIoSkyController skyE = DMIoSkyController.Instance;
                if (skyE != null && skyE.isActiveAndEnabled)
                {
                    // 24ap: eclipse floor
                    if (p.eclipseFloorEnabled)
                        expMin = Mathf.Lerp(expMin, Mathf.Min(expMin, p.eclipseExposureLimitMin), Mathf.Clamp01(skyE.EclipseFactor));
                    // 2026-10-26g: night light floor (same nightFade* factor) so auto-exposure does not eat the fills
                    if (p.enableNightLight)
                        expMin = Mathf.Lerp(expMin, Mathf.Min(expMin, p.nightExposureLimitMin), Mathf.Clamp01(skyE.NightLightFactor));
                }
                exposure.limitMin.Override(Mathf.Min(expMin, p.exposureLimitMax));
                exposure.limitMax.Override(Mathf.Max(p.exposureLimitMin, p.exposureLimitMax));
                exposure.compensation.Override(p.exposureCompensation);
                exposure.histogramPercentages.Override(new Vector2(Mathf.Min(p.exposureHistogramLow, p.exposureHistogramHigh), Mathf.Max(p.exposureHistogramLow, p.exposureHistogramHigh)));
                exposure.histogramUseCurveRemapping.Override(false);
                exposure.adaptationMode.Override(AdaptationMode.Progressive);
                exposure.adaptationSpeedDarkToLight.Override(p.exposureAdaptDarkToLight);
                exposure.adaptationSpeedLightToDark.Override(p.exposureAdaptLightToDark);
                exposure.meteringMode.Override(MeteringMode.CenterWeighted);
            }
            else exposure.active = exposureWasActive; // keep BOTD exposure from the copy

            if (bloom != null)
            {
                bloom.intensity.Override(p.bloomIntensity);
                bloom.threshold.Override(p.bloomThreshold);
            }

            ApplyPolish(p);

            if (!loggedState)
            {
                loggedState = true;
                Debug.Log("[DM] RealtimeIndirect active: profile=" + runtimeProfile.components.Count + " comps, priority=" + volume.priority + ", diffuse=" + ilc.indirectDiffuseLightingMultiplier.value + ", SSGI=" + p.ssgiEnabled + ", patchedCams=" + patchedMasks.Count);
            }
        }

        // 2026-10-24an: subtle AAA polish on the runtime copy only (BOTD/Gaia untouched).
        private void ApplyPolish(DM_IoSkyProfile p)
        {
            bool on = p.polishEnabled;
            if (on)
            {
                ssao.radius.Override(p.ssaoRadius);
                SetParam(ssao, "m_MaximumRadiusInPixels", p.ssaoMaxRadiusPixels);
            }
            if (contact != null && on) { contact.enable.Override(true); contact.length.Override(p.contactShadowLength); }
            micro.active = on && p.microShadowOpacity > 0f;
            micro.enable.Override(true);
            micro.opacity.Override(p.microShadowOpacity);
            if (shadowSettings != null && on)
            {
                // 2026-10-26j: while night/eclipse owns the directional, use Night Shadow max distance.
                float dist = p.shadowMaxDistance;
                var sky = DMIoSkyController.Instance;
                if (sky != null && sky.isActiveAndEnabled && sky.NightShadowMaxDistanceOverride > 0f)
                    dist = sky.NightShadowMaxDistanceOverride;
                shadowSettings.maxShadowDistance.Override(dist);
                shadowSettings.cascadeShadowSplit0.Override(p.shadowCascadeSplit0);
            }
            fog.active = on && p.fogMeanFreePath > 0f;
            if (fog.active) fog.meanFreePath.Override(p.fogMeanFreePath);
            colorAdj.active = on;
            colorAdj.contrast.Override(p.gradeContrast);
            split.active = on && p.splitToneStrength > 0f;
            Color grey = new Color(0.5f, 0.5f, 0.5f, 1f);
            split.shadows.Override(Color.Lerp(grey, p.splitToneShadows, p.splitToneStrength));
            split.highlights.Override(Color.Lerp(grey, p.splitToneHighlights, p.splitToneStrength));
            if (vignette != null && on) vignette.intensity.Override(p.vignetteIntensity);

            Light sunLight = RenderSettings.sun;
            HDAdditionalLightData hd = sunLight != null ? sunLight.GetComponent<HDAdditionalLightData>() : null;
            if (hd != contactSun)
            {
                if (contactSun != null) contactSun.useContactShadow.@override = contactSunWasOn;
                contactSun = hd;
                if (hd != null) contactSunWasOn = hd.useContactShadow.@override;
            }
            if (contactSun != null) contactSun.useContactShadow.@override = on && p.sunContactShadows ? true : contactSunWasOn;
        }

        private void PatchCameraMasks()
        {
            int bit = 1 << gameObject.layer;
            foreach (Camera c in Camera.allCameras)
            {
                if (c == null || c.targetTexture != null) continue;
                var hd = c.GetComponent<HDAdditionalCameraData>();
                if (hd == null || (hd.volumeLayerMask.value & bit) != 0) continue;
                if (!patchedMasks.ContainsKey(hd)) patchedMasks[hd] = hd.volumeLayerMask.value;
                hd.volumeLayerMask = hd.volumeLayerMask.value | bit;
                Debug.Log("[DM] RealtimeIndirect: camera '" + c.name + "' volume mask " + patchedMasks[hd] + " excluded the indirect volume; added layer (restored on exit).");
            }
        }
    }
}

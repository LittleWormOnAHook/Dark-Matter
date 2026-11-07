using System.Collections.Generic;
using Project.World.Clock;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace Project.World.Sky
{
    /// <summary>
    /// 2026-10-24ai (Slice 1): Io sky + orbit on the 30 h Io clock.
    /// - Sun circles the sky once per Io day; local noon (15:00) the sun sits behind Jupiter (eclipse, default 10 h stylized).
    /// - Jupiter fixed in the sky (tidally locked), drawn at a fixed sky distance at its real ~19.5 deg apparent size.
    /// - Europa / Ganymede / Callisto orbit Jupiter (periods 2x / 4x / 9.43x Io), realistic apparent sizes, sun-lit phases.
    /// - Existing small moons under Jupiter keep orbiting, now on the Io clock.
    /// Runtime only: scene objects are adopted in Play (BOTD sun off, Day light driven, vendor Rotate off) and restored on exit.
    /// Nothing is saved. Profile: Resources/World/DM_IoSkyProfile (Studio: World > Sky &amp; Orbit).
    /// </summary>
    [DefaultExecutionOrder(-700)]
    [DisallowMultipleComponent]
    [ExecuteAlways] // 25o: live edit-mode preview (scene-safe: clones + DontSave, see Preview)
    public sealed class DMIoSkyController : MonoBehaviour
    {
        /// <summary>25o: edit mode = preview. Never mutates scene objects: no sun/RenderSettings/camera changes, Jupiter is a DontSave clone.</summary>
        private static bool Preview => !Application.isPlaying;
        private readonly List<Renderer> previewHidden = new List<Renderer>();
        // 25p: Studio preview status.
        public static bool PreviewActive, PreviewSunDriven; public static float PreviewHour; public static string PreviewSource = "-";

        // 25o: edit-mode preview safe destroy.
        private static void Kill(Object o) { if (o == null) return; if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }
        private const float IoOrbitKm = 421700f;
        private static readonly int SunDirId = Shader.PropertyToID("_DMSkySunDirWS");

        private static DMIoSkyController instance;
        public static DMIoSkyController Instance => instance;

        [Tooltip("Optional. Empty = DM_IoSkyProfile from Resources.")]
        public DM_IoSkyProfile profileOverride;

        public float HourOfDay { get; private set; }
        public float EclipseFactor { get; private set; }
        /// <summary>2026-10-26f: 1 at full night (sun elev &lt;= nightFadeStartDeg), 0 at day (elev &gt;= nightFadeEndDeg).</summary>
        public float NightLightFactor { get; private set; }
        /// <summary>2026-10-26j: true when Jupiter-shine (not the sun) owns the single HDRP directional shadow.</summary>
        public bool NightShadowOwnsDirectional { get; private set; }
        /// <summary>Active max shadow distance while night/eclipse owns the directional (else -1 = use polish day distance).</summary>
        public float NightShadowMaxDistanceOverride { get; private set; } = -1f;
        public Vector3 SunDirection { get; private set; }
        public Vector3 JupiterDirection { get; private set; }
        public float SunElevationDeg { get; private set; }

        private sealed class MoonRuntime
        {
            public DMIoMoonSettings settings;
            public Transform tr;
            public Material mat;
            public Vector3 dir;
            public float angularDiameterRad;
            public float distanceFactor = 1f;
        }

        private struct Restore
        {
            public Transform tr;
            public Transform parent;
            public Vector3 localPos;
            public Quaternion localRot;
            public Vector3 localScale;
            public bool active;
        }

        private bool acquired;
        private bool idle;
        private Transform skyRoot;
        private Light sun;
        private HDAdditionalCameraData nanCam; private bool nanCamPrev; // 24ar
        private LensFlareComponentSRP sunFlare; private float sunFlareBase = 1f; // 24aq
        public float SunVisibility { get; private set; } = 1f;
        private float sunBaseLux;
        private Color sunOriginalColor = Color.white; // 24al
        private Quaternion sunOriginalRot;
        private Light previousRenderSun;
        private Light jupiterShine;
        private Light nightLightSky, nightLightMid, nightLightGround; // 2026-10-26f trilight night ambient
        private LightShadows sunOriginalShadows = LightShadows.Soft; // 24av
        private Light shadeFill; // 24al
        private Transform jupiterBody;
        private Renderer jupiterRenderer;
        private float jupiterMeshRadius = 1f;
        private MeshFilter jupiterMeshFilter;
        private Mesh jupiterMeshOriginal;
        private static Mesh s_skyBodySphere;
        private static int s_skyBodySphereSeg;
        private Material jupiterMat;
        private Material otherMoonMat;
        private Restore bodyRestore;
        private readonly List<Transform> anchors = new List<Transform>();
        private readonly List<Quaternion> anchorRestore = new List<Quaternion>();
        private readonly List<Behaviour> disabledBehaviours = new List<Behaviour>();
        private readonly List<KeyValuePair<Renderer, Material[]>> materialRestore = new List<KeyValuePair<Renderer, Material[]>>();
        private readonly List<KeyValuePair<Renderer, ShadowCastingMode>> shadowRestore = new List<KeyValuePair<Renderer, ShadowCastingMode>>();
        private readonly List<MoonRuntime> moons = new List<MoonRuntime>();
        private Volume ambientVolume;
        private VolumeProfile ambientProfile;
        private IndirectLightingController ambientCtl;
        private Vector3 skyN;
        private Vector3 skyV;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;
            DM_IoSkyProfile p = DM_IoSkyProfile.Live;
            if (p == null || !p.skyEnabled || !p.autoSpawnController) return;
            if (FindAnyObjectByType<DMIoSkyController>() != null) return;
            var go = new GameObject("DMIoSkyController (auto)");
            DontDestroyOnLoad(go);
            go.AddComponent<DMIoSkyController>();
        }

        private int structSig;

        private static int StructureSig(DM_IoSkyProfile p)
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + (p.jupiterObjectName ?? "").GetHashCode();
                h = h * 31 + (p.skyBodyShader != null ? p.skyBodyShader.GetHashCode() : 0) /* 28k: was obsolete GetInstanceID */;
                h = h * 31 + (p.otherMoonsUseSkyMaterial ? 1 : 0);
                h = h * 31 + (p.onlyInScenesWithJupiter ? 1 : 0);
                h = h * 31 + (p.Moons != null ? p.Moons.Length : 0);
                h = h * 31 + Mathf.Clamp(p.skyBodySphereSegments, 24, 96);
                return h;
            }
        }

        private DM_IoSkyProfile Profile => profileOverride != null ? profileOverride : DM_IoSkyProfile.Live;

        private void OnEnable()
        {
            if (instance != null && instance != this) { Kill(this); return; }
            instance = this;
            // 2026-10-24ao: Start Hour, once per Play session; skipped if a save already restored the time.
            DM_IoSkyProfile sp = Profile;
            if (Application.isPlaying && sp != null) DMIoClock.ApplyStartHourOnce(sp.startHour);
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            acquired = false;
        }

        private void OnDisable()
        {
            if (instance == this) instance = null;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            Release();
        }

        private void OnActiveSceneChanged(Scene a, Scene b)
        {
            Release();
        }

        private void Update()
        {
            DM_IoSkyProfile p = Profile;
            if (p == null || !p.skyEnabled)
            {
                if (acquired) Release();
                return;
            }

            // 25o: structural settings (body names, shaders, moon list) changed live -> rebuild so Studio edits apply in Play.
            int sig = StructureSig(p);
            if (acquired && sig != structSig) Release();
            structSig = sig;
            if (!acquired) Acquire(p);
            if (idle) return;
            Simulate(p);
        }

        // ---------------------------------------------------------------- acquire / release

        private static T FindSceneComponentByName<T>(string objectName) where T : Component
        {
            if (string.IsNullOrEmpty(objectName)) return null;
            T[] all = FindObjectsByType<T>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].name == objectName && all[i].gameObject.scene.IsValid())
                    return all[i];
            return null;
        }

        private void DisableBehaviour(Behaviour b)
        {
            if (b == null || !b.enabled) return;
            b.enabled = false;
            disabledBehaviours.Add(b);
        }

        private void DisableVendorSpin(Transform t)
        {
            if (t == null) return;
            MonoBehaviour[] mbs = t.GetComponents<MonoBehaviour>();
            for (int i = 0; i < mbs.Length; i++)
            {
                if (mbs[i] == null) continue;
                string n = mbs[i].GetType().Name;
                if (n == "Rotate" || n == "PlanetRotate") DisableBehaviour(mbs[i]);
            }
        }

        private void Acquire(DM_IoSkyProfile p)
        {
            acquired = true;
            idle = false;

            Transform jupiterRoot = FindSceneComponentByName<Transform>(p.jupiterObjectName);
            if (jupiterRoot == null && p.onlyInScenesWithJupiter)
            {
                idle = true;
                return;
            }

            skyRoot = new GameObject(Preview ? "DM Io Sky (edit preview)" : "DM Io Sky (runtime)").transform;
            if (Preview) skyRoot.gameObject.hideFlags = HideFlags.HideAndDontSave;
            else SceneManager.MoveGameObjectToScene(skyRoot.gameObject, SceneManager.GetActiveScene());

            AcquireSun(p);
            AcquireJupiter(p, jupiterRoot);
            CreateJupiterShine(p);
            CreateShadeFill();
            CreateNightLights();
            CreateAmbientVolume(p);
            CreateMoons(p);
            if (Preview)
                foreach (Transform t in skyRoot.GetComponentsInChildren<Transform>(true))
                    t.gameObject.hideFlags = HideFlags.HideAndDontSave;
            if (Preview) // 25p: preview copies never carry a flare
                foreach (LensFlareComponentSRP lf in skyRoot.GetComponentsInChildren<LensFlareComponentSRP>(true)) lf.enabled = false;
            if (Preview) // 25o: preview lights never cast shadows (scene sun keeps the single cascade atlas slot)
                foreach (Light l in skyRoot.GetComponentsInChildren<Light>(true)) l.shadows = LightShadows.None;
        }

        private void AcquireSun(DM_IoSkyProfile p)
        {
            if (Preview)
            {
                // 25p: edit preview drives the scene sun non-destructively (rotation/intensity/colour/shadows cached, restored in
                // Release, which runs before scene save/close, Play and reload). No Undo/SetDirty -> scene not dirtied; no
                // RenderSettings / other-light / flare / camera changes.
                sun = FindSceneComponentByName<Light>(p.sunLightName);
                if (sun != null && sun.type != LightType.Directional) sun = null;
                if (sun == null && RenderSettings.sun != null && RenderSettings.sun.type == LightType.Directional) sun = RenderSettings.sun;
                if (sun == null) { PreviewSunDriven = false; sunBaseLux = 100000f; return; }
                sunBaseLux = sun.intensity; sunOriginalRot = sun.transform.rotation; sunOriginalColor = sun.color; sunOriginalShadows = sun.shadows;
                PreviewSunDriven = true;
                sunFlare = sun.GetComponent<LensFlareComponentSRP>(); // 25p: preview now fades/occludes the flare too (was left at full -> streak through terrain at night)
                if (sunFlare != null) sunFlareBase = sunFlare.intensity;
                CacheFlareOcclusion();
                return;
            }
            sun = FindSceneComponentByName<Light>(p.sunLightName);
            if (sun != null && sun.type != LightType.Directional) sun = null;
            if (sun == null && RenderSettings.sun != null && RenderSettings.sun.type == LightType.Directional) sun = RenderSettings.sun;
            if (sun == null)
            {
                var go = new GameObject("DM Io Sun");
                go.transform.SetParent(skyRoot, false);
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
                if (!go.TryGetComponent(out HDAdditionalLightData _)) go.AddComponent<HDAdditionalLightData>();
                sun.intensity = 1500f;
                sun.shadows = LightShadows.Soft;
            }

            if (!sun.gameObject.activeInHierarchy) sun.gameObject.SetActive(true);
            if (!sun.enabled) sun.enabled = true;
            sunBaseLux = sun.intensity;
            sunOriginalRot = sun.transform.rotation;
            sunOriginalColor = sun.color; // 24al
            sunOriginalShadows = sun.shadows; // 24av
            previousRenderSun = RenderSettings.sun;
            RenderSettings.sun = sun;

            if (p.disableOtherDirectionalLights)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Exclude);
                for (int i = 0; i < lights.Length; i++)
                    if (lights[i] != sun && lights[i].type == LightType.Directional && lights[i].enabled)
                        DisableBehaviour(lights[i]);
            }
            // 24aq: only the driven sun may flare; its flare is scaled by visibility vs Jupiter.
            if (p.disableOtherSunFlares)
            {
                LensFlareComponentSRP[] flares = FindObjectsByType<LensFlareComponentSRP>(FindObjectsInactive.Exclude);
                for (int i = 0; i < flares.Length; i++)
                {
                    if (flares[i] == null || !flares[i].enabled) continue;
                    Light fl = flares[i].GetComponent<Light>();
                    if (fl != sun && (fl == null || fl.type == LightType.Directional)) DisableBehaviour(flares[i]);
                }
            }
            sunFlare = sun.GetComponent<LensFlareComponentSRP>();
            // 24ar: Stop NaNs on the main camera while the Io sky runs (restored on release).
            Camera mc = Camera.main;
            nanCam = mc != null ? mc.GetComponent<HDAdditionalCameraData>() : null;
            if (nanCam != null) { nanCamPrev = nanCam.stopNaNs; nanCam.stopNaNs = true; }
            if (sunFlare != null) sunFlareBase = sunFlare.intensity;
            CacheFlareOcclusion();
        }

        // 25p: flare occlusion is driven from the profile; originals cached and restored in Release.
        private bool flareOccCached; private bool flareOccPrev; private float flareOccRadiusPrev; private uint flareOccSamplesPrev;
        private void CacheFlareOcclusion()
        {
            flareOccCached = false;
            if (sunFlare == null) return;
            flareOccPrev = sunFlare.useOcclusion; flareOccRadiusPrev = sunFlare.occlusionRadius; flareOccSamplesPrev = sunFlare.sampleCount;
            flareOccCached = true;
        }

        private void AcquireJupiter(DM_IoSkyProfile p, Transform root)
        {
            if (root == null) return;
            if (Preview)
            {
                // 25o: hide the scene Jupiter (forceRenderingOff is not serialized) and drive a DontSave clone.
                foreach (Renderer hr in root.GetComponentsInChildren<Renderer>(true))
                    if (hr != null && !hr.forceRenderingOff) { hr.forceRenderingOff = true; previewHidden.Add(hr); }
                GameObject clone = Instantiate(root.gameObject, skyRoot, false);
                clone.name = root.name + " (preview)";
                foreach (Transform t in clone.GetComponentsInChildren<Transform>(true)) t.gameObject.hideFlags = HideFlags.HideAndDontSave;
                foreach (Renderer cr in clone.GetComponentsInChildren<Renderer>(true)) cr.forceRenderingOff = false;
                root = clone.transform;
            }
            DisableVendorSpin(root);

            MeshRenderer best = null;
            MeshRenderer[] rs = root.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < rs.Length; i++)
                if (best == null || rs[i].transform.lossyScale.x > best.transform.lossyScale.x)
                    best = rs[i];
            if (best == null) return;

            jupiterBody = best.transform;
            jupiterRenderer = best;
            bodyRestore = new Restore
            {
                tr = jupiterBody, parent = jupiterBody.parent, localPos = jupiterBody.localPosition,
                localRot = jupiterBody.localRotation, localScale = jupiterBody.localScale, active = jupiterBody.gameObject.activeSelf
            };
            DisableVendorSpin(jupiterBody);
            MeshFilter mf = best.GetComponent<MeshFilter>();
            jupiterMeshFilter = mf;
            jupiterMeshOriginal = mf != null ? mf.sharedMesh : null;
            // 2026-10-26o: swap low-poly scene sphere for a high-res UV sphere (smooth silhouette at ~20-40 deg).
            int segs = Mathf.Clamp(p.skyBodySphereSegments, 24, 96);
            Mesh hi = GetSkyBodySphere(segs);
            if (mf != null && hi != null)
            {
                mf.sharedMesh = hi;
                jupiterMeshRadius = 0.5f; // Unity-style unit-diameter sphere
            }
            else if (mf != null && mf.sharedMesh != null)
            {
                Vector3 e = mf.sharedMesh.bounds.extents;
                jupiterMeshRadius = Mathf.Max(0.0001f, Mathf.Max(e.x, Mathf.Max(e.y, e.z)));
            }

            jupiterBody.SetParent(skyRoot, false);
            jupiterBody.gameObject.SetActive(true);

            // Existing small moons: anchors under the body with a vendor Rotate.
            foreach (Transform child in jupiterBody)
            {
                bool hasRotate = false;
                MonoBehaviour[] mbs = child.GetComponents<MonoBehaviour>();
                for (int i = 0; i < mbs.Length; i++)
                    if (mbs[i] != null && mbs[i].GetType().Name == "Rotate") hasRotate = true;
                if (!hasRotate) continue;
                DisableVendorSpin(child);
                anchors.Add(child);
                anchorRestore.Add(child.localRotation);
            }

            Shader sh = p.skyBodyShader != null ? p.skyBodyShader : Shader.Find("DM/IoSkyBody");
            if (sh != null && sh.isSupported)
            {
                jupiterMat = new Material(sh) { name = "DM Jupiter (runtime)" };
                Material src = best.sharedMaterial;
                if (src != null)
                {
                    foreach (string texName in new[] { "_BaseColorMap", "_MainTex", "_BaseMap" })
                    {
                        if (!src.HasProperty(texName)) continue;
                        Texture t = src.GetTexture(texName);
                        if (t == null) continue;
                        jupiterMat.SetTexture("_MainTex", t);
                        jupiterMat.SetTextureScale("_MainTex", src.GetTextureScale(texName));
                        jupiterMat.SetTextureOffset("_MainTex", src.GetTextureOffset(texName));
                        break;
                    }
                }
                SwapMaterial(best, jupiterMat);

                if (p.otherMoonsUseSkyMaterial)
                {
                    otherMoonMat = new Material(sh) { name = "DM Small Moon (runtime)" };
                    otherMoonMat.SetFloat("_ProcAmount", 1f);
                    otherMoonMat.SetColor("_Color", p.otherMoonColor);
                    otherMoonMat.SetColor("_ColorB", p.otherMoonColor * 0.6f);
                    otherMoonMat.SetFloat("_NoiseScale", 4f);
                    otherMoonMat.SetFloat("_Spots", 0.4f);
                    otherMoonMat.SetFloat("_RimSoftness", p.jupiterRimSoftness);
                    otherMoonMat.SetFloat("_EdgeSoft", p.skyBodyEdgeSoft);
                    for (int i = 0; i < anchors.Count; i++)
                        foreach (Renderer r in anchors[i].GetComponentsInChildren<Renderer>(true))
                            SwapMaterial(r, otherMoonMat);
                }
            }

            foreach (Renderer r in jupiterBody.GetComponentsInChildren<Renderer>(true))
            {
                shadowRestore.Add(new KeyValuePair<Renderer, ShadowCastingMode>(r, r.shadowCastingMode));
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private void SwapMaterial(Renderer r, Material m)
        {
            Material[] old = r.sharedMaterials;
            materialRestore.Add(new KeyValuePair<Renderer, Material[]>(r, old));
            var mats = new Material[Mathf.Max(1, old.Length)];
            for (int i = 0; i < mats.Length; i++) mats[i] = m;
            r.sharedMaterials = mats;
        }

        // 2026-10-24al: shadowless, specular-free fill from the anti-sun side = stand-in for bounce from sun-lit
        // ground behind the camera (SSGI can't see it). Keeps sun-facing views' shaded sides out of black.
        private void CreateShadeFill()
        {
            var go = new GameObject("DM Io Shade Fill");
            go.transform.SetParent(skyRoot, false);
            shadeFill = go.AddComponent<Light>();
            shadeFill.type = LightType.Directional;
            HDAdditionalLightData hd = go.GetComponent<HDAdditionalLightData>();
            if (hd == null) hd = go.AddComponent<HDAdditionalLightData>();
            hd.affectsVolumetric = false;
            hd.affectSpecular = false;
            shadeFill.shadows = LightShadows.None;
            shadeFill.intensity = 0f;
        }

        private void CreateJupiterShine(DM_IoSkyProfile p)
        {
            var go = new GameObject("DM Jupiter Shine");
            go.transform.SetParent(skyRoot, false);
            jupiterShine = go.AddComponent<Light>();
            jupiterShine.type = LightType.Directional;
            HDAdditionalLightData hd = go.GetComponent<HDAdditionalLightData>();
            if (hd == null) hd = go.AddComponent<HDAdditionalLightData>();
            // Never a celestial disc — softness uses angularDiameter with interactsWithSky off (26h/26j).
            hd.interactsWithSky = false;
            hd.angularDiameter = 0f;
            hd.flareSize = 0f;
            hd.flareMultiplier = 0f;
            hd.surfaceTexture = null;
            if (go.TryGetComponent(out LensFlareComponentSRP flare)) Kill(flare);
            jupiterShine.shadows = LightShadows.None;
            jupiterShine.color = p.jupiterShineColor;
            jupiterShine.intensity = 0f;
        }

        // 2026-10-26f: three shadowless specular-free fills (sky / mid / ground) that fade in with nightFade*.
        private void CreateNightLights()
        {
            nightLightSky = MakeNightFill("DM Night Light Sky");
            nightLightMid = MakeNightFill("DM Night Light Mid");
            nightLightGround = MakeNightFill("DM Night Light Ground");
        }

        private Light MakeNightFill(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(skyRoot, false);
            Light l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            HDAdditionalLightData hd = go.GetComponent<HDAdditionalLightData>();
            if (hd == null) hd = go.AddComponent<HDAdditionalLightData>();
            hd.affectsVolumetric = false;
            hd.affectSpecular = false;
            // 2026-10-26h: HDRP defaults (interactsWithSky=true, angularDiameter=0.5) drew the sky fill as a bright sun disc.
            hd.interactsWithSky = false;
            hd.angularDiameter = 0f;
            hd.flareSize = 0f;
            hd.flareMultiplier = 0f;
            hd.surfaceTexture = null;
            if (go.TryGetComponent(out LensFlareComponentSRP flare)) Kill(flare);
            l.shadows = LightShadows.None;
            l.intensity = 0f;
            return l;
        }

        private void ApplyNightLights(DM_IoSkyProfile p, Vector3 N, Vector3 J, Vector3 V)
        {
            float n = p.enableNightLight ? NightLightFactor : 0f;
            float amt = Mathf.Max(0f, p.nightLightAmount) * n;
            if (nightLightSky != null)
            {
                nightLightSky.enabled = amt > 0.001f;
                nightLightSky.color = p.nightLightSkyColor;
                nightLightSky.intensity = amt * 0.5f;
                nightLightSky.transform.rotation = Quaternion.LookRotation(-N, V.sqrMagnitude > 1e-4f ? V : Vector3.forward);
            }
            if (nightLightMid != null)
            {
                Vector3 midDir = Vector3.ProjectOnPlane(J, N);
                if (midDir.sqrMagnitude < 1e-4f) midDir = Vector3.ProjectOnPlane(V, N);
                if (midDir.sqrMagnitude < 1e-4f) midDir = Vector3.right;
                midDir.Normalize();
                nightLightMid.enabled = amt > 0.001f;
                nightLightMid.color = p.nightLightMidColor;
                nightLightMid.intensity = amt * 0.35f;
                nightLightMid.transform.rotation = Quaternion.LookRotation(-midDir, N);
            }
            if (nightLightGround != null)
            {
                nightLightGround.enabled = amt > 0.001f;
                nightLightGround.color = p.nightLightGroundColor;
                nightLightGround.intensity = amt * 0.25f;
                nightLightGround.transform.rotation = Quaternion.LookRotation(N, V.sqrMagnitude > 1e-4f ? V : Vector3.forward);
            }
        }

        private void CreateAmbientVolume(DM_IoSkyProfile p)
        {
            var go = new GameObject("DM Eclipse Ambient");
            go.transform.SetParent(skyRoot, false);
            ambientVolume = go.AddComponent<Volume>();
            ambientVolume.isGlobal = true;
            ambientVolume.priority = p.ambientVolumePriority;
            ambientProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            ambientProfile.name = "DM Eclipse Ambient (runtime)";
            ambientCtl = ambientProfile.Add<IndirectLightingController>(false);
            ambientVolume.sharedProfile = ambientProfile;
            ambientVolume.weight = 0f;
        }

        private void CreateMoons(DM_IoSkyProfile p)
        {
            Shader sh = p.skyBodyShader != null ? p.skyBodyShader : Shader.Find("DM/IoSkyBody");
            if (sh == null || !sh.isSupported) return;
            foreach (DMIoMoonSettings s in p.Moons)
            {
                if (s == null) continue;
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "DM Moon " + s.name;
                Collider c = go.GetComponent<Collider>();
                if (c != null) Kill(c);
                MeshFilter mmf = go.GetComponent<MeshFilter>();
                if (mmf != null) mmf.sharedMesh = GetSkyBodySphere(Mathf.Clamp(p.skyBodySphereSegments, 24, 96));
                go.transform.SetParent(skyRoot, false);
                Renderer r = go.GetComponent<Renderer>();
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
                var m = new Material(sh) { name = "DM Moon " + s.name + " (runtime)" };
                r.sharedMaterial = m;
                moons.Add(new MoonRuntime { settings = s, tr = go.transform, mat = m });
            }
        }

        private void Release()
        {
            if (!acquired) return;
            acquired = false;
            idle = false;

            for (int i = 0; i < previewHidden.Count; i++)
                if (previewHidden[i] != null) previewHidden[i].forceRenderingOff = false;
            previewHidden.Clear();
            for (int i = 0; i < disabledBehaviours.Count; i++)
                if (disabledBehaviours[i] != null) disabledBehaviours[i].enabled = true;
            disabledBehaviours.Clear();

            for (int i = 0; i < materialRestore.Count; i++)
                if (materialRestore[i].Key != null) materialRestore[i].Key.sharedMaterials = materialRestore[i].Value;
            materialRestore.Clear();
            for (int i = 0; i < shadowRestore.Count; i++)
                if (shadowRestore[i].Key != null) shadowRestore[i].Key.shadowCastingMode = shadowRestore[i].Value;
            shadowRestore.Clear();

            for (int i = 0; i < anchors.Count; i++)
                if (anchors[i] != null) anchors[i].localRotation = anchorRestore[i];
            anchors.Clear();
            anchorRestore.Clear();

            if (jupiterMeshFilter != null && jupiterMeshOriginal != null)
                jupiterMeshFilter.sharedMesh = jupiterMeshOriginal;
            jupiterMeshFilter = null;
            jupiterMeshOriginal = null;

            if (bodyRestore.tr != null)
            {
                if (bodyRestore.parent != null)
                {
                    bodyRestore.tr.SetParent(bodyRestore.parent, false);
                    bodyRestore.tr.localPosition = bodyRestore.localPos;
                    bodyRestore.tr.localRotation = bodyRestore.localRot;
                    bodyRestore.tr.localScale = bodyRestore.localScale;
                    bodyRestore.tr.gameObject.SetActive(bodyRestore.active);
                }
                else
                {
                    Kill(bodyRestore.tr.gameObject);
                }
            }
            bodyRestore = default;
            jupiterBody = null;
            jupiterRenderer = null;

            PreviewActive = false; PreviewSunDriven = false;
            if (sun != null && sun.transform.parent != skyRoot)
            {
                sun.intensity = sunBaseLux;
                sun.transform.rotation = sunOriginalRot;
                sun.color = sunOriginalColor;
                sun.shadows = sunOriginalShadows; // 24av
            }
            if (previousRenderSun != null) RenderSettings.sun = previousRenderSun;
            if (sunFlare != null) sunFlare.intensity = sunFlareBase;
            if (sunFlare != null && flareOccCached) { sunFlare.useOcclusion = flareOccPrev; sunFlare.occlusionRadius = flareOccRadiusPrev; sunFlare.sampleCount = flareOccSamplesPrev; }
            flareOccCached = false;
            if (nanCam != null) nanCam.stopNaNs = nanCamPrev;
            nanCam = null;
            sunFlare = null;
            sun = null;
            previousRenderSun = null;

            moons.Clear();
            if (skyRoot != null) Kill(skyRoot.gameObject);
            skyRoot = null;
            jupiterShine = null;
            nightLightSky = nightLightMid = nightLightGround = null;
            ambientVolume = null;
            ambientCtl = null;
            if (ambientProfile != null) Kill(ambientProfile);
            ambientProfile = null;
            if (jupiterMat != null) Kill(jupiterMat);
            if (otherMoonMat != null) Kill(otherMoonMat);
            jupiterMat = null;
            otherMoonMat = null;
        }

        // ---------------------------------------------------------------- simulation

        private static float Wrap(float x, float range)
        {
            x %= range;
            if (x > range * 0.5f) x -= range;
            if (x < -range * 0.5f) x += range;
            return x;
        }

        private float WarpSunHour(DM_IoSkyProfile p, float hour, float hoursPerDay)
        {
            // 25q: day/night fraction remap first; eclipse half-length scaled so it keeps its clock-hour length.
            hour = DM_IoSkyProfile.RemapDayHour(hour, hoursPerDay, p.dayFraction, out float k);
            float noon = hoursPerDay * 0.5f;
            float half = p.eclipseDurationHours * 0.5f * k;
            float g = hoursPerDay * Mathf.Max(0.5f, p.jupiterAngularDiameterDeg * 0.5f - 1.5f) / 360f; // 24aq: sun stays fully behind the disc (was limb + 0.6 deg)
            if (!p.eclipseEnabled || !p.sunHidesBehindJupiter || half <= g || half >= noon - 0.01f) return hour;
            float d = Wrap(hour - noon, hoursPerDay);
            float ad = Mathf.Abs(d);
            float dw = ad <= half ? ad * (g / half) : g + (ad - half) * ((noon - g) / (noon - half));
            return noon + Mathf.Sign(d) * dw;
        }

        private void Simulate(DM_IoSkyProfile p)
        {
            float hpd = Mathf.Max(1f, DMIoClock.HoursPerDayValue);
            double total = p.debugHourOverride >= 0f
                ? (DMIoClock.Day - 1) * (double)hpd + p.debugHourOverride
                : DMIoClock.TotalHoursContinuous;
            // 25p precedence: debug hour override (>=0) > edit Preview Hour (>=0, edit mode only) > clock.
            if (Preview && p.debugHourOverride < 0f && p.editPreviewHour >= 0f) total = (DMIoClock.Day - 1) * (double)hpd + p.editPreviewHour;
            if (Preview) { PreviewActive = true; PreviewHour = (float)(total % hpd); PreviewSource = p.debugHourOverride >= 0f ? "debug override" : (p.editPreviewHour >= 0f ? "Preview Hour" : "clock"); }
            float hour = (float)(total % hpd);
            HourOfDay = hour;
            float noon = hpd * 0.5f;

            // Sky basis: J = Jupiter, N = orbit pole, V = sky motion direction.
            float jEl = Mathf.Clamp(p.jupiterElevationDeg, -10f, 89.5f); // 24aw: avoid degenerate pole at zenith
            Vector3 J = Quaternion.Euler(-jEl, p.jupiterAzimuthDeg, 0f) * Vector3.forward;
            Vector3 N = Vector3.up - Vector3.Dot(Vector3.up, J) * J;
            N = Quaternion.AngleAxis(p.orbitPoleTiltDeg, J) * N.normalized;
            Vector3 V = Vector3.Cross(N, J).normalized;
            if (p.reverseSkyMotion) V = -V;
            JupiterDirection = J;
            skyN = N;
            skyV = V;

            // Eclipse factor (real clock), sun path (optionally warped so it stays behind Jupiter).
            float e = 0f;
            if (p.eclipseEnabled && p.eclipseDurationHours > 0f)
            {
                float d = Mathf.Abs(Wrap(hour - noon, hpd));
                float half = p.eclipseDurationHours * 0.5f;
                float f = p.eclipseFadeHours * 0.5f;
                e = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(half - f, half + f, d));
            }
            EclipseFactor = e;

            float tw = WarpSunHour(p, hour, hpd);
            float th = Mathf.PI + 2f * Mathf.PI * (tw - noon) / hpd;
            Vector3 S = (J * -Mathf.Cos(th) + V * -Mathf.Sin(th)).normalized;

            // 24aq: geometric occlusion of the sun by Jupiter + limb ring strength.
            float jr = p.jupiterAngularDiameterDeg * 0.5f;
            float sep = Vector3.Angle(S, J);
            { // 25q: night factor feeds the clock's Night Time Scale (smooth around the horizon)
                float el = Mathf.Asin(Mathf.Clamp(S.y, -1f, 1f)) * Mathf.Rad2Deg;
                DMIoClockProfile cp = DMIoClockProfile.Live;
                float bl = cp != null ? Mathf.Max(0.01f, cp.nightTransitionDeg) : 4f;
                DMIoClock.NightFactor = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-bl, bl, el));
            }
            float vis = 1f, ring = 0f;
            if (p.eclipseEnabled)
            {
                vis = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(jr - p.sunOcclusionSoftDeg, jr + p.sunOcclusionSoftDeg, sep));
                ring = sep < jr ? Mathf.Lerp(p.ringMinFactor, 1f, sep / Mathf.Max(0.01f, jr))
                                : 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(jr, jr + p.ringEdgeGrowDeg, sep));
            }
            SunVisibility = vis;
            SunDirection = S;
            SunElevationDeg = Mathf.Asin(Mathf.Clamp(S.y, -1f, 1f)) * Mathf.Rad2Deg;

            float baseLux = sunBaseLux * p.sunIntensityMultiplier;
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.LookRotation(-S, N);
                float hf = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-p.horizonFadeDegrees, p.horizonFadeDegrees, SunElevationDeg));
                float lux = baseLux * hf * Mathf.Lerp(1f, p.eclipseSunRemaining, e) * vis;
                // 2026-10-24al: low-sun dimming + colour desaturation near the horizon, then lux cap.
                float low = p.sunHorizonDimDegrees > 0f ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(SunElevationDeg / p.sunHorizonDimDegrees)) : 0f;
                lux *= Mathf.Lerp(1f, p.sunHorizonMinFactor, low);
                if (p.sunMaxLux > 0f) lux = Mathf.Min(lux, p.sunMaxLux);
                sun.intensity = Mathf.Max(lux, 0.01f); // 24ar: never exactly 0 (PBS sky / fog sky-colour guard)
                sun.color = Color.Lerp(sunOriginalColor, Color.white, p.sunHorizonDesaturate * low);
            }
            Shader.SetGlobalVector(SunDirId, new Vector4(S.x, S.y, S.z, baseLux));

            // Jupiter-shine: phase (full at midnight) + stylized eclipse glow.
            if (shadeFill != null)
            {
                Vector3 h = Vector3.ProjectOnPlane(S, N);
                if (h.sqrMagnitude < 1e-4f) h = Vector3.ProjectOnPlane(J, N);
                h.Normalize();
                float er = p.shadeFillElevationDeg * Mathf.Deg2Rad;
                Vector3 fwd = (h * Mathf.Cos(er) - N * Mathf.Sin(er)).normalized; // travels toward the sun side, lights anti-sun faces
                shadeFill.transform.rotation = Quaternion.LookRotation(fwd, N);
                float sunUp = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-p.horizonFadeDegrees, p.horizonFadeDegrees, SunElevationDeg));
                float floorLux = p.eclipseFloorEnabled ? p.eclipseFillLux * e : 0f; // 24ap eclipse readability
                shadeFill.enabled = p.shadeFillEnabled || floorLux > 0f;
                shadeFill.color = p.shadeFillColor;
                shadeFill.intensity = (p.shadeFillEnabled ? p.shadeFillLux * sunUp * Mathf.Lerp(1f, p.eclipseSunRemaining, e) : 0f) + floorLux;
            }

            // 2026-10-26f: night light factor (shared fade with stars / eclipse night sky).
            {
                float a = p.nightFadeStartDeg, b = p.nightFadeEndDeg;
                if (b < a) { float t = a; a = b; b = t; }
                NightLightFactor = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, SunElevationDeg));
            }
            ApplyNightLights(p, N, J, V);

            if (jupiterShine != null)
            {
                float lit = Mathf.Clamp01((1f - Vector3.Dot(S, J)) * 0.5f);
                // Direction: follow Jupiter (default) or custom yaw/pitch when Night Shadow Direction mode = Custom.
                if (p.enableNightShadow && p.nightShadowDirectionMode == 1 && NightLightFactor > 0.05f)
                {
                    Vector3 customFwd = Quaternion.Euler(-p.nightShadowCustomPitchDeg, p.nightShadowCustomYawDeg, 0f) * Vector3.forward;
                    jupiterShine.transform.rotation = Quaternion.LookRotation(-customFwd, Vector3.up);
                }
                else
                    jupiterShine.transform.rotation = Quaternion.LookRotation(-J, N);
                jupiterShine.color = p.jupiterShineColor;
                jupiterShine.intensity = p.jupiterShineLux * lit + p.eclipseJupiterShineLux * e + p.ringLightLux * ring;

                // 24av + 2026-10-26j: HDRP has ONE directional shadow cascade atlas.
                // Day -> sun. Eclipse / night -> Jupiter-shine. Edit preview -> never (scene sun keeps the slot).
                bool eclShadow = p.eclipseShadowsEnabled && e > 0.5f && vis < 0.5f;
                // Hand off when night factor is past mid-fade and sun is at/below the nightFade end elevation.
                bool nightShadow = p.enableNightShadow && NightLightFactor > 0.5f
                    && SunElevationDeg <= Mathf.Max(p.nightFadeEndDeg, p.nightFadeStartDeg);
                bool jupiterOwns = !Preview && (eclShadow || nightShadow);
                NightShadowOwnsDirectional = jupiterOwns;
                NightShadowMaxDistanceOverride = jupiterOwns
                    ? (nightShadow ? Mathf.Max(20f, p.nightShadowMaxDistance) : -1f)
                    : -1f;

                if (sun != null) sun.shadows = jupiterOwns ? LightShadows.None : sunOriginalShadows;
                jupiterShine.shadows = jupiterOwns ? LightShadows.Soft : LightShadows.None;

                if (jupiterShine.TryGetComponent(out HDAdditionalLightData jhd))
                {
                    jhd.interactsWithSky = false; // never a sky disc, even when angularDiameter > 0 for soft penumbra
                    jhd.flareSize = 0f;
                    jhd.flareMultiplier = 0f;
                    jhd.surfaceTexture = null;
                    if (jupiterOwns)
                    {
                        int tier = nightShadow ? Mathf.Clamp(p.nightShadowResolutionTier, 0, 3) : 1;
                        int[] res = { 256, 512, 1024, 2048 };
                        jhd.shadowResolution.useOverride = true;
                        jhd.shadowResolution.@override = res[tier];
                        float dim = 0f;
                        float soft = 4f;
                        if (eclShadow)
                        {
                            dim = Mathf.Max(dim, p.eclipseShadowStrength);
                            soft = p.eclipseShadowSoftnessDeg;
                        }
                        if (nightShadow)
                        {
                            dim = Mathf.Max(dim, p.nightShadowStrength * NightLightFactor);
                            soft = p.nightShadowSoftnessDeg;
                        }
                        jhd.shadowDimmer = Mathf.Clamp01(dim);
                        jhd.angularDiameter = Mathf.Max(0.1f, soft);
                    }
                    else
                    {
                        jhd.shadowDimmer = 1f;
                        jhd.angularDiameter = 0f;
                    }
                }
            }
            else
            {
                NightShadowOwnsDirectional = false;
                NightShadowMaxDistanceOverride = -1f;
            }
            if (sunFlare != null)
            {
                float hfF = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-p.horizonFadeDegrees, p.horizonFadeDegrees, SunElevationDeg));
                float limbOnly = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(jr * 0.85f, jr, sep)); // 24ar: ring flare only at the limb, never at Jupiter's centre
                // 25p: 0 below horizon+margin, faded by eclipse and night; depth occlusion so terrain/mountains hide it.
                float elevF = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(p.flareMinElevationDeg, p.flareMinElevationDeg + Mathf.Max(0.01f, p.flareElevationFadeDeg), SunElevationDeg));
                float eclF = 1f - Mathf.Clamp01(e) * p.flareEclipseFade;
                float ringF = p.ringFlareIntensity * ring * limbOnly * (1f - vis) * p.flareRingDuringEclipse;
                sunFlare.intensity = p.flareEnabled ? sunFlareBase * p.flareIntensityScale * hfF * elevF * (vis * eclF + ringF) : 0f;
                if (p.flareUseOcclusion != sunFlare.useOcclusion) sunFlare.useOcclusion = p.flareUseOcclusion;
                sunFlare.occlusionRadius = p.flareOcclusionRadius;
                sunFlare.sampleCount = (uint)Mathf.Clamp(p.flareOcclusionSamples, 1, 64);
            }

            // Dim ambient during eclipse.
            if (ambientVolume != null && ambientCtl != null)
            {
                ambientVolume.priority = p.ambientVolumePriority;
                ambientCtl.indirectDiffuseLightingMultiplier.Override(p.eclipseAmbientMultiplier);
                ambientCtl.reflectionLightingMultiplier.Override(p.eclipseAmbientMultiplier);
                ambientVolume.weight = DMRealtimeIndirect.IsDriving ? 0f : e; // 24aj: realtime indirect owns ambient when active
            }

            // Jupiter orientation + spin; small moons on the Io clock.
            float spin = 360f * (float)(total / Mathf.Max(0.05f, p.jupiterSpinHours) % 1.0);
            if (jupiterBody != null)
            {
                jupiterBody.rotation = Quaternion.LookRotation(J, N) * Quaternion.Euler(p.jupiterTiltEuler) * Quaternion.Euler(0f, -spin, 0f);
                for (int i = 0; i < anchors.Count; i++)
                {
                    if (anchors[i] == null) continue;
                    float period = p.otherMoonPeriodsHours != null && p.otherMoonPeriodsHours.Length > 0
                        ? p.otherMoonPeriodsHours[Mathf.Min(i, p.otherMoonPeriodsHours.Length - 1)]
                        : 8.4f;
                    float orbit = 360f * (float)(total / Mathf.Max(0.05f, period) % 1.0);
                    anchors[i].localRotation = Quaternion.Euler(0f, -orbit + spin, 0f);
                }
            }
            if (jupiterMat != null)
            {
                jupiterMat.SetFloat("_Brightness", p.jupiterBrightness);
                jupiterMat.SetFloat("_LimbDarkening", p.jupiterLimbDarkening);
                jupiterMat.SetColor("_RimColor", p.jupiterRimColor);
                jupiterMat.SetFloat("_RimLux", p.jupiterEclipseRimLux * Mathf.Max(e, ring) * ring);
                jupiterMat.SetFloat("_RimSunBias", p.ringSunBias);
                jupiterMat.SetFloat("_RimSoftness", p.jupiterRimSoftness);
                jupiterMat.SetFloat("_EdgeSoft", p.skyBodyEdgeSoft);
            }

            // Galilean moons: positions relative to Io in Io's co-rotating frame.
            double thIo = System.Math.PI + 2.0 * System.Math.PI * (total - noon) / hpd;
            double cio = System.Math.Cos(thIo), sio = System.Math.Sin(thIo);
            double px = IoOrbitKm * cio, py = IoOrbitKm * sio;
            for (int i = 0; i < moons.Count; i++)
            {
                MoonRuntime m = moons[i];
                DMIoMoonSettings s = m.settings;
                bool on = s.enabled;
                if (m.tr.gameObject.activeSelf != on) m.tr.gameObject.SetActive(on);
                if (!on) continue;

                double phi = s.startPhaseDeg * Mathf.Deg2Rad + 2.0 * System.Math.PI * total / (hpd * System.Math.Max(0.1, s.periodRatio));
                double qx = s.orbitRadiusKm * System.Math.Cos(phi), qy = s.orbitRadiusKm * System.Math.Sin(phi);
                double rx = qx - px, ry = qy - py;
                double cu = -(rx * cio + ry * sio); // toward Jupiter
                double cv = -rx * sio + ry * cio;   // along Io's motion
                double dist = System.Math.Max(1.0, System.Math.Sqrt(rx * rx + ry * ry));
                m.dir = (J * (float)(cu / dist) + V * (float)(cv / dist)).normalized;
                m.angularDiameterRad = 2f * Mathf.Atan((float)(s.diameterKm * 0.5 / dist)) * s.sizeMultiplier;
                m.distanceFactor = cu > IoOrbitKm ? 1.2f : 0.75f; // behind Jupiter -> drawn behind its limb; in front -> in front of its near face

                m.mat.SetFloat("_ProcAmount", 1f);
                m.mat.SetColor("_Color", s.colorA);
                m.mat.SetColor("_ColorB", s.colorB);
                m.mat.SetFloat("_NoiseScale", s.noiseScale);
                m.mat.SetFloat("_Lineae", s.lineae);
                m.mat.SetFloat("_Spots", s.craterSpots);
                m.mat.SetFloat("_Brightness", s.brightness);
                m.mat.SetFloat("_LimbDarkening", 0.1f);
                m.mat.SetFloat("_RimSoftness", p.jupiterRimSoftness);
                m.mat.SetFloat("_EdgeSoft", p.skyBodyEdgeSoft);
            }
        }

        /// <summary>2026-10-26o: lat-long UV sphere (diameter 1) for smooth sky-body silhouettes.</summary>
        private static Mesh GetSkyBodySphere(int lonSegments)
        {
            lonSegments = Mathf.Clamp(lonSegments, 24, 96);
            int latSegments = Mathf.Max(12, lonSegments / 2);
            if (s_skyBodySphere != null && s_skyBodySphereSeg == lonSegments) return s_skyBodySphere;

            int vCount = (latSegments + 1) * (lonSegments + 1);
            var verts = new Vector3[vCount];
            var norms = new Vector3[vCount];
            var uvs = new Vector2[vCount];
            int vi = 0;
            for (int y = 0; y <= latSegments; y++)
            {
                float v = y / (float)latSegments;
                float phi = (v - 0.5f) * Mathf.PI;
                float cy = Mathf.Sin(phi);
                float r = Mathf.Cos(phi);
                for (int x = 0; x <= lonSegments; x++)
                {
                    float u = x / (float)lonSegments;
                    float th = u * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Cos(th) * r, cy, Mathf.Sin(th) * r);
                    verts[vi] = n * 0.5f;
                    norms[vi] = n;
                    uvs[vi] = new Vector2(u, v);
                    vi++;
                }
            }
            int tCount = latSegments * lonSegments * 6;
            var tris = new int[tCount];
            int ti = 0;
            for (int y = 0; y < latSegments; y++)
            {
                for (int x = 0; x < lonSegments; x++)
                {
                    int i0 = y * (lonSegments + 1) + x;
                    int i1 = i0 + lonSegments + 1;
                    tris[ti++] = i0; tris[ti++] = i1; tris[ti++] = i0 + 1;
                    tris[ti++] = i0 + 1; tris[ti++] = i1; tris[ti++] = i1 + 1;
                }
            }
            var mesh = new Mesh { name = "DM SkyBody Sphere " + lonSegments };
            mesh.vertices = verts;
            mesh.normals = norms;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            if (s_skyBodySphere != null) Kill(s_skyBodySphere);
            s_skyBodySphere = mesh;
            s_skyBodySphereSeg = lonSegments;
            return mesh;
        }

        // Place sky bodies at a fixed distance around each rendering camera.
        private static readonly Vector4[] occ = new Vector4[8];
        private static readonly int OccId = Shader.PropertyToID("_DMSkyOcc");
        private static readonly int OccCountId = Shader.PropertyToID("_DMSkyOccCount");
        private static readonly int OccSoftId = Shader.PropertyToID("_DMSkyOccSoft");

        /// <summary>25o: Jupiter + moon discs for the night dome (stars never draw over sky bodies).</summary>
        private void PushStarOccluders(DM_IoSkyProfile p)
        {
            int n = 0;
            if (p != null && p.starsBehindSkyBodies && acquired && !idle)
            {
                // 2026-10-26n: keep cutout tight to the true disc (margin clamped).
                float marg = Mathf.Clamp(p.starOcclusionMarginDeg, -0.25f, 0.5f) * Mathf.Deg2Rad;
                if (jupiterBody != null && jupiterBody.gameObject.activeInHierarchy)
                {
                    Vector3 d = JupiterDirection.normalized;
                    occ[n++] = new Vector4(d.x, d.y, d.z, 0.5f * p.jupiterAngularDiameterDeg * Mathf.Deg2Rad + marg);
                }
                for (int i = 0; i < moons.Count && n < 8; i++)
                {
                    MoonRuntime m = moons[i];
                    if (m.tr == null || !m.settings.enabled) continue;
                    Vector3 d = m.dir.normalized;
                    occ[n++] = new Vector4(d.x, d.y, d.z, 0.5f * m.angularDiameterRad + marg);
                }
            }
            Shader.SetGlobalVectorArray(OccId, occ);
            Shader.SetGlobalFloat(OccCountId, n);
            // 2026-10-26n: clamp soft so a huge Studio value cannot reopen a dark halo ring.
            Shader.SetGlobalFloat(OccSoftId, p != null ? Mathf.Max(1e-4f, Mathf.Min(1f, p.starOcclusionSoftDeg) * Mathf.Deg2Rad) : 0.0014f);
        }

        private void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
        {
            PushStarOccluders(Profile);
            if (!acquired || idle || cam == null || cam.cameraType == CameraType.Preview) return;
            DM_IoSkyProfile p = Profile;
            if (p == null) return;
            Vector3 c = cam.transform.position;
            float D = Mathf.Min(p.skyDistance, cam.farClipPlane * 0.75f);

            if (jupiterBody != null)
            {
                float r = D * Mathf.Tan(0.5f * p.jupiterAngularDiameterDeg * Mathf.Deg2Rad);
                jupiterBody.position = c + JupiterDirection * D;
                jupiterBody.localScale = Vector3.one * (r / jupiterMeshRadius);
            }

            for (int i = 0; i < moons.Count; i++)
            {
                MoonRuntime m = moons[i];
                if (m.tr == null || !m.settings.enabled) continue;
                float dm = D * m.distanceFactor;
                m.tr.position = c + m.dir * dm;
                m.tr.localScale = Vector3.one * (2f * dm * Mathf.Tan(0.5f * m.angularDiameterRad));
            }
        }
    }
}

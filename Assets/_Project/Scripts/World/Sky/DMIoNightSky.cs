// 2026-10-24au: DM_IoNightSky runtime. Spawns (Play only, not saved) a camera-centred dome with DM/IoNightSky:
// stars + Milky Way + Io aurora. Fades by sun elevation / eclipse depth. Settings: DM_IoSkyProfile (Studio Sky & Orbit).
using Project.World.Clock;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.World.Sky
{
    [ExecuteAlways] // 25o: edit-mode preview (instance spawned HideAndDontSave by DMSkyEditPreview)
    public sealed class DMIoNightSky : MonoBehaviour
    {

        // 25o: edit-mode preview safe destroy.
        private static void Kill(Object o) { if (o == null) return; if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }
        private static DMIoNightSky instance;
        private Transform dome;
        private Material mat;
        private static readonly int RotId = Shader.PropertyToID("_DMNightStarRot");
        private static readonly int MWId = Shader.PropertyToID("_DMNightMWNormal");
        private static readonly int TimeId = Shader.PropertyToID("_DMNightTime");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (instance != null) return;
            var go = new GameObject("DM Io Night Sky (runtime)");
            go.hideFlags = HideFlags.DontSave;
            DontDestroyOnLoad(go);
            instance = go.AddComponent<DMIoNightSky>();
        }

        private void OnEnable() { RenderPipelineManager.beginCameraRendering += OnBeginCamera; }
        private void OnDisable() { RenderPipelineManager.beginCameraRendering -= OnBeginCamera; }
        private void OnDestroy()
        {
            if (mat != null) Kill(mat);
            if (dome != null) Kill(dome.gameObject);
            if (instance == this) instance = null;
        }

        private bool Ensure(DM_IoSkyProfile p)
        {
            if (dome != null) return true;
            Shader sh = p.nightSkyShader != null ? p.nightSkyShader : Shader.Find("DM/IoNightSky");
            if (sh == null) return false;
            Mesh sphere = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            if (sphere == null) return false;
            var go = new GameObject("DM_IoNightSky Dome");
            go.hideFlags = Application.isPlaying ? HideFlags.DontSave : HideFlags.HideAndDontSave;
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = sphere;
            var mr = go.AddComponent<MeshRenderer>();
            mat = new Material(sh) { name = "DM_IoNightSky (runtime)", hideFlags = HideFlags.DontSave };
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            mr.allowOcclusionWhenDynamic = false;
            dome = go.transform;
            return true;
        }

        private void LateUpdate()
        {
            DM_IoSkyProfile p = DM_IoSkyProfile.Live;
            bool on = p != null && (p.nightSkyEnabled || p.auroraEnabled);
            if (!on) { if (dome != null) dome.gameObject.SetActive(false); return; }
            if (!Ensure(p)) return;
            dome.gameObject.SetActive(true);

            DMIoSkyController sky = DMIoSkyController.Instance;
            float elev = sky != null && sky.isActiveAndEnabled ? sky.SunElevationDeg : -20f;
            float ecl = sky != null && sky.isActiveAndEnabled ? sky.EclipseFactor : 0f;
            float night = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(p.nightFadeStartDeg, p.nightFadeEndDeg, elev));
            float dark = Mathf.Max(night, ecl);
            float nightVis = p.nightSkyEnabled ? Mathf.Lerp(p.nightSkyDayVisibility, 1f, dark) : 0f;
            float auroraVis = p.auroraEnabled ? Mathf.Lerp(p.auroraDayVisibility, 1f, dark) : 0f;

            mat.SetFloat("_NightVis", nightVis);
            mat.SetFloat("_AuroraVis", auroraVis);
            mat.SetFloat("_StarDensity", p.starDensity);
            mat.SetFloat("_StarBrightness", p.starBrightness);
            mat.SetFloat("_StarSize", p.starSize);
            mat.SetFloat("_Twinkle", p.starTwinkle);
            mat.SetFloat("_StarColorVar", p.starColorVariation);
            mat.SetFloat("_MWIntensity", p.milkyWayIntensity);
            mat.SetFloat("_MWWidth", p.milkyWayWidth);
            mat.SetFloat("_MWDust", p.milkyWayDust);
            mat.SetColor("_MWColorA", p.milkyWayCoreColor);
            mat.SetColor("_MWColorB", p.milkyWayEdgeColor);
            mat.SetFloat("_AuroraIntensity", p.auroraIntensity);
            mat.SetColor("_AuroraColorLow", p.auroraColorLow);
            mat.SetColor("_AuroraColorHigh", p.auroraColorHigh);
            mat.SetFloat("_AuroraSpeed", p.auroraSpeed);
            mat.SetFloat("_AuroraHeight", p.auroraHeight);
            mat.SetFloat("_AuroraCoverage", p.auroraCoverage);
            mat.SetFloat("_AuroraMinElevDeg", p.auroraMinElevDeg);
            mat.SetFloat("_AuroraMaxElevDeg", p.auroraMaxElevDeg);
            mat.SetFloat("_AuroraHeightVariationDeg", p.auroraHeightVariationDeg);
            mat.SetFloat("_AuroraHeightNoiseScale", p.auroraHeightNoiseScale);
            mat.SetFloat("_AuroraBottomSoftDeg", p.auroraBottomSoftDeg);
            mat.SetFloat("_AuroraTopSoftDeg", p.auroraTopSoftDeg);
            mat.SetFloat("_AuroraAzimuthSpanDeg", p.auroraAzimuthSpanDeg);
            mat.SetFloat("_AuroraAzimuthCenterDeg", p.auroraAzimuthCenterDeg);
            mat.SetFloat("_AuroraAzimuthEdgeSoftDeg", p.auroraAzimuthEdgeSoftDeg);
            mat.SetFloat("_AuroraHeightDriftSpeed", p.auroraHeightDriftSpeed);
            mat.SetFloat("_AuroraHeightBobDeg", p.auroraHeightBobDeg);

            float hpd = Mathf.Max(1f, DMIoClock.HoursPerDayValue);
            float spin = p.starsRotateWithClock ? 360f * (float)(DMIoClock.TotalHoursContinuous / hpd % 1.0) : 0f;
            Quaternion rot = Quaternion.AngleAxis(spin, Vector3.up) * Quaternion.Euler(0f, p.starRotationDeg + p.jupiterAzimuthDeg - 200f, 0f); // 24aw: follows Sky Rotation
            Shader.SetGlobalMatrix(RotId, Matrix4x4.Rotate(Quaternion.Inverse(rot)));
            Vector3 mw = Quaternion.Euler(p.milkyWayTiltDeg, p.milkyWayYawDeg, 0f) * Vector3.up;
            Shader.SetGlobalVector(MWId, mw);
            Shader.SetGlobalFloat(TimeId, Time.time);
        }

        public static float LastDomeRadius, LastTerrainExtent; public static bool LastDomeCapped;
        private static Bounds tbCache; private static bool tbValid; private static float tbTime = -10f;
        private static bool TerrainBounds(out Bounds b)
        {
            float now = Time.realtimeSinceStartup;
            if (now - tbTime > 1f || now < tbTime)
            {
                tbTime = now; tbValid = false;
                foreach (Terrain t in Terrain.activeTerrains)
                {
                    if (t == null || t.terrainData == null) continue;
                    var tb = new Bounds(t.GetPosition() + t.terrainData.size * 0.5f, t.terrainData.size);
                    if (!tbValid) { tbCache = tb; tbValid = true; } else tbCache.Encapsulate(tb);
                }
                LastTerrainExtent = tbValid ? tbCache.size.magnitude : 0f;
            }
            b = tbCache; return tbValid;
        }
        private static float FarthestCorner(Bounds b, Vector3 c)
        {
            Vector3 d = new Vector3(Mathf.Max(Mathf.Abs(c.x - b.min.x), Mathf.Abs(c.x - b.max.x)), Mathf.Max(Mathf.Abs(c.y - b.min.y), Mathf.Abs(c.y - b.max.y)), Mathf.Max(Mathf.Abs(c.z - b.min.z), Mathf.Abs(c.z - b.max.z)));
            return d.magnitude;
        }

        private void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
        {
            if (dome == null || cam == null || cam.cameraType == CameraType.Preview) return;
            DM_IoSkyProfile lp = DM_IoSkyProfile.Live;
            // 25o: was 0.9 far = exactly the far-side moon distance (1.2 x 0.75 far) -> z-fight; now behind every sky body.
            float R = cam.farClipPlane * (lp != null ? Mathf.Clamp(lp.nightDomeFarFraction, 0.92f, 0.995f) : 0.98f);
            if (lp != null)
            {
                // 25o: cover the whole tiled world (all active terrains) + margin; never past the far clip.
                float need = lp.nightDomeMinRadius;
                if (lp.nightDomeCoverTerrain && TerrainBounds(out Bounds tb)) need = Mathf.Max(need, FarthestCorner(tb, cam.transform.position) + lp.nightDomeTerrainMargin);
                R = Mathf.Clamp(Mathf.Max(R, need), 1f, cam.farClipPlane * 0.995f);
                LastDomeRadius = R; LastDomeCapped = need > cam.farClipPlane * 0.995f;
            }
            dome.position = cam.transform.position;
            dome.rotation = Quaternion.Euler(0f, lp != null ? lp.jupiterAzimuthDeg - 200f : 0f, 0f); // 24aw: aurora frame follows Sky Rotation
            dome.localScale = Vector3.one * (2f * R);
        }
    }
}
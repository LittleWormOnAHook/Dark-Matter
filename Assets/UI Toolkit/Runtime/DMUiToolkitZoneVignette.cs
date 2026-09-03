using Project.Core;
using Project.Survival;
using Project.Survival.Exposure;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Fullscreen zone-edge vignette plus a combat hit flash.
    /// Stays up when tilde hides gameplay HUD.
    /// </summary>
    [DefaultExecutionOrder(-370)]
    [DisallowMultipleComponent]
    public class DMUiToolkitZoneVignette : MonoBehaviour
    {
        private const float ZoneAlphaMin = 0.1f;
        private const float ZoneAlphaMax = 0.6f;
        private const float ZoneOpacityLerp = 4.2f;
        private const float ZoneExitFadeSeconds = 0.55f;
        private const float HitPeakAlpha = 0.5f;
        private const float HitHold = 0.08f;
        private const float HitFade = 0.38f;


        private static float ResolvedZoneAlphaMin
        {
            get
            {
                DMUiToolkitConfig config = DMUiToolkitConfig.Instance;
                return config != null ? config.zoneVignetteAlphaMin : ZoneAlphaMin;
            }
        }

        private static float ResolvedZoneAlphaMax
        {
            get
            {
                DMUiToolkitConfig config = DMUiToolkitConfig.Instance;
                return config != null ? config.zoneVignetteAlphaMax : ZoneAlphaMax;
            }
        }

        private static float ResolvedHitPeakAlpha
        {
            get
            {
                DMUiToolkitConfig config = DMUiToolkitConfig.Instance;
                return config != null ? config.damageVignetteAlpha : HitPeakAlpha;
            }
        }

        private static DMUiToolkitZoneVignette instance;

        private UIDocument document;
        private VisualElement root;
        private VisualElement zoneLayer;
        private VisualElement damageLayer;
        private bool bound;

        private Texture2D texCold;
        private Texture2D texHeat;
        private Texture2D texSulfur;
        private Texture2D texRad;
        private Texture2D texMixed;
        private Texture2D texDamage;
        private Texture2D lastZoneTex;
        private float zoneOpacity;
        private float zoneOpacityTarget;

        private SurvivalStats survivalStats;
        private ExposureController exposure;
        private float hitUntil;
        private float hitStarted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isPlaying || !DMUiToolkitConfig.IsEnabled)
                return;

            EnsureHost();
        }

        public static DMUiToolkitZoneVignette EnsureHost()
        {
            if (instance != null)
                return instance;

            UIDocument doc = DMUiToolkitOverlayDocument.Ensure(
                DMUiToolkitOverlayDocument.VignetteName,
                DMUiToolkitOverlayDocument.VignetteUxml,
                DMUiToolkitOverlayDocument.VignetteUss,
                DMUiToolkitOverlayDocument.VignetteSort);
            if (doc == null)
                return null;

            DMUiToolkitZoneVignette host = doc.GetComponent<DMUiToolkitZoneVignette>();
            if (host == null)
                host = doc.gameObject.AddComponent<DMUiToolkitZoneVignette>();

            host.document = doc;
            host.BindTree();
            host.LoadTextures();
            return host;
        }

        public static void NotifyCombatHit()
        {
            DMUiToolkitZoneVignette host = EnsureHost();
            if (host == null)
                return;

            host.hitStarted = Time.unscaledTime;
            host.hitUntil = host.hitStarted + HitHold + HitFade;
        }

        private void Awake()
        {
            instance = this;
            if (document == null)
                document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            instance = this;
            BindTree();
            LoadTextures();
            SubscribeDamage();
        }

        private void OnDisable()
        {
            UnsubscribeDamage();
        }

        private void OnDestroy()
        {
            UnsubscribeDamage();
            if (instance == this)
                instance = null;
        }

        private void LateUpdate()
        {
            if (!bound)
                BindTree();
            if (!bound)
                return;

            TickZone();
            TickHit();
        }

        private void BindTree()
        {
            if (document == null)
                document = GetComponent<UIDocument>();
            if (document == null)
                return;

            VisualElement tree = document.rootVisualElement;
            if (tree == null)
                return;

            root = tree.Q<VisualElement>("vignette-root") ?? tree;
            DMUiToolkitOverlayDocument.ApplyIgnorePicking(root);
            zoneLayer = tree.Q<VisualElement>("zone-vignette");
            damageLayer = tree.Q<VisualElement>("damage-vignette");
            bound = zoneLayer != null && damageLayer != null;
        }

        private void LoadTextures()
        {
            if (texCold == null)
                texCold = Resources.Load<Texture2D>("Vignettes/Vignette_Cold_Frost");
            if (texHeat == null)
                texHeat = Resources.Load<Texture2D>("Vignettes/Vignette_Heat_Red");
            if (texSulfur == null)
                texSulfur = Resources.Load<Texture2D>("Vignettes/Vignette_Sulfur_Yellow");
            if (texRad == null)
                texRad = Resources.Load<Texture2D>("Vignettes/Vignette_Radiation_Green");
            if (texMixed == null)
                texMixed = Resources.Load<Texture2D>("Vignettes/Vignette_Mixed_Purple");
            if (texDamage == null)
                texDamage = Resources.Load<Texture2D>("Vignettes/Vignette_Damage_Hit");

            if (damageLayer != null && texDamage != null)
                damageLayer.style.backgroundImage = new StyleBackground(texDamage);
        }

        private void ResolvePlayer()
        {
            if (survivalStats != null && exposure != null)
                return;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
                return;

            if (survivalStats == null)
                survivalStats = player.GetComponent<SurvivalStats>();
            if (exposure == null)
                exposure = player.GetComponent<ExposureController>();
            SubscribeDamage();
        }

        private void SubscribeDamage()
        {
            ResolvePlayer();
            if (survivalStats == null)
                return;

            survivalStats.OnDamaged -= HandleDamaged;
            survivalStats.OnDamaged += HandleDamaged;
        }

        private void UnsubscribeDamage()
        {
            if (survivalStats == null)
                return;

            survivalStats.OnDamaged -= HandleDamaged;
        }

        private void HandleDamaged(float damage)
        {
            if (damage <= 0f)
                return;

            NotifyCombatHit();
        }

        private void TickZone()
        {
            if (zoneLayer == null)
                return;

            if (!GameSession.HasStarted)
            {
                zoneOpacity = 0f;
                zoneOpacityTarget = 0f;
                zoneLayer.style.opacity = 0f;
                return;
            }

            ResolvePlayer();
            Texture2D tex = null;
            float spatial = 0f;
            ExposureZoneVolume active = null;
            if (exposure != null)
                ResolveStrongestZone(exposure, out tex, out spatial, out active);

            if (tex != null && active != null)
            {
                if (tex != lastZoneTex)
                {
                    zoneLayer.style.backgroundImage = new StyleBackground(tex);
                    lastZoneTex = tex;
                }

                float fade01 = Mathf.Clamp01(Mathf.InverseLerp(0.12f, 1f, spatial));
                float min = active.OverlayAlphaMin;
                float max = active.OverlayAlphaMax;
                zoneOpacityTarget = Mathf.Lerp(min, max, fade01);
            }
            else
            {
                zoneOpacityTarget = 0f;
            }

            float dt = Time.unscaledDeltaTime;
            float rate = zoneOpacityTarget < zoneOpacity - 0.001f
                ? (1f / ZoneExitFadeSeconds)
                : ZoneOpacityLerp;
            zoneOpacity = Mathf.MoveTowards(zoneOpacity, zoneOpacityTarget, dt * rate);
            zoneLayer.style.opacity = zoneOpacity;
        }

        private void ResolveStrongestZone(
            ExposureController controller,
            out Texture2D tex,
            out float spatial,
            out ExposureZoneVolume active)
        {
            tex = null;
            spatial = 0f;
            active = null;
            var zones = controller.ActiveZones;
            if (zones == null || zones.Count == 0)
                return;

            Vector3 pos = controller.transform.position;
            for (int i = 0; i < zones.Count; i++)
            {
                ExposureZoneVolume zone = zones[i];
                if (zone == null || zone.Profile == null || !zone.OverlayEnabled)
                    continue;

                Texture2D mapped = zone.OverlayTexture != null
                    ? zone.OverlayTexture
                    : TextureForKind(zone.Profile.zoneKind);
                if (mapped == null)
                    continue;

                float s = zone.EvaluateSpatialIntensity(pos);
                if (s > spatial)
                {
                    spatial = s;
                    tex = mapped;
                    active = zone;
                }
            }
        }

        private Texture2D TextureForKind(ExposureZoneKind kind)
        {
            switch (kind)
            {
                case ExposureZoneKind.ThermalCold:
                    return texCold;
                case ExposureZoneKind.ThermalHeat:
                case ExposureZoneKind.VolcanoCaldera:
                    return texHeat;
                case ExposureZoneKind.SulfurField:
                    return texSulfur;
                case ExposureZoneKind.RadiationFlat:
                    return texRad;
                case ExposureZoneKind.MixedHazard:
                    return texMixed;
                default:
                    return null;
            }
        }

        private void TickHit()
        {
            if (damageLayer == null)
                return;

            if (Time.unscaledTime >= hitUntil)
            {
                damageLayer.style.opacity = 0f;
                return;
            }

            float t = Time.unscaledTime - hitStarted;
            float alpha;
            if (t <= HitHold)
                alpha = ResolvedHitPeakAlpha;
            else
                alpha = ResolvedHitPeakAlpha * (1f - Mathf.Clamp01((t - HitHold) / HitFade));

            damageLayer.style.opacity = alpha;
        }
    }
}

using Project.UI;
using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>
    /// Swivel Alarm sphere: scan (yellow), track (red), fire-range (green) via HDRP emission + base color.
    /// Track and fire modes blink faster as the engaged target closes; solid while actively firing.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DMTurretRuntime))]
    public sealed class DMTurretAlarmLight : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");
        private static readonly int EmissiveColorLdrId = Shader.PropertyToID("_EmissiveColorLDR");
        private static readonly int UseEmissiveIntensityId = Shader.PropertyToID("_UseEmissiveIntensity");

        [SerializeField] private Renderer targetRenderer;
        [SerializeField] [Min(0f)] private float emissionIntensity = 5f;
        [SerializeField] private bool useProfileColors = true;

        [Header("Proximity blink (track / fire, not while shooting)")]
        [Tooltip("Blink rate at the outer edge of the current mode band (track: track range, fire: fire range).")]
        [SerializeField] [Min(0f)] private float rimBlinkHz = 0.85f;
        [Tooltip("Blink rate when the target is at the inner edge (track: fire range, fire: point-blank).")]
        [SerializeField] [Min(0f)] private float centerBlinkHz = 8f;
        [Tooltip("How dark the blink trough goes. 0 = fully off, 1 = no blink amplitude.")]
        [SerializeField] [Range(0f, 1f)] private float blinkTrough = 0.12f;

        private DMTurretRuntime runtime;
        private MaterialPropertyBlock propertyBlock;
        private DMTurretBrainMode lastApplied = (DMTurretBrainMode)(-1);
        private bool lastPlayerFiring;
        private bool lastActivelyFiring;
        private bool keywordsArmed;
        private Color cachedTint = Color.black;
        private float cachedIntensity = 5f;
        private bool lookDirty = true;
        private float nextBlinkVisualTime;

        private const float BlinkVisualInterval = 0.05f;

        public Renderer TargetRenderer => targetRenderer;

        public void SetTargetRenderer(Renderer renderer)
        {
            targetRenderer = renderer;
            keywordsArmed = false;
            ArmEmissionKeywords();
            lookDirty = true;
            RefreshFromRuntime(force: true);
        }

        private void Awake()
        {
            runtime = GetComponent<DMTurretRuntime>();
            if (targetRenderer == null)
                targetRenderer = DMTurretAlarmResolve.TryFindAlarmRenderer(transform);
            ArmEmissionKeywords();
        }

        private void OnEnable()
        {
            if (runtime == null)
                runtime = GetComponent<DMTurretRuntime>();
            runtime?.RegisterAlarmLight(this);
            lookDirty = true;
            RefreshFromRuntime(force: true);
        }

        private void OnDisable()
        {
            runtime?.UnregisterAlarmLight(this);
            ApplyOff();
            lastApplied = (DMTurretBrainMode)(-1);
        }

        private void LateUpdate()
        {
            if (runtime == null || targetRenderer == null || runtime.IsDestroyed)
                return;

            if (runtime.IsPlayerProximityDormant)
                return;

            DMTurretBrainMode mode = ResolveEffectiveMode();
            if (ShouldProximityBlink(mode, runtime.IsFiring))
            {
                float now = Time.time;
                if (now < nextBlinkVisualTime)
                    return;

                nextBlinkVisualTime = now + BlinkVisualInterval;
                float proximity = ResolveProximity01(mode, runtime.AlarmEngageDistance);
                ApplyProximityBlink(cachedTint, cachedIntensity, proximity);
                return;
            }

            if (!lookDirty)
                return;

            ApplySolidLook(cachedTint, cachedIntensity);
        }

        internal void OnRuntimeBrainModeChanged(DMTurretBrainMode mode)
        {
            RefreshFromRuntime(force: false, brainModeHint: mode);
        }

        internal void RefreshFromRuntime(bool force, DMTurretBrainMode? brainModeHint = null)
        {
            if (runtime == null || targetRenderer == null)
                return;

            if (runtime.IsDestroyed)
            {
                if (force || lastApplied != DMTurretBrainMode.Scan || lastPlayerFiring)
                {
                    ApplyOff();
                    lastApplied = DMTurretBrainMode.Scan;
                    lastPlayerFiring = false;
                    lookDirty = false;
                }

                return;
            }

            DMTurretBrainMode mode = brainModeHint ?? ResolveEffectiveMode();
            bool playerFiring = DMTurretPlayerControl.IsControlling(runtime) && runtime.IsFiring;
            bool activelyFiring = runtime.IsFiring;
            if (!force && mode == lastApplied && playerFiring == lastPlayerFiring && activelyFiring == lastActivelyFiring)
                return;

            lastApplied = mode;
            lastPlayerFiring = playerFiring;
            lastActivelyFiring = activelyFiring;
            CacheLookForMode(mode);
            lookDirty = true;
            nextBlinkVisualTime = 0f;

            if (!ShouldProximityBlink(mode, activelyFiring))
                ApplySolidLook(cachedTint, cachedIntensity);
        }

        private DMTurretBrainMode ResolveEffectiveMode()
        {
            if (DMTurretPlayerControl.IsControlling(runtime))
                return runtime.IsFiring ? DMTurretBrainMode.Fire : DMTurretBrainMode.Scan;

            return runtime.BrainMode;
        }

        private static bool ShouldProximityBlink(DMTurretBrainMode mode, bool activelyFiring)
        {
            if (activelyFiring)
                return false;

            return mode == DMTurretBrainMode.Track || mode == DMTurretBrainMode.Fire;
        }

        private float ResolveProximity01(DMTurretBrainMode mode, float distance)
        {
            if (distance < 0f)
                return 0f;

            float fireRange = Mathf.Max(0.01f, runtime.FireRange);
            float trackRange = Mathf.Max(fireRange, runtime.TrackRange);

            switch (mode)
            {
                case DMTurretBrainMode.Track:
                    if (distance <= fireRange)
                        return 1f;
                    if (distance >= trackRange)
                        return 0f;
                    return 1f - Mathf.InverseLerp(fireRange, trackRange, distance);
                case DMTurretBrainMode.Fire:
                    return 1f - Mathf.InverseLerp(0f, fireRange, distance);
                default:
                    return 0f;
            }
        }

        private void CacheLookForMode(DMTurretBrainMode mode)
        {
            cachedTint = ResolveTint(mode);
            cachedIntensity = ResolveIntensity();
        }

        private void ApplyProximityBlink(Color tint, float intensity, float proximity)
        {
            Color off = new Color(0.04f, 0.04f, 0.04f, 1f);
            float hz = Mathf.Lerp(rimBlinkHz, centerBlinkHz, proximity);
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * hz * Mathf.PI * 2f);
            float blink = Mathf.Lerp(blinkTrough, 1f, wave);
            Color albedo = Color.Lerp(off, tint, blink);
            ApplyLook(albedo, tint * (intensity * blink));
        }

        private void ApplySolidLook(Color tint, float intensity)
        {
            ApplyLook(tint, tint * intensity);
            lookDirty = false;
        }

        private void ApplyOff()
        {
            Color off = new Color(0.04f, 0.04f, 0.04f, 1f);
            ApplyLook(off, Color.black);
        }

        private Color ResolveTint(DMTurretBrainMode mode)
        {
            if (useProfileColors)
            {
                DMTurretProfile profile = DMTurretProfile.Live;
                if (profile != null)
                {
                    switch (mode)
                    {
                        case DMTurretBrainMode.Track:
                            return profile.AlarmTrackColor;
                        case DMTurretBrainMode.Fire:
                            return profile.AlarmFireColor;
                        default:
                            return profile.AlarmScanColor;
                    }
                }
            }

            switch (mode)
            {
                case DMTurretBrainMode.Track:
                    return DarkMatterGenesisUiPalette.MapPoiRed;
                case DMTurretBrainMode.Fire:
                    return DarkMatterGenesisUiPalette.PositiveGreen;
                default:
                    return DarkMatterGenesisUiPalette.Gold;
            }
        }

        private float ResolveIntensity()
        {
            if (useProfileColors)
            {
                DMTurretProfile profile = DMTurretProfile.Live;
                if (profile != null)
                    return profile.AlarmEmissionIntensity;
            }

            return emissionIntensity;
        }

        private void ApplyLook(Color albedo, Color emissionHdr)
        {
            if (targetRenderer == null)
                return;

            albedo.a = 1f;
            emissionHdr.a = 1f;

            propertyBlock ??= new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(ColorId, albedo);
            propertyBlock.SetColor(BaseColorId, albedo);
            propertyBlock.SetColor(EmissionColorId, emissionHdr);
            propertyBlock.SetColor(EmissiveColorId, emissionHdr);
            propertyBlock.SetColor(EmissiveColorLdrId, emissionHdr);
            propertyBlock.SetFloat(UseEmissiveIntensityId, 0f);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private void ArmEmissionKeywords()
        {
            if (keywordsArmed || targetRenderer == null)
                return;

            keywordsArmed = true;
            Material[] mats = targetRenderer.sharedMaterials;
            if (mats == null)
                return;

            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];
                if (mat == null)
                    continue;
                mat.EnableKeyword("_EMISSION");
                mat.EnableKeyword("_EMISSIVE_COLOR");
            }
        }
    }

    /// <summary>Locates the optional Alarm mesh under a turret swivel hierarchy.</summary>
    public static class DMTurretAlarmResolve
    {
        public static Renderer TryFindAlarmRenderer(Transform root)
        {
            if (root == null)
                return null;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            Transform swivel = null;
            Transform alarm = null;
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                string n = t.name;
                if (alarm == null && n.Equals("Alarm", System.StringComparison.OrdinalIgnoreCase))
                    alarm = t;
                if (swivel == null
                    && (n.IndexOf("SOCKET_SWIVEL", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || n.Equals("SWIVEL", System.StringComparison.OrdinalIgnoreCase)))
                    swivel = t;
            }

            if (alarm != null)
                return alarm.GetComponent<Renderer>();

            if (swivel != null)
            {
                for (int i = 0; i < swivel.childCount; i++)
                {
                    Transform child = swivel.GetChild(i);
                    if (child.name.Equals("Alarm", System.StringComparison.OrdinalIgnoreCase))
                        return child.GetComponent<Renderer>();
                }
            }

            return null;
        }
    }
}

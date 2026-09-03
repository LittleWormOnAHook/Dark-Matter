using UnityEngine;

namespace Project.Survival.Exposure
{
    /// <summary>
    /// Per-indicator look for a neck hazard cube (Rad / Heat / Cold / Mix).
    /// Drives the assigned material's albedo + emission to the zone color while inside
    /// a matching volume, blinking faster toward the center. Off = dark grey / black.
    /// Uses a MaterialPropertyBlock so authored materials are not permanently mutated.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExposureHazardIndicator : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int UnlitColorId = Shader.PropertyToID("_UnlitColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");
        private static readonly int EmissiveColorLdrId = Shader.PropertyToID("_EmissiveColorLDR");
        private static readonly int EmissiveIntensityId = Shader.PropertyToID("_EmissiveIntensity");
        private static readonly int UseEmissiveIntensityId = Shader.PropertyToID("_UseEmissiveIntensity");

        [Header("Zone")]
        [SerializeField] private ExposureZoneKind zoneKind = ExposureZoneKind.Custom;

        [Header("Look")]
        [SerializeField] private Color emissionColor = Color.white;
        [SerializeField] private Color offColor = new Color(0.04f, 0.04f, 0.04f, 1f);
        [SerializeField] [Min(0f)] private float idleIntensity = 0.8f;
        [SerializeField] [Min(0f)] private float peakIntensity = 8f;

        [Header("Blink")]
        [Tooltip("Blink rate at the outer rim of a matching zone.")]
        [SerializeField] [Min(0f)] private float rimBlinkHz = 1.2f;
        [Tooltip("Blink rate at the inner / center of a matching zone.")]
        [SerializeField] [Min(0f)] private float centerBlinkHz = 10f;
        [Tooltip("How dark the blink trough goes. 0 = fully off (dark grey/black), 1 = no blink amplitude.")]
        [SerializeField] [Range(0f, 1f)] private float blinkTrough = 0.12f;

        [Header("Target")]
        [SerializeField] private Renderer targetRenderer;

        private ExposureReceiver receiver;
        private MaterialPropertyBlock propertyBlock;
        private bool keywordArmed;

        private void Awake()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();
            receiver = GetComponentInParent<ExposureReceiver>();
            ArmEmissionKeyword();
            ApplyOff();
        }

        private void OnEnable()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();
            if (receiver == null)
                receiver = GetComponentInParent<ExposureReceiver>();
            ArmEmissionKeyword();
            ApplyOff();
        }

        private void OnDisable()
        {
            ApplyOff();
        }

        private void LateUpdate()
        {
            float spatial = SampleSpatial();
            if (spatial <= 0.0001f)
            {
                ApplyOff();
                return;
            }

            float hz = Mathf.Lerp(rimBlinkHz, centerBlinkHz, spatial);
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * hz * Mathf.PI * 2f);
            float blink = Mathf.Lerp(blinkTrough, 1f, wave);
            float intensity = Mathf.Lerp(idleIntensity, peakIntensity, spatial) * blink;
            Color albedo = Color.Lerp(offColor, emissionColor, blink);
            ApplyLook(albedo, emissionColor * intensity);
        }

        private float SampleSpatial()
        {
            if (receiver == null)
                receiver = GetComponentInParent<ExposureReceiver>();
            if (receiver == null)
                return 0f;

            var zones = receiver.ActiveZones;
            if (zones == null || zones.Count == 0)
                return 0f;

            Vector3 point = receiver.transform.position;
            float best = 0f;
            for (int i = 0; i < zones.Count; i++)
            {
                ExposureZoneVolume zone = zones[i];
                if (zone == null || zone.Profile == null)
                    continue;
                if (zone.Profile.zoneKind != zoneKind)
                    continue;

                float spatial = zone.EvaluateSpatialIntensity(point);
                if (spatial > best)
                    best = spatial;
            }

            return best;
        }

        private void ApplyOff()
        {
            ApplyLook(offColor, Color.black);
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
            propertyBlock.SetColor(UnlitColorId, albedo);
            propertyBlock.SetColor(EmissionColorId, emissionHdr);
            propertyBlock.SetColor(EmissiveColorId, emissionHdr);
            propertyBlock.SetColor(EmissiveColorLdrId, emissionHdr);
            propertyBlock.SetFloat(UseEmissiveIntensityId, 0f);
            propertyBlock.SetFloat(EmissiveIntensityId, 0f);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private void ArmEmissionKeyword()
        {
            if (keywordArmed || targetRenderer == null)
                return;

            keywordArmed = true;
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

        private void Reset()
        {
            targetRenderer = GetComponent<Renderer>();
            offColor = new Color(0.04f, 0.04f, 0.04f, 1f);
            string n = gameObject.name;
            if (n.IndexOf("Rad", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                zoneKind = ExposureZoneKind.RadiationFlat;
                emissionColor = new Color(1f, 0.85f, 0.08f, 1f);
            }
            else if (n.IndexOf("Heat", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                zoneKind = ExposureZoneKind.ThermalHeat;
                emissionColor = new Color(1f, 0.12f, 0.04f, 1f);
            }
            else if (n.IndexOf("Cold", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                zoneKind = ExposureZoneKind.ThermalCold;
                emissionColor = new Color(0.15f, 0.45f, 1f, 1f);
            }
            else if (n.IndexOf("Mix", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                zoneKind = ExposureZoneKind.MixedHazard;
                emissionColor = new Color(0.12f, 1f, 0.2f, 1f);
            }
        }
    }
}

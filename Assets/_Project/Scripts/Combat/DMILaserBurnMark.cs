using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Multi-layer surface burn: dark scorched base + mid char + orangish emission glow that pulses
    /// then fades. Used by mining laser and pulse laser pistol hits.
    /// </summary>
    [DisallowMultipleComponent]
    public class DMILaserBurnMark : MonoBehaviour
    {
        [SerializeField] private Renderer scorchedRenderer;
        [SerializeField] private Renderer charRenderer;
        [SerializeField] private Renderer glowRenderer;

        [SerializeField] private float lifetime = 4.5f;
        [SerializeField] private float pulseHz = 2.4f;
        [SerializeField] private float pulseFloor = 0.35f;
        [SerializeField] private float fadeStartNormalized = 0.55f;
        [SerializeField] private Color glowColor = new Color(1f, 0.42f, 0.08f, 1f);
        [SerializeField] private float glowIntensity = 3.2f;

        private float _age;
        private float _scaleMul = 1f;
        private float _intensityMul = 1f;
        private float _twistDegrees;
        private Vector3 _baseLocalScale = Vector3.one;
        private bool _capturedBaseScale;
        private int _leaseId;
        private MaterialPropertyBlock _block;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        /// <summary>Increments whenever the mark is reused or force-released; delayed pool returns check this.</summary>
        public int LeaseId => _leaseId;

        /// <summary>Call before an early <see cref="Project.Core.PoolManager.Release"/> so a pending delayed release is ignored.</summary>
        public void InvalidateLease() => _leaseId++;

        /// <summary>
        /// Align and play with optional twist around the surface normal plus light scale/intensity variation.
        /// </summary>
        public void Play(Vector3 point, Vector3 normal, float twistDegrees = 0f, float scaleMul = 1f, float intensityMul = 1f)
        {
            _leaseId++;
            _age = 0f;
            _twistDegrees = twistDegrees;
            _scaleMul = Mathf.Clamp(scaleMul, 0.75f, 1.35f);
            _intensityMul = Mathf.Clamp(intensityMul, 0.7f, 1.35f);

            if (!_capturedBaseScale)
            {
                _baseLocalScale = transform.localScale;
                if (_baseLocalScale.sqrMagnitude < 0.0001f)
                    _baseLocalScale = Vector3.one;
                _capturedBaseScale = true;
            }

            transform.localScale = _baseLocalScale * _scaleMul;
            Align(point, normal, _twistDegrees);
            ApplyVisuals(1f, 1f);
            gameObject.SetActive(true);
        }

        private void OnEnable()
        {
            _age = 0f;
            if (_block == null)
                _block = new MaterialPropertyBlock();
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = lifetime > 0.01f ? Mathf.Clamp01(_age / lifetime) : 1f;

            float pulse = pulseFloor + (1f - pulseFloor) *
                          (0.5f + 0.5f * Mathf.Sin(_age * pulseHz * Mathf.PI * 2f));

            float fade = 1f;
            if (t > fadeStartNormalized)
            {
                float fadeT = Mathf.InverseLerp(fadeStartNormalized, 1f, t);
                fade = 1f - fadeT;
                // Kill the glow pulse as the mark cools.
                pulse *= fade;
            }

            ApplyVisuals(fade, pulse);

            if (t >= 1f)
                gameObject.SetActive(false);
        }

        private void Align(Vector3 point, Vector3 normal, float twistDegrees)
        {
            Vector3 n = normal.sqrMagnitude > 0.0001f ? normal.normalized : Vector3.up;
            // Sit slightly off the surface to avoid z-fight / terrain clip.
            transform.position = point + n * 0.014f;

            // Stable tangent frame, then twist around the surface normal (ping-pong from spawner).
            Vector3 tangent = Vector3.Cross(n, Vector3.up);
            if (tangent.sqrMagnitude < 0.001f)
                tangent = Vector3.Cross(n, Vector3.right);
            tangent.Normalize();
            Quaternion flat = Quaternion.LookRotation(-n, tangent);
            transform.rotation = Quaternion.AngleAxis(twistDegrees, n) * flat;
        }

        private void ApplyVisuals(float fade, float pulse)
        {
            if (_block == null)
                _block = new MaterialPropertyBlock();

            float i = _intensityMul;

            // Dark ash / charcoal must read clearly larger than the orange glow nest.
            // Keep RGB in a visible charcoal range (not near-black) so Io rock still shows a stain.
            SetLayer(
                scorchedRenderer,
                new Color(0.22f * i, 0.16f * i, 0.12f * i, fade * 0.92f),
                Color.black,
                emissionWeight: 0f);
            SetLayer(
                charRenderer,
                new Color(0.55f * i, 0.22f * i, 0.06f * i, fade * 0.88f),
                Color.black,
                emissionWeight: 0f);

            float glowA = fade * Mathf.Lerp(0.45f, 1f, pulse) * Mathf.Lerp(0.85f, 1.1f, i);
            Color glowTint = new Color(glowColor.r, glowColor.g, glowColor.b, glowA);
            Color emission = glowColor * (glowIntensity * pulse * fade * i);
            emission.a = 1f;
            SetLayer(glowRenderer, glowTint, emission, emissionWeight: 1f);
        }

        private void SetLayer(Renderer renderer, Color baseColor, Color emission, float emissionWeight)
        {
            if (renderer == null)
                return;

            renderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, baseColor);
            _block.SetColor(ColorId, baseColor);
            if (emissionWeight > 0f)
                _block.SetColor(EmissionColorId, emission);
            else
                _block.SetColor(EmissionColorId, Color.black);
            renderer.SetPropertyBlock(_block);

            if (!renderer.enabled)
                renderer.enabled = true;
        }

        public void BindLayers(Renderer scorched, Renderer midChar, Renderer glow)
        {
            scorchedRenderer = scorched;
            charRenderer = midChar;
            glowRenderer = glow;
        }
    }
}

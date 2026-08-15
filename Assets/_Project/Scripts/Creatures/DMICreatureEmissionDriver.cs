using UnityEngine;

namespace Project.Creatures
{
    /// <summary>
    /// Drives URP Lit / Unlit <c>_EmissionColor</c> on creature renderers from Creatures Manager
    /// Material Source intensity settings. Uses MaterialPropertyBlock so shared assets and death
    /// dissolve materials are not permanently mutated.
    /// <para>
    /// Intensity mapping: authored material emission is treated as the look at
    /// <see cref="emissionIdleIntensity"/>. Applied color =
    /// <c>authoredEmission * (currentIntensity / idleIntensity)</c>.
    /// Defaults 5 idle / 10 attack ⇒ attack is 2× the authored glow.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public class DMICreatureEmissionDriver : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");

        [Header("Material Source Emission")]
        [SerializeField] private bool boostEmissionWhileAttacking;
        [SerializeField] private bool flashWhileAttacking;
        [SerializeField] [Min(0.01f)] private float emissionIdleIntensity = 5f;
        [SerializeField] [Min(0.01f)] private float emissionAttackIntensity = 10f;
        [SerializeField] [Min(0.1f)] private float flashRateHz = 8f;
        [SerializeField] private Color flashTint = Color.white;
        [SerializeField] [Min(0.05f)] private float attackPulseDuration = 0.55f;

        [Header("Targets (optional)")]
        [Tooltip("When set, only renderers using this shared material are driven.")]
        [SerializeField] private Material materialFilter;
        [Tooltip("Optional explicit renderers. Empty = auto-find under CreatureVisual / children.")]
        [SerializeField] private Renderer[] targetRenderers;

        private MaterialPropertyBlock propertyBlock;
        private Renderer[] activeRenderers;
        private Color[] authoredEmissions;
        private bool[] hasEmission;
        private float attackUntil;
        private bool isDead;
        private bool cachesReady;

        public bool BoostEmissionWhileAttacking => boostEmissionWhileAttacking;
        public bool FlashWhileAttacking => flashWhileAttacking;

        private void Awake()
        {
            EnsureCaches();
            ApplyIntensity(emissionIdleIntensity, flashBlend: 0f);
        }

        private void OnEnable()
        {
            isDead = false;
            attackUntil = 0f;
            EnsureCaches();
            if (boostEmissionWhileAttacking)
                ApplyIntensity(emissionIdleIntensity, flashBlend: 0f);
        }

        private void OnDisable()
        {
            ClearPropertyBlocks();
        }

        private void LateUpdate()
        {
            if (isDead || !boostEmissionWhileAttacking)
                return;

            if (Time.time >= attackUntil)
            {
                ApplyIntensity(emissionIdleIntensity, flashBlend: 0f);
                return;
            }

            float flashBlend = 0f;
            float intensity = emissionAttackIntensity;
            if (flashWhileAttacking)
            {
                // 0..1 pulse; peaks at attack intensity, troughs toward idle.
                float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * flashRateHz * Mathf.PI * 2f);
                flashBlend = wave;
                intensity = Mathf.Lerp(emissionIdleIntensity, emissionAttackIntensity, wave);
            }

            ApplyIntensity(intensity, flashBlend);
        }

        /// <summary>
        /// Begin attack emission pulse (melee or ranged/spit). Duration matches anim attack lock.
        /// </summary>
        public void NotifyAttack(float durationSeconds = -1f)
        {
            if (isDead || !boostEmissionWhileAttacking)
                return;

            float duration = durationSeconds > 0.01f ? durationSeconds : attackPulseDuration;
            attackUntil = Time.time + Mathf.Max(0.05f, duration);

            if (flashWhileAttacking)
            {
                ApplyIntensity(emissionAttackIntensity, flashBlend: 1f);
            }
            else
            {
                ApplyIntensity(emissionAttackIntensity, flashBlend: 0f);
            }
        }

        public void NotifyDeath()
        {
            isDead = true;
            attackUntil = 0f;
            // Release MPB so EnemyDisintegrationEffect can own material instances.
            ClearPropertyBlocks();
        }

        public void ConfigureFromDefinition(DMICreatureDefinition definition)
        {
            if (definition == null)
                return;

            boostEmissionWhileAttacking = definition.boostEmissionWhileAttacking;
            flashWhileAttacking = definition.flashEmissionWhileAttacking;
            emissionIdleIntensity = Mathf.Max(0.01f, definition.emissionIdleIntensity);
            emissionAttackIntensity = Mathf.Max(0.01f, definition.emissionAttackIntensity);
            flashRateHz = Mathf.Max(0.1f, definition.emissionFlashRateHz);
            flashTint = definition.emissionFlashTint;
            materialFilter = definition.visualMaterialSource;
            cachesReady = false;
            EnsureCaches();

            if (boostEmissionWhileAttacking && !isDead)
                ApplyIntensity(emissionIdleIntensity, flashBlend: 0f);
            else
                ClearPropertyBlocks();
        }

        public void ConfigureAttackPulseDuration(float durationSeconds)
        {
            attackPulseDuration = Mathf.Max(0.05f, durationSeconds);
        }

        private void EnsureCaches()
        {
            if (cachesReady && activeRenderers != null)
                return;

            propertyBlock ??= new MaterialPropertyBlock();

            if (targetRenderers != null && targetRenderers.Length > 0)
            {
                activeRenderers = targetRenderers;
            }
            else
            {
                Transform visual = transform.Find("CreatureVisual");
                Renderer[] found = visual != null
                    ? visual.GetComponentsInChildren<Renderer>(true)
                    : GetComponentsInChildren<Renderer>(true);
                activeRenderers = found;
            }

            int count = activeRenderers != null ? activeRenderers.Length : 0;
            authoredEmissions = new Color[count];
            hasEmission = new bool[count];

            for (int i = 0; i < count; i++)
            {
                Renderer renderer = activeRenderers[i];
                authoredEmissions[i] = Color.black;
                hasEmission[i] = false;
                if (renderer == null)
                    continue;

                Material[] shared = renderer.sharedMaterials;
                if (shared == null || shared.Length == 0)
                    continue;

                // Prefer first slot that matches filter (or first emissive slot).
                for (int m = 0; m < shared.Length; m++)
                {
                    Material mat = shared[m];
                    if (mat == null)
                        continue;
                    if (materialFilter != null && mat != materialFilter)
                        continue;
                    if (!TryReadEmission(mat, out Color emission))
                        continue;

                    authoredEmissions[i] = emission;
                    hasEmission[i] = emission.maxColorComponent > 0.0001f
                                     || mat.IsKeywordEnabled("_EMISSION");
                    break;
                }
            }

            cachesReady = true;
        }

        private void ApplyIntensity(float intensity, float flashBlend)
        {
            EnsureCaches();
            if (activeRenderers == null || authoredEmissions == null)
                return;

            float idle = Mathf.Max(0.01f, emissionIdleIntensity);
            float scale = intensity / idle;
            propertyBlock ??= new MaterialPropertyBlock();

            for (int i = 0; i < activeRenderers.Length; i++)
            {
                Renderer renderer = activeRenderers[i];
                if (renderer == null || i >= hasEmission.Length || !hasEmission[i])
                    continue;

                Color emission = authoredEmissions[i] * scale;
                if (flashBlend > 0.001f && flashTint != Color.white)
                {
                    Color tinted = new Color(
                        authoredEmissions[i].r * flashTint.r,
                        authoredEmissions[i].g * flashTint.g,
                        authoredEmissions[i].b * flashTint.b,
                        authoredEmissions[i].a) * scale;
                    emission = Color.Lerp(emission, tinted, flashBlend);
                }

                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(EmissionColorId, emission);
                propertyBlock.SetColor(EmissiveColorId, emission);
                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void ClearPropertyBlocks()
        {
            if (activeRenderers == null)
                return;

            propertyBlock ??= new MaterialPropertyBlock();
            for (int i = 0; i < activeRenderers.Length; i++)
            {
                Renderer renderer = activeRenderers[i];
                if (renderer == null)
                    continue;
                propertyBlock.Clear();
                renderer.SetPropertyBlock(null);
            }
        }

        private static bool TryReadEmission(Material mat, out Color emission)
        {
            emission = Color.black;
            if (mat == null)
                return false;

            // HDRP Lit exposes both _EmissiveColor (real) and legacy _EmissionColor —
            // prefer the HDRP channel when the shader is HDRP.
            bool hdrp = mat.shader != null
                        && (mat.shader.name.StartsWith("HDRP/", System.StringComparison.Ordinal)
                            || mat.shader.name.StartsWith("Hidden/HDRP", System.StringComparison.Ordinal));

            if (hdrp && mat.HasProperty(EmissiveColorId))
            {
                emission = mat.GetColor(EmissiveColorId);
                if (mat.HasProperty("_UseEmissiveIntensity")
                    && mat.GetFloat("_UseEmissiveIntensity") > 0.5f
                    && mat.HasProperty("_EmissiveIntensity"))
                {
                    emission *= mat.GetFloat("_EmissiveIntensity");
                }

                return true;
            }

            if (mat.HasProperty(EmissionColorId))
            {
                emission = mat.GetColor(EmissionColorId);
                return true;
            }

            if (mat.HasProperty(EmissiveColorId))
            {
                emission = mat.GetColor(EmissiveColorId);
                return true;
            }

            return false;
        }
    }
}

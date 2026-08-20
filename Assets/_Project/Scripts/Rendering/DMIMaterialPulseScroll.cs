using UnityEngine;

namespace Project.Rendering
{
    /// <summary>
    /// Pulses alpha and/or emission intensity and optionally scrolls UVs on a
    /// <see cref="Renderer"/> (MeshRenderer / SkinnedMeshRenderer) via
    /// <see cref="MaterialPropertyBlock"/> so shared materials are not permanently mutated.
    /// <para>
    /// Supports <b>HDRP Lit / Unlit</b> (<c>_EmissiveColor</c>, <c>_BaseColorMap_ST</c>),
    /// <b>URP Lit / Unlit</b> (<c>_BaseColor</c>, <c>_EmissionColor</c>, <c>_EMISSION</c>),
    /// and <b>glTFast Shader Graph</b> <c>glTF-pbrMetallicRoughness</c>
    /// (<c>baseColorFactor</c>, <c>emissiveFactor</c>, <c>_EMISSIVE</c>).
    /// </para>
    /// <para>
    /// <b>Emission pulse:</b> Min/Max are intensity <b>multipliers</b> on the authored
    /// emissive color. If authored emission is near-black, Min/Max are treated as
    /// <b>absolute HDR intensity</b> on <see cref="fallbackEmissionTint"/>.
    /// HDRP Lit also exposes a legacy <c>_EmissionColor</c> property — this driver prefers
    /// <c>_EmissiveColor</c> so pulses hit the real HDRP emissive channel.
    /// When Pulse Emission is on, the shader's emission keyword is enabled if present
    /// (HDRP <c>_EMISSIVE_COLOR</c>; URP <c>_EMISSION</c>; glTF <c>_EMISSIVE</c>).
    /// MPB cannot set keywords.
    /// </para>
    /// <para>
    /// <b>Creature coexistence:</b> Do not drive the same material slots as
    /// <c>Project.Creatures.DMICreatureEmissionDriver</c> — both write emission through MPB
    /// and will fight. Use this for props / VFX meshes; keep creature idle/attack glow on
    /// the emission driver, or disable one. Dissolve / death effects that call
    /// <c>SetPropertyBlock(null)</c> also clear this driver's overrides.
    /// </para>
    /// <para>
    /// Runtime caches are <c>[NonSerialized]</c> and revalidated on enable / LateUpdate so
    /// domain reload cannot leave <c>cachesReady</c> true with a null slot list.
    /// <see cref="ExecuteAlways"/> + Preview In Edit Mode lets designers see the pulse without Play;
    /// Edit Mode always uses MPB so shared material assets are not dirtied.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [AddComponentMenu("Dark Matter/Rendering/DMI Material Pulse Scroll")]
    public class DMIMaterialPulseScroll : MonoBehaviour
    {
        private const float NearBlackLuminance = 0.002f;

        // URP Lit / Unlit (+ HDRP Lit also exposes several of these names)
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int UnlitColorId = Shader.PropertyToID("_UnlitColor");
        private static readonly int CutoffId = Shader.PropertyToID("_Cutoff");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissiveColorId = Shader.PropertyToID("_EmissiveColor");
        private static readonly int EmissiveIntensityId = Shader.PropertyToID("_EmissiveIntensity");
        private static readonly int UseEmissiveIntensityId = Shader.PropertyToID("_UseEmissiveIntensity");
        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
        private static readonly int BaseColorMapStId = Shader.PropertyToID("_BaseColorMap_ST");
        private static readonly int UnlitColorMapStId = Shader.PropertyToID("_UnlitColorMap_ST");
        private static readonly int MainTexStId = Shader.PropertyToID("_MainTex_ST");
        private static readonly int EmissionMapStId = Shader.PropertyToID("_EmissionMap_ST");
        private static readonly int EmissiveColorMapStId = Shader.PropertyToID("_EmissiveColorMap_ST");
        private static readonly int BumpMapStId = Shader.PropertyToID("_BumpMap_ST");
        private static readonly int NormalMapStId = Shader.PropertyToID("_NormalMap_ST");

        // glTFast Shader Graphs/glTF-pbrMetallicRoughness
        private static readonly int GltfBaseColorFactorId = Shader.PropertyToID("baseColorFactor");
        private static readonly int GltfEmissiveFactorId = Shader.PropertyToID("emissiveFactor");
        private static readonly int GltfAlphaCutoffId = Shader.PropertyToID("alphaCutoff");
        private static readonly int GltfBaseColorTextureStId = Shader.PropertyToID("baseColorTexture_ST");
        private static readonly int GltfEmissiveTextureStId = Shader.PropertyToID("emissiveTexture_ST");
        private static readonly int GltfNormalTextureStId = Shader.PropertyToID("normalTexture_ST");

        private const string UrpEmissionKeyword = "_EMISSION";
        private const string HdrpEmissiveColorKeyword = "_EMISSIVE_COLOR";
        private const string GltfEmissiveKeyword = "_EMISSIVE";

        public enum AlphaPulseTarget
        {
            [Tooltip("Pulse alpha on base color (_BaseColor / baseColorFactor / _Color).")]
            BaseColorAlpha = 0,
            [Tooltip("Pulse alpha-clip threshold (_Cutoff / alphaCutoff).")]
            Cutoff = 1,
            [Tooltip("Pulse both base color alpha and cutoff.")]
            BaseColorAlphaAndCutoff = 2
        }

        [Header("Target")]
        [Tooltip("Renderer to drive. Empty = MeshRenderer or SkinnedMeshRenderer on this GameObject.")]
        [SerializeField] private Renderer targetRenderer;

        [Tooltip("Material slot indices on the renderer. Empty = all slots.")]
        [SerializeField] private int[] materialIndices;

        [Tooltip(
            "Prefer MaterialPropertyBlock (default). Shared materials stay unchanged. " +
            "Disable only if you intentionally want to mutate material instances " +
            "(creates renderer.materials copies).")]
        [SerializeField] private bool useMaterialPropertyBlock = true;

        [Header("Alpha Pulse")]
        [Tooltip("Pulse opacity / clip. URP: _BaseColor.a / _Cutoff. glTF: baseColorFactor.a / alphaCutoff.")]
        [SerializeField] private bool pulseAlpha;

        [SerializeField] private AlphaPulseTarget alphaPulseTarget = AlphaPulseTarget.BaseColorAlpha;

        [Tooltip("Normalized pulse trough (0–1). Multiplies authored alpha / cutoff.")]
        [SerializeField] [Range(0f, 1f)] private float alphaPulseMin = 0.25f;

        [Tooltip("Normalized pulse peak (0–1). Multiplies authored alpha / cutoff.")]
        [SerializeField] [Range(0f, 1f)] private float alphaPulseMax = 1f;

        [Tooltip("Full sine cycles per second for alpha.")]
        [SerializeField] [Min(0f)] private float alphaPulseSpeed = 1f;

        [Header("Emission Pulse")]
        [Tooltip(
            "Pulse emissive HDR (HDRP _EmissiveColor, URP _EmissionColor, or glTF emissiveFactor). " +
            "Min/Max multiply authored emission when it has luminance; " +
            "if authored is near-black, Min/Max are absolute HDR intensity on Fallback Emission Tint. " +
            "Conflicts with DMICreatureEmissionDriver on the same slots — disable one.")]
        [SerializeField] private bool pulseEmission;

        [Tooltip(
            "Intensity trough. Multiplier on authored emission RGB when authored luminance > ~0. " +
            "Absolute HDR intensity (× Fallback Tint) when authored emission is near-black.")]
        [SerializeField] [Min(0f)] private float emissionPulseMin = 0.35f;

        [Tooltip(
            "Intensity peak. Multiplier on authored emission RGB when authored luminance > ~0. " +
            "Absolute HDR intensity (× Fallback Tint) when authored emission is near-black.")]
        [SerializeField] [Min(0f)] private float emissionPulseMax = 1.25f;

        [Tooltip("Full sine cycles per second for emission.")]
        [SerializeField] [Min(0f)] private float emissionPulseSpeed = 1.5f;

        [Tooltip(
            "Tint used when authored emissive is near-black (black × any multiplier stays black). " +
            "Min/Max then act as absolute HDR intensity on this tint. Default warm white.")]
        [SerializeField] [ColorUsage(false, true)] private Color fallbackEmissionTint = new Color(1f, 0.85f, 0.55f, 1f);

        [Tooltip(
            "When Pulse Emission is on, enable the shader emission keyword on target materials " +
            "(HDRP _EMISSIVE_COLOR, URP _EMISSION, glTF _EMISSIVE). Required for lit emissive paths. " +
            "MPB cannot set keywords. May dirty shared material assets.")]
        [SerializeField] private bool ensureEmissionKeyword = true;

        [Header("UV Scroll")]
        [Tooltip("Scroll base map UVs (_BaseMap_ST / _MainTex_ST / baseColorTexture_ST). xy = UV/sec.")]
        [SerializeField] private bool scrollBaseMap;

        [SerializeField] private Vector2 baseMapScrollSpeed = new Vector2(0.1f, 0f);

        [Tooltip("Scroll emission map UVs (_EmissionMap_ST / emissiveTexture_ST). xy = UV/sec.")]
        [SerializeField] private bool scrollEmissionMap;

        [SerializeField] private Vector2 emissionMapScrollSpeed = new Vector2(0.05f, 0f);

        [Tooltip("Optional normal map UV scroll (_BumpMap_ST / normalTexture_ST).")]
        [SerializeField] private bool scrollNormalMap;

        [SerializeField] private Vector2 normalMapScrollSpeed = Vector2.zero;

        [Header("Timing")]
        [Tooltip("Use Time.unscaledTime so pause / timescale does not freeze the pulse.")]
        [SerializeField] private bool useUnscaledTime;

        [Tooltip("Phase offset in radians added to both pulse waves.")]
        [SerializeField] private float phaseOffset;

        [Tooltip(
            "Preview pulse/scroll in the Editor without Play Mode ([ExecuteAlways]). " +
            "Edit Mode always uses MaterialPropertyBlock so shared assets are not dirtied.")]
        [SerializeField] private bool previewInEditMode = true;

        // Runtime caches — NonSerialized so domain reload cannot restore cachesReady=true with slots=null.
        [System.NonSerialized] private MaterialPropertyBlock propertyBlock;
        [System.NonSerialized] private SlotCache[] slots;
        [System.NonSerialized] private bool cachesReady;
        [System.NonSerialized] private Material[] mutableMaterials;
        [System.NonSerialized] private bool emissionKeywordEnsured;
        [System.NonSerialized] private bool warnedMissingEmission;

        private struct SlotCache
        {
            public int materialIndex;

            public bool hasBaseColor;
            public int baseColorPropId;

            public bool hasCutoff;
            public int cutoffPropId;

            public bool hasEmission;
            public int emissionPropId;
            /// <summary>
            /// Optional secondary emission id (HDRP Lit often has both <c>_EmissiveColor</c> and
            /// a legacy <c>_EmissionColor</c>). Dual-write keeps MPB overrides coherent.
            /// </summary>
            public bool hasSecondaryEmission;
            public int secondaryEmissionPropId;
            /// <summary>HDRP uses _EMISSIVE_COLOR; URP uses _EMISSION; glTF uses _EMISSIVE.</summary>
            public string emissionKeyword;

            public bool hasBaseMapSt;
            public int baseMapStPropId;

            public bool hasEmissionMapSt;
            public int emissionMapStPropId;

            public bool hasBumpMapSt;
            public int bumpMapStPropId;

            /// <summary>True when authored emission luminance is near-zero — use absolute intensity path.</summary>
            public bool useAbsoluteEmissionIntensity;

            public Color authoredBaseColor;
            public float authoredCutoff;
            public Color authoredEmission;
            public Vector4 authoredBaseMapSt;
            public Vector4 authoredEmissionMapSt;
            public Vector4 authoredBumpMapSt;
        }

        private void Awake()
        {
            EnsureCachesReady(force: true);
            TryEnsureEmissionKeywordIfNeeded();
        }

        private void OnEnable()
        {
            // Domain reload can leave cachesReady=true with slots=null — always re-validate.
            EnsureCachesReady(force: !cachesReady || slots == null);
            TryEnsureEmissionKeywordIfNeeded();
        }

        private void OnDisable()
        {
            ClearOverrides();
        }

        private void OnDestroy()
        {
            ClearOverrides();
            DestroyMutableMaterials();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying && !previewInEditMode)
                return;

            if (!pulseAlpha && !pulseEmission && !scrollBaseMap && !scrollEmissionMap && !scrollNormalMap)
                return;

            if (!EnsureCachesReady(force: false))
                return;

            if (pulseEmission)
                TryEnsureEmissionKeywordIfNeeded();

            float t = ResolvePulseTime();
            float alphaWave = 0.5f + 0.5f * Mathf.Sin((t * alphaPulseSpeed * Mathf.PI * 2f) + phaseOffset);
            float emissionWave = 0.5f + 0.5f * Mathf.Sin((t * emissionPulseSpeed * Mathf.PI * 2f) + phaseOffset);
            float alphaMul = Mathf.Lerp(alphaPulseMin, alphaPulseMax, alphaWave);
            float emissionMul = Mathf.Lerp(emissionPulseMin, emissionPulseMax, emissionWave);

            // Edit Mode: always MPB so shared materials / prefab assets stay clean.
            bool useMpb = useMaterialPropertyBlock || !Application.isPlaying;
            if (useMpb)
                ApplyViaPropertyBlock(t, alphaMul, emissionMul);
            else
                ApplyViaMaterialInstances(t, alphaMul, emissionMul);
        }

        private float ResolvePulseTime()
        {
            if (!Application.isPlaying)
                return Time.realtimeSinceStartup;

            return useUnscaledTime ? Time.unscaledTime : Time.time;
        }

        /// <summary>
        /// Ensures slot caches exist. Returns false when there is nothing to drive.
        /// </summary>
        private bool EnsureCachesReady(bool force)
        {
            if (!force && cachesReady && slots != null && slots.Length > 0 && targetRenderer != null)
                return true;

            RebuildCaches();
            return targetRenderer != null && slots != null && slots.Length > 0;
        }

        /// <summary>Force cache rebuild (e.g. after swapping materials at runtime).</summary>
        public void RebuildCaches()
        {
            cachesReady = false;
            emissionKeywordEnsured = false;
            ResolveRenderer();
            if (targetRenderer == null)
            {
                slots = System.Array.Empty<SlotCache>();
                return;
            }

            Material[] shared = targetRenderer.sharedMaterials;
            if (shared == null || shared.Length == 0)
            {
                slots = System.Array.Empty<SlotCache>();
                return;
            }

            int[] indices = materialIndices != null && materialIndices.Length > 0
                ? materialIndices
                : BuildAllIndices(shared.Length);

            var list = new System.Collections.Generic.List<SlotCache>(indices.Length);
            for (int i = 0; i < indices.Length; i++)
            {
                int mi = indices[i];
                if (mi < 0 || mi >= shared.Length)
                    continue;

                Material mat = shared[mi];
                if (mat == null)
                    continue;

                SlotCache slot = new SlotCache { materialIndex = mi };
                CacheFromMaterial(mat, ref slot);
                list.Add(slot);
            }

            slots = list.ToArray();
            propertyBlock ??= new MaterialPropertyBlock();
            cachesReady = true;

            if (pulseEmission && !warnedMissingEmission)
            {
                bool anyEmission = false;
                for (int i = 0; i < slots.Length; i++)
                {
                    if (slots[i].hasEmission)
                    {
                        anyEmission = true;
                        break;
                    }
                }

                if (!anyEmission && slots.Length > 0)
                {
                    warnedMissingEmission = true;
                    Debug.LogWarning(
                        "[DMIMaterialPulseScroll] No emission color property on '" + name +
                        "' materials (_EmissiveColor / _EmissionColor / emissiveFactor). " +
                        "Pulse Emission will boost base color HDR as a fallback. " +
                        "Prefer enabling Emission on HDRP Lit (or URP Lit).",
                        this);
                }
            }
        }

        /// <summary>Clear MPB / restore nothing (shared materials unchanged when using MPB).</summary>
        public void ClearOverrides()
        {
            if (targetRenderer == null)
                return;

            if (useMaterialPropertyBlock)
            {
                propertyBlock ??= new MaterialPropertyBlock();
                if (slots != null)
                {
                    for (int i = 0; i < slots.Length; i++)
                    {
                        propertyBlock.Clear();
                        targetRenderer.SetPropertyBlock(null, slots[i].materialIndex);
                    }
                }
                else
                {
                    targetRenderer.SetPropertyBlock(null);
                }
            }
        }

        private void ApplyViaPropertyBlock(float time, float alphaMul, float emissionMul)
        {
            propertyBlock ??= new MaterialPropertyBlock();

            for (int i = 0; i < slots.Length; i++)
            {
                SlotCache slot = slots[i];
                targetRenderer.GetPropertyBlock(propertyBlock, slot.materialIndex);

                if (pulseAlpha)
                    ApplyAlphaToBlock(ref slot, alphaMul);

                if (pulseEmission)
                    ApplyEmissionToBlock(ref slot, emissionMul);

                if (scrollBaseMap && slot.hasBaseMapSt)
                    propertyBlock.SetVector(slot.baseMapStPropId, ScrollSt(slot.authoredBaseMapSt, baseMapScrollSpeed, time));

                if (scrollEmissionMap && slot.hasEmissionMapSt)
                    propertyBlock.SetVector(slot.emissionMapStPropId, ScrollSt(slot.authoredEmissionMapSt, emissionMapScrollSpeed, time));

                if (scrollNormalMap && slot.hasBumpMapSt)
                    propertyBlock.SetVector(slot.bumpMapStPropId, ScrollSt(slot.authoredBumpMapSt, normalMapScrollSpeed, time));

                targetRenderer.SetPropertyBlock(propertyBlock, slot.materialIndex);
            }
        }

        private void ApplyViaMaterialInstances(float time, float alphaMul, float emissionMul)
        {
            EnsureMutableMaterials();
            if (mutableMaterials == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                SlotCache slot = slots[i];
                if (slot.materialIndex < 0 || slot.materialIndex >= mutableMaterials.Length)
                    continue;

                Material mat = mutableMaterials[slot.materialIndex];
                if (mat == null)
                    continue;

                if (pulseAlpha)
                    ApplyAlphaToMaterial(mat, ref slot, alphaMul, alphaPulseTarget);

                if (pulseEmission)
                    ApplyEmissionToMaterial(mat, ref slot, emissionMul);

                if (scrollBaseMap && slot.hasBaseMapSt)
                    mat.SetVector(slot.baseMapStPropId, ScrollSt(slot.authoredBaseMapSt, baseMapScrollSpeed, time));

                if (scrollEmissionMap && slot.hasEmissionMapSt)
                    mat.SetVector(slot.emissionMapStPropId, ScrollSt(slot.authoredEmissionMapSt, emissionMapScrollSpeed, time));

                if (scrollNormalMap && slot.hasBumpMapSt)
                    mat.SetVector(slot.bumpMapStPropId, ScrollSt(slot.authoredBumpMapSt, normalMapScrollSpeed, time));
            }
        }

        private void ApplyEmissionToBlock(ref SlotCache slot, float emissionMul)
        {
            if (slot.hasEmission)
            {
                Color pulsed = ResolvePulsedEmission(ref slot, emissionMul);
                propertyBlock.SetColor(slot.emissionPropId, pulsed);
                if (slot.hasSecondaryEmission)
                    propertyBlock.SetColor(slot.secondaryEmissionPropId, pulsed);
                return;
            }

            // Fallback: materials with no emission property — boost base color HDR.
            if (slot.hasBaseColor)
                propertyBlock.SetColor(slot.baseColorPropId, ResolveBaseColorEmissionFallback(ref slot, emissionMul));
        }

        private void ApplyEmissionToMaterial(Material mat, ref SlotCache slot, float emissionMul)
        {
            if (slot.hasEmission)
            {
                Color pulsed = ResolvePulsedEmission(ref slot, emissionMul);
                mat.SetColor(slot.emissionPropId, pulsed);
                if (slot.hasSecondaryEmission)
                    mat.SetColor(slot.secondaryEmissionPropId, pulsed);
                return;
            }

            if (slot.hasBaseColor)
                mat.SetColor(slot.baseColorPropId, ResolveBaseColorEmissionFallback(ref slot, emissionMul));
        }

        private Color ResolveBaseColorEmissionFallback(ref SlotCache slot, float emissionMul)
        {
            Color c = slot.authoredBaseColor;
            // Keep authored alpha; boost RGB as a cheap emissive stand-in.
            c.r *= emissionMul;
            c.g *= emissionMul;
            c.b *= emissionMul;
            return c;
        }

        private Color ResolvePulsedEmission(ref SlotCache slot, float emissionMul)
        {
            if (slot.useAbsoluteEmissionIntensity)
            {
                Color tint = fallbackEmissionTint;
                tint.a = 1f;
                return tint * emissionMul;
            }

            return slot.authoredEmission * emissionMul;
        }

        private void ApplyAlphaToBlock(ref SlotCache slot, float alphaMul)
        {
            bool pulseBase = alphaPulseTarget == AlphaPulseTarget.BaseColorAlpha
                             || alphaPulseTarget == AlphaPulseTarget.BaseColorAlphaAndCutoff;
            bool pulseCutoff = alphaPulseTarget == AlphaPulseTarget.Cutoff
                               || alphaPulseTarget == AlphaPulseTarget.BaseColorAlphaAndCutoff;

            if (pulseBase && slot.hasBaseColor)
            {
                Color c = slot.authoredBaseColor;
                c.a = Mathf.Clamp01(slot.authoredBaseColor.a * alphaMul);
                propertyBlock.SetColor(slot.baseColorPropId, c);
            }

            if (pulseCutoff && slot.hasCutoff)
                propertyBlock.SetFloat(slot.cutoffPropId, Mathf.Clamp01(slot.authoredCutoff * alphaMul));
        }

        private static void ApplyAlphaToMaterial(
            Material mat, ref SlotCache slot, float alphaMul, AlphaPulseTarget target)
        {
            bool pulseBase = target == AlphaPulseTarget.BaseColorAlpha
                             || target == AlphaPulseTarget.BaseColorAlphaAndCutoff;
            bool pulseCutoff = target == AlphaPulseTarget.Cutoff
                               || target == AlphaPulseTarget.BaseColorAlphaAndCutoff;

            if (pulseBase && slot.hasBaseColor)
            {
                Color c = slot.authoredBaseColor;
                c.a = Mathf.Clamp01(slot.authoredBaseColor.a * alphaMul);
                mat.SetColor(slot.baseColorPropId, c);
            }

            if (pulseCutoff && slot.hasCutoff)
                mat.SetFloat(slot.cutoffPropId, Mathf.Clamp01(slot.authoredCutoff * alphaMul));
        }

        private void EnsureMutableMaterials()
        {
            if (mutableMaterials != null || targetRenderer == null)
                return;

            mutableMaterials = targetRenderer.materials;
        }

        private void DestroyMutableMaterials()
        {
            if (mutableMaterials == null)
                return;

            for (int i = 0; i < mutableMaterials.Length; i++)
            {
                if (mutableMaterials[i] == null)
                    continue;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(mutableMaterials[i]);
                else
#endif
                    Destroy(mutableMaterials[i]);
            }

            mutableMaterials = null;
        }

        private void TryEnsureEmissionKeywordIfNeeded()
        {
            if (!pulseEmission)
                return;

            if (emissionKeywordEnsured)
                return;

            if (!ensureEmissionKeyword)
                ensureEmissionKeyword = true;

            TryEnsureEmissionKeyword();
            emissionKeywordEnsured = true;
        }

        private void TryEnsureEmissionKeyword()
        {
            if (targetRenderer == null || slots == null)
                return;

            if (!useMaterialPropertyBlock)
            {
                EnsureMutableMaterials();
                if (mutableMaterials == null)
                    return;

                for (int i = 0; i < slots.Length; i++)
                {
                    int mi = slots[i].materialIndex;
                    if (mi < 0 || mi >= mutableMaterials.Length)
                        continue;
                    EnableEmissionOnMaterial(mutableMaterials[mi], ref slots[i]);
                }

                return;
            }

            Material[] shared = targetRenderer.sharedMaterials;
            if (shared == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                int mi = slots[i].materialIndex;
                if (mi < 0 || mi >= shared.Length)
                    continue;

                EnableEmissionOnMaterial(shared[mi], ref slots[i]);
            }
        }

        private static void EnableEmissionOnMaterial(Material mat, ref SlotCache slot)
        {
            if (mat == null || !slot.hasEmission)
                return;

            // HDRP Lit: ensure emissive path. Prefer _EMISSIVE_COLOR for color-only;
            // when an emissive map is authored, HDRP keeps _EMISSIVE_COLOR_MAP and may
            // reject _EMISSIVE_COLOR (HDMaterial.ValidateMaterial strips it).
            if (IsHdrpShader(mat.shader) && slot.emissionPropId == EmissiveColorId)
            {
                if (mat.HasProperty(UseEmissiveIntensityId) && mat.GetFloat(UseEmissiveIntensityId) > 0.5f)
                {
                    // Bake intensity into color so MPB _EmissiveColor pulses are visible.
                    mat.SetFloat(UseEmissiveIntensityId, 0f);
                    if (mat.HasProperty(EmissiveColorId))
                        mat.SetColor(EmissiveColorId, slot.authoredEmission);
                }

                const string HdrpEmissiveColorMapKeyword = "_EMISSIVE_COLOR_MAP";
                bool hasEmissiveMap = mat.IsKeywordEnabled(HdrpEmissiveColorMapKeyword);
                if (!hasEmissiveMap && !mat.IsKeywordEnabled(HdrpEmissiveColorKeyword))
                    mat.EnableKeyword(HdrpEmissiveColorKeyword);
                return;
            }

            // glTF often exposes emissiveFactor without needing a keyword toggle, but
            // _EMISSIVE is still the graph's feature flag when present.
            if (string.IsNullOrEmpty(slot.emissionKeyword))
                return;

            // Only toggle the keyword when missing. Avoid writing GI flags onto shared
            // material assets (that dirties project materials and can desync variants).
            if (!mat.IsKeywordEnabled(slot.emissionKeyword))
                mat.EnableKeyword(slot.emissionKeyword);
        }

        private static bool IsHdrpShader(Shader shader)
        {
            if (shader == null)
                return false;

            string name = shader.name;
            return name.StartsWith("HDRP/", System.StringComparison.Ordinal)
                   || name.StartsWith("Hidden/HDRP", System.StringComparison.Ordinal);
        }

        private void ResolveRenderer()
        {
            if (targetRenderer != null)
                return;

            targetRenderer = GetComponent<MeshRenderer>();
            if (targetRenderer == null)
                targetRenderer = GetComponent<SkinnedMeshRenderer>();
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();
        }

        private static void CacheFromMaterial(Material mat, ref SlotCache slot)
        {
            bool hdrp = IsHdrpShader(mat.shader);

            // Base color: HDRP Lit/Unlit → URP Lit → legacy → glTF PBR
            if (TryBindColor(mat, BaseColorId, ref slot.hasBaseColor, ref slot.baseColorPropId, ref slot.authoredBaseColor)
                || TryBindColor(mat, UnlitColorId, ref slot.hasBaseColor, ref slot.baseColorPropId, ref slot.authoredBaseColor)
                || TryBindColor(mat, ColorId, ref slot.hasBaseColor, ref slot.baseColorPropId, ref slot.authoredBaseColor)
                || TryBindColor(mat, GltfBaseColorFactorId, ref slot.hasBaseColor, ref slot.baseColorPropId, ref slot.authoredBaseColor))
            {
                // bound
            }

            // Cutoff: URP/HDRP → glTF
            if (mat.HasProperty(CutoffId))
            {
                slot.hasCutoff = true;
                slot.cutoffPropId = CutoffId;
                slot.authoredCutoff = mat.GetFloat(CutoffId);
            }
            else if (mat.HasProperty(GltfAlphaCutoffId))
            {
                slot.hasCutoff = true;
                slot.cutoffPropId = GltfAlphaCutoffId;
                slot.authoredCutoff = mat.GetFloat(GltfAlphaCutoffId);
            }

            // Emission: prefer HDRP _EmissiveColor over legacy _EmissionColor.
            // HDRP Lit exposes BOTH — binding _EmissionColor first made pulses drive the
            // wrong channel while real emissive stayed static.
            if (hdrp && TryBindColor(mat, EmissiveColorId, ref slot.hasEmission, ref slot.emissionPropId, ref slot.authoredEmission))
            {
                slot.emissionKeyword = HdrpEmissiveColorKeyword;
                if (mat.HasProperty(EmissionColorId))
                {
                    slot.hasSecondaryEmission = true;
                    slot.secondaryEmissionPropId = EmissionColorId;
                }

                // When UseEmissiveIntensity is on, authored RGB is LDR and intensity is separate.
                if (mat.HasProperty(UseEmissiveIntensityId)
                    && mat.GetFloat(UseEmissiveIntensityId) > 0.5f
                    && mat.HasProperty(EmissiveIntensityId))
                {
                    float intensity = mat.GetFloat(EmissiveIntensityId);
                    slot.authoredEmission *= intensity;
                }
            }
            else if (!hdrp && TryBindColor(mat, EmissionColorId, ref slot.hasEmission, ref slot.emissionPropId, ref slot.authoredEmission))
            {
                slot.emissionKeyword = UrpEmissionKeyword;
                if (mat.HasProperty(EmissiveColorId))
                {
                    slot.hasSecondaryEmission = true;
                    slot.secondaryEmissionPropId = EmissiveColorId;
                }
            }
            else if (TryBindColor(mat, EmissiveColorId, ref slot.hasEmission, ref slot.emissionPropId, ref slot.authoredEmission))
            {
                slot.emissionKeyword = hdrp ? HdrpEmissiveColorKeyword : UrpEmissionKeyword;
            }
            else if (TryBindColor(mat, EmissionColorId, ref slot.hasEmission, ref slot.emissionPropId, ref slot.authoredEmission))
            {
                slot.emissionKeyword = UrpEmissionKeyword;
            }
            else if (TryBindColor(mat, GltfEmissiveFactorId, ref slot.hasEmission, ref slot.emissionPropId, ref slot.authoredEmission))
            {
                slot.emissionKeyword = GltfEmissiveKeyword;
            }

            if (slot.hasEmission)
                slot.useAbsoluteEmissionIntensity = EmissionLuminance(slot.authoredEmission) < NearBlackLuminance;

            // Base UV ST: HDRP Lit → HDRP Unlit → URP → legacy → glTF
            if (TryBindVector(mat, BaseColorMapStId, ref slot.hasBaseMapSt, ref slot.baseMapStPropId, ref slot.authoredBaseMapSt)
                || TryBindVector(mat, UnlitColorMapStId, ref slot.hasBaseMapSt, ref slot.baseMapStPropId, ref slot.authoredBaseMapSt)
                || TryBindVector(mat, BaseMapStId, ref slot.hasBaseMapSt, ref slot.baseMapStPropId, ref slot.authoredBaseMapSt)
                || TryBindVector(mat, MainTexStId, ref slot.hasBaseMapSt, ref slot.baseMapStPropId, ref slot.authoredBaseMapSt)
                || TryBindVector(mat, GltfBaseColorTextureStId, ref slot.hasBaseMapSt, ref slot.baseMapStPropId, ref slot.authoredBaseMapSt))
            {
                // bound
            }

            // Emission UV ST: HDRP → URP → glTF
            if (TryBindVector(mat, EmissiveColorMapStId, ref slot.hasEmissionMapSt, ref slot.emissionMapStPropId, ref slot.authoredEmissionMapSt)
                || TryBindVector(mat, EmissionMapStId, ref slot.hasEmissionMapSt, ref slot.emissionMapStPropId, ref slot.authoredEmissionMapSt)
                || TryBindVector(mat, GltfEmissiveTextureStId, ref slot.hasEmissionMapSt, ref slot.emissionMapStPropId, ref slot.authoredEmissionMapSt))
            {
                // bound
            }

            // Normal UV ST: HDRP → URP → glTF
            if (TryBindVector(mat, NormalMapStId, ref slot.hasBumpMapSt, ref slot.bumpMapStPropId, ref slot.authoredBumpMapSt)
                || TryBindVector(mat, BumpMapStId, ref slot.hasBumpMapSt, ref slot.bumpMapStPropId, ref slot.authoredBumpMapSt)
                || TryBindVector(mat, GltfNormalTextureStId, ref slot.hasBumpMapSt, ref slot.bumpMapStPropId, ref slot.authoredBumpMapSt))
            {
                // bound
            }
        }

        private static bool TryBindColor(
            Material mat, int propId, ref bool has, ref int boundId, ref Color authored)
        {
            if (!mat.HasProperty(propId))
                return false;

            has = true;
            boundId = propId;
            authored = mat.GetColor(propId);
            return true;
        }

        private static bool TryBindVector(
            Material mat, int propId, ref bool has, ref int boundId, ref Vector4 authored)
        {
            if (!mat.HasProperty(propId))
                return false;

            has = true;
            boundId = propId;
            authored = mat.GetVector(propId);
            return true;
        }

        private static float EmissionLuminance(Color c)
        {
            float maxChannel = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            float rec709 = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
            return Mathf.Max(maxChannel, rec709);
        }

        private static Vector4 ScrollSt(Vector4 authored, Vector2 speed, float time)
        {
            return new Vector4(
                authored.x,
                authored.y,
                authored.z + speed.x * time,
                authored.w + speed.y * time);
        }

        private static int[] BuildAllIndices(int count)
        {
            var indices = new int[count];
            for (int i = 0; i < count; i++)
                indices[i] = i;
            return indices;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            alphaPulseMin = Mathf.Clamp01(alphaPulseMin);
            alphaPulseMax = Mathf.Clamp01(alphaPulseMax);
            if (alphaPulseMax < alphaPulseMin)
                alphaPulseMax = alphaPulseMin;

            emissionPulseMin = Mathf.Max(0f, emissionPulseMin);
            emissionPulseMax = Mathf.Max(emissionPulseMin, emissionPulseMax);
            alphaPulseSpeed = Mathf.Max(0f, alphaPulseSpeed);
            emissionPulseSpeed = Mathf.Max(0f, emissionPulseSpeed);

            if (pulseEmission)
                ensureEmissionKeyword = true;

            // Always invalidate — domain reload / inspector edits must not keep stale caches.
            cachesReady = false;
            emissionKeywordEnsured = false;
            warnedMissingEmission = false;

            if (isActiveAndEnabled && (Application.isPlaying || previewInEditMode))
            {
                // Defer rebuild so we don't touch materials during OnValidate serialization.
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this == null || !isActiveAndEnabled)
                        return;
                    EnsureCachesReady(force: true);
                    TryEnsureEmissionKeywordIfNeeded();
                };
            }
        }

        private void Reset()
        {
            ResolveRenderer();
            ensureEmissionKeyword = true;
            useMaterialPropertyBlock = true;
            previewInEditMode = true;
            fallbackEmissionTint = new Color(1f, 0.85f, 0.55f, 1f);
        }
#endif
    }
}

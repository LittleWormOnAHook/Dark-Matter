using UnityEngine;
using UnityEngine.Serialization;
using Project.Core;

/// <summary>
/// Prefab outline/highlight (OutlineDM).
/// - Look-at: glow when the player is within lookRange and facing the item (non-scannable props).
/// - Live scanner: glow while optics scanner sees this object (non-scannable props).
/// - Scan flash: 5 lerp pulses over 2.5s when the sweep disc hits a scannable item.
/// </summary>
public class OutlineController : MonoBehaviour
{
    private const int PriorityLookAt = 1;
    private const int PriorityPostScan = 2;
    private const int PriorityLiveScanner = 3;
    private const int PriorityResourceScan = 4;

    [Header("Look")]
    public Color outlineColor = new Color(0.35f, 1f, 0.82f, 1f);
    [Range(0f, 1f)] public float alpha = 1f;
    public float thickness = 0.01f;

    [Header("Look At (no scanner)")]
    [Tooltip("Outline when the player is this close and looking toward the item.")]
    [FormerlySerializedAs("scanRange")]
    public float lookRange = 2f;
    [Tooltip("Dot product threshold for \"looking at\" (1 = dead center, ~0.7 ≈ 45°).")]
    [Range(0.5f, 1f)] public float lookDotThreshold = 0.72f;
    [Tooltip("Optional override. Auto-resolves via PlayerReference when empty.")]
    public Transform player;

    [Header("Scanner Falloff")]
    [Tooltip("Distance where scanner outline intensity reaches zero.")]
    public float scannerFalloffDistance = 50f;

    [Header("Scan Discovery")]
    [Tooltip("When true, look-at and live optics outlines are suppressed; only sweep flashes apply.")]
    public bool scannerOnlyOutline;

    public Material outlineMaterial;

    private Renderer rend;
    private Material[] baseMaterials;
    private bool outlineSlotApplied;
    private bool liveScannerHighlight;
    private float liveScannerIntensity;
    private bool resourceScanHighlight;
    private Color resourceScanColor;
    private float resourceScanAlpha;
    private bool postScanActive;
    private Color postScanColor;
    private float postScanAlpha;
    private float postScanExpireTime;
    private int postScanPriority;
    private bool scanFlashActive;
    private float scanFlashStartTime;
    private float scanFlashDuration = 2.5f;
    private int scanFlashPulses = 5;
    private Color scanFlashColor = Color.white;
    private float scanFlashAlpha = 1f;
    private float pulseTimer;
    private Transform resolvedPlayer;
    private Camera playerCamera;
    private MaterialPropertyBlock propertyBlock;
    private int outlineMaterialIndex = -1;
    private float lastAppliedOutlineAlpha = -1f;
    private Color lastAppliedOutlineColor;
    private static Material sharedOutlineMaterial;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public bool IsScannerHighlighted =>
        liveScannerHighlight || postScanActive || scanFlashActive || resourceScanHighlight;
    public bool IsPostScanHighlighted => postScanActive || scanFlashActive;

    private void Awake()
    {
        CacheRenderer();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        GameSession.GameStarted += HandleGameStarted;
    }

    private void OnDisable()
    {
        GameSession.GameStarted -= HandleGameStarted;
        ClearAllHighlights();
    }

    private void OnDestroy()
    {
        GameSession.GameStarted -= HandleGameStarted;
        RemoveOutlineSlot();
    }

    private void Start()
    {
        AutoDetectScannerOnlyOutline();
        ResolvePlayer();
        ApplyOutlineVisible(0f, outlineColor);
    }

    private void HandleGameStarted()
    {
        resolvedPlayer = null;
        playerCamera = null;
        ResolvePlayer();
    }

    private void Update()
    {
        if (rend == null)
            return;

        if (postScanActive && Time.unscaledTime >= postScanExpireTime)
            ClearPostScanHighlight();

        if (scanFlashActive && Time.unscaledTime >= scanFlashStartTime + scanFlashDuration)
            ClearScanDiscoveryFlash();

        bool hasForcedHighlight = resourceScanHighlight
            || scanFlashActive
            || liveScannerHighlight
            || postScanActive;

        // Scanner-only props idle with no sweep/highlight: skip player resolve + look-at work.
        if (scannerOnlyOutline && !hasForcedHighlight)
        {
            if (outlineSlotApplied)
                ApplyOutlineVisible(0f, outlineColor);
            return;
        }

        ResolvePlayer();

        float lookStrength = hasForcedHighlight ? 0f : EvaluateLookAtStrength();
        float strength = 0f;
        Color displayColor = outlineColor;

        if (resourceScanHighlight)
        {
            strength = 1f;
            displayColor = resourceScanColor;
        }
        else if (scanFlashActive)
        {
            strength = EvaluateScanFlashStrength();
            displayColor = scanFlashColor;
        }
        else if (liveScannerHighlight && !scannerOnlyOutline)
        {
            strength = liveScannerIntensity;
            displayColor = outlineColor;
        }
        else if (postScanActive && !scannerOnlyOutline)
        {
            strength = EvaluatePostScanStrength();
            displayColor = postScanColor;
        }
        else if (lookStrength > 0f)
        {
            strength = lookStrength;
            displayColor = outlineColor;
        }

        if (strength <= 0.001f)
        {
            if (outlineSlotApplied)
                ApplyOutlineVisible(0f, displayColor);
            return;
        }

        pulseTimer += Time.deltaTime * 8f;
        float pulse = (Mathf.Sin(pulseTimer) + 1f) * 0.5f;
        float alphaCap = resourceScanHighlight
            ? resourceScanAlpha
            : scanFlashActive
                ? scanFlashAlpha
                : (postScanActive && !liveScannerHighlight ? postScanAlpha : alpha);
        float targetAlpha = alphaCap * strength * (0.85f + 0.15f * pulse);
        ApplyOutlineVisible(Mathf.Min(alphaCap, targetAlpha), displayColor);
    }

    /// <summary>
    /// Sustained low-alpha highlight for mining multi-tool F-scan (works on ResourceNodes with scannerOnlyOutline).
    /// </summary>
    public void SetResourceScanHighlight(bool highlighted, Color color, float highlightAlpha)
    {
        resourceScanHighlight = highlighted;
        if (highlighted)
        {
            resourceScanColor = color;
            resourceScanAlpha = Mathf.Clamp01(highlightAlpha);
        }
        else if (!liveScannerHighlight && !postScanActive && !scanFlashActive)
        {
            ApplyOutlineVisible(0f, outlineColor);
        }
    }

    public void ClearResourceScanHighlight()
    {
        SetResourceScanHighlight(false, outlineColor, 0f);
    }

    public void SetScannerHighlight(bool highlighted, float intensity = 1f)
    {
        if (scannerOnlyOutline)
        {
            liveScannerHighlight = false;
            liveScannerIntensity = 0f;
            return;
        }

        liveScannerHighlight = highlighted;
        liveScannerIntensity = highlighted ? Mathf.Clamp01(intensity) : 0f;

        if (!highlighted && !postScanActive && !scanFlashActive)
            ApplyOutlineVisible(0f, outlineColor);
    }

    public void ClearScannerHighlight()
    {
        SetScannerHighlight(false, 0f);
    }

    public void SetPostScanHighlight(Color color, float highlightAlpha, float durationSeconds, int priority = PriorityPostScan)
    {
        if (scannerOnlyOutline)
        {
            PlayScanDiscoveryFlash(color, highlightAlpha);
            return;
        }

        if (priority < postScanPriority && postScanActive && Time.unscaledTime < postScanExpireTime)
            return;

        postScanActive = true;
        postScanColor = color;
        postScanAlpha = Mathf.Clamp01(highlightAlpha);
        postScanPriority = priority;
        postScanExpireTime = Time.unscaledTime + Mathf.Max(0.5f, durationSeconds);
    }

    /// <summary>Lerp outline on/off <paramref name="pulses"/> times over <paramref name="durationSeconds"/>.</summary>
    public void PlayScanDiscoveryFlash(Color color, float highlightAlpha, int pulses = 5, float durationSeconds = 2.5f)
    {
        ClearPostScanHighlight();
        liveScannerHighlight = false;
        liveScannerIntensity = 0f;

        scanFlashActive = true;
        scanFlashColor = color;
        scanFlashAlpha = Mathf.Clamp01(highlightAlpha);
        scanFlashPulses = Mathf.Max(1, pulses);
        scanFlashDuration = Mathf.Max(0.25f, durationSeconds);
        scanFlashStartTime = Time.unscaledTime;
    }

    public void ClearScanDiscoveryFlash()
    {
        scanFlashActive = false;
        if (!liveScannerHighlight && !postScanActive)
            ApplyOutlineVisible(0f, outlineColor);
    }

    public void ClearPostScanHighlight()
    {
        postScanActive = false;
        postScanPriority = 0;
        postScanExpireTime = 0f;

        if (!liveScannerHighlight && !scanFlashActive)
            ApplyOutlineVisible(0f, outlineColor);
    }

    public void ClearAllHighlights()
    {
        liveScannerHighlight = false;
        liveScannerIntensity = 0f;
        resourceScanHighlight = false;
        resourceScanAlpha = 0f;
        ClearPostScanHighlight();
        ClearScanDiscoveryFlash();
        ApplyOutlineVisible(0f, outlineColor);
    }

    public float GetScannerIntensityForDistance(float distance)
    {
        float falloff = Mathf.Max(1f, scannerFalloffDistance);
        return 1f - Mathf.Clamp01(distance / falloff);
    }

    private void AutoDetectScannerOnlyOutline()
    {
        if (scannerOnlyOutline)
            return;

        if (GetComponent("ScannableTarget") != null
            || GetComponent("ItemPickup") != null
            || GetComponent("ResourceNode") != null
            || GetComponent("DmEvents") != null)
        {
            scannerOnlyOutline = true;
        }
    }

    private float EvaluateScanFlashStrength()
    {
        float elapsed = Time.unscaledTime - scanFlashStartTime;
        if (elapsed < 0f || elapsed >= scanFlashDuration)
            return 0f;

        float t = elapsed / scanFlashDuration;
        float cycle = t * scanFlashPulses;
        float local = cycle - Mathf.Floor(cycle);
        // Triangle lerp 0 -> 1 -> 0 within each pulse.
        return local < 0.5f ? local * 2f : (1f - local) * 2f;
    }

    private float EvaluatePostScanStrength()
    {
        Transform playerTransform = resolvedPlayer;
        if (playerTransform == null)
            return 1f;

        float distance = Vector3.Distance(playerTransform.position, GetHighlightPoint());
        return GetScannerIntensityForDistance(distance);
    }

    private float EvaluateLookAtStrength()
    {
        if (scannerOnlyOutline)
            return 0f;

        if (resolvedPlayer == null || lookRange <= 0f)
            return 0f;

        // Cheap transform distance first — avoid Renderer.bounds unless inside range.
        float rangeSqr = lookRange * lookRange;
        if ((resolvedPlayer.position - transform.position).sqrMagnitude > rangeSqr * 2.25f)
            return 0f;

        Vector3 itemPoint = GetHighlightPoint();
        if ((resolvedPlayer.position - itemPoint).sqrMagnitude > rangeSqr)
            return 0f;

        Transform view = playerCamera != null ? playerCamera.transform : resolvedPlayer;
        Vector3 toItem = itemPoint - view.position;
        if (toItem.sqrMagnitude < 0.0001f)
            return 1f;

        float lookDot = Vector3.Dot(view.forward, toItem.normalized);
        if (lookDot < lookDotThreshold)
            return 0f;

        return 1f;
    }

    private Vector3 GetHighlightPoint()
    {
        if (rend != null)
            return rend.bounds.center;
        return transform.position;
    }

    private void ResolvePlayer()
    {
        if (resolvedPlayer != null && playerCamera != null)
            return;

        if (player != null)
        {
            resolvedPlayer = player;
        }
        else
        {
            resolvedPlayer = PlayerReference.ResolveTransform();
            if (resolvedPlayer == null)
                return;
        }

        if (playerCamera == null)
        {
            playerCamera = PlayerReference.Camera;
            if (playerCamera == null && resolvedPlayer != null)
                playerCamera = resolvedPlayer.GetComponentInChildren<Camera>();
            if (playerCamera == null)
                playerCamera = Camera.main;
        }
    }

    private void CacheRenderer()
    {
        if (rend != null)
            return;

        rend = GetComponent<Renderer>();
        if (rend == null)
            rend = GetComponentInChildren<Renderer>(true);
        if (rend != null)
            baseMaterials = rend.sharedMaterials;
    }

    private static Material GetSharedOutlineMaterial()
    {
        if (sharedOutlineMaterial != null)
            return sharedOutlineMaterial;

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            return null;

        sharedOutlineMaterial = new Material(shader) { name = "OutlineDM_Shared" };
        sharedOutlineMaterial.SetInt("_ZTest", 0);
        sharedOutlineMaterial.SetInt("_Cull", 2);
        sharedOutlineMaterial.SetInt("_ZWrite", 0);
        return sharedOutlineMaterial;
    }

    private void EnsureOutlineSlot()
    {
        CacheRenderer();
        if (rend == null)
            return;

        Material shared = GetSharedOutlineMaterial();
        if (shared == null)
            return;

        outlineMaterial = shared;
        if (outlineSlotApplied)
            return;

        Material[] sharedMats = baseMaterials != null && baseMaterials.Length > 0
            ? baseMaterials
            : rend.sharedMaterials;
        if (sharedMats == null)
            return;

        for (int i = 0; i < sharedMats.Length; i++)
        {
            if (sharedMats[i] == shared)
            {
                outlineSlotApplied = true;
                outlineMaterialIndex = i;
                return;
            }
        }

        Material[] mats = new Material[sharedMats.Length + 1];
        sharedMats.CopyTo(mats, 0);
        mats[mats.Length - 1] = shared;
        rend.materials = mats;
        outlineSlotApplied = true;
        outlineMaterialIndex = mats.Length - 1;
    }

    private void RemoveOutlineSlot()
    {
        if (!outlineSlotApplied || rend == null || outlineMaterialIndex < 0)
            return;

        Material[] current = rend.materials;
        if (current == null || outlineMaterialIndex >= current.Length)
        {
            outlineSlotApplied = false;
            outlineMaterialIndex = -1;
            return;
        }

        Material[] trimmed = new Material[current.Length - 1];
        int write = 0;
        for (int i = 0; i < current.Length; i++)
        {
            if (i == outlineMaterialIndex)
                continue;
            trimmed[write++] = current[i];
        }

        rend.materials = trimmed;
        outlineSlotApplied = false;
        outlineMaterialIndex = -1;
    }

    private void ApplyOutlineVisible(float targetAlpha, Color displayColor)
    {
        if (targetAlpha <= 0.001f)
        {
            // Keep the outline material slot while hidden — removing/re-adding allocates Material[]
            // every time look-at strength flickers across the threshold.
            if (!outlineSlotApplied || rend == null || outlineMaterialIndex < 0)
                return;

            if (lastAppliedOutlineAlpha <= 0.001f)
                return;

            Color hidden = displayColor;
            hidden.a = 0f;
            propertyBlock.SetColor(ColorId, hidden);
            propertyBlock.SetColor(BaseColorId, hidden);
            rend.SetPropertyBlock(propertyBlock, outlineMaterialIndex);
            lastAppliedOutlineAlpha = 0f;
            lastAppliedOutlineColor = hidden;
            return;
        }

        EnsureOutlineSlot();
        if (rend == null || outlineMaterialIndex < 0)
            return;

        Color color = displayColor;
        color.a = Mathf.Clamp01(targetAlpha);
        if (Mathf.Abs(color.a - lastAppliedOutlineAlpha) < 0.002f
            && ColorsApproximatelyEqual(color, lastAppliedOutlineColor))
        {
            return;
        }

        propertyBlock.SetColor(ColorId, color);
        propertyBlock.SetColor(BaseColorId, color);
        rend.SetPropertyBlock(propertyBlock, outlineMaterialIndex);
        lastAppliedOutlineAlpha = color.a;
        lastAppliedOutlineColor = color;
    }

    private static bool ColorsApproximatelyEqual(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.002f
            && Mathf.Abs(a.g - b.g) < 0.002f
            && Mathf.Abs(a.b - b.b) < 0.002f
            && Mathf.Abs(a.a - b.a) < 0.002f;
    }
}

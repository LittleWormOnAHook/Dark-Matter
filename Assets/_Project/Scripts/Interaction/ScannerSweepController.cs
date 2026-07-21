using System.Collections;
using System.Collections.Generic;
using Project.Inventory;
using Project.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Interaction
{
    /// <summary>
    /// Middle-mouse terrain sweep while scanner optics are active.
    /// Applies tag-colored post-scan outlines via OutlineController.
    /// </summary>
    public class ScannerSweepController : MonoBehaviour
    {
        [SerializeField] private LayerMask sweepLayers = ~0;
        [SerializeField] private ScannerHighlightProfile profile;
        [SerializeField] private int maxSweepHits = 48;
        [SerializeField] private Color sweepPulseColor = new Color(0.35f, 1f, 0.82f, 0.65f);

        private OpticsController opticsController;
        private EquipmentController equipment;
        private readonly List<OpticsScanTarget> postScanResults = new List<OpticsScanTarget>(48);
        private readonly HashSet<OutlineController> postScanOutlines = new HashSet<OutlineController>();
        private readonly HashSet<int> postScanKeys = new HashSet<int>();
        private LineRenderer sweepPulseLine;
        private Coroutine sweepRoutine;
        private bool sweepPulseBuilt;

        public IReadOnlyList<OpticsScanTarget> PostScanResults => postScanResults;
        public bool IsSweeping => sweepRoutine != null;

        private void Awake()
        {
            opticsController = GetComponent<OpticsController>();
            equipment = GetComponent<EquipmentController>();
            if (profile == null)
                profile = ScannerHighlightProfile.Load();
        }

        public void TickScannerInput(bool scannerActive, Camera viewCamera)
        {
            if (!scannerActive || viewCamera == null || Mouse.current == null)
                return;

            if (!Mouse.current.middleButton.wasPressedThisFrame || sweepRoutine != null)
                return;

            sweepRoutine = StartCoroutine(RunSweep(viewCamera));
        }

        public void ClearPostScanHighlights()
        {
            foreach (OutlineController outline in postScanOutlines)
            {
                if (outline != null)
                    outline.ClearPostScanHighlight();
            }

            postScanOutlines.Clear();
            postScanResults.Clear();
            postScanKeys.Clear();
        }

        private IEnumerator RunSweep(Camera viewCamera)
        {
            ScannerHighlightProfile activeProfile = profile != null
                ? profile
                : ScannerHighlightProfile.Load();

            float range = activeProfile.EffectiveSweepRange;
            float duration = Mathf.Max(0.2f, activeProfile.sweepDuration);
            int steps = Mathf.Max(4, activeProfile.sweepSampleSteps);
            Vector3 origin = viewCamera.transform.position;

            ClearPostScanHighlights();
            EnsureSweepPulseVisual();
            sweepPulseLine.enabled = true;

            HashSet<Collider> discovered = new HashSet<Collider>();
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float currentRadius = range * t;

                UpdateSweepPulseVisual(origin, currentRadius, t);

                int hitCount = Physics.OverlapSphereNonAlloc(
                    origin,
                    currentRadius,
                    OpticsController.ScanHitBuffer,
                    sweepLayers,
                    QueryTriggerInteraction.Ignore);

                for (int i = 0; i < hitCount; i++)
                {
                    Collider hit = OpticsController.ScanHitBuffer[i];
                    if (hit == null)
                        continue;

                    if (!discovered.Add(hit))
                        continue;

                    RegisterSweepHit(hit, origin, activeProfile);
                }

                yield return null;
            }

            sweepPulseLine.enabled = false;
            sweepRoutine = null;
            opticsController?.NotifyPostScanUpdated();
        }

        private void RegisterSweepHit(Collider hit, Vector3 origin, ScannerHighlightProfile activeProfile)
        {
            if (hit == null || postScanResults.Count >= maxSweepHits)
                return;

            GameObject rootObject = hit.gameObject;
            OutlineController outline = OpticsController.ResolveOutlinePublic(hit.transform);
            if (outline == null)
                return;

            if (!ScannerHighlightResolver.TryResolve(rootObject, activeProfile, out ScannerHighlightRule rule, out string label))
                return;

            Vector3 point = outline.transform.position;
            Renderer renderer = outline.GetComponentInChildren<Renderer>();
            if (renderer != null)
                point = renderer.bounds.center;

            if (!opticsController.HasLineOfSightPublic(origin, point, outline.transform))
                return;

            int key = OpticsController.BuildScanKeyPublic(point, label);
            if (!postScanKeys.Add(key))
                return;

            float duration = rule.durationSeconds > 0f
                ? rule.durationSeconds
                : activeProfile.defaultPostScanDuration;

            outline.SetPostScanHighlight(rule.outlineColor, rule.alpha, duration, rule.priority);
            postScanOutlines.Add(outline);
            postScanResults.Add(new OpticsScanTarget(
                point,
                label,
                rule.outlineColor,
                outline,
                isPostScan: true));
        }

        private void EnsureSweepPulseVisual()
        {
            if (sweepPulseBuilt)
                return;

            GameObject pulseObject = new GameObject("ScannerSweepPulse");
            pulseObject.transform.SetParent(transform, false);
            sweepPulseLine = pulseObject.AddComponent<LineRenderer>();
            sweepPulseLine.useWorldSpace = true;
            sweepPulseLine.loop = true;
            sweepPulseLine.widthMultiplier = 0.08f;
            sweepPulseLine.positionCount = 48;
            sweepPulseLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sweepPulseLine.receiveShadows = false;
            sweepPulseLine.material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"))
            {
                color = sweepPulseColor
            };
            sweepPulseLine.startColor = sweepPulseColor;
            sweepPulseLine.endColor = sweepPulseColor;
            sweepPulseLine.enabled = false;
            sweepPulseBuilt = true;
        }

        private void UpdateSweepPulseVisual(Vector3 center, float radius, float pulseT)
        {
            if (sweepPulseLine == null)
                return;

            int segments = sweepPulseLine.positionCount;
            float alpha = Mathf.Lerp(0.85f, 0.1f, pulseT);
            Color color = sweepPulseColor;
            color.a = alpha;
            sweepPulseLine.startColor = color;
            sweepPulseLine.endColor = color;

            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                sweepPulseLine.SetPosition(i, center + offset);
            }
        }

        private void OnDestroy()
        {
            ClearPostScanHighlights();
        }
    }
}

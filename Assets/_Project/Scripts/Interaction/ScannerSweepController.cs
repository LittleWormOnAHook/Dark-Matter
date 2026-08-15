using System.Collections;
using System.Collections.Generic;
using Project.Map;
using Project.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Interaction
{
    /// <summary>
    /// Middle-mouse scanner sweep: gold filled disc (light center → heavy rim) that rides terrain,
    /// outlines scannables, and reveals fog of war.
    /// </summary>
    public class ScannerSweepController : MonoBehaviour
    {
        private const int RadialSegments = 64;
        private const int RadialRings = 6;
        private const float TopologyProbeHeight = 40f;
        private const float TopologyProbeDistance = 80f;
        private const float TopologySurfaceLift = 0.08f;
        private const float SweepGridSizeMeters = 0.75f;
        private const float SweepGridLineWidth = 0.045f;
        private const float SweepGridAlpha = 0.80f;

        [SerializeField] private LayerMask sweepLayers = ~0;
        [SerializeField] private LayerMask topologyLayers = ~0;
        [SerializeField] private ScannerHighlightProfile profile;
        [SerializeField] private int maxSweepHits = 48;

        private OpticsController opticsController;
        private readonly List<OpticsScanTarget> postScanResults = new List<OpticsScanTarget>(48);
        private readonly HashSet<OutlineController> postScanOutlines = new HashSet<OutlineController>();
        private readonly HashSet<int> postScanKeys = new HashSet<int>();
        private readonly RaycastHit[] topologyHits = new RaycastHit[8];

        private MeshFilter sweepMeshFilter;
        private MeshRenderer sweepMeshRenderer;
        private Mesh sweepMesh;
        private Vector3[] sweepVertices;
        private Color32[] sweepColors;
        private int[] sweepTriangles;
        private Material sweepMaterial;
        private Coroutine sweepRoutine;
        private bool sweepVisualBuilt;

        public IReadOnlyList<OpticsScanTarget> PostScanResults => postScanResults;
        public bool IsSweeping => sweepRoutine != null;

        private void Awake()
        {
            opticsController = GetComponent<OpticsController>();
            if (profile == null)
                profile = ScannerHighlightProfile.Load();
        }

        public void TickScannerInput(bool scannerActive, Camera viewCamera)
        {
            if (!scannerActive || viewCamera == null || Mouse.current == null)
                return;

            if (!Mouse.current.middleButton.wasPressedThisFrame || sweepRoutine != null)
                return;

            sweepRoutine = StartCoroutine(RunSweep());
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

        private IEnumerator RunSweep()
        {
            ScannerHighlightProfile activeProfile = profile != null
                ? profile
                : ScannerHighlightProfile.Load();

            // Marker detect + fog reveal start at 40m, then +skill ranks.
            float range = MapFogOfWar.GetScanRevealRadius();
            // 50% slower than profile (half speed → 2× duration).
            float duration = Mathf.Max(0.2f, activeProfile.sweepDuration) * 2f;
            Vector3 origin = ResolvePlayerSweepOrigin();

            ClearPostScanHighlights();
            EnsureSweepDiscVisual();
            if (sweepMeshRenderer != null)
                sweepMeshRenderer.enabled = true;

            HashSet<Collider> discovered = new HashSet<Collider>();
            float elapsed = 0f;

            MapFogOfWar.EnsureExists();

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float currentRadius = range * t;
                origin = ResolvePlayerSweepOrigin();

                UpdateSweepDiscVisual(origin, currentRadius, t);

                // Soft fog reveal grows with the pulse.
                MapFogOfWar.Instance?.RevealCircle(origin, currentRadius, edgeSoftnessMeters: 5f);

                int hitCount = Physics.OverlapSphereNonAlloc(
                    origin,
                    currentRadius,
                    OpticsController.ScanHitBuffer,
                    sweepLayers,
                    QueryTriggerInteraction.Ignore);

                for (int i = 0; i < hitCount; i++)
                {
                    Collider hit = OpticsController.ScanHitBuffer[i];
                    if (hit == null || !discovered.Add(hit))
                        continue;

                    RegisterSweepHit(hit, origin, activeProfile);
                }

                yield return null;
            }

            MapFogOfWar.Instance?.RevealScanAt(origin);

            if (sweepMeshRenderer != null)
                sweepMeshRenderer.enabled = false;

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

            // World items: outline only via scan flash; unlock map marker on first discovery.
            outline.scannerOnlyOutline = true;
            outline.PlayScanDiscoveryFlash(rule.outlineColor, rule.alpha, pulses: 5, durationSeconds: 2.5f);
            DiscoverHitOnMap(outline.gameObject, label, rule.outlineColor);

            postScanOutlines.Add(outline);
            postScanResults.Add(new OpticsScanTarget(
                point,
                label,
                rule.outlineColor,
                outline,
                isPostScan: true));
        }

        private static void DiscoverHitOnMap(GameObject root, string label, Color color)
        {
            if (root == null)
                return;

            MapMarker marker = root.GetComponentInParent<MapMarker>();
            if (marker == null)
            {
                marker = root.AddComponent<MapMarker>();

                ResourceNode node = root.GetComponentInParent<ResourceNode>();
                if (node != null && node.resourceItem != null)
                    marker.ConfigureForResource(node.resourceItem);
                else
                {
                    ItemPickup pickup = root.GetComponentInParent<ItemPickup>();
                    if (pickup != null && pickup.itemData != null)
                        marker.ConfigureForResource(pickup.itemData);
                    else
                        marker.ConfigureScannedPoi(label, color);
                }
            }

            ScannerDiscoveryRegistry.Discover(marker.DiscoveryId);
        }

        private void EnsureSweepDiscVisual()
        {
            if (sweepVisualBuilt)
                return;

            GameObject pulseObject = new GameObject("ScannerSweepDisc");
            pulseObject.transform.SetParent(transform, false);
            sweepMeshFilter = pulseObject.AddComponent<MeshFilter>();
            sweepMeshRenderer = pulseObject.AddComponent<MeshRenderer>();
            sweepMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sweepMeshRenderer.receiveShadows = false;
            sweepMeshRenderer.enabled = false;

            Shader shader = Shader.Find("Project/ScannerSweepDisc")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color");

            sweepMaterial = new Material(shader)
            {
                name = "ScannerSweepDiscMat",
                hideFlags = HideFlags.DontSave,
                color = DarkMatterGenesisUiPalette.Gold
            };

            ApplySweepMaterialDefaults(sweepMaterial);
            sweepMeshRenderer.sharedMaterial = sweepMaterial;

            BuildSweepMeshTopology();
            sweepMeshFilter.sharedMesh = sweepMesh;
            sweepVisualBuilt = true;
        }

        private void BuildSweepMeshTopology()
        {
            int vertexCount = 1 + RadialSegments * RadialRings;
            sweepVertices = new Vector3[vertexCount];
            sweepColors = new Color32[vertexCount];

            List<int> tris = new List<int>(RadialSegments * RadialRings * 6);
            // Center is index 0. Rings 1..RadialRings.
            for (int ring = 0; ring < RadialRings; ring++)
            {
                int innerStart = ring == 0 ? 0 : 1 + (ring - 1) * RadialSegments;
                int outerStart = 1 + ring * RadialSegments;
                bool innerIsCenter = ring == 0;

                for (int seg = 0; seg < RadialSegments; seg++)
                {
                    int next = (seg + 1) % RadialSegments;
                    if (innerIsCenter)
                    {
                        tris.Add(0);
                        tris.Add(outerStart + seg);
                        tris.Add(outerStart + next);
                    }
                    else
                    {
                        int i0 = innerStart + seg;
                        int i1 = innerStart + next;
                        int o0 = outerStart + seg;
                        int o1 = outerStart + next;
                        tris.Add(i0);
                        tris.Add(o0);
                        tris.Add(o1);
                        tris.Add(i0);
                        tris.Add(o1);
                        tris.Add(i1);
                    }
                }
            }

            sweepTriangles = tris.ToArray();
            sweepMesh = new Mesh { name = "ScannerSweepDiscMesh", hideFlags = HideFlags.DontSave };
            sweepMesh.MarkDynamic();
            sweepMesh.vertices = sweepVertices;
            sweepMesh.colors32 = sweepColors;
            sweepMesh.triangles = sweepTriangles;
        }

        private void UpdateSweepDiscVisual(Vector3 center, float radius, float pulseT)
        {
            if (sweepMesh == null || sweepVertices == null)
                return;

            // Scan disc fill at ~80% alpha (slight fade as the pulse finishes).
            float overallAlpha = Mathf.Lerp(0.80f, 0.55f, pulseT);
            Color gold = DarkMatterGenesisUiPalette.Gold;

            Vector3 centerSurface = SampleTopologyPoint(
                center + Vector3.up * TopologyProbeHeight,
                center.y);
            // Mesh is parented to the player — store local verts so the sweep stays centered.
            sweepVertices[0] = transform.InverseTransformPoint(centerSurface);
            sweepColors[0] = ToColor32(gold, 0.35f * overallAlpha);

            for (int ring = 1; ring <= RadialRings; ring++)
            {
                float ringT = ring / (float)RadialRings;
                float ringRadius = radius * ringT;
                // Heavier toward outer circumference, peak ~80% via overallAlpha.
                float ringAlpha = Mathf.Lerp(0.40f, 1f, Mathf.Pow(ringT, 1.35f)) * overallAlpha;

                int ringStart = 1 + (ring - 1) * RadialSegments;
                for (int seg = 0; seg < RadialSegments; seg++)
                {
                    float angle = seg / (float)RadialSegments * Mathf.PI * 2f;
                    Vector3 planar = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ringRadius;
                    Vector3 probeOrigin = center + planar + Vector3.up * TopologyProbeHeight;
                    Vector3 surface = SampleTopologyPoint(probeOrigin, center.y);
                    sweepVertices[ringStart + seg] = transform.InverseTransformPoint(surface);
                    sweepColors[ringStart + seg] = ToColor32(gold, ringAlpha);
                }
            }

            sweepMesh.vertices = sweepVertices;
            sweepMesh.colors32 = sweepColors;
            sweepMesh.RecalculateBounds();

            if (sweepMaterial != null)
                ApplySweepMaterialDefaults(sweepMaterial);
        }

        private static void ApplySweepMaterialDefaults(Material material)
        {
            if (material == null)
                return;

            Color gold = DarkMatterGenesisUiPalette.Gold;
            gold.a = 1f;
            Color grid = Color.Lerp(gold, Color.white, 0.35f);
            grid.a = 1f;

            material.color = gold;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", gold);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", gold);
            if (material.HasProperty("_GridColor"))
                material.SetColor("_GridColor", grid);
            if (material.HasProperty("_GridSize"))
                material.SetFloat("_GridSize", SweepGridSizeMeters);
            if (material.HasProperty("_GridLineWidth"))
                material.SetFloat("_GridLineWidth", SweepGridLineWidth);
            if (material.HasProperty("_GridAlpha"))
                material.SetFloat("_GridAlpha", SweepGridAlpha);
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);

            material.renderQueue = 3000;
        }

        private static Color32 ToColor32(Color color, float alpha)
        {
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255));
        }

        private Vector3 SampleTopologyPoint(Vector3 probeOrigin, float fallbackY)
        {
            int hitCount = Physics.RaycastNonAlloc(
                probeOrigin,
                Vector3.down,
                topologyHits,
                TopologyProbeDistance,
                topologyLayers,
                QueryTriggerInteraction.Ignore);

            float bestDistance = float.MaxValue;
            Vector3 bestPoint = new Vector3(probeOrigin.x, fallbackY, probeOrigin.z);
            bool found = false;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = topologyHits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                    continue;

                if (hit.distance >= bestDistance)
                    continue;

                bestDistance = hit.distance;
                bestPoint = hit.point;
                found = true;
            }

            if (!found)
                return new Vector3(probeOrigin.x, fallbackY, probeOrigin.z) + Vector3.up * TopologySurfaceLift;

            return bestPoint + Vector3.up * TopologySurfaceLift;
        }

        private Vector3 ResolvePlayerSweepOrigin()
        {
            // Always center on this player root (Optics/Scanner live on the player).
            Vector3 playerPos = transform.position;
            return SampleTopologyPoint(playerPos + Vector3.up * TopologyProbeHeight, playerPos.y);
        }

        private void OnDestroy()
        {
            ClearPostScanHighlights();
            if (sweepMesh != null)
                Destroy(sweepMesh);
            if (sweepMaterial != null)
                Destroy(sweepMaterial);
        }
    }
}

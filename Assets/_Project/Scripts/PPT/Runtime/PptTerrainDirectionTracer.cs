using System.Collections;
using System.Collections.Generic;
using Project.UI;
using UnityEngine;

namespace Project.PPT
{
    /// <summary>
    /// Spawns a lime terrain-hugging direction tracer that curves around scene colliders.
    /// Uses unscaled time so slow-mo menus do not desync the hold.
    /// </summary>
    public sealed class PptTerrainDirectionTracer : MonoBehaviour
    {
        private const float DefaultDurationSeconds = 5f;
        public const float DefaultVisibleSeconds = DefaultDurationSeconds;
        private const float SampleSpacingMeters = 1.25f;
        private const float GroundOffsetMeters = 0.15f;
        private const float ObstacleProbeRadius = 0.35f;
        private const float MaxStepUpMeters = 1.5f;

        private static readonly int DefaultLayerMask = Physics.DefaultRaycastLayers;

        private LineRenderer lineRenderer;
        private Vector3[] pathPoints;
        private float elapsed;
        private float durationSeconds = DefaultDurationSeconds;

        private const float TerrainStartDistanceMeters = 1f;

        public static void Spawn(Vector3 npcAnchor, Vector3 aimPosition, float minHoldRealtimeSeconds = 0f)
        {
            GameObject host = new GameObject("PptDirectionTracer");
            PptTerrainDirectionTracer tracer = host.AddComponent<PptTerrainDirectionTracer>();
            Vector3 from = ResolveTerrainStartPoint(npcAnchor, aimPosition, TerrainStartDistanceMeters);
            Vector3 to = SnapToGround(aimPosition);
            tracer.Initialize(from, to, minHoldRealtimeSeconds);
        }

        /// <summary>
        /// Horizontal point on terrain <paramref name="distanceMeters"/> from the NPC toward the aim.
        /// </summary>
        public static Vector3 ResolveTerrainStartPoint(Vector3 npcAnchor, Vector3 aimPosition, float distanceMeters)
        {
            Vector3 flatDirection = aimPosition - npcAnchor;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude < 0.0001f)
                flatDirection = Vector3.forward;
            else
                flatDirection.Normalize();

            Vector3 horizontalStart = npcAnchor + flatDirection * distanceMeters;
            return SnapToGround(horizontalStart);
        }

        private void Initialize(Vector3 from, Vector3 to, float minHoldRealtimeSeconds)
        {
            durationSeconds = Mathf.Max(DefaultDurationSeconds, minHoldRealtimeSeconds + 0.75f);

            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.widthMultiplier = 0.08f;
            lineRenderer.numCapVertices = 4;
            lineRenderer.numCornerVertices = 4;
            lineRenderer.positionCount = 0;
            Material tracerMaterial = CreateTracerMaterial();
            if (tracerMaterial != null)
                lineRenderer.material = tracerMaterial;
            lineRenderer.startColor = DarkMatterGenesisUiPalette.PositiveGreen;
            lineRenderer.endColor = DarkMatterGenesisUiPalette.PositiveGreen;
            lineRenderer.textureMode = LineTextureMode.Stretch;

            pathPoints = BuildTerrainHuggingPath(from, to);
            lineRenderer.positionCount = pathPoints.Length;
            for (int i = 0; i < pathPoints.Length; i++)
                lineRenderer.SetPosition(i, pathPoints[0]);

            StartCoroutine(AnimateAndDestroy());
        }

        private IEnumerator AnimateAndDestroy()
        {
            while (elapsed < durationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / durationSeconds);
                int visibleCount = Mathf.Max(2, Mathf.CeilToInt(pathPoints.Length * t));
                lineRenderer.positionCount = visibleCount;
                for (int i = 0; i < visibleCount; i++)
                    lineRenderer.SetPosition(i, pathPoints[i]);

                yield return null;
            }

            Destroy(gameObject);
        }

        private static Vector3[] BuildTerrainHuggingPath(Vector3 from, Vector3 to)
        {
            var points = new List<Vector3>(32);
            Vector3 cursor = SnapToGround(from);
            points.Add(cursor);

            Vector3 goal = SnapToGround(to);
            int safety = 0;
            while (Vector3.Distance(cursor, goal) > SampleSpacingMeters && safety < 64)
            {
                safety++;
                Vector3 stepDirection = (goal - cursor);
                stepDirection.y = 0f;
                if (stepDirection.sqrMagnitude < 0.01f)
                    break;

                stepDirection.Normalize();
                Vector3 desired = cursor + stepDirection * SampleSpacingMeters;
                desired = SnapToGround(desired);

                if (Physics.SphereCast(cursor + Vector3.up * 0.5f, ObstacleProbeRadius, stepDirection, out RaycastHit hit,
                        SampleSpacingMeters + 0.25f, DefaultLayerMask, QueryTriggerInteraction.Ignore))
                {
                    Vector3 detour = Vector3.Cross(Vector3.up, hit.normal);
                    if (Vector3.Dot(detour, stepDirection) < 0f)
                        detour = -detour;

                    detour.Normalize();
                    desired = cursor + detour * SampleSpacingMeters;
                    desired = SnapToGround(desired);
                }

                if (Vector3.Distance(desired, cursor) < 0.1f)
                    break;

                cursor = desired;
                points.Add(cursor);
            }

            points.Add(goal);
            return points.ToArray();
        }

        private static Vector3 SnapToGround(Vector3 point)
        {
            Vector3 probeOrigin = point + Vector3.up * MaxStepUpMeters;
            if (Physics.Raycast(probeOrigin, Vector3.down, out RaycastHit hit, MaxStepUpMeters * 2f,
                    DefaultLayerMask, QueryTriggerInteraction.Ignore))
            {
                point.y = hit.point.y + GroundOffsetMeters;
                return point;
            }

            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                float y = terrain.SampleHeight(point) + terrain.transform.position.y;
                point.y = y + GroundOffsetMeters;
            }

            return point;
        }

        private static Material CreateTracerMaterial()
        {
            Shader shader = Shader.Find("HDRP/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
                return null;

            Material material = new Material(shader);
            Color lime = DarkMatterGenesisUiPalette.PositiveGreen;
            material.color = lime;
            if (material.HasProperty("_UnlitColor"))
                material.SetColor("_UnlitColor", lime);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", lime);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", lime);
            return material;
        }
    }
}

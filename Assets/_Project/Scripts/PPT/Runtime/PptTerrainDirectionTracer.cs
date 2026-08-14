using System.Collections;
using System.Collections.Generic;
using Project.UI;
using UnityEngine;

namespace Project.PPT
{
    /// <summary>
    /// Spawns a 3-second lime terrain-hugging direction tracer that curves around scene colliders.
    /// </summary>
    public sealed class PptTerrainDirectionTracer : MonoBehaviour
    {
        private const float DurationSeconds = 3f;
        private const float SampleSpacingMeters = 1.25f;
        private const float GroundOffsetMeters = 0.15f;
        private const float ObstacleProbeRadius = 0.35f;
        private const float MaxStepUpMeters = 1.5f;

        private static readonly int DefaultLayerMask = Physics.DefaultRaycastLayers;

        private LineRenderer lineRenderer;
        private Vector3[] pathPoints;
        private float elapsed;

        public static void Spawn(Vector3 from, Vector3 to)
        {
            GameObject host = new GameObject("PptDirectionTracer");
            PptTerrainDirectionTracer tracer = host.AddComponent<PptTerrainDirectionTracer>();
            tracer.Initialize(from, to);
        }

        private void Initialize(Vector3 from, Vector3 to)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            lineRenderer.widthMultiplier = 0.08f;
            lineRenderer.numCapVertices = 4;
            lineRenderer.numCornerVertices = 4;
            lineRenderer.positionCount = 0;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = SurvivalPioneerUiPalette.PositiveGreen;
            lineRenderer.endColor = SurvivalPioneerUiPalette.PositiveGreen;
            lineRenderer.textureMode = LineTextureMode.Stretch;

            pathPoints = BuildTerrainHuggingPath(from, to);
            lineRenderer.positionCount = pathPoints.Length;
            for (int i = 0; i < pathPoints.Length; i++)
                lineRenderer.SetPosition(i, pathPoints[0]);

            StartCoroutine(AnimateAndDestroy());
        }

        private IEnumerator AnimateAndDestroy()
        {
            while (elapsed < DurationSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / DurationSeconds);
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
    }
}

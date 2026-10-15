using System.Collections.Generic;
using UnityEngine;

namespace Project.Diagnostics
{
    /// <summary>
    /// Fixed-size ring buffer of recent world positions. Intended for debug visualization and
    /// future offline path analysis — not shipped gameplay logic yet.
    /// </summary>
    public sealed class DMMovementPathHistoryBuffer
    {
        public const int DefaultCapacity = 64;

        private readonly Vector3[] points;
        private int head;
        private int count;
        private float nextSampleTime;
        private Vector3 lastSample;
        private bool hasLastSample;

        public int Count => count;
        public int Capacity => points.Length;

        public DMMovementPathHistoryBuffer(int capacity = DefaultCapacity)
        {
            int cap = Mathf.Clamp(capacity, 8, 128);
            points = new Vector3[cap];
        }

        public void Clear()
        {
            head = 0;
            count = 0;
            nextSampleTime = 0f;
            hasLastSample = false;
        }

        public void Record(Vector3 worldPosition, float minMoveDistance, float minIntervalSeconds)
        {
            if (!Application.isPlaying)
                return;

            float now = Time.unscaledTime;
            if (hasLastSample)
            {
                Vector3 delta = worldPosition - lastSample;
                delta.y = 0f;
                if (now < nextSampleTime && delta.sqrMagnitude < minMoveDistance * minMoveDistance)
                    return;
            }

            Push(worldPosition);
            lastSample = worldPosition;
            hasLastSample = true;
            nextSampleTime = now + Mathf.Max(0.05f, minIntervalSeconds);
        }

        private void Push(Vector3 worldPosition)
        {
            points[head] = worldPosition;
            head = (head + 1) % points.Length;
            if (count < points.Length)
                count++;
        }

        public void DrawDebugLines(Color color, float yLift, float durationSeconds)
        {
            if (count < 2)
                return;

            int start = (head - count + points.Length) % points.Length;
            for (int i = 1; i < count; i++)
            {
                Vector3 a = Lift(points[(start + i - 1) % points.Length], yLift);
                Vector3 b = Lift(points[(start + i) % points.Length], yLift);
                Debug.DrawLine(a, b, color, durationSeconds, depthTest: false);
            }
        }

#if UNITY_EDITOR
        public void DrawGizmos(Color color, float yLift)
        {
            if (count < 2)
                return;

            Gizmos.color = color;
            int start = (head - count + points.Length) % points.Length;
            for (int i = 1; i < count; i++)
            {
                Vector3 a = Lift(points[(start + i - 1) % points.Length], yLift);
                Vector3 b = Lift(points[(start + i) % points.Length], yLift);
                Gizmos.DrawLine(a, b);
            }
        }
#endif

        private static Vector3 Lift(Vector3 p, float yLift) => p + Vector3.up * yLift;

        /// <summary>Debug-only dashed polyline (e.g. virtual steer intent).</summary>
        public static void DrawPolylineDashed(
            IReadOnlyList<Vector3> points,
            Color color,
            float yLift,
            float durationSeconds,
            float dashLength = 0.35f,
            float gapLength = 0.22f)
        {
            if (points == null || points.Count < 2)
                return;

            dashLength = Mathf.Max(0.05f, dashLength);
            gapLength = Mathf.Max(0.05f, gapLength);

            for (int i = 1; i < points.Count; i++)
            {
                Vector3 a = Lift(points[i - 1], yLift);
                Vector3 b = Lift(points[i], yLift);
                Vector3 seg = b - a;
                float len = seg.magnitude;
                if (len < 0.001f)
                    continue;

                Vector3 dir = seg / len;
                float t = 0f;
                while (t < len)
                {
                    float dashEnd = Mathf.Min(t + dashLength, len);
                    Debug.DrawLine(a + dir * t, a + dir * dashEnd, color, durationSeconds, depthTest: false);
                    t = dashEnd + gapLength;
                }
            }
        }

#if UNITY_EDITOR
        public static void DrawPolylineDashedGizmos(
            IReadOnlyList<Vector3> points,
            Color color,
            float yLift,
            float dashLength = 0.35f,
            float gapLength = 0.22f)
        {
            if (points == null || points.Count < 2)
                return;

            Gizmos.color = color;
            dashLength = Mathf.Max(0.05f, dashLength);
            gapLength = Mathf.Max(0.05f, gapLength);

            for (int i = 1; i < points.Count; i++)
            {
                Vector3 a = Lift(points[i - 1], yLift);
                Vector3 b = Lift(points[i], yLift);
                Vector3 seg = b - a;
                float len = seg.magnitude;
                if (len < 0.001f)
                    continue;

                Vector3 dir = seg / len;
                float t = 0f;
                while (t < len)
                {
                    float dashEnd = Mathf.Min(t + dashLength, len);
                    Gizmos.DrawLine(a + dir * t, a + dir * dashEnd);
                    t = dashEnd + gapLength;
                }
            }
        }
#endif
    }
}

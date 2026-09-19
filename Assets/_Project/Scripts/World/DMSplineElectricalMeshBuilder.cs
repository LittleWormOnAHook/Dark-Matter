using System.Collections.Generic;
using UnityEngine;

namespace Project.World
{
    /// <summary>
    /// Builds a loose rope-like strip mesh between spline points plus optional pole discs.
    /// Anchor junction samples stay exact; sag only affects interior span samples.
    /// </summary>
    public static class DMSplineElectricalMeshBuilder
    {
        public readonly struct BuildResult
        {
            public BuildResult(Mesh mesh, int[] pinnedStripSampleIndices)
            {
                Mesh = mesh;
                PinnedStripSampleIndices = pinnedStripSampleIndices;
            }

            public Mesh Mesh { get; }
            public int[] PinnedStripSampleIndices { get; }
        }

        /// <summary>
        /// Builds a strip along a dense polyline with optional catenary sag per segment.
        /// </summary>
        public static BuildResult BuildAlongPolyline(
            IReadOnlyList<Vector3> localPoints,
            IReadOnlyList<Vector3> localUps,
            float lineWidth,
            float sagMeters,
            Vector3 sagDirection,
            bool addDiscs,
            IReadOnlyList<Vector3> discCentersLocal,
            IReadOnlyList<Vector3> discNormalsLocal,
            float discRadius,
            int discSegments,
            float crossSectionTwistDegrees = 90f)
        {
            if (localPoints == null || localPoints.Count < 2)
                return default;

            discSegments = Mathf.Clamp(discSegments, 8, 64);
            sagDirection = NormalizeOrDown(sagDirection);

            var path = new List<Vector3>(localPoints.Count * 2);
            var pathUps = new List<Vector3>(localPoints.Count * 2);
            var pinned = new List<int>();

            for (int i = 0; i < localPoints.Count - 1; i++)
            {
                Vector3 a = localPoints[i];
                Vector3 b = localPoints[i + 1];
                Vector3 upA = ResolveUp(localUps, i);
                Vector3 upB = ResolveUp(localUps, i + 1);

                if (i == 0)
                {
                    pinned.Add(path.Count);
                    path.Add(a);
                    pathUps.Add(upA);
                }

                int steps = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(a, b) / 0.75f));
                for (int s = 1; s < steps; s++)
                {
                    float t = s / (float)steps;
                    path.Add(Vector3.Lerp(a, b, t) + sagDirection * (sagMeters * 4f * t * (1f - t)));
                    pathUps.Add(Vector3.Slerp(upA, upB, t));
                }

                pinned.Add(path.Count);
                path.Add(b);
                pathUps.Add(upB);
            }

            Mesh mesh = BuildStripMesh(path, pathUps, lineWidth, addDiscs, discCentersLocal, discNormalsLocal, discRadius, discSegments, crossSectionTwistDegrees);
            return new BuildResult(mesh, pinned.ToArray());
        }

        public static BuildResult Build(
            IReadOnlyList<Vector3> anchorPointsLocal,
            IReadOnlyList<Vector3> anchorUpsLocal,
            float lineWidth,
            int segmentsPerSpan,
            float sagMeters,
            Vector3 sagDirection,
            bool addDiscAtPoints,
            float discRadius,
            int discSegments,
            float crossSectionTwistDegrees = 90f,
            bool connectEndToStart = false)
        {
            if (anchorPointsLocal == null || anchorPointsLocal.Count < 2)
                return default;

            segmentsPerSpan = Mathf.Max(2, segmentsPerSpan);
            discSegments = Mathf.Clamp(discSegments, 8, 64);
            sagDirection = NormalizeOrDown(sagDirection);

            int anchorCount = anchorPointsLocal.Count;
            int spanCount = connectEndToStart && anchorCount >= 3
                ? anchorCount
                : anchorCount - 1;

            var path = new List<Vector3>();
            var pathUps = new List<Vector3>();
            var pinned = new List<int>();

            for (int span = 0; span < spanCount; span++)
            {
                int indexA = span;
                int indexB = connectEndToStart && anchorCount >= 3
                    ? (span + 1) % anchorCount
                    : span + 1;

                Vector3 a = anchorPointsLocal[indexA];
                Vector3 b = anchorPointsLocal[indexB];
                Vector3 upA = ResolveUp(anchorUpsLocal, indexA);
                Vector3 upB = ResolveUp(anchorUpsLocal, indexB);

                if (span == 0)
                {
                    pinned.Add(path.Count);
                    path.Add(a);
                    pathUps.Add(upA);
                }

                for (int s = 1; s < segmentsPerSpan; s++)
                {
                    float t = s / (float)segmentsPerSpan;
                    Vector3 p = Vector3.Lerp(a, b, t);
                    p += sagDirection * (sagMeters * 4f * t * (1f - t));
                    path.Add(p);
                    pathUps.Add(Vector3.Slerp(upA, upB, t));
                }

                pinned.Add(path.Count);
                path.Add(b);
                pathUps.Add(upB);
            }

            Mesh mesh = BuildStripMesh(
                path,
                pathUps,
                lineWidth,
                addDiscAtPoints,
                anchorPointsLocal,
                anchorUpsLocal,
                discRadius,
                discSegments,
                crossSectionTwistDegrees);

            return new BuildResult(mesh, pinned.ToArray());
        }

        private static Vector3 NormalizeOrDown(Vector3 sagDirection)
        {
            if (sagDirection.sqrMagnitude < 0.0001f)
                return Vector3.down;
            return sagDirection.normalized;
        }

        private static Vector3 ResolveUp(IReadOnlyList<Vector3> ups, int index)
        {
            if (ups != null && index >= 0 && index < ups.Count && ups[index].sqrMagnitude > 0.0001f)
                return ups[index].normalized;
            return Vector3.up;
        }

        private static Mesh BuildStripMesh(
            List<Vector3> path,
            List<Vector3> pathUps,
            float lineWidth,
            bool addDiscs,
            IReadOnlyList<Vector3> discCenters,
            IReadOnlyList<Vector3> discNormals,
            float discRadius,
            int discSegments,
            float crossSectionTwistDegrees)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            float pathLength = 0f;
            var cumulative = new float[path.Count];
            cumulative[0] = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                pathLength += Vector3.Distance(path[i - 1], path[i]);
                cumulative[i] = pathLength;
            }

            for (int i = 0; i < path.Count; i++)
            {
                Vector3 tangent = ResolveTangent(path, i);
                Vector3 up = ResolveUp(pathUps, i);
                Vector3 side = Vector3.Cross(tangent, up);
                if (side.sqrMagnitude < 0.0001f)
                    side = Vector3.Cross(tangent, Vector3.forward);
                side.Normalize();
                if (Mathf.Abs(crossSectionTwistDegrees) > 0.01f)
                    side = Quaternion.AngleAxis(crossSectionTwistDegrees, tangent) * side;
                side *= lineWidth * 0.5f;

                verts.Add(path[i] - side);
                verts.Add(path[i] + side);
                float u = pathLength > 0.001f ? cumulative[i] / pathLength : i / (float)Mathf.Max(1, path.Count - 1);
                uvs.Add(new Vector2(0f, u));
                uvs.Add(new Vector2(1f, u));
            }

            for (int i = 0; i < path.Count - 1; i++)
            {
                int vi = i * 2;
                tris.Add(vi);
                tris.Add(vi + 2);
                tris.Add(vi + 1);
                tris.Add(vi + 1);
                tris.Add(vi + 2);
                tris.Add(vi + 3);
            }

            if (addDiscs && discCenters != null)
            {
                for (int p = 0; p < discCenters.Count; p++)
                {
                    Vector3 normal = discNormals != null && p < discNormals.Count && discNormals[p].sqrMagnitude > 0.01f
                        ? discNormals[p].normalized
                        : Vector3.up;
                    AppendDisc(verts, uvs, tris, discCenters[p], normal, discRadius, discSegments);
                }
            }

            var mesh = new Mesh { name = "DMSplineElectricalLine" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 ResolveTangent(List<Vector3> path, int index)
        {
            if (path.Count < 2)
                return Vector3.forward;

            if (index <= 0)
                return (path[1] - path[0]).normalized;
            if (index >= path.Count - 1)
                return (path[index] - path[index - 1]).normalized;

            Vector3 a = (path[index] - path[index - 1]).normalized;
            Vector3 b = (path[index + 1] - path[index]).normalized;
            Vector3 sum = a + b;
            return sum.sqrMagnitude > 0.0001f ? sum.normalized : a;
        }

        private static void AppendDisc(
            List<Vector3> verts,
            List<Vector2> uvs,
            List<int> tris,
            Vector3 center,
            Vector3 normal,
            float radius,
            int segments)
        {
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < 0.0001f)
                tangent = Vector3.Cross(normal, Vector3.forward);
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(normal, tangent);

            int centerIndex = verts.Count;
            verts.Add(center);
            uvs.Add(new Vector2(0.5f, 0.5f));

            int ringStart = verts.Count;
            for (int i = 0; i < segments; i++)
            {
                float ang = (Mathf.PI * 2f * i) / segments;
                Vector3 offset = (Mathf.Cos(ang) * tangent + Mathf.Sin(ang) * bitangent) * radius;
                verts.Add(center + offset);
                uvs.Add(new Vector2(Mathf.Cos(ang) * 0.5f + 0.5f, Mathf.Sin(ang) * 0.5f + 0.5f));
            }

            for (int i = 0; i < segments; i++)
            {
                int a = ringStart + i;
                int b = ringStart + ((i + 1) % segments);
                tris.Add(centerIndex);
                tris.Add(b);
                tris.Add(a);
            }
        }
    }
}

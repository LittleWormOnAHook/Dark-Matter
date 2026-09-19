using UnityEngine;

namespace Project.World
{
    /// <summary>
    /// Lightweight procedural sway for survey wire / rope meshes baked by <see cref="DMSplineCreator"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class DMSplineRopeWiggle : MonoBehaviour
    {
        [SerializeField] private DMSplineCreator creator;
        [SerializeField] private MeshFilter meshFilter;

        public void Bind(DMSplineCreator owner, MeshFilter filter)
        {
            creator = owner;
            meshFilter = filter;
        }

        public void ApplyRestPose(Vector3[] restVertices, Vector3[] sampleBinormals)
        {
            if (meshFilter == null || meshFilter.sharedMesh == null || restVertices == null)
                return;

            meshFilter.sharedMesh.vertices = restVertices;
            meshFilter.sharedMesh.RecalculateBounds();
            meshFilter.sharedMesh.RecalculateNormals();
        }

        public void ApplyAnimatedPose(
            Vector3[] restVertices,
            Vector3[] sampleTangents,
            Vector3[] sampleBinormals,
            Vector3[] sampleUps,
            bool[] pinnedStripSamples,
            float time,
            Vector3 amplitudeDirectional,
            float frequency,
            float travelSpeed)
        {
            if (meshFilter == null || meshFilter.sharedMesh == null || restVertices == null || sampleBinormals == null)
                return;

            int sampleCount = sampleBinormals.Length;
            if (restVertices.Length < sampleCount * 2)
                return;

            var animated = new Vector3[restVertices.Length];
            for (int s = 0; s < sampleCount; s++)
            {
                int vi = s * 2;
                bool pinned = pinnedStripSamples != null && s < pinnedStripSamples.Length && pinnedStripSamples[s];
                if (pinned)
                {
                    animated[vi] = restVertices[vi];
                    animated[vi + 1] = restVertices[vi + 1];
                    continue;
                }

                float phase = time * travelSpeed + s * 0.45f;
                float wave = Mathf.Sin(phase * frequency);
                Vector3 tangent = sampleTangents != null && s < sampleTangents.Length ? sampleTangents[s] : Vector3.forward;
                Vector3 binormal = sampleBinormals[s];
                Vector3 up = sampleUps != null && s < sampleUps.Length ? sampleUps[s] : Vector3.up;

                Vector3 offset =
                    binormal * (wave * amplitudeDirectional.x) +
                    tangent * (Mathf.Sin(phase * frequency * 1.17f + 0.6f) * amplitudeDirectional.y) +
                    up * (Mathf.Sin(phase * frequency * 0.83f + 1.2f) * amplitudeDirectional.z);

                animated[vi] = restVertices[vi] + offset;
                animated[vi + 1] = restVertices[vi + 1] + offset;
            }

            meshFilter.sharedMesh.vertices = animated;
            meshFilter.sharedMesh.RecalculateBounds();
            meshFilter.sharedMesh.RecalculateNormals();
        }
    }
}

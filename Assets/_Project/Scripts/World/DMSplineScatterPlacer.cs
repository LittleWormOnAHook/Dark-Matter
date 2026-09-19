using System.Collections.Generic;
using UnityEngine;

namespace Project.World
{
    /// <summary>
    /// Scatters prefabs on terrain or physics surfaces inside a bounds (optional spline corridor).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark Matter/World/Spline Scatter Placer")]
    public class DMSplineScatterPlacer : MonoBehaviour
    {
        [SerializeField] private Terrain terrain;
        [SerializeField] private Bounds localBounds = new Bounds(Vector3.zero, new Vector3(40f, 0f, 40f));
        [SerializeField] private List<GameObject> scatterPrefabs = new List<GameObject>();
        [SerializeField] private int instanceCount = 40;
        [SerializeField] private float minSeparation = 2.5f;
        [SerializeField] private int randomSeed = 12345;
        [SerializeField] private LayerMask groundMask = Physics.DefaultRaycastLayers;
        [SerializeField] private float rayStartHeight = 50f;
        [SerializeField] private bool alignToSurfaceNormal = true;
        [SerializeField] private float heightOffset = 0f;
        [SerializeField] private bool randomYaw = true;

        [Header("Optional spline corridor")]
        [SerializeField] private DMSplineCreator corridorSpline;
        [SerializeField] private float corridorRadius = 6f;
        [SerializeField] private bool useCorridor;

        [SerializeField] private Transform scatterRoot;

        public void RebuildScatter()
        {
            EnsureRoot();
            ClearChildren(scatterRoot);
            if (scatterPrefabs == null || scatterPrefabs.Count == 0 || instanceCount <= 0)
                return;

            var rng = new System.Random(randomSeed);
            var placed = new List<Vector3>(instanceCount);
            Bounds worldBounds = TransformBounds(localBounds);

            int attempts = instanceCount * 30;
            int spawned = 0;
            for (int attempt = 0; attempt < attempts && spawned < instanceCount; attempt++)
            {
                Vector3 candidate = new Vector3(
                    NextFloat(rng, worldBounds.min.x, worldBounds.max.x),
                    worldBounds.center.y + rayStartHeight,
                    NextFloat(rng, worldBounds.min.z, worldBounds.max.z));

                if (useCorridor && corridorSpline != null && !IsInsideCorridor(candidate))
                    continue;

                if (!TrySampleGround(candidate, out Vector3 hitPoint, out Vector3 hitNormal))
                    continue;

                if (!HasSeparation(hitPoint, placed, minSeparation))
                    continue;

                GameObject prefab = scatterPrefabs[rng.Next(scatterPrefabs.Count)];
                if (prefab == null)
                    continue;

                Quaternion rot = Quaternion.identity;
                if (alignToSurfaceNormal && hitNormal.sqrMagnitude > 0.01f)
                    rot = Quaternion.FromToRotation(Vector3.up, hitNormal);
                if (randomYaw)
                    rot *= Quaternion.Euler(0f, NextFloat(rng, 0f, 360f), 0f);

                hitPoint += hitNormal.normalized * heightOffset;
                GameObject instance = InstantiateScatterPrefab(prefab, hitPoint, rot, scatterRoot);
                if (instance != null)
                {
                    placed.Add(hitPoint);
                    spawned++;
                }
            }
        }

        private bool IsInsideCorridor(Vector3 worldPos)
        {
            if (corridorSpline == null || corridorSpline.PathCreator == null)
                return true;

            MalbersAnimations.PathCreation.VertexPath path = corridorSpline.PathCreator.path;
            if (path == null || path.NumPoints < 2)
                return true;

            float best = float.MaxValue;
            for (int i = 0; i < path.NumPoints - 1; i++)
            {
                Vector3 a = corridorSpline.transform.TransformPoint(path.GetPoint(i));
                Vector3 b = corridorSpline.transform.TransformPoint(path.GetPoint(i + 1));
                float dist = DistancePointToSegmentXZ(worldPos, a, b);
                if (dist < best)
                    best = dist;
            }

            return best <= corridorRadius;
        }

        private static float DistancePointToSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 p2 = new Vector2(p.x, p.z);
            Vector2 a2 = new Vector2(a.x, a.z);
            Vector2 b2 = new Vector2(b.x, b.z);
            Vector2 ab = b2 - a2;
            float denom = Vector2.Dot(ab, ab);
            if (denom < 0.0001f)
                return Vector2.Distance(p2, a2);

            float t = Mathf.Clamp01(Vector2.Dot(p2 - a2, ab) / denom);
            Vector2 closest = a2 + ab * t;
            return Vector2.Distance(p2, closest);
        }

        private bool TrySampleGround(Vector3 fromHigh, out Vector3 point, out Vector3 normal)
        {
            point = default;
            normal = Vector3.up;

            if (terrain != null && terrain.terrainData != null)
            {
                Vector3 terrainLocal = fromHigh - terrain.transform.position;
                float h = terrain.SampleHeight(fromHigh);
                point = new Vector3(fromHigh.x, h, fromHigh.z);
                normal = Vector3.up;
                return true;
            }

            if (Physics.Raycast(fromHigh, Vector3.down, out RaycastHit hit, rayStartHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                normal = hit.normal;
                return true;
            }

            return false;
        }

        private Bounds TransformBounds(Bounds local)
        {
            Vector3 center = transform.TransformPoint(local.center);
            Vector3 extents = Vector3.Scale(local.extents, transform.lossyScale);
            return new Bounds(center, extents * 2f);
        }

        private static bool HasSeparation(Vector3 candidate, List<Vector3> placed, float minSep)
        {
            float minSepSqr = minSep * minSep;
            for (int i = 0; i < placed.Count; i++)
            {
                if ((placed[i] - candidate).sqrMagnitude < minSepSqr)
                    return false;
            }

            return true;
        }

        private void EnsureRoot()
        {
            if (scatterRoot != null)
                return;

            Transform t = transform.Find("ScatterInstances");
            if (t == null)
            {
                var go = new GameObject("ScatterInstances");
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Scatter Placer");
#endif
                t = go.transform;
                t.SetParent(transform, false);
            }

            scatterRoot = t;
        }

        private static GameObject InstantiateScatterPrefab(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
                if (instance != null)
                    instance.transform.SetPositionAndRotation(pos, rot);
                return instance;
            }
#endif
            return Instantiate(prefab, pos, rot, parent);
        }

        private static void ClearChildren(Transform root)
        {
            if (root == null)
                return;

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
                else
#endif
                    Destroy(child.gameObject);
            }
        }

        private static float NextFloat(System.Random rng, float min, float max) =>
            (float)(min + rng.NextDouble() * (max - min));

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.35f);
            Bounds b = TransformBounds(localBounds);
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}

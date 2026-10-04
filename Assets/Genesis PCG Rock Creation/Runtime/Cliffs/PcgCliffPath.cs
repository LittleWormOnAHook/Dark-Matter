using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GenesisPCG.RockCreation
{
    public struct PcgCliffBuildStats
    {
        public int parts, chunks, vertices, triangles;
        public float length, ms;
        public string error;
        public override string ToString() => !string.IsNullOrEmpty(error) ? error
            : $"{parts} pieces, {chunks} chunk(s), {vertices:N0} verts, {triangles:N0} tris, {length:0.#} m path, {ms:0} ms";
    }

    /// <summary>
    /// A sheer cliff along a path of control points (cliff-local space). Generate builds one combined mesh per chunk under a
    /// "Generated" child; the editor's Bake turns those into saved meshes with LODs and colliders. Unbaked previews are not
    /// saved with the scene and are rebuilt when the scene opens.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Genesis PCG Rock Creation/Sheer Cliff")]
    public sealed class PcgCliffPath : MonoBehaviour
    {
        public const string GeneratedName = "Generated";

        [Tooltip("Control points in this object's local space (Scene view: Shift+click adds, Ctrl+click removes).")]
        public List<Vector3> points = new List<Vector3> { new Vector3(-30f, 0f, 0f), new Vector3(-10f, 0f, -3f), new Vector3(10f, 0f, -3f), new Vector3(30f, 0f, 0f) };
        [Tooltip("Join the last point back to the first (a mesa / butte ring).")]
        public bool closed;
        public PcgSheerCliffSettings settings = new PcgSheerCliffSettings();

        [SerializeField, HideInInspector] private bool m_baked;
        [SerializeField, HideInInspector] private string m_bakeId = "";

        [NonSerialized] public PcgCliffBuildStats LastStats;

        public bool Baked { get => m_baked; set => m_baked = value; }
        public string BakeId
        {
            get
            {
                if (string.IsNullOrEmpty(m_bakeId)) m_bakeId = Guid.NewGuid().ToString("N").Substring(0, 8);
                return m_bakeId;
            }
        }

        public Transform GeneratedRoot => transform.Find(GeneratedName);

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // Deferred: adding components while the scene loads is not allowed.
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null && isActiveAndEnabled && NeedsPreviewRestore()) Regenerate(false);
                };
            }
#endif
        }

        private void Start()
        {
            if (Application.isPlaying && NeedsPreviewRestore()) Regenerate(false);
        }

        /// <summary>True when an unbaked preview lost its meshes (scene reopened / play mode).</summary>
        public bool NeedsPreviewRestore()
        {
            if (m_baked || settings == null || settings.kit == null || points == null || points.Count < 2) return false;
            Transform root = GeneratedRoot;
            if (root == null) return true;
            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
                if (mf.sharedMesh == null) return true;
            return root.childCount == 0;
        }

        private static bool IsAsset(Object o)
        {
#if UNITY_EDITOR
            return o != null && UnityEditor.EditorUtility.IsPersistent(o);
#else
            return false;
#endif
        }

        private static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        public void ClearGenerated()
        {
            Transform root = GeneratedRoot;
            while (root != null)
            {
                foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh != null && !IsAsset(mf.sharedMesh)) Kill(mf.sharedMesh);
                root.SetParent(null);
                Kill(root.gameObject);
                root = GeneratedRoot;
            }
            m_baked = false;
        }

        /// <summary>Rebuilds the preview: one combined, unsaved mesh per chunk.</summary>
        public PcgCliffBuildStats Regenerate(bool markDirty = true)
        {
            var sw = Stopwatch.StartNew();
            var stats = new PcgCliffBuildStats();
            ClearGenerated();
            List<PcgCliffPart> parts = PcgSheerCliffGenerator.Plan(this, out string error);
            if (!string.IsNullOrEmpty(error)) { stats.error = error; LastStats = stats; return stats; }

            var rootGo = new GameObject(GeneratedName);
            rootGo.layer = gameObject.layer;
            rootGo.transform.SetParent(transform, false);
            var byChunk = new SortedDictionary<int, List<PcgCliffPart>>();
            foreach (PcgCliffPart p in parts)
            {
                if (!byChunk.TryGetValue(p.chunk, out var list)) byChunk[p.chunk] = list = new List<PcgCliffPart>();
                list.Add(p);
            }
            foreach (var kv in byChunk)
            {
                Vector3 c = Vector3.zero;
                foreach (PcgCliffPart p in kv.Value) c += (Vector3)p.matrix.GetColumn(3);
                c /= Mathf.Max(1, kv.Value.Count);
                string chunkName = $"Chunk_{kv.Key:00}";
                Mesh m = PcgCliffMesher.Combine(settings.kit, kv.Value, Matrix4x4.Translate(-c), $"{name}_{chunkName}", out Material[] mats);
                if (m == null) continue;
                m.hideFlags = HideFlags.DontSave;
                var go = new GameObject(chunkName);
                go.layer = gameObject.layer;
                go.transform.SetParent(rootGo.transform, false);
                go.transform.localPosition = c;
                go.AddComponent<MeshFilter>().sharedMesh = m;
                go.AddComponent<MeshRenderer>().sharedMaterials = mats;
                stats.chunks++;
                stats.vertices += m.vertexCount;
                for (int s = 0; s < m.subMeshCount; s++) stats.triangles += (int)(m.GetIndexCount(s) / 3);
            }
            stats.parts = parts.Count;
            stats.length = PathLength();
            stats.ms = (float)sw.Elapsed.TotalMilliseconds;
            LastStats = stats;
#if UNITY_EDITOR
            if (markDirty && !Application.isPlaying && gameObject.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
            return stats;
        }

        public float PathLength()
        {
            List<Vector3> poly = PcgCliffCurve.Sample(points, closed, 1f);
            float L = 0f;
            for (int i = 1; i < poly.Count; i++) L += Vector3.Distance(poly[i - 1], poly[i]);
            return L;
        }
    }
}

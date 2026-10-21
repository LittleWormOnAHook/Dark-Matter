using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.SurfaceCarve
{
    /// <summary>
    /// 2026-10-20x: original meshes of statically batched carve candidates (static batching replaces their MeshFilter mesh
    /// with the combined batch). Rebuilt automatically in Edit Mode when entering Play (DMCarveStaticMeshCacheBuilder) and
    /// by Tools > Carve > Rebuild Static Mesh Cache. Lives at Resources/Carve/DMCarveStaticMeshCache.
    /// </summary>
    public sealed class DMCarveStaticMeshCache : ScriptableObject
    {
        [Serializable] public struct Entry { public string key; public Mesh mesh; }
        public List<Entry> entries = new List<Entry>();

        [NonSerialized] private Dictionary<string, Mesh> _map;
        private static DMCarveStaticMeshCache s_instance;
        private static bool s_loaded;

        public static DMCarveStaticMeshCache Instance
        {
            get
            {
                if (!s_loaded || s_instance == null) { s_instance = Resources.Load<DMCarveStaticMeshCache>("Carve/DMCarveStaticMeshCache"); s_loaded = true; }
                return s_instance;
            }
        }

        public void Invalidate() => _map = null;

        public static Mesh Find(GameObject go)
        {
            var c = Instance;
            if (c == null || go == null) return null;
            if (c._map == null)
            {
                c._map = new Dictionary<string, Mesh>();
                foreach (var e in c.entries) if (!string.IsNullOrEmpty(e.key) && e.mesh != null) c._map[e.key] = e.mesh;
            }
            return c._map.TryGetValue(DMCarveUnbatch.Key(go), out Mesh m) ? m : null;
        }
    }
}

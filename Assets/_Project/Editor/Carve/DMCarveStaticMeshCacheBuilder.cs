using System.Collections.Generic;
using System.IO;
using Project.SurfaceCarve;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools.Carve
{
    /// <summary>
    /// 2026-10-20x: records the source mesh of every Batching Static renderer that Auto Carvable could hit (tag/name rules,
    /// incl. parents and the collider's object) in the open scenes, right before entering Play (static batching discards it).
    /// Read-only on scenes: only the cache asset is written. Builds: run the menu with the build scenes open first.
    /// </summary>
    [InitializeOnLoad]
    public static class DMCarveStaticMeshCacheBuilder
    {
        private const string AssetPath = "Assets/_Project/Resources/Carve/DMCarveStaticMeshCache.asset";

        static DMCarveStaticMeshCacheBuilder()
        {
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.ExitingEditMode) Rebuild(false);
            };
        }

        [MenuItem("Tools/Carve/Rebuild Static Mesh Cache")]
        private static void MenuRebuild() => Rebuild(true);

        public static int Rebuild(bool log)
        {
            var settings = DMCarveSettings.Active;
            if (settings == null || !settings.carveUnbatchesStatic) return 0;
            var cache = AssetDatabase.LoadAssetAtPath<DMCarveStaticMeshCache>(AssetPath);
            if (cache == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
                cache = ScriptableObject.CreateInstance<DMCarveStaticMeshCache>();
                AssetDatabase.CreateAsset(cache, AssetPath);
            }
            var list = new List<DMCarveStaticMeshCache.Entry>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene sc = SceneManager.GetSceneAt(i);
                if (!sc.isLoaded) continue;
                foreach (GameObject root in sc.GetRootGameObjects())
                    foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (mf.sharedMesh == null || mf.GetComponent<MeshRenderer>() == null) continue;
                        GameObject go = mf.gameObject;
                        if ((GameObjectUtility.GetStaticEditorFlags(go) & StaticEditorFlags.BatchingStatic) == 0) continue;
                        if (!Candidate(go, settings)) continue;
                        list.Add(new DMCarveStaticMeshCache.Entry { key = DMCarveUnbatch.Key(go), mesh = mf.sharedMesh });
                    }
            }
            bool changed = list.Count != cache.entries.Count;
            for (int i = 0; !changed && i < list.Count; i++) changed = list[i].key != cache.entries[i].key || list[i].mesh != cache.entries[i].mesh;
            if (changed)
            {
                cache.entries = list; cache.Invalidate();
                EditorUtility.SetDirty(cache);
                AssetDatabase.SaveAssetIfDirty(cache);
            }
            if (log) Debug.Log($"[Carve] static mesh cache: {list.Count} batched carve candidates recorded.");
            return list.Count;
        }

        private static bool Candidate(GameObject go, DMCarveSettings s)
        {
            if (go.GetComponent<DMCarvable>() != null) return true;
            for (Transform t = go.transform; t != null; t = t.parent) if (s.MatchesAutoCarvable(t.gameObject)) return true;
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) if (s.MatchesAutoCarvable(c.gameObject)) return true;
            return false;
        }
    }
}

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>Bakes a cliff preview into saved chunk meshes with LODs, colliders and occlusion flags, and saves it to the library.</summary>
    public static class PcgCliffBake
    {
        private static readonly float[] Transitions = { 0.35f, 0.15f, 0.05f, 0.015f };

        public static DmRockKit DefaultKit()
        {
            foreach (string g in AssetDatabase.FindAssets("RockKit_ProcRock t:DmRockKit"))
            {
                var k = AssetDatabase.LoadAssetAtPath<DmRockKit>(AssetDatabase.GUIDToAssetPath(g));
                if (k != null) return k;
            }
            foreach (string g in AssetDatabase.FindAssets("t:DmRockKit"))
            {
                var k = AssetDatabase.LoadAssetAtPath<DmRockKit>(AssetDatabase.GUIDToAssetPath(g));
                if (k != null && k.pieces != null && k.pieces.Count > 0) return k;
            }
            return null;
        }

        public static string FolderFor(PcgCliffPath path)
        {
            string scene = path.gameObject.scene.IsValid() && !string.IsNullOrEmpty(path.gameObject.scene.name) ? path.gameObject.scene.name : "Untitled";
            string safe = string.Join("_", path.name.Split(Path.GetInvalidFileNameChars())).Replace(' ', '_');
            return $"{PcgStudioExtensions.DataRoot}/Generated/Cliffs/{scene}/{safe}_{path.BakeId}";
        }

        public static string Bake(PcgCliffPath path)
        {
            if (path == null) return "No cliff selected.";
            PcgCliffBuildStats stats = path.Regenerate(false);
            if (!string.IsNullOrEmpty(stats.error)) return stats.error;
            Transform root = path.GeneratedRoot;
            if (root == null || root.childCount == 0) return "Nothing was generated.";

            string folder = FolderFor(path);
            PcgStudioExtensions.EnsureFolder(folder);
            foreach (string g in AssetDatabase.FindAssets("Chunk_ t:Mesh", new[] { folder }))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetDirectoryName(p).Replace('\\', '/') == folder) AssetDatabase.DeleteAsset(p);
            }

            int lods = Mathf.Clamp(path.settings.lodCount, 1, 4);
            float[] q = DmRockLodSimplifier.Qualities(lods);
            int tris = 0;
            var chunks = new List<Transform>();
            foreach (Transform c in root) chunks.Add(c);
            try
            {
                for (int ci = 0; ci < chunks.Count; ci++)
                {
                    Transform chunk = chunks[ci];
                    EditorUtility.DisplayProgressBar("Bake Sheer Cliff", chunk.name, ci / (float)chunks.Count);
                    var mf = chunk.GetComponent<MeshFilter>();
                    var mr = chunk.GetComponent<MeshRenderer>();
                    if (mf == null || mf.sharedMesh == null) continue;
                    Mesh lod0 = mf.sharedMesh;
                    Material[] mats = mr != null ? mr.sharedMaterials : new Material[0];
                    lod0.hideFlags = HideFlags.None;
                    lod0.name = chunk.name + "_LOD0";
                    string assetPath = $"{folder}/{chunk.name}.asset";
                    AssetDatabase.CreateAsset(lod0, assetPath);

                    var meshes = new Mesh[lods];
                    meshes[0] = lod0;
                    for (int i = 1; i < lods; i++)
                    {
                        Mesh m = DmRockLodSimplifier.Simplify(lod0, q[i], false);
                        if (m == null) m = Object.Instantiate(lod0);
                        m.name = $"{chunk.name}_LOD{i}";
                        m.RecalculateBounds();
                        AssetDatabase.AddObjectToAsset(m, assetPath);
                        meshes[i] = m;
                    }

                    Object.DestroyImmediate(mf);
                    if (mr != null) Object.DestroyImmediate(mr);
                    var lodArr = new LOD[lods];
                    for (int i = 0; i < lods; i++)
                    {
                        var go = new GameObject($"{chunk.name}_LOD{i}");
                        go.layer = chunk.gameObject.layer;
                        go.transform.SetParent(chunk, false);
                        go.AddComponent<MeshFilter>().sharedMesh = meshes[i];
                        var r = go.AddComponent<MeshRenderer>();
                        r.sharedMaterials = mats;
                        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic);
                        lodArr[i] = new LOD(Transitions[i], new Renderer[] { r });
                    }
                    var group = chunk.gameObject.AddComponent<LODGroup>();
                    group.SetLODs(lodArr);
                    group.RecalculateBounds();
                    if (path.settings.collision)
                    {
                        var col = chunk.gameObject.AddComponent<MeshCollider>();
                        col.sharedMesh = meshes[Mathf.Min(1, lods - 1)];
                    }
                    GameObjectUtility.SetStaticEditorFlags(chunk.gameObject, StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
                    tris += (int)(lod0.triangles.Length / 3);
                }
            }
            finally { EditorUtility.ClearProgressBar(); }

            AssetDatabase.SaveAssets();
            path.Baked = true;
            EditorUtility.SetDirty(path);
            if (path.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(path.gameObject.scene);
            return $"Baked {chunks.Count} chunk(s), {tris:N0} LOD0 tris, {lods} LOD(s) into {folder}.";
        }

        public static string SaveToLibrary(PcgCliffPath path, IPcgStudioContext ctx)
        {
            if (path == null || ctx == null) return "No cliff selected.";
            if (!path.Baked || path.GeneratedRoot == null)
            {
                string r = Bake(path);
                if (!path.Baked) return r;
            }
            string folder = string.IsNullOrEmpty(ctx.DefaultPrefabFolder) ? PcgStudioExtensions.PrefabFolder : ctx.DefaultPrefabFolder;
            PcgStudioExtensions.EnsureFolder(folder);
            string safe = string.Join("_", path.name.Split(Path.GetInvalidFileNameChars())).Replace(' ', '_');
            string prefabPath = $"{folder}/Cliff_{safe}.prefab";

            GameObject copy = Object.Instantiate(path.GeneratedRoot.gameObject);
            copy.name = "Cliff_" + safe;
            copy.transform.position = Vector3.zero;
            copy.transform.rotation = Quaternion.identity;
            copy.transform.localScale = path.GeneratedRoot.lossyScale;
            GameObject prefab;
            try { prefab = PrefabUtility.SaveAsPrefabAsset(copy, prefabPath); }
            finally { Object.DestroyImmediate(copy); }
            if (prefab == null) return "Could not save " + prefabPath;
            GameObject p = prefab;
            ctx.SetPreviewSubject(() => (GameObject)PrefabUtility.InstantiatePrefab(p));
            ctx.SaveToLibrary(prefab, "Cliffs", null, null, path.settings.seed);
            return $"Saved {prefabPath} to the library (Cliffs).";
        }
    }
}

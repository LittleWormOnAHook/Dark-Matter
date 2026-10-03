#if UNITY_EDITOR
using System;
using System.Linq;
using GenesisPCG.RockCreation;
using UnityEditor;
using UnityEngine;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>Kit inspector with a "Scan Folder" button so any rock pack can be turned into a kit.</summary>
    [CustomEditor(typeof(DmRockKit))]
    internal sealed class DmRockKitEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var kit = (DmRockKit)target;
            EditorGUILayout.LabelField("Genesis PCG Rock Creation \u2014 Rock Kit", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Scan Folder");
                var folderAsset = string.IsNullOrEmpty(kit.scanFolder) ? null : AssetDatabase.LoadAssetAtPath<DefaultAsset>(kit.scanFolder);
                var picked = EditorGUILayout.ObjectField(folderAsset, typeof(DefaultAsset), false) as DefaultAsset;
                if (picked != folderAsset)
                {
                    string path = picked != null ? AssetDatabase.GetAssetPath(picked) : "";
                    if (picked == null || AssetDatabase.IsValidFolder(path))
                    {
                        Undo.RecordObject(kit, "Kit Scan Folder");
                        kit.scanFolder = path;
                        EditorUtility.SetDirty(kit);
                    }
                }
            }
            DrawDefaultInspector();
            EditorGUILayout.Space(4);
            using (new EditorGUI.DisabledScope(!AssetDatabase.IsValidFolder(kit.scanFolder)))
                if (GUILayout.Button("Scan Folder"))
                {
                    Undo.RecordObject(kit, "Scan Rock Kit Folder");
                    Scan(kit);
                    AssetDatabase.SaveAssetIfDirty(kit);
                }
            if (GUILayout.Button(new GUIContent("Analyze Pieces", "Measure every piece (principal axes, class, top/bottom width, fracture sharpness) and cache it in the kit.")))
            {
                Undo.RecordObject(kit, "Analyze Rock Kit");
                Analyze(kit);
                AssetDatabase.SaveAssetIfDirty(kit);
            }
            int tall = kit.pieces.Count(p => p != null && p.pieceClass == DmRockPieceClass.Tall);
            int boulder = kit.pieces.Count(p => p != null && p.pieceClass == DmRockPieceClass.Boulder);
            int slab = kit.pieces.Count(p => p != null && p.pieceClass == DmRockPieceClass.Slab);
            int small = kit.pieces.Count(p => p != null && p.pieceClass == DmRockPieceClass.Small);
            EditorGUILayout.HelpBox($"{kit.baseCandidates.Count} base candidates, {kit.addOnCandidates.Count} add-on candidates.\n" +
                                    $"Measured pieces: {kit.pieces.Count} (tall {tall}, boulder {boulder}, slab {slab}, small {small}).", MessageType.None);
        }

        /// <summary>
        /// Fills the kit from <see cref="DmRockKit.scanFolder"/>: every prefab with a mesh is considered. Names containing
        /// baseNameContains are base-only; other prefabs are add-ons, and also bases when at least baseMinExtent big.
        /// </summary>
        internal static void Scan(DmRockKit kit)
        {
            if (kit == null || !AssetDatabase.IsValidFolder(kit.scanFolder))
                return;
            kit.baseCandidates.Clear();
            kit.addOnCandidates.Clear();
            string root = kit.scanFolder.TrimEnd('/');
            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { root })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => kit.scanSubfolders || System.IO.Path.GetDirectoryName(p).Replace('\\', '/') == root)
                .OrderBy(p => p, StringComparer.Ordinal);
            foreach (string path in paths)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;
                var mfs = go.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).ToArray();
                if (mfs.Length == 0) continue;
                Bounds b = mfs[0].sharedMesh.bounds;
                foreach (MeshFilter f in mfs) b.Encapsulate(f.sharedMesh.bounds);
                float extent = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                bool baseOnly = !string.IsNullOrEmpty(kit.baseNameContains) &&
                                go.name.IndexOf(kit.baseNameContains, StringComparison.OrdinalIgnoreCase) >= 0;
                if (baseOnly || extent >= kit.baseMinExtent) kit.baseCandidates.Add(go);
                if (!baseOnly) kit.addOnCandidates.Add(go);
            }
            Analyze(kit);
        }

        /// <summary>Measures every base / add-on piece and caches the result in <see cref="DmRockKit.pieces"/>.</summary>
        internal static void Analyze(DmRockKit kit)
        {
            if (kit == null) return;
            kit.pieces.Clear();
            var seen = new System.Collections.Generic.HashSet<GameObject>();
            foreach (GameObject go in kit.baseCandidates.Concat(kit.addOnCandidates))
            {
                if (go == null || !seen.Add(go)) continue;
                DmRockPieceInfo info = DmRockPieceAnalyzer.Analyze(go, kit);
                if (info != null) kit.pieces.Add(info);
            }
            kit.Version++;
            EditorUtility.SetDirty(kit);
        }
    }
}
#endif

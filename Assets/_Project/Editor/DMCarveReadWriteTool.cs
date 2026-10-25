using System.Collections.Generic;
using Project.SurfaceCarve;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>2026-10-20v: enables Read/Write on model imports of open-scene meshes that match Auto Carvable tags/names (asks first).</summary>
    public static class DMCarveReadWriteTool
    {
        [MenuItem("Tools/Dark Matter Genesis/Carve/Enable Read/Write For Auto-Carvable Tags")]
        private static void Run()
        {
            DMCarveSettings s = DMCarveSettings.Active;
            var importers = new HashSet<ModelImporter>();
            foreach (MeshFilter mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include))
            {
                if (mf.sharedMesh == null || mf.sharedMesh.isReadable || !s.MatchesAutoCarvable(mf.gameObject)) continue;
                if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mf.sharedMesh)) is ModelImporter mi && !mi.isReadable) importers.Add(mi);
            }
            if (importers.Count == 0) { EditorUtility.DisplayDialog("Carve", "No non-readable auto-carvable meshes in the open scene(s).", "OK"); return; }
            if (!EditorUtility.DisplayDialog("Carve", $"Enable Read/Write on {importers.Count} model import(s)? (reimports, more memory)", "Enable", "Cancel")) return;
            foreach (ModelImporter mi in importers) { mi.isReadable = true; mi.SaveAndReimport(); Debug.Log("[Carve] Read/Write enabled: " + mi.assetPath); }
        }
    }
}

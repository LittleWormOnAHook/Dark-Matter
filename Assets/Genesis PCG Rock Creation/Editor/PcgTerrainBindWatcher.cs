using UnityEditor;
using UnityEngine;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>
    /// 2026-10-24p: keeps the Rock Blend terrain binding in sync with the CURRENT terrain. Any edit to a TerrainLayer, a terrain
    /// (layers / splat paint), a terrain material (e.g. DM/TerrainParallax per-layer tint) or a re-imported texture/terrain layer
    /// asset triggers PcgTerrainSplat.RefreshAll (debounced): property blocks re-filled in Edit Mode, shared runtime copies
    /// re-filled in Play Mode. Menu: Tools/Genesis PCG Rock Creation/Rebind All Rocks to Terrain.
    /// </summary>
    [InitializeOnLoad]
    public static class PcgTerrainBindWatcher
    {
        private static bool s_pending, s_busy, s_ready;

        static PcgTerrainBindWatcher()
        {
            Undo.postprocessModifications -= OnMods;
            Undo.postprocessModifications += OnMods;
            Undo.undoRedoPerformed -= Request;
            Undo.undoRedoPerformed += Request;
            UnityEngine.TerrainCallbacks.textureChanged -= OnTex;
            UnityEngine.TerrainCallbacks.textureChanged += OnTex;
            // No work at load: only arm after the editor has settled (first delayCall after reload).
            EditorApplication.delayCall += () => s_ready = true;
        }

        [MenuItem("Tools/Dark Matter Genesis/PCG Rocks/Rebind All Rocks to Terrain")]
        public static void RebindAllMenu()
        {
            Run();
            Debug.Log("[RockBlend] Rebound all rocks to the current terrain layers/textures.");
        }

        public static void Request()
        {
            if (!s_ready || s_busy || s_pending) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            s_pending = true;
            EditorApplication.delayCall += () =>
            {
                s_pending = false;
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                Run();
            };
        }

        // Reentrancy-guarded: callbacks fired by the rebind itself are ignored.
        private static void Run()
        {
            if (s_busy) return;
            s_busy = true;
            try { PcgTerrainSplat.RefreshAll(); SceneView.RepaintAll(); }
            catch (System.Exception ex) { Debug.LogWarning("[RockBlend] Rebind failed: " + ex.Message); }
            finally { s_busy = false; }
        }

        private static void OnTex(Terrain t, string name, RectInt r, bool synced) { if (name == "alphamap" && synced) Request(); }

        private static UndoPropertyModification[] OnMods(UndoPropertyModification[] mods)
        {
            foreach (var m in mods)
            {
                Object o = m.currentValue != null ? m.currentValue.target : null;
                if (o is TerrainLayer || o is TerrainData || o is Terrain || (o is Material mat && mat.shader != null && mat.shader.name.Contains("Terrain")))
                { Request(); break; }
            }
            return mods;
        }

        private sealed class Post : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
            {
                foreach (string p in imported)
                {
                    string e = System.IO.Path.GetExtension(p).ToLowerInvariant();
                    if (e == ".terrainlayer" || e == ".mat" || e == ".png" || e == ".tga" || e == ".tif" || e == ".jpg" || e == ".psd" || e == ".exr")
                    { Request(); return; }
                }
            }
        }
    }
}

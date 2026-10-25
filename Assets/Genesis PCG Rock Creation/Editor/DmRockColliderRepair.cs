using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>24z: fixes rocks whose MeshCollider lost its mesh (Mesh = None -> player walks through). Edit mode only, Undo-registered, never saves.</summary>
    internal static class DmRockColliderRepair
    {
        [MenuItem("Tools/Dark Matter Genesis/PCG Rocks/Repair rock colliders (all)")]
        private static void RepairAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[RockColliderRepair] Exit Play mode first."); return; }
            int combiners = 0, plain = 0, still = 0;
            Undo.SetCurrentGroupName("Repair rock colliders");
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene scene = SceneManager.GetSceneAt(s);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (var c in root.GetComponentsInChildren<GenesisPCG.RockCreation.DmRockCombiner>(true))
                    {
                        // 24aa: LOD0 source child gets the collider (its own asset mesh); creator collider disabled.
                        if (c.SyncLod0Collider(true) != null) { combiners++; continue; }
                        var mc = c.GetComponent<MeshCollider>();
                        if (mc != null) Undo.RecordObject(mc, "Repair rock colliders");
                        if (c.EnsureColliderMesh()) { combiners++; EditorUtility.SetDirty(c.gameObject); }
                    }
                    foreach (MeshCollider mc in root.GetComponentsInChildren<MeshCollider>(true))
                    {
                        if (mc.sharedMesh != null || !mc.enabled) continue;
                        var mf = mc.GetComponent<MeshFilter>();
                        if (mf == null || mf.sharedMesh == null || mf.sharedMesh.vertexCount < 3) { still++; Debug.Log("[RockColliderRepair] no mesh to use: " + Path(mc.transform), mc); continue; }
                        Undo.RecordObject(mc, "Repair rock colliders");
                        mc.convex = false;
                        mc.sharedMesh = mf.sharedMesh;
                        EditorUtility.SetDirty(mc);
                        plain++;
                    }
                }
            }
            Debug.Log("[RockColliderRepair] fixed creator rocks=" + combiners + " other MeshColliders=" + plain + " left without mesh=" + still + " (scene not saved)");
        }

        private static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    }
}
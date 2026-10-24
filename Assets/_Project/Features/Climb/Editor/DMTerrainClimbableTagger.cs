using UnityEditor;
using UnityEngine;

namespace Project.Climb.EditorTools
{
    /// <summary>24ae: tags every loaded Terrain "Climbable" and puts it on the Default layer (Climbable LAYER is collision-ignored vs Player while climbing). Undo-able, does not save.</summary>
    public static class DMTerrainClimbableTagger
    {
        [MenuItem("Tools/Dark Matter/Climb/Tag all loaded terrains Climbable (Default layer)")]
        public static void TagAll()
        {
            int n = 0;
            foreach (Terrain t in Resources.FindObjectsOfTypeAll<Terrain>())
            {
                if (EditorUtility.IsPersistent(t) || !t.gameObject.scene.IsValid()) continue;
                Undo.RecordObject(t.gameObject, "Tag terrains Climbable");
                t.gameObject.tag = "Climbable";
                t.gameObject.layer = 0;
                EditorUtility.SetDirty(t.gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
                n++;
            }
            Debug.Log("[TerrainClimbable] tagged " + n + " terrains Climbable on layer Default (scenes marked dirty, not saved).");
        }
    }
}

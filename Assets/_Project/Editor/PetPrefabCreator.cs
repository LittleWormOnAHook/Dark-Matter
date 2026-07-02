using Project.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class PetPrefabCreator
    {
        [MenuItem(SurvivalPioneerEditorMenus.Content + "Create Pet Prefab (Fox Cub Demo)", false, 27)]
        public static void CreateFoxCubPetPrefab()
        {
            PetPrefabBuildSettings settings = PetPrefabBuilder.CreateFoxCubPreset();
            if (settings.SourcePrefab == null)
            {
                Debug.LogError(
                    "PetPrefabCreator: Missing source prefab at Assets/_Project/Prefabs/Players/Fox Cub Variant.prefab");
                return;
            }

            if (PetPrefabBuilder.Build(settings, out string message))
                Debug.Log(message + " Place the prefab in the world and press E to befriend.");
            else
                Debug.LogError(message);
        }
    }
}

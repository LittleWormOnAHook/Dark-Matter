using Project.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>Quick demo menu actions for Pet Manager / PetPrefabBuilder.</summary>
    public static class PetPrefabCreator
    {
        [MenuItem(DarkMatterGenesisEditorMenus.PetPrefabFoxCubDemo, false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Pets_Pet_Prefab_Fox_Cub_Demo)]
        public static void CreateFoxCubPetPrefab()
        {
            PetPrefabBuildSettings settings = PetPrefabBuilder.CreateFoxCubPreset();
            if (settings.SourcePrefab == null)
            {
                Debug.LogError(
                    "Pet Manager: Missing source prefab at Assets/_Project/Prefabs/Players/Fox Cub Variant.prefab");
                return;
            }

            if (PetPrefabBuilder.Build(settings, out string message))
                Debug.Log(message + " Place the prefab in the world and press E to befriend.");
            else
                Debug.LogError(message);
        }
    }
}

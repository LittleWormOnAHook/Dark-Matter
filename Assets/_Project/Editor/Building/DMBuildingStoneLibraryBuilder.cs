using System.Collections.Generic;
using System.IO;
using Project.Building;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Building
{
    /// <summary>
    /// Retired 0925: the Stone kit is owned by <see cref="DMBuildingKitBuilder"/>. This class used to rewrite the
    /// foundation, wall, floor, ceiling, window, door frame and door prefabs on every domain reload, which overwrote
    /// the kit with the old mesh-less versions. Only the stone material finishes are still ensured on load.
    /// </summary>
    [InitializeOnLoad]
    public static class DMBuildingStoneLibraryBuilder
    {
        const string Root = "Assets/_Project/Prefabs/Buildings/Library/Stone";

        static readonly (string id, string folder)[] Pieces =
        {
            ("stone_foundation_4x4", "Foundations"),
            ("stone_wall_4x4", "Walls"),
            ("stone_floor_4x4", "Floors"),
            ("stone_ceiling_4x4", "Ceilings"),
            ("stone_slope_4x4", "Slopes"),
            ("stone_wall_window_4x4", "Windows"),
            ("stone_door_frame_4x4", "DoorFrames"),
            ("stone_door_basic", "Doors"),
        };

        static bool building;

        static DMBuildingStoneLibraryBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                    return;
                DMBuildingMaterialLibraryBuilder.EnsureStoneFinishes();
            };
        }

        [MenuItem("Tools/Dark Matter Genesis/Buildings/Build Stone Library")]
        public static void BuildFromMenu()
        {
            DMBuildingKitBuilder.RebuildStoneKit();
        }

        static void EnsureLibrary(bool force)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || building)
                return;

            const string libraryPath = "Assets/_Project/Resources/Building/DM_BuildingLibrary.asset";
            bool missing = force || AssetDatabase.LoadAssetAtPath<DMBuildingLibrary>(libraryPath) == null;
            if (!missing)
            {
                for (int i = 0; i < Pieces.Length; i++)
                {
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(Pieces[i].folder, Pieces[i].id)) == null)
                    {
                        missing = true;
                        break;
                    }
                }
            }

            if (!missing)
                return;

            building = true;
            try
            {
                EnsureFolders();
                var pieces = new List<DMBuildingLibrary.Entry>();
                for (int i = 0; i < Pieces.Length; i++)
                {
                    string path = PrefabPath(Pieces[i].folder, Pieces[i].id);
                    GameObject source = null;
                    try
                    {
                        source = DMBuildingPieceFactory.CreateRuntimeMesh(Pieces[i].id);
                        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, path);
                        pieces.Add(new DMBuildingLibrary.Entry { id = Pieces[i].id, prefab = prefab });
                    }
                    finally
                    {
                        if (source != null)
                            Object.DestroyImmediate(source);
                    }
                }

                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/Building"))
                    AssetDatabase.CreateFolder("Assets/_Project/Resources", "Building");

                DMBuildingLibrary library = AssetDatabase.LoadAssetAtPath<DMBuildingLibrary>(libraryPath);
                if (library == null)
                {
                    library = ScriptableObject.CreateInstance<DMBuildingLibrary>();
                    AssetDatabase.CreateAsset(library, libraryPath);
                }

                library.pieces = pieces;
                EditorUtility.SetDirty(library);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                building = false;
            }
        }

        static void RefreshWindowAndDoorMeshes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;

            RebuildPiece("stone_wall_window_4x4", "Windows");
            RebuildPiece("stone_door_frame_4x4", "DoorFrames");
            RebuildPiece("stone_door_basic", "Doors");
        }

        static void RebuildPiece(string id, string folder)
        {
            EnsureFolders();
            string path = PrefabPath(folder, id);
            GameObject source = null;
            try
            {
                source = DMBuildingPieceFactory.CreateRuntimeMesh(id);
                PrefabUtility.SaveAsPrefabAsset(source, path);
            }
            finally
            {
                if (source != null)
                    Object.DestroyImmediate(source);
            }
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder("Assets/_Project/Prefabs/Buildings");
            EnsureFolder("Assets/_Project/Prefabs/Buildings/Library");
            EnsureFolder(Root);
            for (int i = 0; i < Pieces.Length; i++)
                EnsureFolder(Root + "/" + Pieces[i].folder);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            if (!string.IsNullOrEmpty(parent))
                AssetDatabase.CreateFolder(parent, leaf);
        }

        static string PrefabPath(string folder, string id)
        {
            return Root + "/" + folder + "/" + id + ".prefab";
        }
    }
}

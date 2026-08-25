#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    [InitializeOnLoad]
    public static class StripGaiaTerrainTrees
    {
        const string FlagPath = "Assets/_Project/Editor/World/STRIP_TREES.now";
        const string TerrainDataFolder = "Assets/Gaia User Data/Sessions/DM Genesis/Terrain Data";

        static StripGaiaTerrainTrees()
        {
            if (!File.Exists(FlagPath))
                return;
            EditorApplication.delayCall += RunIfFlagged;
        }

        [MenuItem("Dark Matter Genesis/World/Strip Trees From All Terrains")]
        public static void StripFromMenu()
        {
            Strip(true);
        }

        static void RunIfFlagged()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (!File.Exists(FlagPath))
                return;

            File.Delete(FlagPath);
            if (File.Exists(FlagPath + ".meta"))
                File.Delete(FlagPath + ".meta");

            Strip(false);
        }

        static void Strip(bool dialog)
        {
            int tiles = 0;
            int instances = 0;

            string[] guids = AssetDatabase.FindAssets("t:TerrainData", new[] { TerrainDataFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                if (data == null)
                    continue;

                instances += data.treeInstanceCount;
                data.SetTreeInstances(new TreeInstance[0], false);
                data.treePrototypes = new TreePrototype[0];
                EditorUtility.SetDirty(data);
                tiles++;
            }

            Terrain[] live = Terrain.activeTerrains;
            for (int i = 0; i < live.Length; i++)
            {
                Terrain terrain = live[i];
                if (terrain == null || terrain.terrainData == null)
                    continue;
                terrain.terrainData.SetTreeInstances(new TreeInstance[0], false);
                terrain.terrainData.treePrototypes = new TreePrototype[0];
                terrain.Flush();
                EditorUtility.SetDirty(terrain.terrainData);
            }

            AssetDatabase.SaveAssets();
            string msg = "Cleared trees on " + tiles + " terrain tile(s), " + instances + " instances.";
            Debug.Log(msg);
            if (dialog)
                EditorUtility.DisplayDialog("Strip Trees", msg, "OK");
        }
    }
}
#endif

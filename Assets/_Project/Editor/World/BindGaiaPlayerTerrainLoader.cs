using System.IO;
using Gaia;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BindGaiaPlayerTerrainLoader
{
    [MenuItem("Dark Matter Genesis/World/Bind Player_v7 Terrain Loader")]
    public static void Bind()
    {
        GameObject player = GameObject.Find("Player_v7");
        if (player == null)
        {
            Debug.LogError("Player_v7 is not in the open scene.");
            return;
        }

        PioneerGaiaTerrainFollow follow = player.GetComponent<PioneerGaiaTerrainFollow>();
        if (follow == null)
        {
            follow = player.AddComponent<PioneerGaiaTerrainFollow>();
        }
        follow.loadRange = 1800f;
        EditorUtility.SetDirty(player);
        Debug.Log("Player_v7 will load Gaia tiles within 1800m at play.");
    }

    [MenuItem("Dark Matter Genesis/World/Set Pixel Error 25 On All Terrains")]
    public static void SetPixelError25()
    {
        const float pixelError = 25f;
        int changed = 0;
        Terrain[] loaded = Terrain.activeTerrains;
        for (int i = 0; i < loaded.Length; i++)
        {
            if (ApplyPixelError(loaded[i], pixelError))
                changed++;
        }

        string root = Path.Combine(Application.dataPath, "Gaia User Data/Sessions/DM Genesis/Terrain Scenes");
        if (Directory.Exists(root))
        {
            string activePath = EditorSceneManager.GetActiveScene().path;
            string[] files = Directory.GetFiles(root, "Terrain_*.unity");
            for (int i = 0; i < files.Length; i++)
            {
                string assetPath = "Assets" + files[i].Substring(Application.dataPath.Length).Replace("\\", "/");
                Scene scene = EditorSceneManager.GetSceneByPath(assetPath);
                bool opened = false;
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);
                    opened = true;
                }

                bool dirty = false;
                GameObject[] roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                {
                    Terrain[] terrains = roots[r].GetComponentsInChildren<Terrain>(true);
                    for (int t = 0; t < terrains.Length; t++)
                    {
                        if (ApplyPixelError(terrains[t], pixelError))
                        {
                            dirty = true;
                            changed++;
                        }
                    }
                }

                if (dirty)
                    EditorSceneManager.SaveScene(scene);

                if (opened && scene.path != activePath)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        Debug.Log("Set heightmapPixelError to 25 on " + changed + " terrain(s).");
    }

    static bool ApplyPixelError(Terrain terrain, float pixelError)
    {
        if (terrain == null)
            return false;
        if (Mathf.Approximately(terrain.heightmapPixelError, pixelError))
            return false;
        Undo.RecordObject(terrain, "Pixel Error 25");
        terrain.heightmapPixelError = pixelError;
        EditorUtility.SetDirty(terrain);
        return true;
    }
}

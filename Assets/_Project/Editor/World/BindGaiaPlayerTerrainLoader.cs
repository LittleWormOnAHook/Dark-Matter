using Gaia;
using UnityEditor;
using UnityEngine;

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
}

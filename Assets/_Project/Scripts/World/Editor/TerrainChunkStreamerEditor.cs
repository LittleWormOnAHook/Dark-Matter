#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Project.World;

namespace Project.World.Editor
{
    [CustomEditor(typeof(TerrainChunkStreamer))]
    public class TerrainChunkStreamerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            TerrainChunkStreamer streamer = (TerrainChunkStreamer)target;
            EditorGUILayout.Space();
            if (GUILayout.Button("Collect Chunks From Children"))
            {
                streamer.CollectChunks();
                EditorUtility.SetDirty(streamer);
            }

            EditorGUILayout.HelpBox(
                "Drop this on the hierarchy folder that holds the 16 terrain chunks. " +
                "At play it keeps the 3 nearest to the player drawn (Gaia-style, no additive scenes). " +
                "Names like Terrain_2_1 set neighbors on the seams.",
                MessageType.Info);
            EditorGUILayout.LabelField("Chunks found", streamer.ChunkCount.ToString());
            EditorGUILayout.LabelField("Active", streamer.ActiveCount.ToString());
        }

        [MenuItem("GameObject/Dark Matter/Add Terrain Chunk Streamer", false, 10)]
        private static void AddToSelection()
        {
            GameObject go = Selection.activeGameObject;
            if (go == null)
            {
                Debug.LogWarning("Select the terrain folder in the hierarchy first.");
                return;
            }

            TerrainChunkStreamer streamer = go.GetComponent<TerrainChunkStreamer>();
            if (streamer == null)
                streamer = go.AddComponent<TerrainChunkStreamer>();
            streamer.CollectChunks();
            EditorUtility.SetDirty(go);
            Debug.Log("TerrainChunkStreamer on '" + go.name + "' — " + streamer.ChunkCount + " chunks.");
        }
    }
}
#endif
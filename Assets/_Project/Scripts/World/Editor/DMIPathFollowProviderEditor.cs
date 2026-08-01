#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using MalbersAnimations.PathCreation;

namespace Project.World.Editor
{
    /// <summary>
    /// Optional AI-follow inspector only. Does not implement OnSceneGUI —
    /// Path Creator's <c>PathEditor</c> owns Scene bezier handles.
    /// </summary>
    [CustomEditor(typeof(DMIPathFollowProvider))]
    public class DMIPathFollowProviderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var provider = (DMIPathFollowProvider)target;
            EditorGUILayout.Space(6f);

            if (GUILayout.Button("Refresh Anchor Cache"))
            {
                provider.RefreshPath();
                EditorUtility.SetDirty(provider);
            }

            if (GUILayout.Button("Strip Legacy DMI Anchor Handles"))
            {
                Undo.RecordObject(provider, "Strip Legacy DMI Anchor Handles");
                provider.RemoveTransformAnchors();
                EditorUtility.SetDirty(provider);
            }

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Assign Manual Agents"))
                    provider.AssignManualAgents();
            }

            EditorGUILayout.HelpBox(
                "Optional AI follow add-on. Path Creator (SL) owns Bézier / Vertex tabs and Scene anchors. " +
                "Edit with the Path Creator component above — Shift-click add, Ctrl-click delete. " +
                "Loop = ordered anchors. PingPong = reverse for enemies/creatures, random next for pets. " +
                "Patrol Wait Duration applies to pet idle-at-anchor. Followers refresh on pathUpdated; no DMI Anchor Handle children.",
                MessageType.Info);
        }
    }
}
#endif

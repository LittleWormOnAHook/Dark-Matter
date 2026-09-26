// force-reimport 2026-09-20-lt-aim
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// RETIRED as binding authority. InputSystem_Actions.inputactions + Project Settings > Input System Package own binds.
    /// This tab only pings the asset; it must not Apply/Save/Listen-overwrite bindings.
    /// stamp: controls-panel-retired-stub 0920h
    /// </summary>
    internal sealed class DMStudioControlsPanel
    {
        private const string ActionsPath = "Assets/_Project/Settings/Input/InputSystem_Actions.inputactions";

        private InputActionAsset asset;
        private string status = string.Empty;

        public void Dispose()
        {
            asset = null;
            status = string.Empty;
        }

        public void Draw()
        {
            EnsureAsset();
            if (asset == null)
            {
                EditorGUILayout.HelpBox(
                    "InputSystem_Actions not found at " + ActionsPath,
                    MessageType.Error);
                return;
            }

            EditorGUILayout.HelpBox(
                "RETIRED — Genesis Studio is not binding authority.\n\n" +
                "Edit binds only in:\n" +
                "• Assets/_Project/Settings/Input/InputSystem_Actions.inputactions\n" +
                "• Edit > Project Settings > Input System Package\n\n" +
                "Do not use Apply/Save/Listen here — wrong KBM rows were confusing the real Input System asset.",
                MessageType.Warning);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Authority asset", GUILayout.Width(100f));
            EditorGUILayout.ObjectField(asset, typeof(InputActionAsset), false);
            if (GUILayout.Button("Ping", GUILayout.Width(52f)))
                EditorGUIUtility.PingObject(asset);
            if (GUILayout.Button("Select / Open", GUILayout.Width(100f)))
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(status))
                EditorGUILayout.LabelField(status, EditorStyles.miniLabel);
        }

        private void EnsureAsset()
        {
            if (asset != null)
                return;
            asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            if (asset == null)
                status = "Missing InputSystem_Actions at " + ActionsPath;
        }
    }
}
#endif

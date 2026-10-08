#if UNITY_EDITOR
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Shared IMGUI drawer for studio roadmap tabs (phases + status + doc links).
    /// </summary>
    public static class DMStudioRoadmapPanel
    {
        public enum Status
        {
            NotStarted,
            InProgress,
            Done,
            Blocked
        }

        public static void DrawIntro(string summary, MessageType messageType = MessageType.Info)
        {
            if (!string.IsNullOrEmpty(summary))
                EditorGUILayout.HelpBox(summary, messageType);
        }

        public static void DrawDocLinks(params (string label, string assetPath)[] docs)
        {
            if (docs == null || docs.Length == 0)
                return;

            DMStudioStyles.DrawSection("Canonical docs", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.HelpBox(
                    "Opens the asset in the Inspector or reveals the file in your OS file browser.",
                    MessageType.None);

                foreach ((string label, string assetPath) in docs)
                {
                    if (string.IsNullOrEmpty(assetPath))
                        continue;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(label, GUILayout.MinWidth(140f));
                    if (GUILayout.Button("Open", GUILayout.Width(56f)))
                        OpenAsset(assetPath);
                    if (GUILayout.Button("Reveal", GUILayout.Width(64f)))
                        RevealAsset(assetPath);
                    EditorGUILayout.EndHorizontal();
                }
            });

            EditorGUILayout.Space(4f);
        }

        public static void DrawPhase(string id, string title, Status status, string note)
        {
            DMStudioStyles.DrawSection(FormatHeading(id, title), DMStudioStyles.ContentPanel, () =>
            {
                DrawStatusRow(status);
                if (!string.IsNullOrEmpty(note))
                    EditorGUILayout.HelpBox(note, MessageType.None);
            });
        }

        public static void DrawExternalStudioButton(string label, string menuPath)
        {
            if (string.IsNullOrEmpty(menuPath))
                return;

            if (GUILayout.Button(label, GUILayout.Height(28f)))
                EditorApplication.ExecuteMenuItem(menuPath);
        }

        public static string FormatStatusLabel(Status status)
        {
            switch (status)
            {
                case Status.Done:
                    return "Done";
                case Status.InProgress:
                    return "In progress";
                case Status.Blocked:
                    return "Blocked";
                default:
                    return "Not started";
            }
        }

        public static void OpenAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;

            Object obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (obj != null)
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
                return;
            }

            RevealAsset(assetPath);
        }

        public static void RevealAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;

            string projectRelative = assetPath.Replace('\\', '/');
            if (!projectRelative.StartsWith("Assets/"))
                projectRelative = "Assets/" + projectRelative.TrimStart('/');

            string fullPath = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath, "..", projectRelative));

            if (System.IO.File.Exists(fullPath) || System.IO.Directory.Exists(fullPath))
                EditorUtility.RevealInFinder(fullPath);
            else
                Debug.LogWarning("[DMStudioRoadmap] Path not found: " + projectRelative);
        }

        private static string FormatHeading(string id, string title)
        {
            if (string.IsNullOrEmpty(id))
                return title ?? string.Empty;
            if (string.IsNullOrEmpty(title))
                return id;
            return id + "  " + title;
        }

        private static void DrawStatusRow(Status status)
        {
            Color previous = GUI.contentColor;
            GUI.contentColor = StatusColor(status);
            EditorGUILayout.LabelField("Status", FormatStatusLabel(status), EditorStyles.boldLabel);
            GUI.contentColor = previous;
        }

        private static Color StatusColor(Status status)
        {
            switch (status)
            {
                case Status.Done:
                    return DarkMatterGenesisUiPalette.PositiveGreen;
                case Status.InProgress:
                    return DarkMatterGenesisUiPalette.Gold;
                case Status.Blocked:
                    return DarkMatterGenesisUiPalette.RichFuchsia;
                default:
                    return DarkMatterGenesisUiPalette.SoftBeigeGray;
            }
        }
    }
}
#endif

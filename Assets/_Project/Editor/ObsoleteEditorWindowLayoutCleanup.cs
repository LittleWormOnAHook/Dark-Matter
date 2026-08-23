using System.IO;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Strips obsolete ItemDataCreatorWindow panes from saved .wlt/.dwlt layouts so
    /// Play Mode FinalizePlaymodeLayout no longer logs invalid-window errors.
    /// </summary>
    [InitializeOnLoad]
    internal static class ObsoleteEditorWindowLayoutCleanup
    {
        private const string ObsoleteTypeToken = "ItemDataCreatorWindow";
        private const string ObsoleteTitleToken = "Item Data Creator";
        private const string PrefKey = "DM.LayoutCleanup.ItemDataCreator.v3";

        static ObsoleteEditorWindowLayoutCleanup()
        {
            EditorApplication.delayCall += RunOncePerEditorSession;
        }

        private static void RunOncePerEditorSession()
        {
            if (SessionState.GetBool(PrefKey, false))
                return;

            SessionState.SetBool(PrefKey, true);
            CloseLiveWindowsByTitle(ObsoleteTitleToken);
            ScrubLayoutFiles();
        }

        private static void CloseLiveWindowsByTitle(string title)
        {
            EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            for (int i = 0; i < windows.Length; i++)
            {
                EditorWindow window = windows[i];
                if (window == null || window.titleContent == null)
                    continue;

                if (window.titleContent.text == title)
                    window.Close();
            }
        }

        private static void ScrubLayoutFiles()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty;
            string appDataLayouts = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                "Unity", "Editor-5.x", "Preferences", "Layouts");

            string[] roots =
            {
                Path.Combine(Application.dataPath, "_Project", "Scenes"),
                Path.Combine(projectRoot, "UserSettings", "Layouts"),
                Path.Combine(projectRoot, "Library"),
                appDataLayouts,
            };

            for (int r = 0; r < roots.Length; r++)
            {
                string root = roots[r];
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                    continue;

                string[] files;
                try
                {
                    files = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories);
                }
                catch
                {
                    continue;
                }

                for (int i = 0; i < files.Length; i++)
                {
                    string path = files[i];
                    if (!path.EndsWith(".wlt") && !path.EndsWith(".dwlt"))
                        continue;

                    ScrubOneLayoutFile(path);
                }
            }
        }

        private static void ScrubOneLayoutFile(string path)
        {
            string text;
            try
            {
                text = File.ReadAllText(path);
            }
            catch
            {
                return;
            }

            if (text.IndexOf(ObsoleteTypeToken, System.StringComparison.Ordinal) < 0
                && text.IndexOf(ObsoleteTitleToken, System.StringComparison.Ordinal) < 0)
            {
                return;
            }

            // Soft scrub: drop m_Panes entries that point at blocks containing the obsolete type.
            // Full YAML surgery is fragile; primary fix is Dark Matter Layout.wlt + static menu type.
            string scrubbed = text
                .Replace("Assembly-CSharp-Editor::ItemDataCreatorWindow", "UnityEditor.dll::UnityEditor.InspectorWindow")
                .Replace("Project.EditorTools.ItemDataCreatorWindow", "UnityEditor.InspectorWindow")
                .Replace("m_Text: Item Data Creator", "m_Text: Inspector")
                .Replace("m_TextWithWhitespace: \"Item Data Creator\\u200B\"", "m_TextWithWhitespace: \"Inspector\\u200B\"");

            if (scrubbed == text)
                return;

            try
            {
                File.WriteAllText(path, scrubbed);
                Debug.Log($"ObsoleteEditorWindowLayoutCleanup: scrubbed {path}");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"ObsoleteEditorWindowLayoutCleanup: could not scrub {path}: {ex.Message}");
            }
        }
    }
}

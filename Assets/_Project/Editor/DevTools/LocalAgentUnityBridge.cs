using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LocalAgentConversationEntry
{
    public string role;
    public string content;
    public DateTime timestamp;

    public LocalAgentConversationEntry(string role, string content)
    {
        this.role = role;
        this.content = content;
        timestamp = DateTime.Now;
    }
}

public static class LocalAgentUnityBridge
{
    public const string SystemPrompt =
        "You are a Unity editor assistant for this project. Help edit scripts, prefabs, scenes, and project setup. " +
        "When you provide a full file to create or replace, use a fenced code block and put the asset path on the first line inside the block as: " +
        "// File: Assets/relative/path/File.cs\n" +
        "The user can click Apply Edits in the Local Agent window to write those files into the project. " +
        "Prefer minimal, focused changes. Use existing project conventions when known from context.";

    private static readonly Regex CodeBlockRegex = new Regex(
        @"```(?:csharp|cs|c#)?[^\n]*\r?\n(?://\s*(?:File|Path)\s*:\s*(?<path>Assets/[^\r\n]+)\s*\r?\n)(?<code>[\s\S]*?)```",
        RegexOptions.Multiline | RegexOptions.IgnoreCase);

    public static string BuildProjectContext(bool includeSelection, bool includeScriptSource, int maxScriptChars = 12000)
    {
        var sb = new StringBuilder(2048);
        sb.AppendLine("[Unity Project Context]");
        sb.AppendLine($"Unity Version: {Application.unityVersion}");
        sb.AppendLine($"Product: {Application.productName}");
        sb.AppendLine($"Project Path: {Directory.GetCurrentDirectory()}");

        Scene activeScene = SceneManager.GetActiveScene();
        sb.AppendLine($"Active Scene: {(string.IsNullOrEmpty(activeScene.path) ? activeScene.name : activeScene.path)}");
        sb.AppendLine($"Scene Dirty: {activeScene.isDirty}");

        if (includeSelection)
            AppendSelectionContext(sb, includeScriptSource, maxScriptChars);

        sb.AppendLine("[End Unity Project Context]");
        return sb.ToString();
    }

    public static int TryApplyFileEdits(string agentResponse, out List<string> results)
    {
        results = new List<string>();
        if (string.IsNullOrEmpty(agentResponse))
        {
            results.Add("No response content to apply.");
            return 0;
        }

        int applied = 0;
        MatchCollection matches = CodeBlockRegex.Matches(agentResponse);
        foreach (Match match in matches)
        {
            if (!match.Success)
                continue;

            string assetPath = match.Groups["path"].Value.Trim().Replace('\\', '/');
            string code = match.Groups["code"].Value.TrimEnd();

            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                results.Add($"Skipped invalid path: {assetPath}");
                continue;
            }

            string fullPath = Path.GetFullPath(assetPath);
            string projectRoot = Directory.GetCurrentDirectory();
            if (!fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                results.Add($"Skipped path outside project: {assetPath}");
                continue;
            }

            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(fullPath, code, Encoding.UTF8);
            results.Add($"Wrote {assetPath} ({code.Length} chars)");
            applied++;
        }

        if (applied > 0)
        {
            AssetDatabase.Refresh();
            results.Add("AssetDatabase refreshed.");
        }
        else
        {
            results.Add("No editable code blocks found. Use // File: Assets/... inside ```csharp blocks.");
        }

        return applied;
    }

    public static string FormatTranscript(IReadOnlyList<LocalAgentConversationEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Local Agent Transcript ===");
        sb.AppendLine($"Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Project: {Application.productName}");
        sb.AppendLine();

        foreach (LocalAgentConversationEntry entry in entries)
        {
            sb.Append('[').Append(entry.role.ToUpperInvariant()).Append("] ");
            sb.AppendLine(entry.timestamp.ToString("HH:mm:ss"));
            sb.AppendLine(entry.content);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static void AppendSelectionContext(StringBuilder sb, bool includeScriptSource, int maxScriptChars)
    {
        UnityEngine.Object[] selected = Selection.objects;
        if (selected == null || selected.Length == 0)
        {
            sb.AppendLine("Selection: (none)");
            return;
        }

        sb.AppendLine($"Selection Count: {selected.Length}");
        foreach (UnityEngine.Object obj in selected)
        {
            if (obj == null)
                continue;

            string path = AssetDatabase.GetAssetPath(obj);
            sb.AppendLine($"- {obj.name} ({obj.GetType().Name}){(string.IsNullOrEmpty(path) ? string.Empty : $" @ {path}")}");

            if (obj is GameObject go)
                AppendGameObjectContext(sb, go, 0, 2);

            if (includeScriptSource)
                AppendScriptSource(sb, obj, maxScriptChars);
        }
    }

    private static void AppendGameObjectContext(StringBuilder sb, GameObject go, int depth, int maxDepth)
    {
        string indent = new string(' ', depth * 2);
        sb.Append(indent).Append("Transform: ").AppendLine(FormatTransform(go.transform));

        Component[] components = go.GetComponents<Component>();
        foreach (Component component in components)
        {
            if (component == null)
                continue;

            sb.Append(indent).Append("Component: ").AppendLine(component.GetType().FullName);
        }

        if (depth >= maxDepth)
            return;

        for (int i = 0; i < go.transform.childCount; i++)
            AppendGameObjectContext(sb, go.transform.GetChild(i).gameObject, depth + 1, maxDepth);
    }

    private static void AppendScriptSource(StringBuilder sb, UnityEngine.Object obj, int maxScriptChars)
    {
        string assetPath = AssetDatabase.GetAssetPath(obj);
        MonoScript script = obj as MonoScript;
        if (script == null && obj is GameObject go)
        {
            Component[] components = go.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null)
                    continue;

                MonoScript ms = MonoScript.FromMonoBehaviour(component as MonoBehaviour);
                if (ms != null)
                {
                    script = ms;
                    assetPath = AssetDatabase.GetAssetPath(ms);
                    break;
                }
            }
        }

        if (script == null)
        {
            if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                AppendFileSnippet(sb, assetPath, maxScriptChars);
            }

            return;
        }

        AppendFileSnippet(sb, assetPath, maxScriptChars);
    }

    private static void AppendFileSnippet(StringBuilder sb, string assetPath, int maxScriptChars)
    {
        if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            return;

        string fullPath = Path.GetFullPath(assetPath);
        if (!File.Exists(fullPath))
            return;

        string source = File.ReadAllText(fullPath);
        if (source.Length > maxScriptChars)
            source = source.Substring(0, maxScriptChars) + "\n... [truncated]";

        sb.AppendLine($"Script Source: {assetPath}");
        sb.AppendLine("```csharp");
        sb.AppendLine(source);
        sb.AppendLine("```");
    }

    private static string FormatTransform(Transform transform)
    {
        Vector3 p = transform.localPosition;
        Vector3 r = transform.localEulerAngles;
        Vector3 s = transform.localScale;
        return $"pos({p.x:F2}, {p.y:F2}, {p.z:F2}) rot({r.x:F1}, {r.y:F1}, {r.z:F1}) scale({s.x:F2}, {s.y:F2}, {s.z:F2})";
    }
}

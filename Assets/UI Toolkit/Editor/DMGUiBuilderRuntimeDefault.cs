using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace DarkMatterGenesis.Editor
{
    /// <summary>
    /// Keeps UI Builder from breaking Play:
    /// editor-extension-mode False, relative Style src, overlay names intact.
    /// Does not lock files. Does not parent UITK_Loading under UITK_Root.
    /// </summary>
    [InitializeOnLoad]
    static class DMGUiBuilderRuntimeDefault
    {
        const string PrefKey = "DMG.UiBuilder.EditorExtensionModeDefaultOff.Applied";
        const string ToolkitRoot = "Assets/UI Toolkit";
        const string LoadingOverlayPath = "Assets/UI Toolkit/Screens/LoadingOverlay.uxml";
        const string LogStamp = "DMUiToolkit 0831-hide";

        static readonly string[] RequiredNames =
        {
            "loading-root", "veil", "content", "blackhole", "title", "status", "percent", "progress-fill"
        };

        static readonly (string Name, string ParentName)[] RestoreOrder =
        {
            ("loading-root", null),
            ("veil", "loading-root"),
            ("content", "loading-root"),
            ("space", "content"),
            ("stars", "content"),
            ("blackhole", "content"),
            ("title", "content"),
            ("progress-block", "content"),
            ("status", "progress-block"),
            ("percent", "progress-block"),
            ("progress-track", "progress-block"),
            ("progress-fill", "progress-track")
        };

        static readonly HashSet<string> InFlight = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        static bool playStamped;

        static DMGUiBuilderRuntimeDefault()
        {
            EditorUserSettings.SetConfigValue("UIBuilder.EditorExtensionModeKey", "False");
            EditorPrefs.SetBool(PrefKey, true);
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
            EditorApplication.delayCall += SanitizeAllToolkitUxml;
        }

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || playStamped)
                return;
            playStamped = true;
            Debug.Log(LogStamp + " UXML sanitizer on — Save cannot set Editor Extension, project:// Style src, or drop overlay names");
        }

        static void SanitizeAllToolkitUxml()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            string[] guids = AssetDatabase.FindAssets("t:VisualTreeAsset", new[] { ToolkitRoot });
            bool dirty = false;
            for (int i = 0; i < (guids != null ? guids.Length : 0); i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                dirty |= SanitizeAsset(path);
            }

            if (dirty)
                EditorApplication.delayCall += () => AssetDatabase.Refresh();
        }

        internal static bool SanitizeAsset(string assetPath)
        {
            if (!IsToolkitUxml(assetPath))
                return false;
            if (!InFlight.Add(assetPath))
                return false;

            try
            {
                string fullPath = ToFullPath(assetPath);
                if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                    return false;

                string original = File.ReadAllText(fullPath, Encoding.UTF8);
                string sanitized = SanitizeText(original, assetPath);
                if (string.Equals(original, sanitized, StringComparison.Ordinal))
                    return false;

                File.WriteAllText(fullPath, sanitized, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                Debug.Log(LogStamp + " sanitized " + assetPath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(LogStamp + " UXML sanitizer skipped " + assetPath + ": " + exception.Message);
                return false;
            }
            finally
            {
                InFlight.Remove(assetPath);
            }
        }

        static string SanitizeText(string text, string assetPath)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            text = ForceRuntimeExtensionMode(text);
            text = RewriteProjectStyleSrc(text, assetPath);
            text = RestoreMissingOverlayNames(text, assetPath);
            return text;
        }

        static string ForceRuntimeExtensionMode(string text)
        {
            text = Regex.Replace(
                text,
                @"editor-extension-mode\s*=\s*([""'])True\1",
                "editor-extension-mode=$1False$1",
                RegexOptions.IgnoreCase);

            if (!Regex.IsMatch(text, @"editor-extension-mode\s*=", RegexOptions.IgnoreCase))
            {
                Match open = Regex.Match(text, @"<ui:UXML\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (open.Success)
                {
                    string tag = open.Value;
                    string insert = tag.Contains("\n")
                        ? "\n    editor-extension-mode=\"False\""
                        : " editor-extension-mode=\"False\"";
                    int gt = tag.LastIndexOf('>');
                    if (gt > 0)
                    {
                        string replaced = tag.Substring(0, gt) + insert + tag.Substring(gt);
                        text = text.Substring(0, open.Index) + replaced + text.Substring(open.Index + open.Length);
                    }
                }
            }

            text = Regex.Replace(
                text,
                @"\s+xmlns:uie\s*=\s*[""']UnityEditor\.UIElements[""']",
                string.Empty,
                RegexOptions.IgnoreCase);
            return text;
        }

        static string RewriteProjectStyleSrc(string text, string assetPath)
        {
            return Regex.Replace(
                text,
                @"src\s*=\s*([""'])project://database/([^""']+?)\1",
                match =>
                {
                    string quote = match.Groups[1].Value;
                    string raw = Uri.UnescapeDataString(match.Groups[2].Value);
                    int cut = raw.IndexOfAny(new[] { '?', '#' });
                    if (cut >= 0)
                        raw = raw.Substring(0, cut);
                    raw = raw.TrimStart('/');
                    if (!raw.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                        return match.Value;

                    string relative = MakeRelativeAssetPath(assetPath, raw);
                    if (string.IsNullOrEmpty(relative))
                        return match.Value;
                    return "src=" + quote + relative + quote;
                },
                RegexOptions.IgnoreCase);
        }

        static string MakeRelativeAssetPath(string fromAsset, string toAsset)
        {
            string fromDir = Path.GetDirectoryName(fromAsset);
            if (string.IsNullOrEmpty(fromDir))
                return toAsset.Replace('\\', '/');

            fromDir = fromDir.Replace('\\', '/').TrimEnd('/') + "/";
            string to = toAsset.Replace('\\', '/');
            try
            {
                Uri fromUri = new Uri("file:///x/" + fromDir);
                Uri toUri = new Uri("file:///x/" + to);
                string relative = Uri.UnescapeDataString(fromUri.MakeRelativeUri(toUri).ToString());
                return string.IsNullOrEmpty(relative) ? to : relative.Replace('\\', '/');
            }
            catch (UriFormatException)
            {
                return to;
            }
        }

        static string RestoreMissingOverlayNames(string text, string assetPath)
        {
            if (!IsLoadingOverlay(assetPath))
                return text;

            if (HasRequiredNames(text))
            {
                SaveLastGood(text);
                return text;
            }

            string known = LoadLastGood();
            XDocument current;
            XDocument good;
            try
            {
                current = XDocument.Parse(text, LoadOptions.PreserveWhitespace);
                good = XDocument.Parse(known, LoadOptions.PreserveWhitespace);
            }
            catch (Exception)
            {
                Debug.LogWarning(LogStamp + " LoadingOverlay.uxml unreadable — restored last known-good overlay tree");
                return known;
            }

            bool changed = false;
            for (int pass = 0; pass < RestoreOrder.Length; pass++)
            {
                for (int i = 0; i < RestoreOrder.Length; i++)
                {
                    string name = RestoreOrder[i].Name;
                    if (FindByName(current, name) != null)
                        continue;

                    XElement template = FindByName(good, name);
                    if (template == null)
                        continue;

                    XElement parent;
                    if (string.IsNullOrEmpty(RestoreOrder[i].ParentName))
                        parent = current.Root;
                    else
                        parent = FindByName(current, RestoreOrder[i].ParentName);

                    if (parent == null)
                        continue;

                    parent.Add(new XElement(template));
                    changed = true;
                }
            }

            if (!changed)
            {
                if (!HasRequiredNames(text))
                {
                    Debug.LogWarning(LogStamp + " LoadingOverlay.uxml missing required names — restored last known-good overlay tree");
                    return known;
                }
                return text;
            }

            string newline = text.Contains("\r\n") ? "\r\n" : "\n";
            string restored = SaveXDocument(current, newline);
            if (!HasRequiredNames(restored))
                restored = known;

            SaveLastGood(restored);
            Debug.Log(LogStamp + " restored missing overlay names in " + assetPath);
            return restored;
        }

        static bool HasRequiredNames(string text)
        {
            for (int i = 0; i < RequiredNames.Length; i++)
            {
                if (text.IndexOf("name=\"" + RequiredNames[i] + "\"", StringComparison.Ordinal) < 0
                    && text.IndexOf("name='" + RequiredNames[i] + "'", StringComparison.Ordinal) < 0)
                    return false;
            }
            return true;
        }

        static XElement FindByName(XDocument document, string name)
        {
            if (document == null || document.Root == null)
                return null;

            foreach (XElement element in document.Descendants())
            {
                XAttribute attribute = element.Attribute("name");
                if (attribute != null && attribute.Value == name)
                    return element;
            }
            return null;
        }

        static string SaveXDocument(XDocument document, string newline)
        {
            StringBuilder builder = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = false,
                Indent = true,
                IndentChars = "    ",
                NewLineChars = newline,
                NewLineHandling = NewLineHandling.Replace
            };
            using (XmlWriter writer = XmlWriter.Create(builder, settings))
                document.Save(writer);

            string result = builder.ToString();
            result = Regex.Replace(result, @"encoding=""utf-16""", "encoding=\"utf-8\"", RegexOptions.IgnoreCase);
            if (!result.EndsWith(newline))
                result += newline;
            return result;
        }

        static string LastGoodPath()
        {
            string project = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(project))
                return null;
            return Path.Combine(project, "Library", "DMGUiToolkit", "LoadingOverlay.lastgood.uxml");
        }

        static void SaveLastGood(string text)
        {
            if (!HasRequiredNames(text))
                return;

            try
            {
                string path = LastGoodPath();
                if (string.IsNullOrEmpty(path))
                    return;
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, text, new UTF8Encoding(false));
            }
            catch (Exception)
            {
            }
        }

        static string LoadLastGood()
        {
            try
            {
                string path = LastGoodPath();
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    string text = File.ReadAllText(path, Encoding.UTF8);
                    if (HasRequiredNames(text))
                        return text;
                }
            }
            catch (Exception)
            {
            }

            string assetFull = ToFullPath(LoadingOverlayPath);
            if (!string.IsNullOrEmpty(assetFull) && File.Exists(assetFull))
            {
                string live = File.ReadAllText(assetFull, Encoding.UTF8);
                if (HasRequiredNames(live))
                    return live;
            }

            return EmbeddedKnownGood;
        }

        static bool IsToolkitUxml(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".uxml", StringComparison.OrdinalIgnoreCase))
                return false;
            string normalized = assetPath.Replace('\\', '/');
            return normalized.StartsWith(ToolkitRoot + "/", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsLoadingOverlay(string assetPath)
        {
            return assetPath.Replace('\\', '/').EndsWith("/LoadingOverlay.uxml", StringComparison.OrdinalIgnoreCase);
        }

        static string ToFullPath(string assetPath)
        {
            string project = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(project))
                return null;
            return Path.GetFullPath(Path.Combine(project, assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        const string EmbeddedKnownGood =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<ui:UXML
    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
    xmlns:ui=""UnityEngine.UIElements""
    xsi:noNamespaceSchemaLocation=""../../../UIElementsSchema/UIElements.xsd""
    editor-extension-mode=""False""
>
    <ui:Style src=""../Themes/DarkMatterGenesis.uss"" />
    <ui:Style src=""LoadingOverlay.uss"" />
    <ui:VisualElement name=""loading-root"" class=""dmg-loading-root"" picking-mode=""Position"">
        <ui:VisualElement name=""veil"" class=""dmg-veil"" picking-mode=""Ignore"" />
        <ui:VisualElement name=""content"" class=""dmg-loading-content"" picking-mode=""Ignore"">
            <ui:VisualElement name=""space"" class=""dmg-space"" picking-mode=""Ignore"" />
            <ui:VisualElement name=""stars"" class=""dmg-stars"" picking-mode=""Ignore"" />
            <ui:VisualElement name=""blackhole"" class=""dmg-blackhole"" picking-mode=""Ignore"" />
            <ui:Label name=""title"" class=""dmg-title"" text=""DARK MATTER : GENESIS"" picking-mode=""Ignore"" />
            <ui:VisualElement name=""progress-block"" class=""dmg-progress-block"" picking-mode=""Ignore"">
                <ui:Label name=""status"" class=""dmg-status"" text=""Loading Genesis..."" picking-mode=""Ignore"" />
                <ui:Label name=""percent"" class=""dmg-percent"" text=""0%"" picking-mode=""Ignore"" />
                <ui:VisualElement name=""progress-track"" class=""dmg-progress-track"" picking-mode=""Ignore"">
                    <ui:VisualElement name=""progress-fill"" class=""dmg-progress-fill"" picking-mode=""Ignore"" />
                </ui:VisualElement>
            </ui:VisualElement>
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
";
    }

    sealed class DMGUxmlSaveHook : UnityEditor.AssetModificationProcessor
    {
        static string[] OnWillSaveAssets(string[] paths)
        {
            if (paths == null)
                return paths;

            for (int i = 0; i < paths.Length; i++)
                DMGUiBuilderRuntimeDefault.SanitizeAsset(paths[i]);
            return paths;
        }
    }

    sealed class DMGUxmlImportHook : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            List<string> changed = null;
            AppendChanged(importedAssets, ref changed);
            AppendChanged(movedAssets, ref changed);
            if (changed == null || changed.Count == 0)
                return;

            EditorApplication.delayCall += () =>
            {
                bool dirty = false;
                for (int i = 0; i < changed.Count; i++)
                    dirty |= DMGUiBuilderRuntimeDefault.SanitizeAsset(changed[i]);
                if (dirty)
                    AssetDatabase.Refresh();
            };
        }

        static void AppendChanged(string[] paths, ref List<string> changed)
        {
            if (paths == null)
                return;
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".uxml", StringComparison.OrdinalIgnoreCase))
                    continue;
                string normalized = path.Replace('\\', '/');
                if (!normalized.StartsWith("Assets/UI Toolkit/", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (changed == null)
                    changed = new List<string>();
                changed.Add(path);
            }
        }
    }
}

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>
    /// Extension point for the Cliffs section (and any later builder). A builder supplies its own controls and a preview
    /// subject; it shares the studio's 512 icon window and asset library through <see cref="IPcgStudioContext"/>.
    /// Register from an [InitializeOnLoad] class: <c>PcgStudioExtensions.RegisterCliffBuilder(new MyCliffBuilder());</c>
    /// </summary>
    public interface IPcgCliffBuilder
    {
        string DisplayName { get; }
        /// <summary>Controls shown in the Cliffs section. Called once per studio panel.</summary>
        VisualElement CreateGUI(IPcgStudioContext context);
        /// <summary>Called when the panel closes (release previews / temp objects).</summary>
        void Dispose();
    }

    /// <summary>What the studio offers a builder.</summary>
    public interface IPcgStudioContext
    {
        PcgIconView IconView { get; }
        PcgAssetLibrary Library { get; }
        /// <summary>Shows <paramref name="build"/>'s object in the icon window (null clears it). The view owns and destroys it.</summary>
        void SetPreviewSubject(Func<GameObject> build);
        /// <summary>Adds (or updates) a library entry for <paramref name="prefab"/> with an icon from the icon window's current angle.</summary>
        PcgAssetLibrary.Entry SaveToLibrary(GameObject prefab, string category, DmRockPreset preset, DmRockStyle style, int seed);
        string DefaultPrefabFolder { get; }
        void SetStatus(string message);
    }

    /// <summary>Theming hook, folders and builder registry for the Rocks and Cliffs studio. No host / theme dependencies.</summary>
    public static class PcgStudioExtensions
    {
        private static readonly List<IPcgCliffBuilder> s_cliffBuilders = new List<IPcgCliffBuilder>();
        public static IReadOnlyList<IPcgCliffBuilder> CliffBuilders => s_cliffBuilders;

        public static void RegisterCliffBuilder(IPcgCliffBuilder builder)
        {
            if (builder != null && !s_cliffBuilders.Contains(builder)) s_cliffBuilders.Add(builder);
        }

        /// <summary>
        /// Optional theming: raised for every new studio panel (standalone window or embedded in a host). A host project adds
        /// its stylesheets / fonts here (e.g. <c>panel.styleSheets.Add(theme)</c>); the panel only uses USS class names (pcg-*).
        /// </summary>
        public static event Action<VisualElement> PanelCreated;
        /// <summary>Extra stylesheets added to every panel (alternative to <see cref="PanelCreated"/>).</summary>
        public static readonly List<StyleSheet> ExtraStyleSheets = new List<StyleSheet>();

        internal static void RaisePanelCreated(VisualElement panel)
        {
            foreach (StyleSheet s in ExtraStyleSheets)
                if (s != null) panel.styleSheets.Add(s);
            if (PanelCreated == null) return;
            foreach (Action<VisualElement> a in PanelCreated.GetInvocationList())
            {
                try { a(panel); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        // ------------------------------------------------------------------------------------------------
        // Folders (resolved from this script's location so the package can move / become a UPM package)

        private static string s_root;

        /// <summary>Package root, e.g. "Assets/Genesis PCG Rock Creation" (or "Packages/com.x.y").</summary>
        public static string PackageRoot
        {
            get
            {
                if (!string.IsNullOrEmpty(s_root)) return s_root;
                s_root = "Assets/Genesis PCG Rock Creation";
                foreach (string g in AssetDatabase.FindAssets("PcgStudioExtensions t:MonoScript"))
                {
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    int i = p.IndexOf("/Editor/RockStudio/", StringComparison.Ordinal);
                    if (i > 0) { s_root = p.Substring(0, i); break; }
                }
                return s_root;
            }
        }

        /// <summary>Writable root for generated data: the package itself, or Assets/... when it lives read-only under Packages/.</summary>
        public static string DataRoot => PackageRoot.StartsWith("Packages/", StringComparison.Ordinal) ? "Assets/Genesis PCG Rock Creation" : PackageRoot;

        public static string LibraryFolder => DataRoot + "/Data/Library";
        public static string IconFolder => LibraryFolder + "/Icons";
        public static string LibraryPath => LibraryFolder + "/PCG_AssetLibrary.asset";
        public static string PrefabFolder => DataRoot + "/Generated/Prefabs";
        public static string StyleSheetPath => PackageRoot + "/Editor/RockStudio/PcgRockStudio.uss";

        internal static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            if (slash <= 0) return;
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
#endif

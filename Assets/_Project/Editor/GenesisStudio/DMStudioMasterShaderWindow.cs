#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>Studio: Rendering -> DM Shaders: Master. Creates DM_ copies of HDRP/Lit materials using DM/Master.
    /// Never modifies/reassigns originals, skips FORGE3D, materials only (no prefab loads), dry-run by default.</summary>
    public class DMStudioMasterShaderWindow : EditorWindow
    {
        public const string MenuPath = "Tools/Dark Matter Genesis/Rendering/DM Shaders/Master";
        const string ShaderName = "DM/Master";
        const string SourceShader = "HDRP/Lit";
        const string ManifestFolder = "Assets/_Project/Rendering/DMShaders/Manifests";

        [SerializeField] bool dryRun = true;
        [SerializeField] bool intoDMFolder = true;
        [SerializeField] string dmFolderName = "DM_Materials";
        [SerializeField] DefaultAsset sourceFolder;
        [SerializeField] bool useSelection = true;
        [SerializeField] int presetOnCopy = 0;
        Vector2 scroll;
        string lastReport = "";

        [MenuItem(MenuPath)]
        public static void Open() { GetWindow<DMStudioMasterShaderWindow>("DM Master Shader"); }

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Creates DM_ copies of HDRP/Lit materials with shader DM/Master (same property names, so textures/values carry over). Originals are never modified or reassigned. FORGE3D is always skipped. Assign copies manually where you want them.", MessageType.Info);
            dryRun = EditorGUILayout.Toggle("Dry Run (no files)", dryRun);
            useSelection = EditorGUILayout.Toggle("Use Project Selection", useSelection);
            using (new EditorGUI.DisabledScope(useSelection))
                sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField("Source Folder", sourceFolder, typeof(DefaultAsset), false);
            intoDMFolder = EditorGUILayout.Toggle("Put copies in subfolder", intoDMFolder);
            using (new EditorGUI.DisabledScope(!intoDMFolder))
                dmFolderName = EditorGUILayout.TextField("Subfolder name", dmFolderName);
            presetOnCopy = EditorGUILayout.Popup("Extras on copies", presetOnCopy, new[] { "Neutral (off)", "Recommended", "Extreme" });

            if (GUILayout.Button(dryRun ? "Scan (dry run)" : "Create DM_ copies", GUILayout.Height(28)))
                Run();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(lastReport, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        List<string> CollectMaterialPaths()
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (useSelection)
            {
                foreach (var o in Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets))
                {
                    string p = AssetDatabase.GetAssetPath(o);
                    if (string.IsNullOrEmpty(p)) continue;
                    if (AssetDatabase.IsValidFolder(p))
                        foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { p })) paths.Add(AssetDatabase.GUIDToAssetPath(g));
                    else if (p.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)) paths.Add(p);
                }
            }
            else if (sourceFolder != null)
            {
                string f = AssetDatabase.GetAssetPath(sourceFolder);
                if (AssetDatabase.IsValidFolder(f))
                    foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { f })) paths.Add(AssetDatabase.GUIDToAssetPath(g));
            }
            var list = new List<string>(paths); list.Sort(StringComparer.OrdinalIgnoreCase); return list;
        }

        static string Csv(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

        void Run()
        {
            Shader dm = Shader.Find(ShaderName);
            if (dm == null) { lastReport = "Shader DM/Master not found."; return; }
            var csv = new StringBuilder("original_path,copy_path,original_shader,new_shader,status\n");
            int made = 0, skipped = 0, planned = 0;
            foreach (string path in CollectMaterialPaths())
            {
                string status;
                string dst = "";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                string sh = mat != null && mat.shader != null ? mat.shader.name : "";
                if (path.IndexOf("FORGE3D", StringComparison.OrdinalIgnoreCase) >= 0 || sh.StartsWith("FORGE3D", StringComparison.OrdinalIgnoreCase)) status = "skip:FORGE3D";
                else if (!path.StartsWith("Assets/", StringComparison.Ordinal)) status = "skip:not in Assets";
                else if (mat == null) status = "skip:not a material";
                else if (sh != SourceShader) status = "skip:shader " + sh;
                else if (Path.GetFileName(path).StartsWith("DM_", StringComparison.Ordinal)) status = "skip:already DM_";
                else
                {
                    string dir = Path.GetDirectoryName(path).Replace('\\', '/');
                    string outDir = intoDMFolder ? dir + "/" + (string.IsNullOrWhiteSpace(dmFolderName) ? "DM_Materials" : dmFolderName.Trim()) : dir;
                    dst = outDir + "/DM_" + Path.GetFileName(path);
                    if (AssetDatabase.LoadMainAssetAtPath(dst) != null) status = "skip:copy exists";
                    else if (dryRun) { status = "planned"; planned++; }
                    else
                    {
                        if (intoDMFolder && !AssetDatabase.IsValidFolder(outDir))
                            AssetDatabase.CreateFolder(dir, Path.GetFileName(outDir));
                        if (AssetDatabase.CopyAsset(path, dst))
                        {
                            var copy = AssetDatabase.LoadAssetAtPath<Material>(dst);
                            copy.shader = dm;
                            Project.EditorTools.Rendering.DMMasterGUI.ApplyPreset(copy, presetOnCopy);
                            EditorUtility.SetDirty(copy);
                            status = "created"; made++;
                        }
                        else status = "error:copy failed";
                    }
                }
                if (status.StartsWith("skip")) skipped++;
                csv.Append(Csv(path)).Append(',').Append(Csv(dst)).Append(',').Append(Csv(sh)).Append(',').Append(Csv(status.StartsWith("skip") ? "" : ShaderName)).Append(',').Append(Csv(status)).Append('\n');
            }
            if (!dryRun) AssetDatabase.SaveAssets();
            Directory.CreateDirectory(ManifestFolder);
            string manifest = ManifestFolder + "/DM_Master_" + (dryRun ? "dryrun_" : "") + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
            File.WriteAllText(manifest, csv.ToString());
            AssetDatabase.ImportAsset(manifest);
            lastReport = (dryRun ? "DRY RUN  planned=" + planned : "created=" + made) + "  skipped=" + skipped + "\nManifest: " + manifest + "\n\n" + csv;
        }
    }
}
#endif

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// 2026-10-31c: texture streaming for textures used by the open scene. Importer-only changes (incl. third-party
    /// .meta, logged): streamingMipmaps on; textures with a source over 4096 capped to 2048 unless hero (player / terrain
    /// layers / UI). Every change logged to Logs/texstream31c.tsv; Restore puts importers + Quality back. Batched reimport.
    /// </summary>
    public static class DMStudioTexStream31c
    {
        public const string LogPath = "Logs/texstream31c.tsv";
        public const int Budget = 2560;

        private static HashSet<string> HeroPaths()
        {
            var hero = new HashSet<string>();
            var roots = new List<Object>();
            foreach (var g in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in g.GetComponentsInChildren<Transform>(true))
                    if (t.name.StartsWith("Player_Kade")) roots.Add(t.gameObject);
            foreach (var tr in Terrain.activeTerrains) if (tr.terrainData) foreach (var l in tr.terrainData.terrainLayers) if (l) roots.Add(l);
            foreach (var o in EditorUtility.CollectDependencies(roots.ToArray())) if (o is Texture) hero.Add(AssetDatabase.GetAssetPath(o));
            return hero;
        }

        public static List<string> SceneTextures()
        {
            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().Cast<Object>().ToArray();
            var set = new HashSet<string>();
            foreach (var o in EditorUtility.CollectDependencies(roots))
                if (o is Texture2D) { var p = AssetDatabase.GetAssetPath(o); if (p.StartsWith("Assets/")) set.Add(p); }
            return set.ToList();
        }

        private static long Est(TextureImporter ti, int max)
        {
            int w, h; ti.GetSourceTextureWidthAndHeight(out w, out h);
            float s = Mathf.Min(1f, (float)max / Mathf.Max(1, Mathf.Max(w, h)));
            long px = (long)(w * s) * (long)(h * s);
            return (long)(px * (ti.mipmapEnabled ? 1.333f : 1f)); // ~1 byte/px (BC7/DXT5); rough
        }

        public static string Stats()
        {
            long mb = 0; int n = 0, stream = 0, big = 0;
            foreach (var p in SceneTextures())
            {
                var ti = AssetImporter.GetAtPath(p) as TextureImporter; if (ti == null) continue;
                n++; if (ti.streamingMipmaps) stream++;
                int w, h; ti.GetSourceTextureWidthAndHeight(out w, out h);
                if (Mathf.Max(w, h) > 4096 && ti.maxTextureSize > 2048) big++;
                mb += Est(ti, ti.maxTextureSize);
            }
            return "sceneTextures=" + n + " streaming=" + stream + " over4Kuncapped=" + big + " estMB~" + (mb / (1024 * 1024)) +
                   " qualityStreaming=" + QualitySettings.streamingMipmapsActive + " budget=" + QualitySettings.streamingMipmapsMemoryBudget;
        }

        public static string Apply(bool dryRun)
        {
            var hero = HeroPaths();
            var sb = new StringBuilder(); int s = 0, c = 0;
            var changes = new List<TextureImporter>();
            foreach (var p in SceneTextures())
            {
                var ti = AssetImporter.GetAtPath(p) as TextureImporter; if (ti == null) continue;
                if (ti.textureType == TextureImporterType.Sprite || ti.textureType == TextureImporterType.GUI || ti.textureType == TextureImporterType.Cursor) continue;
                bool doS = ti.mipmapEnabled && !ti.streamingMipmaps;
                int w, h; ti.GetSourceTextureWidthAndHeight(out w, out h);
                bool doC = Mathf.Max(w, h) > 4096 && ti.maxTextureSize > 2048 && !hero.Contains(p);
                if (!doS && !doC) continue;
                sb.Append(p).Append('\t').Append(ti.streamingMipmaps ? 1 : 0).Append('\t').Append(ti.maxTextureSize).Append('\n');
                if (doS) s++; if (doC) c++;
                if (dryRun) continue;
                if (doS) ti.streamingMipmaps = true;
                if (doC) ti.maxTextureSize = 2048;
                changes.Add(ti);
            }
            if (dryRun) return "dry: stream+=" + s + " cap2048=" + c;
            string q = "#QUALITY\t" + (QualitySettings.streamingMipmapsActive ? 1 : 0) + "\t" + QualitySettings.streamingMipmapsMemoryBudget + "\n";
            File.WriteAllText(LogPath, q + sb.ToString());
            QualitySettings.streamingMipmapsActive = true;
            QualitySettings.streamingMipmapsMemoryBudget = Budget;
            AssetDatabase.StartAssetEditing();
            try { foreach (var ti in changes) ti.SaveAndReimport(); }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.SaveAssets();
            return "applied stream+=" + s + " cap2048=" + c + " budget=" + Budget + "MB log=" + LogPath;
        }

        public static string Restore()
        {
            if (!File.Exists(LogPath)) return "no log";
            int n = 0; var list = new List<TextureImporter>();
            foreach (var line in File.ReadAllLines(LogPath))
            {
                var f = line.Split('\t'); if (f.Length < 3) continue;
                if (f[0] == "#QUALITY") { QualitySettings.streamingMipmapsActive = f[1] == "1"; QualitySettings.streamingMipmapsMemoryBudget = float.Parse(f[2], System.Globalization.CultureInfo.InvariantCulture); continue; }
                var ti = AssetImporter.GetAtPath(f[0]) as TextureImporter; if (ti == null) continue;
                ti.streamingMipmaps = f[1] == "1"; ti.maxTextureSize = int.Parse(f[2]); list.Add(ti); n++;
            }
            AssetDatabase.StartAssetEditing();
            try { foreach (var ti in list) ti.SaveAndReimport(); }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.SaveAssets();
            File.Move(LogPath, LogPath + ".restored" + System.DateTime.Now.ToString("HHmmss"));
            return "restored " + n;
        }
    }
}
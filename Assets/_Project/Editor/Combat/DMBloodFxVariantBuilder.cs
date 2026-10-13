#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Project.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// Builds the coloured KriptoFX Volumetric Blood variants used for enemy hits, without touching vendor files:
    /// every top-level prefab in Assets/KriptoFX/VolumetricBloodFX/Prefabs gets a green _Android and a yellow _Mech prefab variant
    /// under Assets/_Project/Combat/VFX/BloodFX, each with its own copies of the fluid and decal materials.
    /// Colours come from DM_EnemyHitMarkProfile (androidFluidColor / androidDecalTint / mechFluidColor / mechDecalTint);
    /// "Recolor" re-applies them to the existing copies. The profile pools are filled (originals = flesh) and split left / right by name.
    /// </summary>
    public static class DMBloodFxVariantBuilder
    {
        public const string SourceFolder = "Assets/KriptoFX/VolumetricBloodFX/Prefabs";
        public const string TargetFolder = "Assets/_Project/Prefabs/Combat/VFX/BloodFX";
        private const string ProfilePath = "Assets/_Project/Resources/Combat/DM_EnemyHitMarkProfile.asset";

        [MenuItem("Tools/Dark Matter Genesis/Combat/Hit Marks/Blood FX - Build Android + Mech Variants")]
        public static void BuildMenu() { Debug.Log(Run(true)); }

        [MenuItem("Tools/Dark Matter Genesis/Combat/Hit Marks/Blood FX - Recolor Variants From Profile")]
        public static void RecolorMenu() { Debug.Log(Run(false)); }

        public static string Run(bool create)
        {
            var profile = AssetDatabase.LoadAssetAtPath<DM_EnemyHitMarkProfile>(ProfilePath);
            if (profile == null) return "[BloodFxVariants] profile missing at " + ProfilePath;
            var sb = new StringBuilder("[BloodFxVariants] ");
            var sources = AssetDatabase.FindAssets("t:Prefab", new[] { SourceFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => { string dir = Path.GetDirectoryName(p).Replace('\\', '/'); return dir == SourceFolder || dir == SourceFolder + "/SimplePlaneDecals"; })
                .Where(p => { string n = Path.GetFileNameWithoutExtension(p); return n.IndexOf("Adroid", System.StringComparison.OrdinalIgnoreCase) < 0 && n.IndexOf("Android", System.StringComparison.OrdinalIgnoreCase) < 0; })
                .OrderBy(p => p).ToList();
            int madePf = 0, madeMat = 0, recolored = 0;
            var flesh = new DMVolumetricBloodSet(); var android = new DMVolumetricBloodSet(); var mech = new DMVolumetricBloodSet();
            var pflesh = new DMVolumetricBloodSet(); var pandroid = new DMVolumetricBloodSet(); var pmech = new DMVolumetricBloodSet();
            foreach (string src in sources)
            {
                var srcGo = AssetDatabase.LoadAssetAtPath<GameObject>(src);
                bool plane = src.Contains("/SimplePlaneDecals/");
                string baseName = (plane ? "Plane_" : "") + Path.GetFileNameWithoutExtension(src);
                foreach (string suffix in new[] { "Flesh", "Android", "Mech" })
                {
                    string folder = TargetFolder + "/" + suffix; string matFolder = folder + "/Materials";
                    EnsureFolder(matFolder);
                    Color fluid = suffix == "Android" ? profile.androidFluidColor : profile.mechFluidColor;
                    Color tint = suffix == "Android" ? profile.androidDecalTint : profile.mechDecalTint;
                    var map = new Dictionary<Material, Material>();
                    foreach (Material m in SourceMaterials(srcGo))
                    {
                        if (map.ContainsKey(m)) continue;
                        string dst = matFolder + "/" + baseName + "_" + m.name + "_" + suffix + ".mat";
                        var copy = AssetDatabase.LoadAssetAtPath<Material>(dst);
                        if (copy == null)
                        {
                            if (!create) continue;
                            copy = new Material(m); AssetDatabase.CreateAsset(copy, dst); madeMat++;
                        }
                        if (suffix != "Flesh")
                        {
                            if (copy.HasProperty("_Color") && copy.shader != null && copy.shader.name.Contains("BFX_Blood")) { copy.SetColor("_Color", fluid); recolored++; }
                            if (copy.HasProperty("_TintColor")) { copy.SetColor("_TintColor", tint); recolored++; }
                        }
                        FixDecalMask(copy);
                        EditorUtility.SetDirty(copy);
                        map[m] = copy;
                    }
                    string vPath = folder + "/" + baseName + "_" + suffix + ".prefab";
                    var variant = AssetDatabase.LoadAssetAtPath<GameObject>(vPath);
                    if (variant == null && create)
                    {
                        var inst = (GameObject)PrefabUtility.InstantiatePrefab(srcGo);
                        foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                            r.sharedMaterials = r.sharedMaterials.Select(x => x != null && map.TryGetValue(x, out var y) ? y : x).ToArray();
                        foreach (var d in inst.GetComponentsInChildren<DecalProjector>(true))
                            if (d.material != null && map.TryGetValue(d.material, out var y)) d.material = y;
                        variant = PrefabUtility.SaveAsPrefabAsset(inst, vPath);
                        Object.DestroyImmediate(inst);
                        madePf++;
                    }
                    if (variant != null) AddToSet(plane ? (suffix == "Android" ? pandroid : suffix == "Mech" ? pmech : pflesh) : (suffix == "Android" ? android : suffix == "Mech" ? mech : flesh), baseName, variant);
                }
            }
            Undo.RecordObject(profile, "Blood FX pools");
            sb.Append("pools old flesh/android/mech=" + Count(profile.fleshVolumetric) + "/" + Count(profile.androidVolumetric) + "/" + Count(profile.mechVolumetric));
            profile.fleshVolumetric = flesh; profile.androidVolumetric = android; profile.mechVolumetric = mech;
            profile.fleshPlaneDecal = pflesh; profile.androidPlaneDecal = pandroid; profile.mechPlaneDecal = pmech;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            sb.Append(" new=" + Count(flesh) + "/" + Count(android) + "/" + Count(mech) + " plane=" + Count(pflesh) + "/" + Count(pandroid) + "/" + Count(pmech) + "; sources " + sources.Count + ", prefab variants made " + madePf + ", material copies made " + madeMat + ", colour sets " + recolored);
            return sb.ToString();
        }

        /// <summary>
        /// The SimplePlaneDecals (BFX_Decal_Plane) cut out cleanly because they are a transparent multiply pass
        /// (Blend DstColor SrcColor, ZWrite Off): outside the blood the result is neutral and nothing is written to smoothness / metal.
        /// The HDRP patch's BFX_NativeDecal graph writes constant Metallic 0.4 / Smoothness 0.8 into the mask buffer and its
        /// mask-map opacity is not limited to the blood pattern, so the whole projector box shows as a grey glossy rectangle.
        /// Copies keep albedo only (alpha from the blood mask / cutout); metal / smoothness / AO are switched off.
        /// </summary>
        private static void FixDecalMask(Material m)
        {
            if (m == null || m.shader == null || !m.shader.name.Contains("NativeDecal")) return;
            if (m.HasProperty("_AffectMetal")) m.SetFloat("_AffectMetal", 0f);
            if (m.HasProperty("_AffectSmoothness")) m.SetFloat("_AffectSmoothness", 0f);
            if (m.HasProperty("_AffectAO")) m.SetFloat("_AffectAO", 0f);
            m.DisableKeyword("_MATERIAL_AFFECTS_MASKMAP");
            HDMaterial.ValidateMaterial(m);
        }

        private static string Count(DMVolumetricBloodSet s) => s == null ? "0" : (s.any.Count + "+" + s.left.Count + "L+" + s.right.Count + "R");

        private static void AddToSet(DMVolumetricBloodSet set, string baseName, GameObject go)
        {
            if (baseName.IndexOf("Attached", System.StringComparison.OrdinalIgnoreCase) >= 0) return; // attach decal, not a hit burst
            if (baseName.IndexOf("_Left", System.StringComparison.OrdinalIgnoreCase) >= 0) set.left.Add(go);
            else if (baseName.IndexOf("_Right", System.StringComparison.OrdinalIgnoreCase) >= 0) set.right.Add(go);
            else set.any.Add(go);
        }

        private static IEnumerable<Material> SourceMaterials(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) foreach (var m in r.sharedMaterials) if (m != null) yield return m;
            foreach (var d in go.GetComponentsInChildren<DecalProjector>(true)) if (d.material != null) yield return d.material;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/'); string cur = parts[0];
            for (int i = 1; i < parts.Length; i++) { string next = cur + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]); cur = next; }
        }
    }
}
#endif

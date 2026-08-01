#if UNITY_EDITOR
using System.IO;
using Project.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Project.EditorTools
{
    /// <summary>
    /// Builds Laser_Burn_Mark soft alpha textures / transparent materials / prefab.
    /// </summary>
    public static class DMILaserBurnMarkPrefabBuilder
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Combat/VFX/Laser_Burn_Mark.prefab";
        private const string ArtFolder = "Assets/_Project/Art/Combat";
        private const string MatFolder = "Assets/_Project/Materials/Combat";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Combat/VFX";

        [MenuItem("Tools/Dark Matter Genesis/Combat/Build Laser Burn Mark Prefab")]
        public static void Build()
        {
            EnsureFolder("Assets/_Project/Art");
            EnsureFolder(ArtFolder);
            EnsureFolder("Assets/_Project/Materials");
            EnsureFolder(MatFolder);
            EnsureFolder("Assets/_Project/Prefabs/Combat");
            EnsureFolder(PrefabFolder);

            // Soft radial alpha discs. Dark scorch = widest; glow nests inside.
            // Alpha-blend layers use straight alpha (not premultiplied) so charcoal stays visible.
            WriteSoftRadialPng(
                ArtFolder + "/LaserBurn_Scorch.png",
                256,
                coreRadius: 0.22f,
                edgeRadius: 0.98f,
                new Color(0.12f, 0.09f, 0.07f, 1f),
                premultiplyRgb: false,
                falloffPow: 1.15f);
            WriteSoftRadialPng(
                ArtFolder + "/LaserBurn_Char.png",
                256,
                coreRadius: 0.1f,
                edgeRadius: 0.78f,
                new Color(0.28f, 0.11f, 0.03f, 1f),
                premultiplyRgb: false,
                falloffPow: 1.2f);
            WriteSoftRadialPng(
                ArtFolder + "/LaserBurn_Glow.png",
                256,
                coreRadius: 0.0f,
                edgeRadius: 0.48f,
                new Color(1f, 0.5f, 0.12f, 1f),
                premultiplyRgb: true,
                falloffPow: 1.45f);

            AssetDatabase.ImportAsset(ArtFolder + "/LaserBurn_Scorch.png");
            AssetDatabase.ImportAsset(ArtFolder + "/LaserBurn_Char.png");
            AssetDatabase.ImportAsset(ArtFolder + "/LaserBurn_Glow.png");

            ConfigureTexture(ArtFolder + "/LaserBurn_Scorch.png");
            ConfigureTexture(ArtFolder + "/LaserBurn_Char.png");
            ConfigureTexture(ArtFolder + "/LaserBurn_Glow.png");

            Texture2D scorchTex = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtFolder + "/LaserBurn_Scorch.png");
            Texture2D charTex = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtFolder + "/LaserBurn_Char.png");
            Texture2D glowTex = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtFolder + "/LaserBurn_Glow.png");

            Material scorchMat = CreateOrUpdateMaterial("LaserBurn_Scorch", scorchTex, additive: false);
            Material charMat = CreateOrUpdateMaterial("LaserBurn_Char", charTex, additive: false);
            Material glowMat = CreateOrUpdateMaterial("LaserBurn_Glow", glowTex, additive: true);

            GameObject root = new GameObject("Laser_Burn_Mark");
            // Critical: prior builds left a 0.3 root scale that shrunk marks into sparse glow dots.
            root.transform.localScale = Vector3.one;
            DMILaserBurnMark mark = root.AddComponent<DMILaserBurnMark>();

            // Size hierarchy: dark charcoal largest (slightly oval), mid char inside, glow smallest.
            // Stamp spacing (~0.018) + oval + ping-pong twist → continuous trail.
            GameObject scorch = CreateLayer(root.transform, "Scorch", scorchMat, new Vector3(1.05f, 0.78f, 1f), -0.001f);
            GameObject mid = CreateLayer(root.transform, "Char", charMat, new Vector3(0.62f, 0.48f, 1f), -0.002f);
            GameObject glow = CreateLayer(root.transform, "Glow", glowMat, new Vector3(0.30f, 0.24f, 1f), -0.003f);

            mark.BindLayers(
                scorch.GetComponent<Renderer>(),
                mid.GetComponent<Renderer>(),
                glow.GetComponent<Renderer>());

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

            EnsureFolder("Assets/_Project/Resources");
            EnsureFolder("Assets/_Project/Resources/Combat");
            EnsureFolder("Assets/_Project/Resources/Combat/VFX");
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Project/Resources/Combat/VFX/Laser_Burn_Mark.prefab");

            Object.DestroyImmediate(root);
            DMILaserBurnMarkSpawner.SetPrefabForEditor(
                AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            AssetDatabase.SaveAssets();
            Debug.Log($"[DMI] Built soft-alpha laser burn mark prefab at {PrefabPath} (root scale 1, oval scorch, dense trail ready)");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void WriteSoftRadialPng(
            string assetPath,
            int size,
            float coreRadius,
            float edgeRadius,
            Color centerRgb,
            bool premultiplyRgb,
            float falloffPow)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float cx = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - cx) / cx;
                    float dy = (y - cx) / cx;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);

                    float a;
                    if (r <= coreRadius)
                        a = 1f;
                    else if (r >= edgeRadius)
                        a = 0f;
                    else
                        a = 1f - Mathf.SmoothStep(0f, 1f, (r - coreRadius) / Mathf.Max(0.0001f, edgeRadius - coreRadius));

                    a = Mathf.Pow(Mathf.Clamp01(a), falloffPow);

                    Color c = centerRgb;
                    c.a = a;
                    if (premultiplyRgb)
                    {
                        c.r *= a;
                        c.g *= a;
                        c.b *= a;
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply(false, false);
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ConfigureTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.anisoLevel = 2;
            importer.SaveAndReimport();
        }

        private static Material CreateOrUpdateMaterial(string name, Texture2D tex, bool additive)
        {
            string path = MatFolder + "/" + name + ".mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            // Particles Unlit respects texture alpha cleanly for soft discs.
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.shader = shader;
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", tex);
            // White multiply base — runtime MPB drives tint. (Avoid leftover orange from prior mats.)
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", Color.white);
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", Color.black);

            // Transparent / additive surface.
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend"))
                mat.SetFloat("_Blend", additive ? 1f : 0f); // 0 Alpha, 1 Additive on URP Particles
            if (mat.HasProperty("_ColorMode"))
                mat.SetFloat("_ColorMode", 0f); // Multiply
            if (mat.HasProperty("_Cull"))
                mat.SetFloat("_Cull", 2f);
            if (mat.HasProperty("_ZWrite"))
                mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_AlphaClip"))
                mat.SetFloat("_AlphaClip", 0f);

            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            mat.SetInt("_SrcBlendAlpha", (int)BlendMode.One);
            mat.SetInt("_DstBlendAlpha", (int)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (additive)
            {
                mat.EnableKeyword("_BLENDMODE_ADD");
                mat.DisableKeyword("_BLENDMODE_ALPHABLEND");
            }
            else
            {
                mat.DisableKeyword("_BLENDMODE_ADD");
                mat.EnableKeyword("_BLENDMODE_ALPHABLEND");
            }

            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = additive ? 3100 : 3000;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject CreateLayer(
            Transform parent,
            string name,
            Material mat,
            Vector3 localScale,
            float zOffset)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            // Negative Z = out of surface when root looks along -normal (avoids terrain clip).
            go.transform.localPosition = new Vector3(0f, 0f, zOffset);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go;
        }
    }
}
#endif

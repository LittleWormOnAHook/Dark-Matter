#if UNITY_EDITOR
using System.IO;
using Project.Combat;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// Builds the bullet hit marks content (Enemy Spacing &amp; Hit Marks plan, Part B): project settings
    /// (layer 26 DMHitbox, HDRP Decal Layers), decal textures/materials, pooled FX prefabs, the
    /// DM_EnemyHitMarkProfile, enemy body types, the Robot/Humanoid test clones and the hit marks test scene.
    /// Every step is idempotent.
    /// </summary>
    public static class DMEnemyHitMarksBuilder
    {
        private const string Menu = DarkMatterGenesisEditorMenus.Combat + "Hit Marks/";

        public const string ArtFolder = "Assets/_Project/Art/Combat/HitMarks";
        public const string MatFolder = "Assets/_Project/Materials/Combat/HitMarks";
        public const string FxFolder = "Assets/_Project/Prefabs/Combat/VFX/HitMarks";
        public const string EnemyFolder = "Assets/_Project/Prefabs/Combat/Enemies";
        public const string ProfilePath = "Assets/_Project/Resources/Combat/DM_EnemyHitMarkProfile.asset";
        public const string FredPath = EnemyFolder + "/FRED.prefab";
        public const string CorruptAndroidPath = EnemyFolder + "/corrupt_patrol_android.prefab";
        public const string RobotPath = EnemyFolder + "/Robot.prefab";
        public const string HumanoidPath = EnemyFolder + "/Humanoid.prefab";
        public const string SandboxScenePath = "Assets/_Project/Scenes/Combat_Sandbox.unity";
        public const string TestScenePath = "Assets/_Project/Scenes/Combat/DM_HitMarks_Test.unity";
        public const string EnemyMarksRenderingLayerName = "DM Enemy Marks";
        public const int EnemyMarksRenderingLayerIndex = 15;

        private const string BloodSourceMat = "Assets/Synty/PolygonGeneric/Materials/FX/Generic_Circle_Multiply_01.mat";
        private const string PointSourceMat = "Assets/Hovl Studio/AAA Projectiles Vol 1/Materials/Point12cg.mat";
        private const string SmokeSourceMat = "Assets/Hovl Studio/AAA Projectiles Vol 1/Materials/Smoke3cg.mat";
        private const int AtlasSize = 1024;

        // ---------------------------------------------------------------- menu

        [MenuItem(Menu + "Build All (settings, assets, prefabs, test scene)", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Hit_Marks_Build_All_settings_assets_prefabs_test_scene)]
        public static string BuildAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return "Exit Play mode first.";

            string log = ApplyProjectSettings() + "\n" + BuildAssets() + "\n" + SetupEnemyPrefabs() + "\n" + CreateTestScene();
            Debug.Log("[DM Hit Marks] " + log);
            return log;
        }

        [MenuItem(Menu + "1. Apply Project Settings (layer 26, Decal Layers)", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Hit_Marks_1_Apply_Project_Settings_layer_26_Decal_Layers)]
        public static string ApplyProjectSettingsMenu() => LogResult(ApplyProjectSettings());

        [MenuItem(Menu + "2. Build Decal + FX Assets and Profile", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Hit_Marks_2_Build_Decal_FX_Assets_and_Profile)]
        public static string BuildAssetsMenu() => LogResult(BuildAssets());

        [MenuItem(Menu + "3. Set Enemy Body Types + Robot/Humanoid Test Clones", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Hit_Marks_3_Set_Enemy_Body_Types_Robot_Humanoid_Test_Clones)]
        public static string SetupEnemyPrefabsMenu() => LogResult(SetupEnemyPrefabs());

        [MenuItem(Menu + "4. Create Hit Marks Test Scene", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Hit_Marks_4_Create_Hit_Marks_Test_Scene)]
        public static string CreateTestSceneMenu() => LogResult(CreateTestScene());

        private static string LogResult(string s)
        {
            Debug.Log("[DM Hit Marks] " + s);
            return s;
        }

        // ---------------------------------------------------------------- 1. settings

        public static string ApplyProjectSettings()
        {
            var sb = new System.Text.StringBuilder();

            Object tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            var tags = new SerializedObject(tagManager);
            SerializedProperty layers = tags.FindProperty("layers");
            if (layers != null && layers.arraySize > DMEnemyHitQuery.DefaultHitboxLayer)
            {
                SerializedProperty slot = layers.GetArrayElementAtIndex(DMEnemyHitQuery.DefaultHitboxLayer);
                if (string.IsNullOrEmpty(slot.stringValue))
                {
                    slot.stringValue = DMEnemyHitQuery.HitboxLayerName;
                    sb.Append("layer 26 = DMHitbox; ");
                }
                else if (slot.stringValue != DMEnemyHitQuery.HitboxLayerName)
                {
                    sb.Append("WARNING layer 26 already used by '" + slot.stringValue + "'; ");
                }
                else
                {
                    sb.Append("layer 26 ok; ");
                }
            }

            SerializedProperty renderingLayers = tags.FindProperty("m_RenderingLayers");
            if (renderingLayers != null)
            {
                while (renderingLayers.arraySize <= EnemyMarksRenderingLayerIndex)
                {
                    renderingLayers.InsertArrayElementAtIndex(renderingLayers.arraySize);
                    renderingLayers.GetArrayElementAtIndex(renderingLayers.arraySize - 1).stringValue = string.Empty;
                }

                SerializedProperty rl = renderingLayers.GetArrayElementAtIndex(EnemyMarksRenderingLayerIndex);
                if (rl.stringValue != EnemyMarksRenderingLayerName)
                {
                    sb.Append("rendering layer 15 '" + rl.stringValue + "' -> '" + EnemyMarksRenderingLayerName + "'; ");
                    rl.stringValue = EnemyMarksRenderingLayerName;
                }
                else
                {
                    sb.Append("rendering layer 15 ok; ");
                }
            }

            tags.ApplyModifiedPropertiesWithoutUndo();

            int hitboxLayer = DMEnemyHitQuery.DefaultHitboxLayer;
            for (int i = 0; i < 32; i++)
                Physics.IgnoreLayerCollision(hitboxLayer, i, true);
            sb.Append("layer 26 collides with nothing; ");

            int qualityCount = QualitySettings.names.Length;
            for (int i = 0; i < qualityCount; i++)
            {
                RenderPipelineAsset asset = QualitySettings.GetRenderPipelineAssetAt(i);
                if (asset == null)
                    continue;

                var so = new SerializedObject(asset);
                SerializedProperty decals = so.FindProperty("m_RenderPipelineSettings.supportDecals");
                SerializedProperty decalLayers = so.FindProperty("m_RenderPipelineSettings.supportDecalLayers");
                bool changed = false;
                if (decals != null && !decals.boolValue) { decals.boolValue = true; changed = true; }
                if (decalLayers != null && !decalLayers.boolValue) { decalLayers.boolValue = true; changed = true; }
                if (changed)
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(asset);
                }

                sb.Append(asset.name + (changed ? " decal layers ON; " : " ok; "));
            }

            sb.Append(EnsureDefaultFrameSettingsDecalLayers());

            AssetDatabase.SaveAssets();
            return "Settings: " + sb;
        }

        /// <summary>Default camera Frame Settings bit 96 (DecalLayers) in the HDRP global settings.</summary>
        private static string EnsureDefaultFrameSettingsDecalLayers()
        {
            RenderPipelineGlobalSettings global = GraphicsSettings.GetSettingsForRenderPipeline<UnityEngine.Rendering.HighDefinition.HDRenderPipeline>();
            if (global == null)
                return "HDRP global settings not found; ";

            var so = new SerializedObject(global);
            SerializedProperty it = so.GetIterator();
            bool changed = false;
            int found = 0;
            while (it.Next(true))
            {
                if (it.propertyType != SerializedPropertyType.Integer)
                    continue;
                string path = it.propertyPath;
                if (!path.EndsWith("m_Camera.bitDatas.data2") && !path.EndsWith("m_RenderingPathDefaultCameraFrameSettings.bitDatas.data2"))
                    continue;

                found++;
                ulong value = it.ulongValue;
                ulong bit = 1UL << (96 - 64);
                if ((value & bit) == 0)
                {
                    it.ulongValue = value | bit;
                    changed = true;
                }
            }

            if (changed)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(global);
            }

            return found == 0
                ? "default frame settings bit not found (check HDRP Global Settings > Frame Settings > Decal Layers); "
                : (changed ? "default camera frame settings Decal Layers ON; " : "default camera frame settings Decal Layers already ON; ");
        }

        // ---------------------------------------------------------------- 2. assets

        public static string BuildAssets()
        {
            EnsureFolder(ArtFolder);
            EnsureFolder(MatFolder);
            EnsureFolder(FxFolder);
            EnsureFolder("Assets/_Project/Resources/Combat");

            string bulletAlbedo = ArtFolder + "/DM_HitMark_BulletBurn_Albedo.png";
            string glowAlbedo = ArtFolder + "/DM_HitMark_GlowBurn_Albedo.png";
            string glowEmissive = ArtFolder + "/DM_HitMark_GlowBurn_Emissive.png";
            string craterNormal = ArtFolder + "/DM_HitMark_Crater_Normal.png";

            WriteAtlas(bulletAlbedo, BulletBurnPixel);
            WriteAtlas(glowAlbedo, GlowBurnPixel);
            WriteAtlas(glowEmissive, GlowEmissivePixel);
            WriteNormalAtlas(craterNormal);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureTexture(bulletAlbedo, normal: false, srgb: true);
            ConfigureTexture(glowAlbedo, normal: false, srgb: true);
            ConfigureTexture(glowEmissive, normal: false, srgb: true);
            ConfigureTexture(craterNormal, normal: true, srgb: false);

            Texture2D texBullet = AssetDatabase.LoadAssetAtPath<Texture2D>(bulletAlbedo);
            Texture2D texGlow = AssetDatabase.LoadAssetAtPath<Texture2D>(glowAlbedo);
            Texture2D texEmissive = AssetDatabase.LoadAssetAtPath<Texture2D>(glowEmissive);
            Texture2D texNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(craterNormal);

            Material bulletBurn = CreateDecalMaterial(MatFolder + "/DM_Decal_BulletBurn.mat", texBullet, texNormal, smoothness: 0.42f, emissive: false, emissiveMap: null);
            Material glowBurn = CreateDecalMaterial(MatFolder + "/DM_Decal_GlowBurn.mat", texGlow, texNormal, smoothness: 0.12f, emissive: false, emissiveMap: null);
            Material glowHot = CreateDecalMaterial(MatFolder + "/DM_Decal_GlowBurn_Hot.mat", texEmissive, null, smoothness: 0f, emissive: true, emissiveMap: texEmissive);
            Material bulletHot = CreateDecalMaterial(MatFolder + "/DM_Decal_BulletBurn_Hot.mat", texBullet, null, smoothness: 0f, emissive: true, emissiveMap: texBullet);
            if (bulletHot.HasProperty("_EmissiveColorLDR"))
            {
                Color ldr = new Color(1f, 0.12f, 0.08f, 1f);
                const float intensity = 4.5f;
                bulletHot.SetColor("_EmissiveColorLDR", ldr);
                bulletHot.SetColor("_EmissiveColorHDR", ldr * intensity);
                bulletHot.SetColor("_EmissiveColor", ldr * intensity);
                bulletHot.SetFloat("_EmissiveIntensity", intensity);
            }

            Material bloodMat = CopyMaterial(BloodSourceMat, MatFolder + "/DM_FX_Blood_Droplet.mat", m =>
            {
                SetColorIf(m, "_BaseColor", new Color(0.62f, 0.015f, 0.015f, 1f));
                SetColorIf(m, "_Color", new Color(0.62f, 0.015f, 0.015f, 1f));
                SetFloatIf(m, "_Smoothness", 0.75f);
            });
            Material coolantMat = CopyMaterial(PointSourceMat, MatFolder + "/DM_FX_Coolant_Droplet.mat", m =>
            {
                SetColorIf(m, "_UnlitColor", new Color(0.25f, 1f, 0.42f, 1f));
                SetColorIf(m, "_Color", new Color(0.25f, 1f, 0.42f, 1f));
                SetColorIf(m, "_EmissiveColor", new Color(0.25f, 1f, 0.42f, 1f) * 1.5f);
            });
            Material sparkMat = CopyMaterial(PointSourceMat, MatFolder + "/DM_FX_Spark.mat", m =>
            {
                SetColorIf(m, "_UnlitColor", new Color(1f, 0.8f, 0.42f, 1f));
                SetColorIf(m, "_Color", new Color(1f, 0.8f, 0.42f, 1f));
                SetColorIf(m, "_EmissiveColor", new Color(1f, 0.62f, 0.22f, 1f) * 3f);
            });
            Material emberMat = CopyMaterial(PointSourceMat, MatFolder + "/DM_FX_Ember.mat", m =>
            {
                SetColorIf(m, "_UnlitColor", new Color(1f, 0.45f, 0.1f, 1f));
                SetColorIf(m, "_Color", new Color(1f, 0.45f, 0.1f, 1f));
                SetColorIf(m, "_EmissiveColor", new Color(1f, 0.42f, 0.08f, 1f) * 3.2f);
            });
            Material smokeMat = AssetDatabase.LoadAssetAtPath<Material>(SmokeSourceMat);

            GameObject blood = SaveFxPrefab(FxFolder + "/DM_FX_BloodHit_Small.prefab", BuildSplatter("DM_FX_BloodHit_Small", bloodMat, gravity: 1.1f, speed: new Vector2(1.4f, 3.6f), count: new Vector2(6, 12), size: new Vector2(0.012f, 0.032f), mistColorMat: bloodMat));
            GameObject coolant = SaveFxPrefab(FxFolder + "/DM_FX_CoolantHit_Small.prefab", BuildSplatter("DM_FX_CoolantHit_Small", coolantMat, gravity: 1.4f, speed: new Vector2(1.0f, 2.8f), count: new Vector2(6, 11), size: new Vector2(0.014f, 0.034f), mistColorMat: coolantMat));
            GameObject sparkSmall = SaveFxPrefab(FxFolder + "/DM_FX_SparkHit_Small.prefab", BuildSparks("DM_FX_SparkHit_Small", sparkMat, new Vector2(6, 10), new Vector2(2.5f, 5.5f), null));
            GameObject sparkLarge = SaveFxPrefab(FxFolder + "/DM_FX_SparkHit_Large.prefab", BuildSparks("DM_FX_SparkHit_Large", sparkMat, new Vector2(14, 22), new Vector2(3f, 7f), smokeMat));
            GameObject ember = SaveFxPrefab(FxFolder + "/DM_FX_BurnEmber_Small.prefab", BuildEmber("DM_FX_BurnEmber_Small", emberMat));

            DM_EnemyHitMarkProfile profile = AssetDatabase.LoadAssetAtPath<DM_EnemyHitMarkProfile>(ProfilePath);
            bool created = false;
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<DM_EnemyHitMarkProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
                created = true;
            }

            profile.bulletBurnMaterial = bulletBurn;
            profile.glowBurnMaterial = glowBurn;
            profile.glowBurnHotMaterial = glowHot;
            profile.bulletBurnHotMaterial = bulletHot;
            profile.bloodHitPrefab = blood;
            profile.coolantHitPrefab = coolant;
            profile.sparkHitSmallPrefab = sparkSmall;
            profile.sparkHitLargePrefab = sparkLarge;
            profile.burnEmberPrefab = ember;
            profile.enemyDecalRenderingLayer = EnemyMarksRenderingLayerName;
            profile.enemyDecalRenderingLayerFallbackIndex = EnemyMarksRenderingLayerIndex;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            return "Assets: 4 textures, 7 materials, 5 FX prefabs, profile " + (created ? "created" : "updated") + " at " + ProfilePath;
        }

        private delegate Color PixelFn(float x, float y, int variant);

        private static void WriteAtlas(string path, PixelFn fn)
        {
            int cell = AtlasSize / 2;
            var pixels = new Color32[AtlasSize * AtlasSize];
            for (int v = 0; v < 4; v++)
            {
                int ox = (v % 2) * cell;
                int oy = (v / 2) * cell;
                for (int y = 0; y < cell; y++)
                {
                    float fy = (y + 0.5f) / cell * 2f - 1f;
                    for (int x = 0; x < cell; x++)
                    {
                        float fx = (x + 0.5f) / cell * 2f - 1f;
                        pixels[(oy + y) * AtlasSize + ox + x] = fn(fx, fy, v);
                    }
                }
            }

            var tex = new Texture2D(AtlasSize, AtlasSize, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static float Noise(float x, float y, int variant, float freq)
        {
            float s = 17.31f * (variant + 1);
            return Mathf.PerlinNoise(x * freq + s, y * freq + s * 0.61f);
        }

        /// <summary>Polar radius with an irregular edge per variant (0 = centre, 1 = cell edge).</summary>
        private static float WarpedRadius(float x, float y, int variant, float amount)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float a = Mathf.Atan2(y, x);
            float wobble = (Mathf.PerlinNoise(Mathf.Cos(a) * 1.7f + variant * 3.1f, Mathf.Sin(a) * 1.7f + variant * 1.3f) - 0.5f) * 2f;
            return r * (1f + wobble * amount);
        }

        private static float Band(float r, float inner, float outer, float soft)
        {
            float a = Mathf.SmoothStep(0f, 1f, (r - (inner - soft)) / Mathf.Max(0.0001f, soft * 2f));
            float b = 1f - Mathf.SmoothStep(0f, 1f, (r - (outer - soft)) / Mathf.Max(0.0001f, soft * 2f));
            return Mathf.Clamp01(Mathf.Min(a, b));
        }

        /// <summary>D8 Humanoid: bright red blood spot inside a circular char ring, soot falloff, no glow.</summary>
        private static Color BulletBurnPixel(float x, float y, int v)
        {
            float rr = WarpedRadius(x, y, v, 0.12f);
            float n = Noise(x, y, v, 9f);
            float n2 = Noise(x, y, v + 7, 23f);

            float spotR = 0.2f + 0.02f * v;
            float spot = 1f - Mathf.SmoothStep(0f, 1f, (rr - (spotR - 0.035f)) / 0.07f);
            float ring = Band(rr, spotR - 0.01f, spotR + 0.17f, 0.04f);
            float soot = Mathf.Clamp01(1f - Mathf.SmoothStep(0f, 1f, (rr - 0.3f) / 0.38f));
            soot *= Mathf.Lerp(0.55f, 1f, n);

            Color blood = Color.Lerp(new Color(0.78f, 0.02f, 0.02f), new Color(0.5f, 0.0f, 0.01f), n2 * 0.6f + rr * 0.8f);
            Color charCol = Color.Lerp(new Color(0.05f, 0.03f, 0.02f), new Color(0.28f, 0.11f, 0.03f), Mathf.Clamp01((spotR + 0.04f - rr) / 0.06f) * 0.8f);
            Color sootCol = new Color(0.12f, 0.09f, 0.07f);

            Color c = sootCol;
            float a = soot * 0.75f;
            c = Color.Lerp(c, charCol, ring);
            a = Mathf.Max(a, ring * Mathf.Lerp(0.85f, 1f, n2));
            c = Color.Lerp(c, blood, spot);
            a = Mathf.Max(a, spot);
            c.a = Mathf.Clamp01(a) * (rr < 0.98f ? 1f : 0f);
            return c;
        }

        /// <summary>Android / Robot / Laser: the Laser burn look (scorch disc + char core) as one decal layer.</summary>
        private static Color GlowBurnPixel(float x, float y, int v)
        {
            float rr = WarpedRadius(x * (v % 2 == 0 ? 1f : 0.8f), y, v, 0.08f);
            float n = Noise(x, y, v, 8f);
            // Same radii / falloff as DMILaserBurnMarkPrefabBuilder: scorch core .22 edge .98 pow 1.15; char core .1 edge .78 pow 1.2.
            float scorch = Mathf.Pow(Disc(rr, 0.22f, 0.98f), 1.15f) * Mathf.Lerp(0.6f, 1f, n);
            float charA = Mathf.Pow(Disc(rr / 0.6f, 0.1f, 0.78f), 1.2f);
            Color scorchCol = new Color(0.22f, 0.16f, 0.12f) * 0.55f;
            Color charCol = Color.Lerp(new Color(0.04f, 0.025f, 0.02f), new Color(0.55f, 0.22f, 0.06f) * 0.6f, Mathf.Clamp01(rr / 0.5f));
            Color c = Color.Lerp(scorchCol, charCol, charA);
            c.a = Mathf.Clamp01(Mathf.Max(scorch * 0.92f, charA * 0.95f));
            return c;
        }

        /// <summary>Glow mask (premultiplied orange) for the emissive hot layer: Laser glow core .48 pow 1.45.</summary>
        private static Color GlowEmissivePixel(float x, float y, int v)
        {
            float rr = WarpedRadius(x * (v % 2 == 0 ? 1f : 0.8f), y, v, 0.08f) / 0.75f;
            float a = Mathf.Pow(Disc(rr, 0f, 0.48f), 1.45f);
            float n = Noise(x, y, v, 14f);
            a *= Mathf.Lerp(0.75f, 1f, n);
            Color g = new Color(1f, 0.5f, 0.12f) * a;
            g.a = a;
            return g;
        }

        private static float Disc(float r, float core, float edge)
        {
            if (r <= core)
                return 1f;
            if (r >= edge)
                return 0f;
            return 1f - Mathf.SmoothStep(0f, 1f, (r - core) / Mathf.Max(0.0001f, edge - core));
        }

        private static float CraterHeight(float x, float y, int v)
        {
            float rr = WarpedRadius(x, y, v, 0.1f);
            float pit = -Mathf.Pow(Disc(rr, 0.05f, 0.26f), 1.3f);
            float rim = Band(rr, 0.22f, 0.36f, 0.06f) * 0.45f;
            float fade = Disc(rr, 0.4f, 0.75f);
            float n = (Noise(x, y, v, 30f) - 0.5f) * 0.25f * fade;
            return pit + rim + n;
        }

        private static void WriteNormalAtlas(string path)
        {
            int cell = AtlasSize / 2;
            var pixels = new Color32[AtlasSize * AtlasSize];
            float step = 2f / cell;
            const float strength = 6f;
            for (int v = 0; v < 4; v++)
            {
                int ox = (v % 2) * cell;
                int oy = (v / 2) * cell;
                for (int y = 0; y < cell; y++)
                {
                    float fy = (y + 0.5f) / cell * 2f - 1f;
                    for (int x = 0; x < cell; x++)
                    {
                        float fx = (x + 0.5f) / cell * 2f - 1f;
                        float hl = CraterHeight(fx - step, fy, v);
                        float hr = CraterHeight(fx + step, fy, v);
                        float hd = CraterHeight(fx, fy - step, v);
                        float hu = CraterHeight(fx, fy + step, v);
                        Vector3 n = new Vector3((hl - hr) * strength, (hd - hu) * strength, 1f).normalized;
                        pixels[(oy + y) * AtlasSize + ox + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                    }
                }
            }

            var tex = new Texture2D(AtlasSize, AtlasSize, TextureFormat.RGBA32, false, true);
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void ConfigureTexture(string path, bool normal, bool srgb)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb;
            importer.alphaIsTransparency = !normal;
            importer.alphaSource = normal ? TextureImporterAlphaSource.None : TextureImporterAlphaSource.FromInput;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 4;
            importer.maxTextureSize = AtlasSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static Material CreateDecalMaterial(string path, Texture2D albedo, Texture2D normal, float smoothness, bool emissive, Texture2D emissiveMap)
        {
            Shader shader = Shader.Find("HDRP/Decal");
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }

            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BaseColorMap", albedo);
            mat.SetTexture("_NormalMap", normal);
            mat.SetTexture("_MaskMap", null);
            mat.SetFloat("_DecalBlend", 1f);
            mat.SetFloat("_NormalBlendSrc", 0f);
            mat.SetFloat("_MaskBlendSrc", 1f);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_AO", 1f);
            mat.SetFloat("_AffectAlbedo", emissive ? 0f : 1f);
            mat.SetFloat("_AffectNormal", !emissive && normal != null ? 1f : 0f);
            mat.SetFloat("_AffectAO", 0f);
            mat.SetFloat("_AffectMetal", 0f);
            mat.SetFloat("_AffectSmoothness", emissive ? 0f : 1f);
            mat.SetFloat("_AffectEmission", emissive ? 1f : 0f);
            mat.SetFloat("_DrawOrder", emissive ? 1f : 0f);
            if (emissive)
            {
                Color ldr = new Color(1f, 0.42f, 0.08f, 1f);
                const float intensity = 3.2f;
                mat.SetTexture("_EmissiveColorMap", emissiveMap);
                mat.SetFloat("_UseEmissiveIntensity", 0f);
                mat.SetFloat("_EmissiveIntensity", intensity);
                mat.SetColor("_EmissiveColorLDR", ldr);
                mat.SetColor("_EmissiveColorHDR", ldr * intensity);
                mat.SetColor("_EmissiveColor", ldr * intensity);
                // 0 = emission compensated for exposure, so the glow reads the same in any lighting (like the world Laser burn).
                mat.SetFloat("_EmissiveExposureWeight", 0f);
            }
            else
            {
                mat.SetTexture("_EmissiveColorMap", null);
                mat.SetColor("_EmissiveColor", Color.black);
                mat.SetColor("_EmissiveColorLDR", Color.black);
                mat.SetColor("_EmissiveColorHDR", Color.black);
            }

            mat.enableInstancing = true;
            try
            {
                HDShaderUtils.ResetMaterialKeywords(mat);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("DM Hit Marks: could not validate decal material " + path + ": " + e.Message);
            }

            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CopyMaterial(string sourcePath, string destPath, System.Action<Material> tweak)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(destPath);
            if (mat == null)
            {
                Material source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);
                if (source == null)
                {
                    mat = new Material(Shader.Find("HDRP/Unlit"));
                    SetFloatIf(mat, "_SurfaceType", 1f);
                }
                else
                {
                    mat = new Material(source);
                }

                AssetDatabase.CreateAsset(mat, destPath);
            }

            tweak?.Invoke(mat);
            try
            {
                HDShaderUtils.ResetMaterialKeywords(mat);
            }
            catch (System.Exception)
            {
                // Non-HDRP vendor shader: keep as is.
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void SetColorIf(Material m, string name, Color c)
        {
            if (m.HasProperty(name))
                m.SetColor(name, c);
        }

        private static void SetFloatIf(Material m, string name, float f)
        {
            if (m.HasProperty(name))
                m.SetFloat(name, f);
        }

        // ---------------------------------------------------------------- FX prefabs

        private static ParticleSystem AddSystem(GameObject go, Material mat, float duration)
        {
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.stopAction = ParticleSystemStopAction.None;
            main.maxParticles = 64;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.01f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        private static AnimationCurve ShrinkCurve(float holdUntil)
        {
            return new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(holdUntil, 0.9f), new Keyframe(1f, 0f));
        }

        private static void Shrink(ParticleSystem ps, float holdUntil)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, ShrinkCurve(holdUntil));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }

        private static GameObject BuildSplatter(string name, Material mat, float gravity, Vector2 speed, Vector2 count, Vector2 size, Material mistColorMat)
        {
            var root = new GameObject(name);
            ParticleSystem drops = AddSystem(root, mat, 0.35f);
            var main = drops.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.gravityModifier = gravity;
            var emission = drops.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count.x, (short)count.y) });
            var r = root.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.02f;
            r.lengthScale = 1.4f;
            Shrink(drops, 0.55f);

            var mistGo = new GameObject("Mist");
            mistGo.transform.SetParent(root.transform, false);
            ParticleSystem mist = AddSystem(mistGo, mistColorMat, 0.2f);
            var mm = mist.main;
            mm.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.3f);
            mm.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            mm.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
            mm.gravityModifier = 0.1f;
            var me = mist.emission;
            me.SetBursts(new[] { new ParticleSystem.Burst(0f, 2, 3) });
            var ms = mist.shape;
            ms.angle = 25f;
            var sol = mist.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.4f, 1f), new Keyframe(1f, 0f)));
            return root;
        }

        private static GameObject BuildSparks(string name, Material mat, Vector2 count, Vector2 speed, Material smokeMat)
        {
            var root = new GameObject(name);
            ParticleSystem sparks = AddSystem(root, mat, 0.2f);
            var main = sparks.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.018f);
            main.gravityModifier = 0.6f;
            var emission = sparks.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count.x, (short)count.y) });
            var shape = sparks.shape;
            shape.angle = 35f;
            var r = root.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.035f;
            r.lengthScale = 2f;
            Shrink(sparks, 0.4f);

            if (smokeMat != null)
            {
                var smokeGo = new GameObject("SmokeWisp");
                smokeGo.transform.SetParent(root.transform, false);
                ParticleSystem smoke = AddSystem(smokeGo, smokeMat, 0.1f);
                var sm = smoke.main;
                sm.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
                sm.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
                sm.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.12f);
                sm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.3f, 0.3f, 0.3f, 0.25f), new Color(0.2f, 0.2f, 0.2f, 0.18f));
                sm.gravityModifier = -0.05f;
                var se = smoke.emission;
                se.SetBursts(new[] { new ParticleSystem.Burst(0f, 1, 2) });
                var sol = smoke.sizeOverLifetime;
                sol.enabled = true;
                sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.6f)));
                var col = smoke.colorOverLifetime;
                col.enabled = true;
                var g = new Gradient();
                g.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
                col.color = g;
            }

            return root;
        }

        private static GameObject BuildEmber(string name, Material mat)
        {
            var root = new GameObject(name);
            ParticleSystem embers = AddSystem(root, mat, 1.2f);
            var main = embers.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.005f, 0.011f);
            main.gravityModifier = -0.05f;
            var emission = embers.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 8f), new Keyframe(1f, 0f)));
            var shape = embers.shape;
            shape.angle = 30f;
            shape.radius = 0.008f;
            Shrink(embers, 0.5f);

            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(root.transform, false);
            ParticleSystem glow = AddSystem(glowGo, mat, 0.1f);
            var gm = glow.main;
            gm.startLifetime = 1.5f;
            gm.startSpeed = 0f;
            gm.startSize = 0.03f;
            gm.simulationSpace = ParticleSystemSimulationSpace.Local;
            var ge = glow.emission;
            ge.SetBursts(new[] { new ParticleSystem.Burst(0f, 1, 1) });
            var gsh = glow.shape;
            gsh.enabled = false;
            var sol = glow.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.3f, 0.8f), new Keyframe(1f, 0f)));
            return root;
        }

        private static GameObject SaveFxPrefab(string path, GameObject temp)
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            return saved;
        }

        // ---------------------------------------------------------------- 3. prefabs

        public static string SetupEnemyPrefabs()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(SetBodyType(FredPath, DMEnemyBodyType.Android, null, null));
            sb.Append(SetBodyType(CorruptAndroidPath, DMEnemyBodyType.Android, null, null));

            if (AssetDatabase.LoadAssetAtPath<GameObject>(RobotPath) == null)
            {
                AssetDatabase.CopyAsset(FredPath, RobotPath);
                sb.Append("created Robot.prefab (FRED clone); ");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(HumanoidPath) == null)
            {
                AssetDatabase.CopyAsset(FredPath, HumanoidPath);
                sb.Append("created Humanoid.prefab (FRED clone); ");
            }

            Material robotBody = EnsureRobotBodyMaterial();
            sb.Append(SetBodyType(RobotPath, DMEnemyBodyType.Robot, "Robot", robotBody));
            sb.Append(SetBodyType(HumanoidPath, DMEnemyBodyType.Humanoid, "Humanoid", null));
            sb.Append(DMEnemyCombatColliderCleanup.StripPrefabs(new[]
            {
                FredPath,
                CorruptAndroidPath,
                RobotPath,
                HumanoidPath,
                EnemyFolder + "/The_Evil_One.prefab"
            }));
            AssetDatabase.SaveAssets();
            return "Prefabs: " + sb;
        }

        private static Material EnsureRobotBodyMaterial()
        {
            string dest = MatFolder + "/DM_Robot_Body.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(dest);
            if (mat != null)
                return mat;

            GameObject fred = AssetDatabase.LoadAssetAtPath<GameObject>(FredPath);
            Material body = null;
            if (fred != null)
            {
                foreach (SkinnedMeshRenderer r in fred.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    foreach (Material m in r.sharedMaterials)
                    {
                        if (m != null && m.name == "Body")
                        {
                            body = m;
                            break;
                        }
                    }

                    if (body != null)
                        break;
                }
            }

            mat = body != null ? new Material(body) : new Material(Shader.Find("HDRP/Lit"));
            // Industrial hazard-orange mech plating so the Robot reads apart from FRED (Android) at a glance.
            SetColorIf(mat, "_BaseColor", new Color(1f, 0.55f, 0.12f, 1f));
            SetColorIf(mat, "_Color", new Color(1f, 0.55f, 0.12f, 1f));
            AssetDatabase.CreateAsset(mat, dest);
            return mat;
        }

        private static string SetBodyType(string prefabPath, DMEnemyBodyType type, string rename, Material bodyMaterial)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                return "missing " + prefabPath + "; ";

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                DMEnemyHitMarks marks = root.GetComponent<DMEnemyHitMarks>();
                if (marks == null)
                    marks = root.AddComponent<DMEnemyHitMarks>();
                marks.BodyType = type;

                if (!string.IsNullOrEmpty(rename))
                    root.name = rename;

                int swapped = 0;
                if (bodyMaterial != null)
                {
                    foreach (SkinnedMeshRenderer r in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        Material[] mats = r.sharedMaterials;
                        bool changed = false;
                        for (int i = 0; i < mats.Length; i++)
                        {
                            if (mats[i] != null && (mats[i].name == "Body" || mats[i] == bodyMaterial))
                            {
                                if (mats[i] != bodyMaterial)
                                {
                                    mats[i] = bodyMaterial;
                                    changed = true;
                                    swapped++;
                                }
                            }
                        }

                        if (changed)
                            r.sharedMaterials = mats;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return System.IO.Path.GetFileName(prefabPath) + " = " + type + (swapped > 0 ? " (body recolor x" + swapped + ")" : string.Empty) + "; ";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------------------------------------------------------------- 4. test scene

        public static string CreateTestScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return "Exit Play mode first.";

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScenePath) == null)
            {
                if (!AssetDatabase.CopyAsset(SandboxScenePath, TestScenePath))
                    return "Could not copy " + SandboxScenePath;
            }

            Scene active = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Additive);
            try
            {
                foreach (GameObject go in scene.GetRootGameObjects())
                {
                    if (go.name.StartsWith("DM_HitMarks_Targets"))
                        Object.DestroyImmediate(go);
                }

                Transform spawn = null;
                foreach (GameObject go in scene.GetRootGameObjects())
                {
                    if (spawn == null && go.name == "EnemySpawnPoint")
                        spawn = go.transform;
                    if (go.GetComponent<DMCombatSandboxSpawner>() != null || go.GetComponentInChildren<DMCombatSandboxSpawner>(true) != null)
                        go.SetActive(false);
                }

                // Create inside the test scene so the open main scene is never touched.
                SceneManager.SetActiveScene(scene);
                var group = new GameObject("DM_HitMarks_Targets");
                if (active.IsValid())
                    SceneManager.SetActiveScene(active);
                Vector3 origin = spawn != null ? spawn.position : Vector3.zero;
                Quaternion facing = spawn != null ? spawn.rotation : Quaternion.identity;
                group.transform.SetPositionAndRotation(origin, facing);

                string[] paths = { FredPath, RobotPath, HumanoidPath };
                string[] names = { "FRED (Android)", "Robot", "Humanoid" };
                for (int i = 0; i < paths.Length; i++)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]);
                    if (prefab == null)
                        continue;

                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    instance.name = names[i];
                    instance.transform.SetParent(group.transform, false);
                    instance.transform.localPosition = new Vector3((i - 1) * 3.5f, 0f, 0f);
                    instance.transform.localRotation = Quaternion.identity;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (active.IsValid())
                    SceneManager.SetActiveScene(active);
            }

            return "Test scene: " + TestScenePath + " (copy of Combat_Sandbox, sandbox spawner off, FRED / Robot / Humanoid 3.5 m apart at EnemySpawnPoint)";
        }

        // ---------------------------------------------------------------- util

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Project.EditorTools.Rendering
{
    /// <summary>
    /// Material + terrain-layer inspector for DM/TerrainParallax. Layer panel mirrors HDRP TerrainLitGUI
    /// (Color Tint = diffuse remap, normal scale, mask channel remaps, tiling) and adds DM per-layer controls
    /// (parallax strength, extra tint, saturation, brightness) stored on the terrain's material.
    /// </summary>
    public class DMTerrainParallaxGUI : ShaderGUI, ITerrainLayerCustomUI
    {
        static readonly GUIContent kDiffuse = new GUIContent("Diffuse");
        static readonly GUIContent kTint = new GUIContent("Color Tint", "Terrain layer diffuse remap (per layer asset).");
        static readonly GUIContent kDensity = new GUIContent("Opacity as Density");
        static readonly GUIContent kNormal = new GUIContent("Normal Map");
        static readonly GUIContent kNormalScale = new GUIContent("Normal Scale");
        static readonly GUIContent kMask = new GUIContent("Mask", "R: Metallic\nG: AO\nB: Height (blend + parallax)\nA: Smoothness");
        static readonly GUIContent kMetal = new GUIContent("R: Metallic");
        static readonly GUIContent kAO = new GUIContent("G: AO");
        static readonly GUIContent kSmooth = new GUIContent("A: Smoothness");

        bool showRemap;
        bool showDM = true;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            EditorGUILayout.HelpBox("DM/TerrainParallax. Per-layer Color Tint, normal scale and mask remaps are edited in the Terrain inspector > Paint Texture > layer panel (stored on the Terrain Layer assets).", MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preset: Recommended")) ApplyPreset(materialEditor, false);
            if (GUILayout.Button("Preset: Extreme")) ApplyPreset(materialEditor, true);
            EditorGUILayout.EndHorizontal();
            materialEditor.PropertiesDefaultGUI(properties);
        }

        static void ApplyPreset(MaterialEditor editor, bool extreme)
        {
            foreach (var o in editor.targets)
            {
                var m = o as Material;
                if (m == null) continue;
                Undo.RecordObject(m, extreme ? "DM Terrain Extreme Preset" : "DM Terrain Recommended Preset");
                void F(string n, float v) { if (m.HasProperty(n)) m.SetFloat(n, v); }
                F("_DM_Parallax", 2); m.DisableKeyword("_DM_PARALLAX_OFF"); m.DisableKeyword("_DM_PARALLAX_OFFSET"); m.EnableKeyword("_DM_PARALLAX_POM");
                F("_DMSelfShadow", 1); m.EnableKeyword("_DM_SELFSHADOW");
                F("_DMParallaxHeight", extreme ? 0.25f : 0.06f);
                F("_DMOffsetBias", 0.42f);
                F("_DMGrazingCos", extreme ? 0.05f : 0.15f);
                F("_DMParallaxFadeStart", extreme ? 60 : 20); F("_DMParallaxFadeEnd", extreme ? 150 : 50);
                F("_DMSlopeFadeStart", extreme ? 0 : 0.6f); F("_DMSlopeFadeEnd", extreme ? 0 : 0.3f);
                F("_DMHeightMipBias", extreme ? 0 : 0.5f);
                F("_DMPOMSteps", extreme ? 48 : 16); F("_DMPOMMinSteps", extreme ? 16 : 4); F("_DMPOMRefine", extreme ? 6 : 3);
                F("_DMPOMLayers", extreme ? 2 : 1);
                F("_DMPOMFadeStart", extreme ? 40 : 10); F("_DMPOMFadeEnd", extreme ? 100 : 30);
                F("_DMShadowStrength", extreme ? 1.6f : 0.7f); F("_DMShadowSharpness", extreme ? 16 : 6);
                F("_DMShadowSteps", extreme ? 24 : 8); F("_DMShadowLength", extreme ? 2 : 1);
                F("_DMShadowFadeStart", extreme ? 40 : 15); F("_DMShadowFadeEnd", extreme ? 100 : 40);
                F("_DMHeightAOStrength", extreme ? 1.5f : 0.6f); F("_DMHeightAOMid", extreme ? 0.6f : 0.4f);
                // 2026-10-24s: POM depth offset so decals / projections land on the displaced surface.
                F("_DMDepthOffset", 1); m.EnableKeyword("_DEPTHOFFSET_ON");
                F("_DMDepthOffsetScale", extreme ? 1.25f : 1f); F("_DMDepthOffsetMax", extreme ? 1f : 0.3f);
                for (int k = 0; k < 8; k++)
                {
                    F("_DMPeakSoftness" + k, extreme ? 0.8f : 0.5f);
                    F("_DMPeakKnee" + k, extreme ? 0.7f : 0.8f);
                    F("_DMPeakDetail" + k, extreme ? 0.6f : 0.3f);
                    F("_DMPeakDetailScale" + k, extreme ? 8f : 4f);
                }
                EditorUtility.SetDirty(m);
            }
        }

        public override void ValidateMaterial(Material material)
        {
            try
            {
                Type api = Type.GetType("UnityEditor.Rendering.HighDefinition.TerrainLitAPI, Unity.RenderPipelines.HighDefinition.Editor");
                var m = api?.GetMethod("ValidateMaterial", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                m?.Invoke(null, new object[] { material });
                // 24x: HDRP deferred only draws decal receivers (queue OpaqueDecal = 2225) in the depth prepass; at 2000 the terrain got no DBuffer decals.
                bool receiveDecals = !material.HasProperty("_SupportDecals") || material.GetFloat("_SupportDecals") > 0.5f;
                material.renderQueue = receiveDecals ? 2225 : 2000;
                if (material.HasProperty("_DMDepthOffset")) // keep the 24s keyword in sync with its toggle
                {
                    if (material.GetFloat("_DMDepthOffset") > 0.5f) material.EnableKeyword("_DEPTHOFFSET_ON");
                    else material.DisableKeyword("_DEPTHOFFSET_ON");
                }
            }
            catch (Exception) { }
        }

        bool ITerrainLayerCustomUI.OnTerrainLayerGUI(TerrainLayer layer, Terrain terrain)
        {
            Material mat = terrain != null ? terrain.materialTemplate : null;
            bool heightBlend = mat != null && mat.HasProperty("_EnableHeightBlend") && mat.GetFloat("_EnableHeightBlend") > 0;

            layer.diffuseTexture = EditorGUILayout.ObjectField(kDiffuse, layer.diffuseTexture, typeof(Texture2D), false) as Texture2D;
            TerrainLayerUtility.ValidateDiffuseTextureUI(layer.diffuseTexture);

            Vector4 dMin = layer.diffuseRemapMin, dMax = layer.diffuseRemapMax;
            EditorGUI.BeginChangeCheck();
            EditorGUI.indentLevel++;
            Color tint = EditorGUILayout.ColorField(kTint, new Color(dMax.x, dMax.y, dMax.z), true, false, true);
            dMax.x = tint.r; dMax.y = tint.g; dMax.z = tint.b;
            DrawDMHeightMap(layer, terrain, mat);
            dMin.x = dMin.y = dMin.z = 0;
            bool density = dMin.w > 0;
            if (!heightBlend)
                density = EditorGUILayout.Toggle(kDensity, density);
            EditorGUI.indentLevel--;
            dMax.w = 1; dMin.w = density ? 1 : 0;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(layer, "Layer Color Tint");
                layer.diffuseRemapMin = dMin; layer.diffuseRemapMax = dMax;
            }

            layer.normalMapTexture = EditorGUILayout.ObjectField(kNormal, layer.normalMapTexture, typeof(Texture2D), false) as Texture2D;
            TerrainLayerUtility.ValidateNormalMapTextureUI(layer.normalMapTexture, TerrainLayerUtility.CheckNormalMapTextureType(layer.normalMapTexture));
            EditorGUI.indentLevel++;
            layer.normalScale = EditorGUILayout.Slider(kNormalScale, layer.normalScale, 0f, 8f);
            EditorGUI.indentLevel--;

            layer.maskMapTexture = EditorGUILayout.ObjectField(kMask, layer.maskMapTexture, typeof(Texture2D), false) as Texture2D;
            TerrainLayerUtility.ValidateMaskMapTextureUI(layer.maskMapTexture);

            Vector4 mMin = layer.maskMapRemapMin, mMax = layer.maskMapRemapMax;
            float smoothness = layer.smoothness, metallic = layer.metallic;
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            showRemap = EditorGUILayout.Foldout(showRemap, layer.maskMapTexture != null ? "Channel Remapping" : "Channel Default Values", true);
            if (showRemap)
            {
                if (layer.maskMapTexture != null)
                {
                    MinMax(kMetal, ref mMin.x, ref mMax.x);
                    MinMax(kAO, ref mMin.y, ref mMax.y);
                    float amp = Mathf.Max(mMax.z - mMin.z, Mathf.Epsilon);
                    float hBase = -mMin.z / amp;
                    amp = EditorGUILayout.FloatField("B: Height Amplitude (cm)", amp * 100f) / 100f;
                    hBase = EditorGUILayout.Slider("B: Height Base", hBase, 0f, 1f);
                    mMin.z = -hBase * amp; mMax.z = (1 - hBase) * amp;
                    MinMax(kSmooth, ref mMin.w, ref mMax.w);
                }
                else
                {
                    metallic = EditorGUILayout.Slider(kMetal, metallic, 0, 1);
                    mMax.y = EditorGUILayout.Slider(kAO, mMax.y, 0, 1);
                    if (heightBlend) mMax.z = EditorGUILayout.FloatField("B: Height (cm)", mMax.z * 100f) / 100f;
                    mMin.y = Mathf.Min(mMin.y, mMax.y);
                    mMin.z = Mathf.Min(Mathf.Max(0, mMin.z), mMax.z);
                    smoothness = EditorGUILayout.Slider(kSmooth, smoothness, 0, 1);
                }
            }
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(layer, "Layer Mask Remap");
                layer.maskMapRemapMin = mMin; layer.maskMapRemapMax = mMax;
                layer.smoothness = smoothness; layer.metallic = metallic;
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            TerrainLayerUtility.TilingSettingsUI(layer);

            DrawDMLayerExtras(layer, terrain, mat);
            return true;
        }

        void DrawDMLayerExtras(TerrainLayer layer, Terrain terrain, Material mat)
        {
            if (mat == null || terrain == null || terrain.terrainData == null || !mat.HasProperty("_DMLayerParallax0"))
                return;
            int idx = Array.IndexOf(terrain.terrainData.terrainLayers, layer);
            if (idx < 0 || idx > 7)
                return;

            EditorGUILayout.Space();
            showDM = EditorGUILayout.Foldout(showDM, "DM Layer " + idx + " (on material " + mat.name + ")", true);
            if (!showDM)
                return;
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            float par = EditorGUILayout.Slider("Parallax Strength", mat.GetFloat("_DMLayerParallax" + idx), 0f, 4f);
            Color xt = EditorGUILayout.ColorField(new GUIContent("Extra Tint"), mat.GetColor("_DMLayerTint" + idx), true, false, true);
            float sat = EditorGUILayout.Slider("Saturation", mat.GetFloat("_DMLayerSaturation" + idx), 0f, 3f);
            float bri = EditorGUILayout.Slider("Brightness", mat.GetFloat("_DMLayerBrightness" + idx), 0f, 4f);
            float hao = EditorGUILayout.Slider("Height AO Multiplier", mat.GetFloat("_DMLayerHeightAO" + idx), 0f, 4f);
            EditorGUILayout.LabelField("Peaks (clipped white tops)", EditorStyles.miniBoldLabel);
            float pks = EditorGUILayout.Slider("Peak Softness", mat.GetFloat("_DMPeakSoftness" + idx), 0f, 1f);
            float pkk = EditorGUILayout.Slider("Peak Knee", mat.GetFloat("_DMPeakKnee" + idx), 0.5f, 1f);
            float pkd = EditorGUILayout.Slider("Peak Detail", mat.GetFloat("_DMPeakDetail" + idx), 0f, 1f);
            float pkc = EditorGUILayout.Slider("Peak Detail Scale", mat.GetFloat("_DMPeakDetailScale" + idx), 0.1f, 20f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(mat, "DM Layer Settings");
                mat.SetFloat("_DMLayerParallax" + idx, par);
                mat.SetColor("_DMLayerTint" + idx, xt);
                mat.SetFloat("_DMLayerSaturation" + idx, sat);
                mat.SetFloat("_DMLayerBrightness" + idx, bri);
                mat.SetFloat("_DMLayerHeightAO" + idx, hao);
                mat.SetFloat("_DMPeakSoftness" + idx, pks);
                mat.SetFloat("_DMPeakKnee" + idx, pkk);
                mat.SetFloat("_DMPeakDetail" + idx, pkd);
                mat.SetFloat("_DMPeakDetailScale" + idx, pkc);
                EditorUtility.SetDirty(mat);
            }
            EditorGUI.indentLevel--;
        }

        static int LayerIndex(TerrainLayer layer, Terrain terrain, Material mat)
        {
            if (mat == null || terrain == null || terrain.terrainData == null || !mat.HasProperty("_DMHeightMap0"))
                return -1;
            int idx = Array.IndexOf(terrain.terrainData.terrainLayers, layer);
            return idx > 7 ? -1 : idx;
        }

        static void DrawDMHeightMap(TerrainLayer layer, Terrain terrain, Material mat)
        {
            int idx = LayerIndex(layer, terrain, mat);
            if (idx < 0)
                return;
            var cc = GUI.changed;
            EditorGUI.BeginChangeCheck();
            Texture hm = EditorGUILayout.ObjectField(new GUIContent("DM Height Map (R)", "Stored on material " + mat.name + " slot " + idx + ". Reordering layers needs a re-assign."), mat.GetTexture("_DMHeightMap" + idx), typeof(Texture2D), false) as Texture;
            bool use = EditorGUILayout.Toggle("Use Height Map (else Mask B)", mat.GetFloat("_DMUseHeightMap" + idx) > 0.5f);
            float hs = EditorGUILayout.Slider("Height Scale", mat.GetFloat("_DMHeightScale" + idx), 0f, 4f);
            float ho = EditorGUILayout.Slider("Height Offset", mat.GetFloat("_DMHeightOffset" + idx), -1f, 1f);
            float hc = EditorGUILayout.Slider("Height Contrast", mat.GetFloat("_DMHeightContrast" + idx), 0f, 8f);
            bool blend = EditorGUILayout.Toggle("Height Blend Uses DM Maps (all layers)", mat.GetFloat("_DMHeightMapForBlend") > 0.5f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(mat, "DM Layer Height");
                mat.SetTexture("_DMHeightMap" + idx, hm);
                mat.SetFloat("_DMUseHeightMap" + idx, use ? 1f : 0f);
                mat.SetFloat("_DMHeightScale" + idx, hs);
                mat.SetFloat("_DMHeightOffset" + idx, ho);
                mat.SetFloat("_DMHeightContrast" + idx, hc);
                mat.SetFloat("_DMHeightMapForBlend", blend ? 1f : 0f);
                EditorUtility.SetDirty(mat);
                GUI.changed = cc; // keep the tint change check unaffected
            }
        }

        static void MinMax(GUIContent label, ref float min, ref float max)
        {
            EditorGUILayout.MinMaxSlider(label, ref min, ref max, 0f, 1f);
        }
    }
}
#endif

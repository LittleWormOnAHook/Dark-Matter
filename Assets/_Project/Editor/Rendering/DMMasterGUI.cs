#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Rendering
{
    /// <summary>Inspector for DM/Master: HDRP LitGUI (via reflection, keeps keyword validation) + DM extras section.</summary>
    public class DMMasterGUI : ShaderGUI
    {
        ShaderGUI lit;
        bool showDM = true;

        ShaderGUI Lit
        {
            get
            {
                if (lit == null)
                {
                    Type t = Type.GetType("UnityEditor.Rendering.HighDefinition.LitGUI, Unity.RenderPipelines.HighDefinition.Editor");
                    if (t != null)
                        lit = Activator.CreateInstance(t, true) as ShaderGUI;
                }
                return lit;
            }
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (Lit != null) Lit.OnGUI(materialEditor, properties);
            else materialEditor.PropertiesDefaultGUI(properties);
            EditorGUILayout.Space();
            DrawDMExtras(materialEditor, properties);
        }

        public override void ValidateMaterial(Material material)
        {
            Lit?.ValidateMaterial(material);
            SetDMKeywords(material);
        }

        /// <summary>Keyword gating: no DM texture cost unless a texture is assigned.</summary>
        public static void SetDMKeywords(Material m)
        {
            if (m == null) return;
            bool dmH = m.HasProperty("_DMHeightMap") && m.GetTexture("_DMHeightMap") != null
                       && !(m.HasProperty("_DMUseLitHeight") && m.GetFloat("_DMUseLitHeight") > 0.5f);
            bool dmAO = m.HasProperty("_DMAOMap") && m.GetTexture("_DMAOMap") != null;
            if (dmH) m.EnableKeyword("_DM_MASTER_HEIGHTMAP"); else m.DisableKeyword("_DM_MASTER_HEIGHTMAP");
            if (dmAO) m.EnableKeyword("_DM_MASTER_AOMAP"); else m.DisableKeyword("_DM_MASTER_AOMAP");
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (Lit != null) Lit.AssignNewShaderToMaterial(material, oldShader, newShader);
            else base.AssignNewShaderToMaterial(material, oldShader, newShader);
            SetDMKeywords(material);
        }

        void DrawDMExtras(MaterialEditor me, MaterialProperty[] props)
        {
            showDM = EditorGUILayout.BeginFoldoutHeaderGroup(showDM, "DM Master Extras");
            if (showDM)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Preset: Neutral")) ApplyPreset(me, 0);
                if (GUILayout.Button("Preset: Recommended")) ApplyPreset(me, 1);
                if (GUILayout.Button("Preset: Extreme")) ApplyPreset(me, 2);
                EditorGUILayout.EndHorizontal();
                P(me, props, "_DMColor", "Color Adjust");
                EditorGUI.indentLevel++;
                P(me, props, "_DMTint", "Tint"); P(me, props, "_DMSaturation", "Saturation"); P(me, props, "_DMBrightness", "Brightness");
                EditorGUI.indentLevel--;
                EditorGUILayout.LabelField("DM Height Map (R)", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                var hm = FindProperty("_DMHeightMap", props, false);
                if (hm != null) { me.TexturePropertySingleLine(new GUIContent("Height Map (R)"), hm); me.TextureScaleOffsetProperty(hm); }
                Material m0 = me.target as Material;
                bool litHas = m0 != null && m0.HasProperty("_HeightMap") && m0.GetTexture("_HeightMap") != null;
                if (litHas) P(me, props, "_DMUseLitHeight", "Use Lit Height Map instead");
                bool dmHas = hm != null && hm.textureValue != null;
                if (!dmHas) EditorGUILayout.HelpBox(litHas ? "DM height empty: using Lit _HeightMap." : "No height map: parallax/height AO will be flat.", MessageType.None);
                P(me, props, "_DMHeightScale", "Scale"); P(me, props, "_DMHeightOffset", "Offset"); P(me, props, "_DMHeightContrast", "Contrast");
                P(me, props, "_DMHeightPeakKnee", "Peak Knee (1 = off)"); P(me, props, "_DMHeightPeakSoft", "Peak Softness");
                if (dmHas && GUILayout.Button("Drive Lit POM: copy DM height into Lit Height Map + Pixel Displacement"))
                    foreach (var o in me.targets) DrivePOM(o as Material);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("DM AO Map", EditorStyles.boldLabel);
                var ao = FindProperty("_DMAOMap", props, false);
                if (ao != null) { me.TexturePropertySingleLine(new GUIContent("AO Map"), ao); me.TextureScaleOffsetProperty(ao); }
                P(me, props, "_DMAOChannel", "Channel"); P(me, props, "_DMAOStrength", "Strength"); P(me, props, "_DMAOReplace", "Replace Mask Map AO (else multiply)");
                if (EditorGUI.EndChangeCheck()) foreach (var o in me.targets) SetDMKeywords(o as Material);
                EditorGUILayout.Space();
                P(me, props, "_DMHeightAO", "Height AO");
                EditorGUI.indentLevel++;
                P(me, props, "_DMHeightAOStrength", "Strength"); P(me, props, "_DMHeightAOMid", "Crevice Height");
                EditorGUI.indentLevel--;
                P(me, props, "_DMParallax", "Offset Parallax (needs Height Map, not Pixel Displacement)");
                EditorGUI.indentLevel++;
                P(me, props, "_DMParallaxAmount", "Amount (UV)"); P(me, props, "_DMParallaxCenter", "Height Center");
                P(me, props, "_DMParallaxFadeStart", "Fade Start (m)"); P(me, props, "_DMParallaxFadeEnd", "Fade End (m)");
                EditorGUI.indentLevel--;
                EditorGUILayout.HelpBox("POM: use HDRP Displacement Mode = Pixel Displacement (Lit's own POM). Triplanar: Base UV Mapping = Triplanar (off by default).", MessageType.None);
                // 2026-10-24s: depth offset so decals / projections land on the POM surface (Pixel Displacement only).
                Material dm = me.target as Material;
                if (dm != null && dm.HasProperty("_DepthOffsetEnable"))
                {
                    bool pix = dm.HasProperty("_DisplacementMode") && Mathf.RoundToInt(dm.GetFloat("_DisplacementMode")) == 2;
                    EditorGUI.BeginChangeCheck();
                    bool dOn = EditorGUILayout.Toggle(new GUIContent("POM Depth Offset (decals land on POM)", "Writes the POM depth (SV_Depth) so decals, blood and projections follow the displaced surface. Needs Pixel Displacement."), dm.GetFloat("_DepthOffsetEnable") > 0.5f);
                    if (EditorGUI.EndChangeCheck()) foreach (var o in me.targets) SetDepthOffset(o as Material, dOn);
                    if (dOn && !pix) EditorGUILayout.HelpBox("Depth Offset only works with Displacement Mode = Pixel Displacement.", MessageType.None);
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        public static void SetDepthOffset(Material m, bool on)
        {
            if (m == null || !m.HasProperty("_DepthOffsetEnable")) return;
            Undo.RecordObject(m, "DM Depth Offset");
            m.SetFloat("_DepthOffsetEnable", on ? 1f : 0f);
            bool pix = m.HasProperty("_DisplacementMode") && Mathf.RoundToInt(m.GetFloat("_DisplacementMode")) == 2;
            if (on && pix) m.EnableKeyword("_DEPTHOFFSET_ON"); else m.DisableKeyword("_DEPTHOFFSET_ON");
            EditorUtility.SetDirty(m);
        }

        static void DrivePOM(Material m)
        {
            if (m == null || m.GetTexture("_DMHeightMap") == null) return;
            Undo.RecordObject(m, "DM Drive POM");
            m.SetTexture("_HeightMap", m.GetTexture("_DMHeightMap"));
            m.SetTextureScale("_HeightMap", m.GetTextureScale("_DMHeightMap"));
            m.SetTextureOffset("_HeightMap", m.GetTextureOffset("_DMHeightMap"));
            if (m.HasProperty("_DisplacementMode")) m.SetFloat("_DisplacementMode", 2f); // Pixel
            SetDepthOffset(m, true); // 24s: decals land on POM
            EditorUtility.SetDirty(m);
        }

        static void P(MaterialEditor me, MaterialProperty[] props, string name, string label)
        {
            MaterialProperty p = FindProperty(name, props, false);
            if (p != null) me.ShaderProperty(p, label);
        }

        public static void ApplyPreset(MaterialEditor me, int level)
        {
            foreach (var o in me.targets) ApplyPreset(o as Material, level);
        }

        public static void ApplyPreset(Material m, int level)
        {
            if (m == null) return;
            Undo.RecordObject(m, "DM Master Preset");
            void F(string n, float v) { if (m.HasProperty(n)) m.SetFloat(n, v); }
            void K(string prop, string kw, bool on) { F(prop, on ? 1 : 0); if (on) m.EnableKeyword(kw); else m.DisableKeyword(kw); }
            bool on = level > 0, ex = level == 2;
            K("_DMColor", "_DM_MASTER_COLOR", ex);
            if (m.HasProperty("_DMTint")) m.SetColor("_DMTint", Color.white);
            F("_DMSaturation", ex ? 1.4f : 1f); F("_DMBrightness", 1f);
            K("_DMHeightAO", "_DM_MASTER_HEIGHTAO", on);
            F("_DMHeightAOStrength", ex ? 1.5f : 0.5f); F("_DMHeightAOMid", ex ? 0.6f : 0.4f);
            K("_DMParallax", "_DM_MASTER_PARALLAX", on);
            F("_DMParallaxAmount", ex ? 0.08f : 0.02f); F("_DMParallaxCenter", 0.5f);
            F("_DMParallaxFadeStart", ex ? 60 : 20); F("_DMParallaxFadeEnd", ex ? 150 : 50);
            F("_DMHeightScale", 1f); F("_DMHeightOffset", 0f); F("_DMHeightContrast", ex ? 2.5f : (on ? 1.2f : 1f));
            F("_DMHeightPeakKnee", ex ? 0.7f : (on ? 0.85f : 1f)); F("_DMHeightPeakSoft", ex ? 1f : (on ? 0.5f : 0f));
            F("_DMAOStrength", ex ? 2f : 1f); F("_DMAOReplace", 0f);
            SetDepthOffset(m, on); // 24s: Recommended/Extreme -> POM depth offset on (effective with Pixel Displacement)
            SetDMKeywords(m);
            EditorUtility.SetDirty(m);
        }
    }
}
#endif

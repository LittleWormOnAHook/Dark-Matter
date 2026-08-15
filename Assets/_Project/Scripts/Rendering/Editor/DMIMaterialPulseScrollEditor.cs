using UnityEditor;
using UnityEngine;
using Project.Rendering;

namespace Project.Rendering.Editor
{
    [CustomEditor(typeof(DMIMaterialPulseScroll))]
    public class DMIMaterialPulseScrollEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Supported shaders:\n" +
                "• HDRP Lit / Unlit — _BaseColor / _UnlitColor, _EmissiveColor (+ _EMISSIVE_COLOR),\n" +
                "  _BaseColorMap_ST / _EmissiveColorMap_ST / _NormalMap_ST\n" +
                "• URP Lit / Unlit — _BaseColor, _EmissionColor (+ _EMISSION), _BaseMap_ST / _EmissionMap_ST\n" +
                "• glTF-pbrMetallicRoughness (glTFast) — baseColorFactor, emissiveFactor (+ _EMISSIVE),\n" +
                "  baseColorTexture_ST / emissiveTexture_ST / normalTexture_ST\n\n" +
                "Emission Min/Max:\n" +
                "• Authored emission has color → multipliers on that HDR color\n" +
                "• Authored emission near-black → absolute HDR intensity × Fallback Tint\n" +
                "• No emission property → falls back to boosting base color HDR\n" +
                "On HDRP Lit, _EmissiveColor is preferred over legacy _EmissionColor.\n" +
                "Pulse Emission auto-enables the shader emission keyword when needed.\n\n" +
                "Prefer MaterialPropertyBlock (default) so shared materials are not mutated.\n" +
                "Preview In Edit Mode shows the pulse without Play (uses MPB only).\n" +
                "Do not combine emission pulse on the same slots as DMICreatureEmissionDriver — disable one.",
                MessageType.Info);

            DrawDefaultInspector();

            var pulse = (DMIMaterialPulseScroll)target;
            var so = serializedObject;
            var rendererProp = so.FindProperty("targetRenderer");
            var renderer = rendererProp.objectReferenceValue as Renderer;
            if (renderer == null)
                renderer = pulse.GetComponent<Renderer>();

            if (renderer != null && renderer.gameObject != pulse.gameObject
                && !renderer.transform.IsChildOf(pulse.transform)
                && !pulse.transform.IsChildOf(renderer.transform))
            {
                EditorGUILayout.HelpBox(
                    "Target Renderer is on a different object ('" + renderer.name + "'). " +
                    "Brimmy PetVisual must target its own MeshRenderer — not another mesh (e.g. resource node).",
                    MessageType.Warning);
                if (GUILayout.Button("Retarget to this GameObject's Renderer"))
                {
                    var local = pulse.GetComponent<Renderer>();
                    if (local != null)
                    {
                        rendererProp.objectReferenceValue = local;
                        so.ApplyModifiedProperties();
                        pulse.RebuildCaches();
                    }
                }
            }

            if (renderer != null && renderer.sharedMaterial != null)
            {
                Material mat = renderer.sharedMaterial;
                bool hasEm = mat.HasProperty("_EmissiveColor")
                             || mat.HasProperty("_EmissionColor")
                             || mat.HasProperty("emissiveFactor");
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Bind Preview", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Target", renderer.name + " (" + renderer.GetType().Name + ")");
                EditorGUILayout.LabelField("Shader", mat.shader != null ? mat.shader.name : "(null)");
                EditorGUILayout.LabelField("Material", mat.name);
                EditorGUILayout.LabelField("Has emission prop", hasEm ? "Yes" : "No (base-color fallback)");
                bool hdrp = mat.shader != null && mat.shader.name.StartsWith("HDRP/", System.StringComparison.Ordinal);
                if (hdrp && mat.HasProperty("_EmissiveColor"))
                    EditorGUILayout.LabelField("_EmissiveColor", mat.GetColor("_EmissiveColor").ToString());
                else if (mat.HasProperty("_EmissionColor"))
                    EditorGUILayout.LabelField("_EmissionColor", mat.GetColor("_EmissionColor").ToString());
                else if (mat.HasProperty("emissiveFactor"))
                    EditorGUILayout.LabelField("emissiveFactor", mat.GetColor("emissiveFactor").ToString());
                if (mat.HasProperty("_BaseColorMap_ST"))
                    EditorGUILayout.LabelField("UV ST", "_BaseColorMap_ST (HDRP)");
                else if (mat.HasProperty("_BaseMap_ST"))
                    EditorGUILayout.LabelField("UV ST", "_BaseMap_ST (URP)");
                if (!Application.isPlaying)
                    EditorGUILayout.HelpBox(
                        "Pulse drives materials at runtime and in Edit Mode when Preview is on. " +
                        "If you just recompiled scripts, Enter Play once or toggle the component to refresh caches.",
                        MessageType.None);
            }
        }
    }
}

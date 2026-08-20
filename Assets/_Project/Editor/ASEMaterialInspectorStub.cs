#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Stub ShaderGUI for third-party Amplify Shader Editor materials (QFX pack) when ASE is not installed.
/// Prevents "Could not create a custom UI for the shader ... ASEMaterialInspector" console spam.
/// </summary>
public class ASEMaterialInspector : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        materialEditor.PropertiesDefaultGUI(properties);
    }
}
#endif

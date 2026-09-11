#if UNITY_EDITOR
using UnityEditor;

namespace Project.EditorTools
{
    internal static class DMTextureImporterEditorUtility
    {
        internal static bool GetSpriteGenerateFallbackPhysicsShape(TextureImporter importer)
        {
            if (importer == null)
                return false;

            SerializedObject so = new SerializedObject(importer);
            SerializedProperty prop = so.FindProperty("m_SpriteGenerateFallbackPhysicsShape");
            return prop != null && prop.intValue != 0;
        }

        internal static void SetSpriteGenerateFallbackPhysicsShape(TextureImporter importer, bool enabled)
        {
            if (importer == null)
                return;

            SerializedObject so = new SerializedObject(importer);
            SerializedProperty prop = so.FindProperty("m_SpriteGenerateFallbackPhysicsShape");
            if (prop == null)
                return;

            prop.intValue = enabled ? 1 : 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif

using TMPro;
using UnityEngine;

namespace Project.UI
{
    internal static class TmpUiHelper
    {
        private const string FallbackFontResourcePath = "Fonts & Materials/LiberationSans SDF - Fallback";
#if UNITY_EDITOR
        private const string FallbackFontAssetPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset";
#endif

        private static TMP_FontAsset cachedFallbackFont;

        public static TMP_FontAsset FallbackFont => GetFallbackFont();

        public static void ApplyDefaultFont(TMP_Text label)
        {
            if (label == null)
                return;

            TMP_FontAsset font = ShiftUiTheme.RegularFont ?? GetFallbackFont();
            if (font == null)
                return;

            if (label.font != font)
                label.font = font;

            label.ForceMeshUpdate();
        }

        public static void ApplyDefaultFont(TextMeshProUGUI label) => ApplyDefaultFont((TMP_Text)label);

        public static void ApplyDefaultFont(TextMeshPro label) => ApplyDefaultFont((TMP_Text)label);

        public static void ApplyDefaultFont(TMP_InputField inputField)
        {
            if (inputField == null)
                return;

            ApplyDefaultFont(inputField.textComponent);
            ApplyDefaultFont(inputField.placeholder as TMP_Text);
        }

        public static void ApplyToAllLoadedObjects()
        {
            TextMeshProUGUI[] uiLabels = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);

            for (int i = 0; i < uiLabels.Length; i++)
                ApplyDefaultFont(uiLabels[i]);

            TextMeshPro[] worldLabels = Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include);

            for (int i = 0; i < worldLabels.Length; i++)
                ApplyDefaultFont(worldLabels[i]);

            TMP_InputField[] inputFields = Object.FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include);

            for (int i = 0; i < inputFields.Length; i++)
                ApplyDefaultFont(inputFields[i]);
        }

        private static TMP_FontAsset GetFallbackFont()
        {
            if (cachedFallbackFont != null)
                return cachedFallbackFont;

            cachedFallbackFont = Resources.Load<TMP_FontAsset>(FallbackFontResourcePath);
#if UNITY_EDITOR
            if (cachedFallbackFont == null)
            {
                cachedFallbackFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontAssetPath);
            }
#endif

            return cachedFallbackFont;
        }

        public static void TryApplyOutline(TextMeshProUGUI label, float width, Color color)
        {
            ApplyDefaultFont(label);

            if (label == null || label.font == null)
                return;

            try
            {
                label.outlineWidth = width;
                label.outlineColor = color;
            }
            catch (System.Exception)
            {
                // TMP material may not be ready when slots wake from an inactive inventory panel.
            }
        }
    }
}

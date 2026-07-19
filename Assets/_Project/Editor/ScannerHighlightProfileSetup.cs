#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class ScannerHighlightProfileSetup
    {
        private const string AssetPath = "Assets/_Project/Resources/Scanner/ScannerHighlightProfile.asset";

        [MenuItem("Survival Pioneer/Scanner/Create Default Highlight Profile")]
        public static void CreateDefaultProfile()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/Scanner"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Resources");
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "Scanner");
            }

            var existing = AssetDatabase.LoadAssetAtPath<Project.Interaction.ScannerHighlightProfile>(AssetPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            Project.Interaction.ScannerHighlightProfile profile =
                Project.Interaction.ScannerHighlightProfile.CreateDefaultInstance();
            AssetDatabase.CreateAsset(profile, AssetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = profile;
            EditorGUIUtility.PingObject(profile);
        }
    }
}
#endif

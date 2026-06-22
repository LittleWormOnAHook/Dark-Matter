#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Project.Audio.Editor
{
    [InitializeOnLoad]
    internal static class GameAudioProfileAssetSetup
    {
        static GameAudioProfileAssetSetup()
        {
            EditorApplication.delayCall += CreateProfileIfMissing;
        }

        private static void CreateProfileIfMissing()
        {
            EnsureResourcesProfile();
        }

        public static GameAudioProfile EnsureResourcesProfile()
        {
            GameAudioProfile existing = AssetDatabase.LoadAssetAtPath<GameAudioProfile>(
                GameAudioMenuItems.ResourcesProfilePath);

            if (existing != null)
                return existing;

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                    AssetDatabase.CreateFolder("Assets", "_Project");
                AssetDatabase.CreateFolder("Assets/_Project", "Resources");
            }

            GameAudioProfile asset = ScriptableObject.CreateInstance<GameAudioProfile>();
            AssetDatabase.CreateAsset(asset, GameAudioMenuItems.ResourcesProfilePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Created default Game Audio Profile at {GameAudioMenuItems.ResourcesProfilePath}. Assign audio clips in the inspector.");
            return asset;
        }
    }
}
#endif
